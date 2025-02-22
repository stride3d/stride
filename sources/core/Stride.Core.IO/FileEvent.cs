// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class

namespace Stride.Core.IO;

/// <summary>
/// � file event used notified by <see cref="DirectoryWatcher"/>
/// </summary>
public class FileEvent : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileEvent"/> class.
    /// </summary>
    /// <param name="changeType">Type of the change.</param>
    /// <param name="name">The name.</param>
    /// <param name="fullPath">The full path.</param>
    public FileEvent(FileEventChangeType changeType, string name, string fullPath)
    {
        ChangeType = changeType;
        Name = name;
        FullPath = fullPath;
    }

    /// <summary>
    /// Gets the type of the change.
    /// </summary>
    /// <value>The type of the change.</value>
    public FileEventChangeType ChangeType { get; }

    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <value>The name.</value>
    public string Name { get; }

    /// <summary>
    /// Gets the full path.
    /// </summary>
    /// <value>The full path.</value>
    public string FullPath { get; }
}

/// <summary>
/// � file rename event used notified by <see cref="DirectoryWatcher"/>
/// </summary>
public class FileRenameEvent : FileEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileRenameEvent"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="fullPath">The full path.</param>
    /// <param name="oldFullPath">The old full path. (before rename) </param>
    public FileRenameEvent(string name, string fullPath, string oldFullPath) : base(FileEventChangeType.Renamed, name, fullPath)
    {
        OldFullPath = oldFullPath;
    }

    /// <summary>
    /// Gets the full path. (in case of rename)
    /// </summary>
    /// <value>The full path. (in case of rename)</value>
    public string OldFullPath { get; }

    public override string ToString()
    {
        return $"{ChangeType}: {FullPath} -> {OldFullPath}";
    }
}
