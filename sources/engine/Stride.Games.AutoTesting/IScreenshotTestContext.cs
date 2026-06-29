// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace Stride.Games.AutoTesting;

/// <summary>
/// Driver surface passed to an <see cref="IScreenshotTest"/>. All async members yield to the
/// game loop so the script reads top-to-bottom while the game keeps ticking.
/// </summary>
public interface IScreenshotTestContext
{
    /// <summary>Game instance running the sample.</summary>
    Game Game { get; }

    /// <summary>Yield until <paramref name="frames"/> Update ticks have elapsed.</summary>
    Task WaitFrames(int frames);

    /// <summary>Yield until at least <paramref name="duration"/> of game time has elapsed.</summary>
    Task WaitTime(TimeSpan duration);

    /// <summary>
    /// Capture the back buffer and write it as <c>screenshots/&lt;name&gt;.png</c>. Awaits the actual
    /// capture. <paramref name="threshold"/> is the LPIPS distance above which the comparator
    /// flags this frame as a regression — bump it for screenshots that contain unavoidable
    /// nondeterminism (random particle emission, physics-driven trajectories, etc.). The default
    /// (0.05) corresponds to "perceptually indistinguishable" for content that runs deterministically
    /// under <c>Game.IsFixedTimeStep</c>.
    /// <para>
    /// <paramref name="claudeFallback"/> controls the Claude vision second-opinion that runs when
    /// LPIPS is over threshold. <c>true</c> (default) = use the generic same-scene prompt. A
    /// <c>string</c> = generic prompt + this extra guidance (e.g. "chick count must match").
    /// <c>false</c>/<c>null</c> = no fallback. The fallback only runs in the comparator and only
    /// when LPIPS already failed, so it costs nothing on passing frames.
    /// </para>
    /// </summary>
    Task Screenshot(string name, float threshold = 0.05f, object? claudeFallback = null);

    /// <summary>Press <paramref name="key"/> on the simulated keyboard. Stays down until <see cref="ReleaseKey"/>.</summary>
    void PressKey(Keys key);

    /// <summary>Release <paramref name="key"/> on the simulated keyboard.</summary>
    void ReleaseKey(Keys key);

    /// <summary>Press <paramref name="key"/>, hold for <paramref name="duration"/>, release.</summary>
    Task PressKey(Keys key, TimeSpan duration);

    /// <summary>Tap <paramref name="normalizedPosition"/> (0..1 in each axis) for <paramref name="duration"/>.</summary>
    Task Tap(Vector2 normalizedPosition, TimeSpan duration);

    /// <summary>Request the script ends successfully; the harness writes <c>done.json</c> with status="ok" and exits the game.</summary>
    void Exit(int exitCode = 0);
}
