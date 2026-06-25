using System.ComponentModel;

namespace RotationSolver.ExtraRotations.Tank;

[Rotation("FredderslyGNB", CombatType.PvE, GameVersion = "7.5")]
[SourceCode(Path = "main/ExtraRotations/Tank/FredderslyGNB.cs")]
[ExtraRotation]

public sealed class FredderslyGNB : GunbreakerRotation
{
	private const float FillerGnashingHoldWindow = 7f;

	private static bool InSolidBarrelCombo => LiveComboTime > 0f && IsLastComboAction(ActionID.KeenEdgePvE, ActionID.BrutalShellPvE);

	private bool ShouldStartGnashingFang()
	{
		if (AmmoComboStep != 0)
		{
			return false;
		}

		if (HasNoMercy)
		{
			return true;
		}

		// Do not start the filler Gnashing Fang while the normal Solid Barrel
		// combo is active; that combo timer can expire after the burst phase.
		if (InSolidBarrelCombo)
		{
			return false;
		}

		if (GnashingFangPvE.Cooldown.CurrentCharges >= 2)
		{
			return true;
		}

		if (!Use250GnashingOptimization)
		{
			return true;
		}

		// Start the filler Gnashing Fang at the guide's hold window so an
		// extra Gnashing GCD or continuation can land inside No Mercy.
		return NoMercyPvE.Cooldown.WillHaveOneCharge(FillerGnashingHoldWindow);
	}

	private bool ShouldStallForGnashingFang()
	{
		return Use250GnashingOptimization
			&& AmmoComboStep == 0
			&& Ammo > 1
			&& GnashingFangPvE.Cooldown.HasOneCharge
			&& !HasNoMercy
			&& NoMercyPvE.Cooldown.WillHaveOneCharge(13)
			&& !NoMercyPvE.Cooldown.WillHaveOneCharge(FillerGnashingHoldWindow)
			&& !InSolidBarrelCombo;
	}

	private bool ShouldHoldEyeGougeForNoMercy()
	{
		return ReignOfBeastsPvE.EnoughLevel
			&& HasBloodfest
			&& HasReadyToGouge
			&& !HasNoMercy
			&& NoMercyPvE.CanUse(out _);
	}

	private bool ShouldUseLevel100NoMercy(IAction nextGCD)
	{
		if (!HasBloodfest)
		{
			return false;
		}

		// The 2.50 opener delays No Mercy until after Brutal Shell so the
		// window begins on Gnashing Fang instead of the first combo GCD.
		if (CombatElapsedLessGCD(3) && IsLastComboAction(ActionID.BrutalShellPvE))
		{
			return true;
		}

		// Top logs often ramp with Gnashing Fang first, then weave No Mercy
		// before Eye Gouge and the Sonic/Double Down/Reign burst packet.
		if (HasReadyToGouge)
		{
			return true;
		}

		return nextGCD.IsTheSameTo(false,
			(ActionID)SonicBreakPvE.ID,
			(ActionID)DoubleDownPvE.ID,
			(ActionID)ReignOfBeastsPvE.ID);
	}

