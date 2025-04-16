using Dalamud.Interface.Colors;

namespace RotationSolver.Basic.Rotations.Basic;

public partial class PictomancerRotation
{
    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Intelligence;

    #region Job Gauge
    /// <summary>
    /// Tracks use of subjective pallete
    /// </summary>
    public static byte PaletteGauge => JobGauge.PalleteGauge;

    /// <summary>
    /// Number of paint the player has.
    /// </summary>
    public static byte Paint => JobGauge.Paint;

    /// <summary>
    /// Creature Motif Stack
    /// </summary>
    public static bool CreatureMotifDrawn => JobGauge.CreatureMotifDrawn;

    /// <summary>
    /// Weapon Motif Stack
    /// </summary>
    public static bool WeaponMotifDrawn => JobGauge.WeaponMotifDrawn;

    /// <summary>
    /// Landscape Motif Stack
    /// </summary>
    public static bool LandscapeMotifDrawn => JobGauge.LandscapeMotifDrawn;

    /// <summary>
    /// Moogle Portrait Stack
    /// </summary>
    public static bool MooglePortraitReady => JobGauge.MooglePortraitReady;

    /// <summary>
    /// Madeen Portrait Stack
    /// </summary>
    public static bool MadeenPortraitReady => JobGauge.MadeenPortraitReady;

    /// <summary>
    /// Which creature flags are present. Pom = 1, Wings = 2, Claw = 4, MooglePortait = 0x10, MadeenPortrait = 0x20, these are the small paintings above Maw/Pom
    /// </summary>
    public static CreatureFlags CreatureFlags => JobGauge.CreatureFlags;

    /// <summary>
    /// Which canvas flags are present.  Pom = 1, Wing = 2, Claw = 4, Maw = 8, Weapon = 0x10, Landscape = 0x20, these are the motif flags
    /// </summary>
    public static CanvasFlags CanvasFlags => JobGauge.CanvasFlags;
    #endregion

    #region Flag Tracking
    /// <summary>
    /// Is Pom Motif ready
    /// </summary>
    public static bool isPomMotifReady => ((byte)JobGauge.CreatureFlags) == 32 || ((byte)JobGauge.CreatureFlags) == 0;

    /// <summary>
    /// Is Wing Motif ready
    /// </summary>
    public static bool isWingMotifReady => ((byte)JobGauge.CreatureFlags) == 33 || ((byte)JobGauge.CreatureFlags) == 1;

    /// <summary>
    /// Is Claw Motif ready
    /// </summary>
    public static bool isClawMotifReady => ((byte)JobGauge.CreatureFlags) == 19 || ((byte)JobGauge.CreatureFlags) == 3;

    /// <summary>
    /// Is Maw Motif ready
    /// </summary>
    public static bool isMawMotifReady => ((byte)JobGauge.CreatureFlags) == 23 || ((byte)JobGauge.CreatureFlags) == 7;

    /// <summary>
    /// Is Wing ready
    /// </summary>
    public static bool isPomMuseReady => ((byte)JobGauge.CanvasFlags & 1) == 1 || ((byte)JobGauge.CanvasFlags & 17) == 17 || ((byte)JobGauge.CanvasFlags & 33) == 33 || ((byte)JobGauge.CanvasFlags & 49) == 49;

    /// <summary>
    /// Is Claw ready
    /// </summary>
    public static bool isWingMuseReady => ((byte)JobGauge.CanvasFlags & 2) == 2 || ((byte)JobGauge.CanvasFlags & 18) == 18 || ((byte)JobGauge.CanvasFlags & 34) == 34 || ((byte)JobGauge.CanvasFlags & 50) == 50;

    /// <summary>
    /// Is Pom ready
    /// </summary>
    public static bool isClawMuseReady => ((byte)JobGauge.CanvasFlags & 4) == 4 || ((byte)JobGauge.CanvasFlags & 20) == 20 || ((byte)JobGauge.CanvasFlags & 36) == 36 || ((byte)JobGauge.CanvasFlags & 52) == 52;

