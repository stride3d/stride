// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;

using Stride.Core;
using Stride.Graphics;

namespace Stride.Rendering
{
    /// <summary>
    ///   Represents an instance of an Effect, including its values and Graphics Resources to bind
    ///   (such as <see cref="Texture"/>s, <see cref="Buffer"/>s, or <see cref="SamplerState"/>s).
    /// </summary>
    /// <param name="effect">The Effect that will be used by the Effect instance.</param>
    /// <param name="parameters">
    ///   An optional collection of parameters that can be used to customize the Effect instance.
    ///   Specify <see langword="null"/> to use the default parameters.
    /// </param>
    public class EffectInstance(Effect effect, ParameterCollection? parameters = null) : DisposeBase
    {
        /// <summary>
        ///   Gets the Effect currently used by this instance.
        /// </summary>
        public Effect Effect { get; protected set; } = effect;

        protected int permutationCounter;  // TODO: Shoud be named permutationVersion, as it is to know when the permutation has changed

        // Describes how to update resource bindings
        private ResourceGroupBufferUploader bufferUploader;

        private DescriptorSet[] descriptorSets;

        private EffectParameterUpdater parameterUpdater;

        /// <summary>
        ///   Gets a reflection object that describes the Descriptor Sets and their layouts for the Effect
        ///   used by this instance.
        ///   <br/>
        ///   This includes the bindings for Graphics Resources such as <see cref="Texture"/>s, <see cref="Buffer"/>s,
        ///   and <see cref="SamplerState"/>s.
        /// </summary>
        public EffectDescriptorSetReflection DescriptorReflection { get; private set; }

        /// <summary>
        ///   Gets the Root Signature associated with the current graphics pipeline.
        /// </summary>
        /// <remarks>
        ///   A <strong>Root Signature</strong> is used to specify how Graphics Resources, such as Textures and Buffers,
        ///   are bound to the graphics pipeline (i.e. how <see cref="DescriptorSet"/> will be bound together).
        /// </remarks>
        public RootSignature RootSignature { get; private set; }

        /// <summary>
        ///   Gets the collection of parameters used by this Effect instance.
        /// </summary>
        public ParameterCollection Parameters { get; } = parameters ?? new ParameterCollection();


        /// <inheritdoc/>
        protected override void Destroy()
        {
            RootSignature?.Dispose();
            RootSignature = null;

            bufferUploader.Clear();

            base.Destroy();
        }


        /// <summary>
        ///   Updates the Effect associated with this instance.
        /// </summary>
        /// <param name="graphicsDevice">The Graphics Device to which the Effect is applied.</param>
        /// <returns>
        ///   <see langword="true"/> if the Effect was updated; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        ///   This method checks if the <see cref="Effect"/> needs to be updated based on changes in the
        ///   permutation parameters or the source of the Effect.
        ///   If an update is necessary, it reinitializes the Effect, updates the reflection information,
        ///   and prepares the necessary Graphics Resources and data.
        /// </remarks>
        public bool UpdateEffect(GraphicsDevice graphicsDevice)
        {
            if (Effect is null ||
                permutationCounter != Parameters.PermutationCounter ||
                Effect?.SourceChanged is true)
            {
                permutationCounter = Parameters.PermutationCounter;

                var oldEffect = Effect;
                ChooseEffect();

                // Early exit: same Effect, and already initialized
                if (oldEffect == Effect && DescriptorReflection is not null)  // TODO: == is comparing references, not contents. Is this acceptable?
                    return false;

                // Update reflection and rearrange Buffers / resources
                var effectBytecode = Effect?.Bytecode;
                var layoutNames = effectBytecode.Reflection.ResourceBindings.Select(x => x.ResourceGroup ?? "Globals").Distinct().ToList();
                DescriptorReflection = EffectDescriptorSetReflection.New(graphicsDevice, effectBytecode, layoutNames, defaultSetSlot: "Globals");

                RootSignature?.Dispose();
                RootSignature = RootSignature.New(graphicsDevice, DescriptorReflection);

                bufferUploader.Clear();
                bufferUploader.Compile(graphicsDevice, DescriptorReflection, effectBytecode);

                // Create the parameter updater
                var layouts = new DescriptorSetLayoutBuilder[DescriptorReflection.Layouts.Count];
                for (int i = 0; i < DescriptorReflection.Layouts.Count; ++i)
                    layouts[i] = DescriptorReflection.Layouts[i].Layout;

                var parameterUpdaterLayout = new EffectParameterUpdaterLayout(graphicsDevice, effectBytecode, layouts);
                parameterUpdater = new EffectParameterUpdater(parameterUpdaterLayout, Parameters);

                descriptorSets = new DescriptorSet[parameterUpdater.ResourceGroups.Length];

                return true;
            }

            return false;
        }


        /// <summary>
        ///   Selects and compiles the appropriate Effect based on the current parameters.
        /// </summary>
        /// <remarks>
        ///   When overridden in a derived class, this method should implement the logic to
        ///   select and compile the Effect based on the current set of parameters, for example,
        ///   by using the permutation parameters set in <see cref="Parameters"/> to compile a
        ///   specific version of the Effect.
        /// </remarks>
        protected virtual void ChooseEffect()
        {
        }

        /// <summary>
        ///   Applies the current parameters set for this Effect instance to the
        ///   provided graphics context, updating bound Graphics Resources, uploading
        ///   Constant Buffers, etc.
        /// </summary>
        /// <param name="graphicsContext">
        ///   The graphics context to which the Effect parameters will be applied.
        ///   This context must be valid and initialized before calling this method.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="graphicsContext"/> is <see langword="null"/>.</exception>
        public void Apply(GraphicsContext graphicsContext)
        {
            ArgumentNullException.ThrowIfNull(graphicsContext);

            var commandList = graphicsContext.CommandList;

            parameterUpdater.Update(commandList.GraphicsDevice, graphicsContext.ResourceGroupAllocator, Parameters);

            // Flush resource groups and Constant Buffer
            graphicsContext.ResourceGroupAllocator.Flush();

            var resourceGroups = parameterUpdater.ResourceGroups;

            // Update Constant Buffer
            bufferUploader.Apply(commandList, resourceGroups, 0);

            // Bind Descriptor Sets
            for (int i = 0; i < descriptorSets.Length; ++i)
                descriptorSets[i] = resourceGroups[i].DescriptorSet;

            commandList.SetDescriptorSets(index: 0, descriptorSets);
        }
    }
}
