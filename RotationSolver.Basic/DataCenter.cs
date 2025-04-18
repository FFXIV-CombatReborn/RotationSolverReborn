using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using Lumina.Excel.Sheets;
using RotationSolver.Basic.Configuration;
using RotationSolver.Basic.Configuration.Conditions;
using RotationSolver.Basic.Rotations.Duties;
using Action = Lumina.Excel.Sheets.Action;
using CharacterManager = FFXIVClientStructs.FFXIV.Client.Game.Character.CharacterManager;
using CombatRole = RotationSolver.Basic.Data.CombatRole;

namespace RotationSolver.Basic;

internal static class DataCenter
{
    private static ulong _hostileTargetId = 0;

    public static bool ResetActionConfigs { get; set; } = false;

    public static bool IsActivated() => State || IsManual || Service.Config.TeachingMode;

    internal static IBattleChara? HostileTarget
    {
        get => Svc.Objects.SearchById(_hostileTargetId) as IBattleChara;
        set => _hostileTargetId = value?.GameObjectId ?? 0;
    }

    internal static List<uint> PrioritizedNameIds { get; set; } = new();
    internal static List<uint> BlacklistedNameIds { get; set; } = new();

    internal static List<VfxNewData> VfxDataQueue { get; } = new();

    /// <summary>
    /// This one never be null.
    /// </summary>
    public static MajorConditionValue CurrentConditionValue
    {
        get
        {
            if (ConditionSets == null || ConditionSets.Length == 0)
            {
                ConditionSets = new[] { new MajorConditionValue() };
            }

            var index = Service.Config.ActionSequencerIndex;
            if (index < 0 || index >= ConditionSets.Length)
            {
                Service.Config.ActionSequencerIndex = index = 0;
            }

            return ConditionSets[index];
        }
    }

    internal static MajorConditionValue[] ConditionSets { get; set; } = Array.Empty<MajorConditionValue>();

    /// <summary>
    /// Only recorded 15s hps.
    /// </summary>
    public const int HP_RECORD_TIME = 240;

    internal static Queue<(DateTime time, SortedList<ulong, float> hpRatios)> RecordedHP { get; } =
        new(HP_RECORD_TIME + 1);

    public static ICustomRotation? CurrentRotation { get; internal set; }
    public static DutyRotation? CurrentDutyRotation { get; internal set; }

    public static Dictionary<string, DateTime> SystemWarnings { get; set; } = new();

    internal static bool NoPoslock => Svc.Condition[ConditionFlag.OccupiedInEvent]
                                      || !Service.Config.PoslockCasting
                                      //Key cancel.
                                      || Svc.KeyState[Service.Config.PoslockModifier.ToVirtual()]
                                      //Gamepad cancel.
                                      || Svc.GamepadState.Raw(Dalamud.Game.ClientState.GamePad.GamepadButtons.R1) >=
                                      0.5f;

    internal static DateTime EffectTime { private get; set; } = DateTime.Now;
    internal static DateTime EffectEndTime { private get; set; } = DateTime.Now;

    internal static int AttackedTargetsCount { get; set; } = 48;
    internal static Queue<(ulong id, DateTime time)> AttackedTargets { get; } = new(AttackedTargetsCount);

    internal static bool InEffectTime => DateTime.Now >= EffectTime && DateTime.Now <= EffectEndTime;
    internal static Dictionary<ulong, uint> HealHP { get; set; } = new();
    internal static Dictionary<ulong, uint> ApplyStatus { get; set; } = new();
    internal static uint MPGain { get; set; }

    public static TerritoryInfo? Territory { get; set; }

    public static bool IsPvP => Territory?.IsPvP ?? false;
    public static bool IsInDuty => Svc.Condition[ConditionFlag.BoundByDuty] || Svc.Condition[ConditionFlag.BoundByDuty56];
    public static bool IsInAllianceRaid
    {
        get
        {
            var allianceTerritoryIds = new HashSet<ushort>
        {
            151, 174, 372, 508, 556, 627, 734, 776, 826, 882, 917, 966, 1054, 1118, 1178, 1248, 1241
        };
            return allianceTerritoryIds.Contains(TerritoryID);
        }
    }

