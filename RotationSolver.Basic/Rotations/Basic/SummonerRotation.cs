using Dalamud.Interface.Colors;

namespace RotationSolver.Basic.Rotations.Basic;

partial class SummonerRotation
{
    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Intelligence;

    /// <summary>
    /// 
    /// </summary>
    public override bool CanHealSingleSpell => false;

    private protected sealed override IBaseAction Raise => ResurrectionPvE;

    #region JobGauge

    /// <summary>
    /// 
    /// </summary>
    public static SummonPet ReturnSummons => JobGauge.ReturnSummon;

    /// <summary>
    /// 
    /// </summary>
    public static bool HasAetherflowStacks => JobGauge.HasAetherflowStacks;

    /// <summary>
    /// 
    /// </summary>
    public static byte Attunement => JobGauge.Attunement;

    /// <summary>
    /// 
    /// </summary>
    public static bool IsBahamutReady => JobGauge.IsBahamutReady;

    /// <summary>
    /// 
    /// </summary>
    public static bool IsPhoenixReady => JobGauge.IsPhoenixReady;

    /// <summary>
    /// 
    /// </summary>
    public static bool IsIfritReady => JobGauge.IsIfritReady;

    /// <summary>
    /// 
    /// </summary>
    public static bool IsTitanReady => JobGauge.IsTitanReady;

    /// <summary>
    /// 
    /// </summary>
    public static bool IsGarudaReady => JobGauge.IsGarudaReady;

    /// <summary>
    /// 
    /// </summary>
    public static bool InTitan => JobGauge.IsTitanAttuned;

    /// <summary>
    /// 
    /// </summary>
    public static bool InIfrit => JobGauge.IsIfritAttuned;

    /// <summary>
    /// 
    /// </summary>
    public static bool InGaruda => JobGauge.IsGarudaAttuned;

    /// <summary>
    /// 
    /// </summary>
    public static byte AetherflowStacks => JobGauge.AetherflowStacks;

    private static float SummonTimeRaw => JobGauge.SummonTimerRemaining / 1000f;

    /// <summary>
    /// 
    /// </summary>
    public static float SummonTime => SummonTimeRaw - DataCenter.DefaultGCDRemain;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    protected static bool SummonTimeEndAfter(float time) => SummonTime <= time;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gcdCount"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    protected static bool SummonTimeEndAfterGCD(uint gcdCount = 0, float offset = 0)
        => SummonTimeEndAfter(GCDTime(gcdCount, offset));

    private static float AttunmentTimeRaw => JobGauge.AttunementTimerRemaining / 1000f;

    /// <summary>
    /// 
    /// </summary>
    public static float AttunmentTime => AttunmentTimeRaw - DataCenter.DefaultGCDRemain;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    protected static bool AttunmentTimeEndAfter(float time) => AttunmentTime <= time;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gcdCount"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    protected static bool AttunmentTimeEndAfterGCD(uint gcdCount = 0, float offset = 0)
        => AttunmentTimeEndAfter(GCDTime(gcdCount, offset));

    /// <summary>
    /// 
    /// </summary>
    private static bool HasSummon => DataCenter.HasPet && SummonTimeEndAfterGCD();
    #endregion

    #region PvE Actions Unassignable Status

