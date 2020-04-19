// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.VisualStudio;
using Stride.Assets.Presentation.SceneEditor.Services;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Services;

namespace Stride.Assets.Presentation.View
{
    public partial class DebuggerPickerWindow : INotifyPropertyChanged, IDebuggerPickerDialog
    {
        private readonly List<Process> runningInstances;
        private IPickedDebugger selectedDebugger;

        public class Debugger : IPickedDebugger
        {
            private readonly IDEInfo ideInfo;
            private Process process;

            public Debugger(IDEInfo ideInfo)
            {
                this.ideInfo = ideInfo;
            }

            public Debugger(Process process)
            {
                this.process = process;
            }

            public string Name => process?.MainWindowTitle ?? $"New instance of {ideInfo.DisplayName}";

            public async Task<Process> Launch(SessionViewModel session)
            {
                if (process == null)
                {
                    process = await VisualStudioService.StartVisualStudio(session, ideInfo);
                    process?.WaitForInputIdle();
                }

                return process;
            }
        }

        public DebuggerPickerWindow(IEnumerable<Process> runningInstances)
        {
            this.runningInstances = runningInstances.ToList();
            InitializeComponent();
            DataContext = this;
        }

        public ObservableList<Debugger> Debuggers { get; private set; }

        public IPickedDebugger SelectedDebugger { get => selectedDebugger; set { selectedDebugger = value; OnPropertyChanged(nameof(SelectedDebugger), nameof(SelectionValid)); } }

        public bool SelectionValid => IsSelectionValid();

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(params string[] propertyNames)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                foreach (var name in propertyNames)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
            }
        }

        private bool IsSelectionValid()
        {
            if (SelectedDebugger == null)
                return false;

            return true;
        }

        async Task<DialogResult> IModalDialog.ShowModal()
        {
            Debuggers = new ObservableList<Debugger>();

            // New instances
            Debuggers.AddRange(VisualStudioVersions.AvailableVisualStudioInstances.Select(ideInfo => new Debugger(ideInfo)));

            // Running instances
            Debuggers.AddRange(runningInstances.Select(process => new Debugger(process)));

            await base.ShowModal();

            if (Result != Stride.Core.Presentation.Services.DialogResult.Ok)
            {
                SelectedDebugger = null;
            }
            return Result;
        }
    }
}
