using ECommons.Logging;
using RotationSolver.Integrations.IPC;

namespace RotationSolver.Updaters;

internal static class BossModUpdater
{
	private const long ErrorLogIntervalMs = 10000;
	private static readonly Dictionary<string, long> _lastErrorTicks = [];

	private static readonly Func<Vector3, bool> _safeIsPositionSafe = pos =>
	{
		var func = BMRTimeline_IPCSubscriber.IsPositionSafe;
		if (func == null)
		{
			return true;
		}

		try
		{
			return func(pos);
		}
		catch (Exception ex)
		{
			LogIpcError("Hints.IsPositionSafe", ex);
			return true;
		}
	};

	private static readonly Func<Vector3, Vector3, bool> _safeIsDashSafe = (from, to) =>
	{
		var func = BMRTimeline_IPCSubscriber.IsDashSafe;
		if (func == null)
		{
			return true;
		}

		try
		{
			return func(from, to);
		}
		catch (Exception ex)
		{
			LogIpcError("Hints.IsDashSafe", ex);
			return true;
		}
	};

	private static readonly Func<float, bool, bool> _safeIsFixedDashSafe = (distance, forward) =>
	{
		var func = BMRTimeline_IPCSubscriber.IsFixedDashSafe;
		if (func == null)
		{
			return true;
		}

		try
		{
			return func(distance, forward);
		}
		catch (Exception ex)
		{
			LogIpcError("Hints.IsFixedDashSafe", ex);
			return true;
		}
	};

	private static void LogIpcError(string endpoint, Exception ex)
	{
		var now = Environment.TickCount64;
		if (_lastErrorTicks.TryGetValue(endpoint, out var lastTick) && now - lastTick < ErrorLogIntervalMs)
		{
			return;
		}

		_lastErrorTicks[endpoint] = now;
		PluginLog.Debug($"[BossModUpdater] IPC '{endpoint}' failed: {ex.Message}");
	}

	private static T SafeCall<T>(Func<T>? func, T fallback, string endpoint)
	{
		if (func == null)
		{
			return fallback;
		}

		try
		{
			return func();
		}
		catch (Exception ex)
		{
			LogIpcError(endpoint, ex);
			return fallback;
		}
	}

	private static float NormalizeTimer(float value)
	{
		return float.IsFinite(value) && value > 0f ? value : float.MaxValue;
	}