    /// <summary>
    /// 
    /// </summary>
    public static bool AstralImpulsePvEReady => Service.GetAdjustedActionId(ActionID.RuinIiiPvE) == ActionID.AstralImpulsePvE;
    /// <summary>
    /// 
    /// </summary>
    public static bool AstralFlarePvEReady => Service.GetAdjustedActionId(ActionID.TridisasterPvE) == ActionID.AstralFlarePvE;
    /// <summary>
    /// 
    /// </summary>
    public static bool InBahamut => Service.GetAdjustedActionId(ActionID.AstralFlowPvE) == ActionID.DeathflarePvE;
    /// <summary>
    /// 
    /// </summary>
    public static bool RubyRitePvEReady => Service.GetAdjustedActionId(ActionID.GemshinePvE) == ActionID.RubyRitePvE;
    /// <summary>
    /// 
    /// </summary>
    public static bool TopazRitePvEReady => Service.GetAdjustedActionId(ActionID.GemshinePvE) == ActionID.TopazRitePvE;
    /// <summary>
    /// 
    /// </summary>
    public static bool EmeraldRitePvEReady => Service.GetAdjustedActionId(ActionID.GemshinePvE) == ActionID.EmeraldRitePvE;
    /// <summary>
    /// 
    /// </summary>
    public static bool SummonPhoenixPvEReady => Service.GetAdjustedActionId(ActionID.SummonBahamutPvE) == ActionID.SummonPhoenixPvE;
    /// <summary>
    /// 
    /// </summary>
    public static bool FountainOfFirePvEReady => Service.GetAdjustedActionId(ActionID.RuinIiiPvE) == ActionID.FountainOfFirePvE;
    /// <summary>
    /// 
    /// </summary>
    public static bool BrandOfPurgatoryPvEReady => Service.GetAdjustedActionId(ActionID.TridisasterPvE) == ActionID.BrandOfPurgatoryPvE;
    /// <summary>
    /// 
    /// </summary>
    public static bool InPhoenix => Service.GetAdjustedActionId(ActionID.AstralFlowPvE) == ActionID.RekindlePvE;
    /// <summary>
    /// 
    /// </summary>
    public static bool EnkindlePhoenixPvEReady => Service.GetAdjustedActionId(ActionID.EnkindleBahamutPvE) == ActionID.EnkindlePhoenixPvE;
    /// <summary>
    /// 
    /// </summary>
    public static bool RubyCatastrophePvEReady => Service.GetAdjustedActionId(ActionID.PreciousBrilliancePvE) == ActionID.RubyCatastrophePvE;
    /// <summary>
    /// 
    /// </summary>
    public static bool TopazCatastrophePvEReady => Service.GetAdjustedActionId(ActionID.PreciousBrilliancePvE) == ActionID.TopazCatastrophePvE;
    /// <summary>
    /// 
    /// </summary>
    public static bool EmeraldCatastrophePvEReady => Service.GetAdjustedActionId(ActionID.PreciousBrilliancePvE) == ActionID.EmeraldCatastrophePvE;
    /// <summary>
    /// 
    /// </summary>
    public static bool CrimsonCyclonePvEReady => Service.GetAdjustedActionId(ActionID.AstralFlowPvE) == ActionID.CrimsonCyclonePvE;
    /// <summary>
    /// 
    /// </summary>
    public static bool CrimsonStrikePvEReady => Service.GetAdjustedActionId(ActionID.AstralFlowPvE) == ActionID.CrimsonStrikePvE;
    /// <summary>
    /// 
    /// </summary>
    public static bool MountainBusterPvEReady => Service.GetAdjustedActionId(ActionID.AstralFlowPvE) == ActionID.MountainBusterPvE_25836;
    /// <summary>
    /// 
    /// </summary>
    public static bool SlipstreamPvEReady => Service.GetAdjustedActionId(ActionID.AstralFlowPvE) == ActionID.SlipstreamPvE;
    /// <summary>
    /// 
    /// </summary>
    public static bool SummonSolarBahamutPvEReady => Service.GetAdjustedActionId(ActionID.SummonBahamutPvE) == ActionID.SummonSolarBahamutPvE;
    /// <summary>
    /// 
    /// </summary>
    public static bool UmbralImpulsePvEReady => Service.GetAdjustedActionId(ActionID.RuinIiiPvE) == ActionID.UmbralImpulsePvE;
    /// <summary>
    /// 
    /// </summary>
    public static bool UmbralFlarePvEReady => Service.GetAdjustedActionId(ActionID.TridisasterPvE) == ActionID.UmbralFlarePvE;
    /// <summary>
    /// 
    /// </summary>
    public static bool InSolarBahamut => Service.GetAdjustedActionId(ActionID.AstralFlowPvE) == ActionID.SunflarePvE;
    /// <summary>
    /// 
    /// </summary>
    public static bool EnkindleSolarBahamutPvEReady => Service.GetAdjustedActionId(ActionID.EnkindleBahamutPvE) == ActionID.EnkindleSolarBahamutPvE;
    #endregion

