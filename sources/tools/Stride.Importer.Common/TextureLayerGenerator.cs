using System.IO;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;

namespace Stride.Importer.Common
{
    public class TextureLayerGenerator
    {
        public static ShaderClassSource GenerateTextureLayer(string vfsOutputPath, string sourceTextureFile, int textureUVSetIndex, Vector2 textureUVscaling,
                                        ref int textureCount, ParameterKey<Texture> surfaceMaterialKey, Mesh meshData, Logger logger)
        {
            ParameterKey<Texture> parameterKey;

            var url = vfsOutputPath + "_" + Path.GetFileNameWithoutExtension(sourceTextureFile);

            if (File.Exists(sourceTextureFile))
            {
                if (logger != null)
                {
                    logger.Warning($"The texture '{sourceTextureFile}' referenced in the mesh material can not be found on the system. Loading will probably fail at run time.", CallerInfo.Get());
                }
            }

            parameterKey = ParameterKeys.IndexedKey(surfaceMaterialKey, textureCount++);
            var uvSetName = "TEXCOORD";
            if (textureUVSetIndex != 0)
                uvSetName += textureUVSetIndex;
            //albedoMaterial->Add(gcnew ShaderClassSource("TextureStream", uvSetName, "TEXTEST" + uvSetIndex));
            var uvScaling = textureUVscaling;
            var textureName = parameterKey.Name;
            var needScaling = uvScaling != Vector2.One;
            var currentComposition = needScaling
                ? new ShaderClassSource("ComputeColorTextureRepeat", textureName, uvSetName, "float2(" + uvScaling.X + ", " + uvScaling.Y + ")")
            : new ShaderClassSource("ComputeColorTexture", textureName, uvSetName);

            return currentComposition;
        }

        public static ComputeTextureColor GenerateMaterialTextureNode(string vfsOutputPath, string sourceTextureFile, uint textureUVSetIndex, Vector2 textureUVscaling, TextureAddressMode addressModeU, TextureAddressMode addressModeV, Logger logger)
        {
            var textureFileName = Path.GetFileNameWithoutExtension(sourceTextureFile);
            var url = vfsOutputPath + "_" + textureFileName;

            if (File.Exists(sourceTextureFile))
            {
                if (logger != null)
                {
                    logger.Warning($"The texture '{sourceTextureFile}' referenced in the mesh material can not be found on the system. Loading will probably fail at run time.", CallerInfo.Get());
                }
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
