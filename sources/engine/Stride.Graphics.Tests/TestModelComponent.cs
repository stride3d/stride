// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.ProceduralModels;
using Xunit;

namespace Stride.Graphics.Tests
{
    public class TestModelComponent : GraphicTestGameBase
    {
        [Fact]
        public static void TestMutateModel()
        {
            var game = new TestModelComponent();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;
                
                await game.Script.NextFrame();

                var model = new CubeProceduralModel().Generate(game.Services);
                model.Materials.Add(Material.New(game.GraphicsDevice, new MaterialDescriptor
                {
                    Attributes =
                    {
                        Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(Color.White)),
                        DiffuseModel = new MaterialDiffuseLambertModelFeature()
                    }
                }));
                var entity = new Entity { new ModelComponent { Model = model } };

                game.SceneSystem.SceneInstance.RootScene.Entities.Add(entity);

                // Let the engine cache this model by waiting for the next frame
                await game.Script.NextFrame();

                // Now let's mutate this model to ensure that the engine keeps up ...
                
                // Adding a second mesh

                model.Meshes.Add(new CubeProceduralModel().Generate(game.Services).Meshes[0]);
                model.Materials.Add(model.Materials[0]);

                await game.Script.NextFrame();
                
                // Removing a mesh

                model.Meshes.RemoveAt(1);
                model.Materials.RemoveAt(1);

                await game.Script.NextFrame();

                // Changing a meshes' draw
                
                MakeInvalid(model.Meshes[0].Draw);
                model.Meshes[0].Draw = new CubeProceduralModel().Generate(game.Services).Meshes[0].Draw;

                await game.Script.NextFrame();

                // Swapping the whole mesh

                MakeInvalid(model.Meshes[0].Draw);
                model.Meshes[0].MaterialIndex = Int32.MaxValue;
                model.Meshes[0].NodeIndex = Int32.MaxValue;
                model.Meshes[0] = new CubeProceduralModel().Generate(game.Services).Meshes[0];

                await game.Script.NextFrame();

                game.Exit();
            });
            RunGameTest(game);

            static void MakeInvalid(MeshDraw draw)
            {
                // Setting invalid data to ensure that anything that still uses a cached version of this draw would throw
                draw.VertexBuffers[0].Buffer.Dispose();
                draw.VertexBuffers = null;
                draw.IndexBuffer.Buffer.Dispose();
                draw.IndexBuffer = null;
                draw.DrawCount = Int32.MaxValue;
                draw.PrimitiveType = (PrimitiveType)Int32.MaxValue;
            }
        }
    }
}
