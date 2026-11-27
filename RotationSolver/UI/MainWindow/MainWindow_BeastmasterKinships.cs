using Dalamud.Interface.Utility.Raii;
using RebornMaterial;
using RotationSolver.Integrations.IPC;
using RotationSolver.Rotations.Limited.Beastmaster;

namespace RotationSolver.UI;

public partial class MainWindow
{
	private static readonly Dictionary<int, bool> _kinshipBoardExpanded = [];
	private static readonly Dictionary<uint, uint> _kinshipBoardTerritories = [];

	private static readonly Dictionary<BeastmasterKinType, (string Name, ActionID Action)> _kinshipDisplay = new()
	{
		[BeastmasterKinType.Beastkin] = ("Beastkin", ActionID.BeastskinPvE),
		[BeastmasterKinType.Vilekin] = ("Vilekin", ActionID.VileskinPvE),
		[BeastmasterKinType.Cloudkin] = ("Cloudkin", ActionID.CloudSkimPvE),
		[BeastmasterKinType.Seedkin] = ("Seedkin", ActionID.SeedsowerPvE),
		[BeastmasterKinType.Wavekin] = ("Wavekin", ActionID.QuellingWavePvE),
		[BeastmasterKinType.Scalekin] = ("Scalekin", ActionID.ScaleskinPvE),
		[BeastmasterKinType.Soulkin] = ("Soulkin", ActionID.SoulCrushPvE),
		[BeastmasterKinType.Ashkin] = ("Ashkin", ActionID.ScouringAshPvE),
	};

	private static void DrawBeastmasterPreferredKinships()
	{
		M3Widgets.Divider(6f);
		M3Text.Draw("Preferred kinships", M3TextStyle.Title);
		M3Text.Draw("Which familiar kinships help against each Crucible of the Unbroken opponent, taken from BossModReborn's modules. Bring familiars of these kin types when you pick your team.",
			M3TextStyle.Muted, wrap: true);
		ImGui.Spacing();

		if (!BMRCrucible_IPCSubscriber.IsEnabled)
		{
			M3Text.Draw("Load BossModReborn to see them.", M3TextStyle.Faint, wrap: true);
			return;
		}

		var opponents = DataCenter.BMRCrucibleOpponents;
		if (opponents.Count == 0)
		{
			M3Text.Draw("BossModReborn did not send a kinship list. Update BossModReborn to see them.", M3TextStyle.Faint, wrap: true);
			return;
		}

		var activeModule = DataCenter.BMRActiveModuleType;
		BMRCrucibleOpponent? current = null;
		if (activeModule != null)
		{
			foreach (var opponent in opponents)
			{
				if (opponent.ModuleType == activeModule)
				{
					current = opponent;
					break;
				}
			}
		}

		if (current != null)
		{
			M3Text.Draw($"Current opponent: {current.Opponent}", M3TextStyle.Accent);
			DrawKinshipTable("bst_kinship_current", [current], current, false);
			ImGui.Spacing();
		}

		List<BMRCrucibleOpponent> sorted = [.. opponents];
		sorted.Sort(static (a, b) => a.BoardOrder != b.BoardOrder ? a.BoardOrder.CompareTo(b.BoardOrder) : a.SortOrder.CompareTo(b.SortOrder));

		var start = 0;
		while (start < sorted.Count)
		{
			var end = start;
			while (end < sorted.Count && sorted[end].BoardOrder == sorted[start].BoardOrder)
			{
				end++;
			}

			var board = sorted.GetRange(start, end - start);
			var first = board[0];
			var isCurrentBoard = (current != null && current.BoardOrder == first.BoardOrder) || IsInKinshipBoard(first.GroupID);
			if (!_kinshipBoardExpanded.TryGetValue(first.BoardOrder, out var expanded))
			{
				expanded = isCurrentBoard;
			}

			var needed = 0;
			foreach (var opponent in board)
			{
				if (opponent.Kinships.Count > 0)
				{
					needed++;
				}
			}

			using (var card = M3ExpandableCard.Begin($"bst_kinship_board_{first.BoardOrder}", first.Board, ref expanded, FontAwesomeIcon.ChessBoard,
				M3.AccentFor(first.BoardOrder), isCurrentBoard ? "You are here" : null, M3.Scheme.Primary, $"{needed}/{board.Count} need a kinship"))
			{
				if (card.Expanded)
				{
					DrawKinshipTable($"bst_kinship_table_{first.BoardOrder}", board, current, true);
				}
			}

			_kinshipBoardExpanded[first.BoardOrder] = expanded;
			start = end;
		}
	}

