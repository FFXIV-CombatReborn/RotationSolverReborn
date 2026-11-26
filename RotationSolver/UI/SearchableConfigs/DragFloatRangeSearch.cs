using RebornMaterial;

namespace RotationSolver.UI.SearchableConfigs;

internal class DragFloatRangeSearch : UnitSearchable
{
	private readonly Func<Configs, Vector2>? _getter;
	private readonly Action<Configs, Vector2>? _setter;

	public float Min { get; }
	public float Max { get; }

	public DragFloatRangeSearch(PropertyInfo property) : base(property)
	{
		_getter = CreateGetter<Vector2>(property);
		_setter = CreateSetter<Vector2>(property);
		var range = _property.GetCustomAttribute<RangeAttribute>();
		Min = range?.MinValue ?? 0f;
		Max = range?.MaxValue ?? 1f;
	}

	protected Vector2 Value
	{
		get => _getter != null ? _getter(Service.Config) : (Vector2)_property.GetValue(Service.Config)!;
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
		var low = bounds.X * SliderScale;
		var high = bounds.Y * SliderScale;
		var trackWidth = Scale * DRAG_WIDTH;
		var controlSize = new Vector2(trackWidth + M3Widgets.SliderValueGutter($"{Format(Max)} - {Format(Max)}"), M3Widgets.ButtonHeight);

		var row = M3SettingRow.Begin(Name, SupportingText, controlSize, leadingIcon: RowIcon);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		if (M3Widgets.RangeSlider(ControlId, ref low, ref high, Min * SliderScale, Max * SliderScale, trackWidth, SliderFormat))
		{
			Value = new Vector2(low / SliderScale, high / SliderScale);
		}

		RowInteractions(row);
		M3SettingRow.End(row);
	}
}
