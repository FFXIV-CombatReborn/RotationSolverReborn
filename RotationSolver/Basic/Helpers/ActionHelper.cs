using Action = Lumina.Excel.Sheets.Action;

namespace RotationSolver.Basic.Helpers;

internal static class ActionHelper
{
	internal const byte GCDCooldownGroup = 58;

	internal const byte AbilityCooldownGroup = 19;

	internal const byte OGCDCooldownGroup = 71;

	/// <summary>
	/// Gets the action category of the specified action.
	/// </summary>
	/// <param name="action">The action to get the category for.</param>
	/// <returns>The action category.</returns>
	public static ActionCate GetActionCate(this Action action)
	{
		return (ActionCate)(action.ActionCategory.IsValid ? action.ActionCategory.Value.RowId : 0);
	}

	internal static bool IsGeneralGCD(this Action action)
	{
		return action.CooldownGroup == GCDCooldownGroup;
	}

	internal static bool IsRealGCD(this Action action)
	{
		return action.CooldownGroup == GCDCooldownGroup || action.AdditionalCooldownGroup == GCDCooldownGroup;
	}

	internal static byte GetCoolDownGroup(this Action action)
	{
		var group = action.CooldownGroup == GCDCooldownGroup ? action.AdditionalCooldownGroup : action.CooldownGroup;
		return group == 0 ? GCDCooldownGroup : group;
	}

	internal static bool IsInJob(this Action action)
	{
		return action.ClassJobCategory.ValueNullable?.DoesJobMatchCategory(DataCenter.Job) ?? true;
	}

	internal static bool CanUseGCD => DataCenter.DefaultGCDRemain <= DataCenter.CalculatedActionAhead;
}