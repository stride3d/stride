// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Lights;
using Stride.Rendering.Shadows;
using Xunit;
using System.Numerics;

namespace Stride.Engine.Tests;

/// <summary>
/// The cascades of a directional shadow fit the camera frustum the same way with a standard and with a reversed depth projection.
/// </summary>
public class DirectionalShadowCascadeFitTests : IDisposable
{
    private const float Near = 0.1f;
    private const float Far = 100.0f;
    private const int AtlasSize = 2048;
    private const int ShadowSize = 1024;

    private readonly GraphicsDevice graphicsDevice = GraphicsDevice.New(DeviceCreationFlags.Debug);
    private readonly RenderContext context;
    private readonly Texture atlasTexture;

    public DirectionalShadowCascadeFitTests()
    {
        var services = new ServiceRegistry();
        services.AddService<IGraphicsDeviceService>(new GraphicsDeviceServiceLocal(graphicsDevice));
        services.AddService(new EffectSystem(services));
        services.AddService(new GraphicsContext(graphicsDevice));
        context = RenderContext.GetShared(services);
        context.RenderSystem = new RenderSystem();
        atlasTexture = Texture.New2D(graphicsDevice, AtlasSize, AtlasSize, PixelFormat.D32_Float, TextureFlags.DepthStencil | TextureFlags.ShaderResource);
    }

    public void Dispose()
    {
        atlasTexture.Dispose();
        graphicsDevice.Dispose();
    }

    [Theory]
    [InlineData(LightShadowMapStabilizationMode.None, false)]
    [InlineData(LightShadowMapStabilizationMode.ViewSnapping, false)]
    [InlineData(LightShadowMapStabilizationMode.ProjectionSnapping, false)]
    [InlineData(LightShadowMapStabilizationMode.ProjectionSnapping, true)]
    public void ReversedDepthProjectionGivesTheSameCascades(LightShadowMapStabilizationMode stabilization, bool automaticDepthRange)
    {
        var view = Matrix4x4.CreateLookAt(new Vector3(3, 4, 5), new Vector3(-2, 0.5f, -10), Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathUtil.DegreesToRadians(60), 16.0f / 9.0f, Near, Far);

        // z' = w - z: near maps to 1 and far to 0
        var reverseZ = Matrix4x4.Identity;
        reverseZ.M33 = -1.0f;
        reverseZ.M43 = 1.0f;

        var standard = Fit(view, projection, stabilization, automaticDepthRange);
        var reversed = Fit(view, projection * reverseZ, stabilization, automaticDepthRange);

        Assert.Equal(4, standard.CascadeSplits.Length);
        for (int cascade = 0; cascade < standard.CascadeSplits.Length; ++cascade)
        {
            Assert.Equal(standard.CascadeSplits[cascade], reversed.CascadeSplits[cascade], 1e-4f);
            AssertEqual(standard.WorldToShadowCascadeUV[cascade], reversed.WorldToShadowCascadeUV[cascade], cascade);
        }
    }

    private LightDirectionalShadowMapRenderer.ShaderData Fit(Matrix4x4 view, Matrix4x4 projection, LightShadowMapStabilizationMode stabilization, bool automaticDepthRange)
    {
        var shadow = new LightDirectionalShadowMap
        {
            Enabled = true,
            Size = LightShadowMapSize.Medium,
            CascadeCount = LightShadowMapCascadeCount.FourCascades,
            StabilizationMode = stabilization,
        };
        shadow.DepthRange.IsAutomatic = automaticDepthRange;
        shadow.DepthRange.ManualMinDistance = 0.0f;
        shadow.DepthRange.ManualMaxDistance = 60.0f;
        var light = new LightDirectional { Shadow = shadow };

        var renderLight = new RenderLight { Type = light, Direction = Vector3.NormalizeSafe(new Vector3(0.3f, -1.0f, 0.5f)) };

        var sourceView = new RenderView
        {
            View = view,
            Projection = projection,
            ViewProjection = view * projection,
            NearClipPlane = Near,
            FarClipPlane = Far,
            MinimumDistance = 1.0f,
            MaximumDistance = 40.0f,
        };

        var renderer = new LightDirectionalShadowMapRenderer { ShadowCasterRenderStage = new RenderStage("ShadowMapCaster", "ShadowMapCaster") };
        var shadowMap = renderer.CreateShadowMapTexture(sourceView, renderLight, light, ShadowSize);
        shadowMap.Atlas = new ShadowMapAtlasTexture(atlasTexture, 0);
        for (int cascade = 0; cascade < shadowMap.CascadeCount; ++cascade)
            shadowMap.SetRectangle(cascade, new Rectangle((cascade % 2) * ShadowSize, (cascade / 2) * ShadowSize, ShadowSize, ShadowSize));

        renderer.Collect(context, sourceView, shadowMap);
        return (LightDirectionalShadowMapRenderer.ShaderData)shadowMap.ShaderData;
    }

    private static void AssertEqual(Matrix4x4 expected, Matrix4x4 actual, int cascade)
    {
        for (int i = 0; i < 16; ++i)
            Assert.True(Math.Abs(expected[i / 4, i % 4] - actual[i / 4, i % 4]) <= 1e-3f * Math.Max(1.0f, Math.Abs(expected[i / 4, i % 4])),
                $"Cascade {cascade}, WorldToShadowCascadeUV element {i}: expected {expected[i / 4, i % 4]}, got {actual[i / 4, i % 4]}");
    }
}
