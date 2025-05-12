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
using Stride.GameStudio.Git;

namespace Stride.GameStudio.ViewModels
{
    public class GitChangesViewModel : DispatcherViewModel, INotifyPropertyChanged
    {
        private readonly IGitService gitService;
        public GitChangesViewModel(IViewModelServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            gitService = serviceProvider.Get<GitService>();
            RefreshCommand = new AnonymousCommand(serviceProvider, RefreshGitStatus);
            CommitChangesCommand = new AnonymousCommand(serviceProvider, () => CommitChanges("Initial commit"));
            RefreshCommand.Execute();
        }

        public ICommandBase RefreshCommand { get; private set; }
        public ICommandBase CommitChangesCommand { get; private set; }
        public ICommandBase GetCurrentBranchCommand { get; private set; }
        public ICommandBase CheckoutBranchCommand { get; private set; }
        public ICommandBase AddFileToStagedCommand { get; private set; }
        public ICommandBase RemoveFileFromStagedCommand { get; private set; }
        public ICommandBase AddFilesToStagedCommand { get; private set; }
        public ICommandBase RemoveFilesFromStagedCommand { get; private set; }
        public ICommandBase PushChangesCommand { get; private set; }
        public ICommandBase PullChangesCommand { get; private set; }
        public ICommandBase StashChangesCommand { get; private set; }
        public ICommandBase StashPopChangesCommand { get; private set; }

        public ObservableList<GitFile> ChangedFiles { get; } = new();

        private void RefreshGitStatus()
        {             
            ChangedFiles.Clear();
            var result = gitService.GetChangedFiles();
            if(!result.Success)
            {
                return;
            }

            var changedFiles = result.Data;
            foreach (var file in changedFiles)
            {
                ChangedFiles.Add(file);
            }
        }

        private void CommitChanges(string commitMessage)
        {
            var result = gitService.CommitChanges(commitMessage);
            if (!result.Success)
            {
                return;
            }
            var isCommitted = result.Data;
        }

        private void GetCurrentBranch()
        {
            var result = gitService.GetCurrentBranch();
            if (!result.Success)
            {
                return;
            }
            var currentBranch = result.Data;
        }

        private void CheckoutBranch(string branchName)
        {
            var result = gitService.CheckoutBranch(branchName);
            if (!result.Success)
            {
                return;
            }
            var isCheckedOut = result.Data;
        }

        private void AddFileToStaged(string filePath)
        {
            var result = gitService.AddFileToStaged(filePath);
            if (!result.Success)
            {
                return;
            }
            var isAdded = result.Data;
        }

        private void RemoveFileFromStaged(string filePath)
        {
            var result = gitService.RemoveFileFromStaged(filePath);
            if (!result.Success)
            {
                return;
            }
            var isRemoved = result.Data;
        }

        private void AddFilesToStaged(IEnumerable<string> filePaths)
        {
            var result = gitService.AddFilesToStaged(filePaths);
            if (!result.Success)
            {
                return;
            }
            var areAdded = result.Data;
        }

        private void RemoveFilesFromStaged(IEnumerable<string> filePaths)
        {
            var result = gitService.RemoveFilesFromStaged(filePaths);
            if (!result.Success)
            {
                return;
            }
            var areRemoved = result.Data;
        }

        private void PushChanges()
        {
            var result = gitService.PushChanges();
            if (!result.Success)
            {
                return;
            }
            var isPushed = result.Data;
        }

        private void PullChanges()
        {
            var result = gitService.PullChanges();
            if (!result.Success)
            {
                return;
            }
            var isPulled = result.Data;
        }

        private void StashChanges()
        {
            var result = gitService.StashChanges();
            if (!result.Success)
            {
                return;
            }
            var isStashed = result.Data;
        }

        private void StashPopChanges()
        {
            var result = gitService.StashPopChanges();
            if (!result.Success)
            {
                return;
            }
            var isStashPopped = result.Data;
        }
    }
}
