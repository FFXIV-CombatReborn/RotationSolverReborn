using Dalamud.Interface.Colors;

namespace RotationSolver.Basic.Rotations.Basic;

public partial class MachinistRotation
{
    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Dexterity;

    #region Job Gauge
    /// <summary>
    /// Gets a value indicating whether the player is currently Overheated.
    /// </summary>
    public static bool IsOverheated => JobGauge.IsOverheated;

    /// <summary>
    /// Gets a value indicating whether the player has an active Robot.
    /// </summary>
    public static bool IsRobotActive => JobGauge.IsRobotActive;

    /// <summary>
    /// Gets the current Heat level.
    /// </summary>
    public static byte Heat => JobGauge.Heat;

    /// <summary>
    /// Gets the current Overheated Stacks.
    /// </summary>
    public static byte OverheatedStacks
    {
        get
        {
            byte stacks = StatusHelper.PlayerStatusStack(true, StatusID.Overheated);
            return stacks == byte.MaxValue ? (byte)5 : stacks;
        }
    }

    /// <summary>
    /// Gets the current Battery level.
    /// </summary>
    public static byte Battery => JobGauge.Battery;

    /// <summary>
    /// Gets the battery level of the last summon (robot).
    /// </summary>
    public static byte LastSummonBatteryPower => JobGauge.LastSummonBatteryPower;

    private static float OverheatTimeRemainingRaw => JobGauge.OverheatTimeRemaining / 1000f;

    private static float SummonTimeRemainingRaw => JobGauge.SummonTimeRemaining / 1000f;

    /// <summary>
    /// Gets the time remaining for Overheat in seconds minus the DefaultGCDRemain.
    /// </summary>
    public static float OverheatTime => OverheatTimeRemainingRaw - DataCenter.DefaultGCDRemain;

