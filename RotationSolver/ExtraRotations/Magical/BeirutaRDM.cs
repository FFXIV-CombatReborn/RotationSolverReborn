using System.ComponentModel;

namespace RotationSolver.ExtraRotations.Magical;

[Rotation("BeirutaRDM", CombatType.PvE, GameVersion = "7.45")]
[SourceCode(Path = "main/ExtraRotations/Magical/BeirutaRDM.cs")]
[ExtraRotation]
public sealed class BeirutaRDM : RedMageRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name =
        "Please note that this rotation is optimised for Lv100 high-end encounters.\n" +
        "• Recommend GCD for this rotation is 2.48 or 2.49 and 2.50\n" +
        "• It is designed for 123 Embolden 456 and 1E23456. However you may see 12E3456 in some cases\n" +
        "• Try to stay close to the target when Embolden will be ready in ~20s if you selected triple combo before embolden\n" +
        "• Attempts to pool 73|73 mana for triple melee combo\n" +
        "• Intentionally maintains an 11 mana gap in certain situations\n" +
        "• Manually use Reprise if you cannot start a combo at the end of combat\n" +
        "• Auto Burst on/off controls Embolden/Manafication, you may also want to add Enchanted Riposite to your delay macros\n")]
    public bool RotationNotes { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use GCDs to heal. (Ignored if there are no healers alive in party)")]
    public bool GCDHeal { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Pool Black and White Mana for burst Embolden")]
    public bool Pooling { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Try triple combo before embolden (You will need to get in melee range 17s before embolden is ready)")]
    public bool TryTripleCombo { get; set; } = false;

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

    [RotationConfig(CombatType.PvE, Name = "Use Swift/Acceleration for oGCD window alignment (Fleche/Contre drift fix, Experimental)")]
    public bool UseWindowAlignment { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Hold melee combo up to 2s if out of range")]
    public bool HoldMeleeComboIfOutOfRange { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Delay Prefulgence/Vice of Thorns for buff alignment (about 3 gcd after Embolden)")]
    public bool DelayBuffOGCDs { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Opener/Burst open window (GCDs)")]
    [Range(1, 3, ConfigUnitType.None, 1)]
    public OpenWindowGcd OpenWindow { get; set; } = OpenWindowGcd.TwoGcd;

    public enum OpenWindowGcd : byte
    {
        [Description("0 GCD (0.0s)")] ZeroGcd,
        [Description("1 GCD (2.5s)")] OneGcd,
        [Description("2 GCD (5.0s)")] TwoGcd,
    }
    #endregion

    #region Static actions / constants
    private static BaseAction VeraeroPvEStartUp { get; } = new BaseAction(ActionID.VeraeroPvE, false);
    private static BaseAction VerthunderPvEStartUp { get; } = new BaseAction(ActionID.VerthunderPvE, false);

    private const long BlockManaficationAfterMeleeStarterMs = 4000;
    private const long HoldMeleeComboMs = 2000;
    private const long BuffOgcdDelayMs = 5000;
    private const long AccelLockAfterEmboldenMs = 5000;

    private const float PoolStartBeforeEmbolden = 50f;
    private const float TripleDecisionStart = 17f;
    private const float UnlockAt = 5f;

    private const int TripleB = 73;
    private const int TripleW = 73;
    private const int DoubleB = 42;
    private const int DoubleW = 31;
    private const int PoolCapLow = 82;
    private const int PoolCapHigh = 91;
    private const int DumpCapHigh = 92;
    private const int DumpCapLow = 81;
    private const int TargetManaGap = 11;

    private const float GrandImpactExtraDelaySeconds = 3.0f;
    #endregion

    #region Fields
    private long _meleeHoldUntilMs;
    private long _emboldenUsedAtMs;
    private long _meleeStarterUsedAtMs;

    private bool _meleeCommitLockActive;
    private bool _emboldenSeenDuringCommit;

    private bool _tripleComboReached;
    private MeleeComboTrack _activeMeleeTrack = MeleeComboTrack.None;
    #endregion

    #region Enums
    private enum MeleeComboTrack : byte
    {
        None = 0,
        SingleTarget = 1,
        AoE = 2,
    }
    #endregion

    #region Shared state helpers
    private bool InMeleeRange3 => NumberOfHostilesInRangeOf(3) > 0;
    private bool InCombatWithTarget => InCombat && (HasHostilesInRange || HasHostilesInMaxRange);

    private bool NearManaCap =>
        (BlackMana >= DumpCapHigh && WhiteMana >= DumpCapLow) ||
        (WhiteMana >= DumpCapHigh && BlackMana >= DumpCapLow);

    private bool NearPoolingCap =>
        (BlackMana >= PoolCapHigh && WhiteMana >= PoolCapLow) ||
        (WhiteMana >= PoolCapHigh && BlackMana >= PoolCapLow);

    private float OpenWindowSeconds => OpenWindow switch
    {
        OpenWindowGcd.ZeroGcd => 0f,
        OpenWindowGcd.OneGcd => 2.2f,
        _ => 5f,
    };

    private bool IsOpen => InCombat && CombatTime < OpenWindowSeconds;

    private bool IsOpenForGrandImpact =>
        InCombat && CombatTime < (OpenWindowSeconds + GrandImpactExtraDelaySeconds);

    private bool HasInstantBuffToSpend =>
        HasDualcast || HasSwift || (IsOpen && HasAccelerate);

    private bool HasAnyInstantTool =>
        HasSwift || HasDualcast || HasAccelerate || (!IsOpenForGrandImpact && CanGrandImpact);

    private bool InFinisherChain()
    {
        return
            ManaStacks == 3 ||
            IsLastGCD(ActionID.VerholyPvE, ActionID.VerflarePvE, ActionID.ScorchPvE) ||
            ScorchPvE.CanUse(out _) ||
            ResolutionPvE.CanUse(out _);
    }

    private bool IsAoEDesired() => NumberOfHostilesInRangeOf(5) >= 3;

    private bool IsLastSTComboStep()
    {
        return IsLastGCD(true,
            EnchantedRipostePvE, EnchantedRipostePvE_45960,
            EnchantedZwerchhauPvE, EnchantedZwerchhauPvE_45961,
            EnchantedRedoublementPvE, EnchantedRedoublementPvE_45962);
    }

    private bool IsLastAoEComboStep()
    {
        return IsLastGCD(false,
            EnchantedMoulinetPvE,
            EnchantedMoulinetDeuxPvE,
            EnchantedMoulinetTroisPvE);
    }

    private bool IsAnyMeleeComboInProgress()
    {
        return IsInMeleeCombo || IsLastSTComboStep() || IsLastAoEComboStep();
    }

    private void UpdateActiveMeleeTrack()
    {
        if (InFinisherChain() || ManaStacks == 3)
        {
            _activeMeleeTrack = MeleeComboTrack.None;
            return;
        }

        if (IsLastAoEComboStep())
        {
            _activeMeleeTrack = MeleeComboTrack.AoE;
            return;
        }

        if (IsLastSTComboStep())
        {
            _activeMeleeTrack = MeleeComboTrack.SingleTarget;
            return;
        }

        bool hasAoEContinuation =
            EnchantedMoulinetDeuxPvE.CanUse(out _) ||
            EnchantedMoulinetTroisPvE.CanUse(out _);

        if (hasAoEContinuation)
        {
            _activeMeleeTrack = MeleeComboTrack.AoE;
            return;
        }

        bool hasSTContinuation =
            EnchantedZwerchhauPvE.CanUse(out _) || EnchantedZwerchhauPvE_45961.CanUse(out _) ||
            EnchantedRedoublementPvE.CanUse(out _) || EnchantedRedoublementPvE_45962.CanUse(out _);

        if (hasSTContinuation)
        {
            _activeMeleeTrack = MeleeComboTrack.SingleTarget;
            return;
        }

        if (!IsAnyMeleeComboInProgress())
            _activeMeleeTrack = MeleeComboTrack.None;
    }

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

    private float EmboldenRem()
    {
        if (!EmboldenPvE.EnoughLevel) return -1f;
        return EstimateRemainingSeconds(EmboldenPvE.Cooldown, 60f, 0.5f);
    }

    private bool IsPoolingWindow(float embRem) =>
        Pooling && InCombat && EmboldenPvE.EnoughLevel && !HasEmbolden && embRem >= 0f && embRem <= PoolStartBeforeEmbolden;

    private void UpdateTripleComboReached(float embRem)
    {
        if (!IsPoolingWindow(embRem))
        {
            _tripleComboReached = false;
            return;
        }

        if (!_tripleComboReached && BlackMana >= TripleB && WhiteMana >= TripleW)
            _tripleComboReached = true;
    }

    private bool IsBlockingManaficationAfterMeleeStarter()
    {
        long now = Environment.TickCount64;
        return _meleeStarterUsedAtMs != 0
               && (now - _meleeStarterUsedAtMs) < BlockManaficationAfterMeleeStarterMs;
    }

    private void UpdateMeleeCommitLock()
    {
        if (!_meleeCommitLockActive)
        {
            _emboldenSeenDuringCommit = false;
            return;
        }

        if (HasEmbolden)
            _emboldenSeenDuringCommit = true;

        if (_emboldenSeenDuringCommit && !HasEmbolden)
        {
            _meleeCommitLockActive = false;
            _emboldenSeenDuringCommit = false;
        }
    }

    private bool ShouldGateMeleeStarterAndManafication(float embRem)
    {
        if (HasEmbolden) return false;
        if (_meleeCommitLockActive) return false;
        if (!IsPoolingWindow(embRem)) return false;

        if (embRem <= UnlockAt) return false;
        if (TryTripleCombo && embRem <= TripleDecisionStart && _tripleComboReached) return false;

        return true;
    }

    private bool IsMeleeCommitWindow(float embRem) =>
        IsPoolingWindow(embRem) && !ShouldGateMeleeStarterAndManafication(embRem);

    private static bool NextGcdIsBlockedForInstants(IAction nextGCD)
    {
        return nextGCD.IsTheSameTo(true,
            ActionID.EnchantedReprisePvE,
            ActionID.EnchantedRipostePvE, ActionID.EnchantedRipostePvE_45960,
            ActionID.EnchantedZwerchhauPvE, ActionID.EnchantedZwerchhauPvE_45961,
            ActionID.EnchantedRedoublementPvE, ActionID.EnchantedRedoublementPvE_45962,
            ActionID.EnchantedMoulinetPvE,
            ActionID.EnchantedMoulinetDeuxPvE,
            ActionID.EnchantedMoulinetTroisPvE,
            ActionID.VerholyPvE, ActionID.VerflarePvE,
            ActionID.ScorchPvE, ActionID.ResolutionPvE
        );
    }

    private static bool NextGcdIsAnyMeleeStep(IAction nextGCD)
    {
        return nextGCD.IsTheSameTo(true,
            ActionID.RipostePvE, ActionID.ZwerchhauPvE, ActionID.RedoublementPvE,
            ActionID.MoulinetPvE, ActionID.ReprisePvE,
            ActionID.EnchantedRipostePvE, ActionID.EnchantedRipostePvE_45960,
            ActionID.EnchantedZwerchhauPvE, ActionID.EnchantedZwerchhauPvE_45961,
            ActionID.EnchantedRedoublementPvE, ActionID.EnchantedRedoublementPvE_45962,
            ActionID.EnchantedMoulinetPvE,
            ActionID.EnchantedMoulinetDeuxPvE,
            ActionID.EnchantedMoulinetTroisPvE,
            ActionID.EnchantedReprisePvE
        );
    }

    private bool CanContinueTrackedMeleeCombo(out IAction? act)
    {
        act = null;

        switch (_activeMeleeTrack)
        {
            case MeleeComboTrack.AoE:
                if (IsLastGCD(false, EnchantedMoulinetDeuxPvE))
                    return EnchantedMoulinetTroisPvE.CanUse(out act);

                if (IsLastGCD(false, EnchantedMoulinetPvE))
                    return EnchantedMoulinetDeuxPvE.CanUse(out act);

                return false;

            case MeleeComboTrack.SingleTarget:
                if (IsLastGCD(true, EnchantedZwerchhauPvE_45961) || IsLastGCD(true, EnchantedZwerchhauPvE))
                    return EnchantedRedoublementPvE_45962.CanUse(out act) || EnchantedRedoublementPvE.CanUse(out act);

                if (IsLastGCD(true, EnchantedRipostePvE_45960) || IsLastGCD(true, EnchantedRipostePvE))
                    return EnchantedZwerchhauPvE_45961.CanUse(out act) || EnchantedZwerchhauPvE.CanUse(out act);

                return false;
        }

        return false;
    }

    private void RegisterMeleeStarter(MeleeComboTrack track, float embRem)
    {
        _activeMeleeTrack = track;
        _meleeStarterUsedAtMs = Environment.TickCount64;
        if (IsMeleeCommitWindow(embRem))
            _meleeCommitLockActive = true;
    }
    #endregion

    #region Spell selection helpers
    private bool TrySelectTwoAimingGap11(out IAction? act)
    {
        act = null;

        int diff = BlackMana - WhiteMana;
        int gap = Math.Abs(diff);
        bool blackLeads = diff >= 0;

        bool TryAero(out IAction? a)
        {
            if (VeraeroIiiPvE.CanUse(out a, skipStatusProvideCheck: true)) return true;
            if (VeraeroPvE.CanUse(out a, skipStatusProvideCheck: true)) return true;
            a = null;
            return false;
        }

        bool TryThunder(out IAction? a)
        {
            if (VerthunderIiiPvE.CanUse(out a, skipStatusProvideCheck: true)) return true;
            if (VerthunderPvE.CanUse(out a, skipStatusProvideCheck: true)) return true;
            a = null;
            return false;
        }

        bool belowDouble = BlackMana < DoubleB || WhiteMana < DoubleW;
        bool atOrAboveTriple = BlackMana >= TripleB && WhiteMana >= TripleW;
        bool betweenBands = !belowDouble && !atOrAboveTriple;

        if (betweenBands)
        {
            if (diff > 0) return TryAero(out act) || TryThunder(out act);
            if (diff < 0) return TryThunder(out act) || TryAero(out act);
            return TryThunder(out act) || TryAero(out act);
        }

        if (gap > TargetManaGap)
            return blackLeads ? (TryAero(out act) || TryThunder(out act))
                              : (TryThunder(out act) || TryAero(out act));

        if (gap < TargetManaGap)
            return blackLeads ? (TryThunder(out act) || TryAero(out act))
                              : (TryAero(out act) || TryThunder(out act));

        return blackLeads ? (TryThunder(out act) || TryAero(out act))
                          : (TryAero(out act) || TryThunder(out act));
    }

    private bool ShouldHighManaDumpWithEnchantedReprise()
    {
        float embRem = EmboldenRem();
        bool gateMelee = ShouldGateMeleeStarterAndManafication(embRem);

        return NearManaCap
               && !gateMelee
               && !InMeleeRange3
               && !CanMagickedSwordplay
               && !IsAnyMeleeComboInProgress()
               && !InFinisherChain()
               && ManaStacks == 0;
    }

    private bool CanHoldOrWaitForMeleeRange()
    {
        if (!HoldMeleeComboIfOutOfRange || InMeleeRange3)
        {
            _meleeHoldUntilMs = 0;
            return false;
        }

        long now = Environment.TickCount64;
        if (_meleeHoldUntilMs == 0)
            _meleeHoldUntilMs = now + HoldMeleeComboMs;

        if (now < _meleeHoldUntilMs)
            return true;

        _meleeHoldUntilMs = 0;
        return false;
    }

    private bool TryStartTrackedMeleeStarter(MeleeComboTrack desiredTrack, out IAction? act, float embRem)
    {
        act = null;

        UpdateMeleeCommitLock();

        if (ShouldGateMeleeStarterAndManafication(embRem))
            return false;

        if (CanHoldOrWaitForMeleeRange())
            return false;

        if (desiredTrack == MeleeComboTrack.AoE)
        {
            if (!HasSwift && !HasDualcast && EnchantedMoulinetPvE.CanUse(out act))
            {
                RegisterMeleeStarter(MeleeComboTrack.AoE, embRem);
                return true;
            }

            return false;
        }

        if (!HasSwift && !HasDualcast && EnchantedRipostePvE.CanUse(out act))
        {
            RegisterMeleeStarter(MeleeComboTrack.SingleTarget, embRem);
            return true;
        }

        if (!HasSwift && !HasDualcast && !InMeleeRange3 && HasManafication && EnchantedRipostePvE_45960.CanUse(out act))
        {
            RegisterMeleeStarter(MeleeComboTrack.SingleTarget, embRem);
            return true;
        }

        return false;
    }
    #endregion

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (HasDualcast && VerthunderPvEStartUp.CanUse(out IAction? act))
            return act;

        if (remainTime < VeraeroPvEStartUp.Info.CastTime + CountDownAhead
            && VeraeroPvEStartUp.CanUse(out act))
            return act;

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
        UpdateActiveMeleeTrack();
        UpdateMeleeCommitLock();

        float embRem = EmboldenRem();
        UpdateTripleComboReached(embRem);

        bool gateMelee = ShouldGateMeleeStarterAndManafication(embRem);
        bool blockManaficationNow = (NearManaCap && InMeleeRange3) || IsBlockingManaficationAfterMeleeStarter();

        if (!blockManaficationNow && !gateMelee)
        {
            bool canUseManaficationNormally =
                !IsOpen &&
                (HasEmbolden || EmboldenPvE.Cooldown.HasOneCharge || (EmboldenPvE.Cooldown.WillHaveOneCharge(5f) && !IsAnyMeleeComboInProgress()));

            if (canUseManaficationNormally && InCombat && IsBurst && HasHostilesInMaxRange && ManaficationPvE.CanUse(out act))
                return true;
        }

        bool emboldenAllowed = !IsOpen && IsBurst && InCombat && (AnyonesMeleeRule ? InMeleeRange3 : HasHostilesInRange);
        if (emboldenAllowed && EmboldenPvE.CanUse(out act))
        {
            _emboldenUsedAtMs = Environment.TickCount64;
            return true;
        }

        if (UseWindowAlignment)
        {
            long now = Environment.TickCount64;

            bool emboldenSoon = EmboldenPvE.EnoughLevel && !HasEmbolden && EmboldenPvE.Cooldown.WillHaveOneCharge(10f);
            bool burstPrepHoldAccel = emboldenSoon && ManaStacks == 0 && BlackMana >= 50 && WhiteMana >= 50 && !IsAnyMeleeComboInProgress();
            bool inFirst5sAfterEmbolden = _emboldenUsedAtMs != 0 && (now - _emboldenUsedAtMs) < AccelLockAfterEmboldenMs;
            bool blockAccel = burstPrepHoldAccel || inFirst5sAfterEmbolden;

            bool nextIsInstant = HasDualcast || HasSwift || HasAccelerate || (!IsOpenForGrandImpact && CanGrandImpact);
            bool finisherChain = InFinisherChain();
            bool meleeStepComing = NextGcdIsAnyMeleeStep(nextGCD);
            bool blockInstantOgcds = NextGcdIsBlockedForInstants(nextGCD);

            bool allowAlignmentFix =
                InCombatWithTarget
                && !IsAnyMeleeComboInProgress()
                && !finisherChain
                && !meleeStepComing
                && !blockInstantOgcds;

            if (allowAlignmentFix && !nextIsInstant)
            {
                float flecheRem = EstimateRemainingSeconds(FlechePvE.Cooldown, 25f, 0.5f);
                float contreRem = EstimateRemainingSeconds(ContreSixtePvE.Cooldown, 35f, 0.5f);
                const float alignBuffer = 0.15f;

                bool flecheReadyByNextSlot =
                    flecheRem >= 0f &&
                    flecheRem <= NextAbilityToNextGCD + alignBuffer;

                bool contreReadyByNextSlot =
                    contreRem >= 0f &&
                    contreRem <= NextAbilityToNextGCD + alignBuffer;

                if (flecheReadyByNextSlot || contreReadyByNextSlot)
                {
                    if (AccelerationPvE.EnoughLevel
                        && !blockAccel
                        && !blockInstantOgcds
                        && !HasAccelerate
                        && !CanGrandImpact
                        && AccelerationPvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
                        return true;

                    if (!HasSwift && SwiftcastPvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
                        return true;
                }
            }
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        act = null;

        UpdateActiveMeleeTrack();

        bool meleeCheck = NextGcdIsAnyMeleeStep(nextGCD);
        bool finisherChain = InFinisherChain();
        bool blockInstantOgcds = NextGcdIsBlockedForInstants(nextGCD);
        bool blockSwift = IsAnyMeleeComboInProgress() || finisherChain || blockInstantOgcds;

        long now = Environment.TickCount64;

        bool emboldenSoon = EmboldenPvE.EnoughLevel && !HasEmbolden && EmboldenPvE.Cooldown.WillHaveOneCharge(10f);
        bool burstPrepHoldAccel = emboldenSoon && ManaStacks == 0 && BlackMana >= 50 && WhiteMana >= 50 && !IsAnyMeleeComboInProgress();
        bool inFirst5sAfterEmbolden = _emboldenUsedAtMs != 0 && (now - _emboldenUsedAtMs) < AccelLockAfterEmboldenMs;
        bool blockAccel = burstPrepHoldAccel || inFirst5sAfterEmbolden;

        bool nextIsInstant = HasDualcast || HasSwift || HasAccelerate || (!IsOpenForGrandImpact && CanGrandImpact);
        bool openerNeedsInstant = IsOpen && !nextIsInstant;

        bool needsMovementRescue =
            InCombat
            && HasHostilesInMaxRange
            && (MovingTime > 3f || openerNeedsInstant)
            && !nextIsInstant;

        if (needsMovementRescue && !meleeCheck && !IsAnyMeleeComboInProgress())
        {
            if (IsOpen)
            {
                if (!blockSwift && SwiftcastPvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
                    return true;

                if (UseBurstMedicine(out act))
                    return true;

                if (FlechePvE.CanUse(out act))
                    return true;

                if (AccelerationPvE.EnoughLevel
                    && !blockAccel
                    && !blockInstantOgcds
                    && !HasSwift
                    && !CanGrandImpact
                    && AccelerationPvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
                    return true;
            }
            else if (MovingTime > 3f)
            {
                if (AccelerationPvE.EnoughLevel
                    && !blockAccel
                    && !blockInstantOgcds
                    && !HasSwift
                    && !CanGrandImpact
                    && AccelerationPvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
                    return true;

                if (!blockSwift && SwiftcastPvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
                    return true;
            }
        }

        if (!needsMovementRescue && AccelerationPvE.EnoughLevel && !meleeCheck && !blockAccel && !blockInstantOgcds)
        {
            if (!CanGrandImpact && InCombat && HasHostilesInMaxRange)
            {
                bool usedUp = HasEmbolden || !EmboldenPvE.EnoughLevel || AccelerationPvE.Cooldown.WillHaveXChargesGCD(2, 1);

                if (!EnhancedAccelerationTrait.EnoughLevel)
                {
                    if (HasEmbolden || !EmboldenPvE.EnoughLevel)
                    {
                        if (AccelerationPvE.CanUse(out act))
                            return true;
                    }
                }
                else
                {
                    if (AccelerationPvE.CanUse(out act, usedUp: usedUp))
                        return true;
                }
            }
        }

        bool swiftHardGate = InCombat && InCombatWithTarget && ManaStacks != 3;

        if (swiftHardGate
            && !needsMovementRescue
            && !blockSwift
            && !HasSwift
            && (HasEmbolden || (EmboldenPvE.EnoughLevel && !EmboldenPvE.Cooldown.WillHaveOneCharge(30)) || !EmboldenPvE.EnoughLevel))
        {
            if (!HasAccelerate && !HasDualcast && !meleeCheck && !CanVerBoth)
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

        bool emboldenDelayOK =
            !DelayBuffOGCDs ||
            _emboldenUsedAtMs == 0 ||
            (Environment.TickCount64 - _emboldenUsedAtMs >= BuffOgcdDelayMs);

        if (!DelayBuffOGCDs)
        {
            if ((HasEmbolden || StatusHelper.PlayerWillStatusEndGCD(1, 0, true, StatusID.PrefulgenceReady))
                && PrefulgencePvE.CanUse(out act))
                return true;

            if (ViceOfThornsPvE.CanUse(out act))
                return true;
        }
        else
        {
            if (HasEmbolden
                && (emboldenDelayOK || StatusHelper.PlayerWillStatusEndGCD(1, 0, true, StatusID.PrefulgenceReady))
                && PrefulgencePvE.CanUse(out act))
                return true;

            if (HasEmbolden && emboldenDelayOK && ViceOfThornsPvE.CanUse(out act))
                return true;
        }

        if (InCombat && !IsOpen)
        {
            bool usedUp = HasEmbolden || !EmboldenPvE.EnoughLevel;

            if (!IsOpenForGrandImpact && EngagementPvE.CanUse(out act, usedUp: usedUp || EngagementPvE.Cooldown.WillHaveXChargesGCD(2, 1)))
                return true;

            if (!IsOpenForGrandImpact && !IsMoving && CorpsacorpsPvE.CanUse(out act, usedUp: usedUp || CorpsacorpsPvE.Cooldown.WillHaveXChargesGCD(2, 1)))
                return true;
        }

        return base.AttackAbility(nextGCD, out act);
    }

    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        bool emboldenReadyIn15 = EmboldenPvE.EnoughLevel && EmboldenPvE.Cooldown.WillHaveOneCharge(15f);

        if (IsOpen && IsBurst && UseBurstMedicine(out act))
            return true;

        if (!IsOpen && IsBurst && InCombat && emboldenReadyIn15 && IsLastGCD(ActionID.VerholyPvE, ActionID.VerflarePvE, ActionID.ScorchPvE) && UseBurstMedicine(out act))
            return true;

        if (!IsOpen && HasEmbolden && InCombat && UseBurstMedicine(out act))
            return true;

        return base.GeneralAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Ladder
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
        UpdateActiveMeleeTrack();
        UpdateMeleeCommitLock();

        float embRem = EmboldenRem();
        UpdateTripleComboReached(embRem);

        if (TryOpenerGCD(out act)) return true;
        if (TryFinisherGCD(out act)) return true;
        if (TryContinueComboGCD(out act)) return true;
        if (TryStartComboGCD(out act, embRem)) return true;
        if (TryRepriseGCD(out act)) return true;
        if (TryGrandImpactGCD(out act)) return true;
        if (TryLongCastTwoGCD(out act)) return true;
        if (TryProcGCD(out act)) return true;
        if (TryFallbackGCD(out act)) return true;

        return base.GeneralGCD(out act);
    }

    private bool TryOpenerGCD(out IAction? act)
    {
        act = null;

        if (!IsOpen || IsAnyMeleeComboInProgress() || ManaStacks == 3 || !InCombat || !HasHostilesInMaxRange)
            return false;

        bool hasInstant = HasDualcast || HasSwift || HasAccelerate;
        if (!hasInstant) return false;

        int targets = NumberOfHostilesInRangeOf(5);
        int impactThreshold = HasAccelerate ? 2 : 3;

        if (targets >= impactThreshold && ImpactPvE.CanUse(out act))
            return true;

        if (VerthunderIiiPvE.CanUse(out act)) return true;
        if (VerthunderPvE.CanUse(out act)) return true;

        return false;
    }

    private bool TryFinisherGCD(out IAction? act)
    {
        act = null;

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

        if (IsLastGCD(ActionID.ScorchPvE) && ResolutionPvE.CanUse(out act, skipStatusProvideCheck: true))
            return true;

        if (IsLastGCD(ActionID.VerholyPvE, ActionID.VerflarePvE) && ScorchPvE.CanUse(out act, skipStatusProvideCheck: true))
            return true;

        return false;
    }

    private bool TryContinueComboGCD(out IAction? act)
    {
        act = null;

        UpdateActiveMeleeTrack();

        if (!IsAnyMeleeComboInProgress() || _activeMeleeTrack == MeleeComboTrack.None)
        {
            _meleeHoldUntilMs = 0;
            return false;
        }

        if (CanContinueTrackedMeleeCombo(out act))
        {
            _meleeHoldUntilMs = 0;
            return true;
        }

        if (HoldMeleeComboIfOutOfRange)
        {
            long now = Environment.TickCount64;
            if (_meleeHoldUntilMs == 0)
                _meleeHoldUntilMs = now + HoldMeleeComboMs;

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

        return false;
    }

    private bool TryStartComboGCD(out IAction? act, float embRem)
    {
        act = null;

        UpdateActiveMeleeTrack();

        if (_activeMeleeTrack != MeleeComboTrack.None || IsAnyMeleeComboInProgress())
            return false;

        bool gateMelee = ShouldGateMeleeStarterAndManafication(embRem);
        bool blockMeleeStartersAndReprise = gateMelee && !NearPoolingCap;

        if (blockMeleeStartersAndReprise || InFinisherChain())
            return false;

        bool enoughToStart;
        bool burstStartOK;

        if (Pooling)
        {
            burstStartOK =
                !IsOpen &&
                (NearPoolingCap
                 || HasManafication
                 || StatusHelper.PlayerWillStatusEndGCD(4, 0, true, StatusID.MagickedSwordplay)
                 || (HasEmbolden && CanMagickedSwordplay)
                 || !gateMelee);

            enoughToStart = EnoughManaComboPooling || EnoughManaComboNoPooling;
        }
        else
        {
            bool poolCapReached = NearManaCap;
            burstStartOK =
                !IsOpen &&
                (poolCapReached
                 || HasManafication
                 || StatusHelper.PlayerWillStatusEndGCD(4, 0, true, StatusID.MagickedSwordplay)
                 || (HasEmbolden && CanMagickedSwordplay));

            enoughToStart = EnoughManaComboNoPooling || poolCapReached || EnoughManaComboPooling;
        }

        if (!enoughToStart || !burstStartOK)
            return false;

        MeleeComboTrack desiredTrack = IsAoEDesired() ? MeleeComboTrack.AoE : MeleeComboTrack.SingleTarget;
        return TryStartTrackedMeleeStarter(desiredTrack, out act, embRem);
    }

    private bool TryGrandImpactGCD(out IAction? act)
    {
        act = null;

        if (!IsOpen && !IsOpenForGrandImpact && GrandImpactPvE.CanUse(out act, skipStatusProvideCheck: CanGrandImpact, skipCastingCheck: true))
            return true;

        return false;
    }

    private bool TryLongCastTwoGCD(out IAction? act)
    {
        act = null;

        if (CanInstantCast && !CanVerEither)
        {
            if (ScatterPvE.CanUse(out act))
                return true;

            if (TrySelectTwoAimingGap11(out act))
                return true;
        }

        bool finisherChain = InFinisherChain();

        bool canRepriseNow =
            RangedSwordplay
            && ManaStacks == 0
            && (BlackMana < 50 || WhiteMana < 50)
            && EnchantedReprisePvE.CanUse(out _);

        bool noOtherMoveResources =
            !CanGrandImpact
            && !HasSwift
            && !HasDualcast
            && !canRepriseNow
            && !IsAnyMeleeComboInProgress()
            && !finisherChain
            && ManaStacks != 3;

        bool shouldSpendAccelOn2Soon =
            HasAccelerate
            && InCombat
            && HasHostilesInMaxRange
            && !IsAnyMeleeComboInProgress()
            && !finisherChain
            && ManaStacks != 3
            && (!CanVerBoth || (MovingTime > 3f && CanVerBoth && noOtherMoveResources));

        if (shouldSpendAccelOn2Soon)
        {
            if (NumberOfHostilesInRangeOf(5) >= 2 && ImpactPvE.CanUse(out act))
                return true;

            if (TrySelectTwoAimingGap11(out act))
                return true;
        }

        if (!IsAnyMeleeComboInProgress()
            && ManaStacks != 3
            && HasAccelerate
            && !HasSwift
            && !HasDualcast
            && InCombat
            && HasHostilesInMaxRange
            && MovingTime > 3f)
        {
            if (NumberOfHostilesInRangeOf(5) >= 2 && ImpactPvE.CanUse(out act))
                return true;

            if (TrySelectTwoAimingGap11(out act))
                return true;
        }

        if (!IsAnyMeleeComboInProgress()
            && ManaStacks != 3
            && InCombat
            && (HasHostilesInRange || HasHostilesInMaxRange)
            && HasInstantBuffToSpend)
        {
            if (NumberOfHostilesInRangeOf(5) >= 3 && ImpactPvE.CanUse(out act))
                return true;

            if (TrySelectTwoAimingGap11(out act))
                return true;
        }

        return false;
    }

    private bool TryProcGCD(out IAction? act)
    {
        act = null;

        if (!IsAnyMeleeComboInProgress()
            && ManaStacks != 3
            && InCombat
            && HasHostilesInMaxRange
            && CanVerBoth
            && !IsMoving
            && !HasInstantBuffToSpend)
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

        if (VerstonePvE.EnoughLevel && !HasInstantBuffToSpend)
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

        if (!VerstonePvE.EnoughLevel && !HasInstantBuffToSpend && VerfirePvE.CanUse(out act))
            return true;

        return false;
    }

    private bool TryRepriseGCD(out IAction? act)
    {
        act = null;

        if (ShouldHighManaDumpWithEnchantedReprise() && InCombat && HasHostilesInRange && EnchantedReprisePvE.CanUse(out act))
            return true;

        bool canRepriseForMove =
            RangedSwordplay
            && MovingTime > 3.5f
            && ManaStacks == 0
            && (BlackMana < 50 || WhiteMana < 50)
            && !HasAnyInstantTool
            && EnchantedReprisePvE.CanUse(out _);

        if (!canRepriseForMove)
            return false;

        float embRem = EmboldenRem();
        UpdateTripleComboReached(embRem);

        bool gateMelee = ShouldGateMeleeStarterAndManafication(embRem);
        bool blockMeleeStartersAndReprise = gateMelee && !NearPoolingCap;
        if (blockMeleeStartersAndReprise)
            return false;

        bool canRescueMovementWithOgcd =
            InCombat
            && HasHostilesInMaxRange
            && MovingTime > 3f
            && !IsAnyMeleeComboInProgress()
            && ManaStacks != 3
            && (
                (AccelerationPvE.EnoughLevel
                 && !CanGrandImpact
                 && AccelerationPvE.CanUse(out _, usedUp: true, skipCastingCheck: true))
                || SwiftcastPvE.CanUse(out _, usedUp: true, skipCastingCheck: true)
            );

        if (!canRescueMovementWithOgcd && EnchantedReprisePvE.CanUse(out act))
            return true;

        return false;
    }

    private bool TryFallbackGCD(out IAction? act)
    {
        act = null;

        if (MovingTime > 3f && InCombat && HasHostilesInMaxRange && ManaStacks != 3 && !HasAnyInstantTool)
        {
            act = null;
            return false;
        }

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

            if (!HasInstantBuffToSpend && JoltPvE.CanUse(out act))
                return true;
        }

        if (UseVercure && !InCombat && VercurePvE.CanUse(out act))
            return true;

        return false;
    }
    #endregion

    public override bool CanHealSingleSpell
    {
        get
        {
            int aliveHealerCount = 0;
            foreach (IBattleChara healer in PartyMembers.GetJobCategory(JobRole.Healer))
            {
                if (!healer.IsDead)
                    aliveHealerCount++;
            }

            return base.CanHealSingleSpell && (GCDHeal || aliveHealerCount == 0);
        }
    }
}