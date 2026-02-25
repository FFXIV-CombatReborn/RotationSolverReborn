using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using Lumina.Excel.Sheets;
using RotationSolver.Basic.Rotations.Duties;
using RotationSolver.Helpers;


namespace RotationSolver.Updaters;

internal static class RotationUpdater
{
    internal record CustomRotationGroup(Job JobId, Job[] ClassJobIds, Type[] Rotations);
    internal static SortedList<JobRole, CustomRotationGroup[]> CustomRotationsDict { get; private set; } = [];

    internal static Dictionary<Job, Dictionary<CombatType, List<ICustomRotation>>> CustomRotationsLookup { get; private set; } = [];

    internal static CustomRotationGroup[] CustomRotations { get; set; } = [];
    internal static SortedList<uint, Type[]> DutyRotations { get; set; } = [];

    public static IAction[] CurrentRotationActions { get; private set; } = [];

    private static string _curDutyRotationName = string.Empty;

    /// <summary>
    /// Loads custom rotation groups from the current assembly
    /// </summary>
    public static void LoadBuiltInRotations()
    {
        List<Assembly> assemblies = [typeof(RotationUpdater).Assembly];

        DutyRotations = LoadDutyRotationGroup(assemblies);
        CustomRotations = LoadCustomRotationGroup(assemblies);

        Dictionary<JobRole, List<CustomRotationGroup>> customRotationsGroupedByJobRole = [];
        foreach (CustomRotationGroup customRotationGroup in CustomRotations)
        {
            Job job = customRotationGroup.Rotations[0].GetType().GetCustomAttribute<JobsAttribute>()?.Jobs[0] ?? Job.ADV;
            JobRole jobRole = Svc.Data.GetExcelSheet<ClassJob>()!.GetRow((uint)job)!.GetJobRole();
            if (!customRotationsGroupedByJobRole.TryGetValue(jobRole, out List<CustomRotationGroup>? value))
            {
                value = [];
                customRotationsGroupedByJobRole[jobRole] = value;
            }
            value.Add(customRotationGroup);
        }

        CustomRotationsDict = [];
        foreach (KeyValuePair<JobRole, List<CustomRotationGroup>> kvp in customRotationsGroupedByJobRole)
        {
            List<CustomRotationGroup> customRotationGroups = kvp.Value;
            // Sort by JobId
            for (int i = 0; i < customRotationGroups.Count - 1; i++)
            {
                for (int j = i + 1; j < customRotationGroups.Count; j++)
                {
                    if (customRotationGroups[i].JobId > customRotationGroups[j].JobId)
                    {
                        (customRotationGroups[j], customRotationGroups[i]) = (customRotationGroups[i], customRotationGroups[j]);
                    }
                }
            }
            CustomRotationsDict[kvp.Key] = [.. customRotationGroups];
        }
    }

    private static SortedList<uint, Type[]> LoadDutyRotationGroup(List<Assembly> assemblies)
    {
        List<Type> rotationList = [];
        foreach (Assembly assembly in assemblies)
        {
            foreach (Type type in TryGetTypes(assembly))
            {
                if (type.IsAssignableTo(typeof(DutyRotation))
                    && !type.IsAbstract && type.GetConstructor([]) != null)
                {
                    rotationList.Add(type);
                }
            }
        }

        Dictionary<uint, List<Type>> result = [];
        foreach (Type type in rotationList)
        {
            uint[] territories = type.GetCustomAttribute<DutyTerritoryAttribute>()?.TerritoryIds ?? [];

            foreach (uint id in territories)
            {
                if (result.TryGetValue(id, out List<Type>? list))
                {
                    list.Add(type);
                }
                else
                {
                    result[id] = [type];
                }
            }
        }

        SortedList<uint, Type[]> sorted = [];
        foreach (var pair in result)
        {
            sorted.Add(pair.Key, [.. pair.Value]);
        }
        return sorted;
    }

