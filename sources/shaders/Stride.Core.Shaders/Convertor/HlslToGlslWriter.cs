// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Linq;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Glsl;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Writer.Hlsl;
using InterfaceType = Stride.Core.Shaders.Ast.Hlsl.InterfaceType;
using LayoutQualifier = Stride.Core.Shaders.Ast.Glsl.LayoutQualifier;

namespace Stride.Core.Shaders.Convertor
{
    /// <summary>
    /// A writer for a shader.
    /// </summary>
    public class HlslToGlslWriter : HlslWriter
    {
        private readonly GlslShaderPlatform shaderPlatform;
        private readonly int shaderVersion;
        private readonly PipelineStage pipelineStage;

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HlslWriter"/> class. 
        /// </summary>
        /// <param name="useNodeStack">
        /// if set to <c>true</c> [use node stack].
        /// </param>
        public HlslToGlslWriter(GlslShaderPlatform shaderPlatform, int shaderVersion, PipelineStage pipelineStage, bool useNodeStack = false)
            : base(useNodeStack)
        {
            this.shaderPlatform = shaderPlatform;
            this.shaderVersion = shaderVersion;
            this.pipelineStage = pipelineStage;

            if (shaderPlatform == GlslShaderPlatform.OpenGLES)
            {
                TrimFloatSuffix = true;

                GenerateUniformBlocks = shaderVersion >= 300;
                SupportsTextureBuffer = shaderVersion >= 320;
            }
        }

        #endregion

        public bool GenerateUniformBlocks { get; set; } = true;

        public bool TrimFloatSuffix { get; set; } = false;

        public bool SupportsTextureBuffer { get; set; } = true;

        public string ExtraHeaders { get; set; }

        #region Public Methods

        /// <inheritdoc/>
        public override void Visit(Shader shader)
        {
            // #version
            Write("#version ");
            Write(shaderVersion.ToString());

            // ES3+ expects "es" at the end of #version
            if (shaderPlatform == GlslShaderPlatform.OpenGLES && shaderVersion >= 300)
                Write(" es");

            WriteLine();
            WriteLine();

            if (shaderPlatform == GlslShaderPlatform.OpenGLES)
            {
                WriteLine("precision highp float;");
                WriteLine("precision highp int;");

                if (shaderVersion >= 300)
                {
                    WriteLine("precision lowp sampler3D;");
                    WriteLine("precision lowp samplerCubeShadow;");
                    WriteLine("precision lowp sampler2DShadow;");
                    WriteLine("precision lowp sampler2DArray;");
                    WriteLine("precision lowp sampler2DArrayShadow;");
                    WriteLine("precision lowp isampler2D;");
                    WriteLine("precision lowp isampler3D;");
                    WriteLine("precision lowp isamplerCube;");
                    WriteLine("precision lowp isampler2DArray;");
                    WriteLine("precision lowp usampler2D;");
                    WriteLine("precision lowp usampler3D;");
                    WriteLine("precision lowp usamplerCube;");
                    WriteLine("precision lowp usampler2DArray;");
                }

                if (shaderVersion >= 320 || SupportsTextureBuffer)
                {
                    WriteLine("precision lowp samplerBuffer;");
                    WriteLine("precision lowp isamplerBuffer;");
                    WriteLine("precision lowp usamplerBuffer;");
                }

                WriteLine();

                if (shaderVersion < 320 && SupportsTextureBuffer)
                {
                    // In ES 3.1 and previous, we use texelFetchBuffer in case it needs to be remapped into something else by user
                    WriteLine("#define texelFetchBuffer(sampler, P) texelFetch(sampler, P)");
                }
            }

            if (ExtraHeaders != null)
                WriteLine(ExtraHeaders);

            if (shader == null)
            {
                // null entry point for pixel shader means no pixel shader. In that case, we return a default function.
                // TODO: support that directly in HlslToGlslConvertor?
                if (pipelineStage == PipelineStage.Pixel)
                {
                    WriteLine("out float fragmentdepth; void main(){ fragmentdepth = gl_FragCoord.z; }");
                }
                else
                {
                    throw new NotSupportedException($"Can't output empty {pipelineStage} shader for platform {shaderPlatform} version {shaderVersion}.");
                }
            }
            else
            {
                base.Visit(shader);
            }
        }

