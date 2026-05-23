using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Globalization;

namespace Photobooth.BoothNative;

public partial class MainWindow : Window
{
    private const int WmDeviceChange = 0x0219;
    private const int DbtDeviceArrival = 0x8000;
    private const int DbtDeviceRemoveComplete = 0x8004;
    private const int DbtDevNodesChanged = 0x0007;

    private readonly BoothDataService _dataService = new();
    private readonly CameraDetectionService _cameraDetectionService = new();
    private readonly DigiCamControlService _digiCamControlService = new();
    private readonly DispatcherTimer _statusTimer = new() { Interval = TimeSpan.FromSeconds(3) };
    private readonly DispatcherTimer _previewTimer = new() { Interval = TimeSpan.FromMilliseconds(66) };
    private readonly HttpClient _previewClient = new() { Timeout = TimeSpan.FromSeconds(1) };
    private string _currentLanguage = "zh-CN";
    private BoothSnapshot? _lastSnapshot;
    private BoothRuntimeStatus? _lastRuntime;
    private HwndSource? _hwndSource;
    private bool _isRefreshing;
    private bool _isApplyingLanguage;
    private bool _isPreviewRefreshing;
    private bool _isLoadingCameraParameters;
    private bool _isCapturing;
    private bool _isInitializingLive;
    private bool _isRenderingTemplate;
    private bool _isBindingTemplateList;
    private int _previewRequestVersion;
    private double _currentPreviewAspectRatio = 16d / 9d;
    private string? _selectedSessionId;
    private string? _selectedTemplateId;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
        _statusTimer.Tick += StatusTimer_Tick;
        _previewTimer.Tick += PreviewTimer_Tick;
        _cameraDetectionService.RuntimeStatusChanged += CameraDetectionService_RuntimeStatusChanged;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _lastSnapshot = await _dataService.LoadAsync();
        _currentLanguage = Localization.NormalizeLanguage(_lastSnapshot.PreferredLanguage);
        _selectedSessionId = _lastSnapshot.ActiveSessionId;
        _selectedTemplateId = _lastSnapshot.SelectedTemplateId ?? _lastSnapshot.Templates.FirstOrDefault()?.Id;
        SelectLanguage(_currentLanguage);
        _hwndSource = PresentationSource.FromVisual(this) as HwndSource;
        _hwndSource?.AddHook(WndProc);
        _lastRuntime = await _cameraDetectionService.StartAsync();
        _statusTimer.Start();
        _previewTimer.Start();
        await RefreshUiAsync(Localization.Get(_currentLanguage, "starting_auto_detection"));
        await InitializeLiveBridgeAsync(isManual: false);
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _statusTimer.Stop();
        _previewTimer.Stop();
        if (_hwndSource is not null)
        {
            _hwndSource.RemoveHook(WndProc);
        }

