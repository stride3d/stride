// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

/// <summary>
///   An exception that is thrown when a Graphics Device operation fails.
/// </summary>
/// <seealso cref="GraphicsDevice"/>
public class GraphicsDeviceException : GraphicsException
{
    private const string DefaultMessage = "An error occurred in the Graphics Device.";

    /// <summary>
    ///   Gets the status of the Graphics Device when the exception occurred.
    /// </summary>
    public GraphicsDeviceStatus Status { get; } = GraphicsDeviceStatus.Normal;


    /// <summary>
    ///   Initializes a new instance of the <see cref="GraphicsException"/> class with a default message and status.
    /// </summary>
    public GraphicsDeviceException() : base(DefaultMessage) { }

    /// <summary>
    ///   Initializes a new instance of the <see cref="GraphicsException"/> class with a specified error message and status.
    /// </summary>
    /// <param name="message">
    ///   The error message that explains the reason for the exception.
    ///   Specify <see langword="null"/> to use the default message.
    /// </param>
    /// <param name="status">The status of the Graphics Device when the exception occurred.</param>
    public GraphicsDeviceException(string? message, GraphicsDeviceStatus status = GraphicsDeviceStatus.Normal)
        : base(message ?? DefaultMessage)
    {
        Status = status;
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="GraphicsException"/> class with a specified error message, status,
    ///   and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">
    ///   The error message that explains the reason for the exception.
    ///   Specify <see langword="null"/> to use the default message.
    /// </param>
    /// <param name="innerException">
    ///   The exception that is the cause of the current exception, or a <see langword="null"/> reference if no inner exception is specified.
    /// </param>
    public GraphicsDeviceException(string? message, Exception? innerException, GraphicsDeviceStatus status = GraphicsDeviceStatus.Normal)
        : base(message ?? DefaultMessage, innerException)
    {
        Status = status;
    }
}
