# 🤝 Contributing

For questions and general discussions, please join our [Discord server](https://discord.gg/f6aerfE) or participate in [GitHub Discussions](https://github.com/stride3d/stride/discussions).

To report bugs or propose features, please use the [Issues](https://github.com/stride3d/stride/issues) section on GitHub.

We welcome code contributions via pull requests. Issues tagged with **[`good first issue`](https://github.com/stride3d/stride/labels/good%20first%20issue)** are great starting points for code contributions.

You can help us translate Stride; check out our [Localization Guide](https://doc.stride3d.net/latest/en/contributors/engine/localization.html).

## Triggering CI tests on a PR

Most CI runs automatically on PR commits. A few suites — editor screenshots and end-user
screenshots — are slow and drift-prone, so they run only on demand. Arm them by adding a
`ci-force-<suite>` label to the PR (collaborator access required):

- `ci-force-enduser` — end-user sample screenshot + packaging suite
- `ci-force-editor` — GameStudio editor screenshot suite
- `ci-force-ios` — iOS game suite (when the path filter didn't already trigger it)

The labels run through the PR's own workflow ([`.github/workflows/pr-label-suites.yml`](workflows/pr-label-suites.yml)),
so they use the PR head's YAML. Removing a label re-runs with that suite skipped.

## Earn Money by Contributing

If you are a developer with solid experience in C#, rendering techniques, or game development, we want to hire you! We have allocated funds from supporters on [Open Collective](https://opencollective.com/stride3d) and can pay for work on certain projects. [More information is available here](https://doc.stride3d.net/latest/en/contributors/engine/bug-bounties.html).

