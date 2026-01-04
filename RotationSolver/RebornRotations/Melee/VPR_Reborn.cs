namespace RotationSolver.RebornRotations.Melee;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.4")]
[SourceCode(Path = "main/RebornRotations/Melee/VPR_Reborn.cs")]

public sealed class VPR_Reborn : ViperRotation
{
    #region Config Options

    [RotationConfig(CombatType.PvE, Name = "Hold one charge of Uncoiled Fury after burst for movement")]
    public bool BurstUncoiledFuryHold { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use up all charges of Uncoiled Fury if you have used Tincture/Gemdraught (Overrides next option)")]
    public bool MedicineUncoiledFury { get; set; } = true;

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

    [RotationConfig(CombatType.PvE, Name = "Restrict GCD use if Serpent's Tail, Twinblood, or Twinfang oGCDs can be used")]
    public bool AbilityPrio2 { get; set; } = true;
    #endregion

    #region Tracking Properties
    public override void DisplayRotationStatus()
    {
        ImGui.Text($"No Last Combo Action: {IsNoActionCombo()}");
    }
    #endregion

    #region Additional oGCD Logic
    [RotationDesc]
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        //Reawaken Combo
        if (HasReawakenedActive)
        {
            if (FourthLegacyPvE.CanUse(out act))
            {
                return true;
            }

            if (ThirdLegacyPvE.CanUse(out act))
            {
                return true;
            }

            if (SecondLegacyPvE.CanUse(out act))
            {
                return true;
            }

            if (FirstLegacyPvE.CanUse(out act))
            {
                return true;
            }
        }

        // Uncoiled Fury Combo
        switch ((HasPoisedFang, HasPoisedBlood))
        {
            case (true, _):
                if (UncoiledTwinfangPvE.CanUse(out act))
                    return true;
                break;
            case (_, true):
                if (UncoiledTwinbloodPvE.CanUse(out act))
                    return true;
                break;
            case (false, false):
                if (TimeSinceLastAction.TotalSeconds < 2)
                    break;
                if (UncoiledTwinfangPvE.CanUse(out act))
                    return true;
                if (UncoiledTwinbloodPvE.CanUse(out act))
                    return true;
                break;
        }

        // AOE Dread Combo
        switch ((HasFellHuntersVenom, HasFellSkinsVenom))
        {
            case (true, _):
                if (TwinfangThreshPvE.CanUse(out act))
                    return true;
                break;
            case (_, true):
                if (TwinbloodThreshPvE.CanUse(out act))
                    return true;
                break;
            case (false, false):
                if (TimeSinceLastAction.TotalSeconds < 2)
                    break;
                if (TwinfangThreshPvE.CanUse(out act))
                    return true;
                if (TwinbloodThreshPvE.CanUse(out act))
                    return true;
                break;
        }

        // Single Target Dread Combo
        switch ((HasHunterVenom, HasSwiftVenom))
        {
            case (true, _):
                if (TwinfangBitePvE.CanUse(out act))
                    return true;
                break;
            case (_, true):
                if (TwinbloodBitePvE.CanUse(out act))
                    return true;
                break;
            case (false, false):
                if (TimeSinceLastAction.TotalSeconds < 2)
                    break;
                if (TwinfangBitePvE.CanUse(out act))
                    return true;
                if (TwinbloodBitePvE.CanUse(out act))
                    return true;
                break;
        }

        ////Serpent Combo oGCDs
        if (LastLashPvE.CanUse(out act))
        {
            return true;
        }

        if (DeathRattlePvE.CanUse(out act))
        {
            return true;
        }

        // Use burst medicine if cooldown for Serpents Ire has elapsed sufficiently
        if (NoAbilityReady && BurstMed && SerpentsIrePvE.EnoughLevel && SerpentsIrePvE.Cooldown.ElapsedAfter(115)
            && UseBurstMedicine(out act))
        {
            return true;
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    [RotationDesc]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (SlitherPvE.CanUse(out act))
        {
            return true;
        }
        return base.MoveForwardAbility(nextGCD, out act);
    }

    [RotationDesc]
    protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (NoAbilityReady && SecondWindPvE.CanUse(out act))
        {
            return true;
        }

        if (NoAbilityReady && BloodbathPvE.CanUse(out act))
        {
            return true;
        }

        return base.HealSingleAbility(nextGCD, out act);
    }

