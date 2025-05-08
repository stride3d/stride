// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.GameStudio.Git
{
    /// <summary>
    /// An interface that can manage git operations.
    /// </summary>
    public interface IGitService : IDisposable
    {
        public Task<IEnumerable<GitFile>> GetChangedFiles();
        public Task<bool> CommitChanges(string commitMessage);
        public Task<string> GetCurrentBranch();
        public Task<bool> CheckoutBranch(string branchName);
        public Task<bool> AddFileToStaged(string filePath);
        public Task<bool> RemoveFileFromStaged(string filePath);
        public Task<bool> AddFilesToStaged(IEnumerable<string> filePath);
        public Task<bool> RemoveFilesFromStaged(IEnumerable<string> filePath);
        public Task<bool> PushChanges();
        public Task<bool> PullChanges();
        public Task<bool> StashChanges();
        public Task<bool> StashPopChanges();
    }
}
