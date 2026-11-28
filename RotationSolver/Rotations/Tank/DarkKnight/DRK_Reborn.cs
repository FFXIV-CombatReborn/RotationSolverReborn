namespace RotationSolver.Rotations.Tank.DarkKnight;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.58", RotationType = RotationType.Reborn)]
[SourceCode(Path = "main/Rotations/Tank/DarkKnight/DRK_Reborn.cs")]

public sealed class DRK_Reborn : DarkKnightRotation, Updaters.ICdPlanRotation
{
	#region CD Plan

	[RotationConfig(CombatType.PvE, Name = "[CD Plan] Planned cooldowns", Tooltip =
		"Auto: Dark Missionary, Shadowed Vigil, Dark Mind, The Blackest Night, Oblation, Rampart and Reprisal respond to RSR's defense / heal signals (raidwide detection, Boss Mod or the commands).\n" +
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

	[RotationConfig(CombatType.PvE, Name = "    > Use planned invuln (Living Dead)", Parent = nameof(CdPlanFrom), ParentValue = CdPlanNames.BuiltInPlan, Tooltip =
		"On: the plan\'s invuln (Living Dead) are used in their window.\nOff: they are skipped.")]
	public bool UsePlannedInvuln { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > Use planned Provoke / Shirk", Parent = nameof(CdPlanFrom), ParentValue = CdPlanNames.BuiltInPlan, Tooltip =
		"On: the plan\'s Provoke / Shirk are used in their window.\nOff: they are skipped.")]
	public bool UsePlannedProvokeShirk { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > Use planned tank stance", Parent = nameof(CdPlanFrom), ParentValue = CdPlanNames.BuiltInPlan, Tooltip =
		"On: the plan\'s Tank Stance rows say when your stance should be on or off; it is turned on / off only if it isn\'t already, and RSR\'s auto tank stance doesn\'t turn it on during an Off row.\nOff: stance is left to you and RSR\'s auto tank stance.")]
	public bool UsePlannedTankStance { get; set; } = true;

	IEnumerable<(uint ActionId, string Config)> Updaters.ICdPlanRotation.OptionalPlannedActions =>
	[
		((uint)ActionID.LivingDeadPvE, nameof(UsePlannedInvuln)),
		((uint)ActionID.ProvokePvE, nameof(UsePlannedProvokeShirk)),
		((uint)ActionID.ShirkPvE, nameof(UsePlannedProvokeShirk)),
		((uint)ActionID.GritPvE, nameof(UsePlannedTankStance)),
		((uint)ActionID.ReleaseGritPvE, nameof(UsePlannedTankStance)),
	];

	bool Updaters.ICdPlanRotation.AllowPlannedAction(uint actionId) =>
		(UsePlannedInvuln || actionId != (uint)ActionID.LivingDeadPvE) &&
		(UsePlannedProvokeShirk || (actionId != (uint)ActionID.ProvokePvE && actionId != (uint)ActionID.ShirkPvE)) &&
		(UsePlannedTankStance || (actionId != (uint)ActionID.GritPvE && actionId != (uint)ActionID.ReleaseGritPvE));

	private const float PlannedBlackestNightReserveSeconds = 20f;

	private bool PlannedBlackestNightSoon =>
		Updaters.TimelineUpdater.PlannedWithin(PlannedBlackestNightReserveSeconds, TheBlackestNightPvE.ID);

	string? Updaters.ICdPlanRotation.PlannedActionWait(uint actionId) => CdPlanHelper.Wait(actionId);

	protected override bool ShouldDelayCommandAction(IBaseAction action) =>
		CdPlanHelper.HoldCommand(action, UseTimelinePlan);

	#endregion

	#region Config Options
	[RotationConfig(CombatType.PvE, Name = "Use provoke in opening if tank stance is on")]
	public bool UseProvokeInOpening { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Keep at least 3000 MP")]
	public bool TheBlackestNight { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use The Blackest Night on lowest HP party member during AOE scenarios")]
	public bool BlackLantern { get; set; } = false;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "Target health threshold needed to use Blackest Night with above option", Parent = nameof(BlackLantern))]
	private float BlackLanternRatio { get; set; } = 0.5f;

