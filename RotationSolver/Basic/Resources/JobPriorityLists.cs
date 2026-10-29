using ECommons.ExcelServices;

namespace RotationSolver.Basic.Resources;

internal static class JobPriorityLists
{
	public static readonly Job[] DancePartnerPriority = [Job.SAM, Job.PCT, Job.RPR, Job.VPR, Job.MNK, Job.NIN, Job.DRG, Job.BLM, Job.RDM, Job.SMN, Job.MCH, Job.BRD, Job.DNC];

	public static readonly Job[] TheSpearPriority = [Job.PCT, Job.RDM, Job.SMN, Job.MCH, Job.BLM, Job.DNC, Job.BRD, Job.SAM, Job.NIN, Job.DRG, Job.MNK, Job.VPR, Job.DRK, Job.RPR];

	public static readonly Job[] TheBalancePriority = [Job.SAM, Job.NIN, Job.DRG, Job.MNK, Job.VPR, Job.RPR, Job.DRK, Job.PCT, Job.SMN, Job.RDM, Job.MCH, Job.BLM, Job.DNC, Job.BRD];

	public static readonly Job[] KardiaTankPriority = [Job.PLD, Job.WAR, Job.GNB, Job.DRK, Job.GLA, Job.MRD];
}
