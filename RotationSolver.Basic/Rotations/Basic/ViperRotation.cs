namespace RotationSolver.Basic.Rotations.Basic;

public partial class ViperRotation
{
    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Dexterity;

    /// <inheritdoc/>
    public override bool IsBursting()
    {
        if (HasHunterAndSwift)
        {
            return true;
        }
        return false;
    }

    #region JobGauge
    /// <summary>
    /// Gets how many uses of uncoiled fury the player has.
    /// </summary>
    public static byte RattlingCoilStacks => JobGauge.RattlingCoilStacks;

    /// <summary>
    /// Gets Max stacks of Rattling Coil.
    /// </summary>
    public static byte MaxRattling => EnhancedVipersRattleTrait.EnoughLevel ? (byte)3 : (byte)2;

    /// <summary>
    /// Gets Serpent Offering stacks and gauge.
    /// </summary>
    public static byte SerpentOffering => JobGauge.SerpentOffering;

    /// <summary>
    ///  Gets value indicating the use of 1st, 2nd, 3rd, 4th generation and Ouroboros.
    /// </summary>
    public static byte AnguineTributeStacks => JobGauge.AnguineTribute;

    /// <summary>
    /// Gets Max stacks of Anguine Tribute.
    /// </summary>
    public static byte MaxAnguine => EnhancedSerpentsLineageTrait.EnoughLevel ? (byte)5 : (byte)4;

    #region DreadCombo
    /// <summary>
    /// Gets the last Weaponskill used in DreadWinder/Pit of Dread combo. Dreadwinder = 1, HuntersCoil, SwiftskinsCoil, PitOfDread, HuntersDen, SwiftskinsDen
    /// </summary>
    public static DreadCombo DreadCombo => JobGauge.DreadCombo;

    /// <summary>
    /// Indicates that the player is not in a Dread Combo.
    /// </summary>
    public static bool NODREAD => JobGauge.DreadCombo == 0 || ((byte)JobGauge.DreadCombo > 6 && !HasReawakenedActive);

    /// <summary>
    /// Indicates that the player has Dread Combo active and both HuntersCoil and SwiftskinsCoil available.
    /// </summary>
    public static bool DreadActive => JobGauge.DreadCombo.HasFlag(DreadCombo.Dreadwinder);

    /// <summary>
    /// Indicates that the player has Dread Combo active and only SwiftskinsCoil available.
    /// </summary>
    public static bool SwiftskinsCoilOnly => JobGauge.DreadCombo.HasFlag(DreadCombo.HuntersCoil);

    /// <summary>
    /// Indicates that the player has Dread Combo active and only HuntersCoil available.
    /// </summary>
    public static bool HuntersCoilOnly => JobGauge.DreadCombo.HasFlag(DreadCombo.SwiftskinsCoil);

    /// <summary>
    /// Indicates that the player has Pit Combo active and both HuntersDen and SwiftskinsDen available.
    /// </summary>
    public static bool PitActive => JobGauge.DreadCombo.HasFlag(DreadCombo.PitOfDread);

    /// <summary>
    /// Indicates that the player has Pit Combo active and only SwiftskinsDen available.
    /// </summary>
    public static bool SwiftskinsDenOnly => JobGauge.DreadCombo.HasFlag(DreadCombo.HuntersDen);

    /// <summary>
    /// Indicates that the player has Pit Combo active and only HuntersDen available.
    /// </summary>
    public static bool HuntersDenOnly => JobGauge.DreadCombo.HasFlag(DreadCombo.SwiftskinsDen);

    // DreadCombo also indicates the states for Reawakend

    /// <summary>
    ///
    /// </summary>
    public static bool FirstGenReady => (byte)JobGauge.DreadCombo == 7;

    /// <summary>
    ///
    /// </summary>
    public static bool SecondGenReady => (byte)JobGauge.DreadCombo == 8;

    /// <summary>
    ///
    /// </summary>
    public static bool ThirdGenReady => (byte)JobGauge.DreadCombo == 9;

    /// <summary>
    ///
    /// </summary>
    public static bool FourthGenReady => (byte)JobGauge.DreadCombo == 10;

    #endregion

