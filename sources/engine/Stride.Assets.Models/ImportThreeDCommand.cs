// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Stride.Core.BuildEngine;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Animations;
using Stride.Importer.Common;
using Stride.Rendering;
using Stride.Rendering.Data;

namespace Stride.Assets.Models
{
    [Description("Import Assimp")]
    public class ImportThreeDCommand : ImportModelCommand
    {
        private static string[] supportedExtensions = ThreeDAssetImporter.FileExtensions.Split(';');

        /// <inheritdoc/>
        public override string Title { get { string title = "Import Assimp "; try { title += Path.GetFileName(SourcePath) ?? "[File]"; } catch { title += "[INVALID PATH]"; } return title; } }

        public static bool IsSupportingExtensions(string ext)
        {
            if (string.IsNullOrEmpty(ext))
                return false;

            var extToLower = ext.ToLowerInvariant();

            return supportedExtensions.Any(supExt => supExt.Equals(extToLower));
        }

        private Stride.Importer.ThreeD.MeshConverter CreateMeshConverter(ICommandContext commandContext)
        {
            return new Stride.Importer.ThreeD.MeshConverter(commandContext.Logger);
        }

        protected override Model LoadModel(ICommandContext commandContext, ContentManager contentManager)
        {
            var converter = CreateMeshConverter(commandContext);
            var sceneData = converter.Convert(SourcePath, Location, DeduplicateMaterials, out var sourceMaterialNames);

            // Assimp mesh material indices refer to the material order in the freshly imported source.
            // ModelAsset.Materials can still reflect the previous source revision while an asset is being
            // updated, so build a local material list in source order instead of reusing stale positions.
            AlignMaterialsWithSource(sourceMaterialNames);

            return sceneData;
        }

        private void AlignMaterialsWithSource(IReadOnlyList<string> sourceMaterialNames)
        {
            // Keep the existing fallback behavior for formats/scenes where Assimp exposes no materials.
            if (sourceMaterialNames.Count == 0)
                return;

            var materialsByName = new Dictionary<string, ModelMaterial>(StringComparer.Ordinal);
            if (Materials != null)
            {
                foreach (var material in Materials)
                {
                    if (material?.Name != null && !materialsByName.ContainsKey(material.Name))
                        materialsByName.Add(material.Name, material);
                }
            }

            var alignedMaterials = new List<ModelMaterial>(sourceMaterialNames.Count);
            foreach (var materialName in sourceMaterialNames)
            {
                if (materialsByName.TryGetValue(materialName, out var material))
                {
                    alignedMaterials.Add(material);
                }
                else
                {
                    // The source contains a new/reintroduced material that is not present in the
                    // currently saved ModelAsset yet. Keep its source slot so later material indices
                    // do not shift; UpdateAssetFromSource will reconnect its asset reference.
                    alignedMaterials.Add(new ModelMaterial
                    {
                        Name = materialName,
                        MaterialInstance = new MaterialInstance(),
                    });
                }
            }

            Materials = alignedMaterials;
        }

        protected override void ComputeParameterHash(BinarySerializationWriter writer)
        {
            base.ComputeParameterHash(writer);
            writer.Write(1); // Increment when Assimp model compilation behavior changes.
        }

        protected override Dictionary<string, AnimationClip> LoadAnimation(ICommandContext commandContext, ContentManager contentManager, out TimeSpan duration)
        {
            var meshConverter = this.CreateMeshConverter(commandContext);
            var sceneData = meshConverter.ConvertAnimation(SourcePath, Location, AnimationStack);

            duration = sceneData.Duration;
            return sceneData.AnimationClips;
        }

        protected override Skeleton LoadSkeleton(ICommandContext commandContext, ContentManager contentManager)
        {
            var meshConverter = this.CreateMeshConverter(commandContext);
            var sceneData = meshConverter.ConvertSkeleton(SourcePath, Location);
            return sceneData;
        }

        public override string ToString()
        {
            return "Import Assimp " + base.ToString();
        }
    }
}
