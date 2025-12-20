namespace RotationSolver.Basic.Rotations.Basic;

public partial class SamuraiRotation
{
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
    public static byte SenCount
    {
        get
        {
            byte count = 0;
            if (HasGetsu)
            {
                count++;
            }

            if (HasSetsu)
            {
                count++;
            }

            if (HasKa)
            {
                count++;
            }

            return count;
        }
    }
    #endregion

    #region Old Status Tracking

    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Strength;

    /// <summary>
    /// 
    /// </summary>
    public static bool HasMoon => StatusHelper.PlayerHasStatus(true, StatusID.Fugetsu);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasFlower => StatusHelper.PlayerHasStatus(true, StatusID.Fuka);

    /// <summary>
    /// 
    /// </summary>
    public static bool IsMoonTimeLessThanFlower => StatusHelper.PlayerStatusTime(true, StatusID.Fugetsu) < StatusHelper.PlayerStatusTime(true, StatusID.Fuka);
    #endregion

    #region Status Tracking
    /// <summary>
    /// 
    /// </summary>
    public static bool HasMeikyoShisui => StatusHelper.PlayerHasStatus(true, StatusID.MeikyoShisui);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasTendo => StatusHelper.PlayerHasStatus(true, StatusID.Tendo);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasTsubamegaeshiReady => StatusHelper.PlayerHasStatus(true, StatusID.Tsubamegaeshi);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasOgiNamikiri => StatusHelper.PlayerHasStatus(true, StatusID.OgiNamikiri);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasZanshinReady1318 => StatusHelper.PlayerHasStatus(true, StatusID.ZanshinReady);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasZanshinReady => StatusHelper.PlayerHasStatus(true, StatusID.ZanshinReady_3855);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasFugetsuAndFuka => HasFugetsu && HasFuka;

    /// <summary>
    /// 
    /// </summary>
    public static bool WillFugetsuEnd => StatusHelper.PlayerWillStatusEnd(5, true, StatusID.Fugetsu);

    /// <summary>
    /// 
    /// </summary>
    public static bool WillFukaEnd => StatusHelper.PlayerWillStatusEnd(5, true, StatusID.Fuka);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasFugetsu => StatusHelper.PlayerHasStatus(true, StatusID.Fugetsu);

    /// <summary>
    /// 
    /// </summary>
    public static bool HasFuka => StatusHelper.PlayerHasStatus(true, StatusID.Fuka);

    /// <summary>
    /// 
    /// </summary>
    public static float? FugetsuTime => StatusHelper.PlayerStatusTime(true, StatusID.Fugetsu);

    /// <summary>
    /// 
    /// </summary>
    public static float? FukaTime => StatusHelper.PlayerStatusTime(true, StatusID.Fuka);

    /// <summary>
    /// 
    /// </summary>
    public static string? FugetsuOrFukaEndsFirst
    {
        get
        {
            if (!HasFugetsuAndFuka)
                return null;
            if (FugetsuTime == null || FukaTime == null)
                return null;
            if (FugetsuTime < FukaTime)
                return "Fugetsu";
            if (FukaTime < FugetsuTime)
                return "Fuka";
            return "Equal";
        }
    }
    #endregion

    #region Actions Unassignable
    /// <summary>
    /// 
    /// </summary>
    public static bool HiganbanaReady => Service.GetAdjustedActionId(ActionID.IaijutsuPvE) == ActionID.HiganbanaPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool TenkaGokenReady => Service.GetAdjustedActionId(ActionID.IaijutsuPvE) == ActionID.TenkaGokenPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool MidareSetsugekkaReady => Service.GetAdjustedActionId(ActionID.IaijutsuPvE) == ActionID.MidareSetsugekkaPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool KaeshiGokenReady => Service.GetAdjustedActionId(ActionID.TsubamegaeshiPvE) == ActionID.KaeshiGokenPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool KaeshiSetsugekkaReady => Service.GetAdjustedActionId(ActionID.TsubamegaeshiPvE) == ActionID.KaeshiSetsugekkaPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool KaeshiNamikiriReady => Service.GetAdjustedActionId(ActionID.OgiNamikiriPvE) == ActionID.KaeshiNamikiriPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool TendoGokenReady => Service.GetAdjustedActionId(ActionID.IaijutsuPvE) == ActionID.TendoGokenPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool TendoSetsugekkaReady => Service.GetAdjustedActionId(ActionID.IaijutsuPvE) == ActionID.TendoSetsugekkaPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool TendoKaeshiGokenReady => Service.GetAdjustedActionId(ActionID.TsubamegaeshiPvE) == ActionID.TendoKaeshiGokenPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool TendoKaeshiSetsugekkaReady => Service.GetAdjustedActionId(ActionID.TsubamegaeshiPvE) == ActionID.TendoKaeshiSetsugekkaPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool TsubamegaeshiActionReady => Service.GetAdjustedActionId(ActionID.TsubamegaeshiPvE) != ActionID.TsubamegaeshiPvE;
    #endregion

