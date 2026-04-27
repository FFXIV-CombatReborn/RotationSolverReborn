using CombatRole = RotationSolver.Basic.Data.CombatRole;
namespace RotationSolver.Basic.Rotations.Basic;

public partial class BlueMageRotation
{
	#region Status Tracking
	/// <summary>
	/// 
	/// </summary>
	public static bool WaxingNocturneWillEnd => HasWaxingNocturne && StatusHelper.PlayerWillStatusEndGCD(2, 0, false, StatusID.WaxingNocturne);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasWaxingNocturne => StatusHelper.PlayerHasStatus(true, StatusID.WaxingNocturne);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasWaningNocturne => StatusHelper.PlayerHasStatus(true, StatusID.WaningNocturne);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasBasicInstinct => StatusHelper.PlayerHasStatus(true, StatusID.BasicInstinct);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasSurpanakhasFury => StatusHelper.PlayerHasStatus(true, StatusID.SurpanakhasFury);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasHarmonizedBoost => StatusHelper.PlayerHasStatus(true, StatusID.Boost_1716) || StatusHelper.PlayerHasStatus(true, StatusID.Harmonized);

	/// <summary>
	/// 
	/// </summary>
	public static bool IsTank => StatusHelper.PlayerHasStatus(true, StatusID.AethericMimicryTank);

	/// <summary>
	/// 
	/// </summary>
	public static bool IsDPS => StatusHelper.PlayerHasStatus(true, StatusID.AethericMimicryDps);

	/// <summary>
	/// 
	/// </summary>
	public static bool IsHealer => StatusHelper.PlayerHasStatus(true, StatusID.AethericMimicryHealer);

	/// <summary>
	/// 
	/// </summary>
	public static bool NoMimicry => !IsTank && !IsDPS && !IsHealer;
	#endregion


	/// <summary>
	/// 
	/// </summary>
	public override MedicineType MedicineType => MedicineType.Intelligence;

	private protected sealed override IBaseAction Raise => AngelWhisperPvE;
	private protected sealed override IBaseAction TankStance => MightyGuardPvE;

	/// <summary>
	/// 
	/// </summary>
	public static CombatRole BlueId => IsTank ? CombatRole.Tank : IsHealer ? CombatRole.Healer : CombatRole.DPS;