    #region Draw Debug

    /// <inheritdoc/>
    public override void DisplayStatus()
    {
        ImGui.Text("ReturnSummons: " + ReturnSummons.ToString());
        ImGui.Text("HasAetherflowStacks: " + HasAetherflowStacks.ToString());
        ImGui.Text("Attunement: " + Attunement.ToString());
        ImGui.Text("IsBahamutReady: " + IsBahamutReady.ToString());
        ImGui.Text("IsPhoenixReady: " + IsPhoenixReady.ToString());
        ImGui.Text("IsIfritReady: " + IsIfritReady.ToString());
        ImGui.Text("IsTitanReady: " + IsTitanReady.ToString());
        ImGui.Text("IsGarudaReady: " + IsGarudaReady.ToString());
        ImGui.Text("InIfrit: " + InIfrit.ToString());
        ImGui.Text("InTitan: " + InTitan.ToString());
        ImGui.Text("InGaruda: " + InGaruda.ToString());
        ImGui.Text("AetherflowStacks: " + AetherflowStacks.ToString());
        ImGui.Text("SummonTime: " + SummonTime.ToString());
        ImGui.Text("AttunmentTime: " + AttunmentTime.ToString());
        ImGui.Text("HasSummon: " + HasSummon.ToString());
        ImGui.TextColored(ImGuiColors.DalamudViolet, "PvE Actions");
        ImGui.TextColored(AstralImpulsePvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "AstralImpulsePvEReady: " + AstralImpulsePvEReady.ToString());
        ImGui.TextColored(AstralFlarePvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "AstralFlarePvEReady: " + AstralFlarePvEReady.ToString());
        ImGui.TextColored(InBahamut ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "InBahamut: " + InBahamut.ToString());
        ImGui.TextColored(RubyRitePvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "RubyRitePvEReady: " + RubyRitePvEReady.ToString());
        ImGui.TextColored(TopazRitePvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "TopazRitePvEReady: " + TopazRitePvEReady.ToString());
        ImGui.TextColored(EmeraldRitePvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "EmeraldRitePvEReady: " + EmeraldRitePvEReady.ToString());
        ImGui.TextColored(SummonPhoenixPvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "SummonPhoenixPvEReady: " + SummonPhoenixPvEReady.ToString());
        ImGui.TextColored(FountainOfFirePvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "FountainOfFirePvEReady: " + FountainOfFirePvEReady.ToString());
        ImGui.TextColored(BrandOfPurgatoryPvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "BrandOfPurgatoryPvEReady: " + BrandOfPurgatoryPvEReady.ToString());
        ImGui.TextColored(InPhoenix ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "InPhoenix: " + InPhoenix.ToString());
        ImGui.TextColored(EnkindlePhoenixPvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "EnkindlePhoenixPvEReady: " + EnkindlePhoenixPvEReady.ToString());
        ImGui.TextColored(RubyCatastrophePvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "RubyCatastrophePvEReady: " + RubyCatastrophePvEReady.ToString());
        ImGui.TextColored(TopazCatastrophePvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "TopazCatastrophePvEReady: " + TopazCatastrophePvEReady.ToString());
        ImGui.TextColored(EmeraldCatastrophePvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "EmeraldCatastrophePvEReady: " + EmeraldCatastrophePvEReady.ToString());
        ImGui.TextColored(CrimsonCyclonePvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "CrimsonCyclonePvEReady: " + CrimsonCyclonePvEReady.ToString());
        ImGui.TextColored(CrimsonStrikePvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "CrimsonStrikePvEReady: " + CrimsonStrikePvEReady.ToString());
        ImGui.TextColored(SlipstreamPvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "SlipstreamPvEReady: " + SlipstreamPvEReady.ToString());
        ImGui.TextColored(SummonSolarBahamutPvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "SummonSolarBahamutPvEReady: " + SummonSolarBahamutPvEReady.ToString());
        ImGui.TextColored(UmbralImpulsePvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "UmbralImpulsePvEReady: " + UmbralImpulsePvEReady.ToString());
        ImGui.TextColored(UmbralFlarePvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "UmbralFlarePvEReady: " + UmbralFlarePvEReady.ToString());
        ImGui.TextColored(InSolarBahamut ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "InSolarBahamut: " + InSolarBahamut.ToString());
        ImGui.TextColored(EnkindleSolarBahamutPvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "EnkindleSolarBahamutPvEReady: " + EnkindleSolarBahamutPvEReady.ToString());
        ImGui.TextColored(MountainBusterPvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "MountainBusterPvEReady: " + MountainBusterPvEReady.ToString());
    }
    #endregion

