namespace RebornRotations.Melee;

[Rotation("Default", CombatType.PvE, GameVersion = "7.2")]
[SourceCode(Path = "main/BasicRotations/Melee/VPR_Default.cs")]
[Api(4)]
public sealed class VPR_Default : ViperRotation
{
    #region Config Options

    [RotationConfig(CombatType.PvE, Name = "Use up all charges of Uncoiled Fury if you have used Tincture/Gemdraught (Overrides next option)")]
    public bool BurstUncoiledFury { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Allow Uncoiled Fury and Writhing Snap to overwrite oGCDs when at range")]
    public bool UFGhosting { get; set; } = true;

    [Range(1, 3, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "How many charges of Uncoiled Fury needs to be at before be used inside of melee (Ignores burst, leave at 3 to hold charges for out of melee uptime or burst only)")]
    public int MaxUncoiledStacksUser { get; set; } = 3;

    [Range(1, 30, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "How long on the status time for Swift needs to be to allow reawaken use (setting this too low can lead to dropping buff)")]
    public int SwiftTimer { get; set; } = 10;

    [Range(1, 30, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "How long on the status time for Hunt needs to be to allow reawaken use (setting this too low can lead to dropping buff)")]
    public int HuntersTimer { get; set; } = 10;

    [Range(0, 120, ConfigUnitType.None, 5)]
    [RotationConfig(CombatType.PvE, Name = "How long has to pass on Serpents Ire's cooldown before the rotation starts pooling gauge for burst. Leave this alone if you dont know what youre doing. (Will still use Reawaken if you reach cap regardless of timer)")]
    public int ReawakenDelayTimer { get; set; } = 75;

    [RotationConfig(CombatType.PvE, Name = "Experimental Pot Usage(used up to 5 seconds before SerpentsIre comes off cooldown)")]
    public bool BurstMed { get; set; } = false;

    #endregion

    private static bool IsInBurst => Player.Level > 50 && !Player.WillStatusEnd(0, true, StatusID.RagingStrikes);

    #region Additional oGCD Logic
    [RotationDesc]
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        // Uncoiled Fury Combo
        if (UncoiledTwinfangPvE.CanUse(out act)) return true;
        if (UncoiledTwinbloodPvE.CanUse(out act)) return true;

        //AOE Dread Combo
        if (TwinfangThreshPvE.CanUse(out act, skipAoeCheck: true)) return true;
        if (TwinbloodThreshPvE.CanUse(out act, skipAoeCheck: true)) return true;

        //Single Target Dread Combo
        if (TwinfangBitePvE.CanUse(out act)) return true;
        if (TwinbloodBitePvE.CanUse(out act)) return true;

        // Use burst medicine if cooldown for Technical Step has elapsed sufficiently
        if (SerpentCombo == SerpentCombo.None && BurstMed && SerpentsIrePvE.EnoughLevel && SerpentsIrePvE.Cooldown.ElapsedAfter(115)
            && UseBurstMedicine(out act)) return true;

        return base.EmergencyAbility(nextGCD, out act);
    }

