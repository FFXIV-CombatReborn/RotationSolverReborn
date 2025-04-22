using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;
using ECommons.EzIpcManager;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using RotationSolver.Commands;

namespace RotationSolver.Updaters;

internal static class ActionUpdater
{
    internal static DateTime AutoCancelTime { get; set; } = DateTime.MinValue;

    static ActionUpdater()
    {
        EzIPC.Init(typeof(ActionUpdater), "RotationSolverReborn.ActionUpdater");
    }

    [EzIPCEvent] public static Action<uint> NextGCDActionChanged = delegate { };
    [EzIPCEvent] public static Action<uint> NextActionChanged = delegate { };

    private static IAction? _nextAction;
    internal static IAction? NextAction
    {
        get => _nextAction;
        set
        {
            if (_nextAction != value)
            {
                _nextAction = value;
                NextActionChanged?.Invoke(_nextAction?.ID ?? 0);
            }
        }
    }

    private static IBaseAction? _nextGCDAction;
    const float GcdHeight = 5;
    internal static IBaseAction? NextGCDAction
    {
        get => _nextGCDAction;
        set
        {
            if (_nextGCDAction != value)
            {
                _nextGCDAction = value;
                NextGCDActionChanged?.Invoke(_nextGCDAction?.AdjustedID ?? 0);
            }
        }
    }

    internal static void ClearNextAction()
    {
        SetAction(0);
        NextAction = NextGCDAction = null;
    }

    internal static void UpdateNextAction()
    {
        IPlayerCharacter localPlayer = Player.Object;
        var customRotation = DataCenter.CurrentRotation;

        try
        {
            if (localPlayer != null && customRotation != null
                && customRotation.TryInvoke(out var newAction, out var gcdAction))
            {
                NextAction = newAction;
                NextGCDAction = gcdAction as IBaseAction;
                return;
            }
        }
        catch (Exception ex)
        {
            LogError("Failed to update the next action in the rotation", ex);
        }

        NextAction = NextGCDAction = null;
    }

    private static List<uint> actionOverrideList = new();

    private static void SetAction(uint id)
    {
        if (actionOverrideList.Count == 0)
        {
            actionOverrideList.Add(id);
        }
        else
        {
            actionOverrideList[0] = id;
        }
    }

    internal unsafe static void UpdateActionInfo()
    {
        SetAction(NextGCDAction?.AdjustedID ?? 0);
        UpdateCombatTime();
        UpdateSlots();
        UpdateMoving();
        UpdateLifetime();
        UpdateMPTimer();
    }

    private unsafe static void UpdateSlots()
    {
        ActionManager* actionManager = ActionManager.Instance();
        for (int i = 0; i < DataCenter.BluSlots.Length; i++)
        {
            DataCenter.BluSlots[i] = actionManager->GetActiveBlueMageActionInSlot(i);
        }
        for (ushort i = 0; i < DataCenter.DutyActions.Length; i++)
        {
            DataCenter.DutyActions[i] = DutyActionManager.GetDutyActionId(i);
        }
    }

    static DateTime _startMovingTime = DateTime.MinValue;
    static DateTime _stopMovingTime = DateTime.MinValue;

    private unsafe static void UpdateMoving()
    {
        var last = DataCenter.IsMoving;
        DataCenter.IsMoving = AgentMap.Instance()->IsPlayerMoving;
        if (last && !DataCenter.IsMoving)
        {
            _stopMovingTime = DateTime.Now;
        }
        else if (DataCenter.IsMoving && !last)
        {
            _startMovingTime = DateTime.Now;
        }

        DataCenter.StopMovingRaw = DataCenter.IsMoving
            ? 0
            : (float)(DateTime.Now - _stopMovingTime).TotalSeconds;

        DataCenter.MovingRaw = DataCenter.IsMoving
            ? (float)(DateTime.Now - _startMovingTime).TotalSeconds
            : 0;
    }
    private static DateTime _startDeadTime = DateTime.MinValue;
    private static DateTime _startAliveTime = DateTime.Now;
    private static bool _isDead = true;
    public static void UpdateLifetime()
    {
        if (Player.Object == null) return;

        var lastDead = _isDead;
        _isDead = Player.Object.IsDead;

        if (Svc.Condition[ConditionFlag.BetweenAreas])
        {
            _startAliveTime = DateTime.Now;
        }
        switch (lastDead)
        {
            case true when !Player.Object.IsDead:
                _startAliveTime = DateTime.Now;
                break;
            case false when Player.Object.IsDead:
                _startDeadTime = DateTime.Now;
                break;
        }

        DataCenter.DeadTimeRaw = Player.Object.IsDead
            ? (float)(DateTime.Now - _startDeadTime).TotalSeconds
            : 0;

        DataCenter.AliveTimeRaw = Player.Object.IsDead
            ? 0
            : (float)(DateTime.Now - _startAliveTime).TotalSeconds;
    }

