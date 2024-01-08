// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.ViewModels;

/// <summary>
/// Arguments of the events raised by <see cref="IViewModelServiceProvider"/> implementations.
/// </summary>
public sealed class ServiceRegistrationEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceRegistrationEventArgs"/> class.
    /// </summary>
    /// <param name="service">The service related to this event.</param>
    internal ServiceRegistrationEventArgs(object service)
    {
        Service = service;
    }

    /// <summary>
    /// Gets the service related to this event.
    /// </summary>
    public object Service { get; }
}
