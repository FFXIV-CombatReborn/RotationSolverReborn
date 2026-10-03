using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using RotationSolver.Commands;
using RotationSolver.UI.Material;
using RotationSolver.Updaters;
using static RotationSolver.Basic.Configuration.ConfigTypes;

namespace RotationSolver.UI.ExtraWindows;

internal class StateControlWindow : Window
{
	private const ImGuiWindowFlags BaseFlags = ImGuiWindowFlags.NoTitleBar
		| ImGuiWindowFlags.NoCollapse
		| ImGuiWindowFlags.NoScrollbar
		| ImGuiWindowFlags.NoScrollWithMouse;

	private const string Title = "Autorotation";

	private const float MaximumWidth = 900f;

	private const int ManualIndex = 1;

	private readonly record struct Segment(
		StateCommandType State,
		string Label,
		Func<M3Scheme, Vector4> Container,
		Func<M3Scheme, Vector4> OnContainer,
		Func<M3Scheme, Vector4> Accent);

	private static readonly Segment[] Segments =
	[
		new(StateCommandType.Auto, "Auto", s => s.PrimaryContainer, s => s.OnPrimaryContainer, s => s.Primary),
		new(StateCommandType.Manual, "Manual", s => s.TertiaryContainer, s => s.OnTertiaryContainer, s => s.Tertiary),
		new(StateCommandType.Off, "Off", s => s.OutlineVariant, s => s.OnSurface, s => s.OnSurfaceVariant),
	];

	private readonly record struct AoeOption(AoEType Type, string Label, string Tooltip);

	private static readonly AoeOption[] AoeOptions =
	[
		new(AoEType.Cleave, "Cleave", "In Manual, use only single-target AoE actions."),
		new(AoEType.Full, "Full", "In Manual, use all available AoE actions."),
	];

	private static readonly M3WindowAction[] Actions =
	[
		new("##state_settings", FontAwesomeIcon.Cog, "Open the settings"),
	];

	private M3Style.Scope _theme;

	private bool _shownSetting;

	private float _indicatorPos = -1f;
	private float _indicatorWidth;

	private float _aoeIndicatorPos = -1f;

	private float _tabTime;
	private float _tabShown;
	private float _band;

	// Last frame's window position and tab height. The window grows upward as the tab rises, so the switch stays still.
	private Vector2 _windowPos;
	private float _drawnBand;
	private bool _placed;

	private float _baseHeight;
	private float _minimumWidth;

	public StateControlWindow()
		: base("RSR Autorotation###rsrStateControlWindow", BaseFlags)
	{
		Size = new Vector2(360f, 132f);
		SizeCondition = ImGuiCond.FirstUseEver;
		RespectCloseHotkey = true;

		AllowPinning = false;
		AllowClickthrough = false;
	}

	private static float TabPadding => 4f * M3.Scale;

	private static float TabFillet => 8f * M3.Scale;

	private static float TabSegmentHeight => M3.FitText(28f, 4f);

	private static float TabHeight => TabSegmentHeight + (TabPadding * 2f);

	public override void PreOpenCheck()
	{
		var setting = Service.Config.ShowStateControlWindow;
		if (setting.Value != _shownSetting)
		{
			IsOpen = setting.Value;
		}
		else if (IsOpen != setting.Value)
		{
			setting.Value = IsOpen;
			Service.Config.Save();
		}

		_shownSetting = setting.Value;
		base.PreOpenCheck();
	}

	public override bool DrawConditions()
	{
		return MajorUpdater.IsValid && base.DrawConditions();
	}

	public override void OnOpen()
	{
		// Clear any pin or click-through left over from when the window had a title bar.
		IsPinned = false;
		IsClickthrough = false;

		_indicatorPos = -1f;
		_aoeIndicatorPos = -1f;
		_tabTime = CurrentIndex() == ManualIndex ? 1f : 0f;
		_placed = false;
		base.OnOpen();
	}