    #region Debug
    /// <inheritdoc/>
    public override void DisplayBaseStatus()
    {
        ImGui.Text("HasSetsu: " + HasSetsu.ToString());
        ImGui.Text("HasGetsu: " + HasGetsu.ToString());
        ImGui.Text("HasKa: " + HasKa.ToString());
        ImGui.Text("Kenki: " + Kenki.ToString());
        ImGui.Text("MeditationStacks: " + MeditationStacks.ToString());
        ImGui.Text("Kaeshi: " + Kaeshi.ToString());
        ImGui.Text("SenCount: " + SenCount.ToString());
        ImGui.Text("HasMoon: " + HasMoon.ToString());
        ImGui.Text("HasFlower: " + HasFlower.ToString());
        ImGui.Text("HaveMeikyoShisui: " + HasMeikyoShisui.ToString());
        ImGui.Text("HiganbanaReady: " + HiganbanaReady.ToString());
        ImGui.Text("TenkaGokenReady: " + TenkaGokenReady.ToString());
        ImGui.Text("MidareSetsugekkaReady: " + MidareSetsugekkaReady.ToString());
        ImGui.Text("KaeshiGokenReady: " + KaeshiGokenReady.ToString());
        ImGui.Text("KaeshiSetsugekkaReady: " + KaeshiSetsugekkaReady.ToString());
        ImGui.Text("KaeshiNamikiriReady: " + KaeshiNamikiriReady.ToString());
        ImGui.Text("TendoGokenReady: " + TendoGokenReady.ToString());
        ImGui.Text("TendoSetsugekkaReady: " + TendoSetsugekkaReady.ToString());
        ImGui.Text("TendoKaeshiGokenReady: " + TendoKaeshiGokenReady.ToString());
        ImGui.Text("TendoKaeshiSetsugekkaReady: " + TendoKaeshiSetsugekkaReady.ToString());
    }
    #endregion

    #region PvE Actions

    static partial void ModifyHakazePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki <= 95;
    }

    static partial void ModifyJinpuPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki <= 95;
        setting.ComboIds = [ActionID.HakazePvE, ActionID.GyofuPvE];
    }

    static partial void ModifyEnpiPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MeleeRange;
    }

    static partial void ModifyThirdEyePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.ThirdEye];
        setting.IsFriendly = true;
        setting.TargetType = TargetType.Self;
    }

    static partial void ModifyShifuPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki <= 95;
        setting.ComboIds = [ActionID.HakazePvE, ActionID.GyofuPvE];
    }

    static partial void ModifyFugaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki <= 95;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyGekkoPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.JinpuPvE];
        setting.ActionCheck = () => Kenki <= 90;
    }

    static partial void ModifyIaijutsuPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyMangetsuPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.FukoPvE];
        setting.ActionCheck = () => Kenki <= 90;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyKashaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki <= 90;
        setting.ComboIds = [ActionID.ShifuPvE];
    }

    static partial void ModifyOkaPvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.FukoPvE];
        setting.ActionCheck = () => Kenki <= 90;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyYukikazePvE(ref ActionSetting setting)
    {
        setting.ComboIds = [ActionID.HakazePvE, ActionID.GyofuPvE];
        setting.ActionCheck = () => Kenki <= 85;
    }

    static partial void ModifyMeikyoShisuiPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.MeikyoShisui, StatusID.Tendo];
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 0,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifyHissatsuShintenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki >= 25;
    }

    static partial void ModifyHissatsuGyotenPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.HostileMovingForward;
        setting.ActionCheck = () => Kenki >= 10;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyHissatsuYatenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki >= 10;
    }

    static partial void ModifyMeditatePvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 68101;
        setting.ActionCheck = () => !IsMoving;
        setting.IsFriendly = true;
    }

    static partial void ModifyHissatsuKyutenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki >= 25;
    }

    static partial void ModifyHagakurePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (SenCount == 1 && Kenki <= 90) || (SenCount == 2 && Kenki <= 80) || (SenCount == 3 && Kenki <= 70);
        setting.IsFriendly = true;
    }

