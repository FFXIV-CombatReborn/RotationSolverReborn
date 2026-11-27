using RebornMaterial;

namespace RotationSolver.UI.SearchableConfigs;

internal class DragIntRangeSearch : UnitSearchable
{
	private readonly Func<Configs, Vector2Int>? _getter;
	private readonly Action<Configs, Vector2Int>? _setter;

	public int Min { get; }
	public int Max { get; }

	public DragIntRangeSearch(PropertyInfo property) : base(property)
	{
		_getter = CreateGetter<Vector2Int>(property);
		_setter = CreateSetter<Vector2Int>(property);
		var range = _property.GetCustomAttribute<RangeAttribute>();
		Min = (int?)range?.MinValue ?? 0;
		Max = (int?)range?.MaxValue ?? 1;
	}

	protected Vector2Int Value
	{
		get => _getter != null ? _getter(Service.Config) : (Vector2Int)_property.GetValue(Service.Config)!;
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
		var bounds = Value;
		var low = bounds.X;
		var high = bounds.Y;
		var trackWidth = Scale * DRAG_WIDTH;
		var controlSize = new Vector2(trackWidth + M3Widgets.SliderValueGutter($"{Max} - {Max}"), M3Widgets.ButtonHeight);

		var row = M3SettingRow.Begin(Name, SupportingText, controlSize, leadingIcon: RowIcon);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		if (M3Widgets.RangeSliderInt(ControlId, ref low, ref high, Min, Max, trackWidth))
		{
			Value = new Vector2Int(low, high);
		}

		RowInteractions(row);
		M3SettingRow.End(row);
	}
}
