// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Storage;
using Xenko.Core.Threading;
using Xenko.Graphics;
using Xenko.Shaders;
using Xenko.Shaders.Compiler;

namespace Xenko.Rendering
{
    // TODO: Should we keep that separate or merge it into RootRenderFeature?
    /// <summary>
    /// A root render feature that can manipulate effects.
    /// </summary>
    public abstract class RootEffectRenderFeature : RootRenderFeature
    {
        [ThreadStatic]
        private static CompilerParameters staticCompilerParameters;

        // Helper class to build pipeline state
        private ThreadLocal<PrepareThreadContext> prepareThreadContext;

        private class PrepareThreadContext
        {
            public readonly MutablePipelineState MutablePipelineState;
            public readonly RenderDrawContext Context;

            public PrepareThreadContext(RenderContext renderContext)
            {
                MutablePipelineState = new MutablePipelineState(renderContext.GraphicsDevice);
                Context = renderContext.GetThreadContext();
            }
        }

        private readonly List<string> effectDescriptorSetSlots = new List<string>();
        private readonly Dictionary<string, int> effectPermutationSlots = new Dictionary<string, int>();
        private readonly Dictionary<ObjectId, FrameResourceGroupLayout> frameResourceLayouts = new Dictionary<ObjectId, FrameResourceGroupLayout>();
        private readonly Dictionary<ObjectId, ViewResourceGroupLayout> viewResourceLayouts = new Dictionary<ObjectId, ViewResourceGroupLayout>();

        private readonly Dictionary<ObjectId, DescriptorSetLayout> createdDescriptorSetLayouts = new Dictionary<ObjectId, DescriptorSetLayout>();

        private readonly List<NamedSlotDefinition> frameCBufferOffsetSlots = new List<NamedSlotDefinition>();
        private readonly List<NamedSlotDefinition> viewCBufferOffsetSlots = new List<NamedSlotDefinition>();
        private readonly List<NamedSlotDefinition> drawCBufferOffsetSlots = new List<NamedSlotDefinition>();

        private readonly List<NamedSlotDefinition> viewLogicalGroups = new List<NamedSlotDefinition>();
        private readonly List<NamedSlotDefinition> drawLogicalGroups = new List<NamedSlotDefinition>();

        // Common slots
        private EffectDescriptorSetReference perFrameDescriptorSetSlot;
        private EffectDescriptorSetReference perViewDescriptorSetSlot;
        private EffectDescriptorSetReference perDrawDescriptorSetSlot;

        private EffectPermutationSlot[] effectSlots = null;

        public ConcurrentCollector<EffectObjectNode> EffectObjectNodes { get; } = new ConcurrentCollector<EffectObjectNode>();

        public delegate Effect ComputeFallbackEffectDelegate(RenderObject renderObject, RenderEffect renderEffect, RenderEffectState renderEffectState);

        public ComputeFallbackEffectDelegate ComputeFallbackEffect { get; set; }

        public ResourceGroup[] ResourceGroupPool = new ResourceGroup[256];

        public ConcurrentCollector<FrameResourceGroupLayout> FrameLayouts { get; } = new ConcurrentCollector<FrameResourceGroupLayout>();
        public Action<RenderSystem, Effect, RenderEffectReflection> EffectCompiled;

        [DataMember]
        [Category]
        [MemberCollection(CanReorderItems = true, NotNullItems = true)]
        public List<PipelineProcessor> PipelineProcessors { get; } = new List<PipelineProcessor>();

        public int EffectDescriptorSetSlotCount => effectDescriptorSetSlots.Count;

        /// <summary>
        /// Gets number of effect permutation slot, which is the number of effect cached per object.
        /// </summary>
        public int EffectPermutationSlotCount => effectPermutationSlots.Count;

        /// <summary>
        /// Key to store extra info for each effect instantiation of each object.
        /// </summary>
        public StaticObjectPropertyKey<RenderEffect> RenderEffectKey;

        // TODO: Proper interface to register effects
        /// <summary>
        /// Stores reflection info for each effect.
        /// </summary>
        public Dictionary<Effect, RenderEffectReflection> InstantiatedEffects = new Dictionary<Effect, RenderEffectReflection>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EffectObjectNode GetEffectObjectNode(EffectObjectNodeReference reference)
        {
            return EffectObjectNodes[reference.Index];
        }

        /// <inheritdoc/>
        protected override void InitializeCore()
        {
            base.InitializeCore();

            prepareThreadContext = new ThreadLocal<PrepareThreadContext>(() => new PrepareThreadContext(Context));

            // Create RenderEffectKey
            RenderEffectKey = RenderData.CreateStaticObjectKey<RenderEffect>(null, EffectPermutationSlotCount);

            // TODO: Assign weights so that PerDraw is always last? (we usually most custom user ones to be between PerView and PerDraw)
            perFrameDescriptorSetSlot = GetOrCreateEffectDescriptorSetSlot("PerFrame");
            perViewDescriptorSetSlot = GetOrCreateEffectDescriptorSetSlot("PerView");
            perDrawDescriptorSetSlot = GetOrCreateEffectDescriptorSetSlot("PerDraw");

            RenderSystem.RenderStages.CollectionChanged += RenderStages_CollectionChanged;

            // Create effect slots
            Array.Resize(ref effectSlots, RenderSystem.RenderStages.Count);
            for (int index = 0; index < RenderSystem.RenderStages.Count; index++)
            {
                var renderStage = RenderSystem.RenderStages[index];
                effectSlots[index] = CreateEffectPermutationSlot(renderStage.EffectSlotName);
            }
        }

