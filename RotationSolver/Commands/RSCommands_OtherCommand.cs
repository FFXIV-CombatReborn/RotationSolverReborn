using ECommons.DalamudServices;
using ECommons.Logging;
using RotationSolver.Basic.Configuration;
using RotationSolver.Basic.Rotations.Duties;
using RotationSolver.Data;
using RotationSolver.Updaters;

namespace RotationSolver.Commands;

public static partial class RSCommands
{
    public static void DoOtherCommand(OtherCommandType otherType, string str)
    {
        switch (otherType)
        {
            case OtherCommandType.Cycle:
                ExecuteCycleCommand();
                break;

            case OtherCommandType.Rotations:
                ExecuteRotationCommand(str);
                break;

            case OtherCommandType.DutyRotations:
                ExecuteDutyRotationCommand(str);
                break;

            case OtherCommandType.DoActions:
                DoActionCommand(str);
                break;

            case OtherCommandType.ToggleActions:
                ToggleActionCommand(str);
                break;

            case OtherCommandType.Settings:
                DoSettingCommand(str);
                break;

            case OtherCommandType.NextAction:
                DoAction();
                break;
        }
    }

    private static void ExecuteCycleCommand()
    {
        if (Service.Config.CycleType == CycleType.CycleNormal)
        {
            CycleStateWithOneTargetTypes();
        }
        else if (Service.Config.CycleType == CycleType.CycleAllAuto)
        {
            CycleStateWithAllTargetTypes();
        }
        else if (Service.Config.CycleType == CycleType.CycleAuto)
        {
            CycleStateAuto();
        }
        else if (Service.Config.CycleType == CycleType.CycleManual)
        {
            CycleStateManual();
        }
        else if (Service.Config.CycleType == CycleType.CycleManualAuto)
        {
            CycleStateManualAuto();
        }
    }

    private static void ExecuteRotationCommand(string str)
    {
        ICustomRotation? customCombo = DataCenter.CurrentRotation;
        if (customCombo == null)
        {
            return;
        }

        DoRotationCommand(customCombo, str);
    }

    private static void ExecuteDutyRotationCommand(string str)
    {
        DutyRotation? dutyRotation = DataCenter.CurrentDutyRotation;
        if (dutyRotation == null)
        {
            return;
        }

        DoDutyRotationCommand(dutyRotation, str);
    }

    private static void DoSettingCommand(string str)
    {
        string[] strs = str.Split(' ', 3);
        if (strs.Length < 2)
        {
            Svc.Chat.PrintError("Invalid setting command format.");
            return;
        }

        string settingName = strs[0];
        string? command = null;
        if (strs.Length > 1)
        {
            // Equivalent to string.Join(' ', strs.Skip(1))
            string[] arr = new string[strs.Length - 1];
            Array.Copy(strs, 1, arr, 0, strs.Length - 1);
            command = string.Join(' ', arr);
        }

        if (string.IsNullOrEmpty(settingName))
        {
            Svc.Chat.PrintError("Invalid setting command format.");
            return;
        }

        if (settingName.Equals("TargetingTypes", StringComparison.OrdinalIgnoreCase))
        {
            HandleTargetingTypesCommand(command);
            return;
        }

        UpdateSetting(settingName, command);
    }

