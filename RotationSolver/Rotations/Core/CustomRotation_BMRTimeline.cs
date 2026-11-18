using Lumina.Excel.Sheets;

namespace RotationSolver.Rotations;

public partial class CustomRotation
{
	#region BossModReborn Timeline
	/// <summary>
	/// Whether BossModReborn has an active boss module loaded for the current encounter.
	/// </summary>
	[Description("BMR has active module")]
	public static bool BMRActive => DataCenter.BMRHasActiveModule;

	/// <summary>
	/// Seconds until the next raidwide damage event according to BMR's timeline.
	/// Returns float.MaxValue if no raidwide is predicted.
	/// </summary>
	[Description("Seconds until next raidwide")]
	public static float BMRRaidwideIn => DataCenter.BMRNextRaidwideIn;

	/// <summary>
	/// Seconds until the next tankbuster event according to BMR's timeline.
	/// Returns float.MaxValue if no tankbuster is predicted.
	/// </summary>
	[Description("Seconds until next tankbuster")]
	public static float BMRTankbusterIn => DataCenter.BMRNextTankbusterIn;

	/// <summary>
	/// Seconds until the next knockback event according to BMR's timeline.
	/// Returns float.MaxValue if no knockback is predicted.
	/// </summary>
	[Description("Seconds until next knockback")]
	public static float BMRKnockbackIn => DataCenter.BMRNextKnockbackIn;

	/// <summary>
	/// Seconds until the next predicted damage event from BMR's active components.
	/// This is more precise than timeline data but has a shorter lookahead window.
	/// Returns float.MaxValue if no damage is predicted.
	/// </summary>
	[Description("Seconds until next predicted damage")]
	public static float BMRDamageIn => DataCenter.BMRNextDamageIn;

	/// <summary>
	/// The type of the next predicted damage event (0=None, 1=Tankbuster, 2=Raidwide, 3=Shared).
	/// </summary>
	[Description("Next predicted damage type")]
	public static PredictedDamageType BMRDamageType => DataCenter.BMRNextDamageType;

	/// <summary>
	/// Seconds until the boss becomes untargetable (downtime starts).
	/// Returns float.MaxValue if no downtime is predicted.
	/// </summary>
	[Description("Seconds until next downtime")]
	public static float BMRDowntimeIn => DataCenter.BMRNextDowntimeIn;

	/// <summary>
	/// Seconds until the boss becomes targetable again (downtime ends).
	/// Returns float.MaxValue if no downtime end is predicted.
	/// </summary>
	[Description("Seconds until downtime ends")]
	public static float BMRDowntimeEndIn => DataCenter.BMRNextDowntimeEndIn;

	/// <summary>
	/// Seconds until the boss becomes vulnerable (takes extra damage).
	/// Returns float.MaxValue if no vulnerability window is predicted.
	/// </summary>
	[Description("Seconds until vulnerability window")]
	public static float BMRVulnerableIn => DataCenter.BMRNextVulnerableIn;

	/// <summary>
	/// Seconds until the vulnerability window ends.
	/// Returns float.MaxValue if no vulnerability end is predicted.
	/// </summary>
	[Description("Seconds until vulnerability ends")]
	public static float BMRVulnerableEndIn => DataCenter.BMRNextVulnerableEndIn;

	/// <summary>
	/// True when BMR reports downtime within the specified seconds.
	/// Always false when BMR is inactive (safe fallback).
	/// </summary>
	public static bool BMRDowntimeWithin(float seconds)
		=> BMRActive && BMRDowntimeIn is > 0f and < float.MaxValue && BMRDowntimeIn <= seconds;

	/// <summary>
	/// True when BMR reports a vulnerability window within the specified seconds.
	/// Always false when BMR is inactive (safe fallback).
	/// </summary>
	public static bool BMRVulnWithin(float seconds)
		=> BMRActive && BMRVulnerableIn is > 0f and < float.MaxValue && BMRVulnerableIn <= seconds;

	/// <summary>
	/// True when BMR reports a raidwide within the specified seconds.
	/// </summary>
	public static bool BMRRaidwideWithin(float seconds)
		=> BMRActive && BMRRaidwideIn is > 0f and < float.MaxValue && BMRRaidwideIn <= seconds;

