using Dalamud.Game.ClientState.Objects.Enums;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;

namespace RotationSolver.Updaters;

internal static partial class TargetUpdater
{
    private static readonly ObjectListDelay<IBattleChara>
        _raisePartyTargets = new(() => Service.Config.RaiseDelay),
        _raiseAllTargets = new(() => Service.Config.RaiseDelay);

    private static DateTime _lastUpdateTimeToKill = DateTime.MinValue;
    private static readonly TimeSpan TimeToKillUpdateInterval = TimeSpan.FromSeconds(0.1);

    internal static void UpdateTargets()
    {
        DataCenter.AllTargets = GetAllTargets();
        DataCenter.FriendlyNPCMembers = GetFriendlyNPCs();
        DataCenter.AllianceMembers = GetAllianceMembers();
        DataCenter.PartyMembers = GetPartyMembers();
        DataCenter.DeathTarget = GetDeathTarget();
        DataCenter.DispelTarget = GetDispelTarget();
        DataCenter.AllHostileTargets = GetAllHostileTargets();
        DataCenter.ProvokeTarget = GetFirstHostileTarget(ObjectHelper.CanProvoke);
        DataCenter.InterruptTarget = GetFirstHostileTarget(ObjectHelper.CanInterrupt);
        UpdateTimeToKill();
    }

    private static List<IBattleChara> GetAllTargets()
    {
        var allTargets = new List<IBattleChara>();
        foreach (var obj in Svc.Objects)
        {
            if (obj is IBattleChara battleChara)
            {
                if (!battleChara.IsDummy() || !Service.Config.DisableTargetDummys)
                {
                    allTargets.Add(battleChara);
                }
            }
        }
        return allTargets;
    }

