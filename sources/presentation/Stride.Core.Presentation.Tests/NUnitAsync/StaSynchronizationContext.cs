// Source: http://stackoverflow.com/a/18838117
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NUnitAsync
{
    public class StaSynchronizationContext : SynchronizationContext, IDisposable
    {
        private BlockingQueue<SendOrPostCallbackItem> mQueue;
        private StaThread mStaThread;
        private SynchronizationContext oldSync;

        public StaSynchronizationContext()
            : base()
        {
            mQueue = new BlockingQueue<SendOrPostCallbackItem>();
            mStaThread = new StaThread(mQueue, this);
            mStaThread.Start();
            oldSync = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(this);
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            // create an item for execution
            SendOrPostCallbackItem item = new SendOrPostCallbackItem(d, state,
                                                                     ExecutionType.Send);
            // queue the item
            mQueue.Enqueue(item);
            // wait for the item execution to end
            item.ExecutionCompleteWaitHandle.WaitOne();

            // if there was an exception, throw it on the caller thread, not the
            // sta thread.
            if (item.ExecutedWithException)
                throw item.Exception;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            // queue the item and don't wait for its execution. This is risky because
            // an unhandled exception will terminate the STA thread. Use with caution.
            SendOrPostCallbackItem item = new SendOrPostCallbackItem(d, state,
                                                                     ExecutionType.Post);
            mQueue.Enqueue(item);
        }

        public void Dispose()
        {
            mStaThread.Stop();
            SynchronizationContext.SetSynchronizationContext(oldSync);
        }

        public override SynchronizationContext CreateCopy()
        {
            return this;
        }
    }
}
