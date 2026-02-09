namespace RotationSolver.RebornRotations.Magical;

[Rotation("Basic BLU", CombatType.PvE, GameVersion = "7.41")]
[SourceCode(Path = "main/BasicRotations/Limited Jobs/BLU_Basic.cs")]

public sealed class Blue_Basic : BlueMageRotation
{
    [RotationConfig(CombatType.PvE, Name = "Single Target Spell")]
    public BluDPSSpell SingleTargetDPSSpell { get; set; } = BluDPSSpell.SonicBoom;

    [RotationConfig(CombatType.PvE, Name = "AoE Spell")]
    public BluAOESpell AoeSpell { get; set; } = BluAOESpell.MindBlast;

    [RotationConfig(CombatType.PvE, Name = "Healing Spell")]
    public BluHealSpell HealSpell { get; set; } = BluHealSpell.WhiteWind;

    [RotationConfig(CombatType.PvE, Name = "Use Basic Instinct")]
    public bool UseBasicInstinct { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Mighty Guard")]
    public bool UseMightyGuard { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Aetheric Mimicry Role")]
    public CombatRole CombatRole
    {
        get => BlueId;
        set => BlueId = value;
    }

    #region Countdown logic

    // Defines logic for actions to take during the countdown before combat starts.
    protected override IAction? CountDownAction(float remainTime)
    {
        return base.CountDownAction(remainTime);
    }

    #endregion

    #region Emergency Logic

    // Determines emergency actions to take based on the next planned GCD action.
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        return base.EmergencyAbility(nextGCD, out act);
    }

    #endregion

    #region Move oGCD Logic

    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        return base.MoveForwardAbility(nextGCD, out act);
    }

    protected override bool MoveBackAbility(IAction nextGCD, out IAction? act)
    {
        return base.MoveBackAbility(nextGCD, out act);
    }

    protected override bool SpeedAbility(IAction nextGCD, out IAction? act)
    {
        return base.SpeedAbility(nextGCD, out act);
    }

    #endregion

    #region Heal/Defense oGCD Logic

    protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        return base.HealSingleAbility(nextGCD, out act);
    }

    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        return base.DefenseAreaAbility(nextGCD, out act);
    }

    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        return base.DefenseSingleAbility(nextGCD, out act);
    }

    #endregion

    #region oGCD Logic

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        return base.AttackAbility(nextGCD, out act);
    }

    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        if (Player?.CurrentMp < 6000 && InCombat && LucidDreamingPvE.CanUse(out act))
        {
            return true;
        }
        //if (AethericMimicryPvE_19239.CanUse(out act)) return true;
        return base.GeneralAbility(nextGCD, out act);
    }

    #endregion

    public Blue_Basic()
    {
        BluDPSSpellActions.Add(BluDPSSpell.WaterCannon, WaterCannonPvE);
        BluDPSSpellActions.Add(BluDPSSpell.SonicBoom, SonicBoomPvE);
        BluDPSSpellActions.Add(BluDPSSpell.GoblinPunch, GoblinPunchPvE);

        BluHealSpellActions.Add(BluHealSpell.WhiteWind, WhiteWindPvE);
        BluHealSpellActions.Add(BluHealSpell.AngelsSnack, AngelsSnackPvE);

        BluAOESpellActions.Add(BluAOESpell.Glower, GlowerPvE);
        BluAOESpellActions.Add(BluAOESpell.FlyingFrenzy, FlyingFrenzyPvE);
        BluAOESpellActions.Add(BluAOESpell.FlameThrower, FlameThrowerPvE);
        BluAOESpellActions.Add(BluAOESpell.DrillCannons, DrillCannonsPvE);
        BluAOESpellActions.Add(BluAOESpell.Plaincracker, PlaincrackerPvE);
        BluAOESpellActions.Add(BluAOESpell.HighVoltage, HighVoltagePvE);
        BluAOESpellActions.Add(BluAOESpell.MindBlast, MindBlastPvE);
        BluAOESpellActions.Add(BluAOESpell.ThousandNeedles, _1000NeedlesPvE);
    }

    #region GCD Logic

    protected override bool EmergencyGCD(out IAction? act)
    {
        return base.EmergencyGCD(out act);
    }

    protected override bool MyInterruptGCD(out IAction? act)
    {
        return FlyingSardinePvE.CanUse(out act) || base.MyInterruptGCD(out act);
    }

    protected override bool DefenseAreaGCD(out IAction? act)
    {
        //if (ColdFogPvE.CanUse(out act)) return true;
        return base.DefenseAreaGCD(out act);
    }

    protected override bool DefenseSingleGCD(out IAction? act)
    {
        return base.DefenseSingleGCD(out act);
    }

    protected override bool HealAreaGCD(out IAction? act)
    {
        return BluHealSpellActions[HealSpell].CanUse(out act) || base.HealAreaGCD(out act);
    }

    protected override bool HealSingleGCD(out IAction? act)
    {
        return BluHealSpellActions[HealSpell].CanUse(out act) || base.HealSingleGCD(out act);
    }

    protected override bool MoveForwardGCD(out IAction? act)
    {
        return base.MoveForwardGCD(out act);
    }

    protected override bool DispelGCD(out IAction? act)
    {
        return base.DispelGCD(out act);
    }

    protected override bool RaiseGCD(out IAction? act)
    {
        if (AngelWhisperPvE.CanUse(out act))
        {
            return true;
        }
        return base.RaiseGCD(out act);
    }

    protected override bool GeneralGCD(out IAction? act)
    {
        if (AethericMimicryPvE.CanUse(out act))
        {
            return true;
        }

        if (UseMightyGuard && MightyGuardPvE.CanUse(out act))
        {
            return true;
        }

        if (UseBasicInstinct && BasicInstinctPvE.CanUse(out act))
        {
            return true;
        }

        if (BluAOESpellActions[AoeSpell].CanUse(out act))
        {
            return true;
        }

        if (BluDPSSpellActions[SingleTargetDPSSpell].CanUse(out act))
        {
            return true;
        }

        if (FlyingSardinePvE.CanUse(out act))
        {
            return true;
        }

        return base.GeneralGCD(out act);
    }

    #endregion

    protected override IBaseAction[] ActiveActions =>
    [
        WaterCannonPvE,
        SonicBoomPvE,
        GoblinPunchPvE,
        WhiteWindPvE,
        AngelsSnackPvE,
        GlowerPvE,
        FlyingFrenzyPvE,
        FlameThrowerPvE,
        DrillCannonsPvE,
        PlaincrackerPvE,
        HighVoltagePvE,
        MindBlastPvE,
        _1000NeedlesPvE,
        BasicInstinctPvE,
        MightyGuardPvE,
        AethericMimicryPvE,
        FlyingSardinePvE,
        BloodDrainPvE,
        LoomPvE,
        SelfdestructPvE,
        DiamondbackPvE
    ];
}