// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;

namespace Stride.Assets.Textures.Packing
{
    /// <summary>
    /// Implementation of texture packer using MaxRects algorithm.
    /// Reference: http://clb.demon.fi/files/RectangleBinPack.pdf by Jukka Jylanki.
    /// </summary>
    public class MaxRectanglesBinPack
    {
        private bool useRotation;
        private int binWidth;
        private int binHeight;

        private readonly List<AtlasTextureElement> packedElements = new List<AtlasTextureElement>();
        private readonly List<Rectangle> freeRectangles = new List<Rectangle>();

        /// <summary>
        /// Gets those elementsToPack that are already packed
        /// </summary>
        public List<AtlasTextureElement> PackedElements { get { return packedElements; } }

        public MaxRectanglesBinPack()
        {
        }

        /// <summary>
        /// Initializes a new instance of MaxRectanglesBinPack
        /// </summary>
        /// <param name="width">Expected width of a bin</param>
        /// <param name="height">Expected height of a bin</param>
        /// <param name="useRotation">Indicate whether rectangle are allowed to be rotated</param>
        public MaxRectanglesBinPack(int width, int height, bool useRotation)
        {
            Initialize(width, height, useRotation);
        }

        /// <summary>
        /// Initializes a bin given new sets of parameters, and clear states of MaxRectanglesBinPack
        /// </summary>
        /// <param name="width">Expected width of a bin</param>
        /// <param name="height">Expected height of a bin</param>
        /// <param name="allowRotation">Indicate whether rectangle are allowed to be rotated</param>
        public void Initialize(int width, int height, bool allowRotation)
        {
            binWidth = width;
            binHeight = height;

            useRotation = allowRotation;

            packedElements.Clear();
            freeRectangles.Clear();

            freeRectangles.Add(new Rectangle(0, 0, binWidth, binHeight));
        }

        /// <summary>
        /// Packs input elements using the MaxRects algorithm.
        /// Note that any element that could be packed is removed from the elementsToPack collection.
        /// </summary>
        /// <param name="elementsToPack">a list of rectangles to be packed</param>
        /// <param name="method">MaxRects heuristic method which default value is BestShortSideFit</param>
        public void PackRectangles(List<AtlasTextureElement> elementsToPack, TexturePackingMethod method = TexturePackingMethod.BestShortSideFit)
        {
            var bestRectangle = new RotableRectangle();

            // Prune all the empty elements (elements with null region) from the list of elements to pack 
            // Reason: reduce the size of the atlas and wrap/mirror/clamp border mode are undetermined for empty elements.
            for (int i = elementsToPack.Count-1; i >= 0; --i)
            {
                if (elementsToPack[i].SourceRegion.IsEmpty())
                    elementsToPack.RemoveAt(i);
            }

            // Pack the elements.
            while (elementsToPack.Count > 0)
            {
                var bestScore1 = int.MaxValue;
                var bestScore2 = int.MaxValue;

                var bestRectangleIndex = -1;

                for (var i = 0; i < elementsToPack.Count; ++i)
                {
                    int score1;
                    int score2;
                    var element = elementsToPack[i];
                    var width = element.SourceRegion.Width + 2*element.BorderSize;
                    var height = element.SourceRegion.Height+ 2*element.BorderSize;
                    var pickedRectangle = ChooseTargetPosition(width, height, method, out score1, out score2);

                    if (score1 < bestScore1 || (score1 == bestScore1 && score2 < bestScore2))
                    // Found the new best free region to hold a rectangle
                    {
                        bestScore1 = score1;
                        bestScore2 = score2;
                        bestRectangleIndex = i;
                        bestRectangle = pickedRectangle;
                    }
                }

                // Could not find any free region to hold a rectangle, terminate packing process
                if (bestRectangleIndex == -1) break;

                // Update the free space of the packer
                TakeSpaceForRectangle(bestRectangle);

                // Update the packed element
                var packedElement = elementsToPack[bestRectangleIndex];
                packedElement.DestinationRegion = bestRectangle;

                // Update the packed and remaining element lists
                packedElements.Add(packedElement);
                elementsToPack.RemoveAt(bestRectangleIndex);
            }
        }