        /// <inheritdoc/>
        public override void Visit(Literal literal)
        {
            if (TrimFloatSuffix && literal.Value is float)
                literal.Text = literal.Text.Trim('f', 'F', 'l', 'L');

            base.Visit(literal);
        }

        /// <inheritdoc />
        public override void Visit(Ast.Glsl.InterfaceType interfaceType)
        {
            Write(interfaceType.Qualifiers, true);

            Write(" ");
            Write(interfaceType.Name);
            WriteSpace();

            // Post Attributes
            Write(interfaceType.Attributes, false);

            OpenBrace();

            foreach (var variableDeclaration in interfaceType.Fields)
                VisitDynamic(variableDeclaration);

            CloseBrace(false);

            if (IsDeclaratingVariable.Count == 0 || !IsDeclaratingVariable.Peek())
            {
                Write(";").WriteLine();
            }
        }

        /// <inheritdoc/>
        public override void Visit(Ast.Hlsl.Annotations annotations)
        {
        }

        /// <inheritdoc/>
        public override void Visit(ClassType classType)
        {
        }

        /// <inheritdoc/>
        public override void Visit(InterfaceType interfaceType)
        {
        }

        /// <inheritdoc/>
        public override void Visit(AsmExpression asmExpression)
        {
        }

        /// <inheritdoc/>
        public override void Visit(ConstantBuffer constantBuffer)
        {
            // Flatten the constant buffers
            if (constantBuffer.Members.Count > 0)
            {
                if (GenerateUniformBlocks)
                {
                    if (constantBuffer.Register != null)
                    {
                        var layoutQualifier = constantBuffer.Qualifiers.OfType<LayoutQualifier>().FirstOrDefault();
                        if (layoutQualifier == null)
                        {
                            layoutQualifier = new Stride.Core.Shaders.Ast.Glsl.LayoutQualifier();
                            constantBuffer.Qualifiers |= layoutQualifier;
                        }

                        layoutQualifier.Layouts.Insert(0, new LayoutKeyValue("binding", constantBuffer.Register.Register));
                    }
                    Write(constantBuffer.Qualifiers, true);
                    Write("uniform").Write(" ").Write(constantBuffer.Name).WriteSpace().Write("{").WriteLine();
                    Indent();
                    VisitList(constantBuffer.Members);
                }
                else
                {
                    Write("// Begin cbuffer ").Write(constantBuffer.Name).WriteLine();
                    foreach (var member in constantBuffer.Members)
                    {
                        // Prefix each variable with "uniform "
                        if (member is Variable)
                        {
                            Write("uniform");
                            Write(" ");
                        }
                        VisitDynamic(member);
                    }
                }

                if (GenerateUniformBlocks)
                {
                    Outdent();
                    Write("};").WriteLine();
                }
                else
                {
                    Write("// End buffer ").Write(constantBuffer.Name).WriteLine();
                }
            }
        }

        /// <inheritdoc/>
        public override void Visit(Typedef typedef)
        {
        }

        /// <inheritdoc/>
        public override void Visit(AttributeDeclaration attributeDeclaration)
        {

        }

        /// <inheritdoc/>
        public override void Visit(CastExpression castExpression)
        {
        }

        /// <summary>
        /// Visits the specified technique.
        /// </summary>
        /// <param name="technique">The technique.</param>
        public override void Visit(Technique technique)
        {
        }

        /// <inheritdoc />
        public override void Visit(StateInitializer stateInitializer)
        {
        }

        /// <inheritdoc />
        public override void Visit(StateExpression stateExpression)
        {
        }

        /// <inheritdoc />
        public override void Visit(Semantic semantic)
        {
        }

        /// <inheritdoc />
        public override void Visit(PackOffset packOffset)
        {
        }

        /// <inheritdoc />
        public override void Visit(RegisterLocation registerLocation)
        {
        }

        /// <inheritdoc />
        public override void Visit(Ast.Glsl.LayoutQualifier layoutQualifier)
        {
            Write("layout(");
            for (int i = 0; i < layoutQualifier.Layouts.Count; i++)
            {
                var layout = layoutQualifier.Layouts[i];
                if (i > 0) Write(",").WriteSpace();
                Write(layout.Name);
                if (layout.Value != null)
                {
                    WriteSpace().Write("=").WriteSpace();
                    base.Visit(layout.Value);
                }
            }
            Write(")");
            WriteSpace();
        }

        #endregion
    }
}
