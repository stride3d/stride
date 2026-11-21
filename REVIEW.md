# Pull request review

Stride is a game engine, so reviews look closely at correctness and performance. Follow these rules when you review a pull request, together with the rules of [AGENTS.md](AGENTS.md) and of the `AGENTS.md` files in the folders the pull request changes.

- Don't describe what the pull request does: the author knows, and its description says it. On a feature or a behavior change, start with your opinion of the change: does it fit Stride, is the approach right, and the main reason, in one or two sentences. Then give only the points that have no line to attach to: a problem found in many places, the design of the whole change, questions about scope, tests or docs. Don't repeat the line comments.
- Look at the patterns already used in Stride, and read the code around each change (callers, base classes, similar code elsewhere in the engine), before you comment or suggest something.
- Don't review generated code, third-party code, binary files or assets.
- For a large pull request (more than about 20 changed `.cs` files, or about 2,000 hand-written lines), review the design first, over the whole pull request: list its main parts, where their work runs and which existing systems they touch, and check them as in "Design" below. Then review the code for critical and important problems only. When the pull request is too large to review properly, say so and suggest a split along those parts.

## What to look for

In order of importance:

1. Correctness: logic errors, wrong conditions, null handling, races and thread safety, wrong disposal order, GPU or native resources that are never disposed, event handlers that are never removed.
2. Design: see "Design" below.
3. Compatibility:
   - A public API change that breaks user code: say clearly that it is breaking. When a machine can fix the user code, it needs a code upgrader (see [AGENTS.md](AGENTS.md) and "Upgraders" below).
   - Serialization: a change to a `[DataContract]` or `[DataMember]` type, or to a serializer. An asset format change needs an asset upgrader and an `[AssetFormatVersion]` bump. A binary serializer change may need a version bump, so old build caches are not reused.
   - Platforms: Windows, Linux, macOS, Android and iOS, and the graphics APIs (Direct3D 11, Direct3D 12, Vulkan). Code that works on only some of them, without a guard, is a problem.
4. Performance on hot paths (every frame, every draw, every entity): allocations, LINQ, boxing, closures, locks, repeated lookups.
5. Consistency with similar code nearby: patterns, naming and structure. Layering: engine, asset or editor code that knows about one plugin or optional package (a `using` of its namespace, a type name in a string, a special case for its types) is a problem even when it works. Name the extension point that would remove it. When that extension point does not exist yet, ask for a follow-up issue rather than a rewrite in this pull request.
6. Tests: behavior changes and bug fixes should have tests, when the area has tests.
7. XML documentation: see "XML documentation" below.
8. Small things: typos, the rules of `AGENTS.md` that a diff can break (comments that tell the story of the change, a regular expression on XML or YAML, `#region`, the copyright header of new files), and unrelated changes mixed in (formatting of untouched code, unrelated files).

## Design

For new features, new systems and changes to how a core system works, check:

- The agreed design: when the pull request links one, does the code follow it? Say where it departs.
- Where the work runs: CPU or GPU, which thread, every frame or at load. Look for work on the CPU that the engine already does on the GPU (for example, skinning runs in a compute shader), or work done one object at a time where the engine does one batch over all of them.
- Reuse: a second way to do what an existing system already does (its own update loop, buffer management or asset pipeline) instead of plugging into that system.
- Ownership and lifetime: who creates, owns and frees the data, and whether GPU and native resources follow the lifetime of the component or system that holds them.
- Scaling: how the cost grows with the number of objects and the size of the data.

Report a design problem once, at its main place or in the overview, and name the existing system or pattern the code should use.

When you see a better way to design the change, or several good ones, give them in the overview as ideas, not as requests. For each one, say in a sentence or two what it is, which existing system or pattern it builds on, and what it gains or costs compared with the current design. Keep them out of line comments, and don't make the merge wait on them unless the current design has a real problem from the list above.

## Does the change fit Stride?

Apart from the code, judge the change itself, with the rules of [CONTRIBUTING.md](.github/CONTRIBUTING.md) and the project direction below:

- Where does it belong: the core engine, a first-party plugin, a community plugin in the author's own repository, a sample, or the user's own code? Optional features go in plugins, and a new first-party plugin, or a large feature in one, needs an owner.
- Was the design agreed? A large change should link a design that the owner of the area agreed to. A large change with no agreed design, or with no owner, is a question to raise before merging, however good the code is. When you can't tell who owns the area, ask: don't conclude that it has no owner.
- Is the plugin unmaintained? A feature for an unmaintained plugin is a question too, unless the author offers to own the plugin.
- Is there a simpler or more standard way, or does the change duplicate something that exists? Could it be a small extension point in the core plus a community plugin?

