// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;

namespace Stride.Assets.Textures.Packing
{
    /// <summary>
    /// TexturePacker class for packing several textures, using MaxRects <see cref="MaxRectanglesBinPack"/>, into one or more texture atlases
    /// </summary>
    public class TexturePacker
    {
        /// <summary>
        /// Gets or sets MaxRects heuristic algorithm to place rectangles
        /// </summary>
        public TexturePackingMethod Algorithm;

        /// <summary>
        /// Gets or sets the use of rotation for packing
        /// </summary>
        public bool AllowRotation;

        /// <summary>
        /// Gets or sets the use of Multipack.
        /// If Multipack is enabled, a packer could create more than one texture atlases to fit all textures,
        /// whereas if Multipack is disabled, a packer always creates only one texture atlas which might not fit all textures.
        /// </summary>
        public bool AllowMultipack;

        /// <summary>
        /// Allow the atlas texture to have a size that is not power of 2.
        /// </summary>
        public bool AllowNonPowerOfTwo;

        /// <summary>
        /// Gets or sets MaxWidth for expected AtlasTextureLayout
        /// </summary>
        public int MaxWidth;

        /// <summary>
        /// Gets or sets MaxHeight for expected AtlasTextureLayout
        /// </summary>
        public int MaxHeight;

        /// <summary>
        /// Gets available Texture Atlases which contain a set of textures that are already packed
        /// </summary>
        public List<AtlasTextureLayout> AtlasTextureLayouts { get { return atlasTextureLayouts; } }

        private readonly MaxRectanglesBinPack maxRectPacker = new MaxRectanglesBinPack();
        private readonly List<AtlasTextureLayout> atlasTextureLayouts = new List<AtlasTextureLayout>();

        private Int2 atlasMaxSize;

        /// <summary>
        /// Resets the generated list of atlas layouts of the packer.
        /// </summary>
        public void Reset()
        {
            atlasTextureLayouts.Clear();
        }

        /// <summary>
        /// Packs the provided texture elementsToPack into <see cref="AtlasTextureLayouts"/>.
        /// </summary>
        /// <param name="textureElements">The texture elementsToPack to pack</param>
        /// <returns><value>True</value> if the texture could be packed, <value>False</value> otherwise</returns>
        public bool PackTextures(List<AtlasTextureElement> textureElements)
        {
            if (textureElements == null) 
                throw new ArgumentNullException("textureElements");

            // clones the into elementsToPack to avoid side effects on the input.
            var textureElementsClone = textureElements.Select(e => e.Clone()).ToList();

            // calculate the (maximum) size of the atlas 
            atlasMaxSize.X = !AllowNonPowerOfTwo ? MathUtil.PreviousPowerOfTwo(MaxWidth) : MaxWidth;
            atlasMaxSize.Y = !AllowNonPowerOfTwo ? MathUtil.PreviousPowerOfTwo(MaxHeight) : MaxHeight;

            if (Algorithm == TexturePackingMethod.Best)
            {
                var results = new Dictionary<TexturePackingMethod, List<AtlasTextureLayout>>();

                var bestAlgorithm = TexturePackingMethod.BestShortSideFit;
                var canPackAll = PackTextures(textureElementsClone, bestAlgorithm);

                results[bestAlgorithm] = new List<AtlasTextureLayout>(atlasTextureLayouts);

                foreach (var heuristicMethod in (TexturePackingMethod[])Enum.GetValues(typeof(TexturePackingMethod)))
                {
                    if (heuristicMethod == TexturePackingMethod.Best || heuristicMethod == TexturePackingMethod.BestShortSideFit)
                        continue;

                    Reset();

                    // This algorithm can't pack all textures, so discard it 
                    if (!PackTextures(textureElementsClone, heuristicMethod)) continue;

                    results[heuristicMethod] = new List<AtlasTextureLayout>(atlasTextureLayouts);

                    if (CompareTextureAtlasLists(results[heuristicMethod], results[bestAlgorithm]) > 0 || !canPackAll)
                    {
                        canPackAll = true;
                        bestAlgorithm = heuristicMethod;
                    }
                }

                Reset();

                if (canPackAll) atlasTextureLayouts.AddRange(results[bestAlgorithm]);

                return canPackAll;
            }

            Reset();

            return PackTextures(textureElements, Algorithm);
        }

        private static List<Size2> CreateSubSizeArray(int maxWidth, int maxHeight, int startWidth, int startHeight)
        {
            var result = new List<Size2>();

            var currentWidth = (maxWidth > startWidth) ? startWidth : maxWidth;
            var currentHeight = (maxHeight > startHeight) ? startHeight : maxHeight;

            result.Add(new Size2(currentWidth, currentHeight));

            var selector = 0;

            while (currentWidth < maxWidth || currentHeight < maxHeight)
            {
                if (currentWidth < maxWidth && currentHeight < maxHeight)
                {
                    if (selector % 2 == 0)
                        currentWidth = 2 * currentWidth;
                    else
                        currentHeight = 2 * currentHeight;

                    ++selector;
                }
                else if (currentWidth < maxWidth)
                {
                    currentWidth = 2 * currentWidth;
                }
                else
                {
                    currentHeight = 2 * currentHeight;
                }

                result.Add(new Size2(currentWidth, currentHeight));
            }

            return result;
        }

