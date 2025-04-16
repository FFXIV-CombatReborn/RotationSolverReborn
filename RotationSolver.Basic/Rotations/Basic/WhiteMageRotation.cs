namespace RotationSolver.Basic.Rotations.Basic;

partial class WhiteMageRotation
{
    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Mind;

    private protected sealed override IBaseAction Raise => RaisePvE;

    #region Job Gauge
    /// <summary>
    /// Represents the number of Lily stacks.
    /// </summary>
    public static byte Lily => JobGauge.Lily;

    /// <summary>
    /// Represents the number of Blood Lily stacks.
    /// </summary>
    public static byte BloodLily => JobGauge.BloodLily;

    /// <summary>
    /// Gets the raw Lily timer value in seconds.
    /// </summary>
    static float LilyTimeRaw => JobGauge.LilyTimer / 1000f;

    /// <summary>
    /// Gets the Lily timer value adjusted by the default GCD remain.
    /// </summary>
    public static float LilyTime => LilyTimeRaw + DataCenter.DefaultGCDRemain;

    /// <summary>
    /// Determines if the Lily timer will expire after the specified time.
    /// </summary>
    /// <param name="time">The time in seconds to check against the Lily timer.</param>
    /// <returns>True if the Lily timer will expire after the specified time; otherwise, false.</returns>
    protected static bool LilyAfter(float time) => LilyTime <= time;

    /// <summary>
    /// Determines if the Lily timer will expire after a specified number of GCDs and an optional offset.
    /// </summary>
    /// <param name="gcdCount">The number of GCDs to check against the Lily timer.</param>
    /// <param name="offset">An optional offset in seconds to add to the GCD time.</param>
    /// <returns>True if the Lily timer will expire after the specified number of GCDs and offset; otherwise, false.</returns>
    protected static bool LilyAfterGCD(uint gcdCount = 0, float offset = 0)
        => LilyAfter(GCDTime(gcdCount, offset));

    /// <summary>
    /// Gets the remaining number of Sacred Sight stacks.
    /// </summary>
    public static byte SacredSightStacks
    {
        get
        {
            byte stacks = Player.StatusStack(true, StatusID.SacredSight);
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
        ImGui.Text("SacredSightStacks: " + SacredSightStacks.ToString());
        ImGui.Text("LilyTime: " + LilyTime.ToString());
        ImGui.Text("BloodLilyStacks: " + BloodLily.ToString());
        ImGui.Text("Lily: " + Lily.ToString());
    }
    #endregion

    #region PvE Actions
    static partial void ModifyStonePvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyCurePvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyAeroPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [
            StatusID.Aero,
            StatusID.AeroIi,
            StatusID.Dia,
        ];
    }

    static partial void ModifyMedicaPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyRaisePvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyStoneIiPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyCureIiPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 65977;
    }

    static partial void ModifyPresenceOfMindPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InCombat;
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 10,
        };
        setting.UnlockedByQuestID = 66615;
        setting.StatusProvide = [StatusID.SacredSight];
        setting.IsFriendly = true;
    }

    static partial void ModifyRegenPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [
            StatusID.Regen,
            StatusID.Regen_897,
            StatusID.Regen_1330,
        ];
        setting.UnlockedByQuestID = 66616;
    }

    static partial void ModifyCureIiiPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.UnlockedByQuestID = 66617;
    }

    static partial void ModifyAetherialShiftPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
        setting.IsFriendly = true;
    }

    static partial void ModifyHolyPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = false;
        setting.UnlockedByQuestID = 66619;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyAeroIiPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [
            StatusID.Aero,
            StatusID.AeroIi,
            StatusID.Dia,
        ];
    }

    static partial void ModifyMedicaIiPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.MedicaIi, StatusID.TrueMedicaIi, StatusID.MedicaIii];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBenedictionPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 66620;
    }

    static partial void ModifyAfflatusSolacePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Lily > 0 && BloodLily < 3;
    }

    static partial void ModifyAsylumPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67256;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyStoneIiiPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67257;
    }

    static partial void ModifyAssizePvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67258;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyThinAirPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67259;
        setting.StatusProvide = [StatusID.ThinAir];
        setting.IsFriendly = true;
    }

    static partial void ModifyTetragrammatonPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67261;
    }

    static partial void ModifyStoneIvPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyDivineBenisonPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.DivineBenison];
    }

    static partial void ModifyPlenaryIndulgencePvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67954;
        setting.StatusProvide = [StatusID.Confession];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyDiaPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [
            StatusID.Aero,
            StatusID.AeroIi,
            StatusID.Dia,
        ];
    }

    static partial void ModifyGlarePvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyAfflatusMiseryPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BloodLily == 3;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyAfflatusRapturePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Lily > 0 && BloodLily < 3;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyTemperancePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Temperance, StatusID.DivineGrace];
        setting.IsFriendly = true;
    }

    static partial void ModifyGlareIiiPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyHolyIiiPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyAquaveilPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Aquaveil];
    }

    static partial void ModifyLiturgyOfTheBellPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyGlareIvPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SacredSightStacks > 0;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyMedicaIiiPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.MedicaIi, StatusID.TrueMedicaIi, StatusID.MedicaIii];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyDivineCaressPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.DivineGrace];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion

    #region PvP Actions
    static partial void ModifyGlareIiiPvP(ref ActionSetting setting)
    {

    }

    static partial void ModifyCureIiPvP(ref ActionSetting setting)
    {

    }

    static partial void ModifyAfflatusMiseryPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyAquaveilPvP(ref ActionSetting setting)
    {

    }

    static partial void ModifyMiracleOfNaturePvP(ref ActionSetting setting)
    {

    }

    static partial void ModifySeraphStrikePvP(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
    }

    static partial void ModifyGlareIvPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.GlareIiiPvP) == ActionID.GlareIvPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyCureIiiPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.CureIiPvP) == ActionID.CureIiiPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion
}