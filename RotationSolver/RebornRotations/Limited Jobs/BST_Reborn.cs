namespace RotationSolver.RebornRotations.Melee;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.56")]
[SourceCode(Path = "main/RebornRotations/Limited Jobs/BST_Reborn.cs")]
public sealed class BST_Reborn : BeastmasterRotation
{
	[RotationConfig(CombatType.PvE, Name = "First Horn")]
	private HornOrder FirstHorn { get; set; } = HornOrder.FirstBattlehorn;

	[RotationConfig(CombatType.PvE, Name = "Second Horn")]
	private HornOrder SecondHorn { get; set; } = HornOrder.SecondBattlehorn;

	[RotationConfig(CombatType.PvE, Name = "Third Horn")]
	private HornOrder ThirdHorn { get; set; } = HornOrder.ThirdBattlehorn;

	[RotationConfig(CombatType.PvE, Name = "Ignore this")]
	public bool Overcap { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "What to use One with Nature on for the First Horn")]
	private OneWithNatureOrder FirstHornNature { get; set; } = OneWithNatureOrder.Neither;

	[RotationConfig(CombatType.PvE, Name = "What to use One with Nature on for the Second Horn")]
	private OneWithNatureOrder SecondHornNature { get; set; } = OneWithNatureOrder.Neither;

	[RotationConfig(CombatType.PvE, Name = "What to use One with Nature on for the Third Horn")]
	private OneWithNatureOrder ThirdHornNature { get; set; } = OneWithNatureOrder.Neither;

	#region Countdown logic
	// Defines logic for actions to take during the countdown before combat starts.
	protected override IAction? CountDownAction(float remainTime)
	{

		return base.CountDownAction(remainTime);
	}
	#endregion

	#region Emergency Logic
	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{

		return base.EmergencyAbility(nextGCD, out act);
	}
	#endregion

	#region oGCD Logic
	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		var rallyIncrease = 40 + (MasteredInstinct * 70);
		var resultingTP = TPCount + rallyIncrease;

		var shouldRally =
			(MasteredInstinct == 3 && InstinctualMasteryTrait.EnoughLevel && (HasMoonstalker || HasSunstrider)) ||
			(resultingTP >= 100 && resultingTP <= 250 && !InstinctualMasteryTrait.EnoughLevel);

		if (shouldRally)
		{
			if (RallyPvE.CanUse(out act))
			{
				return true;
			}
		}

		var rallycheerIncrease = 40 + (NaturalInstinct * 70);
		var resultingPetTP = TPCount + rallycheerIncrease;

		var shouldRallyCheer = resultingPetTP >= 100 && resultingPetTP <= 250;

		if (shouldRallyCheer)
		{
			if (RallyingCheerPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (BrutalRagePvE.CanUse(out act, usedUp: true))
		{
			return true;
		}
		if (HawkishTalonsPvE.CanUse(out act, usedUp: true))
		{
			return true;
		}
		if (RisenFallPvE.CanUse(out act, usedUp: true))
		{
			return true;
		}
		if (CalamityPvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		if (!Overcap)
		{
			if (TPCount >= 100)
			{
				if (TrickPvE.CanUse(out act, skipStatusNeed: true))
				{
					return true;
				}
			}

			if (AvalancheAxePvE.CanUse(out act, usedUp: true))
			{
				return true;
			}
			if (MistralAxePvE.CanUse(out act, usedUp: true))
			{
				return true;
			}
			if (SpinningAxePvE.CanUse(out act, usedUp: true))
			{
				return true;
			}
			if (GaleAxePvE.CanUse(out act, usedUp: true))
			{
				return true;
			}
		}

		if (Overcap)
		{
			if (NaturalInstinct == 3 || MasteredInstinct < 3)
			{
				if (TPCount >= 100)
				{
					if (TrickPvE.CanUse(out act, skipStatusNeed: true))
					{
						return true;
					}
				}

				if (AvalancheAxePvE.CanUse(out act, usedUp: true))
				{
					return true;
				}
				if (MistralAxePvE.CanUse(out act, usedUp: true))
				{
					return true;
				}
				if (SpinningAxePvE.CanUse(out act, usedUp: true))
				{
					return true;
				}
				if (GaleAxePvE.CanUse(out act, usedUp: true))
				{
					return true;
				}
			}

			if (MasteredInstinct == 3 || NaturalInstinct < 3)
			{
				if (PetTPCount >= 100)
				{
					if (GaleAxePvE.CanUse(out act, skipStatusNeed: true, usedUp: true))
					{
						return true;
					}

					if (SpinningAxePvE.CanUse(out act, skipStatusNeed: true, usedUp: true))
					{
						return true;
					}

					if (MistralAxePvE.CanUse(out act, skipStatusNeed: true, usedUp: true))
					{
						return true;
					}

					if (AvalancheAxePvE.CanUse(out act, skipStatusNeed: true, usedUp: true))
					{
						return true;
					}
				}

				if (TrickPvE.CanUse(out act))
				{
					return true;
				}
			}
		}

		if (ActiveBattlehorn == 1)
		{
			if (FirstHornNature == OneWithNatureOrder.Tempered)
			{
				if (TemperedReleasePvE_47092.CanUse(out act, skipStatusNeed: !TemperedReleaseMasteryTrait.EnoughLevel))
				{
					return true;
				}

				if (TemperedReleasePvE.CanUse(out act, skipStatusNeed: !TemperedReleaseMasteryTrait.EnoughLevel))
				{
					return true;
				}
			}

			if (FirstHornNature == OneWithNatureOrder.Borrow)
			{
				if (BorrowPvE.CanUse(out act, skipStatusNeed: !EnhancedBorrowTrait.EnoughLevel))
				{
					return true;
				}
			}
		}

		if (ActiveBattlehorn == 2)
		{
			if (SecondHornNature == OneWithNatureOrder.Tempered)
			{
				if (TemperedReleasePvE_47092.CanUse(out act, skipStatusNeed: !TemperedReleaseMasteryTrait.EnoughLevel))
				{
					return true;
				}

				if (TemperedReleasePvE.CanUse(out act, skipStatusNeed: !TemperedReleaseMasteryTrait.EnoughLevel))
				{
					return true;
				}
			}

			if (SecondHornNature == OneWithNatureOrder.Borrow)
			{
				if (BorrowPvE.CanUse(out act, skipStatusNeed: !EnhancedBorrowTrait.EnoughLevel))
				{
					return true;
				}
			}
		}

		if (ActiveBattlehorn == 3)
		{
			if (ThirdHornNature == OneWithNatureOrder.Tempered)
			{
				if (TemperedReleasePvE_47092.CanUse(out act, skipStatusNeed: !TemperedReleaseMasteryTrait.EnoughLevel))
				{
					return true;
				}

				if (TemperedReleasePvE.CanUse(out act, skipStatusNeed: !TemperedReleaseMasteryTrait.EnoughLevel))
				{
					return true;
				}
			}

			if (ThirdHornNature == OneWithNatureOrder.Borrow)
			{
				if (BorrowPvE.CanUse(out act, skipStatusNeed: !EnhancedBorrowTrait.EnoughLevel))
				{
					return true;
				}
			}
		}

		if (!BorrowReady && !TemperedReleaseReady)
		{
			if (AnyBattlehornReady)
			{
				if (PartingBlowPvE.CanUse(out act))
				{
					return true;
				}
			}
		}

		if (BrutalRagePvE.CanUse(out act, usedUp: true, skipStatusNeed: true))
		{
			return true;
		}
		if (HawkishTalonsPvE.CanUse(out act, usedUp: true, skipStatusNeed: true))
		{
			return true;
		}
		if (RisenFallPvE.CanUse(out act, usedUp: true, skipStatusNeed: true))
		{
			return true;
		}
		if (CalamityPvE.CanUse(out act, usedUp: true, skipStatusNeed: true))
		{
			return true;
		}

		if (ShieldChargePvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		if (SoulCrushPvE.CanUse(out act))
		{
			return true;
		}

		if (SeedsowerPvE.CanUse(out act))
		{
			return true;
		}

		return base.AttackAbility(nextGCD, out act);
	}

	protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
	{
		if (InCombat)
		{
			if (BeastskinPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (ActiveBattlehorn == 0)
		{
			act = null;

			if (FirstHorn switch
			{
				HornOrder.FirstBattlehorn => FirstBattlehornPvE.CanUse(out act),
				HornOrder.SecondBattlehorn => SecondBattlehornPvE.CanUse(out act),
				HornOrder.ThirdBattlehorn => ThirdBattlehornPvE.CanUse(out act),
				_ => false,
			})
			{
				return true;
			}

			if (SecondHorn switch
			{
				HornOrder.FirstBattlehorn => FirstBattlehornPvE.CanUse(out act),
				HornOrder.SecondBattlehorn => SecondBattlehornPvE.CanUse(out act),
				HornOrder.ThirdBattlehorn => ThirdBattlehornPvE.CanUse(out act),
				_ => false,
			})
			{
				return true;
			}

			if (ThirdHorn switch
			{
				HornOrder.FirstBattlehorn => FirstBattlehornPvE.CanUse(out act),
				HornOrder.SecondBattlehorn => SecondBattlehornPvE.CanUse(out act),
				HornOrder.ThirdBattlehorn => ThirdBattlehornPvE.CanUse(out act),
				_ => false,
			})
			{
				return true;
			}
		}

		return base.GeneralAbility(nextGCD, out act);
	}

	protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
	{
		if (InCombat)
		{
			if (ScaleskinPvE.CanUse(out act))
			{
				return true;
			}
		}

		return base.DefenseSingleAbility(nextGCD, out act);
	}

	protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
	{
		if (InCombat)
		{
			if (ScaleskinPvE.CanUse(out act))
			{
				return true;
			}
		}

		return base.DefenseAreaAbility(nextGCD, out act);
	}

	protected override bool DispelAbility(IAction nextGCD, out IAction? act)
	{
		if (ScouringAshPvE.CanUse(out act))
		{
			return true;
		}

		return base.DispelAbility(nextGCD, out act);
	}

	protected override bool AntiKnockbackAbility(IAction nextGCD, out IAction? act)
	{
		if (VileskinPvE.CanUse(out act))
		{
			return true;
		}

		return base.AntiKnockbackAbility(nextGCD, out act);
	}

	protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
	{

		return base.MoveForwardAbility(nextGCD, out act);
	}
	#endregion

	#region GCD Logic
	protected override bool MoveForwardGCD(out IAction? act)
	{

		return base.MoveForwardGCD(out act);
	}

	protected override bool GeneralGCD(out IAction? act)
	{
		if (QuellingWavePvE.CanUse(out act))
		{
			return true;
		}

		if (ShieldsplitterPvE.CanUse(out act))
		{
			return true;
		}
		if (AxebladeBitePvE.CanUse(out act))
		{
			return true;
		}
		if (SmashAxePvE.CanUse(out act))
		{
			return true;
		}

		if (QuellingWavePvE.CanUse(out act, skipTargetStatusNeedCheck: true))
		{
			return true;
		}
		return base.GeneralGCD(out act);
	}
	#endregion
}