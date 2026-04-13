namespace RotationSolver.Basic.Data;

/// <summary>
/// Hostile target.
/// </summary>
public enum DTRType : byte
{
	/// <summary>
	/// Cycle between first Auto, Manual, and Off
	/// </summary>
	[Description("Cycle between first Auto, Manual, and Off")]
	DTRNormal,

	/// <summary>
	/// Cycle between each Auto, Manual, and Off
	/// </summary>
	[Description("Cycle between each Auto, Manual, and Off")]
	DTRAllAuto,

	/// <summary>
	/// Cycle between Auto and Off
	/// </summary>
	[Description("Cycle between Auto and Off")]
	DTRAuto,

	/// <summary>
	/// Cycle between Manual and Off
	/// </summary>
	[Description("Cycle between Manual and Off")]
	DTRManual,

	/// <summary>
	/// Cycle between Manual and Auto
	/// </summary>
	[Description("Cycle between Manual and Auto")]
	DTRManualAuto,
}