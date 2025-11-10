namespace RotationSolver.Basic.Rotations.Basic;

public partial class ScholarRotation
{
    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Mind;

    #region Job Gauge

    /// <summary>
    /// 
    /// </summary>
    public static byte FairyGauge => JobGauge.FairyGauge;

    /// <summary>
    /// 
    /// </summary>
    public static bool HasAetherflow => JobGauge.Aetherflow > 0;

    /// <summary>
    /// 
    /// </summary>
    public static byte SCHAetherFlowStacks => JobGauge.Aetherflow;

    private static float SeraphTimeRaw => JobGauge.SeraphTimer / 1000f;

    /// <summary>
    /// 
    /// </summary>
    public static float SeraphTime => SeraphTimeRaw - DataCenter.DefaultGCDRemain;

    /// <summary>
    /// 
    /// </summary>
    public static bool FairyDismissed => JobGauge.DismissedFairy > 0;
    #endregion

    #region Status Tracking
    /// <summary>
    /// Has Impact Imminent.
    /// </summary>
    public static bool HasImpactImminent => Player.HasStatus(true, StatusID.ImpactImminent);

    /// <summary>
    /// Has Dissipation.
    /// </summary>
    public static bool HasDissipation => Player.HasStatus(true, StatusID.Dissipation);

    /// <summary>
    /// Has EmergencyTactics.
    /// </summary>
    public static bool HasEmergencyTactics => Player.HasStatus(true, StatusID.EmergencyTactics);

    /// <summary>
    /// Has Recitation.
    /// </summary>
    public static bool HasRecitation => Player.HasStatus(true, StatusID.Recitation);
    #endregion

    #region Actions Unassignable

    /// <summary>
    /// 
    /// </summary>
    public static bool ManifestationReady => Service.GetAdjustedActionId(ActionID.AdloquiumPvE) == ActionID.ManifestationPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool AccessionReady => Service.GetAdjustedActionId(ActionID.ConcitationPvE) == ActionID.AccessionPvE;
    #endregion

    #region Debug

    /// <inheritdoc/>
    public override void DisplayBaseStatus()
    {
        ImGui.Text("FairyGauge: " + FairyGauge.ToString());
        ImGui.Text("HasAetherflow: " + HasAetherflow.ToString());
        ImGui.Text("SCHAetherFlowStacks: " + SCHAetherFlowStacks.ToString());
        ImGui.Text("SeraphTime: " + SeraphTime.ToString());
        ImGui.Text("Has Fairy Out: " + DataCenter.HasPet().ToString());
        ImGui.Text("FairyDismissed: " + FairyDismissed.ToString());
        ImGui.Text("ManifestationReady: " + ManifestationReady.ToString());
        ImGui.Text("AccessionReady: " + AccessionReady.ToString());
        ImGui.Text($"Excog Target: {ExcogitationPvE.Target.Target.Name}");
        ImGui.Text($"Recitation on Cooldown: {RecitationPvE.Cooldown.IsCoolingDown}");
        ImGui.Text($"Excog on cooldown: {ExcogitationPvE.Cooldown.IsCoolingDown}");
        ImGui.Text($"Whispering Dawn On Cooldown: {WhisperingDawnPvE.Cooldown.IsCoolingDown}");
        ImGui.Text($"Fey Blessing On Cooldown: {FeyBlessingPvE.Cooldown.IsCoolingDown}");
    }
    #endregion

    #region PvE Actions
    private protected sealed override IBaseAction Raise => ResurrectionPvE;

