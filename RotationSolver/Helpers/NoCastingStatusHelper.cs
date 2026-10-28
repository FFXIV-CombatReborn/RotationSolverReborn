using ECommons.GameHelpers;
using RotationSolver.Basic.Configuration;

namespace RotationSolver.Helpers;

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

		var statusList = Player.Object?.StatusList;
		if (statusList == null)
		{
			return false;
		}

		var found = false;
		foreach (var status in statusList)
		{
			if (status == null || status.StatusId == 0 || !noCastingStatus.Contains(status.StatusId))
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
