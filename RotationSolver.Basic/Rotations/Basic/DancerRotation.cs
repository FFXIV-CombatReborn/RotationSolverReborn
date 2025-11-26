using Dalamud.Interface.Colors;

namespace RotationSolver.Basic.Rotations.Basic;

public partial class DancerRotation
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

    /// <summary>
    /// 
    /// </summary>
    public static IBattleChara? CurrentDancePartner
    {
        get
        {
            if (Player.HasStatus(true, StatusID.ClosedPosition))
            {
                foreach (var member in PartyMembers)
                {
                    if (member.HasStatus(true, StatusID.DancePartner))
                        return member;
                }
            }
            return null;
        }
    }
    #endregion

    #region PvE Status Tracking
    /// <inheritdoc/>
    public override bool IsBursting()
    {
        if (Player.HasStatus(true, StatusID.StandardFinish)) // Devilment provides Crit/DH buffs, which phantom actions can't use
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Able to execute Last Dance.
    /// </summary>
    public static bool HasLastDance => Player.HasStatus(true, StatusID.LastDanceReady);

    /// <summary>
    /// Has Silken Symmetry status.
    /// </summary>
    public static bool HasSilkenSymmetry => Player.HasStatus(true, StatusID.SilkenSymmetry);

    /// <summary>
    /// Has Flourishing Symmetry status.
    /// </summary>
    public static bool HasFlourishingSymmetry => Player.HasStatus(true, StatusID.FlourishingSymmetry);

    /// <summary>
    /// Has Silken Flow status.
    /// </summary>
    public static bool HasSilkenFlow => Player.HasStatus(true, StatusID.SilkenFlow);

    /// <summary>
    /// Has Flourishing Flow status.
    /// </summary>
    public static bool HasFlourishingFlow => Player.HasStatus(true, StatusID.FlourishingFlow);

    /// <summary>
    /// Has Threefold Fan Dance status.
    /// </summary>
    public static bool HasThreefoldFanDance => Player.HasStatus(true, StatusID.ThreefoldFanDance);

    /// <summary>
    /// Has Fourfold Fan Dance status.
    /// </summary>
    public static bool HasFourfoldFanDance => Player.HasStatus(true, StatusID.FourfoldFanDance);

    /// <summary>
    /// Has Flourishing Starfall status.
    /// </summary>
    public static bool HasFlourishingStarfall => Player.HasStatus(true, StatusID.FlourishingStarfall);

    /// <summary>
    /// Has Standard Finish status.
    /// </summary>
    public static bool HasStandardFinish => Player.HasStatus(true, StatusID.StandardFinish);

    /// <summary>
    /// Has Standard Step status.
    /// </summary>
    public static bool HasStandardStep => Player.HasStatus(true, StatusID.StandardStep);

    /// <summary>
    /// Has Technical Step status.
    /// </summary>
    public static bool HasTechnicalStep => Player.HasStatus(true, StatusID.TechnicalStep);

    /// <summary>
    /// Has Technical Finish status.
    /// </summary>
    public static bool HasTechnicalFinish => Player.HasStatus(true, StatusID.TechnicalFinish);

    /// <summary>
    /// Has Devilment status.
    /// </summary>
    public static bool HasDevilment => Player.HasStatus(true, StatusID.Devilment);

    /// <summary>
    /// Has Closed Position status.
    /// </summary>
    public static bool HasClosedPosition => Player.HasStatus(true, StatusID.ClosedPosition);
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
    public override void DisplayBaseStatus()
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
        ImGui.TextColored(ImGuiColors.DalamudOrange, "Status Tracking");
        ImGui.Text("HasLastDance: " + HasLastDance.ToString());
        ImGui.Text("HasSilkenSymmetry: " + HasSilkenSymmetry.ToString());
        ImGui.Text("HasFlourishingSymmetry: " + HasFlourishingSymmetry.ToString());
        ImGui.Text("HasSilkenFlow: " + HasSilkenFlow.ToString());
        ImGui.Text("HasFlourishingFlow: " + HasFlourishingFlow.ToString());
        ImGui.Text("HasThreefoldFanDance: " + HasThreefoldFanDance.ToString());
        ImGui.Text("HasFourfoldFanDance: " + HasFourfoldFanDance.ToString());
        ImGui.Text("HasFlourishingStarfall: " + HasFlourishingStarfall.ToString());
        ImGui.Text("HasStandardFinish: " + HasStandardFinish.ToString());
        ImGui.Text("HasStandardStep: " + HasStandardStep.ToString());
        ImGui.Text("HasTechnicalStep: " + HasTechnicalStep.ToString());
        ImGui.Text("HasTechnicalFinish: " + HasTechnicalFinish.ToString());
        ImGui.Text("HasDevilment: " + HasDevilment.ToString());
        ImGui.Text("HasClosedPosition: " + HasClosedPosition.ToString());
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
        //setting.StatusNeed = [StatusID.SilkenSymmetry, StatusID.FlourishingSymmetry];
        setting.ActionCheck = () => HasSilkenSymmetry || HasFlourishingSymmetry;
    }

    static partial void ModifyFountainfallPvE(ref ActionSetting setting)
    {
        //setting.StatusNeed = [StatusID.SilkenFlow, StatusID.FlourishingFlow];
        setting.ActionCheck = () => HasSilkenFlow || HasFlourishingFlow;
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
        //setting.StatusNeed = [StatusID.SilkenSymmetry, StatusID.FlourishingSymmetry];
        setting.ActionCheck = () => HasSilkenSymmetry || HasFlourishingSymmetry;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyBloodshowerPvE(ref ActionSetting setting)
    {
        //setting.StatusNeed = [StatusID.SilkenFlow, StatusID.FlourishingFlow];
        setting.ActionCheck = () => HasSilkenFlow || HasFlourishingFlow;
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
        //setting.StatusNeed = [StatusID.ThreefoldFanDance];
        setting.ActionCheck = () => HasThreefoldFanDance;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFanDanceIvPvE(ref ActionSetting setting)
    {
        //setting.StatusNeed = [StatusID.FourfoldFanDance];
        setting.ActionCheck = () => HasFourfoldFanDance;
        setting.MPOverride = () => 0;
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
        //setting.StatusNeed = [StatusID.FlourishingStarfall];
        setting.ActionCheck = () => HasFlourishingStarfall;
        setting.MPOverride = () => 0;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyTillanaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Esprit <= 50 && TillanaPvEReady;
        setting.MPOverride = () => 0;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyEnAvantPvE(ref ActionSetting setting)
    {
        setting.TargetType = TargetType.Move;
        setting.IsFriendly = true;
    }

    static partial void ModifyClosedPositionPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
        setting.TargetType = TargetType.DancePartner;
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
        //setting.StatusNeed = [StatusID.StandardFinish];
        setting.StatusProvide = [StatusID.ThreefoldFanDance, StatusID.FourfoldFanDance, StatusID.FinishingMoveReady];
        setting.ActionCheck = () => InCombat && HasStandardFinish;
        setting.IsFriendly = true;
    }

    static partial void ModifyTechnicalStepPvE(ref ActionSetting setting)
    {
        //setting.StatusNeed = [StatusID.StandardFinish];
        setting.ActionCheck = () => HasStandardFinish;
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
        //setting.StatusNeed = [StatusID.StandardStep];
        setting.StatusProvide = [StatusID.LastDanceReady];
        setting.ActionCheck = () => HasStandardStep && IsDancing && CompletedSteps == 2 && Service.GetAdjustedActionId(ActionID.StandardStepPvE) == ActionID.DoubleStandardFinishPvE;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyQuadrupleTechnicalFinishPvE(ref ActionSetting setting)
    {
        //setting.StatusNeed = [StatusID.TechnicalStep];
        setting.StatusProvide = [StatusID.DanceOfTheDawnReady];
        setting.ActionCheck = () => HasTechnicalStep && IsDancing && CompletedSteps == 4 && Service.GetAdjustedActionId(ActionID.TechnicalStepPvE) == ActionID.QuadrupleTechnicalFinishPvE;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyEmboitePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (ActionID)JobGauge.NextStep == ActionID.EmboitePvE;
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            IsIntercepted = false,
        };
    }

    static partial void ModifyEntrechatPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (ActionID)JobGauge.NextStep == ActionID.EntrechatPvE;
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            IsIntercepted = false,
        };
    }

    static partial void ModifyJetePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (ActionID)JobGauge.NextStep == ActionID.JetePvE;
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            IsIntercepted = false,
        };
    }

    static partial void ModifyPirouettePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (ActionID)JobGauge.NextStep == ActionID.PirouettePvE;
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            IsIntercepted = false,
        };
    }

    static partial void ModifyLastDancePvE(ref ActionSetting setting)
    {
        //setting.StatusNeed = [StatusID.LastDanceReady];
        setting.ActionCheck = () => !IsDancing && HasLastDance;
        setting.MPOverride = () => 0;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFinishingMovePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !IsDancing && FinishingMovePvEReady;
        setting.StatusProvide = [StatusID.LastDanceReady];
        setting.MPOverride = () => 0;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
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

        if (EmboitePvE.CanUse(out act))
        {
            return true;
        }

        return EntrechatPvE.CanUse(out act) || JetePvE.CanUse(out act) || PirouettePvE.CanUse(out act);
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
    }

    static partial void ModifyEnAvantPvP(ref ActionSetting setting)
    {
        //setting.SpecialType = SpecialActionType.MovingForward;
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
            return Player.HasStatus(true, StatusID.FlourishingSaberDance) && Player.HasStatus(true, StatusID.SoloStep);
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