        /// <summary>
        /// Compute the index of first descriptor set stored in <see cref="ResourceGroupPool"/>.
        /// </summary>
        protected internal int ComputeResourceGroupOffset(RenderNodeReference renderNode)
        {
            return renderNode.Index * effectDescriptorSetSlots.Count;
        }

        public EffectDescriptorSetReference GetOrCreateEffectDescriptorSetSlot(string name)
        {
            // Check if it already exists
            var existingIndex = effectDescriptorSetSlots.IndexOf(name);
            if (existingIndex != -1)
                return new EffectDescriptorSetReference(existingIndex);

            // Otherwise creates it
            effectDescriptorSetSlots.Add(name);
            return new EffectDescriptorSetReference(effectDescriptorSetSlots.Count - 1);
        }

        public ConstantBufferOffsetReference CreateFrameCBufferOffsetSlot(string variable)
        {
            // TODO: Handle duplicates, and allow removal
            var slotReference = new ConstantBufferOffsetReference(frameCBufferOffsetSlots.Count);
            frameCBufferOffsetSlots.Add(new NamedSlotDefinition(variable));

            // Update existing instantiated buffers
            foreach (var frameResourceLayoutEntry in frameResourceLayouts)
            {
                var resourceGroupLayout = frameResourceLayoutEntry.Value;

                // Ensure there is enough space
                if (resourceGroupLayout.ConstantBufferOffsets == null || resourceGroupLayout.ConstantBufferOffsets.Length < frameCBufferOffsetSlots.Count)
                    Array.Resize(ref resourceGroupLayout.ConstantBufferOffsets, frameCBufferOffsetSlots.Count);

                ResolveCBufferOffset(resourceGroupLayout, slotReference.Index, variable);
            }

            return slotReference;
        }

        public ConstantBufferOffsetReference CreateViewCBufferOffsetSlot(string variable)
        {
            // TODO: Handle duplicates
            var slotReference = new ConstantBufferOffsetReference(-1);
            for (int index = 0; index < viewCBufferOffsetSlots.Count; index++)
            {
                var slot = viewCBufferOffsetSlots[index];
                if (slot.Variable == null)
                {
                    // Empty slot, reuse it
                    slotReference = new ConstantBufferOffsetReference(index);
                }
            }

            // Need a new slot
            if (slotReference.Index == -1)
            {
                slotReference = new ConstantBufferOffsetReference(viewCBufferOffsetSlots.Count);
                viewCBufferOffsetSlots.Add(new NamedSlotDefinition(variable));
            }

            // Update existing instantiated buffers
            foreach (var viewResourceLayoutEntry in viewResourceLayouts)
            {
                var resourceGroupLayout = viewResourceLayoutEntry.Value;

                // Ensure there is enough space
                if (resourceGroupLayout.ConstantBufferOffsets == null || resourceGroupLayout.ConstantBufferOffsets.Length < viewCBufferOffsetSlots.Count)
                    Array.Resize(ref resourceGroupLayout.ConstantBufferOffsets, viewCBufferOffsetSlots.Count);

                ResolveCBufferOffset(resourceGroupLayout, slotReference.Index, variable);
            }

            return slotReference;
        }

        public void RemoveViewCBufferOffsetSlot(ConstantBufferOffsetReference cbufferOffsetSlot)
        {
            viewCBufferOffsetSlots[cbufferOffsetSlot.Index] = new NamedSlotDefinition(null);
        }

        public LogicalGroupReference CreateViewLogicalGroup(string logicalGroup)
        {
            // Check existing slots
            for (int i = 0; i < viewLogicalGroups.Count; i++)
            {
                if (viewLogicalGroups[i].Variable.Equals(logicalGroup))
                    return new LogicalGroupReference(i);
            }

            // Need a new slot
            var slotReference = new LogicalGroupReference(viewLogicalGroups.Count);
            viewLogicalGroups.Add(new NamedSlotDefinition(logicalGroup));

            // Update existing instantiated buffers
            foreach (var viewResourceLayoutEntry in viewResourceLayouts)
            {
                var resourceGroupLayout = viewResourceLayoutEntry.Value;

                // Ensure there is enough space
                if (resourceGroupLayout.LogicalGroups == null || resourceGroupLayout.LogicalGroups.Length < viewLogicalGroups.Count)
                    Array.Resize(ref resourceGroupLayout.LogicalGroups, viewLogicalGroups.Count);

                ResolveLogicalGroup(resourceGroupLayout, slotReference.Index, logicalGroup);
            }

            return slotReference;
        }

        public LogicalGroupReference CreateDrawLogicalGroup(string logicalGroup)
        {
            // Check existing slots
            for (int i = 0; i < drawLogicalGroups.Count; i++)
            {
                if (drawLogicalGroups[i].Variable.Equals(logicalGroup))
                    return new LogicalGroupReference(i);
            }

            // Need a new slot
            var slotReference = new LogicalGroupReference(drawLogicalGroups.Count);
            drawLogicalGroups.Add(new NamedSlotDefinition(logicalGroup));

            // Update existing instantiated buffers
            foreach (var effect in InstantiatedEffects)
            {
                var resourceGroupLayout = effect.Value.PerDrawLayout;

                // Ensure there is enough space
                if (resourceGroupLayout.LogicalGroups == null || resourceGroupLayout.LogicalGroups.Length < drawLogicalGroups.Count)
                    Array.Resize(ref resourceGroupLayout.LogicalGroups, drawLogicalGroups.Count);

                ResolveLogicalGroup(resourceGroupLayout, slotReference.Index, logicalGroup);
            }

            return slotReference;
        }

