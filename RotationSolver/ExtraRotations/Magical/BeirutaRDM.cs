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

    [RotationConfig(CombatType.PvE, Name = "Pool Black and White Mana for double combo embolden")]
    public bool Pooling { get; set; } = true;

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

    [RotationConfig(CombatType.PvE, Name = "Use Displacement after Engagement (use at own risk).")]
    public bool SuicideByDumber { get; set; } = false;

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

// Opener window = first 5 seconds of combat
private float OpenWindowSeconds => OpenWindow switch
{
    OpenWindowGcd.ZeroGcd => 0f,
    OpenWindowGcd.OneGcd  => 2.2f,
    _                     => 5.1f, // TwoGcd
};

// Opener/Burst open window = first N seconds of combat (based on selection)
private bool IsOpen => InCombat && CombatTime < OpenWindowSeconds;
private const float GrandImpactExtraDelaySeconds = 1.0f;

private bool IsOpenForGrandImpact =>
    InCombat && CombatTime < (OpenWindowSeconds + GrandImpactExtraDelaySeconds);



// Only checks the *correct next melee step* for the combo we are currently in.
// - Supports ST chain (Riposte -> Zwerchhau -> Redoublement)
// - Supports AoE chain (Moulinet -> Moulinet Deux -> Moulinet Trois)
// - Does NOT "swap" between AoE and ST mid-combo
private bool TryContinueCurrentMeleeCombo(out IAction? act)
{
    act = null;

    // --- AoE combo continuation ---
    // If we are already in AoE chain, ONLY try the next AoE step.
    if (IsLastGCD(false, EnchantedMoulinetDeuxPvE))
    {
        return EnchantedMoulinetTroisPvE.CanUse(out act);
    }
    if (IsLastGCD(false, EnchantedMoulinetPvE))
    {
        return EnchantedMoulinetDeuxPvE.CanUse(out act);
    }

    // --- ST combo continuation ---
    // If we are already in ST chain, ONLY try the next ST step.
    // (Your framework already handles range differences via CanUse on _45960 vs normal.)
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

    // If we're "in melee combo" but last GCD isn't clearly one of the above,
    // we don't force anything here—just report we can't continue.
    // (This avoids accidentally starting the wrong chain.)
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
            {
                return act;
            }
        }

        //Remove Swift

        if (HasAccelerate && remainTime < 0f)
        {
            StatusHelper.StatusOff(StatusID.Acceleration);
        }

        if (HasSwift && remainTime < 0f)
        {
            StatusHelper.StatusOff(StatusID.Swiftcast);
        }

        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    [RotationDesc(ActionID.CorpsacorpsPvE)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (CorpsacorpsPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        return base.MoveForwardAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.DisplacementPvE)]
    protected override bool MoveBackAbility(IAction nextGCD, out IAction? act)
    {
        if (DisplacementPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        return base.MoveBackAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.AddlePvE, ActionID.MagickBarrierPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (AddlePvE.CanUse(out act))
        {
            return true;
        }

        if (MagickBarrierPvE.CanUse(out act))
        {
            return true;
        }

        return base.DefenseAreaAbility(nextGCD, out act);
    }

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        bool AnyoneInMeleeRange = NumberOfHostilesInRangeOf(3) > 0;

		if (!IsOpen
    && (HasEmbolden || EmboldenPvE.Cooldown.HasOneCharge || (EmboldenPvE.Cooldown.WillHaveOneCharge(4f) && !IsInMeleeCombo)))
{
			if (InCombat && HasHostilesInMaxRange && ManaficationPvE.CanUse(out act))
			{
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
else // AnyonesMeleeRule
{
    if (!IsOpen && IsBurst && InCombat && AnyoneInMeleeRange && EmboldenPvE.CanUse(out act))
    {
        _emboldenUsedAtMs = Environment.TickCount64;
        return true;
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

// ---------------------------
// Finishers window (used for Swift gating etc.)
// ---------------------------
bool finisherChain =
    ManaStacks == 3 ||
    IsLastGCD(ActionID.VerholyPvE, ActionID.VerflarePvE, ActionID.ScorchPvE) ||
    ScorchPvE.CanUse(out _) ||
    ResolutionPvE.CanUse(out _);

bool blockSwift = IsInMeleeCombo || finisherChain;

// ---------------------------
// Accel hold rules:
// - Do NOT press Accel when Embolden will be ready within 10s AND we already have 50/50+ mana
//   (likely holding for burst; Accel may expire before being spent).
// - Do NOT press Accel for the first 10s after Embolden was used (aligns with your delayed oGCD plan).
// ---------------------------
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


    // "Next GCD is instant" approximation ---
    bool nextIsInstant = HasDualcast || HasSwift || HasAccelerate || (!IsOpenForGrandImpact && CanGrandImpact);
bool openerNeedsInstant = IsOpen && !nextIsInstant;
bool needsMovementRescue =
    InCombat && HasHostilesInMaxRange && (IsMoving || openerNeedsInstant) && !nextIsInstant;


    // ---------------------------
    // If moving and next GCD isn't instant, force Acceleration/Swift so we don't drop casts.
    // ---------------------------
    if (needsMovementRescue)
    {
        // Don't interfere with melee steps / melee combo.
        if (!Meleecheck && !IsInMeleeCombo)
        {
            // Optional: only attempt if we have a safe weave window.
            // If it prevents usage too often, delete this if+braces.
            if (IsOpen || NextAbilityToNextGCD < 0.6f)
{
if (IsOpen)
    {
        // 1) Swift
        if (!blockSwift && SwiftcastPvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
            return true;

        // 2) Pot
        if (InCombat && UseBurstMedicine(out act))
            return true;

        // 3) Fleche
        if (FlechePvE.CanUse(out act))
            return true;

        // 4) Accel
        if (AccelerationPvE.EnoughLevel
            && !blockAccel
            && !HasSwift
            && !CanGrandImpact
            && AccelerationPvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
            return true;
    }
    else
    {
        // Normal priority: Accel first, then Swift (your original behaviour)
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


// ---------------------------
// Acceleration usage (skip if movement rescue needed this frame)
// ---------------------------
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
                if (AccelerationPvE.CanUse(out act, usedUp: HasEmbolden || !EmboldenPvE.EnoughLevel || AccelerationPvE.Cooldown.WillHaveXChargesGCD(2, 1)))
                    return true;
            }

            if (EnhancedAccelerationIiTrait.EnoughLevel)
            {
                if (AccelerationPvE.CanUse(out act, usedUp: HasEmbolden || !EmboldenPvE.EnoughLevel || AccelerationPvE.Cooldown.WillHaveXChargesGCD(2, 1)))
                    return true;
            }
        }
    }
    // ---------------------------
    // Swiftcast usage (skip if movement rescue needed this frame)
    // ---------------------------
    bool swiftHardGate =
    InCombat
    && (HasHostilesInMaxRange || HasHostilesInRange) // must have a valid hostile target state
    && ManaStacks != 3;                              // never during finishers window

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


// ---------------------------
// Prefulgence / Vice alignment option
// Off  -> original behaviour
// On   -> only use during Embolden, and only after ~6s (CD <= 114s)
//        Prefulgence has a safety: if the ready buff is about to expire, use it.
// ---------------------------

const long delayMs = 5000; 

bool emboldenDelayOK =
    !DelayBuffOGCDs ||
    (_emboldenUsedAtMs == 0) ||
    (Environment.TickCount64 - _emboldenUsedAtMs >= delayMs);


// Prefulgence
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
    // delayed behaviour: only inside Embolden, ~6s in
    if (HasEmbolden
        && (emboldenDelayOK || StatusHelper.PlayerWillStatusEndGCD(1, 0, true, StatusID.PrefulgenceReady))
        && PrefulgencePvE.CanUse(out act))
    {
        return true;
    }
}

// Vice of Thorns
if (!DelayBuffOGCDs)
{
    if (ViceOfThornsPvE.CanUse(out act))
    {
        return true;
    }
}
else
{
// delayed behaviour: only inside Embolden, ~6s in
    if (HasEmbolden && emboldenDelayOK && ViceOfThornsPvE.CanUse(out act))
    {
        return true;
    }
}

        if (SuicideByDumber && EngagementPvE.Cooldown.CurrentCharges == 1 && DisplacementPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        if (EngagementPvE.CanUse(out act, usedUp: HasEmbolden || !EmboldenPvE.EnoughLevel || EngagementPvE.Cooldown.WillHaveXChargesGCD(2, 1)))
        {
            return true;
        }

        if (!IsMoving && CorpsacorpsPvE.CanUse(out act, usedUp: HasEmbolden || !EmboldenPvE.EnoughLevel || CorpsacorpsPvE.Cooldown.WillHaveXChargesGCD(2, 1)))
        {
            return true;
        }

        return base.AttackAbility(nextGCD, out act);
    }

    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        if (HasEmbolden && InCombat && UseBurstMedicine(out act))
        {
            return true;
        }

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
        {
            return true;
        }

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
        {
            return true;
        }

        return base.RaiseGCD(out act);
    }

    protected override bool GeneralGCD(out IAction? act)
    {
        bool hasInstantBuffToSpend = HasDualcast || HasSwift || (IsOpen && HasAccelerate);

// ---------------------------
// Opener: first 5s of combat
// Countdown already gives Dualcast from Veraero.
// Spend instants on "2" (Thunder/Aero/Impact), never on "1" or Jolt.
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

        // Opener AoE rule:
        // - If Accel is up, Impact is worth it at 2+
        // - Otherwise, only at 3+
        int impactThreshold = HasAccelerate ? 2 : 3;

        if (targets >= impactThreshold && ImpactPvE.CanUse(out act))
            return true;

        // ST opener rule: always Verthunder line (ignore mana/procs)
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
				// Balance first
				if (diff > 0 && VerholyPvE.CanUse(out act)) return true; 
				if (diff < 0 && VerflarePvE.CanUse(out act)) return true; 
			}
			else
			{
				// Slight imbalance: proc-aware preference to avoid overwriting existing procs
				if (CanVerFire && VerholyPvE.CanUse(out act)) return true;
				if (CanVerStone && VerflarePvE.CanUse(out act)) return true;
			}

			// Fallbacks
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
            {
                return true;
            }
            if (WhiteMana < BlackMana)
            {
                if (VeraeroPvE.CanUse(out act) && BlackMana - WhiteMana != 6)
                {
                    return true;
                }
            }
            if (VerthunderPvE.CanUse(out act))
            {
                return true;
            }
        }

        // Hardcode Resolution & Scorch to avoid double melee without finishers
        if (IsLastGCD(ActionID.ScorchPvE))
        {
            if (ResolutionPvE.CanUse(out act, skipStatusProvideCheck: true))
            {
                return true;
            }
        }

        if (IsLastGCD(ActionID.VerholyPvE, ActionID.VerflarePvE))
        {
            if (ScorchPvE.CanUse(out act, skipStatusProvideCheck: true))
            {
                return true;
            }
        }
// ---------------------------
// Hold melee combo up to 2s if we cannot continue (usually out of range)
// Applies to BOTH ST and AoE melee combos.
// ---------------------------
if (HoldMeleeComboIfOutOfRange)
{
if (IsInMeleeCombo)
{
    // 1) If we can continue melee RIGHT NOW, do it immediately.
    if (TryContinueCurrentMeleeCombo(out act))
    {
        _meleeHoldUntilMs = 0;
        return true;
    }

    // 2) If we can't continue, start a 2-second hold window (one-shot).
    long now = Environment.TickCount64;

    // Start the hold window only once (don’t re-arm it repeatedly)
    if (_meleeHoldUntilMs == 0)
    {
        _meleeHoldUntilMs = now + 2000; // 2 seconds
    }

    // While holding, do not select any other GCD (prevents breaking combo)
    if (now < _meleeHoldUntilMs)
    {
        act = null;
        return false;
    }

    // Hold expired: clear the hold and resume normal behaviour immediately
    _meleeHoldUntilMs = 0;
    // fall through to normal GCD selection
}
else
{
    // Not in melee combo anymore -> make sure no stale hold state remains
    _meleeHoldUntilMs = 0;
}
}
else
{
    _meleeHoldUntilMs = 0;
}

//Melee AOE combo
if (IsLastGCD(false, EnchantedMoulinetDeuxPvE) && EnchantedMoulinetTroisPvE.CanUse(out act))
{
    return true;
}


        if (IsLastGCD(false, EnchantedMoulinetPvE) && EnchantedMoulinetDeuxPvE.CanUse(out act))
        {
            return true;
        }

		if (EnchantedRedoublementPvE_45962.CanUse(out act))
		{
			return true;
		}

		if (EnchantedRedoublementPvE.CanUse(out act))
        {
            return true;
        }

		if (EnchantedZwerchhauPvE_45961.CanUse(out act))
		{
			return true;
		}

		if (EnchantedZwerchhauPvE.CanUse(out act))
        {
            return true;
        }

// ---------------------------
// Pooling cap to prevent overcapping (waste) while waiting for burst.
// If either side is very high and the other is also high, stop pooling and allow melee.
// ---------------------------
bool poolCapReached =
    (BlackMana >= 92 && WhiteMana >= 81) ||
    (WhiteMana >= 92 && BlackMana >= 81);

// If pooling is enabled, use pooling threshold unless cap is reached (then behave like no-pooling).
bool EnoughMana =
    (!Pooling && EnoughManaComboNoPooling) ||
    (Pooling && (poolCapReached || EnoughManaComboPooling));

// ---------------------------
// Check if you can start melee combo
// ---------------------------
if (EnoughMana && !InFinisherChain())
{
    // Burst-start condition:
    // - Manafication active is good enough (we're committing to burst sequencing).
    // - Still allow your original intent: start within ~4 GCDs of Swordplay ending.
    // - Embolden+Swordplay remains valid, but is no longer required.
bool burstStartOK =
    !IsOpen &&
    (
        poolCapReached ||
        HasManafication ||
        StatusHelper.PlayerWillStatusEndGCD(4, 0, true, StatusID.MagickedSwordplay) ||
        (HasEmbolden && CanMagickedSwordplay)
    );

// -----------------------------------------
// Prefer AoE melee start at 3+ targets
// -----------------------------------------
if (NumberOfHostilesInRangeOf(5) >= 3)
{
    if (!IsLastGCD(false, EnchantedMoulinetPvE)
        && EnchantedMoulinetPvE.CanUse(out act))
    {
        return true;
    }
}

// -----------------------------------------
// Otherwise start ST melee with Riposte
// -----------------------------------------
// - Manafication -> prefer extended-range _45960
// - otherwise -> normal Riposte
// Treat both as the same starter to avoid double-Riposte issues.
if (burstStartOK && !IsLastRiposteStarter() && TryRiposteStarter(out act))
{
    return true;
}

}

		//Grand impact usage if not interrupting melee combo
		if (!IsOpenForGrandImpact && GrandImpactPvE.CanUse(out act, skipStatusProvideCheck: CanGrandImpact, skipCastingCheck: true))
{
    return true;
}

// ============================================================
// VerBoth + standstill:
// - If we have Dualcast/Swift, DO NOT spend them on "1" (proc).
//   Let the later "instant -> force a 2" logic handle it.
// - Otherwise, use proc "1" first to avoid proc overcap.
// ============================================================

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


// ============================================================
// Spend Acceleration on a "2" soon (but after Grand Impact)
// Conditions:
// - if NOT VerBoth (0 or 1 proc), OR
// - if moving + VerBoth and you have NO other movement resources
// ============================================================

// Treat "finishers window" as a movement resource you don't want to interrupt
bool finisherChain2 =
    ManaStacks == 3 ||
    IsLastGCD(ActionID.VerholyPvE, ActionID.VerflarePvE, ActionID.ScorchPvE) ||
    ScorchPvE.CanUse(out _) ||
    ResolutionPvE.CanUse(out _);

// Reprise as a "movement resource"
bool canRepriseNow2 =
    RangedSwordplay
    && ManaStacks == 0
    && (BlackMana < 50 || WhiteMana < 50)
    && EnchantedReprisePvE.CanUse(out _);

// "No other moving resources" (your definition)
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
    // AoE (same idea as your mover spender; adjust threshold if desired)
    if (NumberOfHostilesInRangeOf(5) >= 2 && ImpactPvE.CanUse(out act))
        return true;

    int diff = BlackMana - WhiteMana;

    bool TryAero2(out IAction? a)
    {
        if (VeraeroIiiPvE.CanUse(out a)) return true;
        if (VeraeroPvE.CanUse(out a)) return true;
        a = null;
        return false;
    }

    bool TryThunder2(out IAction? a)
    {
        if (VerthunderIiiPvE.CanUse(out a)) return true;
        if (VerthunderPvE.CanUse(out a)) return true;
        a = null;
        return false;
    }

    // Spend Accel on a "2" that helps balance mana
    if (diff > 0)
    {
        if (TryAero2(out act)) return true;
        if (TryThunder2(out act)) return true;
    }
    else
    {
        if (TryThunder2(out act)) return true;
        if (TryAero2(out act)) return true;
    }
}


if (ManaStacks == 3)
{
    return base.GeneralGCD(out act);
}

// ---------------------------
// Acceleration mover spender:
// If we're moving and Acceleration is active (but no Swift/Dual),
// spend the instant on a "2" immediately instead of letting procs ("1") win.
// This ensures Acceleration actually helps movement.
// ---------------------------
if (!IsInMeleeCombo
    && ManaStacks != 3
    && HasAccelerate
    && !HasSwift
    && !HasDualcast
    && InCombat
    && HasHostilesInMaxRange
    && IsMoving)
{
    int aoeTargets = 2; // Accel makes Impact attractive at low counts
    if (NumberOfHostilesInRangeOf(5) >= aoeTargets && ImpactPvE.CanUse(out act))
        return true;

    int diff = BlackMana - WhiteMana;

    bool TryAero2(out IAction? a)
    {
        if (VeraeroIiiPvE.CanUse(out a)) return true;
        if (VeraeroPvE.CanUse(out a)) return true;
        a = null;
        return false;
    }

    bool TryThunder2(out IAction? a)
    {
        if (VerthunderIiiPvE.CanUse(out a)) return true;
        if (VerthunderPvE.CanUse(out a)) return true;
        a = null;
        return false;
    }

    // Spend Accel on a "2" that helps balance mana
    if (diff > 0)
    {
        if (TryAero2(out act)) return true;      // Black leads -> add White
        if (TryThunder2(out act)) return true;
    }
    else
    {
        if (TryThunder2(out act)) return true;   // White leads -> add Black
        if (TryAero2(out act)) return true;
    }
}

// Can we fix movement with oGCDs right now?
// If yes, do NOT burn a GCD on Reprise.
// Accel hold rules (GCD-side mirror for movement rescue logic
// ---------------------------
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

// Can we fix movement with oGCDs right now?
// If yes, do NOT burn a GCD on Reprise.
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


// Define once and reuse for both Reprise + moving gate
bool hasInstantTools = HasSwift || HasDualcast || HasAccelerate || (!IsOpenForGrandImpact && CanGrandImpact);

// Reprise fallback (ONLY when we truly have no instant tools AND cannot rescue with oGCDs)
if (IsMoving
    && RangedSwordplay
    && ManaStacks == 0
    && (BlackMana < 50 || WhiteMana < 50)
    && !hasInstantTools
    && !canRescueMovementWithOgcd
    && EnchantedReprisePvE.CanUse(out act))
{
    return true;
}


// Moving cast gate:
// While moving with NO instant tools, do not attempt any casts (1 or 2).
if (IsMoving
    && InCombat
    && HasHostilesInMaxRange
    && ManaStacks != 3
    && !hasInstantTools)
{
    act = null;
    return false;
}

// ============================================================
// HARD RULE: If Swift/Dualcast is up, NEVER spend it on "1" (proc) or Jolt.
// Force a "2" (Thunder/Aero, or Impact at AoE threshold) right now.
// ============================================================

if (!IsInMeleeCombo
    && ManaStacks != 3
    && InCombat
    && (HasHostilesInRange || HasHostilesInMaxRange)
    && hasInstantBuffToSpend)
{
    // AoE: only use Impact when you'd actually want it
    if (NumberOfHostilesInRangeOf(5) >= 3 && ImpactPvE.CanUse(out act))
        return true;

    // ST: choose a "2" to help balance mana
    if (BlackMana > WhiteMana)
{
    // Black is leading -> add White
    if (VeraeroIiiPvE.CanUse(out act, skipStatusProvideCheck: true)) return true;
    if (VeraeroPvE.CanUse(out act, skipStatusProvideCheck: true)) return true;

    // fallback
    if (VerthunderIiiPvE.CanUse(out act, skipStatusProvideCheck: true)) return true;
    if (VerthunderPvE.CanUse(out act, skipStatusProvideCheck: true)) return true;
}
else
{
    // White is leading or equal -> add Black
    if (VerthunderIiiPvE.CanUse(out act, skipStatusProvideCheck: true)) return true;
    if (VerthunderPvE.CanUse(out act, skipStatusProvideCheck: true)) return true;

    // fallback
    if (VeraeroIiiPvE.CanUse(out act, skipStatusProvideCheck: true)) return true;
    if (VeraeroPvE.CanUse(out act, skipStatusProvideCheck: true)) return true;
}
}

        // Single Target
       if (VerstonePvE.EnoughLevel && !hasInstantBuffToSpend)
{
            if (CanVerBoth)
            {
                switch (VerEndsFirst)
                {
                    case "VerFire":
                        if (VerfirePvE.CanUse(out act))
                            return true;
                        break;
                    case "VerStone":
                        if (VerstonePvE.CanUse(out act))
                            return true;
                        break;
                    case "Equal":
                        if (WhiteMana < BlackMana)
                        {
                            if (VerstonePvE.CanUse(out act))
                                return true;
                        }
                        if (WhiteMana >= BlackMana)
                        {
                            if (VerfirePvE.CanUse(out act))
                                return true;
                        }
                        break;
                }
            }
            if (!CanVerBoth)
            {
                if (VerfirePvE.CanUse(out act))
                {
                    return true;
                }

                if (VerstonePvE.CanUse(out act))
                {
                    return true;
                }
            }
        }
     if (!VerstonePvE.EnoughLevel && !hasInstantBuffToSpend && VerfirePvE.CanUse(out act))
{
            return true;
        }

      if (!CanInstantCast && !CanVerEither)
{
    if (NumberOfHostilesInRangeOf(5) >= 3)
    {
        // AoE short-cast filler (only at 3+)
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

    // ST short-cast filler
    if (!hasInstantBuffToSpend && JoltPvE.CanUse(out act))
        return true;
}


        if (UseVercure && !InCombat && VercurePvE.CanUse(out act))
        {
            return true;
        }

        return base.GeneralGCD(out act);
    }
    #endregion

// Treat both Riposte variants as the same "starter".
// _45960 = extended-range version under Manafication.
private bool IsLastRiposteStarter()
{
    return IsLastGCD(true, EnchantedRipostePvE_45960) || IsLastGCD(true, EnchantedRipostePvE);
}

private bool TryRiposteStarter(out IAction? act)
{
    // Prefer the extended-range starter while Manafication is active.
    if (HasManafication && EnchantedRipostePvE_45960.CanUse(out act))
        return true;

    // Otherwise use the normal starter.
    if (EnchantedRipostePvE.CanUse(out act))
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