using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using RotationSolver.Data;

namespace RotationSolver.UI.SearchableConfigs;

internal class EnumSearch(PropertyInfo property) : Searchable(property)
{
	protected int Value
	{
		get => Convert.ToInt32(_property.GetValue(Service.Config));
		set => _property.SetValue(Service.Config, Enum.ToObject(_property.PropertyType, value));
	}

	private string Popup_Key => _popupKey ??= $"Rotation Solver RightClicking Enum##{ID}_{GetHashCode()}";
	private string? _popupKey;

	// Enum values and descriptions never change, so build the combo contents once instead of every frame.
	private int[]? _enumKeys;
	private string[]? _displayNames;
	private float _maxDisplayNameWidth = -1f;
	private float _measuredFontSize;

	private void EnsureEnumCache()
	{
		if (_enumKeys != null)
		{
			return;
		}

		// Keyed by value so aliases collapse to one entry, matching the previous per-frame behavior.
		Dictionary<int, string> enumValueToNameMap = [];
		foreach (Enum enumValue in Enum.GetValues(_property.PropertyType))
		{
			enumValueToNameMap[Convert.ToInt32(enumValue)] = enumValue.GetDescription();
		}

		_enumKeys = [.. enumValueToNameMap.Keys];
		_displayNames = [.. enumValueToNameMap.Values];
	}

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

		EnsureEnumCache();
		var enumKeys = _enumKeys!;
		var displayNames = _displayNames!;

		var name = Name;
		var drawLabelAbove = false;

		if (displayNames.Length > 0)
		{
			// Set the width of the combo box (text widths only change with the font, so re-measure only then)
			var fontSize = ImGui.GetFontSize();
			if (_maxDisplayNameWidth < 0f || fontSize != _measuredFontSize)
			{
				_measuredFontSize = fontSize;
				var maxText = 0f;
				for (var i = 0; i < displayNames.Length; i++)
				{
					var w = ImGui.CalcTextSize(displayNames[i]).X;
					if (w > maxText)
					{
						maxText = w;
					}
				}
				_maxDisplayNameWidth = maxText;
			}

			var comboWidth = Math.Max(_maxDisplayNameWidth + 30, DRAG_WIDTH) * Scale;

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

			// Find the current index of the selected value, defaulting to the first item if not found
			var currentIndex = Math.Max(0, Array.IndexOf(enumKeys, currentValue));

			// Cache the hash code to avoid multiple calls
			var hashCode = GetHashCode();

			// Draw the combo box
			if (ImGui.Combo($"##Config_{ID}{hashCode}", ref currentIndex, displayNames, displayNames.Length)
				&& currentIndex >= 0 && currentIndex < enumKeys.Length)
			{
				Value = enumKeys[currentIndex];
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