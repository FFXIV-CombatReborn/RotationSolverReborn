using Dalamud.Interface.Utility.Raii;

namespace RotationSolver.UI.Material;

// Cards paint their background using last frame's height. Splitting the draw list instead breaks inside tables, which split it too.
internal static class M3CardHost
{
	private const int MaxTrackedCards = 512;

	private static readonly Dictionary<string, float> _measuredHeights = [];

	internal static float PreviousHeight(string id)
	{
		return _measuredHeights.TryGetValue(id, out var height) ? height : 0f;
	}

	internal static void RecordHeight(string id, float height)
	{
		if (_measuredHeights.Count > MaxTrackedCards)
		{
			_measuredHeights.Clear();
		}

		_measuredHeights[id] = height;
	}

	public static void Reset()
	{
		_measuredHeights.Clear();
	}
}

internal enum M3CardStyle
{
	Outlined,

	Filled,

	Elevated,
}

internal static class M3Card
{
	public static float ContentInset => M3Style.Spacing(16f, 10f);
	public static float RightInset => M3Style.Spacing(16f, 10f);
	public static float TopPadding => M3Style.Spacing(14f, 8f);
	public static float BottomPadding => M3Style.Spacing(14f, 8f);
	public static float Gap => M3Style.Spacing(10f, 6f);

	// The space left under a card, before whatever comes next.
	public static float Margin => M3Style.Spacing(8f, 4f);

	public static Scope Begin(string id, string? title = null, FontAwesomeIcon icon = FontAwesomeIcon.None, Vector4? accent = null, M3CardStyle style = M3CardStyle.Filled, string? subtitle = null)
	{
		var scale = M3.Scale;
		var tone = accent ?? M3.Scheme.Primary;

		var min = ImGui.GetCursorScreenPos();
		var width = MathF.Max(64f * scale, ImGui.GetContentRegionAvail().X);
		var cursorY = min.Y + TopPadding;

		var previous = M3CardHost.PreviousHeight(id);
		if (previous > 0f)
		{
			var max = new Vector2(min.X + width, min.Y + previous);
			Paint(ImGui.GetWindowDrawList(), min, max, tone, style, ImGui.IsMouseHoveringRect(min, max, false));
		}

		if (!string.IsNullOrEmpty(title))
		{
			var drawList = ImGui.GetWindowDrawList();
			var textX = min.X + ContentInset;

			if (icon != FontAwesomeIcon.None)
			{
				M3Draw.Icon(drawList, icon, new Vector2(textX, cursorY + (1f * scale)), tone);
				textX += M3Draw.MeasureIcon(icon).X + (10f * scale);
			}

			float titleHeight;
			using (ImRaii.PushFont(M3.TitleMedium))
			{
				var titleSize = ImGui.CalcTextSize(title);
				drawList.AddText(new Vector2(textX, cursorY), M3.U32(M3.Scheme.OnSurface, 0.98f), title);
				titleHeight = titleSize.Y;
			}

			cursorY += titleHeight + (4f * scale);

			if (!string.IsNullOrEmpty(subtitle))
			{
				cursorY = M3Draw.WrappedText(subtitle,
					new Vector2(min.X + ContentInset, cursorY),
					MathF.Max(32f * scale, width - ContentInset - RightInset),
					M3.Alpha(M3.Scheme.OnSurfaceVariant, 0.92f)) + (4f * scale);
			}

			cursorY += Gap - (4f * scale);
		}

		// Use Indent, not the cursor, so wrapped lines keep the inset.
		ImGui.Indent(ContentInset);
		PushContentWrap(min.X + width);
		ImGui.SetCursorScreenPos(new Vector2(min.X + ContentInset, cursorY));
		return new Scope(id, min, width, ContentInset);
	}

	internal static void PushContentWrap(float cardRightScreenX)
	{
		var localX = cardRightScreenX - RightInset - ImGui.GetWindowPos().X + ImGui.GetScrollX();
		ImGui.PushTextWrapPos(localX);
	}

	internal static void Paint(ImDrawListPtr drawList, Vector2 min, Vector2 max, Vector4 accent, M3CardStyle style, bool hovered)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var rounding = M3.ShapeMedium;

		var fill = style switch
		{
			M3CardStyle.Outlined => M3.Alpha(s.SurfaceContainerLowest, 0.35f),
			M3CardStyle.Elevated => s.SurfaceContainerLow,
			_ => M3.Alpha(s.SurfaceContainerLow, 0.92f),
		};

		if (hovered)
		{
			fill = M3ColorMath.Mix(fill, accent, 0.05f) with { W = fill.W };
		}

		if (style == M3CardStyle.Elevated)
		{
			M3Draw.Elevation(drawList, min, max, rounding, 2);
		}

