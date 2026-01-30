using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using ECommons.Logging;
using RotationSolver.Data;

namespace RotationSolver.UI
{
    internal class WelcomeWindow : Window
    {
        private static readonly HttpClient HttpClient = new();

        public WelcomeWindow() : base($"Welcome to Rotation Solver Reborn!", BaseFlags)
        {
            Size = new Vector2(650, 500);
            SizeCondition = ImGuiCond.FirstUseEver;
            if (_lastSeenChangelog != _assemblyVersion && Service.Config.ChangelogPopup)
            {
                PopulateChangelogs();
                IsOpen = true;
            }
        }

        private const ImGuiWindowFlags BaseFlags = ImGuiWindowFlags.NoCollapse
                                    | ImGuiWindowFlags.NoSavedSettings;
#if DEBUG
        private readonly string _assemblyVersion = "6.9.6.9"; //kekw
#else
        private string _assemblyVersion = typeof(RotationConfigWindow).Assembly.GetName().Version?.ToString() ?? "7.1.5.24";
#endif

        private readonly string _lastSeenChangelog = Service.Config.LastSeenChangelog;

        private GitHubCommitComparison _changeLog = new();

        private void PopulateChangelogs()
        {
            _ = Task.Run(GetGithubComparison);
        }

        private async Task GetGithubComparison()
        {
            string comparisonGoal = _lastSeenChangelog == "0.0.0.0" ? await GetNextMostRecentReleaseTag() : _lastSeenChangelog;
            string url = $"https://api.github.com/repos/{Service.USERNAME}/{Service.REPO}/compare/{comparisonGoal}...{_assemblyVersion}";
            try
            {
                HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("RotationSolver");
                HttpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");
                HttpResponseMessage response = await HttpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    GitHubCommitComparison? changeLog = JsonConvert.DeserializeObject<GitHubCommitComparison>(content);
                    if (changeLog != null)
                    {
                        _changeLog = changeLog;
                    }
                    else
                    {
                        PluginLog.Error("Failed to deserialize GitHub commit comparison.");
                    }
                }
                else
                {
                    PluginLog.Error($"Failed to get comparison 1: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Failed to get comparison 2: {ex.Message}");
            }
        }

        private static async Task<string> GetNextMostRecentReleaseTag()
        {
            string url = $"https://api.github.com/repos/{Service.USERNAME}/{Service.REPO}/releases";
            try
            {
                HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("RotationSolver");
                HttpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");
                HttpResponseMessage response = await HttpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    List<GithubRelease.Release>? releases = JsonConvert.DeserializeObject<List<GithubRelease.Release>>(content);
                    bool foundLatest = false;
                    if (releases?.Count > 0)
                    {
                        foreach (GithubRelease.Release release in releases)
                        {
                            if (release.Prerelease)
                            {
                                continue;
                            }

                            if (!foundLatest)
                            {
                                foundLatest = true;
                                continue;
                            }
                            return release.TagName;
                        }
                    }
                    return "7.2.1.46";
                }
                else
                {
                    PluginLog.Error($"Failed to get releases: {response.StatusCode}");
                    return "7.2.1.46";
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Failed to get releases: {ex.Message}");
                return "7.2.1.46";
            }
        }

        public override void Draw()
        {
            float windowWidth = ImGui.GetWindowWidth();
            // Centered title
            string text = UiString.WelcomeWindow_Header.GetDescription();
            float fontSize = ImGui.GetFontSize();
            ImGui.PushFont(FontManager.GetFont(fontSize + 10));
            float textSize = ImGui.CalcTextSize(text).X;
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

            text = UiString.WelcomeWindow_Welcome.GetDescription();
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
                _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = $"https://github.com/{Service.USERNAME}/{Service.REPO}", UseShellExecute = true });
            }
        }

        private void DrawChangeLog()
        {
            string text = UiString.WelcomeWindow_Changelog.GetDescription();
            float textSize = ImGui.CalcTextSize(text).X;
            ImGuiHelper.DrawItemMiddle(() =>
            {
                ImGui.TextColored(ImGuiColors.HealerGreen, text);
            }, ImGui.GetWindowWidth(), textSize);
            GitHubCommitComparison changeLog = _changeLog;
            if (changeLog == null || changeLog.Commits == null || changeLog.Commits.Count == 0)
            {
                ImGui.Text("No changelog available.");
                return;
            }

            List<Commit> commits = [];
            foreach (Commit c in changeLog.Commits)
            {
                if (!c.CommitData.Message.Contains("Merge pull request"))
                {
                    commits.Add(c);
                }
            }
            // Sort commits by CommitAuthor.Date descending
            for (int i = 0; i < commits.Count - 1; i++)
            {
                for (int j = i + 1; j < commits.Count; j++)
                {
                    if (commits[i].CommitData.CommitAuthor.Date < commits[j].CommitData.CommitAuthor.Date)
                    {
                        (commits[j], commits[i]) = (commits[i], commits[j]);
                    }
                }
            }

            List<string> authors = GetAuthorsFromChangeLogs(commits);
            int commitCount = commits.Count;
            int authorCount = authors.Count;

            ImGui.PushFont(FontManager.GetFont(ImGui.GetFontSize() + 1));
            ImGui.Text($"You've missed {commitCount} changes from {authorCount} contributer{(authorCount > 1 ? "s" : "")}!");
            ImGui.PopFont();

            foreach (Commit commit in commits)
            {
                ImGui.Text($"[{commit.CommitData.CommitAuthor.Date:yyyy-MM-dd}]");

                ImGui.Indent();
                ImGui.TextWrapped($"- {commit.CommitData.Message}");

                ImGui.TextWrapped($"By: @{commit.CommitData.CommitAuthor.Name}");
                ImGui.Unindent();
            }

            ImGui.NewLine();
            ImGui.Text("Contributors:");
            foreach (string author in authors)
            {
                if (ImGui.Button(author))
                {
                    _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = $"https://github.com/{author}", UseShellExecute = true });
                }
            }

            // Build file stats
            int additions = 0;
            int deletions = 0;
            int files = 0;
            if (changeLog.Files != null)
            {
                files = changeLog.Files.Count;
                foreach (CommitFile f in changeLog.Files)
                {
                    additions += f.Additions;
                    deletions += f.Deletions;
                }
            }
            if (ImGui.CollapsingHeader("Fun stats for nerds"))
            {
                ImGui.Text($"Total commits: {changeLog.TotalCommits}");
                ImGui.Text($"Total files changed: {files}");
                ImGui.Text($"Total additions: {additions}");
                ImGui.Text($"Total deletions: {deletions}");
            }
        }

        private static List<string> GetAuthorsFromChangeLogs(IEnumerable<Commit> commits)
        {
            HashSet<string> authors = [];
            foreach (Commit commit in commits)
            {
                _ = authors.Add(commit.CommitData.CommitAuthor.Name);
            }
            List<string> authorList = [.. authors];
            return authorList;
        }

        public override void OnClose()
        {
            Service.Config.LastSeenChangelog = _assemblyVersion;
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