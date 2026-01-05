// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Stride.Core.Windows;
using Xunit;

namespace Stride.Core.Design.Tests.Windows;

public class TestAppHelper
{
    [Fact]
    public void GetCommandLineArgs_ReturnsArgsWithoutFirstElement()
    {
        // GetCommandLineArgs skips the first argument (executable path)
        var args = AppHelper.GetCommandLineArgs();

        // Should return array (empty or with args depending on test runner)
        Assert.NotNull(args);

        // All args should not be null
        Assert.All(args, arg => Assert.NotNull(arg));
    }

    [Fact]
    public void BuildErrorMessage_WithNullException_ThrowsNullReferenceException()
    {
        Assert.Throws<NullReferenceException>(() => AppHelper.BuildErrorMessage(null!));
    }

    [Fact]
    public void BuildErrorMessage_WithExceptionAndNoHeader_ContainsSystemInfo()
    {
        var exception = new InvalidOperationException("Test exception");
        var message = AppHelper.BuildErrorMessage(exception);

        Assert.Contains("Current Directory:", message);
        Assert.Contains("Command Line Args:", message);
        Assert.Contains("OS Version:", message);
        Assert.Contains("Processor Count:", message);
        Assert.Contains("Video configuration:", message);
        Assert.Contains("Exception:", message);
        Assert.Contains("Test exception", message);
    }

    [Fact]
    public void BuildErrorMessage_WithExceptionAndHeader_StartsWithHeader()
    {
        var exception = new InvalidOperationException("Test exception");
        var header = "Critical Error Occurred\n";
        var message = AppHelper.BuildErrorMessage(exception, header);

        Assert.StartsWith(header, message);
        Assert.Contains("Current Directory:", message);
        Assert.Contains("Test exception", message);
    }

    [Fact]
    public void BuildErrorMessage_WithNullHeader_DoesNotIncludeHeader()
    {
        var exception = new InvalidOperationException("Test exception");
        var message = AppHelper.BuildErrorMessage(exception, null);

        Assert.StartsWith("Current Directory:", message);
    }

    [Fact]
    public void BuildErrorMessage_IncludesProcessorCount()
    {
        var exception = new InvalidOperationException("Test");
        var message = AppHelper.BuildErrorMessage(exception);

        Assert.Contains($"Processor Count: {Environment.ProcessorCount}", message);
    }

    [Fact]
    public void BuildErrorMessage_IncludesOSArchitecture()
    {
        var exception = new InvalidOperationException("Test");
        var message = AppHelper.BuildErrorMessage(exception);

        var expectedArch = Environment.Is64BitOperatingSystem ? "x64" : "x86";
        Assert.Contains(expectedArch, message);
    }

    [Fact]
    public void BuildErrorMessage_WithInnerException_IncludesInnerExceptionDetails()
    {
        var innerException = new ArgumentException("Inner error");
        var exception = new InvalidOperationException("Outer error", innerException);
        var message = AppHelper.BuildErrorMessage(exception);

        Assert.Contains("Outer error", message);
        Assert.Contains("Inner error", message);
    }

    [Fact]
    public void WriteVideoConfig_WithValidStringBuilder_WritesGPUInfo()
    {
        var builder = new StringBuilder();

        AppHelper.WriteVideoConfig(builder);
        var result = builder.ToString();

        // Should either contain GPU info or error message
        Assert.True(
            result.Contains("GPU ") ||
            result.Contains("An error occurred while trying to retrieve video configuration."),
            "Expected GPU info or error message");
    }

    [Fact]
    public void WriteVideoConfig_WithNullStringBuilder_ThrowsNullReferenceException()
    {
        Assert.Throws<NullReferenceException>(() => AppHelper.WriteVideoConfig(null!));
    }

    [Fact]
    public void GetVideoConfig_ReturnsDictionary()
    {
        var config = AppHelper.GetVideoConfig();

        Assert.NotNull(config);
        // Dictionary might be empty if WMI query fails or no GPU info available
        // Just verify it returns a valid dictionary
    }

    [Fact]
    public void GetVideoConfig_WhenSuccessful_ContainsGPUKeys()
    {
        var config = AppHelper.GetVideoConfig();

        // On Windows systems, should typically have at least one GPU
        // Keys should start with "GPU0.", "GPU1.", etc.
        if (config.Count > 0)
        {
            Assert.Contains(config.Keys, key => key.StartsWith("GPU"));
        }
    }

    [Fact]
    public void GetVideoConfig_DoesNotIncludeNullValues()
    {
        var config = AppHelper.GetVideoConfig();

        // Verify no null values in dictionary
        Assert.All(config.Values, value => Assert.NotNull(value));
    }

    [Fact]
    public void GetVideoConfig_KeysFollowGPUNumberDotPropertyFormat()
    {
        var config = AppHelper.GetVideoConfig();

        if (config.Count > 0)
        {
            // All keys should match pattern: GPU<number>.<PropertyName>
            Assert.All(config.Keys, key =>
            {
                Assert.Matches(@"^GPU\d+\..+$", key);
            });
        }
    }

    [Fact]
    public void WriteVideoConfig_OutputContainsGPUNumber()
    {
        var builder = new StringBuilder();
        AppHelper.WriteVideoConfig(builder);
        var result = builder.ToString();

        // Should contain numbered GPU entries or error message
        if (!result.Contains("An error occurred"))
        {
            Assert.Matches(@"GPU \d+", result);
        }
    }

    [Fact]
    public void WriteVideoConfig_IndentsPropertyNames()
    {
        var builder = new StringBuilder();
        AppHelper.WriteVideoConfig(builder);
        var result = builder.ToString();

        // Properties should be indented (2 spaces) if GPU info is present
        if (result.Contains("GPU ") && !result.Contains("An error occurred"))
        {
            Assert.Contains("  ", result);
        }
    }
}