        _cameraDetectionService.RuntimeStatusChanged -= CameraDetectionService_RuntimeStatusChanged;
        _cameraDetectionService.Dispose();
        _digiCamControlService.Dispose();
        _previewClient.Dispose();
    }

    private async void StatusTimer_Tick(object? sender, EventArgs e)
    {
        await RefreshUiAsync(RuntimeStatusText.Text);
    }

    private async void PreviewTimer_Tick(object? sender, EventArgs e)
    {
        if (_lastRuntime is not null)
        {
            await UpdateLivePreviewAsync(_lastRuntime);
        }
    }

    private void LivePreviewViewport_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdatePreviewFrameLayout();
    }

    private async void RefreshDevicesButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshUiAsync(Localization.Get(_currentLanguage, "manual_refresh_complete"));
    }

    private async void CreateSessionButton_Click(object sender, RoutedEventArgs e)
    {
        var session = await _dataService.CreateSessionAsync(GetSelectedCaptureMode(), GetShotCount(), GetCountdownSeconds());
        _selectedSessionId = session.Id;
        await RefreshUiAsync($"{Localization.Get(_currentLanguage, "session_created")} {session.Id}.");
    }

    private async void LaunchDigiCamControlButton_Click(object sender, RoutedEventArgs e)
    {
        LaunchDigiCamControlButton.IsEnabled = false;
        try
        {
            var ok = await _digiCamControlService.LaunchOrAttachAsync();
            RuntimeStatusText.Text = ok ? Localization.Get(_currentLanguage, "launch_digicamcontrol_done") : Localization.Get(_currentLanguage, "launch_digicamcontrol_failed");
            await RefreshUiAsync(RuntimeStatusText.Text);
        }
        finally
        {
            LaunchDigiCamControlButton.IsEnabled = true;
        }
    }

    private async void StartLiveButton_Click(object sender, RoutedEventArgs e)
    {
        await InitializeLiveBridgeAsync(isManual: true);
    }

    private async void CaptureNowButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isCapturing)
        {
            RuntimeStatusText.Text = Localization.Get(_currentLanguage, "capture_in_progress");
            return;
        }

        if (_lastRuntime is null || !_lastRuntime.BridgeReachable)
        {
            RuntimeStatusText.Text = Localization.Get(_currentLanguage, "diagnostics_bridge_missing");
            return;
        }

        _isCapturing = true;
        CaptureNowButton.IsEnabled = false;
        try
        {
            var session = await _dataService.GetOrCreateActiveSessionAsync(GetSelectedCaptureMode(), GetShotCount(), GetCountdownSeconds());
            _selectedSessionId = session.Id;
            UpdateActiveSessionText(session);

            var countdown = GetCountdownSeconds();
            for (var current = countdown; current > 0; current--)
            {
                RuntimeStatusText.Text = $"{Localization.Get(_currentLanguage, "countdown_seconds")} {current}";
                await Task.Delay(1000);
            }

            var originalsFolder = Path.Combine(session.FolderPath, "originals");
            var filePrefix = $"{session.Id}_{DateTimeOffset.Now:HHmmssfff}";
            RuntimeStatusText.Text = Localization.Get(_currentLanguage, "capture_starting");
            var captureTask = _digiCamControlService.CapturePhotoAsync(originalsFolder, filePrefix);
            RuntimeStatusText.Text = Localization.Get(_currentLanguage, "capture_waiting_transfer");
            var result = await captureTask;
            if (!result.Success || string.IsNullOrWhiteSpace(result.FilePath))
            {
                RuntimeStatusText.Text = $"{Localization.Get(_currentLanguage, "capture_failed")} {result.Message}";
                return;
            }

            await _dataService.AddCapturedPhotoAsync(session.Id, result.FilePath);
            _lastSnapshot = await _dataService.LoadAsync();
            RenderLists();
            SelectGalleryPhotoByPath(result.FilePath);
            var autoRenderedPath = await TryFinalizeTemplateIfReadyAsync(session);
            RuntimeStatusText.Text = autoRenderedPath is null
                ? Localization.Get(_currentLanguage, "capture_saved")
                : $"{GetTemplateRenderedMessage()} {Path.GetFileName(autoRenderedPath)}";
        }
        finally
        {
            _isCapturing = false;
            CaptureNowButton.IsEnabled = true;
        }
    }

    private async void AutoFocusButton_Click(object sender, RoutedEventArgs e)
    {
        var ok = await _digiCamControlService.AutoFocusAsync();
        RuntimeStatusText.Text = ok ? Localization.Get(_currentLanguage, "focus_done") : Localization.Get(_currentLanguage, "focus_failed");
    }

    private async void OpenSessionFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var session = await _dataService.GetOrCreateActiveSessionAsync(GetSelectedCaptureMode(), GetShotCount(), GetCountdownSeconds());
        _selectedSessionId = session.Id;
        Process.Start(new ProcessStartInfo(session.FolderPath) { UseShellExecute = true });
    }

    private async void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isApplyingLanguage)
        {
            return;
        }

        var selectedLanguage = ((LanguageCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString()) ?? "zh-CN";
        selectedLanguage = Localization.NormalizeLanguage(selectedLanguage);
        if (selectedLanguage == _currentLanguage)
        {
            return;
        }

        _currentLanguage = selectedLanguage;
        await _dataService.SavePreferredLanguageAsync(_currentLanguage);
        ApplyLanguage();
        RenderLists();
    }

    private async void ParameterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingCameraParameters || sender is not ComboBox combo || combo.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        var key = combo.Tag?.ToString();
        var value = item.Tag?.ToString() ?? item.Content?.ToString();
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var ok = await _digiCamControlService.SetParameterAsync(key, value);
        RuntimeStatusText.Text = ok ? Localization.Get(_currentLanguage, "parameter_updated") : Localization.Get(_currentLanguage, "parameter_failed");
    }

    private async void RecentSessionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RecentSessionsList.SelectedItem is not ListBoxItem item || item.Tag is not string sessionId)
        {
            return;
        }

        _selectedSessionId = sessionId;
        await _dataService.SetActiveSessionAsync(sessionId);
        _lastSnapshot = await _dataService.LoadAsync();
        RenderLists();
    }

    private async void TemplateList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isBindingTemplateList)
        {
            return;
        }

        if (TemplateList.SelectedItem is not ListBoxItem item || item.Tag is not NativeTemplateRecord template)
        {
            return;
        }

        _selectedTemplateId = template.Id;
        ShotCountTextBox.Text = GetRequiredPhotoCount(template).ToString(CultureInfo.InvariantCulture);
        await _dataService.SaveSelectedTemplateAsync(template.Id);
        _lastSnapshot = await _dataService.LoadAsync();
        SelectedTemplateText.Text = BuildSelectedTemplateText(template);
        TemplateProgressText.Text = BuildTemplateProgressText(template, GetCurrentSessionPhotos());
        UpdateTemplateWorkspacePreview();
        RuntimeStatusText.Text = $"{GetTemplateActionLabel()}: {Localization.GetTemplateName(_currentLanguage, template)}";
        RenderLists();
    }

    private void PreviousTemplateButton_Click(object sender, RoutedEventArgs e)
    {
        MoveTemplateSelection(-1);
    }

    private void NextTemplateButton_Click(object sender, RoutedEventArgs e)
    {
        MoveTemplateSelection(1);
    }

    private async void RetakeLastSlotButton_Click(object sender, RoutedEventArgs e)
    {
        var session = GetSelectedSession();
        if (session is null)
        {
            RuntimeStatusText.Text = _currentLanguage == "zh-CN" ? "当前没有可重拍的 Session。" : "There is no active session to retake.";
            return;
        }

        var removed = await _dataService.RemoveLastSessionPhotoAsync(session.Id);
        _lastSnapshot = await _dataService.LoadAsync();
        RenderLists();
        RuntimeStatusText.Text = removed is null
            ? (_currentLanguage == "zh-CN" ? "当前没有可重拍的照片。" : "There is no captured photo to retake.")
            : (_currentLanguage == "zh-CN" ? "已移除上一格照片，请重新拍摄这一格。" : "The last slot was cleared. Capture again to refill it.");
    }

    private async void ResetTemplateBoardButton_Click(object sender, RoutedEventArgs e)
    {
        var session = GetSelectedSession();
        if (session is null)
        {
            RuntimeStatusText.Text = _currentLanguage == "zh-CN" ? "当前没有可重置的 Session。" : "There is no active session to reset.";
            return;
        }

        await _dataService.ClearSessionPhotosAsync(session.Id);
        _lastSnapshot = await _dataService.LoadAsync();
        RenderLists();
        RuntimeStatusText.Text = _currentLanguage == "zh-CN" ? "模板看板已清空，可以重新开始填格。" : "Template board cleared. You can start filling slots again.";
    }

    private void GalleryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GalleryList.SelectedItem is not ListBoxItem item || item.Tag is not NativePhotoRecord photo)
        {
            return;
        }

        DisplayGalleryPhoto(photo.FilePath);
    }

    private async void RenderTemplateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRenderingTemplate)
        {
            return;
        }

        var session = await _dataService.GetOrCreateActiveSessionAsync(GetSelectedCaptureMode(), GetShotCount(), GetCountdownSeconds());
        _selectedSessionId = session.Id;
        var template = GetSelectedTemplate();
        if (template is null)
        {
            RuntimeStatusText.Text = GetNoTemplateSelectedMessage();
            return;
        }

        var photos = (_lastSnapshot?.GalleryPhotos ?? [])
            .Where(x => x.SessionId == session.Id && File.Exists(x.FilePath))
            .OrderBy(x => x.CapturedAt)
            .ToList();

        if (photos.Count == 0)
        {
            RuntimeStatusText.Text = GetNoTemplatePhotosMessage();
            return;
        }

        _isRenderingTemplate = true;
        RenderTemplateButton.IsEnabled = false;
        try
        {
            RuntimeStatusText.Text = GetTemplateRenderingMessage();
            var outputPath = await RenderTemplateOutputAsync(session, template, photos);
            RuntimeStatusText.Text = $"{GetTemplateRenderedMessage()} {Path.GetFileName(outputPath)}";
            Process.Start(new ProcessStartInfo(outputPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            RuntimeStatusText.Text = $"{GetTemplateRenderFailedMessage()} {ex.Message}";
        }
        finally
        {
            RenderTemplateButton.IsEnabled = true;
            _isRenderingTemplate = false;
        }
    }

    private async Task RefreshUiAsync(string runtimeStatus)
    {
        if (_isRefreshing)
        {
            return;
        }

        _isRefreshing = true;
        try
        {
            _lastSnapshot ??= await _dataService.LoadAsync();
            _lastRuntime = await _cameraDetectionService.RefreshAsync();
            await LoadCameraParametersAsync(_lastRuntime);
            ApplyRuntimeToUi(_lastRuntime);
            ApplyLanguage();
            await UpdateLivePreviewAsync(_lastRuntime, forceRefresh: true);
            RenderLists();
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private void CameraDetectionService_RuntimeStatusChanged(object? sender, BoothRuntimeStatus runtime)
    {
        Dispatcher.InvokeAsync(async () =>
        {
            _lastRuntime = runtime;
            await LoadCameraParametersAsync(runtime);
            ApplyRuntimeToUi(runtime);
            await UpdateLivePreviewAsync(runtime, forceRefresh: true);
            RenderLists();
        });
    }

    private void ApplyRuntimeToUi(BoothRuntimeStatus runtime)
    {
        if (!_isCapturing)
        {
            RuntimeStatusText.Text = Localization.GetRuntimeStatus(_currentLanguage, runtime);
        }

        DeviceSummaryText.Text = Localization.GetRuntimeStatus(_currentLanguage, runtime);
        TemplateStatText.Text = (_lastSnapshot?.Templates.Count ?? 0).ToString();
        ModeStatText.Text = Localization.GetCaptureMode(_currentLanguage, GetSelectedCaptureMode()).ToUpperInvariant();
        ShotsStatText.Text = ShotCountTextBox.Text;
        CountdownStatText.Text = $"{CountdownTextBox.Text}s";
        UpdateActiveSessionText(GetSelectedSession());
    }

    private void ApplyLanguage()
    {
        _isApplyingLanguage = true;
        try
        {
            Title = Localization.Get(_currentLanguage, "app_title");
            HeaderTitleText.Text = Localization.Get(_currentLanguage, "header_title");
            HeaderSubtitleText.Text = Localization.Get(_currentLanguage, "header_subtitle");
            RefreshDevicesButton.Content = Localization.Get(_currentLanguage, "refresh_devices");
            CreateSessionButton.Content = Localization.Get(_currentLanguage, "new_session");
            LaunchDigiCamControlButton.Content = Localization.Get(_currentLanguage, "launch_digicamcontrol");
            StartLiveButton.Content = Localization.Get(_currentLanguage, "start_live_manual");
            CaptureNowButton.Content = Localization.Get(_currentLanguage, "capture_now");
            AutoFocusButton.Content = Localization.Get(_currentLanguage, "auto_focus");
            OpenSessionFolderButton.Content = Localization.Get(_currentLanguage, "open_session_folder");
            LanguageLabelText.Text = Localization.Get(_currentLanguage, "language");
            ((ComboBoxItem)LanguageCombo.Items[0]).Content = Localization.Get(_currentLanguage, "language_chinese");
            ((ComboBoxItem)LanguageCombo.Items[1]).Content = Localization.Get(_currentLanguage, "language_english");
            StageTitleText.Text = Localization.Get(_currentLanguage, "stage_title");
            StageSubtitleText.Text = Localization.Get(_currentLanguage, "stage_subtitle");
            LivePreviewWaitingText.Text = Localization.Get(_currentLanguage, "waiting_live_view");
            LivePreviewHintText.Text = Localization.Get(_currentLanguage, "placeholder_hint");
            ModeStatLabelText.Text = Localization.Get(_currentLanguage, "stat_mode");
            ShotsStatLabelText.Text = Localization.Get(_currentLanguage, "stat_shots");
            CountdownStatLabelText.Text = Localization.Get(_currentLanguage, "stat_countdown");
            TemplateStatLabelText.Text = Localization.Get(_currentLanguage, "stat_templates");
            RecentSessionsTitleText.Text = Localization.Get(_currentLanguage, "recent_sessions");
            PhotoGalleryTitleText.Text = Localization.Get(_currentLanguage, "photo_gallery");
            GalleryEmptyText.Text = Localization.Get(_currentLanguage, "gallery_empty");
            TemplatePackTitleText.Text = Localization.Get(_currentLanguage, "template_pack");
            RenderTemplateButton.Content = GetRenderTemplateButtonLabel();
            PreviousTemplateButton.Content = _currentLanguage == "zh-CN" ? "上一个模板" : "Previous";
            NextTemplateButton.Content = _currentLanguage == "zh-CN" ? "下一个模板" : "Next";
            RetakeLastSlotButton.Content = _currentLanguage == "zh-CN" ? "重拍上一格" : "Retake Last";
            ResetTemplateBoardButton.Content = _currentLanguage == "zh-CN" ? "清空看板" : "Reset Board";
            TemplatePreviewEmptyText.Text = GetTemplatePreviewEmptyMessage();
            ControlsTitleText.Text = Localization.Get(_currentLanguage, "controls_title");
            ControlsSubtitleText.Text = Localization.Get(_currentLanguage, "controls_subtitle");
            CaptureModeLabelText.Text = Localization.Get(_currentLanguage, "capture_mode");
            ShotCountLabelText.Text = Localization.Get(_currentLanguage, "shot_count");
            CountdownLabelText.Text = Localization.Get(_currentLanguage, "countdown_seconds");
            CameraParametersTitleText.Text = Localization.Get(_currentLanguage, "camera_parameters");
            ActiveSessionLabelText.Text = Localization.Get(_currentLanguage, "active_session");
            IsoLabelText.Text = Localization.Get(_currentLanguage, "iso");
            ShutterLabelText.Text = Localization.Get(_currentLanguage, "shutter_speed");
            ApertureLabelText.Text = Localization.Get(_currentLanguage, "aperture");
            WhiteBalanceLabelText.Text = Localization.Get(_currentLanguage, "white_balance");
            ExposureCompLabelText.Text = Localization.Get(_currentLanguage, "exposure_comp");
            DetectedDevicesTitleText.Text = Localization.Get(_currentLanguage, "detected_devices");
            if (_lastRuntime is null)
            {
                DeviceSummaryText.Text = Localization.Get(_currentLanguage, "checking_bridge");
            }

            ((ComboBoxItem)CaptureModeCombo.Items[0]).Content = Localization.Get(_currentLanguage, "mode_single");
            ((ComboBoxItem)CaptureModeCombo.Items[1]).Content = Localization.Get(_currentLanguage, "mode_multi");
            ((ComboBoxItem)CaptureModeCombo.Items[2]).Content = Localization.Get(_currentLanguage, "mode_burst");
            SelectedTemplateText.Text = BuildSelectedTemplateText(GetSelectedTemplate());
            TemplateProgressText.Text = BuildTemplateProgressText(GetSelectedTemplate(), GetCurrentSessionPhotos());
        }
        finally
        {
            _isApplyingLanguage = false;
        }
    }

    private async Task InitializeLiveBridgeAsync(bool isManual)
    {
        if (_isInitializingLive)
        {
            return;
        }

        _isInitializingLive = true;
        StartLiveButton.IsEnabled = false;
        try
        {
            RuntimeStatusText.Text = Localization.Get(_currentLanguage, isManual ? "start_live_manual_status" : "start_live_auto_status");
            var ok = await _digiCamControlService.InitializeLiveBridgeAsync(launchIfNeeded: true);
            RuntimeStatusText.Text = ok
                ? Localization.Get(_currentLanguage, "start_live_success")
                : Localization.Get(_currentLanguage, "start_live_failed");
            await RefreshUiAsync(RuntimeStatusText.Text);
        }
        finally
        {
            StartLiveButton.IsEnabled = true;
            _isInitializingLive = false;
        }
    }

    private void RenderLists()
    {
        RecentSessionsList.Items.Clear();
        if (_lastSnapshot is not null)
        {
            foreach (var session in _lastSnapshot.RecentSessions)
            {
                RecentSessionsList.Items.Add(BuildSessionItem(session, _currentLanguage, session.Id == (_selectedSessionId ?? _lastSnapshot.ActiveSessionId)));
            }
        }

        TemplateList.Items.Clear();
        if (_lastSnapshot is not null)
        {
            _isBindingTemplateList = true;
            try
            {
                foreach (var template in _lastSnapshot.Templates)
                {
                    TemplateList.Items.Add(BuildTemplateItem(template, template.Id == (_selectedTemplateId ?? _lastSnapshot.SelectedTemplateId)));
                }
            }
            finally
            {
                _isBindingTemplateList = false;
            }
        }

        DevicesList.Items.Clear();
        if (_lastRuntime is not null)
        {
            foreach (var device in _lastRuntime.Devices)
            {
                DevicesList.Items.Add(BuildDeviceRow(device, _currentLanguage));
            }
        }

        RenderGallery();
        SelectedTemplateText.Text = BuildSelectedTemplateText(GetSelectedTemplate());
        TemplateProgressText.Text = BuildTemplateProgressText(GetSelectedTemplate(), GetCurrentSessionPhotos());
        UpdateTemplateWorkspacePreview();
        UpdateActiveSessionText(GetSelectedSession());
    }

    private void RenderGallery()
    {
        GalleryList.Items.Clear();
        var selectedSessionId = _selectedSessionId ?? _lastSnapshot?.ActiveSessionId;
        var photos = _lastSnapshot?.GalleryPhotos
            .Where(x => string.IsNullOrWhiteSpace(selectedSessionId) || x.SessionId == selectedSessionId)
            .OrderByDescending(x => x.CapturedAt)
            .ToList() ?? new List<NativePhotoRecord>();

        foreach (var photo in photos)
        {
            GalleryList.Items.Add(BuildGalleryItem(photo));
        }

        if (photos.Count == 0)
        {
            GalleryPreviewImage.Source = null;
            GalleryPreviewImage.Visibility = Visibility.Collapsed;
            GalleryEmptyText.Visibility = Visibility.Visible;
        }
        else
        {
            SelectGalleryPhotoByPath(photos[0].FilePath);
        }
    }

    private void SelectGalleryPhotoByPath(string path)
    {
        foreach (var item in GalleryList.Items.OfType<ListBoxItem>())
        {
            if (item.Tag is NativePhotoRecord photo && string.Equals(photo.FilePath, path, StringComparison.OrdinalIgnoreCase))
            {
                GalleryList.SelectedItem = item;
                GalleryList.ScrollIntoView(item);
                DisplayGalleryPhoto(photo.FilePath);
                break;
            }
        }
    }

    private void DisplayGalleryPhoto(string path)
    {
        if (!File.Exists(path))
        {
            GalleryPreviewImage.Source = null;
            GalleryPreviewImage.Visibility = Visibility.Collapsed;
            GalleryEmptyText.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 900;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            GalleryPreviewImage.Source = bitmap;
            GalleryPreviewImage.Visibility = Visibility.Visible;
            GalleryEmptyText.Visibility = Visibility.Collapsed;
        }
        catch
        {
            GalleryPreviewImage.Source = null;
            GalleryPreviewImage.Visibility = Visibility.Collapsed;
            GalleryEmptyText.Visibility = Visibility.Visible;
        }
    }

    private void SelectLanguage(string languageCode)
    {
        _isApplyingLanguage = true;
        try
        {
            foreach (var item in LanguageCombo.Items.OfType<ComboBoxItem>())
            {
                if (string.Equals(item.Tag?.ToString(), languageCode, StringComparison.OrdinalIgnoreCase))
                {
                    LanguageCombo.SelectedItem = item;
                    break;
                }
            }
        }
        finally
        {
            _isApplyingLanguage = false;
        }

        ApplyLanguage();
    }

    private async Task UpdateLivePreviewAsync(BoothRuntimeStatus runtime, bool forceRefresh = false)
    {
        if (_isPreviewRefreshing && !forceRefresh)
        {
            return;
        }

        var requestVersion = Interlocked.Increment(ref _previewRequestVersion);

        if (!runtime.LiveViewReachable || string.IsNullOrWhiteSpace(runtime.LiveViewUrl))
        {
            ShowPreviewPlaceholder();
            return;
        }

        _isPreviewRefreshing = true;
        try
        {
            var previewBytes = await _previewClient.GetByteArrayAsync($"{runtime.LiveViewUrl}?t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
            if (requestVersion != _previewRequestVersion || previewBytes.Length == 0)
            {
                return;
            }

            await using var memoryStream = new MemoryStream(previewBytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            bitmap.StreamSource = memoryStream;
            bitmap.EndInit();
            bitmap.Freeze();

            if (bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0)
            {
                _currentPreviewAspectRatio = (double)bitmap.PixelWidth / bitmap.PixelHeight;
            }

            UpdatePreviewFrameLayout();
            LivePreviewImage.Source = bitmap;
            LivePreviewImage.Visibility = Visibility.Visible;
            LivePreviewPlaceholder.Visibility = Visibility.Collapsed;
        }
        catch
        {
            ShowPreviewPlaceholder();
        }
        finally
        {
            _isPreviewRefreshing = false;
        }
    }

    private async Task LoadCameraParametersAsync(BoothRuntimeStatus runtime)
    {
        if (!runtime.BridgeReachable)
        {
            return;
        }

        _isLoadingCameraParameters = true;
        try
        {
            await PopulateParameterComboAsync(IsoComboBox, "iso");
            await PopulateParameterComboAsync(ShutterComboBox, "shutterspeed");
            await PopulateParameterComboAsync(ApertureComboBox, "aperture");
            await PopulateParameterComboAsync(WhiteBalanceComboBox, "whitebalance");
            await PopulateParameterComboAsync(ExposureCompComboBox, "exposurecompensation");
        }
        finally
        {
            _isLoadingCameraParameters = false;
        }
    }

    private async Task PopulateParameterComboAsync(ComboBox comboBox, string parameterKey)
    {
        var options = await _digiCamControlService.ListParameterValuesAsync(parameterKey);
        var current = await _digiCamControlService.GetParameterValueAsync(parameterKey);
        comboBox.Items.Clear();
        comboBox.IsEnabled = options.Count > 0;

        foreach (var option in options)
        {
            var item = new ComboBoxItem
            {
                Content = option,
                Tag = option
            };
            comboBox.Items.Add(item);
            if (!string.IsNullOrWhiteSpace(current) && string.Equals(option.Trim(), current.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedItem = item;
            }
        }

        if (comboBox.SelectedIndex < 0 && comboBox.Items.Count > 0)
        {
            comboBox.SelectedIndex = 0;
        }
    }

    private void ShowPreviewPlaceholder()
    {
        UpdatePreviewFrameLayout();
        LivePreviewImage.Visibility = Visibility.Collapsed;
        LivePreviewPlaceholder.Visibility = Visibility.Visible;
        LivePreviewImage.Source = null;
    }

    private void UpdatePreviewFrameLayout()
    {
        if (LivePreviewViewport is null || LivePreviewFrame is null)
        {
            return;
        }

        var availableWidth = LivePreviewViewport.ActualWidth;
        var availableHeight = LivePreviewViewport.ActualHeight;
        if (availableWidth <= 0 || availableHeight <= 0)
        {
            return;
        }

        var aspectRatio = _currentPreviewAspectRatio <= 0 ? 16d / 9d : _currentPreviewAspectRatio;
        var frameWidth = availableWidth;
        var frameHeight = frameWidth / aspectRatio;

        if (frameHeight > availableHeight)
        {
            frameHeight = availableHeight;
            frameWidth = frameHeight * aspectRatio;
        }

        LivePreviewFrame.Width = Math.Max(320, frameWidth);
        LivePreviewFrame.Height = Math.Max(180, frameHeight);
    }

    private string GetSelectedCaptureMode() => ((CaptureModeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "multi").Trim();

    private int GetShotCount() => int.TryParse(ShotCountTextBox.Text, out var shotCount) && shotCount > 0 ? shotCount : 3;

    private int GetCountdownSeconds() => int.TryParse(CountdownTextBox.Text, out var countdownSeconds) && countdownSeconds >= 0 ? countdownSeconds : 3;

    private NativeSessionRecord? GetSelectedSession()
    {
        var sessionId = _selectedSessionId ?? _lastSnapshot?.ActiveSessionId;
        return _lastSnapshot?.RecentSessions.FirstOrDefault(x => x.Id == sessionId) ?? _lastSnapshot?.RecentSessions.FirstOrDefault();
    }

    private void UpdateActiveSessionText(NativeSessionRecord? session)
    {
        ActiveSessionText.Text = session is null ? "-" : $"{session.Id}\n{session.FolderPath}";
    }

    private static ListBoxItem BuildSessionItem(NativeSessionRecord session, string languageCode, bool selected)
    {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = session.Id, FontWeight = FontWeights.SemiBold, FontSize = 14 });
        panel.Children.Add(new TextBlock
        {
            Text = $"{Localization.GetCaptureMode(languageCode, session.CaptureMode)} - {session.PhotoCount} - {session.CountdownSeconds}s",
            Foreground = Brushes.Gray,
            FontSize = 12,
            Margin = new Thickness(0, 4, 0, 0)
        });
        panel.Children.Add(new TextBlock
        {
            Text = session.FolderPath,
            Foreground = Brushes.DarkGray,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 0)
        });

        return new ListBoxItem
        {
            Tag = session.Id,
            Background = selected ? (Brush)Application.Current.Resources["PanelBrush"] : (Brush)Application.Current.Resources["PanelSoftBrush"],
            BorderBrush = (Brush)Application.Current.Resources["BorderBrushSoft"],
            BorderThickness = new Thickness(1),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 12),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Content = panel
        };
    }

    private static ListBoxItem BuildGalleryItem(NativePhotoRecord photo)
    {
        var dock = new DockPanel();
        var image = new Image { Width = 72, Height = 54, Stretch = Stretch.UniformToFill, Margin = new Thickness(0, 0, 12, 0) };
        try
        {
            if (File.Exists(photo.FilePath))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 160;
                bitmap.UriSource = new Uri(photo.FilePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                image.Source = bitmap;
            }
        }
        catch
        {
        }

        DockPanel.SetDock(image, Dock.Left);
        dock.Children.Add(image);

        var textPanel = new StackPanel();
        textPanel.Children.Add(new TextBlock { Text = photo.FileName, FontWeight = FontWeights.SemiBold, FontSize = 13, TextWrapping = TextWrapping.Wrap });
        textPanel.Children.Add(new TextBlock
        {
            Text = photo.CapturedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            Foreground = Brushes.Gray,
            FontSize = 11,
            Margin = new Thickness(0, 4, 0, 0)
        });
        dock.Children.Add(textPanel);

        return new ListBoxItem
        {
            Tag = photo,
            Background = (Brush)Application.Current.Resources["PanelSoftBrush"],
            BorderBrush = (Brush)Application.Current.Resources["BorderBrushSoft"],
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 10),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Content = dock
        };
    }

    private ListBoxItem BuildTemplateItem(NativeTemplateRecord template, bool isSelected)
    {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = Localization.GetTemplateName(_currentLanguage, template),
            FontWeight = FontWeights.SemiBold,
            FontSize = 14
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"{template.PaperSize} · {Localization.GetTemplateDescription(_currentLanguage, template)}",
            Margin = new Thickness(0, 4, 0, 0),
            Foreground = Brushes.Gray,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap
        });

        return new ListBoxItem
        {
            Tag = template,
            Background = isSelected ? (Brush)Application.Current.Resources["PanelBrush"] : (Brush)Application.Current.Resources["PanelSoftBrush"],
            BorderBrush = (Brush)Application.Current.Resources["BorderBrushSoft"],
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 10),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Content = panel,
            IsSelected = isSelected
        };
    }

    private NativeTemplateRecord? GetSelectedTemplate()
    {
        return _lastSnapshot?.Templates.FirstOrDefault(x => x.Id == (_selectedTemplateId ?? _lastSnapshot.SelectedTemplateId))
            ?? _lastSnapshot?.Templates.FirstOrDefault();
    }

    private string BuildSelectedTemplateText(NativeTemplateRecord? template)
    {
        if (template is null)
        {
            return _currentLanguage == "zh-CN" ? "当前模板：未选择" : "Active template: none selected";
        }

        return _currentLanguage == "zh-CN"
            ? $"当前模板：{Localization.GetTemplateName(_currentLanguage, template)} · {template.PaperSize}"
            : $"Active template: {Localization.GetTemplateName(_currentLanguage, template)} · {template.PaperSize}";
    }

    private string GetRenderTemplateButtonLabel() => _currentLanguage == "zh-CN" ? "生成模板成片" : "Render Template";

    private string GetTemplateActionLabel() => _currentLanguage == "zh-CN" ? "已选择模板" : "Template selected";

    private string GetNoTemplateSelectedMessage() => _currentLanguage == "zh-CN" ? "请先选择一个模板。" : "Choose a template first.";

    private string GetNoTemplatePhotosMessage() => _currentLanguage == "zh-CN" ? "当前 Session 还没有照片，无法生成模板成片。" : "This session has no photos yet, so the template cannot be rendered.";

    private string GetTemplateRenderingMessage() => _currentLanguage == "zh-CN" ? "正在生成模板成片..." : "Rendering template output...";

    private string GetTemplateRenderedMessage() => _currentLanguage == "zh-CN" ? "模板成片已生成：" : "Template output rendered:";

    private string GetTemplateRenderFailedMessage() => _currentLanguage == "zh-CN" ? "模板成片生成失败。" : "Template rendering failed.";

    private string GetTemplatePreviewEmptyMessage() => _currentLanguage == "zh-CN"
        ? "这里会显示模板拼贴预览。拍一张，填一格。"
        : "The collage preview appears here. Each new shot fills the next slot.";

    private void MoveTemplateSelection(int offset)
    {
        if (_lastSnapshot is null || _lastSnapshot.Templates.Count == 0)
        {
            return;
        }

        var templates = _lastSnapshot.Templates;
        var currentIndex = templates.FindIndex(x => x.Id == (_selectedTemplateId ?? _lastSnapshot.SelectedTemplateId));
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        var nextIndex = (currentIndex + offset + templates.Count) % templates.Count;
        var nextTemplate = templates[nextIndex];
        _selectedTemplateId = nextTemplate.Id;
        ShotCountTextBox.Text = GetRequiredPhotoCount(nextTemplate).ToString(CultureInfo.InvariantCulture);

        foreach (var item in TemplateList.Items.OfType<ListBoxItem>())
        {
            if (item.Tag is NativeTemplateRecord template && template.Id == nextTemplate.Id)
            {
                TemplateList.SelectedItem = item;
                TemplateList.ScrollIntoView(item);
                break;
            }
        }
    }

    private string BuildTemplateProgressText(NativeTemplateRecord? template, List<NativePhotoRecord> photos)
    {
        if (template is null)
        {
            return _currentLanguage == "zh-CN" ? "模板进度：未选择模板" : "Template progress: no template selected";
        }

        var slotCount = GetRequiredPhotoCount(template);
        var filledCount = Math.Min(photos.Count, slotCount);
        return _currentLanguage == "zh-CN"
            ? $"模板进度：已填 {filledCount}/{slotCount} 格"
            : $"Template progress: {filledCount}/{slotCount} slots filled";
    }

    private async Task<string> RenderTemplateOutputAsync(NativeSessionRecord session, NativeTemplateRecord template, List<NativePhotoRecord> photos)
    {
        await Task.Yield();
        var (width, height) = GetTemplateCanvasSize(template);
        var outputPath = Path.Combine(session.FolderPath, "outputs", $"{session.Id}_{template.Id}_{DateTimeOffset.Now:HHmmss}.jpg");
        var bitmap = CreateTemplateBitmap(template, photos, width, height);

        var encoder = new JpegBitmapEncoder { QualityLevel = 92 };
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        await using var stream = File.Create(outputPath);
        encoder.Save(stream);
        return outputPath;
    }

    private static (int Width, int Height) GetTemplateCanvasSize(NativeTemplateRecord template) => template.Id switch
    {
        "strip-2x6" => (900, 2700),
        "square-collage" => (2200, 2200),
        _ => (1800, 1200)
    };

    private void DrawTemplateLayout(DrawingContext dc, NativeTemplateRecord template, List<NativePhotoRecord> photos, int width, int height)
    {
        switch (template.Id)
        {
            case "single-hero":
                DrawPhotoSlot(dc, photos, 0, new Rect(80, 80, width - 160, height - 160), "1");
                break;
            case "grid-4x6":
                DrawGridLayout(dc, photos, 2, 3, 44, 54, width, height);
                break;
            case "strip-2x6":
                DrawGridLayout(dc, photos, 1, 4, 56, 70, width, height);
                break;
            case "square-collage":
                DrawGridLayout(dc, photos, 2, 2, 60, 60, width, height);
                break;
            default:
                DrawPhotoSlot(dc, photos, 0, new Rect(70, 70, width * 0.56, height - 140), "1");
                DrawGridLayout(dc, photos, 1, 2, width * 0.68, 120, width * 0.24, (height - 300) / 2d, 1);
                break;
        }

        DrawFooterText(dc, Localization.GetTemplateName(_currentLanguage, template), width, height);
    }

    private void DrawGridLayout(DrawingContext dc, List<NativePhotoRecord> photos, int columns, int rows, double marginX, double marginY, int width, int height, int photoStartIndex = 0)
    {
        var cellWidth = (width - (marginX * 2) - ((columns - 1) * 28)) / columns;
        var cellHeight = (height - (marginY * 2) - ((rows - 1) * 28)) / rows;
        var index = 0;
        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < columns; col++)
            {
                var x = marginX + (col * (cellWidth + 28));
                var y = marginY + (row * (cellHeight + 28));
                DrawPhotoSlot(dc, photos, photoStartIndex + index, new Rect(x, y, cellWidth, cellHeight), (photoStartIndex + index + 1).ToString(CultureInfo.InvariantCulture));
                index++;
            }
        }
    }

    private void DrawGridLayout(DrawingContext dc, List<NativePhotoRecord> photos, int columns, int rows, double startX, double startY, double cellWidth, double cellHeight, int photoStartIndex)
    {
        var index = 0;
        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < columns; col++)
            {
                var x = startX + (col * (cellWidth + 28));
                var y = startY + (row * (cellHeight + 28));
                DrawPhotoSlot(dc, photos, photoStartIndex + index, new Rect(x, y, cellWidth, cellHeight), (photoStartIndex + index + 1).ToString(CultureInfo.InvariantCulture));
                index++;
            }
        }
    }

    private void DrawPhotoSlot(DrawingContext dc, List<NativePhotoRecord> photos, int index, Rect target, string slotLabel)
    {
        if (index < photos.Count)
        {
            DrawPhoto(dc, photos[index].FilePath, target);
            return;
        }

        DrawEmptySlot(dc, target, slotLabel);
    }

    private void DrawPhoto(DrawingContext dc, string path, Rect target)
    {
        dc.DrawRoundedRectangle(Brushes.White, new Pen(new SolidColorBrush(Color.FromRgb(214, 196, 164)), 2), target, 18, 18);

        if (!File.Exists(path))
        {
            return;
        }

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(path, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();

        dc.PushClip(new RectangleGeometry(target, 18, 18));
        var scale = Math.Max(target.Width / bitmap.PixelWidth, target.Height / bitmap.PixelHeight);
        var drawWidth = bitmap.PixelWidth * scale;
        var drawHeight = bitmap.PixelHeight * scale;
        var drawRect = new Rect(
            target.X + ((target.Width - drawWidth) / 2),
            target.Y + ((target.Height - drawHeight) / 2),
            drawWidth,
            drawHeight);
        dc.DrawImage(bitmap, drawRect);
        dc.Pop();
    }

    private void DrawEmptySlot(DrawingContext dc, Rect target, string slotLabel)
    {
        var fill = new SolidColorBrush(Color.FromRgb(255, 251, 244));
        var border = new Pen(new SolidColorBrush(Color.FromRgb(214, 196, 164)), 2) { DashStyle = DashStyles.Dash };
        dc.DrawRoundedRectangle(fill, border, target, 18, 18);

        var text = new FormattedText(
            slotLabel,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI Semibold"),
            Math.Max(18, target.Width / 6d),
            new SolidColorBrush(Color.FromRgb(191, 160, 112)),
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        dc.DrawText(text, new Point(target.X + ((target.Width - text.Width) / 2), target.Y + ((target.Height - text.Height) / 2)));
    }

    private RenderTargetBitmap CreateTemplateBitmap(NativeTemplateRecord template, List<NativePhotoRecord> photos, int width, int height)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(248, 242, 232)), null, new Rect(0, 0, width, height));
            DrawTemplateLayout(dc, template, photos, width, height);
        }

        var bitmap = new RenderTargetBitmap(width, height, 300, 300, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        return bitmap;
    }

    private void UpdateTemplateWorkspacePreview()
    {
        var template = GetSelectedTemplate();
        if (template is null)
        {
            TemplatePreviewImage.Source = null;
            TemplatePreviewImage.Visibility = Visibility.Collapsed;
            TemplatePreviewEmptyText.Visibility = Visibility.Visible;
            return;
        }

        var preview = CreateTemplateBitmap(template, GetCurrentSessionPhotos(), 900, 600);
        TemplatePreviewImage.Source = preview;
        TemplatePreviewImage.Visibility = Visibility.Visible;
        TemplatePreviewEmptyText.Visibility = Visibility.Collapsed;
    }

    private List<NativePhotoRecord> GetCurrentSessionPhotos()
    {
        var selectedSessionId = _selectedSessionId ?? _lastSnapshot?.ActiveSessionId;
        return _lastSnapshot?.GalleryPhotos
            .Where(x => string.IsNullOrWhiteSpace(selectedSessionId) || x.SessionId == selectedSessionId)
            .OrderBy(x => x.CapturedAt)
            .ToList() ?? [];
    }

    private int GetRequiredPhotoCount(NativeTemplateRecord template) => template.Id switch
    {
        "single-hero" => 1,
        "grid-4x6" => 6,
        "strip-2x6" => 4,
        "square-collage" => 4,
        _ => 3
    };

    private async Task<string?> TryFinalizeTemplateIfReadyAsync(NativeSessionRecord session)
    {
        var template = GetSelectedTemplate();
        if (template is null)
        {
            return null;
        }

        var photos = GetCurrentSessionPhotos();
        if (photos.Count < GetRequiredPhotoCount(template))
        {
            TemplateProgressText.Text = BuildTemplateProgressText(template, photos);
            UpdateTemplateWorkspacePreview();
            return null;
        }

        return await RenderTemplateOutputAsync(session, template, photos);
    }

    private void DrawFooterText(DrawingContext dc, string title, int width, int height)
    {
        var text = new FormattedText(
            title,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI Semibold"),
            Math.Max(28, width / 40d),
            new SolidColorBrush(Color.FromRgb(84, 63, 40)),
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        dc.DrawText(text, new Point(48, height - text.Height - 26));
    }

    private static Border BuildDeviceRow(NativeDeviceRecord device, string languageCode)
    {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = Localization.GetDeviceName(languageCode, device), FontWeight = FontWeights.SemiBold, FontSize = 14 });
        panel.Children.Add(new TextBlock
        {
            Text = $"{Localization.GetDeviceTransport(languageCode, device)} - {Localization.Get(languageCode, "connection_label")} {Localization.GetConnectionState(languageCode, device)} - {Localization.Get(languageCode, "trigger_label")} {Localization.Get(languageCode, device.RemoteTriggerSupported ? "yes" : "no")} - {Localization.Get(languageCode, "transfer_label")} {Localization.Get(languageCode, device.TransferSupported ? "yes" : "no")} - {Localization.Get(languageCode, "liveview_label")} {Localization.Get(languageCode, device.LiveViewSupported ? "yes" : "no")}",
            Foreground = Brushes.Gray,
            FontSize = 12,
            Margin = new Thickness(0, 4, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Text = Localization.GetDeviceDiagnostics(languageCode, device),
            Foreground = Brushes.DarkGray,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 0)
        });

        return new Border
        {
            Background = (Brush)Application.Current.Resources["PanelSoftBrush"],
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 12),
            BorderBrush = (Brush)Application.Current.Resources["BorderBrushSoft"],
            BorderThickness = new Thickness(1),
            Child = panel
        };
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmDeviceChange)
        {
            var eventCode = wParam.ToInt32();
            if (eventCode == DbtDeviceArrival || eventCode == DbtDeviceRemoveComplete || eventCode == DbtDevNodesChanged)
            {
                _cameraDetectionService.NotifyDeviceTopologyChanged();
            }
        }

        return IntPtr.Zero;
    }
}






