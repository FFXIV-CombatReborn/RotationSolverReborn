using ECommons.DalamudServices;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using RotationSolver.Updaters;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule;

namespace RotationSolver.UI.TeachingMode;

internal static class HotbarDisabledColor
{
	private static readonly HashSet<uint> _disabledActionIds = [];
	private static readonly List<nint> _addons = [];

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

		for (var hotBarIndex = 0; hotBarIndex < HotbarAddonHelper.AddonCount; hotBarIndex++)
		{
			var intPtr = HotbarAddonHelper.GetHotbarAddon(hotBarIndex);
			if (intPtr == nint.Zero)
			{
				continue;
			}

			var actionBar = (AddonActionBarBase*)intPtr;
			if (!HotbarAddonHelper.IsVisible(&actionBar->AtkUnitBase))
			{
				continue;
			}

			if (!HotbarAddonHelper.TryGetRaptureHotbarIndex(intPtr, hotBarIndex, raptureModule, out var resolvedHotbarIndex))
			{
				continue;
			}

			ref var raptureHotbar = ref raptureModule->Hotbars[resolvedHotbarIndex];

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

				ref var hotbarSlot = ref raptureHotbar.Slots[slotIndex];
				if (hotbarSlot.ApparentSlotType != HotbarSlotType.Action
					|| (hotbarSlot.OriginalApparentSlotType != HotbarSlotType.Action && hotbarSlot.CommandType != HotbarSlotType.PvPCombo))
				{
					continue;
				}

				var adjusted = actionManager->GetAdjustedActionId((uint)slot.ActionId);
				var shouldRedden = adjusted != 0 && (_disabledActionIds.Contains(adjusted) || _disabledActionIds.Contains((uint)slot.ActionId));
				if (iconAddon->Component != null)
				{
					ApplyIconReddening((AtkComponentIcon*)iconAddon->Component, shouldRedden);
				}
			}
		}
	}

	internal static void Reset()
	{
		if (!HasTint)
		{
			return;
		}

		ResetAllHotbarIconColors();
		HasTint = false;
	}

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
			var alpha = Math.Clamp(tint.W, 0f, 1f);
			iconComponent->IconImage->Color.R = (byte)Math.Clamp((int)(float.Lerp(1f, Math.Clamp(tint.X, 0f, 1f), alpha) * 255f), 0, 255);
			iconComponent->IconImage->Color.G = (byte)Math.Clamp((int)(float.Lerp(1f, Math.Clamp(tint.Y, 0f, 1f), alpha) * 255f), 0, 255);
			iconComponent->IconImage->Color.B = (byte)Math.Clamp((int)(float.Lerp(1f, Math.Clamp(tint.Z, 0f, 1f), alpha) * 255f), 0, 255);
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
			if (actionBar == null)
			{
				continue;
			}

			foreach (var slot in actionBar->ActionBarSlotVector.AsSpan())
			{
				if (slot.Icon == null || slot.Icon->Component == null)
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
		Collect(RotationUpdater.CurrentRotationActions);
		Collect(RotationUpdater.CurrentDutyRotationActions);

		static void Collect(IAction[] actions)
		{
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
