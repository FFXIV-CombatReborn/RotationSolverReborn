using System;
using System.ComponentModel;
using System.Linq;

namespace RotationSolver.ExtraRotations.Healer;

[Rotation("BeirutaSGE", CombatType.PvE, GameVersion = "7.45", Description = "Semi-Automatic Savage/Ultimate rotation, need to used with CD planner or manual inputs")]
[SourceCode(Path = "main/ExtraRotations/Healer/BeirutaSGE.cs")]
public sealed class BeirutaSGE : SageRotation
{
    #region Config Options

    [RotationConfig(CombatType.PvE, Name =
        "Please note that this rotation is optimised for high-end encounters.\n" +
        "• Healing actions are designed to be used automatically, while mitigation is kept minimal to better support CD planner or manual input\n" +
        "• Only the actions listed in the description will be automatically used and everything else should be used manually or through CD planner\n" +
        "• If no raid buffs in the team, please set Intercept to GCD usage and use last stack of Phlegma manually where required\n" +
        "• Disabling AutoBurst is sufficient if you need to delay burst timing in this rotation\n" +
        "• Applying Zoe to yourself will be treated as a signal to use Pneuma or Eukrasian Prognosis depending on average party HP settings\n" +
        "• Use Zoe or marco rotation DefenseArea to manually trigger an Eukrasian Prognosis\n" +
        "• Single-target GCD healing heavily restricted in this rotation\n" +
        "• If you enabled countdown zoe/shield be aware of haters who might bait you with short countdowns (Turn StartOnCountdown False when away)\n")]
    public bool RotationNotes { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Attempt to prevent bricking by allowing E.Prog at the end of GCD logic (experimental)")]
    public bool AntiBrick { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Eukrasia when out of combat")]
    public bool OOCEukrasia { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Rhizomata when out of combat")]
    public bool OOCRhizomata { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Limit Panhaima to multihit party stacks")]
    public bool MultiHitRestrict { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Enable Swiftcast Restriction Logic to attempt to prevent actions other than Raise when you have swiftcast")]
    public bool SwiftLogic { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Swiftcast for movement")]
    public bool UseSwiftcastForMovement { get; set; } = true;

    [Range(0, 5, ConfigUnitType.Seconds, 0.1f)]
    [RotationConfig(CombatType.PvE, Name = "Minimum movement time before allowing movement-based actions")]
    public float MovementTimeThreshold { get; set; } = 0.9f;

    [RotationConfig(CombatType.PvE, Name = "Lock healing actions while Macrocosmos is active")]
    public bool LockHealingActionsDuringMacrocosmos { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Zoe during the countdown opener")]
    public bool UseZoeInOpener { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Eukrasian Prognosis during the countdown opener")]
    public bool EukrasianPrognosisDuringCountdown { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Which opener to use")]
    public OpenerStrategy OpenerSelection { get; set; } = OpenerStrategy.PneumaOpener;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Health threshold target needs to be to use Taurochole")]
    public float TaurocholeHeal { get; set; } = 0.7f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Health threshold target needs to be to use Druochole")]
    public float DruocholeHeal { get; set; } = 0.6f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Health threshold Kardion target needs to be to use Soteria")]
    public float SoteriaHeal { get; set; } = 0.8f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Average party HP threshold to use Pepsis")]
    public float PepsisHeal { get; set; } = 0.4f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Average party HP threshold to use Ixochole")]
    public float IxocholeHeal { get; set; } = 0.8f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Average party HP threshold to use Physis")]
    public float PhysisHeal { get; set; } = 0.5f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Average party HP threshold to use Pneuma in Single Target")]
    public float PneumaHeal { get; set; } = 0.40f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Average party HP threshold to use Pneuma in Multi Targets")]
    public float PneumaDyskrasiaHeal { get; set; } = 0.70f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Average party HP threshold to use Taurochole")]
    public float HealSingleTaurocholeHeal { get; set; } = 0.7f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Average party HP threshold to use Druochole")]
    public float HealSingleDruocholeHeal { get; set; } = 0.6f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Average party HP threshold to use Zoe on Pneuma (if lower) or Eukrasian Prognosis (if higher)")]
    public float ZoePneumaHeal { get; set; } = 0.40f;

    public enum OpenerStrategy : byte
    {
        [Description("Use Toxikon prepull opener")]
        ToxikonOpener,

        [Description("Use Pneuma prepull opener")]
        PneumaOpener,
    }

    #endregion

    #region Constants / Fields

    private const long PsycheDotRefreshMs = 15_000;
    private const float EarlyDotRefreshSeconds = 12f;
    private const float SwiftcastPostActionLockSeconds = 2f;
    private const float MovementLeadSeconds = 0.5f;

    private long _psycheUsedAtMs;
    private float _lastSwiftcastLockingActionCombatTime = float.MinValue;

    #endregion

    #region Tracking Properties

    private IBaseAction? _lastEukrasiaActionAim;
    private IBaseAction? _EukrasiaActionAim;

    private bool HasKerachole => StatusHelper.PlayerHasStatus(true, StatusID.Kerachole);
    private bool HasZoe => StatusHelper.PlayerHasStatus(true, StatusID.Zoe);
    private bool HasMacrocosmos => StatusHelper.PlayerHasStatus(true, StatusID.Macrocosmos);
    private bool HasEukrasianPrognosis => StatusHelper.PlayerHasStatus(true, StatusID.EukrasianPrognosis);

    private bool InFirst20sAfterPsyche =>
        _psycheUsedAtMs != 0 &&
        Environment.TickCount64 - _psycheUsedAtMs < PsycheDotRefreshMs;

    private bool HasSufficientMovement =>
        IsMoving &&
        MovingTime > MovementTimeThreshold;

    private bool HasHealingLockout =>
        LockHealingActionsDuringMacrocosmos &&
        HasMacrocosmos;

    private bool IsSwiftcastPostActionLockActive =>
        InCombat &&
        _lastSwiftcastLockingActionCombatTime > float.MinValue / 2 &&
        CombatTime - _lastSwiftcastLockingActionCombatTime <= SwiftcastPostActionLockSeconds;

    private bool ShouldDeferToRaise() =>
        (HasSwift || IsLastAction(ActionID.SwiftcastPvE)) &&
        SwiftLogic &&
        MergedStatus.HasFlag(AutoStatus.Raise);

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

    private void UpdateActionTracking()
    {
        if (!InCombat)
        {
            _lastSwiftcastLockingActionCombatTime = float.MinValue;
            return;
        }

        if (IsLastAction(ActionID.EukrasiaPvE) ||
            IsLastAction(ActionID.PhlegmaPvE) ||
            IsLastAction(ActionID.ToxikonPvE) ||
            IsLastAction(ActionID.ToxikonIiPvE) ||
            IsLastAction(ActionID.DyskrasiaPvE) ||
            IsLastAction(ActionID.DyskrasiaIiPvE) ||
            IsLastAction(ActionID.EsunaPvE))
        {
            _lastSwiftcastLockingActionCombatTime = CombatTime;
        }
    }

    private bool IsMovementPreferredNextGCD(IAction nextGCD)
    {
        if (nextGCD == EukrasiaPvE ||
            nextGCD == PhlegmaPvE ||
            nextGCD == ToxikonPvE ||
            nextGCD == ToxikonIiPvE ||
            nextGCD == DyskrasiaPvE ||
            nextGCD == DyskrasiaIiPvE ||
            nextGCD == EsunaPvE)
        {
            return true;
        }

        return false;
    }

    private bool ShouldSwiftcastForMovement(IAction nextGCD)
    {
        if (!UseSwiftcastForMovement ||
            !InCombat ||
            !HasSufficientMovement ||
            MovingTime <= MovementTimeThreshold + MovementLeadSeconds ||
            HasSwift ||
            Addersting > 0 ||
            IsLastAction(ActionID.SwiftcastPvE) ||
            ShouldDeferToRaise() ||
            IsSwiftcastPostActionLockActive)
        {
            return false;
        }

        return !IsMovementPreferredNextGCD(nextGCD);
    }

    private bool CanUseCountdownEukrasianPrognosis(out IAction? act)
    {
        act = null;

        if (EukrasianPrognosisIiPvE.EnoughLevel &&
            EukrasianPrognosisIiPvE.IsEnabled &&
            EukrasianPrognosisIiPvE.CanUse(out act))
        {
            return true;
        }

        if (EukrasianPrognosisPvE.EnoughLevel &&
            EukrasianPrognosisPvE.IsEnabled &&
            EukrasianPrognosisPvE.CanUse(out act))
        {
            return true;
        }

        return false;
    }

    private bool ActionTargetBelow(IBaseAction action, float threshold)
        => action.Target.Target?.GetHealthRatio() < threshold;

    private bool AnyPartyMemberBelow(float threshold)
        => PartyMembers.Any(member => member.GetHealthRatio() < threshold);

    public override void DisplayRotationStatus()
    {
        ImGui.Text($"Last E.Action Aim Cleared From Queue: {_lastEukrasiaActionAim}");
        ImGui.Text($"Current E.Action Aim: {_EukrasiaActionAim}");
        ImGui.Text($"Swiftcast Movement Lock: {IsSwiftcastPostActionLockActive}");
    }

    #endregion

    #region Countdown Logic

    protected override IAction? CountDownAction(float remainTime)
    {
        IAction? act;

        if (OpenerSelection == OpenerStrategy.PneumaOpener
            && remainTime < PneumaPvE.Info.CastTime + CountDownAhead
            && PneumaPvE.CanUse(out act))
        {
            return act;
        }

        if (remainTime <= 2.1f && UseBurstMedicine(out act))
        {
            return act;
        }

        if (OpenerSelection == OpenerStrategy.ToxikonOpener
            && remainTime < 1.5f + CountDownAhead
            && ToxikonIiPvE.CanUse(out act))
        {
            return act;
        }

        if (remainTime is < 7f and > 6f
            && EukrasianPrognosisDuringCountdown
            && HasEukrasia
            && CanUseCountdownEukrasianPrognosis(out act))
        {
            return act;
        }

        if (remainTime is < 8f and > 7f
            && EukrasianPrognosisDuringCountdown
            && !HasEukrasia
            && EukrasiaPvE.CanUse(out act))
        {
            return act;
        }

        if (remainTime <= 15f
            && UseZoeInOpener
            && ZoePvE.CanUse(out act))
        {
            return act;
        }

        if (remainTime <= 5f && EukrasiaPvE.CanUse(out act))
        {
            return act;
        }

        if (remainTime <= 15f && KardiaPvE.CanUse(out act))
        {
            return act;
        }

        return base.CountDownAction(remainTime);
    }

    #endregion

    #region oGCD Logic

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        UpdateActionTracking();

        act = null;

        if (IsLastGCD(false,
                EukrasianPrognosisIiPvE,
                EukrasianPrognosisPvE,
                EukrasianDiagnosisPvE,
                EukrasianDyskrasiaPvE,
                EukrasianDosisIiiPvE,
                EukrasianDosisIiPvE,
                EukrasianDosisPvE)
            || !InCombat)
        {
            ClearEukrasia(nextGCD);
        }

        if (ChoiceEukrasia(out act))
        {
            return true;
        }

        if (ZoePvE.EnoughLevel && !ZoePvE.Cooldown.IsCoolingDown)
        {
            if (PartyMembersAverHP < 0.5f
                && nextGCD.IsTheSameTo(false, PneumaPvE)
                && ZoePvE.CanUse(out act))
            {
                return true;
            }
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.PsychePvE, ActionID.SwiftcastPvE)]
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        UpdateActionTracking();

        act = null;

        if (ShouldSwiftcastForMovement(nextGCD) &&
            SwiftcastPvE.CanUse(out act))
        {
            return true;
        }

        if (IsBurst && CombatTime > 9f && PsychePvE.CanUse(out act))
        {
            StampPsycheUse();
            return true;
        }

        return base.AttackAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.HaimaPvE, ActionID.TaurocholePvE, ActionID.KrasisPvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        UpdateActionTracking();

        act = null;

        if (KrasisPvE.CanUse(out act))
        {
            return true;
        }

        if (HaimaPvE.CanUse(out act))
        {
            return true;
        }

        if (!HasKerachole
            && TaurocholePvE.CanUse(out act)
            && ActionTargetBelow(TaurocholePvE, TaurocholeHeal))
        {
            return true;
        }

        return base.DefenseSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.PhysisPvE, ActionID.PepsisPvE, ActionID.IxocholePvE)]
    protected override bool HealAreaAbility(IAction nextGCD, out IAction? act)
    {
        UpdateActionTracking();

        act = null;

        if (HasHealingLockout)
            return false;

        if (!MergedStatus.HasFlag(AutoStatus.DefenseArea)
            && !MergedStatus.HasFlag(AutoStatus.DefenseSingle)
            && PartyMembersAverHP < PepsisHeal
            && PepsisPvE.CanUse(out act))
        {
            return true;
        }

        if (Addersgall > 1 && PartyMembersAverHP < IxocholeHeal && IxocholePvE.CanUse(out act))
        {
            return true;
        }

        if (Addersgall <= 1 && PartyMembersAverHP < PhysisHeal && PhysisIiPvE.CanUse(out act))
        {
            return true;
        }

        if (Addersgall <= 1 &&PartyMembersAverHP < PhysisHeal && !PhysisIiPvE.EnoughLevel && PhysisPvE.CanUse(out act))
        {
            return true;
        }

        return base.HealAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.KrasisPvE, ActionID.TaurocholePvE, ActionID.DruocholePvE, ActionID.KardiaPvE, ActionID.SoteriaPvE)]
    protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        UpdateActionTracking();

        act = null;

        if (HasHealingLockout)
            return false;

        IBattleChara? druocholeTarget = DruocholePvE.Target.Target;
        if (Addersgall > 2 &&
            druocholeTarget != null &&
            druocholeTarget.GetHealthRatio() < 0.9f &&
            DruocholePvE.CanUse(out act))
        {
            return true;
        }

        if (KrasisPvE.CanUse(out act))
        {
            return true;
        }

        IBattleChara? taurocholeTarget = TaurocholePvE.Target.Target;
        if (!HasSingleHealLockoutStatus(taurocholeTarget) &&
            PartyMembersAverHP > 0.8f &&
            Addersgall >= 2 &&
            taurocholeTarget != null &&
            taurocholeTarget.GetHealthRatio() < HealSingleTaurocholeHeal &&
            TaurocholePvE.CanUse(out act))
        {
            return true;
        }

        if (IsLastAction(ActionID.TaurocholePvE))
        {
            return base.HealSingleAbility(nextGCD, out act);
        }

        

        if (!HasSingleHealLockoutStatus(druocholeTarget) &&
            PartyMembersAverHP > 0.8f &&
            Addersgall < 3 &&
            druocholeTarget != null &&
            druocholeTarget.GetHealthRatio() < HealSingleDruocholeHeal &&
            (!TaurocholePvE.EnoughLevel || TaurocholePvE.Cooldown.IsCoolingDown) &&
            DruocholePvE.CanUse(out act))
        {
            return true;
        }

        IBattleChara? kardiaTarget = KardiaPvE.Target.Target;
        if (!HasSingleHealLockoutStatus(kardiaTarget) &&
            PartyMembersAverHP > 0.8f &&
            kardiaTarget != null &&
            kardiaTarget.GetHealthRatio() < 0.8f &&
            KardiaPvE.CanUse(out act))
        {
            return true;
        }

        return base.HealSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.KardiaPvE, ActionID.RhizomataPvE, ActionID.SoteriaPvE)]
    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        UpdateActionTracking();

        act = null;

        if (InCombat || !HasKardia)
        {
            if (KardiaPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (OOCRhizomata && !InCombat && Addersgall <= 1 && RhizomataPvE.CanUse(out act))
        {
            return true;
        }

        if (InCombat && Addersgall <= 1 && RhizomataPvE.CanUse(out act))
        {
            return true;
        }

        IBattleChara? soteriaTarget = PartyMembers.FirstOrDefault(member =>
            member.HasStatus(true, StatusID.Kardion));

        if (PartyMembersAverHP > 0.8f &&
            soteriaTarget != null &&
            soteriaTarget.GetHealthRatio() < SoteriaHeal &&
            SoteriaPvE.CanUse(out act))
        {
            return true;
        }

        if (HasBuffs && UseBurstMedicine(out act))
        {
            return true;
        }

        return base.GeneralAbility(nextGCD, out act);
    }

    #endregion

    #region Eukrasia Logic

    private void SetEukrasia(IBaseAction act)
    {
        if (act == null || (_EukrasiaActionAim != null && IsLastGCD(true, _EukrasiaActionAim)))
        {
            return;
        }

        _EukrasiaActionAim = act;
    }

    private void ClearEukrasia(IAction nextGCD)
    {
        if (_EukrasiaActionAim != null)
        {
            _lastEukrasiaActionAim = _EukrasiaActionAim;
            _EukrasiaActionAim = null;

            if (HasEukrasia
                && ((InCombat && HasHostilesInMaxRange && nextGCD == null && SecondsSinceLastNextGCDChange >= 2f)
                    || (!InCombat && !HasHostilesInMaxRange)))
            {
                StatusHelper.StatusOff(StatusID.Eukrasia);
            }
        }
    }

    private bool ChoiceEukrasia(out IAction? act)
    {
        act = null;
        bool selected = false;

        if (EukrasianPrognosisIiPvE.EnoughLevel
            && EukrasianPrognosisIiPvE.IsEnabled
            && Addersting < 3
            && !HasBuffs
            && MovingTime > MovementTimeThreshold
            && MergedStatus.HasFlag(AutoStatus.DefenseArea)
            && EukrasianPrognosisIiPvE.CanUse(out _))
        {
            SetEukrasia(EukrasianPrognosisIiPvE);
            selected = true;
        }
        else if (!EukrasianPrognosisIiPvE.EnoughLevel
            && EukrasianPrognosisPvE.EnoughLevel
            && EukrasianPrognosisPvE.IsEnabled
            && Addersting < 3
            && !HasBuffs
            && MovingTime > MovementTimeThreshold
            && MergedStatus.HasFlag(AutoStatus.DefenseArea)
            && EukrasianPrognosisPvE.CanUse(out _))
        {
            SetEukrasia(EukrasianPrognosisPvE);
            selected = true;
        }
        else if (EukrasianDiagnosisPvE.EnoughLevel
            && EukrasianDiagnosisPvE.IsEnabled
            && Addersting < 3
            && !HasBuffs
            && MovingTime > MovementTimeThreshold
            && MergedStatus.HasFlag(AutoStatus.DefenseSingle)
            && EukrasianDiagnosisPvE.CanUse(out _))
        {
            SetEukrasia(EukrasianDiagnosisPvE);
            selected = true;
        }
        else if (EukrasianDyskrasiaPvE.EnoughLevel
            && EukrasianDyskrasiaPvE.IsEnabled
            && !MergedStatus.HasFlag(AutoStatus.DefenseSingle)
            && !MergedStatus.HasFlag(AutoStatus.DefenseArea)
            && EukrasianDyskrasiaPvE.CanUse(out _))
        {
            SetEukrasia(EukrasianDyskrasiaPvE);
            selected = true;
        }
        else if ((!EukrasianDyskrasiaPvE.CanUse(out _) || !DyskrasiaPvE.CanUse(out _))
            && EukrasianDosisIiiPvE.CanUse(out _)
            && EukrasianDosisIiiPvE.EnoughLevel
            && !MergedStatus.HasFlag(AutoStatus.DefenseSingle)
            && !MergedStatus.HasFlag(AutoStatus.DefenseArea)
            && EukrasianDosisIiiPvE.IsEnabled)
        {
            SetEukrasia(EukrasianDosisIiiPvE);
            selected = true;
        }
        else if ((!EukrasianDyskrasiaPvE.CanUse(out _) || !DyskrasiaPvE.CanUse(out _))
            && EukrasianDosisIiPvE.CanUse(out _)
            && !EukrasianDosisIiiPvE.EnoughLevel
            && EukrasianDosisIiPvE.EnoughLevel
            && !MergedStatus.HasFlag(AutoStatus.DefenseSingle)
            && !MergedStatus.HasFlag(AutoStatus.DefenseArea)
            && EukrasianDosisIiPvE.IsEnabled)
        {
            SetEukrasia(EukrasianDosisIiPvE);
            selected = true;
        }
        else if ((!EukrasianDyskrasiaPvE.CanUse(out _) || !DyskrasiaPvE.CanUse(out _))
            && EukrasianDosisPvE.CanUse(out _)
            && !EukrasianDosisIiPvE.EnoughLevel
            && EukrasianDosisPvE.EnoughLevel
            && !MergedStatus.HasFlag(AutoStatus.DefenseSingle)
            && !MergedStatus.HasFlag(AutoStatus.DefenseArea)
            && EukrasianDosisPvE.IsEnabled)
        {
            SetEukrasia(EukrasianDosisPvE);
            selected = true;
        }

        if (!selected)
        {
            return false;
        }

        if (!HasEukrasia && EukrasiaPvE.CanUse(out act))
        {
            return true;
        }

        return false;
    }

    #endregion

    #region Eukrasia Execution

    private bool DoEukrasianPrognosisIi(out IAction? act)
    {
        act = null;

        if (_EukrasiaActionAim != EukrasianPrognosisIiPvE)
        {
            return false;
        }

        if (!HasEukrasia)
        {
            return EukrasiaPvE.CanUse(out act);
        }

        return EukrasianPrognosisIiPvE.CanUse(out act);
    }

    private bool DoEukrasianPrognosis(out IAction? act)
    {
        act = null;

        if (_EukrasiaActionAim != EukrasianPrognosisPvE)
        {
            return false;
        }

        if (!HasEukrasia)
        {
            return EukrasiaPvE.CanUse(out act);
        }

        return EukrasianPrognosisPvE.CanUse(out act);
    }

    private bool DoEukrasianDiagnosis(out IAction? act)
    {
        act = null;

        if (_EukrasiaActionAim != EukrasianDiagnosisPvE)
        {
            return false;
        }

        if (!HasEukrasia)
        {
            return EukrasiaPvE.CanUse(out act);
        }

        return EukrasianDiagnosisPvE.CanUse(out act);
    }

    private bool DoEukrasianDyskrasia(out IAction? act)
    {
        act = null;

        if (_EukrasiaActionAim != EukrasianDyskrasiaPvE)
        {
            return false;
        }

        if (!HasEukrasia)
        {
            return EukrasiaPvE.CanUse(out act);
        }

        return EukrasianDyskrasiaPvE.CanUse(out act);
    }

    private bool DoEukrasianDosisIii(out IAction? act)
    {
        act = null;

        if (_EukrasiaActionAim != EukrasianDosisIiiPvE)
        {
            return false;
        }

        if (!HasEukrasia)
        {
            return EukrasiaPvE.CanUse(out act);
        }

        return EukrasianDosisIiiPvE.CanUse(out act);
    }

    private bool DoEukrasianDosisIi(out IAction? act)
    {
        act = null;

        if (_EukrasiaActionAim != EukrasianDosisIiPvE)
        {
            return false;
        }

        if (!HasEukrasia)
        {
            return EukrasiaPvE.CanUse(out act);
        }

        return EukrasianDosisIiPvE.CanUse(out act);
    }

    private bool DoEukrasianDosis(out IAction? act)
    {
        act = null;

        if (_EukrasiaActionAim != EukrasianDosisPvE)
        {
            return false;
        }

        if (!HasEukrasia)
        {
            return EukrasiaPvE.CanUse(out act);
        }

        return EukrasianDosisPvE.CanUse(out act);
    }

    #endregion

    #region Damage / DoT Helpers

    private void StampPsycheUse() => _psycheUsedAtMs = Environment.TickCount64;

    private bool CurrentTargetEukrasianDosisMissingOrEnding(float remainingSeconds)
    {
        if (CurrentTarget == null)
        {
            return false;
        }

        return
            (EukrasianDosisIiiPvE.EnoughLevel &&
             (!CurrentTarget.HasStatus(true, StatusID.EukrasianDosisIii) ||
              CurrentTarget.WillStatusEnd(remainingSeconds, true, StatusID.EukrasianDosisIii))) ||
            (!EukrasianDosisIiiPvE.EnoughLevel && EukrasianDosisIiPvE.EnoughLevel &&
             (!CurrentTarget.HasStatus(true, StatusID.EukrasianDosisIi) ||
              CurrentTarget.WillStatusEnd(remainingSeconds, true, StatusID.EukrasianDosisIi))) ||
            (!EukrasianDosisIiiPvE.EnoughLevel && !EukrasianDosisIiPvE.EnoughLevel && EukrasianDosisPvE.EnoughLevel &&
             (!CurrentTarget.HasStatus(true, StatusID.EukrasianDosis) ||
              CurrentTarget.WillStatusEnd(remainingSeconds, true, StatusID.EukrasianDosis)));
    }

    private bool ShouldEarlyRefreshEukrasianDosis()
    {
        if (!InCombat || CurrentTarget == null)
        {
            return false;
        }

        if (!CurrentTargetEukrasianDosisMissingOrEnding(EarlyDotRefreshSeconds))
        {
            return false;
        }

        return MovingTime > MovementTimeThreshold || (HasBuffs && InFirst20sAfterPsyche);
    }

    private bool PrepareEarlyEukrasianDosisRefresh(out IAction? act)
    {
        act = null;

        if (!ShouldEarlyRefreshEukrasianDosis())
        {
            return false;
        }

        _EukrasiaActionAim = null;

        if (EukrasianDosisIiiPvE.EnoughLevel && EukrasianDosisIiiPvE.IsEnabled)
        {
            SetEukrasia(EukrasianDosisIiiPvE);
            return DoEukrasianDosisIii(out act);
        }

        if (EukrasianDosisIiPvE.EnoughLevel && EukrasianDosisIiPvE.IsEnabled)
        {
            SetEukrasia(EukrasianDosisIiPvE);
            return DoEukrasianDosisIi(out act);
        }

        if (EukrasianDosisPvE.EnoughLevel && EukrasianDosisPvE.IsEnabled)
        {
            SetEukrasia(EukrasianDosisPvE);
            return DoEukrasianDosis(out act);
        }

        return false;
    }

    #endregion

    #region GCD Logic

    [RotationDesc(ActionID.PneumaPvE, ActionID.EukrasianPrognosisIiPvE)]
    protected override bool HealAreaGCD(out IAction? act)
    {
        UpdateActionTracking();

        act = null;

        if (HasHealingLockout || HasBuffs)
            return false;

        if (ShouldDeferToRaise())
        {
            return base.HealAreaGCD(out act);
        }

        if (PartyMembersAverHP < PneumaHeal || (DyskrasiaPvE.CanUse(out _) && PartyMembersAverHP < PneumaDyskrasiaHeal))
        {
            if (PneumaPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (PartyMembersAverHP < 0.9f &&
            HasSufficientMovement &&
            Addersting <= 1 &&
            HasEukrasia &&
            EukrasianPrognosisPvE.CanUse(out act))
        {
            return true;
        }

        return base.HealAreaGCD(out act);
    }

    [RotationDesc(ActionID.EukrasianDiagnosisPvE)]
    protected override bool HealSingleGCD(out IAction? act)
    {
        UpdateActionTracking();

        act = null;

        if (HasHealingLockout)
            return false;

        if (ShouldDeferToRaise())
        {
            return base.HealSingleGCD(out act);
        }

        if (HasSufficientMovement &&
            Addersting <= 1 &&
            !HasBuffs &&
            HasEukrasia &&
            EukrasianDiagnosisPvE.CanUse(out act))
        {
            return true;
        }

        return base.HealSingleGCD(out act);
    }

    [RotationDesc(ActionID.EgeiroPvE)]
    protected override bool RaiseGCD(out IAction? act)
    {
        UpdateActionTracking();

        act = null;

        if (EgeiroPvE.CanUse(out act))
        {
            return true;
        }

        return base.RaiseGCD(out act);
    }

    protected override bool GeneralGCD(out IAction? act)
    {
        UpdateActionTracking();

        act = null;

        if (ShouldDeferToRaise())
        {
            return base.GeneralGCD(out act);
        }

        if (HasZoe)
        {
            if (PartyMembersAverHP < ZoePneumaHeal)
            {
                if (PneumaPvE.CanUse(out act))
                {
                    return true;
                }
            }
            else if (!HasEukrasianPrognosis)
            {
                _EukrasiaActionAim = null;

                if (EukrasianPrognosisIiPvE.EnoughLevel && EukrasianPrognosisIiPvE.IsEnabled)
                {
                    SetEukrasia(EukrasianPrognosisIiPvE);

                    if (DoEukrasianPrognosisIi(out act))
                    {
                        return true;
                    }
                }
                else if (EukrasianPrognosisPvE.EnoughLevel && EukrasianPrognosisPvE.IsEnabled)
                {
                    SetEukrasia(EukrasianPrognosisPvE);

                    if (DoEukrasianPrognosis(out act))
                    {
                        return true;
                    }
                }
            }
        }

        if (DoEukrasianPrognosisIi(out act))
        {
            return true;
        }

        if (DoEukrasianPrognosis(out act))
        {
            return true;
        }

        if (DoEukrasianDiagnosis(out act))
        {
            return true;
        }

        if (PrepareEarlyEukrasianDosisRefresh(out act))
        {
            return true;
        }

        if (CombatTime > 9f
            && IsBurst
            && PhlegmaPvE.CanUse(out act, usedUp: HasBuffs
                || PhlegmaPvE.Cooldown.WillHaveXChargesGCD(2, 1)
                || (HasSufficientMovement && PhlegmaPvE.Cooldown.WillHaveXChargesGCD(2, 4))))
        {
            return true;
        }

        if (HasSufficientMovement && ToxikonPvE.CanUse(out act))
        {
            return true;
        }

        if (DoEukrasianDyskrasia(out act))
        {
            return true;
        }

        if (DyskrasiaPvE.CanUse(out act))
        {
            return true;
        }

        if (DoEukrasianDosisIii(out act))
        {
            return true;
        }

        if (DoEukrasianDosisIi(out act))
        {
            return true;
        }

        if (DoEukrasianDosis(out act))
        {
            return true;
        }

        if (DosisPvE.CanUse(out act))
        {
            return true;
        }

        if (OOCEukrasia && !InCombat && !HasEukrasia && EukrasiaPvE.CanUse(out act))
        {
            return true;
        }

        if (InCombat && !HasHostilesInRange && EukrasiaPvE.CanUse(out act))
        {
            return true;
        }

        if (AntiBrick && InCombat && HasHostilesInRange && HasEukrasia)
        {
            if (EukrasianPrognosisPvE.CanUse(out act, skipStatusProvideCheck: true))
            {
                return true;
            }
        }

        return base.GeneralGCD(out act);
    }

    #endregion
}