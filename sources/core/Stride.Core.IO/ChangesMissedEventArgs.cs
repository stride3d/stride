// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.IO;

/// <summary>
/// The directory of a <see cref="DirectoryWatcher.ChangesMissed"/> event.
/// </summary>
public sealed class ChangesMissedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangesMissedEventArgs"/> class.
    /// </summary>
    /// <param name="directory">The directory whose watch started again.</param>
    public ChangesMissedEventArgs(string directory)
    {
        Directory = directory;
    }

    /// <summary>
    /// Gets the full path of the directory whose watch started again. The changes in it and in its subdirectories may have been missed.
    /// </summary>
    /// <remarks>The watcher writes it in lower case on Windows.</remarks>
    public string Directory { get; }
}