    /// <summary>
    /// Is Maw ready
    /// </summary>
    public static bool isMawMuseReady => ((byte)JobGauge.CanvasFlags & 8) == 8 || ((byte)JobGauge.CanvasFlags & 24) == 24 || ((byte)JobGauge.CanvasFlags & 40) == 40 || ((byte)JobGauge.CanvasFlags & 56) == 56;

    /// <summary>
    /// Is Hammer ready
    /// </summary>
    public static bool isHammerMuseReady => ((byte)JobGauge.CanvasFlags & 16) == 16 || ((byte)JobGauge.CanvasFlags & 17) == 17 || ((byte)JobGauge.CanvasFlags & 18) == 18 || ((byte)JobGauge.CanvasFlags & 20) == 20
        || ((byte)JobGauge.CanvasFlags & 24) == 24 || ((byte)JobGauge.CanvasFlags & 48) == 48 || ((byte)JobGauge.CanvasFlags & 49) == 49
        || ((byte)JobGauge.CanvasFlags & 50) == 50 || ((byte)JobGauge.CanvasFlags & 52) == 52
        || ((byte)JobGauge.CanvasFlags & 56) == 56;

    /// <summary>
    /// Is Starry ready
    /// </summary>
    public static bool isStarryMuseReady => ((byte)JobGauge.CanvasFlags & 32) == 32 || ((byte)JobGauge.CanvasFlags & 33) == 33 || ((byte)JobGauge.CanvasFlags & 34) == 34 || ((byte)JobGauge.CanvasFlags & 36) == 36
        || ((byte)JobGauge.CanvasFlags & 40) == 40 || ((byte)JobGauge.CanvasFlags & 48) == 48 || ((byte)JobGauge.CanvasFlags & 49) == 49
        || ((byte)JobGauge.CanvasFlags & 50) == 50 || ((byte)JobGauge.CanvasFlags & 52) == 52
        || ((byte)JobGauge.CanvasFlags & 56) == 56;
    #endregion

    #region Actions Unassignable

    /// <summary>
    /// 
    /// </summary>
    public static bool AeroInGreenPvEReady => Service.GetAdjustedActionId(ActionID.FireInRedPvE) == ActionID.AeroInGreenPvE && !HasSubtractivePalette;

    /// <summary>
    /// 
    /// </summary>
    public static bool WaterInBluePvEReady => Service.GetAdjustedActionId(ActionID.FireInRedPvE) == ActionID.WaterInBluePvE && !HasSubtractivePalette;

    /// <summary>
    /// 
    /// </summary>
    public static bool PomMotifPvEReady => Service.GetAdjustedActionId(ActionID.CreatureMotifPvE) == ActionID.PomMotifPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool WingMotifPvEReady => Service.GetAdjustedActionId(ActionID.CreatureMotifPvE) == ActionID.WingMotifPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool PomMusePvEReady => Service.GetAdjustedActionId(ActionID.LivingMusePvE) == ActionID.PomMusePvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool WingedMusePvEReady => Service.GetAdjustedActionId(ActionID.LivingMusePvE) == ActionID.WingedMusePvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool AeroIiInGreenPvEReady => Service.GetAdjustedActionId(ActionID.FireIiInRedPvE) == ActionID.AeroIiInGreenPvE && !HasSubtractivePalette;

    /// <summary>
    /// 
    /// </summary>
    public static bool WaterIiInBluePvEReady => Service.GetAdjustedActionId(ActionID.FireIiInRedPvE) == ActionID.WaterIiInBluePvE && !HasSubtractivePalette;

    /// <summary>
    /// 
    /// </summary>
    public static bool HammerMotifPvEReady => Service.GetAdjustedActionId(ActionID.WeaponMotifPvE) == ActionID.HammerMotifPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool StrikingMusePvEReady => Service.GetAdjustedActionId(ActionID.SteelMusePvE) == ActionID.StrikingMusePvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool StoneInYellowPvEReady => Service.GetAdjustedActionId(ActionID.BlizzardInCyanPvE) == ActionID.StoneInYellowPvE && HasSubtractivePalette;