    [RotationDesc]
    protected sealed override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (NoAbilityReady && FeintPvE.CanUse(out act))
        {
            return true;
        }

        return base.DefenseAreaAbility(nextGCD, out act);
    }

    [RotationDesc]
    protected sealed override bool AntiKnockbackAbility(IAction nextGCD, out IAction? act)
    {
        if (NoAbilityReady && ArmsLengthPvE.CanUse(out act))
        {
            return true;
        }

        return base.AntiKnockbackAbility(nextGCD, out act);
    }

    [RotationDesc]
    protected sealed override bool InterruptAbility(IAction nextGCD, out IAction? act)
    {
        if (NoAbilityReady && LegSweepPvE.CanUse(out act))
        {
            return true;
        }

        return base.InterruptAbility(nextGCD, out act);
    }
    #endregion

    #region oGCD Logic

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (IsBurst)
        {
            if (!SerpentsLineageTrait.EnoughLevel)
            {
                if (SerpentsIrePvE.CanUse(out act))
                {
                    return true;
                }
            }

            if (SerpentsLineageTrait.EnoughLevel && ReawakenPvE.IsEnabled)
            {
                if (SerpentsIrePvE.CanUse(out act))
                {
                    return true;
                }
            }
        }

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        if (AbilityPrio2 &&
            !NoAbilityReady)
        {
            if ((UFGhosting || (!UFGhosting && NoAbilityReady)) && !HasHostilesInRange && UncoiledFuryPvE.CanUse(out act, usedUp: true))
            {
                return true;
            }

            if ((UFGhosting || (!UFGhosting && NoAbilityReady)) && !HasHostilesInRange && WrithingSnapPvE.CanUse(out act))
            {
                return true;
            }

            return base.GeneralGCD(out act);
        }
            
        ////Reawaken Combo
        if (OuroborosPvE.CanUse(out act))
        {
            return true;
        }

        if (FourthGenerationPvE.CanUse(out act))
        {
            return true;
        }

        if (ThirdGenerationPvE.CanUse(out act))
        {
            return true;
        }

        if (SecondGenerationPvE.CanUse(out act))
        {
            return true;
        }

        if (FirstGenerationPvE.CanUse(out act))
        {
            return true;
        }

        // Check if player meets Serpents Ire requirements, then check buff timers.
        if (LiveComboTime > GCDTime(6) && ((SerpentsIrePvE.EnoughLevel && (!SerpentsIrePvE.Cooldown.ElapsedAfter(ReawakenDelayTimer) || SerpentOffering == 100)) || !SerpentsIrePvE.EnoughLevel)
            && SwiftTime > SwiftTimer && HuntersTime > HuntersTimer)
        {
            // If all above conditions are met, attempt to use Reawaken.
            if (IsBurst && ReawakenPvE.CanUse(out act, skipComboCheck: true))
            {
                return true;
            }
        }

        if (LiveComboTime > GCDTime(1) && !WillSwiftEnd && !WillHunterEnd)
        {
            // Uncoiled Fury Overcap protection
            bool isTargetBoss = CurrentTarget?.IsBossFromTTK() ?? false;
            bool isTargetDying = CurrentTarget?.IsDying() ?? false;
            if ((MaxRattling == RattlingCoilStacks || RattlingCoilStacks >= MaxUncoiledStacksUser || (isTargetBoss && isTargetDying && RattlingCoilStacks > 0)) && !HasReadyToReawaken && NoAbilityReady)
            {
                if (UncoiledFuryPvE.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }

            if (MedicineUncoiledFury && StatusHelper.PlayerHasStatus(true, StatusID.Medicated) && !HasReadyToReawaken && NoAbilityReady)
            {
                if (UncoiledFuryPvE.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }

            if ((RattlingCoilStacks > 1
                || !BurstUncoiledFuryHold)
                && SerpentsIrePvE.Cooldown.JustUsedAfter(30)
                && !HasReadyToReawaken
                && NoAbilityReady)
            {
                if (UncoiledFuryPvE.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }
        }

        ////AOE Dread Combo
        if (PitActive)
        {
            if (HasHunterAndSwift)
            {
                if (WillSwiftEnd)
                {
                    if (SwiftskinsDenPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true, skipAoeCheck: true))
                        return true;
                }

                if (WillHunterEnd)
                {
                    if (HuntersDenPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true, skipAoeCheck: true))
                        return true;
                }

                switch (HunterOrSwiftEndsFirst)
                {
                    case "Hunter":
                        if (HuntersDenPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true, skipAoeCheck: true))
                            return true;
                        break;
                    case "Swift":
                        if (SwiftskinsDenPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true, skipAoeCheck: true))
                            return true;
                        break;
                    case "Equal":
                    case null:
                    default:
                        if (HuntersDenPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true, skipAoeCheck: true))
                            return true;
                        if (SwiftskinsDenPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true, skipAoeCheck: true))
                            return true;
                        break;
                }
            }
            if (!HasHunterAndSwift)
            {
                if (!IsSwift)
                {
                    if (SwiftskinsDenPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true, skipAoeCheck: true))
                        return true;
                }

                if (!IsHunter)
                {
                    if (HuntersDenPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true, skipAoeCheck: true))
                        return true;
                }
            }
        }

        if (!PitActive)
        {
            if (SwiftskinsDenPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true, skipAoeCheck: true))
            {
                return true;
            }

            if (HuntersDenPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true, skipAoeCheck: true))
            {
                return true;
            }
        }

        if (LiveComboTime > GCDTime(3) && IsSwift && VicepitPvE.Cooldown.CurrentCharges > 0)
        {
            if (VicepitPvE.Cooldown.CurrentCharges == 1 && VicepitPvE.Cooldown.RecastTimeRemainOneCharge < 10)
            {
                if (VicepitPvE.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }
            if (VicepitPvE.CanUse(out act, usedUp: true))
            {
                return true;
            }
        }

        //AOE Serpent Combo
        // aoe 3
        switch ((HasGrimHunter, HasGrimSkin))
        {
            case (true, _):
                if (JaggedMawPvE.CanUse(out act, skipAoeCheck: true, skipStatusProvideCheck: true, skipComboCheck: true))
                    return true;
                break;
            case (_, true):
                if (BloodiedMawPvE.CanUse(out act, skipAoeCheck: true, skipStatusProvideCheck: true, skipComboCheck: true))
                    return true;
                break;
            case (false, false):
                if (JaggedMawPvE.CanUse(out act, skipAoeCheck: true, skipStatusProvideCheck: true, skipComboCheck: true))
                    return true;
                if (BloodiedMawPvE.CanUse(out act, skipAoeCheck: true, skipStatusProvideCheck: true, skipComboCheck: true))
                    return true;
                break;
        }

        // aoe 2
        if (SwiftskinsBitePvE.EnoughLevel)
        {
            if (HasHunterAndSwift)
            {
                switch (HunterOrSwiftEndsFirst)
                {
                    case "Hunter":
                        if (HuntersBitePvE.CanUse(out act, skipAoeCheck: true, skipStatusProvideCheck: true, skipComboCheck: true))
                            return true;
                        break;
                    case "Swift":
                        if (SwiftskinsBitePvE.CanUse(out act, skipAoeCheck: true, skipStatusProvideCheck: true, skipComboCheck: true))
                            return true;
                        break;
                    case "Equal":
                    case null:
                    default:
                        if (SwiftskinsBitePvE.CanUse(out act, skipAoeCheck: true, skipStatusProvideCheck: true, skipComboCheck: true))
                            return true;
                        break;
                }
            }

            if (!HasHunterAndSwift)
            {
                if (!IsHunter && !IsSwift)
                {
                    if (SwiftskinsBitePvE.CanUse(out act, skipAoeCheck: true, skipStatusProvideCheck: true, skipComboCheck: true))
                    {
                        return true;
                    }
                }

                if (!IsSwift)
                {
                    if (SwiftskinsBitePvE.CanUse(out act, skipAoeCheck: true, skipStatusProvideCheck: true, skipComboCheck: true))
                    {
                        return true;
                    }
                }

                if (!IsHunter)
                {
                    if (HuntersBitePvE.CanUse(out act, skipAoeCheck: true, skipStatusProvideCheck: true, skipComboCheck: true))
                        return true;
                }
            }
        }
        if (!SwiftskinsBitePvE.EnoughLevel)
        {
            if (HuntersBitePvE.CanUse(out act, skipAoeCheck: true, skipStatusProvideCheck: true, skipComboCheck: true))
                return true;
        }

        // aoe 1
        if (!FlankstingStrikePvE.EnoughLevel || IsNoActionCombo() || IsLastComboAction(ActionID.FlankstingStrikePvE, ActionID.FlanksbaneFangPvE, ActionID.HindstingStrikePvE, ActionID.HindsbaneFangPvE, ActionID.JaggedMawPvE, ActionID.BloodiedMawPvE))
        {
            switch ((HasSteel, HasReavers))
            {
                case (true, _):
                    if (SteelMawPvE.CanUse(out act))
                        return true;
                    break;
                case (_, true):
                    if (ReavingMawPvE.CanUse(out act))
                        return true;
                    break;
                case (false, false):
                    if (ReavingMawPvE.CanUse(out act))
                        return true;
                    if (SteelMawPvE.CanUse(out act))
                        return true;
                    break;
            }
        }

        ////Single Target Dread Combo
        // Try using Coil thats buff provided will end soon
        // then try using Coil that you can hit positional on
        // then try using Coil that will end first
        if (DreadActive)
        {
            if (HasHunterAndSwift)
            {
                if (WillSwiftEnd)
                {
                    if (SwiftskinsCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                        return true;
                }

                if (WillHunterEnd)
                {
                    if (HuntersCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                        return true;
                }

                if (HuntersCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true) && HuntersCoilPvE.Target.Target != null && CanHitPositional(EnemyPositional.Flank, HuntersCoilPvE.Target.Target))
                {
                    return true;
                }

                if (SwiftskinsCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true) && SwiftskinsCoilPvE.Target.Target != null && CanHitPositional(EnemyPositional.Rear, SwiftskinsCoilPvE.Target.Target))
                {
                    return true;
                }

                switch (HunterOrSwiftEndsFirst)
                {
                    case "Hunter":
                        if (HuntersCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                            return true;
                        break;
                    case "Swift":
                        if (SwiftskinsCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                            return true;
                        break;
                    case "Equal":
                    case null:
                    default:
                        if (HuntersCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                            return true;
                        if (SwiftskinsCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                            return true;
                        break;
                }
            }

            if (!HasHunterAndSwift)
            {
                if (!IsHunter && !IsSwift)
                {
                    if (HuntersCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true) && HuntersCoilPvE.Target.Target != null && CanHitPositional(EnemyPositional.Flank, HuntersCoilPvE.Target.Target))
                    {
                        return true;
                    }

                    if (SwiftskinsCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true) && SwiftskinsCoilPvE.Target.Target != null && CanHitPositional(EnemyPositional.Rear, SwiftskinsCoilPvE.Target.Target))
                    {
                        return true;
                    }

                    if (HuntersCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                    {
                        return true;
                    }

                    if (SwiftskinsCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                    {
                        return true;
                    }
                }

                if (!IsSwift)
                {
                    if (SwiftskinsCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                        return true;
                }

                if (!IsHunter)
                {
                    if (HuntersCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                        return true;
                }
            }
        }

        if (!DreadActive)
        {
            if (HuntersCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
            {
                return true;
            }

            if (SwiftskinsCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
            {
                return true;
            }
        }

        if (LiveComboTime > GCDTime(3) && IsSwift)
        {
            if (VicewinderPvE.Cooldown.CurrentCharges == 1 && VicewinderPvE.Cooldown.RecastTimeRemainOneCharge < 10)
            {
                if (VicewinderPvE.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }
            if (VicewinderPvE.CanUse(out act, usedUp: true))
            {
                return true;
            }
        }

        //Single Target Serpent Combo
        // st 3
        switch ((HasHindstung, HasHindsbane, HasFlankstung, HasFlanksbane))
        {
            case (true, _, _, _):
                if (HindstingStrikePvE.CanUse(out act, skipStatusProvideCheck: true))
                    return true;
                break;
            case (_, true, _, _):
                if (HindsbaneFangPvE.CanUse(out act, skipStatusProvideCheck: true))
                    return true;
                break;
            case (_, _, true, _):
                if (FlankstingStrikePvE.CanUse(out act, skipStatusProvideCheck: true))
                    return true;
                break;
            case (_, _, _, true):
                if (FlanksbaneFangPvE.CanUse(out act, skipStatusProvideCheck: true))
                    return true;
                break;
            case (false, false, false, false):
                if (HindstingStrikePvE.CanUse(out act, skipStatusProvideCheck: true))
                    return true;
                if (HindsbaneFangPvE.CanUse(out act, skipStatusProvideCheck: true))
                    return true;
                if (FlankstingStrikePvE.CanUse(out act, skipStatusProvideCheck: true))
                    return true;
                if (FlanksbaneFangPvE.CanUse(out act, skipStatusProvideCheck: true))
                    return true;
                break;
        }

        // st 2
        if (SwiftskinsStingPvE.EnoughLevel)
        {
            if (HasHunterAndSwift)
            {
                if (HasHind || HasFlank)
                {
                    if (HasHind)
                    {
                        if (SwiftskinsStingPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                            return true;
                    }

                    if (HasFlank)
                    {
                        if (HuntersStingPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                            return true;
                    }
                }

                if (!HasHind && !HasFlank)
                {
                    switch (HunterOrSwiftEndsFirst)
                    {
                        case "Hunter":
                            if (HuntersStingPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                                return true;
                            break;
                        case "Swift":
                            if (SwiftskinsStingPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                                return true;
                            break;
                        case "Equal":
                        case null:
                        default:
                            if (SwiftskinsStingPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                                return true;
                            break;
                    }
                }
            }

            if (!HasHunterAndSwift)
            {
                if (HasHind || HasFlank)
                {
                    if (HasHind)
                    {
                        if (SwiftskinsStingPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                            return true;
                    }

                    if (HasFlank)
                    {
                        if (HuntersStingPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                            return true;
                    }
                }

                if (!HasHind && !HasFlank)
                {
                    if (!IsHunter && !IsSwift)
                    {
                        if (SwiftskinsStingPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                        {
                            return true;
                        }
                    }

                    if (!IsSwift)
                    {
                        if (SwiftskinsStingPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                        {
                            return true;
                        }
                    }

                    if (!IsHunter)
                    {
                        if (HuntersStingPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                            return true;
                    }
                }
            }
        }
        if (!SwiftskinsStingPvE.EnoughLevel)
        {
            if (HuntersStingPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                return true;
        }

        // st 1
        if (!JaggedMawPvE.EnoughLevel || IsNoActionCombo() || IsLastComboAction(ActionID.FlankstingStrikePvE, ActionID.FlanksbaneFangPvE, ActionID.HindstingStrikePvE, ActionID.HindsbaneFangPvE, ActionID.JaggedMawPvE, ActionID.BloodiedMawPvE))
        {
            switch ((HasSteel, HasReavers))
            {
                case (true, _):
                    if (SteelFangsPvE.CanUse(out act))
                        return true;
                    break;
                case (_, true):
                    if (ReavingFangsPvE.CanUse(out act))
                        return true;
                    break;
                case (false, false):
                    if (ReavingFangsPvE.CanUse(out act))
                        return true;
                    if (SteelFangsPvE.CanUse(out act))
                        return true;
                    break;
            }
        }

        //Ranged
        if ((UFGhosting || (!UFGhosting && NoAbilityReady)) && UncoiledFuryPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        if ((UFGhosting || (!UFGhosting && NoAbilityReady)) && WrithingSnapPvE.CanUse(out act))
        {
            return true;
        }

        return base.GeneralGCD(out act);
    }
    #endregion
}
