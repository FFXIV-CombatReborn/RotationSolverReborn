using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using ECommons.Reflection;
using RebornMaterial;
using RotationSolver.Integrations.IPC;
using RotationSolver.Updaters;
using System.Text;

namespace RotationSolver.UI;

public partial class MainWindow
{
	private RotationAttribute _curRotationAttribute = new("Unknown", CombatType.PvE);
	private ICustomRotation? _currentRotation;

	private double _rsrIconPressStart = -1;
	private bool _rsrIconTriggered = false;
	private const double RsrIconHoldSeconds = 1.2;

	private static readonly MainWindowTab[] _navigableTabs = BuildNavigableTabs();
	private static readonly HashSet<string> _warnedComboPlugins = new(StringComparer.OrdinalIgnoreCase);

	private static MainWindowTab[] BuildNavigableTabs()
	{
		List<MainWindowTab> tabs = [];
		foreach (var tab in Enum.GetValues<MainWindowTab>())
		{
			if (tab.GetAttribute<TabSkipAttribute>() == null)
			{
				tabs.Add(tab);
			}
		}

		return [.. tabs];
	}

	private void DrawNavigationColumn(float columnWidth, bool expanded, bool iconsOnly)
	{
		using var child = ImRaii.Child("Rotation Solver Side bar", new Vector2(columnWidth, -1f), false,
			ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
		if (!child)
		{
			return;
		}

		var wholeWidth = ImGui.GetContentRegionAvail().X;

		DrawBrandCard(wholeWidth, expanded);
		ImGui.Dummy(new Vector2(0f, M3.Space1));
		DrawRotationCard(wholeWidth, expanded, iconsOnly);
		ImGui.Dummy(new Vector2(0f, M3.Space1));

		if (expanded)
		{
			SearchingBox(wholeWidth);
		}
		else
		{
			DrawRailSearchToggle(wholeWidth);
		}

		ImGui.Dummy(new Vector2(0f, M3.Space1));

		var listHeight = MathF.Max(80f * Scale, ImGui.GetContentRegionAvail().Y - NavigationFooterHeight(iconsOnly));

		using (var list = ImRaii.Child("Rotation Solver Nav List", new Vector2(-1f, listHeight), false,
			iconsOnly ? ImGuiWindowFlags.NoScrollbar : ImGuiWindowFlags.None))
		{
			if (list)
			{
				var items = BuildNavigationItems();
				var clicked = M3Navigation.Draw("rsr_nav", items, expanded, iconsOnly);
				if (clicked != null && Enum.TryParse<MainWindowTab>(clicked, out var tab))
				{
					SetActiveTab(tab);
				}
			}
		}

		DrawNavigationFooter(wholeWidth, iconsOnly);
	}

	private static float IconStripWidth => MathF.Max(44f * Scale, (UiBuilder.IconFont.FontSize * 1.25f) + (8f * Scale));

	private static void CenterInSidebar(float width, float itemWidth)
	{
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + MathF.Max(0f, (width - itemWidth) * 0.5f));
	}