    static partial void ModifyRuinPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyBioPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Bio, StatusID.BioIi, StatusID.Biolysis];
        setting.CreateConfig = () => new ActionConfig()
        {
            IsRestrictedDOT = true,
        };
    }

    static partial void ModifyPhysickPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            GCDSingleHeal = true,
        };
    }

    static partial void ModifySummonEosPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !DataCenter.HasPet() && !FairyDismissed;
        setting.IsFriendly = true;
    }

    static partial void ModifyResurrectionPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
        setting.ActionCheck = () => Player.CurrentMp >= RaiseMPMinimum;
    }

    static partial void ModifyWhisperingDawnPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => DataCenter.HasPet() && !FairyDismissed;
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBioIiPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Bio, StatusID.BioIi, StatusID.Biolysis];
        setting.CreateConfig = () => new ActionConfig()
        {
            IsRestrictedDOT = true,
        };
    }

    static partial void ModifyAdloquiumPvE(ref ActionSetting setting)
    {
        setting.StatusFromSelf = false;
        setting.TargetStatusProvide =
        [
            StatusID.EukrasianDiagnosis,
            StatusID.EukrasianPrognosis,
            StatusID.Galvanize
        ];
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            GCDSingleHeal = true,
        };
        setting.UnlockedByQuestID = 66633;
    }

    static partial void ModifySuccorPvE(ref ActionSetting setting)
    {
        setting.StatusFromSelf = false;
        setting.StatusProvide =
        [
            StatusID.EukrasianDiagnosis,
            StatusID.EukrasianPrognosis,
            StatusID.Galvanize
        ];
        setting.IsFriendly = true;
        setting.UnlockedByQuestID = 66634;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyRuinIiPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyFeyIlluminationPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => DataCenter.HasPet() && !FairyDismissed;
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyAetherflowPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InCombat && !HasAetherflow;
        setting.IsFriendly = true;
    }

    static partial void ModifyEnergyDrainPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasAetherflow;
    }

    static partial void ModifyLustratePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasAetherflow;
        setting.UnlockedByQuestID = 66637;
    }

    static partial void ModifyArtOfWarPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifySacredSoilPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasAetherflow;
        setting.UnlockedByQuestID = 66638;
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyIndomitabilityPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasAetherflow;
        setting.UnlockedByQuestID = 67208;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBroilPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67209;
    }

    static partial void ModifyDeploymentTacticsPvE(ref ActionSetting setting)
    {
        setting.TargetType = TargetType.Deployment;
        setting.UnlockedByQuestID = 67210;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.ActionCheck = () => DataCenter.PartyMembers.Count >= 2;
    }

    static partial void ModifyEmergencyTacticsPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.EmergencyTactics];
        setting.UnlockedByQuestID = 67211;
        setting.IsFriendly = true;
    }

    static partial void ModifyDissipationPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Dissipation];
        setting.ActionCheck = () => !HasAetherflow && SeraphTime <= 0 && InCombat && DataCenter.HasPet();
        setting.UnlockedByQuestID = 67212;
        setting.IsFriendly = true;
    }

    static partial void ModifyExcogitationPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasAetherflow;
    }

    static partial void ModifyBroilIiPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyChainStratagemPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InCombat;
        setting.StatusProvide = [StatusID.ImpactImminent];
        setting.TargetStatusProvide = [StatusID.ChainStratagem];
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 10,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifyAetherpactPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => FairyGauge >= 10 && DataCenter.HasPet() && SeraphTime <= 0;
        setting.UnlockedByQuestID = 68463;
    }

    static partial void ModifyDissolveUnionPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
    }

    static partial void ModifyBiolysisPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Bio, StatusID.BioIi, StatusID.Biolysis];
        setting.CreateConfig = () => new ActionConfig()
        {
            IsRestrictedDOT = true,
        };
    }

    static partial void ModifyBroilIiiPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyRecitationPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Recitation];
        setting.IsFriendly = true;
    }

    static partial void ModifyFeyBlessingPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SeraphTime <= 0 && DataCenter.HasPet() && !FairyDismissed;
        setting.IsFriendly = true;
    }

    static partial void ModifySummonSeraphPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = DataCenter.HasPet;
        setting.IsFriendly = true;
    }

    static partial void ModifyConsolationPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SeraphTime > 0;
        setting.IsFriendly = true;
    }

    static partial void ModifyBroilIvPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyArtOfWarIiPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyProtractionPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Protraction];
    }

    static partial void ModifyExpedientPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Expedience, StatusID.DesperateMeasures];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBanefulImpactionPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.ImpactImminent];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyConcitationPvE(ref ActionSetting setting)
    {
        setting.StatusProvide =
        [
            StatusID.EukrasianDiagnosis,
            StatusID.EukrasianPrognosis,
            StatusID.Galvanize
        ];
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySeraphismPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => DataCenter.HasPet() && InCombat && !FairyDismissed;
        setting.IsFriendly = true;
    }

    static partial void ModifyManifestationPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ManifestationReady;
        setting.TargetStatusProvide =
        [
            StatusID.EukrasianDiagnosis,
            StatusID.EukrasianPrognosis,
            StatusID.Galvanize
        ];
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            GCDSingleHeal = true,
        };
    }

    static partial void ModifyAccessionPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => AccessionReady;
        setting.StatusProvide =
        [
            StatusID.EukrasianDiagnosis,
            StatusID.EukrasianPrognosis,
            StatusID.Galvanize
        ];
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion

    #region PvP Actions
    static partial void ModifyBroilIvPvP(ref ActionSetting setting)
    {

    }

    static partial void ModifyBiolysisPvP(ref ActionSetting setting)
    {

    }

    static partial void ModifyChainStratagemPvP(ref ActionSetting setting)
    {
        setting.IsFriendly = false;
    }

    static partial void ModifySummonSeraphPvP(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySeraphicHaloPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.BroilIvPvP) == ActionID.SeraphicHaloPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySeraphicVeilPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            IsEnabled = false,
        };
    }

    static partial void ModifyAdloquiumPvP(ref ActionSetting setting)
    {

    }

    static partial void ModifyDeploymentTacticsPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };

    }

    static partial void ModifyExpedientPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyAccessionPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Player.HasStatus(true, StatusID.Seraphism_4327);
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    #endregion
}