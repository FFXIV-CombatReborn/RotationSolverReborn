using ECommons.Logging;
using RotationSolver.IPC;

namespace RotationSolver.Updaters;

/// <summary>
/// Polls BossMod's Cooldown Planner IPC for upcoming planned actions and, when a planned action's
/// activation window becomes active, queues it via <see cref="DataCenter.AddCommandAction"/> so RSR
/// itself drives usage of the action through the normal command-action / interception pipeline
/// (<see cref="ActionQueueManager"/>), rather than passively waiting for something else to trigger it.
/// Refreshes immediately when BMR notifies that the plan changed, with a periodic fallback poll.
/// </summary>
internal static class BMRPlanUpdater
{
	private static bool _subscribed;
	private static volatile bool _dirty = true;
	private static DateTime _lastPoll = DateTime.MinValue;

	// Ids of planned actions already queued for the current activation window, so we don't re-queue every frame.
	private static readonly HashSet<uint> _queuedActionIds = [];

	// Ids of planned actions whose activation window is open this update.
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

		// The readiness check is cached, so polling it picks up BossMod loading after RSR.
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

	/// <summary>
	/// Checks the cached planned actions against the actual elapsed time since they were polled and
	/// queues any that are now within their activation window so RSR uses them through the normal
	/// command-action pipeline.
	/// </summary>
	private static void QueueActiveActions(DateTime now)
	{
		var actions = DataCenter.BMRPlannedActions;
		var elapsedSincePoll = (float)(now - _lastPoll).TotalSeconds;

		// The same action can appear several times in a plan (e.g. a cooldown at two timestamps), so
		// work out which ids are active before forgetting any; otherwise a later, inactive entry would
		// clear the queued flag of the active one and it would be re-queued every other frame.
		_activeActionIds.Clear();
		foreach (var planned in actions)
		{
			if (IsActive(planned, elapsedSincePoll, out _))
			{
				_ = _activeActionIds.Add(planned.ActionId);
			}
		}

		// Ids whose window has closed may be queued again the next time they come up.
		_queuedActionIds.IntersectWith(_activeActionIds);

		if (_activeActionIds.Count == 0)
		{
			return;
		}

		var rotationActions = RotationUpdater.CurrentRotationActions ?? [];
		var dutyActions = DataCenter.CurrentDutyRotation?.AllActions ?? [];

		foreach (var planned in actions)
		{
			if (!IsActive(planned, elapsedSincePoll, out var windowEndIn))
			{
				continue;
			}

			if (!_queuedActionIds.Add(planned.ActionId))
			{
				// Already queued for this activation window.
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

		// ActionId 0 means BMR couldn't resolve the plan entry to a concrete action.
		return planned.ActionId != 0
			&& planned.ActivationIn - elapsedSincePoll <= 0f
			&& windowEndIn > 0f;
	}
}