        /// <summary>
        /// Places a given rectangle in the free space.
        /// </summary>
        /// <param name="rectangleToPlace">The rectangle to place</param>
        private void TakeSpaceForRectangle(RotableRectangle rectangleToPlace)
        {
            var numberRectanglesToProcess = freeRectangles.Count;
            for (var i = 0; i < numberRectanglesToProcess; ++i)
            {
                if (SplitFreeNode(freeRectangles[i], rectangleToPlace))
                {
                    freeRectangles.RemoveAt(i);
                    --i;
                    --numberRectanglesToProcess;
                }
            }

            PruneFreeList();
        }

        /// <summary>
        /// Removes those free elementsToPack that are sub-regions of other elementsToPack
        /// </summary>
        private void PruneFreeList()
        {
            // Go through each pair and remove any rectangle that is redundant.
            for (var i = 0; i < freeRectangles.Count; ++i)
                for (var j = i + 1; j < freeRectangles.Count; ++j)
                {
                    if (freeRectangles[j].Contains(freeRectangles[i]))
                    {
                        freeRectangles.RemoveAt(i);
                        --i;
                        break;
                    }
                    if (freeRectangles[i].Contains(freeRectangles[j]))
                    {
                        freeRectangles.RemoveAt(j);
                        --j;
                    }
                }
        }

        /// <summary>
        /// Splits a free region by a usedNode rectangle
        /// </summary>
        /// <param name="freeNode">Free rectangle to be splitted</param>
        /// <param name="usedNode">UsedNode rectangle</param>
        /// <returns></returns>
        private bool SplitFreeNode(Rectangle freeNode, RotableRectangle usedNode)
        {
            // Test with SAT if the elementsToPack even intersect.
            if (usedNode.X >= freeNode.X + freeNode.Width || usedNode.X + usedNode.Width <= freeNode.X ||
                usedNode.Y >= freeNode.Y + freeNode.Height || usedNode.Y + usedNode.Height <= freeNode.Y)
                return false;

            if (usedNode.X < freeNode.X + freeNode.Width && usedNode.X + usedNode.Width > freeNode.X)
            {
                // New node at the top side of the used node.
                if (usedNode.Y > freeNode.Y && usedNode.Y < freeNode.Y + freeNode.Height)
                {
                    var newNode = freeNode;
                    newNode.Height = usedNode.Y - newNode.Y;
                    freeRectangles.Add(newNode);
                }

                // New node at the bottom side of the used node.
                if (usedNode.Y + usedNode.Height < freeNode.Y + freeNode.Height)
                {
                    var newNode = freeNode;
                    newNode.Y = usedNode.Y + usedNode.Height;
                    newNode.Height = freeNode.Y + freeNode.Height - (usedNode.Y + usedNode.Height);
                    freeRectangles.Add(newNode);
                }
            }

            if (usedNode.Y < freeNode.Y + freeNode.Height && usedNode.Y + usedNode.Height > freeNode.Y)
            {
                // New node at the left side of the used node.
                if (usedNode.X > freeNode.X && usedNode.X < freeNode.X + freeNode.Width)
                {
                    var newNode = freeNode;
                    newNode.Width = usedNode.X - newNode.X;
                    freeRectangles.Add(newNode);
                }

                // New node at the right side of the used node.
                if (usedNode.X + usedNode.Width < freeNode.X + freeNode.Width)
                {
                    var newNode = freeNode;
                    newNode.X = usedNode.X + usedNode.Width;
                    newNode.Width = freeNode.X + freeNode.Width - (usedNode.X + usedNode.Width);
                    freeRectangles.Add(newNode);
                }
            }

            return true;
        }

