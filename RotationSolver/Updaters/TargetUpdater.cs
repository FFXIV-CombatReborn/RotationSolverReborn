using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Logging;

namespace RotationSolver.Updaters;

internal static partial class TargetUpdater
{
    private static readonly ObjectListDelay<IBattleChara>
        _raisePartyTargets = new(() => Service.Config.RaiseDelay2),
        _raiseAllTargets = new(() => Service.Config.RaiseDelay2),
        _dispelPartyTargets = new(() => Service.Config.EsunaDelay);

    private static DateTime _lastUpdateTimeToKill = DateTime.MinValue;
    private static readonly TimeSpan TimeToKillUpdateInterval = TimeSpan.FromSeconds(1);

    internal static void UpdateTargets()
    {
        //PluginLog.Debug("Updating targets");
        DataCenter.TargetsByRange.Clear();
        DataCenter.AllTargets = GetAllTargets();
        if (DataCenter.AllTargets != null)
        {
            DataCenter.PartyMembers = GetPartyMembers();
            DataCenter.AllianceMembers = GetAllianceMembers();
            DataCenter.AllHostileTargets = GetAllHostileTargets();
            DataCenter.DeathTarget = GetDeathTarget();
            DataCenter.DispelTarget = GetDispelTarget();
            DataCenter.ProvokeTarget = (DataCenter.Role == JobRole.Tank || Player.Object.HasStatus(true, StatusID.VariantUltimatumSet)) ? GetFirstHostileTarget(ObjectHelper.CanProvoke) : null; // Calculating this per frame rather than on-demand is actually a fair amount worse
            DataCenter.InterruptTarget = GetFirstHostileTarget(ObjectHelper.CanInterrupt); // Tanks, Melee, RDM, and various phantom and duty actions can interrupt so just deal with it
        }
        UpdateTimeToKill();
    }

    private static List<IBattleChara> GetAllTargets()
    {
        List<IBattleChara> allTargets = [];
        bool skipDummyCheck = !Service.Config.DisableTargetDummys;
        foreach (var obj in Svc.Objects)
        {
            if (obj is IBattleChara battleChara)
            {
                if ((skipDummyCheck || !battleChara.IsDummy()) && battleChara.StatusList != null && battleChara.IsTargetable && !battleChara.IsPet())
                {
                    allTargets.Add(battleChara);
                }
            }
        }
        return allTargets;
    }

    private static unsafe List<IBattleChara> GetPartyMembers()
    {
        return GetMembers(DataCenter.AllTargets, isParty: true);
    }

    private static unsafe List<IBattleChara> GetAllianceMembers()
    {
        RaiseType raisetype = Service.Config.RaiseType;

        if (raisetype == RaiseType.PartyOnly)
        {
            return [];
        }

        if (raisetype == RaiseType.AllOutOfDuty)
        {
            return GetMembers(DataCenter.AllTargets, isParty: false, isAlliance: false, IsOutDuty: true);
        }

        return GetMembers(DataCenter.AllTargets, isParty: false, isAlliance: true, IsOutDuty: false);
    }

    private static unsafe List<IBattleChara> GetMembers(List<IBattleChara> source, bool isParty, bool isAlliance = false, bool IsOutDuty = false)
    {
        List<IBattleChara> members = [];
        if (source == null) return members;

        foreach (IBattleChara member in source)
        {
            try
            {
                if (member.IsPet()) continue;
                if (isParty && !member.IsParty()) continue;
                if (isAlliance && (!ObjectHelper.IsAllianceMember(member) || member.IsParty())) continue;
                if (IsOutDuty && (!ObjectHelper.IsOtherPlayerOutOfDuty(member) || member.IsParty())) continue;

                FFXIVClientStructs.FFXIV.Client.Game.Character.Character* character = member.Character();
                if (character == null) continue;

                members.Add(member);
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error in GetMembers: {ex.Message}");
            }
        }
        return members;
    }

