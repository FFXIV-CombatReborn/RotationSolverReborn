using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Config;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using RotationSolver.Basic.Configuration;
using RotationSolver.Basic.Rotations.Duties;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using Action = Lumina.Excel.Sheets.Action;
using CharacterManager = FFXIVClientStructs.FFXIV.Client.Game.Character.CharacterManager;
using CombatRole = RotationSolver.Basic.Data.CombatRole;

namespace RotationSolver.Basic;

internal static class DataCenter
{
	public static List<IBattleChara> PartyMembers { get; set; } = [];

	public static List<IBattleChara> AllianceMembers { get; set; } = [];

	public static List<IBattleChara> AllHostileTargets { get; set; } = [];

	public static IBattleChara? InterruptTarget { get; set; }

	public static IBattleChara? ProvokeTarget { get; set; }

	public static IBattleChara? DeathTarget { get; set; }

	public static IBattleChara? DispelTarget { get; set; }

	public static List<IBattleChara> AllTargets { get; set; } = [];
	public static Dictionary<float, List<IBattleChara>> TargetsByRange { get; set; } = [];

	/// <summary>
	/// The action most recently queued via interception (current).
	/// Set by the interception logic when an action is queued for RSR to attempt.
	/// </summary>
	public static IAction? CurrentInterceptedAction { get; set; }

	public static bool IsInDutyReplay()
	{
		if (!PlayerAvailable())
		{
			return false;
		}

		return Svc.Condition[ConditionFlag.DutyRecorderPlayback];
	}

	private static ulong _hostileTargetId = 0;

	// Tracking fields for Tyrant special sequence (Scythe/Axe -> Charybdistopia)
	private static bool _hasCastScythe = false;
	private static bool _hasCastAxe = false;
	private static bool _wasCastingCharyb = false;
	private static bool _tyrantShouldStopHealing = false;

	public static bool ResetActionConfigs { get; set; } = false;

	public static int PlayerSyncedLevel()
	{
		if (Player.IsLevelSynced)
		{
			return Player.SyncedLevel;
		}

		if (PlayerCurrentLevel < PlayerMaxLevel)
		{
			return PlayerCurrentLevel;
		}

		return PlayerMaxLevel;
	}

	public unsafe static int PlayerCurrentLevel => PlayerState.Instance()->CurrentLevel;

	public static int PlayerMaxLevel => Player.MaxLevel;

	public static bool IsActivated()
	{
		return Player.Available && (State || IsManual || Service.Config.TeachingMode);
	}

	public static bool PlayerAvailable()
	{
		return Player.Available && Player.Object != null;
	}

	public static bool DalamudStagingEnabled = false;
	public static bool IsOnStaging()
	{
		try
		{
			var v = Svc.PluginInterface.GetDalamudVersion();
			if (v.BetaTrack != null && v.BetaTrack.Equals("release", StringComparison.CurrentCultureIgnoreCase))
			{
				DalamudStagingEnabled = false;
				return false;
			}
			else
			{
				DalamudStagingEnabled = true;
				return true;
			}
		}
		catch (Exception ex)
		{
			ex.Log("Probably CN or something");
			DalamudStagingEnabled = false;
			return false;
		}
	}

	public static bool AutoFaceTargetOnActionSetting()
	{
		return Svc.GameConfig.UiControl.GetBool(UiControlOption.AutoFaceTargetOnAction.ToString());
	}

	public static uint MoveModeSetting()
	{
		// 0 is standard, 1 is legacy
		return Svc.GameConfig.UiControl.GetUInt(UiControlOption.MoveMode.ToString());
	}

	internal static IBattleChara? HostileTarget
	{
		get => Svc.Objects.SearchById(_hostileTargetId) as IBattleChara;
		set => _hostileTargetId = value?.GameObjectId ?? 0;
	}

	internal static List<uint> PrioritizedNameIds { get; set; } = [];
	internal static List<uint> BlacklistedNameIds { get; set; } = [];

	/// <summary>
	/// List of hostile NameIds that should be excluded as valid targets when an action opts-in via IsRestrictedDOT.
	/// </summary>
	internal static List<uint> RestrictedDotNameIds { get; set; } =
	[
		9214,
	];

	/// <summary>
	/// 
	/// </summary>
	internal static List<uint> RestrictedActionNameIds { get; set; } =
	[
		14301, 14499,
	];

	internal static ConcurrentQueue<VfxNewData> VfxDataQueue { get; } = new();

	/// <summary>
	/// Players currently targeted by tankbuster VFX markers (populated from VFX queue).
	/// </summary>
	internal static List<IBattleChara> TankbusterTargets { get; } = [];

	private static readonly Lock _tankbusterLock = new();

	/// <summary>
	/// Only recorded 15s hps.
	/// </summary>
	public const int HP_RECORD_TIME = 240;

	internal static Queue<(DateTime time, Dictionary<ulong, float> hpRatios)> RecordedHP { get; } =
		new(HP_RECORD_TIME + 1);

	public static ICustomRotation? CurrentRotation { get; internal set; }
	public static DutyRotation? CurrentDutyRotation { get; internal set; }

	public static Dictionary<string, DateTime> SystemWarnings { get; set; } = [];
	public static bool HoldingRestore = false;

	internal static bool NoPoslock => Svc.Condition[ConditionFlag.OccupiedInEvent]
									  || !Service.Config.PoslockCasting
									  //Key cancel.
									  || Svc.KeyState[Service.Config.PoslockModifier.ToVirtual()]
									  //Gamepad cancel.
									  || Svc.GamepadState.Raw(Dalamud.Game.ClientState.GamePad.GamepadButtons.R1) >=
									  0.5f;

	internal static DateTime EffectTime { private get; set; } = DateTime.Now;
	internal static DateTime EffectEndTime { private get; set; } = DateTime.Now;

	internal static int AttackedTargetsCount { get; set; } = 48;
	internal static Queue<(ulong id, DateTime time)> AttackedTargets { get; } = new(AttackedTargetsCount);

	internal static Queue<MacroItem> Macros { get; } = new Queue<MacroItem>();

	internal static bool InEffectTime => DateTime.Now >= EffectTime && DateTime.Now <= EffectEndTime;
	internal static Dictionary<ulong, uint> HealHP { get; set; } = [];
	internal static Dictionary<ulong, uint> ApplyStatus { get; set; } = [];
	internal static uint MPGain { get; set; }

	public static AutoStatus MergedStatus => AutoStatus | CommandStatus;
	public static AutoStatus AutoStatus { get; set; } = AutoStatus.None;
	public static AutoStatus CommandStatus { get; set; } = AutoStatus.None;

	private static readonly List<NextAct> NextActs = [];

	public static IAction? CommandNextAction
	{
		get
		{
			NextAct? next = null;
			if (NextActs.Count > 0)
			{
				next = NextActs[0];
			}

			while (next != null && NextActs.Count > 0 &&
				   (next.DeadTime < DateTime.Now || IActionHelper.IsLastAction(false, next.Act)))
			{
				NextActs.RemoveAt(0);
				next = NextActs.Count > 0 ? NextActs[0] : null;
			}

			return next?.Act;
		}
	}

	internal static void AddCommandAction(IAction act, double time)
	{
		int index = -1;
		for (int i = 0; i < NextActs.Count; i++)
		{
			if (NextActs[i].Act.ID == act.ID)
			{
				index = i;
				break;
			}
		}

		NextAct newItem = new(act, DateTime.Now.AddSeconds(time));
		if (index < 0)
		{
			NextActs.Add(newItem);
		}
		else
		{
			NextActs[index] = newItem;
		}

		NextActs.Sort((a, b) => a.DeadTime.CompareTo(b.DeadTime));
	}
	public static TargetHostileType CurrentTargetToHostileType => Service.Config.HostileType;

	public static TargetingType? TargetingTypeOverride { get; set; }

