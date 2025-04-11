using ECommons.DalamudServices;

namespace RotationSolver.Data;
/// <summary>
/// Struct representing a 3PP dalamud plugin.
/// </summary>
public class CommunityPlugin
{
    public static CommunityPlugin[] IncompatiblePlugins { get; set; } = [];
    public string Name { get; set; }
    public string Icon { get; set; }
    public string Url { get; set; }
    public string Features { get; set; }
    public string CompatibilityIssues { get; set; }

    /// <summary>
    /// Checks if the plugin is enabled.
    /// </summary>
    [JsonIgnore]
    public bool IsEnabled
    {
        get
        {
            var name = Name;
            var installedPlugins = Svc.PluginInterface.InstalledPlugins;
            return installedPlugins.Any(x =>
                (x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || x.InternalName.Equals(name, StringComparison.OrdinalIgnoreCase)) && x.IsLoaded);
        }
    }

    /// <summary>
    /// Checks if the plugin is installed.
    /// </summary>
    [JsonIgnore]
    public bool IsInstalled
    {
        get
        {
            var name = Name;
            var installedPlugins = Svc.PluginInterface.InstalledPlugins;
            return installedPlugins.Any(x =>
                x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || x.InternalName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}