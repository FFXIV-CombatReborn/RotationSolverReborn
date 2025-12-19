using ECommons.DalamudServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using RotationSolver.UI.HighlightTeachingMode.ElementSpecial;
using RotationSolver.Updaters;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule;

namespace RotationSolver.UI.HighlightTeachingMode;

/// <summary>
/// Draws a semi-transparent red tint over hotbar slots whose actions are disabled in RSR (IsEnabled == false).
/// Only applies to slots whose RaptureHotbarModule.HotbarSlotType == Action (byte 1).
/// </summary>
public sealed class HotbarDisabledColor : DrawingHighlightHotbarBase
{
    private readonly HashSet<uint> _disabledBaseActionIds = [];

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    private protected override unsafe IEnumerable<IDrawing2D> To2D()
    {
        return [];
    }

    protected override unsafe void UpdateOnFrame()
    {
        ApplyFrame();
    }

	public static unsafe void ApplyFrame()
	{
		if (!Service.Config.ReddenDisabledHotbarActions || !MajorUpdater.IsValid || DataCenter.CurrentRotation == null || !DataCenter.IsActivated())
		{
			ResetAllHotbarIconColors();
			return;
		}

		var disabledBase = CollectDisabledActionIds();

		int hotBarIndex = 0;
		foreach (nint intPtr in EnumerateHotbarAddons())
		{
			var actionBar = (AddonActionBarBase*)intPtr;
			if (actionBar == null || !IsVisible(actionBar->AtkUnitBase))
			{
				hotBarIndex++;
				continue;
			}

			bool isCrossBar = hotBarIndex > 9;
			int resolvedHotbarIndex = hotBarIndex;
			if (isCrossBar)
			{
				if (hotBarIndex == 10)
				{
					var actBar = (AddonActionCross*)intPtr;
					resolvedHotbarIndex = actBar != null ? actBar->RaptureHotbarId : hotBarIndex;
				}
				else
				{
					var actBar = (AddonActionDoubleCrossBase*)intPtr;
					resolvedHotbarIndex = actBar != null ? actBar->BarTarget : hotBarIndex;
				}
			}

			var framework = Framework.Instance();
			if (framework == null)
			{
				hotBarIndex++;
				continue;
			}
			var uiModule = framework->GetUIModule();
			if (uiModule == null)
			{
				hotBarIndex++;
				continue;
			}
			var raptureModule = uiModule->GetRaptureHotbarModule();
			if (raptureModule == null)
			{
				hotBarIndex++;
				continue;
			}
			if (resolvedHotbarIndex < 0 || resolvedHotbarIndex >= raptureModule->Hotbars.Length)
			{
				hotBarIndex++;
				continue;
			}
			Hotbar raptureHotbar = raptureModule->Hotbars[resolvedHotbarIndex];

			int slotIndex = 0;
			foreach (ActionBarSlot slot in actionBar->ActionBarSlotVector.AsSpan())
			{
				AtkComponentNode* iconAddon = slot.Icon;
				if ((nint)iconAddon == nint.Zero || !IsVisible(&iconAddon->AtkResNode))
				{
					slotIndex++;
					continue;
				}

				if ((uint)slotIndex >= raptureHotbar.Slots.Length)
				{
					slotIndex++;
					continue;
				}

				HotbarSlot hotbarSlot = raptureHotbar.Slots[slotIndex];

				if (hotbarSlot.ApparentSlotType != HotbarSlotType.Action || hotbarSlot.OriginalApparentSlotType != HotbarSlotType.Action)
				{
					slotIndex++;
					continue;
				}

				uint adjusted = 0;
				try
				{
					var actionManager = ActionManager.Instance();
					if (actionManager != null)
					{
						adjusted = actionManager->GetAdjustedActionId((uint)slot.ActionId);
					}
				}
				catch
				{
					adjusted = 0;
				}

				bool shouldRedden = adjusted != 0 && disabledBase.Contains((uint)slot.ActionId);
				if (slot.Icon != null && slot.Icon->Component != null)
				{
					ApplyIconReddening((AtkComponentIcon*)slot.Icon->Component, shouldRedden);
				}

				slotIndex++;
			}

			hotBarIndex++;
		}
	}

	private static unsafe void ApplyIconReddening(AtkComponentIcon* iconComponent, bool redden)
    {
        if (iconComponent == null) return;
        if (iconComponent->IconImage == null) return;

        if (redden)
        {
            var tint = Service.Config.HotbarDisabledTintColor;
            byte r = (byte)Math.Clamp((int)(tint.X * 255f), 0, 255);
            byte g = (byte)Math.Clamp((int)(tint.Y * 255f), 0, 255);
            byte b = (byte)Math.Clamp((int)(tint.Z * 255f), 0, 255);
            iconComponent->IconImage->Color.R = r;
            iconComponent->IconImage->Color.G = g;
            iconComponent->IconImage->Color.B = b;
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
        foreach (nint intPtr in EnumerateHotbarAddons())
        {
            var actionBar = (AddonActionBarBase*)intPtr;
            if (actionBar == null || !IsVisible(actionBar->AtkUnitBase))
                continue;

            foreach (ActionBarSlot slot in actionBar->ActionBarSlotVector.AsSpan())
            {
                AtkComponentIcon* iconComponent = (AtkComponentIcon*)slot.Icon->Component;
                if (iconComponent == null || iconComponent->IconImage == null) continue;
                iconComponent->IconImage->Color.R = 0xFF;
                iconComponent->IconImage->Color.G = 0xFF;
                iconComponent->IconImage->Color.B = 0xFF;
            }
        }
    }

    private static unsafe bool IsVisible(AtkUnitBase unit)
    {
        if (!unit.IsVisible)
            return false;
        return unit.VisibilityFlags != 1 && IsVisible(unit.RootNode);
    }

    private static unsafe bool IsVisible(AtkResNode* node)
    {
        while (node != null)
        {
            if (!node->IsVisible())
                return false;
            node = node->ParentNode;
        }
        return true;
    }

    private static IEnumerable<nint> EnumerateHotbarAddons()
    {
        foreach (var a in GetAddons<AddonActionBar>()) yield return a;
        foreach (var a in GetAddons<AddonActionBarX>()) yield return a;
        foreach (var a in GetAddons<AddonActionCross>()) yield return a;
        foreach (var a in GetAddons<AddonActionDoubleCrossBase>()) yield return a;
    }

    private static HashSet<uint> CollectDisabledActionIds()
    {
        HashSet<uint> baseIds = [];

        Collect(DataCenter.CurrentRotation?.AllActions);
        Collect(DataCenter.CurrentDutyRotation?.AllActions);

        void Collect(IEnumerable<IAction>? actions)
        {
            if (actions == null) return;
            foreach (var a in actions)
            {
                if (a is IBaseAction ba && !ba.IsEnabled)
                {
                    baseIds.Add(ba.ID);
                }
            }
        }

        return baseIds;
    }

    private static unsafe List<nint> GetAddons<T>() where T : struct
    {
        var attr = typeof(T).GetCustomAttribute<AddonAttribute>();
        if (attr is not AddonAttribute on)
            return [];

        List<nint> result = [];
        foreach (var str in on.AddonIdentifiers)
        {
            var ptr = Svc.GameGui.GetAddonByName(str, 1);
            if (ptr != nint.Zero)
                result.Add(ptr);
        }
        return result;
    }
}