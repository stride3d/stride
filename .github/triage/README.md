# Triage

`triage.cs` labels PRs and issues for `.github/workflows/triage.yml`. It is a .NET file-based app: `dotnet run .github/triage/triage.cs -- <command>`.

## What it does

**New PRs** (when opened, or marked ready for review), once:

- Area labels (`area-*`): from the changed paths (`areas` in `config.json`, weighted by changed lines), then Claude keeps or corrects them, for example a refactor across many folders, or the plugin system.
- Type label: `bug-fix`, `enhancement` or `engineering`.
- Flags, only when the diff shows it: `breaking-change`, `behavioral-change`, `performance`. These sort the release notes.

A PR that already has an area and a type label is left alone. A label a maintainer set or removed is never touched again.

**Whose turn** on every ready PR (on PR events, comments, every push to master, and every 10 minutes). Drafts get none of these:

| Label | When |
|---|---|
| `waiting-on-author` | Merge conflicts, or a maintainer reviewed after the author's last push or reply. |
| `waiting-on-review` | No review yet, or the author pushed or replied after the last review. |
| `ready-to-merge` | The last review is an approval, CI is green, no conflicts. |

After a push that does not fix everything, the simplest is a one-line review (Review changes, Comment, "still missing X"): it notifies the author, and the PR goes to `waiting-on-author` by itself. A plain PR comment does not change the label. A status label a maintainer sets by hand also counts like a review, for when there is nothing to say (the author already said more commits are coming): it stays until the author's next push or reply, or the next review. Label changes notify nobody. A reply in a review thread counts as a reply. Maintainers are the `reviewerAssociations` in `config.json` (owners, members, collaborators). A review by a bot counts only for the `reviewerBots` (`stride-review`, which posts reviews a maintainer checked).

**Merge conflicts**: `has-conflicts` on every PR that has conflicts with its base branch, drafts too, in the same runs. GitHub notifies nobody when changes merged in the base give a PR conflicts, so with `nudgeConflicts`, once the label is `conflictNudgeHours` (24) hours old, a comment asks the author to rebase or merge the base. One comment while the label stays on: new conflicts on top of conflicts not solved yet get none, and the next comment comes only after the conflicts are solved and come back, at most one every `conflictNudgeMinDays` (7) days. Not on drafts or PRs by bots; a draft marked ready gets its 24 hours from then. Maintainers' PRs get it too: GitHub does not notify them either.

A force push that only rebased the PR (common in PR stacks) is not the author's activity: the last review, or a status label set by hand after it, still holds. So an approved PR stays approved, and a PR waiting on its author stays there. Only rebased means the same commit messages in the same order and the same added and removed lines, on the base branch as it is now or was before one of its force pushes. A commit added, dropped, squashed or edited, a conflict fixed during the rebase, a reply from the author, or more than 20 commits: the push counts as before.

**New issues**:

- An area label if there is none, and the type (`bug`, `enhancement`, `question`) when the reporter picked the wrong template.
- For a bug from someone who is not a maintainer: a check for the version, platform, graphics API (rendering only), repro steps and a log (crashes only). When something is missing: one comment that asks for it, and `needs-info`. When the reporter edits the issue or replies, it checks again and removes `needs-info`. It never posts a second comment.
- Up to 3 possible duplicates from a search of existing issues, in that same comment.
- `regression` on a bug that the reporter says worked in an earlier Stride version: the first list to check before a patch release.

**Every Monday** (off until turned on in `config.json`):

- `nudgeReviewers`: a PR waiting on a review for `staleDays` (30) days, with a requested reviewer or an assignee: one comment asking them if they still plan to review it, or to unassign themselves.
- `nudgeAuthors`: a PR waiting on the author for 30 days: one comment asking if they plan to continue.
- `digest`: an issue labeled `triage-digest` (created on the first run, pin it), rewritten every week: PRs with no reviewer for `noReviewerDays` (7) days, approved PRs not merged yet, and PRs quiet for 30 days. `digestMentionTeam` (for example `stride3d/stride-contributors`) also mentions that team when new PRs without a reviewer show up.

One nudge per quiet period: it waits for new activity before it can nudge again. It never closes anything.

Turning nudges off: the `no-nudge` label stops all of them on a PR. Contributors cannot add labels, so when the PR author replies `no-nudge` (alone on a line), the workflow adds the label. `noNudgeAuthors` in `config.json` stops the nudges to these authors (conflicts, quiet PRs) on all their PRs, for maintainers who follow their PRs another way. The nudges on a maintainer's PR link to it.

Claude also adds `platform-Android`, `platform-iOS`, `platform-Linux` or `platform-macOS` to PRs and issues that are specific to a platform. It only adds them, never removes one.

The labels this workflow uses but the repo does not have yet (the status labels, `has-conflicts`, `needs-info`, `regression`, `area-Animation`, the platform labels) are created the first time it needs them. `area-Navigation` is the renamed `area-AI`.

## Safety

- The workflow uses `pull_request_target` to label fork PRs, so it runs from the base branch and never checks out or runs PR code. It reads PRs and issues through the API.
- Claude returns structured output: labels from fixed lists, missing-info keys, and issue numbers it was given. The tool checks each value again, and builds every comment from fixed sentences. The text of a PR or issue cannot make it post anything else.

## Running it locally

`gh auth login` gives the tool a token. `--dry-run` reports what it would do and changes nothing. `--no-ai` skips the Claude calls (no `ANTHROPIC_API_KEY` needed). `--claude-cli` makes the same Claude calls through the local `claude` CLI and its login instead of an API key (same prompts and schema, no tools, safe mode).

```
dotnet run .github/triage/triage.cs -- status --dry-run          # whose-turn and conflict labels of all open PRs
dotnet run .github/triage/triage.cs -- pr <N> --dry-run           # labels of one PR
dotnet run .github/triage/triage.cs -- issue <N> --dry-run        # triage of one issue
dotnet run .github/triage/triage.cs -- weekly --dry-run          # nudges and digest
```

## Backfill

The open PRs and issues from before the workflow have no labels yet. `backfill` classifies the ones without an area or type label and changes nothing: it writes a plan (one JSON line per item, so a rerun continues where it stopped) and a Markdown report to check, split in parts that fit in a GitHub comment. `apply` adds the plan's labels later, and skips items closed since or where someone set an area or a type. Old issues only get labels: no comment, no `needs-info`, and an existing type stays.

```
dotnet run .github/triage/triage.cs -- backfill plan.jsonl --claude-cli --parallel 3
dotnet run .github/triage/triage.cs -- apply plan.jsonl --dry-run
dotnet run .github/triage/triage.cs -- apply plan.jsonl
```

`regressions` does the same for the `regression` label alone, which came after the first backfill: it checks the open `bug` issues that do not have it, created since `--since` (for example the last minor release).

```
dotnet run .github/triage/triage.cs -- regressions regressions.jsonl --since 2025-11-14 --claude-cli --parallel 3
```

On GitHub, the repo variable `TRIAGE_DRY_RUN=true` makes every run report in its job summary instead of changing anything, and "Run workflow" runs any command (dry run by default).
