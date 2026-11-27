using RebornMaterial;

namespace RotationSolver.UI.SearchableConfigs;

internal class ColorEditSearch(PropertyInfo property) : Searchable(property)
{
	private readonly Func<Configs, Vector4>? _getter = CreateGetter<Vector4>(property);
	private readonly Action<Configs, Vector4>? _setter = CreateSetter<Vector4>(property);

	protected Vector4 Value
	{
		get => _getter != null ? _getter(Service.Config) : (Vector4)_property.GetValue(Service.Config)!;
		set
		{
			if (_setter != null)
			{
				_setter(Service.Config, value);
			}
			else
			{
				_property.SetValue(Service.Config, value);
			}
		}
	}

	protected override void DrawMain()
	{
		var value = Value;
		var controlSize = Vector2.One * 28f * Scale;

		var row = M3SettingRow.Begin(Name, SupportingText, controlSize, leadingIcon: RowIcon);

		ImGui.SetCursorScreenPos(row.ControlPosition);
		if (M3Widgets.ColorSwatch(ControlId, ref value))
		{
			Value = value;
		}

		RowInteractions(row);
		M3SettingRow.End(row);
	}
}
