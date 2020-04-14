// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Visitor;

namespace Stride.Core.Shaders.Convertor
{
    /// <summary>
    /// Collect the texture and sampler pair used in the HLSL shader.
    /// </summary>
    class SamplerMappingVisitor : CallstackVisitor
    {
        private Dictionary<SamplerTextureKey, Variable> samplerMapping;
        private HashSet<Variable> textureAccesses;
        private Shader shader;
        private List<TextureSamplerMethodKey> textureSamplerMethods = new List<TextureSamplerMethodKey>();
        private static readonly string ScopeValueKey = "ScopeValue";
        private CloneContext cloneContext = new CloneContext();

        public SamplerMappingVisitor(Shader shader, Dictionary<SamplerTextureKey, Variable> samplerMapping)
        {
            this.shader = shader;
            this.samplerMapping = samplerMapping;
            textureAccesses = new HashSet<Variable>();

            // Add all global declarations for clone context in order to avoid any clone on this object
            foreach (var variable in shader.Declarations)
            {
                cloneContext.Add(variable, variable);
            }
        }

        /// <summary>
        /// Gets or sets a flag specifying whether compatibility profile is used for texture functions.
        /// As an example, with compatibility on, texture() might become texture2D().
        /// </summary>
        /// <value>
        /// true if texture compatibility profile is enabled, false if not.
        /// </value>
        public bool TextureFunctionsCompatibilityProfile { get; set; }

        public override void Run(MethodDefinition methodEntry)
        {
            base.Run(methodEntry);

            var existingTextures = new HashSet<Variable>(samplerMapping.Select(x => x.Key.Texture));
            foreach (var texture in textureAccesses)
            {
                if (!existingTextures.Contains(texture))
                    GenerateGLSampler(null, texture);
            }

            for (int i = this.textureSamplerMethods.Count - 1; i >= 0; i--)
            {
                var textureSamplerMethodKey = this.textureSamplerMethods[i];
                var entryIndex = shader.Declarations.IndexOf(textureSamplerMethodKey.Method);
                this.shader.Declarations.Insert(entryIndex, textureSamplerMethodKey.NewMethod);
            }
        }

        private Variable FindGlobalVariable(Expression expression)
        {
            var variableRef = expression as VariableReferenceExpression;
            if (variableRef != null)
            {
                var variable = variableRef.TypeInference.Declaration as Variable;

                if (variable != null)
                {
                    // If a variable has an initial value, find the global variable
                    if (!shader.Declarations.Contains(variable) && variable.InitialValue != null)
                    {
                        return this.FindGlobalVariable(variable.InitialValue);
                    }

                    variable = (Variable)variable.GetTag(ScopeValueKey) ?? variable;

                    // Is this a global variable?
                    if (shader.Declarations.Contains(variable))
                    {
                        return variable;
                    }
                }
            }
            return null;
        }

        public override Node Visit(VariableReferenceExpression variableRef)
        {
            ((ScopeDeclarationWithRef)ScopeStack.Peek()).VariableReferences.Add(variableRef);
            return variableRef;
        }

