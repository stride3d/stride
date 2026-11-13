// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Stride.BepuPhysics.Soft.Rendering;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Graphics.Regression;
using Stride.Graphics.Semantics;
using Stride.Rendering;
using Stride.Rendering.ProceduralModels;
using Xunit;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.BepuPhysics.Soft.Tests;

/// <summary>
/// Soft bodies deform whatever vertex layout a model comes with: the procedural models have four float tangents, the imported
/// ones three float tangents or none, and no normals when their source file has none.
/// </summary>
public class VertexLayoutTests : GameTestBase
{
    internal const PixelFormat Float3 = PixelFormat.R32G32B32_Float;
    internal const PixelFormat Float4 = PixelFormat.R32G32B32A32_Float;
    internal const PixelFormat Absent = PixelFormat.None;

    private static readonly Color Marker = new(0x12, 0x34, 0x56, 0x78);

    [Theory]
    [InlineData(Float3, Float3, Float4)] // Procedural models
    [InlineData(Float3, Float3, Float3)] // Imported from a file with tangents
    [InlineData(Float3, Float3, Absent)] // Imported from a file without tangents
    [InlineData(Float3, Absent, Absent)] // Imported from a file without normals
    [InlineData(Float4, Float4, Float4)]
    public static void DeformationOnlyWritesWhatTheMeshDeclares(PixelFormat position, PixelFormat normal, PixelFormat tangent)
    {
        SoftBodyTests.RunScene(async scene =>
        {
            // What is checked is what the CPU writes in the vertices
            scene.Game.Services.GetOrCreate<SoftBodyConfiguration>().DeformationMode = SoftBodyDeformationMode.Cpu;
            var cube = WithLayout(scene.Game, new CubeProceduralModel(), position, normal, tangent, out var cubeSource);
            var jelly = new VolumetricSoftBodyComponent { Resolution = 3 };
            scene.Add(jelly, cube, new Vector3(-2f, 1f, 0f));

            // Pinned along the edge at x = -0.5, it swings down
            var plane = new PlaneProceduralModel { Size = new Vector2(1f), Tessellation = new Int2(6), Normal = NormalDirection.UpY, GenerateBackFace = true };
            var edge = new BoundingBox(new Vector3(-0.51f, -1f, -1f), new Vector3(-0.49f, 1f, 1f));
            var cloth = new ClothComponent { Pins = { new SoftBodyPin { Region = edge } } };
            scene.Add(cloth, WithLayout(scene.Game, plane, position, normal, tangent, out var planeSource), new Vector3(2f, 2f, 0f));

            await SoftBodyTests.Seconds(jelly, 1f);

            Check(jelly, cubeSource);
            Check(cloth, planeSource);
        });
    }

    [Theory]
    [InlineData(Float3, PixelFormat.R16G16B16A16_Float, Float4, "NORMAL")]
    [InlineData(Float3, Float3, PixelFormat.R16G16B16A16_Float, "TANGENT")]
    public static void LayoutsThatCannotBeWrittenAreRefused(PixelFormat position, PixelFormat normal, PixelFormat tangent, string refused)
    {
        SoftBodyTests.RunScene(async scene =>
        {
            scene.Game.Services.GetOrCreate<SoftBodyConfiguration>().DeformationMode = SoftBodyDeformationMode.Cpu;
            var body = new VolumetricSoftBodyComponent { Resolution = 3 };
            scene.Add(body, new CubeProceduralModel(), new Vector3(0f, 1f, 0f));
            await SoftBodyTests.Seconds(body, 0.1f);

            var model = WithLayout(scene.Game, new CubeProceduralModel(), position, normal, tangent, out _);
            var exception = Assert.Throws<NotSupportedException>(() => new DeformableModel(scene.Game.GraphicsDevice, scene.Game.Services, model, body.Topology!, SoftBodyDeformationMode.Cpu, SoftBodyDeformationQuality.Fast));
            Assert.Contains(refused, exception.Message);
        });
    }

    /// <summary> What the vertices of a model were before a soft body deformed them </summary>
    internal sealed record Source(VertexDeclaration Declaration, Vector3[] Positions, Vector2[] TexCoords);

