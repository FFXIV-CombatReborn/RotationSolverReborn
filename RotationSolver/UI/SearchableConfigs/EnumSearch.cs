using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using RotationSolver.Basic.Data;
using RotationSolver.Data;

namespace RotationSolver.UI.SearchableConfigs;

internal class EnumSearch(PropertyInfo property) : Searchable(property)
{
	protected int Value
	{
		get => Convert.ToInt32(_property.GetValue(Service.Config));
		set => _property.SetValue(Service.Config, Enum.ToObject(_property.PropertyType, value));
	}

	private string Popup_Key => $"Rotation Solver RightClicking Enum##{ID}_{GetHashCode()}";

	public override unsafe void Draw()
	{
		// Determine the appropriate filter based on the context (PvP or PvE)
		var filter = DataCenter.IsPvP ? PvPFilter : PvEFilter;

		// Check if the filter allows drawing
		if (!filter.CanDraw)
		{
			// If no jobs are available in the filter, return early
			if (filter.AllJobs.Length == 0)
			{
				return;
			}

			// Get the text color for disabled text
			var textColor = *ImGui.GetStyleColorVec4(ImGuiCol.Text);

			// Push the disabled text color style
			ImGui.PushStyleColor(ImGuiCol.Text, *ImGui.GetStyleColorVec4(ImGuiCol.TextDisabled));

			// Calculate the cursor position
			var cursor = ImGui.GetCursorPos() + ImGui.GetWindowPos() - new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY());

			// Ensure Name is not null before using it
			if (!string.IsNullOrEmpty(Name))
			{
				ImGui.TextWrapped(Name);
			}

			// Pop the disabled text color style
			ImGui.PopStyleColor();

			// Calculate the text size and item rectangle size
			var step = ImGui.CalcTextSize(Name ?? string.Empty);
			var size = ImGui.GetItemRectSize();
			var height = step.Y / 2;
			var wholeWidth = step.X;

			// Draw lines to indicate disabled state
			while (height < size.Y)
			{
				var pt = cursor + new Vector2(0, height);
				ImGui.GetWindowDrawList().AddLine(pt, pt + new Vector2(Math.Min(wholeWidth, size.X), 0), ImGui.ColorConvertFloat4ToU32(textColor));
				height += step.Y;
				wholeWidth -= size.X;
			}

			// Show a tooltip with the filter description
			ImguiTooltips.HoveredTooltip(filter.Description);
			return;
		}

		// Draw the main content
		DrawMain();

		// Prepare the group for the popup menu with all enum values
		PrepareEnumPopup();
	}

	private void PrepareEnumPopup()
	{
		using var popup = ImRaii.Popup(Popup_Key);
		if (popup.Success)
		{
			if (ImGui.BeginTable(Popup_Key, 2, ImGuiTableFlags.BordersOuter))
			{
				// Add reset option first
				DrawHotKeys("Reset to Default Value.", ResetToDefault, ImGuiHelper.stringArray);

				var enumValues = Enum.GetValues(_property.PropertyType);
				var isFirst = true;

				foreach (Enum enumValue in enumValues)
				{
					// Add separator before each enum value pair (except the first)
					if (!isFirst)
					{
						ImGui.TableNextRow();
						ImGui.TableNextColumn();
						ImGui.Separator();
					}
					isFirst = false;

					var enumName = enumValue.ToString();
					var command = $"{Service.COMMAND} {OtherCommandType.Settings} {_property.Name} {enumName}";

					// Add Execute option
					DrawHotKeys($"Execute \"{command}\"", () => ExecuteEnumCommand(command), ["Alt"]);
					// Add Copy option
					DrawHotKeys($"Copy \"{command}\"", () => CopyCommand(command), ["Ctrl"]);
				}

				ImGui.EndTable();
			}
		}
	}

	private static void DrawHotKeys(string name, Action action, string[] keys)
	{
		if (action == null)
		{
			return;
		}

		ArgumentNullException.ThrowIfNull(keys);

		ImGui.TableNextRow();
		_ = ImGui.TableNextColumn();
		if (ImGui.Selectable(name))
		{
			action();
			ImGui.CloseCurrentPopup();
		}

		_ = ImGui.TableNextColumn();
		ImGui.TextDisabled(string.Join(' ', keys));
	}

	protected new void ShowTooltip(bool showHand = true)
	{
		var showDesc = !string.IsNullOrEmpty(Description);
		if (showDesc)
		{
			ImguiTooltips.ShowTooltip(() =>
			{
				if (showDesc)
				{
					ImGui.BulletText(Description);
				}
				if (showDesc)
				{
					ImGui.Separator();
				}
			});
		}

		ReactEnumPopup(showHand);
	}

	private void ReactEnumPopup(bool showHand = true)
	{
		if (!ImGui.IsItemHovered())
		{
			return;
		}

		if (showHand)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
		{
			if (!ImGui.IsPopupOpen(Popup_Key))
			{
				ImGui.OpenPopup(Popup_Key);
			}
		}

		// Handle hotkey for reset
		if (Svc.KeyState[VirtualKey.BACK])
		{
			ResetToDefault();
		}
	}

	private static void ExecuteEnumCommand(string command)
	{
		_ = Svc.Commands.ProcessCommand(command);
	}

	private static void CopyCommand(string command)
	{
		ImGui.SetClipboardText(command);
		Notify.Success($"\"{command}\" copied to clipboard.");
	}

	protected override void DrawMain()
	{
		var currentValue = Value;

		// Create a map of enum values to their descriptions
		Dictionary<int, string> enumValueToNameMap = [];
		foreach (Enum enumValue in Enum.GetValues(_property.PropertyType))
		{
			enumValueToNameMap[Convert.ToInt32(enumValue)] = enumValue.GetDescription();
		}

		string[] displayNames;
		{
			displayNames = new string[enumValueToNameMap.Count];
			var idx = 0;
			foreach (var kv in enumValueToNameMap)
			{
				displayNames[idx++] = kv.Value;
			}
		}

		var name = Name;
		var drawLabelAbove = false;

		if (displayNames.Length > 0)
		{
			// Set the width of the combo box
			var maxText = 0f;
			for (var i = 0; i < displayNames.Length; i++)
			{
				var w = ImGui.CalcTextSize(displayNames[i]).X;
				if (w > maxText)
				{
					maxText = w;
				}
			}

			var comboWidth = Math.Max(maxText + 30, DRAG_WIDTH) * Scale;

			if (!string.IsNullOrEmpty(name))
			{
				var availableWidth = ImGui.GetContentRegionAvail().X;
				var spacing = ImGui.GetStyle().ItemSpacing.X;
				var iconWidth = IsJob ? (24 * ImGuiHelpers.GlobalScale + spacing) : 0f;
				var labelWidth = ImGui.CalcTextSize(name).X;

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
			var currentIndex = 0;
			var tmpIdx = 0;
			var found = false;
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
			var hashCode = GetHashCode();

			// Draw the combo box
			if (ImGui.Combo($"##Config_{ID}{hashCode}", ref currentIndex, displayNames, displayNames.Length))
			{
				var i = 0;
				var selectedKey = currentValue;
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