        /// <summary>
        /// Determines a target position to place a given rectangle by a given heuristic method
        /// </summary>
        /// <param name="method">A heuristic placement method</param>
        /// <param name="score1">First score</param>
        /// <param name="score2">Second score</param>
        /// <param name="width">The width of the element to place</param>
        /// <param name="height">The height of the element to place</param>
        /// <returns></returns>
        private RotableRectangle ChooseTargetPosition(int width, int height, TexturePackingMethod method, out int score1, out int score2)
        {
            var bestNode = new RotableRectangle();

            // null sized rectangle fits everywhere with a perfect score.
            if (width == 0 || height == 0)
            {
                score1 = 0;
                score2 = 0;
                return bestNode;
            }

            score1 = int.MaxValue;
            score2 = int.MaxValue;

            switch (method)
            {
                case TexturePackingMethod.BestShortSideFit:
                    bestNode = FindPositionForNewNodeBestShortSideFit(width, height, out score1, ref score2);
                    break;
                case TexturePackingMethod.BottomLeftRule:
                    bestNode = FindPositionForNewNodeBottomLeft(width, height, out score1, ref score2);
                    break;
                case TexturePackingMethod.ContactPointRule:
                    bestNode = FindPositionForNewNodeContactPoint(width, height, out score1);
                    score1 *= -1;
                    break;
                case TexturePackingMethod.BestLongSideFit:
                    bestNode = FindPositionForNewNodeBestLongSideFit(width, height, ref score2, out score1);
                    break;
                case TexturePackingMethod.BestAreaFit:
                    bestNode = FindPositionForNewNodeBestAreaFit(width, height, out score1, ref score2);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("method");
            }

            // there is no available space big enough to fit the rectangle
            if (bestNode.Height == 0)
            {
                score1 = int.MaxValue;
                score2 = int.MaxValue;
            }

            return bestNode;
        }

        /// <summary>
        /// Finds a placement position using NewNodeBestShortSideFit heuristic method
        /// </summary>
        /// <param name="height"></param>
        /// <param name="bestShortSideFit"></param>
        /// <param name="bestLongSideFit"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        private RotableRectangle FindPositionForNewNodeBestShortSideFit(int width, int height, out int bestShortSideFit, ref int bestLongSideFit)
        {
            var bestNode = new RotableRectangle();

            bestShortSideFit = int.MaxValue;

            for (var i = 0; i < freeRectangles.Count; ++i)
            {
                // non-flip
                if (freeRectangles[i].Width >= width && freeRectangles[i].Height >= height)
                {
                    var leftoverHoriz = Math.Abs(freeRectangles[i].Width - width);
                    var leftoverVert = Math.Abs(freeRectangles[i].Height - height);
                    var shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
                    var longSideFit = Math.Max(leftoverHoriz, leftoverVert);

                    if (shortSideFit < bestShortSideFit || (shortSideFit == bestShortSideFit && longSideFit < bestLongSideFit))
                    {
                        bestNode.X = freeRectangles[i].X;
                        bestNode.Y = freeRectangles[i].Y;

                        bestNode.Width = width;
                        bestNode.Height = height;

                        bestShortSideFit = shortSideFit;
                        bestLongSideFit = longSideFit;

                        bestNode.IsRotated = false;
                    }
                }

                if (!useRotation) continue;

                if (freeRectangles[i].Width >= height && freeRectangles[i].Height >= width)
                {
                    var flippedLeftoverHoriz = Math.Abs(freeRectangles[i].Width - height);
                    var flippedLeftoverVert = Math.Abs(freeRectangles[i].Height - width);
                    var flippedShortSideFit = Math.Min(flippedLeftoverHoriz, flippedLeftoverVert);
                    var flippedLongSideFit = Math.Max(flippedLeftoverHoriz, flippedLeftoverVert);

                    if (flippedShortSideFit < bestShortSideFit || (flippedShortSideFit == bestShortSideFit && flippedLongSideFit < bestLongSideFit))
                    {
                        bestNode.X = freeRectangles[i].X;
                        bestNode.Y = freeRectangles[i].Y;

                        bestNode.Width = height;
                        bestNode.Height = width;

                        bestShortSideFit = flippedShortSideFit;
                        bestLongSideFit = flippedLongSideFit;
                        bestNode.IsRotated = true;
                    }
                }
            }
            return bestNode;
        }

