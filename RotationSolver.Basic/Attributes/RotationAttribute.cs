namespace RotationSolver.Basic.Attributes;

/// <summary>
/// Your custom rotation attribute.
/// </summary>
/// <param name="name">the name of this rotation.</param>
/// <param name="type">the type of this rotation.</param>
[AttributeUsage(AttributeTargets.Class)]
public class RotationAttribute(string name, CombatType type) : Attribute
{
    /// <summary>
    /// The name of this rotation.
    /// </summary>
    public string Name => name;

    /// <summary>
    /// The type of this rotation.
    /// </summary>
    public CombatType Type => type;

    /// <summary>
    /// Your description about this rotation.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The Game version of this rotation.
    /// </summary>
    public string? GameVersion { get; set; }

    /// <summary>
    /// If true the rotation will be treated as disabled and not loaded.
    /// </summary>
    public bool Disabled { get; set; } = false;
}
