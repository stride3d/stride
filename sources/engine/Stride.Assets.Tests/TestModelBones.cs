// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.Data;
using Stride.Importer.ThreeD;
using Stride.Rendering;
using Xunit;

namespace Stride.Assets.Tests;

/// <summary>
/// Skinned meshes must sit at their bind pose when the skeleton is at its bind pose. The expected positions come from the scene the FBX
/// describes (the FBX SDK's skinning: vertex, then geometric transform, then mesh at bind), not from the importer's formula.
/// </summary>
public class TestModelBones : IDisposable
{
    // Two parts, each skinned to one bone only, so a wrong bone shows on its own part
    private static readonly Vector3[] Vertices =
    [
        new(0, 0, 0), new(40, 0, 0), new(0, 40, 0), new(40, 40, 10),
        new(0, 0, 80), new(40, 0, 80), new(0, 40, 90), new(40, 40, 90),
    ];
    private static readonly int[] Polygons = [0, 1, -3, 1, 3, -3, 4, 5, -7, 5, 7, -7];

    private readonly string folder = Directory.CreateTempSubdirectory("stride-bones-").FullName;

    public void Dispose() => Directory.Delete(folder, true);

    public record Placement(Vector3 Translation, Vector3 Rotation, Vector3 Scaling)
    {
        public static readonly Placement Identity = new(Vector3.Zero, Vector3.Zero, Vector3.One);

        // FBX local transform with no pivots, rotation order XYZ, in row-vector order
        public Matrix Matrix => Matrix.Scaling(Scaling)
            * Matrix.RotationX(MathUtil.DegreesToRadians(Rotation.X)) * Matrix.RotationY(MathUtil.DegreesToRadians(Rotation.Y)) * Matrix.RotationZ(MathUtil.DegreesToRadians(Rotation.Z))
            * Matrix.Translation(Translation);
    }

    public record Scene
    {
        public Placement MeshAtBind { get; init; } = new(new Vector3(10, 20, 30), new Vector3(0, 30, 0), Vector3.One);
        // The mesh's node as saved, which an artist may have moved after binding
        public Placement MeshNow { get; init; }
        public Placement Geometric { get; init; } = Placement.Identity;
        public Placement Root { get; init; } = new(new Vector3(5, 50, 0), new Vector3(0, 0, 20), Vector3.One);
        public Placement Child { get; init; } = new(new Vector3(0, 40, 0), new Vector3(10, 0, 0), Vector3.One);
        public bool TwoMaterials { get; init; }
        public double UnitScaleFactor { get; init; } = 1;
        // A second model that uses the same skinned geometry
        public Placement SecondInstance { get; init; }
        // FBX rotation order of the mesh's model, 0 being XYZ
        public int RotationOrder { get; init; }

        public Matrix RootBind => Root.Matrix;
        public Matrix ChildBind => Child.Matrix * Root.Matrix;
    }

    public static TheoryData<string, Scene> Scenes => new()
    {
        { "nothing special", new Scene() },
        { "mesh moved after binding", new Scene { MeshNow = new(new Vector3(-50, 0, 10), new Vector3(180, 0, 0), new Vector3(-1, 1, 1)) } },
        { "shear from a parent's non-uniform scale", new Scene { Root = new(new Vector3(5, 50, 0), new Vector3(0, 0, 20), new Vector3(1, 2, 1)), Child = new(new Vector3(0, 40, 0), new Vector3(0, 0, 30), Vector3.One) } },
        { "geometric transform", new Scene { Geometric = new(new Vector3(7, -3, 12), new Vector3(0, 0, 45), new Vector3(1, 1, 2)) } },
        { "geometric transform on a moved mesh", new Scene { Geometric = new(new Vector3(7, -3, 12), Vector3.Zero, Vector3.One), MeshNow = new(new Vector3(-50, 0, 10), Vector3.Zero, Vector3.One) } },
        { "two materials on a moved mesh", new Scene { TwoMaterials = true, MeshNow = new(new Vector3(-50, 0, 10), new Vector3(0, 90, 0), Vector3.One) } },
        { "file in meters", new Scene { UnitScaleFactor = 100 } },
        { "geometry shared by two models", new Scene { SecondInstance = new(new Vector3(-50, 0, 10), new Vector3(0, 90, 0), Vector3.One) } },
    };