	public override void PreDraw()
	{
		_theme = M3Style.Push();

		var target = CurrentIndex() == ManualIndex ? 1f : 0f;
		if (_tabTime != target)
		{
			var step = ImGui.GetIO().DeltaTime / M3Motion.EmphasisedDuration;
			_tabTime = target > _tabTime ? MathF.Min(target, _tabTime + step) : MathF.Max(target, _tabTime - step);
		}

		// ImGui rounds window positions to whole pixels, so keep this whole too or the switch slowly creeps up.
		_tabShown = Ease(_tabTime);
		_band = MathF.Round(TabHeight * _tabShown);

		if (_placed && _band != _drawnBand)
		{
			ImGui.SetNextWindowPos(new Vector2(_windowPos.X, _windowPos.Y - (_band - _drawnBand)), ImGuiCond.Always);
		}

		if (_baseHeight > 0f)
		{
			var height = _baseHeight + _band;
			ImGui.SetNextWindowSizeConstraints(
				new Vector2(_minimumWidth, height),
				new Vector2(MathF.Max(_minimumWidth, MaximumWidth * M3.Scale), height));
		}

		base.PreDraw();
	}

	public override void PostDraw()
	{
		base.PostDraw();
		_theme.Dispose();
		_theme = default;
	}

	public override void Draw()
	{
		var style = ImGui.GetStyle();
		var scale = M3.Scale;
		var width = ImGui.GetContentRegionAvail().X;
		var current = CurrentIndex();

		_windowPos = ImGui.GetWindowPos();
		_drawnBand = _band;
		_placed = true;

		var headerWidth = DrawHeader(width, scale, current);
		var switchWidth = DrawSegmentedButton(width, scale, current);

		_minimumWidth = MathF.Max(headerWidth, switchWidth) + (style.WindowPadding.X * 2f);
		_baseHeight = ImGui.GetCursorPosY() - style.ItemSpacing.Y + style.WindowPadding.Y - _band;
	}

	private static int CurrentIndex()
	{
		return !DataCenter.State ? 2 : DataCenter.IsManual ? 1 : 0;
	}

	private static int ManualAoeIndex()
	{
		return Service.Config.StateWindowManualAoEType == AoEType.Full ? 1 : 0;
	}

	private static float Ease(float t)
	{
		return t < 0.5f ? 4f * t * t * t : 1f - (MathF.Pow((-2f * t) + 2f, 3f) * 0.5f);
	}

	// The AoE type only changes if the state did. Duty replays refuse every state but Off.
	private static void SetState(int index, int previous)
	{
		RSCommands.SetStateCommandType(Segments[index].State);
		if (CurrentIndex() != index)
		{
			return;
		}

		if (Segments[index].State == StateCommandType.Auto)
		{
			Service.Config.AoEType = AoEType.Full;
		}
		else if (index == ManualIndex && previous != ManualIndex)
		{
			Service.Config.AoEType = AoeOptions[ManualAoeIndex()].Type;
		}
	}

	private static void SetManualAoe(int index)
	{
		var type = AoeOptions[index].Type;
		if (Service.Config.StateWindowManualAoEType != type)
		{
			Service.Config.StateWindowManualAoEType = type;
			Service.Config.Save();
		}

		if (CurrentIndex() == ManualIndex)
		{
			Service.Config.AoEType = type;
		}
	}

