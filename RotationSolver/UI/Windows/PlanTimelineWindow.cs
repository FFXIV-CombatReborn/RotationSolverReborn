// CD Plan timeline (Beiruta rotations): the plan's actions coming up in the next few seconds, like the action timeline but looking ahead.
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using RebornMaterial;
using RotationSolver.Updaters;

namespace RotationSolver.UI.Windows;

internal class PlanTimelineWindow : Window
{
	private const ImGuiWindowFlags BaseFlags = ImGuiWindowFlags.NoTitleBar
		| ImGuiWindowFlags.NoCollapse
		| ImGuiWindowFlags.NoScrollbar
		| ImGuiWindowFlags.NoScrollWithMouse
		| ImGuiWindowFlags.NoNav
		| ImGuiWindowFlags.NoFocusOnAppearing;

	public const float AheadSeconds = 10f;
	public const string ShowConfigName = "ShowPlanTimeline";

	// Each lane: an icon row, then thin tracks under it for the action windows, so a long window never covers a later icon.
	private const float GcdIconSize = 34f;
	private const float OgcdIconSize = 28f;
	private const int WindowTracks = 3;
	private const float WindowThickness = 3f;
	private const float WindowTrackGap = 2f;
	private const float LaneGap = 6f;
	private const float IconInset = 3f;
	private const float NowCapRadius = 3.5f;
	private const float MinSecondWidth = 30f;

	private static readonly int[] AxisSteps = [1, 2, 5];

	private static readonly M3WindowAction[] UnlockedActions =
	[
		new("##plan_timeline_lock", FontAwesomeIcon.LockOpen, "Lock the CD Plan timeline in place"),
		new("##plan_timeline_settings", FontAwesomeIcon.Cog, "Open the CD Plan settings"),
	];

	private static readonly M3WindowAction[] LockedActions =
	[
		new("##plan_timeline_lock", FontAwesomeIcon.Lock, "Locked in place. Click to allow moving and resizing."),
		new("##plan_timeline_settings", FontAwesomeIcon.Cog, "Open the CD Plan settings"),
	];

	private readonly record struct Axis(float Now, float NowX, float SecondWidth, float Left, float Right)
	{
		public float X(float time) => NowX + ((time - Now) * SecondWidth);
	}

	private readonly List<BeirutaPlanPreview> _items = new(32);
	private readonly List<int> _tracks = new(32);
	private readonly List<float> _iconX = new(32);

	private M3.WindowScaleScope _scale;
	private M3Style.Scope _theme;

	private float _contentHeight;
	private float _minimumWidth;

	public PlanTimelineWindow()
		: base(nameof(PlanTimelineWindow), BaseFlags)
	{
		Size = new Vector2(560f, 110f);
		SizeCondition = ImGuiCond.FirstUseEver;
		Position = new Vector2(200f, 330f);
		PositionCondition = ImGuiCond.FirstUseEver;

		RespectCloseHotkey = false;

		AllowPinning = false;
		AllowClickthrough = false;
	}

	public override void PreDraw()
	{
		_scale = M3.PushWindowScale(1f);
		_theme = M3Style.Push(M3Window.DefaultDensity);

		ImGui.PushStyleColor(ImGuiCol.WindowBg, Service.Config.InfoWindowBg);

		Flags = BaseFlags;
		if (Service.Config.BeirutaPlanTimelineLock)
		{
			Flags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;
		}

		if (_contentHeight > 0f)
		{
			ImGui.SetNextWindowSizeConstraints(
				new Vector2(_minimumWidth, _contentHeight),
				new Vector2(float.MaxValue, _contentHeight));
		}

		base.PreDraw();
	}

	public override void PostDraw()
	{
		ImGui.PopStyleColor();
		base.PostDraw();
		_theme.Dispose();
		_theme = default;
		_scale.Dispose();
		_scale = default;
	}

	public override void Draw()
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var style = ImGui.GetStyle();
		var drawList = ImGui.GetWindowDrawList();

		var origin = ImGui.GetCursorScreenPos();
		var width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
		var laneGap = LaneGap * scale;

		float labelHeight;
		using (ImRaii.PushFont(M3.LabelSmall))
		{
			labelHeight = ImGui.GetTextLineHeight();
		}

