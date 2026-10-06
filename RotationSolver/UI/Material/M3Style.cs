namespace RotationSolver.UI.Material;

internal enum M3Density
{
	Comfortable,

	Compact,

	// Compact spacing, plus thin window edges and the tighter card, row and drawer padding from M3Style.Spacing.
	Tight,
}

internal static class M3Style
{
	public static M3Density Density { get; private set; }

	// Scaled padding for shared components, which pull in only inside a Tight window so the other windows keep their look.
	public static float Spacing(float regular, float tight)
	{
		return (Density == M3Density.Tight ? tight : regular) * M3.PaddingScale;
	}

	public static Scope Push(M3Density density = M3Density.Comfortable)
	{
		var compact = density != M3Density.Comfortable;
		var s = M3.Scheme;
		var scale = M3.Scale;
		var padding = M3.PaddingScale;
		var colors = 0;

		void Color(ImGuiCol target, Vector4 value)
		{
			ImGui.PushStyleColor(target, value);
			colors++;
		}

		Color(ImGuiCol.Text, s.OnSurface);
		Color(ImGuiCol.TextDisabled, M3.Alpha(s.OnSurfaceVariant, M3.DisabledContent));
		Color(ImGuiCol.WindowBg, M3.Alpha(s.Surface, 0.97f));
		Color(ImGuiCol.ChildBg, new Vector4(0f, 0f, 0f, 0f));
		Color(ImGuiCol.PopupBg, M3.Alpha(s.SurfaceContainerHigh, 0.99f));
		Color(ImGuiCol.Border, M3.Alpha(s.OutlineVariant, 0.80f));
		Color(ImGuiCol.BorderShadow, new Vector4(0f, 0f, 0f, 0f));

		Color(ImGuiCol.FrameBg, M3.Alpha(s.SurfaceContainerHighest, 0.90f));
		Color(ImGuiCol.FrameBgHovered, M3.StateLayer(s.SurfaceContainerHighest, s.OnSurface, true, false));
		Color(ImGuiCol.FrameBgActive, M3.StateLayer(s.SurfaceContainerHighest, s.Primary, true, true));

		Color(ImGuiCol.TitleBg, s.SurfaceContainerLow);
		Color(ImGuiCol.TitleBgActive, s.SurfaceContainer);
		Color(ImGuiCol.TitleBgCollapsed, M3.Alpha(s.SurfaceContainerLow, 0.85f));
		Color(ImGuiCol.MenuBarBg, s.SurfaceContainer);

		Color(ImGuiCol.ScrollbarBg, new Vector4(0f, 0f, 0f, 0f));
		Color(ImGuiCol.ScrollbarGrab, M3.Alpha(s.Outline, 0.45f));
		Color(ImGuiCol.ScrollbarGrabHovered, M3.Alpha(s.Outline, 0.70f));
		Color(ImGuiCol.ScrollbarGrabActive, M3.Alpha(s.Primary, 0.80f));

		Color(ImGuiCol.CheckMark, s.Primary);
		Color(ImGuiCol.SliderGrab, s.Primary);
		Color(ImGuiCol.SliderGrabActive, s.PrimaryFixedDim);

		Color(ImGuiCol.Button, M3.Alpha(s.SecondaryContainer, 0.85f));
		Color(ImGuiCol.ButtonHovered, M3.StateLayer(s.SecondaryContainer, s.OnSecondaryContainer, true, false));
		Color(ImGuiCol.ButtonActive, M3.StateLayer(s.SecondaryContainer, s.OnSecondaryContainer, true, true));

		Color(ImGuiCol.Header, M3.Alpha(s.SecondaryContainer, 0.55f));
		Color(ImGuiCol.HeaderHovered, M3.Alpha(s.OnSurface, M3.StateHover));
		Color(ImGuiCol.HeaderActive, M3.Alpha(s.Primary, M3.StatePressed + M3.StateHover));

		Color(ImGuiCol.Separator, M3.Alpha(s.OutlineVariant, 0.70f));
		Color(ImGuiCol.SeparatorHovered, M3.Alpha(s.Primary, 0.60f));
		Color(ImGuiCol.SeparatorActive, s.Primary);

		Color(ImGuiCol.ResizeGrip, new Vector4(0f, 0f, 0f, 0f));
		Color(ImGuiCol.ResizeGripHovered, M3.Alpha(s.Primary, 0.35f));
		Color(ImGuiCol.ResizeGripActive, M3.Alpha(s.Primary, 0.65f));

		Color(ImGuiCol.Tab, M3.Alpha(s.SurfaceContainer, 0.90f));
		Color(ImGuiCol.TabHovered, M3.Alpha(s.SecondaryContainer, 0.90f));
		Color(ImGuiCol.TabActive, s.SecondaryContainer);
		Color(ImGuiCol.TabUnfocused, M3.Alpha(s.SurfaceContainerLow, 0.90f));
		Color(ImGuiCol.TabUnfocusedActive, M3.Alpha(s.SecondaryContainer, 0.70f));

		Color(ImGuiCol.PlotLines, s.Primary);
		Color(ImGuiCol.PlotLinesHovered, s.PrimaryFixedDim);
		Color(ImGuiCol.PlotHistogram, s.Tertiary);
		Color(ImGuiCol.PlotHistogramHovered, s.TertiaryContainer);

		Color(ImGuiCol.TableHeaderBg, s.SurfaceContainerHigh);
		Color(ImGuiCol.TableBorderStrong, M3.Alpha(s.Outline, 0.55f));
		Color(ImGuiCol.TableBorderLight, M3.Alpha(s.OutlineVariant, 0.55f));
		Color(ImGuiCol.TableRowBg, new Vector4(0f, 0f, 0f, 0f));
		Color(ImGuiCol.TableRowBgAlt, M3.Alpha(s.OnSurface, 0.035f));

		Color(ImGuiCol.TextSelectedBg, M3.Alpha(s.Primary, 0.35f));
		Color(ImGuiCol.DragDropTarget, s.Tertiary);
		Color(ImGuiCol.NavHighlight, M3.Alpha(s.Primary, 0.70f));
		Color(ImGuiCol.NavWindowingHighlight, M3.Alpha(s.OnSurface, 0.35f));
		Color(ImGuiCol.NavWindowingDimBg, M3.Alpha(s.Scrim, 0.35f));
		Color(ImGuiCol.ModalWindowDimBg, M3.Alpha(s.Scrim, 0.55f));

		var vars = 0;

		void Vec(ImGuiStyleVar target, Vector2 value)
		{
			ImGui.PushStyleVar(target, value);
			vars++;
		}

		void Num(ImGuiStyleVar target, float value)
		{
			ImGui.PushStyleVar(target, value);
			vars++;
		}

		Vec(ImGuiStyleVar.WindowPadding, density switch
		{
			M3Density.Tight => new Vector2(6f, 6f),
			M3Density.Compact => new Vector2(10f, 8f),
			_ => new Vector2(16f, 14f),
		} * padding);
		// Frame padding sets the height of inputs and combos, so it follows the element size, not the Padding setting.
		Vec(ImGuiStyleVar.FramePadding, (compact ? new Vector2(6f, 3f) : new Vector2(10f, 6f)) * scale);
		Vec(ImGuiStyleVar.CellPadding, (compact ? new Vector2(4f, 2f) : new Vector2(8f, 6f)) * padding);
		Vec(ImGuiStyleVar.ItemSpacing, (compact ? new Vector2(5f, 4f) : new Vector2(8f, 8f)) * padding);
		Vec(ImGuiStyleVar.ItemInnerSpacing, (compact ? new Vector2(4f, 3f) : new Vector2(8f, 6f)) * padding);
		Vec(ImGuiStyleVar.SelectableTextAlign, new Vector2(0f, 0.5f));
		Vec(ImGuiStyleVar.WindowTitleAlign, new Vector2(0f, 0.5f));
		Vec(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.5f, 0.5f));

