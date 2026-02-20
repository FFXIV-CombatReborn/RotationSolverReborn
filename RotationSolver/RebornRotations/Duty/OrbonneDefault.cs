using RotationSolver.Basic.Rotations.Duties;

namespace RotationSolver.RebornRotations.Duty;

[Rotation("Orbonne Monastery", CombatType.PvE)]

internal class OrbonneDefault : OrbonneRotation
{
	public override bool GeneralAbility(IAction nextGCD, out IAction? act)
	{
		if (InOrbonne)
		{
			if (HeavenlyShieldPvE.CanUse(out act, checkActionManager: true, usedUp: true, skipAoeCheck: true, skipComboCheck: true, skipCastingCheck: true))
			{
				return true;
			}
		}

		return base.GeneralAbility(nextGCD, out act);
	}

	public override bool GeneralGCD(out IAction? act)
	{
		if (InOrbonne)
		{
			if (HeavenlySwordPvE.CanUse(out act, checkActionManager: true, usedUp: true, skipAoeCheck: true, skipComboCheck: true, skipCastingCheck: true))
			{
				return true;
			}
		}

		return base.GeneralGCD(out act);
	}
}