using System;
using System.Collections.Generic;
using System.Numerics;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Assets;
using Stride.Core.BuildEngine;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Graphics;
using Stride.TextureConverter;
using Vector2 = Stride.Core.Mathematics.Vector2;
using Vector3 = Stride.Core.Mathematics.Vector3;

namespace Stride.BepuPhysics.Definitions.Heightfield.Assets;

/// <summary>
/// Hosts a terrain heightmap generator for Runevision's Fast and Gorgeous Erosion Filter,
/// https://blog.runevision.com/2026/03/fast-and-gorgeous-erosion-filter.html
/// </summary>
[DataContract]
[AssetDescription(FileExtension)]
[AssetContentType(typeof(Texture))]
public sealed class RunevisionTerrainAsset : Asset
{
    public const string FileExtension = ".rvtrn";

    /// <summary>
    /// Resolution of the output texture, amount of pixels would be <see cref="Resolution"/>^2
    /// </summary>
    [DataMemberRange(minimum: 32, maximum: 16384, 0, 0, 0)]
    public int Resolution 
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 32);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 16384);
            field = value;
        }
    } = 1024;

    /// <summary>
    /// The width of the terrain captured within the texture
    /// </summary>
    public float Tiling { get; set; } = 1f;

    /// <summary>
    /// A multiplier over the height output by the generator, occurs after <see cref="Normalization"/>
    /// </summary>
    public float Height { get; set; } = 1f;

    /// <inheritdoc cref="NormalizationMethod"/>
    public NormalizationMethod Normalization { get; set; } = NormalizationMethod.Root;

    /// <summary>
    /// The format the generated texture will be in
    /// </summary>
    public PixelFormat Format { get; set; } = PixelFormat.R16G16B16A16_Float;

    /// <inheritdoc cref="DataTypes"/>
    /// <remarks>
    /// Data is laid out into the textures' RGBA channel sequentially, from low to high bit set.
    /// For example <see cref="DataTypes.Ridges"/>(0b0100) | <see cref="DataTypes.Height"/>(0b0001)
    /// would have the latter in the R and former in the G channel.
    /// </remarks>
    public DataTypes Data
    { 
        get;
        set
        {
            if (BitOperations.PopCount((uint)value) > 4)
                throw new ArgumentOutOfRangeException($"More than 4 {nameof(Data)} listed");

            field = value;
        } 
    } = DataTypes.Height | DataTypes.Erosion | DataTypes.Ridges | DataTypes.TreesWhenEnabled;

    /// <summary>
    /// The input heightfield data to apply the erosion filter over
    /// </summary>
    public required IHeightInput HeightInput
    { 
        get;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        } 
    } = new FractalNoise();

    /// <summary>
    /// The settings used by the erosion filter
    /// </summary>
    public RunevisionTerrainErosion.Settings Settings
    { 
        get;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        } 
    } = new();

    /// <summary>
    /// An input heightfield to erode through <see cref="RunevisionTerrainAsset"/>
    /// </summary>
    public interface IHeightInput
    {
        /// <summary>
        /// The function the erosion filter will use when sampling height for a given coordinate
        /// </summary>
        RunevisionTerrainErosion.SampleHeight BuildSampler();

        /// <summary>
        /// The assets required to build the sampler to evaluate heights
        /// </summary>
        IEnumerable<ObjectUrl> GetInputFiles();
    }

    /// <summary>
    /// A basic pseudo-random simple terrain generator
    /// </summary>
    [DataContract]
    public class FractalNoise : IHeightInput
    {
        /// <summary>
        /// The inverse horizontal scale of the terrain noise function.
        /// </summary>
        public float HeightFrequency = 3.0f;

        /// <summary>
        /// The vertical scale (amplitude) of the terrain noise function.
        /// </summary>
        public float HeightAmp = 0.125f;

        /// <summary>
        /// Control over the noise function octaves, with each successive
        /// octave layering smaller bumps onto the terrain.
        /// </summary>
        public int HeightOctaves = 3;

        /// <summary>
        /// The lacunarity controls the frequency (the inverse
        /// horizontal scale) of each octave relative to the last.
        /// </summary>
        public float HeightLacunarity = 2.0f;

        /// <summary>
        /// The gain controls the magnitude (the vertical scale)
        /// of each octave relative to the last.
        /// </summary>
        public float HeightGain = 0.1f;

        /// <summary>
        /// The final height multiplier
        /// </summary>
        public float HeightFunctionScale = 1f;

        /// <inheritdoc/>
        public RunevisionTerrainErosion.SampleHeight BuildSampler() => Compute;

        /// <inheritdoc/>
        public IEnumerable<ObjectUrl> GetInputFiles()
        {
            yield break;
        }

        /// <summary>
        /// Returns the height and normals at a given point
        /// </summary>
        /// <param name="normalizedPoint">The coordinate to sample at</param>
        /// <param name="height">The height of this sample at <see cref="normalizedPoint"/> coordinate</param>
        /// <param name="normal">The normal of the sample at <see cref="normalizedPoint"/> coordinate</param>
        /// <param name="fadeTarget">The erosion mask in [-1,+1] range</param>
        public void Compute(Vector2 normalizedPoint, out float height, out Vector2 normal, out float fadeTarget)
        {
            // Base height noise parameters.

            // The inverse horizontal scale of the terrain noise function.
            float HEIGHT_FREQUENCY = HeightFrequency;
            // The vertical scale (amplitude) of the terrain noise function.
            float HEIGHT_AMP = HeightAmp;
            // Control over the noise function octaves, with each successive
            // octave layering smaller bumps onto the terrain.
            int HEIGHT_OCTAVES = HeightOctaves;
            // The lacunarity controls the frequency (the inverse
            // horizontal scale) of each octave relative to the last.
            float HEIGHT_LACUNARITY = HeightLacunarity;
            // The gain controls the magnitude (the vertical scale)
            // of each octave relative to the last.
            float HEIGHT_GAIN = HeightGain;

            float heightFunctionScale = HeightFunctionScale;
            Vector2 pHeight = normalizedPoint / heightFunctionScale;

            // Calculate the FBM terrain height and derivatives and store them in n.
            // The heights are in the [-1, 1] range.
            var n = RunevisionTerrainErosion.FractalNoise(pHeight, HEIGHT_FREQUENCY, HEIGHT_OCTAVES, HEIGHT_LACUNARITY, HEIGHT_GAIN)
                    * HEIGHT_AMP * new Vector3(heightFunctionScale, 1.0f, 1.0f);

            // Define the erosion fade target based on the altitude of the pre-eroded terrain.
            // The fade target should strive to be -1 at valleys and 1 at peaks, but overshooting is ok.
            fadeTarget = MathUtil.Clamp(n.X / (HEIGHT_AMP * 0.6f), -1.0f, 1.0f);

            // Change terrain heights from [-1, 1] range to [0, 1] range.
            n = n * 0.5f + new Vector3(0.5f, 0, 0);
            height = n.X;
            normal = new Vector2(n.Y, n.Z);
        }
    }

    /// <summary>
    /// An input texture used as the base height for a <see cref="RunevisionTerrainAsset"/>
    /// </summary>
    [DataContract]
    public class HeightTexture : IHeightInput
    {
        public float DefaultHeight = 0.45f;
        public float Tiling = 1f;
        public float HeightMultiplier = 1f;
        public required UrlReference<Texture> Texture;

        /// <inheritdoc/>
        public IEnumerable<ObjectUrl> GetInputFiles()
        {
            if (Texture != null! && Texture.IsEmpty == false)
                yield return new ObjectUrl(UrlType.Content, Texture.Url);
        }

        /// <inheritdoc/>
        public RunevisionTerrainErosion.SampleHeight BuildSampler()
        {
            float[] heights;
            Int2 texResolution;
            if (Texture?.IsEmpty == false)
            {
                using (var context = new Stride.Assets.Skyboxes.SkyboxGeneratorContext(new(), MicrothreadLocalDatabases.ProviderService))
                {
                    var loadedTexture = context.Content.Load<Texture>(Texture.Url, ContentManagerLoaderSettings.StreamingDisabled);
                    var baseImage = loadedTexture.GetDataAsImage(context.RenderDrawContext.CommandList);

                    using (var textureTool = new TextureTool())
                    using (var texImage = textureTool.Load(baseImage, baseImage.Description.Format.IsSRgb))
                    {
                        textureTool.Convert(texImage, PixelFormat.R32_Float);

                        using (var image = textureTool.ConvertToStrideImage(texImage))
                        {
                            var pixelBuffer = image.GetPixelBuffer(0, 0, 0);

                            heights = pixelBuffer.GetPixels<float>();
                            foreach (ref var height in heights.AsSpan())
                                height *= HeightMultiplier;

                            var description = image.Description;
                            texResolution = new Int2(description.Width, description.Height);
                        }
                    }
                }
            }
            else
            {
                heights = new float[32 * 32];
                texResolution = new Int2(32, 32);
            }

            return Compute;

            float SampleTexture(Vector2 normalizedPoint)
            {
                var cellCoord = normalizedPoint * (Vector2)texResolution;
                return Vector2.BilinearSample(cellCoord, texResolution, heights);
            }

            void Compute(Vector2 normalizedPoint, out float height, out Vector2 normal, out float fadeTarget)
            {
                normalizedPoint *= Tiling;
                height = SampleTexture(normalizedPoint);

                var pixelNormalizedSize = 1.0f / (Vector2)texResolution;

                normalizedPoint.X -= pixelNormalizedSize.X; float dx0 = SampleTexture(normalizedPoint);
                normalizedPoint.X += pixelNormalizedSize.X * 2.0f; float dx1 = SampleTexture(normalizedPoint);

                normalizedPoint.X -= pixelNormalizedSize.X;

                normalizedPoint.Y -= pixelNormalizedSize.Y; float dy0 = SampleTexture(normalizedPoint);
                normalizedPoint.Y += pixelNormalizedSize.Y * 2.0f; float dy1 = SampleTexture(normalizedPoint);

                normalizedPoint.Y -= pixelNormalizedSize.Y;

                normal.X = (dx1 - dx0) / (2f * pixelNormalizedSize.X);
                normal.Y = (dy1 - dy0) / (2f * pixelNormalizedSize.Y);

                // Define the erosion fade target based on the altitude of the pre-eroded terrain.
                // The fade target should strive to be -1 at valleys and 1 at peaks, but overshooting is ok.
                fadeTarget = MathUtil.Clamp((height - DefaultHeight) / 0.15f, -1.0f, 1.0f);
            }
        }
    }

    /// <summary>
    /// How the height is mapped within the texture;
    /// <see cref="NormalizationMethod.Root"/> moves the heightmap to lay it down at the base.
    /// <see cref="NormalizationMethod.Normalize"/> moves the heightmap to lay it down at the base and squishes the height down into a [0-1] range.
    /// </summary>
    public enum NormalizationMethod
    {
        /// <summary> No changes </summary>
        None,
        /// <summary> Moves the heightmap to lay it down at the base </summary>
        Root,
        /// <summary>
        /// Moves the heightmap to lay it down at the base and squishes the height down into a [0-1] range.
        /// </summary>
        Normalize
    }

    /// <summary>
    /// What features the output texture should contain
    /// </summary>
    [Flags]
    public enum DataTypes : uint // Used through a cast to uint for PopCount
    {
        Height = 0b0000_0001,
        Erosion = 0b0000_0010,
        Ridges = 0b0000_0100,
        TreesWhenEnabled = 0b0000_1000,
        Debug = 0b0001_0000,
    }
}
