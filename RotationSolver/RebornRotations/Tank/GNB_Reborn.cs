namespace RotationSolver.RebornRotations.Tank;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.4")]
[SourceCode(Path = "main/RebornRotations/Tank/GNB_Reborn.cs")]

public sealed class GNB_Reborn : GunbreakerRotation
{
    #region Config Options

    #endregion

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (remainTime <= 0.7 && LightningShotPvE.CanUse(out IAction? act))
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
        // ST No Mercy Logic
        if (nextGCD.IsTheSameTo(false, (ActionID)GnashingFangPvE.ID) && NoMercyPvE.CanUse(out act))
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

        if (Ammo == 0 && BloodfestPvE.CanUse(out act))
        {
            return true;
        }

        if (AbdomenTearPvE.CanUse(out act))
        {
            return true;
        }

        if (EyeGougePvE.CanUse(out act))
        {
            return true;
        }

        if (FatedBrandPvE.CanUse(out act))
        {
            return true;
        }

        if (HypervelocityPvE.CanUse(out act))
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
        if (HeartOfCorundumPvE.CanUse(out act))
        {
            return true;
        }

		if (!HeartOfCorundumPvE.EnoughLevel && HeartOfStonePvE.CanUse(out act))
		{
			return true;
		}

		//30
		if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && NebulaPvE.CanUse(out act))
        {
            return true;
        }
        //20
        if (NebulaPvE.Cooldown.IsCoolingDown && NebulaPvE.Cooldown.ElapsedAfter(60) && RampartPvE.CanUse(out act))
        {
            return true;
        }

        if (ReprisalPvE.CanUse(out act))
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
            if (AuroraPvE.CanUse(out act))
            {
                return true;
            }
        }

        return base.HealSingleAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (nextGCD.IsTheSameTo(false, (ActionID)GnashingFangPvE.ID) && !NoMercyPvE.Cooldown.IsCoolingDown)
        {
            return base.AttackAbility(nextGCD, out act);
        }

        if (JugularRipPvE.CanUse(out act))
        {
            return true;
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

            if (HasNoMercy && GnashingFangPvE.Cooldown.IsCoolingDown)
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
            if (IsAmmoCapped && BloodfestPvE.EnoughLevel && NoMercyPvE.Cooldown.WillHaveOneChargeGCD(1))
            {
                return true;
            }
        }

        if (!InReignCombo)
        {
            if (AmmoComboStep == 0 && GnashingFangPvE.CanUse(out act, skipComboCheck: true, usedUp: true))
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

            if (SavageClawPvE.CanUse(out act, skipComboCheck: true))
            {
                return true;
            }

            if (WickedTalonPvE.CanUse(out act, skipComboCheck: true))
            {
                return true;
            }
        }

        if ((!InGnashingFang && !GnashingFangPvE.Cooldown.HasOneCharge) || InReignCombo)
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

        if (BurstStrikePvE.CanUse(out act))
        {
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