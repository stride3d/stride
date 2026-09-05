// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Assets.Presentation.Templates;
using Xunit;

namespace Stride.GameStudio.Tests
{
    /// <summary>
    /// A template prompt sets its value through a member path on the created asset.
    /// </summary>
    public class TestTemplatePromptMemberPath
    {
        public class Shape
        {
            public object Model { get; set; }
        }

        public class Holder
        {
            public object Direct { get; set; }

            public List<Shape> Shapes { get; set; } = [];

            public Shape Missing { get; set; }
        }

        [Fact]
        public void ResolvesADirectMember()
        {
            var holder = new Holder();
            var (target, member) = AssetFactoryTemplateGenerator.ResolveMember(holder, "Direct");
            Assert.Same(holder, target);
            Assert.Equal("Direct", member.Name);
        }

        [Fact]
        public void ResolvesThroughAListElement()
        {
            var holder = new Holder { Shapes = { new Shape(), new Shape() } };
            var (target, member) = AssetFactoryTemplateGenerator.ResolveMember(holder, "Shapes[1].Model");
            Assert.Same(holder.Shapes[1], target);
            Assert.Equal("Model", member.Name);
        }

        [Theory]
        [InlineData("Unknown")]
        [InlineData("Missing.Model")]
        [InlineData("Shapes[0]")]
        public void RejectsAnInvalidPath(string path)
        {
            Assert.Throws<InvalidOperationException>(() => AssetFactoryTemplateGenerator.ResolveMember(new Holder { Shapes = { new Shape() } }, path));
        }
    }
}
