// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Packages;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// Tests for the <see cref="NullPackagesLogger"/> class.
/// </summary>
public class TestNullPackagesLogger
{
    [Fact]
    public void TestInstanceIsNotNull()
    {
        // Act
        var instance = NullPackagesLogger.Instance;

        // Assert
        Assert.NotNull(instance);
    }

    [Fact]
    public void TestInstanceIsSingleton()
    {
        // Act
        var instance1 = NullPackagesLogger.Instance;
        var instance2 = NullPackagesLogger.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void TestLogDoesNotThrow()
    {
        // Arrange
        var logger = NullPackagesLogger.Instance;

        // Act & Assert - Should not throw
        logger.Log(MessageLevel.Debug, "Debug message");
        logger.Log(MessageLevel.Info, "Info message");
        logger.Log(MessageLevel.Warning, "Warning message");
        logger.Log(MessageLevel.Error, "Error message");
    }

    [Fact]
    public async Task TestLogAsyncDoesNotThrow()
    {
        // Arrange
        var logger = NullPackagesLogger.Instance;

        // Act & Assert - Should not throw
        await logger.LogAsync(MessageLevel.Debug, "Debug message");
        await logger.LogAsync(MessageLevel.Info, "Info message");
        await logger.LogAsync(MessageLevel.Warning, "Warning message");
        await logger.LogAsync(MessageLevel.Error, "Error message");
    }

    [Fact]
    public async Task TestLogAsyncCompletesImmediately()
    {
        // Arrange
        var logger = NullPackagesLogger.Instance;

        // Act
        var task = logger.LogAsync(MessageLevel.Info, "Test message");

        // Assert
        Assert.True(task.IsCompleted);
        await task; // Should complete immediately
    }

    [Fact]
    public void TestLogWithAllMessageLevels()
    {
        // Arrange
        var logger = NullPackagesLogger.Instance;

        // Act & Assert - All levels should work without throwing
        logger.Log(MessageLevel.Debug, "Debug");
        logger.Log(MessageLevel.Verbose, "Verbose");
        logger.Log(MessageLevel.Info, "Info");
        logger.Log(MessageLevel.Minimal, "Minimal");
        logger.Log(MessageLevel.Warning, "Warning");
        logger.Log(MessageLevel.Error, "Error");
        logger.Log(MessageLevel.InfoSummary, "InfoSummary");
        logger.Log(MessageLevel.ErrorSummary, "ErrorSummary");
    }

    [Fact]
    public async Task TestLogAsyncWithAllMessageLevels()
    {
        // Arrange
        var logger = NullPackagesLogger.Instance;

        // Act & Assert - All levels should work without throwing
        await logger.LogAsync(MessageLevel.Debug, "Debug");
        await logger.LogAsync(MessageLevel.Verbose, "Verbose");
        await logger.LogAsync(MessageLevel.Info, "Info");
        await logger.LogAsync(MessageLevel.Minimal, "Minimal");
        await logger.LogAsync(MessageLevel.Warning, "Warning");
        await logger.LogAsync(MessageLevel.Error, "Error");
        await logger.LogAsync(MessageLevel.InfoSummary, "InfoSummary");
        await logger.LogAsync(MessageLevel.ErrorSummary, "ErrorSummary");
    }

    [Fact]
    public void TestLogWithNullMessage()
    {
        // Arrange
        var logger = NullPackagesLogger.Instance;

        // Act & Assert - Should not throw with null message
        logger.Log(MessageLevel.Info, null!);
    }

    [Fact]
    public async Task TestLogAsyncWithNullMessage()
    {
        // Arrange
        var logger = NullPackagesLogger.Instance;

        // Act & Assert - Should not throw with null message
        await logger.LogAsync(MessageLevel.Info, null!);
    }
}
