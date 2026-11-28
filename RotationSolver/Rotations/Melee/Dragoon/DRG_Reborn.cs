namespace RotationSolver.Rotations.Melee.Dragoon;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.58", RotationType = RotationType.Reborn)]
[SourceCode(Path = "main/Rotations/Melee/Dragoon/DRG_Reborn.cs")]

public sealed class DRG_Reborn : DragoonRotation, Updaters.ICdPlanRotation
{
	#region CD Plan

	[RotationConfig(CombatType.PvE, Name = "[CD Plan] Planned cooldowns", Tooltip =
		"Auto: Feint respond to RSR's defense / heal signals (raidwide detection, Boss Mod or the commands).\n" +
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

	[RotationConfig(CombatType.PvE, Name = "    > Feint from", Parent = nameof(CdPlanFrom), ParentValue = CdPlanNames.BuiltInPlan, Tooltip =
		"Where planned Feint comes from. M1 / M2 profile: the duty's shared Feint plan for that melee slot; the job plan's own Feint rows are then not used (everything else still comes from the job plan).\n" +
		"Auto: M1, and once an M1 Feint turns out to be another melee's, M2 for the rest of the duty.\n" +
		"Job plan: Feint from the picked job plan, e.g. for double caster or your own agreement. Also used when the duty has no profile.")]
	public FeintProfile FeintFrom { get; set; } = FeintProfile.Auto;

	string Updaters.ICdPlanRotation.MitProfileAction => "Feint";

	MitProfileChoice Updaters.ICdPlanRotation.MitProfile => (MitProfileChoice)FeintFrom;

	string? Updaters.ICdPlanRotation.PlannedActionWait(uint actionId) => CdPlanHelper.Wait(actionId);

	protected override bool ShouldDelayCommandAction(IBaseAction action) =>
		CdPlanHelper.HoldCommand(action, UseTimelinePlan);

	#endregion

	#region Config Options
	[RotationConfig(CombatType.PvE, Name = "Use Doom Spike for damage uptime if out of melee range even if it breaks combo")]
	public bool DoomSpikeWhenever { get; set; } = true;

	[Range(1, 20, ConfigUnitType.Yalms)]
	[RotationConfig(CombatType.PvE, Name = "Max distance you need to be from the target for Stardiver useage")]
	public float StardiverDistance { get; set; } = 20;

	[RotationConfig(CombatType.PvE, Name = "Use Stardiver while moving")]
	public bool StardiverMoving { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use Dragonfire Dive while moving")]
	public bool DragonfireDiveMoving { get; set; } = true;

	[Range(1, 20, ConfigUnitType.Yalms)]
	[RotationConfig(CombatType.PvE, Name = "Max distance you need to be from the target for Dragonfire Dive useage")]
	public float DragonfireDiveDistance { get; set; } = 20;
	#endregion

	#region Additional oGCD Logic

	[RotationDesc(ActionID.WingedGlidePvE)]
	protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
	{
		if (IsLastAction(false, StardiverPvE))
		{
			return base.MoveForwardAbility(nextGCD, out act);
		}

		if (WingedGlidePvE.CanUse(out act, skipComboCheck: true))
		{
			return true;
		}
		return base.MoveForwardAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.ElusiveJumpPvE)]
	protected override bool MoveBackAbility(IAction nextGCD, out IAction? act)
	{
		if (IsLastAction(false, StardiverPvE))
		{
			return base.MoveBackAbility(nextGCD, out act);
		}

		if (ElusiveJumpPvE.CanUse(out act, skipComboCheck: true))
		{
			return true;
		}
		return base.MoveBackAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.FeintPvE)]
	protected sealed override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
	{
		if (IsLastAction(false, StardiverPvE))
		{
			return base.DefenseAreaAbility(nextGCD, out act);
		}

		if (AutoDefense && FeintPvE.CanUse(out act, skipComboCheck: true))
		{
			return true;
		}
		return base.DefenseAreaAbility(nextGCD, out act);
	}
	#endregion

	#region oGCD Logic
	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		if (IActionHelper.IsLastActionGCD() && (StardiverMoving || (!StardiverMoving && !IsMoving)))
		{
			if (StardiverPvE.CanUse(out act))
			{
				if (StardiverPvE.Target.Target.DistanceToPlayer() <= StardiverDistance)
				{
					return true;
				}
			}
		}

		if (CdPlanHelper.TryPlanned(out act))
		{
			return true;
		}

		if (HasBattleLitany && InCombat && UseBurstMedicine(out act))
		{
			return true;
		}

		return base.EmergencyAbility(nextGCD, out act);
	}

	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		if (IsLastAction(false, StardiverPvE))
		{
			return base.AttackAbility(nextGCD, out act);
		}

