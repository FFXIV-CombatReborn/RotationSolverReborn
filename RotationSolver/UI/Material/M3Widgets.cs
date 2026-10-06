using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;

namespace RotationSolver.UI.Material;

internal readonly record struct M3Segment(
	string Label,
	FontAwesomeIcon Icon = FontAwesomeIcon.None,
	string? Tooltip = null,
	Vector4? Accent = null);

internal readonly record struct M3WindowAction(string Id, FontAwesomeIcon Icon, string Tooltip);

internal readonly record struct M3WindowBrand(IDalamudTextureWrap? Logo, string Label, FontAwesomeIcon FallbackIcon = FontAwesomeIcon.Fire);

internal enum M3ButtonStyle
{
	Filled,

	Tonal,

	Outlined,

	Text,

	Danger,
}

internal static class M3Widgets
{
	#region Switch

	public static Vector2 SwitchSize()
	{
		return new Vector2(52f, 32f) * M3.Scale;
	}

	public static bool Switch(string id, ref bool value, bool enabled = true)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var size = SwitchSize();

		var changed = false;
		if (ImGui.InvisibleButton(id, size) && enabled)
		{
			value = !value;
			changed = true;
		}

		var hovered = enabled && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = enabled && ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var drawList = ImGui.GetWindowDrawList();
		var dim = enabled ? 1f : M3.DisabledContent;

		var trackSize = new Vector2(48f, 28f) * scale;
		var trackMin = min + ((size - trackSize) * 0.5f);
		var trackMax = trackMin + trackSize;
		var trackRadius = trackSize.Y * 0.5f;

		var progress = M3Motion.Approach(ImGui.GetID(id), value ? 1f : 0f, M3Motion.FastDuration);

		var trackFill = M3ColorMath.Mix(s.SurfaceContainerHighest, s.Primary, progress);
		var trackOutline = M3ColorMath.Mix(s.Outline, s.Primary, progress);
		if (hovered || held)
		{
			trackFill = M3.StateLayer(trackFill, value ? s.OnPrimary : s.OnSurface, hovered, held);
		}

		drawList.AddRectFilled(trackMin, trackMax, M3.U32(trackFill, dim), trackRadius);
		drawList.AddRect(trackMin, trackMax, M3.U32(trackOutline, 0.9f * dim), trackRadius, ImDrawFlags.None, 2f * scale);

		var thumbDiameter = float.Lerp(16f, 24f, progress) * scale;
		if (held)
		{
			thumbDiameter = 28f * scale;
		}

		var thumbRadius = thumbDiameter * 0.5f;
		var travelStart = trackMin.X + (4f * scale) + (8f * scale);
		var travelEnd = trackMax.X - (4f * scale) - (12f * scale);
		var thumbCenter = new Vector2(float.Lerp(travelStart, travelEnd, progress), trackMin.Y + trackRadius);
		var thumbColor = M3ColorMath.Mix(s.Outline, s.OnPrimary, progress);

		if (hovered || held)
		{
			var halo = value ? s.Primary : s.OnSurface;
			drawList.AddCircleFilled(thumbCenter, 20f * scale, M3.U32(halo, held ? M3.StatePressed : M3.StateHover));
		}

		drawList.AddCircleFilled(thumbCenter, thumbRadius, M3.U32(thumbColor, dim), 24);

		if (progress > 0.55f)
		{
			var tick = thumbRadius * 0.45f;
			var checkColor = M3.U32(s.OnPrimaryContainer, (progress - 0.55f) / 0.45f * dim);
			drawList.AddLine(
				thumbCenter + new Vector2(-tick, 0f),
				thumbCenter + new Vector2(-tick * 0.2f, tick * 0.7f),
				checkColor, 2f * scale);
			drawList.AddLine(
				thumbCenter + new Vector2(-tick * 0.2f, tick * 0.7f),
				thumbCenter + new Vector2(tick, -tick * 0.6f),
				checkColor, 2f * scale);
		}

