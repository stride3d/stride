// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#:package Anthropic@12.58.0
#:property PublishAot=false

// Triage for stride3d/stride PRs and issues, run by .github/workflows/triage.yml. See README.md next to this file.
//
// It reads PRs and issues through the GitHub API, as data: it never checks out or runs PR code.
// Claude only returns values from fixed lists (labels, missing-info keys, issue numbers it was given);
// every comment is built here from fixed sentences, so the text of a PR or issue cannot make it post anything else.

using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Anthropic;
using Anthropic.Models.Beta.Messages;

var positional = new List<string>();
string? repoArg = null, actor = null;
var parallel = 2;
for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--dry-run": Run.DryRun = true; break;
        case "--no-ai": Run.NoAi = true; break;
        case "--claude-cli": Run.ClaudeCli = true; break;
        case "--repo": repoArg = args[++i]; break;
        case "--actor": actor = args[++i]; break;
        case "--parallel": parallel = int.Parse(args[++i]); break;
        default: positional.Add(args[i]); break;
    }
}

var repo = repoArg ?? Environment.GetEnvironmentVariable("GITHUB_REPOSITORY") ?? "stride3d/stride";
var config = Config.Load(Path.Combine(Run.Dir, "config.json"));
var github = new GitHub(repo);
var triage = new Triage(github, config);

try
{
    switch (positional.FirstOrDefault())
    {
        case "pr" when positional.Count == 2: await triage.PullRequest(int.Parse(positional[1]), labels: true); break;
        case "status" when positional.Count == 2: await triage.PullRequest(int.Parse(positional[1]), labels: false); break;
        case "status" when positional.Count == 1: await triage.AllStatus(); break;
        case "issue" when positional.Count == 2: await triage.Issue(int.Parse(positional[1])); break;
        case "issue-update" when positional.Count == 2: await triage.IssueUpdate(int.Parse(positional[1]), actor); break;
        case "weekly" when positional.Count == 1: await triage.Weekly(); break;
        case "backfill" when positional.Count == 2: await triage.Backfill(positional[1], parallel); break;
        case "report" when positional.Count == 2: triage.Report(positional[1]); break;
        case "apply" when positional.Count == 2: await triage.Apply(positional[1]); break;
        default:
            Console.Error.WriteLine("""
                Usage: dotnet run triage.cs -- <command> [--dry-run] [--no-ai | --claude-cli] [--repo owner/name] [--actor login]
                  pr <N>             area and type labels (once, if none), then status labels
                  status [<N>]       status labels of one PR, or of all open PRs
                  issue <N>          labels, missing-info check and possible duplicates of a new issue
                  issue-update <N>   removes needs-info when the reporter (--actor) added what was missing
                  weekly             nudges and the digest issue
                  backfill <plan>    classifies the open PRs and issues with no area or type label, changes nothing:
                                     writes <plan> (a JSON line per item, a rerun skips what is in it) and <plan>.md
                  report <plan>      writes <plan>.md again (split in several files when too long for a comment)
                  apply <plan>       adds the plan's labels, except where someone set an area or type since
                --parallel <N> runs backfill on N items at a time (default 2).
                --dry-run writes what it would do to the output and the job summary, and changes nothing.
                --claude-cli asks Claude through the local `claude` CLI (its login, for example a subscription) instead of ANTHROPIC_API_KEY.
                """);
            return 2;
    }
}
finally
{
    Run.Flush();
}
return Run.Failed ? 1 : 0;

static class Run
{
    public static bool DryRun, NoAi, ClaudeCli, Failed;
    public static readonly string Dir = Path.GetDirectoryName(ThisFile())!;
    static readonly List<string> summary = [];

    public static void Log(string line)
    {
        Console.WriteLine(line);
        summary.Add(line);
    }

    public static void Error(string line, Exception? ex = null)
    {
        Failed = true;
        Console.Error.WriteLine(ex == null ? line : $"{line}: {ex}");
        summary.Add($"**Error:** {line}{(ex == null ? "" : $": {ex.Message}")}");
    }