        public ConstantBufferOffsetReference CreateDrawCBufferOffsetSlot(string variable)
        {
            // TODO: Handle duplicates, and allow removal
            var slotReference = new ConstantBufferOffsetReference(drawCBufferOffsetSlots.Count);
            drawCBufferOffsetSlots.Add(new NamedSlotDefinition(variable));

            // Update existing instantiated buffers
            foreach (var effect in InstantiatedEffects)
            {
                var resourceGroupLayout = effect.Value.PerDrawLayout;

                // Ensure there is enough space
                if (resourceGroupLayout.ConstantBufferOffsets == null || resourceGroupLayout.ConstantBufferOffsets.Length < drawCBufferOffsetSlots.Count)
                    Array.Resize(ref resourceGroupLayout.ConstantBufferOffsets, drawCBufferOffsetSlots.Count);

                ResolveCBufferOffset(resourceGroupLayout, slotReference.Index, variable);
            }

            return slotReference;
        }

        private void ResolveLogicalGroup(RenderSystemResourceGroupLayout resourceGroupLayout, int index, string logicalGroupName)
        {
            // Update slot
            resourceGroupLayout.LogicalGroups[index] = resourceGroupLayout.CreateLogicalGroup(logicalGroupName);
        }

        private void ResolveCBufferOffset(RenderSystemResourceGroupLayout resourceGroupLayout, int index, string variable)
        {
            // Update slot
            if (resourceGroupLayout.ConstantBufferReflection != null)
            {
                foreach (var member in resourceGroupLayout.ConstantBufferReflection.Members)
                {
                    if (member.KeyInfo.KeyName == variable)
                    {
                        resourceGroupLayout.ConstantBufferOffsets[index] = member.Offset;
                        return;
                    }
                }
            }

            // Not found?
            resourceGroupLayout.ConstantBufferOffsets[index] = -1;
        }

        /// <summary>
        /// Gets the effect slot for a given render stage.
        /// </summary>
        /// <param name="renderStage"></param>
        /// <returns></returns>
        public EffectPermutationSlot GetEffectPermutationSlot(RenderStage renderStage)
        {
            return effectSlots[renderStage.Index];
        }

        /// <summary>
        /// Creates a slot for storing a particular effect instantiation (per RenderObject).
        /// </summary>
        /// As an example, we could have main shader (automatically created), GBuffer shader and shadow mapping shader.
        /// <returns></returns>
        public EffectPermutationSlot CreateEffectPermutationSlot(string effectName)
        {
            // Allocate effect slot
            // TODO: Should we allow/support this to be called after Initialize()?
            int slot;
            if (!effectPermutationSlots.TryGetValue(effectName, out slot))
            {
                if (effectPermutationSlots.Count >= 32)
                {
                    throw new InvalidOperationException("Only 32 effect slots are currently allowed for meshes");
                }

                slot = effectPermutationSlots.Count;
                effectPermutationSlots.Add(effectName, slot);

                // Add render effect slot
                RenderData.ChangeDataMultiplier(RenderEffectKey, EffectPermutationSlotCount);
            }

            return new EffectPermutationSlot(slot);
        }

        /// <summary>
        /// This is a subpart of effect permutation preparation:
        ///  set the shader classes that are going to be responsible to compute extended render target colors.
        /// </summary>
        /// <param name="context"></param>
        private void PrepareRenderTargetExtensionsMixins(RenderDrawContext context)
        {
            var renderEffectKey = RenderEffectKey;
            var renderEffects = RenderData.GetData(renderEffectKey);

            // TODO dispatcher
            foreach (var node in RenderNodes)
            {
                var renderNode = node;
                var renderObject = renderNode.RenderObject;

                // Get RenderEffect
                var staticObjectNode = renderObject.StaticObjectNode;
                var staticEffectObjectNode = staticObjectNode * EffectPermutationSlotCount + effectSlots[renderNode.RenderStage.Index].Index;
                var renderEffect = renderEffects[staticEffectObjectNode];

                if (renderEffect != null)
                {
                    var renderStage = renderNode.RenderStage;
                    var renderStageShaderSource = renderStage.OutputValidator.ShaderSource;
                    if (renderStageShaderSource != null)
                        renderEffect.EffectValidator.ValidateParameter(XenkoEffectBaseKeys.RenderTargetExtensions, renderStageShaderSource);
                }
            }
        }

        /// <summary>
        /// Actual implementation of <see cref="PrepareEffectPermutations"/>.
        /// </summary>
        /// <param name="context"></param>
        public virtual void PrepareEffectPermutationsImpl(RenderDrawContext context)
        {
        }