	/// <summary>
	/// True when BMR reports a tankbuster within the specified seconds.
	/// </summary>
	public static bool BMRTankbusterWithin(float seconds)
		=> BMRActive && BMRTankbusterIn is > 0f and < float.MaxValue && BMRTankbusterIn <= seconds;
	#endregion

	/// <summary>
	/// The threshold (in seconds) that defines the start of a late weave window.
	/// </summary>
	public static float LateWeaveWindow => WeaponTotal * 0.45f;

	/// <summary>
	/// Checks if there is enough remaining time in the current GCD to execute an off-global action without clipping.
	/// </summary>
	public static bool EnoughWeaveTime => WeaponRemain < WeaponTotal && WeaponRemain > Math.Max(DataCenter.CalculatedActionAhead, AnimationLock);

	/// <summary>
	/// Indicates whether the player can currently execute a late weave.
	/// </summary>
	public static bool CanLateWeave => WeaponRemain <= LateWeaveWindow && CanWeave;

	/// <summary>
	/// Indicates whether the player can currently execute an early weave.
	/// </summary>
	public static bool CanEarlyWeave => (!HasWeaved() || WeaponRemain > LateWeaveWindow) && CanWeave;

	/// <summary>
	/// Safely verifies that the player is in the middle of a GCD and has enough time to weave an oGCD.
	/// </summary>
	public static bool CanWeave => EnoughWeaveTime && AnimationLock <= 0f;

	/// <summary>
	/// Counts how many party members (alive and not full HP) are within range of the pet.
	/// </summary>
	public static int InRangeOfPet(float range)
	{
		if (!DataCenter.HasPet())
		{
			return 0;
		}

		var count = 0;

		foreach (var target in PartyMembers)
		{
			if (target == null)
			{
				continue;
			}

			if (target.IsDead)
			{
				continue;
			}

			if (target.GetHealthRatio() == 1f)
			{
				continue;
			}

			if (ObjectHelper.DistanceToPet(target) < range)
			{
				count++;
			}
		}

		return count;
	}

	/// <summary>
	/// Whether or not the player can use AOE heal oGCDs.
	/// </summary>
	[Description("Can heal area ability")]
	public virtual bool CanHealAreaAbility => true;

	/// <summary>
	/// Whether or not the player can use AOE heal GCDs.
	/// </summary>
	[Description("Can heal area spell")]
	public virtual bool CanHealAreaSpell => true;

	/// <summary>
	/// Whether or not the player can use ST heal oGCDs.
	/// </summary>
	[Description("Can heal single ability")]
	public virtual bool CanHealSingleAbility => true;

	/// <summary>
	/// Whether or not the player can use ST heal GCDs.
	/// </summary>
	[Description("Can heal single spell")]
	public virtual bool CanHealSingleSpell => true;

	/// <summary>
	///
	/// </summary>
	public static int RaiseMPMinimum => Service.Config.LessMPNoRaise;

	/// <summary>
	/// Is RSR enabled.
	/// </summary>
	[Description("The state of auto. True for on.")]
	public static bool AutoState => DataCenter.State;

	/// <summary>
	/// Is RSR in manual mode.
	/// </summary>
	[Description("The state of manual. True for manual.")]
	public static bool IsManual => DataCenter.IsManual;

	/// <summary>
	/// In the burst status.
	/// </summary>
	[Description("Is burst")]
	public static bool IsBurst => MergedStatus.HasFlag(AutoStatus.Burst);

	/// <summary>
	/// The merged status, which contains <see cref="AutoState"/> and <see cref="CommandStatus"/>.
	/// </summary>
	public static AutoStatus MergedStatus => DataCenter.MergedStatus;

	/// <summary>
	/// The automatic status, which is checked from RS.
	/// </summary>
	public static AutoStatus AutoStatus => DataCenter.AutoStatus;

	/// <summary>
	/// The CMD status, which is checked from the player.
	/// </summary>
	public static AutoStatus CommandStatus => DataCenter.CommandStatus;
}
