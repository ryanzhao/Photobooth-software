using System.IO;
using System.Text.Json;

namespace Photobooth.BoothNative;

public sealed class BoothDataService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _dataRoot;
    private readonly string _stateFile;
    private readonly SemaphoreSlim _stateLock = new(1, 1);
    private readonly TemplateManager _templateManager = new();
    private readonly FrameOverlayManager _frameOverlayManager;
    private readonly EffectManager _effectManager;

    public BoothDataService()
    {
        _frameOverlayManager = new FrameOverlayManager(_templateManager);
        _effectManager = new EffectManager(_templateManager);
        _dataRoot = Path.Combine(AppContext.BaseDirectory, "booth-data", "native-booth-portrait-916");
        _stateFile = Path.Combine(_dataRoot, "state.json");
    }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_dataRoot);
        Directory.CreateDirectory(Path.Combine(_dataRoot, "sessions"));
        await _templateManager.InitializeAsync();
        await _frameOverlayManager.InitializeAsync();
        await _effectManager.InitializeAsync();

        if (!File.Exists(_stateFile))
        {
            var initial = await CreateInitialSnapshotAsync();
            await SaveAsync(initial);
        }
    }

    public async Task<BoothSnapshot> LoadAsync()
    {
        await InitializeAsync();
        await _stateLock.WaitAsync();
        try
        {
            BoothSnapshot snapshot;
            await using (var stream = new FileStream(_stateFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                snapshot = await JsonSerializer.DeserializeAsync<BoothSnapshot>(stream, JsonOptions) ?? new BoothSnapshot();
            }

            var templateCatalog = await _templateManager.LoadTemplatesAsync();
            var frameCatalog = await _frameOverlayManager.LoadFramesAsync();
            var presetCatalog = await _effectManager.LoadPresetsAsync();

            snapshot.Templates = NormalizeTemplates(snapshot.Templates, templateCatalog);
            snapshot.Frames = NormalizeFrames(snapshot.Frames, frameCatalog);
            snapshot.EffectPresets = NormalizeEffectPresets(snapshot.EffectPresets, presetCatalog);
            snapshot.SelectedTemplateId = ResolveSelectedTemplateId(snapshot.SelectedTemplateId, snapshot.Templates);
            snapshot.SelectedFrameId = ResolveSelectedFrameId(snapshot.SelectedFrameId, snapshot.Frames, snapshot.Templates, snapshot.SelectedTemplateId);
            snapshot.SelectedBeautyLevel = NormalizeBeautyLevel(snapshot.SelectedBeautyLevel).ToString();
            snapshot.SelectedSourceMode = NormalizeSourceMode(snapshot.SelectedSourceMode).ToString();
            snapshot.SelectedEffectPresetId = ResolveSelectedEffectPresetId(snapshot.SelectedEffectPresetId, snapshot.EffectPresets);
            snapshot.PreferredLanguage = string.IsNullOrWhiteSpace(snapshot.PreferredLanguage) ? "zh-CN" : snapshot.PreferredLanguage;
            snapshot.PreferredWindowOrientation = NormalizeWindowOrientation(snapshot.PreferredWindowOrientation);
            snapshot.RecentSessions = snapshot.RecentSessions
                .Select(session => NormalizeSession(session, snapshot))
                .OrderByDescending(session => session.CreatedAt)
                .Take(24)
                .ToList();
            var gallerySeed = snapshot.GalleryPhotos.Count > 0 ? snapshot.GalleryPhotos : DiscoverGalleryPhotos(snapshot.RecentSessions);
            snapshot.GalleryPhotos = gallerySeed
                .Select(NormalizePhoto)
                .OrderByDescending(photo => photo.CapturedAt)
                .Take(500)
                .ToList();

            await SaveSnapshotAsync(snapshot);
            return snapshot;
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public async Task SavePreferredLanguageAsync(string languageCode)
    {
        var snapshot = await LoadAsync();
        snapshot.PreferredLanguage = languageCode;
        await SaveAsync(snapshot);
    }

    public async Task SavePreferredWindowOrientationAsync(string orientation)
    {
        var snapshot = await LoadAsync();
        snapshot.PreferredWindowOrientation = NormalizeWindowOrientation(orientation);
        await SaveAsync(snapshot);
    }

    public async Task SaveSelectedTemplateAsync(string templateId)
    {
        var snapshot = await LoadAsync();
        if (snapshot.Templates.Any(template => template.Id == templateId))
        {
            snapshot.SelectedTemplateId = templateId;
            snapshot.SelectedFrameId = ResolveSelectedFrameId(snapshot.SelectedFrameId, snapshot.Frames, snapshot.Templates, templateId);
            await SaveAsync(snapshot);
        }
    }

    public async Task SaveSelectedFrameAsync(string frameId)
    {
        var snapshot = await LoadAsync();
        if (snapshot.Frames.Any(frame => frame.Id == frameId))
        {
            snapshot.SelectedFrameId = frameId;
            await SaveAsync(snapshot);
        }
    }

    public async Task SaveBeautyLevelAsync(NativeBeautyLevel level)
    {
        var snapshot = await LoadAsync();
        snapshot.SelectedBeautyLevel = level.ToString();
        await SaveAsync(snapshot);
    }

    public async Task SaveSelectedSourceModeAsync(NativeSourceMode sourceMode)
    {
        var snapshot = await LoadAsync();
        snapshot.SelectedSourceMode = sourceMode.ToString();
        await SaveAsync(snapshot);
    }

    public async Task SaveSelectedEffectPresetAsync(string presetId)
    {
        var snapshot = await LoadAsync();
        snapshot.SelectedEffectPresetId = ResolveSelectedEffectPresetId(presetId, snapshot.EffectPresets);
        await SaveAsync(snapshot);
    }

    public async Task<NativeSessionRecord> CreateSessionAsync(
        string captureMode,
        int shotCount,
        int countdownSeconds,
        NativeSourceMode? sourceMode = null)
    {
        var snapshot = await LoadAsync();
        var template = GetSelectedTemplate(snapshot);
        var frame = GetSelectedFrame(snapshot, template);
        var beautyLevel = NormalizeBeautyLevel(snapshot.SelectedBeautyLevel);
        var requiredShotCount = template?.Slots.Count ?? Math.Max(1, shotCount);
        var resolvedSourceMode = sourceMode ?? NormalizeSourceMode(snapshot.SelectedSourceMode);
        var effectPresetId = ResolveSelectedEffectPresetId(snapshot.SelectedEffectPresetId, snapshot.EffectPresets);
        var session = CreateSessionRecord(
            captureMode,
            Math.Max(requiredShotCount, shotCount),
            requiredShotCount,
            countdownSeconds,
            template,
            frame,
            beautyLevel,
            resolvedSourceMode,
            effectPresetId);
        snapshot.ActiveSessionId = session.Id;
        snapshot.RecentSessions.Insert(0, session);
        snapshot.RecentSessions = snapshot.RecentSessions.OrderByDescending(entry => entry.CreatedAt).Take(24).ToList();
        await SaveAsync(snapshot);
        await SaveSessionMetadataAsync(session.Id);
        return session;
    }

    public async Task<NativeSessionRecord> GetOrCreateActiveSessionAsync(
        string captureMode,
        int shotCount,
        int countdownSeconds,
        NativeSourceMode? sourceMode = null)
    {
        var snapshot = await LoadAsync();
        var active = snapshot.RecentSessions.FirstOrDefault(session => session.Id == snapshot.ActiveSessionId);
        if (active is not null && Directory.Exists(active.FolderPath))
        {
            return active;
        }

        return await CreateSessionAsync(captureMode, shotCount, countdownSeconds, sourceMode);
    }

    public async Task SetActiveSessionAsync(string sessionId)
    {
        var snapshot = await LoadAsync();
        if (snapshot.RecentSessions.Any(session => session.Id == sessionId))
        {
            snapshot.ActiveSessionId = sessionId;
            await SaveAsync(snapshot);
        }
    }

    public async Task ClearSessionPhotosAsync(string sessionId)
    {
        var snapshot = await LoadAsync();
        var session = snapshot.RecentSessions.FirstOrDefault(entry => entry.Id == sessionId);
        if (session is null)
        {
            return;
        }

        snapshot.GalleryPhotos = snapshot.GalleryPhotos
            .Where(photo => !string.Equals(photo.SessionId, sessionId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        session.PhotoCount = 0;
        session.CurrentShotIndex = 0;
        session.LastCaptureAt = null;
        session.Status = session.SourceMode == nameof(NativeSourceMode.Camera) ? "ready_to_capture" : "ready_to_import";
        session.FinalPngPath = null;
        session.FinalJpgPath = null;
        session.CompletedAt = null;
        session.UpdatedAt = DateTime.Now;

        DeleteFilesInDirectory(session.SourceFolderPath);
        DeleteFilesInDirectory(session.ProcessedFolderPath);
        DeleteFilesInDirectory(session.FinalFolderPath);
        await SaveAsync(snapshot);
        await SaveSessionMetadataAsync(sessionId);
    }

    public async Task<NativePhotoRecord?> RemoveLastSessionPhotoAsync(string sessionId)
    {
        var snapshot = await LoadAsync();
        var session = snapshot.RecentSessions.FirstOrDefault(entry => entry.Id == sessionId);
        if (session is null)
        {
            return null;
        }

        var target = snapshot.GalleryPhotos
            .Where(photo => string.Equals(photo.SessionId, sessionId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(photo => photo.SlotIndex)
            .ThenByDescending(photo => photo.CapturedAt)
            .FirstOrDefault();

        if (target is null)
        {
            return null;
        }

        snapshot.GalleryPhotos = snapshot.GalleryPhotos
            .Where(photo => !string.Equals(photo.Id, target.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();

        SafeDeleteFile(target.SourceFilePath);
        SafeDeleteFile(target.RawFilePath);
        SafeDeleteFile(target.ProcessedFilePath);

        var remaining = snapshot.GalleryPhotos
            .Where(photo => string.Equals(photo.SessionId, sessionId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(photo => photo.SlotIndex)
            .ThenBy(photo => photo.CapturedAt)
            .ToList();

        session.PhotoCount = remaining.Count;
        session.CurrentShotIndex = remaining.Count;
        session.LastCaptureAt = remaining.LastOrDefault()?.CapturedAt;
        session.Status = remaining.Count > 0
            ? "reviewing"
            : (session.SourceMode == nameof(NativeSourceMode.Camera) ? "ready_to_capture" : "ready_to_import");
        session.FinalPngPath = null;
        session.FinalJpgPath = null;
        session.CompletedAt = null;
        session.UpdatedAt = DateTime.Now;
        DeleteFilesInDirectory(session.FinalFolderPath);

        await SaveAsync(snapshot);
        await SaveSessionMetadataAsync(sessionId);
        return target;
    }

    public async Task<NativePhotoRecord?> AddCapturedPhotoAsync(
        string sessionId,
        string rawFilePath,
        string processedFilePath,
        NativeBeautyLevel beautyLevel,
        string effectPresetId = "clean-modern")
    {
        if (!File.Exists(rawFilePath) || !File.Exists(processedFilePath))
        {
            return null;
        }

        var snapshot = await LoadAsync();
        var session = snapshot.RecentSessions.FirstOrDefault(entry => entry.Id == sessionId);
        if (session is null)
        {
            return null;
        }

        var slotIndex = Math.Min(session.CurrentShotIndex, Math.Max(0, session.RequiredShotCount - 1));
        var existing = snapshot.GalleryPhotos.FirstOrDefault(photo => string.Equals(photo.ProcessedFilePath, processedFilePath, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing;
        }

        var photo = new NativePhotoRecord
        {
            Id = $"photo_{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}",
            SessionId = sessionId,
            FileName = Path.GetFileName(processedFilePath),
            FilePath = processedFilePath,
            SourceFilePath = rawFilePath,
            RawFilePath = rawFilePath,
            ProcessedFilePath = processedFilePath,
            SlotIndex = slotIndex,
            SourceOrigin = nameof(NativePhotoSourceOrigin.Camera),
            SourceOrder = slotIndex,
            AppliedBeautyLevel = beautyLevel.ToString(),
            AppliedEffectPresetId = effectPresetId,
            CapturedAt = File.GetLastWriteTime(processedFilePath)
        };

        snapshot.GalleryPhotos.Add(photo);
        snapshot.GalleryPhotos = snapshot.GalleryPhotos.OrderByDescending(entry => entry.CapturedAt).Take(500).ToList();
        UpdateSessionPhotoState(session, snapshot.GalleryPhotos);
        snapshot.ActiveSessionId = sessionId;
        await SaveAsync(snapshot);
        await SaveSessionMetadataAsync(sessionId);
        return photo;
    }

    public async Task<List<NativePhotoRecord>> ImportSourcePhotosAsync(
        string sessionId,
        IEnumerable<string> sourceFilePaths,
        NativePhotoSourceOrigin origin,
        NativeBeautyLevel beautyLevel,
        string effectPresetId)
    {
        var snapshot = await LoadAsync();
        var session = snapshot.RecentSessions.FirstOrDefault(entry => entry.Id == sessionId);
        if (session is null)
        {
            return [];
        }

        var files = sourceFilePaths
            .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .ToList();
        if (files.Count == 0)
        {
            return [];
        }

        var created = new List<NativePhotoRecord>();
        var existingCount = snapshot.GalleryPhotos.Count(photo => photo.SessionId == sessionId);
        for (var index = 0; index < files.Count; index++)
        {
            var sourcePath = files[index];
            var extension = Path.GetExtension(sourcePath);
            var baseName = $"{session.Id}_source_{existingCount + index + 1:000}";
            var copiedSourcePath = Path.Combine(session.SourceFolderPath, $"{baseName}{extension}");
            var processedPath = Path.Combine(session.ProcessedFolderPath, $"{baseName}_processed.jpg");
            File.Copy(sourcePath, copiedSourcePath, overwrite: true);
            File.Copy(copiedSourcePath, processedPath, overwrite: true);

            var slotIndex = Math.Min(existingCount + index, Math.Max(0, session.RequiredShotCount - 1));
            var photo = new NativePhotoRecord
            {
                Id = $"photo_{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}_{index}",
                SessionId = sessionId,
                FileName = Path.GetFileName(processedPath),
                FilePath = processedPath,
                SourceFilePath = copiedSourcePath,
                RawFilePath = copiedSourcePath,
                ProcessedFilePath = processedPath,
                SlotIndex = slotIndex,
                SourceOrigin = origin.ToString(),
                SourceOrder = existingCount + index,
                AppliedBeautyLevel = beautyLevel.ToString(),
                AppliedEffectPresetId = effectPresetId,
                CapturedAt = File.GetLastWriteTime(processedPath)
            };
            snapshot.GalleryPhotos.Add(photo);
            created.Add(photo);
        }

        session.SourceMode = origin == NativePhotoSourceOrigin.Gallery
            ? nameof(NativeSourceMode.Gallery)
            : nameof(NativeSourceMode.Upload);
        session.SelectedBeautyLevel = beautyLevel.ToString();
        session.SelectedEffectPresetId = effectPresetId;
        snapshot.GalleryPhotos = snapshot.GalleryPhotos.OrderByDescending(entry => entry.CapturedAt).Take(500).ToList();
        UpdateSessionPhotoState(session, snapshot.GalleryPhotos);
        snapshot.ActiveSessionId = sessionId;
        await SaveAsync(snapshot);
        await SaveSessionMetadataAsync(sessionId);
        return created;
    }

    public async Task AssignPhotoToSlotAsync(string photoId, int slotIndex)
    {
        var snapshot = await LoadAsync();
        var photo = snapshot.GalleryPhotos.FirstOrDefault(entry => entry.Id == photoId);
        if (photo is null)
        {
            return;
        }

        var sameSession = snapshot.GalleryPhotos
            .Where(entry => entry.SessionId == photo.SessionId && entry.Id != photo.Id && entry.SlotIndex == slotIndex)
            .ToList();

        foreach (var item in sameSession)
        {
            item.SlotIndex = photo.SlotIndex;
            item.IsManuallyAssigned = true;
        }

        photo.SlotIndex = slotIndex;
        photo.IsManuallyAssigned = true;

        var session = snapshot.RecentSessions.FirstOrDefault(entry => entry.Id == photo.SessionId);
        if (session is not null)
        {
            session.SourceAssignmentMode = "manual";
            session.UpdatedAt = DateTime.Now;
        }

        await SaveAsync(snapshot);
        await SaveSessionMetadataAsync(photo.SessionId);
    }

    public async Task<bool> UpdatePhotoTransformAsync(string photoId, double scale, double rotation, double offsetX, double offsetY)
    {
        var snapshot = await LoadAsync();
        var photo = snapshot.GalleryPhotos.FirstOrDefault(entry => entry.Id == photoId);
        if (photo is null)
        {
            return false;
        }

        photo.EditScale = Math.Max(0.4d, Math.Min(2.4d, scale));
        photo.EditRotation = Math.Max(-90d, Math.Min(90d, rotation));
        photo.EditOffsetX = Math.Max(-0.6d, Math.Min(0.6d, offsetX));
        photo.EditOffsetY = Math.Max(-0.6d, Math.Min(0.6d, offsetY));

        var session = snapshot.RecentSessions.FirstOrDefault(entry => entry.Id == photo.SessionId);
        if (session is not null)
        {
            session.UpdatedAt = DateTime.Now;
        }

        await SaveAsync(snapshot);
        await SaveSessionMetadataAsync(photo.SessionId);
        return true;
    }

    public async Task<bool> RemovePhotoAsync(string photoId)
    {
        var snapshot = await LoadAsync();
        var photo = snapshot.GalleryPhotos.FirstOrDefault(entry => entry.Id == photoId);
        if (photo is null)
        {
            return false;
        }

        var sessionId = photo.SessionId;
        snapshot.GalleryPhotos = snapshot.GalleryPhotos
            .Where(entry => !string.Equals(entry.Id, photoId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        SafeDeleteFile(photo.SourceFilePath);
        SafeDeleteFile(photo.RawFilePath);
        SafeDeleteFile(photo.ProcessedFilePath);

        var session = snapshot.RecentSessions.FirstOrDefault(entry => entry.Id == sessionId);
        if (session is not null)
        {
            var sessionPhotos = snapshot.GalleryPhotos
                .Where(entry => entry.SessionId == sessionId)
                .OrderBy(entry => entry.SlotIndex)
                .ThenBy(entry => entry.SourceOrder)
                .ThenBy(entry => entry.CapturedAt)
                .ToList();
            for (var index = 0; index < sessionPhotos.Count; index++)
            {
                sessionPhotos[index].SourceOrder = index;
            }

            session.FinalPngPath = null;
            session.FinalJpgPath = null;
            session.CompletedAt = null;
            UpdateSessionPhotoState(session, snapshot.GalleryPhotos);
        }

        await SaveAsync(snapshot);
        await SaveSessionMetadataAsync(sessionId);
        return true;
    }

    public async Task<NativePhotoRecord?> ReplacePhotoSourceAsync(string photoId, string newSourceFilePath)
    {
        if (string.IsNullOrWhiteSpace(newSourceFilePath) || !File.Exists(newSourceFilePath))
        {
            return null;
        }

        var snapshot = await LoadAsync();
        var photo = snapshot.GalleryPhotos.FirstOrDefault(entry => entry.Id == photoId);
        if (photo is null)
        {
            return null;
        }

        var session = snapshot.RecentSessions.FirstOrDefault(entry => entry.Id == photo.SessionId);
        if (session is null)
        {
            return null;
        }

        var extension = Path.GetExtension(newSourceFilePath);
        var baseName = $"{session.Id}_replace_{DateTimeOffset.UtcNow:HHmmssfff}_{photo.SlotIndex + 1:00}";
        var copiedSourcePath = Path.Combine(session.SourceFolderPath, $"{baseName}{extension}");
        var processedPath = Path.Combine(session.ProcessedFolderPath, $"{baseName}_processed.jpg");

        File.Copy(newSourceFilePath, copiedSourcePath, overwrite: true);
        File.Copy(copiedSourcePath, processedPath, overwrite: true);

        SafeDeleteFile(photo.SourceFilePath);
        SafeDeleteFile(photo.RawFilePath);
        SafeDeleteFile(photo.ProcessedFilePath);

        photo.SourceFilePath = copiedSourcePath;
        photo.RawFilePath = copiedSourcePath;
        photo.ProcessedFilePath = processedPath;
        photo.FilePath = processedPath;
        photo.FileName = Path.GetFileName(processedPath);
        photo.CapturedAt = DateTime.Now;
        photo.IsRetake = true;

        session.FinalPngPath = null;
        session.FinalJpgPath = null;
        session.CompletedAt = null;
        UpdateSessionPhotoState(session, snapshot.GalleryPhotos);

        await SaveAsync(snapshot);
        await SaveSessionMetadataAsync(photo.SessionId);
        return photo;
    }

    public async Task MarkSessionCompletedAsync(string sessionId, string pngPath, string jpgPath)
    {
        var snapshot = await LoadAsync();
        var session = snapshot.RecentSessions.FirstOrDefault(entry => entry.Id == sessionId);
        if (session is null)
        {
            return;
        }

        session.Status = "ready_to_print";
        session.FinalPngPath = pngPath;
        session.FinalJpgPath = jpgPath;
        session.CompletedAt = DateTime.Now;
        session.UpdatedAt = DateTime.Now;
        await SaveAsync(snapshot);
        await SaveSessionMetadataAsync(sessionId);
    }

    public async Task<NativeSessionMetadata?> LoadSessionMetadataAsync(string sessionId)
    {
        var snapshot = await LoadAsync();
        var session = snapshot.RecentSessions.FirstOrDefault(entry => entry.Id == sessionId);
        if (session is null)
        {
            return null;
        }

        var template = snapshot.Templates.FirstOrDefault(entry => entry.Id == session.TemplateId);
        var frame = snapshot.Frames.FirstOrDefault(entry => entry.Id == session.SelectedFrameId);
        var preset = snapshot.EffectPresets.FirstOrDefault(entry => entry.Id == session.SelectedEffectPresetId);
        var photos = snapshot.GalleryPhotos
            .Where(photo => string.Equals(photo.SessionId, sessionId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(photo => photo.SlotIndex)
            .ThenBy(photo => photo.SourceOrder)
            .ThenBy(photo => photo.CapturedAt)
            .ToList();

        return new NativeSessionMetadata
        {
            Session = session,
            Photos = photos,
            SelectedTemplate = template,
            SelectedFrame = frame,
            SelectedEffectPreset = preset,
            ExportPngPath = session.FinalPngPath ?? string.Empty,
            ExportJpgPath = session.FinalJpgPath ?? string.Empty
        };
    }

    public async Task SaveSessionMetadataAsync(string sessionId)
    {
        var metadata = await LoadSessionMetadataAsync(sessionId);
        if (metadata is null)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(metadata.Session.MetadataFilePath)!);
        await using var stream = File.Create(metadata.Session.MetadataFilePath);
        await JsonSerializer.SerializeAsync(stream, metadata, JsonOptions);
    }

    private async Task<BoothSnapshot> CreateInitialSnapshotAsync()
    {
        var templates = await _templateManager.LoadTemplatesAsync();
        var frames = await _frameOverlayManager.LoadFramesAsync();
        var presets = await _effectManager.LoadPresetsAsync();
        return new BoothSnapshot
        {
            Templates = templates,
            Frames = frames,
            EffectPresets = presets,
            PreferredWindowOrientation = "Portrait",
            SelectedTemplateId = templates.FirstOrDefault()?.Id,
            SelectedFrameId = frames.FirstOrDefault()?.Id,
            SelectedBeautyLevel = NativeBeautyLevel.Off.ToString(),
            SelectedSourceMode = NativeSourceMode.Camera.ToString(),
            SelectedEffectPresetId = presets.FirstOrDefault()?.Id ?? "clean-modern"
        };
    }

    private NativeTemplateRecord? GetSelectedTemplate(BoothSnapshot snapshot)
    {
        var templateId = ResolveSelectedTemplateId(snapshot.SelectedTemplateId, snapshot.Templates);
        return snapshot.Templates.FirstOrDefault(template => template.Id == templateId) ?? snapshot.Templates.FirstOrDefault();
    }

    private NativeFrameRecord? GetSelectedFrame(BoothSnapshot snapshot, NativeTemplateRecord? template)
    {
        var frameId = ResolveSelectedFrameId(snapshot.SelectedFrameId, snapshot.Frames, snapshot.Templates, template?.Id);
        return snapshot.Frames.FirstOrDefault(frame => frame.Id == frameId) ?? snapshot.Frames.FirstOrDefault();
    }

    private NativeSessionRecord CreateSessionRecord(
        string captureMode,
        int shotCount,
        int requiredShotCount,
        int countdownSeconds,
        NativeTemplateRecord? template,
        NativeFrameRecord? frame,
        NativeBeautyLevel beautyLevel,
        NativeSourceMode sourceMode,
        string effectPresetId)
    {
        var id = $"session_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        var root = Path.Combine(_dataRoot, "sessions", id);
        var source = Path.Combine(root, "source");
        var raw = Path.Combine(root, "raw");
        var processed = Path.Combine(root, "processed");
        var final = Path.Combine(root, "final");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(raw);
        Directory.CreateDirectory(processed);
        Directory.CreateDirectory(final);

        return new NativeSessionRecord
        {
            Id = id,
            Status = sourceMode == NativeSourceMode.Camera ? "ready_to_capture" : "ready_to_import",
            CaptureMode = captureMode,
            SourceMode = sourceMode.ToString(),
            ShotCount = shotCount,
            RequiredShotCount = requiredShotCount,
            CurrentShotIndex = 0,
            CountdownSeconds = countdownSeconds,
            FolderPath = root,
            SourceFolderPath = source,
            RawFolderPath = raw,
            ProcessedFolderPath = processed,
            FinalFolderPath = final,
            MetadataFilePath = Path.Combine(root, "session-metadata.json"),
            TemplateId = template?.Id ?? string.Empty,
            TemplateName = template?.Name ?? string.Empty,
            SelectedFrameId = frame?.Id ?? string.Empty,
            SelectedBeautyLevel = beautyLevel.ToString(),
            SelectedEffectPresetId = effectPresetId,
            PhotoCount = 0,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    private async Task SaveAsync(BoothSnapshot snapshot)
    {
        await _stateLock.WaitAsync();
        try
        {
            await SaveSnapshotAsync(snapshot);
        }
        finally
        {
            _stateLock.Release();
        }
    }

    private async Task SaveSnapshotAsync(BoothSnapshot snapshot)
    {
        await using var stream = new FileStream(_stateFile, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, snapshot, JsonOptions);
    }

    private List<NativeTemplateRecord> NormalizeTemplates(List<NativeTemplateRecord> current, List<NativeTemplateRecord> defaults)
    {
        if (current.Count == 0)
        {
            return defaults;
        }

        var normalized = new List<NativeTemplateRecord>();
        foreach (var existing in current)
        {
            if (!string.IsNullOrWhiteSpace(existing.Id))
            {
                normalized.Add(existing);
                continue;
            }

            var match = defaults.FirstOrDefault(template => string.Equals(template.Name, existing.Name, StringComparison.OrdinalIgnoreCase))
                ?? defaults.FirstOrDefault(template => string.Equals(template.PaperSize, existing.PaperSize, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                normalized.Add(match);
            }
        }

        foreach (var template in defaults)
        {
            if (normalized.All(entry => entry.Id != template.Id))
            {
                normalized.Add(template);
            }
        }

        return normalized;
    }

    private List<NativeFrameRecord> NormalizeFrames(List<NativeFrameRecord> current, List<NativeFrameRecord> defaults)
    {
        if (current.Count == 0)
        {
            return defaults;
        }

        foreach (var fallback in defaults)
        {
            if (current.All(frame => frame.Id != fallback.Id))
            {
                current.Add(fallback);
            }
        }

        return current;
    }

    private List<NativeEffectPresetRecord> NormalizeEffectPresets(List<NativeEffectPresetRecord> current, List<NativeEffectPresetRecord> defaults)
    {
        if (current.Count == 0)
        {
            return defaults;
        }

        foreach (var fallback in defaults)
        {
            if (current.All(preset => preset.Id != fallback.Id))
            {
                current.Add(fallback);
            }
        }

        return current;
    }

    private NativeSessionRecord NormalizeSession(NativeSessionRecord session, BoothSnapshot snapshot)
    {
        var template = snapshot.Templates.FirstOrDefault(entry => entry.Id == session.TemplateId)
            ?? snapshot.Templates.FirstOrDefault(entry => entry.Id == snapshot.SelectedTemplateId)
            ?? snapshot.Templates.FirstOrDefault();
        var frame = snapshot.Frames.FirstOrDefault(entry => entry.Id == session.SelectedFrameId)
            ?? snapshot.Frames.FirstOrDefault(entry => entry.Id == snapshot.SelectedFrameId)
            ?? snapshot.Frames.FirstOrDefault();

        session.SourceFolderPath = string.IsNullOrWhiteSpace(session.SourceFolderPath) ? Path.Combine(session.FolderPath, "source") : session.SourceFolderPath;
        session.RawFolderPath = string.IsNullOrWhiteSpace(session.RawFolderPath) ? Path.Combine(session.FolderPath, "raw") : session.RawFolderPath;
        session.ProcessedFolderPath = string.IsNullOrWhiteSpace(session.ProcessedFolderPath) ? Path.Combine(session.FolderPath, "processed") : session.ProcessedFolderPath;
        session.FinalFolderPath = string.IsNullOrWhiteSpace(session.FinalFolderPath) ? Path.Combine(session.FolderPath, "final") : session.FinalFolderPath;
        session.MetadataFilePath = string.IsNullOrWhiteSpace(session.MetadataFilePath) ? Path.Combine(session.FolderPath, "session-metadata.json") : session.MetadataFilePath;
        session.TemplateId = template?.Id ?? session.TemplateId;
        session.TemplateName = string.IsNullOrWhiteSpace(session.TemplateName) ? template?.Name ?? string.Empty : session.TemplateName;
        session.RequiredShotCount = session.RequiredShotCount <= 0 ? template?.Slots.Count ?? Math.Max(1, session.ShotCount) : session.RequiredShotCount;
        session.ShotCount = Math.Max(session.ShotCount, session.RequiredShotCount);
        session.SelectedFrameId = string.IsNullOrWhiteSpace(session.SelectedFrameId) ? frame?.Id ?? string.Empty : session.SelectedFrameId;
        session.SelectedBeautyLevel = NormalizeBeautyLevel(session.SelectedBeautyLevel).ToString();
        session.SourceMode = NormalizeSourceMode(session.SourceMode).ToString();
        session.SelectedEffectPresetId = ResolveSelectedEffectPresetId(session.SelectedEffectPresetId, snapshot.EffectPresets);
        session.SourceAssignmentMode = string.IsNullOrWhiteSpace(session.SourceAssignmentMode) ? "auto" : session.SourceAssignmentMode;
        session.UpdatedAt = session.UpdatedAt == default ? session.CreatedAt : session.UpdatedAt;
        Directory.CreateDirectory(session.SourceFolderPath);
        Directory.CreateDirectory(session.RawFolderPath);
        Directory.CreateDirectory(session.ProcessedFolderPath);
        Directory.CreateDirectory(session.FinalFolderPath);
        return session;
    }

    private NativePhotoRecord NormalizePhoto(NativePhotoRecord photo)
    {
        photo.SourceFilePath = string.IsNullOrWhiteSpace(photo.SourceFilePath) ? photo.RawFilePath : photo.SourceFilePath;
        photo.RawFilePath = string.IsNullOrWhiteSpace(photo.RawFilePath) ? (string.IsNullOrWhiteSpace(photo.SourceFilePath) ? photo.FilePath : photo.SourceFilePath) : photo.RawFilePath;
        photo.ProcessedFilePath = string.IsNullOrWhiteSpace(photo.ProcessedFilePath) ? photo.FilePath : photo.ProcessedFilePath;
        photo.FilePath = photo.ProcessedFilePath;
        photo.FileName = string.IsNullOrWhiteSpace(photo.FileName) ? Path.GetFileName(photo.ProcessedFilePath) : photo.FileName;
        photo.SourceOrigin = NormalizePhotoSourceOrigin(photo.SourceOrigin).ToString();
        photo.AppliedBeautyLevel = NormalizeBeautyLevel(photo.AppliedBeautyLevel).ToString();
        photo.AppliedEffectPresetId = string.IsNullOrWhiteSpace(photo.AppliedEffectPresetId) ? "clean-modern" : photo.AppliedEffectPresetId;
        if (double.IsNaN(photo.EditScale) || photo.EditScale <= 0d)
        {
            photo.EditScale = 1d;
        }

        photo.EditScale = Math.Max(0.4d, Math.Min(2.4d, photo.EditScale));
        photo.EditRotation = Math.Max(-90d, Math.Min(90d, photo.EditRotation));
        photo.EditOffsetX = Math.Max(-0.6d, Math.Min(0.6d, photo.EditOffsetX));
        photo.EditOffsetY = Math.Max(-0.6d, Math.Min(0.6d, photo.EditOffsetY));
        return photo;
    }

    private string ResolveSelectedTemplateId(string? selectedTemplateId, List<NativeTemplateRecord> templates)
    {
        if (!string.IsNullOrWhiteSpace(selectedTemplateId) && templates.Any(template => template.Id == selectedTemplateId))
        {
            return selectedTemplateId;
        }

        return templates.FirstOrDefault()?.Id ?? string.Empty;
    }

    private string ResolveSelectedFrameId(string? selectedFrameId, List<NativeFrameRecord> frames, List<NativeTemplateRecord> templates, string? templateId)
    {
        if (!string.IsNullOrWhiteSpace(selectedFrameId) && frames.Any(frame => frame.Id == selectedFrameId))
        {
            return selectedFrameId;
        }

        var template = templates.FirstOrDefault(entry => entry.Id == templateId);
        if (!string.IsNullOrWhiteSpace(template?.DefaultFrameId) && frames.Any(frame => frame.Id == template.DefaultFrameId))
        {
            return template.DefaultFrameId!;
        }

        return frames.FirstOrDefault()?.Id ?? string.Empty;
    }

    private string ResolveSelectedEffectPresetId(string? presetId, List<NativeEffectPresetRecord> presets)
    {
        if (!string.IsNullOrWhiteSpace(presetId) && presets.Any(preset => preset.Id == presetId))
        {
            return presetId;
        }

        return presets.FirstOrDefault()?.Id ?? "clean-modern";
    }

    private NativeBeautyLevel NormalizeBeautyLevel(string? value)
    {
        return Enum.TryParse<NativeBeautyLevel>(value, true, out var parsed) ? parsed : NativeBeautyLevel.Off;
    }

    private NativeSourceMode NormalizeSourceMode(string? value)
    {
        return Enum.TryParse<NativeSourceMode>(value, true, out var parsed) ? parsed : NativeSourceMode.Camera;
    }

    private NativePhotoSourceOrigin NormalizePhotoSourceOrigin(string? value)
    {
        return Enum.TryParse<NativePhotoSourceOrigin>(value, true, out var parsed) ? parsed : NativePhotoSourceOrigin.Camera;
    }

    private string NormalizeWindowOrientation(string? value)
    {
        return string.Equals(value, "Portrait", StringComparison.OrdinalIgnoreCase) ? "Portrait" : "Landscape";
    }

    private void UpdateSessionPhotoState(NativeSessionRecord session, List<NativePhotoRecord> allPhotos)
    {
        var sessionPhotos = allPhotos
            .Where(entry => entry.SessionId == session.Id)
            .OrderBy(entry => entry.SlotIndex)
            .ThenBy(entry => entry.SourceOrder)
            .ThenBy(entry => entry.CapturedAt)
            .ToList();
        session.PhotoCount = sessionPhotos.Count;
        session.CurrentShotIndex = Math.Min(session.PhotoCount, session.RequiredShotCount);
        session.LastCaptureAt = sessionPhotos.LastOrDefault()?.CapturedAt;
        session.Status = session.CurrentShotIndex >= session.RequiredShotCount ? "reviewing" : "collecting_sources";
        session.UpdatedAt = DateTime.Now;
    }

    private void SafeDeleteFile(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private void DeleteFilesInDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(path))
        {
            File.Delete(file);
        }
    }

    private List<NativePhotoRecord> DiscoverGalleryPhotos(List<NativeSessionRecord> sessions)
    {
        var photos = new List<NativePhotoRecord>();
        foreach (var session in sessions)
        {
            var sourceFiles = Directory.Exists(session.ProcessedFolderPath)
                ? Directory.GetFiles(session.ProcessedFolderPath)
                    .Where(path => path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(path => File.GetLastWriteTime(path))
                    .ToList()
                : new List<string>();

            for (var index = 0; index < sourceFiles.Count; index++)
            {
                var processedPath = sourceFiles[index];
                photos.Add(new NativePhotoRecord
                {
                    Id = $"recovered_{session.Id}_{index + 1:000}",
                    SessionId = session.Id,
                    FileName = Path.GetFileName(processedPath),
                    FilePath = processedPath,
                    SourceFilePath = processedPath,
                    RawFilePath = processedPath,
                    ProcessedFilePath = processedPath,
                    SlotIndex = index,
                    SourceOrder = index,
                    SourceOrigin = session.SourceMode == nameof(NativeSourceMode.Gallery) ? nameof(NativePhotoSourceOrigin.Gallery) : nameof(NativePhotoSourceOrigin.Camera),
                    AppliedBeautyLevel = session.SelectedBeautyLevel,
                    AppliedEffectPresetId = session.SelectedEffectPresetId,
                    CapturedAt = File.GetLastWriteTime(processedPath)
                });
            }
        }

        return photos;
    }
}
