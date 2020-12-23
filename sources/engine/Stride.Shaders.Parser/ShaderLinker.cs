// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;

using Stride.Core.Mathematics;
using Stride.Core.Shaders.Ast.Stride;
using Stride.Shaders.Parser.Mixins;
using Stride.Shaders.Parser.Utility;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Visitor;
using Stride.Graphics;

using StorageQualifier = Stride.Core.Shaders.Ast.StorageQualifier;

namespace Stride.Shaders.Parser
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
                                    parsingResult.Error(StrideMessageCode.SamplerFilterNotSupported, variable.Span, value);
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
                                    parsingResult.Error(StrideMessageCode.SamplerAddressModeNotSupported, variable.Span, key);
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
                                            parsingResult.Error(StrideMessageCode.SamplerBorderColorNotSupported, variable.Span, borderColor.Arguments[i]);
                                        }
                                    }

                                    samplerState.BorderColor = new Color4(values);
                                }
                                else
                                {
                                    parsingResult.Error(StrideMessageCode.SamplerBorderColorNotSupported, variable.Span, variable);
                                }
                            }
                            else
                            {
                                parsingResult.Error(StrideMessageCode.SamplerBorderColorNotSupported, variable.Span, variable);
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
                            parsingResult.Error(StrideMessageCode.SamplerFieldNotSupported, variable.Span, variable);
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

            if ((qualifiers != null && (qualifiers.Qualifiers.Contains(Stride.Core.Shaders.Ast.Hlsl.StorageQualifier.Static) ||
                                        qualifiers.Qualifiers.Contains(StorageQualifier.Const) ||
                                        qualifiers.Qualifiers.Contains(Stride.Core.Shaders.Ast.Hlsl.StorageQualifier.Groupshared)
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
                    var cbuffer = (ConstantBuffer)variable.GetTag(StrideTags.ConstantBuffer);
                    if (cbuffer != null && cbuffer.Type == StrideConstantBufferType.ResourceGroup)
                    {
                        parameterKey.ResourceGroup = cbuffer.Name;
                    }

                    parameterKey.LogicalGroup = (string)variable.GetTag(StrideTags.LogicalGroup);

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
                if (variableType == ScalarType.Int)
                {
                    parameterTypeInfo.Class = EffectParameterClass.Scalar;
                    parameterTypeInfo.Type = EffectParameterType.Int;
                }
                else if (variableType == ScalarType.UInt)
                {
                    parameterTypeInfo.Class = EffectParameterClass.Scalar;
                    parameterTypeInfo.Type = EffectParameterType.UInt;
                }
                else if (variableType == ScalarType.Float)
                {
                    parameterTypeInfo.Class = EffectParameterClass.Scalar;
                    parameterTypeInfo.Type = EffectParameterType.Float;
                }
                else if (variableType == ScalarType.Double)
                {
                    parameterTypeInfo.Class = EffectParameterClass.Scalar;
                    parameterTypeInfo.Type = EffectParameterType.Double;
                }
                else if (variableType == ScalarType.Bool)
                {
                    parameterTypeInfo.Class = EffectParameterClass.Scalar;
                    parameterTypeInfo.Type = EffectParameterType.Bool;
                }

                parameterTypeInfo.RowCount = 1;
                parameterTypeInfo.ColumnCount = 1;
            }
            else if (variableType is VectorType vectorType)
            {
                if (vectorType.Type == ScalarType.Float)
                {
                    bool isColor = attributes.OfType<AttributeDeclaration>().Any(x => x.Name == "Color");
                    parameterTypeInfo.Class = isColor ? EffectParameterClass.Color : EffectParameterClass.Vector;
                    parameterTypeInfo.Type = EffectParameterType.Float;
                }
                else if (vectorType.Type == ScalarType.Double)
                {
                    parameterTypeInfo.Class = EffectParameterClass.Vector;
                    parameterTypeInfo.Type = EffectParameterType.Double;
                }
                else if (vectorType.Type == ScalarType.Int)
                {
                    parameterTypeInfo.Class = EffectParameterClass.Vector;
                    parameterTypeInfo.Type = EffectParameterType.Int;
                }
                else if (vectorType.Type == ScalarType.UInt)
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
            if (variable.Qualifiers.Contains(Stride.Core.Shaders.Ast.Hlsl.StorageQualifier.Static) ||
                variable.Qualifiers.Contains(StorageQualifier.Const) ||
                variable.Qualifiers.Contains(Stride.Core.Shaders.Ast.Hlsl.StorageQualifier.Groupshared)
                )
                return;

            if (variable.Qualifiers.Contains(StrideStorageQualifier.Stream))
            {
                parsingResult.Error(StrideMessageCode.StreamVariableWithoutPrefix, variable.Span, variable);
                return;
            }

            foreach (var attribute in variable.Attributes.OfType<AttributeDeclaration>())
            {
                if (attribute.Name == "Link")
                {
                    if (attribute.Parameters.Count != 1)
                    {
                        parsingResult.Error(StrideMessageCode.LinkArgumentsError, variable.Span);
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
                parsingResult.Error(StrideMessageCode.LinkError, variable.Span, variable);
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
                LogicalGroup = (string)variable.GetTag(StrideTags.LogicalGroup),
                Type = parameterKey.Type,
                RawName = variable.Name,
                DefaultValue = ParseDefaultValue(variable),
            };
            
            members.Add(binding);
        }

        private object ParseDefaultValue(Variable variable)
        {
            var initialValue = variable.InitialValue;
            if (initialValue is null)
                return default;

            var parameterType = variable.Type.ResolveType();
            if (parameterType is ScalarType scalarType)
            {
                if (scalarType == ScalarType.Bool)
                    return ValueParsing.ToScalar<bool>(initialValue);
                else if (scalarType == ScalarType.Float)
                    return ValueParsing.ToScalar<float>(initialValue);
                else if (scalarType == ScalarType.Double)
                    return ValueParsing.ToScalar<double>(initialValue);
                else if (scalarType == ScalarType.Half)
                    return ValueParsing.ToScalar<Half>(initialValue);
                else if (scalarType == ScalarType.Int)
                    return ValueParsing.ToScalar<int>(initialValue);
                else if (scalarType == ScalarType.UInt)
                    return ValueParsing.ToScalar<uint>(initialValue);
            }
            else if (parameterType is VectorType vectorType)
            {
                var componentType = vectorType.Type;
                if (componentType == ScalarType.Float)
                {
                    var isColor = variable.Attributes.OfType<AttributeDeclaration>().Any(x => x.Name == "Color");
                    if (isColor)
                        return ValueParsing.ToColor(initialValue, vectorType.Dimension);
                    else
                        return ValueParsing.ToFloatVector(initialValue, vectorType.Dimension);
                }
                else if (componentType == ScalarType.Double)
                    return ValueParsing.ToDoubleVector(initialValue, vectorType.Dimension);
                else if (componentType == ScalarType.Half)
                    return ValueParsing.ToHalfVector(initialValue, vectorType.Dimension);
                else if (componentType == ScalarType.Int)
                    return ValueParsing.ToIntVector(initialValue, vectorType.Dimension);
                else if (componentType == ScalarType.UInt)
                    return ValueParsing.ToUIntVector(initialValue, vectorType.Dimension);
            }
            else if (parameterType is MatrixType matrixType)
            {
                var componentType = matrixType.Type;
                if (componentType == ScalarType.Float)
                {
                    if (matrixType.RowCount == 4 && matrixType.ColumnCount == 4)
                        return ValueParsing.ToVector(initialValue, (float[] args) => new Matrix(args));
                }
            }
            else if (parameterType is ArrayType arrayType)
            {
                if (initialValue is ArrayInitializerExpression arrayInitializer)
                    return ValueParsing.ToArray(arrayInitializer.Items, arrayType.Type);
            }

            return default;
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

        private static class ValueParsing
        {
            public static Array ToArray(List<Expression> v, TypeBase elementType)
            {
                if (elementType == ScalarType.Float)
                    return ToArray<float>(v);
                if (elementType == ScalarType.Double)
                    return ToArray<double>(v);
                if (elementType == ScalarType.Half)
                    return ToArray<Half>(v);
                if (elementType == ScalarType.Int)
                    return ToArray<int>(v);
                if (elementType == ScalarType.UInt)
                    return ToArray<uint>(v);
                if (elementType == ScalarType.Bool)
                    return ToArray<bool>(v);
                if (elementType == VectorType.Float2)
                    return ToArray(v, ToVector2);
                if (elementType == VectorType.Float3)
                    return ToArray(v, ToVector3);
                if (elementType == VectorType.Float4)
                    return ToArray(v, ToVector4);
                if (elementType == VectorType.Double2)
                    return ToArray(v, ToDouble2);
                if (elementType == VectorType.Double3)
                    return ToArray(v, ToDouble3);
                if (elementType == VectorType.Double4)
                    return ToArray(v, ToDouble4);
                if (elementType == VectorType.Half2)
                    return ToArray(v, ToHalf2);
                if (elementType == VectorType.Half3)
                    return ToArray(v, ToHalf3);
                if (elementType == VectorType.Half4)
                    return ToArray(v, ToHalf4);
                if (elementType == VectorType.Int2)
                    return ToArray(v, ToInt2);
                if (elementType == VectorType.Int3)
                    return ToArray(v, ToInt3);
                if (elementType == VectorType.Int4)
                    return ToArray(v, ToInt4);
                if (elementType == VectorType.UInt4)
                    return ToArray(v, ToUInt4);
                return default;
            }

            static T[] ToArray<T>(List<Expression> v)
            {
                var a = new T[v.Count];
                for (int i = 0; i < a.Length; i++)
                    a[i] = ToScalar<T>(v[i]);
                return a;
            }

            static T[] ToArray<T>(List<Expression> v, Func<Expression, T> factory)
            {
                var a = new T[v.Count];
                for (int i = 0; i < a.Length; i++)
                    a[i] = factory(v[i]);
                return a;
            }

            public static object ToColor(Expression e, int dimension)
            {
                switch (dimension)
                {
                    case 3:
                        return new Color3(ToVector(e, (float[] args) => new Vector3(args)));
                    case 4:
                        return new Color4(ToVector(e, (float[] args) => new Vector4(args)));
                }
                return null;
            }

            public static object ToFloatVector(Expression e, int dimension)
            {
                switch (dimension)
                {
                    case 2:
                        return ToVector2(e);
                    case 3:
                        return ToVector3(e);
                    case 4:
                        return ToVector4(e);
                }
                return null;
            }

            public static object ToDoubleVector(Expression e, int dimension)
            {
                switch (dimension)
                {
                    case 2:
                        return ToDouble2(e);
                    case 3:
                        return ToDouble3(e);
                    case 4:
                        return ToDouble4(e);
                }
                return null;
            }

            public static object ToHalfVector(Expression e, int dimension)
            {
                switch (dimension)
                {
                    case 2:
                        return ToHalf2(e);
                    case 3:
                        return ToHalf3(e);
                    case 4:
                        return ToHalf4(e);
                }
                return null;
            }

            public static object ToIntVector(Expression e, int dimension)
            {
                switch (dimension)
                {
                    case 2:
                        return ToInt2(e);
                    case 3:
                        return ToInt3(e);
                    case 4:
                        return ToInt4(e);
                }
                return null;
            }

            public static object ToUIntVector(Expression e, int dimension)
            {
                switch (dimension)
                {
                    case 4:
                        return ToUInt4(e);
                }
                return null;
            }

            static Vector2 ToVector2(Expression e) => ToVector(e, (float[] args) => new Vector2(args));

            static Vector3 ToVector3(Expression e) => ToVector(e, (float[] args) => new Vector3(args));

            static Vector4 ToVector4(Expression e) => ToVector(e, (float[] args) => new Vector4(args));

            static Double2 ToDouble2(Expression e) => ToVector(e, (double[] args) => new Double2(args));

            static Double3 ToDouble3(Expression e) => ToVector(e, (double[] args) => new Double3(args));

            static Double4 ToDouble4(Expression e) => ToVector(e, (double[] args) => new Double4(args));

            static Half2 ToHalf2(Expression e) => ToVector(e, (Half[] args) => new Half2(args));

            static Half3 ToHalf3(Expression e) => ToVector(e, (Half[] args) => new Half3(args));

            static Half4 ToHalf4(Expression e) => ToVector(e, (Half[] args) => new Half4(args));

            static Int2 ToInt2(Expression e) => ToVector(e, (int[] args) => new Int2(args));

            static Int3 ToInt3(Expression e) => ToVector(e, (int[] args) => new Int3(args));

            static Int4 ToInt4(Expression e) => ToVector(e, (int[] args) => new Int4(args));

            static UInt4 ToUInt4(Expression e) => ToVector(e, (uint[] args) => new UInt4(args));

            public static TVector ToVector<TVector, TComponent>(Expression e, Func<TComponent[], TVector> factory)
                where TVector : unmanaged
                where TComponent : unmanaged
            {
                if (e is LiteralExpression l)
                    return ToVector(new List<Expression>(1) { e }, factory);
                else if (e is MethodInvocationExpression m)
                    return ToVector(m.Arguments, factory);
                else if (e is ArrayInitializerExpression a)
                    return ToVector(a.Items, factory);
                return default;
            }

            static unsafe TVector ToVector<TVector, TComponent>(List<Expression> args, Func<TComponent[], TVector> factory)
                where TVector : unmanaged
                where TComponent : unmanaged
            {
                var dimension = sizeof(TVector) / sizeof(TComponent);
                if (args.Count == 1)
                    return factory(Enumerable.Repeat(ToScalar<TComponent>(args[0]), dimension).ToArray());
                else if (args.Count == dimension)
                    return factory(ToArray<TComponent>(args));
                return default;
            }

            public static T ToScalar<T>(Expression e)
            {
                if (e is LiteralExpression l)
                    return ToScalar<T>(l.Literal);
                else
                    return default;
            }

            static T ToScalar<T>(Literal l)
            {
                if (l.Value is T t)
                    return t;

                try
                {
                    return (T)Convert.ChangeType(l.Value, typeof(T));
                }
                catch
                {
                    return default;
                }
            }
        }
    }
}
