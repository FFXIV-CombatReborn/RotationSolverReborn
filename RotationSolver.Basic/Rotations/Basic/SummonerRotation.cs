using Dalamud.Interface.Colors;
using FFXIVClientStructs.FFXIV.Client.Game.Gauge;

namespace RotationSolver.Basic.Rotations.Basic;

public partial class SummonerRotation
{
    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Intelligence;

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
    public static byte AttunementCount => JobGauge.AttunementCount;

    

    /// <summary>
    /// 
    /// </summary>
    public static bool IsSolarBahamutReady => JobGauge.AetherFlags.HasFlag((AetherFlags)8) || JobGauge.AetherFlags.HasFlag((AetherFlags)12);

    /// <summary>
    /// 
    /// </summary>
    public static bool IsBahamutReady => !IsPhoenixReady && !IsSolarBahamutReady;

    /// <summary>
    /// 
    /// </summary>
    public static bool IsPhoenixReady => JobGauge.AetherFlags.HasFlag((AetherFlags)4) && !JobGauge.AetherFlags.HasFlag((AetherFlags)8);

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
    public static bool NoElementalSummon => !InGaruda && !InIfrit && !InTitan && !InIfrit && !InPhoenix && !InBahamut && !InSolarBahamut;

    /// <summary>
    /// 
    /// </summary>
    public static byte SMNAetherflowStacks => JobGauge.AetherflowStacks;

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
    protected static bool SummonTimeEndAfter(float time)
    {
        return SummonTime <= time;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gcdCount"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    protected static bool SummonTimeEndAfterGCD(uint gcdCount = 0, float offset = 0)
    {
        return SummonTimeEndAfter(GCDTime(gcdCount, offset));
    }

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
    protected static bool AttunmentTimeEndAfter(float time)
    {
        return AttunmentTime <= time;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gcdCount"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    protected static bool AttunmentTimeEndAfterGCD(uint gcdCount = 0, float offset = 0)
    {
        return AttunmentTimeEndAfter(GCDTime(gcdCount, offset));
    }

    /// <summary>
    /// 
    /// </summary>
    public static bool HasSummon => DataCenter.HasPet() && SummonTimeEndAfterGCD();
    #endregion

    #region Status

    /// <summary>
    /// 
    /// </summary>
    public static bool HasSearingLight => StatusHelper.PlayerHasStatus(true, StatusID.SearingLight);

    #endregion

    #region PvE Actions Unassignable Status

    /// <summary>
    /// 
    /// </summary>
    public static bool InBahamut => Service.GetAdjustedActionId(ActionID.AstralFlowPvE) == ActionID.DeathflarePvE;
    /// <summary>
    /// 
    /// </summary>
    public static bool SummonPhoenixPvEReady => Service.GetAdjustedActionId(ActionID.SummonBahamutPvE) == ActionID.SummonPhoenixPvE;
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
    public static bool InSolarBahamut => Service.GetAdjustedActionId(ActionID.AstralFlowPvE) == ActionID.SunflarePvE;
    /// <summary>
    /// 
    /// </summary>
    public static bool MountainBusterPvEReady => Service.GetAdjustedActionId(ActionID.AstralFlowPvE) == ActionID.MountainBusterPvE_25836;
    #endregion

    #region Draw Debug

    /// <inheritdoc/>
    public override void DisplayBaseStatus()
    {
        ImGui.Text("ReturnSummons: " + ReturnSummons.ToString());
        ImGui.Text("HasAetherflowStacks: " + HasAetherflowStacks.ToString());
        ImGui.Text("Attunement: " + AttunementCount.ToString());
        ImGui.Spacing();
        ImGui.TextColored(IsSolarBahamutReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "IsSolarBahamutReady: " + IsSolarBahamutReady.ToString());
        ImGui.TextColored(IsBahamutReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "IsBahamutReady: " + IsBahamutReady.ToString());
        ImGui.TextColored(IsPhoenixReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "IsPhoenixReady: " + IsPhoenixReady.ToString());
        ImGui.TextColored(IsIfritReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "IsIfritReady: " + InGaruda.ToString());
        ImGui.TextColored(IsTitanReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "IsTitanReady: " + IsTitanReady.ToString());
        ImGui.TextColored(IsGarudaReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "IsGarudaReady: " + IsGarudaReady.ToString());
        ImGui.Spacing();
        ImGui.TextColored(InSolarBahamut ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "InSolarBahamut: " + InSolarBahamut.ToString());
        ImGui.TextColored(InBahamut ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "InBahamut: " + InBahamut.ToString());
        ImGui.TextColored(InPhoenix ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "InPhoenix: " + InPhoenix.ToString());
        ImGui.TextColored(InIfrit ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "InIfrit: " + InIfrit.ToString());
        ImGui.TextColored(InTitan ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "InTitan: " + InTitan.ToString());
        ImGui.TextColored(InGaruda ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "InGaruda: " + InGaruda.ToString());
        ImGui.Spacing();
        ImGui.Text("SMNAetherflowStacks: " + SMNAetherflowStacks.ToString());
        ImGui.Text("SummonTime: " + SummonTime.ToString());
        ImGui.Text("AttunmentTime: " + AttunmentTime.ToString());
        ImGui.Text("HasSummon: " + HasSummon.ToString());
        ImGui.Text("Can Heal Single Spell: " + CanHealSingleSpell.ToString());
        ImGui.TextColored(ImGuiColors.DalamudViolet, "PvE Actions");
        ImGui.TextColored(InBahamut ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "InBahamut: " + InBahamut.ToString());
        ImGui.TextColored(SummonPhoenixPvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "SummonPhoenixPvEReady: " + SummonPhoenixPvEReady.ToString());
        ImGui.TextColored(EnkindlePhoenixPvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "EnkindlePhoenixPvEReady: " + EnkindlePhoenixPvEReady.ToString());
    }
    #endregion

    #region PvE Actions

    //Class Actions
    static partial void ModifyRuinPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !InBahamut && !InPhoenix;
    }

    private static RandomDelay _carbuncleDelay = new(() => (2, 2));
    static partial void ModifySummonCarbunclePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => _carbuncleDelay.Delay(!DataCenter.HasPet() && AttunmentTimeRaw == 0 && SummonTimeRaw == 0) && DataCenter.LastGCD is not ActionID.SummonCarbunclePvE;
    }

