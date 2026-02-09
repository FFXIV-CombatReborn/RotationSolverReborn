namespace RotationSolver.RebornRotations.Tank;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.41")]
[SourceCode(Path = "main/RebornRotations/Tank/PLD_Reborn.cs")]

public sealed class PLD_Reborn : PaladinRotation
{
    #region Config Options

    [RotationConfig(CombatType.PvE, Name = "Use GCDs to heal. (Ignored if there are no healers alive in party)")]
    public bool GCDHeal { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Divine Veil during countdown")]
    public bool DivineVeilCountdown { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Only use Fight or Flight while in melee range of an enemy")]
    public bool MeleeFoF { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Prevent actions while you have Passage of Arms up")]
    public bool PassageProtec { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Hallowed Ground with Cover")]
    private bool HallowedWithCover { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use up both stacks of Intervene during burst window")]
    private bool UseInterveneFight { get; set; } = true;

    [Range(0, 100, ConfigUnitType.Pixels)]
    [RotationConfig(CombatType.PvE, Name = "Use Sheltron at minimum X Oath to prevent over cap (Set to 0 to disable)")]
    private int WhenToSheltron { get; set; } = 100;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Health threshold for Intervention (Set to 0 to disable)")]
    private float InterventionRatio { get; set; } = 0.6f;

    [RotationConfig(CombatType.PvE, Name = "Use Intervention on CoTank during tankbusters")]
    private bool InterventionTank { get; set; } = false;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Health threshold for using Intervention to attempt to save someone")]
    private float InterventionClutch { get; set; } = 0.6f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Health threshold for Cover (Set to 0 to disable)")]
    private float CoverRatio { get; set; } = 0.3f;

    [RotationConfig(CombatType.PvE, Name = "Use Holy Spirit when out of melee range")]
    private bool UseHolyWhenAway { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Clemency with Requiescat")]
    private bool RequiescatHealBot { get; set; } = true;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum HP threshold party member needs to be to use Clemency with Requiescat")]
    public float ClemencyRequi { get; set; } = 0.2f;

    [RotationConfig(CombatType.PvE, Name = "Use Clemency without Requiescat")]
    private bool HealBot { get; set; } = true;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum HP threshold party member needs to be to use Clemency without Requiescat")]
    public float ClemencyNoRequi { get; set; } = 0.4f;
    #endregion

    #region Tracking Properties
    public override void DisplayRotationStatus()
    {
        ImGui.Text($"Use Oath: {UseOath(out _)}");
    }
    #endregion

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (remainTime < HolySpiritPvE.Info.CastTime + CountDownAhead
            && HolySpiritPvE.CanUse(out IAction? act))
        {
            return act;
        }

        if (DivineVeilCountdown && remainTime < 15 && DivineVeilPvE.CanUse(out act))
        {
            return act;
        }

        return base.CountDownAction(remainTime);
    }
    #endregion

    #region Additional oGCD Logic

    [RotationDesc(ActionID.CoverPvE)]
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (PassageProtec && StatusHelper.PlayerHasStatus(true, StatusID.PassageOfArms))
        {
            return false;
        }

        if (StatusHelper.PlayerHasStatus(true, StatusID.Cover) && HallowedWithCover && HallowedGroundPvE.CanUse(out act))
        {
            return true;
        }

        if (HallowedGroundPvE.CanUse(out act)
        && Player?.GetHealthRatio() <= HealthForDyingTanks)
        {
            return true;
        }

        if ((StatusHelper.PlayerHasStatus(true, StatusID.Rampart) || StatusHelper.PlayerHasStatus(true, StatusID.Sentinel)) &&
            InterventionPvE.CanUse(out act, skipTargetStatusNeedCheck: true) &&
            InterventionPvE.Target.Target?.GetHealthRatio() < InterventionClutch)
        {
            return true;
        }

        if (CoverPvE.CanUse(out act) && CoverPvE.Target.Target?.DistanceToPlayer() < 10 &&
            CoverPvE.Target.Target?.GetHealthRatio() < CoverRatio)
        {
            return true;
        }

        if ((HasHostilesInRange && MeleeFoF) || !MeleeFoF)
        {
            if (!RiotBladePvE.EnoughLevel && nextGCD.IsTheSameTo(true, FastBladePvE) && FightOrFlightPvE.CanUse(out act))
            {
                return true;
            }

            if (!RageOfHalonePvE.EnoughLevel && nextGCD.IsTheSameTo(true, RiotBladePvE, TotalEclipsePvE) && FightOrFlightPvE.CanUse(out act))
            {
                return true;
            }

            if (!ProminencePvE.EnoughLevel && nextGCD.IsTheSameTo(true, RageOfHalonePvE, TotalEclipsePvE) && FightOrFlightPvE.CanUse(out act))
            {
                return true;
            }

            if (!AtonementPvE.EnoughLevel && nextGCD.IsTheSameTo(true, RoyalAuthorityPvE, ProminencePvE) && FightOrFlightPvE.CanUse(out act))
            {
                return true;
            }

            if (AtonementPvE.EnoughLevel && (StatusHelper.PlayerHasStatus(true, StatusID.AtonementReady, StatusID.SepulchreReady, StatusID.SupplicationReady, StatusID.DivineMight) || IsLastAction(true, RoyalAuthorityPvE)) && FightOrFlightPvE.CanUse(out act))
            {
                return true;
            }
        }

        // if requiscat is able to proc confiteor, use it immediately after Fight or Flight
        if (RequiescatMasteryTrait.EnoughLevel)
        {
            if ((IsLastAbility(true, FightOrFlightPvE) || HasFightOrFlight) && ImperatorPvE.CanUse(out act, skipAoeCheck: true, usedUp: true, skipTTKCheck: true))
            {
                return true;
            }

            if ((IsLastAbility(true, FightOrFlightPvE) || HasFightOrFlight) && RequiescatPvE.CanUse(out act, skipAoeCheck: true, usedUp: true))
            {
                return true;
            }
        }

        // if requiscat is not able to proc confiteor, use it as AOE tool if able, otherwise as Single Target
        if (!RequiescatMasteryTrait.EnoughLevel)
        {
            if (HolyCirclePvE.EnoughLevel && NumberOfHostilesInRange >= 1 && (IsLastAbility(true, FightOrFlightPvE) || HasFightOrFlight) && RequiescatPvE.CanUse(out act, skipAoeCheck: true, usedUp: true))
            {
                return true;
            }

            if (RequiescatPvE.CanUse(out act, skipAoeCheck: true, usedUp: true))
            {
                if (!HolyCirclePvE.EnoughLevel && (NumberOfHostilesInRange == 1 || RequiescatPvE.Target.Target.IsBossFromIcon()))
                {
                    return true;
                }
            }
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    [RotationDesc]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (PassageProtec && StatusHelper.PlayerHasStatus(true, StatusID.PassageOfArms))
        {
            return false;
        }

        if (IntervenePvE.CanUse(out act))
        {
            return true;
        }

        return base.MoveForwardAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.DivineVeilPvE, ActionID.PassageOfArmsPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (PassageProtec && StatusHelper.PlayerHasStatus(true, StatusID.PassageOfArms))
        {
            return false;
        }

        if (DivineVeilPvE.CanUse(out act))
        {
            return true;
        }

        if (PassageOfArmsPvE.CanUse(out act))
        {
            return true;
        }

        return base.DefenseAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.SentinelPvE, ActionID.RampartPvE, ActionID.BulwarkPvE, ActionID.SheltronPvE, ActionID.ReprisalPvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (PassageProtec && StatusHelper.PlayerHasStatus(true, StatusID.PassageOfArms))
        {
            return false;
        }

        if (InterventionTank && InterventionPvE.CanUse(out act))
        {
            return true;
        }

        // If the player has the Hallowed Ground status, don't use any abilities.
        if (!StatusHelper.PlayerHasStatus(true, StatusID.HallowedGround))
        {
            // If Bulwark can be used, use it and return true.
            if (BulwarkPvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }

            // If Oath can be used, use it and return true.
            if (UseOath(out act))
            {
                return true;
            }

            // If Rampart is not cooling down or has been cooling down for more than 60 seconds, and Sentinel can be used, use Sentinel and return true.
            if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && GuardianPvE.CanUse(out act) && GuardianPvE.EnoughLevel)
            {
                return true;
            }

            if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && SentinelPvE.CanUse(out act) && !GuardianPvE.EnoughLevel)
            {
                return true;
            }

            // If Sentinel is at an enough level and is cooling down for more than 60 seconds, or if Sentinel is not at an enough level, and Rampart can be used, use Rampart and return true.
            if (((GuardianPvE.EnoughLevel && GuardianPvE.Cooldown.IsCoolingDown && GuardianPvE.Cooldown.ElapsedAfter(60)) || (!GuardianPvE.EnoughLevel && SentinelPvE.EnoughLevel && SentinelPvE.Cooldown.IsCoolingDown && SentinelPvE.Cooldown.ElapsedAfter(60)) || !SentinelPvE.EnoughLevel) && RampartPvE.CanUse(out act))
            {
                return true;
            }

            // If Reprisal can be used, use it and return true.
            if (ReprisalPvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }
        }
        return base.DefenseSingleAbility(nextGCD, out act);
    }
    #endregion

    #region oGCD Logic

    [RotationDesc(ActionID.SheltronPvE, ActionID.HolySheltronPvE)]
    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (PassageProtec && StatusHelper.PlayerHasStatus(true, StatusID.PassageOfArms))
        {
            return false;
        }

        if (InCombat && OathGauge >= WhenToSheltron && WhenToSheltron > 0 && UseOath(out act))
        {
            return true;
        }

        if (HasFightOrFlight && InCombat && UseBurstMedicine(out act))
        {
            return true;
        }

        return base.GeneralAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.IntervenePvE, ActionID.SpiritsWithinPvE, ActionID.ExpiacionPvE, ActionID.CircleOfScornPvE, ActionID.RequiescatPvE, ActionID.ImperatorPvE, ActionID.FightOrFlightPvE)]
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (PassageProtec && StatusHelper.PlayerHasStatus(true, StatusID.PassageOfArms))
        {
            return false;
        }

        if (BladeOfHonorPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        if (CircleOfScornPvE.CanUse(out act, skipAoeCheck: true, skipTTKCheck: true) && FightOrFlightPvE.Cooldown.IsCoolingDown && (ImperatorPvE.EnoughLevel && ImperatorPvE.Cooldown.IsCoolingDown || !ImperatorPvE.EnoughLevel))
        {
            return true;
        }

        if (ExpiacionPvE.EnoughLevel && ExpiacionPvE.CanUse(out act, skipAoeCheck: true) && FightOrFlightPvE.Cooldown.IsCoolingDown && (ImperatorPvE.EnoughLevel && ImperatorPvE.Cooldown.IsCoolingDown || !ImperatorPvE.EnoughLevel))
        {
            return true;
        }

        if (!ExpiacionPvE.EnoughLevel && SpiritsWithinPvE.CanUse(out act, skipAoeCheck: true) && FightOrFlightPvE.Cooldown.IsCoolingDown && (ImperatorPvE.EnoughLevel && ImperatorPvE.Cooldown.IsCoolingDown || !ImperatorPvE.EnoughLevel))
        {
            return true;
        }

        if (!IsMoving && IntervenePvE.CanUse(out act, usedUp: UseInterveneFight && HasFightOrFlight))
        {
            return true;
        }

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic

    [RotationDesc(ActionID.ClemencyPvE)]
    protected override bool HealSingleGCD(out IAction? act)
    {
        if (PassageProtec && StatusHelper.PlayerHasStatus(true, StatusID.PassageOfArms))
        {
            return base.HealSingleGCD(out act);
        }

        if (RequiescatHealBot && RequiescatStacks > 0 && ClemencyPvE.CanUse(out act, skipCastingCheck: true) && ClemencyPvE.Target.Target?.GetHealthRatio() < ClemencyRequi)
        {
            return true;
        }

        if (HealBot && ClemencyPvE.CanUse(out act) && ClemencyPvE.Target.Target?.GetHealthRatio() < ClemencyNoRequi)
        {
            return true;
        }

        return base.HealSingleGCD(out act);
    }

    [RotationDesc(ActionID.ShieldBashPvE)]
    protected override bool MyInterruptGCD(out IAction? act)
    {
        act = null;
        if (PassageProtec && StatusHelper.PlayerHasStatus(true, StatusID.PassageOfArms))
        {
            return false;
        }

        if (LowBlowPvE.Cooldown.IsCoolingDown && ShieldBashPvE.CanUse(out act))
        {
            return true;
        }

        return base.MyInterruptGCD(out act);
    }

    protected override bool GeneralGCD(out IAction? act)
    {
        act = null;
        if (PassageProtec && StatusHelper.PlayerHasStatus(true, StatusID.PassageOfArms))
        {
            return false;
        }

        // Confiteor Combo
        if (ConfiteorPvE.CanUse(out act, usedUp: true, skipAoeCheck: true))
        {
            return true;
        }

        if (GoringBladePvE.CanUse(out act))
        {
            return true;
        }

        // Atonement Combo
        if ((!FightOrFlightPvE.Cooldown.WillHaveOneCharge(1) || HasFightOrFlight || StatusHelper.PlayerWillStatusEndGCD(1, 0, true, StatusID.AtonementReady)) && AtonementPvE.CanUse(out act))
        {
            return true;
        }

        if (((!HasAtonementReady && (SepulchreReady || SupplicationReady || HasDivineMight)) ||
             (HasAtonementReady && !HasDivineMight)) &&
            !StatusHelper.PlayerHasStatus(true, StatusID.Medicated) && !HasFightOrFlight && !RageOfHalonePvE.CanUse(out _, skipComboCheck: false))
        {
            if (!TotalEclipsePvE.CanUse(out _) && (RiotBladePvE.CanUse(out act) || FastBladePvE.CanUse(out act)))
            {
                return true;
            }
        }
        if ((RageOfHalonePvE.CanUse(out _, skipComboCheck: false) || HasFightOrFlight || StatusHelper.PlayerWillStatusEndGCD(1, 0, true, StatusID.SupplicationReady)) && SupplicationPvE.CanUse(out act))
        {
            return true;
        }

        if (RequiescatStacks > 0 && IsLastGCD(true, SupplicationPvE) && !HasFightOrFlight && HolySpiritPvE.CanUse(out act, skipCastingCheck: true))
        {
            return true;
        }

        if ((RageOfHalonePvE.CanUse(out _, skipComboCheck: false) || HasFightOrFlight || StatusHelper.PlayerWillStatusEndGCD(1, 0, true, StatusID.SepulchreReady)) && SepulchrePvE.CanUse(out act))
        {
            return true;
        }

        //AoE
        if ((HasDivineMight || RequiescatStacks > 0) && HolyCirclePvE.CanUse(out act, skipCastingCheck: true))
        {
            return true;
        }

        if (ProminencePvE.CanUse(out act, skipStatusProvideCheck: !EnhancedProminenceTrait.EnoughLevel))
        {
            return true;
        }

        if (TotalEclipsePvE.CanUse(out act))
        {
            return true;
        }

        //Single Target
        if ((HasDivineMight || RequiescatStacks > 0) && HolySpiritPvE.CanUse(out act, skipCastingCheck: true))
        {
            return true;
        }

        if (!SupplicationReady && !SepulchreReady && !HasAtonementReady && !HasDivineMight && RoyalAuthorityPvE.CanUse(out act))
        {
            return true;
        }

        if (RoyalAuthorityPvE.CanUse(out act))
        {
            return true;
        }
        if (!RoyalAuthorityPvE.Info.EnoughLevelAndQuest() && RageOfHalonePvE.CanUse(out act))
        {
            return true;
        }

        if (RiotBladePvE.CanUse(out act))
        {
            return true;
        }

        if (FastBladePvE.CanUse(out act))
        {
            return true;
        }

        //Ranged
        if (UseHolyWhenAway && StopMovingTime > 1 && HolySpiritPvE.CanUse(out act))
        {
            return true;
        }

        if (ShieldLobPvE.CanUse(out act))
        {
            return true;
        }

        return base.GeneralGCD(out act);
    }

    #endregion

    #region Extra Methods

    private bool UseOath(out IAction? act)
    {
        if ((InterventionPvE.Target.Target?.GetHealthRatio() <= InterventionRatio) && InterventionPvE.CanUse(out act))
        {
            return true;
        }

        if (HolySheltronPvE.CanUse(out act) && HolySheltronPvE.EnoughLevel)
        {
            return true;
        }

        if (SheltronPvE.CanUse(out act) && !HolySheltronPvE.EnoughLevel)
        {
            return true;
        }

        return false;
    }

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

            return base.CanHealSingleSpell && (GCDHeal || aliveHealerCount == 0);
        }
    }
    #endregion
}
