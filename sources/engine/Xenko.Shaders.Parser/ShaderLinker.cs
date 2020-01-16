// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;

using Xenko.Core.Mathematics;
using Xenko.Core.Shaders.Ast.Xenko;
using Xenko.Shaders.Parser.Mixins;
using Xenko.Shaders.Parser.Utility;
using Xenko.Core.Shaders.Ast;
using Xenko.Core.Shaders.Ast.Hlsl;
using Xenko.Core.Shaders.Visitor;
using Xenko.Graphics;

using StorageQualifier = Xenko.Core.Shaders.Ast.StorageQualifier;

namespace Xenko.Shaders.Parser
{
    /// <summary>
    /// This AST Visitor will look for any "Link" annotation in order to bind EffectVariable to their associated HLSL variables.
    /// </summary>
    internal class ShaderLinker : ShaderWalker
    {
        private readonly Dictionary<string, SamplerStateDescription> samplers = new Dictionary<string, SamplerStateDescription>();
        private readonly EffectReflection effectReflection;
        private readonly Dictionary<EffectConstantBufferDescription, List<EffectValueDescription>> valueBindings = new Dictionary<EffectConstantBufferDescription, List<EffectValueDescription>>();
        private readonly ShaderMixinParsingResult parsingResult;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderLinker" /> class.
        /// </summary>
        /// <param name="parsingResult">The parsing result.</param>
        public ShaderLinker(ShaderMixinParsingResult parsingResult)
            : base(true, false)
        {
            this.parsingResult = parsingResult;
            this.effectReflection = parsingResult.Reflection;
        }

        /// <summary>
        /// Gets the samplers.
        /// </summary>
        public IDictionary<string, SamplerStateDescription> Samplers
        {
            get
            {
                return samplers;
            }
        }

        /// <summary>
        /// Runs the linker on the specified Shader.
        /// </summary>
        /// <param name="shader">The shader.</param>
        public void Run(Shader shader)
        {
            PrepareConstantBuffers(shader);
            Visit(shader);
            foreach (var valueBinding in valueBindings)
            {
                valueBinding.Key.Members = valueBinding.Value.ToArray();
            }
        }

        private void PrepareConstantBuffers(Shader shader)
        {
            var otherNodes = shader.Declarations.Where(declaration => !(declaration is MethodDeclaration) && !(declaration is Variable));
            // Note: flattening variables
            var variables = shader.Declarations.OfType<Variable>().SelectMany(x => x.Instances()).ToList();

            var declarations = new List<Node>();

            // Reorder:
            // - Constants (might be needed by struct/typedef)
            declarations.AddRange(variables.Where(x => x.Qualifiers.Contains(StorageQualifier.Const)));
            // - Non variable/methods (i.e. struct, typedef, cbuffer, etc...)
            declarations.AddRange(otherNodes);
            // - Variables (textures, samplers, etc...)
            declarations.AddRange(variables.Where(x => !x.Qualifiers.Contains(StorageQualifier.Const)));
            // - Method declarations
            declarations.AddRange(shader.Declarations.OfType<MethodDeclaration>());

            shader.Declarations = declarations;
        }


