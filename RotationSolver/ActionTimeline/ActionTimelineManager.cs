using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using Action = Lumina.Excel.Sheets.Action;

namespace RotationSolver.ActionTimeline;

/// <summary>
/// Manages action timeline data for timeline visualization
/// </summary>
public class ActionTimelineManager : IDisposable
{
    internal const byte GCDCooldownGroup = 58;

    private static ActionTimelineManager? _instance;
    public static ActionTimelineManager Instance => _instance ??= new ActionTimelineManager();

    private readonly Queue<TimelineItem> _items = new(2048);
    private TimelineItem? _lastItem;
    private DateTime _lastTime = DateTime.MinValue;
    private DateTime? _combatStartTime = null;
    private bool _wasInCombat = false;

	private delegate void OnActorControlDelegate(uint entityId, uint type, uint buffID, uint direct, uint actionId, uint sourceId, uint arg7, uint arg8, uint arg9, uint arg10, ulong targetId, byte arg12);
	[Signature("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", DetourName = nameof(OnActorControl))]
#pragma warning disable CS0649
	private readonly Hook<OnActorControlDelegate>? _onActorControlHook;
#pragma warning restore CS0649

	private delegate void OnCastDelegate(uint sourceId, IntPtr sourceCharacter);
	[Signature("40 53 57 48 81 EC ?? ?? ?? ?? 48 8B FA 8B D1", DetourName = nameof(OnCast))]
#pragma warning disable CS0649
	private readonly Hook<OnCastDelegate>? _onCastHook;
#pragma warning restore CS0649

	public DateTime EndTime { get; private set; } = DateTime.Now;

    private ActionTimelineManager()
    {
        try
        {
            Svc.Hook.InitializeFromAttributes(this);
            _onActorControlHook?.Enable();
            _onCastHook?.Enable();
            ActionEffect.ActionEffectEvent += ActionFromSelf;
        }
        catch (Exception e)
        {
            Svc.Log.Error("Error initiating ActionTimeline hooks: " + e.Message);
        }
    }

    public void Dispose()
    {
        _items.Clear();
        ActionEffect.ActionEffectEvent -= ActionFromSelf;
        _onActorControlHook?.Disable();
        _onActorControlHook?.Dispose();
        _onCastHook?.Disable();
        _onCastHook?.Dispose();
        GC.SuppressFinalize(this);
    }

    public unsafe float GCD
    {
        get
        {
            var cdGrp = ActionManager.Instance()->GetRecastGroupDetail(GCDCooldownGroup - 1);
            return cdGrp->Total;
        }
    }

    private static TimelineItemType GetActionType(uint actionId, ActionType type)
    {
        switch (type)
        {
            case ActionType.Action:
                if (Svc.Data.GetExcelSheet<Action>()?.TryGetRow(actionId, out var action) != true)
                    break;

                if (actionId == 3) return TimelineItemType.OGCD; // Sprint

                var isRealGcd = action.CooldownGroup == GCDCooldownGroup || action.AdditionalCooldownGroup == GCDCooldownGroup;
                return action.ActionCategory.Value.RowId == 1 // AutoAttack
                    ? TimelineItemType.AutoAttack
                    : !isRealGcd && action.ActionCategory.Value.RowId == 4 ? TimelineItemType.OGCD // Ability
                    : TimelineItemType.GCD;

            case ActionType.Item:
                if (Svc.Data.GetExcelSheet<Item>()?.TryGetRow(actionId, out var item) != true)
                    break;
                return item.CastTimeSeconds > 0 ? TimelineItemType.GCD : TimelineItemType.OGCD;
        }

        return TimelineItemType.GCD;
    }

    private void AddItem(TimelineItem item)
    {
        if (item == null) return;
        if (_items.Count >= 2048)
        {
            _items.Dequeue();
        }
        _items.Enqueue(item);
        if (item.Type != TimelineItemType.AutoAttack)
        {
            _lastItem = item;
            _lastTime = DateTime.Now;
            UpdateEndTime(item.EndTime);
        }
    }

