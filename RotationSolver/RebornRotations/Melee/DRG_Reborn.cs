namespace RotationSolver.RebornRotations.Melee;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.41")]
[SourceCode(Path = "main/RebornRotations/Melee/DRG_Reborn.cs")]


public sealed class DRG_Reborn : DragoonRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Use Doom Spike for damage uptime if out of melee range even if it breaks combo")]
    public bool DoomSpikeWhenever { get; set; } = true;

    [Range(1, 20, ConfigUnitType.Yalms)]
    [RotationConfig(CombatType.PvE, Name = "Max distance you need to be from the target for Stardiver useage")]
    public float StardiverDistance { get; set; } = 20;

    [Range(1, 20, ConfigUnitType.Yalms)]
    [RotationConfig(CombatType.PvE, Name = "Max distance you need to be from the target for Dragonfire Dive useage")]
    public float DragonfireDiveDistance { get; set; } = 20;
    #endregion

    #region Additional oGCD Logic

    [RotationDesc(ActionID.WingedGlidePvE)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (IsLastAction(false, StardiverPvE))
        {
            return base.MoveForwardAbility(nextGCD, out act);
        }

        if (WingedGlidePvE.CanUse(out act, skipComboCheck: true))
        {
            return true;
        }
        return base.MoveForwardAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.ElusiveJumpPvE)]
    protected override bool MoveBackAbility(IAction nextGCD, out IAction? act)
    {
        if (IsLastAction(false, StardiverPvE))
        {
            return base.MoveBackAbility(nextGCD, out act);
        }

        if (ElusiveJumpPvE.CanUse(out act, skipComboCheck: true))
        {
            return true;
        }
        return base.MoveBackAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.FeintPvE)]
    protected sealed override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (IsLastAction(false, StardiverPvE))
        {
            return base.DefenseAreaAbility(nextGCD, out act);
        }

        if (FeintPvE.CanUse(out act, skipComboCheck: true))
        {
            return true;
        }
        return base.DefenseAreaAbility(nextGCD, out act);
    }
    #endregion

    #region oGCD Logic
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (IsLastAction() == IsLastGCD())
        {
            if (StardiverPvE.CanUse(out act))
            {
                if (StardiverPvE.Target.Target.DistanceToPlayer() <= StardiverDistance)
                {
                    return true;
                }
            }
        }

        if (HasBattleLitany && InCombat && UseBurstMedicine(out act))
        {
            return true;
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (IsLastAction(false, StardiverPvE))
        {
            return base.AttackAbility(nextGCD, out act);
        }

        if (DisembowelPvE.EnoughLevel)
        {
            if (!HasPowerSurge)
            {
                return base.AttackAbility(nextGCD, out act);
            }
        }

        if (IsBurst && InCombat && HasHostilesInRange)
        {
            bool lsOnBigHit =
                nextGCD.IsTheSameTo(true, HeavensThrustPvE, DrakesbanePvE) &&
                (HasBattleLitany || LOTDEndAfter(1000) ||
                 LanceChargePvE.Cooldown.IsCoolingDown || BattleLitanyPvE.Cooldown.IsCoolingDown);

            bool lsOnAltFinishersDuringBuff =
                HasBattleLitany &&
                LOTDEndAfter(1000) &&
                nextGCD.IsTheSameTo(true, ChaoticSpringPvE, LanceBarragePvE, WheelingThrustPvE, FangAndClawPvE);

            bool lsLowLevel =
                (!DisembowelPvE.EnoughLevel && nextGCD.IsTheSameTo(true, VorpalThrustPvE)) ||
                (!FullThrustPvE.EnoughLevel && nextGCD.IsTheSameTo(true, VorpalThrustPvE, DisembowelPvE)) ||
                (!LanceChargePvE.EnoughLevel && nextGCD.IsTheSameTo(true, DisembowelPvE, FullThrustPvE)) ||
                (!BattleLitanyPvE.EnoughLevel && nextGCD.IsTheSameTo(true, FullThrustPvE)) ||
                (!HeavensThrustPvE.EnoughLevel && !DrakesbanePvE.EnoughLevel &&
                 HasBattleLitany && HasLanceCharge && LOTDEndAfter(1000) &&
                 nextGCD.IsTheSameTo(true, ChaoticSpringPvE, LanceBarragePvE, WheelingThrustPvE, FangAndClawPvE));

            bool lifeSurgeReady = lsOnBigHit || lsOnAltFinishersDuringBuff || lsLowLevel;

            if ((!BattleLitanyPvE.Cooldown.ElapsedAfter(60) || !BattleLitanyPvE.EnoughLevel)
                && LanceChargePvE.CanUse(out act))
            {
                return true;
            }

            if (BattleLitanyPvE.CanUse(out act))
            {
                return true;
            }

            if (lifeSurgeReady && LifeSurgePvE.CanUse(out act, usedUp: HasLanceCharge))
            {
                return true;
            }
        }

        if (GeirskogulPvE.CanUse(out act))
        {
            return true;
        }

        if ((BattleLitanyPvE.EnoughLevel && HasBattleLitany) || (!BattleLitanyPvE.EnoughLevel))
        {
            if (DragonfireDivePvE.CanUse(out act))
            {
                if (DragonfireDivePvE.Target.Target.DistanceToPlayer() <= DragonfireDiveDistance)
                {
                    return true;
                }
            }
        }

        if (HasBattleLitany || LOTDEndAfter(1000)
            || nextGCD.IsTheSameTo(true, RaidenThrustPvE, DraconianFuryPvE))
        {
            if (WyrmwindThrustPvE.CanUse(out act, usedUp: true))
            {
                return true;
            }
        }

        if (!HighJumpPvE.EnoughLevel && LanceChargePvE.Cooldown.IsCoolingDown)
        {
            if (JumpPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (GeirskogulPvE.Cooldown.IsCoolingDown || LOTDTime > 0)
        {
            if (HighJumpPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (NastrondPvE.CanUse(out act))
        {
            return true;
        }

        if (StarcrossPvE.CanUse(out act))
        {
            return true;
        }

        if (RiseOfTheDragonPvE.CanUse(out act))
        {
            return true;
        }

        if (MirageDivePvE.CanUse(out act))
        {
            return true;
        }

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        bool doomSpikeRightNow = DoomSpikeWhenever;

        if (CoerthanTormentPvE.CanUse(out act))
        {
            return true;
        }

        if (SonicThrustPvE.CanUse(out act, skipStatusProvideCheck: true))
        {
            return true;
        }

        if (LanceMasteryTrait.EnoughLevel)
        {
            if (HasDraconianFire)
            {
                if (DraconianFuryPvE.CanUse(out act, skipComboCheck: doomSpikeRightNow))
                {
                    return true;
                }
            }
            if (!HasDraconianFire)
            {
                if (DoomSpikePvE.CanUse(out act, skipComboCheck: doomSpikeRightNow))
                {
                    return true;
                }
            }
        }
        if (!LanceMasteryTrait.EnoughLevel)
        {
            if (DoomSpikePvE.CanUse(out act, skipComboCheck: doomSpikeRightNow))
            {
                return true;
            }
        }

        if (DrakesbanePvE.CanUse(out act, skipStatusProvideCheck: true))
        {
            return true;
        }

        if (FangAndClawPvE.CanUse(out act))
        {
            return true;
        }

        if (WheelingThrustPvE.CanUse(out act))
        {
            return true;
        }

        if (LanceMasteryIiTrait.EnoughLevel)
        {
            if (HeavensThrustPvE.CanUse(out act))
            {
                return true;
            }
        }
        if (!LanceMasteryIiTrait.EnoughLevel)
        {
            if (FullThrustPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (LanceMasteryIiTrait.EnoughLevel)
        {
            if (ChaoticSpringPvE.CanUse(out act, skipStatusProvideCheck: true))
            {
                return true;
            }
        }
        if (!LanceMasteryIiTrait.EnoughLevel)
        {
            if (ChaosThrustPvE.CanUse(out act, skipStatusProvideCheck: true))
            {
                return true;
            }
        }

        if (LanceMasteryIvTrait.EnoughLevel)
        {
            if (SpiralBlowPvE.CanUse(out act, skipStatusProvideCheck: StatusHelper.PlayerWillStatusEndGCD(6, 0, true, StatusID.PowerSurge_2720)))
			{
                return true;
            }
        }
        if (!LanceMasteryIvTrait.EnoughLevel)
        {
            if (DisembowelPvE.CanUse(out act, skipStatusProvideCheck: StatusHelper.PlayerWillStatusEndGCD(6, 0, true, StatusID.PowerSurge_2720)))
            {
                return true;
            }
        }

        if (LanceMasteryIvTrait.EnoughLevel)
        {
            if (LanceBarragePvE.CanUse(out act))
            {
                return true;
            }
        }
        if (!LanceMasteryIvTrait.EnoughLevel)
        {
            if (VorpalThrustPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (LanceMasteryTrait.EnoughLevel)
        {
            if (HasDraconianFire)
            {
                if (RaidenThrustPvE.CanUse(out act))
                {
                    return true;
                }
            }
            if (!HasDraconianFire)
            {
                if (TrueThrustPvE.CanUse(out act))
                {
                    return true;
                }
            }
        }
        if (!LanceMasteryTrait.EnoughLevel)
        {
            if (TrueThrustPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (!IsLastAction(true, WingedGlidePvE) && PiercingTalonPvE.CanUse(out act))
        {
            return true;
        }

        return base.GeneralGCD(out act);
    }
    #endregion
}