    [Theory]
    [MemberData(nameof(Scenes))]
    public void SkinnedMeshSitsAtItsBindPose(string name, Scene scene)
    {
        var path = Path.Combine(folder, "model.fbx");
        File.WriteAllText(path, Fbx(scene));

        var model = new MeshConverter(null).Convert(path, path, false);
        var skeleton = new MeshConverter(null).ConvertSkeleton(path, path);
        var updater = new SkeletonUpdater(skeleton);
        updater.UpdateMatrices();

        // The file's units, as the importer applied them, from the root bone's position
        updater.GetWorldMatrix(Array.FindIndex(skeleton.Nodes, x => x.Name == "root"), out var root);
        var scale = root.TranslationVector.Length() / scene.RootBind.TranslationVector.Length();

        var expected = Vertices.Select(x => Vector3.TransformCoordinate(x, scene.Geometric.Matrix * scene.MeshAtBind.Matrix) * scale).ToList();
        var actual = model.Meshes.SelectMany(x => Skin(x, updater)).ToList();

        Assert.Equal((scene.TwoMaterials ? 2 : 1) * (scene.SecondInstance != null ? 2 : 1), model.Meshes.Count);
        AssertSamePoints(expected, actual, 1e-3f * Math.Max(1, scale * 100), name);
    }

    /// <summary>
    /// The importer turns a geometric rotation in the model's rotation order into the mesh's node. Skinning must place the geometry the same way.
    /// </summary>
    [Fact]
    public void SkinnedGeometricRotationFollowsTheRotationOrder()
    {
        var scene = new Scene
        {
            // Only the geometric rotation depends on the order
            MeshAtBind = new(new Vector3(10, 20, 30), Vector3.Zero, Vector3.One),
            Geometric = new(Vector3.Zero, new Vector3(20, 30, 40), Vector3.One),
            RotationOrder = 5,
        };
        var path = Path.Combine(folder, "model.fbx");
        File.WriteAllText(path, Fbx(scene));

        var mesh = Assert.Single(new MeshConverter(null).Convert(path, path, false).Meshes);
        var skeleton = new MeshConverter(null).ConvertSkeleton(path, path);
        var updater = new SkeletonUpdater(skeleton);
        updater.UpdateMatrices();
        updater.GetWorldMatrix(mesh.NodeIndex, out var meshWorld);

        var expected = Vertices.Select(x => Vector3.TransformCoordinate(x * 0.01f, meshWorld)).ToList();
        AssertSamePoints(expected, Skin(mesh, updater), 1e-3f, "rotation order ZYX");
    }

    public static TheoryData<string> RealFiles => new() { "knight.fbx", "goblin.fbx", "mannequinModel.fbx" };

