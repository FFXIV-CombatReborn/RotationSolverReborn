using Dalamud.Interface.Colors;

namespace RotationSolver.Basic.Rotations.Basic;

public partial class ReaperRotation
{
    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Strength;

    #region JobGauge
    /// <summary>
    /// 
    /// </summary>
    public static byte Soul => JobGauge.Soul;

    /// <summary>
    /// 
    /// </summary>
    public static byte Shroud => JobGauge.Shroud;

    /// <summary>
    /// 
    /// </summary>
    public static float EnshroudedTiemRemaining => JobGauge.EnshroudedTimeRemaining;

    /// <summary>
    /// 
    /// </summary>
    public static byte LemureShroud => JobGauge.LemureShroud;

    /// <summary>
    /// 
    /// </summary>
    public static byte VoidShroud => JobGauge.VoidShroud;
    #endregion

    #region Status Tracking
    /// <summary>
    /// 
    /// </summary>
    public static bool HasEnshrouded => StatusHelper.PlayerHasStatus(true, StatusID.Enshrouded);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasEnshroudedPvP => StatusHelper.PlayerHasStatus(true, StatusID.Enshrouded_2863);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasSoulReaver => StatusHelper.PlayerHasStatus(true, StatusID.SoulReaver);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasSoulsow => StatusHelper.PlayerHasStatus(true, StatusID.Soulsow);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasEnhancedGallows => StatusHelper.PlayerHasStatus(true, StatusID.EnhancedGallows);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasEnhancedGibbet => StatusHelper.PlayerHasStatus(true, StatusID.EnhancedGibbet);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasEnhancedVoidReaping => StatusHelper.PlayerHasStatus(true, StatusID.EnhancedVoidReaping);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasEnhancedCrossReaping => StatusHelper.PlayerHasStatus(true, StatusID.EnhancedCrossReaping);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasExecutioner => StatusHelper.PlayerHasStatus(true, StatusID.Executioner);

    /// <summary>
    /// Able to execute Enshroud
    /// </summary>
    public static bool HasIdealHost => StatusHelper.PlayerHasStatus(true, StatusID.IdealHost);

    /// <summary>
    /// Able to execute Plentiful Harvest
    /// </summary>
    public static bool HasImmortalSacrifice => StatusHelper.PlayerHasStatus(true, StatusID.ImmortalSacrifice);

    /// <summary>
    /// PvP version of Immortal Sacrifice
    /// </summary>
    public static bool HasImmortalSacrificePvP => StatusHelper.PlayerHasStatus(true, StatusID.ImmortalSacrifice_3204);

    /// <summary>
    /// Grants Immortal Sacrifice to the reaper who applied this effect when duration expires
    /// </summary>
    public static bool HasBloodsownCircleOther => StatusHelper.PlayerHasStatus(true, StatusID.BloodsownCircle);

    /// <summary>
    /// Able to gain stacks of Immortal Sacrifice from party members under the effect of your Circle of Sacrifice
    /// </summary>
    public static bool HasBloodsownCircleSelf => StatusHelper.PlayerHasStatus(true, StatusID.BloodsownCircle_2972);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasArcaneCircle => StatusHelper.PlayerHasStatus(true, StatusID.ArcaneCircle);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasOblatio => StatusHelper.PlayerHasStatus(true, StatusID.Oblatio);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasPerfectioParata => StatusHelper.PlayerHasStatus(true, StatusID.PerfectioParata);
    #endregion

    #region Actions Unassignable

    /// <summary>
    /// 
    /// </summary>
    public static bool UnveiledGibbetPvEReady => Service.GetAdjustedActionId(ActionID.BloodStalkPvE) == ActionID.UnveiledGibbetPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool UnveiledGallowsPvEReady => Service.GetAdjustedActionId(ActionID.BloodStalkPvE) == ActionID.UnveiledGallowsPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool RegressPvEIngressReady => Service.GetAdjustedActionId(ActionID.HellsIngressPvE) == ActionID.RegressPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool RegressPvEEgressReady => Service.GetAdjustedActionId(ActionID.HellsEgressPvE) == ActionID.RegressPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool VoidReapingPvEReady => Service.GetAdjustedActionId(ActionID.GibbetPvE) == ActionID.VoidReapingPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool CrossReapingPvEReady => Service.GetAdjustedActionId(ActionID.GallowsPvE) == ActionID.CrossReapingPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool GrimReapingPvEReady => Service.GetAdjustedActionId(ActionID.GuillotinePvE) == ActionID.GrimReapingPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool HarvestMoonPvEReady => Service.GetAdjustedActionId(ActionID.SoulsowPvE) == ActionID.HarvestMoonPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool LemuresSlicePvEReady => Service.GetAdjustedActionId(ActionID.BloodStalkPvE) == ActionID.LemuresSlicePvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool LemuresScythePvEReady => Service.GetAdjustedActionId(ActionID.GrimSwathePvE) == ActionID.LemuresScythePvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool SacrificiumPvEReady => Service.GetAdjustedActionId(ActionID.GluttonyPvE) == ActionID.SacrificiumPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool ExecutionersGibbetPvEReady => Service.GetAdjustedActionId(ActionID.GibbetPvE) == ActionID.ExecutionersGibbetPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool ExecutionersGallowsPvEReady => Service.GetAdjustedActionId(ActionID.GallowsPvE) == ActionID.ExecutionersGallowsPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool ExecutionersGuillotinePvEReady => Service.GetAdjustedActionId(ActionID.GuillotinePvE) == ActionID.ExecutionersGuillotinePvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool PerfectioPvEReady => Service.GetAdjustedActionId(ActionID.CommunioPvE) == ActionID.PerfectioPvE;
    #endregion

