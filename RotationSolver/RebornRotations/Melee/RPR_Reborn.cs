namespace RotationSolver.RebornRotations.Melee;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.58", RotationType = RotationType.Reborn)]
[SourceCode(Path = "main/RebornRotations/Melee/RPR_Reborn.cs")]

public sealed class RPR_Reborn : ReaperRotation
{
	#region Config Options
	public enum PoolingType
    {
	[Description("None")]
	None,

	[Description("Standard (mid-cycle window + cap)")]
	Standard,

	[Description("Only when another Enshroud stays possible for double enshroud burst")]
	Invasive,
    }

    [RotationConfig(CombatType.PvE, Name = "Pool Shroud for Arcane Circle")]
    public PoolingType Pooling { get; set; } = PoolingType.Invasive;

	[RotationConfig(CombatType.PvE, Name = "Use custom timing to refresh Death's Design")]
	public bool UseCustomDDTiming { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Refresh Death's Design with this many seconds remaining", Parent = nameof(UseCustomDDTiming))]
	public int RefreshDDSecondsRemaining { get; set; } = 10;

	[Range(1, 8, ConfigUnitType.Seconds, 0.5f)]
    [RotationConfig(CombatType.PvE, Name = "Enshroud when Arcane Circle is ready within this many seconds")]
    public float EnshroudArcaneCircleWindow { get; set; } = 1f;

	[RotationConfig(CombatType.PvE, Name = "If Perfectio can't fit in Arcane Circle, use as ranged uptime or before it expires)")]
    public bool HoldPerfectio { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Release Perfectio under other raid buffs", Parent = nameof(HoldPerfectio))]
    public bool PerfectioOnRaidBuffs { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "    > Release Perfectio while medicated", Parent = nameof(HoldPerfectio))]
    public bool PerfectioOnMedicated { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "    > Release Perfectio when the target is about to die", Parent = nameof(HoldPerfectio))]
    public bool PerfectioOnDying { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Refresh Death's Design shortly before Arcane Circle")]
    public bool RefreshDDBeforeArcaneCircle { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Prepare Harvest Moon (Soulsow) when out of combat or with no target")]
    public bool PrepareHarvestMoon { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use Enshroud when the boss is about to die")]
    public bool EnshroudOnBossDying { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Only use Harvest Moon on a target with Death's Design")]
    public bool HarvestMoonNeedsDD { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Wait up to 2 gcd for the ideal positional before Blood Stalk")]
    public bool WaitForPositional { get; set; } = false;

	public enum PotTiming : byte
    {
	[Description("Opener")] FirstArcaneCircle,
	[Description("2 Minutes")] FromSecondArcaneCircle,
    }

    [RotationConfig(CombatType.PvE, Name = "Use potion")]
    public bool UsePotion { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "First Potion timing", Parent = nameof(UsePotion))]
    public PotTiming PotionTiming { get; set; } = PotTiming.FromSecondArcaneCircle;

	public enum ArcaneCircleOpener
    {
	[Description("Balance")]
	Balance,

	[Description("0GCD")]
	ZeroGCD,
    }

    [RotationConfig(CombatType.PvE, Name = "Arcane Circle opener timing")]
    public ArcaneCircleOpener OpenerTiming { get; set; } = ArcaneCircleOpener.Balance;

	#endregion

	#region Countdown Logic
	protected override IAction? CountDownAction(float remainTime)
	{
		if (remainTime < HarpePvE.Info.CastTime + CountDownAhead
			&& HarpePvE.CanUse(out var act))
		{
			return act;
		}
		if (SoulsowPvE.CanUse(out act))
		{
			return act;
		}
		return base.CountDownAction(remainTime);
	}
	#endregion

	#region oGCD Logic
	[RotationDesc(ActionID.HellsIngressPvE)]
	protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
	{
		if (HellsIngressPvE.CanUse(out act))
		{
			return true;
		}
		return base.MoveForwardAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.HellsEgressPvE)]
	protected override bool MoveBackAbility(IAction nextGCD, out IAction? act)
	{
		if (HellsEgressPvE.CanUse(out act))
		{
			return true;
		}
		return base.MoveBackAbility(nextGCD, out act);
	}

	protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
	{
		if (NotInActiveCombo && SecondWindPvE.CanUse(out act))
		{
			return true;
		}

		if (NotInActiveCombo && BloodbathPvE.CanUse(out act))
		{
			return true;
		}

		return base.HealSingleAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.FeintPvE)]
	protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
	{
		if (NotInActiveCombo && FeintPvE.CanUse(out act))
		{
			return true;
		}

		return base.DefenseAreaAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.ArcaneCrestPvE)]
	protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
	{
		if (NotInActiveCombo && ArcaneCrestPvE.CanUse(out act))
		{
			return true;
		}

		return base.DefenseSingleAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.ArmsLengthPvE)]
	protected sealed override bool AntiKnockbackAbility(IAction nextGCD, out IAction? act)
	{
		if (NotInActiveCombo && ArmsLengthPvE.CanUse(out act))
		{
			return true;
		}

		return base.AntiKnockbackAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.LegSweepPvE)]
	protected sealed override bool InterruptAbility(IAction nextGCD, out IAction? act)
	{
		if (NotInActiveCombo && LegSweepPvE.CanUse(out act))
		{
			return true;
		}

		return base.InterruptAbility(nextGCD, out act);
	}

	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{   
		var GluttonySoon = GluttonyPvE.EnoughLevel && GluttonyPvE.Cooldown.WillHaveOneCharge(9);
		var SoulSoon = Soul >= 40 || SoulSlicePvE.Cooldown.WillHaveOneCharge(9);
		var IsTargetBoss = CurrentTarget?.IsBossFromTTK() ?? false;
		var IsTargetDying = CurrentTarget?.IsDying() ?? false;
		var InBurstWindow = IsBurst && (HasArcaneCircle || ArcaneCirclePvE.Cooldown.WillHaveOneCharge(EnshroudArcaneCircleWindow + 9f));
		var PoolingOn = Pooling != PoolingType.None;
        var BurstEnshroud = !ArcaneCirclePvE.EnoughLevel || HasArcaneCircle
        || (IsBurst && (ArcaneCirclePvE.Cooldown.WillHaveOneCharge(EnshroudArcaneCircleWindow) || !ArcaneCirclePvE.Cooldown.IsCoolingDown));
        var OffBurstEnshroud = !HasArcaneCircle && (Pooling == PoolingType.Standard
	    ? (ArcaneCirclePvE.Cooldown.WillHaveOneCharge(65) && !ArcaneCirclePvE.Cooldown.WillHaveOneCharge(50) && !GluttonySoon) || (Shroud >= 90 && !ArcaneCirclePvE.Cooldown.WillHaveOneCharge(22))
	    : (Shroud == 100 && !ArcaneCirclePvE.Cooldown.WillHaveOneCharge(EnshroudArcaneCircleWindow + 9f)) || (!ArcaneCirclePvE.Cooldown.WillHaveOneCharge(EnshroudArcaneCircleWindow + 15f) && ((Shroud >= 80 && GluttonySoon) || (Shroud >= 90 && SoulSoon))));
        var NoEnshroudPooling = !PoolingOn && Shroud >= 50 && (InBurstWindow || !GluttonySoon || Shroud >= 90);
        var YesEnshroudPooling = PoolingOn && Shroud >= 50 && (BurstEnshroud || OffBurstEnshroud);
		var HasIdealHostBool = HasIdealHost;
		var ArcaneGate = !PoolingOn || HasEnshrouded || Shroud < 50
	    || HasExecutioner || HasSoulReaver;
		
		

		if (IsBurst && ArcaneGate)
		{
			if (((CurrentTarget?.HasStatus(true, StatusID.DeathsDesign) ?? false) || OpenerTiming == ArcaneCircleOpener.ZeroGCD)
		    && !CombatElapsedLess(OpenerTiming == ArcaneCircleOpener.Balance ? 3.5f : 0f) && ArcaneCirclePvE.CanUse(out act, skipAoeCheck: true))
			{
				return true;
			}
		}

		if ((!HasExecutioner) && ((EnshroudOnBossDying && IsTargetBoss && IsTargetDying && !(PlentifulHarvestPvE.EnoughLevel && (HasImmortalSacrifice || HasBloodsownCircleSelf || (HasArcaneCircle && !HasIdealHost && IsLastGCD(true, CommunioPvE))))) || (!HasPerfectioParata && !(PlentifulHarvestPvE.EnoughLevel && (HasImmortalSacrifice || HasBloodsownCircleSelf || (HasArcaneCircle && !HasIdealHost && IsLastGCD(true, CommunioPvE)))) && (NoEnshroudPooling || YesEnshroudPooling || HasIdealHostBool))))
		{
			if (EnshroudPvE.CanUse(out act))
			{
				return true;
			}
		}

        var holdSacrificium = PoolingOn && LemureShroud > 4 && ArcaneCirclePvE.Cooldown.WillHaveOneCharge(2.5f);
		if (!holdSacrificium && SacrificiumPvE.CanUse(out act, skipAoeCheck: true, usedUp: true))
		{
			return true;
		}

		if (HasEnshrouded && (HasArcaneCircle || LemureShroud < 4))
		{
			if (LemuresScythePvE.CanUse(out act, usedUp: true))
			{
				return true;
			}

			if (LemuresSlicePvE.CanUse(out act, usedUp: true))
			{
				return true;
			}
		}

		if ((!PlentifulHarvestPvE.EnoughLevel || !HasImmortalSacrifice || HasBloodsownCircleSelf) && !(PoolingOn && HasArcaneCircle && IsLastGCD(true, CommunioPvE)) && (!HasPerfectioParata || !ShouldUsePerfectio()) && !HasIdealHost && Shroud < 90)
		{
			if (GluttonyPvE.CanUse(out act, skipAoeCheck: true))
			{
				return true;
			}
		}

		if (!HasBloodsownCircleSelf && (!HasPerfectioParata || !ShouldUsePerfectio()) && !HasExecutioner && !HasImmortalSacrifice && !HasIdealHost && Shroud < 100 && ((GluttonyPvE.EnoughLevel && !GluttonyPvE.Cooldown.WillHaveOneChargeGCD(4)) || !GluttonyPvE.EnoughLevel || Soul == 100 || (Shroud == 40 && ArcaneCircleSoon)))
		{
			if (GrimSwathePvE.CanUse(out act))
			{
				return true;
			}

			if ((!WaitForPositional || Soul >= 70 || AtIdealPositional) && BloodStalkPvE.CanUse(out act))
			{
				return true;
			}
		}

		return base.AttackAbility(nextGCD, out act);
	}

	protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
	var prePot = Pooling == PoolingType.Invasive && RefreshDDBeforeArcaneCircle;
    var potDue = UsePotion && (PotionTiming == PotTiming.FirstArcaneCircle || CombatTime > 30f);
    var potWindow = HasArcaneCircle
	    || (prePot && IsBurst && CurrentTarget != null && ArcaneCirclePvE.Cooldown.WillHaveOneCharge(3));

	if (potDue && InCombat && potWindow && UseBurstMedicine(out act))
        {
	    return true;
        }

	        return base.GeneralAbility(nextGCD, out act);
    }
	#endregion

	#region GCD Logic
	protected override bool GeneralGCD(out IAction? act)
	{
		if ((!InCombat || (PrepareHarvestMoon && !HasHostilesInMaxRange)) && SoulsowPvE.CanUse(out act))
        {
	    return true;
        }
		
		if ((IsLastComboAction(true, SlicePvE) || IsLastComboAction(true, WaxingSlicePvE)) && LiveComboTime < GCDTime(2) && NotInActiveCombo)
		{
			if (InfernalSlicePvE.CanUse(out act))
			{
				return true;
			}

			if (WaxingSlicePvE.CanUse(out act))
			{
				return true;
			}
		}

		if (EnhancedGluttonyTrait.EnoughLevel && (IsLastAction(true, GluttonyPvE, BloodStalkPvE) || HasExecutioner))
		{
			if (HasExecutioner)
			{
				if (ExecutionersGuillotinePvE.CanUse(out act))
				{
					return true;
				}

				switch ((HasEnhancedGallows, HasEnhancedGibbet))
				{
					case (true, true):
						if (ExecutionersGallowsPvE.CanUse(out act, skipComboCheck: true) && ExecutionersGallowsPvE.Target.Target != null && CanHitPositional(EnemyPositional.Rear, ExecutionersGallowsPvE.Target.Target))
						{
							return true;
						}

						if (ExecutionersGibbetPvE.CanUse(out act, skipComboCheck: true) && ExecutionersGibbetPvE.Target.Target != null && CanHitPositional(EnemyPositional.Flank, ExecutionersGibbetPvE.Target.Target))
						{
							return true;
						}

						if (ExecutionersGallowsPvE.CanUse(out act, skipComboCheck: true))
						{
							return true;
						}

						if (ExecutionersGibbetPvE.CanUse(out act, skipComboCheck: true))
						{
							return true;
						}

						break;
					case (true, false):
						if (ExecutionersGallowsPvE.CanUse(out act, skipComboCheck: true))
						{
							return true;
						}

						break;
					case (false, true):
						if (ExecutionersGibbetPvE.CanUse(out act, skipComboCheck: true))
						{
							return true;
						}

						break;
					case (false, false):
						if (ExecutionersGallowsPvE.CanUse(out act, skipComboCheck: true) && ExecutionersGallowsPvE.Target.Target != null && CanHitPositional(EnemyPositional.Rear, ExecutionersGallowsPvE.Target.Target))
						{
							return true;
						}

						if (ExecutionersGibbetPvE.CanUse(out act, skipComboCheck: true) && ExecutionersGibbetPvE.Target.Target != null && CanHitPositional(EnemyPositional.Flank, ExecutionersGibbetPvE.Target.Target))
						{
							return true;
						}

						if (ExecutionersGallowsPvE.CanUse(out act, skipComboCheck: true))
						{
							return true;
						}

						if (ExecutionersGibbetPvE.CanUse(out act, skipComboCheck: true))
						{
							return true;
						}

						break;
				}
			}
		}

		if (!HasExecutioner && !HasSoulReaver && ShouldUsePerfectio() && PerfectioPvE.CanUse(out act, skipAoeCheck: true, skipComboCheck: true))
    {
	return true;
    } 

		// If at least 2 targets in a best Whorl of Death AoE lack Death's Design, allow refreshing via AoE
		var ddNeeds = 0;
		if (WhorlOfDeathPvE.CanUse(out _, skipAoeCheck: true) && WhorlOfDeathPvE.PreviewTarget.HasValue)
		{
			ddNeeds = WhorlOfDeathPvE.PreviewTarget.Value.AffectedTargets?.Length ?? 0;
		}
		if (ddNeeds >= 2)
		{
			if (WhorlOfDeathPvE.CanUse(out act, skipStatusProvideCheck: true))
			{
				return true;
			}
		}

		if (WhorlOfDeathPvE.CanUse(out act))
		{
			return true;
		}
        
        if (RefreshDDBeforeArcaneCircle
	    && ArcaneCirclePvE.Cooldown.WillHaveOneCharge(4)
	    && (CurrentTarget?.WillStatusEnd(27, true, StatusID.DeathsDesign) ?? false)
	    && ShadowOfDeathPvE.CanUse(out act, skipStatusProvideCheck: true))
        {
	    return true;
        }

		if (UseCustomDDTiming && ((!CurrentTarget?.HasStatus(true, StatusID.DeathsDesign) ?? false) || (CurrentTarget?.WillStatusEnd(RefreshDDSecondsRemaining, true, StatusID.DeathsDesign) ?? false)))
		{
			if (ShadowOfDeathPvE.CanUse(out act, skipStatusProvideCheck: true))
			{
				return true;
			}
		}
		else
		{
			if (ShadowOfDeathPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (HasEnshrouded)
		{
			if (ShadowOfDeathPvE.CanUse(out act))
			{
				return true;
			}

			if (LemureShroud > 1)
			{
				if (PlentifulHarvestPvE.EnoughLevel && ArcaneCirclePvE.Cooldown.WillHaveOneCharge(9) &&
				   ((LemureShroud == 4 && (CurrentTarget?.WillStatusEnd(30, true, StatusID.DeathsDesign) ?? false)) || (LemureShroud == 3 && (CurrentTarget?.WillStatusEnd(50, true, StatusID.DeathsDesign) ?? false))))
				{
					if (ShadowOfDeathPvE.CanUse(out act, skipStatusProvideCheck: true))
					{
						return true;
					}
				}

				if (GrimReapingPvE.CanUse(out act))
				{
					return true;
				}

				switch ((HasEnhancedCrossReaping, HasEnhancedVoidReaping))
				{
					case (true, _):
						if (CrossReapingPvE.CanUse(out act))
						{
							return true;
						}

						break;
					case (_, true):
						if (VoidReapingPvE.CanUse(out act))
						{
							return true;
						}

						break;
					case (false, false):
						if (CrossReapingPvE.CanUse(out act))
						{
							return true;
						}

						if (VoidReapingPvE.CanUse(out act))
						{
							return true;
						}

						break;
				}
			}
			if (LemureShroud == 1)
			{
				if (CommunioPvE.EnoughLevel)
				{
					if (!IsMoving && CommunioPvE.CanUse(out act, skipAoeCheck: true))
					{
						return true;
					}
					else
					{
						if (ShadowOfDeathPvE.CanUse(out act, skipAoeCheck: IsMoving))
						{
							return true;
						}
					}
				}
				else
				{
					if (GrimReapingPvE.CanUse(out act))
					{
						return true;
					}

					switch ((HasEnhancedCrossReaping, HasEnhancedVoidReaping))
					{
						case (true, _):
							if (CrossReapingPvE.CanUse(out act))
							{
								return true;
							}

							break;
						case (_, true):
							if (VoidReapingPvE.CanUse(out act))
							{
								return true;
							}

							break;
						case (false, false):
							if (CrossReapingPvE.CanUse(out act))
							{
								return true;
							}

							if (VoidReapingPvE.CanUse(out act))
							{
								return true;
							}

							break;
					}
				}
			}
		}

		if (HasSoulReaver)
		{
			if (GuillotinePvE.CanUse(out act))
			{
				return true;
			}

			switch ((HasEnhancedGallows, HasEnhancedGibbet))
			{
				case (true, true):
					if (GallowsPvE.CanUse(out act, skipComboCheck: true) && GallowsPvE.Target.Target != null && CanHitPositional(EnemyPositional.Rear, GallowsPvE.Target.Target))
					{
						return true;
					}

					if (GibbetPvE.CanUse(out act, skipComboCheck: true) && GibbetPvE.Target.Target != null && CanHitPositional(EnemyPositional.Flank, GibbetPvE.Target.Target))
					{
						return true;
					}

					if (GallowsPvE.CanUse(out act, skipComboCheck: true))
					{
						return true;
					}

					if (GibbetPvE.CanUse(out act, skipComboCheck: true))
					{
						return true;
					}

					break;
				case (true, _):
					if (GallowsPvE.CanUse(out act, skipComboCheck: true))
					{
						return true;
					}

					break;
				case (_, true):
					if (GibbetPvE.CanUse(out act, skipComboCheck: true))
					{
						return true;
					}

					break;
				case (false, false):
					if (GallowsPvE.CanUse(out act, skipComboCheck: true) && GallowsPvE.Target.Target != null && CanHitPositional(EnemyPositional.Rear, GallowsPvE.Target.Target))
					{
						return true;
					}

					if (GibbetPvE.CanUse(out act, skipComboCheck: true) && GibbetPvE.Target.Target != null && CanHitPositional(EnemyPositional.Flank, GibbetPvE.Target.Target))
					{
						return true;
					}

					if (GallowsPvE.CanUse(out act, skipComboCheck: true))
					{
						return true;
					}

					if (GibbetPvE.CanUse(out act, skipComboCheck: true))
					{
						return true;
					}

					break;
			}
		}

		if (!CombatElapsedLessGCD(2) && PlentifulHarvestPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (SoulScythePvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		if (SoulSlicePvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		if (NightmareScythePvE.CanUse(out act))
		{
			return true;
		}

		if (SpinningScythePvE.CanUse(out act))
		{
			return true;
		}

		if (!HasExecutioner)
		{
			if (InfernalSlicePvE.CanUse(out act))
			{
				return true;
			}

			if (WaxingSlicePvE.CanUse(out act))
			{
				return true;
			}

			if (SlicePvE.CanUse(out act))
			{
				return true;
			}
		}

		if (InCombat && !HasSoulReaver && HarvestMoonPvE.CanUse(out act, skipAoeCheck: true) && (!HarvestMoonNeedsDD || (HarvestMoonPvE.Target.Target?.HasStatus(true, StatusID.DeathsDesign) ?? false)))
        {
	        return true;
        }

		if (HarpePvE.CanUse(out act))
		{
			return true;
		}

		return base.GeneralGCD(out act);
	}
	#endregion

    #region Helpers

	private static readonly HashSet<uint> burstStatusIds =
    [
	(uint)StatusID.Divination,
	(uint)StatusID.Brotherhood,
	(uint)StatusID.BattleLitany,
	(uint)StatusID.StarryMuse,
	(uint)StatusID.Embolden,
	(uint)StatusID.SearingLight,
	(uint)StatusID.BattleVoice,
	(uint)StatusID.TechnicalFinish,
	(uint)StatusID.RadiantFinale
    ];

    private static bool PartyHasStatus(Func<uint, bool> match)
    {
	if (PartyMembers == null)
	    {
		return false;
	    }

	foreach (var member in PartyMembers)
	    {
		if (member?.StatusList == null)
		    {
			continue;
		    }

		foreach (var status in member.StatusList)
		    {
			if (match(status.StatusId))
			    {
				return true;
			    }
		    }
	    }
	return false;
    }

	private bool AtIdealPositional
    {
	    get
	    {
		var t = CurrentTarget;
		if (t == null)
		    {
			return true;
	    	}

		if (HasEnhancedGallows)
		    {
			return CanHitPositional(EnemyPositional.Rear, t);
		    }

		if (HasEnhancedGibbet)
		    {
			return CanHitPositional(EnemyPositional.Flank, t);
		    }

		return CanHitPositional(EnemyPositional.Rear, t) || CanHitPositional(EnemyPositional.Flank, t);
	    }
    }

    private static bool IsPartyBurst => PartyHasStatus(id => burstStatusIds.Contains(id));

    private static bool IsPartyMedicated => PartyHasStatus(id => id == (uint)StatusID.Medicated);
	
	private bool ArcaneCircleSoon =>
	!HasArcaneCircle
	&& ArcaneCirclePvE.Cooldown.WillHaveOneCharge(EnshroudArcaneCircleWindow + 9f);

	private bool ShouldUsePerfectio()
    {
	if (!HoldPerfectio)
	    {
		return true;
	    }

	var target = CurrentTarget;

	if (HasArcaneCircle
		|| StatusHelper.PlayerWillStatusEndGCD(4, 0, true, StatusID.PerfectioParata)
		|| (PerfectioOnRaidBuffs && IsPartyBurst)
		|| (PerfectioOnMedicated && IsPartyMedicated)
		|| (PerfectioOnDying && (target?.IsDying() ?? false)))
	    {
		return true;
	    }

	return target != null && target.DistanceToPlayer() > 3;
    }

	#endregion

}
