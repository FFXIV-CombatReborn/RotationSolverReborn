using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;
using ECommons.Logging;
using ECommons.Reflection;
using RebornMaterial;

namespace RotationSolver.UI;

public partial class MainWindow
{
	private static void DrawAutoduty()
	{
		var scheme = M3.Scheme;

		_ = M3Widgets.Banner("##autoduty_notice",
			"While the RSR team has made an effort to keep RSR compatible with AutoDuty, RSR is not designed with botting in mind.",
			M3Severity.Warning, FontAwesomeIcon.Robot);

		ImGui.Dummy(new Vector2(0f, M3.Space2));
		DrawPageIntro("These are the plugins AutoDuty relies on, and their current state.");

		List<AutoDutyPlugin> pluginsToCheck =
		[
			new AutoDutyPlugin { Name = "AutoDuty", Url = "https://puni.sh/api/repository/erdelf" },
			new AutoDutyPlugin { Name = "vnavmesh", Url = "https://puni.sh/api/repository/veyn" },
			new AutoDutyPlugin { Name = "BossModReborn", Url = "https://raw.githubusercontent.com/FFXIV-CombatReborn/CombatRebornRepo/main/pluginmaster.json" },
			new AutoDutyPlugin { Name = "Boss Mod", Url = "https://puni.sh/api/repository/veyn" },
			new AutoDutyPlugin { Name = "Avarice", Url = "https://love.puni.sh/ment.json" },
			new AutoDutyPlugin { Name = "AutoRetainer", Url = "https://love.puni.sh/ment.json" },
			new AutoDutyPlugin { Name = "SkipCutscene", Url = "https://raw.githubusercontent.com/KangasZ/DalamudPluginRepository/main/plugin_repository.json" },
			new AutoDutyPlugin { Name = "AntiAfkKick", Url = "https://raw.githubusercontent.com/NightmareXIV/MyDalamudPlugins/main/pluginmaster.json" },
			new AutoDutyPlugin { Name = "Gearsetter", Url = "https://puni.sh/api/repository/vera" },
		];

		var isBossModEnabled = false;
		var isBossModRebornEnabled = false;
		foreach (var plugin in pluginsToCheck)
		{
			if (plugin.Name == "Boss Mod" && plugin.IsEnabled)
			{
				isBossModEnabled = true;
			}

			if (plugin.Name == "BossModReborn" && plugin.IsEnabled)
			{
				isBossModRebornEnabled = true;
			}

			if (isBossModEnabled && isBossModRebornEnabled)
			{
				break;
			}
		}

		foreach (var plugin in pluginsToCheck)
		{
			if (plugin.Name == "Boss Mod" && !isBossModEnabled)
			{
				continue;
			}

			var isEnabled = plugin.IsEnabled;
			var isInstalled = plugin.IsInstalled;

			Vector4 accent;
			string? advice = null;

			if (plugin.Name == "Boss Mod" && isBossModEnabled && isBossModRebornEnabled)
			{
				accent = scheme.Warning;
				advice = "Both Boss Mods cannot be installed and enabled at the same time. Please disable Boss Mod.";
			}
			else if (plugin.Name == "Boss Mod" && isBossModEnabled)
			{
				accent = scheme.Warning;
				advice = "Please use BossModReborn instead. BMR has specific integration with RSR that improves its ability to react to combat, for example gaze effects.";
			}
			else
			{
				accent = isEnabled ? scheme.Success : scheme.Error;
			}

			using var card = M3Card.Begin($"autoduty_{plugin.Name}", null,
				style: isEnabled ? M3CardStyle.Filled : M3CardStyle.Outlined, accent: accent);

			using (ImRaii.PushFont(M3.TitleMedium))
			{
				ImGui.TextUnformatted(plugin.Name);
			}

			ImGui.SameLine(0f, M3.Space2);
			_ = M3Widgets.Pill($"##autoduty_state_{plugin.Name}",
				isEnabled ? "Installed and enabled" : isInstalled ? "Installed, not enabled" : "Not installed",
				accent);

			if (!string.IsNullOrEmpty(advice))
			{
				ImGui.Dummy(new Vector2(0f, M3.Space1));
				using (ImRaii.PushColor(ImGuiCol.Text, M3.Alpha(scheme.OnSurfaceVariant, 0.92f)))
				{
					ImGui.TextWrapped(advice);
				}
			}

			// Installing from inside the plugin isn't supported on CN clients.
			if (isEnabled || CNLanguageClient)
			{
				continue;
			}

			ImGui.Dummy(new Vector2(0f, M3.Space2));

			if (DalamudReflector.HasRepo(plugin.Url) && !isInstalled)
			{
				if (M3Widgets.Button($"##autoduty_add_plugin_{plugin.Name}", "Add plugin", M3ButtonStyle.Tonal, FontAwesomeIcon.Download))
				{
					PluginLog.Information($"Attempting to add plugin: {plugin.Name} from URL: {plugin.Url}");
					var pluginName = plugin.Name;
					var pluginUrl = plugin.Url;
					_ = DalamudReflector.AddPlugin(pluginUrl, pluginName).ContinueWith(t =>
					{
						if (t.IsCompletedSuccessfully && t.Result)
						{
							PluginLog.Information($"Successfully added plugin: {pluginName} from URL: {pluginUrl}");
						}
						else
						{
							PluginLog.Error($"Failed to add plugin: {pluginName} from URL: {pluginUrl}");
						}

						DalamudReflector.ReloadPluginMasters();
					});
				}
			}
			else if (!DalamudReflector.HasRepo(plugin.Url))
			{
				if (M3Widgets.Button($"##autoduty_add_repo_{plugin.Name}", "Add repository", M3ButtonStyle.Outlined, FontAwesomeIcon.Plus))
				{
					PluginLog.Information($"Attempting to add repository: {plugin.Url}");
					DalamudReflector.AddRepo(plugin.Url, true);
					DalamudReflector.ReloadPluginMasters();
					PluginLog.Information($"Successfully added repository: {plugin.Url}");
				}
			}
		}
	}
}

public readonly struct AutoDutyPlugin
{
	public string Name { get; init; }
	public string Icon { get; init; }
	public string Url { get; init; }
	public string Features { get; init; }

	[JsonIgnore]
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

	[JsonIgnore]
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
}
