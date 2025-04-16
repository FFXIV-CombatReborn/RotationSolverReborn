using Dalamud.Interface.Colors;

namespace RotationSolver.Basic.Rotations.Basic;

partial class DancerRotation
{
    /// <summary>
    /// 
    /// </summary>
    public override MedicineType MedicineType => MedicineType.Dexterity;

    #region Job Gauge
    /// <summary>
    /// 
    /// </summary>
    public static bool IsDancing => JobGauge.IsDancing;

    /// <summary>
    /// 
    /// </summary>
    public static byte Esprit => JobGauge.Esprit;

    /// <summary>
    /// 
    /// </summary>
    public static byte Feathers => JobGauge.Feathers;

    /// <summary>
    /// 
    /// </summary>
    public static byte CompletedSteps => JobGauge.CompletedSteps;
    #endregion

    #region PvE Actions Unassignable

    /// <summary>
    /// 
    /// </summary>
    public static bool StandardFinishPvEReady => Service.GetAdjustedActionId(ActionID.StandardStepPvE) == ActionID.StandardFinishPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool TechnicalFinishPvEReady => Service.GetAdjustedActionId(ActionID.TechnicalStepPvE) == ActionID.TechnicalFinishPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool ImprovisedFinishPvEReady => Service.GetAdjustedActionId(ActionID.ImprovisationPvE) == ActionID.ImprovisedFinishPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool TillanaPvEReady => Service.GetAdjustedActionId(ActionID.TechnicalStepPvE) == ActionID.TillanaPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool FinishingMovePvEReady => Service.GetAdjustedActionId(ActionID.StandardStepPvE) == ActionID.FinishingMovePvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool DanceOfTheDawnPvEReady => Service.GetAdjustedActionId(ActionID.SaberDancePvE) == ActionID.DanceOfTheDawnPvE;
    #endregion

    #region Debug Status

    /// <inheritdoc/>
    public override void DisplayStatus()
    {
        ImGui.Text("IsDancing: " + IsDancing.ToString());
        ImGui.Text("Esprit: " + Esprit.ToString());
        ImGui.Text("Feathers: " + Feathers.ToString());
        ImGui.Text("CompletedSteps: " + CompletedSteps.ToString());
        ImGui.TextColored(ImGuiColors.DalamudViolet, "PvE Actions");
        ImGui.Text("StandardFinishPvEReady: " + StandardFinishPvEReady.ToString());
        ImGui.Text("TechnicalFinishPvE: " + TechnicalFinishPvE.ToString());
        ImGui.Text("ImprovisedFinishPvEReady: " + ImprovisedFinishPvEReady.ToString());
        ImGui.Text("TillanaPvEReady: " + TillanaPvEReady.ToString());
        ImGui.Text("FinishingMovePvEReady: " + FinishingMovePvEReady.ToString());
        ImGui.Text("DanceOfTheDawnPvEReady: " + DanceOfTheDawnPvEReady.ToString());
    }
    #endregion