	private static void DrawKinshipTable(string id, List<BMRCrucibleOpponent> opponents, BMRCrucibleOpponent? current, bool showOpponent)
	{
		var columns = showOpponent ? 3 : 2;
		using var table = ImRaii.Table(id, columns, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH);
		if (!table)
		{
			return;
		}

		if (showOpponent)
		{
			ImGui.TableSetupColumn("Opponent", ImGuiTableColumnFlags.WidthStretch, 1.2f);
		}

		ImGui.TableSetupColumn("Kinship", ImGuiTableColumnFlags.WidthStretch, 0.8f);
		ImGui.TableSetupColumn("Why", ImGuiTableColumnFlags.WidthStretch, 2.2f);
		ImGui.TableHeadersRow();

		var iconSize = ImGui.GetTextLineHeight();
		var highlight = M3.U32(M3.Alpha(M3.Scheme.Primary, 0.14f));

		foreach (var opponent in opponents)
		{
			var isCurrent = showOpponent && current != null && opponent.ModuleType == current.ModuleType;
			var rows = Math.Max(1, opponent.Kinships.Count);
			for (var i = 0; i < rows; i++)
			{
				ImGui.TableNextRow();
				if (isCurrent)
				{
					ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, highlight);
				}

				if (showOpponent)
				{
					_ = ImGui.TableNextColumn();
					if (i == 0)
					{
						M3Text.Draw(opponent.Opponent, isCurrent ? M3TextStyle.Accent : M3TextStyle.Body);
					}
				}

				if (opponent.Kinships.Count == 0)
				{
					_ = ImGui.TableNextColumn();
					M3Text.Draw("Any", M3TextStyle.Faint);
					_ = ImGui.TableNextColumn();
					M3Text.Draw("No mechanic calls for a specific kinship.", M3TextStyle.Faint, wrap: true);
					continue;
				}

				var kinship = opponent.Kinships[i];
				var kinType = (BeastmasterKinType)kinship.Kinship;
				var name = _kinshipDisplay.TryGetValue(kinType, out var display) ? display.Name : kinType.ToString();

				_ = ImGui.TableNextColumn();
				if (display.Action != ActionID.None && display.Action.GetTexture(out var texture) && texture is { Handle.IsNull: false })
				{
					ImGui.Image(texture.Handle, new Vector2(iconSize));
					ImguiTooltips.HoveredTooltip(display.Action.ToString().Replace("PvE", string.Empty));
					ImGui.SameLine();
				}

				M3Text.Draw(name);

				_ = ImGui.TableNextColumn();
				M3Text.Draw(kinship.Reason, M3TextStyle.Muted, wrap: true);
			}
		}
	}

	private static bool IsInKinshipBoard(uint contentFinderId)
	{
		if (!_kinshipBoardTerritories.TryGetValue(contentFinderId, out var territory))
		{
			territory = Service.GetSheet<Lumina.Excel.Sheets.ContentFinderCondition>().GetRowOrDefault(contentFinderId)?.TerritoryType.RowId ?? 0;
			_kinshipBoardTerritories[contentFinderId] = territory;
		}

		return territory != 0 && territory == DataCenter.TerritoryID;
	}
}
