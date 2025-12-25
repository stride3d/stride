// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Diagnostics;

namespace Stride.Core.Tests.Diagnostics;

public class LoggerActivationTests
{
    [Fact]
    public void ActivateLog_WithSingleType_EnablesOnlyThatType()
    {
        var logger = new LoggerResult();
        // Disable all first, then enable only Warning
        logger.ActivateLog(LogMessageType.Debug, LogMessageType.Fatal, false);
        logger.ActivateLog(LogMessageType.Warning, true);

        Assert.False(logger.Activated(LogMessageType.Debug));
        Assert.False(logger.Activated(LogMessageType.Info));
        Assert.True(logger.Activated(LogMessageType.Warning));
        Assert.False(logger.Activated(LogMessageType.Error));
    }

    [Fact]
    public void ActivateLog_WithRange_EnablesAllTypesInRange()
    {
        var logger = new LoggerResult();
        logger.ActivateLog(LogMessageType.Debug, LogMessageType.Fatal, false);

        logger.ActivateLog(LogMessageType.Info, LogMessageType.Error);

        Assert.False(logger.Activated(LogMessageType.Debug));
        Assert.False(logger.Activated(LogMessageType.Verbose));
        Assert.True(logger.Activated(LogMessageType.Info));
        Assert.True(logger.Activated(LogMessageType.Warning));
        Assert.True(logger.Activated(LogMessageType.Error));
        Assert.False(logger.Activated(LogMessageType.Fatal));
    }

    [Fact]
    public void ActivateLog_WithReversedRange_SwapsToCorrectOrder()
    {
        var logger = new LoggerResult();
        logger.ActivateLog(LogMessageType.Debug, LogMessageType.Fatal, false);

        logger.ActivateLog(LogMessageType.Error, LogMessageType.Warning);

        Assert.True(logger.Activated(LogMessageType.Warning));
        Assert.True(logger.Activated(LogMessageType.Error));
    }

    [Fact]
    public void ActivateLog_WithDisableFlag_DisablesRange()
    {
        var logger = new LoggerResult();

        logger.ActivateLog(LogMessageType.Info, LogMessageType.Fatal, false);

        Assert.False(logger.Activated(LogMessageType.Info));
        Assert.False(logger.Activated(LogMessageType.Warning));
        Assert.False(logger.Activated(LogMessageType.Error));
    }

    [Fact]
    public void ActivateLog_DefaultToLevel_SetsToFatal()
    {
        var logger = new LoggerResult();
        logger.ActivateLog(LogMessageType.Debug, LogMessageType.Fatal, false);

        logger.ActivateLog(LogMessageType.Warning);

        Assert.True(logger.Activated(LogMessageType.Warning));
        Assert.True(logger.Activated(LogMessageType.Error));
        Assert.True(logger.Activated(LogMessageType.Fatal));
    }

    [Fact]
    public void Activated_ReturnsFalseForDisabledType()
    {
        var logger = new LoggerResult();
        logger.ActivateLog(LogMessageType.Info);

        Assert.False(logger.Activated(LogMessageType.Debug));
        Assert.False(logger.Activated(LogMessageType.Verbose));
    }

    [Fact]
    public void Activated_ReturnsTrueForEnabledType()
    {
        var logger = new LoggerResult();

        Assert.True(logger.Activated(LogMessageType.Info));
        Assert.True(logger.Activated(LogMessageType.Warning));
        Assert.True(logger.Activated(LogMessageType.Error));
    }

    [Fact]
    public void Log_SkipsDisabledMessageTypes()
    {
        var logger = new LoggerResult();
        logger.ActivateLog(LogMessageType.Warning);

        logger.Debug("Debug");
        logger.Verbose("Verbose");
        logger.Info("Info");
        logger.Warning("Warning");
        logger.Error("Error");

        Assert.Equal(2, logger.Messages.Count);
        Assert.Equal("Warning", logger.Messages[0].Text);
        Assert.Equal("Error", logger.Messages[1].Text);
    }

    [Fact]
    public void HasErrors_IsSetEvenWhenErrorLoggingDisabled()
    {
        var logger = new LoggerResult();
        logger.ActivateLog(LogMessageType.Info, LogMessageType.Warning);

        logger.Error("Error");

        Assert.True(logger.HasErrors);
        Assert.Empty(logger.Messages);
    }

    [Fact]
    public void MessageLogged_EventFiredOnlyForEnabledTypes()
    {
        var logger = new LoggerResult();
        logger.ActivateLog(LogMessageType.Warning);
        int eventCount = 0;
        logger.MessageLogged += (sender, args) => eventCount++;

        logger.Info("Info");
        logger.Warning("Warning");
        logger.Error("Error");

        Assert.Equal(2, eventCount);
    }
}
