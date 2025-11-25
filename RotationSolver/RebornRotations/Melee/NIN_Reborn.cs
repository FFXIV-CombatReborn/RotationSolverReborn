namespace RotationSolver.RebornRotations.Melee;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.35")]
[SourceCode(Path = "main/RebornRotations/Melee/NIN_Reborn.cs")]

public sealed class NIN_Reborn : NinjaRotation
{
    #region Config Options
    // Configuration properties for rotation behavior.

    [RotationConfig(CombatType.PvE, Name = "Use Hide")]
    public bool UseHide { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Unhide")]
    public bool AutoUnhide { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Mudras outside of combat when enemies are near")]
    public bool CombatMudra { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Forked Raiju instead of Fleeting Raiju if you are outside of range (Dangerous)")]
    public bool ForkedUse { get; set; } = false;
    #endregion

    #region Tracking Properties
    // Properties to track RabbitMediumPvE failures and related information.
    //private int _rabbitMediumFailures = GetActionUsageCount((uint)ActionID.RabbitMediumPvE);
    private IBaseAction? _lastNinActionAim = null;
    // Holds the next ninjutsu action to perform.
    private IBaseAction? _ninActionAim = null;
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

    private bool KeepKassatsuinBurst => !Player.WillStatusEndGCD(2, 0, true, StatusID.Kassatsu) && HasKassatsu && !InTrickAttack && !IsExecutingMudra;

    public override void DisplayRotationStatus()
    {
        ImGui.Text($"Last Ninjutsu Action Cleared From Queue: {_lastNinActionAim}");
        ImGui.Text($"Current Ninjutsu Action: {_ninActionAim}");
        ImGui.Text($"Ninjutsu ID: {AdjustId(NinjutsuPvEid)}");
    }
    #endregion

    #region CountDown Logic
    // Logic to determine the action to take during the countdown phase before combat starts.
    protected override IAction? CountDownAction(float remainTime)
    {
        _ = IsLastAction(false, HutonPvE);
        // Clears ninjutsu setup if countdown is more than 6 seconds or if Suiton is the aim but shouldn't be.
        if (remainTime > 6)
        {
            ClearNinjutsu();
        }

        // Decision-making for ninjutsu actions based on remaining time until combat starts.
        if (DoSuiton(out IAction? act))
        {
            return act == SuitonPvE && remainTime > CountDownAhead ? null : act;
        }

        else if (remainTime < 5)
        {
            SetNinjutsu(SuitonPvE);
        }
        else if (remainTime < 6)
        {
            // If within 10 seconds to start, consider using Hide or setting up Huton.
            if (_ninActionAim == null && TenPvE.Cooldown.IsCoolingDown && HidePvE.CanUse(out act))
            {
                return act;
            }
        }
        return base.CountDownAction(remainTime);
    }
    #endregion

    #region Move Logic
    // Defines logic for actions to take when moving forward during combat.
    // This attribute associates the method with the Forked Raiju PvE action, 
    // indicating it's a relevant ability when considering movement-based actions.
    [RotationDesc(ActionID.ForkedRaijuPvE)]
    protected override bool MoveForwardGCD(out IAction? act)
    {
        // Checks if Forked Raiju, a movement-friendly ability, can be used. 
        // If so, sets it as the action to perform, returning true to indicate an action has been selected.
        if (ForkedRaijuPvE.CanUse(out act))
        {
            return true;
        }

        // If Forked Raiju is not available or not the best option, 
        // falls back to the base class's logic for choosing a move-forward action.
        return base.MoveForwardGCD(out act);
    }
    #endregion

    #region oGCD Logic
    // Determines the emergency abilities to use, overriding the base class implementation.
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        // If the last action performed matches any of a list of specific actions, it clears the Ninjutsu aim.
        // This serves as a reset/cleanup mechanism to ensure the decision logic starts fresh for the next cycle.
        if (IsLastAction(false, FumaShurikenPvE, KatonPvE, RaitonPvE, HyotonPvE, DotonPvE, SuitonPvE)
            || (IsShadowWalking && (_ninActionAim == SuitonPvE || _ninActionAim == HutonPvE))
            || (_ninActionAim == GokaMekkyakuPvE && IsLastGCD(false, GokaMekkyakuPvE))
            || (_ninActionAim == HyoshoRanryuPvE && IsLastGCD(false, HyoshoRanryuPvE))
            || (_ninActionAim == GokaMekkyakuPvE && !HasKassatsu)
            || (_ninActionAim == HyoshoRanryuPvE && !HasKassatsu))
        {
            ClearNinjutsu();
        }

        // Side-effect: decide/refresh ninjutsu aim; do not consume the oGCD slot here.
        if (InCombat || (CombatMudra && HasHostilesInMaxRange && TenPvE.Cooldown.CurrentCharges == TenPvE.Cooldown.MaxCharges))
        {
            _ = ChoiceNinjutsu(out _);
        }

        if (!InCombat && !CombatMudra)
        {
            ClearNinjutsu();
        }

        if (RabbitMediumPvE.CanUse(out act))
        {
            return true;
        }

        // If Ninjutsu is available or not in combat, defers to the base class's emergency ability logic.
        if (!NoNinjutsu || !InCombat)
        {
            return base.EmergencyAbility(nextGCD, out act);
        }

        // First priority is given to Kassatsu if it's available, allowing for an immediate powerful Ninjutsu.
        if (NoNinjutsu && !nextGCD.IsTheSameTo(false, ActionID.TenPvE, ActionID.ChiPvE, ActionID.JinPvE) && IsShadowWalking && KassatsuPvE.CanUse(out act))
        {
            return true;
        }

        if ((!TenChiJinPvE.Cooldown.IsCoolingDown || Player.WillStatusEndGCD(2, 0, true, StatusID.ShadowWalker)) && TrickAttackPvE.Cooldown.IsCoolingDown && MeisuiPvE.CanUse(out act))
        {
            return true;
        }

        if (TenriJindoPvE.CanUse(out act))
        {
            return true;
        }

        // If in a burst phase and not just starting combat, checks if Mug is available to generate additional Ninki.
        if (IsBurst && !CombatElapsedLess(5))
        {
            if (!DokumoriPvE.EnoughLevel)
            {
                if (MugPvE.CanUse(out act))
                {
                    return true;
                }
            }
            else if (DokumoriPvE.EnoughLevel)
            {
                if (DokumoriPvE.CanUse(out act))
                {
                    return true;
                }
            }
        }


        // Prioritizes using Suiton and Trick Attack for maximizing damage, especially outside the initial combat phase.
        if (!CombatElapsedLess(6))
        {
            // Attempts to use Trick Attack if it's available.
            if (!KunaisBanePvE.EnoughLevel)
            {
                if (TrickAttackPvE.CanUse(out act, skipStatusProvideCheck: IsShadowWalking))
                {
                    return true;
                }
            }
            else if (KunaisBanePvE.EnoughLevel)
            {
                if (KunaisBanePvE.CanUse(out act, skipAoeCheck: true, skipStatusProvideCheck: IsShadowWalking))
                {
                    return true;
                }
            }

            // If Trick Attack is on cooldown but will not be ready soon, considers using Meisui to recover Ninki.
            if (TrickAttackPvE.Cooldown.IsCoolingDown && !TrickAttackPvE.Cooldown.WillHaveOneCharge(19) && TenChiJinPvE.Cooldown.IsCoolingDown && TrickAttackPvE.Cooldown.IsCoolingDown && MeisuiPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (InTrickAttack && InCombat && UseBurstMedicine(out act))
        {
            return true;
        }

        // If none of the specific conditions are met, falls back to the base class's emergency ability logic.
        return base.EmergencyAbility(nextGCD, out act);
    }

    // Defines attack abilities to use during combat, overriding the base class implementation.
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        // If Ninjutsu is available or not in combat, it exits early, indicating no attack action to perform.
        if (!NoNinjutsu || !InCombat)
        {
            return base.AttackAbility(nextGCD, out act);
        }

        // If the player is within Trick Attack's effective window, and Ten Chi Jin hasn't recently been used,
        // then Ten Chi Jin is set as the next action to perform.
        if (InTrickAttack && !Player.HasStatus(true, StatusID.ShadowWalker) && !TenPvE.Cooldown.ElapsedAfter(30) && TenChiJinPvE.CanUse(out act))
        {
            return true;
        }

        // If more than 5 seconds have passed in combat, checks if Bunshin is available to use.
        if (!CombatElapsedLess(5) && BunshinPvE.CanUse(out act))
        {
            return true;
        }

        // Special handling if within Trick Attack's effective window:
        if (InTrickAttack)
        {
            // If Dream Within A Dream is not yet available, checks if Assassinate can be used.
            if (DreamWithinADreamPvE.CanUse(out act))
            {
                return true;
            }
            if (!DreamWithinADreamPvE.Info.EnoughLevelAndQuest() && AssassinatePvE.CanUse(out act))
            {
                return true;
            }
        }

        // Checks for the use of Hellfrog Medium or Bhavacakra under certain conditions:
        // - Not in the Mug's effective window or within Trick Attack's window
        // - Certain cooldown conditions are met, or specific statuses are active.
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

        if (Ninki == 100)
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
        // If none of the conditions are met, it falls back to the base class's implementation for attack ability.
        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region Ninjutsu Logic
    private void SetNinjutsu(IBaseAction act)
    {
        if (act == null || AdjustId(ActionID.NinjutsuPvE) == ActionID.RabbitMediumPvE)
        {
            return;
        }

        if (_ninActionAim != null && IsLastAction(false, TenPvE, JinPvE, ChiPvE, FumaShurikenPvE_18873, FumaShurikenPvE_18874, FumaShurikenPvE_18875))
        {
            return;
        }

        if (_ninActionAim != act)
        {
            _ninActionAim = act;
        }
    }

    // Clears the ninjutsu action aim, effectively resetting any planned ninjutsu action.
    private void ClearNinjutsu()
    {
        if (_ninActionAim != null)
        {
            _lastNinActionAim = _ninActionAim;
            _ninActionAim = null;
        }
    }

    // Logic for choosing which ninjutsu action to set up next, based on various game state conditions.
    private bool ChoiceNinjutsu(out IAction? act)
    {
        act = null;

        // Checks for Kassatsu status to prioritize high-impact Ninjutsu due to its buff.
        if (Player.HasStatus(true, StatusID.Kassatsu))
        {
            if ((DeathBlossomPvE.CanUse(out _) || HakkeMujinsatsuPvE.CanUse(out _)) && GokaMekkyakuPvE.EnoughLevel && !IsLastAction(false, GokaMekkyakuPvE) && GokaMekkyakuPvE.IsEnabled && ChiPvE.Info.IsQuestUnlocked())
            {
                SetNinjutsu(GokaMekkyakuPvE);
            }
            if (!DeathBlossomPvE.CanUse(out _) && !HakkeMujinsatsuPvE.CanUse(out _) && HyoshoRanryuPvE.EnoughLevel && !IsLastAction(false, HyoshoRanryuPvE) && HyoshoRanryuPvE.IsEnabled && JinPvE.Info.IsQuestUnlocked())
            {
                SetNinjutsu(HyoshoRanryuPvE);
            }

            if (!IsShadowWalking && ShadowWalkerNeeded && !HyoshoRanryuPvE.EnoughLevel && HutonPvE.EnoughLevel && HutonPvE.IsEnabled && JinPvE.Info.IsQuestUnlocked())
            {
                SetNinjutsu(HutonPvE);
            }

            if ((DeathBlossomPvE.CanUse(out _) || HakkeMujinsatsuPvE.CanUse(out _)) && !HyoshoRanryuPvE.EnoughLevel && KatonPvE.EnoughLevel && KatonPvE.IsEnabled && ChiPvE.Info.IsQuestUnlocked())
            {
                SetNinjutsu(KatonPvE);
            }

            if (!DeathBlossomPvE.CanUse(out _) && !HakkeMujinsatsuPvE.CanUse(out _) && !HyoshoRanryuPvE.EnoughLevel && RaitonPvE.EnoughLevel && RaitonPvE.IsEnabled && ChiPvE.Info.IsQuestUnlocked())
            {
                SetNinjutsu(RaitonPvE);
            }
        }
        else if (TenPvE.CanUse(out _, usedUp: ShadowWalkerNeeded || InTrickAttack || TenPvE.Cooldown.WillHaveXChargesGCD(2, 2, 0)) && _ninActionAim == null)
        {
            //Vulnerable
            if (ShadowWalkerNeeded && (!MeisuiPvE.Cooldown.IsCoolingDown || !TrickAttackPvE.Cooldown.IsCoolingDown || KunaisBanePvE.Cooldown.IsCoolingDown) && !IsShadowWalking && !HasTenChiJin && SuitonPvE.EnoughLevel)
            {
                if (DeathBlossomPvE.CanUse(out _) && JinPvE.CanUse(out _) && JinPvE.Info.IsQuestUnlocked() && HutonPvE.IsEnabled)
                {
                    SetNinjutsu(HutonPvE);
                }
                else if (JinPvE.CanUse(out _) && JinPvE.Info.IsQuestUnlocked() && SuitonPvE.IsEnabled && ((TrickAttackPvE.IsEnabled && !KunaisBanePvE.EnoughLevel) || (KunaisBanePvE.IsEnabled && KunaisBanePvE.EnoughLevel)))
                {
                    SetNinjutsu(SuitonPvE);
                }
            }

            //Aoe
            if (DeathBlossomPvE.CanUse(out _) || HakkeMujinsatsuPvE.CanUse(out _))
            {
                if ((!HasDoton && !IsMoving && !IsLastGCD(true, DotonPvE) && (!TenChiJinPvE.Cooldown.WillHaveOneCharge(6)) && DotonPvE.EnoughLevel)
                    || (!HasDoton && !IsLastGCD(true, DotonPvE) && !TenChiJinPvE.Cooldown.IsCoolingDown && DotonPvE.EnoughLevel))
                {
                    if (JinPvE.CanUse(out _) && DotonPvE.IsEnabled && JinPvE.Info.IsQuestUnlocked())
                    {
                        SetNinjutsu(DotonPvE);
                    }
                }
                else if (KatonPvE.EnoughLevel && KatonPvE.IsEnabled && ChiPvE.Info.IsQuestUnlocked())
                {
                    SetNinjutsu(KatonPvE);
                }
            }

            //Single
            if (!DeathBlossomPvE.CanUse(out _) && !HakkeMujinsatsuPvE.CanUse(out _) && !ShadowWalkerNeeded)
            {
                if (RaitonPvE.EnoughLevel && RaitonPvE.IsEnabled && ChiPvE.Info.IsQuestUnlocked() && (!Player.HasStatus(true, StatusID.RaijuReady) || (Player.HasStatus(true, StatusID.RaijuReady) && Player.StatusStack(true, StatusID.RaijuReady) < 3)))
                {
                    SetNinjutsu(RaitonPvE);
                }

                if (FumaShurikenPvE.EnoughLevel && FumaShurikenPvE.IsEnabled && TenPvE.Info.IsQuestUnlocked() && (!RaitonPvE.EnoughLevel || (Player.HasStatus(true, StatusID.RaijuReady) && Player.StatusStack(true, StatusID.RaijuReady) == 3)))
                {
                    SetNinjutsu(FumaShurikenPvE);
                }
            }
        }
        return false; // Indicates that no specific Ninjutsu action was chosen in this cycle.
    }
    #endregion

    #region Ninjutsu Execution
    private bool DoRabbitMedium(out IAction? act)
    {
        act = null;
        uint ninjutsunId = AdjustId(NinjutsuPvE.ID);
        if (ninjutsunId == RabbitMediumPvE.ID)
        {
            if (RabbitMediumPvE.CanUse(out act))
            {
                return true;
            }

            ClearNinjutsu();
        }
        return false;
    }

    private bool DoTenChiJin(out IAction? act)
    {
        act = null;

        if (HasTenChiJin)
        {
            uint tenId = AdjustId(TenPvE.ID);
            uint chiId = AdjustId(ChiPvE.ID);
            uint jinId = AdjustId(JinPvE.ID);

            //First
            if (tenId == FumaShurikenPvE_18873.ID
                && !IsLastAction(false, FumaShurikenPvE_18875, FumaShurikenPvE_18873))
            {
                //AOE
                if (DeathBlossomPvE.CanUse(out _))
                {
                    if (FumaShurikenPvE_18875.CanUse(out act))
                    {
                        return true;
                    }
                }
                //Single
                if (FumaShurikenPvE_18873.CanUse(out act))
                {
                    return true;
                }
            }

            //Second
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
            //Others
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
        }
        return false;
    }

    private bool DoHyoshoRanryu(out IAction? act)
    {
        act = null;

        if ((!TrickAttackPvE.Cooldown.IsCoolingDown || TrickAttackPvE.Cooldown.WillHaveOneCharge(Player.StatusTime(true, StatusID.Kassatsu))) && !IsExecutingMudra)
        {
            return false;
        }

        if (_ninActionAim == HyoshoRanryuPvE)
        {
            //Failed
            if (RabbitMediumCurrent)
            {
                ClearNinjutsu();
                act = null;
                return false;
            }
            //Action Execution
            else if (HyoshoRanryuCurrent)
            {
                if (HyoshoRanryuPvE.CanUse(out act, skipAoeCheck: true))
                {
                    return true;
                }
            }
            //Second
            else if (FumaShurikenCurrent)
            {
                if (JinPvE_18807.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }
            //First
            else if (NoActiveNinjutsu)
            {
                if (ChiPvE_18806.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool DoGokaMekkyaku(out IAction? act)
    {
        act = null;

        if ((!TrickAttackPvE.Cooldown.IsCoolingDown || TrickAttackPvE.Cooldown.WillHaveOneCharge(Player.StatusTime(true, StatusID.Kassatsu))) && !IsExecutingMudra)
        {
            return false;
        }

        if (_ninActionAim == GokaMekkyakuPvE)
        {
            //Failed
            if (RabbitMediumCurrent)
            {
                ClearNinjutsu();
                act = null;
                return false;
            }
            //Action Execution
            else if (GokaMekkyakuCurrent)
            {
                if (GokaMekkyakuPvE.CanUse(out act, skipAoeCheck: true))
                {
                    return true;
                }
            }
            //Second
            else if (FumaShurikenCurrent)
            {
                if (TenPvE_18805.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }
            //First
            else if (NoActiveNinjutsu)
            {
                if (ChiPvE_18806.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool DoSuiton(out IAction? act)
    {
        act = null;

        if (KeepKassatsuinBurst)
        {
            return false;
        }

        if (_ninActionAim == SuitonPvE)
        {
            //Failed
            if (RabbitMediumCurrent)
            {
                ClearNinjutsu();
                act = null;
                return false;
            }
            //Action Execution
            else if (SuitonCurrent)
            {
                if (SuitonPvE.CanUse(out act))
                {
                    return true;
                }
            }
            //Third
            else if (RaitonCurrent)
            {
                if (JinPvE_18807.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }
            //Second
            else if (FumaShurikenCurrent)
            {
                if (ChiPvE_18806.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }
            //First
            else if (NoActiveNinjutsu)
            {
                if (TenPvE.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool DoDoton(out IAction? act)
    {
        act = null;

        if (KeepKassatsuinBurst)
        {
            return false;
        }

        if (_ninActionAim == DotonPvE)
        {
            //Failed
            if (RabbitMediumCurrent)
            {
                ClearNinjutsu();
                act = null;
                return false;
            }
            //Action Execution
            else if (DotonCurrent)
            {
                if (DotonPvE.CanUse(out act, skipAoeCheck: true))
                {
                    return true;
                }
            }
            //Third
            else if (HyotonCurrent)
            {
                if (ChiPvE_18806.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }
            //Second
            else if (FumaShurikenCurrent)
            {
                if (JinPvE_18807.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }
            //First
            else if (NoActiveNinjutsu)
            {
                if (TenPvE.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool DoHuton(out IAction? act)
    {
        act = null;

        if (KeepKassatsuinBurst)
        {
            return false;
        }

        if (_ninActionAim == HutonPvE)
        {
            //Failed
            if (RabbitMediumCurrent)
            {
                ClearNinjutsu();
                act = null;
                return false;
            }
            //Action Execution
            else if (HutonCurrent)
            {
                if (HutonPvE.CanUse(out act, skipAoeCheck: true))
                {
                    return true;
                }
            }
            //Third
            else if (HyotonCurrent)
            {
                if (TenPvE_18805.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }
            //Second
            else if (FumaShurikenCurrent)
            {
                if (JinPvE_18807.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }
            //First
            else if (NoActiveNinjutsu)
            {
                if (ChiPvE.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool DoHyoton(out IAction? act)
    {
        act = null;

        if (KeepKassatsuinBurst)
        {
            return false;
        }

        if (_ninActionAim == HyotonPvE)
        {
            //Failed
            if (RabbitMediumCurrent)
            {
                ClearNinjutsu();
                act = null;
                return false;
            }
            //Action Execution
            else if (HyotonCurrent)
            {
                if (HyotonPvE.CanUse(out act))
                {
                    return true;
                }
            }
            //Second
            else if (FumaShurikenCurrent)
            {
                if (JinPvE_18807.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }
            //First
            else if (NoActiveNinjutsu)
            {
                if (ChiPvE.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool DoRaiton(out IAction? act)
    {
        act = null;

        if (KeepKassatsuinBurst)
        {
            return false;
        }

        if (_ninActionAim == RaitonPvE)
        {
            //Failed
            if (RabbitMediumCurrent)
            {
                ClearNinjutsu();
                act = null;
                return false;
            }
            //Action Execution
            else if (RaitonCurrent)
            {
                if (RaitonPvE.CanUse(out act))
                {
                    return true;
                }
            }
            //Second
            else if (FumaShurikenCurrent)
            {
                if (ChiPvE_18806.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }
            //First
            else if (NoActiveNinjutsu)
            {
                if (TenPvE.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool DoKaton(out IAction? act)
    {
        act = null;

        if (KeepKassatsuinBurst)
        {
            return false;
        }

        if (_ninActionAim == KatonPvE)
        {
            //Failed
            if (RabbitMediumCurrent)
            {
                ClearNinjutsu();
                act = null;
                return false;
            }
            //Action Execution
            else if (KatonCurrent)
            {
                if (KatonPvE.CanUse(out act, skipAoeCheck: true))
                {
                    return true;
                }
            }
            //Second
            else if (FumaShurikenCurrent)
            {
                if (TenPvE_18805.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }
            //First
            else if (NoActiveNinjutsu)
            {
                if (ChiPvE.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool DoFumaShuriken(out IAction? act)
    {
        act = null;

        if (KeepKassatsuinBurst)
        {
            return false;
        }

        if (_ninActionAim == FumaShurikenPvE)
        {
            //Failed
            if (RabbitMediumCurrent)
            {
                ClearNinjutsu();
                act = null;
                return false;
            }
            //Action Execution
            else if (FumaShurikenCurrent)
            {
                if (FumaShurikenPvE.CanUse(out act))
                {
                    return true;
                }
            }
            //First
            else if (NoActiveNinjutsu)
            {
                if (TenPvE.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }
        }
        return false;
    }
    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        if (!IsExecutingMudra && (InTrickAttack || InMug) && NoNinjutsu && !HasRaijuReady
            && !Player.HasStatus(true, StatusID.TenChiJin)
            && PhantomKamaitachiPvE.CanUse(out act))
        {
            return true;
        }

        if (!IsExecutingMudra)
        {
            if (FleetingRaijuPvE.CanUse(out act))
            {
                return true;
            }

            if (ForkedUse && ForkedRaijuPvE.CanUse(out act))
            {
                return true;
            }
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
            if (DoGokaMekkyaku(out act))
            {
                return true;
            }

            if (DoHuton(out act))
            {
                return true;
            }

            if (DoDoton(out act))
            {
                return true;
            }

            if (DoKaton(out act))
            {
                return true;
            }

            if (DoHyoshoRanryu(out act))
            {
                return true;
            }

            if (DoSuiton(out act))
            {
                return true;
            }

            if (DoHyoton(out act))
            {
                return true;
            }

            if (DoRaiton(out act))
            {
                return true;
            }

            if (DoFumaShuriken(out act))
            {
                return true;
            }
        }

        if (IsExecutingMudra)
        {
            return base.GeneralGCD(out act);
        }

        // AOE
        if (HakkeMujinsatsuPvE.CanUse(out act))
        {
            return true;
        }

        if (DeathBlossomPvE.CanUse(out act))
        {
            return true;
        }

        //Single
        if (AeolianEdgePvE.EnoughLevel)
        {
            // If ArmorCrushPvE is not yet available, checks if AeolianEdgePvE can be used.
            if (!ArmorCrushPvE.EnoughLevel)
            {
                if (AeolianEdgePvE.CanUse(out act))
                {
                    return true;
                }
            }
            else
            {
                if (Kazematoi == 0 && ArmorCrushPvE.CanUse(out act))
                {
                    return true;
                }
                else if (Kazematoi > 0 && AeolianEdgePvE.CanUse(out act) && AeolianEdgePvE.Target.Target != null && CanHitPositional(EnemyPositional.Rear, AeolianEdgePvE.Target.Target))
                {
                    return true;
                }
                else if (Kazematoi < 4 && ArmorCrushPvE.CanUse(out act) && ArmorCrushPvE.Target.Target != null && CanHitPositional(EnemyPositional.Flank, ArmorCrushPvE.Target.Target))
                {
                    return true;
                }
                else if (Kazematoi > 0 && AeolianEdgePvE.CanUse(out act))
                {
                    return true;
                }
                else if (Kazematoi < 4 && ArmorCrushPvE.CanUse(out act))
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

        //Range
        if (!IsExecutingMudra)
        {
            if (ThrowingDaggerPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (AutoUnhide && IsHidden)
        {
            StatusHelper.StatusOff(StatusID.Hidden);
        }

        if (!InCombat && _ninActionAim == null && UseHide
            && TenPvE.Cooldown.IsCoolingDown && HidePvE.CanUse(out act))
        {
            return true;
        }

        return base.GeneralGCD(out act);
    }
    #endregion

    /// <inheritdoc/>
    public override bool IsBursting()
    {
        if (InTrickAttack)
        {
            return true;
        }
        return false;
    }
}