    #region SerpentAbilities
    /// <summary>
    /// Gets current ability for Serpent's Tail. NONE = 0, DEATHRATTLE = 1, LASTLASH = 2, FIRSTLEGACY = 3, SECONDLEGACY = 4, THIRDLEGACY = 5, FOURTHLEGACY = 6, TWINSREADY = 7, THRESHREADY = 8, UNCOILEDREADY = 9
    /// </summary>
    public static SerpentCombo SerpentCombo => JobGauge.SerpentCombo;

    /// <summary>
    /// Indicates if base no abilities are ready.
    /// </summary>
    public static bool NoAbilityReady => JobGauge.SerpentCombo.HasFlag(SerpentCombo.None);

    /// <summary>
    /// Indicates if base Death Rattle oGCD is ready.
    /// </summary>
    public static bool DeathRattleReady => JobGauge.SerpentCombo.HasFlag(SerpentCombo.DeathRattle);

    /// <summary>
    /// Indicates if base Last Lash oGCD is ready.
    /// </summary>
    public static bool LastLashReady => JobGauge.SerpentCombo.HasFlag(SerpentCombo.LastLash);

    /// <summary>
    /// Indicates if base First Legacy oGCD is ready.
    /// </summary>
    public static bool FirstLegacyReady => JobGauge.SerpentCombo.HasFlag(SerpentCombo.FirstLegacy);

    /// <summary>
    /// Indicates if base Second Legacy oGCD is ready.
    /// </summary>
    public static bool SecondLegacyReady => JobGauge.SerpentCombo.HasFlag(SerpentCombo.SecondLegacy);

    /// <summary>
    /// Indicates if base Third Legacy oGCD is ready.
    /// </summary>
    public static bool ThirdLegacyReady => JobGauge.SerpentCombo.HasFlag(SerpentCombo.ThirdLegacy);

    /// <summary>
    /// Indicates if base Fourth Legacy oGCD is ready.
    /// </summary>
    public static bool FourthLegacyReady => JobGauge.SerpentCombo.HasFlag(SerpentCombo.FourthLegacy);

    /// <summary>
    /// Indicates if base Twins oGCDs are ready.
    /// </summary>
    public static bool TwinAbilityReady => (byte)JobGauge.SerpentCombo == 7;

    /// <summary>
    /// Indicates if base Thresh oGCDs are ready.
    /// </summary>
    public static bool ThreshAbilityReady => (byte)JobGauge.SerpentCombo == 8;

    /// <summary>
    /// Indicates if base Uncoiled oGCDs are ready.
    /// </summary>
    public static bool UncoiledAbilityReady => (byte)JobGauge.SerpentCombo == 9;
    #endregion

