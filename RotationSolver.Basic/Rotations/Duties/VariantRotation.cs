namespace RotationSolver.Basic.Rotations.Duties;

/// <summary>
/// Represents a rotation for variant duties in the game.
/// </summary>
[DutyTerritory(1069, 1137, 1176)] // 1069: Sildihn Subterrane, 1137: Mount Rokkon, 1176: Aloalo Island
public abstract class VariantRotation : DutyRotation
{
}

public partial class DutyRotation
{
    /// <summary>
    /// Modifies the settings for Variant Cure PvE.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyVariantRaisePvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.VariantRaiseSet];
        setting.TargetType = TargetType.Death;
    }

    /// <summary>
    /// Modifies the settings for Variant Cure PvE.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyVariantRaiseIiPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.VariantRaiseSet];
        setting.TargetType = TargetType.Death;
    }

    /// <summary>
    /// Modifies the settings for Variant Cure PvE with ID 33862.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyVariantCurePvE_33862(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.VariantCureSet];
        setting.StatusProvide = [StatusID.Rehabilitation_3367];
    }

    /// <summary>
    /// Modifies the settings for Variant Cure PvE.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyVariantCurePvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.VariantCureSet];
        setting.StatusProvide = [StatusID.Rehabilitation_3367];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyVariantUltimatumPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.VariantUltimatumSet];
        setting.TargetType = TargetType.Provoke;
        setting.StatusProvide = [StatusID.EnmityUp];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyVariantRampartPvE_33864(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.VariantRampartSet];
        setting.TargetType = TargetType.Self;
        setting.StatusProvide = [StatusID.VulnerabilityDown_3360];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyVariantRampartPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.VariantRampartSet];
        setting.TargetType = TargetType.Self;
        setting.StatusProvide = [StatusID.VulnerabilityDown_3360];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyVariantSpiritDartPvE_33863(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.VariantSpiritDartSet];
        setting.TargetStatusProvide = [StatusID.SustainedDamage_3359];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyVariantSpiritDartPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.VariantSpiritDartSet];
        setting.TargetStatusProvide = [StatusID.SustainedDamage_3359];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyVariantEagleEyeShotPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.VariantEagleEyeShotSet];
        setting.TargetType = TargetType.HighHP;
		setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
}