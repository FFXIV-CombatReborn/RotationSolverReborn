using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using RotationSolver.Data;

namespace RotationSolver.UI
{
    internal class WelcomeWindow : Window
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public WelcomeWindow() : base($"Welcome to Rotation Solver Reborn!", BaseFlags)
        {
            Size = new Vector2(650, 500);
            SizeCondition = ImGuiCond.FirstUseEver;
            if ((_lastSeenChangelog != _assemblyVersion && Service.Config.ChangelogPopup || !Service.Config.FirstTimeSetupDone) && Service.Config.ChangelogPopup)
            {
                PopulateChangelogs();
                IsOpen = true;
            }
        }

        private const ImGuiWindowFlags BaseFlags = ImGuiWindowFlags.NoCollapse
                                    | ImGuiWindowFlags.NoSavedSettings;
#if DEBUG
        private string _assemblyVersion = "6.9.6.9"; //kekw
#else
        private string _assemblyVersion = typeof(RotationConfigWindow).Assembly.GetName().Version?.ToString() ?? "7.1.5.24";
#endif

        private string _lastSeenChangelog = Service.Config.LastSeenChangelog;

        private GitHubCommitComparison _changeLog = new();

        private void PopulateChangelogs()
        {
            Task.Run(GetGithubComparison);
        }

        private async Task GetGithubComparison()
        {
            var comparisonGoal = _lastSeenChangelog == "0.0.0.0" ? await GetNextMostRecentReleaseTag() : _lastSeenChangelog;
            string url = $"https://api.github.com/repos/{Service.USERNAME}/{Service.REPO}/compare/{comparisonGoal}...{_assemblyVersion}";
            try
            {
                HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("RotationSolver");
                HttpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");
                var response = await HttpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var changeLog = JsonConvert.DeserializeObject<GitHubCommitComparison>(content);
                    if (changeLog != null)
                    {
                        _changeLog = changeLog;
                    }
                    else
                    {
                        Svc.Log.Error("Failed to deserialize GitHub commit comparison.");
                    }
                }
                else
                {
                    Svc.Log.Error($"Failed to get comparison: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Svc.Log.Error(ex, "Failed to get comparison");
            }
        }

        private async Task<string> GetNextMostRecentReleaseTag()
        {
            var url = $"https://api.github.com/repos/{Service.USERNAME}/{Service.REPO}/releases";
            try
            {
                HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("RotationSolver");
                HttpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");
                var response = await HttpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var releases = JsonConvert.DeserializeObject<List<GithubRelease.Release>>(content);
                    var foundLatest = false;
                    if (releases?.Count > 0)
                    {
                        foreach (var release in releases)
                        {
                            if (release.Prerelease) continue;
                            if (!foundLatest)
                            {
                                foundLatest = true;
                                continue;
                            }
                            return release.TagName;
                        }
                    }
                    return "4.1.0.0";
                }
                else
                {
                    Svc.Log.Error($"Failed to get releases: {response.StatusCode}");
                    return "4.1.0.0";
                }
            }
            catch (Exception ex)
            {
                Svc.Log.Error(ex, "Failed to get releases");
                return "4.1.0.0";
            }
        }

