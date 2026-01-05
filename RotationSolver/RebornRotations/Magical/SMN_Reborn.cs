using System.ComponentModel;

namespace RotationSolver.RebornRotations.Magical;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.4")]
[SourceCode(Path = "main/RebornRotations/Magical/SMN_Reborn.cs")]

public sealed class SMN_Reborn : SummonerRotation
{
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

    [RotationConfig(CombatType.PvE, Name = "Use second charge of Radiant Aegis on cooldown")]
    public bool RadiantOnCooldown { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use Radiant Aegis on cooldown")]
	public bool RadiantOnCooldownSpam { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Use this if there's no other raid buff in your party")]
    public bool SecondTypeOpenerLogic { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Physick above level 30")]
    public bool Healbot { get; set; } = false;

    #endregion

    #region Tracking Properties
    public override void DisplayRotationStatus()
    {
        ImGui.Text($"EnergyDrainPvE: Is Cooling Down: {EnergyDrainPvE.Cooldown.IsCoolingDown}");
    }
    #endregion

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (SummonCarbunclePvE.CanUse(out IAction? act))
        {
            return act;
        }
        if (HasSummon && remainTime <= RuinPvE.Info.CastTime + CountDownAhead
            && RuinPvE.CanUse(out act))
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

        if (HasSearingLight && InCombat && UseBurstMedicine(out act))
        {
            return true;
        }

		if (RadiantAegisPvE.Cooldown.CurrentCharges > 0 && !IsLastAction(false, RadiantAegisPvE))
		{
			if (RadiantOnCooldown && (RadiantAegisPvE.Cooldown.CurrentCharges == 2 || (RadiantAegisPvE.Cooldown.CurrentCharges == 1 && RadiantAegisPvE.Cooldown.WillHaveXChargesGCD(2, 1, 0))))
			{
				if (RadiantAegisPvE.CanUse(out act))
				{
					return true;
				}
			}

			if (RadiantOnCooldownSpam)
			{
				if (RadiantAegisPvE.CanUse(out act))
				{
					return true;
				}
			}
		}

		return base.GeneralAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        bool inBigInvocation = !SummonBahamutPvE.EnoughLevel || InBahamut || InPhoenix || InSolarBahamut;
        bool inSolarUnique = DataCenter.PlayerSyncedLevel() == 100 ? !InBahamut && !InPhoenix && InSolarBahamut : InBahamut && !InPhoenix;
        bool burstInSolar = (SummonSolarBahamutPvE.EnoughLevel && InSolarBahamut) || (!SummonSolarBahamutPvE.EnoughLevel && InBahamut) || !SummonBahamutPvE.EnoughLevel;

        if (burstInSolar)
        {
            if (SearingLightPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (inBigInvocation)
        {
            if (EnergySiphonPvE.CanUse(out act))
            {
                if ((EnergySiphonPvE.Target.Target.IsBossFromTTK() || EnergySiphonPvE.Target.Target.IsBossFromIcon()) && EnergySiphonPvE.Target.Target.IsDying())
                {
                    return true;
                }
                if (SummonTime > 0f || !SummonBahamutPvE.EnoughLevel)
                {
                    return true;
                }
            }

            if (EnergyDrainPvE.CanUse(out act))
            {
                if ((EnergyDrainPvE.Target.Target.IsBossFromTTK() || EnergyDrainPvE.Target.Target.IsBossFromIcon()) && EnergyDrainPvE.Target.Target.IsDying())
                {
                    return true;
                }
                if (SummonTime > 0f || !SummonBahamutPvE.EnoughLevel)
                {
                    return true;
                }
            }

            if (EnkindleBahamutPvE.CanUse(out act))
            {
                if ((EnkindleBahamutPvE.Target.Target.IsBossFromTTK() || EnkindleBahamutPvE.Target.Target.IsBossFromIcon()) && EnkindleBahamutPvE.Target.Target.IsDying())
                {
                    return true;
                }
                if (SummonTime > 0f || !SummonBahamutPvE.EnoughLevel)
                {
                    return true;
                }
            }

            if (EnkindleSolarBahamutPvE.CanUse(out act))
            {
                if ((EnkindleSolarBahamutPvE.Target.Target.IsBossFromTTK() || EnkindleSolarBahamutPvE.Target.Target.IsBossFromIcon()) && EnkindleSolarBahamutPvE.Target.Target.IsDying())
                {
                    return true;
                }
                if (SummonTime > 0f || !SummonBahamutPvE.EnoughLevel)
                {
                    return true;
                }
            }

            if (EnkindlePhoenixPvE.CanUse(out act))
            {
                if ((EnkindlePhoenixPvE.Target.Target.IsBossFromTTK() || EnkindlePhoenixPvE.Target.Target.IsBossFromIcon()) && EnkindlePhoenixPvE.Target.Target.IsDying())
                {
                    return true;
                }
                if (SummonTime > 0f || !SummonBahamutPvE.EnoughLevel)
                {
                    return true;
                }
            }

            if (DeathflarePvE.CanUse(out act))
            {
                if ((DeathflarePvE.Target.Target.IsBossFromTTK() || DeathflarePvE.Target.Target.IsBossFromIcon()) && DeathflarePvE.Target.Target.IsDying())
                {
                    return true;
                }
                if (SummonTime > 0f || !SummonBahamutPvE.EnoughLevel)
                {
                    return true;
                }
            }

            if (SunflarePvE.CanUse(out act))
            {
                if ((SunflarePvE.Target.Target.IsBossFromTTK() || SunflarePvE.Target.Target.IsBossFromIcon()) && SunflarePvE.Target.Target.IsDying())
                {
                    return true;
                }
                if (SummonTime > 0f || !SummonBahamutPvE.EnoughLevel)
                {
                    return true;
                }
            }

            if (SearingFlashPvE.CanUse(out act))
            {
                if ((SearingFlashPvE.Target.Target.IsBossFromTTK() || SearingFlashPvE.Target.Target.IsBossFromIcon()) && SearingFlashPvE.Target.Target.IsDying())
                {
                    return true;
                }

                if (SummonTime > 0f || !SummonBahamutPvE.EnoughLevel)
                {
                    return true;
                }
            }
        }

        if (MountainBusterPvE.CanUse(out act))
        {
            return true;
        }

        if (PainflarePvE.CanUse(out act))
        {
            if ((inSolarUnique && HasSearingLight) || !SearingLightPvE.EnoughLevel)
            {
                return true;
            }
            if ((PainflarePvE.Target.Target.IsBossFromTTK() || PainflarePvE.Target.Target.IsBossFromIcon()) && PainflarePvE.Target.Target.IsDying())
            {
                return true;
            }
        }

        if (NecrotizePvE.CanUse(out act))
        {
            if ((inSolarUnique && HasSearingLight) || !SearingLightPvE.EnoughLevel)
            {
                return true;
            }
            if ((NecrotizePvE.Target.Target.IsBossFromTTK() || NecrotizePvE.Target.Target.IsBossFromIcon()) && NecrotizePvE.Target.Target.IsDying())
            {
                return true;
            }
            if (EnergyDrainPvE.Cooldown.WillHaveOneChargeGCD(2))
            {
                return true;
            }
        }

        if (FesterPvE.CanUse(out act))
        {
            if ((inSolarUnique && HasSearingLight) || !SearingLightPvE.EnoughLevel)
            {
                return true;
            }
            if ((FesterPvE.Target.Target.IsBossFromTTK() || FesterPvE.Target.Target.IsBossFromIcon()) && FesterPvE.Target.Target.IsDying())
            {
                return true;
            }
            if (EnergyDrainPvE.Cooldown.WillHaveOneChargeGCD(2))
            {
                return true;
            }
        }

        if (SearingFlashPvE.CanUse(out act))
        {
            if ((SearingFlashPvE.Target.Target.IsBossFromTTK() || SearingFlashPvE.Target.Target.IsBossFromIcon()) && SearingFlashPvE.Target.Target.IsDying())
            {
                return true;
            }
        }
        return base.AttackAbility(nextGCD, out act);
    }

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
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

        if (SummonBahamutPvE.CanUse(out act))
        {
            return true;
        }
        if (!SummonBahamutPvE.Info.EnoughLevelAndQuest() && DreadwyrmTrancePvE.CanUse(out act))
        {
            return true;
        }

        if ((HasSearingLight || SearingLightPvE.Cooldown.IsCoolingDown) && SummonBahamutPvE.CanUse(out act))
        {
            return true;
        }

        if (IsBurst && !SearingLightPvE.Cooldown.IsCoolingDown && SummonSolarBahamutPvE.CanUse(out act))
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

        if (!DreadwyrmTrancePvE.Info.EnoughLevelAndQuest() && HasHostilesInRange && AetherchargePvE.CanUse(out act))
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
        if (RuinIiiPvE.EnoughLevel && RuinIiiPvE.CanUse(out act))
        {
            return true;
        }
        if (!RuinIiiPvE.Info.EnoughLevelAndQuest() && RuinIiPvE.EnoughLevel && RuinIiPvE.CanUse(out act))
        {
            return true;
        }
        if (!RuinIiPvE.Info.EnoughLevelAndQuest() && RuinPvE.CanUse(out act))
        {
            return true;
        }
        return base.GeneralGCD(out act);
    }
    #endregion

    #region Extra Methods
    private bool TitanTime(out IAction? act)
    {
        if (SummonTitanIiPvE.CanUse(out act))
        {
            return true;
        }
        if (!SummonTitanIiPvE.EnoughLevel && SummonTitanPvE.CanUse(out act))
        {
            return true;
        }
        if (!SummonTitanPvE.Info.EnoughLevelAndQuest() && SummonTopazPvE.CanUse(out act))
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
        if (!SummonGarudaIiPvE.EnoughLevel && SummonGarudaPvE.CanUse(out act))
        {
            return true;
        }
        if (!SummonGarudaPvE.Info.EnoughLevelAndQuest() && SummonEmeraldPvE.CanUse(out act))
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
        if (!SummonIfritIiPvE.EnoughLevel && SummonIfritPvE.CanUse(out act))
        {
            return true;
        }
        if (!SummonIfritPvE.Info.EnoughLevelAndQuest() && SummonRubyPvE.CanUse(out act))
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

        if (!SummonIfritPvE.Info.EnoughLevelAndQuest() && RubyRuinPvE.CanUse(out act))
        {
            return true;
        }
        if (!SummonGarudaPvE.Info.EnoughLevelAndQuest() && EmeraldRuinPvE.CanUse(out act))
        {
            return true;
        }
        if (!SummonTitanPvE.Info.EnoughLevelAndQuest() && TopazRuinPvE.CanUse(out act))
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
}
