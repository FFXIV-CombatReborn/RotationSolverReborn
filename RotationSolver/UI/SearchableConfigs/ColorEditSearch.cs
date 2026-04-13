namespace RotationSolver.UI.SearchableConfigs;

internal class ColorEditSearch(PropertyInfo property) : Searchable(property)
{
	protected Vector4 Value
	{
		get => (Vector4)_property.GetValue(Service.Config)!;
		set => _property.SetValue(Service.Config, value);
	}

	protected override void DrawMain()
	{
		Vector4 value = Value;
		ImGui.SetNextItemWidth(DRAG_WIDTH * 1.5f * Scale);

		// Cache the hash code to avoid multiple calls
		int hashCode = GetHashCode();

		// Draw the color edit control
		if (ImGui.ColorEdit4($"{Name}##Config_{ID}{hashCode}", ref value))
		{
			Value = value;
		}

		// Show tooltip if item is hovered
		if (ImGui.IsItemHovered())
		{
			ShowTooltip();
		}
	}
}