// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis.Differencing;

namespace Stride.GameStudio.Git
{
    /// <summary>
    /// An interface that can manage git operations.
    /// </summary>
    public interface IGitService : IDisposable
    {
        public GitResult<IEnumerable<GitFile>> GetChangedFiles();
        public GitResult<bool> CommitChanges(string commitMessage);
        public GitResult<string> GetCurrentBranch();
        public GitResult<IEnumerable<string>> GetBranches();
        public GitResult<bool> CheckoutBranch(string branchName);
        public GitResult<bool> AddFileToStaged(string filePath);
        public GitResult<bool> RemoveFileFromStaged(string filePath);
        public GitResult<bool> AddFilesToStaged(IEnumerable<string> filePath);
        public GitResult<bool> RemoveFilesFromStaged(IEnumerable<string> filePath);
        public GitResult<bool> PushChanges();
        public GitResult<bool> PullChanges();
        public GitResult<bool> StashChanges();
        public GitResult<bool> StashPopChanges();
        public bool InitializeForSession(string solutionDir);
    }
}