    private static CustomRotationGroup[] LoadCustomRotationGroup(List<Assembly> assemblies)
    {
        List<Type> rotationList = [];

        foreach (Assembly assembly in assemblies)
        {
            foreach (Type type in TryGetTypes(assembly))
            {
                bool implementsICustomRotation = false;
                foreach (var iface in type.GetInterfaces())
                {
                    if (iface == typeof(ICustomRotation))
                    {
                        implementsICustomRotation = true;
                        break;
                    }
                }

                if (implementsICustomRotation
                    && !type.IsAbstract && !type.IsInterface && type.GetConstructor(Type.EmptyTypes) != null)
                {
                    rotationList.Add(type);
                }
            }
        }

        Dictionary<Job, List<Type>> rotationGroups = [];
        foreach (Type rotation in rotationList)
        {
            JobsAttribute? attr = rotation.GetCustomAttribute<JobsAttribute>();
            if (attr == null)
            {
                continue;
            }

            Job jobId = attr.Jobs[0];
            if (!rotationGroups.TryGetValue(jobId, out List<Type>? value))
            {
                value = [];
                rotationGroups.Add(jobId, value);
            }

            value.Add(rotation);
        }

        List<CustomRotationGroup> result = [];
        foreach (KeyValuePair<Job, List<Type>> kvp in rotationGroups)
        {
            Job jobId = kvp.Key;
            Type[] rotations = [.. kvp.Value];

            result.Add(new CustomRotationGroup(jobId, rotations[0].GetCustomAttribute<JobsAttribute>()!.Jobs,
                rotations));
        }

        CustomRotationsLookup = []; // CustomRotations aren't disposed or we'd want to loop through and dispose them
        return [.. result];
    }

