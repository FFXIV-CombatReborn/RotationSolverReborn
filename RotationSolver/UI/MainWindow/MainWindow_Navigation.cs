using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using ECommons.Reflection;
using RotationSolver.Basic.Configuration;
using RotationSolver.Basic.Rotations.Duties;
using RotationSolver.Data;
using RotationSolver.Helpers;
using RotationSolver.IPC;
using RotationSolver.UI.Material;
using RotationSolver.Updaters;
using System.Text;

namespace RotationSolver.UI;

public partial class MainWindow
{
	private RotationAttribute _curRotationAttribute = new("Unknown", CombatType.PvE);
	private ICustomRotation? _currentRotation;

	private const string LogoResource = "RotationSolver.Images.Logo.png";

	// Easter egg: holding the RSR icon opens tic-tac-toe.
	private double _rsrIconPressStart = -1;
	private bool _rsrIconTriggered = false;
	private const double RsrIconHoldSeconds = 1.2;

	private static readonly MainWindowTab[] _navigableTabs = BuildNavigableTabs();

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

	private void DrawNavigationColumn(float columnWidth)
	{
		using var child = ImRaii.Child("Rotation Solver Side bar", new Vector2(columnWidth, -1f), false, ImGuiWindowFlags.NoScrollbar);
		if (!child)
		{
			return;
		}

		var wholeWidth = ImGui.GetContentRegionAvail().X;
		var expanded = wholeWidth >= M3Navigation.DrawerBreakpoint * Scale;

		DrawBrandBlock(wholeWidth, expanded);

		if (expanded)
		{
			ImGui.Dummy(new Vector2(0f, M3.Space1));
			ImGui.SetNextItemWidth(wholeWidth);
			SearchingBox(wholeWidth);
		}

		ImGui.Dummy(new Vector2(0f, M3.Space2));

		var footerHeight = M3Widgets.PillSize(string.Empty).Y + (10f * Scale);
		var listHeight = MathF.Max(80f * Scale, ImGui.GetContentRegionAvail().Y - footerHeight);

		using (var list = ImRaii.Child("Rotation Solver Nav List", new Vector2(-1f, listHeight), false))
		{
			if (list)
			{
				var items = BuildNavigationItems(expanded);
				var clicked = M3Navigation.Draw("rsr_nav", items, expanded);
				if (clicked != null && Enum.TryParse<MainWindowTab>(clicked, out var tab))
				{
					SetActiveTab(tab);
				}
			}
		}

		DrawNavigationFooter(expanded);
	}

	private List<M3NavItem> BuildNavigationItems(bool expanded)
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

