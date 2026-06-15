// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Diagnostics;

namespace Stride.Core.Tests.Diagnostics;

public class LogMessageTests
{
    [Fact]
    public void Constructor_CreatesMessageWithModuleAndTypeAndText()
    {
        var message = new LogMessage("TestModule", LogMessageType.Info, "Test text");

        Assert.Equal("TestModule", message.Module);
        Assert.Equal(LogMessageType.Info, message.Type);
        Assert.Equal("Test text", message.Text);
        Assert.Null(message.Exception);
        Assert.Null(message.CallerInfo);
    }

    [Fact]
    public void Constructor_CreatesMessageWithException()
    {
        var exception = new InvalidOperationException("Test exception");
        var message = new LogMessage("Module", LogMessageType.Error, "Error text", exception, null);

        Assert.Equal("Error text", message.Text);
        Assert.Same(exception, message.Exception);
    }

    [Fact]
    public void Constructor_CreatesMessageWithCallerInfo()
    {
        var callerInfo = CallerInfo.Get();
        var message = new LogMessage("Module", LogMessageType.Warning, "Warning", null, callerInfo);

        Assert.Equal("Warning", message.Text);
        Assert.Same(callerInfo, message.CallerInfo);
    }

    [Fact]
    public void Constructor_CreatesMessageWithAllParameters()
    {
        var exception = new ArgumentException("Argument error");
        var callerInfo = CallerInfo.Get();
        var message = new LogMessage("MyModule", LogMessageType.Fatal, "Fatal error", exception, callerInfo);

        Assert.Equal("MyModule", message.Module);
        Assert.Equal(LogMessageType.Fatal, message.Type);
        Assert.Equal("Fatal error", message.Text);
        Assert.Same(exception, message.Exception);
        Assert.Same(callerInfo, message.CallerInfo);
    }

    [Fact]
    public void IsAtLeast_ReturnsTrueForSameLevel()
    {
        var message = new LogMessage("Module", LogMessageType.Warning, "Text");

        Assert.True(message.IsAtLeast(LogMessageType.Warning));
    }

    [Fact]
    public void IsAtLeast_ReturnsTrueForLowerLevel()
    {
        var message = new LogMessage("Module", LogMessageType.Error, "Text");

        Assert.True(message.IsAtLeast(LogMessageType.Warning));
        Assert.True(message.IsAtLeast(LogMessageType.Info));
        Assert.True(message.IsAtLeast(LogMessageType.Debug));
    }

    [Fact]
    public void IsAtLeast_ReturnsFalseForHigherLevel()
    {
        var message = new LogMessage("Module", LogMessageType.Info, "Text");

        Assert.False(message.IsAtLeast(LogMessageType.Warning));
        Assert.False(message.IsAtLeast(LogMessageType.Error));
        Assert.False(message.IsAtLeast(LogMessageType.Fatal));
    }

    [Fact]
    public void ToString_ReturnsFormattedMessage()
    {
        var message = new LogMessage("Module", LogMessageType.Info, "Test message");

        var result = message.ToString();

        Assert.Contains("Info", result);
        Assert.Contains("Test message", result);
    }

    [Fact]
    public void ToString_IncludesExceptionWhenPresent()
    {
        var exception = new InvalidOperationException("Test exception");
        var message = new LogMessage("Module", LogMessageType.Error, "Error", exception, null);

        var result = message.ToString();

        Assert.Contains("Test exception", result);
    }
}
