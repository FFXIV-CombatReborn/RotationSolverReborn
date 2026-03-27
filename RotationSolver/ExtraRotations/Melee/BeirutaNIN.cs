using System;
using System.ComponentModel;

namespace RotationSolver.ExtraRotations.Melee;

[Rotation("BeirutaNIN", CombatType.PvE, GameVersion = "7.45")]
[SourceCode(Path = "main/ExtraRotations/Melee/BeirutaNIN.cs")]
public sealed class BeirutaNIN : NinjaRotation
{
    #region Config

    [RotationConfig(CombatType.PvE, Name =
        "Please note that this rotation is optimised for all fights without consideration of death.\n" +
        "• Standard 4th GCD is the highest adps damage opner, other ones are scarifying own damage for rdps\n" +
        "• Please start fights from flank, or true north will mess your weaving\n" +
        "• Use rotation Settings AutoBurst False AT LEAST 5 seconds BEFORE Kassatsu ready to delay burst\n" +
        "• Use rotation Settings AutoBurst True to turn burst back or just use toggle marcro\n" +
        "• Intercept Forked Raiju if you want to use it\n" +
        "• Use ac Shukuchi gtoff macro to move like RPR\n")]
    public bool RotationNotes { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Mudras outside of combat when enemies are near")]
    public bool CombatMudra { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Raiton/Katon for uptime while disengaged")]
    public bool UseRaitonDisengageFallback { get; set; } = true;

    [Range(0, 20, ConfigUnitType.Yalms, 1)]
    [RotationConfig(CombatType.PvE, Name = "Minimum target distance for disengage fallback")]
    public float RaitonFallbackMinDistance { get; set; } = 3.0f;

    [RotationConfig(CombatType.PvE, Name = "Which Opener to Use")]
    [Range(0, 2, ConfigUnitType.None, 1)]
    public BurstTimingOption BurstTiming { get; set; } = BurstTimingOption.StandardFourthGcd;

    public enum BurstTimingOption : byte
    {
        [Description("Standard 4th GCD")] StandardFourthGcd,
        [Description("Standard 3rd GCD")] StandardThirdGcd,
        [Description("Alignment 4th GCD")] AlignmentFourthGcd,
    }

    private int RaidBuffOpenTiming => BurstTiming switch
    {
        BurstTimingOption.StandardFourthGcd => 4,
        BurstTimingOption.StandardThirdGcd => 4,
        BurstTimingOption.AlignmentFourthGcd => 6,
        _ => 4,
    };

    private int BurstBuffOpenTiming => BurstTiming switch
    {
        BurstTimingOption.StandardFourthGcd => 8,
        BurstTimingOption.StandardThirdGcd => 5,
        BurstTimingOption.AlignmentFourthGcd => 8,
        _ => 9,
    };

    [RotationConfig(CombatType.PvE, Name = "Enable Potion Usage")]
    private static bool PotionUsageEnabled { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Potion Usage Preset", Parent = nameof(PotionUsageEnabled))]
    private static NINPotionPreset PotionUsagePreset { get; set; } = NINPotionPreset.Standard0611;

    #endregion

    #region State Tracking

    private enum NINPotionPreset
    {
        [Description("0-6-11 (Dokumori)")] Standard0611,
        [Description("0-5-10 (Kunai's Bane)")] Standard0510,
    }

    private const long BurstPhaseWindowMs = 14_000;

    private IBaseAction? _lastNinActionAim;
    private IBaseAction? _ninActionAim;

    private long _kunaisBaneUsedAtMs = 0;
    private long _trickAttackUsedAtMs = 0;
    private bool _kunaisBaneSeen = false;
    private bool _trickAttackSeen = false;

    private bool ShouldUseRaitonDisengageFallback =>
        UseRaitonDisengageFallback &&
        CurrentTarget != null &&
        CurrentTarget.DistanceToPlayer() > RaitonFallbackMinDistance;

    private readonly ActionID NinjutsuPvEid = AdjustId(ActionID.NinjutsuPvE);

    private static bool NoActiveNinjutsu => AdjustId(ActionID.NinjutsuPvE) == ActionID.NinjutsuPvE;
    private static bool RabbitMediumCurrent => AdjustId(ActionID.NinjutsuPvE) == ActionID.RabbitMediumPvE;
    private static bool FumaShurikenCurrent => AdjustId(ActionID.NinjutsuPvE) == ActionID.FumaShurikenPvE;
    private static bool KatonCurrent => AdjustId(ActionID.NinjutsuPvE) == ActionID.KatonPvE;
    private static bool RaitonCurrent => AdjustId(ActionID.NinjutsuPvE) == ActionID.RaitonPvE;
    private static bool HyotonCurrent => AdjustId(ActionID.NinjutsuPvE) == ActionID.HyotonPvE;
    private static bool HutonCurrent => AdjustId(ActionID.NinjutsuPvE) == ActionID.HutonPvE;
    private static bool DotonCurrent => AdjustId(ActionID.NinjutsuPvE) == ActionID.DotonPvE;
    private static bool SuitonCurrent => AdjustId(ActionID.NinjutsuPvE) == ActionID.SuitonPvE;
    private static bool GokaMekkyakuCurrent => AdjustId(ActionID.NinjutsuPvE) == ActionID.GokaMekkyakuPvE;
    private static bool HyoshoRanryuCurrent => AdjustId(ActionID.NinjutsuPvE) == ActionID.HyoshoRanryuPvE;

    private bool InBurstWindowAfterKunaisBane =>
        InCombat &&
        _kunaisBaneUsedAtMs != 0 &&
        Environment.TickCount64 - _kunaisBaneUsedAtMs <= BurstPhaseWindowMs;

    private bool InBurstWindowAfterTrickAttack =>
        InCombat &&
        _trickAttackUsedAtMs != 0 &&
        Environment.TickCount64 - _trickAttackUsedAtMs <= BurstPhaseWindowMs;

    private bool InBurstPhase =>
        InBurstWindowAfterKunaisBane || InBurstWindowAfterTrickAttack;

    private void UpdateBurstPhaseTracking()
    {
        if (!InCombat)
        {
            _kunaisBaneUsedAtMs = 0;
            _trickAttackUsedAtMs = 0;
            _kunaisBaneSeen = false;
            _trickAttackSeen = false;
            return;
        }

        long nowMs = Environment.TickCount64;

        bool lastKunais = IsLastAction(ActionID.KunaisBanePvE);
        bool lastTrick = IsLastAction(ActionID.TrickAttackPvE);

        if (lastKunais && !_kunaisBaneSeen)
        {
            _kunaisBaneUsedAtMs = nowMs;
        }

        if (lastTrick && !_trickAttackSeen)
        {
            _trickAttackUsedAtMs = nowMs;
        }

        _kunaisBaneSeen = lastKunais;
        _trickAttackSeen = lastTrick;

        if (_kunaisBaneUsedAtMs != 0 &&
            nowMs - _kunaisBaneUsedAtMs > BurstPhaseWindowMs)
        {
            _kunaisBaneUsedAtMs = 0;
        }

        if (_trickAttackUsedAtMs != 0 &&
            nowMs - _trickAttackUsedAtMs > BurstPhaseWindowMs)
        {
            _trickAttackUsedAtMs = 0;
        }
    }

    private bool KeepKassatsuinBurst =>
        !StatusHelper.PlayerWillStatusEndGCD(2, 0, true, StatusID.Kassatsu) &&
        HasKassatsu &&
        !InBurstPhase &&
        !IsExecutingMudra;

    private bool BurstPrepWindow =>
        IsBurst &&
        (KassatsuPvE.Cooldown.CurrentCharges > 0 ||
         KassatsuPvE.Cooldown.RecastTimeRemain <= 5f);

    private bool ShouldPrepBurstSuitonOrHuton =>
        BurstPrepWindow &&
        !IsShadowWalking &&
        !HasTenChiJin &&
        !HasKassatsu;

    public override void DisplayRotationStatus()
    {
        UpdateBurstPhaseTracking();

        ImGui.Text($"Last Ninjutsu Action Cleared From Queue: {_lastNinActionAim}");
        ImGui.Text($"Current Ninjutsu Action: {_ninActionAim}");
        ImGui.Text($"Ninjutsu ID: {AdjustId(NinjutsuPvEid)}");
        ImGui.Text($"Burst Prep Window: {BurstPrepWindow}");
        ImGui.Text($"Should Prep Suiton/Huton: {ShouldPrepBurstSuitonOrHuton}");
        ImGui.Text($"In Burst Phase: {InBurstPhase}");
        ImGui.Text($"Kassatsu Charges: {KassatsuPvE.Cooldown.CurrentCharges}");
        ImGui.Text($"Kassatsu Recast Remain: {KassatsuPvE.Cooldown.RecastTimeRemain}");
        ImGui.Text($"Ten Charges: {TenPvE.Cooldown.CurrentCharges}");
        ImGui.Text($"Ten Recast Remain: {TenPvE.Cooldown.RecastTimeRemain}");
    }

    #endregion

    #region Shared Helpers

    private bool IsQueuedNinjutsu(IBaseAction action) => _ninActionAim == action;

    private bool ShouldBlockStandardNinjutsu() => KeepKassatsuinBurst;

    private bool ShouldClearQueuedNinjutsu()
    {
        return
            IsLastAction(false, FumaShurikenPvE, KatonPvE, RaitonPvE, HyotonPvE, DotonPvE, SuitonPvE) ||
            (IsShadowWalking && (_ninActionAim == SuitonPvE || _ninActionAim == HutonPvE)) ||
            (_ninActionAim == GokaMekkyakuPvE && IsLastGCD(false, GokaMekkyakuPvE)) ||
            (_ninActionAim == HyoshoRanryuPvE && IsLastGCD(false, HyoshoRanryuPvE)) ||
            (_ninActionAim == GokaMekkyakuPvE && !HasKassatsu) ||
            (_ninActionAim == HyoshoRanryuPvE && !HasKassatsu);
    }

    private void RefreshNinjutsuChoice()
    {
          if ((InCombat && HasHostilesInMaxRange) || (CombatMudra && HasHostilesInMaxRange && TenPvE.Cooldown.CurrentCharges == TenPvE.Cooldown.MaxCharges))
        {
            _ = ChoiceNinjutsu(out _);
        }

        if (!InCombat && !CombatMudra)
        {
            ClearNinjutsu();
        }
    }

    private bool TryUseRaidBuff(out IAction? act)
    {
        act = null;

        if (!IsBurst || CombatElapsedLess(RaidBuffOpenTiming))
        {
            return false;
        }

        if (!DokumoriPvE.EnoughLevel)
        {
            return MugPvE.CanUse(out act);
        }

        return DokumoriPvE.CanUse(out act);
    }

    private static bool EnoughWeaveTime =>
        WeaponRemain > DataCenter.CalculatedActionAhead && WeaponRemain < WeaponTotal;

    private static float LateWeaveWindow => WeaponTotal * 0.4f;

    private static bool CanLateWeave => WeaponRemain <= LateWeaveWindow && EnoughWeaveTime;

    private bool TryUseBurstBuff(out IAction? act)
    {
        act = null;

        if (CombatElapsedLess(BurstBuffOpenTiming) || !CanLateWeave)
        {
            return false;
        }

        if (!KunaisBanePvE.EnoughLevel)
        {
            if (TrickAttackPvE.CanUse(out act, skipStatusProvideCheck: IsShadowWalking))
            {
                return true;
            }
        }
        else
        {
            if (KunaisBanePvE.CanUse(out act, skipAoeCheck: true, skipStatusProvideCheck: IsShadowWalking))
            {
                return true;
            }
        }

        if (TrickAttackPvE.Cooldown.IsCoolingDown &&
            !TrickAttackPvE.Cooldown.WillHaveOneCharge(19) &&
            TenChiJinPvE.Cooldown.IsCoolingDown &&
            TrickAttackPvE.Cooldown.IsCoolingDown &&
            MeisuiPvE.CanUse(out act))
        {
            return true;
        }

        return false;
    }

    private bool ShouldUsePotionNow()
    {
        if (!PotionUsageEnabled || !InCombat || !IsShadowWalking)
        {
            return false;
        }

        return PotionUsagePreset switch
        {
            NINPotionPreset.Standard0611 => ShouldUsePotionForDokumoriBurst(),
            NINPotionPreset.Standard0510 => ShouldUsePotionForKunaisBaneBurst(),
            _ => false,
        };
    }

    private bool ShouldUsePotionForDokumoriBurst()
    {
        if (DokumoriPvE.EnoughLevel)
        {
            return !DokumoriPvE.Cooldown.IsCoolingDown ||
                   DokumoriPvE.Cooldown.WillHaveOneCharge(5);
        }

        return !MugPvE.Cooldown.IsCoolingDown ||
               MugPvE.Cooldown.WillHaveOneCharge(5);
    }

    private bool ShouldUsePotionForKunaisBaneBurst()
    {
        if (KunaisBanePvE.EnoughLevel)
        {
            return !KunaisBanePvE.Cooldown.IsCoolingDown ||
                   KunaisBanePvE.Cooldown.WillHaveOneCharge(5);
        }

        return !MugPvE.Cooldown.IsCoolingDown ||
               MugPvE.Cooldown.WillHaveOneCharge(5);
    }

    #endregion

    #region Countdown

    protected override IAction? CountDownAction(float remainTime)
    {
        _ = IsLastAction(false, HutonPvE);

        if (remainTime > 6)
        {
            ClearNinjutsu();
        }

        if (DoSuiton(out IAction? act))
        {
            return act == SuitonPvE && remainTime > CountDownAhead ? null : act;
        }

        if (remainTime < 5)
        {
            SetNinjutsu(SuitonPvE);
        }
        else if (remainTime < 6)
        {
            if (_ninActionAim == null &&
                TenPvE.Cooldown.IsCoolingDown &&
                HidePvE.CanUse(out act))
            {
                return act;
            }
        }

        return base.CountDownAction(remainTime);
    }

    #endregion

    #region Movement

    [RotationDesc(ActionID.ForkedRaijuPvE)]
    protected override bool MoveForwardGCD(out IAction? act)
    {
        UpdateBurstPhaseTracking();

        if (ForkedRaijuPvE.CanUse(out act))
        {
            return true;
        }

        return base.MoveForwardGCD(out act);
    }

    #endregion

    #region oGCD

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        UpdateBurstPhaseTracking();

        if (ShouldClearQueuedNinjutsu())
        {
            ClearNinjutsu();
        }

        RefreshNinjutsuChoice();

        if (!InCombat)
        {
            ClearNinjutsu();
        }

        if (RabbitMediumPvE.CanUse(out act))
        {
            return true;
        }

        if (!NoNinjutsu || !InCombat)
        {
            return base.EmergencyAbility(nextGCD, out act);
        }

        if (NoNinjutsu &&
            !nextGCD.IsTheSameTo(false, ActionID.TenPvE, ActionID.ChiPvE, ActionID.JinPvE) &&
            IsShadowWalking &&
            KassatsuPvE.CanUse(out act))
        {
            return true;
        }

        if ((!TenChiJinPvE.Cooldown.IsCoolingDown ||
             StatusHelper.PlayerWillStatusEndGCD(2, 0, true, StatusID.ShadowWalker)) &&
            TrickAttackPvE.Cooldown.IsCoolingDown &&
            MeisuiPvE.CanUse(out act))
        {
            return true;
        }

        if (TenriJindoPvE.CanUse(out act))
        {
            return true;
        }

        if (CanLateWeave && ShouldUsePotionNow() && UseBurstMedicine(out act))
        {
            return true;
        }

        if (TryUseRaidBuff(out act))
        {
            return true;
        }

        if (TryUseBurstBuff(out act))
        {
            return true;
        }

        if (IsBurst && !CombatElapsedLess(RaidBuffOpenTiming) && (InBurstPhase || HasBuffs))
        {
            if (!DokumoriPvE.EnoughLevel)
            {
                return MugPvE.CanUse(out act);
            }

            return DokumoriPvE.CanUse(out act);
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        UpdateBurstPhaseTracking();

        if (!NoNinjutsu || !InCombat)
        {
            return base.AttackAbility(nextGCD, out act);
        }

        if (InBurstPhase &&
            !StatusHelper.PlayerHasStatus(true, StatusID.ShadowWalker) &&
            !TenPvE.Cooldown.ElapsedAfter(30) &&
            TenChiJinPvE.CanUse(out act))
        {
            return true;
        }

        if (CanLateWeave && !CombatElapsedLess(RaidBuffOpenTiming) && BunshinPvE.CanUse(out act))
        {
            return true;
        }

        if (TryUseRaidBuff(out act))
        {
            return true;
        }

        if (InBurstPhase)
        {
            if (DreamWithinADreamPvE.CanUse(out act))
            {
                return true;
            }

            if (!DreamWithinADreamPvE.Info.EnoughLevelAndQuest() &&
                AssassinatePvE.CanUse(out act))
            {
                return true;
            }
        }

          if ((!InMug || InTrickAttack)
            && (!BunshinPvE.Cooldown.WillHaveOneCharge(10) || HasPhantomKamaitachi || MugPvE.Cooldown.WillHaveOneCharge(2)))
        {
            if (HellfrogMediumPvE.CanUse(out act, skipAoeCheck: !BhavacakraPvE.EnoughLevel))
            {
                return true;
            }

            if (BhavacakraPvE.CanUse(out act))
            {
                return true;
            }

            if (TenriJindoPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (Ninki >= 90)
        {
            if (HellfrogMediumPvE.CanUse(out act, skipAoeCheck: !BhavacakraPvE.EnoughLevel))
            {
                return true;
            }

            if (BhavacakraPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (MergedStatus.HasFlag(AutoStatus.MoveForward) && MoveForwardAbility(nextGCD, out act))
        {
            return true;
        }

        return base.AttackAbility(nextGCD, out act);
    }

    #endregion

    #region Ninjutsu Queue Management

    private void SetNinjutsu(IBaseAction act)
    {
        if (act == null || AdjustId(ActionID.NinjutsuPvE) == ActionID.RabbitMediumPvE)
        {
            return;
        }

        if (_ninActionAim != null &&
            IsLastAction(false, TenPvE, JinPvE, ChiPvE, FumaShurikenPvE_18873, FumaShurikenPvE_18874, FumaShurikenPvE_18875))
        {
            return;
        }

        if (_ninActionAim != act)
        {
            _ninActionAim = act;
        }
    }

    private void ClearNinjutsu()
    {
        if (_ninActionAim != null)
        {
            _lastNinActionAim = _ninActionAim;
            _ninActionAim = null;
        }
    }

    private bool ChoiceNinjutsu(out IAction? act)
    {
        UpdateBurstPhaseTracking();

        act = null;

        if (HasKassatsu)
        {
            if (GokaMekkyakuPvE.Target.Target != null &&
                GokaMekkyakuPvE.Target.AffectedTargets.Length > 1 &&
                GokaMekkyakuPvE.EnoughLevel &&
                !IsLastAction(false, GokaMekkyakuPvE) &&
                GokaMekkyakuPvE.IsEnabled &&
                ChiPvE.Info.IsQuestUnlocked())
            {
                SetNinjutsu(GokaMekkyakuPvE);
            }

            if (!DeathBlossomPvE.CanUse(out _) &&
                !HakkeMujinsatsuPvE.CanUse(out _) &&
                HyoshoRanryuPvE.EnoughLevel &&
                !IsLastAction(false, HyoshoRanryuPvE) &&
                HyoshoRanryuPvE.IsEnabled &&
                JinPvE.Info.IsQuestUnlocked())
            {
                SetNinjutsu(HyoshoRanryuPvE);
            }

            if (!IsShadowWalking &&
                BurstPrepWindow &&
                !HyoshoRanryuPvE.EnoughLevel &&
                HutonPvE.EnoughLevel &&
                HutonPvE.IsEnabled &&
                JinPvE.Info.IsQuestUnlocked())
            {
                SetNinjutsu(HutonPvE);
            }

            if ((DeathBlossomPvE.CanUse(out _) || HakkeMujinsatsuPvE.CanUse(out _)) &&
                !HyoshoRanryuPvE.EnoughLevel &&
                KatonPvE.EnoughLevel &&
                KatonPvE.IsEnabled &&
                ChiPvE.Info.IsQuestUnlocked())
            {
                SetNinjutsu(KatonPvE);
            }

            if (!DeathBlossomPvE.CanUse(out _) &&
                !HakkeMujinsatsuPvE.CanUse(out _) &&
                !HyoshoRanryuPvE.EnoughLevel &&
                RaitonPvE.EnoughLevel &&
                RaitonPvE.IsEnabled &&
                ChiPvE.Info.IsQuestUnlocked())
            {
                SetNinjutsu(RaitonPvE);
            }
        }
        else if (_ninActionAim == null)
        {
            if (ShouldPrepBurstSuitonOrHuton &&
                TenPvE.Cooldown.CurrentCharges > 0)
            {
                if (NumberOfHostilesInRangeOf(3) >= 3 &&
                    JinPvE.Info.IsQuestUnlocked() &&
                    HutonPvE.EnoughLevel &&
                    HutonPvE.IsEnabled)
                {
                    SetNinjutsu(HutonPvE);
                    return false;
                }

                if (SuitonPvE.EnoughLevel &&
                    JinPvE.Info.IsQuestUnlocked() &&
                    SuitonPvE.IsEnabled &&
                    ((TrickAttackPvE.IsEnabled && !KunaisBanePvE.EnoughLevel) ||
                     (KunaisBanePvE.IsEnabled && KunaisBanePvE.EnoughLevel)))
                {
                    SetNinjutsu(SuitonPvE);
                    return false;
                }
            }
            else if (InBurstPhase)
            {
                if (NumberOfHostilesInRangeOf(3) >= 3)
                {
                    if ((!HasDoton &&
                         !IsMoving &&
                         !IsLastGCD(true, DotonPvE) &&
                         !TenChiJinPvE.Cooldown.WillHaveOneCharge(6) &&
                         DotonPvE.EnoughLevel) ||
                        (!HasDoton &&
                         !IsLastGCD(true, DotonPvE) &&
                         !TenChiJinPvE.Cooldown.IsCoolingDown &&
                         DotonPvE.EnoughLevel))
                    {
                        if (JinPvE.CanUse(out _) &&
                            DotonPvE.IsEnabled &&
                            JinPvE.Info.IsQuestUnlocked())
                        {
                            SetNinjutsu(DotonPvE);
                        }
                    }
                    else if (KatonPvE.EnoughLevel &&
                             KatonPvE.IsEnabled &&
                             ChiPvE.Info.IsQuestUnlocked())
                    {
                        SetNinjutsu(KatonPvE);
                    }
                }

                if (NumberOfHostilesInRangeOf(3) < 3 &&
                    (!BurstPrepWindow || !IsBurst))
                {
                    if (RaitonPvE.EnoughLevel &&
                        RaitonPvE.IsEnabled &&
                        ChiPvE.Info.IsQuestUnlocked() &&
                        (!StatusHelper.PlayerHasStatus(true, StatusID.RaijuReady) ||
                         (StatusHelper.PlayerHasStatus(true, StatusID.RaijuReady) &&
                          StatusHelper.PlayerStatusStack(true, StatusID.RaijuReady) < 3)))
                    {
                        SetNinjutsu(RaitonPvE);
                    }

                    if (FumaShurikenPvE.EnoughLevel &&
                        FumaShurikenPvE.IsEnabled &&
                        TenPvE.Info.IsQuestUnlocked() &&
                        (!RaitonPvE.EnoughLevel ||
                         (StatusHelper.PlayerHasStatus(true, StatusID.RaijuReady) &&
                          StatusHelper.PlayerStatusStack(true, StatusID.RaijuReady) == 3)))
                    {
                        SetNinjutsu(FumaShurikenPvE);
                    }
                }
            }
            else if (_ninActionAim == null &&
                     TenPvE.CanUse(out _, usedUp: true) &&
                     TenPvE.Cooldown.WillHaveXChargesGCD(2, 2))
            {
                if (NumberOfHostilesInRangeOf(3) >= 3)
                {
                    if ((!HasDoton &&
                         !IsMoving &&
                         !IsLastGCD(true, DotonPvE) &&
                         !TenChiJinPvE.Cooldown.WillHaveOneCharge(6) &&
                         DotonPvE.EnoughLevel) ||
                        (!HasDoton &&
                         !IsLastGCD(true, DotonPvE) &&
                         !TenChiJinPvE.Cooldown.IsCoolingDown &&
                         DotonPvE.EnoughLevel))
                    {
                        if (JinPvE.CanUse(out _) &&
                            DotonPvE.IsEnabled &&
                            JinPvE.Info.IsQuestUnlocked())
                        {
                            SetNinjutsu(DotonPvE);
                        }
                    }
                    else if (KatonPvE.EnoughLevel &&
                             KatonPvE.IsEnabled &&
                             ChiPvE.Info.IsQuestUnlocked())
                    {
                        SetNinjutsu(KatonPvE);
                    }
                }

                if (NumberOfHostilesInRangeOf(3) < 3 &&
                    (!BurstPrepWindow || !IsBurst))
                {
                    if (RaitonPvE.EnoughLevel &&
                        RaitonPvE.IsEnabled &&
                        ChiPvE.Info.IsQuestUnlocked() &&
                        (!StatusHelper.PlayerHasStatus(true, StatusID.RaijuReady) ||
                         (StatusHelper.PlayerHasStatus(true, StatusID.RaijuReady) &&
                          StatusHelper.PlayerStatusStack(true, StatusID.RaijuReady) < 3)))
                    {
                        SetNinjutsu(RaitonPvE);
                    }

                    if (FumaShurikenPvE.EnoughLevel &&
                        FumaShurikenPvE.IsEnabled &&
                        TenPvE.Info.IsQuestUnlocked() &&
                        (!RaitonPvE.EnoughLevel ||
                         (StatusHelper.PlayerHasStatus(true, StatusID.RaijuReady) &&
                          StatusHelper.PlayerStatusStack(true, StatusID.RaijuReady) == 3)))
                    {
                        SetNinjutsu(FumaShurikenPvE);
                    }
                }
            }
        }

        return false;
    }

    #endregion

    #region Ninjutsu Execution

    private bool DoRabbitMedium(out IAction? act)
    {
        act = null;
        uint ninjutsuId = AdjustId(NinjutsuPvE.ID);

        if (ninjutsuId != RabbitMediumPvE.ID)
        {
            return false;
        }

        if (RabbitMediumPvE.CanUse(out act))
        {
            return true;
        }

        ClearNinjutsu();
        return false;
    }

    private bool DoTenChiJin(out IAction? act)
    {
        act = null;

        if (!HasTenChiJin)
        {
            return false;
        }

        uint tenId = AdjustId(TenPvE.ID);
        uint chiId = AdjustId(ChiPvE.ID);
        uint jinId = AdjustId(JinPvE.ID);

        if (tenId == FumaShurikenPvE_18873.ID &&
            !IsLastAction(false, FumaShurikenPvE_18875, FumaShurikenPvE_18873))
        {
            if (DeathBlossomPvE.CanUse(out _))
            {
                if (FumaShurikenPvE_18875.CanUse(out act))
                {
                    return true;
                }
            }

            if (FumaShurikenPvE_18873.CanUse(out act))
            {
                return true;
            }
        }
        else if (tenId == KatonPvE_18876.ID && !IsLastAction(false, KatonPvE_18876))
        {
            if (KatonPvE_18876.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }
        }
        else if (chiId == RaitonPvE_18877.ID && !IsLastAction(false, RaitonPvE_18877))
        {
            if (RaitonPvE_18877.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }
        }
        else if (jinId == SuitonPvE_18881.ID && !IsLastAction(false, SuitonPvE_18881))
        {
            if (SuitonPvE_18881.CanUse(out act, skipAoeCheck: true, skipStatusProvideCheck: true))
            {
                return true;
            }
        }
        else if (chiId == DotonPvE_18880.ID && !IsLastAction(false, DotonPvE_18880) && !HasDoton)
        {
            if (DotonPvE_18880.CanUse(out act, skipAoeCheck: true, skipStatusProvideCheck: true))
            {
                return true;
            }
        }

        return false;
    }

    private bool DoHyoshoRanryu(out IAction? act)
    {
        act = null;

        if ((!TrickAttackPvE.Cooldown.IsCoolingDown ||
             TrickAttackPvE.Cooldown.WillHaveOneCharge(StatusHelper.PlayerStatusTime(true, StatusID.Kassatsu))) &&
            !IsExecutingMudra)
        {
            return false;
        }

        if (!IsQueuedNinjutsu(HyoshoRanryuPvE))
        {
            return false;
        }

        if (RabbitMediumCurrent)
        {
            ClearNinjutsu();
            return false;
        }

        if (HyoshoRanryuCurrent)
        {
            return HyoshoRanryuPvE.CanUse(out act, skipAoeCheck: true);
        }

        if (FumaShurikenCurrent)
        {
            return JinPvE_18807.CanUse(out act, usedUp: true);
        }

        if (NoActiveNinjutsu)
        {
            return ChiPvE_18806.CanUse(out act, usedUp: true);
        }

        return false;
    }

    private bool DoGokaMekkyaku(out IAction? act)
    {
        act = null;

        if ((!TrickAttackPvE.Cooldown.IsCoolingDown ||
             TrickAttackPvE.Cooldown.WillHaveOneCharge(StatusHelper.PlayerStatusTime(true, StatusID.Kassatsu))) &&
            !IsExecutingMudra)
        {
            return false;
        }

        if (!IsQueuedNinjutsu(GokaMekkyakuPvE))
        {
            return false;
        }

        if (RabbitMediumCurrent)
        {
            ClearNinjutsu();
            return false;
        }

        if (GokaMekkyakuCurrent)
        {
            return GokaMekkyakuPvE.CanUse(out act, skipAoeCheck: true);
        }

        if (FumaShurikenCurrent)
        {
            return TenPvE_18805.CanUse(out act, usedUp: true);
        }

        if (NoActiveNinjutsu)
        {
            return ChiPvE_18806.CanUse(out act, usedUp: true);
        }

        return false;
    }

    private bool DoSuiton(out IAction? act)
    {
        act = null;

        if (ShouldBlockStandardNinjutsu() || !IsQueuedNinjutsu(SuitonPvE))
        {
            return false;
        }

        if (RabbitMediumCurrent)
        {
            ClearNinjutsu();
            return false;
        }

        if (SuitonCurrent)
        {
            return SuitonPvE.CanUse(out act);
        }

        if (RaitonCurrent)
        {
            return JinPvE_18807.CanUse(out act, usedUp: true);
        }

        if (FumaShurikenCurrent)
        {
            return ChiPvE_18806.CanUse(out act, usedUp: true);
        }

        if (NoActiveNinjutsu)
        {
            return TenPvE.CanUse(out act, usedUp: true);
        }

        return false;
    }

    private bool DoDoton(out IAction? act)
    {
        act = null;

        if (ShouldBlockStandardNinjutsu() || !IsQueuedNinjutsu(DotonPvE))
        {
            return false;
        }

        if (RabbitMediumCurrent)
        {
            ClearNinjutsu();
            return false;
        }

        if (DotonCurrent)
        {
            return DotonPvE.CanUse(out act, skipAoeCheck: true);
        }

        if (HyotonCurrent)
        {
            return ChiPvE_18806.CanUse(out act, usedUp: true);
        }

        if (FumaShurikenCurrent)
        {
            return JinPvE_18807.CanUse(out act, usedUp: true);
        }

        if (NoActiveNinjutsu)
        {
            return TenPvE.CanUse(out act, usedUp: true);
        }

        return false;
    }

    private bool DoHuton(out IAction? act)
    {
        act = null;

        if (ShouldBlockStandardNinjutsu() || !IsQueuedNinjutsu(HutonPvE))
        {
            return false;
        }

        if (RabbitMediumCurrent)
        {
            ClearNinjutsu();
            return false;
        }

        if (HutonCurrent)
        {
            return HutonPvE.CanUse(out act, skipAoeCheck: true);
        }

        if (HyotonCurrent)
        {
            return TenPvE_18805.CanUse(out act, usedUp: true);
        }

        if (FumaShurikenCurrent)
        {
            return JinPvE_18807.CanUse(out act, usedUp: true);
        }

        if (NoActiveNinjutsu)
        {
            return ChiPvE.CanUse(out act, usedUp: true);
        }

        return false;
    }

    private bool DoHyoton(out IAction? act)
    {
        act = null;

        if (ShouldBlockStandardNinjutsu() || !IsQueuedNinjutsu(HyotonPvE))
        {
            return false;
        }

        if (RabbitMediumCurrent)
        {
            ClearNinjutsu();
            return false;
        }

        if (HyotonCurrent)
        {
            return HyotonPvE.CanUse(out act);
        }

        if (FumaShurikenCurrent)
        {
            return JinPvE_18807.CanUse(out act, usedUp: true);
        }

        if (NoActiveNinjutsu)
        {
            return ChiPvE.CanUse(out act, usedUp: true);
        }

        return false;
    }

    private bool DoRaiton(out IAction? act)
    {
        act = null;

        if (ShouldBlockStandardNinjutsu() || !IsQueuedNinjutsu(RaitonPvE))
        {
            return false;
        }

        if (RabbitMediumCurrent)
        {
            ClearNinjutsu();
            return false;
        }

        if (RaitonCurrent)
        {
            return RaitonPvE.CanUse(out act);
        }

        if (FumaShurikenCurrent)
        {
            return ChiPvE_18806.CanUse(out act, usedUp: true);
        }

        if (NoActiveNinjutsu)
        {
            return TenPvE.CanUse(out act, usedUp: true);
        }

        return false;
    }

    private bool DoKaton(out IAction? act)
    {
        act = null;

        if (ShouldBlockStandardNinjutsu() || !IsQueuedNinjutsu(KatonPvE))
        {
            return false;
        }

        if (RabbitMediumCurrent)
        {
            ClearNinjutsu();
            return false;
        }

        if (KatonCurrent)
        {
            return KatonPvE.CanUse(out act, skipAoeCheck: true);
        }

        if (FumaShurikenCurrent)
        {
            return TenPvE_18805.CanUse(out act, usedUp: true);
        }

        if (NoActiveNinjutsu)
        {
            return ChiPvE.CanUse(out act, usedUp: true);
        }

        return false;
    }

    private bool DoFumaShuriken(out IAction? act)
    {
        act = null;

        if (ShouldBlockStandardNinjutsu() || !IsQueuedNinjutsu(FumaShurikenPvE))
        {
            return false;
        }

        if (RabbitMediumCurrent)
        {
            ClearNinjutsu();
            return false;
        }

        if (FumaShurikenCurrent)
        {
            return FumaShurikenPvE.CanUse(out act);
        }

        if (NoActiveNinjutsu)
        {
            return TenPvE.CanUse(out act, usedUp: true);
        }

        return false;
    }

    #endregion

    #region GCD

    protected override bool GeneralGCD(out IAction? act)
    {
        UpdateBurstPhaseTracking();

        if (!IsExecutingMudra && (InTrickAttack || InMug) && NoNinjutsu && !HasRaijuReady
            && Player != null && !StatusHelper.PlayerHasStatus(true, StatusID.TenChiJin)
            && PhantomKamaitachiPvE.CanUse(out act))
        {
            return true;
        }

        if (!IsExecutingMudra && FleetingRaijuPvE.CanUse(out act))
        {
            return true;
        }

        if (DoTenChiJin(out act))
        {
            return true;
        }

        if (DoRabbitMedium(out act))
        {
            return true;
        }

        if (_ninActionAim != null && GCDTime() == 0f)
        {
            if (DoGokaMekkyaku(out act)) return true;
            if (DoHuton(out act)) return true;
            if (DoDoton(out act)) return true;
            if (DoKaton(out act)) return true;
            if (DoHyoshoRanryu(out act)) return true;
            if (DoSuiton(out act)) return true;
            if (DoHyoton(out act)) return true;
            if (DoRaiton(out act)) return true;
            if (DoFumaShuriken(out act)) return true;
        }

        if (IsExecutingMudra)
        {
            return base.GeneralGCD(out act);
        }

        var shouldPrepNinjutsu =
            JinPvE.CanUse(out _) &&
            JinPvE.Info.IsQuestUnlocked() &&
            !IsExecutingMudra &&
            NoNinjutsu &&
            IsBurst &&
            _ninActionAim == null &&
            !IsShadowWalking &&
            !HasKassatsu &&
            !HasTenChiJin &&
            (ShouldPrepBurstSuitonOrHuton ||
             (KunaisBanePvE.Cooldown.IsCoolingDown &&
              KunaisBanePvE.Cooldown.RecastTimeRemain < 22f));

        if (NumberOfHostilesInRangeOf(3) >= 3)
        {
            if (HutonPvE.EnoughLevel &&
                HutonPvE.IsEnabled &&
                shouldPrepNinjutsu)
            {
                SetNinjutsu(HutonPvE);
                return false;
            }
        }
        else
        {
            if (SuitonPvE.EnoughLevel &&
                SuitonPvE.IsEnabled &&
                shouldPrepNinjutsu &&
                ((TrickAttackPvE.IsEnabled && !KunaisBanePvE.EnoughLevel) ||
                 (KunaisBanePvE.IsEnabled && KunaisBanePvE.EnoughLevel)))
            {
                SetNinjutsu(SuitonPvE);
                return false;
            }
        }

        if (HakkeMujinsatsuPvE.CanUse(out act))
        {
            return true;
        }

        if (DeathBlossomPvE.CanUse(out act))
        {
            return true;
        }

        if (AeolianEdgePvE.EnoughLevel)
        {
            if (!ArmorCrushPvE.EnoughLevel)
            {
                if (AeolianEdgePvE.CanUse(out act))
                {
                    return true;
                }
            }
            else
            {
                if (InBurstPhase &&
                    Kazematoi > 0 &&
                    AeolianEdgePvE.CanUse(out act) &&
                    AeolianEdgePvE.Target.Target != null &&
                    CanHitPositional(EnemyPositional.Rear, AeolianEdgePvE.Target.Target))
                {
                    return true;
                }

                if (InBurstPhase &&
                    Kazematoi > 0 &&
                    AeolianEdgePvE.CanUse(out act))
                {
                    return true;
                }

                if (Kazematoi < 2 &&
                    ArmorCrushPvE.CanUse(out act) &&
                    ArmorCrushPvE.Target.Target != null &&
                    CanHitPositional(EnemyPositional.Flank, ArmorCrushPvE.Target.Target))
                {
                    return true;
                }

                if (Kazematoi == 0 && ArmorCrushPvE.CanUse(out act))
                {
                    return true;
                }

                if (Kazematoi > 0 &&
                    AeolianEdgePvE.CanUse(out act) &&
                    AeolianEdgePvE.Target.Target != null &&
                    CanHitPositional(EnemyPositional.Rear, AeolianEdgePvE.Target.Target))
                {
                    return true;
                }

                if (Kazematoi < 4 &&
                    ArmorCrushPvE.CanUse(out act) &&
                    ArmorCrushPvE.Target.Target != null &&
                    CanHitPositional(EnemyPositional.Flank, ArmorCrushPvE.Target.Target))
                {
                    return true;
                }

                if (Kazematoi > 0 && AeolianEdgePvE.CanUse(out act))
                {
                    return true;
                }

                if (Kazematoi < 4 && ArmorCrushPvE.CanUse(out act))
                {
                    return true;
                }
            }
        }

        if (GustSlashPvE.CanUse(out act))
        {
            return true;
        }

        if (SpinningEdgePvE.CanUse(out act))
        {
            return true;
        }

        if (!IsExecutingMudra &&
            NoNinjutsu &&
            Player != null &&
            !StatusHelper.PlayerHasStatus(true, StatusID.TenChiJin) &&
            PhantomKamaitachiPvE.CanUse(out act))
        {
            return true;
        }

        if (!IsExecutingMudra &&
            NoNinjutsu &&
            IsBurst &&
            _ninActionAim == null &&
            !IsShadowWalking &&
            !HasKassatsu &&
            !HasTenChiJin &&
            JinPvE.Info.IsQuestUnlocked() &&
            (ShouldPrepBurstSuitonOrHuton ||
             (KunaisBanePvE.Cooldown.IsCoolingDown && KunaisBanePvE.Cooldown.RecastTimeRemain < 22f)))
        {
            if (NumberOfHostilesInRangeOf(3) >= 3)
            {
                if (HutonPvE.EnoughLevel &&
                    HutonPvE.IsEnabled)
                {
                    SetNinjutsu(HutonPvE);
                }
            }
            else
            {
                if (SuitonPvE.EnoughLevel &&
                    SuitonPvE.IsEnabled)
                {
                    SetNinjutsu(SuitonPvE);
                }
            }
        }

        if (!IsExecutingMudra &&
            NoNinjutsu &&
            IsBurst &&
            _ninActionAim == null &&
            !ShouldPrepBurstSuitonOrHuton &&
            !HasKassatsu)
        {
            if (NumberOfHostilesInRangeOf(3) >= 3 &&
                KatonPvE.EnoughLevel &&
                KatonPvE.IsEnabled &&
                ChiPvE.Info.IsQuestUnlocked())
            {
                SetNinjutsu(KatonPvE);
            }
            else if (ShouldUseRaitonDisengageFallback &&
                     !CombatElapsedLess(BurstBuffOpenTiming) &&
                     RaitonPvE.EnoughLevel &&
                     RaitonPvE.IsEnabled &&
                     ChiPvE.Info.IsQuestUnlocked())
            {
                SetNinjutsu(RaitonPvE);
            }
        }

        if (!IsExecutingMudra && ThrowingDaggerPvE.CanUse(out act))
        {
            return true;
        }

        if (StateEnabled && IsHidden)
        {
            StatusHelper.StatusOff(StatusID.Hidden);
        }

        if (!InCombat &&
            _ninActionAim == null &&
            TenPvE.Cooldown.IsCoolingDown &&
            HidePvE.CanUse(out act))
        {
            return true;
        }

        return base.GeneralGCD(out act);
    }

    #endregion
}