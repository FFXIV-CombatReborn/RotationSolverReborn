using Dalamud.Interface.Colors;

namespace RotationSolver.Basic.Rotations.Basic;

partial class DragoonRotation
{
    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Strength;

    #region Job Gauge
    /// <summary>
    /// 
    /// </summary>
    public static byte EyeCount => JobGauge.EyeCount;

    /// <summary>
    /// Firstminds Count
    /// </summary>
    public static byte FocusCount => JobGauge.FirstmindsFocusCount;

    /// <summary>
    /// 
    /// </summary>
    static float LOTDTimeRaw => JobGauge.LOTDTimer / 1000f;

    /// <summary>
    /// 
    /// </summary>
    public static float LOTDTime => LOTDTimeRaw - DataCenter.DefaultGCDRemain;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    protected static bool LOTDEndAfter(float time) => LOTDTime <= time;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gctCount"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    protected static bool LOTDEndAfterGCD(uint gctCount = 0, float offset = 0)
        => LOTDEndAfter(GCDTime(gctCount, offset));
    #endregion

    #region PvE Actions Unassignable

    /// <summary>
    /// 
    /// </summary>
    public static bool DrakesbanePvEFangReady => Service.GetAdjustedActionId(ActionID.FangAndClawPvE) == ActionID.DrakesbanePvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool DrakesbanePvEWheelingReady => Service.GetAdjustedActionId(ActionID.WheelingThrustPvE) == ActionID.DrakesbanePvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool RaidenThrustPvEReady => Service.GetAdjustedActionId(ActionID.TrueThrustPvE) == ActionID.RaidenThrustPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool DraconianFuryPvEReady => Service.GetAdjustedActionId(ActionID.DoomSpikePvE) == ActionID.DraconianFuryPvE;
    #endregion

    #region Draw Debug

    /// <inheritdoc/>
    public override void DisplayStatus()
    {
        ImGui.Text("EyeCount: " + EyeCount.ToString());
        ImGui.Text("FocusCount: " + FocusCount.ToString());
        ImGui.Text("LOTDTimeRaw: " + LOTDTimeRaw.ToString());
        ImGui.Text("LOTDTime: " + LOTDTime.ToString());
        ImGui.TextColored(ImGuiColors.DalamudViolet, "PvE Actions");
        ImGui.Text("DrakesbanePvEFangReady: " + DrakesbanePvEFangReady.ToString());
        ImGui.Text("DrakesbanePvEWheelingReady: " + DrakesbanePvEWheelingReady.ToString());
        ImGui.Text("RaidenThrustPvEReady: " + RaidenThrustPvEReady.ToString());
        ImGui.Text("DraconianFuryPvEReady: " + DraconianFuryPvEReady.ToString());
    }
    #endregion

    #region PvE Actions