		var gcdLane = new Lane(origin.Y, GcdIconSize * scale, scale);
		var ogcdLane = new Lane(gcdLane.Bottom + laneGap, OgcdIconSize * scale, scale);
		var gcdTop = gcdLane.Top;
		var ogcdBottom = ogcdLane.Bottom;
		var axisTop = ogcdBottom + laneGap;
		var height = axisTop + labelHeight - origin.Y;

		ImGui.Dummy(new Vector2(width, height));

		var pillSize = M3Widgets.WindowActionsSize(UnlockedActions.Length);
		_contentHeight = height + (style.WindowPadding.Y * 2f);
		_minimumWidth = MathF.Max(pillSize.X + (M3.Space1 * 2f), AheadSeconds * MinSecondWidth * scale) + (style.WindowPadding.X * 2f);

		var right = origin.X + width;
		var nowX = origin.X + (NowCapRadius * scale) + (1f * scale);
		var hasPlan = BeirutaTimelineUpdater.PreviewPlan(AheadSeconds, _items, out var now, out var pulled);
		var axis = new Axis(now, nowX, (right - nowX) / AheadSeconds, origin.X, right);

		var windowPos = ImGui.GetWindowPos();
		var pillTopRight = new Vector2(windowPos.X + ImGui.GetWindowSize().X - M3.Space1, windowPos.Y + M3.Space1);
		var pillMin = pillTopRight - new Vector2(pillSize.X, 0f);
		var pillMax = pillMin + pillSize;
		var showPill = ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);

		var mouse = ImGui.GetMousePos();
		var canHover = ImGui.IsWindowHovered() && !ImGui.IsAnyItemActive() && !(showPill && Contains(pillMin, pillMax, mouse));
		BeirutaPlanPreview? hovered = null;
		var hoveredMin = Vector2.Zero;
		var hoveredMax = Vector2.Zero;

		drawList.PushClipRect(origin, new Vector2(right, origin.Y + height), true);

		DrawLaneTrack(drawList, axis, gcdLane.Top, gcdLane.Bottom);
		DrawLaneTrack(drawList, axis, ogcdLane.Top, ogcdLane.Bottom);

		var status = !hasPlan ? "No plan loaded"
			: pulled ? $"{BeirutaTimelineUpdater.Status}"
			: Service.CountDownTime > 0f ? $"Pull in {Service.CountDownTime:F0}s"
			: "Waiting for pull";
		DrawAxis(drawList, axis, gcdTop, ogcdBottom, axisTop, status);

		AssignTracks(axis, gcdLane.IconSize, true);
		AssignTracks(axis, ogcdLane.IconSize, false);

		// Window lines first, then icons over them. Later icons first so the next one to come up is on top.
		for (var i = 0; i < _items.Count; i++)
		{
			var item = _items[i];
			DrawWindowLine(drawList, item, axis, item.Gcd ? gcdLane : ogcdLane, _tracks[i]);
		}

		for (var i = _items.Count - 1; i >= 0; i--)
		{
			var item = _items[i];
			if (!DrawItemIcon(drawList, item, axis, item.Gcd ? gcdLane : ogcdLane, _iconX[i], out var min, out var max))
			{
				continue;
			}

			if (canHover && Contains(min, max, mouse))
			{
				hovered = item;
				hoveredMin = min;
				hoveredMax = max;
			}
		}

		// Hovering a window line also picks its action, when no icon is under the mouse.
		if (canHover && hovered == null)
		{
			for (var i = 0; i < _items.Count; i++)
			{
				var item = _items[i];
				var (min, max) = WindowLineRect(item, axis, item.Gcd ? gcdLane : ogcdLane, _tracks[i]);
				var pad = new Vector2(0f, 2f * scale);
				if (Contains(min - pad, max + pad, mouse))
				{
					hovered = item;
					hoveredMin = min;
					hoveredMax = max;
					break;
				}
			}
		}

		DrawNowLine(drawList, axis, gcdTop, ogcdBottom);

		if (_items.Count == 0)
		{
			var text = hasPlan ? $"Nothing planned in the next {AheadSeconds:F0}s." : "Plan actions show up here in a duty with a CD plan.";
			var size = ImGui.CalcTextSize(text);
			var position = new Vector2(nowX + M3.Space3, ((gcdLane.Top + ogcdBottom) - size.Y) * 0.5f);
			drawList.AddText(position, M3.U32(s.OnSurfaceVariant, 0.7f), text);
		}

		if (hovered != null)
		{
			drawList.AddRectFilled(hoveredMin, hoveredMax, M3.U32(s.OnSurface, M3.StateHover), M3.ShapeSmall);
		}

		drawList.PopClipRect();

		if (hovered != null)
		{
			var item = hovered;
			ImguiTooltips.ShowTooltip(() => DrawTooltip(item, now, pulled));
		}

		if (showPill)
		{
			DrawPill(pillTopRight);
		}
	}

	private static bool Contains(Vector2 min, Vector2 max, Vector2 point) =>
		point.X >= min.X && point.X <= max.X && point.Y >= min.Y && point.Y <= max.Y;

	private void DrawPill(Vector2 topRight)
	{
		var config = Service.Config;
		var locked = config.BeirutaPlanTimelineLock;

		var pressed = M3Widgets.WindowActions("##plan_timeline_actions", topRight, locked ? LockedActions : UnlockedActions,
			out var closed, M3.Scheme.SurfaceContainerHigh, "Hide the CD Plan timeline. Turn it back on in the rotation's CD Plan section.");

		if (pressed == 0)
		{
			config.BeirutaPlanTimelineLock = !locked;
			config.Save();
		}
		else if (pressed == 1)
		{
			RotationSolverPlugin.ShowConfigWindow(MainWindowTab.Rotation);
		}

		if (closed)
		{
			if (DataCenter.CurrentRotation is { } rotation && MainWindow.FindRotationConfig(rotation, ShowConfigName) is { } show)
			{
				show.Value = false.ToString();
			}

			IsOpen = false;
		}
	}

	#region Lanes

	private static void DrawLaneTrack(ImDrawListPtr drawList, in Axis axis, float top, float bottom)
	{
		drawList.AddRectFilled(new Vector2(axis.Left, top), new Vector2(axis.Right, bottom),
			M3.U32(M3.Scheme.SurfaceContainerHighest, 0.3f), M3.ShapeSmall);
	}

	// One lane's layout: the icon row, then the window tracks under it.
	private readonly record struct Lane(float Top, float IconSize, float Scale)
	{
		public float Inset => IconInset * Scale;
		public float IconTop => Top + Inset;
		public float IconBottom => IconTop + IconSize;
		public float TrackStep => (WindowThickness + WindowTrackGap) * Scale;
		public float TrackY(int track) => IconBottom + Inset + (track * TrackStep);
		public float Bottom => TrackY(WindowTracks) + (Inset * 0.5f);
	}

	private static (Vector4 Line, Vector4? Outline, float Alpha) StateLook(BeirutaPlanPreviewState state)
	{
		var s = M3.Scheme;
		return state switch
		{
			BeirutaPlanPreviewState.Queued => (s.Primary, s.Primary, 1f),
			BeirutaPlanPreviewState.Waiting => (s.Tertiary, s.Tertiary, 1f),
			BeirutaPlanPreviewState.Done => (M3.Alpha(s.OnSurfaceVariant, 0.4f), null, 0.45f),
			BeirutaPlanPreviewState.Skipped => (M3.Alpha(s.Error, 0.7f), s.Error, M3.DisabledContent),
			BeirutaPlanPreviewState.Off => (M3.Alpha(s.OnSurfaceVariant, 0.25f), null, 0.35f),
			BeirutaPlanPreviewState.RotationRuns => (M3.Alpha(s.OnSurfaceVariant, 0.4f), s.OutlineVariant, 0.6f),
			_ => (M3.Alpha(s.Secondary, 0.85f), null, 1f),
		};
	}

	// Window line x range: from the window start (or "now" once open) to its end.
	private static (float X0, float X1) WindowSpan(BeirutaPlanPreview item, in Axis axis)
	{
		var x0 = MathF.Max(axis.X(item.Start), axis.NowX);
		return (x0, MathF.Max(axis.X(item.End), x0 + (2f * M3.Scale)));
	}

	// Puts each window line on the first free track, and slides an icon right when it would sit on the previous one.
	private void AssignTracks(in Axis axis, float iconSize, bool gcd)
	{
		while (_tracks.Count < _items.Count)
		{
			_tracks.Add(0);
			_iconX.Add(0f);
		}

		Span<float> trackEnd = stackalloc float[WindowTracks];
		trackEnd.Fill(float.MinValue);
		var gap = 2f * M3.Scale;
		var lastIconEnd = float.MinValue;
		for (var i = 0; i < _items.Count; i++)
		{
			var item = _items[i];
			if (item.Gcd != gcd)
			{
				continue;
			}

			var (x0, x1) = WindowSpan(item, axis);
			var track = -1;
			for (var k = 0; k < WindowTracks; k++)
			{
				if (trackEnd[k] + gap <= x0)
				{
					track = k;
					break;
				}
			}

			if (track < 0)
			{
				track = 0;
				for (var k = 1; k < WindowTracks; k++)
				{
					if (trackEnd[k] < trackEnd[track])
					{
						track = k;
					}
				}
			}

			trackEnd[track] = x1;
			_tracks[i] = track;

			var iconX = MathF.Max(x0, lastIconEnd + gap);
			_iconX[i] = iconX;
			lastIconEnd = iconX + iconSize;
		}
	}

	private static (Vector2 Min, Vector2 Max) WindowLineRect(BeirutaPlanPreview item, in Axis axis, in Lane lane, int track)
	{
		var (x0, x1) = WindowSpan(item, axis);
		var y = lane.TrackY(track);
		return (new Vector2(x0, y), new Vector2(x1, y + (WindowThickness * lane.Scale)));
	}

	// The action's window as a thin line on its track, with a tick up to the icon row at the start.
	private static void DrawWindowLine(ImDrawListPtr drawList, BeirutaPlanPreview item, in Axis axis, in Lane lane, int track)
	{
		var (min, max) = WindowLineRect(item, axis, lane, track);
		if (min.X > axis.Right)
		{
			return;
		}

		var (line, _, _) = StateLook(item.State);
		var color = M3.U32(line);
		drawList.AddRectFilled(min, max, color, (max.Y - min.Y) * 0.5f);
		drawList.AddLine(new Vector2(min.X + (0.5f * lane.Scale), lane.IconBottom), new Vector2(min.X + (0.5f * lane.Scale), max.Y), color, 1f * lane.Scale);
	}

	// The icon at the window start (or "now" once open), with the seconds until it opens in the corner.
	private static bool DrawItemIcon(ImDrawListPtr drawList, BeirutaPlanPreview item, in Axis axis, in Lane lane, float iconX, out Vector2 min, out Vector2 max)
	{
		var s = M3.Scheme;
		min = new Vector2(iconX, lane.IconTop);
		max = min + new Vector2(lane.IconSize, lane.IconSize);
		if (iconX > axis.Right)
		{
			return false;
		}

		var (_, outline, alpha) = StateLook(item.State);
		DrawIcon(drawList, item.Action.IconID, min, lane.IconSize, alpha);
		if (outline is { } o)
		{
			drawList.AddRect(min, max, M3.U32(o, 0.9f), M3ActionIcon.Rounding(lane.IconSize), ImDrawFlags.None, 2f * lane.Scale);
		}

		var startsIn = item.Start - axis.Now;
		if (startsIn >= 0.5f)
		{
			using var font = ImRaii.PushFont(M3.LabelSmall);
			var label = $"{startsIn:F0}";
			var size = ImGui.CalcTextSize(label);
			var pad = new Vector2(2f * lane.Scale, 0f);
			var textMin = max - size - pad;
			drawList.AddRectFilled(textMin - pad, max, M3.U32(s.Scrim, 0.65f), 2f * lane.Scale);
			drawList.AddText(textMin, M3.U32(s.OnSurface), label);
		}

		return true;
	}

	private static void DrawIcon(ImDrawListPtr drawList, uint icon, Vector2 min, float size, float alpha)
	{
		var max = min + new Vector2(size, size);
		var rounding = M3ActionIcon.Rounding(size);

		IDalamudTextureWrap? texture = null;
		if (icon != 0 && IconSet.GetTexture(icon, out var wrap))
		{
			texture = wrap;
		}

		if (!M3ActionIcon.Image(drawList, texture, min, max, rounding, alpha))
		{
			M3ActionIcon.EmptySlot(drawList, min, max, rounding);
			return;
		}

		drawList.AddRect(min, max, M3.U32(M3.Scheme.OutlineVariant, 0.8f), rounding, ImDrawFlags.None, 1f * M3.Scale);
	}

	#endregion

	#region Axis

	private static void DrawAxis(ImDrawListPtr drawList, in Axis axis, float top, float linesBottom, float labelTop, string status)
	{
		var s = M3.Scheme;
		using var font = ImRaii.PushFont(M3.LabelSmall);

		var statusWidth = ImGui.CalcTextSize(status).X;
		var statusX = axis.Right - statusWidth;
		drawList.AddText(new Vector2(statusX, labelTop), M3.U32(s.OnSurfaceVariant, 0.85f), status);

		var labelWidth = ImGui.CalcTextSize("+00s").X;
		var step = AxisSteps[^1];
		foreach (var candidate in AxisSteps)
		{
			if (candidate * axis.SecondWidth >= labelWidth + M3.Space2)
			{
				step = candidate;
				break;
			}
		}

		var lineColor = M3.U32(s.OutlineVariant, 0.45f);
		var labelColor = M3.U32(s.OnSurfaceVariant, 0.85f);
		var thickness = 1f * M3.Scale;
		for (var seconds = step; seconds <= AheadSeconds; seconds += step)
		{
			var x = axis.NowX + (seconds * axis.SecondWidth);
			if (x > axis.Right)
			{
				break;
			}

			drawList.AddLine(new Vector2(x, top), new Vector2(x, linesBottom), lineColor, thickness);

			var label = $"+{seconds}s";
			var w = ImGui.CalcTextSize(label).X;
			var labelX = x - (w * 0.5f);
			if (labelX + w < statusX - M3.Space2)
			{
				drawList.AddText(new Vector2(labelX, labelTop), labelColor, label);
			}
		}
	}

	private static void DrawNowLine(ImDrawListPtr drawList, in Axis axis, float top, float bottom)
	{
		var color = M3.U32(M3.Scheme.Primary);
		var scale = M3.Scale;
		var cap = NowCapRadius * scale;

		drawList.AddLine(new Vector2(axis.NowX, top + cap), new Vector2(axis.NowX, bottom), color, 2f * scale);
		drawList.AddCircleFilled(new Vector2(axis.NowX, top + cap), cap, color, 12);
	}

	#endregion

	#region Tooltip

	private static void DrawTooltip(BeirutaPlanPreview item, float now, bool pulled)
	{
		ImGui.TextUnformatted(item.Action.Name);

		using var color = ImRaii.PushColor(ImGuiCol.Text, M3.Scheme.OnSurfaceVariant);
		var target = item.Target.Length > 0 ? $" - {item.Target}" : string.Empty;
		ImGui.TextUnformatted($"{(item.Gcd ? "GCD" : "oGCD")} - {item.Phase} {item.Start:F1}-{item.End:F1}s{target}");

		var when = item.Start > now
			? $"Window opens in {item.Start - now:F1}s"
			: $"Window open, closes in {item.End - now:F1}s";
		ImGui.TextUnformatted(pulled ? when : $"{when} (preview before the pull)");

		var state = item.State switch
		{
			BeirutaPlanPreviewState.Queued => "Queued",
			BeirutaPlanPreviewState.Waiting => $"Waiting: {item.Note}",
			BeirutaPlanPreviewState.Done => "Used",
			BeirutaPlanPreviewState.Skipped => "Skipped or missed",
			BeirutaPlanPreviewState.Off => "Off in your settings",
			BeirutaPlanPreviewState.RotationRuns => "The rotation decides when to use this",
			_ => null,
		};

		if (state != null)
		{
			ImGui.TextUnformatted(state);
		}
	}

	#endregion
}