        /// <param name="context"></param>
        /// <inheritdoc/>
        public override void PrepareEffectPermutations(RenderDrawContext context)
        {
            base.PrepareEffectPermutations(context);

            // TODO: Temporary until we have a better system for handling permutations
            var renderEffects = RenderData.GetData(RenderEffectKey);
            int effectSlotCount = EffectPermutationSlotCount;

            Dispatcher.ForEach(RenderSystem.Views, view =>
            {
                var viewFeature = view.Features[Index];
                Dispatcher.ForEach(viewFeature.RenderNodes, renderNodeReference =>
                {
                    var renderNode = this.GetRenderNode(renderNodeReference);
                    var renderObject = renderNode.RenderObject;

                    // Get RenderEffect
                    var staticObjectNode = renderObject.StaticObjectNode;
                    var staticEffectObjectNode = staticObjectNode * effectSlotCount + effectSlots[renderNode.RenderStage.Index].Index;
                    var renderEffect = renderEffects[staticEffectObjectNode];

                    var effectSelector = renderObject.ActiveRenderStages[renderNode.RenderStage.Index].EffectSelector;

                    // Create it (first time) or regenerate it if effect changed
                    if (renderEffect == null || effectSelector != renderEffect.EffectSelector)
                    {
                        renderEffect = new RenderEffect(renderObject.ActiveRenderStages[renderNode.RenderStage.Index].EffectSelector);
                        renderEffects[staticEffectObjectNode] = renderEffect;
                    }

                    // Is it the first time this frame that we check this RenderEffect?
                    if (renderEffect.MarkAsUsed(RenderSystem))
                    {
                        renderEffect.EffectValidator.BeginEffectValidation();
                    }
                });
            });

            // Step1: Perform permutations
            PrepareEffectPermutationsImpl(context);

            PrepareRenderTargetExtensionsMixins(context);

            var currentTime = DateTime.UtcNow;

            // Step2: Compile effects
            Dispatcher.ForEach(RenderObjects, renderObject =>
            {
                //var renderObject = RenderObjects[index];
                var staticObjectNode = renderObject.StaticObjectNode;

                for (int i = 0; i < effectSlotCount; ++i)
                {
                    var staticEffectObjectNode = staticObjectNode * effectSlotCount + i;
                    var renderEffect = renderEffects[staticEffectObjectNode];

                    // Skip if not used
                    if (renderEffect == null)
                        continue;

                    // Skip reflection update unless a state change requires it
                    renderEffect.IsReflectionUpdateRequired = false;

                    if (!renderEffect.IsUsedDuringThisFrame(RenderSystem))
                        continue;

                    // Skip if nothing changed
                    if (renderEffect.EffectValidator.ShouldSkip)
                    {
                        // Reset pending effect, as it is now obsolete anyway
                        renderEffect.Effect = null;
                        renderEffect.State = RenderEffectState.Skip;
                    }
                    else if (renderEffect.EffectValidator.EndEffectValidation() && (renderEffect.Effect == null || !renderEffect.Effect.SourceChanged) && !(renderEffect.State == RenderEffectState.Error && currentTime >= renderEffect.RetryTime))
                    {
                        InvalidateEffectPermutation(renderObject, renderEffect);

                        // Still, let's check if there is a pending effect compiling
                        var pendingEffect = renderEffect.PendingEffect;
                        if (pendingEffect == null || !pendingEffect.IsCompleted)
                            continue;

                        renderEffect.ClearFallbackParameters();
                        if (pendingEffect.IsFaulted)
                        {
                            // The effect can fail compilation asynchronously
                            renderEffect.State = RenderEffectState.Error;
                            renderEffect.Effect = ComputeFallbackEffect?.Invoke(renderObject, renderEffect, RenderEffectState.Error);
                        }
                        else
                        {
                            renderEffect.State = RenderEffectState.Normal;
                            renderEffect.Effect = pendingEffect.Result;
                        }
                        renderEffect.PendingEffect = null;
                    }
                    else
                    {
                        // Reset pending effect, as it is now obsolete anyway
                        renderEffect.PendingEffect = null;
                        renderEffect.State = RenderEffectState.Normal;

                        // CompilerParameters are ThreadStatic
                        if (staticCompilerParameters == null)
                            staticCompilerParameters = new CompilerParameters();

                        foreach (var effectValue in renderEffect.EffectValidator.EffectValues)
                        {
                            staticCompilerParameters.SetObject(effectValue.Key, effectValue.Value);
                        }

                        TaskOrResult<Effect> asyncEffect;
                        try
                        {
                            // The effect can fail compilation synchronously
                            asyncEffect = RenderSystem.EffectSystem.LoadEffect(renderEffect.EffectSelector.EffectName, staticCompilerParameters);
                            staticCompilerParameters.Clear();
                        }
                        catch
                        {
                            staticCompilerParameters.Clear();
                            renderEffect.ClearFallbackParameters();
                            renderEffect.State = RenderEffectState.Error;
                            renderEffect.Effect = ComputeFallbackEffect?.Invoke(renderObject, renderEffect, RenderEffectState.Error);
                            continue;
                        }

                        renderEffect.Effect = asyncEffect.Result;
                        if (renderEffect.Effect == null)
                        {
                            // Effect still compiling, let's find if there is a fallback
                            renderEffect.ClearFallbackParameters();
                            renderEffect.PendingEffect = asyncEffect.Task;
                            renderEffect.State = RenderEffectState.Compiling;
                            renderEffect.Effect = ComputeFallbackEffect?.Invoke(renderObject, renderEffect, RenderEffectState.Compiling);
                        }
                    }

                    renderEffect.IsReflectionUpdateRequired = true;
                }
            });

            // Step3: Uupdate reflection infos (offset, etc...)
            foreach (var renderObject in RenderObjects)
            {
                var staticObjectNode = renderObject.StaticObjectNode;

                for (int i = 0; i < effectSlotCount; ++i)
                {
                    var staticEffectObjectNode = staticObjectNode * effectSlotCount + i;
                    var renderEffect = renderEffects[staticEffectObjectNode];

                    // Skip if not used
                    if (renderEffect == null || !renderEffect.IsReflectionUpdateRequired)
                        continue;

                    var effect = renderEffect.Effect;
                    if (effect == null && renderEffect.State == RenderEffectState.Compiling)
                    {
                        // Need to wait for completion because we have nothing else
                        renderEffect.PendingEffect.Wait();

                        if (!renderEffect.PendingEffect.IsFaulted)
                        {
                            renderEffect.Effect = effect = renderEffect.PendingEffect.Result;
                            renderEffect.State = RenderEffectState.Normal;
                        }
                        else
                        {
                            renderEffect.ClearFallbackParameters();
                            renderEffect.State = RenderEffectState.Error;
                            renderEffect.Effect = effect = ComputeFallbackEffect?.Invoke(renderObject, renderEffect, RenderEffectState.Error);
                        }
                    }

                    var effectHashCode = effect != null ? (uint)effect.GetHashCode() : 0;

                    // Effect is last 16 bits
                    renderObject.StateSortKey = (renderObject.StateSortKey & 0xFFFF0000) | (effectHashCode & 0x0000FFFF);

                    if (effect != null)
                    {
                        RenderEffectReflection renderEffectReflection;
                        if (!InstantiatedEffects.TryGetValue(effect, out renderEffectReflection))
                        {
                            renderEffectReflection = new RenderEffectReflection();

                            // Build root signature automatically from reflection
                            renderEffectReflection.DescriptorReflection = EffectDescriptorSetReflection.New(RenderSystem.GraphicsDevice, effect.Bytecode, effectDescriptorSetSlots, "PerFrame");
                            renderEffectReflection.ResourceGroupDescriptions = new ResourceGroupDescription[renderEffectReflection.DescriptorReflection.Layouts.Count];

                            // Compute ResourceGroup hashes
                            for (int index = 0; index < renderEffectReflection.DescriptorReflection.Layouts.Count; index++)
                            {
                                var descriptorSet = renderEffectReflection.DescriptorReflection.Layouts[index];
                                if (descriptorSet.Layout == null)
                                    continue;

                                var constantBufferReflection = effect.Bytecode.Reflection.ConstantBuffers.FirstOrDefault(x => x.Name == descriptorSet.Name);

                                renderEffectReflection.ResourceGroupDescriptions[index] = new ResourceGroupDescription(descriptorSet.Layout, constantBufferReflection);
                            }

                            renderEffectReflection.RootSignature = RootSignature.New(RenderSystem.GraphicsDevice, renderEffectReflection.DescriptorReflection);
                            renderEffectReflection.BufferUploader.Compile(RenderSystem.GraphicsDevice, renderEffectReflection.DescriptorReflection, effect.Bytecode);

                            // Prepare well-known descriptor set layouts
                            renderEffectReflection.PerDrawLayout = CreateDrawResourceGroupLayout(renderEffectReflection.ResourceGroupDescriptions[perDrawDescriptorSetSlot.Index], renderEffect.State);
                            renderEffectReflection.PerFrameLayout = CreateFrameResourceGroupLayout(renderEffectReflection.ResourceGroupDescriptions[perFrameDescriptorSetSlot.Index], renderEffect.State);
                            renderEffectReflection.PerViewLayout = CreateViewResourceGroupLayout(renderEffectReflection.ResourceGroupDescriptions[perViewDescriptorSetSlot.Index], renderEffect.State);

                            InstantiatedEffects.Add(effect, renderEffectReflection);

                            // Notify a new effect has been compiled
                            EffectCompiled?.Invoke(RenderSystem, effect, renderEffectReflection);
                        }

                        // Setup fallback parameters
                        if (renderEffect.State != RenderEffectState.Normal && renderEffectReflection.FallbackUpdaterLayout == null)
                        {
                            // Process all "non standard" layouts
                            var layoutMapping = new int[renderEffectReflection.DescriptorReflection.Layouts.Count - 3];
                            var layouts = new DescriptorSetLayoutBuilder[renderEffectReflection.DescriptorReflection.Layouts.Count - 3];
                            int layoutMappingIndex = 0;
                            for (int index = 0; index < renderEffectReflection.DescriptorReflection.Layouts.Count; index++)
                            {
                                var layout = renderEffectReflection.DescriptorReflection.Layouts[index];

                                // Skip well-known layouts (already handled)
                                if (layout.Name == "PerDraw" || layout.Name == "PerFrame" || layout.Name == "PerView")
                                    continue;

                                layouts[layoutMappingIndex] = layout.Layout;
                                layoutMapping[layoutMappingIndex++] = index;
                            }

                            renderEffectReflection.FallbackUpdaterLayout = new EffectParameterUpdaterLayout(RenderSystem.GraphicsDevice, effect, layouts);
                            renderEffectReflection.FallbackResourceGroupMapping = layoutMapping;
                        }

                        // Update effect
                        renderEffect.Effect = effect;
                        renderEffect.Reflection = renderEffectReflection;

                        // Invalidate pipeline state (new effect)
                        renderEffect.PipelineState = null;

                        renderEffects[staticEffectObjectNode] = renderEffect;
                    }
                    else
                    {
                        renderEffect.Reflection = RenderEffectReflection.Empty;
                        renderEffect.PipelineState = null;
                    }
                }
            }
        }

