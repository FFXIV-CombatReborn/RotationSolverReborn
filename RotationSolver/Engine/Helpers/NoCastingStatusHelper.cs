using ECommons.GameHelpers;

namespace RotationSolver.Engine.Helpers;

internal static class NoCastingStatusHelper
{
	internal const float ResolveWindow = 3f;

	internal static bool PlayerHasNoCastingStatus(out float minRemainingTime)
	{
		minRemainingTime = float.MaxValue;

		var noCastingStatus = OtherConfiguration.NoCastingStatus;
		if (noCastingStatus == null || noCastingStatus.Count == 0)
		{
			return false;
		}

		var found = false;
		foreach (ref readonly var status in Player.Object.GetStatuses())
		{
			if (status.StatusId == 0 || !noCastingStatus.Contains(status.StatusId))
			{
				continue;
			}

			found = true;
			var remaining = status.RemainingTime == 0f ? float.MaxValue : status.RemainingTime;
			if (remaining < minRemainingTime)
			{
				minRemainingTime = remaining;
			}
		}

		return found;
	}
}
