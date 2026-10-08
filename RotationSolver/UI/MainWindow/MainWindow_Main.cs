using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;
using ECommons.Logging;
using RebornMaterial;
using RotationSolver.Basic.Configuration;
using RotationSolver.Data;
using System.Diagnostics;

namespace RotationSolver.UI;

public partial class MainWindow
{
	private static void DrawAbout()
	{
		var scheme = M3.Scheme;

		using (var hero = M3Card.Begin("about_hero", null, style: M3CardStyle.Elevated))
		{
			using (ImRaii.PushFont(M3.TitleLarge))
			using (ImRaii.PushColor(ImGuiCol.Text, scheme.Primary))
			{
				ImGui.TextWrapped(UiString.ConfigWindow_About_Punchline.GetDescription());
			}

			ImGui.Dummy(new Vector2(0f, M3.Space2));

			using (ImRaii.PushColor(ImGuiCol.Text, M3.Alpha(scheme.OnSurfaceVariant, 0.95f)))
			{
				ImGui.TextWrapped(UiString.ConfigWindow_About_Description.GetDescription());
			}

			ImGui.Dummy(new Vector2(0f, M3.Space3));

			_ = M3Widgets.Banner("##about_warning", UiString.ConfigWindow_About_Warning.GetDescription(),
				M3Severity.Warning, FontAwesomeIcon.ExclamationTriangle);

			ImGui.Dummy(new Vector2(0f, M3.Space3));

			if (M3Widgets.Button("##about_tutorial",
				CNLanguageClient ? "打开初次启动向导" : "Open first start tutorial",
				M3ButtonStyle.Filled, FontAwesomeIcon.GraduationCap))
			{
				RotationSolverPlugin.OpenFirstStartTutorial();
			}

			ImGui.SameLine(0f, M3.Space2);

			if (M3Widgets.Button("##about_changelog", CNLanguageClient ? "更新说明" : "What's new",
				M3ButtonStyle.Tonal, FontAwesomeIcon.Newspaper))
			{
				RotationSolverPlugin.OpenChangelog();
			}

			ImGui.SameLine(0f, M3.Space2);

			if (M3Widgets.Button("##about_kofi", "Ko-fi", M3ButtonStyle.Tonal, FontAwesomeIcon.MugHot))
			{
				OpenLinkSafely("https://ko-fi.com/ltscombatreborn");
			}

			ImGui.SameLine(0f, M3.Space2);

			if (M3Widgets.Button("##about_discord", "Discord", M3ButtonStyle.Outlined, FontAwesomeIcon.Comments))
			{
				OpenLinkSafely("https://discord.gg/r9V4RHYt6v");
			}

			var clickingCount = OtherConfiguration.RotationSolverRecord.ClickingCount;
			if (clickingCount > 0)
			{
				var countStr = UiString.ConfigWindow_About_ClickingCount.GetDescription();
				if (!string.IsNullOrEmpty(countStr))
				{
					ImGui.Dummy(new Vector2(0f, M3.Space3));
					_ = M3Widgets.Pill("##about_clicks", string.Format(countStr, clickingCount),
						scheme.Tertiary, FontAwesomeIcon.MousePointer);
				}
			}
		}

		_aboutHeaders.Draw();
	}

	internal static void OpenMacroList()
	{
		_aboutHeaders.OpenHeaderByTitle(UiString.ConfigWindow_About_Macros.GetDescription());
	}

	private static readonly CollapsingHeaderGroup _aboutHeaders = BuildHeaderGroup(
		new Dictionary<Func<string>, Action>
		{
			{ () => UiString.ConfigWindow_About_ThanksToSupporters.GetDescription(), DrawThanksToSupporters },
			{ () => UiString.ConfigWindow_About_Macros.GetDescription(), DrawAboutMacros },
			{ () => UiString.ConfigWindow_About_SettingMacros.GetDescription(), DrawAboutSettingsCommands },
			{ () => UiString.ConfigWindow_About_Compatibility.GetDescription(), DrawAboutCompatibility },
			{ () => UiString.ConfigWindow_About_Links.GetDescription(), DrawAboutLinks },
		},
		(UiString.ConfigWindow_About_ThanksToSupporters, FontAwesomeIcon.Heart),
		(UiString.ConfigWindow_About_Macros, FontAwesomeIcon.Terminal),
		(UiString.ConfigWindow_About_SettingMacros, FontAwesomeIcon.Keyboard),
		(UiString.ConfigWindow_About_Compatibility, FontAwesomeIcon.PuzzlePiece),
		(UiString.ConfigWindow_About_Links, FontAwesomeIcon.Link));

