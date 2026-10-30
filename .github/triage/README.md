# Triage

`triage.cs` labels PRs and issues for `.github/workflows/triage.yml`. It is a .NET file-based app: `dotnet run .github/triage/triage.cs -- <command>`.

## What it does

**New PRs** (when opened, or marked ready for review), once:

- Area labels (`area-*`): from the changed paths (`areas` in `config.json`, weighted by changed lines), then Claude keeps or corrects them, for example a refactor across many folders, or the plugin system.
- Type label: `bug-fix`, `enhancement` or `engineering`.
- Flags, only when the diff shows it: `breaking-change`, `behavioral-change`, `performance`. These sort the release notes.

A PR that already has an area and a type label is left alone. A label a maintainer set or removed is never touched again.

**Whose turn** on every ready PR (on PR events, comments, and every hour). Drafts get none of these:

| Label | When |
|---|---|
| `waiting-on-author` | Merge conflicts, or a maintainer reviewed after the author's last push or reply. |
| `waiting-on-review` | No review yet, or the author pushed or replied after the last review. |
| `ready-to-merge` | The last review is an approval, CI is green, no conflicts. |

A reply in a review thread counts as a reply. Maintainers are the `reviewerAssociations` in `config.json` (owners, members, collaborators).

**New issues**:

- An area label if there is none, and the type (`bug`, `enhancement`, `question`) when the reporter picked the wrong template.
- For a bug from someone who is not a maintainer: a check for the version, platform, graphics API (rendering only), repro steps and a log (crashes only). When something is missing: one comment that asks for it, and `needs-info`. When the reporter edits the issue or replies, it checks again and removes `needs-info`. It never posts a second comment.
- Up to 3 possible duplicates from a search of existing issues, in that same comment.

**Every Monday** (off until turned on in `config.json`):

- `nudgeReviewers`: a PR waiting on a review for `staleDays` (30) days, with a requested reviewer or an assignee: one comment asking them if they still plan to review it, or to unassign themselves.
- `nudgeAuthors`: a PR waiting on the author for 30 days: one comment asking if they plan to continue.
- `digest`: an issue labeled `triage-digest` (created on the first run, pin it), rewritten every week: PRs with no reviewer for `noReviewerDays` (7) days, approved PRs not merged yet, and PRs quiet for 30 days. `digestMentionTeam` (for example `stride3d/stride-contributors`) also mentions that team when new PRs without a reviewer show up.

One nudge per quiet period: it waits for new activity before it can nudge again. The `no-nudge` label stops it on a PR. It never closes anything.

Claude also adds `platform-Android`, `platform-iOS`, `platform-Linux` or `platform-macOS` to PRs and issues that are specific to a platform. It only adds them, never removes one.

The labels this workflow uses but the repo does not have yet (the status labels, `needs-info`, `area-Animation`, the platform labels) are created the first time it needs them. `area-Navigation` is the renamed `area-AI`: rename it on GitHub when this workflow is merged (`gh label edit area-AI --name area-Navigation`), so the existing issues keep it.

## Safety

- The workflow uses `pull_request_target` to label fork PRs, so it runs from the base branch and never checks out or runs PR code. It reads PRs and issues through the API.
- Claude returns structured output: labels from fixed lists, missing-info keys, and issue numbers it was given. The tool checks each value again, and builds every comment from fixed sentences. The text of a PR or issue cannot make it post anything else.

## Running it locally

`gh auth login` gives the tool a token. `--dry-run` reports what it would do and changes nothing. `--no-ai` skips the Claude calls (no `ANTHROPIC_API_KEY` needed). `--claude-cli` makes the same Claude calls through the local `claude` CLI and its login instead of an API key (same prompts and schema, no tools, safe mode).

```
dotnet run .github/triage/triage.cs -- status --dry-run          # whose-turn labels of all open PRs
dotnet run .github/triage/triage.cs -- pr <N> --dry-run           # labels of one PR
dotnet run .github/triage/triage.cs -- issue <N> --dry-run        # triage of one issue
dotnet run .github/triage/triage.cs -- weekly --dry-run          # nudges and digest
```

On GitHub, the repo variable `TRIAGE_DRY_RUN=true` makes every run report in its job summary instead of changing anything, and "Run workflow" runs any command (dry run by default).
