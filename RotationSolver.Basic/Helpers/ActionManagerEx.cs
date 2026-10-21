using Dalamud.Hooking;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using RotationSolver.Basic.Tweaks;
using Framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

namespace RotationSolver.Basic.Helpers;

/// <summary>
/// Applies the animation lock and cooldown delay tweaks to every action the client sends, whether RSR or the player used it.
/// </summary>
public sealed unsafe class ActionManagerEx : IDisposable
{
	private static ActionManagerEx? _instance;

	/// <summary>
	/// Gets the singleton instance of <see cref="ActionManagerEx"/>.
	/// </summary>
	public static ActionManagerEx Instance => _instance ??= new ActionManagerEx();

	/// <summary>
	/// Gets the <see cref="AnimationLockTweak"/> instance used for animation lock timing adjustments.
	/// </summary>
	public AnimationLockTweak AnimationLockTweak { get; } = new();

	/// <summary>
	/// Gets the <see cref="CooldownDelayTweak"/> instance used for cooldown delay adjustments.
	/// </summary>
	public CooldownDelayTweak CooldownDelayTweak { get; } = new();

	private delegate void UpdateDelegate(ActionManager* self);
	private delegate bool UseActionLocationDelegate(ActionManager* self, ActionType actionType, uint actionId, ulong targetId, Vector3* location, uint extraParam, byte comboRouteId);
	private delegate void ReceiveActionEffectDelegate(uint casterEntityId, Character* casterPtr, Vector3* targetPos, ActionEffectHandler.Header* header, ActionEffectHandler.TargetEffects* effects, GameObjectId* targetEntityIds);

	private readonly Hook<UpdateDelegate>? _updateHook;
	private readonly Hook<UseActionLocationDelegate>? _useActionLocationHook;
	private readonly Hook<ReceiveActionEffectDelegate>? _receiveActionEffectHook;

	private ActionManagerEx()
	{
		try
		{
			_updateHook = Svc.Hook.HookFromAddress<UpdateDelegate>(ActionManager.Addresses.Update.Value, UpdateDetour);
			_useActionLocationHook = Svc.Hook.HookFromAddress<UseActionLocationDelegate>(ActionManager.Addresses.UseActionLocation.Value, UseActionLocationDetour);
			_receiveActionEffectHook = Svc.Hook.HookFromAddress<ReceiveActionEffectDelegate>(ActionEffectHandler.Addresses.Receive.Value, ReceiveActionEffectDetour);
			_updateHook.Enable();
			_useActionLocationHook.Enable();
			_receiveActionEffectHook.Enable();
		}
		catch (Exception ex)
		{
			PluginLog.Error($"[ActionManagerEx] Failed to hook the action manager: {ex}");
		}
	}

	/// <summary>
	/// Installs the hooks.
	/// </summary>
	internal static void Enable()
	{
		_ = Instance;
	}

	/// <summary>
	/// Removes the hooks.
	/// </summary>
	internal static void DisposeInstance()
	{
		_instance?.Dispose();
		_instance = null;
	}

	// Queued actions are used from inside Update, so the overshoot is measured from the state before Update ticks the timers.
	private void UpdateDetour(ActionManager* self)
	{
		if (Service.Config.RemoveCooldownDelay && self->ActionQueued)
		{
			CooldownDelayTweak.StartAdjustment(self->AnimationLock, GetRemainingCooldown(self, self->QueuedActionType, self->QueuedActionId),
				Framework.Instance()->FrameDeltaTime);
		}

		try
		{
			_updateHook!.Original(self);
		}
		finally
		{
			CooldownDelayTweak.StopAdjustment();
		}
	}

