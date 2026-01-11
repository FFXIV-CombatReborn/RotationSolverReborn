namespace RotationSolver.Basic.Data;

/// <summary>
/// Specifies who to raise.
/// </summary>
public enum RaiseType : byte
{
    /// <summary>
    /// Raise only party members.
    /// </summary>
    [Description("Raise only party members.")]
    PartyOnly,

    /// <summary>
    /// Raise party members and alliance supports.
    /// </summary>
    [Description("Raise party members and alliance supports.")]
    PartyAndAllianceSupports,

    /// <summary>
    /// Raise party members and alliance healers.
    /// </summary>
    [Description("Raise party members and alliance healers.")]
    PartyAndAllianceHealers,

    /// <summary>
    /// Raise All In Duty.
    /// </summary>
    [Description("Raise All In Duty.")]
    All,

    /// <summary>
    /// Raise all.
    /// </summary>
    [Description("Raise All.")]
    AllOutOfDuty,

	/// <summary>
	/// Raise all.
	/// </summary>
	[Description("Raise only party healers.")]
	PartyHealersOnly,
}