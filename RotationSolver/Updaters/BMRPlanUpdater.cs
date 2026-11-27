using ECommons.Logging;
using RotationSolver.Integrations.IPC;

namespace RotationSolver.Updaters;

internal static class BMRPlanUpdater
{
	private static bool _subscribed;
	private static volatile bool _dirty = true;
	private static volatile bool _clearQueued;
	private static DateTime _lastPoll = DateTime.MinValue;

	private static readonly HashSet<uint> _queuedActionIds = [];

	private static readonly HashSet<uint> _activeActionIds = [];

	private static readonly TimeSpan FallbackPollInterval = TimeSpan.FromSeconds(5);

	public static void Enable()
	{
		if (_subscribed)
		{
			return;
		}

		try
		{
			_subscribed = BMRPlan_IPCSubscriber.SubscribeActionsChanged(OnActionsChanged);
		}
		catch (Exception ex)
		{
			PluginLog.Error($"[BMRPlanUpdater] Failed to subscribe to Plan.ActionsChanged: {ex}");
		}
	}

	public static void Reset()
	{
		_queuedActionIds.Clear();
		_activeActionIds.Clear();
		_dirty = true;
		_clearQueued = false;
		_lastPoll = DateTime.MinValue;
	}

	public static void Disable()
	{
		if (!_subscribed)
		{
			return;
		}

		try
		{
			BMRPlan_IPCSubscriber.UnsubscribeActionsChanged();
		}
		catch (Exception ex)
		{
			PluginLog.Error($"[BMRPlanUpdater] Failed to unsubscribe from Plan.ActionsChanged: {ex}");
		}
		finally
		{
			_subscribed = false;
			Reset();
		}
	}

	private static void OnActionsChanged()
	{
		_dirty = true;
		_clearQueued = true;
	}

	public static void Update()
	{
		if (!Service.Config.UseBmrPlan)
		{
			if (DataCenter.BMRPlannedActions.Count > 0 || _queuedActionIds.Count > 0)
			{
				DataCenter.ResetBmrPlanData();
				Reset();
			}

			return;
		}

		Enable();

		if (!BMRPlan_IPCSubscriber.IsEnabled)
		{
			DataCenter.ResetBmrPlanData();
			Reset();
			return;
		}

		if (_clearQueued)
		{
			_clearQueued = false;
			_queuedActionIds.Clear();
		}

		var now = DateTime.Now;
		if (_dirty || now - _lastPoll >= FallbackPollInterval)
		{
			try
			{
				DataCenter.BMRPlannedActions = BMRPlan_IPCSubscriber.GetUpcomingPlannedActions(Service.Config.BMRPlanLookAheadSeconds);
				_lastPoll = now;
				_dirty = false;
			}
			catch (Exception ex)
			{
				PluginLog.Error($"[BMRPlanUpdater] Failed to poll Plan.GetUpcomingActions: {ex}");
				DataCenter.ResetBmrPlanData();
				return;
			}
		}

		QueueActiveActions(now);
	}

	private static void QueueActiveActions(DateTime now)
	{
		var actions = DataCenter.BMRPlannedActions;
		var elapsedSincePoll = (float)(now - _lastPoll).TotalSeconds;

		_activeActionIds.Clear();
		foreach (var planned in actions)
		{
			if (IsActive(planned, elapsedSincePoll, out _))
			{
				_ = _activeActionIds.Add(planned.ActionId);
			}
		}

		_queuedActionIds.IntersectWith(_activeActionIds);

		if (_activeActionIds.Count == 0)
		{
			return;
		}

		var rotationActions = RotationUpdater.CurrentRotationActions;
		var dutyActions = RotationUpdater.CurrentDutyRotationActions;

		foreach (var planned in actions)
		{
			if (!IsActive(planned, elapsedSincePoll, out var windowEndIn))
			{
				continue;
			}

			if (_queuedActionIds.Contains(planned.ActionId))
			{
				continue;
			}

			var matchingAction = ((ActionID)planned.ActionId).GetActionFromID(false, rotationActions, dutyActions);
			if (matchingAction == null)
			{
				var adjustedId = Service.GetAdjustedActionId(planned.ActionId);
				if (adjustedId != 0 && adjustedId != planned.ActionId)
				{
					matchingAction = ((ActionID)adjustedId).GetActionFromID(false, rotationActions, dutyActions);
				}
			}

			if (matchingAction == null)
			{
				continue;
			}

			_ = _queuedActionIds.Add(planned.ActionId);
			DataCenter.AddCommandAction(matchingAction, windowEndIn);
			PluginLog.Debug($"[BMRPlanUpdater] Queued BMR planned action: {matchingAction.Name} (ActionId: {planned.ActionId}) for {windowEndIn:F1}s");
		}
	}

	private static bool IsActive(in BMRPlannedAction planned, float elapsedSincePoll, out float windowEndIn)
	{
		windowEndIn = planned.WindowEndIn - elapsedSincePoll;

		return planned.ActionType == 1
			&& planned.ActionId != 0
			&& planned.ActivationIn - elapsedSincePoll <= 0f
			&& windowEndIn > 0f;
	}
}
