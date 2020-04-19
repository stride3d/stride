// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using Stride.Core;
using Stride.Rendering;

namespace Stride.Shaders
{
    /// <summary>
    /// A context used when mixin <see cref="ShaderSource"/>.
    /// </summary>
    public class ShaderMixinContext
    {
        private readonly ParameterCollection compilerParameters;
        private readonly Stack<ParameterCollection> parameterCollections = new Stack<ParameterCollection>();
        private readonly Dictionary<string, IShaderMixinBuilder> registeredBuilders;
        private readonly Stack<int> compositionIndices = new Stack<int>();
        private readonly StringBuilder compositionStringBuilder = new StringBuilder();

        private string compositionString = null;

        private readonly ShaderMixinSource currentMixinSourceTree;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderMixinContext" /> class.
        /// </summary>
        /// <param name="mixinTree">The mixin tree.</param>
        /// <param name="compilerParameters">The default property container.</param>
        /// <param name="registeredBuilders">The registered builders.</param>
        /// <exception cref="System.ArgumentNullException">compilerParameters
        /// or
        /// registeredBuilders</exception>
        public ShaderMixinContext(ShaderMixinSource mixinTree, ParameterCollection compilerParameters, Dictionary<string, IShaderMixinBuilder> registeredBuilders)
        {
            if (mixinTree == null) throw new ArgumentNullException("mixinTree");
            if (compilerParameters == null)
                throw new ArgumentNullException("compilerParameters");

            if (registeredBuilders == null)
                throw new ArgumentNullException("registeredBuilders");

            // TODO: use a copy of the compilerParameters?
            this.currentMixinSourceTree = mixinTree;
            this.compilerParameters = compilerParameters;
            this.registeredBuilders = registeredBuilders;
            this.parameterCollections = new Stack<ParameterCollection>();
        }

        /// <summary>
        /// Gets or sets the child effect.
        /// </summary>
        /// <value>The child effect.</value>
        public string ChildEffectName { get; set; }

        /// <summary>
        /// Pushes the current parameters collection being used.
        /// </summary>
        /// <typeparam name="T">Type of the parameter collection</typeparam>
        /// <param name="parameterCollection">The property container.</param>
        public void PushParameters(ParameterCollection parameterCollection)
        {
            parameterCollections.Push(parameterCollection);
        }

        /// <summary>
        /// Pops the parameters collection.
        /// </summary>
        public void PopParameters()
        {
            parameterCollections.Pop();
        }

        public ShaderMixinSource CurrentMixin
        {
            get
            {
                return currentMixinSourceTree;
            }
        }

        public void Discard()
        {
            throw new ShaderMixinDiscardException();
        }

        /// <summary>
        /// Gets a parameter value for the specified key.
        /// </summary>
        /// <typeparam name="T">Type of the parameter value</typeparam>
        /// <param name="paramKey">The parameter key.</param>
        /// <returns>The value or default value associated to this parameter key.</returns>
        /// <exception cref="System.ArgumentNullException">key</exception>
        public T GetParam<T>(PermutationParameterKey<T> paramKey)
        {
            if (paramKey == null)
                throw new ArgumentNullException("paramKey");

            var globalKey = paramKey;
            var composeKey = GetComposeKey(paramKey);
            var selectedKey = globalKey;
            ParameterCollection sourceParameters = null;

            // Try first if a composite key with a value is available for the key
            if (composeKey != globalKey)
            {
                sourceParameters = FindKeyValue(composeKey, out selectedKey);
            }

            // Else try using global key
            if (sourceParameters == null)
            {
                sourceParameters = FindKeyValue(globalKey, out selectedKey);
            }

            // If nothing found, use composeKey and global compiler parameters
            if (sourceParameters == null)
            {
                selectedKey = composeKey;
                sourceParameters = compilerParameters;
            }

            // Gets the value from a source parameters
            var value = Get(sourceParameters, selectedKey);

            return value;
        }

        private ParameterCollection FindKeyValue<T>(PermutationParameterKey<T> key, out PermutationParameterKey<T> selectedKey)
        {
            // Try to get a value from registered containers
            selectedKey = null;
            foreach (var parameterCollection in parameterCollections)
            {
                if (parameterCollection.ContainsKey(key))
                {
                    selectedKey = key;
                    return parameterCollection;
                }
            }
            if (compilerParameters.ContainsKey(key))
            {
                selectedKey = key;
                return compilerParameters;
            }
            
            return null;
        }

        private PermutationParameterKey<T> GetComposeKey<T>(PermutationParameterKey<T> key)
        {
            if (compositionString == null)
            {
                return key;
            }
            key = key.ComposeWith(compositionString);
            return key;
        }

        public void SetParam<T>(PermutationParameterKey<T> key, T value)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            var propertyContainer = parameterCollections.Count > 0 ? parameterCollections.Peek() : compilerParameters;
            Set(propertyContainer, key, value);
        }

