using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Stride.Core.Assets;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Graphics;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.Importer.Gltf
{
    public static class GltfUtils
    {
        /// <summary>
        /// Converts a System.Numerics value into a Stride value
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static Matrix ConvertNumerics(System.Numerics.Matrix4x4 mat)
        {
            return new Matrix(
                    mat.M11, mat.M12, mat.M13, mat.M14,
                    mat.M21, mat.M22, mat.M23, mat.M24,
                    mat.M31, mat.M32, mat.M33, mat.M34,
                    mat.M41, mat.M42, mat.M43, mat.M44
                );
        }
        /// <summary>
        /// Converts a System.Numerics value into a Stride value
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Quaternion ConvertNumerics(System.Numerics.Quaternion v) => new Quaternion(v.X, v.Y, v.Z, v.W);
        /// <summary>
        /// Converts a System.Numerics value into a Stride value
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector4 ConvertNumerics(System.Numerics.Vector4 v) => new Vector4(v.X, v.Y, v.Z, v.W);
        /// <summary>
        /// Converts a System.Numerics value into a Stride value
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector3 ConvertNumerics(System.Numerics.Vector3 v) => new Vector3(v.X, v.Y, v.Z);
        /// <summary>
        /// Converts a System.Numerics value into a Stride value
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector2 ConvertNumerics(System.Numerics.Vector2 v) => new Vector2(v.X, v.Y);

        /// <summary>
        /// Gets the Stride VertexElement equivalent of a GLTF vertex 
        /// </summary>
        /// <param name="accessor"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static (VertexElement, int) ConvertVertexElement(KeyValuePair<string, SharpGLTF.Schema2.Accessor> accessor, int offset)
        {
            // TODO : Simplify this part
            return (accessor.Key, accessor.Value.Format.ByteSize) switch
            {
                ("POSITION", 12) => (VertexElement.Position<Vector3>(0, offset), Vector3.SizeInBytes),
                ("NORMAL", 12) => (VertexElement.Normal<Vector3>(0, offset), Vector3.SizeInBytes),
                ("TANGENT", 12) => (VertexElement.Tangent<Vector3>(0, offset), Vector3.SizeInBytes),
                ("TANGENT", 16) => (VertexElement.Tangent<Vector4>(0, offset), Vector4.SizeInBytes),
                ("COLOR", 16) => (VertexElement.Color<Vector4>(0, offset), Vector4.SizeInBytes),
                ("COLOR_0", 16) => (VertexElement.Color<Vector4>(0, offset), Vector4.SizeInBytes),
                ("TEXCOORD_0", 8) => (VertexElement.TextureCoordinate<Vector2>(0, offset), Vector2.SizeInBytes),
                ("TEXCOORD_1", 8) => (VertexElement.TextureCoordinate<Vector2>(1, offset), Vector2.SizeInBytes),
                ("TEXCOORD_2", 8) => (VertexElement.TextureCoordinate<Vector2>(2, offset), Vector2.SizeInBytes),
                ("TEXCOORD_3", 8) => (VertexElement.TextureCoordinate<Vector2>(3, offset), Vector2.SizeInBytes),
                ("TEXCOORD_4", 8) => (VertexElement.TextureCoordinate<Vector2>(4, offset), Vector2.SizeInBytes),
                ("TEXCOORD_5", 8) => (VertexElement.TextureCoordinate<Vector2>(5, offset), Vector2.SizeInBytes),
                ("TEXCOORD_6", 8) => (VertexElement.TextureCoordinate<Vector2>(6, offset), Vector2.SizeInBytes),
                ("TEXCOORD_7", 8) => (VertexElement.TextureCoordinate<Vector2>(7, offset), Vector2.SizeInBytes),
                ("TEXCOORD_8", 8) => (VertexElement.TextureCoordinate<Vector2>(8, offset), Vector2.SizeInBytes),
                ("TEXCOORD_9", 8) => (VertexElement.TextureCoordinate<Vector2>(9, offset), Vector2.SizeInBytes),
                ("JOINTS_0", 8) => (new VertexElement(VertexElementUsage.BlendIndices, 0, PixelFormat.R16G16B16A16_UInt, offset), 8),
                ("JOINTS_0", 4) => (new VertexElement(VertexElementUsage.BlendIndices, 0, PixelFormat.R8G8B8A8_UInt, offset), 4),
                ("WEIGHTS_0", 16) => (new VertexElement(VertexElementUsage.BlendWeight, 0, PixelFormat.R32G32B32A32_Float, offset), Vector4.SizeInBytes),
                _ => throw new NotImplementedException(),
            };
        }
        /// <summary>
        /// Gets the GLTF primitive's PrimitiveType
        /// </summary>
        /// <param name="gltfType"></param>
        /// <returns></returns>
        public static PrimitiveType ConvertPrimitiveType(SharpGLTF.Schema2.PrimitiveType gltfType)
        {
            return gltfType switch
            {
                SharpGLTF.Schema2.PrimitiveType.LINES => PrimitiveType.LineList,
                SharpGLTF.Schema2.PrimitiveType.POINTS => PrimitiveType.PointList,
                SharpGLTF.Schema2.PrimitiveType.LINE_LOOP => PrimitiveType.Undefined,
                SharpGLTF.Schema2.PrimitiveType.LINE_STRIP => PrimitiveType.LineStrip,
                SharpGLTF.Schema2.PrimitiveType.TRIANGLES => PrimitiveType.TriangleList,
                SharpGLTF.Schema2.PrimitiveType.TRIANGLE_STRIP => PrimitiveType.TriangleStrip,
                SharpGLTF.Schema2.PrimitiveType.TRIANGLE_FAN => PrimitiveType.Undefined,
                _ => throw new NotImplementedException()
            };
        }

        /// <summary>
        /// Generates the texture files paths.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="sourcePath"></param>
        /// <returns></returns>
        public static List<string> GenerateTextureFullPaths(SharpGLTF.Schema2.ModelRoot root, UFile sourcePath)
        {
            var result = new List<String>();
            foreach (var mat in root.LogicalMaterials)
            {
                foreach (var chan in mat.Channels)
                {

                    if (chan.Texture != null && !chan.HasDefaultContent)
                    {

                        var gltfImg = chan.Texture.PrimaryImage;
                        if (gltfImg.Content.SourcePath == null)
                        {
                            var textureName = gltfImg.Name ?? GltfMeshParser.FirstModelName(root) + "_" + (mat.Name??mat.LogicalIndex.ToString()) + "_" + chan.Key;
                            result.Add( Path.Join(sourcePath.GetFullDirectory(), textureName + "." + gltfImg.Content.FileExtension));
                            
                        }
                        else
                        {
                            result.Add(gltfImg.Content.SourcePath);
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Generate a ComputeTextureColor for asset creation
        /// </summary>
        /// <param name="sourceTextureFile"></param>
        /// <param name="textureUVSetIndex"></param>
        /// <param name="textureUVscaling"></param>
        /// <param name="addressModeU"></param>
        /// <param name="addressModeV"></param>
        /// <param name="vfsOutputPath"></param>
        /// <returns></returns>
        public static ComputeTextureColor GenerateTextureColor(string sourceTextureFile, TextureCoordinate textureUVSetIndex, Vector2 textureUVscaling, TextureAddressMode addressModeU = TextureAddressMode.Wrap, TextureAddressMode addressModeV = TextureAddressMode.Wrap, string vfsOutputPath = "")
        {
            var textureFileName = Path.GetFileNameWithoutExtension(sourceTextureFile);

            var uvScaling = textureUVscaling;
            var textureName = textureFileName;

            var texture = AttachedReferenceManager.CreateProxyObject<Texture>(AssetId.Empty, textureName);

            var currentTexture =
                new ComputeTextureColor(texture, (TextureCoordinate)textureUVSetIndex, uvScaling, Vector2.Zero)
                {
                    AddressModeU = addressModeU,
                    AddressModeV = addressModeV
                };

            return currentTexture;
        }

        /// <summary>
        /// Generate a ComputeTextureScalar for asset creation.
        /// </summary>
        /// <param name="sourceTextureFile"></param>
        /// <param name="textureUVSetIndex"></param>
        /// <param name="textureUVscaling"></param>
        /// <param name="addressModeU"></param>
        /// <param name="addressModeV"></param>
        /// <param name="vfsOutputPath"></param>
        /// <returns></returns>
        public static ComputeTextureScalar GenerateTextureScalar(string sourceTextureFile, TextureCoordinate textureUVSetIndex, Vector2 textureUVscaling, TextureAddressMode addressModeU = TextureAddressMode.Wrap, TextureAddressMode addressModeV = TextureAddressMode.Wrap, string vfsOutputPath = "")
        {
            var textureFileName = Path.GetFileNameWithoutExtension(sourceTextureFile);
            
            var uvScaling = textureUVscaling;
            var textureName = textureFileName;

            var texture = AttachedReferenceManager.CreateProxyObject<Texture>(AssetId.Empty, textureName);

            var currentTexture =
                new ComputeTextureScalar(texture, (TextureCoordinate)textureUVSetIndex, uvScaling, Vector2.Zero)
                {
                    AddressModeU = addressModeU,
                    AddressModeV = addressModeV
                };

            return currentTexture;
        }

    }
}
