// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Stride.Core.VisualStudio
{
    /// <summary>
    /// Post actions on a <see cref="Thread"/> having <see cref="ApartmentState.STA"/>.
    /// </summary>
    internal class STAContext : IDisposable
    {
        private readonly Thread thread;
        private BlockingCollection<Task> tasks;

        public STAContext()
        {
            tasks = new BlockingCollection<Task>();

            thread = new Thread(() =>
            {
                foreach (var task in tasks.GetConsumingEnumerable())
                {
                    task.RunSynchronously();
                }
            });

            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        public T Execute<T>(Func<T> func)
        {
            var task = new Task<T>(func);

            tasks.Add(task);
            task.Wait();
            return task.Result;
        }

        public void Execute(Action action)
        {
            var task = new Task(action);

            tasks.Add(task);
            task.Wait();
        }

        public void Dispose()
        {
            if (tasks != null)
            {
                tasks.CompleteAdding();

                thread.Join();

                tasks.Dispose();
                tasks = null;
            }
        }
    }
}
