using Dalamud.Interface.Colors;

namespace RotationSolver.Basic.Rotations.Basic;

public partial class BardRotation
{
    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Dexterity;

    #region Job Gauge
    /// <summary>
    /// Gets the amount of Repertoire accumulated
    /// </summary>
    public static byte Repertoire => JobGauge.Repertoire;

    /// <summary>
    /// Gets the type of song that is active NONE = 0, MAGE = 1, ARMY = 2, WANDERER = 3
    /// </summary>
    protected static Song Song => JobGauge.Song;

    /// <summary>
    /// Gets the type of song that was last played
    /// </summary>
    protected static Song LastSong => JobGauge.LastSong;

    /// <summary>
    /// Gets the amount of Soul Voice accumulated
    /// </summary>
    public static byte SoulVoice => JobGauge.SoulVoice;
    private static float SongTimeRaw => JobGauge.SongTimer / 1000f;

    /// <summary>
    /// Gets the current song timer in milliseconds.
    /// </summary>
    public static float SongTime => SongTimeRaw - DataCenter.DefaultGCDRemain;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    protected static bool SongEndAfter(float time)
    {
        return SongTime <= time;
    }

    /// <summary>
    /// 
    /// </summary>
    public static byte BloodletterMax => EnhancedBloodletterTrait.EnoughLevel ? (byte)3 : (byte)2;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gctCount"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    protected static bool SongEndAfterGCD(uint gctCount = 0, float offset = 0)
    {
        return SongEndAfter(GCDTime(gctCount, offset));
    }
    #endregion

    #region Status Tracking
    /// <summary>
    /// Able to execute Raging Strikes.
    /// </summary>
    public static bool HasRagingStrikes => StatusHelper.PlayerHasStatus(true, StatusID.RagingStrikes);

    /// <summary>
    /// Able to execute Barrage.
    /// </summary>
    public static bool HasBarrage => StatusHelper.PlayerHasStatus(true, StatusID.Barrage);

    /// <summary>
    /// Able to execute Hawks Eye.
    /// </summary>
    public static bool HasHawksEye => StatusHelper.PlayerHasStatus(true, StatusID.HawksEye_3861);

    /// <summary>
    /// Able to execute Battle Voice.
    /// </summary>
    public static bool HasBattleVoice => StatusHelper.PlayerHasStatus(true, StatusID.BattleVoice);

    /// <summary>
    /// Able to execute Radiant Finale.
    /// </summary>
    public static bool HasRadiantFinale => StatusHelper.PlayerHasStatus(true, StatusID.RadiantFinale);
    #endregion

    #region PvE Actions Unassignable

    /// <summary>
    /// 
    /// </summary>
    public static bool BlastArrowPvEReady => Service.GetAdjustedActionId(ActionID.ApexArrowPvE) == ActionID.BlastArrowPvE;
    #endregion

    #region Draw Debug

    /// <inheritdoc/>
    public override void DisplayBaseStatus()
    {
        ImGui.Text("Repertoire: " + Repertoire.ToString());
        ImGui.Text("Song: " + Song.ToString());
        ImGui.Text("LastSong: " + LastSong.ToString());
        ImGui.Text("SoulVoice: " + SoulVoice.ToString());
        ImGui.Text("SongTimeRaw: " + SongTimeRaw.ToString());
        ImGui.Text("SongTime: " + SongTime.ToString());
        ImGui.Text("BloodletterMax: " + BloodletterMax.ToString());
        ImGui.Text("Bloodlettercharges: " + BloodletterPvE.Cooldown.CurrentCharges.ToString());
        ImGui.TextColored(ImGuiColors.DalamudViolet, "PvE Actions");
        ImGui.Text("BlastArrowPvEReady: " + BlastArrowPvEReady.ToString());
    }
    #endregion