    public static ushort TerritoryID => Svc.ClientState.TerritoryType;
    public static bool IsInUCoB => TerritoryID == 733;
    public static bool IsInUwU => TerritoryID == 777;
    public static bool IsInTEA => TerritoryID == 887;
    public static bool IsInDSR => TerritoryID == 968;
    public static bool IsInTOP => TerritoryID == 1122;
    public static bool IsInFRU => TerritoryID == 1238;
    public static bool IsInCOD => TerritoryID == 1241;


    public static AutoStatus MergedStatus => AutoStatus | CommandStatus;

    public static AutoStatus AutoStatus { get; set; } = AutoStatus.None;
    public static AutoStatus CommandStatus { get; set; } = AutoStatus.None;

    public static HashSet<uint> DisabledActionSequencer { get; set; } = new();

    private static List<NextAct> NextActs = new();
    public static IAction? ActionSequencerAction { private get; set; }

    public static IAction? CommandNextAction
    {
        get
        {
            var next = NextActs.FirstOrDefault();

            while (next != null && NextActs.Count > 0 &&
                   (next.DeadTime < DateTime.Now || IActionHelper.IsLastAction(true, next.Act)))
            {
                NextActs.RemoveAt(0);
                next = NextActs.FirstOrDefault();
            }

            return next?.Act ?? ActionSequencerAction;
        }
    }

    public static Job Job => Player.Job;

    public static JobRole Role
    {
        get
        {
            var classJob = Service.GetSheet<ClassJob>().GetRow((uint)Job);
            return classJob.RowId != 0 ? classJob.GetJobRole() : JobRole.None;
        }
    }

    internal static void AddCommandAction(IAction act, double time)
    {
        var index = -1;
        for (int i = 0; i < NextActs.Count; i++)
        {
            if (NextActs[i].Act.ID == act.ID)
            {
                index = i;
                break;
            }
        }

        var newItem = new NextAct(act, DateTime.Now.AddSeconds(time));
        if (index < 0)
        {
            NextActs.Add(newItem);
        }
        else
        {
            NextActs[index] = newItem;
        }

        NextActs.Sort((a, b) => a.DeadTime.CompareTo(b.DeadTime));
    }

    public static TargetHostileType CurrentTargetToHostileType => Service.Config.HostileType;
    public static TinctureUseType CurrentTinctureUseType => Service.Config.TinctureType;

    public static unsafe ActionID LastComboAction => (ActionID)ActionManager.Instance()->Combo.Action;
    public static unsafe float ComboTime => ActionManager.Instance()->Combo.Timer;

    public static TargetingType TargetingType
    {
        get
        {
            if (Service.Config.TargetingTypes.Count == 0)
            {
                Service.Config.TargetingTypes.Add(TargetingType.LowHP);
                Service.Config.TargetingTypes.Add(TargetingType.HighHP);
                Service.Config.TargetingTypes.Add(TargetingType.Small);
                Service.Config.TargetingTypes.Add(TargetingType.Big);
                Service.Config.Save();
            }

            return Service.Config.TargetingTypes[Service.Config.TargetingIndex % Service.Config.TargetingTypes.Count];
        }
    }

    public static bool IsMoving { get; internal set; }

    internal static float StopMovingRaw { get; set; }

    internal static float MovingRaw { get; set; }
    internal static float DeadTimeRaw { get; set; }
    internal static float AliveTimeRaw { get; set; }

    public static unsafe ushort FateId
    {
        get
        {
            try
            {
                if ((IntPtr)FateManager.Instance() != IntPtr.Zero
                    && (IntPtr)FateManager.Instance()->CurrentFate != IntPtr.Zero
                    && Player.Level <= FateManager.Instance()->CurrentFate->MaxLevel)
                {
                    return FateManager.Instance()->CurrentFate->FateId;
                }
            }
            catch (Exception ex)
            {
                Svc.Log.Error(ex.StackTrace ?? ex.Message);
            }

            return 0;
        }
    }

