// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Xenko.Engine.Events
{
    public struct EventReceiverAwaiter<T> : INotifyCompletion
    {
        private TaskAwaiter<T> task;

        public EventReceiverAwaiter(TaskAwaiter<T> task)
        {
            this.task = task;
        }

        public void OnCompleted(Action continuation)
        {
            task.OnCompleted(continuation);
        }

        public bool IsCompleted => task.IsCompleted;

        public T GetResult()
        {
            return task.GetResult();
        }
    }

    /// <summary>
    /// When using EventReceiver.ReceiveOne, this structure is used to contain the received data
    /// </summary>
    public struct EventData
    {
        public EventReceiverBase Receiver { get; internal set; }

        public object Data { get; internal set; }
    }

    /// <summary>
    /// Creates an event receiver that is used to receive T type events from an EventKey
    /// </summary>
    /// <typeparam name="T">The type of data the EventKey will send</typeparam>
    public sealed class EventReceiver<T> : EventReceiverBase<T>
    {
        /// <summary>
        /// Creates an event receiver, ready to receive broadcasts from the key
        /// </summary>
        /// <param name="key">The event key to listen from</param>
        /// <param name="options">Option flags</param>
        public EventReceiver(EventKey<T> key, EventReceiverOptions options = EventReceiverOptions.None) : base(key, options)
        {
        }

        /// <summary>
        /// Awaits a single event
        /// </summary>
        /// <returns></returns>
        public Task<T> ReceiveAsync()
        {
            return InternalReceiveAsync();
        }

        /// <summary>
        /// Receives one event from the buffer, useful specially in Sync scripts
        /// </summary>
        /// <returns></returns>
        public bool TryReceive(out T data)
        {
            return InternalTryReceive(out data);
        }

        /// <summary>
        /// Receives all the events from the queue (if buffered was true during creations), useful mostly only in Sync scripts
        /// </summary>
        /// <returns></returns>
        public int TryReceiveAll(ICollection<T> collection)
        {
            return InternalTryReceiveAll(collection);
        }
    }

    /// <summary>
    /// Creates an event receiver that is used to receive events from an EventKey
    /// </summary>
    public sealed class EventReceiver : EventReceiverBase<bool>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventReceiver"/> class.
        /// Creates an event receiver, ready to receive broadcasts from the key.
        /// </summary>
        /// <param name="key">The event key to listen from</param>
        /// <param name="options">Option flags</param>
        public EventReceiver(EventKey key, EventReceiverOptions options = EventReceiverOptions.None) : base(key, options)
        {
        }

        /// <summary>
        /// Awaits a single event
        /// </summary>
        /// <returns></returns>
        public async Task ReceiveAsync()
        {
            await InternalReceiveAsync();
        }

        /// <summary>
        /// Receives one event from the buffer, useful specially in Sync scripts
        /// </summary>
        /// <returns></returns>
        public bool TryReceive()
        {
            bool foo;
            return InternalTryReceive(out foo);
        }

        /// <summary>
        /// Receives all the events from the queue (if buffered was true during creations), useful mostly only in Sync scripts
        /// </summary>
        /// <returns></returns>
        public int TryReceiveAll()
        {
            return InternalTryReceiveAll(null);
        }

        /// <summary>
        /// Combines multiple receivers in one call and tries to receive the first available events among all the passed receivers
        /// </summary>
        /// <param name="events">The events you want to listen to</param>
        /// <returns></returns>
        public static async Task<EventData> ReceiveOne(params EventReceiverBase[] events)
        {
            while (true)
            {
                var tasks = new Task[events.Length];
                for (var i = 0; i < events.Length; i++)
                {
                    tasks[i] = events[i].GetPeakTask();
                }

                await Task.WhenAny(tasks);

                for (var i = 0; i < events.Length; i++)
                {
                    if (!tasks[i].IsCompleted) continue;

                    object data;
                    if (!events[i].TryReceiveOneInternal(out data)) continue;

                    var res = new EventData
                    {
                        Data = data,
                        Receiver = events[i],
                    };
                    return res;
                }
            }
        }
    }
}