    private static void Check(SoftBodyComponent body, Source source)
    {
        Assert.NotNull(body.RenderModel);
        var bytes = body.RenderModel.VertexBytes(0);
        var declaration = source.Declaration;
        int stride = declaration.VertexStride;
        int count = source.Positions.Length;
        Assert.Equal(count * stride, bytes.Length);

        var offsets = new Dictionary<string, (int Offset, PixelFormat Format)>();
        foreach (var element in declaration.EnumerateWithOffsets())
            offsets[element.VertexElement.SemanticName] = (element.Offset, element.VertexElement.Format);

        float moved = 0f;
        for (int i = 0; i < count; i++)
        {
            var vertex = bytes.Slice(i * stride, stride);

            // What the deformation does not own is as authored
            Assert.Equal(source.TexCoords[i], MemoryMarshal.Read<Vector2>(vertex[offsets[TextureCoordinateSemantic.Name].Offset..]));
            Assert.Equal(Marker, MemoryMarshal.Read<Color>(vertex[offsets[ColorSemantic.Name].Offset..]));

            var (positionOffset, positionFormat) = offsets[PositionSemantic.Name];
            var position = MemoryMarshal.Read<Vector3>(vertex[positionOffset..]);
            Assert.True(float.IsFinite(position.X + position.Y + position.Z), $"Position {position}");
            moved = MathF.Max(moved, Vector3.Distance(position, source.Positions[i]));
            if (positionFormat == Float4)
                Assert.Equal(1f, MemoryMarshal.Read<float>(vertex[(positionOffset + 12)..]));

            Vector3? normal = null;
            if (offsets.TryGetValue(NormalSemantic.Name, out var normalElement))
            {
                normal = MemoryMarshal.Read<Vector3>(vertex[normalElement.Offset..]);
                Assert.Equal(1f, normal.Value.Length(), 3);
            }

            if (offsets.TryGetValue(TangentSemantic.Name, out var tangentElement))
            {
                var tangent = MemoryMarshal.Read<Vector3>(vertex[tangentElement.Offset..]);
                Assert.Equal(1f, tangent.Length(), 3);
                if (normal is { } across)
                    Assert.Equal(0f, Vector3.Dot(across, tangent), 3);

                // The handedness is the mesh's to say
                if (tangentElement.Format == Float4)
                    Assert.Equal(-1f, MemoryMarshal.Read<float>(vertex[(tangentElement.Offset + 12)..]));
            }
        }
        Assert.True(moved > 1e-3f, $"The vertices only moved by {moved}");
    }

    /// <summary> The model of <paramref name="shape"/> with its vertices written in another layout, a color in the middle of it </summary>
    internal static Model WithLayout(Game game, PrimitiveProceduralModelBase shape, PixelFormat position, PixelFormat normal, PixelFormat tangent, out Source source)
    {
        var model = new ProceduralModelDescriptor(shape).GenerateModel(game.Services);
        var draw = model.Meshes[0].Draw;
        draw.VertexBuffers[0].AsReadable(game.Services, out var vertices, out int count);
        var positions = new Vector3[count];
        var normals = new Vector3[count];
        var tangents = new Vector4[count];
        var texCoords = new Vector2[count];
        Assert.True(vertices.Copy<PositionSemantic, Vector3>(positions));
        Assert.True(vertices.Copy<NormalSemantic, Vector3>(normals));
        Assert.True(vertices.Copy<TangentSemantic, Vector4>(tangents));
        Assert.True(vertices.Copy<TextureCoordinateSemantic, Vector2>(texCoords));

        var elements = new List<VertexElement> { new(PositionSemantic.Name, 0, position) };
        if (normal != Absent)
            elements.Add(new VertexElement(NormalSemantic.Name, 0, normal));
        elements.Add(VertexElement.TextureCoordinate<Vector2>());
        elements.Add(VertexElement.Color<Color>());
        if (tangent != Absent)
            elements.Add(new VertexElement(TangentSemantic.Name, 0, tangent));
        var declaration = new VertexDeclaration(elements.ToArray());

        int stride = declaration.VertexStride;
        var bytes = new byte[count * stride];
        foreach (var element in declaration.EnumerateWithOffsets())
        {
            for (int i = 0; i < count; i++)
            {
                var at = bytes.AsSpan(i * stride + element.Offset, element.Size);
                var name = element.VertexElement.SemanticName;
                if (name == TextureCoordinateSemantic.Name)
                    MemoryMarshal.Write(at, in texCoords[i]);
                else if (name == ColorSemantic.Name)
                    MemoryMarshal.Write(at, in Marker);
                else if (name == PositionSemantic.Name)
                    Write(at, element.VertexElement.Format, new Vector4(positions[i], 1f));
                else if (name == NormalSemantic.Name)
                    Write(at, element.VertexElement.Format, new Vector4(normals[i], 0f));
                else
                    Write(at, element.VertexElement.Format, new Vector4((Vector3)tangents[i], -1f));
            }
        }

        var authored = draw.VertexBuffers[0].Buffer;
        draw.VertexBuffers = [new VertexBufferBinding(Buffer.New(game.GraphicsDevice, bytes, BufferFlags.VertexBuffer, GraphicsResourceUsage.Default), declaration, count)];
        authored.Dispose();
        source = new Source(declaration, positions, texCoords);
        return model;
    }

    private static void Write(Span<byte> at, PixelFormat format, Vector4 value)
    {
        switch (format)
        {
            case Float3:
                var xyz = (Vector3)value;
                MemoryMarshal.Write(at, in xyz);
                break;
            case Float4:
                MemoryMarshal.Write(at, in value);
                break;
            case PixelFormat.R16G16B16A16_Float:
                var half = (Half4)value;
                MemoryMarshal.Write(at, in half);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }
    }
}