    /// <inheritdoc/>
    public override void DisplayBaseStatus()
    {
        ImGui.Text($"SerpentOffering: {SerpentOffering}/100");
        ImGui.Text($"RattlingCoilStacks: {RattlingCoilStacks}/{MaxRattling}");
        ImGui.Text($"AnguineTributeStacks: {AnguineTributeStacks}/{MaxAnguine}");
        ImGui.Spacing();
        ImGui.Text("DreadCombo: " + DreadCombo.ToString());
        ImGui.Text("NODREAD: " + NODREAD.ToString());
        ImGui.Text("DreadActive: " + DreadActive.ToString());
        ImGui.Text("SwiftskinsCoilOnly: " + SwiftskinsCoilOnly.ToString());
        ImGui.Text("HuntersCoilOnly: " + HuntersCoilOnly.ToString());
        ImGui.Text("PitActive: " + PitActive.ToString());
        ImGui.Text("SwiftskinsDenOnly: " + SwiftskinsDenOnly.ToString());
        ImGui.Text("HuntersDenOnly: " + HuntersDenOnly.ToString());
        ImGui.Spacing();
        ImGui.Text("SerpentCombo Raw Data: " + SerpentCombo.ToString());
        ImGui.Text("NoAbilityReady: " + NoAbilityReady.ToString());
        ImGui.Text("DeathRattleReady: " + DeathRattleReady.ToString());
        ImGui.Text("LastLashReady: " + LastLashReady.ToString());
        ImGui.Text("FirstLegacyReady: " + FirstLegacyReady.ToString());
        ImGui.Text("SecondLegacyReady: " + SecondLegacyReady.ToString());
        ImGui.Text("ThirdLegacyReady: " + ThirdLegacyReady.ToString());
        ImGui.Text("FourthLegacyReady: " + FourthLegacyReady.ToString());
        ImGui.Text("TwinAbilityReady: " + TwinAbilityReady.ToString());
        ImGui.Text("ThreshAbilityReady: " + ThreshAbilityReady.ToString());
        ImGui.Text("UncoiledAbilityReady: " + UncoiledAbilityReady.ToString());
        ImGui.Spacing();
        ImGui.Text("HasHunterAndSwift: " + HasHunterAndSwift.ToString());
        ImGui.Text("WillSwiftEnd: " + WillSwiftEnd.ToString());
        ImGui.Text("WillHunterEnd: " + WillHunterEnd.ToString());
        ImGui.Text("IsSwift: " + IsSwift.ToString());
        ImGui.Text("SwiftTime: " + SwiftTime.ToString());
        ImGui.Text("IsHunter: " + IsHunter.ToString());
        ImGui.Text("HuntersTime: " + HuntersTime.ToString());
        ImGui.Text("HunterOrSwiftEndsFirst: " + (HunterOrSwiftEndsFirst?.ToString() ?? "null"));
        ImGui.Spacing();
        ImGui.Text("MaxAnguine: " + MaxAnguine.ToString());
        ImGui.Text("HasSteel: " + HasSteel.ToString());
        ImGui.Text("HasReavers: " + HasReavers.ToString());
        ImGui.Text("NoHone: " + NoHone.ToString());
        ImGui.Text("HasHind: " + HasHind.ToString());
        ImGui.Text("HasFlank: " + HasFlank.ToString());
        ImGui.Text("HasBane: " + HasBane.ToString());
        ImGui.Text("HasSting: " + HasSting.ToString());
        ImGui.Text("HasNoVenom: " + HasNoVenom.ToString());
        ImGui.Text("HasReadyToReawaken: " + HasReadyToReawaken.ToString());
        ImGui.Text("HasHunterVenom: " + HasHunterVenom.ToString());
        ImGui.Text("HasSwiftVenom: " + HasSwiftVenom.ToString());
        ImGui.Text("HasFellHuntersVenom: " + HasFellHuntersVenom.ToString());
        ImGui.Text("HasFellSkinsVenom: " + HasFellSkinsVenom.ToString());
        ImGui.Text("HasPoisedFang: " + HasPoisedFang.ToString());
        ImGui.Text("HasPoisedBlood: " + HasPoisedBlood.ToString());
        ImGui.Text("HasGrimHunter: " + HasGrimHunter.ToString());
        ImGui.Text("HasGrimSkin: " + HasGrimSkin.ToString());
    }
    #endregion

    #region Statuses

    /// <summary>
    /// Indicates if both Hunters Instinct and Swiftscaled are active.
    /// </summary>
    public static bool HasHunterAndSwift => IsHunter && IsSwift;

    /// <summary>
    /// 
    /// </summary>
    public static bool WillSwiftEnd => StatusHelper.PlayerWillStatusEnd(5, true, StatusID.Swiftscaled);

    /// <summary>
    /// 
    /// </summary>
    public static bool WillHunterEnd => StatusHelper.PlayerWillStatusEnd(5, true, StatusID.HuntersInstinct);

    /// <summary>
    /// Indicates if the player has Swiftscaled.
    /// </summary>
    public static bool IsSwift => StatusHelper.PlayerHasStatus(true, StatusID.Swiftscaled);

    /// <summary>
    /// Indicates if the player has Hunters Instinct.
    /// </summary>
    public static bool IsHunter => StatusHelper.PlayerHasStatus(true, StatusID.HuntersInstinct);

    /// <summary>
    /// Time left on Swiftscaled.
    /// </summary>
    public static float? SwiftTime => StatusHelper.PlayerStatusTime(true, StatusID.Swiftscaled);

    /// <summary>
    /// Time left on Hunters Instinct.
    /// </summary>
    public static float? HuntersTime => StatusHelper.PlayerStatusTime(true, StatusID.HuntersInstinct);

    /// <summary>
    /// Returns which status will end first when both Hunters Instinct and Swiftscaled are active.
    /// Returns "Hunter" if Hunters Instinct ends first, "Swift" if Swiftscaled ends first, or null if not both are active.
    /// </summary>
    public static string? HunterOrSwiftEndsFirst
    {
        get
        {
            if (!HasHunterAndSwift)
                return null;
            if (HuntersTime == null || SwiftTime == null)
                return null;
            if (HuntersTime < SwiftTime)
                return "Hunter";
            if (SwiftTime < HuntersTime)
                return "Swift";
            return "Equal";
        }
    }

