using System.ComponentModel;
using Dalamud.Game.ClientState.JobGauge.Types;
using ECommons.DalamudServices;

namespace RotationSolver.ExtraRotations.Magical;

[Rotation("Churin SMN", CombatType.PvE, GameVersion = "7.4")]
[SourceCode(Path = "main/ExtraRotations/Magical/ChurinSMN.cs")]

public sealed class ChurinSMN : SummonerRotation
{
    #region Properties
    private bool CanBurst => MergedStatus.HasFlag(AutoStatus.Burst) && SearingLightPvE.IsEnabled;
    private bool InBigSummon => !SummonBahamutPvE.EnoughLevel || InBahamut || InPhoenix || InSolarBahamut;
    private static bool HasFurtherRuin => StatusHelper.PlayerHasStatus(true, StatusID.FurtherRuin_2701);
    private static bool HasCrimsonStrike => StatusHelper.PlayerHasStatus(true, StatusID.CrimsonStrikeReady_4403);
    private static bool HasRadiantAegis => StatusHelper.PlayerHasStatus(true, StatusID.RadiantAegis);
    private static bool HasGarudaFavor => StatusHelper.PlayerHasStatus(true, StatusID.GarudasFavor);
    private static bool HasIfritFavor => StatusHelper.PlayerHasStatus(true, StatusID.IfritsFavor);
    private static bool HasTitanFavor => StatusHelper.PlayerHasStatus(true, StatusID.TitansFavor);
    private static bool NoPrimalReady => !IsIfritReady && !IsGarudaReady && !IsTitanReady;
    private static bool AnyPrimalReady => IsIfritReady || IsGarudaReady || IsTitanReady;
    private static bool HasAnyFavor => HasGarudaFavor || HasIfritFavor || HasTitanFavor;
    private static bool HasAnyAttunement => InGaruda || InIfrit || InTitan;
    private static bool NoAttunement => !InIfrit && !InGaruda && !InTitan;
    private static bool InSolar => DataCenter.PlayerSyncedLevel() == 100 ? !InBahamut && !InPhoenix && InSolarBahamut : InBahamut && !InPhoenix;
    private bool BahamutBurst => ((SummonSolarBahamutPvE.EnoughLevel && InSolarBahamut) 
    || (SummonSolarBahamutPvE.EnoughLevel && (InBahamut || InPhoenix)) 
    || (!SummonSolarBahamutPvE.EnoughLevel && InBahamut) 
    || !SummonBahamutPvE.EnoughLevel) && CanBurst;
    private static SMNGauge SummonerGauge => Svc.Gauges.Get<SMNGauge>();
    private static double LateWeaveWindow => (float)(WeaponTotal * 0.45);
    private static bool CanWeave => WeaponRemain > AnimationLock;
    private static bool CanLateWeave => WeaponRemain < LateWeaveWindow && CanWeave;
    private static float SummonTimer => SummonerGauge.SummonTimerRemaining / 1000f;
    #endregion

    #region Config Options

    public enum SummonOrderType : byte
    {
        [Description("Topaz-Emerald-Ruby")] TopazEmeraldRuby,

        [Description("Topaz-Ruby-Emerald")] TopazRubyEmerald,

        [Description("Emerald-Topaz-Ruby")] EmeraldTopazRuby,

        [Description("Emerald-Ruby-Topaz")] EmeraldRubyTopaz,

        [Description("Ruby-Emerald-Topaz")] RubyEmeraldTopaz,

        [Description("Ruby-Topaz-Emerald")] RubyTopazEmerald,
    }

    public enum FightPreset : ushort
    {
        [Description("None")] None,
        [Description("AAC Cruiserweight M4 (Savage)")] CruiserweightM4S,
        [Description("AAC Cruiserweight M3 (Savage) - WIP")] CruiserweightM3S,
        [Description("AAC Cruiserweight M2 (Savage) - WIP")] CruiserweightM2S,
        [Description("AAC Cruiserweight M1 (Savage) - WIP")] CruiserweightM1S,
    }

    [RotationConfig(CombatType.PvE, Name = "Use Crimson Cyclone at any range, regardless of safety use with caution (Enabling this ignores the below distance setting).")]
    public bool AddCrimsonCyclone { get; set; } = true;

    [Range(1, 20, ConfigUnitType.Yalms)]
    [RotationConfig(CombatType.PvE, Name = "Max distance you can be from the target for Crimson Cyclone use")]
    public float CrimsonCycloneDistance { get; set; } = 3.0f;

