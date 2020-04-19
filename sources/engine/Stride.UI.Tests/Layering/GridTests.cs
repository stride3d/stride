// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

using Xunit;

using Stride.Core.Mathematics;
using Stride.UI.Panels;

namespace Stride.UI.Tests.Layering
{
    /// <summary>
    /// Series of tests for <see cref="Grid"/>
    /// </summary>
    public class GridTests
    {
        private Random rand = new Random(DateTime.Now.Millisecond);

        /// <summary>
        /// launch all the tests of <see cref="GridTests"/>
        /// </summary>
        internal void TestAll()
        {
            TestGridDefaultState();
            TestDefinitionNoCompletion();
            TestFixedOnlyBasicLayering();
            TestFixedOnlyComplexLayering();
            TestAutoOnlyBasicLayering();
            TestAutoOnlyMultiSpanLayering();
            TestAutoOnlyMinMaxLayering();
            TestStarOnlyBasicLayering();
            TestStarOnlyMultiSpanLayering();
            TestStarOnlyMin1EltLayering();
            TestStarOnlyMax1EltLayering();
            TestStarOnlyMin2EltsLayering1();
            TestStarOnlyMin2EltsLayering2();
            TestStarOnlyMin2EltsLayering3();
            TestStarOnlyMax2EltsLayering1();
            TestStarOnlyMax2EltsLayering2();
            TestStarOnlyMax2EltsLayering3();
            TestStarOnlyMultiMinLayering1();
            TestStarOnlyMultiMinLayering2();
            TestStarOnlyMultiMaxLayering1();
            TestStarOnlyMultiMaxLayering2();
            TestStarOnlyMinMaxLayering1();
            TestStarOnlyMinMaxLayering2();
            TestBasicMultiTypeLayering();
            TestMeasureProvidedSizeMix();
            TestMeasureProvidedSizeAuto0();
            TestMeasureProvidedSizeAuto1();
            TestMeasureProvidedSizeFixed();
            TestMeasureProvidedSizeStar0();
            TestMeasureProvidedSizeStar1();
        }

        /// <summary>
        /// Test the default state of the grid.
        /// </summary>
        [Fact]
        public void TestGridDefaultState()
        {
            var grid = new Grid();

            TestDefinitionsDefaultState(grid.ColumnDefinitions);
            TestDefinitionsDefaultState(grid.RowDefinitions);
            TestDefinitionsDefaultState(grid.LayerDefinitions);
        }

        private void TestDefinitionsDefaultState(StripDefinitionCollection definitions)
        {
            Assert.Empty(definitions);
        }

        /// <summary>
        /// Test that no strip definition are added when a child is partially outside of the grid definition.
        /// </summary>
        [Fact]
        public void TestDefinitionNoCompletion()
        {
            var grid = new Grid();

            var c1 = new Canvas();
            c1.DependencyProperties.Set(GridBase.ColumnPropertyKey, 2);
            c1.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 3);
            c1.DependencyProperties.Set(GridBase.RowPropertyKey, 5);
            c1.DependencyProperties.Set(GridBase.RowSpanPropertyKey, 2);

            var c2 = new Canvas();
            c2.DependencyProperties.Set(GridBase.ColumnPropertyKey, 3);
            c2.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 3);
            c2.DependencyProperties.Set(GridBase.LayerPropertyKey, 1);
            c2.DependencyProperties.Set(GridBase.LayerSpanPropertyKey, 4);

            grid.Children.Add(c1);
            grid.Children.Add(c2);

            grid.Measure(Vector3.Zero);
            grid.Arrange(Vector3.Zero, false);

