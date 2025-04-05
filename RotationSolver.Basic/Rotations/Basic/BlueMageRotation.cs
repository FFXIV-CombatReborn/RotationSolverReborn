using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using FFXIVClientStructs.FFXIV.Client.Game;
using CombatRole = RotationSolver.Basic.Data.CombatRole;

namespace RotationSolver.Basic.Rotations.Basic;


partial class BlueMageRotation
{
    /// <summary>
    /// 
    /// </summary>

    public enum BluDPSSpell : byte
    {
        /// <summary>
        /// 
        /// </summary>
        WaterCannon,

        /// <summary>
        /// 
        /// </summary>
        SonicBoom,

        /// <summary>
        /// 
        /// </summary>
        GoblinPunch,

        /// <summary>
        /// 
        /// </summary>
        SharpenedKnife,
    }

    /// <summary>
    /// 
    /// </summary>
    public enum BluAOESpell : byte
    {
        /// <summary>
        /// 
        /// </summary>
        Glower,

        /// <summary>
        /// 
        /// </summary>
        FlyingFrenzy,

        /// <summary>
        /// 
        /// </summary>
        FlameThrower,

        /// <summary>
        /// 
        /// </summary>
        DrillCannons,

        /// <summary>
        /// 
        /// </summary>
        Plaincracker,

        /// <summary>
        /// 
        /// </summary>
        HighVoltage,

        /// <summary>
        /// 
        /// </summary>
        MindBlast,

        /// <summary>
        /// 
        /// </summary>
        ThousandNeedles,

        /// <summary>
        /// 
        /// </summary>
        ChocoMeteor,

        /// <summary>
        ///
        /// </summary>
        FeatherRain,
    }

    /// <summary>
    /// 
    /// </summary>
    public enum BluHealSpell : byte
    {
        /// <summary>
        /// 
        /// </summary>
        WhiteWind,

        /// <summary>
        /// 
        /// </summary>
        AngelsSnack,

        /// <summary>
        /// 
        /// </summary>
        PomCure,
    }

    /// <summary>
    /// 
    /// </summary>
    public Dictionary<BluDPSSpell, IBaseAction> BluDPSSpellActions = [];

    /// <summary>
    /// 
    /// </summary>
    public Dictionary<BluAOESpell, IBaseAction> BluAOESpellActions = [];

    /// <summary>
    /// 
    /// </summary>
    public Dictionary<BluHealSpell, IBaseAction> BluHealSpellActions = [];

    /// <summary>
    /// 
    /// </summary>
    public override MedicineType MedicineType => MedicineType.Intelligence;

    private protected sealed override IBaseAction Raise => AngelWhisperPvE;
    private protected sealed override IBaseAction TankStance => MightyGuardPvE;

    /// <summary>
    /// Represents the collection of active abilities or actions that are currently configured
    /// for the Blue Mage's rotation. This property determines which abilities are available
    /// and used in the rotation solver logic.
    /// The actions stored in this property are of type <see cref="IBaseAction"/>
    /// and are managed to ensure they comply with game constraints, such
    /// as the maximum and minimum number of active actions.
    /// Custom setter logic validates and applies the selected Blue Mage actions,
    /// returning whether the operation was successful. For instance, it ensures
    /// the number of actions is within valid boundaries and synchronizes with the game state when necessary.
    /// Any errors encountered during the set operation (e.g., exceeding allowable actions
    /// or synchronization issues) are logged for debugging purposes.
    /// </summary>
    protected abstract IBaseAction[] ActiveActions { get; }

    public BlueMageRotation()
    {
        SetBlueMageActions();
    }

    /// <summary>
    /// Attempts to set the actions for the Blue Mage character.
    /// </summary>
    /// <param name="actions">An array of actions implementing the <see cref="IBaseAction"/> interface to be set for the Blue Mage. The maximum number of actions is 24.</param>
    /// <returns>Returns <c>true</c> if the actions are successfully set; otherwise, returns <c>false</c>.</returns>
    protected unsafe bool SetBlueMageActions()
    {
        if (!Service.Config.SetBluActions)
            return false;
        if (ActiveActions.Length > 24 || ActiveActions.Length == 0)
        {
            Svc.Log.Error($"Active actions count {ActiveActions.Length} is invalid.");
            return false;
        }

        try
        {
            var idArray = ActiveActions.Where(a => a.Info.SpellUnlocked).Select(a => a.Action.RowId).ToArray();
            if (idArray.Equals(GetBlueMageActions()))
                return true;
            var actionManager = ActionManager.Instance();
            fixed (uint* idArrayPtr = idArray)
            {
                return actionManager->SetBlueMageActions(idArrayPtr);
            }

        }
        catch (Exception ex)
        {
            Svc.Log.Error($"Failed to set BlueMage actions: {ex.Message}");
            return false;
        }
    }

