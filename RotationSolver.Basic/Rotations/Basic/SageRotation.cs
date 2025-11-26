namespace RotationSolver.Basic.Rotations.Basic;

public partial class SageRotation
{
    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Mind;

    #region Job Gauge
    /// <summary>
    /// Gets a value indicating whether Eukrasia is activated. Eukrasia = 1, none = 0
    /// </summary>
    public static bool HasEukrasia => JobGauge.Eukrasia;

    /// <summary>
    /// Gets the amount of Addersgall available.
    /// </summary>
    public static byte Addersgall => AddersgallTrait.EnoughLevel ? JobGauge.Addersgall : (byte)0;

    /// <summary>
    /// Gets the amount of Addersting available.
    /// </summary>
    public static byte Addersting => AdderstingTrait.EnoughLevel ? JobGauge.Addersting : (byte)0;

    private static float AddersgallTimerRaw => JobGauge.AddersgallTimer / 1000f;

    /// <summary>
    /// Gets the amount of milliseconds elapsed until the next Addersgall is available.
    /// This counts from 0 to 20_000.
    /// </summary>
    public static float AddersgallTime => AddersgallTrait.EnoughLevel ? AddersgallTimerRaw - DataCenter.DefaultGCDRemain : 0;

    /// <summary>
    /// Used to determine if the cooldown for the next Addersgall will end within a specified time frame.
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    protected static bool AddersgallEndAfter(float time)
    {
        return AddersgallTime <= time;
    }

    /// <summary>
    /// Used to determine if the cooldown for the next Addersgall will end within a specified number of GCDs.
    /// </summary>
    /// <param name="gctCount"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    protected static bool AddersgallEndAfterGCD(uint gctCount = 0, float offset = 0)
    {
        return AddersgallEndAfter(GCDTime(gctCount, offset));
    }

    /// <inheritdoc/>
    public override void DisplayBaseStatus()
    {
        ImGui.Text("HasEukrasia: " + HasEukrasia.ToString());
        ImGui.Text("Addersgall: " + Addersgall.ToString());
        ImGui.Text("Addersting: " + Addersting.ToString());
        ImGui.Text("AddersgallTime: " + AddersgallTime.ToString());
    }
    #endregion

    #region Status Tracking
    /// <summary>
    /// Player has Kardia.
    /// </summary>
    public static bool HasKardia => Player.HasStatus(true, StatusID.Kardia);
    #endregion

    #region PvE Actions
    private protected sealed override IBaseAction Raise => EgeiroPvE;

    static partial void ModifyDosisPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyDiagnosisPvE(ref ActionSetting setting)
    {
        setting.TargetType = TargetType.BeAttacked;
        setting.CreateConfig = () => new ActionConfig()
        {
            GCDSingleHeal = true,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifyKardiaPvE(ref ActionSetting setting)
    {
        setting.TargetType = TargetType.Kardia;
        setting.TargetStatusProvide = [StatusID.Kardion];
        setting.ActionCheck = () => !DataCenter.PartyMembers.Any(m => m.HasStatus(true, StatusID.Kardion));
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 0,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifyPrognosisPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyEgeiroPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
        setting.ActionCheck = () => Player.CurrentMp >= RaiseMPMinimum;
    }

    static partial void ModifyPhysisPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyPhlegmaPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyEukrasiaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !HasEukrasia;
        setting.StatusProvide = [StatusID.Eukrasia];
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            IsIntercepted = false,
        };
    }

    static partial void ModifyEukrasianDiagnosisPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.EukrasianDiagnosis, StatusID.Galvanize]; // Effect cannot be stacked with scholar's Galvanize.
        setting.TargetType = TargetType.BeAttacked;
        setting.StatusFromSelf = false;
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 0,
        };
    }

    static partial void ModifyEukrasianPrognosisPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.EukrasianPrognosis];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyEukrasianDosisPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide =
        [
            StatusID.EukrasianDosis,
            StatusID.EukrasianDosisIi,
            StatusID.EukrasianDosisIii,
            StatusID.EukrasianDyskrasia
        ];
        setting.CreateConfig = () => new ActionConfig()
        {
            IsRestrictedDOT = true,
        };
    }

    static partial void ModifySoteriaPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Soteria];
        setting.TargetType = TargetType.Self;
        setting.ActionCheck = () => DataCenter.PartyMembers.Any(m => m.HasStatus(true, StatusID.Kardion));
    }

    static partial void ModifyIcarusPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.HostileFriendlyMovingForward;
    }

    static partial void ModifyDruocholePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Addersgall >= 1;
    }

    static partial void ModifyDyskrasiaPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyKeracholePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Addersgall >= 1;
        setting.StatusProvide = [StatusID.Kerachole, StatusID.Kerakeia];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyIxocholePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Addersgall >= 1;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyZoePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Zoe];
    }

    static partial void ModifyPepsisPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.EukrasianPrognosis];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyPhysisIiPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyTaurocholePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Addersgall >= 1;
        setting.TargetStatusProvide = [StatusID.Taurochole];
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 0,
        };
    }

    static partial void ModifyToxikonPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Addersting >= 1;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyHaimaPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Haima];
    }

    static partial void ModifyDosisIiPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyPhlegmaIiPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyEukrasianDosisIiPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide =
        [
            StatusID.EukrasianDosis,
            StatusID.EukrasianDosisIi,
            StatusID.EukrasianDosisIii,
            StatusID.EukrasianDyskrasia
        ];
        setting.CreateConfig = () => new ActionConfig()
        {
            IsRestrictedDOT = true,
        };
    }

    static partial void ModifyRhizomataPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Addersting < 3;
    }

    static partial void ModifyHolosPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyPanhaimaPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Panhaima, StatusID.Panhaimatinon];
        setting.UnlockedByQuestID = 69608;
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyDosisIiiPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyPhlegmaIiiPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyEukrasianDosisIiiPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide =
        [
            StatusID.EukrasianDosis,
            StatusID.EukrasianDosisIi,
            StatusID.EukrasianDosisIii,
            StatusID.EukrasianDyskrasia
        ];
        setting.CreateConfig = () => new ActionConfig()
        {
            IsRestrictedDOT = true,
        };
    }

    static partial void ModifyDyskrasiaIiPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyEukrasianDyskrasiaPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide =
        [
            StatusID.EukrasianDyskrasia
        ];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyToxikonIiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Addersting >= 1;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyKrasisPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Krasis];
    }

    static partial void ModifyPneumaPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyPsychePvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyEukrasianPrognosisIiPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.EukrasianPrognosis];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyPhilosophiaPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Philosophia];
        setting.TargetStatusProvide = [StatusID.Eudaimonia];
        setting.TargetType = TargetType.Self;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion

    #region PvP Actions
    static partial void ModifyDosisIiiPvP(ref ActionSetting setting)
    {

    }

    static partial void ModifyPhlegmaIiiPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyPneumaPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };

    }

    static partial void ModifyEukrasiaPvP(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Eukrasia_3107];
        setting.IsFriendly = true;
    }

    static partial void ModifyIcarusPvP(ref ActionSetting setting)
    {
        //setting.SpecialType = SpecialActionType.MovingForward;
    }

    static partial void ModifyToxikonPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyKardiaPvP(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Kardia_2871];
        setting.TargetStatusProvide = [StatusID.Kardion_2872];
        setting.TargetType = TargetType.Kardia;
    }

    static partial void ModifyEukrasianDosisIiiPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.DosisIiiPvP) == ActionID.EukrasianDosisIiiPvP;
    }

    static partial void ModifyPsychePvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyToxikonIiPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Player.HasStatus(true, StatusID.Addersting);
        setting.TargetStatusProvide = [StatusID.Toxikon];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }


    /// <inheritdoc/>
    [RotationDesc(ActionID.IcarusPvE)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        return IcarusPvE.CanUse(out act) || base.MoveForwardAbility(nextGCD, out act);
    }
    #endregion
}
