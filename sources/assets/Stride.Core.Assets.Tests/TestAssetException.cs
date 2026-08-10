// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Tests;

public class TestAssetException
{
    [Fact]
    public void TestConstructorWithMessage()
    {
        var message = "Test error message";
        var exception = new AssetException(message);

        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void TestConstructorWithFormattedMessage()
    {
        var format = "Error: {0}, Code: {1}";
        var arg1 = "TestError";
        var arg2 = 42;
        var exception = new AssetException(format, arg1, arg2);

        Assert.Contains("TestError", exception.Message);
        Assert.Contains("42", exception.Message);
    }

    [Fact]
    public void TestConstructorWithInnerException()
    {
        var innerException = new InvalidOperationException("Inner error");
        var message = "Outer error";
        var exception = new AssetException(message, innerException);

        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void TestFormattedMessageWithNullArguments()
    {
        var format = "Error: {0}";
        object? nullArg = null;
        var exception = new AssetException(format, nullArg!);

        Assert.Equal("Error: ", exception.Message);
    }
}