    static partial void ModifyRadiantAegisPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasSummon;
        setting.IsFriendly = true;
    }

    static partial void ModifyPhysickPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            GCDSingleHeal = true,
        };
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
        
    }

    static partial void ModifyFesterPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SMNAetherflowStacks > 0;
    }

    static partial void ModifyEnergyDrainPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.FurtherRuin];
        setting.ActionCheck = () => !HasAetherflowStacks;
    }

    static partial void ModifyResurrectionPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Player?.CurrentMp >= RaiseMPMinimum;
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
        setting.ActionCheck = () => !InBahamut && !InPhoenix;
    }    

    static partial void ModifyRuinIiPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 65997;
        setting.ActionCheck = () => !InBahamut && !InPhoenix;
    }

    // Job Actions

    static partial void ModifySummonIfritPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SummonTime <= WeaponRemain && IsIfritReady;
        setting.UnlockedByQuestID = 66627;
    }

    static partial void ModifySummonTitanPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SummonTime <= WeaponRemain && IsTitanReady;
        setting.UnlockedByQuestID = 66628;
    }

    static partial void ModifyPainflarePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasAetherflowStacks;
        setting.UnlockedByQuestID = 66629;
    }

    static partial void ModifySummonGarudaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SummonTime <= WeaponRemain && IsGarudaReady;
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
        setting.ActionCheck = () => !InBahamut && !InPhoenix;
    }

    static partial void ModifyDreadwyrmTrancePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InCombat && SummonTime <= WeaponRemain;
        setting.UnlockedByQuestID = 67640;
    }

    static partial void ModifyAstralFlowPvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 67641;
    }

    static partial void ModifyRuinIvPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.FurtherRuin_2701];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySearingLightPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.SearingLight];
        setting.ActionCheck = () => InCombat;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
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
        setting.ActionCheck = () => !InBahamut && !InPhoenix;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifySummonIfritIiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SummonTime <= WeaponRemain && IsIfritReady;
    }

    static partial void ModifySummonTitanIiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SummonTime <= WeaponRemain && IsTitanReady;
    }

    static partial void ModifySummonGarudaIiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SummonTime <= WeaponRemain && IsGarudaReady;
    }

    static partial void ModifyNecrotizePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SMNAetherflowStacks > 0;
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
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion

    #region PvE Actions Unassignable
    static partial void ModifyAstralImpulsePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InBahamut;
    }

    static partial void ModifyAstralFlarePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InBahamut;
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

    static partial void ModifyRubyRuinPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => AttunementCount > 0 && !AttunmentTimeEndAfter(ActionID.RubyRuinPvE.GetCastTime()) && InIfrit;
    }

    static partial void ModifyEmeraldRuinPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InGaruda;
    }

    static partial void ModifyTopazRuinPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InTitan;
    }

    static partial void ModifyRubyRuinIiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => AttunementCount > 0 && !AttunmentTimeEndAfter(ActionID.RubyRuinIiPvE.GetCastTime()) && InIfrit;
    }

    static partial void ModifyEmeraldRuinIiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InGaruda;
    }

    static partial void ModifyTopazRuinIiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InTitan;
    }

    static partial void ModifyRubyRuinIiiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => AttunementCount > 0 && !AttunmentTimeEndAfter(ActionID.RubyRuinIiiPvE.GetCastTime()) && InIfrit;
    }

    static partial void ModifyEmeraldRuinIiiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InGaruda;
    }

    static partial void ModifyTopazRuinIiiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InTitan;
    }

    static partial void ModifyRubyRitePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => AttunementCount > 0 && !AttunmentTimeEndAfter(ActionID.RubyRitePvE.GetCastTime()) && InIfrit;
    }

    static partial void ModifyEmeraldRitePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => AttunementCount > 0 && InGaruda;
    }

    static partial void ModifyTopazRitePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InTitan;
    }

    static partial void ModifyRubyOutburstPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => AttunementCount > 0 && !AttunmentTimeEndAfter(ActionID.RubyOutburstPvE.GetCastTime()) && InIfrit;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyEmeraldOutburstPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InGaruda;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyTopazOutburstPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InTitan;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyRubyDisasterPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => AttunementCount > 0 && !AttunmentTimeEndAfter(ActionID.RubyDisasterPvE.GetCastTime()) && InIfrit;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyEmeraldDisasterPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InGaruda;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyTopazDisasterPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InTitan;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyRubyCatastrophePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => AttunementCount > 0 && !AttunmentTimeEndAfter(ActionID.RubyCatastrophePvE.GetCastTime()) && InIfrit;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyEmeraldCatastrophePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InGaruda;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyTopazCatastrophePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InTitan;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifySummonPhoenixPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SummonPhoenixPvEReady;
    }

    static partial void ModifyFountainOfFirePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InPhoenix;
    }

    static partial void ModifyBrandOfPurgatoryPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InPhoenix;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
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
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
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

    static partial void ModifyCrimsonCyclonePvE(ref ActionSetting setting)
    {
        //setting.SpecialType = SpecialActionType.HostileMovingForward;
        setting.StatusProvide = [StatusID.CrimsonStrikeReady_4403];
        setting.StatusNeed = [StatusID.IfritsFavor];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyCrimsonStrikePvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.CrimsonStrikeReady_4403];
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
        setting.ActionCheck = () => !HasSwift && !StatusHelper.PlayerWillStatusEnd(ActionID.SlipstreamPvE.GetCastTime(), true, StatusID.GarudasFavor) 
                                    || HasSwift && !StatusHelper.PlayerWillStatusEndGCD(0, 0, true, StatusID.GarudasFavor);
        setting.StatusNeed = [StatusID.GarudasFavor];
        setting.StatusProvide = [StatusID.Slipstream];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySummonSolarBahamutPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => IsSolarBahamutReady && InCombat && SummonTime <= WeaponRemain;
        setting.UnlockedByQuestID = 68165;

    }

    static partial void ModifyUmbralImpulsePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InSolarBahamut;
    }

    static partial void ModifyUmbralFlarePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InSolarBahamut;
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
        setting.ActionCheck = () => InSolarBahamut;
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
        //setting.SpecialType = SpecialActionType.HostileMovingForward;
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
