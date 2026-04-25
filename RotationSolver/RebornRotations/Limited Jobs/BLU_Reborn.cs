namespace RotationSolver.RebornRotations.Magical;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.45")]
[SourceCode(Path = "main/BasicRotations/Limited Jobs/BLU_Reborn.cs")]

public sealed class BLU_Reborn : BlueMageRotation
{
	[RotationConfig(CombatType.PvE, Name = "Use Basic Instinct")]
	public bool UseBasicInstinct { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use Mighty Guard")]
	public bool UseMightyGuard { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use Transfusion to heal")]
	public bool UseTransfusion { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Use Snort to interrupt")]
	public bool UseSnort { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Use Bad Breath for AOE mitigation")]
	public bool UseBadBreath { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Use Low Chance abilities")]
	public bool LowChance { get; set; } = false;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "Minimum HP percent allowed for the use of Devour as a heal")]
	public float DevourHealThreshold { get; set; } = 0.6f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "Minimum HP percent allowed for the use of Missile")]
	public float TheMissileKnowsWhereItIs { get; set; } = 0.6f;

	[Range(0, 10000, ConfigUnitType.None, 100)]
	[RotationConfig(CombatType.PvE, Name = "MP needed to use Blood Drain/Divination Rune for MP gain")]

	public float MPGainNeed { get; set; } = 5000;

	#region Countdown logic

	protected override IAction? CountDownAction(float remainTime)
	{
		return base.CountDownAction(remainTime);
	}

	#endregion

	#region Emergency Logic

	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		if (IsLastAbility(false, SurpanakhaPvE) && SurpanakhaPvE.Cooldown.HasOneCharge && SurpanakhaPvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		return base.EmergencyAbility(nextGCD, out act);
	}

	#endregion

	#region Move oGCD Logic

	protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
	{
		return base.MoveForwardAbility(nextGCD, out act);
	}

	protected override bool MoveBackAbility(IAction nextGCD, out IAction? act)
	{
		return base.MoveBackAbility(nextGCD, out act);
	}

	protected override bool SpeedAbility(IAction nextGCD, out IAction? act)
	{
		return base.SpeedAbility(nextGCD, out act);
	}

	#endregion

	#region Heal/Defense oGCD Logic

	protected override bool HealAreaAbility(IAction nextGCD, out IAction? act)
	{
		return base.HealAreaAbility(nextGCD, out act);
	}

	protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
	{
		return base.HealSingleAbility(nextGCD, out act);
	}

	protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
	{
		return base.DefenseAreaAbility(nextGCD, out act);
	}

	protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
	{
		return base.DefenseSingleAbility(nextGCD, out act);
	}

	#endregion

	#region oGCD Logic

	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		if (SurpanakhaPvE.Cooldown.CurrentCharges == 4 || IsLastAbility(true, SurpanakhaPvE))
		{
			if (SurpanakhaPvE.CanUse(out act, usedUp: true))
			{
				return true;
			}
		}

		if (FeatherRainPvE.CanUse(out act))
		{
			return true;
		}

		if (EruptionPvE.CanUse(out act))
		{
			return true;
		}

		if (MountainBusterPvE.CanUse(out act))
		{
			return true;
		}

		if (ShockStrikePvE.CanUse(out act))
		{
			return true;
		}

		if (GlassDancePvE.CanUse(out act))
		{
			return true;
		}

		if (VeilOfTheWhorlPvE.CanUse(out act))
		{
			return true;
		}

		if (QuasarPvE.CanUse(out act))
		{
			return true;
		}

		if (JKickPvE.CanUse(out act))
		{
			return true;
		}

		if (BothEndsPvE.CanUse(out act))
		{
			return true;
		}

		if (NightbloomPvE.CanUse(out act))
		{
			return true;
		}

		return base.AttackAbility(nextGCD, out act);
	}

