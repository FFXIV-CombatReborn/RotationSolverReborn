using System.ComponentModel;

namespace RebornRotations.Magical;

[Rotation("Default", CombatType.PvE, GameVersion = "7.2")]
[SourceCode(Path = "main/BasicRotations/Magical/SMN_Default.cs")]
[Api(4)]
public sealed class SMN_Default : SummonerRotation
{

    #region Config Options

    public enum SummonOrderType : byte
    {
        [Description("Topaz-Emerald-Ruby")] TopazEmeraldRuby,

        [Description("Topaz-Ruby-Emerald")] TopazRubyEmerald,

        [Description("Emerald-Topaz-Ruby")] EmeraldTopazRuby,

        [Description("Ruby-Emerald-Topaz")] RubyEmeraldTopaz,
    }

    [RotationConfig(CombatType.PvE, Name = "Use Crimson Cyclone at any range, regardless of saftey use with caution (Enabling this ignores the below distance setting).")]
    public bool AddCrimsonCyclone { get; set; } = true;

    [Range(1, 20, ConfigUnitType.Yalms)]
    [RotationConfig(CombatType.PvE, Name = "Max distance you can be from the target for Crimson Cyclone use")]
    public float CrimsonCycloneDistance { get; set; } = 3.0f;

    [RotationConfig(CombatType.PvE, Name = "Use Crimson Cyclone when moving")]
    public bool AddCrimsonCycloneMoving { get; set; } = false;

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

    #endregion

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (SummonCarbunclePvE.CanUse(out var act)) return act;

