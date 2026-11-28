namespace RotationSolver.Rotations.Tank.Warrior;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.58", RotationType = RotationType.Reborn)]
[SourceCode(Path = "main/Rotations/Tank/Warrior/WAR_Reborn.cs")]

public sealed class WAR_Reborn : WarriorRotation, Updaters.ICdPlanRotation
{
	#region CD Plan

	[RotationConfig(CombatType.PvE, Name = "[CD Plan] Planned cooldowns", Tooltip =
		"Auto: Shake It Off, Damnation, Thrill of Battle, Bloodwhetting, Nascent Flash, Rampart and Reprisal respond to RSR's defense / heal signals (raidwide detection, Boss Mod or the commands).\n" +
		"Use built-in CD plan: they follow the plan picked below for the duty you're in.\n" +
		"BMR / VBM / Manual: you or your Boss Mod / BMR CD planner press them.\n" +
		"With the built-in plan or BMR / VBM / Manual, the defense / heal signals do not use them.")]
	public CdPlanSource CdPlanFrom { get; set; } = CdPlanSource.Auto;

	public bool UseTimelinePlan => CdPlanFrom == CdPlanSource.BuiltIn;

	private bool AutoDefense => CdPlanFrom == CdPlanSource.Auto;

	[RotationConfig(CombatType.PvE, Name = "    > Show CD Plan timeline", Parent = nameof(CdPlanFrom), ParentValue = CdPlanNames.BuiltInPlan, Tooltip =
		"A small window like the action timeline, showing the plan's actions coming up in the next 10s. It shows in a duty that has a CD plan; hover the window for lock / close.")]
	public bool ShowPlanTimeline { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Show timeline messages in chat", Parent = nameof(CdPlanFrom), ParentValue = CdPlanNames.BuiltInPlan, Tooltip =
		"Syncs and queued / skipped planned actions. Loading messages are always shown.")]
	public bool ShowTimelineMessages { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Use planned invuln (Holmgang)", Parent = nameof(CdPlanFrom), ParentValue = CdPlanNames.BuiltInPlan, Tooltip =
		"On: the plan\'s invuln (Holmgang) are used in their window.\nOff: they are skipped.")]
	public bool UsePlannedInvuln { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > Use planned Provoke / Shirk", Parent = nameof(CdPlanFrom), ParentValue = CdPlanNames.BuiltInPlan, Tooltip =
		"On: the plan\'s Provoke / Shirk are used in their window.\nOff: they are skipped.")]
	public bool UsePlannedProvokeShirk { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > Use planned tank stance", Parent = nameof(CdPlanFrom), ParentValue = CdPlanNames.BuiltInPlan, Tooltip =
		"On: the plan\'s Tank Stance rows say when your stance should be on or off; it is turned on / off only if it isn\'t already, and RSR\'s auto tank stance doesn\'t turn it on during an Off row.\nOff: stance is left to you and RSR\'s auto tank stance.")]
	public bool UsePlannedTankStance { get; set; } = true;

	IEnumerable<(uint ActionId, string Config)> Updaters.ICdPlanRotation.OptionalPlannedActions =>
	[
		((uint)ActionID.HolmgangPvE, nameof(UsePlannedInvuln)),
		((uint)ActionID.ProvokePvE, nameof(UsePlannedProvokeShirk)),
		((uint)ActionID.ShirkPvE, nameof(UsePlannedProvokeShirk)),
		((uint)ActionID.DefiancePvE, nameof(UsePlannedTankStance)),
		((uint)ActionID.ReleaseDefiancePvE, nameof(UsePlannedTankStance)),
	];

	bool Updaters.ICdPlanRotation.AllowPlannedAction(uint actionId) =>
		(UsePlannedInvuln || actionId != (uint)ActionID.HolmgangPvE) &&
		(UsePlannedProvokeShirk || (actionId != (uint)ActionID.ProvokePvE && actionId != (uint)ActionID.ShirkPvE)) &&
		(UsePlannedTankStance || (actionId != (uint)ActionID.DefiancePvE && actionId != (uint)ActionID.ReleaseDefiancePvE));

	string? Updaters.ICdPlanRotation.PlannedActionWait(uint actionId) => CdPlanHelper.Wait(actionId);

	protected override bool ShouldDelayCommandAction(IBaseAction action) =>
		CdPlanHelper.HoldCommand(action, UseTimelinePlan);

	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		if (CdPlanHelper.TryPlanned(out act))
		{
			return true;
		}

		if (CdPlanHelper.TryPlannedStance(DefiancePvE, ReleaseDefiancePvE, out act))
		{
			return true;
		}

		return base.EmergencyAbility(nextGCD, out act);
	}

	#endregion

	#region Config Options
	[RotationConfig(CombatType.PvE, Name = "Only use Nascent Flash if Tank Stance is off")]
	public bool NeverscentFlash { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Nascent Flash target priority")]
	public NascentFlashTargetStrategy NascentFlashTarget { get; set; } = NascentFlashTargetStrategy.LowestHP;

	public enum NascentFlashTargetStrategy : byte
	{
		[Description("Lowest HP party member")]
		LowestHP,

		[Description("Healers first, then lowest HP party member")]
		HealerFirst,

		[Description("Healers only")]
		HealerOnly,
	}

	[RotationConfig(CombatType.PvE, Name = "Use Bloodwhetting/Raw intuition on single enemies")]
	public bool SoloIntuition { get; set; } = false;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "Bloodwhetting/Raw intuition heal threshold")]
	public float HealIntuition { get; set; } = 0.7f;

	[RotationConfig(CombatType.PvE, Name = "Use both stacks of Onslaught during burst while standing still")]
	public bool YEETBurst { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use a stack of Onslaught when its about to overcap while standing still")]
	public bool YEETCooldown { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Use Inner Release while moving")]
	public bool InnerReleaseMoving { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use Primal Rend while moving (Dangerous)")]
	public bool YEET { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Use Primal Rend while standing still outside of configured melee range (Dangerous)")]
	public bool YEETStill { get; set; } = false;

	[Range(1, 20, ConfigUnitType.Yalms)]
	[RotationConfig(CombatType.PvE, Name = "Max distance you can be from the boss for Primal Rend use (Danger, setting too high will get you killed)")]
	public float PrimalRendDistance2 { get; set; } = 3.5f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "Nascent Flash Heal Threshold")]
	public float FlashHeal { get; set; } = 0.6f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "Thrill Of Battle Heal Threshold")]
	public float ThrillOfBattleHeal { get; set; } = 0.6f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "Equilibrium Heal Threshold")]
	public float EquilibriumHeal { get; set; } = 0.6f;

	#endregion

	#region Countdown Logic
	protected override IAction? CountDownAction(float remainTime)
	{
		if (remainTime < 0.54f && TomahawkPvE.CanUse(out var act))
		{
			return act;
		}
		return base.CountDownAction(remainTime);
	}
	#endregion

	#region oGCD Logic
	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		if (InfuriatePvE.CanUse(out act, gcdCountForAbility: 3))
		{
			return true;
		}

		if (!InnerReleasePvE.EnoughLevel && StatusHelper.PlayerHasStatus(true, StatusID.Berserk) && InfuriatePvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		if (CombatElapsedLessGCD(1))
		{
			return false;
		}

		if (!StatusHelper.PlayerWillStatusEndGCD(2, 0, true, StatusID.SurgingTempest)
			|| !StormsEyePvE.EnoughLevel)
		{
			if ((InnerReleaseMoving || !IsMoving) && InnerReleasePvE.CanUse(out act))
			{
				return true;
			}
			if ((InnerReleaseMoving || !IsMoving) && !InnerReleasePvE.Info.EnoughLevelAndQuest() && BerserkPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (IsBurstStatus && (InnerReleaseStacks == 0 || InnerReleaseStacks == 3))
		{
			if (InfuriatePvE.CanUse(out act, usedUp: true))
			{
				return true;
			}
		}

		if (CombatElapsedLessGCD(4))
		{
			return false;
		}

		if (OrogenyPvE.CanUse(out act))
		{
			return true;
		}

		if (UpheavalPvE.CanUse(out act))
		{
			return true;
		}

		if (StatusHelper.PlayerHasStatus(false, StatusID.Wrathful) && PrimalWrathPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (YEETBurst && OnslaughtPvE.CanUse(out act, usedUp: IsBurstStatus) &&
		   !IsMoving &&
		   !IsLastAction(false, OnslaughtPvE) &&
		   !IsLastAction(false, UpheavalPvE) &&
			StatusHelper.PlayerHasStatus(true, StatusID.SurgingTempest))
		{
			return true;
		}

		if (YEETCooldown && OnslaughtPvE.CanUse(out act, usedUp: true) &&
		   !IsMoving &&
		   !IsLastAction(false, OnslaughtPvE) &&
		   OnslaughtPvE.Cooldown.WillHaveXChargesGCD(OnslaughtMax, 1) &&
			StatusHelper.PlayerHasStatus(true, StatusID.SurgingTempest))
		{
			return true;
		}

		if (MergedStatus.HasFlag(AutoStatus.MoveForward) && MoveForwardAbility(nextGCD, out act))
		{
			return true;
		}

		return base.AttackAbility(nextGCD, out act);
	}

	protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
	{
		var _partyCount = 0;
		foreach (var _ in PartyMembers)
		{
			_partyCount++;
		}

		if ((InCombat && Player?.GetHealthRatio() < HealIntuition && NumberOfHostilesInRange > 0) || (InCombat && _partyCount == 1 && NumberOfHostilesInRange > 0))
		{
			if (BloodwhettingPvE.CanUse(out act))
			{
				return true;
			}
			if (!BloodwhettingPvE.Info.EnoughLevelAndQuest() && RawIntuitionPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (Player?.GetHealthRatio() < ThrillOfBattleHeal)
		{
			if (ThrillOfBattlePvE.CanUse(out act))
			{
				return true;
			}
		}

		if (!StatusHelper.PlayerHasStatus(true, StatusID.Holmgang_409))
		{
			if (Player?.GetHealthRatio() < EquilibriumHeal)
			{
				if (EquilibriumPvE.CanUse(out act))
				{
					return true;
				}
			}
		}

		if (StatusHelper.PlayerHasStatus(true, StatusID.PrimalRendReady) && InCombat && UseBurstMedicine(out act))
		{
			return true;
		}
		return base.GeneralAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.ShakeItOffPvE, ActionID.NascentFlashPvE)]
	protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
	{
		if (!AutoDefense)
		{
			return base.HealSingleAbility(nextGCD, out act);
		}

		if (ShakeItOffPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (InCombat && (!NeverscentFlash || !StatusHelper.PlayerHasStatus(true, StatusID.Defiance)))
		{
			if (NascentFlashTarget != NascentFlashTargetStrategy.LowestHP && NascentFlashCanUse(out act, healersOnly: true))
			{
				return true;
			}

			if (NascentFlashTarget != NascentFlashTargetStrategy.HealerOnly && NascentFlashCanUse(out act, healersOnly: false))
			{
				return true;
			}
		}

		return base.HealSingleAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.RawIntuitionPvE, ActionID.VengeancePvE, ActionID.RampartPvE, ActionID.RawIntuitionPvE, ActionID.ReprisalPvE)]
	protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
	{
		if (!AutoDefense)
		{
			return base.DefenseSingleAbility(nextGCD, out act);
		}

		var RawSingleTargets = SoloIntuition;
		act = null;

		if (StatusHelper.PlayerHasStatus(true, StatusID.Holmgang_409) && Player?.GetHealthRatio() < 0.3f)
		{
			return false;
		}

		if (RawIntuitionPvE.CanUse(out act) && (RawSingleTargets || NumberOfHostilesInRange > 2))
		{
			return true;
		}

		if (!StatusHelper.PlayerWillStatusEndGCD(0, 0, true, StatusID.Bloodwhetting, StatusID.RawIntuition))
		{
			return false;
		}

		if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && DamnationPvE.CanUse(out act) && DamnationPvE.EnoughLevel)
		{
			return true;
		}

		if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && VengeancePvE.CanUse(out act) && !DamnationPvE.EnoughLevel)
		{
			return true;
		}

		if (!VengeancePvE.EnoughLevel)
		{
			if (RampartPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (VengeancePvE.EnoughLevel && !DamnationPvE.EnoughLevel)
		{
			if (VengeancePvE.Cooldown.IsCoolingDown && VengeancePvE.Cooldown.ElapsedAfter(30) && RampartPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (DamnationPvE.EnoughLevel)
		{
			if (DamnationPvE.Cooldown.IsCoolingDown && DamnationPvE.Cooldown.ElapsedAfter(30) && RampartPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (ReprisalPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		return base.DefenseSingleAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.ShakeItOffPvE, ActionID.ReprisalPvE)]
	protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
	{
		if (!AutoDefense)
		{
			return base.DefenseAreaAbility(nextGCD, out act);
		}

		if (ShakeItOffPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (ReprisalPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		return base.DefenseAreaAbility(nextGCD, out act);
	}
	#endregion

	#region GCD Logic
	protected override bool GeneralGCD(out IAction? act)
	{
		if (!StatusHelper.PlayerWillStatusEndGCD(3, 0, true, StatusID.SurgingTempest))
		{
			if (ChaoticCyclonePvE.CanUse(out act))
			{
				return true;
			}

			if (InnerChaosPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (!StatusHelper.PlayerWillStatusEndGCD(3, 0, true, StatusID.SurgingTempest) && !StatusHelper.PlayerHasStatus(true, StatusID.NascentChaos) && InnerReleaseStacks > 0)
		{
			if (DecimatePvE.CanUse(out act, skipStatusProvideCheck: true))
			{
				return true;
			}
			if (!DecimatePvE.Info.EnoughLevelAndQuest() && SteelCyclonePvE.CanUse(out act, skipStatusProvideCheck: true))
			{
				return true;
			}

			if (FellCleavePvE.CanUse(out act, skipStatusProvideCheck: true))
			{
				return true;
			}
			if (!FellCleavePvE.Info.EnoughLevelAndQuest() && InnerBeastPvE.CanUse(out act, skipStatusProvideCheck: true))
			{
				return true;
			}
		}

		if (!StatusHelper.PlayerWillStatusEndGCD(3, 0, true, StatusID.SurgingTempest) && InnerReleaseStacks == 0)
		{
			if (PrimalRendPvE.CanUse(out act, skipAoeCheck: true))
			{
				if (PrimalRendPvE.Target.Target != null && PrimalRendPvE.Target.Target.DistanceToPlayer() <= PrimalRendDistance2)
				{
					return true;
				}
				if (YEET || (YEETStill && !IsMoving))
				{
					return true;
				}
			}
			if (PrimalRuinationPvE.CanUse(out act))
			{
				return true;
			}
		}

		// AOE
		if (!StatusHelper.PlayerWillStatusEndGCD(3, 0, true, StatusID.SurgingTempest) || !StormsEyePvE.EnoughLevel)
		{
			if (DecimatePvE.CanUse(out act, skipStatusProvideCheck: true))
			{
				return true;
			}
			if (!DecimatePvE.Info.EnoughLevelAndQuest() && SteelCyclonePvE.CanUse(out act))
			{
				return true;
			}
		}

		if (MythrilTempestPvE.CanUse(out act))
		{
			return true;
		}

		if (OverpowerPvE.CanUse(out act))
		{
			return true;
		}

		// Single Target
		if (!StatusHelper.PlayerWillStatusEndGCD(3, 0, true, StatusID.SurgingTempest) || !StormsEyePvE.EnoughLevel)
		{
			if (FellCleavePvE.CanUse(out act, skipStatusProvideCheck: true))
			{
				return true;
			}
			if (!FellCleavePvE.Info.EnoughLevelAndQuest() && InnerBeastPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (StormsEyePvE.CanUse(out act))
		{
			return true;
		}

		if (StormsPathPvE.CanUse(out act))
		{
			return true;
		}

		if (MaimPvE.CanUse(out act))
		{
			return true;
		}

		if (HeavySwingPvE.CanUse(out act))
		{
			return true;
		}

		// Ranged
		if (TomahawkPvE.CanUse(out act))
		{
			return true;
		}

		return base.GeneralGCD(out act);
	}
	#endregion

	#region Extra Methods
	private static bool IsBurstStatus => !StatusHelper.PlayerWillStatusEndGCD(0, 0, false, StatusID.InnerStrength);

	private bool NascentFlashCanUse(out IAction? act, bool healersOnly)
	{
		var healRatio = Math.Min(FlashHeal, NascentFlashPvE.Config.AutoHealRatio);
		return NascentFlashPvE.CanUse(out act,targetOverride: TargetType.LowHPPercent,
			targetFilter: t => t.GetHealthRatio() < healRatio && t.CanBeHealed() && (!healersOnly || t.IsJobCategory(JobRole.Healer)));
	}
	#endregion
}