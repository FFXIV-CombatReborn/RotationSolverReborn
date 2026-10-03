using RotationSolver.UI.Material;

namespace RotationSolver.UI.SearchableConfigs;

internal class ColorEditSearch(PropertyInfo property) : Searchable(property)
{
	protected Vector4 Value
	{
		get => (Vector4)_property.GetValue(Service.Config)!;
		set => _property.SetValue(Service.Config, value);
	}

	protected override void DrawMain()
	{
		var value = Value;
		var controlSize = Vector2.One * 28f * Scale;

		var row = M3SettingRow.Begin(Name, SupportingText, controlSize, leadingIcon: RowIcon);

		ImGui.SetCursorScreenPos(row.ControlPosition);
		if (M3Widgets.ColorSwatch($"##Config_{ID}{GetHashCode()}", ref value))
		{
			Value = value;
		}

		RowInteractions(row);
		M3SettingRow.End(row);
	}
}
