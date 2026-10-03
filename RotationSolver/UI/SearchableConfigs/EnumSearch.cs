using Dalamud.Game.ClientState.Keys;
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
	private float _maxDisplayNameWidth = -1f;
	private float _measuredFontSize;

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

		using var table = ImRaii.Table(PopupKey, 2, ImGuiTableFlags.BordersOuter);
		if (!table)
		{
			return;
		}

		DrawHotKeys("Reset to Default Value.", ResetToDefault, ImGuiHelper.BackspaceHint);

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
			DrawHotKeys($"Execute \"{command}\"", () => Svc.Commands.ProcessCommand(command), ["Alt"]);
			DrawHotKeys($"Copy \"{command}\"", () => CopyCommand(command), ["Ctrl"]);
		}
	}

	private static void DrawHotKeys(string name, Action action, string[] keys)
	{
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

	private void ReactEnumPopup(bool hovered)
	{
		if (!hovered)
		{
			return;
		}

		if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !ImGui.IsPopupOpen(PopupKey))
		{
			ImGui.OpenPopup(PopupKey);
		}

		if (Svc.KeyState[VirtualKey.BACK])
		{
			ResetToDefault();
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

		var fontSize = ImGui.GetFontSize();
		if (_maxDisplayNameWidth < 0f || fontSize != _measuredFontSize)
		{
			_measuredFontSize = fontSize;
			_maxDisplayNameWidth = 0f;
			foreach (var name in displayNames)
			{
				_maxDisplayNameWidth = MathF.Max(_maxDisplayNameWidth, ImGui.CalcTextSize(name).X);
			}
		}

		var comboWidth = MathF.Min(
			MathF.Max(_maxDisplayNameWidth + (48f * Scale), DRAG_WIDTH * Scale),
			320f * Scale);

		var row = M3SettingRow.Begin(Name, SupportingText, new Vector2(comboWidth, M3Widgets.ComboHeight),
			leadingIcon: RowIcon);

		var currentIndex = Math.Max(0, Array.IndexOf(enumKeys, Value));
		ImGui.SetCursorScreenPos(row.ControlPosition);
		if (M3Widgets.Combo($"##Config_{ID}{GetHashCode()}", ref currentIndex, displayNames, comboWidth)
			&& currentIndex >= 0 && currentIndex < enumKeys.Length)
		{
			Value = enumKeys[currentIndex];
		}

		RowTooltip(row, "Right-click for the matching chat commands.");
		ReactEnumPopup(row.Hovered);
		M3SettingRow.End(row);
	}
}