    private void UpdateEndTime(DateTime endTime)
    {
        if (endTime > EndTime) EndTime = endTime;
    }

    public List<TimelineItem> GetItems(DateTime time, out DateTime lastEndTime)
    {
        var result = new List<TimelineItem>();
        lastEndTime = DateTime.Now;
        foreach (var item in _items)
        {
            if (item.EndTime > time)
            {
                result.Add(item);
            }
            else if (item.Type == TimelineItemType.GCD)
            {
                lastEndTime = item.EndTime;
            }
        }
        return result;
    }

    private void ActionFromSelf(ActionEffectSet set)
    {
        if (!Player.Available) return;
        if (set.Source?.GameObjectId != Player.Object?.GameObjectId) return;

        var now = DateTime.Now;
        var type = GetActionType(set.Header.ActionID, set.Header.ActionType);

        if (_lastItem != null && _lastItem.CastingTime > 0 && type == TimelineItemType.GCD
            && _lastItem.State == TimelineItemState.Casting)
        {
            _lastItem.AnimationLockTime = set.Header.AnimationLockTime;
            _lastItem.Name = set.Name;
            _lastItem.Icon = set.IconId;
            _lastItem.State = TimelineItemState.Finished;
        }
        else
        {
            AddItem(new TimelineItem()
            {
                StartTime = now,
                AnimationLockTime = type == TimelineItemType.AutoAttack ? 0 : set.Header.AnimationLockTime,
                GCDTime = type == TimelineItemType.GCD ? GCD : 0,
                Type = type,
                Name = set.Name,
                Icon = set.IconId,
                State = TimelineItemState.Finished,
            });
        }

        var effectItem = _lastItem;
        if (effectItem?.Type is TimelineItemType.AutoAttack) return;

        UpdateEndTime(effectItem?.EndTime ?? now);
    }

    private void CancelCasting()
    {
        if (_lastItem == null || _lastItem.CastingTime == 0) return;

        _lastItem.State = TimelineItemState.Canceled;
        var maxTime = (float)(DateTime.Now - _lastItem.StartTime).TotalSeconds;
        _lastItem.GCDTime = 0;
        _lastItem.CastingTime = MathF.Min(maxTime, _lastItem.CastingTime);
    }

	private void OnActorControl(uint entityId, uint type, uint buffID, uint direct, uint actionId, uint sourceId, uint arg7, uint arg8, uint arg9, uint arg10, ulong targetId, byte arg12)
	{
		_onActorControlHook?.Original(entityId, type, buffID, direct, actionId, sourceId, arg7, arg8, arg9, arg10, targetId, arg12);

		try
        {
            if (Player.Object == null || entityId != Player.Object.GameObjectId) return;

            // CancelAbility ActorControlCategory value
            if (type == 15)
            {
                CancelCasting();
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"Error in OnActorControl: {ex.Message}");
        }
    }

    private void OnCast(uint sourceId, IntPtr sourceCharacter)
    {
        _onCastHook?.Original(sourceId, sourceCharacter);
        // Additional cast handling could go here
    }

