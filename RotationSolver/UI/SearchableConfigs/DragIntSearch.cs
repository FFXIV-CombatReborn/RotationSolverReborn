using RebornMaterial;

namespace RotationSolver.UI.SearchableConfigs;

internal class DragIntSearch : Searchable
{
	private readonly Func<Configs, int>? _getter;
	private readonly Action<Configs, int>? _setter;

	public int Min { get; }
	public int Max { get; }

	public DragIntSearch(PropertyInfo property) : base(property)
	{
		_getter = CreateGetter<int>(property);
		_setter = CreateSetter<int>(property);
		var range = _property.GetCustomAttribute<RangeAttribute>();
		Min = range != null ? (int)range.MinValue : 0;
		Max = range != null ? (int)range.MaxValue : 1;
	}

	protected int Value
	{
		get => _getter != null ? _getter(Service.Config) : (int)_property.GetValue(Service.Config)!;
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
		var trackWidth = Scale * DRAG_WIDTH;
		var controlSize = new Vector2(trackWidth + M3Widgets.SliderValueGutter(Max.ToString()), M3Widgets.ButtonHeight);

		var row = M3SettingRow.Begin(Name, SupportingText, controlSize, leadingIcon: RowIcon);

		ImGui.SetCursorScreenPos(row.ControlPosition);
		if (M3Widgets.SliderInt(ControlId, ref value, Min, Max, value.ToString(), trackWidth))
		{
			Value = value;
		}

		RowInteractions(row);
		M3SettingRow.End(row);
	}
}
