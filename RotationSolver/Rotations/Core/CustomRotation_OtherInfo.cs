using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace RotationSolver.Rotations;

public partial class CustomRotation
{
	#region Player
	/// <summary>
	/// This is the player.
	/// </summary>
	protected static IPlayerCharacter? Player => ECommons.GameHelpers.Player.Object;

	/// <summary>
	/// 
	/// </summary>
	[Description("IsCasting")]
	public static bool IsCasting => Player?.IsCasting ?? false;

	/// <summary>
	/// 
	/// </summary>
	public static int AliveOtherPartyMemberCount
	{
		get
		{
			var count = 0;
			foreach (var member in PartyMembers)
			{
				if (!member.IsDead && member.ObjectId != Player?.ObjectId)
				{
					count++;
				}
			}

			return count;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	[Description("Is RSR active")]
	public static bool StateEnabled => DataCenter.State;

	/// <summary>
	/// Does player have swift cast, dual cast or triple cast. State
	/// </summary>
	[Description("Has Swift")]
	public static bool HasSwift => StatusHelper.PlayerHasStatus(true, StatusHelper.SwiftcastStatus);

	/// <summary>
	/// 
	/// </summary>
	[Description("Has tank stance")]
	public static bool HasTankStance => StatusHelper.PlayerHasStatus(true, StatusHelper.TankStanceStatus);

	/// <summary>
	/// 
	/// </summary>
	[Description("Has tank stance")]
	public static bool HasTankInvuln => StatusHelper.PlayerHasStatus(true, StatusHelper.NoNeedHealingStatus);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasVariantCure => StatusHelper.PlayerHasStatus(true, StatusID.VariantCureSet);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasPVPGuard => StatusHelper.PlayerHasStatus(true, StatusID.Guard) || (IsLastAction(ActionID.GuardPvP) && TimeSinceLastActionCapped < 0.5f);

	/// <summary>
	/// Check the player is moving, such as running, walking or jumping.
	/// </summary>
	[Description("Is Moving or Jumping")]
	public static bool IsMoving => DataCenter.IsMoving;

	/// <summary>
	/// Check if the player is dead.
	/// </summary>
	[Description("Is Dead, or inversely, is Alive")]
	public static bool IsDead => Player?.IsDead ?? false;

	/// <summary>
	/// Is in combat.
	/// </summary>
	[Description("In Combat")]
	public static bool InCombat => DataCenter.InCombat;

	/// <summary>
	/// Is out of combat.
	/// </summary>
	[Description("Not In Combat Delay")]
	public static bool NotInCombatDelay => DataCenter.NotInCombatDelay;

	/// <summary>
	/// Player's MP.
	/// </summary>
	[Description("Player's MP")]
	public static uint CurrentMp => DataCenter.CurrentMp;

	/// <summary>
	/// IsPGL.
	/// </summary>
	public static bool IsJobstoneless => DataCenter.BaseClass();

	/// <summary>
	/// Determines if the current combat time is within an even minute.
	/// WARNING: Do not use as a main function of your rotation, hardcoding timers is begging for everything to fuck up.
	/// </summary>
	/// <returns>True if the current combat time is within an even minute; otherwise, false.</returns>
	public static bool IsEvenMinute()
	{
		if (CombatTime <= 0)
		{
			return false;
		}

		var minutes = (int)Math.Floor(CombatTime / 60f);
		return minutes % 2 == 0;
	}

	/// <summary>
	/// Gets the party's class/job composition as a read-only list.
	/// </summary>
	public static IReadOnlyList<RowRef<ClassJob>> PartyComposition
	{
		get
		{
			var result = new List<RowRef<ClassJob>>();
			if (PartyMembers == null)
			{
				return result.AsReadOnly();
			}

			foreach (var member in PartyMembers)
			{
				if (member != null)
				{
					result.Add(member.ClassJob);
				}
			}
			return result.AsReadOnly();
		}
	}

	/// <summary>
	/// Determines whether the player has any active buffs or the hostile target has any active debuffs from the predefined list of status effects.
	/// <para>This property first populates the <see cref="Buffs"/> list by calling <see cref="StatusList"/> to ensure it reflects the current party composition or player's job.</para>
	/// <para>It checks for buffs on the player: if the player exists, it verifies that all buff-type statuses in <see cref="Buffs"/> are currently active on the player and not about to end.</para>
	/// <para>It checks for debuffs on the hostile target: if a hostile target exists, it verifies that all debuff-type statuses in <see cref="Buffs"/> are currently active on the target and not about to end.</para>
	/// <para>In the context of FFXIV rotation solving, this helps determine if beneficial status effects (buffs) are maintained on the player or if detrimental effects (debuffs) are applied to the target, influencing rotation decisions.</para>
	/// </summary>
	/// <returns>
	/// <c>true</c> if the player has all relevant buffs active (or if no player is present but buffs are defined) or if the hostile target has all relevant debuffs active; otherwise, <c>false</c>.
	/// <para>Note: Returns <c>false</c> if <see cref="Buffs"/> is empty after population.</para>
	/// </returns>
	public static bool HasBuffs
	{
		get
		{
			StatusList();

			if (Buffs.Count == 0)
			{
				return false;
			}

			var playerHasBuffs = true;
			if (Player == null)
			{
				playerHasBuffs = false;
			}
			else
			{
				for (var i = 0; i < Buffs.Count; i++)
				{
					var buff = Buffs[i];
					if (buff.Type != StatusType.Buff)
					{
						continue;
					}

					if (!StatusHelper.PlayerHasStatus(false, buff.Ids) || StatusHelper.PlayerWillStatusEnd(0, false, buff.Ids))
					{
						playerHasBuffs = false;
						break;
					}
				}
			}

			var targetHasDebuffs = HostileTarget != null;
			if (targetHasDebuffs)
			{
				var target = HostileTarget!;
				for (var i = 0; i < Buffs.Count; i++)
				{
					var buff = Buffs[i];
					if (buff.Type != StatusType.Debuff)
					{
						continue;
					}

					if (!target.HasStatus(false, buff.Ids) || target.WillStatusEnd(0, false, buff.Ids))
					{
						targetHasDebuffs = false;
						break;
					}
				}
			}

			return playerHasBuffs || targetHasDebuffs;
		}
	}

	/// <summary>
	/// Gets the duration (remaining time in seconds) of the status effect (buff or debuff) that will expire last among all active ones.
	/// </summary>
	public static float PartyBuffDuration
	{
		get
		{
			StatusList();

			if (Buffs.Count == 0)
			{
				return 0;
			}

			float maxDuration = 0;
			foreach (var buff in Buffs)
			{
				if (buff.Type == StatusType.Buff)
				{
					if (Player == null)
					{
						continue;
					}

					if (!StatusHelper.PlayerHasStatus(false, buff.Ids))
					{
						continue;
					}

					var remaining = StatusHelper.PlayerStatusTime(false, buff.Ids);
					if (remaining > maxDuration)
					{
						maxDuration = remaining;
					}
				}
				else if (buff.Type == StatusType.Debuff)
				{
					if (HostileTarget == null)
					{
						continue;
					}

					if (!HostileTarget.HasStatus(false, buff.Ids))
					{
						continue;
					}

					var remaining = HostileTarget.StatusTime(false, buff.Ids);
					if (remaining > maxDuration)
					{
						maxDuration = remaining;
					}
				}
			}

			return maxDuration;
		}
	}


	/// <summary>
	/// Gets the static list of status effects (buffs and debuffs) that are relevant to the current rotation configuration.
	/// <para>This list is populated by <see cref="StatusList"/> based on the party composition or the player's current job, and is used to track which status effects should be monitored for activation on the player (buffs) or hostile target (debuffs).</para>
	/// </summary>
	public static List<StatusInfo> Buffs { get; } = [];

	/// <summary>
	/// Populates the <see cref="Buffs"/> list with the raid buffs and debuffs of every job in the party, or of the player's job when solo.
	/// The list is rebuilt at most once per frame.
	/// </summary>
	public static void StatusList()
	{
		// Read many times per decision, so the list is rebuilt at most once a frame.
		var now = Environment.TickCount64;
		if (_buffsTick != long.MinValue && now - _buffsTick < 15)
		{
			return;
		}

		_buffsTick = now;
		Buffs.Clear();
		_buffJobs.Clear();
		foreach (var member in PartyMembers)
		{
			if (member != null)
			{
				AddJobBuffs((Job)member.ClassJob.RowId);
			}
		}

		if (_buffJobs.Count == 0 && Player != null)
		{
			AddJobBuffs((Job)Player.ClassJob.RowId);
		}
	}

	private static long _buffsTick = long.MinValue;
	private static readonly HashSet<Job> _buffJobs = [];

	private static readonly Dictionary<Job, List<StatusInfo>> JobBuffs = new()
	{
		{ Job.AST, [new StatusInfo("Divination", "AST", StatusType.Buff, StatusID.Divination)] },
		{ Job.BRD, [new StatusInfo("Battle Voice", "BRD", StatusType.Buff, StatusID.BattleVoice),
			new StatusInfo("Radiant Finale", "BRD", StatusType.Buff, StatusID.RadiantFinale_2964,
					StatusID.RadiantFinale)] },
		{ Job.DNC, [new StatusInfo("Technical Finish", "DNC", StatusType.Buff, StatusID.TechnicalFinish)] },
		{ Job.DRG, [new StatusInfo("Battle Litany", "DRG", StatusType.Buff, StatusID.BattleLitany)] },
		{ Job.MNK, [new StatusInfo("Brotherhood", "MNK", StatusType.Buff, StatusID.Brotherhood)] },
		{ Job.NIN, [new StatusInfo("Mug", "NIN", StatusType.Debuff, StatusID.Mug),
			new StatusInfo("Dokumori", "NIN", StatusType.Debuff, StatusID.Dokumori, StatusID.Dokumori_4303)] },
		{ Job.PCT, [new StatusInfo("Starry Muse", "PCT", StatusType.Buff, StatusID.StarryMuse)] },
		{ Job.RPR, [new StatusInfo("Arcane Circle", "RPR", StatusType.Buff, StatusID.ArcaneCircle)] },
		{ Job.RDM, [new StatusInfo("Embolden", "RDM", StatusType.Buff, StatusID.Embolden, StatusID.Embolden_1297)] },
		{ Job.SCH, [new StatusInfo("Chain Stratagem", "SCH", StatusType.Debuff, StatusID.ChainStratagem, StatusID.ChainStratagem_1406)] },
		{ Job.SMN, [new StatusInfo("Searing Light", "SMN", StatusType.Buff, StatusID.SearingLight)] }
	};

	private static void AddJobBuffs(Job job)
	{
		if (_buffJobs.Add(job) && JobBuffs.TryGetValue(job, out var buffs))
		{
			Buffs.AddRange(buffs);
		}
	}

	/// <summary>
	///
	/// </summary>
	public enum StatusType
	{
		/// <summary>
		///
		/// </summary>
		Buff,

		/// <summary>
		///
		/// </summary>
		Debuff
	}

	/// <summary>
	///
	/// </summary>
	public class StatusInfo(string name, string jobAbbr, StatusType type, params StatusID[] ids)
	{
		/// <summary>
		///
		/// </summary>
		public StatusID[] Ids { get; } = ids;

		/// <summary>
		///
		/// </summary>
		public string Name { get; } = name;

		/// <summary>
		///
		/// </summary>
		public string JobAbbr { get; } = jobAbbr;

		/// <summary>
		///
		/// </summary>
		public StatusType Type { get; } = type;
	}

	/// <summary>
	/// Determines if the current combat time is within the first 15 seconds of an even minute.
	/// WARNING: Do not use as a main function of your rotation, hardcoding timers is begging for everything to fuck up.
	/// </summary>
	/// <returns>True if the current combat time is within the first 15 seconds of an even minute; otherwise, false.</returns>
	public static bool IsWithinFirst15SecondsOfEvenMinute()
	{
		if (CombatTime <= 0)
		{
			return false;
		}

		var minutes = (int)Math.Floor(CombatTime / 60f);
		var secondsInCurrentMinute = (int)Math.Floor(CombatTime % 60f);

		return minutes % 2 == 0 && secondsInCurrentMinute < 15;
	}

	/// <summary>
	/// Condition.
	/// </summary>
	protected static ICondition Condition => Svc.Condition;

	#endregion

	#region Friends
	/// <summary>
	/// Has the comapnion now.
	/// </summary>
	[Description("Has companion")]
	public static bool HasCompanion => DataCenter.HasCompanion;

	/// <summary>
	/// True if the player has their chocobo companion out.
	/// </summary>
	[Description("Has Chocobo")]
	public static bool HasChocobo => Svc.Buddies.CompanionBuddy != null;

	/// <summary>
	/// Party member.
	/// </summary>
	protected static IEnumerable<IBattleChara> PartyMembers => DataCenter.PartyMembers;

	/// <summary>
	/// Alliance members.
	/// </summary>
	protected static IEnumerable<IBattleChara> AllianceMembers => DataCenter.AllianceMembers;

	/// <summary>
	/// Whether the number of party members is 8.
	/// </summary>
	[Description("Is Full Party")]
	public static bool IsFullParty
	{
		get
		{
			var count = 0;
			var members = PartyMembers;
			if (members != null)
			{
				foreach (var _ in members)
				{
					count++;
					if (count > 9)
					{
						break;
					}
				}
			}
			return count == 8 || count == 9;
		}
	}

	/// <summary>
	/// party members HP.
	/// </summary>
	protected static IEnumerable<float> PartyMembersHP => DataCenter.PartyMembersHP;

	/// <summary>
	/// Min HP in party members.
	/// </summary>
	[Description("Min HP in party members.")]
	public static float PartyMembersMinHP => DataCenter.PartyMembersMinHP;

	/// <summary>
	/// Average HP in party members.
	/// </summary>
	[Description("Average HP in party members.")]
	public static float PartyMembersAverHP => DataCenter.PartyMembersAverHP;

	/// <summary>
	/// 
	/// </summary>
	public static IBattleChara? LowestHealthPartyMember
	{
		get
		{
			if (Player == null)
			{
				return null;
			}

			IBattleChara lowest = Player;
			var lowestHp = Player?.GetHealthRatio();

			foreach (var member in PartyMembers)
			{
				if (member == null || member.IsDead)
				{
					continue;
				}

				var memberHpRatio = member.GetHealthRatio();
				if (memberHpRatio < lowestHp)
				{
					lowest = member;
					lowestHp = memberHpRatio;
				}
			}

			return lowest;
		}
	}

	/// <summary>
	/// Calculates the current cumulative mitigation percentage applied to an imminent AoE or raid-wide hit.
	/// </summary>
	/// <returns>
	/// A normalized mitigation fraction in the range 0.0–0.95 (e.g. 0.25 == 25% damage reduction).
	/// The value is capped at 0.95 to prevent extreme stacking edge cases.
	/// </returns>
	/// <remarks>
	/// Mitigations and enemy debuffs are applied multiplicatively as damage factors (e.g. 10% reduction = * 0.90).
	/// The function:
	/// <list type="bullet">
	/// <item>Scans hostile targets once for Addle, Feint, and Dismantle.</item>
	/// <item>Scans party members for active raid/party-wide mitigation statuses (e.g. Sacred Soil, Temperance, Troubadour).</item>
	/// <item>Handles mixed scaling for effects whose value differs by damage school (e.g. Addle, Feint, Dark Missionary).</item>
	/// </list>
	/// Performance impact is minimal given normal FFXIV party sizes.
	/// </remarks>
	public static float GetCurrentMitigationPercent()
	{
		var damageFactor = 1.0f;

		var partyEnum = PartyMembers;
		var hostileEnum = AllHostileTargets;

		// Determine (heuristically) if the imminent AoE is magical.
		var incomingMagical = IsMagicalDamageIncoming;

		// Enemy debuffs (scan once).
		var addle = false;
		var feint = false;
		var dismantle = false;
		var reprisal = false;

		if (hostileEnum != null)
		{
			foreach (var e in hostileEnum)
			{
				if (e == null)
				{
					continue;
				}

				// Addle: -10% magical / -5% physical
				if (!addle && e.HasStatus(false, StatusID.Addle))
				{
					addle = true;
				}

				// Feint: -10% physical / -5% magical
				if (!feint && e.HasStatus(false, StatusID.Feint))
				{
					feint = true;
				}

				if (!dismantle && e.HasStatus(false, StatusID.Dismantled))
				{
					dismantle = true;
				}

				// Reprisal: -10% all damage (missing previously)
				if (!reprisal && e.HasStatus(false, StatusID.Reprisal))
				{
					reprisal = true;
				}

				if (addle && feint && dismantle && reprisal)
				{
					break;
				}
			}
		}

		if (addle)
		{
			damageFactor *= incomingMagical ? 0.90f : 0.95f;
		}

		if (feint)
		{
			damageFactor *= incomingMagical ? 0.95f : 0.90f;
		}

		if (dismantle)
		{
			damageFactor *= 0.90f;
		}

		if (reprisal)
		{
			damageFactor *= 0.90f;
		}

		HashSet<StatusID> partyStatuses = [];

		bool HasPartyStatus(StatusID id)
		{
			if (partyStatuses.Contains(id))
			{
				return true;
			}

			if (partyEnum != null)
			{
				foreach (var m in partyEnum)
				{
					if (m == null)
					{
						continue;
					}

					if (m.HasStatus(false, id))
					{
						partyStatuses.Add(id);
						return true;
					}
				}
			}
			return false;
		}

		// Tank LB3: -80% all damage
		if (HasPartyStatus(StatusID.LastBastion)
			|| HasPartyStatus(StatusID.DarkForce)
			|| HasPartyStatus(StatusID.GunmetalSoul)
			|| HasPartyStatus(StatusID.LandWaker))
		{
			damageFactor *= 0.2f;
		}

		if (HasPartyStatus(StatusID.SacredSoil))
		{
			damageFactor *= 0.90f;
		}

		if (incomingMagical && HasPartyStatus(StatusID.FeyIllumination))
		{
			damageFactor *= 0.95f;
		}

		if (HasPartyStatus(StatusID.DesperateMeasures))
		{
			damageFactor *= 0.90f;
		}

		if (HasPartyStatus(StatusID.Temperance_1873))
		{
			damageFactor *= 0.90f;
		}

		if (HasPartyStatus(StatusID.Holos))
		{
			damageFactor *= 0.90f;
		}

		if (HasPartyStatus(StatusID.Kerachole))
		{
			damageFactor *= 0.90f;
		}

		if (HasPartyStatus(StatusID.CollectiveUnconscious_849))
		{
			damageFactor *= 0.90f;
		}

		if (HasPartyStatus(StatusID.Troubadour)
			|| HasPartyStatus(StatusID.ShieldSamba)
			|| HasPartyStatus(StatusID.Tactician_1951))
		{
			damageFactor *= 0.90f;
		}

		if (HasPartyStatus(StatusID.DarkMissionary))
		{
			damageFactor *= incomingMagical ? 0.90f : 0.95f;
		}

		if (HasPartyStatus(StatusID.HeartOfLight))
		{
			damageFactor *= incomingMagical ? 0.90f : 0.95f;
		}

		if (incomingMagical && HasPartyStatus(StatusID.MagickBarrier))
		{
			damageFactor *= 0.90f;
		}

		if (HasPartyStatus(StatusID.PassageOfArms))
		{
			damageFactor *= 0.85f;
		}

		var mitigated = 1.0f - damageFactor;
		return Math.Clamp(mitigated, 0f, 0.95f);
	}

	/// <summary>
	///
	/// </summary>
	[Description("Is an enemy casting magic AOE")]
	public static bool IsMagicalDamageIncoming => DataCenter.IsMagicalDamageIncoming();

	/// <summary>
	///
	/// </summary>
	[Description("Is an enemy casting physical AOE")]
	public static bool IsPhysicalDamageIncoming => DataCenter.IsPhysicalDamageIncoming();

	/// <summary>
	///
	/// </summary>
	[Description("Is an enemy casting a multihit AOE party stack")]
	public static bool IsCastingMultiHit => DataCenter.IsCastingMultiHit();

	#endregion

	#region Target
	/// <summary>
	/// The player's target.
	/// <br> WARNING: Do not use if there is more than one target, this is not the actions target, it is the players current hard target. Try to use <see cref="IBaseAction.Target"/> or <seealso cref="HostileTarget"/> instead after using this.</br>
	/// </summary>
	protected static IBattleChara Target => Svc.Targets.Target as IBattleChara ?? Player!;

	/// <summary>
	/// The player's target, or null if no valid target. (null clears the target)
	/// </summary>
	protected static IBattleChara? CurrentTarget => Svc.Targets.Target is IBattleChara b ? b : null;

	/// <summary>
	/// The last attacked hostile target.
	/// </summary>
	protected static IBattleChara? HostileTarget => DataCenter.HostileTarget ?? null;

	/// <summary>
	/// Is player in position to hit the positional?
	/// </summary>
	/// <param name="positional"> Which Positional? "Flank" or "Rear"?</param>
	/// <param name="enemy"></param>
	/// <returns></returns>
	public static bool CanHitPositional(EnemyPositional positional, IBattleChara enemy)
	{
		if (enemy == null)
		{
			return false;
		}

		if (!enemy.HasPositional())
		{
			return true;
		}

		var enemy_positional = enemy.FindEnemyPositional();

		return enemy_positional == positional;
	}

	/// <summary>
	/// Returns the number of hostile targets within the specified range from the player.
	/// </summary>
	/// <param name="range">The range to check (in yalms).</param>
	/// <returns>The number of hostile targets within the given range.</returns>
	[Description("The number of hostiles in specified range")]
	public static int NumberOfHostilesInRangeOf(float range)
	{
		return DataCenter.NumberOfHostilesInRangeOf(range);
	}

	/// <summary>
	/// Is there any hostile target in range? 25 for ranged jobs and healer, 3 for melee and tank.
	/// </summary>
	[Description("Has hostiles in Range")]
	public static bool HasHostilesInRange => DataCenter.HasHostilesInRange;

	/// <summary>
	/// Is there any hostile target in 25 yalms?
	/// </summary>
	[Description("Has hostiles in 25 yalms")]
	public static bool HasHostilesInMaxRange => DataCenter.HasHostilesInMaxRange;

	/// <summary>
	/// How many hostile targets in range? 25 for ranged jobs and healer, 3 for melee and tank.
	/// </summary>
	[Description("The number of hostiles in Range")]
	public static int NumberOfHostilesInRange => DataCenter.NumberOfHostilesInRange;

	/// <summary>
	/// How many hostile targets in max range (25 yalms) regardless of job
	/// </summary>
	[Description("The number of hostiles in max Range")]
	public static int NumberOfHostilesInMaxRange => DataCenter.NumberOfHostilesInMaxRange;

	/// <summary>
	/// How many hostile targets in range? 25 for ranged jobs and healer, 3 for melee and tank. This is all can attack.
	/// </summary>
	[Description("The number of all hostiles in Range")]
	public static int NumberOfAllHostilesInRange => DataCenter.NumberOfAllHostilesInRange;

	/// <summary>
	/// How many hostile targets in max range (25 yalms) regardless of job. This is all can attack.
	/// </summary>
	[Description("The number of all hostiles in max Range")]
	public static int NumberOfAllHostilesInMaxRange => DataCenter.NumberOfAllHostilesInMaxRange;

	/// <summary>
	/// All hostile Targets. This is all attackable targets.
	/// </summary>
	protected static IEnumerable<IBattleChara> AllHostileTargets => DataCenter.AllHostileTargets;

	/// <summary>
	/// All targets. This includes both hostile and friendly targets.
	/// </summary>
	protected static IEnumerable<IBattleChara> AllTargets => DataCenter.AllTargets;

	/// <summary>
	/// Average time to kill for all targets.
	/// </summary>
	[Description("Average time to kill")]
	public static float AverageTTK => DataCenter.AverageTTK;

	/// <summary>
	/// The level of the LB.
	/// </summary>
	[Description("Limit Break Level")]
	public static unsafe byte LimitBreakLevel
	{
		get
		{
			var controller = &UIState.Instance()->LimitBreakController;
			return controller->BarUnits == 0 ? (byte)0 : (byte)(controller->CurrentUnits / controller->BarUnits);
		}
	}

	/// <summary>
	/// Is the <see cref="AverageTTK"/> larger than <paramref name="time"/>. True while no time to kill is known yet.
	/// </summary>
	/// <param name="time">Time</param>
	/// <returns>Is Longer.</returns>
	public static bool IsLongerThan(float time)
	{
		return !DataCenter.IsAverageTTKKnown || AverageTTK > time;
	}

	/// <summary>
	/// How long each mob has been in combat.
	/// </summary>
	[Description("Mobs Time")]
	public static bool MobsTime => DataCenter.MobsTime;
	#endregion

	#region GCD

	/// <summary>
	/// 
	/// </summary>
	protected static float WeaponRemain => DataCenter.DefaultGCDRemain;

	// Tracks when the Next GCD action (from IAction/IBaseAction) last changed.
	private static DateTime _lastNextGCDChange = DateTime.MinValue;
	private static uint _lastNextGCDId = 0;

	/// <summary>
	/// The DateTime when the last NextGCD action change occurred. DateTime.MinValue if never changed.
	/// </summary>
	public static DateTime LastNextGCDChange => _lastNextGCDChange;

	/// <summary>
	/// The AdjustedID of the last NextGCD action when it changed (0 if none).
	/// </summary>
	public static uint LastNextGCDId => _lastNextGCDId;

	/// <summary>
	/// Seconds elapsed since the last NextGCD change. Returns a large value if never changed.
	/// </summary>
	public static float SecondsSinceLastNextGCDChange => _lastNextGCDChange == DateTime.MinValue ? float.MaxValue : (float)(DateTime.Now - _lastNextGCDChange).TotalSeconds;

	/// <summary>
	/// 
	/// </summary>
	protected static float WeaponTotal => DataCenter.DefaultGCDTotal;

	/// <summary>
	/// 
	/// </summary>
	protected static float WeaponElapsed => DataCenter.DefaultGCDElapsed;

	/// <summary>
	/// 
	/// </summary>
	public static float AnimationLock => DataCenter.AnimationLock;

	/// <summary>
	/// Time from next ability to next GCD
	/// </summary>
	[Description("Time from next ability to next GCD")]
	public static float NextAbilityToNextGCD => DataCenter.NextAbilityToNextGCD;

	/// <summary>
	/// Treats one action as another.
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	public static uint AdjustId(uint id)
	{
		return Service.GetAdjustedActionId(id);
	}

	/// <summary>
	/// Treats one action as another.
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	public static ActionID AdjustId(ActionID id)
	{
		return Service.GetAdjustedActionId(id);
	}
	#endregion

	/// <summary>
	/// Client Language.
	/// </summary>
	protected static ClientLanguage Language => Svc.ClientState.ClientLanguage;

	#region Service

	/// <summary>
	/// The count down ahead.
	/// </summary>
	[Description("Count Down ahead")]
	public static float CountDownAhead => Service.Config.CountDownAhead;

	/// <summary>
	/// 
	/// </summary>
	[Description("Health of Area Ability")]
	public static float HealthAreaAbility => Service.Config.HealthAreaAbility;

	/// <summary>
	/// 
	/// </summary>
	[Description("Health of Area spell")]
	public static float HealthAreaSpell => Service.Config.HealthAreaSpell;

	/// <summary>
	/// 
	/// </summary>
	[Description("Health of Area Ability Hot")]
	public static float HealthAreaAbilityHot => Service.Config.HealthAreaAbilityHot;

	/// <summary>
	/// 
	/// </summary>
	[Description("Health of Area spell Hot")]
	public static float HealthAreaSpellHot => Service.Config.HealthAreaSpellHot;

	/// <summary>
	/// 
	/// </summary>
	[Description("Health of single ability")]
	public static float HealthSingleAbility => Service.Config.HealthSingleAbility;

	/// <summary>
	/// 
	/// </summary>
	[Description("Health of single spell")]
	public static float HealthSingleSpell => Service.Config.HealthSingleSpell;

	/// <summary>
	/// 
	/// </summary>
	[Description("Health of single ability Hot")]
	public static float HealthSingleAbilityHot => Service.Config.HealthSingleAbilityHot;

	/// <summary>
	/// 
	/// </summary>
	[Description("Health of single spell Hot")]
	public static float HealthSingleSpellHot => Service.Config.HealthSingleSpellHot;

	/// <summary>
	/// 
	/// </summary>
	[Description("Health of dying tank")]
	public static float HealthForDyingTanks => Service.Config.HealthForDyingTanks;

	/// <summary>
	/// 
	/// </summary>
	[Description("Whether or not Invincibility should be ignored for a PvP action.")]
	public static bool IgnorePvPInvincibility => Service.Config.IgnorePvPInvincibility;
	#endregion
}
