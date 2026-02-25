namespace RotationSolver.RebornRotations.Magical;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.41")]
[SourceCode(Path = "main/RebornRotations/Magical/RDM_Reborn.cs")]

public sealed class RDM_Reborn : RedMageRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Use GCDs to heal. (Ignored if there are no healers alive in party)")]
    public bool GCDHeal { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Attempt to pool Black and White Mana for burst (Experimental)")]
    public bool Pooling { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Prevent healing during burst combos")]
    public bool PreventHeal { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Prevent raising during burst combos")]
    public bool PreventRaising { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Vercure for Dualcast when out of combat.")]
    public bool UseVercure { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Cast Reprise when moving with no instacast.")]
    public bool RangedSwordplay { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Only use Embolden if in Melee range.")]
    public bool AnyonesMeleeRule { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Displacement after Engagement (use at own risk).")]
    public bool SuicideByDumber { get; set; } = false;
    #endregion

    private static BaseAction VeraeroPvEStartUp { get; } = new BaseAction(ActionID.VeraeroPvE, false);

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (remainTime < VeraeroPvEStartUp.Info.CastTime + CountDownAhead)
        {
            if (VeraeroPvEStartUp.CanUse(out IAction? act))
            {
                return act;
            }
        }

        //Remove Swift
        if (HasDualcast && remainTime < 0f && !InCombat)
        {
            StatusHelper.StatusOff(StatusID.Dualcast);
        }

        if (HasAccelerate && remainTime < 0f)
        {
            StatusHelper.StatusOff(StatusID.Acceleration);
        }

        if (HasSwift && remainTime < 0f)
        {
            StatusHelper.StatusOff(StatusID.Swiftcast);
        }

        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    [RotationDesc(ActionID.CorpsacorpsPvE)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (CorpsacorpsPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        return base.MoveForwardAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.DisplacementPvE)]
    protected override bool MoveBackAbility(IAction nextGCD, out IAction? act)
    {
        if (DisplacementPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        return base.MoveBackAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.AddlePvE, ActionID.MagickBarrierPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (AddlePvE.CanUse(out act))
        {
            return true;
        }

        if (MagickBarrierPvE.CanUse(out act))
        {
            return true;
        }

        return base.DefenseAreaAbility(nextGCD, out act);
    }

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        bool AnyoneInMeleeRange = NumberOfHostilesInRangeOf(3) > 0;

		if (HasEmbolden || EmboldenPvE.Cooldown.HasOneCharge || EmboldenPvE.Cooldown.WillHaveOneCharge(5f) && !IsInMeleeCombo)
		{
			if (InCombat && HasHostilesInMaxRange && ManaficationPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (!AnyonesMeleeRule)
        {
            if (IsBurst && InCombat && HasHostilesInRange && EmboldenPvE.CanUse(out act))
            {
                return true;
            }
        }
        else if (AnyonesMeleeRule)
        {
            if (IsBurst && InCombat && AnyoneInMeleeRange && EmboldenPvE.CanUse(out act))
            {
                return true;
            }
        }

		return base.EmergencyAbility(nextGCD, out act);
	}

	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        bool Meleecheck = nextGCD.IsTheSameTo(true, ActionID.RipostePvE, ActionID.ZwerchhauPvE, ActionID.RedoublementPvE, ActionID.MoulinetPvE, ActionID.ReprisePvE);

        act = null;

        //Swiftcast usage
        if (HasEmbolden || (EmboldenPvE.EnoughLevel && !EmboldenPvE.Cooldown.WillHaveOneCharge(30)) || !EmboldenPvE.EnoughLevel)
        {
            if (!HasAccelerate && !HasDualcast && !Meleecheck)
            {
                if (!CanVerFire && !CanVerStone && IsLastGCD(false, VerthunderPvE, VerthunderIiiPvE, VeraeroPvE, VeraeroIiiPvE))
                {
                    if (SwiftcastPvE.CanUse(out act))
                    {
                        return true;
                    }
                }
                if (!CanVerStone && nextGCD.IsTheSameTo(false, VeraeroPvE, VeraeroIiiPvE))
                {
                    if (SwiftcastPvE.CanUse(out act))
                    {
                        return true;
                    }
                }
                if (!CanVerFire && nextGCD.IsTheSameTo(false, VerthunderPvE , VerthunderIiiPvE))
                {
                    if (SwiftcastPvE.CanUse(out act))
                    {
                        return true;
                    }
                }
            }
        }

        if (AccelerationPvE.EnoughLevel && !Meleecheck)
        {
            if (!CanMagickedSwordplay && !CanGrandImpact && !HasManafication && InCombat && HasHostilesInRange)
            {
                if (!EnhancedAccelerationTrait.EnoughLevel)
                {
                    if (HasEmbolden || !EmboldenPvE.EnoughLevel)
                    {
                        if (AccelerationPvE.CanUse(out act))
                        {
                            return true;
                        }
                    }
                }

                if (EnhancedAccelerationTrait.EnoughLevel && !EnhancedAccelerationIiTrait.EnoughLevel)
                {
                    if (AccelerationPvE.CanUse(out act, usedUp: HasEmbolden || !EmboldenPvE.EnoughLevel || AccelerationPvE.Cooldown.WillHaveXChargesGCD(2, 1)))
                    {
                        return true;
                    }
                }

                if (EnhancedAccelerationIiTrait.EnoughLevel)
                {
                    if (AccelerationPvE.CanUse(out act, usedUp: HasEmbolden || !EmboldenPvE.EnoughLevel || AccelerationPvE.Cooldown.WillHaveXChargesGCD(2, 1)))
                    {
                        return true;
                    }
                }
            }
        }

        if (FlechePvE.CanUse(out act))
        {
            return true;
        }

        if ((HasEmbolden || StatusHelper.PlayerWillStatusEndGCD(1, 0, true, StatusID.PrefulgenceReady)) && PrefulgencePvE.CanUse(out act))
        {
            return true;
        }

        if (ViceOfThornsPvE.CanUse(out act))
        {
            return true;
        }

        if (ContreSixtePvE.CanUse(out act))
        {
            return true;
        }

        if (SuicideByDumber && EngagementPvE.Cooldown.CurrentCharges == 1 && DisplacementPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        if (EngagementPvE.CanUse(out act, usedUp: HasEmbolden || !EmboldenPvE.EnoughLevel || EngagementPvE.Cooldown.WillHaveXChargesGCD(2, 1)))
        {
            return true;
        }

        if (!IsMoving && CorpsacorpsPvE.CanUse(out act, usedUp: HasEmbolden || !EmboldenPvE.EnoughLevel || CorpsacorpsPvE.Cooldown.WillHaveXChargesGCD(2, 1)))
        {
            return true;
        }

        return base.AttackAbility(nextGCD, out act);
    }

    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        if (HasEmbolden && InCombat && UseBurstMedicine(out act))
        {
            return true;
        }

        return base.GeneralAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    [RotationDesc(ActionID.VercurePvE)]
    protected override bool HealSingleGCD(out IAction? act)
    {
        if (PreventHeal)
        {
            if (HasManafication || HasEmbolden || ManaStacks == 3 || CanMagickedSwordplay || CanGrandImpact
                || ScorchPvE.CanUse(out _) || ResolutionPvE.CanUse(out _)
                || IsLastComboAction(ActionID.RipostePvE, ActionID.ZwerchhauPvE))
            {
                return base.HealSingleGCD(out act);
            }
        }

        if (VercurePvE.CanUse(out act, skipStatusProvideCheck: true))
        {
            return true;
        }

        return base.HealSingleGCD(out act);
    }

    [RotationDesc(ActionID.VerraisePvE)]
    protected override bool RaiseGCD(out IAction? act)
    {
        if (PreventRaising)
        {
            if (HasManafication || HasEmbolden || ManaStacks == 3 || CanMagickedSwordplay || CanGrandImpact 
                || ScorchPvE.CanUse(out _) || ResolutionPvE.CanUse(out _)
                || IsLastComboAction(ActionID.RipostePvE, ActionID.ZwerchhauPvE))
            {
                return base.RaiseGCD(out act);
            }
        }

        if (VerraisePvE.CanUse(out act))
        {
            return true;
        }

        return base.RaiseGCD(out act);
    }

    protected override bool GeneralGCD(out IAction? act)
    {
		if (ManaStacks == 3)
		{
			int diff = BlackMana - WhiteMana;
			int gap = Math.Abs(diff);

			bool forceBalance = HasEmbolden || gap >= 19;

			if (forceBalance)
			{
				// Balance first
				if (diff > 0 && VerholyPvE.CanUse(out act)) return true;  // Black leads -> add White
				if (diff < 0 && VerflarePvE.CanUse(out act)) return true; // White leads -> add Black
			}
			else
			{
				// Slight imbalance: proc-aware preference to avoid overwriting existing procs
				if (CanVerFire && VerholyPvE.CanUse(out act)) return true;
				if (CanVerStone && VerflarePvE.CanUse(out act)) return true;
			}

			// Fallbacks
			if (diff > 0 && VerholyPvE.CanUse(out act)) return true;
			if (diff < 0 && VerflarePvE.CanUse(out act)) return true;

			if (CanVerFire && !CanVerStone && VerholyPvE.CanUse(out act)) return true;
			if (CanVerStone && !CanVerFire && VerflarePvE.CanUse(out act)) return true;

			if (VerholyPvE.CanUse(out act)) return true;
			if (VerflarePvE.CanUse(out act)) return true;
		}

		if (CanInstantCast && !CanVerEither)
        {
            if (ScatterPvE.CanUse(out act))
            {
                return true;
            }
            if (WhiteMana < BlackMana)
            {
                if (VeraeroPvE.CanUse(out act) && BlackMana - WhiteMana != 6)
                {
                    return true;
                }
            }
            if (VerthunderPvE.CanUse(out act))
            {
                return true;
            }
        }

        // Hardcode Resolution & Scorch to avoid double melee without finishers
        if (IsLastGCD(ActionID.ScorchPvE))
        {
            if (ResolutionPvE.CanUse(out act, skipStatusProvideCheck: true))
            {
                return true;
            }
        }

        if (IsLastGCD(ActionID.VerholyPvE, ActionID.VerflarePvE))
        {
            if (ScorchPvE.CanUse(out act, skipStatusProvideCheck: true))
            {
                return true;
            }
        }

        //Melee AOE combo
        if (IsLastGCD(false, EnchantedMoulinetDeuxPvE) && EnchantedMoulinetTroisPvE.CanUse(out act))
        {
            return true;
        }

        if (IsLastGCD(false, EnchantedMoulinetPvE) && EnchantedMoulinetDeuxPvE.CanUse(out act))
        {
            return true;
        }

		if (EnchantedRedoublementPvE_45962.CanUse(out act))
		{
			return true;
		}

		if (EnchantedRedoublementPvE.CanUse(out act))
        {
            return true;
        }

		if (EnchantedZwerchhauPvE_45961.CanUse(out act))
		{
			return true;
		}

		if (EnchantedZwerchhauPvE.CanUse(out act))
        {
            return true;
        }

		bool EnoughMana = (!Pooling && EnoughManaComboNoPooling) || (Pooling && EnoughManaComboPooling);
		//Check if you can start melee combo
		if (EnoughMana)
		{
			if (EnchantedRipostePvE.Config.IsEnabled && !IsLastGCD(true, EnchantedRipostePvE_45960) && ((HasEmbolden && CanMagickedSwordplay) || StatusHelper.PlayerWillStatusEndGCD(4, 0, true, StatusID.MagickedSwordplay)) && EnchantedRipostePvE_45960.CanUse(out act))
			{
				return true;
			}

			if (!IsLastGCD(true, EnchantedMoulinetPvE) && EnchantedMoulinetPvE.CanUse(out act))
			{
				return true;
			}

			if (!IsLastGCD(true, EnchantedRipostePvE) && EnchantedRipostePvE.CanUse(out act))
			{
				return true;
			}
		}
		//Grand impact usage if not interrupting melee combo
		if (GrandImpactPvE.CanUse(out act, skipStatusProvideCheck: CanGrandImpact, skipCastingCheck: true))
        {
            return true;
        }

        if (ManaStacks == 3)
        {
            return base.GeneralGCD(out act);
        }

        //Reprise logic
        if (IsMoving && RangedSwordplay &&
            //Check to not use Reprise when player can do melee combo, to not break it
            ManaStacks == 0 && (BlackMana < 50 || WhiteMana < 50) &&
             //Check if dualcast active
             !HasDualcast &&
             //Bunch of checks if anything else can be used instead of Reprise
             !AccelerationPvE.CanUse(out _) &&
             !HasAccelerate &&
             !SwiftcastPvE.CanUse(out _) &&
             !HasSwift &&
             !GrandImpactPvE.CanUse(out _) &&
             !CanGrandImpact &&
             //If nothing else to use and player moving - fire reprise.
             EnchantedReprisePvE.CanUse(out act))
        {
            return true;
        }

        // Single Target
        if (VerstonePvE.EnoughLevel)
        {
            if (CanVerBoth)
            {
                switch (VerEndsFirst)
                {
                    case "VerFire":
                        if (VerfirePvE.CanUse(out act))
                            return true;
                        break;
                    case "VerStone":
                        if (VerstonePvE.CanUse(out act))
                            return true;
                        break;
                    case "Equal":
                        if (WhiteMana < BlackMana)
                        {
                            if (VerstonePvE.CanUse(out act))
                                return true;
                        }
                        if (WhiteMana >= BlackMana)
                        {
                            if (VerfirePvE.CanUse(out act))
                                return true;
                        }
                        break;
                }
            }
            if (!CanVerBoth)
            {
                if (VerfirePvE.CanUse(out act))
                {
                    return true;
                }

                if (VerstonePvE.CanUse(out act))
                {
                    return true;
                }
            }
        }
        if (!VerstonePvE.EnoughLevel && VerfirePvE.CanUse(out act))
        {
            return true;
        }

        if (!CanInstantCast && !CanVerEither)
        {
            if (WhiteMana < BlackMana)
            {
                if (VeraeroIiPvE.CanUse(out act))
                {
                    return true;
                }
            }
            if (WhiteMana >= BlackMana)
            {
                if (VerthunderIiPvE.CanUse(out act))
                {
                    return true;
                }
            }
            if (JoltPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (UseVercure && !InCombat && VercurePvE.CanUse(out act))
        {
            return true;
        }

        return base.GeneralGCD(out act);
    }
    #endregion

    public override bool CanHealSingleSpell
    {
        get
        {
            int aliveHealerCount = 0;
            IEnumerable<IBattleChara> healers = PartyMembers.GetJobCategory(JobRole.Healer);
            foreach (IBattleChara h in healers)
            {
                if (!h.IsDead)
                    aliveHealerCount++;
            }

            return base.CanHealSingleSpell && (GCDHeal || aliveHealerCount == 0);
        }
    }
}
