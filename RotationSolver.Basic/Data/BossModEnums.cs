namespace RotationSolver.Basic.Data;

/// <summary>
/// Defines special combat modes that restrict or alter player actions and movement
/// in response to specific mechanic effects.
/// </summary>
public enum SpecialMode
{
	/// <summary>
	/// No special restrictions are active. The player may move and act freely.
	/// </summary>
	Normal = 0,

	/// <summary>
	/// Pyretic or acceleration-bomb type effect is active. At activation time,
	/// no movement, no actions, and no casting are allowed.
	/// </summary>
	Pyretic = 1,

	/// <summary>
	/// A no-movement effect is active. The player must remain stationary.
	/// </summary>
	NoMovement = 2,

	/// <summary>
	/// A freezing effect is active. The player is expected to be moving at activation time.
	/// </summary>
	Freezing = 4,

	/// <summary>
	/// A temporary misdirection effect is active, altering the player's movement direction.
	/// </summary>
	Misdirection = 5,
}

/// <summary>
/// Describes the type of incoming damage predicted by BossModReborn,
/// used to determine appropriate mitigation or response actions.
/// </summary>
public enum PredictedDamageType
{
	/// <summary>
	/// No incoming damage is predicted.
	/// </summary>
	None,

	/// <summary>
	/// A tankbuster attack is incoming, targeting one or more tanks.
	/// </summary>
	Tankbuster,

	/// <summary>
	/// A raidwide attack is incoming, hitting all party members.
	/// </summary>
	Raidwide,

	/// <summary>
	/// A shared damage attack is incoming, requiring multiple players to stack
	/// in order to split the damage.
	/// </summary>
	Shared
}