		return changed;
	}

	#endregion

	#region Checkbox

	public static Vector2 CheckboxSize()
	{
		return new Vector2(20f, 20f) * M3.Scale;
	}

	public static bool Checkbox(string id, ref bool value)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var box = CheckboxSize();
		var hit = box + new Vector2(8f, 8f) * scale;

		var changed = false;
		if (ImGui.InvisibleButton(id, hit))
		{
			value = !value;
			changed = true;
		}

		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var center = min + (hit * 0.5f);
		var boxMin = center - (box * 0.5f);
		var boxMax = center + (box * 0.5f);
		var drawList = ImGui.GetWindowDrawList();
		var progress = M3Motion.Approach(ImGui.GetID(id), value ? 1f : 0f, M3Motion.FastDuration);

		if (hovered || held)
		{
			drawList.AddCircleFilled(center, hit.X * 0.55f, M3.U32(value ? s.Primary : s.OnSurface, held ? M3.StatePressed : M3.StateHover));
		}

		if (progress > 0.01f)
		{
			drawList.AddRectFilled(boxMin, boxMax, M3.U32(s.Primary, progress), M3.ShapeExtraSmall * 0.5f);
		}

		if (progress < 0.99f)
		{
			drawList.AddRect(boxMin, boxMax, M3.U32(s.OnSurfaceVariant, 1f - progress), M3.ShapeExtraSmall * 0.5f, ImDrawFlags.None, 2f * scale);
		}

		if (progress > 0.2f)
		{
			var tick = box.X * 0.26f;
			var color = M3.U32(s.OnPrimary, progress);
			drawList.AddLine(center + new Vector2(-tick, 0f), center + new Vector2(-tick * 0.25f, tick * 0.8f), color, 2f * scale);
			drawList.AddLine(center + new Vector2(-tick * 0.25f, tick * 0.8f), center + new Vector2(tick, -tick * 0.7f), color, 2f * scale);
		}

		return changed;
	}

	#endregion

	#region Radio buttons

	public static Vector2 RadioSize()
	{
		return new Vector2(28f, 28f) * M3.Scale;
	}

	public static bool RadioButton(string id, bool selected, bool enabled = true)
	{
		var hit = RadioSize();

		var pressed = ImGui.InvisibleButton(id, hit) && enabled;
		var hovered = enabled && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = enabled && ImGui.IsItemActive();
		var center = ImGui.GetItemRectMin() + (hit * 0.5f);
		var progress = M3Motion.Approach(ImGui.GetID(id), selected ? 1f : 0f, M3Motion.FastDuration);

		DrawRadio(ImGui.GetWindowDrawList(), center, hit.X, progress, selected, hovered, held, enabled);

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		return pressed;
	}

	public static int RadioGroup(string id, IReadOnlyList<string> options, int selectedIndex, bool horizontal = false, bool enabled = true)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var radio = RadioSize();
		var height = MathF.Max(radio.Y, ImGui.GetTextLineHeight() + (4f * scale));
		var labelGap = 4f * scale;
		var result = -1;

		using var idScope = ImRaii.PushId(id);

		for (var i = 0; i < options.Count; i++)
		{
			var selected = i == selectedIndex;
			var textSize = ImGui.CalcTextSize(options[i]);
			var width = radio.X + labelGap + textSize.X + (12f * scale);

			if (horizontal && i > 0)
			{
				ImGui.SameLine(0f, 8f * scale);
			}

			if (ImGui.InvisibleButton($"##option{i}", new Vector2(width, height)) && enabled && !selected)
			{
				result = i;
			}

			var hovered = enabled && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
			var held = enabled && ImGui.IsItemActive();
			var min = ImGui.GetItemRectMin();
			var drawList = ImGui.GetWindowDrawList();
			var center = new Vector2(min.X + (radio.X * 0.5f), min.Y + (height * 0.5f));
			var progress = M3Motion.Approach(ImGui.GetID($"##option{i}"), selected ? 1f : 0f, M3Motion.FastDuration);

			DrawRadio(drawList, center, radio.X, progress, selected, hovered, held, enabled);

			drawList.AddText(new Vector2(min.X + radio.X + labelGap, min.Y + ((height - textSize.Y) * 0.5f)),
				M3.U32(s.OnSurface, enabled ? 0.95f : M3.DisabledContent), options[i]);

			if (hovered)
			{
				ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			}
		}

		return result;
	}

	private static void DrawRadio(ImDrawListPtr drawList, Vector2 center, float hit, float progress, bool selected, bool hovered, bool held, bool enabled)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var dim = enabled ? 1f : M3.DisabledContent;

		if (hovered || held)
		{
			drawList.AddCircleFilled(center, hit * 0.55f, M3.U32(selected ? s.Primary : s.OnSurface, held ? M3.StatePressed : M3.StateHover), 24);
		}

		var ring = M3ColorMath.Mix(s.OnSurfaceVariant, s.Primary, progress);
		drawList.AddCircle(center, 9f * scale, M3.U32(ring, dim), 32, 2f * scale);

		if (progress > 0.01f)
		{
			drawList.AddCircleFilled(center, 5f * scale * progress, M3.U32(s.Primary, dim), 24);
		}
	}

	#endregion

	#region Buttons

	public static float ButtonHeight => M3.FitText(40f, 8f);

	public static float ButtonWidth(FontAwesomeIcon icon, string label)
	{
		var scale = M3.Scale;
		var width = 24f * scale * 2f;
		if (icon != FontAwesomeIcon.None)
		{
			width += M3Draw.MeasureIcon(icon).X + (8f * scale);
		}

		if (!string.IsNullOrEmpty(label))
		{
			width += ImGui.CalcTextSize(label).X;
		}

		return MathF.Max(width, 64f * scale);
	}

	public static bool Button(string id, string label, M3ButtonStyle style = M3ButtonStyle.Tonal, FontAwesomeIcon icon = FontAwesomeIcon.None, float? width = null, bool enabled = true, string? tooltip = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = ButtonHeight;
		var size = new Vector2(width ?? ButtonWidth(icon, label), height);

		var pressed = ImGui.InvisibleButton(id, size) && enabled;
		var hovered = enabled && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = enabled && ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var rounding = height * 0.5f;

		var (container, content, outline) = style switch
		{
			M3ButtonStyle.Filled => (s.Primary, s.OnPrimary, (Vector4?)null),
			M3ButtonStyle.Tonal => (s.SecondaryContainer, s.OnSecondaryContainer, (Vector4?)null),
			M3ButtonStyle.Outlined => (M3.Alpha(s.Surface, 0f), s.Primary, s.Outline),
			M3ButtonStyle.Danger => (s.ErrorContainer, s.OnErrorContainer, (Vector4?)null),
			_ => (M3.Alpha(s.Surface, 0f), s.Primary, (Vector4?)null),
		};

		if (!enabled)
		{
			container = container.W > 0f ? M3.Alpha(s.OnSurface, M3.DisabledContainer) : container;
			content = M3.Alpha(s.OnSurface, M3.DisabledContent);
			outline = outline is null ? null : M3.Alpha(s.OnSurface, M3.DisabledContainer);
		}
		else if (hovered || held)
		{
			container = container.W > 0f
				? M3.StateLayer(container, content, hovered, held)
				: M3.Alpha(content, held ? M3.StatePressed : M3.StateHover);
		}

		M3Draw.Container(drawList, min, max, container, rounding, outline, 1f);

		var iconWidth = icon == FontAwesomeIcon.None ? 0f : M3Draw.MeasureIcon(icon).X;
		var gap = icon == FontAwesomeIcon.None || string.IsNullOrEmpty(label) ? 0f : 8f * scale;
		var textSize = string.IsNullOrEmpty(label) ? Vector2.Zero : ImGui.CalcTextSize(label);
		var contentWidth = iconWidth + gap + textSize.X;
		var cursorX = min.X + ((size.X - contentWidth) * 0.5f);

		if (icon != FontAwesomeIcon.None)
		{
			using (ImRaii.PushFont(UiBuilder.IconFont))
			{
				var glyph = icon.ToIconString();
				var glyphSize = ImGui.CalcTextSize(glyph);
				drawList.AddText(UiBuilder.IconFont, ImGui.GetFontSize(),
					new Vector2(cursorX, min.Y + ((height - glyphSize.Y) * 0.5f)), M3.U32(content), glyph);
			}

			cursorX += iconWidth + gap;
		}

		if (!string.IsNullOrEmpty(label))
		{
			drawList.AddText(new Vector2(cursorX, min.Y + ((height - textSize.Y) * 0.5f)), M3.U32(content), label);
		}

		if (hovered && !string.IsNullOrEmpty(tooltip))
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			ImGui.SetTooltip(tooltip);
		}
		else if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		return pressed;
	}

	public static float IconButtonSize => 36f * M3.Scale;

	public static bool IconButton(string id, FontAwesomeIcon icon, string? tooltip = null, M3ButtonStyle style = M3ButtonStyle.Text, Vector4? tint = null, float? diameter = null)
	{
		var s = M3.Scheme;
		var size = Vector2.One * (diameter ?? IconButtonSize);

		var pressed = ImGui.InvisibleButton(id, size);
		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var center = (min + max) * 0.5f;
		var drawList = ImGui.GetWindowDrawList();

		var (container, content) = style switch
		{
			M3ButtonStyle.Filled => (s.Primary, s.OnPrimary),
			M3ButtonStyle.Tonal => (s.SecondaryContainer, s.OnSecondaryContainer),
			M3ButtonStyle.Danger => (s.ErrorContainer, s.OnErrorContainer),
			M3ButtonStyle.Outlined => (M3.Alpha(s.Surface, 0f), s.OnSurfaceVariant),
			_ => (M3.Alpha(s.Surface, 0f), tint ?? s.OnSurfaceVariant),
		};

		if (tint is { } explicitTint)
		{
			content = explicitTint;
		}

		if (container.W > 0f)
		{
			var fill = hovered || held ? M3.StateLayer(container, content, hovered, held) : container;
			drawList.AddCircleFilled(center, size.X * 0.5f, M3.U32(fill), 32);
		}
		else if (hovered || held)
		{
			drawList.AddCircleFilled(center, size.X * 0.5f, M3.U32(content, held ? M3.StatePressed : M3.StateHover), 32);
		}

		if (style == M3ButtonStyle.Outlined)
		{
			drawList.AddCircle(center, size.X * 0.5f, M3.U32(s.Outline, 0.8f), 32, 1f * M3.Scale);
		}

		M3Draw.IconCentered(drawList, icon, min, max, content);

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			if (!string.IsNullOrEmpty(tooltip))
			{
				ImGui.SetTooltip(tooltip);
			}
		}

		return pressed;
	}

	public static bool IconToggle(string id, FontAwesomeIcon icon, ref bool selected, string? tooltip = null, M3ButtonStyle style = M3ButtonStyle.Text, FontAwesomeIcon selectedIcon = FontAwesomeIcon.None, float? diameter = null)
	{
		var s = M3.Scheme;
		var size = Vector2.One * (diameter ?? IconButtonSize);

		var pressed = ImGui.InvisibleButton(id, size);
		if (pressed)
		{
			selected = !selected;
		}

		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var center = (min + max) * 0.5f;
		var radius = size.X * 0.5f;
		var drawList = ImGui.GetWindowDrawList();

		var (container, content) = (style, selected) switch
		{
			(M3ButtonStyle.Filled, true) => (s.Primary, s.OnPrimary),
			(M3ButtonStyle.Filled, false) => (s.SurfaceContainerHighest, s.Primary),
			(M3ButtonStyle.Tonal, true) => (s.SecondaryContainer, s.OnSecondaryContainer),
			(M3ButtonStyle.Tonal, false) => (s.SurfaceContainerHighest, s.OnSurfaceVariant),
			(M3ButtonStyle.Outlined, true) => (s.InverseSurface, s.InverseOnSurface),
			(M3ButtonStyle.Danger, true) => (s.ErrorContainer, s.OnErrorContainer),
			(_, true) => (M3.Alpha(s.Surface, 0f), s.Primary),
			_ => (M3.Alpha(s.Surface, 0f), s.OnSurfaceVariant),
		};

		if (container.W > 0f)
		{
			var fill = hovered || held ? M3.StateLayer(container, content, hovered, held) : container;
			drawList.AddCircleFilled(center, radius, M3.U32(fill), 32);
		}
		else if (hovered || held)
		{
			drawList.AddCircleFilled(center, radius, M3.U32(content, held ? M3.StatePressed : M3.StateHover), 32);
		}

		if (style == M3ButtonStyle.Outlined && !selected)
		{
			drawList.AddCircle(center, radius, M3.U32(s.Outline, 0.8f), 32, 1f * M3.Scale);
		}

		var glyph = selected && selectedIcon != FontAwesomeIcon.None ? selectedIcon : icon;
		M3Draw.IconCentered(drawList, glyph, min, max, content);

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			if (!string.IsNullOrEmpty(tooltip))
			{
				ImGui.SetTooltip(tooltip);
			}
		}

		return pressed;
	}

	#endregion

	#region Window actions

	private static float WindowActionsPadding => 4f * M3.Scale;
	private static float WindowActionsGap => 2f * M3.Scale;
	private static float WindowActionsDividerGap => 8f * M3.Scale;
	private static float WindowBrandLogoSize => 24f * M3.Scale;

	public static Vector2 WindowActionsSize(int actionCount)
	{
		return WindowActionsSize(ActionsWidth(actionCount), 1);
	}

	public static Vector2 WindowActionsSize(int actionCount, in M3WindowBrand brand, float minimized)
	{
		return WindowActionsSize(LeadingWidth(actionCount, brand, minimized), 2);
	}

	public static int WindowActions(string id, Vector2 topRight, ReadOnlySpan<M3WindowAction> actions, out bool closed, Vector4? fill = null, string closeTooltip = "Close")
	{
		return DrawWindowActions(id, topRight, actions, null, 0f, out _, out closed, fill, closeTooltip);
	}

	public static int WindowActions(string id, Vector2 topRight, ReadOnlySpan<M3WindowAction> actions, in M3WindowBrand brand, float minimized, out bool toggled, out bool closed, Vector4? fill = null, string closeTooltip = "Close")
	{
		return DrawWindowActions(id, topRight, actions, brand, minimized, out toggled, out closed, fill, closeTooltip);
	}

	private static int DrawWindowActions(string id, Vector2 topRight, ReadOnlySpan<M3WindowAction> actions, M3WindowBrand? brand, float minimized, out bool toggled, out bool closed, Vector4? fill, string closeTooltip)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var padding = WindowActionsPadding;
		var gap = WindowActionsGap;
		var buttonSize = IconButtonSize;

		minimized = brand is null ? 0f : Math.Clamp(minimized, 0f, 1f);
		var leading = brand is { } measured ? LeadingWidth(actions.Length, measured, minimized) : ActionsWidth(actions.Length);
		var size = WindowActionsSize(leading, brand is null ? 1 : 2);

		var min = new Vector2(topRight.X - size.X, topRight.Y);
		var drawList = ImGui.GetWindowDrawList();
		drawList.AddRectFilled(min, min + size, M3.U32(fill ?? M3.Alpha(s.Surface, 0.72f)), M3.ShapeFull);

		using var idScope = ImRaii.PushId(id);
		var result = -1;
		var x = min.X + padding;
		var y = min.Y + padding;

		ImGui.PushClipRect(new Vector2(x, y), new Vector2(x + leading, y + buttonSize), true);
		var actionsAlpha = Math.Clamp(1f - (minimized * 2f), 0f, 1f);
		if (actionsAlpha > 0f)
		{
			using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * actionsAlpha);
			var actionX = x;
			for (var i = 0; i < actions.Length; i++)
			{
				var action = actions[i];
				ImGui.SetCursorScreenPos(new Vector2(actionX, y));
				if (IconButton(action.Id, action.Icon, action.Tooltip))
				{
					result = i;
				}

				actionX += buttonSize + gap;
			}
		}

		if (brand is { } shown && minimized > 0.5f)
		{
			using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * ((minimized * 2f) - 1f));
			DrawWindowBrand(drawList, shown, new Vector2(x, y), buttonSize);
		}

		ImGui.PopClipRect();
		x += leading;

		var dividerRoom = DividerRoom(leading);
		if (dividerRoom > 0f)
		{
			x += dividerRoom;
			drawList.AddLine(new Vector2(x, y + (8f * scale)), new Vector2(x, y + buttonSize - (8f * scale)),
				M3.U32(s.OutlineVariant, 0.9f * dividerRoom / WindowActionsDividerGap), 1f * scale);
			x += dividerRoom;
		}

		toggled = false;
		if (brand is not null)
		{
			ImGui.SetCursorScreenPos(new Vector2(x, y));
			toggled = CaretButton("##minimize", -MathF.PI * 0.5f * minimized, minimized < 0.5f ? "Minimize" : "Restore");
			x += buttonSize + gap;
		}

		ImGui.SetCursorScreenPos(new Vector2(x, y));
		closed = IconButton("##close", FontAwesomeIcon.Times, closeTooltip);
		return result;
	}

	private static Vector2 WindowActionsSize(float leading, int controls)
	{
		var padding = WindowActionsPadding;
		var width = (padding * 2f) + leading + (DividerRoom(leading) * 2f)
			+ (IconButtonSize * controls) + (WindowActionsGap * (controls - 1));
		return new Vector2(width, IconButtonSize + (padding * 2f));
	}

	private static float DividerRoom(float leading)
	{
		return WindowActionsDividerGap * Math.Clamp(leading / WindowActionsDividerGap, 0f, 1f);
	}

	private static float ActionsWidth(int count)
	{
		return count <= 0 ? 0f : (IconButtonSize * count) + (WindowActionsGap * (count - 1));
	}

	private static float LeadingWidth(int actionCount, in M3WindowBrand brand, float minimized)
	{
		return float.Lerp(ActionsWidth(actionCount), BrandWidth(brand), Math.Clamp(minimized, 0f, 1f));
	}

	private static float BrandWidth(in M3WindowBrand brand)
	{
		var scale = M3.Scale;
		using var font = ImRaii.PushFont(M3.TitleMedium);
		return (6f * scale) + WindowBrandLogoSize + (8f * scale) + ImGui.CalcTextSize(brand.Label).X + (4f * scale);
	}

	private static void DrawWindowBrand(ImDrawListPtr drawList, in M3WindowBrand brand, Vector2 origin, float height)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var logoSize = WindowBrandLogoSize;
		var logoMin = new Vector2(origin.X + (6f * scale), origin.Y + ((height - logoSize) * 0.5f));
		var logoMax = logoMin + new Vector2(logoSize, logoSize);

		var logo = brand.Logo;
		if (logo?.Handle != null)
		{
			drawList.AddImageRounded(logo.Handle, logoMin, logoMax, Vector2.Zero, Vector2.One, M3.U32(Vector4.One), M3.ShapeExtraSmall);
		}
		else
		{
			drawList.AddRectFilled(logoMin, logoMax, M3.U32(s.PrimaryContainer), M3.ShapeExtraSmall);
			M3Draw.IconCentered(drawList, brand.FallbackIcon, logoMin, logoMax, s.OnPrimaryContainer);
		}

		using var font = ImRaii.PushFont(M3.TitleMedium);
		var labelSize = ImGui.CalcTextSize(brand.Label);
		drawList.AddText(new Vector2(logoMax.X + (8f * scale), origin.Y + ((height - labelSize.Y) * 0.5f)), M3.U32(s.OnSurface), brand.Label);
	}

	private static bool CaretButton(string id, float angle, string? tooltip)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var size = Vector2.One * IconButtonSize;

		var pressed = ImGui.InvisibleButton(id, size);
		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = ImGui.IsItemActive();
		var center = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * 0.5f;
		var drawList = ImGui.GetWindowDrawList();
		var content = s.OnSurfaceVariant;

		if (hovered || held)
		{
			drawList.AddCircleFilled(center, size.X * 0.5f, M3.U32(content, held ? M3.StatePressed : M3.StateHover), 32);
		}

		// Wound clockwise, which ImGui's anti-aliased fill needs.
		var halfWidth = 5f * scale;
		var halfHeight = 3.5f * scale;
		var (sin, cos) = MathF.SinCos(angle);
		Vector2 Turn(float px, float py) => center + new Vector2((px * cos) - (py * sin), (px * sin) + (py * cos));
		drawList.AddTriangleFilled(Turn(0f, halfHeight), Turn(-halfWidth, -halfHeight), Turn(halfWidth, -halfHeight), M3.U32(content));

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			if (!string.IsNullOrEmpty(tooltip))
			{
				ImGui.SetTooltip(tooltip);
			}
		}

		return pressed;
	}

	#endregion

	#region Segmented buttons

	public static float SegmentedHeight => M3.FitText(32f, 6f);

	public static float SegmentedWidth(ReadOnlySpan<M3Segment> segments)
	{
		var scale = M3.Scale;
		var widest = 0f;

		foreach (var segment in segments)
		{
			var width = ImGui.CalcTextSize(segment.Label).X + (14f * scale * 2f);
			if (segment.Icon != FontAwesomeIcon.None)
			{
				width += M3Draw.MeasureIcon(segment.Icon).X + (6f * scale);
			}

			widest = MathF.Max(widest, width);
		}

		return widest * segments.Length;
	}

	// A label font only changes the text; the buttons keep their height and icons.
	public static int SegmentedButtons(string id, ReadOnlySpan<M3Segment> segments, int selectedIndex, float? width = null, ImFontPtr? labelFont = null)
	{
		if (segments.Length == 0)
		{
			return -1;
		}

		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = SegmentedHeight;
		var total = width ?? SegmentedWidth(segments);
		var segmentWidth = total / segments.Length;
		var rounding = height * 0.5f;
		var origin = ImGui.GetCursorScreenPos();
		var drawList = ImGui.GetWindowDrawList();
		var result = -1;

		using var idScope = ImRaii.PushId(id);

		for (var i = 0; i < segments.Length; i++)
		{
			var segment = segments[i];
			var selected = i == selectedIndex;
			var segmentMin = new Vector2(origin.X + (segmentWidth * i), origin.Y);

			ImGui.SetCursorScreenPos(segmentMin);
			if (ImGui.InvisibleButton($"##segment{i}", new Vector2(segmentWidth, height)) && !selected)
			{
				result = i;
			}

			var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
			var held = ImGui.IsItemActive();
			var segmentMax = segmentMin + new Vector2(segmentWidth, height);
			var tone = segment.Accent ?? s.Primary;

			var corners = segments.Length == 1 ? ImDrawFlags.RoundCornersAll
				: i == 0 ? ImDrawFlags.RoundCornersLeft
				: i == segments.Length - 1 ? ImDrawFlags.RoundCornersRight
				: ImDrawFlags.RoundCornersNone;

			if (selected)
			{
				drawList.AddRectFilled(segmentMin, segmentMax, M3.U32(s.SecondaryContainer, 0.95f), rounding, corners);
			}

			if (hovered || held)
			{
				drawList.AddRectFilled(segmentMin, segmentMax,
					M3.U32(selected ? tone : s.OnSurface, held ? M3.StatePressed : M3.StateHover), rounding, corners);
				ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			}

			var content = selected ? tone : M3.Alpha(s.OnSurfaceVariant, hovered ? 1f : 0.85f);
			var iconWidth = segment.Icon == FontAwesomeIcon.None ? 0f : M3Draw.MeasureIcon(segment.Icon).X;
			var gap = segment.Icon == FontAwesomeIcon.None ? 0f : 6f * scale;
			var iconTop = segmentMin.Y + ((height - ImGui.GetTextLineHeight()) * 0.5f);

			using (ImRaii.PushFont(labelFont.GetValueOrDefault(), labelFont.HasValue))
			{
				var textSize = ImGui.CalcTextSize(segment.Label);
				var cursorX = segmentMin.X + ((segmentWidth - iconWidth - gap - textSize.X) * 0.5f);

				if (segment.Icon != FontAwesomeIcon.None)
				{
					M3Draw.Icon(drawList, segment.Icon, new Vector2(cursorX, iconTop), content);
					cursorX += iconWidth + gap;
				}

				drawList.AddText(new Vector2(cursorX, segmentMin.Y + ((height - textSize.Y) * 0.5f)), M3.U32(content), segment.Label);
			}

			if (i > 0)
			{
				drawList.AddLine(segmentMin + new Vector2(0f, 1f * scale), new Vector2(segmentMin.X, segmentMax.Y - (1f * scale)),
					M3.U32(s.Outline, 0.55f), 1f * scale);
			}

			if (hovered && !string.IsNullOrEmpty(segment.Tooltip))
			{
				ImguiTooltips.ShowTooltip(segment.Tooltip);
			}
		}

		drawList.AddRect(origin, origin + new Vector2(total, height), M3.U32(s.Outline, 0.65f),
			rounding, ImDrawFlags.RoundCornersAll, 1f * scale);

		ImGui.SetCursorScreenPos(origin);
		ImGui.Dummy(new Vector2(total, height));
		return result;
	}

	#endregion

	#region Chips and pills

	public static float ChipHeight => M3.FitText(32f, 6f);

	public static float ChipWidth(string label, FontAwesomeIcon icon = FontAwesomeIcon.None, FontAwesomeIcon trailingIcon = FontAwesomeIcon.None)
	{
		var scale = M3.Scale;
		var width = ImGui.CalcTextSize(label).X + (16f * scale * 2f);
		if (icon != FontAwesomeIcon.None)
		{
			width += M3Draw.MeasureIcon(icon).X + (8f * scale);
		}

		if (trailingIcon != FontAwesomeIcon.None)
		{
			width += M3Draw.MeasureIcon(trailingIcon).X + (8f * scale);
		}

		return width;
	}

	public static bool Chip(string id, string label, bool selected, FontAwesomeIcon icon = FontAwesomeIcon.None, string? tooltip = null, Vector4? accent = null, FontAwesomeIcon trailingIcon = FontAwesomeIcon.None)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = ChipHeight;
		var size = new Vector2(ChipWidth(label, icon, trailingIcon), height);
		var tone = accent ?? s.Primary;

		var pressed = ImGui.InvisibleButton(id, size);
		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var rounding = M3.ShapeSmall;

		var container = selected ? s.SecondaryContainer : M3.Alpha(s.Surface, 0f);
		var content = selected ? s.OnSecondaryContainer : s.OnSurfaceVariant;
		if (hovered || held)
		{
			container = container.W > 0f
				? M3.StateLayer(container, content, hovered, held)
				: M3.Alpha(s.OnSurface, held ? M3.StatePressed : M3.StateHover);
		}

		M3Draw.Container(drawList, min, max, container, rounding, selected ? null : M3.Alpha(s.Outline, 0.8f), 1f);

		var iconWidth = icon == FontAwesomeIcon.None ? 0f : M3Draw.MeasureIcon(icon).X;
		var gap = icon == FontAwesomeIcon.None ? 0f : 8f * scale;
		var trailingWidth = trailingIcon == FontAwesomeIcon.None ? 0f : M3Draw.MeasureIcon(trailingIcon).X + (8f * scale);
		var textSize = ImGui.CalcTextSize(label);
		var cursorX = min.X + ((size.X - (iconWidth + gap + textSize.X + trailingWidth)) * 0.5f);

		if (icon != FontAwesomeIcon.None)
		{
			using (ImRaii.PushFont(UiBuilder.IconFont))
			{
				var glyph = icon.ToIconString();
				drawList.AddText(UiBuilder.IconFont, ImGui.GetFontSize(),
					new Vector2(cursorX, min.Y + ((height - ImGui.CalcTextSize(glyph).Y) * 0.5f)),
					M3.U32(selected ? tone : content), glyph);
			}

			cursorX += iconWidth + gap;
		}

		drawList.AddText(new Vector2(cursorX, min.Y + ((height - textSize.Y) * 0.5f)), M3.U32(content), label);

		if (trailingIcon != FontAwesomeIcon.None)
		{
			var trailingSize = M3Draw.MeasureIcon(trailingIcon);
			M3Draw.Icon(drawList, trailingIcon,
				new Vector2(cursorX + textSize.X + (8f * scale), min.Y + ((height - trailingSize.Y) * 0.5f)), content);
		}

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			if (!string.IsNullOrEmpty(tooltip))
			{
				ImGui.SetTooltip(tooltip);
			}
		}

		return pressed;
	}

	public static Vector2 PillSize(string label, FontAwesomeIcon icon = FontAwesomeIcon.None)
	{
		var scale = M3.Scale;
		var width = ImGui.CalcTextSize(label).X + (12f * scale * 2f);
		width += icon == FontAwesomeIcon.None
			? (6f * scale) + (6f * scale)
			: M3Draw.MeasureIcon(icon).X + (6f * scale);
		return new Vector2(width, M3.FitText(24f, 3f));
	}

	public static bool Pill(string id, string label, Vector4 accent, FontAwesomeIcon icon = FontAwesomeIcon.None, string? tooltip = null, bool interactive = false)
	{
		var scale = M3.Scale;
		var size = PillSize(label, icon);

		var clicked = false;
		if (interactive)
		{
			clicked = ImGui.InvisibleButton(id, size);
		}
		else
		{
			ImGui.Dummy(size);
		}

		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var rounding = size.Y * 0.5f;

		drawList.AddRectFilled(min, max, M3.U32(accent, hovered ? 0.20f : 0.13f), rounding);
		drawList.AddRect(min, max, M3.U32(accent, hovered ? 0.62f : 0.40f), rounding, ImDrawFlags.None, 1f * scale);

		var cursorX = min.X + (12f * scale);
		if (icon == FontAwesomeIcon.None)
		{
			var radius = 3f * scale;
			drawList.AddCircleFilled(new Vector2(cursorX + radius, min.Y + (size.Y * 0.5f)), radius, M3.U32(accent), 12);
			cursorX += (radius * 2f) + (6f * scale);
		}
		else
		{
			using (ImRaii.PushFont(UiBuilder.IconFont))
			{
				var glyph = icon.ToIconString();
				var glyphSize = ImGui.CalcTextSize(glyph);
				drawList.AddText(UiBuilder.IconFont, ImGui.GetFontSize(),
					new Vector2(cursorX, min.Y + ((size.Y - glyphSize.Y) * 0.5f)), M3.U32(accent), glyph);
				cursorX += glyphSize.X + (6f * scale);
			}
		}

		var textSize = ImGui.CalcTextSize(label);
		drawList.AddText(new Vector2(cursorX, min.Y + ((size.Y - textSize.Y) * 0.5f)), M3.U32(M3.Scheme.OnSurface, 0.95f), label);

		if (hovered && !string.IsNullOrEmpty(tooltip))
		{
			ImGui.SetTooltip(tooltip);
		}

		return clicked;
	}

	private static float InputChipRemoveWidth => ChipHeight;

	public static float InputChipWidth(string label, FontAwesomeIcon icon = FontAwesomeIcon.None)
	{
		var scale = M3.Scale;
		var width = (12f * scale) + ImGui.CalcTextSize(label).X + InputChipRemoveWidth;
		if (icon != FontAwesomeIcon.None)
		{
			width += M3Draw.MeasureIcon(icon).X + (8f * scale);
		}

		return width;
	}

	public static bool InputChip(string id, string label, out bool removed, FontAwesomeIcon icon = FontAwesomeIcon.None, string? tooltip = null, Vector4? accent = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = ChipHeight;
		var width = InputChipWidth(label, icon);
		var removeWidth = InputChipRemoveWidth;
		var tone = accent ?? s.Primary;

		var pressed = ImGui.InvisibleButton(id, new Vector2(width - removeWidth, height));
		var bodyHovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var bodyHeld = ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();

		ImGui.SameLine(0f, 0f);
		removed = ImGui.InvisibleButton($"{id}_remove", new Vector2(removeWidth, height));
		var removeHovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var removeHeld = ImGui.IsItemActive();

		var max = min + new Vector2(width, height);
		var drawList = ImGui.GetWindowDrawList();
		var rounding = M3.ShapeSmall;
		var content = s.OnSurfaceVariant;

		var container = M3.Alpha(s.Surface, 0f);
		if (bodyHovered || bodyHeld || removeHovered || removeHeld)
		{
			container = M3.Alpha(s.OnSurface, bodyHeld || removeHeld ? M3.StatePressed : M3.StateHover);
		}

		M3Draw.Container(drawList, min, max, container, rounding, M3.Alpha(s.Outline, 0.8f), 1f);

		var cursorX = min.X + (12f * scale);
		if (icon != FontAwesomeIcon.None)
		{
			var iconSize = M3Draw.MeasureIcon(icon);
			M3Draw.Icon(drawList, icon, new Vector2(cursorX, min.Y + ((height - iconSize.Y) * 0.5f)), tone);
			cursorX += iconSize.X + (8f * scale);
		}

		var textSize = ImGui.CalcTextSize(label);
		drawList.AddText(new Vector2(cursorX, min.Y + ((height - textSize.Y) * 0.5f)), M3.U32(content), label);

		var removeMin = new Vector2(max.X - removeWidth, min.Y);
		var removeCenter = removeMin + (new Vector2(removeWidth, height) * 0.5f);
		if (removeHovered || removeHeld)
		{
			drawList.AddCircleFilled(removeCenter, height * 0.36f, M3.U32(s.OnSurface, removeHeld ? M3.StatePressed : M3.StateHover), 24);
		}

		var arm = 4f * scale;
		var crossColor = M3.U32(removeHovered ? s.OnSurface : content);
		drawList.AddLine(removeCenter - new Vector2(arm, arm), removeCenter + new Vector2(arm, arm), crossColor, 1.5f * scale);
		drawList.AddLine(removeCenter + new Vector2(-arm, arm), removeCenter + new Vector2(arm, -arm), crossColor, 1.5f * scale);

		if (bodyHovered || removeHovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		if (removeHovered)
		{
			ImGui.SetTooltip($"Remove {label}");
		}
		else if (bodyHovered && !string.IsNullOrEmpty(tooltip))
		{
			ImGui.SetTooltip(tooltip);
		}

		return pressed;
	}

	public static Vector2 BadgeSize(string? text)
	{
		var scale = M3.Scale;
		if (string.IsNullOrEmpty(text))
		{
			return new Vector2(6f, 6f) * scale;
		}

		using var font = ImRaii.PushFont(M3.LabelSmall);
		var textSize = ImGui.CalcTextSize(text);
		var height = MathF.Max(16f * scale, textSize.Y + (2f * scale));
		return new Vector2(MathF.Max(height, textSize.X + (8f * scale)), height);
	}

	public static string BadgeCount(int count)
	{
		return count > 999 ? "999+" : count.ToString();
	}

	public static void Badge(string? text = null, Vector4? color = null)
	{
		var size = BadgeSize(text);
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		BadgeAt(new Vector2(max.X - (size.X * 0.5f), min.Y + (size.Y * 0.5f)), text, color);
	}

	public static void BadgeAt(Vector2 center, string? text = null, Vector4? color = null)
	{
		var s = M3.Scheme;
		var drawList = ImGui.GetWindowDrawList();
		var fill = color ?? s.Error;
		var size = BadgeSize(text);

		if (string.IsNullOrEmpty(text))
		{
			drawList.AddCircleFilled(center, size.X * 0.5f, M3.U32(fill), 12);
			return;
		}

		var min = center - (size * 0.5f);
		drawList.AddRectFilled(min, min + size, M3.U32(fill), size.Y * 0.5f);

		using var font = ImRaii.PushFont(M3.LabelSmall);
		var textSize = ImGui.CalcTextSize(text);

		var onFill = color is null ? s.OnError : M3.ContentOn(fill);
		drawList.AddText(center - (textSize * 0.5f), M3.U32(onFill), text);
	}

	#endregion

	#region Progress

	public static void LinearProgress(Vector2 size, float fraction, float? marker = null, Vector4? markerColor = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;

		ImGui.Dummy(size);
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var rounding = size.Y * 0.5f;
		var centerY = (min.Y + max.Y) * 0.5f;

		fraction = Math.Clamp(fraction, 0f, 1f);
		var split = float.Lerp(min.X, max.X, fraction);

		var gap = fraction > 0f && fraction < 1f ? MathF.Min(4f * scale, size.Y) : 0f;
		var trackStart = MathF.Min(max.X, split + gap);
		if (max.X - trackStart > 0.5f)
		{
			drawList.AddRectFilled(new Vector2(trackStart, min.Y), max, M3.U32(s.SecondaryContainer), rounding);

			if (max.X - trackStart >= size.Y)
			{
				var stopRadius = MathF.Min(2f * scale, rounding);
				drawList.AddCircleFilled(new Vector2(max.X - rounding, centerY), stopRadius, M3.U32(s.Primary), 12);
			}
		}

		if (split - min.X > 0.5f)
		{
			drawList.AddRectFilled(min, new Vector2(split, max.Y), M3.U32(s.Primary), rounding);
		}

		if (marker is { } at && at > 0f && at < 1f)
		{
			var x = float.Lerp(min.X, max.X, at);
			var overhang = 2f * scale;
			drawList.AddLine(new Vector2(x, min.Y - overhang), new Vector2(x, max.Y + overhang),
				M3.U32(markerColor ?? s.Tertiary), 2f * scale);
		}
	}

	public static void LinearProgressIndeterminate(Vector2 size)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;

		ImGui.Dummy(size);
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var rounding = size.Y * 0.5f;
		var gap = MathF.Min(4f * scale, size.Y);

		const float Period = 2f;
		var t = (float)(ImGui.GetTime() % Period) / Period;
		Span<Vector2> segments =
		[
			new(Ease((t - 0.20f) / 0.60f), Ease(t / 0.55f)),
			new(Ease((t - 0.70f) / 0.30f), Ease((t - 0.45f) / 0.40f)),
		];

		if (segments[1].X < segments[0].X)
		{
			(segments[0], segments[1]) = (segments[1], segments[0]);
		}

		var trackStart = min.X;
		foreach (var segment in segments)
		{
			var from = float.Lerp(min.X, max.X, segment.X);
			var to = float.Lerp(min.X, max.X, segment.Y);
			if (to - from < 0.5f)
			{
				continue;
			}

			if (from - gap - trackStart > 0.5f)
			{
				drawList.AddRectFilled(new Vector2(trackStart, min.Y), new Vector2(from - gap, max.Y), M3.U32(s.SecondaryContainer), rounding);
			}

			drawList.AddRectFilled(new Vector2(from, min.Y), new Vector2(to, max.Y), M3.U32(s.Primary), rounding);
			trackStart = to + gap;
		}

		if (max.X - trackStart > 0.5f)
		{
			drawList.AddRectFilled(new Vector2(trackStart, min.Y), max, M3.U32(s.SecondaryContainer), rounding);
		}
	}

	public static void CircularProgress(float diameter, float? fraction = null, float thickness = 4f)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var size = Vector2.One * diameter;

		ImGui.Dummy(size);
		var center = ImGui.GetItemRectMin() + (size * 0.5f);
		var stroke = thickness * scale;
		var radius = MathF.Max(1f, (diameter - stroke) * 0.5f);
		var drawList = ImGui.GetWindowDrawList();

		if (fraction is { } value)
		{
			value = Math.Clamp(value, 0f, 1f);

			var gap = MathF.Min(0.1f, stroke * 1.5f / (MathF.Tau * radius));
			if (value <= 0.001f)
			{
				M3Draw.Arc(drawList, center, radius, 0f, 1f, s.SecondaryContainer, stroke);
				return;
			}

			if (value < 0.999f && 1f - value - (gap * 2f) > 0.001f)
			{
				M3Draw.Arc(drawList, center, radius, value + gap, 1f - gap, s.SecondaryContainer, stroke);
			}

			M3Draw.Arc(drawList, center, radius, 0f, value, s.Primary, stroke);
			if (value < 0.999f)
			{
				RoundCaps(drawList, center, radius, 0f, value, s.Primary, stroke);
			}

			return;
		}

		Spinner(drawList, center, radius, s.Primary, stroke);
	}

	// The indeterminate arc of CircularProgress, drawn without taking up a layout slot.
	public static void Spinner(ImDrawListPtr drawList, Vector2 center, float radius, Vector4 color, float stroke)
	{
		const float Cycle = 1.333f;
		var time = (float)ImGui.GetTime();
		var cycles = MathF.Floor(time / Cycle);
		var phase = (time - (cycles * Cycle)) / Cycle;
		var head = Ease(phase / 0.5f) * 0.75f;
		var tail = Ease((phase - 0.5f) / 0.5f) * 0.75f;
		// Wrapped to one turn so the angle doesn't lose precision over a long session.
		var start = ((time * 0.25f) + (cycles * 0.75f) + tail) % 1f;
		var end = start + (head - tail) + 0.03f;

		M3Draw.Arc(drawList, center, radius, start, end, color, stroke);
		RoundCaps(drawList, center, radius, start, end, color, stroke);
	}

	private static float Ease(float t)
	{
		t = Math.Clamp(t, 0f, 1f);
		return t * t * (3f - (2f * t));
	}

	private static void RoundCaps(ImDrawListPtr drawList, Vector2 center, float radius, float from, float to, Vector4 color, float thickness)
	{
		RoundCap(drawList, center, radius, from, color, thickness);
		RoundCap(drawList, center, radius, to, color, thickness);
	}

	private static void RoundCap(ImDrawListPtr drawList, Vector2 center, float radius, float at, Vector4 color, float thickness)
	{
		var angle = (-MathF.PI * 0.5f) + (MathF.Tau * at);
		var point = center + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius);
		drawList.AddCircleFilled(point, thickness * 0.5f, M3.U32(color), 12);
	}

	#endregion

	#region Sliders

	private static ImRaii.ColorDisposable PushInvisibleSliderChrome(bool condition)
	{
		return ImRaii.PushColor(ImGuiCol.FrameBg, new Vector4(0f, 0f, 0f, 0f), condition)
			.Push(ImGuiCol.FrameBgHovered, new Vector4(0f, 0f, 0f, 0f), condition)
			.Push(ImGuiCol.FrameBgActive, new Vector4(0f, 0f, 0f, 0f), condition)
			.Push(ImGuiCol.SliderGrab, new Vector4(0f, 0f, 0f, 0f), condition)
			.Push(ImGuiCol.SliderGrabActive, new Vector4(0f, 0f, 0f, 0f), condition)
			.Push(ImGuiCol.Text, new Vector4(0f, 0f, 0f, 0f), condition);
	}

	public static bool Slider(string id, ref float value, float min, float max, string displayValue, float width)
	{
		var scale = M3.Scale;
		// Ctrl+click turns the slider into a text box, which must stay visible.
		var editing = ImGuiP.TempInputIsActive(ImGui.GetID(id));
		bool changed;

		using (PushInvisibleSliderChrome(!editing))
		using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(editing ? 8f : 0f, 10f) * scale))
		{
			ImGui.SetNextItemWidth(width);
			changed = ImGui.SliderFloat(id, ref value, min, max, "%.2f", ImGuiSliderFlags.NoRoundToFormat);
		}

		DrawSliderVisual(value, min, max, displayValue, M3.Scheme, scale, editing);
		return changed;
	}

	public static bool SliderInt(string id, ref int value, int min, int max, string displayValue, float width)
	{
		var scale = M3.Scale;
		var editing = ImGuiP.TempInputIsActive(ImGui.GetID(id));
		bool changed;

		using (PushInvisibleSliderChrome(!editing))
		using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(editing ? 8f : 0f, 10f) * scale))
		{
			ImGui.SetNextItemWidth(width);
			changed = ImGui.SliderInt(id, ref value, min, max, "%d");
		}

		DrawSliderVisual(value, min, max, displayValue, M3.Scheme, scale, editing);
		return changed;
	}

	private static void DrawSliderVisual(float value, float min, float max, string displayValue, M3Scheme s, float scale, bool editing)
	{
		var itemMin = ImGui.GetItemRectMin();
		var itemMax = ImGui.GetItemRectMax();
		var centerY = (itemMin.Y + itemMax.Y) * 0.5f;
		var drawList = ImGui.GetWindowDrawList();

		var hasReadout = !string.IsNullOrEmpty(displayValue);
		var textSize = hasReadout ? ImGui.CalcTextSize(displayValue) : Vector2.Zero;
		if (hasReadout)
		{
			drawList.AddText(new Vector2(itemMax.X + (10f * scale), centerY - (textSize.Y * 0.5f)),
				M3.U32(s.OnSurfaceVariant), displayValue);
		}

		if (editing)
		{
			return;
		}

		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var active = ImGui.IsItemActive();

		var trackHeight = 4f * scale;
		var handleRadius = 10f * scale;
		var trackLeft = itemMin.X + handleRadius;
		var trackRight = itemMax.X - handleRadius;
		var fraction = max > min ? Math.Clamp((value - min) / (max - min), 0f, 1f) : 0f;
		var handleX = float.Lerp(trackLeft, trackRight, fraction);

		drawList.AddRectFilled(
			new Vector2(trackLeft, centerY - (trackHeight * 0.5f)),
			new Vector2(trackRight, centerY + (trackHeight * 0.5f)),
			M3.U32(s.SurfaceContainerHighest), trackHeight);

		if (handleX > trackLeft)
		{
			drawList.AddRectFilled(
				new Vector2(trackLeft, centerY - (trackHeight * 0.5f)),
				new Vector2(handleX, centerY + (trackHeight * 0.5f)),
				M3.U32(s.Primary), trackHeight);
		}

		if (hovered || active)
		{
			drawList.AddCircleFilled(new Vector2(handleX, centerY), handleRadius + (8f * scale),
				M3.U32(s.Primary, active ? M3.StatePressed : M3.StateHover), 24);
		}

		drawList.AddCircleFilled(new Vector2(handleX, centerY), handleRadius, M3.U32(s.Primary), 24);
		drawList.AddCircleFilled(new Vector2(handleX, centerY), handleRadius * 0.45f, M3.U32(s.OnPrimary, 0.9f), 16);

		if (hasReadout && active)
		{
			var padding = new Vector2(8f, 4f) * scale;
			var bubbleMin = new Vector2(handleX - (textSize.X * 0.5f) - padding.X, itemMin.Y - textSize.Y - (padding.Y * 2f) - (6f * scale));
			var bubbleMax = bubbleMin + textSize + (padding * 2f);
			drawList.AddRectFilled(bubbleMin, bubbleMax, M3.U32(s.InverseSurface), M3.ShapeSmall);
			drawList.AddText(bubbleMin + padding, M3.U32(s.InverseOnSurface), displayValue);
		}
	}

	public static float SliderValueGutter(string longestValue)
	{
		return ImGui.CalcTextSize(longestValue).X + (14f * M3.Scale);
	}

	#endregion

	#region Full-width setting rows

	private static (string Label, string Id) SplitLabel(string label)
	{
		// "##" hides text but keeps the whole label as the ID; only "###" replaces it. Otherwise controls with the same suffix would share an ID.
		var explicitId = label.IndexOf("###", StringComparison.Ordinal);
		if (explicitId >= 0)
		{
			var trailing = label[(explicitId + 3)..];
			return (label[..explicitId], string.IsNullOrEmpty(trailing) ? label : trailing);
		}

		var hidden = label.IndexOf("##", StringComparison.Ordinal);
		return hidden < 0 ? (label, label) : (label[..hidden], label);
	}

	private static string FormatValue(string format, float value)
	{
		if (string.IsNullOrEmpty(format))
		{
			return value.ToString("0.##");
		}

		var percent = -1;
		for (var i = 0; i < format.Length - 1; i++)
		{
			if (format[i] != '%')
			{
				continue;
			}

			if (format[i + 1] == '%')
			{
				i++;
				continue;
			}

			percent = i;
			break;
		}

		if (percent < 0)
		{
			return Unescape(format);
		}

		var cursor = percent + 1;
		var digits = 0;
		var hasDigits = false;

		if (cursor < format.Length && format[cursor] == '.')
		{
			cursor++;
			while (cursor < format.Length && char.IsAsciiDigit(format[cursor]))
			{
				digits = (digits * 10) + (format[cursor] - '0');
				cursor++;
				hasDigits = true;
			}
		}

		if (cursor >= format.Length)
		{
			return Unescape(format);
		}

		var rendered = format[cursor] switch
		{
			'f' => value.ToString($"F{(hasDigits ? digits : 3)}"),
			'd' or 'i' => ((int)value).ToString(),
			_ => null,
		};

		return rendered == null
			? Unescape(format)
			: Unescape(string.Concat(format.AsSpan(0, percent), rendered, format.AsSpan(cursor + 1)));
	}

	private static string Unescape(string text)
	{
		return text.Contains("%%", StringComparison.Ordinal)
			? text.Replace("%%", "%", StringComparison.Ordinal)
			: text;
	}

	public static bool RowSwitch(string label, ref bool value, string? supporting = null)
	{
		return RowSwitch(label, ref value, out _, supporting);
	}

	public static bool RowSwitch(string label, ref bool value, out bool hovered, string? supporting = null)
	{
		var (display, id) = SplitLabel(label);
		var row = M3SettingRow.Begin(display, supporting, SwitchSize());
		hovered = row.Hovered;
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = Switch($"##{id}_switch", ref value);
		M3SettingRow.End(row);
		return changed;
	}

	public static bool RowDragFloat(string label, ref float value, float min, float max, string format)
	{
		var (display, id) = SplitLabel(label);
		var trackWidth = 150f * M3.Scale;
		var readout = FormatValue(format, value);
		var controlSize = new Vector2(trackWidth + SliderValueGutter(FormatValue(format, max)), ButtonHeight);

		var row = M3SettingRow.Begin(display, null, controlSize);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = Slider($"##{id}_slider", ref value, min, max, readout, trackWidth);
		M3SettingRow.End(row);
		return changed;
	}

	public static bool RowDragInt(string label, ref int value, int min, int max)
	{
		var (display, id) = SplitLabel(label);
		var trackWidth = 150f * M3.Scale;
		var controlSize = new Vector2(trackWidth + SliderValueGutter(max.ToString()), ButtonHeight);

		var row = M3SettingRow.Begin(display, null, controlSize);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = SliderInt($"##{id}_slider", ref value, min, max, value.ToString(), trackWidth);
		M3SettingRow.End(row);
		return changed;
	}

	#endregion

	#region Text fields

	// Busy swaps the search icon for a spinner, for while the results it found are on screen.
	public static bool SearchField(string id, string hint, ref string text, float width, int maxLength = 128, bool busy = false)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = M3.FitText(40f, 8f);
		var origin = ImGui.GetCursorScreenPos();
		var drawList = ImGui.GetWindowDrawList();
		var min = origin;
		var max = origin + new Vector2(width, height);
		var hoveringField = ImGui.IsMouseHoveringRect(min, max);

		drawList.AddRectFilled(min, max, M3.U32(s.SurfaceContainerHigh, hoveringField ? 1f : 0.92f), height * 0.5f);

		var iconPadding = 14f * scale;
		var iconWidth = M3Draw.MeasureIcon(FontAwesomeIcon.Search).X;
		if (busy)
		{
			var stroke = 2f * scale;
			Spinner(drawList, new Vector2(min.X + iconPadding + (iconWidth * 0.5f), min.Y + (height * 0.5f)),
				MathF.Max(1f, (iconWidth - stroke) * 0.5f), s.Primary, stroke);
		}
		else
		{
			M3Draw.Icon(drawList, FontAwesomeIcon.Search,
				new Vector2(min.X + iconPadding, min.Y + ((height - ImGui.GetTextLineHeight()) * 0.5f)),
				M3.Alpha(s.OnSurfaceVariant, 0.9f));
		}

		var fieldStart = min.X + iconPadding + iconWidth + (10f * scale);
		var hasText = !string.IsNullOrEmpty(text);
		var clearWidth = hasText ? 32f * scale : 0f;
		var fieldWidth = MathF.Max(24f * scale, max.X - fieldStart - (14f * scale) - clearWidth);

		ImGui.SetCursorScreenPos(new Vector2(fieldStart, min.Y + ((height - ImGui.GetFrameHeight()) * 0.5f)));
		ImGui.SetNextItemWidth(fieldWidth);

		bool changed;
		using (ImRaii.PushColor(ImGuiCol.FrameBg, new Vector4(0f, 0f, 0f, 0f))
			.Push(ImGuiCol.FrameBgHovered, new Vector4(0f, 0f, 0f, 0f))
			.Push(ImGuiCol.FrameBgActive, new Vector4(0f, 0f, 0f, 0f)))
		{
			changed = ImGui.InputTextWithHint(id, hint, ref text, maxLength, ImGuiInputTextFlags.AutoSelectAll);
		}

		if (hasText)
		{
			ImGui.SetCursorScreenPos(new Vector2(max.X - clearWidth - (6f * scale), min.Y + ((height - (26f * scale)) * 0.5f)));
			if (IconButton($"{id}_clear", FontAwesomeIcon.Times, "Clear search", diameter: 26f * scale))
			{
				text = string.Empty;
				changed = true;
			}
		}

		ImGui.SetCursorScreenPos(new Vector2(min.X, max.Y));
		ImGui.Dummy(new Vector2(width, 0f));
		return changed;
	}

	public static float TextFieldHeight => M3.FitText(48f, 12f);

	// Pass an id starting with "##" to hide ImGui's own label.
	public static bool TextField(string id, string label, ref string text, float width, int maxLength = 256, string? supporting = null, bool error = false, FontAwesomeIcon leadingIcon = FontAwesomeIcon.None, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = TextFieldHeight;
		var paddingX = 16f * scale;
		var framePadding = ImGui.GetStyle().FramePadding.X;
		var bodyFontSize = ImGui.GetFontSize();

		float smallFontSize;
		using (ImRaii.PushFont(M3.LabelSmall))
		{
			smallFontSize = ImGui.GetFontSize();
		}

		var origin = ImGui.GetCursorScreenPos();
		var min = origin + new Vector2(0f, smallFontSize * 0.5f);
		var max = min + new Vector2(width, height);
		var drawList = ImGui.GetWindowDrawList();
		var hovered = ImGui.IsMouseHoveringRect(min, max);

		var textLeft = min.X + paddingX;
		if (leadingIcon != FontAwesomeIcon.None)
		{
			var iconSize = M3Draw.MeasureIcon(leadingIcon);
			M3Draw.Icon(drawList, leadingIcon, new Vector2(textLeft, min.Y + ((height - iconSize.Y) * 0.5f)),
				error ? s.Error : s.OnSurfaceVariant);
			textLeft += iconSize.X + (12f * scale);
		}

		var trailingWidth = error ? M3Draw.MeasureIcon(FontAwesomeIcon.ExclamationCircle).X + (12f * scale) : 0f;
		var textRight = max.X - paddingX - trailingWidth;

		var inputLeft = textLeft - framePadding;
		ImGui.SetCursorScreenPos(new Vector2(inputLeft, min.Y + ((height - ImGui.GetFrameHeight()) * 0.5f)));
		ImGui.SetNextItemWidth(MathF.Max(24f * scale, textRight + framePadding - inputLeft));

		bool changed;
		using (ImRaii.PushColor(ImGuiCol.FrameBg, new Vector4(0f, 0f, 0f, 0f))
			.Push(ImGuiCol.FrameBgHovered, new Vector4(0f, 0f, 0f, 0f))
			.Push(ImGuiCol.FrameBgActive, new Vector4(0f, 0f, 0f, 0f)))
		{
			changed = ImGui.InputText(id, ref text, maxLength, flags);
		}

		var focused = ImGui.IsItemActive();
		var floating = M3Motion.Approach($"{id}_label", focused || !string.IsNullOrEmpty(text) ? 1f : 0f, M3Motion.FastDuration);

		var accent = error ? s.Error : focused ? s.Primary : hovered ? s.OnSurface : s.Outline;
		var labelColor = error ? s.Error : focused ? s.Primary : s.OnSurfaceVariant;

		var hasLabel = !string.IsNullOrEmpty(label);
		var floatingX = min.X + paddingX;
		var notchStart = floatingX - (4f * scale);
		var notchEnd = hasLabel
			? notchStart + (((ImGui.CalcTextSize(label).X * (smallFontSize / bodyFontSize)) + (8f * scale)) * floating)
			: notchStart;
		OutlineWithNotch(drawList, min, max, M3.ShapeExtraSmall, notchStart, notchEnd, M3.U32(accent), (focused || error ? 2f : 1f) * scale);

		if (hasLabel)
		{
			var restingPosition = new Vector2(textLeft, min.Y + ((height - bodyFontSize) * 0.5f));
			var floatingPosition = new Vector2(floatingX, min.Y - (smallFontSize * 0.5f));
			drawList.AddText(ImGui.GetFont(), float.Lerp(bodyFontSize, smallFontSize, floating),
				Vector2.Lerp(restingPosition, floatingPosition, floating), M3.U32(labelColor), label);
		}

		if (error)
		{
			var iconSize = M3Draw.MeasureIcon(FontAwesomeIcon.ExclamationCircle);
			M3Draw.Icon(drawList, FontAwesomeIcon.ExclamationCircle,
				new Vector2(max.X - paddingX - iconSize.X, min.Y + ((height - iconSize.Y) * 0.5f)), s.Error);
		}

		var bottom = max.Y;
		if (!string.IsNullOrEmpty(supporting))
		{
			using var font = ImRaii.PushFont(M3.LabelSmall);
			bottom = M3Draw.WrappedText(supporting, new Vector2(min.X + paddingX, max.Y + (4f * scale)),
				MathF.Max(32f * scale, width - (paddingX * 2f)), error ? s.Error : M3.Alpha(s.OnSurfaceVariant, 0.9f));
		}

		ImGui.SetCursorScreenPos(new Vector2(origin.X, bottom));
		ImGui.Dummy(new Vector2(width, 0f));
		return changed;
	}

	private static void OutlineWithNotch(ImDrawListPtr drawList, Vector2 min, Vector2 max, float rounding, float notchStart, float notchEnd, uint color, float thickness)
	{
		if (notchEnd - notchStart < 1f)
		{
			drawList.AddRect(min, max, color, rounding, ImDrawFlags.None, thickness);
			return;
		}

		// PathArcToFast angles are in twelfths of a turn: 0 right, 3 down, 6 left, 9 up.
		drawList.PathLineTo(new Vector2(notchEnd, min.Y));
		drawList.PathArcToFast(new Vector2(max.X - rounding, min.Y + rounding), rounding, 9, 12);
		drawList.PathArcToFast(new Vector2(max.X - rounding, max.Y - rounding), rounding, 0, 3);
		drawList.PathArcToFast(new Vector2(min.X + rounding, max.Y - rounding), rounding, 3, 6);
		drawList.PathArcToFast(new Vector2(min.X + rounding, min.Y + rounding), rounding, 6, 9);
		drawList.PathLineTo(new Vector2(notchStart, min.Y));
		drawList.PathStroke(color, ImDrawFlags.None, thickness);
	}

	#endregion

	#region Structure

	public static void Divider(float verticalPadding = 8f)
	{
		var scale = M3.Scale;
		var pad = verticalPadding * scale;
		ImGui.Dummy(new Vector2(0f, pad));
		var width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
		var origin = ImGui.GetCursorScreenPos();
		ImGui.GetWindowDrawList().AddLine(origin, origin + new Vector2(width, 0f), M3.U32(M3.Scheme.OutlineVariant, 0.7f), 1f * scale);
		ImGui.Dummy(new Vector2(width, pad));
	}

	public static void SectionLabel(string text, Vector4? accent = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var tone = accent ?? s.Primary;
		var label = text.ToUpperInvariant();

		using var font = ImRaii.PushFont(M3.LabelSmall);
		var textSize = ImGui.CalcTextSize(label);
		var width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
		var height = textSize.Y + (14f * scale);

		ImGui.Dummy(new Vector2(width, height));
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var textY = min.Y + (10f * scale);
		var lineY = textY + (textSize.Y * 0.5f);

		drawList.AddText(new Vector2(min.X, textY), M3.U32(tone, 0.95f), label);

		var lineStart = min.X + textSize.X + (10f * scale);
		if (max.X > lineStart)
		{
			drawList.AddLine(new Vector2(lineStart, lineY), new Vector2(max.X, lineY), M3.U32(s.OutlineVariant, 0.55f), 1f * scale);
		}
	}

	public static bool Banner(string id, string message, M3Severity severity, FontAwesomeIcon icon, string? actionLabel = null, string? tooltip = null)
	{
		return Banner(id, message, severity, icon, out _, actionLabel, tooltip);
	}

	public static bool Banner(string id, string message, M3Severity severity, FontAwesomeIcon icon, out bool hovered, string? actionLabel = null, string? tooltip = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var accent = M3.Severity(severity);
		var container = M3.SeverityContainer(severity);
		var padding = new Vector2(16f, 12f) * scale;
		var width = MathF.Max(64f * scale, ImGui.GetContentRegionAvail().X);

		var iconWidth = M3Draw.MeasureIcon(icon).X + (12f * scale);
		var actionWidth = string.IsNullOrEmpty(actionLabel) ? 0f : ButtonWidth(FontAwesomeIcon.None, actionLabel) + (12f * scale);
		var textWidth = MathF.Max(32f * scale, width - (padding.X * 2f) - iconWidth - actionWidth);
		var textSize = ImGui.CalcTextSize(message, false, textWidth);
		var height = MathF.Max(textSize.Y, string.IsNullOrEmpty(actionLabel) ? 0f : ButtonHeight) + (padding.Y * 2f);

		var origin = ImGui.GetCursorScreenPos();
		var min = origin;
		var max = origin + new Vector2(width, height);
		var drawList = ImGui.GetWindowDrawList();
		hovered = ImGui.IsMouseHoveringRect(min, max, false) && ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows);

		drawList.AddRectFilled(min, max, M3.U32(container, 0.55f), M3.ShapeMedium);
		M3Draw.AccentRail(drawList, min.X + (2f * scale), 3f * scale, min.Y + (6f * scale), max.Y - (6f * scale), M3.Alpha(accent, 0.9f),
			M3Draw.ResolveFade(40f, 0.35f, height));

		M3Draw.Icon(drawList, icon, new Vector2(min.X + padding.X, min.Y + padding.Y + ((ImGui.GetTextLineHeight() * 0.1f))), accent);
		_ = M3Draw.WrappedText(message, new Vector2(min.X + padding.X + iconWidth, min.Y + padding.Y), textWidth, M3.Alpha(s.OnSurface, 0.94f));

		var clicked = false;
		if (!string.IsNullOrEmpty(actionLabel))
		{
			ImGui.SetCursorScreenPos(new Vector2(max.X - padding.X - actionWidth + (12f * scale), min.Y + ((height - ButtonHeight) * 0.5f)));
			clicked = Button($"{id}_action", actionLabel, M3ButtonStyle.Text);
		}

		ImGui.SetCursorScreenPos(new Vector2(min.X, max.Y));
		ImGui.Dummy(new Vector2(width, 0f));

		if (hovered && !string.IsNullOrEmpty(tooltip))
		{
			ImGui.SetTooltip(tooltip);
		}

		return clicked;
	}

	public static bool EmptyState(string id, string headline, string? supporting = null, FontAwesomeIcon icon = FontAwesomeIcon.Inbox, string? actionLabel = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var width = MathF.Max(64f * scale, ImGui.GetContentRegionAvail().X);
		var textWidth = MathF.Min(width, 360f * scale);
		var origin = ImGui.GetCursorScreenPos();
		var centerX = origin.X + (width * 0.5f);
		var drawList = ImGui.GetWindowDrawList();
		var y = origin.Y + (16f * scale);

		var diameter = 56f * scale;
		var circleMin = new Vector2(centerX - (diameter * 0.5f), y);
		var circleMax = circleMin + new Vector2(diameter, diameter);
		drawList.AddCircleFilled((circleMin + circleMax) * 0.5f, diameter * 0.5f, M3.U32(s.SecondaryContainer, 0.8f), 48);
		M3Draw.IconCentered(drawList, icon, circleMin, circleMax, s.OnSecondaryContainer);
		y = circleMax.Y + (12f * scale);

		using (ImRaii.PushFont(M3.TitleMedium))
		{
			y = CenteredBlock(headline, centerX, y, textWidth, M3.Alpha(s.OnSurface, 0.95f));
		}

		if (!string.IsNullOrEmpty(supporting))
		{
			y = CenteredBlock(supporting, centerX, y + (4f * scale), textWidth, M3.Alpha(s.OnSurfaceVariant, 0.9f));
		}

		var clicked = false;
		if (!string.IsNullOrEmpty(actionLabel))
		{
			var buttonWidth = ButtonWidth(FontAwesomeIcon.None, actionLabel);
			ImGui.SetCursorScreenPos(new Vector2(centerX - (buttonWidth * 0.5f), y + (12f * scale)));
			clicked = Button($"{id}_action", actionLabel, M3ButtonStyle.Tonal);
			y = ImGui.GetItemRectMax().Y;
		}

		ImGui.SetCursorScreenPos(new Vector2(origin.X, y + (16f * scale)));
		ImGui.Dummy(new Vector2(width, 0f));
		return clicked;
	}

	private static float CenteredBlock(string text, float centerX, float top, float maxWidth, Vector4 color)
	{
		var blockWidth = ImGui.CalcTextSize(text, false, maxWidth).X;
		return M3Draw.WrappedText(text, new Vector2(centerX - (blockWidth * 0.5f), top), maxWidth, color);
	}

	#endregion

	#region Select

	public static float ComboHeight => M3.FitText(38f, 8f);

	private static float ComboTextInset => 12f * M3.Scale;
	private static float ComboChevronWidth => 24f * M3.Scale;

	public static float ComboWidthFor(string label)
	{
		return MathF.Ceiling(ImGui.CalcTextSize(label).X) + (ComboTextInset * 2f) + ComboChevronWidth + 1f;
	}

	public static bool Combo(string id, ref int index, IReadOnlyList<string> items, float width, string? emptyText = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = ComboHeight;
		var popupId = $"{id}_menu";

		var clicked = ImGui.InvisibleButton(id, new Vector2(width, height));
		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = ImGui.IsItemActive();
		var open = ImGui.IsPopupOpen(popupId);
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var rounding = M3.ShapeSmall;

		var fill = M3.Alpha(s.SurfaceContainerHighest, 0.55f);
		if (hovered || held)
		{
			fill = M3.StateLayer(fill, s.OnSurface, hovered, held);
		}

		drawList.AddRectFilled(min, max, M3.U32(fill), rounding);
		drawList.AddRect(min, max, M3.U32(open ? s.Primary : s.Outline, open ? 1f : 0.75f), rounding, ImDrawFlags.None, (open ? 2f : 1f) * scale);

		var label = index >= 0 && index < items.Count ? items[index] : emptyText ?? string.Empty;
		var textWidth = MathF.Max(8f * scale, width - (ComboTextInset * 2f) - ComboChevronWidth);
		var display = M3Navigation.Truncate(label, textWidth);
		var textSize = ImGui.CalcTextSize(display);
		drawList.AddText(new Vector2(min.X + ComboTextInset, min.Y + ((height - textSize.Y) * 0.5f)), M3.U32(s.OnSurface, 0.95f), display);

		var chevronCenter = new Vector2(max.X - (16f * scale), min.Y + (height * 0.5f));
		var arm = 4.5f * scale;
		var chevronColor = M3.U32(s.OnSurfaceVariant, hovered || open ? 1f : 0.8f);
		drawList.AddLine(chevronCenter + new Vector2(-arm, -arm * 0.5f), chevronCenter + new Vector2(0f, arm * 0.6f), chevronColor, 2f * scale);
		drawList.AddLine(chevronCenter + new Vector2(0f, arm * 0.6f), chevronCenter + new Vector2(arm, -arm * 0.5f), chevronColor, 2f * scale);

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		if (clicked)
		{
			ImGui.OpenPopup(popupId);
		}

		var changed = false;
		ImGui.SetNextWindowSizeConstraints(new Vector2(MathF.Max(width, 160f * scale), 0f), new Vector2(float.MaxValue, 420f * scale));
		using var popup = ImRaii.Popup(popupId);
		if (popup)
		{
			for (var i = 0; i < items.Count; i++)
			{
				if (MenuItem($"{popupId}_{i}", items[i], i == index))
				{
					index = i;
					changed = true;
					ImGui.CloseCurrentPopup();
				}
			}
		}

		return changed;
	}

	public static bool MenuItem(string id, string label, bool selected, FontAwesomeIcon icon = FontAwesomeIcon.None)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = M3.FitText(34f, 7f);
		var checkWidth = 24f * scale;
		var textSize = ImGui.CalcTextSize(label);
		var width = MathF.Max(ImGui.GetContentRegionAvail().X, checkWidth + textSize.X + (24f * scale));

		var pressed = ImGui.InvisibleButton(id, new Vector2(width, height));
		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();

		if (selected)
		{
			drawList.AddRectFilled(min, max, M3.U32(s.SecondaryContainer, 0.8f), M3.ShapeSmall);
		}

		if (hovered || held)
		{
			drawList.AddRectFilled(min, max, M3.U32(s.OnSurface, held ? M3.StatePressed : M3.StateHover), M3.ShapeSmall);
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		if (selected)
		{
			M3Draw.Icon(drawList, FontAwesomeIcon.Check,
				new Vector2(min.X + (8f * scale), min.Y + ((height - ImGui.GetTextLineHeight()) * 0.5f)), s.Primary);
		}
		else if (icon != FontAwesomeIcon.None)
		{
			M3Draw.Icon(drawList, icon,
				new Vector2(min.X + (8f * scale), min.Y + ((height - ImGui.GetTextLineHeight()) * 0.5f)), s.OnSurfaceVariant);
		}

		drawList.AddText(
			new Vector2(min.X + checkWidth + (8f * scale), min.Y + ((height - textSize.Y) * 0.5f)),
			M3.U32(selected ? s.OnSecondaryContainer : s.OnSurface, 0.95f), label);

		return pressed;
	}

	#endregion

	#region Misc

	public static bool ColorSwatch(string id, ref Vector4 color)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var diameter = 28f * scale;

		var clicked = ImGui.InvisibleButton(id, Vector2.One * diameter);
		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var min = ImGui.GetItemRectMin();
		var center = min + (Vector2.One * diameter * 0.5f);
		var drawList = ImGui.GetWindowDrawList();

		drawList.AddCircleFilled(center, diameter * 0.5f, M3.U32(color with { W = 1f }), 32);
		drawList.AddCircle(center, diameter * 0.5f, M3.U32(s.Outline, hovered ? 1f : 0.6f), 32, 1.5f * scale);

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		var popupId = $"{id}_picker";
		if (clicked)
		{
			ImGui.OpenPopup(popupId);
		}

		var changed = false;
		using var popup = ImRaii.Popup(popupId);
		if (popup)
		{
			changed = ImGui.ColorPicker4($"##{popupId}_picker", ref color, ImGuiColorEditFlags.AlphaBar);
		}

		return changed;
	}

	#endregion
}