    static string ThisFile([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;

    public static void Flush()
    {
        if (Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY") is { Length: > 0 } path && summary.Count > 0)
            File.AppendAllText(path, (DryRun ? "**Dry run: nothing was changed.**\n\n" : "") + string.Join("\n", summary) + "\n");
    }
}

sealed class Triage(GitHub github, Config config)
{
    const string NudgeMarker = "<!-- stride-triage:nudge -->";
    const string IssueMarker = "<!-- stride-triage:issue -->";
    const string DigestMarker = "<!-- stride-triage:digest -->";

    static readonly Dictionary<string, string> MissingText = new()
    {
        ["version"] = "the Stride version you use (for example 4.4.1, or the commit if you build Stride from source)",
        ["platform"] = "the platform where it happens (Windows, Linux, macOS, Android, iOS)",
        ["graphics_api"] = "the graphics API (Direct3D 11, Direct3D 12, Vulkan or OpenGL)",
        ["repro"] = "steps to reproduce it, or a small project that shows the problem",
        ["log"] = "the error message, log or callstack (a .txt file is fine)",
    };

    HashSet<string>? repoLabels;

    // PRs

    public async Task PullRequest(int number, bool labels)
    {
        var pr = await github.PullRequest(number);
        if (pr == null)
        {
            Run.Log($"#{number}: not a PR, or not found.");
            return;
        }
        if (labels && !pr.IsDraft)
            await Labels(pr);
        await ApplyStatus(pr);
    }

    public async Task AllStatus()
    {
        var prs = await github.OpenPullRequests();
        Run.Log($"{prs.Count} open PRs.");
        foreach (var pr in prs)
        {
            try
            {
                await ApplyStatus(pr);
            }
            catch (Exception ex)
            {
                Run.Error($"#{pr.Number}: status", ex);
            }
        }
    }

    async Task Labels(Pr pr)
    {
        var (add, _) = await PlanLabels(pr);
        await AddLabels(pr.Number, add, $"#{pr.Number} {pr.Title}");
    }

    bool HasArea(IEnumerable<string> labels) => labels.Any(x => x.StartsWith("area-", StringComparison.Ordinal));
    bool HasPrType(IEnumerable<string> labels) => labels.Any(x => config.PrTypes.Contains(x) || x == "new-feature");
    bool HasIssueType(IEnumerable<string> labels) => labels.Any(config.IssueTypes.Contains);

    // The labels to add to a PR, and Claude's reason
    async Task<(List<string> Add, string Reason)> PlanLabels(Pr pr)
    {
        var hasArea = HasArea(pr.Labels);
        var hasType = HasPrType(pr.Labels);
        if (hasArea && hasType)
            return ([], "already has an area and a type");
        var reason = "";

        var files = await github.PullRequestFiles(pr.Number);
        var pathAreas = Areas(files);
        var areas = pathAreas.Select(x => x.Area).ToList();
        string? type = null;
        var flags = new List<string>();

        if (Run.NoAi || !Ai.Available)
        {
            Run.Log($"#{pr.Number}: no Claude call ({Ai.Missing}), areas from paths only.");
        }
        else
        {
            var diff = await github.PullRequestDiff(pr.Number, files);
            var truncated = diff.Length > config.MaxDiffChars;
            var input = new StringBuilder()
                .AppendLine("<pr>")
                .AppendLine($"title: {pr.Title}")
                .AppendLine($"author: {pr.Author}")
                .AppendLine("description:")
                .AppendLine(Truncate(pr.Body, 8000))
                .AppendLine("</pr>")
                .AppendLine($"<path_areas>{(pathAreas.Count == 0 ? "none" : string.Join(", ", pathAreas.Select(x => $"{x.Area} ({x.Share:P0})")))}</path_areas>")
                .AppendLine($"<files count=\"{files.Count}\">")
                .AppendJoin("\n", files.Take(300).Select(x => $"{x.Status} {x.Path} (+{x.Additions} -{x.Deletions})")).AppendLine()
                .AppendLine("</files>")
                .AppendLine($"<diff truncated=\"{(truncated ? "yes, the rest is left out" : "no")}\">")
                .AppendLine(truncated ? diff[..config.MaxDiffChars] : diff)
                .AppendLine("</diff>")
                .ToString();
            var result = await Ai.Classify(config, "pr-labels.md", input, new JsonObject
            {
                ["areas"] = EnumArray(config.AreaLabels),
                ["type"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray([.. config.PrTypes.Select(x => JsonValue.Create(x))]) },
                ["breaking_change"] = new JsonObject { ["type"] = "boolean" },
                ["behavioral_change"] = new JsonObject { ["type"] = "boolean" },
                ["performance"] = new JsonObject { ["type"] = "boolean" },
                ["platforms"] = EnumArray(config.Platforms),
                ["reason"] = new JsonObject { ["type"] = "string" },
            });
            var aiAreas = Strings(result["areas"]).Where(config.AreaLabels.Contains).Distinct().Take(3).ToList();
            if (aiAreas.Count > 0)
                areas = aiAreas;
            type = (string?)result["type"] is { } t && config.PrTypes.Contains(t) ? t : null;
            if (result["breaking_change"]?.GetValue<bool>() == true) flags.Add("breaking-change");
            if (result["behavioral_change"]?.GetValue<bool>() == true) flags.Add("behavioral-change");
            if (result["performance"]?.GetValue<bool>() == true) flags.Add("performance");
            flags.AddRange(Strings(result["platforms"]).Where(config.Platforms.Contains).Distinct());
            reason = (string?)result["reason"] ?? "";
            Run.Log($"#{pr.Number}: Claude: {reason}{Answer(result)}");
        }

        var add = new List<string>();
        if (!hasArea) add.AddRange(areas);
        if (!hasType && type != null) add.Add(type);
        add.AddRange(flags.Where(x => !pr.Labels.Contains(x)));
        return ([.. add.Distinct()], reason);
    }

    List<(string Area, double Share)> Areas(List<PrFile> files)
    {
        var weights = new Dictionary<string, double>();
        foreach (var file in files)
        {
            var rule = config.Areas.FirstOrDefault(x => x.Pattern.IsMatch(file.Path) || (file.PreviousPath != null && x.Pattern.IsMatch(file.PreviousPath)));
            if (rule.Area != null)
                weights[rule.Area] = weights.GetValueOrDefault(rule.Area) + file.Additions + file.Deletions + 1;
        }
        // A sample or docs page that comes with a change is not what the PR is about
        if (weights.Keys.Any(x => !config.CompanionAreas.Contains(x)))
            foreach (var area in config.CompanionAreas)
                weights.Remove(area);
        var total = weights.Values.Sum();
        return [.. weights.Select(x => (x.Key, x.Value / total)).Where(x => x.Item2 >= config.AreaShare).OrderByDescending(x => x.Item2).Take(3)];
    }

    async Task ApplyStatus(Pr pr)
    {
        if (pr.Mergeable == "UNKNOWN")
        {
            Run.Log($"#{pr.Number}: GitHub has not computed the merge state yet, the next run will look again.");
            return;
        }
        var (label, why) = Status(pr);
        var current = pr.Labels.Where(config.StatusLabels.ContainsKey).ToList();
        if (current.Count == (label == null ? 0 : 1) && (label == null || current.Contains(label)))
            return;

        await EnsureLabels([.. config.StatusLabels.Keys]);
        foreach (var old in current.Where(x => x != label))
            await Write($"#{pr.Number}: remove `{old}`", () => github.RemoveLabel(pr.Number, old));
        if (label != null && !current.Contains(label))
            await Write($"#{pr.Number}: `{label}` ({why})", () => github.AddLabels(pr.Number, [label]));
    }

    // Who has the ball: the author after a maintainer reviewed later than the author's last push or reply, otherwise the reviewers
    (string? Label, string Why) Status(Pr pr)
    {
        if (pr.IsDraft)
            return (null, "draft");
        if (pr.Mergeable == "CONFLICTING")
            return ("waiting-on-author", "merge conflicts");

        var lastReview = MaintainerReviews(pr).MaxBy(x => x.At);
        // A status label a person set counts like a review: it stays until the author's next push or reply, or the next review
        var manual = pr.LabelEvents.Where(x => config.StatusLabels.ContainsKey(x.Label)).MaxBy(x => x.At);
        if (manual != null && manual.At > pr.LastAuthorActivity && (lastReview == null || manual.At > lastReview.At))
            return (manual.Label, $"set by @{manual.By}");
        if (lastReview != null && lastReview.At > pr.LastAuthorActivity)
        {
            if (lastReview.State != "APPROVED")
                return ("waiting-on-author", $"@{lastReview.Author} reviewed after the last push or reply");
            if (pr.ReviewDecision == "CHANGES_REQUESTED")
                return ("waiting-on-review", "approved, but another reviewer still requests changes");
            if (pr.CiNeedsApproval)
                return ("waiting-on-review", "approved, CI runs wait for a maintainer to approve them");
            return pr.Ci switch
            {
                "SUCCESS" or null => ("ready-to-merge", $"approved by @{lastReview.Author}, CI green"),
                "FAILURE" or "ERROR" => ("waiting-on-author", "approved, but CI fails"),
                _ => ("waiting-on-review", "approved, CI still running"),
            };
        }
        return ("waiting-on-review", lastReview == null ? "no review yet" : "the author pushed or replied after the last review");
    }

    IEnumerable<Review> MaintainerReviews(Pr pr) => pr.Reviews.Where(x => x.Author != pr.Author && config.ReviewerAssociations.Contains(x.Association));

    // Weekly: nudges and the digest

    public async Task Weekly()
    {
        var now = DateTimeOffset.UtcNow;
        var prs = await github.OpenPullRequests();
        var noReviewer = new List<(Pr Pr, int Days)>();
        var ready = new List<(Pr Pr, int Days)>();
        var stale = new List<(Pr Pr, int Days, string Status)>();

        foreach (var pr in prs.Where(x => !x.IsDraft))
        {
            var (label, _) = Status(pr);
            var quiet = (int)(now - pr.LastHumanActivity).TotalDays;
            var reviewers = pr.ReviewRequests.Concat(pr.Assignees).Where(x => x != pr.Author).Distinct().ToList();
            var hasReviewer = reviewers.Count > 0 || pr.TeamReviewRequests.Count > 0 || MaintainerReviews(pr).Any();

            if (label == "waiting-on-review" && !hasReviewer && (now - pr.ReadyAt).TotalDays >= config.NoReviewerDays)
                noReviewer.Add((pr, (int)(now - pr.ReadyAt).TotalDays));
            // Merging is the maintainers' step: the digest lists these, no nudge
            if (label == "ready-to-merge")
            {
                ready.Add((pr, quiet));
                continue;
            }
            if (quiet < config.StaleDays)
                continue;

            stale.Add((pr, quiet, label == "waiting-on-author" ? $"waiting on @{pr.Author}" : reviewers.Count > 0 ? $"waiting on {string.Join(", ", reviewers.Select(x => "@" + x))}" : "waiting on a review"));
            if (pr.Labels.Contains("no-nudge") || pr.BotComments.Any(x => x.Body.Contains(NudgeMarker, StringComparison.Ordinal) && x.At > pr.LastHumanActivity))
                continue;

            string? body = null;
            var enabled = false;
            if (label == "waiting-on-review" && reviewers.Count > 0)
            {
                enabled = config.NudgeReviewers;
                body = $"{string.Join(" ", reviewers.Select(x => "@" + x))}, this PR has been waiting on your review for {quiet} days. Are you still planning to look at it? If not, no problem: feel free to unassign yourself or remove the review request, so someone else can pick it up.";
            }
            else if (label == "waiting-on-author")
            {
                enabled = config.NudgeAuthors;
                body = $"@{pr.Author}, any plans to continue this PR? It has been quiet for {quiet} days. If you are busy, no problem: just let us know, a maintainer can take it over, or we can close it for now and reopen it later.";
            }
            if (body == null)
                continue;
            body += $"\n\n{NudgeMarker}\n<sub>Posted by the triage workflow, once per quiet period. Add the `no-nudge` label to stop it on this PR.</sub>";
            if (enabled)
                await Write($"#{pr.Number}: nudge ({quiet} days quiet)", () => github.Comment(pr.Number, body));
            else
                Run.Log($"#{pr.Number}: would nudge ({quiet} days quiet), off in config.json:\n> {body.Split('\n')[0]}");
        }

        await Digest(noReviewer, ready, stale);
    }

    async Task Digest(List<(Pr Pr, int Days)> noReviewer, List<(Pr Pr, int Days)> ready, List<(Pr Pr, int Days, string Status)> stale)
    {
        static string Line(Pr pr, string detail) => $"- #{pr.Number} {pr.Title.Replace("\n", " ")} (@{pr.Author}, {detail})";
        var body = new StringBuilder()
            .AppendLine(DigestMarker)
            .AppendLine($"<!-- no-reviewer: {string.Join(",", noReviewer.Select(x => x.Pr.Number))} -->")
            .AppendLine($"Updated every Monday by the [triage workflow](../blob/master/.github/triage/README.md). Last update: {DateTimeOffset.UtcNow:yyyy-MM-dd}.")
            .AppendLine()
            .AppendLine($"### No reviewer yet ({noReviewer.Count})")
            .AppendLine($"Ready for review for {config.NoReviewerDays}+ days, nobody requested, assigned or reviewed. Pick one up, or request a review from someone who knows the area.")
            .AppendJoin("\n", noReviewer.OrderByDescending(x => x.Days).Select(x => Line(x.Pr, $"{x.Days} days"))).AppendLine()
            .AppendLine()
            .AppendLine($"### Approved, not merged ({ready.Count})")
            .AppendJoin("\n", ready.OrderByDescending(x => x.Days).Select(x => Line(x.Pr, $"quiet for {x.Days} days"))).AppendLine()
            .AppendLine()
            .AppendLine($"### Quiet for {config.StaleDays}+ days ({stale.Count})")
            .AppendJoin("\n", stale.OrderByDescending(x => x.Days).Select(x => Line(x.Pr, $"{x.Days} days, {x.Status}"))).AppendLine()
            .ToString();

        if (!config.Digest)
        {
            Run.Log($"Digest (off in config.json, not posted):\n\n{body}");
            return;
        }

        await EnsureLabels(["triage-digest"]);
        var issue = await github.DigestIssue();
        if (issue == null)
        {
            await Write("create the digest issue", () => github.CreateIssue("PR triage", body, ["triage-digest"]));
            return;
        }
        await Write($"update the digest issue #{issue.Value.Number}", () => github.UpdateIssueBody(issue.Value.Number, body));

        if (config.DigestMentionTeam is { Length: > 0 } team)
        {
            var before = Regex.Match(issue.Value.Body, @"<!-- no-reviewer: ([\d,]*) -->").Groups[1].Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
            var added = noReviewer.Where(x => !before.Contains(x.Pr.Number.ToString())).Select(x => $"#{x.Pr.Number}").ToList();
            if (added.Count > 0)
                await Write($"mention @{team} on the digest", () => github.Comment(issue.Value.Number, $"@{team} new PRs without a reviewer: {string.Join(", ", added)}"));
        }
    }

    // Issues

    public async Task Issue(int number)
    {
        var issue = await github.Issue(number);
        if (issue == null || issue.AuthorIsBot || issue.Labels.Contains("triage-digest"))
            return;

        var result = await ClassifyIssue(issue);
        if (result == null)
            return;
        var (type, areas, platforms, missing, duplicates, _) = result.Value;

        var add = new List<string>();
        var remove = new List<string>();
        if (!issue.Labels.Any(x => x.StartsWith("area-", StringComparison.Ordinal)))
            add.AddRange(areas);
        add.AddRange(platforms.Where(x => !issue.Labels.Contains(x)));
        // The template sets the type: replace it when the reporter picked the wrong template
        if (type != null && !issue.Labels.Contains(type))
        {
            add.Add(type);
            remove.AddRange(issue.Labels.Where(config.IssueTypes.Contains));
        }
        var isMaintainer = config.ReviewerAssociations.Contains(issue.Association);
        if (missing.Count > 0 && !isMaintainer)
            add.Add("needs-info");

        await AddLabels(number, add, $"issue #{number} {issue.Title}");
        foreach (var label in remove)
            await Write($"issue #{number}: remove `{label}`", () => github.RemoveLabel(number, label));

        if (issue.Comments.Any(x => x.Body.Contains(IssueMarker, StringComparison.Ordinal)) || ((missing.Count == 0 || isMaintainer) && duplicates.Count == 0))
            return;

        var body = new StringBuilder();
        if (missing.Count > 0 && !isMaintainer)
        {
            body.AppendLine("Thanks for the report! To help us look into it, could you add:").AppendLine();
            foreach (var key in missing)
                body.AppendLine($"- {MissingText[key]}");
            body.AppendLine().AppendLine("You can edit the issue or reply here.").AppendLine();
        }
        if (duplicates.Count > 0)
            body.AppendLine($"This looks related to {string.Join(", ", duplicates.Select(x => $"#{x}"))}, it may help to have a look there too.").AppendLine();
        body.AppendLine(IssueMarker).Append("<sub>Posted by the triage workflow. Maintainers decide what happens with the issue.</sub>");
        var text = body.ToString();
        await Write($"issue #{number}: comment\n{string.Join("\n", text.Split('\n').Select(x => "> " + x))}", () => github.Comment(number, text));
    }

    public async Task IssueUpdate(int number, string? actor)
    {
        var issue = await github.Issue(number);
        if (issue == null || !issue.Labels.Contains("needs-info"))
            return;
        if (actor != issue.Author)
        {
            Run.Log($"issue #{number}: updated by @{actor}, not by the reporter, nothing to check.");
            return;
        }
        var result = await ClassifyIssue(issue);
        if (result is { Missing.Count: 0 })
            await Write($"issue #{number}: the reporter added what was missing, remove `needs-info`", () => github.RemoveLabel(number, "needs-info"));
        else if (result != null)
            Run.Log($"issue #{number}: still missing {string.Join(", ", result.Value.Missing)}, no new comment.");
    }

    async Task<(string? Type, List<string> Areas, List<string> Platforms, List<string> Missing, List<int> Duplicates, string Reason)?> ClassifyIssue(IssueInfo issue, bool searchDuplicates = true)
    {
        if (Run.NoAi || !Ai.Available)
        {
            Run.Log($"issue #{issue.Number}: no Claude call ({Ai.Missing}), nothing to do.");
            return null;
        }

        var candidates = searchDuplicates ? await github.SearchIssues(issue.Number, issue.Title) : [];
        var input = new StringBuilder()
            .AppendLine("<issue>")
            .AppendLine($"title: {issue.Title}")
            .AppendLine($"labels: {string.Join(", ", issue.Labels)}")
            .AppendLine("text:")
            .AppendLine(Truncate(issue.Body, 20000));
        foreach (var comment in issue.Comments.Where(x => x.Author == issue.Author))
            input.AppendLine($"reply from the reporter, {comment.At:yyyy-MM-dd}:").AppendLine(Truncate(comment.Body, 5000));
        input.AppendLine("</issue>").AppendLine("<candidates>");
        foreach (var c in candidates)
            input.AppendLine($"#{c.Number} [{c.State}] {c.Title}: {Truncate(c.Body, 400).Replace('\n', ' ')}");
        input.AppendLine("</candidates>");

        var result = await Ai.Classify(config, "issue-triage.md", input.ToString(), new JsonObject
        {
            ["type"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray([.. config.IssueTypes.Select(x => JsonValue.Create(x))]) },
            ["areas"] = EnumArray(config.AreaLabels),
            ["platforms"] = EnumArray(config.Platforms),
            ["missing"] = EnumArray(MissingText.Keys),
            ["duplicates"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "integer" } },
            ["reason"] = new JsonObject { ["type"] = "string" },
        });
        Run.Log($"issue #{issue.Number}: Claude: {result["reason"]}{Answer(result)}");

        var type = (string?)result["type"] is { } t && config.IssueTypes.Contains(t) ? t : null;
        var areas = Strings(result["areas"]).Where(config.AreaLabels.Contains).Distinct().Take(3).ToList();
        var platforms = Strings(result["platforms"]).Where(config.Platforms.Contains).Distinct().ToList();
        var missing = type == "bug" ? Strings(result["missing"]).Where(MissingText.ContainsKey).Distinct().ToList() : [];
        var known = candidates.Select(x => x.Number).ToHashSet();
        var duplicates = (result["duplicates"]?.AsArray() ?? []).Select(x => x?.GetValue<int>() ?? 0).Where(known.Contains).Distinct().Take(3).ToList();
        return (type, areas, platforms, missing, duplicates, (string?)result["reason"] ?? "");
    }

    // Backfill: labels for the open PRs and issues that have none yet, as a plan to check first and apply later

    // One JSON line per item, written as soon as it is classified: a rerun skips what is in the plan already
    public async Task Backfill(string planPath, int parallel)
    {
        var done = ReadPlan(planPath).Select(x => $"{x["kind"]}#{x["number"]}").ToHashSet();
        var work = new List<(string Kind, int Number, Func<Task<JsonObject>> Plan)>();

        foreach (var pr in (await github.OpenPullRequests()).Where(x => !x.IsDraft && !(HasArea(x.Labels) && HasPrType(x.Labels))))
            work.Add(("pr", pr.Number, async () =>
            {
                var (add, reason) = await PlanLabels(pr);
                return Planned("pr", pr.Number, pr.Title, add, reason);
            }));
        foreach (var issue in (await github.OpenIssues()).Where(x => !x.AuthorIsBot && !x.Labels.Contains("triage-digest") && !(HasArea(x.Labels) && HasIssueType(x.Labels))))
            work.Add(("issue", issue.Number, async () =>
            {
                // The backlog only gets labels: no comment, no needs-info, and an existing type stays
                var result = await ClassifyIssue(issue, searchDuplicates: false);
                var add = new List<string>();
                if (result is { } r)
                {
                    if (!HasArea(issue.Labels)) add.AddRange(r.Areas);
                    if (!HasIssueType(issue.Labels) && r.Type != null) add.Add(r.Type);
                    add.AddRange(r.Platforms.Where(x => !issue.Labels.Contains(x)));
                }
                return Planned("issue", issue.Number, issue.Title, add, result?.Reason ?? "");
            }));

        work.RemoveAll(x => done.Contains($"{x.Kind}#{x.Number}"));
        Run.Log($"Backfill: {work.Count} items to classify ({done.Count} already in {planPath}).");
        var gate = new object();
        var finished = 0;
        await Parallel.ForEachAsync(work, new ParallelOptions { MaxDegreeOfParallelism = parallel }, async (item, _) =>
        {
            try
            {
                var line = (await item.Plan()).ToJsonString();
                lock (gate)
                {
                    File.AppendAllText(planPath, line + "\n");
                    Console.WriteLine($"[{++finished}/{work.Count}] {item.Kind} #{item.Number}");
                }
            }
            catch (Exception ex)
            {
                // Not in the plan: the next run tries it again
                lock (gate)
                    Run.Error($"{item.Kind} #{item.Number}", ex);
            }
        });
        Report(planPath);
    }

    static JsonObject Planned(string kind, int number, string title, List<string> add, string reason) => new()
    {
        ["kind"] = kind,
        ["number"] = number,
        ["title"] = title,
        ["add"] = new JsonArray([.. add.Distinct().Select(x => JsonValue.Create(x))]),
        ["reason"] = reason,
    };

    static List<JsonNode> ReadPlan(string path) =>
        File.Exists(path) ? [.. File.ReadLines(path).Where(x => x.Trim().Length > 0).Select(x => JsonNode.Parse(x)!)] : [];

    // Markdown for a PR comment, split in parts that fit in a GitHub comment (65,536 characters)
    public void Report(string planPath)
    {
        var plan = ReadPlan(planPath);
        static string Title(JsonNode x) => ((string)x["title"]!).Replace("|", "\\|").Replace("\n", " ") is var t && t.Length > 70 ? t[..70] + "…" : ((string)x["title"]!).Replace("|", "\\|");
        static List<string> Add(JsonNode x) => [.. x["add"]!.AsArray().Select(y => (string)y!)];
        var labeled = plan.Where(x => Add(x).Count > 0).ToList();

        var head = new StringBuilder()
            .AppendLine("### Triage backfill (dry run)")
            .AppendLine()
            .AppendLine($"Labels the triage workflow would add to the open PRs and issues that have no area or type label yet: {labeled.Count(x => (string)x["kind"]! == "pr")} PRs and {labeled.Count(x => (string)x["kind"]! == "issue")} issues ({plan.Count - labeled.Count} more were checked and get nothing). Old issues only get labels: no comment, and an existing type stays.")
            .AppendLine()
            .AppendLine("| Label | PRs | Issues |")
            .AppendLine("|---|---|---|");
        foreach (var label in labeled.SelectMany(Add).Distinct().OrderBy(x => x))
            head.AppendLine($"| `{label}` | {labeled.Count(x => (string)x["kind"]! == "pr" && Add(x).Contains(label))} | {labeled.Count(x => (string)x["kind"]! == "issue" && Add(x).Contains(label))} |");

        var sections = new List<string> { head.ToString() };
        var prs = labeled.Where(x => (string)x["kind"]! == "pr").OrderBy(x => (int)x["number"]!).ToList();
        if (prs.Count > 0)
            sections.Add("#### PRs\n\n| PR | Title | Adds |\n|---|---|---|\n" + string.Join("\n", prs.Select(x => $"| #{x["number"]} | {Title(x)} | {string.Join(", ", Add(x).Select(y => $"`{y}`"))} |")) + "\n");
        // Issues grouped by their first area, so a maintainer of an area can check their part
        foreach (var group in labeled.Where(x => (string)x["kind"]! == "issue").GroupBy(x => Add(x).FirstOrDefault(y => y.StartsWith("area-", StringComparison.Ordinal)) ?? "no new area").OrderBy(x => x.Key))
            sections.Add($"#### Issues: {group.Key} ({group.Count()})\n\n" + string.Join("\n", group.OrderBy(x => (int)x["number"]!).Select(x => $"- #{x["number"]} {Title(x)}: {string.Join(", ", Add(x).Select(y => $"`{y}`"))}")) + "\n");

        var parts = new List<StringBuilder> { new() };
        foreach (var section in sections)
        {
            if (parts[^1].Length + section.Length > 60000)
                parts.Add(new StringBuilder());
            parts[^1].AppendLine(section);
        }
        for (var i = 0; i < parts.Count; i++)
        {
            var path = Path.ChangeExtension(planPath, parts.Count == 1 ? ".md" : $".{i + 1}.md");
            File.WriteAllText(path, (parts.Count > 1 ? $"*Part {i + 1} of {parts.Count}*\n\n" : "") + parts[i]);
            Run.Log($"Report: {path} ({parts[i].Length} characters)");
        }
    }

    // Applies a plan: adds its labels, except where someone set an area or a type since, and skips closed items
    public async Task Apply(string planPath)
    {
        repoLabels ??= await github.Labels();
        if (repoLabels.Contains("area-AI") && !repoLabels.Contains("area-Navigation"))
            throw new InvalidOperationException("Rename area-AI to area-Navigation first (gh label edit area-AI --name area-Navigation), so the issues that have it keep it.");
        foreach (var item in ReadPlan(planPath))
        {
            var number = (int)item["number"]!;
            var add = item["add"]!.AsArray().Select(x => (string)x!).ToList();
            if (add.Count == 0)
                continue;
            var (open, labels) = await github.IssueLabels(number);
            if (!open)
                continue;
            var isPr = (string)item["kind"]! == "pr";
            if (HasArea(labels)) add.RemoveAll(x => x.StartsWith("area-", StringComparison.Ordinal));
            if (isPr ? HasPrType(labels) : HasIssueType(labels)) add.RemoveAll(x => (isPr ? config.PrTypes : config.IssueTypes).Contains(x));
            add.RemoveAll(labels.Contains);
            await AddLabels(number, add, $"{item["kind"]} #{number}");
            // Stay under GitHub's limit on write requests
            if (!Run.DryRun && add.Count > 0)
                await Task.Delay(1000);
        }
    }

    // Helpers

    async Task AddLabels(int number, List<string> labels, string what)
    {
        labels = [.. labels.Distinct()];
        if (labels.Count == 0)
            return;
        await EnsureLabels(labels);
        await Write($"{what}: add {string.Join(", ", labels.Select(x => $"`{x}`"))}", () => github.AddLabels(number, labels));
    }

    // Creates the labels this workflow owns when the repo does not have them yet. Other labels (areas, types) must exist.
    async Task EnsureLabels(List<string> labels)
    {
        repoLabels ??= await github.Labels();
        foreach (var label in labels.Where(x => !repoLabels.Contains(x)))
        {
            var spec = config.StatusLabels.GetValueOrDefault(label) ?? config.OtherLabels.GetValueOrDefault(label)
                ?? throw new InvalidOperationException($"Label `{label}` does not exist in the repo.");
            await Write($"create the label `{label}`", () => github.CreateLabel(label, spec.Color, spec.Description));
            repoLabels.Add(label);
        }
    }

    static async Task Write(string what, Func<Task> action)
    {
        if (Run.DryRun)
        {
            Run.Log($"would {what}");
            return;
        }
        await action();
        Run.Log(what);
    }


    // A dry run also shows the whole answer, to compare it with the labels people set
    static string Answer(JsonNode result) => Run.DryRun ? $"\n  answer: {new JsonObject(result.AsObject().Where(x => x.Key != "reason").Select(x => KeyValuePair.Create(x.Key, x.Value?.DeepClone()))).ToJsonString()}" : "";

    static string Truncate(string text, int length) => text.Length <= length ? text : text[..length] + "\n[...]";

    static JsonObject EnumArray(IEnumerable<string> values) => new()
    {
        ["type"] = "array",
        ["items"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray([.. values.Select(x => JsonValue.Create(x))]) },
    };

    static IEnumerable<string> Strings(JsonNode? node) => node?.AsArray().Select(x => (string?)x).OfType<string>() ?? [];
}

static class Ai
{
    public static bool Available => Run.ClaudeCli || Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") is { Length: > 0 };

    public static string Missing => Run.NoAi ? "--no-ai" : "no ANTHROPIC_API_KEY and no --claude-cli";

    // One call with a structured output: the answer can only hold the values the schema allows
    public static async Task<JsonNode> Classify(Config config, string prompt, string input, JsonObject properties)
    {
        var schema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = new JsonArray([.. properties.Select(x => JsonValue.Create(x.Key))]),
            ["additionalProperties"] = false,
        };
        var promptPath = Path.Combine(Run.Dir, "prompts", prompt);
        if (Run.ClaudeCli)
            return await ClassifyWithCli(config, promptPath, input, schema);

        var client = new AnthropicClient();
        var response = await client.Beta.Messages.Create(new MessageCreateParams
        {
            Model = config.Model,
            MaxTokens = 16000,
            Betas = ["server-side-fallback-2026-07-01"],
            Fallbacks = new Default(),
            System = File.ReadAllText(promptPath),
            OutputConfig = new BetaOutputConfig
            {
                Effort = config.Effort,
                Format = new BetaJsonOutputFormat { Schema = schema.ToDictionary(x => x.Key, x => JsonSerializer.SerializeToElement(x.Value)) },
            },
            Messages = [new() { Role = Role.User, Content = input }],
        });
        if (response.StopReason != "end_turn")
            throw new InvalidOperationException($"Claude stopped with `{response.StopReason}`.");
        var text = string.Concat(response.Content.Select(x => x.Value).OfType<BetaTextBlock>().Select(x => x.Text));
        return JsonNode.Parse(text) ?? throw new InvalidOperationException("Claude returned no JSON.");
    }

    // Local runs: the same prompt and schema through `claude -p`, so they use the Claude Code login (a subscription
    // for example) instead of an API key. No tools, no CLAUDE.md, skills or hooks: the input is all it sees.
    static async Task<JsonNode> ClassifyWithCli(Config config, string promptPath, string input, JsonObject schema)
    {
        var start = new ProcessStartInfo("claude") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in (string[])["-p", "--safe-mode", "--tools", "", "--no-session-persistence", "--output-format", "json",
            "--json-schema", schema.ToJsonString(), "--system-prompt-file", promptPath, "--model", config.Model, "--effort", config.Effort])
            start.ArgumentList.Add(arg);
        start.Environment.Remove("ANTHROPIC_API_KEY");
        using var process = Process.Start(start)!;
        // The input can be larger than a command line: it goes through stdin
        await process.StandardInput.WriteAsync(input);
        process.StandardInput.Close();
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var result = JsonNode.Parse(await output is { Length: > 0 } text ? text : "{}");
        if (process.ExitCode != 0 || (bool?)result?["is_error"] == true || result?["structured_output"] is not JsonNode answer)
            throw new InvalidOperationException($"claude failed ({process.ExitCode}): {(string?)result?["subtype"]} {(string?)result?["result"]} {(await error).Trim()}");
        return answer.DeepClone();
    }
}

sealed class Config
{
    public required string Model, Effort;
    public required int MaxDiffChars, StaleDays, NoReviewerDays;
    public required double AreaShare;
    public required bool NudgeReviewers, NudgeAuthors, Digest;
    public required string DigestMentionTeam;
    public required HashSet<string> ReviewerAssociations, PrTypes, IssueTypes, Platforms, CompanionAreas, ClaudeOnlyAreas;
    public required Dictionary<string, LabelSpec> StatusLabels, OtherLabels;
    public required List<(Regex Pattern, string? Area)> Areas;
    // Areas no path shows (the plugin system is spread over several folders) are Claude's choice only
    public IEnumerable<string> AreaLabels => Areas.Select(x => x.Area).OfType<string>().Concat(ClaudeOnlyAreas).Distinct();