    #region PvE Actions

    //Class Actions
    static partial void ModifyRuinPvE(ref ActionSetting setting)
    {

    }

    static RandomDelay _carbuncleDelay = new(() => (2, 2));
    static partial void ModifySummonCarbunclePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => _carbuncleDelay.Delay(!DataCenter.HasPet && AttunmentTimeRaw == 0 && SummonTimeRaw == 0) && DataCenter.LastGCD is not ActionID.SummonCarbunclePvE;
    }

    static partial void ModifyRadiantAegisPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasSummon;
        setting.IsFriendly = true;
    }

    static partial void ModifyPhysickPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyAetherchargePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InCombat && HasSummon;
    }

    static partial void ModifySummonRubyPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.IfritsFavor];
        setting.ActionCheck = () => SummonTime <= WeaponRemain && IsIfritReady;
    }

    static partial void ModifyGemshinePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Attunement > 0 && !AttunmentTimeEndAfter(ActionID.GemshinePvE.GetCastTime());
    }

    static partial void ModifyFesterPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasAetherflowStacks;
    }

    static partial void ModifyEnergyDrainPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.FurtherRuin];
        setting.ActionCheck = () => !HasAetherflowStacks;
    }

    static partial void ModifyResurrectionPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifySummonTopazPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SummonTime <= WeaponRemain && IsTitanReady;
        setting.UnlockedByQuestID = 66639;
    }

    static partial void ModifySummonEmeraldPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.GarudasFavor];
        setting.ActionCheck = () => SummonTime <= WeaponRemain && IsGarudaReady;
    }

    static partial void ModifyOutburstPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyPreciousBrilliancePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Attunement > 0 && !AttunmentTimeEndAfter(ActionID.PreciousBrilliancePvE.GetCastTime());
    }

    static partial void ModifyRuinIiPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 65997;
    }

    // Job Actions

    static partial void ModifySummonIfritPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 66627;
    }

    static partial void ModifySummonTitanPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 66628;
    }

    static partial void ModifyPainflarePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasAetherflowStacks;
        setting.UnlockedByQuestID = 66629;
    }

    static partial void ModifySummonGarudaPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 66631;
    }

    static partial void ModifyEnergySiphonPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.FurtherRuin];
        setting.ActionCheck = () => !HasAetherflowStacks;
        setting.UnlockedByQuestID = 67637;
    }

    static partial void ModifyRuinIiiPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67638;
    }

    static partial void ModifyDreadwyrmTrancePvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67640;
    }

    static partial void ModifyAstralFlowPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67641;
    }

    static partial void ModifyRuinIvPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.FurtherRuin_2701];
    }

    static partial void ModifySearingLightPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.SearingLight];
        setting.ActionCheck = () => InCombat;
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 15,
        };
    }

    static partial void ModifySummonBahamutPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InCombat && SummonTime <= WeaponRemain;
        setting.UnlockedByQuestID = 68165;
    }

    static partial void ModifyEnkindleBahamutPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InBahamut || InPhoenix;
    }

    static partial void ModifyTridisasterPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifySummonIfritIiPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifySummonTitanIiPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifySummonGarudaIiPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyNecrotizePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasAetherflowStacks;
    }

    static partial void ModifySearingFlashPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.RubysGlimmer];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyLuxSolarisPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.RefulgentLux];
    }
    #endregion

    #region PvE Actions Unassignable
    static partial void ModifyAstralImpulsePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => AstralImpulsePvEReady;
    }

    static partial void ModifyAstralFlarePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => AstralFlarePvEReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyDeathflarePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InBahamut;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyWyrmwavePvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyAkhMornPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyRubyRitePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => RubyRitePvEReady;
    }

    static partial void ModifyTopazRitePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => TopazRitePvEReady;
    }

    static partial void ModifyEmeraldRitePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => EmeraldRitePvEReady;
    }

    static partial void ModifySummonPhoenixPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SummonPhoenixPvEReady;
    }

    static partial void ModifyFountainOfFirePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => FountainOfFirePvEReady;
    }

    static partial void ModifyBrandOfPurgatoryPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BrandOfPurgatoryPvEReady;
    }

    static partial void ModifyRekindlePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InPhoenix;
    }

    static partial void ModifyEnkindlePhoenixPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => EnkindlePhoenixPvEReady;
    }

    static partial void ModifyEverlastingFlightPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyScarletFlamePvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyRevelationPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyRubyCatastrophePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => RubyCatastrophePvEReady;
    }

    static partial void ModifyTopazCatastrophePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => TopazCatastrophePvEReady;
    }

    static partial void ModifyEmeraldCatastrophePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => EmeraldCatastrophePvEReady;
    }

    static partial void ModifyCrimsonCyclonePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.CrimsonStrikeReady_4403];
        setting.ActionCheck = () => CrimsonCyclonePvEReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyCrimsonStrikePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => CrimsonStrikePvEReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyMountainBusterPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => MountainBusterPvEReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySlipstreamPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SlipstreamPvEReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySummonSolarBahamutPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SummonSolarBahamutPvEReady && InCombat && SummonTime <= WeaponRemain;
        setting.UnlockedByQuestID = 68165;

    }

    static partial void ModifyUmbralImpulsePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => UmbralImpulsePvEReady;
    }

    static partial void ModifyUmbralFlarePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => UmbralFlarePvEReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifySunflarePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InSolarBahamut;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyEnkindleSolarBahamutPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => EnkindleSolarBahamutPvEReady;
    }

    static partial void ModifyLuxwavePvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyExodusPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InSolarBahamut;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion

    #region PvP Actions
    static partial void ModifyRuinIiiPvP(ref ActionSetting setting)
    {

    }

    static partial void ModifyRuinIvPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.RuinIiiPvP) == ActionID.RuinIvPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyMountainBusterPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.TargetStatusProvide = [StatusID.Stun_1343];
    }

    static partial void ModifySlipstreamPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.TargetStatusProvide = [StatusID.Slipping];
    }

    static partial void ModifyCrimsonCyclonePvP(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
        setting.StatusProvide = [StatusID.CrimsonStrikeReady_4403];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyCrimsonStrikePvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.CrimsonCyclonePvP) == ActionID.CrimsonStrikePvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyRadiantAegisPvP(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.RadiantAegis_3224];
    }

    static partial void ModifyNecrotizePvP(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.FurtherRuin_4399];
    }

    static partial void ModifyDeathflarePvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.DreadwyrmTrance_3228];
    }

    static partial void ModifyAstralImpulsePvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.RuinIiiPvP) == ActionID.AstralImpulsePvP;
    }

    static partial void ModifyBrandOfPurgatoryPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.FirebirdTrance];
    }

    static partial void ModifyFountainOfFirePvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.RuinIiiPvP) == ActionID.FountainOfFirePvP;
    }
    #endregion

}