    private static void UpdateSetting(string settingName, string? command)
    {
        PropertyInfo[] properties = [.. typeof(Configs).GetRuntimeProperties()];
        for (int i = 0; i < properties.Length; i++)
        {
            PropertyInfo property = properties[i];
            MethodInfo? getMethod = property.GetMethod;
            if (getMethod == null || !getMethod.IsPublic)
            {
                continue;
            }

            if (!settingName.Equals(property.Name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Type type = property.PropertyType == typeof(ConditionBoolean) ? typeof(bool) : property.PropertyType;
            if (!TryConvertValue(type, command, out object? convertedValue))
            {
                if (property.GetValue(Service.Config) is ConditionBoolean config)
                {
                    config.Value = !config.Value;
                    convertedValue = config.Value;
                }
                else
                {
                    Svc.Chat.PrintError("Failed to parse the value.");
                    return;
                }
            }

            if (property.PropertyType == typeof(ConditionBoolean))
            {
                if (convertedValue is bool boolValue)
                {
                    ConditionBoolean relay = (ConditionBoolean)property.GetValue(Service.Config)!;
                    relay.Value = boolValue;
                    convertedValue = relay;
                }
                else
                {
                    Svc.Chat.PrintError("Failed to parse the value as boolean.");
                    return;
                }
            }

            property.SetValue(Service.Config, convertedValue);
            command = convertedValue?.ToString();

            if (Service.Config.ShowToggledSettingInChat)
            {
                Svc.Chat.Print($"Changed setting {property.Name} to {command}");
            }

            return;
        }

        Svc.Chat.PrintError("Failed to find the config in this rotation, please check it.");
    }

    private static bool TryConvertValue(Type type, string? command, out object? convertedValue)
    {
        convertedValue = null;
        if (type.IsEnum)
        {
            return Enum.TryParse(type, command, ignoreCase: true, out convertedValue);
        }

        try
        {
            convertedValue = Convert.ChangeType(command, type);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void HandleTargetingTypesCommand(string? command)
    {
        if (string.IsNullOrEmpty(command))
        {
            Svc.Chat.PrintError("Invalid command for TargetingTypes.");
            return;
        }

        string[] commandParts = command.Split(' ', 2);
        if (commandParts.Length < 1)
        {
            Svc.Chat.PrintError("Invalid command format for TargetingTypes.");
            return;
        }

        string action = commandParts[0];
        string? value = commandParts.Length > 1 ? commandParts[1] : null;

        switch (action.ToLower())
        {
            case "add":
                AddTargetingType(value);
                break;

            case "remove":
                RemoveTargetingType(value);
                break;

            case "removeall":
                Service.Config.TargetingTypes.Clear();
                if (DataCenter.IsActivated())
                {
                    Svc.Chat.Print("Removed all TargetingTypes and reset to default list.");
                }
                else
                {
                    Svc.Chat.Print("Removed all TargetingTypes.");
                }
                break;

            default:
                Svc.Chat.PrintError("Invalid action for TargetingTypes.");
                break;
        }

        Service.Config.Save();
    }

    private static void AddTargetingType(string? value)
    {
        if (string.IsNullOrEmpty(value) || !Enum.TryParse(typeof(TargetingType), value, true, out object? parsedEnumAdd))
        {
            Svc.Chat.PrintError("Invalid TargetingType value.");
            return;
        }

        TargetingType targetingTypeAdd = (TargetingType)parsedEnumAdd;
        if (!Service.Config.TargetingTypes.Contains(targetingTypeAdd))
        {
            Service.Config.TargetingTypes.Add(targetingTypeAdd);
            Svc.Chat.Print($"Added {targetingTypeAdd} to TargetingTypes.");
        }
        else
        {
            Svc.Chat.Print($"{targetingTypeAdd} is already in TargetingTypes.");
        }
    }

    private static void RemoveTargetingType(string? value)
    {
        if (string.IsNullOrEmpty(value) || !Enum.TryParse(typeof(TargetingType), value, true, out object? parsedEnumRemove))
        {
            Svc.Chat.PrintError("Invalid TargetingType value.");
            return;
        }

        TargetingType targetingTypeRemove = (TargetingType)parsedEnumRemove;
        if (Service.Config.TargetingTypes.Contains(targetingTypeRemove))
        {
            _ = Service.Config.TargetingTypes.Remove(targetingTypeRemove);
            Svc.Chat.Print($"Removed {targetingTypeRemove} from TargetingTypes.");
        }
        else
        {
            Svc.Chat.Print($"{targetingTypeRemove} is not in TargetingTypes.");
        }
    }

    private static Enum GetNextEnumValue(Enum currentEnumValue)
    {
        Array values = Enum.GetValues(currentEnumValue.GetType());
        Enum[] enumValues = new Enum[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            enumValues[i] = (Enum)values.GetValue(i)!;
        }
        int nextIndex = Array.IndexOf(enumValues, currentEnumValue) + 1;

        return enumValues.Length == nextIndex ? enumValues[0] : enumValues[nextIndex];
    }

    private static void ToggleActionCommand(string str)
    {
        string trimStr = str.Trim();

        var rotationActions = RotationUpdater.CurrentRotationActions ?? [];
        var dutyActions = DataCenter.CurrentDutyRotation?.AllActions ?? [];

        int totalLength = rotationActions.Length + dutyActions.Length;
        List<IAction> allActionsList = new(totalLength);
        HashSet<IAction> seen = [];
        for (int i = 0; i < rotationActions.Length; i++)
        {
            if (seen.Add(rotationActions[i]))
                allActionsList.Add(rotationActions[i]);
        }
        for (int i = 0; i < dutyActions.Length; i++)
        {
            if (seen.Add(dutyActions[i]))
                allActionsList.Add(dutyActions[i]);
        }
        IAction[] allActions = [.. allActionsList];

        // Sort by Name.Length descending (bubble sort)
        int n = allActions.Length;
        IAction[] sortedActions = new IAction[n];
        Array.Copy(allActions, sortedActions, n);
        for (int i = 0; i < n - 1; i++)
        {
            for (int j = 0; j < n - i - 1; j++)
            {
                if (sortedActions[j].Name.Length < sortedActions[j + 1].Name.Length)
                {
                    (sortedActions[j + 1], sortedActions[j]) = (sortedActions[j], sortedActions[j + 1]);
                }
            }
        }

        for (int i = 0; i < n; i++)
        {
            IAction? act = sortedActions[i];
            if (trimStr.Equals(act.Name, StringComparison.OrdinalIgnoreCase))
            {
                act.IsEnabled = !act.IsEnabled;
                if (Service.Config.ShowToggledSettingInChat)
                {
                    Svc.Chat.Print($"Toggled {act.Name} : {act.IsEnabled}");
                }
                return;
            }
            if (trimStr.StartsWith(act.Name + " ", StringComparison.OrdinalIgnoreCase))
            {
                string flag = trimStr[act.Name.Length..].Trim();
                act.IsEnabled = bool.TryParse(flag, out bool parse) ? parse : !act.IsEnabled;
                if (Service.Config.ShowToggledSettingInChat)
                {
                    Svc.Chat.Print($"Toggled {act.Name} : {act.IsEnabled}");
                }
                return;
            }
        }
    }

    public static void DoActionCommand(string str)
    {
        int lastHyphenIndex = str.LastIndexOf('-');
        if (lastHyphenIndex == -1 || lastHyphenIndex == str.Length - 1)
        {
            Svc.Chat.PrintError(UiString.CommandsInsertActionFailure.GetDescription());
            return;
        }

        string actName = str[..lastHyphenIndex].Trim();
        string timeStr = str[(lastHyphenIndex + 1)..].Trim();

        if (double.TryParse(timeStr, out double time))
        {
            var rotationActions = RotationUpdater.CurrentRotationActions ?? [];
            var dutyActions = DataCenter.CurrentDutyRotation?.AllActions ?? [];

            int totalLength = rotationActions.Length + dutyActions.Length;
            List<IAction> allActionsList = new(totalLength);
            HashSet<IAction> seen = [];
            for (int i = 0; i < rotationActions.Length; i++)
            {
                if (seen.Add(rotationActions[i]))
                    allActionsList.Add(rotationActions[i]);
            }
            for (int i = 0; i < dutyActions.Length; i++)
            {
                if (seen.Add(dutyActions[i]))
                    allActionsList.Add(dutyActions[i]);
            }
            IAction[] allActions = [.. allActionsList];

            for (int i = 0; i < allActions.Length; i++)
            {
                IAction iAct = allActions[i];
                if (actName.Equals(iAct.Name, StringComparison.OrdinalIgnoreCase))
                {
                    DataCenter.AddCommandAction(iAct, time);

                    if (Service.Config.ShowToastsAboutDoAction)
                    {
                        Svc.Toasts.ShowQuest($"Inserted action {iAct.Name} with time {time}",
                            new Dalamud.Game.Gui.Toast.QuestToastOptions()
                            {
                                IconId = iAct.IconID,
                            });
                    }

                    return;
                }
            }
        }

        Svc.Chat.PrintError(UiString.CommandsInsertActionFailure.GetDescription());
    }

    private static void DoRotationCommand(ICustomRotation customCombo, string str)
    {
        IRotationConfigSet configs = customCombo.Configs;
        foreach (IRotationConfig config in configs)
        {
            bool result = config.DoCommand(configs, str);
            if (result)
            {
                if (Service.Config.ShowToggledSettingInChat)
                {
                    Svc.Chat.Print($"Changed setting {config.DisplayName} to {config.Value}");
                }
                return;
            }
        }

        // Only log if all commands failed
        //PluginLog.Debug(UiString.CommandsInsertActionFailure.GetDescription());
    }

    private static void DoDutyRotationCommand(DutyRotation dutyRotation, string str)
    {
        IRotationConfigSet configs = dutyRotation.Configs;
        foreach (IRotationConfig config in configs)
        {
            if (config.DoCommand(configs, str))
            {
                if (Service.Config.ShowToggledSettingInChat)
                {
                    Svc.Chat.Print($"Changed setting {config.DisplayName} to {config.Value}");
                }
                return;
            }
        }

        // Only log if all commands failed
        //PluginLog.Debug(UiString.CommandsInsertActionFailure.GetDescription());
    }
}