		drawList.AddRectFilled(min, max, M3.U32(fill), rounding);
		drawList.AddRect(min, max, M3.U32(s.OutlineVariant, hovered ? 0.85f : 0.55f), rounding, ImDrawFlags.None, 1f * scale);

		var railTop = min.Y + (2f * scale);
		var railBottom = max.Y - (2f * scale);
		M3Draw.AccentRail(drawList, min.X + (2f * scale), 3f * scale, railTop, railBottom,
			M3.Alpha(accent, hovered ? 0.95f : 0.8f),
			M3Draw.ResolveFade(80f, 0.35f, railBottom - railTop));
	}

	internal readonly struct Scope(string id, Vector2 min, float width, float indent) : IDisposable
	{
		public void Dispose()
		{
			var scale = M3.Scale;
			var contentBottom = ImGui.GetCursorScreenPos().Y;

			ImGui.PopTextWrapPos();
			ImGui.Unindent(indent);

			var bottom = MathF.Max(contentBottom + BottomPadding, min.Y + (40f * scale));
			M3CardHost.RecordHeight(id, bottom - min.Y);

			ImGui.SetCursorScreenPos(new Vector2(min.X, bottom + Margin));
			ImGui.Dummy(new Vector2(width, 0f));
		}
	}
}

internal static class M3ExpandableCard
{
	public static Scope Begin(string id, string title, ref bool expanded, FontAwesomeIcon icon = FontAwesomeIcon.None, Vector4? accent = null, string? badge = null, Vector4? badgeAccent = null, string? trailing = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var tone = accent ?? s.Primary;

		var min = ImGui.GetCursorScreenPos();
		var width = MathF.Max(64f * scale, ImGui.GetContentRegionAvail().X);
		var headerHeight = M3Style.Density == M3Density.Tight ? M3.FitText(36f, 8f) : M3.FitText(48f, 12f);

		var previousHeight = MathF.Max(M3CardHost.PreviousHeight(id), headerHeight);
		Paint(ImGui.GetWindowDrawList(), min, new Vector2(min.X + width, min.Y + previousHeight), tone, expanded);

		if (ImGui.InvisibleButton($"{id}_header", new Vector2(width, headerHeight)))
		{
			expanded = !expanded;
		}

		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = ImGui.IsItemActive();
		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		var drawList = ImGui.GetWindowDrawList();
		var progress = M3Motion.Approach($"{id}_chevron", expanded ? 1f : 0f, M3Motion.EmphasisedDuration);

		if (hovered || held)
		{
			drawList.AddRectFilled(min, new Vector2(min.X + width, min.Y + headerHeight),
				M3.U32(s.OnSurface, held ? M3.StatePressed : M3.StateHover), M3.ShapeMedium);
		}

		var textX = min.X + M3Card.ContentInset;
		if (icon != FontAwesomeIcon.None)
		{
			var iconSize = M3Draw.MeasureIcon(icon);
			M3Draw.Icon(drawList, icon, new Vector2(textX, min.Y + ((headerHeight - iconSize.Y) * 0.5f)), tone);
			textX += iconSize.X + (12f * scale);
		}

		var chevronCenter = new Vector2(min.X + width - M3Card.RightInset - (8f * scale), min.Y + (headerHeight * 0.5f));

		var contentRight = chevronCenter.X - (20f * scale);
		var badgePadding = new Vector2(8f, 3f) * scale;
		var badgeSize = Vector2.Zero;
		if (!string.IsNullOrEmpty(badge))
		{
			using var badgeFont = ImRaii.PushFont(M3.LabelSmall);
			badgeSize = ImGui.CalcTextSize(badge) + (badgePadding * 2f);
		}

		var badgeRoom = badgeSize.X > 0f ? badgeSize.X + (10f * scale) : 0f;
		var trailingSize = string.IsNullOrEmpty(trailing) ? Vector2.Zero : ImGui.CalcTextSize(trailing);
		var trailingRoom = trailingSize.X > 0f ? trailingSize.X + (16f * scale) : 0f;

		using (ImRaii.PushFont(M3.TitleMedium))
		{
			var titleRoom = contentRight - textX - badgeRoom;
			if (ImGui.CalcTextSize(title).X > titleRoom - trailingRoom)
			{
				trailingRoom = 0f;
			}

			var label = M3Navigation.Truncate(title, MathF.Max(24f * scale, titleRoom - trailingRoom));
			var titleSize = ImGui.CalcTextSize(label);
			drawList.AddText(new Vector2(textX, min.Y + ((headerHeight - titleSize.Y) * 0.5f)),
				M3.U32(expanded ? s.OnSurface : M3.Alpha(s.OnSurface, 0.88f)), label);
			textX += titleSize.X + (10f * scale);
		}

		if (!string.IsNullOrEmpty(badge))
		{
			var badgeTone = badgeAccent ?? tone;
			using var badgeFont = ImRaii.PushFont(M3.LabelSmall);
			var badgeMin = new Vector2(textX, min.Y + ((headerHeight - badgeSize.Y) * 0.5f));
			drawList.AddRectFilled(badgeMin, badgeMin + badgeSize, M3.U32(badgeTone, 0.18f), M3.ShapeFull);
			drawList.AddText(badgeMin + badgePadding, M3.U32(badgeTone, 0.95f), badge);
		}

		if (trailingRoom > 0f && trailing is not null)
		{
			drawList.AddText(new Vector2(contentRight - trailingSize.X, min.Y + ((headerHeight - trailingSize.Y) * 0.5f)),
				M3.U32(s.OnSurfaceVariant, 0.85f), trailing);
		}

		DrawChevron(drawList, chevronCenter, progress, M3.Alpha(s.OnSurfaceVariant, hovered ? 1f : 0.8f));

		// Follow the open state, not the animation, or the divider gets left under a closed section.
		var indent = 0f;
		if (expanded)
		{
			indent = M3Card.ContentInset;
			ImGui.Indent(indent);
			M3Card.PushContentWrap(min.X + width);

			drawList.AddLine(
				new Vector2(min.X + indent, min.Y + headerHeight),
				new Vector2(min.X + width - M3Card.RightInset, min.Y + headerHeight),
				M3.U32(s.OutlineVariant, 0.5f), 1f * scale);

			ImGui.SetCursorScreenPos(new Vector2(min.X + indent, min.Y + headerHeight));
			ImGui.Dummy(new Vector2(0f, M3Card.Gap));
		}
		else
		{
			ImGui.SetCursorScreenPos(new Vector2(min.X, min.Y + headerHeight - M3Card.BottomPadding));
		}

		return new Scope(id, min, width, indent, expanded);
	}