	[RotationConfig(CombatType.PvE, Name = "Use Oblation on lowest HP party member during AOE scenarios")]
	public bool OblationLantern { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Use Oblation last stack of Oblation for party members", Parent = nameof(OblationLantern))]
	public bool OblationLanternStack { get; set; } = false;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "Target health threshold needed to use Oblation with above option", Parent = nameof(OblationLantern))]
	private float OblationLanternRatio { get; set; } = 0.5f;

	[RotationConfig(CombatType.PvE, Name = "Opener action")]
	public OpenerActionStrategy OpenerActionUsage { get; set; } = OpenerActionStrategy.Unmend;

	public enum OpenerActionStrategy : byte
	{
		[Description("Use Unmend")]
		Unmend,

		[Description("Use Shadowstride")]
		Shadowstride,
	}
	#endregion

	#region Countdown Logic
	// Countdown logic to prepare for combat.
	// Includes logic for using Provoke, tank stances, and burst medicines.
	protected override IAction? CountDownAction(float remainTime)
	{
		//Provoke when has Shield.
		if (UseProvokeInOpening && remainTime <= CountDownAhead)
		{
			if (HasTankStance)
			{
				if (ProvokePvE.CanUse(out _))
				{
					return ProvokePvE;
				}
			}
		}

		if (remainTime < 1f && UseBurstMedicine(out var act))
		{
			return act;
		}

		if (remainTime <= 3f && TheBlackestNightPvE.CanUse(out act))
		{
			return act;
		}

		if (OpenerActionUsage == OpenerActionStrategy.Unmend)
		{
			if (remainTime <= 1f && UnmendPvE.CanUse(out act))
			{
				return act;
			}
		}

		if (OpenerActionUsage == OpenerActionStrategy.Shadowstride)
		{
			if (remainTime <= 1f && ShadowstridePvE.CanUse(out act))
			{
				return act;
			}
		}

		return base.CountDownAction(remainTime);
	}
	#endregion

	#region oGCD Logic
	// Decision-making for emergency abilities, focusing on Blood Weapon usage.
	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		if (CombatElapsedLessGCD(3) && (IsLastAction(false, UnmendPvE) || IsLastAction(false, HardSlashPvE)))
		{
			if (EdgeOfShadowPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (CdPlanHelper.TryPlanned(out act))
		{
			return true;
		}

		if (CdPlanHelper.TryPlannedStance(GritPvE, ReleaseGritPvE, out act))
		{
			return true;
		}

		return base.EmergencyAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.ShadowstridePvE)]
	protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
	{
		if (ShadowstridePvE.CanUse(out act))
		{
			return true;
		}
		return base.MoveForwardAbility(nextGCD, out act);
	}

	protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
	{

		return base.HealSingleAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.DarkMissionaryPvE, ActionID.ReprisalPvE)]
	protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
	{
		if (!AutoDefense)
		{
			return base.DefenseAreaAbility(nextGCD, out act);
		}

		if (!InTwoMIsBurst && BlackLantern && TheBlackestNightPvE.CanUse(out act, targetOverride: TargetType.LowHP) && !TheBlackestNightPvE.Target.Target.HasStatus(false, StatusID.Transcendent) && TheBlackestNightPvE.Target.Target.GetHealthRatio() <= BlackLanternRatio)
		{
			return true;
		}

		if (!IsLastAbility(false, OblationPvE))
		{
			if (!InTwoMIsBurst && OblationLantern && OblationPvE.CanUse(out act, usedUp: OblationLanternStack, targetOverride: TargetType.LowHP) && !OblationPvE.Target.Target.HasStatus(false, StatusID.Transcendent) && OblationPvE.Target.Target.GetHealthRatio() <= OblationLanternRatio)
			{
				return true;
			}
		}

		if (!InTwoMIsBurst && DarkMissionaryPvE.CanUse(out act))
		{
			return true;
		}

		if (!InTwoMIsBurst && ReprisalPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (!IsLastAbility(false, OblationPvE))
		{
			if (!InTwoMIsBurst && OblationPvE.CanUse(out act, skipStatusProvideCheck: false, targetOverride: TargetType.Self))
			{
				return true;
			}
		}

		return base.DefenseAreaAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.OblationPvE, ActionID.TheBlackestNightPvE, ActionID.DarkMindPvE, ActionID.ShadowWallPvE, ActionID.ShadowedVigilPvE, ActionID.RampartPvE, ActionID.ReprisalPvE)]
	protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
	{
		if (!AutoDefense)
		{
			return base.DefenseSingleAbility(nextGCD, out act);
		}

		//10
		if (!IsLastAbility(false, OblationPvE))
		{
			if (OblationPvE.CanUse(out act, usedUp: true, skipStatusProvideCheck: false, targetOverride: TargetType.Self))
			{
				return true;
			}
		}

		if (TheBlackestNightPvE.CanUse(out act, targetOverride: TargetType.Self))
		{
			return true;
		}
		//20
		if (DarkMindPvE.CanUse(out act))
		{
			return true;
		}

		//30
		if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && ShadowWallPvE.CanUse(out act))
		{
			return true;
		}

		if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && ShadowedVigilPvE.CanUse(out act))
		{
			return true;
		}

		//30-40
		if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && ShadowedVigilPvE.CanUse(out act) && ShadowedVigilPvE.EnoughLevel)
		{
			return true;
		}

		if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && ShadowWallPvE.CanUse(out act) && !ShadowedVigilPvE.EnoughLevel)
		{
			return true;
		}

		//20
		if (!ShadowWallPvE.EnoughLevel)
		{
			if (RampartPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (ShadowWallPvE.EnoughLevel && !ShadowedVigilPvE.EnoughLevel)
		{
			if (ShadowWallPvE.Cooldown.IsCoolingDown && ShadowWallPvE.Cooldown.ElapsedAfter(30) && RampartPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (ShadowedVigilPvE.EnoughLevel)
		{
			if (ShadowedVigilPvE.Cooldown.IsCoolingDown && ShadowedVigilPvE.Cooldown.ElapsedAfter(30) && RampartPvE.CanUse(out act))
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

	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		if (IsBurst)
		{
			if (InCombat && (IsLastGCD(false, SouleaterPvE) || !CombatElapsedLessGCD(4)))
			{
				if (DeliriumPvE.CanUse(out act))
				{
					return true;
				}
			}

			if (!DeliriumPvE.EnoughLevel)
			{
				if (BloodWeaponPvE.CanUse(out act))
				{
					return true;
				}
			}
			if (InCombat && (IsLastGCD(false, HardSlashPvE) || !CombatElapsedLessGCD(3)))
			{
				if (LivingShadowPvE.CanUse(out act, skipAoeCheck: true))
				{
					return true;
				}
			}
		}

		if (CombatElapsedLessGCD(4))
		{
			return base.AttackAbility(nextGCD, out act);
		}

		if (CheckDarkSide)
		{
			if (FloodOfDarknessPvE.CanUse(out act))
			{
				return true;
			}

			if (EdgeOfDarknessPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (!IsMoving && SaltedEarthPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (ShadowbringerPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (NumberOfHostilesInRange >= 3 && AbyssalDrainPvE.CanUse(out act))
		{
			return true;
		}

		if (CarveAndSpitPvE.CanUse(out act))
		{
			return true;
		}

		if (InTwoMIsBurst)
		{
			if (ShadowbringerPvE.CanUse(out act, usedUp: true, skipAoeCheck: true))
			{
				return true;
			}
		}

		if (SaltAndDarknessPvE.CanUse(out act))
		{
			return true;
		}

		return base.AttackAbility(nextGCD, out act);
	}
	#endregion

	#region GCD Logic
	protected override bool GeneralGCD(out IAction? act)
	{
		if (CombatElapsedLessGCD(4) && !IsLastGCD(false, SouleaterPvE) && NumberOfHostilesInRange < 2)
		{
			if (!HasDelirium && SouleaterPvE.CanUse(out act))
			{
				return true;
			}

			if (!HasDelirium && SyphonStrikePvE.CanUse(out act))
			{
				return true;
			}

			if (!HasDelirium && HardSlashPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (DisesteemPvE.CanUse(out act, skipComboCheck: true, skipAoeCheck: true))
		{
			return true;
		}

		//AOE Delirium
		if (ImpalementPvE.CanUse(out act))
		{
			return true;
		}

		if (QuietusPvE.CanUse(out act))
		{
			return true;
		}

		// Single Target Delirium
		if (TorcleaverPvE.CanUse(out act, skipComboCheck: true))
		{
			return true;
		}

		if (ComeuppancePvE.CanUse(out act, skipComboCheck: true))
		{
			return true;
		}

		if (ScarletDeliriumPvE.CanUse(out act, skipComboCheck: true))
		{
			return true;
		}

		if (BloodspillerPvE.CanUse(out act, skipComboCheck: true))
		{
			return true;
		}

		//AOE
		if (StalwartSoulPvE.CanUse(out act, skipAoeCheck: true) && NumberOfHostilesInRange > 0)
		{
			return true;
		}

		if (UnleashPvE.CanUse(out act))
		{
			return true;
		}

		//Single Target
		if (!HasDelirium && SouleaterPvE.CanUse(out act))
		{
			return true;
		}

		if (!HasDelirium && SyphonStrikePvE.CanUse(out act))
		{
			return true;
		}

		if (!HasDelirium && HardSlashPvE.CanUse(out act))
		{
			return true;
		}

		if (UnmendPvE.CanUse(out act))
		{
			return true;
		}

		return base.GeneralGCD(out act);
	}
	#endregion

	#region Extra Methods
	// Indicates whether the Dark Knight can heal using a single ability.
	public override bool CanHealSingleAbility => false;

	// Determines if currently in a burst phase based on cooldowns of key abilities.
	private bool InTwoMIsBurst => BloodWeaponPvE.Cooldown.IsCoolingDown && DeliriumPvE.Cooldown.IsCoolingDown && ((LivingShadowPvE.Cooldown.IsCoolingDown && !LivingShadowPvE.Cooldown.ElapsedAfter(15)) || !LivingShadowPvE.EnoughLevel);

	// Manages DarkSide ability based on several conditions.
	private bool CheckDarkSide
	{
		get
		{
			// A planned The Blackest Night is coming: keep its 3000 MP (Dark Arts makes Edge / Flood free).
			if (PlannedBlackestNightSoon && !HasDarkArts && CurrentMp < 6000)
			{
				return false;
			}

			if (DarkSideEndAfterGCD(3))
			{
				return true;
			}

			if ((InTwoMIsBurst && HasDarkArts) || (HasDarkArts && StatusHelper.PlayerHasStatus(true, StatusID.BlackestNight)) || (HasDarkArts && DarkSideEndAfterGCD(3)))
			{
				return true;
			}

			if (InTwoMIsBurst && BloodWeaponPvE.Cooldown.IsCoolingDown && LivingShadowPvE.Cooldown.IsCoolingDown && SaltedEarthPvE.Cooldown.IsCoolingDown && ShadowbringerPvE.Cooldown.CurrentCharges == 0 && CarveAndSpitPvE.Cooldown.IsCoolingDown
				&& (!TheBlackestNight || CurrentMp >= 6000))
			{
				return true;
			}

			return CurrentMp >= 8500;
		}
	}
	#endregion
}