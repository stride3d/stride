// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Xenko.Core.Shaders.Analysis;
using Xenko.Core.Shaders.Analysis.Hlsl;
using Xenko.Core.Shaders.Ast;
using Xenko.Core.Shaders.Ast.Glsl;
using Xenko.Core.Shaders.Ast.Hlsl;
using Xenko.Core.Shaders.Parser;
using Xenko.Core.Shaders.Utility;
using Xenko.Core.Shaders.Visitor;
using Xenko.Core.Shaders.Writer.Hlsl;
using LayoutQualifier = Xenko.Core.Shaders.Ast.Glsl.LayoutQualifier;
using ParameterQualifier = Xenko.Core.Shaders.Ast.ParameterQualifier;
using StorageQualifier = Xenko.Core.Shaders.Ast.StorageQualifier;

namespace Xenko.Core.Shaders.Convertor
{
    /// <summary>
    /// HLSL to GLSL conversion requires several steps:
    /// - Replace input/output structure access by varying variables.
    /// - Replace common types such as float4 =&gt; vec4.
    /// - Change main signature.
    /// - Convert return statements into GLSL out assignments.
    /// </summary>
    public class HlslToGlslConvertor : ShaderRewriter
    {
        // Each sampler+texture pair will map to a GLSL sampler.
        // KeyValuePair<Variable-SamplerType, Variable-TextureType> => Variable
        #region Constants and Fields

        private const string TagLayoutKey = "LAYOUT";
        private const string VertexIOInterfaceName = "_VertexIO_";

        private const string gl_FrontFacing = "gl_FrontFacing";

        private static readonly List<string> SemanticModifiers = new List<string> { "_centroid", "_pp", "_sat" };

        private readonly bool[] allocatedRegistersForSamplers = new bool[16];

        private readonly bool[] allocatedRegistersForUniforms = new bool[256];

        private readonly Dictionary<string, string> builtinInputs;

        private readonly Dictionary<string, string> builtinOutputs;

        private readonly Dictionary<string, TypeBase> builtinGlslTypes;

        private readonly List<Variable> declarationListToRemove = new List<Variable>();

        private readonly GlslShaderPlatform shaderPlatform;

        private readonly int shaderVersion;

        private readonly string entryPointName;

        private readonly Dictionary<string, string> functionMapping;

        private readonly Dictionary<Variable, Variable> inputAssignment = new Dictionary<Variable, Variable>(new ReferenceEqualityComparer<Variable>());

        private readonly List<Variable> listOfMultidimensionArrayVariable = new List<Variable>();

        private readonly Dictionary<MethodInvocationExpression, bool> methodInvocationHandled = new Dictionary<MethodInvocationExpression, bool>(new ReferenceEqualityComparer<MethodInvocationExpression>());

        private readonly PipelineStage pipelineStage;

        private readonly ShaderModel shaderModel;

        private readonly Dictionary<SamplerTextureKey, Variable> samplerMapping = new Dictionary<SamplerTextureKey, Variable>();

        private MethodDefinition entryPoint;

        private string geometryLayoutInput;

        private Parameter geometryInputParameter;

        private string geometryLayoutOutput;

        private List<Variable> inputs = new List<Variable>();

        private bool isAssignmentTarget;

        private List<Variable> outputs = new List<Variable>();

        private ParsingResult parserResult;

        private Shader shader;

        private GlobalUniformVisitor globalUniformVisitor;

        private bool needCustomFragData = true;

