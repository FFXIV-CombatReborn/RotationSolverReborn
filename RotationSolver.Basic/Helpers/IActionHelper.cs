namespace RotationSolver.Basic.Helpers;

/// <summary>
/// Helper about actions.
/// </summary>
public static class IActionHelper
{
    internal static ActionID[] MovingActions { get; } =
    [
        ActionID.EnAvantPvE,
        //ActionID.PlungePvE,
        ActionID.RoughDividePvE,
        ActionID.ThunderclapPvE,
        ActionID.ShukuchiPvE,
        ActionID.IntervenePvE,
        ActionID.CorpsacorpsPvE,
        ActionID.HellsIngressPvE,
        ActionID.HissatsuGyotenPvE,
        ActionID.IcarusPvE,
        ActionID.OnslaughtPvE,
        //ActionID.SpineshatterDivePvE,
        ActionID.DragonfireDivePvE,
    ];
    
    internal static ActionID[] HealingActions { get; } =
    [
        // AST
        ActionID.BeneficIiPvE,
        ActionID.BeneficPvE,
        ActionID.BeneficPvE_21608,
        ActionID.HeliosConjunctionPvE,
        ActionID.HeliosPvE,
        ActionID.AspectedHeliosPvE,
            
        // SGE
        ActionID.DiagnosisPvE,
        ActionID.DiagnosisPvE_26224,
        ActionID.PrognosisPvE,
        ActionID.PrognosisPvE_27043,
        ActionID.PneumaPvE,
        ActionID.PneumaPvE,
            
        // WHM
        ActionID.CurePvE,
        ActionID.CureIiPvE,
        ActionID.CureIiPvE_21886,
        ActionID.MedicaPvE,
        ActionID.MedicaIiPvE,
        ActionID.MedicaIiPvE_21888,
        ActionID.MedicaIiiPvE,
        ActionID.CureIiiPvE,
            
        // SCH
        ActionID.AdloquiumPvE,
        ActionID.SuccorPvE,
        ActionID.ConcitationPvE,
        ActionID.PhysickPvE,
        ActionID.PhysickPvE_11192,
        ActionID.PhysickPvE_16230
    ];

    /// <summary>
    /// Determines if the last GCD action matches any of the provided actions.
    /// </summary>
    /// <param name="isAdjust">Whether to use the adjusted ID.</param>
    /// <param name="actions">The actions to check against.</param>
    /// <returns>True if the last GCD action matches any of the provided actions, otherwise false.</returns>
    internal static bool IsLastGCD(bool isAdjust, params IAction[] actions)
    {
        return actions != null && IsLastGCD(GetIDFromActions(isAdjust, actions));
    }

    /// <summary>
    /// Determines if the last GCD action matches any of the provided action IDs.
    /// </summary>
    /// <param name="ids">The action IDs to check against.</param>
    /// <returns>True if the last GCD action matches any of the provided action IDs, otherwise false.</returns>
    internal static bool IsLastGCD(params ActionID[] ids)
    {
        return IsActionID(DataCenter.LastGCD, ids);
    }

    /// <summary>
    /// Determines if the last ability matches any of the provided actions.
    /// </summary>
    /// <param name="isAdjust">Whether to use the adjusted ID.</param>
    /// <param name="actions">The actions to check against.</param>
    /// <returns>True if the last ability matches any of the provided actions, otherwise false.</returns>
    internal static bool IsLastAbility(bool isAdjust, params IAction[] actions)
    {
        return actions != null && IsLastAbility(GetIDFromActions(isAdjust, actions));
    }

    /// <summary>
    /// Determines if the last ability matches any of the provided action IDs.
    /// </summary>
    /// <param name="ids">The action IDs to check against.</param>
    /// <returns>True if the last ability matches any of the provided action IDs, otherwise false.</returns>
    internal static bool IsLastAbility(params ActionID[] ids)
    {
        return IsActionID(DataCenter.LastAbility, ids);
    }

    /// <summary>
    /// Determines if the last action matches any of the provided actions.
    /// </summary>
    /// <param name="isAdjust">Whether to use the adjusted ID.</param>
    /// <param name="actions">The actions to check against.</param>
    /// <returns>True if the last action matches any of the provided actions, otherwise false.</returns>
    internal static bool IsLastAction(bool isAdjust, params IAction[] actions)
    {
        return actions != null && IsLastAction(GetIDFromActions(isAdjust, actions));
    }

    /// <summary>
    /// Determines if the last action matches any of the provided action IDs.
    /// </summary>
    /// <param name="ids">The action IDs to check against.</param>
    /// <returns>True if the last action matches any of the provided action IDs, otherwise false.</returns>
    internal static bool IsLastAction(params ActionID[] ids)
    {
        return IsActionID(DataCenter.LastAction, ids);
    }

