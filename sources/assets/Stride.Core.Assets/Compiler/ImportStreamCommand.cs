// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Stride.Core.BuildEngine;
using Stride.Core.IO;
using System.Threading.Tasks;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Assets.Compiler
{
    [Description("Import stream")]
    public sealed class ImportStreamCommand : SingleFileImportCommand
    {
        /// <inheritdoc/>
        public override string Title { get { string title = "Import Stream "; try { title += Path.GetFileName(SourcePath) ?? "[File]"; } catch { title += "[INVALID PATH]"; } return title; } }

        public bool DisableCompression { get; set; }

        public bool SaveSourcePath { get; set; }

        public ImportStreamCommand() : this(null, null)
        {
        }

        public ImportStreamCommand(UFile location, UFile sourcePath)
            : base(location, sourcePath)
        {
        }

        protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
        {
            // This path for effects xml is now part of this tool, but it should be done in a separate exporter?
            using (var inputStream = File.OpenRead(SourcePath))
            using (var outputStream = MicrothreadLocalDatabases.DatabaseFileProvider.OpenStream(Location, VirtualFileMode.Create, VirtualFileAccess.Write))
            {
                inputStream.CopyTo(outputStream);

                var objectUrl = new ObjectUrl(UrlType.Content, Location);

                if (DisableCompression)
                    commandContext.AddTag(objectUrl, Builder.DoNotCompressTag);
            }

            if (SaveSourcePath)
            {
                // store absolute path to source
                // TODO: the "/path" is hardcoded, used in EffectSystem and ShaderSourceManager. Find a place to share this correctly.
                var pathLocation = new UFile(Location.FullPath + "/path");
                using (var outputStreamPath = MicrothreadLocalDatabases.DatabaseFileProvider.OpenStream(pathLocation, VirtualFileMode.Create, VirtualFileAccess.Write))
                {
                    using (var sw = new StreamWriter(outputStreamPath))
                    {
                        sw.Write(SourcePath.FullPath);
                    }
                }
            }

            return Task.FromResult(ResultStatus.Successful);
        }

        public override string ToString()
        {
            return "Import stream " + (SourcePath ?? "[File]") + " > " + (Location ?? "[Location]");
        }
    }
}
