// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using LibGit2Sharp;

namespace Stride.GameStudio.Git
{
    /// <summary>
    /// An interface that can manage git operations.
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

        public async Task<IEnumerable<GitFile>> GetChangedFiles()
        {
            return [];
        }
        public void Dispose()
        {
            _repository.Dispose();
        }
    }
}
