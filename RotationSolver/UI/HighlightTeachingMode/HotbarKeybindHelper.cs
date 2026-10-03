using Dalamud.Game.ClientState.Keys;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using RotationSolver.Updaters;
using System.Diagnostics;
using System.Globalization;

namespace RotationSolver.UI.HighlightTeachingMode;

internal static class HotbarKeybindHelper
{
	private sealed record KeybindEntry(IAction Action, string Text);

	// Keybind names call slots 10-12 "0", "A" and "B".
	private static readonly string[] _slotNames = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "A", "B"];
	private static readonly InputId?[,] _slotInputIds = BuildSlotInputIds();

	private const int RefreshIntervalMs = 250;

	private static readonly Dictionary<IAction, string> _cache = new(ReferenceEqualityComparer.Instance);
	private static readonly Stopwatch _refresh = Stopwatch.StartNew();

	private static KeybindEntry? _current;

	internal static string GetKeybind(IAction? action)
	{
		var entry = _current;
		return entry != null && action != null && ReferenceEquals(entry.Action, action) ? entry.Text : string.Empty;
	}

	internal static void Update()
	{
		var action = ActionUpdater.NextAction;
		if (!Service.Config.ShowNextActionWindow || !Service.Config.ShowNextActionKeybind || action == null)
		{
			_cache.Clear();
			_current = null;
			return;
		}

		if (_refresh.ElapsedMilliseconds >= RefreshIntervalMs)
		{
			_cache.Clear();
			_refresh.Restart();
		}

		if (!_cache.TryGetValue(action, out var text))
		{
			text = FindKeybind(action);
			_cache[action] = text;
		}

		var current = _current;
		if (current == null || !ReferenceEquals(current.Action, action) || current.Text != text)
		{
			_current = new KeybindEntry(action, text);
		}
	}

	private static unsafe string FindKeybind(IAction action)
	{
		var hotbarId = HotbarAddonHelper.GetHotbarID(action);
		if (!hotbarId.HasValue)
		{
			return string.Empty;
		}

		var framework = Framework.Instance();
		var uiModule = framework == null ? null : framework->GetUIModule();
		var raptureModule = uiModule == null ? null : uiModule->GetRaptureHotbarModule();
		var actionManager = ActionManager.Instance();
		if (raptureModule == null || actionManager == null)
		{
			return string.Empty;
		}

		for (var addonIndex = 0; addonIndex < HotbarAddonHelper.AddonCount; addonIndex++)
		{
			var addon = HotbarAddonHelper.GetHotbarAddon(addonIndex);
			var actionBar = (AddonActionBarBase*)addon;
			if (actionBar == null || !HotbarAddonHelper.IsVisible(&actionBar->AtkUnitBase)
				|| !HotbarAddonHelper.TryGetRaptureHotbarIndex(addon, addonIndex, raptureModule, out var raptureIndex))
			{
				continue;
			}

			ref var hotBar = ref raptureModule->Hotbars[raptureIndex];

			var slotIndex = -1;
			foreach (var slot in actionBar->ActionBarSlotVector.AsSpan())
			{
				slotIndex++;

				var iconAddon = slot.Icon;
				if (iconAddon == null || (uint)slotIndex >= hotBar.Slots.Length
					|| !HotbarAddonHelper.IsVisible(&iconAddon->AtkResNode))
				{
					continue;
				}

				ref var hotSlot = ref hotBar.Slots[slotIndex];
				var actionId = actionManager->GetAdjustedActionId((uint)slot.ActionId);
				if (!HotbarAddonHelper.IsSlotMatch(actionId, hotSlot, hotbarId.Value))
				{
					continue;
				}

				var text = CleanHint(hotSlot.PopUpKeybindHintString);
				if (text.Length == 0 && addonIndex < HotbarAddonHelper.StandardHotbarCount)
				{
					text = GetBoundKeyText(addonIndex, slotIndex);
				}

				if (text.Length > 0)
				{
					return text;
				}
			}
		}

		return string.Empty;
	}

	// Turns " [Ctrl-3]" into "Ctrl-3". Empty for controller buttons ImGui can't draw.
	private static string CleanHint(string hint)
	{
		var text = hint.Trim().TrimStart('[').TrimEnd(']').Trim();
		foreach (var c in text)
		{
			if (char.IsControl(c) || char.GetUnicodeCategory(c) == UnicodeCategory.PrivateUse)
			{
				return string.Empty;
			}
		}

		return text;
	}

	private static unsafe string GetBoundKeyText(int barIndex, int slotIndex)
	{
		if ((uint)slotIndex >= _slotNames.Length || _slotInputIds[barIndex, slotIndex] is not { } inputId)
		{
			return string.Empty;
		}

		var inputData = UIInputData.Instance();
		var keybind = inputData == null ? null : inputData->GetKeybind(inputId);
		if (keybind == null)
		{
			return string.Empty;
		}

		foreach (var setting in keybind->KeySettings)
		{
			if (setting.Key != SeVirtualKey.NO_KEY)
			{
				return FormatKeySetting(setting);
			}
		}

		return string.Empty;
	}

	private static string FormatKeySetting(KeySetting setting)
	{
		var modifier = setting.KeyModifier;
		return string.Concat(
			modifier.HasFlag(KeyModifierFlag.Shift) ? "Shift-" : string.Empty,
			modifier.HasFlag(KeyModifierFlag.Ctrl) ? "Ctrl-" : string.Empty,
			modifier.HasFlag(KeyModifierFlag.Alt) ? "Alt-" : string.Empty,
			GetKeyName(setting.Key));
	}

	private static string GetKeyName(SeVirtualKey key)
	{
		// Below 128, and for keys the game leaves unnamed, these are Windows virtual-key codes.
		var code = (byte)key;
		if (code < 128 || !Enum.IsDefined(key))
		{
			var virtualKey = (VirtualKey)code;
			if (Enum.IsDefined(virtualKey))
			{
				var name = virtualKey.GetFancyName();
				if (!string.IsNullOrEmpty(name))
				{
					return name;
				}
			}
		}

		return key.ToString();
	}

	private static InputId?[,] BuildSlotInputIds()
	{
		var ids = new InputId?[HotbarAddonHelper.StandardHotbarCount, _slotNames.Length];
		for (var bar = 0; bar < HotbarAddonHelper.StandardHotbarCount; bar++)
		{
			for (var slot = 0; slot < _slotNames.Length; slot++)
			{
				ids[bar, slot] = Enum.TryParse<InputId>($"HOTBAR_{bar + 1}_{_slotNames[slot]}", out var id) ? id : null;
			}
		}

		return ids;
	}
}
