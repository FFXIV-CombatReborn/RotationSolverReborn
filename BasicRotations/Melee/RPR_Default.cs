namespace RebornRotations.Melee;

[Rotation("Default", CombatType.PvE, GameVersion = "7.2")]
[SourceCode(Path = "main/BasicRotations/Melee/RPR_Default.cs")]
[Api(4)]
public sealed class RPR_Default : ReaperRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "[Beta Option] Pool Shroud for Arcane Circle.")]
    public bool EnshroudPooling { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use custom timing to refresh Death's Design")]
    public bool UseCustomDDTiming { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Refresh Death's Design with this many seconds remaining")]
    public int RefreshDDSecondsRemaining { get; set; } = 10;

    public static bool ExecutionerReady => Player.HasStatus(true, StatusID.Executioner);
    #endregion

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (remainTime < HarpePvE.Info.CastTime + CountDownAhead
            && HarpePvE.CanUse(out var act)) return act;

        if (SoulsowPvE.CanUse(out act)) return act;

        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        bool IsTargetBoss = CurrentTarget?.IsBossFromTTK() ?? false;
        bool IsTargetDying = CurrentTarget?.IsDying() ?? false;
        bool NoEnshroudPooling = !EnshroudPooling && Shroud >= 50;
        bool YesEnshroudPooling = EnshroudPooling && Shroud >= 50 && (!PlentifulHarvestPvE.EnoughLevel || Player.HasStatus(true, StatusID.ArcaneCircle) || ArcaneCirclePvE.Cooldown.WillHaveOneCharge(8) || !Player.HasStatus(true, StatusID.ArcaneCircle) && ArcaneCirclePvE.Cooldown.WillHaveOneCharge(65) && !ArcaneCirclePvE.Cooldown.WillHaveOneCharge(50) || !Player.HasStatus(true, StatusID.ArcaneCircle) && Shroud >= 90);
        bool IsIdealHost = Player.HasStatus(true, StatusID.IdealHost);

        if (IsBurst)
        {
            if ((CurrentTarget?.HasStatus(true, StatusID.DeathsDesign) ?? false)
                && !CombatElapsedLess(3.5f) && ArcaneCirclePvE.CanUse(out act, skipAoeCheck: true)) return true;
        }

        if ((!Player.HasStatus(true, StatusID.Executioner)) && (IsTargetBoss && IsTargetDying || NoEnshroudPooling || YesEnshroudPooling || IsIdealHost))
        {
            if (EnshroudPvE.CanUse(out act)) return true;
        }

        if (SacrificiumPvE.CanUse(out act, skipAoeCheck: true, usedUp: true)) return true;

        if (HasEnshrouded && (Player.HasStatus(true, StatusID.ArcaneCircle) || LemureShroud < 3))
        {
            if (LemuresScythePvE.CanUse(out act, usedUp: true)) return true;
            if (LemuresSlicePvE.CanUse(out act, usedUp: true)) return true;
        }

        if (PlentifulHarvestPvE.EnoughLevel && !HasPerfectioParata && !Player.HasStatus(true, StatusID.ImmortalSacrifice) /*&& !Player.HasStatus(true, StatusID.BloodsownCircle_2972) */|| !PlentifulHarvestPvE.EnoughLevel)
        {
            if (GluttonyPvE.CanUse(out act, skipAoeCheck: true)) return true;
        }

        if (!Player.HasStatus(true, StatusID.BloodsownCircle_2972) && !HasPerfectioParata && !Player.HasStatus(true, StatusID.Executioner) && !Player.HasStatus(true, StatusID.ImmortalSacrifice) && (GluttonyPvE.EnoughLevel && !GluttonyPvE.Cooldown.WillHaveOneChargeGCD(4) || !GluttonyPvE.EnoughLevel || Soul == 100))
        {
            if (GrimSwathePvE.CanUse(out act)) return true;
            if (BloodStalkPvE.CanUse(out act)) return true;
        }

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        if (ExecutionersGuillotinePvE.EnoughLevel && (IsLastAction(true, GluttonyPvE) || Player.HasStatus(true, StatusID.Executioner)))
        {
            return ItsGluttonyTime(out act);
        }

        if (SoulsowPvE.CanUse(out act)) return true;

        if (!ExecutionerReady && !HasSoulReaver)
        {
            if (PerfectioPvE.CanUse(out act, skipAoeCheck: true)) return true;
        }

        if (WhorlOfDeathPvE.CanUse(out act)) return true;
        if (UseCustomDDTiming && ((!CurrentTarget?.HasStatus(true, StatusID.DeathsDesign) ?? false) || (CurrentTarget?.WillStatusEnd(RefreshDDSecondsRemaining, true, StatusID.DeathsDesign) ?? false)))
        {
            if (ShadowOfDeathPvE.CanUse(out act, skipStatusProvideCheck: true)) return true;
        }
        else
        {
            if (ShadowOfDeathPvE.CanUse(out act)) return true;
        }

        if (HasEnshrouded)
        {
            if (ShadowOfDeathPvE.CanUse(out act)) return true;

            if (LemureShroud > 1)
            {
                if (PlentifulHarvestPvE.EnoughLevel && ArcaneCirclePvE.Cooldown.WillHaveOneCharge(9) &&
                   (LemureShroud == 4 && (ShadowOfDeathPvE.Target.Target?.WillStatusEnd(30, true, StatusID.DeathsDesign) ?? false) || LemureShroud == 3 && (ShadowOfDeathPvE.Target.Target?.WillStatusEnd(50, true, StatusID.DeathsDesign) ?? false)))
                {
                    if (ShadowOfDeathPvE.CanUse(out act, skipStatusProvideCheck: true)) return true;
                }

                if (Reaping(out act)) return true;
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
                        if (ShadowOfDeathPvE.CanUse(out act, skipAoeCheck: IsMoving)) return true;
                    }
                }
                else
                {
                    if (Reaping(out act)) return true;
                }
            }
        }



        if (HasSoulReaver)
        {
            if (GuillotinePvE.CanUse(out act)) return true;

            if (Player.HasStatus(true, StatusID.EnhancedGallows))
            {
                if (GallowsPvE.CanUse(out act, skipComboCheck: true)) return true;
            }
            else if (Player.HasStatus(true, StatusID.EnhancedGibbet))
            {
                if (GibbetPvE.CanUse(out act, skipComboCheck: true)) return true;
            }

            // Try using Gallows/Gibbet that player is in position for when without Enchanced status
            if (GallowsPvE.CanUse(out act, skipComboCheck: true) && GallowsPvE.Target.Target != null && CanHitPositional(EnemyPositional.Rear, GallowsPvE.Target.Target)) return true;
            if (GibbetPvE.CanUse(out act, skipComboCheck: true) && GibbetPvE.Target.Target != null && CanHitPositional(EnemyPositional.Flank, GibbetPvE.Target.Target)) return true;

            if (GallowsPvE.CanUse(out act, skipComboCheck: true)) return true;
            if (GibbetPvE.CanUse(out act, skipComboCheck: true)) return true;
        }

        if (!CombatElapsedLessGCD(2) && PlentifulHarvestPvE.CanUse(out act, skipAoeCheck: true)) return true;

        if (SoulScythePvE.CanUse(out act, usedUp: true)) return true;
        if (SoulSlicePvE.CanUse(out act, usedUp: true)) return true;

        if (NightmareScythePvE.CanUse(out act)) return true;
        if (SpinningScythePvE.CanUse(out act)) return true;

        if (!Player.HasStatus(true, StatusID.Executioner) && InfernalSlicePvE.CanUse(out act)) return true;
        if (!Player.HasStatus(true, StatusID.Executioner) && WaxingSlicePvE.CanUse(out act)) return true;
        if (!Player.HasStatus(true, StatusID.Executioner) && SlicePvE.CanUse(out act)) return true;

        if (InCombat && !HasSoulReaver && HarvestMoonPvE.CanUse(out act, skipAoeCheck: true)) return true;
        if (HarpePvE.CanUse(out act)) return true;

        return base.GeneralGCD(out act);
    }
    #endregion

    #region Extra Methods
    private bool Reaping(out IAction? act)
    {
        if (GrimReapingPvE.CanUse(out act)) return true;
        if (Player.HasStatus(true, StatusID.EnhancedCrossReaping) || !Player.HasStatus(true, StatusID.EnhancedVoidReaping))
        {
            if (CrossReapingPvE.CanUse(out act)) return true;
        }
        else
        {
            if (VoidReapingPvE.CanUse(out act)) return true;
        }
        return false;
    }

    private bool ItsGluttonyTime(out IAction? act)
    {
        if (ExecutionerReady)
        {
            if (ExecutionersGuillotinePvE.CanUse(out act)) return true;

            if (Player.HasStatus(true, StatusID.EnhancedGallows))
            {
                if (ExecutionersGallowsPvE.CanUse(out act, skipComboCheck: true)) return true;
            }
            else if (Player.HasStatus(true, StatusID.EnhancedGibbet))
            {
                if (ExecutionersGibbetPvE.CanUse(out act, skipComboCheck: true)) return true;
            }

            // Try using Executioners Gallows/Gibbet that player is in position for when without Enchanced status
            if (ExecutionersGallowsPvE.CanUse(out act, skipComboCheck: true) && ExecutionersGallowsPvE.Target.Target != null && CanHitPositional(EnemyPositional.Rear, ExecutionersGallowsPvE.Target.Target)) return true;
            if (ExecutionersGibbetPvE.CanUse(out act, skipComboCheck: true) && ExecutionersGibbetPvE.Target.Target != null && CanHitPositional(EnemyPositional.Flank, ExecutionersGibbetPvE.Target.Target)) return true;

            if (ExecutionersGallowsPvE.CanUse(out act, skipComboCheck: true)) return true;
            if (ExecutionersGibbetPvE.CanUse(out act, skipComboCheck: true)) return true;
        }
        act = null;
        return false;
    }
    #endregion 
}