		Num(ImGuiStyleVar.IndentSpacing, 20f * scale);
		Num(ImGuiStyleVar.ScrollbarSize, 10f * scale);
		Num(ImGuiStyleVar.GrabMinSize, 18f * scale);
		Num(ImGuiStyleVar.WindowBorderSize, 0f);
		Num(ImGuiStyleVar.ChildBorderSize, 0f);
		Num(ImGuiStyleVar.PopupBorderSize, 1f * scale);
		Num(ImGuiStyleVar.FrameBorderSize, 0f);
		Num(ImGuiStyleVar.WindowRounding, compact ? M3.ShapeMedium : M3.ShapeLarge);
		Num(ImGuiStyleVar.ChildRounding, M3.ShapeMedium);
		Num(ImGuiStyleVar.FrameRounding, M3.ShapeSmall);
		Num(ImGuiStyleVar.PopupRounding, M3.ShapeMedium);
		Num(ImGuiStyleVar.ScrollbarRounding, M3.ShapeFull);
		Num(ImGuiStyleVar.GrabRounding, M3.ShapeFull);
		Num(ImGuiStyleVar.TabRounding, M3.ShapeSmall);

		var previous = Density;
		Density = density;
		return new Scope(colors, vars, M3.PushBody(), previous);
	}

	internal readonly struct Scope(int colors, int vars, M3.FontScope font, M3Density previous) : IDisposable
	{
		public void Dispose()
		{
			Density = previous;
			font.Dispose();

			if (vars > 0)
			{
				ImGui.PopStyleVar(vars);
			}

			if (colors > 0)
			{
				ImGui.PopStyleColor(colors);
			}
		}
	}
}
