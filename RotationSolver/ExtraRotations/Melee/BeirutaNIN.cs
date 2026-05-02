using System.ComponentModel;

namespace RotationSolver.ExtraRotations.Melee;

[Rotation("BeirutaNIN", CombatType.PvE, GameVersion = "7.45")]
[SourceCode(Path = "main/ExtraRotations/Melee/BeirutaNIN.cs")]
public sealed class BeirutaNIN : NinjaRotation
{
	#region Config

	[RotationConfig(CombatType.PvE, Name =
	"Please note that this rotation is optimised for most fights. However, V&C/OC GCDs may break mudra.\n" +
	"• Standard 4th GCD is the highest adps damage opener; other options trade personal damage for rdps\n" +
	"• Please start fights from flank, or True North may disrupt weaving\n" +
	"• Use rotation Settings AutoBurst False AT LEAST 5 seconds BEFORE Kassatsu ready to delay burst\n" +
	"• Use rotation Settings AutoBurst True to resume burst or toggle via macro\n" +
	"• You must stay close for Kunai's Bane before suiton buff expires\n" +
	"• Intercept Forked Raiju manually if desired\n" +
	"• Use ac Shukuchi gtoff macro for movement\n\n" +

	"Rotation behaviour (compared to Reborn):\n" +
	"• Uses layered ninjutsu priority (burst prep → disengage → damage)\n" +
	"• Adds early burst preparation (Suiton/Huton) based on burst action cooldown timing\n" +
	"• Includes distance-based disengage uptime (Raiton/Katon)\n" +
	"• Reserving mudra only for burst or uptime\n" +
	"• Customizable Opener and Potion Usage\n" +
	"• Burst delay optimization: only do fillers and prevent mudra cap when delaying. when renable burst (Huton/Suiton - Kassatsu - Dokumori - Kunai's Bane - TCJ)\n" +
	"• Uses explicit AoE target counting for actions around targets/self\n")]
	public bool RotationNotes { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use Raiton/Katon for uptime while disengaged (Moving one raiton/katon from 60s for uptime)")]
	public bool UseRaitonDisengageFallback { get; set; } = true;

	[Range(0, 20, ConfigUnitType.Yalms, 1)]
	[RotationConfig(CombatType.PvE, Name = "Minimum target distance for disengage fallback")]
	public float RaitonFallbackMinDistance { get; set; } = 3.0f;

	[RotationConfig(CombatType.PvE, Name = "Attempt to weave Kunai's Bane/Trick attack second half of GCD (Disable if you miss weaving)")]
	public bool RequireLateWeaveForBurstBuff { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Which Opener to Use")]
	[Range(0, 2, ConfigUnitType.None, 1)]
	public BurstTimingOption BurstTiming { get; set; } = BurstTimingOption.StandardFourthGcd;

	[Range(15f, 21f, ConfigUnitType.Seconds, 1f)]
	[RotationConfig(CombatType.PvE, Name = "Suiton/Huton Prep Window (seconds before Trick Attack / Kunai's Bane)")]
	public float BurstPrepThreshold { get; set; } = 21f;

	public enum BurstTimingOption : byte
	{
		[Description("Standard 4th GCD")] StandardFourthGcd,
		[Description("Standard 3rd GCD")] StandardThirdGcd,
		[Description("Alignment 4th GCD")] AlignmentFourthGcd,
	}

	private int RaidBuffOpenTiming => BurstTiming switch
	{
		BurstTimingOption.StandardFourthGcd => 4,
		BurstTimingOption.StandardThirdGcd => 4,
		BurstTimingOption.AlignmentFourthGcd => 6,
		_ => 4,
	};

	private int BurstBuffOpenTiming => BurstTiming switch
	{
		BurstTimingOption.StandardFourthGcd => 8,
		BurstTimingOption.StandardThirdGcd => 5,
		BurstTimingOption.AlignmentFourthGcd => 8,
		_ => 9,
	};

	[RotationConfig(CombatType.PvE, Name = "Enable Potion Usage")]
	private static bool PotionUsageEnabled { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Potion Usage Preset", Parent = nameof(PotionUsageEnabled))]
	private static NINPotionPreset PotionUsagePreset { get; set; } = NINPotionPreset.Standard0611;

	#endregion

	#region State

	private enum NINPotionPreset
	{
		[Description("0-6-11 (Dokumori)")] Standard0611,
		[Description("0-5-10 (Kunai's Bane)")] Standard0510,
	}

	private delegate bool ActionExecutor(out IAction? act);

	private sealed class StepDef
	{
		public Func<bool> When;
		public ActionExecutor Use;
		public StepDef(Func<bool> when, ActionExecutor use) { When = when; Use = use; }
	}

	private sealed class NinjutsuExecutionDef
	{
		public IBaseAction QueuedAction;
		public Func<bool> CurrentCheck;
		public ActionExecutor FinalCast;
		public StepDef[] Steps;
		public NinjutsuExecutionDef(IBaseAction queuedAction, Func<bool> currentCheck, ActionExecutor finalCast, params StepDef[] steps)
		{
			QueuedAction = queuedAction;
			CurrentCheck = currentCheck;
			FinalCast = finalCast;
			Steps = steps;
		}
	}

	private const int AoeThreshold = 3;

	private IBaseAction? _lastNinActionAim;
	private IBaseAction? _ninActionAim;

	private readonly ActionID NinjutsuPvEid = AdjustId(ActionID.NinjutsuPvE);

	private bool ShouldUseDisengageNinjutsuFallback =>
		UseRaitonDisengageFallback &&
		CurrentTarget != null &&
		CurrentTarget.DistanceToPlayer() > RaitonFallbackMinDistance;

	private bool ShouldSpendDamageMudraNow =>
		TenPvE.CanUse(out _, usedUp: true) &&
		TenPvE.Cooldown.WillHaveXChargesGCD(2, 2);

	private static bool IsCurrentNinjutsu(ActionID id) => AdjustId(ActionID.NinjutsuPvE) == id;
	private static bool NoActiveNinjutsu => IsCurrentNinjutsu(ActionID.NinjutsuPvE);
	private static bool RabbitMediumCurrent => IsCurrentNinjutsu(ActionID.RabbitMediumPvE);

	private bool HasMeisui => StatusHelper.PlayerHasStatus(true, StatusID.Meisui);

	private bool KassatsuExpiringSoon =>
		HasKassatsu &&
		StatusHelper.PlayerWillStatusEndGCD(2, 0, true, StatusID.Kassatsu);

	private float BurstActionRecastRemain =>
		KunaisBanePvE.EnoughLevel
			? KunaisBanePvE.Cooldown.RecastTimeRemain
			: TrickAttackPvE.Cooldown.RecastTimeRemain;

	private bool BurstActionIsCoolingDown =>
		KunaisBanePvE.EnoughLevel
			? KunaisBanePvE.Cooldown.IsCoolingDown
			: TrickAttackPvE.Cooldown.IsCoolingDown;

	private bool ShouldQueueBurstPrepSuitonOrHuton =>
		IsBurst &&
		!IsShadowWalking &&
		!HasTenChiJin &&
		!HasKassatsu &&
		 (
		!BurstActionIsCoolingDown ||
		BurstActionRecastRemain < BurstPrepThreshold
		);

	private bool InBurstPhase =>
		InCombat &&
		BurstActionRecastRemain > 45f;

	private bool KeepKassatsuinBurst =>
		!StatusHelper.PlayerWillStatusEndGCD(2, 0, true, StatusID.Kassatsu) &&
		HasKassatsu &&
		!InBurstPhase &&
		!IsExecutingMudra;

	#endregion

	#region Tiny Helpers

	private bool Q(IBaseAction a) => _ninActionAim == a;
	private static bool C(ActionID a) => IsCurrentNinjutsu(a);

	private bool S(out IAction? a, IBaseAction x) => x.CanUse(out a);
	private bool SA(out IAction? a, IBaseAction x) => x.CanUse(out a, skipAoeCheck: true);
	private bool SU(out IAction? a, IBaseAction x) => x.CanUse(out a, usedUp: true);

	private bool EnoughAndEnabled(IBaseAction a) => a.EnoughLevel && a.IsEnabled;

	private bool ReadyChiAoe(IBaseAction a) => EnoughAndEnabled(a) && ChiPvE.Info.IsQuestUnlocked();
	private bool ReadyTen(IBaseAction a) => EnoughAndEnabled(a) && TenPvE.Info.IsQuestUnlocked();

	private bool HasRaijuLessThan3 =>
		!StatusHelper.PlayerHasStatus(true, StatusID.RaijuReady) ||
		StatusHelper.PlayerStatusStack(true, StatusID.RaijuReady) < 3;

	private bool HasRaijuExactly3 =>
		StatusHelper.PlayerHasStatus(true, StatusID.RaijuReady) &&
		StatusHelper.PlayerStatusStack(true, StatusID.RaijuReady) == 3;

	private bool CanQueueSuiton =>
		SuitonPvE.EnoughLevel &&
		JinPvE.Info.IsQuestUnlocked() &&
		SuitonPvE.IsEnabled &&
		((TrickAttackPvE.IsEnabled && !KunaisBanePvE.EnoughLevel) ||
		 (KunaisBanePvE.IsEnabled && KunaisBanePvE.EnoughLevel));

	private bool CanQueueHuton =>
		HutonPvE.EnoughLevel &&
		JinPvE.Info.IsQuestUnlocked() &&
		HutonPvE.IsEnabled;

	private bool CanQueueKaton => ReadyChiAoe(KatonPvE);

	private bool CanQueueRaiton =>
		ReadyChiAoe(RaitonPvE) &&
		HasRaijuLessThan3;

	private bool CanQueueFuma =>
		ReadyTen(FumaShurikenPvE) &&
		(!RaitonPvE.EnoughLevel || HasRaijuExactly3);

	private bool CanQueueDoton =>
		(((!HasDoton &&
		   !IsMoving &&
		   !IsLastGCD(true, DotonPvE) &&
		   !TenChiJinPvE.Cooldown.WillHaveOneCharge(6) &&
		   DotonPvE.EnoughLevel) ||
		  (!HasDoton &&
		   !IsLastGCD(true, DotonPvE) &&
		   !TenChiJinPvE.Cooldown.IsCoolingDown &&
		   DotonPvE.EnoughLevel)) &&
		 JinPvE.CanUse(out _) &&
		 DotonPvE.IsEnabled &&
		 JinPvE.Info.IsQuestUnlocked());

	#endregion

	#region Ninjutsu Table

	private NinjutsuExecutionDef SuitonDef => new(
		SuitonPvE,
		() => C(ActionID.SuitonPvE),
		(out IAction? a) => S(out a, SuitonPvE),
		new StepDef(() => C(ActionID.RaitonPvE), (out IAction? a) => SU(out a, JinPvE_18807)),
		new StepDef(() => C(ActionID.FumaShurikenPvE), (out IAction? a) => SU(out a, ChiPvE_18806)),
		new StepDef(() => NoActiveNinjutsu, (out IAction? a) => SU(out a, TenPvE))
	);

	private NinjutsuExecutionDef DotonDef => new(
		DotonPvE,
		() => C(ActionID.DotonPvE),
		(out IAction? a) => SA(out a, DotonPvE),
		new StepDef(() => C(ActionID.HyotonPvE), (out IAction? a) => SU(out a, ChiPvE_18806)),
		new StepDef(() => C(ActionID.FumaShurikenPvE), (out IAction? a) => SU(out a, JinPvE_18807)),
		new StepDef(() => NoActiveNinjutsu, (out IAction? a) => SU(out a, TenPvE))
	);

	private NinjutsuExecutionDef HutonDef => new(
		HutonPvE,
		() => C(ActionID.HutonPvE),
		(out IAction? a) => SA(out a, HutonPvE),
		new StepDef(() => C(ActionID.HyotonPvE), (out IAction? a) => SU(out a, TenPvE_18805)),
		new StepDef(() => C(ActionID.FumaShurikenPvE), (out IAction? a) => SU(out a, JinPvE_18807)),
		new StepDef(() => NoActiveNinjutsu, (out IAction? a) => SU(out a, ChiPvE))
	);

	private NinjutsuExecutionDef HyotonDef => new(
		HyotonPvE,
		() => C(ActionID.HyotonPvE),
		(out IAction? a) => S(out a, HyotonPvE),
		new StepDef(() => C(ActionID.FumaShurikenPvE), (out IAction? a) => SU(out a, JinPvE_18807)),
		new StepDef(() => NoActiveNinjutsu, (out IAction? a) => SU(out a, ChiPvE))
	);

	private NinjutsuExecutionDef RaitonDef => new(
		RaitonPvE,
		() => C(ActionID.RaitonPvE),
		(out IAction? a) => S(out a, RaitonPvE),
		new StepDef(() => C(ActionID.FumaShurikenPvE), (out IAction? a) => SU(out a, ChiPvE_18806)),
		new StepDef(() => NoActiveNinjutsu, (out IAction? a) => SU(out a, TenPvE))
	);

	private NinjutsuExecutionDef KatonDef => new(
		KatonPvE,
		() => C(ActionID.KatonPvE),
		(out IAction? a) => SA(out a, KatonPvE),
		new StepDef(() => C(ActionID.FumaShurikenPvE), (out IAction? a) => SU(out a, TenPvE_18805)),
		new StepDef(() => NoActiveNinjutsu, (out IAction? a) => SU(out a, ChiPvE))
	);

	private NinjutsuExecutionDef FumaDef => new(
		FumaShurikenPvE,
		() => C(ActionID.FumaShurikenPvE),
		(out IAction? a) => S(out a, FumaShurikenPvE),
		new StepDef(() => NoActiveNinjutsu, (out IAction? a) => SU(out a, TenPvE))
	);

	#endregion

	#region Generic Executors / Choosers

	private bool ExecuteStandardQueuedNinjutsu(NinjutsuExecutionDef d, out IAction? act)
	{
		act = null;
		if (KeepKassatsuinBurst || !Q(d.QueuedAction)) return false;
		if (RabbitMediumCurrent) { ClearNinjutsu(); return false; }
		if (d.CurrentCheck()) return d.FinalCast(out act);
		foreach (var s in d.Steps) if (s.When()) return s.Use(out act);
		return false;
	}

	private bool TryUseAoeOrSingleTarget(
		bool aoe,
		ActionExecutor aoe1,
		ActionExecutor aoe2,
		ActionExecutor st1,
		ActionExecutor st2,
		out IAction? act)
	{
		act = null;
		if (aoe)
		{
			if (aoe1(out act)) return true;
			if (aoe2(out act)) return true;
		}
		else
		{
			if (st1(out act)) return true;
			if (st2(out act)) return true;
		}
		return false;
	}

	private void QueueStandardDamageNinjutsu(bool dotonAoe, bool katonAoe)
	{
		if (dotonAoe)
		{
			if (CanQueueDoton) { SetNinjutsu(DotonPvE); return; }
			if (katonAoe && CanQueueKaton) { SetNinjutsu(KatonPvE); return; }
		}
		else if (katonAoe && CanQueueKaton)
		{
			SetNinjutsu(KatonPvE);
			return;
		}

		if (!dotonAoe && !katonAoe && !ShouldQueueBurstPrepSuitonOrHuton)
		{
			if (CanQueueRaiton) SetNinjutsu(RaitonPvE);
			if (CanQueueFuma) SetNinjutsu(FumaShurikenPvE);
		}
	}

	private bool TryQueueBurstPrepSuitonOrHuton(bool hutonAoe)
	{
		if (!ShouldQueueBurstPrepSuitonOrHuton || TenPvE.Cooldown.CurrentCharges <= 0)
			return false;

		if (hutonAoe && CanQueueHuton)
		{
			SetNinjutsu(HutonPvE);
			return true;
		}

		if (CanQueueSuiton)
		{
			SetNinjutsu(SuitonPvE);
			return true;
		}

		return false;
	}

	private bool TryQueueDisengageFallbackNinjutsu(bool katonAoe)
	{
		if (!ShouldUseDisengageNinjutsuFallback ||
			!IsBurst ||
			CombatElapsedLess(BurstBuffOpenTiming) ||
			HasKassatsu ||
			HasTenChiJin ||
			ShouldQueueBurstPrepSuitonOrHuton)
		{
			return false;
		}

		if (katonAoe && CanQueueKaton)
		{
			SetNinjutsu(KatonPvE);
			return true;
		}

		if (CanQueueRaiton)
		{
			SetNinjutsu(RaitonPvE);
			return true;
		}

		return false;
	}

	private bool TryUseNinkiSpender(out IAction? act) =>
		TryUseAoeOrSingleTarget(
			ShouldUseAoeNinkiSpender(),
			(out IAction? a) => DeathfrogMediumPvE.CanUse(out a, skipAoeCheck: true),
			(out IAction? a) => HellfrogMediumPvE.CanUse(out a, skipAoeCheck: true),
			(out IAction? a) => ZeshoMeppoPvE.CanUse(out a),
			(out IAction? a) => BhavacakraPvE.CanUse(out a),
			out act);

	#endregion

	#region Display

	public override void DisplayRotationStatus()
	{
		ImGui.Text($"Last Ninjutsu Action Cleared From Queue: {_lastNinActionAim}");
		ImGui.Text($"Current Ninjutsu Action: {_ninActionAim}");
		ImGui.Text($"Ninjutsu ID: {AdjustId(NinjutsuPvEid)}");
		ImGui.Text($"Burst Prep Threshold: {BurstPrepThreshold}");
		ImGui.Text($"Should Queue Burst Prep Suiton/Huton: {ShouldQueueBurstPrepSuitonOrHuton}");
		ImGui.Text($"In Burst Phase: {InBurstPhase}");
		ImGui.Text($"Burst Action Recast Remain: {BurstActionRecastRemain}");
		ImGui.Text($"Kassatsu Charges: {KassatsuPvE.Cooldown.CurrentCharges}");
		ImGui.Text($"Kassatsu Recast Remain: {KassatsuPvE.Cooldown.RecastTimeRemain}");
		ImGui.Text($"Ten Charges: {TenPvE.Cooldown.CurrentCharges}");
		ImGui.Text($"Ten Recast Remain: {TenPvE.Cooldown.RecastTimeRemain}");

		ImGui.Text($"Self AoE 3+ (Death Blossom): {IsSelfAoe3Plus(DeathBlossomPvE)}");
		ImGui.Text($"Self AoE 3+ (Doton): {IsSelfAoe3Plus(DotonPvE)}");
		ImGui.Text($"Target AoE 3+ (Katon): {IsTargetAoe3Plus(KatonPvE)}");
		ImGui.Text($"Target AoE 3+ (Goka): {IsTargetAoe3Plus(GokaMekkyakuPvE)}");
		ImGui.Text($"Target AoE 3+ (Hellfrog): {IsTargetAoe3Plus(HellfrogMediumPvE)}");
		ImGui.Text($"Target AoE 3+ (Huton/Suiton prep): {IsTargetAoe3Plus(HutonPvE)}");

		ImGui.Text($"Current Ninjutsu Aim: {_ninActionAim}");
		ImGui.Text($"Last Ninjutsu Aim: {_lastNinActionAim}");
		ImGui.Text($"Has Kassatsu: {HasKassatsu}");
		ImGui.Text($"InBurstPhase: {InBurstPhase}");
		ImGui.Text($"CanQueueDamageMudra: {_ninActionAim == null && TenPvE.CanUse(out _, usedUp: true) && TenPvE.Cooldown.WillHaveXChargesGCD(2, 2)}");
		ImGui.Text($"ShouldSpendDamageMudraNow: {ShouldSpendDamageMudraNow}");
		ImGui.Text($"ShouldUseDisengageFallback: {ShouldUseDisengageNinjutsuFallback}");
		ImGui.Text($"ShouldQueueBurstPrepSuitonOrHuton: {ShouldQueueBurstPrepSuitonOrHuton}");
		ImGui.Text($"Ten Charges: {TenPvE.Cooldown.CurrentCharges}");
		ImGui.Text($"Ten Recast Remain: {TenPvE.Cooldown.RecastTimeRemain}");
	}

	#endregion

	#region AoE Counting

	private int GetSelfAoeCount(IBaseAction action)
	{
		float radius = action.Info.EffectRange > 0 ? action.Info.EffectRange : action.Info.Range;
		return NumberOfHostilesInRangeOf(radius);
	}

	private bool IsSelfAoe3Plus(IBaseAction action) => GetSelfAoeCount(action) >= AoeThreshold;

	private int GetTargetAoeCount(IBaseAction action)
	{
		int maxAoeCount = 0;
		if (AllHostileTargets == null) return 0;

		foreach (var centerTarget in AllHostileTargets)
		{
			if (centerTarget == null || !centerTarget.CanSee() || centerTarget.DistanceToPlayer() > action.Info.Range)
				continue;
			int currentAoeCount = 0;

			foreach (var otherTarget in AllHostileTargets)
			{
				if (otherTarget == null) continue;

				if (Vector3.Distance(centerTarget.Position, otherTarget.Position) <
					(action.Info.EffectRange + otherTarget.HitboxRadius))
				{
					currentAoeCount++;
				}
			}

			maxAoeCount = Math.Max(maxAoeCount, currentAoeCount);
		}

		return maxAoeCount;
	}

	private bool IsTargetAoe3Plus(IBaseAction action) => GetTargetAoeCount(action) >= AoeThreshold;
	private int NinkiSpenderAoeThreshold => HasMeisui ? 3 : 2;

	private bool IsTargetAoeAtLeast(IBaseAction action, int threshold) =>
		GetTargetAoeCount(action) >= threshold;

	private bool ShouldUseAoeNinkiSpender() =>
		IsTargetAoeAtLeast(HellfrogMediumPvE, NinkiSpenderAoeThreshold);

	private bool IsSelfAoe3PlusForTcj() =>
		IsSelfAoe3Plus(DeathBlossomPvE) ||
		IsSelfAoe3Plus(HakkeMujinsatsuPvE) ||
		IsSelfAoe3Plus(DotonPvE);

	#endregion

	#region Shared Rotation Helpers

	private bool ShouldClearQueuedNinjutsu()
	{
		if (_ninActionAim == null) return false;

		// Clear after successful execution of normal queued ninjutsu.
		if (IsLastAction(false, FumaShurikenPvE, KatonPvE, RaitonPvE, HyotonPvE, DotonPvE, SuitonPvE))
			return true;

		// Prep queues invalidated by ShadowWalking.
		if (IsShadowWalking && (_ninActionAim == SuitonPvE || _ninActionAim == HutonPvE))
			return true;

		// Kassatsu queues invalidated after use or if Kassatsu dropped.
		if (_ninActionAim == GokaMekkyakuPvE && IsLastGCD(false, GokaMekkyakuPvE))
			return true;

		if (_ninActionAim == HyoshoRanryuPvE && IsLastGCD(false, HyoshoRanryuPvE))
			return true;

		if ((_ninActionAim == GokaMekkyakuPvE || _ninActionAim == HyoshoRanryuPvE) && !HasKassatsu)
			return true;

		// Do not clear mid-mudra.
		if (IsExecutingMudra)
			return false;

		// Clear stale burst-prep queues if prep is no longer valid.
		if (_ninActionAim == SuitonPvE && !ShouldQueueBurstPrepSuitonOrHuton)
			return true;

		if (_ninActionAim == HutonPvE && !ShouldQueueBurstPrepSuitonOrHuton)
			return true;

		bool katonAoe = IsTargetAoe3Plus(KatonPvE);
		bool dotonAoe = IsSelfAoe3Plus(DotonPvE);

		bool disengageFallbackStillValid =
			ShouldUseDisengageNinjutsuFallback &&
			IsBurst &&
			!CombatElapsedLess(BurstBuffOpenTiming) &&
			!HasKassatsu &&
			!HasTenChiJin &&
			!ShouldQueueBurstPrepSuitonOrHuton;

		bool standardDamageStillValid =
			InBurstPhase ||
			ShouldSpendDamageMudraNow ||
			disengageFallbackStillValid;

		// ST queues should not remain if AoE became correct, or if no valid queue reason remains.
		if ((_ninActionAim == RaitonPvE || _ninActionAim == FumaShurikenPvE) &&
			(!standardDamageStillValid || dotonAoe || katonAoe))
			return true;

		// Katon should not remain if target-AoE is no longer correct, or if no valid queue reason remains.
		if (_ninActionAim == KatonPvE &&
			(!standardDamageStillValid || !katonAoe))
			return true;

		// Doton should not remain if self-AoE is no longer correct, or if no valid queue reason remains.
		if (_ninActionAim == DotonPvE &&
			(!standardDamageStillValid || !dotonAoe))
			return true;

		return false;
	}

	private void RefreshNinjutsuChoice()
	{
		if (InCombat && HasHostilesInMaxRange)
		{
			_ = ChoiceNinjutsu(out _);
		}

		if (!InCombat) ClearNinjutsu();
	}

	private bool TryUseRaidBuff(out IAction? act)
	{
		act = null;
		if (!IsBurst || CombatElapsedLess(RaidBuffOpenTiming)) return false;
		return !DokumoriPvE.EnoughLevel ? MugPvE.CanUse(out act) : DokumoriPvE.CanUse(out act);
	}

	private bool TryUseBurstBuff(out IAction? act)
	{
		act = null;

		if (CombatElapsedLess(BurstBuffOpenTiming)) return false;

		if (CombatElapsedLess(BurstBuffOpenTiming)) return false;

		if (RequireLateWeaveForBurstBuff && !CanLateWeave) return false;

		if (!KunaisBanePvE.EnoughLevel)
		{
			if (TrickAttackPvE.CanUse(out act, skipStatusProvideCheck: IsShadowWalking)) return true;
		}
		else
		{
			if (KunaisBanePvE.CanUse(out act, skipAoeCheck: true, skipStatusProvideCheck: IsShadowWalking)) return true;
		}

		if (TrickAttackPvE.Cooldown.IsCoolingDown &&
			!TrickAttackPvE.Cooldown.WillHaveOneCharge(19) &&
			TenChiJinPvE.Cooldown.IsCoolingDown &&
			TrickAttackPvE.Cooldown.IsCoolingDown &&
			MeisuiPvE.CanUse(out act))
		{
			return true;
		}

		return false;
	}

	private bool ShouldUsePotionNow()
	{
		if (!PotionUsageEnabled || !InCombat || !IsShadowWalking) return false;
		return PotionUsagePreset switch
		{
			NINPotionPreset.Standard0611 => DokumoriPvE.EnoughLevel
				? !DokumoriPvE.Cooldown.IsCoolingDown || DokumoriPvE.Cooldown.WillHaveOneCharge(5)
				: !MugPvE.Cooldown.IsCoolingDown || MugPvE.Cooldown.WillHaveOneCharge(5),

			NINPotionPreset.Standard0510 => KunaisBanePvE.EnoughLevel
				? !KunaisBanePvE.Cooldown.IsCoolingDown || KunaisBanePvE.Cooldown.WillHaveOneCharge(5)
				: !MugPvE.Cooldown.IsCoolingDown || MugPvE.Cooldown.WillHaveOneCharge(5),

			_ => false,
		};
	}

	#endregion

	#region Countdown / Movement

	protected override IAction? CountDownAction(float remainTime)
	{
		_ = IsLastAction(false, HutonPvE);

		if (remainTime > 6) ClearNinjutsu();

		if (DoSuiton(out IAction? act))
		{
			return act == SuitonPvE && remainTime > CountDownAhead ? null : act;
		}

		if (remainTime < 5)
		{
			SetNinjutsu(SuitonPvE);
		}
		else if (remainTime < 6)
		{
			if (_ninActionAim == null &&
				TenPvE.Cooldown.IsCoolingDown &&
				HidePvE.CanUse(out act))
			{
				return act;
			}
		}

		return base.CountDownAction(remainTime);
	}

	[RotationDesc(ActionID.ForkedRaijuPvE)]
	protected override bool MoveForwardGCD(out IAction? act)
	{
		if (ForkedRaijuPvE.CanUse(out act)) return true;
		return base.MoveForwardGCD(out act);
	}

	#endregion

	#region oGCD

	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		if (ShouldClearQueuedNinjutsu()) ClearNinjutsu();
		RefreshNinjutsuChoice();
		if (!InCombat) ClearNinjutsu();

		if (RabbitMediumPvE.CanUse(out act)) return true;
		if (!NoNinjutsu || !InCombat) return base.EmergencyAbility(nextGCD, out act);

		if (NoNinjutsu &&
			!nextGCD.IsTheSameTo(false, ActionID.TenPvE, ActionID.ChiPvE, ActionID.JinPvE) &&
			IsShadowWalking &&
			KassatsuPvE.CanUse(out act))
		{
			return true;
		}

		if ((!TenChiJinPvE.Cooldown.IsCoolingDown ||
			 StatusHelper.PlayerWillStatusEndGCD(2, 0, true, StatusID.ShadowWalker)) &&
			TrickAttackPvE.Cooldown.IsCoolingDown &&
			MeisuiPvE.CanUse(out act))
		{
			return true;
		}

		if (TenriJindoPvE.CanUse(out act)) return true;
		if (CanLateWeave && ShouldUsePotionNow() && UseBurstMedicine(out act)) return true;
		if (TryUseRaidBuff(out act)) return true;
		if (TryUseBurstBuff(out act)) return true;

		if (IsBurst && !CombatElapsedLess(RaidBuffOpenTiming) && (InBurstPhase || HasBuffs))
		{
			return !DokumoriPvE.EnoughLevel ? MugPvE.CanUse(out act) : DokumoriPvE.CanUse(out act);
		}

		return base.EmergencyAbility(nextGCD, out act);
	}

	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		if (!NoNinjutsu || !InCombat) return base.AttackAbility(nextGCD, out act);

		bool lateRaidWindow = CanLateWeave && !CombatElapsedLess(RaidBuffOpenTiming);
		bool canSpendNinkiNormally =
			(!InMug || InTrickAttack) &&
			(!BunshinPvE.Cooldown.WillHaveOneCharge(10) || HasPhantomKamaitachi || MugPvE.Cooldown.WillHaveOneCharge(2));

		if (InBurstPhase &&
			!StatusHelper.PlayerHasStatus(true, StatusID.ShadowWalker) &&
			!TenPvE.Cooldown.ElapsedAfter(30) &&
			TenChiJinPvE.CanUse(out act))
		{
			return true;
		}

		if (lateRaidWindow && BunshinPvE.CanUse(out act)) return true;
		if (TryUseRaidBuff(out act)) return true;

		if (InBurstPhase)
		{
			if (DreamWithinADreamPvE.CanUse(out act)) return true;
			if (!DreamWithinADreamPvE.Info.EnoughLevelAndQuest() && AssassinatePvE.CanUse(out act)) return true;
		}

		if (canSpendNinkiNormally)
		{
			if (TryUseNinkiSpender(out act)) return true;
			if (TenriJindoPvE.CanUse(out act)) return true;
		}

		if (Ninki >= 90 && TryUseNinkiSpender(out act)) return true;
		if (MergedStatus.HasFlag(AutoStatus.MoveForward) && MoveForwardAbility(nextGCD, out act)) return true;

		return base.AttackAbility(nextGCD, out act);
	}

	#endregion

	#region Queue Management

	private void SetNinjutsu(IBaseAction act)
	{
		if (act == null || AdjustId(ActionID.NinjutsuPvE) == ActionID.RabbitMediumPvE) return;

		if (_ninActionAim != null &&
			IsLastAction(false, TenPvE, JinPvE, ChiPvE, FumaShurikenPvE_18873, FumaShurikenPvE_18874, FumaShurikenPvE_18875))
		{
			return;
		}

		if (_ninActionAim != act) _ninActionAim = act;
	}

	private void ClearNinjutsu()
	{
		if (_ninActionAim == null) return;
		_lastNinActionAim = _ninActionAim;
		_ninActionAim = null;
	}

	private bool ChoiceNinjutsu(out IAction? act)
	{
		act = null;

		bool dotonAoe = IsSelfAoe3Plus(DotonPvE);
		bool katonAoe = IsTargetAoe3Plus(KatonPvE);
		bool gokaAoe = IsTargetAoe3Plus(GokaMekkyakuPvE);
		bool hutonAoe = IsTargetAoe3Plus(HutonPvE);
		bool canQueueDamageMudra =
			_ninActionAim == null &&
			ShouldSpendDamageMudraNow;

		if (HasKassatsu)
		{
			// Expiry protection: force the Kassatsu ninjutsu immediately.
			if (KassatsuExpiringSoon)
			{
				if (gokaAoe &&
					GokaMekkyakuPvE.EnoughLevel &&
					!IsLastAction(false, GokaMekkyakuPvE) &&
					GokaMekkyakuPvE.IsEnabled &&
					ChiPvE.Info.IsQuestUnlocked())
				{
					SetNinjutsu(GokaMekkyakuPvE);
				}
				else if (!gokaAoe &&
						 HyoshoRanryuPvE.EnoughLevel &&
						 !IsLastAction(false, HyoshoRanryuPvE) &&
						 HyoshoRanryuPvE.IsEnabled &&
						 JinPvE.Info.IsQuestUnlocked())
				{
					SetNinjutsu(HyoshoRanryuPvE);
				}
				else if (katonAoe &&
						 !HyoshoRanryuPvE.EnoughLevel &&
						 ReadyChiAoe(KatonPvE))
				{
					SetNinjutsu(KatonPvE);
				}
				else if (!katonAoe &&
						 !HyoshoRanryuPvE.EnoughLevel &&
						 ReadyChiAoe(RaitonPvE))
				{
					SetNinjutsu(RaitonPvE);
				}

				return false;
			}

			if (gokaAoe &&
				GokaMekkyakuPvE.EnoughLevel &&
				!IsLastAction(false, GokaMekkyakuPvE) &&
				GokaMekkyakuPvE.IsEnabled &&
				ChiPvE.Info.IsQuestUnlocked())
			{
				SetNinjutsu(GokaMekkyakuPvE);
			}
			else if (!gokaAoe &&
					 HyoshoRanryuPvE.EnoughLevel &&
					 !IsLastAction(false, HyoshoRanryuPvE) &&
					 HyoshoRanryuPvE.IsEnabled &&
					 JinPvE.Info.IsQuestUnlocked())
			{
				SetNinjutsu(HyoshoRanryuPvE);
			}
			else if (katonAoe &&
					 !HyoshoRanryuPvE.EnoughLevel &&
					 ReadyChiAoe(KatonPvE))
			{
				SetNinjutsu(KatonPvE);
			}
			else if (!katonAoe &&
					 !HyoshoRanryuPvE.EnoughLevel &&
					 ReadyChiAoe(RaitonPvE))
			{
				SetNinjutsu(RaitonPvE);
			}

			return false;
		}

		if (_ninActionAim != null) return false;

		// Burst prep has top priority when inside configured burst timing window.
		if (TryQueueBurstPrepSuitonOrHuton(hutonAoe)) return false;

		// Disengaged fallback sits below burst prep.
		if (TryQueueDisengageFallbackNinjutsu(katonAoe)) return false;

		if (InBurstPhase || canQueueDamageMudra)
			QueueStandardDamageNinjutsu(dotonAoe, katonAoe);

		return false;
	}

	#endregion

	#region Ninjutsu Execution

	private bool DoRabbitMedium(out IAction? act)
	{
		act = null;
		if (AdjustId(NinjutsuPvE.ID) != RabbitMediumPvE.ID) return false;
		if (RabbitMediumPvE.CanUse(out act)) return true;
		ClearNinjutsu();
		return false;
	}

	private bool DoTenChiJin(out IAction? act)
	{
		act = null;
		if (!HasTenChiJin) return false;

		uint tenId = AdjustId(TenPvE.ID);
		uint chiId = AdjustId(ChiPvE.ID);
		uint jinId = AdjustId(JinPvE.ID);

		if (tenId == FumaShurikenPvE_18873.ID &&
			!IsLastAction(false, FumaShurikenPvE_18875, FumaShurikenPvE_18873))
		{
			if (IsSelfAoe3PlusForTcj())
			{
				if (FumaShurikenPvE_18875.CanUse(out act)) return true;
			}

			if (FumaShurikenPvE_18873.CanUse(out act)) return true;
		}
		else if (tenId == KatonPvE_18876.ID && !IsLastAction(false, KatonPvE_18876))
		{
			if (KatonPvE_18876.CanUse(out act, skipAoeCheck: true)) return true;
		}
		else if (chiId == RaitonPvE_18877.ID && !IsLastAction(false, RaitonPvE_18877))
		{
			if (RaitonPvE_18877.CanUse(out act, skipAoeCheck: true)) return true;
		}
		else if (jinId == SuitonPvE_18881.ID && !IsLastAction(false, SuitonPvE_18881))
		{
			if (SuitonPvE_18881.CanUse(out act, skipAoeCheck: true, skipStatusProvideCheck: true)) return true;
		}
		else if (chiId == DotonPvE_18880.ID && !IsLastAction(false, DotonPvE_18880) && !HasDoton)
		{
			if (DotonPvE_18880.CanUse(out act, skipAoeCheck: true, skipStatusProvideCheck: true)) return true;
		}

		return false;
	}

	private bool DoHyoshoRanryu(out IAction? act)
	{
		act = null;

		if (!KassatsuExpiringSoon &&
			(!TrickAttackPvE.Cooldown.IsCoolingDown ||
			 TrickAttackPvE.Cooldown.WillHaveOneCharge(StatusHelper.PlayerStatusTime(true, StatusID.Kassatsu))) &&
			!IsExecutingMudra)
		{
			return false;
		}

		if (!Q(HyoshoRanryuPvE)) return false;
		if (RabbitMediumCurrent) { ClearNinjutsu(); return false; }
		if (C(ActionID.HyoshoRanryuPvE)) return HyoshoRanryuPvE.CanUse(out act, skipAoeCheck: true);
		if (C(ActionID.FumaShurikenPvE)) return JinPvE_18807.CanUse(out act, usedUp: true);
		if (NoActiveNinjutsu) return ChiPvE_18806.CanUse(out act, usedUp: true);
		return false;
	}

	private bool DoGokaMekkyaku(out IAction? act)
	{
		act = null;

		if (!KassatsuExpiringSoon &&
			(!TrickAttackPvE.Cooldown.IsCoolingDown ||
			 TrickAttackPvE.Cooldown.WillHaveOneCharge(StatusHelper.PlayerStatusTime(true, StatusID.Kassatsu))) &&
			!IsExecutingMudra)
		{
			return false;
		}

		if (!Q(GokaMekkyakuPvE)) return false;
		if (RabbitMediumCurrent) { ClearNinjutsu(); return false; }
		if (C(ActionID.GokaMekkyakuPvE)) return GokaMekkyakuPvE.CanUse(out act, skipAoeCheck: true);
		if (C(ActionID.FumaShurikenPvE)) return TenPvE_18805.CanUse(out act, usedUp: true);
		if (NoActiveNinjutsu) return ChiPvE_18806.CanUse(out act, usedUp: true);
		return false;
	}

	private bool DoSuiton(out IAction? act) => ExecuteStandardQueuedNinjutsu(SuitonDef, out act);
	private bool DoDoton(out IAction? act) => ExecuteStandardQueuedNinjutsu(DotonDef, out act);
	private bool DoHuton(out IAction? act) => ExecuteStandardQueuedNinjutsu(HutonDef, out act);
	private bool DoHyoton(out IAction? act) => ExecuteStandardQueuedNinjutsu(HyotonDef, out act);
	private bool DoRaiton(out IAction? act) => ExecuteStandardQueuedNinjutsu(RaitonDef, out act);
	private bool DoKaton(out IAction? act) => ExecuteStandardQueuedNinjutsu(KatonDef, out act);
	private bool DoFumaShuriken(out IAction? act) => ExecuteStandardQueuedNinjutsu(FumaDef, out act);

	#endregion

	#region GCD

	protected override bool GeneralGCD(out IAction? act)
	{
		bool noMudra = !IsExecutingMudra;
		bool noNin = NoNinjutsu;
		bool notTcj = Player != null && !StatusHelper.PlayerHasStatus(true, StatusID.TenChiJin);

		if (noMudra &&
			(InTrickAttack || InMug) &&
			noNin &&
			!HasRaijuReady &&
			notTcj &&
			PhantomKamaitachiPvE.CanUse(out act))
		{
			return true;
		}

		if (noMudra && FleetingRaijuPvE.CanUse(out act)) return true;
		if (DoTenChiJin(out act)) return true;
		if (DoRabbitMedium(out act)) return true;

		if (_ninActionAim != null && GCDTime() == 0f)
		{
			if (DoGokaMekkyaku(out act)) return true;
			if (DoHuton(out act)) return true;
			if (DoDoton(out act)) return true;
			if (DoKaton(out act)) return true;
			if (DoHyoshoRanryu(out act)) return true;
			if (DoSuiton(out act)) return true;
			if (DoHyoton(out act)) return true;
			if (DoRaiton(out act)) return true;
			if (DoFumaShuriken(out act)) return true;
		}

		if (IsExecutingMudra) return base.GeneralGCD(out act);
		if (IsSelfAoe3Plus(HakkeMujinsatsuPvE) && HakkeMujinsatsuPvE.CanUse(out act)) return true;
		if (IsSelfAoe3Plus(DeathBlossomPvE) && DeathBlossomPvE.CanUse(out act)) return true;

		if (AeolianEdgePvE.EnoughLevel)
		{
			if (!ArmorCrushPvE.EnoughLevel)
			{
				if (AeolianEdgePvE.CanUse(out act)) return true;
			}
			else
			{
				if (InBurstPhase &&
					Kazematoi > 0 &&
					AeolianEdgePvE.CanUse(out act) &&
					AeolianEdgePvE.Target.Target != null &&
					CanHitPositional(EnemyPositional.Rear, AeolianEdgePvE.Target.Target))
					return true;

				if (InBurstPhase &&
					Kazematoi > 0 &&
					AeolianEdgePvE.CanUse(out act))
					return true;

				if (Kazematoi < 2 &&
					ArmorCrushPvE.CanUse(out act) &&
					ArmorCrushPvE.Target.Target != null &&
					CanHitPositional(EnemyPositional.Flank, ArmorCrushPvE.Target.Target))
					return true;

				if (Kazematoi == 0 && ArmorCrushPvE.CanUse(out act)) return true;

				if (Kazematoi > 0 &&
					AeolianEdgePvE.CanUse(out act) &&
					AeolianEdgePvE.Target.Target != null &&
					CanHitPositional(EnemyPositional.Rear, AeolianEdgePvE.Target.Target))
					return true;

				if (Kazematoi < 4 &&
					ArmorCrushPvE.CanUse(out act) &&
					ArmorCrushPvE.Target.Target != null &&
					CanHitPositional(EnemyPositional.Flank, ArmorCrushPvE.Target.Target))
					return true;

				if (Kazematoi > 0 && AeolianEdgePvE.CanUse(out act)) return true;
				if (Kazematoi < 4 && ArmorCrushPvE.CanUse(out act)) return true;
			}
		}

		if (GustSlashPvE.CanUse(out act)) return true;
		if (SpinningEdgePvE.CanUse(out act)) return true;

		if (noMudra &&
			noNin &&
			notTcj &&
			PhantomKamaitachiPvE.CanUse(out act))
		{
			return true;
		}

		if (noMudra &&
			noNin &&
			IsBurst &&
			_ninActionAim == null &&
			!HasKassatsu)
		{
			_ = ChoiceNinjutsu(out _);
		}

		if (noMudra && ThrowingDaggerPvE.CanUse(out act)) return true;
		if (StateEnabled && IsHidden) StatusHelper.StatusOff(StatusID.Hidden);

		if (!InCombat &&
			_ninActionAim == null &&
			TenPvE.Cooldown.IsCoolingDown &&
			HidePvE.CanUse(out act))
		{
			return true;
		}

		return base.GeneralGCD(out act);
	}

	#endregion
}