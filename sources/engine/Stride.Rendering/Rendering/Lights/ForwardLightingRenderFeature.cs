// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;
using Stride.Core.Storage;
using Stride.Core.Threading;
using Stride.Graphics;
using Stride.Rendering.Shadows;
using Stride.Shaders;

namespace Stride.Rendering.Lights
{
    /// <summary>
    /// Compute lighting shaders and data. An effect is compiled for the lights of the views that draw its object, its
    /// lighting set: an object drawn in one view is lit by that view's lights alone, and one drawn in several by the
    /// lights of them all, so views with lights of their own in one frame do not compile every material for the
    /// union of their lights.
    /// </summary>
    /// <remarks>
    /// Views that take the same light groups with the same light counts share one lighting set, so views lit alike,
    /// the eyes of a headset or two cameras on one scene, give the permutation a single view gives. Where views are
    /// lit differently, the set of an object follows the views it is visible in: an object that comes into a second
    /// such view changes effect, for one compiled for the lights of both views the first time it is needed. A
    /// material is thus compiled once per different lighting among the sets of views that draw it, rather than once
    /// for the lights of the whole frame.
    /// </remarks>
    public class ForwardLightingRenderFeature : SubRenderFeature
    {
        /// <summary>
        /// Property key to access the current collection of `CameraComponent` from <see cref="ComponentBase.Tags"/>.
        /// </summary>
        public static readonly PropertyKey<RenderLightCollection> CurrentLights = new PropertyKey<RenderLightCollection>("ForwardLightingRenderFeature.CurrentLights", typeof(ForwardLightingRenderFeature));

        public class RenderViewLightData
        {
            /// <summary>
            /// Gets the lights without shadow per light type.
            /// </summary>
            /// <value>The lights.</value>
            public readonly Dictionary<Type, RenderLightCollectionGroup> ActiveLightGroups;

            internal readonly List<ActiveLightGroupRenderer> ActiveRenderers;

            public readonly List<RenderLight> VisibleLights;
            public readonly List<RenderLight> VisibleLightsWithShadows;

            public readonly Dictionary<RenderLight, LightShadowMapTexture> RenderLightsWithShadows;

            // Effects in the same view can resolve to different PerView Lighting layouts (e.g. when
            // light permutation counts differ). Track one variant per layout hash so each layout's
            // resource group gets parameters of its expected shape, instead of throwing.
            internal sealed class ViewLayoutVariant
            {
                public ObjectId Hash;
                public ParameterCollectionLayout ParameterCollectionLayout;
                public readonly ParameterCollection Parameters = new ParameterCollection();
                public int LastFrameProcessed = -1;
            }

            // Inline fast path for the common single-variant case
            internal ViewLayoutVariant SingleVariant;
            internal Dictionary<ObjectId, ViewLayoutVariant> ExtraVariants;

            internal ViewLayoutVariant GetOrAddVariant(ObjectId hash)
            {
                if (SingleVariant == null)
                {
                    SingleVariant = new ViewLayoutVariant { Hash = hash };
                    return SingleVariant;
                }
                if (SingleVariant.Hash == hash)
                    return SingleVariant;

                ExtraVariants ??= new Dictionary<ObjectId, ViewLayoutVariant>();
                if (!ExtraVariants.TryGetValue(hash, out var variant))
                {
                    variant = new ViewLayoutVariant { Hash = hash };
                    ExtraVariants[hash] = variant;
                }
                return variant;
            }

            public RenderViewLightData()
            {
                ActiveLightGroups = new Dictionary<Type, RenderLightCollectionGroup>(16);
                ActiveRenderers = new List<ActiveLightGroupRenderer>(16);

                VisibleLights = new List<RenderLight>(1024);
                VisibleLightsWithShadows = new List<RenderLight>(1024);
                RenderLightsWithShadows = new Dictionary<RenderLight, LightShadowMapTexture>(16);
            }
        }

        private readonly ThreadLocal<PrepareThreadLocals> prepareThreadLocals = new ThreadLocal<PrepareThreadLocals>(() => new PrepareThreadLocals());

        private StaticObjectPropertyKey<RenderEffect> renderEffectKey;

        private const string DirectLightGroupsCompositionName = "directLightGroups";
        private const string EnvironmentLightsCompositionName = "environmentLights";

        /// <summary>
        /// Every light shader group of the frame, in the light renderers' order.
        /// </summary>
        private readonly LightShaderPermutationEntry frameGroups = new LightShaderPermutationEntry();

        /// <summary>
        /// The lighting sets of the frame, one per different lighting among the sets of views the objects are drawn in:
        /// the groups with lights in those views, laid out at their indices in the set, and the shaders the set's
        /// effects are validated with. Pooled; <see cref="lightingSetCount"/> are in use.
        /// </summary>
        private readonly List<LightShaderPermutationEntry> lightingSets = new List<LightShaderPermutationEntry>();
        private int lightingSetCount;

        /// <summary>
        /// The index in <see cref="lightingSets"/> of the set of each set of views the objects are drawn in.
        /// </summary>
        private readonly Dictionary<ulong, int> lightingSetsByViews = new Dictionary<ulong, int>();

