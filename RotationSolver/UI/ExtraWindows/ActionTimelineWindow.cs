using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using RebornMaterial;
using RotationSolver.ActionTimeline;

namespace RotationSolver.UI.ExtraWindows;

internal class ActionTimelineWindow : Window
{
	private const ImGuiWindowFlags BaseFlags = ImGuiWindowFlags.NoTitleBar
		| ImGuiWindowFlags.NoCollapse
		| ImGuiWindowFlags.NoScrollbar
		| ImGuiWindowFlags.NoScrollWithMouse
		| ImGuiWindowFlags.NoNav
		| ImGuiWindowFlags.NoFocusOnAppearing;

	private const float SecondWidth = 80f;
	private const float GcdLaneHeight = 48f;
	private const float OgcdLaneHeight = 36f;
	private const float LaneGap = 6f;
	private const float IconInset = 3f;
	private const float LockLineHeight = 2f;
	private const float AutoAttackRadius = 2.5f;
	private const float NowCapRadius = 3.5f;

	private const float FutureSeconds = 2f;

	private const string EmptyText = "Actions show up here as you use them.";

	private static readonly int[] AxisSteps = [1, 2, 5, 10, 15, 30, 60];

	private static readonly M3WindowAction[] UnlockedActions =
	[
		new("##timeline_lock", FontAwesomeIcon.LockOpen, "Lock the timeline in place"),
		new("##timeline_settings", FontAwesomeIcon.Cog, "Open the timeline settings"),
	];

	private static readonly M3WindowAction[] LockedActions =
	[
		new("##timeline_lock", FontAwesomeIcon.Lock, "Locked in place. Click to allow moving and resizing."),
		new("##timeline_settings", FontAwesomeIcon.Cog, "Open the timeline settings"),
	];

	private static readonly string[] _axisLabels = new string[256];

	private readonly record struct Axis(DateTime Now, float NowX, float SecondWidth, float Left, float Right)
	{
		public float X(DateTime time)
		{
			return NowX + ((float)(time - Now).TotalSeconds * SecondWidth);
		}

		public float Width(float seconds)
		{
			return seconds * SecondWidth;
		}
	}

	private readonly List<TimelineItem> _items = new(64);

	private M3.WindowScaleScope _scale;
	private M3Style.Scope _theme;

	private float _contentHeight;
	private float _minimumWidth;

	public ActionTimelineWindow()
		: base(nameof(ActionTimelineWindow), BaseFlags)
	{
		Size = new Vector2(560f, 110f);
		SizeCondition = ImGuiCond.FirstUseEver;
		Position = new Vector2(200f, 200f);
		PositionCondition = ImGuiCond.FirstUseEver;

		// Opens and closes with combat, so Escape shouldn't close it.
		RespectCloseHotkey = false;

		AllowPinning = false;
		AllowClickthrough = false;
	}

	public override void PreDraw()
	{
		_scale = M3.PushWindowScale(Service.Config.ActionTimelineWindowScale);
		_theme = M3Style.Push(M3Density.Compact);

		ImGui.PushStyleColor(ImGuiCol.WindowBg, Service.Config.InfoWindowBg);

		Flags = BaseFlags;
		if (Service.Config.IsActionTimelineLock)
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
		var config = Service.Config;
		var s = M3.Scheme;
		var scale = M3.Scale;
		var style = ImGui.GetStyle();
		var drawList = ImGui.GetWindowDrawList();

		var origin = ImGui.GetCursorScreenPos();
		var width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
		var showOgcd = config.ActionTimelineShowOgcd;
		var showAutoAttacks = config.ActionTimelineShowAutoAttack;
		var laneGap = LaneGap * scale;

		float labelHeight;
		float labelWidth;
		using (ImRaii.PushFont(M3.LabelSmall))
		{
			labelHeight = ImGui.GetTextLineHeight();
			labelWidth = ImGui.CalcTextSize("-00s").X;
		}

		var gcdTop = origin.Y;
		var gcdBottom = gcdTop + (GcdLaneHeight * scale);
		var ogcdTop = gcdBottom + laneGap;
		var ogcdBottom = ogcdTop + (OgcdLaneHeight * scale);
		var lanesBottom = showOgcd ? ogcdBottom : gcdBottom;
		var axisTop = lanesBottom + laneGap;
		var height = axisTop + labelHeight - origin.Y;

		ImGui.Dummy(new Vector2(width, height));

		var pillSize = M3Widgets.WindowActionsSize(UnlockedActions.Length);
		_contentHeight = height + (style.WindowPadding.Y * 2f);
		_minimumWidth = MathF.Max(pillSize.X + (M3.Space1 * 2f), 4f * SecondWidth * scale) + (style.WindowPadding.X * 2f);

		var right = origin.X + width;
		var secondWidth = SecondWidth * scale;
		var future = MathF.Min(FutureSeconds, width / secondWidth * 0.3f);
		var axis = new Axis(DateTime.Now, right - (future * secondWidth), secondWidth, origin.X, right);

		_items.Clear();
		ActionTimelineManager.Current?.CollectItems(axis.Now.AddSeconds(-(axis.NowX - axis.Left) / secondWidth), _items);

		var windowPos = ImGui.GetWindowPos();
		var pillTopRight = new Vector2(windowPos.X + ImGui.GetWindowSize().X - M3.Space1, windowPos.Y + M3.Space1);
		var pillMin = pillTopRight - new Vector2(pillSize.X, 0f);
		var pillMax = pillMin + pillSize;
		var showPill = ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);

