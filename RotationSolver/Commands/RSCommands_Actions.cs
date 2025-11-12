using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using RotationSolver.Basic.Configuration;
using RotationSolver.Helpers;
using RotationSolver.Updaters;

namespace RotationSolver.Commands
{
    public static partial class RSCommands
    {
        private static DateTime _lastClickTime = DateTime.MinValue;
        private static bool _lastState;
        private static bool started = false;
        internal static DateTime _lastUsedTime = DateTime.MinValue;
        internal static uint _lastActionID;
        private static float _lastCountdownTime = 0;
        private static Job _previousJob = Job.ADV;
        private static readonly Random random = Random.Shared;

        public static void IncrementState()
        {
            if (!DataCenter.State) { DoStateCommandType(StateCommandType.Auto); return; }
            if (DataCenter.State && !DataCenter.IsManual && DataCenter.TargetingType == TargetingType.Big) { DoStateCommandType(StateCommandType.Auto); return; }
            if (DataCenter.State && !DataCenter.IsManual) { DoStateCommandType(StateCommandType.Manual); return; }
            if (DataCenter.State && DataCenter.IsManual) { DoStateCommandType(StateCommandType.Off); return; }
        }

        internal static unsafe bool CanDoAnAction(bool isGCD)
        {
            // Cache frequently accessed properties to avoid redundant calls
            bool currentState = DataCenter.State;

            if (!_lastState || !currentState)
            {
                _lastState = currentState;
                return false;
            }
            _lastState = currentState;

            // Precompute the delay range to avoid recalculating it multiple times
            TimeSpan delayRange = TimeSpan.FromMilliseconds(random.Next(
                (int)(Service.Config.ClickingDelay.X * 1000),
                (int)(Service.Config.ClickingDelay.Y * 1000)));

            if (DateTime.Now - _lastClickTime < delayRange)
            {
                return false;
            }

            _lastClickTime = DateTime.Now;

            // Avoid unnecessary checks if isGCD is true
            return isGCD || ActionUpdater.NextAction is not IBaseAction nextAction || !nextAction.Info.IsRealGCD;
        }

        private static StatusID[]? _cachedNoCastingStatusArray = null;
        private static HashSet<uint>? _cachedNoCastingStatusSet = null;

        public static void DoAction()
        {
            if (Player.Object.StatusList == null)
            {
                return;
            }

            HashSet<uint> noCastingStatus = OtherConfiguration.NoCastingStatus;
            if (noCastingStatus != null)
            {
                if (_cachedNoCastingStatusSet != noCastingStatus)
                {
                    _cachedNoCastingStatusArray = new StatusID[noCastingStatus.Count];
                    int index = 0;
                    foreach (uint status in noCastingStatus)
                    {
                        _cachedNoCastingStatusArray[index++] = (StatusID)status;
                    }
                    _cachedNoCastingStatusSet = noCastingStatus;
                }
            }
            else
            {
                _cachedNoCastingStatusArray = [];
                _cachedNoCastingStatusSet = null;
            }
            StatusID[] noCastingStatusArray = _cachedNoCastingStatusArray!;

            float minStatusTime = float.MaxValue;
            int statusTimesCount = 0;
            foreach (float t in Player.Object.StatusTimes(false, noCastingStatusArray))
            {
                statusTimesCount++;
                if (t < minStatusTime)
                {
                    minStatusTime = t;
                }
            }

            if (statusTimesCount > 0)
            {
                float remainingCastTime = Player.Object.TotalCastTime - Player.Object.CurrentCastTime;
                if (minStatusTime > remainingCastTime && minStatusTime < 5)
                {
                    return;
                }
            }

            if (Player.Object.HasStatus(false, StatusID.Transcendent))
            {
                return;
            }

            IAction? nextAction = ActionUpdater.NextAction;
            if (nextAction == null)
            {
                return;
            }

#if DEBUG
            // if (nextAction is BaseAction debugAct)
            //     PluginLog.Debug($"Will Do {debugAct}");
#endif

            if (nextAction is BaseAction baseAct)
            {
                if (baseAct.Target.Target is IBattleChara target && target != Player.Object && target.IsEnemy())
                {
                    DataCenter.HostileTarget = target;
                    if (!DataCenter.IsManual &&
                        (Service.Config.SwitchTargetFriendly || ((Svc.Targets.Target?.IsEnemy() ?? true)
                        || Svc.Targets.Target.GetObjectKind() == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Treasure)))
                    {
                        Svc.Targets.Target = target;
                    }
                }
            }

            if (Service.Config.KeyBoardNoise)
            {
                MiscUpdater.PulseActionBar(nextAction.AdjustedID);
            }

            if (nextAction.Use())
            {
                if (Service.Config.EnableClickingCount)
                {
                    OtherConfiguration.RotationSolverRecord.ClickingCount++;
                }

                _lastActionID = nextAction.AdjustedID;
                _lastUsedTime = DateTime.Now;

                if (nextAction is BaseAction finalAct)
                {
                    if (Service.Config.KeyBoardNoise)
                    {
                        PulseSimulation(nextAction.AdjustedID);
                    }

                    if (finalAct.Setting.EndSpecial)
                    {
                        ResetSpecial();
                    }
                }
            }
            else if (Service.Config.InDebug)
            {
                PluginLog.Verbose($"Failed to use the action {nextAction} ({nextAction.AdjustedID})");
            }
        }