    /// <summary>
    /// Gets the time remaining for the Rook or Queen in seconds minus the DefaultGCDRemain.
    /// </summary>
    public static float SummonTime => SummonTimeRemainingRaw - DataCenter.DefaultGCDRemain;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    protected static bool OverheatedEndAfter(float time)
    {
        return OverheatTime <= time;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gctCount"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    protected static bool OverheatedEndAfterGCD(uint gctCount = 0, float offset = 0)
    {
        return OverheatedEndAfter(GCDTime(gctCount, offset));
    }
    #endregion

    #region Status Tracking
    /// <summary>
    /// 
    /// </summary>
    public static bool HasWildfire => StatusHelper.PlayerHasStatus(true, StatusID.Wildfire_1946);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasHypercharged => StatusHelper.PlayerHasStatus(true, StatusID.Hypercharged);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasReassembled => StatusHelper.PlayerHasStatus(true, StatusID.Reassembled);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasOverheated => StatusHelper.PlayerHasStatus(true, StatusID.Overheated);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasExcavatorReady => StatusHelper.PlayerHasStatus(true, StatusID.ExcavatorReady);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasFullMetalMachinist => StatusHelper.PlayerHasStatus(true, StatusID.FullMetalMachinist);
    #endregion

    #region PvE Actions Unassignable

    /// <summary>
    /// 
    /// </summary>
    public static bool DetonatorPvEReady => Service.GetAdjustedActionId(ActionID.WildfirePvE) == ActionID.DetonatorPvE;
    /// <summary>
    /// 
    /// </summary>
    public static bool ExcavatorPvEReady => Service.GetAdjustedActionId(ActionID.ChainSawPvE) == ActionID.ExcavatorPvE;
    #endregion

    #region Debug Display
    /// <inheritdoc/>
    public override void DisplayBaseStatus()
    {
        ImGui.Text("IsOverheated: " + IsOverheated.ToString());
        ImGui.Text("IsRobotActive: " + IsRobotActive.ToString());
        ImGui.Text("Heat: " + Heat.ToString());
        ImGui.Text("Battery: " + Battery.ToString());
        ImGui.Text("LastSummonBatteryPower: " + LastSummonBatteryPower.ToString());
        ImGui.Text("SummonTimeRemainingRaw: " + SummonTimeRemainingRaw.ToString());
        ImGui.Text("SummonTime: " + SummonTime.ToString());
        ImGui.Text("OverheatTimeRemainingRaw: " + OverheatTimeRemainingRaw.ToString());
        ImGui.Text("OverheatTime: " + OverheatTime.ToString());
        ImGui.Text("OverheatedStacks: " + OverheatedStacks.ToString());
        ImGui.TextColored(ImGuiColors.DalamudViolet, "PvE Actions");
        ImGui.Text("DetonatorPvEReady: " + DetonatorPvEReady.ToString());
        ImGui.Text("ExcavatorPvEReady: " + ExcavatorPvEReady.ToString());
        ImGui.Spacing();
    }
    #endregion

    #region PvE
    static partial void ModifySplitShotPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifySlugShotPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.HeatedSplitShotPvE, ActionID.SplitShotPvE];
    }

    static partial void ModifyHotShotPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyReassemblePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Reassembled];
        setting.ActionCheck = () => HasHostilesInRange && !HasReassembled && !DataCenter.IsExtremeCastingSpecialIndicator();
    }

    static partial void ModifyGaussRoundPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySpreadShotPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyCleanShotPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.SlugShotPvE, ActionID.HeatedSlugShotPvE];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyHyperchargePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !IsOverheated && (Heat >= 50 || HasHypercharged);
        setting.StatusProvide = [StatusID.Overheated];
        setting.UnlockedByQuestID = 67233;
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 10,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifyHeatBlastPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => IsOverheated && !OverheatedEndAfterGCD();
        setting.UnlockedByQuestID = 67234;
    }

    static partial void ModifyRookAutoturretPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Battery >= 50 && !JobGauge.IsRobotActive;
        setting.UnlockedByQuestID = 67235;
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 16,
        };
    }

    static partial void ModifyRookOverdrivePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => JobGauge.IsRobotActive;
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 16,
        };
    }

    static partial void ModifyRookOverloadPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 16,
        };
    }

    static partial void ModifyWildfirePvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Wildfire];
        setting.StatusProvide = [StatusID.Wildfire_1946];
        setting.ActionCheck = () => Heat >= 50 || HasHypercharged || OverheatedStacks == 5;
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 10,
        };
    }

    static partial void ModifyDetonatorPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => DetonatorPvEReady;
    }

    static partial void ModifyRicochetPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67240;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyAutoCrossbowPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67242;
        setting.ActionCheck = () => IsOverheated && !OverheatedEndAfterGCD();
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyHeatedSplitShotPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67243;
    }

    static partial void ModifyTacticianPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67244;
        setting.StatusProvide = StatusHelper.RangePhysicalDefense;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyDrillPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67246;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyHeatedSlugShotPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.HeatedSplitShotPvE];
        setting.UnlockedByQuestID = 67248;
    }

    static partial void ModifyDismantlePvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Dismantled];
        // Pretty sure this will work as intended, but commented out cause I want a 2nd opinion ~ Kirbo
        //setting.ActionCheck = () => CurrentTarget != null && !CurrentTarget.HasStatus(false, StatusID.Dismantled);
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyHeatedCleanShotPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.HeatedSlugShotPvE];
    }

    static partial void ModifyBarrelStabilizerPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Hypercharged, StatusID.FullMetalMachinist];
        setting.ActionCheck = () => InCombat && !HasFullMetalMachinist;
        setting.IsFriendly = true;
    }

    static partial void ModifyBlazingShotPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => IsOverheated && !OverheatedEndAfterGCD();
    }

    static partial void ModifyFlamethrowerPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !IsMoving;
        setting.UnlockedByQuestID = 68445;
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 6,
        };
    }

    static partial void ModifyBioblasterPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Bioblaster, StatusID.Bioblaster_2019];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyAirAnchorPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyAutomatonQueenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Battery >= 50 && !JobGauge.IsRobotActive;
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 16,
        };
    }

    static partial void ModifyQueenOverdrivePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => JobGauge.IsRobotActive;
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 16,
        };
    }

    static partial void ModifyArmPunchPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 16,
        };
    }

    static partial void ModifyRollerDashPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 16,
        };
    }

    static partial void ModifyPileBunkerPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 16,
        };
    }

    static partial void ModifyScattergunPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyCrownedColliderPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 16,
        };
    }

    static partial void ModifyChainSawPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.ExcavatorReady];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyDoubleCheckPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyCheckmatePvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyExcavatorPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ExcavatorPvEReady || HasExcavatorReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFullMetalFieldPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !HasReassembled && HasFullMetalMachinist;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion

    #region PvP

    static partial void ModifyAnalysisPvP(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
        setting.StatusProvide = [StatusID.Analysis];
    }

    static partial void ModifyDrillPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.DrillPrimed];
    }

    static partial void ModifyBioblasterPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.BioblasterPrimed];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyAirAnchorPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.AirAnchorPrimed];
    }

    static partial void ModifyChainSawPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.ChainSawPrimed];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBishopAutoturretPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFullMetalFieldPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyAetherMortarPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            IsEnabled = false,
        };
    }
    #endregion
}
