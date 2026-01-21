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
        if (OtherConfiguration.NoProvokeNames.TryGetValue(Svc.ClientState.TerritoryType, out string[]? ns1))
        {
            foreach (string n in ns1)
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

        try
        {
            if (battleChara.StatusList == null)
            {
                return false;
            }
        }
        catch
        {
            // StatusList threw, treat as unavailable
            return false;
        }

        if (battleChara.HasStatus(false, StatusID.DirectionalDisregard))
        {
            return false;
        }

        return Svc.Data.GetExcelSheet<BNpcBase>().TryGetRow(battleChara.BaseId, out var dataRow) && !dataRow.IsOmnidirectional;
    }

    internal static unsafe bool IsOthersPlayersMob(this IBattleChara battleChara)
    {
        //SpecialType but no NamePlateIcon
        bool isEventType = false;
        var ev = battleChara.GetEventType();
        for (int i = 0; i < _eventType.Length; i++)
        {
            if (_eventType[i] == ev)
            {
                isEventType = true;
                break;
            }
        }
        return isEventType && battleChara.GetNamePlateIcon() == 0;
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

        foreach (Dalamud.Game.ClientState.Statuses.IStatus status in battleChara.StatusList)
        {
            if (StatusHelper.IsInvincible(status) && ((DataCenter.IsPvP && !Service.Config.IgnorePvPInvincibility) || !DataCenter.IsPvP))
            {
                return false;
            }
        }

        // In No Hostiles Names
        if (OtherConfiguration.NoHostileNames != null &&
            OtherConfiguration.NoHostileNames.TryGetValue(Svc.ClientState.TerritoryType, out string[]? ns1))
        {
            foreach (string n in ns1)
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
            if (battleChara.FateId() != 0 && battleChara.FateId() != DataCenter.PlayerFateId)
            {
                return false;
            }
        }

        if (DataCenter.IsInBozjanFieldOp)
        {
            bool isInCE = DataCenter.IsInBozjanFieldOpCE;

            if (isInCE)
            {
                if (!battleChara.IsBozjanCEMob() && battleChara.GetBattleNPCSubKind() != BattleNpcSubKind.BattleNpcPart)
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

        /*if (DataCenter.IsInOccultCrescentOp)
        {
            bool isInCE = DataCenter.IsInOccultCrescentOpCE;

            if (isInCE)
            {
                if (!battleChara.IsOccultCEMob())
                {
                    return false;
                }
            }

            if (!isInCE)
            {
                if (battleChara.IsOccultCEMob())
                {
                    return false;
                }
            }
        }*/

        if (Service.Config.TargetQuestThings2 && battleChara.IsOthersPlayersMob())
        {
            return false;
        }

        if (Service.Config.ForlornPriority && DataCenter.IsInFate)
        {
			if (Player.Object == null)
			{
				return false;
			}

			const float sipRange = 25f;

            bool sipInRange = false;
            foreach (var o in Svc.Objects)
            {
                if (o is IBattleChara c && c.IsEnemy() && c.IsTargetable)
                {
                    if (c.IsForlorn() && Vector3.Distance(c.Position, Player.Object.Position) <= sipRange)
                    {
                        sipInRange = true;
                        break;
                    }
                }
            }

            if (sipInRange && !battleChara.IsForlorn())
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
		if (battleChara.NameId == 13726 || battleChara.NameId == 13636)
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
        bool hasEngagedNearby = false;
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
        float best = float.MaxValue;
        for (int i = 0, n = hostiles.Count; i < n; i++)
        {
            var h = hostiles[i];
            if (h == null) continue;
            float d = h.DistanceToPlayer();
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

        if (battleChara.NameId == 9441)
        {
            return true; // Special case for Bottom gate in CLL
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
        foreach (char c in input)
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
        if (obj == null)
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
        uint statusId = obj.OnlineStatus.RowId;
        if (statusId != 0)
        {
            return statusId;
        }

        return 0;
    }

    internal static bool IsConditionCannotTarget(this IBattleChara obj)
    {
        uint statusId = obj.OnlineStatus.RowId;
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
            return false;
        if (!battleChara.IsTargetable)
            return false;

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

		if (Player.Object.GameObjectId == Player.Object.GameObjectId)
		{
			return true;
		}

		if (!Player.Object.IsTargetable)
		{
			return false;
		}

		foreach (Dalamud.Game.ClientState.Party.IPartyMember p in Svc.Party)
		{
			if (p.GameObject?.GameObjectId == Player.Object.GameObjectId)
			{
				return true;
			}
		}

		return false;
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

        foreach (Dalamud.Game.ClientState.Party.IPartyMember p in Svc.Party)
        {
            if (p.GameObject?.GameObjectId == battleChara.GameObjectId)
            {
                return true;
            }
        }

        if (Service.Config.FriendlyPartyNpcHealRaise3 && battleChara.IsNpcPartyMember())
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
        return battleChara.GetBattleNPCSubKind() == BattleNpcSubKind.Chocobo;
    }

    internal static bool IsFocusTarget(this IBattleChara battleChara)
    {
        return Svc.Targets.FocusTarget != null && Svc.Targets.FocusTarget.GameObjectId == battleChara.GameObjectId;
    }

	internal static bool PlayerIsTargetOnSelf()
	{
		if (Player.Object == null)
			return false;
		return Player.Object.TargetObject?.TargetObject == Player.Object;
	}

	internal static bool IsTargetOnSelf(this IBattleChara battleChara)
    {
        return battleChara.TargetObject?.TargetObject == battleChara;
    }

	internal static bool PlayerIsAlive()
	{
		if (Player.Object == null)
			return false;
		if (Player.Object.IsDead)
			return false;
		if (!Player.Object.IsTargetable)
			return false;
		if (Player.Object.CurrentHp == 0)
			return false;

		return true;
	}

	internal static bool IsAlive(this IBattleChara battleChara)
    {
        if (battleChara == null)
            return false;
        if (battleChara.IsDead)
            return false;
        if (!battleChara.IsTargetable)
            return false;
        if (battleChara.CurrentHp == 0)
            return false;

        return true;
    }

    /// <summary>
    /// Get the object kind.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static unsafe ObjectKind GetObjectKind(this IGameObject obj)
    {
        FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject* s = obj.Struct();
        return s == null ? default : (ObjectKind)s->ObjectKind;
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

        foreach (uint id in DataCenter.PrioritizedNameIds)
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

        if (battleChara.IsAllianceMember() || battleChara.IsParty())
        {
            return false;
        }

        if (DataCenter.IsInFate && battleChara.IsForlorn())
        {
            return true;
        }

		if (battleChara.IsBroPriority())
		{
			return true;
		}

		if (battleChara.IsM9SavagePriority())
		{
			return true;
		}

		// Check IBattleChara bespoke IsSpecialInclusionPriority method
		if (battleChara.IsSpecialInclusionPriority())
        {
            return true;
        }

        if (battleChara.IsOccultCEMob())
        {
            return true;
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
            foreach (Dalamud.Game.ClientState.Statuses.IStatus status in battleChara.StatusList)
            {
                if (StatusHelper.IsPriority(status))
                {
                    return true;
                }
            }
        }

        if (Service.Config.ChooseAttackMark)
        {
            long[] targets = MarkingHelper.GetAttackSignTargets();
            if (targets != null)
            {
                foreach (long id in targets)
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

        uint icon = battleChara.GetNamePlateIcon();

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
        if (Service.Config.TargetQuestPriority && (icon == 71204 || icon == 71144 || icon == 71224 || icon == 71344 || battleChara.GetEventType() == EventHandlerContent.Quest))
        {
            return true;
        }
        //71204 Main Quest
        //71144 Major Quest
        //71224 Other Quest
        //71344 Major Quest

        // Check if the object is a BattleNpcPart
        if (Service.Config.PrioEnemyParts && battleChara.GetBattleNPCSubKind() == BattleNpcSubKind.BattleNpcPart)
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
	/// 
	/// </summary>
	public static bool IsM9SavagePriority(this IBattleChara battleChara)
	{
		if (Player.Object == null)
		{
			return false;
		}

		if (Service.Config.M9SAdsTargeting && DataCenter.IsInM9S)
		{
			var DeadlyDoornail = battleChara.NameId == 14303;
			var FatalFlail = battleChara.NameId == 14302;
			var CharnelCell = battleChara.NameId == 14304;

			if (CharnelCell)
			{
				// Heel (on target) vs Hell (on player) pairs
				StatusID HeelInACell1 = (StatusID)4739;
				StatusID HellInACell1 = (StatusID)4731;

				StatusID HeelInACell2 = (StatusID)4740;
				StatusID HellInACell2 = (StatusID)4732;

				StatusID HeelInACell3 = (StatusID)4741;
				StatusID HellInACell3 = (StatusID)4733;

				StatusID HeelInACell4 = (StatusID)4742;
				StatusID HellInACell4 = (StatusID)4734;

				StatusID HeelInACell5 = (StatusID)4743;
				StatusID HellInACell5 = (StatusID)4735;

				StatusID HeelInACell6 = (StatusID)4744;
				StatusID HellInACell6 = (StatusID)4736;

				StatusID HeelInACell7 = (StatusID)4745;
				StatusID HellInACell7 = (StatusID)4737;

				StatusID HeelInACell8 = (StatusID)4746;
				StatusID HellInACell8 = (StatusID)4738;

				// Iterate all Heel/Hell pairs; priority if target has Heel and player does have corresponding Hell
				foreach (var (heel, hell) in new (StatusID heel, StatusID hell)[]
				{
					(HeelInACell1, HellInACell1),
					(HeelInACell2, HellInACell2),
					(HeelInACell3, HellInACell3),
					(HeelInACell4, HellInACell4),
					(HeelInACell5, HellInACell5),
					(HeelInACell6, HellInACell6),
					(HeelInACell7, HellInACell7),
					(HeelInACell8, HellInACell8),
				})
				{
					if (battleChara.HasStatus(false, heel) && StatusHelper.PlayerHasStatus(false, hell))
					{
						if (Service.Config.InDebug)
						{
							PluginLog.Information("IsM9SavagePriority: CharnelCell priority due to Heel/Hell match");
						}
						return true;
					}
				}
			}

			if (DeadlyDoornail)
			{
				JobRole role = Player.Object?.ClassJob.Value.GetJobRole() ?? JobRole.None;

				if (role == JobRole.RangedPhysical)
				{
					if (Service.Config.InDebug)
					{
						PluginLog.Information("IsM9SavagePriority DeadlyDoornail mob found on RangedPhysical");
					}
					return true;
				}
				if (role == JobRole.RangedMagical)
				{
					if (Service.Config.InDebug)
					{
						PluginLog.Information("IsM9SavagePriority DeadlyDoornail mob found on RangedMagical");
					}
					return true;
				}
				if (role == JobRole.Healer)
				{
					if (Service.Config.InDebug)
					{
						PluginLog.Information("IsM9SavagePriority DeadlyDoornail mob found on Healer");
					}
					return true;
				}

				if (role == JobRole.Melee && battleChara.DistanceToPlayer() < 5f && !HasFatalFlail)
				{
					if (Service.Config.InDebug)
					{
						PluginLog.Information("IsM9SavagePriority DeadlyDoornail mob found on Melee and in range");
					}
					return true;
				}

				if (role == JobRole.Tank && battleChara.DistanceToPlayer() < 5f && !HasFatalFlail)
				{
					if (Service.Config.InDebug)
					{
						PluginLog.Information("IsM9SavagePriority DeadlyDoornail mob found on Tank and in range");
					}
					return true;
				}
			}

			if (FatalFlail)
			{
				JobRole role = Player.Object?.ClassJob.Value.GetJobRole() ?? JobRole.None;

				if (role == JobRole.Melee)
				{
					if (Service.Config.InDebug)
					{
						PluginLog.Information("IsM9SavagePriority FatalFlail mob found on Melee");
					}
					return true;
				}

				if (role == JobRole.Tank)
				{
					if (Service.Config.InDebug)
					{
						PluginLog.Information("IsM9SavagePriority FatalFlail mob found on Tank");
					}
					return true;
				}

				if (role != JobRole.Tank && role != JobRole.Melee && !HasDeadlyDoornail)
				{
					if (Service.Config.InDebug)
					{
						PluginLog.Information("IsM9SavagePriority FatalFlail mob found on NonMelee");
					}
					return true;
				}
			}

		}

		return false;
	}

	/// <summary>
	/// 
	/// </summary>
	public static bool IsM10SavagePriority(this IBattleChara battleChara)
	{
		if (Player.Object == null)
		{
			return false;
		}

		if (Service.Config.M10SBroTargeting && DataCenter.IsInM10S)
		{
			var RedHot = battleChara.NameId == 14370;
			var DeepBlue = battleChara.NameId == 14369;
			var WateryGrave = battleChara.NameId == 14373;

			var Firesnaking = StatusHelper.PlayerHasStatus(false, StatusID.Firesnaking);
			var Watersnaking = StatusHelper.PlayerHasStatus(false, StatusID.Watersnaking);

			if (RedHot && Firesnaking)
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Information("IsM10SavagePriority RedHot status found");
				}
				return true;
			}

			if (DeepBlue && Watersnaking)
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Information("IsM10SavagePriority DeepBlue status found");
				}
				return true;
			}

			if (WateryGrave)
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Information("IsM10SavagePriority WateryGrave status found");
				}
				return true;
			}

		}

		return false;
	}

	//public static bool IsM12SavagePriority(this IBattleChara battleChara)
	//{
	//	if (Player.Object == null)
	//	{
	//		return false;
	//	}

	//	if (DataCenter.IsInM12S)
	//	{

	//	}

	//	return false;
	//}

	/// <summary>
	/// 
	/// </summary>
	public static bool IsBroPriority(this IBattleChara battleChara)
	{
		if (Player.Object == null)
		{
			return false;
		}

		if (Service.Config.M10SBroTargeting && DataCenter.TerritoryID == 1322)
		{
			var RedHot = battleChara.NameId == 14370;
			var DeepBlue = battleChara.NameId == 14369;

			var Firesnaking = StatusHelper.PlayerHasStatus(false, StatusID.Firesnaking);
			var Watersnaking = StatusHelper.PlayerHasStatus(false, StatusID.Watersnaking);

			if (RedHot && Firesnaking)
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Information("IsBroPriority RedHot status found");
				}
				return true;
			}

			if (DeepBlue && Watersnaking)
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Information("IsBroPriority DeepBlue status found");
				}
				return true;
			}

		}

		return false;
	}

	internal static bool IsSpecialInclusionPriority(this IBattleChara battleChara)
    {
        if (battleChara.NameId == 8145
            || battleChara.NameId == 10259
            || battleChara.NameId == 12704
            || battleChara.NameId == 14052)
        {
            return true;
        }
        //8145 Root in Dohn Meg boss 2
        //10259 Cinduruva in The Tower of Zot
        //12704 Crystalline Debris

        //14052 hellmaker
        if (DataCenter.TerritoryID == 1292)
        {
            if (battleChara.NameId == 14052)
            {
                StatusID CellBlockCPrisoner = (StatusID)4544;
                StatusID CellBlockDPrisoner = (StatusID)4545;

                if (StatusHelper.PlayerHasStatus(false, CellBlockCPrisoner) || StatusHelper.PlayerHasStatus(false, CellBlockDPrisoner))
                {
                    return true;
                }
            }
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

        bool baseCheck = battleChara.IsCasting && battleChara.IsCastInterruptible && battleChara.TotalCastTime >= 2;
        if (!baseCheck)
        {
            return false;
        }

        if (!Service.Config.InterruptibleMoreCheck)
        {
            return false;
        }

        if (_effectRangeCheck.TryGetValue(battleChara.CastActionId, out bool check))
        {
            return check;
        }

        Lumina.Excel.Sheets.Action act = Service.GetSheet<Lumina.Excel.Sheets.Action>().GetRow(battleChara.CastActionId);
        return act.RowId == 0
            ? (_effectRangeCheck[battleChara.CastActionId] = false)
            : act.CastType == 3 || act.CastType == 4 || (act.EffectRange > 0 && act.EffectRange < 8)
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
        return battleChara.IsM9SavageImmune()
			|| battleChara.IsColossusRubricatusImmune()
			|| battleChara.IsColossusRubricatusImmune()
			|| battleChara.IsTrueHeartImmune()
			|| battleChara.IsEminentGriefImmune()
            || battleChara.IsLOTAImmune()
            || battleChara.IsMesoImmune()
            || battleChara.IsJagdDollImmune()
            || battleChara.IsLyreImmune()
            || battleChara.IsDrakeImmune()
            || battleChara.IsWolfImmune()
            || battleChara.IsSuperiorFlightUnitImmune()
            || battleChara.IsJeunoBossImmune()
            || battleChara.IsDeadStarImmune()
            || battleChara.IsCODBossImmune()
            || battleChara.IsCinderDriftImmune()
            || battleChara.IsResistanceImmune()
            || battleChara.IsOmegaImmune()
            || battleChara.IsLimitlessBlue()
            || battleChara.IsHanselorGretelShielded();
    }

	/// <summary>
	/// 
	/// </summary>
	public static bool IsM9SavageImmune(this IBattleChara battleChara)
	{
		if (Player.Object == null)
		{
			return false;
		}

		if (Service.Config.M9SCellTargeting && DataCenter.IsInM9S)
		{
			var CharnelCell = battleChara.NameId == 14304;

			// Heel (on target) vs Hell (on player) pairs
			StatusID HeelInACell1 = (StatusID)4739;
			StatusID HellInACell1 = (StatusID)4731;

			StatusID HeelInACell2 = (StatusID)4740;
			StatusID HellInACell2 = (StatusID)4732;

			StatusID HeelInACell3 = (StatusID)4741;
			StatusID HellInACell3 = (StatusID)4733;

			StatusID HeelInACell4 = (StatusID)4742;
			StatusID HellInACell4 = (StatusID)4734;

			StatusID HeelInACell5 = (StatusID)4743;
			StatusID HellInACell5 = (StatusID)4735;

			StatusID HeelInACell6 = (StatusID)4744;
			StatusID HellInACell6 = (StatusID)4736;

			StatusID HeelInACell7 = (StatusID)4745;
			StatusID HellInACell7 = (StatusID)4737;

			StatusID HeelInACell8 = (StatusID)4746;
			StatusID HellInACell8 = (StatusID)4738;

			if (CharnelCell)
			{
				// Iterate all Heel/Hell pairs; immune if target has Heel and player does NOT have corresponding Hell
				foreach (var (heel, hell) in new (StatusID heel, StatusID hell)[]
				{
					(HeelInACell1, HellInACell1),
					(HeelInACell2, HellInACell2),
					(HeelInACell3, HellInACell3),
					(HeelInACell4, HellInACell4),
					(HeelInACell5, HellInACell5),
					(HeelInACell6, HellInACell6),
					(HeelInACell7, HellInACell7),
					(HeelInACell8, HellInACell8),
				})
				{
					if (battleChara.HasStatus(false, heel) && !StatusHelper.PlayerHasStatus(false, hell))
					{
						if (Service.Config.InDebug)
						{
							PluginLog.Information("IsM9SavageImmune: CharnelCell immune due to Heel/Hell mismatch");
						}
						return true;
					}
				}
			}
		}

		return false;
	}

	/// <summary>
	/// 
	/// </summary>
	public static bool IsColossusRubricatusImmune(this IBattleChara battleChara)
    {
        if (Service.Config.ColossusRubricatusImmune && DataCenter.TerritoryID == 1174)
        {
            var ColossusRubricatus = battleChara.NameId == 9511;

            if (ColossusRubricatus)
            {
                if (battleChara.CastActionId == 14574)
                {
                    if (Service.Config.InDebug)
                    {
                        PluginLog.Information("IsColossusRubricatusImmune action found, ignoring mob");
                    }
                }
                return true;
            }

        }

        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    public static bool IsEminentGriefImmune(this IBattleChara battleChara)
    {
        if (Service.Config.Eminent && (DataCenter.TerritoryID == 1311 || DataCenter.TerritoryID == 1333 || DataCenter.TerritoryID == 1290))
        {
            var EminentGrief = battleChara.NameId == 14037;
            var DevouredEater = battleChara.NameId == 14038;

            var LightVengeance = StatusHelper.PlayerHasStatus(false, StatusID.LightVengeance);
            var DarkVengeance = StatusHelper.PlayerHasStatus(false, StatusID.DarkVengeance);

            if (EminentGrief && !LightVengeance)
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsEminentGriefImmune status found");
                }
                return true;
            }

            if (DevouredEater && !DarkVengeance)
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsEminentGriefImmune status found");
                }
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    public static bool IsLOTAImmune(this IBattleChara battleChara)
    {
        if (Service.Config.ThanatosImmune && DataCenter.TerritoryID == 174)
        {
            var Thanatos = battleChara.NameId == 710;
            var AstralRealignment = StatusHelper.PlayerHasStatus(false, StatusID.AstralRealignment);

            if (Thanatos && !AstralRealignment)
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsLOTAImmune status found");
                }
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    public static bool IsMesoImmune(this IBattleChara battleChara)
    {
        if (Service.Config.JailerImmune && DataCenter.TerritoryID == 1292)
        {
            StatusID CellJailerA = (StatusID)4546;
            StatusID CellJailerB = (StatusID)4547;
            StatusID CellJailerC = (StatusID)4548;
            StatusID CellJailerD = (StatusID)4549;

            var JailerA = battleChara.HasStatus(false, CellJailerA);
            var JailerB = battleChara.HasStatus(false, CellJailerB);
            var JailerC = battleChara.HasStatus(false, CellJailerC);
            var JailerD = battleChara.HasStatus(false, CellJailerD);

            StatusID CellBlockAPrisoner = (StatusID)4542;
            StatusID CellBlockBPrisoner = (StatusID)4543;
            StatusID CellBlockCPrisoner = (StatusID)4544;
            StatusID CellBlockDPrisoner = (StatusID)4545;

            var CellBlockA = StatusHelper.PlayerHasStatus(false, CellBlockAPrisoner);
            var CellBlockB = StatusHelper.PlayerHasStatus(false, CellBlockBPrisoner);
            var CellBlockC = StatusHelper.PlayerHasStatus(false, CellBlockCPrisoner);
            var CellBlockD = StatusHelper.PlayerHasStatus(false, CellBlockDPrisoner);

            if (JailerA && (CellBlockB || CellBlockC || CellBlockD))
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsMesoImmune status found");
                }
                return true;
            }

            if (JailerB && (CellBlockA || CellBlockC || CellBlockD))
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsMesoImmune status found");
                }
                return true;
            }

            if (JailerC && (CellBlockA || CellBlockB || CellBlockD))
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsMesoImmune status found");
                }
                return true;
            }

            if (JailerD && (CellBlockA || CellBlockB || CellBlockC))
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsMesoImmune status found");
                }
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    public static bool IsJagdDollImmune(this IBattleChara battleChara)
    {
        if (Service.Config.TeaJagdDoll && DataCenter.TerritoryID == 887)
        {
            var JagdDoll = battleChara.NameId == 9214;
            var HealthThreshold = battleChara.GetEffectiveHpPercent();

            if (JagdDoll && HealthThreshold < 25f)
            {
                return true;
            }
        }

        return false;
    }

	/// <summary>
	/// 
	/// </summary>
	public static bool IsTrueHeartImmune(this IBattleChara battleChara)
	{
		if (Service.Config.TeaTrueHeart && DataCenter.TerritoryID == 887) // In TEA
		{
			var TrueHeart = battleChara.NameId == 9223;

			if (TrueHeart)
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Information("IsTrueHeartImmune mob found, ignoring mob");
				}
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// 
	/// </summary>
	public static bool IsCrystalOfDarknessImmune(this IBattleChara battleChara)
	{
		if (Service.Config.FruCrystalOfDarkness && DataCenter.TerritoryID == 1238)
		{
			var CrystalOfDarkness = battleChara.NameId == 13556;

			if (CrystalOfDarkness)
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Information("IsCrystalOfDarknessImmune mob found, ignoring mob");
				}
				return true;
			}

		}

		return false;
	}

	/// <summary>
	/// 
	/// </summary>
	public static bool IsLyreImmune(this IBattleChara battleChara)
    {
        if (Service.Config.DohnMhegLyre && DataCenter.TerritoryID == 821)
        {
            var LiarsLyre = battleChara.NameId == 8958;
            var Unfooled = StatusHelper.PlayerHasStatus(false, StatusID.Unfooled);

            if (LiarsLyre && !Unfooled)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    public static bool IsDrakeImmune(this IBattleChara battleChara)
    {
        if (Service.Config.DrakeImmune && DataCenter.TerritoryID == 1069)
        {
            // NameIds for each drake
            const uint DrakefatherId = 11463;
            const uint DrakemotherId = 11464;
            const uint DrakebrotherId = 11465;
            const uint DrakesisterId = 11466;
            const uint DrakelingId = 11467;

            var nameId = battleChara.NameId;

            // Drakemother is immune if Drakefather is alive
            if (nameId == DrakemotherId && CheckDrakesAlive(DrakemotherId, DrakefatherId))
            {
                return true;
            }
            // Drakebrother is immune if Drakemother is alive
            if (nameId == DrakebrotherId && CheckDrakesAlive(DrakebrotherId, DrakemotherId))
            {
                return true;
            }
            // Drakesister is immune if Drakebrother is alive
            if (nameId == DrakesisterId && CheckDrakesAlive(DrakesisterId, DrakebrotherId))
            {
                return true;
            }
            // Drakeling is immune if Drakesister is alive
            if (nameId == DrakelingId && CheckDrakesAlive(DrakelingId, DrakesisterId))
            {
                return true;
            }
        }

        return false;
    }

    private static bool CheckDrakesAlive(uint targetNameId, uint dependentNameId)
    {
        bool targetAlive = false;
        bool dependentAlive = false;

        var targets = DataCenter.AllHostileTargets;
        for (int i = 0, count = targets.Count; i < count; i++)
        {
            var obj = targets[i];
            if (obj?.CurrentHp > 0)
            {
                if (obj.NameId == targetNameId) targetAlive = true;
                else if (obj.NameId == dependentNameId) dependentAlive = true;

                if (targetAlive && dependentAlive) break;
            }
        }

        return targetAlive && dependentAlive;
    }

    /// <summary>
    /// Is target Wolf add immune.
    /// </summary>
    /// <param name="battleChara">the object.</param>
    /// <returns></returns>
    public static bool IsWolfImmune(this IBattleChara battleChara)
    {
        if (Service.Config.M8SWindStone && DataCenter.TerritoryID == 1263)
        {
            // Numeric values used instead of name as Lumina does not provide name yet, and may update to change name
            StatusID WindPack = (StatusID)4389; // Numeric value for "Rsv43891100S74Cfc3B0E74Cfc3B0", unable to hit Wolf of Wind
            StatusID StonePack = (StatusID)4390; // Numeric value for "Rsv43901100S74Cfc3B0E74Cfc3B0", unable to hit Wolf of Stone

            var WolfOfWind = battleChara.NameId == 13846;
            var WolfOfStone = battleChara.NameId == 13847;

            var WindPackPlayer = StatusHelper.PlayerHasStatus(false, WindPack);
            var StonePackPlayer = StatusHelper.PlayerHasStatus(false, StonePack);

            if (WolfOfWind && WindPackPlayer)
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsWolfImmune: WindPack status found");
                }
                return true;
            }

            if (WolfOfStone && StonePackPlayer)
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsWolfImmune: StonePack status found");
                }
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="battleChara">the object.</param>
    /// <returns></returns>
    public static bool IsIrminsulSawtoothImmune(this IBattleChara battleChara)
    {
        if (Service.Config.IrminsulSawtoothImmune && DataCenter.TerritoryID == 508)
        {
            var RangedPhysicalRole = Player.Job.IsPhysicalRangedDps();
            var RangedMagicalRole = Player.Job.IsMagicalRangedDps();
            var HealerRole = Player.Job.IsHealer();

            var RangedResistance = battleChara.HasStatus(false, StatusID.RangedResistance);
            var MagicResistance = battleChara.HasStatus(false, StatusID.MagicResistance);

            if (RangedResistance && RangedPhysicalRole)
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsIrminsulSawtoothImmune: Sawtooth Immune status found");
                }
                return true;
            }

            if (MagicResistance && (RangedMagicalRole || HealerRole))
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsIrminsulSawtoothImmune: Irminsul Immune status found");
                }
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="battleChara">the object.</param>
    /// <returns></returns>
    public static bool IsSuperiorFlightUnitImmune(this IBattleChara battleChara)
    {
        if (Service.Config.SuperiorFlightUnitImmune && DataCenter.TerritoryID == 917)
        {
            var ShieldProtocolAPlayer = StatusHelper.PlayerHasStatus(false, StatusID.ShieldProtocolA);
            var ShieldProtocolBPlayer = StatusHelper.PlayerHasStatus(false, StatusID.ShieldProtocolB);
            var ShieldProtocolCPlayer = StatusHelper.PlayerHasStatus(false, StatusID.ShieldProtocolC);

            var ProcessOfEliminationA = battleChara.HasStatus(false, StatusID.ProcessOfEliminationA);
            var ProcessOfEliminationB = battleChara.HasStatus(false, StatusID.ProcessOfEliminationB);
            var ProcessOfEliminationC = battleChara.HasStatus(false, StatusID.ProcessOfEliminationC);

            if (ProcessOfEliminationA && (ShieldProtocolBPlayer || ShieldProtocolCPlayer))
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsSuperiorFlightUnitImmune: ProcessOfEliminationA Immune status found");
                }
                return true;
            }

            if (ProcessOfEliminationB && (ShieldProtocolAPlayer || ShieldProtocolCPlayer))
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsSuperiorFlightUnitImmune: ProcessOfEliminationB Immune status found");
                }
                return true;
            }

            if (ProcessOfEliminationC && (ShieldProtocolAPlayer || ShieldProtocolBPlayer))
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsSuperiorFlightUnitImmune: ProcessOfEliminationC Immune status found");
                }
                return true;
            }
        }

        return false;
    }

	/// <summary>
	/// Is target Hansel or Gretel and has the Strong of Shield status.
	/// </summary>
	/// <param name="battleChara">the object.</param>
	/// <returns></returns>
	public static bool IsHanselorGretelShielded(this IBattleChara battleChara)
	{
		if (Service.Config.HanselorGretelShieldedImmune && DataCenter.TerritoryID == 966)
		{
			EnemyPositional strongOfShieldPositional = EnemyPositional.Front;
			StatusID strongOfShieldStatus = StatusID.StrongOfShield;

			if (battleChara.HasStatus(false, strongOfShieldStatus) &&
					strongOfShieldPositional != battleChara.FindEnemyPositional())
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Information("IsHanselorGretelShielded: StrongOfShield status found, ignoring status haver if player is out of position");
				}
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Is target Jeuno Boss immune.
	/// </summary>
	/// <param name="battleChara">the object.</param>
	/// <returns></returns>
	public static bool IsJeunoBossImmune(this IBattleChara battleChara)
    {
        if (Service.Config.JeunoBossImmune && DataCenter.TerritoryID == 1248)
        {
            var FatedVillain = battleChara.HasStatus(false, StatusID.FatedVillain);
            var VauntedVillain = battleChara.HasStatus(false, StatusID.VauntedVillain);
            var EpicVillain = battleChara.HasStatus(false, StatusID.EpicVillain);

            var VauntedHero = StatusHelper.PlayerHasStatus(false, StatusID.VauntedHero);
            var FatedHero = StatusHelper.PlayerHasStatus(false, StatusID.FatedHero);
            var EpicHero = StatusHelper.PlayerHasStatus(false, StatusID.EpicHero);

            if (EpicVillain && (VauntedHero || FatedHero))
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsJeunoBossImmune: EpicVillain status found");
                }
                return true;
            }

            if (VauntedVillain && (EpicHero || FatedHero))
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsJeunoBossImmune: VauntedVillain status found");
                }
                return true;
            }

            if (FatedVillain && (EpicHero || VauntedHero))
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsJeunoBossImmune: FatedVillain status found");
                }
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Is target Dead Star immune.
    /// </summary>
    /// <param name="battleChara">the object.</param>
    /// <returns></returns>
    public static bool IsDeadStarImmune(this IBattleChara battleChara)
    {
        if (Service.Config.ForkedtowerDeadStar && DataCenter.IsInForkedTower)
        {
            var Triton = battleChara.NameId == 13730;
            var Nereid = battleChara.NameId == 13731;
            var Phobos = battleChara.NameId == 13732;

            var PhobosicGravity = StatusHelper.PlayerHasStatus(false, StatusID.PhobosicGravity);
            var TritonicGravity = StatusHelper.PlayerHasStatus(false, StatusID.TritonicGravity);
            var NereidicGravity = StatusHelper.PlayerHasStatus(false, StatusID.NereidicGravity);

            if (Triton && (NereidicGravity || PhobosicGravity))
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsDeadStarImmune: Triton immune");
                }
                return true;
            }

            if (Nereid && (TritonicGravity || PhobosicGravity))
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsDeadStarImmune: Nereid immune");
                }
                return true;
            }

            if (Phobos && (TritonicGravity || NereidicGravity))
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsDeadStarImmune: Phobos immune");
                }
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Is target COD Boss immune.
    /// </summary>
    /// <param name="battleChara">the object.</param>
    /// <returns></returns>
    public static bool IsCODBossImmune(this IBattleChara battleChara)
    {
        if (Service.Config.CodImmune && DataCenter.TerritoryID == 1241)
        {
            var CloudOfDarknessStatus = battleChara.HasStatus(false, StatusID.VeilOfDarkness);
            var StygianStatus = battleChara.HasStatus(false, StatusID.UnnamedStatus_4388);

            var AntiCloudOfDarknessStatus = StatusHelper.PlayerHasStatus(false, StatusID.OuterDarkness);
            var AntiStygianStatus = StatusHelper.PlayerHasStatus(false, StatusID.InnerDarkness);

            if (CloudOfDarknessStatus && AntiCloudOfDarknessStatus)
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsCODBossImmune: OuterDarkness status found, CloudOfDarkness immune");
                }
                return true;
            }

            if (StygianStatus && AntiStygianStatus)
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsCODBossImmune: InnerDarkness status found, Stygian immune");
                }
                return true;
            }
        }

        return false;
    }

	/// <summary>
	/// Is target Limitless Blue immune.
	/// </summary>
	/// <param name="battleChara">the object.</param>
	/// <returns></returns>
	public static bool IsLimitlessBlue(this IBattleChara battleChara)
	{
		if (Service.Config.LimitlessBlueTargeting && (DataCenter.TerritoryID == 436 || DataCenter.TerritoryID == 447))
		{
			StatusID WillOfTheWater = StatusID.WillOfTheWater;
			StatusID WillOfTheWind = StatusID.WillOfTheWind;
			StatusID WhaleBack = StatusID.Whaleback;

			bool Green = battleChara.NameId == 3654;
			bool Blue = battleChara.NameId == 3655;
			bool BismarkShell = battleChara.NameId == 3656;
			bool BismarkCorona = battleChara.NameId == 3657;

			if ((BismarkShell || BismarkCorona) &&
					!StatusHelper.PlayerHasStatus(false, WhaleBack))
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Information("IsLimitlessBlue: Bismark found, WhaleBack status not found");
				}
				return true;
			}

			if (Blue &&
				StatusHelper.PlayerHasStatus(false, WillOfTheWater))
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Information("IsLimitlessBlue: WillOfTheWater status found");
				}
				return true;
			}

			if (Green &&
				StatusHelper.PlayerHasStatus(false, WillOfTheWind))
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Information("IsLimitlessBlue: WillOfTheWind status found");
				}
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Is target Cinder Drift Boss immune.
	/// </summary>
	/// <param name="battleChara">the object.</param>
	/// <returns></returns>
	public static bool IsCinderDriftImmune(this IBattleChara battleChara)
    { 
        if (Service.Config.CinderDriftPallTargeting && DataCenter.TerritoryID == 912)
        {
            var GriefAdd = battleChara.HasStatus(false, StatusID.BlindToGrief);
            var RageAdd = battleChara.HasStatus(false, StatusID.BlindToRage);

            var AntiRageAdd = StatusHelper.PlayerHasStatus(false, StatusID.PallOfRage);
            var AntiGriefAdd = StatusHelper.PlayerHasStatus(false, StatusID.PallOfGrief);

            if (GriefAdd && AntiGriefAdd)
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsCinderDriftImmune: AntiGriefAdd status found, GriefAdd immune");
                }
                return true;
            }

            if (RageAdd && AntiRageAdd)
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsCinderDriftImmune: AntiRageAdd status found, RageAdd immune");
                }
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Is target Resistance immune.
    /// </summary>
    /// <param name="battleChara">the object.</param>
    /// <returns></returns>
    public static bool IsResistanceImmune(this IBattleChara battleChara)
    {
        if (DataCenter.TerritoryID == 508 || DataCenter.TerritoryID == 281 || DataCenter.TerritoryID == 359)
        {
            StatusID VoidArkMagicResistance = StatusID.MagicResistance;
            StatusID VoidArkRangedResistance = StatusID.RangedResistance;
            StatusID LeviMagicResistance = StatusID.MantleOfTheWhorl;
            StatusID LeviRangedResistance = StatusID.VeilOfTheWhorl;
            JobRole role = Player.Object?.ClassJob.Value.GetJobRole() ?? JobRole.None;

            if (battleChara.HasStatus(false, VoidArkMagicResistance, LeviMagicResistance) &&
                    (role == JobRole.RangedMagical || role == JobRole.Healer))
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsResistanceImmune: MagicResistance status found");
                }
                return true;
            }

            if (battleChara.HasStatus(false, VoidArkRangedResistance, LeviRangedResistance) &&
                role == JobRole.RangedPhysical)
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsResistanceImmune: RangedResistance status found");
                }
                return true;
            }
        }

        return false;
    }

	/// <summary>
	/// Is target Omega Boss immune.
	/// </summary>
	/// <param name="battleChara">the object.</param>
	/// <returns></returns>
	public static bool IsTOPImmune(this IBattleChara battleChara)
	{
		if (Service.Config.TopOmegaMf && DataCenter.TerritoryID == 1122)
		{
			StatusID AntiOmegaF_Ultimate = StatusID.PacketFilterF_3500;
			StatusID AntiOmegaM_Ultimate = StatusID.PacketFilterM_3499;

			StatusID OmegaF = StatusID.OmegaF;
			StatusID OmegaM = StatusID.Omega;
			StatusID OmegaM2 = StatusID.OmegaM_3454;

			if (battleChara.HasStatus(false, OmegaF) &&
					StatusHelper.PlayerHasStatus(false, AntiOmegaF_Ultimate))
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Information("IsTOPImmune: PacketFilterF status found");
				}
				return true;
			}

			if (battleChara.HasStatus(false, OmegaM, OmegaM2) &&
				StatusHelper.PlayerHasStatus(false, AntiOmegaM_Ultimate))
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Information("IsTOPImmune: PacketFilterM status found");
				}
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Is target Omega Boss immune.
	/// </summary>
	/// <param name="battleChara">the object.</param>
	/// <returns></returns>
	public static bool IsOmegaImmune(this IBattleChara battleChara)
    {
        if (Service.Config.O12SOmegaMf && (DataCenter.TerritoryID == 801 || DataCenter.TerritoryID == 805))
        {
            StatusID AntiOmegaF = StatusID.PacketFilterF;
            StatusID AntiOmegaF_Extreme = StatusID.PacketFilterF_3500;
            StatusID AntiOmegaM = StatusID.PacketFilterM;
            StatusID AntiOmegaM_Extreme = StatusID.PacketFilterM_3499;

            StatusID OmegaF = StatusID.OmegaF;
            StatusID OmegaM = StatusID.OmegaM;
            StatusID OmegaM2 = StatusID.OmegaM_3454;

            if (battleChara.HasStatus(false, OmegaF) &&
					StatusHelper.PlayerHasStatus(false, AntiOmegaF, AntiOmegaF_Extreme))
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsOmegaImmune: PacketFilterF status found");
                }
                return true;
            }

            if (battleChara.HasStatus(false, OmegaM, OmegaM2) &&
                StatusHelper.PlayerHasStatus(false, AntiOmegaM, AntiOmegaM_Extreme))
            {
                if (Service.Config.InDebug)
                {
                    PluginLog.Information("IsOmegaImmune: PacketFilterM status found");
                }
                return true;
            }
        }

        return false;
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
            return 0;

        uint effectiveHp = character.CurrentHp + ObjectHelper.GetObjectShield(battleChara);
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

        ulong id = battleChara.GameObjectId;
        Vector3 currentPos = battleChara.Position;
        if (LastPositions.TryGetValue(id, out Vector3 lastPos))
        {
            // You can adjust the threshold as needed
            bool isMoving = Vector3.Distance(currentPos, lastPos) > 0.01f;
            LastPositions[id] = currentPos;
            return isMoving;
        }
        else
        {
            LastPositions[id] = currentPos;
            return false; // First check, assume not moving
        }
    }

    private static readonly TimeSpan CheckSpan = TimeSpan.FromSeconds(2.5);

    /// <summary>
    /// Calculates the estimated time to kill the specified battle character. Only applicable after the first 2.5 seconds, and uses a moving average of the last 15 seconds of health ratios
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

        const int movingAverageWindow = 5;
        ulong objId = battleChara.GameObjectId;

        DateTime startTime = DateTime.MinValue;
        float initialHpRatio = 0f;

        // Small fixed-size window for last ratios without copying the whole queue
        float[] window = new float[movingAverageWindow];
        int wCount = 0;

        foreach ((DateTime time, Dictionary<ulong, float> hpRatiosDict) in DataCenter.RecordedHP)
        {
            if (hpRatiosDict != null && hpRatiosDict.TryGetValue(objId, out float ratio) && ratio != 1f)
            {
                if (startTime == DateTime.MinValue)
                {
                    startTime = time;
                    initialHpRatio = ratio;
                }

                if (wCount < movingAverageWindow)
                {
                    window[wCount++] = ratio;
                }
                else
                {
                    // shift left by one; window is very small so this is cheap
                    Array.Copy(window, 1, window, 0, movingAverageWindow - 1);
                    window[movingAverageWindow - 1] = ratio;
                }
            }
        }

        if (startTime == DateTime.MinValue || (DateTime.Now - startTime) < CheckSpan)
        {
            return float.NaN;
        }

        float currentHealthRatio = battleChara.GetHealthRatio();
        if (float.IsNaN(currentHealthRatio))
        {
            return float.NaN;
        }

        float sum = 0f;
        for (int i = 0; i < wCount; i++) sum += window[i];
        float avg = wCount > 0 ? sum / wCount : 0f;

        float hpRatioDifference = initialHpRatio - avg;
        if (hpRatioDifference <= 0)
        {
            return float.NaN;
        }

        float elapsedTime = (float)(DateTime.Now - startTime).TotalSeconds;
        return elapsedTime / hpRatioDifference * (wholeTime ? 1 : currentHealthRatio);
    }

    private static readonly ConcurrentDictionary<ulong, DateTime> _aliveStartTimes = [];

	/// <summary>
	/// Gets how long the Player has been alive in seconds since their last death.
	/// </summary>
	/// <returns>
	/// The time in seconds since the character's last death or first appearance, or float.NaN if unable to determine.
	/// </returns>
	internal static float PlayerTimeAlive()
	{
		if (Player.Object == null)
		{
			return float.NaN;
		}

		// If the character is dead, reset their alive time
		if (Player.Object.IsDead || Svc.Condition[ConditionFlag.BetweenAreas])
		{
			_ = _aliveStartTimes.TryRemove(Player.Object.GameObjectId, out _);
			return 0;
		}

		// If we haven't tracked this character yet, start tracking them
		if (!_aliveStartTimes.ContainsKey(Player.Object.GameObjectId))
		{
			_aliveStartTimes[Player.Object.GameObjectId] = DateTime.Now;
		}

		return (float)(DateTime.Now - _aliveStartTimes[Player.Object.GameObjectId]).TotalSeconds > 30 ? 30 : (float)(DateTime.Now - _aliveStartTimes[Player.Object.GameObjectId]).TotalSeconds;
	}

	/// <summary>
	/// Gets how long the battleChara has been alive in seconds since their last death.
	/// </summary>
	/// <param name="battleChara">The battle character to check.</param>
	/// <returns>
	/// The time in seconds since the character's last death or first appearance, or float.NaN if unable to determine.
	/// </returns>
	internal static float TimeAlive(this IBattleChara battleChara)
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

        // If we haven't tracked this character yet, start tracking them
        if (!_aliveStartTimes.ContainsKey(battleChara.GameObjectId))
        {
            _aliveStartTimes[battleChara.GameObjectId] = DateTime.Now;
        }

        return (float)(DateTime.Now - _aliveStartTimes[battleChara.GameObjectId]).TotalSeconds > 30 ? 30 : (float)(DateTime.Now - _aliveStartTimes[battleChara.GameObjectId]).TotalSeconds;
    }

    private static readonly ConcurrentDictionary<ulong, DateTime> _deadStartTimes = [];

    /// <summary>
    /// Gets how long the character has been dead in seconds since their last death.
    /// </summary>
    /// <param name="battleChara">The battle character to check.</param>
    /// <returns>
    /// The time in seconds since the character's death, or float.NaN if unable to determine or character is alive.
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

        // If we haven't tracked this character's death yet, start tracking them
        if (!_deadStartTimes.ContainsKey(battleChara.GameObjectId))
        {
            _deadStartTimes[battleChara.GameObjectId] = DateTime.Now;
        }

        return (float)(DateTime.Now - _deadStartTimes[battleChara.GameObjectId]).TotalSeconds > 30 ? 30 : (float)(DateTime.Now - _deadStartTimes[battleChara.GameObjectId]).TotalSeconds;
    }

    /// <summary>
    /// Determines if the specified battle character has been attacked within the last second.
    /// </summary>
    /// <param name="battleChara">The battle character to check.</param>
    /// <returns>
    /// <c>true</c> if the battle character has been attacked within the last second; otherwise, <c>false</c>.
    /// </returns>
    internal static bool IsAttacked(this IBattleChara battleChara)
    {
        if (battleChara == null)
        {
            return false;
        }

        DateTime now = DateTime.Now;
        foreach ((ulong id, DateTime time) in DataCenter.AttackedTargets)
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
            _losCache.Clear();
    }

    // New overload to allow caller to supply eye position once per loop
    internal static unsafe bool CanSeeFrom(this IBattleChara battleChara, Vector3 playerEyePos, float targetYOffset = 2.0f)
    {
        if (battleChara == null || Player.Object == null) return false;
        var targetStruct = battleChara.Struct();
        if (targetStruct == null) return false;

        MaybeResetLosCache();

        Vector3 targetPos = battleChara.Position; targetPos.Y += targetYOffset;
        ulong id = battleChara.GameObjectId;
        long now = Environment.TickCount64;

        if (_losCache.TryGetValue(id, out var entry))
        {
            if (now <= entry.ExpiresAt &&
                Vector3.DistanceSquared(playerEyePos, entry.PlayerPos) <= LosPosEpsilonSq &&
                Vector3.DistanceSquared(targetPos, entry.TargetPos) <= LosPosEpsilonSq)
            {
                return entry.Visible;
            }
        }

        Vector3 offset = targetPos - playerEyePos;
        float maxDist = offset.Length();
        if (maxDist < 0.01f) return true;

        Vector3 direction = offset / maxDist;

        RaycastHit hit;
        int* materialFilter = stackalloc int[] { 0x4000, 0, 0x4000, 0 };
        var module = Framework.Instance()->BGCollisionModule;
        bool blocked = module->RaycastMaterialFilter(&hit, &playerEyePos, &direction, maxDist, 1, materialFilter);

        bool visible = !blocked;
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
        if (battleChara == null || Player.Object == null) return false;
        Vector3 playerPos = Player.Object.Position; playerPos.Y += playerYOffset;
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

		if (Player.Object.MaxHp == 0)
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

		if (DataCenter.RefinedHP.TryGetValue(Player.Object.GameObjectId, out float hp))
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

        if (DataCenter.RefinedHP.TryGetValue(battleChara.GameObjectId, out float hp))
        {
            return hp;
        }

        if (battleChara.MaxHp == 0)
        {
            return 0; // Avoid division by zero
        }

        return (float)battleChara.CurrentHp / battleChara.MaxHp;
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
		if (enemy == null || Player.Object == null)
		{
			return EnemyPositional.None;
		}

		Vector3 pPosition = enemy.Position;
		Vector3 faceVec = enemy.GetFaceVector();

		Vector3 dir = Player.Object.Position - pPosition;
		dir = Vector3.Normalize(dir);
		faceVec = Vector3.Normalize(faceVec);

		// Calculate the angle between the direction vector and the facing vector
		double dotProduct = Vector3.Dot(faceVec, dir);
		double angle = Math.Acos(dotProduct);

		const double frontAngle = Math.PI / 4;
		const double rearAngle = Math.PI * 3 / 4;

		if (angle < frontAngle)
		{
			return EnemyPositional.Front;
		}
		else if (angle > rearAngle)
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
        if (battleChara == null)
        {
            return Vector3.Zero;
        }

        float rotation = battleChara.Rotation;
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

		float distance = Vector3.Distance(Player.Object.Position, battleChara.Position) - (Player.Object.HitboxRadius + battleChara.HitboxRadius);
		return distance;
	}

}