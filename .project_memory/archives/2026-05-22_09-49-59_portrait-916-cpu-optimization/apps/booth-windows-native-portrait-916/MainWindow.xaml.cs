using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Photobooth.BoothNative;

public partial class MainWindow : Window
{
    private const int WmDeviceChange = 0x0219;
    private const int DbtDeviceArrival = 0x8000;
    private const int DbtDeviceRemoveComplete = 0x8004;
    private const int DbtDevNodesChanged = 0x0007;

    private readonly TemplateManager _templateManager = new();
    private readonly FrameOverlayManager _frameOverlayManager;
    private readonly EffectManager _effectManager;
    private readonly BeautyProcessor _beautyProcessor = new();
    private readonly CompositeRenderer _compositeRenderer = new();
    private readonly LivePhotoPreviewService _livePhotoPreviewService = new();
    private readonly PrintService _printService = new();
    private readonly BoothDataService _dataService = new();
    private readonly WebsiteUploadService _websiteUploadService = new();
    private readonly CameraDetectionService _cameraDetectionService = new();
    private readonly DigiCamControlService _digiCamControlService = new();
    private readonly DispatcherTimer _statusTimer = new() { Interval = TimeSpan.FromSeconds(3) };
    private readonly DispatcherTimer _previewTimer = new() { Interval = TimeSpan.FromMilliseconds(66) };
    private readonly DispatcherTimer _photoTransformSaveTimer = new() { Interval = TimeSpan.FromMilliseconds(220) };
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
    private bool _isBindingFrameList;
    private bool _isBindingPresetList;
    private bool _isApplyingUploadedPreview;
    private int _previewRequestVersion;
    private double _currentPreviewAspectRatio = 9d / 16d;
    private string? _selectedSessionId;
    private string? _selectedTemplateId;
    private string? _selectedFrameId;
    private string? _selectedEffectPresetId;
    private string? _uploadedPreviewPath;
    private string _windowOrientation = "Portrait";
    private NativeBeautyLevel _selectedBeautyLevel = NativeBeautyLevel.Off;
    private NativeSourceMode _selectedSourceMode = NativeSourceMode.Camera;
    private NativePreviewStickerKind _selectedStickerKind = NativePreviewStickerKind.None;
    private NativePreviewMaskMode _selectedMaskMode = NativePreviewMaskMode.None;
    private int? _pendingManualSlotIndex;
    private string? _selectedEditablePhotoId;
    private bool _isBindingEditablePhotoList;
    private bool _isBindingSlotList;
    private bool _isApplyingPhotoTransformControls;
    private bool _hasPendingTransformSave;
    private bool _isUploadingToWebsite;
    private FullscreenPreviewWindow? _fullscreenPreviewWindow;

    public MainWindow()
    {
        _frameOverlayManager = new FrameOverlayManager(_templateManager);
        _effectManager = new EffectManager(_templateManager);
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
        _statusTimer.Tick += StatusTimer_Tick;
        _previewTimer.Tick += PreviewTimer_Tick;
        _photoTransformSaveTimer.Tick += PhotoTransformSaveTimer_Tick;
        _cameraDetectionService.RuntimeStatusChanged += CameraDetectionService_RuntimeStatusChanged;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _templateManager.InitializeAsync();
        await _frameOverlayManager.InitializeAsync();
        await _effectManager.InitializeAsync();
        await _beautyProcessor.InitializeAsync();

        _lastSnapshot = await _dataService.LoadAsync();
        _currentLanguage = Localization.NormalizeLanguage(_lastSnapshot.PreferredLanguage);
        _selectedSessionId = _lastSnapshot.ActiveSessionId;
        _selectedTemplateId = _lastSnapshot.SelectedTemplateId ?? _lastSnapshot.Templates.FirstOrDefault()?.Id;
        _selectedFrameId = _lastSnapshot.SelectedFrameId ?? _lastSnapshot.Frames.FirstOrDefault()?.Id;
        _selectedEffectPresetId = _lastSnapshot.SelectedEffectPresetId ?? _lastSnapshot.EffectPresets.FirstOrDefault()?.Id;
        _selectedBeautyLevel = ParseBeautyLevel(_lastSnapshot.SelectedBeautyLevel);
        _selectedSourceMode = ParseSourceMode(_lastSnapshot.SelectedSourceMode);
        _windowOrientation = NormalizeWindowOrientation(_lastSnapshot.PreferredWindowOrientation);

        SelectLanguage(_currentLanguage);
        ApplyWindowOrientation();
        BindBeautyLevels();
        BindFrameSelection();
        BindEffectPresets();
        BindSourceMode();
        BindAssignmentMode();
        BindPreviewControls();
        BindWebsiteUploadControls();

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
        _photoTransformSaveTimer.Stop();
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

    private async void PhotoTransformSaveTimer_Tick(object? sender, EventArgs e)
    {
        _photoTransformSaveTimer.Stop();
        if (!_hasPendingTransformSave)
        {
            return;
        }

        var photo = GetSelectedEditablePhoto();
        if (photo is null)
        {
            _hasPendingTransformSave = false;
            return;
        }

        _hasPendingTransformSave = false;
        var ok = await _dataService.UpdatePhotoTransformAsync(
            photo.Id,
            PhotoScaleSlider.Value,
            PhotoRotationSlider.Value,
            PhotoOffsetXSlider.Value,
            PhotoOffsetYSlider.Value);
        if (!ok)
        {
            return;
        }

        _lastSnapshot = await _dataService.LoadAsync();
        RenderLists();
        RuntimeStatusText.Text = _currentLanguage == "zh-CN" ? "照片编辑参数已更新。" : "Photo edit parameters updated.";
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

    private async void ToggleOrientationButton_Click(object sender, RoutedEventArgs e)
    {
        _windowOrientation = string.Equals(_windowOrientation, "Portrait", StringComparison.OrdinalIgnoreCase)
            ? "Landscape"
            : "Portrait";
        ApplyWindowOrientation();
        await _dataService.SavePreferredWindowOrientationAsync(_windowOrientation);
        _lastSnapshot = await _dataService.LoadAsync();
    }

    private async void CreateSessionButton_Click(object sender, RoutedEventArgs e)
    {
        await EnsurePreferencesSavedAsync();
        var session = await _dataService.CreateSessionAsync(GetSelectedCaptureMode(), GetShotCount(), GetCountdownSeconds(), _selectedSourceMode);
        _selectedSessionId = session.Id;
        _lastSnapshot = await _dataService.LoadAsync();
        RenderLists();
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
        if (_selectedSourceMode != NativeSourceMode.Camera)
        {
            RuntimeStatusText.Text = Localization.Get(_currentLanguage, "source_import_ready");
            return;
        }

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

        await EnsurePreferencesSavedAsync();
        _isCapturing = true;
        CaptureNowButton.IsEnabled = false;

        try
        {
            var session = await _dataService.GetOrCreateActiveSessionAsync(GetSelectedCaptureMode(), GetShotCount(), GetCountdownSeconds(), _selectedSourceMode);
            _selectedSessionId = session.Id;
            UpdateActiveSessionText(session);
            UpdateSessionProgress(session);

            var nextIndex = Math.Min(session.CurrentShotIndex + 1, Math.Max(1, session.RequiredShotCount));
            var countdown = GetCountdownSeconds();
            for (var current = countdown; current > 0; current--)
            {
                RuntimeStatusText.Text = $"{Localization.Get(_currentLanguage, "countdown_seconds")} {current} · {nextIndex}/{session.RequiredShotCount}";
                await Task.Delay(1000);
            }

            var filePrefix = $"{session.Id}_{DateTimeOffset.Now:HHmmssfff}";
            RuntimeStatusText.Text = Localization.Get(_currentLanguage, "capture_starting");
            var captureTask = _digiCamControlService.CapturePhotoAsync(session.RawFolderPath, filePrefix);
            RuntimeStatusText.Text = Localization.Get(_currentLanguage, "capture_waiting_transfer");
            var result = await captureTask;
            if (!result.Success || string.IsNullOrWhiteSpace(result.FilePath))
            {
                RuntimeStatusText.Text = $"{Localization.Get(_currentLanguage, "capture_failed")} {result.Message}";
                return;
            }

            var processedPath = Path.Combine(session.ProcessedFolderPath, $"{Path.GetFileNameWithoutExtension(result.FilePath)}_processed.jpg");
            await _beautyProcessor.ProcessAndSaveAsync(result.FilePath, processedPath, _selectedBeautyLevel, GetSelectedEffectPreset());
            await _dataService.AddCapturedPhotoAsync(session.Id, result.FilePath, processedPath, _selectedBeautyLevel, _selectedEffectPresetId ?? "clean-modern");

            _lastSnapshot = await _dataService.LoadAsync();
            session = GetSelectedSession() ?? session;
            RenderLists();
            SelectGalleryPhotoByPath(processedPath);

            var autoRendered = await TryFinalizeTemplateIfReadyAsync(session);
            RuntimeStatusText.Text = autoRendered is null
                ? Localization.Get(_currentLanguage, "capture_saved")
                : $"{GetTemplateRenderedMessage()} {Path.GetFileName(autoRendered.PngPath)}";
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
        var session = await _dataService.GetOrCreateActiveSessionAsync(GetSelectedCaptureMode(), GetShotCount(), GetCountdownSeconds(), _selectedSourceMode);
        _selectedSessionId = session.Id;
        Process.Start(new ProcessStartInfo(session.FolderPath) { UseShellExecute = true });
    }

    private void OpenFinalButton_Click(object sender, RoutedEventArgs e)
    {
        var session = GetSelectedSession();
        if (session is null || string.IsNullOrWhiteSpace(session.FinalPngPath))
        {
            RuntimeStatusText.Text = _currentLanguage == "zh-CN" ? "当前还没有最终成片。" : "No final composite is available yet.";
            return;
        }

        try
        {
            _printService.OpenFile(session.FinalPngPath);
        }
        catch (Exception ex)
        {
            RuntimeStatusText.Text = ex.Message;
        }
    }

    private void PrintFinalButton_Click(object sender, RoutedEventArgs e)
    {
        var session = GetSelectedSession();
        if (session is null || string.IsNullOrWhiteSpace(session.FinalPngPath))
        {
            RuntimeStatusText.Text = _currentLanguage == "zh-CN" ? "当前还没有最终成片可打印。" : "No final composite is ready for printing.";
            return;
        }

        try
        {
            _printService.OpenForPrintPreview(session.FinalPngPath);
            RuntimeStatusText.Text = _currentLanguage == "zh-CN" ? "已发送最终成片到系统打印流程。" : "Final composite sent to the system print flow.";
        }
        catch (Exception ex)
        {
            RuntimeStatusText.Text = ex.Message;
        }
    }

    private async void RestartSessionButton_Click(object sender, RoutedEventArgs e)
    {
        var session = GetSelectedSession();
        if (session is null)
        {
            RuntimeStatusText.Text = _currentLanguage == "zh-CN" ? "当前没有可重置的 Session。" : "There is no active session to restart.";
            return;
        }

        await _dataService.ClearSessionPhotosAsync(session.Id);
        _lastSnapshot = await _dataService.LoadAsync();
        RenderLists();
        RuntimeStatusText.Text = _currentLanguage == "zh-CN" ? "Session 已重置，可以重新拍摄。" : "Session reset. You can start capturing again.";
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

    private async void BeautyLevelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isApplyingLanguage)
        {
            return;
        }

        _selectedBeautyLevel = ParseBeautyLevel((BeautyLevelComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString());
        await _dataService.SaveBeautyLevelAsync(_selectedBeautyLevel);
        _lastSnapshot = await _dataService.LoadAsync();
        RenderLists();
        await RefreshUploadedPreviewAsync();
    }

    private async void SourceModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isApplyingLanguage)
        {
            return;
        }

        _selectedSourceMode = ParseSourceMode((SourceModeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString());
        await _dataService.SaveSelectedSourceModeAsync(_selectedSourceMode);
        _lastSnapshot = await _dataService.LoadAsync();
        ApplySourceModeToUi();
        RenderLists();
        RuntimeStatusText.Text = _selectedSourceMode switch
        {
            NativeSourceMode.Upload => _currentLanguage == "zh-CN" ? "已切换到本地上传模式，点击“上传照片”即可导入。" : "Switched to Upload mode. Click Upload Photos to import local images.",
            NativeSourceMode.Gallery => _currentLanguage == "zh-CN" ? "已切换到图库选图模式，可多选后导入。" : "Switched to Gallery mode. Multi-select gallery photos and import.",
            _ => RuntimeStatusText.Text
        };
    }

    private async void EffectPresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isBindingPresetList || EffectPresetComboBox.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        var presetId = item.Tag?.ToString();
        if (string.IsNullOrWhiteSpace(presetId))
        {
            return;
        }

        _selectedEffectPresetId = presetId;
        await _dataService.SaveSelectedEffectPresetAsync(presetId);
        _lastSnapshot = await _dataService.LoadAsync();
        RenderLists();
        await RefreshUploadedPreviewAsync();
    }

    private async void StickerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedStickerKind = ParseStickerKind((StickerComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString());
        await RefreshUploadedPreviewAsync();
    }

    private async void MaskModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedMaskMode = ParseMaskMode((MaskModeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString());
        await RefreshUploadedPreviewAsync();
    }

    private void AssignmentModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _pendingManualSlotIndex = null;
        SelectedSourcesText.Text = BuildSelectedSourcesText();
        ApplySourceModeToUi();
    }