        /// <summary>
        /// The set of an object drawn in no lit view this frame.
        /// </summary>
        private const int NoLightingSet = -1;

        /// <summary>
        /// The set the groups are laid out for now. A group holds one layout, its composition name and its light count,
        /// so a frame of several sets lays the groups out again for each, once to validate the set's effects and once
        /// to prepare its nodes; a frame of one set lays them out once.
        /// </summary>
        private LightShaderPermutationEntry laidOutSet;

        /// <summary>
        /// The index of the lighting set of each per-view layout, in each view by its index in <see cref="renderViews"/>.
        /// </summary>
        private readonly Dictionary<(ViewResourceGroupLayout Layout, int ViewIndex), int> viewLayoutSets = new Dictionary<(ViewResourceGroupLayout, int), int>();

        private StaticObjectPropertyKey<ulong> objectViewsKey;
        private StaticObjectPropertyKey<int> objectSetKey;

        private readonly TrackingCollection<LightGroupRendererBase> lightRenderers = new TrackingCollection<LightGroupRendererBase>();

        /// <summary>
        /// List of renderers that can handle a specific light type by light type
        /// </summary>
        private readonly Dictionary<Type, List<LightGroupRendererBase>> lightRenderersByType = new Dictionary<Type, List<LightGroupRendererBase>>();

        private readonly Dictionary<RenderView, RenderViewLightData> renderViewDatas = new Dictionary<RenderView, RenderViewLightData>();

        // Preallocted for CollectVisibleLights
        private readonly HashSet<RenderView> processedRenderViews = new HashSet<RenderView>();

        private readonly List<RenderView> renderViews = [];

        private readonly Dictionary<ShaderSourceCollection, ShaderSourceCollection> shaderSourcesReadonlyCache = new Dictionary<ShaderSourceCollection, ShaderSourceCollection>();

        private readonly List<int> lightIndicesToProcess = new List<int>();

        private LogicalGroupReference viewLightingKey;
        private LogicalGroupReference drawLightingKey;
        private IShadowMapRenderer shadowMapRenderer;

        private static readonly string[] DirectLightGroupsCompositionNames;
        private static readonly string[] EnvironmentLightGroupsCompositionNames;
        private static readonly ProfilingKey PrepareEffectPermutationsKey = new ProfilingKey("ForwardLightingRenderFeature.PrepareEffectPermutations");
        private static readonly ProfilingKey PrepareKey = new ProfilingKey("ForwardLightingRenderFeature.Prepare");
        private static readonly ProfilingKey DrawKey = new ProfilingKey("ForwardLightingRenderFeature.Draw");

        private readonly HashSet<int> ignoredEffectSlots = new HashSet<int>();

        private RenderView currentRenderView;

        [DataMember]
        [Category]
        [MemberCollection(CanReorderItems = true, NotNullItems = true)]
        public TrackingCollection<LightGroupRendererBase> LightRenderers => lightRenderers;
        
        [DataMember]
        public IShadowMapRenderer ShadowMapRenderer
        {
            get { return shadowMapRenderer; }
            set
            {
                // Unset RenderSystem on old value
                if (shadowMapRenderer != null)
                    shadowMapRenderer.RenderSystem = null;

                shadowMapRenderer = value;

                // Set RenderSystem on new value
                if (shadowMapRenderer != null)
                    shadowMapRenderer.RenderSystem = RenderSystem;
            }
        }

        static ForwardLightingRenderFeature()
        {
            // TODO: 32 is hardcoded and will generate a NullReferenceException in CreateShaderPermutationEntry
            DirectLightGroupsCompositionNames = new string[32];
            for (int i = 0; i < DirectLightGroupsCompositionNames.Length; i++)
            {
                DirectLightGroupsCompositionNames[i] = DirectLightGroupsCompositionName + "[" + i + "]";
            }
            EnvironmentLightGroupsCompositionNames = new string[32];
            for (int i = 0; i < EnvironmentLightGroupsCompositionNames.Length; i++)
            {
                EnvironmentLightGroupsCompositionNames[i] = EnvironmentLightsCompositionName + "[" + i + "]";
            }
        }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            // Initialize light renderers
            foreach (var lightRenderer in lightRenderers)
            {
                lightRenderer.Initialize(Context);
            }
            EvaluateLightTypes();

            // Track changes
            lightRenderers.CollectionChanged += LightRenderers_CollectionChanged;

            renderEffectKey = ((RootEffectRenderFeature)RootRenderFeature).RenderEffectKey;
            objectViewsKey = RootRenderFeature.RenderData.CreateStaticObjectKey<ulong>();
            objectSetKey = RootRenderFeature.RenderData.CreateStaticObjectKey<int>();

            viewLightingKey = ((RootEffectRenderFeature)RootRenderFeature).CreateViewLogicalGroup("Lighting");
            drawLightingKey = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawLogicalGroup("Lighting");
        }

        protected override void Destroy()
        {
            prepareThreadLocals.Dispose();

            base.Destroy();
        }

