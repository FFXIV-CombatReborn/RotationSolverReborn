namespace RotationSolver.RebornRotations.Melee;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.41")]
[SourceCode(Path = "main/RebornRotations/Melee/RPR_Reborn.cs")]

public sealed class RPR_Reborn : ReaperRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Pool Shroud for Arcane Circle.")]
    public bool EnshroudPooling { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use custom timing to refresh Death's Design")]
    public bool UseCustomDDTiming { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Refresh Death's Design with this many seconds remaining", Parent = nameof(UseCustomDDTiming))]
    public int RefreshDDSecondsRemaining { get; set; } = 10;

    #endregion

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (remainTime < HarpePvE.Info.CastTime + CountDownAhead
            && HarpePvE.CanUse(out IAction? act))
        {
            return act;
        }
        if (SoulsowPvE.CanUse(out act))
        {
            return act;
        }
        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    [RotationDesc(ActionID.HellsIngressPvE)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (HellsIngressPvE.CanUse(out act))
        {
            return true;
        }
        return base.MoveForwardAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.HellsEgressPvE)]
    protected override bool MoveBackAbility(IAction nextGCD, out IAction? act)
    {
        if (HellsEgressPvE.CanUse(out act))
        {
            return true;
        }
        return base.MoveBackAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.FeintPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (!HasSoulReaver && !HasEnshrouded && !HasExecutioner && FeintPvE.CanUse(out act))
        {
            return true;
        }
        return base.DefenseAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.ArcaneCrestPvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (!HasSoulReaver && !HasEnshrouded && !HasExecutioner && ArcaneCrestPvE.CanUse(out act))
        {
            return true;
        }
        return base.DefenseSingleAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        bool IsTargetBoss = CurrentTarget?.IsBossFromTTK() ?? false;
        bool IsTargetDying = CurrentTarget?.IsDying() ?? false;
        bool NoEnshroudPooling = !EnshroudPooling && Shroud >= 50;
        bool YesEnshroudPooling = EnshroudPooling && Shroud >= 50 && (!PlentifulHarvestPvE.EnoughLevel || HasArcaneCircle || ArcaneCirclePvE.Cooldown.WillHaveOneCharge(8) || (!HasArcaneCircle && ArcaneCirclePvE.Cooldown.WillHaveOneCharge(65) && !ArcaneCirclePvE.Cooldown.WillHaveOneCharge(50)) || (!HasArcaneCircle && Shroud >= 90));
        bool HasIdealHostBool = HasIdealHost;

        if (IsBurst)
        {
            if ((CurrentTarget?.HasStatus(true, StatusID.DeathsDesign) ?? false)
                && !CombatElapsedLess(3.5f) && ArcaneCirclePvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }
        }

        if ((!HasExecutioner) && ((IsTargetBoss && IsTargetDying) || NoEnshroudPooling || YesEnshroudPooling || HasIdealHostBool))
        {
            if (EnshroudPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (SacrificiumPvE.CanUse(out act, skipAoeCheck: true, usedUp: true))
        {
            return true;
        }

        if (HasEnshrouded && (HasArcaneCircle || LemureShroud < 3))
        {
            if (LemuresScythePvE.CanUse(out act, usedUp: true))
            {
                return true;
            }

            if (LemuresSlicePvE.CanUse(out act, usedUp: true))
            {
                return true;
            }
        }

        if ((PlentifulHarvestPvE.EnoughLevel && !HasPerfectioParata && !HasImmortalSacrifice) /*&& !StatusHelper.PlayerHasStatus(true, StatusID.BloodsownCircle_2972) */|| !PlentifulHarvestPvE.EnoughLevel)
        {
            if (GluttonyPvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }
        }

        if (!HasBloodsownCircleSelf && !HasPerfectioParata && !HasExecutioner && !HasImmortalSacrifice && ((GluttonyPvE.EnoughLevel && !GluttonyPvE.Cooldown.WillHaveOneChargeGCD(4)) || !GluttonyPvE.EnoughLevel || Soul == 100))
        {
            if (GrimSwathePvE.CanUse(out act))
            {
                return true;
            }

            if (BloodStalkPvE.CanUse(out act))
            {
                return true;
            }
        }

        return base.AttackAbility(nextGCD, out act);
    }

    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        if (HasArcaneCircle && InCombat && UseBurstMedicine(out act))
        {
            return true;
        }

        return base.GeneralAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        if (EnhancedGluttonyTrait.EnoughLevel && (IsLastAction(true, GluttonyPvE, BloodStalkPvE) || HasExecutioner))
        {
            if (HasExecutioner)
            {
                if (ExecutionersGuillotinePvE.CanUse(out act))
                {
                    return true;
                }

                switch ((HasEnhancedGallows, HasEnhancedGibbet))
                {
                    case (true, true):
                        if (ExecutionersGallowsPvE.CanUse(out act, skipComboCheck: true) && ExecutionersGallowsPvE.Target.Target != null && CanHitPositional(EnemyPositional.Rear, ExecutionersGallowsPvE.Target.Target))
                            return true;
                        if (ExecutionersGibbetPvE.CanUse(out act, skipComboCheck: true) && ExecutionersGibbetPvE.Target.Target != null && CanHitPositional(EnemyPositional.Flank, ExecutionersGibbetPvE.Target.Target))
                            return true;
                        if (ExecutionersGallowsPvE.CanUse(out act, skipComboCheck: true))
                            return true;
                        if (ExecutionersGibbetPvE.CanUse(out act, skipComboCheck: true))
                            return true;
                        break;
                    case (true, false):
                        if (ExecutionersGallowsPvE.CanUse(out act, skipComboCheck: true))
                            return true;
                        break;
                    case (false, true):
                        if (ExecutionersGibbetPvE.CanUse(out act, skipComboCheck: true))
                            return true;
                        break;
                    case (false, false):
                        if (ExecutionersGallowsPvE.CanUse(out act, skipComboCheck: true) && ExecutionersGallowsPvE.Target.Target != null && CanHitPositional(EnemyPositional.Rear, ExecutionersGallowsPvE.Target.Target))
                            return true;
                        if (ExecutionersGibbetPvE.CanUse(out act, skipComboCheck: true) && ExecutionersGibbetPvE.Target.Target != null && CanHitPositional(EnemyPositional.Flank, ExecutionersGibbetPvE.Target.Target))
                            return true;
                        if (ExecutionersGallowsPvE.CanUse(out act, skipComboCheck: true))
                            return true;
                        if (ExecutionersGibbetPvE.CanUse(out act, skipComboCheck: true))
                            return true;
                        break;
                }
            }
        }

        if (SoulsowPvE.CanUse(out act))
        {
            return true;
        }

        if (!HasExecutioner && !HasSoulReaver)
        {
            if (PerfectioPvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }
        }

        // If at least 2 targets in a best Whorl of Death AoE lack Death's Design, allow refreshing via AoE
        int ddNeeds = 0;
        if (WhorlOfDeathPvE.CanUse(out _, skipAoeCheck: true) && WhorlOfDeathPvE.PreviewTarget.HasValue)
        {
            ddNeeds = WhorlOfDeathPvE.PreviewTarget.Value.AffectedTargets?.Length ?? 0;
        }
        if (ddNeeds >= 2)
        {
            if (WhorlOfDeathPvE.CanUse(out act, skipStatusProvideCheck: true))
            {
                return true;
            }
        }

        if (WhorlOfDeathPvE.CanUse(out act))
        {
            return true;
        }

        if (UseCustomDDTiming && ((!CurrentTarget?.HasStatus(true, StatusID.DeathsDesign) ?? false) || (CurrentTarget?.WillStatusEnd(RefreshDDSecondsRemaining, true, StatusID.DeathsDesign) ?? false)))
        {
            if (ShadowOfDeathPvE.CanUse(out act, skipStatusProvideCheck: true))
            {
                return true;
            }
        }
        else
        {
            if (ShadowOfDeathPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (HasEnshrouded)
        {
            if (ShadowOfDeathPvE.CanUse(out act))
            {
                return true;
            }

            if (LemureShroud > 1)
            {
                if (PlentifulHarvestPvE.EnoughLevel && ArcaneCirclePvE.Cooldown.WillHaveOneCharge(9) &&
                   ((LemureShroud == 4 && (CurrentTarget?.WillStatusEnd(30, true, StatusID.DeathsDesign) ?? false)) || (LemureShroud == 3 && (CurrentTarget?.WillStatusEnd(50, true, StatusID.DeathsDesign) ?? false))))
                {
                    if (ShadowOfDeathPvE.CanUse(out act, skipStatusProvideCheck: true))
                    {
                        return true;
                    }
                }

                if (GrimReapingPvE.CanUse(out act))
                {
                    return true;
                }

                switch ((HasEnhancedCrossReaping, HasEnhancedVoidReaping))
                {
                    case (true, _):
                        if (CrossReapingPvE.CanUse(out act))
                            return true;
                        break;
                    case (_, true):
                        if (VoidReapingPvE.CanUse(out act))
                            return true;
                        break;
                    case (false, false):
                        if (CrossReapingPvE.CanUse(out act))
                            return true;
                        if (VoidReapingPvE.CanUse(out act))
                            return true;
                        break;
                }
            }
            if (LemureShroud == 1)
            {
                if (CommunioPvE.EnoughLevel)
                {
                    if (!IsMoving && CommunioPvE.CanUse(out act, skipAoeCheck: true))
                    {
                        return true;
                    }
                    else
                    {
                        if (ShadowOfDeathPvE.CanUse(out act, skipAoeCheck: IsMoving))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    if (GrimReapingPvE.CanUse(out act))
                    {
                        return true;
                    }

                    switch ((HasEnhancedCrossReaping, HasEnhancedVoidReaping))
                    {
                        case (true, _):
                            if (CrossReapingPvE.CanUse(out act))
                                return true;
                            break;
                        case (_, true):
                            if (VoidReapingPvE.CanUse(out act))
                                return true;
                            break;
                        case (false, false):
                            if (CrossReapingPvE.CanUse(out act))
                                return true;
                            if (VoidReapingPvE.CanUse(out act))
                                return true;
                            break;
                    }
                }
            }
        }

        if (HasSoulReaver)
        {
            if (GuillotinePvE.CanUse(out act))
            {
                return true;
            }

            switch ((HasEnhancedGallows, HasEnhancedGibbet))
            {
                case (true, true):
                    if (GallowsPvE.CanUse(out act, skipComboCheck: true) && GallowsPvE.Target.Target != null && CanHitPositional(EnemyPositional.Rear, GallowsPvE.Target.Target))
                        return true;
                    if (GibbetPvE.CanUse(out act, skipComboCheck: true) && GibbetPvE.Target.Target != null && CanHitPositional(EnemyPositional.Flank, GibbetPvE.Target.Target))
                        return true;
                    if (GallowsPvE.CanUse(out act, skipComboCheck: true))
                        return true;
                    if (GibbetPvE.CanUse(out act, skipComboCheck: true))
                        return true;
                    break;
                case (true, _):
                    if (GallowsPvE.CanUse(out act, skipComboCheck: true))
                        return true;
                    break;
                case (_, true):
                    if (GibbetPvE.CanUse(out act, skipComboCheck: true))
                        return true;
                    break;
                case (false, false):
                    if (GallowsPvE.CanUse(out act, skipComboCheck: true) && GallowsPvE.Target.Target != null && CanHitPositional(EnemyPositional.Rear, GallowsPvE.Target.Target))
                        return true;
                    if (GibbetPvE.CanUse(out act, skipComboCheck: true) && GibbetPvE.Target.Target != null && CanHitPositional(EnemyPositional.Flank, GibbetPvE.Target.Target))
                        return true;
                    if (GallowsPvE.CanUse(out act, skipComboCheck: true))
                        return true;
                    if (GibbetPvE.CanUse(out act, skipComboCheck: true))
                        return true;
                    break;
            }
        }

        if (!CombatElapsedLessGCD(2) && PlentifulHarvestPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        if (SoulScythePvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        if (SoulSlicePvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        if (NightmareScythePvE.CanUse(out act))
        {
            return true;
        }

        if (SpinningScythePvE.CanUse(out act))
        {
            return true;
        }

        if (!HasExecutioner)
        {
            if (InfernalSlicePvE.CanUse(out act))
            {
                return true;
            }

            if (WaxingSlicePvE.CanUse(out act))
            {
                return true;
            }

            if (SlicePvE.CanUse(out act))
            {
                return true;
            }
        }

        if (InCombat && !HasSoulReaver && HarvestMoonPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        if (HarpePvE.CanUse(out act))
        {
            return true;
        }

        return base.GeneralGCD(out act);
    }
    #endregion
}