    #region Debug

    /// <inheritdoc/>
    public override void DisplayBaseStatus()
    {
        ImGui.Text("EnshroudedTiemRemaining: " + EnshroudedTiemRemaining.ToString());
        ImGui.Text("HasEnshrouded: " + HasEnshrouded.ToString());
        ImGui.Text("HasSoulReaver: " + HasSoulReaver.ToString());
        ImGui.Text("HasExecutioner: " + HasExecutioner.ToString());
        ImGui.Text("HasIdealHost: " + HasIdealHost.ToString());
        ImGui.Text("HasOblatio: " + HasOblatio.ToString());
        ImGui.Text("HasPerfectioParata: " + HasPerfectioParata.ToString());
        ImGui.Text("Soul: " + Soul.ToString());
        ImGui.Text("Shroud: " + Shroud.ToString());
        ImGui.Text("LemureShroud: " + LemureShroud.ToString());
        ImGui.Text("VoidShroud: " + VoidShroud.ToString());
        ImGui.TextColored(ImGuiColors.DalamudOrange, "PvE Actions");
        ImGui.Text("UnveiledGibbetPvEReady: " + UnveiledGibbetPvEReady.ToString());
        ImGui.Text("UnveiledGallowsPvEReady: " + UnveiledGallowsPvEReady.ToString());
        ImGui.Text("RegressPvEIngressReady: " + RegressPvEIngressReady.ToString());
        ImGui.Text("RegressPvEEgressReady: " + RegressPvEEgressReady.ToString());
        ImGui.Text("VoidReapingPvEReady: " + VoidReapingPvEReady.ToString());
        ImGui.Text("CrossReapingPvEReady: " + CrossReapingPvEReady.ToString());
        ImGui.Text("GrimReapingPvEReady: " + GrimReapingPvEReady.ToString());
        ImGui.Text("HarvestMoonPvEReady: " + HarvestMoonPvEReady.ToString());
        ImGui.Text("LemuresSlicePvEReady: " + LemuresSlicePvEReady.ToString());
        ImGui.Text("LemuresScythePvEReady: " + LemuresScythePvEReady.ToString());
        ImGui.Text("SacrificiumPvEReady: " + SacrificiumPvEReady.ToString());
        ImGui.Text("ExecutionersGibbetPvEReady: " + ExecutionersGibbetPvEReady.ToString());
        ImGui.Text("ExecutionersGallowsPvEReady: " + ExecutionersGallowsPvEReady.ToString());
        ImGui.Text("ExecutionersGuillotinePvEReady: " + ExecutionersGuillotinePvEReady.ToString());
        ImGui.Text("PerfectioPvEReady: " + PerfectioPvEReady.ToString());
    }
    #endregion

