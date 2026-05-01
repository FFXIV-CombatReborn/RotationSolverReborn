using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using Lumina.Excel.Sheets;
using RotationSolver.Commands;
using RotationSolver.IPC;
using RotationSolver.UI.HighlightTeachingMode;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule;

namespace RotationSolver.Updaters;

internal static class MajorUpdater
{
	private static TimeSpan _timeSinceUpdate = TimeSpan.Zero;

	// Gating and state for segmented updates
	private static bool _shouldRunThisCycle;
	private static bool _isValidThisCycle;
	private static bool _isActivatedThisCycle;
	private static bool _rotationsLoaded;

	// Cached GeneralAction sheet lookup (RowId -> GeneralAction RowId) for teaching mode highlighting
	private static Dictionary<uint, uint>? _generalActionLookup;

	// Reusable list for VFX cleanup to avoid per-frame allocations
	private static readonly List<VfxNewData> _vfxRemaining = [];

	public static bool IsValid
	{
		get
		{
			if (!Player.Available)
			{
				_rotationsLoaded = false;
				return false;
			}

			// Consider the game valid when not transitioning or logging out.
			if (Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51] || Svc.Condition[ConditionFlag.LoggingOut])
			{
				_rotationsLoaded = false;
				return false;
			}

			return true;
		}
	}

	private static Exception? _threadException;

	public static void Enable()
	{
		Svc.Framework.Update += RSRGateUpdate;
		Svc.Framework.Update += RSRTeachingClearUpdate;
		Svc.Framework.Update += RSRInvalidUpdate;
		Svc.Framework.Update += RSRActivatedCoreUpdate;
		Svc.Framework.Update += RSRActivatedHighlightUpdate;
		Svc.Framework.Update += RSRCommonUpdate;
		Svc.Framework.Update += RSRCleanupUpdate;
		Svc.Framework.Update += RSRRotationAndStateUpdate;
		Svc.Framework.Update += RSRMiscAndTargetFreelyUpdate;
		Svc.Framework.Update += RSRResetUpdate;
	}

	private static void RSRGateUpdate(IFramework framework)
	{
		try
		{
			// Throttle by MinUpdatingTime
			_timeSinceUpdate += framework.UpdateDelta;
			if (Service.Config.MinUpdatingTime > 0 && _timeSinceUpdate < TimeSpan.FromSeconds(Service.Config.MinUpdatingTime))
			{
				_shouldRunThisCycle = false;
				return;
			}

			_timeSinceUpdate = TimeSpan.Zero;
			_isValidThisCycle = IsValid;
			_isActivatedThisCycle = DataCenter.IsActivated();
			_shouldRunThisCycle = true;
			if (!Service.Config.TutorialDone)
			{
				RotationSolverPlugin.OpenFirstStartTutorial();
			}

			// Opportunistically load rotations if not yet loaded
			if (_isValidThisCycle && !_rotationsLoaded)
			{
				RotationUpdater.LoadBuiltInRotations();
				_rotationsLoaded = true;
			}
		}
		catch (Exception ex)
		{
			LogOnce("GateUpdate Exception", ex);
		}
	}

	private static void RSRTeachingClearUpdate(IFramework framework)
	{
		if (!_shouldRunThisCycle)
			return;

		if (Service.Config.TeachingMode)
		{
			try
			{
				HotbarHighlightManager.HotbarIDs.Clear();
			}
			catch (Exception ex)
			{
				LogOnce("HotbarHighlightManager.HotbarIDs.Clear Exception", ex);
			}
		}
	}

	private static void RSRInvalidUpdate(IFramework framework)
	{
		if (!_shouldRunThisCycle)
			return;

		if (!_isValidThisCycle)
		{
			try
			{
				RSCommands.UpdateRotationState();
				ActionUpdater.ClearNextAction();
				MiscUpdater.UpdateEntry();
				ActionUpdater.NextAction = ActionUpdater.NextGCDAction = null;
			}
			catch (Exception ex)
			{
				LogOnce("RSRInvalidUpdate Exception", ex);
			}

			// Do not run the rest of the cycle
			_shouldRunThisCycle = false;
		}
	}

	private static void RSRActivatedCoreUpdate(IFramework framework)
	{
		if (!_shouldRunThisCycle)
			return;

		var autoOnEnabled = Service.Config.AutoOnYes && (Service.Config.StartOnAllianceIsInCombat2
			|| Service.Config.StartOnAttackedBySomeone2
			|| Service.Config.StartOnFieldOpInCombat2
			|| Service.Config.StartOnPartyIsInCombat2) && !DataCenter.IsInDutyReplay();

		// Only call UpdateTargets once — cover both the auto-on check and the activated path
		if (autoOnEnabled || _isActivatedThisCycle)
		{
			try
			{
				TargetUpdater.UpdateTargets();
			}
			catch (Exception ex)
			{
				LogOnce("(RSRActivatedCore): TargetUpdater.UpdateTargets Exception", ex);
			}
		}

		if (!_isActivatedThisCycle)
			return;

		// Target updater always needs to be first to update
		try
		{
			MacroUpdater.UpdateMacro();
		}
		catch (Exception ex)
		{
			LogOnce("(RSRActivatedCore): MacroUpdater.UpdateMacro Exception", ex);
		}

		if (DataCenter.BMREndabled)
		{
			try
			{
				BossModUpdater.Update();
			}
			catch (Exception ex)
			{
				LogOnce("(RSRActivatedCore): BossModUpdater.Update Exception", ex);
			}
		}

		try
		{
			StateUpdater.UpdateState();
		}
		catch (Exception ex)
		{
			LogOnce("(RSRActivatedCore): StateUpdater.UpdateState Exception", ex);
		}

		try
		{
			ActionUpdater.UpdateNextAction();
		}
		catch (Exception ex)
		{
			LogOnce("(RSRActivatedCore): ActionUpdater.UpdateNextAction Exception", ex);
		}

		bool canDoAction = false;
		try
		{
			canDoAction = ActionUpdater.CanDoAction();
		}
		catch (Exception ex)
		{
			LogOnce("(RSRActivatedCore): ActionUpdater.CanDoAction Exception", ex);
		}

		try
		{
			MovingUpdater.UpdateCanMove(canDoAction);
		}
		catch (Exception ex)
		{
			LogOnce("(RSRActivatedCore): MovingUpdater.UpdateCanMove Exception", ex);
		}

		if (canDoAction)
		{
			try
			{
				RSCommands.DoAction();
			}
			catch (Exception ex)
			{
				LogOnce("(RSRActivatedCore): RSCommands.DoAction Exception", ex);
			}
		}

		// In Target-Only mode, update the player's target from the computed next action without executing it.
		if (DataCenter.IsTargetOnly)
		{
			try
			{
				RSCommands.UpdateTargetFromNextAction();
			}
			catch (Exception ex)
			{
				LogOnce("(RSRActivatedCore): RSCommands.UpdateTargetFromNextAction (TargetOnly) Exception", ex);
			}
		}

		// In Teaching Mode with auto-target enabled, also update the player's target so it matches
		// the rotation's suggestion (important for tanks/healers where the optimal target varies).
		if (!DataCenter.IsTargetOnly && Service.Config.TeachingMode && Service.Config.TeachingModeAutoTarget && DataCenter.InCombat)
		{
			try
			{
				RSCommands.UpdateTargetFromNextAction();
			}
			catch (Exception ex)
			{
				LogOnce("(RSRActivatedCore): RSCommands.UpdateTargetFromNextAction (TeachingMode) Exception", ex);
			}
		}

		try
		{
			Wrath_IPCSubscriber.DisableAutoRotation();
		}
		catch (Exception ex)
		{
			LogOnce("(RSRActivatedCore): Wrath_IPCSubscriber.DisableAutoRotation Exception", ex);
		}
	}

	private static void RSRActivatedHighlightUpdate(IFramework framework)
	{
		if (!_shouldRunThisCycle || !_isActivatedThisCycle)
			return;

		// Handle Teaching Mode Highlighting
		if (Service.Config.TeachingMode && ActionUpdater.NextAction is not null)
		{
			try
			{
				IAction nextAction = ActionUpdater.NextAction;
				HotbarID? hotbar = null;
				if (nextAction is IBaseItem item)
				{
					hotbar = new HotbarID(HotbarSlotType.Item, item.ID);
				}
				else if (nextAction is IBaseAction baseAction)
				{
					hotbar = baseAction.Action.ActionCategory.RowId is 10 or 11
							? GetGeneralActionHotbarID(baseAction)
							: new HotbarID(HotbarSlotType.Action, baseAction.AdjustedID);
				}

				if (hotbar.HasValue)
				{
					_ = HotbarHighlightManager.HotbarIDs.Add(hotbar.Value);
				}
			}
			catch (Exception ex)
			{
				LogOnce("Hotbar Highlighting Exception", ex);
			}
		}

		// Apply reddening of disabled actions on hotbars alongside highlight
		if (Service.Config.ReddenDisabledHotbarActions)
		{
			try
			{
				HotbarDisabledColor.ApplyFrame();
			}
			catch (Exception ex)
			{
				LogOnce("Hotbar Disabled Redden Exception", ex);
			}
		}
	}

	private static void RSRCommonUpdate(IFramework framework)
	{
		if (!_shouldRunThisCycle)
			return;

		try
		{
			// Update various combat tracking parameters,
			ActionUpdater.UpdateCombatInfo();

			// Update timing tweaks
			ActionManagerEx.Instance.UpdateTweaks();

			// Update displaying the additional UI windows
			RotationSolverPlugin.UpdateDisplayWindow();
		}
		catch (Exception ex)
		{
			LogOnce("CommonUpdate Exception", ex);
		}
	}

	private static void RSRCleanupUpdate(IFramework framework)
	{
		if (!_shouldRunThisCycle)
			return;

		try
		{
			// Handle system warnings
			if (DataCenter.SystemWarnings.Count > 0)
			{
				DateTime now = DateTime.Now;
				List<string> keysToRemove = [];
				foreach (KeyValuePair<string, DateTime> kvp in DataCenter.SystemWarnings)
				{
					if (kvp.Value + TimeSpan.FromMinutes(10) < now)
					{
						keysToRemove.Add(kvp.Key);
					}
				}
				foreach (string key in keysToRemove)
				{
					_ = DataCenter.SystemWarnings.Remove(key);
				}
			}

			// Clear old VFX data
			if (!DataCenter.VfxDataQueue.IsEmpty)
			{
				// ConcurrentQueue does not support removal from the middle, and the previous
				// logic only removed from the head while the head entry was finished.
				// That could leave finished entries behind if an unfinished entry was at the front.
				// To reliably remove finished VFX entries, drain the queue and re-enqueue only
				// the unfinished items.
				_vfxRemaining.Clear();
				while (DataCenter.VfxDataQueue.TryDequeue(out var vfx))
				{
					try
					{
						// If we have a reasonable estimated duration, use it to determine whether the
						// VFX is still active. The hook currently provides remaining cast time at
						// creation which can be very small or zero; treat very small values as unknown
						// and keep those entries for a short default window to avoid immediate drops.
						if (vfx.Duration >= 0.5f)
						{
							if (vfx.TimeDuration.TotalSeconds <= vfx.Duration)
								_vfxRemaining.Add(vfx);
						}
						else
						{
							// Unknown / very short duration: keep for up to 5 seconds by default
							if (vfx.TimeDuration.TotalSeconds <= 5.0)
								_vfxRemaining.Add(vfx);
						}
					}
					catch
					{
						// On any unexpected error, keep the item to avoid data loss
						_vfxRemaining.Add(vfx);
					}
				}

				// Re-enqueue items that are still active
				foreach (var item in _vfxRemaining)
				{
					DataCenter.VfxDataQueue.Enqueue(item);
				}
			}
		}
		catch (Exception ex)
		{
			LogOnce("CleanupUpdate Exception", ex);
		}
	}

	private static void RSRRotationAndStateUpdate(IFramework framework)
	{
		if (!_shouldRunThisCycle)
			return;

		try
		{
			// Change loaded rotation based on job
			RotationUpdater.UpdateRotation();

			// Change RS state
			RSCommands.UpdateRotationState();

			if (Service.Config.TeachingMode)
			{
				try
				{
					HotbarHighlightManager.UpdateSettings();
				}
				catch (Exception ex)
				{
					LogOnce("HotbarHighlightManager.UpdateSettings Exception", ex);
				}
			}
		}
		catch (Exception ex)
		{
			LogOnce("RotationAndStateUpdate Exception", ex);
		}
	}

	private static void RSRMiscAndTargetFreelyUpdate(IFramework framework)
	{
		if (!_shouldRunThisCycle)
			return;

		try
		{
			MiscUpdater.UpdateMisc();

			if ((Service.Config.TargetFreely || DataCenter.TargetFreelyOverride) && !DataCenter.IsPvP && DataCenter.State && DataCenter.InCombat)
			{
				IAction? nextAction2 = ActionUpdater.NextAction;
				if (nextAction2 == null)
				{
					if (Player.Object != null && Svc.Targets.Target == null)
					{
						// Try to find the closest enemy and target it
						IBattleChara? closestEnemy = null;
						float minDistance = float.MaxValue;

						foreach (var enemy in DataCenter.AllHostileTargets)
						{
							if (enemy == null || !enemy.IsEnemy() || enemy == Player.Object)
								continue;

							float distance = Vector3.Distance(Player.Object.Position, enemy.Position);
							if (distance < minDistance)
							{
								minDistance = distance;
								closestEnemy = enemy;
							}
						}

						if (closestEnemy != null)
						{
							if (!Service.Config.TargetDelayEnable)
							{
								Svc.Targets.Target = closestEnemy;
							}
							else
							{
								// Respect TargetDelay before auto-targeting the closest enemy
								RSCommands.SetTargetWithDelay(closestEnemy);
							}
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			LogOnce("Secondary RSRUpdate Exception", ex);
		}
	}

	private static void RSRResetUpdate(IFramework framework)
	{
		if (!_shouldRunThisCycle)
			return;

		_shouldRunThisCycle = false;
	}

	private static HotbarID? GetGeneralActionHotbarID(IBaseAction baseAction)
	{
		// Build the lookup once and cache it to avoid a full sheet scan every frame
		if (_generalActionLookup == null)
		{
			var sheet = Svc.Data.GetExcelSheet<GeneralAction>();
			if (sheet == null)
				return null;

			_generalActionLookup = [];
			foreach (GeneralAction gAct in sheet)
			{
				var actionRowId = gAct.Action.RowId;
				if (actionRowId != 0)
					_generalActionLookup.TryAdd(actionRowId, gAct.RowId);
			}
		}

		return _generalActionLookup.TryGetValue(baseAction.ID, out uint generalActionRowId)
			? new HotbarID(HotbarSlotType.GeneralAction, generalActionRowId)
			: null;
	}

	private static void LogOnce(string context, Exception ex)
	{
		if (_threadException == ex)
		{
			return;
		}

		_threadException = ex;
		PluginLog.Error($"{context}: {ex.Message}");
		if (Service.Config.InDebug)
		{
			_ = BasicWarningHelper.AddSystemWarning(context);
		}
	}

	public static void Dispose()
	{
		Svc.Framework.Update -= RSRGateUpdate;
		Svc.Framework.Update -= RSRTeachingClearUpdate;
		Svc.Framework.Update -= RSRInvalidUpdate;
		Svc.Framework.Update -= RSRActivatedCoreUpdate;
		Svc.Framework.Update -= RSRActivatedHighlightUpdate;
		Svc.Framework.Update -= RSRCommonUpdate;
		Svc.Framework.Update -= RSRCleanupUpdate;
		Svc.Framework.Update -= RSRRotationAndStateUpdate;
		Svc.Framework.Update -= RSRMiscAndTargetFreelyUpdate;
		Svc.Framework.Update -= RSRResetUpdate;

		MiscUpdater.Dispose();
		ActionUpdater.ClearNextAction();
	}
}
