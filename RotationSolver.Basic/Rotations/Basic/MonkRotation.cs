using Dalamud.Interface.Colors;

namespace RotationSolver.Basic.Rotations.Basic;
partial class MonkRotation
{
    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Strength;

    #region Job Gauge
    /// <summary>
    /// 
    /// </summary>
    protected static BeastChakra[] BeastChakras => JobGauge.BeastChakra;

    /// <summary>
    /// 
    /// </summary>
    public static byte Chakra => JobGauge.Chakra;

    /// <summary>
    /// 
    /// </summary>
    public static bool HasSolar => JobGauge.Nadi.HasFlag(Nadi.Solar);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasLunar => JobGauge.Nadi.HasFlag(Nadi.Lunar);

    /// <summary>
    /// .
    /// </summary>
    public static bool NoNadi => JobGauge.Nadi.HasFlag(Nadi.None);

    /// <summary>
    /// Gets the amount of available Opo-opo Fury stacks.
    /// </summary>
    public static int OpoOpoFury => JobGauge.OpoOpoFury;

    /// <summary>
    /// Gets the amount of available Raptor Fury stacks.
    /// </summary>
    public static int RaptorFury => JobGauge.RaptorFury;

    /// <summary>Brotherhood
    /// Gets the amount of available Coeurl Fury stacks.
    /// </summary>
    public static int CoeurlFury => JobGauge.CoeurlFury;

    /// <summary>Brotherhood
    /// Brotherhood
    /// </summary>
    public static bool InBrotherhood => !Player.WillStatusEnd(0, true, StatusID.Brotherhood);
    #endregion

    #region PvE Actions Unassignable

    /// <summary>
    /// 
    /// </summary>
    public static bool CelestialRevolutionPvEReady => Service.GetAdjustedActionId(ActionID.MasterfulBlitzPvE) == ActionID.CelestialRevolutionPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool FlintStrikePvEReady => Service.GetAdjustedActionId(ActionID.MasterfulBlitzPvE) == ActionID.FlintStrikePvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool RisingPhoenixPvEReady => Service.GetAdjustedActionId(ActionID.MasterfulBlitzPvE) == ActionID.RisingPhoenixPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool TornadoKickPvEReady => Service.GetAdjustedActionId(ActionID.MasterfulBlitzPvE) == ActionID.TornadoKickPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool PhantomRushPvEReady => Service.GetAdjustedActionId(ActionID.MasterfulBlitzPvE) == ActionID.PhantomRushPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool ElixirFieldPvEReady => Service.GetAdjustedActionId(ActionID.MasterfulBlitzPvE) == ActionID.ElixirFieldPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool ElixirBurstPvEReady => Service.GetAdjustedActionId(ActionID.MasterfulBlitzPvE) == ActionID.ElixirBurstPvE;
    #endregion

    #region Draw Debug

    /// <inheritdoc/>
    public override void DisplayStatus()
    {
        ImGui.Text($"CoeurlFury: {CoeurlFury}");
        ImGui.Text($"RaptorFury: {RaptorFury}");
        ImGui.Text($"OpoOpoFury: {OpoOpoFury}");
        ImGui.Text($"NoNadi: {NoNadi}");
        ImGui.Text($"HasLunar: {HasLunar}");
        ImGui.Text($"HasSolar: {HasSolar}");
        ImGui.Text($"Chakra: {Chakra}");
        ImGui.Text($"BeastChakras: {string.Join(", ", BeastChakras)}");
        ImGui.TextColored(ImGuiColors.DalamudViolet, "PvE Actions");
        ImGui.Text("CelestialRevolutionPvEReady: " + CelestialRevolutionPvEReady.ToString());
        ImGui.Text("FlintStrikePvEReady: " + FlintStrikePvEReady.ToString());
        ImGui.Text("RisingPhoenixPvEReady: " + RisingPhoenixPvEReady.ToString());
        ImGui.Text("TornadoKickPvEReady: " + TornadoKickPvEReady.ToString());
        ImGui.Text("PhantomRushPvEReady: " + PhantomRushPvEReady.ToString());
        ImGui.Text("ElixirFieldPvEReady: " + ElixirFieldPvEReady.ToString());
        ImGui.Text("ElixirBurstPvEReady: " + ElixirBurstPvEReady.ToString());
    }
    #endregion

