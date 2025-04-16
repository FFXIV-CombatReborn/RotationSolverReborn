using ECommons.DalamudServices;
using ECommons.GameHelpers;
using RotationSolver.Extensions;
using RotationSolver.Helpers;
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
            if (!Service.Config.ShowInfoOnToast) return;

            string currentMessage = $" {EntryString}";
            if (currentMessage == _lastToastMessage) return;

            Svc.Toasts.ShowQuest(currentMessage, new Dalamud.Game.Gui.Toast.QuestToastOptions
            {
                IconId = 101,
            });

            _lastToastMessage = currentMessage;
        }

        public static unsafe void DoStateCommandType(StateCommandType stateType, int index = -1) => DoOneCommandType((type, role) => type.ToStateString(role), role =>
        {
            if (DataCenter.State)
            {
                stateType = AdjustStateType(stateType, ref index);
            }
            UpdateState(stateType, role);
            return stateType;
        });

        private static StateCommandType AdjustStateType(StateCommandType stateType, ref int index)
        {
            if (DataCenter.IsManual && stateType == StateCommandType.Manual && Service.Config.ToggleManual)
            {
                return StateCommandType.Off;
            }
            else if (stateType == StateCommandType.Auto)
            {
                if (Service.Config.ToggleAuto)
                {
                    return StateCommandType.Off;
                }
                else
                {
                    UpdateTargetingIndex(ref index);
                }
            }
            return stateType;
        }

        private static void UpdateTargetingIndex(ref int index)
        {
            if (index == -1)
            {
                index = Service.Config.TargetingIndex + 1;
            }
            index %= Service.Config.TargetingTypes.Count;
            Service.Config.TargetingIndex = index;
        }

        private static void UpdateState(StateCommandType stateType, JobRole role)
        {
            switch (stateType)
            {
                case StateCommandType.Off:
                    DataCenter.State = false;
                    DataCenter.IsManual = false;
                    ActionUpdater.NextAction = ActionUpdater.NextGCDAction = null;
                    if (Service.Config.ShowToggledSettingInChat) { Svc.Chat.Print($"Targeting : Off"); }
                    break;


                case StateCommandType.Auto:
                    DataCenter.IsManual = false;
                    DataCenter.State = true;
                    ActionUpdater.AutoCancelTime = DateTime.MinValue;
                    if (Service.Config.ShowToggledSettingInChat) { Svc.Chat.Print($"Auto Targeting : {Service.Config.TargetingTypes[Service.Config.TargetingIndex]}"); }
                    break;


                case StateCommandType.Manual:
                    DataCenter.IsManual = true;
                    DataCenter.State = true;
                    ActionUpdater.AutoCancelTime = DateTime.MinValue;
                    if (Service.Config.ShowToggledSettingInChat) { Svc.Chat.Print($"Targeting : Manual"); }
                    break;
            }

            _stateString = stateType.ToStateString(role);
            UpdateToast();
        }

        private static void DoSpecialCommandType(SpecialCommandType specialType, bool sayout = true) => DoOneCommandType((type, role) => type.ToSpecialString(role), role =>
        {
            _specialString = specialType.ToSpecialString(role);
            DataCenter.SpecialType = specialType;
            if (sayout) UpdateToast();
            return specialType;
        });

        private static void DoOneCommandType<T>(Func<T, JobRole, string> sayout, Func<JobRole, T> doingSomething)
            where T : struct, Enum
        {
            var role = Player.Object?.ClassJob.Value.GetJobRole() ?? JobRole.None;

            if (role == JobRole.None) return;

            T type = doingSomething(role);

            if (Service.Config.SayOutStateChanged) SpeechHelper.Speak(sayout(type, role));
        }
    }
}