    [RotationDesc]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (SlitherPvE.CanUse(out act)) return true;
        return base.AttackAbility(nextGCD, out act);
    }

    [RotationDesc]
    protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (SerpentCombo == SerpentCombo.None && SecondWindPvE.CanUse(out act)) return true;
        if (SerpentCombo == SerpentCombo.None && BloodbathPvE.CanUse(out act)) return true;
        return base.HealSingleAbility(nextGCD, out act);
    }

    [RotationDesc]
    protected sealed override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (SerpentCombo == SerpentCombo.None && FeintPvE.CanUse(out act)) return true;
        return base.DefenseAreaAbility(nextGCD, out act);
    }

    [RotationDesc]
    protected sealed override bool AntiKnockbackAbility(IAction nextGCD, out IAction? act)
    {
        if (SerpentCombo == SerpentCombo.None && ArmsLengthPvE.CanUse(out act)) return true;
        return base.AntiKnockbackAbility(nextGCD, out act);
    }

    [RotationDesc]
    protected sealed override bool InterruptAbility(IAction nextGCD, out IAction? act)
    {
        if (SerpentCombo == SerpentCombo.None && LegSweepPvE.CanUse(out act)) return true;
        return base.InterruptAbility(nextGCD, out act);
    }
    #endregion

    #region oGCD Logic

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        ////Reawaken Combo
        if (FirstLegacyPvE.CanUse(out act)) return true;
        if (SecondLegacyPvE.CanUse(out act)) return true;
        if (ThirdLegacyPvE.CanUse(out act)) return true;
        if (FourthLegacyPvE.CanUse(out act)) return true;
        if (SerpentsIrePvE.CanUse(out act)) return true;

        ////Serpent Combo oGCDs
        if (LastLashPvE.CanUse(out act)) return true;
        if (DeathRattlePvE.CanUse(out act)) return true;

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {

        ////Reawaken Combo
        if (OuroborosPvE.CanUse(out act)) return true;
        if (FourthGenerationPvE.CanUse(out act)) return true;
        if (ThirdGenerationPvE.CanUse(out act)) return true;
        if (SecondGenerationPvE.CanUse(out act)) return true;
        if (FirstGenerationPvE.CanUse(out act)) return true;

        if (SwiftTime > SwiftTimer &&
            HuntersTime > HuntersTimer &&
            !HasHunterVenom && !HasSwiftVenom &&
            !HasPoisedBlood && !HasPoisedFang && SerpentsIrePvE.EnoughLevel && (!SerpentsIrePvE.Cooldown.ElapsedAfter(ReawakenDelayTimer) || SerpentOffering == 100) ||
            SwiftTime > SwiftTimer &&
            HuntersTime > HuntersTimer &&
            !HasHunterVenom && !HasSwiftVenom &&
            !HasPoisedBlood && !HasPoisedFang && !SerpentsIrePvE.EnoughLevel)
        {
            if (ReawakenPvE.CanUse(out act, skipComboCheck: true)) return true;
        }

        // Uncoiled Fury Overcap protection
        var isTargetBoss = CurrentTarget?.IsBossFromTTK() ?? false;
        var isTargetDying = CurrentTarget?.IsDying() ?? false;
        if ((MaxRattling == RattlingCoilStacks || RattlingCoilStacks >= MaxUncoiledStacksUser || isTargetBoss && isTargetDying && RattlingCoilStacks > 0) && !Player.HasStatus(true, StatusID.ReadyToReawaken) && SerpentCombo == SerpentCombo.None)
        {
            if (UncoiledFuryPvE.CanUse(out act, usedUp: true)) return true;
        }

        if (BurstUncoiledFury && Player.HasStatus(true, StatusID.Medicated) && !Player.HasStatus(true, StatusID.ReadyToReawaken) && SerpentCombo == SerpentCombo.None)
        {
            if (UncoiledFuryPvE.CanUse(out act, usedUp: true)) return true;
        }

        //Uncoiled fury use
        if (SerpentsIrePvE.Cooldown.JustUsedAfter(30) && !Player.HasStatus(true, StatusID.ReadyToReawaken) && SerpentCombo == SerpentCombo.None)
        {
            if (UncoiledFuryPvE.CanUse(out act, usedUp: true)) return true;
        }

        ////AOE Dread Combo
        if (SwiftskinsDenPvE.CanUse(out act, skipComboCheck: true, skipAoeCheck: true)) return true;
        if (HuntersDenPvE.CanUse(out act, skipComboCheck: true, skipAoeCheck: true)) return true;

        if (VicepitPvE.Cooldown.CurrentCharges == 1 && VicepitPvE.Cooldown.RecastTimeRemainOneCharge < 10)
        {
            if (VicepitPvE.CanUse(out act, usedUp: true)) return true;
        }
        if (VicepitPvE.CanUse(out act, usedUp: true)) return true;

        ////Single Target Dread Combo
        // Try using Coil that player is in position for extra damage first
        if (HuntersCoilPvE.CanUse(out act, skipComboCheck: true) && HuntersCoilPvE.Target.Target != null && CanHitPositional(EnemyPositional.Flank, HuntersCoilPvE.Target.Target)) return true;
        if (SwiftskinsCoilPvE.CanUse(out act, skipComboCheck: true) && SwiftskinsCoilPvE.Target.Target != null && CanHitPositional(EnemyPositional.Rear, SwiftskinsCoilPvE.Target.Target)) return true;

        if (HuntersCoilPvE.CanUse(out act, skipComboCheck: true)) return true;
        if (SwiftskinsCoilPvE.CanUse(out act, skipComboCheck: true)) return true;


        if (VicewinderPvE.Cooldown.CurrentCharges == 1 && VicewinderPvE.Cooldown.RecastTimeRemainOneCharge < 10)
        {
            if (VicewinderPvE.CanUse(out act, usedUp: true)) return true;
        }
        if (VicewinderPvE.CanUse(out act, usedUp: true)) return true;
        //AOE Serpent Combo
        if (JaggedMawPvE.CanUse(out act, skipAoeCheck: true)) return true;
        if (BloodiedMawPvE.CanUse(out act, skipAoeCheck: true)) return true;

        if (HuntersBitePvE.CanUse(out act, skipAoeCheck: true)) return true;
        if (SwiftskinsBitePvE.CanUse(out act, skipAoeCheck: true)) return true;

        if (ReavingMawPvE.CanUse(out act)) return true;
        if (SteelMawPvE.CanUse(out act)) return true;

        //Single Target Serpent Combo
        if (FlankstingStrikePvE.CanUse(out act)) return true;
        if (FlanksbaneFangPvE.CanUse(out act)) return true;
        if (HindstingStrikePvE.CanUse(out act)) return true;
        if (HindsbaneFangPvE.CanUse(out act)) return true;

        if (HuntersStingPvE.CanUse(out act)) return true;
        if (SwiftskinsStingPvE.CanUse(out act)) return true;

        if (ReavingFangsPvE.CanUse(out act)) return true;
        if (SteelFangsPvE.CanUse(out act)) return true;

        //Ranged
        if ((UFGhosting || (!UFGhosting && SerpentCombo == SerpentCombo.None)) && UncoiledFuryPvE.CanUse(out act, usedUp: true)) return true;
        if ((UFGhosting || (!UFGhosting && SerpentCombo == SerpentCombo.None)) && WrithingSnapPvE.CanUse(out act)) return true;

        return base.GeneralGCD(out act);
    }
    #endregion
}
