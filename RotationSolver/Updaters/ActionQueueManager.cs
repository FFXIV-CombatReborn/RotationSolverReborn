using Dalamud.Hooking;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using RotationSolver.Basic.Configuration;
using RotationSolver.Commands;

namespace RotationSolver.Updaters
{
	public static class ActionQueueManager
	{
		// Action Manager Hook for intercepting user input
		private static Hook<UseActionDelegate>? _useActionHook;

		// Hook for AutoAttackState.SetImpl to prevent auto attacks
		private static Hook<SetAutoAttackStateDelegate>? _setAutoAttackStateHook;

		// Delegates for ActionManager functions
		private unsafe delegate bool UseActionDelegate(ActionManager* actionManager, uint actionType, uint actionID, ulong targetObjectID, uint param, uint useType, int pvp, bool* isGroundTarget);
		private unsafe delegate bool SetAutoAttackStateDelegate(AutoAttackState* self, bool value, bool sendPacket, bool isInstant);

		public static void Enable()
		{
			// Initialize hooks
			InitializeActionHooks();
		}

		public static void Disable()
		{
			// Dispose hooks
			DisposeActionHooks();
		}

		public static ActionID[] BlackListedInterceptActions { get; } =
		[
            // Ninja mudra actions
            ActionID.TenPvE,
			ActionID.TenPvE_18805,
			ActionID.ChiPvE,
			ActionID.ChiPvE_18806,
			ActionID.JinPvE,
			ActionID.JinPvE_18807,

            // Dancer dance steps
            ActionID.StandardStepPvE,
			ActionID.TechnicalStepPvE,
			ActionID.EmboitePvE,
			ActionID.EntrechatPvE,
			ActionID.JetePvE,
			ActionID.PirouettePvE,
			ActionID.StandardFinishPvE,
			ActionID.TechnicalFinishPvE,

            // Sage Eukrasian actions
            ActionID.EukrasiaPvE,
			ActionID.EukrasianDosisPvE,
			ActionID.EukrasianDosisIiPvE,
			ActionID.EukrasianDosisIiiPvE,
			ActionID.EukrasianDyskrasiaPvE,
			ActionID.EukrasianPrognosisPvE,
			ActionID.EukrasianPrognosisIiPvE,
		];

		private static bool BlackListedInterceptActionsContains(ActionID id)
		{
			var arr = BlackListedInterceptActions;
			for (int i = 0; i < arr.Length; i++)
			{
				if (arr[i] == id) return true;
			}
			return false;
		}

		private static unsafe void InitializeActionHooks()
		{
			try
			{
				var useActionAddress = ActionManager.Addresses.UseAction.Value;
				_useActionHook = Svc.Hook.HookFromAddress<UseActionDelegate>(useActionAddress, UseActionDetour);
				_useActionHook?.Enable();

				var setAutoAttackStateAddress = AutoAttackState.Addresses.SetImpl.Value;
				_setAutoAttackStateHook = Svc.Hook.HookFromAddress<SetAutoAttackStateDelegate>(setAutoAttackStateAddress, SetAutoAttackStateDetour);
				_setAutoAttackStateHook?.Enable();

				PluginLog.Debug("[ActionQueueManager] Action interception hooks initialized");
			}
			catch (Exception ex)
			{
				PluginLog.Error($"[ActionQueueManager] Failed to initialize action hooks: {ex}");
			}
		}

		private static void DisposeActionHooks()
		{
			try
			{
				_useActionHook?.Disable();
				_useActionHook?.Dispose();
				_useActionHook = null;

				_setAutoAttackStateHook?.Disable();
				_setAutoAttackStateHook?.Dispose();
				_setAutoAttackStateHook = null;

				PluginLog.Debug("[ActionQueueManager] Action interception hooks disposed");
			}
			catch (Exception ex)
			{
				PluginLog.Error($"[ActionQueueManager] Failed to dispose action hooks: {ex}");
			}
		}

		/// <summary>
		/// Called every frame. If auto attacks are currently active but a NoCastingStatus is
		/// present, sends the toggle-auto-attack general action to disable them.
		/// </summary>
		public static unsafe void Update()
		{
			if (!Player.Available) return;

			try
			{
				var uiState = UIState.Instance();
				if (uiState == null) return;

				if (uiState->WeaponState.AutoAttackState.IsAutoAttacking && PlayerHasNoCastingStatus())
				{
					// GeneralAction 1 is the auto-attack toggle — same method the game uses
					ActionManager.Instance()->UseAction(ActionType.GeneralAction, 1);
					PluginLog.Information("[ActionQueueManager] Disabled active auto attacks due to NoCastingStatus.");
				}
			}
			catch (Exception ex)
			{
				PluginLog.Warning($"[ActionQueueManager] Error in Update (auto attack disable): {ex.Message}");
			}
		}

