using Dalamud.Interface.Utility.Raii;

namespace RotationSolver.UI.Material;

internal readonly record struct M3DialogAction(
	string Label,
	M3ButtonStyle Style = M3ButtonStyle.Text,
	FontAwesomeIcon Icon = FontAwesomeIcon.None,
	bool Enabled = true);

// Call Open once, then Begin every frame from the same ID scope.
internal static class M3Dialog
{
	private const ImGuiWindowFlags Flags = ImGuiWindowFlags.AlwaysAutoResize
		| ImGuiWindowFlags.NoTitleBar
		| ImGuiWindowFlags.NoSavedSettings;

	public static void Open(string id)
	{
		ImGui.OpenPopup(id);
	}

	public static Scope Begin(string id, string headline, string? supporting = null, FontAwesomeIcon icon = FontAwesomeIcon.None, float width = 360f, bool dismissible = true)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;

		ImGui.SetNextWindowSizeConstraints(new Vector2(width * scale, 0f), new Vector2(MathF.Max(width, 560f) * scale, 720f * scale));

		bool open;
		using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(24f, 24f) * scale)
			.Push(ImGuiStyleVar.WindowRounding, M3.ShapeExtraLarge))
		{
			open = ImGui.BeginPopupModal(id, Flags);
		}

		if (!open)
		{
			return default;
		}

		if (dismissible && ImGui.IsKeyPressed(ImGuiKey.Escape) && !ImGui.IsAnyItemActive())
		{
			ImGui.CloseCurrentPopup();
		}

		var contentWidth = ImGui.GetContentRegionAvail().X;
		var centred = icon != FontAwesomeIcon.None;

		if (centred)
		{
			var iconSize = M3Draw.MeasureIcon(icon);
			var origin = ImGui.GetCursorScreenPos();
			ImGui.Dummy(new Vector2(contentWidth, iconSize.Y));
			M3Draw.Icon(ImGui.GetWindowDrawList(), icon, new Vector2(origin.X + ((contentWidth - iconSize.X) * 0.5f), origin.Y), s.Secondary);
			ImGui.Dummy(new Vector2(0f, 8f * scale));
		}

		using (ImRaii.PushFont(M3.HeadlineSmall))
		{
			var headlineWidth = ImGui.CalcTextSize(headline).X;
			if (centred && headlineWidth <= contentWidth)
			{
				ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ((contentWidth - headlineWidth) * 0.5f));
				ImGui.TextUnformatted(headline);
			}
			else
			{
				ImGui.TextWrapped(headline);
			}
		}

		if (!string.IsNullOrEmpty(supporting))
		{
			ImGui.Dummy(new Vector2(0f, 8f * scale));
			using var color = ImRaii.PushColor(ImGuiCol.Text, s.OnSurfaceVariant);
			ImGui.TextWrapped(supporting);
		}

		ImGui.Dummy(new Vector2(0f, 8f * scale));
		return new Scope(true);
	}

	public static int Actions(ReadOnlySpan<M3DialogAction> actions)
	{
		var gap = M3.Space2;
		var total = 0f;
		foreach (var action in actions)
		{
			total += M3Widgets.ButtonWidth(action.Icon, action.Label);
		}

		total += gap * Math.Max(0, actions.Length - 1);

		ImGui.Dummy(new Vector2(0f, M3.Space3));
		ImGui.SetCursorPosX(MathF.Max(ImGui.GetCursorPosX(), ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - total));

		var pressed = -1;
		for (var i = 0; i < actions.Length; i++)
		{
			if (i > 0)
			{
				ImGui.SameLine(0f, gap);
			}

			var action = actions[i];
			if (M3Widgets.Button($"##dialog_action_{i}", action.Label, action.Style, action.Icon, enabled: action.Enabled))
			{
				pressed = i;
			}
		}

		if (pressed >= 0)
		{
			ImGui.CloseCurrentPopup();
		}

		return pressed;
	}

	internal readonly struct Scope(bool open) : IDisposable
	{
		public bool IsOpen => open;

		public void Dispose()
		{
			if (open)
			{
				ImGui.EndPopup();
			}
		}
	}
}
