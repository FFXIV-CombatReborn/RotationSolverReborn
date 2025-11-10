using System.ComponentModel;
using Dalamud.Game.ClientState.JobGauge.Types;
using ECommons.DalamudServices;

namespace RotationSolver.ExtraRotations.Magical;

[Rotation("Churin SMN", CombatType.PvE, GameVersion = "7.35")]
[SourceCode(Path = "main/ExtraRotations/Magical/ChurinSMN.cs")]

public sealed class ChurinSMN : SummonerRotation
{
    #region Properties
    private bool InBigSummon => !SummonBahamutPvE.EnoughLevel || InBahamut || InPhoenix || InSolarBahamut;
    private static bool InSolar => Player.Level == 100 ? !InBahamut && !InPhoenix && InSolarBahamut : InBahamut && !InPhoenix;
    private bool BahamutBurst => (SummonSolarBahamutPvE.EnoughLevel && InSolarBahamut) || (!SummonSolarBahamutPvE.EnoughLevel && InBahamut) || !SummonBahamutPvE.EnoughLevel;
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

        [Description("Ruby-Emerald-Topaz")] RubyEmeraldTopaz,
    }

    [RotationConfig(CombatType.PvE, Name = "Use GCDs to heal. (Ignored if there are no healers alive in party)")]
    public bool GCDHeal { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Crimson Cyclone at any range, regardless of saftey use with caution (Enabling this ignores the below distance setting).")]
    public bool AddCrimsonCyclone { get; set; } = true;

    [Range(1, 20, ConfigUnitType.Yalms)]
    [RotationConfig(CombatType.PvE, Name = "Max distance you can be from the target for Crimson Cyclone use")]
    public float CrimsonCycloneDistance { get; set; } = 3.0f;

    [RotationConfig(CombatType.PvE, Name = "Use Crimson Cyclone when moving")]
    public bool AddCrimsonCycloneMoving { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Swiftcast on ressurection")]
    public bool AddSwiftcastOnRaise { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Swiftcast on Ruby Ruin when not enough level for Ruby Rite")]
    public bool AddSwiftcastOnLowST { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Swiftcast on Ruby Outburst when not enough level for Ruby Rite")]
    public bool AddSwiftcastOnLowAOE { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Swiftcast on Garuda")]
    public bool AddSwiftcastOnGaruda { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Swiftcast on Ruby Rite if you are not high enough level for Garuda")]
    public bool AddSwiftcastOnRuby { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Order")]
    public SummonOrderType SummonOrder { get; set; } = SummonOrderType.TopazEmeraldRuby;

    [RotationConfig(CombatType.PvE, Name = "Use radiant on cooldown. But still keeping one charge")]
    public bool RadiantOnCooldown { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use this if there's no other raid buff in your party")]
    public bool SecondTypeOpenerLogic { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Physick above level 30")]
    public bool Healbot { get; set; } = false;

    private static readonly ChurinSMNPotions _churinPotions = new();

    private float _firstPotionTiming = 0;
    private float _secondPotionTiming = 0;
    private float _thirdPotionTiming = 0;

    [RotationConfig(CombatType.PvE, Name = "Enable Potion Usage")]
    private bool PotionUsageEnabled
    { get => _churinPotions.Enabled; set => _churinPotions.Enabled = value; }

    [RotationConfig(CombatType.PvE, Name = "Potion Usage Presets", Parent = nameof(PotionUsageEnabled))]
    private PotionStrategy PotionUsagePresets
    { get => _churinPotions.Strategy; set => _churinPotions.Strategy = value; }

    [Range(0,20, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "Use Opener Potion at minus (value in seconds)", Parent = nameof(PotionUsageEnabled))]
    private static float OpenerPotionTime { get => _churinPotions.OpenerPotionTime; set => _churinPotions.OpenerPotionTime = value; }

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

    private void UpdateCustomTimings()
    {
        _churinPotions.CustomTimings = new Potions.CustomTimingsData
        {
            Timings = [FirstPotionTiming, SecondPotionTiming, ThirdPotionTiming]
        };
    }

    #endregion

    #region Tracking Properties
    public override void DisplayRotationStatus()
    {
        ImGui.Text($"EnergyDrainPvE: Is Cooling Down: {EnergyDrainPvE.Cooldown.IsCoolingDown}");
        ImGui.Text($"Max GCDs in Big Summon: {BigSummonGCDLeft}");
        ImGui.Text($"Is Condition Met for Potion: {_churinPotions.IsConditionMet()}");
        ImGui.Text($"Can Late Weave: {CanLateWeave}");
    }
    #endregion

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (_churinPotions.ShouldUsePotion(this, out var potionAct))
        {
            return potionAct;
        }

        if (SummonCarbunclePvE.CanUse(out IAction? act))
        {
            return act;
        }
        if (HasSummon && remainTime <= RuinPvE.Info.CastTime + 0.8f && remainTime > RuinPvE.Info.CastTime && !InCombat
            && RuinPvE.CanUse(out act))
        {
            return act;
        }
        if (BigSummonTime(out act))
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
        if (LuxSolarisPvE.CanUse(out act))
        {
            return true;
        }
        return base.HealAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.RekindlePvE)]
    protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (RekindlePvE.CanUse(out act))
        {
            return true;
        }
        return base.HealSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.LuxSolarisPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (!IsLastAction(false, RadiantAegisPvE) && RadiantAegisPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }
        return base.DefenseAreaAbility(nextGCD, out act);
    }
    #endregion

    #region oGCD Logic
    [RotationDesc(ActionID.LuxSolarisPvE)]
    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        if (Player.WillStatusEndGCD(3, 0, true, StatusID.RefulgentLux))
        {
            if (LuxSolarisPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (Player.WillStatusEndGCD(2, 0, true, StatusID.FirebirdTrance))
        {
            if (RekindlePvE.CanUse(out act))
            {
                return true;
            }
        }

        if (Player.WillStatusEndGCD(3, 0, true, StatusID.FirebirdTrance))
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

        if (TryUseSearingLight(out act))
        {
            return true;
        }
        if (TryUseEnergyDrain(out act))
        {
            return true;
        }
        if (TryUseEnkindle(out act))
        {
            return true;
        }

        if (TryUseAstralFlow(out act))
        {
            return true;
        }

        if (TryUseSearingFlash(out act))
        {
            return true;
        }

        if (TryUseAetherflow(out act))
        {
            return true;
        }
        if (MountainBusterPvE.CanUse(out act))
        {
            return true;
        }
        return base.AttackAbility(nextGCD, out act);
    }

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (_churinPotions.ShouldUsePotion(this, out act))
        {
            return true;
        }

        if (SwiftcastPvE.CanUse(out act))
        {
            if (AddSwiftcastOnRaise && nextGCD.IsTheSameTo(false, ResurrectionPvE))
            {
                return true;
            }
            if (AddSwiftcastOnLowST && !RubyRitePvE.EnoughLevel && nextGCD.IsTheSameTo(false, RubyRuinPvE, RubyRuinIiPvE, RubyRuinIiiPvE))
            {
                return true;
            }
            if (AddSwiftcastOnLowAOE && !RubyRitePvE.EnoughLevel && nextGCD.IsTheSameTo(false, RubyOutburstPvE))
            {
                return true;
            }
            if (AddSwiftcastOnRuby && nextGCD.IsTheSameTo(false, RubyRitePvE) && !ElementalMasteryTrait.EnoughLevel)
            {
                return true;
            }
            if (AddSwiftcastOnGaruda && nextGCD.IsTheSameTo(false, SlipstreamPvE) && ElementalMasteryTrait.EnoughLevel && !InBahamut && !InPhoenix && !InSolarBahamut)
            {
                return true;
            }
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    #endregion

    #region GCD Logic
    [RotationDesc(ActionID.CrimsonCyclonePvE)]
    protected override bool MoveForwardGCD(out IAction? act)
    {
        if (CrimsonCyclonePvE.CanUse(out act))
        {
            return true;
        }
        return base.MoveForwardGCD(out act);
    }

    [RotationDesc(ActionID.PhysickPvE)]
    protected override bool HealSingleGCD(out IAction? act)
    {
        if ((Healbot || Player.Level <= 30) && PhysickPvE.CanUse(out act))
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

        if (BigSummonTime(out act))
        {
            return true;
        }

        if (SlipstreamPvE.CanUse(out act, skipCastingCheck: AddSwiftcastOnGaruda && ((!SwiftcastPvE.Cooldown.IsCoolingDown && IsMoving) || HasSwift)))
        {
            return true;
        }

        if ((!IsMoving || AddCrimsonCycloneMoving) && CrimsonCyclonePvE.CanUse(out act) && (AddCrimsonCyclone || CrimsonCyclonePvE.Target.Target.DistanceToPlayer() <= CrimsonCycloneDistance))
        {
            return true;
        }

        if (CrimsonStrikePvE.CanUse(out act))
        {
            return true;
        }

        if (PreciousBrillianceTime(out act))
        {
            return true;
        }

        if (GemshineTime(out act))
        {
            return true;
        }

        if (!InBahamut && !InPhoenix && !InSolarBahamut)
        {
            switch (SummonOrder)
            {
                case SummonOrderType.TopazEmeraldRuby:
                default:
                    if (TitanTime(out act))
                    {
                        return true;
                    }

                    if (GarudaTime(out act))
                    {
                        return true;
                    }

                    if (IfritTime(out act))
                    {
                        return true;
                    }

                    break;

                case SummonOrderType.TopazRubyEmerald:
                    if (TitanTime(out act))
                    {
                        return true;
                    }

                    if (IfritTime(out act))
                    {
                        return true;
                    }

                    if (GarudaTime(out act))
                    {
                        return true;
                    }

                    break;

                case SummonOrderType.EmeraldTopazRuby:
                    if (GarudaTime(out act))
                    {
                        return true;
                    }

                    if (TitanTime(out act))
                    {
                        return true;
                    }

                    if (IfritTime(out act))
                    {
                        return true;
                    }

                    break;

                case SummonOrderType.RubyEmeraldTopaz:
                    if (IfritTime(out act))
                    {
                        return true;
                    }

                    if (GarudaTime(out act))
                    {
                        return true;
                    }

                    if (TitanTime(out act))
                    {
                        return true;
                    }

                    break;
            }
        }

        if (SummonTimeEndAfterGCD() && AttunmentTimeEndAfterGCD() && !InBahamut && !InPhoenix && !InSolarBahamut &&
            RuinIvPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        if (InIfrit && (!CrimsonCyclonePvE.CanUse(out _) || !CrimsonStrikePvE.CanUse(out _)) && IsMoving)
        {
            return RuinIvPvE.CanUse(out act);
        }

        if (BrandOfPurgatoryPvE.CanUse(out act))
        {
            return true;
        }
        if (UmbralFlarePvE.CanUse(out act))
        {
            return true;
        }
        if (AstralFlarePvE.CanUse(out act))
        {
            return true;
        }
        if (OutburstPvE.CanUse(out act))
        {
            return true;
        }

        if (FountainOfFirePvE.CanUse(out act))
        {
            return true;
        }
        if (UmbralImpulsePvE.CanUse(out act))
        {
            return true;
        }
        if (AstralImpulsePvE.CanUse(out act))
        {
            return true;
        }
        if (RuinIiiPvE.CanUse(out act))
        {
            return true;
        }
        if (RuinIiPvE.CanUse(out act))
        {
            return true;
        }
        if (RuinPvE.CanUse(out act))
        {
            return true;
        }
        return base.GeneralGCD(out act);
    }
    #endregion

    #region Extra Methods

    #region Summons
    private bool TitanTime(out IAction? act)
    {
        if (SummonTitanIiPvE.CanUse(out act))
        {
            return true;
        }
        if (SummonTitanPvE.CanUse(out act))
        {
            return true;
        }
        if (SummonTopazPvE.CanUse(out act))
        {
            return true;
        }
        return false;
    }

    private bool GarudaTime(out IAction? act)
    {
        if (SummonGarudaIiPvE.CanUse(out act))
        {
            return true;
        }
        if (SummonGarudaPvE.CanUse(out act))
        {
            return true;
        }
        if (SummonEmeraldPvE.CanUse(out act))
        {
            return true;
        }
        return false;
    }

    private bool IfritTime(out IAction? act)
    {
        if (SummonIfritIiPvE.CanUse(out act))
        {
            return true;
        }
        if (SummonIfritPvE.CanUse(out act))
        {
            return true;
        }
        if (SummonRubyPvE.CanUse(out act))
        {
            return true;
        }
        return false;
    }

    private bool GemshineTime(out IAction? act)
    {
        if (RubyRitePvE.CanUse(out act))
        {
            return true;
        }
        if (EmeraldRitePvE.CanUse(out act))
        {
            return true;
        }
        if (TopazRitePvE.CanUse(out act))
        {
            return true;
        }

        if (RubyRuinIiiPvE.CanUse(out act))
        {
            return true;
        }
        if (EmeraldRuinIiiPvE.CanUse(out act))
        {
            return true;
        }
        if (TopazRuinIiiPvE.CanUse(out act))
        {
            return true;
        }

        if (RubyRuinIiPvE.CanUse(out act))
        {
            return true;
        }
        if (EmeraldRuinIiPvE.CanUse(out act))
        {
            return true;
        }
        if (TopazRuinIiPvE.CanUse(out act))
        {
            return true;
        }

        if (RubyRuinPvE.CanUse(out act))
        {
            return true;
        }
        if (EmeraldRuinPvE.CanUse(out act))
        {
            return true;
        }
        if (TopazRuinPvE.CanUse(out act))
        {
            return true;
        }
        return false;
    }

    private bool PreciousBrillianceTime(out IAction? act)
    {
        if (RubyCatastrophePvE.CanUse(out act))
        {
            return true;
        }
        if (EmeraldCatastrophePvE.CanUse(out act))
        {
            return true;
        }
        if (TopazCatastrophePvE.CanUse(out act))
        {
            return true;
        }

        if (RubyDisasterPvE.CanUse(out act))
        {
            return true;
        }
        if (EmeraldDisasterPvE.CanUse(out act))
        {
            return true;
        }
        if (TopazDisasterPvE.CanUse(out act))
        {
            return true;
        }

        if (RubyOutburstPvE.CanUse(out act))
        {
            return true;
        }
        if (EmeraldOutburstPvE.CanUse(out act))
        {
            return true;
        }
        if (TopazOutburstPvE.CanUse(out act))
        {
            return true;
        }
        return false;
    }

    private bool BigSummonTime(out IAction? act)
    {
        if (SummonSolarBahamutPvE.CanUse(out act))
        {
            return true;
        }
        if (SummonBahamutPvE.EnoughLevel && SummonBahamutPvE.CanUse(out act)
        || !SummonBahamutPvE.EnoughLevel && DreadwyrmTrancePvE.CanUse(out act)
        || !DreadwyrmTrancePvE.EnoughLevel && HasHostilesInRange && AetherchargePvE.CanUse(out act))
        {
            return true;
        }
        if (SummonPhoenixPvE.CanUse(out act))
        {
            return true;
        }
        return false;
    }

    private int BigSummonGCDLeft
    {
        get
        {
            if (InBigSummon)
            {
                var MaxImpulse = Math.Abs((double)SummonTimer / (double)RuinPvE.Cooldown.RecastTime);
                {
                    if (MaxImpulse > 0)
                    {
                        return (int)(MaxImpulse + 1);
                    }
                }
            }
            return 0;
        }
    }
    #endregion

    #region oGCDs
    private bool TryUseEnergyDrain(out IAction? act)
    {
        act = null;
        if (HasAetherflowStacks || !InBigSummon)
        {
            return false;
        }
        if (BigSummonGCDLeft == 3 && (EnergySiphonPvE.CanUse(out act) || EnergyDrainPvE.CanUse(out act)))
        {
            return true;
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
        if (BigSummonGCDLeft == 5 && CanLateWeave && SearingLightPvE.CanUse(out act))
        {
            return true;
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
        if (BigSummonGCDLeft == 2 && (EnkindleSolarBahamutPvE.CanUse(out act) || EnkindleBahamutPvE.CanUse(out act) || EnkindlePhoenixPvE.CanUse(out act)))
        {
            return true;
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
        if (BigSummonGCDLeft == 1 && (SunflarePvE.CanUse(out act) || DeathflarePvE.CanUse(out act)))
        {
            return true;
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
        if (BigSummonGCDLeft < 1 && SearingFlashPvE.CanUse(out act))
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

        if ((SummonSolarBahamutPvE.EnoughLevel && InSolar || !SummonSolarBahamutPvE.EnoughLevel && InBahamut) && HasSearingLight || !SearingLightPvE.EnoughLevel)
        {
            if (PainflarePvE.CanUse(out act) || NecrotizePvE.CanUse(out act) || FesterPvE.CanUse(out act))
            {
                return true;
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
            int aliveHealerCount = 0;
            IEnumerable<IBattleChara> healers = PartyMembers.GetJobCategory(JobRole.Healer);
            foreach (IBattleChara h in healers)
            {
                if (!h.IsDead)
                    aliveHealerCount++;
            }

            return base.CanHealSingleSpell && (GCDHeal || aliveHealerCount == 0);
        }
    }
    #endregion
#endregion

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

}
