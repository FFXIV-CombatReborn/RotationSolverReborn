using System.ComponentModel;

namespace RotationSolver.ExtraRotations.Healer;

[Rotation("BeirutaAST", CombatType.PvE, GameVersion = "7.41")]
[SourceCode(Path = "main/ExtraRotations/Healer/BeirutaAST.cs")]

public sealed class AST_Reborn : AstrologianRotation
{
    #region Config Options
[RotationConfig(CombatType.PvE, Name = "Opener/Burst open window (GCDs)")]
[Range(0, 2, ConfigUnitType.None, 1)]
public OpenWindowGcd OpenWindow { get; set; } = OpenWindowGcd.TwoGcd; // default = 2 GCD

public enum OpenWindowGcd : byte
{
    [Description("0 GCD (0.0s)")] ZeroGcd,
    [Description("1 GCD (2.5s)")] OneGcd,
    [Description("2 GCD (5.0s)")] TwoGcd,
}
    [RotationConfig(CombatType.PvE, Name = "Limit Macrocosmos to multihit party stacks")]
    public bool MultiHitRestrict { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Enable Swiftcast Restriction Logic to attempt to prevent actions other than Raise when you have swiftcast")]
    public bool SwiftLogic { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use GCDs to heal. (Ignored if you are the only healer in party)")]
    public bool GCDHeal { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Prioritize Microcosmos over all other healing when available")]
    public bool MicroPrio { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Detonate Earlthy Star when you have Giant Dominance")]
    public bool StellarNow { get; set; } = false;

    [Range(4, 20, ConfigUnitType.Seconds)]
    [RotationConfig(CombatType.PvE, Name = "Use Earthly Star during countdown timer.")]
    public float UseEarthlyStarTime { get; set; } = 4;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum HP threshold party member needs to be to use Aspected Benefic")]
    public float AspectedBeneficHeal { get; set; } = 0.4f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum HP threshold party member needs to be to use Synastry")]
    public float SynastryHeal { get; set; } = 0.5f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum HP threshold among party member needed to use Horoscope")]
    public float HoroscopeHeal { get; set; } = 0.3f;

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
    OpenWindowGcd.OneGcd  => 2.2f,
    _                     => 5.1f, // TwoGcd
};

// Opener/burst open window active?
private bool IsOpen => InCombat && CombatTime < OpenWindowSeconds;
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
        if (MicroPrio && HasMacrocosmos)
        {
            return base.EmergencyAbility(nextGCD, out act);
        }

        if (!InCombat)
        {
            return base.EmergencyAbility(nextGCD, out act);
        }

        if (nextGCD.IsTheSameTo(false, HeliosConjunctionPvE, AspectedHeliosPvE))
        {
            if (NeutralSectPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (nextGCD.IsTheSameTo(false, HeliosConjunctionPvE, HeliosPvE))
        {
            if (PartyMembersAverHP < HoroscopeHeal && HoroscopePvE.CanUse(out act))
            {
                return true;
            }
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

        if (!IsOpen && DivinationPvE.CanUse(out _) && UseBurstMedicine(out act))
{
    return true;
}

        if (StellarNow && HasGiantDominance && StellarDetonationPvE.CanUse(out act))
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
        if (InCombat && TheSpirePvE.CanUse(out act))
        {
            return true;
        }

        if (InCombat && TheBolePvE.CanUse(out act))
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

        if ((MacrocosmosPvE.Cooldown.IsCoolingDown && !MacrocosmosPvE.Cooldown.WillHaveOneCharge(150))
            || (CollectiveUnconsciousPvE.Cooldown.IsCoolingDown && !CollectiveUnconsciousPvE.Cooldown.WillHaveOneCharge(40)))
        {
            return base.DefenseAreaAbility(nextGCD, out act);
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
        if (MicroPrio && HasMacrocosmos)
        {
            return base.HealSingleAbility(nextGCD, out act);
        }

        if (InCombat && TheArrowPvE.CanUse(out act))
        {
            return true;
        }

        if (InCombat && TheEwerPvE.CanUse(out act))
        {
            return true;
        }

        if (EssentialDignityPvE.Cooldown.CurrentCharges == 3 && EssentialDignityPvE.CanUse(out act, usedUp: true) && EssentialDignityPvE.Target.Target.GetHealthRatio() < EssentialDignityThird)
        {
            return true;
        }

        if (EssentialDignityPvE.Cooldown.CurrentCharges == 2 && EssentialDignityPvE.CanUse(out act, usedUp: true) && EssentialDignityPvE.Target.Target.GetHealthRatio() < EssentialDignitySecond)
        {
            return true;
        }

        if (EssentialDignityPvE.Cooldown.CurrentCharges == 1 && EssentialDignityPvE.CanUse(out act, usedUp: true) && EssentialDignityPvE.Target.Target.GetHealthRatio() < EssentialDignityLast)
        {
            return true;
        }

if (CelestialIntersectionPvE.Cooldown.CurrentCharges == 2
    && (CelestialIntersectionPvE.Target.Target?.GetHealthRatio() < 0.9f) == true
    && CelestialIntersectionPvE.CanUse(out act, usedUp: true))
{
    return true;
}

        return base.HealSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.CelestialOppositionPvE, ActionID.StellarDetonationPvE, ActionID.HoroscopePvE, ActionID.HoroscopePvE_16558, ActionID.LadyOfCrownsPvE)]
    protected override bool HealAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (HasGiantDominance && StellarDetonationPvE.CanUse(out act))
        {
            return true;
        }

        if (MicrocosmosPvE.CanUse(out act))
        {
            return true;
        }

        if (MicroPrio && HasMacrocosmos)
        {
            return base.HealAreaAbility(nextGCD, out act);
        }

        if (CelestialOppositionPvE.CanUse(out act))
        {
            return true;
        }

        if (StellarDetonationPvE.CanUse(out act))
        {
            return true;
        }

        if (PartyMembersAverHP < HoroscopeHeal && HoroscopePvE_16558.CanUse(out act))
        {
            return true;
        }

        if (PartyMembersAverHP < HoroscopeHeal && HoroscopePvE.CanUse(out act))
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
        if (StatusHelper.PlayerHasStatus(true, StatusID.Suntouched) && StatusHelper.PlayerWillStatusEndGCD(3, 0, true, StatusID.Suntouched))
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

        if (AstralDrawPvE.Cooldown.WillHaveOneCharge(3) && LadyOfCrownsPvE.CanUse(out act))
        {
            return true;
        }

        if (AstralDrawPvE.Cooldown.WillHaveOneCharge(3) && InCombat && TheEwerPvE.CanUse(out act))
        {
            return true;
        }

        if (AstralDrawPvE.Cooldown.WillHaveOneCharge(3) && InCombat && TheBolePvE.CanUse(out act))
        {
            return true;
        }

        if (UmbralDrawPvE.Cooldown.WillHaveOneCharge(3) && InCombat && TheArrowPvE.CanUse(out act))
        {
            return true;
        }

        if (UmbralDrawPvE.Cooldown.WillHaveOneCharge(3) && InCombat && TheSpirePvE.CanUse(out act))
        {
            return true;
        }

        if (AstralDrawPvE.CanUse(out act))
        {
            return true;
        }

        if ((HasDivination || !DivinationPvE.Cooldown.WillHaveOneCharge(66) || !DivinationPvE.EnoughLevel) && InCombat && TheBalancePvE.CanUse(out act))
        {
            return true;
        }

        if (!IsOpen && InCombat && LordOfCrownsPvE.CanUse(out act))
{
    bool divinationLearned = DivinationPvE.EnoughLevel;

    if ((divinationLearned && HasDivination) // simple: only under Divination
        || (!divinationLearned)              // low level: no Divination exists, so spend Lord
        || (divinationLearned && !DivinationPvE.Cooldown.WillHaveOneCharge(60)) // Divination not soon
        || UmbralDrawPvE.Cooldown.WillHaveOneCharge(3)) // avoid holding through imminent Umbral Draw
    {
        return true;
    }
}

// Gate Umbral Draw if we can spend Balance (or Spear) and Lord first
bool burstCardsAllowed =
    (HasDivination || !DivinationPvE.Cooldown.WillHaveOneCharge(66) || !DivinationPvE.EnoughLevel);

bool hasBurstCardToPlay =
    InCombat && burstCardsAllowed && (TheBalancePvE.CanUse(out _) || TheSpearPvE.CanUse(out _));

bool hasLordToSpend =
    InCombat && LordOfCrownsPvE.CanUse(out _);

if (UmbralDrawPvE.CanUse(out act) && !(hasBurstCardToPlay && hasLordToSpend))
{
    return true;
}
        if ((HasDivination || !DivinationPvE.Cooldown.WillHaveOneCharge(66) || !DivinationPvE.EnoughLevel) && InCombat && TheSpearPvE.CanUse(out act))
        {
            return true;
        }

        if (InCombat && OraclePvE.CanUse(out act))
       {
            return true;
        }

        return base.GeneralAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
{
    act = null;

    bool divLearned = DivinationPvE.EnoughLevel;

    bool divReadySoon60 = divLearned && DivinationPvE.Cooldown.WillHaveOneCharge(60f);
    bool divReadySoon2  = divLearned && DivinationPvE.Cooldown.WillHaveOneCharge(2f);

    // Hold last Lightspeed charge if Divination is within 60s but not imminent
    bool holdLastLightspeedForDiv =
        divReadySoon60 &&
        !divReadySoon2 &&
        LightspeedPvE.Cooldown.CurrentCharges == 1 &&
        !HasLightspeed;

// Only these GCDs are allowed while moving without needing Lightspeed
bool nextIsMovementSafeGcd =
    nextGCD.IsTheSameTo(false,
        MacrocosmosPvE,
        AspectedBeneficPvE,
        CombustIiiPvE, CombustIiPvE, CombustPvE);
// True if Combust is missing or will fall off within 18s
bool combustSoon18 =
    CurrentTarget != null &&
    (
        (CombustIiiPvE.EnoughLevel &&
            (!(CurrentTarget?.HasStatus(true, StatusID.CombustIii) ?? false)
             || (CurrentTarget?.WillStatusEnd(18, true, StatusID.CombustIii) ?? false)))
        ||
        (!CombustIiiPvE.EnoughLevel && CombustIiPvE.EnoughLevel &&
            (!(CurrentTarget?.HasStatus(true, StatusID.CombustIi) ?? false)
             || (CurrentTarget?.WillStatusEnd(18, true, StatusID.CombustIi) ?? false)))
        ||
        (!CombustIiiPvE.EnoughLevel && !CombustIiPvE.EnoughLevel && CombustPvE.EnoughLevel &&
            (!(CurrentTarget?.HasStatus(true, StatusID.Combust) ?? false)
             || (CurrentTarget?.WillStatusEnd(18, true, StatusID.Combust) ?? false)))
    );
// If moving, and next GCD is NOT one of the safe ones,
// and we are not already under Swift or Lightspeed,
// then we need Lightspeed.
bool needsMovementRescue =
    InCombat
    && IsMoving
    && !nextIsMovementSafeGcd
    && !HasSwift
    && !HasLightspeed
    && !combustSoon18;


    // First ~5 seconds of Divination (Divination lasts 15s)
    bool divJustStarted =
        HasDivination &&
        StatusHelper.PlayerStatusTime(true, StatusID.Divination) >= 8f;

    // Use Lightspeed once during opener window
    bool openerLightspeed =
        IsOpen &&
        InCombat &&
        !HasLightspeed &&
        !holdLastLightspeedForDiv &&
        LightspeedPvE.Cooldown.CurrentCharges >= 1;

    // Spend last Lightspeed ~2s before Divination (burst prep)
    if (divReadySoon2
        && LightspeedPvE.Cooldown.CurrentCharges >= 1
        && !HasLightspeed
        && InCombat
        && IsBurst
        && LightspeedPvE.CanUse(out act, usedUp: true))
    {
        return true;
    }

    if (!IsOpen && IsBurst && InCombat && DivinationPvE.CanUse(out act))
    {
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

    // Divination early window Lightspeed
    if (!HasLightspeed
        && InCombat
        && divJustStarted
        && !holdLastLightspeedForDiv
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
    && !holdLastLightspeedForDiv
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

        if ((NeutralSectPvE.CanUse(out _) || HasNeutralSect || IsLastAbility(false, NeutralSectPvE)) && AspectedBeneficPvE.CanUse(out act, skipStatusProvideCheck: true))
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

        if ((NeutralSectPvE.CanUse(out _) || HasNeutralSect || IsLastAbility(false, NeutralSectPvE)) && HeliosConjunctionPvE.CanUse(out act, skipStatusProvideCheck: true))
        {
            return true;
        }

        if ((MultiHitRestrict && IsCastingMultiHit) || !MultiHitRestrict)
        {
            if (MacrocosmosPvE.CanUse(out act))
            {
                return true;
            }
        }

        return base.DefenseAreaGCD(out act);
    }

    [RotationDesc(ActionID.AspectedBeneficPvE, ActionID.BeneficIiPvE, ActionID.BeneficPvE)]
    protected override bool HealSingleGCD(out IAction? act)
    {
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
    NextAbilityToNextGCD < 0.6f &&
    (AspectedBeneficPvE.Target.Target?.GetHealthRatio() < 0.9f) == true;

if (AspectedBeneficPvE.CanUse(out act)
    && (AspectedBeneficPvE.Target.Target?.GetHealthRatio() < AspectedBeneficHeal
        || movingHealWindow))
{
    return true;
}

        if (BeneficIiPvE.CanUse(out act))
        {
            return true;
        }

        if (BeneficPvE.CanUse(out act))
        {
            return true;
        }

        return base.HealSingleGCD(out act);
    }

    [RotationDesc(ActionID.AspectedHeliosPvE, ActionID.HeliosPvE, ActionID.HeliosConjunctionPvE)]
    protected override bool HealAreaGCD(out IAction? act)
    {
        if ((HasSwift || IsLastAction(ActionID.SwiftcastPvE)) && SwiftLogic && MergedStatus.HasFlag(AutoStatus.Raise))
        {
            return base.HealAreaGCD(out act);
        }

        if (MicroPrio && HasMacrocosmos)
        {
            return base.HealAreaGCD(out act);
        }

        if (HeliosConjunctionPvE.EnoughLevel && HeliosConjunctionPvE.CanUse(out act))
        {
            return true;
        }

        if (!HeliosConjunctionPvE.EnoughLevel && AspectedHeliosPvE.CanUse(out act))
        {
            return true;
        }

        if (HeliosPvE.CanUse(out act))
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
        if ((HasSwift || IsLastAction(ActionID.SwiftcastPvE)) && SwiftLogic && MergedStatus.HasFlag(AutoStatus.Raise))
        {
            return base.GeneralGCD(out act);
        }

        if (GravityIiPvE.EnoughLevel && GravityIiPvE.CanUse(out act))
        {
            return true;
        }
        if (!GravityIiPvE.EnoughLevel && GravityPvE.EnoughLevel && GravityPvE.CanUse(out act))
        {
            return true;
        }
// Moving Combust refresh (<15s) with timing gate (0.6f)
{
    bool canCommitGcdNow = NextAbilityToNextGCD < 0.6f;

    if (InCombat && IsMoving && canCommitGcdNow && CurrentTarget != null)
    {
        bool combustLow15 =
            (CombustIiiPvE.EnoughLevel &&
                (!(CurrentTarget?.HasStatus(true, StatusID.CombustIii) ?? false)
                 || (CurrentTarget?.WillStatusEnd(15, true, StatusID.CombustIii) ?? false)))
            ||
            (!CombustIiiPvE.EnoughLevel && CombustIiPvE.EnoughLevel &&
                (!(CurrentTarget?.HasStatus(true, StatusID.CombustIi) ?? false)
                 || (CurrentTarget?.WillStatusEnd(15, true, StatusID.CombustIi) ?? false)))
            ||
            (!CombustIiiPvE.EnoughLevel && !CombustIiPvE.EnoughLevel && CombustPvE.EnoughLevel &&
                (!(CurrentTarget?.HasStatus(true, StatusID.Combust) ?? false)
                 || (CurrentTarget?.WillStatusEnd(15, true, StatusID.Combust) ?? false)));

        if (combustLow15)
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