        /// <summary>
        /// The heuristic rule used by this algorithm is to Orient and place each-
        /// rectangle to the position where the y-coordinate of the top side of the rectangle
        /// is the smallest and if there are several such valid positions, pick the
        /// one that has the smallest x-coordinate value
        /// </summary>
        /// <param name="height"></param>
        /// <param name="bestY"></param>
        /// <param name="bestX"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        private RotableRectangle FindPositionForNewNodeBottomLeft(int width, int height, out int bestY, ref int bestX)
        {
            var bestNode = new RotableRectangle();

            bestY = int.MaxValue;

            for (var i = 0; i < freeRectangles.Count; ++i)
            {
                // Try to place the rectangle in upright (non-flipped) orientation.
                if (freeRectangles[i].Width >= width && freeRectangles[i].Height >= height)
                {
                    var topSideY = freeRectangles[i].Y + height;
                    if (topSideY < bestY || (topSideY == bestY && freeRectangles[i].X < bestX))
                    {
                        bestNode.X = freeRectangles[i].X;
                        bestNode.Y = freeRectangles[i].Y;
                        bestNode.Width = width;
                        bestNode.Height = height;
                        bestY = topSideY;
                        bestX = freeRectangles[i].X;
                        bestNode.IsRotated = false;
                    }
                }

                if (!useRotation) continue;

                if (freeRectangles[i].Width >= height && freeRectangles[i].Height >= width)
                {
                    var topSideY = freeRectangles[i].Y + width;
                    if (topSideY < bestY || (topSideY == bestY && freeRectangles[i].X < bestX))
                    {
                        bestNode.X = freeRectangles[i].X;
                        bestNode.Y = freeRectangles[i].Y;
                        bestNode.Width = height;
                        bestNode.Height = width;
                        bestY = topSideY;
                        bestX = freeRectangles[i].X;
                        bestNode.IsRotated = true;
                    }
                }
            }

            return bestNode;
        }

        /// <summary>
        /// Finds a placement position using NodeContactPoint heuristic method
        /// </summary>
        /// <param name="height"></param>
        /// <param name="bestContactScore"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        private RotableRectangle FindPositionForNewNodeContactPoint(int width, int height, out int bestContactScore)
        {
            var bestNode = new RotableRectangle();

            bestContactScore = -1;

            for (var i = 0; i < freeRectangles.Count; ++i)
            {
                // Try to place the rectangle in upright (non-flipped) orientation.
                if (freeRectangles[i].Width >= width && freeRectangles[i].Height >= height)
                {
                    int score = ContactPointScoreNode(freeRectangles[i].X, freeRectangles[i].Y, width, height);
                    if (score > bestContactScore)
                    {
                        bestNode.X = freeRectangles[i].X;
                        bestNode.Y = freeRectangles[i].Y;
                        bestNode.Width = width;
                        bestNode.Height = height;
                        bestContactScore = score;
                        bestNode.IsRotated = false;
                    }
                }

                if (!useRotation) continue;

                // Flip
                if (freeRectangles[i].Width >= height && freeRectangles[i].Height >= width)
                {
                    int score = ContactPointScoreNode(freeRectangles[i].X, freeRectangles[i].Y, width, height);
                    if (score > bestContactScore)
                    {
                        bestNode.X = freeRectangles[i].X;
                        bestNode.Y = freeRectangles[i].Y;
                        bestNode.Width = height;
                        bestNode.Height = width;
                        bestContactScore = score;
                        bestNode.IsRotated = true;
                    }
                }
            }

            return bestNode;
        }

        /// <summary>
        /// Calculates ContactPoint score
        /// </summary>
        private int ContactPointScoreNode(int x, int y, int width, int height)
        {
            var score = 0;

            if (x == 0 || x + width == binWidth)
                score += height;
            if (y == 0 || y + height == binHeight)
                score += width;

            foreach (AtlasTextureElement element in packedElements)
            {
                var rectangle = element.DestinationRegion;
                if (rectangle.X == x + width || rectangle.X + rectangle.Width == x)
                    score += CommonIntervalLength(rectangle.Y, rectangle.Y + rectangle.Height, y, y + height);
                if (rectangle.Y == y + height || rectangle.Y + rectangle.Height == y)
                    score += CommonIntervalLength(rectangle.X, rectangle.X + rectangle.Width, x, x + width);
            }

            return score;
        }

        private int CommonIntervalLength(int i1Start, int i1End, int i2Start, int i2End)
        {
            if (i1End < i2Start || i2End < i1Start)
                return 0;
            return Math.Min(i1End, i2End) - Math.Max(i1Start, i2Start);
        }