        /// <summary>
        /// Implemented by subclasses to reset effect dependent data.
        /// </summary>
        protected virtual void InvalidateEffectPermutation(RenderObject renderObject, RenderEffect renderEffect)
        {
        }

        /// <inheritdoc/>
        public override void Prepare(RenderDrawContext context)
        {
            EffectObjectNodes.Clear(false);

            // Make sure descriptor set pool is large enough
            var expectedDescriptorSetPoolSize = RenderNodes.Count * effectDescriptorSetSlots.Count;
            if (ResourceGroupPool.Length < expectedDescriptorSetPoolSize)
                Array.Resize(ref ResourceGroupPool, expectedDescriptorSetPoolSize);

            // Allocate PerFrame, PerView and PerDraw resource groups and constant buffers
            var renderEffects = RenderData.GetData(RenderEffectKey);
            int effectSlotCount = EffectPermutationSlotCount;
            foreach (var view in RenderSystem.Views)
            {
                var viewFeature = view.Features[Index];

                Dispatcher.ForEach(viewFeature.RenderNodes, () => prepareThreadContext.Value, (renderNodeReference, batch) =>
                {
                    var threadContext = batch.Context;
                    var renderNode = this.GetRenderNode(renderNodeReference);
                    var renderObject = renderNode.RenderObject;

                    // Get RenderEffect
                    var staticObjectNode = renderObject.StaticObjectNode;
                    var staticEffectObjectNode = staticObjectNode * effectSlotCount + effectSlots[renderNode.RenderStage.Index].Index;
                    var renderEffect = renderEffects[staticEffectObjectNode];

                    // Not compiled yet?
                    if (renderEffect.Effect == null)
                    {
                        renderNode.RenderEffect = renderEffect;
                        renderNode.EffectObjectNode = EffectObjectNodeReference.Invalid;
                        renderNode.Resources = null;
                        RenderNodes[renderNodeReference.Index] = renderNode;
                        return;
                    }

                    var renderEffectReflection = renderEffect.Reflection;

                    // PerView resources/cbuffer
                    var viewLayout = renderEffectReflection.PerViewLayout;
                    if (viewLayout != null)
                    {
                        var viewCount = RenderSystem.Views.Count;
                        if (viewLayout.Entries?.Length < viewCount)
                        {
                            // TODO: Should this be a first loop?
                            lock (viewLayout)
                            {
                                if (viewLayout.Entries?.Length < viewCount)
                                {
                                    var newEntries = new ResourceGroupEntry[viewCount];

                                    for (int index = 0; index < viewLayout.Entries.Length; index++)
                                        newEntries[index] = viewLayout.Entries[index];

                                    for (int index = viewLayout.Entries.Length; index < viewCount; index++)
                                        newEntries[index].Resources = new ResourceGroup();

                                    viewLayout.Entries = newEntries;
                                }
                            }
                        }

                        if (viewLayout.Entries[view.Index].MarkAsUsed(RenderSystem))
                        {
                            threadContext.ResourceGroupAllocator.PrepareResourceGroup(viewLayout, BufferPoolAllocationType.UsedMultipleTime, viewLayout.Entries[view.Index].Resources);

                            // Register it in list of view layouts to update for this frame
                            viewFeature.Layouts.Add(viewLayout);
                        }
                    }

                    // PerFrame resources/cbuffer
                    var frameLayout = renderEffect.Reflection.PerFrameLayout;
                    if (frameLayout != null && frameLayout.Entry.MarkAsUsed(RenderSystem))
                    {
                        threadContext.ResourceGroupAllocator.PrepareResourceGroup(frameLayout, BufferPoolAllocationType.UsedMultipleTime, frameLayout.Entry.Resources);

                        // Register it in list of view layouts to update for this frame
                        FrameLayouts.Add(frameLayout);
                    }

                    // PerDraw resources/cbuffer
                    // Get nodes
                    var viewObjectNode = GetViewObjectNode(renderNode.ViewObjectNode);

                    // Allocate descriptor set
                    renderNode.Resources = threadContext.ResourceGroupAllocator.AllocateResourceGroup();
                    if (renderEffectReflection.PerDrawLayout != null)
                    {
                        threadContext.ResourceGroupAllocator.PrepareResourceGroup(renderEffectReflection.PerDrawLayout, BufferPoolAllocationType.UsedOnce, renderNode.Resources);
                    }

                    // Create EffectObjectNode
                    var effectObjectNodeIndex = EffectObjectNodes.Add(new EffectObjectNode(renderEffect, viewObjectNode.ObjectNode));

                    // Link to EffectObjectNode (created right after)
                    // TODO: rewrite this
                    renderNode.EffectObjectNode = new EffectObjectNodeReference(effectObjectNodeIndex);

                    renderNode.RenderEffect = renderEffect;
                    
                    // Bind well-known descriptor sets
                    var descriptorSetPoolOffset = ComputeResourceGroupOffset(renderNodeReference);
                    ResourceGroupPool[descriptorSetPoolOffset + perFrameDescriptorSetSlot.Index] = frameLayout?.Entry.Resources;
                    ResourceGroupPool[descriptorSetPoolOffset + perViewDescriptorSetSlot.Index] = renderEffect.Reflection.PerViewLayout?.Entries[view.Index].Resources;
                    ResourceGroupPool[descriptorSetPoolOffset + perDrawDescriptorSetSlot.Index] = renderNode.Resources;

                    // Create resource group for everything else in case of fallback effects
                    if (renderEffect.State != RenderEffectState.Normal && renderEffect.FallbackParameters != null)
                    {
                        if (renderEffect.FallbackParameterUpdater.ResourceGroups == null)
                        {
                            // First time
                            renderEffect.FallbackParameterUpdater = new EffectParameterUpdater(renderEffect.Reflection.FallbackUpdaterLayout, renderEffect.FallbackParameters);
                        }

                        renderEffect.FallbackParameterUpdater.Update(RenderSystem.GraphicsDevice, threadContext.ResourceGroupAllocator, renderEffect.FallbackParameters);

                        var fallbackResourceGroupMapping = renderEffect.Reflection.FallbackResourceGroupMapping;
                        for (int i = 0; i < fallbackResourceGroupMapping.Length; ++i)
                        {
                            ResourceGroupPool[descriptorSetPoolOffset + fallbackResourceGroupMapping[i]] = renderEffect.FallbackParameterUpdater.ResourceGroups[i];
                        }
                    }

                    // Compile pipeline state object (if first time or need change)
                    // TODO GRAPHICS REFACTOR how to invalidate if we want to change some state? (setting to null should be fine)
                    if (renderEffect.PipelineState == null)
                    {
                        var mutablePipelineState = batch.MutablePipelineState;
                        var pipelineState = mutablePipelineState.State;
                        pipelineState.SetDefaults();

                        // Effect
                        pipelineState.EffectBytecode = renderEffect.Effect.Bytecode;
                        pipelineState.RootSignature = renderEffect.Reflection.RootSignature;

                        // Extract outputs from render stage
                        pipelineState.Output = renderNode.RenderStage.Output;
                        pipelineState.RasterizerState.MultisampleCount = renderNode.RenderStage.Output.MultisampleCount;

                        // Bind VAO
                        ProcessPipelineState(Context, renderNodeReference, ref renderNode, renderObject, pipelineState);

                        foreach (var pipelineProcessor in PipelineProcessors)
                            pipelineProcessor.Process(renderNodeReference, ref renderNode, renderObject, pipelineState);

                        mutablePipelineState.Update();
                        renderEffect.PipelineState = mutablePipelineState.CurrentState;
                    }

                    RenderNodes[renderNodeReference.Index] = renderNode;
                });

                viewFeature.RenderNodes.Close();
                viewFeature.Layouts.Close();
            }

            EffectObjectNodes.Close();
            FrameLayouts.Close();
        }