        public override void Unload()
        {
            // Unload light renderers
            foreach (var lightRenderer in lightRenderers)
            {
                lightRenderer.Unload();
            }
            lightRenderers.CollectionChanged -= LightRenderers_CollectionChanged;

            base.Unload();
        }

        public override void Collect()
        {
            // Collect all visible lights
            CollectVisibleLights();

            // Prepare active renderers in an ordered list (by type and shadow on/off)
            CollectActiveLightRenderers(Context);

            // Collect shadow maps
            ShadowMapRenderer?.Collect(Context, renderViewDatas);
        }

        /// <inheritdoc/>
        public override void Extract()
        {
        }

        /// <inheritdoc/>
        public override void PrepareEffectPermutations(RenderDrawContext context)
        {
            using var _ = Profiler.Begin(PrepareEffectPermutationsKey);
            var renderEffects = RootRenderFeature.RenderData.GetData(renderEffectKey);
            int effectSlotCount = ((RootEffectRenderFeature)RootRenderFeature).EffectPermutationSlotCount;

            ignoredEffectSlots.Clear();
            if (ShadowMapRenderer != null)
            {
                foreach (var lightShadowMapRenderer in ShadowMapRenderer.Renderers)
                {
                    var shadowMapEffectSlot = lightShadowMapRenderer != null ? ((RootEffectRenderFeature)RootRenderFeature).GetEffectPermutationSlot(lightShadowMapRenderer.ShadowCasterRenderStage) : EffectPermutationSlot.Invalid;
                    ignoredEffectSlots.Add(shadowMapEffectSlot.Index);
                }
            }

            // Counter number of RenderView to process
            renderViews.Clear();
            foreach (var view in RenderSystem.Views)
            {
                // TODO: Use another mechanism to filter light-indepepndent views (such as shadow casting views)
                if (view.GetType() != typeof(RenderView))
                    continue;

                RenderViewLightData renderViewData;
                if (!renderViewDatas.TryGetValue(view.LightingView ?? view, out renderViewData))
                    continue;

                renderViews.Add(view);
            }

            // Cleanup light renderers
            foreach (var lightRenderer in lightRenderers)
            {
                lightRenderer.Reset();
                lightRenderer.SetViews(renderViews);
            }

            foreach (var view in RenderSystem.Views)
            {
                // TODO: Use another mechanism to filter light-indepepndent views (such as shadow casting views)
                if (view.GetType() != typeof(RenderView))
                    continue;

                RenderViewLightData renderViewData;
                if (!renderViewDatas.TryGetValue(view.LightingView ?? view, out renderViewData))
                    continue;

                // Prepare shader permutations
                PrepareLightGroups(context, renderViews, view, renderViewData, ShadowMapRenderer, RenderGroup.Group0);
            }

            // Add light shader groups using lightRenderers order to make sure we generate same shaders independently of light order
            frameGroups.Reset();
            laidOutSet = null;
            foreach (var lightRenderer in lightRenderers)
            {
                lightRenderer.PrepareResources(context);
                lightRenderer.UpdateShaderPermutationEntry(frameGroups);
            }

            CollectLightingSets();

            // Generate the shader permutation of each set and validate the set's effects with it
            var objectSets = RootRenderFeature.RenderData.GetData(objectSetKey);
            for (var setIndex = 0; setIndex < lightingSetCount; setIndex++)
            {
                var lightingSet = lightingSets[setIndex];
                LayOut(lightingSet);
                GenerateShaders(lightingSet);
                Dispatcher.ForBatched(RootRenderFeature.RenderObjects.Count, new EffectPermutationJob
                {
                    Feature = this,
                    RenderEffects = renderEffects,
                    EffectSlotCount = effectSlotCount,
                    ObjectSets = objectSets,
                    SetIndex = setIndex,
                    LightingSet = lightingSet,
                });
            }
        }

        /// <summary>
        /// Finds the views each render object is drawn in this frame, and gives the object the lighting set of those
        /// views. An object drawn in no lit view gets none: no effect of it is lit this frame.
        /// </summary>
        private void CollectLightingSets()
        {
            var objectViews = RootRenderFeature.RenderData.GetData(objectViewsKey);
            var objectSets = RootRenderFeature.RenderData.GetData(objectSetKey);

            foreach (var renderObject in RootRenderFeature.RenderObjects)
                objectViews[renderObject.StaticObjectNode] = 0;

            for (var viewIndex = 0; viewIndex < renderViews.Count; viewIndex++)
            {
                var viewBit = LightShaderGroup.ViewBit(viewIndex);
                var viewObjects = renderViews[viewIndex].RenderObjects;
                for (var i = 0; i < viewObjects.Count; i++)
                {
                    var renderObject = viewObjects.Items[i];
                    if (renderObject.RenderFeature == RootRenderFeature)
                        objectViews[renderObject.StaticObjectNode] |= viewBit;
                }
            }

            lightingSetCount = 0;
            lightingSetsByViews.Clear();
            foreach (var renderObject in RootRenderFeature.RenderObjects)
            {
                var views = objectViews[renderObject.StaticObjectNode];
                if (views == 0)
                {
                    objectSets[renderObject.StaticObjectNode] = NoLightingSet;
                    continue;
                }

                if (!lightingSetsByViews.TryGetValue(views, out var setIndex))
                {
                    setIndex = FindOrAddLightingSet(views);
                    lightingSetsByViews.Add(views, setIndex);
                }

                objectSets[renderObject.StaticObjectNode] = setIndex;
            }
        }