    public static Config Load(string path)
    {
        var json = JsonNode.Parse(File.ReadAllText(path), documentOptions: new() { CommentHandling = JsonCommentHandling.Skip })!;
        static HashSet<string> Set(JsonNode? node) => [.. node!.AsArray().Select(x => (string)x!)];
        static Dictionary<string, LabelSpec> Labels(JsonNode? node) => node!.AsObject().ToDictionary(x => x.Key, x => new LabelSpec((string)x.Value!["color"]!, (string)x.Value!["description"]!));
        return new Config
        {
            Model = (string)json["model"]!,
            Effort = (string)json["effort"]!,
            MaxDiffChars = (int)json["maxDiffChars"]!,
            StaleDays = (int)json["staleDays"]!,
            NoReviewerDays = (int)json["noReviewerDays"]!,
            AreaShare = (double)json["areaShare"]!,
            NudgeReviewers = (bool)json["nudgeReviewers"]!,
            NudgeAuthors = (bool)json["nudgeAuthors"]!,
            Digest = (bool)json["digest"]!,
            DigestMentionTeam = (string)json["digestMentionTeam"]!,
            ReviewerAssociations = Set(json["reviewerAssociations"]),
            PrTypes = Set(json["prTypes"]),
            IssueTypes = Set(json["issueTypes"]),
            Platforms = Set(json["platforms"]),
            CompanionAreas = Set(json["companionAreas"]),
            ClaudeOnlyAreas = Set(json["claudeOnlyAreas"]),
            StatusLabels = Labels(json["statusLabels"]),
            OtherLabels = Labels(json["otherLabels"]),
            Areas = [.. json["areas"]!.AsArray().Select(x => (new Regex((string)x![0]!, RegexOptions.IgnoreCase), (string?)x[1]))],
        };
    }
}

record LabelSpec(string Color, string Description);
record Review(string Author, string Association, string State, DateTimeOffset At);
record LabelEvent(string Label, string By, DateTimeOffset At);
record Comment(string Author, DateTimeOffset At, string Body);
record PrFile(string Path, string? PreviousPath, string Status, int Additions, int Deletions, string? Patch);
record IssueInfo(int Number, string Title, string Body, string Author, bool AuthorIsBot, string Association, List<string> Labels, List<Comment> Comments);
record SearchResult(int Number, string State, string Title, string Body);

sealed record Pr(
    int Number, string Title, string Body, string Author, bool IsDraft, string Mergeable, string? ReviewDecision,
    string? Ci, bool CiNeedsApproval, List<string> Labels, List<string> Assignees, List<string> ReviewRequests, List<string> TeamReviewRequests,
    List<Review> Reviews, List<Comment> Comments, List<Comment> BotComments, List<LabelEvent> LabelEvents, DateTimeOffset CreatedAt, DateTimeOffset ReadyAt,
    DateTimeOffset LastPush)
{
    // Pushes, comments and review thread replies of the author (a reply in a review thread is a review on GitHub)
    public DateTimeOffset LastAuthorActivity => Max(LastPush, CreatedAt,
        Comments.Where(x => x.Author == Author).Select(x => x.At), Reviews.Where(x => x.Author == Author).Select(x => x.At));

    public DateTimeOffset LastHumanActivity => Max(LastPush, ReadyAt, Comments.Select(x => x.At), Reviews.Select(x => x.At));

    static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b, params IEnumerable<DateTimeOffset>[] more)
        => more.SelectMany(x => x).Append(a).Append(b).Max();
}

