# Stride Game Engine Copilot Instructions

This document provides guidelines and best practices for using GitHub Copilot and Copilot Chat within the Stride Game Engine repository. The goal is to ensure Copilot is used effectively and consistently to maintain code quality, project standards, and team productivity.

## Coding & Contribution Guidelines

- Prefer concise, well-documented, and idiomatic C# code.
- Do not use `#region` directives; prefer clear, self-documenting code.

## Copilot Pull Request Code Review Instructions

Stride is a game engine project that requires careful code reviews to maintain quality and performance. Please follow these guidelines when reviewing pull requests (PRs):

- Generate a neat and concise Pull Request Overview, highlighting the most important changes.
- Focus reviews on logic, safety, performance, and code consistency with the existing codebase.
- Avoid suggesting large architectural changes in PR reviews.
- Comments on formatting, grammar, or spelling are welcome.
- Minor style or nit-pick comments are acceptable to maintain consistency.
- Do not review auto-generated, third-party code, binary files, or assets.
- If you find a bug or performance issue, suggest a concrete fix in the PR.
- For large PRs (20+ C#/*.cs files), do not attempt a full review, only highlight critical or blocking issues.
- Always consider the context and established patterns in the Stride codebase before making suggestions.
- **Always check for missing, incomplete, or incorrect XML documentation comments** on public types, methods, properties, and fields.
- **Provide XML comment suggestions as individual, separate items** so they can be reviewed and approved independently.
- **Do not rewrite existing XML comments unless they contain errors** such as incorrect information, poor grammar, typos, or lack clarity. Preserve well-written documentation.
- When suggesting XML comments, ensure they are:
  - Accurate and describe the actual functionality
  - Consistent with existing documentation style in the codebase
  - Include `<summary>`, `<param>`, `<returns>`, `<exception>` and `<remarks>` tags where appropriate
  - Clear and helpful for API consumers 

The goal is to minimize noise and maximize helpful, actionable feedback.