		var mouse = ImGui.GetMousePos();
		var canHover = ImGui.IsWindowHovered() && !ImGui.IsAnyItemActive() && !(showPill && Contains(pillMin, pillMax, mouse));
		TimelineItem? hovered = null;
		var hoveredMin = Vector2.Zero;
		var hoveredMax = Vector2.Zero;
		var drawn = 0;

		drawList.PushClipRect(origin, new Vector2(right, origin.Y + height), true);

		DrawLaneTrack(drawList, axis, gcdTop, gcdBottom);
		if (showOgcd)
		{
			DrawLaneTrack(drawList, axis, ogcdTop, ogcdBottom);
		}

		DrawAxis(drawList, axis, gcdTop, lanesBottom, axisTop, labelWidth);

		for (var i = _items.Count - 1; i >= 0; i--)
		{
			var item = _items[i];
			Vector2 min, max;
			var visible = item.Type switch
			{
				TimelineItemType.GCD => DrawGcd(drawList, item, axis, gcdTop, gcdBottom, out min, out max),
				TimelineItemType.OGCD when showOgcd => DrawOgcd(drawList, item, axis, ogcdTop, ogcdBottom, out min, out max),
				TimelineItemType.AutoAttack when showAutoAttacks => DrawAutoAttack(drawList, item, axis, lanesBottom + (laneGap * 0.5f), out min, out max),
				_ => Skip(out min, out max),
			};

			if (!visible)
			{
				continue;
			}

			drawn++;
			if (canHover && Contains(min, max, mouse))
			{
				hovered = item;
				hoveredMin = min;
				hoveredMax = max;
			}
		}

		DrawFuture(drawList, axis, gcdTop, gcdBottom);
		if (showOgcd)
		{
			DrawFuture(drawList, axis, ogcdTop, ogcdBottom);
		}

		DrawNowLine(drawList, axis, gcdTop, lanesBottom, axisTop);

		if (drawn == 0)
		{
			DrawEmpty(drawList, axis, gcdTop, lanesBottom);
		}

		if (hovered != null)
		{
			drawList.AddRectFilled(hoveredMin, hoveredMax, M3.U32(s.OnSurface, M3.StateHover), M3.ShapeSmall);
		}

		drawList.PopClipRect();

		if (hovered != null)
		{
			var item = hovered;
			var now = axis.Now;
			ImguiTooltips.ShowTooltip(() => DrawTooltip(item, now));
		}

