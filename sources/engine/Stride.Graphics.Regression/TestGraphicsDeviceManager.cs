// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;

using Xunit;

using Stride.Games;

namespace Stride.Graphics.Regression;

/// <summary>
///   A specialized <see cref="GraphicsDeviceManager"/> for testing purposes.
/// </summary>
/// <remarks>
///   By default, this Graphics Device Manager asserts that at least one of the requested
///   Graphics Profiles is available. If none are available, the test is skipped.
///   To override this behavior, derive from this class and override the
///   <see cref="IsPreferredProfileAvailable(GraphicsProfile[], out GraphicsProfile)"/>
///   method.
/// </remarks>
public class TestGraphicsDeviceManager : GraphicsDeviceManager
{
    /// <summary>
    ///   Initializes a new instance of the <see cref="TestGraphicsDeviceManager"/> class.
    /// </summary>
    /// <param name="game"></param>
    public TestGraphicsDeviceManager(GameBase game) : base(game) { }


    /// <inheritdoc/>
    /// <remarks>
    ///   The test will be skipped if none of the preferred Graphics Profiles are available.
    /// </remarks>
    protected override bool IsPreferredProfileAvailable(GraphicsProfile[] preferredProfiles, out GraphicsProfile availableProfile)
    {
        Skip.IfNot(base.IsPreferredProfileAvailable(preferredProfiles, out availableProfile),
                   $"This test requires the '{preferredProfiles.Min()}' Graphics Profile. It has been skipped.");

        return true;
    }
}
