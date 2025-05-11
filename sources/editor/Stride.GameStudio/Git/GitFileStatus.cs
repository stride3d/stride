// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.GameStudio.Git
{
    /// <summary>
    /// Represents the status of a file in a Git repository.
    /// </summary>
    public enum GitFileStatus
    {
        Added,
        Deleted,
        Modified,
        Untracked
    }
}
