namespace RebornRotations.Tank;

[Rotation("Default", CombatType.PvE, GameVersion = "7.2")]
[SourceCode(Path = "main/BasicRotations/Tank/DRK_Default.cs")]
[Api(4)]
public sealed class DRK_Default : DarkKnightRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Keep at least 3000 MP")]
    public bool TheBlackestNight { get; set; } = true;
    #endregion

    #region Countdown Logic
    // Countdown logic to prepare for combat.
    // Includes logic for using Provoke, tank stances, and burst medicines.
    protected override IAction? CountDownAction(float remainTime)
    {
        //Provoke when has Shield.
        if (remainTime <= CountDownAhead)
        {
            if (HasTankStance)
            {
                if (ProvokePvE.CanUse(out _)) return ProvokePvE;
            }
        }
        if (remainTime <= 2 && UseBurstMedicine(out var act)) return act;
        if (remainTime <= 3 && TheBlackestNightPvE.CanUse(out act)) return act;
        if (remainTime <= 4 && BloodWeaponPvE.CanUse(out act)) return act;
        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    // Decision-making for emergency abilities, focusing on Blood Weapon usage.
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        
        return base.EmergencyAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.ShadowstridePvE)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (ShadowstridePvE.CanUse(out act)) return true;
        return base.MoveForwardAbility(nextGCD, out act);
    }

    // Determines healing actions based on The Blackest Night ability.
    [RotationDesc(ActionID.TheBlackestNightPvE)]
    protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (TheBlackestNightPvE.CanUse(out act)) return true;
        return base.HealSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.DarkMissionaryPvE, ActionID.ReprisalPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (!InTwoMIsBurst && DarkMissionaryPvE.CanUse(out act)) return true;
        if (!InTwoMIsBurst && ReprisalPvE.CanUse(out act, skipAoeCheck: true)) return true;

        return base.DefenseAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.TheBlackestNightPvE, ActionID.OblationPvE, ActionID.ReprisalPvE, ActionID.ShadowWallPvE, ActionID.RampartPvE, ActionID.DarkMindPvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        act = null;

        if (Player.HasStatus(true, StatusID.BlackestNight)) return false;

        //10
        if (OblationPvE.CanUse(out act, usedUp: true, skipStatusProvideCheck: false)) return true;

        if (TheBlackestNightPvE.CanUse(out act)) return true;
        //20
        if (DarkMindPvE.CanUse(out act)) return true;

        //30
        if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && ShadowWallPvE.CanUse(out act)) return true;
        if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && ShadowedVigilPvE.CanUse(out act)) return true;

        //20
        if (ShadowWallPvE.Cooldown.IsCoolingDown && ShadowWallPvE.Cooldown.ElapsedAfter(60) && RampartPvE.CanUse(out act)) return true;
        if (ShadowedVigilPvE.Cooldown.IsCoolingDown && ShadowedVigilPvE.Cooldown.ElapsedAfter(60) && RampartPvE.CanUse(out act)) return true;

        if (ReprisalPvE.CanUse(out act)) return true;

        return base.DefenseSingleAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (CheckDarkSide)
        {
            if (FloodOfDarknessPvE.CanUse(out act)) return true;
            if (EdgeOfDarknessPvE.CanUse(out act)) return true;
        }

        if (IsBurst)
        {
            if (InCombat && DeliriumPvE.CanUse(out act)) return true;
            if (DeliriumPvE.EnoughLevel && DeliriumPvE.Cooldown.ElapsedAfterGCD(1) && !DeliriumPvE.Cooldown.ElapsedAfterGCD(3)
                && BloodWeaponPvE.CanUse(out act)) return true;
            if (!DeliriumPvE.EnoughLevel)
            {
                if (BloodWeaponPvE.CanUse(out act)) return true;
            }
            if (InCombat && LivingShadowPvE.CanUse(out act, skipAoeCheck: true)) return true;
        }

        if (CombatElapsedLess(3))
        {
            act = null;
            return false;
        }

        if (!IsMoving && SaltedEarthPvE.CanUse(out act, skipAoeCheck: true)) return true;

        if (ShadowbringerPvE.CanUse(out act, skipAoeCheck: true)) return true;

        if (NumberOfHostilesInRange >= 3 && AbyssalDrainPvE.CanUse(out act)) return true;
        if (CarveAndSpitPvE.CanUse(out act)) return true;

        if (InTwoMIsBurst)
        {
            if (ShadowbringerPvE.CanUse(out act, usedUp: true, skipAoeCheck: true)) return true;

        }

        if (SaltAndDarknessPvE.CanUse(out act)) return true;

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        if (DisesteemPvE.CanUse(out act, skipComboCheck: true, skipAoeCheck: true)) return true;

        //AOE Delirium
        if (ImpalementPvE.CanUse(out act)) return true;
        if (QuietusPvE.CanUse(out act)) return true;

        // Single Target Delirium
        if (TorcleaverPvE.CanUse(out act, skipComboCheck: true)) return true;
        if (ComeuppancePvE.CanUse(out act, skipComboCheck: true)) return true;
        if (ScarletDeliriumPvE.CanUse(out act, skipComboCheck: true)) return true;

        if (BloodspillerPvE.CanUse(out act, skipComboCheck: true)) return true;

        //AOE
        if (StalwartSoulPvE.CanUse(out act)) return true;
        if (UnleashPvE.CanUse(out act)) return true;

        //Single Target
        if (!HasDelirium && SouleaterPvE.CanUse(out act)) return true;
        if (!HasDelirium && SyphonStrikePvE.CanUse(out act)) return true;
        if (!HasDelirium && HardSlashPvE.CanUse(out act)) return true;

        if (UnmendPvE.CanUse(out act)) return true;

        return base.GeneralGCD(out act);
    }
    #endregion

    #region Extra Methods
    // Indicates whether the Dark Knight can heal using a single ability.
    public override bool CanHealSingleAbility => false;

    // Logic to determine when to use blood-based abilities.
    private bool UseBlood
    {
        get
        {
            // Conditions based on player statuses and ability cooldowns.
            if (!DeliriumPvE.EnoughLevel || !LivingShadowPvE.EnoughLevel) return true;
            if (Player.HasStatus(true, StatusID.Delirium_3836)) return true;
            if ((Player.HasStatus(true, StatusID.Delirium_1972) || Player.HasStatus(true, StatusID.Delirium_3836)) && LivingShadowPvE.Cooldown.IsCoolingDown) return true;
            if (DeliriumPvE.Cooldown.WillHaveOneChargeGCD(1) && !LivingShadowPvE.Cooldown.WillHaveOneChargeGCD(3) || Blood >= 90 && !LivingShadowPvE.Cooldown.WillHaveOneChargeGCD(1)) return true;

            return false;

        }
    }
    // Determines if currently in a burst phase based on cooldowns of key abilities.
    private bool InTwoMIsBurst
    {
        get
        {
            if (BloodWeaponPvE.Cooldown.IsCoolingDown && DeliriumPvE.Cooldown.IsCoolingDown && (LivingShadowPvE.Cooldown.IsCoolingDown && !LivingShadowPvE.Cooldown.ElapsedAfter(15) || !LivingShadowPvE.EnoughLevel)) return true;
            else return false;
        }
    }

    // Manages DarkSide ability based on several conditions.
    private bool CheckDarkSide
    {
        get
        {
            if (DarkSideEndAfterGCD(3)) return true;

            if (CombatElapsedLess(3)) return false;

            if (InTwoMIsBurst && HasDarkArts || HasDarkArts && Player.HasStatus(true, StatusID.BlackestNight) || HasDarkArts && DarkSideEndAfterGCD(3)) return true;

            if (InTwoMIsBurst && BloodWeaponPvE.Cooldown.IsCoolingDown && LivingShadowPvE.Cooldown.IsCoolingDown && SaltedEarthPvE.Cooldown.IsCoolingDown && ShadowbringerPvE.Cooldown.CurrentCharges == 0 && CarveAndSpitPvE.Cooldown.IsCoolingDown) return true;

            if (TheBlackestNight && CurrentMp < 6000) return false;

            return CurrentMp >= 8500;
        }
    }
    #endregion
}