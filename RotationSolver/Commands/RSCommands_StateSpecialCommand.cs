using Dalamud.Game.Config;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using RotationSolver.IPC;
using RotationSolver.Updaters;

namespace RotationSolver.Commands
{
	public static partial class RSCommands
	{
		public static string _stateString = "Off", _specialString = string.Empty;

		internal static string EntryString => $"{_stateString}{(DataCenter.SpecialTimeLeft < 0 ? string.Empty : $" - {_specialString}: {DataCenter.SpecialTimeLeft:F2}s")}";

		private static string _lastToastMessage = string.Empty;

		private static void UpdateToast()
		{
			if (!Service.Config.ShowInfoOnToast)
			{
				return;
			}

			var currentMessage = $" {EntryString}";
			if (currentMessage == _lastToastMessage)
			{
				return;
			}

			Svc.Toasts.ShowQuest(currentMessage, new Dalamud.Game.Gui.Toast.QuestToastOptions
			{
				IconId = 101,
			});

			_lastToastMessage = currentMessage;
		}

		public static void DoStateCommandType(StateCommandType stateType, int index = -1)
		{
			DoOneCommandType(role =>
			{
				if (!DataCenter.State && DataCenter.IsPvP && !DataCenter.IsPvPStateEnabled && Service.Config.PvpStateControl)
				{
					stateType = AdjustStateType(StateCommandType.PvP, ref index);
				}

				if (DataCenter.State && DataCenter.IsPvP && DataCenter.IsPvPStateEnabled && Service.Config.PvpStateControl)
				{
					stateType = AdjustStateType(StateCommandType.Off, ref index);
				}

				if (DataCenter.State)
				{
					stateType = AdjustStateType(stateType, ref index);
				}
				else if (stateType != StateCommandType.Off && DataCenter.IsInDutyReplay())
				{
					stateType = StateCommandType.Off;
				}
				else if (index != -1 && stateType is StateCommandType.Auto or StateCommandType.TargetOnly)
				{
					UpdateTargetingIndex(ref index);
				}
				UpdateState(stateType, role);

				if (!DataCenter.AutoFaceTargetOnActionSetting() && DataCenter.MoveModeSetting() == 1)
				{
					Svc.GameConfig.UiControl.Set(UiControlOption.AutoFaceTargetOnAction.ToString(), 1);
				}
			});
		}

		public static void SetStateCommandType(StateCommandType stateType)
		{
			DoOneCommandType(role =>
			{
				if (stateType != StateCommandType.Off)
				{
					if (DataCenter.IsInDutyReplay())
					{
						stateType = StateCommandType.Off;
					}
					else if (DataCenter.IsPvP && Service.Config.PvpStateControl)
					{
						stateType = StateCommandType.PvP;
					}
				}

				UpdateState(stateType, role);

				if (!DataCenter.AutoFaceTargetOnActionSetting() && DataCenter.MoveModeSetting() == 1)
				{
					Svc.GameConfig.UiControl.Set(UiControlOption.AutoFaceTargetOnAction.ToString(), 1);
				}
			});
		}

		public static void SetTargetingIndex(int index)
		{
			var count = Service.Config.TargetingTypes.Count;
			if (count == 0)
			{
				return;
			}

			Service.Config.TargetingIndex = ((index % count) + count) % count;

			if (!DataCenter.State || DataCenter.IsManual || DataCenter.IsAutoDuty || DataCenter.IsPvPStateEnabled)
			{
				return;
			}

			SetStateCommandType(DataCenter.IsTargetOnly ? StateCommandType.TargetOnly : StateCommandType.Auto);
		}

		public static void DoAutodutyStateCommandType(StateCommandType stateType, TargetingType targetingType)
		{
			DoOneCommandType(role =>
			{
				AutodutyUpdateState(stateType, role, targetingType);
			});
		}

		private static StateCommandType AdjustStateType(StateCommandType stateType, ref int index)
		{
			if (DataCenter.IsInDutyReplay())
			{
				return StateCommandType.Off;
			}

			if (!DataCenter.State && DataCenter.IsPvP && !DataCenter.IsPvPStateEnabled && Service.Config.PvpStateControl)
			{
				return StateCommandType.PvP;
			}

			if (DataCenter.State && DataCenter.IsPvP && DataCenter.IsPvPStateEnabled && Service.Config.PvpStateControl)
			{
				return StateCommandType.Off;
			}

			if (DataCenter.IsManual && stateType == StateCommandType.Manual && Service.Config.ToggleManual)
			{
				return StateCommandType.Off;
			}
			else if (stateType == StateCommandType.Auto)
			{
				var isAlreadyAuto = DataCenter.State && !DataCenter.IsManual && !DataCenter.IsTargetOnly && !DataCenter.IsAutoDuty && !DataCenter.IsPvPStateEnabled;
				if (!isAlreadyAuto)
				{
					if (index != -1)
					{
						UpdateTargetingIndex(ref index);
					}
				}
				else if (Service.Config.ToggleAuto)
				{
					return StateCommandType.Off;
				}
				else
				{
					// If no explicit index passed, we are cycling; clear any override to resume rotating the list.
					if (index == -1)
					{
						DataCenter.TargetingTypeOverride = null;
					}
					UpdateTargetingIndex(ref index);
				}
			}
			return stateType;
		}

