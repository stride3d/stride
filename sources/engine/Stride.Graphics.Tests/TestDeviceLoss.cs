// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Graphics.Tests;

/// <summary>
/// A lost device is not recovered: the loss surfaces as a <see cref="GraphicsDeviceException"/> carrying the device
/// status, for the host to handle (Game Studio restarts its games).
/// </summary>
public class TestDeviceLoss : GraphicTestGameBase
{
    private const int LossFrame = 3;

    protected override void RegisterTests()
    {
        base.RegisterTests();

        FrameGameSystem.Update(LossFrame, GraphicsDevice.SimulateReset);
    }

    [SkippableFact]
    public void LossSurfacesAsException()
    {
        var exception = Assert.Throws<GraphicsDeviceException>(() => RunGameTest(new TestDeviceLoss()));
        Assert.Equal(GraphicsDeviceStatus.Reset, exception.Status);
    }
}
