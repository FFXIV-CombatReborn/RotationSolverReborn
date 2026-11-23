using RebornMaterial;

namespace RotationSolver.UI.SearchableConfigs;

internal class AccentColorSearch(PropertyInfo property) : ColorEditSearch(property)
{
	internal static readonly M3ColorChoice[] Presets =
	[
		new("Crimson (default)", M3ColorMath.FromRgb(0xB0201F)),
		new("Ocean", M3ColorMath.FromRgb(0x2F6DB5)),
		new("Teal", M3ColorMath.FromRgb(0x00897B)),
		new("Forest", M3ColorMath.FromRgb(0x3E8E41)),
		new("Amber", M3ColorMath.FromRgb(0xC98A00)),
		new("Violet", M3ColorMath.FromRgb(0x7E57C2)),
	];

	protected override void DrawMain()
	{
		var value = Value;
		var count = Presets.Length + 1;
		var swatch = M3Widgets.ColorSwatchSize;
		var gap = M3Widgets.ColorChoicesWidth(2) - (swatch * 2f);
		var width = MathF.Min(M3Widgets.ColorChoicesWidth(Presets.Length, custom: true), M3SettingRow.MaxControlWidth(RowIcon));
		var perLine = Math.Max(1, (int)MathF.Floor((width + gap + 0.5f) / (swatch + gap)));
		var lines = (count + perLine - 1) / perLine;
		var controlSize = new Vector2(width, (lines * swatch) + ((lines - 1) * gap));

		var row = M3SettingRow.Begin(Name, SupportingText, controlSize, leadingIcon: RowIcon);

		ImGui.SetCursorScreenPos(row.ControlPosition);
		if (M3Widgets.ColorChoices($"##Config_{ID}{GetHashCode()}", ref value, Presets, custom: true, maxWidth: width))
		{
			Value = value;
		}

		RowInteractions(row);
		M3SettingRow.End(row);
	}
}
