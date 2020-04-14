// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Stride.Core.BuildEngine;
using Stride.Core.Serialization.Contents;
using Stride.Animations;
using Stride.Rendering;

namespace Stride.Assets.Models
{
    [Description("Import FBX")]
    public class ImportFbxCommand : ImportModelCommand
    {
        /// <inheritdoc/>
        public override string Title { get { string title = "Import FBX "; try { title += Path.GetFileName(SourcePath) ?? "[File]"; } catch { title += "[INVALID PATH]"; } return title; } }

        public static bool IsSupportingExtensions(string ext)
        {
            return !String.IsNullOrEmpty(ext) && ext.ToLowerInvariant().Equals(".fbx");
        }

        protected override Model LoadModel(ICommandContext commandContext, ContentManager contentManager)
        {
            var meshConverter = CreateMeshConverter(commandContext);
            var materialMapping = Materials.Select((s, i) => new { Value = s, Index = i }).ToDictionary(x => x.Value.Name, x => x.Index);
            var sceneData = meshConverter.Convert(SourcePath, Location, materialMapping);
            return sceneData;
        }

        protected override Dictionary<string, AnimationClip> LoadAnimation(ICommandContext commandContext, ContentManager contentManager, out TimeSpan duration)
        {
            var meshConverter = CreateMeshConverter(commandContext);
            var sceneData = meshConverter.ConvertAnimation(SourcePath, Location, ImportCustomAttributes);
            duration = sceneData.Duration;
            return sceneData.AnimationClips;
        }

        protected override Skeleton LoadSkeleton(ICommandContext commandContext, ContentManager contentManager)
        {
            var meshConverter = CreateMeshConverter(commandContext);
            var sceneData = meshConverter.ConvertSkeleton(SourcePath, Location);
            return sceneData;
        }

        private Importer.FBX.MeshConverter CreateMeshConverter(ICommandContext commandContext)
        {
            return new Importer.FBX.MeshConverter(commandContext.Logger)
            {
                AllowUnsignedBlendIndices = AllowUnsignedBlendIndices,
            };
        }

        public override bool ShouldSpawnNewProcess()
        {
            return true;
        }

        public override string ToString()
        {
            return "Import FBX " + base.ToString();
        }
    }
}