	private static void DrawThanksToSupporters()
	{
		if (M3Widgets.Button("##join_supporters", "Join this list", M3ButtonStyle.Tonal, FontAwesomeIcon.MugHot))
		{
			OpenLinkSafely("https://ko-fi.com/ltscombatreborn");
		}

		if (_supporters == null || _supporters.Length == 0)
		{
			ImGui.Dummy(new Vector2(0f, M3.Space2));
			ImGui.TextWrapped("No supporters to display yet. Thank you for checking!");
			return;
		}

		M3Widgets.SectionLabel(
			$"Special thanks to the {_supporters.Length} supporters, including those not listed here",
			M3.Scheme.Tertiary);

		var names = new List<string>(_supporters);
		names.Sort(StringComparer.OrdinalIgnoreCase);

		var startX = ImGui.GetCursorPosX();
		var available = ImGui.GetContentRegionAvail().X;
		var usedWidth = 0f;

		for (var i = 0; i < names.Count; i++)
		{
			var name = names[i];
			var chipWidth = M3Widgets.ChipWidth(name) + M3.Space1;

			if (usedWidth > 0f && usedWidth + chipWidth > available)
			{
				ImGui.NewLine();
				ImGui.SetCursorPosX(startX);
				usedWidth = 0f;
			}
			else if (usedWidth > 0f)
			{
				ImGui.SameLine(0f, M3.Space1);
			}

			if (M3Widgets.Chip($"##supporter_{i}", name, false, FontAwesomeIcon.None, "Click to copy",
				M3.Scheme.Tertiary))
			{
				ImGui.SetClipboardText(name);
			}

			usedWidth += chipWidth;
		}

		ImGui.NewLine();
	}

