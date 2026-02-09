using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using RotationSolver.Basic.Configuration;
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

		internal static IBaseAction? CurrentAction { get; set; } = null;

		public static void IncrementState()
		{
			if (!DataCenter.State) { DoStateCommandType(StateCommandType.Auto); return; }
			if (DataCenter.State && !DataCenter.IsManual && DataCenter.TargetingType == TargetingType.Big) { DoStateCommandType(StateCommandType.Auto); return; }
			if (DataCenter.State && !DataCenter.IsManual) { DoStateCommandType(StateCommandType.Manual); return; }
			if (DataCenter.State && DataCenter.IsManual) { DoStateCommandType(StateCommandType.Off); return; }
		}

		internal static unsafe bool CanDoAnAction(bool isGCD)
		{
			bool currentState = DataCenter.State;

			if (!_lastState || !currentState)
			{
				_lastState = currentState;
				return false;
			}
			_lastState = currentState;

			TimeSpan delayRange = TimeSpan.FromMilliseconds(random.Next(
				(int)(Service.Config.ClickingDelay.X * 1000),
				(int)(Service.Config.ClickingDelay.Y * 1000)));

			if (DateTime.Now - _lastClickTime < delayRange)
			{
				return false;
			}

			_lastClickTime = DateTime.Now;

			return isGCD || ActionUpdater.NextAction is not IBaseAction nextAction || !nextAction.Info.IsRealGCD;
		}

		private static StatusID[]? _cachedNoCastingStatusArray = null;
		private static HashSet<uint>? _cachedNoCastingStatusSet = null;

		public static void DoAction()
		{
			if (Player.Object != null && Player.Object.StatusList == null)
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
			if (Player.Object != null && !DataCenter.IsPvP)
			{
				foreach (float t in Player.Object.StatusTimes(false, noCastingStatusArray))
				{
					statusTimesCount++;
					if (t < minStatusTime)
					{
						minStatusTime = t;
					}
				}
			}

			if (statusTimesCount > 0 && Player.Object != null)
			{
				float remainingCastTime = Player.Object.TotalCastTime - Player.Object.CurrentCastTime;
				if (minStatusTime > remainingCastTime && minStatusTime < 5)
				{
					return;
				}
			}

			if (StatusHelper.PlayerHasStatus(false, StatusID.Transcendent))
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
				if (baseAct.Target.Target != null && baseAct.Target.Target is IBattleChara target && target != Player.Object && target.IsEnemy())
				{
					DataCenter.HostileTarget = target;
					if (!DataCenter.IsManual &&
						(Service.Config.SwitchTargetFriendly
						|| (Svc.Targets.Target?.IsEnemy() ?? true)
						|| (Svc.Targets.Target?.GetObjectKind() == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Treasure)))
					{
						Svc.Targets.Target = target;
					}
				}
			}

			CurrentAction = nextAction as IBaseAction;

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

				// If this action was the one intercepted by the user, clear intercepted state and end the intercept window early
				try
				{
					if (DataCenter.CurrentInterceptedAction != null && DataCenter.CurrentInterceptedAction.AdjustedID == nextAction.AdjustedID)
					{
						DataCenter.CurrentInterceptedAction = null;
						// End the special intercepting state without showing toast
						DoSpecialCommandType(SpecialCommandType.EndSpecial, false);
					}
				}
				catch (Exception ex)
				{
					PluginLog.Warning($"Failed to clear CurrentInterceptedAction after execution: {ex}");
				}

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

		internal static void SetTargetWithDelay(IGameObject? candidate)
		{
			if (candidate == null)
			{
				return;
			}

			float min = Service.Config.TargetDelay.X;
			float max = Service.Config.TargetDelay.Y;
			double delay = Math.Max(0, min + (random.NextDouble() * Math.Max(0, max - min)));
			if (delay <= 0)
			{
				Svc.Targets.Target = candidate;
				return;
			}

			ulong initialTargetId = Svc.Targets.Target?.GameObjectId ?? 0;
			ulong candidateId = candidate.GameObjectId;

			_ = Svc.Framework.RunOnTick(() =>
			{
				try
				{
					var current = Svc.Targets.Target;
					ulong currentId = current?.GameObjectId ?? 0;

					if (currentId == initialTargetId)
					{
						IGameObject? cand = null;
						foreach (var obj in Svc.Objects)
						{
							if (obj != null && obj.GameObjectId == candidateId)
							{
								cand = obj;
								break;
							}
						}

						if (cand != null && cand.IsTargetable)
						{
							Svc.Targets.Target = cand;
						}
					}
				}
				catch
				{
					// Intentionally swallow; candidate may have despawned
				}
			}, TimeSpan.FromSeconds(delay));
		}

		public static void UpdateTargetFromNextAction()
		{
			if (Player.Object == null)
			{
				return;
			}

			IAction? nextAction = ActionUpdater.NextAction;
			if (nextAction is BaseAction baseAct)
			{
				if (baseAct.Target.Target != null && baseAct.Target.Target is IBattleChara target && target != Player.Object && target.IsEnemy())
				{
					DataCenter.HostileTarget = target;
					if (!DataCenter.IsManual &&
						(Service.Config.SwitchTargetFriendly || ((Svc.Targets.Target?.IsEnemy() ?? true)
						|| Svc.Targets.Target?.GetObjectKind() == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Treasure)))
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
				if (Player.Object == null)
				{
					return;
				}

				if (ActionUpdater.AutoCancelTime != DateTime.MinValue &&
					(!DataCenter.State || DataCenter.InCombat))
				{
					ActionUpdater.AutoCancelTime = DateTime.MinValue;
				}

				var hostileTargetObjectIds = new HashSet<ulong>();
				for (int i = 0; i < DataCenter.AllHostileTargets.Count; i++)
				{
					var ht = DataCenter.AllHostileTargets[i];
					try
					{
						if (ht != null && ht.TargetObjectId != 0)
							hostileTargetObjectIds.Add(ht.TargetObjectId);
					}
					catch (AccessViolationException)
					{
						// Log or ignore; object is invalid
						continue;
					}
				}

				if (Svc.Condition[ConditionFlag.LoggingOut] ||
					(Service.Config.AutoOffWhenDead && DataCenter.Territory != null && !DataCenter.Territory.IsPvP && Player.Object != null && Player.Object.CurrentHp == 0) ||
					(Service.Config.AutoOffWhenDeadPvP && DataCenter.Territory != null && DataCenter.Territory.IsPvP && Player.Object != null && Player.Object.CurrentHp == 0) ||
					(Service.Config.AutoOffPvPMatchEnd && Svc.Condition[ConditionFlag.PvPDisplayActive]) ||
					(Service.Config.AutoOffCutScene && !DataCenter.IsAutoDuty && Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent]) ||
					(Service.Config.AutoOffSwitchClass && Player.Job != _previousJob) ||
					(Service.Config.AutoOffBetweenArea && !DataCenter.IsAutoDuty && (Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51])) ||
					(Service.Config.CancelStateOnCombatBeforeCountdown && Service.CountDownTime > 0.2f && DataCenter.InCombat) ||
					(ActionUpdater.AutoCancelTime != DateTime.MinValue && DateTime.Now > ActionUpdater.AutoCancelTime) || false)
				{
					CancelState();
					if (Player.Job != _previousJob)
					{
						_previousJob = Player.Job;
					}

					ActionUpdater.AutoCancelTime = DateTime.MinValue;
					return;
				}

				if (Service.Config.AutoOnPvPMatchStart &&
					Svc.Condition[ConditionFlag.BetweenAreas] &&
					Svc.Condition[ConditionFlag.BoundByDuty] &&
					!DataCenter.State &&
					(DataCenter.Territory?.IsPvP ?? false))
				{
					DoStateCommandType(StateCommandType.Auto);
					return;
				}

				if (Service.Config.StartOnPartyIsInCombat2 && !DataCenter.State && DataCenter.PartyMembers.Count > 1)
				{
					for (int i = 0; i < DataCenter.PartyMembers.Count; i++)
					{
						var p = DataCenter.PartyMembers[i];
						if (p != null && p.InCombat())
						{
							DoStateCommandType(StateCommandType.Auto);
							return;
						}

						if (p != null && hostileTargetObjectIds.Contains(p.GameObjectId))
						{
							DoStateCommandType(StateCommandType.Auto);
							return;
						}
					}
				}

				if ((Service.Config.StartOnAllianceIsInCombat2 && !DataCenter.State && DataCenter.AllianceMembers.Count > 1) && !(DataCenter.IsInBozjanFieldOp || DataCenter.IsInBozjanFieldOpCE || DataCenter.IsInOccultCrescentOp))
				{
					for (int i = 0; i < DataCenter.AllianceMembers.Count; i++)
					{
						var a = DataCenter.AllianceMembers[i];
						if (a != null && a.InCombat())
						{
							DoStateCommandType(StateCommandType.Auto);
							return;
						}

						if (a != null && hostileTargetObjectIds.Contains(a.GameObjectId))
						{
							DoStateCommandType(StateCommandType.Auto);
							return;
						}
					}
				}

				if (Service.Config.StartOnFieldOpInCombat2 && !DataCenter.State && (DataCenter.IsInBozjanFieldOp || DataCenter.IsInBozjanFieldOpCE || DataCenter.IsInOccultCrescentOp) && Player.Object != null)
				{
					var targets = TargetHelper.GetTargetsByRange(30f);
					for (int i = 0; i < targets.Count; i++)
					{
						var t = targets[i];
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
							DoStateCommandType(StateCommandType.Auto);
							return;
						}
						if (t != null && hostileTargetObjectIds.Contains(t.GameObjectId))
						{
							DoStateCommandType(StateCommandType.Auto);
							return;
						}
					}
				}
				IBattleChara? target = null;
				if (Service.Config.StartOnAttackedBySomeone2 && !DataCenter.State && Player.Object != null)
				{
					for (int i = 0; i < DataCenter.AllHostileTargets.Count; i++)
					{
						var t = DataCenter.AllHostileTargets[i];
						if (t != null && t is IBattleChara battleChara)
						{
							try
							{
								if (battleChara.TargetObjectId == Player.Object.GameObjectId)
								{
									target = battleChara;
									break;
								}
							}
							catch (AccessViolationException)
							{
								// Object became invalid while reading TargetObjectId.
								continue;
							}
						}
					}
					if (target != null && !ObjectHelper.IsDummy(target))
					{
						DoStateCommandType(StateCommandType.Manual);
					}
				}

				if (Service.Config.StartOnCountdown && !DataCenter.IsInDutyReplay())
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
			}
			catch (Exception ex)
			{
				PluginLog.Error($"Exception in UpdateRotationState: {ex.Message}");
			}
		}

	}
}