        if (remainTime <= RuinPvE.Info.CastTime + CountDownAhead
            && RuinPvE.CanUse(out act)) return act;
        return base.CountDownAction(remainTime);
    }
    #endregion

    #region Move Logic
    [RotationDesc(ActionID.CrimsonCyclonePvE)]
    protected override bool MoveForwardGCD(out IAction? act)
    {
        if (CrimsonCyclonePvE.CanUse(out act, skipAoeCheck: true)) return true;
        return base.MoveForwardGCD(out act);
    }
    #endregion

    #region oGCD Logic
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        bool isTargetBoss = CurrentTarget?.IsBossFromTTK() ?? false;
        bool isTargetDying = CurrentTarget?.IsDying() ?? false;
        bool targetIsBossAndDying = isTargetBoss && isTargetDying;
        bool inBigInvocation = !SummonBahamutPvE.EnoughLevel || (InBahamut || InPhoenix || InSolarBahamut);
        bool inSolarUnique = Player.Level == 100 ? !InBahamut && !InPhoenix && InSolarBahamut : InBahamut && !InPhoenix;
        if (SecondTypeOpenerLogic)
        {
            bool elapsed0ChargeAfterInvocation = SummonSolarBahamutPvE.Cooldown.ElapsedOneChargeAfterGCD() || SummonBahamutPvE.Cooldown.ElapsedOneChargeAfterGCD() || SummonPhoenixPvE.Cooldown.ElapsedOneChargeAfterGCD();
            bool elapsed1ChargeAfterInvocation = SummonSolarBahamutPvE.Cooldown.ElapsedOneChargeAfterGCD(1) || SummonBahamutPvE.Cooldown.ElapsedOneChargeAfterGCD(1) || SummonPhoenixPvE.Cooldown.ElapsedOneChargeAfterGCD(1);
            bool elapsed2ChargeAfterInvocation = SummonSolarBahamutPvE.Cooldown.ElapsedOneChargeAfterGCD(2) || SummonBahamutPvE.Cooldown.ElapsedOneChargeAfterGCD(2) || SummonPhoenixPvE.Cooldown.ElapsedOneChargeAfterGCD(2);
            bool burstInSolar = Player.Level == 100 ? InSolarBahamut : InBahamut;

            if (!Player.HasStatus(false, StatusID.SearingLight) && burstInSolar && elapsed0ChargeAfterInvocation)
            {
                if (SearingLightPvE.CanUse(out act, skipAoeCheck: true)) return true;
            }

            if (inBigInvocation && (elapsed0ChargeAfterInvocation || targetIsBossAndDying) && EnergySiphonPvE.CanUse(out act)) return true;
            if (inBigInvocation && (elapsed0ChargeAfterInvocation || targetIsBossAndDying) && EnergyDrainPvE.CanUse(out act)) return true;
            if (inBigInvocation && (elapsed1ChargeAfterInvocation || targetIsBossAndDying) && EnkindleBahamutPvE.CanUse(out act)) return true;
            if (inBigInvocation && (elapsed1ChargeAfterInvocation || targetIsBossAndDying) && EnkindleSolarBahamutPvE.CanUse(out act)) return true;
            if (inBigInvocation && (elapsed1ChargeAfterInvocation || targetIsBossAndDying) && EnkindlePhoenixPvE.CanUse(out act)) return true;
            if (inBigInvocation && (elapsed1ChargeAfterInvocation || targetIsBossAndDying) && DeathflarePvE.CanUse(out act, skipAoeCheck: true)) return true;
            if (inBigInvocation && (elapsed1ChargeAfterInvocation || targetIsBossAndDying) && SunflarePvE.CanUse(out act, skipAoeCheck: true)) return true;

            if (RekindlePvE.CanUse(out act, skipAoeCheck: true)) return true;
            if (MountainBusterPvE.CanUse(out act)) return true;

            if ((inSolarUnique && Player.HasStatus(false, StatusID.SearingLight) || !SearingLightPvE.EnoughLevel || isTargetBoss && isTargetDying) && EnergyDrainPvE.IsInCooldown && PainflarePvE.CanUse(out act)) return true;
            if ((inSolarUnique && Player.HasStatus(false, StatusID.SearingLight) || !SearingLightPvE.EnoughLevel || isTargetBoss && isTargetDying) && EnergyDrainPvE.IsInCooldown && FesterPvE.CanUse(out act)) return true;

            if ((elapsed2ChargeAfterInvocation || targetIsBossAndDying) && SearingFlashPvE.CanUse(out act, skipAoeCheck: true)) return true;
            if (DoesAnyPlayerNeedHeal() && !inBigInvocation && LuxSolarisPvE.CanUse(out act)) return true;
        }
        else
        {
            bool elapsed1ChargeAfterInvocation = SummonSolarBahamutPvE.Cooldown.ElapsedOneChargeAfterGCD(1) || SummonBahamutPvE.Cooldown.ElapsedOneChargeAfterGCD(1) || SummonPhoenixPvE.Cooldown.ElapsedOneChargeAfterGCD(1);
            bool elapsed2ChargeAfterInvocation = SummonSolarBahamutPvE.Cooldown.ElapsedOneChargeAfterGCD(2) || SummonBahamutPvE.Cooldown.ElapsedOneChargeAfterGCD(2) || SummonPhoenixPvE.Cooldown.ElapsedOneChargeAfterGCD(2);
            bool elapsed3ChargeAfterInvocation = SummonSolarBahamutPvE.Cooldown.ElapsedOneChargeAfterGCD(3) || SummonBahamutPvE.Cooldown.ElapsedOneChargeAfterGCD(3) || SummonPhoenixPvE.Cooldown.ElapsedOneChargeAfterGCD(3);
            bool elapsed4ChargeAfterInvocation = SummonSolarBahamutPvE.Cooldown.ElapsedOneChargeAfterGCD(4) || SummonBahamutPvE.Cooldown.ElapsedOneChargeAfterGCD(4) || SummonPhoenixPvE.Cooldown.ElapsedOneChargeAfterGCD(4);
            bool burstInSolar = Player.Level == 100 ? InSolarBahamut : InBahamut;

            if (!Player.HasStatus(false, StatusID.SearingLight) && burstInSolar && elapsed1ChargeAfterInvocation)
            {
                if (SearingLightPvE.CanUse(out act, skipAoeCheck: true)) return true;
            }

            if (inBigInvocation && (elapsed2ChargeAfterInvocation || targetIsBossAndDying) && EnergySiphonPvE.CanUse(out act)) return true;
            if (inBigInvocation && (elapsed2ChargeAfterInvocation || targetIsBossAndDying) && EnergyDrainPvE.CanUse(out act)) return true;
            if (inBigInvocation && (elapsed3ChargeAfterInvocation || targetIsBossAndDying) && EnkindleBahamutPvE.CanUse(out act)) return true;
            if (inBigInvocation && (elapsed3ChargeAfterInvocation || targetIsBossAndDying) && EnkindleSolarBahamutPvE.CanUse(out act)) return true;
            if (inBigInvocation && (elapsed3ChargeAfterInvocation || targetIsBossAndDying) && EnkindlePhoenixPvE.CanUse(out act)) return true;
            if (inBigInvocation && (elapsed3ChargeAfterInvocation || targetIsBossAndDying) && DeathflarePvE.CanUse(out act, skipAoeCheck: true)) return true;
            if (inBigInvocation && (elapsed3ChargeAfterInvocation || targetIsBossAndDying) && SunflarePvE.CanUse(out act, skipAoeCheck: true)) return true;

            if (RekindlePvE.CanUse(out act, skipAoeCheck: true)) return true;
            if (MountainBusterPvE.CanUse(out act)) return true;

            if ((inSolarUnique && Player.HasStatus(false, StatusID.SearingLight) && elapsed2ChargeAfterInvocation && EnergyDrainPvE.Cooldown.WillHaveOneCharge(2) || !SearingLightPvE.EnoughLevel || isTargetBoss && isTargetDying) && EnergyDrainPvE.IsInCooldown && PainflarePvE.CanUse(out act)) return true;
            if ((inSolarUnique && Player.HasStatus(false, StatusID.SearingLight) && elapsed2ChargeAfterInvocation && EnergyDrainPvE.Cooldown.WillHaveOneCharge(2) || !SearingLightPvE.EnoughLevel || isTargetBoss && isTargetDying) && EnergyDrainPvE.IsInCooldown && FesterPvE.CanUse(out act)) return true;

            if ((inSolarUnique && Player.HasStatus(false, StatusID.SearingLight) && elapsed2ChargeAfterInvocation || !SearingLightPvE.EnoughLevel || isTargetBoss && isTargetDying) && EnergyDrainPvE.IsInCooldown && PainflarePvE.CanUse(out act)) return true;
            if ((inSolarUnique && Player.HasStatus(false, StatusID.SearingLight) && elapsed2ChargeAfterInvocation || !SearingLightPvE.EnoughLevel || isTargetBoss && isTargetDying) && EnergyDrainPvE.IsInCooldown && FesterPvE.CanUse(out act)) return true;

            if ((elapsed4ChargeAfterInvocation || targetIsBossAndDying) && SearingFlashPvE.CanUse(out act, skipAoeCheck: true)) return true;
            if (DoesAnyPlayerNeedHeal() && !inBigInvocation && LuxSolarisPvE.CanUse(out act)) return true;
        }


        return base.AttackAbility(nextGCD, out act);
    }

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        bool anyBigInvocationIsCoolingDown = SummonBahamutPvE.Cooldown.IsCoolingDown || SummonSolarBahamutPvE.Cooldown.IsCoolingDown || SummonPhoenixPvE.Cooldown.IsCoolingDown;
        if (AddSwiftcastOnGaruda && nextGCD == SlipstreamPvE && Player.Level > 86 && !InBahamut && !InPhoenix && !InSolarBahamut)
        {
            if (SwiftcastPvE.CanUse(out act)) return true;
        }

        if (AddSwiftcastOnRuby && nextGCD == RubyRitePvE && Player.Level < 86)
        {
            if (SwiftcastPvE.CanUse(out act)) return true;
        }

        if ((RadiantOnCooldown && RadiantAegisPvE.Cooldown.CurrentCharges == 2 || RadiantAegisPvE.Cooldown.CurrentCharges == 1 && RadiantAegisPvE.Cooldown.WillHaveOneCharge(5)) && (anyBigInvocationIsCoolingDown && Player.Level <= 100) && RadiantAegisPvE.CanUse(out act)) return true;
        if (RadiantOnCooldown && Player.Level < 88 && anyBigInvocationIsCoolingDown && RadiantAegisPvE.CanUse(out act)) return true;

        return base.EmergencyAbility(nextGCD, out act);
    }

    #endregion

    #region GCD Logic
    [RotationDesc(ActionID.PhysickPvE)]
    protected override bool HealSingleGCD(out IAction? act)
    {
        if ((Healbot || Player.Level <= 30) && PhysickPvE.CanUse(out act)) return true;
        return base.HealSingleGCD(out act);
    }

    protected override bool GeneralGCD(out IAction? act)
    {
        if (SummonCarbunclePvE.CanUse(out act)) return true;

        if (SummonBahamutPvE.CanUse(out act)) return true;
        if ((Player.HasStatus(false, StatusID.SearingLight) || SearingLightPvE.Cooldown.IsCoolingDown) && SummonBahamutPvE.CanUse(out act)) return true;
        if (IsBurst && (!SearingLightPvE.Cooldown.IsCoolingDown && SummonSolarBahamutPvE.CanUse(out act))) return true;

        if (AddSwiftcastOnGaruda && SlipstreamPvE.CanUse(out act, skipAoeCheck: true, skipCastingCheck: !SwiftcastPvE.Cooldown.IsCoolingDown || HasSwift)) return true;
        if (SlipstreamPvE.CanUse(out act, skipAoeCheck: true)) return true;

        if (CrimsonStrikePvE.CanUse(out act, skipAoeCheck: true)) return true;

        if (PreciousBrilliancePvE.CanUse(out act)) return true;

        if (GemshinePvE.CanUse(out act)) return true;

        if ((!IsMoving || AddCrimsonCycloneMoving) && (AddCrimsonCyclone || CrimsonCyclonePvE.Target.Target?.DistanceToPlayer() <= CrimsonCycloneDistance) && CrimsonCyclonePvE.CanUse(out act, skipAoeCheck: true)) return true;

        if (!SummonBahamutPvE.EnoughLevel && HasHostilesInRange && AetherchargePvE.CanUse(out act)) return true;

        if (!InBahamut && !InPhoenix && !InSolarBahamut)
        {
            switch (SummonOrder)
            {
                case SummonOrderType.TopazEmeraldRuby:
                default:
                    if (SummonTopazPvE.CanUse(out act)) return true;
                    if (SummonEmeraldPvE.CanUse(out act)) return true;
                    if (SummonRubyPvE.CanUse(out act)) return true;
                    break;

                case SummonOrderType.TopazRubyEmerald:
                    if (SummonTopazPvE.CanUse(out act)) return true;
                    if (SummonRubyPvE.CanUse(out act)) return true;
                    if (SummonEmeraldPvE.CanUse(out act)) return true;
                    break;

                case SummonOrderType.EmeraldTopazRuby:
                    if (SummonEmeraldPvE.CanUse(out act)) return true;
                    if (SummonTopazPvE.CanUse(out act)) return true;
                    if (SummonRubyPvE.CanUse(out act)) return true;
                    break;

                case SummonOrderType.RubyEmeraldTopaz:
                    if (SummonRubyPvE.CanUse(out act)) return true;
                    if (SummonEmeraldPvE.CanUse(out act)) return true;
                    if (SummonTopazPvE.CanUse(out act)) return true;
                    break;
            }
        }

        if (SummonTimeEndAfterGCD() && AttunmentTimeEndAfterGCD() && !InBahamut && !InPhoenix && !InSolarBahamut && SummonEmeraldPvE.IsInCooldown && SummonTopazPvE.IsInCooldown && SummonRubyPvE.IsInCooldown &&
            RuinIvPvE.CanUse(out act, skipAoeCheck: true)) return true;

        if (OutburstPvE.CanUse(out act)) return true;
        if (RuinPvE.CanUse(out act)) return true;
        return base.GeneralGCD(out act);
    }
    #endregion

    #region Extra Methods
    public override bool CanHealSingleSpell => false;

    public bool DoesAnyPlayerNeedHeal()
    {
        return PartyMembersAverHP < 0.8f;
    }
    #endregion
}
