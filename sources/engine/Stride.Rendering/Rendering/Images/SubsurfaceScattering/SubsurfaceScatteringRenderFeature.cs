//#define DEDUPLICATE_KERNELS

using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Rendering.SubsurfaceScattering;

namespace Xenko.Rendering.Materials
{
    /// <summary>
    /// Custom render feature, that prepares constants needed by SubsurfaceScatteringBlurEffect.
    /// </summary>
    public class SubsurfaceScatteringRenderFeature : SubRenderFeature
    {
        private struct ScatteringParameters : IEquatable<ScatteringParameters>
        {
            public readonly Vector4[] ScatteringKernel;
            public readonly float ScatteringWidth;

            private readonly int scatteringKernelHashCode;

            // TODO: STABILITY: This might cause issues if there's a hash collision. The material won't be saved.
            //                  An idea is to use two hashes generated with different methods and to compare those instead.

            private static int GetVector4Hash(Vector4 vector) // I'm not using "Vector4.GetHashCode()" because it's terrible.
            {
                unchecked
                {
                    int hashCode = vector.X.GetHashCode();
                    hashCode = (hashCode * 397) ^ vector.Y.GetHashCode();
                    hashCode = (hashCode * 397) ^ vector.Z.GetHashCode();
                    hashCode = (hashCode * 397) ^ vector.W.GetHashCode();

                    return hashCode;
                }
            }

            public ScatteringParameters(MaterialPass material)
            {
                // Extract all required parameters from the material:
                ScatteringWidth = material.Parameters.Get(MaterialSurfaceSubsurfaceScatteringShadingKeys.ScatteringWidth);   // TODO: Instead of saving this shit, how about just generating a hash of the kernel inside the material??
                ScatteringKernel = material.Parameters.Get(MaterialSurfaceSubsurfaceScatteringShadingKeys.ScatteringKernel);
                scatteringKernelHashCode = 0;

                // TODO: Use a better hash function (and maybe a higher bit depth for the hash).
                if (ScatteringKernel.Length > 0)
                {
                    scatteringKernelHashCode = GetVector4Hash(ScatteringKernel[0]);

                    for (int i = 1; i < ScatteringKernel.Length; ++i)
                    {
                        scatteringKernelHashCode = (scatteringKernelHashCode * 397) ^ GetVector4Hash(ScatteringKernel[i]);
                    }
                }
            }

            public bool Equals(ScatteringParameters other) // TODO: PERFORMANCE: This function is slow because it compares the whole kernel. 
            {
                if (ScatteringKernel.Length != other.ScatteringKernel.Length) // TODO: Is this check necessary or does "SequenceEqual()" already check for that?
                {
                    return false;
                }

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                return ScatteringWidth == other.ScatteringWidth &&
                       ScatteringKernel.SequenceEqual(other.ScatteringKernel); // &&
            }

            public override int GetHashCode() // We ignore "ScatteringKernel" because it's generated based on "ScatteringKernel" and "ScatteringFalloff".
            {
                unchecked
                {
                    var hashCode = ScatteringWidth.GetHashCode();
                    hashCode = (hashCode * 397) ^ scatteringKernelHashCode;
                    return hashCode;
                }
            }
        }

        [DataMember(10)]
        [Display("SubsurfaceScatteringBlurEffect")]
        public SubsurfaceScatteringBlur SubsurfaceScatteringBlurEffect { get; set; }

        /// <summary>
        /// Turns material parameter deduplication on or off.
        /// </summary>
        /// <userdoc>
        /// When enabled, SSS material parameters are deduplicated every frame and therefore more unique materials can be rendered at once but it comes at a CPU performance hit.
        /// The maximum number of unique SSS materials that can be rendered per frame is defined in SubsurfaceScatteringBlur (because it depends on the bit depth of the material index framebuffer).
        /// </userdoc>
        [DataMember(20)]
        [Display("Deduplicate material parameters")]
        public bool DeduplicateMaterialParameters { get; set; } = false;    // TODO: Don't even instantiate the dictionary if this is set to false?

        //HashSet<Material> MaterialsHighlightedForModel = new HashSet<Material>();    // TODO: PERFORMANCE: Is this faster than the regular dictionary below?
        // TODO: PERFORMANCE: Or use ConcurrentDictionary() and multithread the code?
        private Dictionary<ScatteringParameters, uint> scatteringParametersToArrayIndexDictionary = new Dictionary<ScatteringParameters, uint>();
        private ConstantBufferOffsetReference materialIndexConstantBufferOffsetReference;

        /// <inheritdoc/>
        protected override void InitializeCore()
        {
            base.InitializeCore();
            materialIndexConstantBufferOffsetReference = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawCBufferOffsetSlot(GBufferOutputSubsurfaceScatteringMaterialIndexKeys.MaterialIndex.Name);
        }

        private bool HasScatteringKernel(MaterialPass material)
        {
            return (material.Parameters.ContainsKey(MaterialSurfaceSubsurfaceScatteringShadingKeys.ScatteringKernel));
        }

