# 🤝 Contributing

For questions and general discussions, please join our [Discord server](https://discord.gg/f6aerfE) or participate in [GitHub Discussions](https://github.com/stride3d/stride/discussions).

To report bugs or propose features, please use the [Issues](https://github.com/stride3d/stride/issues) section on GitHub.

We welcome code contributions via pull requests. Issues tagged with **[`good first issue`](https://github.com/stride3d/stride/labels/good%20first%20issue)** are great starting points for code contributions.

You can help us translate Stride; check out our [Localization Guide](https://doc.stride3d.net/latest/en/contributors/engine/localization.html).

## Triggering CI tests on a PR

Most CI runs automatically on PR commits, gated by path filters. A few things run only on demand;
opt in by adding a label to the PR (collaborator access required):

- `ci-enduser` — end-user sample screenshot + packaging suite (never auto-runs)
- `ci-editor` — GameStudio editor screenshot suite (never auto-runs)
- `ci-ios` — iOS game suite, when its narrow path filter didn't already trigger it
- `ci-android` — Android game suite, when its narrow path filter didn't already trigger it
- `ci-run-on-draft` — run the normal path-gated CI on a **draft** PR (which otherwise skips CI)

Labels run through the PR's own workflow ([`pr-label-suites.yml`](workflows/pr-label-suites.yml) /
[`main.yml`](workflows/main.yml)), so they use the PR head's YAML and re-run on every push while
applied. Removing a label re-runs with that suite skipped.

## Earn Money by Contributing

If you are a developer with solid experience in C#, rendering techniques, or game development, we want to hire you! We have allocated funds from supporters on [Open Collective](https://opencollective.com/stride3d) and can pay for work on certain projects. [More information is available here](https://doc.stride3d.net/latest/en/contributors/engine/bug-bounties.html).

