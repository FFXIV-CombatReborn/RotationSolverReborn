using RebornMaterial;

namespace RotationSolver.UI.SearchableConfigs;

internal class DragFloatSearch : UnitSearchable
{
	private readonly Func<Configs, float>? _getter;
	private readonly Action<Configs, float>? _setter;
	private string? _compactId;

	public float Min { get; }
	public float Max { get; }

	public DragFloatSearch(PropertyInfo property) : base(property)
	{
		_getter = CreateGetter<float>(property);
		_setter = CreateSetter<float>(property);
		var range = _property.GetCustomAttribute<RangeAttribute>();
		Min = range?.MinValue ?? 0f;
		Max = range?.MaxValue ?? 1f;
	}

	protected float Value
	{
		get => _getter != null ? _getter(Service.Config) : (float)_property.GetValue(Service.Config)!;
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

	private float FromSlider(float shown)
	{
		var value = shown / SliderScale;
		return Step > 0f ? MathF.Round(value, 4) : value;
	}

	internal void DrawCompact(float width)
	{
		var value = Value;
		var shown = value * SliderScale;
		var trackWidth = MathF.Max(60f * Scale, width - M3Widgets.SliderValueGutter(Format(Max)));

		if (M3Widgets.Slider(_compactId ??= $"##Config_{ID}{GetHashCode()}_compact", ref shown, Min * SliderScale, Max * SliderScale, Format(value), trackWidth, step: Step * SliderScale))
		{
			Value = FromSlider(shown);
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
		if (M3Widgets.Slider(ControlId, ref shown, Min * SliderScale, Max * SliderScale, Format(value), trackWidth, step: Step * SliderScale))
		{
			Value = FromSlider(shown);
		}

		RowInteractions(row);
		M3SettingRow.End(row);
	}
}
