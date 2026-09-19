using ECommons.DalamudServices;

namespace RotationSolver.Basic.Data;

/// <summary>
/// Enum representing different types of compatibility issues.
/// </summary>
[Flags]
public enum CompatibleType : byte
{
	/// <summary>
	/// 
	/// </summary>
	Skill_Usage = 1 << 0,

	/// <summary>
	/// 
	/// </summary>
	Skill_Selection = 1 << 1,

	/// <summary>
	/// 
	/// </summary>
	Crash = 1 << 2,

	/// <summary>
	/// 
	/// </summary>
	Broken = 1 << 3,
}

/// <summary>
/// Struct representing an incompatible plugin.
/// </summary>
public readonly struct IncompatiblePlugin
{
	/// <summary>
	/// 
	/// </summary>
	public string Name { get; init; }

	/// <summary>
	///
	/// </summary>
	public string Icon { get; init; }

	/// <summary>
	/// 
	/// </summary>
	public string Url { get; init; }

	/// <summary>
	/// 
	/// </summary>
	public string Features { get; init; }

	/// <summary>
	/// Checks if the plugin is enabled.
	/// </summary>
	[Newtonsoft.Json.JsonIgnore]
	public readonly bool IsEnabled
	{
		get
		{
			var name = Name;
			var installedPlugins = Svc.PluginInterface.InstalledPlugins;
			foreach (var x in installedPlugins)
			{
				if ((x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || x.InternalName.Equals(name, StringComparison.OrdinalIgnoreCase)) && x.IsLoaded)
				{
					return true;
				}
			}
			return false;
		}
	}

	/// <summary>
	/// Checks if the plugin is installed.
	/// </summary>
	[Newtonsoft.Json.JsonIgnore]
	public readonly bool IsInstalled
	{
		get
		{
			var name = Name;
			var installedPlugins = Svc.PluginInterface.InstalledPlugins;
			foreach (var x in installedPlugins)
			{
				if (x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || x.InternalName.Equals(name, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public CompatibleType Type { get; init; }
}

internal static class PluginCompatibility
{
	public static readonly IncompatiblePlugin[] IncompatiblePlugins =
	[
		new()
		{
			Name = "XIV Combo",
			Icon = "https://raw.githubusercontent.com/MKhayle/XIVComboExpanded/master/res/icon.png",
			Url = "https://github.com/attickdoor/XivComboPlugin",
			Features = "May have issues with Auto rotation and targetting conflicts",
			Type = CompatibleType.Skill_Usage | CompatibleType.Broken
		},
		new()
		{
			Name = "XIV Combo Expanded",
			Icon = "https://raw.githubusercontent.com/MKhayle/XIVComboExpanded/master/res/icon.png",
			Url = "https://github.com/daemitus/XIVComboPlugin",
			Features = "Fork of XIV Combo, may have issues with auto rotation and targetting conflicts",
			Type = CompatibleType.Skill_Usage | CompatibleType.Broken
		},
		new()
		{
			Name = "XIVSlothCombo",
			Icon = "https://raw.githubusercontent.com/Nik-Potokar/XIVSlothCombo/main/res/plugin/xivslothcombo.png",
			Url = "https://github.com/Nik-Potokar/XIVSlothCombo",
			Features = "Fork of XIV Combo Expanded, may have issues with auto rotation and targetting conflicts",
			Type = CompatibleType.Skill_Usage | CompatibleType.Broken
		},
		new()
		{
			Name = "Wrath Combo",
			Icon = "https://s3.puni.sh/media/plugin/60/icon-2bwhkn3zf1f.png",
			Url = "https://github.com/PunishXIV/WrathCombo",
			Features = "Fork of XIVSlothCombo, may have issues with auto rotation and targetting conflicts",
			Type = CompatibleType.Skill_Usage | CompatibleType.Broken | CompatibleType.Crash
		},
		new()
		{
			Name = "BossMod Reborn",
			Icon = "https://raw.githubusercontent.com/FFXIV-CombatReborn/RebornAssets/main/IconAssets/BMR_Icon.png",
			Url = "https://github.com/FFXIV-CombatReborn/BossmodReborn",
			Features = "Combat Reborn fork of Bossmod, may have issues with auto rotation and targetting conflicts, though mitigations exist",
			Type = CompatibleType.Skill_Usage
		},
		new()
		{
			Name = "BossMod",
			Icon = "https://raw.githubusercontent.com/awgil/ffxiv_bossmod/master/Data/icon.png",
			Url = "https://github.com/FFXIV-CombatReborn/BossmodReborn",
			Features = "Auto rotation and targetting conflicts",
			Type = CompatibleType.Skill_Usage
		},
		new()
		{
			Name = "Redirect",
			Icon = "https://raw.githubusercontent.com/cairthenn/Redirect/main/Redirect/icon.png",
			Url = "https://github.com/cairthenn/Redirect",
			Features = "Skill targetting conflicts",
			Type = CompatibleType.Skill_Usage
		},
		new()
		{
			Name = "ReAction",
			Icon = "",
			Url = "https://github.com/UnknownX7/ReAction",
			Features = "May have issues with skill queueing settings",
			Type = CompatibleType.Skill_Usage
		},
		new()
		{
			Name = "ReActionEX",
			Icon = "",
			Url = "https://github.com/Taurenkey/ReActionEX",
			Features = "May have issues with skill queueing settings",
			Type = CompatibleType.Skill_Usage
		},
		new()
		{
			Name = "Olympus",
			Icon = "",
			Url = "https://github.com/RoseOfficial/Olympus",
			Features = "AI slop clone of RSR",
			Type = CompatibleType.Skill_Usage | CompatibleType.Broken | CompatibleType.Crash
		},
		new()
		{
			Name = "Auto PVP Series Grind",
			Icon = "https://raw.githubusercontent.com/XeldarAlz/FFXIV-AutoPVPSeriesGrind/master/AutoPvpSeriesGrind/Images/Icon.png",
			Url = "https://github.com/XeldarAlz/FFXIV-AutoPVPSeriesGrind",
			Features = "AI slop plugin that bot-farms PvP matches by driving RSR through its own chat commands; RSR blocks autorotation in PvP while this is enabled",
			Type = CompatibleType.Broken
		}
	];
}
