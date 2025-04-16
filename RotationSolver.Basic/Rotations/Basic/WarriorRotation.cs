using Dalamud.Interface.Colors;

namespace RotationSolver.Basic.Rotations.Basic;

partial class WarriorRotation
{
    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Strength;

    #region Job Gauge
    /// <summary>
    /// 
    /// </summary>
    public static byte BeastGauge => JobGauge.BeastGauge;

    /// <summary>
    /// 
    /// </summary>
    public static byte OnslaughtMax => EnhancedOnslaughtTrait.EnoughLevel ? (byte)3 : (byte)2;

    /// <summary>
    /// Holds the remaining amount of InnerRelease stacks
    /// </summary>
    public static byte InnerReleaseStacks
    {
        get
        {
            byte stacks = Player.StatusStack(true, StatusID.InnerRelease);
            return stacks == byte.MaxValue ? (byte)3 : stacks;
        }
    }

    /// <summary>
    /// Holds the remaining amount of Berserk stacks
    /// </summary>
    public static byte BerserkStacks
    {
        get
        {
            byte stacks = Player.StatusStack(true, StatusID.Berserk);
            return stacks == byte.MaxValue ? (byte)3 : stacks;
        }
    }
    #endregion

    #region Actions Unassignable

    /// <summary>
    /// 
    /// </summary>
    public static bool ChaoticCyclonePvEReady => Service.GetAdjustedActionId(ActionID.DecimatePvE) == ActionID.ChaoticCyclonePvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool InnerChaosPvEeady => Service.GetAdjustedActionId(ActionID.FellCleavePvE) == ActionID.InnerChaosPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool PrimalWrathPvEReady => Service.GetAdjustedActionId(ActionID.InnerReleasePvE) == ActionID.PrimalWrathPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool PrimalRuinationPvEReady => Service.GetAdjustedActionId(ActionID.PrimalRendPvE) == ActionID.PrimalRuinationPvE;
    #endregion

    #region Debug
    /// <inheritdoc/>
    public override void DisplayStatus()
    {
        ImGui.Text("InnerReleaseStacks: " + InnerReleaseStacks.ToString());
        ImGui.Text("BerserkStacks: " + BerserkStacks.ToString());
        ImGui.Text("BeastGaugeValue: " + BeastGauge.ToString());
        ImGui.Text("OnslaughtMax: " + OnslaughtMax.ToString());
        ImGui.TextColored(ImGuiColors.DalamudViolet, "PvE Actions");
        ImGui.Text("ChaoticCyclonePvEReady: " + ChaoticCyclonePvEReady.ToString());
        ImGui.Text("InnerChaosPvEeady: " + InnerChaosPvEeady.ToString());
        ImGui.Text("PrimalWrathPvEReady: " + PrimalWrathPvEReady.ToString());
        ImGui.Text("PrimalRuinationPvEReady: " + PrimalRuinationPvEReady.ToString());
    }
    #endregion

    #region PvE Actions
    private sealed protected override IBaseAction TankStance => DefiancePvE;