		if (DisembowelPvE.EnoughLevel)
		{
			if (!HasPowerSurge)
			{
				return base.AttackAbility(nextGCD, out act);
			}
		}

		if (IsBurst && InCombat && HasHostilesInRange)
		{
			var lsOnBigHit =
				nextGCD.IsTheSameTo(true, HeavensThrustPvE, DrakesbanePvE) &&
				(HasBattleLitany || LOTDTime > 0 ||
				 LanceChargePvE.Cooldown.IsCoolingDown || BattleLitanyPvE.Cooldown.IsCoolingDown);

			var lsOnAltFinishersDuringBuff =
				HasBattleLitany &&
				LOTDTime > 0 &&
				nextGCD.IsTheSameTo(true, ChaoticSpringPvE, LanceBarragePvE, WheelingThrustPvE, FangAndClawPvE);

			var lsLowLevel =
				(!DisembowelPvE.EnoughLevel && nextGCD.IsTheSameTo(true, VorpalThrustPvE)) ||
				(!FullThrustPvE.EnoughLevel && nextGCD.IsTheSameTo(true, VorpalThrustPvE, DisembowelPvE)) ||
				(!LanceChargePvE.EnoughLevel && nextGCD.IsTheSameTo(true, DisembowelPvE, FullThrustPvE)) ||
				(!BattleLitanyPvE.EnoughLevel && nextGCD.IsTheSameTo(true, FullThrustPvE)) ||
				(!HeavensThrustPvE.EnoughLevel && !DrakesbanePvE.EnoughLevel &&
				 HasBattleLitany && HasLanceCharge && LOTDTime > 0 &&
				 nextGCD.IsTheSameTo(true, ChaoticSpringPvE, LanceBarragePvE, WheelingThrustPvE, FangAndClawPvE));

			var lifeSurgeReady = lsOnBigHit || lsOnAltFinishersDuringBuff || lsLowLevel;

			// Hold Lance Charge briefly so it lines up with Battle Litany.
			if ((!BattleLitanyPvE.EnoughLevel || !BattleLitanyPvE.Cooldown.IsCoolingDown || !BattleLitanyPvE.Cooldown.WillHaveOneCharge(10))
				&& LanceChargePvE.CanUse(out act))
			{
				return true;
			}

			if (BattleLitanyPvE.CanUse(out act))
			{
				return true;
			}

			if (lifeSurgeReady && LifeSurgePvE.CanUse(out act, usedUp: HasLanceCharge))
			{
				return true;
			}
		}

		if (GeirskogulPvE.CanUse(out act))
		{
			return true;
		}

		if ((BattleLitanyPvE.EnoughLevel && HasBattleLitany) || (!BattleLitanyPvE.EnoughLevel))
		{
			if ((DragonfireDiveMoving || (!DragonfireDiveMoving && !IsMoving)) && DragonfireDivePvE.CanUse(out act))
			{
				if (DragonfireDivePvE.Target.Target.DistanceToPlayer() <= DragonfireDiveDistance)
				{
					return true;
				}
			}
		}

		if (HasBattleLitany || LOTDTime > 0
			|| nextGCD.IsTheSameTo(true, RaidenThrustPvE, DraconianFuryPvE))
		{
			if (WyrmwindThrustPvE.CanUse(out act, usedUp: true))
			{
				return true;
			}
		}

