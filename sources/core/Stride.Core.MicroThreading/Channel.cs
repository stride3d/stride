// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xenko.Core.MicroThreading
{
    /// <summary>
    /// Provides a communication mechanism between <see cref="MicroThread"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="MicroThread"/> can send and receive to a <see cref="Channel"/>. Depending on the <see cref="Channel.Preference"/>,
    /// sending or receiving <see cref="MicroThread"/> might be suspended and yield execution to another <see cref="MicroThread"/>.
    /// </remarks>
    /// <typeparam name="T">The type of element handled by this channel.</typeparam>
    // TODO: Thread-safety
    public class Channel<T>
    {
        private readonly Queue<ChannelMicroThreadAwaiter<T>> receivers = new Queue<ChannelMicroThreadAwaiter<T>>();
        private readonly Queue<ChannelMicroThreadAwaiter<T>> senders = new Queue<ChannelMicroThreadAwaiter<T>>();
        
        public Channel()
        {
            Preference = ChannelPreference.PreferReceiver;
        }

        public void Reset()
        {
            receivers.Clear();
            senders.Clear();
        }

        /// <summary>
        /// Gets or sets the preference, allowing you to customize how <see cref="Send"/> and <see cref="Receive"/> behave regarding scheduling.
        /// </summary>
        /// <value>
        /// The preference.
        /// </value>
        public ChannelPreference Preference { get; set; }

        /// <summary>
        /// Gets the balance, which is the number of <see cref="MicroThread"/> waiting to send (if greater than 0) or receive (if smaller than 0).
        /// </summary>
        /// <value>
        /// The balance.
        /// </value>
        public int Balance { get { return senders.Count - receivers.Count; } }

        /// <summary>
        /// Sends a value over the channel. If no other <see cref="MicroThread"/> is waiting for data, the sender will be blocked.
        /// If someone was waiting for data, which of the sender or receiver continues next depends on <see cref="Preference"/>.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>Awaitable data.</returns>
        public ChannelMicroThreadAwaiter<T> Send(T data)
        {
            if (receivers.Count == 0)
            {
                // Nobody receiving, let's wait until something comes up
                var microThread = MicroThread.Current;
                var waitingMicroThread = ChannelMicroThreadAwaiter<T>.New(microThread);
                waitingMicroThread.Result = data;
                senders.Enqueue(waitingMicroThread);
                return waitingMicroThread;
            }

            var receiver = receivers.Dequeue();
            receiver.Result = data;
            if (Preference == ChannelPreference.PreferSender)
            {
                receiver.MicroThread.ScheduleContinuation(ScheduleMode.Last, receiver.Continuation);
            }
            else if (Preference == ChannelPreference.PreferReceiver)
            {
                receiver.MicroThread.ScheduleContinuation(ScheduleMode.First, receiver.Continuation);
                throw new NotImplementedException();
                //await Scheduler.Yield();
            }
            receiver.IsCompleted = true;
            return receiver;
        }

        /// <summary>
        /// Receives a value over the channel. If no other <see cref="MicroThread"/> is sending data, the receiver will be blocked.
        /// If someone was sending data, which of the sender or receiver continues next depends on <see cref="Preference"/>.
        /// </summary>
        /// <returns>Awaitable data.</returns>
        public ChannelMicroThreadAwaiter<T> Receive()
        {
            if (senders.Count == 0)
            {
                var microThread = MicroThread.Current;
                if (microThread == null)
                    throw new Exception("Cannot receive out of micro-thread context.");

                var waitingMicroThread = ChannelMicroThreadAwaiter<T>.New(microThread);
                receivers.Enqueue(waitingMicroThread);
                return waitingMicroThread;
            }

            var sender = senders.Dequeue();
            if (Preference == ChannelPreference.PreferReceiver)
            {
                sender.MicroThread.ScheduleContinuation(ScheduleMode.Last, sender.Continuation);
            }
            else if (Preference == ChannelPreference.PreferSender)
            {
                sender.MicroThread.ScheduleContinuation(ScheduleMode.First, sender.Continuation);
                throw new NotImplementedException();
                //await Scheduler.Yield();
            }
            sender.IsCompleted = true;
            return sender;
        }
    }
}
