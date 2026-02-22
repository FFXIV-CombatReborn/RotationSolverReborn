namespace RotationSolver.RebornRotations.Ranged;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.41")]
[SourceCode(Path = "main/RebornRotations/Ranged/MCH_Reborn.cs")]

public sealed class MCH_Reborn : MachinistRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Use burst medicine in countdown (requires auto burst option on)")]
    private bool OpenerBurstMeds { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Bioblaster while moving")]
    private bool BioMove { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Only use Wildfire on Boss targets")]
    private bool WildfireBoss { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Restrict mitigations to not overlap")]
    private bool MitOverlap { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Use AirAnchor at 1 second remaining on countdown")]
	private bool AirAnchorCountdown { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Restrict Tactician to only be allowed to be used when there are multiple hostile targets")]
	private bool MultiTact { get; set; } = false;
	#endregion

	#region Countdown logic
	protected override IAction? CountDownAction(float remainTime)
    {
		if (AirAnchorCountdown && remainTime < 1f && AirAnchorPvE.EnoughLevel && AirAnchorPvE.CanUse(out IAction? act))
		{
			return act;
		}

		if (!AirAnchorCountdown && remainTime < 0.1f && AirAnchorPvE.EnoughLevel && AirAnchorPvE.CanUse(out act))
		{
			return act;
		}

		if (remainTime < 4.75f && ReassemblePvE.CanUse(out act))
        {
            return act;
        }

		if (AirAnchorCountdown && IsBurst && OpenerBurstMeds && remainTime <= 1.5f && UseBurstMedicine(out act))
		{
			return act;
		}

		if (!AirAnchorCountdown && IsBurst && OpenerBurstMeds && remainTime <= 1f && UseBurstMedicine(out act))
        {
            return act;
        }

		return base.CountDownAction(remainTime);
    }
    #endregion

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (InCombat)
        {
            UpdateQueenStep();
            UpdateFoundStepPair();
        }

        if (HyperchargePvE.EnoughLevel)
        {
            if (!WildfirePvE.EnoughLevel)
            {
                if (HyperchargePvE.CanUse(out act, skipTTKCheck: true))
                {
                    return true;
                }
            }
            if (!FullMetalFieldPvE.EnoughLevel && (HasWildfire || (WildfirePvE.Cooldown.IsCoolingDown && Battery == 100)))
            {
                if (HyperchargePvE.CanUse(out act, skipTTKCheck: true))
                {
                    return true;
                }
            }
            if (HasWildfire && IsLastAction(false, FullMetalFieldPvE))
            {
                if (HyperchargePvE.CanUse(out act, skipTTKCheck: true))
                {
                    return true;
                }
            }
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    #region oGCD Logic
    [RotationDesc(ActionID.TacticianPvE, ActionID.DismantlePvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (IsOverheated || HasWildfire || HasFullMetalMachinist)
        {
            return base.DefenseAreaAbility(nextGCD, out act);
        }

		if (!MultiTact || (MultiTact && NumberOfAllHostilesInMaxRange > 1))
		{
			if (TacticianPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (!MitOverlap || (MitOverlap && !StatusHelper.PlayerHasStatus(true, StatusID.Tactician_1951)))
        {
            if (DismantlePvE.CanUse(out act))
            {
                return true;
            }
        }

        return base.DefenseAreaAbility(nextGCD, out act);
    }

    // Logic for using attack abilities outside of GCD, focusing on burst windows and cooldown management.
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (FullMetalFieldPvE.EnoughLevel && HasFullMetalMachinist && IsLastAction(false, WildfirePvE))
        {
            return base.AttackAbility(nextGCD, out act);
        }

        // Reassemble Logic
        // Check next GCD action and conditions for Reassemble.
        bool isReassembleUsable =
            //Reassemble current # of charges and double proc protection
            ReassemblePvE.Cooldown.CurrentCharges > 0 && !HasReassembled &&
            (nextGCD.IsTheSameTo(true, [ChainSawPvE, ExcavatorPvE])
            || (!ChainSawPvE.EnoughLevel && nextGCD.IsTheSameTo(true, SpreadShotPvE) && ((IBaseAction)nextGCD).Target.AffectedTargets.Length >= (SpreadShotMasteryTrait.EnoughLevel ? 4 : 5))
            || nextGCD.IsTheSameTo(false, [AirAnchorPvE])
            || (!ChainSawPvE.EnoughLevel && nextGCD.IsTheSameTo(true, DrillPvE))
            || (!DrillPvE.EnoughLevel && nextGCD.IsTheSameTo(true, CleanShotPvE))
            || (!CleanShotPvE.EnoughLevel && nextGCD.IsTheSameTo(false, HotShotPvE)));
        // Attempt to use Reassemble if it's ready
        if (isReassembleUsable)
        {
            if (ReassemblePvE.CanUse(out act, usedUp: true))
            {
                return true;
            }
        }

        // Start Ricochet/Gauss cooldowns rolling if they are not already
        if (!RicochetPvE.Cooldown.IsCoolingDown)
        {
            if (CheckmatePvE.EnoughLevel && CheckmatePvE.CanUse(out act))
            {
                return true;
            }
            if (!CheckmatePvE.EnoughLevel && RicochetPvE.CanUse(out act))
            {
                return true;
            }
        }
        if (!GaussRoundPvE.Cooldown.IsCoolingDown)
        {
            if (DoubleCheckPvE.EnoughLevel && DoubleCheckPvE.CanUse(out act))
            {
                return true;
            }
            if (!DoubleCheckPvE.EnoughLevel && GaussRoundPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (IsBurst)
        {
            if (BarrelStabilizerPvE.CanUse(out act))
            {
                return true;
            }
        }

        bool LowLevelHyperCheck = !AutoCrossbowPvE.EnoughLevel && SpreadShotPvE.CanUse(out _);

        if (IsBurst)
        {
            if (FullMetalFieldPvE.EnoughLevel)
            {
                if (Heat >= 50 || HasHypercharged)
                {
                    if (WeaponRemain < (GCDTime(1) / 2) && nextGCD.IsTheSameTo(false, FullMetalFieldPvE))
                    {
                        if (WildfirePvE.CanUse(out act))
                        {
                            if ((WildfirePvE.Target.Target.IsBossFromIcon() && WildfireBoss) || !WildfireBoss)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            if (!FullMetalFieldPvE.EnoughLevel)
            {
                if ((Heat >= 50 || HasHypercharged) && ToolChargeSoon(out _) && !LowLevelHyperCheck)
                {
                    if (WeaponRemain < (GCDTime(1) / 2))
                    {
                        if (WildfirePvE.CanUse(out act))
                        {
                            if ((WildfirePvE.Target.Target.IsBossFromIcon() && WildfireBoss) || !WildfireBoss)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
        }

        if (UseQueen(out act, nextGCD))
        {
            return true;
        }

        // Use Hypercharge if wildfire will not be up in 30 seconds or if you hit 100 heat
        if (!LowLevelHyperCheck && !HasReassembled && (!WildfirePvE.Cooldown.WillHaveOneCharge(30) || (Heat == 100)))
        {
            if (!(LiveComboTime <= 9f && LiveComboTime > 0f) && ToolChargeSoon(out act))
            {
                return true;
            }
        }

        // Decide which oGCD to use based on which has more RecastTimeElapsed
        var whichToUse = RicochetPvE.EnoughLevel switch
        {
            true when RicochetPvE.Cooldown.RecastTimeElapsed > GaussRoundPvE.Cooldown.RecastTimeElapsed => "Ricochet",
            true when GaussRoundPvE.Cooldown.RecastTimeElapsed > RicochetPvE.Cooldown.RecastTimeElapsed => "GaussRound",
            true => "Ricochet", // Default to Ricochet if equal
            _ => "GaussRound"
        };

        if (!FullMetalFieldPvE.EnoughLevel || (FullMetalFieldPvE.EnoughLevel && !nextGCD.IsTheSameTo(false, FullMetalFieldPvE)))
        {
            switch (whichToUse)
            {
                case "Ricochet":
                    if (CheckmatePvE.EnoughLevel && CheckmatePvE.CanUse(out act, usedUp: IsBurst || IsOverheated))
                    {
                        return true;
                    }
                    if (!CheckmatePvE.EnoughLevel && RicochetPvE.CanUse(out act, usedUp: IsBurst || IsOverheated))
                    {
                        return true;
                    }
                    break;
                case "GaussRound":
                    if (DoubleCheckPvE.EnoughLevel && DoubleCheckPvE.CanUse(out act, usedUp: IsBurst || IsOverheated))
                    {
                        return true;
                    }
                    if (!DoubleCheckPvE.EnoughLevel && GaussRoundPvE.CanUse(out act, usedUp: IsBurst || IsOverheated))
                    {
                        return true;
                    }
                    break;
            }
        }

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        // ensure combo is not broken, okay to drop during overheat
        if (IsLastComboAction(true, SlugShotPvE) && LiveComboTime >= GCDTime(1) && LiveComboTime <= GCDTime(2) && !IsOverheated)
        {
            // 3
            if (HeatedCleanShotPvE.EnoughLevel && HeatedCleanShotPvE.CanUse(out act))
            {
                return true;
            }
            if (!HeatedCleanShotPvE.EnoughLevel && CleanShotPvE.CanUse(out act))
            {
                return true;
            }
        }

        // ensure combo is not broken, okay to drop during overheat
        if (IsLastComboAction(true, SplitShotPvE) && LiveComboTime >= GCDTime(1) && LiveComboTime <= GCDTime(2) && !IsOverheated)
        {
            // 2
            if (HeatedSlugShotPvE.EnoughLevel && HeatedSlugShotPvE.CanUse(out act))
            {
                return true;
            }
            if (!HeatedSlugShotPvE.Info.EnoughLevelAndQuest() && SlugShotPvE.CanUse(out act))
            {
                return true;
            }
        }

        // Overheated AOE
        if (AutoCrossbowPvE.CanUse(out act))
        {
            return true;
        }

        // Overheated ST
        if (BlazingShotPvE.EnoughLevel && BlazingShotPvE.CanUse(out act))
        {
            return true;
        }
        if (!BlazingShotPvE.EnoughLevel && HeatBlastPvE.CanUse(out act))
        {
            return true;
        }

        if (IsLastAction(false, HyperchargePvE) && HeatBlastPvE.EnoughLevel)
        {
            return base.GeneralGCD(out act);
        }

        // Drill AOE
        if ((BioMove || (!IsMoving && !BioMove)) && BioblasterPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        // ST Big GCDs
        if (HotShotMasteryTrait.EnoughLevel && AirAnchorPvE.CanUse(out act))
        {
            return true;
        }

        // for opener: only use the first charge of Drill after AirAnchor when there are two
        if (DrillPvE.CanUse(out act, usedUp: false))
        {
            return true;
        }

        if (!HotShotMasteryTrait.EnoughLevel && HotShotPvE.CanUse(out act))
        {
            return true;
        }

        // ChainSaw is always used after Drill
        if (ChainSawPvE.CanUse(out act))
        {
            return true;
        }

        // use combo finisher asap
        if (ExcavatorPvE.CanUse(out act))
        {
            return true;
        }

        if (!AirAnchorPvE.CanUse(out _) && !ChainSawPvE.CanUse(out _) && !ExcavatorPvE.CanUse(out _) && !HasExcavatorReady
            && !IsLastGCD(false, ChainSawPvE) && DrillPvE.Cooldown.CurrentCharges < 2 && (!WildfirePvE.Cooldown.IsCoolingDown || IsLastAction(false, WildfirePvE)))
        {
            if (FullMetalFieldPvE.CanUse(out act))
            {
				return true;
			}
        }

        if (DrillPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        if (StatusHelper.PlayerWillStatusEnd(3, true, StatusID.FullMetalMachinist))
        {
            if (FullMetalFieldPvE.CanUse(out act))
            {
				return true;
			}
        }

        if (StatusHelper.PlayerWillStatusEnd(3, true, StatusID.ExcavatorReady))
        {
            if (ExcavatorPvE.CanUse(out act))
            {
                return true;
            }
        }

        // 1 AOE
        if (!IsOverheated)
        {
            if (ScattergunPvE.EnoughLevel)
            {
                if (ScattergunPvE.CanUse(out act))
                {
                    return true;
                }
            }
            if (!ScattergunPvE.EnoughLevel)
            {
                if (SpreadShotPvE.CanUse(out act))
                {
                    return true;
                }
            }
        }

        // 3 ST
        if (HeatedCleanShotPvE.EnoughLevel && HeatedCleanShotPvE.CanUse(out act))
        {
            return true;
        }
        if (!HeatedCleanShotPvE.EnoughLevel && CleanShotPvE.CanUse(out act))
        {
            return true;
        }
        // 2 ST
        if (HeatedSlugShotPvE.EnoughLevel && HeatedSlugShotPvE.CanUse(out act))
        {
            return true;
        }
        if (!HeatedSlugShotPvE.Info.EnoughLevelAndQuest() && SlugShotPvE.CanUse(out act))
        {
            return true;
        }
        // 1 ST
        if (HeatedSplitShotPvE.EnoughLevel && HeatedSplitShotPvE.CanUse(out act))
        {
            return true;
        }
        if (!HeatedSplitShotPvE.Info.EnoughLevelAndQuest() && SplitShotPvE.CanUse(out act))
        {
            return true;
        }

        return base.GeneralGCD(out act);
    }
    #endregion

    #region Tracking Properties
    public override void DisplayRotationStatus()
    {
        ImGui.Text($"QueenStep: {_currentStep}");
        ImGui.Text($"Step Pair Found: {foundStepPair}");
    }
    #endregion

    // Logic for Hypercharge
    private bool ToolChargeSoon(out IAction? act)
    {
        float REST_TIME = 8f;
        if
            //Cannot AOE
            (!SpreadShotPvE.CanUse(out _)
            &&
            // AirAnchor Enough Level % AirAnchor 
            ((AirAnchorPvE.EnoughLevel && AirAnchorPvE.Cooldown.WillHaveOneCharge(REST_TIME))
            ||
            // HotShot Charge Detection
            (!AirAnchorPvE.EnoughLevel && HotShotPvE.EnoughLevel && HotShotPvE.Cooldown.WillHaveOneCharge(REST_TIME))
            ||
            // Drill Charge Detection
            (DrillPvE.EnoughLevel && DrillPvE.Cooldown.WillHaveXCharges(DrillPvE.Cooldown.MaxCharges, REST_TIME))
            ||
            // Chainsaw Charge Detection
            (ChainSawPvE.EnoughLevel && ChainSawPvE.Cooldown.WillHaveOneCharge(REST_TIME))))
        {
            act = null;
            return false;
        }
        else
        {
            return HyperchargePvE.CanUse(out act, skipTTKCheck: true);
        }
    }

    private readonly (byte from, byte to, int step)[] _stepPairs =
    [
        (0, 60, 0),
        (60, 90, 1),
        (90, 100, 2),
        (100, 50, 3),
        (50, 60, 4),
        (60, 100, 5),
        (100, 50, 6),
        (50, 70, 7),
        (70, 100, 8),
        (100, 50, 9),
        (50, 80, 10),
        (70, 100, 11),
        (100, 50, 12),
        (50, 60, 13)
    ];

    private int _currentStep = 0; // Track the current step
    private bool foundStepPair = false;

    /// <summary>
    /// Checks if the current battery transition matches the current step only.
    /// </summary>
    private void UpdateFoundStepPair()
    {
        // Only check the current step
        if (_currentStep < _stepPairs.Length)
        {
            var (from, to, _) = _stepPairs[_currentStep];
            foundStepPair = (LastSummonBatteryPower == from && Battery == to);
        }
        else
        {
            foundStepPair = false;
        }
    }

    private byte _lastTrackedSummonBatteryPower = 0;

    public void UpdateQueenStep()
    {
        // If LastSummonBatteryPower has changed since last check, advance the step
        if (_lastTrackedSummonBatteryPower != LastSummonBatteryPower)
        {
            _lastTrackedSummonBatteryPower = LastSummonBatteryPower;
            AdvanceStep();
        }
    }

    private void AdvanceStep()
    {
        _currentStep++;
    }
    private bool UseQueen(out IAction? act, IAction nextGCD)
    {
        act = null;
        if (!InCombat || IsRobotActive)
            return false;

        // Opener
        if (Battery == 60 && IsLastGCD(false, ExcavatorPvE) && CombatTime < 15)
        {
            if (AutomatonQueenPvE.EnoughLevel && AutomatonQueenPvE.CanUse(out act, skipTTKCheck: true))
            {
                return true;
            }

            if (!AutomatonQueenPvE.EnoughLevel && RookAutoturretPvE.CanUse(out act, skipTTKCheck: true))
            {
                return true;
            }
        }

        // Only allow battery usage if the current transition matches the expected step
        if (foundStepPair)
        {
            if (AutomatonQueenPvE.EnoughLevel && AutomatonQueenPvE.CanUse(out act, skipTTKCheck: true))
            {
                return true;
            }

            if (!AutomatonQueenPvE.EnoughLevel && RookAutoturretPvE.CanUse(out act, skipTTKCheck: true))
            {
                return true;
            }
        }

        // overcap protection
        if ((nextGCD.IsTheSameTo(false, CleanShotPvE, HeatedCleanShotPvE) && Battery > 90)
            || (nextGCD.IsTheSameTo(false, HotShotPvE, AirAnchorPvE, ChainSawPvE, ExcavatorPvE) && Battery > 80))
        {
            if (AutomatonQueenPvE.EnoughLevel && AutomatonQueenPvE.CanUse(out act, skipTTKCheck: true))
            {
                return true;
            }

            if (!AutomatonQueenPvE.EnoughLevel && RookAutoturretPvE.CanUse(out act, skipTTKCheck: true))
            {
                return true;
            }
        }
        return false;
    }
}