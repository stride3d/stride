using System.Runtime.InteropServices;
using DotRecast.Detour;
using DotRecast.Recast;
using DotRecast.Recast.Geom;
using DotRecast.Recast.Toolset;
using DotRecast.Recast.Toolset.Builder;
using DotRecast.Recast.Toolset.Geom;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Navigation.Components;
using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;
using Stride.Graphics;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.Materials;
using Stride.Core.Mathematics;
using Stride.BepuPhysics.Systems;

namespace Stride.BepuPhysics.Navigation.Processors;
public class RecastMeshProcessor : EntityProcessor<BepuNavigationBoundingBoxComponent>
{
    private IGame _game;
    private SceneSystem _sceneSystem;
    private InputManager _input;
    private ContainerProcessor _containerProcessor;
    private ShapeCacheSystem _shapeCache;

    private DtNavMesh? _navMesh;
    private Task<DtNavMesh>? _runningRebuild;

    private CancellationTokenSource _rebuildingTask = new();
    private RcNavMeshBuildSettings _navSettings = new();
    private List<BepuNavigationBoundingBoxComponent> _boundingBoxes = new();

    public RecastMeshProcessor()
    {
        // this is done to ensure that this processor runs after the BepuPhysicsProcessors
        Order = 20000;
    }

    protected override void OnSystemAdd()
    {
        base.OnSystemAdd();
        _game = Services.GetService<IGame>();
        _sceneSystem = Services.GetService<SceneSystem>();
        _input = Services.GetService<InputManager>();
        _containerProcessor = _sceneSystem.SceneInstance.Processors.Get<ContainerProcessor>();
        _shapeCache = Services.GetService<ShapeCacheSystem>();
    }

    protected override void OnEntityComponentAdding(Entity entity, [NotNull] BepuNavigationBoundingBoxComponent component, [NotNull] BepuNavigationBoundingBoxComponent data)
    {
        _boundingBoxes.Add(data);
    }

    protected override void OnEntityComponentRemoved(Entity entity, [NotNull] BepuNavigationBoundingBoxComponent component, [NotNull] BepuNavigationBoundingBoxComponent data)
    {
        _boundingBoxes.Remove(data);
    }

    public override void Update(GameTime time)
    {
        if (_runningRebuild?.Status == TaskStatus.RanToCompletion)
        {
            _navMesh = _runningRebuild.Result;
            _runningRebuild = null;

            List<Vector3> strideVerts = new List<Vector3>();
            for (int i = 0; i < _navMesh.GetTileCount(); i++)
            {
                for (int j = 0; j < _navMesh.GetTile(i).data.verts.Length;)
                {
                    strideVerts.Add(
                        new Vector3(_navMesh.GetTile(i).data.verts[j++], _navMesh.GetTile(i).data.verts[j++], _navMesh.GetTile(i).data.verts[j++])
                        );
                }
            }
            SpawPrefabAtVerts(strideVerts);
        }

#warning Remove debug logic
        if (_input.IsKeyPressed(Keys.Space))
        {
            RebuildNavMesh();
        }
    }

    public Task RebuildNavMesh()
    {
        // The goal of this method is to do the strict minimum here on the main thread, gathering data for the async thread to do the rest on its own

        // Cancel any ongoing rebuild
        _rebuildingTask.Cancel();
        _rebuildingTask = new CancellationTokenSource();

        // Fetch mesh data from the scene - this may be too slow
        // There are a couple of avenues we could go down into to fix this but none of them are easy
        // Something we'll have to investigate later.
        var asyncInput = new AsyncInput();
        for (var e = _containerProcessor.ComponentDataEnumerator; e.MoveNext(); )
        {
            var container = e.Current.Value;

#warning should we really ignore all bodies ?
            if (container is BodyComponent)
                continue;

            // No need to store cache, nav mesh recompute should be rare enough were it would waste more memory than necessary
            container.Collider.AppendModel(asyncInput.shapeData, _shapeCache, out object? cache);
            var shapeCount = container.Collider.Transforms;
            for (int i = shapeCount - 1; i >= 0; i--)
                asyncInput.transformsOut.Add(default);
            container.Collider.GetLocalTransforms(container, CollectionsMarshal.AsSpan(asyncInput.transformsOut)[^shapeCount..]);
            asyncInput.matrices.Add((container.Entity.Transform.WorldMatrix, shapeCount));
        }

        var settingsCopy = new RcNavMeshBuildSettings
        {
            cellSize = _navSettings.cellSize,
            cellHeight = _navSettings.cellHeight,
            agentHeight = _navSettings.agentHeight,
            agentRadius = _navSettings.agentRadius,
            agentMaxClimb = _navSettings.agentMaxClimb,
            agentMaxSlope = _navSettings.agentMaxSlope,
            agentMaxAcceleration = _navSettings.agentMaxAcceleration,
            agentMaxSpeed = _navSettings.agentMaxSpeed,
            minRegionSize = _navSettings.minRegionSize,
            mergedRegionSize = _navSettings.mergedRegionSize,
            partitioning = _navSettings.partitioning,
            filterLowHangingObstacles = _navSettings.filterLowHangingObstacles,
            filterLedgeSpans = _navSettings.filterLedgeSpans,
            filterWalkableLowHeightSpans = _navSettings.filterWalkableLowHeightSpans,
            edgeMaxLen = _navSettings.edgeMaxLen,
            edgeMaxError = _navSettings.edgeMaxError,
            vertsPerPoly = _navSettings.vertsPerPoly,
            detailSampleDist = _navSettings.detailSampleDist,
            detailSampleMaxError = _navSettings.detailSampleMaxError,
            tiled = _navSettings.tiled,
            tileSize = _navSettings.tileSize,
        };
        var token = _rebuildingTask.Token;
        var task = Task.Run(() => _navMesh = CreateNavMesh(settingsCopy, asyncInput, token), token);
        _runningRebuild = task;
        return task;
    }