    #region GCD
    /// <summary>
    /// Returns the time remaining until the next GCD (Global Cooldown) after considering the current animation lock.
    /// </summary>
    public static float NextAbilityToNextGCD => DefaultGCDRemain - Math.Min(ActionManagerHelper.GetCurrentAnimationLock(), MinAnimationLock);

    /// <summary>
    /// Returns the total duration of the default GCD.
    /// </summary>
    public static float DefaultGCDTotal => ActionManagerHelper.GetDefaultRecastTime();

    /// <summary>
    /// Returns the remaining time for the default GCD by subtracting the elapsed time from the total recast time.
    /// </summary>
    public static float DefaultGCDRemain => DefaultGCDTotal - DefaultGCDElapsed;

    /// <summary>
    /// Returns the elapsed time since the start of the default GCD.
    /// </summary>
    public static float DefaultGCDElapsed => ActionManagerHelper.GetDefaultRecastTimeElapsed();

    /// <summary>
    /// Returns the action ahead time, which can be overridden by a configuration setting.
    /// </summary>
    public static float ActionAhead => Service.Config.OverrideActionAheadTimer ? Service.Config.Action4Head : CalculatedActionAhead;

    /// <summary>
    /// Calculates the action ahead time based on the default GCD total and minimum animation lock.
    /// </summary>
    public static float CalculatedActionAhead => Math.Min(DefaultGCDTotal * 0.20f, MinAnimationLock);

    /// <summary>
    /// Calculates the total GCD time for a given number of GCDs and an optional offset.
    /// </summary>
    /// <param name="gcdCount">The number of GCDs.</param>
    /// <param name="offset">The optional offset.</param>
    /// <returns>The total GCD time.</returns>
    public static float GCDTime(uint gcdCount = 0, float offset = 0) => DefaultGCDTotal * gcdCount + offset;
    #endregion


    public static uint[] BluSlots { get; internal set; } = new uint[24];

    public static uint[] DutyActions { get; internal set; } = new uint[2];

    static DateTime _specialStateStartTime = DateTime.MinValue;
    private static double SpecialTimeElapsed => (DateTime.Now - _specialStateStartTime).TotalSeconds;
    public static double SpecialTimeLeft => Service.Config.SpecialDuration - SpecialTimeElapsed;

    static SpecialCommandType _specialType = SpecialCommandType.EndSpecial;

    internal static SpecialCommandType SpecialType
    {
        get => SpecialTimeLeft < 0 ? SpecialCommandType.EndSpecial : _specialType;
        set
        {
            _specialType = value;
            _specialStateStartTime = value == SpecialCommandType.EndSpecial ? DateTime.MinValue : DateTime.Now;
        }
    }

    public static bool State { get; set; } = false;

    public static bool IsManual { get; set; } = false;

    public static bool InCombat { get; set; }

    static RandomDelay _notInCombatDelay = new(() => Service.Config.NotInCombatDelay);

    /// <summary>
    /// Is out of combat.
    /// </summary>
    public static bool NotInCombatDelay => _notInCombatDelay.Delay(!InCombat);

    internal static float CombatTimeRaw { get; set; }
    private static DateTime _startRaidTime = DateTime.MinValue;

    internal static float RaidTimeRaw
    {
        get
        {
            // If the raid start time is not set, return 0.
            if (_startRaidTime == DateTime.MinValue) return 0;

            // Calculate and return the total seconds elapsed since the raid started.
            return (float)(DateTime.Now - _startRaidTime).TotalSeconds;
        }
        set
        {
            // If the provided value is negative, reset the raid start time.
            if (value < 0)
            {
                _startRaidTime = DateTime.MinValue;
            }
            else
            {
                // Set the raid start time to the current time minus the provided value in seconds.
                _startRaidTime = DateTime.Now - TimeSpan.FromSeconds(value);
            }
        }
    }

