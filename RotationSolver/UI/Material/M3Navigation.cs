using Dalamud.Interface.Utility.Raii;

namespace RotationSolver.UI.Material;

internal readonly record struct M3NavItem(
	string Id,
	string Label,
	FontAwesomeIcon Icon,
	bool Selected,
	string? Tooltip = null,
	Vector4? Accent = null,
	string? Badge = null,
	bool SeparatorAfter = false);

internal readonly record struct M3Tab(
	string Label,
	FontAwesomeIcon Icon = FontAwesomeIcon.None,
	string? Badge = null,
	string? Tooltip = null);

internal static class M3Navigation
{
	public const float DrawerBreakpoint = 128f;

	private static float DrawerRowHeight => M3Style.Density == M3Density.Tight ? M3.FitText(36f, 8f) : M3.FitText(48f, 12f);

	private static float RailItemHeight
	{
		get
		{
			using var font = ImRaii.PushFont(M3.LabelSmall);
			return MathF.Max(58f * M3.Scale, (44f * M3.Scale) + ImGui.GetTextLineHeight());
		}
	}

	public static string? Draw(string id, IReadOnlyList<M3NavItem> items, bool expanded)
	{
		string? clicked = null;

		using var idScope = ImRaii.PushId(id);
		for (var i = 0; i < items.Count; i++)
		{
			var item = items[i];
			if (expanded ? DrawerItem(item) : RailItem(item))
			{
				clicked = item.Id;
			}

			if (item.SeparatorAfter)
			{
				M3Widgets.Divider(6f);
			}
		}

		return clicked;
	}

	private static bool DrawerItem(in M3NavItem item)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var tone = item.Accent ?? s.Primary;
		var width = MathF.Max(32f * scale, ImGui.GetContentRegionAvail().X);
		var height = DrawerRowHeight;

		var pressed = ImGui.InvisibleButton($"nav_{item.Id}", new Vector2(width, height));
		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var rounding = height * 0.5f;

		var selection = M3Motion.Approach($"nav_sel_{item.Id}", item.Selected ? 1f : 0f, M3Motion.EmphasisedDuration);

		if (selection > 0.01f)
		{
			drawList.AddRectFilled(min, max, M3.U32(s.SecondaryContainer, 0.95f * selection), rounding);
		}

		if (hovered || held)
		{
			drawList.AddRectFilled(min, max, M3.U32(s.OnSurface, held ? M3.StatePressed : M3.StateHover), rounding);
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		var iconColor = M3ColorMath.Mix(s.OnSurfaceVariant, tone, selection);
		var textColor = M3ColorMath.Mix(M3.Alpha(s.OnSurfaceVariant, 0.95f), s.OnSecondaryContainer, selection);

		var iconSize = M3Draw.MeasureIcon(item.Icon);
		var iconX = min.X + M3Style.Spacing(16f, 12f);
		M3Draw.Icon(drawList, item.Icon, new Vector2(iconX, min.Y + ((height - iconSize.Y) * 0.5f)), iconColor);

		var textX = iconX + MathF.Max(iconSize.X, 18f * scale) + M3Style.Spacing(14f, 10f);
		var available = MathF.Max(16f * scale, max.X - textX - (12f * scale));
		var label = Truncate(item.Label, available);
		var labelSize = ImGui.CalcTextSize(label);
		drawList.AddText(new Vector2(textX, min.Y + ((height - labelSize.Y) * 0.5f)), M3.U32(textColor), label);

		if (!string.IsNullOrEmpty(item.Badge))
		{
			using var badgeFont = ImRaii.PushFont(M3.LabelSmall);
			var badgeSize = ImGui.CalcTextSize(item.Badge);
			var padding = new Vector2(6f, 2f) * scale;
			var badgeMax = new Vector2(max.X - (12f * scale), min.Y + ((height + badgeSize.Y + (padding.Y * 2f)) * 0.5f));
			var badgeMin = badgeMax - badgeSize - (padding * 2f);
			drawList.AddRectFilled(badgeMin, badgeMax, M3.U32(s.Error, 0.9f), M3.ShapeFull);
			drawList.AddText(badgeMin + padding, M3.U32(s.OnError), item.Badge);
		}

		if (hovered && !string.IsNullOrEmpty(item.Tooltip))
		{
			ImguiTooltips.ShowTooltip(item.Tooltip);
		}

		return pressed;
	}

