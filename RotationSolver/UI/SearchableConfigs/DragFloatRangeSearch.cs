using RebornMaterial;

namespace RotationSolver.UI.SearchableConfigs;

internal class DragFloatRangeSearch : UnitSearchable
{
	public float Min { get; }
	public float Max { get; }

	public DragFloatRangeSearch(PropertyInfo property) : base(property)
	{
		var range = _property.GetCustomAttribute<RangeAttribute>();
		Min = range?.MinValue ?? 0f;
		Max = range?.MaxValue ?? 1f;
	}

	protected Vector2 Value
	{
		get => (Vector2)_property.GetValue(Service.Config)!;
		set => _property.SetValue(Service.Config, value);
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
		if (M3Widgets.RangeSlider($"##Config_{ID}{GetHashCode()}", ref low, ref high, Min * SliderScale, Max * SliderScale, trackWidth, SliderFormat))
		{
			Value = new Vector2(low / SliderScale, high / SliderScale);
		}

		RowInteractions(row);
		M3SettingRow.End(row);
	}
}
