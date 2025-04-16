using ECommons.DalamudServices;
using RotationSolver.Basic.Configuration;
using RotationSolver.Updaters;

namespace RotationSolver.Commands;

public static partial class RSCommands
{
    private static void DoOtherCommand(OtherCommandType otherType, string str)
    {
        switch (otherType)
        {
            case OtherCommandType.Rotations:
                ExecuteRotationCommand(str);
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

    private static void ExecuteRotationCommand(string str)
    {
        var customCombo = DataCenter.CurrentRotation;
        if (customCombo == null) return;

        DoRotationCommand(customCombo, str);
    }

    private static void DoSettingCommand(string str)
    {
        var strs = str.Split(' ', 3);
        if (strs.Length < 2)
        {
            Svc.Chat.PrintError("Invalid setting command format.");
            return;
        }

        var settingName = strs[0];
        var command = strs.Length > 1 ? string.Join(' ', strs.Skip(1)) : null;

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
        foreach (var property in typeof(Configs).GetRuntimeProperties().Where(p => p.GetMethod?.IsPublic ?? false))
        {
            if (!settingName.Equals(property.Name, StringComparison.OrdinalIgnoreCase))
                continue;

            var type = property.PropertyType == typeof(ConditionBoolean) ? typeof(bool) : property.PropertyType;
            if (!TryConvertValue(type, command, out var convertedValue))
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
                    var relay = (ConditionBoolean)property.GetValue(Service.Config)!;
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

        var commandParts = command.Split(' ', 2);
        if (commandParts.Length < 1)
        {
            Svc.Chat.PrintError("Invalid command format for TargetingTypes.");
            return;
        }

        var action = commandParts[0];
        var value = commandParts.Length > 1 ? commandParts[1] : null;

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
                Svc.Chat.Print("Removed all TargetingTypes.");
                break;

            default:
                Svc.Chat.PrintError("Invalid action for TargetingTypes.");
                break;
        }

        Service.Config.Save();
    }

    private static void AddTargetingType(string? value)
    {
        if (string.IsNullOrEmpty(value) || !Enum.TryParse(typeof(TargetingType), value, true, out var parsedEnumAdd))
        {
            Svc.Chat.PrintError("Invalid TargetingType value.");
            return;
        }

        var targetingTypeAdd = (TargetingType)parsedEnumAdd;
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
        if (string.IsNullOrEmpty(value) || !Enum.TryParse(typeof(TargetingType), value, true, out var parsedEnumRemove))
        {
            Svc.Chat.PrintError("Invalid TargetingType value.");
            return;
        }

        var targetingTypeRemove = (TargetingType)parsedEnumRemove;
        if (Service.Config.TargetingTypes.Contains(targetingTypeRemove))
        {
            Service.Config.TargetingTypes.Remove(targetingTypeRemove);
            Svc.Chat.Print($"Removed {targetingTypeRemove} from TargetingTypes.");
        }
        else
        {
            Svc.Chat.Print($"{targetingTypeRemove} is not in TargetingTypes.");
        }
    }

    private static Enum GetNextEnumValue(Enum currentEnumValue)
    {
        var enumValues = Enum.GetValues(currentEnumValue.GetType()).Cast<Enum>().ToArray();
        var nextIndex = Array.IndexOf(enumValues, currentEnumValue) + 1;

        return enumValues.Length == nextIndex ? enumValues[0] : enumValues[nextIndex];
    }

    private static void ToggleActionCommand(string str)
    {
        foreach (var act in RotationUpdater.CurrentRotationActions)
        {
            if (str.StartsWith(act.Name))
            {
                var flag = str[act.Name.Length..].Trim();
                act.IsEnabled = bool.TryParse(flag, out var parse) ? parse : !act.IsEnabled;

                if (Service.Config.ShowToggledSettingInChat)
                {
                    Svc.Chat.Print($"Toggled {act.Name} : {act.IsEnabled}");
                }

                return;
            }
        }
    }

    private static void DoActionCommand(string str)
    {
        var lastHyphenIndex = str.LastIndexOf('-');
        if (lastHyphenIndex == -1 || lastHyphenIndex == str.Length - 1)
        {
            Svc.Chat.PrintError("Cannot find the action. Please check the action name.");
            return;
        }

        var actName = str.Substring(0, lastHyphenIndex).Trim();
        var timeStr = str.Substring(lastHyphenIndex + 1).Trim();

        if (double.TryParse(timeStr, out var time))
        {
            foreach (var iAct in RotationUpdater.CurrentRotationActions)
            {
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

        Svc.Chat.PrintError("Cannot find the action. Please check the action name.");
    }

    private static void DoRotationCommand(ICustomRotation customCombo, string str)
    {
        var configs = customCombo.Configs;
        foreach (var config in configs)
        {
            if (config.DoCommand(configs, str))
            {
                if (Service.Config.ShowToggledSettingInChat)
                {
                    Svc.Chat.Print($"Changed setting {config.DisplayName} to {config.Value}");
                    return;
                }
            }
        }

        Svc.Chat.PrintError("Cannot find the action. Please check the action name.");
    }
}
