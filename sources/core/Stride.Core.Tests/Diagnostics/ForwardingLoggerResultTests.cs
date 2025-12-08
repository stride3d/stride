// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Diagnostics;

namespace Stride.Core.Tests.Diagnostics;

public class ForwardingLoggerResultTests
{
    [Fact]
    public void Constructor_RequiresTargetLogger()
    {
        var targetLogger = new LoggerResult("Target");

        var forwardingLogger = new ForwardingLoggerResult(targetLogger);

        Assert.NotNull(forwardingLogger);
    }

    [Fact]
    public void Log_ForwardsToTargetLogger()
    {
        var targetLogger = new LoggerResult("Target");
        var forwardingLogger = new ForwardingLoggerResult(targetLogger);

        forwardingLogger.Info("Test message");

        Assert.Single(targetLogger.Messages);
        Assert.Equal("Test message", targetLogger.Messages[0].Text);
    }

    [Fact]
    public void Log_StoresMessageLocally()
    {
        var targetLogger = new LoggerResult("Target");
        var forwardingLogger = new ForwardingLoggerResult(targetLogger);

        forwardingLogger.Info("Test message");

        Assert.Single(forwardingLogger.Messages);
        Assert.Equal("Test message", forwardingLogger.Messages[0].Text);
    }

    [Fact]
    public void Log_ForwardsBothLocallyAndToTarget()
    {
        var targetLogger = new LoggerResult("Target");
        var forwardingLogger = new ForwardingLoggerResult(targetLogger);

        forwardingLogger.Info("Message 1");
        forwardingLogger.Warning("Message 2");
        forwardingLogger.Error("Message 3");

        Assert.Equal(3, forwardingLogger.Messages.Count);
        Assert.Equal(3, targetLogger.Messages.Count);
        Assert.Equal("Message 1", forwardingLogger.Messages[0].Text);
        Assert.Equal("Message 1", targetLogger.Messages[0].Text);
    }

    [Fact]
    public void Log_ForwardsAllMessageTypes()
    {
        var targetLogger = new LoggerResult("Target");
        var forwardingLogger = new ForwardingLoggerResult(targetLogger);

        forwardingLogger.Debug("Debug");
        forwardingLogger.Verbose("Verbose");
        forwardingLogger.Info("Info");
        forwardingLogger.Warning("Warning");
        forwardingLogger.Error("Error");
        forwardingLogger.Fatal("Fatal");

        Assert.Equal(6, targetLogger.Messages.Count);
        Assert.Equal(LogMessageType.Debug, targetLogger.Messages[0].Type);
        Assert.Equal(LogMessageType.Fatal, targetLogger.Messages[5].Type);
    }

    [Fact]
    public void Clear_OnlyAffectsLocalMessages()
    {
        var targetLogger = new LoggerResult("Target");
        var forwardingLogger = new ForwardingLoggerResult(targetLogger);

        forwardingLogger.Info("Message");
        forwardingLogger.Clear();

        Assert.Empty(forwardingLogger.Messages);
        Assert.Single(targetLogger.Messages);
    }

    [Fact]
    public void HasErrors_IsSetByErrorMessage()
    {
        var targetLogger = new LoggerResult("Target");
        var forwardingLogger = new ForwardingLoggerResult(targetLogger);

        forwardingLogger.Error("Error");

        Assert.True(forwardingLogger.HasErrors);
        Assert.True(targetLogger.HasErrors);
    }

    [Fact]
    public void ActivateLog_AffectsForwarding()
    {
        var targetLogger = new LoggerResult("Target");
        var forwardingLogger = new ForwardingLoggerResult(targetLogger);

        forwardingLogger.ActivateLog(LogMessageType.Info);
        forwardingLogger.Verbose("Verbose");
        forwardingLogger.Info("Info");

        Assert.Single(forwardingLogger.Messages);
        Assert.Single(targetLogger.Messages);
        Assert.Equal("Info", targetLogger.Messages[0].Text);
    }

    [Fact]
    public void Log_WithException_ForwardsExceptionToTarget()
    {
        var targetLogger = new LoggerResult("Target");
        var forwardingLogger = new ForwardingLoggerResult(targetLogger);
        var exception = new InvalidOperationException("Test");

        forwardingLogger.Error("Error with exception", exception);

        var targetMessage = (LogMessage)targetLogger.Messages[0];
        Assert.Same(exception, targetMessage.Exception);
    }
}