	public static void Update()
	{
		if (!BMRTimeline_IPCSubscriber.IsEnabled)
		{
			DataCenter.ResetBmrData();
			return;
		}

		try
		{
			DataCenter.BMRHasActiveModule = SafeCall(BMRTimeline_IPCSubscriber.HasActiveModule, false, "HasActiveModule");

			DataCenter.BMRForceCancelCast = SafeCall(BMRInfo_IPCSubscriber.ForceCancelCast, false, "ForceCancelCast");
			DataCenter.BMRForceCancelCastAI = SafeCall(BMRInfo_IPCSubscriber.ForceCancelCastAI, false, "ForceCancelCastAI");
			DataCenter.BMRIsMoving = SafeCall(BMRInfo_IPCSubscriber.IsMoving, false, "IsMoving");

			DataCenter.BMRActiveModuleName = SafeCall(BMRTimeline_IPCSubscriber.ActiveModuleName, (string?)null, "ActiveModuleName");

			DataCenter.BMRDebugTimelineRwFunc = BMRTimeline_IPCSubscriber.NextRaidwideIn != null;
			DataCenter.BMRDebugTimelineTbFunc = BMRTimeline_IPCSubscriber.NextTankbusterIn != null;
			DataCenter.BMRDebugHintsRwFunc = BMRTimeline_IPCSubscriber.NextRaidwideDamageIn != null;
			DataCenter.BMRDebugHintsTbFunc = BMRTimeline_IPCSubscriber.NextTankbusterDamageIn != null;

			var timelineRaidwide = NormalizeTimer(SafeCall(BMRTimeline_IPCSubscriber.NextRaidwideIn, float.MaxValue, "Timeline.NextRaidwideIn"));
			var timelineTankbuster = NormalizeTimer(SafeCall(BMRTimeline_IPCSubscriber.NextTankbusterIn, float.MaxValue, "Timeline.NextTankbusterIn"));
			DataCenter.BMRNextKnockbackIn = NormalizeTimer(SafeCall(BMRTimeline_IPCSubscriber.NextKnockbackIn, float.MaxValue, "Timeline.NextKnockbackIn"));
			DataCenter.BMRNextDowntimeIn = NormalizeTimer(SafeCall(BMRTimeline_IPCSubscriber.NextDowntimeIn, float.MaxValue, "Timeline.NextDowntimeIn"));
			DataCenter.BMRNextDowntimeEndIn = NormalizeTimer(SafeCall(BMRTimeline_IPCSubscriber.NextDowntimeEndIn, float.MaxValue, "Timeline.NextDowntimeEndIn"));
			DataCenter.BMRNextVulnerableIn = NormalizeTimer(SafeCall(BMRTimeline_IPCSubscriber.NextVulnerableIn, float.MaxValue, "Timeline.NextVulnerableIn"));
			DataCenter.BMRNextVulnerableEndIn = NormalizeTimer(SafeCall(BMRTimeline_IPCSubscriber.NextVulnerableEndIn, float.MaxValue, "Timeline.NextVulnerableEndIn"));
			DataCenter.BMRDebugTimelineRaidwide = timelineRaidwide;
			DataCenter.BMRDebugTimelineTankbuster = timelineTankbuster;

			var damageIn = NormalizeTimer(SafeCall(BMRTimeline_IPCSubscriber.NextDamageIn, float.MaxValue, "Hints.NextDamageIn"));
			var damageType = SafeCall(BMRTimeline_IPCSubscriber.NextDamageType, 0, "Hints.NextDamageType");
			DataCenter.BMRNextDamageIn = damageIn;
			DataCenter.BMRNextDamageType = Enum.IsDefined(typeof(PredictedDamageType), damageType) ? (PredictedDamageType)damageType : PredictedDamageType.None;
			DataCenter.BMRDebugGenericDamageIn = damageIn;
			DataCenter.BMRDebugGenericDamageType = damageType;

			var hintsRaidwide = NormalizeTimer(SafeCall(BMRTimeline_IPCSubscriber.NextRaidwideDamageIn, float.MaxValue, "Hints.NextRaidwideDamageIn"));
			var hintsTankbuster = NormalizeTimer(SafeCall(BMRTimeline_IPCSubscriber.NextTankbusterDamageIn, float.MaxValue, "Hints.NextTankbusterDamageIn"));
			DataCenter.BMRDebugHintsRaidwide = hintsRaidwide;
			DataCenter.BMRDebugHintsTankbuster = hintsTankbuster;

			var genericRaidwide = damageType == 2 ? damageIn : float.MaxValue;
			var genericTankbuster = damageType == 1 ? damageIn : float.MaxValue;

			DataCenter.BMRNextRaidwideIn = Math.Min(Math.Min(timelineRaidwide, hintsRaidwide), genericRaidwide);
			DataCenter.BMRNextTankbusterIn = Math.Min(Math.Min(timelineTankbuster, hintsTankbuster), genericTankbuster);

			DataCenter.BMRSpecialModeIn = NormalizeTimer(SafeCall(BMRTimeline_IPCSubscriber.SpecialModeIn, float.MaxValue, "Hints.SpecialModeIn"));
			var specialModeType = SafeCall(BMRTimeline_IPCSubscriber.SpecialModeType, 0, "Hints.SpecialModeType");
			DataCenter.BMRSpecialModeType = Enum.IsDefined(typeof(SpecialMode), specialModeType) ? (SpecialMode)specialModeType : SpecialMode.Normal;

			DataCenter.BMRIsPositionSafe = BMRTimeline_IPCSubscriber.IsPositionSafe != null ? _safeIsPositionSafe : null;
			DataCenter.BMRIsDashSafe = BMRTimeline_IPCSubscriber.IsDashSafe != null ? _safeIsDashSafe : null;
			DataCenter.BMRIsFixedDashSafe = BMRTimeline_IPCSubscriber.IsFixedDashSafe != null ? _safeIsFixedDashSafe : null;
		}
		catch (Exception ex)
		{
			LogIpcError("Update", ex);
			DataCenter.ResetBmrData();
		}
	}
}