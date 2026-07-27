using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;
using ECommons.EzIpcManager;
using ECommons.Logging;

#pragma warning disable CS0649

namespace RotationSolver.IPC;

/// <summary>
/// Subscribes to BossMod's Cooldown Planner IPC endpoints so RSR can consume planned actions from
/// BMR's Cooldown Planner and feed them into the action interception system.
/// </summary>
internal static class BMRPlan_IPCSubscriber
{
	private static readonly EzIPCDisposalToken[] _disposalTokens =
		EzIPC.Init(typeof(BMRPlan_IPCSubscriber), "BossMod", SafeWrapper.AnyException);

	internal static bool IsEnabled => IPCSubscriber_Common.IsReady("BossModReborn");

	/// <summary>
	/// Returns a JSON-serialized array of <see cref="BMRPlannedAction"/>
	/// </summary>
	[EzIPC("Plan.GetUpcomingActions", true)]
	internal static readonly Func<float, string>? GetUpcomingActions;

	private static ICallGateSubscriber<object>? _actionsChangedSubscriber;
	private static Action? _actionsChangedCallback;

	/// <summary>
	/// Subscribes to the BossMod.Plan.ActionsChanged push notification, fired whenever the active
	/// plan changes so consumers know to re-poll <see cref="GetUpcomingActions"/>.
	/// </summary>
	internal static void SubscribeActionsChanged(Action callback)
	{
		UnsubscribeActionsChanged();

		try
		{
			_actionsChangedCallback = callback;
			_actionsChangedSubscriber = Svc.PluginInterface.GetIpcSubscriber<object>("BossMod.Plan.ActionsChanged");
			_actionsChangedSubscriber.Subscribe(OnActionsChanged);
		}
		catch (Exception ex)
		{
			PluginLog.LogDebug($"[BMRPlan_IPCSubscriber] Failed to subscribe to Plan.ActionsChanged: {ex}");
			_actionsChangedSubscriber = null;
			_actionsChangedCallback = null;
		}
	}

	internal static void UnsubscribeActionsChanged()
	{
		if (_actionsChangedSubscriber != null)
		{
			try
			{
				_actionsChangedSubscriber.Unsubscribe(OnActionsChanged);
			}
			catch (Exception ex)
			{
				PluginLog.LogDebug($"[BMRPlan_IPCSubscriber] Failed to unsubscribe from Plan.ActionsChanged: {ex}");
			}
			finally
			{
				_actionsChangedSubscriber = null;
			}
		}

		_actionsChangedCallback = null;
	}

	private static void OnActionsChanged() => _actionsChangedCallback?.Invoke();

	/// <summary>
	/// Polls BMR for the upcoming planned actions within the given lookahead window.
	/// Returns an empty list if BMR is unavailable, no plan is active, or the call fails.
	/// </summary>
	internal static List<BMRPlannedAction> GetUpcomingPlannedActions(float lookAheadSeconds)
	{
		var func = GetUpcomingActions;
		if (func == null)
		{
			PluginLog.LogDebug("[BMRPlan_IPCSubscriber] GetUpcomingActions IPC func is not bound (BossMod.Plan.GetUpcomingActions endpoint not found).");
			return [];
		}

		try
		{
			var json = func(lookAheadSeconds);
			PluginLog.LogDebug($"[BMRPlan_IPCSubscriber] Plan.GetUpcomingActions({lookAheadSeconds}) raw result: {json}");
			if (string.IsNullOrEmpty(json))
			{
				return [];
			}

			var result = System.Text.Json.JsonSerializer.Deserialize<List<BMRPlannedAction>>(json) ?? [];
			PluginLog.LogDebug($"[BMRPlan_IPCSubscriber] Deserialized {result.Count} planned action(s).");
			return result;
		}
		catch (Exception ex)
		{
			PluginLog.LogDebug($"[BMRPlan_IPCSubscriber] Failed to deserialize Plan.GetUpcomingActions result: {ex}");
			return [];
		}
	}

	internal static void Dispose()
	{
		UnsubscribeActionsChanged();
		IPCSubscriber_Common.DisposeAll(_disposalTokens);
	}
}
