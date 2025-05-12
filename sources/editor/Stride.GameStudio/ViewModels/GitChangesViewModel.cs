// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.ComponentModel;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;
using Stride.GameStudio.Git;

namespace Stride.GameStudio.ViewModels
{
    public class GitChangesViewModel : DispatcherViewModel, INotifyPropertyChanged
    {
        private readonly IGitService gitService;
        public GitChangesViewModel(IViewModelServiceProvider serviceProvider) : base(serviceProvider)
        {
            gitService = serviceProvider.Get<GitService>();

            RefreshCommand = new AnonymousCommand(serviceProvider, RefreshGitStatus);
            CommitChangesCommand = new AnonymousCommand(serviceProvider, CommitChanges);
            GetCurrentBranchCommand = new AnonymousCommand(serviceProvider, GetCurrentBranch);
            GetBranchesCommand = new AnonymousCommand(serviceProvider, GetBranches);
            CheckoutBranchCommand = new AnonymousCommand<string>(serviceProvider, CheckoutBranch);
            AddFileToStagedCommand = new AnonymousCommand<string>(serviceProvider, AddFileToStaged);
            RemoveFileFromStagedCommand = new AnonymousCommand<string>(serviceProvider, RemoveFileFromStaged);
            AddFilesToStagedCommand = new AnonymousCommand(serviceProvider, AddFilesToStaged);
            RemoveFilesFromStagedCommand = new AnonymousCommand(serviceProvider, RemoveFilesFromStaged);
            PushChangesCommand = new AnonymousCommand(serviceProvider, PushChanges);
            PullChangesCommand = new AnonymousCommand(serviceProvider, PullChanges);
            StashChangesCommand = new AnonymousCommand(serviceProvider, StashChanges);
            StashPopChangesCommand = new AnonymousCommand(serviceProvider, StashPopChanges);

            RefreshCommand.Execute();
            GetBranchesCommand.Execute();
            GetCurrentBranchCommand.Execute();
        }

        public ICommandBase RefreshCommand { get; private set; }
        public ICommandBase CommitChangesCommand { get; private set; }
        public ICommandBase GetCurrentBranchCommand { get; private set; }
        public ICommandBase GetBranchesCommand { get; private set; }
        public ICommandBase CheckoutBranchCommand { get; private set; }
        public ICommandBase AddFileToStagedCommand { get; private set; }
        public ICommandBase RemoveFileFromStagedCommand { get; private set; }
        public ICommandBase AddFilesToStagedCommand { get; private set; }
        public ICommandBase RemoveFilesFromStagedCommand { get; private set; }
        public ICommandBase PushChangesCommand { get; private set; }
        public ICommandBase PullChangesCommand { get; private set; }
        public ICommandBase StashChangesCommand { get; private set; }
        public ICommandBase StashPopChangesCommand { get; private set; }

        private string commitMessage = string.Empty;
        public string CommitMessage
        {
            get => commitMessage;
            set
            {
                commitMessage = value;
                OnPropertyChanged(nameof(CommitMessage));
            }
        }

        private string currentBranch = string.Empty;
        public string CurrentBranch
        {
            get => currentBranch;
            set
            {
                if (currentBranch != value && Branches.Contains(value))
                {
                    OnPropertyChanging(nameof(CurrentBranch));
                    currentBranch = value;
                    OnPropertyChanged(nameof(CurrentBranch));

                    if (CheckoutBranchCommand?.CanExecute(value) == true)
                    {
                        CheckoutBranchCommand.Execute(value);
                    }
                    if (RefreshCommand?.CanExecute(value) == true)
                    {
                        RefreshCommand.Execute(value);
                    }
                }
            }
        }

        public ObservableList<string> Branches { get; private set; } = new();
        public ObservableList<GitFile> StagedFiles { get; private set; } = new();
        public ObservableList<GitFile> UnstagedFiles { get; private set; } = new();

        private void RefreshGitStatus()
        {
            var result = gitService.GetChangedFiles();
            if(!result.Success)
            {
                return;
            }

            StagedFiles.Clear();
            UnstagedFiles.Clear();
            var changedFiles = result.Data;
            foreach (var file in changedFiles)
            {
                if(file.IsStaged)
                {
                    StagedFiles.Add(file);
                }
                else
                {
                    UnstagedFiles.Add(file);
                }
            }
        }

        private void CommitChanges()
        {
            if (string.IsNullOrWhiteSpace(CommitMessage))
            {
                return;
            }
            var result = gitService.CommitChanges(CommitMessage);
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
            CurrentBranch = result.Data;
        }

        private void GetBranches()
        {
            var result = gitService.GetBranches();
            if (!result.Success)
            {
                return;
            }

            Branches.Clear();
            var branches = result.Data;
            foreach (var branch in branches)
            {
                Branches.Add(branch);
            }
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
            RefreshGitStatus();
        }

        private void RemoveFileFromStaged(string filePath)
        {
            var result = gitService.RemoveFileFromStaged(filePath);
            if (!result.Success)
            {
                return;
            }
            var isRemoved = result.Data;
            RefreshGitStatus();
        }

        private void AddFilesToStaged()
        {
            IEnumerable<string> filePaths = UnstagedFiles.Select(gitFile => gitFile.RelativeFilePath);
            var result = gitService.AddFilesToStaged(filePaths);
            if (!result.Success)
            {
                return;
            }
            var areAdded = result.Data;
            RefreshGitStatus();
        }

        private void RemoveFilesFromStaged()
        {
            IEnumerable<string> filePaths = StagedFiles.Select(gitFile => gitFile.RelativeFilePath);
            var result = gitService.RemoveFilesFromStaged(filePaths);
            if (!result.Success)
            {
                return;
            }
            var areRemoved = result.Data;
            RefreshGitStatus();
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
