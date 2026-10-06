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

	// Full, Cleave, Off sits under Auto, Manual, Off, so each link joins a state to the AoE type below it.
	private static readonly M3Segment[] AoeSegments =
	[
		new("Full", Tooltip: "Use all available AoE actions."),
		new("Cleave", Tooltip: "Use only single-target AoE actions."),
		new("Off", Tooltip: "Do not use any AoE actions."),
	];

	private static readonly ConfigTypes.AoEType[] AoeTypes =
	[
		ConfigTypes.AoEType.Full,
		ConfigTypes.AoEType.Cleave,
		ConfigTypes.AoEType.Off,
	];

	private readonly record struct StateLink(string Id, string LinkedTooltip, string UnlinkedTooltip);

	private static readonly StateLink[] Links =
	[
		new("##rsr_link_auto",
			"Auto and Full are linked: choosing Auto here also sets AoE to Full. Click to unlink.",
			"Link Auto and Full, so choosing Auto here also sets AoE to Full."),
		new("##rsr_link_manual",
			"Manual and Cleave are linked: choosing Manual here also sets AoE to Cleave. Click to unlink.",
			"Link Manual and Cleave, so choosing Manual here also sets AoE to Cleave."),
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

	private static float PanePadding => 6f * M3.PaddingScale;
	private static float PaneGap => M3.Space1;
	private static Vector2 TilePadding => new Vector2(4f, 4f) * M3.PaddingScale;
	private static float TileIconGap => 3f * M3.PaddingScale;
	private static float TileLabelGap => 4f * M3.PaddingScale;
	private static float LinkRowHeight => M3.FitText(20f, 2f);

	internal static float NextGcdSize => NextGcdBaseSize * Service.Config.ControlWindowNextSizeRatio;
	internal static float NextAbilitySize => NextAbilityBaseSize * Service.Config.ControlWindowNextSizeRatio;

	private static float SpecialGcdSize => SpecialGcdBaseSize * Service.Config.ControlWindowSpecialsScale;
	private static float SpecialAbilitySize => SpecialAbilityBaseSize * Service.Config.ControlWindowSpecialsScale;

	private static M3WindowBrand Brand => new(MainWindow.GetLogoTexture(), "RSR Control");

	private readonly M3WindowAction[] _actions = new M3WindowAction[3];
	private readonly M3WindowFold _fold = new();

	// How many of _actions fit in the pill, from the front. Set by the header and used next frame by the fold.
	private int _shownActions = 3;

	private M3Style.Scope _theme;

	private float _contentHeight;
	private float _minimumWidth;
	private Vector2 _openSize;
	private float _sideBySideWidth;
	private float _statusOnlyWidth;
	private bool? _specialsShown;
	private float _wideWidth;
	private float _narrowWidth;
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
		_theme = M3Style.Push(M3Density.Tight);

		base.PreDraw();
		Flags |= ImGuiWindowFlags.NoTitleBar;

		TrackSpecials();

		if (_fold.Prepare(this, _shownActions, Brand))
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
			ImGui.SetNextWindowSizeConstraints(
				new Vector2(_minimumWidth, _contentHeight),
				new Vector2(float.MaxValue, _contentHeight));
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
		// Each layout keeps its own width, so hiding and showing the specials goes back to the width you left each at.
		if (_specialsShown is { } was && was != shown)
		{
			if (shown)
			{
				_narrowWidth = _openSize.X;
				_pendingWidth = _wideWidth > 0f ? _wideWidth : _sideBySideWidth;
			}
			else
			{
				_wideWidth = _openSize.X;
				_pendingWidth = _narrowWidth > 0f ? _narrowWidth : _statusOnlyWidth;
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

		var pressed = _fold.DrawBar("##rsr_control_actions", _actions.AsSpan(0, _shownActions), Brand, out var closed,
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

		var (headerMinimum, headerNatural) = DrawHeader(width);

		var statusWidth = StatusPaneWidth();
		var specialsMinWidth = SpecialsWidth(4);
		var origin = ImGui.GetCursorScreenPos();
		float height;

		_sideBySideWidth = statusWidth + PaneGap + specialsMinWidth + (padding.X * 2f);
		_statusOnlyWidth = MathF.Max(headerNatural, statusWidth) + (padding.X * 2f);

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

		// The panes can go narrower than they like to be, since their text truncates and their chips wrap.
		var minimumContent = MathF.Max(headerMinimum, StatusPaneMinimumWidth());
		if (Service.Config.ShowControlWindowSpecials)
		{
			// A large special button scale can make one tile wider than the status pane, so keep a single column in view.
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
				M3.Alpha(s.SurfaceContainerLow, 0.72f), M3.ShapeSmall, M3.Alpha(s.OutlineVariant, 0.45f));
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

	// Returns the narrowest width the header fits in, and the width it fits in with every icon shown.
	private (float Minimum, float Natural) DrawHeader(float width)
	{
		ReadOnlySpan<M3Segment> labelled =
		[
			new(AutoLabel(), FontAwesomeIcon.Play, StateCommandType.Auto.GetDescription()),
			new(DataCenter.IsHenched ? "Henched" : "Manual", FontAwesomeIcon.HandPointer, StateCommandType.Manual.GetDescription()),
			new("Off", FontAwesomeIcon.PowerOff, StateCommandType.Off.GetDescription(), M3.Scheme.Error),
		];

		ReadOnlySpan<M3Segment> compact =
		[
			labelled[0] with { Icon = FontAwesomeIcon.None },
			labelled[1] with { Icon = FontAwesomeIcon.None },
			labelled[2] with { Icon = FontAwesomeIcon.None },
		];

		// Both switches share a width so their columns line up under each other.
		var aoeWidth = M3Widgets.SegmentedWidth(AoeSegments);
		var labelledWidth = MathF.Max(M3Widgets.SegmentedWidth(labelled), aoeWidth);
		var compactWidth = MathF.Max(M3Widgets.SegmentedWidth(compact), aoeWidth);
		var smallWidth = SmallSwitchWidth(compact);
		var segmentHeight = M3Widgets.SegmentedHeight;
		var fullBar = M3Widgets.WindowActionsSize(_actions.Length, Brand, 0f);
		var barSize = fullBar;
		var shown = _actions.Length;
		var origin = ImGui.GetCursorScreenPos();

		// As the window narrows the state icons go first. Then the pill gets the top row and the switches span the rows below it.
		// Narrower still, the switch labels shrink and the pill drops Settings, then Lock, but always keeps the specials toggle.
		bool icons;
		ImFontPtr? labelFont = null;
		float switchTop, switchWidth, rowHeight;
		if (width >= compactWidth + M3.Space2 + fullBar.X)
		{
			icons = width >= labelledWidth + M3.Space2 + fullBar.X;
			switchWidth = icons ? labelledWidth : compactWidth;
			rowHeight = MathF.Max(segmentHeight, barSize.Y);
			switchTop = (rowHeight - segmentHeight) * 0.5f;
			_fold.BarTop = (rowHeight - barSize.Y) * 0.5f;
		}
		else
		{
			while (shown > 1 && barSize.X > width)
			{
				shown--;
				barSize = M3Widgets.WindowActionsSize(shown, Brand, 0f);
			}

			icons = width >= labelledWidth;
			labelFont = width >= compactWidth ? null : M3.LabelSmall;
			switchWidth = width;
			rowHeight = barSize.Y;
			switchTop = barSize.Y + M3.Space1;
			_fold.BarTop = 0f;
		}

		_shownActions = shown;

		ImGui.SetCursorScreenPos(origin + new Vector2(0f, switchTop));
		var state = M3Widgets.SegmentedButtons("##rsr_state", icons ? labelled : compact, CurrentStateIndex(), switchWidth, labelFont);

		var linkTop = switchTop + segmentHeight;
		DrawLinks(origin + new Vector2(0f, linkTop), switchWidth);

		var aoeTop = linkTop + LinkRowHeight;
		ImGui.SetCursorScreenPos(origin + new Vector2(0f, aoeTop));
		var aoe = M3Widgets.SegmentedButtons("##rsr_aoe", AoeSegments, Array.IndexOf(AoeTypes, Service.Config.AoEType), switchWidth, labelFont);
		if (aoe >= 0)
		{
			Service.Config.AoEType = AoeTypes[aoe];
		}

		if (state >= 0)
		{
			SetState(state);
		}

		var height = MathF.Max(rowHeight, aoeTop + segmentHeight);
		ImGui.SetCursorScreenPos(new Vector2(origin.X, origin.Y + height));
		ImGui.Dummy(new Vector2(width, 0f));
		return (MathF.Max(smallWidth, M3Widgets.WindowActionsSize(1, Brand, 0f).X), MathF.Max(labelledWidth, fullBar.X));
	}

	// The narrowest the switches go: small labels with a little room either side.
	private static float SmallSwitchWidth(ReadOnlySpan<M3Segment> state)
	{
		var widest = 0f;
		using (ImRaii.PushFont(M3.LabelSmall))
		{
			foreach (var segment in state)
			{
				widest = MathF.Max(widest, ImGui.CalcTextSize(segment.Label).X);
			}

			foreach (var segment in AoeSegments)
			{
				widest = MathF.Max(widest, ImGui.CalcTextSize(segment.Label).X);
			}
		}

		return (widest + (8f * M3.Scale)) * state.Length;
	}

	private static void DrawLinks(Vector2 origin, float switchWidth)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var rowHeight = LinkRowHeight;
		var columnWidth = switchWidth / AoeSegments.Length;
		var size = new Vector2(30f * scale, rowHeight - (2f * scale));
		var rounding = size.Y * 0.5f;
		var drawList = ImGui.GetWindowDrawList();

		// Names the AoE switch below, when there is room left of the first link.
		using (ImRaii.PushFont(M3.LabelSmall))
		{
			var caption = ImGui.CalcTextSize("AOE");
			if (caption.X + M3.Space1 <= (columnWidth - size.X) * 0.5f)
			{
				drawList.AddText(new Vector2(origin.X, origin.Y + ((rowHeight - caption.Y) * 0.5f)),
					M3.U32(s.OnSurfaceVariant, 0.9f), "AOE");
			}
		}

		for (var i = 0; i < Links.Length; i++)
		{
			var link = Links[i];
			var linked = IsLinked(i);
			var min = new Vector2(origin.X + (columnWidth * (i + 0.5f)) - (size.X * 0.5f), origin.Y + ((rowHeight - size.Y) * 0.5f));
			var max = min + size;

			ImGui.SetCursorScreenPos(min);
			var pressed = ImGui.InvisibleButton(link.Id, size);
			var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
			var held = ImGui.IsItemActive();
			var content = linked ? s.OnSecondaryContainer : M3.Alpha(s.OnSurfaceVariant, 0.7f);

			if (linked)
			{
				drawList.AddRectFilled(min, max, M3.U32(s.SecondaryContainer, 0.95f), rounding);
			}

			if (hovered || held)
			{
				drawList.AddRectFilled(min, max, M3.U32(linked ? s.OnSecondaryContainer : s.OnSurface, held ? M3.StatePressed : M3.StateHover), rounding);
				ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
				ImguiTooltips.ShowTooltip(linked ? link.LinkedTooltip : link.UnlinkedTooltip);
			}

			var icon = linked ? FontAwesomeIcon.Link : FontAwesomeIcon.Unlink;
			M3Draw.IconCentered(drawList, icon, min, max, content, MathF.Min(1f, size.Y * 0.7f / M3Draw.MeasureIcon(icon).Y));

			if (pressed)
			{
				ToggleLink(i);
			}
		}
	}

	private static int CurrentStateIndex()
	{
		return !DataCenter.State ? 2 : DataCenter.IsManual ? 1 : 0;
	}

	private static void SetState(int index)
	{
		// Set, not toggled, so pressing Auto never turns it off.
		RSCommands.SetStateCommandType(index switch
		{
			0 => StateCommandType.Auto,
			1 => StateCommandType.Manual,
			_ => StateCommandType.Off,
		});

		// Duty replays refuse every state but Off, so only follow a link if the state really changed.
		if (CurrentStateIndex() == index && IsLinked(index))
		{
			Service.Config.AoEType = AoeTypes[index];
		}
	}

	private static bool IsLinked(int index)
	{
		return index switch
		{
			0 => Service.Config.ControlWindowLinkAutoFull,
			1 => Service.Config.ControlWindowLinkManualCleave,
			_ => false,
		};
	}

	private static void ToggleLink(int index)
	{
		var config = Service.Config;
		var linked = !IsLinked(index);
		if (index == 0)
		{
			config.ControlWindowLinkAutoFull = linked;
		}
		else
		{
			config.ControlWindowLinkManualCleave = linked;
		}

		config.Save();

		// Linking the state you are already in applies its AoE type straight away.
		if (linked && CurrentStateIndex() == index)
		{
			config.AoEType = AoeTypes[index];
		}
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

	private static float NextIconsWidth => NextGcdSize + NextAbilitySize + M3.Space2;

	private static float StatusPaneWidth()
	{
		return MathF.Max(NextIconsWidth, 232f * M3.Scale) + (PanePadding * 2f);
	}

	// Measured over the whole targeting list, so cycling targets doesn't push the window wider.
	// A narrow pane drops the targeting chip's crosshair, so that chip is measured without it.
	private static float StatusPaneMinimumWidth()
	{
		var chips = MathF.Max(M3Widgets.ChipWidth("Burst", FontAwesomeIcon.FireAlt), M3Widgets.ChipWidth("Burst", FontAwesomeIcon.Check));
		chips = MathF.Max(chips, M3Widgets.ChipWidth(DataCenter.TargetingType.GetDescription(), trailingIcon: FontAwesomeIcon.CaretDown));
		foreach (var type in Service.Config.TargetingTypes)
		{
			chips = MathF.Max(chips, M3Widgets.ChipWidth(type.GetDescription(), trailingIcon: FontAwesomeIcon.CaretDown));
		}

		return MathF.Max(NextIconsWidth, chips) + (PanePadding * 2f);
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
		DrawModeChips(width);
		DrawStatusLines(width);
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
		var icon = M3Widgets.ChipWidth(targeting, FontAwesomeIcon.Crosshairs, FontAwesomeIcon.CaretDown) <= width
			? FontAwesomeIcon.Crosshairs
			: FontAwesomeIcon.None;
		var needed = ImGui.GetItemRectSize().X + M3.Space1 + M3Widgets.ChipWidth(targeting, icon, FontAwesomeIcon.CaretDown);
		if (needed <= width)
		{
			ImGui.SameLine(0f, M3.Space1);
		}

		if (M3Widgets.Chip("##rsr_targeting", targeting, false, icon,
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
		const string HostileLabel = "HOSTILE";
		const string TriggerLabel = "STATE TRIGGER";

		float labelWidth;
		using (ImRaii.PushFont(M3.LabelSmall))
		{
			labelWidth = MathF.Max(ImGui.CalcTextSize(HostileLabel).X, ImGui.CalcTextSize(TriggerLabel).X);
		}

		// Too narrow for a readable value beside its label, so each value goes under its label instead.
		var stacked = labelWidth + M3.Space2 + (64f * M3.Scale) > width;

		var hostile = DataCenter.CurrentTargetToHostileType.GetDescription();
		StatusLine(HostileLabel, DataCenter.ActiveIpcOverrides?.HostileType is not null ? $"{hostile} (IPC)" : hostile, labelWidth, width, stacked);
		StatusLine(TriggerLabel, DataCenter.AutoStatus.ToString(), labelWidth, width, stacked);
	}

	private static void StatusLine(string label, string value, float labelWidth, float width, bool stacked)
	{
		var s = M3.Scheme;
		var lineHeight = ImGui.GetTextLineHeight();
		float captionHeight;
		using (ImRaii.PushFont(M3.LabelSmall))
		{
			captionHeight = ImGui.GetTextLineHeight();
		}

		var inset = 1f * M3.Scale;
		var height = (stacked ? captionHeight + lineHeight : lineHeight) + (inset * 2f);

		ImGui.Dummy(new Vector2(width, height));
		var min = ImGui.GetItemRectMin();
		var hovered = ImGui.IsItemHovered();
		var drawList = ImGui.GetWindowDrawList();

		using (ImRaii.PushFont(M3.LabelSmall))
		{
			var captionY = stacked ? min.Y + inset : min.Y + ((height - captionHeight) * 0.5f);
			drawList.AddText(new Vector2(min.X, captionY), M3.U32(s.OnSurfaceVariant, 0.9f), label);
		}

		var valuePosition = stacked
			? new Vector2(min.X, min.Y + inset + captionHeight)
			: new Vector2(min.X + labelWidth + M3.Space2, min.Y + ((height - lineHeight) * 0.5f));
		var text = M3Navigation.Truncate(value, min.X + width - valuePosition.X);
		drawList.AddText(valuePosition, M3.U32(s.OnSurface, 0.92f), text);

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
