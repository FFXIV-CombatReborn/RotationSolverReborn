namespace RotationSolver.UI.SearchableConfigs;

internal class DragIntSearch : Searchable
{
	public int Min { get; }
	public int Max { get; }
	public float Speed { get; }

	protected int Value
	{
		get
		{
			// Retrieve the current value of the property
			object? value = _property.GetValue(Service.Config);

			// Ensure the value is not null before casting to int
			return value == null ? throw new InvalidOperationException("The property value cannot be null.") : (int)value;
		}

		set =>
			// Set the property value
			_property.SetValue(Service.Config, value);
	}

	public DragIntSearch(PropertyInfo property) : base(property)
	{
		// Retrieve the RangeAttribute from the property
		RangeAttribute? range = _property.GetCustomAttribute<RangeAttribute>();

		// Set the minimum value, defaulting to 0 if the attribute is not present
		Min = range != null ? (int)range.MinValue : 0;

		// Set the maximum value, defaulting to 1 if the attribute is not present
		Max = range != null ? (int)range.MaxValue : 1;

		// Set the speed, defaulting to 0.001f if the attribute is not present
		Speed = range?.Speed ?? 0.001f;
	}

	protected override void DrawMain()
	{
		// Retrieve the current value of the property
		int value = Value;

		// Set the width for the next item
		ImGui.SetNextItemWidth(Scale * DRAG_WIDTH);

		// Cache the hash code to avoid multiple calls to GetHashCode
		int hashCode = GetHashCode();

		// Draw the drag integer control
		if (ImGui.DragInt($"##Config_{ID}{hashCode}", ref value, Speed, Min, Max))
		{
			// Update the property value if it has changed
			Value = value;
		}

		// Show tooltip if the item is hovered
		if (ImGui.IsItemHovered())
		{
			ShowTooltip();
		}

		// Draw job icon if applicable
		if (IsJob)
		{
			DrawJobIcon();
		}

		// Draw the name of the property
		ImGui.SameLine();
		ImGui.TextWrapped(Name ?? string.Empty);

		// Show tooltip if the item is hovered
		if (ImGui.IsItemHovered())
		{
			ShowTooltip(false);
		}
	}

}
