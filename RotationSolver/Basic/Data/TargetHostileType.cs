namespace RotationSolver.Basic.Data;

/// <summary>
/// Hostile target.
/// </summary>
public enum TargetHostileType : byte
{
	/// <summary>
	/// All targets that are in range for any abilities (Tanks/Autoduty).
	/// </summary>
	[Description("All targets that are in range for any abilities (Tanks/Autoduty)")]
	AllTargetsCanAttack,

	/// <summary>
	/// Previously engaged targets (Non-Tanks).
	/// </summary>
	[Description("Previously engaged targets (Non-Tanks)")]
	TargetsHaveTarget,

	/// <summary>
	/// All targets when solo in duty, or previously engaged.
	/// </summary>
	[Description("All targets when solo in duty (includes Occult Crescent), or previously engaged.")]
	AllTargetsWhenSoloInDuty,

	/// <summary>
	/// All targets when solo, or previously engaged.
	/// </summary>
	[Description("All targets when solo, or previously engaged.")]
	AllTargetsWhenSolo,

	/// <summary>
	/// Solo Deep Dungeons: out of combat pull the nearest single enemy; in combat only previously engaged.
	/// </summary>
	[Description("Solo Deep Dungeons: if solo, out of combat pull the nearest single enemy; in combat only previously engaged.")]
	SoloDeepDungeonSmart,
}