    /// <summary>
    /// 
    /// </summary>
    public static bool ThunderInMagentaPvEReady => Service.GetAdjustedActionId(ActionID.StoneInYellowPvE) == ActionID.ThunderInMagentaPvE && HasSubtractivePalette;

    /// <summary>
    /// 
    /// </summary>
    public static bool StoneIiInYellowPvEReady => Service.GetAdjustedActionId(ActionID.BlizzardIiInCyanPvE) == ActionID.StoneIiInYellowPvE && HasSubtractivePalette;

    /// <summary>
    /// 
    /// </summary>
    public static bool ThunderIiInMagentaPvEReady => Service.GetAdjustedActionId(ActionID.BlizzardIiInCyanPvE) == ActionID.ThunderIiInMagentaPvE && HasSubtractivePalette;

    /// <summary>
    /// 
    /// </summary>
    public static bool StarrySkyMotifPvEReady => Service.GetAdjustedActionId(ActionID.LandscapeMotifPvE) == ActionID.StarrySkyMotifPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool StarryMusePvEReady => Service.GetAdjustedActionId(ActionID.ScenicMusePvE) == ActionID.StarryMusePvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool HammerBrushPvEReady => Service.GetAdjustedActionId(ActionID.HammerStampPvE) == ActionID.HammerBrushPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool PolishingHammerPvEReady => Service.GetAdjustedActionId(ActionID.HammerStampPvE) == ActionID.PolishingHammerPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool ClawMotifPvEReady => Service.GetAdjustedActionId(ActionID.CreatureMotifPvE) == ActionID.ClawMotifPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool MawMotifPvEReady => Service.GetAdjustedActionId(ActionID.CreatureMotifPvE) == ActionID.MawMotifPvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool ClawedMusePvEReady => Service.GetAdjustedActionId(ActionID.LivingMusePvE) == ActionID.ClawedMusePvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool FangedMusePvEReady => Service.GetAdjustedActionId(ActionID.LivingMusePvE) == ActionID.FangedMusePvE;

    /// <summary>
    /// 
    /// </summary>
    public static bool RetributionOfTheMadeenPvEReady => Service.GetAdjustedActionId(ActionID.MogOfTheAgesPvE) == ActionID.RetributionOfTheMadeenPvE;
    #endregion

