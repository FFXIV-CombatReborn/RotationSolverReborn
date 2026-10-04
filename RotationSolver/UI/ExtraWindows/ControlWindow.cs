using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;
using RotationSolver.Basic.Configuration;
using RotationSolver.Commands;
using RotationSolver.Data;
using RotationSolver.UI.Material;
using RotationSolver.Updaters;

namespace RotationSolver.UI.ExtraWindows;

internal class FullControlWindow : FullCtrlWindow
{
	private const string StatusPaneId = "##rsr_control_status";
	private const string SpecialsPaneId = "##rsr_control_specials";
	private const string TargetingMenuId = "##rsr_targeting_menu";

	private const int MaxTileColumns = 5;

	private readonly record struct SpecialTile(
		SpecialCommandType Command,
		string Label,
		FontAwesomeIcon Glyph,
		Func<M3Scheme, Vector4> Accent,
		Func<ICustomRotation, IAction?>? Gcd = null,
		Func<ICustomRotation, IAction?>? Ability = null);

	private readonly record struct SpecialGroup(string Title, SpecialTile[] Tiles);

	private static readonly SpecialGroup[] SpecialGroups =
	[
		new("Healing and mitigation",
		[
			new(SpecialCommandType.HealArea, "Heal AoE", FontAwesomeIcon.HandHoldingMedical, s => s.Success,
				r => r.ActionHealAreaGCD, r => r.ActionHealAreaAbility),
			new(SpecialCommandType.HealSingle, "Heal ST", FontAwesomeIcon.Heart, s => s.Success,
				r => r.ActionHealSingleGCD, r => r.ActionHealSingleAbility),
			new(SpecialCommandType.DefenseArea, "Def AoE", FontAwesomeIcon.ShieldAlt, s => s.Info,
				r => r.ActionDefenseAreaGCD, r => r.ActionDefenseAreaAbility),
			new(SpecialCommandType.DefenseSingle, "Def ST", FontAwesomeIcon.UserShield, s => s.Info,
				r => r.ActionDefenseSingleGCD, r => r.ActionDefenseSingleAbility),
		]),
		new("Movement",
		[
			new(SpecialCommandType.MoveForward, "Forward", FontAwesomeIcon.AngleDoubleUp, s => s.Warning,
				r => r.ActionMoveForwardGCD, r => r.ActionMoveForwardAbility),
			new(SpecialCommandType.MoveBack, "Back", FontAwesomeIcon.AngleDoubleDown, s => s.Warning,
				Ability: r => r.ActionMoveBackAbility),
			new(SpecialCommandType.AntiKnockback, "Anti-KB", FontAwesomeIcon.Anchor, s => s.Warning,
				Ability: r => r.ActionAntiKnockbackAbility),
			new(SpecialCommandType.Speed, "Speed", FontAwesomeIcon.Running, s => s.Warning,
				Ability: r => r.ActionSpeedAbility),
		]),
		new("Rotation",
		[
			new(SpecialCommandType.DispelStancePositional, "Dispel", FontAwesomeIcon.Magic, s => s.Tertiary,
				r => r.ActionDispelStancePositionalGCD, r => r.ActionDispelStancePositionalAbility),
			new(SpecialCommandType.RaiseShirk, "Raise", FontAwesomeIcon.Ankh, s => s.Primary,
				r => r.ActionRaiseShirkGCD, r => r.ActionRaiseShirkAbility),
			new(SpecialCommandType.NoCasting, "No cast", FontAwesomeIcon.Ban, s => s.Error),
			new(SpecialCommandType.Burst, "Burst", FontAwesomeIcon.FireAlt, s => s.Primary),
			new(SpecialCommandType.EndSpecial, "End", FontAwesomeIcon.Stop, s => s.OnSurfaceVariant),
		]),
	];

	private static readonly M3Segment[] AoeSegments =
	[
		new("Off", Tooltip: "Do not use any AoE actions."),
		new("Cleave", Tooltip: "Use only single-target AoE actions."),
		new("Full", Tooltip: "Use all available AoE actions."),
	];

	private static readonly Vector2 DefaultSize = new(700f, 380f);
	private const float NextGcdBaseSize = 40f;
	private const float NextAbilityBaseSize = 30f;
	private const float SpecialGcdBaseSize = 40f;
	private const float SpecialAbilityBaseSize = 30f;

