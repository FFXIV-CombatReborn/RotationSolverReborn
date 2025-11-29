using RotationSolver.Basic.Rotations.Duties;
using System.ComponentModel;

namespace RotationSolver.RebornRotations.Duty;

[Rotation("Phantom Jobs Loaded", CombatType.PvE)]

public sealed class PhantomDefault : PhantomRotation
{
    #region Configs
    [RotationConfig(CombatType.PvE, Name = "Save Phantom Attacks for class specific damage bonus?")]
    public bool SaveForBurstWindow { get; set; } = true;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Player HP percent needed to use Occult Resuscitation", PhantomJob = PhantomJob.Freelancer)]
    public float OccultResuscitationThreshold { get; set; } = 0.7f;

    [RotationConfig(CombatType.PvE, Name = "Use Pray as a Heal", PhantomJob = PhantomJob.Knight)]
    public bool PrayHeal { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Phantom Judgement", PhantomJob = PhantomJob.Oracle)]
    public bool PhantomJudgementUseage { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Cleansing", PhantomJob = PhantomJob.Oracle)]
    public bool CleansingUseage { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Blessing", PhantomJob = PhantomJob.Oracle)]
    public bool BlessingUseage { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Starfall", PhantomJob = PhantomJob.Oracle)]
    public bool StarfallUseage { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Invulnerability for Starfall", PhantomJob = PhantomJob.Oracle)]
    public bool SaveInvulnForStarfall { get; set; } = true;

    [Range(1, 15, ConfigUnitType.Yalms)]
    [RotationConfig(CombatType.PvE, Name = "Max distance you can be from target for Phantom Kick use (Danger, you will die)", PhantomJob = PhantomJob.Monk)]
    public float PhantomKickDistance { get; set; } = 5f;

    [Range(0, 10000, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Your MP needed to use Occult Chakra", PhantomJob = PhantomJob.Monk)]
    public int OccultChakraMPThreshold { get; set; } = 3000;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Your HP percentage needed to use Occult Chakra", PhantomJob = PhantomJob.Monk)]
    public float OccultChakraHPThreshold { get; set; } = 0.3f;

    [RotationConfig(CombatType.PvE, Name = "Use Dark Cannon or Shock Cannon in cases where the mob is immune to both blind and paralysis", PhantomJob = PhantomJob.Cannoneer)]
    public DarkShockCannonImmuneStrategy DarkShockCannonImmuneUsage { get; set; } = DarkShockCannonImmuneStrategy.DarkCannon;

    public enum DarkShockCannonImmuneStrategy : byte
    {
        [Description("Dark Cannon")]
        DarkCannon,

        [Description("Shock Cannon")]
        ShockCannon,
    }

    [RotationConfig(CombatType.PvE, Name = "Use Dark Cannon or Shock Cannon in cases where the mob is susceptible to both blind and paralysis", PhantomJob = PhantomJob.Cannoneer)]
    public DarkShockCannonStrategy DarkShockCannonUsage { get; set; } = DarkShockCannonStrategy.DarkCannon;

    public enum DarkShockCannonStrategy : byte
    {
        [Description("Dark Cannon")]
        DarkCannon,

        [Description("Shock Cannon")]
        ShockCannon,
    }

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Average party HP percent to predict to heal with judgement instead of damage things", PhantomJob = PhantomJob.Oracle)]
    public float PredictJudgementThreshold { get; set; } = 0.7f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Average party HP percent to predict to heal instead of damage things", PhantomJob = PhantomJob.Oracle)]
    public float PredictBlessingThreshold { get; set; } = 0.5f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Average party HP percent needed to use Occult Elixir", PhantomJob = PhantomJob.Chemist)]
    public float OccultElixirThreshold { get; set; } = 0.3f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Target HP percent needed to use Occult Potion", PhantomJob = PhantomJob.Chemist)]
    public float OccultPotionThreshold { get; set; } = 0.5f;

    [RotationConfig(CombatType.PvE, Name = "Only use Occult Potion on self", PhantomJob = PhantomJob.Chemist)]
    public bool OccultPotionSelf { get; set; } = true;

    [Range(0, 10000, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Target MP needed to use Occult Ether", PhantomJob = PhantomJob.Chemist)]
    public int OccultEtherThreshold { get; set; } = 2000;

    [RotationConfig(CombatType.PvE, Name = "Only use Occult Ether on self", PhantomJob = PhantomJob.Chemist)]
    public bool OccultEtherSelf { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Suspend out of combat", PhantomJob = PhantomJob.Geomancer)]
    public bool SuspendOut { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Suspend in combat", PhantomJob = PhantomJob.Geomancer)]
    public bool SuspendIn { get; set; } = false;

    #endregion

    readonly HashSet<IBaseAction> _remainingCards = new(4);
    private IBaseAction? _currentCard = null;

    public override void DisplayDutyStatus()
    {
        base.DisplayDutyStatus();
        if (string.Equals(ActivePhantomJob, "Oracle", StringComparison.OrdinalIgnoreCase))
        {
            ImGui.Text($"Remaining Cards: {_remainingCards.Count}");
        }
    }

    public override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (VigilancePvE.CanUse(out act))
        {
            return true;
        }

        if (HandleOraclePrediction(nextGCD, out act))
        {
            return true;
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    public override bool InterruptAbility(IAction nextGCD, out IAction? act)
    {
        if (HasLockoutStatus)
        {
            return base.InterruptAbility(nextGCD, out act);
        }

        if (OccultFalconPvE.CanUse(out act))
        {
            return true;
        }

        if (CleansingPvE.CanUse(out act)) // Technically this is an interrupt but it probably won't come up often
        {
            return true;
        }

        if (RomeosBalladPvE.CanUse(out act))
        {
            return true;
        }

        return base.InterruptAbility(nextGCD, out act);
    }

    public override bool DispelAbility(IAction nextGCD, out IAction? act)
    {
        if (HasLockoutStatus)
        {
            return base.DispelAbility(nextGCD, out act);
        }

        if (RecuperationPvE.CanUse(out act))
        {
            return true;
        }

        return base.DispelAbility(nextGCD, out act);
    }

    public override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        if (HasLockoutStatus)
        {
            return base.GeneralAbility(nextGCD, out act);
        }

        if (BattleBellPvE.CanUse(out act))
        {
            return true;
        }

        if (RingingRespitePvE.CanUse(out act))
        {
            return true;
        }

        if (InCombat && HerosRimePvE.CanUse(out act))
        {
            return true;
        }

        if (InCombat && OffensiveAriaPvE.CanUse(out act))
        {
            return true;
        }

        if (InCombat && PhantomAimPvE.CanUse(out act))
        {
            return true;
        }

        if (!IsMoving && OccultSprintPvE.CanUse(out act))
        {
            return true;
        }

        if (SuspendPvE.CanUse(out act))
        {
            if (!InCombat && SuspendOut)
            {
                return true;
            }
            if (InCombat && SuspendIn)
            {
                return true;
            }
        }

        if (OccultEtherPvE.CanUse(out act) && InCombat)
        {
            if (!OccultEtherSelf)
            {
                if (OccultEtherPvE.Target.Target.CurrentMp < OccultEtherThreshold)
                    return true;
            }
            else
            {
                if (OccultEtherPvE.Target.Target == Player && (Player.CurrentMp < OccultEtherThreshold))
                    return true;
            }
        }

        if (OccultChakraPvE.CanUse(out act) && InCombat)
        {
            if (Player.CurrentMp < OccultChakraMPThreshold)
                return true;
        }

        return base.GeneralAbility(nextGCD, out act);
    }

    public override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (HasLockoutStatus)
        {
            return base.AttackAbility(nextGCD, out act);
        }
        #region Utility/Non-scaling abilities that don't care about burst
        if (PhantomDoomPvE.CanUse(out act))
        {
            return true;
        }

        if (StealPvE.CanUse(out act))
        {
            return true;
        }
        #endregion Utility/Non-scaling abilities that don't care about burst

        #region Burst abilities
        if (ShouldHoldBurst())
        {
            return base.AttackAbility(nextGCD, out act);
        }

        if (PhantomKickPvE.CanUse(out act))
        {
            if (PhantomKickPvE.Target.Target.DistanceToPlayer() <= PhantomKickDistance)
            {
                return true;
            }
        }

        if (PilferWeaponPvE.CanUse(out act))
        {
            return true;
        }

        if (OccultCounterPvE.CanUse(out act, checkActionManager: true))
        {
            return true;
        }
        #endregion Burst abilities

        return base.AttackAbility(nextGCD, out act);
    }

    public override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (HasLockoutStatus)
        {
            return base.DefenseSingleAbility(nextGCD, out act);
        }

        if (PhantomGuardPvE.CanUse(out act))
        {
            return true;
        }

        if (InvulnerabilityPvE.CanUse(out act) && !SaveInvulnForStarfall)
        {
            return true;
        }

        if (ShirahadoriPvE.CanUse(out act))
        {
            return true;
        }

        return base.DefenseSingleAbility(nextGCD, out act);
    }

    public override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (HasLockoutStatus)
        {
            return base.DefenseAreaAbility(nextGCD, out act);
        }

        if (MightyMarchPvE.CanUse(out act))
        {
            return true;
        }

        if (OccultUnicornPvE.CanUse(out act))
        {
            return true;
        }

        if (PhantomRejuvenationPvE.CanUse(out act))
        {
            return true;
        }

        if (OccultMageMasherPvE.CanUse(out act))
        {
            return true;
        }

        if (TrapDetectionPvE.CanUse(out act))
        {
            return true;
        }

        if (ShirahadoriPvE.CanUse(out act))
        {
            return true;
        }

        return base.DefenseAreaAbility(nextGCD, out act);
    }

    public override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (HasLockoutStatus)
        {
            return base.HealSingleAbility(nextGCD, out act);
        }

        if (OccultHealPvE.CanUse(out act))
        {
            return true;
        }

        if (BlessingUseage && BlessingPvE.CanUse(out act) && (PartyMembersAverHP < PredictBlessingThreshold || Player.GetHealthRatio() < PredictBlessingThreshold)) // Phantom heal gets a larger threshold than normal healing
        {
            return true;
        }

        if (PhantomJudgementUseage && (PartyMembersAverHP < PredictJudgementThreshold || Player.GetHealthRatio() < PredictJudgementThreshold) && PhantomJudgmentPvE.CanUse(out act)) // Heal the party or self if we're below the heal + damage threshold
        {
            return true;
        }

        if (OccultChakraPvE.CanUse(out act))
        {
            return true;
        }

        if (OccultPotionPvE.CanUse(out act) && InCombat)
        {
            if (!OccultPotionSelf)
            {
                if (OccultPotionPvE.Target.Target.GetHealthRatio() < OccultPotionThreshold)
                    return true;
            }
            else
            {
                if (OccultPotionPvE.Target.Target == Player && Player.GetHealthRatio() < OccultPotionThreshold)
                    return true;
            }
        }

        if (OccultChakraPvE.CanUse(out act) && InCombat)
        {
            if (Player.GetHealthRatio() < OccultChakraHPThreshold)
                return true;
        }

        return base.HealSingleAbility(nextGCD, out act);
    }

    public override bool HealAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (HasLockoutStatus)
        {
            return base.HealAreaAbility(nextGCD, out act);
        }

        if (BlessingUseage && BlessingPvE.CanUse(out act) && (PartyMembersAverHP < PredictBlessingThreshold || Player.GetHealthRatio() < PredictBlessingThreshold)) // Phantom heal gets a larger threshold than normal healing
        {
            return true;
        }

        if (PhantomJudgementUseage && (PartyMembersAverHP < PredictJudgementThreshold || Player.GetHealthRatio() < PredictJudgementThreshold) && PhantomJudgmentPvE.CanUse(out act)) // Heal the party or self if we're below the heal + damage threshold
        {
            return true;
        }

        if (OccultElixirPvE.CanUse(out act))
        {
            if (InCombat && (PartyMembersAverHP < OccultElixirThreshold))
            {
                return true;
            }
        }

        return base.HealAreaAbility(nextGCD, out act);
    }

    public override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (HasLockoutStatus)
        {
            return base.MoveForwardAbility(nextGCD, out act);
        }

        if (OccultFeatherfootPvE.CanUse(out act))
        {
            return true;
        }

        return base.MoveForwardAbility(nextGCD, out act);
    }

    public override bool MyInterruptGCD(out IAction? act)
    {
        if (HasLockoutStatus)
        {
            return base.MyInterruptGCD(out act);
        }

        if (MineuchiPvE.CanUse(out act))
        {
            return true;
        }

        return base.MyInterruptGCD(out act);
    }

    public override bool RaiseGCD(out IAction? act)
    {
        if (HasLockoutStatus)
        {
            return base.RaiseGCD(out act);
        }

        if (RevivePvE.CanUse(out act))
        {
            return true;
        }

        return base.RaiseGCD(out act);
    }

    public override bool HealSingleGCD(out IAction? act)
    {
        if (HasLockoutStatus)
        {
            return base.HealSingleGCD(out act);
        }

        if (OccultResuscitationPvE.CanUse(out act))
        {
            if (Player.GetHealthRatio() < OccultResuscitationThreshold)
            {
                return true;
            }
        }

        if (PrayPvE.CanUse(out act))
        {
            return true;
        }

        return base.HealSingleGCD(out act);
    }

    public override bool HealAreaGCD(out IAction? act)
    {
        if (HasLockoutStatus)
        {
            return base.HealAreaGCD(out act);
        }

        if (SunbathPvE.CanUse(out act))
        {
            return true;
        }

        if (CloudyCaressPvE.CanUse(out act))
        {
            return true;
        }

        return base.HealAreaGCD(out act);
    }

    public override bool DefenseSingleGCD(out IAction? act)
    {
        if (HasLockoutStatus)
        {
            return base.DefenseSingleGCD(out act);
        }

        if (PrayPvE.CanUse(out act))
        {
            return true;
        }

        return base.DefenseSingleGCD(out act);
    }

    public override bool DefenseAreaGCD(out IAction? act)
    {
        if (HasLockoutStatus)
        {
            return base.DefenseAreaGCD(out act);
        }

        if (CloudyCaressPvE.CanUse(out act))
        {
            return true;
        }

        if (BlessedRainPvE.CanUse(out act))
        {
            return true;
        }

        return base.DefenseAreaGCD(out act);
    }

    public override bool GeneralGCD(out IAction? act)
    {
        if (HasLockoutStatus || Player.HasStatus(true, StatusID.Reassembled))
        {
            return base.GeneralGCD(out act);
        }

        if (InCombat && AetherialGainPvE.CanUse(out act))
        {
            return true;
        }

        if (!InCombat && HastyMiragePvE.CanUse(out act))
        {
            return true;
        }

        if (InCombat && OccultDispelPvE.CanUse(out act))
        {
            return true;
        }

        if (DeadlyBlowPvE.CanUse(out act, skipComboCheck: true)) // Ideally we want to use this in burst windows, but 30 second cooldown means we can use it outside of burst windows too
        {
            if (BerserkerLevel == 2)
            {
                return true;
            }
            if (BerserkerLevel >= 3 && (!RagePvE.IsEnabled || Player.WillStatusEndGCD(1, 0, true, StatusID.PentupRage) || (RagePvE.Cooldown.IsCoolingDown && !Player.HasStatus(true, StatusID.PentupRage))))
            {
                return true;
            }
        }

        if (InCombat && OccultQuickPvE.CanUse(out act) && !Player.HasStatus(true, StatusID.Manafication) && !Player.HasStatus(true, StatusID.Embolden) && !Player.HasStatus(true, StatusID.MagickedSwordplay) && !Player.HasStatus(true, StatusID.GrandImpactReady))
        {
            return true;
        }

        if (ShouldHoldBurst())
        {
            return base.GeneralGCD(out act);
        }

        if (ZeninagePvE.CanUse(out act, skipComboCheck: true))
        {
            return true;
        }

        if (InCombat && PredictPvE.CanUse(out act))
        {
            return true;
        }

        if (SilverCannonPvE.CanUse(out act, skipStatusProvideCheck: true))
        {
            if (SilverCannonPvE.Target.Target?.WillStatusEnd(15, true, SilverCannonPvE.Setting.TargetStatusProvide ?? []) ?? false)
            {
                if (SilverCannonPvE.Target.Target?.WillStatusEnd(15, false, SilverCannonPvE.Setting.TargetStatusProvide ?? []) ?? false)
                {
                    return true;
                }
            }
        }

        if (HolyCannonPvE.CanUse(out act))
        {
            return true;
        }

        if (InCombat)
        {
            // logic if both cannons effects can be used on target
            if (ShockCannonPvE.CanUse(out _) && DarkCannonPvE.CanUse(out _))
            {
                if (DarkShockCannonUsage == DarkShockCannonStrategy.DarkCannon && DarkCannonPvE.CanUse(out act))
                {
                    return true;
                }
                if (DarkShockCannonUsage == DarkShockCannonStrategy.ShockCannon && ShockCannonPvE.CanUse(out act))
                {
                    return true;
                }
            }

            // logic for each cannon effect seperately
            if (DarkCannonPvE.CanUse(out act))
            {
                return true;
            }

            if (ShockCannonPvE.CanUse(out act))
            {
                return true;
            }

            // logic if neither cannons effects can be used on target
            if (CannoneerLevel < 4 || DarkShockCannonImmuneUsage == DarkShockCannonImmuneStrategy.DarkCannon)
            {
                DarkCannonPvE.Setting.TargetType = TargetType.HighHP; // Set the target type to HighHP so we can use it on targets that are immune to both blind and paralysis
                if (DarkCannonPvE.CanUse(out act))
                {
                    DarkCannonPvE.Setting.TargetType = TargetType.DarkCannon; // Reset the target type to DarkCannon for next time
                    return true;
                }
            }
            if (DarkShockCannonImmuneUsage == DarkShockCannonImmuneStrategy.ShockCannon)
            {
                ShockCannonPvE.Setting.TargetType = TargetType.HighHP; // Set the target type to HighHP so we can use it on targets that are immune to both blind and paralysis
                if (ShockCannonPvE.CanUse(out act))
                {
                    ShockCannonPvE.Setting.TargetType = TargetType.ShockCannon; // Reset the target type to ShockCannon for next time
                    return true;
                }
            }
        }

        if (PhantomFirePvE.CanUse(out act))
        {
            return true;
        }

        if (OccultCometPvE.CanUse(out act)) // Adding this to general swiftcast check is slightly more expensive for the many operations it will never be valid in
        {
            if (IsBLM && HasSwift)
            {
                return true;
            }
            if (IsRDM && HasSwift)
            {
                return true;
            }
            if (IsPLD && (Player.HasStatus(true, StatusID.Requiescat) || HasSwift))
            {
                return true;
            }
            if (!IsRDM && !IsPLD && !IsBLM)
            {
                return true;
            }
        }

        if (IainukiPvE.CanUse(out act))
        {
            return true;
        }

        if (InCombat && CounterstancePvE.CanUse(out act))
        {
            return true; // Even if you're not directly being attacked this second, it's 1 GCD / minute for 15% less damage taken
        }

        return base.GeneralGCD(out act);
    }

    private bool HandleOraclePrediction(IAction nextGCD, out IAction? act)
    {
        act = null;

        if (OracleLevel == 0)
        {
            return false; // Not an oracle, don't bother doing all of this
        }

        if (nextGCD == PredictPvE)
        {
            // New prediction used by RSR, clear the deck
            ResetDeck();
            return false;
        }

        _currentCard ??= PredictPvE;

        // Track the current card we're seeing and discard the previous card if it's changed
        if (HasBlessing)
        {
            if (_currentCard != BlessingPvE) //card expired, remove the old card
            {
                _remainingCards.Remove(_currentCard);
            }
            _currentCard = BlessingPvE;
        }
        else if (HasCleansing)
        {
            if (_currentCard != CleansingPvE) //card expired, remove the old card
            {
                _remainingCards.Remove(_currentCard);
            }
            _currentCard = CleansingPvE;
        }
        else if (HasPhantomJudgment)
        {
            if (_currentCard != PhantomJudgmentPvE) //card expired, remove the old card
            {
                _remainingCards.Remove(_currentCard);
            }
            _currentCard = PhantomJudgmentPvE;
        }
        else if (HasStarfall)
        {
            if (_currentCard != StarfallPvE) //card expired, remove the old card
            {
                _remainingCards.Remove(_currentCard);
            }
            _currentCard = StarfallPvE;
        }
        else
        {
            return false; // We have no active cards, so we don't need to do anything
        }

        // Desired Card Actions before we get into forced actions
        // Check starfall-invuln combo
        if (StarfallUseage && StarfallPvE.CanUse(out act))
        {
            if (ShouldHoldBurst() && !Player.WillStatusEnd(3, true, StatusID.PredictionOfStarfall) && !Player.WillStatusEnd(3, false, StatusID.PredictionOfStarfall)) // If we're holding burst, we don't want to use starfall yet, but only to a point
            {
                return false;
            }

            if (HasTankInvuln) // already invuln'd
            {
                return true;
            }
            if (Player.GetEffectiveHpPercent() > 90 && (!HasTankStance || Player.GetEffectiveHpPercent() > 120) && (!SaveInvulnForStarfall || OracleLevel < 6)// Not the tank or won't kill ourselves (directly) and either can't or won't be invuln'ing self for this
                && !MergedStatus.HasFlag(AutoStatus.DefenseArea)) // And not getting hit with a raidwide
            {
                return true;
            }
        }

        // Do the invuln part of the combo
        if (StarfallUseage && InCombat && HasStarfall && SaveInvulnForStarfall && InvulnerabilityPvE.CanUse(out act))
        {
            // If we have starfall and can use Invulnerability and we've opted to save invuln for this we're going to make sure we use it on ourselves
            if (InvulnerabilityPvE.Target.Target == Player)
            {
                return true;
            }
        }

        if (HasTankStance && (!SaveInvulnForStarfall || OracleLevel < 6)) // Tanking and either can't or won't invuln self
        {
            if (CleansingUseage && CleansingPvE.CanUse(out act)) // Cleansing is our highest potency option that doesn't rely on invulns
            {
                if (!ShouldHoldBurst() || Player.WillStatusEnd(3, true, StatusID.PredictionOfCleansing) || Player.WillStatusEnd(3, false, StatusID.PredictionOfCleansing))
                {
                    return true;
                }
            }
        }

        // Otherwise we need to go through our cards
        if (_remainingCards.Count == 1) // Last card! Play literally anything; if we screwed up starfall at least we go out with a bang
        {
            foreach (var card in _remainingCards)
            {
                return card.CanUse(out act); // If we can't use this for some reason, we're probably dead anyway
            }
        }

        if (_remainingCards.Count == 2) // We have a little time but need to think it through
        {
            bool hasStarfall = false;
            foreach (var card in _remainingCards)
            {
                if (card == StarfallPvE)
                {
                    hasStarfall = true;
                    break;
                }
            }

            if (hasStarfall) // We still have a starfall in the deck
            {
                if (_currentCard == StarfallPvE) // We've opted not to use it above, so either we're below health threshold, or are tanking and not invuln
                {
                    // Let's just wait it out, maybe we'll get enough health to use it. We've got a card left in case.
                    return false;
                }

                // Otherwise it must be the last card
                if (OracleLevel >= 6 && !InvulnerabilityPvE.Cooldown.IsCoolingDown && SaveInvulnForStarfall) // We can use invuln, so this is fine
                {
                    return false;
                }

                if (!InCombat //If we're not in combat and don't have starfall, use the current card
                    || (HasTankStance && Player.GetEffectiveHpPercent() < 120) // If we're tanking and got here we can't invuln and we don't seem safe enough to try this gimmick
                    || (Player.StatusTime(false, GetStatusForAction(_currentCard)) < 3f && Player.GetEffectiveHpPercent() <= 90)) // Or we don't have a lot of time and probably can't survive starfall
                {
                    // We must need to use our current card
                    if (_currentCard.CanUse(out act))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private void ResetDeck()
    {
        _remainingCards.Add(BlessingPvE);
        _remainingCards.Add(CleansingPvE);
        _remainingCards.Add(PhantomJudgmentPvE);
        _remainingCards.Add(StarfallPvE);
    }

    private StatusID GetStatusForAction(IBaseAction card)
    {
        if (card == BlessingPvE)
        {
            return StatusID.PredictionOfBlessing;
        }
        else if (card == CleansingPvE)
        {
            return StatusID.PredictionOfCleansing;
        }
        else if (card == PhantomJudgmentPvE)
        {
            return StatusID.PredictionOfJudgment;
        }
        else if (card == StarfallPvE)
        {
            return StatusID.PredictionOfStarfall;
        }

        return StatusID.None;
    }

    private bool ShouldHoldBurst()
    {
        if (!SaveForBurstWindow)
        {
            return false;
        }

        return !InBurstWindow();
    }
}