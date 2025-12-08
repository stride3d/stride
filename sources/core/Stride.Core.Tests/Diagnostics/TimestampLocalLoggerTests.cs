// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Diagnostics;

namespace Stride.Core.Tests.Diagnostics;

public class TimestampLocalLoggerTests
{
    [Fact]
    public void Constructor_CreatesLoggerWithStartTime()
    {
        var startTime = DateTime.Now;
        var logger = new TimestampLocalLogger(startTime);

        Assert.Null(logger.Module);
        Assert.Empty(logger.Messages);
        Assert.True(logger.Activated(LogMessageType.Verbose));
    }

    [Fact]
    public void Constructor_CreatesLoggerWithModuleName()
    {
        var startTime = DateTime.Now;
        var logger = new TimestampLocalLogger(startTime, "TestModule");

        Assert.Equal("TestModule", logger.Module);
    }

    [Fact]
    public void Info_LogsMessageWithTimestamp()
    {
        var startTime = DateTime.Now;
        var logger = new TimestampLocalLogger(startTime);

        logger.Info("Test message");

        Assert.Single(logger.Messages);
        Assert.True(logger.Messages[0].Timestamp >= 0);
        Assert.Equal("Test message", logger.Messages[0].LogMessage.Text);
        Assert.Equal(LogMessageType.Info, logger.Messages[0].LogMessage.Type);
    }

    [Fact]
    public void MultipleMessages_HaveIncreasingTimestamps()
    {
        var startTime = DateTime.Now;
        var logger = new TimestampLocalLogger(startTime);

        logger.Info("First");
        Thread.Sleep(10);
        logger.Info("Second");
        Thread.Sleep(10);
        logger.Info("Third");

        Assert.Equal(3, logger.Messages.Count);
        Assert.True(logger.Messages[0].Timestamp < logger.Messages[1].Timestamp);
        Assert.True(logger.Messages[1].Timestamp < logger.Messages[2].Timestamp);
    }

    [Fact]
    public void Messages_StoresAllLogMessageTypes()
    {
        var startTime = DateTime.Now;
        var logger = new TimestampLocalLogger(startTime);
        logger.ActivateLog(LogMessageType.Debug); // Enable Debug level too

        logger.Debug("Debug");
        logger.Verbose("Verbose");
        logger.Info("Info");
        logger.Warning("Warning");
        logger.Error("Error");
        logger.Fatal("Fatal");

        Assert.Equal(6, logger.Messages.Count);
        Assert.Equal(LogMessageType.Debug, logger.Messages[0].LogMessage.Type);
        Assert.Equal(LogMessageType.Verbose, logger.Messages[1].LogMessage.Type);
        Assert.Equal(LogMessageType.Info, logger.Messages[2].LogMessage.Type);
        Assert.Equal(LogMessageType.Warning, logger.Messages[3].LogMessage.Type);
        Assert.Equal(LogMessageType.Error, logger.Messages[4].LogMessage.Type);
        Assert.Equal(LogMessageType.Fatal, logger.Messages[5].LogMessage.Type);
    }

    [Fact]
    public void Timestamp_ReflectsTimeSinceStart()
    {
        var startTime = DateTime.Now;
        var logger = new TimestampLocalLogger(startTime);

        Thread.Sleep(50);
        logger.Info("Message");

        Assert.Single(logger.Messages);
        var elapsedTicks = logger.Messages[0].Timestamp;
        var elapsedMs = TimeSpan.FromTicks(elapsedTicks).TotalMilliseconds;

        Assert.True(elapsedMs >= 40); // Allow some margin
    }

    [Fact]
    public void Message_Struct_StoresTimestampAndLogMessage()
    {
        var logMessage = new LogMessage("Module", LogMessageType.Info, "Test");
        var message = new TimestampLocalLogger.Message(12345, logMessage);

        Assert.Equal(12345, message.Timestamp);
        Assert.Same(logMessage, message.LogMessage);
    }

    [Fact]
    public void Error_SetsHasErrors()
    {
        var startTime = DateTime.Now;
        var logger = new TimestampLocalLogger(startTime);

        logger.Error("Error");

        Assert.True(logger.HasErrors);
    }

    [Fact]
    public void ActivateLog_FiltersMessagesByType()
    {
        var startTime = DateTime.Now;
        var logger = new TimestampLocalLogger(startTime);

        logger.ActivateLog(LogMessageType.Info);
        logger.Verbose("Verbose");
        logger.Debug("Debug");
        logger.Info("Info");

        Assert.Single(logger.Messages);
        Assert.Equal("Info", logger.Messages[0].LogMessage.Text);
    }
}