	private bool UseActionLocationDetour(ActionManager* self, ActionType actionType, uint actionId, ulong targetId, Vector3* location, uint extraParam, byte comboRouteId)
	{
		var prevSequence = self->LastUsedActionSequence;
		var result = _useActionLocationHook!.Original(self, actionType, actionId, targetId, location, extraParam, comboRouteId);

		// An unchanged sequence means the action was queued or refused, so no request went out.
		if (self->LastUsedActionSequence == prevSequence)
		{
			return result;
		}

		try
		{
			var adjustment = CooldownDelayTweak.Adjustment;
			if (adjustment > 0)
			{
				var group = self->GetRecastGroup((int)actionType, actionId);
				var additionalGroup = self->GetAdditionalRecastGroup(actionType, actionId);
				AdjustRecast(self, group, adjustment);
				if (additionalGroup != group)
				{
					AdjustRecast(self, additionalGroup, adjustment);
				}

				self->AnimationLock = Math.Max(0, self->AnimationLock - adjustment);
				if (Service.Config.InDebug)
				{
					PluginLog.Debug($"[ActionManagerEx] Used {actionId} {adjustment:f3}s earlier to make up for the frame rate.");
				}
			}

			if (Service.Config.RemoveAnimationLockDelay)
			{
				AnimationLockTweak.RecordRequest(self->LastUsedActionSequence, self->AnimationLock);
			}
		}
		catch (Exception ex)
		{
			PluginLog.Warning($"[ActionManagerEx] Failed to apply the action tweaks: {ex.Message}");
		}

		return result;
	}

	private void ReceiveActionEffectDetour(uint casterEntityId, Character* casterPtr, Vector3* targetPos, ActionEffectHandler.Header* header, ActionEffectHandler.TargetEffects* effects, GameObjectId* targetEntityIds)
	{
		var actionManager = ActionManager.Instance();
		var packetAnimLock = header->AnimationLock;
		var prevAnimLock = actionManager == null ? 0 : actionManager->AnimationLock;

		_receiveActionEffectHook!.Original(casterEntityId, casterPtr, targetPos, header, effects, targetEntityIds);

		// Only the player's own requests carry a source sequence.
		if (!Service.Config.RemoveAnimationLockDelay || actionManager == null || header->SourceSequence == 0
			|| casterEntityId != Player.Object?.EntityId)
		{
			return;
		}

		try
		{
			var reduction = AnimationLockTweak.Apply(header->SourceSequence, prevAnimLock, actionManager->AnimationLock,
				packetAnimLock, header->AnimationLock, out var delay);
			if (reduction > 0)
			{
				actionManager->AnimationLock = Math.Max(0, actionManager->AnimationLock - reduction);
				if (Service.Config.InDebug)
				{
					PluginLog.Debug($"[ActionManagerEx] Reduced animation lock by {reduction:f3}s (delay: {delay:f3}s)");
				}
			}
		}
		catch (Exception ex)
		{
			PluginLog.Warning($"[ActionManagerEx] Failed to apply the animation lock tweak: {ex.Message}");
		}
	}

	private static float GetRemainingCooldown(ActionManager* actionManager, ActionType actionType, uint actionId)
	{
		var group = actionManager->GetRecastGroup((int)actionType, actionId);
		if (group < 0)
		{
			return 0;
		}

		var detail = actionManager->GetRecastGroupDetail(group);
		return detail == null || !detail->IsActive ? 0 : Math.Max(0, detail->Total - detail->Elapsed);
	}

	private static void AdjustRecast(ActionManager* actionManager, int group, float adjustment)
	{
		if (group < 0)
		{
			return;
		}

		var detail = actionManager->GetRecastGroupDetail(group);
		if (detail != null && detail->IsActive)
		{
			detail->Elapsed = Math.Min(detail->Total, detail->Elapsed + adjustment);
		}
	}

	/// <summary>
	/// Get animation lock delay estimate for rotation timing
	/// </summary>
	public float GetAnimationLockDelayEstimate() => AnimationLockTweak.DelayEstimate;

	/// <summary>
	/// Get current cooldown adjustment
	/// </summary>
	public float GetCooldownAdjustment() => CooldownDelayTweak.Adjustment;

	/// <summary>
	/// Releases all resources used by the <see cref="ActionManagerEx"/> instance.
	/// </summary>
	public void Dispose()
	{
		_updateHook?.Dispose();
		_useActionLocationHook?.Dispose();
		_receiveActionEffectHook?.Dispose();
		CooldownDelayTweak.StopAdjustment();
	}
}
