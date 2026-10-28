namespace RotationSolver.Basic.Data;

/// <summary>
/// The type of description.
/// </summary>
public enum DescType : byte
{
	/// <summary>
	/// No description.
	/// </summary>
	None,

	/// <summary>
	/// Burst actions.
	/// </summary>
	[Description("Burst Actions")]
	BurstActions,

	/// <summary>
	/// Area heal GCDs.
	/// </summary>
	[Description("Heal Area GCD")]
	HealAreaGCD,

	/// <summary>
	/// Area heal oGCDs.
	/// </summary>
	[Description("Heal Area Ability")]
	HealAreaAbility,

	/// <summary>
	/// Single target heal GCDs.
	/// </summary>
	[Description("Heal Single GCD")]
	HealSingleGCD,

	/// <summary>
	/// Single target heal oGCDs.
	/// </summary>
	[Description("Heal Single Ability")]
	HealSingleAbility,

	/// <summary>
	/// Area defensive GCDs (shields, mitigation, etc).
	/// </summary>
	[Description("Defense Area GCD")]
	DefenseAreaGCD,

	/// <summary>
	/// Area defensive oGCDs (shields, mitigation, etc).
	/// </summary>
	[Description("Defense Area Ability")]
	DefenseAreaAbility,

	/// <summary>
	/// Single target defensive GCDs (shields, mitigation, etc).
	/// </summary>
	[Description("Defense Single GCD")]
	DefenseSingleGCD,

	/// <summary>
	/// Single target defensive oGCDs (shields, mitigation, etc).
	/// </summary>
	[Description("Defense Single Ability")]
	DefenseSingleAbility,

	/// <summary>
	/// Move forward GCD.
	/// </summary>
	[Description("Move Forward GCD")]
	MoveForwardGCD,

	/// <summary>
	/// Move forward ability.
	/// </summary>
	[Description("Move Forward Ability")]
	MoveForwardAbility,

	/// <summary>
	/// Move back ability.
	/// </summary>
	[Description("Move Back Ability")]
	MoveBackAbility,

	/// <summary>
	/// Speed ability.
	/// </summary>
	[Description("Speed Ability")]
	SpeedAbility,
}