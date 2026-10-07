// Source: http://stackoverflow.com/a/18838117
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NUnitAsync
{
    internal interface IQueueReader<T> : IDisposable
    {
        T Dequeue();
        void ReleaseReader();
    }

    internal interface IQueueWriter<T> : IDisposable
    {
        void Enqueue(T data);
    }


    internal class BlockingQueue<T> : IQueueReader<T>,
                                         IQueueWriter<T>, IDisposable
    {
        // use a .NET queue to store the data
        private Queue<T> mQueue = new Queue<T>();
        // create a semaphore that contains the items in the queue as resources.
        // initialize the semaphore to zero available resources (empty queue).
        private Semaphore mSemaphore = new Semaphore(0, int.MaxValue);
        // a event that gets triggered when the reader thread is exiting
        private ManualResetEvent mKillThread = new ManualResetEvent(false);
        // wait handles that are used to unblock a Dequeue operation.
        // Either when there is an item in the queue
        // or when the reader thread is exiting.
        private WaitHandle[] mWaitHandles;

        public BlockingQueue()
        {
            mWaitHandles = new WaitHandle[2] { mSemaphore, mKillThread };
        }
        public void Enqueue(T data)
        {
            lock (mQueue) mQueue.Enqueue(data);
            // add an available resource to the semaphore,
            // because we just put an item
            // into the queue.
            mSemaphore.Release();
        }

        public T Dequeue()
        {
            // wait until there is an item in the queue
            WaitHandle.WaitAny(mWaitHandles);
            lock (mQueue)
            {
                if (mQueue.Count > 0)
                    return mQueue.Dequeue();
            }
            return default(T);
        }

        public void ReleaseReader()
        {
            mKillThread.Set();
        }


        void IDisposable.Dispose()
        {
            if (mSemaphore != null)
            {
                mSemaphore.Close();
                mQueue.Clear();
                mSemaphore = null;
            }
        }
    }
}
