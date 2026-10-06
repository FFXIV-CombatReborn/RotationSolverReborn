using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;

namespace RotationSolver.UI.Material;

internal static class M3SubGroup
{
	public static Scope Begin(Vector4? accent = null)
	{
		var scale = M3.Scale;
		var indent = 22f * scale;
		var start = ImGui.GetCursorScreenPos();
		ImGui.Indent(indent);
		ImGui.Dummy(new Vector2(0f, 2f * scale));

		return new Scope(start, indent, accent ?? M3.Scheme.OutlineVariant);
	}

	internal readonly struct Scope(Vector2 start, float indent, Vector4 accent) : IDisposable
	{
		public void Dispose()
		{
			var scale = M3.Scale;
			ImGui.Dummy(new Vector2(0f, 2f * scale));
			var end = ImGui.GetCursorScreenPos();
			ImGui.Unindent(indent);

			var x = start.X + (8f * scale);
			if (end.Y > start.Y)
			{
				ImGui.GetWindowDrawList().AddLine(
					new Vector2(x, start.Y + (2f * scale)),
					new Vector2(x, end.Y - (4f * scale)),
					M3.U32(accent, 0.45f), 2f * scale);
			}
		}
	}
}

internal readonly struct M3RowInfo
{
	public Vector2 Min { get; init; }
	public Vector2 Max { get; init; }

	public Vector2 ControlPosition { get; init; }

	public float ControlWidth { get; init; }

	public bool Hovered { get; init; }

	public bool ControlBelow { get; init; }
}

internal static class M3SettingRow
{
	private static float PaddingX => M3Style.Spacing(12f, 8f);
	private static float PaddingY => M3Style.Spacing(9f, 5f);
	private static float ControlGap => M3Style.Spacing(16f, 10f);

	private static float MinLabelWidth => 120f * M3.Scale;

	private static float RowWidth => MathF.Max(64f * M3.Scale, ImGui.GetContentRegionAvail().X - M3Card.RightInset);

	private static float InnerWidth(float rowWidth, float iconWidth)
	{
		return MathF.Max(32f * M3.Scale, rowWidth - (PaddingX * 2f) - iconWidth);
	}

	private static float IconWidth(FontAwesomeIcon icon)
	{
		return icon == FontAwesomeIcon.None ? 0f : M3Draw.MeasureIcon(icon).X + (10f * M3.Scale);
	}

	public static float MaxControlWidth(FontAwesomeIcon leadingIcon = FontAwesomeIcon.None)
	{
		return InnerWidth(RowWidth, IconWidth(leadingIcon));
	}

	public static M3RowInfo Begin(
		string label,
		string? supporting,
		Vector2 controlSize,
		Vector4? labelColor = null,
		FontAwesomeIcon leadingIcon = FontAwesomeIcon.None,
		bool disabled = false,
		bool strikeThrough = false,
		IDalamudTextureWrap? leadingTexture = null,
		float leadingTextureSize = 32f)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;

		// End() already starts a new line. Checking for one here would misfire in table cells.
		var min = ImGui.GetCursorScreenPos();
		var width = RowWidth;

		var texture = leadingTexture?.Handle == null ? null : leadingTexture;
		var textureExtent = texture == null ? 0f : leadingTextureSize * scale;
		var iconWidth = texture != null
			? textureExtent + (10f * scale)
			: IconWidth(leadingIcon);
		var textLeft = min.X + PaddingX + iconWidth;
		var innerWidth = InnerWidth(width, iconWidth);

		var hasControl = controlSize.X > 0f && controlSize.Y > 0f;
		var controlBelow = hasControl && innerWidth - controlSize.X - ControlGap < MinLabelWidth;
		var labelWidth = hasControl && !controlBelow
			? MathF.Max(32f * scale, innerWidth - controlSize.X - ControlGap)
			: innerWidth;

		var labelSize = string.IsNullOrEmpty(label) ? Vector2.Zero : ImGui.CalcTextSize(label, false, labelWidth);

		var supportingHeight = 0f;
		if (!string.IsNullOrEmpty(supporting))
		{
			using var font = ImRaii.PushFont(M3.LabelSmall);
			supportingHeight = ImGui.CalcTextSize(supporting, false, innerWidth).Y + (4f * scale);
		}

		var headlineHeight = hasControl && !controlBelow
			? MathF.Max(labelSize.Y, controlSize.Y)
			: labelSize.Y;
		headlineHeight = MathF.Max(headlineHeight, textureExtent);
		var belowHeight = hasControl && controlBelow ? controlSize.Y + (8f * scale) : 0f;
		var height = (PaddingY * 2f) + headlineHeight + supportingHeight + belowHeight;

