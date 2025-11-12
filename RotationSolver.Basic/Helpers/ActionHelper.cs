using ECommons.GameHelpers;
using ECommons.ExcelServices;
using System.Collections.Concurrent;
using Action = Lumina.Excel.Sheets.Action;

namespace RotationSolver.Basic.Helpers;

/// <summary>
/// Provides helper methods for actions.
/// </summary>
internal static class ActionHelper
{
    /// <summary>
    /// The cooldown group for general GCD actions.
    /// </summary>
    internal const byte GCDCooldownGroup = 58;

    internal const byte AbilityCooldownGroup = 19;

    internal const byte OGCDCooldownGroup = 71;

    /// <inheritdoc/>
    public static float AnimationLockTime => Player.AnimationLock;

    /// <summary>
    /// Gets the action category of the specified action.
    /// </summary>
    /// <param name="action">The action to get the category for.</param>
    /// <returns>The action category.</returns>
    public static ActionCate GetActionCate(this Action action)
    {
        return (ActionCate)(action.ActionCategory.IsValid ? action.ActionCategory.Value.RowId : 0);
    }

    /// <summary>
    /// Determines whether the specified action is a general GCD action.
    /// </summary>
    /// <param name="action">The action to check.</param>
    /// <returns><c>true</c> if the action is a general GCD action; otherwise, <c>false</c>.</returns>
    internal static bool IsGeneralGCD(this Action action)
    {
        return action.CooldownGroup == GCDCooldownGroup;
    }

    /// <summary>
    /// Determines whether the specified action is a real GCD action.
    /// </summary>
    /// <param name="action">The action to check.</param>
    /// <returns><c>true</c> if the action is a real GCD action; otherwise, <c>false</c>.</returns>
    internal static bool IsRealGCD(this Action action)
    {
        return action.CooldownGroup == GCDCooldownGroup || action.AdditionalCooldownGroup == GCDCooldownGroup;
    }

    /// <summary>
    /// Gets the cooldown group of the specified action.
    /// </summary>
    /// <param name="action">The action to get the cooldown group for.</param>
    /// <returns>The cooldown group.</returns>
    internal static byte GetCoolDownGroup(this Action action)
    {
        byte group = action.CooldownGroup == GCDCooldownGroup ? action.AdditionalCooldownGroup : action.CooldownGroup;
        return group == 0 ? GCDCooldownGroup : group;
    }

    private static readonly ConcurrentDictionary<Job, PropertyInfo?> JobPropertyCache = new();

    /// <summary>
    /// Determines whether the specified action is in the current job.
    /// </summary>
    /// <param name="action">The action to check.</param>
    /// <returns><c>true</c> if the action is in the current job; otherwise, <c>false</c>.</returns>
    internal static bool IsInJob(this Action action)
    {
        Lumina.Excel.Sheets.ClassJobCategory? cate = action.ClassJobCategory.ValueNullable;
        if (cate != null)
        {
            PropertyInfo? property = JobPropertyCache.GetOrAdd(DataCenter.Job, job =>
            {
                // Cache the property info for this job once
                var t = cate.GetType();
                return t.GetProperty(job.ToString());
            });

            if (property != null)
            {
                object? val = property.GetValue(cate);
                if (val is bool b) return b;
                if (val is byte by) return by != 0;
                return true;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets a value indicating whether a GCD action can be used.
    /// </summary>
    internal static bool CanUseGCD => DataCenter.DefaultGCDRemain <= DataCenter.CalculatedActionAhead;
}