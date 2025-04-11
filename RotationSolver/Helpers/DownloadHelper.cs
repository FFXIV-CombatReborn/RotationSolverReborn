using ECommons.DalamudServices;

namespace RotationSolver.Helpers;

public static class DownloadHelper
{
    public static async Task<T?> DownloadOneAsync<T>(string url)
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