sealed class GitHub
{
    const string PrFields = """
        number title body isDraft createdAt mergeable reviewDecision
        author { login __typename }
        labels(first: 50) { nodes { name } }
        assignees(first: 10) { nodes { login } }
        reviewRequests(first: 20) { nodes { requestedReviewer { __typename ... on User { login } ... on Team { slug } } } }
        commits(last: 1) { nodes { commit { committedDate statusCheckRollup { state } checkSuites(first: 40, filterBy: { appId: 15368 }) { nodes { conclusion } } } } }
        timelineItems(last: 100, itemTypes: [ISSUE_COMMENT, PULL_REQUEST_REVIEW, HEAD_REF_FORCE_PUSHED_EVENT, PULL_REQUEST_COMMIT, READY_FOR_REVIEW_EVENT]) {
          nodes {
            __typename
            ... on IssueComment { createdAt body author { login __typename } }
            ... on PullRequestReview { submittedAt state authorAssociation author { login __typename } }
            ... on HeadRefForcePushedEvent { createdAt }
            ... on PullRequestCommit { commit { committedDate } }
            ... on ReadyForReviewEvent { createdAt }
          }
        }
        labelEvents: timelineItems(last: 20, itemTypes: [LABELED_EVENT]) {
          nodes { ... on LabeledEvent { createdAt label { name } actor { login __typename } } }
        }
        """;

