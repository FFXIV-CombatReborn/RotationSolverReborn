using ECommons.DalamudServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule;

namespace RotationSolver.UI.HighlightTeachingMode;

/// <summary>
/// Shared lookups for the hotbar addons, in RaptureHotbarModule order: the ten standard bars, then the cross hotbars.
/// </summary>
internal static class HotbarAddonHelper
{
	internal const int StandardHotbarCount = 10;

	private static readonly string[] _addonNames = GetAddonNames();

	private static Dictionary<uint, uint>? _generalActionLookup;

	private static string[] GetAddonNames()
	{
		List<string> names = [];
		AddNames<AddonActionBar>(names);
		AddNames<AddonActionBarX>(names);
		AddNames<AddonActionCross>(names);
		AddNames<AddonActionDoubleCrossBase>(names);
		return [.. names];

		static void AddNames<T>(List<string> list) where T : struct
		{
			var attr = typeof(T).GetCustomAttribute<AddonAttribute>();
			if (attr != null)
			{
				list.AddRange(attr.AddonIdentifiers);
			}
		}
	}

	/// <summary>
	/// Fills <paramref name="addons"/> with the currently loaded hotbar addons.
	/// Each caller passes its own buffer: a shared one would be cleared by one thread while another
	/// iterates it, since hotbar work runs from both framework updates and UI drawing.
	/// </summary>
	internal static void GetHotbarAddons(List<nint> addons)
	{
		addons.Clear();
		foreach (var name in _addonNames)
		{
			nint ptr = Svc.GameGui.GetAddonByName(name, 1);
			if (ptr != nint.Zero)
			{
				addons.Add(ptr);
			}
		}
	}

	internal static int AddonCount => _addonNames.Length;

	internal static nint GetHotbarAddon(int index)
	{
		return Svc.GameGui.GetAddonByName(_addonNames[index], 1);
	}

	internal static unsafe bool TryGetRaptureHotbarIndex(nint addon, int addonIndex, RaptureHotbarModule* raptureModule, out int raptureIndex)
	{
		raptureIndex = addonIndex < StandardHotbarCount
			? addonIndex
			: addonIndex == StandardHotbarCount
				? ((AddonActionCross*)addon)->RaptureHotbarId
				: ((AddonActionDoubleCrossBase*)addon)->BarTarget;

		return raptureIndex >= 0 && raptureIndex < raptureModule->Hotbars.Length;
	}

	internal static bool IsSlotMatch(uint adjustedActionId, in HotbarSlot hot, HotbarID hotbarId)
	{
		return hot.OriginalApparentSlotType == hotbarId.SlotType
			&& hot.ApparentSlotType == hotbarId.SlotType
			&& adjustedActionId == hotbarId.Id;
	}

	internal static HotbarID? GetHotbarID(IAction? action)
	{
		if (action is IBaseItem item)
		{
			return new HotbarID(HotbarSlotType.Item, item.ID);
		}

		if (action is IBaseAction baseAction)
		{
			return baseAction.Action.ActionCategory.RowId is 10 or 11
				? GetGeneralActionHotbarID(baseAction)
				: new HotbarID(HotbarSlotType.Action, baseAction.AdjustedID);
		}

		return null;
	}

	private static HotbarID? GetGeneralActionHotbarID(IBaseAction baseAction)
	{
		if (_generalActionLookup == null)
		{
			var sheet = Svc.Data.GetExcelSheet<GeneralAction>();
			if (sheet == null)
			{
				return null;
			}

			_generalActionLookup = [];
			foreach (var gAct in sheet)
			{
				var actionRowId = gAct.Action.RowId;
				if (actionRowId != 0)
				{
					_generalActionLookup.TryAdd(actionRowId, gAct.RowId);
				}
			}
		}

		return _generalActionLookup.TryGetValue(baseAction.ID, out var generalActionRowId)
			? new HotbarID(HotbarSlotType.GeneralAction, generalActionRowId)
			: null;
	}

	internal static unsafe bool IsVisible(AtkUnitBase* unit)
	{
		return unit != null && unit->IsVisible && unit->VisibilityFlags != 1 && IsVisible(unit->RootNode);
	}

	internal static unsafe bool IsVisible(AtkResNode* node)
	{
		while (node != null)
		{
			if (!node->IsVisible())
			{
				return false;
			}

			node = node->ParentNode;
		}

		return true;
	}
}
