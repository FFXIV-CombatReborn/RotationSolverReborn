namespace RotationSolver.Rotations.Melee.Ninja;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.58", RotationType = RotationType.Reborn)]
[SourceCode(Path = "main/Rotations/Melee/Ninja/NIN_Reborn.cs")]

public sealed class NIN_Reborn : NinjaRotation, Updaters.ICdPlanRotation
{
	#region CD Plan

	[RotationConfig(CombatType.PvE, Name = "[CD Plan] Planned cooldowns", Tooltip =
		"Auto: Feint and Shade Shift respond to RSR's defense / heal signals (raidwide detection, Boss Mod or the commands).\n" +
		"Use built-in CD plan: they follow the plan picked below for the duty you're in.\n" +
		"BMR / VBM / Manual: you or your Boss Mod / BMR CD planner press them.\n" +
		"With the built-in plan or BMR / VBM / Manual, the defense / heal signals do not use them.")]
	public CdPlanSource CdPlanFrom { get; set; } = CdPlanSource.Auto;

	public bool UseTimelinePlan => CdPlanFrom == CdPlanSource.BuiltIn;

	protected override bool AutoDefense => CdPlanFrom == CdPlanSource.Auto;

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

	[RotationConfig(CombatType.PvE, Name = "    > Use planned Shade Shift", Parent = nameof(CdPlanFrom), ParentValue = CdPlanNames.BuiltInPlan, Tooltip =
		"On: the plan's Shade Shift are used in their window.\nOff: they are skipped.")]
	public bool UsePlannedShadeShift { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > Downtime Mudra (experimental)", Parent = nameof(CdPlanFrom), ParentValue = CdPlanNames.BuiltInPlan, Tooltip =
		"On: plan rows naming a ninjutsu (Suiton, Raiton, ...) are used for downtime. While there is nothing to attack, the mudras for it start when the row opens and the ninjutsu goes off once the boss is targetable.\n" +
		"While there is nothing to attack and no such row is open, no other mudras are started.\n" +
		"Off: those rows are skipped.")]
	public bool DowntimeMudra { get; set; } = false;

	private IBaseAction[] DowntimeNinjutsu =>
		[SuitonPvE, HutonPvE, RaitonPvE, KatonPvE, DotonPvE, HyotonPvE, FumaShurikenPvE, HyoshoRanryuPvE, GokaMekkyakuPvE];

	private bool InDowntimeMudra => UseTimelinePlan && DowntimeMudra && InCombat && DataCenter.AllHostileTargets.Count == 0;

	// The ninjutsu named by the plan's open downtime row, as written.
	private IBaseAction? PlannedDowntimeRow
	{
		get
		{
			if (!InDowntimeMudra)
			{
				return null;
			}

			foreach (var ninjutsu in DowntimeNinjutsu)
			{
				if (Updaters.TimelineUpdater.PlannedOpenByName(ninjutsu.Name))
				{
					return ninjutsu;
				}
			}

			return null;
		}
	}

	private bool NeedsKassatsu(IBaseAction ninjutsu) =>
		ninjutsu.ID == HyoshoRanryuPvE.ID || ninjutsu.ID == GokaMekkyakuPvE.ID;

	// Hyosho Ranryu / Goka Mekkyaku rows: Kassatsu goes in first (as the logs do, during the downtime).
	private bool PlannedDowntimeKassatsu =>
		PlannedDowntimeRow is { } row && NeedsKassatsu(row) && !HasKassatsu && !KassatsuPvE.Cooldown.IsCoolingDown;

	// What the downtime mudras aim at: the row's ninjutsu; for a Kassatsu ninjutsu nothing until Kassatsu is up
	// (mudras wait), or Raiton when Kassatsu is on cooldown.
	private IBaseAction? PlannedDowntimeNinjutsu => PlannedDowntimeRow switch
	{
		null => null,
		var row when !NeedsKassatsu(row) || HasKassatsu => row,
		_ when KassatsuPvE.Cooldown.IsCoolingDown => RaitonPvE,
		_ => null,
	};

	private bool DowntimeMudraHold => InDowntimeMudra && !IsExecutingMudra && PlannedDowntimeNinjutsu == null;

	private bool IsDowntimeNinjutsu(uint actionId)
	{
		foreach (var action in AllBaseActions)
		{
			if (action.ID != actionId)
			{
				continue;
			}

			foreach (var ninjutsu in DowntimeNinjutsu)
			{
				if (ninjutsu.Name == action.Name)
				{
					return true;
				}
			}

			return false;
		}

		return false;
	}

	IEnumerable<(uint ActionId, string Config)> Updaters.ICdPlanRotation.OptionalPlannedActions
	{
		get
		{
			yield return ((uint)ActionID.ShadeShiftPvE, nameof(UsePlannedShadeShift));
			foreach (var ninjutsu in DowntimeNinjutsu)
			{
				yield return (ninjutsu.ID, nameof(DowntimeMudra));
			}
		}
	}

	bool Updaters.ICdPlanRotation.AllowPlannedAction(uint actionId) =>
		(UsePlannedShadeShift || actionId != (uint)ActionID.ShadeShiftPvE) &&
		(DowntimeMudra || !IsDowntimeNinjutsu(actionId));

	bool Updaters.ICdPlanRotation.RotationRunsPlannedAction(uint actionId) => IsDowntimeNinjutsu(actionId);

	string? Updaters.ICdPlanRotation.PlannedActionWait(uint actionId) => CdPlanHelper.Wait(actionId);

	protected override bool ShouldDelayCommandAction(IBaseAction action) =>
		CdPlanHelper.HoldCommand(action, UseTimelinePlan) ||
		(UseTimelinePlan && IsExecutingMudra);

	#endregion

	#region Config Options
	// Configuration properties for rotation behavior.

	[RotationConfig(CombatType.PvE, Name = "Use Hide")]
	public bool UseHide { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use Unhide")]
	public bool AutoUnhide { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use Mudras outside of combat when enemies are near")]
	public bool CombatMudra { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Use both stacks of Mudras")]
	public bool BurnMudraStacks { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Use Forked Raiju instead of Fleeting Raiju if you are outside of range (Dangerous)")]
	public bool ForkedUse { get; set; } = false;
	#endregion

	#region Tracking Properties
	// Properties to track RabbitMediumPvE failures and related information.
	//private int _rabbitMediumFailures = GetActionUsageCount((uint)ActionID.RabbitMediumPvE);
	private IBaseAction? _lastNinActionAim = null;
	// Holds the next ninjutsu action to perform.
	private IBaseAction? _ninActionAim = null;
	private static bool NoActiveNinjutsu => AdjustId(ActionID.NinjutsuPvE) == ActionID.NinjutsuPvE;
	private static bool RabbitMediumCurrent => AdjustId(ActionID.NinjutsuPvE) == ActionID.RabbitMediumPvE;
	private static bool FumaShurikenCurrent => AdjustId(ActionID.NinjutsuPvE) == ActionID.FumaShurikenPvE;
	private static bool KatonCurrent => AdjustId(ActionID.NinjutsuPvE) == ActionID.KatonPvE;
	private static bool RaitonCurrent => AdjustId(ActionID.NinjutsuPvE) == ActionID.RaitonPvE;
	private static bool HyotonCurrent => AdjustId(ActionID.NinjutsuPvE) == ActionID.HyotonPvE;
	private static bool HutonCurrent => AdjustId(ActionID.NinjutsuPvE) == ActionID.HutonPvE;
	private static bool DotonCurrent => AdjustId(ActionID.NinjutsuPvE) == ActionID.DotonPvE;
	private static bool SuitonCurrent => AdjustId(ActionID.NinjutsuPvE) == ActionID.SuitonPvE;
	private static bool GokaMekkyakuCurrent => AdjustId(ActionID.NinjutsuPvE) == ActionID.GokaMekkyakuPvE;
	private static bool HyoshoRanryuCurrent => AdjustId(ActionID.NinjutsuPvE) == ActionID.HyoshoRanryuPvE;

	private bool KeepKassatsuinBurst => !StatusHelper.PlayerWillStatusEndGCD(2, 0, true, StatusID.Kassatsu) && HasKassatsu && !InTrickAttack && !IsExecutingMudra;

	public override void DisplayRotationStatus()
	{
		ImGui.Text($"Last Ninjutsu Action Cleared From Queue: {_lastNinActionAim}");
		ImGui.Text($"Current Ninjutsu Action: {_ninActionAim}");
		ImGui.Text($"Ninjutsu ID: {AdjustId(ActionID.NinjutsuPvE)}");
	}
	#endregion

	#region CountDown Logic
	// Logic to determine the action to take during the countdown phase before combat starts.
	protected override IAction? CountDownAction(float remainTime)
	{
		_ = IsLastAction(false, HutonPvE);
		// Clears ninjutsu setup if countdown is more than 6 seconds or if Suiton is the aim but shouldn't be.
		if (remainTime > 6)
		{
			ClearNinjutsu();
		}

		// Decision-making for ninjutsu actions based on remaining time until combat starts.
		if (DoSuiton(out var act))
		{
			return act == SuitonPvE && remainTime > CountDownAhead ? null : act;
		}

		else if (remainTime < 5)
		{
			SetNinjutsu(SuitonPvE);
		}
		else if (remainTime < 6)
		{
			// If within 10 seconds to start, consider using Hide or setting up Huton.
			if (_ninActionAim == null && TenPvE.Cooldown.IsCoolingDown && HidePvE.CanUse(out act))
			{
				return act;
			}
		}
		return base.CountDownAction(remainTime);
	}
	#endregion

	#region Move Logic
	// Defines logic for actions to take when moving forward during combat.
	// This attribute associates the method with the Forked Raiju PvE action, 
	// indicating it's a relevant ability when considering movement-based actions.
	[RotationDesc(ActionID.ForkedRaijuPvE)]
	protected override bool MoveForwardGCD(out IAction? act)
	{
		// Checks if Forked Raiju, a movement-friendly ability, can be used. 
		// If so, sets it as the action to perform, returning true to indicate an action has been selected.
		if (ForkedRaijuPvE.CanUse(out act))
		{
			return true;
		}

		// If Forked Raiju is not available or not the best option, 
		// falls back to the base class's logic for choosing a move-forward action.
		return base.MoveForwardGCD(out act);
	}
	#endregion

	protected override void OnFrameUpdate()
	{
		if (IsInCountdown)
		{
			return;
		}

		if (!InCombat && !CombatMudra)
		{
			ClearNinjutsu();
		}

		if (IsLastAction(false, FumaShurikenPvE, KatonPvE, RaitonPvE, HyotonPvE, DotonPvE, SuitonPvE)
			|| (IsShadowWalking && (_ninActionAim == SuitonPvE || _ninActionAim == HutonPvE))
			|| (_ninActionAim == GokaMekkyakuPvE && IsLastGCD(false, GokaMekkyakuPvE))
			|| (_ninActionAim == HyoshoRanryuPvE && IsLastGCD(false, HyoshoRanryuPvE))
			|| (_ninActionAim == GokaMekkyakuPvE && !HasKassatsu)
			|| (_ninActionAim == HyoshoRanryuPvE && !HasKassatsu))
		{
			ClearNinjutsu();
		}

		if (PlannedDowntimeNinjutsu is { } planned)
		{
			SetNinjutsu(planned);
		}
		else if ((InCombat && HasHostilesInMaxRange) || (CombatMudra && HasHostilesInMaxRange && TenPvE.Cooldown.CurrentCharges == TenPvE.Cooldown.MaxCharges))
		{
			ChoiceNinjutsu();
		}
	}

	#region oGCD Logic
	// Determines the emergency abilities to use, overriding the base class implementation.
	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		if (RabbitMediumPvE.CanUse(out act))
		{
			return true;
		}

		if (!IsExecutingMudra && PlannedDowntimeKassatsu && KassatsuPvE.CanUse(out act))
		{
			return true;
		}

		if (!IsExecutingMudra && CdPlanHelper.TryPlanned(out act))
		{
			return true;
		}

		// If Ninjutsu is available or not in combat, defers to the base class's emergency ability logic.
		if (!NoNinjutsu || !InCombat)
		{
			return base.EmergencyAbility(nextGCD, out act);
		}

		// First priority is given to Kassatsu if it's available, allowing for an immediate powerful Ninjutsu.
		if (NoNinjutsu && !nextGCD.IsTheSameTo(false, ActionID.TenPvE, ActionID.ChiPvE, ActionID.JinPvE) && IsShadowWalking && KassatsuPvE.CanUse(out act))
		{
			return true;
		}

		if ((!TenChiJinPvE.Cooldown.IsCoolingDown || StatusHelper.PlayerWillStatusEndGCD(2, 0, true, StatusID.ShadowWalker)) && TrickAttackPvE.Cooldown.IsCoolingDown && MeisuiPvE.CanUse(out act))
		{
			return true;
		}

		if (TenriJindoPvE.CanUse(out act))
		{
			return true;
		}

		// If in a burst phase and not just starting combat, checks if Mug is available to generate additional Ninki.
		if (IsBurst && !CombatElapsedLess(5))
		{
			if (!DokumoriPvE.EnoughLevel)
			{
				if (MugPvE.CanUse(out act))
				{
					return true;
				}
			}
			else if (DokumoriPvE.EnoughLevel)
			{
				if (DokumoriPvE.CanUse(out act))
				{
					return true;
				}
			}
		}


		// Prioritizes using Suiton and Trick Attack for maximizing damage, especially outside the initial combat phase.
		if (!CombatElapsedLess(6))
		{
			// Attempts to use Trick Attack if it's available.
			if (!KunaisBanePvE.EnoughLevel)
			{
				if (TrickAttackPvE.CanUse(out act, skipStatusProvideCheck: IsShadowWalking))
				{
					return true;
				}
			}
			else if (KunaisBanePvE.EnoughLevel)
			{
				if (KunaisBanePvE.CanUse(out act, skipAoeCheck: true, skipStatusProvideCheck: IsShadowWalking))
				{
					return true;
				}
			}

			// If Trick Attack is on cooldown but will not be ready soon, considers using Meisui to recover Ninki.
			if (TrickAttackPvE.Cooldown.IsCoolingDown && !TrickAttackPvE.Cooldown.WillHaveOneCharge(19) && TenChiJinPvE.Cooldown.IsCoolingDown && TrickAttackPvE.Cooldown.IsCoolingDown && MeisuiPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (InTrickAttack && InCombat && UseBurstMedicine(out act))
		{
			return true;
		}

		// If none of the specific conditions are met, falls back to the base class's emergency ability logic.
		return base.EmergencyAbility(nextGCD, out act);
	}

	// Defines attack abilities to use during combat, overriding the base class implementation.
	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		// If Ninjutsu is available or not in combat, it exits early, indicating no attack action to perform.
		if (!NoNinjutsu || !InCombat)
		{
			return base.AttackAbility(nextGCD, out act);
		}

		// If the player is within Trick Attack's effective window, and Ten Chi Jin hasn't recently been used,
		// then Ten Chi Jin is set as the next action to perform.
		if (InTrickAttack && !StatusHelper.PlayerHasStatus(true, StatusID.ShadowWalker) && !TenPvE.Cooldown.ElapsedAfter(30) && TenChiJinPvE.CanUse(out act))
		{
			return true;
		}

		// If more than 5 seconds have passed in combat, checks if Bunshin is available to use.
		if (!CombatElapsedLess(5) && BunshinPvE.CanUse(out act))
		{
			return true;
		}

		// Special handling if within Trick Attack's effective window:
		if (InTrickAttack)
		{
			// If Dream Within A Dream is not yet available, checks if Assassinate can be used.
			if (DreamWithinADreamPvE.CanUse(out act))
			{
				return true;
			}
			if (!DreamWithinADreamPvE.Info.EnoughLevelAndQuest() && AssassinatePvE.CanUse(out act))
			{
				return true;
			}
		}

		// Checks for the use of Hellfrog Medium or Bhavacakra under certain conditions:
		// - Not in the Mug's effective window or within Trick Attack's window
		// - Certain cooldown conditions are met, or specific statuses are active.
		if ((!InMug || InTrickAttack)
			&& (!BunshinPvE.Cooldown.WillHaveOneCharge(10) || HasPhantomKamaitachi || MugPvE.Cooldown.WillHaveOneCharge(2)))
		{
			if (HellfrogMediumPvE.CanUse(out act, skipAoeCheck: !BhavacakraPvE.EnoughLevel))
			{
				return true;
			}

			if (BhavacakraPvE.CanUse(out act))
			{
				return true;
			}

			if (TenriJindoPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (Ninki == 100)
		{
			if (HellfrogMediumPvE.CanUse(out act, skipAoeCheck: !BhavacakraPvE.EnoughLevel))
			{
				return true;
			}

			if (BhavacakraPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (MergedStatus.HasFlag(AutoStatus.MoveForward) && MoveForwardAbility(nextGCD, out act))
		{
			return true;
		}
		// If none of the conditions are met, it falls back to the base class's implementation for attack ability.
		return base.AttackAbility(nextGCD, out act);
	}
	#endregion

	#region Ninjutsu Logic
	private void SetNinjutsu(IBaseAction act)
	{
		if (act == null || AdjustId(ActionID.NinjutsuPvE) == ActionID.RabbitMediumPvE)
		{
			return;
		}

		if (_ninActionAim != null && IsLastAction(false, TenPvE, JinPvE, ChiPvE, FumaShurikenPvE_18873, FumaShurikenPvE_18874, FumaShurikenPvE_18875))
		{
			return;
		}

		if (_ninActionAim != act)
		{
			_ninActionAim = act;
		}
	}

	// Clears the ninjutsu action aim, effectively resetting any planned ninjutsu action.
	private void ClearNinjutsu()
	{
		if (_ninActionAim != null)
		{
			_lastNinActionAim = _ninActionAim;
			_ninActionAim = null;
		}
	}

	// Logic for choosing which ninjutsu action to set up next, based on various game state conditions.
	private void ChoiceNinjutsu()
	{
		// Checks for Kassatsu status to prioritize high-impact Ninjutsu due to its buff.
		if (HasKassatsu)
		{
			if ((DeathBlossomPvE.CanUse(out _) || HakkeMujinsatsuPvE.CanUse(out _)) && GokaMekkyakuPvE.EnoughLevel && !IsLastAction(false, GokaMekkyakuPvE) && GokaMekkyakuPvE.IsEnabled && ChiPvE.Info.IsQuestUnlocked())
			{
				SetNinjutsu(GokaMekkyakuPvE);
			}
			else if (!DeathBlossomPvE.CanUse(out _) && !HakkeMujinsatsuPvE.CanUse(out _) && HyoshoRanryuPvE.EnoughLevel && !IsLastAction(false, HyoshoRanryuPvE) && HyoshoRanryuPvE.IsEnabled && JinPvE.Info.IsQuestUnlocked())
			{
				SetNinjutsu(HyoshoRanryuPvE);
			}
			else if (!IsShadowWalking && ShadowWalkerNeeded && !HyoshoRanryuPvE.EnoughLevel && HutonPvE.EnoughLevel && HutonPvE.IsEnabled && JinPvE.Info.IsQuestUnlocked())
			{
				SetNinjutsu(HutonPvE);
			}
			else if ((DeathBlossomPvE.CanUse(out _) || HakkeMujinsatsuPvE.CanUse(out _)) && !HyoshoRanryuPvE.EnoughLevel && KatonPvE.EnoughLevel && KatonPvE.IsEnabled && ChiPvE.Info.IsQuestUnlocked())
			{
				SetNinjutsu(KatonPvE);
			}
			else if (!DeathBlossomPvE.CanUse(out _) && !HakkeMujinsatsuPvE.CanUse(out _) && !HyoshoRanryuPvE.EnoughLevel && RaitonPvE.EnoughLevel && RaitonPvE.IsEnabled && ChiPvE.Info.IsQuestUnlocked())
			{
				SetNinjutsu(RaitonPvE);
			}
		}
		else if (TenPvE.CanUse(out _, usedUp: ShadowWalkerNeeded || InTrickAttack || TenPvE.Cooldown.WillHaveXChargesGCD(2, 2, 0) || BurnMudraStacks) && _ninActionAim == null)
		{
			//Aoe
			if (DeathBlossomPvE.CanUse(out _) || HakkeMujinsatsuPvE.CanUse(out _))
			{
				if (!HasDoton && !IsMoving && !IsLastGCD(true, DotonPvE) && DotonPvE.EnoughLevel && (!TenChiJinPvE.EnoughLevel || !TenChiJinPvE.Cooldown.WillHaveOneCharge(6)))
				{
					if (JinPvE.CanUse(out _) && DotonPvE.IsEnabled && JinPvE.Info.IsQuestUnlocked())
					{
						SetNinjutsu(DotonPvE);
					}
				}
				else if (KatonPvE.EnoughLevel && KatonPvE.IsEnabled && ChiPvE.Info.IsQuestUnlocked())
				{
					SetNinjutsu(KatonPvE);
				}
			}

			//Vulnerable
			if (ShadowWalkerNeeded && (!MeisuiPvE.Cooldown.IsCoolingDown || !TrickAttackPvE.Cooldown.IsCoolingDown || !KunaisBanePvE.Cooldown.IsCoolingDown) && !IsShadowWalking && !HasTenChiJin && SuitonPvE.EnoughLevel)
			{
				if (DeathBlossomPvE.CanUse(out _) && JinPvE.CanUse(out _) && JinPvE.Info.IsQuestUnlocked() && HutonPvE.IsEnabled)
				{
					SetNinjutsu(HutonPvE);
				}
				else if (JinPvE.CanUse(out _) && JinPvE.Info.IsQuestUnlocked() && SuitonPvE.IsEnabled && ((TrickAttackPvE.IsEnabled && !KunaisBanePvE.EnoughLevel) || (KunaisBanePvE.IsEnabled && KunaisBanePvE.EnoughLevel)))
				{
					SetNinjutsu(SuitonPvE);
				}
			}

			//Single
			if (!DeathBlossomPvE.CanUse(out _) && !HakkeMujinsatsuPvE.CanUse(out _) && !ShadowWalkerNeeded)
			{
				if (RaitonPvE.EnoughLevel && RaitonPvE.IsEnabled && ChiPvE.Info.IsQuestUnlocked() && (!StatusHelper.PlayerHasStatus(true, StatusID.RaijuReady) || (StatusHelper.PlayerHasStatus(true, StatusID.RaijuReady) && StatusHelper.PlayerStatusStack(true, StatusID.RaijuReady) < 3)))
				{
					SetNinjutsu(RaitonPvE);
				}

				if (FumaShurikenPvE.EnoughLevel && FumaShurikenPvE.IsEnabled && TenPvE.Info.IsQuestUnlocked() && (!RaitonPvE.EnoughLevel || (StatusHelper.PlayerHasStatus(true, StatusID.RaijuReady) && StatusHelper.PlayerStatusStack(true, StatusID.RaijuReady) == 3)))
				{
					SetNinjutsu(FumaShurikenPvE);
				}
			}
		}
	}
	#endregion

	#region Ninjutsu Execution
	private bool DoRabbitMedium(out IAction? act)
	{
		act = null;
		var ninjutsunId = AdjustId(NinjutsuPvE.ID);
		if (ninjutsunId == RabbitMediumPvE.ID)
		{
			if (RabbitMediumPvE.CanUse(out act))
			{
				return true;
			}

			ClearNinjutsu();
		}
		return false;
	}

	private bool DoTenChiJin(out IAction? act)
	{
		act = null;

		if (HasTenChiJin)
		{
			var tenId = AdjustId(TenPvE.ID);
			var chiId = AdjustId(ChiPvE.ID);
			var jinId = AdjustId(JinPvE.ID);

			//First
			if (tenId == FumaShurikenPvE_18873.ID
				&& !IsLastAction(false, FumaShurikenPvE_18875, FumaShurikenPvE_18873))
			{
				//AOE
				if (DeathBlossomPvE.CanUse(out _))
				{
					if (FumaShurikenPvE_18875.CanUse(out act))
					{
						return true;
					}
				}
				//Single
				if (FumaShurikenPvE_18873.CanUse(out act))
				{
					return true;
				}
			}

			//Second
			else if (tenId == KatonPvE_18876.ID && !IsLastAction(false, KatonPvE_18876))
			{
				if (KatonPvE_18876.CanUse(out act, skipAoeCheck: true))
				{
					return true;
				}
			}
			else if (chiId == RaitonPvE_18877.ID && !IsLastAction(false, RaitonPvE_18877))
			{
				if (RaitonPvE_18877.CanUse(out act, skipAoeCheck: true))
				{
					return true;
				}
			}
			//Others
			else if (jinId == SuitonPvE_18881.ID && !IsLastAction(false, SuitonPvE_18881))
			{
				if (SuitonPvE_18881.CanUse(out act, skipAoeCheck: true, skipStatusProvideCheck: true))
				{
					return true;
				}
			}
			else if (chiId == DotonPvE_18880.ID && !IsLastAction(false, DotonPvE_18880) && !HasDoton)
			{
				if (DotonPvE_18880.CanUse(out act, skipAoeCheck: true, skipStatusProvideCheck: true))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool DoHyoshoRanryu(out IAction? act)
	{
		act = null;

		if ((!TrickAttackPvE.Cooldown.IsCoolingDown || TrickAttackPvE.Cooldown.WillHaveOneCharge(StatusHelper.PlayerStatusTime(true, StatusID.Kassatsu))) && !IsExecutingMudra)
		{
			return false;
		}

		if (_ninActionAim == HyoshoRanryuPvE)
		{
			//Failed
			if (RabbitMediumCurrent)
			{
				ClearNinjutsu();
				act = null;
				return false;
			}
			//Action Execution
			else if (HyoshoRanryuCurrent)
			{
				if (HyoshoRanryuPvE.CanUse(out act, skipAoeCheck: true))
				{
					return true;
				}
			}
			//Second
			else if (FumaShurikenCurrent)
			{
				if (JinPvE_18807.CanUse(out act, usedUp: true))
				{
					return true;
				}
			}
			//First
			else if (NoActiveNinjutsu)
			{
				if (ChiPvE_18806.CanUse(out act, usedUp: true))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool DoGokaMekkyaku(out IAction? act)
	{
		act = null;

		if ((!TrickAttackPvE.Cooldown.IsCoolingDown || TrickAttackPvE.Cooldown.WillHaveOneCharge(StatusHelper.PlayerStatusTime(true, StatusID.Kassatsu))) && !IsExecutingMudra)
		{
			return false;
		}

		if (_ninActionAim == GokaMekkyakuPvE)
		{
			//Failed
			if (RabbitMediumCurrent)
			{
				ClearNinjutsu();
				act = null;
				return false;
			}
			//Action Execution
			else if (GokaMekkyakuCurrent)
			{
				if (GokaMekkyakuPvE.CanUse(out act, skipAoeCheck: true))
				{
					return true;
				}
			}
			//Second
			else if (FumaShurikenCurrent)
			{
				if (TenPvE_18805.CanUse(out act, usedUp: true))
				{
					return true;
				}
			}
			//First
			else if (NoActiveNinjutsu)
			{
				if (ChiPvE_18806.CanUse(out act, usedUp: true))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool DoSuiton(out IAction? act)
	{
		act = null;

		if (KeepKassatsuinBurst)
		{
			return false;
		}

		if (_ninActionAim == SuitonPvE)
		{
			//Failed
			if (RabbitMediumCurrent)
			{
				ClearNinjutsu();
				act = null;
				return false;
			}
			//Action Execution
			else if (SuitonCurrent)
			{
				if (SuitonPvE.CanUse(out act))
				{
					return true;
				}
			}
			//Third
			else if (RaitonCurrent)
			{
				if (JinPvE_18807.CanUse(out act, usedUp: true))
				{
					return true;
				}
			}
			//Second
			else if (FumaShurikenCurrent)
			{
				if (ChiPvE_18806.CanUse(out act, usedUp: true))
				{
					return true;
				}
			}
			//First
			else if (NoActiveNinjutsu)
			{
				if (TenPvE.CanUse(out act, usedUp: true))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool DoDoton(out IAction? act)
	{
		act = null;

		if (KeepKassatsuinBurst)
		{
			return false;
		}

		if (_ninActionAim == DotonPvE)
		{
			//Failed
			if (RabbitMediumCurrent)
			{
				ClearNinjutsu();
				act = null;
				return false;
			}
			//Action Execution
			else if (DotonCurrent)
			{
				if (DotonPvE.CanUse(out act, skipAoeCheck: true))
				{
					return true;
				}
			}
			//Third
			else if (HyotonCurrent)
			{
				if (ChiPvE_18806.CanUse(out act, usedUp: true))
				{
					return true;
				}
			}
			//Second
			else if (FumaShurikenCurrent)
			{
				if (JinPvE_18807.CanUse(out act, usedUp: true))
				{
					return true;
				}
			}
			//First
			else if (NoActiveNinjutsu)
			{
				if (TenPvE.CanUse(out act, usedUp: true))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool DoHuton(out IAction? act)
	{
		act = null;

		if (KeepKassatsuinBurst)
		{
			return false;
		}

		if (_ninActionAim == HutonPvE)
		{
			//Failed
			if (RabbitMediumCurrent)
			{
				ClearNinjutsu();
				act = null;
				return false;
			}
			//Action Execution
			else if (HutonCurrent)
			{
				if (HutonPvE.CanUse(out act, skipAoeCheck: true))
				{
					return true;
				}
			}
			//Third
			else if (HyotonCurrent)
			{
				if (TenPvE_18805.CanUse(out act, usedUp: true))
				{
					return true;
				}
			}
			//Second
			else if (FumaShurikenCurrent)
			{
				if (JinPvE_18807.CanUse(out act, usedUp: true))
				{
					return true;
				}
			}
			//First
			else if (NoActiveNinjutsu)
			{
				if (ChiPvE.CanUse(out act, usedUp: true))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool DoHyoton(out IAction? act)
	{
		act = null;

		if (KeepKassatsuinBurst)
		{
			return false;
		}

		if (_ninActionAim == HyotonPvE)
		{
			//Failed
			if (RabbitMediumCurrent)
			{
				ClearNinjutsu();
				act = null;
				return false;
			}
			//Action Execution
			else if (HyotonCurrent)
			{
				if (HyotonPvE.CanUse(out act))
				{
					return true;
				}
			}
			//Second
			else if (FumaShurikenCurrent)
			{
				if (JinPvE_18807.CanUse(out act, usedUp: true))
				{
					return true;
				}
			}
			//First
			else if (NoActiveNinjutsu)
			{
				if (ChiPvE.CanUse(out act, usedUp: true))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool DoRaiton(out IAction? act)
	{
		act = null;

		if (KeepKassatsuinBurst)
		{
			return false;
		}

		if (_ninActionAim == RaitonPvE)
		{
			//Failed
			if (RabbitMediumCurrent)
			{
				ClearNinjutsu();
				act = null;
				return false;
			}
			//Action Execution
			else if (RaitonCurrent)
			{
				if (RaitonPvE.CanUse(out act))
				{
					return true;
				}
			}
			//Second
			else if (FumaShurikenCurrent)
			{
				if (ChiPvE_18806.CanUse(out act, usedUp: true))
				{
					return true;
				}
			}
			//First
			else if (NoActiveNinjutsu)
			{
				if (TenPvE.CanUse(out act, usedUp: true))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool DoKaton(out IAction? act)
	{
		act = null;

		if (KeepKassatsuinBurst)
		{
			return false;
		}

		if (_ninActionAim == KatonPvE)
		{
			//Failed
			if (RabbitMediumCurrent)
			{
				ClearNinjutsu();
				act = null;
				return false;
			}
			//Action Execution
			else if (KatonCurrent)
			{
				if (KatonPvE.CanUse(out act, skipAoeCheck: true))
				{
					return true;
				}
			}
			//Second
			else if (FumaShurikenCurrent)
			{
				if (TenPvE_18805.CanUse(out act, usedUp: true))
				{
					return true;
				}
			}
			//First
			else if (NoActiveNinjutsu)
			{
				if (ChiPvE.CanUse(out act, usedUp: true))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool DoFumaShuriken(out IAction? act)
	{
		act = null;

		if (KeepKassatsuinBurst)
		{
			return false;
		}

		if (_ninActionAim == FumaShurikenPvE)
		{
			//Failed
			if (RabbitMediumCurrent)
			{
				ClearNinjutsu();
				act = null;
				return false;
			}
			//Action Execution
			else if (FumaShurikenCurrent)
			{
				if (FumaShurikenPvE.CanUse(out act))
				{
					return true;
				}
			}
			//First
			else if (NoActiveNinjutsu)
			{
				if (TenPvE.CanUse(out act, usedUp: true))
				{
					return true;
				}
			}
		}
		return false;
	}
	#endregion

	#region GCD Logic
	protected override bool EmergencyGCD(IAction? nextGCD, out IAction? act)
	{
		if (RabbitMediumPvE.CanUse(out act))
		{
			return true;
		}

		if (DoTenChiJin(out act))
		{
			return true;
		}

		if (DoRabbitMedium(out act))
		{
			return true;
		}

		if (_ninActionAim != null && !DowntimeMudraHold)
		{
			if (DoGokaMekkyaku(out act))
			{
				return true;
			}

			if (DoHuton(out act))
			{
				return true;
			}

			if (DoDoton(out act))
			{
				return true;
			}

			if (DoKaton(out act))
			{
				return true;
			}

			if (DoHyoshoRanryu(out act))
			{
				return true;
			}

			if (DoSuiton(out act))
			{
				return true;
			}

			if (DoHyoton(out act))
			{
				return true;
			}

			if (DoRaiton(out act))
			{
				return true;
			}

			if (DoFumaShuriken(out act))
			{
				return true;
			}
		}

		return base.EmergencyGCD(nextGCD, out act);
	}

	protected override bool GeneralGCD(out IAction? act)
	{
		if (!IsExecutingMudra && (InTrickAttack || InMug) && NoNinjutsu && !HasRaijuReady
			&& Player != null && !StatusHelper.PlayerHasStatus(true, StatusID.TenChiJin)
			&& PhantomKamaitachiPvE.CanUse(out act))
		{
			return true;
		}

		if (!IsExecutingMudra)
		{
			if (FleetingRaijuPvE.CanUse(out act))
			{
				return true;
			}

			if (ForkedUse && ForkedRaijuPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (IsExecutingMudra)
		{
			return base.GeneralGCD(out act);
		}

		// AOE
		if (HakkeMujinsatsuPvE.CanUse(out act))
		{
			return true;
		}

		if (DeathBlossomPvE.CanUse(out act))
		{
			return true;
		}

		//Single
		if (AeolianEdgePvE.EnoughLevel)
		{
			// If ArmorCrushPvE is not yet available, checks if AeolianEdgePvE can be used.
			if (!ArmorCrushPvE.EnoughLevel)
			{
				if (AeolianEdgePvE.CanUse(out act))
				{
					return true;
				}
			}
			else
			{
				if (Kazematoi == 0 && ArmorCrushPvE.CanUse(out act))
				{
					return true;
				}
				else if (Kazematoi > 0 && AeolianEdgePvE.CanUse(out act) && AeolianEdgePvE.Target.Target != null && CanHitPositional(EnemyPositional.Rear, AeolianEdgePvE.Target.Target))
				{
					return true;
				}
				else if (Kazematoi < 4 && ArmorCrushPvE.CanUse(out act) && ArmorCrushPvE.Target.Target != null && CanHitPositional(EnemyPositional.Flank, ArmorCrushPvE.Target.Target))
				{
					return true;
				}
				else if (Kazematoi > 0 && AeolianEdgePvE.CanUse(out act))
				{
					return true;
				}
				else if (Kazematoi < 4 && ArmorCrushPvE.CanUse(out act))
				{
					return true;
				}
			}
		}

		if (GustSlashPvE.CanUse(out act))
		{
			return true;
		}

		if (SpinningEdgePvE.CanUse(out act))
		{
			return true;
		}

		//Range
		if (!IsExecutingMudra)
		{
			if (ThrowingDaggerPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (StateEnabled && AutoUnhide && IsHidden)
		{
			StatusHelper.StatusOff(StatusID.Hidden);
		}

		if (!InCombat && _ninActionAim == null && UseHide
			&& TenPvE.Cooldown.IsCoolingDown && HidePvE.CanUse(out act))
		{
			return true;
		}

		return base.GeneralGCD(out act);
	}
	#endregion
}