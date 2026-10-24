using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using Lumina.Excel.Sheets;
using RotationSolver.Basic.Configuration;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace RotationSolver.Basic.Helpers;

/// <summary>
/// Get the information from object.
/// </summary>
public static class ObjectHelper
{
	private static readonly EventHandlerContent[] _eventType =
	[
		EventHandlerContent.TreasureHuntDirector,
		EventHandlerContent.BattleLeveDirector,
		EventHandlerContent.CompanyLeveDirector,
		EventHandlerContent.Quest,
	];

	private static readonly ConcurrentDictionary<string, Regex> _regexCache = [];

	private static Regex GetCachedRegex(string pattern)
	{
		return _regexCache.GetOrAdd(pattern, p => new Regex(p, RegexOptions.Compiled));
	}

	internal static BNpcBase? GetObjectNPC(this IBattleChara battleChara)
	{
		return battleChara == null ? null : Service.GetSheet<Lumina.Excel.Sheets.BNpcBase>().GetRow(battleChara.BaseId);
	}

	/// <summary>
	/// Returns true if any current hostile target has the specified BNpc NameId.
	/// </summary>
	private static bool AnyHostileHasNameId(uint nameId)
	{
		var hostiles = DataCenter.AllHostileTargets;
		if (hostiles == null || hostiles.Count == 0)
		{
			return false;
		}

		for (int i = 0, n = hostiles.Count; i < n; i++)
		{
			var h = hostiles[i];
			if (h != null && h.NameId == nameId)
			{
				return true;
			}
		}

		return false;
	}

	internal static bool CanProvoke(this IBattleChara target)
	{
		if (target == null)
		{
			return false;
		}

		if (Service.Config.ProvokeAnything)
		{
			return true;
		}

		if (!target.IsAttackable())
		{
			return false;
		}

		if (DataCenter.PlayerFateId != 0 && target.FateId() == DataCenter.PlayerFateId)
		{
			return false;
		}

		// Removed the listed names.
		if (OtherConfiguration.NoProvokeNames.TryGetValue(Svc.ClientState.TerritoryType, out var ns1))
		{
			foreach (var n in ns1)
			{
				if (!string.IsNullOrEmpty(n) && GetCachedRegex(n).IsMatch(target.Name?.GetText() ?? string.Empty))
				{
					return false;
				}
			}
		}

		if (!Service.Config.ProvokeAnything)
		{
			// Target can move or too big and has a target
			if ((target.GetObjectNPC()?.Unknown0 == 0 || target.HitboxRadius >= 5) // Unknown12 used to be the flag checked for the mobs ability to move, honestly just guessing on this one
				&& (target.TargetObject?.IsValid() ?? false))
			{
				// The target is not a tank role
				if (Svc.Objects.SearchById(target.TargetObjectId) is IBattleChara targetObject && !targetObject.IsJobCategory(JobRole.Tank)
					&& (Vector3.Distance(target.Position, Player.Object?.Position ?? Vector3.Zero) > 5))
				{
					return true;
				}
			}
		}
		return false;
	}

	internal static bool HasPositional(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return false;
		}

		// Validate liveness before the native BaseId read below; see the note
		// in IsEnemy() for why the try/catch alone can't cover this.
		if (!battleChara.IsValid() || battleChara.Address == nint.Zero)
		{
			return false;
		}

		try
		{
			// battleChara can be a reference captured a frame or two earlier (e.g. the next GCD's
			// target). It can go stale between the validity check above and the StatusList read
			// inside HasStatus() below, most commonly right as the target dies and its object table
			// entry gets torn down - that's a hard crash, not a catchable exception, so re-resolve
			// from the live object table immediately before touching it instead of trusting the
			// reference we were handed. Same approach as StatusHelper.DoomNeedHealing.
			if (Svc.Objects.SearchById(battleChara.GameObjectId) is not IBattleChara fresh
				|| !fresh.IsValid() || fresh.Address == nint.Zero || fresh.IsDead)
			{
				return false;
			}

			if (!fresh.IsEnemy())
			{
				return false;
			}

			if (fresh.HasStatus(false, StatusID.DirectionalDisregard))
			{
				return false;
			}

			return Svc.Data.GetExcelSheet<BNpcBase>().TryGetRow(fresh.BaseId, out var dataRow) && !dataRow.IsOmnidirectional;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Checks if the battle character has the specified NPC name.
	/// </summary>
	/// <param name="battleChara">The battle character to check.</param>
	/// <param name="npcName">The NPC name to compare against.</param>
	/// <returns>True if the battle character's NameId matches the specified NPC name; otherwise, false.</returns>
	public static bool IsNamed(this IBattleChara battleChara, NPCName npcName)
	{
		return battleChara != null && battleChara.NameId == (uint)npcName;
	}

	internal static bool IsOthersPlayersMob(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return false;
		}

		// If the mob has a nameplate icon it is tagged as belonging to the current player's quest/leve/treasure.
		if (battleChara.GetNamePlateIcon() != 0)
		{
			return false;
		}

		// If the mob is actively targeting the player or the player's pet it cannot be treated as
		// another player's mob regardless of event type — it is a direct threat to us.
		if (battleChara.TargetObject == Player.Object)
		{
			return false;
		}

		if (Player.Object != null && battleChara.TargetObject?.OwnerId == Player.Object.GameObjectId)
		{
			return false;
		}

		// If the mob's owner is a party member it belongs to our party and can be attacked
		if (Player.Object != null && battleChara.OwnerId != 0 && PartyEntityIds.Contains(battleChara.OwnerId))
		{
			return false;
		}

		// SpecialType but no NamePlateIcon — check whether the mob's event type matches one of the
		// player-owned content directors that can produce mobs belonging to OTHER players.
		var ev = battleChara.GetEventType();
		for (var i = 0; i < _eventType.Length; i++)
		{
			if (_eventType[i] == ev)
			{
				return true;
			}
		}

		return false;
	}

	private static bool _sipInRangeCache;
	private static long _sipInRangeCacheTick = long.MinValue;
	private const long SipInRangeTtlMs = 15;

	// Whether any Forlorn enemy is within range of the player. Independent of the specific target
	// being checked, so it's computed once per frame instead of once per IsAttackable() call.
	private static bool IsForlornSipInRange()
	{
		var now = Environment.TickCount64;
		if (_sipInRangeCacheTick != long.MinValue && now - _sipInRangeCacheTick < SipInRangeTtlMs)
		{
			return _sipInRangeCache;
		}

		const float sipRange = 25f;
		var sipInRange = false;
		if (Player.Object != null)
		{
			foreach (var o in Svc.Objects)
			{
				if (o is IBattleChara c && c.IsEnemy() && c.IsTargetable && c.IsForlorn()
					&& Vector3.Distance(c.Position, Player.Object.Position) <= sipRange)
				{
					sipInRange = true;
					break;
				}
			}
		}

		_sipInRangeCache = sipInRange;
		_sipInRangeCacheTick = now;
		return sipInRange;
	}

	internal static bool IsAttackable(this IBattleChara battleChara)
	{
		if (Player.Object == null)
		{
			return false;
		}

		if (battleChara.IsAllianceMember())
		{
			return false;
		}

		if (!battleChara.IsEnemy())
		{
			return false;
		}

		if (battleChara.IsSpecialExceptionImmune())
		{
			return false; // For specific named mobs that are immune to everything.
		}

		if (battleChara.IsSpecialImmune())
		{
			return false; // For conditionally immune mobs
		}

		if (battleChara.GetEventType() == EventHandlerContent.DpsChallengeDirector && Player.Object.GetEventType() != EventHandlerContent.DpsChallengeDirector)
		{
			return false;
		}

		// Dead.
		if (Service.Config.FilterOneHpInvincible && battleChara.CurrentHp <= 1)
		{
			return false;
		}

		foreach (var status in battleChara.StatusList)
		{
			if (StatusHelper.IsInvincible(status) && ((DataCenter.IsPvP && !Service.Config.IgnorePvPInvincibility) || !DataCenter.IsPvP))
			{
				return false;
			}
		}

		// In No Hostiles Names
		if (OtherConfiguration.NoHostileNames != null &&
			OtherConfiguration.NoHostileNames.TryGetValue(Svc.ClientState.TerritoryType, out var ns1))
		{
			foreach (var n in ns1)
			{
				if (!string.IsNullOrEmpty(n) && GetCachedRegex(n).IsMatch(battleChara.Name.TextValue))
				{
					return false;
				}
			}
		}

		// Fate
		if (Service.Config.IgnoreNonFateInFate && DataCenter.Territory?.ContentType != TerritoryContentType.Eureka)
		{
			if (battleChara.FateId() != DataCenter.PlayerFateId)
			{
				return false;
			}
		}

		if (DataCenter.IsInBozjanFieldOp)
		{
			var isInCE = DataCenter.IsInBozjanFieldOpCE;

			if (isInCE)
			{
				if (!battleChara.IsBozjanCEMob() && battleChara.GetBattleNPCSubKind() != BattleNpcSubKind.BNpcPart)
				{
					return false;
				}
			}

			if (!isInCE)
			{
				if (battleChara.IsBozjanCEMob())
				{
					return false;
				}
			}
		}

		if (Service.Config.TargetQuestThings3 && battleChara.IsOthersPlayersMob())
		{
			return false;
		}

		if (Service.Config.ForlornPriority && DataCenter.IsInFate)
		{
			if (Player.Object == null)
			{
				return false;
			}

			if (IsForlornSipInRange() && !battleChara.IsForlorn())
			{
				return false;
			}
		}

		if (battleChara.IsTopPriorityNamedHostile())
		{
			return true;
		}

		if (battleChara.IsTopPriorityHostile())
		{
			return true;
		}

		if (Service.CountDownTime > 0 || DataCenter.IsPvP)
		{
			return true;
		}

		//Special cases for Black Star and Mythic Idol, which do not have valid target objects but are still attackable.
		if (battleChara.IsNamed(NPCName.BlackStar) || battleChara.IsNamed(NPCName.MythicIdol))
		{
			return true;
		}

		// Tar on me
		return (battleChara.TargetObject == Player.Object)
			|| (Player.Object != null && battleChara.TargetObject?.OwnerId == Player.Object.GameObjectId)
			|| DataCenter.IsHenched
			|| DataCenter.CurrentTargetToHostileType switch
			{
				TargetHostileType.AllTargetsCanAttack => true,
				TargetHostileType.TargetsHaveTarget => battleChara.TargetObject is not null,
				TargetHostileType.AllTargetsWhenSolo => DataCenter.PartyMembers.Count == 1 || battleChara.TargetObject is not null,
				TargetHostileType.AllTargetsWhenSoloInDuty => (DataCenter.PartyMembers.Count == 1 && (Svc.Condition[ConditionFlag.BoundByDuty] || Svc.Condition[ConditionFlag.BoundByDuty56]))
									|| battleChara.TargetObject is not null,
				TargetHostileType.SoloDeepDungeonSmart => IsSoloDeepDungeonSmartAttackable(battleChara),
				_ => true,
			};
	}

