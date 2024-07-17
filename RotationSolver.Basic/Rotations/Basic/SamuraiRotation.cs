namespace RotationSolver.Basic.Rotations.Basic;

partial class SamuraiRotation
{
    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Strength;

    /// <summary>
    /// 
    /// </summary>
    public static bool HasMoon => Player.HasStatus(true, StatusID.Fugetsu);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasFlower => Player.HasStatus(true, StatusID.Fuka);

    /// <summary>
    /// 
    /// </summary>
    public static bool IsMoonTimeLessThanFlower
        => Player.StatusTime(true, StatusID.Fugetsu) < Player.StatusTime(true, StatusID.Fuka);

    #region JobGauge
    /// <summary>
    /// 
    /// </summary>
    public static bool HasSetsu => JobGauge.HasSetsu;

    /// <summary>
    /// 
    /// </summary>
    public static bool HasGetsu => JobGauge.HasGetsu;

    /// <summary>
    /// 
    /// </summary>
    public static bool HasKa => JobGauge.HasKa;

    /// <summary>
    /// 
    /// </summary>
    public static byte Kenki => JobGauge.Kenki;

    /// <summary>
    /// 
    /// </summary>
    public static byte MeditationStacks => JobGauge.MeditationStacks;

    /// <summary>
    /// 
    /// </summary>
    public static Kaeshi Kaeshi => JobGauge.Kaeshi;

    /// <summary>
    /// 
    /// </summary>
    public static byte SenCount => (byte)((HasGetsu ? 1 : 0) + (HasSetsu ? 1 : 0) + (HasKa ? 1 : 0));
    #endregion

    static partial void ModifyShohaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => MeditationStacks == 3;
    }

    // static partial void ModifyShohaIiPvE(ref ActionSetting setting)
    // {
    //     setting.ActionCheck = () => MeditationStacks == 3;
    // }
    
    static partial void ModifyJinpuPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.HakazePvE, ActionID.GyofuPvE];
    }
    
    static partial void ModifyShifuPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.HakazePvE, ActionID.GyofuPvE];
    }
    
    static partial void ModifyYukikazePvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.HakazePvE, ActionID.GyofuPvE];
    }
    
    static partial void ModifyTendoSetsugekkaPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Tendo];
    }
    
    static partial void ModifyTendoKaeshiSetsugekkaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (byte)Kaeshi == 6; // Temporary until Dalamud enums are updated
    }
    
    static partial void ModifyTendoKaeshiGokenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (byte)Kaeshi == 5; // Temporary until Dalamud enums are updated
    }
    
    static partial void ModifyTendoGokenPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Tendo];
        setting.IsFriendly = false;
    }
    
    static partial void ModifyMangetsuPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.FugaPvE, ActionID.FukoPvE];
    }
    
    static partial void ModifyZanshinPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.ZanshinReady];
        setting.ActionCheck = () => Kenki >= 50;
    }
    
    static partial void ModifyOkaPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.FugaPvE, ActionID.FukoPvE];
    }

    static partial void ModifyOgiNamikiriPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.OgiNamikiriReady];
        setting.ActionCheck = () => !IsMoving;
    }

    static partial void ModifyKaeshiNamikiriPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kaeshi == Kaeshi.NAMIKIRI;
    }

    static partial void ModifyHiganbanaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SenCount == 1;
        setting.TargetStatusProvide = [StatusID.Higanbana];
    }

    static partial void ModifyTenkaGokenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SenCount == 2;
        setting.IsFriendly = false;
    }
    static partial void ModifyMidareSetsugekkaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SenCount == 3;
    }

    static partial void ModifyKaeshiGokenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kaeshi == Kaeshi.GOKEN;
        setting.IsFriendly = false;
    }

    static partial void ModifyKaeshiSetsugekkaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kaeshi == Kaeshi.SETSUGEKKA;
    }

    static partial void ModifyEnpiPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MeleeRange;
    }

    static partial void ModifyMeikyoShisuiPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.MeikyoShisui];
        setting.CreateConfig = () => new()
        {
            TimeToKill = 0,
        };
    }

    static partial void ModifyHagakurePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SenCount > 0;
    }

    static partial void ModifyIkishotenPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.OgiNamikiriReady];
        setting.StatusProvide = [StatusID.ZanshinReady];
        setting.ActionCheck = () => InCombat;
    }

    static partial void ModifyHissatsuShintenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki >= 25;
    }

    static partial void ModifyHissatsuGyotenPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
        setting.ActionCheck = () => Kenki >= 10;
    }

    static partial void ModifyHissatsuYatenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki >= 10;
    }

    static partial void ModifyHissatsuKyutenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki >= 25;
    }

    static partial void ModifyHissatsuGurenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki >= 25;
        setting.UnlockedByQuestID = 68106;
    }

    static partial void ModifyHissatsuSeneiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki >= 25;
    }

    static partial void ModifyHissatsuSotenPvP(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
    }

    /// <inheritdoc/>
    [RotationDesc(ActionID.HissatsuGyotenPvE)]
    protected sealed override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (HissatsuGyotenPvE.CanUse(out act)) return true;
        return base.MoveForwardAbility(nextGCD, out act);
    }

    /// <inheritdoc/>
    [RotationDesc(ActionID.FeintPvE)]
    protected sealed override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (FeintPvE.CanUse(out act)) return true;
        return base.DefenseAreaAbility(nextGCD, out act);
    }

    /// <inheritdoc/>
    [RotationDesc(ActionID.ThirdEyePvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (TengentsuPvE.CanUse(out act)) return true;
        if (ThirdEyePvE.CanUse(out act)) return true;
        return base.DefenseSingleAbility(nextGCD, out act);
    }
}