        protected virtual void ProcessPipelineState(RenderContext context, RenderNodeReference renderNodeReference, ref RenderNode renderNode, RenderObject renderObject, PipelineStateDescription pipelineState)
        {
        }

        public override void Reset()
        {
            base.Reset();
            FrameLayouts.Clear(false);
        }

        public DescriptorSetLayout CreateUniqueDescriptorSetLayout(DescriptorSetLayoutBuilder descriptorSetLayoutBuilder)
        {
            DescriptorSetLayout descriptorSetLayout;

            if (!createdDescriptorSetLayouts.TryGetValue(descriptorSetLayoutBuilder.Hash, out descriptorSetLayout))
            {
                descriptorSetLayout = DescriptorSetLayout.New(RenderSystem.GraphicsDevice, descriptorSetLayoutBuilder);
                createdDescriptorSetLayouts.Add(descriptorSetLayoutBuilder.Hash, descriptorSetLayout);
            }

            return descriptorSetLayout;
        }

        private RenderSystemResourceGroupLayout CreateDrawResourceGroupLayout(ResourceGroupDescription resourceGroupDescription, RenderEffectState effectState)
        {
            if (resourceGroupDescription.DescriptorSetLayout == null)
                return null;

            var result = new RenderSystemResourceGroupLayout
            {
                DescriptorSetLayoutBuilder = resourceGroupDescription.DescriptorSetLayout,
                DescriptorSetLayout = DescriptorSetLayout.New(RenderSystem.GraphicsDevice, resourceGroupDescription.DescriptorSetLayout),
                ConstantBufferReflection = resourceGroupDescription.ConstantBufferReflection,
            };

            result.State = effectState;

            if (resourceGroupDescription.ConstantBufferReflection != null)
            {
                result.ConstantBufferSize = resourceGroupDescription.ConstantBufferReflection.Size;
                result.ConstantBufferHash = resourceGroupDescription.ConstantBufferReflection.Hash;
            }

            // Resolve slots
            result.ConstantBufferOffsets = new int[drawCBufferOffsetSlots.Count];
            for (int index = 0; index < drawCBufferOffsetSlots.Count; index++)
            {
                ResolveCBufferOffset(result, index, drawCBufferOffsetSlots[index].Variable);
            }

            // Resolve logical groups
            result.LogicalGroups = new LogicalGroup[drawLogicalGroups.Count];
            for (int index = 0; index < drawLogicalGroups.Count; index++)
            {
                ResolveLogicalGroup(result, index, drawLogicalGroups[index].Variable);
            }

            return result;
        }

