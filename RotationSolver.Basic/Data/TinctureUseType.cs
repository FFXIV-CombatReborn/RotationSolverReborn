namespace RotationSolver.Basic.Data;

/// <summary>
/// Where to use tinctures.
/// </summary>
public enum TinctureUseType : byte
{
	/// <summary>
	/// Do not use tinctures.
	/// </summary>
	[Description("Do not use Gemdraughts/Tinctures/Pots")]
	Nowhere,

	/// <summary>
	/// Only use tinctures in high-end duties.
	/// </summary>
	[Description("Use Gemdraughts/Tinctures/Pots in High-End Duties")]
	InHighEndDuty,

	/// <summary>
	/// Use tinctures anywhere.
	/// </summary>
	[Description("Use Gemdraughts/Tinctures/Pots Anywhere")]
	Anywhere,
}