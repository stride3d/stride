// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.IO;

namespace Stride.Core.Assets
{
    /// <summary>
    /// Describes a template folder.
    /// </summary>
    [DataContract("TemplateFolder")]
    public sealed class TemplateFolder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateFolder"/> class.
        /// </summary>
        public TemplateFolder() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateFolder"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        public TemplateFolder(UDirectory path)
        {
            Path = path;
            Files = new List<UFile>();
        }

        /// <summary>
        /// Gets or sets the folder relative to the package where templates are available.
        /// </summary>
        /// <value>The path.</value>
        [DataMember(10)]
        public UDirectory Path { get; set; }

        /// <summary>
        /// Gets or sets the group (used when building a package archive)
        /// </summary>
        /// <value>The group.</value>
        [DataMember(20)]
        [DefaultValue(null)]
        [UPath(UPathRelativeTo.None)]
        public UDirectory Group { get; set; }

        /// <summary>
        /// Gets or sets the exclude pattern to exclude files from package archive.
        /// </summary>
        /// <value>The exclude.</value>
        [DataMember(30)]
        [DefaultValue(null)]
        public string Exclude { get; set; }

        /// <summary>
        /// Gets or sets the files.
        /// </summary>
        /// <value>The files.</value>
        [DataMember(40)]
        public List<UFile> Files { get; private set; }
    }
}
