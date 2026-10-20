// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Stride.Animations;
using Stride.Core.Assets;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Rendering;
using Stride.Updater;
using Xunit;

namespace Stride.Assets.Tests;

public class TestModelComponentMorphs
{
    private static Model ModelWithTargets(int meshes = 1, string target = "Smile")
    {
        var model = new Model();
        for (int i = 0; i < meshes; i++)
            model.Meshes.Add(new Mesh { MorphTargets = MeshMorphData.Create(0, new[] { target }, Array.Empty<MeshMorphEntry>()) });
        return model;
    }

    [Fact]
    public void SharedModelHasIndependentNamedWeightsAndZeroDefaults()
    {
        var shared = ModelWithTargets(2);
        var first = new ModelComponent(shared);
        var second = new ModelComponent(shared);
        Assert.True(first.HasMorphTargets);
        Assert.Equal(0, first.GetMorphWeight(first.FindMorphTarget("Smile")));
        first.SetMorphWeight("Smile", -0.75f);
        Assert.Equal(-0.75f, first.GetMorphWeight(0));
        Assert.Equal(0, second.GetMorphWeight(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => first.SetMorphWeight("Smile", float.NaN));
        Assert.Throws<ArgumentException>(() => first.SetMorphWeight("Missing", 1));
        Assert.False(new ModelComponent(new Model()).HasMorphTargets);
    }

    [Fact]
    public void SerializedControlsSurviveDelayedLoadingAndReplacement()
    {
        var component = new ModelComponent();
        Author(component, "Smile", 0.125f);
        using var stream = new MemoryStream();
        var writer = new BinarySerializationWriter(stream);
        writer.Serialize(ref component, ArchiveMode.Serialize);
        stream.Position = 0;
        ModelComponent restored = null;
        var reader = new BinarySerializationReader(stream);
        reader.Serialize(ref restored, ArchiveMode.Deserialize);
        restored.Model = ModelWithTargets();
        Assert.Equal(0.125f, restored.GetMorphWeight(restored.FindMorphTarget("Smile")));
        restored.Model = ModelWithTargets(target: "Blink");
        Assert.Equal(0, restored.GetMorphWeight(restored.FindMorphTarget("Blink")));
        Assert.Equal(0.125f, restored.Morphs.Weights["Smile"]);
        restored.Model = ModelWithTargets();
        Assert.Equal(0.125f, restored.GetMorphWeight(restored.FindMorphTarget("Smile")));
    }

    [Fact]
    public void SetAllMorphWeightsUpdatesAllMeshesAndOnlyThisInstance()
    {
        var shared = ModelWithTargets(2);
        shared.Meshes.Add(new Mesh { MorphTargets = MeshMorphData.Create(0, new[] { "Blink" }, Array.Empty<MeshMorphEntry>()) });
        var first = new ModelComponent(shared);
        var second = new ModelComponent(shared);
        first.SetAllMorphWeights(-1f);
        Assert.Equal(new[] { -1f, -1f }, Weights(first));
        Assert.Empty(first.Morphs.Weights); // runtime writes do not touch authored weights
        Assert.Equal(new[] { 0f, 0f }, Weights(second));
        Assert.Throws<ArgumentOutOfRangeException>(() => first.SetAllMorphWeights(float.NaN));
        Assert.Equal(new[] { -1f, -1f }, Weights(first));
        first.SetAllMorphWeights(0f);
        Assert.Equal(new[] { 0f, 0f }, Weights(first));
        first.Model = ModelWithTargets(1, "NewTarget");
        first.SetAllMorphWeights(1f);
        Assert.Equal(new[] { 1f }, Weights(first));
        new ModelComponent().SetAllMorphWeights(1f);
    }

    [Fact]
    public void SlotsAreSharedByNameAndAuthoredWeightsSeedThem()
    {
        var shared = ModelWithTargets(2);
        shared.Meshes.Add(new Mesh { MorphTargets = MeshMorphData.Create(0, new[] { "Blink", "Smile" }, Array.Empty<MeshMorphEntry>()) });
        var component = new ModelComponent(shared);
        Assert.Equal(new[] { "Smile", "Blink" }, component.MorphTargetNames.ToArray());
        Assert.Equal(2, component.MorphTargetCount);
        Assert.Equal("Blink", component.GetMorphTargetName(1));
        int smile = component.FindMorphTarget("Smile"), blink = component.FindMorphTarget("Blink");
        Assert.Equal(-1, component.FindMorphTarget("Missing"));
        component.SetMorphWeight(smile, 0.5f);
        Assert.Equal(new[] { 0.5f, 0f }, Weights(component));
        Assert.Throws<ArgumentOutOfRangeException>(() => component.SetMorphWeight(5, 1));
        // An authored item change re-seeds runtime weights from the authored values.
        Author(component, "Blink", 0.25f);
        Assert.Equal(0.25f, component.GetMorphWeight(blink));
        Assert.Equal(0f, component.GetMorphWeight(smile));
        component.SetMorphWeight(blink, 1f);
        component.Morphs.Weights.Remove("Blink");
        Assert.Equal(0f, component.GetMorphWeight(blink));
        // So does replacing the dictionary.
        component.Morphs.Weights = new() { ["Smile"] = 0.75f };
        Assert.Equal(0.75f, component.GetMorphWeight(smile));
        Assert.True(component.MorphTargetNames == new ModelComponent(shared).MorphTargetNames);
    }

    [Fact]
    public void NamedWeightsRoundTripThroughAssetYaml()
    {
        var scene = new Stride.Assets.Entities.SceneAsset();
        var model = new ModelComponent();
        Author(model, "Smile", -0.25f);
        var entity = new Entity { model };
        scene.Hierarchy.Parts.Add(new Stride.Assets.Entities.EntityDesign(entity));
        using var stream = new MemoryStream();
        AssetFileSerializer.Save(stream, scene, null);
        stream.Position = 0;
        var restored = AssetFileSerializer.Load<Stride.Assets.Entities.SceneAsset>(stream, "scene.sdscene").Asset;
        var restoredModel = System.Linq.Enumerable.First(restored.Hierarchy.Parts).Value.Entity.Get<ModelComponent>();
        Assert.Equal(-0.25f, restoredModel.Morphs.Weights["Smile"]);
    }

    [Fact]
    public void AnimationPathsWriteWeightsByName()
    {
        var shared = new Model();
        shared.Meshes.Add(new Mesh { MorphTargets = MeshMorphData.Create(0, new[] { "Blink", "Smile" }, Array.Empty<MeshMorphEntry>()) });
        var other = ModelWithTargets(target: "Frown");
        var entity = new Entity { new ModelComponent(shared) };
        var members = new List<UpdateMemberInfo>
        {
            new("[ModelComponent.Key].MorphWeights[Smile]", 0),
            new("[ModelComponent.Key].MorphWeights[Frown]", 8),
        };
        var update = UpdateEngine.Compile(typeof(Entity), members);
        // Each channel is a (condition, value) pair, as written by the animation blender.
        var data = new float[] { 1, 0.5f, 1, 0.75f };
        var component = entity.Get<ModelComponent>();
        Run(entity, update, data);
        Assert.Equal(new[] { 0f, 0.5f }, Weights(component));

        // The same compiled update binds to another model by name; missing targets are skipped.
        component.Model = other;
        Run(entity, update, data);
        Assert.Equal(new[] { 0.75f }, Weights(component));
    }

    [Fact]
    public void NullModelAndInPlaceMeshReplacementRebindAuthoredWeights()
    {
        var shared = ModelWithTargets();
        var first = new ModelComponent(shared);
        var second = new ModelComponent(shared);
        Author(first, "Smile", 0.25f);
        Author(first, "Blink", 0.75f);
        first.SetMorphWeight("Smile", 2f);
        first.Model = null;
        Assert.False(first.HasMorphTargets);
        Assert.Equal(0, first.MorphTargetCount);
        Assert.True(first.MorphTargetNames.IsEmpty);
        Assert.Equal(-1, first.FindMorphTarget("Smile"));
        first.SetAllMorphWeights(0f);
        first.Model = shared;
        Assert.Equal(0.25f, first.GetMorphWeight(first.FindMorphTarget("Smile")));

        shared.Meshes[0].MorphTargets = MeshMorphData.Create(0, new[] { "Blink", "Smile" }, Array.Empty<MeshMorphEntry>());
        Assert.Equal(new[] { "Blink", "Smile" }, first.MorphTargetNames.ToArray());
        Assert.Equal(new[] { 0.75f, 0.25f }, Weights(first));
        Assert.Equal(new[] { 0f, 0f }, Weights(second));
        shared.Meshes.Clear();
        Assert.False(first.HasMorphTargets);
        Assert.Empty(Weights(first));
        shared.Meshes.Add(ModelWithTargets().Meshes[0]);
        Assert.Equal(new[] { 0.25f }, Weights(first));
        Assert.Equal(new[] { 0f }, Weights(second));
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void NonfiniteRuntimeWritesLeaveExistingWeightsUnchanged(float value)
    {
        var component = new ModelComponent(ModelWithTargets());
        component.SetMorphWeight("Smile", 0.5f);
        Assert.Throws<ArgumentOutOfRangeException>(() => component.SetMorphWeight(0, value));
        Assert.Throws<ArgumentOutOfRangeException>(() => component.SetMorphWeight("Smile", value));
        Assert.Throws<ArgumentOutOfRangeException>(() => component.SetMorphWeight("Smile".AsSpan(), value));
        Assert.Throws<ArgumentOutOfRangeException>(() => component.SetAllMorphWeights(value));
        Assert.Equal(0.5f, component.GetMorphWeight(0));
        Assert.Empty(component.Morphs.Weights);
    }

    [Fact]
    public void NamesAreOrdinalAndInvalidNamesAndSlotsDoNotChangeWeights()
    {
        var component = new ModelComponent(ModelWithTargets());
        component.SetMorphWeight("Smile".AsSpan(), -2f);
        Assert.Equal(0, component.FindMorphTarget("Smile".AsSpan()));
        Assert.Equal(-1, component.FindMorphTarget("smile"));
        Assert.Throws<ArgumentException>(() => component.FindMorphTarget(" ".AsSpan()));
        Assert.Throws<ArgumentException>(() => component.SetMorphWeight("smile".AsSpan(), 1f));
        Assert.Throws<ArgumentNullException>(() => component.FindMorphTarget((string)null));
        Assert.Throws<ArgumentOutOfRangeException>(() => component.GetMorphWeight(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => component.SetMorphWeight(1, 1f));
        Assert.Throws<IndexOutOfRangeException>(() => component.GetMorphTargetName(1));
        Assert.Equal(-2f, component.GetMorphWeight(0));
    }

    [Fact]
    public void BlendedMorphAnimationAppliesOnTheFirstUpdateAfterModelChanges()
    {
        var component = new ModelComponent(ModelWithTargets());
        Author(component, "Smile", 0.1f);
        var entity = new Entity { component };
        var blender = new AnimationBlender();
        var evaluators = new List<AnimationClipEvaluator>();
        foreach (float value in new[] { 0.2f, 0.8f })
        {
            var clip = new AnimationClip { Duration = TimeSpan.FromSeconds(1) };
            foreach (var name in new[] { "Smile", "Frown" })
            {
                var curve = new AnimationCurve<float>();
                curve.KeyFrames.Add(new KeyFrameData<float>(CompressedTimeSpan.Zero, value));
                curve.KeyFrames.Add(new KeyFrameData<float>(CompressedTimeSpan.FromSeconds(1), value));
                clip.AddCurve($"[ModelComponent.Key].MorphWeights[{name}]", curve);
            }
            evaluators.Add(blender.CreateEvaluator(clip));
        }
        try
        {
            AnimationClipResult result = null;
            blender.Compute([AnimationOperation.NewPush(evaluators[0]), AnimationOperation.NewPush(evaluators[1]),
                AnimationOperation.NewBlend(CoreAnimationOperation.Blend, 0.5f)], ref result);
            var updater = new AnimationUpdater();
            updater.Update(entity, result);
            Assert.Equal(0.5f, component.GetMorphWeight(component.FindMorphTarget("Smile")), 5);

            Author(component, "Smile", 0.3f);
            updater.Update(entity, result);
            Assert.Equal(0.5f, component.GetMorphWeight(component.FindMorphTarget("Smile")), 5);

            var replacement = new Model();
            replacement.Meshes.Add(new Mesh { MorphTargets = MeshMorphData.Create(0, new[] { "Frown", "Smile" }, Array.Empty<MeshMorphEntry>()) });
            component.Model = replacement;
            updater.Update(entity, result);
            Assert.Equal(new[] { 0.5f, 0.5f }, Weights(component));

            component.Model = null;
            updater.Update(entity, result);
            component.Model = replacement;
            // Resume with only the second clip, without priming the component through a weight getter.
            blender.Compute([AnimationOperation.NewPush(evaluators[1])], ref result);
            updater.Update(entity, result);
            Assert.Equal(new[] { 0.8f, 0.8f }, Weights(component));
            Assert.Equal(0.3f, component.Morphs.Weights["Smile"]);

            // A model-reference curve in the same result must be applied before named morph curves.
            var animatedReplacement = ModelWithTargets(target: "Frown");
            var replacementClip = new AnimationClip { Duration = TimeSpan.FromSeconds(1) };
            var replacementCurve = new AnimationCurve<object>();
            replacementCurve.KeyFrames.Add(new KeyFrameData<object>(CompressedTimeSpan.Zero, animatedReplacement));
            replacementClip.AddCurve("[ModelComponent.Key].Model", replacementCurve);
            var weightCurve = new AnimationCurve<float>();
            weightCurve.KeyFrames.Add(new KeyFrameData<float>(CompressedTimeSpan.Zero, 0.65f));
            weightCurve.KeyFrames.Add(new KeyFrameData<float>(CompressedTimeSpan.FromSeconds(1), 0.65f));
            replacementClip.AddCurve("[ModelComponent.Key].MorphWeights[Frown]", weightCurve);
            var replacementEvaluator = blender.CreateEvaluator(replacementClip);
            evaluators.Add(replacementEvaluator);
            blender.Compute([AnimationOperation.NewPush(replacementEvaluator)], ref result);
            updater.Update(entity, result);
            Assert.Same(animatedReplacement, component.Model);
            Assert.Equal(0.65f, component.GetMorphWeight(component.FindMorphTarget("Frown")));
        }
        finally
        {
            foreach (var evaluator in evaluators)
                blender.ReleaseEvaluator(evaluator);
        }
    }

    private static float[] Weights(ModelComponent component)
        => Enumerable.Range(0, component.MorphTargetCount).Select(component.GetMorphWeight).ToArray();

    private static void Author(ModelComponent component, string name, float value)
        => component.Morphs.Weights[name] = value;

    private static void Run(Entity entity, CompiledUpdate update, float[] data)
    {
        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            UpdateEngine.Run(entity, update, handle.AddrOfPinnedObject(), Array.Empty<UpdateObjectData>());
        }
        finally { handle.Free(); }
    }
}