    /// <summary>
    /// Determines if the last action was a GCD.
    /// </summary>
    /// <returns>True if the last action was a GCD, otherwise false.</returns>
    public static bool IsLastActionGCD()
    {
        return DataCenter.LastAction == DataCenter.LastGCD;
    }

    /// <summary>
    /// Determines if the last action was an ability.
    /// </summary>
    /// <returns>True if the last action was an ability, otherwise false.</returns>
    public static bool IsLastActionAbility()
    {
        return DataCenter.LastAction == DataCenter.LastAbility;
    }

    /// <summary>
    /// Determines if the action is the same as any of the provided actions.
    /// </summary>
    /// <param name="action">The action to check.</param>
    /// <param name="isAdjust">Whether to use the adjusted ID.</param>
    /// <param name="actions">The actions to check against.</param>
    /// <returns>True if the action is the same as any of the provided actions, otherwise false.</returns>
    public static bool IsTheSameTo(this IAction action, bool isAdjust, params IAction[] actions)
    {
        return actions != null && action.IsTheSameTo(isAdjust, GetIDFromActions(isAdjust, actions));
    }

    /// <summary>
    /// Determines if the action is the same as any of the provided action IDs.
    /// </summary>
    /// <param name="action">The action to check.</param>
    /// <param name="isAdjust">Whether to use the adjusted ID.</param>
    /// <param name="actions">The action IDs to check against.</param>
    /// <returns>True if the action is the same as any of the provided action IDs, otherwise false.</returns>
    public static bool IsTheSameTo(this IAction action, bool isAdjust, params ActionID[] actions)
    {
        return action != null && actions != null && IsActionID(isAdjust ? (ActionID)action.AdjustedID : (ActionID)action.ID, actions);
    }

    /// <summary>
    /// Searches the provided list of lists for an action ID.
    /// </summary>
    /// <param name="id">The action ID</param>
    /// <param name="isAdjust">Whether to use the AdjustedID parameter</param>
    /// <param name="lists">The list of lists of actions to search from.</param>
    /// <returns></returns>
    public static IAction? GetActionFromID(this ActionID id, bool isAdjust, params IAction[][] lists)
    {
        foreach (var list in lists)
        {
            foreach (var action in list)
            {
                if ((isAdjust && action.AdjustedID == (uint)id) ||
                    (! isAdjust && action.ID == (uint)id)) return action;
            }
        }

        return null;
    }

    /// <summary>
    /// Determines if the action ID matches any of the provided action IDs.
    /// </summary>
    /// <param name="id">The action ID to check.</param>
    /// <param name="ids">The action IDs to check against.</param>
    /// <returns>True if the action ID matches any of the provided action IDs, otherwise false.</returns>
    private static bool IsActionID(ActionID id, params ActionID[] ids)
    {
        if (ids == null)
        {
            return false;
        }

        for (int i = 0; i < ids.Length; i++)
        {
            if (ids[i].Equals(id))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets the action IDs from the provided actions.
    /// </summary>
    /// <param name="isAdjust">Whether to use the adjusted ID.</param>
    /// <param name="actions">The actions to get the IDs from.</param>
    /// <returns>An array of action IDs.</returns>
    private static ActionID[] GetIDFromActions(bool isAdjust, params IAction[] actions)
    {
        if (actions == null)
        {
            return [];
        }

        ActionID[] result = new ActionID[actions.Length];
        for (int i = 0; i < actions.Length; i++)
        {
            result[i] = isAdjust ? (ActionID)actions[i].AdjustedID : (ActionID)actions[i].ID;
        }
        return result;
    }

    /// <summary>
    /// Determines if the last combo action matches any of the provided actions.
    /// </summary>
    /// <param name="isAdjust">Whether to use the adjusted ID.</param>
    /// <param name="actions">The actions to check against.</param>
    /// <returns>True if the last combo action matches any of the provided actions, otherwise false.</returns>
    internal static bool IsLastComboAction(bool isAdjust, params IAction[] actions)
    {
        return actions != null && IsLastComboAction(GetIDFromActions(isAdjust, actions));
    }

    /// <summary>
    /// Determines if the last combo action matches any of the provided action IDs.
    /// </summary>
    /// <param name="ids">The action IDs to check against.</param>
    /// <returns>True if the last combo action matches any of the provided action IDs, otherwise false.</returns>
    internal static bool IsLastComboAction(params ActionID[] ids)
    {
        return IsActionID(DataCenter.LastComboAction, ids);
    }

    /// <summary>
    /// Determines if the last action was a combo action.
    /// </summary>
    /// <returns>True if the last action was a combo action, otherwise false.</returns>
    public static bool IsLastActionCombo()
    {
        return DataCenter.LastAction == DataCenter.LastComboAction;
    }

    /// <summary>
    ///
    /// </summary>
    public static bool IsNoActionCombo()
    {
        return DataCenter.LastComboAction == ActionID.None;
    }
}