using Dalamud.Interface.Textures.TextureWraps;

namespace RotationSolver.UI.Material;

internal static class M3ActionIcon
{
	public static float Rounding(float size)
	{
		return MathF.Min(M3.ShapeSmall, size * 0.18f);
	}

	public static bool Draw(string id, IAction? action, float size, bool showCooldown, string? tooltip = null)
	{
		var s = M3.Scheme;

		// Size settings can go to 0, which InvisibleButton rejects.
		size = MathF.Max(size, 1f);
		var extent = new Vector2(size, size);

		var pressed = ImGui.InvisibleButton(id, extent);
		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = min + extent;
		var drawList = ImGui.GetWindowDrawList();
		var rounding = Rounding(size);

		IDalamudTextureWrap? texture = null;
		if (action != null && action.GetTexture(out var wrap))
		{
			texture = wrap;
		}

		if (action == null || !Image(drawList, texture, min, max, rounding, action.EnoughLevel ? 1f : M3.DisabledContent))
		{
			EmptySlot(drawList, min, max, rounding);
			return false;
		}

		if (showCooldown && action.EnoughLevel)
		{
			Cooldown(drawList, action.Cooldown, min, max, rounding);
		}

		drawList.AddRect(min, max, M3.U32(s.OutlineVariant, 0.8f), rounding, ImDrawFlags.None, 1f * M3.Scale);

		if (hovered || held)
		{
			drawList.AddRectFilled(min, max, M3.U32(s.OnSurface, held ? M3.StatePressed : M3.StateHover), rounding);
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		if (hovered)
		{
			ImguiTooltips.ShowTooltip(tooltip ?? action.Name);
		}

		return pressed;
	}

	public static bool Image(ImDrawListPtr drawList, IDalamudTextureWrap? texture, Vector2 min, Vector2 max, float rounding, float alpha = 1f)
	{
		if (texture == null)
		{
			return false;
		}

		try
		{
			var handle = texture.Handle;
			if (handle.IsNull)
			{
				return false;
			}

			drawList.AddImageRounded(handle, min, max, Vector2.Zero, Vector2.One, M3.U32(Vector4.One, alpha), rounding);
			return true;
		}
		catch (ObjectDisposedException)
		{
			return false;
		}
	}

	public static void EmptySlot(ImDrawListPtr drawList, Vector2 min, Vector2 max, float rounding)
	{
		var s = M3.Scheme;
		drawList.AddRectFilled(min, max, M3.U32(s.SurfaceContainerLowest, 0.55f), rounding);
		drawList.AddRect(min, max, M3.U32(s.OutlineVariant, 0.6f), rounding, ImDrawFlags.None, 1f * M3.Scale);
	}

	private static void Cooldown(ImDrawListPtr drawList, ICooldown cooldown, Vector2 min, Vector2 max, float rounding)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var size = max.X - min.X;
		var center = (min + max) * 0.5f;

		if (cooldown.IsCoolingDown)
		{
			var recast = cooldown.RecastTimeOneChargeRaw;
			var elapsed = recast > 0f ? cooldown.RecastTimeElapsedRaw % recast : 0f;

			var usable = cooldown.MaxCharges > 1 && cooldown.CurrentCharges > 0;
			drawList.AddRectFilled(min, max, M3.U32(s.Scrim, usable ? 0.25f : 0.6f), rounding);

			if (recast > 0f && size >= 28f * scale)
			{
				var thickness = MathF.Max(2f * scale, size * 0.06f);
				var radius = (size * 0.5f) - thickness - (3f * scale);
				drawList.AddCircle(center, radius, M3.U32(s.OnSurface, 0.2f), 32, thickness);
				M3Draw.Arc(drawList, center, radius, 0f, elapsed / recast, s.Primary, thickness);
			}

			var text = recast <= 0f ? "0" : ((int)(recast - elapsed) + 1).ToString();
			var textSize = ImGui.CalcTextSize(text);
			var textPosition = center - (textSize * 0.5f);
			drawList.AddText(textPosition + (new Vector2(1f, 1f) * scale), M3.U32(s.Scrim, 0.8f), text);
			drawList.AddText(textPosition, M3.U32(s.OnSurface), text);
		}

		if (cooldown.MaxCharges > 1)
		{
			Charges(drawList, cooldown.CurrentCharges, cooldown.MaxCharges, min, max);
		}
	}

	private static void Charges(ImDrawListPtr drawList, int current, int maximum, Vector2 min, Vector2 max)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var size = max.X - min.X;
		var inset = 4f * scale;

		var radius = MathF.Min(MathF.Max(2f * scale, size * 0.045f), (size - (inset * 2f)) / (maximum * 3.2f));
		var gap = radius * 1.2f;
		var rowWidth = (maximum * radius * 2f) + ((maximum - 1) * gap);
		var y = max.Y - radius - inset;
		var x = ((min.X + max.X) * 0.5f) - (rowWidth * 0.5f) + radius;

		var pad = 2f * scale;
		drawList.AddRectFilled(
			new Vector2(x - radius - pad, y - radius - pad),
			new Vector2(x - radius + rowWidth + pad, y + radius + pad),
			M3.U32(s.Scrim, 0.55f), radius + pad);

		for (var i = 0; i < maximum; i++)
		{
			var pip = new Vector2(x + (i * ((radius * 2f) + gap)), y);
			if (i < current)
			{
				drawList.AddCircleFilled(pip, radius, M3.U32(s.Primary), 12);
			}
			else
			{
				drawList.AddCircle(pip, radius - (0.5f * scale), M3.U32(s.OnSurface, 0.6f), 12, 1f * scale);
			}
		}
	}
}
