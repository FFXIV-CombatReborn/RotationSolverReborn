using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using AttackType = RotationSolver.Engine.Data.AttackType;
using Player = ECommons.GameHelpers.Player;

namespace RotationSolver.Engine.Actions;

/// <summary>
/// The target info
/// </summary>
/// <param name="action">the input action.</param>
public partial struct ActionTargetInfo(IBaseAction action)
{
	/// <summary>
	/// The range of this action.
	/// </summary>
	public readonly float Range => ActionBasicInfo.IsInstinctualAxe(action.Info.ID) ? 3f : ActionManager.GetActionRange(action.Info.ID);

	/// <summary>
	/// The effect range of this action.
	/// </summary>
	public readonly float EffectRange => (ActionID)action.Info.ID == ActionID.LiturgyOfTheBellPvE ? 20 : action.Action.EffectRange;

	/// <summary>
	/// Is this action single target.
	/// </summary>
	public readonly bool IsSingleTarget => action.Action.CastType == 1;
	/// <summary>
	/// Is this action target area.
	/// </summary>
	public readonly bool IsTargetArea => action.Action.TargetArea;

	/// <summary>
	/// Is this action friendly.
	/// </summary>
	public readonly bool IsTargetFriendly => action.Setting.IsFriendly;

	#region Targetting Behaviour

	/// <summary>
	/// Retrieves a collection of valid battle characters that can be targeted based on the specified criteria.
	/// </summary>
	/// <param name="skipStatusProvideCheck">If set to <c>true</c>, skips the status provide check.</param>
	/// <param name="skipTargetStatusNeedCheck">If set to <c>true</c>, skips the target status need check.</param>
	/// <param name="type">The type of target to filter (e.g., Heal).</param>
	/// <param name="targetOverride">Overrides the default target type for the action.</param>
	/// <returns>
	/// A <see cref="List{IBattleChara}"/> containing the valid targets.
	/// </returns>
	private readonly List<IBattleChara> GetCanTargets(bool skipStatusProvideCheck, bool skipTargetStatusNeedCheck, TargetType type, TargetType targetOverride)
	{
		if (DataCenter.AllTargets == null)
		{
			return [];
		}

		if (Player.Object == null)
		{
			return [];
		}

		if (targetOverride == TargetType.Self)
		{
			return [Player.Object];
		}

		var range = Range;
		var config = action.Config;
		var restrictedDot = config.IsRestrictedDOT;
		var timeToKill = config.TimeToKill;

		List<IBattleChara> validTargets = [];
		foreach (var target in TargetHelper.GetTargetsByRange(range, action.Setting.IsFriendly))
		{
			if (type == TargetType.Heal && target.GetHealthRatio() >= 1)
			{
				continue;
			}

			if (restrictedDot && DataCenter.RestrictedDotNameIds.Contains(target.NameId))
			{
				continue;
			}

			if (!IsInPvPPlayerRange(target))
			{
				continue;
			}

			if (!CheckStatus(target, skipStatusProvideCheck, skipTargetStatusNeedCheck) || !CheckTimeToKill(target, timeToKill) || !CheckResistance(target, range))
			{
				continue;
			}

			if (!DataCenter.IsManual || IsTargetFriendly || target.GameObjectId == Svc.Targets.Target?.GameObjectId || target.GameObjectId == Player.Object.GameObjectId)
			{
				if ((IsTargetFriendly || TargetOnScreen(target)) && CanUseTo(target) && action.Setting.CanTarget(target)
					&& (!config.MinHPFeature || target.GetHealthRatio() > config.MinHPPercent))
				{
					validTargets.Add(target);
				}
			}
		}

		return validTargets;
	}

