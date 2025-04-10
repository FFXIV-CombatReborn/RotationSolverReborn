using ECommons.DalamudServices;
using System.ComponentModel;

namespace RotationSolver.UI;

/// <summary>
/// Attribute to mark tabs that should be skipped.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
internal class TabSkipAttribute : Attribute
{
}

/// <summary>
/// Attribute to specify an icon for a tab.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
internal class TabIconAttribute : Attribute
{
    public uint Icon { get; init; }
}

/// <summary>
/// Enum representing different tabs in the rotation config window.
/// </summary>
internal enum RotationConfigWindowTab : byte
{
    [TabSkip] About,
    [TabSkip] Rotation,

    [Description("Configure abilities and custom conditions for your current job.")]
    [TabIcon(Icon = 4)] Actions,

    [Description("View and manage all loaded RSR rotations.")]
    [TabIcon(Icon = 47)] Rotations,

    [Description("Configure reactive actions and status effect lists.")]
    [TabIcon(Icon = 21)] List,

    [Description("Configure basic settings.")]
    [TabIcon(Icon = 14)] Basic,

    [Description("Configure user interface settings.")]
    [TabIcon(Icon = 42)] UI,

    [Description("Configure general action usage and control settings.")]
    [TabIcon(Icon = 29)] Auto,

    [Description("Configure targeting settings.")]
    [TabIcon(Icon = 16)] Target,

    [Description("Configure optional helpful features.")]
    [TabIcon(Icon = 51)] Extra,

    [Description("Debug options for developers and rotation writers (disable when not in use).")]
    [TabIcon(Icon = 5)] Debug,

    [Description("Configure AutoDuty settings and view related information.")]
    [TabIcon(Icon = 4)] AutoDuty,
}

/// <summary>
/// Struct representing a 3PP dalamud plugin.
/// </summary>
public struct CommunityPlugin
{
    public string Name { get; init; }
    public string Icon { get; init; }
    public string Url { get; init; }
    public string Features { get; init; }

    /// <summary>
    /// Checks if the plugin is enabled.
    /// </summary>
    [JsonIgnore]
    public readonly bool IsEnabled
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
    public readonly bool IsInstalled
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
public enum CompatibilityType : byte
{
    Compatible = 0,
    Skill_Usage = 1 << 0,
    Skill_Selection = 1 << 1,
    Crash = 1 << 2,
    Broken = 1 << 3,
}