            Assert.Empty(grid.ColumnDefinitions);
            Assert.Empty(grid.RowDefinitions);
            Assert.Empty(grid.LayerDefinitions);
        }

        /// <summary>
        /// Tests that a grid without row/column/layer definitions behave like a grid with one of each.
        /// </summary>
        [Fact]
        public void TestDefaultGridLayering()
        {
            var grid = new Grid();

            var child0 = new MeasureArrangeValidator { ExpectedMeasureValue = new Vector3(200, 200, 0), ExpectedArrangeValue = new Vector3(200, 200, 0), ReturnedMeasuredValue = new Vector3(100, 400, 0), DepthAlignment = DepthAlignment.Stretch };
            //var child1 = new MeasureArrangeValidator { ExpectedMeasureValue = new Vector3(200, 200, 0), ExpectedArrangeValue = new Vector3(200, 200, 0), ReturnedMeasuredValue = new Vector3(100, 400, 0), Width = 100, Height = 400, DepthAlignment = DepthAlignment.Stretch };

            grid.Children.Add(child0);
            //grid.Children.Add(child1);

            grid.Measure(new Vector3(200, 200, 0));
            grid.Arrange(new Vector3(200, 200, 0), false);

            // Try again with strips (it should behave the same)
            grid.ColumnDefinitions.Add(new StripDefinition());
            grid.RowDefinitions.Add(new StripDefinition());
            grid.LayerDefinitions.Add(new StripDefinition());

            grid.Measure(new Vector3(200, 200, 0));
            grid.Arrange(new Vector3(200, 200, 0), false);
        }

        /// <summary>
        /// Test that fix-size strip layering (both measuring and arranging) works properly.
        /// </summary>
        [Fact]
        public void TestFixedOnlyComplexLayering()
        {
            // create a 3x3 grid with elements of every every different span and too small/big size
            var grid = new Grid();

            // the grid definition
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Fixed, 100));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Fixed, 200));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Fixed, 300));
            grid.RowDefinitions.Add(new StripDefinition(StripType.Fixed, 400));
            grid.RowDefinitions.Add(new StripDefinition(StripType.Fixed, 500));
            grid.RowDefinitions.Add(new StripDefinition(StripType.Fixed, 600));
            grid.LayerDefinitions.Add(new StripDefinition(StripType.Fixed, 700));
            grid.LayerDefinitions.Add(new StripDefinition(StripType.Fixed, 800));
            grid.LayerDefinitions.Add(new StripDefinition(StripType.Fixed, 900));

            // the simple cells children
            var child000 = new MeasureArrangeValidator { ExpectedMeasureValue = new Vector3(100, 400, 700), ExpectedArrangeValue = new Vector3(100, 400, 700), ReturnedMeasuredValue = 1000 * rand.NextVector3(), DepthAlignment = DepthAlignment.Stretch };
            var child100 = new MeasureArrangeValidator { ExpectedMeasureValue = new Vector3(200, 400, 700), ExpectedArrangeValue = new Vector3(200, 400, 700), ReturnedMeasuredValue = 1000 * rand.NextVector3(), DepthAlignment = DepthAlignment.Stretch };
            var child200 = new MeasureArrangeValidator { ExpectedMeasureValue = new Vector3(300, 400, 700), ExpectedArrangeValue = new Vector3(300, 400, 700), ReturnedMeasuredValue = 1000 * rand.NextVector3(), DepthAlignment = DepthAlignment.Stretch };
            var child010 = new MeasureArrangeValidator { ExpectedMeasureValue = new Vector3(100, 500, 700), ExpectedArrangeValue = new Vector3(100, 500, 700), ReturnedMeasuredValue = 1000 * rand.NextVector3(), DepthAlignment = DepthAlignment.Stretch };
            var child020 = new MeasureArrangeValidator { ExpectedMeasureValue = new Vector3(100, 600, 700), ExpectedArrangeValue = new Vector3(100, 600, 700), ReturnedMeasuredValue = 1000 * rand.NextVector3(), DepthAlignment = DepthAlignment.Stretch };
            var child001 = new MeasureArrangeValidator { ExpectedMeasureValue = new Vector3(100, 400, 800), ExpectedArrangeValue = new Vector3(100, 400, 800), ReturnedMeasuredValue = 1000 * rand.NextVector3(), DepthAlignment = DepthAlignment.Stretch };
            var child002 = new MeasureArrangeValidator { ExpectedMeasureValue = new Vector3(100, 400, 900), ExpectedArrangeValue = new Vector3(100, 400, 900), ReturnedMeasuredValue = 1000 * rand.NextVector3(), DepthAlignment = DepthAlignment.Stretch };

            // two cells children
            var child000C2 = new MeasureArrangeValidator { ExpectedMeasureValue = new Vector3(300, 400, 700), ExpectedArrangeValue = new Vector3(300, 400, 700), ReturnedMeasuredValue = 1000 * rand.NextVector3(), DepthAlignment = DepthAlignment.Stretch };
            var child100C2 = new MeasureArrangeValidator { ExpectedMeasureValue = new Vector3(500, 400, 700), ExpectedArrangeValue = new Vector3(500, 400, 700), ReturnedMeasuredValue = 1000 * rand.NextVector3(), DepthAlignment = DepthAlignment.Stretch };
            var child000C3 = new MeasureArrangeValidator { ExpectedMeasureValue = new Vector3(600, 400, 700), ExpectedArrangeValue = new Vector3(600, 400, 700), ReturnedMeasuredValue = 1000 * rand.NextVector3(), DepthAlignment = DepthAlignment.Stretch };
            var child000R2 = new MeasureArrangeValidator { ExpectedMeasureValue = new Vector3(100, 900, 700), ExpectedArrangeValue = new Vector3(100, 900, 700), ReturnedMeasuredValue = 1000 * rand.NextVector3(), DepthAlignment = DepthAlignment.Stretch };
            var child010R2 = new MeasureArrangeValidator { ExpectedMeasureValue = new Vector3(100, 1100, 700), ExpectedArrangeValue = new Vector3(100, 1100, 700), ReturnedMeasuredValue = 1000 * rand.NextVector3(), DepthAlignment = DepthAlignment.Stretch };
            var child000R3 = new MeasureArrangeValidator { ExpectedMeasureValue = new Vector3(100, 1500, 700), ExpectedArrangeValue = new Vector3(100, 1500, 700), ReturnedMeasuredValue = 1000 * rand.NextVector3(), DepthAlignment = DepthAlignment.Stretch };
            var child000L2 = new MeasureArrangeValidator { ExpectedMeasureValue = new Vector3(100, 400, 1500), ExpectedArrangeValue = new Vector3(100, 400, 1500), ReturnedMeasuredValue = 1000 * rand.NextVector3(), DepthAlignment = DepthAlignment.Stretch };
            var child001L2 = new MeasureArrangeValidator { ExpectedMeasureValue = new Vector3(100, 400, 1700), ExpectedArrangeValue = new Vector3(100, 400, 1700), ReturnedMeasuredValue = 1000 * rand.NextVector3(), DepthAlignment = DepthAlignment.Stretch };
            var child000L3 = new MeasureArrangeValidator { ExpectedMeasureValue = new Vector3(100, 400, 2400), ExpectedArrangeValue = new Vector3(100, 400, 2400), ReturnedMeasuredValue = 1000 * rand.NextVector3(), DepthAlignment = DepthAlignment.Stretch };

            // set the span of the children
            child000C2.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 2);
            child100C2.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 2);
            child000C3.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 3);
            child000R2.DependencyProperties.Set(GridBase.RowSpanPropertyKey, 2);
            child010R2.DependencyProperties.Set(GridBase.RowSpanPropertyKey, 2);
            child000R3.DependencyProperties.Set(GridBase.RowSpanPropertyKey, 3);
            child000L2.DependencyProperties.Set(GridBase.LayerSpanPropertyKey, 2);
            child001L2.DependencyProperties.Set(GridBase.LayerSpanPropertyKey, 2);
            child000L3.DependencyProperties.Set(GridBase.LayerSpanPropertyKey, 3);

            // place the children in the grid
            child100.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            child200.DependencyProperties.Set(GridBase.ColumnPropertyKey, 2);
            child010.DependencyProperties.Set(GridBase.RowPropertyKey, 1);
            child020.DependencyProperties.Set(GridBase.RowPropertyKey, 2);
            child001.DependencyProperties.Set(GridBase.LayerPropertyKey, 1);
            child002.DependencyProperties.Set(GridBase.LayerPropertyKey, 2);
            child100C2.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            child010R2.DependencyProperties.Set(GridBase.RowPropertyKey, 1);
            child001L2.DependencyProperties.Set(GridBase.LayerPropertyKey, 1);

            // add the children to the grid
            grid.Children.Add(child000);
            grid.Children.Add(child100);
            grid.Children.Add(child200);
            grid.Children.Add(child010);
            grid.Children.Add(child020);
            grid.Children.Add(child001);
            grid.Children.Add(child002);
            grid.Children.Add(child000C2);
            grid.Children.Add(child100C2);
            grid.Children.Add(child000C3);
            grid.Children.Add(child000R2);
            grid.Children.Add(child010R2);
            grid.Children.Add(child000R3);
            grid.Children.Add(child000L2);
            grid.Children.Add(child001L2);
            grid.Children.Add(child000L3);

            //measure with too small size
            grid.Measure(Vector3.Zero);
            Assert.Equal(new Vector3(600, 1500, 2400), grid.DesiredSizeWithMargins);
            // measure with too big size
            grid.Measure(float.PositiveInfinity * Vector3.One);
            Assert.Equal(new Vector3(600, 1500, 2400), grid.DesiredSizeWithMargins);

            // arrange with too small size
            grid.Arrange(Vector3.Zero, false);
            Assert.Equal(new Vector3(600, 1500, 2400), grid.RenderSize);
            // arrange with too big size
            grid.Arrange(float.PositiveInfinity * Vector3.One, false);

            // test the strip actual size
            Assert.Equal(100, grid.ColumnDefinitions[0].ActualSize);
            Assert.Equal(200, grid.ColumnDefinitions[1].ActualSize);
            Assert.Equal(300, grid.ColumnDefinitions[2].ActualSize);
            Assert.Equal(400, grid.RowDefinitions[0].ActualSize);
            Assert.Equal(500, grid.RowDefinitions[1].ActualSize);
            Assert.Equal(600, grid.RowDefinitions[2].ActualSize);
            Assert.Equal(700, grid.LayerDefinitions[0].ActualSize);
            Assert.Equal(800, grid.LayerDefinitions[1].ActualSize);
            Assert.Equal(900, grid.LayerDefinitions[2].ActualSize);
        }


        /// <summary>
        /// Basic Test that check that fix-size strip layering (both measuring and arranging) works properly.
        /// </summary>
        [Fact]
        public void TestFixedOnlyBasicLayering()
        {
            var grid = new Grid();

            var columnSizes = new List<float>();
            var rowSizes = new List<float>();
            var layerSizes = new List<float>();

            for (int i = 0; i < 4; i++)
                columnSizes.Add((float)rand.NextDouble());
            for (int i = 0; i < 5; i++)
                rowSizes.Add((float)rand.NextDouble());
            for (int i = 0; i < 6; i++)
                layerSizes.Add((float)rand.NextDouble());

            CreateFixedSizeDefinition(grid.ColumnDefinitions, columnSizes);
            CreateFixedSizeDefinition(grid.RowDefinitions, rowSizes);
            CreateFixedSizeDefinition(grid.LayerDefinitions, layerSizes);

            var size = rand.NextVector3();
            grid.Measure(size);
            grid.Arrange(size, false);

            CheckFixedSizeStripSize(grid.ColumnDefinitions, columnSizes);
        }

        private void CheckFixedSizeStripSize(StripDefinitionCollection definitions, List<float> sizes)
        {
            for (var i=0; i<definitions.Count; ++i)
                Assert.Equal(sizes[i], definitions[i].ActualSize);
        }

        private void CreateFixedSizeDefinition(StripDefinitionCollection definitions, List<float> sizes)
        {
            definitions.Clear();
            foreach (var size in sizes)
                definitions.Add(new StripDefinition(StripType.Fixed, size));
        }


        /// <summary>
        /// Test the values of the layering with basic star values definitions.
        /// </summary>
        [Fact]
        public void TestStarOnlyBasicLayering()
        {
            // 0  10*  10   20*    30    30*     60
            // +-------+-----------+-------------+
            // |<-c00->|<--c01-->  |<---c02--->  |
            // +-------+-----------+-------------+

            var grid = new Grid();
            var ratios = new List<float> { 10, 20, 30 };

            grid.ColumnDefinitions.Clear();
            foreach (var ratio in ratios)
                grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, ratio));

            var c00 = new ArrangeValidator { ReturnedMeasuredValue = new Vector3(10, 0, 0), ExpectedArrangeValue = new Vector3(20, 0, 0) };
            var c01 = new ArrangeValidator { ReturnedMeasuredValue = new Vector3(15, 0, 0), ExpectedArrangeValue = new Vector3(40, 0, 0) };
            var c02 = new ArrangeValidator { ReturnedMeasuredValue = new Vector3(20, 0, 0), ExpectedArrangeValue = new Vector3(60, 0, 0) };

            c01.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            c02.DependencyProperties.Set(GridBase.ColumnPropertyKey, 2);

            grid.Children.Add(c00);
            grid.Children.Add(c01);
            grid.Children.Add(c02);

            grid.Measure(50 * rand.NextVector3());
            Assert.Equal(new Vector3(60,0,0), grid.DesiredSizeWithMargins);

            grid.Arrange(2*grid.DesiredSizeWithMargins, false);

            for (int i = 0; i < ratios.Count; i++)
                Assert.Equal(2*ratios[i], grid.ColumnDefinitions[i].ActualSize);
        }

        /// <summary>
        /// Test the layering with star only but multiple strip definitions
        /// </summary>
        [Fact]
        public void TestStarOnlyMultiSpanLayering()
        {
            // 0      30*     30     20*    50    10*    60
            // +--------------+--------------+-----------+
            // |<-c00->       |<--c01-->     |<--c02-->  |
            // +--------------+--------------+-----------+
            // |              |<----------c11----------->|
            // +--------------+--------------+-----------+
            // |<---------------c20---------------->     |
            // +--------------+--------------+-----------+

            var grid = new Grid();
            var ratios = new List<float> { 30, 20, 10 };

            grid.ColumnDefinitions.Clear();
            foreach (var ratio in ratios)
                grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, ratio));

            var c00 = new ArrangeValidator { Name = "c00", ReturnedMeasuredValue = new Vector3(20, 0, 0), ExpectedArrangeValue = new Vector3(60, 0, 0) };
            var c01 = new ArrangeValidator { Name = "c01", ReturnedMeasuredValue = new Vector3(15, 0, 0), ExpectedArrangeValue = new Vector3(40, 0, 0) };
            var c02 = new ArrangeValidator { Name = "c02", ReturnedMeasuredValue = new Vector3( 8, 0, 0), ExpectedArrangeValue = new Vector3(20, 0, 0) };
            var c11 = new ArrangeValidator { Name = "c11", ReturnedMeasuredValue = new Vector3(30, 0, 0), ExpectedArrangeValue = new Vector3(60, 0, 0) };
            var c20 = new ArrangeValidator { Name = "c20", ReturnedMeasuredValue = new Vector3(55, 0, 0), ExpectedArrangeValue = new Vector3(120, 0, 0) };
            
            c11.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 2);
            c20.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 3);

            c01.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            c02.DependencyProperties.Set(GridBase.ColumnPropertyKey, 2);
            c11.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            c11.DependencyProperties.Set(GridBase.RowPropertyKey, 1);
            c20.DependencyProperties.Set(GridBase.RowPropertyKey, 2);

            grid.Children.Add(c00);
            grid.Children.Add(c01);
            grid.Children.Add(c02);
            grid.Children.Add(c11);
            grid.Children.Add(c20);

            grid.Measure(50 * rand.NextVector3());
            Assert.Equal(new Vector3(60, 0, 0), grid.DesiredSizeWithMargins);

            grid.Arrange(2 * grid.DesiredSizeWithMargins, false);

            for (int i = 0; i < ratios.Count; i++)
                Assert.Equal(2 * ratios[i], grid.ColumnDefinitions[i].ActualSize);
        }

        /// <summary>
        /// Basic test on single star element with minimum
        /// </summary>
        [Fact]
        public void TestStarOnlyMin1EltLayering()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 10) { MinimumSize = 20 });
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));

            //    min = 20
            // 0    10*   20   
            // +----------+
            // |<-c00->   |
            // +----------+

            var c00 = new ArrangeValidator { Name = "c00", ReturnedMeasuredValue = new Vector3(10, 0, 0), ExpectedArrangeValue = new Vector3(20, 0, 0) };
            grid.Children.Add(c00);

            grid.Measure(50 * rand.NextVector3());
            Assert.Equal(new Vector3(20, 0, 0), grid.DesiredSizeWithMargins);

            grid.Arrange(15 * Vector3.One, false);
            Assert.Equal(20 , grid.ColumnDefinitions[0].ActualSize);

         }

        /// <summary>
        /// Basic test on single star element with maximum
        /// </summary>
        [Fact]
        public void TestStarOnlyMax1EltLayering()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 10) { MaximumSize = 20 });
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));

            //    max = 20
            // 0    10*   20   
            // +----------+
            // |<----c00--|-->
            // +----------+

            var c00 = new ArrangeValidator { Name = "c00", ReturnedMeasuredValue = new Vector3(30, 0, 0), ExpectedArrangeValue = new Vector3(20, 0, 0) };
            grid.Children.Add(c00);

            grid.Measure(50 * rand.NextVector3());
            Assert.Equal(new Vector3(20, 0, 0), grid.DesiredSizeWithMargins);

            grid.Arrange(40 * Vector3.One, false);
            Assert.Equal(20, grid.ColumnDefinitions[0].ActualSize);
        }

        /// <summary>
        /// Basic test on double star elements with minimum
        /// </summary>
        [Fact]
        public void TestStarOnlyMin2EltsLayering1()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 10) { MinimumSize = 20 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 20) { MinimumSize = 30 });
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));

            //    min = 20   min = 30
            // 0    10*   20   20*   50
            // +----------+----------+
            // |<-c00->   |<-c01->   |          
            // +----------+----------+

            var c00 = new ArrangeValidator { Name = "c00", ReturnedMeasuredValue = new Vector3(10, 0, 0), ExpectedArrangeValue = new Vector3(20, 0, 0) };
            var c01 = new ArrangeValidator { Name = "c01", ReturnedMeasuredValue = new Vector3(20, 0, 0), ExpectedArrangeValue = new Vector3(30, 0, 0) };

            c01.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);

            grid.Children.Add(c00);
            grid.Children.Add(c01);

            grid.Measure(50 * rand.NextVector3());
            Assert.Equal(new Vector3(50, 0, 0), grid.DesiredSizeWithMargins);

            grid.Arrange(15 * Vector3.One, false);
            Assert.Equal(20, grid.ColumnDefinitions[0].ActualSize);
            Assert.Equal(30, grid.ColumnDefinitions[1].ActualSize);
        }   
        
        /// <summary>
        /// Basic test on double star elements with minimum
        /// </summary>
        [Fact]
        public void TestStarOnlyMin2EltsLayering2()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 10) { MinimumSize = 20 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 20) { MinimumSize = 80 });
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));

            //    min = 20    min = 80
            // 0    10*    20   20*   50
            // +-----------+----------+
            // |<---c00--->|<-c01->   |          
            // +---- ------+----------+

            var c00 = new ArrangeValidator { Name = "c00", ReturnedMeasuredValue = new Vector3(25, 0, 0), ExpectedArrangeValue = new Vector3(30, 0, 0) };
            var c01 = new ArrangeValidator { Name = "c01", ReturnedMeasuredValue = new Vector3(60, 0, 0), ExpectedArrangeValue = new Vector3(80, 0, 0) };

            c01.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);

            grid.Children.Add(c00);
            grid.Children.Add(c01);

            grid.Measure(50 * rand.NextVector3());
            Assert.Equal(new Vector3(105, 0, 0), grid.DesiredSizeWithMargins);

            grid.Arrange(110 * Vector3.One, false);
            Assert.Equal(30, grid.ColumnDefinitions[0].ActualSize);
            Assert.Equal(80, grid.ColumnDefinitions[1].ActualSize);
        }

        /// <summary>
        /// Basic test on double star elements with minimum
        /// </summary>
        [Fact]
        public void TestStarOnlyMin2EltsLayering3()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 10) { MinimumSize = 20 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 20) { MinimumSize = 30 });
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));

            //    min = 20   min = 30
            // 0    10*    20   20*   50
            // +-----------+----------+
            // |<---c00--->|<-c01->   |          
            // +-----------+----------+

            var c00 = new ArrangeValidator { Name = "c00", ReturnedMeasuredValue = new Vector3(25, 0, 0), ExpectedArrangeValue = new Vector3(30, 0, 0) };
            var c01 = new ArrangeValidator { Name = "c01", ReturnedMeasuredValue = new Vector3(20, 0, 0), ExpectedArrangeValue = new Vector3(60, 0, 0) };

            c01.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);

            grid.Children.Add(c00);
            grid.Children.Add(c01);

            grid.Measure(50 * rand.NextVector3());
            Assert.Equal(new Vector3(75, 0, 0), grid.DesiredSizeWithMargins);

            grid.Arrange(90 * Vector3.One, false);
            Assert.Equal(30, grid.ColumnDefinitions[0].ActualSize);
            Assert.Equal(60, grid.ColumnDefinitions[1].ActualSize);
        }

        /// <summary>
        /// Basic test on double star elements with maximum
        /// </summary>
        [Fact]
        public void TestStarOnlyMax2EltsLayering1()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 10) { MaximumSize = 20 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 20) { MaximumSize = 30 });
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));

            //    max = 20   max = 30
            // 0    10*    20   20*   50
            // +-----------+----------+
            // |<---c00----|<->--c01--|-->           
            // +-----------+----------+

            var c00 = new ArrangeValidator { Name = "c00", ReturnedMeasuredValue = new Vector3(25, 0, 0), ExpectedArrangeValue = new Vector3(20, 0, 0) };
            var c01 = new ArrangeValidator { Name = "c01", ReturnedMeasuredValue = new Vector3(35, 0, 0), ExpectedArrangeValue = new Vector3(30, 0, 0) };

            c01.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);

            grid.Children.Add(c00);
            grid.Children.Add(c01);

            grid.Measure(50 * rand.NextVector3());
            Assert.Equal(new Vector3(50, 0, 0), grid.DesiredSizeWithMargins);

            grid.Arrange(90 * Vector3.One, false);
            Assert.Equal(20, grid.ColumnDefinitions[0].ActualSize);
            Assert.Equal(30, grid.ColumnDefinitions[1].ActualSize);
        }

        /// <summary>
        /// Basic test on double star elements with maximum
        /// </summary>
        [Fact]
        public void TestStarOnlyMax2EltsLayering2()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 10) { MaximumSize = 20 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 20) { MaximumSize = 50 });
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));

            //    max = 20   max = 50
            // 0    10*    20   20*   50
            // +-----------+----------+
            // |<---c00----|<->-c01-->|           
            // +-----------+----------+

            var c00 = new ArrangeValidator { Name = "c00", ReturnedMeasuredValue = new Vector3(30, 0, 0), ExpectedArrangeValue = new Vector3(20, 0, 0) };
            var c01 = new ArrangeValidator { Name = "c01", ReturnedMeasuredValue = new Vector3(30, 0, 0), ExpectedArrangeValue = new Vector3(45, 0, 0) };

            c01.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);

            grid.Children.Add(c00);
            grid.Children.Add(c01);

            grid.Measure(50 * rand.NextVector3());
            Assert.Equal(new Vector3(60, 0, 0), grid.DesiredSizeWithMargins);

            grid.Arrange(65 * Vector3.One, false);
            Assert.Equal(20, grid.ColumnDefinitions[0].ActualSize);
            Assert.Equal(45, grid.ColumnDefinitions[1].ActualSize);
        }

        /// <summary>
        /// Basic test on double star elements with maximum
        /// </summary>
        [Fact]
        public void TestStarOnlyMax2EltsLayering3()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 10) { MaximumSize = 40 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 20) { MaximumSize = 50 });
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));

            //    max = 40   max = 50
            // 0    10*    20   20*   50
            // +-----------+----------+
            // |<---c00--->|<--c01--> |           
            // +-----------+----------+

            var c00 = new ArrangeValidator { Name = "c00", ReturnedMeasuredValue = new Vector3(15, 0, 0), ExpectedArrangeValue = new Vector3(20, 0, 0) };
            var c01 = new ArrangeValidator { Name = "c01", ReturnedMeasuredValue = new Vector3(25, 0, 0), ExpectedArrangeValue = new Vector3(40, 0, 0) };

            c01.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);

            grid.Children.Add(c00);
            grid.Children.Add(c01);

            grid.Measure(50 * rand.NextVector3());
            Assert.Equal(new Vector3(45, 0, 0), grid.DesiredSizeWithMargins);

            grid.Arrange(60 * Vector3.One, false);
            Assert.Equal(20, grid.ColumnDefinitions[0].ActualSize);
            Assert.Equal(40, grid.ColumnDefinitions[1].ActualSize);
        }

        /// <summary>
        /// Test on multi minimum bounded star element
        /// </summary>
        [Fact]
        public void TestStarOnlyMultiMinLayering1()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 10) { MinimumSize = 20 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 20) { MinimumSize = 30 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 30) { MinimumSize = 40 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 40) { MinimumSize = 50 });
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));

            //    min = 20    min = 30  min = 40  min = 50
            // 0    10*    20   20*   50        90       140
            // +-----------+----------+---------+--------+
            // |<---------------c00--------------->      |
            // +---- ------+----------+---------+--------+

            var c00 = new ArrangeValidator { Name = "c00", ReturnedMeasuredValue = new Vector3(25, 0, 0), ExpectedArrangeValue = new Vector3(140, 0, 0) };
            c00.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 4);
            grid.Children.Add(c00);

            grid.Measure(50 * rand.NextVector3());
            Assert.Equal(new Vector3(140, 0, 0), grid.DesiredSizeWithMargins);

            grid.Arrange(50 * Vector3.One, false);
            Assert.Equal(20, grid.ColumnDefinitions[0].ActualSize);
            Assert.Equal(30, grid.ColumnDefinitions[1].ActualSize);
            Assert.Equal(40, grid.ColumnDefinitions[2].ActualSize);
            Assert.Equal(50, grid.ColumnDefinitions[3].ActualSize);
        }

        /// <summary>
        /// Test on multi minimum bounded star element
        /// </summary>
        [Fact]
        public void TestStarOnlyMultiMinLayering2()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star) { MinimumSize = 20 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star) { MinimumSize = 50 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star) { MinimumSize = 40 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star) { MinimumSize = 30 });
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));

            // | <--min 20-> |<---min 50--->|<---min 40-->| <-min 30--> |
            // 0      1*     20     1*      75     1*     115    1*     140
            // +-------------+--------------+-------------+-------------+
            // |<-------------------------c00-------------------------->|
            // +-------------+--------------+-------------+-------------+

            var c00 = new ArrangeValidator { Name = "c00", ReturnedMeasuredValue = new Vector3(150, 0, 0), ExpectedArrangeValue = new Vector3(150, 0, 0) };
            c00.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 4);
            grid.Children.Add(c00);

            grid.Measure(50 * rand.NextVector3());
            Assert.Equal(new Vector3(150, 0, 0), grid.DesiredSizeWithMargins);

            grid.Arrange(150 * Vector3.One, false);
            Assert.Equal(30, grid.ColumnDefinitions[0].ActualSize);
            Assert.Equal(50, grid.ColumnDefinitions[1].ActualSize);
            Assert.Equal(40, grid.ColumnDefinitions[2].ActualSize);
            Assert.Equal(30, grid.ColumnDefinitions[3].ActualSize);
        }

        /// <summary>
        /// Test on multi maximum bounded star element
        /// </summary>
        [Fact]
        public void TestStarOnlyMultiMaxLayering1()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star) { MaximumSize = 20 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star) { MaximumSize = 50 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star) { MaximumSize = 40 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star) { MaximumSize = 30 });
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));

            // |<---max 20-->|<---max 50--->|<---max 40-->|<--max 30--->|
            // 0      1*     20     1*      75     1*     115    1*     140
            // +-------------+--------------+-------------+-------------+
            // |<-------------------------c00---------------------------|----->
            // +-------------+--------------+-------------+-------------+

            var c00 = new ArrangeValidator { Name = "c00", ReturnedMeasuredValue = new Vector3(160, 0, 0), ExpectedArrangeValue = new Vector3(140, 0, 0) };
            c00.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 4);
            grid.Children.Add(c00);

            grid.Measure(50 * rand.NextVector3());
            Assert.Equal(new Vector3(140, 0, 0), grid.DesiredSizeWithMargins);

            grid.Arrange(200 * Vector3.One, false);
            Assert.Equal(20, grid.ColumnDefinitions[0].ActualSize);
            Assert.Equal(50, grid.ColumnDefinitions[1].ActualSize);
            Assert.Equal(40, grid.ColumnDefinitions[2].ActualSize);
            Assert.Equal(30, grid.ColumnDefinitions[3].ActualSize);
        }        
        
        /// <summary>
        /// Test on multi maximum bounded star element
        /// </summary>
        [Fact]
        public void TestStarOnlyMultiMaxLayering2()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star) { MaximumSize = 20 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star) { MaximumSize = 50 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star) { MaximumSize = 40 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star) { MaximumSize = 30 });
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));

            // |<---max 20-->|<---max 50----|<->---max 40-|<->-max 30-->|
            // 0      1*     20     1*      75     1*     115    1*     140
            // +-------------+--------------+-------------+-------------+
            // |<-------------------------c00-------------------------->|
            // +-------------+--------------+-------------+-------------+

            var c00 = new ArrangeValidator { Name = "c00", ReturnedMeasuredValue = new Vector3(110, 0, 0), ExpectedArrangeValue = new Vector3(110, 0, 0) };
            c00.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 4);
            grid.Children.Add(c00);

            grid.Measure(50 * rand.NextVector3());
            Assert.Equal(new Vector3(110, 0, 0), grid.DesiredSizeWithMargins);

            grid.Arrange(110 * Vector3.One, false);
            Assert.Equal(20, grid.ColumnDefinitions[0].ActualSize);
            Assert.Equal(30, grid.ColumnDefinitions[1].ActualSize);
            Assert.Equal(30, grid.ColumnDefinitions[2].ActualSize);
            Assert.Equal(30, grid.ColumnDefinitions[3].ActualSize);
        }

        /// <summary>
        /// Test on multi minimum maximum bounded star element
        /// </summary>
        [Fact]
        public void TestStarOnlyMinMaxLayering1()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star) { MinimumSize = 10, MaximumSize = 20 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star) { MinimumSize = 25, MaximumSize = 35 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star) { MinimumSize = 30, MaximumSize = 40 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star) { MinimumSize = 45, MaximumSize = 60 });
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));

            // |<--min 10->  |<---min 25--> |<---min 30-> |<--min 45-->|
            // |<---max 20-->|<---max 35--->|<---max 40-->|<----max 60---->|
            // 0      1*     20     1*      55     1*     95     1*     145
            // +-------------+--------------+-------------+-------------+
            // |<-------------------------c00-------------------------->|
            // +-------------+--------------+-------------+-------------+

            var c00 = new ArrangeValidator { Name = "c00", ReturnedMeasuredValue = new Vector3(145, 0, 0), ExpectedArrangeValue = new Vector3(145, 0, 0) };
            c00.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 4);
            grid.Children.Add(c00);

            grid.Measure(50 * rand.NextVector3());
            Assert.Equal(new Vector3(145, 0, 0), grid.DesiredSizeWithMargins);

            grid.Arrange(145 * Vector3.One, false);
            Assert.Equal(20, grid.ColumnDefinitions[0].ActualSize);
            Assert.Equal(35, grid.ColumnDefinitions[1].ActualSize);
            Assert.Equal(40, grid.ColumnDefinitions[2].ActualSize);
            Assert.Equal(50, grid.ColumnDefinitions[3].ActualSize);
        }

        /// <summary>
        /// Test on multi minimum maximum bounded star element
        /// </summary>
        [Fact]
        public void TestStarOnlyMinMaxLayering2()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star) { MinimumSize = 10, MaximumSize = 70 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star) { MinimumSize = 20, MaximumSize = 35 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star) { MinimumSize = 30, MaximumSize = 55 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star) { MinimumSize = 40, MaximumSize = 45 });
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));

            // |<--min 10->  |<---min 20->  |<---min 30-> |<--min 40->|
            // |<---max 70---|<-->-max 35-->|<---max 55-->|<---max 45-->|
            // 0      1*     20     1*      55     1*     95     1*     145
            // +-------------+--------------+-------------+-------------+
            // |<-------------------------c00-------------------------->|
            // +-------------+--------------+-------------+-------------+

            var c00 = new ArrangeValidator { Name = "c00", ReturnedMeasuredValue = new Vector3(195, 0, 0), ExpectedArrangeValue = new Vector3(195, 0, 0) };
            c00.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 4);
            grid.Children.Add(c00);

            grid.Measure(50 * rand.NextVector3());
            Assert.Equal(new Vector3(195, 0, 0), grid.DesiredSizeWithMargins);

            grid.Arrange(195 * Vector3.One, false);
            Assert.Equal(60, grid.ColumnDefinitions[0].ActualSize);
            Assert.Equal(35, grid.ColumnDefinitions[1].ActualSize);
            Assert.Equal(55, grid.ColumnDefinitions[2].ActualSize);
            Assert.Equal(45, grid.ColumnDefinitions[3].ActualSize);
        }

        /// <summary>
        /// Basic test on multi type of strips
        /// </summary>
        [Fact]
        public void TestBasicMultiTypeLayering()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Fixed, 30));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star));
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));

            // 0     30     30     auto     60     1*     100
            // +-------------+--------------+-------------+
            // |<---c00--->  |<----c01----->|             |
            // +-------------+--------------+-------------+
            // |             |<-------------c11---------->|
            // +-------------+--------------+-------------+

            var c00 = new ArrangeValidator { Name = "c00", ReturnedMeasuredValue = new Vector3(20, 0, 0), ExpectedArrangeValue = new Vector3(30, 0, 0) };
            var c01 = new ArrangeValidator { Name = "c01", ReturnedMeasuredValue = new Vector3(30, 0, 0), ExpectedArrangeValue = new Vector3(30, 0, 0) };
            var c11 = new ArrangeValidator { Name = "c11", ReturnedMeasuredValue = new Vector3(70, 0, 0), ExpectedArrangeValue = new Vector3(80, 0, 0) };

            c01.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            c11.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            c11.DependencyProperties.Set(GridBase.RowPropertyKey, 1);
            c11.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 2);

            grid.Children.Add(c00);
            grid.Children.Add(c01);
            grid.Children.Add(c11);

            grid.Measure(50 * rand.NextVector3());
            Assert.Equal(new Vector3(100, 0, 0), grid.DesiredSizeWithMargins);

            grid.Arrange(110 * Vector3.One, false);
            Assert.Equal(30, grid.ColumnDefinitions[0].ActualSize);
            Assert.Equal(30, grid.ColumnDefinitions[1].ActualSize);
            Assert.Equal(50, grid.ColumnDefinitions[2].ActualSize);
        }

        /// <summary>
        /// Test the values of the layering with simples auto values.
        /// </summary>
        [Fact]
        public void TestAutoOnlyBasicLayering()
        {
            // 0       10          40            80
            // +-------+-----------+-------------+
            // |<-c00->|<--c01-->  |             |
            // +-------+-----------+-------------+
            // |       |<---c11--->|<----c12---->|
            // +-------+-----------+-------------+

            var grid = new Grid();

            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto));
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));

            var c00 = new ArrangeValidator { ReturnedMeasuredValue = new Vector3(10, 0, 0), ExpectedArrangeValue = new Vector3(10, 0, 0) };
            var c01 = new ArrangeValidator { ReturnedMeasuredValue = new Vector3(20, 0, 0), ExpectedArrangeValue = new Vector3(30, 0, 0) };
            var c11 = new ArrangeValidator { ReturnedMeasuredValue = new Vector3(30, 0, 0), ExpectedArrangeValue = new Vector3(30, 0, 0) };
            var c12 = new ArrangeValidator { ReturnedMeasuredValue = new Vector3(40, 0, 0), ExpectedArrangeValue = new Vector3(40, 0, 0) };

            c00.DependencyProperties.Set(GridBase.ColumnPropertyKey, 0);
            c00.DependencyProperties.Set(GridBase.RowPropertyKey, 0);

            c01.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            c01.DependencyProperties.Set(GridBase.RowPropertyKey, 0);

            c11.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            c11.DependencyProperties.Set(GridBase.RowPropertyKey, 1);

            c12.DependencyProperties.Set(GridBase.ColumnPropertyKey, 2);
            c12.DependencyProperties.Set(GridBase.RowPropertyKey, 1);

            grid.Children.Add(c00);
            grid.Children.Add(c01);
            grid.Children.Add(c11);
            grid.Children.Add(c12);

            grid.Measure(30 * rand.NextVector3());
            Assert.Equal(new Vector3(80,0,0), grid.DesiredSizeWithMargins);

            grid.Arrange(30 * rand.NextVector3(), false);

            Assert.Equal(10, grid.ColumnDefinitions[0].ActualSize);
            Assert.Equal(30, grid.ColumnDefinitions[1].ActualSize);
            Assert.Equal(40, grid.ColumnDefinitions[2].ActualSize);
        }

        /// <summary>
        /// Tests auto layering with strips of multi-span
        /// </summary>
        [Fact]
        public void TestAutoOnlyMultiSpanLayering()
        {
            // 0       10          40                70
            // +-------+-----------+-----------------+
            // |<-c00->|<--c01-->  |                 |
            // +-------+-----------+-----------------+
            // |<-------c10------->|<---c12--->      |
            // +-------+-----------+-----------------+
            // |       |<-----------c21---------->   |
            // +-------+-----------+-----------------+
            // |<-----------------c30--------------->|
            // +-------+-----------+-----------------+

            var grid = new Grid();

            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto));
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));

            var c00 = new ArrangeValidator { ReturnedMeasuredValue = new Vector3(10, 0, 0), ExpectedArrangeValue = new Vector3(10, 0, 0) };
            var c01 = new ArrangeValidator { ReturnedMeasuredValue = new Vector3(20, 0, 0), ExpectedArrangeValue = new Vector3(30, 0, 0) };
            var c10 = new ArrangeValidator { ReturnedMeasuredValue = new Vector3(40, 0, 0), ExpectedArrangeValue = new Vector3(40, 0, 0) };
            var c12 = new ArrangeValidator { ReturnedMeasuredValue = new Vector3(10, 0, 0), ExpectedArrangeValue = new Vector3(30, 0, 0) };
            var c21 = new ArrangeValidator { ReturnedMeasuredValue = new Vector3(30, 0, 0), ExpectedArrangeValue = new Vector3(60, 0, 0) };
            var c30 = new ArrangeValidator { ReturnedMeasuredValue = new Vector3(70, 0, 0), ExpectedArrangeValue = new Vector3(70, 0, 0) };

            // set the spans 
            c10.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 2);
            c21.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 2);
            c30.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 3);

            // set the positions
            c01.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            c10.DependencyProperties.Set(GridBase.RowPropertyKey, 1);
            c12.DependencyProperties.Set(GridBase.ColumnPropertyKey, 2);
            c12.DependencyProperties.Set(GridBase.RowPropertyKey, 1);
            c21.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            c21.DependencyProperties.Set(GridBase.RowPropertyKey, 2);
            c30.DependencyProperties.Set(GridBase.RowPropertyKey, 3);

            // add the children
            grid.Children.Add(c00);
            grid.Children.Add(c01);
            grid.Children.Add(c10);
            grid.Children.Add(c12);
            grid.Children.Add(c21);
            grid.Children.Add(c30);

            grid.Measure(30 * rand.NextVector3());
            Assert.Equal(new Vector3(70, 0, 0), grid.DesiredSizeWithMargins);

            grid.Arrange(50 * rand.NextVector3(), false);

            Assert.Equal(10, grid.ColumnDefinitions[0].ActualSize);
            Assert.Equal(30, grid.ColumnDefinitions[1].ActualSize);
            Assert.Equal(30, grid.ColumnDefinitions[2].ActualSize);
        }

        /// <summary>
        /// Tests auto layering with strips with min and max values
        /// </summary>
        [Fact] 
        public void TestAutoOnlyMinMaxLayering()
        {
            //  min = 20                                                      min = 20
            //                                 max = 20             max = 20  max = 20
            // 0         20        40         60        80        100        120      140
            // +---------+---------+----------+---------+---------+----------+--------+
            // |<-c00->  |<--c01-->|                    |<-c04->  |<----c05--|-->     |
            // +---------+---------+----------+---------+---------+----------+--------+
            // |<-------c10------->|                    |         |          |<-c16-> |
            // +---------+---------+----------+---------+---------+----------+--------+
            // |         |<----------c21-------->       |         |          |<---c26-|-->
            // +---------+---------+----------+---------+---------+----------+--------|
            // |         |         |<-------c32-------->|         |          |        |
            // +---------+---------+----------+---------+---------+----------+--------+
            // |         |         |          |         |<------------c44------------>|
            // +---------+---------+----------+---------+---------+----------+--------+

            var grid = new Grid();

            // set the grid definition
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto) { MinimumSize = 20 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto) { MaximumSize = 20 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto) { MaximumSize = 20 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto) { MaximumSize = 20, MinimumSize = 20 });
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));

            // create the children
            var c00 = new ArrangeValidator { ReturnedMeasuredValue = new Vector3(10, 0, 0), ExpectedArrangeValue = new Vector3(20, 0, 0), Name = "c00" };
            var c01 = new ArrangeValidator { ReturnedMeasuredValue = new Vector3(20, 0, 0), ExpectedArrangeValue = new Vector3(20, 0, 0), Name = "c01" };
            var c04 = new ArrangeValidator { ReturnedMeasuredValue = new Vector3(10, 0, 0), ExpectedArrangeValue = new Vector3(20, 0, 0), Name = "c03" };
            var c05 = new ArrangeValidator { ReturnedMeasuredValue = new Vector3(30, 0, 0), ExpectedArrangeValue = new Vector3(20, 0, 0), Name = "c04" };
            var c10 = new ArrangeValidator { ReturnedMeasuredValue = new Vector3(40, 0, 0), ExpectedArrangeValue = new Vector3(40, 0, 0), Name = "c10" };
            var c16 = new ArrangeValidator { ReturnedMeasuredValue = new Vector3(10, 0, 0), ExpectedArrangeValue = new Vector3(20, 0, 0), Name = "c15" };
            var c21 = new ArrangeValidator { ReturnedMeasuredValue = new Vector3(50, 0, 0), ExpectedArrangeValue = new Vector3(60, 0, 0), Name = "c21" };
            var c26 = new ArrangeValidator { ReturnedMeasuredValue = new Vector3(30, 0, 0), ExpectedArrangeValue = new Vector3(20, 0, 0), Name = "c25" };
            var c32 = new ArrangeValidator { ReturnedMeasuredValue = new Vector3(40, 0, 0), ExpectedArrangeValue = new Vector3(40, 0, 0), Name = "c32" };
            var c44 = new ArrangeValidator { ReturnedMeasuredValue = new Vector3(60, 0, 0), ExpectedArrangeValue = new Vector3(60, 0, 0), Name = "c44" };

            // set the spans 
            c10.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 2);
            c21.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 3);
            c32.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 2);
            c44.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 3);

            // set the positions
            c01.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            c04.DependencyProperties.Set(GridBase.ColumnPropertyKey, 4);
            c05.DependencyProperties.Set(GridBase.ColumnPropertyKey, 5);
            c10.DependencyProperties.Set(GridBase.RowPropertyKey, 1);
            c16.DependencyProperties.Set(GridBase.ColumnPropertyKey, 6);
            c16.DependencyProperties.Set(GridBase.RowPropertyKey, 1);
            c21.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            c21.DependencyProperties.Set(GridBase.RowPropertyKey, 2);
            c26.DependencyProperties.Set(GridBase.ColumnPropertyKey, 6);
            c26.DependencyProperties.Set(GridBase.RowPropertyKey, 2);
            c32.DependencyProperties.Set(GridBase.ColumnPropertyKey, 2);
            c32.DependencyProperties.Set(GridBase.RowPropertyKey, 3);
            c44.DependencyProperties.Set(GridBase.ColumnPropertyKey, 4);
            c44.DependencyProperties.Set(GridBase.RowPropertyKey, 4);

            // add the children
            grid.Children.Add(c00);
            grid.Children.Add(c01);
            grid.Children.Add(c04);
            grid.Children.Add(c05);
            grid.Children.Add(c10);
            grid.Children.Add(c16);
            grid.Children.Add(c21);
            grid.Children.Add(c26);
            grid.Children.Add(c32);
            grid.Children.Add(c44);

            grid.Measure(30 * rand.NextVector3());
            Assert.Equal(new Vector3(140, 0, 0), grid.DesiredSizeWithMargins);

            grid.Arrange(100 * rand.NextVector3(), false);

            Assert.Equal(20, grid.ColumnDefinitions[0].ActualSize);
            Assert.Equal(20, grid.ColumnDefinitions[1].ActualSize);
            Assert.Equal(20, grid.ColumnDefinitions[2].ActualSize);
            Assert.Equal(20, grid.ColumnDefinitions[3].ActualSize);
            Assert.Equal(20, grid.ColumnDefinitions[4].ActualSize);
            Assert.Equal(20, grid.ColumnDefinitions[5].ActualSize);
            Assert.Equal(20, grid.ColumnDefinitions[6].ActualSize);
        }

        /// <summary>
        /// Check that the available sizes provided to the children during measure are correct whatever the strips type is
        /// </summary>
        [Fact]
        public void TestMeasureProvidedSizeMix()
        {
            var grid = new Grid();

            var providedSize = 100 * Vector3.One;

            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 4));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 6));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Fixed, 10));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Fixed, 15));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto));

            var child0 = new MeasureReflector { Name = "0", };
            var child1 = new MeasureReflector { Name = "1", };
            var child2 = new MeasureReflector { Name = "2", };
            var child3 = new ArrangeValidator { Name = "3", ReturnedMeasuredValue = new Vector3(5, providedSize.Y, providedSize.Z) };
            var child4 = new MeasureReflector { Name = "4", };
            var child5 = new ArrangeValidator { Name = "5", ReturnedMeasuredValue = new Vector3(20, providedSize.Y, providedSize.Z)};

            child0.DependencyProperties.Set(GridBase.ColumnPropertyKey, 0);
            child1.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            child2.DependencyProperties.Set(GridBase.ColumnPropertyKey, 2);
            child3.DependencyProperties.Set(GridBase.ColumnPropertyKey, 3);
            child4.DependencyProperties.Set(GridBase.ColumnPropertyKey, 4);
            child5.DependencyProperties.Set(GridBase.ColumnPropertyKey, 5);

            grid.Children.Add(child0);
            grid.Children.Add(child1);
            grid.Children.Add(child2);
            grid.Children.Add(child3);
            grid.Children.Add(child4);
            grid.Children.Add(child5);

            grid.Measure(providedSize);

            Utilities.AssertAreNearlyEqual(20, child0.DesiredSize.X);
            Utilities.AssertAreNearlyEqual(30, child1.DesiredSize.X);
            Utilities.AssertAreNearlyEqual(10, child2.DesiredSize.X);
            Utilities.AssertAreNearlyEqual(5, child3.DesiredSize.X);
            Utilities.AssertAreNearlyEqual(15, child4.DesiredSize.X);
            Utilities.AssertAreNearlyEqual(20, child5.DesiredSize.X);
        }

        /// <summary>
        /// Check that the available sizes provided to the children during measure are correct for fixed strips
        /// </summary>
        [Fact]
        public void TestMeasureProvidedSizeFixed()
        {
            var grid = new Grid();

            var providedSize = 1000 * rand.NextVector3();

            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Fixed) { MinimumSize = 5 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Fixed) { MaximumSize = 0.5f });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Fixed));

            var child0 = new MeasureValidator { Name = "0", ExpectedMeasureValue = new Vector3(5, providedSize.Y, providedSize.Z), ReturnedMeasuredValue = providedSize };
            var child1 = new MeasureValidator { Name = "1", ExpectedMeasureValue = new Vector3(0.5f, providedSize.Y, providedSize.Z), ReturnedMeasuredValue = providedSize };
            var child2 = new MeasureValidator { Name = "2", ExpectedMeasureValue = new Vector3(1, providedSize.Y, providedSize.Z), ReturnedMeasuredValue = providedSize };

            child0.DependencyProperties.Set(GridBase.ColumnPropertyKey, 0);
            child1.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            child2.DependencyProperties.Set(GridBase.ColumnPropertyKey, 2);

            grid.Children.Add(child0);
            grid.Children.Add(child1);
            grid.Children.Add(child2);

            grid.Measure(providedSize);
        }

        /// <summary>
        /// Check that the available sizes provided to the children during measure are correct for fixed strips
        /// </summary>
        [Fact]
        public void TestMeasureProvidedSizeFixedMulti()
        {
            var grid = new Grid();

            var providedSize = 1000 * rand.NextVector3();

            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Fixed) { MinimumSize = 5 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Fixed) { MaximumSize = 0.5f });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Fixed));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Fixed, 2));

            var child2 = new MeasureValidator { Name = "2", ExpectedMeasureValue = new Vector3(3f, providedSize.Y, providedSize.Z), ReturnedMeasuredValue = providedSize };
            var child4 = new MeasureValidator { Name = "4", ExpectedMeasureValue = new Vector3(8.5f, providedSize.Y, providedSize.Z), ReturnedMeasuredValue = providedSize };

            child2.DependencyProperties.Set(GridBase.ColumnPropertyKey, 2);
            child4.DependencyProperties.Set(GridBase.ColumnPropertyKey, 0);

            child2.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 2);
            child4.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 4);

            grid.Children.Add(child2);
            grid.Children.Add(child4);

            grid.Measure(providedSize);
        }

        /// <summary>
        /// Check that the available sizes provided to the children during measure are correct for fixed strips
        /// </summary>
        [Fact]
        public void TestMeasureProvidedSizeStar0()
        {
            var grid = new Grid();

            var providedSize = 1000 * rand.NextVector3();

            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 10));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 40));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 50));

            var child0 = new MeasureReflector { Name = "0" };
            var child1 = new MeasureReflector { Name = "1" };
            var child2 = new MeasureReflector { Name = "2" };

            child0.DependencyProperties.Set(GridBase.ColumnPropertyKey, 0);
            child1.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            child2.DependencyProperties.Set(GridBase.ColumnPropertyKey, 2);

            grid.Children.Add(child0);
            grid.Children.Add(child1);
            grid.Children.Add(child2);

            grid.Measure(providedSize);

            Utilities.AssertAreNearlyEqual(0.1f * providedSize.X, child0.DesiredSize.X);
            Utilities.AssertAreNearlyEqual(0.4f * providedSize.X, child1.DesiredSize.X);
            Utilities.AssertAreNearlyEqual(0.5f * providedSize.X, child2.DesiredSize.X);
        }

        /// <summary>
        /// Check that the available sizes provided to the children during measure are correct for fixed strips
        /// </summary>
        [Fact]
        public void TestMeasureProvidedSizeStar1()
        {
            var grid = new Grid();

            var providedSize = 100 * Vector3.One;

            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 10) { MinimumSize = 50 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 40) { MaximumSize = 10 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 50));

            var child0 = new MeasureReflector { Name = "0" };
            var child1 = new MeasureReflector { Name = "1" };
            var child2 = new MeasureReflector { Name = "2" };

            child0.DependencyProperties.Set(GridBase.ColumnPropertyKey, 0);
            child1.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            child2.DependencyProperties.Set(GridBase.ColumnPropertyKey, 2);

            grid.Children.Add(child0);
            grid.Children.Add(child1);
            grid.Children.Add(child2);

            grid.Measure(providedSize);

            Utilities.AssertAreNearlyEqual(50, child0.DesiredSize.X);
            Utilities.AssertAreNearlyEqual(10, child1.DesiredSize.X);
            Utilities.AssertAreNearlyEqual(40, child2.DesiredSize.X);
        }
        
        /// <summary>
        /// Check that the available sizes provided to the children during measure are correct for auto strips
        /// </summary>
        [Fact]
        public void TestMeasureProvidedSizeAuto0()
        {
            var grid = new Grid();

            var providedSize = 100 * Vector3.One;

            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto) { MaximumSize = 10 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto));

            var child0 = new MeasureValidator { Name = "0", ReturnedMeasuredValue = providedSize, ExpectedMeasureValue = providedSize };
            var child1 = new MeasureValidator { Name = "1", ReturnedMeasuredValue = providedSize, ExpectedMeasureValue = new Vector3(10, providedSize.Y, providedSize.Z) };
            var child2 = new MeasureValidator { Name = "2", ReturnedMeasuredValue = providedSize, ExpectedMeasureValue = providedSize };

            child0.DependencyProperties.Set(GridBase.ColumnPropertyKey, 0);
            child1.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            child2.DependencyProperties.Set(GridBase.ColumnPropertyKey, 2);

            grid.Children.Add(child0);
            grid.Children.Add(child1);
            grid.Children.Add(child2);

            grid.Measure(providedSize);
        }

        /// <summary>
        /// Check that the available sizes provided to the children during measure are correct for auto strips
        /// </summary>
        [Fact]
        public void TestMeasureProvidedSizeAuto1()
        {
            var grid = new Grid();

            var providedSize = 100 * Vector3.One;

            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star) { MinimumSize = 15 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto) { MinimumSize = 10 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Fixed, 20));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto) { MaximumSize = 10 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto));

            var child0 = new MeasureReflector { Name = "0", };
            var child1 = new MeasureReflector { Name = "1", };
            var child2 = new MeasureReflector { Name = "2", };
            var child3 = new MeasureReflector { Name = "3", };
            var child4 = new MeasureReflector { Name = "4", };

            child0.DependencyProperties.Set(GridBase.ColumnPropertyKey, 0);
            child1.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            child2.DependencyProperties.Set(GridBase.ColumnPropertyKey, 2);
            child3.DependencyProperties.Set(GridBase.ColumnPropertyKey, 3);
            child4.DependencyProperties.Set(GridBase.ColumnPropertyKey, 4);

            grid.Children.Add(child0);
            grid.Children.Add(child1);
            grid.Children.Add(child2);
            grid.Children.Add(child3);
            grid.Children.Add(child4);

            grid.Measure(providedSize);

            Utilities.AssertAreNearlyEqual(15, child0.DesiredSize.X);
            Utilities.AssertAreNearlyEqual(65, child1.DesiredSize.X);
            Utilities.AssertAreNearlyEqual(20, child2.DesiredSize.X);
            Utilities.AssertAreNearlyEqual(10, child3.DesiredSize.X);
            Utilities.AssertAreNearlyEqual(55, child4.DesiredSize.X);
        }

        /// <summary>
        /// Check that the available sizes provided to the children during measure are correct for auto strips
        /// </summary>
        [Fact]
        public void TestMeasureProvidedSizeAutoMix()
        {
            var grid = new Grid();

            var providedSize = 100 * Vector3.One;

            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto) { MaximumSize = 10 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Fixed, 20));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Fixed, 30) { MaximumSize = 25 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Auto) { MinimumSize = 10 });
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star) { MinimumSize = 15 });

            var child2 = new MeasureValidator { Name = "2", ExpectedMeasureValue = new Vector3(030, providedSize.Y, providedSize.Z), ReturnedMeasuredValue = new Vector3(030, 100, 100) };
            var child3 = new MeasureValidator { Name = "3", ExpectedMeasureValue = new Vector3(050, providedSize.Y, providedSize.Z), ReturnedMeasuredValue = new Vector3(050, 100, 100) };
            var child4 = new MeasureValidator { Name = "4", ExpectedMeasureValue = new Vector3(075, providedSize.Y, providedSize.Z), ReturnedMeasuredValue = new Vector3(075, 100, 100) };
            var child5 = new MeasureValidator { Name = "5", ExpectedMeasureValue = new Vector3(085, providedSize.Y, providedSize.Z), ReturnedMeasuredValue = new Vector3(085, 100, 100) };
            var child6 = new MeasureValidator { Name = "6", ExpectedMeasureValue = new Vector3(100, providedSize.Y, providedSize.Z), ReturnedMeasuredValue = new Vector3(100, 100, 100) };

            child2.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 2);
            child3.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 3);
            child4.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 4);
            child5.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 5);
            child6.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 6);

            grid.Children.Add(child2);
            grid.Children.Add(child3);
            grid.Children.Add(child4);
            grid.Children.Add(child5);
            grid.Children.Add(child6);

            grid.Measure(providedSize);
        }

        /// <summary>
        /// Test the invalidations generated object property changes.
        /// </summary>
        [Fact]
        public void TestBasicInvalidations()
        {
            var grid = new Grid();

            var rowDefinition = new StripDefinition();
            grid.RowDefinitions.Add(rowDefinition);

            // ReSharper disable ImplicitlyCapturedClosure

            // - test the properties that are supposed to invalidate the object measurement
            UIElementLayeringTests.TestMeasureInvalidation(grid, () => grid.RowDefinitions.Add(new StripDefinition()));
            UIElementLayeringTests.TestMeasureInvalidation(grid, () => grid.ColumnDefinitions.Add(new StripDefinition()));
            UIElementLayeringTests.TestMeasureInvalidation(grid, () => grid.LayerDefinitions.Add(new StripDefinition()));
            UIElementLayeringTests.TestMeasureInvalidation(grid, () => rowDefinition.MinimumSize = 37);
            UIElementLayeringTests.TestMeasureInvalidation(grid, () => rowDefinition.MaximumSize = 38);
            UIElementLayeringTests.TestMeasureInvalidation(grid, () => rowDefinition.Type = StripType.Fixed);
            UIElementLayeringTests.TestMeasureInvalidation(grid, () => rowDefinition.SizeValue = 39);
            UIElementLayeringTests.TestMeasureInvalidation(grid, () => grid.RowDefinitions.Remove(rowDefinition));

            // ReSharper restore ImplicitlyCapturedClosure
        }

        /// <summary>
        /// Test for the <see cref="StackPanel.GetSurroudingAnchorDistances"/>
        /// </summary>
        [Fact]
        public void TestSurroudingAnchor()
        {
            var childSize1 = new Vector3(50, 150, 250);
            var childSize2 = new Vector3(100, 200, 300);

            var grid = new Grid { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Fixed, 100));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Fixed, 200));
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));
            grid.RowDefinitions.Add(new StripDefinition(StripType.Auto));
            grid.LayerDefinitions.Add(new StripDefinition(StripType.Star));
            grid.LayerDefinitions.Add(new StripDefinition(StripType.Star));

            var child1 = new UniformGrid { Size = childSize1 };
            var child2 = new UniformGrid { Size = childSize2 };
            child2.DependencyProperties.Set(GridBase.RowPropertyKey, 1);

            grid.Children.Add(child1);
            grid.Children.Add(child2);

            grid.Measure(1000 * Vector3.One);
            grid.Arrange(1000 * Vector3.One, false);
            
            Assert.Equal(new Vector2(   0, 100), grid.GetSurroudingAnchorDistances(Orientation.Horizontal, -1));
            Assert.Equal(new Vector2(   0, 100), grid.GetSurroudingAnchorDistances(Orientation.Horizontal, 0));
            Assert.Equal(new Vector2( -50,  50), grid.GetSurroudingAnchorDistances(Orientation.Horizontal, 50));
            Assert.Equal(new Vector2( -80,  20), grid.GetSurroudingAnchorDistances(Orientation.Horizontal, 80));
            Assert.Equal(new Vector2(   0, 200), grid.GetSurroudingAnchorDistances(Orientation.Horizontal, 100));
            Assert.Equal(new Vector2( -10, 190), grid.GetSurroudingAnchorDistances(Orientation.Horizontal, 110));
            Assert.Equal(new Vector2(-200,   0), grid.GetSurroudingAnchorDistances(Orientation.Horizontal, 300));
            Assert.Equal(new Vector2(-200,   0), grid.GetSurroudingAnchorDistances(Orientation.Horizontal, 500));
            
            Assert.Equal(new Vector2(   0, 150), grid.GetSurroudingAnchorDistances(Orientation.Vertical, -1));
            Assert.Equal(new Vector2(   0, 150), grid.GetSurroudingAnchorDistances(Orientation.Vertical, 0));
            Assert.Equal(new Vector2( -50, 100), grid.GetSurroudingAnchorDistances(Orientation.Vertical, 50));
            Assert.Equal(new Vector2( -80,  70), grid.GetSurroudingAnchorDistances(Orientation.Vertical, 80));
            Assert.Equal(new Vector2(   0, 200), grid.GetSurroudingAnchorDistances(Orientation.Vertical, 150));
            Assert.Equal(new Vector2( -10, 190), grid.GetSurroudingAnchorDistances(Orientation.Vertical, 160));
            Assert.Equal(new Vector2(-200,   0), grid.GetSurroudingAnchorDistances(Orientation.Vertical, 350));
            Assert.Equal(new Vector2(-200,   0), grid.GetSurroudingAnchorDistances(Orientation.Vertical, 500));
            
            Assert.Equal(new Vector2(   0, 300), grid.GetSurroudingAnchorDistances(Orientation.InDepth, -1));
            Assert.Equal(new Vector2(   0, 300), grid.GetSurroudingAnchorDistances(Orientation.InDepth, 0));
            Assert.Equal(new Vector2( -50, 250), grid.GetSurroudingAnchorDistances(Orientation.InDepth, 50));
            Assert.Equal(new Vector2( -80, 220), grid.GetSurroudingAnchorDistances(Orientation.InDepth, 80));
            Assert.Equal(new Vector2(   0, 300), grid.GetSurroudingAnchorDistances(Orientation.InDepth, 300));
            Assert.Equal(new Vector2( -10, 290), grid.GetSurroudingAnchorDistances(Orientation.InDepth, 310));
            Assert.Equal(new Vector2(-300,   0), grid.GetSurroudingAnchorDistances(Orientation.InDepth, 600));
            Assert.Equal(new Vector2(-300,   0), grid.GetSurroudingAnchorDistances(Orientation.InDepth, 900));
        }
    }
}