	internal static bool IsBozjanCEMob(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return false;
		}

		if (!battleChara.IsEnemy())
		{
			return false;
		}

		if (!DataCenter.IsInBozjanFieldOp)
		{
			return false;
		}

		// Get the EventId of the mob
		return battleChara.GetEventType() == EventHandlerContent.PublicContentDirector;
	}

	private static bool IsSoloDeepDungeonSmartAttackable(IBattleChara battleChara)
	{
		// In combat: only previously engaged targets.
		if (DataCenter.InCombat)
		{
			return battleChara.TargetObject is not null;
		}

		if (DataCenter.PartyMembers.Count > 1)
		{
			return battleChara.TargetObject is not null;
		}

		// Out of combat: if any previously engaged targets are nearby, only attack those; otherwise, only the nearest single enemy.
		var hasEngagedNearby = false;
		var hostiles = DataCenter.AllHostileTargets;
		for (int i = 0, n = hostiles.Count; i < n; i++)
		{
			var h = hostiles[i];
			if (h != null && h.TargetObject != null && h.DistanceToPlayer() < 25f)
			{
				hasEngagedNearby = true;
				break;
			}
		}

		if (hasEngagedNearby)
		{
			return battleChara.TargetObject is not null;
		}

		IBattleChara? nearest = null;
		var best = float.MaxValue;
		for (int i = 0, n = hostiles.Count; i < n; i++)
		{
			var h = hostiles[i];
			if (h == null)
			{
				continue;
			}

			var d = h.DistanceToPlayer();
			if (d < best)
			{
				best = d;
				nearest = h;
			}
		}
		return nearest != null && battleChara.GameObjectId == nearest.GameObjectId;
	}

	internal static bool IsOccultCEMob(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return false;
		}

		if (!battleChara.IsEnemy())
		{
			return false;
		}

		if (!DataCenter.IsInOccultCrescentOp)
		{
			return false;
		}

		// Get the EventId of the mob
		return battleChara.GetEventType() == EventHandlerContent.PublicContentDirector;
	}

	internal static bool IsOccultFateMob(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return false;
		}

		if (!battleChara.IsEnemy())
		{
			return false;
		}

		if (!DataCenter.IsInOccultCrescentOp)
		{
			return false;
		}

		// Get the EventId of the mob
		return battleChara.GetEventType() == EventHandlerContent.FateDirector;
	}

	internal static bool IsSpecialExceptionImmune(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return false;
		}

		if (battleChara.IsNamed(NPCName.CastrumGate))
		{
			return true;
		}

		return false;
	}

	private static string RemoveControlCharacters(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return input;
		}

		// Use a StringBuilder for efficient string manipulation
		StringBuilder output = new(input.Length);
		foreach (var c in input)
		{
			// Exclude control characters and private use area characters
			if (!char.IsControl(c) && (c < '\uE000' || c > '\uF8FF'))
			{
				_ = output.Append(c);
			}
		}
		return output.ToString();
	}

	internal static unsafe bool IsEnemy(this IGameObject obj)
	{
		// Validate liveness before any native read/call: a try/catch around
		// AccessViolationException does not protect us here,
		// so the object must be confirmed live beforehand.
		if (obj == null)
		{
			return false;
		}

		if (!obj.IsValid())
		{
			return false;
		}

		if (obj.Address == nint.Zero)
		{
			return false;
		}

		if (!obj.IsTargetable)
		{
			return false;
		}

		if (ActionManager.CanUseActionOnTarget((uint)ActionID.BlizzardPvE, obj.Struct()))
		{
			return true;
		}

		return false;
	}

	internal static uint TargetCharaCondition(this IBattleChara obj)
	{
		var statusId = obj.OnlineStatus.RowId;
		if (statusId != 0)
		{
			return statusId;
		}

		return 0;
	}

	internal static bool IsConditionCannotTarget(this IBattleChara obj)
	{
		var statusId = obj.OnlineStatus.RowId;
		if (statusId == 15 || statusId == 5)
		{
			return true;
		}

		return false;
	}

	internal static unsafe bool IsFriendly(this IGameObject obj)
	{
		if (obj == null)
		{
			return false;
		}

		if (!obj.IsTargetable)
		{
			return false;
		}

		if (ActionManager.CanUseActionOnTarget((uint)ActionID.CurePvE, obj.Struct()))
		{
			return true;
		}

		if (ActionManager.CanUseActionOnTarget((uint)ActionID.RaisePvE, obj.Struct()))
		{
			return true;
		}

		return false;
	}

	internal static unsafe bool IsAllianceMember(this ICharacter obj)
	{
		return obj.GameObjectId is not 0
			&& !DataCenter.IsPvP && (DataCenter.IsInAllianceRaid || DataCenter.IsInBozjanFieldOpCE || DataCenter.IsInOccultCrescentOp) && obj is IPlayerCharacter
			&& (ActionManager.CanUseActionOnTarget((uint)ActionID.RaisePvE, (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)obj.Struct())
			|| ActionManager.CanUseActionOnTarget((uint)ActionID.CurePvE, (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)obj.Struct()));
	}

	internal static unsafe bool IsOtherPlayerOutOfDuty(this ICharacter obj)
	{
		return obj.GameObjectId is not 0
			&& !DataCenter.IsPvP && obj is IPlayerCharacter
			&& (ActionManager.CanUseActionOnTarget((uint)ActionID.RaisePvE, (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)obj.Struct())
			|| ActionManager.CanUseActionOnTarget((uint)ActionID.CurePvE, (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)obj.Struct()));
	}

	internal static unsafe bool CanBeRaised(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return false;
		}

		if (!battleChara.IsTargetable)
		{
			return false;
		}

		return ActionManager.CanUseActionOnTarget((uint)ActionID.RaisePvE, (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)battleChara.Struct());
	}

	internal static unsafe bool IsPlayer(this IBattleChara battleChara)
	{
		return battleChara == Player.Object;
	}

	/// <summary>
	///
	/// </summary>
	public static bool IsPlayerInParty()
	{
		if (Player.Object == null)
		{
			return false;
		}

		return Player.Object.IsTargetable && PartyEntityIds.Contains(Player.Object.EntityId);
	}

	private static readonly HashSet<uint> _partyEntityIds = [];
	private static long _partyEntityIdsTick = long.MinValue;

	// Ids of the real party members, read once per frame: membership is checked for every candidate of every action.
	private static HashSet<uint> PartyEntityIds
	{
		get
		{
			var now = Environment.TickCount64;
			if (_partyEntityIdsTick != long.MinValue && now - _partyEntityIdsTick < 15)
			{
				return _partyEntityIds;
			}

			_partyEntityIds.Clear();
			foreach (var p in Svc.Party)
			{
				if (p.EntityId is not 0 and not 0xE0000000)
				{
					_ = _partyEntityIds.Add(p.EntityId);
				}
			}

			_partyEntityIdsTick = now;
			return _partyEntityIds;
		}
	}

	/// <summary>
	///
	/// </summary>
	public static bool IsParty(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return false;
		}

		if (Player.Object == null)
		{
			return false;
		}

		if (battleChara.GameObjectId == Player.Object.GameObjectId)
		{
			return true;
		}

		if (!battleChara.IsTargetable)
		{
			return false;
		}

		if (battleChara.IsPet())
		{
			return false;
		}

		if (PartyEntityIds.Contains(battleChara.EntityId))
		{
			return true;
		}

		if (DataCenter.FriendlyPartyNpcHealRaiseEnabled && battleChara.IsNpcPartyMember())
		{
			return true;
		}

		if (Service.Config.ChocoboPartyMember && battleChara.IsPlayerCharacterChocobo())
		{
			return true;
		}

		if (Service.Config.FriendlyBattleNpcHeal && battleChara.IsFriendlyBattleNPC())
		{
			return true;
		}

		if (Service.Config.FocusTargetIsParty && battleChara.IsFocusTarget() && battleChara.IsAllianceMember())
		{
			return true;
		}

		return false;
	}

	// Finds an object the player placed, e.g. the marker an object-based movement action teleports to.
	internal static bool TryGetOwnedObjectPosition(uint baseId, out Vector3 position)
	{
		position = default;
		var playerId = Player.Object?.GameObjectId ?? 0;
		if (playerId == 0)
		{
			return false;
		}

		foreach (var obj in Svc.Objects)
		{
			if (obj != null && obj.BaseId == baseId && obj.OwnerId == playerId)
			{
				position = obj.Position;
				return true;
			}
		}

		return false;
	}

	internal static bool IsPet(this IBattleChara battleChara)
	{
		if (battleChara == null || Svc.Buddies.PetBuddy == null)
		{
			return false;
		}

		return battleChara.GameObjectId == Svc.Buddies.PetBuddy.GameObject?.GameObjectId;
	}

	internal static bool IsNpcPartyMember(this IBattleChara battleChara)
	{
		if (battleChara.IsPet())
		{
			return false;
		}

		return battleChara.GetBattleNPCSubKind() == BattleNpcSubKind.NpcPartyMember;
	}

	internal static bool IsFriendlyBattleNPC(this IBattleChara battleChara)
	{
		if (DataCenter.TerritoryID == 952)
		{
			return false;
		}

		if (battleChara.IsPet())
		{
			return false;
		}

		return battleChara.GetNameplateKind() == NameplateKind.FriendlyBattleNPC;
	}

	internal static bool IsPlayerCharacterChocobo(this IBattleChara battleChara)
	{
		return battleChara.GetBattleNPCSubKind() == BattleNpcSubKind.Buddy;
	}

	internal static bool IsFocusTarget(this IBattleChara battleChara)
	{
		return Svc.Targets.FocusTarget != null && Svc.Targets.FocusTarget.GameObjectId == battleChara.GameObjectId;
	}

	internal static bool PlayerIsTargetOnSelf()
	{
		if (Player.Object == null)
		{
			return false;
		}

		return Player.Object.TargetObject?.TargetObject == Player.Object;
	}

	internal static bool IsTargetOnSelf(this IBattleChara battleChara)
	{
		return battleChara.TargetObject?.TargetObject == battleChara;
	}

	internal static bool PlayerIsAlive()
	{
		if (Player.Object == null)
		{
			return false;
		}

		if (Player.Object.IsDead)
		{
			return false;
		}

		if (!Player.Object.IsTargetable)
		{
			return false;
		}

		if (Player.Object.CurrentHp == 0)
		{
			return false;
		}

		return true;
	}

	internal static bool IsAlive(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return false;
		}

		if (battleChara.IsDead)
		{
			return false;
		}

		if (!battleChara.IsTargetable)
		{
			return false;
		}

		if (battleChara.CurrentHp == 0)
		{
			return false;
		}

		return true;
	}

	/// <summary>
	/// Get the object kind.
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	public static unsafe ObjectKind GetObjectKind(this IGameObject obj)
	{
		var s = obj.Struct();
		return s == null ? default : (ObjectKind)s->ObjectKind;
	}

	/// <summary>
	/// Gets the priority value for treasure hunt nameplate icons.
	/// Lower values indicate higher priority (1 is highest priority).
	/// </summary>
	/// <param name="icon">The nameplate icon ID.</param>
	/// <returns>
	/// Priority value (1-5) for treasure hunt icons, or int.MaxValue if not a treasure hunt priority icon.
	/// </returns>
	internal static int GetNamePlateIconPriority(uint icon)
	{
		return icon switch
		{
			60687 => 1, // Treasure hunt icon 1
			60688 => 2, // Treasure hunt icon 2
			60689 => 3, // Treasure hunt icon 3
			60690 => 4, // Treasure hunt icon 4
			60691 => 5, // Treasure hunt icon 5
			_ => int.MaxValue
		};
	}

	/// <summary>
	/// Determines whether the specified game object is a top priority hostile target based on its name being listed.
	/// </summary>
	/// <param name="battleChara">The battleChara to check.</param>
	/// <returns>
	/// <c>true</c> if the game object is a top priority named hostile, target; otherwise, <c>false</c>.
	/// </returns>
	internal static bool IsTopPriorityNamedHostile(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return false;
		}

		foreach (var id in DataCenter.PrioritizedNameIds)
		{
			if (battleChara.NameId == id)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Determines whether the specified game object is a top priority hostile target.
	/// </summary>
	/// <param name="battleChara">The game object to check.</param>
	/// <returns>
	/// <c>true</c> if the game object is a top priority hostile target; otherwise, <c>false</c>.
	/// </returns>
	internal static bool IsTopPriorityHostile(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return false;
		}

		var icon = battleChara.GetNamePlateIcon();

		if (battleChara.IsAllianceMember() || battleChara.IsParty())
		{
			return false;
		}

		if (Service.Config.Treasuredungeonnumbered && DataCenter.IsInTreasureHunt)
		{
			if (icon == 60687 || icon == 60688 || icon == 60689 || icon == 60690 || icon == 60691)
			{
				return true;
			}
		}

		//TODO: 14542 - Mage in board 1 of crucible, likely needs prio logic

		if (Service.Config.Treasuredungeontimed && battleChara.TreasureDungeonPrio())
		{
			return true;
		}

		if (DataCenter.IsInFate && battleChara.IsForlorn())
		{
			return true;
		}

		if (battleChara.IsM10SavagePriority())
		{
			return true;
		}

		if (battleChara.IsM9SavagePriority())
		{
			return true;
		}

		if (battleChara.IsCrucibleThirdBoardPriority())
		{
			return true;
		}

		// Check IBattleChara bespoke IsSpecialInclusionPriority method
		if (battleChara.IsSpecialInclusionPriority())
		{
			return true;
		}

		if (DataCenter.IsInOccultCrescentOp)
		{
			if (battleChara.IsOccultCEMob() && battleChara.NameId != 14717)
			{
				return true;
			}

			if (DataCenter.IsInNorthHorn)
			{
				if (battleChara.NameId == 14719)
				{
					return true;
				}
			}
		}

		// MCH prio targeting for Wildfire
		if (Player.Job == Job.MCH && (battleChara.HasStatus(true, StatusID.Wildfire) || battleChara.HasStatus(true, StatusID.Wildfire_1323)))
		{
			return true;
		}

		if (Service.Config.PrioAtomelith && DataCenter.IsPvP)
		{
			var IceBoundTomeLithA1 = battleChara.NameId == 4822;
			var IceBoundTomeLithA2 = battleChara.NameId == 4823;
			var IceBoundTomeLithA3 = battleChara.NameId == 4824;
			var IceBoundTomeLithA4 = battleChara.NameId == 4825;
			if (IceBoundTomeLithA1 || IceBoundTomeLithA2 || IceBoundTomeLithA3 || IceBoundTomeLithA4)
			{
				return true;
			}
		}

		if (Service.Config.PrioBtomelith && DataCenter.IsPvP)
		{
			var IceBoundTomeLithB1 = battleChara.NameId == 4826;
			var IceBoundTomeLithB2 = battleChara.NameId == 4827;
			var IceBoundTomeLithB3 = battleChara.NameId == 4828;
			var IceBoundTomeLithB4 = battleChara.NameId == 4829;
			var IceBoundTomeLithB5 = battleChara.NameId == 4830;
			var IceBoundTomeLithB6 = battleChara.NameId == 4831;
			var IceBoundTomeLithB7 = battleChara.NameId == 4832;
			var IceBoundTomeLithB8 = battleChara.NameId == 4833;
			var IceBoundTomeLithB9 = battleChara.NameId == 4834;
			var IceBoundTomeLithB10 = battleChara.NameId == 4835;
			var IceBoundTomeLithB11 = battleChara.NameId == 4836;
			var IceBoundTomeLithB12 = battleChara.NameId == 4837;
			var IceBoundTomeLithB13 = battleChara.NameId == 4840;
			var IceBoundTomeLithB14 = battleChara.NameId == 4841;
			var IceBoundTomeLithB15 = battleChara.NameId == 4842;
			var IceBoundTomeLithB16 = battleChara.NameId == 4843;
			var IceBoundTomeLithB17 = battleChara.NameId == 4844;
			var IceBoundTomeLithB18 = battleChara.NameId == 4845;

			if (IceBoundTomeLithB1 || IceBoundTomeLithB2 || IceBoundTomeLithB3 || IceBoundTomeLithB4 ||
				IceBoundTomeLithB5 || IceBoundTomeLithB6 || IceBoundTomeLithB7 || IceBoundTomeLithB8 ||
				IceBoundTomeLithB9 || IceBoundTomeLithB10 || IceBoundTomeLithB11 || IceBoundTomeLithB12 ||
				IceBoundTomeLithB13 || IceBoundTomeLithB14 || IceBoundTomeLithB15 || IceBoundTomeLithB16 ||
				IceBoundTomeLithB17 || IceBoundTomeLithB18)
			{
				return true;
			}
		}

		// Ensure StatusList is not null before iterating
		if (battleChara.StatusList != null)
		{
			foreach (var status in battleChara.StatusList)
			{
				if (StatusHelper.IsPriority(status))
				{
					return true;
				}
			}
		}

		if (Service.Config.ChooseAttackMark)
		{
			var targets = MarkingHelper.GetAttackSignTargets();
			if (targets != null)
			{
				foreach (var id in targets)
				{
					if (id != 0 && id == (long)battleChara.GameObjectId && battleChara.IsEnemy())
					{
						return true;
					}
				}
			}
		}

		if (Service.Config.TargetFatePriority && DataCenter.PlayerFateId != 0 && battleChara.FateId() == DataCenter.PlayerFateId)
		{
			return true;
		}

		if (Service.Config.TargetHuntingRelicLevePriority && (icon == 60092 || icon == 60094 || icon == 60096 || icon == 60097 || icon == 60098 || icon == 71244))
		{
			return true;
		}
		//60092 Hunt Log
		//60094 Treasure Mob
		//60096 Relic Weapon
		//60097 Hunt Bill
		//60098 Crescent
		//71244 Leve Target

		// Quest
		if (Service.Config.TargetQuestPriority && (icon == 71204 || icon == 71144 || icon == 71224 || icon == 71344 || (battleChara.GetEventType() == EventHandlerContent.Quest) && battleChara.GetNamePlateIcon() != 0))
		{
			return true;
		}
		//71204 Main Quest
		//71144 Major Quest
		//71224 Other Quest
		//71344 Major Quest

		// Check if the object is a BattleNpcPart
		if (Service.Config.PrioEnemyParts && battleChara.GetBattleNPCSubKind() == BattleNpcSubKind.BNpcPart)
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// True if a Deadly Doornail (NameId 14303) is currently in AllHostileTargets.
	/// </summary>
	public static bool HasDeadlyDoornail => AnyHostileHasNameId(14303);

	/// <summary>
	/// True if a Fatal Flail (NameId 14302) is currently in AllHostileTargets.
	/// </summary>
	public static bool HasFatalFlail => AnyHostileHasNameId(14302);

	/// <summary>
	/// M9S add priority: the Charnel Cell matching the player's Hell in a Cell, and Deadly Doornail or Fatal Flail depending on role.
	/// </summary>
	public static bool IsM9SavagePriority(this IBattleChara battleChara)
	{
		if (Player.Object == null || !Service.Config.M9SAdsTargeting || !DataCenter.IsInM9S)
		{
			return false;
		}

		var role = DataCenter.Role;
		var isMeleeOrTank = role is JobRole.Melee or JobRole.Tank;
		var priority = battleChara.NameId switch
		{
			14304 => IsPlayersCharnelCell(battleChara),
			// Deadly Doornail: ranged and healers always, melee and tanks only when it's close and no Fatal Flail is up.
			14303 => role is JobRole.RangedPhysical or JobRole.RangedMagical or JobRole.Healer
				|| (isMeleeOrTank && battleChara.DistanceToPlayer() < 5f && !HasFatalFlail),
			// Fatal Flail: melee and tanks always, everyone else only when no Deadly Doornail is up.
			14302 => isMeleeOrTank || !HasDeadlyDoornail,
			_ => false,
		};

		if (priority && Service.Config.InDebug)
		{
			PluginLog.Information($"IsM9SavagePriority: {battleChara.NameId} is a priority target for {role}");
		}

		return priority;
	}

	// Each Heel in a Cell on a Charnel Cell pairs with the Hell in a Cell at the same index on the player.
	private static bool IsPlayersCharnelCell(IBattleChara cell)
	{
		for (var i = 0; i < StatusHelper.HeelOfTheCellStatus.Length; i++)
		{
			if (cell.HasStatus(false, StatusHelper.HeelOfTheCellStatus[i]) && StatusHelper.PlayerHasStatus(false, StatusHelper.HellInACellStatus[i]))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// M10 (Normal and Savage) priority: Red Hot while the player has Firesnaking, Deep Blue while the player has Watersnaking.
	/// </summary>
	public static bool IsM10SavagePriority(this IBattleChara battleChara)
	{
		if (Player.Object == null || !Service.Config.M10SBroTargeting || !(DataCenter.IsInM10S || DataCenter.TerritoryID == 1322))
		{
			return false;
		}

		var priority = battleChara.NameId switch
		{
			14370 => StatusHelper.PlayerHasStatus(false, StatusID.Firesnaking),
			14369 => StatusHelper.PlayerHasStatus(false, StatusID.Watersnaking),
			_ => false,
		};

		if (priority && Service.Config.InDebug)
		{
			PluginLog.Information($"IsM10SavagePriority: {battleChara.NameId} is a priority target");
		}

		return priority;
	}

	// Crucible third board: Sahagin Piece until it has Paralyzing Spikes, then Ymir Shell, then Ymir Piece; Crawling Piece always.
	internal static bool IsCrucibleThirdBoardPriority(this IBattleChara battleChara)
	{
		if (!DataCenter.IsinThirdBoard)
		{
			return false;
		}

		return battleChara.NameId switch
		{
			14571 => Service.Config.CrucibleYmir && !battleChara.HasStatus(false, StatusID.ParalyzingSpikes), // Sahagin Piece
			14570 => Service.Config.CrucibleYmir && !HasUnspikedSahagin(), // Ymir Shell
			14569 => Service.Config.CrucibleYmir && !HasUnspikedSahagin() && !AnyLivingHostile(14570), // Ymir Piece
			14586 => Service.Config.CrucibleSiren, // Crawling Piece
			_ => false,
		};

		static bool HasUnspikedSahagin()
		{
			return AnyLivingHostile(14571, static s => !s.HasStatus(false, StatusID.ParalyzingSpikes));
		}
	}

	private static bool AnyLivingHostile(uint nameId, Func<IBattleChara, bool>? predicate = null)
	{
		foreach (var hostile in DataCenter.AllHostileTargets)
		{
			if (hostile != null && hostile.NameId == nameId && hostile.CurrentHp > 0 && (predicate == null || predicate(hostile)))
			{
				return true;
			}
		}

		return false;
	}

	internal static bool IsSpecialInclusionPriority(this IBattleChara battleChara)
	{
		if (battleChara.NameId == 8145
			|| battleChara.NameId == 10259
			|| battleChara.NameId == 12704)
		{
			return true;
		}
		//8145 Root in Dohn Meg boss 2
		//10259 Cinduruva in The Tower of Zot
		//12704 Crystalline Debris

		//14052 hellmaker, only for the Cell Block C and D prisoners
		if (DataCenter.TerritoryID == 1292 && battleChara.NameId == 14052
			&& StatusHelper.PlayerHasStatus(false, (StatusID)4544, (StatusID)4545))
		{
			return true;
		}

		// striking shrublet - Floor 10 boss ads
		if (DataCenter.TerritoryID == 1281)
		{
			if (battleChara.NameId == 13980)
			{
				return true;
			}
		}

		// forgiven adulation - Floor 30 boss ads
		if (DataCenter.TerritoryID == 1284)
		{
			if (battleChara.NameId == 13978)
			{
				return true;
			}
		}

		return false;
	}

	internal static bool TreasureDungeonPrio(this IBattleChara battleChara)
	{
		if (DataCenter.IsInTreasureHunt)
		{
			if (DataCenter.IsInTheLostCanalsofUznair)
			{
				if (battleChara.IsNamed(NPCName.NamazuStickywhisker))
				{
					return true;
				}
			}

			if (DataCenter.IsInTheShiftingAltarsofUznair)
			{
				if (battleChara.IsNamed(NPCName.GoldWhisker))
				{
					return true;
				}

				if (battleChara.IsNamed(NPCName.GoldWhisker_7625))
				{
					return true;
				}
			}

			if (DataCenter.IsInTheHiddenCanalsofUznair)
			{
				if (battleChara.IsNamed(NPCName.NamazuStickywhisker))
				{
					return true;
				}

				if (battleChara.IsNamed(NPCName.Abharamu))
				{
					return true;
				}
			}

			if (DataCenter.IsInTheDungeonsofLyheGhiah)
			{
				if (battleChara.IsNamed(NPCName.FuathTrickster))
				{
					return true;
				}

				if (battleChara.IsNamed(NPCName.TheKeeperOfTheKeys))
				{
					return true;
				}
			}

			if (DataCenter.IsInTheShiftingOubliettesofLyheGhiah)
			{
				if (battleChara.IsNamed(NPCName.FuathTrickster_9774))
				{
					return true;
				}

				if (battleChara.IsNamed(NPCName.TheKeeperOfTheKeys_9773))
				{
					return true;
				}
			}

			if (DataCenter.IsInTheExcitatron6000)
			{
				if (battleChara.IsNamed(NPCName.RainbowGolem))
				{
					return true;
				}

				if (battleChara.IsNamed(NPCName.GoldenSupporter))
				{
					return true;
				}
			}

			if (DataCenter.IsInTheShiftingGymnasionAgonon)
			{
				if (battleChara.IsNamed(NPCName.GymnasiouLampas))
				{
					return true;
				}

				if (battleChara.IsNamed(NPCName.GymnasiouLyssa))
				{
					return true;
				}
			}

			if (DataCenter.IsInCenoteJaJaGural)
			{
				if (battleChara.IsNamed(NPCName.AlpacaOfFortune))
				{
					return true;
				}

				if (battleChara.IsNamed(NPCName.UolonOfFortune))
				{
					return true;
				}
			}

			if (DataCenter.IsInVaultOneiron)
			{
				if (battleChara.IsNamed(NPCName.Vaultkeeper))
				{
					return true;
				}

				if (battleChara.IsNamed(NPCName.GoldyCat))
				{
					return true;
				}
			}
		}

		return false;
	}

	internal static bool IsForlorn(this IBattleChara battleChara)
	{
		if (battleChara.NameId == 6737
			|| battleChara.NameId == 6738)
		{
			return true;
		}
		//6737 forlorn maiden
		//6738 forlorn maiden

		return false;
	}

	/// <summary>
	/// List of NameIds that Undead enemies in Occult Crecent.
	/// </summary>
	private static readonly HashSet<uint> IsOCUndeadSet =
	[
		13741, //Lifereaper
        13924, //Armor
        13922, //Ghost
        13921, //Caoineag
        13926, //Gourmand
        13925, //Troubadour
        13923, //Geshunpest
        13927, //Dullahan
    ];

	/// <summary>
	/// Check to see if Occult Crecent target is Undead.
	/// </summary>
	public static bool IsOCUndeadTarget(this IBattleChara battleChara)
	{
		return IsOCUndeadSet.Contains(battleChara.NameId);
	}

	/// <summary>
	/// List of NameIds that are immune to OC Slowga.
	/// </summary>
	private static readonly HashSet<uint> IsOCSlowgaImmuneSet =
	[
		13933, //Marolith
        13893, //AnimatedDoll
        13894, //AnimatedDoll
        13905, //Meraevis
        13888, //Triceratops

        13918, //Diplocaulus

        13936, //Zaratan
        13902, //Cetus
        13879, //Monk
        13911, //Panther

        13666, //Cloister Demon
        13668, //Cloister Torch
        13729, //Megaloknight
    ];

	/// <summary>
	/// Check to see if target is immune to Slowga.
	/// </summary>
	public static bool IsOCSlowgaImmuneTarget(this IBattleChara battleChara)
	{
		return IsOCSlowgaImmuneSet.Contains(battleChara.NameId);
	}

	/// <summary>
	/// List of NameIds that are immune to OC Doom.
	/// </summary>
	private static readonly HashSet<uint> IsOCDoomImmuneSet =
	[
		13893, //AnimatedDoll
        13894, //AnimatedDoll
        13917, //Sculpture
        13888, //Triceratops
        13887, //Rosebear
        13935, //Harpuia
        13886, //Aetherscab
        13912, //Havoc

        13937, //Apa
        13743, //Goobbue
        13878, //Goobbue
        13892, //Dirty Eye
        13898, //Blackguard
        13901, //Demon Pawn
        13745, //Headstone
        13881, //Headstone
        13908, //Blood Demon

        13666, //Cloister Demon
        13668, //Cloister Torch
        13729, //Megaloknight
    ];

	/// <summary>
	/// Check to see if target is immune to Phantom Doom.
	/// </summary>
	public static bool IsOCDoomImmuneTarget(this IBattleChara battleChara)
	{
		return IsOCDoomImmuneSet.Contains(battleChara.NameId);
	}

	/// <summary>
	/// List of NameIds that are immune to OC Stun.
	/// </summary>
	private static readonly HashSet<uint> IsOCStunImmuneSet =
	[
		13873, //Tormentor
        13891, //LionStatant
        13916, //Brachiosaur
        13887, //Rosebear
        13895, //Byblos
        13912, //Havoc

        13937, //Apa
        13928, //Crecent Golem
        13745, //Headstone
        13881, //Headstone
        13879, //Monk

        13666, //Cloister Demon
        13668, //Cloister Torch
        13729, //Megaloknight
    ];

	/// <summary>
	/// Check to see if target is immune to Stun.
	/// </summary>
	public static bool IsOCStunImmuneTarget(this IBattleChara battleChara)
	{
		return IsOCStunImmuneSet.Contains(battleChara.NameId);
	}

	/// <summary>
	/// List of NameIds that are immune to OC Freeze.
	/// </summary>
	private static readonly HashSet<uint> IsOCFreezeImmuneSet =
	[
		13876, //Fan
        13917, //Sculpture
        13916, //Brachiosaur
        13887, //Rosebear
        13744, //Taurus
        13880, //Taurus
        13909, //Dahak

        13919, //Zaghnal
        13934, //Uragnite
        13898, //Blackguard
        13901, //Demon Pawn
        13745, //Headstone
        13881, //Headstone

        13666, //Cloister Demon
        13668, //Cloister Torch
        13729, //Megaloknight
    ];

	/// <summary>
	/// Check to see if target is immune to Freeze.
	/// </summary>
	public static bool IsOCFreezeImmuneTarget(this IBattleChara battleChara)
	{
		return IsOCFreezeImmuneSet.Contains(battleChara.NameId);
	}

	/// <summary>
	/// List of NameIds that are immune to OC Blind.
	/// </summary>
	private static readonly HashSet<uint> IsOCBlindImmuneSet =
	[
		13931, //Chaochu
        13874, //Snapweed
        13932, //Leshy
        13933, //Marolith
        13893, //AnimatedDoll
        13894, //AnimatedDoll
        13887, //Rosebear

        13930, //Flame
        13928, //Crecent Golem
        13745, //Headstone
        13881, //Headstone

        13666, //Cloister Demon
        13668, //Cloister Torch
        13729, //Megaloknight
    ];

	/// <summary>
	/// Check to see if target is immune to Blind.
	/// </summary>
	public static bool IsOCBlindImmuneTarget(this IBattleChara battleChara)
	{
		return IsOCBlindImmuneSet.Contains(battleChara.NameId);
	}

	/// <summary>
	/// List of NameIds that are immune to OC Paralysis.
	/// </summary>
	private static readonly HashSet<uint> IsOCParalysisImmuneSet =
	[
		13931, //Chaochu
        13874, //Snapweed
        13932, //Leshy
        13933, //Marolith
        13917, //Sculpture
        13916, //Brachiosaur
        13871, //Foper
        13909, //Dahak

        13930, //Flame
        13919, //Zaghnal
        13928, //Crecent Golem
        13904, //Bachelor
        13898, //Blackguard
        13745, //Headstone
        13881, //Headstone
        13879, //Monk
        13911, //Panther

        13666, //Cloister Demon
        13668, //Cloister Torch
        13729, //Megaloknight
    ];

	/// <summary>
	/// Check to see if target is immune to Paralysis.
	/// </summary>
	public static bool IsOCParalysisImmuneTarget(this IBattleChara battleChara)
	{
		return IsOCParalysisImmuneSet.Contains(battleChara.NameId);
	}

	internal static unsafe uint GetNamePlateIcon(this IBattleChara battleChara)
	{
		return battleChara.Struct()->NamePlateIconId;
	}

	internal static unsafe EventHandlerContent GetEventType(this IBattleChara battleChara)
	{
		return battleChara.Struct()->EventId.ContentId;
	}

	internal static unsafe BattleNpcSubKind GetBattleNPCSubKind(this IBattleChara battleChara)
	{
		return (BattleNpcSubKind)battleChara.Struct()->SubKind;
	}

	internal static unsafe uint FateId(this IBattleChara battleChara)
	{
		return battleChara.Struct()->FateId;
	}

	// The candidate methods are resolved once via reflection and reused, since the shape of the
	// external TetherInfo API can't change at runtime.
	private static MethodInfo[]? _tetherProviderCandidates;

	/// <summary>
	/// Attempts to retrieve all tethers currently present using the ECommons TetherInfo API via reflection.
	/// </summary>
	public static IReadOnlyList<TetherInfo> GetAllTethers()
	{
		try
		{
			if (_tetherProviderCandidates == null)
			{
				var methods = typeof(TetherInfo).GetMethods(BindingFlags.Public | BindingFlags.Static);
				List<MethodInfo> matching = [];
				foreach (var m in methods)
				{
					if (m.ReturnType == typeof(TetherInfo[]) || typeof(System.Collections.IEnumerable).IsAssignableFrom(m.ReturnType))
					{
						matching.Add(m);
					}
				}
				_tetherProviderCandidates = [.. matching];
			}
			var candidates = _tetherProviderCandidates;

			foreach (var m in candidates)
			{
				var res = m.Invoke(null, null);
				if (res == null)
				{
					continue;
				}

				if (res is TetherInfo[] arr)
				{
					return arr;
				}

				if (res is System.Collections.IEnumerable ie)
				{
					var list = new List<TetherInfo>();
					foreach (var o in ie)
					{
						if (o is TetherInfo ti)
						{
							list.Add(ti);
						}
					}
					return list;
				}
			}
		}
		catch
		{
			// ignore and fallthrough to empty
		}

		return [];
	}

	/// <summary>
	/// Returns tethers where the object is either source or target.
	/// </summary>
	public static IReadOnlyList<TetherInfo> GetTethersFor(this IGameObject obj)
	{
		if (obj == null)
		{
			return [];
		}

		var all = GetAllTethers();
		var id = obj.GameObjectId;
		var result = new List<TetherInfo>();
		foreach (var t in all)
		{
			if (t == null)
			{
				continue;
			}

			if (ExtractTetherId(t, out _, out var src, out var tgt) && (src == id || tgt == id))
			{
				result.Add(t);
			}
		}
		return result;
	}

	/// <summary>
	/// Returns outgoing tethers (where object is source).
	/// </summary>
	public static IReadOnlyList<TetherInfo> GetOutgoingTethers(this IGameObject obj)
	{
		if (obj == null)
		{
			return [];
		}

		var all = GetAllTethers();
		var id = obj.GameObjectId;
		var result = new List<TetherInfo>();
		foreach (var t in all)
		{
			if (t == null)
			{
				continue;
			}

			if (ExtractTetherId(t, out _, out var src, out _) && src == id)
			{
				result.Add(t);
			}
		}
		return result;
	}

	/// <summary>
	/// Returns incoming tethers (where object is target).
	/// </summary>
	public static IReadOnlyList<TetherInfo> GetIncomingTethers(this IGameObject obj)
	{
		if (obj == null)
		{
			return [];
		}

		var all = GetAllTethers();
		var id = obj.GameObjectId;
		var result = new List<TetherInfo>();
		foreach (var t in all)
		{
			if (t == null)
			{
				continue;
			}

			if (ExtractTetherId(t, out _, out _, out var tgt) && tgt == id)
			{
				result.Add(t);
			}
		}
		return result;
	}

	/// <summary>
	/// Try to extract common tether fields (tether id, source object id, target object id) using reflection.
	/// Returns true if at least source/target were obtained.
	/// </summary>
	// Possible names for fields/properties on the external TetherInfo type, and the members
	// resolved from them - resolved once via reflection and reused, rather than re-scanning
	// type metadata on every tether processed.
	private static readonly string[] TetherIdNames = ["TetherId", "Id", "Tether"];
	private static readonly string[] TetherSourceNames = ["SourceObjectId", "SourceId", "Source", "SourceActorId"];
	private static readonly string[] TetherTargetNames = ["TargetObjectId", "TargetId", "Target", "TargetActorId"];
	private static MemberInfo[]? _tetherIdMembers;
	private static MemberInfo[]? _tetherSourceMembers;
	private static MemberInfo[]? _tetherTargetMembers;

	private static bool ExtractTetherId(TetherInfo t, out uint tetherId, out ulong sourceObjectId, out ulong targetObjectId)
	{
		tetherId = 0;
		sourceObjectId = 0;
		targetObjectId = 0;
		try
		{
			var tType = typeof(TetherInfo);
			_tetherIdMembers ??= ResolveTetherMembers(tType, TetherIdNames);
			_tetherSourceMembers ??= ResolveTetherMembers(tType, TetherSourceNames);
			_tetherTargetMembers ??= ResolveTetherMembers(tType, TetherTargetNames);

			object? val;

			val = TryGetMemberValue(t, _tetherIdMembers);
			if (val != null)
			{
				if (val is uint ui)
				{
					tetherId = ui;
				}
				else if (val is int i)
				{
					tetherId = (uint)i;
				}
				else if (uint.TryParse(val.ToString(), out var parsed))
				{
					tetherId = parsed;
				}
			}

			val = TryGetMemberValue(t, _tetherSourceMembers);
			if (val != null)
			{
				if (val is ulong ul)
				{
					sourceObjectId = ul;
				}
				else if (val is uint u)
				{
					sourceObjectId = u;
				}
				else if (ulong.TryParse(val.ToString(), out var pul))
				{
					sourceObjectId = pul;
				}
			}

			val = TryGetMemberValue(t, _tetherTargetMembers);
			if (val != null)
			{
				if (val is ulong ul2)
				{
					targetObjectId = ul2;
				}
				else if (val is uint u2)
				{
					targetObjectId = u2;
				}
				else if (ulong.TryParse(val.ToString(), out var pul2))
				{
					targetObjectId = pul2;
				}
			}

			return sourceObjectId != 0 || targetObjectId != 0 || tetherId != 0;
		}
		catch
		{
			return false;
		}
	}

	private static MemberInfo[] ResolveTetherMembers(Type tType, string[] names)
	{
		List<MemberInfo> members = [];
		foreach (var n in names)
		{
			var f = tType.GetField(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			if (f != null)
			{
				members.Add(f);
				continue;
			}
			var p = tType.GetProperty(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			if (p != null)
			{
				members.Add(p);
			}
		}
		return [.. members];
	}

	private static object? TryGetMemberValue(object instance, MemberInfo[] members)
	{
		foreach (var m in members)
		{
			var v = m is FieldInfo f ? f.GetValue(instance) : ((PropertyInfo)m).GetValue(instance);
			if (v != null)
			{
				return v;
			}
		}
		return null;
	}

	/// <summary>
	/// Returns true if the specified battleChara is tethered to the player.
	/// </summary>
	public static bool IsTetheredToPlayer(this IBattleChara battleChara)
	{
		if (battleChara == null || Player.Object == null)
		{
			return false;
		}

		var playerId = Player.Object.GameObjectId;
		var tethers = GetTethersFor(battleChara);
		foreach (var t in tethers)
		{
			if (t == null)
			{
				continue;
			}

			if (ExtractTetherId(t, out _, out var src, out var tgt) && (src == playerId || tgt == playerId))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Returns true if the specified checkedtarget is tethered to checkedtarget.
	/// </summary>
	public static bool IsTetheredToSpecificTarget(this IBattleChara battleChara, IBattleChara checkedtarget)
	{
		if (battleChara == null || checkedtarget == null)
		{
			return false;
		}

		var memberId = checkedtarget.GameObjectId;
		var tethers = GetTethersFor(battleChara);
		foreach (var t in tethers)
		{
			if (t == null)
			{
				continue;
			}

			if (ExtractTetherId(t, out _, out var src, out var tgt) && (src == memberId || tgt == memberId))
			{
				return true;
			}
		}
		return false;
	}

	private static readonly ConcurrentDictionary<uint, bool> _effectRangeCheck = [];

	/// <summary>
	/// Determines whether the specified game object can be interrupted.
	/// </summary>
	/// <param name="battleChara">The game object to check.</param>
	/// <returns>
	/// <c>true</c> if the game object can be interrupted; otherwise, <c>false</c>.
	/// </returns>
	internal static bool CanInterrupt(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return false;
		}

		// Ensure the IBattleChara object is valid before accessing its properties
		unsafe
		{
			if (battleChara.Struct() == null)
			{
				return false;
			}
		}

		var baseCheck = battleChara.IsCasting && battleChara.IsCastInterruptible && battleChara.TotalCastTime >= 2;
		if (!baseCheck)
		{
			return false;
		}

		if (!Service.Config.InterruptibleMoreCheck)
		{
			return false;
		}

		if (_effectRangeCheck.TryGetValue(battleChara.CastActionId, out var check))
		{
			return check;
		}

		var act = Service.GetSheet<Lumina.Excel.Sheets.Action>().GetRow(battleChara.CastActionId);
		return act.RowId == 0
			? (_effectRangeCheck[battleChara.CastActionId] = false)
			: (CastType)act.CastType == CastType.Cone || (CastType)act.CastType == CastType.Donut || (act.EffectRange > 0 && act.EffectRange < 8)
			? (_effectRangeCheck[battleChara.CastActionId] = false)
			: (_effectRangeCheck[battleChara.CastActionId] = true);
	}

	internal static bool IsDummy(this IBattleChara battleChara)
	{
		return battleChara?.NameId == 541;
	}

	/// <summary>
	/// Checks if the target is immune due to any special mob/boss.
	/// </summary>
	/// <param name="battleChara">The object to check.</param>
	/// <returns>True if the target is immune due to any special mechanic; otherwise, false.</returns>
	public static bool IsSpecialImmune(this IBattleChara battleChara)
	{
		return EncounterImmunity.IsImmune(battleChara);
	}

	/// <summary>
	/// Is target a boss depends on the ttk.
	/// </summary>
	/// <param name="battleChara">the object.</param>
	/// <returns></returns>
	public static bool IsBossFromTTK(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return false;
		}

		if (Service.Config.DummyBoss && battleChara.IsDummy())
		{
			return true;
		}

		//Fate
		return battleChara.GetTTK(true) >= Service.Config.BossTimeToKill;
	}

	/// <summary>
	/// Is target a boss depends on the icon.
	/// </summary>
	/// <param name="battleChara">the object.</param>
	/// <returns></returns>
	public static bool IsBossFromIcon(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return false;
		}

		if (Service.Config.DummyBoss && battleChara.IsDummy())
		{
			return true;
		}

		return Svc.Data.GetExcelSheet<BNpcBase>().TryGetRow(battleChara.BaseId, out var dataRow) && dataRow.Rank is 2 or 6;
	}

	/// <summary>
	/// Returns object's calculated shield value.
	/// </summary>
	/// <param name="battleChara"></param>
	/// <returns></returns>
	public static uint GetObjectShield(this IBattleChara battleChara)
	{
		return battleChara is ICharacter character && character.MaxHp > 0 && character.ShieldPercentage > 0
			? character.MaxHp * character.ShieldPercentage / 100
			: 0;
	}

	/// <summary>
	/// Returns object's calculated effective HP.
	/// </summary>
	/// <param name="battleChara"></param>
	/// <returns></returns>
	public static uint GetEffectiveHp(this IBattleChara battleChara)
	{
		return battleChara is ICharacter
			? battleChara.CurrentHp + GetObjectShield(battleChara)
			: 0;
	}

	/// <summary>
	/// Returns object's calculated effective HP as a percentage of Max HP, rounded down to the nearest whole number.
	/// </summary>
	/// <param name="battleChara"></param>
	/// <returns>Effective HP percentage (0-100). Returns 0 if MaxHp is 0 or not an ICharacter.</returns>
	public static int GetEffectiveHpPercent(this IBattleChara battleChara)
	{
		if (battleChara is not ICharacter character || character.MaxHp == 0)
		{
			return 0;
		}

		var effectiveHp = character.CurrentHp + GetObjectShield(battleChara);
		return (int)Math.Floor((float)effectiveHp / character.MaxHp * 100f);
	}

	/// <summary>
	/// Is object dying.
	/// </summary>
	/// <param name="battleChara"></param>
	/// <returns></returns>
	public static bool IsDying(this IBattleChara battleChara)
	{
		return battleChara != null && !battleChara.IsDummy() && (battleChara.GetTTK() <= Service.Config.DyingTimeToKill || battleChara.GetHealthRatio() < Service.Config.IsDyingConfig);
	}

	/// <summary>
	/// Determines whether the specified battle character is currently in combat.
	/// </summary>
	/// <param name="battleChara">The battle character to check.</param>
	/// <returns>
	/// <c>true</c> if the battle character is in combat; otherwise, <c>false</c>.
	/// </returns>
	internal static unsafe bool InCombat(this IBattleChara battleChara)
	{
		return battleChara != null && battleChara.Struct() != null && battleChara.Struct()->Character.InCombat;
	}

	private static readonly Dictionary<ulong, Vector3> LastPositions = [];
	internal static bool IsTargetMoving(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return false;
		}

		if (Svc.Condition[ConditionFlag.BetweenAreas] || LastPositions.Count > 4096)
		{
			LastPositions.Clear();
		}

		var id = battleChara.GameObjectId;
		var currentPos = battleChara.Position;
		if (LastPositions.TryGetValue(id, out var lastPos))
		{
			// You can adjust the threshold as needed
			var isMoving = Vector3.Distance(currentPos, lastPos) > 0.01f;
			LastPositions[id] = currentPos;
			return isMoving;
		}
		else
		{
			LastPositions[id] = currentPos;
			return false; // First check, assume not moving
		}
	}

	/// <summary>
	/// Calculates the estimated time to kill the specified battle character from its damage rate over the last 30 seconds. Known once it has taken damage for about 2 seconds.
	/// </summary>
	/// <param name="battleChara">The battle character to calculate the time to kill for.</param>
	/// <param name="wholeTime">If set to <c>true</c>, calculates the total time to kill; otherwise, calculates the remaining time to kill.</param>
	/// <returns>
	/// The estimated time to kill the battle character in seconds, or <see cref="float.NaN"/> if the calculation cannot be performed.
	/// </returns>
	internal static float GetTTK(this IBattleChara battleChara, bool wholeTime = false)
	{
		if (battleChara == null)
		{
			return float.NaN;
		}

		if (battleChara.IsDummy())
		{
			return 999.99f;
		}

		var rate = TimeToKillTracker.GetRate(battleChara.GameObjectId);
		if (float.IsNaN(rate))
		{
			return float.NaN;
		}

		return wholeTime ? 1f / rate : battleChara.GetHealthRatio() / rate;
	}

	/// <summary>
	/// Seconds until the next DoT tick on the target, or <see cref="float.NaN"/> until one of its ticks has been seen.
	/// </summary>
	/// <param name="battleChara">The target.</param>
	public static float TimeToNextDotTick(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return float.NaN;
		}

		var now = Environment.TickCount64;
		return DotTickTracker.TryGetNextTick(battleChara.GameObjectId, now, out var next) ? (next - now) / 1000f : float.NaN;
	}

	/// <summary>
	/// DoT ticks on the target between two times in seconds from now. Exact once one of its ticks has been seen, otherwise one per 3 seconds on average.
	/// </summary>
	/// <param name="battleChara">The target.</param>
	/// <param name="fromSeconds">Start of the window; a tick exactly at this time is not counted.</param>
	/// <param name="toSeconds">End of the window; a tick exactly at this time is counted.</param>
	public static float DotTicksBetween(this IBattleChara battleChara, float fromSeconds, float toSeconds)
	{
		if (battleChara == null || !float.IsFinite(fromSeconds) || !float.IsFinite(toSeconds))
		{
			return float.NaN;
		}

		var now = Environment.TickCount64;
		return DotTickTracker.CountTicks(battleChara.GameObjectId, now + (long)(fromSeconds * 1000), now + (long)(toSeconds * 1000));
	}

	private static readonly ConcurrentDictionary<ulong, DateTime> _aliveStartTimes = [];
	private static readonly ConcurrentDictionary<ulong, DateTime> _deadStartTimes = [];

	/// <summary>
	/// Gets how long the battleChara has been alive in seconds since their last death, capped at 30.
	/// </summary>
	/// <param name="battleChara">The battle character to check.</param>
	/// <returns>
	/// The time in seconds since the character's last death or first appearance, or float.NaN if unable to determine.
	/// </returns>
	internal static float TimeAlive(this IBattleChara? battleChara)
	{
		if (battleChara == null)
		{
			return float.NaN;
		}

		// If the character is dead, reset their alive time
		if (battleChara.IsDead || Svc.Condition[ConditionFlag.BetweenAreas])
		{
			_ = _aliveStartTimes.TryRemove(battleChara.GameObjectId, out _);
			return 0;
		}

		return SecondsSinceFirstSeen(_aliveStartTimes, battleChara.GameObjectId);
	}

	/// <summary>
	/// Gets how long the character has been dead in seconds, capped at 30.
	/// </summary>
	/// <param name="battleChara">The battle character to check.</param>
	/// <returns>
	/// The time in seconds since the character's death, 0 while alive, or float.NaN if unable to determine.
	/// </returns>
	internal static float TimeDead(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return float.NaN;
		}

		// If the character is alive, reset their dead time
		if (!battleChara.IsDead)
		{
			_ = _deadStartTimes.TryRemove(battleChara.GameObjectId, out _);
			return 0;
		}

		return SecondsSinceFirstSeen(_deadStartTimes, battleChara.GameObjectId);
	}

	private static float SecondsSinceFirstSeen(ConcurrentDictionary<ulong, DateTime> startTimes, ulong id)
	{
		var start = startTimes.GetOrAdd(id, static _ => DateTime.Now);
		return MathF.Min(30, (float)(DateTime.Now - start).TotalSeconds);
	}

	/// <summary>
	/// Determines whether the player has been hitting the specified battle character for at least a second,
	/// counted from the player's first damaging hit on it.
	/// </summary>
	/// <param name="battleChara">The battle character to check.</param>
	/// <returns>
	/// <c>true</c> if the player's first hit on the battle character is at least one second old; otherwise, <c>false</c>.
	/// </returns>
	internal static bool IsEngagedByPlayer(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return false;
		}

		var now = DateTime.Now;
		foreach ((var id, var time) in DataCenter.AttackedTargets)
		{
			if (id == battleChara.GameObjectId)
			{
				return now - time >= TimeSpan.FromSeconds(1);
			}
		}
		return false;
	}

	private struct LosCacheEntry
	{
		public Vector3 PlayerPos;
		public Vector3 TargetPos;
		public long ExpiresAt;
		public bool Visible;
	}
	private static readonly ConcurrentDictionary<ulong, LosCacheEntry> _losCache = [];
	private const float LosPosEpsilonSq = 0.04f;   // ~20 cm tolerance
	private const long LosTtlMs = 33;      // one 30–60 FPS frame

	// Optional: clear cache when swapping areas or if it grows too large
	private static void MaybeResetLosCache()
	{
		if (Svc.Condition[ConditionFlag.BetweenAreas] || _losCache.Count > 4096)
		{
			_losCache.Clear();
		}
	}

	// New overload to allow caller to supply eye position once per loop
	internal static unsafe bool CanSeeFrom(this IBattleChara battleChara, Vector3 playerEyePos, float targetYOffset = 2.0f)
	{
		if (battleChara == null || Player.Object == null)
		{
			return false;
		}

		var targetStruct = battleChara.Struct();
		if (targetStruct == null)
		{
			return false;
		}

		MaybeResetLosCache();

		var targetPos = battleChara.Position;
		targetPos.Y += targetYOffset;
		var id = battleChara.GameObjectId;
		var now = Environment.TickCount64;

		if (_losCache.TryGetValue(id, out var entry))
		{
			if (now <= entry.ExpiresAt &&
				Vector3.DistanceSquared(playerEyePos, entry.PlayerPos) <= LosPosEpsilonSq &&
				Vector3.DistanceSquared(targetPos, entry.TargetPos) <= LosPosEpsilonSq)
			{
				return entry.Visible;
			}
		}

		var offset = targetPos - playerEyePos;
		var maxDist = offset.Length();
		if (maxDist < 0.01f)
		{
			return true;
		}

		var direction = offset / maxDist;

		RaycastHit hit;
		var materialFilter = stackalloc int[] { 0x4000, 0, 0x4000, 0 };
		var module = Framework.Instance()->BGCollisionModule;
		var blocked = module->RaycastMaterialFilter(&hit, &playerEyePos, &direction, maxDist, 1, materialFilter);

		var visible = !blocked;
		_losCache[id] = new LosCacheEntry
		{
			PlayerPos = playerEyePos,
			TargetPos = targetPos,
			ExpiresAt = now + LosTtlMs,
			Visible = visible
		};
		return visible;
	}

	// Backward-compatible existing API calls the overload
	internal static unsafe bool CanSee(this IBattleChara battleChara, float playerYOffset = 2.0f, float targetYOffset = 2.0f)
	{
		if (battleChara == null || Player.Object == null)
		{
			return false;
		}

		var playerPos = Player.Object.Position;
		playerPos.Y += playerYOffset;
		return CanSeeFrom(battleChara, playerPos, targetYOffset);
	}

	/// <summary>
	/// Get the Player's current MP percentage.
	/// </summary>
	/// <returns></returns>
	public static float GetPlayerMPRatio()
	{
		if (Player.Object == null)
		{
			return 0;
		}

		if (Player.Object.MaxMp == 0)
		{
			return 0; // Avoid division by zero
		}

		return (float)Player.Object.CurrentMp / Player.Object.MaxMp;
	}

	/// <summary>
	/// Get the Player's current HP percentage.
	/// </summary>
	/// <returns></returns>
	public static float GetPlayerHealthRatio()
	{
		if (Player.Object == null)
		{
			return 0; // This may need to be changed to 100
		}

		if (DataCenter.RefinedHP.TryGetValue(Player.Object.GameObjectId, out var hp))
		{
			return hp;
		}

		if (Player.Object.MaxHp == 0)
		{
			return 0; // Avoid division by zero
		}

		return (float)Player.Object.CurrentHp / Player.Object.MaxHp;
	}

	/// <summary>
	/// Get the <paramref name="battleChara"/>'s current HP percentage.
	/// </summary>
	/// <param name="battleChara"></param>
	/// <returns></returns>
	public static float GetHealthRatio(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return 0; // This may need to be changed to 100
		}

		if (!battleChara.IsValid())
		{
			return 0; // This may need to be changed to 100
		}

		// Called for every candidate of every action, sorts included, so each target is worked out once per frame.
		var id = battleChara.GameObjectId;
		var now = Environment.TickCount64;
		if (_healthRatioCache.TryGetValue(id, out var cached) && now - cached.Tick < HealthRatioTtlMs)
		{
			return cached.Ratio;
		}

		if (_healthRatioCache.Count > 512)
		{
			_healthRatioCache.Clear();
		}

		var ratio = ComputeHealthRatio(battleChara);
		_healthRatioCache[id] = (now, ratio);
		return ratio;
	}

	private const long HealthRatioTtlMs = 15;
	private static readonly Dictionary<ulong, (long Tick, float Ratio)> _healthRatioCache = [];

	private static float ComputeHealthRatio(IBattleChara battleChara)
	{
		if (battleChara.IsParty() && battleChara.DoomNeedHealing())
		{
			return 0.01f;
		}

		if (DataCenter.RefinedHP.TryGetValue(battleChara.GameObjectId, out var hp))
		{
			return hp;
		}

		try
		{
			return battleChara.MaxHp == 0 ? 0 : (float)battleChara.CurrentHp / battleChara.MaxHp;
		}
		catch (Exception)
		{
			return 0;
		}
	}

	/// <summary>
	/// Determines the positional relationship of the player relative to the enemy.
	/// </summary>
	/// <param name="enemy">The enemy game object.</param>
	/// <returns>
	/// An <see cref="EnemyPositional"/> value indicating whether the player is in front, at the rear, or on the flank of the enemy.
	/// </returns>
	public static EnemyPositional FindEnemyPositional(this IBattleChara enemy)
	{
		// Validate liveness before the native Position/Rotation reads below;
		// see the note in IsEnemy() for why the try/catch alone can't cover this.
		if (enemy == null || !enemy.IsValid() || enemy.Address == nint.Zero || Player.Object == null)
		{
			return EnemyPositional.None;
		}

		var faceVec = enemy.GetFaceVector();

		// Positionals are judged on the ground plane, so height differences must not skew the angle.
		var offset = Player.Object.Position - enemy.Position;
		var dir = new Vector2(offset.X, offset.Z);
		if (dir.LengthSquared() < 0.0001f)
		{
			return EnemyPositional.None;
		}

		var dotProduct = Vector2.Dot(Vector2.Normalize(dir), new Vector2(faceVec.X, faceVec.Z));

		// 90° sectors: front and rear are within 45° of the facing axis.
		const float sectorCos = 0.70710677f;

		if (dotProduct > sectorCos)
		{
			return EnemyPositional.Front;
		}
		else if (dotProduct < -sectorCos)
		{
			return EnemyPositional.Rear;
		}

		return EnemyPositional.Flank;
	}

	/// <summary>
	/// Gets the facing direction vector of the game object.
	/// </summary>
	/// <param name="battleChara">The game object.</param>
	/// <returns>
	/// A <see cref="Vector3"/> representing the facing direction of the game object.
	/// </returns>
	internal static Vector3 GetFaceVector(this IBattleChara battleChara)
	{
		if (battleChara == null || !battleChara.IsValid() || battleChara.Address == nint.Zero)
		{
			return Vector3.Zero;
		}

		var rotation = battleChara.Rotation;
		return new Vector3((float)Math.Sin(rotation), 0, (float)Math.Cos(rotation));
	}

	/// <summary>
	/// Calculates the angle between two vectors.
	/// </summary>
	/// <param name="vec1">The first vector.</param>
	/// <param name="vec2">The second vector.</param>
	/// <returns>
	/// The angle in radians between the two vectors.
	/// </returns>
	internal static double AngleTo(this Vector3 vec1, Vector3 vec2)
	{
		double lengthProduct = vec1.Length() * vec2.Length();
		if (lengthProduct == 0)
		{
			return 0;
		}

		double dotProduct = Vector3.Dot(vec1, vec2);
		return Math.Acos(dotProduct / lengthProduct);
	}

	/// <summary>
	/// The distance from <paramref name="battleChara"/> to the player
	/// </summary>
	/// <param name="battleChara"></param>
	/// <returns></returns>
	public static float DistanceToPlayer(this IBattleChara battleChara)
	{
		if (battleChara == null || Player.Object == null)
		{
			return float.MaxValue;
		}

		var playerPos = Player.Object.Position;
		var targetPos = battleChara.Position;

		// Check vertical distance first - if too far vertically, the target is unreachable
		var dy = MathF.Abs(targetPos.Y - playerPos.Y);
		if (dy > 30f)
		{
			return dy;
		}

		// Use XZ-plane (horizontal) distance only — the game engine measures action range
		// purely on the horizontal plane, ignoring Y-axis differences (when within vertical threshold).
		var dx = targetPos.X - playerPos.X;
		var dz = targetPos.Z - playerPos.Z;
		var distance = MathF.Sqrt(dx * dx + dz * dz) - (Player.Object.HitboxRadius + battleChara.HitboxRadius);
		return MathF.Max(0f, distance);
	}

	/// <summary>
	/// The distance from <paramref name="battleChara"/> to the player's pet
	/// </summary>
	/// <param name="battleChara"></param>
	/// <returns></returns>
	public static float DistanceToPet(IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return float.MaxValue;
		}
		var pet = DataCenter.GetPet();
		if (pet == null)
		{
			return float.MaxValue;
		}

		var pdx = battleChara.Position.X - pet.Position.X;
		var pdz = battleChara.Position.Z - pet.Position.Z;
		return MathF.Max(0f, MathF.Sqrt(pdx * pdx + pdz * pdz) - battleChara.HitboxRadius);
	}

}