	private float DrawHeader(float width, float scale, int current)
	{
		var s = M3.Scheme;
		var drawList = ImGui.GetWindowDrawList();
		var origin = ImGui.GetCursorScreenPos();
		var pillSize = M3Widgets.WindowActionsSize(Actions.Length);
		var logoSize = 32f * scale;
		var logoGap = 12f * scale;
		var pillGap = 8f * scale;
		var lineGap = 2f * scale;
		var status = RSCommands.EntryString;

		Vector2 titleSize;
		using (ImRaii.PushFont(M3.TitleMedium))
		{
			titleSize = ImGui.CalcTextSize(Title);
		}

		var statusHeight = ImGui.GetTextLineHeight();
		var textHeight = titleSize.Y + lineGap + statusHeight;
		var height = MathF.Max(pillSize.Y, MathF.Max(logoSize, textHeight));

		var logoMin = new Vector2(origin.X, origin.Y + ((height - logoSize) * 0.5f));
		var logoMax = logoMin + new Vector2(logoSize, logoSize);
		var logo = MainWindow.GetLogoTexture();
		if (logo?.Handle != null)
		{
			drawList.AddImageRounded(logo.Handle, logoMin, logoMax, Vector2.Zero, Vector2.One, M3.U32(Vector4.One), M3.ShapeSmall);
		}
		else
		{
			drawList.AddRectFilled(logoMin, logoMax, M3.U32(s.PrimaryContainer), M3.ShapeSmall);
			M3Draw.IconCentered(drawList, FontAwesomeIcon.Fire, logoMin, logoMax, s.OnPrimaryContainer);
		}

		var textX = logoMax.X + logoGap;
		var textWidth = MathF.Max(0f, origin.X + width - pillSize.X - pillGap - textX);
		var textY = origin.Y + ((height - textHeight) * 0.5f);
		using (ImRaii.PushFont(M3.TitleMedium))
		{
			drawList.AddText(new Vector2(textX, textY), M3.U32(s.OnSurface), M3Navigation.Truncate(Title, textWidth));
		}

		drawList.AddText(new Vector2(textX, textY + titleSize.Y + lineGap), M3.U32(Segments[current].Accent(s)),
			M3Navigation.Truncate(status, textWidth));

		var pressed = M3Widgets.WindowActions("##state_actions",
			new Vector2(origin.X + width, origin.Y + ((height - pillSize.Y) * 0.5f)), Actions, out var closed, s.SurfaceContainerHigh);
		if (pressed == 0)
		{
			RotationSolverPlugin.ShowConfigWindow();
		}

		if (closed)
		{
			IsOpen = false;
		}

		ImGui.SetCursorScreenPos(origin);
		ImGui.Dummy(new Vector2(width, height));

		return logoSize + logoGap + titleSize.X + pillGap + pillSize.X;
	}

