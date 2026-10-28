namespace RotationSolver.Basic.Data;

/// <summary>
/// Hostile target.
/// </summary>
public enum CycleType : byte
{
	/// <summary>
	/// Cycle between first Auto, Manual, and Off
	/// </summary>
	[Description("Cycle between first Auto, Manual, and Off")]
	CycleNormal,

	/// <summary>
	/// Cycle between each Auto, Manual, and Off
	/// </summary>
	[Description("Cycle between each Auto, Manual, and Off")]
	CycleAllAuto,

	/// <summary>
	/// Cycle between Auto and Off
	/// </summary>
	[Description("Cycle between Auto and Off")]
	CycleAuto,

	/// <summary>
	/// Cycle between Manual and Off
	/// </summary>
	[Description("Cycle between Manual and Off")]
	CycleManual,

	/// <summary>
	/// Cycle between Manual and Auto
	/// </summary>
	[Description("Cycle between Manual and Auto")]
	CycleManualAuto,
}