        /// <summary>
        /// Visits the specified variable.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>The variable visited</returns>
        public override void Visit(Variable variable)
        {
            var parameterKey = GetLinkParameterKey(variable);
            if (parameterKey == null) return;

            var resolvedType = variable.Type.ResolveType();
            var slotCount = 1;
            if (resolvedType is ArrayType)
            {
                // TODO: Use evaluator?
                slotCount = (int)((LiteralExpression)((ArrayType)resolvedType).Dimensions[0]).Literal.Value;
                resolvedType = ((ArrayType)resolvedType).Type;
            }
            if (resolvedType.IsStateType())
            {
                var samplerState = SamplerStateDescription.Default;

                var stateInitializer = variable.InitialValue as StateInitializer;
                if (stateInitializer != null)
                {
                    foreach (var samplerField in stateInitializer.Items.OfType<AssignmentExpression>())
                    {
                        string key = samplerField.Target.ToString();
                        string value = samplerField.Value.ToString();

                        if (key == "Filter")
                        {
                            switch (value)
                            {
                                case "COMPARISON_MIN_MAG_LINEAR_MIP_POINT":
                                    samplerState.Filter = TextureFilter.ComparisonMinMagLinearMipPoint;
                                    break;
                                case "COMPARISON_MIN_MAG_MIP_POINT":
                                    samplerState.Filter = TextureFilter.ComparisonPoint;
                                    break;
                                case "MIN_MAG_LINEAR_MIP_POINT":
                                    samplerState.Filter = TextureFilter.MinMagLinearMipPoint;
                                    break;
                                case "MIN_MAG_MIP_LINEAR":
                                    samplerState.Filter = TextureFilter.Linear;
                                    break;
                                case "ANISOTROPIC":
                                    samplerState.Filter = TextureFilter.Anisotropic;
                                    break;
                                case "MIN_MAG_MIP_POINT":
                                    samplerState.Filter = TextureFilter.Point;
                                    break;
                                default:
                                    parsingResult.Error(XenkoMessageCode.SamplerFilterNotSupported, variable.Span, value);
                                    break;
                            }
                        }
                        else if (key == "ComparisonFunc")
                        {
                            CompareFunction compareFunction;
                            Enum.TryParse(value, true, out compareFunction);
                            samplerState.CompareFunction = compareFunction;
                        }
                        else if (key == "AddressU" || key == "AddressV" || key == "AddressW")
                        {
                            TextureAddressMode textureAddressMode;
                            Enum.TryParse(value, true, out textureAddressMode);
                            switch (key)
                            {
                                case "AddressU":
                                    samplerState.AddressU = textureAddressMode;
                                    break;
                                case "AddressV":
                                    samplerState.AddressV = textureAddressMode;
                                    break;
                                case "AddressW":
                                    samplerState.AddressW = textureAddressMode;
                                    break;
                                default:
                                    parsingResult.Error(XenkoMessageCode.SamplerAddressModeNotSupported, variable.Span, key);
                                    break;
                            }
                        }
                        else if (key == "BorderColor")
                        {
                            var borderColor = samplerField.Value as MethodInvocationExpression;
                            if (borderColor != null)
                            {
                                var targetType = borderColor.Target as TypeReferenceExpression;
                                if (targetType != null && targetType.Type.ResolveType() == VectorType.Float4 && borderColor.Arguments.Count == 4)
                                {
                                    var values = new float[4];
                                    for (int i = 0; i < 4; i++)
                                    {
                                        var argValue = borderColor.Arguments[i] as LiteralExpression;
                                        if (argValue != null)
                                        {
                                            values[i] = (float)Convert.ChangeType(argValue.Value, typeof(float));
                                        }
                                        else
                                        {
                                            parsingResult.Error(XenkoMessageCode.SamplerBorderColorNotSupported, variable.Span, borderColor.Arguments[i]);
                                        }
                                    }

                                    samplerState.BorderColor = new Color4(values);
                                }
                                else
                                {
                                    parsingResult.Error(XenkoMessageCode.SamplerBorderColorNotSupported, variable.Span, variable);
                                }
                            }
                            else
                            {
                                parsingResult.Error(XenkoMessageCode.SamplerBorderColorNotSupported, variable.Span, variable);
                            }
                        }
                        else if (key == "MinLOD")
                        {
                            samplerState.MinMipLevel = float.Parse(value);
                        }
                        else if (key == "MaxLOD")
                        {
                            samplerState.MaxMipLevel = float.Parse(value);
                        }
                        else if (key == "MaxAnisotropy")
                        {
                            samplerState.MaxAnisotropy = int.Parse(value);
                        }
                        else
                        {
                            parsingResult.Error(XenkoMessageCode.SamplerFieldNotSupported, variable.Span, variable);
                        }
                    }

                    effectReflection.SamplerStates.Add(new EffectSamplerStateBinding(parameterKey.Name, samplerState));
                }

                LinkVariable(effectReflection, variable.Name, parameterKey, slotCount);
            }
            else if (variable.Type is TextureType || variable.Type is GenericBaseType)
            {
                LinkVariable(effectReflection, variable.Name, parameterKey, slotCount);
            }
            else
            {
                ParseConstantBufferVariable("$Globals", variable);
            }
        }

        /// <summary>
        /// Visits the specified constant buffer.
        /// </summary>
        /// <param name="constantBuffer">The constant buffer.</param>
        /// <returns></returns>
        public override void Visit(ConstantBuffer constantBuffer)
        {
            foreach (var variable in constantBuffer.Members.OfType<Variable>().SelectMany(x => x.Instances()))
            {
                ParseConstantBufferVariable(constantBuffer.Name, variable);
            }
        }

