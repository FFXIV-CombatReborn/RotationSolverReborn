using ECommons.Logging;
using RotationSolver.UI;

namespace RotationSolver.Helpers;

public static class DownloadHelper
{
    private static readonly HttpClient Http = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        try
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("RotationSolver");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }
        catch { /* headers are best-effort */ }
        return client;
    }

    public static IncompatiblePlugin[] IncompatiblePlugins { get; private set; } = [];

    public static async Task DownloadAsync()
    {
        IncompatiblePlugins = await DownloadOneAsync<IncompatiblePlugin[]>($"https://raw.githubusercontent.com/{Service.USERNAME}/{Service.REPO}/main/Resources/IncompatiblePlugins.json") ?? [];
    }

    private static async Task<T?> DownloadOneAsync<T>(string url)
    {
        try
        {
            string str = await Http.GetStringAsync(url);
            return JsonConvert.DeserializeObject<T>(str);
        }
        catch (Exception ex)
        {
            BasicWarningHelper.AddSystemWarning($"Failed to load downloading List because: {ex.Message}");
#if DEBUG
            PluginLog.Warning($"Failed to load downloading List: {ex.Message}");
#endif
            return default;
        }
    }
}
