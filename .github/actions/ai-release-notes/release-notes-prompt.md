# Task

You are generating the **GitHub release notes** for a new release of **$PRODUCT**, part of
**Stride**, an open-source C#/.NET game engine (https://github.com/$REPO). You have
read-only access to the checked-out repository and its full git history. Produce polished,
user-facing release notes in Markdown.

# Audience & voice

Write primarily for **the people who use $PRODUCT** — game developers, not engine internals
experts. Describe what changed for *them*: features, behavior, fixes. Say what a change lets
them do or stops going wrong, not how it was implemented.

Use **plain, simple English** and keep it **high level**:

- Avoid internal jargon, engine-implementation terms, and dependency name-drops the reader
  won't recognize. For example, don't write things like "quiesce", "mtime-LRU GC",
  "platform heads", "apphost", or "NRE" — use everyday words instead ("shut down cleanly",
  "reuses cached build results", "each platform's build", "crash").
- Domain terms that are standard for the area a change touches are fine when they aid
  clarity — a GPU/graphics fix may say "command buffer" or "shader"; a physics fix may say
  "collider" or "inertia". The test is whether a developer working in that area would
  recognize the term, not whether the text is entirely jargon-free.
- If a change is genuinely technical, describe its **effect** in ordinary language rather
  than naming the mechanism. When a precise term is truly unavoidable, add a few plain words
  of explanation.
- Prefer a short, concrete description of the user-visible benefit over an accurate but
  opaque one. When in doubt, go simpler and higher-level.
- Punctuation: prefer commas or periods over em dashes; don't lean on `—`. An occasional
  line break is fine, even inside a bullet.

Pure-internal work worth a mention goes under a `### 🔧 Under the hood` subheading (alongside
the other area subheadings, at the end) as several separate bullets, one per grouped topic
(never one bullet per commit, never a single comma- or semicolon-joined bullet). Keep even
that readable.

# Scope

$SCOPE_NOTE

# Authoritative change list

$GH_CHANGELOG

# Input & how to investigate

This release covers the commit range `$PREV..$TAG`. Begin with:

    $LOG_CMD

Then dig in **selectively**:

- `git show <sha>` or `git diff <sha>^ <sha>` for any commit whose subject is too vague to
  summarize confidently.
- `grep` / read source to confirm which subsystem a change touches, or what a symbol is.

Only open diffs for commits you can't already summarize from the message — don't ingest
every diff. Reading a handful of ambiguous ones is expected; dumping all of them is not.

# Filter — keep only what a user cares about

Drop or collapse noise:

- WIP / review-churn ("Address the review", "fix per review", "no tests yet").
- Merge commits and "(cherry picked from …)" artifacts.
- Pure test-coverage, CI/build plumbing, formatting, dependency bookkeeping, and internal
  refactors with **no** user-visible effect. Omit these, or fold them into several separate
  "Under the hood" bullets — never one bullet per commit, and never a single comma- or
  semicolon-joined bullet.

Group related commits into **one** entry. A multi-commit feature is one highlight, not ten
bullets.

# Output format

$FORMAT_INSTRUCTION

Match Stride's house style: crisp, user-impact framing, no walls of text. If there are
genuinely no user-facing changes in scope, say so in one line. The workflow adds the top-level
section heading and appends the "New Contributors" and "Full Changelog" parts, so do not emit
a document title, those two sections, or any closing commentary. Follow the format instruction
above for the internal headings and structure.

Categorize each change by the subsystem it affects, not by which tool a developer used to
reach it: reserve `🖥️ Editor / Game Studio` for changes to the editor application itself. A
runtime or engine change goes under its own area (Graphics, Physics, Content & Assets, etc.),
even if a developer might first notice it inside Game Studio — for example, an asset-loading
API change belongs with content/assets, not Editor.

# Prioritize by user impact

Order by how much a change matters to a user of $PRODUCT — **not** by commit order or diff
size. Critically: a commit is often a **follow-up or hardening of a feature that already
shipped in an earlier release**. Treat that as a minor fix, not a highlight. If you're
unsure whether something is new *in this release*, check history before `$PREV`
(`git log` earlier, or `gh release view <earlier-tag>`).

# References — strict

- When the **authoritative change list** above is present, treat it as the source of truth
  for PR references and author handles. In `bullets` output, end each bullet with
  `(@handle, <url>)`, where `<url>` is the pull-request URL copied verbatim from that list (the
  workflow shortens it to a `#NNNN` link) and `@handle` is that PR's author. Never invent,
  renumber, or reassign a handle, and never attach another change's PR to a bullet.
- For a change with only a direct commit (no PR), end the bullet with `(<commit-url>)` — the
  full `https://github.com/$REPO/commit/<sha>` URL, and **no** handle (the workflow shortens
  the URL and fills in the author). When several commits belong to one entry, list each:
  `(<commit-url>, <commit-url>)`. Never invent a commit.
- Any `#NNNN` you mention *inside* a bullet's wording must appear verbatim in that commit's own
  message; never derive one from list position or invent it. (Old low numbers like `#1020` are
  fine when the commit cites them.)

# Rules

- Don't fabricate. If, after checking its diff, you still can't tell what a commit does for
  the user, omit it rather than guess.
- Don't inflate scale. Reserve words like "new", "overhaul", "rewrite", "major", or
  "redesign" for changes whose diff clearly shows that scope. When unsure, prefer modest
  framing ("improved", "fixed", "extended"). A follow-up or hardening of a feature that
  already shipped in an earlier release is a fix, not a new feature.
- Be concise: make every line count — the bullet list is exhaustive but each bullet stays
  tight, and the highlights are few and curated.
- Output **only** the Markdown — no preamble, no "here are the notes", no description of your
  process. Begin directly with the first `###` heading.