    public static List<IBattleChara> PartyMembers { get; set; } = [];

    public static List<IBattleChara> AllianceMembers { get; set; } = [];

    public static List<IBattleChara> FriendlyNPCMembers { get; set; } = [];

    public static List<IBattleChara> AllHostileTargets { get; set; } = [];

    public static IBattleChara? InterruptTarget { get; set; }

    public static IBattleChara? ProvokeTarget { get; set; }

    public static IBattleChara? DeathTarget { get; set; }

    public static IBattleChara? DispelTarget { get; set; }

    public static List<IBattleChara> AllTargets { get; set; } = [];

    public static ulong[] TreasureCharas
    {
        get
        {
            List<ulong> charas = new(5);
            //60687 - 60691 For treasure hunt.
            for (int i = 60687; i <= 60691; i++)
            {
                var b = AllTargets.FirstOrDefault(obj => obj.GetNamePlateIcon() == i);
                if (b == null || b.CurrentHp == 0) continue;
                charas.Add(b.GameObjectId);
            }

            return charas.ToArray();
        }
    }

    public static bool HasHostilesInRange => NumberOfHostilesInRange > 0;
    public static bool HasHostilesInMaxRange => NumberOfHostilesInMaxRange > 0;
    public static int NumberOfHostilesInRange
    {
        get
        {
            int count = 0;
            foreach (var o in AllHostileTargets)
            {
                if (o.DistanceToPlayer() < JobRange)
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
            int count = 0;
            foreach (var o in AllHostileTargets)
            {
                if (o.DistanceToPlayer() < 25)
                {
                    count++;
                }
            }
            return count;
        }
    }
    public static int NumberOfAllHostilesInRange => NumberOfHostilesInRange;
    public static int NumberOfAllHostilesInMaxRange => NumberOfHostilesInMaxRange;

    public static bool MobsTime
    {
        get
        {
            int count = 0;
            foreach (var o in AllHostileTargets)
            {
                if (o.DistanceToPlayer() < JobRange && o.CanSee())
                {
                    count++;
                }
            }
            return count >= Service.Config.AutoDefenseNumber;
        }
    }

    public static bool AreHostilesCastingKnockback
    {
        get
        {
            foreach (var h in AllHostileTargets)
            {
                if (IsHostileCastingKnockback(h))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public static float JobRange
    {
        get
        {
            float radius = 25;
            if (!Player.Available) return radius;

            switch (DataCenter.Role)
            {
                case JobRole.Tank:
                case JobRole.Melee:
                    radius = 3;
                    break;
            }

            return radius;
        }
    }

    public static float AverageTTK
    {
        get
        {
            float total = 0;
            int count = 0;
            foreach (var b in AllHostileTargets)
            {
                var tTK = b.GetTTK();
                if (!float.IsNaN(tTK))
                {
                    total += tTK;
                    count++;
                }
            }
            return count > 0 ? total / count : 0;
        }
    }

    public static bool IsHostileCastingAOE => IsCastingAreaVfx() || AllHostileTargets.Any(IsHostileCastingArea);

    public static bool IsHostileCastingToTank => IsCastingTankVfx() || AllHostileTargets.Any(IsHostileCastingTank);

    public static bool IsHostileCastingStop => InCombat && Service.Config.CastingStop && AllHostileTargets.Any(IsHostileStop);

    private static DateTime _petLastSeen = DateTime.MinValue;

    public static bool IsHostileStop(IBattleChara h)
    {
        return IsHostileCastingStopBase(h,
            (act) => act.RowId != 0 && OtherConfiguration.HostileCastingStop.Contains(act.RowId));
    }

    public static bool IsHostileCastingStopBase(IBattleChara h, Func<Action, bool> check)
    {
        // Check if h is null
        if (h == null) return false;

        // Check if the hostile character is casting
        if (!h.IsCasting) return false;

        // Check if the cast is interruptible
        if (h.IsCastInterruptible) return false;

        // Validate the cast time
        if ((h.TotalCastTime - h.CurrentCastTime) > (Service.Config.CastingStopCalculate ? 100 : Service.Config.CastingStopTime)) return false;

        // Get the action sheet
        var actionSheet = Service.GetSheet<Action>();
        if (actionSheet == null) return false; // Check if actionSheet is null

        // Get the action being cast
        var action = actionSheet.GetRow(h.CastActionId);
        if (action.RowId == 0) return false; // Check if action is not initialized

        // Invoke the check function on the action and return the result
        return check?.Invoke(action) ?? false; // Check if check is null
    }

    public static bool HasPet
    {
        get
        {
            foreach (var npc in AllTargets)
            {
                if (npc is IBattleNpc battleNpc && battleNpc.OwnerId == Player.Object.GameObjectId && battleNpc.BattleNpcKind == BattleNpcSubKind.Pet)
                {
                    _petLastSeen = DateTime.Now;
                    return true;
                }
            }

            if (Svc.Condition[ConditionFlag.Mounted] ||
                Svc.Condition[ConditionFlag.Mounted2] ||
                Svc.Condition[ConditionFlag.BetweenAreas] ||
                Svc.Condition[ConditionFlag.BetweenAreas51] ||
                Svc.Condition[ConditionFlag.BeingMoved] ||
                Svc.Condition[ConditionFlag.WatchingCutscene] ||
                Svc.Condition[ConditionFlag.OccupiedInEvent] ||
                Svc.Condition[ConditionFlag.Occupied33] ||
                Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent] ||
                Svc.Condition[ConditionFlag.Jumping61])
            {
                _petLastSeen = DateTime.Now;
                return true;
            }

            if (_petLastSeen.AddSeconds(3) < DateTime.Now)
            {
                return false;
            }

            return true;
        }
    }

    public static unsafe bool HasCompanion
    {
        get
        {
            var playerBattleChara = Player.BattleChara;
            if (playerBattleChara == null) return false;

            var characterManager = CharacterManager.Instance();
            if (characterManager == null) return false;

            var companion = characterManager->LookupBuddyByOwnerObject(playerBattleChara);
            return (IntPtr)companion != IntPtr.Zero;
        }
    }

    public static unsafe BattleChara* GetCompanion()
    {
        var playerBattleChara = Player.BattleChara;
        if (playerBattleChara == null) return null;

        var characterManager = CharacterManager.Instance();
        if (characterManager == null) return null;

        return characterManager->LookupBuddyByOwnerObject(playerBattleChara);
    }

    #region HP

    public static Dictionary<ulong, float> RefinedHP
    {
        get
        {
            var refinedHP = new Dictionary<ulong, float>();
            foreach (var member in PartyMembers)
            {
                try
                {
                    if (member == null || member.GameObjectId == 0)
                    {
                        continue; // Skip invalid or null members
                    }

                    refinedHP[member.GameObjectId] = GetPartyMemberHPRatio(member);
                }
                catch (AccessViolationException ex)
                {
                    Svc.Log.Error($"AccessViolationException in RefinedHP: {ex.Message}");
                    continue; // Skip problematic members
                }
            }
            return refinedHP;
        }
    }

    private static Dictionary<ulong, uint> _lastHp = new Dictionary<ulong, uint>();

    private static float GetPartyMemberHPRatio(IBattleChara member)
    {
        if (member == null) throw new ArgumentNullException(nameof(member));

        if (!DataCenter.InEffectTime || !DataCenter.HealHP.TryGetValue(member.GameObjectId, out var healedHp))
        {
            return (float)member.CurrentHp / member.MaxHp;
        }

        var currentHp = member.CurrentHp;
        if (currentHp > 0)
        {
            _lastHp.TryGetValue(member.GameObjectId, out var lastHp);

            if (currentHp - lastHp == healedHp)
            {
                DataCenter.HealHP.Remove(member.GameObjectId);
                return (float)currentHp / member.MaxHp;
            }

            return Math.Min(1, (healedHp + currentHp) / (float)member.MaxHp);
        }

        return (float)currentHp / member.MaxHp;
    }

    public static IEnumerable<float> PartyMembersHP
    {
        get
        {
            foreach (var hp in RefinedHP.Values)
            {
                if (hp > 0)
                {
                    yield return hp;
                }
            }
        }
    }

    public static float PartyMembersMinHP
    {
        get
        {
            float minHP = float.MaxValue;
            bool hasMembers = false;

            foreach (var hp in PartyMembersHP)
            {
                if (hp < minHP)
                {
                    minHP = hp;
                }
                hasMembers = true;
            }

            return hasMembers ? minHP : 0;
        }
    }

    public static float PartyMembersAverHP
    {
        get
        {
            float totalHP = 0;
            int count = 0;

            foreach (var hp in PartyMembersHP)
            {
                totalHP += hp;
                count++;
            }

            return count > 0 ? totalHP / count : 0;
        }
    }

    public static float PartyMembersDifferHP
    {
        get
        {
            var partyMembersHP = new List<float>(PartyMembersHP);
            if (partyMembersHP.Count == 0) return 0;

            var averageHP = partyMembersHP.Average();
            var variance = 0f;

            foreach (var hp in partyMembersHP)
            {
                variance += (hp - averageHP) * (hp - averageHP);
            }

            return (float)Math.Sqrt(variance / partyMembersHP.Count);
        }
    }

    public static bool HPNotFull => PartyMembersMinHP < 1;

    public static uint CurrentMp => Math.Min(10000, Player.Object.CurrentMp + MPGain);
    #endregion

    internal static Queue<MacroItem> Macros { get; } = new Queue<MacroItem>();

    #region Action Record
    public const float MinAnimationLock = 0.6f;

    const int QUEUECAPACITY = 32;
    private static readonly Queue<ActionRec> _actions = new(QUEUECAPACITY);
    private static readonly Queue<DamageRec> _damages = new(QUEUECAPACITY);

    internal static CombatRole? BluRole => (CurrentRotation as BlueMageRotation)?.BlueId;

    public static float DPSTaken
    {
        get
        {
            try
            {
                var recs = new List<DamageRec>();
                foreach (var rec in _damages)
                {
                    if (DateTime.Now - rec.ReceiveTime < TimeSpan.FromMilliseconds(5))
                    {
                        recs.Add(rec);
                    }
                }

                if (recs.Count == 0) return 0;

                var damages = recs.Sum(r => r.Ratio);
                var time = recs.Last().ReceiveTime - recs.First().ReceiveTime + TimeSpan.FromMilliseconds(2.5f);

                return damages / (float)time.TotalSeconds;
            }
            catch
            {
                return 0;
            }
        }
    }

    public static ActionRec[] RecordActions => _actions.Reverse().ToArray();
    private static DateTime _timeLastActionUsed = DateTime.Now;
    public static TimeSpan TimeSinceLastAction => DateTime.Now - _timeLastActionUsed;

    public static ActionID LastAction { get; private set; } = 0;

    public static ActionID LastGCD { get; private set; } = 0;

    public static ActionID LastAbility { get; private set; } = 0;

    internal static unsafe void AddActionRec(Action act)
    {
        if (!Player.Available) return;

        var id = (ActionID)act.RowId;

        //Record
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
            _actions.Dequeue();
        }

        _timeLastActionUsed = DateTime.Now;
        _actions.Enqueue(new ActionRec(_timeLastActionUsed, act));
    }

    internal static void ResetAllRecords()
    {
        LastAction = 0;
        LastGCD = 0;
        LastAbility = 0;
        _timeLastActionUsed = DateTime.Now;
        _actions.Clear();

        AttackedTargets.Clear();
        VfxDataQueue.Clear();
    }

    internal static void AddDamageRec(float damageRatio)
    {
        if (_damages.Count >= QUEUECAPACITY)
        {
            _damages.Dequeue();
        }

        _damages.Enqueue(new DamageRec(DateTime.Now, damageRatio));
    }

    internal static DateTime KnockbackFinished { get; set; } = DateTime.MinValue;
    internal static DateTime KnockbackStart { get; set; } = DateTime.MinValue;

    #endregion

    internal static SortedList<string, string> AuthorHashes { get; set; } = [];

    public static bool IsCastingTankVfx()
    {
        return IsCastingVfx(VfxDataQueue, s =>
        {
            if (!s.Path.StartsWith("vfx/lockon/eff/tank_lockon")
            && !s.Path.StartsWith("vfx/lockon/eff/tank_laser")) return false;

            if (!Player.Available) return false;
            if (Player.Object.IsJobCategory(JobRole.Tank) && s.ObjectId != Player.Object.GameObjectId) return false;
            return true;
        });
    }

    public static bool IsCastingAreaVfx()
    {
        return IsCastingVfx(VfxDataQueue, s =>
        {
            if (!s.Path.StartsWith("vfx/lockon/eff/coshare")
            && !s.Path.StartsWith("vfx/lockon/eff/share_laser")
            && !s.Path.StartsWith("vfx/lockon/eff/com_share")) return false;

            if (!Player.Available) return false;
            return true;
        });
    }

    public static bool IsCastingVfx(List<VfxNewData> vfxDataQueueCopy, Func<VfxNewData, bool> isVfx)
    {
        // Create a copy of the list to avoid modification during enumeration
        var vfxDataQueueSnapshot = new List<VfxNewData>(vfxDataQueueCopy);

        // Ensure the list is not empty
        if (vfxDataQueueSnapshot.Count == 0)
        {
            return false;
        }

        // Iterate over the copied list
        foreach (var vfx in vfxDataQueueSnapshot)
        {
            if (isVfx(vfx))
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsHostileCastingTank(IBattleChara h)
    {
        return IsHostileCastingBase(h, (act) =>
        {
            return OtherConfiguration.HostileCastingTank.Contains(act.RowId)
                   || h.CastTargetObjectId == h.TargetObjectId;
        });
    }

    public static bool IsHostileCastingArea(IBattleChara h)
    {
        return IsHostileCastingBase(h, (act) => { return OtherConfiguration.HostileCastingArea.Contains(act.RowId); });
    }

    public static bool IsHostileCastingKnockback(IBattleChara h)
    {
        return IsHostileCastingBase(h,
            (act) => act.RowId != 0 && OtherConfiguration.HostileCastingKnockback.Contains(act.RowId));
    }

    public static bool IsHostileCastingBase(IBattleChara h, Func<Action, bool> check)
    {
        // Check if h is null
        if (h == null) return false;

        try
        {
            // Check if the hostile character is casting
            if (!h.IsCasting) return false;
        }
        catch (AccessViolationException ex)
        {
            // Log the exception and return false
            Svc.Log.Error($"AccessViolationException: {ex.Message}");
            return false;
        }

        // Check if the cast is interruptible
        if (h.IsCastInterruptible) return false;

        // Calculate the time since the cast started
        var last = h.TotalCastTime - h.CurrentCastTime;
        var t = last - DataCenter.DefaultGCDTotal;

        // Check if the total cast time is greater than the minimum cast time and if the calculated time is within a valid range
        if (!(h.TotalCastTime > DataCenter.DefaultGCDTotal && t > 0 && t < DataCenter.GCDTime(1))) return false;

        // Get the action sheet
        var actionSheet = Service.GetSheet<Action>();
        if (actionSheet == null) return false; // Check if actionSheet is null

        // Get the action being cast
        var action = actionSheet.GetRow(h.CastActionId);
        if (action.RowId == 0) return false; // Check if action is not initialized

        // Invoke the check function on the action and return the result
        return check?.Invoke(action) ?? false; // Check if check is null
    }
}