    [RotationConfig(CombatType.PvE, Name = "Use Crimson Cyclone when moving")]
    public bool AddCrimsonCycloneMoving { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Swiftcast on resurrection")]
    public bool AddSwiftcastOnRaise { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Swiftcast on Ruby Ruin when not enough level for Ruby Rite")]
    public bool AddSwiftcastOnLowSt { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Swiftcast on Ruby Outburst when not enough level for Ruby Rite")]
    public bool AddSwiftcastOnLowAOE { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Swiftcast on Garuda")]
    public bool AddSwiftcastOnGaruda { get; set; }

    [RotationConfig(CombatType.PvE, Name = "Use Swiftcast on Ruby Rite if you are not high enough level for Garuda")]
    public bool AddSwiftcastOnRuby { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Order")]
    public SummonOrderType SummonOrder { get; set; } = SummonOrderType.TopazEmeraldRuby;

    [RotationConfig(CombatType.PvE, Name = "Use Physick above level 30")]
    public bool Healbot { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Enable Potion Usage")]
    private static bool PotionUsageEnabled
    { get => ChurinPotions.Enabled; set => ChurinPotions.Enabled = value; }

    [RotationConfig(CombatType.PvE, Name = "Potion Usage Presets", Parent = nameof(PotionUsageEnabled))]
    private static PotionStrategy PotionUsagePresets
    { get => ChurinPotions.Strategy; set => ChurinPotions.Strategy = value; }

    [Range(0, 20, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "Use Opener Potion at minus (value in seconds)", Parent = nameof(PotionUsageEnabled))]
    private static float OpenerPotionTime { get => ChurinPotions.OpenerPotionTime; set => ChurinPotions.OpenerPotionTime = value; }

    [Range(0, 1200, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "Use 1st Potion at (value in seconds - leave at 0 if using in opener)", Parent = nameof(PotionUsagePresets), ParentValue = "Use custom potion timings")]
    private float FirstPotionTiming
    {
        get => _firstPotionTiming;
        set
        {
            _firstPotionTiming = value;
            UpdateCustomTimings();
        }
    }

    [Range(0, 1200, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "Use 2nd Potion at (value in seconds)", Parent = nameof(PotionUsagePresets), ParentValue = "Use custom potion timings")]
    private float SecondPotionTiming
    {
        get => _secondPotionTiming;
        set
        {
            _secondPotionTiming = value;
            UpdateCustomTimings();
        }
    }

    [Range(0, 1200, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "Use 3rd Potion at (value in seconds)", Parent = nameof(PotionUsagePresets), ParentValue = "Use custom potion timings")]
    private float ThirdPotionTiming
    {
        get => _thirdPotionTiming;
        set
        {
            _thirdPotionTiming = value;
            UpdateCustomTimings();
        }
    }
    
    [RotationConfig(CombatType.PvE, Name = "Enable Fight Presets? (Experimental)")]
    private bool EnableFightPresets {get; set;}

    [RotationConfig(CombatType.PvE, Name = "Choose a Fight", Parent = nameof(EnableFightPresets))]
    public FightPreset FightPresets { get; set; } = FightPreset.None;
     
    #endregion

    #region Tracking Properties
    public override void DisplayRotationStatus()
    {
        // Big Summon Status
        ImGui.Text("=== Big Summon Status ===");
        ImGui.Text($"Max GCDs in Big Summon: {BigSummonGCDLeft}");
        ImGui.Text($"In Big Summon Count: {InBigSummonCount}");

        ImGui.Separator();

        // Attunement Status
        ImGui.Text("=== Attunement Status ===");
        ImGui.Text($"Current Attunement: {(InIfrit ? "Ifrit" : InGaruda ? "Garuda" : InTitan ? "Titan" : "None")}");
        ImGui.Text($"Attunement Count: {AttunementCount}");
        ImGui.NewLine();
        ImGui.Text("Attunement Order:");
        ImGui.NewLine();
        if (_m4SOrderMap.Count > 0)
        {
            foreach (var kv in _m4SOrderMap.OrderBy(k => k.Key))
            {
                if (kv.Key < InBigSummonCount) continue;
                ImGui.SameLine();
                ImGui.Text($"#{kv.Key}:");
                ImGui.SameLine();
                var primals = GetPrimalsFromOrder(kv.Value);
                const float IconSize = 22f;
                for (var i = 0; i < primals.Length; i++)
                {
                    var act = primals[i];
                    if (act.GetTexture(out var tex) && tex.Handle != IntPtr.Zero)
                    {
                        ImGui.Image(tex.Handle, new Vector2(IconSize, IconSize));
                        if (ImGui.IsItemHovered()) ImGui.SetTooltip(act?.Name ?? kv.Value.ToString());
                    }
                    else
                    {
                        ImGui.Text(act?.Name ?? "?");
                    }
                    if (i < primals.Length - 1) ImGui.SameLine();
                }
                ImGui.NewLine();
            }
        }

        ImGui.Separator();

        //Ruin III Tracking
        ImGui.Text("=== Ruin III Tracking ===");
        ImGui.Text($"Ruin III Cast Count: {Ruin3Count}");
        ImGui.Text($"Just Used Ruin III: {JustUsedRuin3}");

        ImGui.Separator();

        // Timing & Potions
        ImGui.Text("=== Timing & Potions ===");
        ImGui.Text($"Can Late Weave: {CanLateWeave}");
        ImGui.Text($"Is Condition Met for Potion: {ChurinPotions.IsConditionMet()}");

        ImGui.Separator();

        // Configuration
        ImGui.Text("=== Configuration ===");
        ImGui.Text($"Order: {SummonOrder}");
        ImGui.Text($"Is Crimson Cyclone Target in Range: {CrimsonCyclonePvE.Target.Target.DistanceToPlayer() <= CrimsonCycloneDistance && HasIfritFavor}");
        ImGui.Text($"Add Crimson Cyclone: {AddCrimsonCyclone}");
        ImGui.Text($"Add Swiftcast on Garuda: {AddSwiftcastOnGaruda}");
        ImGui.Text($"Skip Attunement: {SkipAttunement}");
        
        ImGui.Text($"Fight Preset Check: {FightPresetCheck(FightPresets)}");
    }

    private const int M4SMaxOrderCount = 12;

    private IAction?[] GetPrimalsFromOrder(SummonOrderType order)
    {
        return order switch
        {
            SummonOrderType.TopazEmeraldRuby => [SummonTitanPvE, SummonGarudaPvE, SummonIfritPvE],
            SummonOrderType.TopazRubyEmerald => [SummonTitanPvE, SummonIfritPvE, SummonGarudaPvE],
            SummonOrderType.EmeraldTopazRuby => [SummonGarudaPvE, SummonTitanPvE, SummonIfritPvE],
            SummonOrderType.EmeraldRubyTopaz => [SummonGarudaPvE, SummonIfritPvE, SummonTitanPvE],
            SummonOrderType.RubyEmeraldTopaz => [SummonIfritPvE, SummonGarudaPvE, SummonTitanPvE],
            SummonOrderType.RubyTopazEmerald => [SummonIfritPvE, SummonTitanPvE, SummonGarudaPvE],
            _ => [SummonIfritPvE, SummonGarudaPvE, SummonTitanPvE],
        };
    }

    private static SummonOrderType GetOrderForCount(int count)
    {
        return count switch
        {
            <= 3 => SummonOrderType.TopazEmeraldRuby,
            4 => SummonOrderType.RubyTopazEmerald,
            <= 6 => SummonOrderType.RubyEmeraldTopaz,
            <= 9 => SummonOrderType.TopazRubyEmerald,
            10 => SummonOrderType.RubyTopazEmerald,
            11 => SummonOrderType.TopazRubyEmerald,
            _ => SummonOrderType.TopazEmeraldRuby
        };
    }


    #endregion

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        UpdateCounts();
        if (ChurinPotions.ShouldUsePotion(this, out var potionAct))
        {
            return potionAct;
        }

        if (SummonCarbunclePvE.CanUse(out var act)
            || HasSummon && remainTime <= RuinPvE.Info.CastTime + 0.8f && remainTime > RuinPvE.Info.CastTime && !InCombat
            && RuinPvE.CanUse(out act)
            || BigSummonTime(out act))
        {
            return act;
        }

        return base.CountDownAction(remainTime);
    }
    #endregion

    #region Additional oGCD Logic
    [RotationDesc(ActionID.LuxSolarisPvE)]
    protected override bool HealAreaAbility(IAction nextGCD, out IAction? act)
    {
        return LuxSolarisPvE.CanUse(out act) || base.HealAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.RekindlePvE)]
    protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (!HasRadiantAegis)
        {
            return RadiantAegisPvE.CanUse(out act);
        }

        return RekindlePvE.CanUse(out act)
               || base.HealSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.LuxSolarisPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (!HasRadiantAegis)
        {
            return RadiantAegisPvE.CanUse(out act);
        }

        if (HostileTarget != null && !HostileTarget.HasStatus(false, StatusID.Addle))
        {
            return AddlePvE.CanUse(out act);
        }

        return base.DefenseAreaAbility(nextGCD, out act);
    }
    #endregion

    #region oGCD Logic
    [RotationDesc(ActionID.LuxSolarisPvE)]
    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        if (StatusHelper.PlayerWillStatusEndGCD(3, 0, true, StatusID.RefulgentLux))
        {
            if (LuxSolarisPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (StatusHelper.PlayerWillStatusEndGCD(2, 0, true, StatusID.FirebirdTrance))
        {
            if (RekindlePvE.CanUse(out act))
            {
                return true;
            }
        }

        if (StatusHelper.PlayerWillStatusEndGCD(3, 0, true, StatusID.FirebirdTrance))
        {
            if (RekindlePvE.CanUse(out act))
            {
                if (RekindlePvE.Target.Target == LowestHealthPartyMember)
                {
                    return true;
                }
            }
        }
        return base.GeneralAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        return  TryUseSearingFlash(out act)
            || TryUseAetherflow(out act)
            || MountainBusterPvE.CanUse(out act)
            || base.AttackAbility(nextGCD, out act);
    }

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (EnableFightPresets)
        {
            PresetEnabled(FightPresets);
        }

        UpdateCounts();
        return ChurinPotions.ShouldUsePotion(this, out act)
        || TryUseSwiftcast(nextGCD, out act)
        || TryUseSearingLight(out act)
        || TryUseEnergyDrain(out act)
        || TryUseEnkindle(out act)
        || TryUseAstralFlow(out act)
        || base.EmergencyAbility(nextGCD, out act);
    }

    #endregion

    #region GCD Logic
    
    [RotationDesc(ActionID.CrimsonCyclonePvE)]
    protected override bool MoveForwardGCD(out IAction? act)
    {
        return CrimsonCyclonePvE.CanUse(out act) || base.MoveForwardGCD(out act);
    }

    [RotationDesc(ActionID.PhysickPvE)]
    protected override bool HealSingleGCD(out IAction? act)
    {
        if ((Healbot || DataCenter.PlayerSyncedLevel() <= 30) && PhysickPvE.CanUse(out act))
        {
            return true;
        }
        return base.HealSingleGCD(out act);
    }

    protected override bool GeneralGCD(out IAction? act)
    {
        if (SummonCarbunclePvE.CanUse(out act))
        {
            return true;
        }

        return BigSummonTime(out act)
            || PrimalsFiller(out act)
            || PrimalsTime(out act)
            || SummonFiller(out act)
            || base.GeneralGCD(out act);
    }
    
    #endregion

    #region Extra Methods

    #region Summon GCD

    #region Big Summons

    private bool BigSummonTime(out IAction? act)
        {
            act = null;
            if (!SummonBahamutPvE.IsEnabled || !SummonSolarBahamutPvE.IsEnabled || !SummonPhoenixPvE.IsEnabled)
            {
                return false;
            }

            if (SummonSolarBahamutPvE.EnoughLevel && IsSolarBahamutReady)
            {
                return SummonSolarBahamutPvE.CanUse(out act);
            }

            if (SummonPhoenixPvE.EnoughLevel && IsPhoenixReady)
            {
                return SummonPhoenixPvE.CanUse(out act);
            }

            switch (SummonBahamutPvE.EnoughLevel)
            {
                case true when IsBahamutReady:
                    return SummonBahamutPvE.CanUse(out act);

                case false when DreadwyrmTrancePvE.EnoughLevel:
                    return DreadwyrmTrancePvE.CanUse(out act);
            }

            if (!DreadwyrmTrancePvE.EnoughLevel && AetherchargePvE.EnoughLevel)
            {
                return AetherchargePvE.CanUse(out act);
            }

            return false;
        }
    private bool SummonFiller(out IAction? act)
        {
            act = null;
            var solarBahamutReadySoon = IsSolarBahamutReady && SummonSolarBahamutPvE.Cooldown.WillHaveOneChargeGCD(1) && SummonSolarBahamutPvE.IsEnabled;
            var bahamutReadySoon = IsBahamutReady && SummonBahamutPvE.Cooldown.WillHaveOneChargeGCD(1) && SummonBahamutPvE.IsEnabled;
            var phoenixReadySoon = IsPhoenixReady && SummonPhoenixPvE.Cooldown.WillHaveOneChargeGCD(1) && SummonPhoenixPvE.IsEnabled;
            var bigSummonReadySoon = solarBahamutReadySoon || bahamutReadySoon || phoenixReadySoon;

            if ((!InBigSummon && AnyPrimalReady && NoAttunement) || HasAnyFavor || HasAnyAttunement)
            {
                return false;
            }

            if (InBigSummon)
            {
                if (InPhoenix)
                {
                    return BrandOfPurgatoryPvE.CanUse(out act)
                    || FountainOfFirePvE.CanUse(out act);
                }

                if (InBahamut)
                {
                    return AstralFlarePvE.CanUse(out act)
                    || AstralImpulsePvE.CanUse(out act);
                }

                if (InSolarBahamut)
                {
                    return UmbralFlarePvE.CanUse(out act)
                    || UmbralImpulsePvE.CanUse(out act);
                }
            }
            else
            {
                if (IsMoving)
                {
                    if (HasFurtherRuin)
                    {
                        return RuinIvPvE.CanUse(out act);
                    }

                    if (Ruin3Count < 1)
                    {
                        return OutburstPvE.CanUse(out act)
                               || RuinIiiPvE.CanUse(out act)
                               || RuinIiPvE.CanUse(out act)
                               || RuinPvE.CanUse(out act);
                    }
                }
                else
                {
                    if (HasFurtherRuin)
                    {
                        if (!bigSummonReadySoon && Ruin3Count < 1)
                        {
                            return OutburstPvE.CanUse(out act)
                            || RuinIiiPvE.CanUse(out act)
                            || RuinIiPvE.CanUse(out act)
                            || RuinPvE.CanUse(out act);
                        }

                        return RuinIvPvE.CanUse(out act);
                    }

                    if (!bigSummonReadySoon || Ruin3Count < 1)
                    {
                        return OutburstPvE.CanUse(out act)
                               || RuinIiiPvE.CanUse(out act)
                               || RuinIiPvE.CanUse(out act)
                               || RuinPvE.CanUse(out act);
                    }
                }
            }
            return false;
        }
    private int BigSummonGCDLeft
        {
            get
            {
                if (InBigSummon)
                {
                    var maxImpulse = Math.Abs(SummonTimer / (double)RuinPvE.Cooldown.RecastTime);
                    {
                        if (maxImpulse > 0)
                        {
                            return (int)(maxImpulse + 1);
                        }
                    }
                }
                return 0;
            }
        }

    #endregion

    #region Primals

    private bool PrimalsTime(out IAction? act)
        {
            act = null;

            if (((InBigSummon && !SummonTimeEndAfterGCD())
            || (NoAttunement && (HasGarudaFavor || HasIfritFavor || HasTitanFavor))
            || (AttunementCount > 0 && !AttunmentTimeEndAfterGCD())
            || NoPrimalReady) && !SkipAttunement)
            {
                return false;
            }

            if (UseRuin3 && Ruin3Count < 1)
            {
                return RuinIiiPvE.CanUse(out act);
            }

            if (UseRuin4 && HasFurtherRuin)
            {
                return RuinIvPvE.CanUse(out act);
            }

            return SummonOrder switch
            {
                SummonOrderType.TopazRubyEmerald =>
                    TitanTime(out act)
                    || IfritTime(out act)
                    || GarudaTime(out act),

                SummonOrderType.TopazEmeraldRuby =>
                    TitanTime(out act)
                    || GarudaTime(out act)
                    || IfritTime(out act),

                SummonOrderType.EmeraldTopazRuby =>
                    GarudaTime(out act)
                    || TitanTime(out act)
                    || IfritTime(out act),

                SummonOrderType.EmeraldRubyTopaz =>
                    GarudaTime(out act)
                    || IfritTime(out act)
                    || TitanTime(out act),

                SummonOrderType.RubyEmeraldTopaz =>
                    IfritTime(out act)
                    || GarudaTime(out act)
                    || TitanTime(out act),

                SummonOrderType.RubyTopazEmerald =>
                    IfritTime(out act)
                    || TitanTime(out act)
                    || GarudaTime(out act),

                _ => false,
            };
        }
    private bool PrimalsFiller(out IAction? act)
        {
            act = null;

            if (InBigSummon)
            {
                return false;
            }

            if (SkipAttunement)
            {
                return PrimalsTime(out act);
            }

            return TitanAttunement(out act)
            || GarudaAttunement(out act)
            || IfritAttunement(out act);
        }
    private bool GemshineTime(out IAction? act)
        {
            act = null;
            if (InBigSummon || AttunementCount < 1)
            {
                return false;
            }

            if (InIfrit)
            {
                return RubyRitePvE.CanUse(out act)
                || RubyRuinIiiPvE.CanUse(out act)
                || RubyRuinIiPvE.CanUse(out act)
                || RubyRuinPvE.CanUse(out act);
            }

            if (InGaruda)
            {
                return EmeraldRitePvE.CanUse(out act)
                || EmeraldRuinIiiPvE.CanUse(out act)
                || EmeraldRuinIiPvE.CanUse(out act)
                || EmeraldRuinPvE.CanUse(out act);
            }

            if (InTitan)
            {
                return TopazRitePvE.CanUse(out act)
                || TopazRuinIiiPvE.CanUse(out act)
                || TopazRuinIiPvE.CanUse(out act)
                || TopazRuinPvE.CanUse(out act);
            }

            return false;
        }
    private bool PreciousBrillianceTime(out IAction? act)
        {
            act = null;
            if (InBigSummon || AttunementCount < 1)
            {
                return false;
            }

            if (InIfrit)
            {
                return RubyCatastrophePvE.CanUse(out act)
                || RubyDisasterPvE.CanUse(out act)
                || RubyOutburstPvE.CanUse(out act);
            }
            if (InGaruda)
            {
                return EmeraldCatastrophePvE.CanUse(out act)
                || EmeraldDisasterPvE.CanUse(out act)
                || EmeraldOutburstPvE.CanUse(out act);
            }
            if (InTitan)
            {
                return TopazCatastrophePvE.CanUse(out act)
                || TopazDisasterPvE.CanUse(out act)
                || TopazOutburstPvE.CanUse(out act);
            }
            return false;
        }

    #region Titan

    private bool TitanTime(out IAction? act)
        {
            act = null;
            if (!IsTitanReady || HasGarudaFavor || HasIfritFavor)
            {
                return false;
            }
            return SummonTitanPvE.CanUse(out act)
            || SummonTitanIiPvE.CanUse(out act)
            || SummonTopazPvE.CanUse(out act);
        }
    private bool TitanAttunement(out IAction? act)
        {
            act = null;
            if (InIfrit || InGaruda || HasGarudaFavor || HasIfritFavor || !HasTitanFavor && AttunementCount < 1)
            {
                return false;
            }

            return PreciousBrillianceTime(out act)
            || GemshineTime(out act);
        }

    #endregion

    #region Garuda

    private bool GarudaTime(out IAction? act)
        {
            act = null;
            if (!IsGarudaReady || HasTitanFavor || HasIfritFavor)
            {
                return false;
            }
            return SummonGarudaPvE.CanUse(out act)
            || SummonGarudaIiPvE.CanUse(out act)
            || SummonEmeraldPvE.CanUse(out act);
        }
    private bool GarudaAttunement(out IAction? act)
        {
            act = null;
            if (InTitan || InIfrit || HasIfritFavor || HasTitanFavor || (!HasGarudaFavor && AttunementCount < 1))
            {
                return false;
            }

            if (EnableFightPresets)
            {
                PresetEnabled(FightPresets);

                if (UseRuin3 && Ruin3Count < 1)
                {
                    return RuinIiiPvE.CanUse(out act);
                }

                if (UseRuin4 && HasFurtherRuin)
                {
                    return RuinIvPvE.CanUse(out act);
                }
            }

            return GarudaStrategy(IsMoving, out act);
        }
    private bool GarudaStrategy(bool isMoving, out IAction? act)
        {
            act = null;
            var canSwiftcastSlipstream = AddSwiftcastOnGaruda && (HasSwift || !SwiftcastPvE.Cooldown.IsCoolingDown);

            if (isMoving)
            {
                if (HasGarudaFavor)
                {
                    if (canSwiftcastSlipstream)
                    {
                        return SlipstreamPvE.CanUse(out act, skipCastingCheck: true);
                    }

                    if (AttunementCount > 0)
                    {
                        return PreciousBrillianceTime(out act) || GemshineTime(out act);
                    }

                    if (HasFurtherRuin)
                    {
                        return RuinIvPvE.CanUse(out act);
                    }

                }
            }
            else
            {
                if (HasGarudaFavor && (canSwiftcastSlipstream || !AddSwiftcastOnGaruda))
                {
                    return SlipstreamPvE.CanUse(out act, skipCastingCheck: AddSwiftcastOnGaruda);
                }
            }

            if (AttunementCount > 0)
            {
                return PreciousBrillianceTime(out act) || GemshineTime(out act);
            }

            return false;
        }

    #endregion

    #region Ifrit

    private bool IfritTime(out IAction? act)
        {
            act = null;
            if (!IsIfritReady || HasGarudaFavor || HasTitanFavor)
            {
                return false;
            }
            return SummonIfritPvE.CanUse(out act)
            || SummonIfritIiPvE.CanUse(out act)
            || SummonRubyPvE.CanUse(out act);
        }
    private bool IfritAttunement(out IAction? act)
        {
            act = null;

            if (InTitan || InGaruda || HasGarudaFavor || HasTitanFavor || (!HasIfritFavor && !HasCrimsonStrike && AttunementCount < 1))
            {
                return false;
            }

            if (EnableFightPresets)
            {
                PresetEnabled(FightPresets);
            }

            return HasCrimsonStrike ? CrimsonStrikePvE.CanUse(out act) : IfritStrategy(IsMoving, out act);
        }
    private bool IfritStrategy(bool isMoving, out IAction? act)
       {
           act = null;
           var canUseCrimsonCyclone = AddCrimsonCyclone || CrimsonCyclonePvE.Target.Target.DistanceToPlayer() <= CrimsonCycloneDistance;
           var cycloneAvailable = canUseCrimsonCyclone && HasIfritFavor;
           var cycloneMoveAllowed = AddCrimsonCycloneMoving;
           var cycloneBaseCondition = (UseCycloneFirst && AttunementCount > 1)
                                       || (UseOneAttunement && AttunementCount == 1)
                                       || (AttunementCount < 1 && UseBothAttunements && !UseOneAttunement);
           var cycloneMoveCondition = cycloneAvailable && cycloneMoveAllowed && cycloneBaseCondition;
		    var usedOneAttunement = Player != null && IsCasting && Player.CastActionId == (uint)ActionID.RubyRitePvE && !IsLastGCD(ActionID.RubyRitePvE) && AttunementCount >= 1;
		switch (isMoving)
           {
               case true:
               {
                   if (EnableFightPresets)
                   {
                       if (UseRuin4 && HasFurtherRuin)
                       {
                           return RuinIvPvE.CanUse(out act);
                       }

                       if (cycloneMoveCondition)
                       {
                           return CrimsonCyclonePvE.CanUse(out act);
                       }
                   }

                   if (canUseCrimsonCyclone && AddCrimsonCycloneMoving)
                   {
                       return CrimsonCyclonePvE.CanUse(out act);
                   }

                   if (HasFurtherRuin)
                   {
                       return RuinIvPvE.CanUse(out act);
                   }

                   break;
               }

               case false:
               {
                   if (EnableFightPresets)
                   {
                       PresetEnabled(FightPresets);

                       if (UseRuin3 && Ruin3Count < 1)
                       {
                           return RuinIiiPvE.CanUse(out act);
                       }

                       if (UseRuin4 && HasFurtherRuin)
                       {
                           return RuinIvPvE.CanUse(out act);
                       }

                       if (HasIfritFavor)
                       {
                           if (canUseCrimsonCyclone && UseCycloneFirst)
                           {
                               return CrimsonCyclonePvE.CanUse(out act);
                           }

                           if ((UseOneAttunement && usedOneAttunement && (UseBothAttunements || !UseBothAttunements))
                               || (AttunementCount < 1 && UseBothAttunements && !UseOneAttunement))
                           {
                               if (canUseCrimsonCyclone)
                               {
                                   return CrimsonCyclonePvE.CanUse(out act);
                               }
                           }
                       }

                       if (UseBothAttunements && AttunementCount > 0
                           || UseOneAttunement && (AttunementCount == 2 || !UseBothAttunements && AttunementCount is <= 2 and >=1 && !IsCasting)
                           || UseOneAttunement && UseBothAttunements && !HasIfritFavor && !HasCrimsonStrike && AttunementCount == 1)
                       {
                           return PreciousBrillianceTime(out act) || GemshineTime(out act);
                       }

                       if (UseOneAttunement && !UseBothAttunements && !HasIfritFavor && !HasCrimsonStrike && AttunementCount == 1)
                       {
                           return false;
                       }
                   }

                   if (AttunementCount > 0)
                   {
                       return PreciousBrillianceTime(out act) || GemshineTime(out act);
                   }

                   if (HasIfritFavor && canUseCrimsonCyclone)
                   {
                       return CrimsonCyclonePvE.CanUse(out act);
                   }

                   if (HasFurtherRuin)
                   {
                       return RuinIvPvE.CanUse(out act);
                   }

                   if (Ruin3Count < 1)
                   {
                       return RuinIiiPvE.CanUse(out act);
                   }

                   break;
               }
           }
           return false;
       }

    #endregion

    #endregion

    #endregion

    #region oGCDs
    private bool TryUseEnergyDrain(out IAction? act)
    {
        act = null;
        if (HasAetherflowStacks)
        {
            return false;
        }

        if (((SummonSolarBahamutPvE.EnoughLevel && InSolarBahamut 
        || !SummonSolarBahamutPvE.EnoughLevel && (InBahamut|| InPhoenix)) && BigSummonGCDLeft <= 4)
        || (SummonSolarBahamutPvE.EnoughLevel && (InBahamut || InPhoenix))
        || (HasSearingLight && !InBigSummon && !NoAttunement))
        {
            return EnergySiphonPvE.CanUse(out act)
            || EnergyDrainPvE.CanUse(out act);
        }
        return false;
    }

    private bool TryUseSearingLight(out IAction? act)
    {
        act = null;
        if (!BahamutBurst)
        {
            return false;
        }

        if (InBigSummon && BigSummonGCDLeft <= 5)
        {
            return SearingLightPvE.CanUse(out act);
        }

        return false;
    }

    private bool TryUseEnkindle(out IAction? act)
    {
        act = null;
        if (!InBigSummon)
        {
            return false;
        }
        if (BigSummonGCDLeft <= 3)
        {
            return EnkindleSolarBahamutPvE.CanUse(out act) 
            || EnkindleBahamutPvE.CanUse(out act) 
            || EnkindlePhoenixPvE.CanUse(out act);
        }
        return false;
    }

    private bool TryUseAstralFlow(out IAction? act)
    {
        act = null;
        if (!InBigSummon)
        {
            return false;
        }
        if (BigSummonGCDLeft <= 3)
        {
            return SunflarePvE.CanUse(out act) 
            || DeathflarePvE.CanUse(out act);
        }
        return false;
    }

    private bool TryUseSearingFlash(out IAction? act)
    {
        act = null;
        if (!HasSearingLight)
        {
            return false;
        }

        if ((BigSummonGCDLeft <= 2 || !InBigSummon) && SearingFlashPvE.CanUse(out act))
        {
            return true;
        }
        return false;
    }

    private bool TryUseAetherflow(out IAction? act)
    {
        act = null;
        if (!HasAetherflowStacks)
        {
            return false;
        }

        if (HasSearingLight && (InBigSummon && BigSummonGCDLeft <= 4 || !InBigSummon) 
        || !SearingLightPvE.EnoughLevel)
        {
            return PainflarePvE.CanUse(out act)
            || NecrotizePvE.CanUse(out act)
            || FesterPvE.CanUse(out act);
        }
        return false;
    }

    private bool TryUseSwiftcast(IAction nextGCD, out IAction? act)
    {
        if (SwiftcastPvE.CanUse(out act))
        {
            if (nextGCD.IsTheSameTo(false, ResurrectionPvE))
            {
                return AddSwiftcastOnRaise;
            }

            if (nextGCD.IsTheSameTo(false, RubyRuinPvE, RubyRuinIiPvE, RubyRuinIiiPvE))
            {
                return AddSwiftcastOnLowSt && !RubyRitePvE.EnoughLevel;
            }

            if (nextGCD.IsTheSameTo(false, RubyOutburstPvE))
            {
                return AddSwiftcastOnLowAOE && !RubyRitePvE.EnoughLevel;
            }

            if (nextGCD.IsTheSameTo(false, RubyRitePvE))
            {
                return AddSwiftcastOnRuby && ElementalMasteryTrait.EnoughLevel && !InGaruda;
            }

            if (nextGCD.IsTheSameTo(false, SlipstreamPvE))
            {
                return AddSwiftcastOnGaruda && ElementalMasteryTrait.EnoughLevel && InGaruda;
            }

            if (nextGCD.IsTheSameTo(false, RubyRitePvE, RubyCatastrophePvE) && IsMoving)
            {
                return !HasFurtherRuin && InIfrit && (!StatusHelper.PlayerHasStatus(true, StatusID.IfritsFavor) || !AddCrimsonCyclone && CrimsonCyclonePvE.Target.Target.DistanceToPlayer() > CrimsonCycloneDistance);
            }
        }
        return false;
    }
    #endregion

    #region Miscellaneous
    public override bool CanHealSingleSpell
    {
        get
        {
            var aliveHealerCount = 0;
            var healers = PartyMembers.GetJobCategory(JobRole.Healer);
            foreach (var h in healers)
            {
                if (!h.IsDead)
                    aliveHealerCount++;
            }

            return base.CanHealSingleSpell && aliveHealerCount == 0;
        }
    }    
    
    #region Potions
    private void UpdateCustomTimings()
    {
        ChurinPotions.CustomTimings = new Potions.CustomTimingsData
        {
            Timings = [FirstPotionTiming, SecondPotionTiming, ThirdPotionTiming]
        };
    }
    
    private static readonly ChurinSMNPotions ChurinPotions = new();
    
    private float _firstPotionTiming;
   
    private float _secondPotionTiming;
    
    private float _thirdPotionTiming;
    
    /// <summary>
    /// SMN-specific potion manager that extends base potion logic with job-specific conditions.
    /// </summary>
    private class ChurinSMNPotions : Potions
    {
        public override bool IsConditionMet()
        {
            if (InSolar || InBahamut || InPhoenix)
            {
                return CanLateWeave;
            }

            return false;
        }

        protected override bool IsTimingValid(float timing)
        {
            if (timing > 0 && DataCenter.CombatTimeRaw >= timing && DataCenter.CombatTimeRaw - timing <= TimingWindowSeconds)
            {
                return true;
            }

            // Check opener timing: if it's an opener potion and countdown is within configured time
            float countDown = Service.CountDownTime;
            if (IsOpenerPotion(timing) && countDown > 0 && ChurinSMN.OpenerPotionTime > 0)
            {
                return countDown <= ChurinSMN.OpenerPotionTime;
            }

            if (IsOpenerPotion(timing) && ChurinSMN.OpenerPotionTime == 0 && DataCenter.CombatTimeRaw < TimingWindowSeconds)
            {
                return IsConditionMet();
            }

            return false;
        }
    
    }
    
    #endregion
    
    #region Experimental Methods
    
    private bool JustInBigSummon {get; set;}
    
    private int Ruin3Count { get; set;}
    
    private bool JustUsedRuin3 { get; set;}
    
    private bool UseRuin4 { get; set;}
    
    private int InBigSummonCount{ get; set;}
    
    private bool UseRuin3 { get; set;}
    
    private bool CrimsonCycloneTargetTooFar => (CrimsonCyclonePvE.Target.Target.DistanceToPlayer() > CrimsonCycloneDistance) && HasIfritFavor;
    
    private bool SkipAttunement { get; set;}

    private bool UseBothAttunements { get; set;}

    private bool UseOneAttunement { get; set;}

    private bool UseCycloneFirst { get; set;}
    
    private readonly Dictionary<int, SummonOrderType> _m4SOrderMap = [];
    
    private static bool FightPresetCheck(FightPreset preset)
    {
        ushort territory;
        switch (preset)
        {
            case FightPreset.CruiserweightM4S: 
            territory = 1263;
            break;

            case FightPreset.CruiserweightM3S: 
            territory = 1261;
            break;

            case FightPreset.CruiserweightM2S: 
            territory = 1259;
            break;

            case FightPreset.CruiserweightM1S: 
            territory = 1257;
            break;

            case FightPreset.None:
            default:
            return false;
        }

        return IsInTerritory(territory) || CurrentTarget != null && CurrentTarget.IsDummy();
    }
    
    private void SetIfritAttunementFlags(bool both, bool one, bool cycloneFirst, bool addCyclone)
    {
        UseBothAttunements = both;
        UseOneAttunement = one;
        UseCycloneFirst = cycloneFirst;
        AddCrimsonCyclone = addCyclone;
    }
    
    private void SetRuin3Flag(bool ruin3Flags)
    {
        UseRuin3 = ruin3Flags;
    }
    
    private void SetRuin4Flag(bool ruin4Flags)
    {
        UseRuin4 = ruin4Flags;
    }
    
    private void SetSkipAttunementFlag(bool skipAttunementFlag)
    {
        SkipAttunement = skipAttunementFlag;
    }

    private void ResetCounts()
    {
        InBigSummonCount = 0;
        JustInBigSummon = false;
        _m4SOrderMap.Clear();
        Ruin3Count = 0;
        UseBothAttunements = true;
        UseOneAttunement = false;
        UseCycloneFirst = false;
        SkipAttunement = false;
        UseRuin3 = false;
        UseRuin4 = false; 
    }

    private void UpdateCounts()
    {
        // Avoid updating while out of combat
        if (!InCombat)
        {   
            ResetCounts();
            return;
        }

        if (InBigSummon && !JustInBigSummon)
        {
            InBigSummonCount++;
            Ruin3Count = 0;
            var keysToRemove = _m4SOrderMap.Keys.Where(k => k < InBigSummonCount).ToList();
            foreach (var k in keysToRemove)
            {
                _m4SOrderMap.Remove(k);
            }
        }
        
        if (IsLastGCD(ActionID.RuinIiiPvE) && !JustUsedRuin3)
        {
            Ruin3Count++;
            JustUsedRuin3 = true;
        }
        
        if (!IsLastGCD(ActionID.RuinIiiPvE))
        {
            JustUsedRuin3 = false;
        }
        // Persist current state for the next check
        JustInBigSummon = InBigSummon;
    }

    private void PresetEnabled(FightPreset preset)
    {
        if (FightPresetCheck(preset))
        {
           switch(preset)
            {
                case FightPreset.CruiserweightM4S:
                    M4SPreset();
                    break;
                case FightPreset.CruiserweightM3S:
                case FightPreset.CruiserweightM2S:
                case FightPreset.CruiserweightM1S:
                    break;
            }
        }
        else
        {
            DefaultPreset();
        }
    }    
    
    #region Default Preset Logic
    
    private void DefaultPreset()
    {
        if (!InCombat)
        {
            return;
        }

        UseBothAttunements = true;
        UseOneAttunement = false;
        UseCycloneFirst = false;
        SkipAttunement = false;
        UseRuin3 = false;
        UseRuin4 = false;
    }
    
    #endregion
    
    #region M4S Preset Logic
    
    private void M4SPreset()
    {
        if (!InCombat)
        {
            return;
        }

        if (!AddSwiftcastOnGaruda)
        {
            AddSwiftcastOnGaruda = true;
        }

        if (InBigSummon)
        {
            M4SPrimalsOrder();
        }
        M4SInPrimals();
    }
    
    private void M4SPrimalsOrder()
    {
       if (InBigSummonCount < 0) return;

        for (var c = InBigSummonCount; c <= M4SMaxOrderCount; c++)
        {
            var order = GetOrderForCount(c);
            _m4SOrderMap[c] = order;
        }

        var currentOrder = GetOrderForCount(InBigSummonCount);
        if (SummonOrder != currentOrder)
        {
            SummonOrder = currentOrder;
        }
    }

    private void M4SInPrimals()
    {
		var isCastingRubyRite = Player != null && Player.CastActionId == (uint)ActionID.RubyRitePvE;
		var castAlmostFinished = Player != null && IsCasting && isCastingRubyRite && WeaponRemain < 1f;
		var attunementUsedUp = (HasIfritFavor && !InIfrit) || (Player != null && IsLastGCD(ActionID.RubyRitePvE) && castAlmostFinished && AttunementCount <= 1);
		var hasOneAttunementLeft = (castAlmostFinished && AttunementCount <= 2) || (Player != null && IsLastGCD(ActionID.RubyRitePvE) && AttunementCount == 1);
		var crimsonCycloneTargetInRange = CrimsonCyclonePvE.Target.Target.DistanceToPlayer() <= CrimsonCycloneDistance;
		var defaultCycloneCondition = (CrimsonCycloneTargetTooFar || crimsonCycloneTargetInRange) && attunementUsedUp;

		UseRuin3 = false;
        UseRuin4 = false;
        AddCrimsonCyclone = false;
        SkipAttunement = false;
        UseCycloneFirst = false;
    
        switch (InBigSummonCount)
        {
            case <= 1 or 3:
                SetRuin3Flag(InGaruda && (!HasGarudaFavor || IsLastGCD(ActionID.SlipstreamPvE)) && Ruin3Count < 1);
                SetIfritAttunementFlags(true, false, false, defaultCycloneCondition);
                break;
            case 4:
                SetIfritAttunementFlags(true, false, false, defaultCycloneCondition);
                SetRuin3Flag(!IsIfritReady && !InIfrit && IsLastGCD(ActionID.CrimsonStrikePvE) && IsTitanReady && IsGarudaReady && Ruin3Count < 1);
                SetRuin4Flag(InGaruda && !HasGarudaFavor && HasFurtherRuin);
                break;
            case 5:
                SetIfritAttunementFlags(false, true, false, hasOneAttunementLeft && (CrimsonCycloneTargetTooFar || crimsonCycloneTargetInRange));
                SetSkipAttunementFlag((InIfrit &&  !HasIfritFavor && !HasCrimsonStrike && AttunementCount == 1 && IsGarudaReady && IsLastGCD(ActionID.CrimsonStrikePvE)) || (InGaruda && !HasGarudaFavor && AttunementCount < 3 && IsTitanReady));
                break;
            case 6:
                SetRuin3Flag(attunementUsedUp && Ruin3Count < 1);
                SetIfritAttunementFlags(true, false, false, defaultCycloneCondition && Ruin3Count == 1);
                break;   
            case 8:
                SetRuin3Flag(!InIfrit && !HasCrimsonStrike && !HasIfritFavor && IsLastGCD(ActionID.CrimsonStrikePvE) && !IsIfritReady && IsGarudaReady && IsTitanReady && Ruin3Count < 1);
                break;
            case 9:
                SetIfritAttunementFlags(true, true, false, hasOneAttunementLeft && (CrimsonCycloneTargetTooFar || crimsonCycloneTargetInRange));
                if (InGaruda)
                {
                    if (!HasGarudaFavor && AttunementCount > 3)
                    {
                        SetRuin3Flag(Ruin3Count < 1);
                        SetRuin4Flag(HasFurtherRuin);
                    }
                }
                break;
            case 10:
                SetIfritAttunementFlags(true, true, false, hasOneAttunementLeft && (CrimsonCycloneTargetTooFar || crimsonCycloneTargetInRange));
                SetRuin3Flag(attunementUsedUp && Ruin3Count < 1 && IsTitanReady);
                SetRuin4Flag(InGaruda && !HasGarudaFavor && AttunementCount > 2 && HasFurtherRuin);
                break;
            case 11:
                SetIfritAttunementFlags(true, false, false, defaultCycloneCondition && Ruin3Count == 1);
                SetRuin3Flag(attunementUsedUp && Ruin3Count < 1);
                SetRuin4Flag(InGaruda && !HasGarudaFavor && AttunementCount > 3 && HasFurtherRuin);
                break;
            case 12:
                SetRuin3Flag(InGaruda && !HasGarudaFavor && AttunementCount > 3 && Ruin3Count < 1);
                SetIfritAttunementFlags(true, false, true, CrimsonCycloneTargetTooFar || crimsonCycloneTargetInRange);
                break;
            default:
                SetIfritAttunementFlags(true, false, false, defaultCycloneCondition);
                break;
        }
    }
    
    #endregion
    
    #endregion
    
    #endregion

    #endregion

}   