    static DateTime _startCombatTime = DateTime.MinValue;
    private static void UpdateCombatTime()
    {
        var lastInCombat = DataCenter.InCombat;
        DataCenter.InCombat = Svc.Condition[ConditionFlag.InCombat];

        if (!lastInCombat && DataCenter.InCombat)
        {
            _startCombatTime = DateTime.Now;
        }
        else if (lastInCombat && !DataCenter.InCombat)
        {
            _startCombatTime = DateTime.MinValue;

            if (Service.Config.AutoOffAfterCombat)
            {
                AutoCancelTime = DateTime.Now.AddSeconds(Service.Config.AutoOffAfterCombatTime);
            }
        }

        DataCenter.CombatTimeRaw = _startCombatTime == DateTime.MinValue
            ? 0
            : (float)(DateTime.Now - _startCombatTime).TotalSeconds;
    }

    static uint _lastMP = 0;
    static DateTime _lastMPUpdate = DateTime.Now;

    internal static float MPUpdateElapsed => (float)(DateTime.Now - _lastMPUpdate).TotalSeconds % 3;

    private static void UpdateMPTimer()
    {
        if (Player.Object == null) return;

        // Ignore if player is Black Mage
        if (Player.Object.ClassJob.RowId != (uint)ECommons.ExcelServices.Job.BLM) return;

        // Ignore if player is Lucid Dreaming
        if (Player.Object.HasStatus(true, StatusID.LucidDreaming)) return;

        if (_lastMP < Player.Object.CurrentMp)
        {
            _lastMPUpdate = DateTime.Now;
        }
        _lastMP = Player.Object.CurrentMp;
    }

    internal unsafe static bool CanDoAction()
    {
        if (IsPlayerOccupied() || Player.Object.CurrentHp == 0) return false;

        if (NextAction == null) return false;

        // Skip when casting
        if (Player.Object.TotalCastTime - DataCenter.ActionAhead > 0) return false;

        // GCD
        return RSCommands.CanDoAnAction(ActionHelper.CanUseGCD);
    }

    private unsafe static bool IsPlayerOccupied()
    {
        if (Svc.Condition[ConditionFlag.OccupiedInQuestEvent]
            || Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent]
            || Svc.Condition[ConditionFlag.Occupied33]
            || Svc.Condition[ConditionFlag.Occupied38]
            || Svc.Condition[ConditionFlag.Jumping61]
            || Svc.Condition[ConditionFlag.BetweenAreas]
            || Svc.Condition[ConditionFlag.BetweenAreas51]
            || Svc.Condition[ConditionFlag.Mounted]
            || Svc.Condition[ConditionFlag.SufferingStatusAffliction2]
            || Svc.Condition[ConditionFlag.RolePlaying]
            || Svc.Condition[ConditionFlag.InFlight]
            || Svc.Condition[ConditionFlag.Diving]
            || Svc.Condition[ConditionFlag.Swimming]
            || Svc.Condition[ConditionFlag.Unconscious]
            || Svc.Condition[ConditionFlag.MeldingMateria])
        {
            return true;
        }

        if (ActionManager.Instance()->ActionQueued && NextAction != null
            && ActionManager.Instance()->QueuedActionId != NextAction.AdjustedID)
        {
            return true;
        }

        return false;
    }

    private static void LogError(string message, Exception ex)
    {
#pragma warning disable 0436

        WarningHelper.AddSystemWarning($"{message} because: {ex.Message}");
        Svc.Log.Error(ex, message);
    }
}