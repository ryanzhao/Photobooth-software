using System.IO;
using System.Text.Json;

namespace Photobooth.BoothNative;

public sealed class BoothDataService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _dataRoot;
    private readonly string _stateFile;

    public BoothDataService()
    {
        _dataRoot = Path.Combine(AppContext.BaseDirectory, "booth-data", "native-booth");
        _stateFile = Path.Combine(_dataRoot, "state.json");
    }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_dataRoot);
        Directory.CreateDirectory(Path.Combine(_dataRoot, "sessions"));

        if (!File.Exists(_stateFile))
        {
            var initial = new BoothSnapshot
            {
                Templates = BuildDefaultTemplates(),
                SelectedTemplateId = "single-hero"
            };

            await SaveAsync(initial);
        }
    }

    public async Task<BoothSnapshot> LoadAsync()
    {
        await InitializeAsync();
        await using var stream = File.OpenRead(_stateFile);
        var snapshot = await JsonSerializer.DeserializeAsync<BoothSnapshot>(stream, JsonOptions) ?? new BoothSnapshot();
        snapshot.Templates = snapshot.Templates.Count == 0 ? BuildDefaultTemplates() : snapshot.Templates;
        snapshot.SelectedTemplateId = string.IsNullOrWhiteSpace(snapshot.SelectedTemplateId)
            ? snapshot.Templates.FirstOrDefault()?.Id
            : snapshot.SelectedTemplateId;
        snapshot.PreferredLanguage = string.IsNullOrWhiteSpace(snapshot.PreferredLanguage) ? "zh-CN" : snapshot.PreferredLanguage;
        snapshot.RecentSessions = snapshot.RecentSessions.OrderByDescending(x => x.CreatedAt).Take(24).ToList();
        snapshot.GalleryPhotos = snapshot.GalleryPhotos.OrderByDescending(x => x.CapturedAt).Take(120).ToList();
        return snapshot;
    }

    public async Task SavePreferredLanguageAsync(string languageCode)
    {
        var snapshot = await LoadAsync();
        snapshot.PreferredLanguage = languageCode;
        await SaveAsync(snapshot);
    }

    public async Task<NativeSessionRecord> CreateSessionAsync(string captureMode, int shotCount, int countdownSeconds)
    {
        var snapshot = await LoadAsync();
        var session = CreateSessionRecord(captureMode, shotCount, countdownSeconds);
        snapshot.ActiveSessionId = session.Id;
        snapshot.RecentSessions.Insert(0, session);
        snapshot.RecentSessions = snapshot.RecentSessions.OrderByDescending(x => x.CreatedAt).Take(24).ToList();
        await SaveAsync(snapshot);
        return session;
    }

    public async Task<NativeSessionRecord> GetOrCreateActiveSessionAsync(string captureMode, int shotCount, int countdownSeconds)
    {
        var snapshot = await LoadAsync();
        var active = snapshot.RecentSessions.FirstOrDefault(x => x.Id == snapshot.ActiveSessionId);
        if (active is not null && Directory.Exists(active.FolderPath))
        {
            return active;
        }

        var session = CreateSessionRecord(captureMode, shotCount, countdownSeconds);
        snapshot.ActiveSessionId = session.Id;
        snapshot.RecentSessions.Insert(0, session);
        snapshot.RecentSessions = snapshot.RecentSessions.OrderByDescending(x => x.CreatedAt).Take(24).ToList();
        await SaveAsync(snapshot);
        return session;
    }

    public async Task SetActiveSessionAsync(string sessionId)
    {
        var snapshot = await LoadAsync();
        if (snapshot.RecentSessions.Any(x => x.Id == sessionId))
        {
            snapshot.ActiveSessionId = sessionId;
            await SaveAsync(snapshot);
        }
    }

    public async Task SaveSelectedTemplateAsync(string templateId)
    {
        var snapshot = await LoadAsync();
        if (snapshot.Templates.Any(x => x.Id == templateId))
        {
            snapshot.SelectedTemplateId = templateId;
            await SaveAsync(snapshot);
        }
    }

    public async Task ClearSessionPhotosAsync(string sessionId)
    {
        var snapshot = await LoadAsync();
        snapshot.GalleryPhotos = snapshot.GalleryPhotos
            .Where(x => !string.Equals(x.SessionId, sessionId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var session = snapshot.RecentSessions.FirstOrDefault(x => x.Id == sessionId);
        if (session is not null)
        {
            session.PhotoCount = 0;
            session.LastCaptureAt = null;
            session.Status = "ready_to_capture";
        }

        await SaveAsync(snapshot);
    }

    public async Task<NativePhotoRecord?> RemoveLastSessionPhotoAsync(string sessionId)
    {
        var snapshot = await LoadAsync();
        var target = snapshot.GalleryPhotos
            .Where(x => string.Equals(x.SessionId, sessionId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.CapturedAt)
            .FirstOrDefault();

        if (target is null)
        {
            return null;
        }

        snapshot.GalleryPhotos = snapshot.GalleryPhotos
            .Where(x => !string.Equals(x.Id, target.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var session = snapshot.RecentSessions.FirstOrDefault(x => x.Id == sessionId);
        if (session is not null)
        {
            var remaining = snapshot.GalleryPhotos
                .Where(x => string.Equals(x.SessionId, sessionId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.CapturedAt)
                .ToList();
            session.PhotoCount = remaining.Count;
            session.LastCaptureAt = remaining.FirstOrDefault()?.CapturedAt;
            session.Status = remaining.Count > 0 ? "captured" : "ready_to_capture";
        }

        await SaveAsync(snapshot);
        return target;
    }

    public async Task<NativePhotoRecord?> AddCapturedPhotoAsync(string sessionId, string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        var snapshot = await LoadAsync();
        var session = snapshot.RecentSessions.FirstOrDefault(x => x.Id == sessionId);
        if (session is null)
        {
            return null;
        }

        var existing = snapshot.GalleryPhotos.FirstOrDefault(x => string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing;
        }

        var photo = new NativePhotoRecord
        {
            Id = $"photo_{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}",
            SessionId = sessionId,
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            CapturedAt = File.GetLastWriteTime(filePath)
        };

        snapshot.GalleryPhotos.Insert(0, photo);
        snapshot.GalleryPhotos = snapshot.GalleryPhotos.OrderByDescending(x => x.CapturedAt).Take(120).ToList();
        session.PhotoCount = snapshot.GalleryPhotos.Count(x => x.SessionId == sessionId);
        session.LastCaptureAt = photo.CapturedAt;
        session.Status = "captured";
        snapshot.ActiveSessionId = sessionId;
        await SaveAsync(snapshot);
        return photo;
    }

    private NativeSessionRecord CreateSessionRecord(string captureMode, int shotCount, int countdownSeconds)
    {
        var id = $"session_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        var folder = Path.Combine(_dataRoot, "sessions", id);
        Directory.CreateDirectory(folder);
        Directory.CreateDirectory(Path.Combine(folder, "originals"));
        Directory.CreateDirectory(Path.Combine(folder, "processed"));
        Directory.CreateDirectory(Path.Combine(folder, "outputs"));

        return new NativeSessionRecord
        {
            Id = id,
            Status = "ready_to_capture",
            CaptureMode = captureMode,
            ShotCount = shotCount,
            CountdownSeconds = countdownSeconds,
            FolderPath = folder,
            PhotoCount = 0,
            CreatedAt = DateTime.Now
        };
    }

    private async Task SaveAsync(BoothSnapshot snapshot)
    {
        await using var stream = File.Create(_stateFile);
        await JsonSerializer.SerializeAsync(stream, snapshot, JsonOptions);
    }

    private List<NativeTemplateRecord> BuildDefaultTemplates() =>
    [
        new() { Id = "single-hero", Name = "4x6 Single Hero", PaperSize = "4x6", Description = "Single photo layout for portrait hero shots." },
        new() { Id = "grid-4x6", Name = "4x6 Grid", PaperSize = "4x6", Description = "Four-up event collage." },
        new() { Id = "strip-2x6", Name = "2x6 Classic Strip", PaperSize = "2x6", Description = "Classic photo strip with QR footer." },
        new() { Id = "square-collage", Name = "Square Collage", PaperSize = "Square", Description = "Social-first square output." },
        new() { Id = "freeform-hero", Name = "Freeform Event Hero", PaperSize = "Custom", Description = "Asymmetric premium event composition." }
    ];
}