	/// <summary>
	/// Retrieves a list of battle characters that can be affected based on the specified criteria.
	/// </summary>
	/// <param name="skipStatusProvideCheck">If set to <c>true</c>, skips the status provide check.</param>
	/// <param name="skipTargetStatusNeedCheck">If set to <c>true</c>, skips the target status need check.</param>
	/// <param name="type">The type of target to filter (e.g., Heal).</param>
	/// <param name="targetOverride">Overrides the default target type for the action.</param>
	/// <returns>
	/// A <see cref="List{IBattleChara}"/> containing the valid targets.
	/// </returns>
	private readonly List<IBattleChara> GetCanAffects(bool skipStatusProvideCheck, bool skipTargetStatusNeedCheck, TargetType type, TargetType targetOverride)
	{
		if (EffectRange == 0)
		{
			return [];
		}

		if (Player.Object == null)
		{
			return [];
		}

		if ((action.Setting.IsFriendly
			? DataCenter.PartyMembers
			: DataCenter.AllHostileTargets) == null)
		{
			return [];
		}

		if (targetOverride == TargetType.Self)
		{
			return [Player.Object];
		}

		var range = Range;
		var config = action.Config;
		var restrictedDot = config.IsRestrictedDOT;
		var timeToKill = config.TimeToKill;
		var items = TargetHelper.GetTargetsByRange(range + EffectRange, action.Setting.IsFriendly);

		List<IBattleChara> validTargets = [];
		foreach (var tar in items)
		{
			if (type == TargetType.Heal && tar.GetHealthRatio() >= 1)
			{
				continue;
			}

			if ((restrictedDot && DataCenter.RestrictedDotNameIds.Contains(tar.NameId))
				|| (DataCenter.IsInM9S && action.AdjustedID == 36982 && DataCenter.RestrictedActionNameIds.Contains(tar.NameId)))
			{
				continue;
			}

			if (CheckStatus(tar, skipStatusProvideCheck, skipTargetStatusNeedCheck) && CheckTimeToKill(tar, timeToKill) && CheckResistance(tar, range))
			{
				validTargets.Add(tar);
			}
		}
		return validTargets;
	}

	private static readonly Dictionary<ulong, (long Tick, bool OnScreen)> _onScreenCache = [];

	private static bool TargetOnScreen(IBattleChara battleChara)
	{
		if (!Service.Config.OnlyAttackInView && !Service.Config.OnlyAttackInVisionCone)
		{
			return true;
		}

		var now = Environment.TickCount64;
		var id = battleChara.GameObjectId;
		if (_onScreenCache.TryGetValue(id, out var cached) && now - cached.Tick < 15)
		{
			return cached.OnScreen;
		}

		if (_onScreenCache.Count > 512)
		{
			_onScreenCache.Clear();
		}

		var onScreen = IsOnScreen(battleChara);
		_onScreenCache[id] = (now, onScreen);
		return onScreen;
	}

	private static bool IsOnScreen(IBattleChara battleChara)
	{
		if (Service.Config.OnlyAttackInView)
		{
			if (!Svc.GameGui.WorldToScreen(battleChara.Position, out _))
			{
				return false;
			}
		}

		if (Service.Config.OnlyAttackInVisionCone && Player.Object != null)
		{
			var dir = battleChara.Position - Player.Object.Position;
			var faceVec = Player.Object.GetFaceVector();
			dir = Vector3.Normalize(dir);
			faceVec = Vector3.Normalize(faceVec);

			double dot = Vector3.Dot(faceVec, dir);
			var thresholdCos = Math.Cos(Math.PI * Service.Config.AngleOfVisionCone / 360);
			if (dot < thresholdCos)
			{
				return false;
			}
		}

		return true;
	}

	private readonly unsafe bool CanUseTo(IBattleChara tar)
	{
		if (tar == null)
		{
			return false;
		}

		if (Player.Object == null)
		{
			return false;
		}

		if (!Player.Available)
		{
			return false;
		}

		if (!(tar.GameObjectId != 0 && tar.Struct() != null))
		{
			return false;
		}

		var specialAbilityCheck = IsSpecialAbility(action.Info.ID);
		var actionmanagerCheck = ActionManager.CanUseActionOnTarget(action.Info.AdjustedID, (GameObject*)tar.Struct());

		if (!(specialAbilityCheck || actionmanagerCheck))
		{
			return false;
		}
		if (!IsTargetFriendly)
		{
			return tar.GameObjectId == Player.Object.GameObjectId || DataCenter.AllHostileTargetIds.Contains(tar.GameObjectId);
		}
		else
		{
			return tar.CanSee();
		}
	}

	private readonly bool IsInPvPPlayerRange(IBattleChara tar)
	{
		if (!DataCenter.IsPvP || !IsSingleTarget || tar is not IPlayerCharacter
			|| Player.Object == null || tar.GameObjectId == Player.Object.GameObjectId)
		{
			return true;
		}

		return tar.DistanceToPlayer() + Player.Object.HitboxRadius <= Range;
	}

	private static readonly HashSet<ActionID> _specialActions =
	[
		ActionID.AethericMimicryPvE,
		ActionID.EruptionPvE,
		ActionID.BishopAutoturretPvP,
		ActionID.CometPvP,
		ActionID.DotonPvE,
		ActionID.DotonPvE_18880,
		ActionID.FeatherRainPvE,
		ActionID.SaltAndDarknessPvP,
		ActionID.AvalancheAxePvE,
		ActionID.SpinningAxePvE,
		ActionID.MistralAxePvE,
		ActionID.GaleAxePvE,
	];

