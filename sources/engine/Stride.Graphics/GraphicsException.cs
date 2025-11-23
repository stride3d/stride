// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

/// <summary>
///   An exception that is thrown when a graphics operation fails.
/// </summary>
/// <remarks>
///   This is the base class for all exceptions related to graphics operations. Look at more specific exceptions
///   to better understand the nature of the error.
/// </remarks>
public class GraphicsException : Exception
{
    private const string DefaultMessage = "An error occurred in the graphics subsystem.";

    /// <summary>
    ///   Initializes a new instance of the <see cref="GraphicsException"/> class with a default message.
    /// </summary>
    public GraphicsException() : base(DefaultMessage) { }

    /// <summary>
    ///   Initializes a new instance of the <see cref="GraphicsException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public GraphicsException(string message) : base(message) { }

    /// <summary>
    ///   Initializes a new instance of the <see cref="GraphicsException"/> class with a specified error message
    ///   and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">
    ///   The error message that explains the reason for the exception.
    ///   Specify <see langword="null"/> to use the default message.
    /// </param>
    /// <param name="innerException">
    ///   The exception that is the cause of the current exception, or a <see langword="null"/> reference if no inner exception is specified.
    /// </param>
    public GraphicsException(string? message, Exception? innerException) : base(message ?? DefaultMessage, innerException) { }
}
