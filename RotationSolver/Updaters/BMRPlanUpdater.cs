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
	private static bool _checkedAvailability;
	private static bool _isAvailable;
	private static volatile bool _dirty = true;
	private static DateTime _lastPoll = DateTime.MinValue;

	// Ids of planned actions already queued for the current activation window, so we don't re-queue every frame.
	private static readonly HashSet<uint> _queuedActionIds = [];

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

		if (!_checkedAvailability)
		{
			_isAvailable = BMRPlan_IPCSubscriber.IsEnabled;
			_checkedAvailability = true;
		}

		if (!_isAvailable)
		{
			DataCenter.ResetBmrPlanData();
			_queuedActionIds.Clear();
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
				_checkedAvailability = false;
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
		if (actions.Count == 0)
		{
			return;
		}

		var elapsedSincePoll = (float)(now - _lastPoll).TotalSeconds;
		var rotationActions = RotationUpdater.CurrentRotationActions ?? [];
		var dutyActions = DataCenter.CurrentDutyRotation?.AllActions ?? [];

		for (var i = 0; i < actions.Count; i++)
		{
			var planned = actions[i];

			// ActionId 0 means BMR couldn't resolve the plan entry to a concrete action.
			if (planned.ActionId == 0)
			{
				continue;
			}

			var activationIn = planned.ActivationIn - elapsedSincePoll;
			var windowEndIn = planned.WindowEndIn - elapsedSincePoll;

			if (activationIn > 0f || windowEndIn <= 0f)
			{
				_ = _queuedActionIds.Remove(planned.ActionId);
				//PluginLog.Debug($"[BMRPlanUpdater] Planned action {planned.ActionId} is not active yet or already expired (ActivationIn: {activationIn:F1}s, WindowEndIn: {windowEndIn:F1}s)");
				continue;
			}

			if (!_queuedActionIds.Add(planned.ActionId))
			{
				// Already queued for this activation window.
				//PluginLog.Debug($"[BMRPlanUpdater] Planned action {planned.ActionId} already queued for this activation window.");
				continue;
			}

			var matchingAction = ((ActionID)planned.ActionId).GetActionFromID(false, rotationActions, dutyActions);
			if (matchingAction == null)
			{
				//PluginLog.Debug($"[BMRPlanUpdater] No matching action found for planned ActionId {planned.ActionId}");
				continue;
			}

			DataCenter.AddCommandAction(matchingAction, windowEndIn);
			PluginLog.Debug($"[BMRPlanUpdater] Queued BMR planned action: {matchingAction.Name} (ActionId: {planned.ActionId}) for {windowEndIn:F1}s");
		}
	}

	public static void ResetAvailabilityCheck()
	{
		_checkedAvailability = false;
		_dirty = true;
	}
}