        /// <summary>
        /// Gives the index of the lighting set of <paramref name="views"/>: a set of the frame those views are lit
        /// like, which then covers them too, or a new one of the groups that hold lights of the views, in the light
        /// renderers' order.
        /// </summary>
        private int FindOrAddLightingSet(ulong views)
        {
            for (var setIndex = 0; setIndex < lightingSetCount; setIndex++)
            {
                var lightingSet = lightingSets[setIndex];
                if (AreLitAlike(lightingSet.ViewMask, views))
                {
                    lightingSet.ViewMask |= views;
                    return setIndex;
                }
            }

            if (lightingSets.Count == lightingSetCount)
                lightingSets.Add(new LightShaderPermutationEntry());

            var newSet = lightingSets[lightingSetCount];
            newSet.Reset();
            newSet.ViewMask = views;
            foreach (var directLightGroup in frameGroups.DirectLightGroups)
            {
                if (directLightGroup.IsUsedIn(views))
                    newSet.DirectLightGroups.Add(directLightGroup);
            }
            foreach (var environmentLight in frameGroups.EnvironmentLights)
            {
                if (environmentLight.IsUsedIn(views))
                    newSet.EnvironmentLights.Add(environmentLight);
            }

            return lightingSetCount++;
        }

        /// <summary>
        /// Whether two sets of views take the same groups with the same light counts, so the same shaders, laid out
        /// alike. The views of both together are then lit like either: a group's count for them is the larger of two
        /// equal counts.
        /// </summary>
        private bool AreLitAlike(ulong views, ulong otherViews)
        {
            foreach (var directLightGroup in frameGroups.DirectLightGroups)
            {
                if (!IsLaidOutAlike(directLightGroup, views, otherViews))
                    return false;
            }
            foreach (var environmentLight in frameGroups.EnvironmentLights)
            {
                if (!IsLaidOutAlike(environmentLight, views, otherViews))
                    return false;
            }

            return true;
        }

        private static bool IsLaidOutAlike(LightShaderGroup lightGroup, ulong views, ulong otherViews)
        {
            var isUsed = lightGroup.IsUsedIn(views);
            return isUsed == lightGroup.IsUsedIn(otherViews)
                && (!isUsed || lightGroup.GetLightCount(views) == lightGroup.GetLightCount(otherViews));
        }

        /// <summary>
        /// Gives every group of <paramref name="lightingSet"/> the composition name of its index in the set and the
        /// set's views to size itself by, unless the groups hold them already.
        /// </summary>
        private void LayOut(LightShaderPermutationEntry lightingSet)
        {
            if (laidOutSet == lightingSet)
                return;

            for (int index = 0; index < lightingSet.DirectLightGroups.Count; index++)
            {
                var directLightGroup = lightingSet.DirectLightGroups[index];
                directLightGroup.SelectViews(lightingSet.ViewMask);
                directLightGroup.UpdateLayout(DirectLightGroupsCompositionNames[index]);
            }
            for (int index = 0; index < lightingSet.EnvironmentLights.Count; index++)
            {
                var environmentLight = lightingSet.EnvironmentLights[index];
                environmentLight.SelectViews(lightingSet.ViewMask);
                environmentLight.UpdateLayout(EnvironmentLightGroupsCompositionNames[index]);
            }

            laidOutSet = lightingSet;
        }

        /// <summary>
        /// Generates the shader permutation of <paramref name="lightingSet"/> out of its groups as they are laid out.
        /// </summary>
        private void GenerateShaders(LightShaderPermutationEntry lightingSet)
        {
            foreach (var directLightGroup in lightingSet.DirectLightGroups)
            {
                lightingSet.DirectLightShaders.Add(directLightGroup.ShaderSource);
                if (directLightGroup.HasEffectPermutations)
                    lightingSet.PermutationLightGroups.Add(directLightGroup);
            }
            foreach (var environmentLight in lightingSet.EnvironmentLights)
            {
                lightingSet.EnvironmentLightShaders.Add(environmentLight.ShaderSource);
                if (environmentLight.HasEffectPermutations)
                    lightingSet.PermutationLightGroups.Add(environmentLight);
            }

            // Make copy so that we can continue to mutate the set's ShaderSourceCollection during next frame
            lightingSet.DirectLightShadersReadOnly = GetReadonlyShaderSources(lightingSet.DirectLightShaders);
            lightingSet.EnvironmentLightShadersReadOnly = GetReadonlyShaderSources(lightingSet.EnvironmentLightShaders);
        }