	private static bool RailItem(in M3NavItem item)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var tone = item.Accent ?? s.Primary;
		var width = MathF.Max(32f * scale, ImGui.GetContentRegionAvail().X);
		var height = RailItemHeight;

		var pressed = ImGui.InvisibleButton($"nav_{item.Id}", new Vector2(width, height));
		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var drawList = ImGui.GetWindowDrawList();
		var selection = M3Motion.Approach($"nav_sel_{item.Id}", item.Selected ? 1f : 0f, M3Motion.EmphasisedDuration);

		var indicatorSize = new Vector2(MathF.Min(56f * scale, width - (4f * scale)), 32f * scale);
		var indicatorMin = new Vector2(min.X + ((width - indicatorSize.X) * 0.5f), min.Y + (4f * scale));
		var indicatorMax = indicatorMin + indicatorSize;

		if (selection > 0.01f)
		{
			var grow = (1f - selection) * 6f * scale;
			drawList.AddRectFilled(
				indicatorMin + new Vector2(grow, 0f),
				indicatorMax - new Vector2(grow, 0f),
				M3.U32(s.SecondaryContainer, 0.95f * selection), indicatorSize.Y * 0.5f);
		}

		if (hovered || held)
		{
			drawList.AddRectFilled(indicatorMin, indicatorMax,
				M3.U32(s.OnSurface, held ? M3.StatePressed : M3.StateHover), indicatorSize.Y * 0.5f);
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		var iconColor = M3ColorMath.Mix(s.OnSurfaceVariant, tone, selection);
		M3Draw.IconCentered(drawList, item.Icon, indicatorMin, indicatorMax, iconColor);

		using (ImRaii.PushFont(M3.LabelSmall))
		{
			var label = Truncate(item.Label, width - (4f * scale));
			var labelSize = ImGui.CalcTextSize(label);
			var textColor = M3ColorMath.Mix(M3.Alpha(s.OnSurfaceVariant, 0.9f), s.OnSurface, selection);
			drawList.AddText(
				new Vector2(min.X + ((width - labelSize.X) * 0.5f), indicatorMax.Y + (4f * scale)),
				M3.U32(textColor), label);
		}

		if (hovered)
		{
			var tip = string.IsNullOrEmpty(item.Tooltip) ? item.Label : $"{item.Label}\n \n{item.Tooltip}";
			ImguiTooltips.ShowTooltip(tip);
		}

		return pressed;
	}

