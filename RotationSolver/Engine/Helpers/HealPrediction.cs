namespace RotationSolver.Engine.Helpers;

internal static class HealPrediction
{
	internal static bool HasLanded(uint currentHp, uint maxHp, ref uint baselineHp, uint healedHp)
	{
		if (currentHp < baselineHp)
		{
			baselineHp = currentHp;
		}

		if (maxHp > 0 && currentHp >= maxHp)
		{
			return true;
		}

		var expectedGain = maxHp > baselineHp ? Math.Min(healedHp, maxHp - baselineHp) : healedHp;
		return currentHp - baselineHp >= expectedGain;
	}
}
