using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Logging;
using RotationSolver.Rotations.Limited.BlueMage;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using Action = Lumina.Excel.Sheets.Action;
using CombatRole = RotationSolver.Engine.Data.CombatRole;

namespace RotationSolver.Engine.State;

internal static partial class DataCenter
{
	#region HP

	private static readonly Dictionary<ulong, float> _refinedHpCache = [];
	private static long _refinedHpCacheTick = long.MinValue;

	private const long RefinedHpTtlMs = 15;

	public static Dictionary<ulong, float> RefinedHP
	{
		get
		{
			var now = Environment.TickCount64;
			if (_refinedHpCacheTick != long.MinValue && now - _refinedHpCacheTick < RefinedHpTtlMs)
			{
				return _refinedHpCache;
			}

			_refinedHpCache.Clear();
			foreach (var member in PartyMembers)
			{
				if (member == null || member.GameObjectId == 0)
				{
					continue;
				}

				_refinedHpCache[member.GameObjectId] = GetPartyMemberHPRatio(member);
			}

			_refinedHpCacheTick = now;
			return _refinedHpCache;
		}
	}

	private static readonly Dictionary<ulong, uint> _lastHp = [];

	private static float GetPartyMemberHPRatio(IBattleChara member)
	{
		ArgumentNullException.ThrowIfNull(member);

		if (member.MaxHp == 0)
		{
			return 0f;
		}

		var id = member.GameObjectId;

		if (!InEffectTime || !HealHP.TryGetValue(id, out var healedHp))
		{
			_lastHp[id] = member.CurrentHp;
			return (float)member.CurrentHp / member.MaxHp;
		}

		var currentHp = member.CurrentHp;
		if (currentHp > 0)
		{
			_ = _lastHp.TryGetValue(id, out var lastHp);

			if (HealPrediction.HasLanded(currentHp, ref lastHp, healedHp))
			{
				_ = HealHP.Remove(id);
				_lastHp[id] = currentHp;
				return (float)currentHp / member.MaxHp;
			}

			_lastHp[id] = lastHp;

			return Math.Min(1, (healedHp + currentHp) / (float)member.MaxHp);
		}

		return (float)currentHp / member.MaxHp;
	}

	private static readonly float[] _hpBuffer = new float[16];
	private static long _partyHpStatsCacheTick = long.MinValue;
	private static float _minHpCache, _avgHpCache, _stdDevHpCache, _lowestAvgHpCache, _lowestStdDevHpCache;
	private const long PartyHpStatsTtlMs = 15;

	private static void ComputePartyHpStats(out float minHp, out float avgHp, out float stdDevHp, out float lowestAvgHp, out float lowestStdDevHp)
	{
		var now = Environment.TickCount64;
		if (_partyHpStatsCacheTick != long.MinValue && now - _partyHpStatsCacheTick < PartyHpStatsTtlMs)
		{
			minHp = _minHpCache;
			avgHp = _avgHpCache;
			stdDevHp = _stdDevHpCache;
			lowestAvgHp = _lowestAvgHpCache;
			lowestStdDevHp = _lowestStdDevHpCache;
			return;
		}

		var hpCount = 0;
		foreach (var member in PartyMembers)
		{
			if (member.GameObjectId != 0 && hpCount < _hpBuffer.Length)
			{
				var hp = GetPartyMemberHPRatio(member);
				if (hp > 0)
				{
					_hpBuffer[hpCount++] = hp;
				}
			}
		}

		if (hpCount == 0)
		{
			minHp = avgHp = stdDevHp = lowestAvgHp = lowestStdDevHp = 0;
			_minHpCache = _avgHpCache = _stdDevHpCache = _lowestAvgHpCache = _lowestStdDevHpCache = 0;
			_partyHpStatsCacheTick = now;
			return;
		}

		if (hpCount > 4)
		{
			Array.Sort(_hpBuffer, 0, hpCount);
		}

		float sum = 0;
		float lowestHpMembersSum = 0;
		var min = float.MaxValue;
		for (var i = 0; i < hpCount; i++)
		{
			sum += _hpBuffer[i];
			if (i < 4)
			{
				lowestHpMembersSum += _hpBuffer[i];
			}

			if (_hpBuffer[i] < min)
			{
				min = _hpBuffer[i];
			}
		}

		var avg = sum / hpCount;
		var lowestHpMembersAvg = lowestHpMembersSum / (hpCount > 4 ? 4 : hpCount);
		float variance = 0;
		float lowestHpMembersVariance = 0;
		for (var i = 0; i < hpCount; i++)
		{
			var diff = _hpBuffer[i] - avg;
			variance += diff * diff;
			if (i < 4)
			{
				var lowestHpMembersDiff = _hpBuffer[i] - lowestHpMembersAvg;
				lowestHpMembersVariance += lowestHpMembersDiff * lowestHpMembersDiff;
			}
		}

		minHp = min;
		avgHp = avg;
		stdDevHp = (float)Math.Sqrt(variance / hpCount);
		lowestAvgHp = lowestHpMembersAvg;
		lowestStdDevHp = (float)Math.Sqrt(lowestHpMembersVariance / (hpCount > 4 ? 4 : hpCount));

		_minHpCache = minHp;
		_avgHpCache = avgHp;
		_stdDevHpCache = stdDevHp;
		_lowestAvgHpCache = lowestAvgHp;
		_lowestStdDevHpCache = lowestStdDevHp;
		_partyHpStatsCacheTick = now;
	}