    static partial void ModifyTrueThrustPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyVorpalThrustPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.TrueThrustPvE, ActionID.RaidenThrustPvE];
    }

    static partial void ModifyLifeSurgePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.LifeSurge];
        setting.ActionCheck = () => !IsLastAbility(ActionID.LifeSurgePvE);
        setting.IsFriendly = true;
    }

    static partial void ModifyPiercingTalonPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MeleeRange;
        setting.UnlockedByQuestID = 65591;
    }

    static partial void ModifyDisembowelPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.TrueThrustPvE, ActionID.RaidenThrustPvE];
        setting.StatusProvide = [StatusID.PowerSurge_2720];
        setting.CreateConfig = () => new ActionConfig()
        {
            StatusGcdCount = 2,
        };
    }

    static partial void ModifyFullThrustPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.VorpalThrustPvE, ActionID.LanceBarragePvE];
    }

    static partial void ModifyLanceChargePvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 10,
        };
        setting.StatusProvide = [StatusID.LanceCharge];
        setting.UnlockedByQuestID = 65975;
        setting.IsFriendly = true;
    }

    static partial void ModifyJumpPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.DiveReady];
        setting.UnlockedByQuestID = 66603;
    }

    static partial void ModifyElusiveJumpPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 66604;
        setting.StatusProvide = [StatusID.EnhancedPiercingTalon];
        setting.IsFriendly = true;
    }

    static partial void ModifyDoomSpikePvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 66605;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyWingedGlidePvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
        setting.UnlockedByQuestID = 66607;
    }

    static partial void ModifyChaosThrustPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.DisembowelPvE, ActionID.SpiralBlowPvE];
        setting.TargetStatusProvide = [StatusID.ChaosThrust, StatusID.ChaoticSpring];
        setting.CreateConfig = () => new ActionConfig()
        {
            StatusGcdCount = 3,
        };
    }

    //Class

    static partial void ModifyDragonfireDivePvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 66608;
        setting.StatusProvide = [StatusID.DragonsFlight];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBattleLitanyPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.BattleLitany];
        setting.UnlockedByQuestID = 67226;
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 10,
            AoeCount = 1,
        };
    }

    static partial void ModifyFangAndClawPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.HeavensThrustPvE];
        setting.UnlockedByQuestID = 67229;
    }


    static partial void ModifyWheelingThrustPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.ChaoticSpringPvE];
        setting.UnlockedByQuestID = 67230;
    }

    static partial void ModifyGeirskogulPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67231;
        setting.StatusProvide = [StatusID.NastrondReady, StatusID.LifeOfTheDragon];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySonicThrustPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.DraconianFuryPvE, ActionID.DoomSpikePvE];
        setting.StatusProvide = [StatusID.PowerSurge_2720];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyDrakesbanePvE(ref ActionSetting setting) //aka Kendrick Lamar
    {
        setting.ActionCheck = () => DrakesbanePvEFangReady || DrakesbanePvEWheelingReady;
        setting.ComboIds = [ActionID.WheelingThrustPvE, ActionID.FangAndClawPvE];
        setting.StatusProvide = [StatusID.DraconianFire];
        setting.CreateConfig = () => new ActionConfig()
        {
            StatusGcdCount = 5,
        };
    }

    static partial void ModifyMirageDivePvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.DiveReady];
    }

    static partial void ModifyNastrondPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.NastrondReady];
        setting.UnlockedByQuestID = 68450;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyCoerthanTormentPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.DraconianFire];
        setting.ComboIds = [ActionID.SonicThrustPvE];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyHighJumpPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.DiveReady];
    }

    static partial void ModifyRaidenThrustPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => RaidenThrustPvEReady;
    }

    static partial void ModifyStardiverPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => JobGauge.IsLOTDActive;
        setting.StatusProvide = [StatusID.StarcrossReady];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyDraconianFuryPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => DraconianFuryPvEReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyHeavensThrustPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.VorpalThrustPvE, ActionID.LanceBarragePvE];
    }

    static partial void ModifyChaoticSpringPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.SpiralBlowPvE];
        setting.TargetStatusProvide = [StatusID.ChaoticSpring];
        setting.CreateConfig = () => new ActionConfig()
        {
            StatusGcdCount = 3,
        };
    }

    static partial void ModifyWyrmwindThrustPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => FocusCount == 2;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyRiseOfTheDragonPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 66608;
        setting.StatusNeed = [StatusID.DragonsFlight];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyLanceBarragePvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.TrueThrustPvE, ActionID.RaidenThrustPvE];
    }

    static partial void ModifySpiralBlowPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.TrueThrustPvE, ActionID.RaidenThrustPvE];
        setting.StatusProvide = [StatusID.PowerSurge_2720];
        setting.CreateConfig = () => new ActionConfig()
        {
            StatusGcdCount = 3,
        };
    }

    static partial void ModifyStarcrossPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.StarcrossReady];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion

    #region PvP Actions

    static partial void ModifyRaidenThrustPvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyFangAndClawPvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyWheelingThrustPvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyDrakesbanePvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyChaoticSpringPvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyHorridRoarPvP(ref ActionSetting setting)
    {
        setting.IsFriendly = false;
    }

    static partial void ModifyHeavensThrustPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.RaidenThrustPvP) == ActionID.HeavensThrustPvP;
    }

    static partial void ModifyStarcrossPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.RaidenThrustPvP) == ActionID.StarcrossPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyGeirskogulPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyNastrondPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.GeirskogulPvP) == ActionID.NastrondPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyElusiveJumpPvP(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingBackward;
        setting.IsFriendly = true;
    }

    static partial void ModifyWyrmwindThrustPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.ElusiveJumpPvP) == ActionID.WyrmwindThrustPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyHighJumpPvP(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
    }
    #endregion


}