        public override void Visit(MethodDefinition method)
        {
            // Parse stream output declarations (if any)
            // TODO: Currently done twice, one time in ShaderMixer, one time in ShaderLinker
            var streamOutputAttribute = method.Attributes.OfType<AttributeDeclaration>().FirstOrDefault(x => x.Name == "StreamOutput");
            if (streamOutputAttribute != null)
            {
                var rasterizedStream = streamOutputAttribute.Parameters.LastOrDefault();

                // Ignore last parameter if it's not an integer (it means there is no rasterized stream info)
                // We should make a new StreamOutputRasterizedStream attribute instead maybe?
                if (rasterizedStream != null && !(rasterizedStream.Value is int))
                    rasterizedStream = null;

                int[] streamOutputStrides;

                // Parse declarations
                // Everything should be registered in GS_OUTPUT (previous pass in ShaderMixer).
                StreamOutputParser.Parse(effectReflection.ShaderStreamOutputDeclarations, out streamOutputStrides, streamOutputAttribute, ((StructType)FindDeclaration("GS_OUTPUT")).Fields);

                effectReflection.StreamOutputStrides = streamOutputStrides;
                effectReflection.StreamOutputRasterizedStream = rasterizedStream != null ? (int)rasterizedStream.Value : -1;
            }
        }

        /// <inheritdoc/>
        public override void VisitNode(Node node)
        {
            if (node is IDeclaration)
            {
                var parameterKey = this.GetLinkParameterKey(node);
                if (parameterKey != null)
                    LinkVariable(effectReflection, ((IDeclaration)node).Name, parameterKey, parameterKey.Type.Elements);
            }

            base.VisitNode(node);
        }

        private LocalParameterKey GetLinkParameterKey(Node node)
        {
            var qualifiers = node as IQualifiers;
            var attributable = node as IAttributes;

            if ((qualifiers != null && (qualifiers.Qualifiers.Contains(Xenko.Core.Shaders.Ast.Hlsl.StorageQualifier.Static) ||
                                        qualifiers.Qualifiers.Contains(StorageQualifier.Const) ||
                                        qualifiers.Qualifiers.Contains(Xenko.Core.Shaders.Ast.Hlsl.StorageQualifier.Groupshared)
                                       )) || attributable == null)
            {
                return null;
            }

            foreach (var annotation in attributable.Attributes.OfType<AttributeDeclaration>())
            {
                if (annotation.Name != "Link" || annotation.Parameters.Count < 1)
                {
                    continue;
                }

                var variableName = (string)annotation.Parameters[0].Value;
                var parameterKey = new LocalParameterKey() {Name = variableName};
                var variable = node as Variable;
                if (variable != null)
                {
                    var cbuffer = (ConstantBuffer)variable.GetTag(XenkoTags.ConstantBuffer);
                    if (cbuffer != null && cbuffer.Type == XenkoConstantBufferType.ResourceGroup)
                    {
                        parameterKey.ResourceGroup = cbuffer.Name;
                    }

                    parameterKey.LogicalGroup = (string)variable.GetTag(XenkoTags.LogicalGroup);

                    var variableType = variable.Type;

                    parameterKey.Type = CreateTypeInfo(variableType, attributable.Attributes, out parameterKey.ElementType);
                }

                return parameterKey;
            }

            return null;
        }

