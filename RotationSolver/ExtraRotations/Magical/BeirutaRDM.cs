using System;
using System.ComponentModel;
using System.Linq;
using System.Numerics;

namespace RotationSolver.ExtraRotations.Magical;

[Rotation("BeirutaRDM", CombatType.PvE, GameVersion = "7.45")]
[SourceCode(Path = "main/ExtraRotations/Magical/BeirutaRDM.cs")]
[ExtraRotation]
public sealed class BeirutaRDM : RedMageRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name =
        "Please note that this rotation is optimised for Lv100 high-end encounters. V&C/OC GCDs may break combo.\n" +
        "• Recommend GCD for this rotation is 2.48 and above\n" +
        "• Try to stay close to the target when Embolden will be ready in ~20s if you selected triple combo before embolden\n" +
        "• Attempts to pool 73|73 mana for triple melee combo\n" +
        "• Ideally do not intercept defence ability during first 5s of the fights or burst\n" +
        "• Intentionally maintains an 11 mana gap to get Verholy/Verflare procs\n" +
        "• Disabling AutoBurst is sufficient if you need to delay burst timing in this rotation. However, you will need to mannually intercept enchanted riposte if you want to start a combo when burst off\n" +
        "• Manually use Enchanted Reprise if you cannot start a combo at the end of combat\n" +
        "• Go to Actions - GCD - Attack - Impact, change number of targets needed to use this action to 2. It will be using on 2 when has accelation, on 3 when not\n")]
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

    [RotationConfig(CombatType.PvE, Name = "Use Swift/Acceleration for oGCD window alignment (Fleche/Contre drift fix)")]
    public bool UseWindowAlignment { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Hold melee combo up to 2s if out of range")]
    public bool HoldMeleeComboIfOutOfRange { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Delay Prefulgence/Vice of Thorns for buff alignment (about 3 gcd after Embolden)")]
    public bool DelayBuffOGCDs { get; set; } = true;

    [Range(0, 4, ConfigUnitType.Seconds, 0.1f)]
    [RotationConfig(CombatType.PvE, Name = "Minimum movement time before allowing movement-based actions")]
    public float MovementTimeThreshold { get; set; } = 3f;

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

    #region Display
    public override void DisplayRotationStatus()
    {
        DrawStatus("Adjusted Riposte", Service.GetAdjustedActionId(ActionID.RipostePvE));
        DrawStatus("Adjusted Zwerchhau", Service.GetAdjustedActionId(ActionID.ZwerchhauPvE));
        DrawStatus("Adjusted Redoublement", Service.GetAdjustedActionId(ActionID.RedoublementPvE));
        DrawStatus("Adjusted Moulinet", Service.GetAdjustedActionId(ActionID.MoulinetPvE));
        DrawStatus("IsInMeleeCombo", IsInMeleeCombo);
        DrawStatus("ManaStacks", ManaStacks);
        DrawStatus("CanMagickedSwordplay", CanMagickedSwordplay);
        DrawStatus("EnchantedZwerchhau CanUse", EnchantedZwerchhauPvE.CanUse(out _));
        DrawStatus("EnchantedRedoublement CanUse", EnchantedRedoublementPvE.CanUse(out _));
        DrawStatus("EnchantedMoulinetDeux CanUse", EnchantedMoulinetDeuxPvE.CanUse(out _));
        DrawStatus("EnchantedMoulinetTrois CanUse", EnchantedMoulinetTroisPvE.CanUse(out _));
        DrawStatus("AoE Count Impact", GetTargetAoeCount(ImpactPvE));
        DrawStatus("AoE Count Veraero II", GetTargetAoeCount(VeraeroIiPvE));
        DrawStatus("AoE Count Verthunder II", GetTargetAoeCount(VerthunderIiPvE));
        DrawStatus("AoE Count Moulinet", GetTargetAoeCount(EnchantedMoulinetPvE));
        DrawStatus("EmboldenRem", EmboldenRem());
        DrawStatus("IsPoolingWindow", IsPoolingWindow(EmboldenRem()));
        DrawStatus("GateMeleeStarter", ShouldGateMeleeStarterAndManafication(EmboldenRem()));
        DrawStatus("DesiredMeleeTrack", DesiredMeleeTrackForCurrentState());
        DrawStatus("ActiveMeleeTrack", _activeMeleeTrack);
    }
    #endregion

    #region Static actions / constants
    private static BaseAction VeraeroPvEStartUp { get; } = new(ActionID.VeraeroPvE, false);
    private static BaseAction VerthunderPvEStartUp { get; } = new(ActionID.VerthunderPvE, false);

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
    private const int DefaultAoeThreshold = 3;
    private const int ImpactLowThreshold = 2;

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
    private bool HasValidTarget => InCombat && HasHostilesInMaxRange;
    private bool MoveThresholdMet => MovingTime > MovementTimeThreshold;
    private bool MoveThresholdMetForRescue => MovingTime > MovementTimeThreshold;
    private bool MoveThresholdMetForReprise => MovingTime > MovementTimeThreshold;

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
        _ => 5.5f,
    };

    private bool IsOpen => InCombat && CombatTime < OpenWindowSeconds;
    private bool IsOpenForGrandImpact => InCombat && CombatTime < (OpenWindowSeconds + GrandImpactExtraDelaySeconds);
    private bool HasInstantBuffToSpend => HasDualcast || HasSwift || (IsOpen && HasAccelerate);
    private bool HasAnyInstantTool => HasSwift || HasDualcast || HasAccelerate || (!IsOpenForGrandImpact && CanGrandImpact);
    private bool IsBurstLocked => IsAnyMeleeComboInProgress() || InFinisherChain() || ManaStacks == 3;

    private void DrawStatus(string label, object value) => ImGui.Text($"{label}: {value}");

    private static bool TryUse(out IAction? act, params IBaseAction[] actions)
    {
        foreach (var a in actions)
            if (a.CanUse(out act))
                return true;

        act = null;
        return false;
    }

    private static bool TryUseSkipStatus(out IAction? act, params IBaseAction[] actions)
    {
        foreach (var a in actions)
            if (a.CanUse(out act, skipStatusProvideCheck: true))
                return true;

        act = null;
        return false;
    }

    private bool TryUseAeroPair(out IAction? act, bool skipStatus = false)
        => skipStatus ? TryUseSkipStatus(out act, VeraeroIiiPvE, VeraeroPvE) : TryUse(out act, VeraeroIiiPvE, VeraeroPvE);

    private bool TryUseThunderPair(out IAction? act, bool skipStatus = false)
        => skipStatus ? TryUseSkipStatus(out act, VerthunderIiiPvE, VerthunderPvE) : TryUse(out act, VerthunderIiiPvE, VerthunderPvE);

    private bool InFinisherChain() =>
        ManaStacks == 3 ||
        IsLastGCD(ActionID.VerholyPvE, ActionID.VerflarePvE, ActionID.ScorchPvE) ||
        ScorchPvE.CanUse(out _) ||
        ResolutionPvE.CanUse(out _);

    private bool AccelerateEndingSoon =>
        Player != null &&
        HasAccelerate &&
        StatusHelper.PlayerWillStatusEndGCD(2, 0, true, StatusID.Acceleration);

    private bool IsLastSTComboStep() => IsLastGCD(true,
        EnchantedRipostePvE, EnchantedRipostePvE_45960,
        EnchantedZwerchhauPvE, EnchantedZwerchhauPvE_45961,
        EnchantedRedoublementPvE, EnchantedRedoublementPvE_45962);

    private bool IsLastAoEComboStep() => IsLastGCD(false,
        EnchantedMoulinetPvE,
        EnchantedMoulinetDeuxPvE,
        EnchantedMoulinetTroisPvE);

    private bool IsAnyMeleeComboInProgress() => IsInMeleeCombo || IsLastSTComboStep() || IsLastAoEComboStep();

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

        bool hasAoEContinuation = EnchantedMoulinetDeuxPvE.CanUse(out _) || EnchantedMoulinetTroisPvE.CanUse(out _);
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
        if (cooldown.HasOneCharge)
            return 0f;

        for (float t = 0f; t <= maxProbeSeconds; t += stepSeconds)
            if (cooldown.WillHaveOneCharge(t))
                return t;

        return -1f;
    }

    private float EmboldenRem() => !EmboldenPvE.EnoughLevel ? -1f : EstimateRemainingSeconds(EmboldenPvE.Cooldown, 60f, 0.5f);

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
        if (HasEmbolden || _meleeCommitLockActive || !IsPoolingWindow(embRem))
            return false;

        if (embRem <= UnlockAt)
            return false;

        if (TryTripleCombo && embRem <= TripleDecisionStart && _tripleComboReached)
            return false;

        return true;
    }

    private bool IsMeleeCommitWindow(float embRem) =>
        IsPoolingWindow(embRem) && !ShouldGateMeleeStarterAndManafication(embRem);

    private static bool NextGcdIsBlockedForInstants(IAction nextGCD) => nextGCD.IsTheSameTo(true,
        ActionID.EnchantedReprisePvE,
        ActionID.EnchantedRipostePvE, ActionID.EnchantedRipostePvE_45960,
        ActionID.EnchantedZwerchhauPvE, ActionID.EnchantedZwerchhauPvE_45961,
        ActionID.EnchantedRedoublementPvE, ActionID.EnchantedRedoublementPvE_45962,
        ActionID.EnchantedMoulinetPvE,
        ActionID.EnchantedMoulinetDeuxPvE,
        ActionID.EnchantedMoulinetTroisPvE,
        ActionID.VerholyPvE, ActionID.VerflarePvE,
        ActionID.ScorchPvE, ActionID.ResolutionPvE);

    private static bool NextGcdIsAnyMeleeStep(IAction nextGCD) => nextGCD.IsTheSameTo(true,
        ActionID.RipostePvE, ActionID.ZwerchhauPvE, ActionID.RedoublementPvE,
        ActionID.MoulinetPvE, ActionID.ReprisePvE,
        ActionID.EnchantedRipostePvE, ActionID.EnchantedRipostePvE_45960,
        ActionID.EnchantedZwerchhauPvE, ActionID.EnchantedZwerchhauPvE_45961,
        ActionID.EnchantedRedoublementPvE, ActionID.EnchantedRedoublementPvE_45962,
        ActionID.EnchantedMoulinetPvE,
        ActionID.EnchantedMoulinetDeuxPvE,
        ActionID.EnchantedMoulinetTroisPvE,
        ActionID.EnchantedReprisePvE);

    private bool CanContinueTrackedMeleeCombo(out IAction? act)
{
    act = null;

    // Give up immediately if combo is effectively over or in finisher chain
    if (InFinisherChain() ||
        (ManaStacks == 0 && !ScorchPvE.CanUse(out _) && !ResolutionPvE.CanUse(out _)))
    {
        _activeMeleeTrack = MeleeComboTrack.None;
        return false;
    }

    return _activeMeleeTrack switch
    {
        MeleeComboTrack.AoE =>
            // Step 3
            ((IsLastGCD(false, EnchantedMoulinetDeuxPvE) || ManaStacks == 2) &&
                EnchantedMoulinetTroisPvE.CanUse(out act)) ||

            // Step 2
            ((IsLastGCD(false, EnchantedMoulinetPvE) || ManaStacks == 1) &&
                EnchantedMoulinetDeuxPvE.CanUse(out act)),

        MeleeComboTrack.SingleTarget =>
            // Step 3
            (((IsLastGCD(true, EnchantedZwerchhauPvE_45961) || IsLastGCD(true, EnchantedZwerchhauPvE)) || ManaStacks == 2) &&
                (EnchantedRedoublementPvE_45962.CanUse(out act) || EnchantedRedoublementPvE.CanUse(out act))) ||

            // Step 2
            (((IsLastGCD(true, EnchantedRipostePvE_45960) || IsLastGCD(true, EnchantedRipostePvE)) || ManaStacks == 1) &&
                (EnchantedZwerchhauPvE_45961.CanUse(out act) || EnchantedZwerchhauPvE.CanUse(out act))),

        _ => false,
    };
}

    private void RegisterMeleeStarter(MeleeComboTrack track, float embRem)
    {
        _activeMeleeTrack = track;
        _meleeStarterUsedAtMs = Environment.TickCount64;
        if (IsMeleeCommitWindow(embRem))
            _meleeCommitLockActive = true;
    }

    private bool ShouldBlockUtilityCast() =>
        HasManafication || HasEmbolden || ManaStacks == 3 || CanMagickedSwordplay || CanGrandImpact ||
        ScorchPvE.CanUse(out _) || ResolutionPvE.CanUse(out _) ||
        IsLastComboAction(ActionID.RipostePvE, ActionID.ZwerchhauPvE);

    private bool ShouldHighManaDumpWithEnchantedReprise()
    {
        float embRem = EmboldenRem();
        return NearManaCap
            && !ShouldGateMeleeStarterAndManafication(embRem)
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

        if (desiredTrack == MeleeComboTrack.AoE)
        {
            if (CanHoldOrWaitForMeleeRange())
                return false;

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
            _meleeHoldUntilMs = 0;
            return true;
        }

        if (CanHoldOrWaitForMeleeRange())
            return false;

        return false;
    }

    private bool NeedsMovementRescue(bool nextIsInstant, bool meleeCheck) =>
        HasValidTarget &&
        (MoveThresholdMetForRescue || (IsOpen && !nextIsInstant)) &&
        !nextIsInstant &&
        !meleeCheck &&
        !IsAnyMeleeComboInProgress();

    private bool CanRescueMovementWithOgcd(bool blockSwift, bool blockAccel, bool blockInstantOgcds, out IAction? act)
    {
        act = null;

        if (IsAnyMeleeComboInProgress() || InFinisherChain() || ManaStacks == 3) return false;

        if (AccelerationPvE.EnoughLevel
            && !blockAccel
            && !blockInstantOgcds
            && !HasSwift
            && !CanGrandImpact
            && AccelerationPvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
            return true;

        if (!blockSwift && SwiftcastPvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
            return true;

        return false;
    }
    #endregion

    #region AoE Helpers
    private int GetTargetAoeCount(IBaseAction action)
    {
        if (AllHostileTargets == null)
            return 0;

        int maxAoeCount = 0;
        float castRange = action.Info.Range;
        float effectRange = action.Info.EffectRange;

        foreach (var centerTarget in AllHostileTargets.Where(t =>
                     t != null &&
                     t.CanSee() &&
                     t.DistanceToPlayer() <= castRange))
        {
            int currentAoeCount = 0;

            foreach (var otherTarget in AllHostileTargets)
            {
                if (otherTarget == null)
                    continue;

                if (Vector3.Distance(centerTarget.Position, otherTarget.Position) <=
                    (effectRange + otherTarget.HitboxRadius))
                {
                    currentAoeCount++;
                }
            }

            maxAoeCount = Math.Max(maxAoeCount, currentAoeCount);
        }

        return maxAoeCount;
    }

    private bool IsTargetAoeAtLeast(IBaseAction action, int threshold) =>
        GetTargetAoeCount(action) >= threshold;

    private bool IsImpactAoeDesired(int threshold) =>
        IsTargetAoeAtLeast(ImpactPvE, threshold);

    private bool IsSpellAoeDesired() =>
        IsTargetAoeAtLeast(VeraeroIiPvE, DefaultAoeThreshold) ||
        IsTargetAoeAtLeast(VerthunderIiPvE, DefaultAoeThreshold);

    private bool IsMeleeAoeDesired() =>
        IsTargetAoeAtLeast(EnchantedMoulinetPvE, DefaultAoeThreshold);

    private MeleeComboTrack DesiredMeleeTrackForCurrentState() =>
        IsMeleeAoeDesired() ? MeleeComboTrack.AoE : MeleeComboTrack.SingleTarget;
    #endregion

    #region Small policy helpers
    private bool ShouldUseImpactInOpener() =>
        IsImpactAoeDesired(HasAccelerate ? ImpactLowThreshold : DefaultAoeThreshold);

    private bool ShouldUseImpactAsAccelExpirySaver() =>
        IsImpactAoeDesired(ImpactLowThreshold);

    private bool ShouldUseImpactAtTwoTargets() =>
        IsImpactAoeDesired(ImpactLowThreshold);

    private bool ShouldUseImpactAtThreeTargets() =>
        IsImpactAoeDesired(DefaultAoeThreshold);

    private bool ShouldUseFallbackAoeSpells() =>
        IsSpellAoeDesired();
    private bool IsUnsafeForManafication()
{
    return
        IsLastGCD(
            ActionID.EnchantedRipostePvE, ActionID.EnchantedRipostePvE_45960,
            ActionID.EnchantedZwerchhauPvE, ActionID.EnchantedZwerchhauPvE_45961,
            ActionID.EnchantedMoulinetPvE,
            ActionID.EnchantedMoulinetDeuxPvE,
            ActionID.VerholyPvE,
            ActionID.VerflarePvE,
            ActionID.ScorchPvE) ||
        ScorchPvE.CanUse(out _) ||
        ResolutionPvE.CanUse(out _);
}
    private bool CanUseManaficationNow(bool gateMelee)
    {
        if (gateMelee)
            return false;

        if (IsUnsafeForManafication())
        return false;

        if (NearManaCap && InMeleeRange3)
            return false;

        return
            !IsOpen &&
            InCombat &&
            IsBurst &&
            HasHostilesInMaxRange &&
            (HasEmbolden ||
             EmboldenPvE.Cooldown.HasOneCharge ||
             EmboldenPvE.Cooldown.WillHaveOneCharge(5f));
    }

    private bool ShouldUseAccelerationForMovement(bool blockAccel, bool blockInstantOgcds) =>
        AccelerationPvE.EnoughLevel &&
        !blockAccel &&
        !blockInstantOgcds &&
        !HasSwift &&
        !CanGrandImpact;

    private bool ShouldUseSwiftForMovement(bool blockSwift) =>
        !blockSwift;

    private bool CanUseAccelerationForWindowAlignment(bool blockAccel, bool blockInstantOgcds) =>
        AccelerationPvE.EnoughLevel &&
        !blockAccel &&
        !HasAccelerate &&
        !CanGrandImpact &&
        !blockInstantOgcds;

    private bool WantsInstantForOgcdWindow(IAction nextGCD, bool finisherChain, bool blockInstantOgcds, bool nextIsInstant)
    {
        bool meleeStepComing = NextGcdIsAnyMeleeStep(nextGCD);

        return
            InCombatWithTarget &&
            !IsAnyMeleeComboInProgress() &&
            !finisherChain &&
            !meleeStepComing &&
            !blockInstantOgcds &&
            !nextIsInstant &&
            ((FlechePvE.EnoughLevel && !FlechePvE.Cooldown.HasOneCharge && FlechePvE.Cooldown.WillHaveOneCharge(2f)) ||
             (ContreSixtePvE.EnoughLevel && !ContreSixtePvE.Cooldown.HasOneCharge && ContreSixtePvE.Cooldown.WillHaveOneCharge(2f)));
    }

    private bool ShouldSpendAccelerationOnTwoTargetSpell(bool finisherChain, bool noOtherMoveResources) =>
        HasAccelerate &&
        HasValidTarget &&
        !IsAnyMeleeComboInProgress() &&
        !finisherChain &&
        ManaStacks != 3 &&
        (!CanVerBoth || (MoveThresholdMetForRescue && CanVerBoth && noOtherMoveResources));
    #endregion

    #region Spell selection helpers
    private bool TrySelectTwoAimingGap11(out IAction? act)
    {
        act = null;
        int diff = BlackMana - WhiteMana;
        int gap = Math.Abs(diff);
        bool blackLeads = diff >= 0;
        bool belowDouble = BlackMana < DoubleB || WhiteMana < DoubleW;
        bool atOrAboveTriple = BlackMana >= TripleB && WhiteMana >= TripleW;
        bool betweenBands = !belowDouble && !atOrAboveTriple;

        if (betweenBands)
            return diff > 0 ? (TryUseAeroPair(out act, true) || TryUseThunderPair(out act, true))
                 : diff < 0 ? (TryUseThunderPair(out act, true) || TryUseAeroPair(out act, true))
                 : (TryUseThunderPair(out act, true) || TryUseAeroPair(out act, true));

        if (gap > TargetManaGap)
            return blackLeads ? (TryUseAeroPair(out act, true) || TryUseThunderPair(out act, true))
                              : (TryUseThunderPair(out act, true) || TryUseAeroPair(out act, true));

        if (gap < TargetManaGap)
            return blackLeads ? (TryUseThunderPair(out act, true) || TryUseAeroPair(out act, true))
                              : (TryUseAeroPair(out act, true) || TryUseThunderPair(out act, true));

        return blackLeads ? (TryUseThunderPair(out act, true) || TryUseAeroPair(out act, true))
                          : (TryUseAeroPair(out act, true) || TryUseThunderPair(out act, true));
    }
    #endregion

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (HasDualcast && VerthunderPvEStartUp.CanUse(out IAction? act))
            return act;

        if (remainTime < VeraeroPvEStartUp.Info.CastTime + CountDownAhead && VeraeroPvEStartUp.CanUse(out act))
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

         if (IsOpen && IsBurst)
        {
            if (SwiftcastPvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
                return true;

            if (CombatTime > 1f && UseBurstMedicine(out act))
                return true;
        }

        if (CanUseManaficationNow(gateMelee) && ManaficationPvE.CanUse(out act))
            return true;

        bool emboldenAllowed = !IsOpen && IsBurst && InCombat && (AnyonesMeleeRule ? InMeleeRange3 : HasHostilesInRange);
        if (emboldenAllowed && EmboldenPvE.CanUse(out act))
        {
            _emboldenUsedAtMs = Environment.TickCount64;
            return true;
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
{
    act = null;
    UpdateActiveMeleeTrack();

    bool comboInProgress = IsAnyMeleeComboInProgress();
    bool finisherChain = InFinisherChain();
    bool meleeOrFinisherLocked = comboInProgress || finisherChain;

    bool meleeCheck = NextGcdIsAnyMeleeStep(nextGCD);
    bool blockInstantOgcds = meleeOrFinisherLocked || NextGcdIsBlockedForInstants(nextGCD);
    bool blockSwift = meleeOrFinisherLocked || blockInstantOgcds;

    long now = Environment.TickCount64;

    bool emboldenSoon =
        EmboldenPvE.EnoughLevel &&
        !HasEmbolden &&
        EmboldenPvE.Cooldown.WillHaveOneCharge(25f);

    bool burstPrepHoldAccel =
        emboldenSoon &&
        ManaStacks == 0 &&
        BlackMana >= 50 &&
        WhiteMana >= 50 &&
        !comboInProgress;

    bool inFirst5sAfterEmbolden =
        _emboldenUsedAtMs != 0 &&
        (now - _emboldenUsedAtMs) < AccelLockAfterEmboldenMs;

    bool blockAccel = meleeOrFinisherLocked || burstPrepHoldAccel || inFirst5sAfterEmbolden;

    bool nextIsInstant =
        HasDualcast ||
        HasSwift ||
        HasAccelerate ||
        (!IsOpenForGrandImpact && CanGrandImpact);

    bool needsMovementRescue = NeedsMovementRescue(nextIsInstant, meleeCheck);

    bool canUseSwiftForMovement =
        !meleeOrFinisherLocked &&
        !meleeCheck &&
        ShouldUseSwiftForMovement(blockSwift);

    bool canUseAccelForMovement =
        !meleeOrFinisherLocked &&
        !meleeCheck &&
        ShouldUseAccelerationForMovement(blockAccel, blockInstantOgcds);

    if (needsMovementRescue && !meleeOrFinisherLocked && !meleeCheck)
    {
        if (IsOpen)
        {
            if (canUseSwiftForMovement &&
                SwiftcastPvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
                return true;

            if (UseBurstMedicine(out act))
                return true;

            if (FlechePvE.CanUse(out act))
                return true;

            if (canUseAccelForMovement &&
                AccelerationPvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
                return true;
        }
        else if (MoveThresholdMetForRescue)
        {
            if (canUseAccelForMovement &&
                AccelerationPvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
                return true;

            if (canUseSwiftForMovement &&
                SwiftcastPvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
                return true;
        }
    }

    bool canUseAccelGenerically =
        !needsMovementRescue &&
        !meleeOrFinisherLocked &&
        !meleeCheck &&
        AccelerationPvE.EnoughLevel &&
        !blockAccel &&
        !blockInstantOgcds;

    if (canUseAccelGenerically && !CanGrandImpact && HasValidTarget)
    {
        bool usedUp =
            HasEmbolden ||
            !EmboldenPvE.EnoughLevel ||
            AccelerationPvE.Cooldown.WillHaveXChargesGCD(2, 1);

        if (!EnhancedAccelerationTrait.EnoughLevel)
        {
            if ((HasEmbolden || !EmboldenPvE.EnoughLevel) &&
                AccelerationPvE.CanUse(out act))
                return true;
        }
        else if (AccelerationPvE.CanUse(out act, usedUp: usedUp))
        {
            return true;
        }
    }

    bool accelAboutToCap =
        AccelerationPvE.EnoughLevel &&
        AccelerationPvE.Cooldown.WillHaveXChargesGCD(2, 2);

    bool canUseAccelForCap =
        accelAboutToCap &&
        !needsMovementRescue &&
        !meleeOrFinisherLocked &&
        !meleeCheck &&
        !blockAccel &&
        !blockInstantOgcds &&
        !HasAccelerate &&
        !CanGrandImpact;

    if (canUseAccelForCap &&
        AccelerationPvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
        return true;

    bool swiftHardGate =
        InCombat &&
        InCombatWithTarget &&
        !finisherChain;

    bool canUseSwiftGenerically =
        swiftHardGate &&
        !needsMovementRescue &&
        !meleeOrFinisherLocked &&
        !blockSwift &&
        !HasSwift &&
        !HasAccelerate &&
        !HasDualcast &&
        !meleeCheck &&
        !CanVerBoth &&
        (HasEmbolden ||
         (EmboldenPvE.EnoughLevel && !EmboldenPvE.Cooldown.WillHaveOneCharge(30)) ||
         !EmboldenPvE.EnoughLevel);

    if (canUseSwiftGenerically)
    {
        if (!CanVerFire && !CanVerStone &&
            IsLastGCD(false, VerthunderPvE, VerthunderIiiPvE, VeraeroPvE, VeraeroIiiPvE) &&
            SwiftcastPvE.CanUse(out act))
            return true;

        if (!CanVerStone &&
            nextGCD.IsTheSameTo(false, VeraeroPvE, VeraeroIiiPvE) &&
            SwiftcastPvE.CanUse(out act))
            return true;

        if (!CanVerFire &&
            nextGCD.IsTheSameTo(false, VerthunderPvE, VerthunderIiiPvE) &&
            SwiftcastPvE.CanUse(out act))
            return true;
    }

    if (FlechePvE.CanUse(out act))
        return true;

    if (!IsOpenForGrandImpact && ContreSixtePvE.CanUse(out act))
        return true;

    if (UseWindowAlignment &&
        !meleeOrFinisherLocked &&
        WantsInstantForOgcdWindow(nextGCD, finisherChain, blockInstantOgcds, nextIsInstant))
    {
        if (CanUseAccelerationForWindowAlignment(blockAccel, blockInstantOgcds) &&
            AccelerationPvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
            return true;

        if (!HasSwift &&
            SwiftcastPvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
            return true;
    }

    bool emboldenDelayOK =
        !DelayBuffOGCDs ||
        _emboldenUsedAtMs == 0 ||
        (Environment.TickCount64 - _emboldenUsedAtMs >= BuffOgcdDelayMs);

    if (!DelayBuffOGCDs)
    {
        if ((HasEmbolden || StatusHelper.PlayerWillStatusEndGCD(1, 0, true, StatusID.PrefulgenceReady)) &&
            PrefulgencePvE.CanUse(out act))
            return true;

        if (ViceOfThornsPvE.CanUse(out act))
            return true;
    }
    else
    {
        if (HasEmbolden &&
            (emboldenDelayOK || StatusHelper.PlayerWillStatusEndGCD(1, 0, true, StatusID.PrefulgenceReady)) &&
            PrefulgencePvE.CanUse(out act))
            return true;

        if (HasEmbolden &&
            emboldenDelayOK &&
            ViceOfThornsPvE.CanUse(out act))
            return true;
    }

    if (InCombat && !IsOpen)
    {
        bool usedUp = HasEmbolden || !EmboldenPvE.EnoughLevel;

        if (!IsOpenForGrandImpact &&
            EngagementPvE.CanUse(out act, usedUp: usedUp || EngagementPvE.Cooldown.WillHaveXChargesGCD(2, 1)))
            return true;

        if (!IsOpenForGrandImpact &&
            !IsMoving &&
            CorpsacorpsPvE.CanUse(out act, usedUp: usedUp || CorpsacorpsPvE.Cooldown.WillHaveXChargesGCD(2, 1)))
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
        if (PreventHeal && ShouldBlockUtilityCast())
            return base.HealSingleGCD(out act);

        if (VercurePvE.CanUse(out act, skipStatusProvideCheck: true))
            return true;

        return base.HealSingleGCD(out act);
    }

    [RotationDesc(ActionID.VerraisePvE)]
    protected override bool RaiseGCD(out IAction? act)
    {
        if (PreventRaising && ShouldBlockUtilityCast())
            return base.RaiseGCD(out act);

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

        if (TryFinisherGCD(out act)) return true;
        if (TryOpenerGCD(out act)) return true;
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

        if (!(HasDualcast || HasSwift || HasAccelerate))
            return false;

        if (ShouldUseImpactInOpener() && ImpactPvE.CanUse(out act))
            return true;

        return TryUse(out act, VerthunderIiiPvE, VerthunderPvE);
    }

    private bool TryFinisherGCD(out IAction? act)
    {
        act = null;

        if (ResolutionPvE.CanUse(out act, skipStatusProvideCheck: true))
            return true;

        if (ScorchPvE.CanUse(out act, skipStatusProvideCheck: true))
            return true;

        if (ManaStacks != 3)
            return false;

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

        if (ResolutionPvE.CanUse(out _) || ScorchPvE.CanUse(out _) || ManaStacks == 3 || !IsBurst || HasSwift)
            return false;

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
            burstStartOK = !IsOpen &&
                (NearPoolingCap || HasManafication || StatusHelper.PlayerWillStatusEndGCD(4, 0, true, StatusID.MagickedSwordplay) || (HasEmbolden && CanMagickedSwordplay) || !gateMelee);
            enoughToStart = EnoughManaComboPooling || EnoughManaComboNoPooling;
        }
        else
        {
            bool poolCapReached = NearManaCap;
            burstStartOK = !IsOpen &&
                (poolCapReached || HasManafication || StatusHelper.PlayerWillStatusEndGCD(4, 0, true, StatusID.MagickedSwordplay) || (HasEmbolden && CanMagickedSwordplay));
            enoughToStart = EnoughManaComboNoPooling || poolCapReached || EnoughManaComboPooling;
        }

        if (!enoughToStart || !burstStartOK)
            return false;

        return TryStartTrackedMeleeStarter(DesiredMeleeTrackForCurrentState(), out act, embRem);
    }

    private bool TryGrandImpactGCD(out IAction? act)
{
    act = null;

    if (AccelerateEndingSoon)
        return false;

    if (IsAnyMeleeComboInProgress() || InFinisherChain() || ManaStacks == 3)
        return false;

    return !IsOpen && !IsOpenForGrandImpact &&
           GrandImpactPvE.CanUse(out act, skipStatusProvideCheck: CanGrandImpact, skipCastingCheck: true);
}

    private bool TryLongCastTwoGCD(out IAction? act)
{
    act = null;

    if (IsAnyMeleeComboInProgress() || InFinisherChain() || ManaStacks == 3)
        return false;

    if (AccelerateEndingSoon)
    {
        if (ShouldUseImpactAsAccelExpirySaver() && ImpactPvE.CanUse(out act))
            return true;

        if (TrySelectTwoAimingGap11(out act))
            return true;
    }

    if (CanInstantCast && !CanVerEither)
    {
        if (ScatterPvE.CanUse(out act)) return true;
        if (TrySelectTwoAimingGap11(out act)) return true;
    }

    bool finisherChain = InFinisherChain();
    bool canRepriseNow = RangedSwordplay && ManaStacks == 0 && (BlackMana < 50 || WhiteMana < 50) && EnchantedReprisePvE.CanUse(out _);
    bool noOtherMoveResources = !CanGrandImpact && !HasSwift && !HasDualcast && !canRepriseNow && !IsAnyMeleeComboInProgress() && !finisherChain && ManaStacks != 3;

    bool shouldSpendAccelOn2Soon = ShouldSpendAccelerationOnTwoTargetSpell(finisherChain, noOtherMoveResources);
    bool shouldUseTwoTargetImpactForMovement =
        !IsAnyMeleeComboInProgress() &&
        ManaStacks != 3 &&
        HasAccelerate &&
        !HasSwift &&
        !HasDualcast &&
        HasValidTarget &&
        MoveThresholdMetForRescue;

    bool shouldUseThreeTargetImpactForInstantSpend =
        !IsAnyMeleeComboInProgress() &&
        ManaStacks != 3 &&
        InCombat &&
        (HasHostilesInRange || HasHostilesInMaxRange) &&
        HasInstantBuffToSpend;

    if (shouldSpendAccelOn2Soon || shouldUseTwoTargetImpactForMovement)
    {
        if (ShouldUseImpactAtTwoTargets() && ImpactPvE.CanUse(out act))
            return true;

        if (TrySelectTwoAimingGap11(out act))
            return true;
    }

    if (shouldUseThreeTargetImpactForInstantSpend)
    {
        if (ShouldUseImpactAtThreeTargets() && ImpactPvE.CanUse(out act))
            return true;

        if (TrySelectTwoAimingGap11(out act))
            return true;
    }

    return false;
}

    private bool TryProcGCD(out IAction? act)
{
    act = null;

    if (IsAnyMeleeComboInProgress() || InFinisherChain() || ManaStacks == 3)
        return false;

    if (!IsAnyMeleeComboInProgress() && ManaStacks != 3 && HasValidTarget && CanVerBoth && !IsMoving && !HasInstantBuffToSpend)
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
            RangedSwordplay &&
            MoveThresholdMetForReprise &&
            ManaStacks == 0 &&
            (BlackMana < 50 || WhiteMana < 50) &&
            !HasAnyInstantTool &&
            EnchantedReprisePvE.CanUse(out _);

        if (!canRepriseForMove)
            return false;

        float embRem = EmboldenRem();
        UpdateTripleComboReached(embRem);

        bool gateMelee = ShouldGateMeleeStarterAndManafication(embRem);
        if (gateMelee && !NearPoolingCap)
            return false;

        bool blockInstantOgcds = IsAnyMeleeComboInProgress() || InFinisherChain();
        bool canRescueMovementWithOgcd =
            HasValidTarget &&
            MoveThresholdMetForRescue &&
            !IsAnyMeleeComboInProgress() &&
            ManaStacks != 3 &&
            CanRescueMovementWithOgcd(
                blockSwift: blockInstantOgcds,
                blockAccel: false,
                blockInstantOgcds: blockInstantOgcds,
                out _);

        if (!canRescueMovementWithOgcd && EnchantedReprisePvE.CanUse(out act))
            return true;

        return false;
    }

    private bool TryFallbackGCD(out IAction? act)
{
    act = null;

    if (IsAnyMeleeComboInProgress() || InFinisherChain() || ManaStacks == 3)
        return false;

    if (MoveThresholdMetForRescue && HasValidTarget && ManaStacks != 3 && !HasAnyInstantTool)
    {
        act = null;
        return false;
    }

    if (!CanInstantCast && !CanVerEither)
    {
        if (ShouldUseFallbackAoeSpells())
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
                if (!healer.IsDead)
                    aliveHealerCount++;

            return base.CanHealSingleSpell && (GCDHeal || aliveHealerCount == 0);
        }
    }
}