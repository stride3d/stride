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

A short "Under the hood" section at the end can carry internal work worth a mention — keep
even that readable.

# Scope

$SCOPE_NOTE

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
  refactors with **no** user-visible effect. Omit these, or fold them into a single terse
  "Under the hood" line — never one bullet each.

Group related commits into **one** entry. A multi-commit feature is one highlight, not ten
bullets.

# Structure

- Lead with **Highlights** only if there are standout user-facing changes (1–3, each a short
  paragraph on the benefit). For a small release, skip Highlights and just use a categorized
  list.
- Categorized sections by area, using emoji headers where they fit, e.g.:
  `🖥️ Editor / Game Studio`, `🎮 Graphics & Shaders`, `🧨 Physics`, `🔊 Audio`,
  `🐧 Cross-platform / CLI / Build`. Use headers that match this product's actual changes.
- A short `🔧 Under the hood` catch-all for internal work worth a mention.
- End with exactly:
  `**Full Changelog**: https://github.com/$REPO/compare/$PREV...$TAG`

Match Stride's house style: crisp bullets, user-impact framing, no walls of text. If there
are genuinely no user-facing changes in scope, say so in one line.

# Prioritize by user impact

Order by how much a change matters to a user of $PRODUCT — **not** by commit order or diff
size. Critically: a commit is often a **follow-up or hardening of a feature that already
shipped in an earlier release**. Treat that as a minor fix, not a highlight. If you're
unsure whether something is new *in this release*, check history before `$PREV`
(`git log` earlier, or `gh release view <earlier-tag>`).

# References — strict

- Cite `#NNNN` **only** when that exact number appears verbatim in the commit's own message
  (subject or body) as a real PR/issue reference. The number must come from the commit text
  itself — never derive one from a commit's position in a list, its ordering, or your own
  enumeration. (Genuinely low numbers are fine: Stride has old references like `#1020` or
  `#1577`; keep them when the commit cites them.)
- For a direct commit with no PR/issue, link the **short commit SHA**:
  `https://github.com/$REPO/commit/<sha>` — or leave it unreferenced. Never invent a number.

# Rules

- Don't fabricate. If, after checking its diff, you still can't tell what a commit does for
  the user, omit it rather than guess.
- Be concise: fewer, well-written bullets beat an exhaustive dump.
- Output **only** the Markdown release notes — no preamble, no description of your process.
  Begin your response directly with the first heading.