	private float DrawSegmentedButton(float width, float scale, int current)
	{
		var s = M3.Scheme;
		var height = 48f * scale;
		var pad = 4f * scale;
		var top = ImGui.GetCursorScreenPos();
		var origin = top + new Vector2(0f, _band);
		var segWidth = (width - (pad * 2f)) / Segments.Length;
		var segHeight = height - (pad * 2f);
		var radius = segHeight * 0.5f;
		var iconSize = 18f * scale;
		var gap = 8f * scale;
		var sideRoom = 8f * scale;
		var widestSegment = 0f;

		var tabPad = TabPadding;
		var tabSegHeight = TabSegmentHeight;
		var tabLabelWidth = 0f;
		foreach (var option in AoeOptions)
		{
			tabLabelWidth = MathF.Max(tabLabelWidth, ImGui.CalcTextSize(option.Label).X);
		}

		var tabSegWidth = tabLabelWidth + (28f * scale);
		var tabWidth = (tabSegWidth * AoeOptions.Length) + (tabPad * 2f);
		var manualCentre = origin.X + pad + (segWidth * (ManualIndex + 0.5f));
		var tabMin = new Vector2(manualCentre - (tabWidth * 0.5f), top.Y);
		var tabMax = new Vector2(tabMin.X + tabWidth, origin.Y);
		var tabVisible = _band >= 1.5f * scale;
		var tabInteractive = current == ManualIndex && _tabShown > 0.6f;

		var hovered = -1;
		var held = -1;
		var clicked = -1;
		for (var i = 0; i < Segments.Length; i++)
		{
			ImGui.SetCursorScreenPos(origin + new Vector2(pad + (segWidth * i), pad));
			if (ImGui.InvisibleButton($"##rsrState{Segments[i].State}", new Vector2(segWidth, segHeight)))
			{
				clicked = i;
			}

			if (ImGui.IsItemHovered())
			{
				hovered = i;
			}

			if (ImGui.IsItemActive())
			{
				held = i;
			}
		}

		var aoeHovered = -1;
		var aoeHeld = -1;
		var aoeClicked = -1;
		if (tabInteractive)
		{
			for (var i = 0; i < AoeOptions.Length; i++)
			{
				ImGui.SetCursorScreenPos(tabMin + new Vector2(tabPad + (tabSegWidth * i), tabPad));
				if (ImGui.InvisibleButton($"##rsrManualAoe{AoeOptions[i].Type}", new Vector2(tabSegWidth, tabSegHeight)))
				{
					aoeClicked = i;
				}

				if (ImGui.IsItemHovered())
				{
					aoeHovered = i;
					ImGui.SetTooltip(AoeOptions[i].Tooltip);
				}

				if (ImGui.IsItemActive())
				{
					aoeHeld = i;
				}
			}
		}

		var draw = ImGui.GetWindowDrawList();
		var trackFill = M3.U32(s.SurfaceContainerHighest);
		var trackOutline = M3.U32(s.OutlineVariant);

		draw.AddRectFilled(origin, origin + new Vector2(width, height), trackFill, height * 0.5f);
		draw.AddRect(origin, origin + new Vector2(width, height), trackOutline, height * 0.5f,
			ImDrawFlags.RoundCornersAll, scale);

		if (tabVisible)
		{
			DrawTabSurface(draw, tabMin, tabMax, tabSegHeight * 0.5f + tabPad, scale, trackFill, trackOutline);
		}

		var target = pad + (segWidth * current);
		if (_indicatorPos < 0f)
		{
			_indicatorPos = target;
			_indicatorWidth = segWidth;
		}
		else
		{
			var t = Math.Clamp(1f - MathF.Exp(-ImGui.GetIO().DeltaTime * 18f), 0f, 1f);
			_indicatorPos += (target - _indicatorPos) * t;
			_indicatorWidth += (segWidth - _indicatorWidth) * t;
		}

		var pillMin = origin + new Vector2(_indicatorPos, pad);
		draw.AddRectFilled(pillMin, pillMin + new Vector2(_indicatorWidth, segHeight),
			M3.U32(Segments[current].Container(s)), radius);

		using (ImRaii.PushFont(M3.TitleMedium))
		{
			var selectedContent = Segments[current].OnContainer(s);
			for (var i = 0; i < Segments.Length; i++)
			{
				var segMin = origin + new Vector2(pad + (segWidth * i), pad);
				var segMax = segMin + new Vector2(segWidth, segHeight);
				var centre = (segMin + segMax) * 0.5f;

				var centreLocal = pad + (segWidth * i) + (segWidth * 0.5f);
				var covered = _indicatorPos < centreLocal && _indicatorPos + _indicatorWidth > centreLocal;
				var content = covered ? selectedContent : s.OnSurfaceVariant;

				if (held == i || hovered == i)
				{
					draw.AddRectFilled(segMin, segMax,
						M3.U32(content, held == i ? M3.StatePressed : M3.StateHover), radius);
					ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
				}

				var label = Segments[i].Label;
				var labelSize = ImGui.CalcTextSize(label);
				var showIcon = i == current;
				var contentWidth = labelSize.X + (showIcon ? iconSize + gap : 0f);
				var contentLeft = centre.X - (contentWidth * 0.5f);

				widestSegment = MathF.Max(widestSegment, labelSize.X + iconSize + gap + (sideRoom * 2f));

				if (showIcon)
				{
					DrawCheck(new Vector2(contentLeft + (iconSize * 0.5f), centre.Y), iconSize, M3.U32(content), 2f * scale);
				}

				var textLeft = contentLeft + (showIcon ? iconSize + gap : 0f);
				draw.AddText(new Vector2(textLeft, centre.Y - (labelSize.Y * 0.5f)), M3.U32(content), label);
			}
		}

		if (tabVisible)
		{
			DrawTabChoices(draw, tabMin, tabMax, tabSegWidth, tabSegHeight, tabPad, aoeHovered, aoeHeld);
		}

		ImGui.SetCursorScreenPos(top);
		ImGui.Dummy(new Vector2(width, _band + height));

		if (clicked >= 0)
		{
			SetState(clicked, current);
		}

		if (aoeClicked >= 0)
		{
			SetManualAoe(aoeClicked);
		}

		var tabRoom = tabWidth + (((height * 0.5f) + TabFillet + scale) * 2f);
		return MathF.Max((widestSegment * Segments.Length) + (pad * 2f), tabRoom);
	}