        private FrameResourceGroupLayout CreateFrameResourceGroupLayout(ResourceGroupDescription resourceGroupDescription, RenderEffectState effectState)
        {
            if (resourceGroupDescription.DescriptorSetLayout == null)
                return null;

            // We combine both hash for DescriptorSet and cbuffer itself (if it exists)
            var hash = resourceGroupDescription.Hash;
            var effectStateHash = new ObjectId(0, 0, 0, (uint)effectState);
            ObjectId.Combine(ref effectStateHash, ref hash, out hash);

            FrameResourceGroupLayout result;
            if (!frameResourceLayouts.TryGetValue(hash, out result))
            {
                result = new FrameResourceGroupLayout
                {
                    DescriptorSetLayoutBuilder = resourceGroupDescription.DescriptorSetLayout,
                    DescriptorSetLayout = DescriptorSetLayout.New(RenderSystem.GraphicsDevice, resourceGroupDescription.DescriptorSetLayout),
                    ConstantBufferReflection = resourceGroupDescription.ConstantBufferReflection,
                    State = effectState,
                };

                result.Entry.Resources = new ResourceGroup();

                if (resourceGroupDescription.ConstantBufferReflection != null)
                {
                    result.ConstantBufferSize = resourceGroupDescription.ConstantBufferReflection.Size;
                    result.ConstantBufferHash = resourceGroupDescription.ConstantBufferReflection.Hash;
                }

                // Resolve slots
                result.ConstantBufferOffsets = new int[frameCBufferOffsetSlots.Count];
                for (int index = 0; index < frameCBufferOffsetSlots.Count; index++)
                {
                    ResolveCBufferOffset(result, index, frameCBufferOffsetSlots[index].Variable);
                }

                frameResourceLayouts.Add(hash, result);
            }

            return result;
        }

