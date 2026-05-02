using RotationSolver.IPC;

namespace RotationSolver.Updaters;

internal static class BossModUpdater
{
	private static bool _checkedAvailability;
	private static bool _isAvailable;

	public static void Update()
	{
		if (!Service.Config.UseBmrTimeline)
		{
			if (DataCenter.BMRHasActiveModule)
				DataCenter.ResetBmrData();
			return;
		}

		if (!_checkedAvailability)
		{
			_isAvailable = BMRTimeline_IPCSubscriber.IsEnabled;
			_checkedAvailability = true;
		}

		if (!_isAvailable)
		{
			DataCenter.ResetBmrData();
			return;
		}

		try
		{
			DataCenter.BMRHasActiveModule = BMRTimeline_IPCSubscriber.HasActiveModule?.Invoke() ?? false;

			if (!DataCenter.BMRHasActiveModule)
			{
				DataCenter.ResetBmrData();
				return;
			}

			DataCenter.BMRActiveModuleName = BMRTimeline_IPCSubscriber.ActiveModuleName?.Invoke();

			// Store whether IPC Funcs are bound (null = BMR doesn't have that endpoint)
			DataCenter.BMRDebugTimelineRwFunc = BMRTimeline_IPCSubscriber.NextRaidwideIn != null;
			DataCenter.BMRDebugTimelineTbFunc = BMRTimeline_IPCSubscriber.NextTankbusterIn != null;
			DataCenter.BMRDebugHintsRwFunc = BMRTimeline_IPCSubscriber.NextRaidwideDamageIn != null;
			DataCenter.BMRDebugHintsTbFunc = BMRTimeline_IPCSubscriber.NextTankbusterDamageIn != null;

			// Poll Timeline endpoints (state machine flags)
			var timelineRaidwide = BMRTimeline_IPCSubscriber.NextRaidwideIn?.Invoke() ?? float.MaxValue;
			var timelineTankbuster = BMRTimeline_IPCSubscriber.NextTankbusterIn?.Invoke() ?? float.MaxValue;
			DataCenter.BMRNextKnockbackIn = BMRTimeline_IPCSubscriber.NextKnockbackIn?.Invoke() ?? float.MaxValue;
			DataCenter.BMRNextDowntimeIn = BMRTimeline_IPCSubscriber.NextDowntimeIn?.Invoke() ?? float.MaxValue;
			DataCenter.BMRNextDowntimeEndIn = BMRTimeline_IPCSubscriber.NextDowntimeEndIn?.Invoke() ?? float.MaxValue;
			DataCenter.BMRNextVulnerableIn = BMRTimeline_IPCSubscriber.NextVulnerableIn?.Invoke() ?? float.MaxValue;
			DataCenter.BMRNextVulnerableEndIn = BMRTimeline_IPCSubscriber.NextVulnerableEndIn?.Invoke() ?? float.MaxValue;
			DataCenter.BMRDebugTimelineRaidwide = timelineRaidwide;
			DataCenter.BMRDebugTimelineTankbuster = timelineTankbuster;

			// Poll Hints endpoints (component-level damage predictions)
			var damageIn = BMRTimeline_IPCSubscriber.NextDamageIn?.Invoke() ?? float.MaxValue;
			var damageType = BMRTimeline_IPCSubscriber.NextDamageType?.Invoke() ?? 0;
			DataCenter.BMRNextDamageIn = damageIn;
			DataCenter.BMRNextDamageType = (PredictedDamageType)damageType;
			DataCenter.BMRDebugGenericDamageIn = damageIn;
			DataCenter.BMRDebugGenericDamageType = damageType;

			// Type-specific Hints endpoints
			var hintsRaidwide = BMRTimeline_IPCSubscriber.NextRaidwideDamageIn?.Invoke() ?? float.MaxValue;
			var hintsTankbuster = BMRTimeline_IPCSubscriber.NextTankbusterDamageIn?.Invoke() ?? float.MaxValue;
			DataCenter.BMRDebugHintsRaidwide = hintsRaidwide;
			DataCenter.BMRDebugHintsTankbuster = hintsTankbuster;

			// Filter out invalid values (<=0 means endpoint missing/SafeWrapper default or damage already resolved)
			if (hintsRaidwide <= 0f) hintsRaidwide = float.MaxValue;
			if (hintsTankbuster <= 0f) hintsTankbuster = float.MaxValue;

			// Final fallback: use generic damage prediction if type matches
			var genericRaidwide = (damageType == 2 && damageIn > 0f) ? damageIn : float.MaxValue;
			var genericTankbuster = (damageType == 1 && damageIn > 0f) ? damageIn : float.MaxValue;

			// Merge all sources: Timeline OR type-specific Hints OR generic damage prediction
			DataCenter.BMRNextRaidwideIn = Math.Min(Math.Min(timelineRaidwide, hintsRaidwide), genericRaidwide);
			DataCenter.BMRNextTankbusterIn = Math.Min(Math.Min(timelineTankbuster, hintsTankbuster), genericTankbuster);

			DataCenter.BMRSpecialModeIn = BMRTimeline_IPCSubscriber.SpecialModeIn?.Invoke() ?? float.MaxValue;
			DataCenter.BMRSpecialModeType = (SpecialMode)(BMRTimeline_IPCSubscriber.SpecialModeType?.Invoke() ?? 0);
			DataCenter.BMRDebugTimelineWalk = BMRTimeline_IPCSubscriber.DebugTimelineWalk?.Invoke();
		}
		catch
		{
			DataCenter.ResetBmrData();
			_checkedAvailability = false;
		}
	}

	public static void ResetAvailabilityCheck()
	{
		_checkedAvailability = false;
	}
}