using Windows.UI.Input;

namespace RotationSolver.RebornRotations.Tank;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.5")]
[SourceCode(Path = "main/RebornRotations/Tank/DRK_Reborn.cs")]

public sealed class DRK_Reborn : DarkKnightRotation
{
	#region Config Options
	[RotationConfig(CombatType.PvE, Name = "Use provoke in opening if tank stance is on")]
	public bool UseProvokeInOpening { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Keep at least 3000 MP")]
	public bool TheBlackestNight { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use The Blackest Night on lowest HP party member during AOE scenarios")]
	public bool BlackLantern { get; set; } = false;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "Target health threshold needed to use Blackest Night with above option", Parent = nameof(BlackLantern))]
	private float BlackLanternRatio { get; set; } = 0.5f;

	[RotationConfig(CombatType.PvE, Name = "Use Oblation on lowest HP party member during AOE scenarios")]
	public bool OblationLantern { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Use Oblation last stack of Oblation for party members", Parent = nameof(OblationLantern))]
	public bool OblationLanternStack { get; set; } = false;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "Target health threshold needed to use Oblation with above option", Parent = nameof(OblationLantern))]
	private float OblationLanternRatio { get; set; } = 0.5f;
	#endregion

	#region Countdown Logic
	// Countdown logic to prepare for combat.
	// Includes logic for using Provoke, tank stances, and burst medicines.
	protected override IAction? CountDownAction(float remainTime)
	{
		//Provoke when has Shield.
		if (UseProvokeInOpening && remainTime <= CountDownAhead)
		{
			if (HasTankStance)
			{
				if (ProvokePvE.CanUse(out _))
				{
					return ProvokePvE;
				}
			}
		}

		if (remainTime < 1f && UseBurstMedicine(out var act))
		{
			return act;
		}

		if (remainTime <= 3f && TheBlackestNightPvE.CanUse(out act))
		{
			return act;
		}

		if (remainTime <= 1f && UnmendPvE.CanUse(out act))
		{
			return act;
		}

		return base.CountDownAction(remainTime);
	}
	#endregion

	#region oGCD Logic
	// Decision-making for emergency abilities, focusing on Blood Weapon usage.
	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		if (CombatElapsedLessGCD(3) && (IsLastAction(false, UnmendPvE) || IsLastAction(false, HardSlashPvE)))
		{
			if (EdgeOfShadowPvE.CanUse(out act))
			{
				return true;
			}
		}

		return base.EmergencyAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.ShadowstridePvE)]
	protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
	{
		if (ShadowstridePvE.CanUse(out act))
		{
			return true;
		}
		return base.MoveForwardAbility(nextGCD, out act);
	}

	protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
	{

		return base.HealSingleAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.DarkMissionaryPvE, ActionID.ReprisalPvE)]
	protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
	{
		if (!InTwoMIsBurst && OblationLantern && TheBlackestNightPvE.CanUse(out act, targetOverride: TargetType.LowHP) && !TheBlackestNightPvE.Target.Target.HasStatus(false, StatusID.Transcendent) && TheBlackestNightPvE.Target.Target.GetHealthRatio() <= BlackLanternRatio)
		{
			return true;
		}

		if (!InTwoMIsBurst && OblationLantern && OblationPvE.CanUse(out act, usedUp: OblationLanternStack, targetOverride: TargetType.LowHP) && !OblationPvE.Target.Target.HasStatus(false, StatusID.Transcendent) && OblationPvE.Target.Target.GetHealthRatio() <= OblationLanternRatio)
		{
			return true;
		}

		if (!InTwoMIsBurst && DarkMissionaryPvE.CanUse(out act))
		{
			return true;
		}

		if (!InTwoMIsBurst && ReprisalPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (!InTwoMIsBurst && OblationPvE.CanUse(out act, skipStatusProvideCheck: false, targetOverride: TargetType.Self))
		{
			return true;
		}

		return base.DefenseAreaAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.OblationPvE, ActionID.TheBlackestNightPvE, ActionID.DarkMindPvE, ActionID.ShadowWallPvE, ActionID.ShadowedVigilPvE, ActionID.RampartPvE, ActionID.ReprisalPvE)]
	protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
	{
		//10
		if (OblationPvE.CanUse(out act, usedUp: true, skipStatusProvideCheck: false, targetOverride: TargetType.Self))
		{
			return true;
		}

		if (TheBlackestNightPvE.CanUse(out act, targetOverride: TargetType.Self))
		{
			return true;
		}
		//20
		if (DarkMindPvE.CanUse(out act))
		{
			return true;
		}

		//30
		if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && ShadowWallPvE.CanUse(out act))
		{
			return true;
		}

		if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && ShadowedVigilPvE.CanUse(out act))
		{
			return true;
		}

		//30-40
		if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && ShadowedVigilPvE.CanUse(out act) && ShadowedVigilPvE.EnoughLevel)
		{
			return true;
		}

		if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && ShadowWallPvE.CanUse(out act) && !ShadowedVigilPvE.EnoughLevel)
		{
			return true;
		}

		//20
		if (!ShadowWallPvE.EnoughLevel)
		{
			if (RampartPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (ShadowWallPvE.EnoughLevel && !ShadowedVigilPvE.EnoughLevel)
		{
			if (ShadowWallPvE.Cooldown.IsCoolingDown && ShadowWallPvE.Cooldown.ElapsedAfter(30) && RampartPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (ShadowedVigilPvE.EnoughLevel)
		{
			if (ShadowedVigilPvE.Cooldown.IsCoolingDown && ShadowedVigilPvE.Cooldown.ElapsedAfter(30) && RampartPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (ReprisalPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		return base.DefenseSingleAbility(nextGCD, out act);
	}

	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		if (IsBurst)
		{
			if (InCombat && (IsLastGCD(false, SouleaterPvE) || !CombatElapsedLessGCD(4)))
			{
				if (DeliriumPvE.CanUse(out act))
				{
					return true;
				}
			}

			if (!DeliriumPvE.EnoughLevel)
			{
				if (BloodWeaponPvE.CanUse(out act))
				{
					return true;
				}
			}
			if (InCombat && (IsLastGCD(false, HardSlashPvE) || !CombatElapsedLessGCD(3)))
			{
				if (LivingShadowPvE.CanUse(out act, skipAoeCheck: true))
				{
					return true;
				}
			}
		}

		if (CombatElapsedLessGCD(4))
		{
			return base.AttackAbility(nextGCD, out act);
		}

		if (CheckDarkSide)
		{
			if (FloodOfDarknessPvE.CanUse(out act))
			{
				return true;
			}

			if (EdgeOfDarknessPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (!IsMoving && SaltedEarthPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (ShadowbringerPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (NumberOfHostilesInRange >= 3 && AbyssalDrainPvE.CanUse(out act))
		{
			return true;
		}

		if (CarveAndSpitPvE.CanUse(out act))
		{
			return true;
		}

		if (InTwoMIsBurst)
		{
			if (ShadowbringerPvE.CanUse(out act, usedUp: true, skipAoeCheck: true))
			{
				return true;
			}
		}

		if (SaltAndDarknessPvE.CanUse(out act))
		{
			return true;
		}

		return base.AttackAbility(nextGCD, out act);
	}
	#endregion

	#region GCD Logic
	protected override bool GeneralGCD(out IAction? act)
	{
		if (CombatElapsedLessGCD(4) && !IsLastGCD(false, SouleaterPvE))
		{
			if (!HasDelirium && SouleaterPvE.CanUse(out act))
			{
				return true;
			}

			if (!HasDelirium && SyphonStrikePvE.CanUse(out act))
			{
				return true;
			}

			if (!HasDelirium && HardSlashPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (DisesteemPvE.CanUse(out act, skipComboCheck: true, skipAoeCheck: true))
		{
			return true;
		}

		//AOE Delirium
		if (ImpalementPvE.CanUse(out act))
		{
			return true;
		}

		if (QuietusPvE.CanUse(out act))
		{
			return true;
		}

		// Single Target Delirium
		if (TorcleaverPvE.CanUse(out act, skipComboCheck: true))
		{
			return true;
		}

		if (ComeuppancePvE.CanUse(out act, skipComboCheck: true))
		{
			return true;
		}

		if (ScarletDeliriumPvE.CanUse(out act, skipComboCheck: true))
		{
			return true;
		}

		if (BloodspillerPvE.CanUse(out act, skipComboCheck: true))
		{
			return true;
		}

		//AOE
		if (StalwartSoulPvE.CanUse(out act, skipAoeCheck: true) && NumberOfHostilesInRange > 0)
		{
			return true;
		}

		//Single Target
		if (!HasDelirium && SouleaterPvE.CanUse(out act))
		{
			return true;
		}

		if (!HasDelirium && SyphonStrikePvE.CanUse(out act))
		{
			return true;
		}

		if (UnleashPvE.CanUse(out act))
		{
			return true;
		}

		if (!HasDelirium && HardSlashPvE.CanUse(out act))
		{
			return true;
		}

		if (UnmendPvE.CanUse(out act))
		{
			return true;
		}

		return base.GeneralGCD(out act);
	}
	#endregion

	#region Extra Methods
	// Indicates whether the Dark Knight can heal using a single ability.
	public override bool CanHealSingleAbility => false;

	// Logic to determine when to use blood-based abilities.
	private bool UseBlood
	{
		get
		{
			// Conditions based on player statuses and ability cooldowns.
			if (!DeliriumPvE.EnoughLevel || !LivingShadowPvE.EnoughLevel)
			{
				return true;
			}

			if (StatusHelper.PlayerHasStatus(true, StatusID.Delirium_3836))
			{
				return true;
			}

			if ((StatusHelper.PlayerHasStatus(true, StatusID.Delirium_1972) || StatusHelper.PlayerHasStatus(true, StatusID.Delirium_3836)) && LivingShadowPvE.Cooldown.IsCoolingDown)
			{
				return true;
			}

			return (DeliriumPvE.Cooldown.WillHaveOneChargeGCD(1) && !LivingShadowPvE.Cooldown.WillHaveOneChargeGCD(3)) || (Blood >= 90 && !LivingShadowPvE.Cooldown.WillHaveOneChargeGCD(1));
		}
	}
	// Determines if currently in a burst phase based on cooldowns of key abilities.
	private bool InTwoMIsBurst => BloodWeaponPvE.Cooldown.IsCoolingDown && DeliriumPvE.Cooldown.IsCoolingDown && ((LivingShadowPvE.Cooldown.IsCoolingDown && !LivingShadowPvE.Cooldown.ElapsedAfter(15)) || !LivingShadowPvE.EnoughLevel);

	// Manages DarkSide ability based on several conditions.
	private bool CheckDarkSide
	{
		get
		{
			if (DarkSideEndAfterGCD(3))
			{
				return true;
			}

			if ((InTwoMIsBurst && HasDarkArts) || (HasDarkArts && StatusHelper.PlayerHasStatus(true, StatusID.BlackestNight)) || (HasDarkArts && DarkSideEndAfterGCD(3)))
			{
				return true;
			}

			if (InTwoMIsBurst && BloodWeaponPvE.Cooldown.IsCoolingDown && LivingShadowPvE.Cooldown.IsCoolingDown && SaltedEarthPvE.Cooldown.IsCoolingDown && ShadowbringerPvE.Cooldown.CurrentCharges == 0 && CarveAndSpitPvE.Cooldown.IsCoolingDown)
			{
				return true;
			}

			return (!TheBlackestNight || CurrentMp >= 6000) && CurrentMp >= 8500;
		}
	}
	#endregion
}