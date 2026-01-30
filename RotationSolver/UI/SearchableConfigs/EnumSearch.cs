using Dalamud.Interface.Utility;
using RotationSolver.Data;

namespace RotationSolver.UI.SearchableConfigs;

internal class EnumSearch(PropertyInfo property) : Searchable(property)
{
	protected int Value
	{
		get => Convert.ToInt32(_property.GetValue(Service.Config));
		set => _property.SetValue(Service.Config, Enum.ToObject(_property.PropertyType, value));
	}

	protected override void DrawMain()
	{
		int currentValue = Value;

		// Create a map of enum values to their descriptions
		Dictionary<int, string> enumValueToNameMap = [];
		foreach (Enum enumValue in Enum.GetValues(_property.PropertyType))
		{
			enumValueToNameMap[Convert.ToInt32(enumValue)] = enumValue.GetDescription();
		}

		string[] displayNames;
		{
			displayNames = new string[enumValueToNameMap.Count];
			int idx = 0;
			foreach (var kv in enumValueToNameMap)
			{
				displayNames[idx++] = kv.Value;
			}
		}

		string name = Name;
		bool drawLabelAbove = false;

		if (displayNames.Length > 0)
		{
			// Set the width of the combo box
			float maxText = 0f;
			for (int i = 0; i < displayNames.Length; i++)
			{
				float w = ImGui.CalcTextSize(displayNames[i]).X;
				if (w > maxText) maxText = w;
			}

			float comboWidth = Math.Max(maxText + 30, DRAG_WIDTH) * Scale;

			if (!string.IsNullOrEmpty(name))
			{
				float availableWidth = ImGui.GetContentRegionAvail().X;
				float spacing = ImGui.GetStyle().ItemSpacing.X;
				float iconWidth = IsJob ? (24 * ImGuiHelpers.GlobalScale + spacing) : 0f;
				float labelWidth = ImGui.CalcTextSize(name).X;

				drawLabelAbove = comboWidth + spacing + iconWidth + labelWidth > availableWidth;
				if (drawLabelAbove)
				{
					ImGui.TextWrapped(name);
					if (ImGui.IsItemHovered())
					{
						ShowTooltip(false);
					}
				}
			}

			ImGui.SetNextItemWidth(comboWidth);

			// Find the current index of the selected value
			int currentIndex = 0;
			int tmpIdx = 0;
			bool found = false;
			foreach (var kv in enumValueToNameMap)
			{
				if (kv.Key == currentValue)
				{
					currentIndex = tmpIdx;
					found = true;
					break;
				}
				tmpIdx++;
			}
			if (!found)
			{
				currentIndex = 0; // Default to first item if not found
			}

			// Cache the hash code to avoid multiple calls
			int hashCode = GetHashCode();

			// Draw the combo box
			if (ImGui.Combo($"##Config_{ID}{hashCode}", ref currentIndex, displayNames, displayNames.Length))
			{
				int i = 0;
				int selectedKey = currentValue;
				foreach (var kv in enumValueToNameMap)
				{
					if (i == currentIndex)
					{
						selectedKey = kv.Key;
						break;
					}
					i++;
				}
				Value = selectedKey;
			}
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

		if (!drawLabelAbove && !string.IsNullOrEmpty(name))
		{
			ImGui.SameLine();
			ImGui.TextWrapped(name);

			// Show tooltip if item is hovered
			if (ImGui.IsItemHovered())
			{
				ShowTooltip(false);
			}
		}
	}
}