    /// <summary>
    /// Indicates if the player has Honed Steel.
    /// </summary>
    public static bool HasSteel => StatusHelper.PlayerHasStatus(true, StatusID.HonedSteel);

    /// <summary>
    /// Indicates if the player has Honed Reavers.
    /// </summary>
    public static bool HasReavers => StatusHelper.PlayerHasStatus(true, StatusID.HonedReavers);

    /// <summary>
    /// Indicates if the player does not have Honed Reavers or Honed Steel.
    /// </summary>
    public static bool NoHone => !StatusHelper.PlayerHasStatus(true, StatusID.HonedSteel) || !StatusHelper.PlayerHasStatus(true, StatusID.HonedReavers);

    /// <summary>
    /// Indicates if the player has upcoming Hind attack.
    /// </summary>
    public static bool HasHind => StatusHelper.PlayerHasStatus(true, StatusID.HindsbaneVenom) || StatusHelper.PlayerHasStatus(true, StatusID.HindstungVenom);

    /// <summary>
    /// Indicates if the player has upcoming Hind attack.
    /// </summary>
    public static bool HasHindsbane => StatusHelper.PlayerHasStatus(true, StatusID.HindsbaneVenom);

    /// <summary>
    /// Indicates if the player has upcoming Hind attack.
    /// </summary>
    public static bool HasHindstung => StatusHelper.PlayerHasStatus(true, StatusID.HindstungVenom);

    /// <summary>
    /// Indicates if the player has upcoming Flanks attack.
    /// </summary>
    public static bool HasFlank => StatusHelper.PlayerHasStatus(true, StatusID.FlanksbaneVenom) || StatusHelper.PlayerHasStatus(true, StatusID.FlankstungVenom);

    /// <summary>
    /// Indicates if the player has upcoming Hind attack.
    /// </summary>
    public static bool HasFlanksbane => StatusHelper.PlayerHasStatus(true, StatusID.FlanksbaneVenom);

    /// <summary>
    /// Indicates if the player has upcoming Hind attack.
    /// </summary>
    public static bool HasFlankstung => StatusHelper.PlayerHasStatus(true, StatusID.FlankstungVenom);

    /// <summary>
    /// Indicates if the player has upcoming Bane attack.
    /// </summary>
    public static bool HasBane => StatusHelper.PlayerHasStatus(true, StatusID.HindsbaneVenom) || StatusHelper.PlayerHasStatus(true, StatusID.FlanksbaneVenom);

    /// <summary>
    /// Indicates if the player has upcoming Bane attack.
    /// </summary>
    public static bool HasSting => StatusHelper.PlayerHasStatus(true, StatusID.HindstungVenom) || StatusHelper.PlayerHasStatus(true, StatusID.FlankstungVenom);

    /// <summary>
    /// Indicates if the player has no venom prepped.
    /// </summary>
    public static bool HasNoVenom => !StatusHelper.PlayerHasStatus(true, StatusID.HindstungVenom) && !StatusHelper.PlayerHasStatus(true, StatusID.FlankstungVenom) && !StatusHelper.PlayerHasStatus(true, StatusID.HindsbaneVenom) && !StatusHelper.PlayerHasStatus(true, StatusID.FlanksbaneVenom);

    /// <summary>
    /// Indicates if the player can use Reawakened.
    /// </summary>
    public static bool HasReadyToReawaken => StatusHelper.PlayerHasStatus(true, StatusID.ReadyToReawaken);

    /// <summary>
    /// Indicates if the player can use Reawakened.
    /// </summary>
    public static bool HasReawakenedActive => StatusHelper.PlayerHasStatus(true, StatusID.Reawakened);

    /// <summary>
    /// Indicates if the player has Hunters Venom.
    /// </summary>
    public static bool HasHunterVenom => StatusHelper.PlayerHasStatus(true, StatusID.HuntersVenom);

    /// <summary>
    /// Indicates if the player has Hunters Venom.
    /// </summary>
    public static bool HasSwiftVenom => StatusHelper.PlayerHasStatus(true, StatusID.SwiftskinsVenom);