    readonly string owner, name;
    readonly HttpClient http;

    public GitHub(string repo)
    {
        (owner, name) = (repo.Split('/')[0], repo.Split('/')[1]);
        var token = Environment.GetEnvironmentVariable("GH_TOKEN") ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? GhAuthToken();
        http = new HttpClient { BaseAddress = new Uri("https://api.github.com/") };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        http.DefaultRequestHeaders.UserAgent.ParseAdd("stride-triage");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    }

    // Local runs use the gh CLI login
    static string GhAuthToken()
    {
        using var process = Process.Start(new ProcessStartInfo("gh", "auth token") { RedirectStandardOutput = true })!;
        var token = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        return token.Length > 0 ? token : throw new InvalidOperationException("No GH_TOKEN, and `gh auth token` returned nothing.");
    }

    public async Task<Pr?> PullRequest(int number)
    {
        var data = await GraphQL($"query($owner: String!, $name: String!, $number: Int!) {{ repository(owner: $owner, name: $name) {{ pullRequest(number: $number) {{ {PrFields} }} }} }}",
            new { owner, name, number });
        return data["repository"]?["pullRequest"] is { } node ? ParsePr(node) : null;
    }

    public async Task<List<Pr>> OpenPullRequests()
    {
        var result = new List<Pr>();
        string? after = null;
        do
        {
            var data = await GraphQL($"query($owner: String!, $name: String!, $after: String) {{ repository(owner: $owner, name: $name) {{ pullRequests(states: OPEN, first: 20, after: $after) {{ pageInfo {{ hasNextPage endCursor }} nodes {{ {PrFields} }} }} }} }}",
                new { owner, name, after });
            var page = data["repository"]!["pullRequests"]!;
            result.AddRange(page["nodes"]!.AsArray().Select(x => ParsePr(x!)));
            after = page["pageInfo"]!["hasNextPage"]!.GetValue<bool>() ? (string?)page["pageInfo"]!["endCursor"] : null;
        }
        while (after != null);
        return result;
    }

