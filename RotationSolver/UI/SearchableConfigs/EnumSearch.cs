using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using RebornMaterial;

namespace RotationSolver.UI.SearchableConfigs;

internal class EnumSearch(PropertyInfo property) : Searchable(property)
{
	private int[]? _enumKeys;
	private string[]? _displayNames;
	private (Func<Configs, int> Get, Action<Configs, int> Set)? _enumAccessors;

	private static (Func<Configs, int>, Action<Configs, int>) CreateEnumAccessors(PropertyInfo property)
	{
		if (typeof(Configs).IsAssignableFrom(property.DeclaringType) && property.PropertyType.IsEnum)
		{
			var method = typeof(EnumSearch).GetMethod(nameof(CreateEnumAccessorsCore), BindingFlags.Static | BindingFlags.NonPublic)!
				.MakeGenericMethod(property.PropertyType);
			return ((Func<Configs, int>, Action<Configs, int>))method.Invoke(null, [property])!;
		}

		return (
			cfg => Convert.ToInt32(property.GetValue(cfg)),
			(cfg, val) => property.SetValue(cfg, Enum.ToObject(property.PropertyType, val)));
	}

	private static (Func<Configs, int>, Action<Configs, int>) CreateEnumAccessorsCore<TEnum>(PropertyInfo property)
		where TEnum : struct, Enum
	{
		var getter = property.GetMethod?.CreateDelegate<Func<Configs, TEnum>>();
		var setter = property.SetMethod?.CreateDelegate<Action<Configs, TEnum>>();
		return (
			getter != null ? cfg => Convert.ToInt32(getter(cfg)) : cfg => Convert.ToInt32(property.GetValue(cfg)),
			setter != null ? (cfg, val) => setter(cfg, (TEnum)Enum.ToObject(typeof(TEnum), val)) : (cfg, val) => property.SetValue(cfg, Enum.ToObject(typeof(TEnum), val)));
	}

	protected int Value
	{
		get
		{
			_enumAccessors ??= CreateEnumAccessors(_property);
			return _enumAccessors.Value.Get(Service.Config);
		}
		set
		{
			_enumAccessors ??= CreateEnumAccessors(_property);
			_enumAccessors.Value.Set(Service.Config, value);
		}
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
		if (M3Widgets.Combo(ControlId, ref currentIndex, displayNames, comboWidth)
			&& currentIndex >= 0 && currentIndex < enumKeys.Length)
		{
			Value = enumKeys[currentIndex];
		}

		RowTooltip(row, "Right-click for the matching chat commands.");
		ImGuiHelper.ReactPopupAt(row.Hovered, PopupKey, false);
		M3SettingRow.End(row);
	}
}
