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
    public static bool HasSolar => JobGauge.Nadi.HasFlag(Nadi.SOLAR);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasLunar => JobGauge.Nadi.HasFlag(Nadi.LUNAR);

    /// <summary>
    /// .
    /// </summary>
    public static bool NoNadi => JobGauge.Nadi.HasFlag(Nadi.NONE);

    /// <summary>
    /// Gets the amount of available Opo-opo Fury stacks.
    /// </summary>
    public static int OpoOpoFury => JobGauge.OpoOpoFury;

    /// <summary>
    /// Gets the amount of available Raptor Fury stacks.
    /// </summary>
    public static int RaptorFury => JobGauge.RaptorFury;

    /// <summary>
    /// Gets the amount of available Coeurl Fury stacks.
    /// </summary>
    public static int CoeurlFury => JobGauge.CoeurlFury;
    #endregion

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

    }

    static partial void ModifySteelPeakPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InCombat && Chakra >= 5;
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
        setting.CreateConfig = () => new()
        {
            AoeCount = 2,
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
        setting.CreateConfig = () => new()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyThunderclapPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
        setting.UnlockedByQuestID = 66598;
    }

    static partial void ModifyInspiritedMeditationPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyHowlingFistPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InCombat && Chakra >= 5;
        setting.UnlockedByQuestID = 66599;
        setting.CreateConfig = () => new()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyMantraPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Mantra];
        setting.CreateConfig = () => new()
        {
            TimeToKill = 10,
        };
    }

    static partial void ModifyFourpointFuryPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 66600;
        setting.StatusNeed = [StatusID.RaptorForm, StatusID.PerfectBalance];
        setting.StatusProvide = [StatusID.CoeurlForm];
        setting.CreateConfig = () => new()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyDragonKickPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.RaptorForm];
        setting.ActionCheck = () => OpoOpoFury == 0;
    }

    static partial void ModifyPerfectBalancePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InCombat && IsLongerThan(5);
        setting.UnlockedByQuestID = 66602;
        setting.StatusProvide = [StatusID.PerfectBalance];
    }

    static partial void ModifyFormShiftPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.FormlessFist];
        setting.UnlockedByQuestID = 67563;
    }

    static partial void ModifyForbiddenMeditationPvE(ref ActionSetting setting)
    {

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
        setting.ActionCheck = () => HasSolar && HasLunar && BeastChakras.Any(chakra => chakra != BeastChakra.NONE);
        setting.StatusProvide = [StatusID.FormlessFist];
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyElixirFieldPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BeastChakras.Distinct().Count() == 1 && BeastChakras.Any(chakra => chakra != BeastChakra.NONE);
        setting.StatusProvide = [StatusID.FormlessFist];
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyCelestialRevolutionPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BeastChakras.Distinct().Count() == 2 && BeastChakras.Any(chakra => chakra != BeastChakra.NONE);
        setting.StatusProvide = [StatusID.FormlessFist];
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFlintStrikePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BeastChakras.Distinct().Count() == 3 && BeastChakras.Any(chakra => chakra != BeastChakra.NONE);
        setting.StatusProvide = [StatusID.FormlessFist];
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyRiddleOfEarthPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.RiddleOfEarth, StatusID.EarthsRumination];
    }

    static partial void ModifyEarthsReplyPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.EarthsRumination];
    }

    static partial void ModifyRiddleOfFirePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.RiddleOfFire, StatusID.FiresRumination];
        setting.CreateConfig = () => new()
        {
            TimeToKill = 10,
        };
    }

    static partial void ModifyBrotherhoodPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Brotherhood, StatusID.MeditativeBrotherhood];
        setting.CreateConfig = () => new()
        {
            TimeToKill = 10,
        };
        setting.UnlockedByQuestID = 67966;
    }

    static partial void ModifyRiddleOfWindPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.RiddleOfWind, StatusID.WindsRumination];
        setting.CreateConfig = () => new()
        {
            TimeToKill = 10,
        };
    }

    static partial void ModifyEnlightenedMeditationPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyEnlightenmentPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InCombat && Chakra >= 5;
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySixsidedStarPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InCombat && Chakra >= 5;
        setting.StatusProvide = [StatusID.SixsidedStar];
    }

    static partial void ModifyShadowOfTheDestroyerPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.RaptorForm];
    }

    static partial void ModifyRisingPhoenixPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BeastChakras.Distinct().Count() == 3 && BeastChakras.Any(chakra => chakra != BeastChakra.NONE);
        setting.StatusProvide = [StatusID.FormlessFist];
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyPhantomRushPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasSolar && HasLunar && BeastChakras.Any(chakra => chakra != BeastChakra.NONE);
        setting.StatusProvide = [StatusID.FormlessFist];
        setting.CreateConfig = () => new()
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
        setting.ActionCheck = () => BeastChakras.Distinct().Count() == 1 && BeastChakras.Any(chakra => chakra != BeastChakra.NONE);
        setting.StatusProvide = [StatusID.FormlessFist];
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyWindsReplyPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.WindsRumination];
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFiresReplyPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.FiresRumination];
        setting.StatusProvide = [StatusID.FormlessFist];
        setting.CreateConfig = () => new()
        {
            AoeCount = 1,
        };
    }


    // PvP
    static partial void ModifyThunderclapPvP(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
    }

    /// <inheritdoc/>
    [RotationDesc(ActionID.ThunderclapPvE)]
    protected sealed override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (ThunderclapPvE.CanUse(out act)) return true;
        return base.MoveForwardAbility(nextGCD, out act);
    }

    /// <inheritdoc/>
    [RotationDesc(ActionID.FeintPvE)]
    protected sealed override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (FeintPvE.CanUse(out act)) return true;
        return base.DefenseAreaAbility(nextGCD, out act);
    }

    /// <inheritdoc/>
    [RotationDesc(ActionID.MantraPvE)]
    protected sealed override bool HealAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (MantraPvE.CanUse(out act)) return true;
        return base.HealAreaAbility(nextGCD, out act);
    }

    /// <inheritdoc/>
    [RotationDesc(ActionID.RiddleOfEarthPvE)]
    protected sealed override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (RiddleOfEarthPvE.CanUse(out act, usedUp: true)) return true;
        return base.DefenseSingleAbility(nextGCD, out act);
    }
}
