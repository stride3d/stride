# 🤝 Contributing

For questions and general discussions, please join our [Discord server](https://discord.gg/f6aerfE) or participate in [GitHub Discussions](https://github.com/stride3d/stride/discussions).

To report bugs or propose features, please use the [Issues](https://github.com/stride3d/stride/issues) section on GitHub.

We welcome code contributions via pull requests. Issues tagged with **[`good first issue`](https://github.com/stride3d/stride/labels/good%20first%20issue)** are great starting points for code contributions.

You can help us translate Stride; check out our [Localization Guide](https://doc.stride3d.net/latest/en/contributors/engine/localization.html).

## Triggering CI tests on a PR

Most CI runs automatically on PR commits. Two suites — editor screenshots and samples
screenshots — only run on demand because they're slow and drift-prone. A bot listens for
`/test` comments to dispatch them. Comment `/test help` on any PR for the full command list.

Quick examples:
- `/test editor samples` — run both screenshot suites
- `/test linux-game` — re-run a specific main-CI suite (useful for transient failures)
- `/test windows-game-vulkan` — re-run a specific graphics-API variant

Bot definition: [`.github/workflows/pr-test-chatops.yml`](workflows/pr-test-chatops.yml).
Only repo collaborators can trigger.

## Earn Money by Contributing

If you are a developer with solid experience in C#, rendering techniques, or game development, we want to hire you! We have allocated funds from supporters on [Open Collective](https://opencollective.com/stride3d) and can pay for work on certain projects. [More information is available here](https://doc.stride3d.net/latest/en/contributors/engine/bug-bounties.html).