	private static void DrawTabSurface(ImDrawListPtr draw, Vector2 min, Vector2 max, float cornerRadius, float scale, uint fill, uint outline)
	{
		var trackTop = max.Y;
		var rise = trackTop - min.Y;
		var fillet = MathF.Min(TabFillet, rise * 0.5f);
		var corner = MathF.Min(cornerRadius, rise - fillet);

		draw.AddRectFilled(min, max, fill, corner, ImDrawFlags.RoundCornersTop);
		draw.AddRectFilled(new Vector2(min.X - fillet, trackTop), new Vector2(max.X + fillet, trackTop + scale + 1.5f), fill);

		// Both fillets wind clockwise, which ImGui's anti-aliased fill needs.
		draw.PathLineTo(new Vector2(min.X, trackTop));
		draw.PathArcTo(new Vector2(min.X - fillet, trackTop - fillet), fillet, MathF.PI * 0.5f, 0f, 0);
		draw.PathFillConvex(fill);

		draw.PathLineTo(new Vector2(max.X, trackTop));
		draw.PathArcTo(new Vector2(max.X + fillet, trackTop - fillet), fillet, MathF.PI, MathF.PI * 0.5f, 0);
		draw.PathFillConvex(fill);

		var line = trackTop + 0.5f;
		var filletLine = fillet + 0.5f;
		var cornerLine = MathF.Max(0f, corner - 0.5f);
		draw.PathLineTo(new Vector2(min.X - fillet - 1f, line));
		draw.PathArcTo(new Vector2(min.X - fillet, trackTop - fillet), filletLine, MathF.PI * 0.5f, 0f, 0);
		draw.PathArcTo(new Vector2(min.X + corner, min.Y + corner), cornerLine, MathF.PI, MathF.PI * 1.5f, 0);
		draw.PathArcTo(new Vector2(max.X - corner, min.Y + corner), cornerLine, MathF.PI * 1.5f, MathF.PI * 2f, 0);
		draw.PathArcTo(new Vector2(max.X + fillet, trackTop - fillet), filletLine, MathF.PI, MathF.PI * 0.5f, 0);
		draw.PathLineTo(new Vector2(max.X + fillet + 1f, line));
		draw.PathStroke(outline, ImDrawFlags.None, scale);
	}

	private void DrawTabChoices(ImDrawListPtr draw, Vector2 tabMin, Vector2 tabMax, float segWidth, float segHeight, float pad, int hovered, int held)
	{
		var s = M3.Scheme;
		var radius = segHeight * 0.5f;
		var current = ManualAoeIndex();

		var target = pad + (segWidth * current);
		if (_aoeIndicatorPos < 0f)
		{
			_aoeIndicatorPos = target;
		}
		else
		{
			var t = Math.Clamp(1f - MathF.Exp(-ImGui.GetIO().DeltaTime * 18f), 0f, 1f);
			_aoeIndicatorPos += (target - _aoeIndicatorPos) * t;
		}

		using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha,
			ImGui.GetStyle().Alpha * Math.Clamp((_tabShown - 0.35f) / 0.65f, 0f, 1f));
		draw.PushClipRect(tabMin, tabMax, true);

		var pillMin = tabMin + new Vector2(_aoeIndicatorPos, pad);
		draw.AddRectFilled(pillMin, pillMin + new Vector2(segWidth, segHeight), M3.U32(s.TertiaryContainer), radius);

		for (var i = 0; i < AoeOptions.Length; i++)
		{
			var segMin = tabMin + new Vector2(pad + (segWidth * i), pad);
			var segMax = segMin + new Vector2(segWidth, segHeight);
			var centreLocal = pad + (segWidth * i) + (segWidth * 0.5f);
			var covered = _aoeIndicatorPos < centreLocal && _aoeIndicatorPos + segWidth > centreLocal;
			var content = covered ? s.OnTertiaryContainer : s.OnSurfaceVariant;

			if (held == i || hovered == i)
			{
				draw.AddRectFilled(segMin, segMax, M3.U32(content, held == i ? M3.StatePressed : M3.StateHover), radius);
				ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			}

			var label = AoeOptions[i].Label;
			var labelSize = ImGui.CalcTextSize(label);
			draw.AddText((segMin + segMax - labelSize) * 0.5f, M3.U32(content), label);
		}

		draw.PopClipRect();
	}

	private static void DrawCheck(Vector2 centre, float size, uint color, float thickness)
	{
		var draw = ImGui.GetWindowDrawList();
		var a = centre + (new Vector2(-0.38f, 0.02f) * size);
		var b = centre + (new Vector2(-0.12f, 0.28f) * size);
		var c = centre + (new Vector2(0.38f, -0.26f) * size);
		draw.AddLine(a, b, color, thickness);
		draw.AddLine(b, c, color, thickness);
		draw.AddCircleFilled(b, thickness * 0.5f, color);
	}
}
