// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.ComponentModel;
using System.Text;
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
            GetStashesCommand = new AnonymousCommand(serviceProvider, GetStashes);

            RefreshCommand.Execute();
            GetBranchesCommand.Execute();
            GetCurrentBranchCommand.Execute();
            GetStashesCommand.Execute();
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
        public ICommandBase GetStashesCommand { get; private set; }

        private string commitMessage = string.Empty;
        public string CommitMessage
        {
            get => commitMessage;
            set
            {
                OnPropertyChanging(nameof(CommitMessage));
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
                }
            }
        }

        public ObservableList<string> Branches { get; private set; } = new();
        public ObservableList<GitFile> StagedFiles { get; private set; } = new();
        public ObservableList<GitFile> UnstagedFiles { get; private set; } = new();
        public ObservableList<string> StashedFiles { get; private set; } = new();

        private void ExecuteGitAction<T>(Func<GitResult<T>> action, bool refresh = true)
        {
            var result = action();
            if (result.Success && refresh)
            {
                RefreshGitStatus();
            }
        }

        private void RefreshGitStatus()
        {
            var result = gitService.GetChangedFiles();
            if (!result.Success)
            {
                return;
            }

            StagedFiles.Clear();
            UnstagedFiles.Clear();
            var changedFiles = result.Data;
            foreach (var file in changedFiles)
            {
                if (file.IsStaged)
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
            ExecuteGitAction(() => gitService.CommitChanges(CommitMessage));
        }

        private void GetCurrentBranch()
        {
            ExecuteGitAction(() =>
            {
                var result = gitService.GetCurrentBranch();
                if (!result.Success)
                {
                    return result;
                }

                CurrentBranch = result.Data;
                return result;
            }, false);
        }

        private void GetBranches()
        {
            ExecuteGitAction(() =>
            {
                var result = gitService.GetBranches();
                if (!result.Success)
                {
                    return result;
                }

                Branches.Clear();
                var branches = result.Data;
                foreach (var branch in branches)
                {
                    Branches.Add(branch);
                }
                return result;
            }, false);
        }

        private void CheckoutBranch(string branchName)
        {
            ExecuteGitAction(() => gitService.CheckoutBranch(branchName));
        }

        private void AddFileToStaged(string filePath)
        {
            ExecuteGitAction(() => gitService.AddFileToStaged(filePath));
        }

        private void RemoveFileFromStaged(string filePath)
        {
            ExecuteGitAction(() => gitService.RemoveFileFromStaged(filePath));
        }

        private void AddFilesToStaged()
        {
            IEnumerable<string> filePaths = UnstagedFiles.Select(gitFile => gitFile.RelativeFilePath);
            ExecuteGitAction(() => gitService.AddFilesToStaged(filePaths));
        }

        private void RemoveFilesFromStaged()
        {
            IEnumerable<string> filePaths = StagedFiles.Select(gitFile => gitFile.RelativeFilePath);
            ExecuteGitAction(() => gitService.RemoveFilesFromStaged(filePaths));
        }

        private void PushChanges()
        {
            ExecuteGitAction(gitService.PushChanges, false);
        }

        private void PullChanges()
        {
            ExecuteGitAction(gitService.PullChanges, false);
        }

        private void StashChanges()
        {
            ExecuteGitAction(gitService.StashChanges);
        }

        private void StashPopChanges()
        {
            ExecuteGitAction(gitService.StashPopChanges);
        }

        private void GetStashes()
        {
            ExecuteGitAction(() =>
            {
                var result = gitService.GetStashes();
                if (!result.Success)
                {
                    return result;
                }

                StashedFiles.Clear();
                var stashes = result.Data;
                foreach (var stash in stashes)
                {
                    StashedFiles.Add(stash);
                }
                return result;
            });
        }
    }
}
