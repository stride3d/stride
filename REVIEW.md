# Pull request review

Stride is a game engine, so reviews look closely at correctness and performance. Follow these rules when you review a pull request, together with the rules of [AGENTS.md](AGENTS.md) and of the `AGENTS.md` files in the folders the pull request changes.

- Start with a short overview of the pull request that names its most important changes.
- Focus on logic, safety, performance, and consistency with the existing code. Look at the patterns already used in Stride before you suggest something.
- Don't suggest large architectural changes.
- Comments on formatting, grammar or spelling are welcome. Small style comments are fine when they keep the code consistent.
- Don't review generated code, third-party code, binary files or assets.
- When you find a bug or a performance problem, suggest a concrete fix.
- For a large pull request (20 or more `.cs` files), don't do a full review: point out only critical or blocking problems.
- Check the rules of `AGENTS.md` that a diff can break: comments that tell the story of the change, a regular expression on XML or YAML, a public API break without a code upgrader, the copyright header of new files.

## XML documentation

- Check for missing, incomplete or wrong XML documentation comments on public types, methods, properties and fields.
- Give each XML comment suggestion as a separate item, so it can be accepted on its own.
- Don't rewrite existing XML comments unless they are wrong, unclear, or have grammar mistakes or typos.
- Suggested comments must describe what the code really does, match the style of the existing documentation, and use `<summary>`, `<param>`, `<returns>`, `<exception>` and `<remarks>` where they help.

The goal is few comments, each one useful and easy to act on.
