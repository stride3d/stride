// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Updater;
using Unsafe = System.Runtime.CompilerServices.Unsafe;

namespace Stride.Engine.Design
{
    /// <summary>
    /// Lets animation curves drive morph weights by target name: <c>[ModelComponent.Key].MorphWeights[Smile]</c>.
    /// The name is resolved to a slot of the model's layout, so playback writes the slot array directly.
    /// </summary>
    internal class ModelMorphWeightResolver : UpdateMemberResolver
    {
        [ModuleInitializer]
        internal static void InitializeModule()
        {
            UpdateEngine.RegisterMemberResolver(new ModelMorphWeightResolver());
        }

        public override Type SupportedType => typeof(ModelComponent);

        public override UpdatableMember ResolveProperty(string memberName)
        {
            return memberName == "MorphWeights" ? new MorphWeightsAccessor() : null;
        }

        public override UpdatableMember ResolveIndexer(string indexerName)
        {
            return new MorphWeightAccessor(indexerName);
        }

        /// <summary>
        /// Enters <c>MorphWeights</c> as the component itself, so the following indexer resolves on <see cref="ModelComponent"/>.
        /// </summary>
        private class MorphWeightsAccessor : UpdatableCustomAccessor
        {
            /// <inheritdoc/>
            public override Type MemberType => typeof(ModelComponent);

            /// <inheritdoc/>
            public override void GetBlittable(IntPtr obj, IntPtr data)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override void SetBlittable(IntPtr obj, IntPtr data)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override void SetStruct(IntPtr obj, object data)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override IntPtr GetStructAndUnbox(IntPtr obj, object data)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override object GetObject(IntPtr obj)
            {
                return UpdateEngineHelper.PointerToObject<ModelComponent>(obj);
            }

            /// <inheritdoc/>
            public override void SetObject(IntPtr obj, object data)
            {
                throw new NotSupportedException();
            }
        }

        private class MorphWeightAccessor : UpdatableCustomAccessor
        {
            private readonly string targetName;

            public MorphWeightAccessor(string targetName)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(targetName);
                this.targetName = targetName;
            }

            /// <inheritdoc/>
            public override Type MemberType => typeof(float);

            /// <inheritdoc/>
            public override unsafe void GetBlittable(IntPtr obj, IntPtr data)
            {
                var modelComponent = UpdateEngineHelper.PointerToObject<ModelComponent>(obj);
                var slot = FindSlot(modelComponent);
                Unsafe.WriteUnaligned((void*)data, slot >= 0 ? modelComponent.GetBoundMorphWeight(slot) : 0.0f);
            }

            /// <inheritdoc/>
            public override unsafe void SetBlittable(IntPtr obj, IntPtr data)
            {
                var modelComponent = UpdateEngineHelper.PointerToObject<ModelComponent>(obj);
                var slot = FindSlot(modelComponent);
                // Models without this target are skipped, so one clip can play on different models
                if (slot >= 0)
                    modelComponent.SetBoundMorphWeight(slot, Unsafe.ReadUnaligned<float>((void*)data));
            }

            /// <inheritdoc/>
            public override void SetStruct(IntPtr obj, object data)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override IntPtr GetStructAndUnbox(IntPtr obj, object data)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override object GetObject(IntPtr obj)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override void SetObject(IntPtr obj, object data)
            {
                throw new NotSupportedException();
            }

            // Must not allocate: the update engine holds the component by an unpinned address, and a garbage collection
            // would move it. A component the renderer has not bound yet (its first frame) is skipped.
            private int FindSlot(ModelComponent modelComponent)
            {
                var layout = modelComponent.BoundMorphLayout;
                return layout != null && layout.SlotByName.TryGetValue(targetName, out var slot) ? slot : -1;
            }
        }
    }
}