		private static bool PlayerHasNoCastingStatus()
		{
			try
			{
				HashSet<uint> noCastingStatus = OtherConfiguration.NoCastingStatus;
				if (noCastingStatus == null || noCastingStatus.Count == 0) return false;
				if (Player.Object?.StatusList == null) return false;

				// Check for Motion Tracker seperately, idk its weird
				if (StatusHelper.PlayerHasStatus(false, StatusID.MotionTracker))
				{
					return true;
				}

				foreach (var status in Player.Object.StatusList)
				{
					if (noCastingStatus.Contains(status.StatusId))
						return true;
				}
			}
			catch (Exception ex)
			{
				PluginLog.Warning($"[ActionQueueManager] Error checking NoCastingStatus: {ex.Message}");
			}
			return false;
		}

		private static unsafe bool SetAutoAttackStateDetour(AutoAttackState* self, bool value, bool sendPacket, bool isInstant)
		{
			// Block attempts to enable auto attacks while a NoCastingStatus is active
			if (value && Player.Available && PlayerHasNoCastingStatus())
			{
				PluginLog.Debug("[ActionQueueManager] Prevented auto attack activation due to NoCastingStatus.");
				return true;
			}

			return _setAutoAttackStateHook!.Original(self, value, sendPacket, isInstant);
		}