    public static Type[] TryGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (Exception ex)
        {
            PluginLog.Warning($"Failed to load the types from {assembly.FullName}: {ex.Message}");
            return [];
        }
    }

    public static IEnumerable<IGrouping<string, IAction>>? AllGroupedActions
        => GroupActions([
            .. DataCenter.CurrentRotation?.AllActions ?? [],
            .. DataCenter.CurrentDutyRotation?.AllActions ?? []]);

    public static IEnumerable<IGrouping<string, IAction>>? GroupActions(IEnumerable<IAction> actions)
    {
        if (actions == null)
        {
            return null;
        }

        Dictionary<string, List<IAction>> groups = [];
        foreach (IAction a in actions)
        {
            string key = string.Empty;
            if (a is IBaseAction act)
            {
				// Filter out special actions, usually related to duty specifc mechanics but not duty actions
				if (act.Info.IsSpecialAction)
                {
                    continue;
                }
                // Filter out Mount actions
                else if (act.Info.IsMountAction)
                {
                    continue;
                }
                // Filter out anything but sprint for now
                else if (act.Info.IsSystemAction && act.AdjustedID != 3)
                {
                    continue;
                }
                else if (!act.Info.IsOnSlot && act.ID != 14415 && act.ID != 14414)
                {
                    key = string.Empty;
                }
                else if (act.Info.IsSystemAction)
                {
                    key = "System Action";
                }
                else if (act.Action.IsRoleAction)
                {
                    key = "Role Action";
                }
                else if (act.Info.IsPvPLimitBreak && DataCenter.IsPvP)
                {
                    key = "PvP Limit Break";
                }
                else if (act.Info.IsLimitBreak && !DataCenter.IsPvP)
                {
                    key = "Limit Break";
                }
                else if (act.Info.IsDutyAction)
                {
                    key = "Duty Action";
                }
                else
                {
                    key = act.Info.IsRealGCD ? "GCD" : "oGCD";
                    if (act.Setting.IsFriendly)
                    {
                        key += "-Friendly";
                    }
                    else
                    {
                        key += "-Attack";
                    }
                }
            }
            else if (a is IBaseItem && !DataCenter.IsPvP)
            {
                key = "Item";
            }

            // Always add to groups since we now have meaningful keys for all cases
            if (!groups.TryGetValue(key, out List<IAction>? list))
            {
                list = [];
                groups[key] = list;
            }
            list.Add(a);
        }

        // Sort groups by key
        List<string> sortedKeys = [.. groups.Keys];
        sortedKeys.Sort(StringComparer.Ordinal);

        List<IGrouping<string, IAction>> result = [];
        foreach (string key in sortedKeys)
        {
            result.Add(new SimpleGrouping<string, IAction>(key, groups[key]));
        }
        return result;
    }

    public static ICustomRotation[] GetRotations(Job playerJob, CombatType combatType)
    {
        if (Player.Object == null || CustomRotations.Length < 22) // If we haven't loaded rotations, we don't have anything to return
        {
            return [];
        }

        if (!CustomRotationsLookup.TryGetValue(playerJob, out _))
        {
            InitReferenceDict(playerJob);
        }

        if (CustomRotationsLookup.TryGetValue(playerJob, out Dictionary<CombatType, List<ICustomRotation>>? validCustomRotations))
        {
            if (validCustomRotations.Count == 0)
            {
                PluginLog.Warning($"No valid rotations found for {playerJob}");
                return [];
            }

            if (validCustomRotations.TryGetValue(combatType, out List<ICustomRotation>? validCustomRotationsList))
            {
                return [.. validCustomRotationsList];
            }
        }

        return [];
    }

    private class SimpleGrouping<TKey, TElement>(TKey key, IEnumerable<TElement> elements) : IGrouping<TKey, TElement>
    {
        private readonly IEnumerable<TElement> _elements = elements;

        public TKey Key { get; } = key;
        public IEnumerator<TElement> GetEnumerator()
        {
            return _elements.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _elements.GetEnumerator();
        }
    }

    public static void UpdateRotation()
    {
        UpdateCustomRotation();
        UpdateDutyRotation();
    }

    private static void UpdateDutyRotation()
    {
        if (!DutyRotations.TryGetValue(Svc.ClientState.TerritoryType, out Type[]? rotations))
        {
            // Unload the current duty rotation if leaving a duty
            if (DataCenter.CurrentDutyRotation != null)
            {
                DataCenter.CurrentDutyRotation.Dispose();
                DataCenter.CurrentDutyRotation = null;
                _curDutyRotationName = string.Empty;
            }
            return;
        }

        _ = Service.Config.DutyRotationChoice.TryGetValue(Svc.ClientState.TerritoryType, out string? value);
        string name = value ?? string.Empty;
        if (name == _curDutyRotationName && DataCenter.CurrentDutyRotation != null)
        {
            return; // No change, so we don't need to update
        }

        Type? type = GetChosenType(rotations, name);
        if (type != DataCenter.CurrentDutyRotation?.GetType())
        {
            DataCenter.CurrentDutyRotation?.Dispose();
            DataCenter.CurrentDutyRotation = GetRotation(type);
            _curDutyRotationName = name;
        }

        static DutyRotation? GetRotation(Type? t)
        {
            if (t == null)
            {
                return null;
            }

            try
            {
                return (DutyRotation?)Activator.CreateInstance(t);
            }
            catch (Exception ex)
            {
                BasicWarningHelper.AddSystemWarning($"Failed to create the rotation: {t.Name}");
                PluginLog.Error($"Failed to create the rotation: {t.Name}: {ex.Message}");
                return null;
            }
        }
    }

    private static void UpdateCustomRotation()
    {
        if (Player.Object == null || CustomRotations.Length < 22) // If we haven't loaded rotations, don't bother calling this and ultimately clearing everything; wait for it to load and try again
        {
            return;
        }

        Job nowJob = Player.Job;
        CombatType curCombatType = DataCenter.IsPvP ? CombatType.PvP : CombatType.PvE;

        if (DataCenter.CurrentRotation?.Job == nowJob && DataCenter.CurrentRotation?.GetAttributes()?.Type == curCombatType)
        {
            return; // Nothing has changed, so we don't need to try and find a new rotation
        }

        if (!CustomRotationsLookup.TryGetValue(nowJob, out _))
        {
            InitReferenceDict(nowJob);
        }

        if (CustomRotationsLookup.TryGetValue(nowJob, out Dictionary<CombatType, List<ICustomRotation>>? validCustomRotations)) // Because default rotations exist, this *should* always have something; if not, no rotations
        {
            if (validCustomRotations.Count == 0) // We successfully got something, but we'll still check if there are any valid rotations
            {
                PluginLog.Warning($"No valid rotations found for {nowJob}");
                return;
            }

            if (validCustomRotations.TryGetValue(curCombatType, out List<ICustomRotation>? validCustomRotationsList))
            {
                string desiredRotationName = DataCenter.IsPvP ? Service.Config.PvPRotationChoice : Service.Config.RotationChoice;

                // Check if we have a matching rotation for the config, or use the first rotation which should be our default
                ICustomRotation rotation = validCustomRotationsList[0];
                foreach (var possibleRotation in validCustomRotationsList)
                {
                    if (possibleRotation.GetType().FullName == desiredRotationName)
                    {
                        rotation = possibleRotation;
                        break;
                    }
                }

                //If the rotation has changed, perform a clear
                if (rotation != DataCenter.CurrentRotation)
                {
                    ChangeRotation(rotation);
                }

                return;
            }
        }

        DataCenter.CurrentRotation = null;
        CurrentRotationActions = [];
    }

    public static void ChangeRotation(ICustomRotation rotation)
    {
        rotation.OnTerritoryChanged();
        DataCenter.CurrentRotation = rotation;
        CurrentRotationActions = DataCenter.CurrentRotation?.AllActions ?? [];
    }

    private static ICustomRotation? GetRotation(Type? t)
    {
        if (t == null)
        {
            return null;
        }

        try
        {
            return (ICustomRotation?)Activator.CreateInstance(t);
        }
        catch (Exception)
        {
#if DEBUG
            PluginLog.Error($"Failed to create the rotation: {t.Name}");
#endif
            return null;
        }
    }

    private static void InitReferenceDict(Job currentJob)
    {
        foreach (CustomRotationGroup customRotationGroup in CustomRotations)
        {
            bool classMatch = false;
            var classJobs = customRotationGroup.ClassJobIds;
            for (int i = 0; i < classJobs.Length; i++)
            {
                if (classJobs[i] == currentJob)
                {
                    classMatch = true;
                    break;
                }
            }
            if (!classMatch)
            {
                continue;
            }

            // Add this group for every job in ClassJobIds
            foreach (var job in customRotationGroup.ClassJobIds)
            {
                if (!CustomRotationsLookup.TryGetValue(job, out var rotationsListByType))
                {
                    rotationsListByType = new Dictionary<CombatType, List<ICustomRotation>>
                {
                    { CombatType.PvE, new List<ICustomRotation>() },
                    { CombatType.PvP, new List<ICustomRotation>() }
                };
                    CustomRotationsLookup[job] = rotationsListByType;
                }

                foreach (Type rotationType in customRotationGroup.Rotations)
                {
                    var rotAttr = rotationType.GetCustomAttribute<RotationAttribute>();
                    if (rotAttr == null)
                    {
                        continue;
                    }

                    // Skip rotations that are explicitly disabled
                    if (rotAttr.Disabled)
                    {
                        continue;
                    }

                    CombatType? comType = rotAttr.Type;
                    if (comType == null)
                    {
                        continue;
                    }

                    var possibleRotation = GetRotation(rotationType);
                    if (possibleRotation == null)
                    {
                        continue;
                    }

                    rotationsListByType[(CombatType)comType].Add(possibleRotation);
                }
            }
        }
    }

    private static Type? GetChosenType(IEnumerable<Type> types, string name)
    {
        Type? rotation = null;
        foreach (var r in types)
        {
            if (r.FullName == name)
            {
                rotation = r;
                break;
            }
        }

        if (rotation == null)
        {
            foreach (var r in types)
            {
                if (r.Assembly.FullName != null && r.Assembly.FullName.Contains("DefaultRotations", StringComparison.OrdinalIgnoreCase))
                {
                    rotation = r;
                    break;
                }
            }
        }

        if (rotation == null)
        {
            foreach (var r in types)
            {
                rotation = r;
                break;
            }
        }

        return rotation;
    }
}