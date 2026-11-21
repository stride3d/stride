// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine.Design;
using Stride.Rendering;
using System.Numerics;

namespace Stride.Engine.Tests
{
    /// <summary>
    /// Tests for <see cref="TransformComponent"/>.
    /// </summary>
    public class TestTransformComponent
    {
        /// <summary>
        /// Test conversions between entity local/world space
        /// </summary>
        [Fact]
        public void TestWorldAndLocalSpace()
        {
            var entity = new Entity();
            var trans = entity.Transform;

            // Make sure that an entity has a transform component
            Assert.NotNull(trans);
            Assert.Single(entity.Components);
            Assert.Equal(entity.Transform, entity.Components[0]);

            // Test point to world/local space conversion
            trans.Position = new Vector3(1, 2, 3);
            trans.UpdateWorldMatrix();
            AssertNearEqual(new Vector3(1, 2, 3), trans.LocalToWorld(new Vector3(0, 0, 0)));
            AssertNearEqual(new Vector3(4, 4, 4), trans.LocalToWorld(new Vector3(3, 2, 1)));
            AssertNearEqual(new Vector3(-1, -2, -3), trans.WorldToLocal(new Vector3(0, 0, 0)));
            AssertNearEqual(new Vector3(0, 0, 0), trans.WorldToLocal(new Vector3(1, 2, 3)));
            trans.Position = new Vector3(1, 0, 0);
            trans.Rotation = Quaternion.CreateRotationX(MathF.PI * 0.5f);
            trans.Scale = new Vector3(2, 2, 2);
            trans.UpdateWorldMatrix();
            AssertNearEqual(new Vector3(1, 0, 2), trans.LocalToWorld(new Vector3(0, 1, 0)));
            Vector3 tP1 = new Vector3(0, 0, 0);
            Quaternion tR1 = new Quaternion(0, 0, 0, 1);
            Vector3 tS1 = new Vector3(1, 1, 1);
            trans.WorldToLocal(ref tP1, ref tR1, ref tS1);
            AssertNearEqual(new Vector3(-0.5f, 0, 0), tP1);
            AssertNearEqual(Quaternion.CreateRotationX(MathF.PI * -0.5f), tR1);
            AssertNearEqual(new Vector3(0.5f, 0.5f, 0.5f), tS1);
        }

        [Fact]
        public void TestSetWorldTransformation()
        {
            var scene = new Scene();
            scene.Offset = Vector3.UnitX;
            var parent = new Entity{ Scene = scene }.Transform;
            var child = new Entity{ Transform = { Parent = parent } }.Transform;

            parent.Position = Vector3.UnitX;
            parent.Rotation = Quaternion.CreateRotationY(MathF.PI * -0.5f);
            parent.Scale = Vector3.One * 0.5f;
            child.UpdateWorldMatrix();
            child.SetWorld(Vector3.Zero, Quaternion.Identity);
            child.UpdateWorldMatrix();


            AssertNearEqual(Vector3.UnitZ * 4f, child.Position); // Validate our assumptions
            AssertNearEqual(Quaternion.CreateRotationY(MathF.PI * 0.5f), child.Rotation);
            
            child.GetWorldTransformation(out var pos, out var rot, out _); // Validate transformations
            AssertNearEqual(Vector3.Zero, pos);
            AssertNearEqual(Quaternion.Identity, rot);
        }

        [Fact]
        public void TestSetWorldTransformationNested()
        {
            var scene = new Scene();
            var parent = new Entity { Scene = scene }.Transform;
            var child = new Entity { Transform = { Parent = parent } }.Transform;
            var targetPosition = new Vector3(2, 2, 2);
            var targetRotation = Quaternion.CreateRotationY(MathF.PI * 0.1f);

            parent.Position = new Vector3(1, 1, 1);
            parent.Rotation = Quaternion.CreateRotationX(MathF.PI * 0.05f);

            child.UpdateWorldMatrix();
            child.SetWorld(targetPosition, targetRotation);
            child.UpdateWorldMatrix();

            child.GetWorldTransformation(out var position, out var rotation, out _);
            AssertNearEqual(targetPosition, position);
            AssertNearEqual(targetRotation, rotation);
        }

        // The transformations lose precision, as all floating point operations: the values are compared with a tolerance
        private static void AssertNearEqual(Vector3 expected, Vector3 actual) => Assert.Equal(expected, actual, (a, b) => a.NearEquals(b));

        private static void AssertNearEqual(Quaternion expected, Quaternion actual) => Assert.Equal(expected, actual, (a, b) => a.NearEquals(b));
    }
}
