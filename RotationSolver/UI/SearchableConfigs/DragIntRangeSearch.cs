namespace RotationSolver.UI.SearchableConfigs;

internal class DragIntRangeSearch : Searchable
{
	public int Min { get; }
	public int Max { get; }
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

	protected Vector2Int Value
	{
		get => (Vector2Int)_property.GetValue(Service.Config)!;
		set => _property.SetValue(Service.Config, value);
	}

	protected int MinValue
	{
		get => Value.X;
		set => Value = Value.WithX(value);
	}

	protected int MaxValue
	{
		get => Value.Y;
		set => Value = Value.WithY(value);
	}

	public DragIntRangeSearch(PropertyInfo property) : base(property)
	{
		// Retrieve the RangeAttribute from the property
		RangeAttribute? range = _property.GetCustomAttribute<RangeAttribute>();
		Min = (int?)range?.MinValue ?? 0;
		Max = (int?)range?.MaxValue ?? 1;
		Speed = range?.Speed ?? 0.001f;
		Unit = range?.UnitType ?? ConfigUnitType.None;
	}

	protected override void DrawMain()
	{
		int minValue = MinValue;
		int maxValue = MaxValue;

		// Set the width of the drag control
		ImGui.SetNextItemWidth(Scale * DRAG_WIDTH);

		// Cache the hash code to avoid multiple calls
		int hashCode = GetHashCode();

		// Draw the integer range drag control
		if (ImGui.DragIntRange2($"##Config_{ID}{hashCode}", ref minValue, ref maxValue, Speed, Min, Max))
		{
			MinValue = Math.Min(minValue, maxValue);
			MaxValue = Math.Max(minValue, maxValue);
		}

		// Show tooltip if item is hovered
		if (ImGui.IsItemHovered())
		{
			ShowTooltip();
		}

		// Draw job icon if IsJob is true
		if (IsJob)
		{
			DrawJobIcon();
		}

		ImGui.SameLine();
		ImGui.TextWrapped(Name);

		// Show tooltip if item is hovered
		if (ImGui.IsItemHovered())
		{
			ShowTooltip(false);
		}
	}
}