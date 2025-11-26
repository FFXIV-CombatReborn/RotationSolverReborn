using FFXIVClientStructs.FFXIV.Client.Game.Event;

namespace RotationSolver.Basic.Rotations.Duties;

/// <summary>
/// Represents a rotation for phantom duties in the game.
/// </summary>
[DutyTerritory(1252)]
public partial class PhantomRotation : DutyRotation
{
}

public partial class DutyRotation
{
    #region Item Tracking
    private static readonly BaseItem ZeninageItem = new(47740);
    private static readonly BaseItem OccultElixirItem = new(47743);
    private static readonly BaseItem OccultPotionItem = new(47741);
    #endregion

    #region Status Tracking

    /// <summary>
    /// Has a status that is important to the main rotation and should prevent Duty Actions from being executed.
    /// </summary>
    public static bool HasLockoutStatus => Player.HasStatus(true, StatusHelper.RotationLockoutStatus) && InCombat;

    /// <summary>
    /// Able to execute Cleansing.
    /// </summary>
    public static bool HasCleansing => Player.HasStatus(true, StatusID.PredictionOfCleansing) || Player.HasStatus(false, StatusID.PredictionOfCleansing);

    /// <summary>
    /// Able to execute Starfall.
    /// </summary>
    public static bool HasStarfall => Player.HasStatus(true, StatusID.PredictionOfStarfall) || Player.HasStatus(false, StatusID.PredictionOfStarfall);

    /// <summary>
    /// Able to execute Phantom Judgment.
    /// </summary>
    public static bool HasPhantomJudgment => Player.HasStatus(true, StatusID.PredictionOfJudgment) || Player.HasStatus(false, StatusID.PredictionOfJudgment);

    /// <summary>
    /// Able to execute Blessing.
    /// </summary>
    public static bool HasBlessing => Player.HasStatus(true, StatusID.PredictionOfBlessing) || Player.HasStatus(false, StatusID.PredictionOfBlessing);
    #endregion

