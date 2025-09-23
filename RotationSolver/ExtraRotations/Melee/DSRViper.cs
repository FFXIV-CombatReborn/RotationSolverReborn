namespace RotationSolver.RebornRotations.Melee;

[Rotation("Optimal", CombatType.PvE, GameVersion = "7.31")]
[SourceCode(Path = "main/RebornRotations/Melee/VPR_Optimal.cs")]

public sealed class VPR_Optimal : ViperRotation
{
    #region Config Options

    [RotationConfig(CombatType.PvE, Name = "Use Standard Double Reawaken burst (vs Immediate)")]
    public bool StandardDoubleReawaken { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Pre-pull Tincture")]
    public bool PrePullTincture { get; set; } = true;

    [Range(35, 45, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Minimum buff time for safe Reawaken usage")]
    public int MinBuffTimeForReawaken { get; set; } = 40;
    #endregion

    #region State Management
    private enum RotationPhase
    {
        Opener,
        FillerPhase,
        BurstPrep,
        StandardDoubleBurst,
        ImmediateDoubleBurst,
        PostBurst
    }

    private enum OpenerStep
    {
        Start,
        ReavingFangs,
        SwiftskinsStingDone,
        SerpentsIreUsed,
        VicewinderStarted,
        VicewinderComboDone,
        FirstReawakenDone,
        OpenerComplete
    }

    private RotationPhase currentPhase = RotationPhase.Opener;
    private OpenerStep openerStep = OpenerStep.Start;
    private bool burstGCDBufferUsed = false;
    private bool firstReawakenInBurst = false;
    private DateTime lastPhaseChange = DateTime.Now;
    private bool tinctureUsed = false;

    private RotationPhase GetCurrentPhase()
    {
        // Reset to opener on combat start
        if (!InCombat)
        {
            currentPhase = RotationPhase.Opener;
            openerStep = OpenerStep.Start;
            burstGCDBufferUsed = false;
            firstReawakenInBurst = false;
            tinctureUsed = false;
            return currentPhase;
        }

        // Opener phase
        if (currentPhase == RotationPhase.Opener && openerStep != OpenerStep.OpenerComplete)
        {
            return RotationPhase.Opener;
        }

        // Check for burst window conditions
        if (SerpentsIrePvE.EnoughLevel)
        {
            // Burst prep phase - within 10s of Ire being ready
            if (SerpentsIrePvE.Cooldown.RecastTimeRemain <= 10 && SerpentsIrePvE.Cooldown.RecastTimeRemain > 0)
            {
                if (currentPhase != RotationPhase.BurstPrep)
                {
                    currentPhase = RotationPhase.BurstPrep;
                    lastPhaseChange = DateTime.Now;
                }
                return RotationPhase.BurstPrep;
            }

            // Active burst phase - Ire just used or in process
            if (SerpentsIrePvE.Cooldown.JustUsedAfter(0) && !SerpentsIrePvE.Cooldown.JustUsedAfter(30))
            {
                if (HasReadyToReawaken || InReawakenCombo())
                {
                    if (StandardDoubleReawaken && !burstGCDBufferUsed)
                    {
                        currentPhase = RotationPhase.StandardDoubleBurst;
                    }
                    else
                    {
                        currentPhase = RotationPhase.ImmediateDoubleBurst;
                    }
                    return currentPhase;
                }
            }

            // Post burst - within 30s after Ire, cleaning up resources
            if (SerpentsIrePvE.Cooldown.JustUsedAfter(0) && SerpentsIrePvE.Cooldown.JustUsedAfter(30))
            {
                if (currentPhase != RotationPhase.PostBurst)
                {
                    currentPhase = RotationPhase.PostBurst;
                    burstGCDBufferUsed = false;
                    firstReawakenInBurst = false;
                }
                return RotationPhase.PostBurst;
            }
        }

        // Default to filler
        currentPhase = RotationPhase.FillerPhase;
        return RotationPhase.FillerPhase;
    }

    private bool InReawakenCombo()
    {
        return HasReawakenedActive || FirstGenerationPvE.CanUse(out _) || 
               SecondGenerationPvE.CanUse(out _) || ThirdGenerationPvE.CanUse(out _) || 
               FourthGenerationPvE.CanUse(out _) || OuroborosPvE.CanUse(out _);
    }

    private bool IsBuffTimeSafeForReawaken()
    {
        // More lenient in opener
        if (currentPhase == RotationPhase.Opener)
        {
            return SwiftTime > 10 && HuntersTime > 10;
        }
        return SwiftTime > MinBuffTimeForReawaken && HuntersTime > MinBuffTimeForReawaken;
    }

    public override void DisplayRotationStatus()
    {
        ImGui.Text($"Phase: {GetCurrentPhase()}");
        if (currentPhase == RotationPhase.Opener)
            ImGui.Text($"Opener Step: {openerStep}");
        ImGui.Text($"Buff Safe: {IsBuffTimeSafeForReawaken()}");
        ImGui.Text($"Should Pool: {ShouldPoolOfferingsForBurst()}");
        ImGui.Text($"Ready to Reawaken: {HasReadyToReawaken}");
    }

    private bool ShouldPoolOfferingsForBurst()
    {
        var phase = GetCurrentPhase();
        return phase == RotationPhase.BurstPrep && SerpentOffering < 100;
    }
    #endregion

    #region oGCD Logic
    [RotationDesc]
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        // Priority 1: Uncoiled Fury follow-ups
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

        // Priority 2: Reawaken Legacy abilities
        if (HasReawakenedActive)
        {
            if (FirstLegacyPvE.CanUse(out act)) return true;
            if (SecondLegacyPvE.CanUse(out act)) return true;
            if (ThirdLegacyPvE.CanUse(out act)) return true;
            if (FourthLegacyPvE.CanUse(out act)) return true;
        }

        // Priority 3: Dread follow-ups
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

        // Priority 4: AOE Dread follow-ups
        switch ((HasFellHuntersVenom, HasFellSkinsVenom))
        {
            case (true, _):
                if (TwinfangThreshPvE.CanUse(out act, skipAoeCheck: true))
                    return true;
                break;
            case (_, true):
                if (TwinbloodThreshPvE.CanUse(out act, skipAoeCheck: true))
                    return true;
                break;
            case (false, false):
                if (TimeSinceLastAction.TotalSeconds < 2)
                    break;
                if (TwinfangThreshPvE.CanUse(out act, skipAoeCheck: true))
                    return true;
                if (TwinbloodThreshPvE.CanUse(out act, skipAoeCheck: true))
                    return true;
                break;
        }

        // Priority 5: Serpent Tail abilities
        if (LastLashPvE.CanUse(out act, skipAoeCheck: true))
            return true;

        if (DeathRattlePvE.CanUse(out act))
            return true;

        // Priority 6: Pre-pull Tincture in opener
        if (PrePullTincture && !tinctureUsed && currentPhase == RotationPhase.Opener && 
            openerStep == OpenerStep.Start && UseBurstMedicine(out act))
        {
            tinctureUsed = true;
            return true;
        }

        // Priority 7: Later burst window tinctures
        if (!PrePullTincture && NoAbilityReady && SerpentsIrePvE.EnoughLevel && 
            SerpentsIrePvE.Cooldown.ElapsedAfter(115) && SerpentsIrePvE.Cooldown.RecastTimeRemain <= 5 &&
            UseBurstMedicine(out act))
        {
            return true;
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        var phase = GetCurrentPhase();
        
        // Serpent's Ire usage based on phase
        if (SerpentsIrePvE.CanUse(out act))
        {
            // FIXED: Opener uses Ire early, after 2nd GCD
            if (phase == RotationPhase.Opener && openerStep == OpenerStep.SwiftskinsStingDone)
            {
                openerStep = OpenerStep.SerpentsIreUsed;
                return true;
            }
            
            // Burst phases: Use on cooldown
            if (phase == RotationPhase.BurstPrep && IsBurst)
            {
                return true;
            }
        }

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        var phase = GetCurrentPhase();

        // Always prioritize Reawaken combo
        if (OuroborosPvE.CanUse(out act)) 
        {
            if (phase == RotationPhase.StandardDoubleBurst && !firstReawakenInBurst)
            {
                firstReawakenInBurst = true;
            }
            return true;
        }
        if (FourthGenerationPvE.CanUse(out act)) return true;
        if (ThirdGenerationPvE.CanUse(out act)) return true;
        if (SecondGenerationPvE.CanUse(out act)) return true;
        if (FirstGenerationPvE.CanUse(out act)) return true;

        // Phase-specific logic
        switch (phase)
        {
            case RotationPhase.Opener:
                return HandleOpenerLogic(out act);
                
            case RotationPhase.BurstPrep:
                return HandleBurstPrepLogic(out act);
                
            case RotationPhase.StandardDoubleBurst:
                return HandleStandardDoubleBurstLogic(out act);
                
            case RotationPhase.ImmediateDoubleBurst:
                return HandleImmediateDoubleBurstLogic(out act);
                
            case RotationPhase.PostBurst:
                return HandlePostBurstLogic(out act);
                
            case RotationPhase.FillerPhase:
            default:
                return HandleFillerLogic(out act);
        }
    }

    private bool HandleOpenerLogic(out IAction? act)
    {
        switch (openerStep)
        {
            case OpenerStep.Start:
                // First GCD: Reaving Fangs
                if (ReavingFangsPvE.CanUse(out act))
                {
                    openerStep = OpenerStep.ReavingFangs;
                    return true;
                }
                break;

            case OpenerStep.ReavingFangs:
                // Second GCD: Swiftskin's Sting
                if (SwiftskinsStingPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                {
                    openerStep = OpenerStep.SwiftskinsStingDone;
                    return true;
                }
                break;

            case OpenerStep.SwiftskinsStingDone:
                // Serpent's Ire gets weaved here (handled in AttackAbility)
                // Wait for it to be used before continuing
                if (openerStep == OpenerStep.SerpentsIreUsed)
                {
                    break;
                }
                // If Ire hasn't been used yet, hold
                act = null;
                return false;

            case OpenerStep.SerpentsIreUsed:
                // Third GCD onwards: Vicewinder combo AFTER Ire
                if (VicewinderPvE.CanUse(out act))
                {
                    openerStep = OpenerStep.VicewinderStarted;
                    return true;
                }
                break;

            case OpenerStep.VicewinderStarted:
                // Continue Vicewinder combo
                if (HuntersCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                {
                    return true;
                }
                if (SwiftskinsCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                {
                    openerStep = OpenerStep.VicewinderComboDone;
                    return true;
                }
                break;

            case OpenerStep.VicewinderComboDone:
                // Uncoiled Fury if available before Reawaken
                if (RattlingCoilStacks > 0 && NoAbilityReady && 
                    UncoiledFuryPvE.CanUse(out act, usedUp: true))
                {
                    return true;
                }
                
                // Use Reawaken
                if (HasReadyToReawaken && ReawakenPvE.CanUse(out act, skipComboCheck: true))
                {
                    openerStep = OpenerStep.FirstReawakenDone;
                    return true;
                }
                
                // If not ready for Reawaken yet, continue with normal rotation
                break;

            case OpenerStep.FirstReawakenDone:
                // Opener complete after first Reawaken sequence finishes
                if (!InReawakenCombo())
                {
                    openerStep = OpenerStep.OpenerComplete;
                    currentPhase = RotationPhase.FillerPhase;
                }
                break;
        }

        // Fallback to standard logic if opener steps don't match
        return HandleStandardLogic(out act);
    }

    private bool HandleBurstPrepLogic(out IAction? act)
    {
        // Only use dual wield combos in prep phase to avoid breaking flow
        // Spend excess resources before burst
        if (RattlingCoilStacks == 3 && NoAbilityReady)
        {
            if (UncoiledFuryPvE.CanUse(out act, usedUp: true))
                return true;
        }

        // Use dual wield combos only
        return HandleDualWieldComboLogic(out act);
    }

    private bool HandleStandardDoubleBurstLogic(out IAction? act)
    {
        // Standard: Use one GCD between Ire and first Reawaken
        if (HasReadyToReawaken && !burstGCDBufferUsed)
        {
            // Use one dual wield GCD as buffer
            if (HandleDualWieldComboLogic(out act))
            {
                burstGCDBufferUsed = true;
                return true;
            }
        }

        // After buffer GCD, use first Reawaken
        if (HasReadyToReawaken && burstGCDBufferUsed && IsBuffTimeSafeForReawaken())
        {
            if (ReawakenPvE.CanUse(out act, skipComboCheck: true))
            {
                return true;
            }
        }

        // After first Reawaken completes, immediately use second
        if (!HasReadyToReawaken && !InReawakenCombo() && firstReawakenInBurst && 
            SerpentOffering >= 50 && IsBuffTimeSafeForReawaken())
        {
            if (ReawakenPvE.CanUse(out act, skipComboCheck: true))
            {
                return true;
            }
        }

        return HandleStandardLogic(out act);
    }

    private bool HandleImmediateDoubleBurstLogic(out IAction? act)
    {
        // Immediate: Use Reawaken right after Ire, no buffer
        if (HasReadyToReawaken && IsBuffTimeSafeForReawaken())
        {
            if (ReawakenPvE.CanUse(out act, skipComboCheck: true))
            {
                return true;
            }
        }

        // After first Reawaken completes, immediately use second
        if (!HasReadyToReawaken && !InReawakenCombo() && 
            SerpentOffering >= 50 && IsBuffTimeSafeForReawaken())
        {
            if (ReawakenPvE.CanUse(out act, skipComboCheck: true))
            {
                return true;
            }
        }

        return HandleStandardLogic(out act);
    }

    private bool HandlePostBurstLogic(out IAction? act)
    {
        // Aggressively spend Uncoiled Fury after burst
        if (RattlingCoilStacks > 1 && NoAbilityReady)
        {
            if (UncoiledFuryPvE.CanUse(out act, usedUp: true))
                return true;
        }

        // Continue with standard rotation
        return HandleStandardLogic(out act);
    }

    private bool HandleFillerLogic(out IAction? act)
    {
        // Don't pool offerings in filler unless approaching burst
        if (!ShouldPoolOfferingsForBurst())
        {
            // Use Reawaken at 50+ gauge if buffs are safe
            if (SerpentOffering >= 50 && IsBuffTimeSafeForReawaken() && 
                ReawakenPvE.CanUse(out act, skipComboCheck: true))
            {
                return true;
            }

            // Regular Uncoiled Fury usage
            if (RattlingCoilStacks == 3 && NoAbilityReady)
            {
                if (UncoiledFuryPvE.CanUse(out act, usedUp: true))
                    return true;
            }
        }

        return HandleStandardLogic(out act);
    }

    private bool HandleStandardLogic(out IAction? act)
    {
        // Standard rotation priority when not in specific phases

        // Dread combos
        if (DreadActive)
        {
            if (HasHunterAndSwift)
            {
                if (WillSwiftEnd && SwiftskinsCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                    return true;
                if (WillHunterEnd && HuntersCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                    return true;

                // Positional optimization
                if (HuntersCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true) && 
                    HuntersCoilPvE.Target.Target != null && CanHitPositional(EnemyPositional.Flank, HuntersCoilPvE.Target.Target))
                    return true;
                if (SwiftskinsCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true) && 
                    SwiftskinsCoilPvE.Target.Target != null && CanHitPositional(EnemyPositional.Rear, SwiftskinsCoilPvE.Target.Target))
                    return true;

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
                    default:
                        if (HuntersCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                            return true;
                        if (SwiftskinsCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                            return true;
                        break;
                }
            }
            else
            {
                if (!IsSwift && SwiftskinsCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                    return true;
                if (!IsHunter && HuntersCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                    return true;
            }
        }

        // Non-Dread Coil usage
        if (!DreadActive)
        {
            if (HuntersCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                return true;
            if (SwiftskinsCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                return true;
        }

        // Vicewinder charge management
        if (IsSwift && VicewinderPvE.Cooldown.CurrentCharges >= 1)
        {
            if (VicewinderPvE.CanUse(out act, usedUp: true))
                return true;
        }

        // AOE logic
        if (PitActive)
        {
            // AOE Dread
            if (HasHunterAndSwift)
            {
                if (WillSwiftEnd && SwiftskinsDenPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true, skipAoeCheck: true))
                    return true;
                if (WillHunterEnd && HuntersDenPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true, skipAoeCheck: true))
                    return true;
                
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
                    default:
                        if (HuntersDenPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true, skipAoeCheck: true))
                            return true;
                        if (SwiftskinsDenPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true, skipAoeCheck: true))
                            return true;
                        break;
                }
            }
            else
            {
                if (!IsSwift && SwiftskinsDenPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true, skipAoeCheck: true))
                    return true;
                if (!IsHunter && HuntersDenPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true, skipAoeCheck: true))
                    return true;
            }

            if (VicepitPvE.CanUse(out act, usedUp: true))
                return true;
        }

        // Dual wield combos
        return HandleDualWieldComboLogic(out act);
    }

    private bool HandleDualWieldComboLogic(out IAction? act)
    {
        // AOE finishers
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

        // ST finishers in rotation order
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
                // Standard rotation order: Flanksting -> Hindsting -> Flanksbane -> Hindsbane
                if (FlankstingStrikePvE.CanUse(out act, skipStatusProvideCheck: true))
                    return true;
                if (HindstingStrikePvE.CanUse(out act, skipStatusProvideCheck: true))
                    return true;
                if (FlanksbaneFangPvE.CanUse(out act, skipStatusProvideCheck: true))
                    return true;
                if (HindsbaneFangPvE.CanUse(out act, skipStatusProvideCheck: true))
                    return true;
                break;
        }

        // Second hits
        if (SwiftskinsStingPvE.EnoughLevel)
        {
            if (HasHunterAndSwift)
            {
                if (HasHind && SwiftskinsStingPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                    return true;
                if (HasFlank && HuntersStingPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                    return true;
                
                switch (HunterOrSwiftEndsFirst)
                {
                    case "Hunter":
                        if (HuntersStingPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                            return true;
                        break;
                    default:
                        if (SwiftskinsStingPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                            return true;
                        break;
                }
            }
            else
            {
                if (!IsSwift && SwiftskinsStingPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                    return true;
                if (!IsHunter && HuntersStingPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                    return true;
            }
        }
        else
        {
            if (HuntersStingPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                return true;
        }

        // AOE second hits  
        if (SwiftskinsBitePvE.EnoughLevel)
        {
            if (!IsSwift && SwiftskinsBitePvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                return true;
            if (!IsHunter && HuntersBitePvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                return true;
        }
        else
        {
            if (HuntersBitePvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                return true;
        }

        // Combo starters
        switch ((HasSteel, HasReavers))
        {
            case (true, _):
                if (SteelFangsPvE.CanUse(out act))
                    return true;
                if (SteelMawPvE.CanUse(out act))
                    return true;
                break;
            case (_, true):
                if (ReavingFangsPvE.CanUse(out act))
                    return true;
                if (ReavingMawPvE.CanUse(out act))
                    return true;
                break;
            case (false, false):
                if (ReavingFangsPvE.CanUse(out act))
                    return true;
                if (SteelFangsPvE.CanUse(out act))
                    return true;
                if (ReavingMawPvE.CanUse(out act))
                    return true;
                if (SteelMawPvE.CanUse(out act))
                    return true;
                break;
        }

        act = null;
        return false;
    }
    #endregion
}
