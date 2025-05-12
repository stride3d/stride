// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using LibGit2Sharp;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TreeView;

namespace Stride.GameStudio.Git
{
    /// <summary>
    /// an implementation of <see cref="IGitService"/> that uses LibGit2Sharp to manage git operations.
    /// </summary>
    public class GitService : IGitService
    {
        private Repository repository;
        public GitService()
        {
        }

        public GitResult<IEnumerable<GitFile>> GetChangedFiles()
        {
            var resultFiles = new List<GitFile>();
            if (repository == null)
                return GitResult<IEnumerable<GitFile>>.Fail("Repository not found.");

            var status = repository.RetrieveStatus();

            foreach (var entry in status)
            {
                if (entry.State == FileStatus.Ignored || entry.State == FileStatus.Unaltered)
                    continue;

                resultFiles.Add(new GitFile
                {
                    RelativeFilePath = entry.FilePath,
                    Status = MapFileStatus(entry.State),
                    IsStaged = IsFileStaged(entry.State)
                });
            }

            return GitResult<IEnumerable<GitFile>>.Ok(resultFiles);
        }
        public GitResult<bool> CommitChanges(string commitMessage)
        {
            if (repository == null)
                return GitResult<bool>.Fail("Repository not found.");

            var config = repository.Config;
            var name = config.Get<string>("user.name")?.Value;
            var email = config.Get<string>("user.email")?.Value;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
                return GitResult<bool>.Fail("Git user name or email not configured.");

            var signature = new Signature(name, email, DateTimeOffset.Now);

            try
            {
                repository.Commit(commitMessage, signature, signature);
                return GitResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return GitResult<bool>.Fail($"Failed to commit changes: {ex.Message}");
            }
        }
        public GitResult<string> GetCurrentBranch()
        {
            if (repository == null)
                return GitResult<string>.Fail("Repository not found.");
            var branch = repository.Head;
            if (branch == null)
                return GitResult<string>.Fail("No current branch found.");
            return GitResult<string>.Ok(branch.FriendlyName);
        }
        public GitResult<IEnumerable<string>> GetBranches()
        {
            if (repository == null)
                return GitResult<IEnumerable<string>>.Fail("Repository not found.");
            var branches = repository.Branches.Where(b=>!b.IsRemote).Select(b => b.FriendlyName).ToList();
            if (branches.Count == 0)
            {
                var currentBranch = repository.Head;
                if (currentBranch == null)
                    return GitResult<IEnumerable<string>>.Fail("No current branch found.");
                branches.Add(currentBranch.FriendlyName);
            }
            return GitResult<IEnumerable<string>>.Ok(branches);
        }
        public GitResult<bool> CheckoutBranch(string branchName)
        {
            if (repository == null)
                return GitResult<bool>.Fail("Repository not found.");

            var branch = repository.Branches[branchName];
            if (branch == null)
                return GitResult<bool>.Fail($"Branch '{branchName}' not found.");

            Commands.Checkout(repository, branch);
            return GitResult<bool>.Ok(true);
        }

        public GitResult<bool> AddFileToStaged(string filePath)
        {
            if (repository == null)
                return GitResult<bool>.Fail("Repository not found.");
            try
            {
                Commands.Stage(repository, filePath);
                return GitResult<bool>.Ok(true);
            }
            catch
            {
                return GitResult<bool>.Fail($"Failed to stage file: {filePath}");
            }
        }

        public GitResult<bool> RemoveFileFromStaged(string filePath)
        {
            if (repository == null)
                return GitResult<bool>.Fail("Repository not found.");
            try
            {
                Commands.Unstage(repository, filePath);
                return GitResult<bool>.Ok(true);
            }
            catch
            {
                return GitResult<bool>.Fail($"Failed to unstage file: {filePath}");
            }
        }

        public GitResult<bool> AddFilesToStaged(IEnumerable<string> filePath)
        {
            if (repository == null)
                return GitResult<bool>.Fail("Repository not found.");

            try
            {
                foreach (var file in filePath)
                {
                    Commands.Stage(repository, file);
                }
                return GitResult<bool>.Ok(true);
            }
            catch
            {
                return GitResult<bool>.Fail($"Failed to stage files: {string.Join(", ", filePath)}");
            }
        }