    #region Freelancer
    /// <summary>
    /// Modifies the settings for Occult Resuscitation.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyOccultResuscitationPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => FreelancerLevel >= 5;
        setting.TargetType = TargetType.Self;
    }

    /// <summary>
    /// Modifies the settings for Occult Treasuresight.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyOccultTreasuresightPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => FreelancerLevel >= 10;
        setting.CreateConfig = () => new ActionConfig()
        {
            IsEnabled = false,
        };
    }
    #endregion

    #region Knight
    /// <summary>
    /// Modifies the settings for Phantom Guard.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyPhantomGuardPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => KnightLevel >= 1;
        setting.TargetType = TargetType.Self;
    }

    /// <summary>
    /// Modifies the settings for Pray.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyPrayPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => KnightLevel >= 2;
        setting.StatusProvide = [StatusID.Pray];
        setting.TargetType = TargetType.Self;
    }

    /// <summary>
    /// Modifies the settings for Occult Heal.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyOccultHealPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => KnightLevel >= 3;
    }

    /// <summary>
    /// Modifies the settings for Pledge.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyPledgePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => KnightLevel >= 6;
    }
    #endregion

    #region Monk
    /// <summary>
    /// Modifies the settings for Phantom Kick.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyPhantomKickPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => MonkLevel >= 1;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
            IsEnabled = false,
        };
    }

    /// <summary>
    /// Modifies the settings for Occult Counter.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyOccultCounterPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => MonkLevel >= 2;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// Modifies the settings for Counterstance.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyCounterstancePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => MonkLevel >= 3;
        setting.StatusProvide = [StatusID.Counterstance];
        setting.TargetType = TargetType.Self;
    }

    /// <summary>
    /// Modifies the settings for Occult Chakra.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyOccultChakraPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => MonkLevel >= 5;
        setting.TargetType = TargetType.Self;
    }
    #endregion

    #region Bard
    /// <summary>
    /// Modifies the settings for Offensive Aria.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyOffensiveAriaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BardLevel >= 1 && !Player.HasStatus(false, StatusID.HerosRime) && !Player.HasStatus(true, StatusID.HerosRime);
        setting.StatusProvide = [StatusID.OffensiveAria, StatusID.HerosRime];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// Modifies the settings for Romeo's Ballad.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyRomeosBalladPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BardLevel >= 2;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// Modifies the settings for Mighty March.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyMightyMarchPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BardLevel >= 3;
        setting.StatusProvide = [StatusID.MightyMarch];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// Modifies the settings for Hero's Rime.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyHerosRimePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BardLevel >= 4;
        setting.StatusProvide = [StatusID.HerosRime];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion

    #region Chemist
    /// <summary>
    /// Modifies the settings for Occult Potion.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyOccultPotionPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ChemistLevel >= 1 && OccultPotionItem.HasIt;
        setting.IsFriendly = true;
        setting.MPOverride = () => 0;
    }

    /// <summary>
    /// Modifies the settings for Occult Ether.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyOccultEtherPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ChemistLevel >= 2 && OccultPotionItem.HasIt;
        setting.IsFriendly = true;
        setting.MPOverride = () => 0;
    }

    /// <summary>
    /// Modifies the settings for Revive.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyRevivePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ChemistLevel >= 3;
        setting.IsFriendly = true;
    }

    /// <summary>
    /// Modifies the settings for Occult Elixir.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyOccultElixirPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ChemistLevel >= 4 && OccultElixirItem.HasIt;
        setting.IsFriendly = true;
        setting.MPOverride = () => 0;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion

    #region TimeMage
    /// <summary>
    /// Modifies the settings for Occult Slowga.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyOccultSlowgaPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => TimeMageLevel >= 1;
        setting.TargetStatusProvide = [StatusID.Slow_3493];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.CanTarget = (tar) => !tar.IsBossFromIcon() && tar.IsAttackable() && tar.GetEventType() != EventHandlerContent.PublicContentDirector;
    }

    /// <summary>
    /// Modifies the settings for Occult Comet.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyOccultCometPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => TimeMageLevel >= 2;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// Modifies the settings for Occult Mage Masher.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyOccultMageMasherPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => TimeMageLevel >= 3;
    }

    /// <summary>
    /// Modifies the settings for Occult Dispel.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyOccultDispelPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => TimeMageLevel >= 4;
        setting.CanTarget = tar => tar.HasStatus(false, StatusHelper.PhantomDispellable);
    }

    /// <summary>
    /// Modifies the settings for Occult Quick.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyOccultQuickPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => TimeMageLevel >= 5;
        setting.TargetType = TargetType.Self;
        setting.StatusProvide = StatusHelper.SwiftcastStatus;
    }
    #endregion

    #region Cannoneer
    /// <summary>
    /// Modifies the settings for Phantom Fire.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyPhantomFirePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => CannoneerLevel >= 1;
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// Modifies the settings for PhantomAimPvE.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyHolyCannonPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => CannoneerLevel >= 2;
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// Modifies the settings for PhantomAimPvE.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyDarkCannonPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => CannoneerLevel >= 3;
        setting.TargetType = TargetType.DarkCannon;
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// Modifies the settings for PhantomAimPvE.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyShockCannonPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => CannoneerLevel >= 4;
        setting.TargetType = TargetType.ShockCannon;
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// Modifies the settings for PhantomAimPvE.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifySilverCannonPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => CannoneerLevel >= 6;
        setting.TargetStatusProvide = [StatusID.SilverSickness];
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion

    #region Oracle
    /// <summary>
    /// Modifies the settings for Predict.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyPredictPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => OracleLevel >= 1;
    }

    /// <summary>
    /// Modifies the settings for Cleansing.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyCleansingPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => OracleLevel >= 1 && HasCleansing;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// Modifies the settings for Starfall.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyStarfallPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => OracleLevel >= 1 && HasStarfall;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// Modifies the settings for Phantom Judgment.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyPhantomJudgmentPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => OracleLevel >= 1 && HasPhantomJudgment;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// Modifies the settings for Blessing.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyBlessingPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => OracleLevel >= 1 && HasBlessing;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// Modifies the settings for Recuperation.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyRecuperationPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => OracleLevel >= 2;
        setting.StatusProvide = [StatusID.Recuperation_4271, StatusID.FortifiedRecuperation];
    }

    /// <summary>
    /// Modifies the settings for Phantom Doom.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyPhantomDoomPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => OracleLevel >= 3 && InCombat;
        setting.TargetStatusProvide = [StatusID.PhantomDoom];
        setting.CanTarget = tar => !tar.IsBossFromIcon() && tar.IsAttackable() && tar.GetEventType() != EventHandlerContent.PublicContentDirector && tar.InCombat();
    }

    /// <summary>
    /// Modifies the settings for Phantom Rejuvenation.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyPhantomRejuvenationPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => OracleLevel >= 4;
        setting.StatusProvide = [StatusID.PhantomRejuvenation];
    }

    /// <summary>
    /// Modifies the settings for Invulnerability.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyInvulnerabilityPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => OracleLevel >= 6;
        setting.TargetStatusProvide = [StatusID.Invulnerability];
    }
    #endregion

    #region Berserker
    /// <summary>
    /// Modifies the settings for Rage.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyRagePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BerserkerLevel >= 1;
    }

    /// <summary>
    /// Modifies the settings for Deadly Blow.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyDeadlyBlowPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => BerserkerLevel >= 2;
    }
    #endregion

    #region Ranger
    /// <summary>
    /// Modifies the settings for Phantom Aim.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyPhantomAimPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => RangerLevel >= 1;
        setting.StatusProvide = [StatusID.DeadlyPhantomAim];
    }

    /// <summary>
    /// Modifies the settings for Occult Featherfoot.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyOccultFeatherfootPvE(ref ActionSetting setting)
    {
        setting.TargetType = TargetType.Move;
        setting.ActionCheck = () => RangerLevel >= 2;
    }

    /// <summary>
    /// Modifies the settings for Occult Falcon.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyOccultFalconPvE(ref ActionSetting setting)
    {
        setting.TargetType = TargetType.Interrupt;
        setting.ActionCheck = () => RangerLevel >= 4;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// Modifies the settings for Occult Unicorn.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyOccultUnicornPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => RangerLevel >= 6;
        setting.StatusProvide = [StatusID.OccultUnicorn];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion

    #region Thief
    /// <summary>
    /// Modifies the settings for Occult Sprint.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyOccultSprintPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ThiefLevel >= 1;
        setting.StatusProvide = [StatusID.OccultSprint];
    }

    /// <summary>
    /// Modifies the settings for Steal.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyStealPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ThiefLevel >= 2;
        setting.TargetType = TargetType.LowHP;
    }

    /// <summary>
    /// Modifies the settings for Vigilance.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyVigilancePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ThiefLevel >= 3 && !InCombat;
        setting.StatusProvide = [StatusID.Vigilance];
    }

    /// <summary>
    /// Modifies the settings for Trap Detection.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyTrapDetectionPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ThiefLevel >= 4 && DataCenter.IsInForkedTower;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// Modifies the settings for Pilfer Weapon.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyPilferWeaponPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ThiefLevel >= 5;
        setting.TargetStatusProvide = [StatusID.WeaponPilfered];
    }
    #endregion

    #region Samurai
    /// <summary>
    /// Modifies the settings for Mineuchi.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyMineuchiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SamuraiLevel >= 1;
        setting.TargetType = TargetType.Interrupt;
    }

    /// <summary>
    /// Modifies the settings for Shirahadori.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyShirahadoriPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SamuraiLevel >= 2;
        setting.TargetType = TargetType.Self;
        setting.StatusNeed = [StatusID.Shirahadori];
    }

    /// <summary>
    /// Modifies the settings for Iainuki.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyIainukiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SamuraiLevel >= 3;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// Modifies the settings for Zeninage.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyZeninagePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => SamuraiLevel >= 4 && ZeninageItem.HasIt;
        setting.MPOverride = () => 0;
    }
    #endregion

    #region Geomancer
    /// <summary>
    /// Modifies the settings for Battle Bell.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyBattleBellPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => GeomancerLevel >= 1;
        setting.TargetStatusProvide = [StatusID.BattleBell]; 
        setting.TargetType = TargetType.PhantomBell;
    }

    /// <summary>
    /// Modifies the settings for Weather.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyWeatherPvE(ref ActionSetting setting)
    {
        //this isn't a real action
    }

    /// <summary>
    /// Modifies the settings for Weather.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifySunbathPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => GeomancerLevel >= 2;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// Modifies the settings for Weather.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyCloudyCaressPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => GeomancerLevel >= 2;
        setting.StatusProvide = [StatusID.CloudyCaress];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// Modifies the settings for Weather.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyBlessedRainPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => GeomancerLevel >= 2;
        setting.StatusProvide = [StatusID.BlessedRain];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// Modifies the settings for Weather.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyMistyMiragePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => GeomancerLevel >= 2;
        setting.StatusProvide = [StatusID.MistyMirage];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// Modifies the settings for Weather.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyHastyMiragePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => GeomancerLevel >= 2;
        setting.StatusProvide = [StatusID.HastyMirage];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// Modifies the settings for Weather.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyAetherialGainPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => GeomancerLevel >= 2;
        setting.StatusProvide = [StatusID.AetherialGain];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    /// Modifies the settings for Ringing Respite.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifyRingingRespitePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => GeomancerLevel >= 3;
        setting.StatusProvide = [StatusID.RingingRespite];
        setting.TargetType = TargetType.PhantomRespite;
    }

    /// <summary>
    /// Modifies the settings for Suspend.
    /// </summary>
    /// <param name="setting">The action setting to modify.</param>
    static partial void ModifySuspendPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => GeomancerLevel >= 4;
        setting.TargetStatusProvide = [StatusID.Suspend];
        setting.IsFriendly = true;
    }
    #endregion
}