    /// <summary>
    /// Each bone's offset is what the file recorded: the mesh's geometric transform, then the cluster's Transform.
    /// Files exported by Maya and 3ds Max, read with a reader of the binary format of our own.
    /// </summary>
    [Theory]
    [MemberData(nameof(RealFiles))]
    public void RealFileBoneOffsetsAreTheOnesTheFileRecorded(string file)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "app_data", "Stride.Assets.Tests", "bones", file);
        var recorded = BinaryFbx.ReadClusters(path, out var root);

        var model = new MeshConverter(null).Convert(path, path, false);
        var skeleton = new MeshConverter(null).ConvertSkeleton(path, path);

        var checkedBones = 0;
        foreach (var mesh in model.Meshes.Where(x => x.Skinning != null))
        {
            var meshName = skeleton.Nodes[mesh.NodeIndex].Name;
            foreach (var bone in mesh.Skinning.Bones)
            {
                var boneName = skeleton.Nodes[bone.NodeIndex].Name;
                Assert.True(recorded.TryGetValue((meshName, boneName), out var expected), $"{file}: no cluster in the file for mesh {meshName}, bone {boneName}");
                // The importer's units, then its scene root, which the importer bakes into the offsets
                expected.TranslationVector *= 0.01f;
                expected = Matrix.Invert(root) * expected * root;
                var actual = bone.LinkToMeshMatrix;
                for (var i = 0; i < 16; i++)
                    Assert.True(Math.Abs(actual[i] - expected[i]) < 1e-3f, $"{file}, {meshName}, bone {boneName}: {actual} instead of {expected}");
                checkedBones++;
            }
        }
        Assert.True(checkedBones > 0);
    }

    private static List<Vector3> Skin(Mesh mesh, SkeletonUpdater updater)
    {
        var binding = mesh.Draw.VertexBuffers[0];
        var data = binding.Buffer.GetSerializationData().Content;
        var elements = binding.Declaration.VertexElements;
        var position = elements.First(x => x.SemanticName == "POSITION").AlignedByteOffset;
        var weights = elements.First(x => x.SemanticName == "BLENDWEIGHT").AlignedByteOffset;
        var indices = elements.First(x => x.SemanticName == "BLENDINDICES");
        var bones = mesh.Skinning.Bones.Select(x =>
        {
            updater.GetWorldMatrix(x.NodeIndex, out var world);
            return x.LinkToMeshMatrix * world;
        }).ToArray();

        var result = new List<Vector3>();
        for (var v = 0; v < binding.Count; v++)
        {
            var start = binding.Offset + v * binding.Stride;
            var p = new Vector3(BitConverter.ToSingle(data, start + position), BitConverter.ToSingle(data, start + position + 4), BitConverter.ToSingle(data, start + position + 8));
            var skinned = Vector3.Zero;
            for (var w = 0; w < 4; w++)
            {
                var weight = BitConverter.ToSingle(data, start + weights + 4 * w);
                if (weight <= 0)
                    continue;
                var index = indices.Format == PixelFormat.R16G16B16A16_UInt
                    ? BitConverter.ToUInt16(data, start + indices.AlignedByteOffset + 2 * w)
                    : data[start + indices.AlignedByteOffset + w];
                skinned += weight * Vector3.TransformCoordinate(p, bones[index]);
            }
            result.Add(skinned);
        }
        return result;
    }

    private static void AssertSamePoints(List<Vector3> expected, List<Vector3> actual, float tolerance, string name)
    {
        foreach (var point in actual)
        {
            var nearest = expected.Min(x => (x - point).Length());
            Assert.True(nearest < tolerance, $"{name}: skinned vertex {point} is {nearest} away from the bind pose");
        }
        foreach (var point in expected)
            Assert.True(actual.Any(x => (x - point).Length() < tolerance), $"{name}: no skinned vertex at {point}");
    }

    private static string Fbx(Scene scene)
    {
        var text = new StringBuilder();
        void Line(string line) => text.Append(line).Append('\n');
        string Numbers(IEnumerable<float> values) => string.Join(",", values.Select(x => x.ToString("R", CultureInfo.InvariantCulture)));
        string Matrix(Matrix m) => Numbers(Enumerable.Range(0, 16).Select(i => m[i]));
        string Vector(Vector3 v) => Numbers([v.X, v.Y, v.Z]);
        void Placement(Placement p)
        {
            Line($"P: \"Lcl Translation\", \"Lcl Translation\", \"\", \"A\",{Vector(p.Translation)}");
            Line($"P: \"Lcl Rotation\", \"Lcl Rotation\", \"\", \"A\",{Vector(p.Rotation)}");
            Line($"P: \"Lcl Scaling\", \"Lcl Scaling\", \"\", \"A\",{Vector(p.Scaling)}");
        }
        void Cluster(long id, int[] indices, Matrix bind)
        {
            Line($"Deformer: {id}, \"SubDeformer::\", \"Cluster\" {{");
            Line("Version: 100");
            Line($"Indexes: *{indices.Length} {{ a: {string.Join(",", indices)} }}");
            Line($"Weights: *{indices.Length} {{ a: {string.Join(",", indices.Select(_ => "1"))} }}");
            // As the FBX SDK writes it: the mesh at bind, then into the bone's space at bind
            Line($"Transform: *16 {{ a: {Matrix(scene.MeshAtBind.Matrix * Stride.Core.Mathematics.Matrix.Invert(bind))} }}");
            Line($"TransformLink: *16 {{ a: {Matrix(bind)} }}");
            Line("}");
        }

        Line("; FBX 7.4.0 project file");
        Line("FBXHeaderExtension: { FBXHeaderVersion: 1003 FBXVersion: 7400 }");
        Line("GlobalSettings: { Version: 1000 Properties70: {");
        Line($"P: \"UnitScaleFactor\", \"double\", \"Number\", \"\",{scene.UnitScaleFactor.ToString("R", CultureInfo.InvariantCulture)}");
        Line("} }");
        Line("Objects: {");
        Line("Geometry: 10, \"Geometry::skin\", \"Mesh\" {");
        Line($"Vertices: *{Vertices.Length * 3} {{ a: {Numbers(Vertices.SelectMany(x => new[] { x.X, x.Y, x.Z }))} }}");
        Line($"PolygonVertexIndex: *{Polygons.Length} {{ a: {string.Join(",", Polygons)} }}");
        Line("GeometryVersion: 124");
        if (scene.TwoMaterials)
        {
            Line("LayerElementMaterial: 0 { Version: 101 Name: \"\" MappingInformationType: \"ByPolygon\" ReferenceInformationType: \"IndexToDirect\" Materials: *4 { a: 0,0,1,1 } }");
            Line("Layer: 0 { Version: 100 LayerElement: { Type: \"LayerElementMaterial\" TypedIndex: 0 } }");
        }
        Line("}");
        Line("Model: 20, \"Model::skin\", \"Mesh\" { Version: 232 Properties70: {");
        Placement(scene.MeshNow ?? scene.MeshAtBind);
        Line($"P: \"GeometricTranslation\", \"Vector3D\", \"Vector\", \"\",{Vector(scene.Geometric.Translation)}");
        Line($"P: \"GeometricRotation\", \"Vector3D\", \"Vector\", \"\",{Vector(scene.Geometric.Rotation)}");
        Line($"P: \"GeometricScaling\", \"Vector3D\", \"Vector\", \"\",{Vector(scene.Geometric.Scaling)}");
        Line($"P: \"RotationOrder\", \"enum\", \"\", \"\",{scene.RotationOrder}");
        Line("} }");
        if (scene.SecondInstance != null)
        {
            Line("Model: 21, \"Model::copy\", \"Mesh\" { Version: 232 Properties70: {");
            Placement(scene.SecondInstance);
            Line("} }");
        }
        Line("Model: 30, \"Model::root\", \"LimbNode\" { Version: 232 Properties70: {");
        Placement(scene.Root);
        Line("} }");
        Line("Model: 31, \"Model::child\", \"LimbNode\" { Version: 232 Properties70: {");
        Placement(scene.Child);
        Line("} }");
        Line("Material: 40, \"Material::first\", \"\" { Version: 102 }");
        Line("Material: 41, \"Material::second\", \"\" { Version: 102 }");
        Line("Deformer: 50, \"Deformer::\", \"Skin\" { Version: 101 Link_DeformAcuracy: 50 }");
        Cluster(51, [0, 1, 2, 3], scene.RootBind);
        Cluster(52, [4, 5, 6, 7], scene.ChildBind);
        Line("}");
        Line("Connections: {");
        foreach (var (child, parent) in new[] { (20, 0), (10, 20), (30, 0), (31, 30), (40, 20), (41, 20), (50, 10), (51, 50), (52, 50), (30, 51), (31, 52) })
            Line($"C: \"OO\",{child},{parent}");
        if (scene.SecondInstance != null)
        {
            Line("C: \"OO\",21,0");
            Line("C: \"OO\",10,21");
            Line("C: \"OO\",40,21");
        }
        Line("}");
        return text.ToString();
    }

    /// <summary>
    /// The skin clusters of a binary FBX file, keyed by mesh and bone names as the importer cleans them.
    /// </summary>
    private static class BinaryFbx
    {
        private record Node(string Name, List<object> Properties, List<Node> Children);

        /// <param name="root">The scene root Assimp makes from the file's axes and units, in row-vector order.</param>
        public static Dictionary<(string Mesh, string Bone), Matrix> ReadClusters(string path, out Matrix root)
        {
            var data = File.ReadAllBytes(path);
            Assert.Equal("Kaydara FBX Binary  ", Encoding.ASCII.GetString(data, 0, 20));
            var wide = BitConverter.ToUInt32(data, 23) >= 7500;
            var position = 27;
            var roots = new List<Node>();
            while (ReadNode(data, ref position, wide) is { } node)
                roots.Add(node);

            // As Assimp's correctRootTransform: its rows are the right, up and front axes, scaled by the file's unit
            var settings = new Dictionary<string, double> { ["UpAxis"] = 1, ["UpAxisSign"] = 1, ["FrontAxis"] = 2, ["FrontAxisSign"] = 1, ["CoordAxis"] = 0, ["CoordAxisSign"] = 1, ["UnitScaleFactor"] = 1 };
            foreach (var property in roots.First(x => x.Name == "GlobalSettings").Children.First(x => x.Name == "Properties70").Children)
            {
                if (settings.ContainsKey((string)property.Properties[0]))
                    settings[(string)property.Properties[0]] = Convert.ToDouble(property.Properties[4], CultureInfo.InvariantCulture);
            }
            var unit = (float)settings["UnitScaleFactor"];
            var axes = new Matrix { M44 = 1 };
            axes[0, (int)settings["CoordAxis"]] = (float)settings["CoordAxisSign"] * unit;
            axes[1, (int)settings["UpAxis"]] = (float)settings["UpAxisSign"] * unit;
            axes[2, (int)settings["FrontAxis"]] = (float)settings["FrontAxisSign"] * unit;
            // Assimp's matrices act on column vectors, Stride's on row vectors
            axes.Transpose();
            root = axes;

            var models = new Dictionary<long, (string Name, Matrix Geometric)>();
            var clusters = new Dictionary<long, Matrix>();
            foreach (var item in roots.First(x => x.Name == "Objects").Children)
            {
                var id = (long)item.Properties[0];
                if (item.Name == "Model")
                {
                    var name = ((string)item.Properties[1]).Split('\0')[0];
                    var properties = item.Children.FirstOrDefault(x => x.Name == "Properties70")?.Children ?? [];
                    Vector3 Get(string key, Vector3 fallback) => properties.FirstOrDefault(x => (string)x.Properties[0] == key) is { } p
                        ? new Vector3(Convert.ToSingle(p.Properties[4]), Convert.ToSingle(p.Properties[5]), Convert.ToSingle(p.Properties[6]))
                        : fallback;
                    // Placement composes rotations in the order XYZ, the default
                    Assert.DoesNotContain(properties, x => (string)x.Properties[0] == "RotationOrder" && Convert.ToInt32(x.Properties[4]) != 0);
                    var geometric = new Placement(Get("GeometricTranslation", Vector3.Zero), Get("GeometricRotation", Vector3.Zero), Get("GeometricScaling", Vector3.One));
                    models[id] = (name.CleanNodeName(), geometric.Matrix);
                }
                else if (item.Name == "Deformer" && (string)item.Properties[2] == "Cluster")
                {
                    var values = (double[])item.Children.First(x => x.Name == "Transform").Properties[0];
                    var matrix = new Matrix();
                    for (var i = 0; i < 16; i++)
                        matrix[i] = (float)values[i];
                    clusters[id] = matrix;
                }
            }

            var links = roots.First(x => x.Name == "Connections").Children
                .Where(x => (string)x.Properties[0] == "OO")
                .Select(x => (Child: (long)x.Properties[1], Parent: (long)x.Properties[2]))
                .ToList();

            // Bone to cluster, cluster to skin, skin to geometry, geometry to the models that use it
            var result = new Dictionary<(string, string), Matrix>();
            foreach (var (bone, cluster) in links.Where(x => models.ContainsKey(x.Child) && clusters.ContainsKey(x.Parent)))
            {
                foreach (var skin in links.Where(x => x.Child == cluster).Select(x => x.Parent))
                foreach (var geometry in links.Where(x => x.Child == skin).Select(x => x.Parent))
                foreach (var mesh in links.Where(x => x.Child == geometry && models.ContainsKey(x.Parent)).Select(x => x.Parent))
                    result[(models[mesh].Name, models[bone].Name)] = models[mesh].Geometric * clusters[cluster];
            }
            return result;
        }

        private static Node ReadNode(byte[] data, ref int position, bool wide)
        {
            var end = wide ? (long)BitConverter.ToUInt64(data, position) : BitConverter.ToUInt32(data, position);
            var count = wide ? (long)BitConverter.ToUInt64(data, position + 8) : BitConverter.ToUInt32(data, position + 4);
            position += wide ? 24 : 12;
            if (end == 0)
            {
                position += 1;
                return null;
            }
            var name = Encoding.ASCII.GetString(data, position + 1, data[position]);
            position += 1 + data[position];
            var properties = new List<object>();
            for (var i = 0; i < count; i++)
                properties.Add(ReadProperty(data, ref position));
            var children = new List<Node>();
            while (position < end)
            {
                if (ReadNode(data, ref position, wide) is { } child)
                    children.Add(child);
            }
            return new Node(name, properties, children);
        }

        private static object ReadProperty(byte[] data, ref int position)
        {
            var type = (char)data[position++];
            switch (type)
            {
                case 'Y': position += 2; return BitConverter.ToInt16(data, position - 2);
                case 'C': position += 1; return data[position - 1];
                case 'I': position += 4; return BitConverter.ToInt32(data, position - 4);
                case 'F': position += 4; return BitConverter.ToSingle(data, position - 4);
                case 'D': position += 8; return BitConverter.ToDouble(data, position - 8);
                case 'L': position += 8; return BitConverter.ToInt64(data, position - 8);
                case 'S':
                case 'R':
                {
                    var length = BitConverter.ToInt32(data, position);
                    position += 4 + length;
                    return type == 'S' ? Encoding.UTF8.GetString(data, position - length, length) : data[(position - length)..position];
                }
                default:
                {
                    var count = BitConverter.ToInt32(data, position);
                    var compressed = BitConverter.ToInt32(data, position + 4) == 1;
                    var length = BitConverter.ToInt32(data, position + 8);
                    var raw = data[(position + 12)..(position + 12 + length)];
                    position += 12 + length;
                    if (compressed)
                    {
                        using var stream = new System.IO.Compression.ZLibStream(new MemoryStream(raw), System.IO.Compression.CompressionMode.Decompress);
                        using var decompressed = new MemoryStream();
                        stream.CopyTo(decompressed);
                        raw = decompressed.ToArray();
                    }
                    return type switch
                    {
                        'd' => Enumerable.Range(0, count).Select(i => BitConverter.ToDouble(raw, i * 8)).ToArray(),
                        'f' => Enumerable.Range(0, count).Select(i => BitConverter.ToSingle(raw, i * 4)).ToArray(),
                        'l' => Enumerable.Range(0, count).Select(i => BitConverter.ToInt64(raw, i * 8)).ToArray(),
                        'i' => Enumerable.Range(0, count).Select(i => BitConverter.ToInt32(raw, i * 4)).ToArray(),
                        'b' or 'c' => raw,
                        _ => throw new InvalidDataException($"Unknown FBX property type {type}"),
                    };
                }
            }
        }
    }
}
