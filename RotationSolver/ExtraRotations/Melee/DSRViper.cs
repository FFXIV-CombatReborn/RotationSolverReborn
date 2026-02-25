namespace RotationSolver.ExtraRotations.Melee;

[Rotation("DSRViper by freddersly", CombatType.PvE, GameVersion = "7.31", Disabled = true)]
[SourceCode(Path = "main/ExtraRotations/Melee/DSRViper.cs")]
[ExtraRotation]
public sealed class DSRViper : ViperRotation
{
    #region Config Options

    [RotationConfig(CombatType.PvE, Name = "Use Uncoiled Fury for movement optimization")]
    public bool UFMovementOptimization { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Early Tincture timing (5s before Serpent's Ire)")]
    public bool EarlyTincture { get; set; } = true;

    [Range(10, 30, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Minimum buff time for safe Reawaken usage")]
    public int MinBuffTimeForReawaken { get; set; } = 15;

    [Range(1, 3, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Max Rattling Coils before forced UF")]
    public int MaxCoilsBeforeUF { get; set; } = 2;
    
    [RotationConfig(CombatType.PvE, Name = "Prioritize buff alternation over timers")]
    public bool AlternateBuffs { get; set; } = true;
    #endregion

    #region Tracking Variables
    private static bool LastUsedHunters { get; set; } = false;
    private static bool LastUsedSwiftskins { get; set; } = false;
    #endregion

    #region Status Display
    public override void DisplayRotationStatus()
    {
        ImGui.Text($"Buff Time Safe: {IsBuffTimeSafeForReawaken()}");
        ImGui.Text($"Ready to Reawaken: {CanUseReawaken()}");
        ImGui.Text($"Rattling Coils: {RattlingCoilStacks}");
        ImGui.Text($"Serpent Offerings: {SerpentOffering}");
        ImGui.Text($"Last Hunter's: {LastUsedHunters} | Last Swift's: {LastUsedSwiftskins}");
    }

    private bool IsBuffTimeSafeForReawaken()
    {
        return SwiftTime > MinBuffTimeForReawaken && HuntersTime > MinBuffTimeForReawaken;
    }
    
    private bool CanUseReawaken()
    {
        // More intelligent Reawaken usage
        return (HasReadyToReawaken || SerpentOffering >= 50) && 
               IsBuffTimeSafeForReawaken() &&
               RattlingCoilStacks <= 1 && // Don't waste coils
               !DreadActive && !PitActive && // Not mid-combo
               !HasPoisedFang && !HasPoisedBlood && // No Uncoiled follow-ups pending
               !HasHunterVenom && !HasSwiftVenom && // No Dread follow-ups pending
               !HasFellHuntersVenom && !HasFellSkinsVenom; // No AOE Dread follow-ups pending
    }
    
    private bool ShouldAlternateToHunters()
    {
        if (!AlternateBuffs) return false;
        
        // Force Hunter's if Swift was used last and we have both buffs
        if (LastUsedSwiftskins && HasHunterAndSwift) return true;
        
        // Use Hunter's if we don't have it
        if (!IsHunter && IsSwift) return true;
        
        return false;
    }
    
    private bool ShouldAlternateToSwiftskins()
    {
        if (!AlternateBuffs) return false;
        
        // Force Swiftskin's if Hunter's was used last and we have both buffs
        if (LastUsedHunters && HasHunterAndSwift) return true;
        
        // Use Swiftskin's if we don't have it
        if (!IsSwift && IsHunter) return true;
        
        return false;
    }
    #endregion

    #region oGCD Logic
    [RotationDesc]
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        // Priority 1: Uncoiled Fury follow-ups - Always highest priority
        if (HasPoisedFang && UncoiledTwinfangPvE.CanUse(out act))
            return true;
        if (HasPoisedBlood && UncoiledTwinbloodPvE.CanUse(out act))
            return true;

        // Priority 2: Reawaken Legacy abilities
        if (HasReawakenedActive)
        {
            if (FirstLegacyPvE.CanUse(out act)) return true;
            if (SecondLegacyPvE.CanUse(out act)) return true;
            if (ThirdLegacyPvE.CanUse(out act)) return true;
            if (FourthLegacyPvE.CanUse(out act)) return true;
        }

        // Priority 3: Dread follow-ups (Single Target) - Use immediately
        if (HasHunterVenom && TwinfangBitePvE.CanUse(out act))
            return true;
        if (HasSwiftVenom && TwinbloodBitePvE.CanUse(out act))
            return true;

        // Priority 4: Dread follow-ups (AOE) - Use immediately
        if (HasFellHuntersVenom && TwinfangThreshPvE.CanUse(out act, skipAoeCheck: true))
            return true;
        if (HasFellSkinsVenom && TwinbloodThreshPvE.CanUse(out act, skipAoeCheck: true))
            return true;

        // Priority 5: Serpent Tail abilities
        if (LastLashPvE.CanUse(out act, skipAoeCheck: true))
            return true;
        if (DeathRattlePvE.CanUse(out act))
            return true;

        // Priority 6: Early Tincture timing
        if (EarlyTincture && NoAbilityReady && SerpentsIrePvE.EnoughLevel && 
            SerpentsIrePvE.Cooldown.ElapsedAfter(115) && SerpentsIrePvE.Cooldown.RecastTimeRemain <= 5 &&
            UseBurstMedicine(out act))
        {
            return true;
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    [RotationDesc]
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        // Use Serpent's Ire on cooldown for burst windows
        if (IsBurst && SerpentsIrePvE.CanUse(out act))
        {
            return true;
        }

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        // Priority 1: Always complete Reawaken combo when active
        if (OuroborosPvE.CanUse(out act)) return true;
        if (FourthGenerationPvE.CanUse(out act)) return true;
        if (ThirdGenerationPvE.CanUse(out act)) return true;
        if (SecondGenerationPvE.CanUse(out act)) return true;
        if (FirstGenerationPvE.CanUse(out act)) return true;

        // Priority 2: Use Reawaken intelligently
        if (CanUseReawaken())
        {
            if (ReawakenPvE.CanUse(out act, skipComboCheck: true))
                return true;
        }

        // Priority 3: Resource Management - Uncoiled Fury
        // Use at lower stacks to avoid overcapping
        if (RattlingCoilStacks >= MaxCoilsBeforeUF && NoAbilityReady)
        {
            if (UncoiledFuryPvE.CanUse(out act, usedUp: true))
                return true;
        }

        // Movement optimization with Uncoiled Fury
        if (UFMovementOptimization && !HasHostilesInRange && RattlingCoilStacks > 0 && NoAbilityReady)
        {
            if (UncoiledFuryPvE.CanUse(out act, usedUp: true))
                return true;
        }

        // Priority 4: Twinblade Combos (Vicewinder/Vicepit chains)
        
        // AOE Dread Combos - Alternate buffs properly
        if (PitActive)
        {
            if (HasHunterAndSwift)
            {
                // Critical timer checks first
                if (SwiftTime <= 3 && SwiftskinsDenPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true, skipAoeCheck: true))
                {
                    LastUsedSwiftskins = true;
                    LastUsedHunters = false;
                    return true;
                }
                if (HuntersTime <= 3 && HuntersDenPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true, skipAoeCheck: true))
                {
                    LastUsedHunters = true;
                    LastUsedSwiftskins = false;
                    return true;
                }
                
                // Alternation logic
                if (ShouldAlternateToHunters() && HuntersDenPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true, skipAoeCheck: true))
                {
                    LastUsedHunters = true;
                    LastUsedSwiftskins = false;
                    return true;
                }
                if (ShouldAlternateToSwiftskins() && SwiftskinsDenPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true, skipAoeCheck: true))
                {
                    LastUsedSwiftskins = true;
                    LastUsedHunters = false;
                    return true;
                }
            }
            else
            {
                // Use whichever buff we don't have
                if (!IsSwift && SwiftskinsDenPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true, skipAoeCheck: true))
                {
                    LastUsedSwiftskins = true;
                    LastUsedHunters = false;
                    return true;
                }
                if (!IsHunter && HuntersDenPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true, skipAoeCheck: true))
                {
                    LastUsedHunters = true;
                    LastUsedSwiftskins = false;
                    return true;
                }
            }
        }

        // Single Target Dread Combos - Alternate buffs properly
        if (DreadActive)
        {
            if (HasHunterAndSwift)
            {
                // Critical timer checks first
                if (SwiftTime <= 3 && SwiftskinsCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                {
                    LastUsedSwiftskins = true;
                    LastUsedHunters = false;
                    return true;
                }
                if (HuntersTime <= 3 && HuntersCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                {
                    LastUsedHunters = true;
                    LastUsedSwiftskins = false;
                    return true;
                }
                
                // Alternation logic with positional awareness
                if (ShouldAlternateToHunters())
                {
                    if (HuntersCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                    {
                        LastUsedHunters = true;
                        LastUsedSwiftskins = false;
                        return true;
                    }
                }
                if (ShouldAlternateToSwiftskins())
                {
                    if (SwiftskinsCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                    {
                        LastUsedSwiftskins = true;
                        LastUsedHunters = false;
                        return true;
                    }
                }
            }
            else
            {
                // Use whichever buff we don't have
                if (!IsSwift && SwiftskinsCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                {
                    LastUsedSwiftskins = true;
                    LastUsedHunters = false;
                    return true;
                }
                if (!IsHunter && HuntersCoilPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                {
                    LastUsedHunters = true;
                    LastUsedSwiftskins = false;
                    return true;
                }
            }
        }

        // Priority 5: Vicewinder/Vicepit usage - Maintain buffs, don't spam
        if (!DreadActive && !PitActive)
        {
            // Use Vicewinder/Vicepit to maintain buffs or when buffs are low
            bool needBuffRefresh = SwiftTime <= 10 || HuntersTime <= 10 || (!IsSwift && !IsHunter);
            
            if (needBuffRefresh || (VicewinderPvE.Cooldown.CurrentCharges >= 2) || (VicepitPvE.Cooldown.CurrentCharges >= 2))
            {
                if (VicewinderPvE.CanUse(out act, usedUp: true))
                    return true;
                if (VicepitPvE.CanUse(out act, usedUp: true))
                    return true;
            }
        }

        // Priority 6: Dual Wield Combo Finishers
        
        // AOE finishers - Use based on what venom we have
        if (HasGrimHunter && JaggedMawPvE.CanUse(out act, skipAoeCheck: true, skipStatusProvideCheck: true, skipComboCheck: true))
            return true;
        if (HasGrimSkin && BloodiedMawPvE.CanUse(out act, skipAoeCheck: true, skipStatusProvideCheck: true, skipComboCheck: true))
            return true;

        // Single Target finishers - Use based on positional buffs
        if (HasHindstung && HindstingStrikePvE.CanUse(out act, skipStatusProvideCheck: true))
            return true;
        if (HasHindsbane && HindsbaneFangPvE.CanUse(out act, skipStatusProvideCheck: true))
            return true;
        if (HasFlankstung && FlankstingStrikePvE.CanUse(out act, skipStatusProvideCheck: true))
            return true;
        if (HasFlanksbane && FlanksbaneFangPvE.CanUse(out act, skipStatusProvideCheck: true))
            return true;

        // Priority 7: Dual Wield Combo Second Hits

        // AOE second hits - Maintain buff alternation
        if (SwiftskinsBitePvE.EnoughLevel)
        {
            if (HasHunterAndSwift)
            {
                if (ShouldAlternateToHunters() && HuntersBitePvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                    return true;
                if (ShouldAlternateToSwiftskins() && SwiftskinsBitePvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                    return true;
                // Fallback to whichever is available
                if (HuntersBitePvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                    return true;
                if (SwiftskinsBitePvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                    return true;
            }
            else
            {
                if (!IsSwift && SwiftskinsBitePvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                    return true;
                if (!IsHunter && HuntersBitePvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                    return true;
            }
        }
        else
        {
            if (HuntersBitePvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                return true;
        }

        // Single Target second hits - Use based on positionals
        if (SwiftskinsStingPvE.EnoughLevel)
        {
            // Prioritize based on positional buffs
            if (HasHind && SwiftskinsStingPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                return true;
            if (HasFlank && HuntersStingPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                return true;
            
            // Fallback to buff maintenance
            if (!IsSwift && SwiftskinsStingPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                return true;
            if (!IsHunter && HuntersStingPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                return true;
        }
        else
        {
            if (HuntersStingPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true))
                return true;
        }

        // Priority 8: Combo Starters - Natural flow
        if (HasSteel)
        {
            if (SteelFangsPvE.CanUse(out act))
                return true;
            if (SteelMawPvE.CanUse(out act))
                return true;
        }
        if (HasReavers)
        {
            if (ReavingFangsPvE.CanUse(out act))
                return true;
            if (ReavingMawPvE.CanUse(out act))
                return true;
        }
        
        // Start new combo chain
        if (ReavingFangsPvE.CanUse(out act))
            return true;
        if (SteelFangsPvE.CanUse(out act))
            return true;
        if (ReavingMawPvE.CanUse(out act))
            return true;
        if (SteelMawPvE.CanUse(out act))
            return true;

        // Priority 9: Ranged options when out of melee range
        if (!HasHostilesInRange)
        {
            if (RattlingCoilStacks > 0 && UncoiledFuryPvE.CanUse(out act, usedUp: true))
                return true;
            if (WrithingSnapPvE.CanUse(out act))
                return true;
        }

        return base.GeneralGCD(out act);
    }
    #endregion
}
