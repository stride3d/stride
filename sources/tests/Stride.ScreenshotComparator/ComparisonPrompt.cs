// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;

namespace Stride.Tests.ScreenshotComparator;

/// <summary>
/// Base for the Claude-vision comparison prompts. Subclasses (one per capture domain — gameplay,
/// editor UI) declare their own tolerance / regression-trigger bool properties and override
/// <see cref="Build"/> to compose the actual prompt string. The base supplies the shared
/// framing (BASELINE/CAPTURE intro, YES/NO format suffix, frame-level extra hint).
/// </summary>
public abstract record ComparisonPrompt
{
    /// <summary>Optional per-frame guidance appended to the prompt (e.g. "this frame includes a transient WorkProgress dialog").</summary>
    public string? ExtraHint { get; init; }

    /// <summary>
    /// Composes the full prompt sent to Claude vision. <paramref name="baselineCount"/> &gt; 1 reframes
    /// the comparison as "is the capture consistent with the variance shown across N baselines?".
    /// </summary>
    public abstract string Build(int baselineCount = 1);

    /// <summary>One-line opener describing the comparison and its tolerance bias.</summary>
    protected static string Intro(string domain, int baselineCount = 1) =>
        baselineCount <= 1
            ? $"Compare these two {domain} screenshots — BASELINE (expected) vs CAPTURE (this run). " +
              "Both came from the same code; visible differences are typically harness-timing nondeterminism, " +
              "NOT a regression. Be tolerant.\n\n"
            : $"Compare CAPTURE against {baselineCount} {domain} BASELINES that show the acceptable variance " +
              "for this frame. The capture is acceptable if its content falls within the range of variation " +
              "demonstrated by the baselines (it does not have to match any single baseline exactly). Flag a " +
              "regression only when the capture exhibits a quality / structural problem that NONE of the " +
              "baselines show.\n\n";

    /// <summary>YES/NO format directive plus optional per-frame hint.</summary>
    protected string OutroWithHint() =>
        "\nFormat: \"YES: <one-line reason>\" or \"NO: <one-line reason>\"."
        + (string.IsNullOrEmpty(ExtraHint) ? "" : " Frame context: " + ExtraHint);

    /// <summary>Appends "- {line}\n" to <paramref name="sb"/> when <paramref name="flag"/> is true.</summary>
    protected static void AppendIf(StringBuilder sb, bool flag, string line)
    {
        if (flag) sb.Append("- ").Append(line).Append('\n');
    }
}