	private static readonly M3WindowAction HideSpecialsAction = new("##rsr_specials", FontAwesomeIcon.CompressAlt, "Hide the special buttons");
	private static readonly M3WindowAction ShowSpecialsAction = new("##rsr_specials", FontAwesomeIcon.ExpandAlt, "Show the special buttons");
	private static readonly M3WindowAction UnlockedAction = new("##rsr_lock", FontAwesomeIcon.LockOpen, "Lock the window in place");
	private static readonly M3WindowAction LockedAction = new("##rsr_lock", FontAwesomeIcon.Lock, "Locked in place. Click to allow moving and resizing.");
	private static readonly M3WindowAction SettingsAction = new("##rsr_settings", FontAwesomeIcon.Cog, "Open the settings");

	private static float PanePadding => M3.Space3;
	private static float PaneGap => M3.Space2;
	private static Vector2 TilePadding => new Vector2(6f, 6f) * M3.Scale;
	private static float TileIconGap => 3f * M3.Scale;
	private static float TileLabelGap => 4f * M3.Scale;

	internal static float NextGcdSize => NextGcdBaseSize * Service.Config.ControlWindowNextSizeRatio;
	internal static float NextAbilitySize => NextAbilityBaseSize * Service.Config.ControlWindowNextSizeRatio;

	private static float SpecialGcdSize => SpecialGcdBaseSize * Service.Config.ControlWindowSpecialsScale;
	private static float SpecialAbilitySize => SpecialAbilityBaseSize * Service.Config.ControlWindowSpecialsScale;

	private static M3WindowBrand Brand => new(MainWindow.GetLogoTexture(), "RSR Control");

	private readonly M3WindowAction[] _actions = new M3WindowAction[3];
	private readonly M3WindowFold _fold = new();

	private M3Style.Scope _theme;

	private float _contentHeight;
	private float _minimumWidth;
	private Vector2 _openSize;
	private float _sideBySideWidth;
	private bool? _specialsShown;
	private float _wideWidth;
	private float _pendingWidth;

	public FullControlWindow()
		: base(nameof(FullControlWindow))
	{
	}

	public override void OnOpen()
	{
		DataCenter.DrawingActions = true;
		base.OnOpen();
	}

	public override void OnClose()
	{
		DataCenter.DrawingActions = false;

		if (!Service.Config.ShowControlWindow)
		{
			_fold.Reset();
		}

		base.OnClose();
	}

	public override void PreDraw()
	{
		_theme = M3Style.Push(compact: true);

		base.PreDraw();
		Flags |= ImGuiWindowFlags.NoTitleBar;

		TrackSpecials();

		if (_fold.Prepare(this, _actions.Length, Brand))
		{
			Position = null;
			Size = null;
			SizeConstraints = null;
		}

		if (_fold.IsActive)
		{
			return;
		}

		if (_pendingWidth > 0f && _openSize.Y > 0f)
		{
			ImGui.SetNextWindowSize(new Vector2(_pendingWidth, _openSize.Y), ImGuiCond.Always);
		}
		else
		{
			ImGui.SetNextWindowSize(DefaultSize * ImGuiHelpers.GlobalScale, ImGuiCond.FirstUseEver);
		}

		_pendingWidth = 0f;

		if (_contentHeight > 0f)
		{
			var maximumWidth = Service.Config.ShowControlWindowSpecials ? float.MaxValue : _minimumWidth;
			ImGui.SetNextWindowSizeConstraints(
				new Vector2(_minimumWidth, _contentHeight),
				new Vector2(maximumWidth, _contentHeight));
		}
	}

	public override void PostDraw()
	{
		_fold.PopStyle();
		base.PostDraw();
		_theme.Dispose();
		_theme = default;
	}

	private void TrackSpecials()
	{
		bool shown = Service.Config.ShowControlWindowSpecials;
		if (_specialsShown is { } was && was != shown)
		{
			if (shown)
			{
				_pendingWidth = _wideWidth > 0f ? _wideWidth : _sideBySideWidth;
			}
			else
			{
				_wideWidth = _openSize.X;
			}
		}

		_specialsShown = shown;
	}

	public override void Draw()
	{
		_fold.BeginDraw();
		if (!_fold.IsActive)
		{
			_openSize = ImGui.GetWindowSize();
		}

		var folded = _fold.Amount;
		if (folded < 1f)
		{
			using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * (1f - MathF.Min(1f, folded * 1.4f)));
			var (openPos, openSize) = _fold.OpenRect();
			DrawContent(openPos, openSize);
		}