        private int breakIndex = 0;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HlslToGlslConvertor" /> class.
        /// </summary>
        /// <param name="entryPointName">Name of the entry point.</param>
        /// <param name="pipelineStage">The pipeline stage.</param>
        /// <param name="shaderModel">The shader model.</param>
        /// <param name="useBuiltinSemantic">if set to <c>true</c> [use builtin semantic].</param>
        public HlslToGlslConvertor(GlslShaderPlatform shaderPlatform, int shaderVersion, string entryPointName, PipelineStage pipelineStage, ShaderModel shaderModel, bool useBuiltinSemantic = true)
            : base(true, true)
        {
            bool isOpenGLES2 = shaderPlatform == GlslShaderPlatform.OpenGLES && shaderVersion < 300;
            bool isVulkan = shaderPlatform == GlslShaderPlatform.Vulkan;

            this.shaderPlatform = shaderPlatform;
            this.shaderVersion = shaderVersion;
            this.entryPointName = entryPointName;
            this.pipelineStage = pipelineStage;
            this.shaderModel = shaderModel;
            this.VariableLayouts = new Dictionary<string, VariableLayoutRule>();
            this.ConstantBufferLayouts = new Dictionary<string, ConstantBufferLayoutRule>();
            this.MapRules = new Dictionary<string, MapRule>();
            this.KeepConstantBuffer = !isOpenGLES2;
            this.TextureFunctionsCompatibilityProfile = isOpenGLES2;
            this.KeepNonUniformArrayInitializers = shaderPlatform != GlslShaderPlatform.OpenGLES;
            this.ViewFrustumRemap = !isVulkan;
            this.KeepSamplers = isVulkan;
            this.UseLocationLayout = isVulkan;

            if (useBuiltinSemantic)
            {
                builtinInputs = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
                builtinOutputs = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

                // Don't use gl_FragData except on ES2
                if (shaderPlatform == GlslShaderPlatform.OpenGLES && shaderVersion < 300)
                    needCustomFragData = false;

                // Register defaults Semantics with ShaderModel
                switch (pipelineStage)
                {
                    case PipelineStage.Vertex:
                        builtinInputs.Add("SV_VertexID", "gl_VertexID");
                        builtinInputs.Add("SV_InstanceID", "gl_InstanceID");
                        if (shaderModel < ShaderModel.Model40)
                        {
                            builtinOutputs.Add("POSITION", "gl_Position");
                            builtinOutputs.Add("PSIZE", "gl_PointSize");
                        }
                        else
                        {
                            builtinOutputs.Add("SV_Position", "gl_Position");
                            builtinOutputs.Add("SV_ClipDistance", "gl_ClipDistance[]");
                        }
                        break;
                    case PipelineStage.Geometry:
                        if (shaderModel < ShaderModel.Model40)
                        {
                            builtinInputs.Add("PSIZE", "gl_PointSize");
                            builtinOutputs.Add("PSIZE", "gl_PointSize");
                        }
                        else
                        {
                            builtinInputs.Add("SV_POSITION", "gl_Position");
                            builtinInputs.Add("SV_ClipDistance", "gl_ClipDistance[]");
                            builtinInputs.Add("SV_PrimitiveID", "gl_PrimitiveIDIn");
                            builtinOutputs.Add("SV_POSITION", "gl_Position");
                            builtinOutputs.Add("SV_ClipDistance", "gl_ClipDistance[]");
                            builtinOutputs.Add("SV_RenderTargetArrayIndex", "gl_Layer");
                        }
                        break;
                    case PipelineStage.Pixel:
                        if (shaderModel < ShaderModel.Model40)
                        {
                            builtinInputs.Add("VPOS", "gl_FragCoord");
                            builtinInputs.Add("VFACE", gl_FrontFacing);
                            builtinInputs.Add("POSITION", "gl_FragCoord");
                            builtinOutputs.Add("DEPTH", "gl_FragDepth");
                            builtinOutputs.Add("COLOR", "gl_FragData[]");
                        }
                        else
                        {
                            builtinInputs.Add("SV_Position", "gl_FragCoord");
                            builtinInputs.Add("SV_IsFrontFace", "gl_FrontFacing");
                            builtinInputs.Add("SV_ClipDistance", "gl_ClipDistance[]");
                            builtinOutputs.Add("SV_Depth", "gl_FragDepth");
                            builtinOutputs.Add("SV_Target", "gl_FragData[]");
                        }
                        break;
                }

                builtinGlslTypes = new Dictionary<string, TypeBase>(StringComparer.CurrentCultureIgnoreCase) 
                {
                   { "gl_ClipDistance", ScalarType.Float}, // array
                   { "gl_FragCoord", VectorType.Float4},
                   { "gl_FragDepth", ScalarType.Float}, 
                   { "gl_FragColor", VectorType.Float4}, 
                   { "gl_FragData", VectorType.Float4}, // array
                   { "gl_FrontFacing", ScalarType.Bool}, 
                   { "gl_InstanceID", ScalarType.Int },
                   { "gl_InvocationID", ScalarType.Int},
                   { "gl_Layer", ScalarType.Int},
                   { "gl_NumSamples", ScalarType.Int},
                   { "gl_PatchVerticesIn", ScalarType.Int},
                   { "gl_PointCoord", VectorType.Float2},
                   { "gl_PointSize", ScalarType.Float},
                   { "gl_Position", VectorType.Float4},
                   { "gl_PrimitiveID", ScalarType.Int},
                   { "gl_PrimitiveIDIn", ScalarType.Int},
                   { "gl_SampleID", ScalarType.Int},
                   { "gl_SampleMask", ScalarType.Int}, // array
                   { "gl_SampleMaskIn", ScalarType.Int}, // array
                   { "gl_SamplePosition", VectorType.Float2},
                   { "gl_TessCoord", VectorType.Float3},
                   { "gl_VertexID", ScalarType.Int},
                   { "gl_ViewportIndex", ScalarType.Int},
                };
            }

            functionMapping = new Dictionary<string, string> {
                                                                   { "ddx", "dFdx" }, 
                                                                   { "ddy", "dFdy" }, 
                                                                   { "fmod", "mod" }, 
                                                                   { "frac", "fract" }, 
                                                                   { "lerp", "mix" }, 
                                                                   { "rsqrt", "inversesqrt" }, 
                                                                   { "atan2", "atan" }, 
                                                                   { "saturate", "clamp" }, 
                                                                   //{ "D3DCOLORtoUBYTE4", "ivec4" }, 
                                                               };
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets a value indicating wether Z projection coordinates will be remapped from [0;1] to [-1;1] at end of vertex shader.
        /// </summary>
        public bool ViewFrustumRemap { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating wether Y projection will be inverted at end of vertex shader.
        /// </summary>
        public bool FlipRenderTarget { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is point sprite shader.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is point sprite shader; otherwise, <c>false</c>.
        /// </value>
        public bool IsPointSpriteShader { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [no fix for mul for matrix].
        /// </summary>
        /// <value>
        /// <c>true</c> if [no fix for mul for matrix]; otherwise, <c>false</c>.
        /// </value>
        public bool NoSwapForBinaryMatrixOperation { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether [no implicit layout].
        /// </summary>
        /// <value>
        /// <c>true</c> if [no implicit layout]; otherwise, <c>false</c>.
        /// </value>
        public bool UseBindingLayout { get; set; }

        public bool KeepSamplers { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use builtin semantic].
        /// </summary>
        /// <value>
        /// <c>true</c> if [use builtin semantic]; otherwise, <c>false</c>.
        /// </value>
        public bool UseBuiltinSemantic { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use explicit layout position].
        /// </summary>
        /// <value>
        /// <c>true</c> if [use explicit layout position]; otherwise, <c>false</c>.
        /// </value>
        public bool UseLocationLayout { get; set; }

        public IDictionary<int, string> InputAttributeNames { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether texture name will be [texture] or [texture]_[sampler] for DX10 texture objects conversion.
        /// </summary>
        public bool UseOnlyTextureName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use semantic name].
        /// </summary>
        /// <value>
        /// <c>true</c> if [use semantic name]; otherwise, <c>false</c>.
        /// </value>
        public bool UseSemanticForVariable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use semantic for location].
        /// </summary>
        /// <value>
        /// <c>true</c> if [use semantic for location]; otherwise, <c>false</c>.
        /// </value>
        public bool UseSemanticForLocation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use interface for in out].
        /// </summary>
        /// <value><c>true</c> if [use interface for in out]; otherwise, <c>false</c>.</value>
        public bool UseInterfaceForInOut { get; set; }

        /// <summary>
        /// Gets the map config.
        /// </summary>
        /// <value>
        /// The map config.
        /// </value>
        public Dictionary<string, VariableLayoutRule> VariableLayouts { get; private set; }

        /// <summary>
        /// Gets the constant buffer layouts.
        /// </summary>
        /// <value>
        /// The constant buffer layouts.
        /// </value>
        public Dictionary<string, ConstantBufferLayoutRule> ConstantBufferLayouts { get; private set; }

        /// <summary>
        /// Gets or sets the map rules.
        /// </summary>
        /// <value>
        /// The map rules.
        /// </value>
        public Dictionary<string, MapRule> MapRules { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether to keep array initializers for uniforms.
        /// </summary>
        /// <value>
        /// true if keep uniform array initializers, false if not.
        /// </value>
        public bool KeepUniformArrayInitializers { get; set; }

        /// <summary>
        /// Gets or sets a flag specifying whether compatibility profile is used for texture functions.
        /// As an example, with compatibility on, texture() might become texture2D().
        /// </summary>
        /// <value>
        /// true if texture compatibility profile is enabled, false if not.
        /// </value>
        public bool TextureFunctionsCompatibilityProfile { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool KeepConstantBuffer { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to keep array initializers.
        /// </summary>
        public bool KeepNonUniformArrayInitializers { get; set; }

        public GlslShaderPlatform Platform { get; set; }

        public int PlatformVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to unroll the loops with the [unroll] annotation.
        /// </summary>
        public bool UnrollForLoops { get; set; } = true;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the current function being parsed.
        /// </summary>
        /// <value>
        /// The current function.
        /// </value>
        private MethodDeclaration CurrentFunction { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is in entry point.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is in entry point; otherwise, <c>false</c>.
        /// </value>
        private bool IsInEntryPoint { get { return CurrentFunction != null && CurrentFunction.Name.Text == entryPointName; } }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Prepares the specified shader for glsl to hlsl conversion (before type inference analysis).
        /// </summary>
        /// <param name="shader">
        /// The shader.
        /// </param>
        public static void Prepare(Shader shader)
        {
            // Replace all Half types to float, as there are no equivalent in glsl
            // This will force the type inference analysis to use float instead of half
            SearchVisitor.Run(
                shader, 
                node =>
                    {
                        if (node.Equals(ScalarType.Half))
                            return ScalarType.Float;

                        // Transform half vectors to float vectors
                        var typeBase = node as TypeBase;
                        if (typeBase != null)
                        {
                            var vectorType = typeBase.ResolveType() as VectorType;
                            if (vectorType != null)
                            {
                                var subType = vectorType.Type.ResolveType();
                                if (subType == ScalarType.Half)
                                    typeBase.TypeInference.TargetType = TypeBase.CreateWithBaseType(vectorType, ScalarType.Float);
                            }
                        }

                        return node;
                    });
        }

        /// <summary>
        /// Runs the convertor on the specified parser result.
        /// </summary>
        /// <param name="parserResultIn">The parser result.</param>
        public void Run(ParsingResult parserResultIn)
        {
            parserResult = parserResultIn;
            shader = parserResultIn.Shader;

            // Transform typedef with inline declaration to separate declaration + typedef
            // in order for the strip visitor to work
            SplitTypeDefs();

            // Find entry point
            entryPoint = shader.Declarations.OfType<MethodDefinition>().FirstOrDefault(x => x.Name.Text == entryPointName);

            if (entryPoint == null)
                throw new ArgumentException(string.Format("Could not find entry point named {0}", entryPointName));

            // Transform multiple variable declaration to single
            TransformGlobalMultipleVariableToSingleVariable();

            // Gather all samplers and create new samplers
            // Strips unused code 
            GenerateSamplerMappingAndStrip();

            // Look for global uniforms used as global temp variable
            globalUniformVisitor = new GlobalUniformVisitor(shader);
            globalUniformVisitor.Run(entryPoint);

            var writer = new HlslWriter();
            writer.Visit(shader);
            var text = writer.Text;

            var castVisitor = new CastAnalysis(parserResult);
            castVisitor.Run();

            // This the shader
            Visit(shader);

            // Remove Default parameters for all function
            RemoveDefaultParametersForMethods();

            // Transform all types to glsl types
            TranformToGlslTypes();

            // Rename variable using a glsl keyword
            RenameGlslKeywords();

            // Rebind all renamed variables
            RebindVariableReferenceExpressions();

            // Order first all non-method declarations and then after method declarations
            var declarations = shader.Declarations.Where(declaration => !(declaration is MethodDeclaration)).ToList();
            declarations.AddRange(shader.Declarations.OfType<MethodDeclaration>());
            shader.Declarations = declarations;

            // Add std140 layout
            ApplyStd140Layout();

            // Sort qualifiers in the order GLSL expects them
            ReorderVariableQualifiers();

            if (shaderPlatform == GlslShaderPlatform.OpenGLES && shaderVersion < 300)
                FixupVaryingES2();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Visits the specified variable.
        /// </summary>
        /// <param name="variable">
        /// The variable.
        /// </param>
        public override Node Visit(Variable variable)
        {
            // All variable arrays are processed later to simplify/unify their bounds
            var variableType = variable != null ? variable.Type.ResolveType() : null;
            var arrayType = variableType as ArrayType;

            if (arrayType != null)
            {
                if (!this.listOfMultidimensionArrayVariable.Contains(variable))
                    this.listOfMultidimensionArrayVariable.Add(variable);
            }

            var isInMethod = !shader.Declarations.Contains(variable);

            // Static variable are allowed inside HLSL functions 
            // but only at global scope for glsl
            // TODO check if removing the static modifier is enough or we need to move the variable at the toplevel scope (a bit more harder to implement)
            if (CurrentFunction != null && variable.Qualifiers.Contains(Ast.Hlsl.StorageQualifier.Static))
                variable.Qualifiers.Values.Remove(Ast.Hlsl.StorageQualifier.Static);

            // Because const qualifier in HLSL is way too permissive, we need to remove it for GLSL
            // Remove only const qualifiers inside methods
            if (isInMethod && variable.Qualifiers.Contains(Ast.StorageQualifier.Const))
                variable.Qualifiers.Values.Remove(Ast.StorageQualifier.Const);

            base.Visit(variable);

            // Set the Type of a variable by using the resolve type
            if (variable.Type.TypeInference.Declaration is Typedef)
            {
                var typeDefType = variable.Type.ResolveType();
                if (typeDefType is StructType)
                {
                    variable.Type = new TypeName(typeDefType.Name) { TypeInference = { Declaration = (IDeclaration)typeDefType, TargetType = typeDefType } };
                }
                else
                {
                    variable.Type = typeDefType;
                }
            }

            if (variable.Type is ArrayType)
            {
                if (variable.InitialValue is MethodInvocationExpression && !KeepNonUniformArrayInitializers)
                {
                    if (isInMethod) // inside a method
                    {
                        var arrayInit = variable.InitialValue as MethodInvocationExpression;
                        if (arrayInit.Target is IndexerExpression) // HACK: this checks that it is an initialization. It is a hack because the GLSL grammar was mapped into the hlsl one
                        {
                            // build the statement list
                            var statements = new StatementList();
                            statements.Add(new DeclarationStatement(variable));
                            for (int i = 0; i < arrayInit.Arguments.Count; ++i)
                            {
                                var statement = new ExpressionStatement(new AssignmentExpression(AssignmentOperator.Default, new IndexerExpression(new VariableReferenceExpression(variable.Name), new LiteralExpression(i)), arrayInit.Arguments[i]));
                                statements.Add(statement);
                            }

                            // patch the variable
                            variable.InitialValue = null;

                            return statements;
                        }
                    }
                    else if (!isInMethod && !IsUniformLike(variable)) // non-uniform variable oustide a method
                    {
                        variable.InitialValue = null;
                    }
                }
            }

            return variable;
        }

        /// <summary>
        /// Visits the specified function.
        /// </summary>
        /// <param name="function">The function.</param>
        public override Node Visit(MethodDefinition function)
        {
            // Enter this function
            CurrentFunction = function;


            // Convert HLSL "in out" qualifier to "inout" qualifier
            foreach (var arg in function.Parameters)
            {
                if (arg.Qualifiers.Contains(Ast.ParameterQualifier.Out) && arg.Qualifiers.Contains(Ast.ParameterQualifier.In))
                {
                    arg.Qualifiers.Values.Remove(Ast.ParameterQualifier.Out);
                    arg.Qualifiers.Values.Remove(Ast.ParameterQualifier.In);
                    arg.Qualifiers.Values.Add(Ast.ParameterQualifier.InOut);
                }
            }

            if (function == entryPoint)
                PrepareVisitEntryPoint(function);    // Prepare visit of entry point
            else
                function.Qualifiers.Values.Clear();  // Remove semantics from function indirectly used by entrypoint

            // Convert function return type
            // Visit all subnodes of this function
            base.Visit(function);

            // For entry point restore arguments/declcontext
            if (function == entryPoint)
                PostVisitEntryPoint(function);

            // Remove uniform parameters
            foreach (var modifier in function.Parameters.Select(variable => variable.Qualifiers).Where(modifier => modifier.Contains(Ast.StorageQualifier.Uniform)))
                modifier.Values.Remove(Ast.StorageQualifier.Uniform);

            // For GeometryShader, remove StreamType parameters
            RemoveStreamTypeFromMethodDefinition(function);

            // Clear current function viside
            CurrentFunction = null;

            return function;
        }

        /// <summary>
        /// Prepares the visit of entry point.
        /// </summary>
        /// <param name="function">The entry point function.</param>
        protected void PrepareVisitEntryPoint(MethodDefinition function)
        {
            inputs.Clear();
            outputs.Clear();

            // Make a copy of arguments
            // var savedDeclarationContext = function.Declarations.ToList();
            foreach (var arg in function.Parameters)
            {
                if (arg.Qualifiers.Contains(Ast.ParameterQualifier.Out) || arg.Qualifiers.Contains(Ast.ParameterQualifier.InOut))
                {
                    outputs.Add(arg);
                }
                else
                {
                    inputs.Add(arg);

                    // Process and convert GS input layout type
                    foreach (var qualifier in arg.Qualifiers)
                    {
                        switch ((string)qualifier.Key)
                        {
                            case "triangleadj":
                                geometryLayoutInput = "triangles_adjacency";
                                break;
                            case "triangle":
                                geometryLayoutInput = "triangles";
                                break;
                            case "lineadj":
                                geometryLayoutInput = "lines_adjacency";
                                break;
                            case "line":
                                geometryLayoutInput = "lines";
                                break;
                            case "point":
                                geometryLayoutInput = "points";
                                break;
                        }

                        if (geometryLayoutInput != null)
                        {
                            geometryInputParameter = arg;
                        }
                    }
                }
            }


            // ------------------------------------------------ 
            // Check the type of the output for pixel shaders
            // If glFragData has multiple types, than we need to output a
            // new output type for glFragData.
            // ------------------------------------------------ 

            if (pipelineStage == PipelineStage.Pixel)
            {
                int countDifferentType = 0;

                foreach (var output in outputs)
                {
                    var outputType = output.Type.ResolveType();
                    if (outputType is StructType)
                    {
                        countDifferentType += GetMembers((StructType)outputType).Select(fieldRef => fieldRef.Field).Count(field => this.CheckFragDataOutputType(field.Semantic(), field.Type.ResolveType()));
                    }
                    else
                    {
                        if (CheckFragDataOutputType(output.Semantic(), outputType))
                            countDifferentType++;
                    }
                }

                var returnType = function.ReturnType.ResolveType();
                var returnStructType = returnType as StructType;
                if (returnStructType != null)
                {
                    countDifferentType += GetMembers(returnStructType).Select(fieldRef => fieldRef.Field).Count(field => this.CheckFragDataOutputType(field.Semantic(), field.Type.ResolveType()));
                }
                else if (function.Semantic() != null)
                {
                    if (CheckFragDataOutputType(function.Semantic(), returnType))
                        countDifferentType++;
                }

                needCustomFragData |= countDifferentType > 0;
            }
        }

        private bool CheckFragDataOutputType(Semantic inputSemantic, TypeBase type)
        {
            if (inputSemantic == null)
                return false;

            TypeBase newFieldType;
            int semanticIndex = 0;
            var semantic = ResolveSemantic(inputSemantic, type, false, "tmptmp", out newFieldType, out semanticIndex, inputSemantic.Span);
            if (CultureInfo.InvariantCulture.CompareInfo.IsPrefix(semantic.Name.Text, "gl_fragdata", CompareOptions.IgnoreCase) && (newFieldType != type || type is ArrayType))
            {
                return true;
                //// Generate only fragdata when whe basetype is completly changing
                //// TODO: improve handling gl_fragdata: Current code is not robust.
                //var baseElementType = type is ArrayType ? ((ArrayType)type).Type.ResolveType() : type;
                //var baseNewElementType = newFieldType is ArrayType ? ((ArrayType)newFieldType).Type.ResolveType() : newFieldType;

                //// Get type of the element
                //baseElementType = TypeBase.GetBaseType(baseElementType);
                //baseNewElementType = TypeBase.GetBaseType(baseNewElementType);

                //return (baseElementType != baseNewElementType);
            }
            return false;
        }

        /// <summary>
        /// Visits the entry point.
        /// </summary>
        /// <param name="function">The entry point function.</param>
        protected void PostVisitEntryPoint(MethodDefinition function)
        {
            int inputSemanticLocation = 0;
            int outputSemanticLocation = 0;

            // For structure in input, make a local copy
            foreach (var variable in this.inputs)
            {
                var structType = variable.Type.ResolveType() as StructType;
                if (structType != null)
                {
                    bool semanticFound = false;
                    foreach (var fieldRef in GetMembers(structType))
                    {
                        var field = fieldRef.Field;

                        var semantic = field.Semantic();
                        if (semantic != null)
                        {
                            var fieldType = field.Type.ResolveType();
                            var variableFromSemantic = this.BindLocation(semantic, fieldType, true, fieldRef.FieldNamePath, ref inputSemanticLocation, variable.Span);

                            // Link to the original variable
                            // var variableSemanticRef = new VariableReferenceExpression(variableFromSemantic.Name) { TypeInference = { Declaration = variableFromSemantic } };

                            function.Body.Insert(
                                0,
                                new ExpressionStatement(
                                    new AssignmentExpression(
                                        AssignmentOperator.Default, fieldRef.GetMemberReference(new VariableReferenceExpression(variable.Name)), 
                                        this.CastSemanticToReferenceType(variableFromSemantic.Name, fieldType, variableFromSemantic))) { Span = variable.Span });
                            semanticFound = true;
                        }
                    }

                    if (semanticFound)
                    {
                        // No modifiers for structure inlined in the code
                        variable.Qualifiers = Qualifier.None;
                        function.Body.Statements.Insert(0, new DeclarationStatement(variable) { Span = variable.Span });
                    }
                }
                else
                {
                    var semantic = variable.Semantic();
                    if (semantic != null)
                        this.BindLocation(semantic, variable.Type.ResolveType(), true, variable.Name, ref inputSemanticLocation, variable.Span);
                }
            }

            // For structure in output, declare a local variable
            foreach (var variable in this.outputs)
            {
                var structType = variable.Type.ResolveType() as StructType;
                if (structType != null)
                {
                    // No modifiers for structure inlined in the code
                    variable.Qualifiers = Qualifier.None;
                    function.Body.Statements.Insert(0, new DeclarationStatement(variable));

                    var statementList = new StatementList();

                    var lastStatement = function.Body.Statements.LastOrDefault();

                    ReturnStruct(structType, new VariableReferenceExpression(variable.Name) { Span = lastStatement != null ? lastStatement.Span : new SourceSpan() }, statementList);

                    function.Body.Statements.Add(statementList);
                }
            }

            // Process return type
            var returnType = function.ReturnType.ResolveType();
            var returnStructType = returnType as StructType;
            if (returnStructType != null)
            {
                foreach (var fieldRef in GetMembers(returnStructType))
                {
                    var field = fieldRef.Field;
                    BindLocation(field.Semantic(), field.Type.ResolveType(), false, field.Name, ref outputSemanticLocation, function.ReturnType.Span);
                }
            }
            else if (function.Semantic() != null)
            {
                var semantic = function.Semantic();
                BindLocation(semantic, returnType, false, null, ref outputSemanticLocation, semantic.Span);
            }

            // Set Location for each output
            if (pipelineStage == PipelineStage.Geometry)
            {
                foreach (var variable in shader.Declarations.OfType<Variable>())
                {
                    if (variable.Qualifiers.Contains(Ast.ParameterQualifier.Out))
                    {
                        BindLocation(variable.Semantic(), variable.Type.ResolveType(), false, variable.Name, ref outputSemanticLocation, variable.Span);
                    }
                }
            }
            else
            {
                foreach (var outputVariable in outputs)
                {
                    BindLocation(outputVariable.Semantic(), outputVariable.Type.ResolveType(), false, outputVariable.Name, ref outputSemanticLocation, outputVariable.Span);
                }
            }

            // Process parameters
            for (int i = 0; i < function.Parameters.Count; ++i)
            {
                var variable = function.Parameters[i];
                var modifier = variable.Qualifiers;
                if (modifier.Contains(Ast.StorageQualifier.Uniform))
                {
                    function.Parameters.RemoveAt(i--);
                    ScopeStack.Peek().RemoveDeclaration(variable);

                    if (!shader.Declarations.Contains(variable))
                    {
                        // Generate name by appending _uniform and _1, _2 etc... if already existing
                        var variableNameBase = variable.Name;
                        variable.Name.Text += "_uniform";
                        int variableNameIndex = 1;
                        while (FindDeclaration(variable.Name) != null)
                            variable.Name.Text = variableNameBase + "_" + variableNameIndex++;

                        AddGlobalDeclaration(variable);
                    }
                }
            }

            // Fix variable references that were transform to local variable
            // This is not ideal, as we should instead perform a pre-pass to detect these variables
            // and patch them after the pre-pass
            // The problem is that ConvertReferenceToSemantics is working only if a variable is modified
            // first and then used, but if a variable is used, and then modified, ConvertReferenceToSemantics
            // will not modify the previous 'local' variable.
            // This code is a workaround. A refactoring of the whole process would be more adequate but requires 
            // more changes to the overall logic that we can't really afford now.
            SearchVisitor.Run(
                function,
                node =>
                {
                    var varRefExpr = node as VariableReferenceExpression;
                    if (varRefExpr != null)
                    {
                        var variable = FindDeclaration(varRefExpr.Name) as Variable;
                        if (variable != null)
                        {
                            Variable newVariable;
                            inputAssignment.TryGetValue(variable, out newVariable);

                            if (newVariable != null)
                            {
                                return new VariableReferenceExpression(newVariable);
                            }
                        }
                    }
                    return node;
                });
        }


        /// <summary>
        /// Visits the specified cast expression.
        /// </summary>
        /// <param name="castExpression">The cast expression.</param>
        /// <returns>A transformed cast expression</returns>
        public override Node Visit(CastExpression castExpression)
        {
            base.Visit(castExpression);

            var targetType = castExpression.Target.TypeInference.TargetType;

            // If there is a cast from an integer 0 to a struct, than remove the cast for GLSL, as it is not supported
            if (targetType is StructType && castExpression.From is LiteralExpression)
            {
                var literalExpression = (LiteralExpression)castExpression.From;
                if (literalExpression.Value != null)
                {
                    var toStringValue = literalExpression.Value.ToString().Trim();
                    if (toStringValue == "0")
                        return null;
                }
            }

            // Remove cast for literal integer/float by generating a proper literal
            if (targetType == ScalarType.Float && castExpression.From is LiteralExpression)
            {
                var literalExpression = (LiteralExpression)castExpression.From;
                literalExpression.Value = Convert.ChangeType(literalExpression.Value, typeof(float));
                return literalExpression;
            }

            var castByMethod = new MethodInvocationExpression(new TypeReferenceExpression(castExpression.Target), castExpression.From);

            targetType = castExpression.TypeInference.TargetType;
            if (targetType != null)
                castByMethod.TypeInference.TargetType = targetType;

            CheckCastMethod(castByMethod);

            return castByMethod;
        }

        /// <summary>
        /// Visits the specified statement.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <returns>A transformed statement</returns>
        public override Node Visit(ExpressionStatement expressionStatement)
        {
            Statement newStatement = null;

            var methodInvocationExpression = expressionStatement.Expression as MethodInvocationExpression;
            if (methodInvocationExpression != null)
            {
                var methodVar = methodInvocationExpression.Target as VariableReferenceExpression;
                if (methodVar != null)
                {
                    newStatement = VisitStatementAsFunctionInvocation(expressionStatement, methodInvocationExpression, methodVar);
                }
                else
                {
                    var memberReferenceExpression = methodInvocationExpression.Target as MemberReferenceExpression;
                    if (memberReferenceExpression != null)
                    {
                        newStatement = VisitStatementAsMemberInvocation(expressionStatement, methodInvocationExpression, memberReferenceExpression);
                    }
                }
            }
            else
            {
                var assignExpression = expressionStatement.Expression as AssignmentExpression;
                if (assignExpression != null)
                {
                    newStatement = VisitStatementAsAssignExpression(expressionStatement, assignExpression);
                }
            }

            return newStatement ?? base.Visit(expressionStatement);
        }

        /// <summary>
        /// Visits a statement that is a function invocation.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <param name="methodInvocationExpression">The function invocation expression.</param>
        /// <param name="methodVar">The name of the function.</param>
        /// <returns></returns>
        protected Statement VisitStatementAsFunctionInvocation(ExpressionStatement statement, MethodInvocationExpression methodInvocationExpression, VariableReferenceExpression methodVar)
        {
            var methodName = methodVar.Name;

            switch (methodName)
            {
                case "clip":
                    if (methodInvocationExpression.Arguments.Count == 1)
                    {
                        Expression conditionExpression;

                        if (!methodInvocationHandled.ContainsKey(methodInvocationExpression))
                            methodInvocationHandled.Add(methodInvocationExpression, true);

                        base.Visit(statement);

                        var clipArgType = methodInvocationExpression.Arguments[0].TypeInference.TargetType;

                        bool isSingleValue = clipArgType is ScalarType; // || clipArgType.Generics == null);
                        if (isSingleValue)
                            conditionExpression = new BinaryExpression(
                                BinaryOperator.Less, ConvertToSafeExpressionForBinary(methodInvocationExpression.Arguments[0]), new LiteralExpression(ScalarType.IsInteger(clipArgType) ? (object)0 : 0.0f));
                        else
                        {
                            var castToZero = new MethodInvocationExpression(new TypeReferenceExpression(clipArgType), new LiteralExpression(0));
                            var lessThan = new MethodInvocationExpression("lessThan", methodInvocationExpression.Arguments[0], castToZero);
                            var methodAll = new MethodInvocationExpression("all", lessThan);
                            conditionExpression = methodAll;
                        }

                        return new IfStatement { Condition = conditionExpression, Then = new ExpressionStatement(new KeywordExpression("discard")) };
                    }

                    break;
                case "sincos":

                    if (methodInvocationExpression.Arguments.Count == 3)
                    {
                        if (!methodInvocationHandled.ContainsKey(methodInvocationExpression))
                            methodInvocationHandled.Add(methodInvocationExpression, true);
                        base.Visit(statement);

                        var sinAssign =
                            new ExpressionStatement(
                                new AssignmentExpression(
                                    AssignmentOperator.Default, methodInvocationExpression.Arguments[1], new MethodInvocationExpression("sin", methodInvocationExpression.Arguments[0])));

                        var cosAssign =
                            new ExpressionStatement(
                                new AssignmentExpression(
                                    AssignmentOperator.Default, methodInvocationExpression.Arguments[2], new MethodInvocationExpression("cos", methodInvocationExpression.Arguments[0])));

                        return new StatementList(sinAssign, cosAssign);
                    }

                    break;
            }

            return null;
        }

        /// <summary>
        /// Visits a statement that is a member invocation.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <param name="methodInvocationExpression">The method invocation expression.</param>
        /// <param name="memberReferenceExpression">The member reference expression.</param>
        /// <returns>A new statement if handled, null otherwise</returns>
        protected Statement VisitStatementAsMemberInvocation(Statement statement, MethodInvocationExpression methodInvocationExpression, MemberReferenceExpression memberReferenceExpression)
        {
            if (memberReferenceExpression.Member == "GetDimensions")
            {
                var textureRef = memberReferenceExpression.Target as VariableReferenceExpression;
                var variableTexture = this.FindGlobalVariableFromExpression(textureRef);

                if (variableTexture == null)
                {
                    parserResult.Error("Unable to find target variable for expression [{0}]", methodInvocationExpression.Span, methodInvocationExpression);
                    return null;
                }

                var glslSampler = GetGLSampler(null, variableTexture, false);

                if (glslSampler == null)
                {
                    parserResult.Error("Unable to find matching sampler for GetDimensions() for expression [{0}]", methodInvocationExpression.Span, methodInvocationExpression);
                    return null;
                }

                // Convert texture.GetDimensions(x, y) into
                // {
                // var texSize = textureSize(texture);
                // x = texSize.x;
                // y = texSize.y;
                // }
                var resultBlock = new BlockStatement();

                var textureSizeCall = new MethodInvocationExpression(new VariableReferenceExpression("textureSize"));
                textureSizeCall.Arguments.Add(glslSampler);
                textureSizeCall.Arguments.Add(new LiteralExpression(0));

                // TODO: Support all the versions of GetDimensions based on texture type and parameter count
                // GetDimensions signature can be (uint mipLevel, uint width, uint height) or (uint width, uint height)
                var startArgIndex = 0;
                if (methodInvocationExpression.Arguments.Count > 2)
                    startArgIndex = 1;

                // TODO: Support for sampler size other than 2D
                var textureSizeVariable = new Variable(VectorType.Int2, "tempTextureSize", textureSizeCall);
                resultBlock.Statements.Add(new DeclarationStatement(textureSizeVariable));
                resultBlock.Statements.Add(
                    new ExpressionStatement(
                        new AssignmentExpression(
                            AssignmentOperator.Default,
                            methodInvocationExpression.Arguments[startArgIndex],
                            new MemberReferenceExpression(new VariableReferenceExpression(textureSizeVariable.Name), "x"))));
                resultBlock.Statements.Add(
                    new ExpressionStatement(
                        new AssignmentExpression(
                            AssignmentOperator.Default,
                            methodInvocationExpression.Arguments[startArgIndex + 1],
                            new MemberReferenceExpression(new VariableReferenceExpression(textureSizeVariable.Name), "y"))));

                return resultBlock;
            }

            return null;
        }

        protected Statement VisitStatementAsAssignExpression(Statement statement, AssignmentExpression assignmentExpression)
        {
            var indexerExpression = assignmentExpression.Target as IndexerExpression;
            if (NoSwapForBinaryMatrixOperation && indexerExpression != null)
            {
                // Collect all indices in the order of the declaration
                var targetIterator = indexerExpression.Target;
                var indices = new List<Expression> { indexerExpression.Index };
                while (targetIterator is IndexerExpression)
                {
                    indices.Add(((IndexerExpression)targetIterator).Index);
                    targetIterator = ((IndexerExpression)targetIterator).Target;
                }

                // Check that index apply to an array variable
                var variableReferenceExpression = targetIterator as VariableReferenceExpression;
                if (variableReferenceExpression != null)
                {
                    var variable = FindDeclaration(variableReferenceExpression.Name) as Variable;

                    // If array is a multidimension array
                    var variableType = variable != null ? variable.Type.ResolveType() : null;
                    var matrixType = variableType as MatrixType;

                    if (matrixType != null)
                    {
                        if (indices.Count == 2)
                        {
                            IndexerExpression nextExpression = null;

                            // float4x3[0][1] -> mat4x3[1][0]
                            for (int i = 0; i < indices.Count; i++)
                            {
                                nextExpression = nextExpression == null ? new IndexerExpression(variableReferenceExpression, indices[i]) : new IndexerExpression(nextExpression, indices[i]);
                            }

                            assignmentExpression.Target = nextExpression;
                        }
                        else
                        {
                            // matrixType.ColumnCount
                            var matrixElementType = matrixType.Type.ResolveType() as ScalarType;
                            var matrixRowType = new VectorType(matrixElementType, matrixType.ColumnCount);

                            // Convert mat3x4[0] = ...; into
                            // {
                            // var local = ...;
                            // mat3x4[0][0] = local.x;
                            // mat3x4[0][1] = local.y;
                            // mat3x4[0][2] = local.z;
                            // mat3x4[0][3] = local.w;
                            // }
                            var resultBlock = new BlockStatement();

                            // need to call the visitor on the value here since Visit(AssignmentExpression ) won't be called afterwards (non null function's return).
                            assignmentExpression.Value = (Expression)VisitDynamic(assignmentExpression.Value);

                            var localResult = new Variable(matrixRowType, "_localmat_", assignmentExpression.Value);
                            resultBlock.Statements.Add(new DeclarationStatement(localResult));

                            for (int i = 0; i < matrixType.ColumnCount; i++)
                            {
                                var targetExpression = new IndexerExpression(new IndexerExpression(indexerExpression.Target, new LiteralExpression(i)), indexerExpression.Index);
                                var valueExpression = new IndexerExpression(new VariableReferenceExpression("_localmat_"), new LiteralExpression(i));
                                var assignRowCol = new AssignmentExpression(AssignmentOperator.Default, targetExpression, valueExpression);
                                resultBlock.Statements.Add(new ExpressionStatement(assignRowCol));
                            }
                            return resultBlock;
                        }
                    }
                }
            }

            return null;
        }



        /// <summary>
        /// Visits the specified method invocation expression.
        /// </summary>
        /// <param name="methodInvocationExpression">The method invocation expression.</param>
        /// <returns>
        /// A transformed method invocation expression.
        /// </returns>
        public override Node Visit(MethodInvocationExpression methodInvocationExpression)
        {
            base.Visit(methodInvocationExpression);

            // If method is already handled
            if (methodInvocationHandled.ContainsKey(methodInvocationExpression))
                return methodInvocationExpression;

            MethodDeclaration methodDeclaration = null;

            // Transform various method calls to match OpenGL specs.
            var methodVar = methodInvocationExpression.Target as VariableReferenceExpression;
            if (methodVar != null)
            {
                var methodName = methodVar.Name;
                methodDeclaration = methodVar.TypeInference.Declaration as MethodDeclaration;

                // When a method is calling a typedef, use the type of the type def as a TypeReference instead of a VariableReference
                if (methodInvocationExpression.TypeInference.Declaration is Typedef)
                {
                    methodInvocationExpression.Target = new TypeReferenceExpression(methodInvocationExpression.TypeInference.TargetType);
                    return methodInvocationExpression;
                }

                if (methodName == "mul")
                {
                    //// Swap all binary expressions in order for matrix multiplications to be compatible with matrix layout
                    var leftParameter = ConvertToSafeExpressionForBinary(methodInvocationExpression.Arguments[NoSwapForBinaryMatrixOperation ? 0 : 1]);
                    var rightParameter = ConvertToSafeExpressionForBinary(methodInvocationExpression.Arguments[NoSwapForBinaryMatrixOperation ? 1 : 0]);
                    return new ParenthesizedExpression(new BinaryExpression(BinaryOperator.Multiply, leftParameter, rightParameter));
                } 
                
                if (methodName == "lit")
                {
                    // http://msdn.microsoft.com/en-us/library/bb509619%28v=vs.85%29.aspx
                    // ret lit(n_dot_l, n_dot_h, m) {
                    // ambient = 1.
                    // diffuse = ((n • l) < 0) ? 0 : n • l.
                    // specular = ((n • l) < 0) || ((n • h) < 0) ? 0 : ((n • h) ^ m).
                    // return float4((ambient, diffuse, specular, 1);
                    // }
                    var methodLit = new MethodInvocationExpression(new TypeReferenceExpression(VectorType.Float4));
                    methodLit.Arguments.Add(new LiteralExpression(1.0f));

                    var diffuseArg = new ConditionalExpression(
                        new BinaryExpression(BinaryOperator.Less, methodInvocationExpression.Arguments[0], new LiteralExpression(0.0f)), 
                        new LiteralExpression(0.0f), 
                        methodInvocationExpression.Arguments[0]);

                    methodLit.Arguments.Add(diffuseArg);

                    var specularArg =
                        new ConditionalExpression(
                            new BinaryExpression(
                                BinaryOperator.LogicalOr, 
                                new BinaryExpression(BinaryOperator.Less, methodInvocationExpression.Arguments[0], new LiteralExpression(0.0f)), 
                                new BinaryExpression(BinaryOperator.Less, methodInvocationExpression.Arguments[1], new LiteralExpression(0.0f))), 
                            new LiteralExpression(0.0f), 
                            new MethodInvocationExpression("pow", methodInvocationExpression.Arguments[1], methodInvocationExpression.Arguments[2]));

                    methodLit.Arguments.Add(specularArg);
                    methodLit.Arguments.Add(new LiteralExpression(1.0f));

                    //// Swap all binary expressions in order for matrix multiplications to be compatible with matrix layout
                    return methodLit;
                }

                if (methodName == "isfinite")
                {
                    methodVar.Name = "isinf";
                    var result = new MethodInvocationExpression("not", methodInvocationExpression);
                    return result;
                }

                if (methodName == "log10")
                {
                    methodVar.Name = "log";
                    var log10 = new MethodInvocationExpression("log", new LiteralExpression(10.0f));
                    return new BinaryExpression(BinaryOperator.Divide, methodInvocationExpression, log10);
                }

                if (methodName == "saturate")
                {
                    methodVar.Name = "saturate";
                    methodInvocationExpression.Arguments.Add(new LiteralExpression(0.0f));
                    methodInvocationExpression.Arguments.Add(new LiteralExpression(1.0f));
                }

                // Transform all(x) into all(x != 0) because OpenGL expects only boolean
                if (methodName == "all" || methodName == "any")
                {
                    var argType = methodInvocationExpression.Arguments[0].TypeInference.TargetType;
                    if (argType == null || TypeBase.GetBaseType(argType) != ScalarType.Bool)
                    {
                        var castToZero = new MethodInvocationExpression(new TypeReferenceExpression(argType), new LiteralExpression(0));
                        var notEqual = new MethodInvocationExpression("notEqual", methodInvocationExpression.Arguments[0], castToZero);
                        methodInvocationExpression.Arguments[0] = notEqual;
                    }
                }

                if (string.Compare(methodName, "D3DCOLORtoUBYTE4", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return new MethodInvocationExpression(new TypeReferenceExpression(VectorType.Int4), methodInvocationExpression.Arguments[0]) { TypeInference = { TargetType = VectorType.Int4 }};
                }

                string methodNameGl;
                if (functionMapping.TryGetValue(methodName, out methodNameGl))
                    methodVar.Name = methodNameGl;
            }

            // Convert member expression
            var memberReferenceExpression = methodInvocationExpression.Target as MemberReferenceExpression;
            if (memberReferenceExpression != null)
            {
                var targetVariable = FindGlobalVariableFromExpression(memberReferenceExpression.Target);
                if (targetVariable == null)
                {
                    parserResult.Error("Unable to find target variable for expression [{0}]", methodInvocationExpression.Span, methodInvocationExpression);
                    return methodInvocationExpression;
                }
                var targetVariableType = targetVariable.Type.ResolveType();
                methodDeclaration = memberReferenceExpression.TypeInference.Declaration as MethodDeclaration;

                switch (memberReferenceExpression.Member)
                {
                        // Geometry shader
                    case "RestartStrip":
                        methodInvocationExpression.Target = new VariableReferenceExpression("EndPrimitive");
                        break;

                        // Texture object
                    case "GetDimensions":
                        // We should not be here
                        parserResult.Error("GetDimensions should have been already preprocessed for expression [{0}]", methodInvocationExpression.Span, methodInvocationExpression);
                        break;
                    case "Load":
                    case "Sample":
                    case "SampleBias":
                    case "SampleGrad":
                    case "SampleLevel":
                    case "SampleCmp":
                    case "SampleCmpLevelZero":
                        {
                            string methodName = "texture";

                            bool isLoad = memberReferenceExpression.Member == "Load";
                            int baseParameterCount = isLoad ? 1 : 2;

                            Variable sampler = null;

                            // texture.Load() doesn't require a sampler
                            if (!isLoad)
                            {
                                sampler = this.FindGlobalVariableFromExpression(methodInvocationExpression.Arguments[0]);
                            }
                            var glslSampler = GetGLSampler(sampler, targetVariable, true);

                            if (TextureFunctionsCompatibilityProfile)
                            {
                                if (targetVariable.Type == TextureType.Texture1D)
                                    methodName += "1D";
                                else if (targetVariable.Type == TextureType.Texture2D)
                                    methodName += "2D";
                                else if (targetVariable.Type == TextureType.Texture3D)
                                    methodName += "3D";
                                else if (targetVariable.Type == TextureType.TextureCube)
                                    methodName += "Cube";
                                else
                                    parserResult.Error("Unable to find texture profile in compatibility mode [{0}]", methodInvocationExpression.Span, methodInvocationExpression);
                            }

                            if (glslSampler == null)
                            {
                                parserResult.Error("Unable to find matching texture/sampler expression [{0}]", methodInvocationExpression.Span, methodInvocationExpression);
                                return methodInvocationExpression;
                            }

                            bool hasBias = memberReferenceExpression.Member == "SampleBias";
                            if (hasBias)
                                baseParameterCount++;

                            if (memberReferenceExpression.Member == "SampleGrad")
                            {
                                baseParameterCount += 2;
                                methodName += "Grad";
                            }

                            if (memberReferenceExpression.Member == "SampleLevel" || memberReferenceExpression.Member == "SampleCmpLevelZero")
                            {
                                baseParameterCount++;
                                methodName += "Lod";

                                if (memberReferenceExpression.Member == "SampleCmpLevelZero")
                                {
                                    methodInvocationExpression.Arguments.Add(new LiteralExpression(0.0f));
                                }
                            }

                            if (isLoad)
                            {
                                if (shaderPlatform == GlslShaderPlatform.OpenGLES && shaderVersion < 300) // ES2
                                    methodName += "Lod";
                                else
                                    methodName = "texelFetch";
                            }

                            if (memberReferenceExpression.Member == "SampleCmp" || memberReferenceExpression.Member == "SampleCmpLevelZero")
                            {
                                // Need to convert texture.SampleCmp(texcoord, compareValue) to texture(vec3(texcoord, compareValue))
                                var texcoord = methodInvocationExpression.Arguments[1];
                                methodInvocationExpression.Arguments[1] = new MethodInvocationExpression(
                                    new TypeReferenceExpression(new VectorType(ScalarType.Float, TypeBase.GetDimensionSize(texcoord.TypeInference.TargetType, 1) + 1)),
                                    methodInvocationExpression.Arguments[1],
                                    methodInvocationExpression.Arguments[2]
                                    );
                                methodInvocationExpression.Arguments.RemoveAt(2);
                            }

                            if (methodInvocationExpression.Arguments.Count == baseParameterCount + 1)
                            {
                                methodName += "Offset";
                            }
                            else if (methodInvocationExpression.Arguments.Count != baseParameterCount)
                            {
                                parserResult.Error("Unable to match arguments count with expected parameters for expression [{0}]", methodInvocationExpression.Span, methodInvocationExpression);
                                return methodInvocationExpression;
                            }

                            // texture.Sample has a sampler parameter but texture.Load doesn't, so replace/add accordingly
                            if (isLoad)
                                methodInvocationExpression.Arguments.Insert(0, glslSampler);
                            else
                                methodInvocationExpression.Arguments[0] = glslSampler;

                            // SampleBias and textureOffset conversion requires a parameter swap between bias and offset.
                            if (hasBias && methodName == "textureOffset")
                            {
                                var temp = methodInvocationExpression.Arguments[2];
                                methodInvocationExpression.Arguments[3] = methodInvocationExpression.Arguments[2];
                                methodInvocationExpression.Arguments[2] = temp;
                            }

                            // For OpenGL ES, texelFetch on Buffer might not be available, so we use a #define to easily convert it to a Texture instead
                            if (shaderPlatform == GlslShaderPlatform.OpenGLES && shaderVersion < 320 && isLoad && targetVariableType.Name.Text == "Buffer" && methodName == "texelFetch")
                                methodName = "texelFetchBuffer";

                            methodInvocationExpression.Target = new VariableReferenceExpression(methodName);

                            if (isLoad)
                            {
                                // Since Texture.Load works with integer coordinates, need to convert texture.Load(coords, [offset]) to:
                                //    - textureLod[Offset](texture_sampler, coords.xy / textureSize(texture_sampler), coords.z, [offset]) on OpenGL ES 2
                                //    - texelFetch[Offset](texture_sampler, coords.xy, coords.z, [offset]) on OpenGL and ES 3
                                
                                string dimP = "??";
                                string mipLevel = "?";

                                switch (targetVariableType.Name.Text)
                                {
                                    case "Buffer":
                                        dimP = "x";
                                        mipLevel = string.Empty;
                                        break;
                                    case "Texture1D":
                                        dimP = "x";
                                        mipLevel = "y";
                                        break;
                                    case "Texture2D":
                                    case "Texture2DMS":
                                    case "Texture1DArray":
                                        dimP = "xy";
                                        mipLevel = "z";
                                        break;
                                    case "Texture2DArray":
                                    case "Texture2DArrayDMS":
                                    case "Texture3D":
                                        dimP = "xyz";
                                        mipLevel = "w";
                                        break;
                                    default:
                                        parserResult.Error("Unable to process texture coordinates for type [{0}] when processing expression [{1}]", methodInvocationExpression.Span, targetVariableType.Name.Text,  methodInvocationExpression);
                                        break;
                                }

                                bool isES2 = shaderPlatform == GlslShaderPlatform.OpenGLES && shaderVersion < 300;
                                
                                // Note: older GL versions don't allow scalar swizzle, so let's avoid them
                                var coordExpr = dimP == "x" ? NewCast(new VectorType(isES2 ? ScalarType.Float : ScalarType.Int, dimP.Length), methodInvocationExpression.Arguments[1]) : new MemberReferenceExpression(new ParenthesizedExpression(methodInvocationExpression.Arguments[1]), dimP);

                                if (shaderPlatform == GlslShaderPlatform.OpenGLES && shaderVersion < 300) // ES2
                                {
                                    if (mipLevel.Length > 0)
                                        methodInvocationExpression.Arguments.Insert(2, NewCast(ScalarType.Float, new MemberReferenceExpression(new ParenthesizedExpression(methodInvocationExpression.Arguments[1].DeepClone()), mipLevel)));

                                    methodInvocationExpression.Arguments[1] = NewCast(new VectorType(ScalarType.Float, dimP.Length), new BinaryExpression(
                                        BinaryOperator.Divide,
                                        coordExpr,
                                        NewCast(new VectorType(ScalarType.Float, dimP.Length), new MethodInvocationExpression("textureSize", glslSampler, new LiteralExpression(0)))));
                                }
                                else
                                {
                                    if (mipLevel.Length > 0)
                                        methodInvocationExpression.Arguments.Insert(2, NewCast(ScalarType.Int, new MemberReferenceExpression(new ParenthesizedExpression(methodInvocationExpression.Arguments[1].DeepClone()), mipLevel)));

                                    methodInvocationExpression.Arguments[1] = coordExpr;
                                }

                                // D3D returns an object of type T, but OpenGL returns an object of type gvec4
                                var expectedResultType = methodInvocationExpression.TypeInference.TargetType;
                                methodInvocationExpression.TypeInference.TargetType = new VectorType((ScalarType)TypeBase.GetBaseType(expectedResultType), 4);
                                methodInvocationExpression = (MethodInvocationExpression)NewCast(expectedResultType, methodInvocationExpression);
                            }

                            // TODO: Check how many components are required
                            // methodInvocationExpression.Arguments[1] = new MemberReferenceExpression(new ParenthesizedExpression(methodInvocationExpression.Arguments[1]), "xy");
                        }

                        // Set methodDeclaration to null, so that "Add default parameters" doesn't do anything little bit further in this method
                        methodDeclaration = null;

                        break;
                }
            }

            // Handle type reference expression
            var typeReferenceExpression = methodInvocationExpression.Target as TypeReferenceExpression;
            if (typeReferenceExpression != null)
            {
                // Convert matrix type initializers
                var matrixType = typeReferenceExpression.Type.ResolveType() as MatrixType;
                if (matrixType != null)
                {
                    methodInvocationExpression.Arguments = this.ConvertMatrixInitializer(matrixType, methodInvocationExpression.Arguments);
                }
            }

            // Add default parameters
            if (methodDeclaration != null)
            {
                for (int i = methodInvocationExpression.Arguments.Count; i < methodDeclaration.Parameters.Count; i++)
                    methodInvocationExpression.Arguments.Add(methodDeclaration.Parameters[i].InitialValue);
            }

            CheckCastMethod(methodInvocationExpression);

            // For GeometryShader remove stream type method invocation
            RemoveStreamTypeFromMethodInvocation(methodInvocationExpression);

            return methodInvocationExpression;
        }

        private void RemoveStreamTypeFromMethodInvocation(MethodInvocationExpression expression)
        {
            // Remove parameters that are StreamType
            for (int i = expression.Arguments.Count - 1; i >= 0; i--)
            {
                var argument = expression.Arguments[i];
                if (ClassType.IsStreamOutputType(argument.TypeInference.TargetType))
                {
                    expression.Arguments.RemoveAt(i);
                }
            }
        }

        private void RemoveStreamTypeFromMethodDefinition(MethodDeclaration declaration)
        {
            // Remove parameters that are StreamType
            for (int i = declaration.Parameters.Count - 1; i >= 0; i--)
            {
                var argument = declaration.Parameters[i];
                if (ClassType.IsStreamOutputType(argument.Type.TypeInference.TargetType))
                {
                    declaration.Parameters.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Visits the specified conditional expression.
        /// </summary>
        /// <param name="conditionalExpression">The conditional expression.</param>
        public override Node Visit(ConditionalExpression conditionalExpression)
        {
            base.Visit(conditionalExpression);

            var conditionType = conditionalExpression.Condition.TypeInference.TargetType;

            // Convert float4(xxx) ? left : right to mix(left, right, float4(xxx) == 0);
            if (conditionType is VectorType)
            {
                var methodInvocation = new MethodInvocationExpression("mix", conditionalExpression.Left, conditionalExpression.Right,   
                new MethodInvocationExpression("equal", conditionalExpression.Condition, new MethodInvocationExpression(new TypeReferenceExpression(conditionType), new LiteralExpression(0)) ));
                return methodInvocation;
            }
            else
            {
                conditionalExpression.Condition = ConvertCondition(conditionalExpression.Condition);
            }

            return conditionalExpression;
        }

        /// <summary>
        /// Visits the specified constant buffer.
        /// </summary>
        /// <param name="constantBuffer">The constant buffer.</param>
        public override Node Visit(ConstantBuffer constantBuffer)
        {
            base.Visit(constantBuffer);

            // Remove initializers from constant buffers
            foreach (var variable in constantBuffer.Members.OfType<Variable>())
            {
                if (variable.InitialValue != null)
                {
                    parserResult.Warning("Initializer in uniform block are not supported in glsl [{0}]", variable.Span, variable);
                    variable.InitialValue = null;
                }
            }

            return constantBuffer;
        }

        /// <summary>
        /// Visits the specified var ref expr.
        /// </summary>
        /// <param name="varRefExpr">The var ref expr.</param>
        /// <returns>A transformed expression.</returns>
        public override Node Visit(VariableReferenceExpression varRefExpr)
        {
            base.Visit(varRefExpr);

            // If this is a global variable used as a temporary, don't perform any transform on it
            if (globalUniformVisitor.IsVariableAsGlobalTemporary(varRefExpr))
            {
                return varRefExpr;
            }

            // Use ConvertExpression on variable.
            var variable = FindDeclaration(varRefExpr.Name) as Variable;
            if (variable != null)
            {
                var result = this.ConvertReferenceToSemantics(varRefExpr, variable.Semantic(), variable.Type.ResolveType(), variable.Name, variable.Span);
                if (result != null)
                    return result;
            }

            return varRefExpr;
        }

        /// <summary>
        /// Visits the specified if statement.
        /// </summary>
        /// <param name="ifStatement">If statement.</param>
        public override Node Visit(IfStatement ifStatement)
        {
            base.Visit(ifStatement);
            ifStatement.Condition = ConvertCondition(ifStatement.Condition);

            return ifStatement;
        }

        /// <summary>
        /// Visit the for statement and unroll it if necessary
        /// </summary>
        /// <param name="forStatement"></param>
        /// <returns></returns>
        public override Node Visit(ForStatement forStatement)
        {
            base.Visit(forStatement);

            // unroll foreach if necessary
            if (UnrollForLoops && forStatement.Attributes.OfType<AttributeDeclaration>().Any(x => x.Name.Text == "unroll"))
            {
                var breakFlag = new Variable(ScalarType.Bool, "isBreak" + breakIndex, new LiteralExpression(false));
                ++breakIndex;
                var breakVisitor = new BreakContinueVisitor();
                var hasBreak = breakVisitor.Run(forStatement, breakFlag, "break", parserResult);
                
                var continueFlag = new Variable(ScalarType.Bool, "isContinue" + breakIndex, new LiteralExpression(false));
                ++breakIndex;
                var continueVisitor = new BreakContinueVisitor();
                var hasContinue = continueVisitor.Run(forStatement, continueFlag, "continue", parserResult);

                int startValue;
                var varName = GetStartForStatement(forStatement, out startValue);

                if (varName != null)
                {
                    var iterCount = GetIterCountForStatement(forStatement, varName, startValue);

                    if (iterCount > 0)
                    {
                        var statements = new BlockStatement(new StatementList());
                        statements.Statements.Add(forStatement.Start);
                        if (hasBreak)
                            statements.Statements.Add(new DeclarationStatement(breakFlag));
                        if (hasContinue)
                            statements.Statements.Add(new DeclarationStatement(continueFlag));

                        var lastStatement = statements;

                        for (int i = 0; i < iterCount; ++i)
                        {
                            var clonedBody = forStatement.Body.DeepClone();
                            var blockStatement = clonedBody as BlockStatement ?? new BlockStatement(new StatementList(clonedBody));
                            blockStatement.Statements.Add(new ExpressionStatement(forStatement.Next));
                            
                            if (hasContinue) // reset the flag
                                blockStatement.Statements.Add(new ExpressionStatement(new AssignmentExpression(AssignmentOperator.Default, new VariableReferenceExpression(continueFlag), new LiteralExpression(false))));
                            
                            if (hasBreak)
                            {
                                var ifStatement = new IfStatement();
                                ifStatement.Condition = new UnaryExpression(UnaryOperator.LogicalNot, new VariableReferenceExpression(breakFlag));
                                ifStatement.Then = blockStatement;
                                lastStatement.Statements.Add(ifStatement);
                            }
                            else
                            {
                                lastStatement.Statements.Add(blockStatement);
                            }
                            lastStatement = blockStatement;
                        }
                        return statements;
                    }
                    if (iterCount == 0)
                    {
                        return new EmptyStatement();
                    }
                }
                parserResult.Error("Unable to unroll for statement [{0}]", forStatement.Span, forStatement);
            }

            return forStatement;
        }

        /// <summary>
        /// Get the Variable used 
        /// </summary>
        /// <param name="forStatement">the for statement</param>
        /// <param name="startValue">the start value of the loop, to fill</param>
        /// <returns>the variable</returns>
        private static string GetStartForStatement(ForStatement forStatement, out int startValue)
        {
            var startStatement = forStatement.Start as DeclarationStatement;
            var startStatementAssign = forStatement.Start as ExpressionStatement;
            startValue = 0;

            if (startStatement != null)
            {
                var variable = startStatement.Content as Variable;
                if (variable != null)
                {
                    var evaluatorStart = new ExpressionEvaluator();
                    var resultStart = evaluatorStart.Evaluate(variable.InitialValue);
                    if (resultStart.HasErrors)
                        return null;
                    startValue = (int)((double)resultStart.Value);

                    return variable.Name.Text;
                }
            }
            else if (startStatementAssign != null)
            {
                var assign = startStatementAssign.Expression as AssignmentExpression;
                if (assign != null && assign.Operator == AssignmentOperator.Default)
                {
                    var vre = assign.Target as VariableReferenceExpression;
                    if (vre != null)
                    {
                        var evaluatorStart = new ExpressionEvaluator();
                        var resultStart = evaluatorStart.Evaluate(assign.Value);
                        if (resultStart.HasErrors)
                            return null;
                        startValue = (int)((double)resultStart.Value);

                        return vre.Name.Text;
                    }
                }
            }
            
            return null;
        }

        /// <summary>
        /// Get the number of loops
        /// </summary>
        /// <param name="forStatement">the for statement</param>
        /// <param name="variableName">the name of the iterator variable</param>
        /// <param name="startValue">the start value of the iterator</param>
        /// <returns>the number of loops</returns>
        private static int GetIterCountForStatement(ForStatement forStatement, string variableName, int startValue)
        {
            var condition = forStatement.Condition as BinaryExpression;
            if (condition == null)
                return -1;

            var evaluatorStop = new ExpressionEvaluator();
            var resultStop = evaluatorStop.Evaluate(condition.Right);
            if (resultStop.HasErrors)
                return -1;

            var stopValue = (int)((double)resultStop.Value);
            var step = 1;

            var stepExpr = forStatement.Next as UnaryExpression;
            if (stepExpr == null)
            {
                var stepAssign = forStatement.Next as AssignmentExpression;
                if (stepAssign != null)
                {
                    if (stepAssign.Operator == AssignmentOperator.Default)
                    {
                        var assignedVar = stepAssign.Target as VariableReferenceExpression;
                        var valueAssigned = stepAssign.Value as BinaryExpression;
                        if (assignedVar == null || valueAssigned == null)
                            return -1;

                        var left = valueAssigned.Left as VariableReferenceExpression;
                        var right = valueAssigned.Right as VariableReferenceExpression;

                        if (left != null && left.Name.Text == variableName && valueAssigned.Right is LiteralExpression)
                        {
                            step = (int)(valueAssigned.Right as LiteralExpression).Value;
                        }
                        else if (right != null && right.Name.Text == variableName && valueAssigned.Left is LiteralExpression)
                        {
                            step = (int)(valueAssigned.Left as LiteralExpression).Value;
                        }
                        else
                            return -1;
                    }
                    else if (stepAssign.Operator == AssignmentOperator.Addition || stepAssign.Operator == AssignmentOperator.Subtraction)
                    {
                        var assignedVar = stepAssign.Target as VariableReferenceExpression;
                        if (assignedVar == null || assignedVar.Name.Text != variableName)
                            return -1;

                        var evaluatorValueAssigned = new ExpressionEvaluator();
                        var resultValueAssigned = evaluatorValueAssigned.Evaluate(stepAssign.Value);
                        if (resultValueAssigned.HasErrors)
                            return -1;

                        step = (int)((double)resultValueAssigned.Value);
                        if (stepAssign.Operator == AssignmentOperator.Subtraction)
                            step = -step;
                    }
                }
                else
                    return -1;
            }
            else
            {
                switch (stepExpr.Operator)
                {
                    case UnaryOperator.PostDecrement:
                    case UnaryOperator.PreDecrement:
                        step = -1;
                        break;
                    case UnaryOperator.PostIncrement:
                    case UnaryOperator.PreIncrement:
                        step = 1;
                        break;
                }
            }

            switch (condition.Operator)
            {
                case BinaryOperator.Less:
                    return (stopValue - startValue - 1) / step + 1;
                case BinaryOperator.Greater:
                    return (stopValue - startValue - 1) / step + 1;
                case BinaryOperator.LessEqual:
                    return (stopValue - startValue) / step + 1;
                case BinaryOperator.GreaterEqual:
                    return (stopValue - startValue) / step + 1;
                case BinaryOperator.Equality:
                    {
                        if (startValue == stopValue)
                            return 1;
                        else
                            return 0;
                    }
                default:
                    return -1;
            }
        }

        /// <summary>
        /// Visits the specified expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>A transformed expression</returns>
        public override Node Visit(MemberReferenceExpression expression)
        {
            base.Visit(expression);

            // A matrix contains values organized in rows and columns, which can be accessed using the structure operator "." followed by one of two naming sets:
            // The zero-based row-column position:
            // _m00, _m01, _m02, _m03
            // _m10, _m11, _m12, _m13
            // _m20, _m21, _m22, _m23
            // _m30, _m31, _m32, _m33
            // The one-based row-column position:
            // _11, _12, _13, _14
            // _21, _22, _23, _24
            // _31, _32, _33, _34
            // _41, _42, _43, _44
            var matrixType = expression.Target.TypeInference.TargetType as MatrixType;
            if (matrixType != null)
            {
                var swizzles = HlslSemanticAnalysis.MatrixSwizzleDecode(expression);

                // When NoSwapForBinaryMatrixOperation, we need to transpose accessor
                if (NoSwapForBinaryMatrixOperation)
                {
                    for (int i = 0; i < swizzles.Count; ++i)
                    {
                        swizzles[i] = new MatrixType.Indexer(swizzles[i].Column, swizzles[i].Row);
                    }
                }

                if (swizzles.Count == 1)
                    return new IndexerExpression(new IndexerExpression(expression.Target, new LiteralExpression(swizzles[0].Row)), new LiteralExpression(swizzles[0].Column));

                if (swizzles.Count > 1 && swizzles.Count <= 4)
                {
                    var swizzleVectorInvoke = new MethodInvocationExpression(new TypeReferenceExpression(expression.TypeInference.TargetType));

                    foreach (var swizzle in swizzles)
                        swizzleVectorInvoke.Arguments.Add(new IndexerExpression(new IndexerExpression(expression.Target, new LiteralExpression(swizzle.Row)), new LiteralExpression(swizzle.Column)));
                    return swizzleVectorInvoke;
                }
            }

            // Scalars can only be swizzled in OpenGL 4.2. Wrap in vector type.
            var scalarType = expression.Target.TypeInference.TargetType as ScalarType;
            if (scalarType != null && expression.Member.Text.All(x => x == 'x'))
            {
                var targetAsVector = new MethodInvocationExpression(new TypeReferenceExpression(new VectorType(scalarType, expression.Member.Text.Length)));
                targetAsVector.Arguments.Add(expression.Target);

                return targetAsVector;
            }

            return expression;
        }

        /// <summary>
        /// Visits the specified array creation expression.
        /// </summary>
        /// <param name="arrayCreationExpression">The array creation expression.</param>
        /// <returns>A transformed expression</returns>
        public override Node Visit(ArrayInitializerExpression arrayCreationExpression)
        {
            var variable = ParentNode as Variable;

            var result = (Expression)base.Visit(arrayCreationExpression);

            // If there is a parent variable and no subscript, It is probably a cast to an implicit array type (float2,float3,float4...etc.)
            if (variable != null)
            {
                var variableType = variable.Type.ResolveType();

                var arrayType = variableType as ArrayType;
                if (arrayType != null)
                {
                    return this.ConvertArrayInitializer(arrayType, arrayCreationExpression);
                }
                else
                {
                    // Transform array creation to an explicit cast
                    // HLSL => float4 toto = {1,2,3,4};
                    // GLSL => vec4 toto = vec4(1,2,3,4);
                    var items = new List<Expression>();
                    FlattenArrayCreationExpression(arrayCreationExpression, items);
                    var castToType = new MethodInvocationExpression(new TypeReferenceExpression(variable.Type));

                    // If matrix type, then use common function to convert the initializer
                    var matrixType = variableType as MatrixType;
                    if (matrixType != null)
                    {
                        items = this.ConvertMatrixInitializer(matrixType, items);
                    }

                    foreach (var expression in items)
                        castToType.Arguments.Add(expression);

                    result = castToType;
                } 
            }

            return result;
        }

        /// <summary>
        /// Visits the specified assign expression.
        /// </summary>
        /// <param name="assignExpression">The assign expression.</param>
        /// <returns>A transformed expression</returns>
        public override Node Visit(AssignmentExpression assignExpression)
        {
            // Put a special flag while visiting assignment target for tracking assignment to input varying (not allowed in OpenGL)
            // TODO: use stack of assignmentTarget instead, as it would not work with nested assignement
            isAssignmentTarget = true;
            assignExpression.Target = (Expression)VisitDynamic(assignExpression.Target);
            isAssignmentTarget = false;
            assignExpression.Value = (Expression)VisitDynamic(assignExpression.Value);

            // If right expression is null, we can assume that it was removed on the right side
            // So we can safely remove the whole expression
            if (assignExpression.Value == null)
                return null;

            return assignExpression;
        }

        /// <summary>
        /// Visits the specified technique.
        /// </summary>
        /// <param name="technique">The technique.</param>
        /// <returns>The technique</returns>
        public override Node Visit(Technique technique)
        {
            // Skip all techniques while parsing
            return technique;
        }

        /// <summary>
        /// Visits the specified binary expression.
        /// </summary>
        /// <param name="binaryExpression">The binary expression.</param>
        /// <returns>A transformed binary expression.</returns>
        public override Node Visit(BinaryExpression binaryExpression)
        {
            base.Visit(binaryExpression);

            // -----------------------------------------------------
            // Handle binary expression with gl_FrontFacing variable
            // -----------------------------------------------------
            bool isLeftFrontFacing = string.Compare(VariableReferenceExpression.GetVariableName(binaryExpression.Left), gl_FrontFacing, StringComparison.OrdinalIgnoreCase) == 0;
            bool isRightFrontFacing = string.Compare(VariableReferenceExpression.GetVariableName(binaryExpression.Right), gl_FrontFacing, StringComparison.OrdinalIgnoreCase) == 0;
            if (isLeftFrontFacing || isRightFrontFacing)
            {
                bool isLessOperator = binaryExpression.Operator == BinaryOperator.Less || binaryExpression.Operator == BinaryOperator.LessEqual;
                bool isGreaterOperator = binaryExpression.Operator == BinaryOperator.Greater || binaryExpression.Operator == BinaryOperator.GreaterEqual;

                // If the operator is supported, then return gl_FrontFacing or !glFrontFacing
                var glFrontFacingVar = isLeftFrontFacing ? binaryExpression.Left : binaryExpression.Right;
                glFrontFacingVar.TypeInference.TargetType = ScalarType.Bool;
                if (isLessOperator || isGreaterOperator)
                {
                    if ((isLessOperator && isLeftFrontFacing) || (isRightFrontFacing && isGreaterOperator))
                        return new UnaryExpression(UnaryOperator.LogicalNot, glFrontFacingVar) { TypeInference = { TargetType = ScalarType.Bool } };

                    return glFrontFacingVar;
                }

                // Else convert the glFrontFacing to a -1/1 variable
                var newGlFrontFacing = new ParenthesizedExpression(new ConditionalExpression(glFrontFacingVar, new LiteralExpression(1), new LiteralExpression(-1)));

                if (isLeftFrontFacing)
                    binaryExpression.Left = newGlFrontFacing;
                else
                    binaryExpression.Right = newGlFrontFacing;
            }

            // -----------------------------------------------------
            // Handle conversion between types
            // -----------------------------------------------------
            var leftType = binaryExpression.Left.TypeInference.TargetType;
            var rightType = binaryExpression.Right.TypeInference.TargetType;

            Expression outputExpression = binaryExpression;

            if (leftType != null && rightType != null)
            {
                bool isOperationOnVectors = leftType is VectorType && rightType is VectorType && ((VectorType)leftType).Dimension > 1 && ((VectorType)rightType).Dimension > 1;

                if (binaryExpression.Operator == BinaryOperator.Multiply)
                {
                    if (leftType is MatrixType && rightType is MatrixType)
                    {
                        var matrixMul = new MethodInvocationExpression(new VariableReferenceExpression(new Identifier("matrixCompMult")));
                        matrixMul.Arguments.Add(binaryExpression.Left);
                        matrixMul.Arguments.Add(binaryExpression.Right);

                        outputExpression = matrixMul;
                    }
                }
                else if (binaryExpression.Operator == BinaryOperator.Modulo)
                {
                    if (!ScalarType.IsInteger(leftType) || !ScalarType.IsInteger(rightType))
                    {
                        var matrixMul = new MethodInvocationExpression(new VariableReferenceExpression(new Identifier("mod")));
                        matrixMul.Arguments.Add(binaryExpression.Left);
                        matrixMul.Arguments.Add(binaryExpression.Right);

                        outputExpression = matrixMul;
                    }
                }
                else if (binaryExpression.Operator == BinaryOperator.Less || binaryExpression.Operator == BinaryOperator.Greater || binaryExpression.Operator == BinaryOperator.LessEqual
                         || binaryExpression.Operator == BinaryOperator.GreaterEqual || binaryExpression.Operator == BinaryOperator.Equality || binaryExpression.Operator == BinaryOperator.Inequality)
                {
                    if (isOperationOnVectors)
                    {
                        string comparisonName;
                        switch (binaryExpression.Operator)
                        {
                            case BinaryOperator.Less:
                                comparisonName = "lessThan";
                                break;
                            case BinaryOperator.LessEqual:
                                comparisonName = "lessThanEqual";
                                break;
                            case BinaryOperator.Greater:
                                comparisonName = "greaterThan";
                                break;
                            case BinaryOperator.GreaterEqual:
                                comparisonName = "greaterThanEqual";
                                break;
                            case BinaryOperator.Equality:
                                comparisonName = "equal";
                                break;
                            case BinaryOperator.Inequality:
                                comparisonName = "notEqual";
                                break;
                            default:
                                parserResult.Error("Unsupported binary expression on vectors [{0}]", binaryExpression.Span, binaryExpression);
                                return binaryExpression;
                        }

                        var comparisonExpr = new MethodInvocationExpression(new VariableReferenceExpression(new Identifier(comparisonName)));
                        comparisonExpr.Arguments.Add(binaryExpression.Left);
                        comparisonExpr.Arguments.Add(binaryExpression.Right);

                        int dimension = ((VectorType)binaryExpression.TypeInference.TargetType).Dimension;
                        comparisonExpr.TypeInference.TargetType = new VectorType(ScalarType.Bool, dimension);
                        outputExpression = comparisonExpr;
                    }
                }
                else if (binaryExpression.Operator == BinaryOperator.LogicalOr || binaryExpression.Operator == BinaryOperator.LogicalAnd)
                {
                    binaryExpression.Left = ConvertCondition(binaryExpression.Left);
                    binaryExpression.Right = ConvertCondition(binaryExpression.Right);
                    binaryExpression.TypeInference.TargetType = ScalarType.Bool;

                    if (isOperationOnVectors)
                    {
                        parserResult.Error(
                            "Boolean operation && || on expression [{0}] cannot be converted safely to GLSL, as GLSL doesn't support boolean operators function on a per-component basis. Code is generated but invalid", 
                            binaryExpression.Span, 
                            binaryExpression);
                    }
                }
            }

            return outputExpression;
        }

        /// <summary>
        /// Visits the specified return statement.
        /// </summary>
        /// <param name="returnStatement">The return statement.</param>
        /// <returns>A transformed return statement.</returns>
        public override Node Visit(ReturnStatement returnStatement)
        {
            base.Visit(returnStatement);

            // Only transform return in entry function
            if (!IsInEntryPoint)
                return returnStatement;

            // This should only process return statements with ConvertReturn which are not detected at the block level.
            // As an example, a return statement which is not enclosed in a block, such as "if (X) return Y;", should be converted to if (X) { out_x = x; out_y = y; ... }
            return ConvertReturn(returnStatement.Value, true, returnStatement.Span);
        }

        /// <summary>
        /// Visits the specified statement list.
        /// </summary>
        /// <param name="statementList">The statement list.</param>
        /// <returns>
        /// A transformed statement list.
        /// </returns>
        public override Node Visit(StatementList statementList)
        {
            // Try to transform return statement with ConvertReturn at the block level first.
            // As an example, { stmt1; return Y; } gets converted to { stmt1; out_x = x; out_y = y; ... }
            var newStatementList = new StatementList();
            for (int i = 0; i < statementList.Statements.Count; i++)
            {
                var stmt = statementList.Statements[i];
                bool converted = false;

                if (stmt is ReturnStatement)
                {
                    // Only transform return in entry function
                    if (IsInEntryPoint)
                    {
                        var returnValue = ((ReturnStatement)stmt).Value;

                        // Don't emit return for last return of a function
                        bool emitReturn = !(ParentNode is MethodDefinition && (i + 1) == statementList.Statements.Count);

                        // Return statements could not have a value
                        if (returnValue != null)
                        {
                            var subStatements = ConvertReturn(((ReturnStatement)stmt).Value, emitReturn, stmt.Span);
                            if (subStatements is StatementList)
                                newStatementList.AddRange((StatementList)subStatements);
                            else
                                newStatementList.Add(subStatements);

                            converted = true;
                        }
                        else if (!emitReturn)
                            converted = true;
                    }
                }
                else if (stmt is DeclarationStatement)
                {
                    var variable = ((DeclarationStatement)stmt).Content as Variable;

                    // Remove register/semantics from local variable declaration
                    if (variable != null)
                        variable.Qualifiers.Values.RemoveAll(qualifierArg => qualifierArg is RegisterLocation || qualifierArg is Semantic);
                }
                else if (stmt is ExpressionStatement)
                {
                    var exprStmt = (ExpressionStatement)stmt;

                    // Handle tuple cast. Only support default scalars 
                    if (exprStmt.Expression is AssignmentExpression)
                    {
                        var assignExpression = exprStmt.Expression as AssignmentExpression;
                        if (assignExpression.Target is MethodInvocationExpression && assignExpression.Operator == AssignmentOperator.Default)
                        {
                            var tupleExpression = (MethodInvocationExpression)assignExpression.Target;
                            var typeReferenceExpression = tupleExpression.Target as TypeReferenceExpression;
                            var tupleType = typeReferenceExpression != null ? typeReferenceExpression.Type.ResolveType() : null;

                            if (typeReferenceExpression != null && tupleType != null && !(tupleType is MatrixType))
                            {
                                var tupleBlock = new BlockStatement();
                                const string TemporaryTupleName = "_tuple_temp_";
                                var variableTuple = new Variable(VectorType.Float4, TemporaryTupleName, assignExpression.Target);
                                tupleBlock.Statements.Add(new DeclarationStatement(variableTuple));

                                int startMember = 0;

                                const string SwizzleMembers = "xyzw";

                                bool hasError = false;

                                foreach (var expression in tupleExpression.Arguments)
                                {
                                    var argumentType = expression.TypeInference.TargetType;
                                    if (argumentType != null)
                                    {
                                        var argumentDimension = (argumentType is VectorType) ? ((VectorType)argumentType).Dimension : 1;

                                        tupleBlock.Statements.Add(
                                            new ExpressionStatement(
                                                new AssignmentExpression(
                                                    AssignmentOperator.Default, 
                                                    expression, 
                                                    new MemberReferenceExpression(new VariableReferenceExpression(TemporaryTupleName), SwizzleMembers.Substring(startMember, argumentDimension)))));
                                        startMember += argumentDimension;
                                    }
                                    else
                                        hasError = true;
                                }

                                if (!hasError)
                                {
                                    converted = true;
                                    newStatementList.Add(tupleBlock);
                                }
                            }
                        }
                    }
                    else
                    {
                        var methodInvocationExpr = exprStmt.Expression as MethodInvocationExpression;
                        var method = methodInvocationExpr != null ? methodInvocationExpr.Target as MemberReferenceExpression : null;

                        // Handle geometry shader vertex emit
                        if (method != null && method.Target is VariableReferenceExpression)
                        {
                            var targetVariable = (VariableReferenceExpression) method.Target;
                            var targetType = targetVariable.TypeInference.TargetType;
                            if (ClassType.IsStreamOutputType(targetType))
                            {
                                if (method.Member == "Append")
                                {
                                    //var globalVariable = shader.Declarations.Find(node => node is Variable && ((Variable) node).Name == targetVariable.Name);
                                    //if (globalVariable == null)
                                    //{
                                    //    var streamType = ((ClassType) targetType).GenericArguments[0];
                                    //    var streamOutVariable = new Variable(new ArrayType(streamType), targetVariable.Name);
                                    //    streamOutVariable.Qualifiers |= ParameterQualifier.Out;
                                    //    AddGlobalDeclaration(streamOutVariable);
                                    //}
       
                                    if (targetType.Name == "TriangleStream")
                                        geometryLayoutOutput = "triangle_strip";
                                    else if (targetType.Name == "LineStream")
                                        geometryLayoutOutput = "line_strip";
                                    else if (targetType.Name == "PointStream")
                                        geometryLayoutOutput = "points";
                                    else
                                    {
                                        parserResult.Error("Unknown OutputStream type [{0}] (should be TriangleStream, LineStream or PointStream", exprStmt.Span, targetType.Name);
                                        return newStatementList;
                                    }

                                    var returnStatement = ConvertReturn(methodInvocationExpr.Arguments[0], false, null);
                                    if (returnStatement is StatementList)
                                        newStatementList.AddRange((StatementList) returnStatement);
                                    else
                                        newStatementList.Add(returnStatement);
                                    newStatementList.Add(new ExpressionStatement(new MethodInvocationExpression(new VariableReferenceExpression("EmitVertex"))));
                                    converted = true;
                                }
                                else if (method.Member == "RestartStrip")
                                {
                                    newStatementList.Add(new ExpressionStatement(new MethodInvocationExpression(new VariableReferenceExpression("EndPrimitive"))));
                                    converted = true;
                                }
                            }
                        }
                    }
                }

                if (!converted)
                    newStatementList.Add(stmt);
            }

            base.Visit(newStatementList);

            return newStatementList;
        }

        /// <summary>
        /// Visits the specified indexer expression.
        /// </summary>
        /// <param name="indexerExpression">The indexer expression.</param>
        /// <returns>A transformed indexer expression</returns>
        public override Node Visit(IndexerExpression indexerExpression)
        {
            // Collect all indices in the order of the declaration
            var targetIterator = indexerExpression.Target;
            var indices = new List<Expression> { indexerExpression.Index };
            while (targetIterator is IndexerExpression)
            {
                indices.Add(((IndexerExpression)targetIterator).Index);
                targetIterator = ((IndexerExpression)targetIterator).Target;
            }

            Variable variable = null;
            Identifier varName = null;
            // Check that index apply to an array variable
            if (targetIterator is VariableReferenceExpression)
            {
                varName = ((VariableReferenceExpression)targetIterator).Name;
                variable = FindDeclaration(varName) as Variable;
            }
            else if (targetIterator is MemberReferenceExpression) // Also check arrays inside structures
            {
                varName = ((MemberReferenceExpression)targetIterator).Member;

                var target = ((MemberReferenceExpression)targetIterator).Target;
                var targetType = target.TypeInference.TargetType as StructType;

                if (targetType != null)
                    variable = targetType.Fields.FirstOrDefault(x => x.Name.Text == varName.Text);
            }

            MatrixType matrixType = null;
            if (varName != null)
            {
                // If array is a multidimension array
                var variableType = variable != null ? variable.Type.ResolveType() : null;
                var arrayType = variableType as ArrayType;
                matrixType = variableType as MatrixType;

                if (arrayType != null && arrayType.Dimensions.Count == indices.Count)
                {
                    // Transform multi-dimensionnal array to single dimension
                    // float myarray[s1][s2][s3]...[sn] = {{.{..{...}}};
                    // float value = myarray[i1][i2][i3]...[in]    => float value = myarray[(i1)*(s2)*(s3)*...*(sn) + (i2)*(s3)*...*(sn) + (i#)*(s#+1)*(s#+2)*...*(sn)];
                    // The indice list is in reversed order => <[sn]...[s3][s2][s1]>
                    Expression finalIndex = null;
                    for (int i = 0; i < indices.Count; i++)
                    {
                        Expression indexExpression = indices[i];
                        for (int j = indices.Count - i; j < indices.Count; ++j)
                        {
                            var nextExpression = arrayType.Dimensions[j];
                            indexExpression = new BinaryExpression(BinaryOperator.Multiply, indexExpression, nextExpression);
                        }

                        finalIndex = finalIndex == null ? indexExpression : new BinaryExpression(BinaryOperator.Plus, finalIndex, indexExpression);
                    }

                    // Return a 1d indexer
                    indexerExpression = new IndexerExpression(targetIterator, finalIndex);
                }
            }

            base.Visit(indexerExpression);

            // When NoSwapForBinaryMatrixOperation, we need to transpose accessor
            // HLSL: float4x3[0] -> first row -> float4(...)
            // GLSL: mat4x3[0] -> first column -> float4x3[0] = vec4(mat4x3[0][0], mat4x3[1][0], mat4x3[2][0], mat4x3[3][0]);
            if (matrixType != null && NoSwapForBinaryMatrixOperation && !isAssignmentTarget)
            {
                if (indices.Count == 2)
                {
                    IndexerExpression nextExpression = null;

                    // float4x3[0][1] -> mat4x3[1][0]
                    for (int i = 0; i < indices.Count; i++)
                    {
                        nextExpression = nextExpression == null ? new IndexerExpression(targetIterator, indices[i]) : new IndexerExpression(nextExpression, indices[i]);
                    }
                    return nextExpression;
                }
                else
                {
                    // matrixType.ColumnCount
                    var matrixElementType = matrixType.Type.ResolveType() as ScalarType;
                    var matrixRowType = new VectorType(matrixElementType, matrixType.ColumnCount);

                    var convertRowToColumnMethod = new MethodInvocationExpression(new TypeReferenceExpression(matrixRowType));

                    for (int i = 0; i < matrixType.ColumnCount; i++)
                    {
                        convertRowToColumnMethod.Arguments.Add(new IndexerExpression(new IndexerExpression(indexerExpression.Target, new LiteralExpression(i)), indexerExpression.Index));
                    }
                    return convertRowToColumnMethod;
                }
            }
            
            return indexerExpression;
        }

        private void GenerateSamplerMappingAndStrip()
        {
            //var samplerMappingVisitor = new SamplerMappingVisitor(samplerMapping);
            var samplerMappingVisitor = new SamplerMappingVisitor(shader, samplerMapping)
                {
                    TextureFunctionsCompatibilityProfile = TextureFunctionsCompatibilityProfile
                };
            samplerMappingVisitor.Run(entryPoint);

            // Use the strip visitor in order to remove unused functions/declaration 
            // from the entrypoint
            var stripVisitor = new StripVisitor(entryPointName);
            stripVisitor.KeepConstantBuffers = KeepConstantBuffer;
            stripVisitor.Visit(shader);

            // Then add the newly created variable
            if (!KeepSamplers)
            {
                foreach (var textureSampler in samplerMapping)
                {
                    declarationListToRemove.Add(textureSampler.Key.Sampler);
                    declarationListToRemove.Add(textureSampler.Key.Texture);
                    AddGlobalDeclaration(textureSampler.Value);
                }
            }
            else
            {
                AddGlobalDeclaration(new Variable(StateType.SamplerState, "NoSampler"));
            }
        }

        /// <summary>
        /// Visits the specified shader.
        /// </summary>
        /// <param name="shader">The shader.</param>
        public override Node Visit(Shader shader)
        {
            geometryLayoutInput = null;
            geometryInputParameter = null;
            geometryLayoutOutput = null;

            // Remove all texture and samplers. They will be added again as they are referenced (this is because sampler+texture becomes a sampler in OpenGL).
            // shader.Declarations.RemoveAll(x => x is SamplerType);
            // shader.Declarations.RemoveAll(x => x is TextureType);

            // Visit AST.
            base.Visit(shader);

            // Post transform all array variable with multidimension
            TransformArrayDimensions();

            // Add explicit layout for ConstantBuffers
            foreach (var cBuffer in shader.Declarations.OfType<ConstantBuffer>())
            {
                AddExplicitLayout(cBuffer);
            }

            // Add uniform keyword to variables.
            foreach (var variable in shader.Declarations.OfType<Variable>())
            {
                var layoutRule = this.GetTagLayout(variable);
                bool isUniform = IsUniformLike(variable);

                // Uniforms used as global temporary are not tagged as uniforms
                if (!globalUniformVisitor.IsVariableAsGlobalTemporary(variable))
                {
                    if (isUniform)
                    {
                        variable.Qualifiers |= Ast.StorageQualifier.Uniform;

                        // For arrays, remove initializers if configured
                        var variableArrayType = variable.Type.ResolveType() as ArrayType;
                        if (variableArrayType != null && variable.InitialValue != null && !KeepUniformArrayInitializers)
                        {
                            variable.InitialValue = null;
                        }
                    }
                    else
                    {
                        if (UseLocationLayout && layoutRule.Location != null)
                        {
                            layoutRule.Qualifier.Layouts.Add(new LayoutKeyValue("location", layoutRule.Location));
                        }
                    }
                }

                // Remove HLSL Register
                variable.Qualifiers.Values.RemoveAll(qualifierType => qualifierType is RegisterLocation);
                variable.Qualifiers.Values.Remove(Ast.Hlsl.StorageQualifier.Static);
                variable.Qualifiers.Values.Remove(Ast.Hlsl.StorageQualifier.Shared);

                // If variable is an object type, remove any initial values
                var type = variable.Type.ResolveType();
                if (type is ObjectType)
                    variable.InitialValue = null;
            }

            // Add implicit layout for uniforms
            if (UseBindingLayout)
            {
                foreach (var variable in shader.Declarations.OfType<Variable>())
                {
                    if (variable.Qualifiers.Contains(Ast.StorageQualifier.Uniform))
                    {
                        // GLSL doesn't support initial values for uniforms, so we are removing them
                        // Errata: A third party GLSL compiler is supporting initial values, so we don't need to remove them
                        // variable.InitialValue = null;
                        AddImplicitLayout(variable);
                    }
                }
            }

            // Add all defined layouts to the variable's qualifiers
            foreach (var variable in shader.Declarations.OfType<Variable>())
            {
                var layoutRule = this.GetTagLayout(variable);

                // If there is any explicit layout, we have to add them to the variable
                if (layoutRule.Qualifier.Layouts.Count > 0)
                    variable.Qualifiers.Values.Insert(0, layoutRule.Qualifier);

                // Replace type if needed
                if (layoutRule.Type != null)
                    variable.Type = new TypeName(layoutRule.Type) { Span = variable.Type.Span };

                if (layoutRule.Name != null && variable.Name.Text != layoutRule.Name)
                    variable.Name = new Identifier(layoutRule.Name);
            }

            // Geometry shader specific analysis (in/out layouts).
            if (pipelineStage == PipelineStage.Geometry)
            {
                if (geometryLayoutInput != null)
                {
                    // Add layout(XXX) in; to the geometry shader
                    AddGeometryShaderInputDeclaration();
                    AddGlobalDeclaration(new Variable(new TypeName(string.Format("layout({0})", geometryLayoutInput)), "in"));
                }

                var maxVertexCount = entryPoint.Attributes().FirstOrDefault(x => x.Name == "maxvertexcount");
                if (maxVertexCount != null && geometryLayoutOutput != null)
                {
                    entryPoint.Attributes.Remove(maxVertexCount);

                    // Add layout(XXX, max_vertices=Y) out; to the geometry shader
                    AddGlobalDeclaration(new Variable(new TypeName(string.Format("layout({0}, max_vertices={1})", geometryLayoutOutput, maxVertexCount.Parameters[0])), "out"));
                }
            }

            // If there is any global uniforms used as local uniforms, we need to create locals
            foreach (var globalToLocalVariable in inputAssignment)
            {
                var globalVariable = globalToLocalVariable.Key;
                var localVariable = globalToLocalVariable.Value;
                int indexOfVariable = shader.Declarations.IndexOf(globalVariable) + 1;

                entryPoint.Body.Statements.Insert(0, new ExpressionStatement(new AssignmentExpression(AssignmentOperator.Default,
                            new VariableReferenceExpression(localVariable),
                            localVariable.InitialValue as VariableReferenceExpression) { Span = globalVariable.Span }));

                localVariable.InitialValue = null;
                shader.Declarations.Insert(indexOfVariable, new DeclarationStatement(localVariable) { Span = globalVariable.Span });
            }

            // Remove all texture and sampler declarations
            RemoveTextureAndSamplerDeclarations();

            // Transform all input/output to interface block if enabled
            TransformInputAndOutputToInterfaceBlock();

            // Clear all semantic information.
            foreach (var structureType in shader.Declarations.OfType<StructType>())
            {
                if (structureType is Ast.Glsl.InterfaceType)
                    continue;

                foreach (var fieldRef in GetMembers(structureType))
                {
                    var field = fieldRef.Field;
                    field.Qualifiers = Qualifier.None;
                }
            }

            // Remove all arguments from main.
            entryPoint.Parameters.Clear();

            // Change main definition to be void main()
            entryPoint.Qualifiers = Qualifier.None;
            entryPoint.ReturnType = TypeBase.Void;
            entryPoint.Name = new Identifier("main");

            return shader;
        }

        private void TransformInputAndOutputToInterfaceBlock()
        {
            // Transform all variables with Interface block type if enabled
            if (!UseInterfaceForInOut && pipelineStage != PipelineStage.Geometry)
                return;

            var interfaceIn = new Ast.Glsl.InterfaceType(VertexIOInterfaceName) {Qualifiers = Ast.ParameterQualifier.In};

            var interfaceOut = new Ast.Glsl.InterfaceType(VertexIOInterfaceName) {Qualifiers = Ast.ParameterQualifier.Out};

            var isInAllowed = pipelineStage != PipelineStage.Vertex && pipelineStage != PipelineStage.Geometry;
            var isOutAllowed = pipelineStage != PipelineStage.Pixel;

            for (int i = shader.Declarations.Count - 1; i >= 0; i--)
            {
                var variable = shader.Declarations[i] as Variable;
                if (variable == null || variable.Type is Ast.Glsl.InterfaceType)
                    continue;

                if (isInAllowed && variable.Qualifiers.Contains(Ast.ParameterQualifier.In))
                {
                    variable.Qualifiers.Values.Remove(Ast.ParameterQualifier.In);
                    interfaceIn.Fields.Insert(0, variable);
                    shader.Declarations.RemoveAt(i);
                }
                else if (isOutAllowed && variable.Qualifiers.Contains(Ast.ParameterQualifier.Out))
                {
                    variable.Qualifiers.Values.Remove(Ast.ParameterQualifier.Out);
                    interfaceOut.Fields.Insert(0, variable);
                    shader.Declarations.RemoveAt(i);
                }
            }


            var index = shader.Declarations.IndexOf(entryPoint);
            if (interfaceOut.Fields.Count > 0)
                shader.Declarations.Insert(index, interfaceOut);

            if (interfaceIn.Fields.Count > 0)
                shader.Declarations.Insert(index, interfaceIn);
        }


        private void AddGeometryShaderInputDeclaration()
        {
            // Convert a HLSL struct like struct VertexData { float2 texCoord; float3 normal; }
            // triangle VertexData input[3];

            // in _VertexData_ {
            //     vec2 texCoord;
            //     vec3 normal;
            // } input[];

            // TODO ADD CHECKING
            var arrayType = (ArrayType) geometryInputParameter.Type;
            var structType = arrayType.Type.TypeInference.TargetType as StructType;
            var interfaceType = new Ast.Glsl.InterfaceType { Name = VertexIOInterfaceName };
            int location = 0;
            var evaluator = new ExpressionEvaluator();
            var result = evaluator.Evaluate(arrayType.Dimensions[0]);

            int arrayLength = 0;
            if (result.HasErrors)
            {
                result.CopyTo(parserResult);
            }
            else
            {
                arrayLength = Convert.ToInt32(result.Value);
            }

            if (structType != null)
            {
                int insertPosition = 0;
                // Insert the variable to declare
                geometryInputParameter.Qualifiers = Qualifier.None;
                entryPoint.Body.Statements.Insert(insertPosition, new DeclarationStatement(geometryInputParameter));
                insertPosition++;

                const string GSInputName = "_gs_input_";

                for (int i = 0; i < arrayLength; i++)
                {
                    foreach (var field in structType.Fields)
                    {
                        var dest = new MemberReferenceExpression(new IndexerExpression(new VariableReferenceExpression(geometryInputParameter), new LiteralExpression(i)), field.Name);

                        MemberReferenceExpression src;

                        var glVariableName = GetGlVariableNameFromSemantic(field.Semantic(), true);
                        if (glVariableName != null && glVariableName.StartsWith("gl_"))
                        {
                            var newIndexerExpression = new IndexerExpression(new VariableReferenceExpression("gl_in"), new LiteralExpression(i));
                            src = new MemberReferenceExpression(newIndexerExpression, glVariableName);
                        }
                        else
                        {
                            // For the first loop, generate the interface type
                            if (i == 0)
                            {
                                var variable = field.DeepClone();
                                if (UseLocationLayout)
                                {
                                    var variableTag = this.GetTagLayout(variable);

                                    if (variableTag.Location == null)
                                    {
                                        if (UseSemanticForLocation)
                                        {
                                            variableTag.Location = "S_" + variable.Semantic().Name.Text;
                                        }
                                        else
                                        {
                                            variableTag.Location = location;
                                        }

                                        location++;
                                    }

                                    variableTag.Qualifier.Layouts.Add(new LayoutKeyValue("location", variableTag.Location));

                                    variable.Qualifiers = Qualifier.None;
                                    variable.Qualifiers |= variableTag.Qualifier;
                                }
                                interfaceType.Fields.Add(variable);
                            }

                            src = new MemberReferenceExpression(new IndexerExpression(new VariableReferenceExpression(GSInputName), new LiteralExpression(i)), field.Name);
                        }

                        entryPoint.Body.Statements.Insert(insertPosition, new ExpressionStatement(new AssignmentExpression(AssignmentOperator.Default, dest, src)));
                        insertPosition++;
                    }
                }

                var globalInterfaceType = new Variable(new ArrayType(interfaceType, new EmptyExpression()), GSInputName) { Qualifiers = Ast.ParameterQualifier.In };

                AddGlobalDeclaration(globalInterfaceType);
            }
        }

        private void RemoveTextureAndSamplerDeclarations()
        {
            // Remove all texture declaration and sampler declaration
            //shader.Declarations.RemoveAll(x => (x is Variable) && (((Variable)x).Type is TextureType));
            shader.Declarations.RemoveAll(declarationListToRemove.Contains);

            SearchVisitor.Run(
                shader,
                node =>
                {
                    var variable = node as Variable;
                    if (variable != null)
                    {
                        var variableRef = variable.InitialValue as VariableReferenceExpression;
                        if ((variable.Type is TextureType && samplerMapping.All(x => x.Key.Texture != variable)) ||
                            (variableRef != null && declarationListToRemove.Contains(variableRef.TypeInference.Declaration)))
                        {
                            return null;
                        }
                    }
                    return node;
                });
        }



        /// <summary>
        /// Visits the specified parenthesized expression.
        /// </summary>
        /// <param name="parenthesizedExpression">The parenthesized expression.</param>
        /// <returns>A transformed expression.</returns>
        public override Node Visit(ParenthesizedExpression parenthesizedExpression)
        {
            base.Visit(parenthesizedExpression);

            // Copy back type inference target type to parennthesized expression
            parenthesizedExpression.TypeInference.TargetType = parenthesizedExpression.Content.TypeInference.TargetType;

            // As it is not supported by opengl, return the last element of the list for multiple arguments
            // when cast expression is used with an expression list
            if (parenthesizedExpression.Content is ExpressionList && ParentNode is CastExpression)
            {
                var expressionList = (ExpressionList)parenthesizedExpression.Content;
                return ConvertToSafeExpressionForBinary(expressionList[expressionList.Count - 1]);
            }

            // else return the parenthesized as-is
            return parenthesizedExpression;
        }

        /// <summary>
        /// Splits typedefs declaration when a struct inline is used as the type
        /// </summary>
        private void SplitTypeDefs()
        {
            var newDeclarations = new List<Node>();
            foreach (var declaration in this.shader.Declarations)
            {
                var typedef = declaration as Typedef;
                if (typedef != null && typedef.Type is StructType)
                {
                    var structType = typedef.Type as StructType;
                    if (typedef.Type.Name == null)
                    {
                        typedef.Type.Name = (typedef.IsGroup ? typedef.SubDeclarators[0].Name : typedef.Name) + "_";
                    }
                    newDeclarations.Add(typedef.Type);

                    typedef.Type = new TypeName(typedef.Type.Name) { TypeInference = { Declaration = structType, TargetType = typedef.Type } };

                    if (typedef.IsGroup)
                    {
                        foreach (var typedefDeclarator in typedef.SubDeclarators)
                        {
                            typedefDeclarator.TypeInference.Declaration = structType;
                            typedefDeclarator.TypeInference.TargetType = structType;
                        }
                    }
                    else
                    {
                        typedef.TypeInference.Declaration = structType;
                        typedef.TypeInference.TargetType = structType;
                    }

                    newDeclarations.Add(typedef);
                }
                else
                {
                    newDeclarations.Add(declaration);
                }
            }
            shader.Declarations = newDeclarations;
        }

        /// <summary>
        /// Allocates the new binding.
        /// </summary>
        /// <param name="allocatedRegisters">
        /// The allocated registers.
        /// </param>
        /// <param name="startingIndex">
        /// Index of the starting.
        /// </param>
        /// <param name="sizeOfAllocation">
        /// The size of allocation.
        /// </param>
        private static void AllocateNewBinding(bool[] allocatedRegisters, int startingIndex, int sizeOfAllocation)
        {
            for (int i = 0; i < sizeOfAllocation; i++)
                allocatedRegisters[startingIndex + i] = true;
        }

        /// <summary>
        /// Converts to specified expression to a safe expression for binary operation.
        /// </summary>
        /// <param name="expression">
        /// The expression.
        /// </param>
        /// <returns>
        /// If the expression was a binary expression, then it is embraced by a <see cref="ParenthesizedExpression"/>
        /// </returns>
        private static Expression ConvertToSafeExpressionForBinary(Expression expression)
        {
            if (expression is BinaryExpression || expression is ConditionalExpression)
                return new ParenthesizedExpression(expression);
            return expression;
        }

        /// <summary>
        /// Finds the available binding.
        /// </summary>
        /// <param name="allocatedRegisters">
        /// The allocated registers.
        /// </param>
        /// <param name="startingIndex">
        /// Index of the starting.
        /// </param>
        /// <param name="sizeOfAllocation">
        /// The size of allocation.
        /// </param>
        /// <returns>
        /// The find available binding.
        /// </returns>
        private static int FindAvailableBinding(bool[] allocatedRegisters, int startingIndex, int sizeOfAllocation)
        {
            int newIndex = -1;
            int allocSize = sizeOfAllocation;
            for (int i = startingIndex; i < allocatedRegisters.Length; i++)
            {
                if (allocatedRegisters[i])
                {
                    newIndex = -1;
                    allocSize = sizeOfAllocation;
                }
                else
                {
                    if (newIndex < 0)
                        newIndex = i;
                    allocSize--;
                    if (allocSize == 0)
                        break;
                }
            }

            // Only return selected Index if we were able to allocate sizeOfAllocation
            if (allocSize == 0)
                return newIndex;

            return -1;
        }

        /// <summary>
        /// Determines whether the specified variable is an uniform like.
        /// </summary>
        /// <param name="variable">
        /// The variable.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified variable is an uniform like; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsUniformLike(Variable variable)
        {
            return !variable.Qualifiers.Contains(Ast.ParameterQualifier.InOut) && !variable.Qualifiers.Contains(Ast.ParameterQualifier.In) && !variable.Qualifiers.Contains(Ast.ParameterQualifier.Out)
                   && !variable.Qualifiers.Contains(Ast.Hlsl.StorageQualifier.Static) && !variable.Qualifiers.Contains(Ast.StorageQualifier.Const);
        }

        /// <summary>
        /// Convert semantic string into a (semantic, semanticIndex) pair.
        /// </summary>
        /// <param name="semantic">
        /// The semantic.
        /// </param>
        /// <returns>
        /// A KeyvalueParis semantic -&gt; location
        /// </returns>
        private static KeyValuePair<string, int> ParseSemantic(string semantic)
        {
            // A semantic can have a modifier. We parse it but we don't handle it
            // http://msdn.microsoft.com/en-us/library/bb219850%28v=vs.85%29.aspx
            foreach (var semanticModifier in SemanticModifiers)
            {
                if (semantic.ToLowerInvariant().EndsWith(semanticModifier))
                {
                    // Console.WriteLine("Warning, unsupported semantic modifier [{0}] for semantic [{1}]", semanticModifier, semantic);
                    semantic = semantic.Substring(0, semantic.Length - semanticModifier.Length);
                    break;
                }
            }

            return Semantic.Parse(semantic);
        }

        /// <summary>
        /// Adds the explicit layout.
        /// </summary>
        /// <param name="variable">
        /// The variable.
        /// </param>
        private void AddExplicitLayout(Variable variable)
        {
            var layout = this.GetTagLayout(variable);
            var registerLocation = variable.Qualifiers.Values.OfType<RegisterLocation>().FirstOrDefault();

            if (registerLocation != null && layout.Binding == null)
            {
                int registerIndex;
                var register = registerLocation.Register.Text;

                var allocatedRegister = register.StartsWith("s") ? allocatedRegistersForSamplers : allocatedRegistersForUniforms;
                string registerIndexStr = register[1] != '[' ? register.Substring(1) : register.Substring(2, register.Length - 3);

                if (!int.TryParse(registerIndexStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out registerIndex))
                {
                    parserResult.Error("Invalid layout binding for variable [{0}]", variable.Span, variable);
                    return;
                }

                var size = GetNumberOfFloat4FromVariable(variable.Type);

                var newIndex = FindAvailableBinding(allocatedRegister, registerIndex, size);

                // If this register index was already allocated, try to allocate the new register as an implicit layout
                if (newIndex != registerIndex)
                {
                    parserResult.Warning("Unable to use explicit layout for variable {0} as the location is already used. Use of an implicit layout", variable.Span, variable);
                    AddImplicitLayout(variable);
                }
                else
                {
                    AllocateNewBinding(allocatedRegister, registerIndex, size);
                    layout.Binding = registerIndex;
                    layout.Qualifier.Layouts.Add(new LayoutKeyValue("binding", registerIndex));
                }
            }
        }

        /// <summary>
        /// Adds the explicit layout for a constant buffer.
        /// </summary>
        /// <param name="cBuffer">The variable.</param>
        private void AddExplicitLayout(ConstantBuffer cBuffer)
        {
            // Clear old register
            var register = cBuffer.Register;
            cBuffer.Register = null;

            // If a register was defined
            if (register != null)
            {
                var registerStr = register.Register.Text;

                var layout = this.GetTagLayout(cBuffer, registerStr);

                if (registerStr.StartsWith("b"))
                {
                    int registerIndex;
                    string registerIndexStr = registerStr.Substring(1);

                    if (!int.TryParse(registerIndexStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out registerIndex))
                    {
                        parserResult.Error("Invalid layout binding for Constant Buffer [{0}]", cBuffer.Span, cBuffer);
                        return;
                    }

                    // A third party GLSL compiler requires layout binding to start at 1-15
                    registerIndex++;

                    if (layout.Binding == null)
                        layout.Binding = registerIndex;
                }

                if (layout.Binding != null)
                {
                    cBuffer.Register = new RegisterLocation(string.Empty, layout.Binding.ToString());
                }
            }

            // Add Location layout from packoffset for constant buffers/uniform blocks
            if (UseLocationLayout)
            {
                foreach (var variable in cBuffer.Members.OfType<Variable>())
                {
                    var packOffset = variable.Qualifiers.OfType<PackOffset>().FirstOrDefault();

                    if (packOffset != null)
                    {
                        variable.Qualifiers = Qualifier.None;
                        variable.Qualifiers |= new Ast.Glsl.LayoutQualifier(new LayoutKeyValue("location", packOffset.ToFloat4SlotIndex()));
                    }
                }
            }
        }

        /// <summary>
        /// Adds the global declaration.
        /// </summary>
        /// <typeparam name="T">
        /// Type of the declaration
        /// </typeparam>
        /// <param name="declaration">
        /// The declaration.
        /// </param>
        private void AddGlobalDeclaration<T>(T declaration, bool forceToAdd = false) where T : Node, IDeclaration
        {
            // Don't add glsl variable 
            if (!declaration.Name.Text.StartsWith("gl_") || forceToAdd)
            {
                var index = shader.Declarations.IndexOf(entryPoint);
                shader.Declarations.Insert(index, declaration);
            }

            var topStack = ScopeStack.LastOrDefault();
            if (topStack != null)
                topStack.AddDeclaration(declaration);
        }

        /// <summary>
        /// Adds the implicit layout.
        /// </summary>
        /// <param name="variable">
        /// The variable.
        /// </param>
        private void AddImplicitLayout(Variable variable)
        {
            var registerLocation = variable.Qualifiers.Values.OfType<RegisterLocation>().FirstOrDefault();
            var layout = this.GetTagLayout(variable);

            if (registerLocation == null && layout.Binding == null)
            {
                // Remove any kind of register location
                // if (forceImplicitLayout)
                // variable.Qualifiers.Values.RemoveAll((type) => type is RegisterLocation);
                var allocatedRegister = variable.Type.IsSamplerType() ? allocatedRegistersForSamplers : allocatedRegistersForUniforms;
                var size = GetNumberOfFloat4FromVariable(variable.Type);

                int registerIndex = FindAvailableBinding(allocatedRegister, 0, size);

                // if (layout.Layout.Binding.HasValue)
                // registerIndex = layout.Layout.Binding.Value;
                if (registerIndex < 0)
                    parserResult.Error("Unable to find a free slot for uniform {0}", variable.Span, variable);
                else
                {
                    AllocateNewBinding(allocatedRegister, registerIndex, size);
                    layout.Binding = registerIndex;
                    layout.Qualifier.Layouts.Add(new LayoutKeyValue("binding", registerIndex));
                }
            }
        }

        /// <summary>
        /// Binds the location.
        /// </summary>
        /// <param name="semantic">The semantic.</param>
        /// <param name="typebase">The typebase.</param>
        /// <param name="isInput">if set to <c>true</c> [is input].</param>
        /// <param name="defaultName">The default name.</param>
        /// <param name="location">The location.</param>
        /// <returns>
        /// A variable
        /// </returns>
        private Variable BindLocation(Semantic semantic, TypeBase typebase, bool isInput, string defaultName, ref int location, SourceSpan span)
        {
            var variableFromSemantic = GetVariableFromSemantic(semantic, typebase, isInput, defaultName, span);
            if (!variableFromSemantic.Name.Text.StartsWith("gl_"))
            {
                var variableTag = this.GetTagLayout(variableFromSemantic);

                if (variableTag.Location == null)
                {
                    if (UseSemanticForLocation)
                    {
                        variableTag.Location = "S_" + semantic.Name.Text;
                    }
                    else
                    {
                        variableTag.Location = location;

                        if (InputAttributeNames != null && isInput && (pipelineStage == PipelineStage.Vertex || pipelineStage == PipelineStage.Geometry))
                            InputAttributeNames[location] = semantic.Name.Text;
                    }

                    var matrixType = typebase as MatrixType;
                    if (matrixType != null)
                    {
                        location += 4; // TODO: Pack
                    }
                    else if (typebase is ScalarType || typebase is VectorType)
                    {
                        location++;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }

            return variableFromSemantic;
        }

        /// <summary>
        /// Calculates the GLSL prefix.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <returns>
        /// The prefix of the glsl variable
        /// </returns>
        private string CalculateGlslPrefix(ScalarType type)
        {
            string prefix = string.Empty;
            if (type == ScalarType.Float || type == ScalarType.Half)
                prefix = string.Empty;
            else if (type == ScalarType.Bool)
                prefix = "b";
            else if (type == ScalarType.Int)
                prefix = "i";
            else if (type == ScalarType.UInt)
                prefix = "u";
            else if (type == ScalarType.Double)
                prefix = "d";
            return prefix;
        }

        /// <summary>
        /// Checks a cast method.
        /// </summary>
        /// <param name="methodInvocationExpression">
        /// The method invocation expression.
        /// </param>
        private void CheckCastMethod(MethodInvocationExpression methodInvocationExpression)
        {
            var typeReferenceExpression = methodInvocationExpression.Target as TypeReferenceExpression;
            if (typeReferenceExpression != null)
            {
                // Transform vector to array initializer:
                // float value[4] = float[4](myVectorFloat4); to => float value[4] = float[4](myVectorFloat4[0], myVectorFloat4[1], myVectorFloat4[2], myVectorFloat4[3]);
                var arrayType = typeReferenceExpression.Type as ArrayType;
                if (arrayType != null)
                {
                    if (methodInvocationExpression.Arguments.Count == 1)
                    {
                        var argument = methodInvocationExpression.Arguments[0];
                        methodInvocationExpression.Arguments.Clear();

                        var argType = argument.TypeInference.TargetType;
                        var arrayElementType = arrayType.Type.ResolveType();
                        if (argType is VectorType && arrayElementType is ScalarType && arrayType.Dimensions.Count == 1)
                        {
                            var vectorType = (VectorType)argType;

                            for (int i = 0; i < vectorType.Dimension; i++)
                            {
                                Expression indexerExpression = new IndexerExpression(argument, new LiteralExpression(i)) { TypeInference = { TargetType = arrayElementType } };

                                if (vectorType.Type != arrayElementType)
                                    indexerExpression = new MethodInvocationExpression(new TypeReferenceExpression(arrayElementType), indexerExpression);

                                methodInvocationExpression.Arguments.Add(indexerExpression);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Converts the condition.
        /// </summary>
        /// <param name="expression">
        /// The expression.
        /// </param>
        /// <returns>
        /// A converted expression
        /// </returns>
        private Expression ConvertCondition(Expression expression)
        {
            var expressionType = expression.TypeInference.TargetType;
            // Convert !value, with value not bool to int(value) == 0
            if (expressionType != ScalarType.Bool)
            {
                expression = new MethodInvocationExpression(new TypeReferenceExpression(ScalarType.Bool), expression) { TypeInference = { TargetType = ScalarType.Bool } };
            }
            return expression;
        }


        private Expression CastSemanticToReferenceType(Identifier name, TypeBase semanticType, Variable semanticAsVariable)
        {
            TypeBase glslType = semanticAsVariable.Type;

            var defaultGlslRef = new VariableReferenceExpression(name) { TypeInference = { Declaration = semanticAsVariable, TargetType = glslType } };

            var semanticVectorType = semanticType as VectorType;
            var glslVectorType = glslType as VectorType;
            if (semanticVectorType != null && glslVectorType != null)
            {
                if (semanticVectorType.Dimension < glslVectorType.Dimension)
                {
                    return new MemberReferenceExpression(defaultGlslRef, "xyzw".Substring(0, semanticVectorType.Dimension)) { TypeInference = { TargetType = semanticVectorType } };
                }

                if (semanticVectorType.Dimension > glslVectorType.Dimension)
                {
                    // TODO: Is this case is relevant?
                }
            }

            return defaultGlslRef;
        }

        private Expression ConvertReferenceToSemantics(VariableReferenceExpression varRefExpr, Semantic semantic, TypeBase type, string varName, SourceSpan span)
        {
            // Detect and transform input/output structure member reference.
            // i.e. output.position should get transformed to gl_Position
            if (varRefExpr != null)
            {
                bool isInputOrOutput = false;

                if (IsInEntryPoint && (semantic != null || (type != null && type is StructType)))
                {
                    bool isInput = inputs.Any(x => x.Name == varRefExpr.Name);
                    bool isOutput = outputs.Any(x => x.Name == varRefExpr.Name);

                    isInputOrOutput = isInput || isOutput;

                    // if isInput and not StructType
                    // if isOutput and not structType
                    // if isOutput and structType && not assigntarget
                    if (((isInput || isOutput) && !(type is StructType)) || (isOutput && !isAssignmentTarget))
                    {
                        var variable = GetVariableFromSemantic(semantic, type, isInput, varName, span );
                        Variable newVariable;
                        inputAssignment.TryGetValue(variable, out newVariable);

                        if (isInput && isAssignmentTarget && newVariable == null)
                        {
                            newVariable = new Variable(variable.Type, "local_" + variable.Name.Text, CastSemanticToReferenceType(variable.Name, type, variable));
                            inputAssignment.Add(variable, newVariable);
                            return new VariableReferenceExpression(newVariable);
                        }

                        if (newVariable != null)
                            variable = newVariable;

                        return this.CastSemanticToReferenceType(variable.Name, type, variable);
                    }
                }

                // Some uniforms have semantics attached, so if this is not an input or output variable, this is more likely to be an uniform
                if (!isInputOrOutput)
                {
                    var variable = FindDeclaration(varRefExpr.Name) as Variable;

                    if (variable != null)
                    {
                        Variable newVariable;
                        inputAssignment.TryGetValue(variable, out newVariable);
                        if (isAssignmentTarget && IsUniformLike(variable) && shader.Declarations.Contains(variable) && newVariable == null)
                        {
                            newVariable = new Variable(variable.Type, "local_" + variable.Name.Text, new VariableReferenceExpression(variable.Name) { TypeInference = { TargetType = variable.Type } });
                            inputAssignment.Add(variable, newVariable);
                        }

                        if (newVariable != null)
                            return new VariableReferenceExpression(newVariable.Name) { TypeInference = { TargetType = newVariable.Type.ResolveType() } };
                    }
                }
            }

            return null;
        }

        private void ReturnStruct(StructType structType, Expression returnValueExpression, StatementList statementList)
        {
            var span = returnValueExpression.Span;
            foreach (var fieldRef in GetMembers(structType))
            {
                var field = fieldRef.Field;


                // When a field is an array, we need to properly handle return values for semantics
                var fieldType = field.Type.ResolveType();
                var fieldArrayType = fieldType as ArrayType;

                var semanticVariable = GetVariableFromSemantic(field.Semantic(), fieldType, false, fieldRef.FieldNamePath, span);

                // If this is a special semantic we need to convert each indices
                if (fieldArrayType != null && semanticVariable.Name.Text.StartsWith("gl_"))
                {
                    var arrayDimension = fieldArrayType.Dimensions[0] as LiteralExpression;
                    var arrayValue = arrayDimension != null ? Convert.ChangeType(arrayDimension.Literal.Value, typeof(int)) : null;
                    if (arrayDimension != null && arrayValue != null)
                    {
                        var count = (int)arrayValue;
                        for (int i = 0; i < count; i++)
                        {
                            var semantic = field.Semantic();
                            var newSemantic = new Semantic(semantic.BaseName + i);

                            statementList.Add(
                                new ExpressionStatement(
                                    new AssignmentExpression(
                                        AssignmentOperator.Default,
                                        new VariableReferenceExpression(GetVariableFromSemantic(newSemantic, fieldArrayType, false, fieldRef.FieldNamePath, span).Name),
                                        new IndexerExpression(fieldRef.GetMemberReference(returnValueExpression), new LiteralExpression(i)))) { Span = span });
                        }

                    }
                    else
                    {
                        parserResult.Error("Unable to convert semantic expression [{0}]. Array dimension must be a literal expression", field.Span, field);
                    }
                }
                else
                {
                    var semanticValue = (Expression)fieldRef.GetMemberReference(returnValueExpression);
                    if (fieldType != semanticVariable.Type)
                    {
                        semanticValue = NewCast(semanticVariable.Type, semanticValue);
                    }

                    statementList.Add(
                        new ExpressionStatement(
                            new AssignmentExpression(
                                AssignmentOperator.Default, new VariableReferenceExpression(semanticVariable.Name) { TypeInference = { Declaration = semanticVariable } }, semanticValue)) { Span = returnValueExpression.Span });
                }
            }
        }

        /// <summary>
        /// This helper function will transform a "return X;" statement into a list of assignment for each semantic and a "return;".
        /// </summary>
        /// <param name="returnValueExpression">
        /// The expression.
        /// </param>
        /// <param name="emitReturn">
        /// if set to <c>true</c> [emit return].
        /// </param>
        /// <returns>
        /// A statement used to replace the return statement
        /// </returns>
        private Statement ConvertReturn(Expression returnValueExpression, bool emitReturn, SourceSpan? span)
        {
            var statementList = new StatementList();
            Statement result = statementList;

            StructType structType = null;

            // Handle structure returned by variable
            if (returnValueExpression != null)
            {
                if (!span.HasValue)
                    span = returnValueExpression.Span;

                var varRefExpr = returnValueExpression as VariableReferenceExpression;
                if (varRefExpr != null)
                {
                    var variableDeclarator = varRefExpr.TypeInference.Declaration as Variable;
                    structType = variableDeclarator != null ? variableDeclarator.Type.ResolveType() as StructType : null;

                    if (structType != null)
                    {
                        ReturnStruct(structType, returnValueExpression, statementList);
                    }
                }
                else
                {
                    // Handle structure returned by method invocation
                    var methodRefExp = returnValueExpression as MethodInvocationExpression;
                    if (methodRefExp != null)
                    {
                        var variableDeclarator = methodRefExp.Target.TypeInference.Declaration as MethodDeclaration;
                        structType = variableDeclarator != null ? variableDeclarator.ReturnType.ResolveType() as StructType : null;
                    }

                    if (structType != null)
                    {
                        // Replace plain statement list with a block statement
                        result = new BlockStatement(statementList);

                        statementList.Add(new DeclarationStatement(new Variable(new TypeName(structType.Name), "_local_ret_", returnValueExpression)));

                        var localRet = new VariableReferenceExpression("_local_ret_");

                        ReturnStruct(structType, localRet, statementList);
                    }
                }

                // Note: if we return a struct but the method also has a semantic, it is ignored as the struct members should contain the semantic
                if (structType == null && CurrentFunction.Semantic() != null)
                {
                    var semantic = CurrentFunction.Semantic();
                    statementList.Add(
                        new ExpressionStatement(
                            new AssignmentExpression(
                                AssignmentOperator.Default,
                                new VariableReferenceExpression(GetVariableFromSemantic(semantic, CurrentFunction.ReturnType.ResolveType(), false, null, semantic.Span).Name),
                                returnValueExpression)) { Span = span.Value } );
                }
            }

            // For structure in output, declare a local variable
            foreach (var variable in this.outputs)
            {
                structType = variable.Type.ResolveType() as StructType;
                if (structType != null)
                {
                    // No modifiers for structure inlined in the code
                    variable.Qualifiers = Qualifier.None;
                    ReturnStruct(structType, new VariableReferenceExpression(variable.Name), statementList);
                }
            }

            // Remap Z coordinates
            this.RemapCoordinates(statementList);

            if (emitReturn)
                statementList.Add(new ReturnStatement() { Span = span.HasValue ? span.Value : new SourceSpan() });

            return result;
        }

        /// <summary>
        /// Finds the name of the type by its name.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// A type
        /// </returns>
        private TypeBase FindTypeByName(string name)
        {
            return FindDeclaration(name) as TypeBase;
        }

        /// <summary>
        /// Finds the vertex layout rule by semantic name.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// A <see cref="VariableLayoutRule"/>
        /// </returns>
        private VariableLayoutRule FindVariableLayoutBySemantic(string name)
        {
            VariableLayoutRule rule;
            this.VariableLayouts.TryGetValue(name, out rule);
            return rule;
        }

        /// <summary>
        /// Finds the vertex layout rule by semantic name.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// A <see cref="VariableLayoutRule"/>
        /// </returns>
        private ConstantBufferLayoutRule FindConstantBufferLayoutByRegister(string name)
        {
            ConstantBufferLayoutRule rule;
            this.ConstantBufferLayouts.TryGetValue(name, out rule);
            return rule;
        }


        /// <summary>
        /// Flattens the array creation expression.
        /// </summary>
        /// <param name="from">
        /// The source array that could be composed of inner array creation expression.
        /// </param>
        /// <param name="to">
        /// The destination array that will receive all flattened values
        /// </param>
        private void FlattenArrayCreationExpression(ArrayInitializerExpression from, List<Expression> to)
        {
            foreach (var nextElement in from.Items)
            {
                // Recursive call if there is an array creation expression.
                if (nextElement is ArrayInitializerExpression)
                    FlattenArrayCreationExpression((ArrayInitializerExpression)nextElement, to);
                else
                    to.Add(nextElement);
            }
        }

        private Variable FindGlobalVariableFromExpression(Expression expression)
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
                        return this.FindGlobalVariableFromExpression(variable.InitialValue);
                    }

                    // Is this a global variable?
                    if (shader.Declarations.Contains(variable))
                    {
                        return variable;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the GL sampler associated with a sampler and a texture.
        /// </summary>
        /// <param name="sampler">The sampler.</param>
        /// <param name="texture">The texture.</param>
        /// <param name="forceNullSampler">if set to <c>true</c> [force null sampler] to match.</param>
        /// <returns>
        /// The variable associated with the sampler and the texture
        /// </returns>
        private Expression GetGLSampler(Variable sampler, Variable texture, bool forceNullSampler)
        {
            Variable glslSampler;

            if (sampler == null && !forceNullSampler)
            {
                // Access using only texture, any sampler/texture pair with matching texture is OK (in case forceNullSampler is not set)
                var matchingTextureSampler = samplerMapping.Where(x => x.Key.Texture == texture);
                if (!matchingTextureSampler.Any())
                {
                    return null;
                }

                return new VariableReferenceExpression(matchingTextureSampler.First().Value.Name);
            }

            var samplerKey = new SamplerTextureKey(sampler, texture);
            if (!samplerMapping.TryGetValue(samplerKey, out glslSampler))
            {
                return null;
            }

            if (KeepSamplers)
            {
                if (sampler != null)
                {
                    return new MethodInvocationExpression(new TypeReferenceExpression(glslSampler.Type), new VariableReferenceExpression(texture), new VariableReferenceExpression(sampler));
                }
                else
                {
                    return new MethodInvocationExpression(new TypeReferenceExpression(glslSampler.Type), new VariableReferenceExpression(texture), new VariableReferenceExpression("NoSampler"));
                }
            }

            return new VariableReferenceExpression(glslSampler.Name);
        }

        /// <summary>
        /// Gets the number of float4 from a type.
        /// </summary>
        /// <param name="typeOfVar">
        /// The type.
        /// </param>
        /// <returns>
        /// Number of float4 from a type
        /// </returns>
        private int GetNumberOfFloat4FromVariable(TypeBase typeOfVar)
        {
            var variableType = typeOfVar.ResolveType();

            var matrixType = variableType as MatrixType;
            if (matrixType != null)
                return matrixType.RowCount * matrixType.ColumnCount / 4;

            var arrayType = variableType as ArrayType;
            if (arrayType != null)
            {
                // var dimExpr = arrayType.Dimensions[0] as LiteralExpression;
                var evaluator = new ExpressionEvaluator();
                var result = evaluator.Evaluate(arrayType.Dimensions[0]);

                if (result.HasErrors)
                {
                    result.CopyTo(parserResult);
                }
                else
                {
                    var dimValue = Convert.ToInt32(result.Value);
                    return dimValue * GetNumberOfFloat4FromVariable(arrayType.Type);
                }
            }

            var structType = variableType as StructType;
            if (structType != null)
            {
                int structSize = 0;
                foreach (var variable in structType.Fields)
                {
                    structSize += GetNumberOfFloat4FromVariable(variable.Type.ResolveType());
                }
                return structSize;
            }

            // Else default is 1 float4
            return 1;
        }

        /// <summary>
        /// Gets the variable from semantic.
        /// </summary>
        /// <param name="semantic">
        /// The semantic.
        /// </param>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="isInput">
        /// if set to <c>true</c> [is input].
        /// </param>
        /// <param name="varName">
        /// Name of the var.
        /// </param>
        /// <returns>
        /// The variable associated with a semantic
        /// </returns>
        private Variable GetVariableFromSemantic(Semantic semantic, TypeBase type, bool isInput, string varName, SourceSpan span)
        {
            type = type.ResolveType();

            bool isArray = type is ArrayType;
            TypeBase elementType = isArray ? ((ArrayType)type).Type.ResolveType() : type;

            TypeBase defaultType = type;
            int semanticIndex = 0;

            var glSemantic = semantic;

            if (glSemantic != null)
            {
                glSemantic = ResolveSemantic(semantic, type, isInput, varName, out defaultType, out semanticIndex, span);
            }
            else
            {
                span = type.Span;
            }

            string variableName = glSemantic == null ? varName : glSemantic.Name.Text;

            bool addGlslGlobalVariable = CultureInfo.InvariantCulture.CompareInfo.Compare(variableName, "gl_Position", CompareOptions.IgnoreCase) == 0 && defaultType != type;

            bool isFragData = CultureInfo.InvariantCulture.CompareInfo.IsPrefix(variableName, "gl_fragdata", CompareOptions.IgnoreCase);
            if (isFragData && needCustomFragData)
            {
                // IF varName is null, this is a semantic from a returned function, so use a generic out_gl_fragdata name
                // otherwise, use the original variable name.
                variableName = varName != null ? "out_gl_fragdata_" + varName : "out_gl_fragdata";
                addGlslGlobalVariable = true;
            }

            // var firstContext = DeclarationContexts.First();
            var variable = FindDeclaration(variableName) as Variable;
            if (variable == null)
            {
                variable = new Variable(defaultType, variableName) { Span = span };

                if (addGlslGlobalVariable)
                {
                    variable.Type = type;
                    if (isInput)
                    {
                        variable.Qualifiers |= Ast.ParameterQualifier.In;
                    }
                    else
                    {
                        if (isFragData && needCustomFragData)
                        {
                            // Write location on outputs in case of MRT
                            variable.Qualifiers |= new LayoutQualifier { Layouts = { new LayoutKeyValue("location", semanticIndex) } };
                        }

                        variable.Qualifiers |= Ast.ParameterQualifier.Out;
                    }
                }

                AddGlobalDeclaration(variable, addGlslGlobalVariable);
            }

            return variable;
        }

        /// <summary>
        /// Gets the variable tag.
        /// </summary>
        /// <param name="node">The variable.</param>
        /// <param name="alias">The alias name (semantic or register).</param>
        /// <returns>
        /// The tag associated with a variable
        /// </returns>
        private TagLayout GetTagLayout(Node node, string alias = null)
        {
            var variable = node as Variable;
            var constantBuffer = node as ConstantBuffer;

            var layoutTag = node.GetTag(TagLayoutKey) as TagLayout;
            if (layoutTag == null)
            {
                layoutTag = new TagLayout();
                node.SetTag(TagLayoutKey, layoutTag);

                if (variable != null)
                {
                    MapRule mapType;
                    if (MapRules.TryGetValue(variable.Name, out mapType))
                    {
                        layoutTag.Type = mapType.Type;
                    }
                }

                // Only for vertex shader input
                if (alias != null)
                {
                    if (variable != null)
                    {
                        var variableLayoutRule = this.FindVariableLayoutBySemantic(alias);
                        if (variableLayoutRule != null)
                        {
                            // Update location from external layout
                            if (variableLayoutRule.Location != null)
                            {
                                int locationIndex;
                                if (int.TryParse(variableLayoutRule.Location, out locationIndex))
                                {
                                    layoutTag.Location = locationIndex;

                                    if (InputAttributeNames != null)
                                        InputAttributeNames[locationIndex] = alias;
                                }
                                else
                                {
                                    layoutTag.Location = variableLayoutRule.Location;
                                }
                            }

                            // Use output or input name
                            layoutTag.Name = variable.Qualifiers.Contains(Ast.ParameterQualifier.Out) ? variableLayoutRule.NameOutput : variableLayoutRule.Name;                           
                        }
                    }
                    else if (constantBuffer != null)
                    {
                        var cBufferLayoutRule = this.FindConstantBufferLayoutByRegister(alias);
                        if (cBufferLayoutRule != null)
                        {
                            if (cBufferLayoutRule.Binding != null)
                            {
                                int bindingIndex;
                                if (int.TryParse(cBufferLayoutRule.Binding, out bindingIndex))
                                {
                                    layoutTag.Binding = bindingIndex;
                                }
                                else
                                {
                                    layoutTag.Binding = cBufferLayoutRule.Binding;
                                }
                            }
                        }
                    }
                }

                if (variable != null)
                {
                    
                }

                if (variable != null && layoutTag.Name == null)
                    layoutTag.Name = variable.Name.Text;

                layoutTag.Qualifier = new Ast.Glsl.LayoutQualifier();
            }

            return layoutTag;
        }

        /// <summary>
        /// Rebinds all VariableReferenceExpression to the final name.
        /// </summary>
        private void RebindVariableReferenceExpressions()
        {
            SearchVisitor.Run(
                shader, 
                node =>
                    {
                        if (node is VariableReferenceExpression)
                        {
                            var variableRef = (VariableReferenceExpression)node;
                            if (variableRef.TypeInference.Declaration is Variable)
                                variableRef.Name = variableRef.TypeInference.Declaration.Name;
                        }
                        else if (node is MethodInvocationExpression)
                        {
                            var methodRef = (MethodInvocationExpression)node;
                            var variableRef = methodRef.Target as VariableReferenceExpression;
                            var methodDeclaration = variableRef != null ? variableRef.TypeInference.Declaration as MethodDeclaration : null;
                            if (variableRef != null && methodDeclaration != null && !methodDeclaration.IsBuiltin)
                                variableRef.Name = methodDeclaration.Name;
                        }

                        return node;
                    });
        }

        /// <summary>
        /// Removes the default parameters for methods.
        /// </summary>
        private void RemoveDefaultParametersForMethods()
        {
            SearchVisitor.Run(
                shader, 
                node =>
                    {
                        var declaration = node as Parameter;
                        if (declaration != null && declaration.InitialValue != null)
                            declaration.InitialValue = null;
                        return node;
                    });
        }

        private static string RenameGlslKeyword(string name)
        {
            // Note: we try to avoid ending up with a _, since glsl_optimizer might add another _X, and GLSL doesn't like usage of double underscore
            if (GlslKeywords.IsReserved(name))
                name = "_" + name;

            // Replace all variable using __ with _0
            return name.Replace("__", "_0");
        }

        /// <summary>
        /// If requested, Z projection coordinates will be remapped from [0;1] to [-1;1] at end of vertex shader.
        /// </summary>
        private void RemapCoordinates(StatementList list)
        {
            if (pipelineStage == PipelineStage.Vertex && (entryPoint != null))
            {
                if (ViewFrustumRemap)
                {
                    // Add gl_Position.z = gl_Position.z * 2.0f - gl_Position.w
                    list.Add(
                        new ExpressionStatement(
                            new AssignmentExpression(
                                AssignmentOperator.Default,
                                new MemberReferenceExpression(new VariableReferenceExpression("gl_Position"), "z"),
                                new BinaryExpression(
                                    BinaryOperator.Minus,
                                    new BinaryExpression(
                                        BinaryOperator.Multiply,
                                        new MemberReferenceExpression(new VariableReferenceExpression("gl_Position"), "z"),
                                        new LiteralExpression(2.0f)),
                                    new MemberReferenceExpression(new VariableReferenceExpression("gl_Position"), "w"))
                                )));
                }

                if (FlipRenderTarget)
                {
                    // Add gl_Position.y = -gl_Position.y
                    list.Add(
                        new ExpressionStatement(
                            new AssignmentExpression(
                                AssignmentOperator.Default,
                                new MemberReferenceExpression(new VariableReferenceExpression("gl_Position"), "y"),
                                new UnaryExpression(
                                    UnaryOperator.Minus,
                                    new MemberReferenceExpression(new VariableReferenceExpression("gl_Position"), "y"))
                                )));
                }
            }
        }

        /// <summary>
        /// Renames all declaration that are using a GLSL keywords.
        /// </summary>
        private void RenameGlslKeywords()
        {
            SearchVisitor.Run(
                shader, 
                node =>
                    {
                        var declaration = node as IDeclaration;
                        var variableRef = node as VariableReferenceExpression;
                        if (declaration != null && declaration.Name != null)
                        {
                            if (!(declaration is Variable && ((Variable)declaration).Type.Name.Text.StartsWith("layout")))
                            {
                                declaration.Name.Text = RenameGlslKeyword(declaration.Name.Text);
                            }
                        }
                        else if (variableRef != null)
                        {
                            variableRef.Name.Text = RenameGlslKeyword(variableRef.Name.Text);
                        }

                        return node;
                    });
        }

        private KeyValuePair<string, int> GetGlVariableFromSemantic(Semantic rawSemantic, bool isInput, out string semanticGl, out string semanticGlBase, out int semanticIndex)
        {
            var semanticName = rawSemantic.Name.Text;
            var semantic = ParseSemantic(semanticName);

            var semanticMapping = isInput ? builtinInputs : builtinOutputs;

            semanticGl = null;
            
            if (semanticMapping != null && !semanticMapping.TryGetValue(semanticName, out semanticGl))
                semanticMapping.TryGetValue(semantic.Key, out semanticGl);

            // Special case for point sprite
            if (semanticName != null && semanticName.Equals("TEXCOORD0") && IsPointSpriteShader && pipelineStage == PipelineStage.Pixel)
                semanticGl = "gl_PointCoord";

            // Set the semantic gl base name
            semanticGlBase = semanticGl;
            semanticIndex = semantic.Value;

            if (semanticGl != null && semanticGl.EndsWith("[]"))
            {
                semanticGlBase = semanticGl.Substring(0, semanticGl.Length - 2);

                // If there is [] at the end of the string, insert semantic index within []
                semanticGl = semanticGlBase + "[" + semantic.Value + "]";
            }

            return semantic;
        }

        private string GetGlVariableNameFromSemantic(Semantic rawSemantic, bool isInput)
        {
            string semanticGlBase = null;
            string semanticGl = null;
            int semanticIndex;
            GetGlVariableFromSemantic(rawSemantic, isInput, out semanticGl, out semanticGlBase, out semanticIndex);

            return semanticGl;
        }

        /// <summary>
        /// Resolves the HLSL semantic into GLSL one for a given uniform.
        /// It will also adds the varying to the GLSL shader the first time it is found.
        /// </summary>
        /// <param name="rawSemantic">The raw semantic.</param>
        /// <param name="type">The type.</param>
        /// <param name="isInput">if set to <c>true</c> input, otherwise output.</param>
        /// <param name="varName">Name of the var.</param>
        /// <returns>A semantic transformed</returns>
        private Semantic ResolveSemantic(Semantic rawSemantic, TypeBase type, bool isInput, string varName, out TypeBase glslType, out int semanticIndex, SourceSpan span)
        {
            string semanticGlBase = null;
            string semanticGl = null;
            var  semantic = GetGlVariableFromSemantic(rawSemantic, isInput, out semanticGl, out semanticGlBase, out semanticIndex);

            if (semanticGl == null)
            {
                // Prefix with a_ or v_ ( attribute or varying )
                if (isInput && (pipelineStage == PipelineStage.Vertex))
                    semanticGl = "a_" + (this.UseSemanticForVariable ? semantic.Key + semantic.Value : varName);
                else if ((isInput == false) && (pipelineStage == PipelineStage.Pixel))
                    semanticGl = "vout_" + (this.UseSemanticForVariable ? semantic.Key + semantic.Value : varName);
                else
                    semanticGl = "v_" + (this.UseSemanticForVariable ? semantic.Key + semantic.Value : varName);

                var variable = FindDeclaration(semanticGl) as Variable;
                if (variable == null)
                {
                    variable = new Variable(type, semanticGl) { Span = span };

                    // int must be "flat" between stages (everywhere except VS input)
                    if (!(isInput == true && pipelineStage == PipelineStage.Vertex))
                    {
                        var baseType = TypeBase.GetBaseType(variable.Type);
                        // Note: not sure why, but it seems scalar are not properly resolved?
                        if (baseType.Name == ScalarType.Int.Name || baseType.Name == ScalarType.UInt.Name)
                        {
                            variable.Qualifiers |= Ast.ParameterQualifier.Flat;
                        }
                    }

                    variable.Qualifiers |= isInput ? Ast.ParameterQualifier.In : Ast.ParameterQualifier.Out;

                    // Setup Variable Tag for LayoutQualifiers
                    //GetTagLayout(variable, semanticName, isInput && pipelineStage == PipelineStage.Vertex);
                    this.GetTagLayout(variable, rawSemantic.Name.Text);

                    AddGlobalDeclaration(variable);
                }
                glslType = type;
            }
            else
            {
                if (builtinGlslTypes.TryGetValue(semanticGlBase, out glslType))
                {
                    // If type is an array type, create the equivalent arrayType
                    var arrayType = type as ArrayType;
                    if (arrayType != null)
                        glslType = new ArrayType(glslType, arrayType.Dimensions.ToArray());
                }
                else
                {
                    parserResult.Warning("No default type defined for glsl semantic [{0}]. Use [{1}] implicit type instead.", rawSemantic.Span, semanticGlBase, type);
                    glslType = type;                    
                }
            }

            return new Semantic(semanticGl);
        }

        /// <summary>
        /// Tranforms to GLSL types.
        /// </summary>
        private void TranformToGlslTypes()
        {
            var mapToGlsl = new Dictionary<TypeBase, TypeBase>();

            // Add vector type conversion
            foreach (var type in new[] { ScalarType.Bool, ScalarType.Int, ScalarType.UInt, ScalarType.Float, ScalarType.Double, ScalarType.Half })
            {
                var targetSubType = type == ScalarType.Half ? ScalarType.Float : type;
                var prefix = CalculateGlslPrefix(type);

                for (int i = 2; i <= 4; ++i)
                    mapToGlsl.Add(new VectorType(type, i), new TypeName(prefix + "vec" + i));

                // Half are converted to float
                mapToGlsl.Add(new VectorType(type, 1), targetSubType);
            }

            // Add matrix type conversion
            foreach (var type in new[] { ScalarType.Double, ScalarType.Float, ScalarType.Half })
            {
                var prefix = CalculateGlslPrefix(type);
                for (int i = 2; i <= 4; ++i)
                {
                    for (int j = 2; j <= 4; ++j)
                    {
                        // Swap column/matrix if NoSwapForBinaryMatrixOperation is true
                        int column = NoSwapForBinaryMatrixOperation ? j : i;
                        int row = NoSwapForBinaryMatrixOperation ? i : j;

                        string matrixName = i == j ? string.Format(prefix + "mat{0}", i) : string.Format(prefix + "mat{0}x{1}", column, row);

                        mapToGlsl.Add(new MatrixType(type, i, j), new TypeName(matrixName));
                    }
                }
            }

            mapToGlsl.Add(ScalarType.Half, ScalarType.Float);
            mapToGlsl.Add(new MatrixType(ScalarType.Float, 1, 1), ScalarType.Float);

            // Sampler objects
            mapToGlsl.Add(StateType.SamplerState, new TypeName("sampler"));
            mapToGlsl.Add(StateType.SamplerComparisonState, new TypeName("samplerShadow"));
            //mapToGlsl.Add(SamplerStateType.SamplerComparisonState, new TypeName("sampler"));

            // Texture objects
            //mapToGlsl.Add(TextureType.Texture, new TextureType("texture2D"));
            //mapToGlsl.Add(TextureType.Texture1D, new TextureType("texture1D"));
            //mapToGlsl.Add(TextureType.Texture2D, new TextureType("texture2D"));
            //mapToGlsl.Add(TextureType.Texture3D, new TextureType("texture3D"));
            //mapToGlsl.Add(TextureType.TextureCube, new TextureType("textureCube"));

            // Replace all generic shader types to their glsl equivalent.
            SearchVisitor.Run(
                shader, 
                node =>
                    {
                        if (node is TypeBase && !(node is Typedef) && !(node is ArrayType))
                        {
                            var type = (TypeBase)node;
                            var targetType = type.ResolveType();

                            TypeBase outputType;
                            if (mapToGlsl.TryGetValue(targetType, out outputType))
                                return outputType;
                            if (mapToGlsl.TryGetValue(type, out outputType))
                                return outputType;

                            outputType = ConvertType(targetType);
                            if (outputType != null)
                                return outputType;
                        }

                        return node;
                    });
        }

        private TypeBase ConvertType(TypeBase targetType)
        {
            var targetTypeName = targetType.Name.Text;

            if (targetTypeName.StartsWith("Texture"))
                targetTypeName = "texture" + targetTypeName.Substring("Texture".Length);
            else if (targetTypeName.StartsWith("Buffer"))
                targetTypeName = "textureBuffer";
            else return null;

            // TODO: How do we support this on OpenGL ES 2.0? Cast to int/uint on Load()/Sample()?
            var genericSamplerType = targetType as IGenerics;
            if (genericSamplerType != null && genericSamplerType.GenericArguments.Count == 1)
            {
                var genericArgument = genericSamplerType.GenericArguments[0].ResolveType();
                if (TypeBase.GetBaseType(genericArgument) == ScalarType.UInt)
                    targetTypeName = "u" + targetTypeName;
                else if (TypeBase.GetBaseType(genericArgument) == ScalarType.Int)
                    targetTypeName = "i" + targetTypeName;
            }

            //// Handle comparison samplers
            //if (needsComparison)
            //    targetTypeName += "Shadow";

            return new TypeName(targetTypeName);
        }

        /// <summary>
        /// Transforms all variable declared with a multidimensional array to a single dimension.
        /// </summary>
        private void TransformArrayDimensions()
        {
            foreach (var variable in listOfMultidimensionArrayVariable)
            {
                Expression newSubscript = null;

                var arrayType = (ArrayType)variable.Type.ResolveType();
                var arrayElementType = arrayType.Type.ResolveType();

                // float myarray[i1][i2][i3]...[in] => float myarray[(i1)*(i2)*(i3)*...*(in)]
                foreach (var subscript in arrayType.Dimensions)
                    newSubscript = newSubscript == null ? subscript : new BinaryExpression(BinaryOperator.Multiply, newSubscript, subscript);

                // Set the new subscript
                var newArrayType = new ArrayType { Type = new TypeName(arrayElementType) };
                newArrayType.Dimensions.Add(newSubscript);
                variable.Type = newArrayType;
            }

            // For arrays with dynamic dimension, we need to replace it with the actual number of initializers
            foreach (var variable in shader.Declarations.OfType<Variable>())
            {
                var variableArrayType = variable.Type.ResolveType() as ArrayType;
                if (variableArrayType != null)
                {
                    if (variable.InitialValue != null)
                    {
                        var arrayInitializers = variable.InitialValue as MethodInvocationExpression;
                        if (arrayInitializers != null && variableArrayType.Dimensions.Count == 1)
                        {
                            var currentDimension = variableArrayType.Dimensions[0] as LiteralExpression;
                            if (currentDimension != null)
                            {
                                int currentDim = Convert.ToInt32(currentDimension.Literal.Value);
                                if (currentDim < arrayInitializers.Arguments.Count)
                                {
                                    variableArrayType.Dimensions.Clear();
                                    variableArrayType.Dimensions.Add(new LiteralExpression(arrayInitializers.Arguments.Count));
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Converts an HLSL array initializer to a GLSL array initializer. 
        /// Example: 
        /// HLSL float[4] test = {1,2,3,4}; 
        /// GLSL float[4] test = float[](1,2,3,4);
        /// </summary>
        /// <param name="arrayType">Type of the array.</param>
        /// <param name="arrayInitializer">The array initializer.</param>
        /// <returns>A converted array</returns>
        private Expression ConvertArrayInitializer(ArrayType arrayType, ArrayInitializerExpression arrayInitializer)
        {
            var arrayElementType = arrayType.Type.ResolveType();

            var newArrayExpression = new MethodInvocationExpression(new IndexerExpression(new TypeReferenceExpression(arrayType.Type), new LiteralExpression()));
            var arrayItems = new List<Expression>();
            FlattenArrayCreationExpression(arrayInitializer, arrayItems);

            // By default add array
            newArrayExpression.Arguments.AddRange(arrayItems);

            // Convert array/vector initializer
            // vec4 value[4] = vec4[4](0,0,0,0, 0,0,0,0, 0,0,0,0); to => vec4 value[4] = vec4[4](vec4(0,0,0,0), vec4(0,0,0,0), vec4(0,0,0,0));
            var vectorType = arrayElementType as VectorType;
            if (vectorType != null)
            {
                // If there is exactly the same number of arguments and all arguments are primitives, then convert
                if ((arrayItems.Count % vectorType.Dimension) == 0 && arrayItems.All(arg => (arg.TypeInference.TargetType is ScalarType)))
                {
                    var arguments = new List<Expression>();
                    var vectorArgs = new List<Expression>();
                    foreach (var arg in arrayItems)
                    {
                        vectorArgs.Add(arg);
                        if (vectorArgs.Count == vectorType.Dimension)
                        {
                            arguments.Add(new MethodInvocationExpression(new TypeReferenceExpression(vectorType), vectorArgs.ToArray()));
                            vectorArgs.Clear();
                        }
                    }

                    newArrayExpression.Arguments = arguments;
                }
            }

            if (arrayType.IsDimensionEmpty)
            {
                arrayType.Dimensions.Clear();
                arrayType.Dimensions.Add(new LiteralExpression(newArrayExpression.Arguments.Count));
            }

            return newArrayExpression;
        }

        /// <summary>
        /// Converts an HLSL matrix initializer to a GLSL matrix initializer.
        /// </summary>
        /// <param name="matrixType">Type of the matrix.</param>
        /// <param name="initializers">The initializers.</param>
        /// <returns>
        /// A converted matrix
        /// </returns>
        private List<Expression> ConvertMatrixInitializer(MatrixType matrixType, List<Expression> initializers)
        {
            if (!NoSwapForBinaryMatrixOperation)
                return initializers;

            var newInitializers = new List<Expression>();

            int columnCount = matrixType.ColumnCount;
            int rowCount = matrixType.RowCount;

            // Initializers could be
            // 1) matrix test = matrix( float4(row1), float4(row2), float4(row3), float4(row4) );
            // or 
            // 2) matrix test = matrix( 1,2,3,4, 5,6,7,8, 9,10,11,12, 13,14,15,16);
            if (rowCount == initializers.Count)
            {
                // Case 1)
                // We need to transpose rows into columns
                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    var columnInitializerType = new VectorType((ScalarType)matrixType.Type.ResolveType(), rowCount);
                    var columnInitializer = new MethodInvocationExpression(new TypeReferenceExpression(columnInitializerType));
                    newInitializers.Add(columnInitializer);

                    for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                    {
                        var elementIndexer = new IndexerExpression(initializers[rowIndex], new LiteralExpression(columnIndex));
                        columnInitializer.Arguments.Add(elementIndexer);
                    }
                }
            }
            else if ((rowCount * columnCount) == initializers.Count)
            {
                // Case 2)
                // We need to transpose all the elements
                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                    {
                        newInitializers.Add(initializers[columnCount * rowIndex + columnIndex]);
                    }
                }
            }
            else
            {
                newInitializers = initializers;
                parserResult.Warning("Unable to convert matrix initializer [{0}] to matrix type [{1}]", matrixType.Span, string.Join(",", initializers), matrixType);
            }

            return newInitializers;
        }

        /// <summary>
        /// Transforms the global multiple variable to single variable.
        /// </summary>
        private void TransformGlobalMultipleVariableToSingleVariable()
        {
            var declarations = new List<Node>();

            foreach (var declaration in shader.Declarations)
            {
                var variable = declaration as Variable;
                if (variable != null && variable.IsGroup)
                {
                    foreach (var subVariable in variable.SubVariables)
                    {
                        // Copy Qualifiers
                        subVariable.Qualifiers = Qualifier.None;
                        subVariable.Qualifiers |= variable.Qualifiers;
                        declarations.Add(subVariable);
                    }
                }
                else
                    declarations.Add(declaration);
            }

            shader.Declarations = declarations;
        }


        private static List<StructMemberReference> GetMembers(StructType structType, List<StructMemberReference> members = null, List<Variable> fieldStack = null )
        {
            // Cache the members if they have been already calculated for a particular type
            // Though, this is not realy efficient (should cache nested struct member reference...)
            if (members == null)
            {
                members = (List<StructMemberReference>)structType.GetTag("Members");
                if (members != null)
                    return members;

                members = new List<StructMemberReference>();
                structType.SetTag("Members", members);
            }

            if (fieldStack == null)
                fieldStack = new List<Variable>();

            // Iterate on all fields to build the member references
            foreach (var field in structType.Fields.SelectMany(item => item.Instances()))
            {
                var fieldType = field.Type.ResolveType();

                // This is a "recursive" struct type
                if (fieldType is StructType)
                {
                    fieldStack.Add(field);
                    GetMembers((StructType)fieldType, members, fieldStack);
                    fieldStack.RemoveAt(fieldStack.Count - 1);
                }
                else
                {
                    var structMember = new StructMemberReference();
                    structMember.Field = field;
                    structMember.ParentFields.AddRange(fieldStack);

                    var fieldPath = new StringBuilder();
                    bool isFirst = true;
                    foreach(var parentField in Enumerable.Reverse(fieldStack))
                    {
                        if (!isFirst)
                            fieldPath.Append("_");
                        fieldPath.Append(parentField.Name);
                        isFirst = false;
                    }

                    if (fieldPath.Length > 0)
                        fieldPath.Append("_");
                    fieldPath.Append(field.Name);

                    structMember.FieldNamePath = fieldPath.ToString();
                    members.Add(structMember);
                }
            }

            return members;
        }

        private static Expression NewCast(TypeBase type, Expression expression)
        {
            if (type != expression.TypeInference.TargetType)
            {
                var result = new MethodInvocationExpression(new TypeReferenceExpression(type), expression);
                result.TypeInference.TargetType = type;
                return result;
            }

            return expression;
        }

        private void ReorderVariableQualifiers()
        {
            foreach (var variable in shader.Declarations.OfType<Variable>())
            {
                variable.Qualifiers.Values.Sort(QualifierComparer.Default);
            }
        }

        private void ApplyStd140Layout()
        {
            foreach (var constantBuffer in shader.Declarations.OfType<ConstantBuffer>())
            {
                var layoutQualifier = constantBuffer.Qualifiers.OfType<Xenko.Core.Shaders.Ast.Glsl.LayoutQualifier>().FirstOrDefault();
                if (layoutQualifier == null)
                {
                    layoutQualifier = new Xenko.Core.Shaders.Ast.Glsl.LayoutQualifier();
                    constantBuffer.Qualifiers |= layoutQualifier;
                }
                layoutQualifier.Layouts.Add(new LayoutKeyValue("std140"));
            }
        }

        private void FixupVaryingES2()
        {
            foreach (var variable in shader.Declarations.OfType<Variable>())
            {
                if (variable.Qualifiers.Contains(ParameterQualifier.In))
                {
                    variable.Qualifiers.Values.Remove(ParameterQualifier.In);
                    // "in" becomes "attribute" in VS, "varying" in other stages
                    variable.Qualifiers.Values.Add(
                        pipelineStage == PipelineStage.Vertex
                            ? global::Xenko.Core.Shaders.Ast.Glsl.ParameterQualifier.Attribute
                            : global::Xenko.Core.Shaders.Ast.Glsl.ParameterQualifier.Varying);
                }
                if (variable.Qualifiers.Contains(ParameterQualifier.Out))
                {
                    variable.Qualifiers.Values.Remove(ParameterQualifier.Out);
                    variable.Qualifiers.Values.Add(global::Xenko.Core.Shaders.Ast.Glsl.ParameterQualifier.Varying);
                }
            }
        }

        private class StructMemberReference
        {
            public string FieldNamePath;

            public MemberReferenceExpression GetMemberReference(Expression target)
            {
                var currentMemberRef = new MemberReferenceExpression(target, Field.Name);

                foreach (var parentField in Enumerable.Reverse(ParentFields))
                {
                    currentMemberRef.Target  = new MemberReferenceExpression(currentMemberRef.Target, parentField.Name);
                }
                return currentMemberRef;
            }

            public List<Variable> ParentFields = new List<Variable>();

            public Variable Field;
        }

        class SemanticReference
        {
            public SemanticReference(string name, Expression variableReference)
            {
                this.Name = name;
                this.VariableReference = variableReference;
            }

            public string Name;

            public Expression VariableReference;
        }

        #endregion

        /// <summary>
        /// A Tag associated to a variable
        /// </summary>
        private class TagLayout
        {
            #region Constants and Fields

            public object Binding { get; set; }

            public object Location { get; set; }

            public string Name;

            public string Type;

            public Ast.Glsl.LayoutQualifier Qualifier;

            #endregion
        }

        /// <summary>
        /// Sort qualifiers: layout(xx) first, then others (out, int, etc...)
        /// </summary>
        class QualifierComparer : IComparer<CompositeEnum>
        {
            public static readonly QualifierComparer Default = new QualifierComparer();

            public int Compare(CompositeEnum x, CompositeEnum y)
            {
                int xOrder = x is LayoutQualifier ? 0 : 1;
                int yOrder = y is LayoutQualifier ? 0 : 1;

                return xOrder.CompareTo(yOrder);
            }
        }
    }
}
