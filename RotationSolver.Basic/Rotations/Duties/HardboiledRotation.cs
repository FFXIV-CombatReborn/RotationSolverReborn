namespace RotationSolver.Basic.Rotations.Duties
{
	/// <summary>
	///
	/// </summary>
	[DutyTerritory(1329)]
	public abstract class HardboiledRotation : DutyRotation
	{
		/// <inheritdoc/>
		protected override uint[] QuestBattleActionIds =>
		[
			(uint)ActionID.ShootHardPvE,
			(uint)ActionID.ShootHarderPvE,
			(uint)ActionID.SmokingGunPvE,
			(uint)ActionID.PulpFissionPvE,
			(uint)ActionID.MoltingPythonPvE,
			(uint)ActionID.NightStallionPvE,
			(uint)ActionID.ThickSkinPvE,
			(uint)ActionID.TheShortGoodbyePvE,
		];
	}

	public partial class DutyRotation
	{
		// Relevant statuses, FissionChamber, ElectrifiedGentlemen, Hardboiled, GuiltyAsCharged

		/// <summary>
		/// 
		/// </summary>
		public static bool HasFissionChamber => StatusHelper.PlayerHasStatus(true, StatusID.FissionChamber);

		/// <summary>
		///
		/// </summary>
		public static bool HasGuiltyAsCharged => StatusHelper.PlayerHasStatus(true, StatusID.GuiltyAsCharged);

		static partial void ModifyShootHardPvE(ref ActionSetting setting)
		{
			setting.IsFriendly = false;
			setting.IsQuestBattleAction = true;
			setting.CreateConfig = () => new ActionConfig()
			{
				AoeCount = 1,
			};
		}

		static partial void ModifyShootHarderPvE(ref ActionSetting setting)
		{
			setting.IsFriendly = false;
			setting.IsQuestBattleAction = true;
			setting.CreateConfig = () => new ActionConfig()
			{
				AoeCount = 1,
			};
		}

		static partial void ModifySmokingGunPvE(ref ActionSetting setting)
		{
			setting.IsFriendly = false;
			setting.IsQuestBattleAction = true;
			setting.CreateConfig = () => new ActionConfig()
			{
				AoeCount = 1,
			};
		}

		static partial void ModifyPulpFissionPvE(ref ActionSetting setting)
		{
			setting.ActionCheck = () => HasFissionChamber || HasGuiltyAsCharged;
			setting.IsFriendly = false;
			setting.IsQuestBattleAction = true;
			setting.StatusNeed = [StatusID.FissionChamber, StatusID.GuiltyAsCharged];
			setting.MPOverride = () => 0;
			setting.CreateConfig = () => new ActionConfig()
			{
				AoeCount = 1,
			};
		}

		static partial void ModifyMoltingPythonPvE(ref ActionSetting setting)
		{
			setting.IsFriendly = false;
			setting.IsQuestBattleAction = true;
			setting.CreateConfig = () => new ActionConfig()
			{
				AoeCount = 1,
			};
		}

		static partial void ModifyNightStallionPvE(ref ActionSetting setting)
		{
			setting.IsFriendly = false;
			setting.IsQuestBattleAction = true;
			setting.CreateConfig = () => new ActionConfig()
			{
				AoeCount = 1,
			};
		}

		static partial void ModifyThickSkinPvE(ref ActionSetting setting)
		{
			setting.IsFriendly = true;
			setting.IsQuestBattleAction = true;
			setting.StatusProvide = [StatusID.Hardboiled];
			setting.TargetType = TargetType.Self;
			setting.CreateConfig = () => new ActionConfig()
			{
				AoeCount = 1,
			};
		}

		static partial void ModifyTheShortGoodbyePvE(ref ActionSetting setting)
		{
			setting.IsFriendly = false;
			setting.IsQuestBattleAction = true;
			setting.CreateConfig = () => new ActionConfig()
			{
				AoeCount = 1,
			};
		}

	}
}