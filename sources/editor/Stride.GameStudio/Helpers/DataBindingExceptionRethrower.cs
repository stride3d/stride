using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Stride.GameStudio.Helpers
{
    /// <summary>
    /// A class that listens to WPF data binding events, and makes sure that the application crashes if an unhandled exception is thrown in the setter
    /// of a bound property. Normally, WPF swallows all these exceptions, which prevents user exceptions to crash the application and can leave it in a corrupted state.
    /// </summary>
    /// <remarks>
    /// Writes nothing: the default listener already prints the messages.
    /// </remarks>
    public sealed class DataBindingExceptionRethrower : TraceListener
    {
        private readonly int threadId;
        private Exception lastException;

        public DataBindingExceptionRethrower()
        {
            threadId = Thread.CurrentThread.ManagedThreadId;
            AppDomain.CurrentDomain.FirstChanceException += FirstChanceExceptionThrown;
            PresentationTraceSources.Refresh();
            PresentationTraceSources.DataBindingSource.Listeners.Add(this);
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                AppDomain.CurrentDomain.FirstChanceException -= FirstChanceExceptionThrown;
            }
            base.Dispose(disposing);
        }

        public override void Write(string message)
        {
        }

        public override void WriteLine(string message)
        {
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
        {
            ThrowIfExceptionInSetter(eventType, id);
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
        {
            ThrowIfExceptionInSetter(eventType, id);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            ThrowIfExceptionInSetter(eventType, id);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            ThrowIfExceptionInSetter(eventType, id);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            ThrowIfExceptionInSetter(eventType, id);
        }

        public override void TraceTransfer(TraceEventCache eventCache, string source, int id, string message, Guid relatedActivityId)
        {
            ThrowIfExceptionInSetter(TraceEventType.Error, id); // in doubt always consider such event as an error
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfExceptionInSetter(TraceEventType eventType, int id)
        {
            // Id 8 corresponds to the error "Cannot save value from target back to source.", meaning that an exception occurred in the setter of the source property.
            // In this scenario, we want to crash the application to avoid remaining in a corrupted state.
            if (eventType <= TraceEventType.Error && id == 8 && lastException != null)
            {
                ExceptionDispatchInfo.Capture(lastException).Throw();
            }
        }

        private void FirstChanceExceptionThrown(object sender, FirstChanceExceptionEventArgs e)
        {
            // Store the last exception that occurred in the thread this instance was created on.
            if (threadId == Thread.CurrentThread.ManagedThreadId)
            {
                lastException = e.Exception;
            }
        }
    }
}