        private readonly struct EffectPermutationJob : Dispatcher.IBatchJob
        {
            public required ForwardLightingRenderFeature Feature { get; init; }
            public required StaticObjectPropertyData<RenderEffect> RenderEffects { get; init; }
            public required int EffectSlotCount { get; init; }
            public required StaticObjectPropertyData<int> ObjectSets { get; init; }
            public required int SetIndex { get; init; }
            public required LightShaderPermutationEntry LightingSet { get; init; }

            public void Process(int start, int endExclusive)
            {
                foreach (var renderObject in CollectionsMarshal.AsSpan(Feature.RootRenderFeature.RenderObjects)[start..endExclusive])
                {
                    var renderMesh = (RenderMesh)renderObject;

                    if (!renderMesh.MaterialPass.IsLightDependent)
                        continue;

                    var staticObjectNode = renderMesh.StaticObjectNode;

                    // Objects of the other sets are validated with their own
                    if (ObjectSets[staticObjectNode] != SetIndex)
                        continue;

                    for (int i = 0; i < EffectSlotCount; ++i)
                    {
                        // Don't apply lighting for shadow casters
                        if (Feature.ignoredEffectSlots.Contains(i))
                                continue;

                        var staticEffectObjectNode = staticObjectNode * EffectSlotCount + i;
                        var renderEffect = RenderEffects[staticEffectObjectNode];

                        // Skip effects not used during this frame
                        if (renderEffect == null || !renderEffect.IsUsedDuringThisFrame(Feature.RenderSystem))
                            continue;

                        renderEffect.EffectValidator.ValidateParameter(LightingKeys.DirectLightGroups, LightingSet.DirectLightShadersReadOnly);
                        renderEffect.EffectValidator.ValidateParameter(LightingKeys.EnvironmentLights, LightingSet.EnvironmentLightShadersReadOnly);

                        // Some light groups have additional effect permutation
                        foreach (var lightGroup in LightingSet.PermutationLightGroups)
                            lightGroup.ApplyEffectPermutations(renderEffect);
                    }
                }
            }
        }

        /// <summary>
        /// Create a read-only copy of the given shader sources.
        /// </summary>
        private ShaderSourceCollection GetReadonlyShaderSources(ShaderSourceCollection shaderSources)
        {
            // Keyed by the copies themselves, which never change, and found by content
            ShaderSourceCollection readonlyShaderSources;
            if (!shaderSourcesReadonlyCache.TryGetValue(shaderSources, out readonlyShaderSources))
            {
                readonlyShaderSources = new ShaderSourceCollection(shaderSources);
                shaderSourcesReadonlyCache.Add(readonlyShaderSources, readonlyShaderSources);
            }
            return readonlyShaderSources;
        }

        /// <inheritdoc/>
        public override void Prepare(RenderDrawContext context)
        {
            using var _ = Profiler.Begin(PrepareKey);
            var objectSets = RootRenderFeature.RenderData.GetData(objectSetKey);
            int frameCounter = RenderSystem.FrameCounter;

            CollectViewLayoutSets(objectSets);

            for (var setIndex = 0; setIndex < lightingSetCount; setIndex++)
            {
                var lightingSet = lightingSets[setIndex];
                LayOut(lightingSet);

                for (var viewIndex = 0; viewIndex < renderViews.Count; viewIndex++)
                {
                    if ((lightingSet.ViewMask & LightShaderGroup.ViewBit(viewIndex)) == 0)
                        continue;

                    var view = renderViews[viewIndex];
                    var viewFeature = view.Features[RootRenderFeature.Index];
                    if (viewFeature.Layouts.Count == 0)
                        continue;

                    var renderViewData = renderViewDatas[view.LightingView ?? view];

                    // Build/refresh one variant per distinct PerView Lighting layout hash, then bind each
                    // layout's resource group to its matching variant. Light groups' ApplyViewParameters
                    // is idempotent per (view, parameter collection), so it runs once per variant per frame.
                    foreach (var viewLayout in viewFeature.Layouts)
                    {
                        if (viewLayout.State != RenderEffectState.Normal)
                            continue;

                        // Layouts of the other sets are bound with their own
                        if (!viewLayoutSets.TryGetValue((viewLayout, viewIndex), out var layoutSetIndex) || layoutSetIndex != setIndex)
                            continue;

                        var viewLighting = viewLayout.GetLogicalGroup(viewLightingKey);
                        if (viewLighting.Hash == ObjectId.Empty)
                            continue;

                        var variant = renderViewData.GetOrAddVariant(viewLighting.Hash);
                        if (variant.ParameterCollectionLayout == null)
                        {
                            variant.ParameterCollectionLayout = new ParameterCollectionLayout();
                            variant.ParameterCollectionLayout.ProcessLogicalGroup(viewLayout, ref viewLighting);
                            variant.Parameters.UpdateLayout(variant.ParameterCollectionLayout);
                        }

                        if (variant.LastFrameProcessed != frameCounter)
                        {
                            foreach (var directLightGroup in lightingSet.DirectLightGroups)
                                directLightGroup.ApplyViewParameters(context, viewIndex, variant.Parameters);
                            foreach (var environmentLight in lightingSet.EnvironmentLights)
                                environmentLight.ApplyViewParameters(context, viewIndex, variant.Parameters);
                            variant.LastFrameProcessed = frameCounter;
                        }

                        var resourceGroup = viewLayout.Entries[view.Index].Resources;
                        resourceGroup.UpdateLogicalGroup(ref viewLighting, variant.Parameters);
                    }

                    // PerDraw
                    Dispatcher.ForBatched(viewFeature.RenderNodes.Count, new PrepareJob
                    {
                        Feature = this,
                        Context = context,
                        ViewIndex = viewIndex,
                        RenderNodes = viewFeature.RenderNodes,
                        ObjectSets = objectSets,
                        SetIndex = setIndex,
                        LightingSet = lightingSet,
                    });
                }
            }
        }