    static Pr ParsePr(JsonNode node)
    {
        var author = (string?)node["author"]?["login"] ?? "ghost";
        var commit = node["commits"]?["nodes"]?.AsArray().FirstOrDefault()?["commit"];
        var items = node["timelineItems"]!["nodes"]!.AsArray().OfType<JsonNode>().ToList();
        var reviews = new List<Review>();
        var comments = new List<Comment>();
        var botComments = new List<Comment>();
        var createdAt = Date(node["createdAt"]);
        var readyAt = createdAt;
        var lastPush = commit?["committedDate"] is { } committed ? Date(committed) : createdAt;
        foreach (var item in items)
        {
            switch ((string?)item["__typename"])
            {
                case "IssueComment":
                    var comment = new Comment((string?)item["author"]?["login"] ?? "ghost", Date(item["createdAt"]), (string?)item["body"] ?? "");
                    (IsBot(item["author"]) ? botComments : comments).Add(comment);
                    break;
                case "PullRequestReview" when item["submittedAt"] is { } submitted && !IsBot(item["author"]):
                    reviews.Add(new Review((string?)item["author"]?["login"] ?? "ghost", (string?)item["authorAssociation"] ?? "NONE", (string)item["state"]!, Date(submitted)));
                    break;
                case "HeadRefForcePushedEvent":
                    lastPush = Max(lastPush, Date(item["createdAt"]));
                    break;
                case "PullRequestCommit":
                    lastPush = Max(lastPush, Date(item["commit"]!["committedDate"]));
                    break;
                case "ReadyForReviewEvent":
                    readyAt = Max(readyAt, Date(item["createdAt"]));
                    break;
            }
        }
        // Labels people set (the workflow's own changes are by a bot)
        var labelEvents = node["labelEvents"]!["nodes"]!.AsArray().OfType<JsonNode>()
            .Where(x => x["label"] != null && !IsBot(x["actor"]))
            .Select(x => new LabelEvent((string)x["label"]!["name"]!, (string?)x["actor"]?["login"] ?? "ghost", Date(x["createdAt"]))).ToList();
        var requested = node["reviewRequests"]!["nodes"]!.AsArray().Select(x => x!["requestedReviewer"]).OfType<JsonNode>().ToList();
        return new Pr(
            Number: (int)node["number"]!,
            Title: (string)node["title"]!,
            Body: (string?)node["body"] ?? "",
            Author: author,
            IsDraft: (bool)node["isDraft"]!,
            Mergeable: (string)node["mergeable"]!,
            ReviewDecision: (string?)node["reviewDecision"],
            Ci: (string?)commit?["statusCheckRollup"]?["state"],
            // The rollup ignores fork PR workflow runs that wait for a maintainer's approval
            CiNeedsApproval: commit?["checkSuites"]?["nodes"]?.AsArray().Any(x => (string?)x?["conclusion"] == "ACTION_REQUIRED") == true,
            Labels: [.. node["labels"]!["nodes"]!.AsArray().Select(x => (string)x!["name"]!)],
            Assignees: [.. node["assignees"]!["nodes"]!.AsArray().Select(x => (string)x!["login"]!)],
            ReviewRequests: [.. requested.Where(x => (string?)x["__typename"] == "User").Select(x => (string)x["login"]!)],
            TeamReviewRequests: [.. requested.Where(x => (string?)x["__typename"] == "Team").Select(x => (string)x["slug"]!)],
            Reviews: reviews, Comments: comments, BotComments: botComments, LabelEvents: labelEvents,
            CreatedAt: createdAt, ReadyAt: readyAt, LastPush: lastPush);
    }

