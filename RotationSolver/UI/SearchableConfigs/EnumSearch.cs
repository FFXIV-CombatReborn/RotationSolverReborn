using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using RotationSolver.Data;
using RotationSolver.UI.Material;

namespace RotationSolver.UI.SearchableConfigs;

internal class EnumSearch(PropertyInfo property) : Searchable(property)
{
	private int[]? _enumKeys;
	private string[]? _displayNames;

	protected int Value
	{
		get => Convert.ToInt32(_property.GetValue(Service.Config));
		set => _property.SetValue(Service.Config, Enum.ToObject(_property.PropertyType, value));
	}

	private void EnsureEnumCache()
	{
		if (_enumKeys != null)
		{
			return;
		}

		Dictionary<int, string> enumValueToNameMap = [];
		foreach (Enum enumValue in Enum.GetValues(_property.PropertyType))
		{
			enumValueToNameMap[Convert.ToInt32(enumValue)] = enumValue.GetDescription();
		}

		_enumKeys = [.. enumValueToNameMap.Keys];
		_displayNames = [.. enumValueToNameMap.Values];
	}

	protected override void PreparePopup()
	{
		using var popup = ImRaii.Popup(PopupKey);
		if (!popup.Success)
		{
			return;
		}

		using var table = ImRaii.Table(PopupKey, 1, ImGuiTableFlags.BordersOuter);
		if (!table)
		{
			return;
		}

		DrawPopupItem("Reset to Default Value.", ResetToDefault);

		var isFirst = true;
		foreach (Enum enumValue in Enum.GetValues(_property.PropertyType))
		{
			if (!isFirst)
			{
				ImGui.TableNextRow();
				ImGui.TableNextColumn();
				ImGui.Separator();
			}

			isFirst = false;

			var command = $"{Service.COMMAND} {OtherCommandType.Settings} {_property.Name} {enumValue}";
			DrawPopupItem($"Execute \"{command}\"", () => Svc.Commands.ProcessCommand(command));
			DrawPopupItem($"Copy \"{command}\"", () => CopyCommand(command));
		}
	}

	private static void DrawPopupItem(string name, Action action)
	{
		ImGui.TableNextRow();
		_ = ImGui.TableNextColumn();
		if (ImGui.Selectable(name))
		{
			action();
			ImGui.CloseCurrentPopup();
		}
	}

	private static void CopyCommand(string command)
	{
		ImGui.SetClipboardText(command);
		Notify.Success($"\"{command}\" copied to clipboard.");
	}

	protected override void DrawMain()
	{
		EnsureEnumCache();
		var enumKeys = _enumKeys!;
		var displayNames = _displayNames!;

		if (displayNames.Length == 0)
		{
			return;
		}

		var currentIndex = Math.Max(0, Array.IndexOf(enumKeys, Value));
		var comboWidth = MathF.Min(
			MathF.Max(M3Widgets.ComboWidthFor(displayNames[currentIndex]), DRAG_WIDTH * Scale),
			M3SettingRow.MaxControlWidth(RowIcon));

		var row = M3SettingRow.Begin(Name, SupportingText, new Vector2(comboWidth, M3Widgets.ComboHeight),
			leadingIcon: RowIcon);

		ImGui.SetCursorScreenPos(row.ControlPosition);
		if (M3Widgets.Combo($"##Config_{ID}{GetHashCode()}", ref currentIndex, displayNames, comboWidth)
			&& currentIndex >= 0 && currentIndex < enumKeys.Length)
		{
			Value = enumKeys[currentIndex];
		}

		RowTooltip(row, "Right-click for the matching chat commands.");
		ImGuiHelper.ReactPopupAt(row.Hovered, PopupKey, false);
		M3SettingRow.End(row);
	}
}
