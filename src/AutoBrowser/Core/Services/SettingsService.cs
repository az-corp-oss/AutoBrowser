using AutoBrowser.Models;

namespace AutoBrowser.Services;

public class SettingsService(ISettingsRepository settingsRepository) : ISettingsService
{
    private readonly ISettingsRepository _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));

    public AppSettings LoadSettings() => _settingsRepository.Load();

    public void SaveSettings(AppSettings settings) => _settingsRepository.Save(settings);

    public async Task<AppSettings> LoadSettingsAsync(CancellationToken ct = default)
    {
        return await _settingsRepository.LoadAsync(ct);
    }

    public async Task SaveSettingsAsync(AppSettings settings, CancellationToken ct = default)
    {
        await _settingsRepository.SaveAsync(settings, ct);
    }
}