using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using RotationSolver.Helpers;

namespace RotationSolver.Updaters;

internal static class CancelCastUpdater
{
	private static readonly RandomDelay _tarStopCastDelay = new(() => Service.Config.StopCastingDelay);

	internal static unsafe void UpdateCancelCast()
	{
		var player = Player.Object;
		if (player == null || !player.IsCasting)
		{
			return;
		}

		if (!DataCenter.State)
		{
			return;
		}

		var castTarget = Svc.Objects.SearchById(player.CastTargetObjectId) as IBattleChara;

		var tarDead = Service.Config.UseStopCasting
			&& castTarget != null
			&& castTarget.IsEnemy()
			&& castTarget.CurrentHp == 0;

		// Evaluate the delay every frame so its timer tracks the target's state.
		var stopForDeadTarget = _tarStopCastDelay.Delay(tarDead);

		// Cancel raise cast if target already has Raise status
		var tarHasRaise = castTarget != null && castTarget.HasStatus(false, StatusID.Raise);

		// Cancel immediately if the player currently has any active NoCastingStatus
		var hasNoCastingStatus = NoCastingStatusHelper.PlayerHasNoCastingStatus(out _);

		// Cancel cast in PvP if an enemy target gains Guard and the action does not ignore Guard
		var tarHasGuard = DataCenter.IsPvP && Service.Config.PvpGuardCancel
			&& castTarget != null
			&& castTarget.IsEnemy()
			&& castTarget.HasStatus(false, StatusID.Guard)
			&& ((ActionID)player.CastActionId).GetActionFromID(true, RotationUpdater.CurrentRotationActions)
				is IBaseAction { Setting.IgnoreGuard: false };

		// Cancel the cast if BossMod's own AI hints (module or AI controller) are requesting a cancel,
		// e.g. because the boss module determined the cast is no longer safe/useful.
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
