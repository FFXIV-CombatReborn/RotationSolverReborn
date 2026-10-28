using ECommons.Logging;
using RotationSolver.IPC;

namespace RotationSolver.Updaters;

internal static class BMRPlanUpdater
{
	private static bool _subscribed;
	private static volatile bool _dirty = true;
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
			BMRPlan_IPCSubscriber.SubscribeActionsChanged(OnActionsChanged);
			_subscribed = true;
		}
		catch (Exception ex)
		{
			PluginLog.Error($"[BMRPlanUpdater] Failed to subscribe to Plan.ActionsChanged: {ex}");
		}
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
			_queuedActionIds.Clear();
		}
	}

	private static void OnActionsChanged()
	{
		_dirty = true;
		_queuedActionIds.Clear();
	}

	public static void Update()
	{
		if (!Service.Config.UseBmrPlan)
		{
			if (DataCenter.BMRPlannedActions.Count > 0)
			{
				DataCenter.ResetBmrPlanData();
				_queuedActionIds.Clear();
			}

			return;
		}

		Enable();

		if (!BMRPlan_IPCSubscriber.IsEnabled)
		{
			DataCenter.ResetBmrPlanData();
			_queuedActionIds.Clear();
			_dirty = true;
			return;
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

			if (!_queuedActionIds.Add(planned.ActionId))
			{
				continue;
			}

			var matchingAction = ((ActionID)planned.ActionId).GetActionFromID(false, rotationActions, dutyActions);
			if (matchingAction == null)
			{
				continue;
			}

			DataCenter.AddCommandAction(matchingAction, windowEndIn);
			PluginLog.Debug($"[BMRPlanUpdater] Queued BMR planned action: {matchingAction.Name} (ActionId: {planned.ActionId}) for {windowEndIn:F1}s");
		}
	}

	private static bool IsActive(in BMRPlannedAction planned, float elapsedSincePoll, out float windowEndIn)
	{
		windowEndIn = planned.WindowEndIn - elapsedSincePoll;

		return planned.ActionId != 0
			&& planned.ActivationIn - elapsedSincePoll <= 0f
			&& windowEndIn > 0f;
	}
}