	public static float PartyMembersMinHP
	{
		get
		{
			ComputePartyHpStats(out var minHp, out _, out _, out _, out _);
			return minHp;
		}
	}

	public static float PartyMembersAverHP
	{
		get
		{
			ComputePartyHpStats(out _, out var avgHp, out _, out _, out _);
			return avgHp;
		}
	}

	public static float PartyMembersDifferHP
	{
		get
		{
			ComputePartyHpStats(out _, out _, out var stdDevHp, out _, out _);
			return stdDevHp;
		}
	}

	public static float LowestPartyMembersAverHP
	{
		get
		{
			ComputePartyHpStats(out _, out _, out _, out var lowestAvgHp, out _);
			return lowestAvgHp;
		}
	}

	public static float LowestPartyMembersDifferHP
	{
		get
		{
			ComputePartyHpStats(out _, out _, out _, out _, out var lowestStdDevHp);
			return lowestStdDevHp;
		}
	}

	public static IEnumerable<float> PartyMembersHP
	{
		get
		{
			foreach (var member in PartyMembers)
			{
				if (member == null || member.GameObjectId == 0)
				{
					continue;
				}

				var hp = GetPartyMemberHPRatio(member);
				if (hp > 0)
				{
					yield return hp;
				}
			}
		}
	}

	public static bool HPNotFull => PartyMembersMinHP < 1;

	public static uint CurrentMp => Player.Object != null ? Math.Min(10000, Player.Object.CurrentMp + MPGain) : MPGain;
	#endregion

	#region Action Record
	private const int QUEUECAPACITY = 48;
	private static readonly Queue<ActionRec> _actions = new(QUEUECAPACITY);
	private static readonly Queue<DamageRec> _damages = new(QUEUECAPACITY);

	internal static CombatRole? BluRole => CurrentRotation is BlueMageRotation ? BlueMageRotation.BlueId : null;
	public static float DPSTaken
	{
		get
		{
			try
			{
				var now = DateTime.Now;
				float damages = 0;
				DateTime? first = null, last = null;
				foreach (var rec in _damages)
				{
					if (now - rec.ReceiveTime < TimeSpan.FromSeconds(5))
					{
						damages += rec.Ratio;
						first ??= rec.ReceiveTime;
						last = rec.ReceiveTime;
					}
				}

				if (first == null || last == null)
				{
					return 0;
				}

				var time = last.Value - first.Value + TimeSpan.FromSeconds(2.5);
				return damages / (float)time.TotalSeconds;
			}
			catch
			{
				return 0;
			}
		}
	}