	public static TargetingType TargetingType
	{
		get
		{
			if (TargetingTypeOverride.HasValue)
			{
				return TargetingTypeOverride.Value;
			}

			if (Service.Config.TargetingTypes.Count == 0)
			{
				Service.Config.TargetingTypes.Add(TargetingType.LowHP);
				Service.Config.TargetingTypes.Add(TargetingType.HighHP);
				Service.Config.TargetingTypes.Add(TargetingType.Small);
			 Service.Config.TargetingTypes.Add(TargetingType.Big);
				Service.Config.Save();
			}

			return Service.Config.TargetingTypes[Service.Config.TargetingIndex % Service.Config.TargetingTypes.Count];
		}
	}

	public static TinctureUseType CurrentTinctureUseType => Service.Config.TinctureType;

	public static unsafe ActionID LastComboAction => (ActionID)ActionManager.Instance()->Combo.Action;

	public static unsafe float ComboTime => ActionManager.Instance()->Combo.Timer;

	public static bool IsMoving => Player.IsMoving;

	internal static float StopMovingRaw { get; set; }

	internal static float MovingRaw { get; set; }
	internal static float DeadTimeRaw { get; set; }
	internal static float AliveTimeRaw { get; set; }

	public static uint[] BluSlots { get; internal set; } = new uint[24];

	public static uint[] DutyActions { get; internal set; } = new uint[5];

	private static DateTime _specialStateStartTime = DateTime.MinValue;
	private static double SpecialTimeElapsed => (DateTime.Now - _specialStateStartTime).TotalSeconds;
	public static double SpecialTimeLeft => Service.Config.SpecialDuration - SpecialTimeElapsed;

	private static SpecialCommandType _specialType = SpecialCommandType.EndSpecial;

	internal static SpecialCommandType SpecialType
	{
		get => SpecialTimeLeft < 0 ? SpecialCommandType.EndSpecial : _specialType;
		set
		{
			_specialType = value;
			_specialStateStartTime = value == SpecialCommandType.EndSpecial ? DateTime.MinValue : DateTime.Now;
		}
	}

	public static bool State { get; set; } = false;

	public static bool IsManual { get; set; } = false;

	public static bool IsAutoDuty { get; set; } = false;

	public static bool IsHenched { get; set; } = false;

	public static bool IsPvPStateEnabled { get; set; } = false;

	public static bool IsTargetOnly { get; set; } = false;

	public static bool InCombat { get; set; } = false;

	public static bool DrawingActions { get; set; } = false;

	private static RandomDelay _notInCombatDelay = new(() => Service.Config.NotInCombatDelay);

	/// <summary>
	/// Is out of combat.
	/// </summary>
	public static bool NotInCombatDelay => _notInCombatDelay.Delay(!InCombat);

	internal static float CombatTimeRaw { get; set; }

	private static DateTime _startRaidTime = DateTime.MinValue;

	internal static float RaidTimeRaw
	{
		get
		{
			// If the raid start time is not set, return 0.
			if (_startRaidTime == DateTime.MinValue)
			{
				return 0;
			}

			// Calculate and return the total seconds elapsed since the raid started.
			return (float)(DateTime.Now - _startRaidTime).TotalSeconds;
		}
		set
		{
			// If the provided value is negative, reset the raid start time.
			if (value < 0)
			{
				_startRaidTime = DateTime.MinValue;
			}
			else
			{
				// Set the raid start time to the current time minus the provided value in seconds.
				_startRaidTime = DateTime.Now - TimeSpan.FromSeconds(value);
			}
		}
	}

	private static float _cachedJobRange = -1f;
	private static int _cachedTargetCount = 0;
	private static int _lastTargetFrame = -1;

	public static bool MobsTime
	{
		get
		{
			int currentFrame = Environment.TickCount;
			if (_lastTargetFrame != currentFrame)
			{
				_cachedJobRange = JobRange;
				_cachedTargetCount = 0;
				var targets = AllHostileTargets;
				for (int i = 0, n = targets.Count; i < n; i++)
				{
					var o = targets[i];
					if (o.DistanceToPlayer() < _cachedJobRange && o.CanSee())
					{
						_cachedTargetCount++;
					}
				}
				_lastTargetFrame = currentFrame;
			}
			return _cachedTargetCount >= Service.Config.AutoDefenseNumber;
		}
	}

	private static float _avgTTK = 0f;
	public static float AverageTTK
	{
		get
		{
			float total = 0f;
			int count = 0;
			var targets = AllHostileTargets;
			for (int i = 0, n = targets.Count; i < n; i++)
			{
				float ttk = targets[i].GetTTK();
				if (!float.IsNaN(ttk))
				{
					total += ttk;
					count++;
				}
			}
			_avgTTK = count > 0 ? total / count : 0f;
			return _avgTTK;
		}
	}

	#region Territory Info Tracking

	public static Data.TerritoryInfo? Territory { get; set; }
	public static uint TerritoryID => (ushort)Svc.ClientState.TerritoryType;

	public static bool IsPvP => Territory?.IsPvP ?? false;

	/// <summary>
	/// When set to <c>true</c> by an external plugin via IPC, the TargetFreely behaviour is
	/// activated for the current session without modifying the user's <c>TargetFreely</c>
	/// config value.  Reset to <c>false</c> by calling the corresponding IPC method.
	/// </summary>
	public static bool TargetFreelyOverride { get; set; }

	public static bool IsInMaskedCarnivale => Territory?.ContentType == TerritoryContentType.TheMaskedCarnivale;

	public static bool IsInDuty => Svc.Condition[ConditionFlag.BoundByDuty] || Svc.Condition[ConditionFlag.BoundByDuty56];

	public static bool IsInAllianceRaid
	{
		get
		{
			ushort[] allianceTerritoryIds =
			[
				151, 174, 372, 508, 556, 627, 734, 776, 826, 882, 917, 966, 1054, 1118, 1178, 1248, 1304, 1368
			];

			for (int i = 0; i < allianceTerritoryIds.Length; i++)
			{
				if (allianceTerritoryIds[i] == TerritoryID)
					return true;
			}

			return false;
		}
	}

	public static bool IsInTerritory(ushort territoryId)
	{
		return TerritoryID == territoryId;
	}

	#endregion

	#region Ultimate
	public static bool IsInUCoB => TerritoryID == 733;
	public static bool IsInUwU => TerritoryID == 777;
	public static bool IsInTEA => TerritoryID == 887;
	public static bool IsInDSR => TerritoryID == 968;
	public static bool IsInTOP => TerritoryID == 1122;
	public static bool IsInFRU => TerritoryID == 1238;
	public static bool IsInCOD => TerritoryID == 1241;
	#endregion

	#region Savage
	public static bool IsInM9S => TerritoryID == 1321;
	public static bool IsInM10S => TerritoryID == 1323;
	public static bool IsInM11S => TerritoryID == 1327;
	public static bool IsInM12S => TerritoryID == 1325;
	#endregion

	#region Savage
	public static bool IsTheUnmaking => TerritoryID == 1362;
	#endregion

	#region Alliance Raid
	public static bool IsInWindurst => TerritoryID == 1368;
	#endregion

