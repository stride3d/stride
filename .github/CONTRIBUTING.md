# 🤝 Contributing

For questions and general discussions, please join our [Discord server](https://discord.gg/f6aerfE) or participate in [GitHub Discussions](https://github.com/stride3d/stride/discussions).

To report bugs or propose features, please use the [Issues](https://github.com/stride3d/stride/issues) section on GitHub.

We welcome code contributions via pull requests. Issues tagged with **[`good first issue`](https://github.com/stride3d/stride/labels/good%20first%20issue)** are great starting points for code contributions.

You can help us translate Stride; check out our [Localization Guide](https://doc.stride3d.net/latest/en/contributors/engine/localization.html).

## Large changes: agree on the design first

Review time is limited, so a large change needs a design that the owner of the area agreed to, before the code. This applies to maintainers too, in areas that others own. Large means a new plugin or feature system, new public API in the core engine, a change to how a core system works, or more than about 1,000 to 2,000 hand-written lines. Bug fixes and small features don't need a design.

Write the design in an issue or a discussion. Keep it short, a page or two:

- The problem, and the other options you considered.
- The architecture, at a high level: the main parts and the data flow between them, where the work runs (CPU or GPU, which thread, every frame or at load) and how it scales, who owns which data, and which existing systems and extension points it plugs into. A diagram helps.
- What it changes in existing code, public API, serialization and asset formats.
- How it follows the patterns the engine already uses.
- Who maintains it afterwards.
- How it lands as pull requests small enough to review.

The owner of the area answers. If nobody answers within two weeks, or the area has no owner, the core team decides.

A prototype branch or a draft pull request is welcome in the discussion, to show the idea, but it is not reviewed as code until the design is agreed. Once it is, link the design in each pull request. A pull request too large to review properly will be asked to split, even with an agreed design.

## Plugins

Optional features go in plugins, not in the core engine assemblies. Stride has three levels:

- **Core**: the engine itself.
- **First-party plugins**: in this repository, under `sources/plugins`. They are released with the engine, and the team keeps them working in every release. Each one has an owner.
- **Community plugins**: in their author's own repository, released by them.

A new first-party plugin, or a large feature in one, needs a maintainer of that area who agrees to own it. Without an owner, it starts as a community plugin, and it can move into this repository later.

A first-party plugin is unmaintained when its owner steps down, or when a bug that breaks it or blocks a release stays unfixed by that release:

- It still ships, marked as unmaintained, while we look for a new owner. New features for it wait until it has one: you can offer to be that owner.
- Until then, the team only keeps it building. Whoever breaks its build fixes it.
- If it still has no owner at the next major or minor release (for example 4.6), it moves out of this repository as a community plugin, or it is dropped.

## Triggering CI tests on a PR

Most CI runs automatically on PR commits, gated by path filters. A few things run only on demand;
opt in by adding a label to the PR (collaborator access required):

- `ci-enduser` — end-user sample screenshot + packaging suite (never auto-runs)
- `ci-gamestudio-screenshots` — Game Studio screenshot suite (never auto-runs)
- `ci-ios` — iOS game suite, when its narrow path filter didn't already trigger it
- `ci-android` — Android game suite, when its narrow path filter didn't already trigger it
- `ci-run-on-draft` — run the normal path-gated CI on a **draft** PR (which otherwise skips CI)

Labels run through the PR's own workflow ([`pr-label-suites.yml`](workflows/pr-label-suites.yml) /
[`main.yml`](workflows/main.yml)), so they use the PR head's YAML. Adding a label runs that suite
right away, even on a draft; every push re-runs all applied suites (on a draft, only with
`ci-run-on-draft`). Removing a label only stops the re-runs.

## Earn Money by Contributing

If you are a developer with solid experience in C#, rendering techniques, or game development, we want to hire you! We have allocated funds from supporters on [Open Collective](https://opencollective.com/stride3d) and can pay for work on certain projects. [More information is available here](https://doc.stride3d.net/latest/en/contributors/engine/bug-bounties.html).