		if (showPill)
		{
			DrawPill(pillTopRight);
		}
	}

	private static bool Contains(Vector2 min, Vector2 max, Vector2 point)
	{
		return point.X >= min.X && point.X <= max.X && point.Y >= min.Y && point.Y <= max.Y;
	}

	private static bool Skip(out Vector2 min, out Vector2 max)
	{
		min = max = Vector2.Zero;
		return false;
	}

	private void DrawPill(Vector2 topRight)
	{
		var config = Service.Config;
		bool locked = config.IsActionTimelineLock;

		var pressed = M3Widgets.WindowActions("##timeline_actions", topRight, locked ? LockedActions : UnlockedActions,
			out var closed, M3.Scheme.SurfaceContainerHigh, "Hide the timeline. Turn it back on in the UI settings.");

		if (pressed == 0)
		{
			config.IsActionTimelineLock.Value = !locked;
		}
		else if (pressed == 1)
		{
			RotationSolverPlugin.ShowConfigWindow(MainWindowTab.UI);
		}

		if (closed)
		{
			config.ShowActionTimelineWindow.Value = false;
			config.Save();
			IsOpen = false;
		}
	}

	#region Lanes

	private static void DrawLaneTrack(ImDrawListPtr drawList, in Axis axis, float top, float bottom)
	{
		drawList.AddRectFilled(new Vector2(axis.Left, top), new Vector2(axis.Right, bottom),
			M3.U32(M3.Scheme.SurfaceContainerHighest, 0.3f), M3.ShapeSmall);
	}

	private static void DrawFuture(ImDrawListPtr drawList, in Axis axis, float top, float bottom)
	{
		if (axis.NowX >= axis.Right)
		{
			return;
		}

		drawList.AddRectFilled(new Vector2(axis.NowX, top), new Vector2(axis.Right, bottom),
			M3.U32(M3.Scheme.Surface, 0.35f), M3.ShapeSmall, ImDrawFlags.RoundCornersRight);
	}

	private static bool DrawGcd(ImDrawListPtr drawList, TimelineItem item, in Axis axis, float top, float bottom, out Vector2 min, out Vector2 max)
	{
		var s = M3.Scheme;
		var laneHeight = bottom - top;
		var inset = IconInset * M3.Scale;

		var x0 = axis.X(item.StartTime);
		var x1 = MathF.Max(axis.X(item.EndTime), x0 + laneHeight);
		min = new Vector2(x0, top);
		max = new Vector2(x1, bottom);
		if (x1 < axis.Left || x0 > axis.Right)
		{
			return false;
		}

		var rounding = M3.ShapeSmall;
		var canceled = item.State == TimelineItemState.Canceled;
		var container = item.State switch
		{
			TimelineItemState.Casting => M3.Alpha(s.PrimaryContainer, 0.9f),
			TimelineItemState.Canceled => M3.Alpha(s.ErrorContainer, 0.75f),
			_ => M3.Alpha(s.SecondaryContainer, 0.85f),
		};
		drawList.AddRectFilled(min, max, M3.U32(container), rounding);

		if (item.CastingTime > 0f)
		{
			var accent = canceled ? s.Error : s.Primary;
			var castEnd = MathF.Min(x1, x0 + axis.Width(item.CastingTime));
			if (item.State == TimelineItemState.Casting)
			{
				FillFrom(drawList, min, max, castEnd, M3.Alpha(accent, 0.2f), rounding);
				FillFrom(drawList, min, max, Math.Clamp(axis.NowX, x0, castEnd), M3.Alpha(accent, 0.6f), rounding);
			}
			else
			{
				FillFrom(drawList, min, max, castEnd, M3.Alpha(accent, 0.4f), rounding);
			}
		}

		if (canceled)
		{
			drawList.AddRect(min, max, M3.U32(s.Error, 0.8f), rounding, ImDrawFlags.None, 1f * M3.Scale);
		}

		DrawLock(drawList, item, axis, x0, bottom - (inset * 0.5f));

		var iconX = MathF.Max(x0, MathF.Min(axis.Left, x1 - laneHeight)) + inset;
		DrawIcon(drawList, item, new Vector2(iconX, top + inset), laneHeight - (inset * 2f), canceled ? M3.DisabledContent : 1f);
		return true;
	}

	private static bool DrawOgcd(ImDrawListPtr drawList, TimelineItem item, in Axis axis, float top, float bottom, out Vector2 min, out Vector2 max)
	{
		var inset = IconInset * M3.Scale;
		var size = bottom - top - (inset * 2f);

		var x0 = axis.X(item.StartTime);
		var lockEnd = x0 + axis.Width(item.CastingTime + item.AnimationLockTime);
		min = new Vector2(x0, top);
		max = new Vector2(MathF.Max(x0 + size, lockEnd), bottom);
		if (max.X < axis.Left || x0 > axis.Right)
		{
			return false;
		}

		DrawLock(drawList, item, axis, x0, bottom - (inset * 0.5f));
		DrawIcon(drawList, item, new Vector2(x0, top + inset), size, 1f);
		return true;
	}

	private static bool DrawAutoAttack(ImDrawListPtr drawList, TimelineItem item, in Axis axis, float y, out Vector2 min, out Vector2 max)
	{
		var radius = AutoAttackRadius * M3.Scale;
		var center = new Vector2(axis.X(item.StartTime), y);

		var reach = new Vector2(radius * 2f, radius * 2f);
		min = center - reach;
		max = center + reach;
		if (center.X + radius < axis.Left || center.X - radius > axis.Right)
		{
			return false;
		}

		drawList.AddCircleFilled(center, radius, M3.U32(M3.Scheme.OnSurfaceVariant, 0.8f), 12);
		return true;
	}

	private static void DrawLock(ImDrawListPtr drawList, TimelineItem item, in Axis axis, float x0, float centerY)
	{
		if (item.AnimationLockTime <= 0f || item.State == TimelineItemState.Canceled)
		{
			return;
		}

		var half = LockLineHeight * M3.Scale * 0.5f;
		var start = x0 + axis.Width(item.CastingTime);
		var end = start + axis.Width(item.AnimationLockTime);
		drawList.AddRectFilled(new Vector2(start, centerY - half), new Vector2(end, centerY + half),
			M3.U32(M3.Scheme.Tertiary, 0.9f), half);
	}

	private static void FillFrom(ImDrawListPtr drawList, Vector2 min, Vector2 max, float end, Vector4 color, float rounding)
	{
		if (end <= min.X)
		{
			return;
		}

		var flags = end >= max.X - rounding ? ImDrawFlags.RoundCornersAll : ImDrawFlags.RoundCornersLeft;
		drawList.AddRectFilled(min, new Vector2(end, max.Y), M3.U32(color), rounding, flags);
	}

	private static void DrawIcon(ImDrawListPtr drawList, TimelineItem item, Vector2 min, float size, float alpha)
	{
		var max = min + new Vector2(size, size);
		var rounding = M3ActionIcon.Rounding(size);

		IDalamudTextureWrap? texture = null;
		if (item.Icon != 0 && IconSet.GetTexture(item.Icon, out var wrap))
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

	private static void DrawAxis(ImDrawListPtr drawList, in Axis axis, float top, float linesBottom, float labelTop, float labelWidth)
	{
		if (axis.SecondWidth <= 0f)
		{
			return;
		}

		var s = M3.Scheme;
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

		using var font = ImRaii.PushFont(M3.LabelSmall);
		for (var seconds = step; ; seconds += step)
		{
			var x = axis.NowX - (seconds * axis.SecondWidth);
			if (x < axis.Left)
			{
				break;
			}

			drawList.AddLine(new Vector2(x, top), new Vector2(x, linesBottom), lineColor, thickness);

			var label = AxisLabel(seconds);
			var labelX = x - (ImGui.CalcTextSize(label).X * 0.5f);
			if (labelX >= axis.Left)
			{
				drawList.AddText(new Vector2(labelX, labelTop), labelColor, label);
			}
		}
	}

	private static string AxisLabel(int seconds)
	{
		return seconds < _axisLabels.Length
			? _axisLabels[seconds] ??= $"-{seconds}s"
			: $"-{seconds}s";
	}

	private static void DrawNowLine(ImDrawListPtr drawList, in Axis axis, float top, float bottom, float labelTop)
	{
		var color = M3.U32(M3.Scheme.Primary);
		var scale = M3.Scale;
		var cap = NowCapRadius * scale;

		drawList.AddLine(new Vector2(axis.NowX, top + cap), new Vector2(axis.NowX, bottom), color, 2f * scale);
		drawList.AddCircleFilled(new Vector2(axis.NowX, top + cap), cap, color, 12);

		using var font = ImRaii.PushFont(M3.LabelSmall);
		const string label = "now";
		var labelWidth = ImGui.CalcTextSize(label).X;
		var labelX = MathF.Min(axis.NowX - (labelWidth * 0.5f), axis.Right - labelWidth);
		drawList.AddText(new Vector2(labelX, labelTop), color, label);
	}

	private static void DrawEmpty(ImDrawListPtr drawList, in Axis axis, float top, float bottom)
	{
		var size = ImGui.CalcTextSize(EmptyText);
		var room = axis.NowX - axis.Left - (M3.Space3 * 2f);
		if (size.X > room)
		{
			return;
		}

		var position = new Vector2(((axis.Left + axis.NowX) - size.X) * 0.5f, ((top + bottom) - size.Y) * 0.5f);
		drawList.AddText(position, M3.U32(M3.Scheme.OnSurfaceVariant, 0.7f), EmptyText);
	}

	#endregion

	#region Tooltip

	private static void DrawTooltip(TimelineItem item, DateTime now)
	{
		ImGui.TextUnformatted(string.IsNullOrEmpty(item.Name) ? "Unknown action" : item.Name);

		using var color = ImRaii.PushColor(ImGuiCol.Text, M3.Scheme.OnSurfaceVariant);
		ImGui.TextUnformatted(Describe(item));
		ImGui.TextUnformatted(item.State == TimelineItemState.Casting
			? "Casting now"
			: $"{(now - item.StartTime).TotalSeconds:F1}s ago");
	}

	private static string Describe(TimelineItem item)
	{
		var text = item.Type switch
		{
			TimelineItemType.GCD => "GCD",
			TimelineItemType.OGCD => "oGCD",
			_ => "Auto-attack",
		};

		if (item.State == TimelineItemState.Canceled)
		{
			text += $" - Cancelled after {item.CastingTime:F2}s";
			return text;
		}

		if (item.CastingTime > 0f)
		{
			text += $" - Cast {item.CastingTime:F2}s";
		}

		if (item.GCDTime > 0f)
		{
			text += $" - Recast {item.GCDTime:F2}s";
		}

		if (item.AnimationLockTime > 0f)
		{
			text += $" - Lock {item.AnimationLockTime:F2}s";
		}

		return text;
	}

	#endregion
}
