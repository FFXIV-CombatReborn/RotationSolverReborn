namespace RotationSolver.Basic.Data;

/// <summary>
/// Hostile target.
/// </summary>
public enum HardCastRaiseType : byte
{
	/// <summary>
	///
	/// </summary>
	[Description("Do not hard cast Raise ")]
	NoHardCast,

	/// <summary>
	///
	/// </summary>
	[Description("Raise while Swiftcast is on cooldown")]
	HardCastNormal,

	/// <summary>
	///
	/// </summary>
	[Description("Raise while Swiftcast is on cooldown and other healers are dead")]
	HardCastOnlyHealer,

	/// <summary>
	///
	/// </summary>
	[Description("Raise while Swiftcast is on cooldown and cooldown is higher than raise cast time")]
	HardCastSwiftCooldown,

	/// <summary>
	/// 
	/// </summary>
	[Description("Raise while Swiftcast is on cooldown and cooldown is higher than raise cast time and other healers are dead")]
	HardCastOnlyHealerSwiftCooldown,
}