    /// <summary>
    /// Indicates if the player has Fellhunters Venom.
    /// </summary>
    public static bool HasFellHuntersVenom => StatusHelper.PlayerHasStatus(true, StatusID.FellhuntersVenom);

    /// <summary>
    /// Indicates if the player has Fellskins Venom.
    /// </summary>
    public static bool HasFellSkinsVenom => StatusHelper.PlayerHasStatus(true, StatusID.FellskinsVenom);

    /// <summary>
    /// Indicates if the player has Poised For Twinfang.
    /// </summary>
    public static bool HasPoisedFang => StatusHelper.PlayerHasStatus(true, StatusID.PoisedForTwinfang);

    /// <summary>
    /// Indicates if the player has Poised For Twinblood.
    /// </summary>
    public static bool HasPoisedBlood => StatusHelper.PlayerHasStatus(true, StatusID.PoisedForTwinblood);

    /// <summary>
    /// Indicates if the player has Grimhunters Venom.
    /// </summary>
    public static bool HasGrimHunter => StatusHelper.PlayerHasStatus(true, StatusID.GrimhuntersVenom);

    /// <summary>
    /// Indicates if the player has Grimskins Venom.
    /// </summary>
    public static bool HasGrimSkin => StatusHelper.PlayerHasStatus(true, StatusID.GrimskinsVenom);
    #endregion

    #region Actions Unassignable

    /// <summary>
    /// 
    /// </summary>
    public static bool SteelFangsPvEReady => Service.GetAdjustedActionId(ActionID.SteelFangsPvE) == ActionID.SteelFangsPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool HuntersStingPvEReady => Service.GetAdjustedActionId(ActionID.SteelFangsPvE) == ActionID.HuntersStingPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool FlankstingStrikePvEReady => Service.GetAdjustedActionId(ActionID.SteelFangsPvE) == ActionID.FlankstingStrikePvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool HindstingStrikePvEReady => Service.GetAdjustedActionId(ActionID.SteelFangsPvE) == ActionID.HindstingStrikePvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool ReavingFangsPvEReady => Service.GetAdjustedActionId(ActionID.ReavingFangsPvE) == ActionID.ReavingFangsPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool SwiftskinsStingPvEReady => Service.GetAdjustedActionId(ActionID.ReavingFangsPvE) == ActionID.SwiftskinsStingPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool FlanksbaneFangPvEReady => Service.GetAdjustedActionId(ActionID.ReavingFangsPvE) == ActionID.FlanksbaneFangPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool HindsbaneFangPvEReady => Service.GetAdjustedActionId(ActionID.ReavingFangsPvE) == ActionID.HindsbaneFangPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool SteelMawPvEReady => Service.GetAdjustedActionId(ActionID.SteelMawPvE) == ActionID.SteelMawPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool HuntersBitePvEReady => Service.GetAdjustedActionId(ActionID.SteelMawPvE) == ActionID.HuntersBitePvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool JaggedMawPvEReady => Service.GetAdjustedActionId(ActionID.SteelMawPvE) == ActionID.JaggedMawPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool ReavingMawPvEReady => Service.GetAdjustedActionId(ActionID.ReavingMawPvE) == ActionID.ReavingMawPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool SwiftskinsBitePvEReady => Service.GetAdjustedActionId(ActionID.ReavingMawPvE) == ActionID.SwiftskinsBitePvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool BloodiedMawPvEReady => Service.GetAdjustedActionId(ActionID.ReavingMawPvE) == ActionID.BloodiedMawPvE;

    #endregion

    #region PvE Actions

