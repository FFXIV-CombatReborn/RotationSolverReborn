using Dalamud.Hooking;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using RotationSolver.Helpers;

namespace RotationSolver.Updaters
{
	internal static class AutoAttackUpdater
	{
		private static Hook<SetAutoAttackStateDelegate>? _setAutoAttackStateHook;

		private unsafe delegate bool SetAutoAttackStateDelegate(AutoAttackState* self, bool value, bool sendPacket, bool isInstant);

		public static unsafe void Enable()
		{
			try
			{
				var setAutoAttackStateAddress = AutoAttackState.Addresses.SetImpl.Value;
				_setAutoAttackStateHook = Svc.Hook.HookFromAddress<SetAutoAttackStateDelegate>(setAutoAttackStateAddress, SetAutoAttackStateDetour);
				_setAutoAttackStateHook?.Enable();

				PluginLog.Debug("[AutoAttackUpdater] Auto attack state hook initialized");
			}
			catch (Exception ex)
			{
				PluginLog.Error($"[AutoAttackUpdater] Failed to initialize auto attack hook: {ex}");
			}
		}

		public static void Disable()
		{
			try
			{
				_setAutoAttackStateHook?.Disable();
				_setAutoAttackStateHook?.Dispose();
				_setAutoAttackStateHook = null;

				PluginLog.Debug("[AutoAttackUpdater] Auto attack state hook disposed");
			}
			catch (Exception ex)
			{
				PluginLog.Error($"[AutoAttackUpdater] Failed to dispose auto attack hook: {ex}");
			}
		}

		private static readonly TimeSpan ToggleCooldown = TimeSpan.FromMilliseconds(500);
		private static DateTime _lastToggle = DateTime.MinValue;

		public static unsafe void Update()
		{
			if (!Player.Available)
			{
				return;
			}

			try
			{
				var uiState = UIState.Instance();
				var actionManager = ActionManager.Instance();
				if (uiState == null || actionManager == null)
				{
					return;
				}

				var now = DateTime.Now;
				if (uiState->WeaponState.AutoAttackState.IsAutoAttacking
					&& now - _lastToggle >= ToggleCooldown
					&& PlayerHasNoCastingStatus())
				{
					actionManager->UseAction(ActionType.GeneralAction, 1);
					_lastToggle = now;
					PluginLog.Information("[AutoAttackUpdater] Disabled active auto attacks due to NoCastingStatus.");
				}
			}
			catch (Exception ex)
			{
				PluginLog.Warning($"[AutoAttackUpdater] Error in Update (auto attack disable): {ex.Message}");
			}
		}

		internal static bool PlayerHasNoCastingStatus()
		{
			try
			{
				if (DataCenter.BMRSpecialModeType == SpecialMode.Pyretic)
				{
					return true;
				}

				if (Player.Object?.StatusList == null)
				{
					return false;
				}

				if (StatusHelper.PlayerHasStatus(false, StatusID.MotionTracker))
				{
					return true;
				}

				return NoCastingStatusHelper.PlayerHasNoCastingStatus(out _);
			}
			catch (Exception ex)
			{
				PluginLog.Warning($"[AutoAttackUpdater] Error checking NoCastingStatus: {ex.Message}");
			}
			return false;
		}

		private static unsafe bool SetAutoAttackStateDetour(AutoAttackState* self, bool value, bool sendPacket, bool isInstant)
		{
			if (value && Player.Available && DataCenter.IsActivated() && PlayerHasNoCastingStatus())
			{
				PluginLog.Debug("[AutoAttackUpdater] Prevented auto attack activation due to NoCastingStatus.");
				return true;
			}

			return _setAutoAttackStateHook!.Original(self, value, sendPacket, isInstant);
		}
	}
}
