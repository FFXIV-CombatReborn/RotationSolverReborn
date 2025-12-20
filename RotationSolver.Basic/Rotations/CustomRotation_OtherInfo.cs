using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace RotationSolver.Basic.Rotations;
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
	/// Does player have swift cast, dual cast or triple cast.
	/// </summary>
	[Description("Has Swift")]
    public static bool HasSwift => Player?.HasStatus(true, StatusHelper.SwiftcastStatus) ?? false;

    /// <summary>
    /// 
    /// </summary>
    [Description("Has tank stance")]
    public static bool HasTankStance => Player?.HasStatus(true, StatusHelper.TankStanceStatus) ?? false;

    /// <summary>
    /// 
    /// </summary>
    [Description("Has tank stance")]
    public static bool HasTankInvuln => Player?.HasStatus(true, StatusHelper.NoNeedHealingStatus) ?? false;

    /// <summary>
    /// 
    /// </summary>
    public static bool HasVariantCure => Player?.HasStatus(true, StatusID.VariantCureSet) ?? false;

	/// <summary>
	/// 
	/// </summary>
	public static bool HasPVPGuard => Player?.HasStatus(true, StatusID.Guard) ?? false;

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

        int minutes = (int)Math.Floor(CombatTime / 60f);
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

            if (Buffs.Count == 0) return false;

            bool playerHasBuffs = true;
            if (Player == null)
            {
                playerHasBuffs = false;
            }
            else
            {
                for (int i = 0; i < Buffs.Count; i++)
                {
                    var buff = Buffs[i];
                    if (buff.Type != StatusType.Buff) continue;

                    if (!StatusHelper.PlayerHasStatus(false, buff.Ids) || StatusHelper.PlayerWillStatusEnd(0, false, buff.Ids))
                    {
                        playerHasBuffs = false;
                        break;
                    }
                }
            }

            bool targetHasDebuffs = HostileTarget != null;
            if (targetHasDebuffs)
            {
                var target = HostileTarget!;
                for (int i = 0; i < Buffs.Count; i++)
                {
                    var buff = Buffs[i];
                    if (buff.Type != StatusType.Debuff) continue;

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

                if (Buffs.Count == 0) return 0;

                float maxDuration = 0;
                foreach (var buff in Buffs)
                {
                    if (buff.Type == StatusType.Buff)
                    {
                        if (Player == null) continue;
                        if (!StatusHelper.PlayerHasStatus(false, buff.Ids)) continue;

                        float remaining = StatusHelper.PlayerStatusTime(true, buff.Ids[0]);
                        if (remaining > maxDuration) maxDuration = remaining;
                    }
                    else if (buff.Type == StatusType.Debuff)
                    {
                        if (HostileTarget == null) continue;
                        if (!HostileTarget.HasStatus(false, buff.Ids)) continue;

                        float remaining = HostileTarget.StatusTime(true, buff.Ids[0]);
                        if (remaining > maxDuration) maxDuration = remaining;
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
    /// Populates the <see cref="Buffs"/> list with status effects (buffs and debuffs) based on the current party composition or the player's job.
    /// <para>This method clears any existing entries in <see cref="Buffs"/> and rebuilds the list to reflect the jobs present in the party or the solo player's job.</para>
    /// <para>If <see cref="PartyComposition"/> is null (indicating a solo scenario), it adds buffs specific to the player's current job abbreviation.</para>
    /// <para>If <see cref="PartyComposition"/> is available, it iterates through each party member's job and adds relevant buffs, avoiding duplicates via a processed jobs set.</para>
    /// <para>The <see cref="AddJobBuffs"/> method is called for each unique job abbreviation to append appropriate <see cref="StatusInfo"/> entries to <see cref="Buffs"/>.</para>
    /// <para>This ensures that <see cref="Buffs"/> contains only the status effects pertinent to the current group setup.</para>
    /// </summary>
    public static void StatusList()
    {
        Buffs.Clear();
        var processedJobs = new HashSet<string>();

		if (PartyComposition == null)
		{
			if (Player?.ClassJob.Value.Abbreviation != null)
			{
				var abbr = Player.ClassJob.Value.Abbreviation.ToString();
				AddJobBuffs(abbr, processedJobs);
			}
		}
		else
        {
            foreach (var job in PartyComposition)
            {
                var abbr = job.Value.Abbreviation.ToString();
                AddJobBuffs(abbr, processedJobs);
            }
        }
    }

    private static readonly Dictionary<string, List<StatusInfo>> JobBuffs = new()
    {
        { "AST", [new StatusInfo("Divination", "AST", StatusType.Buff, StatusID.Divination)] },
        { "BRD", [new StatusInfo("Battle Voice", "BRD", StatusType.Buff, StatusID.BattleVoice),
            new StatusInfo("Radiant Finale", "BRD", StatusType.Buff, StatusID.RadiantFinale_2964,
                    StatusID.RadiantFinale)] },
        { "DNC", [new StatusInfo("Technical Finish", "DNC", StatusType.Buff, StatusID.TechnicalFinish)] },
        { "DRG", [new StatusInfo("Battle Litany", "DRG", StatusType.Buff, StatusID.BattleLitany)] },
        { "MNK", [new StatusInfo("Brotherhood", "MNK", StatusType.Buff, StatusID.Brotherhood)] },
        { "NIN", [new StatusInfo("Mug", "NIN", StatusType.Debuff, StatusID.Mug),
            new StatusInfo("Dokumori", "NIN", StatusType.Debuff, StatusID.Dokumori, StatusID.Dokumori_4303)] },
        { "PCT", [new StatusInfo("Starry Muse", "PCT", StatusType.Buff, StatusID.StarryMuse)] },
        { "RPR", [new StatusInfo("Arcane Circle", "RPR", StatusType.Buff, StatusID.ArcaneCircle)] },
        { "RDM", [new StatusInfo("Embolden", "RDM", StatusType.Buff, StatusID.Embolden, StatusID.Embolden_1297)] },
        { "SCH", [new StatusInfo("Chain Stratagem", "SCH", StatusType.Debuff, StatusID.ChainStratagem, StatusID.ChainStratagem_1406)] },
        { "SMN", [new StatusInfo("Searing Light", "SMN", StatusType.Buff, StatusID.SearingLight)] }
    };

    private static void AddJobBuffs(string abbr, HashSet<string> processedJobs)
    {
        if (!processedJobs.Add(abbr)) return;

        if (JobBuffs.TryGetValue(abbr, out var buffs))
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

        int minutes = (int)Math.Floor(CombatTime / 60f);
        int secondsInCurrentMinute = (int)Math.Floor(CombatTime % 60f);

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
            int count = 0;
            var members = PartyMembers;
            if (members != null)
            {
                foreach (var _ in members)
                {
                    count++;
                    if (count > 9) break;
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
            if (Player == null) return null;
            IBattleChara lowest = Player;
            var lowestHp = Player?.GetHealthRatio();

            foreach (var member in PartyMembers)
            {
                if (member == null || member.IsDead) continue;
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
        float damageFactor = 1.0f;

        var partyEnum = PartyMembers;
        var hostileEnum = AllHostileTargets;

        // Determine (heuristically) if the imminent AoE is magical.
        bool incomingMagical = IsMagicalDamageIncoming();

        // Enemy debuffs (scan once).
        bool addle = false;
        bool feint = false;
        bool dismantle = false;
        bool reprisal = false;

        if (hostileEnum != null)
        {
            foreach (var e in hostileEnum)
            {
                if (e == null) continue;

                // Addle: -10% magical / -5% physical
                if (!addle && e.HasStatus(false, StatusID.Addle))
                    addle = true;

                // Feint: -10% physical / -5% magical
                if (!feint && e.HasStatus(false, StatusID.Feint))
                    feint = true;

                if (!dismantle && e.HasStatus(false, StatusID.Dismantled))
                    dismantle = true;

                // Reprisal: -10% all damage (missing previously)
                if (!reprisal && e.HasStatus(false, StatusID.Reprisal))
                    reprisal = true;

                if (addle && feint && dismantle && reprisal)
                    break;
            }
        }

        if (addle)
            damageFactor *= incomingMagical ? 0.90f : 0.95f;
        if (feint)
            damageFactor *= incomingMagical ? 0.95f : 0.90f;
        if (dismantle)
            damageFactor *= 0.90f;
        if (reprisal)
            damageFactor *= 0.90f;

        // Collect party statuses once into a hash set for O(1) lookups.
        HashSet<StatusID> partyStatuses = [];
        if (partyEnum != null)
        {
            foreach (var m in partyEnum)
            {
                if (m == null) continue;
                // Here we just probe the relevant IDs.
                // To avoid N*M calls, we gather by probing only needed IDs below if not already present.
            }
        }

        // Helper to lazily test & cache a status.
        bool HasPartyStatus(StatusID id)
        {
            bool haspartyStatuses = false;
            foreach (var status in partyStatuses)
            {
                if (status == id)
                {
                    haspartyStatuses = true;
                    break;
                }
            }
            if (haspartyStatuses) return true;

            if (partyEnum != null)
            {
                foreach (var m in partyEnum)
                {
                    if (m == null) continue;
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
            damageFactor *= 0.2f;

        if (HasPartyStatus(StatusID.SacredSoil))
            damageFactor *= 0.90f;
        if (incomingMagical && HasPartyStatus(StatusID.FeyIllumination))
            damageFactor *= 0.95f;
        if (HasPartyStatus(StatusID.DesperateMeasures)) // Expedient mitigation component
            damageFactor *= 0.90f;
        if (HasPartyStatus(StatusID.Temperance_1873))
            damageFactor *= 0.90f;
        if (HasPartyStatus(StatusID.Holos))
            damageFactor *= 0.90f;
        if (HasPartyStatus(StatusID.Kerachole))
            damageFactor *= 0.90f;
        if (HasPartyStatus(StatusID.CollectiveUnconscious_849))
            damageFactor *= 0.90f;

        if (HasPartyStatus(StatusID.Troubadour)
            || HasPartyStatus(StatusID.ShieldSamba)
            || HasPartyStatus(StatusID.Tactician_1951))
            damageFactor *= 0.90f;

        if (HasPartyStatus(StatusID.DarkMissionary))
            damageFactor *= incomingMagical ? 0.90f : 0.95f;

        if (HasPartyStatus(StatusID.HeartOfLight))
            damageFactor *= incomingMagical ? 0.90f : 0.95f;

        if (incomingMagical && HasPartyStatus(StatusID.MagickBarrier))
            damageFactor *= 0.90f;

        if (HasPartyStatus(StatusID.PassageOfArms))
            damageFactor *= 0.85f;

        float mitigated = 1.0f - damageFactor;
        return Math.Clamp(mitigated, 0f, 0.95f);
    }

    /// <summary>
    /// Determines whether any currently casting hostile action is classified as magical.
    /// </summary>
    /// <returns>
    /// True if at least one hostile target is casting an action whose <c>AttackType.RowId == 5</c> (interpreted as magical); otherwise false.
    /// </returns>
    /// <remarks>
    /// Scans all hostile entities with a non-zero <c>CastActionId</c>, looks up the action row, and inspects the attack type.
    /// Returns early on the first confirmed magical cast.
    /// If the action sheet cannot be loaded or no valid casts exist, returns false.
    /// </remarks>
    public static bool IsMagicalDamageIncoming()
    {
        var hostileEnum = AllHostileTargets;
        if (hostileEnum == null) return false;

        var actionSheet = Service.GetSheet<Lumina.Excel.Sheets.Action>();
        if (actionSheet == null) return false;

        foreach (var hostile in hostileEnum)
        {
            if (hostile == null) continue;
            if (hostile.CastActionId == 0) continue;

            var action = actionSheet.GetRow(hostile.CastActionId);
            if (action.RowId == 0) continue;

            // AttackType row id 5 interpreted as magical.
            if (action.AttackType.RowId == 5)
            {
                return true;
            }
        }
        return false;
    }

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

        EnemyPositional enemy_positional = enemy.FindEnemyPositional();

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
            LimitBreakController controller = UIState.Instance()->LimitBreakController;
            ushort barValue = *(ushort*)&controller.BarCount;
            return barValue == 0 ? (byte)0 : (byte)(controller.BarCount / barValue);
        }
    }

    /// <summary>
    /// Is the <see cref="AverageTTK"/> larger than <paramref name="time"/>.
    /// </summary>
    /// <param name="time">Time</param>
    /// <returns>Is Longer.</returns>
    public static bool IsLongerThan(float time)
    {
        //if (IsInHighEndDuty) return true;
        return AverageTTK > time;
    }

    /// <summary>
    /// How long each mob has been in combat.
    /// </summary>
    [Description("Mobs Time")]
    public static bool MobsTime => DataCenter.MobsTime;
    #endregion

    #region ChurinHelpers
    /// <summary>
    /// Simplified potion strategy for timing-based potion usage
    /// </summary>
    public enum PotionStrategy
    {
        /// <summary>
        /// 
        /// </summary>
        [Description("Use potions in the opener and at 6 minutes")] ZeroSix,

        /// <summary>
        /// 
        /// </summary>
        [Description("Use potions at 2 and 8 minutes")] TwoEight,

        /// <summary>
        /// 
        /// </summary>
        [Description("Use potions in the opener, at 5 minutes and at 10 minutes")] ZeroFiveTen,

        /// <summary>
        /// 
        /// </summary>
        [Description("Use custom potion timings")] Custom
    }

    /// <summary>
    /// Lightweight potion manager focused purely on condition tracking and timing presets.
    /// </summary>
    public class Potions
    {
        #region Fields and Properties

        /// <summary>
        /// Gets or sets the current potion strategy
        /// </summary>
        public PotionStrategy Strategy { get; set; } = PotionStrategy.ZeroSix;

        /// <summary>
        /// Gets or sets whether the potion system is enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the custom timing struct
        /// </summary>
        public CustomTimingsData CustomTimings { get; set; } = new CustomTimingsData();

        /// <summary>
        /// 
        /// </summary>
        public float OpenerPotionTime { get; set; }

        #endregion
        /// <summary>
        /// Struct to hold custom potion timings
        /// </summary>
        public struct CustomTimingsData
        {
            /// <summary>
            /// Gets or sets the array of timing values in seconds
            /// </summary>
            public float[] Timings { get; set; }

            /// <summary>
            /// Initializes a new instance of the CustomTimings struct
            /// </summary>
            public CustomTimingsData()
            {
                Timings = [];
            }
        }

        #region Preset Timing Arrays

        /// <summary>
        /// Gets the 0-6 minute rotation timing pattern
        /// </summary>
        protected static readonly float[] ZeroSixTimings =
        [
            0.0f,    // Pull
            360.0f,  // 6 minutes
        ];

        /// <summary>
        /// Gets the 0-5-10 minute rotation timing pattern
        /// </summary>
        protected static readonly float[] ZeroFiveTenTimings =
        [
            0.0f,    // Pull
            300.0f,  // 5 minutes
            600.0f,  // 10 minutes
        ];

        /// <summary>
        /// Gets the 2-8 minute rotation timing pattern
        /// </summary>
        protected static readonly float[] TwoEightTimings =
        [
            120.0f,  // 2 minutes
            480.0f,  // 8 minutes
        ];

        #endregion

        /// <summary>
        /// The timing window in seconds for potion usage alignment.
        /// Set to 59 seconds to provide a generous buffer for GCD timing variations,
        /// ensuring potions are used within the intended combat phase while accounting for slight delays.
        /// </summary>
        protected const float TimingWindowSeconds = 59.0f;

        #region Core Methods

        /// <summary>
        /// Main method to check if potion should be used based on conditions and timing
        /// </summary>
        /// <param name="rotation">The custom rotation instance</param>
        /// <param name="act">The action to use if potion usage is recommended</param>
        /// <returns>True if both conditions and timing are met for potion usage</returns>
        public bool ShouldUsePotion(CustomRotation rotation, out IAction? act)
        {
            act = null;

            if (!Enabled)
                return false;

            // Check if conditions are met for potion usage
            if (!IsConditionMet())
                return false;

            // Check if current time aligns with strategy timing
            if (!CanUseAtTime())
                return false;

            // Finally, attempt to use the burst medicine
            return IsConditionMet() && CanUseAtTime() && rotation.UseBurstMedicine(out act);
        }

        /// <summary>
        /// Checks if potion can be used at the current combat time.
        /// Validates timing against the configured strategy and windows.
        /// </summary>
        /// <returns>True if potion can be used at this time</returns>
        public virtual bool CanUseAtTime()
        {
            if (!Enabled)
                return false;

            // Null-safe check for custom timings: ensure array exists and has non-zero values (no LINQ)
            if (Strategy == PotionStrategy.Custom)
            {
                var timingsArr = CustomTimings.Timings;
                if (timingsArr == null || timingsArr.Length == 0)
                    return false;

                bool allZero = true;
                for (int i = 0; i < timingsArr.Length; i++)
                {
                    if (timingsArr[i] != 0f)
                    {
                        allZero = false;
                        break;
                    }
                }
                if (allZero)
                    return false;
            }

            // Get timing array based on strategy using extracted method
            float[] timings = GetTimingsArray();

            // Check if current time aligns with any timing window
            foreach (float timing in timings)
            {
                if (IsTimingValid(timing))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Validates current game state conditions for potion usage
        /// </summary>
        /// <returns>True if conditions are met for potion usage</returns>
        public virtual bool IsConditionMet()
        {
            if (!Enabled)
                return false;

            // Basic condition checks - override in derived classes for job-specific logic
            return true;
        }

        /// <summary>
        /// Checks if the specified timing represents an opener potion (timing == 0)
        /// </summary>
        /// <param name="timing">The potion timing to check</param>
        /// <returns>True if the timing is 0 (opener), false otherwise</returns>
        public static bool IsOpenerPotion(float timing) => timing == 0.0f;

        /// <summary>
        /// Gets the timing array based on the current strategy.
        /// This method encapsulates the strategy-to-timings mapping to eliminate duplication.
        /// </summary>
        /// <returns>Array of timing values in seconds for the current strategy.</returns>
        protected float[] GetTimingsArray()
        {
            return Strategy switch
            {
                PotionStrategy.ZeroSix => ZeroSixTimings,
                PotionStrategy.TwoEight => TwoEightTimings,
                PotionStrategy.ZeroFiveTen => ZeroFiveTenTimings,
                PotionStrategy.Custom => CustomTimings.Timings ?? [],
                _ => ZeroSixTimings
            };
        }

        /// <summary>
        /// Checks if the given timing is valid for potion usage at the current combat time.
        /// Validates both combat timing windows and opener countdown conditions.
        /// Virtual to allow job-specific timing logic overrides (e.g., DNC's more lenient >= timing check).
        /// </summary>
        /// <param name="timing">The timing value to check in seconds.</param>
        /// <returns>True if the timing allows potion usage.</returns>
        protected virtual bool IsTimingValid(float timing)
        {
            // Check combat timing window: timing must be positive, current time must be past the timing,
            // and within the allowed window to account for GCD variations
            if (timing > 0f
            && DataCenter.CombatTimeRaw >= timing
            && (DataCenter.CombatTimeRaw - timing) <= TimingWindowSeconds)
            {
                return true;
            }

            // Check opener timing: if it's an opener potion and countdown is within configured time
            float countDown = Service.CountDownTime;
            if (IsOpenerPotion(timing) && countDown <= OpenerPotionTime && !InCombat)
            {
                return true;
            }

            return false;
        }

        #endregion
    }

    #endregion

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

    #region GCD

    /// <summary>
    /// 
    /// </summary>
    protected static float WeaponRemain => DataCenter.DefaultGCDRemain;

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

    #region Territory Info

    /// <summary>
    /// Type of the content player is in.
    /// </summary>
    protected static TerritoryContentType TerritoryContentType => DataCenter.Territory?.ContentType ?? TerritoryContentType.None;

    /// <summary>
    /// Is player in high-end duty, savage, extrene or ultimate.
    /// </summary>
    [Description("Is in the high-end duty")]
    public static bool IsInHighEndDuty => DataCenter.Territory?.IsHighEndDuty ?? false;

    /// <summary>
    /// Is player in a normal or chaotic Alliance Raid.
    /// </summary>
    [Description("Is in an Alliance Raid (including Chaotic)")]
    public static bool IsInAllianceRaid => DataCenter.IsInAllianceRaid;

    /// <summary>
    /// Is player in UCoB duty.
    /// </summary>
    [Description("Is in UCoB duty")]
    public static bool IsInUCoB => DataCenter.IsInUCoB;

    /// <summary>
    /// Is player in UwU duty.
    /// </summary>
    [Description("Is in UwU duty")]
    public static bool IsInUwU => DataCenter.IsInUwU;

    /// <summary>
    /// Is player in TEA duty.
    /// </summary>
    [Description("Is in TEA duty")]
    public static bool IsInTEA => DataCenter.IsInTEA;

    /// <summary>
    /// Is player in DSR duty.
    /// </summary>
    [Description("Is in DSR duty")]
    public static bool IsInDSR => DataCenter.IsInDSR;

    /// <summary>
    /// Is player in TOP duty.
    /// </summary>
    [Description("Is in TOP duty")]
    public static bool IsInTOP => DataCenter.IsInTOP;

    ///<summary>
    /// Is player in FRU duty.
    ///</summary>
    [Description("Is in FRU duty")]
    public static bool IsInFRU => DataCenter.IsInFRU;

    ///<summary>
    /// Is player in COD duty.
    ///</summary>
    [Description("Is in FRU duty")]
    public static bool IsInCOD => DataCenter.IsInCOD;

    /// <summary>
    /// Is player in any instanced duty.
    /// </summary>
    [Description("Is player in duty")]
    public static bool IsInDuty => DataCenter.IsInDuty;

    /// <summary>
    /// Is in specified territory.
    /// </summary>
    /// <param name="territoryId">The ID of the territory to check.</param>
    /// <returns>True if the player is in the specified territory; otherwise, false.</returns>
    [Description("Is in specified territory")]
    public static bool IsInTerritory(ushort territoryId) => DataCenter.IsInTerritory(territoryId);

    #endregion

    /// <summary>
    /// Average amount of times a rotation calls IsLastGCD, IsLastAbility, or IsLastAction.
    /// </summary>
    public double AverageCountOfLastUsing { get; internal set; } = 0;

    /// <summary>
    /// Max amount of times a rotation calls IsLastGCD, IsLastAbility, or IsLastAction.
    /// </summary>
    public int MaxCountOfLastUsing { get; internal set; } = 0;

    /// <summary>
    /// The average count of not recommend members using.
    /// </summary>
    public double AverageCountOfCombatTimeUsing { get; internal set; } = 0;

    /// <summary>
    /// The max count of not recommend members using.
    /// </summary>
    public int MaxCountOfCombatTimeUsing { get; internal set; } = 0;
    internal long CountOfTracking { get; set; } = 0;

    internal static int CountingOfLastUsing { get; set; } = 0;
    internal static int CountingOfCombatTimeUsing { get; set; } = 0;


    /// <summary>
    ///  The actions that were used by player successfully. The first one is the latest successfully used one.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    protected static ActionRec[] RecordActions
    {
        get
        {
            CountingOfLastUsing++;
            return DataCenter.RecordActions;
        }
    }

    /// <summary>
    /// How much time has passed since the last action was used.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    public static TimeSpan TimeSinceLastAction
    {
        get
        {
            CountingOfLastUsing++;
            return DataCenter.TimeSinceLastAction;
        }
    }

    /// <summary>
    /// Last used GCD.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    /// <param name="isAdjust">Check for adjust id not raw id.</param>
    /// <param name="actions">True if any of this is matched.</param>
    /// <returns></returns>
    [Description("Just used GCD")]
    public static bool IsLastGCD(bool isAdjust, params IAction[] actions)
    {
        CountingOfLastUsing++;
        return IActionHelper.IsLastGCD(isAdjust, actions);
    }

    /// <summary>
    /// Last used GCD.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    /// <param name="ids">True if any of this is matched.</param>
    /// <returns></returns>
    public static bool IsLastGCD(params ActionID[] ids)
    {
        CountingOfLastUsing++;
        return IActionHelper.IsLastGCD(ids);
    }

    /// <summary>
    /// Last used Ability.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    /// <param name="isAdjust">Check for adjust id not raw id.</param>
    /// <param name="actions">True if any of this is matched.</param>
    /// <returns></returns>
    [Description("Just used Ability")]
    public static bool IsLastAbility(bool isAdjust, params IAction[] actions)
    {
        CountingOfLastUsing++;
        return IActionHelper.IsLastAbility(isAdjust, actions);
    }

    /// <summary>
    /// Last used Ability.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    /// <param name="ids">True if any of this is matched.</param>
    /// <returns></returns>
    public static bool IsLastAbility(params ActionID[] ids)
    {
        CountingOfLastUsing++;
        return IActionHelper.IsLastAbility(ids);
    }

    /// <summary>
    /// Last used Action.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    /// <param name="isAdjust">Check for adjust id not raw id.</param>
    /// <param name="actions">True if any of this is matched.</param>
    /// <returns></returns>
    [Description("Just used Action")]
    public static bool IsLastAction(bool isAdjust, params IAction[] actions)
    {
        CountingOfLastUsing++;
        return IActionHelper.IsLastAction(isAdjust, actions);
    }

    /// <summary>
    /// Last used Action.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    /// <param name="ids">True if any of this is matched.</param>
    /// <returns></returns>
    public static bool IsLastAction(params ActionID[] ids)
    {
        CountingOfLastUsing++;
        return IActionHelper.IsLastAction(ids);
    }

    /// <summary>
    /// Last used Combo Action.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    /// <param name="isAdjust">Check for adjust id not raw id.</param>
    /// <param name="actions">True if any of this is matched.</param>
    /// <returns></returns>
    [Description("Just used Combo Action")]
    public static bool IsLastComboAction(bool isAdjust, params IAction[] actions)
    {
        CountingOfLastUsing++;
        return IActionHelper.IsLastComboAction(isAdjust, actions);
    }

    /// <summary>
    /// Last used Combo Action.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    /// <param name="ids">True if any of this is matched.</param>
    /// <returns></returns>
    public static bool IsLastComboAction(params ActionID[] ids)
    {
        CountingOfLastUsing++;
        return IActionHelper.IsLastComboAction(ids);
    }

    /// <summary>
    ///
    /// </summary>
    public static bool IsNoActionCombo()
    {
        return IActionHelper.IsNoActionCombo();
    }

    /// <summary>
    /// Have you already weaved an oGCD.
    /// </summary>
    public static bool HasWeaved()
    {
        // Returns true if the last action and last ability are the same (i.e., an oGCD was weaved).
        // Returns false otherwise.
        return IsLastAction() == IsLastAbility();
    }

    /// <summary>
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    /// <param name="GCD"></param>
    /// <returns></returns>
    protected static bool CombatElapsedLessGCD(int GCD)
    {
        CountingOfCombatTimeUsing++;
        return CombatElapsedLess(GCD * DataCenter.DefaultGCDTotal);
    }

    /// <summary>
    /// Whether the battle lasted less than <paramref name="time"/> seconds
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    /// <param name="time">time in second.</param>
    /// <returns></returns>
    protected static bool CombatElapsedLess(float time)
    {
        CountingOfCombatTimeUsing++;
        return CombatTime <= time;
    }

    /// <summary>
    /// How long combat has been going.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    [Description("Combat time")]
    public static float CombatTime
    {
        get
        {
            CountingOfCombatTimeUsing++;
            return InCombat ? DataCenter.CombatTimeRaw + DataCenter.DefaultGCDRemain : 0;
        }
    }

    /// <summary>
    /// How long is remaining on the Combo Timer.
    /// <br>WARNING: Do not make this method the main logic of your rotation.</br>
    /// </summary>
    [Description("Combo time")]
    public static float LiveComboTime
    {
        get
        {
            try
            {
                return DataCenter.ComboTime;
            }
            catch (Exception)
            {
                return 0f;
            }
        }
    }

    /// <summary>
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    /// <param name="GCD"></param>
    /// <returns></returns>
    protected static bool StopMovingElapsedLessGCD(int GCD)
    {
        return StopMovingElapsedLess(GCD * DataCenter.DefaultGCDTotal);
    }

    /// <summary>
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    /// <param name="time">time in second.</param>
    /// <returns></returns>
    protected static bool StopMovingElapsedLess(float time)
    {
        return StopMovingTime <= time;
    }

    /// <summary>
    /// How long the player has been standing still.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    [Description("Stop moving time")]
    public static float StopMovingTime => IsMoving ? 0 : DataCenter.StopMovingRaw + DataCenter.DefaultGCDRemain;

    /// <summary>
    /// How long the player has been moving.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    [Description("Moving time")]
    public static float MovingTime => IsMoving ? DataCenter.MovingRaw + DataCenter.DefaultGCDRemain : 0;
    /// <summary>
    /// How long the player has been alive.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    [Description("How long the player has been alive.")]
    public static float AliveTime => ObjectHelper.PlayerIsAlive() ? DataCenter.AliveTimeRaw + DataCenter.DefaultGCDRemain : 0;

    /// <summary>
    /// How long the player has been dead.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    [Description("How long the player has been dead.")]
    public static float DeadTime => ObjectHelper.PlayerIsAlive() ? 0 : DataCenter.DeadTimeRaw + DataCenter.DefaultGCDRemain;

    /// <summary>
    /// Time from GCD.
    /// </summary>
    /// <param name="gcdCount"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    protected static float GCDTime(uint gcdCount = 0, float offset = 0)
    {
        return DataCenter.GCDTime(gcdCount, offset);
    }

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
