// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core;
using Stride.Shaders.Compiler;

namespace Stride.Rendering
{
    /// <summary>
    ///   Represents an instance of an Effect that can be dynamically compiled in different permutations
    ///   according to the permutation parameters set in its <see cref="EffectInstance.Parameters"/>.
    /// </summary>
    public class DynamicEffectInstance : EffectInstance
    {
        private EffectSystem effectSystem;

        /// <summary>
        ///   Gets or sets the name of the Effect to be used by this instance.
        /// </summary>
        public string EffectName { get; set; }

        /// <summary>
        ///   Gets the parameters used when compiling the Effect used by this instance.
        /// </summary>
        public ref EffectCompilerParameters EffectCompilerParameters => ref effectCompilerParameters;
        private EffectCompilerParameters effectCompilerParameters = EffectCompilerParameters.Default;


        /// <summary>
        ///   Initializes a new instance of the <see cref="DynamicEffectInstance"/> class with the specified effect name and parameters.
        /// </summary>
        /// <param name="effectName">The name of the Effect.</param>
        /// <param name="parameters"></param>
        public DynamicEffectInstance(string effectName, ParameterCollection? parameters = null)
            : base(effect: null, parameters)
        {
            EffectName = effectName;
        }

        /// <summary>
        ///   Initializes the Effect instance.
        /// </summary>
        /// <param name="services">The service registry used to obtain necessary services for initialization.</param>
        /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>"
        /// <exception cref="ServiceNotFoundException">The required service <see cref="EffectSystem"/> was not found.</exception>
        /// <remarks>
        ///   This method retrieves the <see cref="EffectSystem"/> service from the provided service registry.
        /// </remarks>
        public void Initialize(IServiceRegistry services)
        {
            ArgumentNullException.ThrowIfNull(services);

            effectSystem = services.GetSafeServiceAs<EffectSystem>();
        }

        /// <summary>
        ///   Selects and compiles the appropriate Effect based on the current parameters.
        /// </summary>
        /// <remarks>
        ///   This method updates the Effect by recompiling and reloading it with the current set of
        ///   permutation parameters.
        /// </remarks>
        protected override void ChooseEffect()
        {
            // TODO: Free previous descriptor sets and layouts?

            // Looks like the Effect changed, so it needs a recompilation
            var compilerParameters = new CompilerParameters
            {
                EffectParameters = EffectCompilerParameters
            };

            foreach (var effectParameterKey in Parameters.ParameterKeyInfos)
            {
                if (effectParameterKey.Key.Type == ParameterKeyType.Permutation)
                {
                    // TODO: GRAPHICS REFACTOR: Avoid direct access, esp. since permutation values might be separated from Objects at some point
                    compilerParameters.SetObject(effectParameterKey.Key, Parameters.ObjectValues[effectParameterKey.BindingSlot]);
                }
            }

            Effect = effectSystem.LoadEffect(EffectName, compilerParameters).WaitForResult();
        }
    }
}
