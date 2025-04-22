using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using RotationSolver.Basic.Configuration;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace RotationSolver.Basic.Helpers;

/// <summary>
/// Get the information from object.
/// </summary>
public static class ObjectHelper
{
    static readonly EventHandlerContent[] _eventType =
    {
        FFXIVClientStructs.FFXIV.Client.Game.Event.EventHandlerContent.TreasureHuntDirector,
        EventHandlerContent.Quest,
    };

    internal static Lumina.Excel.Sheets.BNpcBase? GetObjectNPC(this IGameObject obj)
    {
        return obj == null ? null : Service.GetSheet<Lumina.Excel.Sheets.BNpcBase>().GetRow(obj.DataId);
    }

    internal static bool CanProvoke(this IGameObject target)
    {
        if (target == null) return false;

        if (DataCenter.FateId != 0 && target.FateId() == DataCenter.FateId) return false;

        // Removed the listed names.
        if (OtherConfiguration.NoProvokeNames.TryGetValue(Svc.ClientState.TerritoryType, out var ns1))
        {
            foreach (var n in ns1)
            {
                if (!string.IsNullOrEmpty(n) && new Regex(n).Match(target.Name?.GetText() ?? string.Empty).Success)
                {
                    return false;
                }
            }
        }

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
        return false;
    }

    internal static bool HasPositional(this IGameObject obj)
    {
        return obj != null && (!(obj.GetObjectNPC()?.IsOmnidirectional ?? false)); // Unknown10 used to be the flag for no positional, believe this was changed to IsOmnidirectional
    }

    internal static unsafe bool IsOthersPlayers(this IGameObject obj)
    {
        //SpecialType but no NamePlateIcon
        return _eventType.Contains(obj.GetEventType()) && obj.GetNamePlateIcon() == 0;
    }

    internal static bool IsAttackable(this IBattleChara battleChara)
    {
        if (battleChara.IsAllianceMember()) return false;

        // Dead.
        if (Service.Config.FilterOneHpInvincible && battleChara.CurrentHp <= 1) return false;

        // Check if the target is invincible.
        if (Service.Config.CodTarget && battleChara.IsCODBossImmune()) return false;
        if (Service.Config.JeunoTarget && battleChara.IsJeunoBossImmune()) return false;
        if (Service.Config.StrongOfSheildTarget && battleChara.IsHanselorGretelSheilded()) return false;

        // Ensure StatusList is not null before accessing it
        if (battleChara.StatusList == null) return false;

        foreach (var status in battleChara.StatusList)
        {
            if (StatusHelper.IsInvincible(status) && (DataCenter.IsPvP && !Service.Config.IgnorePvPInvincibility || !DataCenter.IsPvP)) return false;
        }

        if (Svc.ClientState == null) return false;

        // In No Hostiles Names
        if (OtherConfiguration.NoHostileNames != null &&
            OtherConfiguration.NoHostileNames.TryGetValue(Svc.ClientState.TerritoryType, out var ns1))
        {
            foreach (var n in ns1)
            {
                if (!string.IsNullOrEmpty(n) && new Regex(n).Match(battleChara.Name.TextValue).Success)
                {
                    return false;
                }
            }
        }

        // Fate
        if (DataCenter.Territory?.ContentType != TerritoryContentType.Eureka)
        {
            if (battleChara.FateId() != 0 && battleChara.FateId() != DataCenter.FateId) return false;
        }

        if (Service.Config.TargetQuestThings && battleChara.IsOthersPlayers()) return false;

        if (battleChara.IsTopPriorityNamedHostile()) return true;

        if (battleChara.IsTopPriorityHostile()) return true;

        if (Service.CountDownTime > 0 || DataCenter.IsPvP) return true;

        // Tar on me
        if (battleChara.TargetObject == Player.Object
            || battleChara.TargetObject?.OwnerId == Player.Object.GameObjectId) return true;

        return DataCenter.CurrentTargetToHostileType switch
        {
            TargetHostileType.AllTargetsCanAttack => true,
            TargetHostileType.TargetsHaveTarget => battleChara.TargetObject is IBattleChara,
            TargetHostileType.AllTargetsWhenSolo => DataCenter.PartyMembers.Count == 1 || battleChara.TargetObject is IBattleChara,
            TargetHostileType.AllTargetsWhenSoloInDuty => (DataCenter.PartyMembers.Count == 1 && (Svc.Condition[ConditionFlag.BoundByDuty] || Svc.Condition[ConditionFlag.BoundByDuty56]))
                                || battleChara.TargetObject is IBattleChara,
            //Below options do not work while in party, isAttackable will always return false
            TargetHostileType.TargetIsInEnemiesList => battleChara.TargetObject is IBattleChara target && target.IsInEnemiesList(),
            TargetHostileType.AllTargetsWhenSoloTargetIsInEnemiesList => (DataCenter.PartyMembers.Count == 1 && (Svc.Condition[ConditionFlag.BoundByDuty] || Svc.Condition[ConditionFlag.BoundByDuty56])) || battleChara.TargetObject is IBattleChara target && target.IsInEnemiesList(),
            TargetHostileType.AllTargetsWhenSoloInDutyTargetIsInEnemiesList => DataCenter.PartyMembers.Count == 1 || battleChara.TargetObject is IBattleChara target && target.IsInEnemiesList(),
            _ => true,
        };
    }

