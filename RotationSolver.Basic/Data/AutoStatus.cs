namespace RotationSolver.Basic.Data;

/// <summary>
/// The status of auto.
/// </summary>
[Flags]
public enum AutoStatus : uint
{
	/// <summary>
	/// Nothing.
	/// </summary>
	None = 0,

	/// <summary>
	/// We should use interrupt.
	/// </summary>
	Interrupt = 1 << 0,

	/// <summary>
	/// We should use tank stance.
	/// </summary>
	TankStance = 1 << 1,

	/// <summary>
	/// We should provoke some enemy.
	/// </summary>
	Provoke = 1 << 2,

	/// <summary>
	/// We should dispel.
	/// </summary>
	Dispel = 1 << 3,

	/// <summary>
	/// We should use defense single.
	/// </summary>
	DefenseSingle = 1 << 4,

	/// <summary>
	/// We should use defense area.
	/// </summary>
	DefenseArea = 1 << 5,

	/// <summary>
	/// We should heal area by ability.
	/// </summary>
	HealAreaAbility = 1 << 6,

	/// <summary>
	/// We should heal area by spell.
	/// </summary>
	HealAreaSpell = 1 << 7,

	/// <summary>
	/// We should heal single by ability.
	/// </summary>
	HealSingleAbility = 1 << 8,

	/// <summary>
	/// We should heal single by spell.
	/// </summary>
	HealSingleSpell = 1 << 9,

	/// <summary>
	/// We should raise.
	/// </summary>
	Raise = 1 << 10,

	/// <summary>
	/// We should use positional abilities.
	/// </summary>
	Positional = 1 << 11,

	/// <summary>
	/// We should shirk.
	/// </summary>
	Shirk = 1 << 12,

	/// <summary>
	/// We should move forward.
	/// </summary>
	MoveForward = 1 << 13,

	/// <summary>
	/// We should move back.
	/// </summary>
	MoveBack = 1 << 14,

	/// <summary>
	/// We should use anti-knockback abilities.
	/// </summary>
	AntiKnockback = 1 << 15,

	/// <summary>
	/// We should burst.
	/// </summary>
	Burst = 1 << 16,

	/// <summary>
	/// We should use speed abilities.
	/// </summary>
	Speed = 1 << 17,

	/// <summary>
	/// Stop taking actions.
	/// </summary>
	NoCasting = 1 << 18,

	/// <summary>
	/// Intercepting indicator.
	/// </summary>
	Intercepting = 1 << 19,
}