		ImGui.Dummy(new Vector2(width, height));
		var max = ImGui.GetItemRectMax();
		var hovered = ImGui.IsMouseHoveringRect(min, max, false) && ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows);

		var drawList = ImGui.GetWindowDrawList();
		if (hovered)
		{
			drawList.AddRectFilled(min, max, M3.U32(s.OnSurface, 0.045f), M3.ShapeSmall);
		}

		var contentColor = disabled
			? M3.Alpha(s.OnSurface, M3.DisabledContent)
			: labelColor ?? M3.Alpha(s.OnSurface, 0.95f);

		if (texture != null)
		{
			var textureMin = new Vector2(min.X + PaddingX, min.Y + PaddingY + ((headlineHeight - textureExtent) * 0.5f));
			drawList.AddImage(texture.Handle, textureMin, textureMin + new Vector2(textureExtent, textureExtent),
				Vector2.Zero, Vector2.One, M3.U32(Vector4.One, disabled ? M3.DisabledContent : 1f));
		}
		else if (leadingIcon != FontAwesomeIcon.None)
		{
			var iconSize = M3Draw.MeasureIcon(leadingIcon);
			M3Draw.Icon(drawList, leadingIcon,
				new Vector2(min.X + PaddingX, min.Y + PaddingY + ((headlineHeight - iconSize.Y) * 0.5f)),
				M3.Alpha(s.Primary, disabled ? M3.DisabledContent : 0.9f));
		}

		if (!string.IsNullOrEmpty(label))
		{
			var labelY = min.Y + PaddingY + ((headlineHeight - labelSize.Y) * 0.5f);
			_ = M3Draw.WrappedText(label, new Vector2(textLeft, labelY), labelWidth, contentColor);

			if (strikeThrough)
			{
				var lineCount = MathF.Max(1f, MathF.Round(labelSize.Y / ImGui.GetTextLineHeight()));
				var lineHeight = labelSize.Y / lineCount;
				for (var line = 0; line < (int)lineCount; line++)
				{
					var y = labelY + (lineHeight * (line + 0.5f));
					var lineWidth = line == (int)lineCount - 1 ? MathF.Min(labelSize.X, labelWidth) : labelWidth;
					drawList.AddLine(new Vector2(textLeft, y), new Vector2(textLeft + lineWidth, y), M3.U32(contentColor, 0.7f), 1f * scale);
				}
			}
		}

		if (!string.IsNullOrEmpty(supporting))
		{
			using var font = ImRaii.PushFont(M3.LabelSmall);
			_ = M3Draw.WrappedText(supporting,
				new Vector2(textLeft, min.Y + PaddingY + headlineHeight + (4f * scale)),
				innerWidth,
				M3.Alpha(s.OnSurfaceVariant, disabled ? M3.DisabledContent : 0.88f));
		}

		var controlPosition = controlBelow
			? new Vector2(textLeft, min.Y + PaddingY + headlineHeight + supportingHeight + (8f * scale))
			: new Vector2(max.X - PaddingX - controlSize.X, min.Y + PaddingY + ((headlineHeight - controlSize.Y) * 0.5f));

		if (hasControl)
		{
			ImGui.SetCursorScreenPos(controlPosition);
		}

		return new M3RowInfo
		{
			Min = min,
			Max = max,
			ControlPosition = controlPosition,
			ControlWidth = controlSize.X,
			Hovered = hovered,
			ControlBelow = controlBelow,
		};
	}

	public static void End(in M3RowInfo row)
	{
		ImGui.SetCursorScreenPos(new Vector2(row.Min.X, row.Max.Y));
		ImGui.Dummy(new Vector2(row.Max.X - row.Min.X, 2f * M3.Scale));
	}

	public static bool NavigationRow(string id, string label, string? supporting, FontAwesomeIcon leadingIcon, FontAwesomeIcon trailingIcon, Vector4? accent = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var tone = accent ?? s.Primary;
		var width = MathF.Max(64f * scale, ImGui.GetContentRegionAvail().X - M3Card.RightInset);

		var iconWidth = leadingIcon == FontAwesomeIcon.None ? 0f : M3Draw.MeasureIcon(leadingIcon).X + (12f * scale);
		var trailingWidth = trailingIcon == FontAwesomeIcon.None ? 0f : M3Draw.MeasureIcon(trailingIcon).X + (12f * scale);
		var textWidth = MathF.Max(32f * scale, width - (PaddingX * 2f) - iconWidth - trailingWidth);

		var labelSize = ImGui.CalcTextSize(label, false, textWidth);
		var supportingHeight = 0f;
		if (!string.IsNullOrEmpty(supporting))
		{
			using var font = ImRaii.PushFont(M3.LabelSmall);
			supportingHeight = ImGui.CalcTextSize(supporting, false, textWidth).Y + (4f * scale);
		}

		var height = (PaddingY * 2f) + labelSize.Y + supportingHeight;
		var pressed = ImGui.InvisibleButton(id, new Vector2(width, height));
		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();

		if (hovered || held)
		{
			drawList.AddRectFilled(min, max, M3.U32(s.OnSurface, held ? M3.StatePressed : M3.StateHover), M3.ShapeSmall);
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		var textLeft = min.X + PaddingX + iconWidth;
		if (leadingIcon != FontAwesomeIcon.None)
		{
			var iconSize = M3Draw.MeasureIcon(leadingIcon);
			M3Draw.Icon(drawList, leadingIcon, new Vector2(min.X + PaddingX, min.Y + PaddingY + ((labelSize.Y - iconSize.Y) * 0.5f)), tone);
		}

		_ = M3Draw.WrappedText(label, new Vector2(textLeft, min.Y + PaddingY), textWidth, M3.Alpha(s.OnSurface, 0.95f));

		if (!string.IsNullOrEmpty(supporting))
		{
			using var font = ImRaii.PushFont(M3.LabelSmall);
			_ = M3Draw.WrappedText(supporting, new Vector2(textLeft, min.Y + PaddingY + labelSize.Y + (4f * scale)), textWidth,
				M3.Alpha(s.OnSurfaceVariant, 0.85f));
		}

		if (trailingIcon != FontAwesomeIcon.None)
		{
			var iconSize = M3Draw.MeasureIcon(trailingIcon);
			M3Draw.Icon(drawList, trailingIcon,
				new Vector2(max.X - PaddingX - iconSize.X, min.Y + ((height - iconSize.Y) * 0.5f)),
				M3.Alpha(s.OnSurfaceVariant, hovered ? 1f : 0.7f));
		}

		ImGui.SetCursorScreenPos(new Vector2(min.X, max.Y));
		ImGui.Dummy(new Vector2(width, 2f * scale));
		return pressed;
	}
}
