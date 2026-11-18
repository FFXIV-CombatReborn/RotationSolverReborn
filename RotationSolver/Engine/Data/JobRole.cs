using ECommons.ExcelServices;
using Lumina.Excel.Sheets;

namespace RotationSolver.Engine.Data;

/// <summary>
/// The role of jobs.
/// </summary>
public enum JobRole : byte
{
	/// <summary>
	/// No specific role.
	/// </summary>
	None = 0,

	/// <summary>
	/// Tank role.
	/// </summary>
	Tank = 1,

	/// <summary>
	/// Melee role.
	/// </summary>
	Melee = 2,

	// Uncomment and document if needed in the future.
	///// <summary>
	///// Ranged role.
	///// </summary>
	//Ranged = 3,

	/// <summary>
	/// Healer role.
	/// </summary>
	Healer = 4,

	/// <summary>
	/// Ranged physical role.
	/// </summary>
	RangedPhysical = 5,

	/// <summary>
	/// Ranged magical role.
	/// </summary>
	RangedMagical = 6,

	/// <summary>
	/// Disciple of the Land role.
	/// </summary>
	DiscipleOfTheLand = 7,

	/// <summary>
	/// Disciple of the Hand role.
	/// </summary>
	DiscipleOfTheHand = 8,

	/// <summary>
	/// All DPS Roles
	/// </summary>
	AllDPS = 9
}

/// <summary>
/// The extension of the job.
/// </summary>
public static class JobRoleExtension
{
	/// <summary>
	/// Gets the job role from a class.
	/// </summary>
	/// <param name="job">The class job.</param>
	/// <returns>The job role.</returns>
	public static JobRole GetJobRole(this ClassJob job)
	{
		var role = (JobRole)job.Role;

		if (role is (JobRole)3 or JobRole.None)
		{
			role = job.ClassJobCategory.RowId switch
			{
				30 => JobRole.RangedPhysical,
				31 => JobRole.RangedMagical,
				32 => JobRole.DiscipleOfTheLand,
				33 => JobRole.DiscipleOfTheHand,
				_ => JobRole.None,
			};
		}
		return role;
	}

	/// <summary>
	/// Gets the jobs from a role.
	/// </summary>
	/// <param name="role">The job role.</param>
	/// <returns>An array of jobs corresponding to the role.</returns>
	public static Job[] ToJobs(this JobRole role)
	{
		return role switch
		{
			JobRole.Tank => TankJobs,
			JobRole.Healer => HealerJobs,
			JobRole.Melee => MeleeJobs,
			JobRole.RangedPhysical => RangedPhysicalJobs,
			JobRole.RangedMagical => RangedMagicalJobs,
			JobRole.AllDPS => AllDpsJobs,
			_ => [],
		};
	}

	private static readonly Job[] TankJobs = [Job.WAR, Job.PLD, Job.DRK, Job.GNB, Job.GLA, Job.MRD];
	private static readonly Job[] HealerJobs = [Job.WHM, Job.SCH, Job.AST, Job.SGE, Job.CNJ];
	private static readonly Job[] MeleeJobs = [Job.MNK, Job.DRG, Job.NIN, Job.SAM, Job.RPR, Job.VPR, Job.PGL, Job.LNC, Job.ROG];
	private static readonly Job[] RangedPhysicalJobs = [Job.BRD, Job.MCH, Job.DNC, Job.ARC];
	private static readonly Job[] RangedMagicalJobs = [Job.BLM, Job.SMN, Job.RDM, Job.BLU, Job.PCT, Job.ACN, Job.THM];
	private static readonly Job[] AllDpsJobs = [.. MeleeJobs, .. RangedPhysicalJobs, .. RangedMagicalJobs];
}