        /// <summary>
        /// Finds the lighting set of each per-view layout of each lit view: that of any node drawn with it, since the
        /// effects that share a layout were compiled with the same light shaders, the shaders of one set.
        /// </summary>
        private void CollectViewLayoutSets(StaticObjectPropertyData<int> objectSets)
        {
            viewLayoutSets.Clear();
            for (var viewIndex = 0; viewIndex < renderViews.Count; viewIndex++)
            {
                var viewFeature = renderViews[viewIndex].Features[RootRenderFeature.Index];
                var renderNodes = viewFeature.RenderNodes;
                var layoutCount = 0;
                for (var i = 0; i < renderNodes.Count && layoutCount < viewFeature.Layouts.Count; i++)
                {
                    var renderNode = RootRenderFeature.GetRenderNode(renderNodes.Items[i]);

                    // Ignore fallback effects
                    if (renderNode.RenderEffect.State != RenderEffectState.Normal)
                        continue;

                    var viewLayout = renderNode.RenderEffect.Reflection?.PerViewLayout;
                    if (viewLayout != null && viewLayoutSets.TryAdd((viewLayout, viewIndex), objectSets[renderNode.RenderObject.StaticObjectNode]))
                        layoutCount++;
                }
            }
        }

        private readonly struct PrepareJob : Dispatcher.IBatchJob
        {
            public required ForwardLightingRenderFeature Feature { get; init; }
            public required RenderDrawContext Context { get; init; }
            public required int ViewIndex { get; init; }
            public required ConcurrentCollector<RenderNodeReference> RenderNodes { get; init; }
            public required StaticObjectPropertyData<int> ObjectSets { get; init; }
            public required int SetIndex { get; init; }
            public required LightShaderPermutationEntry LightingSet { get; init; }

            public void Process(int start, int endExclusive)
            {
                var locals = Feature.prepareThreadLocals.Value;
                foreach (var renderNodeReference in RenderNodes.Items.AsSpan(start..endExclusive))
                {
                    var renderNode = Feature.RootRenderFeature.GetRenderNode(renderNodeReference);

                    // Ignore fallback effects
                    if (renderNode.RenderEffect.State != RenderEffectState.Normal)
                        continue;

                    // Nodes of the other sets are prepared with their own
                    if (ObjectSets[renderNode.RenderObject.StaticObjectNode] != SetIndex)
                        continue;

                    var drawLayout = renderNode.RenderEffect.Reflection?.PerDrawLayout;
                    if (drawLayout == null)
                        continue;

                    var drawLighting = drawLayout.GetLogicalGroup(Feature.drawLightingKey);
                    if (drawLighting.Hash == ObjectId.Empty)
                        continue;

                    // Rebuild thread-local layout when this node's hash differs from the previous one
                    // processed on this thread (different effects in the same view can produce different
                    // PerDraw Lighting layouts).
                    if (drawLighting.Hash != locals.DrawLayoutHash)
                    {
                        locals.DrawLayoutHash = drawLighting.Hash;

                        var drawParameterLayout = new ParameterCollectionLayout();
                        drawParameterLayout.ProcessLogicalGroup(drawLayout, ref drawLighting);

                        locals.DrawParameters.UpdateLayout(drawParameterLayout);
                    }

                    // Compute PerDraw lighting
                    foreach (var directLightGroup in LightingSet.DirectLightGroups)
                    {
                        directLightGroup.ApplyDrawParameters(Context, ViewIndex, locals.DrawParameters, ref renderNode.RenderObject.BoundingBox);
                    }
                    foreach (var environmentLight in LightingSet.EnvironmentLights)
                    {
                        environmentLight.ApplyDrawParameters(Context, ViewIndex, locals.DrawParameters, ref renderNode.RenderObject.BoundingBox);
                    }

                    // Update resources
                    renderNode.Resources.UpdateLogicalGroup(ref drawLighting, locals.DrawParameters);
                }
            }
        }

