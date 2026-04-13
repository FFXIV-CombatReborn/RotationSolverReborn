namespace RotationSolver.Basic.Data;

/// <summary>
/// The type of the combat.
/// </summary>
[Flags]
public enum CombatType : byte
{
	/// <summary>
	/// None of them! (Invalid)
	/// </summary>
	None = 0,

	/// <summary>
	/// Only for PvP.
	/// </summary>
	PvP = 1 << 0,

	/// <summary>
	/// Only for PvE.
	/// </summary>
	PvE = 1 << 1,

	/// <summary>
	/// PvP and PvE.
	/// </summary>
	Both = PvP | PvE,
}

/// <summary>
/// Represents the role of a combat participant.
/// </summary>
public enum CombatRole
{
	/// <summary>
	/// 
	/// </summary>
	NonCombat,

	/// <summary>
	/// 
	/// </summary>
	Tank,

	/// <summary>
	/// 
	/// </summary>
	Healer,

	/// <summary>
	/// 
	/// </summary>
	DPS
}

/// <summary>
/// Extension methods for the CombatType enum.
/// </summary>
internal static class CombatTypeExtension
{
	/// <summary>
	/// Gets the icon associated with the combat type.
	/// </summary>
	/// <param name="type">The combat type.</param>
	/// <returns>The icon identifier.</returns>
	public static uint GetIcon(this CombatType type)
	{
		return type switch
		{
			CombatType.Both => 61540u,
			CombatType.PvE => 61542u,
			CombatType.PvP => 61544u,
			_ => 61523u,
		};
	}
}