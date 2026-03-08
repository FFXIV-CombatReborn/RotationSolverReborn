using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RotationSolver.ExtraRotations.Healer;

[Rotation("BeirutaSCH", CombatType.PvE, GameVersion = "7.45")]
[SourceCode(Path = "main/ExtraRotations/Healer/BeirutaSCH.cs")]
public sealed class BeirutaSCH : ScholarRotation
{
    #region Config Options

    [RotationConfig(CombatType.PvE, Name =
        "Please note that this rotation is optimised for high-end encounters.\n" +
        "• Healing actions are designed to be used automatically, while mitigation is kept minimal to better support CD planner or manual input\n" +
        "• Sacred Soil, Summon Seraph, Seraphism, and Expedient should generally be used manually or via the CD planner\n" +
        "• Please set Intercept to GCD usage only, and use Concitation manually where required\n" +
        "• Disabling AutoBurst is sufficient if you need to delay burst timing in this rotation\n" +
        "• Applying Protraction to yourself will be treated as a signal to prepare Deployment Tactics\n" +
        "• Dissipation is used to make Energy Drain dump window aligned with bursts\n" +
        "• Single-target GCD healing is not used in this rotation\n" +
        "• When using the CD planner, please note that after using Dissipation, no fairy abilities or Seraphism can be used for 30 seconds\n" +
        "• Without burst delay, this restriction will occur during the 30-second window at 0s and 180s then every multiple of 180s thereafter\n")]
    public bool RotationNotes { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use first stack of Consolation ASAP when Seraph is out")]
    public bool UseFirstConsolationAsapWhenSeraphIsOut { get; set; } = true;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Party HP percent threshold to use Emergency Tactics with Succor")]
    public float EmergencyTacticsHeal { get; set; } = 0.4f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Average party HP percent to use Consolation")]
    public float ConsolationHeal { get; set; } = 0.8f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Average party HP percent to prioritize Indomitability and instant heals over heal-over-time effects")]
    public float EmergencyHealPercent { get; set; } = 0.1f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Average party HP percent to use Whispering Dawn or Angel's Whisper")]
    public float WhisperingDawnHeal { get; set; } = 0.6f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Average party HP percent to use Fey Blessing when Whispering Dawn or Angel's Whisper is missing")]
    public float FeyBlessingHeal { get; set; } = 0.7f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Average party HP percent to use Indomitability")]
    public float IndomitabilityHeal { get; set; } = 0.3f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum average party HP required before using single-target oGCDs on non-tanks")]
    public float SingleAbilityNonTankPartyAverageGate { get; set; } = 0.8f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Average party HP percent to use Emergency Tactics Succor in HealAreaGCD")]
    public float HealAreaGcdEmergencyTacticsHeal { get; set; } = 0.3f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Average party HP percent to use Accession in HealAreaGCD")]
    public float HealAreaGcdAccessionHeal { get; set; } = 0.6f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Average party HP percent to use Accession in HealAreaGCD while moving for 1s or more and not under Emergency Tactics")]
    public float HealAreaGcdMovingAccessionHeal { get; set; } = 0.8f;

    [Range(0, 10000, ConfigUnitType.None)]
    [RotationConfig(CombatType.PvE, Name = "Minimum MP before prioritizing emergency healing and rezzing (willing to use Seraphism sooner)")]
    public int EmergencyHealingMPThreshold { get; set; } = 2000;

    [RotationConfig(CombatType.PvE, Name = "Enable Swiftcast restriction: only allow Raise while Swiftcast is active")]
    public bool SwiftLogic { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Recitation during the countdown opener")]
    public bool UseRecitationInOpener { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Adloquium during the countdown opener")]
    public bool AdloquiumDuringCountdown { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "How to control Deployment Tactics usage")]
    public DeploymentTacticsUsageStrategy DeploymentTacticsUsage { get; set; } = DeploymentTacticsUsageStrategy.ProtractionControl;

    [RotationConfig(CombatType.PvE, Name = "Only use Deployment Tactics on Critical Shields?")]
    public bool OnlyUseDeploymentTacticsOnCriticalShields { get; set; } = true;

    public enum DeploymentTacticsUsageStrategy : byte
    {
        [Description("Controlled by having Protraction on self (configured by below option)")]
        ProtractionControl,

        [Description("Controlled by having Recitation (always crit)")]
        RecitationControl,

        [Description("Controlled by both (allow non-crit)")]
        BothControl,
    }

    #endregion

    #region Constants / Fields

    private const long ChainStratagemBanefulBlockMs = 5_000;
    private const long ChainStratagemDotRefreshMs = 20_000;
    private const long DeploymentTacticsAfterAdloquiumWindowMs = 5_000;

    private const float ChainStratagemDotRefreshSeconds = 11f;
    private const float AetherpactRemoveHpThreshold = 0.9f;
    private const float AetherpactStartHpThreshold = 0.8f;
    private const float ExcogHealHpThreshold = 0.4f;
    private const float BallparkDamagePercent = 0.08f;
    private const float RuinIIMovementThresholdSeconds = 1f;

    private const int DotOffsetMobs = 1;
    private const int MinimumFairyGaugeForLinkPriority = 70;

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

private float SummonSeraphRem()
{
    if (!SummonSeraphPvE.EnoughLevel) return -1f;
    return EstimateRemainingSeconds(SummonSeraphPvE.Cooldown, 180f, 0.5f);
}

private float DissipationRem()
{
    if (!DissipationPvE.EnoughLevel) return -1f;
    return EstimateRemainingSeconds(DissipationPvE.Cooldown, 180f, 0.5f);
}

private bool ShouldUseSummonEos()
{
    float summonSeraphRem = SummonSeraphRem();
    float dissipationRem = DissipationRem();

    return summonSeraphRem < 90f || dissipationRem < 140f;
}
    private const bool UseDissipationDuringBurst = true;
    private const bool EnableBallparkTtkEstimator = true;

    private long _chainStratagemUsedAtMs;
    private long _adloquiumUsedAtMs;

    #endregion

    #region Simple Status Helpers

    // Local shortcuts for commonly checked player/party states.
    private bool HasGalvanize => StatusHelper.PlayerHasStatus(true, StatusID.Galvanize);
    private bool HasProtraction => StatusHelper.PlayerHasStatus(true, StatusID.Protraction);

    private bool HasWhisperingDawn => HasPartyMemberWithOwnStatus(StatusID.WhisperingDawn);
    private bool HasAngelsWhisper => HasPartyMemberWithOwnStatus(StatusID.AngelsWhisper);

    private bool InFirst5sAfterChainStratagem =>
        _chainStratagemUsedAtMs != 0 &&
        Environment.TickCount64 - _chainStratagemUsedAtMs < ChainStratagemBanefulBlockMs;

    private bool InFirst20sAfterChainStratagem =>
        _chainStratagemUsedAtMs != 0 &&
        Environment.TickCount64 - _chainStratagemUsedAtMs < ChainStratagemDotRefreshMs;

    private bool InFirst5sAfterAdloquium =>
        _adloquiumUsedAtMs != 0 &&
        Environment.TickCount64 - _adloquiumUsedAtMs < DeploymentTacticsAfterAdloquiumWindowMs;

    #endregion

    #region Countdown Logic

    // Pre-pull opener logic.
    protected override IAction? CountDownAction(float remainTime)
    {
        if (SummonEosPvE.CanUse(out IAction? act))
            return act;

        if (remainTime < RuinPvE.Info.CastTime + CountDownAhead && RuinPvE.CanUse(out act))
            return act;

        if (remainTime < 3f && UseBurstMedicine(out act))
            return act;

        if (remainTime is < 4f and > 3f && DeploymentTacticsPvE.CanUse(out act))
            return act;

        if (remainTime is < 7f and > 6f && AdloquiumDuringCountdown &&
            AdloquiumPvE.CanUse(out act, targetOverride: TargetType.Tank))
        {
            StampAdloquiumUse();
            return act;
        }

        if (remainTime <= 15f && UseRecitationInOpener && RecitationPvE.CanUse(out act))
            return act;

        return base.CountDownAction(remainTime);
    }

    #endregion

    #region oGCD Logic

    // Highest-priority emergency oGCD handling.
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        int closeTargetCount = NumberOfHostilesInRangeOf(5);

        if (ShouldUseRecitationForDeploymentTactics() && RecitationPvE.CanUse(out act))
            return true;

        if (PartyMembersAverHP < HealAreaGcdEmergencyTacticsHeal &&
            EmergencyTacticsPvE.CanUse(out act) &&
            CountEmergencyTacticsTargets() > 1)
        {
            return true;
        }

        if (InFirst5sAfterAdloquium && DeploymentTacticsPvE.CanUse(out act))
            return true;

        if (UseFirstConsolationAsapWhenSeraphIsOut &&
            ConsolationPvE.Cooldown.CurrentCharges == 2 &&
            ConsolationPvE.CanUse(out act))
{
            return true;
    }

        if (ShouldRemoveAetherpact())
        {
            act = AetherpactPvE;
            return true;
        }

        if (TryUseBurstSupportActions(out act))
            return true;

        if (!HasAetherflow && AetherflowPvE.CanUse(out act))
            return true;

        if (SeraphTime < 3 && ConsolationPvE.CanUse(out act, usedUp: true))
            return true;

        if (ShouldUseBanefulImpaction(closeTargetCount, out act))
            return true;

        return base.EmergencyAbility(nextGCD, out act);
    }

    // AoE healing oGCD handling.
    [RotationDesc(
        ActionID.ConsolationPvE,
        ActionID.IndomitabilityPvE,
        ActionID.WhisperingDawnPvE,
        ActionID.FeyBlessingPvE)]
    protected override bool HealAreaAbility(IAction nextGCD, out IAction? act)
    {
        act = null;

        if (PartyMembersAverHP < ConsolationHeal &&
            ConsolationPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        if (PartyMembersAverHP < EmergencyHealPercent)
        {
            if (FeyBlessingPvE.CanUse(out act))
                return true;

            if (IndomitabilityPvE.CanUse(out act))
                return true;
        }

        if (PartyMembersAverHP < WhisperingDawnHeal &&
            WhisperingDawnPvE.CanUse(out act))
        {
            return true;
        }

        if ((!HasWhisperingDawn || !HasAngelsWhisper) &&
            PartyMembersAverHP < FeyBlessingHeal &&
            FeyBlessingPvE.CanUse(out act))
        {
            return true;
        }

        if ((!HasWhisperingDawn || !HasAngelsWhisper) &&
            PartyMembersAverHP < IndomitabilityHeal &&
            IndomitabilityPvE.CanUse(out act))
        {
            return true;
        }

        return base.HealAreaAbility(nextGCD, out act);
    }

    // Single-target healing oGCD handling.
    [RotationDesc(
        ActionID.AetherpactPvE,
        ActionID.ExcogitationPvE,
        ActionID.LustratePvE,
        ActionID.SacredSoilPvE,
        ActionID.WhisperingDawnPvE,
        ActionID.FeyBlessingPvE)]
    protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        bool hasLink = HasPartyMemberWithOwnStatus(StatusID.FeyUnion_1223);

        if (AetherpactPvE.CanUse(out act) &&
            FairyGauge >= MinimumFairyGaugeForLinkPriority &&
            !hasLink &&
            CanUseSingleAbilityTarget(AetherpactPvE.Target.Target) &&
            AetherpactPvE.Target.Target.GetHealthRatio() <= AetherpactStartHpThreshold)
        {
            return true;
        }

        if (!HasRecitation &&
            !IsLastAbility(false, RecitationPvE) &&
            ExcogitationPvE.CanUse(out act) &&
            CanUseSingleAbilityTarget(ExcogitationPvE.Target.Target) &&
            ExcogitationPvE.Target.Target.GetHealthRatio() < ExcogHealHpThreshold)
        {
            return true;
        }

        if (!hasLink &&
            FairyGauge > 20 &&
            AetherpactPvE.CanUse(out act) &&
            CanUseSingleAbilityTarget(AetherpactPvE.Target.Target))
        {
            return true;
        }

        return base.HealSingleAbility(nextGCD, out act);
    }

    // Area mitigation / defensive oGCD handling.
    [RotationDesc(
        ActionID.FeyIlluminationPvE,
        ActionID.ExpedientPvE,
        ActionID.ConsolationPvE,
        ActionID.SacredSoilPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (ConsolationPvE.CanUse(out act, usedUp: true))
            return true;

        if (SacredSoilPvE.CanUse(out act))
            return true;

        if (ExpedientPvE.CanUse(out act))
            return true;

        if ((SummonSeraphPvE.Cooldown.IsCoolingDown || CurrentMp <= EmergencyHealingMPThreshold) &&
            SeraphismPvE.CanUse(out act))
        {
            return true;
        }

        if (FeyIlluminationPvE.CanUse(out act))
            return true;

        return base.DefenseAreaAbility(nextGCD, out act);
    }

    // Single-target mitigation oGCD handling.
    [RotationDesc(ActionID.ExcogitationPvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        act = null;

        if (!HasRecitation &&
            !IsLastAbility(false, RecitationPvE) &&
            ExcogitationPvE.CanUse(out act) &&
            !HasDefenseSingleLockoutStatus(ExcogitationPvE.Target.Target))
        {
            return true;
        }

        return base.DefenseSingleAbility(nextGCD, out act);
    }

    // Movement speed utility.
    [RotationDesc(ActionID.ExpedientPvE)]
    protected override bool SpeedAbility(IAction nextGCD, out IAction? act)
    {
        if (InCombat && ExpedientPvE.CanUse(out act, usedUp: true))
            return true;

        return base.SpeedAbility(nextGCD, out act);
    }

    

    // Damage-oriented oGCD handling.
    [RotationDesc(
        ActionID.ChainStratagemPvE,
        ActionID.EnergyDrainPvE,
        ActionID.BanefulImpactionPvE,
        ActionID.AetherflowPvE,
        ActionID.DissipationPvE)]
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        int closeTargetCount = NumberOfHostilesInRangeOf(5);

        if (TryUseBurstSupportActions(out act))
            return true;

        bool shouldDumpAether =
            (UseDissipationDuringBurst &&
             DissipationPvE.EnoughLevel &&
             DissipationPvE.Cooldown.WillHaveOneChargeGCD(3) &&
             DissipationPvE.IsEnabled) ||
            AetherflowPvE.Cooldown.WillHaveOneChargeGCD(3);

        if (CombatTime > 5 && shouldDumpAether && EnergyDrainPvE.CanUse(out act, usedUp: true))
            return true;

        if (!HasAetherflow && AetherflowPvE.CanUse(out act))
            return true;

        if (HasBuffs && IsBurst &&UseBurstMedicine(out act))
            return true;

        if (ShouldUseBanefulImpaction(closeTargetCount, out act))
            return true;

        return base.AttackAbility(nextGCD, out act);
    }

    #endregion

    #region GCD Logic

    // AoE healing GCD choices.
    [RotationDesc(ActionID.SuccorPvE, ActionID.ConcitationPvE, ActionID.AccessionPvE)]
    protected override bool HealAreaGCD(out IAction? act)
    {
        if (ShouldDeferToRaise())
            return base.HealAreaGCD(out act);

        if (HasEmergencyTactics &&
            PartyMembersAverHP < HealAreaGcdEmergencyTacticsHeal &&
            SuccorPvE.CanUse(out act, skipStatusProvideCheck: true))
        {
            return true;
        }

        if (!HasEmergencyTactics &&
            PartyMembersAverHP < HealAreaGcdAccessionHeal &&
            AccessionPvE.CanUse(out act, skipCastingCheck: true))
        {
            return true;
        }

        if (!HasEmergencyTactics &&
            MovingTime >= RuinIIMovementThresholdSeconds &&
            PartyMembersAverHP < HealAreaGcdMovingAccessionHeal &&
            AccessionPvE.CanUse(out act, skipCastingCheck: true))
        {
            return true;
        }

        if (HasEmergencyTactics &&
            PartyMembersAverHP < HealAreaGcdMovingAccessionHeal &&
            ConcitationPvE.CanUse(out act))
        {
            return true;
        }

        if (HasEmergencyTactics &&
            PartyMembersAverHP < HealAreaGcdMovingAccessionHeal &&
            SuccorPvE.CanUse(out act))
        {
            return true;
        }

        return base.HealAreaGCD(out act);
    }

    // Single-target healing GCD choices.
    [RotationDesc(ActionID.ManifestationPvE)]
    protected override bool HealSingleGCD(out IAction? act)
    {
        if (ShouldDeferToRaise())
            return base.HealSingleGCD(out act);

        if (HasEmergencyTactics &&
            PartyMembersAverHP > 0.8f &&
            ManifestationPvE.CanUse(out act, skipCastingCheck: true))
        {
            return true;
        }

        return base.HealSingleGCD(out act);
    }

    // Area shielding / defensive GCD choices.
    [RotationDesc(ActionID.SuccorPvE, ActionID.ConcitationPvE, ActionID.AccessionPvE)]
    protected override bool DefenseAreaGCD(out IAction? act)
    {
        if (ShouldDeferToRaise())
            return base.DefenseAreaGCD(out act);

        if (AccessionPvE.CanUse(out act, skipCastingCheck: true))
            return true;

        if (ConcitationPvE.CanUse(out act))
            return true;

        return base.DefenseAreaGCD(out act);
    }

    // Raise logic.
    [RotationDesc(ActionID.ResurrectionPvE)]
    protected override bool RaiseGCD(out IAction? act)
    {
        if (ResurrectionPvE.CanUse(out act))
            return true;

        return base.RaiseGCD(out act);
    }

    // Main damage / filler GCD logic.
    protected override bool GeneralGCD(out IAction? act)
    {
        if (ShouldDeferToRaise())
            return base.GeneralGCD(out act);

        if (CurrentMp < EmergencyHealingMPThreshold)
            return base.GeneralGCD(out act);

         if (ShouldUseSummonEos() &&
    SummonEosPvE.CanUse(out act))
{
    return true;
}

        if (ShouldUseDeploymentAdloquium() &&
            AdloquiumPvE.CanUse(out act, targetOverride: TargetType.Self))
        {
            StampAdloquiumUse();
            return true;
        }

        int nearbyHostiles = NumberOfHostilesInRangeOf(5);
        float expectedHPToLive12Seconds = CalculateExpectedHpToLive12Seconds();

        if (InCombat &&
            InFirst20sAfterChainStratagem &&
            nearbyHostiles < GetAoWBreakevenTargets() &&
            CurrentTargetBioMissingOrEnding(ChainStratagemDotRefreshSeconds) &&
            CurrentTarget != null &&
            CurrentTarget.CurrentHp >= expectedHPToLive12Seconds &&
            CanUseCurrentBio(out act, skipStatusProvideCheck: true))
        {
            return true;
        }

        if (!BioIiPvE.EnoughLevel)
        {
            if (BioPvE.CanUse(out act) && BioPvE.Target.Target.CurrentHp >= expectedHPToLive12Seconds)
                return true;

            if (RuinPvE.CanUse(out act))
                return true;

            if (BioPvE.CanUse(out act, skipTTKCheck: true))
                return true;
        }
        else if (!ArtOfWarPvE.EnoughLevel)
        {
            if (BioIiPvE.CanUse(out act) && BioIiPvE.Target.Target.CurrentHp >= expectedHPToLive12Seconds)
                return true;

            if (RuinPvE.CanUse(out act))
                return true;
        }
        else if (!BroilMasteryTrait.EnoughLevel)
        {
            if (BioIiPvE.CanUse(out act) &&
                nearbyHostiles < GetAoWBreakevenTargets() &&
                BioIiPvE.Target.Target.CurrentHp >= expectedHPToLive12Seconds)
            {
                return true;
            }

            if (ArtOfWarPvE.CanUse(out act, skipAoeCheck: true) && nearbyHostiles > 0 && InCombat)
                return true;

            if (RuinPvE.CanUse(out act))
                return true;
        }
        else if (!BroilMasteryIiTrait.EnoughLevel)
        {
            if (BioIiPvE.CanUse(out act) &&
                nearbyHostiles < GetAoWBreakevenTargets() &&
                BioIiPvE.Target.Target.CurrentHp >= expectedHPToLive12Seconds)
            {
                return true;
            }

            if (ArtOfWarPvE.CanUse(out act, skipAoeCheck: true) && nearbyHostiles > 1 && InCombat)
                return true;

            if (BroilPvE.CanUse(out act))
                return true;

            if (ArtOfWarPvE.CanUse(out act, skipAoeCheck: true) && nearbyHostiles > 0 && InCombat)
                return true;
        }
        else if (!BroilIiiPvE.EnoughLevel)
        {
            if (BioIiPvE.CanUse(out act) &&
                nearbyHostiles < GetAoWBreakevenTargets() &&
                BioIiPvE.Target.Target.CurrentHp >= expectedHPToLive12Seconds)
            {
                return true;
            }

            if (ArtOfWarPvE.CanUse(out act, skipAoeCheck: true) && nearbyHostiles > 1 && InCombat)
                return true;

            if (BroilIiPvE.CanUse(out act))
                return true;
        }
        else if (!BroilIvPvE.EnoughLevel)
        {
            if (BiolysisPvE.CanUse(out act) &&
                nearbyHostiles < GetAoWBreakevenTargets() &&
                BiolysisPvE.Target.Target.CurrentHp >= expectedHPToLive12Seconds)
            {
                return true;
            }

            if (ArtOfWarPvE.CanUse(out act, skipAoeCheck: true) && nearbyHostiles > 1 && InCombat)
                return true;

            if (BroilIiiPvE.CanUse(out act))
                return true;
        }
        else
        {
            if (BiolysisPvE.CanUse(out act) &&
                nearbyHostiles < GetAoWBreakevenTargets() &&
                BiolysisPvE.Target.Target.CurrentHp >= expectedHPToLive12Seconds)
            {
                return true;
            }

            if (ArtOfWarPvE.CanUse(out act, skipAoeCheck: true) && nearbyHostiles > 1 && InCombat)
                return true;

            if (BroilIvPvE.CanUse(out act))
                return true;
        }

        if (MovingTime > RuinIIMovementThresholdSeconds &&
            RuinIiPvE.CanUse(out act))
        {
            return true;
        }

        return base.GeneralGCD(out act);
    }

    #endregion

    #region Decision Helpers

    // Determines whether Recitation should be used to force a crit shield for Deployment Tactics.
    private bool ShouldUseRecitationForDeploymentTactics()
    {
        if (!OnlyUseDeploymentTacticsOnCriticalShields ||
            !RecitationPvE.EnoughLevel ||
            HasRecitation ||
            HasGalvanize ||
            !DeploymentTacticsPvE.Cooldown.WillHaveOneChargeGCD(2))
        {
            return false;
        }

        return DeploymentTacticsUsage switch
        {
            DeploymentTacticsUsageStrategy.ProtractionControl => HasProtraction,
            DeploymentTacticsUsageStrategy.RecitationControl => true,
            DeploymentTacticsUsageStrategy.BothControl => false,
            _ => false,
        };
    }

    // Determines whether to cast Adloquium in preparation for Deployment Tactics.
    private bool ShouldUseDeploymentAdloquium()
    {
        if (!DeploymentTacticsPvE.Cooldown.WillHaveOneChargeGCD(2) || HasGalvanize)
            return false;

        return DeploymentTacticsUsage switch
        {
            DeploymentTacticsUsageStrategy.ProtractionControl =>
                HasProtraction &&
                (!OnlyUseDeploymentTacticsOnCriticalShields || HasRecitation),

            DeploymentTacticsUsageStrategy.RecitationControl =>
                HasRecitation,

            DeploymentTacticsUsageStrategy.BothControl =>
                HasRecitation || HasProtraction,

            _ => false,
        };
    }

    // Defers non-raise GCDs when raise logic is active under Swiftcast rules.
    private bool ShouldDeferToRaise() =>
        (HasSwift || IsLastAction(ActionID.SwiftcastPvE)) &&
        SwiftLogic &&
        MergedStatus.HasFlag(AutoStatus.Raise);

    // Shared burst support logic used by both emergency and attack oGCD paths.
    private bool TryUseBurstSupportActions(out IAction? act)
    {
        act = null;

        if (!IsBurst)
            return false;

        if (CombatTime > 5 && ChainStratagemPvE.CanUse(out act))
        {
            StampChainStratagemUse();
            return true;
        }

        if (!HasAetherflow && DissipationPvE.CanUse(out act))
            return true;

        return false;
    }

    // Shared Baneful Impaction conditions.
    private bool ShouldUseBanefulImpaction(int closeTargetCount, out IAction? act)
    {
        act = null;

        if (InFirst5sAfterChainStratagem)
            return false;

        return BanefulImpactionPvE.CanUse(out act) &&
               (closeTargetCount > 3 ||
                Target.IsBossFromTTK() ||
                Player != null && StatusHelper.PlayerWillStatusEndGCD(2, 0, true, StatusID.ImpactImminent));
    }

    #endregion

    #region Party / Target Helpers

    // Counts nearby party members worth covering with Emergency Tactics Succor.
    private int CountEmergencyTacticsTargets()
    {
        int count = 0;

        foreach (IBattleChara member in PartyMembers)
        {
            if (member.DistanceToPlayer() > 15)
                continue;

            if (!member.DoomNeedHealing() && member.GetHealthRatio() >= EmergencyTacticsHeal)
                continue;

            count++;
            if (count > 1)
                break;
        }

        return count;
    }

    // Checks whether Fey Union should be cancelled because the target is healthy enough.
    private bool ShouldRemoveAetherpact()
    {
        foreach (IBattleChara member in PartyMembers)
        {
            if (member.HasStatus(true, StatusID.FeyUnion_1223) &&
                member.GetHealthRatio() >= AetherpactRemoveHpThreshold)
            {
                return true;
            }
        }

        return false;
    }

    // Checks whether any party member has a given self-owned status.
    private bool HasPartyMemberWithOwnStatus(StatusID statusId)
    {
        foreach (IBattleChara member in PartyMembers)
        {
            if (member.HasStatus(true, statusId))
                return true;
        }

        return false;
    }

    // Lockout checks for single-target healing oGCDs.
    private bool HasSingleAbilityLockoutStatus(IBattleChara? target)
    {
        if (target == null)
            return true;

        try
        {
            return target.HasStatus(false, StatusID.LivingDead) ||
                   target.HasStatus(false, StatusID.Holmgang);
        }
        catch
        {
            return true;
        }
    }

    // Lockout checks for single-target mitigation oGCDs.
    private bool HasDefenseSingleLockoutStatus(IBattleChara? target)
    {
        if (target == null)
            return true;

        try
        {
            return target.HasStatus(false, StatusID.LivingDead) ||
                   target.HasStatus(false, StatusID.Holmgang) ||
                   target.HasStatus(false, StatusID.HallowedGround) ||
                   target.HasStatus(false, StatusID.Superbolide);
        }
        catch
        {
            return true;
        }
    }

    // Validates whether a single-target heal oGCD is allowed on this target.
    private bool CanUseSingleAbilityTarget(IBattleChara? target)
    {
        if (target == null || HasSingleAbilityLockoutStatus(target))
            return false;

        return ExcogTargetIsTank(target) || PartyMembersAverHP > SingleAbilityNonTankPartyAverageGate;
    }

    // Checks whether the given target is one of the party tanks.
    private bool ExcogTargetIsTank(IBattleChara? target)
    {
        if (target == null)
            return false;

        foreach (IBattleChara tank in PartyMembers.GetJobCategory(JobRole.Tank))
        {
            if (tank == target)
                return true;
        }

        return false;
    }

    #endregion

    #region Damage / DoT Helpers

    // Calculates the target count where Art of War becomes preferable to DoTing.
    private int GetAoWBreakevenTargets()
    {
        if (!ArtOfWarPvE.EnoughLevel)
            return 100;

        if (!BroilMasteryTrait.EnoughLevel)
            return 3 - DotOffsetMobs;

        if (!ArtOfWarMasteryTrait.EnoughLevel)
            return 4 - DotOffsetMobs;

        return 5 - DotOffsetMobs;
    }

    // Very rough target HP estimate needed to survive long enough for DoT value.
    private float CalculateExpectedHpToLive12Seconds()
    {
        if (Player is null || !EnableBallparkTtkEstimator)
            return 1f;

        int partyMemberCount = 0;
        foreach (IBattleChara _ in PartyMembers)
            partyMemberCount++;

        return BallparkDamagePercent * Player.MaxHp * partyMemberCount * 12;
    }

    // Checks whether the currently relevant Bio effect is missing or expiring soon.
    private bool CurrentTargetBioMissingOrEnding(float remainingSeconds)
    {
        if (CurrentTarget == null)
            return false;

        return
            (BiolysisPvE.EnoughLevel &&
             (!CurrentTarget.HasStatus(true, StatusID.Biolysis) ||
              CurrentTarget.WillStatusEnd(remainingSeconds, true, StatusID.Biolysis))) ||
            (!BiolysisPvE.EnoughLevel && BioIiPvE.EnoughLevel &&
             (!CurrentTarget.HasStatus(true, StatusID.BioIi) ||
              CurrentTarget.WillStatusEnd(remainingSeconds, true, StatusID.BioIi))) ||
            (!BiolysisPvE.EnoughLevel && !BioIiPvE.EnoughLevel && BioPvE.EnoughLevel &&
             (!CurrentTarget.HasStatus(true, StatusID.Bio) ||
              CurrentTarget.WillStatusEnd(remainingSeconds, true, StatusID.Bio)));
    }

    // Uses the highest-level currently available Bio spell.
    private bool CanUseCurrentBio(out IAction? act, bool skipStatusProvideCheck = false)
    {
        if (BiolysisPvE.EnoughLevel &&
            BiolysisPvE.CanUse(out act, skipStatusProvideCheck: skipStatusProvideCheck))
        {
            return true;
        }

        if (!BiolysisPvE.EnoughLevel && BioIiPvE.EnoughLevel &&
            BioIiPvE.CanUse(out act, skipStatusProvideCheck: skipStatusProvideCheck))
        {
            return true;
        }

        if (!BiolysisPvE.EnoughLevel && !BioIiPvE.EnoughLevel && BioPvE.EnoughLevel &&
            BioPvE.CanUse(out act, skipStatusProvideCheck: skipStatusProvideCheck))
        {
            return true;
        }

        act = null;
        return false;
    }

    #endregion

    #region Timestamp Helpers

    // Stores timing windows used by burst/deployment follow-up logic.
    private void StampChainStratagemUse() => _chainStratagemUsedAtMs = Environment.TickCount64;

    private void StampAdloquiumUse() => _adloquiumUsedAtMs = Environment.TickCount64;

    #endregion

    #region Overrides

    public override bool CanHealSingleSpell => base.CanHealSingleSpell;

    public override bool CanHealAreaSpell => base.CanHealAreaSpell;

    #endregion
}