    private static unsafe List<IBattleChara> GetPartyMembers()
    {
        var partyMembers = new List<IBattleChara>();
        try
        {
            if (DataCenter.PartyMembers != null)
            {
                foreach (var member in DataCenter.AllTargets)
                {
                    if (member == null) continue;

                    if (ObjectHelper.IsParty(member) && member.IsParty() && member.Character() != null)
                    {
                        var character = member.Character();
                        if (character != null && character->CharacterData.OnlineStatus != 15 &&
                            character->CharacterData.OnlineStatus != 5 && member.IsTargetable)
                        {
                            partyMembers.Add(member);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"Error in GetPartyMembers: {ex.Message}");
        }
        return partyMembers;
    }

    private static unsafe List<IBattleChara> GetAllianceMembers()
    {
        var allianceMembers = new List<IBattleChara>();
        try
        {
            if (DataCenter.AllianceMembers != null)
            {
                foreach (var target in DataCenter.AllTargets)
                {
                    if (ObjectHelper.IsAllianceMember(target) && !target.IsParty() && target.Character() != null &&
                        target.Character()->CharacterData.OnlineStatus != 15 &&
                        target.Character()->CharacterData.OnlineStatus != 5 && target.IsTargetable)
                    {
                        allianceMembers.Add(target);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"Error in GetAllianceMembers: {ex.Message}");
        }
        return allianceMembers;
    }

    private static List<IBattleChara> GetFriendlyNPCs()
    {
        var friendlyNpcs = new List<IBattleChara>();
        if (!Service.Config.FriendlyBattleNpcHeal && !Service.Config.FriendlyPartyNpcHealRaise3)
        {
            return friendlyNpcs;
        }

        try
        {
            if (Svc.Objects != null)
            {
                foreach (var obj in Svc.Objects)
                {
                    if (obj != null && obj.ObjectKind == ObjectKind.BattleNpc)
                    {
                        try
                        {
                            if (obj.GetNameplateKind() == NameplateKind.FriendlyBattleNPC ||
                                obj.GetBattleNPCSubKind() == BattleNpcSubKind.NpcPartyMember)
                            {
                                if (obj is IBattleChara battleChara)
                                {
                                    friendlyNpcs.Add(battleChara);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Svc.Log.Error($"Error filtering object in GetFriendlyNPCs: {ex.Message}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"Error in GetFriendlyNPCs: {ex.Message}");
        }
        return friendlyNpcs;
    }

    private static List<IBattleChara> GetAllHostileTargets()
    {
        var hostileTargets = new List<IBattleChara>();
        try
        {
            foreach (var target in DataCenter.AllTargets)
            {
                if (target == null) continue;
                if (!target.IsEnemy() || !target.IsTargetable) continue;
                if (target.StatusList != null && target.StatusList.Any(StatusHelper.IsInvincible) &&
                    (DataCenter.IsPvP && !Service.Config.IgnorePvPInvincibility || !DataCenter.IsPvP)) continue;

                hostileTargets.Add(target);
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"Error in GetAllHostileTargets: {ex.Message}");
        }
        return hostileTargets;
    }

    private static IBattleChara? GetFirstHostileTarget(Func<IBattleChara, bool> predicate)
    {
        foreach (var target in DataCenter.AllHostileTargets)
        {
            try
            {
                if (predicate(target))
                {
                    return target;
                }
            }
            catch (Exception ex)
            {
                Svc.Log.Error($"Error in GetFirstHostileTarget: {ex.Message}");
            }
        }
        return null;
    }

    private static IBattleChara? GetDeathTarget()
    {
        if (Player.Job == Job.WHM || Player.Job == Job.SCH || Player.Job == Job.AST || Player.Job == Job.SGE ||
            Player.Job == Job.SMN || Player.Job == Job.RDM)
        {
            try
            {
                var deathParty = DataCenter.PartyMembers?.GetDeath().ToList() ?? [];
                var deathAll = DataCenter.AllTargets?.GetDeath().ToList() ?? [];
                var deathNPC = DataCenter.FriendlyNPCMembers?.GetDeath().ToList() ?? [];
                var deathAllianceMembers = DataCenter.AllianceMembers?.GetDeath().ToList() ?? [];
                var deathAllianceHealers = new List<IBattleChara>(deathParty);
                var deathAllianceSupports = new List<IBattleChara>(deathParty);

                if (DataCenter.AllianceMembers != null)
                {
                    foreach (var member in DataCenter.AllianceMembers)
                    {
                        if (member.IsJobCategory(JobRole.Healer))
                        {
                            deathAllianceHealers.Add(member);
                        }
                        if (member.IsJobCategory(JobRole.Healer) || member.IsJobCategory(JobRole.Tank))
                        {
                            deathAllianceSupports.Add(member);
                        }
                    }
                }

                var raisePartyAndAllianceSupports = new List<IBattleChara>(deathParty);
                raisePartyAndAllianceSupports.AddRange(deathAllianceSupports);

                var raisePartyAndAllianceHealers = new List<IBattleChara>(deathParty);
                raisePartyAndAllianceHealers.AddRange(deathAllianceHealers);

                var raisetype = Service.Config.RaiseType;

                var validRaiseTargets = new List<IBattleChara>();

                if (raisetype == RaiseType.PartyOnly)
                {
                    validRaiseTargets.AddRange(deathParty);
                }
                else if (raisetype == RaiseType.PartyAndAllianceSupports)
                {
                    validRaiseTargets.AddRange(raisePartyAndAllianceSupports);
                }
                else if (raisetype == RaiseType.PartyAndAllianceHealers)
                {
                    validRaiseTargets.AddRange(raisePartyAndAllianceHealers);
                }
                else if (raisetype == RaiseType.All)
                {
                    validRaiseTargets.AddRange(deathAll);
                }

                if (Service.Config.FriendlyPartyNpcHealRaise3 || Service.Config.FocusTargetIsParty)
                {
                    validRaiseTargets.AddRange(deathNPC);
                }

                foreach (RaiseType type in Enum.GetValues(typeof(RaiseType)))
                {
                    var deathTarget = GetPriorityDeathTarget(validRaiseTargets, type);
                    if (deathTarget != null) return deathTarget;
                }
            }
            catch (Exception ex)
            {
                Svc.Log.Error($"Error in GetDeathTarget: {ex.Message}");
            }
        }
        return null;
    }


    private static IBattleChara? GetPriorityDeathTarget(List<IBattleChara> validRaiseTargets, RaiseType raiseType = RaiseType.PartyOnly)
    {
        if (validRaiseTargets.Count == 0) return null;

        var deathTanks = new List<IBattleChara>();
        var deathHealers = new List<IBattleChara>();
        var deathOffHealers = new List<IBattleChara>();
        var deathOthers = new List<IBattleChara>();

        foreach (var chara in validRaiseTargets)
        {
            if (chara.IsJobCategory(JobRole.Tank))
            {
                deathTanks.Add(chara);
            }
            else if (chara.IsJobCategory(JobRole.Healer))
            {
                deathHealers.Add(chara);
            }
            else if (Service.Config.OffRaiserRaise && chara.IsJobs(Job.SMN, Job.RDM))
            {
                deathOffHealers.Add(chara);
            }
            else
            {
                deathOthers.Add(chara);
            }
        }

        if (raiseType == RaiseType.PartyAndAllianceHealers && deathHealers.Count > 0)
        {
            return deathHealers[0];
        }

        if (Service.Config.H2)
        {
            deathOffHealers.Reverse();
            deathOthers.Reverse();
        }

        if (deathTanks.Count > 1) return deathTanks[0];
        if (deathHealers.Count > 0) return deathHealers[0];
        if (deathTanks.Count > 0) return deathTanks[0];
        if (Service.Config.OffRaiserRaise && deathOffHealers.Count > 0) return deathOffHealers[0];

        return deathOthers.Count > 0 ? deathOthers[0] : null;
    }

    private static IBattleChara? GetDispelTarget()
    {
        if (Player.Job == Job.WHM || Player.Job == Job.SCH || Player.Job == Job.AST || Player.Job == Job.SGE ||
            Player.Job == Job.BRD)
        {
            var weakenPeople = new List<IBattleChara>();
            var weakenNPC = new List<IBattleChara>();
            var dyingPeople = new List<IBattleChara>();

            AddDispelTargets(DataCenter.PartyMembers, weakenPeople);
            AddDispelTargets(DataCenter.FriendlyNPCMembers, weakenNPC);

            foreach (var person in weakenPeople)
            {
                if (person.StatusList != null && person.StatusList.Any(status => status != null && status.IsDangerous()))
                {
                    dyingPeople.Add(person);
                }
            }

            return GetClosestTarget(dyingPeople) ?? GetClosestTarget(weakenPeople) ?? GetClosestTarget(weakenNPC);
        }
        return null;
    }

    private static void AddDispelTargets(List<IBattleChara>? members, List<IBattleChara> targetList)
    {
        if (members == null) return;

        foreach (var member in members)
        {
            if (member.StatusList != null && member.StatusList.Any(status => status != null && status.CanDispel()))
            {
                targetList.Add(member);
            }
        }
    }

    private static IBattleChara? GetClosestTarget(List<IBattleChara> targets)
    {
        IBattleChara? closestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (var target in targets)
        {
            var distance = ObjectHelper.DistanceToPlayer(target);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = target;
            }
        }

        return closestTarget;
    }

    private static void UpdateTimeToKill()
    {
        var now = DateTime.Now;
        if (now - _lastUpdateTimeToKill < TimeToKillUpdateInterval) return;
        _lastUpdateTimeToKill = now;

        if (DataCenter.RecordedHP.Count >= DataCenter.HP_RECORD_TIME)
        {
            DataCenter.RecordedHP.Dequeue();
        }

        var currentHPs = new SortedList<ulong, float>();
        foreach (var target in DataCenter.AllTargets)
        {
            if (target.CurrentHp != 0)
            {
                currentHPs[target.GameObjectId] = target.GetHealthRatio();
            }
        }

        DataCenter.RecordedHP.Enqueue((now, currentHPs));
    }
}