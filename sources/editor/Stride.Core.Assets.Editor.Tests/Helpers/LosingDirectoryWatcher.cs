// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;

namespace Stride.Core.Assets.Editor.Tests.Helpers;

/// <summary>
/// A directory watcher that loses all its events, as a watch whose events overflow, and reports that it missed changes when told.
/// </summary>
internal sealed class LosingDirectoryWatcher : DirectoryWatcher
{
    protected override void OnModified(object sender, FileEvent e)
    {
    }

    /// <summary>
    /// Raises <see cref="DirectoryWatcher.ChangesMissed"/>, as when the watch of the directory starts again after an error.
    /// </summary>
    public void RaiseChangesMissed(string directory) => OnChangesMissed(this, new ChangesMissedEventArgs(directory));
}
