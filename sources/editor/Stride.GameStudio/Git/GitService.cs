// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using LibGit2Sharp;

namespace Stride.GameStudio.Git
{
    /// <summary>
    /// an implementation of <see cref="IGitService"/> that uses LibGit2Sharp to manage git operations.
    /// </summary>
    public class GitService : IGitService
    {
        private readonly Repository _repository;
        public GitService()
        {
            string projectDir = AppDomain.CurrentDomain.BaseDirectory;

            string repoPath = Repository.Discover(projectDir);

            if (repoPath == null)
            {
                throw new InvalidOperationException("Git repository not found.");
            }

            _repository = new Repository(repoPath);
        }

        public GitResult<IEnumerable<GitFile>> GetChangedFiles()
        {
            var resultFiles = new List<GitFile>();
            if (_repository == null)
                return GitResult<IEnumerable<GitFile>>.Fail("Repository not found.");

            var status = _repository.RetrieveStatus();

            foreach (var entry in status)
            {
                if (entry.State == FileStatus.Ignored || entry.State == FileStatus.Unaltered)
                    continue;

                resultFiles.Add(new GitFile
                {
                    RelativeFilePath = entry.FilePath,
                    Status = entry.State,
                    IsStaged = IsFileStaged(entry.State)
                });
            }

            return GitResult<IEnumerable<GitFile>>.Ok(resultFiles);
        }
        public GitResult<bool> CommitChanges(string commitMessage)
        {
            if (_repository == null)
                return GitResult<bool>.Fail("Repository not found.");

            var config = _repository.Config;
            var name = config.Get<string>("user.name")?.Value;
            var email = config.Get<string>("user.email")?.Value;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
                return GitResult<bool>.Fail("Git user name or email not configured.");

            var signature = new Signature(name, email, DateTimeOffset.Now);

            try
            {
                _repository.Commit(commitMessage, signature, signature);
                return GitResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return GitResult<bool>.Fail($"Failed to commit changes: {ex.Message}");
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

        public void Dispose()
        {
            _repository.Dispose();
        }
    }
}