        public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage)
        {
            using var _ = Profiler.Begin(DrawKey);
            // Update per-view resources only when view changes
            if (currentRenderView == renderView)
                return;

            var viewFeature = renderView.Features[RootRenderFeature.Index];

            RenderViewLightData renderViewData;
            if (!renderViewDatas.TryGetValue(renderView.LightingView ?? renderView, out renderViewData) || viewFeature.Layouts.Count == 0)
                return;

            var viewIndex = renderViews.IndexOf(renderView);

            // Update PerView resources
            foreach (var directLightGroup in frameGroups.DirectLightGroups)
            {
                directLightGroup.UpdateViewResources(context, viewIndex);
            }

            foreach (var environmentLight in frameGroups.EnvironmentLights)
            {
                environmentLight.UpdateViewResources(context, viewIndex);
            }

            currentRenderView = renderView;
        }

        /// <inheritdoc/>
        public override void Flush(RenderDrawContext context)
        {
            base.Flush(context);
            ShadowMapRenderer?.Flush(context);

            // Invalidate per-view data
            currentRenderView = null;
        }

        protected override void OnRenderSystemChanged()
        {
            if (ShadowMapRenderer != null)
                ShadowMapRenderer.RenderSystem = RenderSystem;
        }

        private void CollectActiveLightRenderers(RenderContext context)
        {
            foreach (var renderViewData in renderViewDatas)
            {
                var viewData = renderViewData.Value;
                viewData.ActiveRenderers.Clear();

                foreach (var p in lightRenderersByType)
                {
                    RenderLightCollectionGroup lightGroup;
                    viewData.ActiveLightGroups.TryGetValue(p.Key, out lightGroup);
                    
                    if (lightGroup != null && lightGroup.Count > 0)
                    {
                        var activeLightGroup = new ActiveLightGroupRenderer(lightGroup, p.Value);
                        viewData.ActiveRenderers.Add(activeLightGroup);
                    }
                }
            }
        }

        /// <summary>
        /// Collects the visible lights by intersecting them with the frustum.
        /// </summary>
        private void CollectVisibleLights()
        {
            foreach (var renderView in RenderSystem.Views)
            {
                if (renderView.GetType() != typeof(RenderView))
                    continue;

                var lightRenderView = renderView.LightingView ?? renderView;

                // Check if already processed
                if (!processedRenderViews.Add(lightRenderView))
                    continue;

                RenderViewLightData renderViewLightData;
                if (!renderViewDatas.TryGetValue(lightRenderView, out renderViewLightData))
                {
                    renderViewLightData = new RenderViewLightData();
                    renderViewDatas.Add(lightRenderView, renderViewLightData);
                }
                else
                {
                    // 1) Clear the cache of current lights (without destroying collections but keeping previously allocated ones)
                    ClearCache(renderViewLightData.ActiveLightGroups);
                }

                renderViewLightData.VisibleLights.Clear();
                renderViewLightData.VisibleLightsWithShadows.Clear();

                var lights = Context.VisibilityGroup.Tags.Get(CurrentLights);

                // No light processors means no light in the scene, so we can early exit
                if (lights == null)
                    continue;

                // TODO GRAPHICS REFACTOR
                var sceneCullingMask = lightRenderView.CullingMask;

                // 2) Cull lights with the frustum
                var frustum = lightRenderView.Frustum;
                foreach (var light in lights)
                {
                    // TODO: New mechanism for light selection (probably in ForwardLighting configuration)
                    //       Light should probably have their own LightGroup (separate from RenderGroup)
                    // If light is not part of the culling mask group, we can skip it
                    //var entityLightMask = (RenderGroupMask)(1 << (int)light.Entity.Group);
                    //if ((entityLightMask & sceneCullingMask) == 0 && (light.CullingMask & sceneCullingMask) == 0)
                    //{
                    //    continue;
                    //}

                    // If light is not in the frustum, we can skip it
                    var directLight = light.Type as IDirectLight;
                    if (directLight != null && directLight.HasBoundingBox && !frustum.Contains(ref light.BoundingBoxExt))
                    {
                        continue;
                    }

                    // Find the group for this light
                    var lightGroup = GetLightGroup(renderViewLightData, light);
                    lightGroup.PrepareLight(light);

                    // This is a visible light
                    renderViewLightData.VisibleLights.Add(light);

                    // Add light to a special list if it has shadows
                    if (directLight != null && directLight.Shadow.Enabled && ShadowMapRenderer != null)
                    {
                        // A visible light with shadows
                        renderViewLightData.VisibleLightsWithShadows.Add(light);
                    }
                }

                // 3) Allocate collection based on their culling mask
                AllocateCollectionsPerGroupOfCullingMask(renderViewLightData.ActiveLightGroups);

                // 4) Collect lights to the correct light collection group
                foreach (var light in renderViewLightData.VisibleLights)
                {
                    var lightGroup = GetLightGroup(renderViewLightData, light);
                    lightGroup.AddLight(light);
                }
            }

            processedRenderViews.Clear();
        }

        private void PrepareLightGroups(RenderDrawContext context, List<RenderView> renderViews, RenderView renderView, RenderViewLightData renderViewData, IShadowMapRenderer shadowMapRenderer, RenderGroup group)
        {
            var viewIndex = renderViews.IndexOf(renderView);

            foreach (var activeRenderer in renderViewData.ActiveRenderers)
            {
                // Find lights
                var lightCollection = activeRenderer.LightGroup.FindLightCollectionByGroup(group);

                // Light collections aren't cleared (see ClearCache). Can be null after switching to empty scenes.
                if (lightCollection is null)
                    continue;

                // Indices of lights in lightCollection that need processing
                lightIndicesToProcess.Clear();
                for (int i = 0; i < lightCollection.Count; i++)
                    lightIndicesToProcess.Add(i);
                
                // Loop over all the renderers in order
                int rendererIndex = 0;
                foreach (var renderer in activeRenderer.Renderers)
                {
                    var processLightsParameters = new LightGroupRendererBase.ProcessLightsParameters
                    {
                        Context = context,
                        ViewIndex = viewIndex,
                        View = renderView,
                        Views = renderViews,
                        Renderers = activeRenderer.Renderers,
                        RendererIndex = rendererIndex++,
                        LightCollection = lightCollection,
                        LightIndices = lightIndicesToProcess,
                        LightType = activeRenderer.LightGroup.LightType,
                        ShadowMapRenderer = shadowMapRenderer,
                        ShadowMapTexturesPerLight = renderViewData.RenderLightsWithShadows,
                    };
                    renderer.ProcessLights(processLightsParameters);
                }
            }
        }

        private static void AllocateCollectionsPerGroupOfCullingMask(Dictionary<Type, RenderLightCollectionGroup> lights)
        {
            foreach (var lightPair in lights)
            {
                lightPair.Value.AllocateCollectionsPerGroupOfCullingMask();
            }
        }

        private static void ClearCache(Dictionary<Type, RenderLightCollectionGroup> lights)
        {
            foreach (var lightPair in lights)
            {
                lightPair.Value.Clear();
            }
        }

        private RenderLightCollectionGroup GetLightGroup(RenderViewLightData renderViewData, RenderLight light)
        {
            RenderLightCollectionGroup lightGroup;

            var directLight = light.Type as IDirectLight;
            var lightGroups = renderViewData.ActiveLightGroups;

            var type = light.Type.GetType();
            if (!lightGroups.TryGetValue(type, out lightGroup))
            {
                lightGroup = new RenderLightCollectionGroup(type);
                lightGroups.Add(type, lightGroup);
            }
            return lightGroup;
        }

        private void LightRenderers_CollectionChanged(object sender, TrackingCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    var item = e.Item as LightGroupRendererBase;
                    item?.Initialize(Context);
                    break;
                }
                case NotifyCollectionChangedAction.Remove:
                {
                    var item = e.OldItem as LightGroupRendererBase;
                    item?.Unload();
                    break;
                }
            }
            EvaluateLightTypes();
        }
        
        private void EvaluateLightTypes()
        {
            lightRenderersByType.Clear();
            foreach (var renderer in lightRenderers)
            {
                foreach (var lightType in renderer.LightTypes)
                {
                    List<LightGroupRendererBase> renderers;
                    if (!lightRenderersByType.TryGetValue(lightType, out renderers))
                        lightRenderersByType.Add(lightType, renderers = new List<LightGroupRendererBase>());
                    renderers.Add(renderer);
                }
            }
        }

        /// <summary>
        /// The light shader groups of a set of views and the shaders they make: the lighting set the effects of the
        /// objects drawn in those views are compiled for.
        /// </summary>
        public class LightShaderPermutationEntry
        {
            public void Reset()
            {
                ViewMask = 0;

                DirectLightGroups.Clear();
                DirectLightShaders.Clear();
                DirectLightShadersReadOnly = null;

                EnvironmentLights.Clear();
                EnvironmentLightShaders.Clear();
                EnvironmentLightShadersReadOnly = null;

                PermutationLightGroups.Clear();
            }

            /// <summary>
            /// The views whose lights the set holds, one bit per view index; see <see cref="LightShaderGroup.ViewBit"/>.
            /// </summary>
            public ulong ViewMask;

            /// <summary>
            /// <see cref="DirectLightShaders"/> as the set's effects were validated with them; never changed afterwards.
            /// </summary>
            public ShaderSourceCollection DirectLightShadersReadOnly;

            /// <summary>
            /// <see cref="EnvironmentLightShaders"/> as the set's effects were validated with them; never changed afterwards.
            /// </summary>
            public ShaderSourceCollection EnvironmentLightShadersReadOnly;

            public FastListStruct<LightShaderGroup> DirectLightGroups = new(8);

            public readonly ShaderSourceCollection DirectLightShaders = new();

            public FastListStruct<LightShaderGroup> EnvironmentLights = new(8);

            public readonly ShaderSourceCollection EnvironmentLightShaders = new();

            /// <summary>
            /// Light groups that have <see cref="LightShaderGroup.HasEffectPermutations"/>.
            /// </summary>
            public FastListStruct<LightShaderGroup> PermutationLightGroups = new(2);
        }

        internal struct ActiveLightGroupRenderer
        {
            public ActiveLightGroupRenderer(RenderLightCollectionGroup lightGroup, IEnumerable<LightGroupRendererBase> lightGroupRenderers)
            {
                LightGroup = lightGroup;
                Renderers = lightGroupRenderers.ToArray();
            }

            /// <summary>
            /// List of renderers that can render lights in this group
            /// </summary>
            public readonly LightGroupRendererBase[] Renderers;

            public readonly RenderLightCollectionGroup LightGroup;
        }

        private class PrepareThreadLocals
        {
            public ObjectId DrawLayoutHash;

            public readonly ParameterCollection DrawParameters = new ParameterCollection();
        }
    }
}
