namespace RotationSolver.Basic.Actions;

/// <summary>
/// Represents an action that can be performed.
/// </summary>
public interface IAction : ITexture, IEnoughLevel
{
    /// <summary>
    /// Gets the ID of this action.
    /// </summary>
    uint ID { get; }

    /// <summary>
    /// Gets the adjusted ID of this action.
    /// </summary>
    uint AdjustedID { get; }

    /// <summary>
    /// Gets the key used for sorting this action.
    /// </summary>
    uint SortKey { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this action is in the cooldown UI window.
    /// </summary>
    bool IsOnCooldownWindow { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this action is restricted by min HP.
    /// </summary>
    bool MinHPFeature { get; set; }

    /// <summary>
    /// 
    /// </summary>
    float MinHPPercent { get; set; }

    /// <summary>
    /// Gets the cooldown information for this action.
    /// </summary>
    ICooldown Cooldown { get; }

    /// <summary>
    /// Uses the action.
    /// </summary>
    /// <returns>True if the action was successfully used; otherwise, false.</returns>
    bool Use();
}