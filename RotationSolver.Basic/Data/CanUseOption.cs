namespace RotationSolver.Basic.Data;

/// <summary>
/// Specifies options for determining whether an action can be used.
/// </summary>
[Flags]
public enum CanUseOption : byte
{
    /// <summary>
    /// No options specified.
    /// </summary>
    None,

    /// <summary>
    /// Skip status provide check.
    /// </summary>
    [Description("Skip status provide check")]
    SkipStatusProvideCheck = 1 << 0,

    /// <summary>
    /// Skip combo check.
    /// </summary>
    [Description("Skip combo check")]
    SkipComboCheck = 1 << 1,

    /// <summary>
    /// Skip casting and moving check.
    /// </summary>
    [Description("Skip casting and moving check")]
    SkipCastingCheck = 1 << 2,

    /// <summary>
    /// Indicates that all stacks should be used up.
    /// </summary>
    [Description("Use up all stacks")]
    UsedUp = 1 << 3,

    /// <summary>
    /// Indicates that the action is the last ability.
    /// </summary>
    [Description("On the last ability")]
    OnLastAbility = 1 << 4,

    /// <summary>
    /// Skip clipping check.
    /// </summary>
    [Description("Skip clipping check")]
    SkipClippingCheck = 1 << 5,

    /// <summary>
    /// Skip AoE check.
    /// </summary>
    [Description("Skip AoE check")]
    SkipAoeCheck = 1 << 6,

	/// <summary>
	/// Overriding targettype.
	/// </summary>
	[Description("Overriding targettype")]
	targetOverride = 1 << 7,
}