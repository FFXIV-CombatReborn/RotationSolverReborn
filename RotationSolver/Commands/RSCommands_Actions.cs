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

		internal static IBaseAction? CurrentAction { get; set; } = null;

		public static void IncrementState()
		{
			if (!DataCenter.State)
			{ DoStateCommandType(StateCommandType.Auto); return; }
			if (DataCenter.State && !DataCenter.IsManual && DataCenter.TargetingType == TargetingType.Big)
			{ DoStateCommandType(StateCommandType.Auto); return; }
			if (DataCenter.State && !DataCenter.IsManual)
			{ DoStateCommandType(StateCommandType.Manual); return; }
			if (DataCenter.State && DataCenter.IsManual)
			{ DoStateCommandType(StateCommandType.Off); return; }
		}

		internal static bool CanDoAnAction(bool isGCD)
		{
			var currentState = DataCenter.State;

			if (!_lastState || !currentState)
			{
				_lastState = currentState;
				return false;
			}
			_lastState = currentState;

			var minDelayMs = (int)(Service.Config.ClickingDelay.X * 1000);
			var maxDelayMs = (int)(Service.Config.ClickingDelay.Y * 1000);
			var delayRange = TimeSpan.FromMilliseconds(random.Next(
				Math.Min(minDelayMs, maxDelayMs),
				Math.Max(minDelayMs, maxDelayMs)));

			if (DateTime.Now - _lastClickTime < delayRange)
			{
				return false;
			}

			_lastClickTime = DateTime.Now;

			if (!isGCD && DataCenter.DefaultGCDRemain <= 0.5f && DataCenter.DefaultGCDRemain > 0f)
			{
				return false;
			}

			return isGCD || ActionUpdater.NextAction is not IBaseAction nextAction || !nextAction.Info.IsRealGCD;
		}

		public static void DoAction()
		{
			if (Player.Object != null && Player.Object.StatusList == null)
			{
				return;
			}

			var nextAction = ActionUpdater.NextAction;
			if (nextAction == null)
			{
				return;
			}

			if (DataCenter.AnimationLock > 0f)
			{
				return;
			}

			if (nextAction is BaseAction baseAct)
			{
				// If this is an ability and not a Ninjutsu-type action, and GCD remaining is between 0 and 0.5s, skip using it
				if (baseAct.Info.IsAbility && !baseAct.Setting.IsMudra && DataCenter.DefaultGCDRemain <= 0.5f && DataCenter.DefaultGCDRemain > 0f)
				{
					return;
				}
			}

			var player = Player.Object;
			if (player != null && !DataCenter.IsPvP
				&& NoCastingStatusHelper.PlayerHasNoCastingStatus(out var minStatusTime))
			{
				var remainingCastTime = player.TotalCastTime - player.CurrentCastTime;
				if (minStatusTime > remainingCastTime && minStatusTime < 3f)
				{
					return;
				}
			}

			if (DataCenter.BMRSpecialModeType == SpecialMode.Pyretic)
			{
				PluginLog.Verbose("Player has Pyretic special mode active, skipping action use to avoid potential issues.");
				return;
			}

			if (StatusHelper.PlayerHasStatus(false, StatusID.MotionTracker))
			{
				PluginLog.Verbose("Player has Motion Tracker status, skipping action use to avoid potential issues.");
				return;
			}

			if (StatusHelper.PlayerHasStatus(false, StatusID.Transcendent))
			{
				return;
			}

			if (StatusHelper.PlayerHasStatus(false, StatusID.WaningNocturne))
			{
				return;
			}

#if DEBUG
			// if (nextAction is BaseAction debugAct)
			//     PluginLog.Debug($"Will Do {debugAct}");
#endif

			SwitchTargetForAction(nextAction);

			CurrentAction = nextAction as IBaseAction;

			if (nextAction.Use())
			{

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
					if (Service.Config.KeyboardNoise)
					{
						PulseSimulation(nextAction.AdjustedID);
						if (Service.Config.EnableClickingCount)
						{
							OtherConfiguration.RotationSolverRecord.ClickingCount++;
						}
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
				var pulseCount = random.Next(Service.Config.KeyboardNoisePresses.X, Service.Config.KeyboardNoisePresses.Y);
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
			var time = Service.Config.ClickingDelay.X + (random.NextDouble() * (Service.Config.ClickingDelay.Y - Service.Config.ClickingDelay.X));
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

		internal static void SetTargetWithDelay(IBattleChara? candidate)
		{
			if (candidate == null)
			{
				return;
			}

			var min = Service.Config.TargetDelay.X;
			var max = Service.Config.TargetDelay.Y;
			var delay = Math.Max(0, min + (random.NextDouble() * Math.Max(0, max - min)));
			if (delay <= 0)
			{
				Svc.Targets.Target = candidate;
				return;
			}

			var initialTargetId = Svc.Targets.Target?.GameObjectId ?? 0;
			var candidateId = candidate.GameObjectId;

			_ = Svc.Framework.RunOnTick(() =>
			{
				try
				{
					var current = Svc.Targets.Target;
					var currentId = current?.GameObjectId ?? 0;

					if (currentId == initialTargetId)
					{
						IBattleChara? cand = null;
						foreach (var obj in Svc.Objects)
						{
							if (obj != null && obj.GameObjectId == candidateId)
							{
								cand = obj as IBattleChara;
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

			SwitchTargetForAction(ActionUpdater.NextAction);
		}

		private static void SwitchTargetForAction(IAction? action)
		{
			if (action is not BaseAction baseAct || baseAct.Target.Target is not IBattleChara target)
			{
				return;
			}

			if (target.GameObjectId == Player.Object?.GameObjectId || !(Service.Config.SwitchTargetFriendly2 || target.IsEnemy()))
			{
				return;
			}

			DataCenter.HostileTarget = target;
			if (!DataCenter.IsManual &&
				(Service.Config.SwitchTargetFriendly2
				|| (Svc.Targets.Target?.IsEnemy() ?? true)
				|| Svc.Targets.Target?.GetObjectKind() == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Treasure))
			{
				Svc.Targets.Target = target;
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

				var currentJob = Player.Job;
				var jobChanged = currentJob != _previousJob;
				_previousJob = currentJob;

				if (Svc.Condition[ConditionFlag.LoggingOut] ||
					(Service.Config.AutoOffWhenDead && DataCenter.Territory != null && !DataCenter.Territory.IsPvP && Player.Object.CurrentHp == 0) ||
					(Service.Config.AutoOffWhenDeadPvP && DataCenter.Territory != null && DataCenter.Territory.IsPvP && Player.Object.CurrentHp == 0) ||
					(Service.Config.AutoOffPvPMatchEnd && Svc.Condition[ConditionFlag.PvPDisplayActive]) ||
					(Service.Config.AutoOffCutScene && !DataCenter.IsAutoDuty && Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent]) ||
					(Service.Config.AutoOffSwitchClass && jobChanged) ||
					(Service.Config.AutoOffBetweenArea && !DataCenter.IsAutoDuty && (Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51])) ||
					(Service.Config.CancelStateOnCombatBeforeCountdown && Service.CountDownTime > 0.2f && DataCenter.InCombat) ||
					(ActionUpdater.AutoCancelTime != DateTime.MinValue && DateTime.Now > ActionUpdater.AutoCancelTime))
				{
					if (DataCenter.State)
					{
						CancelState();
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

				if (Service.Config.AutoOnYes && !DataCenter.State)
				{
					HashSet<ulong>? targetedIds = null;

					if (Service.Config.StartOnPartyIsInCombat2 && DataCenter.PartyMembers.Count > 1)
					{
						targetedIds ??= CollectHostileTargetObjectIds();
						if (AnyInCombatOrTargeted(DataCenter.PartyMembers, targetedIds))
						{
							DoStateCommandType(StateCommandType.Auto);
							return;
						}
					}

					var inFieldOp = DataCenter.IsInBozjanFieldOp || DataCenter.IsInBozjanFieldOpCE || DataCenter.IsInOccultCrescentOp;

					if (Service.Config.StartOnAllianceIsInCombat2 && DataCenter.AllianceMembers.Count > 1 && !inFieldOp)
					{
						targetedIds ??= CollectHostileTargetObjectIds();
						if (AnyInCombatOrTargeted(DataCenter.AllianceMembers, targetedIds))
						{
							DoStateCommandType(StateCommandType.Auto);
							return;
						}
					}

					if (Service.Config.StartOnFieldOpInCombat2 && inFieldOp)
					{
						targetedIds ??= CollectHostileTargetObjectIds();

						_hostileIds.Clear();
						foreach (var hostile in DataCenter.AllHostileTargets)
						{
							_ = _hostileIds.Add(hostile.GameObjectId);
						}

						var targets = TargetHelper.GetTargetsByRange(30f);
						for (var i = 0; i < targets.Count; i++)
						{
							var t = targets[i];
							if (t == null || (_hostileIds.Contains(t.GameObjectId) && !ObjectHelper.IsDummy(t)))
							{
								continue;
							}

							if (t.InCombat() || targetedIds.Contains(t.GameObjectId))
							{
								DoStateCommandType(StateCommandType.Auto);
								return;
							}
						}
					}

					if (Service.Config.StartOnAttackedBySomeone2)
					{
						IBattleChara? target = null;
						var playerId = Player.Object.GameObjectId;
						for (var i = 0; i < DataCenter.AllHostileTargets.Count; i++)
						{
							var battleChara = DataCenter.AllHostileTargets[i];

							// Validate liveness before the native TargetObjectId read: a try/catch around
							// AccessViolationException can't protect us (corrupted-state exceptions are not
							// catchable since .NET Core), so the object must be confirmed live beforehand.
							//future me dont mess with this unneccessarily
							if (battleChara == null || !battleChara.IsValid() || battleChara.Address == nint.Zero)
							{
								continue;
							}

							if (battleChara.TargetObjectId == playerId)
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

		// Scratch sets reused by UpdateRotationState, which runs every frame.
		private static readonly HashSet<ulong> _targetedIds = [];
		private static readonly HashSet<ulong> _hostileIds = [];

		/// <summary>
		/// Collects the ids of everything a hostile is currently targeting.
		/// </summary>
		private static HashSet<ulong> CollectHostileTargetObjectIds()
		{
			_targetedIds.Clear();
			foreach (var hostile in DataCenter.AllHostileTargets)
			{
				// Pre-validate before touching the native TargetObjectId read: a
				// try/catch around AccessViolationException does NOT protect us here
				// (corrupted-state exceptions are no longer catchable by managed code
				// since .NET Core), so the object must be confirmed live beforehand.
				if (hostile == null || !hostile.IsValid() || hostile.Address == nint.Zero)
				{
					continue;
				}

				if (hostile.TargetObjectId != 0)
				{
					_ = _targetedIds.Add(hostile.TargetObjectId);
				}
			}
			return _targetedIds;
		}

		private static bool AnyInCombatOrTargeted(List<IBattleChara> members, HashSet<ulong> targetedIds)
		{
			foreach (var member in members)
			{
				if (member != null && (member.InCombat() || targetedIds.Contains(member.GameObjectId)))
				{
					return true;
				}
			}
			return false;
		}
	}
}