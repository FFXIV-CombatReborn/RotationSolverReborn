using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using RotationSolver.Basic.Helpers;
using Action = Lumina.Excel.Sheets.Action;

namespace RotationSolver.Basic.Actions;

/// <summary>
/// The base action for all actions.
/// </summary>
public class BaseAction : IBaseAction
{
	/// <summary>
	/// Gets or sets the target to use for the action.
	/// </summary>
	/// <value>
	/// A <see cref="TargetResult"/> representing the target of the action.
	/// </value>
	public TargetResult Target { get; set; } = Player.Object is IBattleChara bc
	? new(bc, [], null)
	: default;

	/// <summary>
	/// Gets the target for preview purposes.
	/// </summary>
	/// <value>
	/// A nullable <see cref="TargetResult"/> representing the preview target, or <c>null</c> if no preview target is available.
	/// </value>
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
    public static float AnimationLockTime => Player.AnimationLock;

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
    public bool IsIntercepted
    {
        get => Config.IsIntercepted;
        set => Config.IsIntercepted = value;
    }

    /// <summary>
    /// 
    /// </summary>
    public bool IsRestrictedDOT
    {
        get => Config.IsRestrictedDOT;
        set => Config.IsRestrictedDOT = value;
    }

    /// <inheritdoc/>
    public bool IsOnCooldownWindow
    {
        get => Config.IsOnCooldownWindow;
        set => Config.IsOnCooldownWindow = value;
    }

    /// <inheritdoc/>
    public bool MinHPFeature
    {
        get => Config.MinHPFeature;
        set => Config.MinHPFeature = value;
    }

    /// <inheritdoc/>
    public float MinHPPercent
    {
        get => Config.MinHPPercent;
        set => Config.MinHPPercent = value;
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
            if (!Service.Config.RotationActionConfig.TryGetValue(ID, out ActionConfig? value) || DataCenter.ResetActionConfigs)
            {
                value = Setting.CreateConfig?.Invoke() ?? new ActionConfig();
                Service.Config.RotationActionConfig[ID] = value;

                if (!Action.ClassJob.IsValid)
                {
                    // Log the error for debugging purposes
                    PluginLog.Debug($"ClassJob is not valid for Action ID: {ID}");
                    return value;
                }

                _ = Action.ClassJob.Value;

                if (Setting.TargetStatusProvide != null)
                {
                    value.TimeToKill = 0;
                }
            }

            // One-time AoE count reset: force AOE Count to rotation default (or global default),
            // without touching other user-configured fields.
            if (!value.AoeResetDone)
            {
                byte defaultAoe = (Setting.CreateConfig?.Invoke()?.AoeCount) ?? new ActionConfig().AoeCount;
                value.AoeCount = defaultAoe;
                value.AoeResetDone = true;

                Service.Config.RotationActionConfig[ID] = value;
                // Optionally persist immediately:
                // Service.Config.Save();
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
    public bool CanUse(out IAction act, bool skipStatusProvideCheck = false, bool skipStatusNeed = false, bool skipTargetStatusNeedCheck = false, bool skipComboCheck = false, bool skipCastingCheck = false,
    bool usedUp = false, bool skipAoeCheck = false, bool skipTTKCheck = false, byte gcdCountForAbility = 0, bool checkActionManagerDirectly = false, TargetType targetOverride = default)
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

        if (!Info.BasicCheck(skipStatusProvideCheck, skipStatusNeed, skipComboCheck, skipCastingCheck, checkActionManagerDirectly, targetOverride))
        {
            return false;
        }

        if (!Cooldown.CooldownCheck(usedUp, gcdCountForAbility))
        {
            return false;
        }

        if (Setting.SpecialType == SpecialActionType.MeleeRange && IActionHelper.IsLastAction(IActionHelper.MovingActions))
        {
            return false; // No range actions after moving.
        }

        if (!skipTTKCheck)
        {
            if (!DataCenter.IsPvP || (DataCenter.IsPvP && !Service.Config.IgnorePvPttk))
            {
                if (!IsTimeToKillValid())
                {
                    return false;
                }
            }
        }
        PreviewTarget = TargetInfo.FindTarget(skipAoeCheck, skipStatusProvideCheck, skipTargetStatusNeedCheck, targetOverride);
        if (PreviewTarget == null)
        {
            return false;
        }

        if (!IBaseAction.ActionPreview)
        {
            Target = PreviewTarget.Value;
        }

        return true;
    }

    private bool IsTimeToKillValid()
    {
        return DataCenter.AverageTTK >= Config.TimeToKill;
    }

    /// <inheritdoc/>
    public unsafe bool Use()
    {
        if (Player.Object == null) return false;

        TargetResult target = Target;
        uint adjustId = AdjustedID;

        if (TargetInfo.IsTargetArea)
        {
            if (adjustId != ID || !target.Position.HasValue)
                return false;

            Vector3 loc = target.Position.Value;

            // Use ActionManagerEx for enhanced timing if tweaks are enabled
            if (Service.Config.RemoveAnimationLockDelay || Service.Config.RemoveCooldownDelay)
            {
                return ActionManagerEx.Instance.UseActionLocationWithTweaks(ActionType.Action, ID, Player.Object.GameObjectId, &loc);
            }
            else
            {
                var actionManager = ActionManager.Instance();
                return actionManager != null &&
                       actionManager->UseActionLocation(ActionType.Action, ID, Player.Object.GameObjectId, &loc);
            }
        }
        else
        {
            ulong targetId = target.Target?.GameObjectId ?? Player.Object.GameObjectId;

            if (targetId == 0 || Svc.Objects.SearchById(targetId) == null)
                return false;

            // Use ActionManagerEx for enhanced timing if tweaks are enabled
            if (Service.Config.RemoveAnimationLockDelay || Service.Config.RemoveCooldownDelay)
            {
                return ActionManagerEx.Instance.UseActionWithTweaks(ActionType.Action, adjustId, targetId);
            }
            else
            {
                var actionManager = ActionManager.Instance();
                return actionManager != null &&
                       actionManager->UseAction(ActionType.Action, adjustId, targetId);
            }
        }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Name;
    }
}