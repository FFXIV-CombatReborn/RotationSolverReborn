namespace RotationSolver.UI.SearchableConfigs;

internal class DragFloatRangeSearch : Searchable
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
			return !string.IsNullOrEmpty(baseDesc) ? $"{baseDesc}\n{Unit}" : Unit.ToString();
		}
	}

	protected Vector2 Value
	{
		get => (Vector2)_property.GetValue(Service.Config)!;
		set => _property.SetValue(Service.Config, value);
	}

	protected float MinValue
	{
		get => Value.X;
		set
		{
			Vector2 v = Value;
			v.X = value;
			Value = v;
		}
	}

	protected float MaxValue
	{
		get => Value.Y;
		set
		{
			Vector2 v = Value;
			v.Y = value;
			Value = v;
		}
	}

	public DragFloatRangeSearch(PropertyInfo property) : base(property)
	{
		RangeAttribute? range = _property.GetCustomAttribute<RangeAttribute>();
		Min = range?.MinValue ?? 0f;
		Max = range?.MaxValue ?? 1f;
		Speed = range?.Speed ?? 0.001f;
		Unit = range?.UnitType ?? ConfigUnitType.None;
	}

	protected override void DrawMain()
	{
		float minValue = MinValue;
		float maxValue = MaxValue;
		ImGui.SetNextItemWidth(Scale * DRAG_WIDTH);

		// Cache the hash code to avoid multiple calls
		int hashCode = GetHashCode();

		// Draw the drag float range control
		if (ImGui.DragFloatRange2(
			$"##Config_{ID}{hashCode}",
			ref minValue,
			ref maxValue,
			Speed,
			Min,
			Max,
			Unit == ConfigUnitType.Percent ? $"{minValue * 100:F1}{Unit.ToSymbol()}" : $"{minValue:F2}{Unit.ToSymbol()}",
			Unit == ConfigUnitType.Percent ? $"{maxValue * 100:F1}{Unit.ToSymbol()}" : $"{maxValue:F2}{Unit.ToSymbol()}"
		))
		{
			// Ensure MinValue is less than or equal to MaxValue
			MinValue = Math.Min(minValue, maxValue);
			MaxValue = Math.Max(minValue, maxValue);
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