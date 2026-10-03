using RotationSolver.UI.Material;

namespace RotationSolver.UI.SearchableConfigs;

internal class DragFloatSearch : UnitSearchable
{
	public float Min { get; }
	public float Max { get; }

	public DragFloatSearch(PropertyInfo property) : base(property)
	{
		var range = _property.GetCustomAttribute<RangeAttribute>();
		Min = range?.MinValue ?? 0f;
		Max = range?.MaxValue ?? 1f;
	}

	protected float Value
	{
		get => (float)_property.GetValue(Service.Config)!;
		set => _property.SetValue(Service.Config, value);
	}

	internal void DrawCompact(float width)
	{
		var value = Value;
		var shown = value * SliderScale;
		var trackWidth = MathF.Max(60f * Scale, width - M3Widgets.SliderValueGutter(Format(Max)));

		if (M3Widgets.Slider($"##Config_{ID}{GetHashCode()}_compact", ref shown, Min * SliderScale, Max * SliderScale, Format(value), trackWidth))
		{
			Value = shown / SliderScale;
		}

		if (ImGui.IsItemHovered() && !ImGui.IsItemActive())
		{
			ShowTooltip();
		}

		PreparePopup();
	}

	protected override void DrawMain()
	{
		var value = Value;
		var shown = value * SliderScale;
		var trackWidth = Scale * DRAG_WIDTH;
		var controlSize = new Vector2(trackWidth + M3Widgets.SliderValueGutter(Format(Max)), M3Widgets.ButtonHeight);

		var row = M3SettingRow.Begin(Name, SupportingText, controlSize, leadingIcon: RowIcon);

		ImGui.SetCursorScreenPos(row.ControlPosition);
		if (M3Widgets.Slider($"##Config_{ID}{GetHashCode()}", ref shown, Min * SliderScale, Max * SliderScale, Format(value), trackWidth))
		{
			Value = shown / SliderScale;
		}

		RowInteractions(row);
		M3SettingRow.End(row);
	}
}
