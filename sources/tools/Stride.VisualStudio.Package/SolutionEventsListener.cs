// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Stride.VisualStudio
{
    public class SolutionEventsListener : IVsSolutionEvents, IVsSolutionLoadEvents, IDisposable
    {
        private IVsSolution solution;
        private uint solutionEventsCookie;

        public event Action AfterSolutionOpened;
        public event Action AfterSolutionBackgroundLoadComplete;
        public event Action BeforeSolutionClosed;

        public event Action<IVsHierarchy> AfterProjectOpened;
        public event Action<IVsHierarchy> BeforeProjectClosed;

        public SolutionEventsListener(IServiceProvider serviceProvider)
        {
            solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            solution?.AdviseSolutionEvents(this, out solutionEventsCookie);
        }

        public int OnBeforeOpenSolution(string pszSolutionFilename)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeBackgroundSolutionLoadBegins()
        {
            return VSConstants.S_OK;
        }

        public int OnQueryBackgroundLoadProjectBatch(out bool pfShouldDelayLoadToNextIdle)
        {
            pfShouldDelayLoadToNextIdle = false;
            return VSConstants.S_OK;
        }

        public int OnBeforeLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterBackgroundSolutionLoadComplete()
        {
            AfterSolutionBackgroundLoadComplete?.Invoke();
            return VSConstants.S_OK;
        }

        #region IVsSolutionEvents Members

        int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            AfterProjectOpened?.Invoke(pHierarchy);
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            AfterSolutionOpened?.Invoke();
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            var beforeProjectClosed = BeforeProjectClosed;
            beforeProjectClosed?.Invoke(pHierarchy);
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved)
        {
            BeforeSolutionClosed?.Invoke();
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (solution != null && solutionEventsCookie != 0)
            {
                solution.UnadviseSolutionEvents(solutionEventsCookie);
            }
        }

        #endregion
    }
}