        /// <summary>
        /// Removes the specified mixin from this instance.
        /// </summary>
        /// <param name="mixinTree">The mixin tree.</param>
        /// <param name="name">The name.</param>
        public void RemoveMixin(ShaderMixinSource mixinTree, string name)
        {
            var mixinParent = mixinTree;
            for (int i = mixinParent.Mixins.Count - 1; i >= 0; i--)
            {
                var mixin = mixinParent.Mixins[i];
                if (mixin.ClassName == name)
                {
                    mixinParent.Mixins.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Mixins a <see cref="ShaderMixinSource" /> into the specified mixin tree.
        /// </summary>
        /// <param name="mixinTree">The mixin tree.</param>
        /// <param name="name">The name.</param>
        public void Mixin(ShaderMixinSource mixinTree, string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name", "Invalid null mixin name");
            }

            IShaderMixinBuilder builder;
            if (!registeredBuilders.TryGetValue(name, out builder))
            {
                // Else simply add the name of the shader
                mixinTree.Mixins.Add(new ShaderClassSource(name));
            }
            else if (builder != null)
            {
                builder.Generate(mixinTree, this);
            }
        }

        /// <summary>
        /// Mixins a <see cref="ShaderClassSource" /> identified by its name/generic parameters into the specified mixin tree.
        /// </summary>
        /// <param name="mixinTree">The mixin tree.</param>
        /// <param name="name">The name.</param>
        /// <param name="genericParameters">The generic parameters.</param>
        /// <exception cref="System.InvalidOperationException">If the class source doesn't support generic parameters</exception>
        public void Mixin(ShaderMixinSource mixinTree, string name, params object[] genericParameters)
        {
            IShaderMixinBuilder builder;
            if (!registeredBuilders.TryGetValue(name, out builder))
            {
                // Else simply add the name of the shader
                mixinTree.Mixins.Add(new ShaderClassSource(name, genericParameters));
            } 
            else if (builder != null)
            {
                if (genericParameters != null && genericParameters.Length != 0)
                {
                    throw new InvalidOperationException(string.Format("Generic Parameters are not supported with [{0}]", builder.GetType().GetTypeInfo().Name));
                }
                builder.Generate(mixinTree, this);
            }
        }

        public void PushComposition(ShaderMixinSource mixin, string compositionName, ShaderMixinSource composition)
        {
            mixin.AddComposition(compositionName, composition);

            compositionIndices.Push(compositionStringBuilder.Length);
            if (compositionString != null)
            {
                compositionStringBuilder.Insert(0, '.');
            }

            compositionStringBuilder.Insert(0, compositionName);

            compositionString = compositionStringBuilder.ToString();
        }

        public void PushCompositionArray(ShaderMixinSource mixin, string compositionName, ShaderMixinSource composition)
        {
            int arrayIndex = mixin.AddCompositionToArray(compositionName, composition);

            compositionIndices.Push(compositionStringBuilder.Length);
            if (compositionString != null)
            {
                compositionStringBuilder.Insert(0, '.');
            }

            compositionStringBuilder.Insert(0, ']');
            compositionStringBuilder.Insert(0, arrayIndex);
            compositionStringBuilder.Insert(0, '[');
            compositionStringBuilder.Insert(0, compositionName);

            compositionString = compositionStringBuilder.ToString();
        }

        public void PopComposition()
        {
            var compositionIndex = compositionIndices.Pop();
            compositionStringBuilder.Remove(0, compositionStringBuilder.Length - compositionIndex);
            compositionString = compositionIndex == 0 ? null : compositionStringBuilder.ToString();
        }

        /// <summary>
        /// Mixins a <see cref="ShaderMixinSource"/> into the specified mixin tree.
        /// </summary>
        /// <param name="mixinTree">The mixin tree.</param>
        /// <param name="shaderSource">The shader source.</param>
        public void Mixin(ShaderMixinSource mixinTree, ShaderSource shaderSource)
        {
            if (shaderSource == null)
            {
                return;
            }

            if (shaderSource is ShaderMixinSource shaderMixinSource)
            {
                mixinTree.CloneFrom(shaderMixinSource);
            }
            else if (shaderSource is ShaderClassCode shaderClassCode)
            {
                mixinTree.Mixins.Add(shaderClassCode);
            }
            else if (shaderSource is ShaderMixinGeneratorSource mixinGeneratorSource)
            {
                Mixin(mixinTree, mixinGeneratorSource.Name);
            }
            else
            {
                throw new InvalidOperationException("ShaderSource [{0}] is not supported (Only ShaderMixinSource and ShaderClassSource)".ToFormat(shaderSource.GetType()));
            }

            // If we are mixin a shader source that has an attached discard, don't proceed further
            if (shaderSource.Discard)
            {
                Discard();
            }
        }

        // Helpers, until we get rid of ParameterCollection
        private void Set<T>(ParameterCollection parameterCollection, PermutationParameterKey<T> key, T value)
        {
            parameterCollection.Set(key, value);
        }

        private T Get<T>(ParameterCollection parameterCollection, PermutationParameterKey<T> key)
        {
            return parameterCollection.Get(key);
        }
    }
}