        public override Node Visit(MethodInvocationExpression methodInvocationExpression)
        {
            // Visit first children
            base.Visit(methodInvocationExpression);

            // Convert member expression
            var variableRef = methodInvocationExpression.Target as VariableReferenceExpression;
            var memberRef = methodInvocationExpression.Target as MemberReferenceExpression;
            if (memberRef != null)
            {
                // TODO handle Texture2D<float> 
                var textureVariable = this.FindGlobalVariable(memberRef.Target);

                if (textureVariable != null)
                {
                    var textureType = textureVariable.Type.ResolveType();

                    if (textureType is TextureType || (CultureInfo.InvariantCulture.CompareInfo.IsPrefix(textureType.Name.Text, "Texture", CompareOptions.IgnoreCase))
                        || (textureType.IsBuiltIn && textureType.Name.Text.StartsWith("Buffer")))
                    {
                        switch (memberRef.Member)
                        {
                            case "Load":
                                {
                                    GenerateGLSampler(null, textureVariable);
                                }
                                break;
                            case "GetDimensions":
                                {
                                    textureAccesses.Add(textureVariable);
                                }
                                break;
                            case "Sample":
                            case "SampleBias":
                            case "SampleGrad":
                            case "SampleLevel":
                            case "SampleCmp":
                            case "SampleCmpLevelZero":
                                {
                                    var sampler = this.FindGlobalVariable(methodInvocationExpression.Arguments[0]);
                                    if (sampler == null)
                                        throw new InvalidOperationException(string.Format("Unable to find sampler [{0}] as a global variable",
                                                                                          methodInvocationExpression.Arguments[0]));

                                    bool needsComparison = memberRef.Member == "SampleCmp" || memberRef.Member == "SampleCmpLevelZero";
                                    GenerateGLSampler(sampler, textureVariable, needsComparison);
                                }
                                break;
                        }
                    }
                }
            }
            else if (variableRef != null)
            {
                string methodName = variableRef.Name.Text;

                // Transform texture fetch
                var texFetchInfo = ParseTexFetch(methodName);
                if (texFetchInfo != null)
                {
                    var fetchInstructionSecondPart = string.Empty;
                    switch (texFetchInfo.Item2)
                    {
                        case TexFetchType.Default:
                            if (methodInvocationExpression.Arguments.Count == 4)
                                fetchInstructionSecondPart = "Grad";
                            break;
                        case TexFetchType.Bias:
                            // Bias is encoded in w, so replicated argument and extract only w. Compiler/optimizer should do the rest of the job.
                            methodInvocationExpression.Arguments.Add(new MemberReferenceExpression(new ParenthesizedExpression(methodInvocationExpression.Arguments[1]), "w"));
                            break;
                        case TexFetchType.Grad:
                            fetchInstructionSecondPart = "Grad";
                            break;
                        case TexFetchType.Proj:
                            fetchInstructionSecondPart = "Proj";
                            break;
                        case TexFetchType.Lod:
                            fetchInstructionSecondPart = "Lod";

                            // LOD is encoded in w, so replicated argument and extract only w. Compiler/optimizer should do the rest of the job.
                            methodInvocationExpression.Arguments.Add(new MemberReferenceExpression(new ParenthesizedExpression(methodInvocationExpression.Arguments[1]), "w"));
                            break;
                    }

                    if (TextureFunctionsCompatibilityProfile)
                    {
                        var stringBuilder = new StringBuilder("texture", 32);
                        if (texFetchInfo.Item1 == 4)
                            stringBuilder.Append("Cube");
                        else
                            stringBuilder.Append(texFetchInfo.Item1).Append('D');
                        stringBuilder.Append(fetchInstructionSecondPart);
                        variableRef.Name = stringBuilder.ToString();
                    }
                    else
                    {
                        variableRef.Name = "texture" + fetchInstructionSecondPart;
                    }

                    // TODO: Check how many components are required (for now it only do xy, but it might be x or xyz depending on texture dimension).
                    if (texFetchInfo.Item2 != TexFetchType.Proj)
                    {
                        var previousArgument = methodInvocationExpression.Arguments[1];
                        var sizeOfArguments = texFetchInfo.Item1 == 4 ? 3 : texFetchInfo.Item1;
                        var vectorType = previousArgument.TypeInference.TargetType as VectorType;

                        // If argument type is not the size of the expected argument, use explicit swizzle
                        if (vectorType == null || vectorType.Dimension != sizeOfArguments)
                            methodInvocationExpression.Arguments[1] = new MemberReferenceExpression(new ParenthesizedExpression(previousArgument), "xyzw".Substring(0, sizeOfArguments));
                    }

                    // Add the sampler
                    var samplerRefExpr = methodInvocationExpression.Arguments[0] as VariableReferenceExpression;
                    if (samplerRefExpr != null)
                    {
                        var samplerVariable = samplerRefExpr.TypeInference.Declaration as Variable;
                        var newSamplerType = texFetchInfo.Item1 < 4 ? new ObjectType("sampler" + texFetchInfo.Item1 + "D") : new ObjectType("samplerCube");
                        this.ChangeVariableType(samplerVariable, newSamplerType);
                    }
                }
            }

            return methodInvocationExpression;
        }

