using Lumina.Excel.Sheets;

namespace RotationSolver.Basic.Rotations;

/// <summary>
/// Represents a custom rotation with various item-related methods.
/// </summary>
public partial class CustomRotation
{
	internal static PhoenixDownItem[] PhoenixDowns { get; } = [new PhoenixDownItem(Service.GetSheet<Item>().GetRow(4570))];

	private static class Consumables
	{
		internal static readonly MedicineItem[] Medicines;
		internal static readonly MpPotionItem[] MpPotions;
		internal static readonly HpPotionItem[] HpPotions;

		static Consumables()
		{
			List<MedicineItem> medicines = [];
			List<MpPotionItem> mpPotions = [];
			List<HpPotionItem> hpPotions = [];
			foreach (var i in Service.GetSheet<Item>())
			{
				var isMedicineCategory = i.ItemSearchCategory.RowId == 43;
				if (isMedicineCategory && i.FilterGroup == 6)
				{
					var medicine = new MedicineItem(i);
					if (medicine.Type != MedicineType.None)
					{
						medicines.Add(medicine);
					}
				}
				else if (isMedicineCategory && i.FilterGroup == 9)
				{
					mpPotions.Add(new MpPotionItem(i));
				}

				if ((isMedicineCategory && i.FilterGroup == 8) || i.RowId is 22306 or 47102 or 20309)
				{
					hpPotions.Add(new HpPotionItem(i));
				}
			}

			medicines.Reverse();
			mpPotions.Reverse();
			hpPotions.Reverse();
			Medicines = [.. medicines];
			MpPotions = [.. mpPotions];
			HpPotions = [.. hpPotions];
		}
	}

	/// <summary>
	/// Uses a Phoenix Down.
	/// </summary>
	/// <param name="nextGCD">The next GCD action.</param>
	/// <param name="act">The action to be performed.</param>
	/// <returns>True if a Phoenix Down can be used; otherwise, false.</returns>
	public static bool UsePhoenixDown(IAction nextGCD, out IAction? act)
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
			foreach (var phoenixdown in PhoenixDowns)
			{
				if (phoenixdown.CanUse(out act, true))
				{
					if (phoenixdown.Use())
					{
						return true;
					}
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

	internal static MedicineItem[] Medicines => Consumables.Medicines;

	/// <summary>
	/// Uses the burst medicines.
	/// </summary>
	/// <param name="act">The action to be performed.</param>
	/// <param name="clippingCheck">Indicates whether to perform a clipping check.</param>
	/// <returns>True if a burst medicine was used; otherwise, false.</returns>
	public bool UseBurstMedicine(out IAction? act, bool clippingCheck = true)
	{
		act = null;

		var isHostileTargetDummy = HostileTarget?.IsDummy() ?? false;
		var isInHighEndDuty = DataCenter.Territory?.IsHighEndDuty ?? false;

		if (!isHostileTargetDummy && !isInHighEndDuty && DataCenter.CurrentTinctureUseType == TinctureUseType.InHighEndDuty)
		{
			return false;
		}

		if (DataCenter.CurrentTinctureUseType == TinctureUseType.Nowhere)
		{
			return false;
		}

		foreach (var medicine in Medicines)
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

	internal static MpPotionItem[] MpPotions => Consumables.MpPotions;

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

	internal static HpPotionItem[] HpPotions => Consumables.HpPotions;

	private static bool UseHpPotion(IAction nextGCD, out IAction? act)
	{
		HpPotionItem? best = null;
		foreach (var a in HpPotions)
		{
			if (a.ID != 47102 && a.ID != 22306 && a.ID != 20309 && a.CanUse(out _, true))
			{
				if (best == null || a.MaxHp >= best.MaxHp)
				{
					best = a;
				}
			}

			if ((DataCenter.IsInPilgrimsTraverse || DataCenter.IsInTheFinalVerse) && a.ID == 47102 && a.CanUse(out _, true) && !StatusHelper.PlayerHasStatus(false, StatusID.Rehabilitation_4191))
			{
				if (best == null || a.MaxHp >= best.MaxHp)
				{
					best = a;
				}
			}

			if (DataCenter.IsInEurekaFieldOp && a.ID == 22306 && !StatusHelper.PlayerHasStatus(false, StatusID.Rehabilitation_648) && a.CanUse(out _, true))
			{
				if (best == null || a.MaxHp >= best.MaxHp)
				{
					best = a;
				}
			}

			if (DataCenter.IsInPalaceOfTheDead && a.ID == 20309 && !StatusHelper.PlayerHasStatus(false, StatusID.Rehabilitation_648) && a.CanUse(out _, true))
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