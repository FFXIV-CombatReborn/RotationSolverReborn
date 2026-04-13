namespace RotationSolver.UI.SearchableConfigs;

internal class DragFloatSearch : Searchable
{
	public float Min { get; }
	public float Max { get; }
	public float Speed { get; }
	public ConfigUnitType Unit { get; }

	public override string Description
	{
		get
		{
			string baseDesc = base.Description;
			return !string.IsNullOrEmpty(baseDesc) ? baseDesc + "\n" + Unit.ToString() : Unit.ToString();
		}
	}

	public DragFloatSearch(PropertyInfo property) : base(property)
	{
		RangeAttribute? range = _property.GetCustomAttribute<RangeAttribute>();
		if (range != null)
		{
			Min = range.MinValue;
			Max = range.MaxValue;
			Speed = range.Speed;
			Unit = range.UnitType;
		}
		else
		{
			Min = 0f;
			Max = 1f;
			Speed = 0.001f;
			Unit = ConfigUnitType.None;
		}
	}

	protected float Value
	{
		get => (float)_property.GetValue(Service.Config)!;
		set => _property.SetValue(Service.Config, value);
	}

	protected override void DrawMain()
	{
		float value = Value;
		ImGui.SetNextItemWidth(Scale * DRAG_WIDTH);

		// Cache the hash code to avoid multiple calls
		int hashCode = GetHashCode();

		// Draw slider or drag float based on unit type
		if (Unit == ConfigUnitType.Percent)
		{
			// Convert the value to percentage for display
			float displayValue = value * 100f;
			if (ImGui.SliderFloat($"##Config_{ID}{hashCode}", ref displayValue, Min * 100f, Max * 100f, $"{displayValue:F1}{Unit.ToSymbol()}"))
			{
				// Convert the display value back to the original scale
				Value = displayValue / 100f;
			}
		}
		else
		{
			if (ImGui.DragFloat($"##Config_{ID}{hashCode}", ref value, Speed, Min, Max, $"{value:F2}{Unit.ToSymbol()}"))
			{
				Value = value;
			}
		}

		// Show tooltip if item is hovered
		if (ImGui.IsItemHovered())
		{
			ShowTooltip();
		}

		// Draw job icon if applicable
		if (IsJob)
		{
			DrawJobIcon();
		}

		ImGui.SameLine();

		// Set text color if specified
		if (Color != 0)
		{
			ImGui.PushStyleColor(ImGuiCol.Text, Color);
		}

		ImGui.TextWrapped(Name);
		if (Color != 0)
		{
			ImGui.PopStyleColor();
		}

		// Show tooltip if item is hovered
		if (ImGui.IsItemHovered())
		{
			ShowTooltip(false);
		}
	}
}