		if (!HighJumpPvE.EnoughLevel && LanceChargePvE.Cooldown.IsCoolingDown)
		{
			if (JumpPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (GeirskogulPvE.Cooldown.IsCoolingDown || LOTDTime > 0)
		{
			if (HighJumpPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (NastrondPvE.CanUse(out act))
		{
			return true;
		}

		if (StarcrossPvE.CanUse(out act))
		{
			return true;
		}

		if (RiseOfTheDragonPvE.CanUse(out act))
		{
			return true;
		}

		if (MirageDivePvE.CanUse(out act))
		{
			return true;
		}

		return base.AttackAbility(nextGCD, out act);
	}
	#endregion

	#region GCD Logic
	protected override bool GeneralGCD(out IAction? act)
	{
		var doomSpikeRightNow = DoomSpikeWhenever;

		if (CoerthanTormentPvE.CanUse(out act))
		{
			return true;
		}

		if (SonicThrustPvE.CanUse(out act, skipStatusProvideCheck: true))
		{
			return true;
		}

		if (LanceMasteryTrait.EnoughLevel)
		{
			if (HasDraconianFire)
			{
				if (DraconianFuryPvE.CanUse(out act))
				{
					return true;
				}
			}
			if (!HasDraconianFire)
			{
				if (DoomSpikePvE.CanUse(out act))
				{
					return true;
				}
			}
		}
		if (!LanceMasteryTrait.EnoughLevel)
		{
			if (DoomSpikePvE.CanUse(out act))
			{
				return true;
			}
		}

		if (DrakesbanePvE.CanUse(out act, skipStatusProvideCheck: true))
		{
			return true;
		}

		if (FangAndClawPvE.CanUse(out act))
		{
			return true;
		}

		if (WheelingThrustPvE.CanUse(out act))
		{
			return true;
		}

		if (LanceMasteryIiTrait.EnoughLevel)
		{
			if (HeavensThrustPvE.CanUse(out act))
			{
				return true;
			}
		}
		if (!LanceMasteryIiTrait.EnoughLevel)
		{
			if (FullThrustPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (LanceMasteryIiTrait.EnoughLevel)
		{
			if (ChaoticSpringPvE.CanUse(out act, skipStatusProvideCheck: true))
			{
				return true;
			}
		}
		if (!LanceMasteryIiTrait.EnoughLevel)
		{
			if (ChaosThrustPvE.CanUse(out act, skipStatusProvideCheck: true))
			{
				return true;
			}
		}

		if (LanceMasteryIvTrait.EnoughLevel)
		{
			if (SpiralBlowPvE.CanUse(out act, skipStatusProvideCheck: StatusHelper.PlayerWillStatusEndGCD(6, 0, true, StatusID.PowerSurge_2720)))
			{
				return true;
			}
		}
		if (!LanceMasteryIvTrait.EnoughLevel)
		{
			if (DisembowelPvE.CanUse(out act, skipStatusProvideCheck: StatusHelper.PlayerWillStatusEndGCD(6, 0, true, StatusID.PowerSurge_2720)))
			{
				return true;
			}
		}

		if (LanceMasteryIvTrait.EnoughLevel)
		{
			if (LanceBarragePvE.CanUse(out act))
			{
				return true;
			}
		}
		if (!LanceMasteryIvTrait.EnoughLevel)
		{
			if (VorpalThrustPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (LanceMasteryTrait.EnoughLevel)
		{
			if (HasDraconianFire)
			{
				if (RaidenThrustPvE.CanUse(out act))
				{
					return true;
				}
			}
			if (!HasDraconianFire)
			{
				if (TrueThrustPvE.CanUse(out act))
				{
					return true;
				}
			}
		}
		if (!LanceMasteryTrait.EnoughLevel)
		{
			if (TrueThrustPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (LanceMasteryTrait.EnoughLevel)
		{
			if (HasDraconianFire)
			{
				if (DraconianFuryPvE.CanUse(out act, skipComboCheck: doomSpikeRightNow))
				{
					return true;
				}
			}
			if (!HasDraconianFire)
			{
				if (DoomSpikePvE.CanUse(out act, skipComboCheck: doomSpikeRightNow))
				{
					return true;
				}
			}
		}
		if (!LanceMasteryTrait.EnoughLevel)
		{
			if (DoomSpikePvE.CanUse(out act, skipComboCheck: doomSpikeRightNow))
			{
				return true;
			}
		}

		if (!IsLastAction(true, WingedGlidePvE) && PiercingTalonPvE.CanUse(out act))
		{
			return true;
		}

		return base.GeneralGCD(out act);
	}
	#endregion
}