	#region FATE
	/// <summary>
	/// 
	/// </summary>
	public static unsafe ushort PlayerFateId
	{
		get
		{
			try
			{
				if ((IntPtr)FateManager.Instance() != IntPtr.Zero
					&& (IntPtr)FateManager.Instance()->CurrentFate != IntPtr.Zero
					&& DataCenter.PlayerSyncedLevel() <= FateManager.Instance()->CurrentFate->MaxLevel)
				{
					return FateManager.Instance()->CurrentFate->FateId;
				}
			}
			catch (Exception ex)
			{
				PluginLog.Error(ex.StackTrace ?? ex.Message);
			}

			return 0;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public static bool IsInFate => PlayerFateId != 0 && !IsInBozja && !IsInOccultCrescentOp;

	#endregion

	#region Bozja
	/// <summary>
	/// Determines if the current content is Bozjan Southern Front or Zadnor.
	/// </summary>
	public static bool IsInBozjanFieldOp => Content.ContentType == ECommons.GameHelpers.ContentType.FieldOperations
		&& Territory?.ContentType == TerritoryContentType.SaveTheQueen;

	/// <summary>
	/// Determines if the current content is Bozjan Southern Front CE or Zadnor CE.
	/// </summary>
	public static bool IsInBozjanFieldOpCE => IsInBozjanFieldOp
		&& StatusHelper.PlayerHasStatus(false, StatusID.DutiesAsAssigned);

	/// <summary>
	/// Determines if the current content is Delubrum Reginae.
	/// </summary>
	public static bool IsInDelubrumNormal => Content.ContentType == ECommons.GameHelpers.ContentType.FieldRaid
		&& Territory?.ContentType == TerritoryContentType.SaveTheQueen;

	/// <summary>
	/// Determines if the current content is Delubrum Reginae (Savage).
	/// </summary>
	public static bool IsInDelubrumSavage => Content.ContentType == ECommons.GameHelpers.ContentType.FieldRaid
		&& Content.ContentDifficulty == ContentDifficulty.FieldRaidsSavage
		&& Territory?.ContentType == TerritoryContentType.SaveTheQueen;

	/// <summary>
	/// Determines if the current territory is Bozja and is either a field operation or field raid.
	/// </summary>
	public static bool IsInBozja => IsInBozjanFieldOp || IsInDelubrumNormal || IsInDelubrumSavage;
	#endregion

	#region Occult Crescent
	/// <summary>
	/// Determines if the current content is Occult
	/// </summary>
	public static bool IsInOccultCrescentOp => Content.ContentType == ECommons.GameHelpers.ContentType.FieldOperations
		&& Territory?.ContentType == TerritoryContentType.OccultCrescent;

	/// <summary>
	/// Determines if the current content is Forked Tower.
	/// </summary>
	public static bool IsInForkedTower => IsInOccultCrescentOp
		&& StatusHelper.PlayerHasStatus(false, StatusID.DutiesAsAssigned_4228);
	#endregion

	#region Variant Dungeon
	/// <summary>
	/// 
	/// </summary>
	public static bool TheMerchantsTaleAdvanced => IsInTerritory(1316);

	/// <summary>
	/// 
	/// </summary>
	public static bool TheMerchantsTale => IsInTerritory(1315);

	/// <summary>
	/// 
	/// </summary>
	public static bool SildihnSubterrane => IsInTerritory(1069);

	/// <summary>
	/// 
	/// </summary>
	public static bool MountRokkon => IsInTerritory(1137);

	/// <summary>
	/// 
	/// </summary>
	public static bool AloaloIsland => IsInTerritory(1176);

	/// <summary>
	/// 
	/// </summary>
	public static bool InVariantDungeon => TheMerchantsTaleAdvanced || TheMerchantsTale || AloaloIsland || MountRokkon || SildihnSubterrane;
	#endregion

	#region Misc Duty Info

	/// <summary>
	///
	/// </summary>
	public static bool IsInEurekaFieldOp => Territory?.ContentType == TerritoryContentType.Eureka;

	/// <summary>
	///
	/// </summary>
	public static bool IsInDeepDungeons => Territory?.ContentType == TerritoryContentType.DeepDungeons;

	/// <summary>
	/// 
	/// </summary>
	public static bool IsInPilgrimsTraverse => IsInTerritory(1281)
	|| IsInTerritory(1282) || IsInTerritory(1283)
	|| IsInTerritory(1284) || IsInTerritory(1285)
	|| IsInTerritory(1286) || IsInTerritory(1287)
	|| IsInTerritory(1288) || IsInTerritory(1289)
	|| IsInTerritory(1290);

	/// <summary>
	/// 
	/// </summary>
	public static bool IsInPalaceOfTheDead => IsInTerritory(561)
	|| IsInTerritory(562) || IsInTerritory(563)
	|| IsInTerritory(564) || IsInTerritory(565)
	|| IsInTerritory(593) || IsInTerritory(594)
	|| IsInTerritory(595) || IsInTerritory(596)
	|| IsInTerritory(597) || IsInTerritory(598)
	|| IsInTerritory(599) || IsInTerritory(600)
	|| IsInTerritory(601) || IsInTerritory(602)
	|| IsInTerritory(603) || IsInTerritory(604)
	|| IsInTerritory(605) || IsInTerritory(606)
	|| IsInTerritory(607);

	/// <summary>
	/// 
	/// </summary>
	public static bool IsInTheFinalVerse => IsInTerritory(1311) || IsInTerritory(1333);

	/// <summary>
	/// Determines if the current content is a Monster Hunter duty
	/// </summary>
	public static bool IsInMonsterHunterDuty => RathalosNormal || RathalosEX || ArkveldNormal || ArkveldEX;

	/// <summary>
	/// 
	/// </summary>
	public static bool RathalosNormal => IsInTerritory(761);

	/// <summary>
	/// 
	/// </summary>
	public static bool RathalosEX => IsInTerritory(762);

	/// <summary>
	/// 
	/// </summary>
	public static bool ArkveldNormal => IsInTerritory(1300);

	/// <summary>
	/// 
	/// </summary>
	public static bool ArkveldEX => IsInTerritory(1306);

	/// <summary>
	/// 
	/// </summary>
	public static bool Orbonne => IsInTerritory(826);

	/// <summary>
	/// 
	/// </summary>
	public static bool Emanation => IsInTerritory(719);

	/// <summary>
	/// 
	/// </summary>
	public static bool EmanationEX => IsInTerritory(720);
	#endregion

	#region Job Info
	public static Job Job => Player.Job;

	private static readonly BaseItem PhoenixDownItem = new(4570);
	public static bool CanRaise()
	{
		if (IsPvP)
		{
			return false;
		}

		if (Service.Config.UsePhoenixDown && PhoenixDownItem.HasIt)
		{
			return true;
		}

		if ((Role == JobRole.Healer || Job == Job.SMN) && PlayerSyncedLevel() >= 12)
		{
			return true;
		}

		if (Job == Job.RDM && PlayerSyncedLevel() >= 64)
		{
			return true;
		}

		if (DutyRotation.ChemistLevel >= 3)
		{
			return true;
		}

		if (StatusHelper.PlayerHasStatus(false, StatusID.VariantRaiseSet))
		{
			return true;
		}
		return false;
	}

	public static JobRole Role
	{
		get
		{
			if (CurrentRotation is BlueMageRotation)
			{
				return BlueMageRotation.Role;
			}

			ClassJob classJob = Service.GetSheet<ClassJob>().GetRow((uint)Job);
			return classJob.RowId != 0 ? classJob.GetJobRole() : JobRole.None;
		}
	}

	public static float JobRange
	{
		get
		{
			float radius = 25;
			if (!Player.Available)
			{
				return radius;
			}

			switch (Role)
			{
				case JobRole.Tank:
				case JobRole.Melee:
					radius = 3;
					break;
			}

			return radius;
		}
	}

	/// <summary>
	/// This quest is needed to do the quests that give Job Stones.
	/// </summary>
	public static unsafe bool SylphManagementFinished()
	{
		if (UIState.Instance()->IsUnlockLinkUnlockedOrQuestCompleted(66049))
			return true;

		return false;
	}

	/// <summary>
	/// Returns true if the current class is a base class (pre-jobstone), otherwise false.
	/// </summary>
	public static bool BaseClass()
	{
		// FFXIV base classes: 1-7, 26, 29 (GLA, PGL, MRD, LNC, ARC, CNJ, THM, ACN, ROG)
		if (Svc.Objects.LocalPlayer == null) return false;
		var rowId = Svc.Objects.LocalPlayer.ClassJob.RowId;
		return (rowId >= 1 && rowId <= 7) || rowId == 26 || rowId == 29;
	}
	#endregion

	#region GCD
	/// <summary>
	/// Returns the current animation lock remaining time (seconds).
	/// </summary>
	public static float AnimationLock => Player.AnimationLock;

	/// <summary>
	/// Time until the next ability relative to the next GCD window.
	/// Non-negative (clamped to 0).
	/// </summary>
	public static float NextAbilityToNextGCD => Math.Max(0f, DefaultGCDRemain - AnimationLock);

	/// <summary>
	/// Returns the total duration of the default GCD (seconds). Clamped to non-negative.
	/// </summary>
	public static float DefaultGCDTotal => Math.Max(0f, ActionManagerHelper.GetDefaultRecastTime());

	/// <summary>
	/// Returns the remaining time for the default GCD by subtracting the elapsed time from the total recast time.
	/// Clamped to non-negative.
	/// </summary>
	public static float DefaultGCDRemain => Math.Max(0f, DefaultGCDTotal - DefaultGCDElapsed);

	/// <summary>
	/// Returns the elapsed time since the start of the default GCD. Clamped to non-negative.
	/// </summary>
	public static float DefaultGCDElapsed => Math.Max(0f, ActionManagerHelper.GetDefaultRecastTimeElapsed());

	/// <summary>
	/// Calculates the action ahead time based on the default GCD total and configured multiplier.
	/// Result is clamped to non-negative.
	/// </summary>
	public static float CalculatedActionAhead => Math.Max(0f, DefaultGCDTotal * Service.Config.Action6Head);

	/// <summary>
	/// Calculates the total GCD time for a given number of GCDs and an optional offset.
	/// </summary>
	/// <param name="gcdCount">The number of GCDs.</param>
	/// <param name="offset">The optional offset.</param>
	/// <returns>The total GCD time.</returns>
	public static float GCDTime(uint gcdCount = 0, float offset = 0)
	{
		return (DefaultGCDTotal * gcdCount) + offset;
	}
	#endregion

	#region Pet Tracking
	public static bool HasPet()
	{
		return Svc.Buddies.PetBuddy != null;
	}

	public static unsafe bool HasCompanion
	{
		get
		{
			BattleChara* playerBattleChara = Player.BattleChara;
			if (playerBattleChara == null)
			{
				return false;
			}

			CharacterManager* characterManager = CharacterManager.Instance();
			if (characterManager == null)
			{
				return false;
			}

			BattleChara* companion = characterManager->LookupBuddyByOwnerObject(playerBattleChara);
			return (IntPtr)companion != IntPtr.Zero;
		}
	}

	public static unsafe BattleChara* GetCompanion()
	{
		BattleChara* playerBattleChara = Player.BattleChara;
		if (playerBattleChara == null)
		{
			return null;
		}

		CharacterManager* characterManager = CharacterManager.Instance();
		return characterManager == null ? (BattleChara*)null : characterManager->LookupBuddyByOwnerObject(playerBattleChara);
	}

	/// <summary>
	/// Gets the players pet
	/// </summary>
	/// <returns>IBattleChara? pet</returns>
	public static IBattleChara? GetPet()
	{
		return Svc.Buddies.PetBuddy?.GameObject as IBattleChara;
	}

	/// <summary>
	/// The distance from <paramref name="battleChara"/> to the player's pet
	/// </summary>
	/// <param name="battleChara"></param>
	/// <returns></returns>
	public static float DistanceToPet(IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return float.MaxValue;
		}
		var pet = GetPet();
		if (pet == null)
		{
			return float.MaxValue;
		}

		return Vector3.Distance(pet.Position, battleChara.Position) - (battleChara.HitboxRadius);
	}
	#endregion

	#region HP

	public static Dictionary<ulong, float> RefinedHP
	{
		get
		{
			Dictionary<ulong, float> refinedHP = [];
			foreach (IBattleChara member in PartyMembers)
			{
				try
				{
					if (member == null || member.GameObjectId == 0)
					{
						continue; // Skip invalid or null members
					}

					refinedHP[member.GameObjectId] = GetPartyMemberHPRatio(member);
				}
				catch (AccessViolationException ex)
				{
					PluginLog.Error($"AccessViolationException in RefinedHP: {ex.Message}");
					continue; // Skip problematic members
				}
			}
			return refinedHP;
		}
	}

	private static readonly Dictionary<ulong, uint> _lastHp = [];

	private static float GetPartyMemberHPRatio(IBattleChara member)
	{
		ArgumentNullException.ThrowIfNull(member);

		if (member.MaxHp == 0) return 0f;

		if (!InEffectTime || !HealHP.TryGetValue(member.GameObjectId, out uint healedHp))
		{
			return (float)member.CurrentHp / member.MaxHp;
		}

		uint currentHp = member.CurrentHp;
		if (currentHp > 0)
		{
			_ = _lastHp.TryGetValue(member.GameObjectId, out uint lastHp);

			if (currentHp - lastHp == healedHp)
			{
				_ = HealHP.Remove(member.GameObjectId);
				return (float)currentHp / member.MaxHp;
			}

			return Math.Min(1, (healedHp + currentHp) / (float)member.MaxHp);
		}

		return (float)currentHp / member.MaxHp;
	}

	private static int _partyHpCacheFrame = -1;
	private static float _partyMinHp = 0;
	private static float _partyAvgHp = 0;
	private static float _partyStdDevHp = 0;
	private static int _partyHpCount = 0;
	private static float _lowestPartyAvgHp = 0;
	private static float _lowestPartyStdDevHp = 0;

	private static readonly float[] _hpBuffer = new float[8];
	private static void UpdatePartyHpCache()
	{
		int currentFrame = Environment.TickCount;
		if (_partyHpCacheFrame == currentFrame)
			return;

		int hpCount = 0;
		foreach (var member in PartyMembers)
		{
			if (member.GameObjectId != 0 && hpCount < _hpBuffer.Length)
			{
				try
				{
					float hp = GetPartyMemberHPRatio(member);
					if (hp > 0) _hpBuffer[hpCount++] = hp;
				}
				catch (AccessViolationException ex)
				{
					PluginLog.Error($"AccessViolationException in Party HP cache: {ex.Message}");
				}
			}
		}

		_partyHpCount = hpCount;
		if (hpCount == 0)
		{
			_partyMinHp = 0;
			_partyAvgHp = 0;
			_partyStdDevHp = 0;
			_lowestPartyAvgHp = 0;
			_lowestPartyStdDevHp = 0;
			return;
		}

		// If there are more than 4 players, we order the array
		if (hpCount > 4)
		{
			Array.Sort(_hpBuffer);
		}

		float sum = 0;
		float lowestHpMembersSum = 0;
		float min = float.MaxValue;
		for (int i = 0; i < hpCount; i++)
		{
			sum += _hpBuffer[i];
			if (i < 4) lowestHpMembersSum += _hpBuffer[i];
			if (_hpBuffer[i] < min) min = _hpBuffer[i];
		}

		float avg = sum / hpCount;
		float lowestHpMembersAvg = lowestHpMembersSum / (hpCount > 4 ? 4 : hpCount);
		float variance = 0;
		float lowestHpMembersVariance = 0;
		for (int i = 0; i < hpCount; i++)
		{
			float diff = _hpBuffer[i] - avg;
			variance += diff * diff;
			if (i < 4)
			{
				float lowestHpMembersDiff = _hpBuffer[i] - lowestHpMembersAvg;
				lowestHpMembersVariance += lowestHpMembersDiff * lowestHpMembersDiff;
			}
		}

		_partyMinHp = min;
		_partyAvgHp = avg;
		_partyStdDevHp = (float)Math.Sqrt(variance / hpCount);
		_lowestPartyAvgHp = lowestHpMembersAvg;
		_lowestPartyStdDevHp = (float)Math.Sqrt(lowestHpMembersVariance / (hpCount > 4 ? 4 : hpCount));
		_partyHpCacheFrame = currentFrame;
	}

	public static float PartyMembersMinHP
	{
		get { UpdatePartyHpCache(); return _partyMinHp; }
	}

	public static float PartyMembersAverHP
	{
		get { UpdatePartyHpCache(); return _partyAvgHp; }
	}

	public static float PartyMembersDifferHP
	{
		get { UpdatePartyHpCache(); return _partyStdDevHp; }
	}

	public static float LowestPartyMembersAverHP
	{
		get { UpdatePartyHpCache(); return _lowestPartyAvgHp; }
	}

	public static float LowestPartyMembersDifferHP
	{
		get { UpdatePartyHpCache(); return _lowestPartyStdDevHp; }
	}

	public static IEnumerable<float> PartyMembersHP
	{
		get
		{
			UpdatePartyHpCache();
			// Return a snapshot of the current frame's HPs
			if (_partyHpCount == 0) yield break;

			var hpList = new List<float>();
			foreach (var member in PartyMembers)
			{
				try
				{
					if (member == null || member.GameObjectId == 0) continue;
					float hp = GetPartyMemberHPRatio(member);
					if (hp > 0) hpList.Add(hp);
				}
				catch (AccessViolationException ex)
				{
					PluginLog.Error($"AccessViolationException in PartyMembersHP: {ex.Message}");
				}
			}

			foreach (var hp in hpList)
			{
				yield return hp;
			}
		}
	}

	public static bool HPNotFull => PartyMembersMinHP < 1;

	public static uint CurrentMp => Player.Object != null ? Math.Min(10000, Player.Object.CurrentMp + MPGain) : MPGain;
	#endregion

	#region Action Record
	private const int QUEUECAPACITY = 48;
	private static readonly Queue<ActionRec> _actions = new(QUEUECAPACITY);
	private static readonly Queue<DamageRec> _damages = new(QUEUECAPACITY);

	internal static CombatRole? BluRole => CurrentRotation is BlueMageRotation ? BlueMageRotation.BlueId : null;
	public static float DPSTaken
	{
		get
		{
			try
			{
				List<DamageRec> recs = [];
				foreach (DamageRec rec in _damages)
				{
					if (DateTime.Now - rec.ReceiveTime < TimeSpan.FromMilliseconds(5))
					{
						recs.Add(rec);
					}
				}

				if (recs.Count == 0)
				{
					return 0;
				}

				float damages = 0;
				for (int i = 0; i < recs.Count; i++)
				{
					damages += recs[i].Ratio;
				}
				DateTime first = recs[0].ReceiveTime;
				DateTime last = recs[^1].ReceiveTime;
				TimeSpan time = last - first + TimeSpan.FromMilliseconds(2.5f);

				return damages / (float)time.TotalSeconds;
			}
			catch
			{
				return 0;
			}
		}
	}

	public static ActionRec[] RecordActions
	{
		get
		{
			ActionRec[] arr = new ActionRec[_actions.Count];
			int i = _actions.Count - 1;
			foreach (ActionRec rec in _actions)
			{
				arr[i--] = rec;
			}
			return arr;
		}
	}
	private static DateTime _timeLastActionUsed = DateTime.Now;
	public static TimeSpan TimeSinceLastAction => DateTime.Now - _timeLastActionUsed;

	public static ActionID LastAction { get; private set; } = 0;

	public static ActionID LastGCD { get; private set; } = 0;

	public static ActionID LastAbility { get; private set; } = 0;

	internal static unsafe void AddActionRec(Action act)
	{
		ActionID id = (ActionID)act.RowId;

		//Record
		switch (act.GetActionCate())
		{
			case ActionCate.Spell:
			case ActionCate.Weaponskill:
				LastAction = LastGCD = id;
				break;
			case ActionCate.Ability:
				LastAction = LastAbility = id;
				break;
			default:
				return;
		}

		if (_actions.Count >= QUEUECAPACITY)
		{
			_ = _actions.Dequeue();
		}

		_timeLastActionUsed = DateTime.Now;
		_actions.Enqueue(new ActionRec(_timeLastActionUsed, act));
	}

	internal static void ResetAllRecords()
	{
		LastAction = 0;
		LastGCD = 0;
		LastAbility = 0;
		_avgTTK = 0;
		_timeLastActionUsed = DateTime.Now;
		_actions.Clear();

		AttackedTargets.Clear();
		while (VfxDataQueue.TryDequeue(out _)) { }
		AllHostileTargets.Clear();
		AllianceMembers.Clear();
		PartyMembers.Clear();
		AllTargets.Clear();
		TargetsByRange.Clear();
	}

	internal static void AddDamageRec(float damageRatio)
	{
		if (_damages.Count >= QUEUECAPACITY)
		{
			_ = _damages.Dequeue();
		}

		_damages.Enqueue(new DamageRec(DateTime.Now, damageRatio));
	}

	internal static DateTime KnockbackFinished { get; set; } = DateTime.MinValue;
	internal static DateTime KnockbackStart { get; set; } = DateTime.MinValue;

	#endregion

	#region Hostile Range
	public static bool HasHostilesInRange => NumberOfHostilesInRange > 0;
	public static bool HasHostilesInMaxRange => NumberOfHostilesInMaxRange > 0;
	public static int NumberOfHostilesInRange
	{
		get
		{
			float jobRange = JobRange;
			var targets = AllHostileTargets;
			int count = 0;
			for (int i = 0, n = targets.Count; i < n; i++)
			{
				if (targets[i].DistanceToPlayer() < jobRange)
				{
					count++;
				}
			}
			return count;
		}
	}
	public static int NumberOfHostilesInMaxRange
	{
		get
		{
			var targets = AllHostileTargets;
			int count = 0;
			for (int i = 0, n = targets.Count; i < n; i++)
			{
				if (targets[i].DistanceToPlayer() < 25)
				{
					count++;
				}
			}
			return count;
		}
	}
	public static int NumberOfHostilesInRangeOf(float range)
	{
		var targets = AllHostileTargets;
		int count = 0;
		for (int i = 0, n = targets.Count; i < n; i++)
		{
			if (targets[i].DistanceToPlayer() < range)
			{
				count++;
			}
		}
		return count;
	}

	public static int NumberOfPartyMembersInRangeOf(float range)
	{
		var targets = PartyMembers;
		int count = 0;
		for (int i = 0, n = targets.Count; i < n; i++)
		{
			if (targets[i].DistanceToPlayer() < range)
			{
				count++;
			}
		}
		return count;
	}
	public static int NumberOfAllHostilesInRange => NumberOfHostilesInRange;
	public static int NumberOfAllHostilesInMaxRange => NumberOfHostilesInMaxRange;
	#endregion

	#region Hostile Casting

	/// <summary>
	/// Determines whether any currently casting hostile action is classified as magical.
	/// </summary>
	/// <returns>
	/// True if at least one hostile target is casting an action whose <c>AttackType.RowId == 5</c> (interpreted as magical); otherwise false.
	/// </returns>
	/// <remarks>
	/// Scans all hostile entities with a non-zero <c>CastActionId</c>, looks up the action row, and inspects the attack type.
	/// Returns early on the first confirmed magical cast.
	/// If the action sheet cannot be loaded or no valid casts exist, returns false.
	/// </remarks>
	public static bool IsMagicalDamageIncoming()
	{
		var hostileEnum = AllHostileTargets;
		if (hostileEnum == null) return false;

		var actionSheet = Service.GetSheet<Action>();
		if (actionSheet == null) return false;

		for (int i = 0, n = hostileEnum.Count; i < n; i++)
		{
			var hostile = hostileEnum[i];
			if (hostile == null) continue;

			try
			{
				if (hostile.CastActionId == 0) continue;

				var action = actionSheet.GetRow(hostile.CastActionId);
				if (action.RowId == 0) continue;

				// AttackType row id 5 interpreted as magical.
				if (action.AttackType.RowId == 5)
				{
					return true;
				}
			}
			catch (AccessViolationException ex)
			{
				PluginLog.Warning($"AccessViolation in IsMagicalDamageIncoming for obj {hostile?.GameObjectId}: {ex.Message}");
			}
		}
		return false;
	}

	/// <summary>
	/// Determines whether any currently casting hostile action is classified as physical.
	/// </summary>
	/// <returns>
	/// True if at least one hostile target is casting an action whose <c>AttackType.RowId == 7</c> (interpreted as physical); otherwise false.
	/// </returns>
	/// <remarks>
	/// Scans all hostile entities with a non-zero <c>CastActionId</c>, looks up the action row, and inspects the attack type.
	/// Returns early on the first confirmed magical cast.
	/// If the action sheet cannot be loaded or no valid casts exist, returns false.
	/// </remarks>
	public static bool IsPhysicalDamageIncoming()
	{
		var hostileEnum = AllHostileTargets;
		if (hostileEnum == null) return false;

		var actionSheet = Service.GetSheet<Action>();
		if (actionSheet == null) return false;

		for (int i = 0, n = hostileEnum.Count; i < n; i++)
		{
			var hostile = hostileEnum[i];
			if (hostile == null) continue;

			try
			{
				if (hostile.CastActionId == 0) continue;

				var action = actionSheet.GetRow(hostile.CastActionId);
				if (action.RowId == 0) continue;

				// AttackType row id 7 interpreted as physical.
				if (action.AttackType.RowId == 7)
				{
					return true;
				}
			}
			catch (AccessViolationException ex)
			{
				PluginLog.Warning($"AccessViolation in IsPhysicalDamageIncoming for obj {hostile?.GameObjectId}: {ex.Message}");
			}
		}
		return false;
	}

	/// <summary>
	/// True if any hostile is currently casting action 46553 or 46554.
	/// </summary>
	public static bool IsExtremeCastingSpecialIndicator()
	{
		if (IsInM10S)
		{
			var hostileEnum = AllHostileTargets;
			if (hostileEnum == null) return false;

			for (int i = 0, n = hostileEnum.Count; i < n; i++)
			{
				var hostile = hostileEnum[i];
				if (hostile == null) continue;

				try
				{
					if (hostile.CastActionId == 46553 || hostile.CastActionId == 46554)
					{
						return true;
					}
				}
				catch (AccessViolationException ex)
				{
					PluginLog.Warning($"AccessViolation in IsHostileCastingSpecialIndicator for obj {hostile?.GameObjectId}: {ex.Message}");
				}
			}
		}

		return false;
	}

	/// <summary>
	/// True if any hostile is currently casting action 46553 or 46554.
	/// </summary>
	public static bool IsTyrantCastingSpecialIndicator()
	{
		if (IsInM11S)
		{
			var hostileEnum = AllHostileTargets;
			if (hostileEnum == null) return false;

			for (int i = 0, n = hostileEnum.Count; i < n; i++)
			{
				var hostile = hostileEnum[i];
				if (hostile == null) continue;

				try
				{
					if (hostile.CastActionId == 46117)
					{
						return true;
					}
				}
				catch (AccessViolationException ex)
				{
					PluginLog.Warning($"AccessViolation in IsTyrantCastingSpecialIndicator for obj {hostile?.GameObjectId}: {ex.Message}");
				}
			}
		}

		return false;
	}

	/// <summary>
	/// 
	/// </summary>
	public static bool IsTyrantCastingSpecialIndicator2()
	{
		if (!IsInM11S)
		{
			// Not in the relevant duty, ensure flags are cleared
			_hasCastScythe = false;
			_hasCastAxe = false;
			_wasCastingCharyb = false;
			_tyrantShouldStopHealing = false;
			return false;
		}

		// If combat hasn't started, reset everything
		if (CombatTimeRaw == 0)
		{
			_hasCastScythe = false;
			_hasCastAxe = false;
			_wasCastingCharyb = false;
			_tyrantShouldStopHealing = false;
			return false;
		}

		var hostileEnum = AllHostileTargets;
		if (hostileEnum == null) return false;

		bool anyCurrentlyCastingCharyb = false;

		for (int i = 0, n = hostileEnum.Count; i < n; i++)
		{
			var hostile = hostileEnum[i];
			if (hostile == null) continue;

			try
			{
				if (!hostile.IsCasting)
				{
					anyCurrentlyCastingCharyb = false;
					continue;
				}

				var castId = hostile.CastActionId;
				if (castId == 46115)
				{
					_hasCastScythe = true;
					continue;
				}
				else if (castId == 46114)
				{
					_hasCastAxe = true;
					continue;
				}
				else if (castId == 46117)
				{
					anyCurrentlyCastingCharyb = true;
					_wasCastingCharyb = true;
					continue;
				}
			}
			catch (AccessViolationException ex)
			{
				PluginLog.Warning($"AccessViolation in IsTyrantCastingSpecialIndicator for obj {hostile?.GameObjectId}: {ex.Message}");
			}
		}

		// If we've observed both scythe and axe, flip the stop-healing flag
		if (_hasCastScythe && _hasCastAxe)
		{
			_tyrantShouldStopHealing = true;
		}

		// If Charybdistopia was casting and now finished, clear everything
		if (_wasCastingCharyb && !anyCurrentlyCastingCharyb)
		{
			_hasCastScythe = false;
			_hasCastAxe = false;
			_wasCastingCharyb = false;
			_tyrantShouldStopHealing = false;
		}

		return _tyrantShouldStopHealing;
	}

	/// <summary>
	/// 
	/// </summary>
	public static bool IsAgriasCastingSpecialIndicator()
	{
		if (!Orbonne)
		{
			return false;
		}

		var hostileEnum = AllHostileTargets;
		if (hostileEnum == null) return false;

		for (int i = 0, n = hostileEnum.Count; i < n; i++)
		{
			var hostile = hostileEnum[i];
			if (hostile == null) continue;

			try
			{
				// Ensure the hostile is actually casting
				if (!hostile.IsCasting) continue;

				// We're only interested in this specific cast id
				if (hostile.CastActionId != 14423) continue;

				// Remaining cast time is exposed as CurrentCastTime (units consistent with other checks)
				float remaining = hostile.TotalCastTime - hostile.CurrentCastTime;

				// If the remaining cast time is less than or equal to the player's remaining GCD,
				// trigger as close to the last second as possible.
				if (remaining <= 2.5f)
				{
					if (Service.Config.InDebug)
					{
						PluginLog.Debug($"Agrias cast detected on obj {hostile.GameObjectId} - remaining: {remaining:F3}s, GCD remain: {DefaultGCDRemain:F3}s");
					}
					return true;
				}
			}
			catch (AccessViolationException ex)
			{
				PluginLog.Warning($"AccessViolation in IsHostileCastingSpecialIndicator for obj {hostile?.GameObjectId}: {ex.Message}");
			}
		}

		return false;
	}

	/// <summary>
	/// 
	/// </summary>
	public static bool IsLakshmiCastingSpecialIndicator()
	{
		if (!EmanationEX && !Emanation)
		{
			return false;
		}

		var hostileEnum = AllHostileTargets;
		if (hostileEnum == null) return false;

		for (int i = 0, n = hostileEnum.Count; i < n; i++)
		{
			var hostile = hostileEnum[i];
			if (hostile == null) continue;

			try
			{
				// Ensure the hostile is actually casting
				if (!hostile.IsCasting) continue;

				// We're only interested in a specific set of Lakshmi cast ids
				// Known special casts (from duty):
				// Stotram - 8519 (Raidwide DOT)
				// Divine Denial - 8521 (Raidwide knockback)
				// Divine Doubt - 8522 (Raidwide confuse)
				// Divine Desire - 8523 (Raidwide pull and bleed)
				// The Path of Light - 8539 (high damage when Chanchala buffed)
				// The Pull of Light - 8543 (high damage when Chanchala buffed)
				uint castId = hostile.CastActionId;

				if (EmanationEX)
				{
					if (hostile.HasStatus(false, StatusID.Chanchala_1410))
					{
						if (castId != 8519 && castId != 8521 && castId != 8522 && castId != 8523 && castId != 8539 && castId != 8543)
							continue;
					}
					if (!hostile.HasStatus(false, StatusID.Chanchala_1410))
					{
						if (castId != 8519 && castId != 8521 && castId != 8522 && castId != 8523)
							continue;
					}
				}

				if (Emanation)
				{
					if (castId != 9349)
						continue;
				}

				// Remaining cast time is exposed as CurrentCastTime (units consistent with other checks)
				float remaining = hostile.TotalCastTime - hostile.CurrentCastTime;

				// If the remaining cast time is less than or equal to the player's remaining GCD,
				// trigger as close to the last second as possible.
				if (remaining <= 3f)
				{
					if (Service.Config.InDebug)
					{
						PluginLog.Debug($"Lakshmi cast detected on obj {hostile.GameObjectId} - remaining: {remaining:F3}s, GCD remain: {DefaultGCDRemain:F3}s");
					}
					return true;
				}
			}
			catch (AccessViolationException ex)
			{
				PluginLog.Warning($"AccessViolation in IsHostileCastingSpecialIndicator for obj {hostile?.GameObjectId}: {ex.Message}");
			}
		}

		return false;
	}

	// Cached, case-insensitive path sets modeled after WrathCombo VFX.cs
	private static readonly FrozenSet<string> TankbusterPaths = FrozenSet.ToFrozenSet(
	[
		"vfx/lockon/eff/tank_lockon",
		"vfx/lockon/eff/tank_laser",
		"vfx/lockon/eff/x6fe_fan100_50_0t1",     // Necron Blue Shockwave - Cone Tankbuster
		//"vfx/common/eff/mon_eisyo03t",           // M10 Deep Impact AoE TB need different path for this, this is the generic target vfx part
		"vfx/lockon/eff/m0676trg_tw_d0t1p",      // M10 Hot Impact shared TB
		"vfx/lockon/eff/m0676trg_tw_s6_d0t1p",   // M11 Raw Steel
		"vfx/lockon/eff/z6r2b3_8sec_lockon_c0a1",// Kam'lanaut Princely Blow
		"vfx/lockon/eff/m0742trg_b1t1",          // M7 Abominable Blink
		"vfx/lockon/eff/x6r9_tank_lockonae"      // M9 Hardcore Large TB
	], StringComparer.OrdinalIgnoreCase);

	private static readonly FrozenSet<string> MultiHitSharedPaths = FrozenSet.ToFrozenSet(
	[
		"vfx/lockon/eff/com_share4a1",
		"vfx/lockon/eff/com_share5a1",
		"vfx/lockon/eff/com_share6m7s_1v",
		"vfx/lockon/eff/com_share8s_0v",
		"vfx/lockon/eff/share_laser_5s_c0w",     // Line
		"vfx/lockon/eff/share_laser_8s_c0g",     // Line
		"vfx/lockon/eff/m0922trg_t2w"
	], StringComparer.OrdinalIgnoreCase);

	private static readonly FrozenSet<string> SharedDamagePaths = FrozenSet.ToFrozenSet(
	[
		"vfx/lockon/eff/coshare",
		"vfx/lockon/eff/share_laser",
		"vfx/lockon/eff/com_share",
		// Duty-specific AOE share markers
		"vfx/monster/gimmick2/eff/z3o7_b1_g06c0t", // Puppet's Bunker, Superior Flight Unit.
		"vfx/monster/gimmick4/eff/z5r1_b4_g09c0c"  // Aglaia, Nald'thal
	], StringComparer.OrdinalIgnoreCase);

	private static readonly StringComparison PathCmp = StringComparison.OrdinalIgnoreCase;

	public static bool IsHostileCastingAOE =>
		InCombat && (IsCastingAreaVfx() || (AllHostileTargets != null && IsAnyHostileCastingArea()));

	private static bool IsAnyHostileCastingArea()
	{
		if (AllHostileTargets == null)
		{
			return false;
		}

		for (int i = 0; i < AllHostileTargets.Count; i++)
		{
			if (IsHostileCastingArea(AllHostileTargets[i]))
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsHostileCastingToTank =>
		InCombat && (IsCastingTankVfx() || (AllHostileTargets != null && IsAnyHostileCastingTank()));

	private static bool IsAnyHostileCastingTank()
	{
		if (AllHostileTargets == null)
		{
			return false;
		}

		for (int i = 0; i < AllHostileTargets.Count; i++)
		{
			if (IsHostileCastingTank(AllHostileTargets[i]))
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsHostileCastingStop =>
		InCombat && Service.Config.CastingStop && AllHostileTargets != null && IsAnyHostileStop();

	private static bool IsAnyHostileStop()
	{
		if (AllHostileTargets == null)
		{
			return false;
		}

		for (int i = 0; i < AllHostileTargets.Count; i++)
		{
			if (IsHostileStop(AllHostileTargets[i]))
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsHostileStop(IBattleChara h)
	{
		return IsHostileCastingStopBase(h,
			(act) =>
			{
				if (act.RowId == 0) return false;
				foreach (var id in OtherConfiguration.HostileCastingStop)
				{
					if (id == act.RowId) return true;
				}
				return false;
			});
	}

	public static bool IsHostileCastingStopBase(IBattleChara h, Func<Action, bool> check)
	{
		if (h == null || check == null)
		{
			return false;
		}

		try
		{
			if (h.GameObjectId == 0)
			{
				return false;
			}

			// Check if the hostile character is casting
			if (!h.IsCasting)
			{
				return false;
			}

			// Check if the cast is interruptible
			if (h.IsCastInterruptible)
			{
				return false;
			}

			// Validate the cast time
			if ((h.TotalCastTime - h.CurrentCastTime) > (Service.Config.CastingStopCalculate ? 100 : Service.Config.CastingStopTime))
			{
				return false;
			}

			// Get the action sheet
			Lumina.Excel.ExcelSheet<Action> actionSheet = Service.GetSheet<Action>();
			if (actionSheet == null)
			{
				return false; // Check if actionSheet is null
			}

			// Get the action being cast
			Action action = actionSheet.GetRow(h.CastActionId);
			if (action.RowId == 0)
			{
				return false; // Check if action is not initialized
			}

			// Invoke the check function on the action and return the result
			return check(action);
		}
		catch (AccessViolationException ex)
		{
			PluginLog.Warning($"AccessViolation in IsHostileCastingStopBase for obj {h?.GameObjectId}: {ex.Message}");
			return false;
		}
	}

	public static bool IsCastingVfx(ConcurrentQueue<VfxNewData> vfxData, Func<VfxNewData, bool> isVfx)
	{
		if (vfxData == null || vfxData.IsEmpty)
		{
			return false;
		}

		foreach (var data in vfxData)
		{
			if (isVfx(data))
			{
				return true;
			}
		}
		return false;
	}

	// Improved multi-hit detection using a cached set of known multi-hit share paths
	public static bool IsCastingMultiHit()
	{
		return IsCastingVfx(VfxDataQueue, s =>
		{
			if (!Player.Available || Player.Object == null)
			{
				return false;
			}

			if (string.IsNullOrEmpty(s.Path))
			{
				return false;
			}

			// Any path in multi-hit share list qualifies
			foreach (var p in MultiHitSharedPaths)
			{
				if (s.Path.StartsWith(p, PathCmp))
				{
					return true;
				}
			}

			return false;
		});
	}

	public static bool IsCastingTankVfx()
	{
		// Populate TankbusterTargets from the VFX queue and return whether any tankbuster VFX was found.
		lock (_tankbusterLock)
		{
			TankbusterTargets.Clear();
			if (!Player.Available || Player.Object == null) return false;

			if (VfxDataQueue == null || VfxDataQueue.IsEmpty) return false;

			bool found = false;
			bool isTank = TargetFilter.PlayerJobCategory(JobRole.Tank);

			foreach (var s in VfxDataQueue)
			{
				try
				{
					if (string.IsNullOrEmpty(s.Path)) continue;

					foreach (var p in TankbusterPaths)
					{
						if (!s.Path.StartsWith(p, PathCmp)) continue;

						bool isPlayerTarget = s.ObjectId == Player.Object.GameObjectId;

						if (!isTank || isPlayerTarget)
						{
							if (Service.Config.InDebug)
							{
								PluginLog.Debug($"Tank lock-on VFX triggered: {s.Path}, ObjectId: {s.ObjectId}");
							}

							// Try to resolve the object id to a party/alliance member and add to the list
							if (Svc.Objects.SearchById(s.ObjectId) is IBattleChara obj && obj.IsParty() && !obj.IsDead && !TankbusterTargets.Contains(obj))
							{
								TankbusterTargets.Add(obj);
							}

							found = true;
						}
					}
				}
				catch (AccessViolationException ex)
				{
					PluginLog.Warning($"AccessViolation in IsCastingTankVfx while scanning VFX: {ex.Message}");
				}
			}

			return found;
		}
	}

	// Improved shared AOE detection using cached sets, covers both regular and multi-hit stack markers
	public static bool IsCastingAreaVfx()
	{
		return IsCastingVfx(VfxDataQueue, s =>
		{
			if (!Player.Available || Player.Object == null)
			{
				return false;
			}

			if (string.IsNullOrEmpty(s.Path))
			{
				return false;
			}

			// Multi-hit markers (treated as area/stack mechanics)
			foreach (var p in MultiHitSharedPaths)
			{
				if (s.Path.StartsWith(p, PathCmp))
				{
					return true;
				}
			}

			// Regular shared markers
			foreach (var p in SharedDamagePaths)
			{
				if (s.Path.StartsWith(p, PathCmp))
				{
					return true;
				}
			}

			return false;
		});
	}

	public static bool IsHostileCastingTank(IBattleChara h)
	{
		return h != null && IsHostileCastingBase(h, (act) =>
		{
			foreach (var id in OtherConfiguration.HostileCastingTank)
			{
				if (id == act.RowId) return true;
			}
			return h.CastTargetObjectId == h.TargetObjectId;
		});
	}

	public static bool IsHostileCastingArea(IBattleChara h)
	{
		return IsHostileCastingBase(h, (act) =>
		{
			foreach (var id in OtherConfiguration.HostileCastingArea)
			{
				if (id == act.RowId) return true;
			}
			return false;
		});
	}

	public static bool AreHostilesCastingKnockback
	{
		get
		{
			var targets = AllHostileTargets;
			for (int i = 0, n = targets.Count; i < n; i++)
			{
				var h = targets[i];
				try
				{
					if (IsHostileCastingKnockback(h))
					{
						return true;
					}
				}
				catch (AccessViolationException ex)
				{
					PluginLog.Warning($"AccessViolation when checking knockback for obj {h?.GameObjectId}: {ex.Message}");
				}
			}
			return false;
		}
	}

	public static bool IsHostileCastingKnockback(IBattleChara h)
	{
		return IsHostileCastingBase(h,
			(act) =>
			{
				if (act.RowId == 0) return false;
				foreach (var id in OtherConfiguration.HostileCastingKnockback)
				{
					if (id == act.RowId) return true;
				}
				return false;
			});
	}

	public static bool IsHostileCastingBase(IBattleChara h, Func<Action, bool> check)
	{
		if (h == null || check == null)
		{
			return false;
		}

		unsafe
		{
			if (h.Struct() == null)
			{
				return false;
			}
		}

		try
		{
			if (h.GameObjectId == 0)
			{
				return false;
			}

			if (!h.IsEnemy())
			{
				return false;
			}

			if (!h.IsCasting)
			{
				return false;
			}

			// Check if the cast is interruptible
			if (h.IsCastInterruptible)
			{
				return false;
			}

			// Calculate the time since the cast started
			float last = h.TotalCastTime - h.CurrentCastTime;
			float t = last - DefaultGCDTotal;

			// Check if the total cast time is greater than the minimum cast time and if the calculated time is within a valid range
			if (!(h.TotalCastTime > DefaultGCDTotal && t > 0 && t < GCDTime(1)))
			{
				return false;
			}

			// Get the action sheet
			Lumina.Excel.ExcelSheet<Action> actionSheet = Service.GetSheet<Action>();
			if (actionSheet == null)
			{
				PluginLog.Error("IsHostileCastingBase: Action sheet is null.");
				return false;
			}

			// Get the action being cast
			Action action = actionSheet.GetRow(h.CastActionId);
			if (action.RowId == 0)
			{
				PluginLog.Error("IsHostileCastingBase: Action is not initialized.");
				return false;
			}

			// Invoke the check function on the action and return the result
			return check(action);
		}
		catch (AccessViolationException ex)
		{
			PluginLog.Warning($"AccessViolation in IsHostileCastingBase for obj {h?.GameObjectId}: {ex.Message}");
			return false;
		}
	}
	#endregion

	#region BossModReborn Timeline Integration

	public static bool BMREndabled
	{
		get
		{
			string name = "BossModReborn";
			IEnumerable<Dalamud.Plugin.IExposedPlugin> installedPlugins = Svc.PluginInterface.InstalledPlugins;
			foreach (Dalamud.Plugin.IExposedPlugin x in installedPlugins)
			{
				if ((x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || x.InternalName.Equals(name, StringComparison.OrdinalIgnoreCase)) && x.IsLoaded)
				{
					return true;
				}
			}
			return false;
		}
	}

	public static bool BMRHasActiveModule { get; set; }
	public static string? BMRActiveModuleName { get; set; }
	public static float BMRNextRaidwideIn { get; set; } = float.MaxValue;
	public static float BMRNextTankbusterIn { get; set; } = float.MaxValue;
	public static float BMRNextKnockbackIn { get; set; } = float.MaxValue;
	public static float BMRNextDowntimeIn { get; set; } = float.MaxValue;
	public static float BMRNextDowntimeEndIn { get; set; } = float.MaxValue;
	public static float BMRNextVulnerableIn { get; set; } = float.MaxValue;
	public static float BMRNextVulnerableEndIn { get; set; } = float.MaxValue;
	public static float BMRNextDamageIn { get; set; } = float.MaxValue;
	public static PredictedDamageType BMRNextDamageType { get; set; } = PredictedDamageType.None;
	public static float BMRSpecialModeIn { get; set; } = float.MaxValue;
	public static SpecialMode BMRSpecialModeType { get; set; } = SpecialMode.Normal;

	// Debug diagnostics
	public static float BMRDebugTimelineRaidwide { get; set; } = float.MaxValue;
	public static float BMRDebugTimelineTankbuster { get; set; } = float.MaxValue;
	public static float BMRDebugHintsRaidwide { get; set; } = float.MaxValue;
	public static float BMRDebugHintsTankbuster { get; set; } = float.MaxValue;
	public static float BMRDebugGenericDamageIn { get; set; } = float.MaxValue;
	public static int BMRDebugGenericDamageType { get; set; }
	public static bool BMRDebugTimelineRwFunc { get; set; }
	public static bool BMRDebugTimelineTbFunc { get; set; }
	public static bool BMRDebugHintsRwFunc { get; set; }
	public static bool BMRDebugHintsTbFunc { get; set; }
	public static string? BMRDebugTimelineWalk { get; set; }
	public static void ResetBmrData()
	{
		BMRHasActiveModule = false;
		BMRActiveModuleName = null;
		BMRNextRaidwideIn = float.MaxValue;
		BMRNextTankbusterIn = float.MaxValue;
		BMRNextKnockbackIn = float.MaxValue;
		BMRNextDowntimeIn = float.MaxValue;
		BMRNextDowntimeEndIn = float.MaxValue;
		BMRNextVulnerableIn = float.MaxValue;
		BMRNextVulnerableEndIn = float.MaxValue;
		BMRNextDamageIn = float.MaxValue;
		BMRNextDamageType = 0;
		BMRSpecialModeIn = float.MaxValue;
		BMRSpecialModeType = 0;
		BMRDebugTimelineRaidwide = float.MaxValue;
		BMRDebugTimelineTankbuster = float.MaxValue;
		BMRDebugHintsRaidwide = float.MaxValue;
		BMRDebugHintsTankbuster = float.MaxValue;
		BMRDebugGenericDamageIn = float.MaxValue;
		BMRDebugGenericDamageType = 0;
		BMRDebugTimelineRwFunc = false;
		BMRDebugTimelineTbFunc = false;
		BMRDebugHintsRwFunc = false;
		BMRDebugHintsTbFunc = false;
		BMRDebugTimelineWalk = null;
	}
	#endregion
}