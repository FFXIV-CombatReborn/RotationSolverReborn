namespace RotationSolver.Basic.Helpers;

internal static class HealPrediction
{
	// Damage lowers the baseline, so the heal is measured from the damaged HP.
	internal static bool HasLanded(uint currentHp, ref uint baselineHp, uint healedHp)
	{
		if (currentHp < baselineHp)
		{
			baselineHp = currentHp;
		}

		return currentHp - baselineHp >= healedHp;
	}
}