        private static EffectTypeDescription CreateTypeInfo(TypeBase variableType, List<AttributeBase> attributes, out EffectTypeDescription elementType)
        {
            elementType = default;

            var parameterTypeInfo = new EffectTypeDescription();

            if (variableType.TypeInference.TargetType != null)
                variableType = variableType.TypeInference.TargetType;

            if (variableType is ArrayType)
            {
                var arrayType = (ArrayType)variableType;
                variableType = arrayType.Type;
                parameterTypeInfo.Elements = (int)((LiteralExpression)arrayType.Dimensions[0]).Literal.Value;

                if (variableType.TypeInference.TargetType != null)
                    variableType = variableType.TypeInference.TargetType;
            }

            if (variableType is ScalarType)
            {
                // Uint and int are collapsed to int
                if (variableType == ScalarType.Int || variableType == ScalarType.UInt)
                {
                    parameterTypeInfo.Class = EffectParameterClass.Scalar;
                    parameterTypeInfo.Type = variableType == ScalarType.Int ? EffectParameterType.Int : EffectParameterType.UInt;
                }
                else if (variableType == ScalarType.Float)
                {
                    parameterTypeInfo.Class = EffectParameterClass.Scalar;
                    parameterTypeInfo.Type = EffectParameterType.Float;
                }
                else if (variableType == ScalarType.Bool)
                {
                    parameterTypeInfo.Class = EffectParameterClass.Scalar;
                    parameterTypeInfo.Type = EffectParameterType.Bool;
                }

                parameterTypeInfo.RowCount = 1;
                parameterTypeInfo.ColumnCount = 1;
            }
            else if (variableType is VectorType)
            {
                if (variableType == VectorType.Float2 || variableType == VectorType.Float3 || variableType == VectorType.Float4)
                {
                    bool isColor = attributes.OfType<AttributeDeclaration>().Any(x => x.Name == "Color");
                    parameterTypeInfo.Class = isColor ? EffectParameterClass.Color : EffectParameterClass.Vector;
                    parameterTypeInfo.Type = EffectParameterType.Float;
                }
                else if (variableType == VectorType.Int2 || variableType == VectorType.Int3 || variableType == VectorType.Int4)
                {
                    parameterTypeInfo.Class = EffectParameterClass.Vector;
                    parameterTypeInfo.Type = EffectParameterType.Int;
                }
                else if (variableType == VectorType.UInt2 || variableType == VectorType.UInt3 || variableType == VectorType.UInt4)
                {
                    parameterTypeInfo.Class = EffectParameterClass.Vector;
                    parameterTypeInfo.Type = EffectParameterType.UInt;
                }

                parameterTypeInfo.RowCount = 1;
                parameterTypeInfo.ColumnCount = ((VectorType)variableType).Dimension;
            }
            else if (variableType is MatrixType)
            {
                parameterTypeInfo.Class = EffectParameterClass.MatrixColumns;
                parameterTypeInfo.Type = EffectParameterType.Float;
                parameterTypeInfo.RowCount = ((MatrixType)variableType).RowCount;
                parameterTypeInfo.ColumnCount = ((MatrixType)variableType).ColumnCount;
            }
            else if (variableType is StructType)
            {
                var structType = (StructType)variableType;

                parameterTypeInfo.Class = EffectParameterClass.Struct;
                parameterTypeInfo.RowCount = 1;
                parameterTypeInfo.ColumnCount = 1;
                parameterTypeInfo.Name = structType.Name.Text;

                var members = new List<EffectTypeMemberDescription>();
                foreach (var field in structType.Fields)
                {
                    var memberInfo = new EffectTypeMemberDescription
                    {
                        Name = field.Name.Text,
                        Type = CreateTypeInfo(field.Type, field.Attributes, out var _),
                    };
                    members.Add(memberInfo);
                }

                parameterTypeInfo.Members = members.ToArray();
            }
            else
            {
                var variableTypeName = variableType.Name.Text.ToLowerInvariant();

                if (variableType is ClassType classType && classType.GenericArguments.Count == 1)
                {
                    elementType = CreateTypeInfo(classType.GenericArguments[0], new List<AttributeBase>(), out var _);
                }

                switch (variableTypeName)
                {
                    case "cbuffer":
                        parameterTypeInfo.Class = EffectParameterClass.ConstantBuffer;
                        parameterTypeInfo.Type = EffectParameterType.ConstantBuffer;
                        break;

                    case "tbuffer":
                        parameterTypeInfo.Class = EffectParameterClass.TextureBuffer;
                        parameterTypeInfo.Type = EffectParameterType.TextureBuffer;
                        break;

                    case "structuredbuffer":
                        parameterTypeInfo.Class = EffectParameterClass.ShaderResourceView;
                        parameterTypeInfo.Type = EffectParameterType.StructuredBuffer;
                        break;
                    case "rwstructuredbuffer":
                        parameterTypeInfo.Class = EffectParameterClass.UnorderedAccessView;
                        parameterTypeInfo.Type = EffectParameterType.RWStructuredBuffer;
                        break;
                    case "consumestructuredbuffer":
                        parameterTypeInfo.Class = EffectParameterClass.UnorderedAccessView;
                        parameterTypeInfo.Type = EffectParameterType.ConsumeStructuredBuffer;
                        break;
                    case "appendstructuredbuffer":
                        parameterTypeInfo.Class = EffectParameterClass.UnorderedAccessView;
                        parameterTypeInfo.Type = EffectParameterType.AppendStructuredBuffer;
                        break;
                    case "buffer":
                        parameterTypeInfo.Class = EffectParameterClass.ShaderResourceView;
                        parameterTypeInfo.Type = EffectParameterType.Buffer;
                        break;
                    case "rwbuffer":
                        parameterTypeInfo.Class = EffectParameterClass.UnorderedAccessView;
                        parameterTypeInfo.Type = EffectParameterType.RWBuffer;
                        break;
                    case "byteaddressbuffer":
                        parameterTypeInfo.Class = EffectParameterClass.ShaderResourceView;
                        parameterTypeInfo.Type = EffectParameterType.ByteAddressBuffer;
                        break;
                    case "rwbyteaddressbuffer":
                        parameterTypeInfo.Class = EffectParameterClass.UnorderedAccessView;
                        parameterTypeInfo.Type = EffectParameterType.RWByteAddressBuffer;
                        break;

                    case "texture1d":
                        parameterTypeInfo.Class = EffectParameterClass.ShaderResourceView;
                        parameterTypeInfo.Type = EffectParameterType.Texture1D;
                        break;

                    case "texturecube":
                        parameterTypeInfo.Class = EffectParameterClass.ShaderResourceView;
                        parameterTypeInfo.Type = EffectParameterType.TextureCube;
                        break;

                    case "texture2d":
                        parameterTypeInfo.Class = EffectParameterClass.ShaderResourceView;
                        parameterTypeInfo.Type = EffectParameterType.Texture2D;
                        break;

                    case "texture2dms":
                        parameterTypeInfo.Class = EffectParameterClass.ShaderResourceView;
                        parameterTypeInfo.Type = EffectParameterType.Texture2DMultisampled;
                        break;                   

                    case "texture3d":
                        parameterTypeInfo.Class = EffectParameterClass.ShaderResourceView;
                        parameterTypeInfo.Type = EffectParameterType.Texture3D;
                        break;

                    case "texture1darray":
                        parameterTypeInfo.Class = EffectParameterClass.ShaderResourceView;
                        parameterTypeInfo.Type = EffectParameterType.Texture1DArray;
                        break;

                    case "texturecubearray":
                        parameterTypeInfo.Class = EffectParameterClass.ShaderResourceView;
                        parameterTypeInfo.Type = EffectParameterType.TextureCubeArray;
                        break;

                    case "texture2darray":
                        parameterTypeInfo.Class = EffectParameterClass.ShaderResourceView;
                        parameterTypeInfo.Type = EffectParameterType.Texture2DArray;
                        break;

                    case "texture2dmsarray":
                        parameterTypeInfo.Class = EffectParameterClass.ShaderResourceView;
                        parameterTypeInfo.Type = EffectParameterType.Texture2DMultisampledArray;
                        break;

                    case "rwtexture1d":
                        parameterTypeInfo.Class = EffectParameterClass.UnorderedAccessView;
                        parameterTypeInfo.Type = EffectParameterType.RWTexture1D;
                        break;

                    case "rwtexture2d":
                        parameterTypeInfo.Class = EffectParameterClass.UnorderedAccessView;
                        parameterTypeInfo.Type = EffectParameterType.RWTexture2D;
                        break;

                    case "rwtexture3d":
                        parameterTypeInfo.Class = EffectParameterClass.UnorderedAccessView;
                        parameterTypeInfo.Type = EffectParameterType.RWTexture3D;
                        break;

                    case "rwtexture1darray":
                        parameterTypeInfo.Class = EffectParameterClass.UnorderedAccessView;
                        parameterTypeInfo.Type = EffectParameterType.RWTexture1DArray;
                        break;

                    case "rwtexture2darray":
                        parameterTypeInfo.Class = EffectParameterClass.UnorderedAccessView;
                        parameterTypeInfo.Type = EffectParameterType.RWTexture2DArray;
                        break;

                    case "samplerstate":
                    case "samplercomparisonstate":
                        parameterTypeInfo.Class = EffectParameterClass.Sampler;
                        parameterTypeInfo.Type = EffectParameterType.Sampler;
                        break;
                }
            }

            return parameterTypeInfo;
        }

