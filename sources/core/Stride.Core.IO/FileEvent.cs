// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class
using System;

namespace Stride.Core.IO
{
    /// <summary>
    /// � file event used notified by <see cref="DirectoryWatcher"/>
    /// </summary>
    public class FileEvent : EventArgs
    {
        private readonly FileEventChangeType changeType;
        private readonly string name;
        private readonly string fullPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileEvent"/> class.
        /// </summary>
        /// <param name="changeType">Type of the change.</param>
        /// <param name="name">The name.</param>
        /// <param name="fullPath">The full path.</param>
        public FileEvent(FileEventChangeType changeType, string name, string fullPath)
        {
            this.changeType = changeType;
            this.name = name;
            this.fullPath = fullPath;
        }

        /// <summary>
        /// Gets the type of the change.
        /// </summary>
        /// <value>The type of the change.</value>
        public FileEventChangeType ChangeType
        {
            get
            {
                return changeType;
            }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        /// Gets the full path.
        /// </summary>
        /// <value>The full path.</value>
        public string FullPath
        {
            get
            {
                return fullPath;
            }
        }
    }

    /// <summary>
    /// � file rename event used notified by <see cref="DirectoryWatcher"/>
    /// </summary>
    public class FileRenameEvent : FileEvent
    {
        private readonly string oldFullPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileRenameEvent"/> class.
        /// </summary>
        /// <param name="changeType">Type of the change.</param>
        /// <param name="name">The name.</param>
        /// <param name="fullPath">The full path.</param>
        /// <param name="oldFullPath">The old full path. (before rename) </param>
        public FileRenameEvent(string name, string fullPath, string oldFullPath) : base(FileEventChangeType.Renamed, name, fullPath)
        {
            this.oldFullPath = oldFullPath;
        }

        /// <summary>
        /// Gets the full path. (in case of rename)
        /// </summary>
        /// <value>The full path. (in case of rename)</value>
        public string OldFullPath
        {
            get { return oldFullPath; }
        }

        public override string ToString()
        {
            return string.Format("{0}: {1} -> {2}", ChangeType, FullPath, OldFullPath);
        }
    }
}
