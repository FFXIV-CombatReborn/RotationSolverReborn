namespace RotationSolver.Basic.Rotations.Basic;

partial class RedMageRotation
{
    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Intelligence;

    /// <inheritdoc/>
    public override bool CanHealSingleSpell => DataCenter.PartyMembers.Count() == 1 && base.CanHealSingleSpell;

    #region Job Gauge
    /// <summary>
    /// 
    /// </summary>
    public static byte WhiteMana => JobGauge.WhiteMana;

    /// <summary>
    /// 
    /// </summary>
    public static byte BlackMana => JobGauge.BlackMana;

    /// <summary>
    /// 
    /// </summary>
    public static byte ManaStacks => JobGauge.ManaStacks;

    /// <summary>
    /// Is <see cref="WhiteMana"/> larger than <see cref="BlackMana"/>
    /// </summary>
    public static bool IsWhiteManaLargerThanBlackMana => WhiteMana > BlackMana;

    /// <inheritdoc/>
    public override void DisplayStatus()
    {
        ImGui.Text("WhiteMana: " + WhiteMana.ToString());
        ImGui.Text("BlackMana: " + BlackMana.ToString());
        ImGui.Text("ManaStacks: " + ManaStacks.ToString());
        ImGui.Text("IsWhiteManaLargerThanBlackMana: " + IsWhiteManaLargerThanBlackMana.ToString());
        ImGui.Text("CanHealSingleSpell: " + CanHealSingleSpell.ToString());
    }
    #endregion

    private static readonly StatusID[] SwiftcastStatus = [.. StatusHelper.SwiftcastStatus, StatusID.Acceleration];
    static partial void ModifyJoltPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = SwiftcastStatus;
    }
    #region PvE Actions
    static partial void ModifyVerfirePvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.VerfireReady];
        setting.StatusProvide = SwiftcastStatus;
    }

    static partial void ModifyVerstonePvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.VerstoneReady];
        setting.StatusProvide = SwiftcastStatus;
    }

    static partial void ModifyVerthunderPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = SwiftcastStatus;
    }

    static partial void ModifyVeraeroPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = SwiftcastStatus;
    }

    static partial void ModifyReprisePvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyRipostePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (BlackMana >= 20 && WhiteMana >= 20) || Player.HasStatus(true, StatusID.MagickedSwordplay);
    }

    static partial void ModifyZwerchhauPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (BlackMana >= 15 && WhiteMana >= 15) || Player.HasStatus(true, StatusID.MagickedSwordplay);
        setting.ComboIds = [ActionID.RipostePvE];
    }

    static partial void ModifyRedoublementPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (BlackMana >= 15 && WhiteMana >= 15) || Player.HasStatus(true, StatusID.MagickedSwordplay);
        setting.ComboIds = [ActionID.ZwerchhauPvE];
    }

    static partial void ModifyScatterPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = SwiftcastStatus;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyVerthunderIiPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = SwiftcastStatus;
    }

    static partial void ModifyVeraeroIiPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = SwiftcastStatus;
    }

    static partial void ModifyMoulinetPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BlackMana >= 20 && WhiteMana >= 20;
    }

    static partial void ModifyScorchPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.VerholyPvE, ActionID.VerfirePvE];
    }

    static partial void ModifyResolutionPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.ScorchPvE];
    }

    private protected sealed override IBaseAction Raise => VerraisePvE;

    static partial void ModifyAccelerationPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Acceleration];
    }

    static partial void ModifyVercurePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = SwiftcastStatus;
    }

    static partial void ModifyEmboldenPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 10,
        };
    }

    static partial void ModifyManaficationPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => WhiteMana <= 50 && BlackMana <= 50 && InCombat && ManaStacks == 0 && !IsLastGCD(
            ActionID.RipostePvE,
            ActionID.EnchantedRipostePvE,
            ActionID.EnchantedRipostePvE_27055,
            ActionID.EnchantedRipostePvE_24918,
            ActionID.ZwerchhauPvE,
            ActionID.EnchantedZwerchhauPvE,
            ActionID.EnchantedZwerchhauPvE_27056,
            ActionID.EnchantedZwerchhauPvE_24919,
            ActionID.ScorchPvE,
            ActionID.ScorchPvE_24831,
            ActionID.ScorchPvE_24898,
            ActionID.VerflarePvE,
            ActionID.VerholyPvE,
            ActionID.VerholyPvE_21923,
            ActionID.VerholyPvE_27059,
            ActionID.VerflarePvE_20532,
            ActionID.VerflarePvE_27052
        );
        ;

        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 10,
        };
        setting.UnlockedByQuestID = 68118;
        setting.IsFriendly = true;
    }

    static partial void ModifyCorpsacorpsPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
    }

    static partial void ModifyVerholyPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 68123;
    }

    /// <inheritdoc/>
    [RotationDesc(ActionID.VercurePvE)]
    protected override bool HealSingleGCD(out IAction? act)
    {
        if (VercurePvE.CanUse(out act, skipStatusProvideCheck: true)) return true;
        return base.HealSingleGCD(out act);
    }

    /// <inheritdoc/>
    [RotationDesc(ActionID.CorpsacorpsPvE)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (CorpsacorpsPvE.CanUse(out act)) return true;
        return base.MoveForwardAbility(nextGCD, out act);
    }

    /// <inheritdoc/>
    [RotationDesc(ActionID.AddlePvE, ActionID.MagickBarrierPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (AddlePvE.CanUse(out act)) return true;
        if (MagickBarrierPvE.CanUse(out act, skipAoeCheck: true)) return true;
        return base.DefenseAreaAbility(nextGCD, out act);
    }

    static partial void ModifyViceOfThornsPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.ThornedFlourish];
    }

    static partial void ModifyGrandImpactPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.GrandImpactReady];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyPrefulgencePvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.PrefulgenceReady];
    }
    #endregion

    #region PvP Actions
    static partial void ModifyJoltIiiPvP(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Dualcast_1393];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyGrandImpactPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Dualcast_1393];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyEnchantedRipostePvP(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.EnchantedRiposte];
    }

    static partial void ModifyEnchantedZwerchhauPvP(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.EnchantedRipostePvP];
        setting.StatusProvide = [StatusID.EnchantedZwerchhau_3238];
    }

    static partial void ModifyEnchantedRedoublementPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.EnchantedRipostePvP) == ActionID.EnchantedRedoublementPvP;
        setting.StatusProvide = [StatusID.EnchantedRedoublement_3239];
    }

    static partial void ModifyScorchPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.EnchantedRipostePvP) == ActionID.ScorchPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyResolutionPvP(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Silence_1347];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyEmboldenPvP(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Embolden_2282, StatusID.PrefulgenceReady_4322];
    }

    static partial void ModifyCorpsacorpsPvP(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Monomachy_3242];
    }

    static partial void ModifyDisplacementPvP(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Displacement_3243];
    }

    static partial void ModifyFortePvP(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Forte];
    }

    static partial void ModifyPrefulgencePvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.EmboldenPvP) == ActionID.PrefulgencePvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyViceOfThornsPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.FortePvP) == ActionID.ViceOfThornsPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion
}