        private int ComputeSize(TypeBase type)
        {
            if (type.TypeInference.TargetType != null)
                type = type.TypeInference.TargetType;

            var structType = type as StructType;
            if (structType != null)
            {
                var structSize = 0;
                foreach (var field in structType.Fields)
                {
                    var memberSize = ComputeSize(field.Type);

                    // Seems like this element needs to be split accross multiple lines, 
                    if ((structSize + memberSize - 1) / 16 != structSize / 16)
                        structSize = (structSize + 16 - 1) / 16 * 16;

                    structSize += memberSize;
                }
                return structSize;
            }

            if (type is ScalarType)
            {
                // Uint and int are collapsed to int
                if (type == ScalarType.Int || type == ScalarType.UInt
                    || type == ScalarType.Float || type == ScalarType.Bool)
                {
                    return 4;
                }
                else if (type == ScalarType.Double)
                {
                    return 8;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            var vectorType = type as VectorType;
            if (vectorType != null)
            {
                return ComputeSize(vectorType.Type) * vectorType.Dimension;
            }

            var matrixType = type as MatrixType;
            if (matrixType is MatrixType)
            {
                return (4 * (matrixType.ColumnCount - 1) + matrixType.RowCount);
            }

            throw new NotImplementedException();
        }

        private void ParseConstantBufferVariable(string cbName, Variable variable)
        {
            if (variable.Qualifiers.Contains(Xenko.Core.Shaders.Ast.Hlsl.StorageQualifier.Static) ||
                variable.Qualifiers.Contains(StorageQualifier.Const) ||
                variable.Qualifiers.Contains(Xenko.Core.Shaders.Ast.Hlsl.StorageQualifier.Groupshared)
                )
                return;

            if (variable.Qualifiers.Contains(XenkoStorageQualifier.Stream))
            {
                parsingResult.Error(XenkoMessageCode.StreamVariableWithoutPrefix, variable.Span, variable);
                return;
            }

            foreach (var attribute in variable.Attributes.OfType<AttributeDeclaration>())
            {
                if (attribute.Name == "Link")
                {
                    if (attribute.Parameters.Count != 1)
                    {
                        parsingResult.Error(XenkoMessageCode.LinkArgumentsError, variable.Span);
                    }
                }
            }

            //// Try to resolve key
            var parameterKey = GetLinkParameterKey(variable);

            if (parameterKey != null)
            {
                LinkConstant(cbName, variable, parameterKey);
            }
            else
            {
                parsingResult.Error(XenkoMessageCode.LinkError, variable.Span, variable);
            }
        }

        private static void LinkVariable(EffectReflection reflection, string variableName, LocalParameterKey parameterKey, int slotCount)
        {
            var binding = new EffectResourceBindingDescription { KeyInfo = { KeyName = parameterKey.Name }, Class = parameterKey.Type.Class, Type = parameterKey.Type.Type, ElementType = parameterKey.ElementType, RawName = variableName, SlotStart = -1, SlotCount = slotCount > 0 ? slotCount : 1, ResourceGroup = parameterKey.ResourceGroup, LogicalGroup = parameterKey.LogicalGroup };
            reflection.ResourceBindings.Add(binding);
        }

        private void LinkConstant(string cbName, Variable variable, LocalParameterKey parameterKey)
        {
            // If the constant buffer is not present, add it
            var constantBuffer = effectReflection.ConstantBuffers.FirstOrDefault(buffer => buffer.Name == cbName);
            if (constantBuffer == null)
            {
                constantBuffer = new EffectConstantBufferDescription() {Name = cbName, Type = ConstantBufferType.ConstantBuffer};
                effectReflection.ConstantBuffers.Add(constantBuffer);
                var constantBufferBinding = new EffectResourceBindingDescription { KeyInfo = { KeyName = cbName }, Class = EffectParameterClass.ConstantBuffer, Type = EffectParameterType.ConstantBuffer, RawName = cbName, SlotStart = -1, SlotCount = 1, ResourceGroup = cbName };
                effectReflection.ResourceBindings.Add(constantBufferBinding);
                valueBindings.Add(constantBuffer, new List<EffectValueDescription>());
            }

            // Get the list of members of this constant buffer
            var members = valueBindings[constantBuffer];

            var binding = new EffectValueDescription
            {
                KeyInfo =
                {
                    KeyName = parameterKey.Name,
                },
                LogicalGroup = (string)variable.GetTag(XenkoTags.LogicalGroup),
                Type = parameterKey.Type,
                RawName = variable.Name,
            };
            
            members.Add(binding);
        }

        private class LocalParameterKey
        {
            public string Name;

            public string ResourceGroup;

            public string LogicalGroup;

            public EffectTypeDescription Type;

            /// <summary>
            /// The element type (for buffers or textures).
            /// </summary>
            public EffectTypeDescription ElementType;

            /*public EffectParameterClass Class;

            public EffectParameterType Type;

            public int RowCount;

            public int ColumnCount;

            public int Elements;

            public string TypeName;
            public EffectParameterTypeInfo[] Members;*/
        }
    }
}
