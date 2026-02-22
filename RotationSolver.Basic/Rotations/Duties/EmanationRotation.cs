namespace RotationSolver.Basic.Rotations.Duties;

/// <summary>
/// The variant action.
/// </summary>
[DutyTerritory(719, 720)]
public abstract class EmanationRotation : DutyRotation
{
}

public partial class DutyRotation
{
    /// <summary>
    /// Modifies the settings for Vril PvE.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyVrilPvE(ref ActionSetting setting)
    {
        setting.StatusFromSelf = true;
        setting.TargetType = TargetType.Self;
        setting.TargetStatusProvide = [StatusID.Vril];
        setting.ActionCheck = () => DataCenter.IsLakshmiCastingSpecialIndicator();
	}

    /// <summary>
    /// Modifies the settings for Vril PvE with ID 9345.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyVrilPvE_9345(ref ActionSetting setting)
    {
        setting.StatusFromSelf = true;
        setting.TargetType = TargetType.Self;
        setting.TargetStatusProvide = [StatusID.Vril];
		setting.ActionCheck = () => DataCenter.IsLakshmiCastingSpecialIndicator();
	}
}