	private static bool IsSpecialAbility(uint iD)
	{
		return _specialActions.Contains((ActionID)iD);
	}

	private readonly bool CheckStatus(IBattleChara battleChara, bool skipStatusProvideCheck, bool skipTargetStatusNeedCheck)
	{
		if (battleChara == null)
		{
			return false;
		}

		if (action.Setting.TargetStatusNeed != null && !skipTargetStatusNeedCheck && action.Config.ShouldCheckTargetStatus)
		{
			if (DataCenter.IsInOccultCrescentOp && battleChara.NameId != 0)
			{
				foreach (var id in action.Setting.TargetStatusNeed)
				{
					if (StatusHelper.IsOccultWeaknessStatus(id) && StatusHelper.HasKnownOccultWeakness(battleChara.NameId, id))
					{
						return true;
					}
				}
			}

			if (battleChara.WillStatusEndGCD(0, 0, action.Setting.StatusFromSelf, action.Setting.TargetStatusNeed))
			{
				return false;
			}
		}

		if (action.Setting.TargetStatusProvide != null && !skipStatusProvideCheck && action.Config.ShouldCheckStatus)
		{
			if (!NeedsTargetStatus(battleChara) || (Service.Config.Statuscap2 && StatusHelper.IsStatusCapped(battleChara)))
			{
				return false;
			}
		}

		return true;
	}

	private readonly bool NeedsTargetStatus(IBattleChara battleChara)
	{
		var setting = action.Setting;
		return Service.Config.UseDotTickTiming && setting.Dot != null
			? DotPlanner.ShouldApply(action, battleChara)
			: battleChara.WillStatusEndGCD(action.Config.StatusRefreshGcdCount, 0, setting.StatusFromSelf, setting.TargetStatusProvide);
	}

	private readonly bool CheckResistance(IBattleChara battleChara, float range)
	{
		if (battleChara == null)
		{
			return false;
		}

		if (DataCenter.Job == Job.BLU && DataCenter.IsInMaskedCarnivale)
		{
			if (Service.Config.BlockflatdamagedeathimmuneBlu && action.Setting.IsFlatDamageDeath
			&& !MaskedCarnivaleHelper.IsVulnerableToFlatOrDeath(battleChara))
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Debug($"[CheckResistance] {battleChara.Name} is not vulnerable to flat/death — skipping {action.Info.Name}.");
				}

				return false;
			}