    private static DtNavMesh CreateNavMesh(RcNavMeshBuildSettings _navSettings, AsyncInput input, CancellationToken cancelToken)
    {
        // /!\ THIS IS NOT RUNNING ON THE MAIN THREAD /!\

        var verts = new List<VertexPosition3>();
        var indices = new List<int>();
        for (int containerI = 0, shapeI = 0; containerI < input.matrices.Count; containerI++)
        {
            var (containerMatrix, shapeCount) = input.matrices[containerI];
            containerMatrix.Decompose(out _, out Matrix worldMatrix, out var translation);
            worldMatrix.TranslationVector = translation;

            for (int j = 0; j < shapeCount; j++, shapeI++)
            {
                var transform = input.transformsOut[shapeI];
                Matrix.Transformation(ref transform.Scale, ref transform.RotationLocal, ref transform.PositionLocal, out var localMatrix);
                var finalMatrix = localMatrix * worldMatrix;

                var shape = input.shapeData[shapeI];
                verts.EnsureCapacity(verts.Count + shape.Vertices.Length);
                indices.EnsureCapacity(indices.Count + shape.Indices.Length);

                int vertexBufferStart = verts.Count;

                for (int i = 0; i < shape.Indices.Length; i += 3)
                {
                    var index0 = shape.Indices[i];
                    var index1 = shape.Indices[i+1];
                    var index2 = shape.Indices[i+2];
                    indices.Add(vertexBufferStart + index0);
                    indices.Add(vertexBufferStart + index2);
                    indices.Add(vertexBufferStart + index1);
                }

                //foreach (int index in shape.Indices)
                //    indices.Add(vertexBufferStart + index);

                for (int l = 0; l < shape.Vertices.Length; l++)
                {
                    var vertex = shape.Vertices[l].Position;
                    Vector3.Transform(ref vertex, ref finalMatrix, out Vector3 transformedVertex);
                    verts.Add(new(transformedVertex));
                }
            }
        }

        // Get the backing array of this list,
        // get a span to that backing array,
        var spanToPoints = CollectionsMarshal.AsSpan(verts);
        // cast the type of span to read it as if it was a series of contiguous floats instead of contiguous vectors
        var reinterpretedPoints = MemoryMarshal.Cast<VertexPosition3, float>(spanToPoints);
        StrideGeomProvider geom = new StrideGeomProvider(reinterpretedPoints.ToArray(), indices.ToArray());

        cancelToken.ThrowIfCancellationRequested();

        RcPartition partitionType = RcPartitionType.OfValue(_navSettings.partitioning);
        RcConfig cfg = new RcConfig(
            useTiles: true,
            _navSettings.tileSize,
            _navSettings.tileSize,
            RcConfig.CalcBorder(_navSettings.agentRadius, _navSettings.cellSize),
            partitionType,
            _navSettings.cellSize,
            _navSettings.cellHeight,
            _navSettings.agentMaxSlope,
            _navSettings.agentHeight,
            _navSettings.agentRadius,
            _navSettings.agentMaxClimb,
            (_navSettings.minRegionSize * _navSettings.minRegionSize) * _navSettings.cellSize * _navSettings.cellSize,
            (_navSettings.mergedRegionSize * _navSettings.mergedRegionSize) * _navSettings.cellSize * _navSettings.cellSize,
            _navSettings.edgeMaxLen,
            _navSettings.edgeMaxError,
            _navSettings.vertsPerPoly,
            _navSettings.detailSampleDist,
            _navSettings.detailSampleMaxError,
            _navSettings.filterLowHangingObstacles,
            _navSettings.filterLedgeSpans,
            _navSettings.filterWalkableLowHeightSpans,
            SampleAreaModifications.SAMPLE_AREAMOD_WALKABLE,
            buildMeshDetail: true);

        cancelToken.ThrowIfCancellationRequested();

        List<DtMeshData> dtMeshes = new();
        foreach (RcBuilderResult result in new RcBuilder().BuildTiles(geom, cfg, Task.Factory))
        {
            DtNavMeshCreateParams navMeshCreateParams = DemoNavMeshBuilder.GetNavMeshCreateParams(geom, _navSettings.cellSize, _navSettings.cellHeight, _navSettings.agentHeight, _navSettings.agentRadius, _navSettings.agentMaxClimb, result);
            navMeshCreateParams.tileX = result.tileX;
            navMeshCreateParams.tileZ = result.tileZ;
            DtMeshData dtMeshData = DtNavMeshBuilder.CreateNavMeshData(navMeshCreateParams);
            if (dtMeshData != null)
            {
                dtMeshes.Add(DemoNavMeshBuilder.UpdateAreaAndFlags(dtMeshData));
            }

            cancelToken.ThrowIfCancellationRequested();
        }

        cancelToken.ThrowIfCancellationRequested();

        DtNavMeshParams option = default;
        option.orig = geom.GetMeshBoundsMin();
        option.tileWidth = _navSettings.tileSize * _navSettings.cellSize;
        option.tileHeight = _navSettings.tileSize * _navSettings.cellSize;
        option.maxTiles = GetMaxTiles(geom, _navSettings.cellSize, _navSettings.tileSize);
        option.maxPolys = GetMaxPolysPerTile(geom, _navSettings.cellSize, _navSettings.tileSize);
        DtNavMesh navMesh = new DtNavMesh(option, _navSettings.vertsPerPoly);
        foreach (DtMeshData dtMeshData1 in dtMeshes)
        {
            navMesh.AddTile(dtMeshData1, 0, 0L);
        }

        cancelToken.ThrowIfCancellationRequested();

        return navMesh;
    }