        public GitResult<bool> RemoveFilesFromStaged(IEnumerable<string> filePath)
        {
            if (repository == null)
                return GitResult<bool>.Fail("Repository not found.");
            try
            {
                foreach (var file in filePath)
                {
                    Commands.Unstage(repository, file);
                }
                return GitResult<bool>.Ok(true);
            }
            catch
            {
                return GitResult<bool>.Fail($"Failed to unstage files: {string.Join(", ", filePath)}");
            }
        }

        public GitResult<bool> PushChanges()
        {
            if (repository == null)
                return GitResult<bool>.Fail("Repository not found.");

            try
            {
                var remote = repository.Network.Remotes["origin"];
                var options = new PushOptions();
                repository.Network.Push(remote, @"refs/heads/" + repository.Head.FriendlyName, options);
                return GitResult<bool>.Ok(true);
            }
            catch
            {
                return GitResult<bool>.Fail("Failed to push changes.");
            }
        }

        public GitResult<bool> PullChanges()
        {
            if (repository == null)
                return GitResult<bool>.Fail("Repository not found.");

            var config = repository.Config;
            var name = config.Get<string>("user.name")?.Value;
            var email = config.Get<string>("user.email")?.Value;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
                return GitResult<bool>.Fail("Git user name or email not configured.");

            var signature = new Signature(name, email, DateTimeOffset.Now);

            try
            {
                var options = new PullOptions
                {
                    FetchOptions = new FetchOptions()
                };
                Commands.Pull(repository, signature, options);
                return GitResult<bool>.Ok(true);
            }
            catch
            {
                return GitResult<bool>.Fail("Failed to pull changes.");
            }
        }

        public GitResult<bool> StashChanges()
        {
            if (repository == null)
                return GitResult<bool>.Fail("Repository not found.");

            var config = repository.Config;
            var name = config.Get<string>("user.name")?.Value;
            var email = config.Get<string>("user.email")?.Value;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
                return GitResult<bool>.Fail("Git user name or email not configured.");

            var signature = new Signature(name, email, DateTimeOffset.Now);

            try
            {
                repository.Stashes.Add(signature, null, StashModifiers.Default);
                return GitResult<bool>.Ok(true);
            }
            catch
            {
                return GitResult<bool>.Fail("Failed to stash changes.");
            }
        }

        public GitResult<bool> StashPopChanges()
        {
            if (repository == null || repository.Stashes.Count() == 0)
                return GitResult<bool>.Fail("Repository not found or no stashes available.");

            try
            {
                repository.Stashes.Pop(0);
                return GitResult<bool>.Ok(true);
            }
            catch
            {
                return GitResult<bool>.Fail("Failed to pop stash changes.");
            }
        }
        private static bool IsFileStaged(FileStatus status)
        {
            return status.HasFlag(FileStatus.NewInIndex)
                || status.HasFlag(FileStatus.ModifiedInIndex)
                || status.HasFlag(FileStatus.DeletedFromIndex)
                || status.HasFlag(FileStatus.TypeChangeInIndex)
                || status.HasFlag(FileStatus.RenamedInIndex);
        }
        private static GitFileStatus MapFileStatus(FileStatus status)
        {
            if (status.HasFlag(FileStatus.NewInIndex) || status.HasFlag(FileStatus.NewInWorkdir))
                return GitFileStatus.Added;

            if (status.HasFlag(FileStatus.DeletedFromIndex) || status.HasFlag(FileStatus.DeletedFromWorkdir))
                return GitFileStatus.Deleted;

            if (status.HasFlag(FileStatus.ModifiedInIndex) || status.HasFlag(FileStatus.ModifiedInWorkdir))
                return GitFileStatus.Modified;

            if (status.HasFlag(FileStatus.Ignored) || status.HasFlag(FileStatus.Unaltered))
                return GitFileStatus.Untracked;

            return GitFileStatus.Untracked;
        }
        public bool InitializeForSession(string solutionDir)
        {
            if (string.IsNullOrWhiteSpace(solutionDir))
                return false;
            try
            {
                repository?.Dispose();
                repository = null;

                var repoPath = Repository.Discover(solutionDir);
                if (repoPath != null && Repository.IsValid(repoPath))
                {
                    repository = new Repository(repoPath);
                    return true;
                }

                Repository.Init(solutionDir);
                repository = new Repository(solutionDir);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            repository?.Dispose();
        }
    }
}
