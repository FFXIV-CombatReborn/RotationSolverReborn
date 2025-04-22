using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using Action = Lumina.Excel.Sheets.Action;

namespace RotationSolver.Basic.Helpers;

/// <summary>
/// The helper for the action id.
/// </summary>
public static class ActionIdHelper
{
    /// <summary>
    /// Checks if the action is cooling down.
    /// </summary>
    /// <param name="actionID">The action ID.</param>
    /// <returns>True if the action is cooling down, otherwise false.</returns>
    public unsafe static bool IsCoolingDown(this ActionID actionID)
    {
        var action = actionID.GetAction();
        if (!action.HasValue) return false;

        // Use IsActionOffCooldown to determine if the action is off cooldown
        return !ActionManager.Instance()->IsActionOffCooldown(ActionType.Action, (uint)actionID);
    }

    /// <summary>
    /// Checks if the action is cooling down.
    /// </summary>
    /// <param name="cdGroup">The cooldown group.</param>
    /// <returns>True if the action is cooling down, otherwise false.</returns>
    public unsafe static bool IsCoolingDownGroup(byte cdGroup)
    {
        var detail = GetCoolDownDetail(cdGroup);
        return detail != null && detail->IsActive != 0;
    }

    /// <summary>
    /// Gets the cooldown details.
    /// </summary>
    /// <param name="cdGroup">The cooldown group.</param>
    /// <returns>A pointer to the cooldown details.</returns>
    public static unsafe RecastDetail* GetCoolDownDetail(byte cdGroup)
    {
        var actionManager = ActionManager.Instance();
        if (actionManager == null)
        {
            Svc.Log.Error("ActionManager.Instance() returned null.");
            return null;
        }
        return actionManager->GetRecastGroupDetail(cdGroup - 1);
    }

    /// <summary>
    /// Gets the action associated with the action ID.
    /// </summary>
    /// <param name="actionID">The action ID.</param>
    /// <returns>The action associated with the action ID.</returns>
    private static Action? GetAction(this ActionID actionID)
    {
        return Svc.Data.GetExcelSheet<Action>()?.GetRow((uint)actionID);
    }

    /// <summary>
    /// Gets the cast time of the action.
    /// </summary>
    /// <param name="actionID">The action ID.</param>
    /// <returns>The cast time of the action in seconds.</returns>
    public unsafe static float GetCastTime(this ActionID actionID)
    {
        return ActionManager.GetAdjustedCastTime(ActionType.Action, (uint)actionID) / 1000f;
    }
}