    private static List<IBattleChara> GetAllHostileTargets()
    {
        List<IBattleChara> hostileTargets = [];
        var allTargets = DataCenter.AllTargets;
        if (allTargets == null) return hostileTargets;

        foreach (IBattleChara target in allTargets)
        {
            if (!target.IsEnemy() || !target.IsTargetable || !target.CanSee() || target.DistanceToPlayer() >= 48)
                continue;

            bool hasInvincible = false;
            var statusList = target.StatusList;
            if (statusList != null)
            {
                var statusCount = statusList.Length;
                for (int i = 0; i < statusCount; i++)
                {
                    var status = statusList[i];
                    if (status != null)
                    {
                        if (status.StatusId != 0 && StatusHelper.IsInvincible(status))
                        {
                            hasInvincible = true;
                            break;
                        }
                    }
                }
            }
            if (hasInvincible &&
                ((DataCenter.IsPvP && !Service.Config.IgnorePvPInvincibility) || !DataCenter.IsPvP))
            {
                continue;
            }

            hostileTargets.Add(target);
        }
        return hostileTargets;
    }

    private static IBattleChara? GetFirstHostileTarget(Func<IBattleChara, bool> predicate)
    {
        var hostileTargets = DataCenter.AllHostileTargets;
        if (hostileTargets == null) return null;

        foreach (IBattleChara target in hostileTargets)
        {
            try
            {
                if (predicate(target))
                    return target;
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error in GetFirstHostileTarget: {ex.Message}");
            }
        }
        return null;
    }

    private static IBattleChara? GetDeathTarget()
    {
        if (DataCenter.CanRaise())
        {
            try
            {
                RaiseType raisetype = Service.Config.RaiseType;

                // Use HashSet for fast lookup
                var deathParty = new HashSet<IBattleChara>();
                if (DataCenter.PartyMembers != null)
                {
                    foreach (var target in DataCenter.PartyMembers.GetDeath())
                    {
                        deathParty.Add(target);
                    }
                }

                var validRaiseTargets = new List<IBattleChara>(deathParty);

                if (raisetype == RaiseType.PartyAndAllianceSupports || raisetype == RaiseType.PartyAndAllianceHealers)
                {
                    if (DataCenter.AllianceMembers != null)
                    {
                        foreach (var member in DataCenter.AllianceMembers.GetDeath())
                        {
                            if (!deathParty.Contains(member))
                            {
                                if (raisetype == RaiseType.PartyAndAllianceHealers && member.IsJobCategory(JobRole.Healer))
                                    validRaiseTargets.Add(member);
                                else if (raisetype == RaiseType.PartyAndAllianceSupports && (member.IsJobCategory(JobRole.Healer) || member.IsJobCategory(JobRole.Tank)))
                                    validRaiseTargets.Add(member);
                            }
                        }
                    }
                }
                else if (raisetype == RaiseType.All || raisetype == RaiseType.AllOutOfDuty)
                {
                    if (DataCenter.AllianceMembers != null)
                    {
                        foreach (var target in DataCenter.AllianceMembers.GetDeath())
                        {
                            if (!deathParty.Contains(target))
                            {
                                validRaiseTargets.Add(target);
                            }
                        }
                    }
                }

                // Apply raise delay
                if (raisetype == RaiseType.PartyOnly)
                {
                    _raisePartyTargets.Delay(validRaiseTargets);
                    validRaiseTargets = [.. _raisePartyTargets];
                }
                else
                {
                    _raiseAllTargets.Delay(validRaiseTargets);
                    validRaiseTargets = [.. _raiseAllTargets];
                }

                // Only use the current RaiseType
                return GetPriorityDeathTarget(validRaiseTargets, raisetype);
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error in GetDeathTarget: {ex.Message}");
            }
        }
        return null;
    }

    private static IBattleChara? GetPriorityDeathTarget(List<IBattleChara> validRaiseTargets, RaiseType raiseType = RaiseType.PartyOnly)
    {
        if (validRaiseTargets.Count == 0)
        {
            return null;
        }

        List<IBattleChara> deathTanks = [];
        List<IBattleChara> deathHealers = [];
        List<IBattleChara> deathOffHealers = [];
        List<IBattleChara> deathOthers = [];

        foreach (IBattleChara chara in validRaiseTargets)
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

        if (deathTanks.Count > 1)
        {
            return deathTanks[0];
        }

        return deathHealers.Count > 0
            ? deathHealers[0]
            : deathTanks.Count > 0
            ? deathTanks[0]
            : Service.Config.OffRaiserRaise && deathOffHealers.Count > 0
            ? deathOffHealers[0]
            : deathOthers.Count > 0 ? deathOthers[0] : null;
    }

