using Lumina.Excel.Sheets;

namespace RotationSolver.Rotations;

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

				if (i.RowId is 22306 or 47102 or 20309)
				{
					hpPotions.Add(new HpPotionItem(i, true));
				}
				else if (isMedicineCategory && i.FilterGroup == 8)
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

		if (!DataCenter.InCombat && Service.CountDownTime <= 0)
		{
			return false;
		}

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
		act = null;
		if (DataCenter.IsPvP || !Service.Config.UseMpPotions)
		{
			return false;
		}

		if (!DataCenter.InCombat && !Service.Config.UseMpPotionsOutOfCombat)
		{
			return false;
		}

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

	private static bool AnyLivingHealerInParty()
	{
		foreach (var member in DataCenter.PartyMembers)
		{
			if (member.IsJobCategory(JobRole.Healer) && !member.IsDead)
			{
				return true;
			}
		}
		return false;
	}

	private static bool CanUseRegenHpPotion(HpPotionItem potion)
	{
		return potion.ID switch
		{
			47102 => (DataCenter.IsInPilgrimsTraverse || DataCenter.IsInTheFinalVerse) && !StatusHelper.PlayerHasStatus(false, StatusID.Rehabilitation_4191),
			22306 => DataCenter.IsInEurekaFieldOp && !StatusHelper.PlayerHasStatus(false, StatusID.Rehabilitation_648),
			20309 => DataCenter.IsInPalaceOfTheDead && !StatusHelper.PlayerHasStatus(false, StatusID.Rehabilitation_648),
			_ => false,
		};
	}

	private static bool UseHpPotion(IAction nextGCD, out IAction? act)
	{
		act = null;
		if (DataCenter.IsPvP || !Service.Config.UseHpPotions || DataCenter.IsTyrantCastingSpecialIndicator())
		{
			return false;
		}

		if (DataCenter.InCombat)
		{
			if (Service.Config.UseHpPotionsTimeToKill > 0f && !IsLongerThan(Service.Config.UseHpPotionsTimeToKill))
			{
				return false;
			}
		}
		else if (!Service.Config.UseHpPotionsOutOfCombat)
		{
			return false;
		}

		if (Service.Config.UseHpPotionsOnlyWhenNoHealer && DataCenter.Role != JobRole.Healer && AnyLivingHealerInParty())
		{
			return false;
		}

		HpPotionItem? bestRegular = null;
		foreach (var a in HpPotions)
		{
			if (a.IsRegenPotion)
			{
				if (CanUseRegenHpPotion(a) && a.CanUse(out _, true))
				{
					act = a;
					return true;
				}
				continue;
			}

			if (a.CanUse(out _, true) && (bestRegular == null || a.MaxHp >= bestRegular.MaxHp))
			{
				bestRegular = a;
			}
		}
		act = bestRegular;
		return act != null;
	}
	#endregion
}