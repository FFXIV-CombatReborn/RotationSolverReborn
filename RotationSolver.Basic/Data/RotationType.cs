namespace RotationSolver.Basic.Data;

/// <summary>
/// Which version of a job's kit a rotation is written for.
/// </summary>
public enum RotationType : byte
{
	/// <summary>
	/// The job kits from before Evercold.
	/// </summary>
	Reborn,

	/// <summary>
	/// The Evolved job kits from Evercold onwards.
	/// </summary>
	Evolved,
}
