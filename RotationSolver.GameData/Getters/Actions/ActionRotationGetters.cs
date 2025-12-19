using Lumina.Excel.Sheets;

namespace RotationSolver.GameData.Getters.Actions;

/// <summary>
/// Gets the single rotation actions for a specific job.
/// </summary>
internal class ActionSingleRotationGetter(Lumina.GameData gameData, ClassJob job)
    : ActionRotationGetterBase(gameData)
{
    /// <summary>
    /// Gets a value indicating whether the action is a duty action.
    /// </summary>
    public override bool IsDutyAction => false;

    /// <summary>
    /// Adds the specified action to the list if it meets the criteria.
    /// </summary>
    /// <param name="item">The action item to check.</param>
    /// <returns>True if the action is added; otherwise, false.</returns>
    protected override bool AddToList(Lumina.Excel.Sheets.Action item)
    {
        if (!base.AddToList(item)) return false;

        if (!item.ClassJobCategory.IsValid) return false;
        var category = item.ClassJobCategory.Value;
        if (!category.IsSingleJobForCombat()) return false;

        var jobName = job.Abbreviation.ToString();
        return (bool?)category.GetType().GetRuntimeProperty(jobName)?.GetValue(category) ?? false;
    }
}

/// <summary>
/// Abstract base class for getting multi-rotation actions.
/// </summary>
internal abstract class ActionMultiRotationGetter(Lumina.GameData gameData)
    : ActionRotationGetterBase(gameData)
{
    /// <summary>
    /// Determines whether the specified action is a duty action.
    /// </summary>
    /// <param name="action">The action to check.</param>
    /// <returns>True if the action is a duty action; otherwise, false.</returns>
    protected static bool IsADutyAction(Lumina.Excel.Sheets.Action action)
    {
        return !action.IsRoleAction && !action.IsPvP && action.ActionCategory.RowId
            is not 10 and not 11 // Not System
            and not 9 and not 15; // Not LB.
    }

    /// <summary>
    /// Adds the specified action to the list if it meets the criteria.
    /// </summary>
    /// <param name="item">The action item to check.</param>
    /// <returns>True if the action is added; otherwise, false.</returns>
    protected override bool AddToList(Lumina.Excel.Sheets.Action item)
    {
        if (!base.AddToList(item)) return false;

        if (!item.ClassJobCategory.IsValid) return false;
        var category = item.ClassJobCategory.Value;
        if (category.IsSingleJobForCombat()) return false;

        return true;
    }
}

/// <summary>
/// Gets the duty rotation actions.
/// </summary>
internal class ActionDutyRotationGetter(Lumina.GameData gameData)
    : ActionMultiRotationGetter(gameData)
{
    /// <summary>
    /// Gets a value indicating whether the action is a duty action.
    /// </summary>
    public override bool IsDutyAction => true;

    /// <summary>
    /// Adds the specified action to the list if it meets the criteria.
    /// </summary>
    /// <param name="item">The action item to check.</param>
    /// <returns>True if the action is added; otherwise, false.</returns>
protected override bool AddToList(Lumina.Excel.Sheets.Action item)
    {
        if (!base.AddToList(item)) return false;
        return IsADutyAction(item);
    }
}

/// <summary>
/// Gets the role rotation actions.
/// </summary>
internal class ActionRoleRotationGetter(Lumina.GameData gameData)
    : ActionMultiRotationGetter(gameData)
{
    /// <summary>
    /// Gets a value indicating whether the action is a duty action.
    /// </summary>
    public override bool IsDutyAction => false;

    /// <summary>
    /// Adds the specified action to the list if it meets the criteria.
    /// </summary>
    /// <param name="item">The action item to check.</param>
    /// <returns>True if the action is added; otherwise, false.</returns>
protected override bool AddToList(Lumina.Excel.Sheets.Action item)
    {
        if (!base.AddToList(item)) return false;
        return !IsADutyAction(item);
    }
}