    /// <summary>
    /// Export timeline data to JSON file
    /// </summary>
    /// <param name="filePath">Path to save the JSON file</param>
    /// <param name="combatStartTime">When combat started (for calculating combat time)</param>
    /// <returns>True if export was successful</returns>
    public bool ExportToJson(string filePath, DateTime? combatStartTime = null)
    {
        try
        {
            var session = CreateExportSession(combatStartTime);
            var json = JsonConvert.SerializeObject(session, Formatting.Indented);
            
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(filePath, json);
            return true;
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"Failed to export timeline to JSON: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Create export session data from current timeline items
    /// </summary>
    /// <param name="combatStartTime">When combat started</param>
    /// <returns>Export session data</returns>
    private TimelineExportSession CreateExportSession(DateTime? combatStartTime)
    {
        var items = _items.ToArray();
        var session = new TimelineExportSession();

        if (items.Length == 0)
        {
            return session;
        }

        DateTime startTime = combatStartTime ?? items[0].StartTime;
        DateTime endTime = items[0].EndTime;
        for (int i = 1; i < items.Length; i++)
        {
            if (items[i].StartTime < startTime) startTime = items[i].StartTime;
            if (items[i].EndTime > endTime) endTime = items[i].EndTime;
        }

		// Fill session info
		session.SessionInfo = new SessionInfo
		{
			StartTime = startTime,
			EndTime = endTime,
			DurationSeconds = (endTime - startTime).TotalSeconds,
			PlayerName = Player.Available && Player.Object != null ? Player.Object.Name.TextValue : "Unknown",
			PlayerJob = Player.Available ? Player.Job.ToString() : "Unknown",
			Territory = DataCenter.Territory?.Name ?? "Unknown",
			Duty = DataCenter.Territory?.ContentFinderName ?? "Unknown",
			ExportedAt = DateTime.Now
		};

		// Convert timeline items to export format
		Array.Sort(items, (a, b) => a.StartTime.CompareTo(b.StartTime));
        foreach (var item in items)
        {
            var exportedAction = new ExportedAction
            {
                Name = item.Name,
                Id = 0, // We don't store action ID in TimelineItem currently
                Icon = item.Icon,
                Type = item.Type.ToString(),
                StartTime = item.StartTime,
                EndTime = item.EndTime,
                CombatTimeSeconds = (item.StartTime - startTime).TotalSeconds,
                CastTimeSeconds = Math.Max(item.CastingTime + item.AnimationLockTime, item.GCDTime),
                State = item.State.ToString(),
                Target = "" // We don't currently track target information
            };

            session.Actions.Add(exportedAction);
        }

        return session;
    }

    /// <summary>
    /// Get a suggested filename for the export
    /// </summary>
    /// <returns>Suggested filename with timestamp</returns>
    public static string GetSuggestedFilename()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var jobName = Player.Available ? Player.Job.ToString() : "Unknown";
        var dutyName = DataCenter.Territory?.ContentFinderName ?? DataCenter.Territory?.Name ?? "Timeline";
        
        // Sanitize filename
        dutyName = string.Join("_", dutyName.Split(Path.GetInvalidFileNameChars()));
        
        return $"{timestamp}_{jobName}_{dutyName}.json";
    }

    /// <summary>
    /// Update combat state and handle automatic export
    /// </summary>
    public void UpdateCombatState()
    {
        if (DataCenter.InCombat && !_wasInCombat)
        {
            // Combat started
            _combatStartTime = DateTime.Now;
        }
        else if (!DataCenter.InCombat && _wasInCombat && _combatStartTime.HasValue)
        {
            // Combat ended - check if we should auto-export
            if (Service.Config.ActionTimelineSaveToFile && _items.Count > 0)
            {
                var timelineFolder = Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "ActionTimeline");
                var filename = GetSuggestedFilename();
                var fullPath = Path.Combine(timelineFolder, filename);
                
                if (ExportToJson(fullPath, _combatStartTime))
                {
                    Svc.Log.Info($"Action timeline exported to: {fullPath}");
                }
            }
            
            _combatStartTime = null;
        }
        
        _wasInCombat = DataCenter.InCombat;
    }
}

/// <summary>
/// Represents an item in the action timeline
/// </summary>
public class TimelineItem
{
    public DateTime StartTime { get; set; }
    public string Name { get; set; } = "";
    public uint Icon { get; set; }
    public float CastingTime { get; set; }
    public float AnimationLockTime { get; set; }
    public float GCDTime { get; set; }
    public TimelineItemType Type { get; set; }
    public TimelineItemState State { get; set; }

    public DateTime EndTime => StartTime.AddSeconds(Math.Max(CastingTime + AnimationLockTime, GCDTime));
}

/// <summary>
/// Type of timeline item
/// </summary>
public enum TimelineItemType
{
    GCD,
    OGCD,
    AutoAttack
}

/// <summary>
/// State of timeline item
/// </summary>
public enum TimelineItemState
{
    Casting,
    Finished,
    Canceled
}
