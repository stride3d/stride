// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Graphics.Regression;
using Xunit;
using Xunit.Sdk;

namespace Stride.Graphics.Tests;

public class TestAllowGpuValidationError : GraphicTestGameBase
{
    private const string AllowedMessage = "Allowed validation error raised by the test";

    // Reported through the same logger as the validation layers, so it is caught on every API without the layers
    private static void ReportValidationError(string message) => GlobalLogger.GetLogger(GraphicsDevice.DebugLogModule).Error(message);

    /// <summary>
    /// [AllowGpuValidationError] on a test method is honored when the test runs through PerformTest.
    /// </summary>
    [Fact]
    [AllowGpuValidationError(GraphicsPlatform.Direct3D11, AllowedMessage)]
    [AllowGpuValidationError(GraphicsPlatform.Direct3D12, AllowedMessage)]
    [AllowGpuValidationError(GraphicsPlatform.Vulkan, AllowedMessage)]
    public void AllowedErrorIsIgnoredThroughPerformTest()
    {
        PerformTest(_ => ReportValidationError(AllowedMessage));
    }

    /// <summary>
    /// An error the test method does not allow still fails it.
    /// </summary>
    [Fact]
    [AllowGpuValidationError(GraphicsPlatform.Direct3D11, AllowedMessage)]
    [AllowGpuValidationError(GraphicsPlatform.Direct3D12, AllowedMessage)]
    [AllowGpuValidationError(GraphicsPlatform.Vulkan, AllowedMessage)]
    public void OtherErrorStillFailsThroughPerformTest()
    {
        Assert.ThrowsAny<XunitException>(() => PerformTest(_ => ReportValidationError("Unexpected validation error raised by the test")));
    }
}