    private unsafe uint[] GetBlueMageActions()
    {
        var actionManager = ActionManager.Instance();
        if (actionManager == null) return [];

        List<uint> loadedActionIds = [];
        for (var slot = 1; slot < 24; slot++)
        {
            var loadedId = actionManager->GetActiveBlueMageActionInSlot(slot);
            if (loadedId > 0)
                loadedActionIds.Add(loadedId);
        }
        return loadedActionIds.ToArray();
    }

    /// <summary>
    /// 
    /// </summary>
    public CombatRole BlueId { get; protected set; } = CombatRole.DPS;

    static partial void ModifyWaterCannonPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyFlameThrowerPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyAquaBreathPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Dropsy_1736];
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyFlyingFrenzyPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyDrillCannonsPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyHighVoltagePvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Paralysis];
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyLoomPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFinalStingPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !Player.HasStatus(true, StatusID.BrushWithDeath);
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySongOfTormentPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Bleeding_1714];
    }

    static partial void ModifyGlowerPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Paralysis];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyPlaincrackerPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBristlePvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
        setting.StatusProvide = [StatusID.Boost_1716, StatusID.Harmonized];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyWhiteWindPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyLevel5PetrifyPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Petrification];
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySharpenedKnifePvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyIceSpikesPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.IceSpikes_1720, StatusID.VeilOfTheWhorl_1724, StatusID.Schiltron];
        setting.IsFriendly = false;
    }

    static partial void ModifyBloodDrainPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyAcornBombPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Sleep];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBombTossPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Stun];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyOffguardPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Offguard];
    }

    static partial void ModifySelfdestructPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !Player.HasStatus(true, StatusID.BrushWithDeath);
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyTransfusionPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !Player.HasStatus(true, StatusID.BrushWithDeath);
    }

    static partial void ModifyFazePvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Stun];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFlyingSardinePvE(ref ActionSetting setting)
    {
        //setting.TargetType = TargetType.Interrupt;
    }

    static partial void ModifySnortPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = false;
    }

    static partial void Modify_4TonzeWeightPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Heavy];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyTheLookPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = false;
        //setting.TargetType = TargetType.Provoke;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBadBreathPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = false;
        setting.TargetStatusProvide = [StatusID.Slow, StatusID.Heavy, StatusID.Blind, StatusID.Paralysis, StatusID.Poison, StatusID.Malodorous];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyDiamondbackPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Diamondback];
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyMightyGuardPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.MightyGuard];
    }

    static partial void ModifyStickyTonguePvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Stun];
    }

    static partial void ModifyToadOilPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.ToadOil];
        setting.IsFriendly = true;
    }

    static partial void ModifyTheRamsVoicePvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.DeepFreeze_1731];
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyTheDragonsVoicePvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Paralysis];
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyMissilePvE(ref ActionSetting setting)
    {

    }

    static partial void Modify_1000NeedlesPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyInkJetPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Blind];
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFireAngonPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyMoonFlutePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.WaxingNocturne];
        setting.IsFriendly = true;
    }

    static partial void ModifyTailScrewPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyMindBlastPvE(ref ActionSetting setting)
    {
        //setting.TargetStatusProvide = [StatusID.Paralysis];
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyDoomPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Doom_1738];
    }

    static partial void ModifyPeculiarLightPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.PeculiarLight];
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyFeatherRainPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Windburn_1723];
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyEruptionPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyMountainBusterPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyShockStrikePvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyGlassDancePvE(ref ActionSetting setting)
    {
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyVeilOfTheWhorlPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.IceSpikes_1720, StatusID.VeilOfTheWhorl_1724, StatusID.Schiltron];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyAlpineDraftPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyProteanWavePvE(ref ActionSetting setting)
    {
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyNortherliesPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyKaltstrahlPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyAbyssalTransfixionPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Paralysis];
    }

    static partial void ModifyChirpPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Sleep];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyEerieSoundwavePvE(ref ActionSetting setting)
    {
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyPomCurePvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
    }

    static partial void ModifyGobskinPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Gobskin];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyMagicHammerPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Conked];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyAvailPvE(ref ActionSetting setting)
    {
        //setting.StatusProvide = [StatusID.Avail];
    }

    static partial void ModifyFrogLegsPvE(ref ActionSetting setting)
    {
        setting.TargetType = TargetType.Provoke;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySonicBoomPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyWhistlePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Boost_1716, StatusID.Harmonized];
        setting.IsFriendly = true;
    }

    static partial void ModifyWhiteKnightsTourPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Blind];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBlackKnightsTourPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Slow];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyLevel5DeathPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyLauncherPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyPerpetualRayPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Stun];

    }

    static partial void ModifyCactguardPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Cactguard];
        setting.TargetType = TargetType.BeAttacked;
    }

    static partial void ModifyRevengeBlastPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Player.GetHealthRatio() > 0.2;
    }

    static partial void ModifyAngelWhisperPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyExuviationPvE(ref ActionSetting setting)
    {
        setting.TargetType = TargetType.Dispel;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyRefluxPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Heavy];
    }

    static partial void ModifyDevourPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyCondensedLibraPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.PhysicalAttenuation, StatusID.AstralAttenuation, StatusID.UmbralAttenuation];
    }

    static partial void ModifyAethericMimicryPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
        setting.TargetType = TargetType.MimicryTarget;
        setting.StatusProvide =
            [StatusID.AethericMimicryDps, StatusID.AethericMimicryHealer, StatusID.AethericMimicryTank];
    }

    static partial void ModifySurpanakhaPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.SurpanakhasFury];
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyQuasarPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyJKickPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !Player.HasStatus(false, StatusID.Bind);
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyTripleTridentPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyTinglePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Tingling];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyTatamigaeshiPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Tingling];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyColdFogPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.ColdFog];
        setting.TargetType = TargetType.Self;
        setting.IsFriendly = true;
    }

    static partial void ModifyWhiteDeathPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.TouchOfFrost];
        setting.TargetStatusProvide = [StatusID.DeepFreeze_1731];
    }

    static partial void ModifyStotramPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifySaintlyBeamPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyFeculentFloodPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyAngelsSnackPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyChelonianGatePvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
        setting.StatusProvide = [StatusID.ChelonianGate];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyDivineCataractPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.AuspiciousTrance];
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyTheRoseOfDestructionPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyBasicInstinctPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
        setting.StatusProvide = [StatusID.BasicInstinct];
        setting.ActionCheck = () => IsInDuty && PartyMembers.Count() <= 1 && DataCenter.Territory?.ContentType != TerritoryContentType.TheMaskedCarnivale;
    }

    static partial void ModifyUltravibrationPvE(ref ActionSetting setting)
    {
        setting.TargetStatusNeed = [StatusID.DeepFreeze_1731, StatusID.Petrification];
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBlazePvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyMustardBombPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyDragonForcePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.DragonForce];
        setting.IsFriendly = false;
    }

    static partial void ModifyAetherialSparkPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Bleeding_1714];
    }

    static partial void ModifyHydroPullPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyMaledictionOfWaterPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyChocoMeteorPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyMatraMagicPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyPeripheralSynthesisPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyBothEndsPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyPhantomFlurryPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = false;
        setting.ActionCheck = () => !IsMoving;
        setting.StatusProvide = [StatusID.PhantomFlurry];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyNightbloomPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = false;
        setting.TargetStatusProvide = [StatusID.Bleeding_1714];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyGoblinPunchPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyRightRoundPvE(ref ActionSetting setting)
    {
        //Need data
    }

    static partial void ModifySchiltronPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.IceSpikes_1720, StatusID.VeilOfTheWhorl_1724, StatusID.Schiltron];
    }

    static partial void ModifyRehydrationPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = true;
    }

    static partial void ModifyBreathOfMagicPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.BreathOfMagic];
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyWildRagePvE(ref ActionSetting setting)
    {
        //Need data
    }

    static partial void ModifyPeatPeltPvE(ref ActionSetting setting)
    {
        //Need data
    }

    static partial void ModifyDeepCleanPvE(ref ActionSetting setting)
    {
        //Need data
    }

    static partial void ModifyRubyDynamicsPvE(ref ActionSetting setting)
    {
        //Need data
    }

    static partial void ModifyDivinationRunePvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyDimensionalShiftPvE(ref ActionSetting setting)
    {
        //Need data
    }

    static partial void ModifyConvictionMarcatoPvE(ref ActionSetting setting)
    {
        //Need data
    }

    static partial void ModifyForceFieldPvE(ref ActionSetting setting)
    {
        //Need data
    }

    static partial void ModifyWingedReprobationPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.WingedReprobation, StatusID.WingedRedemption];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyLaserEyePvE(ref ActionSetting setting)
    {
        //Need data
    }

    static partial void ModifyCandyCanePvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.CandyCane];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyMortalFlamePvE(ref ActionSetting setting)
    {
        //Need data
    }

    static partial void ModifySeaShantyPvE(ref ActionSetting setting)
    {
        //Need data
    }

    static partial void ModifyApokalypsisPvE(ref ActionSetting setting)
    {
        //Need data
    }

    static partial void ModifyBeingMortalPvE(ref ActionSetting setting)
    {
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    /// <summary>
    ///
    /// </summary>
    public override void DisplayStatus()
    {
        ImGui.TextWrapped($"Aetheric Mimicry Role: {BlueId.ToString()}");
        ImGui.Text($"This rotation requires the following actions:");
        foreach (var action in ActiveActions)
        {
            ImGui.Text($" - {action.Name}");
        }
    }
}