    private static IBattleChara? GetDispelTarget()
    {
        if (Player.Job is Job.WHM or Job.SCH or Job.AST or Job.SGE or Job.BRD or Job.CNJ)
        {
            List<IBattleChara> weakenPeople = [];
            List<IBattleChara> dyingPeople = [];

            AddDispelTargets(DataCenter.PartyMembers, weakenPeople);

            // Apply dispel delay
            _dispelPartyTargets.Delay(weakenPeople);
            var delayedWeakenPeople = new List<IBattleChara>();
            foreach (var person in _dispelPartyTargets)
            {
                delayedWeakenPeople.Add(person);
            }

            var CanDispelNonDangerous = !DataCenter.MergedStatus.HasFlag(AutoStatus.HealAreaAbility)
                    && !DataCenter.MergedStatus.HasFlag(AutoStatus.HealAreaSpell)
                    && !DataCenter.MergedStatus.HasFlag(AutoStatus.HealSingleAbility)
                    && !DataCenter.MergedStatus.HasFlag(AutoStatus.HealSingleSpell)
                    && !DataCenter.MergedStatus.HasFlag(AutoStatus.DefenseArea)
                    && !DataCenter.MergedStatus.HasFlag(AutoStatus.DefenseSingle);

            foreach (IBattleChara person in delayedWeakenPeople)
            {
                bool hasDangerous = false;
                if (person.StatusList != null)
                {
                    for (int i = 0; i < person.StatusList.Length; i++)
                    {
                        Dalamud.Game.ClientState.Statuses.Status? status = person.StatusList[i];
                        if (status != null && status.IsDangerous())
                        {
                            hasDangerous = true;
                            break;
                        }
                    }
                }
                if (hasDangerous)
                {
                    dyingPeople.Add(person);
                }
            }

            // Allow non-dangerous dispels when either we're in a safe context or explicitly configured to do so.
            bool allowNonDangerous = CanDispelNonDangerous
                                     || !DataCenter.HasHostilesInRange
                                     || Service.Config.DispelAll
                                     || DataCenter.IsPvP;

            IBattleChara? dangerousTarget = GetClosestTarget(dyingPeople);
            if (!allowNonDangerous)
            {
                return dangerousTarget;
            }

            return dangerousTarget ?? GetClosestTarget(delayedWeakenPeople);
        }
        return null;
    }

    private static void AddDispelTargets(List<IBattleChara>? members, List<IBattleChara> targetList)
    {
        if (members == null)
        {
            return;
        }

        foreach (IBattleChara member in members)
        {
            try
            {
                if (member.StatusList != null)
                {
                    for (int i = 0; i < member.StatusList.Length; i++)
                    {
                        Dalamud.Game.ClientState.Statuses.Status? status = member.StatusList[i];
                        if (status != null && status.CanDispel())
                        {
                            targetList.Add(member);
                            break; // Add only once per member if any status can be dispelled
                        }
                    }
                }
            }
            catch (NullReferenceException ex)
            {
                PluginLog.Error($"NullReferenceException in AddDispelTargets for member {member?.ToString()}: {ex.Message}");
            }
        }
    }

    private static IBattleChara? GetClosestTarget(List<IBattleChara> targets)
    {
        IBattleChara? closestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (IBattleChara target in targets)
        {
            float distance = ObjectHelper.DistanceToPlayer(target);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = target;
            }
        }

        return closestTarget;
    }

    // Recording new entries at 1/second and dequeuing old values to keep only the last DataCenter.HP_RECORD_TIME worth of combat time
    // Has performance implications for keeping too much data for too many targets as they're also all evaluated multiple times a frame for expected TTK
    private static void UpdateTimeToKill()
    {
        DateTime now = DateTime.Now;
        if (now - _lastUpdateTimeToKill < TimeToKillUpdateInterval)
        {
            return;
        }

        _lastUpdateTimeToKill = now;

        var hostiles = DataCenter.AllHostileTargets;
        if (hostiles == null || hostiles.Count == 0)
        {
            return;
        }

        if (DataCenter.RecordedHP.Count >= DataCenter.HP_RECORD_TIME)
        {
            _ = DataCenter.RecordedHP.Dequeue();
        }

        Dictionary<ulong, float> currentHPs = new(hostiles.Count);
        for (int i = 0; i < hostiles.Count; i++)
        {
            var target = hostiles[i];
            if (target != null && target.CurrentHp != 0)
            {
                currentHPs[target.GameObjectId] = target.GetHealthRatio();
            }
        }

        DataCenter.RecordedHP.Enqueue((now, currentHPs));
    }
}