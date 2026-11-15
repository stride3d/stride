// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using Stride.Core.Diagnostics;
using Stride.Importer.ThreeD;
using Xunit;

namespace Stride.Assets.Tests;

public class TestModelMaterialSlots : IDisposable
{
    // Material A has one triangle and material B has two, which tells their meshes apart
    private const string Obj = """
        mtllib materials.mtl
        v 0 0 0
        v 1 0 0
        v 0 1 0
        v 1 1 0
        v 2 0 0
        v 2 1 0
        usemtl A
        f 1 2 3
        usemtl B
        f 2 4 3
        f 2 5 6
        """;
    private const string DifferentMaterials = "newmtl A\nKd 1 0 0\nnewmtl B\nKd 0 1 0\n";
    private const string SameMaterials = "newmtl A\nKd 1 0 0\nnewmtl B\nKd 1 0 0\n";

    private readonly string folder = Directory.CreateTempSubdirectory("stride-material-slots-").FullName;

    public void Dispose() => Directory.Delete(folder, true);

    private (int A, int B) Import(string materials, bool deduplicate, string[] slots, Logger logger = null)
    {
        var path = Path.Combine(folder, "model.obj");
        File.WriteAllText(path, Obj);
        File.WriteAllText(Path.Combine(folder, "materials.mtl"), materials);

        var meshes = new MeshConverter(logger).Convert(path, path, deduplicate, slots).Meshes;
        return (meshes.Single(x => x.Draw.DrawCount == 3).MaterialIndex, meshes.Single(x => x.Draw.DrawCount == 6).MaterialIndex);
    }

    [Theory]
    [InlineData(new[] { "A", "B" }, 0, 1)]
    [InlineData(new[] { "B", "A" }, 1, 0)]
    [InlineData(new[] { "Removed", "B", "A" }, 2, 1)]
    public void SlotsAreFoundByName(string[] slots, int a, int b)
    {
        Assert.Equal((a, b), Import(DifferentMaterials, false, slots));
        Assert.Equal((a, b), Import(DifferentMaterials, true, slots));
    }

    [Fact]
    public void MergedMaterialKeepsTheSlotOfItsName()
    {
        // An asset can list both slots, each with its own material
        Assert.Equal((1, 0), Import(SameMaterials, true, ["B", "A"]));
    }

    [Fact]
    public void MergedMaterialWithoutSlotUsesTheSlotOfTheMaterialThatRemains()
    {
        Assert.Equal((0, 0), Import(SameMaterials, true, ["A"]));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WithoutAnyNameSlotsGoByPosition(bool deduplicate)
    {
        var logger = new LoggerResult();

        var result = Import(DifferentMaterials, deduplicate, ["namespace_A", "namespace_B", "namespace_C"], logger);

        Assert.Equal(Import(DifferentMaterials, deduplicate, null), result);
        Assert.False(logger.HasErrors);
        Assert.DoesNotContain(logger.Messages, x => x.Type == LogMessageType.Warning);
    }

    [Fact]
    public void MaterialWithoutSlotWarnsWhenOthersHaveOne()
    {
        var logger = new LoggerResult();

        var (a, b) = Import(DifferentMaterials, false, ["B", "A2"], logger);

        Assert.Equal(0, b);
        Assert.Equal(Import(DifferentMaterials, false, null).A, a);
        var warning = Assert.Single(logger.Messages, x => x.Type == LogMessageType.Warning);
        Assert.Contains("A", warning.Text);
    }

    [Fact]
    public void SlotWithoutNameIsIgnored()
    {
        Assert.Equal((1, 2), Import(DifferentMaterials, false, [null, "A", "B"]));
    }

    [Fact]
    public void WithoutSlotsIndicesReferToTheFile()
    {
        var all = Import(SameMaterials, false, null);
        Assert.NotEqual(all.A, all.B);

        var deduplicated = Import(SameMaterials, true, null);
        Assert.Equal(deduplicated.A, deduplicated.B);
    }
}
