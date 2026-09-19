using ECommons.DalamudServices;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using RotationSolver.Updaters;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule;

namespace RotationSolver.UI.HighlightTeachingMode;

/// <summary>
/// Tints hotbar slots whose actions are disabled in RSR (IsEnabled == false).
/// Only applies to slots whose RaptureHotbarModule.HotbarSlotType == Action (byte 1).
/// </summary>
internal static class HotbarDisabledColor
{
	// Reused every frame to avoid allocating a new set per update. Framework thread only.
	private static readonly HashSet<uint> _disabledActionIds = [];
	private static readonly List<nint> _addons = [];

	/// <summary>
	/// True while hotbar icon colors have been modified and still need restoring.
	/// </summary>
	internal static bool HasTint { get; private set; }

	public static unsafe void ApplyFrame()
	{
		if (!Service.Config.ReddenDisabledHotbarActions || !MajorUpdater.IsValid || DataCenter.CurrentRotation == null || !DataCenter.IsActivated())
		{
			Reset();
			return;
		}

		var framework = Framework.Instance();
		var uiModule = framework == null ? null : framework->GetUIModule();
		var raptureModule = uiModule == null ? null : uiModule->GetRaptureHotbarModule();
		var actionManager = ActionManager.Instance();
		if (raptureModule == null || actionManager == null)
		{
			return;
		}

		CollectDisabledActionIds();
		HasTint = true;

		HotbarAddonHelper.GetHotbarAddons(_addons);

		var hotBarIndex = 0;
		foreach (var intPtr in _addons)
		{
			var actionBar = (AddonActionBarBase*)intPtr;
			if (!HotbarAddonHelper.IsVisible(&actionBar->AtkUnitBase))
			{
				hotBarIndex++;
				continue;
			}

			var resolvedHotbarIndex = hotBarIndex;
			if (hotBarIndex > 9)
			{
				resolvedHotbarIndex = hotBarIndex == 10
					? ((AddonActionCross*)intPtr)->RaptureHotbarId
					: ((AddonActionDoubleCrossBase*)intPtr)->BarTarget;
			}

			if (resolvedHotbarIndex < 0 || resolvedHotbarIndex >= raptureModule->Hotbars.Length)
			{
				hotBarIndex++;
				continue;
			}
			var raptureHotbar = raptureModule->Hotbars[resolvedHotbarIndex];

			var slotIndex = -1;
			foreach (var slot in actionBar->ActionBarSlotVector.AsSpan())
			{
				slotIndex++;

				var iconAddon = slot.Icon;
				if (iconAddon == null || !HotbarAddonHelper.IsVisible(&iconAddon->AtkResNode))
				{
					continue;
				}

				if ((uint)slotIndex >= raptureHotbar.Slots.Length)
				{
					continue;
				}

				var hotbarSlot = raptureHotbar.Slots[slotIndex];
				if (hotbarSlot.ApparentSlotType != HotbarSlotType.Action || hotbarSlot.OriginalApparentSlotType != HotbarSlotType.Action)
				{
					continue;
				}

				var adjusted = actionManager->GetAdjustedActionId((uint)slot.ActionId);
				var shouldRedden = adjusted != 0 && _disabledActionIds.Contains((uint)slot.ActionId);
				if (iconAddon->Component != null)
				{
					ApplyIconReddening((AtkComponentIcon*)iconAddon->Component, shouldRedden);
				}
			}

			hotBarIndex++;
		}
	}

	/// <summary>
	/// Restores every hotbar icon to its untinted color if a tint was applied.
	/// </summary>
	internal static void Reset()
	{
		if (!HasTint)
		{
			return;
		}

		ResetAllHotbarIconColors();
		HasTint = false;
	}

	/// <summary>
	/// Restores icon colors when the plugin unloads, marshalling to the framework thread if needed.
	/// </summary>
	internal static void ResetOnUnload()
	{
		if (!HasTint)
		{
			return;
		}

		try
		{
			if (Svc.Framework.IsInFrameworkUpdateThread)
			{
				Reset();
			}
			else
			{
				// Bounded wait so unloading can never hang on the framework thread.
				_ = Svc.Framework.RunOnFrameworkThread(Reset).Wait(TimeSpan.FromSeconds(1));
			}
		}
		catch (Exception ex)
		{
			PluginLog.Warning($"Failed to reset hotbar icon colors: {ex.Message}");
		}
	}

	private static unsafe void ApplyIconReddening(AtkComponentIcon* iconComponent, bool redden)
	{
		if (iconComponent == null || iconComponent->IconImage == null)
		{
			return;
		}

		if (redden)
		{
			var tint = Service.Config.HotbarDisabledTintColor;
			iconComponent->IconImage->Color.R = (byte)Math.Clamp((int)(tint.X * 255f), 0, 255);
			iconComponent->IconImage->Color.G = (byte)Math.Clamp((int)(tint.Y * 255f), 0, 255);
			iconComponent->IconImage->Color.B = (byte)Math.Clamp((int)(tint.Z * 255f), 0, 255);
		}
		else
		{
			iconComponent->IconImage->Color.R = 0xFF;
			iconComponent->IconImage->Color.G = 0xFF;
			iconComponent->IconImage->Color.B = 0xFF;
		}
	}

	private static unsafe void ResetAllHotbarIconColors()
	{
		HotbarAddonHelper.GetHotbarAddons(_addons);

		foreach (var intPtr in _addons)
		{
			var actionBar = (AddonActionBarBase*)intPtr;
			if (!HotbarAddonHelper.IsVisible(&actionBar->AtkUnitBase))
			{
				continue;
			}

			foreach (var slot in actionBar->ActionBarSlotVector.AsSpan())
			{
				if (slot.Icon == null)
				{
					continue;
				}

				ApplyIconReddening((AtkComponentIcon*)slot.Icon->Component, false);
			}
		}
	}

	private static void CollectDisabledActionIds()
	{
		_disabledActionIds.Clear();
		Collect(DataCenter.CurrentRotation?.AllActions);
		Collect(DataCenter.CurrentDutyRotation?.AllActions);

		static void Collect(IAction[]? actions)
		{
			if (actions == null)
			{
				return;
			}

			foreach (var a in actions)
			{
				if (a is IBaseAction ba && !ba.IsEnabled)
				{
					_ = _disabledActionIds.Add(ba.ID);
				}
			}
		}
	}
}
