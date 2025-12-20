using Dalamud.Hooking;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using RotationSolver.Commands;

namespace RotationSolver.Updaters
{
    public static class ActionQueueManager
    {
        // Action Manager Hook for intercepting user input
        private static Hook<UseActionDelegate>? _useActionHook;

        // Delegates for ActionManager functions
        private unsafe delegate bool UseActionDelegate(ActionManager* actionManager, uint actionType, uint actionID, ulong targetObjectID, uint param, uint useType, int pvp, bool* isGroundTarget);

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

                PluginLog.Debug("[ActionQueueManager] Action interception hooks disposed");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"[ActionQueueManager] Failed to dispose action hooks: {ex}");
            }
        }

        private static unsafe bool UseActionDetour(ActionManager* actionManager, uint actionType, uint actionID, ulong targetObjectID, uint param, uint useType, int pvp, bool* isGroundTarget)
        {
            if (Player.Available && Service.Config.InterceptAction2 && DataCenter.State && DataCenter.InCombat && !DataCenter.IsPvP)
            {
                try
                {
                    if (actionType == 1 && (useType != 2 || Service.Config.InterceptMacro) && !StatusHelper.PlayerHasStatus(false, StatusHelper.RotationLockoutStatus)) // ActionType.Action == 1
                    {
                        // Always compute adjusted ID first to keep logic consistent
                        uint adjustedActionId = Service.GetAdjustedActionId(actionID);

                        if (adjustedActionId == 7419 && _useActionHook?.Original != null)
                        {
                            return _useActionHook.Original(actionManager, actionType, actionID, targetObjectID, param, useType, pvp, isGroundTarget);
                        }

                        if (ShouldInterceptAction(adjustedActionId))
                        {
                            // More efficient action lookup - avoid creating new collections
                            var rotationActions = RotationUpdater.CurrentRotationActions ?? [];
                            var dutyActions = DataCenter.CurrentDutyRotation?.AllActions ?? [];

                            PluginLog.Debug($"[ActionQueueManager] Detected player input: ID={actionID}, AdjustedID={adjustedActionId}");

                            var matchingAction = ((ActionID)adjustedActionId).GetActionFromID(false, rotationActions, dutyActions);

                            if (matchingAction != null && !BlackListedInterceptActionsContains((ActionID)matchingAction.ID))
                            {
                                PluginLog.Debug($"[ActionQueueManager] Matching action decided: {matchingAction.Name} (ID: {matchingAction.ID}, AdjustedID: {matchingAction.AdjustedID})");

                                if (matchingAction.IsIntercepted && ((ActionUpdater.NextAction != null && matchingAction != ActionUpdater.NextAction) || ActionUpdater.NextAction == null))
                                {
                                    if (!matchingAction.EnoughLevel)
                                    {
                                        PluginLog.Debug($"[ActionQueueManager] Not intercepting: insufficient level for {matchingAction.Name}.");
                                    }
                                    else if (!CanInterceptAction(matchingAction))
                                    {
                                        PluginLog.Debug($"[ActionQueueManager] Not intercepting: cooldown/window check failed for {matchingAction.Name}.");
                                    }
                                    else
                                    {
                                        HandleInterceptedAction(matchingAction, actionID);
                                        return false; // Block the original action
                                    }
                                }
                                else
                                {
                                    PluginLog.Debug($"[ActionQueueManager] Not intercepting: {matchingAction.Name} is not marked for interception.");
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

            if (!Service.Config.InterceptSpell2 && type == ActionCate.Spell)
            {
                return false;
            }

            if (!Service.Config.InterceptWeaponskill2 && type == ActionCate.Weaponskill)
            {
                return false;
            }

            if (!Service.Config.InterceptAbility2 && type == ActionCate.Ability)
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

                // Use DataCenter.AddCommandAction directly instead of going through RSCommands.DoActionCommand
                // This avoids the string parsing overhead and potential format issues
                RSCommands.DoSpecialCommandType(SpecialCommandType.Intercepting);
                DataCenter.AddCommandAction(matchingAction, Service.Config.InterceptActionTime);

                PluginLog.Debug($"[ActionQueueManager] Intercepted and queued action: {matchingAction.Name} (OriginalID: {actionID}, AdjustedID: {matchingAction.AdjustedID})");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"[ActionQueueManager] Error handling intercepted action {actionID}: {ex}");
            }
        }
    }
}