	public static ActionRec[] RecordActions
	{
		get
		{
			var arr = new ActionRec[_actions.Count];
			var i = _actions.Count - 1;
			foreach (var rec in _actions)
			{
				arr[i--] = rec;
			}
			return arr;
		}
	}
	private static DateTime _timeLastActionUsed = DateTime.Now;
	public static TimeSpan TimeSinceLastAction => DateTime.Now - _timeLastActionUsed;

	public static ActionID LastAction { get; private set; } = 0;

	public static ActionID LastGCD { get; private set; } = 0;

	public static ActionID LastAbility { get; private set; } = 0;

	internal static unsafe void AddActionRec(Action act)
	{
		var id = (ActionID)act.RowId;

		switch (act.GetActionCate())
		{
			case ActionCate.Spell:
			case ActionCate.Weaponskill:
				LastAction = LastGCD = id;
				break;
			case ActionCate.Ability:
				LastAction = LastAbility = id;
				break;
			default:
				return;
		}

		if (_actions.Count >= QUEUECAPACITY)
		{
			_ = _actions.Dequeue();
		}

		_timeLastActionUsed = DateTime.Now;
		_actions.Enqueue(new ActionRec(_timeLastActionUsed, act));
	}

	private static readonly Dictionary<uint, long> _actionRejectedTicks = [];

	internal static void AddActionRejected(uint actionId)
	{
		_actionRejectedTicks[actionId] = Environment.TickCount64;
	}

	internal static bool WasActionRejectedRecently(uint actionId, long withinMs)
	{
		return _actionRejectedTicks.TryGetValue(actionId, out var tick) && Environment.TickCount64 - tick < withinMs;
	}

	internal static void ResetAllRecords()
	{
		LastAction = 0;
		LastGCD = 0;
		LastAbility = 0;
		_avgTTK = 0;
		_avgTTKCacheTick = long.MinValue;
		TimeToKillTracker.Clear();
		DotTickTracker.Clear();
		_partyHpStatsCacheTick = long.MinValue;
		_timeLastActionUsed = DateTime.Now;
		_actions.Clear();
		_actionRejectedTicks.Clear();

		AttackedTargets.Clear();
		while (VfxDataQueue.TryDequeue(out _))
		{ }
		lock (_tankbusterLock)
		{
			TankbusterTargetIds.Clear();
		}
		AllHostileTargets.Clear();
		AllHostileTargetIds.Clear();
		AllianceMembers.Clear();
		PartyMembers.Clear();
		AllTargets.Clear();
		TargetsByRange.Clear();
	}

	internal static void AddDamageRec(float damageRatio)
	{
		if (_damages.Count >= QUEUECAPACITY)
		{
			_ = _damages.Dequeue();
		}

		_damages.Enqueue(new DamageRec(DateTime.Now, damageRatio));
	}

	internal static DateTime KnockbackFinished { get; set; } = DateTime.MinValue;
	internal static DateTime KnockbackStart { get; set; } = DateTime.MinValue;

	#endregion

	#region Hostile Range
	public static bool HasHostilesInRange => NumberOfHostilesInRange > 0;
	public static bool HasHostilesInMaxRange => NumberOfHostilesInMaxRange > 0;
	public static int NumberOfHostilesInRange
	{
		get
		{
			var jobRange = JobRange;
			var targets = AllHostileTargets;
			var count = 0;
			for (int i = 0, n = targets.Count; i < n; i++)
			{
				if (targets[i].DistanceToPlayer() < jobRange)
				{
					count++;
				}
			}
			return count;
		}
	}
	public static int NumberOfHostilesInMaxRange
	{
		get
		{
			var targets = AllHostileTargets;
			var count = 0;
			for (int i = 0, n = targets.Count; i < n; i++)
			{
				if (targets[i].DistanceToPlayer() < 25)
				{
					count++;
				}
			}
			return count;
		}
	}
	public static int NumberOfHostilesInRangeOf(float range)
	{
		var targets = AllHostileTargets;
		var count = 0;
		for (int i = 0, n = targets.Count; i < n; i++)
		{
			if (targets[i].DistanceToPlayer() < range)
			{
				count++;
			}
		}
		return count;
	}

