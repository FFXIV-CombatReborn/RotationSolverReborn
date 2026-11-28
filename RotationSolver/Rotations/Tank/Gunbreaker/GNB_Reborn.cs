namespace RotationSolver.Rotations.Tank.Gunbreaker;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.58", RotationType = RotationType.Reborn)]
[SourceCode(Path = "main/Rotations/Tank/Gunbreaker/GNB_Reborn.cs")]

public sealed class GNB_Reborn : GunbreakerRotation, Updaters.ICdPlanRotation
{
	#region CD Plan

	[RotationConfig(CombatType.PvE, Name = "[CD Plan] Planned cooldowns", Tooltip =
		"Auto: Heart of Light, Great Nebula, Camouflage, Heart of Corundum, Rampart and Reprisal respond to RSR's defense / heal signals (raidwide detection, Boss Mod or the commands).\n" +
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

	[RotationConfig(CombatType.PvE, Name = "    > Use planned invuln (Superbolide)", Parent = nameof(CdPlanFrom), ParentValue = CdPlanNames.BuiltInPlan, Tooltip =
		"On: the plan\'s invuln (Superbolide) are used in their window.\nOff: they are skipped.")]
	public bool UsePlannedInvuln { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > Use planned Provoke / Shirk", Parent = nameof(CdPlanFrom), ParentValue = CdPlanNames.BuiltInPlan, Tooltip =
		"On: the plan\'s Provoke / Shirk are used in their window.\nOff: they are skipped.")]
	public bool UsePlannedProvokeShirk { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > Use planned tank stance", Parent = nameof(CdPlanFrom), ParentValue = CdPlanNames.BuiltInPlan, Tooltip =
		"On: the plan\'s Tank Stance rows say when your stance should be on or off; it is turned on / off only if it isn\'t already, and RSR\'s auto tank stance doesn\'t turn it on during an Off row.\nOff: stance is left to you and RSR\'s auto tank stance.")]
	public bool UsePlannedTankStance { get; set; } = true;

	IEnumerable<(uint ActionId, string Config)> Updaters.ICdPlanRotation.OptionalPlannedActions =>
	[
		((uint)ActionID.SuperbolidePvE, nameof(UsePlannedInvuln)),
		((uint)ActionID.ProvokePvE, nameof(UsePlannedProvokeShirk)),
		((uint)ActionID.ShirkPvE, nameof(UsePlannedProvokeShirk)),
		((uint)ActionID.RoyalGuardPvE, nameof(UsePlannedTankStance)),
		((uint)ActionID.ReleaseRoyalGuardPvE, nameof(UsePlannedTankStance)),
	];

	bool Updaters.ICdPlanRotation.AllowPlannedAction(uint actionId) =>
		(UsePlannedInvuln || actionId != (uint)ActionID.SuperbolidePvE) &&
		(UsePlannedProvokeShirk || (actionId != (uint)ActionID.ProvokePvE && actionId != (uint)ActionID.ShirkPvE)) &&
		(UsePlannedTankStance || (actionId != (uint)ActionID.RoyalGuardPvE && actionId != (uint)ActionID.ReleaseRoyalGuardPvE));

	string? Updaters.ICdPlanRotation.PlannedActionWait(uint actionId) => CdPlanHelper.Wait(actionId);

	protected override bool ShouldDelayCommandAction(IBaseAction action) =>
		CdPlanHelper.HoldCommand(action, UseTimelinePlan) ||
		(UseTimelinePlan && (JugularRipPvEReady || AbdomenTearPvEReady || EyeGougePvEReady || HypervelocityPvEReady || FatedBrandPvEReady));

	#endregion

	#region Config Options
	[RotationConfig(CombatType.PvE, Name = "How to use Aurora")]
	public AuroraUsageStrategy AuroraUsage { get; set; } = AuroraUsageStrategy.TankbusterTarget;

	[RotationConfig(CombatType.PvE, Name = "How to use Heart Of Stone/Heart Of Corundum")]
	public HeartOfStoneStrategy HeartOfStoneUsage { get; set; } = HeartOfStoneStrategy.TankbusterTarget;

	public enum HeartOfStoneStrategy : byte
	{
		[Description("Full target usage")]
		Fullusage,

		[Description("Only use on tankbuster targets prioritizing self")]
		TankbusterTarget,

		[Description("Only use on self")]
		SelfOnly,
	}

	public enum AuroraUsageStrategy : byte
	{
		[Description("Full target usage")]
		Fullusage,

		[Description("Only use on tankbuster targets prioritizing self")]
		TankbusterTarget,

		[Description("Only use on self")]
		SelfOnly,
	}

	#endregion

