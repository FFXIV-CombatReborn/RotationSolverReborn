using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;
using ECommons.EzIpcManager;
using ECommons.GameHelpers;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using RotationSolver.Commands;

namespace RotationSolver.Updaters;

internal static class ActionUpdater
{
    internal static DateTime AutoCancelTime { get; set; } = DateTime.MinValue;

    static ActionUpdater()
    {
        _ = EzIPC.Init(typeof(ActionUpdater), "RotationSolverReborn.ActionUpdater");
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
		IPlayerCharacter? localPlayer = Player.Object;
		ICustomRotation? customRotation = DataCenter.CurrentRotation;

        try
        {
            if (localPlayer != null && customRotation != null
                && customRotation.TryInvoke(out IAction? newAction, out IAction? gcdAction))
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

    internal static unsafe void UpdateCombatInfo()
    {
        var now = DateTime.Now;
        SetAction(NextGCDAction?.AdjustedID ?? 0);
        UpdateCombatTime(now);
        UpdateSlots();
        UpdateMoving(now);
        UpdateLifetime(now);
        UpdateMPTimer(now);
    }

    private static uint actionOverride = 0;
    private static void SetAction(uint id)
    {
        actionOverride = id;
    }

    private static DateTime _startCombatTime = DateTime.MinValue;
    private static void UpdateCombatTime(DateTime now)
    {
        bool lastInCombat = DataCenter.InCombat;
        DataCenter.InCombat = Svc.Condition[ConditionFlag.InCombat];

        if (!lastInCombat && DataCenter.InCombat)
        {
            _startCombatTime = now;
        }
        else if (lastInCombat && !DataCenter.InCombat)
        {
            _startCombatTime = DateTime.MinValue;

            if (Service.Config.AutoOffAfterCombat && !DataCenter.IsHenched && !DataCenter.IsAutoDuty)
            {
                AutoCancelTime = now.AddSeconds(Service.Config.AutoOffAfterCombatTime);
            }
        }

        DataCenter.CombatTimeRaw = _startCombatTime == DateTime.MinValue
            ? 0
            : (float)(now - _startCombatTime).TotalSeconds;
    }

    private static unsafe void UpdateSlots()
    {
        ActionManager* actionManager = ActionManager.Instance();
        if (actionManager == null)
        {
            return;
        }

        for (int i = 0; i < DataCenter.BluSlots.Length; i++)
        {
            DataCenter.BluSlots[i] = actionManager->GetActiveBlueMageActionInSlot(i);
        }
        for (ushort i = 0; i < DataCenter.DutyActions.Length; i++)
        {
            DataCenter.DutyActions[i] = DutyActionManager.GetDutyActionId(i);
        }
    }

    private static bool _lastIsMoving = false;
    private static DateTime _startMovingTime = DateTime.MinValue;
    private static DateTime _stopMovingTime = DateTime.MinValue;
    private static void UpdateMoving(DateTime now)
    {
        if (_lastIsMoving && !DataCenter.IsMoving)
        {
            _stopMovingTime = now;
        }
        else if (DataCenter.IsMoving && !_lastIsMoving)
        {
            _startMovingTime = now;
        }

        DataCenter.StopMovingRaw = DataCenter.IsMoving
            ? 0
            : Math.Min(10, (float)(now - _stopMovingTime).TotalSeconds);

        DataCenter.MovingRaw = DataCenter.IsMoving
            ? Math.Min(10, (float)(now - _startMovingTime).TotalSeconds)
            : 0;

        _lastIsMoving = DataCenter.IsMoving;
    }

    private static DateTime _startDeadTime = DateTime.MinValue;
    private static DateTime _startAliveTime = DateTime.Now;
    private static bool _isDead = true;
    public static void UpdateLifetime(DateTime now)
    {
        var player = Player.Object;
        if (player == null)
        {
            DataCenter.DeadTimeRaw = 0;
            DataCenter.AliveTimeRaw = 0;
            return;
        }

        bool lastDead = _isDead;
        _isDead = player.IsDead;

        if (Svc.Condition[ConditionFlag.BetweenAreas])
        {
            _startAliveTime = now;
        }
        switch (lastDead)
        {
            case true when !_isDead:
                _startAliveTime = now;
                break;
            case false when _isDead:
                _startDeadTime = now;
                break;
        }

        DataCenter.DeadTimeRaw = _isDead
            ? Math.Min(10, (float)(now - _startDeadTime).TotalSeconds)
            : 0;

        DataCenter.AliveTimeRaw = _isDead
            ? 0
            : Math.Min(10, (float)(now - _startAliveTime).TotalSeconds);
    }

    private static uint _lastMP = 0;
    private static DateTime _lastMPUpdate = DateTime.Now;

    internal static float MPUpdateElapsed => (float)((DateTime.Now - _lastMPUpdate).TotalSeconds % 3);

    private static void UpdateMPTimer(DateTime now)
    {
        if (Player.Object == null)
        {
            return;
        }

        if (Player.Object.ClassJob.RowId != (uint)ECommons.ExcelServices.Job.BLM)
        {
            return;
        }

        if (StatusHelper.PlayerHasStatus(true, StatusID.LucidDreaming))
        {
            return;
        }

        if (_lastMP < Player.Object.CurrentMp)
        {
            _lastMPUpdate = now;
        }
        _lastMP = Player.Object.CurrentMp;
    }

    internal static unsafe bool CanDoAction()
    {
        // In Target-Only mode we never perform actions.
        if (DataCenter.IsTargetOnly)
        {
            return false;
        }

        var player = Player.Object;
        if (player == null || IsPlayerOccupied() || player.CurrentHp == 0)
        {
            return false;
        }

        if (PlayerHasLockActions())
        {
            return false;
        }

        if (NextAction == null)
        {
            return false;
        }

        // Skip when casting
        if (player.TotalCastTime - DataCenter.CalculatedActionAhead > 0)
        {
            return false;
        }

        // GCD
        return RSCommands.CanDoAnAction(ActionHelper.CanUseGCD);
    }

    internal static bool PlayerHasLockActions()
    {
        if (Player.Object == null)
        {
            return false;
        }

        if (Player.Object.StatusList == null)
        {
            return false;
        }

        foreach (var status in Player.Object.StatusList)
        {
            if (status != null && status != null && status.GameData.Value.LockActions == true && status.RemainingTime > 1 + DataCenter.DefaultGCDRemain)
                return true;
        }
        return false;
    }

    private unsafe static bool IsPlayerOccupied()
    {
        if (Svc.Objects.LocalPlayer?.IsTargetable != true)
        {
            return true;
        }

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
            || Svc.Condition[ConditionFlag.InThisState89] // frog state in Tower of Babil
            || Svc.Condition[ConditionFlag.MeldingMateria])
        {
            return true;
        }

        ActionManager* am = ActionManager.Instance();
        if (am != null
            && am->ActionQueued
            && NextAction != null
            && am->QueuedActionId != NextAction.AdjustedID)
        {
            return true;
        }

        return false;
    }

    private static void LogError(string message, Exception ex)
    {
        BasicWarningHelper.AddSystemWarning($"{message} because: {ex.Message}");
        PluginLog.Error($"{message} because: {ex.Message}");
    }
}