        private static void PulseSimulation(uint id)
        {
            if (started)
            {
                return;
            }

            started = true;
            try
            {
                int pulseCount = random.Next(Service.Config.KeyboardNoise.X, Service.Config.KeyboardNoise.Y);
                PulseAction(id, pulseCount);
            }
            catch (Exception ex)
            {
                PluginLog.Warning($"Pulse Failed!: {ex.Message}");
                BasicWarningHelper.AddSystemWarning($"Action bar failed to pulse because: {ex.Message}");
            }
            finally
            {
                started = false;
            }
        }

        private static void PulseAction(uint id, int remainingPulses)
        {
            if (remainingPulses <= 0)
            {
                started = false;
                return;
            }

            MiscUpdater.PulseActionBar(id);
            double time = Service.Config.ClickingDelay.X + (random.NextDouble() * (Service.Config.ClickingDelay.Y - Service.Config.ClickingDelay.X));
            _ = Svc.Framework.RunOnTick(() =>
            {
                PulseAction(id, remainingPulses - 1);
            }, TimeSpan.FromSeconds(time));
        }

        internal static void ResetSpecial()
        {
            DoSpecialCommandType(SpecialCommandType.EndSpecial, false);
        }

internal static void CancelState()
        {
            DataCenter.ResetAllRecords();
            if (DataCenter.State)
            {
                DoStateCommandType(StateCommandType.Off);
            }
        }

        public static void UpdateTargetFromNextAction()
        {
            IAction? nextAction = ActionUpdater.NextAction;
            if (nextAction is BaseAction baseAct)
            {
                if (baseAct.Target.Target is IBattleChara target && target != Player.Object && target.IsEnemy())
                {
                    DataCenter.HostileTarget = target;
                    if (!DataCenter.IsManual &&
                        (Service.Config.SwitchTargetFriendly || ((Svc.Targets.Target?.IsEnemy() ?? true)
                        || Svc.Targets.Target.GetObjectKind() == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Treasure)))
                    {
                        Svc.Targets.Target = target;
                    }
                }
            }
        }

        internal static void UpdateRotationState()
        {
            try
            {
                // Avoid redundant checks for AutoCancelTime
                if (ActionUpdater.AutoCancelTime != DateTime.MinValue &&
                    (!DataCenter.State || DataCenter.InCombat))
                {
                    ActionUpdater.AutoCancelTime = DateTime.MinValue;
                }

                if (!Player.AvailableThreadSafe)
                {
                    return;
                }

                // Precompute hostile target object IDs for O(1) lookup
                var hostileTargetObjectIds = new HashSet<ulong>();
                foreach (var ht in DataCenter.AllHostileTargets)
                {
                    if (ht != null) hostileTargetObjectIds.Add(ht.TargetObjectId);
                }

                // Combine conditions to reduce redundant checks
                if (Svc.Condition[ConditionFlag.LoggingOut] ||
                    (Service.Config.AutoOffWhenDead && DataCenter.Territory != null && !DataCenter.Territory.IsPvP && Player.Object.CurrentHp == 0) ||
                    (Service.Config.AutoOffWhenDeadPvP && DataCenter.Territory != null && DataCenter.Territory.IsPvP && Player.Object.CurrentHp == 0) ||
                    (Service.Config.AutoOffPvPMatchEnd && Svc.Condition[ConditionFlag.PvPDisplayActive]) ||
                    (Service.Config.AutoOffCutScene && !DataCenter.IsAutoDuty && Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent]) ||
                    (Service.Config.AutoOffSwitchClass && Player.Job != _previousJob) ||
                    (Service.Config.AutoOffBetweenArea && !DataCenter.IsAutoDuty && (Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51])) ||
                    (Service.Config.CancelStateOnCombatBeforeCountdown && Service.CountDownTime > 0.2f && DataCenter.InCombat) ||
                    (ActionUpdater.AutoCancelTime != DateTime.MinValue && DateTime.Now > ActionUpdater.AutoCancelTime) ||
false)
                {
                    CancelState();
                    if (Player.Job != _previousJob)
                    {
                        _previousJob = Player.Job;
                    }

                    ActionUpdater.AutoCancelTime = DateTime.MinValue;
                    return;
                }

                // Simplify PvP match start condition
                if (Service.Config.AutoOnPvPMatchStart &&
                    Svc.Condition[ConditionFlag.BetweenAreas] &&
                    Svc.Condition[ConditionFlag.BoundByDuty] &&
                    !DataCenter.State &&
                    (DataCenter.Territory?.IsPvP ?? false))
                {
                    DoStateCommandType(StateCommandType.Auto);
                    return;
                }

