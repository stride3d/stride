// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;
using Stride.Core.Assets.Editor.View.Behaviors;

namespace Stride.Core.Assets.Editor.Tests
{
    public class TestDropZoneCalculator
    {
        private const double RowHeight = 24.0;

        [Theory]
        [InlineData(0.0)]
        [InlineData(3.0)]
        [InlineData(7.0)]
        public void TestTopBandInsertsBefore(double positionY)
        {
            Assert.Equal(InsertPosition.Before, DropZoneCalculator.GetInsertPosition(positionY, RowHeight, true));
        }

        [Theory]
        [InlineData(20.0)]
        [InlineData(24.0)]
        public void TestBottomBandInsertsAfter(double positionY)
        {
            Assert.Equal(InsertPosition.After, DropZoneCalculator.GetInsertPosition(positionY, RowHeight, true));
        }

        [Theory]
        [InlineData(9.0)]
        [InlineData(12.0)]
        [InlineData(15.0)]
        public void TestMiddleIsDropZone(double positionY)
        {
            Assert.Null(DropZoneCalculator.GetInsertPosition(positionY, RowHeight, true));
        }

        [Fact]
        public void TestBottomBandIsDropZoneWhenAfterIsNotAllowed()
        {
            for (var positionY = 0.0; positionY <= RowHeight; positionY += 0.5)
            {
                Assert.NotEqual(InsertPosition.After, DropZoneCalculator.GetInsertPosition(positionY, RowHeight, false));
            }
        }

        [Fact]
        public void TestBandsAreHalfOpen()
        {
            var band = DropZoneCalculator.GetInsertBandHeight(RowHeight);
            Assert.Null(DropZoneCalculator.GetInsertPosition(band, RowHeight, true));
            Assert.Null(DropZoneCalculator.GetInsertPosition(RowHeight - band, RowHeight, true));
        }

        [Fact]
        public void TestBandIsProportionalToRowHeight()
        {
            Assert.Equal(RowHeight * DropZoneCalculator.InsertBandRatio, DropZoneCalculator.GetInsertBandHeight(RowHeight));
        }

        [Fact]
        public void TestBandIsClampedOnTallRow()
        {
            Assert.Equal(DropZoneCalculator.MaxInsertBandHeight, DropZoneCalculator.GetInsertBandHeight(100.0));
        }

        [Fact]
        public void TestShortRowKeepsADropZone()
        {
            const double shortRow = 6.0;
            var band = DropZoneCalculator.GetInsertBandHeight(shortRow);
            Assert.True(band * 2 < shortRow);
            Assert.Null(DropZoneCalculator.GetInsertPosition(shortRow / 2, shortRow, true));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-1.0)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        public void TestInvalidRowHeightHasNoBand(double rowHeight)
        {
            Assert.Equal(0.0, DropZoneCalculator.GetInsertBandHeight(rowHeight));
            Assert.Null(DropZoneCalculator.GetInsertPosition(0.0, rowHeight, true));
        }

        [Theory]
        [InlineData(-1.0)]
        [InlineData(RowHeight + 1.0)]
        [InlineData(double.NaN)]
        public void TestPositionOutsideRowIsNotAnInsert(double positionY)
        {
            Assert.Null(DropZoneCalculator.GetInsertPosition(positionY, RowHeight, true));
        }
    }
}