	private static void DrawAboutMacros()
	{
		using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 5f));

		M3Widgets.SectionLabel("State");
		DisplayCommandHelp(StateCommandType.Auto);
		DisplayCommandHelp(StateCommandType.Manual);
		DisplayCommandHelp(StateCommandType.Off);
		DisplayCommandHelp(OtherCommandType.Cycle);
		DisplayCommandHelp(StateCommandType.TargetOnly);

		M3Widgets.SectionLabel("Windows");
		DisplayCommandHelp(OtherCommandType.NextAction);
		DisplayCommandHelp(OtherCommandType.Control);
		DisplayCommandHelp(OtherCommandType.Changelog);

		M3Widgets.SectionLabel("Special actions");
		DisplayCommandHelp(SpecialCommandType.EndSpecial);
		DisplayCommandHelp(SpecialCommandType.HealArea);
		DisplayCommandHelp(SpecialCommandType.HealSingle);
		DisplayCommandHelp(SpecialCommandType.DefenseArea);
		DisplayCommandHelp(SpecialCommandType.DefenseSingle);
		DisplayCommandHelp(SpecialCommandType.MoveForward);
		DisplayCommandHelp(SpecialCommandType.MoveBack);
		DisplayCommandHelp(SpecialCommandType.Speed);
		DisplayCommandHelp(SpecialCommandType.DispelStancePositional);
		DisplayCommandHelp(SpecialCommandType.RaiseShirk);
		DisplayCommandHelp(SpecialCommandType.AntiKnockback);
		DisplayCommandHelp(SpecialCommandType.Burst);
		DisplayCommandHelp(SpecialCommandType.NoCasting);
	}

	private static void DrawAboutSettingsCommands()
	{
		DrawPageIntro("These commands can be used to open or change plugin settings directly from chat or macros.");
		DrawPageIntro("Right-clicking any action, setting or toggle pops up the macro associated with it.");
	}

	private static void DisplayCommandHelp<T>(T commandType) where T : Enum
	{
		commandType.DisplayCommandHelp(getHelp: Data.EnumExtensions.GetDescription);
	}

	private static void DrawAboutCompatibility()
	{
		DrawPageIntro(UiString.ConfigWindow_About_Compatibility_Description.GetDescription());

		var scheme = M3.Scheme;
		var iconSize = 40 * Scale;

		foreach (var item in PluginCompatibility.IncompatiblePlugins)
		{
			var severity = item.Type.HasFlag(CompatibleType.Crash) || item.Type.HasFlag(CompatibleType.Broken)
				? M3Severity.Error
				: M3Severity.Warning;
			var accent = M3.Severity(severity);

			using var card = M3Card.Begin($"compat_{item.Name}", null,
				style: item.IsEnabled ? M3CardStyle.Filled : M3CardStyle.Outlined, accent: accent);

			var iconUrl = string.IsNullOrEmpty(item.Icon)
				? "https://raw.githubusercontent.com/goatcorp/DalamudAssets/master/UIRes/defaultIcon.png"
				: item.Icon;

			if (IconSet.GetTexture(iconUrl, out var texture)
				&& ImGuiHelper.NoPaddingNoColorImageButton(texture, Vector2.One * iconSize, item.Name ?? "plugin"))
			{
				OpenLinkSafely(item.Url);
			}

			ImguiTooltips.HoveredTooltip($"Open {item.Name}");

			ImGui.SameLine(0f, M3.Space3);
			ImGui.BeginGroup();

			using (ImRaii.PushFont(M3.TitleMedium))
			{
				ImGui.TextUnformatted(item.Name ?? "Unnamed plugin");
			}

			ImGui.SameLine(0f, M3.Space2);
			_ = M3Widgets.Pill($"##compat_state_{item.Name}",
				item.IsEnabled ? "Enabled" : "Not loaded",
				item.IsEnabled ? accent : scheme.OnSurfaceVariant);

			DisplayPluginType(item.Type);

			if (!string.IsNullOrEmpty(item.Features))
			{
				using (ImRaii.PushColor(ImGuiCol.Text, M3.Alpha(scheme.OnSurfaceVariant, 0.9f)))
				{
					ImGui.TextWrapped(item.Features);
				}
			}

			ImGui.EndGroup();
		}
	}

	private static void DisplayPluginType(CompatibleType type)
	{
		var scheme = M3.Scheme;
		var first = true;

		void Badge(CompatibleType flag, Vector4 accent, UiString tooltip)
		{
			if (!type.HasFlag(flag))
			{
				return;
			}

			if (!first)
			{
				ImGui.SameLine(0f, M3.Space1);
			}

			first = false;
			_ = M3Widgets.Pill($"##compat_type_{flag}_{type}", flag.GetDescription().Replace('_', ' '), accent,
				FontAwesomeIcon.None, tooltip.GetDescription());
		}

		Badge(CompatibleType.Skill_Usage, scheme.Warning, UiString.ConfigWindow_About_Compatibility_Mistake);
		Badge(CompatibleType.Skill_Selection, scheme.Tertiary, UiString.ConfigWindow_About_Compatibility_Mislead);
		Badge(CompatibleType.Crash, scheme.Error, UiString.ConfigWindow_About_Compatibility_Crash);
		Badge(CompatibleType.Broken, scheme.Error, UiString.ConfigWindow_About_Compatibility_Crash);
	}

	private static void DrawAboutLinks()
	{
		if (M3Widgets.Button("##links_config_folder", UiString.ConfigWindow_About_OpenConfigFolder.GetDescription(),
			M3ButtonStyle.Tonal, FontAwesomeIcon.FolderOpen))
		{
			try
			{
				_ = Process.Start("explorer.exe", Svc.PluginInterface.ConfigDirectory.FullName);
			}
			catch (Exception ex)
			{
				PluginLog.Warning($"Failed to open config folder: {ex.Message}");
			}
		}

		ImGui.SameLine(0f, M3.Space2);

		if (M3Widgets.Button("##links_github", "GitHub", M3ButtonStyle.Outlined, FontAwesomeIcon.Code))
		{
			OpenLinkSafely($"https://GitHub.com/{Service.USERNAME}/{Service.REPO}");
		}

		ImGui.SameLine(0f, M3.Space2);

		if (M3Widgets.Button("##links_discord", "Discord", M3ButtonStyle.Outlined, FontAwesomeIcon.Comments))
		{
			OpenLinkSafely("https://discord.gg/r9V4RHYt6v");
		}

		ImGui.SameLine(0f, M3.Space2);

		if (M3Widgets.Button("##links_kofi", "Ko-fi", M3ButtonStyle.Outlined, FontAwesomeIcon.MugHot))
		{
			OpenLinkSafely("https://ko-fi.com/ltscombatreborn");
		}
	}
}