		public static void CycleStateManualAuto()
		{
			if (!DataCenter.State && DataCenter.IsPvP && Service.Config.PvpStateControl)
			{
				DoStateCommandType(StateCommandType.PvP);
				return;
			}

			// If currently Off, go to Manual
			if (!DataCenter.State && (!DataCenter.IsPvP || (DataCenter.IsPvP && !Service.Config.PvpStateControl)))
			{
				DoStateCommandType(StateCommandType.Manual);
				return;
			}

			// If currently in Manual mode, switch to Auto
			if (DataCenter.IsManual)
			{
				DoStateCommandType(StateCommandType.Auto);
				return;
			}

			// If currently On but not Manual, switch to Manual
			DoStateCommandType(StateCommandType.Manual);
		}

		public static void CycleStateAuto()
		{
			if (!DataCenter.State && DataCenter.IsPvP && Service.Config.PvpStateControl)
			{
				DoStateCommandType(StateCommandType.PvP);
				return;
			}

			// If currently Off, go to Auto
			if (!DataCenter.State && (!DataCenter.IsPvP || (DataCenter.IsPvP && !Service.Config.PvpStateControl)))
			{
				DoStateCommandType(StateCommandType.Auto);
				return;
			}

			// If currently in Auto mode, turn Off
			if (DataCenter.State && !DataCenter.IsManual)
			{
				DoStateCommandType(StateCommandType.Off);
				return;
			}

			// If currently On but not Auto (i.e., Manual), switch to Auto
			DoStateCommandType(StateCommandType.Auto);
		}

		public static void CycleStateManual()
		{
			if (!DataCenter.State && DataCenter.IsPvP && Service.Config.PvpStateControl)
			{
				DoStateCommandType(StateCommandType.PvP);
				return;
			}

			// If currently Off, go to Manual
			if (!DataCenter.State && (!DataCenter.IsPvP || (DataCenter.IsPvP && !Service.Config.PvpStateControl)))
			{
				DoStateCommandType(StateCommandType.Manual);
				return;
			}

			// If currently in Manual mode, turn Off
			if (DataCenter.IsManual)
			{
				DoStateCommandType(StateCommandType.Off);
				return;
			}

			// If currently On but not Manual, switch to Manual
			DoStateCommandType(StateCommandType.Manual);
		}

		public static void CycleStateWithAllTargetTypes()
		{
			if (!DataCenter.State && DataCenter.IsPvP && Service.Config.PvpStateControl)
			{
				DoStateCommandType(StateCommandType.PvP);
				return;
			}

			// If currently Off, start with the first TargetType
			if (!DataCenter.State && (!DataCenter.IsPvP || (DataCenter.IsPvP && !Service.Config.PvpStateControl)))
			{
				if (Service.Config.TargetingTypes.Count > 0)
				{
					Service.Config.TargetingIndex = 0;
					DoStateCommandType(StateCommandType.Auto, 0);
				}
				else
				{
					// No targeting types configured, go to Manual
					DoStateCommandType(StateCommandType.Manual);
				}
				return;
			}

			// If currently in Auto mode, cycle through all TargetTypes
			if (DataCenter.State && !DataCenter.IsManual)
			{
				var nextIndex = Service.Config.TargetingIndex + 1;

				// If we've gone through all TargetTypes, switch to Manual
				if (nextIndex >= Service.Config.TargetingTypes.Count)
				{
					DoStateCommandType(StateCommandType.Manual);
				}
				else
				{
					// Move to next TargetType
					Service.Config.TargetingIndex = nextIndex;
					DoStateCommandType(StateCommandType.Auto, nextIndex);
				}
				return;
			}

			// If currently in Manual mode, turn off
			if (DataCenter.State && DataCenter.IsManual)
			{
				DoStateCommandType(StateCommandType.Off);
				return;
			}
		}

