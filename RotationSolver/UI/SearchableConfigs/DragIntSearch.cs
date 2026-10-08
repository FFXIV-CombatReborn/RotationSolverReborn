using RebornMaterial;

namespace RotationSolver.UI.SearchableConfigs;

internal class DragIntSearch : Searchable
{
	public int Min { get; }
	public int Max { get; }

	public DragIntSearch(PropertyInfo property) : base(property)
	{
		var range = _property.GetCustomAttribute<RangeAttribute>();
		Min = range != null ? (int)range.MinValue : 0;
		Max = range != null ? (int)range.MaxValue : 1;
	}

	protected int Value
	{
		get => (int)_property.GetValue(Service.Config)!;
		set => _property.SetValue(Service.Config, value);
	}

	protected override void DrawMain()
	{
		var value = Value;
		var trackWidth = Scale * DRAG_WIDTH;
		var controlSize = new Vector2(trackWidth + M3Widgets.SliderValueGutter(Max.ToString()), M3Widgets.ButtonHeight);

		var row = M3SettingRow.Begin(Name, SupportingText, controlSize, leadingIcon: RowIcon);

		ImGui.SetCursorScreenPos(row.ControlPosition);
		if (M3Widgets.SliderInt($"##Config_{ID}{GetHashCode()}", ref value, Min, Max, value.ToString(), trackWidth))
		{
			Value = value;
		}

		RowInteractions(row);
		M3SettingRow.End(row);
	}
}
