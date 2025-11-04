using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using static Dalamud.Interface.Utility.Raii.ImRaii;

namespace RotationSolver.Basic.Rotations;

/// <summary>
/// Represents a custom rotation with various item-related methods.
/// </summary>
public partial class CustomRotation
{
    private static readonly BaseItem PhoenixDownItemNumber = new(4570);

    internal static PhoenixDownItem[] PhoenixDowns { get; } = GetPhoenixDownItem();

    private static PhoenixDownItem[] GetPhoenixDownItem()
    {
        var items = Service.GetSheet<Item>();
        var list = new List<PhoenixDownItem>();
        foreach (var i in items)
        {
            if (i.RowId == PhoenixDownItemNumber.ID)
            {
                list.Add(new PhoenixDownItem(i));
            }
        }
        return [.. list];
    }

    /// <summary>
    /// Uses a Phoenix Down.
    /// </summary>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The action to be performed.</param>
    /// <returns>True if a Phoenix Down can be used; otherwise, false.</returns>
    public unsafe static bool UsePhoenixDown(IAction nextGCD, out IAction? act)
    {
        act = null;

        var target = DataCenter.DeathTarget;
        if (target == null || !ObjectHelper.CanBeRaised(target))
        {
            return false;
        }

        var prevOverride = IBaseAction.TargetOverride;
        IBaseAction.TargetOverride = TargetType.Death;
        try
        {
            foreach (PhoenixDownItem phoenixdown in PhoenixDowns)
            {
                // Ensure we propagate the action outward if needed by upstream code
                if (phoenixdown.CanUse(out act, true))
                {
                    // Use() handles HQ/NQ and correct target GameObjectId
                    if (phoenixdown.Use())
                    {
                        return true;
                    }
                    // If use failed, clear and continue scanning
                    act = null;
                }
            }
            return false;
        }
        finally
        {
            IBaseAction.TargetOverride = prevOverride;
        }
    }

    #region Burst Medicine

    /// <summary>
    /// Gets the type of medicine.
    /// </summary>
    public abstract MedicineType MedicineType { get; }

    /// <summary>
    /// Gets the collection of available medicines.
    /// </summary>
    internal static MedicineItem[] Medicines { get; } = GetMedicines();

    private static MedicineItem[] GetMedicines()
    {
        var items = Service.GetSheet<Item>();
        var list = new List<MedicineItem>();
        foreach (var i in items)
        {
            if (i.FilterGroup == 6 && i.ItemSearchCategory.RowId == 43)
            {
                var med = new MedicineItem(i);
                if (med.Type != MedicineType.None)
                {
                    list.Add(med);
                }
            }
        }
        // Reverse the list
        int n = list.Count;
        var arr = new MedicineItem[n];
        for (int i = 0; i < n; i++)
        {
            arr[i] = list[n - i - 1];
        }
        return arr;
    }

    /// <summary>
    /// Uses the burst medicines.
    /// </summary>
    /// <param name="act">The action to be performed.</param>
    /// <param name="clippingCheck">Indicates whether to perform a clipping check.</param>
    /// <returns>True if a burst medicine was used; otherwise, false.</returns>
    public bool UseBurstMedicine(out IAction? act, bool clippingCheck = true)
    {
        act = null;

        bool isHostileTargetDummy = HostileTarget?.IsDummy() ?? false;
        bool isInHighEndDuty = DataCenter.Territory?.IsHighEndDuty ?? false;

        if (!isHostileTargetDummy && !isInHighEndDuty && DataCenter.CurrentTinctureUseType == TinctureUseType.InHighEndDuty)
        {
            return false;
        }

        if (DataCenter.CurrentTinctureUseType == TinctureUseType.Nowhere)
        {
            return false;
        }

        foreach (MedicineItem medicine in Medicines)
        {
            if (medicine.Type != MedicineType)
            {
                continue;
            }

            if (medicine.CanUse(out act, clippingCheck))
            {
                return true;
            }
        }

        return false;
    }
    #endregion

    #region MP Potions

    /// <summary>
    /// Gets the collection of available MP potions.
    /// </summary>
    internal static MpPotionItem[] MpPotions { get; } = GetMpPotions();

    private static MpPotionItem[] GetMpPotions()
    {
        var items = Service.GetSheet<Item>();
        var list = new List<MpPotionItem>();
        foreach (var i in items)
        {
            if (i.FilterGroup == 9 && i.ItemSearchCategory.RowId == 43)
            {
                list.Add(new MpPotionItem(i));
            }
        }
        // Reverse the list
        int n = list.Count;
        var arr = new MpPotionItem[n];
        for (int i = 0; i < n; i++)
        {
            arr[i] = list[n - i - 1];
        }
        return arr;
    }

    /// <summary>
    /// Uses an MP potion.
    /// </summary>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The action to be performed.</param>
    /// <returns>True if an MP potion was used; otherwise, false.</returns>
    private static bool UseMpPotion(IAction nextGCD, out IAction? act)
    {
        MpPotionItem? best = null;
        foreach (var a in MpPotions)
        {
            if (a.CanUse(out _, true))
            {
                if (best == null || a.MaxMp >= best.MaxMp)
                {
                    best = a;
                }
            }
        }
        act = best;
        return act != null;
    }
    #endregion

    #region HP Potions

    /// <summary>
    /// Gets the collection of available HP potions.
    /// </summary>
    internal static HpPotionItem[] HpPotions { get; } = GetHpPotions();

    private static HpPotionItem[] GetHpPotions()
    {
        var items = Service.GetSheet<Item>();
        var list = new List<HpPotionItem>();
        foreach (var i in items)
        {
            if ((i.FilterGroup == 8 && i.ItemSearchCategory.RowId == 43) || i.RowId == 22306 || i.RowId == 47102 || i.RowId == 20309)
            {
                list.Add(new HpPotionItem(i));
            }
        }
        // Reverse the list
        int n = list.Count;
        var arr = new HpPotionItem[n];
        for (int i = 0; i < n; i++)
        {
            arr[i] = list[n - i - 1];
        }
        return arr;
    }

    /// <summary>
    /// Uses an HP potion.
    /// </summary>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The action to be performed.</param>
    /// <returns>True if an HP potion was used; otherwise, false.</returns>
    private static bool UseHpPotion(IAction nextGCD, out IAction? act)
    {
        HpPotionItem? best = null;
        foreach (var a in HpPotions)
        {
            bool isDeepDungeons = DataCenter.Territory?.ContentType == TerritoryContentType.DeepDungeons;
            if (a.CanUse(out _, true) && (a.ID != 47102 || (a.ID == 47102 && isDeepDungeons) || (a.ID == 20309 && isDeepDungeons && !Player.HasStatus(false, StatusID.Rehabilitation_648))))
            {
                if (best == null || a.MaxHp >= best.MaxHp)
                {
                    best = a;
                }
            }
        }
        act = best;
        return act != null;
    }
    #endregion
}