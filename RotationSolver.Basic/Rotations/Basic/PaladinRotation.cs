namespace RotationSolver.Basic.Rotations.Basic;

public partial class PaladinRotation
{
    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Strength;

    /// <summary>
    /// 
    /// </summary>
    public override bool CanHealAreaAbility => false;

    /// <inheritdoc/>
    public override bool IsBursting()
    {
        if (Player.HasStatus(true, StatusID.FightOrFlight) || FightOrFlightPvE.Cooldown.RecastTimeRemainOneCharge > 15f)
        {
            return true; // Either have Fight or Flight or more than 15 seconds until we can use it
        }
        return false;
    }

    /// <summary>
    /// Holds the remaining amount of Requiescat stacks
    /// </summary>
    public static byte RequiescatStacks
    {
        get
        {
            byte stacks = Player.StatusStack(true, StatusID.Requiescat);
            return stacks == byte.MaxValue ? (byte)5 : stacks;
        }
    }

    #region Job Gauge
    /// <summary>
    /// Gets the current level of the Oath gauge.
    /// </summary>
    public static byte OathGauge => JobGauge.OathGauge;
    #endregion

    #region Status Tracking

    /// <summary>
    /// 
    /// </summary>
    public static bool HasDivineMight => Player.HasStatus(true, StatusID.DivineMight);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasFightOrFlight => Player.HasStatus(true, StatusID.FightOrFlight);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasConfiteorReady => Player.HasStatus(true, StatusID.ConfiteorReady);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasAtonementReady => Player.HasStatus(true, StatusID.AtonementReady);
    #endregion

    #region Actions Unassignable

    /// <summary>
    /// 
    /// </summary>
    public static bool SupplicationReady => Service.GetAdjustedActionId(ActionID.AtonementPvE) == ActionID.SupplicationPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool SepulchreReady => Service.GetAdjustedActionId(ActionID.AtonementPvE) == ActionID.SepulchrePvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool BladeOfFaithReady => Service.GetAdjustedActionId(ActionID.ConfiteorPvE) == ActionID.BladeOfFaithPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool BladeOfTruthReady => Service.GetAdjustedActionId(ActionID.ConfiteorPvE) == ActionID.BladeOfTruthPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool BladeOfValorReady => Service.GetAdjustedActionId(ActionID.ConfiteorPvE) == ActionID.BladeOfValorPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool BladeOfHonorReady => Service.GetAdjustedActionId(ActionID.ImperatorPvE) == ActionID.BladeOfHonorPvE;
    #endregion

    #region Debug
    /// <inheritdoc/>
    public override void DisplayBaseStatus()
    {
        ImGui.Text("RequiescatStacks: " + RequiescatStacks.ToString());
        ImGui.Text("OathGauge: " + OathGauge.ToString());
        ImGui.Text("HasDivineMight: " + HasDivineMight.ToString());
        ImGui.Text("HasFightOrFlight: " + HasFightOrFlight.ToString());
        ImGui.Text("Can Heal Area Ability: " + CanHealAreaAbility.ToString());
        ImGui.Text("Can Heal Single Spell: " + CanHealSingleSpell.ToString());
        ImGui.Spacing();
        ImGui.Text("HasConfiteorReady: " + HasConfiteorReady.ToString());
        ImGui.Text("BladeOfFaithReady: " + BladeOfFaithReady.ToString());
        ImGui.Text("BladeOfTruthReady: " + BladeOfTruthReady.ToString());
        ImGui.Text("BladeOfValorReady: " + BladeOfValorReady.ToString());
        ImGui.Text("BladeOfHonorReady: " + BladeOfHonorReady.ToString());
        ImGui.Spacing();
        ImGui.Text("HasAtonementReady: " + HasAtonementReady.ToString());
        ImGui.Text("SupplicationReady: " + SupplicationReady.ToString());
        ImGui.Text("SepulchreReady: " + SepulchreReady.ToString());
    }

