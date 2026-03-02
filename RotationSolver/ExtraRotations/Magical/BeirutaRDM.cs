using System.ComponentModel;

namespace RotationSolver.ExtraRotations.Magical;

[Rotation("BeirutaRDM", CombatType.PvE, GameVersion = "7.4")]
[SourceCode(Path = "main/ExtraRotations/Magical/BeirutaRDM.cs")]
[ExtraRotation]
public sealed class BeirutaRDM : RedMageRotation
// This rotation is derived from the original Reborn Red Mage rotation and movement logic is inspired by RabbsBLM’s movement-rescue model,
// (RDM_Reborn) and retains its core decision-making for GCD flow,
// melee combo logic, and burst alignment.
// Key features:
// Balance standard opener
// Pot handling
// Movement handling
// Buff alignment for Prefulgence/Vice of Thorns
// Melee combo hold when out of range
// Prevent cap for mana pooling
// Prevent wasting Swift/Dual on short casts
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Use GCDs to heal. (Ignored if there are no healers alive in party)")]
    public bool GCDHeal { get; set; } = false;

    // Keep existing toggle for backwards compatibility:
    // - ON: use NEW 2-minute pooling planner logic (triple/double plan)
    // - OFF: use the ORIGINAL pooling behaviour (no changes to your old logic)
    [RotationConfig(CombatType.PvE, Name = "Pool Black and White Mana for double combo embolden")]
    public bool Pooling { get; set; } = true;

    // NEW: choose between Triple vs Double plan
    [RotationConfig(CombatType.PvE, Name = "2-minute pooling plan")]
    public TwoMinutePlan Plan2m { get; set; } = TwoMinutePlan.Triple;

    public enum TwoMinutePlan : byte
    {
        [Description("Triple combo (fallback to Double if fail)")] Triple,
        [Description("Double combo")] Double,
    }

    [RotationConfig(CombatType.PvE, Name = "Prevent healing during burst combos")]
    public bool PreventHeal { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Prevent raising during burst combos")]
    public bool PreventRaising { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Vercure for Dualcast when out of combat.")]
    public bool UseVercure { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Cast Reprise when moving with no instacast.")]
    public bool RangedSwordplay { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Only use Embolden if in Melee range.")]
    public bool AnyonesMeleeRule { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Swift/Acceleration for oGCD window alignment (Fleche/Contre drift fix)")]
    public bool UseWindowAlignment { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Hold melee combo up to 2s if out of range")]
    public bool HoldMeleeComboIfOutOfRange { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Delay Prefulgence/Vice of Thorns for buff alignment (about 3 gcd after Embolden)")]
    public bool DelayBuffOGCDs { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Opener/Burst open window (GCDs)")]
    [Range(1, 3, ConfigUnitType.None, 1)]
    public OpenWindowGcd OpenWindow { get; set; } = OpenWindowGcd.TwoGcd; // default = 2 GCD

    public enum OpenWindowGcd : byte
    {
        [Description("0 GCD (0.0s)")] ZeroGcd,
        [Description("1 GCD (2.5s)")] OneGcd,
        [Description("2 GCD (5.0s)")] TwoGcd,
    }

    #endregion

    private static BaseAction VeraeroPvEStartUp { get; } = new BaseAction(ActionID.VeraeroPvE, false);

    // Hold window end time for melee combo
    private long _meleeHoldUntilMs = 0;

    // Track when we actually used Embolden so we can delay buff oGCDs consistently.
    private long _emboldenUsedAtMs = 0;

    private static float EstimateRemainingSeconds(dynamic cooldown, float maxProbeSeconds, float stepSeconds = 0.5f)
    {
        if (cooldown.HasOneCharge) return 0f;

        for (float t = 0f; t <= maxProbeSeconds; t += stepSeconds)
        {
            if (cooldown.WillHaveOneCharge(t))
                return t;
        }

        return -1f;
    }

    // =========================
    // NEW 2-minute pooling planner constants (NOT configurable)
    // =========================
    private const float PoolStartBeforeEmbolden = 50f;   // start blocking melee starters/reprise
    private const float TripleCheckBeforeEmbolden = 15f; // triple checkpoint
    private const float DoubleCheckBeforeEmbolden = 5f;  // double checkpoint

    private const int TripleB = 73, TripleW = 73; // triple target
    private const int DoubleB = 42, DoubleW = 31; // double target (requires Manafication)

    // cap escape hatch: 82|91
    private const int CapLow = 82;
    private const int CapHigh = 91;

    private enum BurstPlanState
    {
        None,
        PoolingTriple,
        PoolingDouble,
        CommitTriple,
        CommitDouble,
        FailHold, // both failed -> hold gauge + hold Manafication until Embolden is ready
    }

    private BurstPlanState _planState = BurstPlanState.None;

    private const int TargetManaGap = 11;

    // ---------------------------------------------------------------------
    // NEW: If next GCD is a combo GCD or finisher GCD, block Swift usage.
    // This prevents Swift being spent right before/into melee/finishers.
    // ---------------------------------------------------------------------
    private static bool NextGcdIsComboOrFinisher(IAction nextGCD)
    {
        // "Combo" here refers to the RDM melee chain + AoE chain starters/steps,
        // and "finishers" refer to Verholy/Verflare/Scorch/Resolution.
        return nextGCD.IsTheSameTo(true,
            ActionID.RipostePvE, ActionID.ZwerchhauPvE, ActionID.RedoublementPvE,
            ActionID.MoulinetPvE, ActionID.ReprisePvE,
            ActionID.VerholyPvE, ActionID.VerflarePvE, ActionID.ScorchPvE, ActionID.ResolutionPvE);
    }

    // Change rule:
    // - If BOTH mana are < 42|31  OR  BOTH mana are >= 73|73  -> aim for an absolute gap of 11.
    // - If we are BETWEEN those bands (i.e. we have reached at least 42|31 but not yet 73|73 on both) -> aim to BALANCE.
    private bool TrySelectTwoAimingGap11(out IAction? act)
    {
        act = null;

        int diff = BlackMana - WhiteMana; // + => Black leads, - => White leads
        int gap = Math.Abs(diff);

        bool blackLeads = diff > 0;
        bool whiteLeads = diff < 0;

        if (!blackLeads && !whiteLeads)
            blackLeads = true; // default: make Black lead

        bool TryAero2(out IAction? a)
        {
            if (VeraeroIiiPvE.CanUse(out a, skipStatusProvideCheck: true)) return true;
            if (VeraeroPvE.CanUse(out a, skipStatusProvideCheck: true)) return true;
            a = null;
            return false;
        }

        bool TryThunder2(out IAction? a)
        {
            if (VerthunderIiiPvE.CanUse(out a, skipStatusProvideCheck: true)) return true;
            if (VerthunderPvE.CanUse(out a, skipStatusProvideCheck: true)) return true;
            a = null;
            return false;
        }

        bool belowDouble = (BlackMana < 42) || (WhiteMana < 31);
        bool atOrAboveTriple = (BlackMana >= 73) && (WhiteMana >= 73);
        bool betweenBands = !belowDouble && !atOrAboveTriple;

        // BETWEEN: balance (cast into the LOWER mana)
        if (betweenBands)
        {
            if (diff > 0)
            {
                if (TryAero2(out act)) return true;
                if (TryThunder2(out act)) return true;
                return false;
            }

            if (diff < 0)
            {
                if (TryThunder2(out act)) return true;
                if (TryAero2(out act)) return true;
                return false;
            }

            if (TryThunder2(out act)) return true;
            if (TryAero2(out act)) return true;
            return false;
        }

        // BELOW or ABOVE: aim for gap == 11 (original behaviour)
        if (gap > TargetManaGap)
        {
            if (blackLeads)
            {
                if (TryAero2(out act)) return true;
                if (TryThunder2(out act)) return true;
            }
            else
            {
                if (TryThunder2(out act)) return true;
                if (TryAero2(out act)) return true;
            }
            return false;
        }

        if (gap < TargetManaGap)
        {
            if (blackLeads)
            {
                if (TryThunder2(out act)) return true;
                if (TryAero2(out act)) return true;
            }
            else
            {
                if (TryAero2(out act)) return true;
                if (TryThunder2(out act)) return true;
            }
            return false;
        }

        if (blackLeads)
        {
            if (TryThunder2(out act)) return true;
            if (TryAero2(out act)) return true;
        }
        else
        {
            if (TryAero2(out act)) return true;
            if (TryThunder2(out act)) return true;
        }

        return false;
    }

    // Opener window = first 5 seconds of combat
    private float OpenWindowSeconds => OpenWindow switch
    {
        OpenWindowGcd.ZeroGcd => 0f,
        OpenWindowGcd.OneGcd => 2.2f,
        _ => 5.1f,
    };

    private bool IsOpen => InCombat && CombatTime < OpenWindowSeconds;
    private const float GrandImpactExtraDelaySeconds = 1.0f;

    private bool IsOpenForGrandImpact =>
        InCombat && CombatTime < (OpenWindowSeconds + GrandImpactExtraDelaySeconds);

    private bool TryContinueCurrentMeleeCombo(out IAction? act)
    {
        act = null;

        if (IsLastGCD(false, EnchantedMoulinetDeuxPvE))
            return EnchantedMoulinetTroisPvE.CanUse(out act);

        if (IsLastGCD(false, EnchantedMoulinetPvE))
            return EnchantedMoulinetDeuxPvE.CanUse(out act);

        if (IsLastGCD(true, EnchantedZwerchhauPvE_45961) || IsLastGCD(true, EnchantedZwerchhauPvE))
        {
            if (EnchantedRedoublementPvE_45962.CanUse(out act)) return true;
            if (EnchantedRedoublementPvE.CanUse(out act)) return true;
            return false;
        }

        if (IsLastGCD(true, EnchantedRipostePvE_45960) || IsLastGCD(true, EnchantedRipostePvE))
        {
            if (EnchantedZwerchhauPvE_45961.CanUse(out act)) return true;
            if (EnchantedZwerchhauPvE.CanUse(out act)) return true;
            return false;
        }

        return false;
    }

    private bool InFinisherChain()
    {
        return
            ManaStacks == 3 ||
            IsLastGCD(ActionID.VerholyPvE, ActionID.VerflarePvE, ActionID.ScorchPvE) ||
            ScorchPvE.CanUse(out _) ||
            ResolutionPvE.CanUse(out _);
    }

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (remainTime < VeraeroPvEStartUp.Info.CastTime + CountDownAhead)
        {
            if (VeraeroPvEStartUp.CanUse(out IAction? act))
                return act;
        }

        if (HasAccelerate && remainTime < 0f)
            StatusHelper.StatusOff(StatusID.Acceleration);

        if (HasSwift && remainTime < 0f)
            StatusHelper.StatusOff(StatusID.Swiftcast);

        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    [RotationDesc(ActionID.CorpsacorpsPvE)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (CorpsacorpsPvE.CanUse(out act, usedUp: true))
            return true;

        return base.MoveForwardAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.DisplacementPvE)]
    protected override bool MoveBackAbility(IAction nextGCD, out IAction? act)
    {
        if (DisplacementPvE.CanUse(out act, usedUp: true))
            return true;

        return base.MoveBackAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.AddlePvE, ActionID.MagickBarrierPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (AddlePvE.CanUse(out act))
            return true;

        if (MagickBarrierPvE.CanUse(out act))
            return true;

        return base.DefenseAreaAbility(nextGCD, out act);
    }

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        bool AnyoneInMeleeRange = NumberOfHostilesInRangeOf(3) > 0;

        bool over92_81 =
            (BlackMana >= 92 && WhiteMana >= 81) ||
            (WhiteMana >= 92 && BlackMana >= 81);

        if (Pooling)
        {
            bool commit = _planState == BurstPlanState.CommitTriple || _planState == BurstPlanState.CommitDouble;
            bool holding = _planState == BurstPlanState.PoolingTriple || _planState == BurstPlanState.PoolingDouble || _planState == BurstPlanState.FailHold;

            bool blockManafication92_81InMelee = over92_81 && AnyoneInMeleeRange;

            if (commit && !IsInMeleeCombo && !blockManafication92_81InMelee)
            {
                if (InCombat && HasHostilesInMaxRange && ManaficationPvE.CanUse(out act))
                    return true;
            }

            if (!holding)
            {
                if (!blockManafication92_81InMelee
                    && !IsOpen
                    && (HasEmbolden || EmboldenPvE.Cooldown.HasOneCharge || (EmboldenPvE.Cooldown.WillHaveOneCharge(4f) && !IsInMeleeCombo)))
                {
                    if (InCombat && HasHostilesInMaxRange && ManaficationPvE.CanUse(out act))
                        return true;
                }
            }
        }
        else
        {
            if (!(over92_81 && AnyoneInMeleeRange)
                && !IsOpen
                && (HasEmbolden || EmboldenPvE.Cooldown.HasOneCharge || (EmboldenPvE.Cooldown.WillHaveOneCharge(4f) && !IsInMeleeCombo)))
            {
                if (InCombat && HasHostilesInMaxRange && ManaficationPvE.CanUse(out act))
                    return true;
            }
        }

        if (!AnyonesMeleeRule)
        {
            if (!IsOpen && IsBurst && InCombat && HasHostilesInRange && EmboldenPvE.CanUse(out act))
            {
                _emboldenUsedAtMs = Environment.TickCount64;
                return true;
            }
        }
        else
        {
            if (!IsOpen && IsBurst && InCombat && AnyoneInMeleeRange && EmboldenPvE.CanUse(out act))
            {
                _emboldenUsedAtMs = Environment.TickCount64;
                return true;
            }
        }

        if (UseWindowAlignment)
        {
            long nowMsAlign = Environment.TickCount64;

            bool emboldenSoonAlign =
                EmboldenPvE.EnoughLevel
                && !HasEmbolden
                && EmboldenPvE.Cooldown.WillHaveOneCharge(10f);

            bool burstPrepHoldAccelAlign =
                emboldenSoonAlign
                && ManaStacks == 0
                && BlackMana >= 50
                && WhiteMana >= 50
                && !IsInMeleeCombo;

            const long accelLockAfterEmboldenMsAlign = 5000;
            bool inFirst5sAfterEmboldenAlign =
                _emboldenUsedAtMs != 0
                && (nowMsAlign - _emboldenUsedAtMs) < accelLockAfterEmboldenMsAlign;

            bool blockAccelAlign = burstPrepHoldAccelAlign || inFirst5sAfterEmboldenAlign;

            bool nextIsInstant =
                HasDualcast || HasSwift || HasAccelerate || (!IsOpenForGrandImpact && CanGrandImpact);

            bool finisherChain =
                ManaStacks == 3 ||
                IsLastGCD(ActionID.VerholyPvE, ActionID.VerflarePvE, ActionID.ScorchPvE) ||
                ScorchPvE.CanUse(out _) ||
                ResolutionPvE.CanUse(out _);

            bool meleeStepComing = nextGCD.IsTheSameTo(true,
                ActionID.RipostePvE, ActionID.ZwerchhauPvE, ActionID.RedoublementPvE,
                ActionID.MoulinetPvE, ActionID.ReprisePvE);

            bool allowAlignmentFix =
                InCombat
                && (HasHostilesInRange || HasHostilesInMaxRange)
                && !IsInMeleeCombo
                && !finisherChain
                && !meleeStepComing;

            if (allowAlignmentFix && !nextIsInstant)
            {
                float flecheRem = EstimateRemainingSeconds(FlechePvE.Cooldown, 25f, 0.5f);
                float contreRem = EstimateRemainingSeconds(ContreSixtePvE.Cooldown, 35f, 0.5f);

                const float soon = 3.0f;

                bool flecheReadyOrSoon = (flecheRem >= 0f && flecheRem <= soon);
                bool contreReadyOrSoon = (contreRem >= 0f && contreRem <= soon);

                if (flecheReadyOrSoon || contreReadyOrSoon)
                {
                    if (AccelerationPvE.EnoughLevel
                        && !blockAccelAlign
                        && !HasAccelerate
                        && !CanGrandImpact
                        && AccelerationPvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
                        return true;

                    if (!HasSwift
                        && SwiftcastPvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
                        return true;
                }
            }
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        bool Meleecheck = nextGCD.IsTheSameTo(true,
            ActionID.RipostePvE, ActionID.ZwerchhauPvE, ActionID.RedoublementPvE,
            ActionID.MoulinetPvE, ActionID.ReprisePvE);

        act = null;

        bool finisherChain =
            ManaStacks == 3 ||
            IsLastGCD(ActionID.VerholyPvE, ActionID.VerflarePvE, ActionID.ScorchPvE) ||
            ScorchPvE.CanUse(out _) ||
            ResolutionPvE.CanUse(out _);

        // ---------------------------------------------------------------------
        // CHANGE: block Swift not only during melee/finishers window,
        // but also if the *next GCD* is any combo/finisher GCD.
        // ---------------------------------------------------------------------
        bool blockSwift = IsInMeleeCombo || finisherChain || NextGcdIsComboOrFinisher(nextGCD);

        long nowMs = Environment.TickCount64;

        bool emboldenSoon =
            EmboldenPvE.EnoughLevel
            && !HasEmbolden
            && EmboldenPvE.Cooldown.WillHaveOneCharge(10f);

        bool burstPrepHoldAccel =
            emboldenSoon
            && ManaStacks == 0
            && BlackMana >= 50
            && WhiteMana >= 50
            && !IsInMeleeCombo;

        const long accelLockAfterEmboldenMs = 5000;
        bool inFirst5sAfterEmbolden =
            _emboldenUsedAtMs != 0
            && (nowMs - _emboldenUsedAtMs) < accelLockAfterEmboldenMs;

        bool blockAccel = burstPrepHoldAccel || inFirst5sAfterEmbolden;

        bool nextIsInstant =
            HasDualcast || HasSwift || HasAccelerate || (!IsOpenForGrandImpact && CanGrandImpact);

        bool openerNeedsInstant = IsOpen && !nextIsInstant;

        bool needsMovementRescue =
            InCombat
            && HasHostilesInMaxRange
            && (IsMoving || openerNeedsInstant)
            && !nextIsInstant;

        if (needsMovementRescue)
        {
            if (!Meleecheck && !IsInMeleeCombo)
            {
                if (IsOpen || NextAbilityToNextGCD < 0.6f)
                {
                    if (IsOpen)
                    {
                        if (!blockSwift && SwiftcastPvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
                            return true;

                        if (InCombat && UseBurstMedicine(out act))
                            return true;

                        if (FlechePvE.CanUse(out act))
                            return true;

                        if (AccelerationPvE.EnoughLevel
                            && !blockAccel
                            && !HasSwift
                            && !CanGrandImpact
                            && AccelerationPvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
                            return true;
                    }
                    else
                    {
                        if (AccelerationPvE.EnoughLevel
                            && !blockAccel
                            && !HasSwift
                            && !CanGrandImpact
                            && AccelerationPvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
                            return true;

                        if (!blockSwift && SwiftcastPvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
                            return true;
                    }
                }
            }
        }

        if (!needsMovementRescue && AccelerationPvE.EnoughLevel && !Meleecheck && !blockAccel)
        {
            if (!CanGrandImpact && InCombat && HasHostilesInMaxRange)
            {
                if (!EnhancedAccelerationTrait.EnoughLevel)
                {
                    if (HasEmbolden || !EmboldenPvE.EnoughLevel)
                    {
                        if (AccelerationPvE.CanUse(out act))
                            return true;
                    }
                }

                if (EnhancedAccelerationTrait.EnoughLevel && !EnhancedAccelerationIiTrait.EnoughLevel)
                {
                    if (AccelerationPvE.CanUse(out act,
                            usedUp: HasEmbolden || !EmboldenPvE.EnoughLevel || AccelerationPvE.Cooldown.WillHaveXChargesGCD(2, 1)))
                        return true;
                }

                if (EnhancedAccelerationIiTrait.EnoughLevel)
                {
                    if (AccelerationPvE.CanUse(out act,
                            usedUp: HasEmbolden || !EmboldenPvE.EnoughLevel || AccelerationPvE.Cooldown.WillHaveXChargesGCD(2, 1)))
                        return true;
                }
            }
        }

        bool swiftHardGate =
            InCombat
            && (HasHostilesInMaxRange || HasHostilesInRange)
            && ManaStacks != 3;

        if (swiftHardGate
            && !needsMovementRescue
            && !blockSwift
            && !HasSwift
            && (HasEmbolden || (EmboldenPvE.EnoughLevel && !EmboldenPvE.Cooldown.WillHaveOneCharge(30)) || !EmboldenPvE.EnoughLevel))
        {
            if (!HasAccelerate && !HasDualcast && !Meleecheck && !CanVerBoth)
            {
                if (!CanVerFire && !CanVerStone && IsLastGCD(false, VerthunderPvE, VerthunderIiiPvE, VeraeroPvE, VeraeroIiiPvE))
                {
                    if (SwiftcastPvE.CanUse(out act))
                        return true;
                }

                if (!CanVerStone && nextGCD.IsTheSameTo(false, VeraeroPvE, VeraeroIiiPvE))
                {
                    if (SwiftcastPvE.CanUse(out act))
                        return true;
                }

                if (!CanVerFire && nextGCD.IsTheSameTo(false, VerthunderPvE, VerthunderIiiPvE))
                {
                    if (SwiftcastPvE.CanUse(out act))
                        return true;
                }
            }
        }

        if (FlechePvE.CanUse(out act))
            return true;

        if (!IsOpenForGrandImpact && ContreSixtePvE.CanUse(out act))
            return true;

        const long delayMs = 5000;

        bool emboldenDelayOK =
            !DelayBuffOGCDs ||
            (_emboldenUsedAtMs == 0) ||
            (Environment.TickCount64 - _emboldenUsedAtMs >= delayMs);

        if (!DelayBuffOGCDs)
        {
            if ((HasEmbolden || StatusHelper.PlayerWillStatusEndGCD(1, 0, true, StatusID.PrefulgenceReady))
                && PrefulgencePvE.CanUse(out act))
            {
                return true;
            }
        }
        else
        {
            if (HasEmbolden
                && (emboldenDelayOK || StatusHelper.PlayerWillStatusEndGCD(1, 0, true, StatusID.PrefulgenceReady))
                && PrefulgencePvE.CanUse(out act))
            {
                return true;
            }
        }

        if (!DelayBuffOGCDs)
        {
            if (ViceOfThornsPvE.CanUse(out act))
                return true;
        }
        else
        {
            if (HasEmbolden && emboldenDelayOK && ViceOfThornsPvE.CanUse(out act))
                return true;
        }

        if (InCombat && !IsOpen)
        {
            if (EngagementPvE.CanUse(out act,
                    usedUp: HasEmbolden || !EmboldenPvE.EnoughLevel || EngagementPvE.Cooldown.WillHaveXChargesGCD(2, 1)))
            {
                return true;
            }

            if (!IsMoving && CorpsacorpsPvE.CanUse(out act,
                    usedUp: HasEmbolden || !EmboldenPvE.EnoughLevel || CorpsacorpsPvE.Cooldown.WillHaveXChargesGCD(2, 1)))
            {
                return true;
            }
        }

        return base.AttackAbility(nextGCD, out act);
    }

    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        if (HasEmbolden && InCombat && UseBurstMedicine(out act))
            return true;

        return base.GeneralAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    [RotationDesc(ActionID.VercurePvE)]
    protected override bool HealSingleGCD(out IAction? act)
    {
        if (PreventHeal)
        {
            if (HasManafication || HasEmbolden || ManaStacks == 3 || CanMagickedSwordplay || CanGrandImpact
                || ScorchPvE.CanUse(out _) || ResolutionPvE.CanUse(out _)
                || IsLastComboAction(ActionID.RipostePvE, ActionID.ZwerchhauPvE))
            {
                return base.HealSingleGCD(out act);
            }
        }

        if (VercurePvE.CanUse(out act, skipStatusProvideCheck: true))
            return true;

        return base.HealSingleGCD(out act);
    }

    [RotationDesc(ActionID.VerraisePvE)]
    protected override bool RaiseGCD(out IAction? act)
    {
        if (PreventRaising)
        {
            if (HasManafication || HasEmbolden || ManaStacks == 3 || CanMagickedSwordplay || CanGrandImpact
                || ScorchPvE.CanUse(out _) || ResolutionPvE.CanUse(out _)
                || IsLastComboAction(ActionID.RipostePvE, ActionID.ZwerchhauPvE))
            {
                return base.RaiseGCD(out act);
            }
        }

        if (VerraisePvE.CanUse(out act))
            return true;

        return base.RaiseGCD(out act);
    }

    protected override bool GeneralGCD(out IAction? act)
    {
        bool hasInstantBuffToSpend = HasDualcast || HasSwift || (IsOpen && HasAccelerate);

        // =========================
        // NEW 2-minute pooling planner (only active when Pooling toggle ON)
        // =========================
        float embRem = -1f;
        if (EmboldenPvE.EnoughLevel)
            embRem = EstimateRemainingSeconds(EmboldenPvE.Cooldown, 60f, 0.5f);

        bool embTimeKnown = embRem >= 0f;
        bool inPoolingWindow = Pooling && InCombat && EmboldenPvE.EnoughLevel && embTimeKnown && embRem > 0f && embRem <= PoolStartBeforeEmbolden;

        bool poolCapReached82_91 =
            (BlackMana >= CapHigh && WhiteMana >= CapLow) ||
            (WhiteMana >= CapHigh && BlackMana >= CapLow);

        bool inMeleeRange3 = NumberOfHostilesInRangeOf(3) > 0;

        bool manaficationReady =
            ManaficationPvE.CanUse(out _, skipCastingCheck: true);

        bool tripleManaReady = BlackMana >= TripleB && WhiteMana >= TripleW;
        bool doubleManaReady = BlackMana >= DoubleB && WhiteMana >= DoubleW;

        if (!Pooling || !InCombat || !EmboldenPvE.EnoughLevel || !embTimeKnown)
        {
            _planState = BurstPlanState.None;
        }
        else
        {
            if (embRem == 0f)
            {
                _planState = BurstPlanState.None;
            }
            else
            {
                if (inPoolingWindow && _planState == BurstPlanState.None)
                    _planState = (Plan2m == TwoMinutePlan.Triple) ? BurstPlanState.PoolingTriple : BurstPlanState.PoolingDouble;

                if (Plan2m == TwoMinutePlan.Triple
                    && _planState == BurstPlanState.PoolingTriple
                    && embRem <= TripleCheckBeforeEmbolden)
                {
                    bool tripleFeasible = tripleManaReady && (manaficationReady || inMeleeRange3);
                    _planState = tripleFeasible ? BurstPlanState.CommitTriple : BurstPlanState.PoolingDouble;
                }

                if (_planState == BurstPlanState.PoolingDouble
                    && embRem <= DoubleCheckBeforeEmbolden)
                {
                    bool doubleFeasible = doubleManaReady && manaficationReady;
                    _planState = doubleFeasible ? BurstPlanState.CommitDouble : BurstPlanState.FailHold;
                }

                if (HasEmbolden && _planState != BurstPlanState.None)
                    _planState = BurstPlanState.None;
            }
        }

        bool commitPlan = _planState == BurstPlanState.CommitTriple || _planState == BurstPlanState.CommitDouble;
        bool holdingPlan = _planState == BurstPlanState.PoolingTriple || _planState == BurstPlanState.PoolingDouble || _planState == BurstPlanState.FailHold;

        bool blockMeleeStartersAndReprise =
            inPoolingWindow
            && holdingPlan
            && !poolCapReached82_91;

        // ---------------------------
        // Opener: first 5s of combat (unchanged)
        // ---------------------------
        if (IsOpen
            && !IsInMeleeCombo
            && ManaStacks != 3
            && InCombat
            && HasHostilesInMaxRange)
        {
            bool hasInstant = HasDualcast || HasSwift || HasAccelerate;
            if (hasInstant)
            {
                int targets = NumberOfHostilesInRangeOf(5);
                int impactThreshold = HasAccelerate ? 2 : 3;

                if (targets >= impactThreshold && ImpactPvE.CanUse(out act))
                    return true;

                if (VerthunderIiiPvE.CanUse(out act)) return true;
                if (VerthunderPvE.CanUse(out act)) return true;
            }
        }

        if (ManaStacks == 3)
        {
            int diff = BlackMana - WhiteMana;
            int gap = Math.Abs(diff);

            bool forceBalance = HasEmbolden || gap >= 19;

            if (forceBalance)
            {
                if (diff > 0 && VerholyPvE.CanUse(out act)) return true;
                if (diff < 0 && VerflarePvE.CanUse(out act)) return true;
            }
            else
            {
                if (CanVerFire && VerholyPvE.CanUse(out act)) return true;
                if (CanVerStone && VerflarePvE.CanUse(out act)) return true;
            }

            if (diff > 0 && VerholyPvE.CanUse(out act)) return true;
            if (diff < 0 && VerflarePvE.CanUse(out act)) return true;

            if (CanVerFire && !CanVerStone && VerholyPvE.CanUse(out act)) return true;
            if (CanVerStone && !CanVerFire && VerflarePvE.CanUse(out act)) return true;

            if (VerholyPvE.CanUse(out act)) return true;
            if (VerflarePvE.CanUse(out act)) return true;
        }

        if (CanInstantCast && !CanVerEither)
        {
            if (ScatterPvE.CanUse(out act))
                return true;

            if (TrySelectTwoAimingGap11(out act))
                return true;
        }

        if (IsLastGCD(ActionID.ScorchPvE))
        {
            if (ResolutionPvE.CanUse(out act, skipStatusProvideCheck: true))
                return true;
        }

        if (IsLastGCD(ActionID.VerholyPvE, ActionID.VerflarePvE))
        {
            if (ScorchPvE.CanUse(out act, skipStatusProvideCheck: true))
                return true;
        }

        if (HoldMeleeComboIfOutOfRange)
        {
            if (IsInMeleeCombo)
            {
                if (TryContinueCurrentMeleeCombo(out act))
                {
                    _meleeHoldUntilMs = 0;
                    return true;
                }

                long now = Environment.TickCount64;
                if (_meleeHoldUntilMs == 0)
                    _meleeHoldUntilMs = now + 2000;

                if (now < _meleeHoldUntilMs)
                {
                    act = null;
                    return false;
                }

                _meleeHoldUntilMs = 0;
            }
            else
            {
                _meleeHoldUntilMs = 0;
            }
        }
        else
        {
            _meleeHoldUntilMs = 0;
        }

        if (IsLastGCD(false, EnchantedMoulinetDeuxPvE) && EnchantedMoulinetTroisPvE.CanUse(out act))
            return true;

        if (IsLastGCD(false, EnchantedMoulinetPvE) && EnchantedMoulinetDeuxPvE.CanUse(out act))
            return true;

        if (EnchantedRedoublementPvE_45962.CanUse(out act))
            return true;

        if (EnchantedRedoublementPvE.CanUse(out act))
            return true;

        if (EnchantedZwerchhauPvE_45961.CanUse(out act))
            return true;

        if (EnchantedZwerchhauPvE.CanUse(out act))
            return true;

        // =========================
        // ORIGINAL pooling logic (kept when Pooling toggle OFF)
        // =========================
        if (!Pooling)
        {
            bool poolCapReached =
                (BlackMana >= 92 && WhiteMana >= 81) ||
                (WhiteMana >= 92 && BlackMana >= 81);

            bool EnoughMana =
                (!Pooling && EnoughManaComboNoPooling) ||
                (Pooling && (poolCapReached || EnoughManaComboPooling));

            if (EnoughMana && !InFinisherChain())
            {
                bool burstStartOK =
                    !IsOpen &&
                    (
                        poolCapReached ||
                        HasManafication ||
                        StatusHelper.PlayerWillStatusEndGCD(4, 0, true, StatusID.MagickedSwordplay) ||
                        (HasEmbolden && CanMagickedSwordplay)
                    );

                if (NumberOfHostilesInRangeOf(5) >= 3)
                {
                    if (!IsLastGCD(false, EnchantedMoulinetPvE)
                        && EnchantedMoulinetPvE.CanUse(out act))
                        return true;
                }

                if (burstStartOK && !IsLastRiposteStarter() && TryRiposteStarter(out act))
                    return true;
            }
        }
        else
        {
            bool startersBlocked = blockMeleeStartersAndReprise;

            bool burstStartOK_old =
                !IsOpen &&
                (
                    poolCapReached82_91 ||
                    HasManafication ||
                    StatusHelper.PlayerWillStatusEndGCD(4, 0, true, StatusID.MagickedSwordplay) ||
                    (HasEmbolden && CanMagickedSwordplay)
                );

            bool burstStartOK = commitPlan || burstStartOK_old;

            bool enoughToStart =
                commitPlan ||
                EnoughManaComboPooling ||
                EnoughManaComboNoPooling;

            if (!startersBlocked && enoughToStart && !InFinisherChain())
            {
                if (NumberOfHostilesInRangeOf(5) >= 3)
                {
                    if (!IsLastGCD(false, EnchantedMoulinetPvE)
                        && EnchantedMoulinetPvE.CanUse(out act))
                        return true;
                }

                if (burstStartOK && !IsLastRiposteStarter() && TryRiposteStarter(out act))
                    return true;
            }
        }

        if (!IsOpenForGrandImpact && GrandImpactPvE.CanUse(out act, skipStatusProvideCheck: CanGrandImpact, skipCastingCheck: true))
            return true;

        if (!IsInMeleeCombo
            && ManaStacks != 3
            && InCombat
            && HasHostilesInMaxRange
            && CanVerBoth
            && !IsMoving
            && !hasInstantBuffToSpend)
        {
            switch (VerEndsFirst)
            {
                case "VerFire":
                    if (VerfirePvE.CanUse(out act)) return true;
                    if (VerstonePvE.CanUse(out act)) return true;
                    break;

                case "VerStone":
                    if (VerstonePvE.CanUse(out act)) return true;
                    if (VerfirePvE.CanUse(out act)) return true;
                    break;

                case "Equal":
                default:
                    if (WhiteMana < BlackMana)
                    {
                        if (VerstonePvE.CanUse(out act)) return true;
                        if (VerfirePvE.CanUse(out act)) return true;
                    }
                    else
                    {
                        if (VerfirePvE.CanUse(out act)) return true;
                        if (VerstonePvE.CanUse(out act)) return true;
                    }
                    break;
            }
        }

        bool finisherChain2 =
            ManaStacks == 3 ||
            IsLastGCD(ActionID.VerholyPvE, ActionID.VerflarePvE, ActionID.ScorchPvE) ||
            ScorchPvE.CanUse(out _) ||
            ResolutionPvE.CanUse(out _);

        bool canRepriseNow2 =
            RangedSwordplay
            && ManaStacks == 0
            && (BlackMana < 50 || WhiteMana < 50)
            && EnchantedReprisePvE.CanUse(out _);

        bool noOtherMoveResources2 =
            !CanGrandImpact
            && !HasSwift
            && !HasDualcast
            && !canRepriseNow2
            && !IsInMeleeCombo
            && !finisherChain2
            && ManaStacks != 3;

        bool shouldSpendAccelOn2Soon2 =
            HasAccelerate
            && InCombat
            && HasHostilesInMaxRange
            && !IsInMeleeCombo
            && !finisherChain2
            && ManaStacks != 3
            && (
                !CanVerBoth
                || (IsMoving && CanVerBoth && noOtherMoveResources2)
            );

        if (shouldSpendAccelOn2Soon2)
        {
            if (NumberOfHostilesInRangeOf(5) >= 2 && ImpactPvE.CanUse(out act))
                return true;

            if (TrySelectTwoAimingGap11(out act))
                return true;
        }

        if (ManaStacks == 3)
            return base.GeneralGCD(out act);

        if (!IsInMeleeCombo
            && ManaStacks != 3
            && HasAccelerate
            && !HasSwift
            && !HasDualcast
            && InCombat
            && HasHostilesInMaxRange
            && IsMoving)
        {
            int aoeTargets = 2;
            if (NumberOfHostilesInRangeOf(5) >= aoeTargets && ImpactPvE.CanUse(out act))
                return true;

            if (TrySelectTwoAimingGap11(out act))
                return true;
        }

        long nowMsAccel = Environment.TickCount64;

        bool emboldenSoonAccel =
            EmboldenPvE.EnoughLevel
            && !HasEmbolden
            && EmboldenPvE.Cooldown.WillHaveOneCharge(10f);

        bool burstPrepHoldAccelGcd =
            emboldenSoonAccel
            && ManaStacks == 0
            && BlackMana >= 50
            && WhiteMana >= 50
            && !IsInMeleeCombo;

        const long accelLockAfterEmboldenMsGcd = 10000;
        bool inFirst5sAfterEmboldenGcd =
            _emboldenUsedAtMs != 0
            && (nowMsAccel - _emboldenUsedAtMs) < accelLockAfterEmboldenMsGcd;

        bool blockAccelGcd = burstPrepHoldAccelGcd || inFirst5sAfterEmboldenGcd;

        bool canRescueMovementWithOgcd =
            InCombat
            && HasHostilesInMaxRange
            && IsMoving
            && NextAbilityToNextGCD < 0.6f
            && !IsInMeleeCombo
            && ManaStacks != 3
            && (
                (AccelerationPvE.EnoughLevel
                    && !blockAccelGcd
                    && !CanGrandImpact
                    && AccelerationPvE.CanUse(out _, usedUp: true, skipCastingCheck: true))
                || SwiftcastPvE.CanUse(out _, usedUp: true, skipCastingCheck: true)
            );

        bool hasInstantTools = HasSwift || HasDualcast || HasAccelerate || (!IsOpenForGrandImpact && CanGrandImpact);

        if (!blockMeleeStartersAndReprise
            && IsMoving
            && RangedSwordplay
            && ManaStacks == 0
            && (BlackMana < 50 || WhiteMana < 50)
            && !hasInstantTools
            && !canRescueMovementWithOgcd
            && EnchantedReprisePvE.CanUse(out act))
        {
            return true;
        }

        if (IsMoving
            && InCombat
            && HasHostilesInMaxRange
            && ManaStacks != 3
            && !hasInstantTools)
        {
            act = null;
            return false;
        }

        if (!IsInMeleeCombo
            && ManaStacks != 3
            && InCombat
            && (HasHostilesInRange || HasHostilesInMaxRange)
            && hasInstantBuffToSpend)
        {
            if (NumberOfHostilesInRangeOf(5) >= 3 && ImpactPvE.CanUse(out act))
                return true;

            if (TrySelectTwoAimingGap11(out act))
                return true;
        }

        if (VerstonePvE.EnoughLevel && !hasInstantBuffToSpend)
        {
            if (CanVerBoth)
            {
                switch (VerEndsFirst)
                {
                    case "VerFire":
                        if (VerfirePvE.CanUse(out act)) return true;
                        break;
                    case "VerStone":
                        if (VerstonePvE.CanUse(out act)) return true;
                        break;
                    case "Equal":
                        if (WhiteMana < BlackMana)
                        {
                            if (VerstonePvE.CanUse(out act)) return true;
                        }
                        else
                        {
                            if (VerfirePvE.CanUse(out act)) return true;
                        }
                        break;
                }
            }
            else
            {
                if (VerfirePvE.CanUse(out act)) return true;
                if (VerstonePvE.CanUse(out act)) return true;
            }
        }

        if (!VerstonePvE.EnoughLevel && !hasInstantBuffToSpend && VerfirePvE.CanUse(out act))
            return true;

        if (!CanInstantCast && !CanVerEither)
        {
            if (NumberOfHostilesInRangeOf(5) >= 3)
            {
                if (WhiteMana < BlackMana)
                {
                    if (VeraeroIiPvE.CanUse(out act)) return true;
                    if (VerthunderIiPvE.CanUse(out act)) return true;
                }
                else
                {
                    if (VerthunderIiPvE.CanUse(out act)) return true;
                    if (VeraeroIiPvE.CanUse(out act)) return true;
                }
            }

            if (!hasInstantBuffToSpend && JoltPvE.CanUse(out act))
                return true;
        }

        if (UseVercure && !InCombat && VercurePvE.CanUse(out act))
            return true;

        return base.GeneralGCD(out act);
    }
    #endregion

    // Treat both Riposte variants as the same "starter".
    private bool IsLastRiposteStarter()
    {
        return IsLastGCD(true, EnchantedRipostePvE_45960) || IsLastGCD(true, EnchantedRipostePvE);
    }

    private bool TryRiposteStarter(out IAction? act)
    {
        // ---------------------------------------------------------------------
        // FIX 1: Prioritise normal EnchantedRiposte as the true combo starter.
        // The _45960 variant has been observed to break combo tracking; therefore:
        // - If normal Riposte is usable, ALWAYS use it first.
        // - Only fall back to _45960 if normal cannot be used (typically out of range)
        //   AND Manafication is active.
        //
        // FIX 2: Keep your existing rule: during CommitTriple without Manafication,
        // do not allow _45960 at all.
        // ---------------------------------------------------------------------

        bool inMeleeRange3 = NumberOfHostilesInRangeOf(3) > 0;

        // CommitTriple without Manafication: force true melee only.
        if (Pooling && _planState == BurstPlanState.CommitTriple && !HasManafication)
        {
            if (EnchantedRipostePvE.CanUse(out act))
                return true;

            act = null;
            return false;
        }

        // Always prefer the NORMAL starter first (prevents _45960 breaking combo).
        if (EnchantedRipostePvE.CanUse(out act))
            return true;

        // Only if normal is not usable, consider _45960 (typically range).
        // Also: if we are already in melee range, do NOT use _45960.
        if (!inMeleeRange3 && HasManafication && EnchantedRipostePvE_45960.CanUse(out act))
            return true;

        act = null;
        return false;
    }

    public override bool CanHealSingleSpell
    {
        get
        {
            int aliveHealerCount = 0;
            IEnumerable<IBattleChara> healers = PartyMembers.GetJobCategory(JobRole.Healer);
            foreach (IBattleChara h in healers)
            {
                if (!h.IsDead)
                    aliveHealerCount++;
            }

            return base.CanHealSingleSpell && (GCDHeal || aliveHealerCount == 0);
        }
    }
}