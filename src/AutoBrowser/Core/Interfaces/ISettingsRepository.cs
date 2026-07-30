using AutoBrowser.Models;

namespace AutoBrowser.Services;

public interface ISettingsRepository
{
    AppSettings Load();
    void Save(AppSettings settings);
    Task<AppSettings> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(AppSettings settings, CancellationToken ct = default);
}