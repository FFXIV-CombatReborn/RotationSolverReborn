// Shared by the rotations that run the CD plan: where their mitigation comes from, which shared
// Feint / Addle profile they use, and the "target already has it" check.
namespace RotationSolver.Engine.Helpers;

// The [CD Plan] setting's choices, as shown in the settings (also used as ParentValue).
public static class CdPlanNames
{
	public const string BuiltInPlan = "Use built-in CD plan";
	public const string ManualPlan = "BMR / VBM / Manual";
}

public enum CdPlanSource : byte
{
	[Description("Auto")]
	Auto,

	[Description(CdPlanNames.BuiltInPlan)]
	BuiltIn,

	[Description(CdPlanNames.ManualPlan)]
	Manual,
}

// Same order as MitProfileChoice.
public enum FeintProfile : byte
{
	[Description("Auto (M1, then M2 if M1 is taken)")]
	Auto,

	[Description("M1 profile")]
	M1,

	[Description("M2 profile")]
	M2,

	[Description("Job plan")]
	JobPlan,
}

// Same order as MitProfileChoice.
public enum AddleProfile : byte
{
	[Description("Auto (R2, then Fake melee if R2 is taken)")]
	Auto,

	[Description("R2 profile")]
	R2,

	[Description("Fake melee profile (double caster)")]
	FakeMelee,

	[Description("Job plan")]
	JobPlan,
}

public enum MitProfileChoice : byte
{
	Auto,
	First,
	Second,
	JobPlan,
}

internal static class CdPlanHelper
{
	public static bool TargetHas(StatusID status) =>
		DataCenter.HostileTarget is { } target && !target.WillStatusEndGCD(1, 0, false, status);

	public static string? Wait(uint actionId) => actionId switch
	{
		(uint)ActionID.FeintPvE when TargetHas(StatusID.Feint) => "target already has Feint",
		(uint)ActionID.AddlePvE when TargetHas(StatusID.Addle) => "target already has Addle",
		_ => null,
	};

	public static bool HoldCommand(IBaseAction action, bool useTimelinePlan) =>
		Updaters.TimelineUpdater.HoldQueuedPlannedAction(action.ID) ||
		(useTimelinePlan && Wait(action.ID) != null);

	// Tank: the plan's "Tank Stance" row says stance should be on / off now; turn it on or off only if it isn't.
	public static bool TryPlannedStance(IBaseAction stance, IBaseAction release, out IAction? act)
	{
		act = null;
		return Updaters.TimelineUpdater.PlannedTankStance switch
		{
			true => !CustomRotation.HasTankStance && stance.CanUse(out act),
			false => CustomRotation.HasTankStance && release.CanUse(out act),
			_ => false,
		};
	}

	public static bool TryPlanned(out IAction? act) =>
		Updaters.TimelineUpdater.TryPlannedAction(false, out act) && act != null && Wait(act.ID) == null;
}
