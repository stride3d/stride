// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using Stride.Core;
using Stride.Core.IO;

namespace Stride.Core.Assets
{
    /// <summary>
    /// A location relative to a package from where assets will be loaded
    /// </summary>
    [DataContract("AssetFolder")]
    [DebuggerDisplay("Assets: {Path}")]
    public sealed class AssetFolder
    {
        private UDirectory path;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetFolder"/> class.
        /// </summary>
        public AssetFolder()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetFolder"/> class.
        /// </summary>
        /// <param name="path">The folder.</param>
        public AssetFolder(UDirectory path) : this()
        {
            if (path == null) throw new ArgumentNullException("path");
            this.path = path;
        }

        /// <summary>
        /// Gets or sets the folder.
        /// </summary>
        /// <value>The folder.</value>
        public UDirectory Path
        {
            get
            {
                return path;
            }
            set
            {
                if (value == null) throw new ArgumentNullException();
                path = value;
            }
        }

        public AssetFolder Clone()
        {
            var sourceFolder = new AssetFolder();
            if (Path != null)
            {
                sourceFolder.Path = path;
            }
            return sourceFolder;
        }
    }
}
