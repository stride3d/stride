// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

namespace Stride.Core;

/// <summary>
/// Provides a base implementation for managing services within an application. Implements the <see cref="IServiceRegistry"/> interface.
/// </summary>
/// <remarks>
/// This class uses a dictionary to store services by their types. It is thread-safe.
/// </remarks>
public class ServiceRegistry : IServiceRegistry
{
    /// <summary>
    /// The key used to identify the <see cref="ServiceRegistry"/> instance.
    /// </summary>
    public static readonly PropertyKey<IServiceRegistry> ServiceRegistryKey = new(nameof(ServiceRegistryKey), typeof(IServiceRegistry));

    private readonly Dictionary<Type, object> registeredService = [];

    /// <inheritdoc />
    public event EventHandler<ServiceEventArgs>? ServiceAdded;

    /// <inheritdoc />
    public event EventHandler<ServiceEventArgs>? ServiceRemoved;

    /// <inheritdoc />
    public T? GetService<T>()
        where T : class
    {
        var type = typeof(T);
        lock (registeredService)
        {
            if (registeredService.TryGetValue(type, out var service))
                return (T)service;
        }

        return null;
    }

    /// <inheritdoc />
    /// <remarks>
    /// This implementation triggers the <see cref="ServiceAdded"/> event after a service is successfully added.
    /// </remarks>
    public void AddService<T>(T service)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(service);

        var type = typeof(T);
        lock (registeredService)
        {
            if (!registeredService.TryAdd(type, service))
                throw new ArgumentException("Service is already registered with this type", nameof(type));
        }
        OnServiceAdded(new ServiceEventArgs(type, service));
    }

    /// <inheritdoc />
    /// <remarks>
    /// This implementation triggers the <see cref="ServiceAdded"/> event after a service is successfully added.
    /// </remarks>
    public void AddService(object service, Type type)
    {
        ArgumentNullException.ThrowIfNull(service);

        lock (registeredService)
        {
            if (!registeredService.TryAdd(type, service))
                throw new ArgumentException("Service is already registered with this type", nameof(type));
        }
        OnServiceAdded(new ServiceEventArgs(type, service));
    }

    /// <inheritdoc />
    /// <remarks>
    /// This implementation triggers the <see cref="ServiceRemoved"/> event after a service is successfully removed.
    /// If the service type is not found, this method does nothing.
    /// </remarks>
    public void RemoveService<T>()
        where T : class
    {
        var type = typeof(T);
        object? oldService;
        lock (registeredService)
        {
            registeredService.Remove(type, out oldService);
        }
        if (oldService != null)
            OnServiceRemoved(new ServiceEventArgs(type, oldService));
    }

    /// <inheritdoc />
    public bool RemoveService<T>(T serviceObject) where T : class
    {
        lock (registeredService)
        {
            if (ReferenceEquals(GetService<T>(), serviceObject))
            {
                RemoveService<T>();
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    /// <inheritdoc />
    public T GetOrCreate<T>() where T : class, IService
    {
        lock (registeredService)
        {
            var t = GetService<T>();
            if (t is null)
            {
                t = (T)T.NewInstance(this);
                AddService(t);
            }

            return t;
        }
    }

    private void OnServiceAdded(ServiceEventArgs e)
    {
        ServiceAdded?.Invoke(this, e);
    }

    private void OnServiceRemoved(ServiceEventArgs e)
    {
        ServiceRemoved?.Invoke(this, e);
    }
}
