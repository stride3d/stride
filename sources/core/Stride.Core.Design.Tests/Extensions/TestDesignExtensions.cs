// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;
using Stride.Core.Extensions;

namespace Stride.Core.Design.Tests.Extensions
{
    public class TestDesingExtensions
    {
        private class Node
        {
            public Node(string value)
            {
                Value = value;
            }

            public ICollection<Node> Children { get; } = new List<Node>();

            public string Value { get; }
        }

        private Node tree;

        public TestDesingExtensions()
        {
            tree = new Node("A")
            {
                Children =
                {
                    new Node("B")
                    {
                        Children =
                        {
                            new Node("D"),
                            new Node("E")
                            {
                                Children =
                                {
                                    new Node("H")
                                },
                            },
                        },
                    },
                    new Node("C")
                    {
                        Children =
                        {
                            new Node("F"),
                            new Node("G"),
                        },
                    },
                },
            };
        }

        [Fact]
        public void TestBreadthFirst()
        {
            var result = tree.Children.BreadthFirst(n => n.Children).Aggregate(string.Empty, (s, n) => string.Concat(s, n.Value));
            Assert.Equal("BCDEFGH", result);
        }
        
        [Fact]
        public void TestDepthFirst()
        {
            var result = tree.Children.DepthFirst(n => n.Children).Aggregate(string.Empty, (s, n) => string.Concat(s, n.Value));
            Assert.Equal("BDEHCFG", result);
        }

        [Fact]
        public void TestSelectDeep()
        {
            var result = tree.Children.SelectDeep(n => n.Children).Aggregate(string.Empty, (s, n) => string.Concat(s, n.Value));
            Assert.Equal("BCFGDEH", result);
        }
    }
}