    private static int GetMaxTiles(IInputGeomProvider geom, float cellSize, int tileSize)
    {
        int tileBits = GetTileBits(geom, cellSize, tileSize);
        return 1 << tileBits;
    }

    private static int GetMaxPolysPerTile(IInputGeomProvider geom, float cellSize, int tileSize)
    {
        int num = 22 - GetTileBits(geom, cellSize, tileSize);
        return 1 << num;
    }

    private static int GetTileBits(IInputGeomProvider geom, float cellSize, int tileSize)
    {
        RcCommons.CalcGridSize(geom.GetMeshBoundsMin(), geom.GetMeshBoundsMax(), cellSize, out var sizeX, out var sizeZ);
        int num = (sizeX + tileSize - 1) / tileSize;
        int num2 = (sizeZ + tileSize - 1) / tileSize;
        return Math.Min(DtUtils.Ilog2(DtUtils.NextPow2(num * num2)), 14);
    }

    private static int[] GetTiles(DemoInputGeomProvider geom, float cellSize, int tileSize)
    {
        RcCommons.CalcGridSize(geom.GetMeshBoundsMin(), geom.GetMeshBoundsMax(), cellSize, out var sizeX, out var sizeZ);
        int num = (sizeX + tileSize - 1) / tileSize;
        int num2 = (sizeZ + tileSize - 1) / tileSize;
        return [num, num2];
    }

#warning this is just me debugging should remove later
    private void SpawPrefabAtVerts(List<Vector3> verts)
    {
        // Make sure the cube is a root asset or else this wont load
        var cube = _game.Content.Load<Model>("Cube");
        foreach (var vert in verts)
        {
            AddMesh(_game.GraphicsDevice, _sceneSystem.SceneInstance.RootScene.Children[0], vert, cube.Meshes[0].Draw);
        }
    }
    Entity AddMesh(GraphicsDevice graphicsDevice, Scene rootScene, Vector3 position, MeshDraw meshDraw)
    {
        var entity = new Entity { Scene = rootScene, Transform = { Position = position } };
        var model = new Model
        {
        new MaterialInstance
        {
            Material = Material.New(graphicsDevice, new MaterialDescriptor
            {
                Attributes = new MaterialAttributes
                {
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    Diffuse = new MaterialDiffuseMapFeature
                    {
                        DiffuseMap = new ComputeVertexStreamColor()
                    },
                }
            })
        },
        new Mesh
        {
            Draw = meshDraw,
            MaterialIndex = 0
        }
        };
        entity.Add(new ModelComponent { Model = model });
        return entity;
    }

    class AsyncInput
    {
        public List<BasicMeshBuffers> shapeData = new();
        public List<ShapeTransform> transformsOut = new();
        public List<(Matrix entity, int count)> matrices = new();
    }
}
