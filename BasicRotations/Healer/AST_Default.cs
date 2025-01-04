namespace DefaultRotations.Healer;

[Rotation("Default", CombatType.PvE, GameVersion = "7.15")]
[SourceCode(Path = "main/BasicRotations/Healer/AST_Default.cs")]
[Api(4)]
public sealed class AST_Default : AstrologianRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Enable Swiftcast Restriction Logic to attempt to prevent actions other than Raise when you have swiftcast")]
    public bool SwiftLogic { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use both stacks of Lightspeed while moving")]
    public bool LightspeedMove { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use spells with cast times to heal. (Ignored if you are the only healer in party)")]
    public bool GCDHeal { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Prevent actions while you have the bubble mit up")]
    public bool BubbleProtec { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Prioritize Microcosmos over all other healing when available")]
    public bool MicroPrio { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Simple Lord of Crowns logic (use under divinaiton)")]
    public bool SimpleLord { get; set; } = false;

    [Range(4, 20, ConfigUnitType.Seconds)]
    [RotationConfig(CombatType.PvE, Name = "Use Earthly Star during countdown timer.")]
    public float UseEarthlyStarTime { get; set; } = 15;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum HP threshold party member needs to be to use Aspected Benefic")]
    public float AspectedBeneficHeal { get; set; } = 0.4f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum HP threshold among party member needed to use Horoscope")]
    public float HoroscopeHeal { get; set; } = 0.3f;

    [RotationConfig(CombatType.PvE, Name = "Use DOT while moving even if it does not need refresh (disabling is a damage down)")]
    public bool DOTUpkeep { get; set; } = true;
    #endregion

    private static bool InBurstStatus => !Player.WillStatusEnd(0, true, StatusID.Divination);

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (remainTime < MaleficPvE.Info.CastTime + CountDownAhead
            && MaleficPvE.CanUse(out var act)) return act;
        if (remainTime < 3 && UseBurstMedicine(out act)) return act;
        if (remainTime is < 4 and > 3 && AspectedBeneficPvE.CanUse(out act)) return act;
        if (remainTime < UseEarthlyStarTime
            && EarthlyStarPvE.CanUse(out act, skipTTKCheck: true)) return act;
        if (remainTime < 30 && AstralDrawPvE.CanUse(out act)) return act;

        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (BubbleProtec && Player.HasStatus(true, StatusID.CollectiveUnconscious_848)) return false;
        if (MicroPrio && Player.HasStatus(true, StatusID.Macrocosmos)) return false;

        if (!InCombat) return false;

        if (OraclePvE.CanUse(out act)) return true;
        if (nextGCD.IsTheSameTo(true, AspectedHeliosPvE, HeliosPvE))
        {
            if (HoroscopePvE.CanUse(out act)) return true;
            if (NeutralSectPvE.CanUse(out act)) return true;
        }

        if (nextGCD.IsTheSameTo(true, BeneficPvE, BeneficIiPvE, AspectedBeneficPvE))
        {
            if (SynastryPvE.CanUse(out act)) return true;
        }

        if (DivinationPvE.CanUse(out _)
            && UseBurstMedicine(out act)) return true;

        return base.EmergencyAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.ExaltationPvE, ActionID.TheArrowPvE, ActionID.TheSpirePvE, ActionID.TheBolePvE, ActionID.TheEwerPvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (BubbleProtec && Player.HasStatus(true, StatusID.CollectiveUnconscious_848)) return false;

        if (InCombat && TheSpirePvE.CanUse(out act)) return true;
        if (InCombat && TheBolePvE.CanUse(out act)) return true;

        if (ExaltationPvE.CanUse(out act)) return true;
        return base.DefenseSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.MacrocosmosPvE)]
    protected override bool DefenseAreaGCD(out IAction? act)
    {
        act = null;
        if (BubbleProtec && Player.HasStatus(true, StatusID.CollectiveUnconscious_848)) return false;

        if (MacrocosmosPvE.Cooldown.IsCoolingDown && !MacrocosmosPvE.Cooldown.WillHaveOneCharge(150)
            || CollectiveUnconsciousPvE.Cooldown.IsCoolingDown && !CollectiveUnconsciousPvE.Cooldown.WillHaveOneCharge(40)) return false;

        if (MacrocosmosPvE.CanUse(out act)) return true;
        return base.DefenseAreaGCD(out act);
    }

    [RotationDesc(ActionID.CollectiveUnconsciousPvE, ActionID.SunSignPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (SunSignPvE.CanUse(out act)) return true;

        act = null;
        if (BubbleProtec && Player.HasStatus(true, StatusID.CollectiveUnconscious_848)) return false;

        if (MacrocosmosPvE.Cooldown.IsCoolingDown && !MacrocosmosPvE.Cooldown.WillHaveOneCharge(150)
            || CollectiveUnconsciousPvE.Cooldown.IsCoolingDown && !CollectiveUnconsciousPvE.Cooldown.WillHaveOneCharge(40)) return false;

        if (CollectiveUnconsciousPvE.CanUse(out act)) return true;
        return base.DefenseAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.TheArrowPvE, ActionID.TheEwerPvE, ActionID.EssentialDignityPvE,
        ActionID.CelestialIntersectionPvE)]
    protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (BubbleProtec && Player.HasStatus(true, StatusID.CollectiveUnconscious_848)) return false;
        if (MicroPrio && Player.HasStatus(true, StatusID.Macrocosmos)) return false;

        if (InCombat && TheArrowPvE.CanUse(out act)) return true;
        if (InCombat && TheEwerPvE.CanUse(out act)) return true;

        if (EssentialDignityPvE.CanUse(out act, usedUp: true)) return true;

        if (CelestialIntersectionPvE.CanUse(out act, usedUp: true)) return true;

        return base.HealSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.CelestialOppositionPvE, ActionID.StellarDetonationPvE, ActionID.HoroscopePvE, ActionID.HoroscopePvE_16558, ActionID.LadyOfCrownsPvE, ActionID.HeliosConjunctionPvE)]
    protected override bool HealAreaAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (BubbleProtec && Player.HasStatus(true, StatusID.CollectiveUnconscious_848)) return false;

        if (MicrocosmosPvE.CanUse(out act)) return true;
        if (MicroPrio && Player.HasStatus(true, StatusID.Macrocosmos)) return false;

        if (CelestialOppositionPvE.CanUse(out act)) return true;

        if (StellarDetonationPvE.CanUse(out act)) return true;

        if (HoroscopePvE.CanUse(out act)) return true;

        if (HoroscopePvE_16558.CanUse(out act)) return true;

        if (LadyOfCrownsPvE.CanUse(out act)) return true;

        if (HeliosConjunctionPvE.CanUse(out act)) return true;
        return base.HealAreaAbility(nextGCD, out act);
    }

    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (BubbleProtec && Player.HasStatus(true, StatusID.CollectiveUnconscious_848)) return false;

        // Use LadyOfCrownsPvE if your party is needs to be topped up or you are about to use Astral Draw (to prevent overwriting)
        if (PartyMembersAverHP < .8 && LadyOfCrownsPvE.CanUse(out act)) return true;
        if (AstralDrawPvE.Cooldown.WillHaveOneCharge(3) && LadyOfCrownsPvE.CanUse(out act)) return true;

        // Use cards if you are about to use Umbral or Astral Draw (to prevent overwriting)
        if (AstralDrawPvE.Cooldown.WillHaveOneCharge(3) && InCombat && TheEwerPvE.CanUse(out act)) return true;
        if (AstralDrawPvE.Cooldown.WillHaveOneCharge(3) && InCombat && TheBolePvE.CanUse(out act)) return true;
        if (UmbralDrawPvE.Cooldown.WillHaveOneCharge(3) && InCombat && TheArrowPvE.CanUse(out act)) return true;
        if (UmbralDrawPvE.Cooldown.WillHaveOneCharge(3) && InCombat && TheSpirePvE.CanUse(out act)) return true;

        if (AstralDrawPvE.CanUse(out act)) return true;
        if (UmbralDrawPvE.CanUse(out act)) return true;
        if ((Player.HasStatus(true, StatusID.Divination) || !DivinationPvE.Cooldown.WillHaveOneCharge(66) || !DivinationPvE.EnoughLevel) && InCombat && TheBalancePvE.CanUse(out act)) return true;
        if ((Player.HasStatus(true, StatusID.Divination) || !DivinationPvE.Cooldown.WillHaveOneCharge(66) || !DivinationPvE.EnoughLevel) && InCombat && TheSpearPvE.CanUse(out act)) return true;
        return base.GeneralAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (BubbleProtec && Player.HasStatus(true, StatusID.CollectiveUnconscious_848)) return false;

        // Use LordOfCrownsPvE if you have SimpleLord enabled and  Divination is active
        if (SimpleLord && InCombat && Player.HasStatus(true, StatusID.Divination) && LordOfCrownsPvE.CanUse(out act)) return true;

        if (!Player.HasStatus(true, StatusID.Lightspeed)
            && InCombat
            && DivinationPvE.Cooldown.ElapsedAfter(115)
            && LightspeedPvE.CanUse(out act, usedUp: true)) return true;

        if (IsBurst && !IsMoving
            && DivinationPvE.CanUse(out act)) return true;

        if (AstralDrawPvE.CanUse(out act, usedUp: IsBurst)) return true;

        if (!Player.HasStatus(true, StatusID.Lightspeed)
            && (InBurstStatus || DivinationPvE.Cooldown.ElapsedAfter(115))
            && InCombat
            && LightspeedPvE.CanUse(out act, usedUp: true)) return true;

        if (InCombat)
        {
            if (!Player.HasStatus(true, StatusID.Lightspeed) && IsMoving && LightspeedPvE.CanUse(out act, usedUp: LightspeedMove)) return true;

            if (!IsMoving)
            {
                if (!Player.HasStatus(true, StatusID.EarthlyDominance, StatusID.GiantDominance))
                {
                    if (EarthlyStarPvE.CanUse(out act)) return true;
                }
            }

            {
                if (!SimpleLord && (Player.HasStatus(true, StatusID.Divination) || !DivinationPvE.Cooldown.WillHaveOneCharge(45) || !DivinationPvE.EnoughLevel || UmbralDrawPvE.Cooldown.WillHaveOneCharge(3)) && LordOfCrownsPvE.CanUse(out act)) return true;
            }
        }
        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    [RotationDesc(ActionID.AspectedBeneficPvE, ActionID.BeneficIiPvE, ActionID.BeneficPvE)]
    protected override bool HealSingleGCD(out IAction? act)
    {
        act = null;
        if (BubbleProtec && Player.HasStatus(true, StatusID.CollectiveUnconscious_848)) return false;
        if (MicroPrio && Player.HasStatus(true, StatusID.Macrocosmos)) return false;
        if (HasSwift && SwiftLogic && AscendPvE.CanUse(out _)) return false;

        if (AspectedBeneficPvE.CanUse(out act)
            && (IsMoving
            || AspectedBeneficPvE.Target.Target?.GetHealthRatio() > AspectedBeneficHeal)) return true;

        if (BeneficIiPvE.CanUse(out act)) return true;
        if (BeneficPvE.CanUse(out act)) return true;

        return base.HealSingleGCD(out act);
    }

    [RotationDesc(ActionID.AspectedHeliosPvE, ActionID.HeliosPvE)]
    protected override bool HealAreaGCD(out IAction? act)
    {
        act = null;
        if (BubbleProtec && Player.HasStatus(true, StatusID.CollectiveUnconscious_848)) return false;
        if (MicroPrio && Player.HasStatus(true, StatusID.Macrocosmos)) return false;
        if (HasSwift && SwiftLogic && AscendPvE.CanUse(out _)) return false;

        if (AspectedHeliosPvE.CanUse(out act)) return true;
        if (HeliosPvE.CanUse(out act)) return true;
        return base.HealAreaGCD(out act);
    }

    protected override bool GeneralGCD(out IAction? act)
    {
        act = null;
        if (BubbleProtec && Player.HasStatus(true, StatusID.CollectiveUnconscious_848)) return false;
        if (HasSwift && SwiftLogic) return false;

        if (GravityPvE.CanUse(out act)) return true;

        if (CombustIiiPvE.CanUse(out act)) return true;
        if (CombustIiPvE.CanUse(out act)) return true;
        if (CombustPvE.CanUse(out act)) return true;
        if (MaleficPvE.CanUse(out act)) return true;
        if (CombustIiiPvE.CanUse(out act, skipStatusProvideCheck: DOTUpkeep)) return true;
        if (CombustIiPvE.CanUse(out act, skipStatusProvideCheck: DOTUpkeep)) return true;
        if (CombustPvE.CanUse(out act, skipStatusProvideCheck: DOTUpkeep)) return true;

        return base.GeneralGCD(out act);
    }
    #endregion



    #region Extra Methods
    public override bool CanHealSingleSpell => base.CanHealSingleSpell && (GCDHeal || PartyMembers.GetJobCategory(JobRole.Healer).Count() < 2);
    public override bool CanHealAreaSpell => base.CanHealAreaSpell && (GCDHeal || PartyMembers.GetJobCategory(JobRole.Healer).Count() < 2);

    #endregion
}