			if (Service.Config.BlockslowimmuneBlu && action.Setting.IsSlowSpell
			&& !MaskedCarnivaleHelper.IsVulnerableToSlow(battleChara))
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Debug($"[CheckResistance] {battleChara.Name} is not vulnerable to slow — skipping {action.Info.Name}.");
				}

				return false;
			}

			if (Service.Config.BlockpetrificationimmuneBlu && action.Setting.IsPetrificationSpell
				&& !MaskedCarnivaleHelper.IsVulnerableToPetrification(battleChara))
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Debug($"[CheckResistance] {battleChara.Name} is not vulnerable to petrification — skipping {action.Info.Name}.");
				}

				return false;
			}

			if (Service.Config.BlockparalysisimmuneBlu && action.Setting.IsParalysisSpell
				&& !MaskedCarnivaleHelper.IsVulnerableToParalysis(battleChara))
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Debug($"[CheckResistance] {battleChara.Name} is not vulnerable to paralysis — skipping {action.Info.Name}.");
				}

				return false;
			}

			if (Service.Config.BlockinterruptimmuneBlu && action.Setting.IsInterruptSpell
			&& !MaskedCarnivaleHelper.IsVulnerableToInterruption(battleChara))
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Debug($"[CheckResistance] {battleChara.Name} is not vulnerable to interruption — skipping {action.Info.Name}.");
				}

				return false;
			}

			if (Service.Config.BlockblindimmuneBlu && action.Setting.IsBlindSpell
				&& !MaskedCarnivaleHelper.IsVulnerableToBlind(battleChara))
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Debug($"[CheckResistance] {battleChara.Name} is not vulnerable to blind — skipping {action.Info.Name}.");
				}

				return false;
			}

			if (Service.Config.BlockstunimmuneBlu && action.Setting.IsStunSpell
				&& !MaskedCarnivaleHelper.IsVulnerableToStun(battleChara))
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Debug($"[CheckResistance] {battleChara.Name} is not vulnerable to stun — skipping {action.Info.Name}.");
				}

				return false;
			}

			if (Service.Config.BlocksleepimmuneBlu && action.Setting.IsSleepSpell
				&& !MaskedCarnivaleHelper.IsVulnerableToSleep(battleChara))
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Debug($"[CheckResistance] {battleChara.Name} is not vulnerable to sleep — skipping {action.Info.Name}.");
				}

				return false;
			}

			if (Service.Config.BlockbindimmuneBlu && action.Setting.IsBindSpell
				&& !MaskedCarnivaleHelper.IsVulnerableToBind(battleChara))
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Debug($"[CheckResistance] {battleChara.Name} is not vulnerable to bind — skipping {action.Info.Name}.");
				}

				return false;
			}

			if (Service.Config.BlockheavyimmuneBlu && action.Setting.IsHeavySpell
				&& !MaskedCarnivaleHelper.IsVulnerableToHeavy(battleChara))
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Debug($"[CheckResistance] {battleChara.Name} is not vulnerable to heavy — skipping {action.Info.Name}.");
				}

				return false;
			}

			if (Service.Config.BlockimmuneBlu && !MaskedCarnivaleHelper.IsActionAspectAllowed(battleChara, action))
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Debug($"[CheckResistance] {battleChara.Name} is immune to action {action.Info.Name} aspect (BlockimmuneBLU).");
				}

				return false;
			}

			if (Service.Config.BlocknonweakBlu && !MaskedCarnivaleHelper.HasNoResistancesOrImmunities(battleChara))
			{

				var shouldCheckWeakness = false;
				if (action.Info.Aspects != null)
				{
					for (var i = 0; i < action.Info.Aspects.Count; i++)
					{
						if ((action.Info.Aspects[i] != Aspect.Unaspected && Service.Config.AllowunaspectedBlu) || !Service.Config.AllowunaspectedBlu)
						{
							shouldCheckWeakness = true;
							break;
						}
					}
				}
				if (shouldCheckWeakness && !MaskedCarnivaleHelper.IsActionAspectWeak(battleChara, action))
				{
					return false;
				}
			}
		}

		try
		{
			if (DataCenter.IsPvP && !action.Setting.IgnoreGuard)
			{
				if (battleChara.HasStatus(false, StatusID.Guard) && !battleChara.WillStatusEnd((float)action.Info.CastTime, false, StatusID.Guard))
				{
					return false;
				}
			}
			if (action.Info.HasAttackType(AttackType.Magic))
			{
				if (battleChara.HasStatus(false, StatusHelper.MagicResistance))
				{
					return false;
				}
			}
			if (action.Info.HasAttackType(AttackType.Physical) || action.Info.HasAttackType(AttackType.Slashing) || action.Info.HasAttackType(AttackType.Piercing) || action.Info.HasAttackType(AttackType.Blunt)) // Physical
			{
				if (battleChara.HasStatus(false, StatusHelper.PhysicalResistance))
				{
					return false;
				}
			}
			if (range >= 20) // Range
			{
				if (battleChara.HasStatus(false, StatusID.EnergyField))
				{
					return false;
				}
			}
		}
		catch (Exception ex)
		{
			PluginLog.Error($"Error checking resistance: {ex.Message}");
			return false;
		}

		return true;
	}

	private static bool CheckTimeToKill(IBattleChara battleChara, float timeToKill)
	{
		if (battleChara == null)
		{
			return false;
		}

		if (timeToKill <= 0)
		{
			return true;
		}

		var time = battleChara.GetTTK();
		return float.IsNaN(time) || time >= timeToKill;
	}

	#endregion
}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public enum TargetType : byte
{
	Big,
	Small,
	HighHP,
	LowHP,
	HighMaxHP,
	LowMaxHP,
	Interrupt,
	Provoke,
	Death,
	Dispel,
	Move,
	FriendMove,
	BeAttacked,
	Heal,
	Tank,
	Melee,
	Range,
	Physical,
	Magical,
	Self,
	DancePartner,
	MimicryTarget,
	TheBalance,
	TheSpear,
	Kardia,
	Deployment,
	PhantomBell,
	PhantomRespite,
	DarkCannon,
	ShockCannon,
	Farthest,
	Nearest,
	PvPHealers,
	PvPTanks,
	PvPDPS,
	HighHPPercent,
	LowHPPercent,
	Tankbuster,
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// The target result
/// </summary>
/// <param name="Target">the target.</param>
/// <param name="AffectedTargets">the targets that be affected by this action.</param>
/// <param name="Position">the position to use this action.</param>
public readonly record struct TargetResult(IBattleChara Target, IBattleChara[] AffectedTargets, Vector3? Position);