        /// <summary>
        /// Finds a placement position using BestLongSideFit heuristic method
        /// </summary>
        /// <param name="height"></param>
        /// <param name="bestShortSideFit"></param>
        /// <param name="bestLongSideFit"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        private RotableRectangle FindPositionForNewNodeBestLongSideFit(int width, int height, ref int bestShortSideFit, out int bestLongSideFit)
        {
            var bestNode = new RotableRectangle();

            bestLongSideFit = int.MaxValue;

            for (var i = 0; i < freeRectangles.Count; ++i)
            {
                // Try to place the rectangle in upright (non-flipped) orientation.
                if (freeRectangles[i].Width >= width && freeRectangles[i].Height >= height)
                {
                    var leftoverHoriz = Math.Abs(freeRectangles[i].Width - width);
                    var leftoverVert = Math.Abs(freeRectangles[i].Height - height);
                    var shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
                    var longSideFit = Math.Max(leftoverHoriz, leftoverVert);

                    if (longSideFit < bestLongSideFit || (longSideFit == bestLongSideFit && shortSideFit < bestShortSideFit))
                    {
                        bestNode.X = freeRectangles[i].X;
                        bestNode.Y = freeRectangles[i].Y;
                        bestNode.Width = width;
                        bestNode.Height = height;
                        bestShortSideFit = shortSideFit;
                        bestLongSideFit = longSideFit;
                        bestNode.IsRotated = false;
                    }
                }

                if (!useRotation) continue;

                // Flip
                if (freeRectangles[i].Width >= height && freeRectangles[i].Height >= width)
                {
                    var leftoverHoriz = Math.Abs(freeRectangles[i].Width - height);
                    var leftoverVert = Math.Abs(freeRectangles[i].Height - width);
                    var shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
                    var longSideFit = Math.Max(leftoverHoriz, leftoverVert);

                    if (longSideFit < bestLongSideFit || (longSideFit == bestLongSideFit && shortSideFit < bestShortSideFit))
                    {
                        bestNode.X = freeRectangles[i].X;
                        bestNode.Y = freeRectangles[i].Y;
                        bestNode.Width = height;
                        bestNode.Height = width;
                        bestShortSideFit = shortSideFit;
                        bestLongSideFit = longSideFit;
                        bestNode.IsRotated = true;
                    }
                }
            }

            return bestNode;
        }

        /// <summary>
        /// Finds a placement position using BestAreaFit heuristic method
        /// </summary>
        /// <param name="height"></param>
        /// <param name="bestAreaFit"></param>
        /// <param name="bestShortSideFit"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        private RotableRectangle FindPositionForNewNodeBestAreaFit(int width, int height, out int bestAreaFit, ref int bestShortSideFit)
        {
            var bestNode = new RotableRectangle();

            bestAreaFit = int.MaxValue;

            for (var i = 0; i < freeRectangles.Count; ++i)
            {
                var areaFit = freeRectangles[i].Width * freeRectangles[i].Height - width * height;

                // Try to place the rectangle in upright (non-flipped) orientation.
                if (freeRectangles[i].Width >= width && freeRectangles[i].Height >= height)
                {
                    var leftoverHoriz = Math.Abs(freeRectangles[i].Width - width);
                    var leftoverVert = Math.Abs(freeRectangles[i].Height - height);
                    var shortSideFit = Math.Min(leftoverHoriz, leftoverVert);

                    if (areaFit < bestAreaFit || (areaFit == bestAreaFit && shortSideFit < bestShortSideFit))
                    {
                        bestNode.X = freeRectangles[i].X;
                        bestNode.Y = freeRectangles[i].Y;
                        bestNode.Width = width;
                        bestNode.Height = height;
                        bestNode.IsRotated = false;
                        bestShortSideFit = shortSideFit;
                        bestAreaFit = areaFit;
                    }
                }

                if (!useRotation) continue;

                // Flip
                if (freeRectangles[i].Width >= height && freeRectangles[i].Height >= width)
                {
                    var leftoverHoriz = Math.Abs(freeRectangles[i].Width - height);
                    var leftoverVert = Math.Abs(freeRectangles[i].Height - width);
                    var shortSideFit = Math.Min(leftoverHoriz, leftoverVert);

                    if (areaFit < bestAreaFit || (areaFit == bestAreaFit && shortSideFit < bestShortSideFit))
                    {
                        bestNode.X = freeRectangles[i].X;
                        bestNode.Y = freeRectangles[i].Y;
                        bestNode.Width = height;
                        bestNode.Height = width;
                        bestNode.IsRotated = true;
                        bestShortSideFit = shortSideFit;
                        bestAreaFit = areaFit;
                    }
                }
            }

            return bestNode;
        }
    }
}