	public static int Tabs(string id, ReadOnlySpan<M3Tab> tabs, int selectedIndex, bool secondary = false, float? width = null)
	{
		if (tabs.Length == 0)
		{
			return -1;
		}

		var s = M3.Scheme;
		var scale = M3.Scale;

		var stacked = false;
		foreach (var tab in tabs)
		{
			stacked |= !secondary && tab.Icon != FontAwesomeIcon.None;
		}

		var lineHeight = ImGui.GetTextLineHeight();
		var height = stacked
			? MathF.Max(64f * scale, (lineHeight * 2f) + (26f * scale))
			: M3.FitText(48f, 12f);
		var total = width ?? MathF.Max(32f * scale, ImGui.GetContentRegionAvail().X);
		var tabWidth = total / tabs.Length;
		var origin = ImGui.GetCursorScreenPos();
		var drawList = ImGui.GetWindowDrawList();
		var result = -1;

		var indicatorX = 0f;
		var indicatorWidth = tabWidth;

		using var idScope = ImRaii.PushId(id);

		for (var i = 0; i < tabs.Length; i++)
		{
			var tab = tabs[i];
			var selected = i == selectedIndex;
			var tabMin = new Vector2(origin.X + (tabWidth * i), origin.Y);
			var tabMax = tabMin + new Vector2(tabWidth, height);

			ImGui.SetCursorScreenPos(tabMin);
			if (ImGui.InvisibleButton($"##tab{i}", new Vector2(tabWidth, height)) && !selected)
			{
				result = i;
			}

			var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
			var held = ImGui.IsItemActive();
			if (hovered || held)
			{
				drawList.AddRectFilled(tabMin, tabMax, M3.U32(selected && !secondary ? s.Primary : s.OnSurface,
					held ? M3.StatePressed : M3.StateHover));
				ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			}

			var content = !selected ? s.OnSurfaceVariant : secondary ? s.OnSurface : s.Primary;
			var hasIcon = tab.Icon != FontAwesomeIcon.None;
			var iconSize = hasIcon ? M3Draw.MeasureIcon(tab.Icon) : Vector2.Zero;
			var inlineGap = hasIcon && !stacked ? 8f * scale : 0f;
			var inlineIcon = hasIcon && !stacked ? iconSize.X + inlineGap : 0f;

			var badgeSize = string.IsNullOrEmpty(tab.Badge) ? Vector2.Zero : M3Widgets.BadgeSize(tab.Badge);
			var badgeRoom = badgeSize.X > 0f ? (badgeSize.X + (4f * scale)) * 2f : 0f;
			var label = Truncate(tab.Label, MathF.Max(8f * scale, tabWidth - (16f * scale) - inlineIcon - badgeRoom));
			var labelSize = ImGui.CalcTextSize(label);

			float contentLeft;
			float contentWidth;
			Vector2 labelPosition;
			if (stacked)
			{
				var blockHeight = (hasIcon ? iconSize.Y + (4f * scale) : 0f) + labelSize.Y;
				var top = tabMin.Y + ((height - blockHeight) * 0.5f) - (1.5f * scale);
				if (hasIcon)
				{
					M3Draw.Icon(drawList, tab.Icon, new Vector2(tabMin.X + ((tabWidth - iconSize.X) * 0.5f), top), content);
					top += iconSize.Y + (4f * scale);
				}

				labelPosition = new Vector2(tabMin.X + ((tabWidth - labelSize.X) * 0.5f), top);
				contentWidth = MathF.Max(labelSize.X, iconSize.X);
				contentLeft = tabMin.X + ((tabWidth - contentWidth) * 0.5f);
			}
			else
			{
				contentWidth = inlineIcon + labelSize.X;
				contentLeft = tabMin.X + ((tabWidth - contentWidth) * 0.5f);
				if (hasIcon)
				{
					M3Draw.Icon(drawList, tab.Icon, new Vector2(contentLeft, tabMin.Y + ((height - iconSize.Y) * 0.5f)), content);
				}

				labelPosition = new Vector2(contentLeft + inlineIcon, tabMin.Y + ((height - labelSize.Y) * 0.5f));
			}

			drawList.AddText(labelPosition, M3.U32(content), label);

			if (badgeSize.X > 0f)
			{
				M3Widgets.BadgeAt(new Vector2(labelPosition.X + labelSize.X + (4f * scale) + (badgeSize.X * 0.5f), labelPosition.Y + (badgeSize.Y * 0.5f)), tab.Badge);
			}

			if (selected)
			{
				indicatorWidth = secondary ? tabWidth : MathF.Max(contentWidth, 24f * scale);
				indicatorX = secondary ? tabMin.X - origin.X : contentLeft + ((contentWidth - indicatorWidth) * 0.5f) - origin.X;
			}

			if (hovered && !string.IsNullOrEmpty(tab.Tooltip))
			{
				ImguiTooltips.ShowTooltip(tab.Tooltip);
			}
		}

		var divider = 1f * scale;
		drawList.AddLine(new Vector2(origin.X, origin.Y + height - (divider * 0.5f)), new Vector2(origin.X + total, origin.Y + height - (divider * 0.5f)),
			M3.U32(s.OutlineVariant, 0.8f), divider);

		if (selectedIndex >= 0 && selectedIndex < tabs.Length)
		{
			var x = M3Motion.Approach("indicator_x", indicatorX, M3Motion.EmphasisedDuration);
			var w = M3Motion.Approach("indicator_w", indicatorWidth, M3Motion.EmphasisedDuration);
			var thickness = (secondary ? 2f : 3f) * scale;
			var indicatorMin = new Vector2(origin.X + x, origin.Y + height - thickness);
			drawList.AddRectFilled(indicatorMin, indicatorMin + new Vector2(w, thickness), M3.U32(s.Primary),
				secondary ? 0f : thickness, secondary ? ImDrawFlags.None : ImDrawFlags.RoundCornersTop);
		}

		ImGui.SetCursorScreenPos(origin);
		ImGui.Dummy(new Vector2(total, height));
		return result;
	}

	public static string Truncate(string text, float maxWidth)
	{
		if (string.IsNullOrEmpty(text) || ImGui.CalcTextSize(text).X <= maxWidth)
		{
			return text;
		}

		var ellipsisWidth = ImGui.CalcTextSize("…").X;
		for (var length = text.Length - 1; length > 0; length--)
		{
			var candidate = text[..length];
			if (ImGui.CalcTextSize(candidate).X + ellipsisWidth <= maxWidth)
			{
				return candidate + "…";
			}
		}

		return "…";
	}
}
