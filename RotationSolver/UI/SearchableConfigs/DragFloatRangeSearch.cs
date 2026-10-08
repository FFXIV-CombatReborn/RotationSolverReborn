using RebornMaterial;
using RotationSolver.Data;

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
		var trackWidth = Scale * DRAG_WIDTH;
		var controlSize = new Vector2(trackWidth + M3Widgets.SliderValueGutter(Format(Max)), M3Widgets.ButtonHeight);

		var row = M3SettingRow.Begin(Name, SupportingText, Vector2.Zero, leadingIcon: RowIcon);
		RowInteractions(row);
		M3SettingRow.End(row);

		using var group = M3SubGroup.Begin();

		var low = bounds.X * SliderScale;
		var lowRow = M3SettingRow.Begin(UiString.ConfigWindow_RangeLower.GetDescription(), null, controlSize);
		ImGui.SetCursorScreenPos(lowRow.ControlPosition);
		if (M3Widgets.Slider($"##Config_{ID}_low{GetHashCode()}", ref low, Min * SliderScale, Max * SliderScale, Format(bounds.X), trackWidth))
		{
			Value = new Vector2(MathF.Min(low / SliderScale, bounds.Y), bounds.Y);
		}

		RowInteractions(lowRow);
		M3SettingRow.End(lowRow);

		var high = bounds.Y * SliderScale;
		var highRow = M3SettingRow.Begin(UiString.ConfigWindow_RangeUpper.GetDescription(), null, controlSize);
		ImGui.SetCursorScreenPos(highRow.ControlPosition);
		if (M3Widgets.Slider($"##Config_{ID}_high{GetHashCode()}", ref high, Min * SliderScale, Max * SliderScale, Format(bounds.Y), trackWidth))
		{
			Value = new Vector2(bounds.X, MathF.Max(high / SliderScale, bounds.X));
		}

		RowInteractions(highRow);
		M3SettingRow.End(highRow);
	}
}
