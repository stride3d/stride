// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Graphics;
using Stride.Shaders.Compiler;

namespace Stride.Rendering
{
    public class DynamicEffectInstance : EffectInstance
    {
        // Parameter keys used for effect permutation
        //private KeyValuePair<ParameterKey, object>[] effectParameterKeys;

        private string effectName;
        private EffectSystem effectSystem;

        public DynamicEffectInstance(string effectName, ParameterCollection parameters = null) : base(null, parameters)
        {
            this.effectName = effectName;
        }

        public string EffectName
        {
            get { return effectName; }
            set { effectName = value; }
        }

        /// <summary>
        /// Defines the effect parameters used when compiling this effect.
        /// </summary>
        public EffectCompilerParameters EffectCompilerParameters = EffectCompilerParameters.Default;

        public void Initialize(IServiceRegistry services)
        {
            this.effectSystem = services.GetSafeServiceAs<EffectSystem>();
        }

        protected override void ChooseEffect(GraphicsDevice graphicsDevice)
        {
            // TODO: Free previous descriptor sets and layouts?

            // Looks like the effect changed, it needs a recompilation
            var compilerParameters = new CompilerParameters
            {
                EffectParameters = EffectCompilerParameters,
            };

            foreach (var effectParameterKey in Parameters.ParameterKeyInfos)
            {
                if (effectParameterKey.Key.Type == ParameterKeyType.Permutation)
                {
                    // TODO GRAPHICS REFACTOR avoid direct access, esp. since permutation values might be separated from Objects at some point
                    compilerParameters.SetObject(effectParameterKey.Key, Parameters.ObjectValues[effectParameterKey.BindingSlot]);
                }
            }

            effect = effectSystem.LoadEffect(effectName, compilerParameters).WaitForResult();
        }
    }
}
