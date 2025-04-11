using ECommons.DalamudServices;

namespace RotationSolver.Data;
/// <summary>
/// Struct representing a 3PP dalamud plugin.
/// </summary>
public class CommunityPlugin
{
    public static CommunityPlugin[] IncompatiblePlugins { get; set; } = [];
    public string Name { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Features { get; init; } = string.Empty;

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

    public CompatibilityType Type { get; init; }
}


/// <summary>
/// Enum representing different types of compatibility issues.
/// </summary>
[Flags]
public enum CompatibilityType
{
    Compatible,
    Minor,
    Major
}