	static partial void ModifyWaterCannonPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Water;
	}

	static partial void ModifyFlameThrowerPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Fire;
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 3,
		};
	}

	static partial void ModifyAquaBreathPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Water;
		setting.TargetStatusProvide = [StatusID.Dropsy_1736];
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 3,
		};
	}

	static partial void ModifyFlyingFrenzyPvE(ref ActionSetting setting)
	{
		//setting.SpecialType = SpecialActionType.MovingForward;
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Blunt;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 3,
		};
	}

	static partial void ModifyDrillCannonsPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Piercing;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 3,
		};
		setting.IsFriendly = false;
		setting.TargetStatusNeed = [StatusID.Petrification, StatusID.Petrification_1511, StatusID.Petrification_3007, StatusID.Petrification_4445, StatusID.Petrification];
	}

	static partial void ModifyHighVoltagePvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Lightning;
		setting.IsParalysisSpell = true;
		setting.TargetStatusProvide = [StatusID.Paralysis];
		setting.TargetStatusNeed = [StatusID.Dropsy_1736];
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 3,
		};
	}

	static partial void ModifyLoomPvE(ref ActionSetting setting)
	{
		//setting.SpecialType = SpecialActionType.MovingForward;
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Lightning;
		setting.IsFriendly = true;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyFinalStingPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Piercing;
		setting.ActionCheck = () => !StatusHelper.PlayerHasStatus(true, StatusID.BrushWithDeath);
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifySongOfTormentPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.TargetStatusProvide = [StatusID.Bleeding_1714];
	}

	static partial void ModifyGlowerPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Lightning;
		setting.IsParalysisSpell = true;
		setting.TargetStatusProvide = [StatusID.Paralysis];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyPlaincrackerPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Earth;
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyBristlePvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.StatusProvide = [StatusID.Boost_1716, StatusID.Harmonized];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyWhiteWindPvE(ref ActionSetting setting)
	{
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyLevel5PetrifyPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.IsPetrificationSpell = true;
		setting.TargetStatusProvide = [StatusID.Petrification];
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifySharpenedKnifePvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Slashing;
		setting.TargetStatusNeed = [StatusID.Stun, StatusID.Stun_1343, StatusID.Stun_142, StatusID.Stun_149, StatusID.Stun_1513, StatusID.Stun_1521, StatusID.Stun_1522, StatusID.Stun_201, StatusID.Stun_2656, StatusID.Stun_2953, StatusID.Stun_3408, StatusID.Stun_4163, StatusID.Stun_4374, StatusID.Stun_4378, StatusID.Stun_4433, StatusID.Stun_5043];
	}

	static partial void ModifyIceSpikesPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Ice;
		setting.StatusProvide = [StatusID.IceSpikes_1720, StatusID.VeilOfTheWhorl_1724, StatusID.Schiltron];
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
	}

	static partial void ModifyBloodDrainPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
	}

	static partial void ModifyAcornBombPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.IsSleepSpell = true;
		setting.TargetStatusProvide = [StatusID.Sleep];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyBombTossPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Fire;
		setting.IsStunSpell = true;
		setting.TargetStatusProvide = [StatusID.Stun];
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyOffguardPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.TargetStatusProvide = [StatusID.Offguard];
	}

	static partial void ModifySelfdestructPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Fire;
		setting.ActionCheck = () => !StatusHelper.PlayerHasStatus(true, StatusID.BrushWithDeath);
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyTransfusionPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.ActionCheck = () => !StatusHelper.PlayerHasStatus(true, StatusID.BrushWithDeath);
		setting.IsFriendly = true;
	}

	static partial void ModifyFazePvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.IsStunSpell = true;
		setting.TargetStatusProvide = [StatusID.Stun];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyFlyingSardinePvE(ref ActionSetting setting)
	{
		//setting.TargetType = TargetType.Interrupt;
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Piercing;
		setting.IsInterruptSpell = true;
	}

	static partial void ModifySnortPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Wind;
		setting.IsFriendly = false;
		setting.IsInterruptSpell = true;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
		setting.ActionCheck = () =>
		{
			if (Player == null) return false;
			var playerFace = Player.GetFaceVector();
			var playerPos = Player.Position;
			const float snortRange = 6f;
			const double coneHalfAngle = Math.PI / 4; // 45 degrees each side = 90 degree cone
			foreach (var target in DataCenter.AllHostileTargets)
			{
				if (!target.CanInterrupt()) continue;
				if (target.DistanceToPlayer() > snortRange) continue;
				var dir = Vector3.Normalize(target.Position - playerPos);
				var angle = playerFace.AngleTo(dir);
				if (angle <= coneHalfAngle) return true;
			}
			return false;
		};
	}

	static partial void Modify_4TonzeWeightPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Blunt;
		setting.IsHeavySpell = true;
		setting.TargetStatusProvide = [StatusID.Heavy];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyTheLookPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.IsFriendly = false;
		//setting.TargetType = TargetType.Provoke;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyBadBreathPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.IsFriendly = false;
		setting.TargetStatusProvide = [StatusID.Slow, StatusID.Heavy, StatusID.Blind, StatusID.Paralysis, StatusID.Poison, StatusID.Malodorous];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyDiamondbackPvE(ref ActionSetting setting)
	{
		setting.StatusProvide = [StatusID.Diamondback];
		setting.IsFriendly = true;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyMightyGuardPvE(ref ActionSetting setting)
	{
		setting.StatusProvide = [StatusID.MightyGuard];
	}

	static partial void ModifyStickyTonguePvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Blunt;
		setting.IsStunSpell = true;
		setting.TargetStatusProvide = [StatusID.Stun];
	}

	static partial void ModifyToadOilPvE(ref ActionSetting setting)
	{
		setting.StatusProvide = [StatusID.ToadOil];
		setting.IsFriendly = true;
	}

	static partial void ModifyTheRamsVoicePvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Ice;
		setting.TargetStatusProvide = [StatusID.DeepFreeze_1731];
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyTheDragonsVoicePvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Lightning;
		setting.IsParalysisSpell = true;
		setting.TargetStatusProvide = [StatusID.Paralysis];
		setting.TargetStatusNeed = [StatusID.DeepFreeze_1731];
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyMissilePvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.TargetType = TargetType.HighHPPercent;
		setting.IsFlatDamageDeath = true;
	}

	static partial void Modify_1000NeedlesPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Piercing;
		setting.TargetType = TargetType.LowHP;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyInkJetPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.IsBlindSpell = true;
		setting.TargetStatusProvide = [StatusID.Blind];
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyFireAngonPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Piercing;
		setting.AdditionalAspects = [Aspect.Fire];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyMoonFlutePvE(ref ActionSetting setting)
	{
		setting.StatusProvide = [StatusID.WaxingNocturne];
		setting.IsFriendly = true;
	}

	static partial void ModifyTailScrewPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.IsFlatDamageDeath = true;
	}

	static partial void ModifyMindBlastPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.IsParalysisSpell = true;
		setting.TargetStatusProvide = [StatusID.Paralysis];
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 2,
		};
	}

	static partial void ModifyDoomPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.TargetStatusProvide = [StatusID.Doom_1738];
	}

	static partial void ModifyPeculiarLightPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.TargetStatusProvide = [StatusID.PeculiarLight];
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 3,
		};
	}

	static partial void ModifyFeatherRainPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Wind;
		setting.TargetStatusProvide = [StatusID.Windburn_1723];
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyEruptionPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Fire;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyMountainBusterPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Blunt;
		setting.AdditionalAspects = [Aspect.Earth];
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyShockStrikePvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Lightning;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyGlassDancePvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Ice;
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyVeilOfTheWhorlPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Water;
		setting.StatusProvide = [StatusID.IceSpikes_1720, StatusID.VeilOfTheWhorl_1724, StatusID.Schiltron];
		setting.TargetType = TargetType.Self;
		setting.IsFriendly = true;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyAlpineDraftPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Wind;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyProteanWavePvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Water;
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyNortherliesPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Ice;
		setting.TargetStatusNeed = [StatusID.Dropsy_1736];
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyElectrogenesisPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Lightning;
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyKaltstrahlPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Slashing;
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyAbyssalTransfixionPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Piercing;
		setting.IsParalysisSpell = true;
		setting.TargetStatusProvide = [StatusID.Paralysis];
	}

	static partial void ModifyChirpPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.IsSleepSpell = true;
		setting.TargetStatusProvide = [StatusID.Sleep];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyEerieSoundwavePvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.TargetStatusNeed = [StatusID.VulnerabilityDown, StatusID.CriticalStrikes_1797];
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyPomCurePvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
	}

	static partial void ModifyGobskinPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.StatusProvide = [StatusID.Gobskin];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyMagicHammerPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.StatusProvide = [StatusID.Conked];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyAvailPvE(ref ActionSetting setting)
	{
		//setting.StatusProvide = [StatusID.Avail];
	}

	static partial void ModifyFrogLegsPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.TargetType = TargetType.Provoke;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifySonicBoomPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Wind;
	}

	static partial void ModifyWhistlePvE(ref ActionSetting setting)
	{
		setting.StatusProvide = [StatusID.Boost_1716, StatusID.Harmonized];
		setting.IsFriendly = true;
	}

	static partial void ModifyWhiteKnightsTourPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.IsSlowSpell = true;
		setting.TargetStatusProvide = [StatusID.Slow];
		setting.TargetStatusNeed = [StatusID.Blind];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyBlackKnightsTourPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.IsBlindSpell = true;
		setting.TargetStatusProvide = [StatusID.Blind];
		setting.TargetStatusNeed = [StatusID.Slow];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyLevel5DeathPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.IsFriendly = false;
		setting.IsFlatDamageDeath = true;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyLauncherPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.IsFriendly = false;
		setting.IsFlatDamageDeath = true;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyPerpetualRayPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.IsStunSpell = true;
		setting.TargetStatusProvide = [StatusID.Stun];
	}

	static partial void ModifyCactguardPvE(ref ActionSetting setting)
	{
		setting.TargetStatusProvide = [StatusID.Cactguard];
		setting.TargetType = TargetType.BeAttacked;
	}

	static partial void ModifyRevengeBlastPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Blunt;
		setting.ActionCheck = () => Player?.GetHealthRatio() > 0.2f;
	}

	static partial void ModifyAngelWhisperPvE(ref ActionSetting setting)
	{
		setting.TargetType = TargetType.Death;
	}

	static partial void ModifyExuviationPvE(ref ActionSetting setting)
	{
		setting.TargetType = TargetType.Dispel;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyRefluxPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Lightning;
		setting.IsHeavySpell = true;
		setting.TargetStatusProvide = [StatusID.Heavy];
	}

	static partial void ModifyDevourPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.StatusProvide = [StatusID.HpBoost_2120];
	}

	static partial void ModifyCondensedLibraPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.TargetStatusProvide = [StatusID.PhysicalAttenuation, StatusID.AstralAttenuation, StatusID.UmbralAttenuation];
	}

	static partial void ModifyAethericMimicryPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.TargetType = TargetType.MimicryTarget;
		setting.StatusProvide =
			[StatusID.AethericMimicryDps, StatusID.AethericMimicryHealer, StatusID.AethericMimicryTank];
	}

	static partial void ModifySurpanakhaPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Earth;
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyQuasarPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 3,
		};
	}

	static partial void ModifyJKickPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Blunt;
		setting.ActionCheck = () => !StatusHelper.PlayerHasStatus(false, StatusID.Bind);
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyTripleTridentPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Piercing;
	}

	static partial void ModifyTinglePvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Lightning;
		setting.StatusProvide = [StatusID.Tingling];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyTatamigaeshiPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.TargetStatusProvide = [StatusID.Tingling];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyColdFogPvE(ref ActionSetting setting)
	{
		setting.StatusProvide = [StatusID.ColdFog];
		setting.TargetType = TargetType.Self;
		setting.IsFriendly = true;
	}

	static partial void ModifyWhiteDeathPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Ice;
		setting.StatusNeed = [StatusID.TouchOfFrost];
		setting.RequiredBluSlotActionId = 23267; // ColdFogPvE
		setting.TargetStatusProvide = [StatusID.DeepFreeze_1731];
	}

	static partial void ModifyStotramPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.IsFriendly = false;
		setting.ActionCheck = () => !IsHealer;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 3,
		};
	}

	static partial void ModifyStotramPvE_23416(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.ActionCheck = () => IsHealer;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifySaintlyBeamPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyFeculentFloodPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Earth;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyAngelsSnackPvE(ref ActionSetting setting)
	{
		setting.StatusProvide = [StatusID.AngelsSnack];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyChelonianGatePvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
		setting.StatusProvide = [StatusID.ChelonianGate];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyDivineCataractPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Water;
		setting.StatusNeed = [StatusID.AuspiciousTrance];
		setting.RequiredBluSlotActionId = 23273; // ChelonianGatePvE
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyTheRoseOfDestructionPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyBasicInstinctPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.StatusProvide = [StatusID.BasicInstinct];
		setting.ActionCheck = () => IsInDuty && AliveOtherPartyMemberCount == 0 && DataCenter.Territory?.ContentType != TerritoryContentType.TheMaskedCarnivale;
	}

	static partial void ModifyUltravibrationPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.TargetStatusNeed = [StatusID.DeepFreeze_1731, StatusID.Petrification];
		setting.IsFriendly = false;
		setting.IsFlatDamageDeath = true;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyBlazePvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Ice;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyMustardBombPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Fire;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyDragonForcePvE(ref ActionSetting setting)
	{
		setting.StatusProvide = [StatusID.DragonForce];
		setting.TargetType = TargetType.Self;
		setting.IsFriendly = true;
	}

	static partial void ModifyAetherialSparkPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.StatusProvide = [StatusID.Bleeding_1714];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyHydroPullPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Water;
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyMaledictionOfWaterPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Water;
		setting.IsFriendly = false;
		setting.ActionCheck	= () => InCombat;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyChocoMeteorPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyMatraMagicPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
	}

	static partial void ModifyPeripheralSynthesisPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Blunt;
		setting.StatusProvide = [StatusID.Lightheaded_2501];
		setting.StatusNeed = [StatusID.Lightheaded_2501];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyBothEndsPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Blunt;
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyPhantomFlurryPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Blunt;
		setting.IsFriendly = false;
		setting.ActionCheck = () => !IsMoving;
		setting.StatusProvide = [StatusID.PhantomFlurry];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyPhantomFlurryPvE_23289(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Blunt;
		setting.IsFriendly = false;
		setting.ActionCheck = () => !IsMoving;
		setting.StatusNeed = [StatusID.PhantomFlurry];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyNightbloomPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.IsFriendly = false;
		setting.TargetStatusProvide = [StatusID.Bleeding_1714];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyGoblinPunchPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Blunt;
		setting.StatusNeed = [StatusID.MightyGuard];
	}

	static partial void ModifyRightRoundPvE(ref ActionSetting setting)
	{
		//Need data
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Blunt;
		setting.ActionCheck = () => InCombat;
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifySchiltronPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.StatusProvide = [StatusID.IceSpikes_1720, StatusID.VeilOfTheWhorl_1724, StatusID.Schiltron];
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
	}

	static partial void ModifyRehydrationPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
	}

	static partial void ModifyBreathOfMagicPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.TargetStatusProvide = [StatusID.BreathOfMagic];
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyWildRagePvE(ref ActionSetting setting)
	{
		//Need data
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Blunt;
		setting.ActionCheck = () => Player?.GetEffectiveHpPercent() > 50f;
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyPeatPeltPvE(ref ActionSetting setting)
	{
		//Need data
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Earth;
		setting.TargetStatusProvide = [StatusID.Begrimed];
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyDeepCleanPvE(ref ActionSetting setting)
	{
		//Need data
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Blunt;
		setting.TargetStatusNeed = [StatusID.Begrimed];
		setting.ActionCheck = () => StatusHelper.PlayerStatusStack(true, StatusID.Spickandspan) < 6;
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyRubyDynamicsPvE(ref ActionSetting setting)
	{
		//Need data
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Slashing;
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyDivinationRunePvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 3,
		};
		setting.IsFriendly = false;
	}

	static partial void ModifyDimensionalShiftPvE(ref ActionSetting setting)
	{
		//Need data
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyConvictionMarcatoPvE(ref ActionSetting setting)
	{
		//Need data
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyForceFieldPvE(ref ActionSetting setting)
	{
		//Need data
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
	}

	static partial void ModifyWingedReprobationPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Physical;
		setting.AspectOverride = Aspect.Piercing;
		setting.StatusProvide = [StatusID.WingedReprobation, StatusID.WingedRedemption];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyLaserEyePvE(ref ActionSetting setting)
	{
		//Need data
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyCandyCanePvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.TargetStatusProvide = [StatusID.CandyCane];
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyMortalFlamePvE(ref ActionSetting setting)
	{
		//Need data
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Fire;
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifySeaShantyPvE(ref ActionSetting setting)
	{
		//Need data
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Water;
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyApokalypsisPvE(ref ActionSetting setting)
	{
		//Need data
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyBeingMortalPvE(ref ActionSetting setting)
	{
		setting.AttackTypeOverride = AttackType.Magic;
		setting.AspectOverride = Aspect.Unaspected;
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	/// <summary>
	///
	/// </summary>
	public override void DisplayBaseStatus()
	{
		ImGui.TextWrapped($"Aetheric Mimicry Role: {BlueId}");
	}
}