        private void ChangeVariableType(Variable samplerVariable, TypeBase newType)
        {
            if (samplerVariable != null)
            {
                samplerVariable.Type = newType;
                if (samplerVariable is Parameter)
                {
                    return;
                }

                var variableInitialValue = samplerVariable.InitialValue as VariableReferenceExpression;
                if (variableInitialValue != null)
                {
                    this.ChangeVariableType(variableInitialValue.TypeInference.Declaration as Variable, newType);
                }
            }
        }

        protected override void  ProcessMethodInvocation(MethodInvocationExpression invoke, MethodDefinition method)
        {
            var textureParameters = new List<Parameter>();
            var parameterValues = new List<Expression>();
            var parameterGlobalValues = new List<Variable>();

            var samplerTypes = new List<int>();

            for (int i = 0; i < method.Parameters.Count; i++)
            {
                var parameter = method.Parameters[i];
                if (parameter.Type is TextureType || parameter.Type.IsStateType())
                {
                    textureParameters.Add(parameter);

                    // Find global variable
                    var parameterValue = this.FindGlobalVariable(invoke.Arguments[i]);

                    // Set the tag ScopeValue for the current parameter
                    parameter.SetTag(ScopeValueKey, parameterValue);

                    // Add only new variable
                    if (!parameterGlobalValues.Contains(parameterValue))
                        parameterGlobalValues.Add(parameterValue);
                }
                else if ( i < invoke.Arguments.Count)
                {
                    parameterValues.Add(invoke.Arguments[i]);
                    if (parameter.Type.IsSamplerType())
                    {
                        samplerTypes.Add(i);
                    }
                }
            }

            // We have texture/sampler parameters. We need to generate a new specialized method
            if (textureParameters.Count > 0)
            {
                // Order parameter values by name
                parameterGlobalValues.Sort((left, right) => left.Name.Text.CompareTo(right.Name.Text));

                var methodKey = new TextureSamplerMethodKey(method);

                int indexOf = textureSamplerMethods.IndexOf(methodKey);

                if (indexOf < 0)
                {
                    methodKey.Initialize(cloneContext);
                    textureSamplerMethods.Add(methodKey);
                }
                else
                {
                    // If a key is found again, add it as it was reused in order to keep usage in order
                    methodKey = textureSamplerMethods[indexOf];
                    textureSamplerMethods.RemoveAt(indexOf);
                    textureSamplerMethods.Add(methodKey);
                }

                methodKey.Invokers.Add(invoke);

                var newTarget = new VariableReferenceExpression(methodKey.NewMethod.Name) { TypeInference = { Declaration = methodKey.NewMethod, TargetType = invoke.TypeInference.TargetType } };
                invoke.Target = newTarget;
                invoke.Arguments = parameterValues;
                invoke.TypeInference.Declaration = methodKey.NewMethod;
                invoke.TypeInference.TargetType = invoke.TypeInference.TargetType;

                this.VisitDynamic(methodKey.NewMethod);
            }
            else
            {
                // Visit the method callstack
                this.VisitDynamic(method);

                // There is an anonymous sampler type
                // We need to resolve its types after the method definition was processed
                if (samplerTypes.Count > 0)
                {
                    foreach (var samplerTypeIndex in samplerTypes)
                    {
                        var samplerRef = invoke.Arguments[samplerTypeIndex] as VariableReferenceExpression;
                        if (samplerRef != null)
                        {
                            var samplerDecl = samplerRef.TypeInference.Declaration as Variable;
                            ChangeVariableType(samplerDecl, method.Parameters[samplerTypeIndex].Type);
                        }
                    }
                }
            }

            // Remove temporary parameters
            if (textureParameters.Count > 0)
            {
                foreach (var textureParameter in textureParameters)
                {
                    textureParameter.RemoveTag(ScopeValueKey);
                }
            }
        }