	public static int NumberOfPartyMembersInRangeOf(float range)
	{
		var targets = PartyMembers;
		var count = 0;
		for (int i = 0, n = targets.Count; i < n; i++)
		{
			if (targets[i].DistanceToPlayer() < range)
			{
				count++;
			}
		}
		return count;
	}
	public static int NumberOfAllHostilesInRange => NumberOfHostilesInRange;
	public static int NumberOfAllHostilesInMaxRange => NumberOfHostilesInMaxRange;
	#endregion

	#region Hostile Casting

	/// <summary>
	/// Determines whether any hostile is casting an action whose attack type is magical (<c>AttackType.RowId == 5</c>).
	/// </summary>
	public static bool IsMagicalDamageIncoming()
	{
		return AnyHostileCasting(static action => action.AttackType.RowId == 5);
	}

	/// <summary>
	/// Determines whether any hostile is casting a physical action (slashing, piercing, blunt or shot).
	/// </summary>
	public static bool IsPhysicalDamageIncoming()
	{
		return AnyHostileCasting(static action => action.AttackType.RowId is >= 1 and <= 4);
	}

	private static bool AnyHostileCasting(Func<Action, bool> check)
	{
		return AnyHostile(h => h != null && h.CastActionId != 0 && TryGetCastAction(h, out var action) && check(action));
	}

	/// <summary>
	/// True if any hostile in M10S is currently casting action 46553 or 46554.
	/// </summary>
	public static bool IsExtremeCastingSpecialIndicator()
	{
		return IsInM10S && AnyHostile(static h => h?.CastActionId is 46553 or 46554);
	}

	/// <summary>
	/// True if any hostile in M11S is currently casting Charybdistopia (46117).
	/// </summary>
	public static bool IsTyrantCastingSpecialIndicator()
	{
		return IsInM11S && AnyHostile(static h => h?.CastActionId == 46117);
	}

	/// <summary>
	/// True from the moment the Tyrant has cast both Scythe (46115) and Axe (46114) until its next Charybdistopia (46117) finishes.
	/// </summary>
	public static bool IsTyrantCastingSpecialIndicator2()
	{
		if (!IsInM11S || CombatTimeRaw == 0)
		{
			_hasCastScythe = false;
			_hasCastAxe = false;
			_wasCastingCharyb = false;
			_tyrantShouldStopHealing = false;
			return false;
		}

		var anyCurrentlyCastingCharyb = false;
		foreach (var hostile in AllHostileTargets)
		{
			if (hostile == null || !hostile.IsCasting)
			{
				continue;
			}

			switch (hostile.CastActionId)
			{
				case 46115:
					_hasCastScythe = true;
					break;
				case 46114:
					_hasCastAxe = true;
					break;
				case 46117:
					anyCurrentlyCastingCharyb = true;
					_wasCastingCharyb = true;
					break;
			}
		}

		if (_hasCastScythe && _hasCastAxe)
		{
			_tyrantShouldStopHealing = true;
		}

		if (_wasCastingCharyb && !anyCurrentlyCastingCharyb)
		{
			_hasCastScythe = false;
			_hasCastAxe = false;
			_wasCastingCharyb = false;
			_tyrantShouldStopHealing = false;
		}

		return _tyrantShouldStopHealing;
	}

	/// <summary>
	/// True while Agrias's Judgment Blade (14423) in Orbonne resolves within 2.5 seconds.
	/// </summary>
	public static bool IsAgriasCastingSpecialIndicator()
	{
		return Orbonne && AnyHostile(static h => IsCastResolving(h, 2.5f, "Agrias", 14423));
	}

	/// <summary>
	/// True while the Lich's Alexandrian Quake (46419, 46427) in the quest battle resolves within 3 seconds.
	/// </summary>
	public static bool IsLichCastingSpecialIndicator()
	{
		return IsInQuestBattle && AnyHostile(static h => IsCastResolving(h, 3f, "Lich", 46419, 46427));
	}