		DrawWindowBar();
	}

	private void DrawWindowBar()
	{
		var config = Service.Config;
		bool locked = config.IsControlWindowLock;
		bool specials = config.ShowControlWindowSpecials;

		_actions[0] = specials ? HideSpecialsAction : ShowSpecialsAction;
		_actions[1] = locked ? LockedAction : UnlockedAction;
		_actions[2] = SettingsAction;

		var pressed = _fold.DrawBar("##rsr_control_actions", _actions, Brand, out var closed,
			M3.Scheme.SurfaceContainerHigh, "Hide the Control window. Turn it back on in the UI settings.");

		switch (pressed)
		{
			case 0:
				config.ShowControlWindowSpecials.Value = !specials;
				break;

			case 1:
				config.IsControlWindowLock.Value = !locked;
				break;

			case 2:
				RotationSolverPlugin.ShowConfigWindow();
				break;
		}

		if (closed)
		{
			config.ShowControlWindow.Value = false;
			config.Save();
			IsOpen = false;
		}
	}

	private void DrawContent(Vector2 openPos, Vector2 openSize)
	{
		var padding = _fold.OpenPadding;
		ImGui.SetCursorScreenPos(openPos + padding);
		using var content = ImRaii.Child("##rsr_control_content", Vector2.Max(Vector2.One, openSize - (padding * 2f)), false,
			ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBackground);
		if (!content)
		{
			return;
		}

		var width = ImGui.GetContentRegionAvail().X;

		var headerWidth = DrawHeader(width);
		ImGui.Dummy(new Vector2(0f, M3.Space1));

		var statusWidth = StatusPaneWidth();
		var specialsMinWidth = SpecialsWidth(4);
		var origin = ImGui.GetCursorScreenPos();
		float height;

		_sideBySideWidth = statusWidth + PaneGap + specialsMinWidth + (padding.X * 2f);

		if (!Service.Config.ShowControlWindowSpecials)
		{
			BeginPane(origin, width, M3CardHost.PreviousHeight(StatusPaneId));
			DrawStatus(width - (PanePadding * 2f));
			height = EndPane(StatusPaneId, origin);
		}
		else if (width >= statusWidth + PaneGap + specialsMinWidth)
		{
			var paintHeight = MathF.Max(M3CardHost.PreviousHeight(StatusPaneId), M3CardHost.PreviousHeight(SpecialsPaneId));
			var specialsOrigin = origin + new Vector2(statusWidth + PaneGap, 0f);
			var specialsWidth = width - statusWidth - PaneGap;

			BeginPane(origin, statusWidth, paintHeight);
			DrawStatus(statusWidth - (PanePadding * 2f));
			var statusHeight = EndPane(StatusPaneId, origin);

			BeginPane(specialsOrigin, specialsWidth, paintHeight);
			DrawSpecials(specialsWidth - (PanePadding * 2f));
			var specialsHeight = EndPane(SpecialsPaneId, specialsOrigin);

			height = MathF.Max(statusHeight, specialsHeight);
		}
		else
		{
			BeginPane(origin, width, M3CardHost.PreviousHeight(StatusPaneId));
			DrawStatus(width - (PanePadding * 2f));
			var statusHeight = EndPane(StatusPaneId, origin);

			var specialsOrigin = origin + new Vector2(0f, statusHeight + PaneGap);
			BeginPane(specialsOrigin, width, M3CardHost.PreviousHeight(SpecialsPaneId));
			DrawSpecials(width - (PanePadding * 2f));
			var specialsHeight = EndPane(SpecialsPaneId, specialsOrigin);

			height = statusHeight + PaneGap + specialsHeight;
		}

		ImGui.SetCursorScreenPos(origin);
		ImGui.Dummy(new Vector2(width, height));

		// A large special button scale can make one tile wider than the status pane, so keep a single column in view.
		var minimumContent = MathF.Max(headerWidth, statusWidth);
		if (Service.Config.ShowControlWindowSpecials)
		{
			minimumContent = MathF.Max(minimumContent, SpecialsWidth(1));
		}

		_minimumWidth = minimumContent + (padding.X * 2f);
		_contentHeight = ImGui.GetCursorPosY() - ImGui.GetStyle().ItemSpacing.Y + (padding.Y * 2f);
	}

	private static void BeginPane(Vector2 origin, float width, float paintHeight)
	{
		var s = M3.Scheme;
		if (paintHeight > 0f)
		{
			M3Draw.Container(ImGui.GetWindowDrawList(), origin, origin + new Vector2(width, paintHeight),
				M3.Alpha(s.SurfaceContainerLow, 0.72f), M3.ShapeMedium, M3.Alpha(s.OutlineVariant, 0.45f));
		}

		ImGui.SetCursorScreenPos(origin + new Vector2(PanePadding, PanePadding));
		ImGui.BeginGroup();
	}

	private static float EndPane(string id, Vector2 origin)
	{
		ImGui.EndGroup();
		var height = ImGui.GetItemRectMax().Y + PanePadding - origin.Y;
		M3CardHost.RecordHeight(id, height);
		return height;
	}

	private static void Overline(string text)
	{
		using var font = ImRaii.PushFont(M3.LabelSmall);
		using var color = ImRaii.PushColor(ImGuiCol.Text, M3.Alpha(M3.Scheme.OnSurfaceVariant, 0.9f));
		ImGui.TextUnformatted(text.ToUpperInvariant());
	}

	#region Header

	private float DrawHeader(float width)
	{
		ReadOnlySpan<M3Segment> segments =
		[
			new(AutoLabel(), FontAwesomeIcon.Play, StateCommandType.Auto.GetDescription()),
			new(DataCenter.IsHenched ? "Henched" : "Manual", FontAwesomeIcon.HandPointer, StateCommandType.Manual.GetDescription()),
			new("Off", FontAwesomeIcon.PowerOff, StateCommandType.Off.GetDescription(), M3.Scheme.Error),
		];

		var segmentedWidth = M3Widgets.SegmentedWidth(segments);
		var barSize = M3Widgets.WindowActionsSize(_actions.Length, Brand, 0f);
		var origin = ImGui.GetCursorScreenPos();

		// Too narrow for one row: the pill gets the top row and the switch spans the row below it.
		float switchTop, switchWidth, height;
		if (width >= segmentedWidth + M3.Space2 + barSize.X)
		{
			height = MathF.Max(M3Widgets.SegmentedHeight, barSize.Y);
			switchTop = (height - M3Widgets.SegmentedHeight) * 0.5f;
			switchWidth = segmentedWidth;
			_fold.BarTop = (height - barSize.Y) * 0.5f;
		}
		else
		{
			switchTop = barSize.Y + M3.Space1;
			switchWidth = width;
			height = switchTop + M3Widgets.SegmentedHeight;
			_fold.BarTop = 0f;
		}

		ImGui.SetCursorScreenPos(origin + new Vector2(0f, switchTop));
		var clicked = M3Widgets.SegmentedButtons("##rsr_state", segments, CurrentStateIndex(), switchWidth);
		if (clicked >= 0)
		{
			// Set, not toggled, so pressing Auto never turns it off.
			RSCommands.SetStateCommandType(clicked switch
			{
				0 => StateCommandType.Auto,
				1 => StateCommandType.Manual,
				_ => StateCommandType.Off,
			});
		}

		ImGui.SetCursorScreenPos(new Vector2(origin.X, origin.Y + height));
		ImGui.Dummy(new Vector2(width, 0f));
		return MathF.Max(segmentedWidth, barSize.X);
	}

	private static int CurrentStateIndex()
	{
		return !DataCenter.State ? 2 : DataCenter.IsManual ? 1 : 0;
	}

	private static string AutoLabel()
	{
		return DataCenter.IsAutoDuty ? "AutoDuty"
			: DataCenter.IsTargetOnly ? "Target only"
			: DataCenter.IsPvPStateEnabled ? "PvP"
			: "Auto";
	}

	#endregion

	#region Status pane

	private static float StatusPaneWidth()
	{
		var icons = NextGcdSize + NextAbilitySize + M3.Space2;
		var content = MathF.Max(MathF.Max(icons, M3Widgets.SegmentedWidth(AoeSegments)), 232f * M3.Scale);
		return content + (PanePadding * 2f);
	}

	private static void DrawStatus(float width)
	{
		var config = Service.Config;

		Overline("Next action");

		var gcd = ActionUpdater.NextGCDAction;
		if (M3ActionIcon.Draw("##next_gcd", gcd, NextGcdSize, config.ShowCooldownsAlways))
		{
			UseOrQueue(gcd);
		}

		var ability = gcd != ActionUpdater.NextAction ? ActionUpdater.NextAction : null;
		ImGui.SameLine(0f, M3.Space2);
		if (M3ActionIcon.Draw("##next_ability", ability, NextAbilitySize, config.ShowCooldownsAlways))
		{
			UseOrQueue(ability);
		}

		NextActionWindow.DrawGcdProgress(width, showTime: true);
		DrawQueuedCommand(width);

		ImGui.Dummy(new Vector2(0f, M3.Space1));
		Overline("AoE");
		var aoe = M3Widgets.SegmentedButtons("##rsr_aoe", AoeSegments, (int)config.AoEType, width);
		if (aoe >= 0)
		{
			config.AoEType = (ConfigTypes.AoEType)aoe;
		}

		DrawModeChips(width);

		ImGui.Dummy(new Vector2(0f, M3.Space1));
		DrawStatusLines(width);
	}

	private static void DrawQueuedCommand(float width)
	{
		var s = M3.Scheme;
		var action = DataCenter.CommandNextAction;
		var iconSize = 28f * M3.Scale;
		var lineHeight = ImGui.GetTextLineHeight();

		float captionHeight;
		using (ImRaii.PushFont(M3.LabelSmall))
		{
			captionHeight = ImGui.GetTextLineHeight();
		}

		var rowHeight = MathF.Max(iconSize, captionHeight + lineHeight);
		var origin = ImGui.GetCursorScreenPos();

		ImGui.SetCursorScreenPos(origin + new Vector2(0f, (rowHeight - iconSize) * 0.5f));
		if (M3ActionIcon.Draw("##queued_command", action, iconSize, showCooldown: false))
		{
			UseOrQueue(action);
		}

		var drawList = ImGui.GetWindowDrawList();
		var textX = origin.X + iconSize + M3.Space2;
		var textY = origin.Y + ((rowHeight - captionHeight - lineHeight) * 0.5f);

		using (ImRaii.PushFont(M3.LabelSmall))
		{
			drawList.AddText(new Vector2(textX, textY), M3.U32(s.OnSurfaceVariant, 0.9f), "QUEUED");
		}

		var name = action == null ? "Nothing queued" : M3Navigation.Truncate(action.Name, width - (textX - origin.X));
		drawList.AddText(new Vector2(textX, textY + captionHeight),
			action == null ? M3.U32(s.OnSurfaceVariant, 0.6f) : M3.U32(s.OnSurface), name);

		ImGui.SetCursorScreenPos(origin);
		ImGui.Dummy(new Vector2(width, rowHeight));
	}

	private static void DrawModeChips(float width)
	{
		var config = Service.Config;
		bool burst = config.AutoBurst;

		if (M3Widgets.Chip("##rsr_burst", "Burst", burst, burst ? FontAwesomeIcon.Check : FontAwesomeIcon.FireAlt,
			"Allow the rotation to spend its burst cooldowns."))
		{
			config.AutoBurst.Value = !burst;
		}

		var targeting = DataCenter.TargetingType.GetDescription();
		var needed = ImGui.GetItemRectSize().X + M3.Space1 + M3Widgets.ChipWidth(targeting, FontAwesomeIcon.Crosshairs, FontAwesomeIcon.CaretDown);
		if (needed <= width)
		{
			ImGui.SameLine(0f, M3.Space1);
		}

		if (M3Widgets.Chip("##rsr_targeting", targeting, false, FontAwesomeIcon.Crosshairs,
			"How Auto mode picks its target. Click to choose from your targeting list.",
			trailingIcon: FontAwesomeIcon.CaretDown))
		{
			ImGui.OpenPopup(TargetingMenuId);
		}

		DrawTargetingMenu();
	}

	private static void DrawTargetingMenu()
	{
		ImGui.SetNextWindowSizeConstraints(new Vector2(160f * M3.Scale, 0f), new Vector2(float.MaxValue, float.MaxValue));
		using var popup = ImRaii.Popup(TargetingMenuId);
		if (!popup)
		{
			return;
		}

		var types = Service.Config.TargetingTypes;
		var current = DataCenter.TargetingTypeOverride.HasValue || types.Count == 0 ? -1 : Service.Config.TargetingIndex % types.Count;

		for (var i = 0; i < types.Count; i++)
		{
			if (M3Widgets.MenuItem($"{TargetingMenuId}_{i}", types[i].GetDescription(), i == current))
			{
				RSCommands.SetTargetingIndex(i);
				ImGui.CloseCurrentPopup();
			}
		}
	}

	private static void DrawStatusLines(float width)
	{
		float labelWidth;
		using (ImRaii.PushFont(M3.LabelSmall))
		{
			labelWidth = MathF.Max(ImGui.CalcTextSize("HOSTILE").X, ImGui.CalcTextSize("AUTO").X);
		}

		var hostile = DataCenter.CurrentTargetToHostileType.GetDescription();
		StatusLine(FontAwesomeIcon.Users, "Hostile", DataCenter.ActiveIpcOverrides?.HostileType is not null ? $"{hostile} (IPC)" : hostile, labelWidth, width);
		StatusLine(FontAwesomeIcon.Robot, "Auto", DataCenter.AutoStatus.ToString(), labelWidth, width);
	}

	private static void StatusLine(FontAwesomeIcon icon, string label, string value, float labelWidth, float width)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = ImGui.GetTextLineHeight() + (4f * scale);

		ImGui.Dummy(new Vector2(width, height));
		var min = ImGui.GetItemRectMin();
		var hovered = ImGui.IsItemHovered();
		var drawList = ImGui.GetWindowDrawList();

		var iconSize = M3Draw.MeasureIcon(icon);
		M3Draw.Icon(drawList, icon, new Vector2(min.X, min.Y + ((height - iconSize.Y) * 0.5f)), M3.Alpha(s.OnSurfaceVariant, 0.8f));

		var labelX = min.X + (22f * scale);
		using (ImRaii.PushFont(M3.LabelSmall))
		{
			var caption = label.ToUpperInvariant();
			drawList.AddText(new Vector2(labelX, min.Y + ((height - ImGui.GetTextLineHeight()) * 0.5f)),
				M3.U32(s.OnSurfaceVariant, 0.9f), caption);
		}

		var valueX = labelX + labelWidth + M3.Space2;
		var text = M3Navigation.Truncate(value, min.X + width - valueX);
		drawList.AddText(new Vector2(valueX, min.Y + ((height - ImGui.GetTextLineHeight()) * 0.5f)), M3.U32(s.OnSurface, 0.92f), text);

		if (hovered && text != value)
		{
			ImguiTooltips.ShowTooltip(value);
		}
	}

	#endregion

	#region Specials pane

	private static M3.WindowScaleScope PushSpecialsScale()
	{
		return M3.PushWindowScale(Service.Config.ControlWindowSpecialsScale);
	}

	private static float SpecialsWidth(int columns)
	{
		float tiles;
		using (PushSpecialsScale())
		{
			tiles = (TileSize().X * columns) + (M3.Space1 * (columns - 1));
		}

		return tiles + (PanePadding * 2f);
	}

	private static Vector2 TileSize()
	{
		var padding = TilePadding;
		var labelWidth = 0f;
		float labelHeight;

		using (ImRaii.PushFont(M3.LabelSmall))
		{
			foreach (var group in SpecialGroups)
			{
				foreach (var tile in group.Tiles)
				{
					labelWidth = MathF.Max(labelWidth, ImGui.CalcTextSize(tile.Label).X);
				}
			}

			labelHeight = ImGui.GetTextLineHeight();
		}

		var iconsWidth = SpecialGcdSize + TileIconGap + SpecialAbilitySize;
		return new Vector2(
			MathF.Max(iconsWidth, labelWidth) + (padding.X * 2f),
			SpecialGcdSize + TileLabelGap + labelHeight + (padding.Y * 2f));
	}

	private static void DrawSpecials(float width)
	{
		using var scale = PushSpecialsScale();
		var rotation = DataCenter.CurrentRotation;
		var spacing = M3.Space1;
		var tile = TileSize();
		var columns = Math.Clamp((int)MathF.Floor((width + spacing) / (tile.X + spacing)), 1, MaxTileColumns);

		var stretched = (width - (spacing * (columns - 1))) / columns;
		tile.X = Math.Clamp(stretched, tile.X, tile.X * 1.5f);

		foreach (var group in SpecialGroups)
		{
			Overline(group.Title);
			for (var i = 0; i < group.Tiles.Length; i++)
			{
				if (i % columns != 0)
				{
					ImGui.SameLine(0f, spacing);
				}

				DrawSpecialTile(group.Tiles[i], rotation, tile);
			}
		}
	}

	private static void DrawSpecialTile(in SpecialTile tile, ICustomRotation? rotation, Vector2 size)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var accent = tile.Accent(s);
		var gcd = rotation == null ? null : tile.Gcd?.Invoke(rotation);
		var ability = rotation == null ? null : tile.Ability?.Invoke(rotation);

		// SpecialType reads EndSpecial when nothing is running, so End is never shown as selected.
		var running = DataCenter.SpecialType;
		var isEnd = tile.Command == SpecialCommandType.EndSpecial;
		var active = !isEnd && running == tile.Command;
		var enabled = !isEnd || running != SpecialCommandType.EndSpecial;
		var dim = enabled ? 1f : M3.DisabledContent;

		var pressed = ImGui.InvisibleButton($"##special_{tile.Command}", size) && enabled;
		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = enabled && ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var rounding = M3.ShapeMedium;

		drawList.AddRectFilled(min, max, M3.U32(active ? M3.Alpha(accent, 0.18f) : M3.Alpha(s.SurfaceContainerHighest, 0.45f)), rounding);

		if (enabled && (hovered || held))
		{
			drawList.AddRectFilled(min, max, M3.U32(active ? accent : s.OnSurface, held ? M3.StatePressed : M3.StateHover), rounding);
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		drawList.AddRect(min, max, M3.U32(active ? accent : s.OutlineVariant, active ? 0.95f : 0.6f * dim),
			rounding, ImDrawFlags.None, (active ? 2f : 1f) * scale);

		var padding = TilePadding;
		float labelHeight;
		using (ImRaii.PushFont(M3.LabelSmall))
		{
			var labelSize = ImGui.CalcTextSize(tile.Label);
			labelHeight = labelSize.Y;
			drawList.AddText(new Vector2(min.X + ((size.X - labelSize.X) * 0.5f), max.Y - padding.Y - labelSize.Y),
				M3.U32(active ? accent : s.OnSurfaceVariant, 0.95f * dim), tile.Label);
		}

		DrawTileIcons(drawList, tile.Glyph, gcd, ability,
			new Vector2(min.X + padding.X, min.Y + padding.Y),
			new Vector2(max.X - padding.X, max.Y - padding.Y - labelHeight - TileLabelGap),
			accent, dim);

		if (active && DataCenter.SpecialTimeLeft > 0)
		{
			using var font = ImRaii.PushFont(M3.LabelSmall);
			var time = $"{DataCenter.SpecialTimeLeft:F1}s";
			var timeSize = ImGui.CalcTextSize(time);
			var badgePadding = new Vector2(5f, 1f) * scale;
			var badgeMin = new Vector2(max.X - timeSize.X - (badgePadding.X * 2f) - (3f * scale), min.Y + (3f * scale));
			drawList.AddRectFilled(badgeMin, badgeMin + timeSize + (badgePadding * 2f), M3.U32(s.InverseSurface, 0.92f), M3.ShapeFull);
			drawList.AddText(badgeMin + badgePadding, M3.U32(s.InverseOnSurface), time);
		}

		if (hovered)
		{
			var help = tile.Command.GetDescription();
			if (gcd != null)
			{
				help += $"\nGCD: {gcd.Name}";
			}
			if (ability != null)
			{
				help += $"\nAbility: {ability.Name}";
			}

			ImguiTooltips.ShowTooltip(help);
		}

		if (pressed)
		{
			_ = Svc.Commands.ProcessCommand(tile.Command.GetCommandStr());
		}
	}

	private static void DrawTileIcons(ImDrawListPtr drawList, FontAwesomeIcon glyph, IAction? gcd, IAction? ability, Vector2 min, Vector2 max, Vector4 accent, float alpha)
	{
		IDalamudTextureWrap? first = null;
		IDalamudTextureWrap? second = null;
		var firstSize = SpecialGcdSize;
		var secondSize = SpecialAbilitySize;

		if (gcd != null && gcd.GetTexture(out var gcdTexture))
		{
			first = gcdTexture;
		}

		if (ability != null && ability.GetTexture(out var abilityTexture))
		{
			second = abilityTexture;
		}

		if (first == null)
		{
			first = second;
			firstSize = secondSize;
			second = null;
		}

		var center = (min + max) * 0.5f;
		if (first == null)
		{
			var radius = MathF.Min(max.Y - min.Y, 36f * M3.Scale) * 0.5f;
			drawList.AddCircleFilled(center, radius, M3.U32(accent, 0.16f * alpha), 32);
			M3Draw.IconCentered(drawList, glyph, center - new Vector2(radius, radius), center + new Vector2(radius, radius), M3.Alpha(accent, alpha),
				Service.Config.ControlWindowSpecialsScale);
			return;
		}

		var x = center.X - ((firstSize + (second == null ? 0f : TileIconGap + secondSize)) * 0.5f);
		_ = M3ActionIcon.Image(drawList, first, new Vector2(x, max.Y - firstSize), new Vector2(x + firstSize, max.Y),
			M3ActionIcon.Rounding(firstSize), alpha);

		if (second != null)
		{
			x += firstSize + TileIconGap;
			_ = M3ActionIcon.Image(drawList, second, new Vector2(x, max.Y - secondSize), new Vector2(x + secondSize, max.Y),
				M3ActionIcon.Rounding(secondSize), alpha);
		}
	}

	#endregion

	internal static void UseOrQueue(IAction? action)
	{
		if (!DataCenter.State)
		{
			var canDoIt = false;
			if (action is IBaseAction act)
			{
				// ForceEnable is global, so always turn it back off, even if CanUse throws.
				IBaseAction.ForceEnable = true;
				try
				{
					canDoIt = act.CanUse(out _, usedUp: true, skipAoeCheck: true);
				}
				finally
				{
					IBaseAction.ForceEnable = false;
				}
			}
			else if (action is IBaseItem item)
			{
				canDoIt = item.CanUse(out _, true);
			}
			if (canDoIt)
			{
				_ = (action?.Use());
			}
		}
		else if (action != null)
		{
			DataCenter.AddCommandAction(action, 5);
		}
	}

	internal static (Vector2, Vector2) DrawIAction(IAction? action, float width, float percent, bool isAdjust = true)
	{
		if (!action.GetTexture(out var texture, isAdjust))
		{
			return (default, default);
		}

		var cursor = ImGui.GetCursorPos();

		var desc = action?.Name ?? string.Empty;
		if (texture?.Handle != null && ImGuiHelper.NoPaddingNoColorImageButton(texture, Vector2.One * width, desc))
		{
			UseOrQueue(action);
		}
		var size = ImGui.GetItemRectSize();
		var pos = cursor;

		if (action == null || !Service.Config.ShowCooldownsAlways)
		{
			ImGuiHelper.DrawActionOverlay(pos, width, -1);
			ImguiTooltips.HoveredTooltip(desc);

			return (pos, size);
		}
		else
		{
			var recast = action.Cooldown.RecastTimeOneChargeRaw;
			var elapsed = action.Cooldown.RecastTimeElapsedRaw;
			var winPos = ImGui.GetWindowPos();
			var r = -1f;
			if (Service.Config.UseOriginalCooldown)
			{
				r = !action.EnoughLevel ? 0 : recast == 0 || !action.Cooldown.IsCoolingDown ? 1 : elapsed / recast;
			}
			ImGuiHelper.DrawActionOverlay(cursor, width, r);
			ImguiTooltips.HoveredTooltip(desc);

			if (!action.EnoughLevel)
			{
				if (!Service.Config.UseOriginalCooldown)
				{
					ImGui.GetWindowDrawList().AddRectFilled(new Vector2(pos.X, pos.Y) + winPos,
						new Vector2(pos.X + size.X, pos.Y + size.Y) + winPos, ImGuiHelper.ProgressCol);
				}
			}
			else if (action.Cooldown.IsCoolingDown)
			{
				if (!Service.Config.UseOriginalCooldown)
				{
					var ratio = recast == 0 || !action.EnoughLevel ? 0 : elapsed % recast / recast;
					var startPos = new Vector2(pos.X + (size.X * ratio), pos.Y) + winPos;
					ImGui.GetWindowDrawList().AddRectFilled(startPos,
						new Vector2(pos.X + size.X, pos.Y + size.Y) + winPos, ImGuiHelper.ProgressCol);

					ImGui.GetWindowDrawList().AddLine(startPos, startPos + new Vector2(0, size.Y), ImGuiHelper.Black);
				}

				using var font = ImRaii.PushFont(ImGui.GetFont());
				var time = recast == 0 ? "0" : ((int)(recast - (elapsed % recast)) + 1).ToString();
				var strSize = ImGui.CalcTextSize(time);
				var fontPos = new Vector2(pos.X + (size.X / 2) - (strSize.X / 2), pos.Y + (size.Y / 2) - (strSize.Y / 2)) + winPos;

				ImGuiHelper.TextShade(fontPos, time);
			}

			if (action.EnoughLevel && action is IBaseAction bAct && bAct.Cooldown.MaxCharges > 1)
			{
				for (var i = 0; i < bAct.Cooldown.CurrentCharges; i++)
				{
					ImGui.GetWindowDrawList().AddCircleFilled(winPos + pos + ((i + 0.5f) * new Vector2(width / 5, 0)), width / 12, ImGuiHelper.White);
				}
			}

			return (pos, size);
		}
	}
}
