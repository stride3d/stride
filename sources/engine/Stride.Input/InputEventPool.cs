// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Stride.Core.Collections;

namespace Stride.Input
{
    /// <summary>
    /// Pools input events of a given type
    /// </summary>
    /// <typeparam name="TEventType">The type of event to pool</typeparam>
    public static class InputEventPool<TEventType> where TEventType : InputEvent, new()
    {
        private static ThreadLocal<Pool> pool;

        static InputEventPool()
        {
            pool = new ThreadLocal<Pool>(
                () => new Pool());
        }

        /// <summary>
        /// The number of events in circulation, if this number keeps increasing, Enqueue is possible not called somewhere
        /// </summary>
        public static int ActiveObjects => pool.Value.ActiveObjects;

        private static TEventType CreateEvent()
        {
            return new TEventType();
        }

        /// <summary>
        /// Retrieves a new event that can be used, either from the pool or a new instance
        /// </summary>
        /// <param name="device">The device that generates this event</param>
        /// <returns>An event</returns>
        public static TEventType GetOrCreate(IInputDevice device)
        {
            return pool.Value.GetOrCreate(device);
        }
        
        /// <summary>
        /// Puts a used event back into the pool to be recycled
        /// </summary>
        /// <param name="item">The event to reuse</param>
        public static void Enqueue(TEventType item)
        {
            pool.Value.Enqueue(item);
        }

        /// <summary>
        /// Pool class, since <see cref="PoolListStruct{T}"/> can not be placed inside <see cref="ThreadLocal{T}"/>
        /// </summary>
        private class Pool
        {
            private PoolListStruct<TEventType> pool = new PoolListStruct<TEventType>(8, CreateEvent);

            public int ActiveObjects => pool.Count;

            public TEventType GetOrCreate(IInputDevice device)
            {
                TEventType item = pool.Add();
                item.Device = device;
                return item;
            }
            
            public void Enqueue(TEventType item)
            {
                item.Device = null;
                pool.Remove(item);
            }
        }
    }
}