	/// <summary>
	/// True while one of Lakshmi's raid-wide casts resolves within 3 seconds. In the Extreme version, The Path of Light (8539)
	/// and The Pull of Light (8543) only count while she has Chanchala.
	/// </summary>
	public static bool IsLakshmiCastingSpecialIndicator()
	{
		if (Emanation)
		{
			return AnyHostile(static h => IsCastResolving(h, 3f, "Lakshmi", 9349));
		}

		return EmanationEX && AnyHostile(static h => IsCastResolving(h, 3f, "Lakshmi", 8519, 8521, 8522, 8523)
			|| (h != null && h.HasStatus(false, StatusID.Chanchala_1410) && IsCastResolving(h, 3f, "Lakshmi", 8539, 8543)));
	}

	private static bool IsCastResolving(IBattleChara? hostile, float seconds, string source, params ReadOnlySpan<uint> castIds)
	{
		if (hostile == null || !hostile.IsCasting || !castIds.Contains(hostile.CastActionId))
		{
			return false;
		}

		var remaining = hostile.TotalCastTime - hostile.CurrentCastTime;
		if (remaining > seconds)
		{
			return false;
		}

		if (Service.Config.InDebug)
		{
			PluginLog.Debug($"{source} cast detected on obj {hostile.GameObjectId} - remaining: {remaining:F3}s, GCD remain: {DefaultGCDRemain:F3}s");
		}

		return true;
	}

	private static readonly FrozenSet<string> TankbusterPaths = FrozenSet.ToFrozenSet(
	[
		"vfx/lockon/eff/tank_lockon",
		"vfx/lockon/eff/tank_laser",
		"vfx/lockon/eff/sharelaser2tank5sec_c0k1",
		"vfx/lockon/eff/sharelaser2tank8sec_c0p",
		"vfx/lockon/eff/x6fe_fan100_50_0t1",     // Necron Blue Shockwave - Cone Tankbuster
		//"vfx/common/eff/mon_eisyo03t",           // M10 Deep Impact AoE TB need different path for this, this is the generic target vfx part
		"vfx/lockon/eff/m0676trg_tw_d0t1p",      // M10 Hot Impact shared TB
		"vfx/lockon/eff/m0676trg_tw_s6_d0t1p",   // M11 Raw Steel
		"vfx/lockon/eff/z6r2b3_8sec_lockon_c0a1",// Kam'lanaut Princely Blow
		"vfx/lockon/eff/m0742trg_b1t1",          // M7 Abominable Blink
		"vfx/lockon/eff/x6r9_tank_lockonae",      // M9 Hardcore Large TB
		"vfx/lockon/eff/z6r2b3_8sec_lockon_c0a1",  // Tankbuster line cleave knockback
		"vfx/lockon/eff/m0926trg_t0a1"
	], StringComparer.OrdinalIgnoreCase);

	private static readonly FrozenSet<string> MultiHitSharedPaths = FrozenSet.ToFrozenSet(
	[
		"vfx/lockon/eff/com_share4a1",
		"vfx/lockon/eff/com_share5a1",
		"vfx/lockon/eff/com_share6m7s_1v",
		"vfx/lockon/eff/com_share8s_0v",
		"vfx/lockon/eff/share_laser_5s_c0w",     // Line
		"vfx/lockon/eff/share_laser_8s_c0g",     // Line
		"vfx/lockon/eff/m0922trg_t2w"
	], StringComparer.OrdinalIgnoreCase);

	private static readonly FrozenSet<string> SharedDamagePaths = FrozenSet.ToFrozenSet(
	[
		"vfx/lockon/eff/coshare",
		"vfx/lockon/eff/share_laser",
		"vfx/lockon/eff/com_share",
		"vfx/lockon/eff/share_10s_6m_0w",
		"vfx/lockon/eff/share_12s_6m_t1",
		"vfx/lockon/eff/share_14s_6m_t1",
		"vfx/lockon/eff/com_trg01_0c",
		"vfx/lockon/eff/com_trg02_0c",
		"vfx/lockon/eff/com_trg01_0c",
		"vfx/lockon/eff/x6r9_loc01_t0a1",
		"vfx/lockon/eff/x6r9_loc02_t0a1",
		// Duty-specific AOE share markers
		"vfx/lockon/eff/m0982trg_g0c",
		"vfx/lockon/eff/m0906_share4_7s0k2",
		"vfx/lockon/eff/x6fd_share_4m_5s_c0v", //Zelenia EX party stack
		"vfx/lockon/eff/bahamut_kakyu_target_t01i", // UCOB, party stack.
		"vfx/monster/gimmick2/eff/z3o7_b1_g06c0t", // Puppet's Bunker, Superior Flight Unit.
		"vfx/monster/gimmick4/eff/z5r1_b4_g09c0c"  // Aglaia, Nald'thal
	], StringComparer.OrdinalIgnoreCase);

