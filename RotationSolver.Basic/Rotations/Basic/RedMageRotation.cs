namespace RotationSolver.Basic.Rotations.Basic;

public partial class RedMageRotation
{
    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Intelligence;

    private protected sealed override IBaseAction Raise => VerraisePvE;

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
    public static bool MoreWhiteMana => WhiteMana > BlackMana;

    /// <summary>
    /// 
    /// </summary>
    public static bool MoreBlackMana => BlackMana >= WhiteMana;

    /// <summary>
    /// 
    /// </summary>
    public static bool BlackWhiteEqual => WhiteMana == BlackMana;

    /// <summary>
    /// 
    /// </summary>
    public bool EnoughManaCombo => JobGauge.BlackMana >= ManaNeeded() && JobGauge.WhiteMana >= ManaNeeded();

    /// <summary>
    /// 
    /// </summary>
    public static bool HasEnoughManaFor1Combo => BlackMana >= 20 && WhiteMana >= 20;

    /// <summary>
    /// 
    /// </summary>
    public static bool HasEnoughManaFor23Combo => BlackMana >= 15 && WhiteMana >= 15;

    /// <summary>
    /// 
    /// </summary>
    public static bool HasEnoughManaFor4Combo => BlackMana >= 5 && WhiteMana >= 5;

    /// <summary>
    /// 
    /// </summary>
    public static bool ManaStacksMaxed => JobGauge.ManaStacks == 3;

    /// <summary>
    /// 
    /// </summary>
    public static bool CanUseFlare => MoreBlackMana && JobGauge.BlackMana - JobGauge.WhiteMana < 18;

    /// <summary>
    /// 
    /// </summary>
    public static bool CanUseHoly => MoreWhiteMana && JobGauge.WhiteMana - JobGauge.BlackMana < 18;

    /// <summary>
    /// 
    /// </summary>
    public int ManaNeeded()
    {
        if (!ZwerchhauPvE.EnoughLevel) return 20;

        if (ZwerchhauPvE.EnoughLevel && !RedoublementPvE.EnoughLevel) return 35;

        if (RedoublementPvE.EnoughLevel && !EmboldenPvE.EnoughLevel) return 50;

        if (EmboldenPvE.EnoughLevel)
        {
            if (HasEmbolden) return 50;
            switch (EmboldenPvE.Cooldown.RecastTimeElapsed)
            {
                case > 80:
                    return 60;
                case > 40 and <= 80:
                    return 50;
                case > 15 and <= 40:
                    return 70;
                case <= 15:
                    return 90;
            }
        }

        return 0;
    }

    /// <summary>
    /// 
    /// </summary>
    public static string? VerEndsFirst
    {
        get
        {
            if (!CanVerBoth)
                return null;
            if (VerStoneTime == null || VerFireTime == null)
                return null;
            if (VerStoneTime < VerFireTime)
                return "VerStone";
            if (VerFireTime < VerStoneTime)
                return "VerFire";
            return "Equal";
        }
    }
    #endregion

    #region Status Check
    /// <summary>
    /// 
    /// </summary>
    public static bool HasEmbolden => StatusHelper.PlayerHasStatus(true, StatusID.Embolden);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasEmbolden2 => StatusHelper.PlayerHasStatus(true, StatusID.Embolden_1297);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasDualcast => StatusHelper.PlayerHasStatus(true, StatusID.Dualcast);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasAccelerate => StatusHelper.PlayerHasStatus(true, StatusID.Acceleration);

    /// <summary>
    /// Time left on VerFire.
    /// </summary>
    public static float? VerFireTime => StatusHelper.PlayerStatusTime(true, StatusID.VerfireReady);

    /// <summary>
    /// Time left on VerStone.
    /// </summary>
    public static float? VerStoneTime => StatusHelper.PlayerStatusTime(true, StatusID.VerstoneReady);

    /// <summary>
    /// 
    /// </summary>
    public static bool CanVerBoth => CanVerStone && CanVerFire;

    /// <summary>
    /// 
    /// </summary>
    public static bool CanVerEither => CanVerFire || CanVerStone;

    /// <summary>
    /// 
    /// </summary>
    public static bool CanVerStone => StatusHelper.PlayerHasStatus(true, StatusID.VerstoneReady);

    /// <summary>
    /// 
    /// </summary>
    public static bool CanVerFire => StatusHelper.PlayerHasStatus(true, StatusID.VerfireReady);

    /// <summary>
    /// 
    /// </summary>
    public static bool CanGrandImpact => StatusHelper.PlayerHasStatus(true, StatusID.GrandImpactReady);

    /// <summary>
    /// 
    /// </summary>
    public static bool CanMagickedSwordplay => StatusHelper.PlayerHasStatus(true, StatusID.MagickedSwordplay);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasManafication => StatusHelper.PlayerHasStatus(true, StatusID.Manafication);

    /// <summary>
    /// 
    /// </summary>
    public static bool CanPrefulgence => StatusHelper.PlayerHasStatus(true, StatusID.PrefulgenceReady);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasThornedFlourish => StatusHelper.PlayerHasStatus(true, StatusID.ThornedFlourish);

    /// <summary>
    /// 
    /// </summary>
    public static bool CanInstantCast => HasSwift || HasAccelerate;
    #endregion

