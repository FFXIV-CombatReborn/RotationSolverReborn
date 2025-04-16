using ECommons.ExcelServices;
using Lumina.Excel.Sheets;

namespace RotationSolver.Basic.Data;

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
                // Uncomment and document if needed in the future.
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
            JobRole.Tank => [Job.WAR, Job.PLD, Job.DRK, Job.GNB],
            JobRole.Healer => [Job.WHM, Job.SCH, Job.AST, Job.SGE],
            JobRole.Melee => [Job.MNK, Job.DRG, Job.NIN, Job.SAM, Job.RPR, Job.VPR],
            JobRole.RangedPhysical => [Job.BRD, Job.MCH, Job.DNC],
            JobRole.RangedMagical => [Job.BLM, Job.SMN, Job.RDM, Job.BLU, Job.PCT],
            JobRole.AllDPS => JobRole.Melee.ToJobs()
                .Concat(JobRole.RangedPhysical.ToJobs())
                .Concat(JobRole.RangedMagical.ToJobs())
                .ToArray(),
            _ => [],
        };
    }
}