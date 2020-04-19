// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
using System;
using System.Collections.Generic;

namespace Stride.Core
{
    /// <summary>
    /// Base implementation for <see cref="IServiceRegistry"/>
    /// </summary>
    public class ServiceRegistry : IServiceRegistry
    {
        public static readonly PropertyKey<IServiceRegistry> ServiceRegistryKey = new PropertyKey<IServiceRegistry>(nameof(ServiceRegistryKey), typeof(IServiceRegistry));

        private readonly Dictionary<Type, object> registeredService = new Dictionary<Type, object>();

        /// <inheritdoc />
        public event EventHandler<ServiceEventArgs> ServiceAdded;

        /// <inheritdoc />
        public event EventHandler<ServiceEventArgs> ServiceRemoved;

        /// <inheritdoc />
        public T GetService<T>()
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
        public void AddService<T>(T service)
            where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            var type = typeof(T);
            lock (registeredService)
            {
                if (registeredService.ContainsKey(type))
                    throw new ArgumentException("Service is already registered with this type", nameof(type));
                registeredService.Add(type, service);
            }
            OnServiceAdded(new ServiceEventArgs(type, service));
        }

        /// <inheritdoc />
        public void RemoveService<T>()
            where T : class
        {
            var type = typeof(T);
            object oldService;
            lock (registeredService)
            {
                if (registeredService.TryGetValue(type, out oldService))
                    registeredService.Remove(type);
            }
            if (oldService != null)
                OnServiceRemoved(new ServiceEventArgs(type, oldService));
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
}
