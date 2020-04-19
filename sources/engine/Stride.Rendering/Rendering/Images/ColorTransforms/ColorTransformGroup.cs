// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

using Stride.Core;
using Stride.Core.Annotations;

namespace Stride.Rendering.Images
{
    /// <summary>
    /// An effect combining a list of <see cref="ColorTransform"/> sub-effects.
    /// </summary>
    /// <remarks>
    /// This effect and all <see cref="Transforms"/> are collected and compiled into a single shader.
    /// </remarks>
    [DataContract("ColorTransformGroup")]
    [Display("Color Transforms")]
    public class ColorTransformGroup : ImageEffect
    {
        // TODO GRAPHICS REFACTOR
        // private readonly ParameterCollection transformsParameters;
        private ImageEffectShader transformGroupEffect;
        private readonly Dictionary<ParameterCompositeKey, ParameterKey> compositeKeys;
        private readonly ColorTransformCollection preTransforms;
        private readonly ColorTransformCollection transforms;
        private readonly ColorTransformCollection postTransforms;
        private readonly List<ColorTransform> collectTransforms;
        private List<ColorTransform> enabledTransforms;
        private ColorTransformContext transformContext;
        private readonly string colorTransformGroupEffectName;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorTransformGroup"/> class.
        /// </summary>
        public ColorTransformGroup() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorTransformGroup"/> class.
        /// </summary>
        /// <param name="colorTransformGroupEffect">The color transform group effect.</param>
        public ColorTransformGroup(string colorTransformGroupEffect)
            : base(colorTransformGroupEffect)
        {
            compositeKeys = new Dictionary<ParameterCompositeKey, ParameterKey>();
            preTransforms = new ColorTransformCollection();
            transforms = new ColorTransformCollection();
            postTransforms = new ColorTransformCollection();
            enabledTransforms = new List<ColorTransform>();
            collectTransforms = new List<ColorTransform>();
            colorTransformGroupEffectName = colorTransformGroupEffect ?? "ColorTransformGroupEffect";
        }

        /// <inheritdoc/>
        protected override void InitializeCore()
        {
            base.InitializeCore();

            transformGroupEffect = new ImageEffectShader(colorTransformGroupEffectName);
            // TODO GRAPHICS REFACTOR
            //transformGroupEffect.SharedParameterCollections.Add(Parameters);
            transformGroupEffect.Initialize(Context);
            Parameters = transformGroupEffect.Parameters;

            // we are adding parameter collections after as transform parameters should override previous parameters
            // TODO GRAPHICS REFACTOR
            //transformGroupEffect.ParameterCollections.Add(transformsParameters);

            this.transformContext = new ColorTransformContext(this, Context);
        }

        /// <summary>
        /// Gets the pre transforms applied before the standard <see cref="Transforms"/>.
        /// </summary>
        /// <value>The pre transforms.</value>
        [DataMemberIgnore]
        public ColorTransformCollection PreTransforms
        {
            get
            {
                return preTransforms;
            }
        }

        /// <summary>
        /// Gets the color transforms.
        /// </summary>
        /// <value>The transforms.</value>
        [DataMember(10)]
        [Display("Transforms", Expand = ExpandRule.Always)]
        [MemberCollection(CanReorderItems = true, NotNullItems = true)]
        public ColorTransformCollection Transforms
        {
            get
            {
                return transforms;
            }
        }

        /// <summary>
        /// Gets the post transforms applied after the standard <see cref="Transforms"/>.
        /// </summary>
        /// <value>The post transforms.</value>
        [DataMemberIgnore]
        public ColorTransformCollection PostTransforms
        {
            get
            {
                return postTransforms;
            }
        }

        protected override void DrawCore(RenderDrawContext context1)
        {
            var output = GetOutput(0);
            if (output == null)
            {
                return;
            }

            // Collect all transform parameters
            CollectTransformsParameters(context1);

            for (int i = 0; i < transformContext.Inputs.Count; i++)
            {
                transformGroupEffect.SetInput(i, transformContext.Inputs[i]);
            }
            transformGroupEffect.SetOutput(output);
            transformGroupEffect.Draw(context1, name: Name);
        }