    private async void FrameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isBindingFrameList)
        {
            return;
        }

        if (FrameComboBox.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        var frameId = item.Tag?.ToString();
        if (string.IsNullOrWhiteSpace(frameId))
        {
            return;
        }

        _selectedFrameId = frameId;
        await _dataService.SaveSelectedFrameAsync(frameId);
        _lastSnapshot = await _dataService.LoadAsync();
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
        ShotCountTextBox.Text = template.Slots.Count.ToString(CultureInfo.InvariantCulture);
        await _dataService.SaveSelectedTemplateAsync(template.Id);
        _lastSnapshot = await _dataService.LoadAsync();
        _selectedFrameId = _lastSnapshot.SelectedFrameId ?? _selectedFrameId;
        BindFrameSelection();
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

        _selectedEditablePhotoId = photo.Id;
        DisplayGalleryPhoto(photo.ProcessedFilePath);
        _ = SetUploadedPreviewAsync(photo.ProcessedFilePath);
        if (GetAssignmentMode() == "manual" && _pendingManualSlotIndex.HasValue)
        {
            _ = AssignSelectedPhotoToPendingSlotAsync(photo);
        }
    }

    private void TemplatePreviewImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (GetAssignmentMode() != "manual")
        {
            return;
        }

        var template = GetSelectedTemplate();
        if (template is null || TemplatePreviewImage.Source is not BitmapSource bitmap)
        {
            return;
        }

        var position = e.GetPosition(TemplatePreviewImage);
        if (position.X < 0 || position.Y < 0 || TemplatePreviewImage.ActualWidth <= 0 || TemplatePreviewImage.ActualHeight <= 0)
        {
            return;
        }

        var scale = Math.Min(TemplatePreviewImage.ActualWidth / bitmap.PixelWidth, TemplatePreviewImage.ActualHeight / bitmap.PixelHeight);
        var renderedWidth = bitmap.PixelWidth * scale;
        var renderedHeight = bitmap.PixelHeight * scale;
        var offsetX = (TemplatePreviewImage.ActualWidth - renderedWidth) / 2d;
        var offsetY = (TemplatePreviewImage.ActualHeight - renderedHeight) / 2d;
        var localX = position.X - offsetX;
        var localY = position.Y - offsetY;
        if (localX < 0 || localY < 0 || localX > renderedWidth || localY > renderedHeight)
        {
            return;
        }

        var normalizedX = localX / renderedWidth;
        var normalizedY = localY / renderedHeight;
        for (var index = 0; index < template.Slots.Count; index++)
        {
            var slot = template.Slots[index];
            var minX = slot.X / template.ExportWidth;
            var minY = slot.Y / template.ExportHeight;
            var maxX = (slot.X + slot.Width) / template.ExportWidth;
            var maxY = (slot.Y + slot.Height) / template.ExportHeight;
            if (normalizedX >= minX && normalizedX <= maxX && normalizedY >= minY && normalizedY <= maxY)
            {
                _pendingManualSlotIndex = index;
                RuntimeStatusText.Text = _currentLanguage == "zh-CN"
                    ? $"已选中第 {index + 1} 格，请在图库中点击要放入这格的照片。"
                    : $"Slot {index + 1} selected. Click a photo in the gallery to assign it.";
                break;
            }
        }
    }

    private async void UploadPhotosButton_Click(object sender, RoutedEventArgs e)
    {
        UploadPhotosButton.IsEnabled = false;
        RuntimeStatusText.Text = Localization.Get(_currentLanguage, "upload_opening_dialog");
        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.webp",
                Multiselect = true,
                Title = Localization.Get(_currentLanguage, "upload_photos"),
                CheckFileExists = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            var result = dialog.ShowDialog();
            if (result != true)
            {
                RuntimeStatusText.Text = Localization.Get(_currentLanguage, "source_import_ready");
                return;
            }

            var files = dialog.FileNames
                .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (files.Length == 0)
            {
                RuntimeStatusText.Text = Localization.Get(_currentLanguage, "upload_no_valid_files");
                return;
            }

            await ImportExternalPhotosAsync(files, NativePhotoSourceOrigin.Upload);
        }
        catch (Exception)
        {
            RuntimeStatusText.Text = Localization.Get(_currentLanguage, "upload_dialog_failed");
        }
        finally
        {
            UploadPhotosButton.IsEnabled = true;
        }
    }

    private async void UseGallerySelectionButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedPaths = GalleryList.SelectedItems
            .OfType<ListBoxItem>()
            .Select(item => item.Tag as NativePhotoRecord)
            .Where(photo => photo is not null)
            .Select(photo => photo!.ProcessedFilePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (selectedPaths.Count == 0)
        {
            RuntimeStatusText.Text = _currentLanguage == "zh-CN"
                ? "请先在图库里选中至少一张照片。"
                : "Select at least one photo from the gallery first.";
            return;
        }

        await ImportExternalPhotosAsync(selectedPaths, NativePhotoSourceOrigin.Gallery);
    }

    private async void EditablePhotoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isBindingEditablePhotoList || EditablePhotoComboBox.SelectedItem is not ComboBoxItem item || item.Tag is not NativePhotoRecord photo)
        {
            return;
        }

        _selectedEditablePhotoId = photo.Id;
        if (_isBindingSlotList)
        {
            return;
        }

        ApplyEditablePhotoToUi(photo);
        await RefreshUploadedPreviewFromPhotoAsync(photo);
    }

    private async void ReplaceEditablePhotoButton_Click(object sender, RoutedEventArgs e)
    {
        var targetPhoto = GetSelectedEditablePhoto();
        if (targetPhoto is null)
        {
            RuntimeStatusText.Text = _currentLanguage == "zh-CN" ? "请先选择一张要替换的照片。" : "Choose a photo to replace first.";
            return;
        }

        await ForceSaveCurrentPhotoTransformAsync();
        var dialog = new OpenFileDialog
        {
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.webp",
            Multiselect = false,
            Title = _currentLanguage == "zh-CN" ? "替换照片" : "Replace Photo",
            CheckFileExists = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
        };

        var result = dialog.ShowDialog();
        if (result != true || string.IsNullOrWhiteSpace(dialog.FileName) || !File.Exists(dialog.FileName))
        {
            return;
        }

        var replaced = await _dataService.ReplacePhotoSourceAsync(targetPhoto.Id, dialog.FileName);
        _lastSnapshot = await _dataService.LoadAsync();
        RenderLists();
        if (replaced is not null)
        {
            _selectedEditablePhotoId = replaced.Id;
            ApplyEditablePhotoToUi(replaced);
            await RefreshUploadedPreviewFromPhotoAsync(replaced);
            RuntimeStatusText.Text = _currentLanguage == "zh-CN" ? "已替换选中照片。" : "Selected photo replaced.";
        }
    }

    private async void DeleteEditablePhotoButton_Click(object sender, RoutedEventArgs e)
    {
        var targetPhoto = GetSelectedEditablePhoto();
        if (targetPhoto is null)
        {
            RuntimeStatusText.Text = _currentLanguage == "zh-CN" ? "请先选择一张要删除的照片。" : "Choose a photo to delete first.";
            return;
        }

        await ForceSaveCurrentPhotoTransformAsync();
        var deleted = await _dataService.RemovePhotoAsync(targetPhoto.Id);
        if (!deleted)
        {
            return;
        }

        _selectedEditablePhotoId = null;
        _lastSnapshot = await _dataService.LoadAsync();
        RenderLists();
        RuntimeStatusText.Text = _currentLanguage == "zh-CN" ? "已删除选中照片。" : "Selected photo deleted.";
        await RefreshUploadedPreviewAsync();
    }

    private async void AssignSlotComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isBindingSlotList || _isApplyingPhotoTransformControls)
        {
            return;
        }

        var photo = GetSelectedEditablePhoto();
        if (photo is null)
        {
            return;
        }

        if (AssignSlotComboBox.SelectedItem is not ComboBoxItem slotItem)
        {
            return;
        }

        var slotValue = slotItem.Tag?.ToString();
        if (!int.TryParse(slotValue, out var slotIndex))
        {
            return;
        }

        if (slotIndex == photo.SlotIndex)
        {
            return;
        }

        await ForceSaveCurrentPhotoTransformAsync();
        await _dataService.AssignPhotoToSlotAsync(photo.Id, slotIndex);
        _lastSnapshot = await _dataService.LoadAsync();
        RenderLists();
        RuntimeStatusText.Text = _currentLanguage == "zh-CN" ? "照片已移动到指定格子。" : "Photo moved to selected slot.";
    }

    private void MovePhotoPrevButton_Click(object sender, RoutedEventArgs e)
    {
        _ = MoveEditablePhotoByOffsetAsync(-1);
    }

    private void MovePhotoNextButton_Click(object sender, RoutedEventArgs e)
    {
        _ = MoveEditablePhotoByOffsetAsync(1);
    }

    private async void ResetPhotoTransformButton_Click(object sender, RoutedEventArgs e)
    {
        var photo = GetSelectedEditablePhoto();
        if (photo is null)
        {
            return;
        }

        var ok = await _dataService.UpdatePhotoTransformAsync(photo.Id, 1d, 0d, 0d, 0d);
        if (!ok)
        {
            return;
        }

        _lastSnapshot = await _dataService.LoadAsync();
        RenderLists();
        RuntimeStatusText.Text = _currentLanguage == "zh-CN" ? "已重置该照片的编辑参数。" : "Photo transform reset.";
    }

    private void PhotoTransformSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isApplyingPhotoTransformControls ||
            ScaleValueText is null ||
            RotationValueText is null ||
            OffsetXValueText is null ||
            OffsetYValueText is null ||
            PhotoScaleSlider is null ||
            PhotoRotationSlider is null ||
            PhotoOffsetXSlider is null ||
            PhotoOffsetYSlider is null)
        {
            return;
        }

        ScaleValueText.Text = FormatScaleText(PhotoScaleSlider.Value);
        RotationValueText.Text = FormatRotationText(PhotoRotationSlider.Value);
        OffsetXValueText.Text = FormatOffsetText(PhotoOffsetXSlider.Value);
        OffsetYValueText.Text = FormatOffsetText(PhotoOffsetYSlider.Value);

        _hasPendingTransformSave = true;
        _photoTransformSaveTimer.Stop();
        _photoTransformSaveTimer.Start();
    }

    private void ImportDropZone_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
            return;
        }

        e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    private async void ImportDropZone_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            RuntimeStatusText.Text = Localization.Get(_currentLanguage, "drop_no_valid_files");
            return;
        }

        var filePaths = (e.Data.GetData(DataFormats.FileDrop) as string[] ?? Array.Empty<string>())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Where(IsSupportedImageFile)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (filePaths.Length == 0)
        {
            RuntimeStatusText.Text = Localization.Get(_currentLanguage, "drop_no_valid_files");
            return;
        }

        RuntimeStatusText.Text = Localization.Get(_currentLanguage, "drop_import_started");
        await ImportExternalPhotosAsync(filePaths, NativePhotoSourceOrigin.Upload);
    }

    private async void RenderTemplateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRenderingTemplate)
        {
            return;
        }

        var session = await _dataService.GetOrCreateActiveSessionAsync(GetSelectedCaptureMode(), GetShotCount(), GetCountdownSeconds());
        _selectedSessionId = session.Id;
        var template = GetSelectedTemplate(session.Id);
        if (template is null)
        {
            RuntimeStatusText.Text = GetNoTemplateSelectedMessage();
            return;
        }

        var photos = GetCurrentSessionPhotos(session.Id);
        if (photos.Count == 0)
        {
            RuntimeStatusText.Text = GetNoTemplatePhotosMessage();
            return;
        }

        if (photos.Count < template.Slots.Count)
        {
            RuntimeStatusText.Text = _currentLanguage == "zh-CN"
                ? $"当前模板需要 {template.Slots.Count} 张照片，已拍 {photos.Count} 张，暂不能生成最终成片。"
                : $"This template requires {template.Slots.Count} photos. Only {photos.Count} captured, so the final composite is not ready yet.";
            return;
        }

        _isRenderingTemplate = true;
        RenderTemplateButton.IsEnabled = false;
        try
        {
            RuntimeStatusText.Text = GetTemplateRenderingMessage();
            var result = await RenderFinalCompositeAsync(session, template, photos);
            RuntimeStatusText.Text = $"{GetTemplateRenderedMessage()} {Path.GetFileName(result.PngPath)}";
            _printService.OpenFile(result.PngPath);
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

    private async void UploadToWebsiteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isUploadingToWebsite)
        {
            return;
        }

        var code = (WebsiteCodeTextBox.Text ?? string.Empty).Trim();
        if (!Regex.IsMatch(code, "^\\d{4}$"))
        {
            RuntimeStatusText.Text = Localization.Get(_currentLanguage, "website_upload_need_code");
            return;
        }

        var visibility = GetSelectedWebsiteVisibility();
        var privatePassword = (WebsitePrivatePasswordTextBox.Text ?? string.Empty).Trim();
        if (string.Equals(visibility, "private", StringComparison.OrdinalIgnoreCase) && privatePassword.Length == 0)
        {
            privatePassword = $"{RandomNumberGenerator.GetInt32(0, 10000)}".PadLeft(4, '0');
            WebsitePrivatePasswordTextBox.Text = privatePassword;
        }
        if (string.Equals(visibility, "private", StringComparison.OrdinalIgnoreCase) &&
            privatePassword.Length > 0 &&
            !Regex.IsMatch(privatePassword, "^\\d{4}$"))
        {
            RuntimeStatusText.Text = Localization.Get(_currentLanguage, "website_upload_need_password");
            return;
        }

        var targetImagePath = GetUploadCandidateImagePath();
        if (string.IsNullOrWhiteSpace(targetImagePath) || !File.Exists(targetImagePath))
        {
            RuntimeStatusText.Text = Localization.Get(_currentLanguage, "website_upload_need_image");
            return;
        }

        var baseUrl = (WebsiteBaseUrlTextBox.Text ?? "http://localhost:3000").Trim();
        var eventName = (WebsiteEventNameTextBox.Text ?? string.Empty).Trim();
        var layoutFormat = GetSelectedWebsiteLayoutFormat();
        _isUploadingToWebsite = true;
        UploadToWebsiteButton.IsEnabled = false;
        RuntimeStatusText.Text = Localization.Get(_currentLanguage, "website_upload_starting");

        try
        {
            var result = await _websiteUploadService.UploadAsync(
                baseUrl,
                targetImagePath,
                code,
                eventName,
                visibility,
                layoutFormat,
                privatePassword);

            if (!result.Ok)
            {
                RuntimeStatusText.Text = $"{Localization.Get(_currentLanguage, "website_upload_failed")} {result.Message}";
                return;
            }

            if (string.Equals(result.Visibility, "private", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(result.PrivatePassword))
            {
                WebsitePrivatePasswordTextBox.Text = result.PrivatePassword;
            }

            var accessPath = string.IsNullOrWhiteSpace(result.AccessUrl) ? $"/gallery/{result.Code}" : result.AccessUrl;
            RuntimeStatusText.Text = string.Equals(result.Visibility, "private", StringComparison.OrdinalIgnoreCase)
                ? $"{Localization.Get(_currentLanguage, "website_upload_success")} {result.Code} · {accessPath} · PIN {result.PrivatePassword ?? "****"}"
                : $"{Localization.Get(_currentLanguage, "website_upload_success")} {result.Code} · {accessPath}";
        }
        finally
        {
            _isUploadingToWebsite = false;
            UploadToWebsiteButton.IsEnabled = true;
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
            _lastSnapshot = await _dataService.LoadAsync();
            _selectedTemplateId = _lastSnapshot.SelectedTemplateId ?? _selectedTemplateId;
            _selectedFrameId = _lastSnapshot.SelectedFrameId ?? _selectedFrameId;
            _selectedBeautyLevel = ParseBeautyLevel(_lastSnapshot.SelectedBeautyLevel);
            _lastRuntime = await _cameraDetectionService.RefreshAsync();
            await LoadCameraParametersAsync(_lastRuntime);
            ApplyRuntimeToUi(_lastRuntime);
            ApplyLanguage();
            BindFrameSelection();
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
            ToggleOrientationButton.Content = GetOrientationButtonText();
            LaunchDigiCamControlButton.Content = Localization.Get(_currentLanguage, "launch_digicamcontrol");
            StartLiveButton.Content = Localization.Get(_currentLanguage, "start_live_manual");
            CaptureNowButton.Content = Localization.Get(_currentLanguage, "capture_now");
            AutoFocusButton.Content = Localization.Get(_currentLanguage, "auto_focus");
            OpenSessionFolderButton.Content = Localization.Get(_currentLanguage, "open_session_folder");
            OpenFinalButton.Content = Localization.Get(_currentLanguage, "open_final");
            PrintFinalButton.Content = Localization.Get(_currentLanguage, "print_final");
            UploadToWebsiteButton.Content = Localization.Get(_currentLanguage, "upload_to_website");
            RestartSessionButton.Content = Localization.Get(_currentLanguage, "restart_session");
            LanguageLabelText.Text = Localization.Get(_currentLanguage, "language");
            ((ComboBoxItem)LanguageCombo.Items[0]).Content = Localization.Get(_currentLanguage, "language_chinese");
            ((ComboBoxItem)LanguageCombo.Items[1]).Content = Localization.Get(_currentLanguage, "language_english");
            StageTitleText.Text = Localization.Get(_currentLanguage, "stage_title");
            StageSubtitleText.Text = Localization.Get(_currentLanguage, "stage_subtitle");
            UploadStageButton.Content = Localization.Get(_currentLanguage, "stage_upload_photo");
            FullscreenPreviewButton.Content = Localization.Get(_currentLanguage, "stage_fullscreen");
            ClearPreviewButton.Content = Localization.Get(_currentLanguage, "stage_clear_preview");
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
            SourceModeLabelText.Text = Localization.Get(_currentLanguage, "source_mode");
            ShotCountLabelText.Text = Localization.Get(_currentLanguage, "shot_count");
            CountdownLabelText.Text = Localization.Get(_currentLanguage, "countdown_seconds");
            BeautyLevelLabelText.Text = Localization.Get(_currentLanguage, "beauty_level");
            FrameLabelText.Text = Localization.Get(_currentLanguage, "frame_overlay");
            EffectPresetLabelText.Text = Localization.Get(_currentLanguage, "effect_preset");
            StickerLabelText.Text = Localization.Get(_currentLanguage, "preview_sticker");
            MaskModeLabelText.Text = Localization.Get(_currentLanguage, "preview_mask_mode");
            AssignmentModeLabelText.Text = Localization.Get(_currentLanguage, "assignment_mode");
            SelectedSourcesLabelText.Text = Localization.Get(_currentLanguage, "selected_sources");
            SessionProgressLabelText.Text = Localization.Get(_currentLanguage, "session_progress");
            ImportModeTitleText.Text = Localization.Get(_currentLanguage, "import_mode_title");
            ImportModeHintText.Text = Localization.Get(_currentLanguage, "import_mode_hint");
            ImportDropHintText.Text = Localization.Get(_currentLanguage, "import_drop_hint");
            CameraParametersTitleText.Text = Localization.Get(_currentLanguage, "camera_parameters");
            WebsiteUploadTitleText.Text = Localization.Get(_currentLanguage, "website_upload_title");
            WebsiteUploadHintText.Text = Localization.Get(_currentLanguage, "website_upload_hint");
            WebsiteBaseUrlLabelText.Text = Localization.Get(_currentLanguage, "website_base_url");
            WebsiteCodeLabelText.Text = Localization.Get(_currentLanguage, "website_code");
            WebsiteEventNameLabelText.Text = Localization.Get(_currentLanguage, "website_event_name");
            WebsiteFormatLabelText.Text = Localization.Get(_currentLanguage, "website_format");
            WebsiteVisibilityLabelText.Text = Localization.Get(_currentLanguage, "website_visibility");
            WebsitePrivatePasswordLabelText.Text = Localization.Get(_currentLanguage, "website_private_password");
            ActiveSessionLabelText.Text = Localization.Get(_currentLanguage, "active_session");
            PhotoEditorTitleText.Text = Localization.Get(_currentLanguage, "photo_editor_title");
            PhotoEditorHintText.Text = Localization.Get(_currentLanguage, "photo_editor_hint");
            EditablePhotoLabelText.Text = Localization.Get(_currentLanguage, "editable_photo");
            ReplaceEditablePhotoButton.Content = Localization.Get(_currentLanguage, "replace_photo");
            DeleteEditablePhotoButton.Content = Localization.Get(_currentLanguage, "delete_photo");
            AssignSlotLabelText.Text = Localization.Get(_currentLanguage, "assign_slot");
            MovePhotoPrevButton.Content = Localization.Get(_currentLanguage, "move_prev_slot");
            MovePhotoNextButton.Content = Localization.Get(_currentLanguage, "move_next_slot");
            ResetPhotoTransformButton.Content = Localization.Get(_currentLanguage, "reset_photo_edit");
            ScaleLabelText.Text = Localization.Get(_currentLanguage, "scale_label");
            RotationLabelText.Text = Localization.Get(_currentLanguage, "rotation_label");
            OffsetXLabelText.Text = Localization.Get(_currentLanguage, "offset_x_label");
            OffsetYLabelText.Text = Localization.Get(_currentLanguage, "offset_y_label");
            IsoLabelText.Text = Localization.Get(_currentLanguage, "iso");
            ShutterLabelText.Text = Localization.Get(_currentLanguage, "shutter_speed");
            ApertureLabelText.Text = Localization.Get(_currentLanguage, "aperture");
            WhiteBalanceLabelText.Text = Localization.Get(_currentLanguage, "white_balance");
            ExposureCompLabelText.Text = Localization.Get(_currentLanguage, "exposure_comp");
            DetectedDevicesTitleText.Text = Localization.Get(_currentLanguage, "detected_devices");

            ((ComboBoxItem)CaptureModeCombo.Items[0]).Content = Localization.Get(_currentLanguage, "mode_single");
            ((ComboBoxItem)CaptureModeCombo.Items[1]).Content = Localization.Get(_currentLanguage, "mode_multi");
            ((ComboBoxItem)CaptureModeCombo.Items[2]).Content = Localization.Get(_currentLanguage, "mode_burst");
            ((ComboBoxItem)SourceModeComboBox.Items[0]).Content = Localization.Get(_currentLanguage, "source_mode_camera");
            ((ComboBoxItem)SourceModeComboBox.Items[1]).Content = Localization.Get(_currentLanguage, "source_mode_upload");
            ((ComboBoxItem)SourceModeComboBox.Items[2]).Content = Localization.Get(_currentLanguage, "source_mode_gallery");
            ((ComboBoxItem)BeautyLevelComboBox.Items[0]).Content = Localization.Get(_currentLanguage, "beauty_off");
            ((ComboBoxItem)BeautyLevelComboBox.Items[1]).Content = Localization.Get(_currentLanguage, "beauty_low");
            ((ComboBoxItem)BeautyLevelComboBox.Items[2]).Content = Localization.Get(_currentLanguage, "beauty_medium");
            ((ComboBoxItem)BeautyLevelComboBox.Items[3]).Content = Localization.Get(_currentLanguage, "beauty_high");
            ((ComboBoxItem)AssignmentModeComboBox.Items[0]).Content = Localization.Get(_currentLanguage, "assignment_auto");
            ((ComboBoxItem)AssignmentModeComboBox.Items[1]).Content = Localization.Get(_currentLanguage, "assignment_manual");
            ((ComboBoxItem)StickerComboBox.Items[0]).Content = Localization.Get(_currentLanguage, "sticker_none");
            ((ComboBoxItem)StickerComboBox.Items[1]).Content = Localization.Get(_currentLanguage, "sticker_dog_ears");
            ((ComboBoxItem)StickerComboBox.Items[2]).Content = Localization.Get(_currentLanguage, "sticker_party_hat");
            ((ComboBoxItem)StickerComboBox.Items[3]).Content = Localization.Get(_currentLanguage, "sticker_hearts");
            ((ComboBoxItem)MaskModeComboBox.Items[0]).Content = Localization.Get(_currentLanguage, "mask_none");
            ((ComboBoxItem)MaskModeComboBox.Items[1]).Content = Localization.Get(_currentLanguage, "mask_left_half");
            ((ComboBoxItem)MaskModeComboBox.Items[2]).Content = Localization.Get(_currentLanguage, "mask_right_half");
            ((ComboBoxItem)MaskModeComboBox.Items[3]).Content = Localization.Get(_currentLanguage, "mask_center_spotlight");
            ((ComboBoxItem)WebsiteVisibilityComboBox.Items[0]).Content = Localization.Get(_currentLanguage, "website_public");
            ((ComboBoxItem)WebsiteVisibilityComboBox.Items[1]).Content = Localization.Get(_currentLanguage, "website_private");
            UploadPhotosButton.Content = Localization.Get(_currentLanguage, "upload_photos");
            UseGallerySelectionButton.Content = Localization.Get(_currentLanguage, "use_gallery_selection");
            SelectSlotHintText.Text = Localization.Get(_currentLanguage, "select_slot_hint");
            ScaleValueText.Text = FormatScaleText(PhotoScaleSlider.Value);
            RotationValueText.Text = FormatRotationText(PhotoRotationSlider.Value);
            OffsetXValueText.Text = FormatOffsetText(PhotoOffsetXSlider.Value);
            OffsetYValueText.Text = FormatOffsetText(PhotoOffsetYSlider.Value);

            var activeSession = GetSelectedSession();
            var currentTemplate = GetSelectedTemplate(activeSession?.Id);
            if (currentTemplate is not null)
            {
                ShotCountTextBox.Text = currentTemplate.Slots.Count.ToString(CultureInfo.InvariantCulture);
            }

            SelectedTemplateText.Text = BuildSelectedTemplateText(currentTemplate);
            TemplateProgressText.Text = BuildTemplateProgressText(currentTemplate, GetCurrentSessionPhotos(activeSession?.Id));
            SelectedSourcesText.Text = BuildSelectedSourcesText();
            UpdateSessionProgress(activeSession);
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

        BindFrameSelection();
        BindEffectPresets();
        RenderGallery();
        BindEditablePhotoControls(GetCurrentSessionPhotos());
        SelectedTemplateText.Text = BuildSelectedTemplateText(GetSelectedTemplate());
        TemplateProgressText.Text = BuildTemplateProgressText(GetSelectedTemplate(), GetCurrentSessionPhotos());
        SelectedSourcesText.Text = BuildSelectedSourcesText();
        UpdateTemplateWorkspacePreview();
        UpdateActiveSessionText(GetSelectedSession());
        UpdateSessionProgress(GetSelectedSession());
        ApplySourceModeToUi();
    }

    private void BindBeautyLevels()
    {
        foreach (var item in BeautyLevelComboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Tag?.ToString(), _selectedBeautyLevel.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                BeautyLevelComboBox.SelectedItem = item;
                break;
            }
        }
    }

    private void BindPreviewControls()
    {
        StickerComboBox.SelectedIndex = 0;
        MaskModeComboBox.SelectedIndex = 0;
    }

    private void BindWebsiteUploadControls()
    {
        if (WebsiteFormatComboBox.Items.Count > 0)
        {
            WebsiteFormatComboBox.SelectedIndex = 0;
        }
        if (WebsiteVisibilityComboBox.Items.Count > 0)
        {
            WebsiteVisibilityComboBox.SelectedIndex = 1;
        }
        WebsitePrivatePasswordTextBox.IsEnabled = true;
    }

    private void BindFrameSelection()
    {
        if (_lastSnapshot is null)
        {
            return;
        }

        _isBindingFrameList = true;
        try
        {
            FrameComboBox.Items.Clear();
            foreach (var frame in _lastSnapshot.Frames)
            {
                var item = new ComboBoxItem
                {
                    Content = frame.Name,
                    Tag = frame.Id
                };
                FrameComboBox.Items.Add(item);
                if (frame.Id == (_selectedFrameId ?? _lastSnapshot.SelectedFrameId))
                {
                    FrameComboBox.SelectedItem = item;
                }
            }

            if (FrameComboBox.SelectedIndex < 0 && FrameComboBox.Items.Count > 0)
            {
                FrameComboBox.SelectedIndex = 0;
            }
        }
        finally
        {
            _isBindingFrameList = false;
        }
    }

    private void BindEffectPresets()
    {
        if (_lastSnapshot is null)
        {
            return;
        }

        _isBindingPresetList = true;
        try
        {
            EffectPresetComboBox.Items.Clear();
            foreach (var preset in _lastSnapshot.EffectPresets)
            {
                var item = new ComboBoxItem
                {
                    Content = Localization.GetEffectPresetName(_currentLanguage, preset),
                    Tag = preset.Id
                };
                EffectPresetComboBox.Items.Add(item);
                if (preset.Id == (_selectedEffectPresetId ?? _lastSnapshot.SelectedEffectPresetId))
                {
                    EffectPresetComboBox.SelectedItem = item;
                }
            }

            if (EffectPresetComboBox.SelectedIndex < 0 && EffectPresetComboBox.Items.Count > 0)
            {
                EffectPresetComboBox.SelectedIndex = 0;
            }
        }
        finally
        {
            _isBindingPresetList = false;
        }
    }

    private void BindSourceMode()
    {
        foreach (var item in SourceModeComboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Tag?.ToString(), _selectedSourceMode.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                SourceModeComboBox.SelectedItem = item;
                break;
            }
        }
    }

    private void BindAssignmentMode()
    {
        AssignmentModeComboBox.SelectedIndex = 0;
    }

    private void ApplySourceModeToUi()
    {
        var isCameraMode = _selectedSourceMode == NativeSourceMode.Camera;
        LaunchDigiCamControlButton.IsEnabled = isCameraMode;
        StartLiveButton.IsEnabled = isCameraMode;
        CaptureNowButton.IsEnabled = isCameraMode && !_isCapturing;
        AutoFocusButton.IsEnabled = isCameraMode;
        UploadPhotosButton.IsEnabled = true;
        UploadStageButton.IsEnabled = true;
        FullscreenPreviewButton.IsEnabled = true;
        ClearPreviewButton.IsEnabled = true;
        UseGallerySelectionButton.IsEnabled = true;
        UploadToWebsiteButton.IsEnabled = !_isUploadingToWebsite;
        SelectSlotHintText.Visibility = GetAssignmentMode() == "manual" ? Visibility.Visible : Visibility.Collapsed;
        ReplaceEditablePhotoButton.IsEnabled = GetSelectedEditablePhoto() is not null;
        DeleteEditablePhotoButton.IsEnabled = GetSelectedEditablePhoto() is not null;
    }

    private void RenderGallery()
    {
        GalleryList.Items.Clear();
        IEnumerable<NativePhotoRecord> photoSource = _selectedSourceMode == NativeSourceMode.Gallery
            ? (_lastSnapshot?.GalleryPhotos ?? Enumerable.Empty<NativePhotoRecord>())
            : (_lastSnapshot?.GalleryPhotos.Where(photo => string.IsNullOrWhiteSpace(_selectedSessionId ?? _lastSnapshot?.ActiveSessionId) || photo.SessionId == (_selectedSessionId ?? _lastSnapshot?.ActiveSessionId)) ?? Enumerable.Empty<NativePhotoRecord>());
        var photos = photoSource
            .OrderByDescending(photo => photo.SlotIndex)
            .ThenByDescending(photo => photo.CapturedAt)
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
            BindEditablePhotoControls([]);
        }
        else
        {
            SelectGalleryPhotoByPath(photos[0].ProcessedFilePath);
            BindEditablePhotoControls(photos);
        }
    }

    private void SelectGalleryPhotoByPath(string path)
    {
        foreach (var item in GalleryList.Items.OfType<ListBoxItem>())
        {
            if (item.Tag is NativePhotoRecord photo && string.Equals(photo.ProcessedFilePath, path, StringComparison.OrdinalIgnoreCase))
            {
                GalleryList.SelectedItem = item;
                GalleryList.ScrollIntoView(item);
                DisplayGalleryPhoto(photo.ProcessedFilePath);
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

    private async void FullscreenPreviewButton_Click(object sender, RoutedEventArgs e)
    {
        if (LivePreviewImage.Source is not BitmapSource bitmap)
        {
            RuntimeStatusText.Text = _currentLanguage == "zh-CN" ? "当前没有可全屏查看的测试图像。" : "There is no preview image to show fullscreen.";
            return;
        }

        _fullscreenPreviewWindow ??= new FullscreenPreviewWindow();
        _fullscreenPreviewWindow.SetPreview(bitmap, Localization.Get(_currentLanguage, "fullscreen_preview_title"));
        _fullscreenPreviewWindow.Show();
        _fullscreenPreviewWindow.Activate();
        await Task.CompletedTask;
    }

    private void ClearPreviewButton_Click(object sender, RoutedEventArgs e)
    {
        _uploadedPreviewPath = null;
        _isApplyingUploadedPreview = false;
        RuntimeStatusText.Text = Localization.Get(_currentLanguage, "preview_test_cleared");
        if (_selectedSourceMode != NativeSourceMode.Camera)
        {
            ShowPreviewPlaceholder();
            return;
        }

        _ = RefreshUiAsync(RuntimeStatusText.Text);
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
        if (_isApplyingUploadedPreview && !string.IsNullOrWhiteSpace(_uploadedPreviewPath))
        {
            await RefreshUploadedPreviewAsync();
            return;
        }

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
            UpdateLiveGuideOverlay();
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
        LiveGuideOverlayImage.Visibility = Visibility.Collapsed;
        LivePreviewPlaceholder.Visibility = Visibility.Visible;
        LivePreviewImage.Source = null;
        LiveGuideOverlayImage.Source = null;
    }

    private void ApplyWindowOrientation()
    {
        var portrait = string.Equals(_windowOrientation, "Portrait", StringComparison.OrdinalIgnoreCase);
        if (portrait)
        {
            Width = 1280;
            Height = 900;
            MinWidth = 1100;
            MinHeight = 720;

            MainColumnLeft.Width = new GridLength(2.2d, GridUnitType.Star);
            MainColumnRight.Width = new GridLength(1.05d, GridUnitType.Star);
            MainControlsRow.Height = new GridLength(0d, GridUnitType.Pixel);

            Grid.SetColumn(MainStagePane, 0);
            Grid.SetRow(MainStagePane, 1);
            MainStagePane.Margin = new Thickness(0, 0, 18, 0);

            Grid.SetColumn(MainControlsPane, 1);
            Grid.SetRow(MainControlsPane, 1);
            MainControlsPane.Margin = new Thickness(0);
        }
        else
        {
            Width = 1540;
            Height = 920;
            MinWidth = 1280;
            MinHeight = 760;

            MainColumnLeft.Width = new GridLength(2.8d, GridUnitType.Star);
            MainColumnRight.Width = new GridLength(0.95d, GridUnitType.Star);
            MainControlsRow.Height = new GridLength(0d, GridUnitType.Pixel);

            Grid.SetColumn(MainStagePane, 0);
            Grid.SetRow(MainStagePane, 1);
            MainStagePane.Margin = new Thickness(0, 0, 18, 0);

            Grid.SetColumn(MainControlsPane, 1);
            Grid.SetRow(MainControlsPane, 1);
            MainControlsPane.Margin = new Thickness(0);
        }

        ToggleOrientationButton.Content = GetOrientationButtonText();
        RuntimeStatusText.Text = _currentLanguage == "zh-CN"
            ? (portrait ? "已切换到竖屏布局。" : "已切换到横屏布局。")
            : (portrait ? "Switched to portrait layout." : "Switched to landscape layout.");
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

        var aspectRatio = _currentPreviewAspectRatio <= 0 ? 9d / 16d : _currentPreviewAspectRatio;
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

    private int GetShotCount() => int.TryParse(ShotCountTextBox.Text, out var shotCount) && shotCount > 0 ? shotCount : GetSelectedTemplate()?.Slots.Count ?? 4;

    private int GetCountdownSeconds() => int.TryParse(CountdownTextBox.Text, out var countdownSeconds) && countdownSeconds >= 0 ? countdownSeconds : 3;

    private NativeSessionRecord? GetSelectedSession()
    {
        var sessionId = _selectedSessionId ?? _lastSnapshot?.ActiveSessionId;
        return _lastSnapshot?.RecentSessions.FirstOrDefault(session => session.Id == sessionId) ?? _lastSnapshot?.RecentSessions.FirstOrDefault();
    }

    private void UpdateActiveSessionText(NativeSessionRecord? session)
    {
        ActiveSessionText.Text = session is null
            ? "-"
            : $"{session.Id}\n{session.FolderPath}\nTemplate: {session.TemplateName}\nBeauty: {session.SelectedBeautyLevel}";
    }

    private void UpdateSessionProgress(NativeSessionRecord? session)
    {
        if (session is null)
        {
            SessionProgressText.Text = _currentLanguage == "zh-CN" ? "尚未开始 Session" : "No active session yet.";
            return;
        }

        SessionProgressText.Text = _currentLanguage == "zh-CN"
            ? $"已拍 {session.CurrentShotIndex} / {session.RequiredShotCount} · 状态 {session.Status}"
            : $"Captured {session.CurrentShotIndex} / {session.RequiredShotCount} · Status {session.Status}";
    }
    private static ListBoxItem BuildSessionItem(NativeSessionRecord session, string languageCode, bool selected)
    {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = session.Id, FontWeight = FontWeights.SemiBold, FontSize = 14 });
        panel.Children.Add(new TextBlock
        {
            Text = $"{Localization.GetCaptureMode(languageCode, session.CaptureMode)} - {session.CurrentShotIndex}/{session.RequiredShotCount} - {session.CountdownSeconds}s",
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
            if (File.Exists(photo.ProcessedFilePath))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 160;
                bitmap.UriSource = new Uri(photo.ProcessedFilePath, UriKind.Absolute);
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
        textPanel.Children.Add(new TextBlock { Text = $"Slot {photo.SlotIndex + 1}: {photo.FileName}", FontWeight = FontWeights.SemiBold, FontSize = 13, TextWrapping = TextWrapping.Wrap });
        textPanel.Children.Add(new TextBlock
        {
            Text = photo.CapturedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            Foreground = Brushes.Gray,
            FontSize = 11,
            Margin = new Thickness(0, 4, 0, 0)
        });
        textPanel.Children.Add(new TextBlock
        {
            Text = $"Beauty: {photo.AppliedBeautyLevel}",
            Foreground = Brushes.DarkGray,
            FontSize = 11,
            Margin = new Thickness(0, 2, 0, 0)
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
            Text = $"{template.PaperSize} · {template.Slots.Count} shots · {Localization.GetTemplateDescription(_currentLanguage, template)}",
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

    private static ListBoxItem BuildDeviceRow(NativeDeviceRecord device, string languageCode)
    {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = Localization.GetDeviceName(languageCode, device),
            FontWeight = FontWeights.SemiBold,
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"{Localization.Get(languageCode, "connection_label")}: {Localization.GetConnectionState(languageCode, device)}  |  {Localization.GetDeviceTransport(languageCode, device)}",
            Foreground = Brushes.Gray,
            FontSize = 12,
            Margin = new Thickness(0, 4, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"{Localization.Get(languageCode, "trigger_label")}: {Localization.Get(languageCode, device.RemoteTriggerSupported ? "yes" : "no")}  |  " +
                   $"{Localization.Get(languageCode, "transfer_label")}: {Localization.Get(languageCode, device.TransferSupported ? "yes" : "no")}  |  " +
                   $"{Localization.Get(languageCode, "liveview_label")}: {Localization.Get(languageCode, device.LiveViewSupported ? "yes" : "no")}",
            Foreground = Brushes.DarkGray,
            FontSize = 11,
            Margin = new Thickness(0, 4, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Text = Localization.GetDeviceDiagnostics(languageCode, device),
            Foreground = Brushes.DarkGray,
            FontSize = 11,
            Margin = new Thickness(0, 6, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });

        return new ListBoxItem
        {
            Tag = device,
            Background = (Brush)Application.Current.Resources["PanelSoftBrush"],
            BorderBrush = (Brush)Application.Current.Resources["BorderBrushSoft"],
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 10),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Content = panel
        };
    }

    private NativeTemplateRecord? GetSelectedTemplate(string? sessionId = null)
    {
        var session = !string.IsNullOrWhiteSpace(sessionId)
            ? _lastSnapshot?.RecentSessions.FirstOrDefault(entry => entry.Id == sessionId)
            : GetSelectedSession();
        var templateId = !string.IsNullOrWhiteSpace(session?.TemplateId)
            ? session.TemplateId
            : (_selectedTemplateId ?? _lastSnapshot?.SelectedTemplateId);

        return _lastSnapshot?.Templates.FirstOrDefault(template => template.Id == templateId)
            ?? _lastSnapshot?.Templates.FirstOrDefault();
    }

    private NativeFrameRecord? GetSelectedFrame(string? sessionId = null)
    {
        var session = !string.IsNullOrWhiteSpace(sessionId)
            ? _lastSnapshot?.RecentSessions.FirstOrDefault(entry => entry.Id == sessionId)
            : GetSelectedSession();
        var frameId = !string.IsNullOrWhiteSpace(session?.SelectedFrameId)
            ? session.SelectedFrameId
            : (_selectedFrameId ?? _lastSnapshot?.SelectedFrameId);

        return _lastSnapshot?.Frames.FirstOrDefault(frame => frame.Id == frameId)
            ?? _lastSnapshot?.Frames.FirstOrDefault();
    }

    private NativeEffectPresetRecord? GetSelectedEffectPreset()
    {
        return _lastSnapshot?.EffectPresets.FirstOrDefault(preset => preset.Id == (_selectedEffectPresetId ?? _lastSnapshot?.SelectedEffectPresetId))
            ?? _lastSnapshot?.EffectPresets.FirstOrDefault();
    }

    private string BuildSelectedTemplateText(NativeTemplateRecord? template)
    {
        if (template is null)
        {
            return _currentLanguage == "zh-CN" ? "当前模板：未选择" : "Active template: none selected";
        }

        var frame = GetSelectedFrame(GetSelectedSession()?.Id);
        var preset = GetSelectedEffectPreset();
        var styleName = preset is null ? "-" : Localization.GetEffectPresetName(_currentLanguage, preset);
        return _currentLanguage == "zh-CN"
            ? $"当前模板：{Localization.GetTemplateName(_currentLanguage, template)} · {template.PaperSize} · 边框 {frame?.Name ?? "-"} · 风格 {styleName}"
            : $"Active template: {Localization.GetTemplateName(_currentLanguage, template)} · {template.PaperSize} · Frame {frame?.Name ?? "-"} · Style {styleName}";
    }

    private string GetRenderTemplateButtonLabel() => _currentLanguage == "zh-CN" ? "生成模板成片" : "Render Template";

    private string GetTemplateActionLabel() => _currentLanguage == "zh-CN" ? "已选择模板" : "Template selected";

    private string GetNoTemplateSelectedMessage() => _currentLanguage == "zh-CN" ? "请先选择一个模板。" : "Choose a template first.";

    private string GetNoTemplatePhotosMessage() => _currentLanguage == "zh-CN" ? "当前 Session 还没有照片，无法生成模板成片。" : "This session has no photos yet, so the template cannot be rendered.";

    private string GetTemplateRenderingMessage() => _currentLanguage == "zh-CN" ? "正在生成模板成片..." : "Rendering template output...";

    private string GetTemplateRenderedMessage() => _currentLanguage == "zh-CN" ? "模板成片已生成：" : "Template output rendered:";

    private string GetTemplateRenderFailedMessage() => _currentLanguage == "zh-CN" ? "模板成片生成失败。" : "Template rendering failed.";

    private string GetTemplatePreviewEmptyMessage() => _currentLanguage == "zh-CN"
        ? "这里会显示模板拼贴预览。每拍一张，就自动填入下一个空槽位。"
        : "The collage preview appears here. Each new shot fills the next slot.";

    private string GetSelectedWebsiteVisibility()
    {
        return ((WebsiteVisibilityComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "private").Trim().ToLowerInvariant();
    }

    private string GetSelectedWebsiteLayoutFormat()
    {
        var fromCombo = ((WebsiteFormatComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "original").Trim();
        if (!string.Equals(fromCombo, "original", StringComparison.OrdinalIgnoreCase))
        {
            return fromCombo;
        }

        var template = GetSelectedTemplate();
        return template?.Id switch
        {
            "strip-1x4" => "strip-1x4",
            "grid-4x6-2x3" => "grid-2x3",
            "grid-4x6-2x4" => "grid-2x4",
            _ => "original"
        };
    }

    private string? GetUploadCandidateImagePath()
    {
        var session = GetSelectedSession();
        if (session is not null)
        {
            if (!string.IsNullOrWhiteSpace(session.FinalPngPath) && File.Exists(session.FinalPngPath))
            {
                return session.FinalPngPath;
            }
            if (!string.IsNullOrWhiteSpace(session.FinalJpgPath) && File.Exists(session.FinalJpgPath))
            {
                return session.FinalJpgPath;
            }
        }

        if (!string.IsNullOrWhiteSpace(_uploadedPreviewPath) && File.Exists(_uploadedPreviewPath))
        {
            return _uploadedPreviewPath;
        }

        var current = GetCurrentSessionPhotos(session?.Id)
            .OrderByDescending(photo => photo.CapturedAt)
            .FirstOrDefault();
        if (current is not null && File.Exists(current.ProcessedFilePath))
        {
            return current.ProcessedFilePath;
        }

        return null;
    }

    private void WebsiteVisibilityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var isPrivate = string.Equals(GetSelectedWebsiteVisibility(), "private", StringComparison.OrdinalIgnoreCase);
        WebsitePrivatePasswordTextBox.IsEnabled = isPrivate;
        if (!isPrivate)
        {
            WebsitePrivatePasswordTextBox.Text = string.Empty;
        }
    }

    private string GetOrientationButtonText()
    {
        var portrait = string.Equals(_windowOrientation, "Portrait", StringComparison.OrdinalIgnoreCase);
        if (_currentLanguage == "zh-CN")
        {
            return portrait ? "切到横屏" : "切到竖屏";
        }

        return portrait ? "Switch Landscape" : "Switch Portrait";
    }
    private void MoveTemplateSelection(int offset)
    {
        if (_lastSnapshot is null || _lastSnapshot.Templates.Count == 0)
        {
            return;
        }

        var templates = _lastSnapshot.Templates;
        var currentIndex = templates.FindIndex(template => template.Id == (_selectedTemplateId ?? _lastSnapshot.SelectedTemplateId));
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        var nextIndex = (currentIndex + offset + templates.Count) % templates.Count;
        var nextTemplate = templates[nextIndex];
        _selectedTemplateId = nextTemplate.Id;
        ShotCountTextBox.Text = nextTemplate.Slots.Count.ToString(CultureInfo.InvariantCulture);

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

        var slotCount = template.Slots.Count;
        var filledCount = Math.Min(photos.Count, slotCount);
        return _currentLanguage == "zh-CN"
            ? $"模板进度：已填 {filledCount}/{slotCount} 格"
            : $"Template progress: {filledCount}/{slotCount} slots filled";
    }

    private async Task<NativeRenderResult> RenderFinalCompositeAsync(NativeSessionRecord session, NativeTemplateRecord template, List<NativePhotoRecord> photos)
    {
        await Task.Yield();
        var orderedPhotos = photos.OrderBy(photo => photo.SlotIndex).ThenBy(photo => photo.CapturedAt).ToList();
        var renderResult = _compositeRenderer.RenderFinal(template, orderedPhotos, GetSelectedFrame(session.Id), session, GetSelectedEffectPreset());
        await _dataService.MarkSessionCompletedAsync(session.Id, renderResult.PngPath, renderResult.JpgPath);
        _lastSnapshot = await _dataService.LoadAsync();
        RenderLists();
        return renderResult;
    }

    private void UpdateTemplateWorkspacePreview()
    {
        var session = GetSelectedSession();
        var template = GetSelectedTemplate(session?.Id);
        if (template is null)
        {
            TemplatePreviewImage.Source = null;
            TemplatePreviewImage.Visibility = Visibility.Collapsed;
            TemplatePreviewEmptyText.Visibility = Visibility.Visible;
            LiveGuideOverlayImage.Source = null;
            LiveGuideOverlayImage.Visibility = Visibility.Collapsed;
            return;
        }

        var orderedPhotos = GetCurrentSessionPhotos(session?.Id).OrderBy(photo => photo.SlotIndex).ThenBy(photo => photo.CapturedAt).ToList();
        var preview = _compositeRenderer.RenderPreview(template, orderedPhotos, GetSelectedFrame(session?.Id), session, GetSelectedEffectPreset(), 900, 600);
        TemplatePreviewImage.Source = preview;
        TemplatePreviewImage.Visibility = Visibility.Visible;
        TemplatePreviewEmptyText.Visibility = Visibility.Collapsed;
        UpdateLiveGuideOverlay();
    }

    private void UpdateLiveGuideOverlay()
    {
        if (LivePreviewImage.Visibility != Visibility.Visible)
        {
            LiveGuideOverlayImage.Source = null;
            LiveGuideOverlayImage.Visibility = Visibility.Collapsed;
            return;
        }

        var session = GetSelectedSession();
        var template = GetSelectedTemplate(session?.Id);
        if (template is null)
        {
            LiveGuideOverlayImage.Source = null;
            LiveGuideOverlayImage.Visibility = Visibility.Collapsed;
            return;
        }

        var targetWidth = Math.Max(1, (int)Math.Round(LivePreviewFrame.ActualWidth > 0 ? LivePreviewFrame.ActualWidth : 1600));
        var targetHeight = Math.Max(1, (int)Math.Round(LivePreviewFrame.ActualHeight > 0 ? LivePreviewFrame.ActualHeight : 900));
        var guide = _compositeRenderer.RenderGuideOverlay(
            template,
            GetCurrentSessionPhotos(session?.Id).OrderBy(photo => photo.SlotIndex).ThenBy(photo => photo.CapturedAt).ToList(),
            GetSelectedFrame(session?.Id),
            targetWidth,
            targetHeight);
        LiveGuideOverlayImage.Source = guide;
        LiveGuideOverlayImage.Visibility = Visibility.Visible;
    }

    private List<NativePhotoRecord> GetCurrentSessionPhotos(string? sessionId = null)
    {
        var selectedSessionId = sessionId ?? _selectedSessionId ?? _lastSnapshot?.ActiveSessionId;
        return _lastSnapshot?.GalleryPhotos
            .Where(photo => string.IsNullOrWhiteSpace(selectedSessionId) || photo.SessionId == selectedSessionId)
            .OrderBy(photo => photo.SlotIndex)
            .ThenBy(photo => photo.CapturedAt)
            .ToList() ?? [];
    }

    private async Task<NativeRenderResult?> TryFinalizeTemplateIfReadyAsync(NativeSessionRecord session)
    {
        var template = GetSelectedTemplate(session.Id);
        if (template is null)
        {
            return null;
        }

        var photos = GetCurrentSessionPhotos(session.Id);
        if (photos.Count < template.Slots.Count)
        {
            TemplateProgressText.Text = BuildTemplateProgressText(template, photos);
            UpdateTemplateWorkspacePreview();
            UpdateSessionProgress(session);
            return null;
        }

        return await RenderFinalCompositeAsync(session, template, photos);
    }
    private async Task EnsurePreferencesSavedAsync()
    {
        if (_selectedTemplateId is not null)
        {
            await _dataService.SaveSelectedTemplateAsync(_selectedTemplateId);
        }

        if (_selectedFrameId is not null)
        {
            await _dataService.SaveSelectedFrameAsync(_selectedFrameId);
        }

        await _dataService.SaveBeautyLevelAsync(_selectedBeautyLevel);
        await _dataService.SaveSelectedSourceModeAsync(_selectedSourceMode);
        if (_selectedEffectPresetId is not null)
        {
            await _dataService.SaveSelectedEffectPresetAsync(_selectedEffectPresetId);
        }
    }

    private NativeBeautyLevel ParseBeautyLevel(string? value)
    {
        return Enum.TryParse<NativeBeautyLevel>(value, true, out var level) ? level : NativeBeautyLevel.Off;
    }

    private NativeSourceMode ParseSourceMode(string? value)
    {
        return Enum.TryParse<NativeSourceMode>(value, true, out var mode) ? mode : NativeSourceMode.Camera;
    }

    private string NormalizeWindowOrientation(string? value)
    {
        return string.Equals(value, "Portrait", StringComparison.OrdinalIgnoreCase) ? "Portrait" : "Landscape";
    }

    private string GetAssignmentMode() => ((AssignmentModeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "auto").Trim().ToLowerInvariant();

    private string BuildSelectedSourcesText()
    {
        var session = GetSelectedSession();
        var photos = GetCurrentSessionPhotos(session?.Id);
        var required = GetSelectedTemplate(session?.Id)?.Slots.Count ?? 0;
        var selectedCount = GalleryList.SelectedItems.Count;
        return _currentLanguage == "zh-CN"
            ? $"当前来源模式：{_selectedSourceMode} · Session 已收集 {photos.Count}/{required} 张 · 图库选中 {selectedCount} 张"
            : $"Source mode: {_selectedSourceMode} · Session has {photos.Count}/{required} images · Gallery selected {selectedCount}";
    }

    private void BindEditablePhotoControls(IEnumerable<NativePhotoRecord> photos)
    {
        _isBindingEditablePhotoList = true;
        _isBindingSlotList = true;
        try
        {
            var list = photos
                .OrderBy(photo => photo.SlotIndex)
                .ThenBy(photo => photo.SourceOrder)
                .ThenBy(photo => photo.CapturedAt)
                .ToList();

            EditablePhotoComboBox.Items.Clear();
            AssignSlotComboBox.Items.Clear();

            var template = GetSelectedTemplate();
            var slotCount = Math.Max(1, template?.Slots.Count ?? 1);
            for (var index = 0; index < slotCount; index++)
            {
                AssignSlotComboBox.Items.Add(new ComboBoxItem
                {
                    Content = _currentLanguage == "zh-CN" ? $"第 {index + 1} 格" : $"Slot {index + 1}",
                    Tag = index.ToString(CultureInfo.InvariantCulture)
                });
            }

            foreach (var photo in list)
            {
                EditablePhotoComboBox.Items.Add(new ComboBoxItem
                {
                    Content = _currentLanguage == "zh-CN"
                        ? $"格 {photo.SlotIndex + 1} · {photo.FileName}"
                        : $"Slot {photo.SlotIndex + 1} · {photo.FileName}",
                    Tag = photo
                });
            }

            if (list.Count == 0)
            {
                _selectedEditablePhotoId = null;
                EditablePhotoComboBox.SelectedIndex = -1;
                AssignSlotComboBox.SelectedIndex = -1;
                _pendingManualSlotIndex = null;
                ApplyEditablePhotoToUi(null);
                return;
            }

            var selected = list.FirstOrDefault(photo => photo.Id == _selectedEditablePhotoId) ?? list.First();
            _selectedEditablePhotoId = selected.Id;

            foreach (var item in EditablePhotoComboBox.Items.OfType<ComboBoxItem>())
            {
                if (item.Tag is NativePhotoRecord photo && photo.Id == selected.Id)
                {
                    EditablePhotoComboBox.SelectedItem = item;
                    break;
                }
            }

            ApplyEditablePhotoToUi(selected);
        }
        finally
        {
            _isBindingSlotList = false;
            _isBindingEditablePhotoList = false;
        }
    }

    private NativePhotoRecord? GetSelectedEditablePhoto()
    {
        if (!string.IsNullOrWhiteSpace(_selectedEditablePhotoId))
        {
            var found = GetCurrentSessionPhotos().FirstOrDefault(photo => photo.Id == _selectedEditablePhotoId);
            if (found is not null)
            {
                return found;
            }
        }

        return EditablePhotoComboBox.SelectedItem is ComboBoxItem item && item.Tag is NativePhotoRecord selected
            ? selected
            : null;
    }

    private void ApplyEditablePhotoToUi(NativePhotoRecord? photo)
    {
        _isApplyingPhotoTransformControls = true;
        try
        {
            if (photo is null)
            {
                PhotoScaleSlider.Value = 1d;
                PhotoRotationSlider.Value = 0d;
                PhotoOffsetXSlider.Value = 0d;
                PhotoOffsetYSlider.Value = 0d;
                ScaleValueText.Text = FormatScaleText(PhotoScaleSlider.Value);
                RotationValueText.Text = FormatRotationText(PhotoRotationSlider.Value);
                OffsetXValueText.Text = FormatOffsetText(PhotoOffsetXSlider.Value);
                OffsetYValueText.Text = FormatOffsetText(PhotoOffsetYSlider.Value);
                ReplaceEditablePhotoButton.IsEnabled = false;
                DeleteEditablePhotoButton.IsEnabled = false;
                MovePhotoPrevButton.IsEnabled = false;
                MovePhotoNextButton.IsEnabled = false;
                ResetPhotoTransformButton.IsEnabled = false;
                AssignSlotComboBox.IsEnabled = false;
                return;
            }

            PhotoScaleSlider.Value = Math.Max(PhotoScaleSlider.Minimum, Math.Min(PhotoScaleSlider.Maximum, photo.EditScale <= 0d ? 1d : photo.EditScale));
            PhotoRotationSlider.Value = Math.Max(PhotoRotationSlider.Minimum, Math.Min(PhotoRotationSlider.Maximum, photo.EditRotation));
            PhotoOffsetXSlider.Value = Math.Max(PhotoOffsetXSlider.Minimum, Math.Min(PhotoOffsetXSlider.Maximum, photo.EditOffsetX));
            PhotoOffsetYSlider.Value = Math.Max(PhotoOffsetYSlider.Minimum, Math.Min(PhotoOffsetYSlider.Maximum, photo.EditOffsetY));

            ScaleValueText.Text = FormatScaleText(PhotoScaleSlider.Value);
            RotationValueText.Text = FormatRotationText(PhotoRotationSlider.Value);
            OffsetXValueText.Text = FormatOffsetText(PhotoOffsetXSlider.Value);
            OffsetYValueText.Text = FormatOffsetText(PhotoOffsetYSlider.Value);

            ReplaceEditablePhotoButton.IsEnabled = true;
            DeleteEditablePhotoButton.IsEnabled = true;
            MovePhotoPrevButton.IsEnabled = true;
            MovePhotoNextButton.IsEnabled = true;
            ResetPhotoTransformButton.IsEnabled = true;
            AssignSlotComboBox.IsEnabled = AssignSlotComboBox.Items.Count > 0;

            foreach (var item in AssignSlotComboBox.Items.OfType<ComboBoxItem>())
            {
                if (string.Equals(item.Tag?.ToString(), photo.SlotIndex.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase))
                {
                    AssignSlotComboBox.SelectedItem = item;
                    break;
                }
            }
        }
        finally
        {
            _isApplyingPhotoTransformControls = false;
        }
    }

    private async Task MoveEditablePhotoByOffsetAsync(int direction)
    {
        var photo = GetSelectedEditablePhoto();
        if (photo is null)
        {
            return;
        }

        var template = GetSelectedTemplate();
        var slotCount = Math.Max(1, template?.Slots.Count ?? 1);
        var nextSlot = photo.SlotIndex + direction;
        nextSlot = Math.Max(0, Math.Min(slotCount - 1, nextSlot));
        if (nextSlot == photo.SlotIndex)
        {
            return;
        }

        await ForceSaveCurrentPhotoTransformAsync();
        await _dataService.AssignPhotoToSlotAsync(photo.Id, nextSlot);
        _lastSnapshot = await _dataService.LoadAsync();
        RenderLists();
        RuntimeStatusText.Text = _currentLanguage == "zh-CN" ? "照片位置已更新。" : "Photo position updated.";
    }

    private async Task RefreshUploadedPreviewFromPhotoAsync(NativePhotoRecord? photo)
    {
        if (photo is null)
        {
            return;
        }

        if (File.Exists(photo.ProcessedFilePath))
        {
            await SetUploadedPreviewAsync(photo.ProcessedFilePath);
        }
    }

    private string FormatScaleText(double value)
    {
        return _currentLanguage == "zh-CN"
            ? $"缩放：{value:0.00}x"
            : $"Scale: {value:0.00}x";
    }

    private string FormatRotationText(double value)
    {
        return _currentLanguage == "zh-CN"
            ? $"旋转：{value:0.#}°"
            : $"Rotation: {value:0.#}°";
    }

    private string FormatOffsetText(double value)
    {
        return _currentLanguage == "zh-CN"
            ? $"偏移：{value:+0.00;-0.00;0.00}"
            : $"Offset: {value:+0.00;-0.00;0.00}";
    }

    private async Task ForceSaveCurrentPhotoTransformAsync()
    {
        _photoTransformSaveTimer.Stop();
        _hasPendingTransformSave = false;
        var photo = GetSelectedEditablePhoto();
        if (photo is null)
        {
            return;
        }

        var ok = await _dataService.UpdatePhotoTransformAsync(
            photo.Id,
            PhotoScaleSlider.Value,
            PhotoRotationSlider.Value,
            PhotoOffsetXSlider.Value,
            PhotoOffsetYSlider.Value);
        if (!ok)
        {
            return;
        }

        _lastSnapshot = await _dataService.LoadAsync();
        RenderLists();
    }

    private static bool IsSupportedImageFile(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
    }

    private async Task ImportExternalPhotosAsync(IEnumerable<string> filePaths, NativePhotoSourceOrigin origin)
    {
        await EnsurePreferencesSavedAsync();
        await ForceSaveCurrentPhotoTransformAsync();
        var session = await _dataService.GetOrCreateActiveSessionAsync(GetSelectedCaptureMode(), GetShotCount(), GetCountdownSeconds(), origin == NativePhotoSourceOrigin.Gallery ? NativeSourceMode.Gallery : NativeSourceMode.Upload);
        _selectedSessionId = session.Id;
        var created = await _dataService.ImportSourcePhotosAsync(
            session.Id,
            filePaths,
            origin,
            _selectedBeautyLevel,
            _selectedEffectPresetId ?? "clean-modern");
        _lastSnapshot = await _dataService.LoadAsync();
        RenderLists();
        if (created.Count > 0)
        {
            DisplayGalleryPhoto(created[0].ProcessedFilePath);
            await SetUploadedPreviewAsync(created[0].ProcessedFilePath);
            _selectedEditablePhotoId = created[0].Id;
        }

        var refreshedSession = GetSelectedSession() ?? session;
        var autoRendered = await TryFinalizeTemplateIfReadyAsync(refreshedSession);
        RuntimeStatusText.Text = autoRendered is null
            ? (_currentLanguage == "zh-CN"
                ? $"已导入 {created.Count} 张照片，可继续补齐模板。"
                : $"Imported {created.Count} photo(s). Add more if the template still needs images.")
            : $"{GetTemplateRenderedMessage()} {Path.GetFileName(autoRendered.PngPath)}";
    }

    private async Task SetUploadedPreviewAsync(string path)
    {
        _uploadedPreviewPath = path;
        _isApplyingUploadedPreview = true;
        await RefreshUploadedPreviewAsync();
    }

    private async Task RefreshUploadedPreviewAsync()
    {
        if (!_isApplyingUploadedPreview || string.IsNullOrWhiteSpace(_uploadedPreviewPath))
        {
            return;
        }

        if (!File.Exists(_uploadedPreviewPath))
        {
            _isApplyingUploadedPreview = false;
            _uploadedPreviewPath = null;
            return;
        }

        var preview = await _livePhotoPreviewService.RenderPreviewAsync(
            _uploadedPreviewPath,
            _selectedBeautyLevel,
            GetSelectedEffectPreset(),
            _selectedStickerKind,
            _selectedMaskMode,
            1600);

        if (preview is null)
        {
            return;
        }

        _currentPreviewAspectRatio = preview.PixelHeight > 0 ? preview.PixelWidth / (double)preview.PixelHeight : (9d / 16d);
        UpdatePreviewFrameLayout();
        LivePreviewImage.Source = preview;
        LivePreviewImage.Visibility = Visibility.Visible;
        LivePreviewPlaceholder.Visibility = Visibility.Collapsed;
        LiveGuideOverlayImage.Visibility = Visibility.Collapsed;
        RuntimeStatusText.Text = Localization.Get(_currentLanguage, "preview_test_ready");
        _fullscreenPreviewWindow?.SetPreview(preview, Localization.Get(_currentLanguage, "fullscreen_preview_title"));
    }

    private NativePreviewStickerKind ParseStickerKind(string? value)
    {
        return Enum.TryParse<NativePreviewStickerKind>(value, true, out var kind) ? kind : NativePreviewStickerKind.None;
    }

    private NativePreviewMaskMode ParseMaskMode(string? value)
    {
        return Enum.TryParse<NativePreviewMaskMode>(value, true, out var mode) ? mode : NativePreviewMaskMode.None;
    }

    private async Task AssignSelectedPhotoToPendingSlotAsync(NativePhotoRecord photo)
    {
        if (!_pendingManualSlotIndex.HasValue)
        {
            return;
        }

        await ForceSaveCurrentPhotoTransformAsync();
        await _dataService.AssignPhotoToSlotAsync(photo.Id, _pendingManualSlotIndex.Value);
        _pendingManualSlotIndex = null;
        _lastSnapshot = await _dataService.LoadAsync();
        RenderLists();
        RuntimeStatusText.Text = _currentLanguage == "zh-CN"
            ? "已手动更新模板格子对应的照片。"
            : "Template slot assignment updated.";
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



