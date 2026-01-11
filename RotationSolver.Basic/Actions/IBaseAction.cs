using Action = Lumina.Excel.Sheets.Action;

namespace RotationSolver.Basic.Actions;

/// <summary>
/// The interface of the base action.
/// </summary>
public interface IBaseAction : IAction
{
    internal static TargetType? TargetOverride { get; set; } = null;
    internal static bool ForceEnable { get; set; } = false;
    internal static bool AutoHealCheck { get; set; } = false;
    internal static bool ActionPreview { get; set; } = false;
    internal static bool IgnoreClipping { get; set; } = false;
    internal static bool AllEmpty { get; set; } = false;
    internal static bool ShouldEndSpecial { get; set; } = false;

    /// <summary>
    /// The action itself.
    /// </summary>
    Action Action { get; }

    /// <summary>
    /// The target to use on.
    /// </summary>
    TargetResult Target { get; set; }

    /// <summary>
    /// The target for preview.
    /// </summary>
    TargetResult? PreviewTarget { get; }

    /// <summary>
    /// The information about the target.
    /// </summary>
    ActionTargetInfo TargetInfo { get; }

    /// <summary>
    /// The basic information of this action.
    /// </summary>
    ActionBasicInfo Info { get; }

    /// <summary>
    /// The cd information.
    /// </summary>
    new ActionCooldownInfo Cooldown { get; }

    /// <summary>
    /// The setting to use this action.
    /// </summary>
    ActionSetting Setting { get; set; }

    /// <summary>
    /// The current setting for this action.
    /// </summary>
    ActionConfig Config { get; }

	/// <summary>
	/// Can I use this action.
	/// </summary>
	/// <param name="act">The return action</param>
	/// <param name="skipStatusProvideCheck">Skip Status Provide Check</param>
	/// <param name="skipStatusNeed">Skip Status Need Check</param>
	/// <param name="skipTargetStatusNeedCheck">Skip Status Provide Check</param>
	/// <param name="skipComboCheck">Skip Combo Check</param>
	/// <param name="skipCastingCheck">Skip Casting and Moving Check</param>
	/// <param name="usedUp">Is it used up all stacks</param>
	/// <param name="skipAoeCheck">Skip aoe Check</param>
	/// <param name="skipTTKCheck">skip IsTimeToKillValid check in BaseAction</param>
	/// <param name="gcdCountForAbility">the gcd count for the ability.</param>
	/// <param name="checkActionManager">Whether to check the in-game action manager in addition to our own internal logic.</param>
	/// <param name="targetOverride">Overrides the default target type for the action.</param>
	/// <returns>can I use it</returns>
	bool CanUse(out IAction act, bool skipStatusProvideCheck = false, bool skipStatusNeed = false, bool skipTargetStatusNeedCheck = false, bool skipComboCheck = false, bool skipCastingCheck = false,
		bool usedUp = false, bool skipAoeCheck = false, bool skipTTKCheck = false, byte gcdCountForAbility = 0, bool checkActionManager = false, TargetType targetOverride = default);
}