	private static void DrawChevron(ImDrawListPtr drawList, Vector2 center, float progress, Vector4 color)
	{
		var scale = M3.Scale;
		var arm = 5f * scale;
		var angle = float.Lerp(0f, MathF.PI * 0.5f, progress);
		var cos = MathF.Cos(angle);
		var sin = MathF.Sin(angle);

		Vector2 Rotate(Vector2 point)
		{
			return center + new Vector2((point.X * cos) - (point.Y * sin), (point.X * sin) + (point.Y * cos));
		}

		var left = Rotate(new Vector2(-arm, -arm * 0.6f));
		var tip = Rotate(new Vector2(0f, arm * 0.6f));
		var right = Rotate(new Vector2(arm, -arm * 0.6f));

		drawList.AddLine(left, tip, M3.U32(color), 2f * scale);
		drawList.AddLine(tip, right, M3.U32(color), 2f * scale);
	}

	private static void Paint(ImDrawListPtr drawList, Vector2 min, Vector2 max, Vector4 accent, bool expanded)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var rounding = M3.ShapeMedium;
		var hovered = ImGui.IsMouseHoveringRect(min, max, false);
		var fill = expanded
			? M3.Alpha(s.SurfaceContainerLow, 0.95f)
			: M3.Alpha(s.SurfaceContainerLowest, hovered ? 0.85f : 0.65f);

		drawList.AddRectFilled(min, max, M3.U32(fill), rounding);
		drawList.AddRect(min, max, M3.U32(s.OutlineVariant, expanded ? 0.8f : 0.5f), rounding, ImDrawFlags.None, 1f * scale);

		if (expanded)
		{
			var railTop = min.Y + (2f * scale);
			var railBottom = max.Y - (2f * scale);
			M3Draw.AccentRail(drawList, min.X + (2f * scale), 3f * scale, railTop, railBottom,
				M3.Alpha(accent, 0.9f), M3Draw.ResolveFade(80f, 0.35f, railBottom - railTop));
		}
	}

	internal readonly struct Scope(string id, Vector2 min, float width, float indent, bool expanded) : IDisposable
	{
		public bool Expanded => expanded;

		public void Dispose()
		{
			var contentBottom = ImGui.GetCursorScreenPos().Y;

			if (indent > 0f)
			{
				ImGui.PopTextWrapPos();
				ImGui.Unindent(indent);
			}

			var bottom = contentBottom + (expanded ? M3Card.BottomPadding : 0f);
			M3CardHost.RecordHeight(id, bottom - min.Y);

			ImGui.SetCursorScreenPos(new Vector2(min.X, bottom + M3Card.Margin));
			ImGui.Dummy(new Vector2(width, 0f));
		}
	}
}