	private static readonly FrozenSet<string> SpreadDamagePaths = FrozenSet.ToFrozenSet(
	[
		"vfx/lockon/eff/x6r9_loc01_t0a1",
		"vfx/lockon/eff/x6r9_loc02_t0a1",
		// Duty-specific AOE share markers
		"vfx/lockon/eff/x6fd_loc04m_5s1v",
		"vfx/lockon/eff/m0922tar_a0w"
	], StringComparer.OrdinalIgnoreCase);

	private static readonly StringComparison PathCmp = StringComparison.OrdinalIgnoreCase;
	internal static bool IsTrackedVfxPath(string path)
	{
		return path.StartsWith("vfx/lockon/", PathCmp) || StartsWithAny(path, SharedDamagePaths) || StartsWithAny(path, MultiHitSharedPaths)
			|| StartsWithAny(path, SpreadDamagePaths) || StartsWithAny(path, TankbusterPaths);
	}

	private static bool StartsWithAny(string path, FrozenSet<string> prefixes)
	{
		foreach (var p in prefixes)
		{
			if (path.StartsWith(p, PathCmp))
			{
				return true;
			}
		}

		return false;
	}

	private static readonly FrameCached<bool> _isHostileCastingAoe = new(() => InCombat && (IsCastingAreaVfx() || AnyHostile(IsHostileCastingArea)));
	private static readonly FrameCached<bool> _isHostileCastingToTank = new(IsHostileCastingToTankNow);
	private static readonly FrameCached<bool> _isHostileCastingStop = new(() => InCombat && Service.Config.CastingStop && AnyHostile(IsHostileStop));
	private static readonly FrameCached<bool> _areHostilesCastingKnockback = new(() => AnyHostile(IsHostileCastingKnockback));

	public static bool IsHostileCastingAOE => _isHostileCastingAoe.Value;

	public static bool IsHostileCastingToTank => _isHostileCastingToTank.Value;

	public static bool IsHostileCastingStop => _isHostileCastingStop.Value;

	public static bool AreHostilesCastingKnockback => _areHostilesCastingKnockback.Value;

	private static bool IsHostileCastingToTankNow()
	{
		if (!InCombat)
		{
			lock (_tankbusterLock)
			{
				TankbusterTargetIds.Clear();
			}

			return false;
		}

		return IsCastingTankVfx() || AnyHostile(IsHostileCastingTank);
	}