		public static void CycleStateWithOneTargetTypes()
		{
			if (!DataCenter.State && DataCenter.IsPvP && Service.Config.PvpStateControl)
			{
				DoStateCommandType(StateCommandType.PvP);
				return;
			}

			// If currently Off, go to Auto using the highest TargetingIndex (last configured type)
			if (!DataCenter.State && (!DataCenter.IsPvP || (DataCenter.IsPvP && !Service.Config.PvpStateControl)))
			{
				if (Service.Config.TargetingTypes.Count > 0)
				{
					var lastIdx = Service.Config.TargetingTypes.Count - 1;
					Service.Config.TargetingIndex = lastIdx;
					DoStateCommandType(StateCommandType.Auto, lastIdx);
				}
				else
				{
					// No targeting types configured, go to Manual
					DoStateCommandType(StateCommandType.Manual);
				}
				return;
			}

			// If currently in Auto mode, switch to Manual
			if (DataCenter.State && !DataCenter.IsManual)
			{
				DoStateCommandType(StateCommandType.Manual);
				return;
			}

			// If currently in Manual mode, turn Off
			if (DataCenter.State && DataCenter.IsManual)
			{
				DoStateCommandType(StateCommandType.Off);
				return;
			}
		}

		private static void UpdateTargetingIndex(ref int index)
		{
			var count = Service.Config.TargetingTypes.Count;
			if (count == 0)
			{
				index = 0;
				Service.Config.TargetingIndex = 0;
				return;
			}

			if (index == -1)
			{
				index = Service.Config.TargetingIndex + 1;
			}
			index %= count;
			Service.Config.TargetingIndex = index;
		}

		public static void UpdateState(StateCommandType stateType, JobRole role, IpcStateOverrides? ipcOverrides = null)
		{
			ApplyState(stateType, role, null, ipcOverrides);
		}

		public static void AutodutyUpdateState(StateCommandType stateType, JobRole role, TargetingType targetingType, IpcStateOverrides? ipcOverrides = null)
		{
			ApplyState(stateType, role, targetingType, ipcOverrides);
		}

		// autodutyTargeting is the targeting AutoDuty asked for; it applies to TargetOnly and AutoDuty.
		private static void ApplyState(StateCommandType stateType, JobRole role, TargetingType? autodutyTargeting, IpcStateOverrides? ipcOverrides)
		{
			if (DataCenter.PvPAutomationBlocked && stateType != StateCommandType.Off)
			{
				Svc.Chat.PrintError("Rotation Solver Reborn: Autorotation is blocked in PvP while 'Auto PVP Series Grind' is enabled. It is an AI generated plugin and has been breaking RSR installations.");
				stateType = StateCommandType.Off;
			}

			DataCenter.State = stateType != StateCommandType.Off;
			DataCenter.IsManual = stateType is StateCommandType.Manual or StateCommandType.Henched;
			DataCenter.IsTargetOnly = stateType == StateCommandType.TargetOnly;
			DataCenter.IsAutoDuty = stateType == StateCommandType.AutoDuty;
			DataCenter.IsHenched = stateType == StateCommandType.Henched;
			DataCenter.IsPvPStateEnabled = stateType == StateCommandType.PvP;
			DataCenter.TargetingTypeOverride = stateType switch
			{
				StateCommandType.PvP => TargetingType.LowHP,
				StateCommandType.TargetOnly or StateCommandType.AutoDuty => autodutyTargeting,
				_ => null,
			};

			if (DataCenter.State)
			{
				ActionUpdater.AutoCancelTime = DateTime.MinValue;
			}
			else
			{
				DataCenter.ResetAllRecords();
				Wrath_IPCSubscriber.Release();
				ActionUpdater.NextAction = ActionUpdater.NextGCDAction = null;
				Service.CanMove = true;
			}

			if (Service.Config.ShowToggledSettingInChat)
			{
				Svc.Chat.Print(stateType switch
				{
					StateCommandType.Auto => $"Auto Targeting : {DataCenter.TargetingType}",
					StateCommandType.TargetOnly => $"Auto Targeting Only : {DataCenter.TargetingType}",
					_ => $"Targeting : {stateType}",
				});
			}

			DataCenter.IpcOverrides = DataCenter.State ? ipcOverrides : null;
			_stateString = stateType == StateCommandType.AutoDuty && autodutyTargeting.HasValue
				? $"{stateType.ToStateString()} ({autodutyTargeting})"
				: stateType.ToStateString();
			UpdateToast();
		}
		public static void DoSpecialCommandType(SpecialCommandType specialType, bool sayout = true)
		{
			DoOneCommandType(role =>
			{
				_specialString = specialType.ToSpecialString();
				DataCenter.SpecialType = specialType;
				if (sayout)
				{
					UpdateToast();
				}
			});
		}

		private static void DoOneCommandType(Action<JobRole> doingSomething)
		{
			var role = Player.Object?.ClassJob.Value.GetJobRole() ?? JobRole.None;

			if (role == JobRole.None)
			{
				return;
			}

			doingSomething(role);
		}
	}
}