    #region Debug
    /// <inheritdoc/>
    public override void DisplayStatus()
    {
        ImGui.Text($"PaletteGauge: {PaletteGauge}");
        ImGui.Text($"Paint: {Paint}");
        ImGui.Text($"CreatureMotifDrawn: {CreatureMotifDrawn}");
        ImGui.Text($"WeaponMotifDrawn: {WeaponMotifDrawn}");
        ImGui.Text($"LandscapeMotifDrawn: {LandscapeMotifDrawn}");
        ImGui.Text($"MooglePortraitReady: {MooglePortraitReady}");
        ImGui.Text($"MadeenPortraitReady: {MadeenPortraitReady}");
        ImGui.Text($"CreatureFlags: {CreatureFlags}");
        ImGui.Text($"isPomMotifReady: {isPomMotifReady}");
        ImGui.Text($"isWingMotifReady: {isWingMotifReady}");
        ImGui.Text($"isClawMotifReady: {isClawMotifReady}");
        ImGui.Text($"isMawMotifReady: {isMawMotifReady}");
        ImGui.Text($"CanvasFlags: {CanvasFlags}");
        ImGui.Text($"isPomMuseReady: {isPomMuseReady}");
        ImGui.Text($"isWingMuseReady: {isWingMuseReady}");
        ImGui.Text($"isClawMuseReady: {isClawMuseReady}");
        ImGui.Text($"isMawMuseReady: {isMawMuseReady}");
        ImGui.Text($"isHammerMuseReady: {isHammerMuseReady}");
        ImGui.Text($"isStarryMuseReady: {isStarryMuseReady}");
        ImGui.Text($"MaxStrikingMuse: {MaxStrikingMuse}");
        ImGui.Text($"Level100: {Level100}");
        ImGui.Text($"HasSubtractivePalette: {HasSubtractivePalette}");
        ImGui.Text($"HasAetherhues: {HasAetherhues}");
        ImGui.Text($"HasAetherhues2: {HasAetherhues2}");
        ImGui.Text($"HasSubtractiveSpectrum: {HasSubtractiveSpectrum}");
        ImGui.Text($"HasHyperphantasia: {HasHyperphantasia}");
        ImGui.Text($"HasHammerTime: {HasHammerTime}");
        ImGui.Text($"HasMonochromeTones: {HasMonochromeTones}");
        ImGui.Text($"HasStarryMuse: {HasStarryMuse}");
        ImGui.Text($"HammerStacks: {HammerStacks}");
        ImGui.Text($"SubtractiveStacks: {SubtractiveStacks}");
        ImGui.TextColored(ImGuiColors.DalamudViolet, "PvE Actions");
        ImGui.TextColored(AeroInGreenPvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "AeroInGreenPvEReady: " + AeroInGreenPvEReady.ToString());
        ImGui.TextColored(WaterInBluePvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "WaterInBluePvEReady: " + WaterInBluePvEReady.ToString());
        ImGui.TextColored(PomMotifPvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "PomMotifPvEReady: " + PomMotifPvEReady.ToString());
        ImGui.TextColored(WingMotifPvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "WingMotifPvEReady: " + WingMotifPvEReady.ToString());
        ImGui.TextColored(PomMusePvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "PomMusePvEReady: " + PomMusePvEReady.ToString());
        ImGui.TextColored(WingedMusePvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "WingedMusePvEReady: " + WingedMusePvEReady.ToString());
        ImGui.TextColored(AeroIiInGreenPvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "AeroIiInGreenPvEReady: " + AeroIiInGreenPvEReady.ToString());
        ImGui.TextColored(WaterIiInBluePvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "WaterIiInBluePvEReady: " + WaterIiInBluePvEReady.ToString());
        ImGui.TextColored(HammerMotifPvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "HammerMotifPvEReady: " + HammerMotifPvEReady.ToString());
        ImGui.TextColored(StrikingMusePvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "StrikingMusePvEReady: " + StrikingMusePvEReady.ToString());
        ImGui.TextColored(StoneInYellowPvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "StoneInYellowPvEReady: " + StoneInYellowPvEReady.ToString());
        ImGui.TextColored(ThunderInMagentaPvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "ThunderInMagentaPvEReady: " + ThunderInMagentaPvEReady.ToString());
        ImGui.TextColored(StoneIiInYellowPvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "StoneIiInYellowPvEReady: " + StoneIiInYellowPvEReady.ToString());
        ImGui.TextColored(ThunderIiInMagentaPvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "ThunderIiInMagentaPvEReady: " + ThunderIiInMagentaPvEReady.ToString());
        ImGui.TextColored(StarrySkyMotifPvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "StarrySkyMotifPvEReady: " + StarrySkyMotifPvEReady.ToString());
        ImGui.TextColored(StarryMusePvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "StarryMusePvEReady: " + StarryMusePvEReady.ToString());
        ImGui.TextColored(HammerBrushPvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "HammerBrushPvEReady: " + HammerBrushPvEReady.ToString());
        ImGui.TextColored(PolishingHammerPvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "PolishingHammerPvEReady: " + PolishingHammerPvEReady.ToString());
        ImGui.TextColored(ClawMotifPvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "ClawMotifPvEReady: " + ClawMotifPvEReady.ToString());
        ImGui.TextColored(MawMotifPvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "MawMotifPvEReady: " + MawMotifPvEReady.ToString());
        ImGui.TextColored(ClawedMusePvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "ClawedMusePvEReady: " + ClawedMusePvEReady.ToString());
        ImGui.TextColored(FangedMusePvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "FangedMusePvEReady: " + FangedMusePvEReady.ToString());
        ImGui.TextColored(RetributionOfTheMadeenPvEReady ? ImGuiColors.HealerGreen : ImGuiColors.DalamudWhite, "RetributionOfTheMadeenPvEReady: " + RetributionOfTheMadeenPvEReady.ToString());
    }
    #endregion

