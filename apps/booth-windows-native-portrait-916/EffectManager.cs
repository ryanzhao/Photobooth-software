namespace Photobooth.BoothNative;

public sealed class EffectManager
{
    private readonly TemplateManager _templateManager;

    public EffectManager(TemplateManager templateManager)
    {
        _templateManager = templateManager;
    }

    public async Task InitializeAsync()
    {
        await _templateManager.InitializeAsync();
    }

    public async Task<List<NativeEffectPresetRecord>> LoadPresetsAsync()
    {
        await InitializeAsync();
        return await _templateManager.LoadEffectPresetsAsync();
    }

    public async Task<NativeEffectPresetRecord?> GetPresetAsync(string? presetId)
    {
        var presets = await LoadPresetsAsync();
        if (!string.IsNullOrWhiteSpace(presetId))
        {
            var match = presets.FirstOrDefault(preset => string.Equals(preset.Id, presetId, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return match;
            }
        }

        return presets.FirstOrDefault();
    }
}