        private ViewResourceGroupLayout CreateViewResourceGroupLayout(ResourceGroupDescription resourceGroupDescription, RenderEffectState effectState)
        {
            if (resourceGroupDescription.DescriptorSetLayout == null)
                return null;

            // We combine both hash for DescriptorSet and cbuffer itself (if it exists)
            var hash = resourceGroupDescription.Hash;
            var effectStateHash = new ObjectId(0, 0, 0, (uint)effectState);
            ObjectId.Combine(ref effectStateHash, ref hash, out hash);

            ViewResourceGroupLayout result;
            if (!viewResourceLayouts.TryGetValue(hash, out result))
            {
                result = new ViewResourceGroupLayout
                {
                    DescriptorSetLayoutBuilder = resourceGroupDescription.DescriptorSetLayout,
                    DescriptorSetLayout = DescriptorSetLayout.New(RenderSystem.GraphicsDevice, resourceGroupDescription.DescriptorSetLayout),
                    ConstantBufferReflection = resourceGroupDescription.ConstantBufferReflection,
                    Entries = new ResourceGroupEntry[RenderSystem.Views.Count],
                    State = effectState,
                };

                for (int index = 0; index < result.Entries.Length; index++)
                {
                    result.Entries[index].Resources = new ResourceGroup();
                }

                if (resourceGroupDescription.ConstantBufferReflection != null)
                {
                    result.ConstantBufferSize = resourceGroupDescription.ConstantBufferReflection.Size;
                    result.ConstantBufferHash = resourceGroupDescription.ConstantBufferReflection.Hash;
                }

                // Resolve slots
                result.ConstantBufferOffsets = new int[viewCBufferOffsetSlots.Count];
                for (int index = 0; index < viewCBufferOffsetSlots.Count; index++)
                {
                    ResolveCBufferOffset(result, index, viewCBufferOffsetSlots[index].Variable);
                }

                // Resolve logical groups
                result.LogicalGroups = new LogicalGroup[viewLogicalGroups.Count];
                for (int index = 0; index < viewLogicalGroups.Count; index++)
                {
                    ResolveLogicalGroup(result, index, viewLogicalGroups[index].Variable);
                }

                viewResourceLayouts.Add(hash, result);
            }

            return result;
        }

        protected override int ComputeDataArrayExpectedSize(DataType type)
        {
            switch (type)
            {
                case DataType.EffectObject:
                    return EffectObjectNodes.Count;
            }

            return base.ComputeDataArrayExpectedSize(type);
        }

        private void RenderStages_CollectionChanged(object sender, ref Core.Collections.FastTrackingCollectionChangedEventArgs e)
        {
            var renderStage = (RenderStage)e.Item;

            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    Array.Resize(ref effectSlots, RenderSystem.RenderStages.Count);
                    effectSlots[e.Index] = CreateEffectPermutationSlot(renderStage.EffectSlotName);
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    // TODO GRAPHICS REFACTOR support removal of render stages
                    throw new NotImplementedException();
            }
        }

        protected override void Destroy()
        {
            foreach (var effect in InstantiatedEffects)
            {
                var effectReflection = effect.Value;
                effectReflection.RootSignature.Dispose();
                effectReflection.PerDrawLayout?.DescriptorSetLayout.Dispose();
                effectReflection.PerViewLayout?.DescriptorSetLayout.Dispose();
                effectReflection.PerFrameLayout?.DescriptorSetLayout.Dispose();
            }

            prepareThreadContext?.Dispose();
            prepareThreadContext = null;

            base.Destroy();
        }

        private struct NamedSlotDefinition
        {
            public readonly string Variable;

            public NamedSlotDefinition(string variable)
            {
                Variable = variable;
            }
        }
    }
}
