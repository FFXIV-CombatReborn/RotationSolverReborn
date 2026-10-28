namespace RotationSolver.Basic.Helpers;

internal static class HealPrediction
{
	internal static bool HasLanded(uint currentHp, ref uint baselineHp, uint healedHp)
	{
		if (currentHp < baselineHp)
		{
			baselineHp = currentHp;
		}

		return currentHp - baselineHp >= healedHp;
	}
}
