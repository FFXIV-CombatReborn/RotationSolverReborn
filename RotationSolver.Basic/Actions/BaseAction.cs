using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Action = Lumina.Excel.Sheets.Action;

namespace RotationSolver.Basic.Actions;

/// <summary>
/// The base action for all actions.
/// </summary>
public class BaseAction : IBaseAction
{
    /// <inheritdoc/>
    public TargetResult Target { get; set; } = new(Player.Object, [], null);

    /// <inheritdoc/>
    public TargetResult? PreviewTarget { get; private set; } = null;

    /// <inheritdoc/>
    public Action Action { get; }

    /// <inheritdoc/>
    public ActionTargetInfo TargetInfo { get; }

    /// <inheritdoc/>
    public ActionBasicInfo Info { get; }

    /// <inheritdoc/>
    public ActionCooldownInfo Cooldown { get; }

    ICooldown IAction.Cooldown => Cooldown;

    /// <inheritdoc/>
    public uint ID => Info.ID;

    /// <inheritdoc/>
    public uint AdjustedID => Info.AdjustedID;

    /// <inheritdoc/>
    public float AnimationLockTime => ActionManagerHelper.GetCurrentAnimationLock();

    /// <inheritdoc/>
    public uint SortKey => Cooldown.CoolDownGroup;

    /// <inheritdoc/>
    public uint IconID => Info.IconID;

    /// <inheritdoc/>
    public string Name => Info.Name;

    /// <inheritdoc/>
    public string Description => string.Empty;

    /// <inheritdoc/>
    public byte Level => Info.Level;

    /// <inheritdoc/>
    public bool IsEnabled
    {
        get => Config.IsEnabled;
        set => Config.IsEnabled = value;
    }

    /// <inheritdoc/>
    public bool IsInCooldown
    {
        get => Config.IsInCooldown;
        set => Config.IsInCooldown = value;
    }

    /// <inheritdoc/>
    public bool EnoughLevel => Info.EnoughLevel;

    /// <inheritdoc/>
    public ActionSetting Setting { get; set; }

    /// <inheritdoc/>
    public ActionConfig Config
    {
        get
        {
            if (!Service.Config.RotationActionConfig.TryGetValue(ID, out var value) || DataCenter.ResetActionConfigs)
            {
                value = Setting.CreateConfig?.Invoke() ?? new ActionConfig();
                Service.Config.RotationActionConfig[ID] = value;

                if (!Action.ClassJob.IsValid)
                {
                    // Log the error for debugging purposes
                    Svc.Log.Debug($"ClassJob is not valid for Action ID: {ID}");
                    return value;
                }

                var classJob = Action.ClassJob.Value;

                if (value.TimeToUntargetable == 0)
                {
                    value.TimeToUntargetable = value.TimeToKill;
                }

                if (Setting.TargetStatusProvide != null)
                {
                    value.TimeToKill = 0;
                }
            }
            return value;
        }
    }

    /// <summary>
    /// The default constructor
    /// </summary>
    /// <param name="actionID">action id</param>
    /// <param name="isDutyAction">is this action a duty action</param>
    public BaseAction(ActionID actionID, bool isDutyAction = false)
    {
        Action = Service.GetSheet<Action>().GetRow((uint)actionID);
        TargetInfo = new(this);
        Info = new(this, isDutyAction);
        Cooldown = new(this);

        Setting = new();
    }

    /// <inheritdoc/>
    public bool CanUse(out IAction act, bool isLastAbility = false, bool isFirstAbility = false, bool skipStatusProvideCheck = false, bool skipComboCheck = false, bool skipCastingCheck = false,
    bool usedUp = false, bool skipAoeCheck = false, bool skipTTKCheck = false, byte gcdCountForAbility = 0)
    {
        act = this;

        if (IBaseAction.ActionPreview)
        {
            skipCastingCheck = true;
        }
        else
        {
            Setting.EndSpecial = IBaseAction.ShouldEndSpecial;
        }

        if (IBaseAction.AllEmpty)
        {
            usedUp = true;
        }

        if (!Info.BasicCheck(skipStatusProvideCheck, skipComboCheck, skipCastingCheck)) return false;

        if (!Cooldown.CooldownCheck(usedUp, gcdCountForAbility)) return false;

        if (Setting.SpecialType == SpecialActionType.MeleeRange && IActionHelper.IsLastAction(IActionHelper.MovingActions)) return false; // No range actions after moving.

        if (!skipTTKCheck)
        {
            switch (DataCenter.IsPvP)
            {
                case true when Service.Config.IgnorePvPttk:
                    break;
                case false when !IsTimeToKillValid():
                    return false;
            }
        }

        PreviewTarget = TargetInfo.FindTarget(skipAoeCheck, skipStatusProvideCheck);
        if (PreviewTarget == null) return false;

        if (!IBaseAction.ActionPreview)
        {
            Target = PreviewTarget.Value;
        }

        return true;
    }

    private bool IsTimeToKillValid()
    {
        return DataCenter.AverageTTK >= Config.TimeToKill && DataCenter.AverageTTK >= Config.TimeToUntargetable;
    }


    /// <inheritdoc/>
    public unsafe bool Use()
    {
        var target = Target;

        var adjustId = AdjustedID;
        if (TargetInfo.IsTargetArea)
        {
            if (adjustId != ID) return false;
            if (!target.Position.HasValue) return false;

            var loc = target.Position.Value;

            if (Player.Object == null || ActionManager.Instance() == null)
            {
                return false;
            }

            return ActionManager.Instance()->UseActionLocation(ActionType.Action, ID, Player.Object.GameObjectId, &loc);
        }
        else
        {
            var targetId = target.Target?.GameObjectId ?? Player.Object?.GameObjectId ?? 0;
            if (Svc.Objects.SearchById(targetId) == null)
            {
                return false;
            }

            if (ActionManager.Instance() == null)
            {
                return false;
            }

            return ActionManager.Instance()->UseAction(ActionType.Action, adjustId, targetId);
        }
    }

    /// <inheritdoc/>
    public override string ToString() => Name;
}