                //PluginLog.Debug($"AllTargetsCount = {DataCenter.AllTargets.Count} && AllHostileTargets: {DataCenter.AllHostileTargets.Count} && PartyCount: {DataCenter.PartyMembers.Count} && DataCenter.State = {DataCenter.State} && StartOnPartyIsInCombat = {Service.Config.StartOnPartyIsInCombat} && StartOnAllianceIsInCombat = {Service.Config.StartOnAllianceIsInCombat} && StartOnFieldOpInCombat = {Service.Config.StartOnFieldOpInCombat}");
                
                if (Service.Config.StartOnPartyIsInCombat && !DataCenter.State && DataCenter.PartyMembers.Count > 1)
                {
                    foreach (var p in DataCenter.PartyMembers)
                    {
                        
                        if (p != null && p.InCombat())
                        {
                            PluginLog.Debug($"StartOnPartyIsInCombat: {p.Name} InCombat: {p.InCombat()}.");
                            DoStateCommandType(StateCommandType.Auto);
                            return;
                        }

                        if (p != null && hostileTargetObjectIds.Contains(p.GameObjectId))
                        {
                            PluginLog.Debug($"StartOnPartyIsInCombat: {p.Name} Is Targeted By Hostile.");
                            DoStateCommandType(StateCommandType.Auto);
                            return;
                        }
                    }
                    
                }

                if ((Service.Config.StartOnAllianceIsInCombat && !DataCenter.State && DataCenter.AllianceMembers.Count > 1)  && !(DataCenter.IsInBozjanFieldOp || DataCenter.IsInBozjanFieldOpCE || DataCenter.IsInOccultCrescentOp))
                {
                    foreach (var a in DataCenter.AllianceMembers)
                    {
                        
                        if (a != null && a.InCombat())
                        {
                            PluginLog.Debug($"StartOnAllianceIsInCombat: {a.Name} InCombat: {a.InCombat()}.");
                            DoStateCommandType(StateCommandType.Auto);
                            return;
                        }

                        if (a != null && hostileTargetObjectIds.Contains(a.GameObjectId))
                        {
                            PluginLog.Debug($"StartOnAllianceIsInCombat: {a.Name} Is Targeted By Hostile.");
                            DoStateCommandType(StateCommandType.Auto);
                            return;
                        }
                    }
                }

                if (Service.Config.StartOnFieldOpInCombat && !DataCenter.State && (DataCenter.IsInBozjanFieldOp || DataCenter.IsInBozjanFieldOpCE || DataCenter.IsInOccultCrescentOp))
                {
                    foreach (var t in TargetHelper.GetTargetsByRange(30f))
                    {
                        if (t != null && DataCenter.AllHostileTargets.Contains(t) && !ObjectHelper.IsDummy(t))
                        {
                            continue;
                        }
                        if (t != null && t.GameObjectId != Player.Object.GameObjectId)
                        {
                           // PluginLog.Debug($"StartOnFieldOpInCombat: {t.Name} InCombat: {t.InCombat()} Distance: {t.DistanceToPlayer()} ");    
                        }
                        
                        if (t != null && t.InCombat())
                        {
                            PluginLog.Debug($"StartOnFieldOpInCombat: {t.Name} InCombat: {t.InCombat()}.");
                            DoStateCommandType(StateCommandType.Auto);
                            return;
                        }
                        if (t != null && hostileTargetObjectIds.Contains(t.GameObjectId))
                        {
                            PluginLog.Debug($"StartOnFieldOpInCombat: {t.Name} Is Targeted By Hostile.");
                            DoStateCommandType(StateCommandType.Auto);
                            return;
                        }
                    }
                }
                IBattleChara? target = null;
                if (Service.Config.StartOnAttackedBySomeone && !DataCenter.State)
                {
                    foreach (var t in DataCenter.AllHostileTargets)
                    {
                        if (t != null && t is IBattleChara battleChara && battleChara.TargetObjectId == Player.Object.GameObjectId)
                        {
                            target = battleChara;
                            break;
                        }
                    }
                    if (target != null && !ObjectHelper.IsDummy(target))
                    {
                        DoStateCommandType(StateCommandType.Manual);
                    }
                }

                if (Service.Config.StartOnCountdown)
                {
                    if (Service.CountDownTime > 0)
                    {
                        _lastCountdownTime = Service.CountDownTime;
                        if (!DataCenter.State)
                        {
                            DoStateCommandType(Service.Config.CountdownStartsManualMode
                                ? StateCommandType.Manual
                                : StateCommandType.Auto);
                        }
                        return;
                    }
                    else if (Service.CountDownTime == 0 && _lastCountdownTime > 0.2f)
                    {
                        _lastCountdownTime = 0;
                        CancelState();
                        return;
                    }
                }

                // Combine manual and auto condition checks
                if (!DataCenter.State)
                {
                    if (DataCenter.CurrentConditionValue.SwitchManualConditionSet?.IsTrue(DataCenter.CurrentRotation) ?? false)
                    {
                        DoStateCommandType(StateCommandType.Manual);
                    }
                    else if (DataCenter.CurrentConditionValue.SwitchAutoConditionSet?.IsTrue(DataCenter.CurrentRotation) ?? false)
                    {
                        DoStateCommandType(StateCommandType.Auto);
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Exception in UpdateRotationState: {ex.Message}");
            }
        }

    }
}