    #region Status Display
    /// <inheritdoc/>
    public override void DisplayBaseStatus()
    {
        ImGui.Text("WhiteMana: " + WhiteMana.ToString());
        ImGui.Text("BlackMana: " + BlackMana.ToString());
        ImGui.Text("ManaStacks: " + ManaStacks.ToString());
        ImGui.Text("MoreWhiteMana: " + MoreWhiteMana.ToString());
        ImGui.Text("Can Heal Single Spell: " + CanHealSingleSpell.ToString());
    }
    #endregion

    #region PvE Actions
    static partial void ModifyRipostePvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyJoltPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Dualcast];
    }

    static partial void ModifyVerthunderPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.VerfireReady];
    }

    static partial void ModifyCorpsacorpsPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.HostileMovingForward;
    }

    static partial void ModifyVeraeroPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.VerstoneReady];
    }

    static partial void ModifyScatterPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyVerthunderIiPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyVeraeroIiPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyVerfirePvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.VerfireReady];
    }

    static partial void ModifyVerstonePvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.VerstoneReady];
    }

    static partial void ModifyZwerchhauPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.RipostePvE];
    }

    static partial void ModifyDisplacementPvE(ref ActionSetting setting)
    {
        
    }

    static partial void ModifyEngagementPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyFlechePvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyRedoublementPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.ZwerchhauPvE];
    }

    static partial void ModifyAccelerationPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Acceleration];
    }

    static partial void ModifyMoulinetPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyVercurePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Dualcast];
        setting.CreateConfig = () => new ActionConfig()
        {
            GCDSingleHeal = true,
        };
    }

    static partial void ModifyContreSixtePvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyEmboldenPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 10,
            AoeCount = 1,
        };
    }

    static partial void ModifyManaficationPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InCombat;
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 0,
        };
        setting.UnlockedByQuestID = 68118;
        setting.IsFriendly = true;
    }

    static partial void ModifyJoltIiPvE(ref ActionSetting setting)
    {
        
    }

    static partial void ModifyVerraisePvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Dualcast];
        setting.ActionCheck = () => Player?.CurrentMp >= RaiseMPMinimum;
    }

    static partial void ModifyVerflarePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ManaStacks == 3;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyVerholyPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 68123;
        setting.ActionCheck = () => ManaStacks == 3;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyReprisePvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyScorchPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.VerholyPvE, ActionID.VerfirePvE];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyVerthunderIiiPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.VerfireReady];
    }

    static partial void ModifyVeraeroIiiPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.VerstoneReady];
    }

    static partial void ModifyJoltIiiPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyMagickBarrierPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => IsMagicalDamageIncoming();
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyResolutionPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.ScorchPvE];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }


    static partial void ModifyViceOfThornsPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.ThornedFlourish];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
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
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion

    #region Enchanted Actions
    static partial void ModifyEnchantedRipostePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasEnoughManaFor1Combo || CanMagickedSwordplay;
    }

	static partial void ModifyEnchantedRipostePvE_45960(ref ActionSetting setting)
	{
		setting.ActionCheck = () => HasManafication && (HasEnoughManaFor1Combo || CanMagickedSwordplay);
	}

	static partial void ModifyEnchantedZwerchhauPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasEnoughManaFor23Combo || CanMagickedSwordplay;
    }

	static partial void ModifyEnchantedZwerchhauPvE_45961(ref ActionSetting setting)
	{
		setting.ActionCheck = () => HasManafication && (HasEnoughManaFor23Combo || CanMagickedSwordplay);
	}

	static partial void ModifyEnchantedRedoublementPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasEnoughManaFor23Combo || CanMagickedSwordplay;
    }

	static partial void ModifyEnchantedRedoublementPvE_45962(ref ActionSetting setting)
	{
		setting.ActionCheck = () => HasManafication && (HasEnoughManaFor23Combo || CanMagickedSwordplay);
	}

	static partial void ModifyEnchantedMoulinetPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasEnoughManaFor1Combo || CanMagickedSwordplay;
    }

    static partial void ModifyEnchantedMoulinetDeuxPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasEnoughManaFor23Combo || CanMagickedSwordplay;
    }

    static partial void ModifyEnchantedMoulinetTroisPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasEnoughManaFor23Combo || CanMagickedSwordplay;
    }

    static partial void ModifyEnchantedReprisePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasEnoughManaFor4Combo || CanMagickedSwordplay;
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
        setting.MPOverride = () => 0;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyEnchantedRipostePvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.EnchantedRipostePvP) == ActionID.EnchantedRipostePvP;
        setting.StatusProvide = [StatusID.EnchantedRiposte];
    }

    static partial void ModifyEnchantedZwerchhauPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => IsLastComboAction(ActionID.EnchantedRipostePvP);
        setting.StatusProvide = [StatusID.EnchantedZwerchhau_3238];
    }

    static partial void ModifyEnchantedRedoublementPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => IsLastComboAction(ActionID.EnchantedZwerchhauPvP);
        setting.StatusProvide = [StatusID.EnchantedRedoublement_3239];
    }

    static partial void ModifyScorchPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => IsLastComboAction(ActionID.EnchantedRedoublementPvP);
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
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyCorpsacorpsPvP(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Monomachy_3242];
        setting.SpecialType = SpecialActionType.HostileMovingForward;
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
        setting.StatusNeed = [StatusID.PrefulgenceReady_4322];
        setting.MPOverride = () => 0;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyViceOfThornsPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.ThornedFlourish_4321];
        setting.MPOverride = () => 0;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion
}