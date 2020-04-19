// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.BuildEngine
{
    public abstract class SingleFileImportCommand : IndexFileCommand
    {
        /// <summary>
        /// This is useful if the asset binary format has changed and we want to bump the version to force re-evaluation/compilation of the command
        /// </summary>
        protected int Version;

        protected SingleFileImportCommand()
        {
        }

        protected SingleFileImportCommand(UFile location, UFile sourcePath)
        {
            Location = location;
            SourcePath = sourcePath;
        }

        /// <summary>
        /// Gets or sets the source path of the raw asset.
        /// </summary>
        /// <value>The source path.</value>
        public UFile SourcePath { get; set; }

        /// <summary>
        /// Gets or sets the destination location in the storage.
        /// </summary>
        /// <value>The location.</value>
        public UFile Location { get; set; }

        public override IEnumerable<ObjectUrl> GetInputFiles()
        {
            yield return new ObjectUrl(UrlType.File, SourcePath);
        }

        protected override void ComputeParameterHash(BinarySerializationWriter writer)
        {
            base.ComputeParameterHash(writer);

            writer.Write(Version);

            writer.Write(SourcePath);
            writer.Write(Location);
        }
    }
}