    #region PvE Actions
    static partial void ModifySlicePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !HasEnshrouded && !HasSoulReaver;
    }

    static partial void ModifyWaxingSlicePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !HasEnshrouded && !HasSoulReaver;
    }

    static partial void ModifyShadowOfDeathPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.DeathsDesign];
        setting.ActionCheck = () => !HasSoulReaver;
        setting.CreateConfig = () => new ActionConfig()
        {
            IsRestrictedDOT = true,
        };
    }

    static partial void ModifyHarpePvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MeleeRange;
    }

    static partial void ModifyHellsIngressPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.EnhancedHarpe, StatusID.Bind];
        setting.IsFriendly = true;
    }

    static partial void ModifyHellsEgressPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.EnhancedHarpe, StatusID.Bind];
        setting.IsFriendly = true;
    }

    static partial void ModifySpinningScythePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !HasEnshrouded && !HasSoulReaver;
    }

    static partial void ModifyInfernalSlicePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !HasEnshrouded && !HasSoulReaver;
    }

    static partial void ModifyWhorlOfDeathPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !HasSoulReaver;
        setting.TargetStatusProvide = [StatusID.DeathsDesign];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
            IsRestrictedDOT = true,
        };
    }

    static partial void ModifyArcaneCrestPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.CrestOfTimeBorrowed];
        setting.IsFriendly = true;
    }

    static partial void ModifyNightmareScythePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !HasEnshrouded && !HasSoulReaver;
    }

    static partial void ModifyBloodStalkPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.SoulReaver];
        setting.ActionCheck = () => !HasEnshrouded && !HasSoulReaver && Soul >= 50;
    }

    static partial void ModifyGrimSwathePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.SoulReaver];
        setting.ActionCheck = () => !HasEnshrouded && !HasSoulReaver && Soul >= 50;
    }

    static partial void ModifySoulSlicePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !HasEnshrouded && !HasSoulReaver && Soul <= 50;
    }

    static partial void ModifySoulScythePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !HasEnshrouded && !HasSoulReaver && Soul <= 50;
    }

    static partial void ModifyGibbetPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasSoulReaver;
    }

    static partial void ModifyGallowsPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasSoulReaver;
    }

    static partial void ModifyGuillotinePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasSoulReaver;
    }

    static partial void ModifyArcaneCirclePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.BloodsownCircle_2972];
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 10,
        };
    }

    static partial void ModifyGluttonyPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.SoulReaver, StatusID.Executioner];
        setting.ActionCheck = () => !HasEnshrouded && !HasSoulReaver && Soul >= 50;
    }

    static partial void ModifyEnshroudPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !HasEnshrouded && !HasSoulReaver && Soul >= 50;
        setting.UnlockedByQuestID = 69614;
    }

    static partial void ModifySoulsowPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Soulsow];
        setting.ActionCheck = () => !InCombat;
        setting.IsFriendly = true;
    }

    static partial void ModifyPlentifulHarvestPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !HasBloodsownCircleSelf && HasImmortalSacrifice;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyCommunioPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.PerfectioParata];
        setting.ActionCheck = () => LemureShroud == 1 && HasEnshrouded;
    }
    #endregion

    #region PvE Actions Unassaignable

    static partial void ModifyUnveiledGibbetPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => UnveiledGibbetPvEReady && Soul >= 50;
    }

    static partial void ModifyUnveiledGallowsPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => UnveiledGallowsPvEReady && Soul >= 50;
    }

    static partial void ModifyRegressPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => RegressPvEIngressReady || RegressPvEEgressReady;
        setting.IsFriendly = true;
    }

    static partial void ModifyVoidReapingPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => VoidReapingPvEReady && LemureShroud >= 1;
    }

    static partial void ModifyCrossReapingPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => CrossReapingPvEReady && LemureShroud >= 1;
    }

    static partial void ModifyGrimReapingPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => GrimReapingPvEReady && LemureShroud >= 1;
    }

    static partial void ModifyHarvestMoonPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HarvestMoonPvEReady && HasSoulsow;
    }

    static partial void ModifyLemuresSlicePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => LemuresSlicePvEReady && VoidShroud >= 2;
    }

    static partial void ModifyLemuresScythePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => LemuresScythePvEReady && VoidShroud >= 2;
    }

    static partial void ModifySacrificiumPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SacrificiumPvEReady && HasEnshrouded && HasOblatio;
    }

    static partial void ModifyExecutionersGibbetPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ExecutionersGibbetPvEReady && HasExecutioner;
    }

    static partial void ModifyExecutionersGallowsPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ExecutionersGallowsPvEReady && HasExecutioner;
    }

    static partial void ModifyExecutionersGuillotinePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ExecutionersGuillotinePvEReady && HasExecutioner;
    }

    static partial void ModifyPerfectioPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => PerfectioPvEReady && HasPerfectioParata;
    }

    #endregion

    #region PvP Actions
    static partial void ModifySlicePvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyWaxingSlicePvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyInfernalSlicePvP(ref ActionSetting setting)
    {
    }
    static partial void ModifyHarvestMoonPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyPlentifulHarvestPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyGrimSwathePvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyDeathWarrantPvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyHellsIngressPvP(ref ActionSetting setting)
    {
        //setting.SpecialType = SpecialActionType.MovingForward;
        setting.IsFriendly = true;
    }

    static partial void ModifyRegressPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.HellsIngressPvP) == ActionID.RegressPvP;
        setting.SpecialType = SpecialActionType.MovingBackward;
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyArcaneCrestPvP(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
    }

    static partial void ModifyExecutionersGuillotinePvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.SlicePvP) == ActionID.ExecutionersGuillotinePvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyVoidReapingPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Enshrouded_2863];
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.SlicePvP) == ActionID.VoidReapingPvP;
    }

    static partial void ModifyCrossReapingPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Enshrouded_2863];
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.SlicePvP) == ActionID.CrossReapingPvP;
    }

    static partial void ModifyLemuresSlicePvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.GrimSwathePvP) == ActionID.LemuresSlicePvP;
        setting.StatusNeed = [StatusID.Enshrouded_2863];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFateSealedPvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyPerfectioPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.PerfectioParata_4309];
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyCommunioPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Enshrouded_2863];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion
}