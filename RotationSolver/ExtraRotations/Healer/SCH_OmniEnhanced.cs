using System.ComponentModel;

namespace RotationSolver.ExtraRotations.Healer;

[Rotation("OmniEnhanced", CombatType.PvE, GameVersion = "7.5", Description = "OmniDuty inspired intelligent rotation")]
[SourceCode(Path = "main/ExtraRotations/Healer/SCH_OmniEnhanced.cs")]

public sealed class SCH_OmniEnhanced : ScholarRotation
{
	#region Config Options
	[RotationConfig(CombatType.PvE, Name = "Seraphism Emergency HP Threshold (0.0 to 1.0)")]
	public float SeraphismEmergencyHp { get; set; } = 0.35f;

	[RotationConfig(CombatType.PvE, Name = "Dungeon Pre-Mitigation (Auto W2W Mit)")]
	public bool DungeonPreMitigation { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Hold Chain Stratagem if 2+ DPS are dead/weak")]
	public bool SafeChainStratagem { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Lock healing actions while Macrocosmos is active (AST Synergy)")]
	public bool LockHealingDuringMacrocosmos { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use Swiftcast for movement (DPS Uptime)")]
	public bool UseSwiftcastForMovement { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Manual Protraction on self triggers Recitation+Adlo+Deployment")]
	public bool ProtractionDeploymentTrigger { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Reserve Aetherflow for Excogitation/Sacred Soil on bosses")]
	public bool ReserveAetherflowForExcog { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Limit Seraphism to multihit party stacks")]
	public bool MultiHitRestrict { get; set; } = false;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "Remove Aetherpact if the linked party member's HP is above this percentage")]
	public float AetherpactRemove { get; set; } = 0.9f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "Do not start Aetherpact if the target's HP is above this percentage (prevents toggling)")]
	public float AetherpactMinimum { get; set; } = 0.8f;

	[Range(0, 0.5f, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "Minimum HP percent to use Excogitation as a heal instead of a defensive buff")]
	public float ExcogHeal { get; set; } = 0.5f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "Party HP percent threshold to use Emergency Tactics with Succor")]
	public float EmergencyTacticsHeal { get; set; } = 0.4f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "Average party HP percent to use Recitation with Indomitability (must be below AoE heal threshold)")]
	public float ReciteIndomitability { get; set; } = 0.5f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "Average party HP percent to prioritize Indomitability and instant heals over heal-over-time effects")]
	public float EmergencyHealPercent { get; set; } = 0.1f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "Estimated percent of HP dealt as DPS for ballpark calculations")]
	public float BallparkPercent { get; set; } = 0.08f;

	[Range(0, 10, ConfigUnitType.Seconds)]
	[RotationConfig(CombatType.PvE, Name = "Seconds you must be stationary before Sacred Soil can be used")]
	public float SacredSoilTimeStill { get; set; } = 3f;

	[Range(0, 5, ConfigUnitType.Seconds)]
	[RotationConfig(CombatType.PvE, Name = "Seconds you must be moving before Ruin II will be used")]
	public float RuinTime { get; set; } = 0f;

	[Range(0, 10000, ConfigUnitType.None)]
	[RotationConfig(CombatType.PvE, Name = "Minimum MP before prioritizing emergency healing and rezzing (willing to use Seraphism sooner)")]
	public int EmergencyHealingMPThreshold { get; set; } = 2000;

	[Range(0, 2, ConfigUnitType.None)]
	[RotationConfig(CombatType.PvE, Name = "Number of fewer mobs required to favor AoW spam over Bio (0 = use Bio if break-even below 30s)")]
	public int DotOffsetMobs { get; set; } = 1;

	[Range(0, 100, ConfigUnitType.None)]
	[RotationConfig(CombatType.PvE, Name = "Minimum Fairy Gauge required before prioritizing Fey Union (link)")]
	public int LinkFairyGauge { get; set; } = 70;

	[RotationConfig(CombatType.PvE, Name = "Enable Swiftcast restriction: only allow Raise while Swiftcast is active")]
	public bool SwiftLogic { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use GCDs to heal. (Ignored if you are the only healer in party)")]
	public bool GCDHeal { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use Recitation during the countdown opener")]
	public bool UseRecitationInOpener { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use Adloquium during the countdown opener")]
	public bool AdloquiumDuringCountdown { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use Recitation with Succor, Concitation, or Accession")]
	public bool ReciteSuccor { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use Dissipation during burst phases")]
	public bool ShouldDissipate { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use Sacred Soil's regeneration as a healing effect")]
	public bool SacredSoilHeal { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Allow Sacred Soil while moving if fighting a boss")]
	public bool SacredSoilBossExemption { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Enable ballpark DoT time-to-kill estimator (in addition to normal TTK configs)")]
	public bool UseBallparkTTK { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "How to use Deployment Tactics")]
	public DeploymentTacticsUsageStrategy DeploymentTacticsUsage { get; set; } = DeploymentTacticsUsageStrategy.CatalyzeOnly;

	public enum DeploymentTacticsUsageStrategy : byte
	{
		[Description("Use when a party member has Catalyze status")]
		CatalyzeOnly,

		[Description("Use when a party member has Catalyze or Galvanize status")]
		CatalyzeOrGalvanize,
	}
	#endregion

	#region Tracking Properties
	public override void DisplayRotationStatus()
	{
		ImGui.Text($"Max Targets to apply Bio to rather than spamming AoW: {GetAoWBreakevenTargets() - 1}");
	}
	#endregion

	#region Countdown Logic
	protected override IAction? CountDownAction(float remainTime)
	{
		if (SummonEosPvE.CanUse(out IAction? act))
		{
			return act;
		}

		if (remainTime < RuinPvE.Info.CastTime + CountDownAhead && RuinPvE.CanUse(out act))
		{
			return act;
		}

		if (remainTime < 3f && UseBurstMedicine(out act))
		{
			return act;
		}

		if (remainTime is < 4f and > 3f && DeploymentTacticsPvE.CanUse(out act))
		{
			return act;
		}

		if (remainTime is < 7f and > 6f && AdloquiumDuringCountdown && AdloquiumPvE.CanUse(out act, targetOverride: TargetType.Tank))
		{
			return act;
		}

		if (remainTime <= 15f && UseRecitationInOpener && RecitationPvE.CanUse(out act))
		{
			return act;
		}

		return base.CountDownAction(remainTime);
	}
	#endregion

	private bool HasHealingLockout
	{
		get
		{
			if (!LockHealingDuringMacrocosmos) return false;
			foreach (var p in PartyMembers)
			{
				if (p.HasStatus(true, StatusID.Macrocosmos)) return true;
			}
			return false;
		}
	}

	#region oGCD Logic
	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		if (HasHealingLockout)
		{
			act = null;
			return false;
		}

		// 6.3 Protraction Trigger (Recitation)
		if (ProtractionDeploymentTrigger && Target != null && StatusHelper.PlayerHasStatus(true, StatusID.Protraction))
		{
			if (!StatusHelper.PlayerHasStatus(true, StatusID.Recitation) && RecitationPvE.CanUse(out act)) return true;
		}

		// 1.5 Living Dead Full Support
		foreach (IBattleChara member in PartyMembers)
		{
			if (member.HasStatus(true, StatusID.WalkingDead) && member.GetHealthRatio() < 1)
			{
				if (ExcogitationPvE.CanUse(out act)) return true;
				if (LustratePvE.CanUse(out act)) return true;
			}
		}

		// 5.2 Defensive Dissipation (Emergency Panic Button)
		// If the party is dying (<35% avg HP), we have 0 Aetherflow stacks, and we can't use Aetherflow, we sacrifice the fairy.
		if (PartyMembersAverHP < 0.35f && !HasAetherflow && AetherflowPvE.Cooldown.IsCoolingDown && SeraphTime <= 0)
		{
			if (DissipationPvE.CanUse(out act))
			{
				return true;
			}
		}

		// Verified Excog and Indomitability are NOT recognized by "next GCD"
		if (ShouldUseRecitation(nextGCD) && RecitationPvE.CanUse(out act))
		{
			return true;
		}

		// Only use emergency tactics if we're healing from a raid wide damage or multiple members need to be healed for doom
		if (nextGCD.IsTheSameTo(false, SuccorPvE, ConcitationPvE, AccessionPvE) && EmergencyTacticsPvE.CanUse(out act))
		{
			int count = 0;
			foreach (IBattleChara member in PartyMembers)
			{
				if (member.DistanceToPlayer() <= 15)
				{
					if (member.DoomNeedHealing() || member.GetHealthRatio() < EmergencyTacticsHeal)
					{
						count++;
						if (count > 1)
						{
							break;
						}
					}
				}
			}
			if (count > 1)
			{
				return true;
			}
		}

		// Remove Aetherpact
		foreach (IBattleChara item in PartyMembers)
		{
			if (!item.HasStatus(true, StatusID.FeyUnion_1223))
			{
				continue;
			}

			if (item.GetHealthRatio() >= AetherpactRemove)
			{
				act = AetherpactPvE;
				return true;
			}
		}

		// Burn Consolation if about to run out of time on Seraph and still have charges
		return SeraphTime < 3 && ConsolationPvE.CanUse(out act, usedUp: true) || base.EmergencyAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.SummonSeraphPvE, ActionID.ConsolationPvE, ActionID.SacredSoilPvE, ActionID.IndomitabilityPvE, ActionID.WhisperingDawnPvE, ActionID.FeyBlessingPvE, ActionID.SeraphismPvE)]
	protected override bool HealAreaAbility(IAction nextGCD, out IAction? act)
	{
		if (HasHealingLockout)
		{
			act = null;
			return false;
		}

		// Always try to use Indomitability if we've just used Recitation and are area healing
		if (HasRecitation && IndomitabilityPvE.CanUse(out act))
		{
			return true;
		}

		// Always use Consolation if we have a Seraph out and need area healing/shielding
		if (ConsolationPvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		// This is normally an inefficient choice, but there are *some* mechanics where it can be a good idea to burst heal at lower efficiency (likely triggering GCD Succors as well)
		if (PartyMembersAverHP < GetAdaptedThreshold(EmergencyHealPercent))
		{
			if (FeyBlessingPvE.CanUse(out act))
			{
				return true;
			}

			if (IndomitabilityPvE.CanUse(out act))
			{
				return true;
			}
		}

		// Otherwise we use fairy abilities as these are cheaper/better than our aether charges
		if (WhisperingDawnPvE.CanUse(out act))
		{
			return true;
		}

		if (FeyBlessingPvE.CanUse(out act))
		{
			return true;
		}

		if (WhisperingDawnPvE.Cooldown.IsCoolingDown && FeyBlessingPvE.Cooldown.IsCoolingDown)
		{
			if (SummonSeraphPvE.CanUse(out act))
			{
				return true;
			}
		}

		// Once we have enhanced regen on sacred soil this becomes incredibly efficient for healing (500 potency HoT) in addition to being a defensive ability
		if (EnhancedSacredSoilTrait.EnoughLevel && SacredSoilHeal && ShouldUseSacredSoil(out act))
		{
			return true;
		}

		// 2.1 Predictive Seraphism Timing (Emergency Mode)
		if (PartyMembersAverHP < SeraphismEmergencyHp && SeraphismPvE.CanUse(out act))
		{
			return true;
		}

		// Seraphism is really good but we want to save it if we can, and should alternate it with Summon Seraph defensively outside of the hardest content
		if ((SummonSeraphPvE.Cooldown.IsCoolingDown || CurrentMp <= EmergencyHealingMPThreshold) && SeraphismPvE.CanUse(out act))
		{
			return true;
		}

		// Lower Priority Indomitability without Recitation
		if (IndomitabilityPvE.CanUse(out act))
		{
			return true;
		}

		return base.HealAreaAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.AetherpactPvE, ActionID.ExcogitationPvE, ActionID.LustratePvE, ActionID.SacredSoilPvE, ActionID.WhisperingDawnPvE, ActionID.FeyBlessingPvE)]
	protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
	{
		if (HasHealingLockout)
		{
			act = null;
			return false;
		}

		bool haveLinkDRK = false;
		foreach (IBattleChara p in PartyMembers)
		{
			if (p.HasStatus(true, StatusID.FeyUnion_1223) && p.HasStatus(false, StatusID.LivingDead))
			{
				haveLinkDRK = true;
				break;
			}
		}
		// remove link if the party member has link and also has Living Dead status
		if (AetherpactPvE.CanUse(out act) && haveLinkDRK)
		{
			return true;
		}

		// 1.3 Guardian Angel Healing
		if (ExcogitationPvE.CanUse(out act) || LustratePvE.CanUse(out act))
		{
			var tar = act == ExcogitationPvE ? ExcogitationPvE.Target.Target : LustratePvE.Target.Target;
			if (tar != null)
			{
				bool hasWeakness = tar.HasStatus(false, StatusID.Weakness, StatusID.BrinkOfDeath);
				if (hasWeakness && tar.GetHealthRatio() < 0.8f)
				{
					return true;
				}
			}
		}

		if (ExcogitationPvE.CanUse(out act))
		{
			// Check if any tank matches Excogitation target
			bool tankHasExcogTarget = false;
			IEnumerable<IBattleChara> tanks = PartyMembers.GetJobCategory(JobRole.Tank);
			foreach (IBattleChara member in tanks)
			{
				if (member == ExcogitationPvE.Target.Target)
				{
					tankHasExcogTarget = true;
					break;
				}
			}
			if (HasRecitation && tankHasExcogTarget && ExcogitationPvE.Target.Target.GetHealthRatio() < ExcogHeal)
			{
				return true;
			}
		}

		// Check if any party member has Fey Union status
		bool haveLink = false;
		foreach (IBattleChara p in PartyMembers)
		{
			if (p.HasStatus(true, StatusID.FeyUnion_1223))
			{
				haveLink = true;
				break;
			}
		}
		// 5.3 Aggressive W2W Fey Union
		// If in a trash pull and a tank drops below 85%, aggressively tether them.
		if (InCombat && !Target.IsBossFromTTK() && AetherpactPvE.CanUse(out act) && !haveLink)
		{
			if (AetherpactPvE.Target.Target != null && AetherpactPvE.Target.Target.IsJobCategory(JobRole.Tank) && AetherpactPvE.Target.Target.GetHealthRatio() < 0.85f)
			{
				return true;
			}
		}

		// If we have gauge to spend and don't have a link, we should use this resource first
		if (AetherpactPvE.CanUse(out act) &&
			FairyGauge >= LinkFairyGauge &&
			!haveLink &&
			AetherpactPvE.Target.Target.GetHealthRatio() <= AetherpactMinimum)
		{
			return true;
		}

		// Otherwise we'll spend aether charges; we didn't burn it on the tank above so use excog based on oGCD heal toggle
		if (!HasRecitation && !IsLastAbility(false, RecitationPvE) && ExcogitationPvE.CanUse(out act) && ExcogitationPvE.Target.Target.GetHealthRatio() < ExcogHeal)
		{
			return true;
		}

		// Once we have enhanced regen on sacred soil we should be willing to use it even for single target [500 potency+DR aoe vs Lustrate's 600 potency single target]
		if (EnhancedSacredSoilTrait.EnoughLevel && SacredSoilHeal && ShouldUseSacredSoil(out act))
		{
			return true;
		}

		if (LustratePvE.CanUse(out act))
		{
			return true; // Technically Whispering Dawn is better to burn first, but the 15y range from the faerie makes this unreliable in dungeons
		}

		// Use aoe faerie abilities even for single target to avoid GCD heals
		if (WhisperingDawnPvE.CanUse(out act))
		{
			return true;
		}

		if (FeyBlessingPvE.CanUse(out act))
		{
			return true;
		}

		// We don't have any other resources to use, so we'll use link even if it only has a small amount of charges
		if (!haveLink && FairyGauge > 20 && AetherpactPvE.CanUse(out act))
		{
			return true;
		}

		return base.HealSingleAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.FeyIlluminationPvE, ActionID.ExpedientPvE, ActionID.SummonSeraphPvE, ActionID.ConsolationPvE, ActionID.SacredSoilPvE, ActionID.SeraphismPvE)]
	protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
	{
		// 6.3 Protraction Deployment Tactics Trigger (Beiruta)
		if (ProtractionDeploymentTrigger && Target != null && StatusHelper.PlayerHasStatus(true, StatusID.Protraction))
		{
			if (DeploymentTacticsPvE.CanUse(out act)) return true;
		}

		// Deployment Tactics is modified in the base rotation to only use if they have galvanize so can trust that targets are at least valid?
		// The number of times that we adlo to heal a DPS and then would like to use this is actually reasonably high in dungeons
		// TODO: This is typically skipping because the target it's trying to cast the area defense on isn't the galvanized target
		if (DeploymentTacticsPvE.EnoughLevel && InCombat && (!RecitationPvE.EnoughLevel || RecitationPvE.Cooldown.IsCoolingDown))
		{
			if (DeploymentTacticsUsage == DeploymentTacticsUsageStrategy.CatalyzeOnly)
			{
				if (DeploymentTacticsPvE.CanUse(out act))
				{
					var target = DeploymentTacticsPvE.Target.Target;
					// Avoid touching StatusList directly; HasStatus is safer but still guard it.
					if (target != null)
					{
						try
						{
							if (target.HasStatus(true, StatusID.Catalyze))
							{
								return true;
							}
						}
						catch
						{
							// Target may be invalid/disposed; ignore and continue.
						}
					}
				}
			}
			else if (DeploymentTacticsUsage == DeploymentTacticsUsageStrategy.CatalyzeOrGalvanize)
			{
				if (DeploymentTacticsPvE.CanUse(out act))
				{
					return true;
				}
			}
		}

		// Consolation is great if Seraph is up
		if (ConsolationPvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		// It's always good as a defensive ability
		if (ShouldUseSacredSoil(out act))
		{
			return true;
		}

		// Should be using this manually in high end content, but if it's enabled let's use it
		if (ExpedientPvE.CanUse(out act))
		{
			return true;
		}

		// Seraphism is really good but we want to save it if we can, and should alternate it with Summon Seraph outside of the hardest content
		if ((MultiHitRestrict && IsCastingMultiHit) || !MultiHitRestrict)
		{
			if ((SummonSeraphPvE.Cooldown.IsCoolingDown || CurrentMp <= EmergencyHealingMPThreshold) && SeraphismPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (WhisperingDawnPvE.Cooldown.IsCoolingDown && FeyBlessingPvE.Cooldown.IsCoolingDown)
		{
			if (SummonSeraphPvE.CanUse(out act))
			{
				return true;
			}
		}

		// It's better than nothing for an oGCD, and at level 40 is the only thing we've got
		if (FeyIlluminationPvE.CanUse(out act))
		{
			return true;
		}

		// 2.3 Dungeon Mode W2W Pre-Mitigation
		if (DungeonPreMitigation && !Target.IsBossFromTTK() && CombatTime > 0 && CombatTime < 5)
		{
			if (SacredSoilPvE.CanUse(out act)) return true;
			if (ExpedientPvE.CanUse(out act)) return true;
			// WhisperingDawn is in HealAreaAbility, so we'll just mitigate here
		}

		return base.DefenseAreaAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.ProtractionPvE, ActionID.ExcogitationPvE, ActionID.SacredSoilPvE)]
	protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
	{
		// 1.4 Smart Protraction
		if (ProtractionPvE.CanUse(out act))
		{
			// Try to protect the tank if a Tankbuster is incoming
			if (DamagePredictor.IsTankbusterPredictable(ProtractionPvE.Target.Target))
			{
				return true;
			}
			return true;
		}

		if (!HasRecitation && !IsLastAbility(false, RecitationPvE) && ExcogitationPvE.CanUse(out act))
		{
			return true;
		}

		if (ShouldUseSacredSoil(out act))
		{
			return true;
		}

		return base.DefenseSingleAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.ExpedientPvE)]
	protected override bool SpeedAbility(IAction nextGCD, out IAction? act)
	{
		// Should be using this manually in high end content, but if it's enabled let's use it
		if (InCombat && ExpedientPvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		return base.SpeedAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.ChainStratagemPvE, ActionID.EnergyDrainPvE, ActionID.BanefulImpactionPvE, ActionID.AetherflowPvE, ActionID.DissipationPvE)]
	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		// 6.2 Swiftcast for Movement Uptime (Beiruta)
		if (UseSwiftcastForMovement && IsMoving && MovingTime > 0.5f && SwiftcastPvE.CanUse(out act))
		{
			bool shouldSaveForRaise = false;
			foreach (var member in PartyMembers)
			{
				if (member.IsDead)
				{
					shouldSaveForRaise = true;
					break;
				}
			}
			if (!shouldSaveForRaise) return true;
		}

		// Count how many hostile targets are within 5 units
		int closeTargetCount = NumberOfHostilesInRangeOf(5);

		if (BanefulImpactionPvE.CanUse(out act) &&
			(closeTargetCount > 3 // Mobs are grouped up
			|| Target.IsBossFromTTK() // Or it's a boss
			|| Player != null && StatusHelper.PlayerWillStatusEndGCD(2, 0, true, StatusID.ImpactImminent))) // Or we'll lose the ability if we don't use it
		{
			return true;
		}

		if (IsBurst)
		{
			// 3.3 Advanced Burst Safety
			bool dpsDying = false;
			if (SafeChainStratagem)
			{
				int deadOrWeakDps = 0;
				foreach (var member in PartyMembers.GetJobCategory(JobRole.Melee, JobRole.RangedMagical, JobRole.RangedPhysical))
				{
					// 1.3 Guardian Angel (TriageEngine)
					// Prioritize targets with high risk scores
					if (TriageEngine.IsCriticalTriage(member))
					{
						dpsDying = true;
						break;
					}

					if (member.IsDead || member.HasStatus(false, StatusID.Weakness, StatusID.BrinkOfDeath))
					{
						deadOrWeakDps++;
					}
				}
				if (deadOrWeakDps >= 2)
				{
					dpsDying = true;
				}
			}

			// 1.2 Chain Stratagem Sync
			if (!dpsDying && ChainStratagemPvE.CanUse(out act) && (!Target.IsBossFromTTK() || Target.GetHealthRatio() > 0.05f))
			{
				return true;
			}

			// We could likely make better decisions for both this and energy drain if JobGauge.Aetherflow was available
			if (!HasAetherflow && AetherflowPvE.Cooldown.IsCoolingDown && // No Aether and aetherflow is on cooldown
				((SummonSeraphPvE.Cooldown.IsCoolingDown && SeraphTime <= 0 && WhisperingDawnPvE.Cooldown.IsCoolingDown && FeyBlessingPvE.Cooldown.IsCoolingDown) // And all our fairy abilities on are cooldown
				|| (Target.IsBossFromTTK() && Target.IsDying()) // Or the boss is dying and we can snag some aether to carry forward
				|| (ShouldDissipate && Target.HasStatus(true, StatusID.ChainStratagem) && PartyMembersAverHP > 0.75f)) // 2.2 Offensive Dissipation safely
				&& DissipationPvE.CanUse(out act))
			{
				return true;
			}
		}

		// 3.4 Aetherflow Overcap Dump
		if ((ShouldDissipate && DissipationPvE.EnoughLevel && DissipationPvE.Cooldown.WillHaveOneChargeGCD(2) && DissipationPvE.IsEnabled) || AetherflowPvE.Cooldown.WillHaveOneCharge(5f))
		{
			if (EnergyDrainPvE.CanUse(out act, usedUp: true))
			{
				return true;
			}
		}

		// 1.1 Aetherflow Budgeting
		bool safeToUseEnergyDrain = true;
		if (ReserveAetherflowForExcog && Target.IsBossFromTTK() && SCHAetherFlowStacks <= 1)
		{
			if (!ExcogitationPvE.Cooldown.IsCoolingDown || !SacredSoilPvE.Cooldown.IsCoolingDown)
			{
				safeToUseEnergyDrain = false;
			}
		}

		if (safeToUseEnergyDrain && SCHAetherFlowStacks > 0 && EnergyDrainPvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		if (!HasAetherflow && AetherflowPvE.CanUse(out act))
		{
			return true;
		}

		if (HasBuffs && UseBurstMedicine(out act))
		{
			return true;
		}

		return base.AttackAbility(nextGCD, out act);
	}
	#endregion

	#region GCD Logic
	[RotationDesc(ActionID.SuccorPvE, ActionID.ConcitationPvE, ActionID.AccessionPvE)]
	protected override bool HealAreaGCD(out IAction? act)
	{
		if (HasHealingLockout)
		{
			act = null;
			return false;
		}

		if ((HasSwift || IsLastAction(ActionID.SwiftcastPvE)) && SwiftLogic && MergedStatus.HasFlag(AutoStatus.Raise))
		{
			return base.HealAreaGCD(out act);
		}

		// If emergency tactics is up we are using succor for raidwide recovery not shields
		if (HasEmergencyTactics && SuccorPvE.CanUse(out act, skipStatusProvideCheck: true))
		{
			return true;
		}

		// Only have all 3 checks in case players have added their own custom configurations based on current level.
		if (AccessionPvE.CanUse(out act, skipCastingCheck: true))
		{
			return true;
		}

		if (ConcitationPvE.CanUse(out act))
		{
			return true;
		}

		if (SuccorPvE.CanUse(out act))
		{
			return true;
		}

		return base.HealAreaGCD(out act);
	}

	[RotationDesc(ActionID.AdloquiumPvE, ActionID.ManifestationPvE, ActionID.PhysickPvE)]
	protected override bool HealSingleGCD(out IAction? act)
	{
		if ((HasSwift || IsLastAction(ActionID.SwiftcastPvE)) && SwiftLogic && MergedStatus.HasFlag(AutoStatus.Raise))
		{
			return base.HealSingleGCD(out act);
		}

		// 2.4 Esuna Priority System
		if (EsunaPvE.CanUse(out act))
		{
			var tar = EsunaPvE.Target.Target;
			if (tar != null)
			{
				bool hasParalysis = tar.HasStatus(false, StatusID.Paralysis);
				bool hasSlow = tar.HasStatus(false, StatusID.Slow);

				if (tar.DoomNeedHealing() || hasParalysis || hasSlow)
				{
					return true;
				}
			}
		}

		if (ManifestationPvE.CanUse(out act, skipCastingCheck: true))
		{
			return true;
		}

		if (AdloquiumPvE.CanUse(out act))
		{
			return true;
		}

		if (PhysickPvE.CanUse(out act))
		{
			return true;
		}

		return base.HealSingleGCD(out act);
	}

	[RotationDesc(ActionID.SuccorPvE, ActionID.ConcitationPvE, ActionID.AccessionPvE)]
	protected override bool DefenseAreaGCD(out IAction? act)
	{
		if ((HasSwift || IsLastAction(ActionID.SwiftcastPvE)) && SwiftLogic && MergedStatus.HasFlag(AutoStatus.Raise))
		{
			return base.DefenseAreaGCD(out act);
		}

		// 3.1 Pre-Shielding Engine (Raidwide Prediction via Global Helper)
		if (DamagePredictor.IsRaidwidePredictable())
		{
			bool hasShield = false;
			foreach (var member in PartyMembers)
			{
				if (member.HasStatus(true, StatusID.Galvanize, StatusID.EukrasianDiagnosis))
				{
					hasShield = true;
					break;
				}
			}

			if (!hasShield)
			{
				if (AccessionPvE.CanUse(out act, skipCastingCheck: true)) return true;
				if (ConcitationPvE.CanUse(out act)) return true;
				if (SuccorPvE.CanUse(out act)) return true;
			}
		}

		// Only have all 3 checks in case players have added their own custom configurations.
		if (AccessionPvE.CanUse(out act, skipCastingCheck: true))
		{
			return true;
		}

		if (ConcitationPvE.CanUse(out act))
		{
			return true;
		}

		if (SuccorPvE.CanUse(out act))
		{
			return true;
		}

		return base.DefenseAreaGCD(out act);
	}

	[RotationDesc(ActionID.ResurrectionPvE)]
	protected override bool RaiseGCD(out IAction? act)
	{
		if (ResurrectionPvE.CanUse(out act))
		{
			return true;
		}

		return base.RaiseGCD(out act);
	}

	protected override bool GeneralGCD(out IAction? act)
	{
		if ((HasSwift || IsLastAction(ActionID.SwiftcastPvE)) && SwiftLogic && MergedStatus.HasFlag(AutoStatus.Raise))
		{
			return base.GeneralGCD(out act);
		}

		// Summon Eos
		if (SummonEosPvE.CanUse(out act))
		{
			return true;
		}

		// Don't use attacks if we're in a wipe scenario spamming rezzes and heals
		if (CurrentMp < EmergencyHealingMPThreshold)
		{
			return base.GeneralGCD(out act);
		}

		int nearbyHostiles = NumberOfHostilesInRangeOf(5);

		float expectedHPToLive12Seconds = 1f;

		int partyMemberCount = 0;
		foreach (IBattleChara _ in PartyMembers)
		{
			partyMemberCount++;
		}

		// Expect that players do ~ 10% of healer hp as DPS and ballpark to ensure we're not wasting dots on something that's going to die immediately based on nobody hitting it
		// This is still wildly overestimating mob survival in some contexts but initial TTK estimates from RSR can be poor based on how far mobs were being kited
		if (Player != null && UseBallparkTTK)
		{
			expectedHPToLive12Seconds = BallparkPercent * Player.MaxHp * partyMemberCount * 12;
		}

		/*
         *  Bio TTK should only be compared to Ruin 1; it's 24 seconds to break even; recommend 24 second TTK when not using it while moving
         *  Bio2/Biolysis TTK should be considered relative to other spells even for single target when adjusting TTK settings; recommend 12 seconds
         *      Ruin 2 [ level 54=12 seconds, level 64=9 seconds, level 72=12 seconds, level 94=9 seconds ]
         *      Ruin 1 [ level below 54=12 seconds ]
         *      Broil  [ level 54=18 seconds, level 64=18 seconds, level 72=15 seconds, level 82=15 seconds, level 94=12 seconds ]
         *      
         *  Level 10-46 -> DoT targets and then spam appropriate Ruins at them; no better choices
         *  Level 46 gets Art of War as an instant cast beats out other options when in range (enabling weaving + auto attacks)
         *  Level 54 Broil beats out Art of War for single target
         *  Level 64 Ruin II beats out Art of War for single target and AoW becomes strictly an AoE ability
         *  Level 72 Biolysis now breaks even against 4 targets
         *  Level 82 Broil 3 changes to Broil 4
         *  
        */
		if (!BioIiPvE.EnoughLevel)
		{
			if (BioPvE.CanUse(out act) && BioPvE.Target.Target.CurrentHp >= expectedHPToLive12Seconds)
			{
				return true;
			}

			if (RuinPvE.CanUse(out act))
			{
				return true;
			}

			if (BioPvE.CanUse(out act, skipTTKCheck: true))
			{
				return true;
			}
		}
		else if (!ArtOfWarPvE.EnoughLevel)
		{
			if (BioIiPvE.CanUse(out act) && BioIiPvE.Target.Target.CurrentHp >= expectedHPToLive12Seconds)
			{
				return true; // No better options still and configured TTK should cover whether we want to use it
			}

			if (RuinPvE.CanUse(out act))
			{
				return true;
			}
		}
		else if (!BroilMasteryTrait.EnoughLevel)
		{
			if (BioIiPvE.CanUse(out act) && nearbyHostiles < GetAoWBreakevenTargets() && BioIiPvE.Target.Target.CurrentHp >= expectedHPToLive12Seconds)
			{
				return true; // This is better against 2 targets IFF it will last >= 24 seconds
			}

			if (ArtOfWarPvE.CanUse(out act, skipAoeCheck: true) && nearbyHostiles > 0 && InCombat)
			{
				return true;
			}

			if (RuinPvE.CanUse(out act))
			{
				return true; // 25m range may still allow us to do better than AoW does even at same potency and with a cast time
			}
		}
		else if (!BroilMasteryIiTrait.EnoughLevel)
		{
			if (BioIiPvE.CanUse(out act) && nearbyHostiles < GetAoWBreakevenTargets() && BioIiPvE.Target.Target.CurrentHp >= expectedHPToLive12Seconds)
			{
				return true; // This is better against 3 targets IFF it will last >= 24 seconds
			}

			if (ArtOfWarPvE.CanUse(out act, skipAoeCheck: true) && nearbyHostiles > 1 && InCombat)
			{
				return true;
			}

			if (BroilPvE.CanUse(out act))
			{
				return true;
			}

			if (ArtOfWarPvE.CanUse(out act, skipAoeCheck: true) && nearbyHostiles > 0 && InCombat)
			{
				return true;
			}
		}
		else if (!BroilIiiPvE.EnoughLevel)
		{
			if (BioIiPvE.CanUse(out act) && nearbyHostiles < GetAoWBreakevenTargets() && BioIiPvE.Target.Target.CurrentHp >= expectedHPToLive12Seconds)
			{
				return true; // This is better against 3 targets IFF it will last >= 24 seconds
			}

			if (ArtOfWarPvE.CanUse(out act, skipAoeCheck: true) && nearbyHostiles > 1 && InCombat)
			{
				return true;
			}

			if (BroilIiPvE.CanUse(out act))
			{
				return true;
			}
		}
		else if (!BroilIvPvE.EnoughLevel)
		{
			if (BiolysisPvE.CanUse(out act) && nearbyHostiles < GetAoWBreakevenTargets() && BiolysisPvE.Target.Target.CurrentHp >= expectedHPToLive12Seconds)
			{
				return true; // This is better against 4 targets IFF it will last >= 27 seconds
			}

			if (ArtOfWarPvE.CanUse(out act, skipAoeCheck: true) && nearbyHostiles > 1 && InCombat)
			{
				return true;
			}

			if (BroilIiiPvE.CanUse(out act))
			{
				return true;
			}
		}
		else
		{
			if (BiolysisPvE.CanUse(out act) && nearbyHostiles < GetAoWBreakevenTargets() && BiolysisPvE.Target.Target.CurrentHp >= expectedHPToLive12Seconds)
			{
				return true; // This is better against 4 targets IFF it will last >= 27 seconds
			}

			if (ArtOfWarPvE.CanUse(out act, skipAoeCheck: true) && nearbyHostiles > 1 && InCombat)
			{
				return true;
			}

			if (BroilIvPvE.CanUse(out act))
			{
				return true;
			}
		}

		// Starting at 38, we always default to Ruin II when moving and no bio targets
		if (MovingTime > RuinTime && RuinIiPvE.CanUse(out act))
		{
			return true;
		}

		return base.GeneralGCD(out act);
	}
	#endregion

	#region Extra Methods

	// 3.2 Adaptive Co-Healer Synergy Helper
	private float GetAdaptedThreshold(float baseThreshold)
	{
		bool hasPureHealer = false;
		foreach (var member in PartyMembers)
		{
			if (member.IsJobs(ECommons.ExcelServices.Job.WHM, ECommons.ExcelServices.Job.AST))
			{
				hasPureHealer = true;
				break;
			}
		}
		return hasPureHealer ? Math.Max(0f, baseThreshold - 0.15f) : baseThreshold;
	}

	// If Excog or Indomitability are up, or if we are going to hard cast Adloquium, we should use Recitation on cooldown
	private bool ShouldUseRecitation(IAction nextGCD)
	{
		if (!RecitationPvE.EnoughLevel || RecitationPvE.Cooldown.IsCoolingDown)
		{
			return false;
		}

		// Big Excog the tank if they look dangerous
		if (!ExcogitationPvE.Cooldown.IsCoolingDown)
		{
			bool tankNeedsExcog = false;
			IEnumerable<IBattleChara> tanks = PartyMembers.GetJobCategory(JobRole.Tank);
			foreach (IBattleChara member in tanks)
			{
				if (member.GetHealthRatio() <= ExcogHeal && !member.NoNeedHealingInvuln())
				{
					tankNeedsExcog = true;
					break;
				}
			}
			if (tankNeedsExcog || MergedStatus.HasFlag(AutoStatus.DefenseSingle))
			{
				return true;
			}
		}

		// Check average hp as a quick check on whether we're in a raid wide healing situation
		if (!IndomitabilityPvE.Cooldown.IsCoolingDown && PartyMembersAverHP <= ReciteIndomitability)
		{
			return true;
		}

		// Or if we're desperate and hard casting Adlo (or potentially succor)
		IAction[] recitationActions = ReciteSuccor ? [AccessionPvE, ConcitationPvE, SuccorPvE, AdloquiumPvE, ManifestationPvE] : [AdloquiumPvE, ManifestationPvE];
		return nextGCD.IsTheSameTo(true, recitationActions);
	}

	private bool ShouldUseSacredSoil(out IAction? act)
	{
		bool passedSacredMoveCheck = false;

		// Check if we are allowed to use sacred soil while moving or have stopped moving for long enough
		if (SacredSoilTimeStill == 0 || StopMovingTime >= SacredSoilTimeStill || (SacredSoilBossExemption && Target.IsBossFromTTK()))
		{
			passedSacredMoveCheck = true;
		}

		if (passedSacredMoveCheck && SacredSoilPvE.CanUse(out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	private int GetAoWBreakevenTargets()
	{
		int targets;
		if (!ArtOfWarPvE.EnoughLevel)
		{
			targets = 100; // AoW is not available yet
		}
		else if (!BroilMasteryTrait.EnoughLevel)
		{
			targets = 3 - DotOffsetMobs; // Broil is not available yet
		}
		else if (!ArtOfWarMasteryTrait.EnoughLevel)
		{
			targets = 4 - DotOffsetMobs; // Broil3 is not available yet
		}
		else
		{
			targets = 5 - DotOffsetMobs; // Broil3 is available
		}
		return targets;
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

			return base.CanHealSingleSpell && (GCDHeal || aliveHealerCount == 1);
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

			return base.CanHealAreaSpell && (GCDHeal || aliveHealerCount == 1);
		}
	}
	#endregion
}