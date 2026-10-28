using Dalamud.Hooking;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using Lumina.Excel.Sheets;
using RotationSolver.ActionTimeline;
using RotationSolver.Basic.Configuration;
using System.Text.RegularExpressions;

namespace RotationSolver;

public static class Watcher
{
	private const uint CancelAbilityCategory = 15;
	private const uint EnemyDotTickCategory = 0x605;

	private delegate void ActorControlDelegate(uint entityId, uint category, uint p1, uint p2, uint p3, uint p4, uint p5, uint p6, uint p7, uint p8, ulong targetId, byte replaying);
	private static Hook<ActorControlDelegate>? _actorControlHook;

	public static void Enable()
	{
		ActionEffect.ActionEffectEvent += ActionFromEnemy;
		ActionEffect.ActionEffectEvent += ActionFromSelf;

		try
		{
			_actorControlHook = Svc.Hook.HookFromSignature<ActorControlDelegate>("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", ActorControlDetour);
			_actorControlHook.Enable();
		}
		catch (Exception ex)
		{
			PluginLog.Error($"[Watcher] Failed to hook ActorControl: {ex}");
		}
	}

	public static void Disable()
	{
		ActionEffect.ActionEffectEvent -= ActionFromEnemy;
		ActionEffect.ActionEffectEvent -= ActionFromSelf;

		_actorControlHook?.Dispose();
		_actorControlHook = null;
	}

	private static void ActorControlDetour(uint entityId, uint category, uint p1, uint p2, uint p3, uint p4, uint p5, uint p6, uint p7, uint p8, ulong targetId, byte replaying)
	{
		_actorControlHook!.Original(entityId, category, p1, p2, p3, p4, p5, p6, p7, p8, targetId, replaying);

		try
		{
			if (category == EnemyDotTickCategory)
			{
				DotTickTracker.RecordTick(entityId, Environment.TickCount64);
			}
			else if (category == CancelAbilityCategory && entityId == Player.Object?.EntityId)
			{
				ActionTimelineManager.Current?.CancelInterruptedCast(DateTime.Now);
			}
		}
		catch (Exception ex)
		{
			PluginLog.Error($"Error in ActorControlDetour: {ex.Message}");
		}
	}

	public static string ShowStrSelf { get; private set; } = string.Empty;

	private static void ActionFromEnemy(ActionEffectSet set)
	{
		try
		{
			if (set.Source is not IBattleChara battle || !set.Source.IsEnemy())
			{
				return;
			}

			var playerObject = Player.Object;
			if (playerObject == null)
			{
				return;
			}

			float damageRatio = 0;
			var playerId = playerObject.GameObjectId;
			var maxHp = playerObject.MaxHp;
			var denom = Math.Max(1u, maxHp);

			foreach (var effect in set.TargetEffects)
			{
				if (effect.TargetID == playerId)
				{
					effect.ForEach(entry =>
					{
						if (entry.type == ActionEffectType.Damage)
						{
							damageRatio += (float)entry.value / denom;
						}
					});
				}
			}

			DataCenter.AddDamageRec(damageRatio);

			foreach (var effect in set.TargetEffects)
			{
				if (effect.TargetID != playerId)
				{
					continue;
				}

				if (effect.GetSpecificTypeEffect(ActionEffectType.Knockback, out var entry))
				{
					var knock = Svc.Data.GetExcelSheet<Knockback>()?.GetRow(entry.value);
					if (knock != null)
					{
						DataCenter.KnockbackStart = DateTime.Now;
						if (knock.Value.Speed > 0)
						{
							DataCenter.KnockbackFinished = DateTime.Now + TimeSpan.FromSeconds(knock.Value.Distance / (float)knock.Value.Speed);
						}

						if (set.Action.HasValue && Service.Config.RecordKnockbackies
							&& OtherConfiguration.HostileCastingKnockback.Add(set.Action.Value.RowId))
						{
							_ = OtherConfiguration.SaveHostileCastingKnockback();
						}
					}
					break;
				}
			}

			var partyMembers = DataCenter.PartyMembers;
			var partyMemberCount = partyMembers.Count;

			if (Service.Config.RecordCastingArea && partyMemberCount >= 4 && set.Action?.Cast100ms > 0)
			{
				var type = set.Action?.GetActionCate();
				if (type is ActionCate.Spell or ActionCate.Weaponskill or ActionCate.Ability)
				{
					var damageEffectCount = 0;

					foreach (var effect in set.TargetEffects)
					{
						var isPartyMember = false;
						foreach (var pm in partyMembers)
						{
							if (pm.GameObjectId == effect.TargetID)
							{
								isPartyMember = true;
								break;
							}
						}

						if (isPartyMember &&
							effect.GetSpecificTypeEffect(ActionEffectType.Damage, out var damageEffect) &&
							(damageEffect.value > 0 || (damageEffect.param0 & 6) == 6))
						{
							damageEffectCount++;
						}
					}

					if (damageEffectCount == partyMemberCount
						&& OtherConfiguration.HostileCastingArea.Add(set.Action!.Value.RowId))
					{
						_ = OtherConfiguration.SaveHostileCastingArea();
					}
				}
			}
		}
		catch (Exception ex)
		{
			PluginLog.Error($"Error in ActionFromEnemy: {ex}");
		}
	}

