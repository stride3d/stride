// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Diagnostics;

namespace Stride.Core.Tests.Diagnostics;

public class LogMessageExtensionsTests
{
    [Fact]
    public void IsAtLeast_ReturnsTrueForDebugAndHigher()
    {
        Assert.True(CreateMessage(LogMessageType.Debug).IsAtLeast(LogMessageType.Debug));
        Assert.True(CreateMessage(LogMessageType.Verbose).IsAtLeast(LogMessageType.Debug));
        Assert.True(CreateMessage(LogMessageType.Info).IsAtLeast(LogMessageType.Debug));
        Assert.True(CreateMessage(LogMessageType.Warning).IsAtLeast(LogMessageType.Debug));
        Assert.True(CreateMessage(LogMessageType.Error).IsAtLeast(LogMessageType.Debug));
        Assert.True(CreateMessage(LogMessageType.Fatal).IsAtLeast(LogMessageType.Debug));
    }

    [Fact]
    public void IsAtLeast_ReturnsTrueForVerboseAndHigher()
    {
        Assert.False(CreateMessage(LogMessageType.Debug).IsAtLeast(LogMessageType.Verbose));
        Assert.True(CreateMessage(LogMessageType.Verbose).IsAtLeast(LogMessageType.Verbose));
        Assert.True(CreateMessage(LogMessageType.Info).IsAtLeast(LogMessageType.Verbose));
        Assert.True(CreateMessage(LogMessageType.Warning).IsAtLeast(LogMessageType.Verbose));
        Assert.True(CreateMessage(LogMessageType.Error).IsAtLeast(LogMessageType.Verbose));
        Assert.True(CreateMessage(LogMessageType.Fatal).IsAtLeast(LogMessageType.Verbose));
    }

    [Fact]
    public void IsAtLeast_ReturnsTrueForInfoAndHigher()
    {
        Assert.False(CreateMessage(LogMessageType.Debug).IsAtLeast(LogMessageType.Info));
        Assert.False(CreateMessage(LogMessageType.Verbose).IsAtLeast(LogMessageType.Info));
        Assert.True(CreateMessage(LogMessageType.Info).IsAtLeast(LogMessageType.Info));
        Assert.True(CreateMessage(LogMessageType.Warning).IsAtLeast(LogMessageType.Info));
        Assert.True(CreateMessage(LogMessageType.Error).IsAtLeast(LogMessageType.Info));
        Assert.True(CreateMessage(LogMessageType.Fatal).IsAtLeast(LogMessageType.Info));
    }

    [Fact]
    public void IsAtLeast_ReturnsTrueForWarningAndHigher()
    {
        Assert.False(CreateMessage(LogMessageType.Debug).IsAtLeast(LogMessageType.Warning));
        Assert.False(CreateMessage(LogMessageType.Verbose).IsAtLeast(LogMessageType.Warning));
        Assert.False(CreateMessage(LogMessageType.Info).IsAtLeast(LogMessageType.Warning));
        Assert.True(CreateMessage(LogMessageType.Warning).IsAtLeast(LogMessageType.Warning));
        Assert.True(CreateMessage(LogMessageType.Error).IsAtLeast(LogMessageType.Warning));
        Assert.True(CreateMessage(LogMessageType.Fatal).IsAtLeast(LogMessageType.Warning));
    }

    [Fact]
    public void IsAtLeast_ReturnsTrueForErrorAndHigher()
    {
        Assert.False(CreateMessage(LogMessageType.Debug).IsAtLeast(LogMessageType.Error));
        Assert.False(CreateMessage(LogMessageType.Verbose).IsAtLeast(LogMessageType.Error));
        Assert.False(CreateMessage(LogMessageType.Info).IsAtLeast(LogMessageType.Error));
        Assert.False(CreateMessage(LogMessageType.Warning).IsAtLeast(LogMessageType.Error));
        Assert.True(CreateMessage(LogMessageType.Error).IsAtLeast(LogMessageType.Error));
        Assert.True(CreateMessage(LogMessageType.Fatal).IsAtLeast(LogMessageType.Error));
    }

    [Fact]
    public void IsDebug_ReturnsTrueOnlyForDebug()
    {
        Assert.True(CreateMessage(LogMessageType.Debug).IsDebug());
        Assert.False(CreateMessage(LogMessageType.Verbose).IsDebug());
        Assert.False(CreateMessage(LogMessageType.Info).IsDebug());
    }

    [Fact]
    public void IsVerbose_ReturnsTrueOnlyForVerbose()
    {
        Assert.False(CreateMessage(LogMessageType.Debug).IsVerbose());
        Assert.True(CreateMessage(LogMessageType.Verbose).IsVerbose());
        Assert.False(CreateMessage(LogMessageType.Info).IsVerbose());
    }

    [Fact]
    public void IsInfo_ReturnsTrueOnlyForInfo()
    {
        Assert.False(CreateMessage(LogMessageType.Verbose).IsInfo());
        Assert.True(CreateMessage(LogMessageType.Info).IsInfo());
        Assert.False(CreateMessage(LogMessageType.Warning).IsInfo());
    }

    [Fact]
    public void IsWarning_ReturnsTrueOnlyForWarning()
    {
        Assert.False(CreateMessage(LogMessageType.Info).IsWarning());
        Assert.True(CreateMessage(LogMessageType.Warning).IsWarning());
        Assert.False(CreateMessage(LogMessageType.Error).IsWarning());
    }

    [Fact]
    public void IsError_ReturnsTrueOnlyForError()
    {
        Assert.False(CreateMessage(LogMessageType.Warning).IsError());
        Assert.True(CreateMessage(LogMessageType.Error).IsError());
        Assert.False(CreateMessage(LogMessageType.Fatal).IsError());
    }

    [Fact]
    public void IsFatal_ReturnsTrueOnlyForFatal()
    {
        Assert.False(CreateMessage(LogMessageType.Error).IsFatal());
        Assert.True(CreateMessage(LogMessageType.Fatal).IsFatal());
    }

    private static ILogMessage CreateMessage(LogMessageType type)
    {
        return new LogMessage("TestModule", type, "Test message");
    }
}
