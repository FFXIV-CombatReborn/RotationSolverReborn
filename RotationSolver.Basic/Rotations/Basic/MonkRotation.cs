using Dalamud.Interface.Colors;

namespace RotationSolver.Basic.Rotations.Basic;
public partial class MonkRotation
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
    /// 
    /// </summary>
    public static bool OpoOpoUnlocked => DataCenter.PlayerSyncedLevel() >= 50;

    /// <summary>
    /// Gets the amount of available Raptor Fury stacks.
    /// </summary>
    public static int RaptorFury => JobGauge.RaptorFury;

    /// <summary>
    /// 
    /// </summary>
    public static bool RaptorUnlocked => DataCenter.PlayerSyncedLevel() >= 18;

    /// <summary>
    /// Gets the amount of available Coeurl Fury stacks.
    /// </summary>
    public static int CoeurlFury => JobGauge.CoeurlFury;

    /// <summary>
    /// 
    /// </summary>
    public static bool CoeurlUnlocked => DataCenter.PlayerSyncedLevel() >= 30;

    /// <summary>
    /// Determines whether all elements in the <see cref="BeastChakras"/> array are the same.
    /// </summary>
    /// <returns>
    /// <c>true</c> if all elements are equal; otherwise, <c>false</c>.
    /// </returns>
    public static bool BeastChakrasAllSame()
    {
        var first = BeastChakras[0];
        for (int i = 1; i < BeastChakras.Length; i++)
        {
            if (!BeastChakras[i].Equals(first))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Determines whether all elements in the <see cref="BeastChakras"/> array are different from each other.
    /// </summary>
    /// <returns>
    /// <c>true</c> if all elements are unique or the array is empty; otherwise, <c>false</c>.
    /// </returns>
    public static bool BeastChakrasAllDifferent()
    {
        for (int i = 0; i < BeastChakras.Length; i++)
        {
            for (int j = i + 1; j < BeastChakras.Length; j++)
            {
                if (BeastChakras[i].Equals(BeastChakras[j]))
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Determines whether the specified <paramref name="value"/> exists in the <see cref="BeastChakras"/> array.
    /// </summary>
    /// <param name="value">The <see cref="BeastChakra"/> value to search for.</param>
    /// <returns>
    /// <c>true</c> if the value is found; otherwise, <c>false</c>.
    /// </returns>
    public static bool BeastChakrasContains(BeastChakra value)
    {
        for (int i = 0; i < BeastChakras.Length; i++)
        {
            if (BeastChakras[i].Equals(value))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Determines whether all elements in the <see cref="BeastChakras"/> array do <b>not</b> equal the specified <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The <see cref="BeastChakra"/> value to compare against each element.</param>
    /// <returns>
    /// <c>true</c> if none of the elements equal <paramref name="value"/>; otherwise, <c>false</c>.
    /// </returns>
    public static bool BeastChakrasAllNot(BeastChakra value)
    {
        for (int i = 0; i < BeastChakras.Length; i++)
        {
            if (BeastChakras[i].Equals(value))
                return false;
        }
        return true;
    }
    #endregion

    #region Status Tracking

    /// <summary>
    /// 
    /// </summary>
    public static bool InBrotherhood => StatusHelper.PlayerHasStatus(true, StatusID.Brotherhood);

    /// <summary>
    /// 
    /// </summary>
    public static bool InOpoopoForm => StatusHelper.PlayerHasStatus(true, StatusID.OpoopoForm);

    /// <summary>
    /// 
    /// </summary>
    public static bool InRaptorForm => StatusHelper.PlayerHasStatus(true, StatusID.RaptorForm);

    /// <summary>
    /// 
    /// </summary>
    public static bool InCoeurlForm => StatusHelper.PlayerHasStatus(true, StatusID.CoeurlForm);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasFormlessFist => StatusHelper.PlayerHasStatus(true, StatusID.FormlessFist);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasRiddleOfFire => StatusHelper.PlayerHasStatus(true, StatusID.RiddleOfFire);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasPerfectBalance => StatusHelper.PlayerHasStatus(true, StatusID.PerfectBalance);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasFiresRumination => StatusHelper.PlayerHasStatus(true, StatusID.FiresRumination);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasWindsRumination => StatusHelper.PlayerHasStatus(true, StatusID.WindsRumination);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasBrotherhood => StatusHelper.PlayerHasStatus(true, StatusID.Brotherhood);

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
    public override void DisplayBaseStatus()
    {
        ImGui.Text($"BeastChakrasAllSame: {BeastChakrasAllSame()}");
        ImGui.Text($"BeastChakrasContains(BeastChakra.None): {BeastChakrasContains(BeastChakra.None)}");
        ImGui.Text($"All Beast Chakras filled: {BeastChakrasAllNot(BeastChakra.None)}");
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
        setting.ActionCheck = () => (InRaptorForm || HasFormlessFist || HasPerfectBalance) && (RaptorFury > 0 || !RaptorUnlocked);
        setting.StatusProvide = [StatusID.CoeurlForm];
        setting.MPOverride = () => 0;
    }

    static partial void ModifySnapPunchPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (InCoeurlForm || HasFormlessFist || HasPerfectBalance) && (CoeurlFury > 0 || !CoeurlUnlocked);
        setting.StatusProvide = [StatusID.OpoopoForm];
    }

    static partial void ModifySteeledMeditationPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Chakra < 5;
        setting.IsFriendly = true;
    }

    static partial void ModifySteelPeakPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InCombat && Chakra >= 5;
        setting.UnlockedByQuestID = 66094;
    }

    static partial void ModifyTwinSnakesPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (InRaptorForm || HasFormlessFist || HasPerfectBalance) && RaptorFury == 0;
        setting.StatusProvide = [StatusID.CoeurlForm];
        setting.MPOverride = () => 0;
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
        setting.ActionCheck = () => (InCoeurlForm || HasFormlessFist || HasPerfectBalance) && CoeurlFury == 0;
        setting.StatusProvide = [StatusID.OpoopoForm];
    }

    static partial void ModifyRockbreakerPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (InCoeurlForm || HasFormlessFist || HasPerfectBalance);
        setting.StatusProvide = [StatusID.OpoopoForm];
        setting.UnlockedByQuestID = 66597;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyThunderclapPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.HostileFriendlyMovingForward;
        setting.UnlockedByQuestID = 66598;
        setting.IsFriendly = false;
    }

    static partial void ModifyInspiritedMeditationPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Chakra < 5;
        setting.IsFriendly = true;
    }

    static partial void ModifyHowlingFistPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InCombat && Chakra >= 5;
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
            AoeCount = 1,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifyFourpointFuryPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InRaptorForm || HasFormlessFist || HasPerfectBalance;
        setting.StatusProvide = [StatusID.CoeurlForm];
        setting.UnlockedByQuestID = 66600;
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
        setting.ActionCheck = () => InCombat && BeastChakrasAllSame() && BeastChakrasContains(BeastChakra.None);
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
        setting.ActionCheck = () => Chakra < 5;
        setting.IsFriendly = true;
    }

    static partial void ModifyTheForbiddenChakraPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InCombat && Chakra >= 5;
        setting.UnlockedByQuestID = 67564;
    }

    static partial void ModifyMasterfulBlitzPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67567;
    }

    static partial void ModifyTornadoKickPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasSolar && HasLunar && BeastChakrasAllNot(BeastChakra.None);
        setting.StatusProvide = [StatusID.FormlessFist];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyElixirFieldPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BeastChakrasAllSame() && !BeastChakrasContains(BeastChakra.None);
        setting.StatusProvide = [StatusID.FormlessFist];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyCelestialRevolutionPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BeastChakrasAllNot(BeastChakra.None) && !HasSolar && !HasLunar;
        setting.StatusProvide = [StatusID.FormlessFist];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFlintStrikePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BeastChakrasAllDifferent() && !BeastChakrasContains(BeastChakra.None);
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
        setting.ActionCheck = () => Chakra < 5;
        setting.IsFriendly = true;
    }

    static partial void ModifyEnlightenmentPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InCombat && ((!InBrotherhood && Chakra == 5) || (InBrotherhood && Chakra >= 5));
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
        setting.ActionCheck = () => BeastChakrasAllDifferent() && !BeastChakrasContains(BeastChakra.None);
        setting.StatusProvide = [StatusID.FormlessFist];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyPhantomRushPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasSolar && HasLunar && BeastChakrasAllNot(BeastChakra.None);
        setting.StatusProvide = [StatusID.FormlessFist];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyLeapingOpoPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.RaptorForm];
        setting.ActionCheck = () => (InOpoopoForm || HasFormlessFist || HasPerfectBalance) && (OpoOpoFury > 0 || !OpoOpoUnlocked);
    }

    static partial void ModifyRisingRaptorPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.CoeurlForm];
        setting.ActionCheck = () => (InRaptorForm || HasFormlessFist || HasPerfectBalance) && (RaptorFury > 0 || !RaptorUnlocked);
    }

    static partial void ModifyPouncingCoeurlPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.OpoopoForm];
        setting.ActionCheck = () => (InCoeurlForm || HasFormlessFist || HasPerfectBalance) && (CoeurlFury > 0 || !CoeurlUnlocked);
    }

    static partial void ModifyElixirBurstPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BeastChakrasAllSame() && !BeastChakrasContains(BeastChakra.None);
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
        setting.IsFriendly = false;
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
        setting.StatusNeed = [StatusID.EarthResonance];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyThunderclapPvP(ref ActionSetting setting)
    {
        //setting.SpecialType = SpecialActionType.MovingForward;
    }


    #endregion
}