    static partial void ModifyWrithingSnapPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MeleeRange;
    }

    static partial void ModifySlitherPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.HostileFriendlyMovingForward;
        setting.IsFriendly = false;
    }

    static partial void ModifySteelFangsPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => NODREAD && SteelFangsPvEReady;
    }

    static partial void ModifyHuntersStingPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => NODREAD && HuntersStingPvEReady;
        setting.StatusProvide = [StatusID.HuntersInstinct];
        setting.CreateConfig = () => new ActionConfig()
        {
            ShouldCheckStatus = false,
        };
    }

    static partial void ModifyFlankstingStrikePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => NODREAD && FlankstingStrikePvEReady;
        setting.StatusProvide = [StatusID.HindstungVenom];
    }

    static partial void ModifyFlanksbaneFangPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => NODREAD && FlanksbaneFangPvEReady;
        setting.StatusProvide = [StatusID.HindsbaneVenom];
    }

    static partial void ModifyReavingFangsPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => NODREAD && ReavingFangsPvEReady;
        setting.StatusProvide = [StatusID.HonedSteel];
    }

    static partial void ModifySwiftskinsStingPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => NODREAD && SwiftskinsStingPvEReady;
        setting.StatusProvide = [StatusID.Swiftscaled];
    }

    static partial void ModifyHindstingStrikePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => NODREAD && HindstingStrikePvEReady;
        setting.StatusProvide = [StatusID.FlanksbaneVenom];
    }

    static partial void ModifyHindsbaneFangPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => NODREAD && HindsbaneFangPvEReady;
        setting.StatusProvide = [StatusID.FlankstungVenom];
    }

    static partial void ModifySteelMawPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => NODREAD && SteelMawPvEReady;
        setting.StatusProvide = [StatusID.HonedReavers];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyHuntersBitePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => NODREAD && HuntersBitePvEReady;
        setting.StatusProvide = [StatusID.HuntersInstinct];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyJaggedMawPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => NODREAD && JaggedMawPvEReady;
        setting.StatusProvide = [StatusID.GrimskinsVenom];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyReavingMawPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => NODREAD && ReavingMawPvEReady;
        setting.StatusProvide = [StatusID.HonedSteel];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifySwiftskinsBitePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => NODREAD && SwiftskinsBitePvEReady;
        setting.StatusProvide = [StatusID.Swiftscaled];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBloodiedMawPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => NODREAD && BloodiedMawPvEReady;
        setting.StatusProvide = [StatusID.GrimhuntersVenom];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySerpentsTailPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyDeathRattlePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => DeathRattleReady;
    }

    static partial void ModifyLastLashPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => LastLashReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyVicewinderPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SerpentOffering <= 90 && RattlingCoilStacks < MaxRattling && AnguineTributeStacks == 0 && NODREAD;
    }

    static partial void ModifyHuntersCoilPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => DreadActive || HuntersCoilOnly;
    }

    static partial void ModifySwiftskinsCoilPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => DreadActive || SwiftskinsCoilOnly;
    }

    static partial void ModifyVicepitPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SerpentOffering <= 90 && RattlingCoilStacks < MaxRattling && AnguineTributeStacks == 0 && NODREAD;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyHuntersDenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => PitActive || HuntersDenOnly;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySwiftskinsDenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => PitActive || SwiftskinsDenOnly;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyTwinfangPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyTwinbloodPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyTwinfangBitePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => TwinAbilityReady;
    }

    static partial void ModifyTwinbloodBitePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => TwinAbilityReady;
    }

    static partial void ModifyTwinfangThreshPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ThreshAbilityReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyTwinbloodThreshPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ThreshAbilityReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyUncoiledFuryPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => RattlingCoilStacks >= 1;
        setting.StatusProvide = [StatusID.PoisedForTwinfang];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyUncoiledTwinfangPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => UncoiledAbilityReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyUncoiledTwinbloodPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => UncoiledAbilityReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySerpentsIrePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => RattlingCoilStacks < MaxRattling && InCombat;
        setting.StatusProvide = [StatusID.ReadyToReawaken];
    }

    static partial void ModifyReawakenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => NODREAD && (HasReadyToReawaken || SerpentOffering >= 50);
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFirstGenerationPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ((MaxAnguine == 5 && AnguineTributeStacks == 5) || (MaxAnguine == 4 && AnguineTributeStacks == 4)) && FirstGenReady;
        setting.StatusNeed = [StatusID.Reawakened];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySecondGenerationPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ((MaxAnguine == 5 && AnguineTributeStacks == 4) || (MaxAnguine == 4 && AnguineTributeStacks == 3)) && SecondGenReady;
        setting.StatusNeed = [StatusID.Reawakened];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyThirdGenerationPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ((MaxAnguine == 5 && AnguineTributeStacks == 3) || (MaxAnguine == 4 && AnguineTributeStacks == 2)) && ThirdGenReady;
        setting.StatusNeed = [StatusID.Reawakened];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFourthGenerationPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ((MaxAnguine == 5 && AnguineTributeStacks == 2) || (MaxAnguine == 4 && AnguineTributeStacks == 1)) && FourthGenReady;
        setting.StatusNeed = [StatusID.Reawakened];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyOuroborosPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => EnhancedSerpentsLineageTrait.EnoughLevel && AnguineTributeStacks == 1;
        setting.StatusNeed = [StatusID.Reawakened];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFirstLegacyPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => FirstLegacyReady;
        setting.StatusNeed = [StatusID.Reawakened];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySecondLegacyPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SecondLegacyReady;
        setting.StatusNeed = [StatusID.Reawakened];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyThirdLegacyPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ThirdLegacyReady;
        setting.StatusNeed = [StatusID.Reawakened];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFourthLegacyPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => FourthLegacyReady;
        setting.StatusNeed = [StatusID.Reawakened];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion

    #region PvP Actions
    static partial void ModifyRavenousBitePvP(ref ActionSetting setting)
    {
        
    }

    static partial void ModifySwiftskinsStingPvP(ref ActionSetting setting)
    {
        
    }

    static partial void ModifyPiercingFangsPvP(ref ActionSetting setting)
    {
        
    }

    static partial void ModifyBarbarousBitePvP(ref ActionSetting setting)
    {
        
    }

    static partial void ModifyHuntersStingPvP(ref ActionSetting setting)
    {
        
    }

    static partial void ModifySteelFangsPvP(ref ActionSetting setting)
    {
        
    }

    static partial void ModifyBloodcoilPvP(ref ActionSetting setting)
    {
        
    }

    static partial void ModifyUncoiledFuryPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySerpentsTailPvP(ref ActionSetting setting)
    {
        // technically not a real move
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySlitherPvP(ref ActionSetting setting)
    {
        
    }

    static partial void ModifySnakeScalesPvP(ref ActionSetting setting)
    {
        
    }

    static partial void ModifyRattlingCoilPvP(ref ActionSetting setting)
    {
        
    }

    static partial void ModifyFirstGenerationPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.SteelFangsPvP) == ActionID.FirstGenerationPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySecondGenerationPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.SteelFangsPvP) == ActionID.SecondGenerationPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyThirdGenerationPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.SteelFangsPvP) == ActionID.ThirdGenerationPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFourthGenerationPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.SteelFangsPvP) == ActionID.FourthGenerationPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySanguineFeastPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.BloodcoilPvP) == ActionID.SanguineFeastPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            ShouldCheckCombo = false,
        };
    }

    static partial void ModifyOuroborosPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.BloodcoilPvP) == ActionID.OuroborosPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyDeathRattlePvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.SerpentsTailPvP) == ActionID.DeathRattlePvP;
		setting.MPOverride = () => 0;
	}

    static partial void ModifyTwinfangBitePvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.SerpentsTailPvP) == ActionID.TwinfangBitePvP;
		setting.MPOverride = () => 0;
	}

    static partial void ModifyTwinbloodBitePvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.SerpentsTailPvP) == ActionID.TwinbloodBitePvP;
		setting.MPOverride = () => 0;
	}

    static partial void ModifyUncoiledTwinfangPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.SerpentsTailPvP) == ActionID.UncoiledTwinfangPvP;
		setting.MPOverride = () => 0;
		setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyUncoiledTwinbloodPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.SerpentsTailPvP) == ActionID.UncoiledTwinbloodPvP;
		setting.MPOverride = () => 0;
		setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFirstLegacyPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.SerpentsTailPvP) == ActionID.FirstLegacyPvP;
		setting.MPOverride = () => 0;
		setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySecondLegacyPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.SerpentsTailPvP) == ActionID.SecondLegacyPvP;
		setting.MPOverride = () => 0;
		setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyThirdLegacyPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.SerpentsTailPvP) == ActionID.ThirdLegacyPvP;
		setting.MPOverride = () => 0;
		setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFourthLegacyPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.SerpentsTailPvP) == ActionID.FourthLegacyPvP;
		setting.MPOverride = () => 0;
		setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBacklashPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.SnakeScalesPvP) == ActionID.BacklashPvP;
		setting.MPOverride = () => 0;
		setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFuriousBacklashPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion
}
