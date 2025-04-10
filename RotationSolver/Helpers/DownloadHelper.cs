using ECommons.DalamudServices;
using RotationSolver.UI;

namespace RotationSolver.Helpers;

public static class DownloadHelper
{
    public static CommunityPlugin[] IncompatiblePlugins { get; private set; } = [];

    public static async Task DownloadAsync()
    {
        IncompatiblePlugins = await DownloadOneAsync<CommunityPlugin[]>($"https://raw.githubusercontent.com/{Service.USERNAME}/{Service.REPO}/main/Resources/IncompatiblePlugins.json") ?? [];
    }

    private static async Task<T?> DownloadOneAsync<T>(string url)
    {
        using var client = new HttpClient();
        try
        {
            var str = await client.GetStringAsync(url);
            return JsonConvert.DeserializeObject<T>(str);
        }
        catch (Exception ex)
        {
#pragma warning disable 0436
            WarningHelper.AddSystemWarning($"Failed to load downloading List because: {ex.Message}");
#if DEBUG
            Svc.Log.Information(ex, "Failed to load downloading List.");
#endif
            return default;
        }
    }
}