	private static void DrawSidebarIcon(float width, FontAwesomeIcon icon, Vector4 color, string tooltip)
	{
		var extent = M3Widgets.IconButtonSize;
		CenterInSidebar(width, extent);
		ImGui.Dummy(new Vector2(extent));
		M3Draw.IconCentered(ImGui.GetWindowDrawList(), icon, ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), color);
		M3Tooltip.Hovered(tooltip);
	}

	private List<M3NavItem> BuildNavigationItems()
	{
		var scheme = M3.Scheme;
		List<M3NavItem> items = [];

		foreach (var item in _navigableTabs)
		{
			var displayName = CNLanguageClient ? item.CNString() : item.ToString();

			if (item == MainWindowTab.Job && Player.Object != null)
			{
				displayName = CNLanguageClient
					? Player.ClassJob.ValueNullable?.Name.ExtractText() ?? Player.Job.ToString()
					: Player.Job.ToString();
			}
			else if (item == MainWindowTab.DutyRotation)
			{
				if (Player.Object == null || !DataCenter.IsInDuty || DataCenter.CurrentDutyRotation == null)
				{
					continue;
				}

				displayName = GetDutyRotationTabName();
			}

			var separator = item is MainWindowTab.Main
				or MainWindowTab.DutyRotation
				or MainWindowTab.Debug;

			items.Add(new M3NavItem(
				Id: item.ToString(),
				Label: displayName,
				Icon: item.Glyph(),
				Selected: _activeTab == item,
				Tooltip: item.GetDescription(),
				Accent: item == MainWindowTab.Debug ? scheme.Tertiary : scheme.Primary,
				SeparatorAfter: separator));
		}

		return items;
	}

	private static string GetDutyRotationTabName()
	{
		if (CNLanguageClient)
		{
			return true switch
			{
				var _ when DataCenter.IsInOccultCrescentOp => $"副本 - {DutyRotation.ActivePhantomJob}",
				var _ when DataCenter.InVariantDungeon => "副本 - 多变迷宫",
				var _ when DataCenter.IsInBozja => "副本 - 博兹雅",
				var _ when DataCenter.IsInMonsterHunterDuty => "副本 - 怪猎联动",
				var _ when DataCenter.Orbonne => "Duty - 瓯博讷修道院",
				_ => "Duty",
			};
		}

		return true switch
		{
			var _ when DataCenter.IsInOccultCrescentOp => $"Duty - {DutyRotation.ActivePhantomJob}",
			var _ when DataCenter.InVariantDungeon => "Duty - Variant",
			var _ when DataCenter.IsInBozja => "Duty - Bozja",
			var _ when DataCenter.IsInMonsterHunterDuty => "Duty - Monster Hunter",
			var _ when DataCenter.Orbonne => "Duty - Orbonne Monastery",
			_ => "Duty",
		};
	}

	private void DrawBrandCard(float width, bool expanded)
	{
		var scheme = M3.Scheme;
		var size = MathF.Min(36f * Scale, width);
		var height = size + (2f * M3.Space1);
		var origin = ImGui.GetCursorScreenPos();
		var max = origin + new Vector2(width, height);

		var pressed = ImGui.InvisibleButton("##rsr_brand", new Vector2(width, height));
		var hovered = ImGui.IsItemHovered();
		var active = ImGui.IsItemActive();
		var drawList = ImGui.GetWindowDrawList();

		if (hovered)
		{
			drawList.AddRectFilled(origin, max, M3.U32(scheme.OnSurface, M3.StateHover), M3.ShapeMedium);
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			M3Tooltip.Show(nameof(MainWindowTab.About));
		}

		M3Draw.FocusRing(origin, max, M3.ShapeMedium);

		var logoMin = new Vector2(expanded ? origin.X + M3.Space1 : origin.X + ((width - size) * 0.5f), origin.Y + M3.Space1);
		M3Brand.DrawLogo(drawList, logoMin, size);

		if (expanded)
		{
			var textX = logoMin.X + size + M3.Space1;
			var textWidth = MathF.Max(1f, max.X - textX - (2f * Scale));
			var lineGap = 2f * Scale;
			var titleFont = BrandTitleFont(textWidth);

			string title;
			float titleHeight;
			using (ImRaii.PushFont(titleFont))
			{
				title = M3Navigation.Truncate(M3Brand.Name, textWidth);
				titleHeight = ImGui.CalcTextSize(title).Y;
			}

			string version;
			float versionHeight;
			using (ImRaii.PushFont(M3.LabelSmall))
			{
				version = M3Navigation.Truncate($"v{M3Brand.Version}", textWidth);
				versionHeight = ImGui.CalcTextSize(version).Y;
			}

			var top = logoMin.Y + ((size - titleHeight - versionHeight - lineGap) * 0.5f);
			using (ImRaii.PushFont(titleFont))
			{
				drawList.AddText(new Vector2(textX, top), M3.U32(scheme.OnSurface), title);
			}

			using (ImRaii.PushFont(M3.LabelSmall))
			{
				drawList.AddText(new Vector2(textX, top + titleHeight + lineGap), M3.U32(scheme.OnSurfaceVariant, 0.9f), version);
			}
		}

		if (active)
		{
			var now = ImGui.GetTime();
			if (_rsrIconPressStart < 0)
			{
				_rsrIconPressStart = now;
			}
			else if (!_rsrIconTriggered && now - _rsrIconPressStart >= RsrIconHoldSeconds)
			{
				RotationSolverPlugin.OpenTicTacToe();
				_rsrIconTriggered = true;
			}
		}
		else
		{
			_rsrIconPressStart = -1;
			_rsrIconTriggered = false;
		}

		if (pressed && !_rsrIconTriggered)
		{
			SetActiveTab(MainWindowTab.About);
		}
	}

	private static ImFontPtr BrandTitleFont(float textWidth)
	{
		var largest = 16f * M3.TextScale;
		float titleWidth;
		using (ImRaii.PushFont(M3.TitleMedium))
		{
			titleWidth = ImGui.CalcTextSize(M3Brand.Name).X;
		}

		if (titleWidth <= textWidth)
		{
			return M3.TitleMedium;
		}

		return M3Fonts.GetFont(MathF.Max(9f * M3.TextScale, MathF.Floor(MathF.Round(largest) * textWidth / titleWidth)));
	}

	private void DrawRotationCard(float wholeWidth, bool expanded, bool iconsOnly)
	{
		var scheme = M3.Scheme;
		var rotation = DataCenter.CurrentRotation;

		if (rotation == null)
		{
			DrawNoRotationNotice(wholeWidth, expanded, iconsOnly);
			return;
		}

		if (_currentRotation != rotation)
		{
			var attributes = rotation.GetAttributes();
			if (attributes == null)
			{
				return;
			}

			_currentRotation = rotation;
			_curRotationAttribute = attributes;
		}

		var attribute = _curRotationAttribute ?? new RotationAttribute("Unknown", CombatType.PvE);
		var jobIcon = IconSet.GetJobPixelArtIcon();
		var rotationName = attribute.Name ?? string.Empty;

		var padding = (iconsOnly ? 3f : 6f) * M3.PaddingScale;
		var iconExtent = iconsOnly
			? MathF.Max(1f, wholeWidth - (padding * 2f))
			: MathF.Min(wholeWidth - (16f * 2), (expanded ? 56f : 40f) * 2);
		var nameGap = 4f * M3.PaddingScale;
		var nameWidth = MathF.Max(16f * Scale, wholeWidth - (padding * 2f));
		var nameSize = ImGui.CalcTextSize(rotationName);
		var height = (padding * 2f) + iconExtent + (iconsOnly ? 0f : nameGap + nameSize.Y);

		var pressed = ImGui.InvisibleButton("##rsr_rotation_card", new Vector2(wholeWidth, height));
		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var selected = _activeTab == MainWindowTab.Rotation;

		var fill = selected ? M3.Alpha(scheme.SecondaryContainer, 0.75f) : M3.Alpha(scheme.SurfaceContainer, 0.75f);
		if (hovered || held)
		{
			fill = M3.StateLayer(fill, scheme.OnSurface, hovered, held);
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		drawList.AddRectFilled(min, max, M3.U32(fill), M3.ShapeMedium);
		drawList.AddRect(min, max, M3.U32(scheme.OutlineVariant, 0.6f), M3.ShapeMedium, ImDrawFlags.None, 1f * Scale);

		var iconMin = new Vector2(min.X + ((wholeWidth - iconExtent) * 0.5f), min.Y + padding);
		if (jobIcon is { Handle.IsNull: false })
		{
			drawList.AddImage(jobIcon.Handle, iconMin, iconMin + new Vector2(iconExtent, iconExtent));
		}

		if (DutyRotation.GetPhantomJob() != DutyRotation.PhantomJob.None)
		{
			var phantomIcon = IconSet.GetOccultIcon();
			if (phantomIcon is { Handle.IsNull: false })
			{
				var badgeExtent = iconExtent * 0.55f;
				var badgeMin = iconMin + new Vector2(iconExtent - badgeExtent, iconExtent - badgeExtent);
				drawList.AddImage(phantomIcon.Handle, badgeMin, badgeMin + new Vector2(badgeExtent, badgeExtent));
			}
		}

		if (!iconsOnly)
		{
			var displayName = M3Navigation.Truncate(rotationName, nameWidth);
			var displaySize = ImGui.CalcTextSize(displayName);
			drawList.AddText(
				new Vector2(min.X + ((wholeWidth - displaySize.X) * 0.5f), iconMin.Y + iconExtent + nameGap),
				M3.U32(rotation.IsExtra() ? scheme.Tertiary : scheme.Primary, 0.98f),
				displayName);
		}

		if (hovered)
		{
			ImguiTooltips.ShowTooltip(() =>
			{
				ImGui.TextColored(rotation.GetColor(), $"{rotation.Name ?? string.Empty} ({attribute.Name ?? string.Empty})");
				attribute.Type.Draw();

				if (!string.IsNullOrEmpty(rotation.Description))
				{
					ImGui.TextWrapped(rotation.Description);
				}

				ImGui.Separator();
				ImGui.TextDisabled($"Game version: {attribute.GameVersion}");
				ImGui.TextDisabled("Right-click to pick a different rotation.");
			});
		}

		if (pressed)
		{
			SetActiveTab(MainWindowTab.Rotation);
		}

		const string popupId = "Rotation Solver Select Rotation";
		if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
		{
			ImGui.OpenPopup(popupId);
		}

		DrawRotationPicker(popupId, rotation);

		if (BMRTimeline_IPCSubscriber.IsEnabled)
		{
			ImGui.Dummy(new Vector2(0f, M3.Space1));
			const string bmrTooltip = "Boss Mod Reborn integration is active.";
			if (iconsOnly)
			{
				DrawSidebarIcon(wholeWidth, FontAwesomeIcon.Link, scheme.Success, bmrTooltip);
			}
			else
			{
				_ = M3Widgets.Pill("##bmr_pill", BmrIntegrationLabel, scheme.Success, FontAwesomeIcon.Link, bmrTooltip);
			}
		}
	}

	private static void DrawNoRotationNotice(float wholeWidth, bool expanded, bool iconsOnly)
	{
		var isDoHDoL = Player.Job is Job.CRP or Job.BSM or Job.ARM or Job.GSM
			or Job.LTW or Job.WVR or Job.ALC or Job.CUL
			or Job.MIN or Job.FSH or Job.BTN;

		if (isDoHDoL)
		{
			if (expanded)
			{
				_ = M3Widgets.Pill("##no_combat_job", "Non-combat job", M3.Scheme.OnSurfaceVariant, FontAwesomeIcon.Hammer,
					"Rotation Solver only drives combat jobs.");
			}

			return;
		}

		var text = UiString.ConfigWindow_NoRotation.GetDescription();
		if (string.IsNullOrEmpty(text))
		{
			PluginLog.Error("UiString.ConfigWindow_NoRotation.GetDescription() returned null or empty.");
			return;
		}

		if (iconsOnly)
		{
			DrawSidebarIcon(wholeWidth, FontAwesomeIcon.ExclamationTriangle, M3.Scheme.Error, text);
			return;
		}

		if (!expanded)
		{
			_ = M3Widgets.Pill("##no_rotation_pill", "!", M3.Scheme.Error, FontAwesomeIcon.ExclamationTriangle, text);
			return;
		}

		using var wrap = ImRaii.TextWrapPos(ImGui.GetCursorPosX() + wholeWidth);
		using var color = ImRaii.PushColor(ImGuiCol.Text, M3.Scheme.Error);
		ImGui.TextWrapped(text);
		ImguiTooltips.HoveredTooltip("Please update your rotations!");
	}

	internal static void DrawRotationPicker(string popupId, ICustomRotation current)
	{
		using var popup = ImRaii.Popup(popupId);
		if (!popup)
		{
			return;
		}

		var rotations = RotationUpdater.GetRotations(Player.Job, DataCenter.IsPvP ? CombatType.PvP : CombatType.PvE);

		using (ImRaii.PushFont(M3.LabelSmall))
		using (ImRaii.PushColor(ImGuiCol.Text, M3.Scheme.OnSurfaceVariant))
		{
			ImGui.TextUnformatted(UiString.ConfigWindow_Helper_SwitchRotation.GetDescription().ToUpperInvariant());
		}

		ImGui.Dummy(new Vector2(0f, M3.Space1));

		foreach (var r in rotations)
		{
			var attributes = r.GetAttributes();
			if (attributes == null)
			{
				continue;
			}

			if (M3Widgets.MenuItem($"##rotation_{attributes.Name}_{r.GetType().FullName}", attributes.Name,
				ReferenceEquals(r, current)))
			{
				if (DataCenter.IsPvP)
				{
					Service.Config.PvPRotationChoice = r.GetType().FullName;
				}
				else
				{
					Service.Config.RotationChoice = r.GetType().FullName;
				}

				Service.Config.Save();
				RotationUpdater.ChangeRotation(r);
				ImGui.CloseCurrentPopup();
			}

			if (ImGui.IsItemHovered() && !string.IsNullOrEmpty(attributes.Description))
			{
				ImguiTooltips.ShowTooltip(attributes.Description);
			}
		}
	}

	private const string DiagnosticsLabel = "Copy diagnostics";
	private const string DiagnosticsMediumLabel = "Diagnostics";
	private const string DiagnosticsShortLabel = "Diag";
	private const string BmrIntegrationLabel = "BMR integration";

	private static float NavigationColumnWidth(float available)
	{
		var width = MathF.Max(M3Widgets.PillSize(BmrIntegrationLabel, FontAwesomeIcon.Link).X, M3Navigation.DrawerBreakpoint * Scale);
		return MathF.Min(MathF.Ceiling(width), available * 0.5f);
	}

	private static string DiagnosticsLabelFor(float width)
	{
		if (OtherConfiguration.RotationSolverRecord.TicTacToeWinStar)
		{
			width -= M3.Space1 + M3Widgets.PillSize(string.Empty, FontAwesomeIcon.Star).X;
		}

		if (M3Widgets.PillSize(DiagnosticsLabel, FontAwesomeIcon.Cube).X <= width)
		{
			return DiagnosticsLabel;
		}

		return M3Widgets.PillSize(DiagnosticsMediumLabel, FontAwesomeIcon.Cube).X <= width
			? DiagnosticsMediumLabel
			: DiagnosticsShortLabel;
	}

	private static float NavigationFooterHeight(bool iconsOnly)
	{
		var spacing = ImGui.GetStyle().ItemSpacing.Y;
		if (!iconsOnly)
		{
			return M3Widgets.PillSize(string.Empty).Y + spacing + (2f * Scale);
		}

		var rows = OtherConfiguration.RotationSolverRecord.TicTacToeWinStar ? 2 : 1;
		return ((M3Widgets.IconButtonSize + spacing) * rows) + (2f * Scale);
	}

	private void DrawNavigationFooter(float wholeWidth, bool iconsOnly)
	{
		var tick = Environment.TickCount64;
		if (_diagInfoTick == long.MinValue || tick - _diagInfoTick >= DiagInfoRefreshMs)
		{
			_diagInfoText = BuildDiagnosticInfo(out _diagInfoAnyCrash);
			_diagInfoTick = tick;
		}

		var scheme = M3.Scheme;
		Vector4 accent;
		if (_diagInfoAnyCrash)
		{
			var pulse = M3.ReducedMotion ? 0f : (MathF.Sin((float)ImGui.GetTime() * 4f) + 1f) * 0.5f;
			accent = M3ColorMath.Mix(scheme.Error, scheme.OnErrorContainer, pulse * 0.5f);
		}
		else
		{
			accent = scheme.OnSurfaceVariant;
		}

		bool copy;
		if (iconsOnly)
		{
			CenterInSidebar(wholeWidth, M3Widgets.IconButtonSize);
			copy = M3Widgets.IconButton("##diag_icon", FontAwesomeIcon.Cube, $"{DiagnosticsLabel}\n \n{_diagInfoText}", tint: accent);
		}
		else
		{
			copy = M3Widgets.Pill("##diag_pill", DiagnosticsLabelFor(wholeWidth), accent, FontAwesomeIcon.Cube, _diagInfoText, interactive: true);
		}

		if (copy)
		{
			ImGui.SetClipboardText(BuildDiagnosticInfo(out _));
			M3Snackbar.Show(CNLanguageClient ? "诊断信息已复制到剪贴板" : "Diagnostic info copied to clipboard");
		}

		if (OtherConfiguration.RotationSolverRecord.TicTacToeWinStar == true)
		{
			const string trophyTooltip = "Tic-tac-toe winner!";
			if (iconsOnly)
			{
				DrawSidebarIcon(wholeWidth, FontAwesomeIcon.Star, ImGuiColors.ParsedGold, trophyTooltip);
			}
			else
			{
				ImGui.SameLine(0f, M3.Space1);
				_ = M3Widgets.Pill("##trophy_pill", string.Empty, ImGuiColors.ParsedGold, FontAwesomeIcon.Star, trophyTooltip);
			}
		}
	}

	private string _diagInfoText = string.Empty;
	private bool _diagInfoAnyCrash;
	private long _diagInfoTick = long.MinValue;
	private const long DiagInfoRefreshMs = 1000;

	private string BuildDiagnosticInfo(out bool anyCrash)
	{
		StringBuilder diagInfo = new();

		if (_cachedDiagInfo == null && DalamudReflector.TryGetDalamudStartInfo(out var startinfo, Svc.PluginInterface))
		{
			_cachedDiagInfo = new DiagInfo(startinfo);
		}

		if (_cachedDiagInfo == null)
		{
			_ = diagInfo.AppendLine($"Rotation Solver Reborn v{typeof(MainWindow).Assembly.GetName().Version?.ToString() ?? "?.?.?"}");
			_ = diagInfo.AppendLine("Failed to get Dalamud start info.");
		}
		else
		{
			_ = diagInfo.AppendLine($"OS Type: {_cachedDiagInfo.Platform}");
			_ = diagInfo.AppendLine($"FFXIV Version: {_cachedDiagInfo.GameVersion}");
			_ = diagInfo.AppendLine($"Dalamud Version: {Svc.PluginInterface.GetDalamudVersion().Version.ToString()}");
			_ = diagInfo.AppendLine($"Rotation Solver Reborn v{_cachedDiagInfo.RSRVersion}");
			_ = diagInfo.AppendLine($"Dalamud Staging: {DataCenter.DalamudStagingEnabled}");
			_ = diagInfo.AppendLine($"Game Language: {_cachedDiagInfo.Language}");
			_ = diagInfo.AppendLine($"Update Frequency: {Service.Config.MinUpdatingTime}");
			_ = diagInfo.AppendLine($"Intercept: {Service.Config.InterceptAction3}");
			_ = diagInfo.AppendLine($"Player Level: {DataCenter.PlayerSyncedLevel()}");
			_ = diagInfo.AppendLine($"Rotation Name: {_curRotationAttribute?.Name ?? string.Empty}");
			_ = diagInfo.AppendLine($"Player Job: {Player.Job}");
			_ = diagInfo.AppendLine($"AutoFaceTargetOnActionSetting: {DataCenter.AutoFaceTargetOnActionSetting()}");
			var moveModeValue = DataCenter.MoveModeSetting();
			var moveModeText = moveModeValue switch
			{
				0 => "Standard",
				1 => "Legacy",
				_ => moveModeValue.ToString()
			};
			_ = diagInfo.AppendLine($"MoveModeSetting: {moveModeText}");
		}

		var lastFrame = ActionTracer.LastFrameSummary;
		if (!string.IsNullOrEmpty(lastFrame))
		{
			_ = diagInfo.AppendLine();
			_ = diagInfo.AppendLine("Last Action Tracer Frame:");
			_ = diagInfo.Append(lastFrame);
		}

		anyCrash = false;
		_ = diagInfo.AppendLine("\nPlugins:");
		foreach (var item in PluginCompatibility.IncompatiblePlugins)
		{
			if (item.IsEnabled)
			{
				var name = item.Name ?? "Unnamed Incompatible Plugin";

				if (item.Type.HasFlag(CompatibleType.Crash))
				{
					anyCrash = true;
				}

				if (!string.IsNullOrEmpty(item.Name) && item.Name.Contains("Combo") && _warnedComboPlugins.Add(item.Name))
				{
					BasicWarningHelper.AddSystemWarning($"Disable {item.Name}");
				}

				_ = diagInfo.AppendLine($"{name}");
			}
		}

		return diagInfo.ToString();
	}
}
