using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;

namespace RotationSolver.Basic.Helpers;

internal static class HealingHelper
{
	internal const float EmergencyHealthRatio = 0.3f;
	internal const float SingleHealRange = 30;

	internal static bool NeedsEmergencyHealing(this IBattleChara target)
	{
		if (target == null || !target.IsValid() || target.IsDead || target.MaxHp == 0
			|| target.CurrentHp == 0 || target.CurrentHp >= target.MaxHp || !target.CanBeHealed())
		{
			return false;
		}

		var threshold = EmergencyHealthRatio;
		if (target.GameObjectId == Player.Object?.GameObjectId)
		{
			threshold = Math.Max(threshold, Service.Config.HealthSelfRatio);
		}
		if (target.IsJobCategory(JobRole.Tank))
		{
			threshold = Math.Max(threshold, Service.Config.HealthTankRatio);
		}
		else if (target.IsJobCategory(JobRole.Healer))
		{
			threshold = Math.Max(threshold, Service.Config.HealthHealerRatio);
		}

		return (float)target.CurrentHp / target.MaxHp <= threshold;
	}

	internal static IBattleChara? FindEmergencyHealingTarget(IEnumerable<IBattleChara> targets, bool requireLineOfSight = false)
	{
		IBattleChara? emergency = null;
		var lowestHp = float.MaxValue;
		foreach (var target in targets)
		{
			if (!target.NeedsEmergencyHealing() || (requireLineOfSight && !target.CanSee()))
			{
				continue;
			}

			var hp = (float)target.CurrentHp / target.MaxHp;
			if (hp < lowestHp)
			{
				emergency = target;
				lowestHp = hp;
			}
		}

		return emergency;
	}

	internal static bool CoversEmergencyHealingTarget(IEnumerable<IBattleChara> affectedTargets, IBattleChara? emergency)
	{
		if (emergency == null)
		{
			return true;
		}
		foreach (var target in affectedTargets)
		{
			if (target.GameObjectId == emergency.GameObjectId)
			{
				return true;
			}
		}
		return false;
	}
}
