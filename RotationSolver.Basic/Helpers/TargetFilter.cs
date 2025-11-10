using ECommons.ExcelServices;
using Lumina.Excel.Sheets;

namespace RotationSolver.Basic.Helpers;

/// <summary>
/// The filter for target.
/// </summary>
public static class TargetFilter
{
    private static Dictionary<JobRole, HashSet<byte>>? _roleJobs;
    private static readonly Lock _roleJobsLock = new();

    private static Dictionary<JobRole, HashSet<byte>> GetRoleMap()
    {
        var map = _roleJobs;
        if (map != null) return map;
        lock (_roleJobsLock)
        {
            if (_roleJobs != null) return _roleJobs;
            var sheet = Service.GetSheet<ClassJob>();
            var dict = new Dictionary<JobRole, HashSet<byte>>();
            if (sheet != null)
            {
                foreach (var job in sheet)
                {
                    var role = job.GetJobRole();
                    if (!dict.TryGetValue(role, out var set))
                    {
                        set = new HashSet<byte>();
                        dict[role] = set;
                    }
                    set.Add((byte)job.RowId);
                }
            }
            _roleJobs = dict;
            return _roleJobs;
        }
    }

    #region Find one target
    /// <summary>
    /// Get the dead ones in the list.
    /// </summary>
    /// <param name="charas">The list of characters.</param>
    /// <returns>The dead characters.</returns>
    public static IEnumerable<IBattleChara> GetDeath(this IEnumerable<IBattleChara> charas)
    {
        if (charas == null) yield break;

        foreach (IBattleChara item in charas)
        {
            if (item == null)
                continue;
            if (!item.IsDead)
                continue;
            if (item.CurrentHp != 0 || !item.IsTargetable || item.IsTargetMoving() || item.IsEnemy())
                continue;
            RaiseType raisetype = Service.Config.RaiseType;

            if (raisetype == RaiseType.AllOutOfDuty)
            {
                if (!item.IsParty() && !item.IsOtherPlayerOutOfDuty())
                    continue;
            }
            if (raisetype != RaiseType.AllOutOfDuty)
            {
                if (!item.IsParty() && !item.IsAllianceMember())
                    continue;
            }
            if (item.DistanceToPlayer() > 30)
                continue;
            if (!item.CanSee())
                continue;
            if (item.HasStatus(false, StatusID.Raise))
                continue;
            if (item.HasStatus(false, StatusID.ResurrectionDenied))
                continue;
            if (!Service.Config.RaiseBrinkOfDeath && item.HasStatus(false, StatusID.BrinkOfDeath))
                continue;
            if (!item.CanBeRaised())
                continue;
            yield return item;
        }
    }

    /// <summary>
    /// Get the specific roles members.
    /// </summary>
    /// <param name="objects">The list of objects.</param>
    /// <param name="roles">The roles to filter by.</param>
    /// <returns>The objects that match the roles.</returns>
public static IEnumerable<IBattleChara> GetJobCategory(this IEnumerable<IBattleChara> objects, params JobRole[] roles)
    {
        if (objects == null || roles == null || roles.Length == 0)
        {
            return [];
        }

        var map = GetRoleMap();
        HashSet<byte> validJobs = [];
        foreach (var role in roles)
        {
            if (map.TryGetValue(role, out var set) && set != null)
            {
                foreach (var j in set) validJobs.Add(j);
            }
        }

        List<IBattleChara> result = [];
        foreach (IBattleChara obj in objects)
        {
            if (obj != null && obj.IsJobs(validJobs))
            {
                result.Add(obj);
            }
        }

        return result;
    }

    /// <summary>
    /// Is the target the role.
    /// </summary>
    /// <param name="battleChara">The game object.</param>
    /// <param name="role">The role to check.</param>
    /// <returns>True if the object is of the specified role, otherwise false.</returns>
public static bool IsJobCategory(this IBattleChara battleChara, JobRole role)
    {
        if (battleChara == null)
        {
            return false;
        }

        var map = GetRoleMap();
        return map.TryGetValue(role, out var set) && battleChara.IsJobs(set);
    }

    /// <summary>
    /// Is the target in the jobs.
    /// </summary>
    /// <param name="battleChara">The game object.</param>
    /// <param name="validJobs">The valid jobs.</param>
    /// <returns>True if the object is in the valid jobs, otherwise false.</returns>
    public static bool IsJobs(this IBattleChara battleChara, params Job[] validJobs)
    {
        if (battleChara == null || validJobs == null || validJobs.Length == 0)
        {
            return false;
        }

        HashSet<byte> validJobSet = [];
        foreach (Job job in validJobs)
        {
            _ = validJobSet.Add((byte)(uint)job);
        }

        return battleChara.IsJobs(validJobSet);
    }

    private static bool IsJobs(this IGameObject battleChara, HashSet<byte> validJobs)
    {
        return battleChara is IBattleChara b && validJobs != null && validJobs.Contains((byte)b.ClassJob.Value.RowId);
    }
    #endregion

    /// <summary>
    /// Get the <paramref name="objects"/> in <paramref name="radius"/>.
    /// </summary>
    /// <typeparam name="T">The type of objects.</typeparam>
    /// <param name="objects">The list of objects.</param>
    /// <param name="radius">The radius to filter by.</param>
    /// <returns>The objects within the radius.</returns>
    public static IEnumerable<T> GetObjectInRadius<T>(this IEnumerable<T> objects, float radius) where T : IBattleChara
    {
        if (objects == null) yield break;

        foreach (T obj in objects)
        {
            if (obj.DistanceToPlayer() < radius)
                yield return obj;
        }
    }
}