	private static bool AnyHostile(Func<IBattleChara, bool> predicate)
	{
		var targets = AllHostileTargets;
		for (var i = 0; i < targets.Count; i++)
		{
			if (predicate(targets[i]))
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsHostileStop(IBattleChara h)
	{
		return IsHostileCastingStopBase(h, static act => OtherConfiguration.HostileCastingStop.Contains(act.RowId));
	}

	public static bool IsHostileCastingStopBase(IBattleChara h, Func<Action, bool> check)
	{
		if (h == null || check == null || h.GameObjectId == 0)
		{
			return false;
		}

		if (!h.IsCasting || h.IsCastInterruptible)
		{
			return false;
		}

		if ((h.TotalCastTime - h.CurrentCastTime) > (Service.Config.CastingStopCalculate ? 100 : Service.Config.CastingStopTime))
		{
			return false;
		}

		return TryGetCastAction(h, out var action) && check(action);
	}

	private static bool TryGetCastAction(IBattleChara h, out Action action)
	{
		var row = Service.GetSheet<Action>().GetRowOrDefault(h.CastActionId);
		action = row ?? default;
		return row is { RowId: not 0 };
	}

	public static bool IsCastingVfx(ConcurrentQueue<VfxNewData> vfxData, Func<VfxNewData, bool> isVfx)
	{
		if (vfxData == null || vfxData.IsEmpty)
		{
			return false;
		}

		foreach (var data in vfxData)
		{
			if (isVfx(data))
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsCastingMultiHit()
	{
		return IsCastingVfx(VfxDataQueue, s =>
		{
			if (!Player.Available || Player.Object == null)
			{
				return false;
			}

			if (string.IsNullOrEmpty(s.Path))
			{
				return false;
			}

			foreach (var p in MultiHitSharedPaths)
			{
				if (s.Path.StartsWith(p, PathCmp))
				{
					return true;
				}
			}

			return false;
		});
	}

	public static bool IsCastingTankVfx()
	{
		lock (_tankbusterLock)
		{
			TankbusterTargetIds.Clear();
			if (!Player.Available || Player.Object == null)
			{
				return false;
			}

			if (VfxDataQueue == null || VfxDataQueue.IsEmpty)
			{
				return false;
			}

			var found = false;
			var isTank = TargetFilter.PlayerJobCategory(JobRole.Tank);

			foreach (var s in VfxDataQueue)
			{
				if (string.IsNullOrEmpty(s.Path))
				{
					continue;
				}

				foreach (var p in TankbusterPaths)
				{
					if (!s.Path.StartsWith(p, PathCmp))
					{
						continue;
					}

					var isPlayerTarget = s.ObjectId == Player.Object.GameObjectId;

					if (!isTank || isPlayerTarget)
					{
						if (Service.Config.InDebug)
						{
							PluginLog.Debug($"Tank lock-on VFX triggered: {s.Path}, ObjectId: {s.ObjectId}");
						}

						if (Svc.Objects.SearchById(s.ObjectId) is IBattleChara obj && obj.IsParty() && !obj.IsDead && !TankbusterTargetIds.Contains(obj.GameObjectId))
						{
							TankbusterTargetIds.Add(obj.GameObjectId);
						}

						found = true;
					}
				}
			}

			return found;
		}
	}

	public static bool IsCastingAreaVfx()
	{
		return IsCastingVfx(VfxDataQueue, s =>
		{
			if (!Player.Available || Player.Object == null)
			{
				return false;
			}

			if (string.IsNullOrEmpty(s.Path))
			{
				return false;
			}

			foreach (var p in MultiHitSharedPaths)
			{
				if (s.Path.StartsWith(p, PathCmp))
				{
					return true;
				}
			}

			foreach (var p in SharedDamagePaths)
			{
				if (s.Path.StartsWith(p, PathCmp))
				{
					return true;
				}
			}

			foreach (var p in SpreadDamagePaths)
			{
				if (s.Path.StartsWith(p, PathCmp))
				{
					return true;
				}
			}

			return false;
		});
	}

	public static bool IsHostileCastingTank(IBattleChara h)
	{
		return h != null && IsHostileCastingBase(h, act => OtherConfiguration.HostileCastingTank.Contains(act.RowId)
			|| h.CastTargetObjectId == h.TargetObjectId);
	}

	public static bool IsHostileCastingArea(IBattleChara h)
	{
		return IsHostileCastingBase(h, static act => OtherConfiguration.HostileCastingArea.Contains(act.RowId));
	}

	public static bool IsHostileCastingKnockback(IBattleChara h)
	{
		return IsHostileCastingBase(h, static act => OtherConfiguration.HostileCastingKnockback.Contains(act.RowId));
	}

	public static bool IsHostileCastingBase(IBattleChara h, Func<Action, bool> check)
	{
		if (h == null || check == null || h.GameObjectId == 0)
		{
			return false;
		}

		unsafe
		{
			if (h.Struct() == null)
			{
				return false;
			}
		}

		if (!h.IsEnemy() || !h.IsCasting || h.IsCastInterruptible)
		{
			return false;
		}

		var t = h.TotalCastTime - h.CurrentCastTime - DefaultGCDTotal;
		if (!(h.TotalCastTime > DefaultGCDTotal && t > 0 && t < GCDTime(1)))
		{
			return false;
		}

		return TryGetCastAction(h, out var action) && check(action);
	}
	#endregion
}
