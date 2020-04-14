// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single type
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Stride.Engine.Events
{
    /// <summary>
    /// Base class for EventReceivers
    /// </summary>
    public abstract class EventReceiverBase
    {
        internal abstract Task<bool> GetPeakTask();

        internal abstract bool TryReceiveOneInternal(out object obj);
    }

    /// <summary>
    /// Base type for EventReceiver.
    /// </summary>
    /// <typeparam name="T">The type of data the EventKey will send</typeparam>
    public class EventReceiverBase<T> : EventReceiverBase, IDisposable
    {
        private IDisposable link;
        private string receivedDebugString;
        private string receivedManyDebugString;

        internal BufferBlock<T> BufferBlock;

        public EventKeyBase<T> Key { get; private set; }

        // ReSharper disable once StaticMemberInGenericType
        private static readonly DataflowBlockOptions CapacityOptions = new DataflowBlockOptions
        {
            BoundedCapacity = 1,
            TaskScheduler = EventTaskScheduler.Scheduler,
        };

        private void Init(EventKeyBase<T> key, EventReceiverOptions options)
        {
            Key = key;

            BufferBlock = ((options & EventReceiverOptions.Buffered) != 0) ? new BufferBlock<T>(new DataflowBlockOptions { TaskScheduler = EventTaskScheduler.Scheduler }) : new BufferBlock<T>(CapacityOptions);

            link = key.Connect(this);

            receivedDebugString = $"Received '{key.EventName}' ({key.EventId})";
            receivedManyDebugString = $"Received All '{key.EventName}' ({key.EventId})";

            T foo;
            InternalTryReceive(out foo); //clear any previous event, we don't want to receive old events, as broadcast block will always send us the last avail event on connect
        }

        internal EventReceiverBase(EventKeyBase<T> key, EventReceiverOptions options = EventReceiverOptions.None)
        {
            Init(key, options);
        }

        protected async Task<T> InternalReceiveAsync()
        {
            var res = await BufferBlock.ReceiveAsync();

            Key.Logger.Debug(receivedDebugString);

            return res;
        }

        public EventReceiverAwaiter<T> GetAwaiter()
        {
            return new EventReceiverAwaiter<T>(InternalReceiveAsync().GetAwaiter());
        }

        /// <summary>
        /// Returns the count of currently buffered events
        /// </summary>
        public int Count => BufferBlock.Count;

        protected bool InternalTryReceive(out T data)
        {
            if (BufferBlock.Count == 0)
            {
                data = default(T);
                return false;
            }

            data = BufferBlock.Receive();
            Key.Logger.Debug(receivedDebugString);
            return true;
        }

        protected int InternalTryReceiveAll(ICollection<T> collection)
        {
            IList<T> result;
            if (!BufferBlock.TryReceiveAll(out result))
            {
                return 0;
            }

            Key.Logger.Debug(receivedManyDebugString);

            var count = 0;
            foreach (var e in result)
            {
                count++;
                collection?.Add(e);
            }

            return count;
        }

        /// <summary>
        /// Clears all currently buffered events.
        /// </summary>
        public void Reset()
        {
            //consume all in one go
            IList<T> result;
            BufferBlock.TryReceiveAll(out result);
        }

        ~EventReceiverBase()
        {
            Dispose();
        }

        public void Dispose()
        {
            link?.Dispose();

            GC.SuppressFinalize(this);
        }

        internal override Task<bool> GetPeakTask()
        {
            return BufferBlock.OutputAvailableAsync();
        }

        internal override bool TryReceiveOneInternal(out object obj)
        {
            T res;
            if (!InternalTryReceive(out res))
            {
                obj = null;
                return false;
            }

            obj = res;
            return true;
        }
    }
}
