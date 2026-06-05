// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;

namespace Stride.Tests.ScreenshotComparator;

/// <summary>
/// Prompt tuned for GameStudio editor UI captures (docked panels, dialogs, scene-document floats).
/// <see cref="Default"/> carries the curated preset used by editor screenshot tests; bare
/// <c>new()</c> is a blank slate. Tweak via <c>EditorComparisonPrompt.Default with { ... }</c>.
/// </summary>
public sealed record EditorComparisonPrompt : ComparisonPrompt
{
    // Tolerances — sources of false-positive drift in editor captures.
    public bool TolerateBuildLogTimestamps { get; init; }
    public bool TolerateAssetCounts { get; init; }
    public bool TolerateScenePreviewDrift { get; init; }
    public bool TolerateFontRasterization { get; init; }
    public bool TolerateThumbnailRenderTiming { get; init; }
    public bool TolerateScrollPosition { get; init; }
    public bool TolerateSelectionHighlight { get; init; }

    // Regression triggers — visible differences that ARE a real regression.
    public bool RegressionOnBlankPanel { get; init; }
    public bool RegressionOnExceptionDialog { get; init; }
    public bool RegressionOnBrokenLayout { get; init; }
    public bool RegressionOnMissingControls { get; init; }
    public bool RegressionOnWrongLabels { get; init; }
    public bool RegressionOnBrokenScenePreview { get; init; }
    public bool RegressionOnThemeColorShift { get; init; }
    public bool RegressionOnBuildLogErrors { get; init; }

    /// <summary>Curated preset for editor screenshot tests.</summary>
    public static readonly EditorComparisonPrompt Default = new()
    {
        TolerateBuildLogTimestamps = true,
        TolerateAssetCounts = true,
        TolerateScenePreviewDrift = true,
        TolerateFontRasterization = true,
        TolerateThumbnailRenderTiming = true,
        TolerateScrollPosition = true,
        TolerateSelectionHighlight = true,
        RegressionOnBlankPanel = true,
        RegressionOnExceptionDialog = true,
        RegressionOnBrokenLayout = true,
        RegressionOnMissingControls = true,
        RegressionOnWrongLabels = true,
        RegressionOnBrokenScenePreview = true,
        RegressionOnThemeColorShift = true,
        RegressionOnBuildLogErrors = true,
    };

    public override string Build(int baselineCount = 1)
    {
        var sb = new StringBuilder(Intro("Stride GameStudio editor UI", baselineCount));
        sb.Append("YES (not a regression):\n");
        AppendIf(sb, TolerateBuildLogTimestamps, "Timestamps / elapsed-time strings / dates in build or output logs.");
        AppendIf(sb, TolerateAssetCounts, "Asset, file, or item counts differ slightly (template content drift between runs).");
        AppendIf(sb, TolerateScenePreviewDrift, "Embedded 3D scene viewport content differs (camera angle, lighting, frame timing — nondeterministic).");
        AppendIf(sb, TolerateFontRasterization, "Sub-pixel font rasterization differences (ClearType, theme variations).");
        AppendIf(sb, TolerateThumbnailRenderTiming, "Asset-thumbnail loading state (rendered icon vs placeholder vs spinner).");
        AppendIf(sb, TolerateScrollPosition, "Scroll position / first-visible-item differs in lists or trees.");
        AppendIf(sb, TolerateSelectionHighlight, "Selection / hover / focus highlight on a different item.");
        sb.Append("\nNO (real regression):\n");
        AppendIf(sb, RegressionOnBlankPanel, "Panel is blank / black / shows only chrome with no content.");
        AppendIf(sb, RegressionOnExceptionDialog, "An error / exception / crash dialog is visible.");
        AppendIf(sb, RegressionOnBrokenLayout, "Broken layout (panels overlapping, controls clipped, docking glitches, content overflowing chrome).");
        AppendIf(sb, RegressionOnMissingControls, "Missing UI controls (toolbar buttons, menu items, tab headers, treeview nodes).");
        AppendIf(sb, RegressionOnWrongLabels, "Wrong UI text labels (button captions, panel titles, menu item names — not numeric values).");
        AppendIf(sb, RegressionOnBrokenScenePreview, "Embedded scene preview is BROKEN (pink-checker, all-black, distorted, debug-error overlay) — distinct from normal viewport drift.");
        AppendIf(sb, RegressionOnThemeColorShift, "Whole-window color / theme shift (light vs dark theme, wrong accent color throughout).");
        AppendIf(sb, RegressionOnBuildLogErrors, "Build/output log shows error or warning lines (color-coded red/yellow vs the normal info-level color) — but timestamps and elapsed-time numbers in those lines are still tolerated.");
        sb.Append(OutroWithHint());
        return sb.ToString();
    }
}