    private static string RemoveControlCharacters(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Use a StringBuilder for efficient string manipulation
        var output = new StringBuilder(input.Length);
        foreach (char c in input)
        {
            // Exclude control characters and private use area characters
            if (!char.IsControl(c) && (c < '\uE000' || c > '\uF8FF'))
            {
                output.Append(c);
            }
        }
        return output.ToString();
    }

    internal static unsafe bool IsInEnemiesList(this IBattleChara battleChara)
    {
        if (Service.GetAddons<AddonEnemyList>() == null) return false;
        if (!Service.GetAddons<AddonEnemyList>().Any()) return false;
        if (Service.GetAddons<AddonEnemyList>().First() == IntPtr.Zero) return false;
        if ((AddonEnemyList*)Service.GetAddons<AddonEnemyList>().First() == null) return false;
        if (((AddonEnemyList*)Service.GetAddons<AddonEnemyList>().First())->EnemyOneComponent == null) return false;

        for (int i = 0; i < ((AddonEnemyList*)Service.GetAddons<AddonEnemyList>().First())->EnemyCount; i++)
        {
            if (*(((AddonEnemyList*)Service.GetAddons<AddonEnemyList>().First())->EnemyOneComponent + i) == null) continue;

            for (int j = 0; j < (*(((AddonEnemyList*)Service.GetAddons<AddonEnemyList>().First())->EnemyOneComponent + i))->AtkComponentBase.UldManager.NodeListCount; j++)
            {
                if ((*(((AddonEnemyList*)Service.GetAddons<AddonEnemyList>().First())->EnemyOneComponent + i))->AtkComponentBase.UldManager.NodeList[j] == null) continue;
                if ((*(((AddonEnemyList*)Service.GetAddons<AddonEnemyList>().First())->EnemyOneComponent + i))->AtkComponentBase.UldManager.NodeList[j]->Type != NodeType.Text) continue;

                if (!string.IsNullOrEmpty(RemoveControlCharacters(((AtkTextNode*)(*(((AddonEnemyList*)Service.GetAddons<AddonEnemyList>().First())->EnemyOneComponent + i))->AtkComponentBase.UldManager.NodeList[j])->NodeText.StringPtr.ToString())) &&
                    string.Equals(RemoveControlCharacters(((AtkTextNode*)(*(((AddonEnemyList*)Service.GetAddons<AddonEnemyList>().First())->EnemyOneComponent + i))->AtkComponentBase.UldManager.NodeList[j])->NodeText.StringPtr.ToString()), battleChara.Name.TextValue, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }


    internal static unsafe bool IsEnemy(this IGameObject obj)
    => obj != null
    && ActionManager.CanUseActionOnTarget((uint)ActionID.BlizzardPvE, obj.Struct());

    internal static unsafe bool IsAllianceMember(this IGameObject obj)
        => obj.GameObjectId is not 0
        && (!DataCenter.IsPvP && DataCenter.IsInAllianceRaid && obj is IPlayerCharacter && (ActionManager.CanUseActionOnTarget((uint)ActionID.RaisePvE, obj.Struct()) || ActionManager.CanUseActionOnTarget((uint)ActionID.CurePvE, obj.Struct())));

    internal static bool IsParty(this IGameObject? gameObject)
    {
        if (gameObject == null) return false;

        if (Player.Object == null) return false;

        if (gameObject.GameObjectId == Player.Object.GameObjectId) return true;

        foreach (var p in Svc.Party)
        {
            if (p?.GameObject?.GameObjectId == gameObject.GameObjectId) return true;
        }

        if (Service.Config.FriendlyPartyNpcHealRaise3 && gameObject.IsNpcPartyMember()) return true;
        if (Service.Config.ChocoboPartyMember && gameObject.IsPlayerCharacterChocobo()) return true;
        if (Service.Config.FriendlyBattleNpcHeal && gameObject.IsFriendlyBattleNPC()) return true;
        if (Service.Config.FocusTargetIsParty && gameObject.IsFocusTarget() && gameObject.IsAllianceMember()) return true;

        return false;
    }

    internal static bool IsNpcPartyMember(this IGameObject gameObj)
    {
        return gameObj?.GetBattleNPCSubKind() == BattleNpcSubKind.NpcPartyMember;
    }

    internal static bool IsPlayerCharacterChocobo(this IGameObject gameObj)
    {
        return gameObj?.GetBattleNPCSubKind() == BattleNpcSubKind.Chocobo;
    }

    internal static bool IsFriendlyBattleNPC(this IGameObject? gameObj)
    {
        if (gameObj == null)
        {
            return false;
        }

        var nameplateKind = gameObj?.GetNameplateKind();
        return nameplateKind == NameplateKind.FriendlyBattleNPC;
    }

    internal static bool IsFocusTarget(this IGameObject gameObj)
    {
        return Svc.Targets.FocusTarget != null && Svc.Targets.FocusTarget.GameObjectId == gameObj.GameObjectId;
    }

    internal static bool IsTargetOnSelf(this IBattleChara IBattleChara)
    {
        return IBattleChara.TargetObject?.TargetObject == IBattleChara;
    }

    internal static bool IsDeathToRaise(this IGameObject obj)
    {
        if (obj == null) return false;
        if (!obj.IsDead || !obj.IsTargetable) return false;
        if (obj is IBattleChara b && b.CurrentHp != 0) return false;
        if (obj.HasStatus(false, StatusID.Raise)) return false;
        if (!Service.Config.RaiseBrinkOfDeath && obj.HasStatus(false, StatusID.BrinkOfDeath)) return false;
        foreach (var c in DataCenter.PartyMembers)
        {
            if (c.CastTargetObjectId == obj.GameObjectId) return false;
        }

        return true;
    }

    internal static bool IsAlive(this IGameObject obj)
    {
        return obj is not IBattleChara b || b.CurrentHp > 0 && obj.IsTargetable;
    }

    /// <summary>
    /// Get the object kind.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static unsafe ObjectKind GetObjectKind(this IGameObject obj)
    => (ObjectKind)obj.Struct()->ObjectKind;

    /// <summary>
    /// Determines whether the specified game object is a top priority hostile target based on its name being listed.
    /// </summary>
    /// <param name="obj">The game object to check.</param>
    /// <returns>
    /// <c>true</c> if the game object is a top priority named hostile, target; otherwise, <c>false</c>.
    /// </returns>
    internal static bool IsTopPriorityNamedHostile(this IGameObject obj)
    {
        if (obj == null) return false;

        if (obj is IBattleChara npc)
        {
            foreach (var id in DataCenter.PrioritizedNameIds)
            {
                if (npc.NameId == id) return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether the specified game object is a top priority hostile target.
    /// </summary>
    /// <param name="obj">The game object to check.</param>
    /// <returns>
    /// <c>true</c> if the game object is a top priority hostile target; otherwise, <c>false</c>.
    /// </returns>
    internal static bool IsTopPriorityHostile(this IGameObject obj)
    {
        if (obj == null) return false;
        if (obj.IsAllianceMember() || obj.IsParty()) return false;

        if (obj is IBattleChara b)
        {
            if (Player.Object == null) return false;
            // Check IBattleChara against the priority target list of OIDs
            if (PriorityTargetHelper.IsPriorityTarget(b.DataId)) return true;

            if (Player.Job == Job.MCH && obj.HasStatus(true, StatusID.Wildfire)) return true;

            // Ensure StatusList is not null before calling Any
            foreach (var status in b.StatusList)
            {
                if (StatusHelper.IsPriority(status)) return true;
            }
        }

        if (Service.Config.ChooseAttackMark && MarkingHelper.GetAttackSignTargets().Any(id => id != 0 && id == (long)obj.GameObjectId && obj.IsEnemy())) return true;

        if (Service.Config.TargetFatePriority && DataCenter.FateId != 0 && obj.FateId() == DataCenter.FateId) return true;

        var icon = obj.GetNamePlateIcon();

        // Hunting log and weapon
        if (Service.Config.TargetHuntingRelicLevePriority && (icon == 60092 || icon == 60096 || icon == 71244)) return true;
        //60092 Hunt Target
        //60096 Relic Weapon
        //71244 Leve Target

        // Quest
        if (Service.Config.TargetQuestPriority && (icon == 71204 || icon == 71144 || icon == 71224 || icon == 71344 || obj.GetEventType() == EventHandlerContent.Quest)) return true;
        //71204 Main Quest
        //71144 Major Quest
        //71224 Other Quest
        //71344 Major Quest

        // Check if the object is a BattleNpcPart
        if (Service.Config.PrioEnemyParts && obj.GetBattleNPCSubKind() == BattleNpcSubKind.BattleNpcPart) return true;

        return false;
    }

    internal static unsafe uint GetNamePlateIcon(this IGameObject obj) => obj.Struct()->NamePlateIconId;

    internal static unsafe EventHandlerContent GetEventType(this IGameObject obj) => obj.Struct()->EventId.ContentId;

    internal static unsafe BattleNpcSubKind GetBattleNPCSubKind(this IGameObject obj) => (BattleNpcSubKind)obj.Struct()->SubKind;

    internal static unsafe uint FateId(this IGameObject obj) => obj.Struct()->FateId;

    static readonly ConcurrentDictionary<uint, bool> _effectRangeCheck = [];

    /// <summary>
    /// Determines whether the specified game object can be interrupted.
    /// </summary>
    /// <param name="obj">The game object to check.</param>
    /// <returns>
    /// <c>true</c> if the game object can be interrupted; otherwise, <c>false</c>.
    /// </returns>
    internal static bool CanInterrupt(this IBattleChara obj)
    {
        if (obj == null) return false;
        if (obj is not IBattleChara b) return false;

        // Ensure the IBattleChara object is valid before accessing its properties
        unsafe
        {
            if (b.Struct() == null) return false;
        }

        var baseCheck = b.IsCasting && b.IsCastInterruptible && b.TotalCastTime >= 2;
        if (!baseCheck) return false;
        if (!Service.Config.InterruptibleMoreCheck) return false;

        if (_effectRangeCheck.TryGetValue(b.CastActionId, out var check)) return check;

        var act = Service.GetSheet<Lumina.Excel.Sheets.Action>().GetRow(b.CastActionId);
        if (act.RowId == 0) return _effectRangeCheck[b.CastActionId] = false;
        if (act.CastType == 3 || act.CastType == 4 || (act.EffectRange > 0 && act.EffectRange < 8)) return _effectRangeCheck[b.CastActionId] = false;

        return _effectRangeCheck[b.CastActionId] = true;
    }

    internal static bool IsDummy(this IBattleChara obj) => obj?.NameId == 541;

    /// <summary>
    /// Is target Jeuno Boss immune.
    /// </summary>
    /// <param name="obj">the object.</param>
    /// <returns></returns>
    public static bool IsJeunoBossImmune(this IBattleChara obj)
    {
        if (obj == null) return false;
        if (Player.Object == null) return false;

        if (obj.IsEnemy())
        {
            if (obj.HasStatus(false, StatusID.EpicVillain) &&
                (Player.Object.HasStatus(false, StatusID.VauntedHero) || Player.Object.HasStatus(false, StatusID.FatedHero)))
            {
                if (Service.Config.InDebug)
                {
                    Svc.Log.Information("IsJeunoBossImmune: EpicVillain status found");
                }
                return true;
            }

            if (obj.HasStatus(false, StatusID.VauntedVillain) &&
                (Player.Object.HasStatus(false, StatusID.EpicHero) || Player.Object.HasStatus(false, StatusID.FatedHero)))
            {
                if (Service.Config.InDebug)
                {
                    Svc.Log.Information("IsJeunoBossImmune: VauntedVillain status found");
                }
                return true;
            }

            if (obj.HasStatus(false, StatusID.FatedVillain) &&
                (Player.Object.HasStatus(false, StatusID.EpicHero) || Player.Object.HasStatus(false, StatusID.VauntedHero)))
            {
                if (Service.Config.InDebug)
                {
                    Svc.Log.Information("IsJeunoBossImmune: FatedVillain status found");
                }
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Is target COD Boss immune.
    /// </summary>
    /// <param name="obj">the object.</param>
    /// <returns></returns>
    public static bool IsCODBossImmune(this IBattleChara obj)
    {
        if (obj == null) return false;
        if (Player.Object == null) return false;

        var StygianStatus = StatusID.UnnamedStatus_4388;
        var CloudOfDarknessStatus = StatusID.VeilOfDarkness;
        var AntiCloudOfDarknessStatus = StatusID.OuterDarkness;
        var AntiStygianStatus = StatusID.InnerDarkness;

        if (obj.IsEnemy())
        {
            if (obj.HasStatus(false, CloudOfDarknessStatus) &&
                Player.Object.HasStatus(false, AntiCloudOfDarknessStatus))
            {
                if (Service.Config.InDebug)
                {
                    Svc.Log.Information("IsCODBossImmune: OuterDarkness status found, CloudOfDarkness immune");
                }
                return true;
            }

            if (obj.HasStatus(false, StygianStatus) &&
                Player.Object.HasStatus(false, AntiStygianStatus))
            {
                if (Service.Config.InDebug)
                {
                    Svc.Log.Information("IsCODBossImmune: InnerDarkness status found, Stygian immune");
                }
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Is target Jeuno Boss immune.
    /// </summary>
    /// <param name="obj">the object.</param>
    /// <returns></returns>
    public static bool IsHanselorGretelSheilded(this IBattleChara obj)
    {
        if (obj == null) return false;
        if (Player.Object == null) return false;

        var strongOfShieldPositional = EnemyPositional.Front;
        var strongOfShieldStatus = StatusID.StrongOfShield;

        if (obj.IsEnemy())
        {
            if (obj.HasStatus(false, strongOfShieldStatus) &&
                strongOfShieldPositional != obj.FindEnemyPositional())
            {
                if (Service.Config.InDebug)
                {
                    Svc.Log.Information("IsHanselorGretelSheilded: StrongOfShield status found, ignoring status haver if player is out of position");
                }
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Is target a boss depends on the ttk.
    /// </summary>
    /// <param name="obj">the object.</param>
    /// <returns></returns>
    public static bool IsBossFromTTK(this IBattleChara obj)
    {
        if (obj == null) return false;
        if (Player.Object == null) return false;

        if (obj.IsDummy()) return true;

        //Fate
        return obj.GetTTK(true) >= Service.Config.BossTimeToKill;
    }

    /// <summary>
    /// Is target a boss depends on the icon.
    /// </summary>
    /// <param name="obj">the object.</param>
    /// <returns></returns>
    public static bool IsBossFromIcon(this IBattleChara obj)
    {
        if (obj == null) return false;
        if (Player.Object == null) return false;

        if (obj.IsDummy()) return true;

        //Icon
        var npc = obj.GetObjectNPC();
        return npc?.Rank is 1 or 2 or 6;
    }

    /// <summary>
    /// Is object dying.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static bool IsDying(this IBattleChara obj)
    {
        if (obj == null) return false;
        if (Player.Object == null) return false;

        if (obj.IsDummy()) return false;
        return obj.GetTTK() <= Service.Config.DyingTimeToKill || obj.GetHealthRatio() < Service.Config.IsDyingConfig;
    }

    /// <summary>
    /// Determines whether the specified battle character is currently in combat.
    /// </summary>
    /// <param name="obj">The battle character to check.</param>
    /// <returns>
    /// <c>true</c> if the battle character is in combat; otherwise, <c>false</c>.
    /// </returns>
    internal static unsafe bool InCombat(this IBattleChara obj)
    {
        if (obj == null) return false;
        if (Player.Object == null) return false;
        if (obj.Struct() == null) return false;

        return obj.Struct()->Character.InCombat;
    }

    private static readonly TimeSpan CheckSpan = TimeSpan.FromSeconds(2.5);

    /// <summary>
    /// Calculates the estimated time to kill the specified battle character.
    /// </summary>
    /// <param name="obj">The battle character to calculate the time to kill for.</param>
    /// <param name="wholeTime">If set to <c>true</c>, calculates the total time to kill; otherwise, calculates the remaining time to kill.</param>
    /// <returns>
    /// The estimated time to kill the battle character in seconds, or <see cref="float.NaN"/> if the calculation cannot be performed.
    /// </returns>
    internal static float GetTTK(this IBattleChara obj, bool wholeTime = false)
    {
        if (obj == null) return float.NaN;
        if (obj.IsDummy()) return 999.99f;

        DateTime startTime = DateTime.MinValue;
        float initialHpRatio = 0;

        // Use a snapshot of the RecordedHP collection to avoid modification during enumeration
        var recordedHPCopy = DataCenter.RecordedHP.ToArray();

        // Calculate a moving average of HP ratios
        const int movingAverageWindow = 5;
        Queue<float> hpRatios = new Queue<float>();

        foreach (var (time, hpRatiosDict) in recordedHPCopy)
        {
            if (hpRatiosDict.TryGetValue(obj.GameObjectId, out var ratio) && ratio != 1)
            {
                if (startTime == DateTime.MinValue)
                {
                    startTime = time;
                    initialHpRatio = ratio;
                }

                hpRatios.Enqueue(ratio);
                if (hpRatios.Count > movingAverageWindow)
                {
                    hpRatios.Dequeue();
                }
            }
        }

        if (startTime == DateTime.MinValue || (DateTime.Now - startTime) < CheckSpan) return float.NaN;

        if (float.IsNaN(obj.GetHealthRatio())) return float.NaN;

        // Calculate the moving average of the HP ratios
        var hpRatioDifference = initialHpRatio - hpRatios.Average();
        if (hpRatioDifference <= 0) return float.NaN;

        var elapsedTime = (float)(DateTime.Now - startTime).TotalSeconds;
        return elapsedTime / hpRatioDifference * (wholeTime ? 1 : obj.GetHealthRatio());
    }

    private static readonly ConcurrentDictionary<ulong, DateTime> _aliveStartTimes = new();

    /// <summary>
    /// Gets how long the character has been alive in seconds since their last death.
    /// </summary>
    /// <param name="obj">The battle character to check.</param>
    /// <returns>
    /// The time in seconds since the character's last death or first appearance, or float.NaN if unable to determine.
    /// </returns>
    internal static float TimeAlive(this IBattleChara obj)
    {
        if (obj == null) return float.NaN;
        if (Player.Object == null) return float.NaN;

        // If the character is dead, reset their alive time
        if (obj.IsDead || Svc.Condition[ConditionFlag.BetweenAreas])
        {
            _aliveStartTimes.TryRemove(obj.GameObjectId, out _);
            return 0;
        }

        // If we haven't tracked this character yet, start tracking them
        if (!_aliveStartTimes.ContainsKey(obj.GameObjectId))
        {
            _aliveStartTimes[obj.GameObjectId] = DateTime.Now;
        }

        return (float)(DateTime.Now - _aliveStartTimes[obj.GameObjectId]).TotalSeconds > 30 ? 30 : (float)(DateTime.Now - _aliveStartTimes[obj.GameObjectId]).TotalSeconds;
    }

    private static readonly ConcurrentDictionary<ulong, DateTime> _deadStartTimes = new();

    /// <summary>
    /// Gets how long the character has been dead in seconds since their last death.
    /// </summary>
    /// <param name="obj">The battle character to check.</param>
    /// <returns>
    /// The time in seconds since the character's death, or float.NaN if unable to determine or character is alive.
    /// </returns>
    internal static float TimeDead(this IBattleChara obj)
    {
        if (obj == null) return float.NaN;
        if (Player.Object == null) return float.NaN;

        // If the character is alive, reset their dead time
        if (!obj.IsDead)
        {
            _deadStartTimes.TryRemove(obj.GameObjectId, out _);
            return 0;
        }

        // If we haven't tracked this character's death yet, start tracking them
        if (!_deadStartTimes.ContainsKey(obj.GameObjectId))
        {
            _deadStartTimes[obj.GameObjectId] = DateTime.Now;
        }

        return (float)(DateTime.Now - _deadStartTimes[obj.GameObjectId]).TotalSeconds > 30 ? 30 : (float)(DateTime.Now - _deadStartTimes[obj.GameObjectId]).TotalSeconds;
    }

    /// <summary>
    /// Determines if the specified battle character has been attacked within the last second.
    /// </summary>
    /// <param name="obj">The battle character to check.</param>
    /// <returns>
    /// <c>true</c> if the battle character has been attacked within the last second; otherwise, <c>false</c>.
    /// </returns>
    internal static bool IsAttacked(this IBattleChara obj)
    {
        if (obj == null) return false;
        if (Player.Object == null) return false;
        var now = DateTime.Now;
        foreach (var (id, time) in DataCenter.AttackedTargets)
        {
            if (id == obj.GameObjectId)
            {
                return now - time >= TimeSpan.FromSeconds(1);
            }
        }
        return false;
    }

    /// <summary>
    /// Determines if the player can see the specified game object.
    /// </summary>
    /// <param name="obj">The game object to check visibility for.</param>
    /// <returns>
    /// <c>true</c> if the player can see the specified game object; otherwise, <c>false</c>.
    /// </returns>
    internal static unsafe bool CanSee(this IGameObject obj)
    {
        if (obj == null) return false;
        if (Player.Object == null) return false;
        if (obj.Struct() == null) return false;

        const uint specificEnemyId = 3830; // Bioculture Node in Aetherial Chemical Research Facility
        if (obj.GameObjectId == specificEnemyId)
        {
            return true;
        }

        var point = Player.Object.Position + Vector3.UnitY * Player.GameObject->Height;
        var tarPt = obj.Position + Vector3.UnitY * obj.Struct()->Height;
        var direction = tarPt - point;

        int* unknown = stackalloc int[] { 0x4000, 0, 0x4000, 0 };

        RaycastHit hit;
        var ray = new Ray(point, direction);

        return !FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->BGCollisionModule
            ->RaycastMaterialFilter(&hit, &point, &direction, direction.Length(), 1, unknown);
    }

    /// <summary>
    /// Get the <paramref name="obj"/>'s current HP percentage.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static float GetHealthRatio(this IGameObject obj)
    {
        if (obj == null) return 0;
        if (Player.Object == null) return 0;
        if (obj is not IBattleChara b) return 0;

        if (DataCenter.RefinedHP.TryGetValue(b.GameObjectId, out var hp)) return hp;
        if (b.MaxHp == 0) return 0; // Avoid division by zero
        return (float)b.CurrentHp / b.MaxHp;
    }

    /// <summary>
    /// Determines the positional relationship of the player relative to the enemy.
    /// </summary>
    /// <param name="enemy">The enemy game object.</param>
    /// <returns>
    /// An <see cref="EnemyPositional"/> value indicating whether the player is in front, at the rear, or on the flank of the enemy.
    /// </returns>
    public static EnemyPositional FindEnemyPositional(this IGameObject enemy)
    {
        if (enemy == null) return EnemyPositional.None;
        if (Player.Object == null) return EnemyPositional.None;
        if (enemy is not IBattleChara b) return EnemyPositional.None;

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

        if (angle < frontAngle) return EnemyPositional.Front;
        else if (angle > rearAngle) return EnemyPositional.Rear;
        return EnemyPositional.Flank;
    }

    /// <summary>
    /// Gets the facing direction vector of the game object.
    /// </summary>
    /// <param name="obj">The game object.</param>
    /// <returns>
    /// A <see cref="Vector3"/> representing the facing direction of the game object.
    /// </returns>
    internal static Vector3 GetFaceVector(this IGameObject obj)
    {
        if (obj == null) return Vector3.Zero;
        if (Player.Object == null) return Vector3.Zero;
        if (obj is not IBattleChara b) return Vector3.Zero;

        float rotation = obj.Rotation;
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
        if (lengthProduct == 0) return 0;

        double dotProduct = Vector3.Dot(vec1, vec2);
        return Math.Acos(dotProduct / lengthProduct);
    }

    /// <summary>
    /// The distance from <paramref name="obj"/> to the player
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static float DistanceToPlayer(this IGameObject? obj)
    {
        if (obj == null) return float.MaxValue;
        if (Player.Object == null) return float.MaxValue;
        if (obj is not IBattleChara b) return float.MaxValue;

        var distance = Vector3.Distance(Player.Object.Position, obj.Position) - (Player.Object.HitboxRadius + obj.HitboxRadius);
        return distance;
    }

}
