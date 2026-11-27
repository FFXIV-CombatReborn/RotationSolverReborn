using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace RotationSolver.Updaters;

internal static class CancelCastUpdater
{
#pragma warning disable IDE0044
	private static RandomDelay _tarStopCastDelay = new(() => Service.Config.StopCastingDelay);
#pragma warning restore IDE0044
	private static uint _lastCastActionId;
	private static ulong _lastCastTargetId;

	internal static unsafe void UpdateCancelCast()
	{
		var player = Player.Object;
		if (player == null || !player.IsCasting || !DataCenter.State)
		{
			_ = _tarStopCastDelay.Delay(false);
			_lastCastActionId = 0;
			_lastCastTargetId = 0;
			return;
		}

		if (player.CastActionId != _lastCastActionId || player.CastTargetObjectId != _lastCastTargetId)
		{
			_ = _tarStopCastDelay.Delay(false);
			_lastCastActionId = player.CastActionId;
			_lastCastTargetId = player.CastTargetObjectId;
		}

		var castTarget = Svc.Objects.SearchById(player.CastTargetObjectId) as IBattleChara;

		var tarDead = Service.Config.UseStopCasting
			&& castTarget != null
			&& castTarget.IsEnemy()
			&& castTarget.CurrentHp == 0;

		var stopForDeadTarget = _tarStopCastDelay.Delay(tarDead);

		var tarHasRaise = castTarget != null && castTarget.HasStatus(false, StatusID.Raise, StatusID.Raise_1140);

		var hasNoCastingStatus = !DataCenter.IsPvP
			&& NoCastingStatusHelper.PlayerHasNoCastingStatus(out var noCastingRemaining)
			&& noCastingRemaining < NoCastingStatusHelper.ResolveWindow;

		var tarHasGuard = DataCenter.IsPvP && Service.Config.PvpGuardCancel
			&& castTarget != null
			&& castTarget.IsEnemy()
			&& castTarget.HasStatus(false, StatusID.Guard)
			&& ((ActionID)player.CastActionId).GetActionFromID(true, RotationUpdater.CurrentRotationActions)
				is IBaseAction { Setting.IgnoreGuard: false };

		var bmrForceCancelCast = Service.Config.UseBmrTimeline
			&& (DataCenter.BMRForceCancelCast || DataCenter.BMRForceCancelCastAI);

		if (stopForDeadTarget || hasNoCastingStatus || tarHasRaise || tarHasGuard || bmrForceCancelCast || ShouldStopHealing(player))
		{
			var uiState = UIState.Instance();
			if (uiState != null)
			{
				uiState->Hotbar.CancelCast();
			}
		}
	}

	private static bool ShouldStopHealing(IPlayerCharacter player)
	{
		return Service.Config.StopHealingAfterThresholdExperimental2
			&& DataCenter.InCombat
			&& !CustomRotation.HealingWhileDoingNothing
			&& DataCenter.CommandNextAction?.AdjustedID != player.CastActionId
			&& ((ActionID)player.CastActionId).GetActionFromID(true, RotationUpdater.CurrentRotationActions)
				is IBaseAction { Setting.GCDSingleHeal: true }
			&& (DataCenter.MergedStatus & (AutoStatus.HealAreaSpell | AutoStatus.HealSingleSpell)) == 0;
	}
}