        protected virtual void CollectPreTransforms()
        {
            foreach (var transform in preTransforms)
            {
                AddTemporaryTransform(transform);
            }
        }

        protected virtual void CollectPostTransforms()
        {
            foreach (var transform in postTransforms)
            {
                AddTemporaryTransform(transform);
            }
        }

        protected void AddTemporaryTransform(ColorTransform transform)
        {
            if (transform == null) throw new ArgumentNullException("transform");
            if (transform.Shader == null) throw new ArgumentOutOfRangeException("transform", "Transform parameter must have a Shader not null");
            if (transform.Enabled)
                collectTransforms.Add(transform);
        }

        private void CollectTransforms()
        {
            collectTransforms.Clear();
            CollectPreTransforms();
            foreach (var transform in transforms)
            {
                AddTemporaryTransform(transform);
            }
            CollectPostTransforms();

            // Copy all parameters from ColorTransform to effect parameters
            if (collectTransforms.Count != enabledTransforms.Count)
            {
                enabledTransforms = new List<ColorTransform>(collectTransforms);
            }
            else
            {
                for (int i = 0; i < enabledTransforms.Count; i++)
                {
                    if (collectTransforms[i] != enabledTransforms[i])
                    {
                        enabledTransforms = new List<ColorTransform>(collectTransforms);
                        break;
                    }
                }
            }
        }

        private void CollectTransformsParameters(RenderDrawContext context)
        {
            transformContext.Inputs.Clear();
            for (int i = 0; i < InputCount; i++)
            {
                transformContext.Inputs.Add(GetInput(i));
            }

            // Grab all color transforms
            CollectTransforms();

            // Update effect
            // TODO: if the list was the same than previous one, we could optimize this and not setup the value
            Parameters.Set(ColorTransformGroupKeys.Transforms, enabledTransforms);
            if (transformGroupEffect.EffectInstance.UpdateEffect(context.GraphicsDevice))
            {
                // Update layouts
                for (int i = 0; i < enabledTransforms.Count; i++)
                {
                    // Set group
                    enabledTransforms[i].Group = this;

                    // Prepare layouts of ColorTransforms to be able to copy them easily
                    enabledTransforms[i].PrepareParameters(transformContext, Parameters, $".Transforms[{i}]");
                }
            }

            for (int i = 0; i < enabledTransforms.Count; i++)
            {
                var transform = enabledTransforms[i];

                // Always update parameters
                transform.UpdateParameters(transformContext);
            }
        }

        public void NotifyPermutationChange()
        {
            Parameters.PermutationCounter++;
        }

        private ParameterKey GetComposedKey(ParameterKey key, int transformIndex)
        {
            var compositeKey = new ParameterCompositeKey(key, transformIndex);

            ParameterKey rawCompositeKey;
            if (!compositeKeys.TryGetValue(compositeKey, out rawCompositeKey))
            {
                rawCompositeKey = ParameterKeys.FindByName(string.Format("{0}.Transforms[{1}]", key.Name, transformIndex));
                compositeKeys.Add(compositeKey, rawCompositeKey);
            }
            return rawCompositeKey;
        }

        /// <summary>
        /// An internal key to cache {Key,TransformIndex} => CompositeKey
        /// </summary>
        private struct ParameterCompositeKey : IEquatable<ParameterCompositeKey>
        {
            private readonly ParameterKey key;

            private readonly int index;

            /// <summary>
            /// Initializes a new instance of the <see cref="ParameterCompositeKey"/> struct.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="transformIndex">Index of the transform.</param>
            public ParameterCompositeKey(ParameterKey key, int transformIndex)
            {
                if (key == null) throw new ArgumentNullException("key");
                this.key = key;
                index = transformIndex;
            }

            public bool Equals(ParameterCompositeKey other)
            {
                return key.Equals(other.key) && index == other.index;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is ParameterCompositeKey && Equals((ParameterCompositeKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (key.GetHashCode() * 397) ^ index;
                }
            }
        }
    }
}