	protected override bool InterruptAbility(IAction nextGCD, out IAction? act)
	{
		return base.InterruptAbility(nextGCD, out act);
	}

	protected override bool DispelAbility(IAction nextGCD, out IAction? act)
	{
		return base.DispelAbility(nextGCD, out act);
	}

	protected override bool AntiKnockbackAbility(IAction nextGCD, out IAction? act)
	{
		return base.AntiKnockbackAbility(nextGCD, out act);
	}

	protected override bool ProvokeAbility(IAction nextGCD, out IAction? act)
	{
		return base.ProvokeAbility(nextGCD, out act);
	}

	protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
	{

		return base.GeneralAbility(nextGCD, out act);
	}

	#endregion

	#region GCD Logic

	protected override bool EmergencyGCD(out IAction? act)
	{
		if (SurpanakhaPvE.Cooldown.CurrentCharges == 4 || IsLastAbility(true, SurpanakhaPvE))
		{
			if (SurpanakhaPvE.CanUse(out act, usedUp: true))
			{
				return true;
			}
		}

		if (WaxingNocturneWillEnd)
		{
			if (DiamondbackPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (IsTank || HasBasicInstinct)
		{
			if (MightyGuardPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (BasicInstinctPvE.CanUse(out act))
		{
			return true;
		}

		if (EerieSoundwavePvE.CanUse(out act))
		{
			return true;
		}

		return base.EmergencyGCD(out act);
	}

	protected override bool ProvokeGCD(out IAction? act)
	{
		if (TheLookPvE.CanUse(out act, targetOverride: TargetType.Provoke, skipAoeCheck: true))
		{
			return true;
		}

		if (BadBreathPvE.CanUse(out act, targetOverride: TargetType.Provoke, skipAoeCheck: true))
		{
			return true;
		}

		if (StickyTonguePvE.CanUse(out act, targetOverride: TargetType.Provoke))
		{
			return true;
		}

		if (FrogLegsPvE.CanUse(out act))
		{
			return true;
		}

		return base.ProvokeGCD(out act);
	}

	protected override bool MyInterruptGCD(out IAction? act)
	{
		if (BombTossPvE.CanUse(out act, skipAoeCheck: true, targetOverride: TargetType.Interrupt))
		{
			return true;
		}

		if (FazePvE.CanUse(out act, skipAoeCheck: true, targetOverride: TargetType.Interrupt))
		{
			return true;
		}

		if (FlyingSardinePvE.CanUse(out act, targetOverride: TargetType.Interrupt))
		{
			return true;
		}

		if (UseSnort)
		{
			if (SnortPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (StickyTonguePvE.CanUse(out act, targetOverride: TargetType.Interrupt))
		{
			return true;
		}

		return base.MyInterruptGCD(out act);
	}

	protected override bool GeneralGCD(out IAction? act)
	{
		if (IsTank && DivineCataractPvE.CanUse(out act))
		{
			return true;
		}

		if (ChirpPvE.CanUse(out act))
		{
			return true;
		}

		if (LowChance)
		{
			if (DoomPvE.CanUse(out act))
			{
				return true;
			}

			if (Level5DeathPvE.CanUse(out act))
			{
				return true;
			}

			if (TailScrewPvE.CanUse(out act))
			{
				return true;
			}

			if (LauncherPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (CondensedLibraPvE.CanUse(out act))
		{
			return true;
		}

		if (OffguardPvE.CanUse(out act))
		{
			return true;
		}

		if (PeculiarLightPvE.CanUse(out act))
		{
			return true;
		}

		if (InCombat && MoonFlutePvE.CanUse(out act))
		{
			return true;
		}

		if (UltravibrationPvE.CanUse(out act))
		{
			return true;
		}

		if (SongOfTormentPvE.Info.IsOnSlot && SongOfTormentPvE.CanUse(out _, skipStatusProvideCheck: true) && SongOfTormentPvE.Target.Target != null && (SongOfTormentPvE.Target.Target?.WillStatusEndGCD(3, 0, true, SongOfTormentPvE.Setting.TargetStatusProvide ?? []) ?? false))
		{
			if (BristlePvE.CanUse(out act))
			{
				return true;
			}

			if (WhistlePvE.CanUse(out act))
			{
				return true;
			}
		}

		if (SongOfTormentPvE.CanUse(out act))
		{
			return true;
		}

		if (AetherialSparkPvE.CanUse(out act))
		{
			return true;
		}

		if (BreathOfMagicPvE.CanUse(out act))
		{
			return true;
		}

		if (SongOfTormentPvE.CanUse(out act, skipStatusProvideCheck: true) && SongOfTormentPvE.Target.Target != null && (SongOfTormentPvE.Target.Target?.WillStatusEndGCD(3, 0, true, SongOfTormentPvE.Setting.TargetStatusProvide ?? []) ?? false))
		{
			if ((BristlePvE.Info.IsOnSlot || WhistlePvE.Info.IsOnSlot) && HasHarmonizedBoost)
			{
				return true;
			}
		}

		if (TripleTridentPvE.Info.IsOnSlot && (TripleTridentPvE.Cooldown.HasOneCharge || TripleTridentPvE.Cooldown.WillHaveOneChargeGCD(1)) && IsLastGCD(false, TinglePvE))
		{
			if (TinglePvE.CanUse(out act))
			{
				return true;
			}
		}

		if (TripleTridentPvE.CanUse(out act))
		{
			return true;
		}

		if (DivineCataractPvE.CanUse(out act))
		{
			return true;
		}

		if (MatraMagicPvE.CanUse(out act))
		{
			return true;
		}

		if (TheRoseOfDestructionPvE.CanUse(out act))
		{
			return true;
		}

		if (WingedReprobationPvE.CanUse(out act))
		{
			return true;
		}

		if (WingedReprobationPvE.CanUse(out act))
		{
			return true;
		}

		if (BeingMortalPvE.CanUse(out act))
		{
			return true;
		}

		if (PeripheralSynthesisPvE.CanUse(out act, skipStatusProvideCheck: true))
		{
			return true;
		}

		if (DevourPvE.CanUse(out act, skipStatusProvideCheck: Player?.GetEffectiveHpPercent() <= DevourHealThreshold || IsTank))
		{
			return true;
		}

		if (NumberOfHostilesInMaxRange > 0 && IceSpikesPvE.CanUse(out act))
		{
			return true;
		}

		if (NumberOfHostilesInMaxRange > 0 && SchiltronPvE.CanUse(out act))
		{
			return true;
		}

		if (MissilePvE.CanUse(out act))
		{
			if (MissilePvE.Target.Target != null && MissilePvE.Target.Target.GetEffectiveHpPercent() >= TheMissileKnowsWhereItIs)
			{
				return true;
			}
		}

		if (_1000NeedlesPvE.CanUse(out act))
		{
			if (_1000NeedlesPvE.Target.Target != null && _1000NeedlesPvE.Target.Target.CurrentHp >= 1000)
			{
				return true;
			}
		}

		if (DrillCannonsPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (Player?.GetEffectiveHpPercent() <= 20f)
		{
			if (RevengeBlastPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (WhiteDeathPvE.CanUse(out act))
		{
			return true;
		}

		if (SaintlyBeamPvE.CanUse(out act))
		{
			return true;
		}

		if (BlackKnightsTourPvE.CanUse(out act))
		{
			return true;
		}

		if (WhiteKnightsTourPvE.CanUse(out act))
		{
			return true;
		}

		if (StotramPvE.CanUse(out act))
		{
			return true;
		}

		if (SharpenedKnifePvE.CanUse(out act))
		{
			return true;
		}
		
		if (GoblinPunchPvE.CanUse(out act))
		{
			if (GoblinPunchPvE.Target.Target != null && CanHitPositional(EnemyPositional.Front, GoblinPunchPvE.Target.Target))
			{
				return true;
			}
		}

		if (Player?.CurrentMp <= MPGainNeed)
		{
			if (DivinationRunePvE.CanUse(out act))
			{
				return true;
			}

			if (BloodDrainPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (DevourPvE.CanUse(out act))
		{
			return true;
		}

		if (MagicHammerPvE.CanUse(out act))
		{
			return true;
		}

		if (CandyCanePvE.CanUse(out act))
		{
			return true;
		}

		if (HasChocobo && ChocoMeteorPvE.CanUse(out act))
		{
			return true;
		}

		if (PeripheralSynthesisPvE.CanUse(out act, skipStatusNeed: true))
		{
			return true;
		}

		if (HydroPullPvE.CanUse(out act))
		{
			return true;
		}

		if (BlazePvE.CanUse(out act))
		{
			return true;
		}

		if (MustardBombPvE.CanUse(out act))
		{
			return true;
		}

		if (FlameThrowerPvE.CanUse(out act))
		{
			return true;
		}

		if (GlowerPvE.CanUse(out act))
		{
			return true;
		}

		if (TheLookPvE.CanUse(out act))
		{
			return true;
		}

		if (PlaincrackerPvE.CanUse(out act))
		{
			return true;
		}

		if (HighVoltagePvE.CanUse(out act))
		{
			return true;
		}

		if (FeculentFloodPvE.CanUse(out act))
		{
			return true;
		}

		if (KaltstrahlPvE.CanUse(out act))
		{
			return true;
		}

		if (ElectrogenesisPvE.CanUse(out act))
		{
			return true;
		}

		if (NortherliesPvE.CanUse(out act))
		{
			return true;
		}

		if (ProteanWavePvE.CanUse(out act))
		{
			return true;
		}

		if (AlpineDraftPvE.CanUse(out act))
		{
			return true;
		}

		if (TatamigaeshiPvE.CanUse(out act))
		{
			return true;
		}

		if (PerpetualRayPvE.CanUse(out act))
		{
			return true;
		}

		if (RefluxPvE.CanUse(out act))
		{
			return true;
		}

		if (GoblinPunchPvE.CanUse(out act))
		{
			return true;
		}

		if (TheRamsVoicePvE.CanUse(out act, skipAoeCheck: UltravibrationPvE.Info.IsOnSlot && UltravibrationPvE.Cooldown.HasOneCharge))
		{
			if (UltravibrationPvE.Info.IsOnSlot && UltravibrationPvE.Cooldown.HasOneCharge)
			{
				return true;
			}

			if (!UltravibrationPvE.Info.IsOnSlot)
			{
				return true;
			}
		}

		if (TheDragonsVoicePvE.CanUse(out act, skipAoeCheck: TheRamsVoicePvE.Info.IsOnSlot, skipStatusNeed: !TheRamsVoicePvE.Info.IsOnSlot))
		{
			return true;
		}

		if (BlackKnightsTourPvE.CanUse(out act, skipStatusNeed: WhiteKnightsTourPvE.Info.IsOnSlot))
		{
			return true;
		}

		if (WhiteKnightsTourPvE.CanUse(out act, skipStatusNeed: BlackKnightsTourPvE.Info.IsOnSlot))
		{
			return true;
		}

		if (ChocoMeteorPvE.CanUse(out act))
		{
			return true;
		}

		if (FireAngonPvE.CanUse(out act))
		{
			return true;
		}

		if (_4TonzeWeightPvE.CanUse(out act))
		{
			return true;
		}

		if (InkJetPvE.CanUse(out act))
		{
			return true;
		}

		if (BombTossPvE.CanUse(out act))
		{
			return true;
		}

		if (DrillCannonsPvE.CanUse(out act, skipTargetStatusNeedCheck: true))
		{
			return true;
		}

		if (HighVoltagePvE.CanUse(out act, skipTargetStatusNeedCheck: true))
		{
			return true;
		}

		if (FlyingFrenzyPvE.CanUse(out act))
		{
			return true;
		}

		if (AquaBreathPvE.CanUse(out act))
		{
			return true;
		}

		if (AbyssalTransfixionPvE.CanUse(out act))
		{
			return true;
		}

		if (SharpenedKnifePvE.CanUse(out act, skipTargetStatusNeedCheck: true))
		{
			return true;
		}

		if (SonicBoomPvE.CanUse(out act))
		{
			return true;
		}

		if (WaterCannonPvE.CanUse(out act))
		{
			return true;
		}

		if (GoblinPunchPvE.CanUse(out act, skipStatusNeed: true))
		{
			return true;
		}

		if (FlyingSardinePvE.CanUse(out act))
		{
			return true;
		}

		return base.GeneralGCD(out act);
	}

	protected override bool RaiseGCD(out IAction? act)
	{
		if (AngelWhisperPvE.CanUse(out act))
		{
			return true;
		}

		return base.RaiseGCD(out act);
	}

	protected override bool DispelGCD(out IAction? act)
	{
		if (ExuviationPvE.CanUse(out act))
		{
			return true;
		}

		return base.DispelGCD(out act);
	}

	protected override bool MoveForwardGCD(out IAction? act)
	{
		if (LoomPvE.CanUse(out act))
		{
			return true;
		}

		return base.MoveForwardGCD(out act);
	}

	protected override bool HealSingleGCD(out IAction? act)
	{
		if (UseTransfusion)
		{
			if (TransfusionPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (RehydrationPvE.CanUse(out act))
		{
			return true;
		}

		if (PomCurePvE.CanUse(out act))
		{
			return true;
		}

		return base.HealSingleGCD(out act);
	}

	protected override bool HealAreaGCD(out IAction? act)
	{
		if (Player?.GetEffectiveHpPercent() > 50 && WhiteWindPvE.CanUse(out act))
		{
			return true;
		}

		if (StotramPvE_23416.CanUse(out act))
		{
			return true;
		}

		if (ExuviationPvE.CanUse(out act))
		{
			return true;
		}

		if (AngelsSnackPvE.CanUse(out act, skipStatusProvideCheck: !IsHealer))
		{
			return true;
		}

		return base.HealAreaGCD(out act);
	}

	protected override bool DefenseSingleGCD(out IAction? act)
	{
		if (DiamondbackPvE.CanUse(out act))
		{
			return true;
		}

		if (ToadOilPvE.CanUse(out act))
		{
			return true;
		}

		if (DragonForcePvE.CanUse(out act))
		{
			return true;
		}

		if (IsTank)
		{
			if (CactguardPvE.CanUse(out act, targetOverride: TargetType.Self))
			{
				return true;
			}
		}

		if (CactguardPvE.CanUse(out act))
		{
			return true;
		}

		if (ChelonianGatePvE.CanUse(out act, targetOverride: TargetType.Self))
		{
			return true;
		}

		return base.DefenseSingleGCD(out act);
	}

	protected override bool DefenseAreaGCD(out IAction? act)
	{
		if (UseBadBreath)
		{
			if (BadBreathPvE.CanUse(out act, targetOverride: TargetType.HighHP, skipAoeCheck: true))
			{
				return true;
			}
		}

		if (IsHealer)
		{
			if (AngelsSnackPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (GobskinPvE.CanUse(out act))
		{
			return true;
		}

		if (ColdFogPvE.CanUse(out act))
		{
			return true;
		}

		return base.DefenseAreaGCD(out act);
	}

	#endregion

	#region Extra Methods
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

			return base.CanHealSingleSpell && (IsHealer || aliveHealerCount == 0);
		}
	}
	public override bool CanHealAreaSpell
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

			return base.CanHealAreaSpell && (IsHealer || aliveHealerCount == 0);
		}
	}
	#endregion
}