		private static unsafe bool UseActionDetour(ActionManager* actionManager, uint actionType, uint actionID, ulong targetObjectID, uint param, uint useType, int pvp, bool* isGroundTarget)
		{
			if (Player.Available && Service.Config.InterceptAction3 && DataCenter.State && DataCenter.InCombat && !DataCenter.IsPvP)
			{
				try
				{
					if (actionType == 1 && (useType != 2 || Service.Config.InterceptMacro) && !StatusHelper.PlayerHasStatus(false, StatusHelper.RotationLockoutStatus)) // ActionType.Action == 1
					{
						// Always compute adjusted ID first to keep logic consistent
						uint adjustedActionId = Service.GetAdjustedActionId(actionID);

						if (_useActionHook?.Original != null && RSCommands.CurrentAction != null)
						{
							if (adjustedActionId == RSCommands.CurrentAction.AdjustedID)
							{
								return _useActionHook.Original(actionManager, actionType, actionID, targetObjectID, param, useType, pvp, isGroundTarget);
							}
						}

						if (adjustedActionId == 7419 && _useActionHook?.Original != null)
						{
							return _useActionHook.Original(actionManager, actionType, actionID, targetObjectID, param, useType, pvp, isGroundTarget);
						}

						if (ShouldInterceptAction(adjustedActionId))
						{
							// More efficient action lookup - avoid creating new collections
							var rotationActions = RotationUpdater.CurrentRotationActions ?? [];
							var dutyActions = DataCenter.CurrentDutyRotation?.AllActions ?? [];

							//PluginLog.Debug($"[ActionQueueManager] Detected player input: ID={actionID}, AdjustedID={adjustedActionId}");

							var matchingAction = ((ActionID)adjustedActionId).GetActionFromID(false, rotationActions, dutyActions);

							if (matchingAction != null && !BlackListedInterceptActionsContains((ActionID)matchingAction.ID))
							{
								//PluginLog.Debug($"[ActionQueueManager] Matching action decided: {matchingAction.Name} (ID: {matchingAction.ID}, AdjustedID: {matchingAction.AdjustedID})");

								if (_useActionHook?.Original != null && matchingAction.IsIntercepted && ((ActionUpdater.NextAction != null && matchingAction != ActionUpdater.NextAction) || ActionUpdater.NextAction == null))
								{
									if (!matchingAction.EnoughLevel)
									{
										//PluginLog.Debug($"[ActionQueueManager] Not intercepting: insufficient level for {matchingAction.Name}.");
										if (Service.Config.InterceptPassing)
										{
											return _useActionHook.Original(actionManager, actionType, actionID, targetObjectID, param, useType, pvp, isGroundTarget);
										}
									}
									else if (!CanInterceptAction(matchingAction))
									{
										//PluginLog.Debug($"[ActionQueueManager] Not intercepting: cooldown/window check failed for {matchingAction.Name}.");
										if (Service.Config.InterceptPassing)
										{
											return _useActionHook.Original(actionManager, actionType, actionID, targetObjectID, param, useType, pvp, isGroundTarget);
										}
									}
									else
									{
										HandleInterceptedAction(matchingAction, actionID);
										if (Service.Config.InterceptPassing)
										{
											return _useActionHook.Original(actionManager, actionType, actionID, targetObjectID, param, useType, pvp, isGroundTarget);
										}
										return false;
									}
								}
								else
								{
									//PluginLog.Debug($"[ActionQueueManager] Not intercepting: {matchingAction.Name} is not marked for interception.");
									if (_useActionHook?.Original != null)
									{
										return _useActionHook.Original(actionManager, actionType, actionID, targetObjectID, param, useType, pvp, isGroundTarget);
									}
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					PluginLog.Error($"[ActionQueueManager] Error in UseActionDetour: {ex}");
				}
			}

			// Call original function if available, otherwise return true (allow action)
			if (_useActionHook?.Original != null)
			{
				return _useActionHook.Original(actionManager, actionType, actionID, targetObjectID, param, useType, pvp, isGroundTarget);
			}

			// Return true to allow the action to proceed if hook is unavailable
			return true;
		}

		private static bool ShouldInterceptAction(uint actionId)
		{
			// Note: actionId is expected to be the adjusted ID
			if (ActionUpdater.NextAction != null && actionId == ActionUpdater.NextAction.AdjustedID)
				return false;

			var actionSheet = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>();
			if (actionSheet == null) return false;

			var action = actionSheet.GetRow(actionId);
			var type = ActionHelper.GetActionCate(action);

			if (type == ActionCate.None)
			{
				return false;
			}

			if (type == ActionCate.Autoattack)
			{
				return false;
			}

			if (!Service.Config.InterceptSpell3 && type == ActionCate.Spell)
			{
				return false;
			}

			if (!Service.Config.InterceptWeaponskill3 && type == ActionCate.Weaponskill)
			{
				return false;
			}

			if (!Service.Config.InterceptAbility3 && type == ActionCate.Ability)
			{
				return false;
			}

			return true;
		}

		private static bool CanInterceptAction(IAction action)
		{
			if (Service.Config.InterceptCooldown || action.Cooldown.CurrentCharges > 0) return true;

			// Guard against invalid GCD totals to avoid division by zero
			var gcdTotal = DataCenter.DefaultGCDTotal;
			if (gcdTotal <= 0)
				return false;

			// We check if the skill will fit inside the intercept action time window
			var gcdCount = (byte)Math.Floor(Service.Config.InterceptActionTime / gcdTotal);
			if (gcdCount < 1) gcdCount = 1;

			return action is IBaseAction baseAction && baseAction.Cooldown.CooldownCheck(false, gcdCount);
		}

		private static void HandleInterceptedAction(IAction matchingAction, uint actionID)
		{
			try
			{
				// Abandoned idea
				//if (matchingAction is IBaseAction baseAction && baseAction.Setting.SpecialType == SpecialActionType.HostileMovingForward)
				//{
				//    RSCommands.DoSpecialCommandType(SpecialCommandType.Intercepting);
				//    DataCenter.AddCommandAction(matchingAction, Service.Config.InterceptActionTime);
				//    return; // Do not queue the original action; open the special window instead
				//}

				// Track intercepted actions so UI can display current & previous intercepted actions
				try
				{
					// Move current to previous before updating current
					DataCenter.CurrentInterceptedAction = matchingAction;
				}
				catch (Exception ex)
				{
					PluginLog.Warning($"[ActionQueueManager] Failed to set intercepted action tracking: {ex}");
				}

				RSCommands.DoSpecialCommandType(SpecialCommandType.Intercepting);
				DataCenter.AddCommandAction(matchingAction, Service.Config.InterceptActionTime);

				// Let the AddCommandAction system handle executing the queued action (do not attempt immediate execution here).
				PluginLog.Debug($"[ActionQueueManager] Queued intercepted action: {matchingAction.Name} (OriginalID: {actionID}, AdjustedID: {matchingAction.AdjustedID})");
			}
			catch (Exception ex)
			{
				PluginLog.Error($"[ActionQueueManager] Error handling intercepted action {actionID}: {ex}");
			}
		}
	}
}