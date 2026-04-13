namespace RotationSolver.Basic.Data;

/// <summary>
/// The category of the status.
/// </summary>
public enum StatusCategory : byte
{
	/// <summary>
	/// No specific category.
	/// </summary>
	None = 0,

	/// <summary>
	/// A beneficial status effect.
	/// </summary>
	Beneficial = 1,

	/// <summary>
	/// A detrimental status effect.
	/// </summary>
	Detrimental = 2,
}