    public async Task<List<PrFile>> PullRequestFiles(int number)
    {
        var result = new List<PrFile>();
        for (var page = 1; page <= 30; page++)
        {
            var files = (await Rest(HttpMethod.Get, $"repos/{owner}/{name}/pulls/{number}/files?per_page=100&page={page}"))!.AsArray();
            result.AddRange(files.Select(x => new PrFile((string)x!["filename"]!, (string?)x["previous_filename"], (string)x["status"]!, (int)x["additions"]!, (int)x["deletions"]!, (string?)x["patch"])));
            if (files.Count < 100)
                break;
        }
        return result;
    }

    // GitHub refuses the diff of a very large PR: the patches of the file list are the fallback
    public async Task<string> PullRequestDiff(int number, List<PrFile> files)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"repos/{owner}/{name}/pulls/{number}");
        request.Headers.Accept.Clear();
        request.Headers.Accept.ParseAdd("application/vnd.github.diff");
        using var response = await http.SendAsync(request);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadAsStringAsync();
        return string.Join("\n", files.Select(x => $"--- {x.Path}\n{x.Patch ?? "(no patch: binary or too large)"}"));
    }

    public async Task<IssueInfo?> Issue(int number)
    {
        var data = await GraphQL("""
            query($owner: String!, $name: String!, $number: Int!) {
              repository(owner: $owner, name: $name) {
                issue(number: $number) {
                  number title body authorAssociation
                  author { login __typename }
                  labels(first: 50) { nodes { name } }
                  comments(last: 50) { nodes { createdAt body author { login __typename } } }
                }
              }
            }
            """, new { owner, name, number });
        return data["repository"]?["issue"] is { } node ? ParseIssue(node) : null;
    }

    // Without their comments: the backfill only labels, from the issue text
    public async Task<List<IssueInfo>> OpenIssues()
    {
        var result = new List<IssueInfo>();
        string? after = null;
        do
        {
            var data = await GraphQL("""
                query($owner: String!, $name: String!, $after: String) {
                  repository(owner: $owner, name: $name) {
                    issues(states: OPEN, first: 50, after: $after) {
                      pageInfo { hasNextPage endCursor }
                      nodes { number title body authorAssociation author { login __typename } labels(first: 50) { nodes { name } } }
                    }
                  }
                }
                """, new { owner, name, after });
            var page = data["repository"]!["issues"]!;
            result.AddRange(page["nodes"]!.AsArray().Select(x => ParseIssue(x!)));
            after = page["pageInfo"]!["hasNextPage"]!.GetValue<bool>() ? (string?)page["pageInfo"]!["endCursor"] : null;
        }
        while (after != null);
        return result;
    }

    static IssueInfo ParseIssue(JsonNode node) => new(
        (int)node["number"]!, (string)node["title"]!, (string?)node["body"] ?? "",
        (string?)node["author"]?["login"] ?? "ghost", IsBot(node["author"]), (string?)node["authorAssociation"] ?? "NONE",
        [.. node["labels"]!["nodes"]!.AsArray().Select(x => (string)x!["name"]!)],
        [.. (node["comments"]?["nodes"]?.AsArray() ?? []).Select(x => new Comment((string?)x!["author"]?["login"] ?? "ghost", Date(x["createdAt"]), (string?)x["body"] ?? ""))]);

    // Works for PRs too: on GitHub's REST API a PR is an issue
    public async Task<(bool Open, List<string> Labels)> IssueLabels(int number)
    {
        var issue = (await Rest(HttpMethod.Get, $"repos/{owner}/{name}/issues/{number}"))!;
        return ((string?)issue["state"] == "open", [.. issue["labels"]!.AsArray().Select(x => (string)x!["name"]!)]);
    }

    // A few searches from the title words: GitHub search requires every word, so one search with all of them finds little
    public async Task<List<SearchResult>> SearchIssues(int exclude, string title)
    {
        var words = Regex.Matches(title.ToLowerInvariant(), @"[a-z0-9][a-z0-9._#+-]{2,}").Select(x => x.Value)
            .Where(x => !StopWords.Contains(x)).Distinct().OrderByDescending(x => x.Length).Take(4).ToList();
        var queries = new List<string>();
        if (words.Count >= 3) queries.Add(string.Join(" ", words.Take(3)));
        if (words.Count >= 2) queries.Add(string.Join(" ", words.Take(2)));
        queries.AddRange(words.Take(2));

        var result = new Dictionary<int, SearchResult>();
        foreach (var query in queries.Distinct())
        {
            var data = await Rest(HttpMethod.Get, $"search/issues?per_page=8&q={Uri.EscapeDataString($"repo:{owner}/{name} is:issue {query}")}");
            foreach (var item in data!["items"]!.AsArray())
            {
                var number = (int)item!["number"]!;
                if (number != exclude)
                    result.TryAdd(number, new SearchResult(number, (string)item["state"]!, (string)item["title"]!, (string?)item["body"] ?? ""));
            }
            if (result.Count >= 15)
                break;
        }
        return [.. result.Values.Take(15)];
    }

    static readonly HashSet<string> StopWords = ["the", "and", "for", "not", "with", "when", "from", "does", "doesn't", "can't", "cannot", "after", "into", "that", "this", "are", "using", "use", "stride", "issue", "bug", "error", "feature", "request", "support", "add", "should", "would", "could", "have", "has", "been", "but", "all", "any", "its", "work", "working", "works"];

    public async Task<(int Number, string Body)?> DigestIssue()
    {
        var data = await Rest(HttpMethod.Get, $"repos/{owner}/{name}/issues?labels=triage-digest&state=open&per_page=5");
        return data!.AsArray().FirstOrDefault() is { } issue ? ((int)issue["number"]!, (string?)issue["body"] ?? "") : null;
    }

    public async Task<HashSet<string>> Labels()
    {
        var result = new HashSet<string>();
        for (var page = 1; ; page++)
        {
            var labels = (await Rest(HttpMethod.Get, $"repos/{owner}/{name}/labels?per_page=100&page={page}"))!.AsArray();
            result.UnionWith(labels.Select(x => (string)x!["name"]!));
            if (labels.Count < 100)
                return result;
        }
    }

    public Task CreateLabel(string label, string color, string description) => Rest(HttpMethod.Post, $"repos/{owner}/{name}/labels", new { name = label, color, description });
    public Task AddLabels(int number, List<string> labels) => Rest(HttpMethod.Post, $"repos/{owner}/{name}/issues/{number}/labels", new { labels });
    public Task RemoveLabel(int number, string label) => Rest(HttpMethod.Delete, $"repos/{owner}/{name}/issues/{number}/labels/{Uri.EscapeDataString(label)}");
    public Task Comment(int number, string body) => Rest(HttpMethod.Post, $"repos/{owner}/{name}/issues/{number}/comments", new { body });
    public Task CreateIssue(string title, string body, List<string> labels) => Rest(HttpMethod.Post, $"repos/{owner}/{name}/issues", new { title, body, labels });
    public Task UpdateIssueBody(int number, string body) => Rest(HttpMethod.Patch, $"repos/{owner}/{name}/issues/{number}", new { body });

    async Task<JsonNode> GraphQL(string query, object variables)
    {
        var result = (await Rest(HttpMethod.Post, "graphql", new { query, variables }))!;
        if (result["errors"] is JsonArray { Count: > 0 } errors)
            throw new InvalidOperationException($"GraphQL: {errors.ToJsonString()}");
        return result["data"]!;
    }

    async Task<JsonNode?> Rest(HttpMethod method, string path, object? body = null)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(method, path);
            if (body != null)
                request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            using var response = await http.SendAsync(request);
            // Rate limited (the search API allows 30 calls a minute): wait for the reset, then try again
            if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests && attempt < 3
                && response.Headers.TryGetValues("x-ratelimit-remaining", out var remaining) && remaining.First() == "0"
                && response.Headers.TryGetValues("x-ratelimit-reset", out var reset))
            {
                var wait = DateTimeOffset.FromUnixTimeSeconds(long.Parse(reset.First())) - DateTimeOffset.UtcNow;
                if (wait < TimeSpan.FromMinutes(2))
                {
                    await Task.Delay(wait > TimeSpan.Zero ? wait + TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(1));
                    continue;
                }
            }
            return await Read(method, path, response);
        }
    }

    static async Task<JsonNode?> Read(HttpMethod method, string path, HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        // Removing a label the issue does not have is not an error for us
        if (method == HttpMethod.Delete && response.StatusCode == HttpStatusCode.NotFound)
            return null;
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"{method} {path}: {(int)response.StatusCode} {text}");
        return text.Length > 0 ? JsonNode.Parse(text) : null;
    }

    static bool IsBot(JsonNode? author) => (string?)author?["__typename"] == "Bot" || ((string?)author?["login"])?.EndsWith("[bot]", StringComparison.Ordinal) == true;
    static DateTimeOffset Date(JsonNode? node) => DateTimeOffset.Parse((string)node!);
    static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;
}