    static partial void ModifyHeavySwingPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyMaimPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.HeavySwingPvE];
    }

    static partial void ModifyBerserkPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasHostilesInRange && !ActionID.InnerReleasePvE.IsCoolingDown();
        setting.StatusProvide = [StatusID.Berserk];
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 0,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifyOverpowerPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyDefiancePvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
    }

    static partial void ModifyReleaseDefiancePvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
    }

    static partial void ModifyTomahawkPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MeleeRange;
        setting.UnlockedByQuestID = 65852;
    }

    static partial void ModifyStormsPathPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BeastGauge <= 80;
        setting.ComboIds = [ActionID.MaimPvE];
    }

    static partial void ModifyThrillOfBattlePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.ThrillOfBattle];
        setting.UnlockedByQuestID = 65855;
        setting.IsFriendly = true;
    }

    static partial void ModifyInnerBeastPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BeastGauge >= 50;
        setting.UnlockedByQuestID = 66586;
    }

    static partial void ModifyVengeancePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = StatusHelper.RampartStatus;
        setting.ActionCheck = Player.IsTargetOnSelf;
        setting.IsFriendly = true;
    }

    static partial void ModifyMythrilTempestPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.OverpowerPvE];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 2,
        };
        setting.UnlockedByQuestID = 66587;

    }

    static partial void ModifyHolmgangPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Holmgang_409];
        setting.TargetType = TargetType.Self;
        setting.ActionCheck = () => InCombat;
        setting.IsFriendly = true;
    }

    static partial void ModifySteelCyclonePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BeastGauge >= 50;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 2,
        };
        setting.UnlockedByQuestID = 66589;
    }

    static partial void ModifyStormsEyePvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.MaimPvE];
        setting.StatusProvide = [StatusID.SurgingTempest];
        setting.CreateConfig = () => new ActionConfig()
        {
            StatusGcdCount = 9,
        };
    }

    static partial void ModifyInfuriatePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.NascentChaos];
        setting.ActionCheck = () => HasHostilesInRange && BeastGauge <= 50 && InCombat;
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 0,
        };
        setting.UnlockedByQuestID = 66590;
        setting.IsFriendly = true;
    }

    static partial void ModifyFellCleavePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BeastGauge >= 50 || InnerReleaseStacks > 0;
        setting.UnlockedByQuestID = 66124;
        setting.StatusProvide = [StatusID.BurgeoningFury];
    }

    static partial void ModifyRawIntuitionPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = Player.IsTargetOnSelf;
        setting.StatusProvide = [StatusID.RawIntuition];
        setting.UnlockedByQuestID = 66132;
        setting.IsFriendly = true;
    }

    static partial void ModifyEquilibriumPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 66134;
        setting.ActionCheck = Player.IsTargetOnSelf;
        setting.StatusProvide = [StatusID.Equilibrium];
        setting.IsFriendly = true;
    }

    static partial void ModifyDecimatePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BeastGauge >= 50 || InnerReleaseStacks > 0;
        setting.UnlockedByQuestID = 66137;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyOnslaughtPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
    }

    static partial void ModifyUpheavalPvE(ref ActionSetting setting)
    {
        //TODO: Why is that status? Answer: This is Warrior's 10% damage buff. Don't want to cast Upheaval at the start of combat without the buff. 
        setting.StatusNeed = [StatusID.SurgingTempest];
    }

    static partial void ModifyShakeItOffPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.ShakeItOff, StatusID.ShakeItOffOverTime];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifyInnerReleasePvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 0,
        };
        setting.UnlockedByQuestID = 68440;
        setting.StatusProvide = [StatusID.InnerRelease, StatusID.PrimalRendReady, StatusID.InnerStrength];
        setting.IsFriendly = true;
    }

    static partial void ModifyNascentFlashPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.NascentFlash];
        setting.TargetStatusProvide = [StatusID.NascentGlint];
        setting.IsFriendly = true;
    }

    static partial void ModifyBloodwhettingPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.StemTheTide, StatusID.StemTheFlow];
        setting.IsFriendly = true;
    }

    static partial void ModifyOrogenyPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyPrimalRendPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.PrimalRendReady];
        setting.StatusProvide = [StatusID.PrimalRuinationReady];
        setting.SpecialType = SpecialActionType.MovingForward;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyDamnationPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.PrimevalImpulse];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion

    #region PvE Actions Unassignable

    static partial void ModifyChaoticCyclonePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BeastGauge >= 50 && InnerChaosPvEeady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyInnerChaosPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BeastGauge >= 50 && InnerChaosPvEeady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyPrimalWrathPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => PrimalWrathPvEReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyPrimalRuinationPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => PrimalRuinationPvEReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion

    #region PvP Actions
    // PvP
    static partial void ModifyInnerChaosPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.InnerChaosReady];
    }

    static partial void ModifyPrimalRendPvP(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyPrimalRuinationPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.PrimalRuinationReady_4285];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBlotaPvP(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
    }

    static partial void ModifyOnslaughtPvP(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
    }

    static partial void ModifyFellCleavePvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.InnerRelease_1303];
    }

    static partial void ModifyPrimalWrathPvP(ref ActionSetting setting)
    {
        setting.IsFriendly = false;
        setting.StatusNeed = [StatusID.Wrathful_4286];
    }

    static partial void ModifyBloodwhettingPvP(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
    }

    static partial void ModifyChaoticCyclonePvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.ChaoticCycloneReady];
    }
    #endregion

    /// <inheritdoc/>
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (HolmgangPvE.CanUse(out act)
            && Player.GetHealthRatio() <= Service.Config.HealthForDyingTanks) return true;
        return base.EmergencyAbility(nextGCD, out act);
    }

    /// <inheritdoc/>
    [RotationDesc(ActionID.OnslaughtPvE)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (OnslaughtPvE.CanUse(out act)) return true;
        return base.MoveForwardAbility(nextGCD, out act);
    }

    /// <inheritdoc/>
    [RotationDesc(ActionID.PrimalRendPvE)]
    protected override bool MoveForwardGCD(out IAction? act)
    {
        if (PrimalRendPvE.CanUse(out act, skipAoeCheck: true)) return true;
        return base.MoveForwardGCD(out act);
    }
}