	#region Countdown Logic
	protected override IAction? CountDownAction(float remainTime)
	{
		if (remainTime <= 0.7 && LightningShotPvE.CanUse(out var act))
		{
			return act;
		}

		if (remainTime <= 1.2 && UseBurstMedicine(out act))
		{
			return act;
		}

		return base.CountDownAction(remainTime);
	}
	#endregion

	#region oGCD Logic
	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		if (JugularRipPvE.CanUse(out act))
		{
			return true;
		}

		if (AbdomenTearPvE.CanUse(out act))
		{
			return true;
		}

		if (EyeGougePvE.CanUse(out act))
		{
			return true;
		}

		if (HypervelocityPvE.CanUse(out act))
		{
			return true;
		}

		if (FatedBrandPvE.CanUse(out act))
		{
			return true;
		}

		if (CdPlanHelper.TryPlanned(out act))
		{
			return true;
		}

		if (CdPlanHelper.TryPlannedStance(RoyalGuardPvE, ReleaseRoyalGuardPvE, out act))
		{
			return true;
		}

		return base.EmergencyAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.TrajectoryPvE)]
	protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
	{
		if (TrajectoryPvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		return base.MoveForwardAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.HeartOfLightPvE, ActionID.ReprisalPvE)]
	protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
	{
		if (!AutoDefense)
		{
			return base.DefenseAreaAbility(nextGCD, out act);
		}

		if (nextGCD.IsTheSameTo(false, (ActionID)GnashingFangPvE.ID) && !NoMercyPvE.Cooldown.IsCoolingDown)
		{
			return base.DefenseAreaAbility(nextGCD, out act);
		}

		if (!HasNoMercy && HeartOfLightPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (!HasNoMercy && ReprisalPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		return base.DefenseAreaAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.HeartOfStonePvE, ActionID.NebulaPvE, ActionID.RampartPvE, ActionID.CamouflagePvE, ActionID.ReprisalPvE)]
	protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
	{
		if (!AutoDefense)
		{
			return base.DefenseSingleAbility(nextGCD, out act);
		}

		if (nextGCD.IsTheSameTo(false, (ActionID)GnashingFangPvE.ID) && !NoMercyPvE.Cooldown.IsCoolingDown)
		{
			return base.DefenseSingleAbility(nextGCD, out act);
		}

		//10
		if (CamouflagePvE.CanUse(out act))
		{
			return true;
		}
		//15

		if (HeartOfStonePvE.EnoughLevel)
		{
			switch (HeartOfStoneUsage)
			{
				case HeartOfStoneStrategy.SelfOnly:
					if (HeartOfCorundumPvE.EnoughLevel && HeartOfCorundumPvE.CanUse(out act, targetOverride: TargetType.Self))
					{
						return true;
					}
					if (!HeartOfCorundumPvE.EnoughLevel && HeartOfStonePvE.CanUse(out act, targetOverride: TargetType.Self))
					{
						return true;
					}
					break;

				case HeartOfStoneStrategy.TankbusterTarget:
					if (HeartOfCorundumPvE.EnoughLevel && HeartOfCorundumPvE.CanUse(out act, targetOverride: TargetType.Tankbuster))
					{
						return true;
					}
					if (!HeartOfCorundumPvE.EnoughLevel && HeartOfStonePvE.CanUse(out act, targetOverride: TargetType.Tankbuster))
					{
						return true;
					}
					break;

				case HeartOfStoneStrategy.Fullusage:
				default:
					if (HeartOfCorundumPvE.EnoughLevel && HeartOfCorundumPvE.CanUse(out act, targetOverride: TargetType.LowHP))
					{
						return true;
					}
					if (!HeartOfCorundumPvE.EnoughLevel && HeartOfStonePvE.CanUse(out act, targetOverride: TargetType.LowHP))
					{
						return true;
					}
					break;
			}
		}

		//30
		if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && GreatNebulaPvE.CanUse(out act) && GreatNebulaPvE.EnoughLevel)
		{
			return true;
		}

		if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && NebulaPvE.CanUse(out act) && !GreatNebulaPvE.EnoughLevel)
		{
			return true;
		}

		//20
		if (!NebulaPvE.EnoughLevel)
		{
			if (RampartPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (NebulaPvE.EnoughLevel && !GreatNebulaPvE.EnoughLevel)
		{
			if (NebulaPvE.Cooldown.IsCoolingDown && NebulaPvE.Cooldown.ElapsedAfter(30) && RampartPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (GreatNebulaPvE.EnoughLevel)
		{
			if (GreatNebulaPvE.Cooldown.IsCoolingDown && GreatNebulaPvE.Cooldown.ElapsedAfter(30) && RampartPvE.CanUse(out act))
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

	[RotationDesc(ActionID.AuroraPvE)]
	protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
	{
		if (nextGCD.IsTheSameTo(false, (ActionID)GnashingFangPvE.ID) && !NoMercyPvE.Cooldown.IsCoolingDown)
		{
			return base.HealSingleAbility(nextGCD, out act);
		}

		if (!IsLastAbility(ActionID.AuroraPvE))
		{
			switch (AuroraUsage)
			{
				case AuroraUsageStrategy.SelfOnly:
					if (AuroraPvE.CanUse(out act, targetOverride: TargetType.Self))
					{
						return true;
					}
					break;

				case AuroraUsageStrategy.TankbusterTarget:
					if (AuroraPvE.CanUse(out act, targetOverride: TargetType.Tankbuster))
					{
						return true;
					}
					break;

				case AuroraUsageStrategy.Fullusage:
				default:
					if (AuroraPvE.CanUse(out act, targetOverride: TargetType.Heal))
					{
						return true;
					}
					break;
			}
		}

		return base.HealSingleAbility(nextGCD, out act);
	}

	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		// ST No Mercy Logic
		if (NoMercyPvE.CanUse(out act))
		{
			if ((ReignOfBeastsPvE.EnoughLevel && HasBloodfest) || !BloodfestPvE.IsEnabled)
			{
				return true;
			}

			if (!ReignOfBeastsPvE.EnoughLevel && nextGCD.IsTheSameTo(false, (ActionID)GnashingFangPvE.ID, (ActionID)ReignOfBeastsPvE.ID))
			{
				return true;
			}

			if (!GnashingFangPvE.EnoughLevel && nextGCD.IsTheSameTo(false, (ActionID)BurstStrikePvE.ID))
			{
				return true;
			}

			if (!BurstStrikePvE.EnoughLevel && nextGCD.IsTheSameTo(false, (ActionID)SolidBarrelPvE.ID))
			{
				return true;
			}

			if (!SolidBarrelPvE.EnoughLevel && nextGCD.IsTheSameTo(false, (ActionID)BrutalShellPvE.ID))
			{
				return true;
			}

			if (!BrutalShellPvE.EnoughLevel && nextGCD.IsTheSameTo(false, (ActionID)KeenEdgePvE.ID))
			{
				return true;
			}
		}

		// AOE No Mercy Logic
		if (NoMercyPvE.CanUse(out act))
		{
			if (DemonSlicePvE.CanUse(out _) && nextGCD.IsTheSameTo(false, (ActionID)DoubleDownPvE.ID))
			{
				return true;
			}

			if (!DoubleDownPvE.EnoughLevel && nextGCD.IsTheSameTo(false, (ActionID)FatedCirclePvE.ID))
			{
				return true;
			}

			if (!FatedCirclePvE.EnoughLevel && nextGCD.IsTheSameTo(false, (ActionID)DemonSlaughterPvE.ID))
			{
				return true;
			}

			if (!DemonSlaughterPvE.EnoughLevel && nextGCD.IsTheSameTo(false, (ActionID)DemonSlicePvE.ID))
			{
				return true;
			}
		}

		if (BloodfestPvE.CanUse(out act))
		{
			return true;
		}

		if (nextGCD.IsTheSameTo(false, (ActionID)GnashingFangPvE.ID) && !NoMercyPvE.Cooldown.IsCoolingDown)
		{
			return base.AttackAbility(nextGCD, out act);
		}

		if (DangerZonePvE.CanUse(out act) && !DoubleDownPvE.EnoughLevel)
		{

			if (!IsFullParty && !(DangerZonePvE.Target.Target?.IsBossFromTTK() ?? false))
			{
				return true;
			}

			if (!GnashingFangPvE.EnoughLevel && (HasNoMercy || !NoMercyPvE.Cooldown.WillHaveOneCharge(15)))
			{
				return true;
			}

			if (HasNoMercy && !GnashingFangPvE.Cooldown.HasOneCharge)
			{
				return true;
			}

			if (!HasNoMercy && !GnashingFangPvE.Cooldown.WillHaveOneCharge(20))
			{
				return true;
			}
		}

		if (HasNoMercy && BowShockPvE.CanUse(out act, skipAoeCheck: true))
		{
			//AOE CHECK
			if (DemonSlicePvE.CanUse(out _) && !IsFullParty)
			{
				return true;
			}

			if (!SonicBreakPvE.EnoughLevel && HasNoMercy)
			{
				return true;
			}

			if (HasNoMercy && SonicBreakPvE.Cooldown.IsCoolingDown)
			{
				return true;
			}
		}

		if (HasNoMercy && IsLastGCD(ActionID.DoubleDownPvE) && BlastingZonePvE.CanUse(out act))
		{
			return true;
		}

		if (NoMercyPvE.Cooldown.IsCoolingDown && BloodfestPvE.Cooldown.IsCoolingDown && BlastingZonePvE.CanUse(out act))
		{
			return true;
		}

		return base.AttackAbility(nextGCD, out act);
	}
	#endregion

	#region GCD Logic
	protected override bool GeneralGCD(out IAction? act)
	{
		if (BurstStrikePvE.CanUse(out act))
		{
			if (IsAmmoCapped && BloodfestPvE.EnoughLevel && NoMercyPvE.Cooldown.WillHaveOneChargeGCD(1))
			{
				return true;
			}
		}

		if (InReignCombo)
		{
			if (LionHeartPvE.CanUse(out act, skipComboCheck: true))
			{
				return true;
			}

			if (NobleBloodPvE.CanUse(out act, skipComboCheck: true))
			{
				return true;
			}
		}

		if (!InGnashingFang && GnashingFangPvE.Cooldown.CurrentCharges < 2 && HasNoMercy && DoubleDownPvE.Cooldown.IsCoolingDown)
		{
			if (LionHeartPvE.CanUse(out act, skipComboCheck: true))
			{
				return true;
			}

			if (NobleBloodPvE.CanUse(out act, skipComboCheck: true))
			{
				return true;
			}

			if (ReignOfBeastsPvE.CanUse(out act, skipComboCheck: true))
			{
				return true;
			}
		}

		if (!InReignCombo)
		{
			if (AmmoComboStep == 0 && GnashingFangPvE.CanUse(out act, skipComboCheck: true))
			{
				return true;
			}

			if (HasNoMercy && DoubleDownPvE.CanUse(out act))
			{
				return true;
			}

			if (HasNoMercy && SonicBreakPvE.CanUse(out act))
			{
				return true;
			}

			if (AmmoComboStep == 0 && GnashingFangPvE.CanUse(out act, skipComboCheck: true, usedUp: HasNoMercy || GnashingFangPvE.Cooldown.WillHaveXChargesGCD(2, 1)))
			{
				return true;
			}

			if (SavageClawPvE.CanUse(out act, skipComboCheck: true))
			{
				return true;
			}

			if (WickedTalonPvE.CanUse(out act, skipComboCheck: true))
			{
				return true;
			}
		}

		if (BurstStrikePvE.CanUse(out act))
		{
			if (Ammo > 3 && OvercappedAmmo() > 0)
			{
				if (StatusHelper.PlayerWillStatusEndGCD(OvercappedAmmo(), 0, true, StatusID.Bloodfest))
				{
					return true;
				}
			}

			if (
				// Condition 1: No Mercy is active, AmmoComboStep is 0, and Gnashing Fang cooldown won't have a charge
				(HasNoMercy && AmmoComboStep == 0 && !GnashingFangPvE.Cooldown.WillHaveOneCharge(1)) ||

				// Condition 2: Last combo action was Brutal Shell, and either Ammo is capped or Bloodfest conditions are met
				(IsLastComboAction((ActionID)BrutalShellPvE.ID) &&
				(IsAmmoCapped || (BloodfestPvE.Cooldown.WillHaveOneCharge(6) && Ammo <= 2 && !NoMercyPvE.Cooldown.WillHaveOneCharge(10) && BloodfestPvE.EnoughLevel))) ||

				// Condition 3: Ammo is capped and one of the following is true:
				// - Last GCD was Brutal Shell
				// - Ready to Reign and last combo action was Keen Edge
				// - Gnashing Fang is available and No Mercy is active
				(IsAmmoCapped && (IsLastGCD(ActionID.BrutalShellPvE) || (HasReadyToReign && IsLastComboAction(false, KeenEdgePvE)) || (GnashingFangPvE.EnoughLevel && HasNoMercy)))
				)
			{
				return true;
			}
		}

		if (!InGnashingFang && !InReignCombo)
		{
			if (FatedCirclePvE.CanUse(out act))
			{
				return true;
			}

			if (DemonSlaughterPvE.CanUse(out act))
			{
				return true;
			}

			if (DemonSlicePvE.CanUse(out act))
			{
				return true;
			}

			if (SolidBarrelPvE.CanUse(out act))
			{
				return true;
			}

			if (BrutalShellPvE.CanUse(out act))
			{
				return true;
			}

			if (KeenEdgePvE.CanUse(out act))
			{
				return true;
			}
		}

		if (LightningShotPvE.CanUse(out act))
		{
			return true;
		}

		return base.GeneralGCD(out act);
	}
	#endregion
}