Project direction:

- Windows is the tooling host: Game Studio, the asset compiler and the editor tools. Linux, macOS, Android and iOS are runtime targets. Runtime code that breaks a target is a problem; tooling that runs only on Windows is expected.
- Future-proof over legacy: don't bring back old behavior only for compatibility. Old code that no longer works gets a clear error that gives the fix.
- Game Studio is moving from WPF to Avalonia. Large work only for WPF is a question to raise.
- The boundary between the editor and the game should be shaped like one between processes: async messages and serialized data.
- A "drop the X dependency" or "separate X from Y" change is a layering cleanup: the feature stays. Don't read it as a removal.

## Upgraders

For a change to an upgrader (a `PackageUpgrader`, the steps of `StridePackageUpgrader`, the helpers of [CodeUpgrade](sources/engine/Stride.Assets/CodeUpgrade)), check:

- Each code rule works on the code as the user wrote it, and never relies on what an earlier rule changed: a rule that only matches code another rule wrote is a problem. The code binds against the Stride packages the project had, which can be as old as the `PackageMinimumVersion` of the upgrader.
- Each new rule has a test with the code before and after (`TestCodeUpgrade`). A rule that could change what the code does without a build error (an overload that still binds, a member of the same name) also checks what the upgraded code binds to.
- Each new step has a title that says what it changes, in one line. `Custom` rules and project steps need a title written by hand.
- Steps are only added at the end of their gate. A step is never removed or moved: it is replaced in place by `Retired(...)` (`registry.RetiredProject(...)` for a project step). [StridePackageUpgrader.Steps.md](sources/engine/Stride.Assets/StridePackageUpgrader.Steps.md) lists the IDs: in its diff, no ID moves to another step. Lines are only added at the end of a gate, and a retired step keeps its line, with `Retired: ` before its title.
- A project step changes files through `ProjectUpgradeContext` (`WriteFile`, `MoveFile`, `UnitFailed`), never directly, so that a step that throws changes nothing.
- Each new step has a section in the upgrade guide page of its release (stride-docs), whose heading starts with its ID: `### Stride-4.5.0-P2 Add the plugin packages whose assets the project has`. The build of stride-docs fails when a step of the list has none: without it, the link of the step opens the top of the page.

## CI failures

- A job that fails on the pull request and passes on the head of its base branch: this pull request most likely broke it. Find the cause in the diff and report it with the test name. When you can't find the cause, say which test fails and what its message says, as a question.
- A job that fails on the base branch too is not from this pull request: leave it out.
- Say nothing about CI that passed, is still running, or has not run.

## Re-reviews

- Comment only on what changed since the last review, and don't repeat earlier comments, open or resolved.
- An earlier comment that the author has not answered yet, by code or reply, needs nothing: don't ask again. When the author says the pull request is ready for another look, a short "this one still looks open" is fine.
- When a fix is partial, or the author's reply needs an answer, reply in the same thread instead of starting a new one.

## Writing the comments

- Every finding comes from code you read. Confirm the problem in the code before you write it. When you are not sure, or it depends on the author's intent, ask a real question ("Is this intended?", "Does this ever run?"). A short review with real problems is better than a long one with guesses.
- Stay high level: the problem and why it matters, in one to three sentences, plus the fix when it is short. Leave out long reasoning and step-by-step traces: the author will ask if they want details. Prefer a few comments on what matters to many small ones, and group related small points into one comment.
- Say what you confirmed plainly, without "I think", "probably" or "seems". Suggestions stay polite requests: "Could we...?", "Fine as a follow-up too".
- When you find a bug or a performance problem, suggest a concrete fix. For a small one, use a GitHub suggestion block.
- Comments on formatting, grammar or spelling are welcome. Start them, and other small points, with `Nit: `.
- Write like a maintainer talking to a colleague: friendly and to the point. Plain text, no headings or bold, no closing sentence that sums up the comment. Avoid words that sound generated ("It's worth noting", "Additionally", "This ensures", "robust", "comprehensive", "leverage", "seamless"), and don't restate the code back to the author.
- Many contributors are not native English speakers: short sentences, simple words, no idioms.

## XML documentation

- Check for missing, incomplete or wrong XML documentation comments on public types, methods, properties and fields.
- Give each XML comment suggestion as a separate item, so it can be accepted on its own.
- Don't rewrite existing XML comments unless they are wrong, unclear, or have grammar mistakes or typos.
- Suggested comments must describe what the code really does, match the style of the existing documentation, and use `<summary>`, `<param>`, `<returns>`, `<exception>` and `<remarks>` where they help.

The goal is few comments, each one useful and easy to act on.
