using System;
using System.ComponentModel;

namespace RotationSolver.ExtraRotations.Healer;

[Rotation("BeirutaAST", CombatType.PvE, GameVersion = "7.45")]
[SourceCode(Path = "main/ExtraRotations/Healer/BeirutaAST.cs")]
public sealed class BeirutaAST : AstrologianRotation
{
    #region Config Options

   [RotationConfig(CombatType.PvE, Name =
    "Please note that this rotation is optimised for high-end encounters.\n" +
    "• Collective Unconscious, Horoscope, Neutral Sect, and Macrocosmos should generally be used manually or through CD planner\n" +
    "• Please set Intercept for GCD usage only\n" +
    "• Disabling AutoBurst is sufficient if you need to delay burst timing in this rotation\n" +
    "• DoT effects may refresh slightly earlier during burst phases or while moving\n" +
    "• Lightspeed is managed automatically by the rotation and should not be used manually\n" +
    "• Single-target healing usage is intentionally more conservative in this rotation\n")]
public bool RotationNotes { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Opener/Burst open window (GCDs)")]
    [Range(0, 2, ConfigUnitType.None, 1)]
    public OpenWindowGcd OpenWindow { get; set; } = OpenWindowGcd.ThreeGcd; // default = 2 GCD

    public enum OpenWindowGcd : byte
    {
        [Description("0 GCD (0.0s)")] ZeroGcd,
        [Description("1 GCD (2.2s)")] OneGcd,
        [Description("2 GCD (5.0s)")] TwoGcd,
        [Description("Balance")] ThreeGcd,
    }

    [RotationConfig(CombatType.PvE, Name = "Automatically upgrade Horoscope with Helios/Aspected Helios")]
    public bool AutoUpgradeHoroscope { get; set; } = true;
    [RotationConfig(CombatType.PvE, Name = "Enable Swiftcast Restriction Logic to attempt to prevent actions other than Raise when you have swiftcast")]
    public bool SwiftLogic { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use GCDs to heal. (Ignored if you are the only healer in party)")]
    public bool GCDHeal { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Prioritize Microcosmos over all other healing when available")]
    public bool MicroPrio { get; set; } = false;

    [Range(4, 20, ConfigUnitType.Seconds)]
    [RotationConfig(CombatType.PvE, Name = "Use Earthly Star during countdown timer.")]
    public float UseEarthlyStarTime { get; set; } = 4;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum HP threshold party member needs to be to use Aspected Benefic")]
    public float AspectedBeneficHeal { get; set; } = 0.6f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum HP threshold party member needs to be to use Synastry")]
    public float SynastryHeal { get; set; } = 0.5f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum HP threshold among party member needed to pop Horoscope)")]
    public float HoroscopeHeal { get; set; } = 0.6f;

    // Microcosmos threshold (default 0.3f like Horoscope)
    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum HP threshold among party member needed to use Microcosmos")]
    public float MicrocosmosHeal { get; set; } = 0.4f;

    // NEW: Detonate Earthly Star based on PartyMembersAverHP (default 0.7f)
    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum average HP threshold among party members needed to detonate Earthly Star (when Giant Dominance)")]
    public float StellarDetonationHeal { get; set; } = 0.7f;

    // NEW: Celestial Opposition configurable threshold (default 0.7f) AND only when !HasGiantDominance
    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum average HP threshold among party members needed to use Celestial Opposition (only when NOT holding Giant Dominance)")]
    public float CelestialOppositionHeal { get; set; } = 0.7f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum average HP threshold among party members needed to use Lady Of Crowns")]
    public float LadyOfHeals { get; set; } = 0.8f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum HP threshold party member needs to be to use Essential Dignity 3rd charge")]
    public float EssentialDignityThird { get; set; } = 0.8f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum HP threshold party member needs to be to use Essential Dignity 2nd charge")]
    public float EssentialDignitySecond { get; set; } = 0.7f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum HP threshold party member needs to be to use Essential Dignity last charge")]
    public float EssentialDignityLast { get; set; } = 0.6f;

    [RotationConfig(CombatType.PvE, Name = "Prioritize Essential Dignity over single target GCD heals when available")]
    public EssentialPrioStrategy EssentialPrio2 { get; set; } = EssentialPrioStrategy.UseGCDs;

    public enum EssentialPrioStrategy : byte
    {
        [Description("Ignore setting")]
        UseGCDs,

        [Description("When capped")]
        CappedCharges,

        [Description("Any charges")]
        AnyCharges,
    }
    #endregion

    // Opener window seconds based on GCD selection
    private float OpenWindowSeconds => OpenWindow switch
    {
        OpenWindowGcd.ZeroGcd => 0f,
        OpenWindowGcd.OneGcd => 2.2f,
        OpenWindowGcd.TwoGcd => 5.1f,
        OpenWindowGcd.ThreeGcd => 7.2f,
        _ => 5.5f,
    };

    // Opener/burst open window active?
    private bool IsOpen => InCombat && CombatTime < OpenWindowSeconds;

// Neutral Sect timing (stamp when buff is gained)
private long _neutralSectUsedAtMs = 0;
private bool _neutralSectWasUp = false;
private const long NeutralSectEarlyMs = 15_000;

private bool CardsUnderDivinationOnly { get; set; } = true;

private void RefreshNeutralSectStamp()
{
    bool isUpNow = HasNeutralSect; // use the inherited AstrologianRotation.HasNeutralSect (buff on you)

    // Rising edge: Neutral Sect just became active
    if (isUpNow && !_neutralSectWasUp)
    {
        _neutralSectUsedAtMs = Environment.TickCount64;
    }

    _neutralSectWasUp = isUpNow;

    // Optional hygiene: clear stamp when it falls off
    if (!isUpNow)
    {
        _neutralSectUsedAtMs = 0;
    }
}

private bool InFirst15sAfterNeutralSect
{
    get
    {
        if (_neutralSectUsedAtMs == 0) return false;
        return (Environment.TickCount64 - _neutralSectUsedAtMs) <= NeutralSectEarlyMs;
    }
}

    #region Divination / Oracle helpers (timestamp gating)

    // Track when we actually used Divination so we can gate Oracle / early-window logic reliably.
    private long _divinationUsedAtMs = 0;
    private const long DivinationFirst5sMs = 5000;

    private bool HasHeliosConjunction => StatusHelper.PlayerHasStatus(true, StatusID.HeliosConjunction);
    private bool HasAspectedHelios    => StatusHelper.PlayerHasStatus(true, StatusID.AspectedHelios);
    private bool HasDivining          => StatusHelper.PlayerHasStatus(true, StatusID.Divining);
    private bool HasHoroscopeHelios   => StatusHelper.PlayerHasStatus(true, StatusID.HoroscopeHelios);
    private bool HasHoroscope   => StatusHelper.PlayerHasStatus(true, StatusID.Horoscope);

    private bool InFirst5sAfterDivination
    {
        get
        {
            if (_divinationUsedAtMs == 0) return false;
            long now = Environment.TickCount64;
            return (now - _divinationUsedAtMs) < DivinationFirst5sMs;
        }
    }

    // Gate Oracle for 5s after using Divination.
    private bool OracleGatedByDivination => InFirst5sAfterDivination;

    #endregion

    #region Cooldown timing helpers (PCT-style "time until ready")

    // Time until Divination has 1 charge (0 if already active/usable)
    private float DivIn =>
        DivinationPvE.Cooldown.CurrentCharges >= 1
            ? 0f
            : DivinationPvE.Cooldown.RecastTimeRemainOneCharge;

    // Time until Lightspeed gains the next charge:
    // - If capped (2/2): 0
    // - If 1/2: time until 2/2
    // - If 0/2: time until 1/2
    private float LightspeedNextChargeIn =>
        LightspeedPvE.Cooldown.CurrentCharges >= LightspeedPvE.Cooldown.MaxCharges
            ? 0f
            : LightspeedPvE.Cooldown.RecastTimeRemainOneCharge;

    // "Burst prep" replaces divReadySoon2, using the real time-to-ready instead of a yes/no.
    // (4s window)
    private bool BurstPrep
    {
        get
        {
            if (!DivinationPvE.EnoughLevel) return false;
            return DivIn <= 4f;
        }
    }

    // Hold last Lightspeed charge for Divination unless it will naturally regain a charge
    // at least 4s BEFORE Divination comes up.
    private bool HoldLastLightspeedForDivination
    {
        get
        {
            if (!DivinationPvE.EnoughLevel) return false;

            // only matters when Divination is within a relevant horizon
            bool divSoon60 = DivIn <= 60f;
            if (!divSoon60) return false;

            // don’t bother holding if we’re in the "prep" window already
            if (BurstPrep) return false;

            // only consider holding if we're on the last charge
            if (LightspeedPvE.Cooldown.CurrentCharges != 1) return false;

            // don't hold if Lightspeed buff is already up (we already spent it)
            if (HasLightspeed) return false;

            // Allow spending last LS if LS will regain by (DivIn - 4s)
            float lsMustBeBackBy = MathF.Max(0f, DivIn - 4f);
            bool spendingLastLsIsSafe = LightspeedNextChargeIn <= lsMustBeBackBy;

            return !spendingLastLsIsSafe;
        }
    }

    #endregion

    #region Tracking Properties
    public override void DisplayRotationStatus()
    {
        ImGui.Text($"Suntouched 1: {StatusHelper.PlayerWillStatusEndGCD(1, 0, true, StatusID.Suntouched)}");
        ImGui.Text($"Suntouched 2: {StatusHelper.PlayerWillStatusEndGCD(2, 0, true, StatusID.Suntouched)}");
        ImGui.Text($"Suntouched 3: {StatusHelper.PlayerWillStatusEndGCD(3, 0, true, StatusID.Suntouched)}");
        ImGui.Text($"Suntouched 4: {StatusHelper.PlayerWillStatusEndGCD(4, 0, true, StatusID.Suntouched)}");
        ImGui.Text($"Suntouched Time: {StatusHelper.PlayerStatusTime(true, StatusID.Suntouched)}");
    }
    #endregion

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (remainTime < MaleficPvE.Info.CastTime + CountDownAhead && MaleficPvE.CanUse(out IAction? act))
        {
            return act;
        }

        if (remainTime < 3 && UseBurstMedicine(out act))
        {
            return act;
        }

        if (remainTime < UseEarthlyStarTime && EarthlyStarPvE.CanUse(out act, skipTTKCheck: true))
        {
            return act;
        }

        return remainTime < 30 && AstralDrawPvE.CanUse(out act) ? act : base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        act = null;

        if (!HasLightspeed
            && InCombat
            && IsOpen
            && LightspeedPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        if (MicroPrio && HasMacrocosmos)
        {
            return base.EmergencyAbility(nextGCD, out act);
        }

        if (!InCombat)
        {
            return base.EmergencyAbility(nextGCD, out act);
        }


        if (SynastryPvE.CanUse(out act))
        {
            if (CanCastSynastry(AspectedBeneficPvE, SynastryPvE, SynastryHeal, nextGCD) ||
                CanCastSynastry(BeneficIiPvE, SynastryPvE, SynastryHeal, nextGCD) ||
                CanCastSynastry(BeneficPvE, SynastryPvE, SynastryHeal, nextGCD))
            {
                return true;
            }
        }

        // Burst-prep Lightspeed (same as your intent, but BurstPrep is now DivIn<=4)
        if (BurstPrep
            && LightspeedPvE.Cooldown.CurrentCharges >= 1
            && !HasLightspeed
            && InCombat
            && IsBurst
            && LightspeedPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

// Burst-prep potion
if (!IsOpen
    && InCombat
    && IsBurst
    && BurstPrep
    && UseBurstMedicine(out act))
{
    return true;
}
        // Divination here does NOT need !HasLightspeed
        if (!IsOpen && IsBurst && InCombat && DivinationPvE.CanUse(out act))
        {
            _divinationUsedAtMs = Environment.TickCount64; // stamp for gating + early window
            return true;
        }

        if (!IsOpen && DivinationPvE.CanUse(out _) && UseBurstMedicine(out act))
        {
            return true;
        }

        return base.EmergencyAbility(nextGCD, out act);

        static bool CanCastSynastry(IBaseAction actionCheck, IBaseAction synastry, float synastryHp, IAction next)
            => next.IsTheSameTo(false, actionCheck) &&
               synastry.Target.Target == actionCheck.Target.Target &&
               synastry.Target.Target.GetHealthRatio() < synastryHp;
    }

    [RotationDesc(ActionID.ExaltationPvE, ActionID.TheArrowPvE, ActionID.TheSpirePvE, ActionID.TheBolePvE, ActionID.TheEwerPvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (!HasDivining && InCombat && TheSpirePvE.CanUse(out act))
        {
            return true;
        }

        if (!HasDivining && InCombat && TheBolePvE.CanUse(out act))
        {
            return true;
        }

        if (ExaltationPvE.CanUse(out act))
        {
            return true;
        }

        if (CelestialIntersectionPvE.Cooldown.CurrentCharges == 1
            && CelestialIntersectionPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        return base.DefenseSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.CollectiveUnconsciousPvE, ActionID.SunSignPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (SunSignPvE.CanUse(out act))
        {
            return true;
        }

        if (CollectiveUnconsciousPvE.CanUse(out act))
        {
            return true;
        }

        return base.DefenseAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.TheArrowPvE, ActionID.TheEwerPvE, ActionID.EssentialDignityPvE, ActionID.CelestialIntersectionPvE)]
    protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        act = null;

        // Gate all healing (Single Ability) when HasMacrocosmos
        if (!HasDivining && HasMacrocosmos && HasGiantDominance && !HasEarthlyDominance)
        {
            return false;
        }

        if (MicroPrio && HasMacrocosmos)
        {
            return base.HealSingleAbility(nextGCD, out act);
        }

        if (!IsOpen && InCombat && TheArrowPvE.CanUse(out act))
        {
            return true;
        }

        if (InCombat && TheEwerPvE.CanUse(out act)
        && (TheEwerPvE.Target.Target?.GetHealthRatio() < 0.8f) == true)
        {
            return true;
        }

        if (EssentialDignityPvE.Cooldown.CurrentCharges == 3 &&
            PartyMembersAverHP > 0.6f &&
            EssentialDignityPvE.CanUse(out act, usedUp: true) &&
            EssentialDignityPvE.Target.Target.GetHealthRatio() < EssentialDignityThird)
        {
            return true;
        }

        if (EssentialDignityPvE.Cooldown.CurrentCharges == 2 &&
            PartyMembersAverHP > 0.6f &&
            EssentialDignityPvE.CanUse(out act, usedUp: true) &&
            EssentialDignityPvE.Target.Target.GetHealthRatio() < EssentialDignitySecond)
        {
            return true;
        }

        if (EssentialDignityPvE.Cooldown.CurrentCharges == 1 &&
            PartyMembersAverHP > 0.7f &&
            EssentialDignityPvE.CanUse(out act, usedUp: true) &&
            EssentialDignityPvE.Target.Target.GetHealthRatio() < EssentialDignityLast)
        {
            return true;
        }

        if (CelestialIntersectionPvE.Cooldown.CurrentCharges == 2
            && (CelestialIntersectionPvE.Target.Target?.GetHealthRatio() < 0.9f) == true
            && CelestialIntersectionPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        if (CelestialIntersectionPvE.Cooldown.CurrentCharges == 1
            && (CelestialIntersectionPvE.Target.Target?.GetHealthRatio() < 0.8f) == true
            && CelestialIntersectionPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        return base.HealSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.CelestialOppositionPvE, ActionID.StellarDetonationPvE, ActionID.HoroscopePvE, ActionID.HoroscopePvE_16558, ActionID.LadyOfCrownsPvE)]
    protected override bool HealAreaAbility(IAction nextGCD, out IAction? act)
    {
        act = null;

        // Gate all healing (Area Ability) when HasMacrocosmos
        if (!HasDivining && HasMacrocosmos)
        {
            return false;
        }

        // NEW: Detonate Earthly Star thresholded by party average HP, only when holding Giant Dominance
        if (HasGiantDominance
            && PartyMembersAverHP < StellarDetonationHeal
            && StellarDetonationPvE.CanUse(out act))
        {
            return true;
        }

        // Microcosmos thresholded (default 0.3f)
        if (PartyMembersAverHP < MicrocosmosHeal && MicrocosmosPvE.CanUse(out act))
        {
            return true;
        }

        if (MicroPrio && HasMacrocosmos)
        {
            return base.HealAreaAbility(nextGCD, out act);
        }

        // NEW: Celestial Opposition thresholded and only when NOT holding Giant Dominance
        if (!HasGiantDominance
            && PartyMembersAverHP < CelestialOppositionHeal
            && CelestialOppositionPvE.CanUse(out act))
        {
            return true;
        }

        if (!HasHoroscope && HasHoroscopeHelios && PartyMembersAverHP < HoroscopeHeal && HoroscopePvE_16558.CanUse(out act))
        {
            return true;
        }

        if (!HasHoroscope && HasHoroscopeHelios && PartyMembersAverHP < HoroscopeHeal && HoroscopePvE.CanUse(out act))
        {
            return true;
        }

        if (LadyOfCrownsPvE.CanUse(out act))
        {
            return true;
        }

        return base.HealAreaAbility(nextGCD, out act);
    }

    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        act = null;

        if (StatusHelper.PlayerHasStatus(true, StatusID.Suntouched) &&
            StatusHelper.PlayerWillStatusEndGCD(3, 0, true, StatusID.Suntouched))
        {
            if (SunSignPvE.CanUse(out act, skipAoeCheck: true, skipTTKCheck: true))
            {
                return true;
            }
        }

        if (PartyMembersAverHP < LadyOfHeals && LadyOfCrownsPvE.CanUse(out act))
        {
            return true;
        }

        if (AstralDrawPvE.Cooldown.WillHaveOneCharge(5) && LadyOfCrownsPvE.CanUse(out act))
        {
            return true;
        }

        if (AstralDrawPvE.CanUse(out act))
        {
            return true;
        }

        bool divLearned = DivinationPvE.EnoughLevel;

        // Cards gating (default: only under Divination if learned)
        bool burstCardsAllowed =
            CardsUnderDivinationOnly
                ? (!divLearned || HasDivination)
                : (HasDivination || !DivinationPvE.Cooldown.WillHaveOneCharge(66) || !divLearned);

        if (burstCardsAllowed && InCombat && TheBalancePvE.CanUse(out act))
        {
            return true;
        }

        // Lord of Crowns usage rules
        if (!IsOpen && InCombat && LordOfCrownsPvE.CanUse(out act))
        {
            if (CardsUnderDivinationOnly)
            {
                // Only under Divination when learned; if not learned, spend freely.
                if (!divLearned || HasDivination)
                {
                    return true;
                }
            }
            else
            {
                bool divinationLearned = divLearned;

                if ((divinationLearned && HasDivination)
                    || (!divinationLearned)
                    || (divinationLearned && !DivinationPvE.Cooldown.WillHaveOneCharge(60))
                    || UmbralDrawPvE.Cooldown.WillHaveOneCharge(3))
                {
                    return true;
                }
            }
        }

        // Gate Umbral Draw if we can spend Balance (or Spear) and Lord first
        bool hasBurstCardToPlay =
            InCombat && burstCardsAllowed && (TheBalancePvE.CanUse(out _) || TheSpearPvE.CanUse(out _));

        bool hasLordToSpend =
            InCombat && LordOfCrownsPvE.CanUse(out _);

        if (UmbralDrawPvE.CanUse(out act) && !(hasBurstCardToPlay && hasLordToSpend))
        {
            return true;
        }

        if (burstCardsAllowed && InCombat && TheSpearPvE.CanUse(out act))
        {
            return true;
        }

        // Gate Oracle for 5s after Divination (timestamp-based)
        if (InCombat && !OracleGatedByDivination && OraclePvE.CanUse(out act))
        {
            return true;
        }

        if (!HasDivining && AstralDrawPvE.Cooldown.WillHaveOneCharge(10) && InCombat && TheEwerPvE.CanUse(out act))
        {
            return true;
        }

        if (!HasDivining && AstralDrawPvE.Cooldown.WillHaveOneCharge(10) && InCombat && TheBolePvE.CanUse(out act))
        {
            return true;
        }

        if (!HasDivining && !IsOpen && UmbralDrawPvE.Cooldown.WillHaveOneCharge(10) && InCombat && TheArrowPvE.CanUse(out act))
        {
            return true;
        }

        if (UmbralDrawPvE.Cooldown.WillHaveOneCharge(10) && InCombat && TheSpirePvE.CanUse(out act))
        {
            return true;
        }

        return base.GeneralAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        act = null;

        // Only these GCDs are allowed while moving without needing Lightspeed
        bool nextIsMovementSafeGcd =
            nextGCD.IsTheSameTo(false,
                MacrocosmosPvE,
                AspectedBeneficPvE,
                CombustIiiPvE, CombustIiPvE, CombustPvE);

        // True if Combust is missing or will fall off within 6s
        bool combustSoon6 =
            CurrentTarget != null &&
            (
                (CombustIiiPvE.EnoughLevel &&
                    (!(CurrentTarget?.HasStatus(true, StatusID.CombustIii) ?? false)
                     || (CurrentTarget?.WillStatusEnd(6, true, StatusID.CombustIii) ?? false)))
                ||
                (!CombustIiiPvE.EnoughLevel && CombustIiPvE.EnoughLevel &&
                    (!(CurrentTarget?.HasStatus(true, StatusID.CombustIi) ?? false)
                     || (CurrentTarget?.WillStatusEnd(6, true, StatusID.CombustIi) ?? false)))
                ||
                (!CombustIiiPvE.EnoughLevel && !CombustIiPvE.EnoughLevel && CombustPvE.EnoughLevel &&
                    (!(CurrentTarget?.HasStatus(true, StatusID.Combust) ?? false)
                     || (CurrentTarget?.WillStatusEnd(6, true, StatusID.Combust) ?? false)))
            );

        bool needsMovementRescue =
            InCombat
            && IsMoving
            && !nextIsMovementSafeGcd
            && !HasSwift
            && !HasLightspeed
            && !combustSoon6;

        // First 5 seconds after Divination (timestamp-based, reliable)
        bool divJustStarted = InFirst5sAfterDivination;

        // Use Lightspeed once during opener window
        bool openerLightspeed =
            IsOpen &&
            InCombat &&
            !HasLightspeed &&
            !HoldLastLightspeedForDivination &&
            LightspeedPvE.Cooldown.CurrentCharges >= 1;


        // Divination does NOT need !HasLightspeed
        if (!IsOpen && IsBurst && InCombat && DivinationPvE.CanUse(out act))
        {
            _divinationUsedAtMs = Environment.TickCount64; // stamp for gating + early window
            return true;
        }

        // Opener Lightspeed
        if (openerLightspeed && LightspeedPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        if (AstralDrawPvE.CanUse(out act, usedUp: IsBurst))
        {
            return true;
        }

        // Divination early window Lightspeed (first 5s after Divination)
        if (!HasLightspeed
            && InCombat
            && HasDivination
            && divJustStarted
            && !HoldLastLightspeedForDivination
            && LightspeedPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        if (InCombat)
        {
            bool canWeaveNow = NextAbilityToNextGCD < 0.6f;

            // Movement rescue
            if (needsMovementRescue
                && canWeaveNow
                && !HoldLastLightspeedForDivination
                && LightspeedPvE.CanUse(out act, usedUp: true))
            {
                return true;
            }

            // Earthly Star
            if (!HasGiantDominance && !HasEarthlyDominance && EarthlyStarPvE.CanUse(out act))
            {
                return true;
            }
        }

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    protected override bool DefenseSingleGCD(out IAction? act)
    {
        if ((MacrocosmosPvE.Cooldown.IsCoolingDown && !MacrocosmosPvE.Cooldown.WillHaveOneCharge(150))
            || (CollectiveUnconsciousPvE.Cooldown.IsCoolingDown && !CollectiveUnconsciousPvE.Cooldown.WillHaveOneCharge(40)))
        {
            return base.DefenseAreaGCD(out act);
        }

        if ((NeutralSectPvE.CanUse(out _) || HasNeutralSect || IsLastAbility(false, NeutralSectPvE)) &&
            AspectedBeneficPvE.CanUse(out act, skipStatusProvideCheck: true))
        {
            return true;
        }

        return base.DefenseAreaGCD(out act);
    }

    [RotationDesc(ActionID.MacrocosmosPvE)]
    protected override bool DefenseAreaGCD(out IAction? act)
    {
        if ((MacrocosmosPvE.Cooldown.IsCoolingDown && !MacrocosmosPvE.Cooldown.WillHaveOneCharge(150))
            || (CollectiveUnconsciousPvE.Cooldown.IsCoolingDown && !CollectiveUnconsciousPvE.Cooldown.WillHaveOneCharge(40)))
        {
            return base.DefenseAreaGCD(out act);
        }

        if ((NeutralSectPvE.CanUse(out _) || HasNeutralSect || IsLastAbility(false, NeutralSectPvE)) &&
            HeliosConjunctionPvE.CanUse(out act, skipStatusProvideCheck: true))
        {
            return true;
        }

        return base.DefenseAreaGCD(out act);
    }

    [RotationDesc(ActionID.AspectedBeneficPvE, ActionID.BeneficIiPvE, ActionID.BeneficPvE)]
    protected override bool HealSingleGCD(out IAction? act)
    {
        act = null;

        // Gate all healing (Single GCD) when HasMacrocosmos
        if (HasMacrocosmos && HasGiantDominance && !HasEarthlyDominance)
        {
            return false;
        }

        if ((HasSwift || IsLastAction(ActionID.SwiftcastPvE)) && SwiftLogic && MergedStatus.HasFlag(AutoStatus.Raise))
        {
            return base.HealSingleGCD(out act);
        }

        if (MicroPrio && HasMacrocosmos)
        {
            return base.HealSingleGCD(out act);
        }

        var shouldUseEssentialDignity =
            (EssentialPrio2 == EssentialPrioStrategy.AnyCharges && EssentialDignityPvE.EnoughLevel &&
             EssentialDignityPvE.Cooldown.CurrentCharges > 0) ||
            (EssentialPrio2 == EssentialPrioStrategy.CappedCharges && EssentialDignityPvE.EnoughLevel &&
             EssentialDignityPvE.Cooldown.CurrentCharges == EssentialDignityPvE.Cooldown.MaxCharges);

        if (shouldUseEssentialDignity)
        {
            return base.HealSingleGCD(out act);
        }

        bool movingHealWindow =
            InCombat &&
            IsMoving &&
            !HoldLastLightspeedForDivination &&
            NextAbilityToNextGCD < 0.6f &&
            (AspectedBeneficPvE.Target.Target?.GetHealthRatio() < 0.8f) == true;

        if (AspectedBeneficPvE.CanUse(out act)
            && (AspectedBeneficPvE.Target.Target?.GetHealthRatio() < AspectedBeneficHeal && PartyMembersAverHP > 0.7f || movingHealWindow))
        {
            return true;
        }

        if (PartyMembersAverHP > 0.8f && BeneficIiPvE.CanUse(out act))
        {
            return true;
        }

        if (PartyMembersAverHP > 0.8f && BeneficPvE.CanUse(out act))
        {
            return true;
        }

        return base.HealSingleGCD(out act);
    }

    [RotationDesc(ActionID.AspectedHeliosPvE, ActionID.HeliosPvE, ActionID.HeliosConjunctionPvE)]
    protected override bool HealAreaGCD(out IAction? act)
    {
        act = null;

        // Gate all healing (Area GCD) when HasMacrocosmos
        if (HasMacrocosmos && HasGiantDominance && !HasEarthlyDominance)
        {
            return false;
        }

        if ((HasSwift || IsLastAction(ActionID.SwiftcastPvE)) && SwiftLogic && MergedStatus.HasFlag(AutoStatus.Raise))
        {
            return base.HealAreaGCD(out act);
        }

        if (MicroPrio && HasMacrocosmos)
        {
            return base.HealAreaGCD(out act);
        }

        if (!HasHeliosConjunction && PartyMembersAverHP < 0.6f && HeliosConjunctionPvE.EnoughLevel && HeliosConjunctionPvE.CanUse(out act))
        {
            return true;
        }

        if (!HasAspectedHelios && PartyMembersAverHP < 0.6f && !HeliosConjunctionPvE.EnoughLevel && AspectedHeliosPvE.CanUse(out act))
        {
            return true;
        }

        if ((HasHeliosConjunction || HasAspectedHelios) && PartyMembersAverHP < 0.4f && HeliosPvE.CanUse(out act))
        {
            return true;
        }

        return base.HealAreaGCD(out act);
    }

    [RotationDesc(ActionID.AscendPvE)]
    protected override bool RaiseGCD(out IAction? act)
    {
        if (AscendPvE.CanUse(out act))
        {
            return true;
        }

        return base.RaiseGCD(out act);
    }

    protected override bool GeneralGCD(out IAction? act)
    {
        RefreshNeutralSectStamp();
        if ((HasSwift || IsLastAction(ActionID.SwiftcastPvE)) && SwiftLogic && MergedStatus.HasFlag(AutoStatus.Raise))
        {
            return base.GeneralGCD(out act);
        }

if (AutoUpgradeHoroscope &&
   ((HasHoroscope && !HasHoroscopeHelios) ||
    (InFirst15sAfterNeutralSect && !HasHeliosConjunction && !HasAspectedHelios)))
{
    // Prefer Helios Conjunction if available
    if (HeliosConjunctionPvE.EnoughLevel && HeliosConjunctionPvE.CanUse(out act, skipStatusProvideCheck: true))
        return true;

    // Otherwise fall back to Aspected Helios
    if (!HeliosConjunctionPvE.EnoughLevel && AspectedHeliosPvE.CanUse(out act, skipStatusProvideCheck: true))
        return true;
}

        if (GravityIiPvE.EnoughLevel && GravityIiPvE.CanUse(out act))
        {
            return true;
        }
        if (!GravityIiPvE.EnoughLevel && GravityPvE.EnoughLevel && GravityPvE.CanUse(out act))
        {
            return true;
        }

        // Moving Combust refresh (<6s) with timing gate (0.6f)
        {
            bool canCommitGcdNow = NextAbilityToNextGCD < 0.6f;

            if (InCombat && IsMoving && canCommitGcdNow && CurrentTarget != null)
            {
                bool combustLow6 =
                    (CombustIiiPvE.EnoughLevel &&
                        (!(CurrentTarget?.HasStatus(true, StatusID.CombustIii) ?? false)
                         || (CurrentTarget?.WillStatusEnd(6, true, StatusID.CombustIii) ?? false)))
                    ||
                    (!CombustIiiPvE.EnoughLevel && CombustIiPvE.EnoughLevel &&
                        (!(CurrentTarget?.HasStatus(true, StatusID.CombustIi) ?? false)
                         || (CurrentTarget?.WillStatusEnd(6, true, StatusID.CombustIi) ?? false)))
                    ||
                    (!CombustIiiPvE.EnoughLevel && !CombustIiPvE.EnoughLevel && CombustPvE.EnoughLevel &&
                        (!(CurrentTarget?.HasStatus(true, StatusID.Combust) ?? false)
                         || (CurrentTarget?.WillStatusEnd(6, true, StatusID.Combust) ?? false)));

                if (combustLow6)
                {
                    if (CombustIiiPvE.EnoughLevel && CombustIiiPvE.CanUse(out act, skipStatusProvideCheck: true)) return true;
                    if (!CombustIiiPvE.EnoughLevel && CombustIiPvE.EnoughLevel && CombustIiPvE.CanUse(out act, skipStatusProvideCheck: true)) return true;
                    if (!CombustIiPvE.EnoughLevel && CombustPvE.EnoughLevel && CombustPvE.CanUse(out act, skipStatusProvideCheck: true)) return true;
                }
            }
        }

        // Force earlier Combust refresh during Divination: refresh if remaining < 11s
        if (HasDivination && InCombat && CurrentTarget != null)
        {
            bool combustMissingOrLow =
                (CombustIiiPvE.EnoughLevel &&
                    (!(CurrentTarget?.HasStatus(true, StatusID.CombustIii) ?? false)
                     || (CurrentTarget?.WillStatusEnd(11, true, StatusID.CombustIii) ?? false)))
                ||
                (!CombustIiiPvE.EnoughLevel && CombustIiPvE.EnoughLevel &&
                    (!(CurrentTarget?.HasStatus(true, StatusID.CombustIi) ?? false)
                     || (CurrentTarget?.WillStatusEnd(11, true, StatusID.CombustIi) ?? false)))
                ||
                (!CombustIiiPvE.EnoughLevel && !CombustIiPvE.EnoughLevel && CombustPvE.EnoughLevel &&
                    (!(CurrentTarget?.HasStatus(true, StatusID.Combust) ?? false)
                     || (CurrentTarget?.WillStatusEnd(11, true, StatusID.Combust) ?? false)));

            if (combustMissingOrLow)
            {
                if (CombustIiiPvE.EnoughLevel && CombustIiiPvE.CanUse(out act, skipStatusProvideCheck: true)) return true;
                if (!CombustIiiPvE.EnoughLevel && CombustIiPvE.EnoughLevel && CombustIiPvE.CanUse(out act, skipStatusProvideCheck: true)) return true;
                if (!CombustIiPvE.EnoughLevel && CombustPvE.EnoughLevel && CombustPvE.CanUse(out act, skipStatusProvideCheck: true)) return true;
            }
        }

        if (CombustIiiPvE.EnoughLevel && CombustIiiPvE.CanUse(out act))
        {
            return true;
        }
        if (!CombustIiiPvE.EnoughLevel && CombustIiPvE.EnoughLevel && CombustIiPvE.CanUse(out act))
        {
            return true;
        }
        if (!CombustIiPvE.EnoughLevel && CombustPvE.EnoughLevel && CombustPvE.CanUse(out act))
        {
            return true;
        }

        if (FallMaleficPvE.EnoughLevel && FallMaleficPvE.CanUse(out act))
        {
            return true;
        }
        if (!FallMaleficPvE.EnoughLevel && MaleficIvPvE.EnoughLevel && MaleficIvPvE.CanUse(out act))
        {
            return true;
        }
        if (!MaleficIvPvE.EnoughLevel && MaleficIiiPvE.EnoughLevel && MaleficIiiPvE.CanUse(out act))
        {
            return true;
        }
        if (!MaleficIiiPvE.EnoughLevel && MaleficIiPvE.EnoughLevel && MaleficIiPvE.CanUse(out act))
        {
            return true;
        }
        if (!MaleficIiPvE.Info.EnoughLevelAndQuest() && MaleficPvE.CanUse(out act))
        {
            return true;
        }

        return base.GeneralGCD(out act);
    }
    #endregion

    #region Extra Methods
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

            return base.CanHealSingleSpell && (GCDHeal || aliveHealerCount == 1);
        }
    }

    public override bool CanHealAreaSpell
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

            return base.CanHealAreaSpell && (GCDHeal || aliveHealerCount == 1);
        }
    }
    #endregion
}