    #region PvE
    static partial void ModifyHeavyShotPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.HawksEye_3861];
    }

    static partial void ModifyStraightShotPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.HawksEye_3861, StatusID.Barrage];
    }

    static partial void ModifyRagingStrikesPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
        setting.StatusProvide = [StatusID.RagingStrikes];
        setting.CreateConfig = () => new()
        {
            TimeToKill = 10,
        };
    }

    static partial void ModifyVenomousBitePvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.VenomousBite];
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 0,
            IsRestrictedDOT = true,
        };
    }

    static partial void ModifyBloodletterPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyRepellingShotPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 65604;
        setting.SpecialType = SpecialActionType.MovingBackward;
    }

    static partial void ModifyQuickNockPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.HawksEye_3861];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyWideVolleyPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.HawksEye_3861];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyWindbitePvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Windbite];
        setting.UnlockedByQuestID = 65612;
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 0,
            IsRestrictedDOT = true,

        };
    }

    static partial void ModifyMagesBalladPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.MagesBallad_2217];
        setting.ActionCheck = () => InCombat;
        setting.TargetType = TargetType.Self;
        setting.UnlockedByQuestID = 66621;
        setting.IsFriendly = true;
    }

    static partial void ModifyTheWardensPaeanPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.TheWardensPaean];
        setting.UnlockedByQuestID = 66622;
        setting.TargetType = TargetType.Dispel;
    }

    static partial void ModifyBarragePvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
        setting.StatusProvide = [StatusID.Barrage, StatusID.ResonantArrowReady];
    }

    static partial void ModifyArmysPaeonPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.ArmysPaeon_2218];
        setting.ActionCheck = () => InCombat;
        setting.TargetType = TargetType.Self;
        setting.UnlockedByQuestID = 66623;
        setting.IsFriendly = true;
    }

    static partial void ModifyRainOfDeathPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 66624;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyBattleVoicePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.BattleVoice];
        setting.UnlockedByQuestID = 66626;
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 10,
            AoeCount = 1,
        };
    }

    static partial void ModifyTheWanderersMinuetPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.TheWanderersMinuet_2216];
        setting.ActionCheck = () => InCombat;
        setting.TargetType = TargetType.Self;
        setting.UnlockedByQuestID = 67250;
        setting.IsFriendly = true;
    }

    static partial void ModifyPitchPerfectPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Song == Song.Wanderer && Repertoire > 0;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyEmpyrealArrowPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67251;
    }

    static partial void ModifyIronJawsPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.VenomousBite, StatusID.CausticBite, StatusID.Windbite, StatusID.Stormbite];
        setting.StatusProvide = [StatusID.HawksEye_3861];
        setting.CanTarget = t =>
        {
            return !t.WillStatusEndGCD(0, 0, true, StatusID.VenomousBite, StatusID.CausticBite) && !t.WillStatusEndGCD(0, 0, true, StatusID.Windbite, StatusID.Stormbite);
        };
        setting.UnlockedByQuestID = 67252;
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 0,
            IsRestrictedDOT = true,
        };
    }

    static partial void ModifySidewinderPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67254;
    }

    static partial void ModifyTroubadourPvE(ref ActionSetting setting)
    {
        setting.StatusFromSelf = false;
        setting.StatusProvide = StatusHelper.RangePhysicalDefense;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyCausticBitePvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.CausticBite];
        setting.StatusProvide = [StatusID.HawksEye_3861];
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 0,
            IsRestrictedDOT = true,
        };
    }

    static partial void ModifyStormbitePvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Stormbite];
        setting.StatusProvide = [StatusID.HawksEye_3861];
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 0,
            IsRestrictedDOT = true,
        };
    }

    static partial void ModifyNaturesMinnePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.NaturesMinne];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyRefulgentArrowPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.HawksEye_3861, StatusID.Barrage];
        setting.UnlockedByQuestID = 68430;
    }

    static partial void ModifyShadowbitePvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.HawksEye_3861, StatusID.Barrage];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyBurstShotPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.HawksEye_3861];
    }

    static partial void ModifyApexArrowPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.BlastArrowReady];
        setting.ActionCheck = () => SoulVoice >= 20;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyLadonsbitePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.HawksEye_3861];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyBlastArrowPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BlastArrowPvEReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyRadiantFinalePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => JobGauge.Coda.Any(s => s != Song.None);
        setting.StatusProvide = [StatusID.RadiantEncoreReady];
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 10,
            AoeCount = 1,
        };
    }

    static partial void ModifyHeartbreakShotPvE(ref ActionSetting setting)
    {
        //Maximum Charges: 3 Shares a recast timer with Rain of Death.
    }

    static partial void ModifyResonantArrowPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.ResonantArrowReady];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyRadiantEncorePvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.RadiantEncoreReady];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion

    #region PvP
    // PvP
    static partial void ModifyPowerfulShotPvP(ref ActionSetting setting)
    {

    }

    static partial void ModifyPitchPerfectPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Repertoire];
    }

    static partial void ModifyApexArrowPvP(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.BlastArrowReady_3142, StatusID.FrontlinersMarch];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBlastArrowPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.BlastArrowReady_3142];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySilentNocturnePvP(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Repertoire];
        setting.TargetStatusProvide = [StatusID.Silenced];
    }

    static partial void ModifyRepellingShotPvP(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingBackward;
    }

    static partial void ModifyEncoreOfLightPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.EncoreOfLightReady];
        setting.MPOverride = () => 0;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyTheWardensPaeanPvP(ref ActionSetting setting)
    {
        setting.TargetStatusNeed = StatusHelper.PurifyPvPStatuses;
        setting.IsFriendly = true;
    }

    #endregion
}