    #endregion

    private protected sealed override IBaseAction TankStance => IronWillPvE;
    #region PvE

    static partial void ModifyIronWillPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
    }

    static partial void ModifyReleaseIronWillPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
    }

    static partial void ModifyFastBladePvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyFightOrFlightPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.GoringBladeReady];
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 0,
        };
        setting.IsFriendly = false;
    }

    static partial void ModifyRiotBladePvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.FastBladePvE];
    }

    static partial void ModifyTotalEclipsePvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyShieldBashPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Stun];
        setting.TargetType = TargetType.Interrupt;
        //setting.CanTarget = o =>
        //{
        //    if (o is not IBattleChara b) return false;

        //    if (b.IsBossFromIcon() || IsMoving || b.CastActionId == 0) return false;

        //    if (!b.IsCastInterruptible || ActionID.InterjectPvE.IsCoolingDown()) return true;
        //    return false;
        //};
    }

    static partial void ModifyShieldLobPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MeleeRange;
        setting.UnlockedByQuestID = 65798;
    }

    static partial void ModifyRageOfHalonePvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.RiotBladePvE];
    }

    static partial void ModifySpiritsWithinPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 66591;
    }

    static partial void ModifySheltronPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => OathGauge >= 50;
        setting.StatusProvide = [StatusID.Sheltron];
        setting.UnlockedByQuestID = 66592;
        setting.TargetType = TargetType.Self;
    }

    static partial void ModifySentinelPvE(ref ActionSetting setting)
    {
        setting.TargetType = TargetType.Self;
        setting.IsFriendly = true;
    }

    static partial void ModifyProminencePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.DivineMight];
        setting.ComboIds = [ActionID.TotalEclipsePvE];
        setting.UnlockedByQuestID = 66593;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyCoverPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Cover];
        setting.TargetStatusProvide = [StatusID.Covered];
        setting.ActionCheck = () => OathGauge >= 50;
        setting.UnlockedByQuestID = 66595;
        setting.TargetType = TargetType.BeAttacked;
        setting.IsFriendly = true;
    }

    static partial void ModifyCircleOfScornPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.CircleOfScorn];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
            TimeToKill = 0,
        };
    }

    static partial void ModifyHallowedGroundPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.HallowedGround];
        setting.UnlockedByQuestID = 66596;
        setting.ActionCheck = () => InCombat;
        setting.IsFriendly = true;
    }

    static partial void ModifyBulwarkPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Bulwark];
        setting.IsFriendly = true;
    }

    static partial void ModifyGoringBladePvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67570;
        setting.StatusNeed = [StatusID.GoringBladeReady];
    }

    static partial void ModifyDivineVeilPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67571;
        setting.StatusProvide = [StatusID.DivineVeil_1362];
        setting.IsFriendly = true;
    }

    static partial void ModifyClemencyPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67572;
        //setting.CanTarget = t =>
        //{
        //    return !t.HasStatus(false, StatusHelper.TankStanceStatus);
        //};
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            GCDSingleHeal = true,
        };
    }

    static partial void ModifyRoyalAuthorityPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.DivineMight, StatusID.AtonementReady];
        setting.ComboIds = [ActionID.RiotBladePvE];
        setting.UnlockedByQuestID = 67573;
    }

    static partial void ModifyInterventionPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => OathGauge >= 50;
        setting.TargetStatusNeed = [StatusID.Grit, StatusID.RoyalGuard_1833, StatusID.IronWill, StatusID.Defiance];
        setting.TargetStatusProvide = [StatusID.KnightsResolve, StatusID.KnightsBenediction, StatusID.Intervention];
    }

    static partial void ModifyHolySpiritPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyRequiescatPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.ConfiteorReady];
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 0,
        };
    }

    static partial void ModifyPassageOfArmsPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.PassageOfArms];
        setting.UnlockedByQuestID = 68111;
        setting.IsFriendly = true;
    }

    static partial void ModifyHolyCirclePvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyIntervenePvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.HostileMovingForward;
    }

    static partial void ModifyAtonementPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasAtonementReady;
        setting.StatusProvide = [StatusID.SupplicationReady];
    }

    static partial void ModifySepulchrePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SepulchreReady;
    }

    static partial void ModifyConfiteorPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasConfiteorReady || BladeOfFaithReady || BladeOfTruthReady || BladeOfValorReady || IsLastAction(ActionID.ImperatorPvE) || (IsLastAction(ActionID.RequiescatPvE) && RequiescatMasteryTrait.EnoughLevel);
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyHolySheltronPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => OathGauge >= 50;
        setting.StatusProvide = [StatusID.HolySheltron];
        setting.UnlockedByQuestID = 66592;
        setting.TargetType = TargetType.Self;
        setting.IsFriendly = true;
    }

    static partial void ModifyExpiacionPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBladeOfFaithPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BladeOfFaithReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBladeOfTruthPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BladeOfTruthReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBladeOfValorPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BladeOfValorReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyGuardianPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = StatusHelper.RampartStatus;
        setting.TargetType = TargetType.Self;
        setting.IsFriendly = true;
    }

    static partial void ModifyImperatorPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.ConfiteorReady, StatusID.Requiescat];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    static partial void ModifyBladeOfHonorPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BladeOfHonorReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    // Actions Unassignable

    static partial void ModifySupplicationPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SupplicationReady;
        setting.StatusProvide = [StatusID.SepulchreReady];
    }
    #endregion

    #region PvP
    /// <summary>
    /// 
    /// </summary>
    public static bool BladeOfTruthPvPReady => Service.GetAdjustedActionId(ActionID.BladeOfFaithPvP) == ActionID.BladeOfTruthPvP;

    /// <summary>
    /// 
    /// </summary>
    public static bool BladeOfValorPvPReady => Service.GetAdjustedActionId(ActionID.BladeOfFaithPvP) == ActionID.BladeOfValorPvP;

    /// <summary>
    /// 
    /// </summary>
    public static bool ConfiteorPvPReady => Service.GetAdjustedActionId(ActionID.ImperatorPvP) == ActionID.ConfiteorPvP;
    static partial void ModifyFastBladePvP(ref ActionSetting setting)
    {

    }

    static partial void ModifyRiotBladePvP(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.FastBladePvP];
    }

    static partial void ModifyRoyalAuthorityPvP(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.RiotBladePvP];
    }

    static partial void ModifyAtonementPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.AtonementReady_2015];
        setting.MPOverride = () => 0;
    }

    static partial void ModifySupplicationPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.SupplicationReady_4281];
        setting.MPOverride = () => 0;
    }

    static partial void ModifySepulchrePvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.SepulchreReady_4282];
        setting.MPOverride = () => 0;
    }

    static partial void ModifyHolySpiritPvP(ref ActionSetting setting)
    {

    }

    static partial void ModifyShieldSmitePvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1
        };
    }

    static partial void ModifyImperatorPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1
        };
    }

    static partial void ModifyHolySheltronPvP(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
    }

    static partial void ModifyConfiteorPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ConfiteorPvPReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1
        };
    }

    static partial void ModifyIntervenePvP(ref ActionSetting setting)
    {
        //setting.SpecialType = SpecialActionType.MovingForward;
    }

    static partial void ModifyBladeOfFaithPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.BladeOfFaithReady];
        setting.MPOverride = () => 0;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1
        };
    }

    static partial void ModifyBladeOfTruthPvP(ref ActionSetting setting)
    {
        // setting.ActionCheck = () => BladeOfTruthPvPReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1
        };
    }

    static partial void ModifyBladeOfValorPvP(ref ActionSetting setting)
    {
        // setting.ActionCheck = () => BladeOfValorPvPReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1
        };
    }
    #endregion
}