    #region Job States

    /// <summary>
    /// Number of max charges Striking Muse can have
    /// </summary>
    public static byte MaxStrikingMuse => EnhancedPictomancyIiTrait.EnoughLevel ? (byte)2 : (byte)1;

    /// <summary>
    /// Determines if player is max level or not
    /// </summary>
    public static bool Level100 => EnhancedPictomancyVTrait.EnoughLevel;

    #endregion

    #region Statuses
    /// <summary>
    /// Indicates if the player has Aetherhues.
    /// </summary>
    public static bool HasAetherhues => !Player.WillStatusEnd(0, true, StatusID.Aetherhues);

    /// <summary>
    /// Indicates if the player has Aetherhues II.
    /// </summary>
    public static bool HasAetherhues2 => !Player.WillStatusEnd(0, true, StatusID.AetherhuesIi);

    /// <summary>
    /// Indicates if the player has Subtractive Palette.
    /// </summary>
    public static bool HasSubtractivePalette => !Player.WillStatusEnd(0, true, StatusID.SubtractivePalette);

    /// <summary>
    /// Indicates if the player has Subtractive Spectrum.
    /// </summary>
    public static bool HasSubtractiveSpectrum => !Player.WillStatusEnd(0, true, StatusID.SubtractiveSpectrum);

    /// <summary>
    /// Indicates if the player has Hyperphantasia.
    /// </summary>
    public static bool HasHyperphantasia => !Player.WillStatusEnd(0, true, StatusID.Hyperphantasia);

    /// <summary>
    /// Indicates if the player has Hammer Time.
    /// </summary>
    public static bool HasHammerTime => !Player.WillStatusEnd(0, true, StatusID.HammerTime);

    /// <summary>
    /// Indicates if the player has Monochrome Tones.
    /// </summary>
    public static bool HasMonochromeTones => !Player.WillStatusEnd(0, true, StatusID.MonochromeTones);

    /// <summary>
    /// Indicates if the player has Starry Muse.
    /// </summary>
    public static bool HasStarryMuse => !Player.WillStatusEnd(0, true, StatusID.StarryMuse);

    /// <summary>
    /// Indicates if the player has Rainbow Bright.
    /// </summary>
    public static bool HasRainbowBright => !Player.WillStatusEnd(0, true, StatusID.RainbowBright);

    /// <summary>
    /// Holds the remaining amount of HammerTime stacks
    /// </summary>
    public static byte HammerStacks
    {
        get
        {
            byte stacks = Player.StatusStack(true, StatusID.HammerTime);
            return stacks == byte.MaxValue ? (byte)3 : stacks;
        }
    }

    /// <summary>
    /// Holds the remaining amount of SubtractivePalette stacks
    /// </summary>
    public static byte SubtractiveStacks
    {
        get
        {
            byte stacks = Player.StatusStack(true, StatusID.SubtractivePalette);
            return stacks == byte.MaxValue ? (byte)3 : stacks;
        }
    }

    #endregion