        /// <summary>
        /// Generates a OpenGL sampler based on sampler/texture combination.
        /// </summary>
        /// <param name="sampler">The D3D sampler (can be null).</param>
        /// <param name="texture">The D3D texture.</param>
        private void GenerateGLSampler(Variable sampler, Variable texture, bool needsComparison = false)
        {
            Variable glslSampler;

            if (texture == null)
                throw new InvalidOperationException();

            var samplerKey = new SamplerTextureKey(sampler, texture);
            if (!samplerMapping.TryGetValue(samplerKey, out glslSampler))
            {
                var samplerType = texture.Type.ResolveType();
                var samplerTypeName = samplerType.Name.Text;

                if (samplerTypeName.StartsWith("Texture"))
                    samplerTypeName = "sampler" + samplerTypeName.Substring("Texture".Length);
                else if (samplerTypeName.StartsWith("Buffer"))
                    samplerTypeName = "samplerBuffer";

                // TODO: How do we support this on OpenGL ES 2.0? Cast to int/uint on Load()/Sample()?
                var genericSamplerType = samplerType as IGenerics;
                if (genericSamplerType != null && genericSamplerType.GenericArguments.Count == 1)
                {
                    var genericArgument = genericSamplerType.GenericArguments[0].ResolveType();
                    if (TypeBase.GetBaseType(genericArgument) == ScalarType.UInt)
                        samplerTypeName = "u" + samplerTypeName;
                    else if (TypeBase.GetBaseType(genericArgument) == ScalarType.Int)
                        samplerTypeName = "i" + samplerTypeName;
                }

                // Handle comparison samplers
                if (needsComparison)
                    samplerTypeName += "Shadow";

                glslSampler = new Variable(new TypeName(samplerTypeName), texture.Name + (sampler != null ? "_" + sampler.Name : "_NoSampler")) { Span = sampler == null ? texture.Span : sampler.Span };
                samplerMapping.Add(samplerKey, glslSampler);
            }
        }

        /// <summary>
        /// Parses the texture fetch.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// A tuple indicating the dimension and the <see cref="TexFetchType"/>
        /// </returns>
        private static Tuple<int, TexFetchType> ParseTexFetch(string name)
        {
            if (!name.StartsWith("tex"))
                return null;

            name = name.Substring(3);

            int dimension;

            if (name.StartsWith("1D"))
                dimension = 1;
            else if (name.StartsWith("2D"))
                dimension = 2;
            else if (name.StartsWith("3D"))
                dimension = 3;
            else if (name.StartsWith("CUBE"))
                dimension = 4;
            else
                return null;

            // Remove parsed size
            name = name.Substring((dimension == 4) ? 4 : 2);

            TexFetchType fetchType;
            switch (name)
            {
                case "":
                    fetchType = TexFetchType.Default;
                    break;
                case "lod":
                    fetchType = TexFetchType.Lod;
                    break;
                case "grad":
                    fetchType = TexFetchType.Grad;
                    break;
                case "bias":
                    fetchType = TexFetchType.Bias;
                    break;
                case "proj":
                    fetchType = TexFetchType.Proj;
                    break;
                default:
                    return null;
            }

            return new Tuple<int, TexFetchType>(dimension, fetchType);
        }


        /// <summary>
        /// Texture fetch type.
        /// </summary>
        private enum TexFetchType
        {
            /// <summary>
            /// Default fetch.
            /// </summary>
            Default,

            /// <summary>
            /// Mipmap Lod fetch.
            /// </summary>
            Lod,

            /// <summary>
            /// Gradient fetch.
            /// </summary>
            Grad,

