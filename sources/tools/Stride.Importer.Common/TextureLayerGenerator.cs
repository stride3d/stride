// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Graphics;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.Importer.Common
{
    public class TextureLayerGenerator
    {
        public static ComputeTextureColor GenerateMaterialTextureNode(
            string vfsOutputPath,
            string sourceTextureFile,
            uint textureUVSetIndex,
            Vector2 textureUVscaling,
            TextureAddressMode addressModeU,
            TextureAddressMode addressModeV,
            Logger logger
        )
        {
            var textureFileName = Path.GetFileNameWithoutExtension(sourceTextureFile);

            if (File.Exists(sourceTextureFile) && logger != null)
            {
                logger.Warning($"The texture '{sourceTextureFile}' referenced in the mesh material can not be found on the system. Loading will probably fail at run time.", CallerInfo.Get());
            }

            var uvScaling = textureUVscaling;
            var textureName = textureFileName;

            var texture = AttachedReferenceManager.CreateProxyObject<Texture>(AssetId.Empty, textureName);

            var currentTexture = new ComputeTextureColor(texture, (TextureCoordinate)textureUVSetIndex, uvScaling, Vector2.Zero);
            currentTexture.AddressModeU = addressModeU;
            currentTexture.AddressModeV = addressModeV;

            return currentTexture;
        }
    }
}
