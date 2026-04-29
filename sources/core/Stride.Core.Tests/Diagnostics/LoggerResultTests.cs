// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Diagnostics;

namespace Stride.Core.Tests.Diagnostics;

public class LoggerResultTests
{
    [Fact]
    public void Constructor_CreatesLoggerWithNullModule()
    {
        var logger = new LoggerResult();

        Assert.Null(logger.Module);
        Assert.Empty(logger.Messages);
        Assert.False(logger.HasErrors);
        Assert.False(logger.IsLoggingProgressAsInfo);
        Assert.True(logger.Activated(LogMessageType.Debug));
    }

    [Fact]
    public void Constructor_CreatesLoggerWithSpecifiedModule()
    {
        var logger = new LoggerResult("TestModule");

        Assert.Equal("TestModule", logger.Module);
        Assert.Empty(logger.Messages);
    }

    [Fact]
    public void Module_CanBeSetAfterConstruction()
    {
        var logger = new LoggerResult("Initial");

        logger.Module = "Updated";

        Assert.Equal("Updated", logger.Module);
    }

    [Fact]
    public void Info_LogsMessageWithCorrectType()
    {
        var logger = new LoggerResult();

        logger.Info("Info message");

        Assert.Single(logger.Messages);
        Assert.Equal(LogMessageType.Info, logger.Messages[0].Type);
        Assert.Equal("Info message", logger.Messages[0].Text);
    }

    [Fact]
    public void Debug_LogsMessageWithCorrectType()
    {
        var logger = new LoggerResult();

        logger.Debug("Debug message");

        Assert.Single(logger.Messages);
        Assert.Equal(LogMessageType.Debug, logger.Messages[0].Type);
        Assert.Equal("Debug message", logger.Messages[0].Text);
    }

    [Fact]
    public void Verbose_LogsMessageWithCorrectType()
    {
        var logger = new LoggerResult();

        logger.Verbose("Verbose message");

        Assert.Single(logger.Messages);
        Assert.Equal(LogMessageType.Verbose, logger.Messages[0].Type);
        Assert.Equal("Verbose message", logger.Messages[0].Text);
    }

    [Fact]
    public void Warning_LogsMessageWithCorrectType()
    {
        var logger = new LoggerResult();

        logger.Warning("Warning message");

        Assert.Single(logger.Messages);
        Assert.Equal(LogMessageType.Warning, logger.Messages[0].Type);
        Assert.Equal("Warning message", logger.Messages[0].Text);
    }

    [Fact]
    public void Error_LogsMessageWithCorrectTypeAndSetsHasErrors()
    {
        var logger = new LoggerResult();

        logger.Error("Error message");

        Assert.Single(logger.Messages);
        Assert.Equal(LogMessageType.Error, logger.Messages[0].Type);
        Assert.Equal("Error message", logger.Messages[0].Text);
        Assert.True(logger.HasErrors);
    }

    [Fact]
    public void Fatal_LogsMessageWithCorrectTypeAndSetsHasErrors()
    {
        var logger = new LoggerResult();

        logger.Fatal("Fatal message");

        Assert.Single(logger.Messages);
        Assert.Equal(LogMessageType.Fatal, logger.Messages[0].Type);
        Assert.Equal("Fatal message", logger.Messages[0].Text);
        Assert.True(logger.HasErrors);
    }

    [Fact]
    public void Error_WithException_LogsExceptionInformation()
    {
        var logger = new LoggerResult();
        var exception = new InvalidOperationException("Test exception");

        logger.Error("Error with exception", exception);

        Assert.Single(logger.Messages);
        var logMessage = (LogMessage)logger.Messages[0];
        Assert.Equal("Error with exception", logMessage.Text);
        Assert.Same(exception, logMessage.Exception);
    }

    [Fact]
    public void Clear_RemovesAllMessages()
    {
        var logger = new LoggerResult();
        logger.Info("Message 1");
        logger.Info("Message 2");
        logger.Info("Message 3");

        logger.Clear();

        Assert.Empty(logger.Messages);
    }

    [Fact]
    public void MultipleMessages_AreStoredInOrder()
    {
        var logger = new LoggerResult();

        logger.Debug("First");
        logger.Info("Second");
        logger.Warning("Third");

        Assert.Equal(3, logger.Messages.Count);
        Assert.Equal("First", logger.Messages[0].Text);
        Assert.Equal("Second", logger.Messages[1].Text);
        Assert.Equal("Third", logger.Messages[2].Text);
    }

    [Fact]
    public void Progress_RaisesProgressChangedEvent()
    {
        var logger = new LoggerResult();
        string? progressMessage = null;
        logger.ProgressChanged += (sender, args) =>
        {
            progressMessage = args.Message;
        };

        logger.Progress("Progress message");

        Assert.Equal("Progress message", progressMessage);
    }

    [Fact]
    public void Progress_WithSteps_RaisesProgressChangedEventWithStepInfo()
    {
        var logger = new LoggerResult();
        int? currentStep = null;
        int? stepCount = null;
        logger.ProgressChanged += (sender, args) =>
        {
            currentStep = args.CurrentStep;
            stepCount = args.StepCount;
        };

        logger.Progress("Processing", 3, 10);

        Assert.Equal(3, currentStep);
        Assert.Equal(10, stepCount);
    }

    [Fact]
    public void IsLoggingProgressAsInfo_DefaultsToFalse()
    {
        var logger = new LoggerResult();

        Assert.False(logger.IsLoggingProgressAsInfo);
    }

    [Fact]
    public void IsLoggingProgressAsInfo_CanBeSet()
    {
        var logger = new LoggerResult();

        logger.IsLoggingProgressAsInfo = true;

        Assert.True(logger.IsLoggingProgressAsInfo);
    }

    [Fact]
    public void ActivateLog_DisablesVerboseByDefault()
    {
        var logger = new LoggerResult();

        logger.ActivateLog(LogMessageType.Info);
        logger.Verbose("Verbose");
        logger.Info("Info");

        Assert.Single(logger.Messages);
        Assert.Equal("Info", logger.Messages[0].Text);
    }

    [Fact]
    public void ActivateLog_CanEnableVerbose()
    {
        var logger = new LoggerResult();
        logger.ActivateLog(LogMessageType.Info);

        logger.ActivateLog(LogMessageType.Verbose);
        logger.Verbose("Verbose");

        Assert.Single(logger.Messages);
        Assert.Equal("Verbose", logger.Messages[0].Text);
    }

    [Fact]
    public void ActivateLog_CanEnableSpecificTypeSelectively()
    {
        var logger = new LoggerResult();
        logger.ActivateLog(LogMessageType.Info);

        logger.ActivateLog(LogMessageType.Debug, true);
        logger.Verbose("Verbose");
        logger.Debug("Debug");
        logger.Info("Info");

        Assert.Equal(2, logger.Messages.Count);
        Assert.Equal("Debug", logger.Messages[0].Text);
        Assert.Equal("Info", logger.Messages[1].Text);
    }

    [Fact]
    public async Task Messages_IsThreadSafe()
    {
        var logger = new LoggerResult();
        var tasks = new List<Task>();

        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() => logger.Info($"Message {index}")));
        }

        await Task.WhenAll(tasks);

        Assert.Equal(10, logger.Messages.Count);
    }
}
