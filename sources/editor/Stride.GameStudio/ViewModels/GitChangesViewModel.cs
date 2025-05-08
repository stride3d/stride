// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;

namespace Stride.GameStudio.ViewModels
{
    public class GitChangesViewModel : DispatcherViewModel, INotifyPropertyChanged
    {
        public GitChangesViewModel(IViewModelServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            RefreshCommand = new AnonymousCommand(serviceProvider, RefreshGitStatus);
        }

        public ICommandBase RefreshCommand { get; }
        public ObservableList<string> ChangedFiles { get; } = new();

        private void RefreshGitStatus()
        {             
            ChangedFiles.Clear();
            // TODO: Implement the logic to get the changed files from the git repository.
            var changedFiles = new List<string>()
            { "../sources/editor/Stride.GameStudio/ViewModels/GitChangesViewModel.cs",
            "../sources/editor/Stride.GameStudio/ViewModels/GitChangesViewModel.cs",
            "../sources/editor/Stride.GameStudio/ViewModels/GitChangesViewModel.cs",
            "../sources/editor/Stride.GameStudio/ViewModels/GitChangesViewModel.cs"};
            foreach (var file in changedFiles)
            {
                ChangedFiles.Add(file);
            }
        }
    }
}
