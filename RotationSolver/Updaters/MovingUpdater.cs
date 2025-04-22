using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using RotationSolver.Commands;

namespace RotationSolver.Updaters;

internal static class MovingUpdater
{
    internal unsafe static void UpdateCanMove(bool doNextAction)
    {
        // Special state.
        if (Svc.Condition?[ConditionFlag.OccupiedInEvent] == true)
        {
            Service.CanMove = true;
            return;
        }

        // Casting the action in list.
        if (Svc.Condition?[ConditionFlag.Casting] == true && Player.Available == true)
        {
            Service.CanMove = ActionBasicInfo.ActionsNoNeedCasting.Contains(Player.Object?.CastActionId ?? 0);
            return;
        }

        // Special actions.
        var statusList = new List<StatusID>(4);
        var actionList = new List<ActionID>(4);

        if (Service.Config?.PosFlameThrower == true)
        {
            statusList.Add(StatusID.Flamethrower);
            actionList.Add(ActionID.FlameThrowerPvE);
        }
        if (Service.Config?.PosPassageOfArms == true)
        {
            statusList.Add(StatusID.PassageOfArms);
            actionList.Add(ActionID.PassageOfArmsPvE);
        }
        if (Service.Config?.PosImprovisation == true)
        {
            statusList.Add(StatusID.Improvisation);
            actionList.Add(ActionID.ImprovisationPvE);
        }

        // Action
        var action = DateTime.Now - RSCommands._lastUsedTime < TimeSpan.FromMilliseconds(100)
            ? (ActionID)RSCommands._lastActionID
            : doNextAction ? (ActionID)(ActionUpdater.NextAction?.AdjustedID ?? 0) : 0;

        bool specialActions = ActionManager.GetAdjustedCastTime(ActionType.Action, (uint)action) > 0;
        foreach (var id in actionList)
        {
            if (Service.GetAdjustedActionId(id) == action)
            {
                specialActions = true;
                break;
            }
        }

        // Status
        bool specialStatus = false;
        foreach (var status in statusList)
        {
            if (Player.Object?.HasStatus(true, status) == true)
            {
                specialStatus = true;
                break;
            }
        }

        Service.CanMove = !specialStatus && !specialActions;
    }
}