        private unsafe void WriteMaterialIndexIntoRenderNodeConstantBuffer(RenderSystemResourceGroupLayout perDrawLayout, RenderNode renderNode, uint materialIndex)
        {
            var mappedConstantBuffer = renderNode.Resources.ConstantBuffer.Data;

            // Assign the generated material index to the renderNode:
            var materialIndexOffset = perDrawLayout.GetConstantBufferOffset(materialIndexConstantBufferOffsetReference);
            if (materialIndexOffset != -1)
            {
                uint* pointerToMaterialIndex = (uint*)((byte*)mappedConstantBuffer + materialIndexOffset);
                *pointerToMaterialIndex = materialIndex;
            }
        }

        private void AddMaterialToArrayAndDictionary(ScatteringParameters scatteringParameters, uint materialArrayIndex)
        {
            // "SetScatteringWidth()" throws an exception if the range is exceeded. // TODO: How to handle this? Don't throw at all?
            SubsurfaceScatteringBlurEffect.SetScatteringWidth(materialArrayIndex, scatteringParameters.ScatteringWidth); // Add the scattering width to the scattering width array.
            if (scatteringParameters.ScatteringKernel != null)
            { 
                // TODO: STABILITY: What to do if the scattering width is present but no kernel? The post-process wouldn't be able to handle that correctly.
                //                  Maybe just save a dummy kernel?
                SubsurfaceScatteringBlurEffect.SetScatteringKernel(materialArrayIndex, scatteringParameters.ScatteringKernel);
            }

            scatteringParametersToArrayIndexDictionary[scatteringParameters] = materialArrayIndex; // Add the material to the dictionary and save its associated index in the scattering width array.
        }

        private uint AddMaterialToDictionaryAndGetArrayIndex(RenderMesh renderMesh, ref uint materialArrayIndexCounter)
        {
            // Fill the structure because the dictionary compares the contents for equality:
            ScatteringParameters scatteringParameters = new ScatteringParameters(renderMesh.MaterialPass);

            uint materialArrayIndex;

            if (DeduplicateMaterialParameters)
            {
                // Since we deduplicate materials here, we must check whether or not we should add a material or not before doing so.

                // If "TryGetValue()" succeeds, "materialArrayIndex" will receive the correct value. If it fails, it will be computed below.
                if (!scatteringParametersToArrayIndexDictionary.TryGetValue(scatteringParameters, out materialArrayIndex)) // If the material isn't present in the collection:
                {
                    AddMaterialToArrayAndDictionary(scatteringParameters, materialArrayIndexCounter);
                    materialArrayIndex = materialArrayIndexCounter++; // Use the index before the increment.
                }
            }
            else
            {
                // Since we don't deduplicate materials here, we always add each material and increment the index.
                AddMaterialToArrayAndDictionary(scatteringParameters, materialArrayIndexCounter);
                materialArrayIndex = materialArrayIndexCounter++; // Use the index before the increment.
            }

            return materialArrayIndex;
        }

        /// <inheritdoc/>
        public override void Prepare(RenderDrawContext context)
        {
            if (DeduplicateMaterialParameters)
            {
                scatteringParametersToArrayIndexDictionary.Clear();
            }

            // TODO: Generate a material array per view? This could in some cases limit the number of materials present in the array.
            //       In case every object is visible from every view, it wouldn't save us anything though.

            uint materialArrayIndexCounter = 1; // We start at index 1 instead of 0 because we use index 0 to flag (and discard) non-scattering materials.
            // TODO: Not sure if the following line is even necessary, because we don't use any material parameters of material 0 anyway. I mean not even the kernel of material 0 is set. So either set both or none.
            SubsurfaceScatteringBlurEffect.SetScatteringWidth(0, 0.0f);    // This element is unused because a material index of 0 means the material is not a subsurface scattering material and therefore will be skipped by the post-process.

            // Generate the material dictionary that contains only scattering materials:
            //Dispatcher.ForEach(((RootEffectRenderFeature)RootRenderFeature).RenderNodes, (ref RenderNode renderNode) =>   // TODO: PERFORMANCE: Use this instead?
            foreach (RenderNode renderNode in ((RootEffectRenderFeature)RootRenderFeature).RenderNodes)
            {
                var perDrawLayout = renderNode.RenderEffect.Reflection?.PerDrawLayout;
                if (perDrawLayout == null)
                { 
                    continue;
                }

                var renderMesh = (RenderMesh)renderNode.RenderObject;
                uint materialArrayIndex = 0; // If the mesh doesn't have a scattering kernel we write index 0 into the constant buffer.

                if (HasScatteringKernel(renderMesh.MaterialPass))
                {
                    materialArrayIndex = AddMaterialToDictionaryAndGetArrayIndex(renderMesh, ref materialArrayIndexCounter);
                }

                WriteMaterialIndexIntoRenderNodeConstantBuffer(perDrawLayout, renderNode, materialArrayIndex);
            }

            bool scatteringMaterialsAreVisible = (materialArrayIndexCounter > 1);
            SubsurfaceScatteringBlurEffect.Enabled = scatteringMaterialsAreVisible;   // Disable the post-process if no scattering objects are visible (to save performance).
        }
    }
}
