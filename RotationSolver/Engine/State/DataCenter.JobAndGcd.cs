using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using RotationSolver.Rotations.Limited.BlueMage;

namespace RotationSolver.Engine.State;

internal static partial class DataCenter
{
	#region Job Info
	public static Job Job => Player.Job;

	public static RotationType PlayerRotationType => RotationType.Reborn;

	// TODO: add the two Evercold jobs once they exist in ecommons.
	private static readonly HashSet<Job> EvolvedOnlyJobs = [];

	public static bool IsRotationTypeAllowed(Job job, RotationType type)
	{
		if (job is Job.BLU or Job.BST)
		{
			return true;
		}

		return type == (EvolvedOnlyJobs.Contains(job) ? RotationType.Evolved : PlayerRotationType);
	}

	private static readonly BaseItem PhoenixDownItem = new(4570);
	public static bool CanRaise()
	{
		if (IsPvP)
		{
			return false;
		}

		if (Service.Config.UsePhoenixDown && PhoenixDownItem.HasIt)
		{
			return true;
		}

		if ((Role == JobRole.Healer || Job == Job.SMN) && PlayerSyncedLevel() >= 12)
		{
			return true;
		}

		if (Job == Job.RDM && PlayerSyncedLevel() >= 64)
		{
			return true;
		}

		if (DutyRotation.ChemistLevel >= 3)
		{
			return true;
		}

		if (DutyRotation.WhiteMageLevel >= 4)
		{
			return true;
		}

		if (StatusHelper.PlayerHasStatus(false, StatusID.VariantRaiseSet))
		{
			return true;
		}
		return false;
	}

	private static Job _roleJob = Job.ADV;
	private static JobRole _role = JobRole.None;

	public static JobRole Role
	{
		get
		{
			if (CurrentRotation is BlueMageRotation)
			{
				return BlueMageRotation.Role;
			}

			var job = Job;
			if (job != _roleJob || job == Job.ADV)
			{
				var classJob = Service.GetSheet<ClassJob>().GetRowOrDefault((uint)job);
				_role = classJob is { RowId: not 0 } row ? row.GetJobRole() : JobRole.None;
				_roleJob = job;
			}

			return _role;
		}
	}

	public static float JobRange
	{
		get
		{
			float radius = 25;
			if (!Player.Available)
			{
				return radius;
			}

			switch (Role)
			{
				case JobRole.Tank:
				case JobRole.Melee:
					radius = IsPvP ? 5 : 3;
					break;
			}

			return radius;
		}
	}

	public static unsafe bool SylphManagementFinished()
	{
		if (UIState.Instance()->IsUnlockLinkUnlockedOrQuestCompleted(66049))
		{
			return true;
		}

		return false;
	}

	public static bool BaseClass()
	{
		if (Svc.Objects.LocalPlayer == null)
		{
			return false;
		}

		var rowId = Svc.Objects.LocalPlayer.ClassJob.RowId;
		return (rowId >= 1 && rowId <= 7) || rowId == 26 || rowId == 29;
	}
	#endregion

	#region GCD
	/// <summary>
	/// Returns the current animation lock remaining time (seconds).
	/// </summary>
	public static float AnimationLock => Player.AnimationLock;

	/// <summary>
	/// Time until the next ability relative to the next GCD window.
	/// Non-negative (clamped to 0).
	/// </summary>
	public static float NextAbilityToNextGCD => Math.Max(0f, DefaultGCDRemain - AnimationLock);

	/// <summary>
	/// Returns the total duration of the default GCD (seconds). Clamped to non-negative.
	/// </summary>
	public static float DefaultGCDTotal => Math.Max(0f, ActionManagerHelper.GetDefaultRecastTime());

	/// <summary>
	/// Returns the remaining time for the default GCD by subtracting the elapsed time from the total recast time.
	/// Clamped to non-negative.
	/// </summary>
	public static float DefaultGCDRemain => Math.Max(0f, DefaultGCDTotal - DefaultGCDElapsed);

	/// <summary>
	/// Returns the elapsed time since the start of the default GCD. Clamped to non-negative.
	/// </summary>
	public static float DefaultGCDElapsed => Math.Max(0f, ActionManagerHelper.GetDefaultRecastTimeElapsed());

	/// <summary>
	/// Calculates the action ahead time based on the default GCD total and configured multiplier.
	/// Result is clamped to non-negative.
	/// </summary>
	public static float CalculatedActionAhead => Math.Max(0f, DefaultGCDTotal * Service.Config.Action6Head);

	/// <summary>
	/// Calculates the total GCD time for a given number of GCDs and an optional offset.
	/// </summary>
	/// <param name="gcdCount">The number of GCDs.</param>
	/// <param name="offset">The optional offset.</param>
	/// <returns>The total GCD time.</returns>
	public static float GCDTime(uint gcdCount = 0, float offset = 0)
	{
		return (DefaultGCDTotal * gcdCount) + offset;
	}
	#endregion
}
