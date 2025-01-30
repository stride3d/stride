// Source: http://stackoverflow.com/a/18838117
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NUnitAsync
{
    internal class StaThread
    {
        private Thread mStaThread;
        private IQueueReader<SendOrPostCallbackItem> mQueueConsumer;
        private readonly SynchronizationContext syncContext;

        private ManualResetEvent mStopEvent = new ManualResetEvent(false);


        internal StaThread(IQueueReader<SendOrPostCallbackItem> reader, SynchronizationContext syncContext)
        {
            mQueueConsumer = reader;
            this.syncContext = syncContext;
            mStaThread = new Thread(Run);
            mStaThread.Name = "STA Worker Thread";
            mStaThread.SetApartmentState(ApartmentState.STA);
        }

        internal void Start()
        {
            mStaThread.Start();
        }


        internal void Join()
        {
            mStaThread.Join();
        }

        private void Run()
        {
            SynchronizationContext.SetSynchronizationContext(syncContext);
            while (true)
            {
                bool stop = mStopEvent.WaitOne(0);
                if (stop)
                {
                    mQueueConsumer.Dispose();
                    break;
                }

                SendOrPostCallbackItem workItem = mQueueConsumer.Dequeue();
                if (workItem != null)
                    workItem.Execute();
            }
        }

        internal void Stop()
        {
            mStopEvent.Set();
            mQueueConsumer.ReleaseReader();
        }
    }
}
