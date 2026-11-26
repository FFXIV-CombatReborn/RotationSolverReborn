using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using RotationSolver.Commands;

namespace RotationSolver.Updaters;

internal static class MovingUpdater
{
	internal static unsafe void UpdateCanMove(bool doNextAction)
	{
		if (Player.Object == null)
		{
			Service.CanMove = true;
			return;
		}

		if (Svc.Condition?[ConditionFlag.OccupiedInEvent] == true)
		{
			Service.CanMove = true;
			return;
		}

		if (Svc.Condition?[ConditionFlag.Casting] == true)
		{
			Service.CanMove = ActionBasicInfo.ActionsNoNeedCasting.Contains(Player.Object?.CastActionId ?? 0);
			return;
		}

		var cfgFlame = Service.Config?.PosFlameThrower == true;
		var cfgPassage = Service.Config?.PosPassageOfArms == true;
		var cfgImpro = Service.Config?.PosImprovisation == true;

		ActionID action;
		bool checkCastTime;
		if (DateTime.Now - RSCommands._lastUsedTime < TimeSpan.FromMilliseconds(100f))
		{
			action = (ActionID)RSCommands._lastActionID;
			checkCastTime = RSCommands._lastActionIsBaseAction;
		}
		else if (doNextAction)
		{
			action = (ActionID)(ActionUpdater.NextAction?.AdjustedID ?? 0);
			checkCastTime = ActionUpdater.NextAction is IBaseAction;
		}
		else
		{
			action = 0;
			checkCastTime = false;
		}

		var specialActions = (action != 0 && checkCastTime && Array.IndexOf(ActionBasicInfo.ActionsNoNeedCasting, (uint)action) < 0 && ActionManager.GetAdjustedCastTime(ActionType.Action, (uint)action) > 0)
							  || (cfgFlame && Service.GetAdjustedActionId(ActionID.FlameThrowerPvE) == action)
							  || (cfgPassage && Service.GetAdjustedActionId(ActionID.PassageOfArmsPvE) == action)
							  || (cfgImpro && Service.GetAdjustedActionId(ActionID.ImprovisationPvE) == action);

		var specialStatus = (cfgFlame && StatusHelper.PlayerHasStatus(true, StatusID.Flamethrower))
							 || (cfgPassage && StatusHelper.PlayerHasStatus(true, StatusID.PassageOfArms))
							 || (cfgImpro && StatusHelper.PlayerHasStatus(true, StatusID.Improvisation));

		Service.CanMove = !specialStatus && !specialActions;
	}
}