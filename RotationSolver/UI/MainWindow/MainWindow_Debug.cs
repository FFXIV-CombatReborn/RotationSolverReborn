using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using RebornMaterial;
using RotationSolver.Basic.Configuration;
using RotationSolver.Basic.Rotations.Duties;
using RotationSolver.Helpers;
using RotationSolver.IPC;
using RotationSolver.Updaters;
using System.Diagnostics;
using System.Text;

namespace RotationSolver.UI;

public partial class MainWindow
{
	private static void DrawDebug()
	{
		_allSearchable.DrawItems(Configs.Debug);

		{
			var tracePath = ActionTracer.CurrentFilePath;
			var hasFile = !string.IsNullOrEmpty(tracePath) && File.Exists(tracePath);
			var hasAnyData = hasFile
				|| !string.IsNullOrEmpty(ActionTracer.LastFrameSummary)
				|| ActionTracer.HasAnyTraceFiles();

			if (!hasFile)
			{
				ImGui.BeginDisabled();
			}
			if (ImGui.Button("Open Action Trace File"))
			{
				try
				{
					_ = Process.Start("explorer.exe", $"\"{tracePath}\"");
				}
				catch (Exception ex)
				{
					PluginLog.Warning($"Failed to open trace file: {ex.Message}");
				}
			}
			if (!hasFile)
			{
				ImGui.EndDisabled();
			}
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip(hasFile
					? tracePath
					: "No trace file yet — enable the tracer and enter combat to create one.");
			}

			ImGui.SameLine();

			if (!hasAnyData)
			{
				ImGui.BeginDisabled();
			}
			if (ImGui.Button("Clear Trace"))
			{
				ActionTracer.ClearTrace();
			}
			if (!hasAnyData)
			{
				ImGui.EndDisabled();
			}
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip("Delete every actiontrace_*.log file in the Traces folder and clear the buffered last-frame data.");
			}
		}

		if (!Player.Available || !Service.Config.InDebug)
		{
			return;
		}

		_debugHeader?.Draw();

		if (ImGui.Button("Reset Action Configs"))
		{
			DataCenter.ResetActionConfigs = DataCenter.ResetActionConfigs != true;
		}
		ImGui.Text($"Reset Action Configs: {DataCenter.ResetActionConfigs}");
		if (ImGui.Button("Add Test Warning"))
		{
			BasicWarningHelper.AddSystemWarning("This is a test warning.");
		}
	}

	private static readonly CollapsingHeaderGroup _debugHeader = BuildDebugHeaderGroup();

	private static CollapsingHeaderGroup BuildDebugHeaderGroup()
	{
		var group = new CollapsingHeaderGroup(new()
	{
		{() => DataCenter.CurrentRotation != null ? "Loaded Rotation Info" : string.Empty, DrawDebugRotationStatus},
		{() => DataCenter.CurrentRotation != null ? "Base Rotation Info" : string.Empty, DrawDebugBaseStatus},
		{() => "Player Status", DrawStatus },
		{() => "Raise Info", DrawRaiseInfo },
		{() => "Duty Info", DrawDutyInfo },
		{() => "Party", DrawParty },
		{() => "Target Data", DrawTargetData },
		{() => "Next Action", DrawNextAction },
		{() => "Last Action", DrawLastAction },
		{() => "IPC Testing", DrawIPC },
		{() => "BMR Data", DrawBMRData },
		{() => "Occult Crescent Weaknesses", DrawOccultWeaknesses },

		{() => "Effect", () =>
			{
				ImGui.Text(Watcher.ShowStrSelf);
				ImGui.Separator();
				ImGui.Text(DataCenter.Role.ToString());
			} },
		{() => "Material 3 Gallery", M3Gallery.Draw },
	});

		group.SetHeaderIcon("Loaded Rotation Info", FontAwesomeIcon.Sync);
		group.SetHeaderIcon("Base Rotation Info", FontAwesomeIcon.Cube);
		group.SetHeaderIcon("Player Status", FontAwesomeIcon.User);
		group.SetHeaderIcon("Raise Info", FontAwesomeIcon.Ankh);
		group.SetHeaderIcon("Duty Info", FontAwesomeIcon.Dungeon);
		group.SetHeaderIcon("Party", FontAwesomeIcon.Users);
		group.SetHeaderIcon("Target Data", FontAwesomeIcon.Crosshairs);
		group.SetHeaderIcon("Next Action", FontAwesomeIcon.StepForward);
		group.SetHeaderIcon("Last Action", FontAwesomeIcon.StepBackward);
		group.SetHeaderIcon("IPC Testing", FontAwesomeIcon.PlugCircleBolt);
		group.SetHeaderIcon("BMR Data", FontAwesomeIcon.ChartLine);
		group.SetHeaderIcon("Occult Crescent Weaknesses", FontAwesomeIcon.Moon);
		group.SetHeaderIcon("Effect", FontAwesomeIcon.Magic);
		group.SetHeaderIcon("Material 3 Gallery", FontAwesomeIcon.Palette);

		return group;
	}

	private static void DrawDebugRotationStatus()
	{
		DataCenter.CurrentRotation?.DisplayRotationStatus();
	}

	private static void DrawOccultWeaknesses()
	{
		ImGui.TextWrapped("Records the elemental weaknesses (Lightning, Fire, Ice, Wind) observed on hostiles " +
			"encountered in Occult Crescent, keyed by their NameId. This is populated automatically while in " +
			"Occult Crescent.");

		if (ImGui.Button("Open Weakness Data Folder"))
		{
			try
			{
				var path = Svc.PluginInterface.ConfigDirectory.FullName;
				_ = Process.Start("explorer.exe", $"\"{path}\"");
			}
			catch (Exception ex)
			{
				PluginLog.Warning($"Failed to open weakness data folder: {ex.Message}");
			}
		}
		ImGui.SameLine();
		if (ImGui.Button("Clear Weakness Data"))
		{
			OtherConfiguration.ResetOccultWeaknessRecords();
		}
		ImGui.SameLine();
		if (ImGui.Button("Copy as Curated List Entries"))
		{
			var sb = new StringBuilder();
			void AppendEntries(Dictionary<uint, List<string>> records)
			{
				foreach (var kvp in records)
				{
					var statusesSb = new StringBuilder();
					for (var i = 0; i < kvp.Value.Count; i++)
					{
						if (i > 0)
						{
							_ = statusesSb.Append(", ");
						}
						_ = statusesSb.Append("StatusID.").Append(kvp.Value[i]);
					}
					_ = sb.AppendLine($"\t\t{{ {kvp.Key}, [{statusesSb}] }},");
				}
			}
			sb.AppendLine("// North Horn");
			AppendEntries(OtherConfiguration.NorthHornWeaknessRecords);
			sb.AppendLine("// South Horn");
			AppendEntries(OtherConfiguration.SouthHornWeaknessRecords);
			ImGui.SetClipboardText(sb.ToString());
		}
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip("Copies each recorded NameId/weakness.");
		}

		using var table = ImRaii.Table("OccultWeaknessTable", 4,
			ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp,
			new Vector2(0, 200 * Scale));
		if (table)
		{
			ImGui.TableSetupScrollFreeze(0, 1);
			ImGui.TableSetupColumn("Zone");
			ImGui.TableSetupColumn("NameId");
			ImGui.TableSetupColumn("Name");
			ImGui.TableSetupColumn("Weaknesses");
			ImGui.TableHeadersRow();

			void DrawRows(string zoneName, Dictionary<uint, List<string>> records)
			{
				foreach (var kvp in records)
				{
					ImGui.TableNextRow();
					_ = ImGui.TableNextColumn();
					ImGui.TextUnformatted(zoneName);
					_ = ImGui.TableNextColumn();
					ImGui.TextUnformatted(kvp.Key.ToString());
					_ = ImGui.TableNextColumn();
					var npcName = string.Empty;
					try
					{
						npcName = Service.GetSheet<Lumina.Excel.Sheets.BNpcName>().GetRow(kvp.Key).Singular.ToString();
					}
					catch { }
					ImGui.TextUnformatted(npcName);
					_ = ImGui.TableNextColumn();
					ImGui.TextUnformatted(string.Join(", ", kvp.Value));
				}
			}

			DrawRows("North Horn", OtherConfiguration.NorthHornWeaknessRecords);
			DrawRows("South Horn", OtherConfiguration.SouthHornWeaknessRecords);
		}

		ImGui.Spacing();
		ImGui.Separator();
		ImGui.TextWrapped("Hostiles in Range Without Weakness Data");

		var unknownNames = new List<string>();
		var seenNameIds = new HashSet<uint>();
		var hostiles = DataCenter.AllHostileTargets;
		if (hostiles != null)
		{
			for (var i = 0; i < hostiles.Count; i++)
			{
				var hostile = hostiles[i];
				if (hostile == null || hostile.NameId == 0)
				{
					continue;
				}

				if (!seenNameIds.Add(hostile.NameId))
				{
					continue;
				}

				var alreadyRecorded = DataCenter.IsInNorthHorn
					? OtherConfiguration.NorthHornWeaknessRecords.ContainsKey(hostile.NameId)
					: DataCenter.IsInSouthHorn && OtherConfiguration.SouthHornWeaknessRecords.ContainsKey(hostile.NameId);

				if (StatusHelper.HasKnownOccultWeakness(hostile.NameId) || alreadyRecorded)
				{
					continue;
				}

				var npcName = string.Empty;
				try
				{
					npcName = Service.GetSheet<Lumina.Excel.Sheets.BNpcName>().GetRow(hostile.NameId).Singular.ToString();
				}
				catch { }

				unknownNames.Add(string.IsNullOrEmpty(npcName) ? $"NameId {hostile.NameId}" : npcName);
			}
		}

		if (unknownNames.Count == 0)
		{
			ImGui.TextUnformatted("None.");
		}
		else
		{
			ImGui.TextUnformatted(string.Join(", ", unknownNames));
		}
	}

	private static void DrawDebugBaseStatus()
	{
		DataCenter.CurrentRotation?.DisplayBaseStatus();
	}

	private static unsafe void DrawStatus()
	{
		if (Player.Object == null)
		{
			return;
		}
		ImGui.Text($"PlayerSyncedLevel: {DataCenter.PlayerSyncedLevel()}");
		ImGui.Text($"PlayerUnsyncedLevel: {DataCenter.PlayerMaxLevel}");
		ImGui.Text($"Merged Status: {DataCenter.MergedStatus}");
		ImGui.Text($"PlayerHasLockActions: {ActionUpdater.PlayerHasLockActions()}");
		ImGui.Text($"Height: {Player.Character->ModelContainer.CalculateHeight()}");
		ImGui.Text($"AutoFaceTargetOnActionSetting: {DataCenter.AutoFaceTargetOnActionSetting()}");
		ImGui.Text($"MoveModeSetting: {DataCenter.MoveModeSetting()}");
		Dalamud.Game.ClientState.Conditions.ConditionFlag[] conditions = [.. Svc.Condition.AsReadOnlySet()];
		ImGui.Text("InternalCondition:");
		foreach (var condition in conditions)
		{
			ImGui.Text($"    {condition}");
		}
		ImGui.Text($"OnlineStatus: {Player.OnlineStatus.RowId}");
		ImGui.Text($"CanBeRaised: {Player.Object.CanBeRaised()}");
		ImGui.Text($"Current Hp: {Player.Object.CurrentHp}");
		ImGui.Text($"Effective Hp: {ObjectHelper.GetEffectiveHp(Player.Object)}");
		ImGui.Text($"Effective Hp Percent: {ObjectHelper.GetEffectiveHpPercent(Player.Object)}");
		ImGui.Text($"IsDead: {Player.Object.IsDead}");
		ImGui.Text($"DoomNeedHealing: {Player.Object.DoomNeedHealing()}");
		ImGui.Text($"Dead Time: {DataCenter.DeadTimeRaw}");
		ImGui.Text($"Alive Time: {DataCenter.AliveTimeRaw}");
		ImGui.Text($"Moving: {DataCenter.IsMoving}");
		ImGui.Text($"Moving Time: {DataCenter.MovingRaw}");
		ImGui.Text($"Stop Moving: {DataCenter.StopMovingRaw}");
		ImGui.Text($"CountDownTime: {Service.CountDownTime}");
		ImGui.Text($"Combo Time: {DataCenter.ComboTime}");
		ImGui.Text($"TargetingType: {DataCenter.TargetingType}");
		ImGui.Spacing();
		ImGui.Text($"IsHostileCastingToTank: {DataCenter.IsHostileCastingToTank}");
		ImGui.Text($"AttackedTargets: {DataCenter.AttackedTargets?.Count ?? 0}");
		if (DataCenter.AttackedTargets != null)
		{
			foreach ((var id, var time) in DataCenter.AttackedTargets)
			{
				ImGui.Text(id.ToString() ?? "Unknown ID");
			}
		}


		ImGui.Text("Casting Vfx:");
		List<VfxNewData> filteredVfx = [];
		foreach (var s in DataCenter.VfxDataQueue)
		{
			if (s.Path.StartsWith("vfx/lockon/eff/") && s.TimeDuration.TotalSeconds > 0 && s.TimeDuration.TotalSeconds < 6)
			{
				filteredVfx.Add(s);
			}
		}
		foreach (var vfx in filteredVfx)
		{
			ImGui.Text($"Path: {vfx.Path}");
		}

		var partyMembers = DataCenter.PartyMembers;
		if (partyMembers.Count != 0)
		{
			ImGui.Text("Party Members:");
			foreach (var member in partyMembers)
			{
				ImGui.Text($"- {member.Name}");
			}
		}
		else
		{
			ImGui.Text("Party Members: None");
		}

		List<IBattleChara> tankPartyMembers = [];
		foreach (var member in DataCenter.PartyMembers)
		{
			if (member.IsJobCategory(JobRole.Tank))
			{
				tankPartyMembers.Add(member);
			}
		}
		if (tankPartyMembers.Count != 0)
		{
			ImGui.Text("Tank Party Members:");
			foreach (var member in tankPartyMembers)
			{
				ImGui.Text($"- {member.Name}");
			}
		}
		else
		{
			ImGui.Text("Tank Party Members: None");
		}

		var dispelTarget = DataCenter.DispelTarget;
		if (dispelTarget != null)
		{
			ImGui.Text("Dispel Target:");
			ImGui.Text($"- {dispelTarget.Name}");
		}
		else
		{
			ImGui.Text("Dispel Target: None");
		}

		ImGui.Text($"DPSTaken: {DataCenter.DPSTaken}");
		ImGui.Text($"CurrentRotation: {DataCenter.CurrentRotation}");
		ImGui.Text($"Job: {DataCenter.Job}");
		ImGui.Text($"JobRange: {DataCenter.JobRange}");
		ImGui.Text($"Job Role: {DataCenter.Role}");
		ImGui.Text($"Have pet: {DataCenter.HasPet()}");
		ImGui.Text($"Hostile Near Count: {DataCenter.NumberOfHostilesInRange}");
		ImGui.Text($"Hostile Near Count Max Range: {DataCenter.NumberOfHostilesInMaxRange}");
		ImGui.Text($"Have Companion: {DataCenter.HasCompanion}");
		ImGui.Text($"MP: {DataCenter.CurrentMp}");
		ImGui.Text($"Count Down: {Service.CountDownTime}");

		ImGui.Spacing();
		ImGui.Text($"Statuses:");
		using var statusTable = ImRaii.Table("TargetStatusTable", 5,
			ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.ScrollY,
			new Vector2(0, 200 * Scale));
		if (statusTable)
		{
			ImGui.TableSetupScrollFreeze(0, 1);
			ImGui.TableSetupColumn("Name");
			ImGui.TableSetupColumn("ID");
			ImGui.TableSetupColumn("Source");
			ImGui.TableSetupColumn("Stacks");
			ImGui.TableSetupColumn("Time");
			ImGui.TableHeadersRow();

			foreach (var status in Player.Object.StatusList)
			{
				if (Player.Object == null)
				{
					continue;
				}

				var source = status.SourceId == Player.Object.GameObjectId ? "You" : Svc.Objects.SearchById(status.SourceId) == null ? "None" : "Others";
				var stacks = Player.Object.StatusStack(true, (StatusID)status.StatusId);
				var stackDisplay = stacks == byte.MaxValue ? "N/A" : stacks.ToString();
				var timeDisplay = status.RemainingTime <= 0f ? "Perm" : $"{status.RemainingTime:F1}s";

				ImGui.TableNextRow();
				_ = ImGui.TableNextColumn();
				ImGui.TextUnformatted(status.GameData.Value.Name.ToString());
				_ = ImGui.TableNextColumn();
				ImGui.TextUnformatted(status.StatusId.ToString());
				_ = ImGui.TableNextColumn();
				ImGui.TextUnformatted(source);
				_ = ImGui.TableNextColumn();
				ImGui.TextUnformatted(stackDisplay);
				_ = ImGui.TableNextColumn();
				ImGui.TextUnformatted(timeDisplay);
			}
		}
	}

	private static void DrawRaiseInfo()
	{
		ImGui.Text($"Can Raise: {DataCenter.CanRaise()}");
		ImGui.Text($"Death Target: {DataCenter.DeathTarget}");

		var deadPartyMembersList = new List<IBattleChara>();
		foreach (var member in DataCenter.PartyMembers.GetDeath())
		{
			deadPartyMembersList.Add(member);
		}

		if (deadPartyMembersList.Count > 0)
		{
			ImGui.Text("Dead Party Members:");
			foreach (var member in deadPartyMembersList)
			{
				ImGui.Text($"- {member.Name}");
			}
		}
		else
		{
			ImGui.Text("Dead Party Members: None");
		}

		var deadAllianceMembersList = new List<IBattleChara>();
		foreach (var member in DataCenter.AllianceMembers.GetDeath())
		{
			deadAllianceMembersList.Add(member);
		}

		if (deadAllianceMembersList.Count > 0)
		{
			ImGui.Text("Dead Alliance Members:");
			foreach (var member in deadAllianceMembersList)
			{
				ImGui.Text($"- {member.Name}");
			}
		}
		else
		{
			ImGui.Text("Dead Alliance Members: None");
		}
	}

	private static unsafe void DrawDutyInfo()
	{
		ImGui.Spacing();
		ImGui.Text($"DC State: {DataCenter.State}");
		ImGui.Text($"Your combat state: {DataCenter.InCombat}");
		ImGui.Text($"Combat Time: {DataCenter.CombatTimeRaw}");
		ImGui.Text($"TerritoryID: {DataCenter.TerritoryID}");
		ImGui.Text($"TerritoryType: {DataCenter.Territory?.ContentType}");
		ImGui.Text($"Is in Alliance Raid: {DataCenter.IsInAllianceRaid}");
		ImGui.Spacing();
		ImGui.Text($"IsPvP: {DataCenter.IsPvP}");
		ImGui.Text($"IsInFate: {DataCenter.IsInFate}");
		if ((IntPtr)FateManager.Instance() != IntPtr.Zero)
		{
			ImGui.Text($"Fate ID: {DataCenter.PlayerFateId}");
		}
		ImGui.Spacing();
		ImGui.Text($"IsInWindurst: {DataCenter.IsInWindurst}");
		ImGui.Spacing();
		ImGui.Text($"In Field Operations: {DataCenter.IsInFieldOperations}");
		ImGui.Text($"In Field Raid: {DataCenter.IsInFieldRaid}");
		ImGui.Spacing();
		if (DataCenter.IsInBozjanFieldOp)
		{
			ImGui.Text($"IsInBozjanFieldOp: {DataCenter.IsInBozjanFieldOp}");
			ImGui.Text($"IsInBozjanFieldOpCE: {DataCenter.IsInBozjanFieldOpCE}");
			ImGui.Text($"IsInDelubrumNormal: {DataCenter.IsInDelubrumNormal}");
			ImGui.Text($"IsInDelubrumSavage: {DataCenter.IsInDelubrumSavage}");
			ImGui.Text($"IsInBozja: {DataCenter.IsInBozja}");
		}
		if (DataCenter.IsInOccultCrescentOp)
		{
			ImGui.Text($"In North Horn: {DataCenter.IsInNorthHorn}");
			ImGui.Text($"In South Horn: {DataCenter.IsInSouthHorn}");
			ImGui.Text($"Is In Forked Tower Blood: {DataCenter.IsInForkedTowerBlood}");
			ImGui.Text($"FreelancerLevel: {DutyRotation.FreelancerLevel}");
			ImGui.Text($"KnightLevel: {DutyRotation.KnightLevel}");
			ImGui.Text($"MonkLevel: {DutyRotation.MonkLevel}");
			ImGui.Text($"BardLevel: {DutyRotation.BardLevel}");
			ImGui.Text($"ChemistLevel: {DutyRotation.ChemistLevel}");
			ImGui.Text($"TimeMageLevel: {DutyRotation.TimeMageLevel}");
			ImGui.Text($"CannoneerLevel: {DutyRotation.CannoneerLevel}");
			ImGui.Text($"OracleLevel: {DutyRotation.OracleLevel}");
			ImGui.Text($"BerserkerLevel: {DutyRotation.BerserkerLevel}");
			ImGui.Text($"RangerLevel: {DutyRotation.RangerLevel}");
			ImGui.Text($"ThiefLevel: {DutyRotation.ThiefLevel}");
			ImGui.Text($"SamuraiLevel: {DutyRotation.SamuraiLevel}");
			ImGui.Text($"GeomancerLevel: {DutyRotation.GeomancerLevel}");
			ImGui.Text($"MysticKnightLevel: {DutyRotation.MysticKnightLevel}");
			ImGui.Text($"DancerLevel: {DutyRotation.DancerLevel}");
			ImGui.Text($"NinjaLevel: {DutyRotation.NinjaLevel}");
			ImGui.Text($"WhiteMageLevel: {DutyRotation.WhiteMageLevel}");
			ImGui.Text($"BlackMageLevel: {DutyRotation.BlackMageLevel}");
			ImGui.Text($"DragoonLevel: {DutyRotation.DragoonLevel}");
			ImGui.Text($"SummonerLevel: {DutyRotation.SummonerLevel}");
			ImGui.Text($"BlueMageLevel: {DutyRotation.BlueMageLevel}");
			ImGui.Text($"RedMageLevel: {DutyRotation.RedMageLevel}");
			ImGui.Text($"NecromancerLevel: {DutyRotation.NecromancerLevel}");
		}
		ImGui.Text($"InVariantDungeon: {DataCenter.InVariantDungeon}");
		ImGui.Text($"The Merchant's Tale Advanced: {DataCenter.TheMerchantsTaleAdvanced}");
		ImGui.Text($"The Merchant's Tale: {DataCenter.TheMerchantsTale}");
		ImGui.Text($"AloaloIsland: {DataCenter.AloaloIsland}");
		ImGui.Text($"MountRokkon: {DataCenter.MountRokkon}");
		ImGui.Text($"SildihnSubterrane: {DataCenter.SildihnSubterrane}");
		ImGui.Spacing();
		ImGui.Text($"AreHostilesCastingKnockback: {DataCenter.AreHostilesCastingKnockback}");
		ImGui.Text($"IsHostileCastingAOE: {DataCenter.IsHostileCastingAOE}");
		ImGui.Text($"IsHostileCastingToTank: {DataCenter.IsHostileCastingToTank}");
		ImGui.Text($"IsHostileCastingStop: {DataCenter.IsHostileCastingStop}");
		ImGui.Spacing();
		ImGui.Text($"IsCastingMultiHit: {DataCenter.IsCastingMultiHit()}");
		ImGui.Text($"IsCastingAreaVfx: {DataCenter.IsCastingAreaVfx()}");
		ImGui.Text($"IsCastingTankVfx: {DataCenter.IsCastingTankVfx()}");
		ImGui.Text($"TankbusterTargets: {DataCenter.TankbusterTargets.Count}");
		ImGui.Spacing();
		ImGui.Text($"IsInM11S: {DataCenter.IsInM11S}");
		ImGui.Text($"IsTyrantCastingSpecialIndicator2: {DataCenter.IsTyrantCastingSpecialIndicator2()}");
		ImGui.Text($"IsLichCastingSpecialIndicator: {DataCenter.IsLichCastingSpecialIndicator()}");
	}

	private static void DrawParty()
	{
		ImGui.Text($"Number of Party Members: {DataCenter.PartyMembers.Count}");
		ImGui.Text($"Number of Alliance Members: {DataCenter.AllianceMembers.Count}");
		ImGui.Text($"Average Party HP Percent: {DataCenter.PartyMembersAverHP * 100}");
		ImGui.Text($"Average Lowest Party HP Percent: {DataCenter.LowestPartyMembersAverHP * 100}");
		var doomedCount = 0;
		foreach (var member in DataCenter.PartyMembers)
		{
			if (member.DoomNeedHealing())
			{
				doomedCount++;
			}
		}
		ImGui.Text($"Number of Party Members with Doomed To Heal status: {doomedCount}");


		if (Player.Object != null && Player.Object.IsJobs(Job.AST))
		{
			var spear = ActionTargetInfo.FindTargetByType(DataCenter.PartyMembers, TargetType.TheSpear, 0, SpecialActionType.None, TargetType.TheSpear, true);
			var balance = ActionTargetInfo.FindTargetByType(DataCenter.PartyMembers, TargetType.TheBalance, 0, SpecialActionType.None, TargetType.TheBalance, true);
			ImGui.Spacing();
			ImGui.Text("AST Card Targets (Preview):");
			ImGui.Text($"- The Spear: {spear?.Name ?? "None"}");
			ImGui.Text($"- The Balance: {balance?.Name ?? "None"}");
			ImGui.Spacing();
		}

		foreach (var p in DataCenter.PartyMembers)
		{
			var text = $"Name: {p.Name}, HP: {p.GetEffectiveHpPercent()}%";

			ImGui.Text(text);
		}

		foreach (var p in Svc.Party)
		{
			if (p.GameObject is not IBattleChara b)
			{
				continue;
			}

			var text = $"Name: {b.Name}, In Combat: {b.InCombat()}";
			if (b.TimeAlive() > 0)
			{
				text += $", Time Alive: {b.TimeAlive()}";
			}

			if (b.TimeDead() > 0)
			{
				text += $", Time Dead: {b.TimeDead()}";
			}

			ImGui.Text(text);
		}
		ImGui.Spacing();
		ImGui.Text($"Limit Break: {CustomRotation.LimitBreakLevel}");
		ImGui.Spacing();
		ImGui.Text($"Object Data");
		ImGui.Text($"NumberOfPartyMembersInRangeOf 5m: {DataCenter.NumberOfPartyMembersInRangeOf(5)}");
		ImGui.Text($"AllTargets Count: {DataCenter.AllTargets.Count}");
		ImGui.Text($"AllHostileTargets Count: {DataCenter.AllHostileTargets.Count}");
		foreach (var item in DataCenter.AllHostileTargets)
		{
			ImGui.Text(item.Name.ToString());
		}
		ImGui.Spacing();
		ImGui.Text($"Party Composition:");
		var party = CustomRotation.PartyComposition;
		if (party.Count == 0)
		{
			ImGui.Text("No party members.");
		}
		else
		{
			for (var i = 0; i < party.Count; i++)
			{
				var classJob = party[i].Value;
				var jobName = classJob.Abbreviation.ToString() ?? classJob.Name.ToString() ?? $"Job #{i}";
				ImGui.Text($"{i + 1}: {jobName}");
			}
		}
		ImGui.Spacing();
		var mitigationFraction = CustomRotation.GetCurrentMitigationPercent();
		ImGui.Text($"Current Mitigation Percent: {mitigationFraction * 100f:F1}%");
		ImGui.Text($"Current Mitigation Percent RAW: {mitigationFraction}");

		ImGui.Text($"Is Magical Damage Incoming: {CustomRotation.IsMagicalDamageIncoming}");
	}

	private static unsafe void DrawTargetData()
	{
		if (Svc.Targets.Target is not IBattleChara target)
		{
			return;
		}

		ImGui.Text($"Height: {target.Struct()->Height}");
		ImGui.Text($"Kind: {target.GetObjectKind()}");
		ImGui.Text($"SubKind: {target.GetBattleNPCSubKind()}");

		var owner = Svc.Objects.SearchById(target.OwnerId);
		if (owner != null)
		{
			ImGui.Text($"Owner: {owner.Name}");
		}

		if (target is IBattleChara battleChara)
		{
			ImGui.Text($"IsCasting: {battleChara.IsCasting}");
			ImGui.Text($"CastID: {battleChara.CastInfo.ActionId}");
			ImGui.Text($"Is Status Capped: {StatusHelper.IsStatusCapped(battleChara)}");
			ImGui.Text($"CanSee: {battleChara.CanSee()}");
			ImGui.Text($"CanBeRaised: {battleChara.CanBeRaised()}");
			ImGui.Text($"HP: {battleChara.CurrentHp} / {battleChara.MaxHp}");
			ImGui.Text($"HealthRatio: {battleChara.GetHealthRatio()}");
			ImGui.Text($"HitboxRadius: {battleChara.HitboxRadius}");
			ImGui.Text($"Distance To Player: {battleChara.DistanceToPlayer()}");
			ImGui.Spacing();
			ImGui.Text($"NamePlate Icon ID: {battleChara.GetNamePlateIcon()}");
			ImGui.Text($"Event Type: {battleChara.GetEventType()}");
			ImGui.Text($"TargetCharaCondition: {battleChara.TargetCharaCondition()}");
			var npcName = string.Empty;
			var npcEnumName = string.Empty;
			if (battleChara.NameId != 0)
			{
				var bnpcName = Service.GetSheet<Lumina.Excel.Sheets.BNpcName>().GetRow(battleChara.NameId);
				npcName = bnpcName.Singular.ToString();

				if (Enum.IsDefined(typeof(NPCName), battleChara.NameId))
				{
					npcEnumName = $"{Enum.GetName(typeof(NPCName), battleChara.NameId)}";
				}
			}
			ImGui.Text($"NPC Name: {npcEnumName}");
			ImGui.Text($"Name Id: {battleChara.NameId}");
			ImGui.Text($"Data Id: {battleChara.BaseId}");
			ImGui.Spacing();
			ImGui.Text($"Is Attackable: {battleChara.IsAttackable()}");
			ImGui.Text($"Is Others Players Mob: {battleChara.IsOthersPlayersMob()}");
			ImGui.Text($"Is Alliance: {battleChara.IsAllianceMember()}");
			ImGui.Text($"Is Enemy Action Check: {battleChara.IsEnemy()}");
			ImGui.Text($"IsSpecialExecptionImmune: {battleChara.IsSpecialExceptionImmune()}");
			ImGui.Text($"IsSpecialImmune: {battleChara.IsSpecialImmune()}");
			ImGui.Text($"IsTopPriorityNamedHostile: {battleChara.IsTopPriorityNamedHostile()}");
			ImGui.Text($"IsTopPriorityHostile: {battleChara.IsTopPriorityHostile()}");
			ImGui.Spacing();
			ImGui.Text($"FateID: {battleChara.FateId().ToString() ?? string.Empty}");
			ImGui.Text($"EventType: {battleChara.GetEventType().ToString() ?? string.Empty}");
			if (DataCenter.IsInBozja)
			{
				ImGui.Text($"IsBozjanCEFateMob: {battleChara.IsBozjanCEMob()}");
			}
			ImGui.Spacing();
			if (DataCenter.IsInOccultCrescentOp)
			{
				ImGui.Text($"IsOccultCEMob: {battleChara.IsOccultCEMob()}");
				ImGui.Text($"IsOccultFateMob: {battleChara.IsOccultFateMob()}");
				ImGui.Text($"IsOCUndeadTarget: {battleChara.IsOCUndeadTarget()}");
				ImGui.Text($"IsOCSlowgaImmuneTarget: {battleChara.IsOCSlowgaImmuneTarget()}");
				ImGui.Text($"IsOCDoomImmuneTarget: {battleChara.IsOCDoomImmuneTarget()}");
				ImGui.Text($"IsOCStunImmuneTarget: {battleChara.IsOCStunImmuneTarget()}");
				ImGui.Text($"IsOCFreezeImmuneTarget: {battleChara.IsOCFreezeImmuneTarget()}");
				ImGui.Text($"IsOCBlindImmuneTarget: {battleChara.IsOCBlindImmuneTarget()}");
				ImGui.Text($"IsOCParalysisImmuneTarget: {battleChara.IsOCParalysisImmuneTarget()}");
				ImGui.Spacing();
			}
			ImGui.Text($"Is Current Focus Target: {battleChara.IsFocusTarget()}");
			ImGui.Text($"TTK: {battleChara.GetTTK()}");
			ImGui.Text($"Is Boss TTK: {battleChara.IsBossFromTTK()}");
			ImGui.Text($"Is Boss Icon: {battleChara.IsBossFromIcon()}");
			ImGui.Text($"Rank: {battleChara.GetObjectNPC()?.Rank.ToString() ?? string.Empty}");
			ImGui.Text($"Has Positional: {battleChara.HasPositional()}");
			ImGui.Text($"IsNpcPartyMember: {battleChara.IsNpcPartyMember()}");
			ImGui.Text($"IsPlayerCharacterChocobo: {battleChara.IsPlayerCharacterChocobo()}");
			ImGui.Text($"IsFriendlyBattleNPC: {battleChara.IsFriendlyBattleNPC()}");
			ImGui.Text($"Is Dying: {battleChara.IsDying()}");
			ImGui.Text($"Is Alive: {battleChara.IsAlive()}");
			ImGui.Text($"Is Party: {battleChara.IsParty()}");
			ImGui.Text($"Is Healer: {battleChara.IsJobCategory(JobRole.Healer)}");
			ImGui.Text($"Is DPS: {battleChara.IsJobCategory(JobRole.AllDPS)}");
			ImGui.Text($"Is Tank: {battleChara.IsJobCategory(JobRole.Tank)}");
			ImGui.Text($"Is Alliance: {battleChara.IsAllianceMember()}");
			ImGui.Text($"CanProvoke: {battleChara.CanProvoke()}");
			ImGui.Text($"StatusFlags: {battleChara.StatusFlags}");
			ImGui.Text($"InView: {Svc.GameGui.WorldToScreen(battleChara.Position, out _)}");
			ImGui.Text($"Enemy Positional: {battleChara.FindEnemyPositional()}");
			ImGui.Text($"NameplateKind: {battleChara.GetNameplateKind()}");
			ImGui.Text($"BattleNPCSubKind: {battleChara.GetBattleNPCSubKind()}");
			ImGui.Text($"Is Top Priority Hostile: {battleChara.IsTopPriorityHostile()}");
			ImGui.Text($"Targetable: {battleChara.Struct()->Character.GameObject.TargetableStatus}");
			if (DataCenter.IsInMaskedCarnivale)
			{
				ImGui.Spacing();
				ImGui.Text($"Aspect Resistance (Fire): {MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Fire)}");
				ImGui.Text($"Aspect Resistance (Ice): {MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Ice)}");
				ImGui.Text($"Aspect Resistance (Wind): {MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Wind)}");
				ImGui.Text($"Aspect Resistance (Earth): {MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Earth)}");
				ImGui.Text($"Aspect Resistance (Lightning): {MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Lightning)}");
				ImGui.Text($"Aspect Resistance (Water): {MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Water)}");
				ImGui.Text($"Aspect Resistance (Slashing): {MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Slashing)}");
				ImGui.Text($"Aspect Resistance (Piercing): {MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Piercing)}");
				ImGui.Text($"Aspect Resistance (Blunt): {MaskedCarnivaleHelper.GetAspectResistance(battleChara, Aspect.Blunt)}");
				ImGui.Spacing();
				ImGui.Text($"IsVulnerableToSlow: {MaskedCarnivaleHelper.IsVulnerableToSlow(battleChara)}");
				ImGui.Text($"IsVulnerableToPetrification: {MaskedCarnivaleHelper.IsVulnerableToPetrification(battleChara)}");
				ImGui.Text($"IsVulnerableToParalysis: {MaskedCarnivaleHelper.IsVulnerableToParalysis(battleChara)}");
				ImGui.Text($"IsVulnerableToInterruption: {MaskedCarnivaleHelper.IsVulnerableToInterruption(battleChara)}");
				ImGui.Text($"IsVulnerableToBlind: {MaskedCarnivaleHelper.IsVulnerableToBlind(battleChara)}");
				ImGui.Text($"IsVulnerableToStun: {MaskedCarnivaleHelper.IsVulnerableToStun(battleChara)}");
				ImGui.Text($"IsVulnerableToSleep: {MaskedCarnivaleHelper.IsVulnerableToSleep(battleChara)}");
				ImGui.Text($"IsVulnerableToBind: {MaskedCarnivaleHelper.IsVulnerableToBind(battleChara)}");
				ImGui.Text($"IsVulnerableToHeavy: {MaskedCarnivaleHelper.IsVulnerableToHeavy(battleChara)}");
				ImGui.Text($"IsVulnerableToFlatOrDeath: {MaskedCarnivaleHelper.IsVulnerableToFlatOrDeath(battleChara)}");
			}
			ImGui.Spacing();
			ImGui.Text($"Statuses:");
			using var statusTable = ImRaii.Table("TargetStatusTable", 5,
				ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.ScrollY,
				new Vector2(0, 200 * Scale));
			if (statusTable)
			{
				ImGui.TableSetupScrollFreeze(0, 1);
				ImGui.TableSetupColumn("Name");
				ImGui.TableSetupColumn("ID");
				ImGui.TableSetupColumn("Source");
				ImGui.TableSetupColumn("Stacks");
				ImGui.TableSetupColumn("Time");
				ImGui.TableHeadersRow();

				foreach (var status in battleChara.StatusList)
				{
					if (Player.Object == null)
					{
						continue;
					}

					var source = status.SourceId == Player.Object.GameObjectId ? "You" : Svc.Objects.SearchById(status.SourceId) == null ? "None" : "Others";
					var stacks = battleChara.StatusStack(true, (StatusID)status.StatusId);
					var stackDisplay = stacks == byte.MaxValue ? "N/A" : stacks.ToString();
					var timeDisplay = status.RemainingTime <= 0f ? "Perm" : $"{status.RemainingTime:F1}s";

					ImGui.TableNextRow();
					_ = ImGui.TableNextColumn();
					ImGui.TextUnformatted(status.GameData.Value.Name.ToString());
					_ = ImGui.TableNextColumn();
					ImGui.TextUnformatted(status.StatusId.ToString());
					_ = ImGui.TableNextColumn();
					ImGui.TextUnformatted(source);
					_ = ImGui.TableNextColumn();
					ImGui.TextUnformatted(stackDisplay);
					_ = ImGui.TableNextColumn();
					ImGui.TextUnformatted(timeDisplay);
				}
			}
		}
	}

	private static void DrawNextAction()
	{
		ImGui.Text(DataCenter.CurrentRotation?.GetAttributes()?.Name);
		ImGui.Text(DataCenter.SpecialType.ToString());

		ImGui.Text(ActionUpdater.NextAction?.Name ?? "null");
		ImGui.Text($"GCD Total: {DataCenter.DefaultGCDTotal}");
		ImGui.Text($"GCD Remain: {DataCenter.DefaultGCDRemain}");
		ImGui.Text($"GCD Elapsed: {DataCenter.DefaultGCDElapsed}");
		ImGui.Text($"Calculated Action Ahead: {DataCenter.CalculatedActionAhead}");
		ImGui.Text($"Animation Lock Delay: {DataCenter.AnimationLock}");
	}

	private static void DrawLastAction()
	{
		DrawAction(DataCenter.LastAction, nameof(DataCenter.LastAction));
		DrawAction(DataCenter.LastAbility, nameof(DataCenter.LastAbility));
		DrawAction(DataCenter.LastGCD, nameof(DataCenter.LastGCD));
		DrawAction(DataCenter.LastComboAction, nameof(DataCenter.LastComboAction));
		ImGui.Text($"IsLastActionAbility: {IActionHelper.IsLastActionAbility()}");
		ImGui.Text($"IsLastActionGCD: {IActionHelper.IsLastActionGCD()}");
	}

	private static string _ipcTestText = "Sent data";

	private static void DrawIPC()
	{
		ImGui.SetNextItemWidth(200 * Scale);
		ImGui.InputText("##IPCTextBox", ref _ipcTestText, 128);
		ImGui.SameLine();
		if (ImGui.Button("Test Function"))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.Test(_ipcTestText);
		}

		if (ImGui.Button("Test ChangeOperatingMode to Manual IPC"))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.ChangeOperatingMode(StateCommandType.Manual);
		}

		if (ImGui.Button("Test ChangeOperatingMode to Off IPC"))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.ChangeOperatingMode(StateCommandType.Off);
		}

		if (ImGui.Button("Test TriggerSpecialState DefenseArea IPC"))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.TriggerSpecialState(SpecialCommandType.DefenseArea);
		}

		if (ImGui.Button("Test TriggerSpecialState AntiKnockback IPC"))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.TriggerSpecialState(SpecialCommandType.AntiKnockback);
		}

		if (ImGui.Button("Test Setting IPC (Changing engage setting to All Target)"))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.OtherCommand(OtherCommandType.Settings, "HostileType AllTargetsCanAttack");
		}

		if (ImGui.Button("Test OtherCommand DoAction IPC (Magick Barrier on RDM)"))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.OtherCommand(OtherCommandType.DoActions, "Magick Barrier-5");
		}

		if (ImGui.Button("Test ToggleAction IPC (Magick Barrier on RDM)"))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.OtherCommand(OtherCommandType.ToggleActions, "Magick Barrier");
		}

		if (ImGui.Button("Test ActionCommand IPC (Magick Barrier on RDM)"))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.ActionCommand("Magick Barrier", 7);
		}
		if (ImGui.Button("Test AutodutyChangeOperatingMode IPC (AutoDuty, HighHPPercent)"))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.AutodutyChangeOperatingMode(StateCommandType.AutoDuty, TargetingType.HighHPPercent);
		}
		if (ImGui.Button("Test ChangeOperatingModeWithOverrides IPC (Auto, AllTargetsCanAttack, TargetFreely on, AutoOffAfterCombat off, NPC heal/raise on)"))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.ChangeOperatingModeWithOverrides(StateCommandType.Auto, TargetHostileType.AllTargetsCanAttack, SettingOverride.ForceOn, SettingOverride.ForceOff, SettingOverride.ForceOn);
		}
		if (ImGui.Button("Test AutodutyChangeOperatingModeWithOverrides IPC (AutoDuty, HighHPPercent, AllTargetsCanAttack, others unchanged)"))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.AutodutyChangeOperatingModeWithOverrides(StateCommandType.AutoDuty, TargetingType.HighHPPercent, TargetHostileType.AllTargetsCanAttack, SettingOverride.UseSetting, SettingOverride.UseSetting, SettingOverride.UseSetting);
		}
		ImGui.Text($"IPC overrides: {DataCenter.IpcOverrides?.ToString() ?? "None"} (Active: {DataCenter.ActiveIpcOverrides is not null})");
		ImGui.Text($"Effective: HostileType {DataCenter.CurrentTargetToHostileType}, TargetFreely {DataCenter.TargetFreelyEnabled}, AutoOffAfterCombat {DataCenter.AutoOffAfterCombatEnabled}, FriendlyPartyNpcHealRaise {DataCenter.FriendlyPartyNpcHealRaiseEnabled}");
		if (ImGui.Button("Test Henchman IPC support"))
		{
			var ipcProvider = RotationSolverPlugin.IPCProvider;
			ipcProvider.ChangeOperatingMode(StateCommandType.Henched);
		}
	}

	private static void DrawBMRData()
	{
		ImGui.Text($"Cooldown Planner IPC Enabled: {BMRPlan_IPCSubscriber.IsEnabled}");
		ImGui.Text($"BMRPlannedActionsCount: {DataCenter.BMRPlannedActions.Count}");
		ImGui.Text($"BMRForceCancelCast: {DataCenter.BMRForceCancelCast}");
		ImGui.Text($"BMRForceCancelCastAI: {DataCenter.BMRForceCancelCastAI}");
		ImGui.Text($"BMRIsMoving: {DataCenter.BMRIsMoving}");
	}

	private static void DrawAction(ActionID id, string type)
	{
		ImGui.Text($"{type}: {id}");
	}
}