        public override void Draw()
        {
            var windowWidth = ImGui.GetWindowWidth();
            // Centered title
            var text = "Welcome to Rotation Solver Reborn!";
            var fontSize = ImGui.GetFontSize();
            ImGui.PushFont(FontManager.GetFont(fontSize + 10));
            var textSize = ImGui.CalcTextSize(text).X;
            ImGuiHelper.DrawItemMiddle(() =>
            {
                ImGui.TextColored(ImGuiColors.ParsedGold, text);
            }, windowWidth, textSize);
            ImGui.PopFont();

            text = $"Version {_assemblyVersion}";
            ImGui.PushFont(FontManager.GetFont(fontSize + 3));
            textSize = ImGui.CalcTextSize(text).X;
            ImGuiHelper.DrawItemMiddle(() =>
            {
                ImGui.TextColored(ImGuiColors.TankBlue, text);
            }, windowWidth, textSize);
            ImGui.PopFont();

            text = Service.Config.FirstTimeSetupDone ? "Here's what you missed since you were last here" : "It looks like you might be new here! Let's get you started!";
            ImGui.PushFont(FontManager.GetFont(fontSize + 1));
            textSize = ImGui.CalcTextSize(text).X;
            ImGuiHelper.DrawItemMiddle(() =>
            {
                ImGui.TextColored(ImGuiColors.ParsedBlue, text);
            }, windowWidth, textSize);
            ImGui.PopFont();

            ImGui.Separator();  // Separator for aesthetic or logical separation

            DrawChangeLog();

            ImGui.Separator();
            ImGui.Text("Older changelogs are available on GitHub");
            if (ImGui.Button("Open GitHub"))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = $"https://github.com/{Service.USERNAME}/{Service.REPO}", UseShellExecute = true });
            }
        }

        private void DrawChangeLog()
        {
            var text = "Recent Changes:";
            var textSize = ImGui.CalcTextSize(text).X;
            ImGuiHelper.DrawItemMiddle(() =>
            {
                ImGui.TextColored(ImGuiColors.HealerGreen, text);
            }, ImGui.GetWindowWidth(), textSize);
            var changeLog = _changeLog;
            if (changeLog == null || changeLog.Commits == null || changeLog.Commits.Count == 0)
            {
                ImGui.Text("No changelog available.");
                return;
            }

            var commits = changeLog.Commits
                .Where(c => !c.CommitData.Message.Contains("Merge pull request"))
                .OrderByDescending(c => c.CommitData.CommitAuthor.Date)
                .ToList();
            var authors = GetAuthorsFromChangeLogs(commits);
            var commitCount = commits.Count;
            var authorCount = authors.Count;

            ImGui.PushFont(FontManager.GetFont(ImGui.GetFontSize() + 1));
            ImGui.Text($"You've missed {commitCount} changes from {authorCount} contributer{(authorCount > 1 ? "s" : "")}!");
            ImGui.PopFont();

            foreach (var commit in commits)
            {
                ImGui.Text($"[{commit.CommitData.CommitAuthor.Date:yyyy-MM-dd}]");

                ImGui.Indent();
                ImGui.TextWrapped($"- {commit.CommitData.Message}");

                ImGui.TextWrapped($"By: @{commit.CommitData.CommitAuthor.Name}");
                ImGui.Unindent();
            }

            ImGui.NewLine();
            ImGui.Text("Contributors:");
            foreach (var author in authors)
            {
                if (ImGui.Button(author))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = $"https://github.com/{author}", UseShellExecute = true });
                }
            }
            // Build file stats
            var additions = changeLog.Files.Sum(f => f.Additions);
            var deletions = changeLog.Files.Sum(f => f.Deletions);
            var files = changeLog.Files.Count;
            if (ImGui.CollapsingHeader("Fun stats for nerds"))
            {
                ImGui.Text($"Total commits: {changeLog.TotalCommits}");
                ImGui.Text($"Total files changed: {files}");
                ImGui.Text($"Total additions: {additions}");
                ImGui.Text($"Total deletions: {deletions}");
            }
        }

        private List<string> GetAuthorsFromChangeLogs(IEnumerable<Commit> commits)
        {
            var authors = new HashSet<string>();
            foreach (var commit in commits)
            {
                authors.Add(commit.CommitData.CommitAuthor.Name);
            }
            return authors.ToList();
        }

        public override void OnClose()
        {
            Service.Config.LastSeenChangelog = _assemblyVersion;
            Service.Config.FirstTimeSetupDone = true;
            Service.Config.Save();
            IsOpen = false;
            base.OnClose();
        }

        public override bool DrawConditions()
        {
            return Svc.ClientState.IsLoggedIn;
        }
    }
}
