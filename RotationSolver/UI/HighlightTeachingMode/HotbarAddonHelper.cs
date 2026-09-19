using ECommons.DalamudServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace RotationSolver.UI.HighlightTeachingMode;

/// <summary>
/// Shared lookups for the hotbar addons, in RaptureHotbarModule order: the ten standard bars, then the cross hotbars.
/// </summary>
internal static class HotbarAddonHelper
{
	private static readonly string[] _addonNames = GetAddonNames();

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
