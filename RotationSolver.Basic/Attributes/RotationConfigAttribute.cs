using static RotationSolver.Basic.Rotations.Duties.DutyRotation;

namespace RotationSolver.Basic.Attributes;

/// <summary>
///
/// </summary>
/// <param name="type"></param>
[AttributeUsage(AttributeTargets.Property)]
public class RotationConfigAttribute(CombatType type) : Attribute
{
    /// <summary>
    /// The type of this config.
    /// </summary>
    public CombatType Type => type;

    /// <summary>
    /// The display name for this config.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent of this config item.
    /// </summary>
    public string Parent { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value that the parent property must have for this config item to be enabled/visible.
    /// </summary>
    public object? ParentValue { get; set; }

    /// <summary>
    /// Phantom Job for this config.
    /// </summary>
    public PhantomJob PhantomJob { get; set; } = PhantomJob.None;

    /// <summary>
    /// Tooltip for this config.
    /// </summary>
    public string Tooltip { get; set; } = string.Empty;
}