            /// <summary>
            /// Bias fetch.
            /// </summary>
            Bias,

            /// <summary>
            /// Proj fetch.
            /// </summary>
            Proj,
        }

        protected override ScopeDeclaration NewScope(IScopeContainer container = null)
        {
            return new ScopeDeclarationWithRef(container);
        }

        private class ScopeDeclarationWithRef : ScopeDeclaration
        {
            public ScopeDeclarationWithRef()
                : this(null)
            {
            }

            public ScopeDeclarationWithRef(IScopeContainer scopeContainer)
                : base(scopeContainer)
            {
                VariableReferences = new List<VariableReferenceExpression>();
            }

            public List<VariableReferenceExpression> VariableReferences { get; private set; }
        }
    


        private class TextureSamplerMethodKey
        {
            private string methodName;

            public TextureSamplerMethodKey(MethodDefinition method)
            {
                Invokers = new List<MethodInvocationExpression>();
                this.Method = method;

                Variables = new List<Variable>();
                foreach (var parameter in Method.Parameters)
                {
                    var variableValue = (Variable)parameter.GetTag(ScopeValueKey);
                    if (variableValue != null)
                    {
                        Variables.Add(variableValue);
                    }
                }
            }

            public List<MethodInvocationExpression> Invokers { get; set; }

            public void Initialize(CloneContext previousCloneContext)
            {
                // Clone original context
                var cloneContext = new CloneContext(previousCloneContext);

                // Removes the method to clone
                cloneContext.Remove(Method);

                // Clone the old method with the clone context
                NewMethod = Method.DeepClone(cloneContext);

                var oldParameters = NewMethod.Parameters;
                NewMethod.Parameters = new List<Parameter>();
                int j = 0;
                for (int i = 0; i < oldParameters.Count; i++)
                {
                    var parameter = oldParameters[i];
                    var variableValue = (Variable)this.Method.Parameters[i].GetTag(ScopeValueKey);
                    if (variableValue != null)
                    {
                        this.NewMethod.Body.Insert(j, new DeclarationStatement(parameter));
                        j++;
                        parameter.InitialValue = new VariableReferenceExpression(variableValue.Name) { TypeInference = { Declaration = variableValue, TargetType = variableValue.Type } };
                    }
                    else
                        NewMethod.Parameters.Add(parameter);
                }

                // Create new method with new method name
                var methodNameBuild = new StringBuilder();
                methodNameBuild.Append(Method.Name);
                foreach (var variable in Variables)
                {
                    methodNameBuild.Append("_");
                    methodNameBuild.Append(variable.Name);
                }
                methodName = methodNameBuild.ToString();
                NewMethod.Name = methodName;
            }

            public MethodDefinition Method { get; private set; }

            public List<Variable> Variables { get; private set; }

            public MethodDefinition NewMethod { get; private set; }

            public bool Equals(TextureSamplerMethodKey other)
            {
                if (ReferenceEquals(null, other))
                    return false;
                if (ReferenceEquals(this, other))
                    return true;

                if (this.Variables.Count != other.Variables.Count)
                    return false;

                if (!ReferenceEquals(other.Method, this.Method))
                    return false;

                for (int i = 0; i < this.Variables.Count; i++)
                {
                    if (!ReferenceEquals(this.Variables[i], other.Variables[i]))
                        return false;
                }

                return true;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;
                if (ReferenceEquals(this, obj))
                    return true;
                if (obj.GetType() != typeof(TextureSamplerMethodKey))
                    return false;
                return Equals((TextureSamplerMethodKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int result = 0;
                    foreach (var variable in Variables)
                    {
                        result = (result * 397) ^ variable.GetHashCode();
                    }
                    result = (result * 397) ^ (this.Method != null ? this.Method.GetHashCode() : 0);
                    return result;
                }
            }

            public override string ToString()
            {
                return NewMethod == null ? "[" + Method.Name.Text + "]" : NewMethod.Name.Text;
            }
        }
    }
}