    #region PvE Actions
    static partial void ModifyFireInRedPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !HasSubtractivePalette;
        setting.StatusProvide = [StatusID.Aetherhues];
    }

    static partial void ModifyTemperaCoatPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.TemperaCoat];
    }

    static partial void ModifySmudgePvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
        setting.IsFriendly = true;
    }

    static partial void ModifyFireIiInRedPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !HasSubtractivePalette;
        setting.StatusProvide = [StatusID.Aetherhues];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyCreatureMotifPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyLivingMusePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => CreatureMotifDrawn;
    }

    static partial void ModifyMogOfTheAgesPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => MooglePortraitReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyWeaponMotifPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifySteelMusePvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyHammerStampPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HammerStacks == 3 || (!EnhancedPictomancyIiTrait.EnoughLevel && HammerStacks > 0);
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBlizzardInCyanPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasSubtractivePalette;
        setting.StatusProvide = [StatusID.Aetherhues];
    }

    static partial void ModifyBlizzardIiInCyanPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HasSubtractivePalette;
        setting.StatusProvide = [StatusID.Aetherhues];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifySubtractivePalettePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !HasSubtractivePalette && (PaletteGauge >= 50 || HasSubtractiveSpectrum) && !HasMonochromeTones;
        setting.StatusProvide = [StatusID.SubtractivePalette];
    }

    static partial void ModifyLandscapeMotifPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyScenicMusePvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyHolyInWhitePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Paint > 0 && !HasMonochromeTones;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyTemperaGrassaPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.TemperaCoat];
        setting.StatusProvide = [StatusID.TemperaGrassa];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyCometInBlackPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Paint > 0 && HasMonochromeTones;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyRainbowDripPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyStarPrismPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Starstruck];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion

    #region PvE Actions Unassignable

    static partial void ModifyAeroInGreenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => AeroInGreenPvEReady;
        setting.StatusProvide = [StatusID.AetherhuesIi];
    }

    static partial void ModifyWaterInBluePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => WaterInBluePvEReady;
    }

    static partial void ModifyPomMotifPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => PomMotifPvEReady;
    }

    static partial void ModifyWingMotifPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => WingMotifPvEReady;
        //setting.ActionCheck = () => !CreatureMotifDrawn && isWingMotifReady;
    }

    static partial void ModifyPomMusePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => PomMusePvEReady;
        //setting.ActionCheck = () => isPomMuseReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyWingedMusePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => WingedMusePvEReady;
        //setting.ActionCheck = () => isWingMuseReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyAeroIiInGreenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => AeroIiInGreenPvEReady;
        //setting.ActionCheck = () => !HasSubtractivePalette && HasAetherhues;
        setting.StatusProvide = [StatusID.AetherhuesIi];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyWaterIiInBluePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => WaterIiInBluePvEReady;
        //setting.ActionCheck = () => (Paint <= 4) && (PaletteGauge <= 75) && !HasSubtractivePalette && HasAetherhues2;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyHammerMotifPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HammerMotifPvEReady;
        //setting.ActionCheck = () => !HasHammerTime && !WeaponMotifDrawn;
    }

    static partial void ModifyStrikingMusePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => StrikingMusePvEReady && InCombat;
        //setting.ActionCheck = () => WeaponMotifDrawn && InCombat;
    }

    static partial void ModifyStoneInYellowPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => StoneInYellowPvEReady;
        //setting.ActionCheck = () => HasSubtractivePalette && HasAetherhues;
        setting.StatusProvide = [StatusID.AetherhuesIi];
    }

    static partial void ModifyThunderInMagentaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ThunderInMagentaPvEReady;
        //setting.ActionCheck = () => Paint <= 4 && HasSubtractivePalette && HasAetherhues2;
    }

    static partial void ModifyStoneIiInYellowPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => StoneIiInYellowPvEReady;
        //setting.ActionCheck = () => HasSubtractivePalette && HasAetherhues;
        setting.StatusProvide = [StatusID.AetherhuesIi];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyThunderIiInMagentaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ThunderIiInMagentaPvEReady;
        //setting.ActionCheck = () => Paint <= 4 && HasSubtractivePalette && HasAetherhues2;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyStarrySkyMotifPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => StarrySkyMotifPvEReady;
        //setting.ActionCheck = () => !HasStarryMuse && !LandscapeMotifDrawn;
    }

    static partial void ModifyStarryMusePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => StarryMusePvEReady && InCombat;
        //setting.ActionCheck = () => isStarryMuseReady && InCombat;
        setting.TargetType = TargetType.Self;
        setting.StatusProvide = [StatusID.Starstruck, StatusID.SubtractiveSpectrum, StatusID.Inspiration, StatusID.Hyperphantasia, StatusID.RainbowBright];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyHammerBrushPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => HammerBrushPvEReady;
        //setting.ActionCheck = () => HammerStacks == 2;
        setting.ComboIds = [ActionID.HammerStampPvE];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyPolishingHammerPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => PolishingHammerPvEReady;
        //setting.ActionCheck = () => HammerStacks == 1;
        setting.ComboIds = [ActionID.HammerBrushPvE];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyClawMotifPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ClawMotifPvEReady;
        //setting.ActionCheck = () => !CreatureMotifDrawn && isClawMotifReady;
    }

    static partial void ModifyMawMotifPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => MawMotifPvEReady;
        //setting.ActionCheck = () => !CreatureMotifDrawn && isMawMotifReady;
    }

    static partial void ModifyClawedMusePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ClawedMusePvEReady;
        //setting.ActionCheck = () => isClawMuseReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFangedMusePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => FangedMusePvEReady;
        //setting.ActionCheck = () => isMawMuseReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyRetributionOfTheMadeenPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => RetributionOfTheMadeenPvEReady;
        //setting.ActionCheck = () => MadeenPortraitReady;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion

    #region PvP Actions
    static partial void ModifyFireInRedPvP(ref ActionSetting setting)
    {
        // setting.ActionCheck = () => true;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyHolyInWhitePvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyCreatureMotifPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.CreatureMotifPvP) != ActionID.CreatureMotifPvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifyLivingMusePvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.LivingMusePvP) != ActionID.LivingMusePvP;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyMogOfTheAgesPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.MooglePortrait];
        setting.TargetStatusProvide = [StatusID.Silence_1347];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySmudgePvP(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Smudge_4113, StatusID.QuickSketch];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifyTemperaCoatPvP(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.TemperaCoat_4114];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifySubtractivePalettePvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Service.GetAdjustedActionId(ActionID.SubtractivePalettePvP) == ActionID.SubtractivePalettePvP;
        setting.StatusProvide = [StatusID.SubtractivePalette_4102];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifyAdventOfChocobastionPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyAeroInGreenPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Aetherhues_4100];
        setting.StatusProvide = [StatusID.AetherhuesIi_4101];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyWaterInBluePvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.AetherhuesIi_4101];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBlizzardInCyanPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.SubtractivePalette_4102];
        setting.StatusProvide = [StatusID.Aetherhues_4100];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyStoneInYellowPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Aetherhues_4100];
        setting.StatusProvide = [StatusID.AetherhuesIi_4101];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyThunderInMagentaPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.AetherhuesIi_4101];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyCometInBlackPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.SubtractivePalette_4102];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyPomMotifPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.PomSketch];
        setting.StatusProvide = [StatusID.PomMotif];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifyWingMotifPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.WingSketch];
        setting.StatusProvide = [StatusID.WingMotif];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifyClawMotifPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.ClawSketch];
        setting.StatusProvide = [StatusID.ClawMotif];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifyMawMotifPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.MawSketch];
        setting.StatusProvide = [StatusID.MawMotif];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifyPomMusePvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.PomMotif];
        setting.StatusProvide = [StatusID.WingSketch];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyWingedMusePvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.WingMotif];
        setting.StatusProvide = [StatusID.ClawSketch];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyClawedMusePvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.ClawMotif];
        setting.StatusProvide = [StatusID.MawSketch];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFangedMusePvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.MawMotif];
        setting.StatusProvide = [StatusID.PomSketch, StatusID.MadeenPortrait];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyRetributionOfTheMadeenPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.MadeenPortrait];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyTemperaGrassaPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.TemperaCoat];
        setting.StatusProvide = [StatusID.TemperaGrassa];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyStarPrismPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.StarPrism];
        setting.StatusProvide = [StatusID.StarPrism];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion
}