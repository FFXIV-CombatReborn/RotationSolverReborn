using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using ECommons.Logging;
using RotationSolver.Basic.Configuration;

namespace RotationSolver.UI;

internal class EasterEggWindow : Window
{
    private const ImGuiWindowFlags BaseFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoResize;

    private enum Cell { Empty, X, O }
    private readonly Cell[] _board = new Cell[9];
    private bool _playerTurn = true; // Player is X
    private bool _gameOver = false;
    private string _status = "You are X. Click to play.";
    private readonly float _cellSize = 64f;

    private static readonly Random _rng = new();
    private bool _aiBlunderThisGame = false; // 1/1000 chance per match to intentionally blunder once
    private bool _aiBlunderUsed = false;

    public EasterEggWindow() : base("RSR Lab — Tic‑tac‑toe", BaseFlags)
    {
        Size = new Vector2(300, 360);
        SizeCondition = ImGuiCond.FirstUseEver;
        RespectCloseHotkey = true;
    }

    public override bool DrawConditions()
    {
        return Svc.ClientState.IsLoggedIn;
    }

    public override void OnOpen()
    {
        Reset();
        base.OnOpen();
    }

    public override void Draw()
    {
        float scale = ImGui.GetIO().FontGlobalScale;
        float size = _cellSize * scale;

        using var _ = ImRaii.Group();
        // Board 3x3
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                int i = r * 3 + c;
                ImGui.PushID(i);

                Vector2 buttonSize = new(size, size);
                bool clicked = ImGui.Button(RenderCell(_board[i]), buttonSize);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }

                if (clicked && !_gameOver && _playerTurn && _board[i] == Cell.Empty)
                {
                    _board[i] = Cell.X;
                    if (CheckWin(_board, Cell.X))
                    {
                        _gameOver = true;
                        _status = "You win!";
                        try
                        {
                            OtherConfiguration.RotationSolverRecord.TicTacToeWinStar = true;
                            OtherConfiguration.SaveRotationSolverRecord();
                        }
                        catch { /* non-fatal */ }
                    }
                    else if (IsDraw(_board))
                    {
                        _gameOver = true;
                        _status = "Draw.";
                    }
                    else
                    {
                        _playerTurn = false;
                        AIMove();
                    }
                }

                ImGui.PopID();
                if (c < 2) ImGui.SameLine();
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Text("I made this because i was bored");
        ImGui.Spacing();

        using (var __ = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudYellow))
        {
            ImGui.TextWrapped(_status);
        }
        ImGui.Spacing();

        if (ImGui.Button("Reset"))
        {
            Reset();
        }
        ImGui.SameLine();
        if (ImGui.Button("Close"))
        {
            IsOpen = false;
        }
    }

    private static string RenderCell(Cell c)
    {
        return c switch
        {
            Cell.X => "X",
            Cell.O => "O",
            _ => " ",
        };
    }

    private void Reset()
    {
        Array.Fill(_board, Cell.Empty);
        _playerTurn = true;
        _gameOver = false;
        _status = "You are X. Click to play.";
        // 1/1000 chance at the start of each match for the AI to intentionally make one bad move
        _aiBlunderThisGame = _rng.Next(0, 1000) == 0;
        _aiBlunderUsed = false;
    }

    private void AIMove()
    {
        try
        {
            int move;
            if (_aiBlunderThisGame && !_aiBlunderUsed)
            {
                move = FindWorstMoveMinimax();
                if (move >= 0)
                {
                    _aiBlunderUsed = true; // consume the one-time blunder
                }
                else
                {
                    move = FindBestMoveMinimax();
                }
            }
            else
            {
                move = FindBestMoveMinimax();
            }
            if (move < 0)
            {
                // Fallback (should never happen): pick first empty
                move = Array.FindIndex(_board, c => c == Cell.Empty);
            }

            if (move >= 0)
            {
                _board[move] = Cell.O;
            }

            if (CheckWin(_board, Cell.O))
            {
                _gameOver = true;
                _status = "RSR wins!";
            }
            else if (IsDraw(_board))
            {
                _gameOver = true;
                _status = "Draw.";
            }
            else
            {
                _playerTurn = true;
                _status = "Your turn.";
            }
        }
        catch (Exception ex)
        {
            PluginLog.Warning($"TicTacToe AI error: {ex.Message}");
            _playerTurn = true;
        }
    }

    // Minimax with alpha-beta pruning (AI is O and maximizes)
    private int FindBestMoveMinimax()
    {
        int bestScore = int.MinValue;
        int bestMove = -1;
        for (int i = 0; i < 9; i++)
        {
            if (_board[i] != Cell.Empty) continue;
            _board[i] = Cell.O;
            int score = Minimax(_board, aiTurn: false, depth: 0, alpha: int.MinValue + 1, beta: int.MaxValue - 1);
            _board[i] = Cell.Empty;
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = i;
            }
        }
        return bestMove;
    }

    private int FindWorstMoveMinimax()
    {
        int worstScore = int.MaxValue;
        int worstMove = -1;
        for (int i = 0; i < 9; i++)
        {
            if (_board[i] != Cell.Empty) continue;
            _board[i] = Cell.O;
            int score = Minimax(_board, aiTurn: false, depth: 0, alpha: int.MinValue + 1, beta: int.MaxValue - 1);
            _board[i] = Cell.Empty;
            if (score < worstScore)
            {
                worstScore = score;
                worstMove = i;
            }
        }
        return worstMove;
    }

    private static int Minimax(Cell[] board, bool aiTurn, int depth, int alpha, int beta)
    {
        var (terminal, score) = EvaluateTerminal(board, depth);
        if (terminal) return score;

        if (aiTurn)
        {
            int best = int.MinValue;
            for (int i = 0; i < 9; i++)
            {
                if (board[i] != Cell.Empty) continue;
                board[i] = Cell.O;
                best = Math.Max(best, Minimax(board, false, depth + 1, alpha, beta));
                board[i] = Cell.Empty;
                alpha = Math.Max(alpha, best);
                if (beta <= alpha) break;
            }
            return best;
        }
        else
        {
            int best = int.MaxValue;
            for (int i = 0; i < 9; i++)
            {
                if (board[i] != Cell.Empty) continue;
                board[i] = Cell.X;
                best = Math.Min(best, Minimax(board, true, depth + 1, alpha, beta));
                board[i] = Cell.Empty;
                beta = Math.Min(beta, best);
                if (beta <= alpha) break;
            }
            return best;
        }
    }

    private static (bool terminal, int score) EvaluateTerminal(Cell[] board, int depth)
    {
        if (CheckWin(board, Cell.O)) return (true, 10 - depth);
        if (CheckWin(board, Cell.X)) return (true, depth - 10);
        if (IsDraw(board)) return (true, 0);
        return (false, 0);
    }

    private static bool IsDraw(Cell[] board) => !board.Any(c => c == Cell.Empty);

    private static bool CheckWin(Cell[] board, Cell who)
    {
        foreach (var (a, b, c) in Lines())
        {
            if (board[a] == who && board[b] == who && board[c] == who) return true;
        }
        return false;
    }

    private bool IsEmpty(int i) => _board[i] == Cell.Empty;

    private static IEnumerable<(int a, int b, int c)> Lines()
    {
        yield return (0, 1, 2);
        yield return (3, 4, 5);
        yield return (6, 7, 8);
        yield return (0, 3, 6);
        yield return (1, 4, 7);
        yield return (2, 5, 8);
        yield return (0, 4, 8);
        yield return (2, 4, 6);
    }
}
