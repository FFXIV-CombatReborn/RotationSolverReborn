using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using RotationSolver.Basic.Configuration;

namespace RotationSolver.Updaters;

internal static class CancelCastUpdater
{
	private static RandomDelay _tarStopCastDelay = new(() => Service.Config.StopCastingDelay);

	internal static unsafe void UpdateCancelCast()
	{
		if (TrainingModeGate.ExecutionLocked)
		{
			return;
		}

		if (Player.Object == null || !Player.Object.IsCasting)
		{
			return;
		}

		if (!DataCenter.State)
		{
			return;
		}

		var castTarget = Svc.Objects.SearchById(Player.Object.CastTargetObjectId) as IBattleChara;

		var tarDead = Service.Config.UseStopCasting
			&& castTarget != null
			&& castTarget.IsEnemy()
			&& castTarget.CurrentHp == 0;

		// Cancel raise cast if target already has Raise status
		var tarHasRaise = castTarget != null && castTarget.HasStatus(false, StatusID.Raise);

		// Cancel cast in PvP if the target gains Guard and the action does not ignore Guard
		var tarHasGuard = DataCenter.IsPvP && Service.Config.PvpGuardCancel
			&& castTarget != null
			&& castTarget.HasStatus(false, StatusID.Guard)
			&& !(((ActionID)Player.Object.CastActionId).GetActionFromID(true, RotationUpdater.CurrentRotationActions)
				is IBaseAction guardCheckAction && (!guardCheckAction.Setting.IgnoreGuard || (DataCenter.Job == Job.BLM && !guardCheckAction.Setting.IgnoreGuard && !StatusHelper.PlayerHasStatus(true, StatusID.WreathOfFire))));

		var statusTimes = GetStatusTimes();

		var minStatusTime = float.MaxValue;
		for (var i = 0; i < statusTimes.Length; i++)
		{
			if (statusTimes[i] < minStatusTime)
			{
				minStatusTime = statusTimes[i];
			}
		}

		var remainingCast = MathF.Max(0, Player.Object.TotalCastTime - Player.Object.CurrentCastTime);

		// Cancel immediately if the player currently has any active NoCastingStatus
		var hasNoCastingStatus = statusTimes.Length > 0;

		// Cancel if a "no-casting" status will expire before the cast completes and it's soon (<3s)
		var stopDueStatus = hasNoCastingStatus
			&& minStatusTime <= remainingCast
			&& minStatusTime < 3f;

		var bmrPyretic = DataCenter.BMRSpecialModeType == SpecialMode.Pyretic;

		var shouldStopHealing =
			Service.Config.StopHealingAfterThresholdExperimental2
			&& DataCenter.InCombat
			&& !CustomRotation.HealingWhileDoingNothing
			&& DataCenter.CommandNextAction?.AdjustedID != Player.Object.CastActionId
			&& ((ActionID)Player.Object.CastActionId).GetActionFromID(true, RotationUpdater.CurrentRotationActions)
				is IBaseAction { Setting.GCDSingleHeal: true }
			&& (DataCenter.MergedStatus & (AutoStatus.HealAreaSpell | AutoStatus.HealSingleSpell)) == 0;

		if (_tarStopCastDelay.Delay(tarDead) || hasNoCastingStatus || stopDueStatus || tarHasRaise || tarHasGuard || shouldStopHealing)
		{
			var uiState = UIState.Instance();
			if (uiState != null)
			{
				uiState->Hotbar.CancelCast();
			}
		}
	}

	private static float[] GetStatusTimes()
	{
		List<float> statusTimes = [];
		if (Player.Object?.StatusList != null)
		{
			foreach (var status in Player.Object.StatusList)
			{
				if (OtherConfiguration.NoCastingStatus.Contains(status.StatusId))
				{
					statusTimes.Add(status.RemainingTime);
				}
			}
		}
		return [.. statusTimes];
	}
}
