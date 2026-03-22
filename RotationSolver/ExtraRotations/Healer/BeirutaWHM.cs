using System.Collections.Generic;
using System.ComponentModel;

namespace RotationSolver.ExtraRotations.Healer;

[Rotation("BeirutaWHM", CombatType.PvE, GameVersion = "7.45")]
[SourceCode(Path = "main/ExtraRotations/Healer/BeirutaWHM.cs")]
public sealed class WHM_Reborn : WhiteMageRotation
{
    #region Config Options

     [RotationConfig(CombatType.PvE, Name =
        "Please note that this rotation is optimised for high-end encounters.\n" +
        "• Temperance, Plenary Indulgence and Liturgy of the Bell should generally be used manually or through CD planner\n" +
        "• Please set Intercept for GCD usage only\n" +
        "• Disabling AutoBurst is sufficient if you need to delay burst timing in this rotation\n" +
        "• Dia refresh slightly earlier during burst phases or while moving\n" +
        "• Afflatus Misery will ONLY be used during burst phases, blue lily overcap is not a damage down\n" +
        "• After 6s in combats Assize is used on cooldown in this rotation, disable it in Actions if you want to use CD planner for it\n" +
        "• Will start dumping blue lilies if not having 3 blood lilies 15 before burst\n" +
        "• Single-target healing usage is intentionally more conservative in this rotation\n")]
    public bool RotationNotes { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Limit Liturgy Of The Bell to multihit party stacks")]
    public bool MultiHitRestrict { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Enable Swiftcast Restriction Logic to attempt to prevent actions other than Raise when you have swiftcast")]
    public bool SwiftLogic { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Divine Caress as soon as its available")]
    public bool UseDivine { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Only use Benediction on tanks")]
    public bool BenedictionTankOnly { get; set; } = true;

    [Range(0, 10000, ConfigUnitType.None, 100)]
    [RotationConfig(CombatType.PvE, Name = "Casting cost requirement for Thin Air to be used")]
    public float ThinAirNeed { get; set; } = 1000;

    [Range(0, 5, ConfigUnitType.Seconds, 0.1f)]
    [RotationConfig(CombatType.PvE, Name = "Minimum movement time before allowing early Dia refresh")]
    public float MovingDiaRefreshTime { get; set; } = 1f;

    [RotationConfig(CombatType.PvE, Name = "How to manage the last thin air charge")]
    public ThinAirUsageStrategy ThinAirLastChargeUsage { get; set; } = ThinAirUsageStrategy.ReserveLastChargeForRaise;

    public enum ThinAirUsageStrategy : byte
    {
        [Description("Use all thin air charges on expensive spells")]
        UseAllCharges,

        [Description("Reserve the last charge for raise")]
        ReserveLastChargeForRaise,

        [Description("Reserve the last charge for manual use")]
        ReserveLastCharge,
    }

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum HP threshold party member needs to be to use Benediction")]
    public float BenedictionHeal { get; set; } = 0.1f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum HP threshold party member needs to be to use Tetragrammaton")]
    public float TetragrammatonHeal { get; set; } = 0.6f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum HP threshold party member needs to be to use Afflatus Solace")]
    public float SolaceHeal { get; set; } = 0.7f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum HP threshold party member needs to be to use Regen")]
    public float RegenHeal { get; set; } = 0.6f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum average HP threshold among party members needed to use Afflatus Rapture")]
    public float RaptureHeal { get; set; } = 0.7f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum average HP threshold among party members needed to use Medica III")]
    public float MedicaIIIHeal { get; set; } = 0.5f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum average HP threshold among party members needed to use Medica II")]
    public float MedicaIIHeal { get; set; } = 0.5f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum average HP threshold among party members needed to use Cure III")]
    public float CureIIIHeal { get; set; } = 0.4f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum average HP threshold among party members needed to use Asylum")]
    public float AsylumHeal { get; set; } = 0.6f;

    #endregion

    #region Helpers

    private const float PresenceOfMindDiaRefreshSeconds = 11f;
    private const float MovingDiaRefreshSeconds = 21f;

    private bool HasPresenceOfMind => StatusHelper.PlayerHasStatus(true, StatusID.PresenceOfMind);

    private bool InLast5sOfPresenceOfMind =>
        HasPresenceOfMind &&
        StatusHelper.PlayerStatusTime(true, StatusID.PresenceOfMind) <= 5f;

    private bool HasAsylum => StatusHelper.PlayerHasStatus(true, StatusID.Asylum);

    private bool HasLiturgyOfTheBell => StatusHelper.PlayerHasStatus(true, StatusID.LiturgyOfTheBell);

    private bool HasMedicaIii => StatusHelper.PlayerHasStatus(true, StatusID.MedicaIii);

    private bool HasHealingLockout => HasAsylum || HasLiturgyOfTheBell;

    private bool ShouldHoldRaiseSwift =>
        (HasSwift || IsLastAction(ActionID.SwiftcastPvE)) &&
        SwiftLogic &&
        MergedStatus.HasFlag(AutoStatus.Raise);

    private bool IsTank(IBattleChara? target)
    {
        if (target == null)
            return false;

        IEnumerable<IBattleChara> tanks = PartyMembers.GetJobCategory(JobRole.Tank);
        foreach (IBattleChara tank in tanks)
        {
            if (tank == target)
                return true;
        }

        return false;
    }

    private bool BenedictionTargetAllowed(IBattleChara? target)
    {
        if (target == null)
            return false;

        if (!BenedictionTankOnly)
            return true;

        return IsTank(target);
    }

    private static bool HasSingleHealLockoutStatus(IBattleChara? target)
    {
        if (target == null)
            return true;

        try
        {
            return target.HasStatus(false, StatusID.LivingDead) ||
                   target.HasStatus(false, StatusID.Holmgang) ||
                   target.HasStatus(false, StatusID.WalkingDead);
        }
        catch
        {
            return true;
        }
    }

    private bool CurrentTargetDiaMissingOrEnding(float remainingSeconds)
    {
        if (CurrentTarget == null)
            return false;

        return
            (DiaPvE.EnoughLevel &&
             (!CurrentTarget.HasStatus(true, StatusID.Dia) ||
              CurrentTarget.WillStatusEnd(remainingSeconds, true, StatusID.Dia))) ||
            (!DiaPvE.EnoughLevel && AeroIiPvE.EnoughLevel &&
             (!CurrentTarget.HasStatus(true, StatusID.AeroIi) ||
              CurrentTarget.WillStatusEnd(remainingSeconds, true, StatusID.AeroIi))) ||
            (!DiaPvE.EnoughLevel && !AeroIiPvE.EnoughLevel && AeroPvE.EnoughLevel &&
             (!CurrentTarget.HasStatus(true, StatusID.Aero) ||
              CurrentTarget.WillStatusEnd(remainingSeconds, true, StatusID.Aero)));
    }

    private bool CanUseCurrentDia(out IAction? act, bool skipStatusProvideCheck = false)
    {
        if (DiaPvE.EnoughLevel &&
            DiaPvE.CanUse(out act, skipStatusProvideCheck: skipStatusProvideCheck))
        {
            return true;
        }

        if (!DiaPvE.EnoughLevel && AeroIiPvE.EnoughLevel &&
            AeroIiPvE.CanUse(out act, skipStatusProvideCheck: skipStatusProvideCheck))
        {
            return true;
        }

        if (!DiaPvE.EnoughLevel && !AeroIiPvE.EnoughLevel && AeroPvE.EnoughLevel &&
            AeroPvE.CanUse(out act, skipStatusProvideCheck: skipStatusProvideCheck))
        {
            return true;
        }

        act = null;
        return false;
    }

    #endregion

    #region Countdown Logic

    protected override IAction? CountDownAction(float remainTime)
{
    IAction? act;

    if (remainTime < 3 && UseBurstMedicine(out act))
        return act;

    if (remainTime < StonePvE.Info.CastTime + CountDownAhead &&
        StonePvE.CanUse(out act))
    {
        return act;
    }

    return base.CountDownAction(remainTime);
}

    #endregion

    #region oGCD Logic

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        bool useLastThinAirCharge =
            ThinAirLastChargeUsage == ThinAirUsageStrategy.UseAllCharges ||
            (ThinAirLastChargeUsage == ThinAirUsageStrategy.ReserveLastChargeForRaise && nextGCD == RaisePvE);

        if (((nextGCD is IBaseAction action && action.Info.MPNeed >= ThinAirNeed && IsLastAction() == IsLastGCD()) ||
             ((MergedStatus.HasFlag(AutoStatus.Raise) || nextGCD == RaisePvE) && IsLastAction() == IsLastGCD())) &&
            ThinAirPvE.CanUse(out act, usedUp: useLastThinAirCharge))
        {
            return true;
        }

        if (IsBurst &&
            PresenceOfMindPvE.Cooldown.WillHaveOneCharge(5) &&
            UseBurstMedicine(out act))
        {
            return true;
        }

        if (IsBurst && CombatTime > 6f && PresenceOfMindPvE.CanUse(out act, skipTTKCheck: IsInHighEndDuty))
        {
            return true;
        }

        if (StatusHelper.PlayerWillStatusEndGCD(2, 0, true, StatusID.DivineGrace) &&
            DivineCaressPvE.CanUse(out act))
        {
            return true;
        }

        if (HasHealingLockout)
        {
            return base.EmergencyAbility(nextGCD, out act);
        }

        if (nextGCD.IsTheSameTo(true, AfflatusRapturePvE, MedicaPvE, MedicaIiPvE, CureIiiPvE) &&
            (MergedStatus.HasFlag(AutoStatus.HealAreaSpell) || MergedStatus.HasFlag(AutoStatus.HealSingleSpell)))
        {
            if (PlenaryIndulgencePvE.CanUse(out act))
            {
                return true;
            }
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        if (UseDivine && DivineCaressPvE.CanUse(out act))
        {
            return true;
        }

        return base.GeneralAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.TemperancePvE, ActionID.LiturgyOfTheBellPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if ((TemperancePvE.Cooldown.IsCoolingDown && !TemperancePvE.Cooldown.WillHaveOneCharge(100)) ||
            (LiturgyOfTheBellPvE.Cooldown.IsCoolingDown && !LiturgyOfTheBellPvE.Cooldown.WillHaveOneCharge(160)))
        {
            return base.DefenseAreaAbility(nextGCD, out act);
        }

        if (MultiHitRestrict && IsCastingMultiHit)
        {
            if (LiturgyOfTheBellPvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }
        }

        if (PlenaryIndulgencePvE.CanUse(out act))
        {
            return true;
        }

        if (TemperancePvE.CanUse(out act))
        {
            return true;
        }

        if (DivineCaressPvE.CanUse(out act))
        {
            return true;
        }

        if ((MultiHitRestrict && IsCastingMultiHit) || !MultiHitRestrict)
        {
            if (LiturgyOfTheBellPvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }
        }

        return base.DefenseAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.DivineBenisonPvE, ActionID.AquaveilPvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        if ((DivineBenisonPvE.Cooldown.IsCoolingDown && !DivineBenisonPvE.Cooldown.WillHaveOneCharge(15)) ||
            (AquaveilPvE.Cooldown.IsCoolingDown && !AquaveilPvE.Cooldown.WillHaveOneCharge(52)))
        {
            return base.DefenseSingleAbility(nextGCD, out act);
        }

        if (DivineBenisonPvE.CanUse(out act))
        {
            return true;
        }

        if (AquaveilPvE.CanUse(out act))
        {
            return true;
        }

        return base.DefenseSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.AsylumPvE)]
    protected override bool HealAreaAbility(IAction nextGCD, out IAction? act)
    {
        act = null;

        if (HasHealingLockout)
            return false;

        if (PartyMembersAverHP < AsylumHeal &&
            AsylumPvE.CanUse(out act))
        {
            return true;
        }

        return base.HealAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.BenedictionPvE, ActionID.TetragrammatonPvE)]
    protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        act = null;

        if (HasHealingLockout)
            return false;

        IBattleChara? benedictionTarget = RegenPvE.Target.Target;

        if (!HasSingleHealLockoutStatus(benedictionTarget) &&
            BenedictionTargetAllowed(benedictionTarget) &&
            PartyMembersAverHP > 0.8f &&
            BenedictionPvE.CanUse(out act) &&
            benedictionTarget != null &&
            benedictionTarget.GetHealthRatio() < BenedictionHeal)
        {
            return true;
        }

        if (IsLastAction(ActionID.BenedictionPvE))
        {
            return base.HealSingleAbility(nextGCD, out act);
        }

        IBattleChara? tetraTarget = RegenPvE.Target.Target;

        if (!HasSingleHealLockoutStatus(tetraTarget) &&
            PartyMembersAverHP > 0.8f &&
            TetragrammatonPvE.CanUse(out act, usedUp: true) &&
            tetraTarget != null &&
            tetraTarget.GetHealthRatio() < TetragrammatonHeal)
        {
            return true;
        }

        return base.HealSingleAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (InCombat)
        {
            if (IsBurst && CombatTime > 6f && PresenceOfMindPvE.CanUse(out act, skipTTKCheck: IsInHighEndDuty))
            {
                return true;
            }

            if (!HasHealingLockout &&
                CombatTime > 6f &&
                AssizePvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }

            if (CombatTime > 6f && AssizePvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }
        }

        return base.AttackAbility(nextGCD, out act);
    }

    #endregion

    #region GCD Logic

    [RotationDesc(ActionID.AfflatusRapturePvE, ActionID.MedicaIiPvE, ActionID.CureIiiPvE, ActionID.MedicaPvE)]
    protected override bool HealAreaGCD(out IAction? act)
    {
        act = null;

        if (HasHealingLockout || HasPresenceOfMind)
            return false;

        if (ShouldHoldRaiseSwift)
            return base.HealAreaGCD(out act);

        if (BloodLily != 3 &&
    MovingTime > 1f &&
    PartyMembersAverHP < 0.8f &&
    AfflatusRapturePvE.CanUse(out act))
{
    return true;
}

        if (BloodLily != 3 &&
            PartyMembersAverHP < RaptureHeal &&
            AfflatusRapturePvE.CanUse(out act))
        {
            return true;
        }

        int hasMedica2 = 0;
        foreach (IBattleChara n in PartyMembers)
        {
            if (n.HasStatus(true, StatusID.MedicaIi))
            {
                hasMedica2++;
            }
        }

        int partyCount = 0;
        foreach (IBattleChara _ in PartyMembers)
        {
            partyCount++;
        }

        if (MedicaIiPvE.EnoughLevel)
        {
            if (MedicaIiiPvE.EnoughLevel &&
                PartyMembersAverHP < MedicaIIIHeal &&
                MedicaIiiPvE.CanUse(out act) &&
                hasMedica2 < partyCount / 2 &&
                !IsLastAction(true, MedicaIiPvE))
            {
                return true;
            }

            if (!MedicaIiiPvE.EnoughLevel &&
                PartyMembersAverHP < MedicaIIHeal &&
                MedicaIiPvE.CanUse(out act) &&
                hasMedica2 < partyCount / 2 &&
                !IsLastAction(true, MedicaIiPvE))
            {
                return true;
            }
        }

        if (HasMedicaIii && PartyMembersAverHP < CureIIIHeal &&
            CureIiiPvE.CanUse(out act))
        {
            return true;
        }

        if (HasMedicaIii && PartyMembersAverHP < 0.3f &&
            MedicaPvE.CanUse(out act))
        {
            return true;
        }

        return base.HealAreaGCD(out act);
    }

    [RotationDesc(ActionID.AfflatusSolacePvE, ActionID.RegenPvE, ActionID.CureIiPvE, ActionID.CurePvE)]
protected override bool HealSingleGCD(out IAction? act)
{
    act = null;

    if (HasHealingLockout || HasPresenceOfMind)
        return false;

    if (ShouldHoldRaiseSwift)
        return base.HealSingleGCD(out act);

    IBattleChara? solaceTarget = AfflatusSolacePvE.Target.Target;

    // Movement-based loose Solace (any target)
    if (!HasSingleHealLockoutStatus(solaceTarget) &&
        solaceTarget != null &&
        PartyMembersAverHP > 0.8f &&
        MovingTime > 1f &&
        BloodLily != 3 &&
        solaceTarget.GetHealthRatio() < 0.75f &&
        AfflatusSolacePvE.CanUse(out act))
    {
        return true;
    }

    // Normal Solace logic
    if (!HasSingleHealLockoutStatus(solaceTarget) &&
        PartyMembersAverHP > 0.8f &&
        BloodLily != 3 &&
        solaceTarget != null &&
        solaceTarget.GetHealthRatio() < SolaceHeal &&
        AfflatusSolacePvE.CanUse(out act))
    {
        return true;
    }

    IBattleChara? regenTarget = RegenPvE.Target.Target;

    // Movement-based loose Regen (tank only)
    if (!HasSingleHealLockoutStatus(regenTarget) &&
        regenTarget != null &&
        IsTank(regenTarget) &&
        MovingTime > 1f &&
        regenTarget.GetHealthRatio() < 0.8f &&
        RegenPvE.CanUse(out act))
    {
        return true;
    }

    // Normal Regen logic
    if (!HasSingleHealLockoutStatus(regenTarget) &&
        PartyMembersAverHP > 0.8f &&
        regenTarget != null &&
        regenTarget.GetHealthRatio() < RegenHeal &&
        RegenPvE.CanUse(out act))
    {
        return true;
    }

    IBattleChara? cure2Target = CureIiPvE.Target.Target;
    if (!HasSingleHealLockoutStatus(cure2Target) &&
        PartyMembersAverHP > 0.9f &&
        cure2Target != null &&
        cure2Target.GetHealthRatio() < 0.3f &&
        CureIiPvE.CanUse(out act))
    {
        return true;
    }

    IBattleChara? cure1Target = CurePvE.Target.Target;
    if (!HasSingleHealLockoutStatus(cure1Target) &&
        PartyMembersAverHP > 0.9f &&
        cure1Target != null &&
        cure1Target.GetHealthRatio() < 0.3f &&
        CurePvE.CanUse(out act))
    {
        return true;
    }

    return base.HealSingleGCD(out act);
}

    [RotationDesc(ActionID.RaisePvE)]
    protected override bool RaiseGCD(out IAction? act)
    {
        if (RaisePvE.CanUse(out act))
        {
            return true;
        }

        return base.RaiseGCD(out act);
    }

    protected override bool GeneralGCD(out IAction? act)
    {
        act = null;

        if (HasThinAir && MergedStatus.HasFlag(AutoStatus.Raise))
        {
            return RaiseGCD(out act);
        }

        if (ShouldHoldRaiseSwift)
            return base.GeneralGCD(out act);

        if (!HasHealingLockout &&
            !HasPresenceOfMind &&
            IsBurst &&
            BloodLily < 3 &&
            PresenceOfMindPvE.Cooldown.WillHaveOneCharge(15) &&
            AfflatusRapturePvE.CanUse(out act))
        {
            return true;
        }

        if (HasPresenceOfMind && AfflatusMiseryPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        if (GlareIvPvE.CanUse(out act))
        {
            return true;
        }

        if (!HasHealingLockout &&
            !HasPresenceOfMind &&
            BloodLily != 3 &&
            StatusHelper.PlayerHasStatus(true, StatusID.Confession) &&
            StatusHelper.PlayerWillStatusEndGCD(1, 0, true, StatusID.Confession))
        {
            if (PartyMembersAverHP < RaptureHeal &&
                AfflatusRapturePvE.CanUse(out act))
            {
                return true;
            }
        }

        if (HolyPvE.EnoughLevel)
        {
            if (HolyIiiPvE.EnoughLevel && HolyIiiPvE.CanUse(out act))
            {
                return true;
            }

            if (!HolyIiiPvE.EnoughLevel && HolyPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (InCombat &&
            InLast5sOfPresenceOfMind &&
            CurrentTargetDiaMissingOrEnding(PresenceOfMindDiaRefreshSeconds) &&
            CanUseCurrentDia(out act, skipStatusProvideCheck: true))
        {
            return true;
        }

        if (InCombat &&
            MovingTime >= MovingDiaRefreshTime &&
            CurrentTargetDiaMissingOrEnding(MovingDiaRefreshSeconds) &&
            CanUseCurrentDia(out act, skipStatusProvideCheck: true))
        {
            return true;
        }

        if (CanUseCurrentDia(out act))
        {
            return true;
        }

        if (GlareIiiPvE.EnoughLevel && GlareIiiPvE.CanUse(out act))
        {
            return true;
        }

        if (GlarePvE.EnoughLevel && !GlareIiiPvE.EnoughLevel && GlarePvE.CanUse(out act))
        {
            return true;
        }

        if (StoneIvPvE.EnoughLevel && !GlarePvE.EnoughLevel && StoneIvPvE.CanUse(out act))
        {
            return true;
        }

        if (StoneIiiPvE.EnoughLevel && !StoneIvPvE.EnoughLevel && StoneIiiPvE.CanUse(out act))
        {
            return true;
        }

        if (StoneIiPvE.EnoughLevel && !StoneIiiPvE.Info.EnoughLevelAndQuest() && StoneIiPvE.CanUse(out act))
        {
            return true;
        }

        if (!StoneIiPvE.EnoughLevel && StonePvE.CanUse(out act))
        {
            return true;
        }

        return base.GeneralGCD(out act);
    }

    #endregion
}