        /// <summary>
        /// Compares two atlas List to check which list is more optimal in term of the number of atlas and areas
        /// </summary>
        /// <param name="atlasList1">Source 1</param>
        /// <param name="atlasList2">Source 2</param>
        /// <returns>Return -1 if atlasList1 is less optimal, 0 if the two list is the same level of optimal, 1 if atlasList1 is more optimal </returns>
        private int CompareTextureAtlasLists(List<AtlasTextureLayout> atlasList1, List<AtlasTextureLayout> atlasList2)
        {
            // Check the number of pages
            if (atlasList1.Count != atlasList2.Count)
                return (atlasList1.Count > atlasList2.Count) ? -1 : 1;

            // Check area
            var area1 = atlasList1.SelectMany(atlas => atlas.Textures).Sum(texture => texture.DestinationRegion.Width * texture.DestinationRegion.Height);
            var area2 = atlasList2.SelectMany(atlas => atlas.Textures).Sum(texture => texture.DestinationRegion.Width * texture.DestinationRegion.Height);

            if (area1 == area2)
                return 0;

            return (area1 > area2) ? -1 : 1;
        }

        /// <summary>
        /// Packs the provided texture elementsToPack into <see cref="AtlasTextureLayouts"/>, given the provided heuristic algorithm.
        /// </summary>
        /// <param name="textureElements">The texture elementsToPack to pack</param>
        /// <param name="algorithm">Packing algorithm to use</param>
        /// <returns>True indicates all textures could be packed; False otherwise</returns>
        private bool PackTextures(List<AtlasTextureElement> textureElements, TexturePackingMethod algorithm)
        {
            var elementsToPack = textureElements;
            if (elementsToPack.Count == 0) // always successful if there is no element to pack (note we do not create a layout)
                return true;

            do
            {
                // Do not create the atlas if all the elements are "empty". We don't want empty atlas.
                if (textureElements.All(e => e.SourceRegion.IsEmpty()))
                    return true;

                List<AtlasTextureElement> remainingElements;
                var atlasLayout = CreateBestAtlasLayout(elementsToPack, algorithm, out remainingElements);

                // Check if at least one element could be packed in the texture.
                if (elementsToPack.Count == remainingElements.Count)
                    return false;

                elementsToPack = remainingElements;
                atlasTextureLayouts.Add(atlasLayout);
            }
            while (AllowMultipack && elementsToPack.Count > 0);

            return elementsToPack.Count == 0;
        }

        /// <summary>
        /// Create the best atlas layout possible given the elementsToPack to pack, the algorithm and the atlas maximum size.
        /// Note: when all the elementsToPack cannot fit into the texture, it tries to pack as much as possible of them.
        /// </summary>
        /// <returns>False if</returns>
        private AtlasTextureLayout CreateBestAtlasLayout(List<AtlasTextureElement> elementsToPack, TexturePackingMethod algorithm, out List<AtlasTextureElement> remainingElements)
        {
            remainingElements = elementsToPack;

            var textureAtlas = new AtlasTextureLayout();

            var bestElementPackedCount = int.MaxValue;

            // Generate sub size array
            var subSizeArray = CreateSubSizeArray(atlasMaxSize.X, atlasMaxSize.Y, 512, 512);

            foreach (var subArray in subSizeArray)
            {
                var currentRemaingElements = new List<AtlasTextureElement>(elementsToPack);

                // Reset packer state
                maxRectPacker.Initialize(subArray.Width, subArray.Height, AllowRotation);

                // Pack
                maxRectPacker.PackRectangles(currentRemaingElements, algorithm);

                // Find true size from packed regions
                var packedSize = CalculatePackedRectanglesBound(maxRectPacker.PackedElements);

                // Alter the size of atlas so that it is a power of two
                if (!AllowNonPowerOfTwo)
                {
                    packedSize.Width = MathUtil.NextPowerOfTwo(packedSize.Width);
                    packedSize.Height = MathUtil.NextPowerOfTwo(packedSize.Height);

                    if (packedSize.Width > subArray.Width || packedSize.Height > subArray.Height)
                        continue;
                }

                if (currentRemaingElements.Count >= bestElementPackedCount)
                    continue;

                // Found new best pack, cache it
                bestElementPackedCount = currentRemaingElements.Count;

                // Resize texture atlas
                textureAtlas.Width = packedSize.Width;
                textureAtlas.Height = packedSize.Height;

                textureAtlas.Textures.Clear();

                // Store all packed regions into Atlas
                foreach (var element in maxRectPacker.PackedElements)
                    textureAtlas.Textures.Add(element.Clone());

                remainingElements = currentRemaingElements;
            }

            return textureAtlas;
        }

        /// <summary>
        /// Calculates bound for the packed textures
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        private Size2 CalculatePackedRectanglesBound(IReadOnlyCollection<AtlasTextureElement> elements)
        {
            if (elements.Count == 0) return Size2.Zero;

            var minX = int.MaxValue;
            var minY = int.MaxValue;

            var maxX = int.MinValue;
            var maxY = int.MinValue;

            foreach (var element in elements)
            {
                var usedRectangle = element.DestinationRegion;
                if (minX > usedRectangle.X) minX = usedRectangle.X;
                if (minY > usedRectangle.Y) minY = usedRectangle.Y;

                if (maxX < usedRectangle.X + usedRectangle.Width) maxX = usedRectangle.X + usedRectangle.Width;
                if (maxY < usedRectangle.Y + usedRectangle.Height) maxY = usedRectangle.Y + usedRectangle.Height;
            }

            return new Size2(maxX - minX, maxY - minY);
        }
    }
}