    #region PvE Actions
    static partial void ModifyBootshinePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.RaptorForm];
    }

    static partial void ModifyTrueStrikePvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.RaptorForm, StatusID.PerfectBalance];
        setting.StatusProvide = [StatusID.CoeurlForm];
    }

    static partial void ModifySnapPunchPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.CoeurlForm, StatusID.PerfectBalance];
        setting.StatusProvide = [StatusID.OpoopoForm];
    }

    static partial void ModifySteeledMeditationPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (!InBrotherhood && Chakra < 5 || InBrotherhood && Chakra < 10);
        setting.IsFriendly = true;
    }

    static partial void ModifySteelPeakPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InCombat && (!InBrotherhood && Chakra == 5 || InBrotherhood && Chakra >= 5);
        setting.UnlockedByQuestID = 66094;
    }

    static partial void ModifyTwinSnakesPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.RaptorForm, StatusID.PerfectBalance];
        setting.StatusProvide = [StatusID.CoeurlForm];
        setting.ActionCheck = () => RaptorFury == 0;
    }

    static partial void ModifyArmOfTheDestroyerPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.RaptorForm];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyDemolishPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.CoeurlForm, StatusID.PerfectBalance];
        setting.StatusProvide = [StatusID.OpoopoForm];
        setting.ActionCheck = () => CoeurlFury == 0;
    }

    static partial void ModifyRockbreakerPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 66597;
        setting.StatusNeed = [StatusID.CoeurlForm, StatusID.PerfectBalance];
        setting.StatusProvide = [StatusID.OpoopoForm];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyThunderclapPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
        setting.UnlockedByQuestID = 66598;
        setting.IsFriendly = false;
    }

    static partial void ModifyInspiritedMeditationPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (!InBrotherhood && Chakra < 5 || InBrotherhood && Chakra < 10);
        setting.IsFriendly = true;
    }

    static partial void ModifyHowlingFistPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InCombat && (!InBrotherhood && Chakra == 5 || InBrotherhood && Chakra >= 5);
        setting.UnlockedByQuestID = 66599;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyMantraPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Mantra];
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 10,
            AoeCount = 1,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifyFourpointFuryPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 66600;
        setting.StatusNeed = [StatusID.RaptorForm, StatusID.PerfectBalance];
        setting.StatusProvide = [StatusID.CoeurlForm];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyDragonKickPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.RaptorForm];
        setting.ActionCheck = () => OpoOpoFury == 0;
    }

    static partial void ModifyPerfectBalancePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BeastChakras.Distinct().Count() == 1 && BeastChakras.Any(chakra => chakra == BeastChakra.None);
        setting.UnlockedByQuestID = 66602;
        setting.StatusProvide = [StatusID.PerfectBalance];
        setting.IsFriendly = true;
    }

    static partial void ModifyFormShiftPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.FormlessFist];
        setting.UnlockedByQuestID = 67563;
        setting.IsFriendly = true;
    }

    static partial void ModifyForbiddenMeditationPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (!InBrotherhood && Chakra < 5 || InBrotherhood && Chakra < 10);
        setting.IsFriendly = true;
    }

    static partial void ModifyTheForbiddenChakraPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InCombat && (!InBrotherhood && Chakra == 5 || InBrotherhood && Chakra >= 5);
        setting.UnlockedByQuestID = 67564;
    }

    static partial void ModifyMasterfulBlitzPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67567;
    }

    static partial void ModifyTornadoKickPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasSolar && HasLunar && BeastChakras.Any(chakra => chakra != BeastChakra.None);
        setting.StatusProvide = [StatusID.FormlessFist];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyElixirFieldPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ElixirFieldPvEReady;
        setting.StatusProvide = [StatusID.FormlessFist];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyCelestialRevolutionPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => CelestialRevolutionPvEReady;
        setting.StatusProvide = [StatusID.FormlessFist];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFlintStrikePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => FlintStrikePvEReady;
        setting.StatusProvide = [StatusID.FormlessFist];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyRiddleOfEarthPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.RiddleOfEarth, StatusID.EarthsRumination];
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyEarthsReplyPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.EarthsRumination];
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyRiddleOfFirePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.RiddleOfFire, StatusID.FiresRumination];
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 10,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifyBrotherhoodPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Brotherhood, StatusID.MeditativeBrotherhood];
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 10,
            AoeCount = 1,
        };
        setting.UnlockedByQuestID = 67966;
    }

    static partial void ModifyRiddleOfWindPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.RiddleOfWind, StatusID.WindsRumination];
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 10,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifyEnlightenedMeditationPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (!InBrotherhood && Chakra < 5 || InBrotherhood && Chakra < 10);
        setting.IsFriendly = true;
    }

    static partial void ModifyEnlightenmentPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InCombat && (!InBrotherhood && Chakra == 5 || InBrotherhood && Chakra >= 5);
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifySixsidedStarPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InCombat && Chakra >= 1;
        setting.StatusProvide = [StatusID.SixsidedStar];
    }

    static partial void ModifyShadowOfTheDestroyerPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.RaptorForm];
    }

    static partial void ModifyRisingPhoenixPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => RisingPhoenixPvEReady;
        setting.StatusProvide = [StatusID.FormlessFist];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyPhantomRushPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => PhantomRushPvEReady;
        setting.StatusProvide = [StatusID.FormlessFist];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyLeapingOpoPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.RaptorForm];
        setting.ActionCheck = () => OpoOpoFury >= 1;
    }

    static partial void ModifyRisingRaptorPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.RaptorForm, StatusID.PerfectBalance];
        setting.StatusProvide = [StatusID.CoeurlForm];
        setting.ActionCheck = () => RaptorFury >= 1;
    }

    static partial void ModifyPouncingCoeurlPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.CoeurlForm, StatusID.PerfectBalance];
        setting.StatusProvide = [StatusID.OpoopoForm];
        setting.ActionCheck = () => CoeurlFury >= 1;
    }

    static partial void ModifyElixirBurstPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ElixirBurstPvEReady;
        setting.StatusProvide = [StatusID.FormlessFist];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyWindsReplyPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.WindsRumination];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFiresReplyPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.FiresRumination];
        setting.StatusProvide = [StatusID.FormlessFist];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion

    #region PvP Actions

    static partial void ModifyDragonKickPvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyTwinSnakesPvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyDemolishPvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyLeapingOpoPvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyRisingRaptorPvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyPouncingCoeurlPvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyPhantomRushPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFlintsReplyPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyWindsReplyPvP(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.WindsRumination];
        setting.TargetStatusProvide = [StatusID.PressurePoint];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyRisingPhoenixPvP(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.FiresRumination_4301, StatusID.FireResonance];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyRiddleOfEarthPvP(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.EarthResonance];
        setting.IsFriendly = true;
    }

    static partial void ModifyFiresReplyPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.FiresRumination_4301];
    }

    static partial void ModifyEarthsReplyPvP(ref ActionSetting setting)
    {
        setting.TargetStatusNeed = [StatusID.EarthResonance];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyThunderclapPvP(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
    }


    #endregion
}