	#region Config Options
	[RotationConfig(CombatType.PvE, Name = "Use 2.50 Gnashing Fang Optimization")]
	public bool Use250GnashingOptimization { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "How to use Aurora")]
	public AuroraUsageStrategy AuroraUsage { get; set; } = AuroraUsageStrategy.TankbusterTarget;

	[RotationConfig(CombatType.PvE, Name = "How to use Heart Of Stone/Heart Of Corundum")]
	public HeartOfStoneStrategy HeartOfStoneUsage { get; set; } = HeartOfStoneStrategy.TankbusterTarget;

	public enum HeartOfStoneStrategy : byte
	{
		[Description("Full target usage")]
		Fullusage,

		[Description("Only use on tankbuster targets prioritizing self")]
		TankbusterTarget,

		[Description("Only use on self")]
		SelfOnly,
	}

	public enum AuroraUsageStrategy : byte
	{
		[Description("Full target usage")]
		Fullusage,

		[Description("Only use on tankbuster targets prioritizing self")]
		TankbusterTarget,

		[Description("Only use on self")]
		SelfOnly,
	}

	#endregion

	#region Countdown Logic
	protected override IAction? CountDownAction(float remainTime)
	{
		if (remainTime <= 0.7 && LightningShotPvE.CanUse(out var act))
		{
			return act;
		}

		if (remainTime <= 1.2 && UseBurstMedicine(out act))
		{
			return act;
		}

		return base.CountDownAction(remainTime);
	}
	#endregion

	#region oGCD Logic
	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		if (JugularRipPvE.CanUse(out act))
		{
			return true;
		}

		if (AbdomenTearPvE.CanUse(out act))
		{
			return true;
		}

		if (!ShouldHoldEyeGougeForNoMercy() && EyeGougePvE.CanUse(out act))
		{
			return true;
		}

		if (HypervelocityPvE.CanUse(out act))
		{
			return true;
		}

		if (FatedBrandPvE.CanUse(out act))
		{
			return true;
		}

		return base.EmergencyAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.HeartOfLightPvE, ActionID.ReprisalPvE)]
	protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
	{
		if (nextGCD.IsTheSameTo(false, (ActionID)GnashingFangPvE.ID) && !NoMercyPvE.Cooldown.IsCoolingDown)
		{
			return base.DefenseAreaAbility(nextGCD, out act);
		}

		if (!HasNoMercy && HeartOfLightPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (!HasNoMercy && ReprisalPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		return base.DefenseAreaAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.HeartOfStonePvE, ActionID.NebulaPvE, ActionID.RampartPvE, ActionID.CamouflagePvE, ActionID.ReprisalPvE)]
	protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
	{
		if (nextGCD.IsTheSameTo(false, (ActionID)GnashingFangPvE.ID) && !NoMercyPvE.Cooldown.IsCoolingDown)
		{
			return base.DefenseSingleAbility(nextGCD, out act);
		}

		//10
		if (CamouflagePvE.CanUse(out act))
		{
			return true;
		}
		//15

		if (HeartOfStonePvE.EnoughLevel)
		{
			switch (HeartOfStoneUsage)
			{
				case HeartOfStoneStrategy.SelfOnly:
					if (HeartOfCorundumPvE.EnoughLevel && HeartOfCorundumPvE.CanUse(out act))
					{
						return true;
					}
					if (!HeartOfCorundumPvE.EnoughLevel && HeartOfStonePvE.CanUse(out act))
					{
						return true;
					}
					break;

				case HeartOfStoneStrategy.TankbusterTarget:
					if (HeartOfCorundumPvE.EnoughLevel && HeartOfCorundumPvE.CanUse(out act, targetOverride: TargetType.Tankbuster))
					{
						return true;
					}
					if (!HeartOfCorundumPvE.EnoughLevel && HeartOfStonePvE.CanUse(out act, targetOverride: TargetType.Tankbuster))
					{
						return true;
					}
					break;

				case HeartOfStoneStrategy.Fullusage:
				default:
					if (HeartOfCorundumPvE.EnoughLevel && HeartOfCorundumPvE.CanUse(out act, targetOverride: TargetType.LowHP))
					{
						return true;
					}
					if (!HeartOfCorundumPvE.EnoughLevel && HeartOfStonePvE.CanUse(out act, targetOverride: TargetType.LowHP))
					{
						return true;
					}
					break;
			}
		}

		//30
		if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && GreatNebulaPvE.CanUse(out act) && GreatNebulaPvE.EnoughLevel)
		{
			return true;
		}

		if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && NebulaPvE.CanUse(out act) && !GreatNebulaPvE.EnoughLevel)
		{
			return true;
		}

		//20
		if (!NebulaPvE.EnoughLevel)
		{
			if (RampartPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (NebulaPvE.EnoughLevel && !GreatNebulaPvE.EnoughLevel)
		{
			if (NebulaPvE.Cooldown.IsCoolingDown && NebulaPvE.Cooldown.ElapsedAfter(30) && RampartPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (GreatNebulaPvE.EnoughLevel)
		{
			if (GreatNebulaPvE.Cooldown.IsCoolingDown && GreatNebulaPvE.Cooldown.ElapsedAfter(30) && RampartPvE.CanUse(out act))
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

	[RotationDesc(ActionID.AuroraPvE)]
	protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
	{
		if (nextGCD.IsTheSameTo(false, (ActionID)GnashingFangPvE.ID) && !NoMercyPvE.Cooldown.IsCoolingDown)
		{
			return base.HealSingleAbility(nextGCD, out act);
		}

		if (!IsLastAbility(ActionID.AuroraPvE))
		{
			switch (AuroraUsage)
			{
				case AuroraUsageStrategy.SelfOnly:
					if (AuroraPvE.CanUse(out act))
					{
						return true;
					}
					break;

				case AuroraUsageStrategy.TankbusterTarget:
					if (AuroraPvE.CanUse(out act, targetOverride: TargetType.Tankbuster))
					{
						return true;
					}
					break;

				case AuroraUsageStrategy.Fullusage:
				default:
					if (AuroraPvE.CanUse(out act, targetOverride: TargetType.LowHP))
					{
						return true;
					}
					break;
			}
		}

		return base.HealSingleAbility(nextGCD, out act);
	}

	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		// ST No Mercy Logic
		if (ReignOfBeastsPvE.EnoughLevel && ShouldUseLevel100NoMercy(nextGCD) && NoMercyPvE.CanUse(out act))
		{
			return true;
		}

		if (!ReignOfBeastsPvE.EnoughLevel && nextGCD.IsTheSameTo(false, (ActionID)GnashingFangPvE.ID, (ActionID)ReignOfBeastsPvE.ID) && NoMercyPvE.CanUse(out act))
		{
			return true;
		}

		if (!GnashingFangPvE.EnoughLevel && nextGCD.IsTheSameTo(false, (ActionID)BurstStrikePvE.ID) && NoMercyPvE.CanUse(out act))
		{
			return true;
		}

		if (!BurstStrikePvE.EnoughLevel && nextGCD.IsTheSameTo(false, (ActionID)SolidBarrelPvE.ID) && NoMercyPvE.CanUse(out act))
		{
			return true;
		}

		if (!SolidBarrelPvE.EnoughLevel && nextGCD.IsTheSameTo(false, (ActionID)BrutalShellPvE.ID) && NoMercyPvE.CanUse(out act))
		{
			return true;
		}

		if (!BrutalShellPvE.EnoughLevel && nextGCD.IsTheSameTo(false, (ActionID)KeenEdgePvE.ID) && NoMercyPvE.CanUse(out act))
		{
			return true;
		}

		// AOE No Mercy Logic
		if (DemonSlicePvE.CanUse(out _) && nextGCD.IsTheSameTo(false, (ActionID)DoubleDownPvE.ID) && NoMercyPvE.CanUse(out act))
		{
			return true;
		}

		if (!DoubleDownPvE.EnoughLevel && nextGCD.IsTheSameTo(false, (ActionID)FatedCirclePvE.ID) && NoMercyPvE.CanUse(out act))
		{
			return true;
		}

		if (!FatedCirclePvE.EnoughLevel && nextGCD.IsTheSameTo(false, (ActionID)DemonSlaughterPvE.ID) && NoMercyPvE.CanUse(out act))
		{
			return true;
		}

		if (!DemonSlaughterPvE.EnoughLevel && nextGCD.IsTheSameTo(false, (ActionID)DemonSlicePvE.ID) && NoMercyPvE.CanUse(out act))
		{
			return true;
		}

		if (BloodfestPvE.CanUse(out act))
		{
			if (HasNoMercy
				|| !NoMercyPvE.EnoughLevel
				|| NoMercyPvE.Cooldown.WillHaveOneCharge(FillerGnashingHoldWindow)
				|| InGnashingFang
				|| HasReadyToGouge)
			{
				return true;
			}
		}

		if (nextGCD.IsTheSameTo(false, (ActionID)GnashingFangPvE.ID) && !NoMercyPvE.Cooldown.IsCoolingDown)
		{
			return base.AttackAbility(nextGCD, out act);
		}

		if (DangerZonePvE.CanUse(out act) && !DoubleDownPvE.EnoughLevel)
		{

			if (!IsFullParty && !(DangerZonePvE.Target.Target?.IsBossFromTTK() ?? false))
			{
				return true;
			}

			if (!GnashingFangPvE.EnoughLevel && (HasNoMercy || !NoMercyPvE.Cooldown.WillHaveOneCharge(15)))
			{
				return true;
			}

			if (HasNoMercy && !GnashingFangPvE.Cooldown.HasOneCharge)
			{
				return true;
			}

			if (!HasNoMercy && !GnashingFangPvE.Cooldown.WillHaveOneCharge(20))
			{
				return true;
			}
		}

		if (HasNoMercy && BowShockPvE.CanUse(out act, skipAoeCheck: true))
		{
			//AOE CHECK
			if (DemonSlicePvE.CanUse(out _) && !IsFullParty)
			{
				return true;
			}

			if (!SonicBreakPvE.EnoughLevel && HasNoMercy)
			{
				return true;
			}

			if (HasNoMercy && SonicBreakPvE.Cooldown.IsCoolingDown)
			{
				return true;
			}
		}

		if (HasNoMercy && IsLastGCD(ActionID.DoubleDownPvE) && BlastingZonePvE.CanUse(out act))
		{
			return true;
		}

		if (NoMercyPvE.Cooldown.IsCoolingDown && BloodfestPvE.Cooldown.IsCoolingDown && BlastingZonePvE.CanUse(out act))
		{
			return true;
		}

		return base.AttackAbility(nextGCD, out act);
	}
	#endregion

	#region GCD Logic
	protected override bool GeneralGCD(out IAction? act)
	{
		if (BurstStrikePvE.CanUse(out act))
		{
			if (IsAmmoCapped && SolidBarrelPvE.CanUse(out _))
			{
				return true;
			}

			if (IsAmmoCapped && BloodfestPvE.EnoughLevel && NoMercyPvE.Cooldown.WillHaveOneChargeGCD(1))
			{
				return true;
			}

			if (ShouldStallForGnashingFang())
			{
				return true;
			}
		}

		if (InReignCombo)
		{
			if (LionHeartPvE.CanUse(out act, skipComboCheck: true))
			{
				return true;
			}

			if (NobleBloodPvE.CanUse(out act, skipComboCheck: true))
			{
				return true;
			}
		}

		if (!InGnashingFang && GnashingFangPvE.Cooldown.CurrentCharges < 2 && HasNoMercy && DoubleDownPvE.Cooldown.IsCoolingDown)
		{
			if (LionHeartPvE.CanUse(out act, skipComboCheck: true))
			{
				return true;
			}

			if (NobleBloodPvE.CanUse(out act, skipComboCheck: true))
			{
				return true;
			}

			if (ReignOfBeastsPvE.CanUse(out act, skipComboCheck: true))
			{
				return true;
			}
		}

		if (!InReignCombo)
		{
			if (SavageClawPvE.CanUse(out act, skipComboCheck: true))
			{
				return true;
			}

			if (WickedTalonPvE.CanUse(out act, skipComboCheck: true))
			{
				return true;
			}

			if (ShouldStartGnashingFang() && GnashingFangPvE.CanUse(out act, skipComboCheck: true))
			{
				return true;
			}

			if (HasNoMercy && DoubleDownPvE.CanUse(out act))
			{
				return true;
			}

			if (HasNoMercy && SonicBreakPvE.CanUse(out act))
			{
				return true;
			}

			if (ShouldStartGnashingFang() && GnashingFangPvE.CanUse(out act, skipComboCheck: true, usedUp: HasNoMercy))
			{
				return true;
			}
		}

		if (BurstStrikePvE.CanUse(out act))
		{
			if (Ammo > 3 && OvercappedAmmo() > 0)
			{
				if (StatusHelper.PlayerWillStatusEndGCD(OvercappedAmmo(), 0, true, StatusID.Bloodfest))
				{
					return true;
				}
			}

			if (
				// Condition 1: No Mercy is active, AmmoComboStep is 0, and Gnashing Fang cooldown won't have a charge
				(HasNoMercy && AmmoComboStep == 0 && !GnashingFangPvE.Cooldown.WillHaveOneCharge(1)) ||

				// Condition 2: Last combo action was Brutal Shell, and either Ammo is capped or Bloodfest conditions are met
				(IsLastComboAction((ActionID)BrutalShellPvE.ID) &&
				(IsAmmoCapped || (BloodfestPvE.Cooldown.WillHaveOneCharge(6) && Ammo <= 2 && !NoMercyPvE.Cooldown.WillHaveOneCharge(10) && BloodfestPvE.EnoughLevel))) ||

				// Condition 3: Ammo is capped and one of the following is true:
				// - Last GCD was Brutal Shell
				// - Ready to Reign and last combo action was Keen Edge
				// - Gnashing Fang is available and No Mercy is active
				(IsAmmoCapped && (IsLastGCD(ActionID.BrutalShellPvE) || (HasReadyToReign && IsLastComboAction(false, KeenEdgePvE)) || (GnashingFangPvE.EnoughLevel && HasNoMercy)))
				)
			{
				return true;
			}
		}

		if (!InGnashingFang && !InReignCombo)
		{
			if (FatedCirclePvE.CanUse(out act))
			{
				return true;
			}

			if (DemonSlaughterPvE.CanUse(out act))
			{
				return true;
			}

			if (DemonSlicePvE.CanUse(out act))
			{
				return true;
			}

			if (SolidBarrelPvE.CanUse(out act))
			{
				return true;
			}

			if (BrutalShellPvE.CanUse(out act))
			{
				return true;
			}

			if (KeenEdgePvE.CanUse(out act))
			{
				return true;
			}
		}

		if (LightningShotPvE.CanUse(out act))
		{
			return true;
		}

		return base.GeneralGCD(out act);
	}
	#endregion
}
