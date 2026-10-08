using RebornMaterial;
using RotationSolver.Data;

namespace RotationSolver.UI.SearchableConfigs;

internal class DragIntRangeSearch : UnitSearchable
{
	public int Min { get; }
	public int Max { get; }

	public DragIntRangeSearch(PropertyInfo property) : base(property)
	{
		var range = _property.GetCustomAttribute<RangeAttribute>();
		Min = (int?)range?.MinValue ?? 0;
		Max = (int?)range?.MaxValue ?? 1;
	}

	protected Vector2Int Value
	{
		get => (Vector2Int)_property.GetValue(Service.Config)!;
		set => _property.SetValue(Service.Config, value);
	}

	protected override void DrawMain()
	{
		var bounds = Value;
		var minValue = bounds.X;
		var maxValue = bounds.Y;
		var trackWidth = Scale * DRAG_WIDTH;
		var controlSize = new Vector2(trackWidth + M3Widgets.SliderValueGutter(Max.ToString()), M3Widgets.ButtonHeight);

		var row = M3SettingRow.Begin(Name, SupportingText, Vector2.Zero, leadingIcon: RowIcon);
		RowInteractions(row);
		M3SettingRow.End(row);

		using var group = M3SubGroup.Begin();

		var lowRow = M3SettingRow.Begin(UiString.ConfigWindow_RangeLower.GetDescription(), null, controlSize);
		ImGui.SetCursorScreenPos(lowRow.ControlPosition);
		if (M3Widgets.SliderInt($"##Config_{ID}_low{GetHashCode()}", ref minValue, Min, Max, minValue.ToString(), trackWidth))
		{
			Value = bounds.WithX(Math.Min(minValue, maxValue));
		}

		RowInteractions(lowRow);
		M3SettingRow.End(lowRow);

		var highRow = M3SettingRow.Begin(UiString.ConfigWindow_RangeUpper.GetDescription(), null, controlSize);
		ImGui.SetCursorScreenPos(highRow.ControlPosition);
		if (M3Widgets.SliderInt($"##Config_{ID}_high{GetHashCode()}", ref maxValue, Min, Max, maxValue.ToString(), trackWidth))
		{
			Value = bounds.WithY(Math.Max(maxValue, minValue));
		}

		RowInteractions(highRow);
		M3SettingRow.End(highRow);
	}
}