	private static void ActionFromSelf(ActionEffectSet set)
	{
		try
		{
			var playerObject = Player.Object;
			if (set.Source == null || playerObject == null)
			{
				return;
			}

			if (set.Source.GameObjectId != playerObject.GameObjectId)
			{
				return;
			}

			if (set.Action == null)
			{
				return;
			}

			if (set.Action.Value.ActionCategory.RowId == (uint)ActionCate.Autoattack)
			{
				return;
			}

			if (set.TargetEffects.Length == 0)
			{
				return;
			}

			var action = set.Action;
			var tar = set.Target;

			DataCenter.AddActionRec(action!.Value);

			if (Service.Config.InDebug)
			{
				ShowStrSelf = set.ToString();
			}

			DataCenter.HealHP = set.GetSpecificTypeEffect(ActionEffectType.Heal);

			DataCenter.ApplyStatus = set.GetSpecificTypeEffect(ActionEffectType.ApplyStatusEffectTarget) ?? [];
			var sourceApply = set.GetSpecificTypeEffect(ActionEffectType.ApplyStatusEffectSource);
			if (sourceApply is { Count: > 0 })
			{
				foreach (var effect in sourceApply)
				{
					DataCenter.ApplyStatus[effect.Key] = effect.Value;
				}
			}

			uint mpGain = 0;
			var mpEffects = set.GetSpecificTypeEffect(ActionEffectType.MpGain);
			if (mpEffects != null)
			{
				foreach (var effect in mpEffects)
				{
					if (effect.Key == playerObject.GameObjectId)
					{
						mpGain += effect.Value;
					}
				}
			}
			DataCenter.MPGain = mpGain;

			DataCenter.EffectTime = DateTime.Now;
			DataCenter.EffectEndTime = DateTime.Now.AddSeconds(set.Header.AnimationLockTime + 1);

			var attackedTargets = DataCenter.AttackedTargets;
			var attackedTargetsCount = DataCenter.AttackedTargetsCount;

			if (attackedTargetsCount > 0)
			{
				foreach (var effect in set.TargetEffects)
				{
					if (!effect.GetSpecificTypeEffect(ActionEffectType.Damage, out _))
					{
						continue;
					}

					var targetExists = false;
					foreach ((var id, var time) in attackedTargets)
					{
						if (id == effect.TargetID)
						{
							targetExists = true;
							break;
						}
					}
					if (targetExists)
					{
						continue;
					}

					while (attackedTargets.Count >= attackedTargetsCount && attackedTargets.Count > 0)
					{
						(var id, var time) = attackedTargets.Peek();
						if (id == effect.TargetID)
						{
							break;
						}
						_ = attackedTargets.Dequeue();
					}

					attackedTargets.Enqueue((effect.TargetID, DateTime.Now));
				}
			}

			var regexOptions = RegexOptions.IgnoreCase;
			var eventsList = Service.Config.Events ?? [];
			var actionName = action.Value.Name.ExtractText() ?? string.Empty;
			if (!string.IsNullOrEmpty(actionName))
			{
				foreach (var item in eventsList)
				{
					if (string.IsNullOrWhiteSpace(item.Name))
					{
						continue;
					}

					bool isMatch;
					try
					{
						isMatch = Regex.IsMatch(actionName, item.Name, regexOptions);
					}
					catch (ArgumentException ex)
					{
						PluginLog.Warning($"Invalid regex in ActionEventInfo.Name: \"{item.Name}\". {ex.Message}");
						continue;
					}

					if (!isMatch)
					{
						continue;
					}

					if (item.AddMacro(tar))
					{
						break;
					}
				}
			}
		}
		catch (Exception ex)
		{
			PluginLog.Error($"Error in ActionFromSelf: {ex}");
		}
	}
}