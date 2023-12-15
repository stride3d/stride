using DotRecast.Detour;
using DotRecast.Recast.Toolset;
using DotRecast.Recast.Toolset.Builder;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Navigation.Components;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.Materials;
using System.Xml.Linq;
using Stride.Core;
using Stride.Input;

namespace Stride.BepuPhysics.Navigation.Processors;
public class RecastMeshProcessor : EntityProcessor<TriggerBoundingBox>
{

	public List<Vector3> Points = new List<Vector3>();
	public List<int> Indices = new List<int>();

	private TileNavMeshBuilder _tileNavMeshBuilder = new TileNavMeshBuilder();
	private DtNavMesh? _navMesh;
	private List<TriggerBoundingBox> _boundingBoxes = new();
	private IGame _game;
	private SceneSystem _sceneSystem;
	private InputManager _input;

	private List<ContainerComponent> _containerComponents = new List<ContainerComponent>();
	private int _previousContainerCount = 0;

	public RecastMeshProcessor()
	{
		Order = 20000;
	}

	protected override void OnSystemAdd()
	{
		base.OnSystemAdd();
		_game = Services.GetService<IGame>();
		_sceneSystem = Services.GetService<SceneSystem>();
		_input = Services.GetSafeServiceAs<InputManager>();
	}

	protected override void OnEntityComponentAdding(Entity entity, [NotNull] TriggerBoundingBox component, [NotNull] TriggerBoundingBox data)
	{
		_boundingBoxes.Add(data);
		data.ContainerEnter += ContainerEnter;
		data.ContainerLeave += ContainerExit;
		var test = entity.Scene.Entities;
		foreach (var entityTest in test)
		{
			foreach(var child in entityTest.GetChildren())
			{
				_containerComponents.Add(child.Get<StaticContainerComponent>());
			}	
		}
	}

	private void ContainerEnter(object? sender, ContainerComponent e)
	{
		if (e is StaticContainerComponent)
		{
			_containerComponents.Add(e);
		}
	}

	private void ContainerExit(object? sender, ContainerComponent e)
	{
		if (e is StaticContainerComponent && _containerComponents.Contains(e))
		{
			_containerComponents.Remove(e);
		}
	}

	protected override void OnEntityComponentRemoved(Entity entity, [NotNull] TriggerBoundingBox component, [NotNull] TriggerBoundingBox data)
	{

	}

	public override void Update(GameTime time)
	{
		if (_input.IsKeyPressed(Keys.Space))
		{
			Points.Clear();
			Indices.Clear();
			foreach (var container in _containerComponents)
			{
				AddContainerData(container);
			}
			_previousContainerCount = _containerComponents.Count;
			CreateNavMesh();
		}
	}

	public void CreateNavMesh()
	{
		List<float> verts = new List<float>();
		foreach (var v in Points)
		{
			verts.Add(v.X);
			verts.Add(v.Y);
			verts.Add(v.Z);
		}
		StrideGeomProvider geom = new StrideGeomProvider(verts, Indices);
		var result = _tileNavMeshBuilder.Build(geom, new RcNavMeshBuildSettings());

		_navMesh = result.NavMesh;
		SpawPrefabAtVerts(Points);
	}

	private void SpawPrefabAtVerts(List<Vector3> verts)
	{
		// Make sure the cube is a root asset or else this wont load
		var cube = _game.Content.Load<Model>("Cube");

		foreach (var vert in verts)
		{
			AddMesh(_game.GraphicsDevice, _sceneSystem.SceneInstance.RootScene, vert, cube.Meshes[0].Draw);
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

	public void AddContainerData(ContainerComponent containerData)
	{
		var shape = containerData.GetShapeData();
		AppendArrays(shape.Points.ToArray(), shape.Indices.ToArray(), containerData.Entity.Transform.WorldMatrix);
	}

	public void AppendArrays(Vector3[] vertices, int[] indices, Matrix objectTransform)
	{
		// Copy vertices
		int vbase = Points.Count;
		for (int i = 0; i < vertices.Length; i++)
		{
			var vertex = Vector3.Transform(vertices[i], objectTransform).XYZ();
			Points.Add(vertex);
		}

		// Copy indices with offset applied
		foreach (int index in indices)
		{
			Indices.Add(index + vbase);
		}
	}
}
