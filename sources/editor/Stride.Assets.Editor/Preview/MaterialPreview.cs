// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Assets.Materials;
using Stride.Editor.Annotations;
using Stride.Editor.Preview;
using Stride.Engine;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering.ProceduralModels;
using Stride.Rendering;

namespace Stride.Assets.Editor.Preview;

public enum MaterialPreviewPrimitive
{
    Sphere,
    Cube,
    Cylinder,
    Torus,
    Plane,
    Teapot,
    Cone,
    Capsule
}

/// <summary>
/// An implementation of the <see cref="AssetPreview"/> that can preview materials.
/// </summary>
[AssetPreview<MaterialAsset>]
public class MaterialPreview : PreviewFromEntity<MaterialAsset>
{
    public const string EditorMaterialPreviewEffect = "StrideEditorMaterialPreviewEffect";

    private MaterialPreviewPrimitive previewPrimitive;

    public MaterialPreview() : base(EditorMaterialPreviewEffect)
    {
    }

    protected override async Task Initialize()
    {
        CameraScript.DefaultYaw = MathUtil.Pi; // want to see the center of the texture by default (and not corners).

        await base.Initialize();
    }

    public async void SetPrimitive(MaterialPreviewPrimitive primitive)
    {
        if (previewPrimitive != primitive)
        {
            previewPrimitive = primitive;
            await Update();
        }
    }

    /// <inheritdoc/>
    protected override PreviewEntity CreatePreviewEntity()
    {
        // load the material from the data base
        var material = LoadAsset<Material>(AssetItem.Location);

        // create a sphere model to display the material
        var proceduralModel = CreatePrimitiveModel(previewPrimitive);
        var model = proceduralModel.GenerateModel(Game.Services); // TODO: should dispose those resources at some points!
        model.Add(material);

        // create the entity, create and set the model component
        var materialEntity = new Entity { Name = BuildName() };
        materialEntity.Add(new ModelComponent { Model = model });

        var previewEntity = new PreviewEntity(materialEntity);
        previewEntity.Disposed += () => UnloadAsset(material);
        return previewEntity;
    }

    internal static ProceduralModelDescriptor CreatePrimitiveModel(MaterialPreviewPrimitive primitive)
    {
        return primitive switch
        {
            MaterialPreviewPrimitive.Sphere => new ProceduralModelDescriptor { Type = new SphereProceduralModel() },
            MaterialPreviewPrimitive.Cube => new ProceduralModelDescriptor { Type = new CubeProceduralModel() },
            MaterialPreviewPrimitive.Cylinder => new ProceduralModelDescriptor { Type = new CylinderProceduralModel() },
            MaterialPreviewPrimitive.Torus => new ProceduralModelDescriptor { Type = new TorusProceduralModel() },
            MaterialPreviewPrimitive.Plane => new ProceduralModelDescriptor { Type = new PlaneProceduralModel { GenerateBackFace = true, Normal = NormalDirection.UpZ } },
            MaterialPreviewPrimitive.Teapot => new ProceduralModelDescriptor { Type = new TeapotProceduralModel() },
            MaterialPreviewPrimitive.Cone => new ProceduralModelDescriptor { Type = new ConeProceduralModel() },
            MaterialPreviewPrimitive.Capsule => new ProceduralModelDescriptor { Type = new CapsuleProceduralModel() },
            _ => throw new ArgumentOutOfRangeException(nameof(primitive)),
        };
    }
}
