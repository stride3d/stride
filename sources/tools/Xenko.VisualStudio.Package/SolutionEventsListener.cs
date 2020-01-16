// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Xenko.VisualStudio
{
    public class SolutionEventsListener : IVsSolutionEvents, IVsSolutionLoadEvents, IVsUpdateSolutionEvents3, IVsSelectionEvents, IDisposable
    {
        private IVsSolution solution;
        private IVsSolutionBuildManager3 buildManager;
        private IVsMonitorSelection monitorSelection;
        private uint solutionEventsCookie;
        private uint updateSolutionEventsCookie;
        private uint selectionEventsCoockie;

        public event Action AfterSolutionOpened;
        public event Action AfterSolutionBackgroundLoadComplete;
        public event Action BeforeSolutionClosed;

        public event Action<IVsHierarchy> AfterProjectOpened;
        public event Action<IVsHierarchy> BeforeProjectClosed;

        public event Action<IVsCfg, IVsCfg> AfterActiveConfigurationChange;
        public event Action<IVsHierarchy> StartupProjectChanged;

        public SolutionEventsListener(IServiceProvider serviceProvider)
        {
            solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            solution?.AdviseSolutionEvents(this, out solutionEventsCookie);

            buildManager = serviceProvider.GetService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager3;
            buildManager?.AdviseUpdateSolutionEvents3(this, out updateSolutionEventsCookie);

            monitorSelection = serviceProvider.GetService(typeof(IVsMonitorSelection)) as IVsMonitorSelection;
            monitorSelection?.AdviseSelectionEvents(this, out selectionEventsCoockie);
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

        #region IVsUpdateSolutionEvents Members

        int IVsUpdateSolutionEvents3.OnBeforeActiveSolutionCfgChange(IVsCfg pOldActiveSlnCfg, IVsCfg pNewActiveSlnCfg)
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents3.OnAfterActiveSolutionCfgChange(IVsCfg pOldActiveSlnCfg, IVsCfg pNewActiveSlnCfg)
        {
            AfterActiveConfigurationChange?.Invoke(pOldActiveSlnCfg, pNewActiveSlnCfg);
            return VSConstants.S_OK;
        }

        #endregion

        #region IVsSelectionEvents Members

        int IVsSelectionEvents.OnSelectionChanged(IVsHierarchy pHierOld, uint itemidOld, IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld, IVsHierarchy pHierNew, uint itemidNew, IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew)
        {
            return VSConstants.S_OK;
        }

        int IVsSelectionEvents.OnElementValueChanged(uint elementid, object varValueOld, object varValueNew)
        {
            if (elementid == (uint)VSConstants.VSSELELEMID.SEID_StartupProject)
            {
                StartupProjectChanged?.Invoke((IVsHierarchy)varValueNew);
            }

            return VSConstants.S_OK;
        }

        int IVsSelectionEvents.OnCmdUIContextChanged(uint dwCmdUICookie, int fActive)
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

            if (buildManager != null && updateSolutionEventsCookie != 0)
            {
                buildManager.UnadviseUpdateSolutionEvents3(updateSolutionEventsCookie);
            }

            if (monitorSelection != null && selectionEventsCoockie != 0)
            {
                monitorSelection.UnadviseSelectionEvents(selectionEventsCoockie);
            }
        }

        #endregion
    }
}