static partial void ModifyIkishotenPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.OgiNamikiriReady, StatusID.ZanshinReady];
        setting.ActionCheck = () => InCombat && Kenki >= 50;
        setting.IsFriendly = true;
    }

    static partial void ModifyHissatsuGurenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki >= 25;
        setting.UnlockedByQuestID = 68106;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyHissatsuSeneiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki >= 25;
    }

    static partial void ModifyTsubamegaeshiPvE(ref ActionSetting setting)
    {
        
    }

    static partial void ModifyShohaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InCombat && MeditationStacks == 3;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyTengentsuPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Tengentsu];
        setting.IsFriendly = true;
        setting.TargetType = TargetType.Self;
    }

    static partial void ModifyFukoPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki <= 90;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyOgiNamikiriPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.OgiNamikiriReady];
        setting.ActionCheck = () => MeditationStacks <= 2;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyGyofuPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Kenki <= 95;
    }

    static partial void ModifyZanshinPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.ZanshinReady_3855];
        setting.ActionCheck = () => Kenki >= 50;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    //Iaijutsu 

    static partial void ModifyHiganbanaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SenCount == 1;
        setting.TargetStatusProvide = [StatusID.Higanbana];
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 48,
            StatusGcdCount = 6,
        };
    }

    static partial void ModifyTenkaGokenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SenCount == 2 && !HasTendo;
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyMidareSetsugekkaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SenCount == 3 && !HasTendo;
    }

    static partial void ModifyKaeshiGokenPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.TsubamegaeshiReady];
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyKaeshiSetsugekkaPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Tsubamegaeshi];
    }

    static partial void ModifyKaeshiNamikiriPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => KaeshiNamikiriReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyTendoGokenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SenCount == 2;
        setting.StatusNeed = [StatusID.Tendo];
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyTendoSetsugekkaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SenCount == 3;
        setting.StatusProvide = [StatusID.Tsubamegaeshi];
        setting.StatusNeed = [StatusID.Tendo];
    }

    static partial void ModifyTendoKaeshiGokenPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Tsubamegaeshi_4217];
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyTendoKaeshiSetsugekkaPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Tsubamegaeshi_4218];
    }

    #endregion

    #region PvP Actions

    static partial void ModifyYukikazePvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyGekkoPvP(ref ActionSetting setting)
    {
    }

    static partial void ModifyKashaPvP(ref ActionSetting setting)
    {
    }
    static partial void ModifyOgiNamikiriPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyHissatsuChitenPvP(ref ActionSetting setting)
    {

    }

    static partial void ModifyMineuchiPvP(ref ActionSetting setting)
    {
        setting.TargetStatusNeed = [StatusID.Kuzushi];
    }

    static partial void ModifyMeikyoShisuiPvP(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
    }

    static partial void ModifyHyosetsuPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.YukikazePvP) == ActionID.HyosetsuPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyMangetsuPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.YukikazePvP) == ActionID.MangetsuPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyOkaPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.YukikazePvP) == ActionID.OkaPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyKaeshiNamikiriPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.OgiNamikiriPvP) == ActionID.KaeshiNamikiriPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyZanshinPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.HissatsuChitenPvP) == ActionID.ZanshinPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyTendoSetsugekkaPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.MeikyoShisuiPvP) == ActionID.TendoSetsugekkaPvP;
    }

    static partial void ModifyTendoKaeshiSetsugekkaPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.MeikyoShisuiPvP) == ActionID.TendoKaeshiSetsugekkaPvP;
    }


    static partial void ModifyHissatsuSotenPvP(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Kaiten_3201];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion

    /// <inheritdoc/>
    public override bool IsBursting()
    {
        return StatusHelper.PlayerHasStatus(true, StatusID.Fugetsu) && StatusHelper.PlayerHasStatus(true, StatusID.Fuka);
    }
}