    #region PvE Actions
    static partial void ModifyCascadePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.SilkenSymmetry];
    }

    static partial void ModifyCuringWaltzPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifyShieldSambaPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = StatusHelper.RangePhysicalDefense;
        setting.StatusFromSelf = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyImprovisationPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Improvisation, StatusID.Improvisation_2695, StatusID.RisingRhythm];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyImprovisedFinishPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFountainPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.CascadePvE];
        setting.StatusProvide = [StatusID.SilkenFlow];
    }

    static partial void ModifyWindmillPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.SilkenSymmetry];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyReverseCascadePvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.SilkenSymmetry, StatusID.FlourishingSymmetry];
    }

    static partial void ModifyFountainfallPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.SilkenFlow, StatusID.FlourishingFlow];
    }

    static partial void ModifyFanDancePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Feathers > 0;
        setting.StatusProvide = [StatusID.ThreefoldFanDance];
    }

    static partial void ModifyBladeshowerPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.SilkenFlow];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyRisingWindmillPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.SilkenSymmetry, StatusID.FlourishingSymmetry];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyBloodshowerPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.SilkenFlow, StatusID.FlourishingFlow];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyFanDanceIiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Feathers > 0;
        setting.StatusProvide = [StatusID.ThreefoldFanDance];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyFanDanceIiiPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.ThreefoldFanDance];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFanDanceIvPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.FourfoldFanDance];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySaberDancePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Esprit >= 50;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyStarfallDancePvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.FlourishingStarfall];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyTillanaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Esprit <= 50 && TillanaPvEReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyEnAvantPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
    }

    static partial void ModifyClosedPositionPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
        setting.TargetType = TargetType.DancePartner;
        setting.ActionCheck = () => !IsDancing && !AllianceMembers.Any(b => b.HasStatus(true, StatusID.ClosedPosition_2026));
    }

    static partial void ModifyEndingPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
    }

    static partial void ModifyDevilmentPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 10,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifyFlourishPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.StandardFinish];
        setting.StatusProvide = [StatusID.ThreefoldFanDance, StatusID.FourfoldFanDance, StatusID.FinishingMoveReady];
        setting.ActionCheck = () => InCombat;
        setting.IsFriendly = true;
    }

    static partial void ModifyTechnicalStepPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.StandardFinish];
        setting.UnlockedByQuestID = 68790;
    }

    static partial void ModifyDoubleTechnicalFinishPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.StandardStep, StatusID.TechnicalStep, StatusID.DanceOfTheDawnReady];
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 20,
            AoeCount = 1,
        };
    }

    static partial void ModifyDoubleStandardFinishPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.StandardStep];
        setting.StatusProvide = [StatusID.LastDanceReady];
        setting.ActionCheck = () => IsDancing && CompletedSteps == 2 && Service.GetAdjustedActionId(ActionID.StandardStepPvE) == ActionID.DoubleStandardFinishPvE;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyQuadrupleTechnicalFinishPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.TechnicalStep];
        setting.StatusProvide = [StatusID.DanceOfTheDawnReady];
        setting.ActionCheck = () => IsDancing && CompletedSteps == 4 && Service.GetAdjustedActionId(ActionID.TechnicalStepPvE) == ActionID.QuadrupleTechnicalFinishPvE;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyEmboitePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (ActionID)JobGauge.NextStep == ActionID.EmboitePvE;
        setting.IsFriendly = true;
    }

    static partial void ModifyEntrechatPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (ActionID)JobGauge.NextStep == ActionID.EntrechatPvE;
        setting.IsFriendly = true;
    }

    static partial void ModifyJetePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (ActionID)JobGauge.NextStep == ActionID.JetePvE;
        setting.IsFriendly = true;
    }

    static partial void ModifyPirouettePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (ActionID)JobGauge.NextStep == ActionID.PirouettePvE;
        setting.IsFriendly = true;
    }

    static partial void ModifyLastDancePvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.LastDanceReady];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        //setting.ActionCheck = () => !IsDancing
    }

    static partial void ModifyFinishingMovePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => FinishingMovePvEReady;
        setting.StatusProvide = [StatusID.LastDanceReady];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        //setting.ActionCheck = () => !IsDancing
    }

    static partial void ModifyDanceOfTheDawnPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Esprit >= 50 && DanceOfTheDawnPvEReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        //setting.ActionCheck = () => !IsDancing
    }
    #endregion

    #region Step
    /// <summary>
    /// Finish the dance.
    /// </summary>
    /// <param name="act"></param>
    /// <param name="finishNow">Finish the dance as soon as possible</param>
    /// <returns></returns>
    protected bool DanceFinishGCD(out IAction? act, bool finishNow = false)
    {
        if (Player.HasStatus(true, StatusID.StandardStep) && CompletedSteps == 2)
        {
            if (DoubleStandardFinishPvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }
            if (Player.WillStatusEnd(1, true, StatusID.StandardStep, StatusID.StandardFinish) || finishNow)
            {
                act = StandardStepPvE;
                return true;
            }
            return false;
        }

        if (Player.HasStatus(true, StatusID.TechnicalStep) && CompletedSteps == 4)
        {
            if (QuadrupleTechnicalFinishPvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }
            if (Player.WillStatusEnd(1, true, StatusID.TechnicalStep) || finishNow)
            {
                act = TechnicalStepPvE;
                return true;
            }
            return false;
        }

        act = null;
        return false;
    }

    /// <summary>
    /// Do the dancing steps.
    /// </summary>
    /// <param name="act"></param>
    /// <returns></returns>
    protected bool ExecuteStepGCD(out IAction? act)
    {
        if (!IsDancing)
        {
            act = null;
            return false;
        }

        if (EmboitePvE.CanUse(out act)) return true;
        if (EntrechatPvE.CanUse(out act)) return true;
        if (JetePvE.CanUse(out act)) return true;
        if (PirouettePvE.CanUse(out act)) return true;
        return false;
    }
    #endregion

    #region PvP

    /// <summary>
    /// 
    /// </summary>

    static partial void ModifyCascadePvP(ref ActionSetting setting)
    {

    }

    static partial void ModifyFountainPvP(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.CascadePvP, ActionID.ReverseCascadePvP];
    }

    static partial void ModifyClosedPositionPvP(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
        setting.StatusProvide = [StatusID.ClosedPosition];
        setting.TargetStatusProvide = [StatusID.DancePartner];
        setting.TargetType = TargetType.DancePartner;
        setting.CreateConfig = () => new ActionConfig()
        {
        };
    }

    static partial void ModifyEnAvantPvP(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
        setting.IsFriendly = true;
        setting.StatusProvide = [StatusID.EnAvant];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyStarfallDancePvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.StatusProvide = [StatusID.StarfallDance];
    }

    static partial void ModifyFanDancePvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.StatusProvide = [StatusID.FanDance];
    }

    static partial void ModifyCuringWaltzPvP(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyReverseCascadePvP(ref ActionSetting setting)
    {
        setting.MPOverride = () => 0;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.StatusNeed = [StatusID.EnAvant];
        setting.StatusProvide = [StatusID.Bladecatcher];
    }

    static partial void ModifyFountainfallPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.MPOverride = () => 0;
        setting.StatusNeed = [StatusID.EnAvant];
        setting.StatusProvide = [StatusID.Bladecatcher];
    }

    static partial void ModifySaberDancePvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.MPOverride = () => 0;
        setting.StatusNeed = [StatusID.FlourishingSaberDance];
        setting.StatusProvide = [StatusID.SaberDance];
    }

    static partial void ModifyDanceOfTheDawnPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () =>
        {
            if (Player.HasStatus(true, StatusID.FlourishingSaberDance) && Player.HasStatus(true, StatusID.SoloStep))
            {
                return true;
            }
            return false;
        };
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.MPOverride = () => 0;
        setting.StatusNeed = [StatusID.FlourishingSaberDance, StatusID.SoloStep];
    }

    static partial void ModifyHoningDancePvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.StatusProvide = [StatusID.HoningDance];

    }

    static partial void ModifyHoningOvationPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.StatusProvide = [StatusID.HoningOvation];
    }

    #endregion
}
