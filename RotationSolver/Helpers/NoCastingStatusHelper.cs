using ECommons.GameHelpers;
using RotationSolver.Basic.Configuration;

namespace RotationSolver.Helpers;

internal static class NoCastingStatusHelper
{
	/// <summary>
	/// Scans the player's statuses against <see cref="OtherConfiguration.NoCastingStatus"/>.
	/// Reads the configured set directly each call so edits made in the UI apply immediately.
	/// </summary>
	/// <param name="minRemainingTime">The shortest remaining time among matching statuses; permanent statuses count as <see cref="float.MaxValue"/>.</param>
	/// <returns>True if the player has at least one no-casting status.</returns>
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