	private void DrawBrandBlock(float wholeWidth, bool expanded)
	{
		var scheme = M3.Scheme;
		var logoSize = expanded
			? MathF.Min(wholeWidth - (8f * 2), 72f * 2)
			: MathF.Min(wholeWidth - (8f * 2), 44f * 2);

		var origin = ImGui.GetCursorScreenPos();

		ImGui.SetCursorScreenPos(new Vector2(origin.X + ((wholeWidth - logoSize) * 0.5f), origin.Y));
		var pressed = ImGui.InvisibleButton("##rsr_brand", new Vector2(logoSize, logoSize));
		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var logoMin = ImGui.GetItemRectMin();
		var logoMax = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();

		var logo = GetLogoTexture();
		if (logo?.Handle != null)
		{
			drawList.AddImageRounded(logo.Handle, logoMin, logoMax, Vector2.Zero, Vector2.One,
				M3.U32(Vector4.One), M3.ShapeSmall);
		}
		else
		{
			M3Draw.IconCentered(drawList, FontAwesomeIcon.Fire, logoMin, logoMax, scheme.Primary);
		}

		if (hovered)
		{
			drawList.AddRectFilled(logoMin, logoMax, M3.U32(scheme.OnSurface, M3.StateHover), M3.ShapeSmall);
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		if (ImGui.IsItemActive())
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

		ImGui.SetCursorScreenPos(new Vector2(origin.X, logoMax.Y));
		ImGui.Dummy(new Vector2(wholeWidth, M3.Space1));

		DrawRotationCard(wholeWidth, expanded);
	}

	private void DrawRotationCard(float wholeWidth, bool expanded)
	{
		var scheme = M3.Scheme;
		var rotation = DataCenter.CurrentRotation;

		if (rotation == null)
		{
			DrawNoRotationNotice(wholeWidth, expanded);
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

		var iconExtent = MathF.Min(wholeWidth - (16f * 2), (expanded ? 56f : 40f) * 2);
		var padding = 10f * Scale;
		var nameGap = 6f * Scale;
		var nameWidth = MathF.Max(16f * Scale, wholeWidth - (padding * 2f));
		var nameSize = ImGui.CalcTextSize(rotationName);
		var height = (padding * 2f) + iconExtent + nameGap + nameSize.Y;

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
		if (jobIcon?.Handle != null)
		{
			drawList.AddImage(jobIcon.Handle, iconMin, iconMin + new Vector2(iconExtent, iconExtent));
		}

		if (DutyRotation.GetPhantomJob() != DutyRotation.PhantomJob.None)
		{
			var phantomIcon = IconSet.GetOccultIcon();
			if (phantomIcon?.Handle != null)
			{
				var badgeExtent = iconExtent * 0.55f;
				var badgeMin = iconMin + new Vector2(iconExtent - badgeExtent, iconExtent - badgeExtent);
				drawList.AddImage(phantomIcon.Handle, badgeMin, badgeMin + new Vector2(badgeExtent, badgeExtent));
			}
		}

		var displayName = M3Navigation.Truncate(rotationName, nameWidth);
		var displaySize = ImGui.CalcTextSize(displayName);
		drawList.AddText(
			new Vector2(min.X + ((wholeWidth - displaySize.X) * 0.5f), iconMin.Y + iconExtent + nameGap),
			M3.U32(rotation.IsExtra() ? scheme.Tertiary : scheme.Primary, 0.98f),
			displayName);

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
			_ = M3Widgets.Pill("##bmr_pill", BmrIntegrationLabel, scheme.Success, FontAwesomeIcon.Link,
				"Boss Mod Reborn integration is active.");
		}
	}

	private static void DrawNoRotationNotice(float wholeWidth, bool expanded)
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

	// Fetched every frame on purpose; Dalamud caches the texture.
	internal static IDalamudTextureWrap? GetLogoTexture()
	{
		return Svc.Texture.GetFromManifestResource(typeof(MainWindow).Assembly, LogoResource)
			.TryGetWrap(out var logo, out _) ? logo : null;
	}

	private const string DiagnosticsLabel = "Copy diagnostics";
	private const string DiagnosticsShortLabel = "Diag";
	private const string BmrIntegrationLabel = "BMR integration";

	private static float NavigationColumnMinWidth()
	{
		var footer = M3Widgets.PillSize(DiagnosticsLabel, FontAwesomeIcon.Cube).X;

		if (OtherConfiguration.RotationSolverRecord.TicTacToeWinStar == true)
		{
			footer += M3.Space1 + M3Widgets.PillSize(string.Empty, FontAwesomeIcon.Star).X;
		}

		var widest = MathF.Max(footer, M3Widgets.PillSize(BmrIntegrationLabel, FontAwesomeIcon.Link).X);
		widest = MathF.Max(widest, 160f * Scale);

		return widest + (4f * Scale);
	}

	private void DrawNavigationFooter(bool expanded)
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
			var pulse = (MathF.Sin((float)ImGui.GetTime() * 4f) + 1f) * 0.5f;
			accent = M3ColorMath.Mix(scheme.Error, scheme.OnErrorContainer, pulse * 0.5f);
		}
		else
		{
			accent = scheme.OnSurfaceVariant;
		}

		var label = expanded ? DiagnosticsLabel : DiagnosticsShortLabel;
		if (M3Widgets.Pill("##diag_pill", label, accent, FontAwesomeIcon.Cube, _diagInfoText, interactive: true))
		{
			ImGui.SetClipboardText(BuildDiagnosticInfo(out _));
			M3Snackbar.Show(CNLanguageClient ? "诊断信息已复制到剪贴板" : "Diagnostic info copied to clipboard");
		}

		if (OtherConfiguration.RotationSolverRecord.TicTacToeWinStar == true)
		{
			ImGui.SameLine(0f, M3.Space1);
			_ = M3Widgets.Pill("##trophy_pill", string.Empty, ImGuiColors.ParsedGold, FontAwesomeIcon.Star,
				"Tic-tac-toe winner!");
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

				if (!string.IsNullOrEmpty(item.Name) && item.Name.Contains("Combo"))
				{
					BasicWarningHelper.AddSystemWarning($"Disable {item.Name}");
				}

				_ = diagInfo.AppendLine($"{name}");
			}
		}

		return diagInfo.ToString();
	}
}
