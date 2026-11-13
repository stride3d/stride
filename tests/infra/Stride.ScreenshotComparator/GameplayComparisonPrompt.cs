// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;

namespace Stride.Tests.ScreenshotComparator;

/// <summary>
/// Prompt tuned for in-game (rendered scene) captures. <see cref="Default"/> carries the curated
/// preset used by sample screenshot tests; bare <c>new()</c> is a blank slate. Tweak via
/// <c>GameplayComparisonPrompt.Default with { ... }</c>.
/// </summary>
public sealed record GameplayComparisonPrompt : ComparisonPrompt
{
    // Tolerances — sources of false-positive drift between runs.
    public bool TolerateHudValues { get; init; }
    public bool ToleratePoseAndCameraAngle { get; init; }
    public bool TolerateAnimationPhase { get; init; }
    public bool TolerateParticleAndPostFxNoise { get; init; }

    // Regression triggers — visible differences that ARE a real regression.
    public bool RegressionOnColorShift { get; init; }
    public bool RegressionOnMissingGeometry { get; init; }
    public bool RegressionOnMissingTextures { get; init; }
    public bool RegressionOnMissingPostProcess { get; init; }
    public bool RegressionOnUiPageChange { get; init; }
    public bool RegressionOnWrongScene { get; init; }

    /// <summary>Curated preset for sample screenshot tests.</summary>
    public static readonly GameplayComparisonPrompt Default = new()
    {
        TolerateHudValues = true,
        ToleratePoseAndCameraAngle = true,
        TolerateAnimationPhase = true,
        TolerateParticleAndPostFxNoise = true,
        RegressionOnColorShift = true,
        RegressionOnMissingGeometry = true,
        RegressionOnMissingTextures = true,
        RegressionOnMissingPostProcess = true,
        RegressionOnUiPageChange = true,
        RegressionOnWrongScene = true,
    };

    public override string Build(int baselineCount = 1)
    {
        var sb = new StringBuilder(Intro("Stride engine", baselineCount));
        sb.Append("YES (not a regression):\n");
        AppendIf(sb, TolerateHudValues, "HUD numeric values differ (ammo, score, timer, FPS, health). Gameplay state.");
        AppendIf(sb, ToleratePoseAndCameraAngle, "Character / weapon / camera pose, aim angle, hand-bob phase differs. Animation phase.");
        AppendIf(sb, TolerateAnimationPhase, "Animation phase / IK / skinning state differs.");
        AppendIf(sb, TolerateParticleAndPostFxNoise, "Particle / smoke / fire / cloth / water / lighting / post-process noise differs.");
        sb.Append("\nNO (real regression):\n");
        AppendIf(sb, RegressionOnColorShift, "Whole-frame color / gamma / brightness shift (capture noticeably darker, washed-out, wrong sRGB encoding).");
        AppendIf(sb, RegressionOnMissingGeometry, "Missing or corrupt geometry (broken meshes, distorted models, holes).");
        AppendIf(sb, RegressionOnMissingTextures, "Missing or wrong textures (pink/purple checkerboard, all-black surfaces, wrong materials).");
        AppendIf(sb, RegressionOnMissingPostProcess, "Missing post-process pass (no bloom / shadow / SSAO / tonemapping where baseline has them).");
        AppendIf(sb, RegressionOnUiPageChange, "Different UI page, missing UI elements, wrong UI text labels (label text, not numeric values).");
        AppendIf(sb, RegressionOnWrongScene, "Wrong scene entirely (different level, camera angle differs by 90°+, missing major objects).");
        sb.Append(OutroWithHint());
        return sb.ToString();
    }
}
