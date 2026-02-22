namespace RotationSolver.Basic.Rotations.Duties;

/// <summary>
/// The variant action.
/// </summary>
[DutyTerritory(826)]
public abstract class OrbonneRotation : DutyRotation
{
}

public partial class DutyRotation
{
	/// <summary>
	///
	/// </summary>
	/// <param name="setting">The action setting to modify.</param>
	static partial void ModifyHeavenlyShieldPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
		setting.StatusNeed = [StatusID.Shieldbearer];
		setting.ActionCheck = () => IsAgriasCastingJudgementBlade;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="setting">The action setting to modify.</param>
	static partial void ModifyHeavenlySwordPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = false;
		// For Heavenly Sword we only want to use it when there are hostiles in the
		// 25° cone in front of the player that have the vulnerability debuffs.
		setting.StatusNeed = [StatusID.Swordbearer];
		setting.ActionCheck = () => HeavenlySwordHasVulnerabilityInCone();
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	private static bool HeavenlySwordHasVulnerabilityInCone()
	{
		var player = Player;
		if (player == null) return false;
		if (DataCenter.AllHostileTargets == null) return false;

		const float range = 6f;
		// 25 degree cone -> half-angle 12.5 degrees
		double halfAngleRad = 12.5 * Math.PI / 180.0;
		double thresholdCos = Math.Cos(halfAngleRad);

		var faceVec = Vector3.Normalize(player.GetFaceVector());

		foreach (var t in DataCenter.AllHostileTargets)
		{
			if (t == null) continue;
			// quick distance check
			var dir = t.Position - player.Position;
			if (dir.LengthSquared() > range * range) continue;

			var ndir = Vector3.Normalize(dir);
			double dot = Vector3.Dot(faceVec, ndir);
			if (dot < thresholdCos) continue;

			// check for the vulnerability statuses
			if (t.HasStatus(false, StatusID.VulnerabilityDown_1782, StatusID.VulnerabilityDown_350))
			{
				return true;
			}
		}

		return false;
	}
}