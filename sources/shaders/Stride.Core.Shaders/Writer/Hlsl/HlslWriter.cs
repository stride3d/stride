// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;

namespace Stride.Core.Shaders.Writer.Hlsl
{
    /// <summary>
    /// A writer for a shader.
    /// </summary>
    public class HlslWriter : ShaderWriter
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HlslWriter"/> class. 
        /// </summary>
        /// <param name="useNodeStack">
        /// if set to <c>true</c> [use node stack].
        /// </param>
        public HlslWriter(bool useNodeStack = false) : base(useNodeStack)
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Visits the specified Annotations.
        /// </summary>
        /// <param name="annotations">The Annotations.</param>
        public override void Visit(Ast.Hlsl.Annotations annotations)
        {
            if (annotations.Variables.Count == 0) return;

            Write("<").WriteSpace();

            foreach (var variable in annotations.Variables)
            {
                VisitDynamic(variable);
            }

            WriteSpace().Write(">");
        }

        /// <summary>
        /// Visits the specified class type.
        /// </summary>
        /// <param name="classType">Type of the class.</param>
        public override void Visit(ClassType classType)
        {
            Write(classType.Attributes, true);

            Write("class").Write(" ").Write(classType.Name);

            if (classType.GenericParameters.Count > 0)
            {
                Write("<");
                for (int i = 0; i < classType.GenericParameters.Count; i++)
                {
                    var genericArgument = classType.GenericParameters[i];
                    if (i > 0) Write(", ");
                    Write(genericArgument.Name);
                }
                Write(">");
            }

            if (classType.BaseClasses.Count > 0)
            {
                WriteSpace().Write(":").WriteSpace();
                for (int i = 0; i < classType.BaseClasses.Count; i++)
                {
                    var baseClass = classType.BaseClasses[i];
                    if (i > 0)
                    {
                        Write(",").WriteSpace();
                    }

                    Write(baseClass.Name);
                }

                WriteSpace();
            }
            else
            {
                WriteSpace();
            }

            OpenBrace();

            VisitList(classType.Members);

            CloseBrace(false).Write(";").WriteLine();
        }

        /// <summary>
        /// Visits the specified interface type.
        /// </summary>
        /// <param name="interfaceType">Type of the interface.</param>
        public override void Visit(InterfaceType interfaceType)
        {
            Write(interfaceType.Attributes, true);
            Write("interface").Write(" ").Write(interfaceType.Name);
            WriteSpace();
            OpenBrace();
            VisitList(interfaceType.Methods);
            CloseBrace(false).Write(";").WriteLine(); 
        }

        /// <summary>
        /// Visits the specified asm expression.
        /// </summary>
        /// <param name="asmExpression">The asm expression.</param>
        public override void Visit(AsmExpression asmExpression)
        {
            WriteLine();
            Write("asm");
            OpenBrace();
            Write(asmExpression.Text);
            CloseBrace();
        }

        /// <summary>
        /// Visits the specified constant buffer.
        /// </summary>
        /// <param name="constantBuffer">The constant buffer.</param>
        public override void Visit(ConstantBuffer constantBuffer)
        {
            Write(constantBuffer.Attributes, true);

            Write(constantBuffer.Type.Key.ToString());

            if (constantBuffer.Name != null)
            {
                Write(" ").Write(constantBuffer.Name);
            }

            WriteSpace();
            VisitDynamic(constantBuffer.Register);
            OpenBrace();
            VisitList(constantBuffer.Members);
            CloseBrace(false).Write(";").WriteLine(); 
        }

        /// <summary>
        /// Visits the specified typedef.
        /// </summary>
        /// <param name="typedef">The typedef.</param>
        public override void Visit(Typedef typedef)
        {
            Write("typedef").Write(" ");
            Write(typedef.Qualifiers, true);
            VisitDynamic(typedef.Type);
            Write(" ");

            if (typedef.IsGroup)
            {
                for (int i = 0; i < typedef.SubDeclarators.Count; i++)
                {
                    var declarator = typedef.SubDeclarators[i];
                    if (i > 0)
                    {
                        Write(",").WriteSpace();
                    }

                    Write(declarator.Name);
                }
            }
            else
            {
                Write(typedef.Name);
            }

            Write(";");
            WriteLine();
        }

        /// <summary>
        /// Visits the specified attribute declaration.
        /// </summary>
        /// <param name="attributeDeclaration">The attribute declaration.</param>
        public override void Visit(AttributeDeclaration attributeDeclaration)
        {
            Write("[").Write(attributeDeclaration.Name);
            if (attributeDeclaration.Parameters.Count > 0)
            {
                Write("(");
                for (int i = 0; i < attributeDeclaration.Parameters.Count; i++)
                {
                    var parameter = attributeDeclaration.Parameters[i];
                    if (i > 0)
                    {
                        Write(",").WriteSpace();
                    }

                    VisitDynamic(parameter);
                }

                Write(")");
            }

            WriteLine("]");
        }

        /// <summary>
        /// Visits the specified cast expression.
        /// </summary>
        /// <param name="castExpression">The cast expression.</param>
        public override void Visit(CastExpression castExpression)
        {
            Write("(");
            VisitDynamic(castExpression.Target);
            Write(")");
            VisitDynamic(castExpression.From);
        }

        /// <summary>
        /// Visits the specified composite identifier.
        /// </summary>
        /// <param name="compositeIdentifier">The composite identifier.</param>
        public override void Visit(IdentifierDot compositeIdentifier)
        {
            Write((Identifier)compositeIdentifier);
        }

        /// <summary>
        /// Visits the specified composite identifier.
        /// </summary>
        /// <param name="compositeIdentifier">The composite identifier.</param>
        public override void Visit(IdentifierNs compositeIdentifier)
        {
            Write((Identifier)compositeIdentifier);
        }

        /// <summary>
        /// Visits the specified composite identifier.
        /// </summary>
        /// <param name="compositeIdentifier">The composite identifier.</param>
        public override void Visit(IdentifierGeneric compositeIdentifier)
        {
            Write((Identifier)compositeIdentifier);
        }

        /// <summary>
        /// Visits the specified state expression.
        /// </summary>
        /// <param name="stateExpression">The state expression.</param>
        public override void Visit(StateExpression stateExpression)
        {
            VisitDynamic(stateExpression.StateType);
            WriteSpace();
            VisitDynamic(stateExpression.Initializer);
        }

        /// <summary>
        /// Visits the specified compile expression.
        /// </summary>
        /// <param name="compileExpression">The compile expression.</param>
        public override void Visit(CompileExpression compileExpression)
        {
            Write("compile").Write(" ");
            Write(compileExpression.Profile);
            Write(" ");
            VisitDynamic(compileExpression.Function);
        }

        /// <summary>
        /// Visits the specified technique.
        /// </summary>
        /// <param name="technique">The technique.</param>
        public override void Visit(Technique technique)
        {
            Write(technique.Attributes, true);
            Write(technique.Type);
            if (technique.Name != null)
            {
                Write(" ").Write(technique.Name);
            }

            WriteSpace();
            Write(technique.Attributes, false);
            OpenBrace();
            VisitList(technique.Passes);
            CloseBrace();
        }

        /// <summary>
        /// Visits the specified pass.
        /// </summary>
        /// <param name="pass">The pass.</param>
        public override void Visit(Pass pass)
        {
            Write(pass.Attributes, true);
            Write("pass");
            if (pass.Name != null)
            {
                Write(" ").Write(pass.Name);
            }

            WriteSpace();
            Write(pass.Attributes, false);
            OpenBrace();
            foreach (var expression in pass.Items)
            {
                VisitDynamic(expression);
                WriteLine(";");
            }

            CloseBrace();
        }

        /// <inheritdoc />
        public override void Visit(StateInitializer stateInitializer)
        {
            OpenBrace();
            for (int i = 0; i < stateInitializer.Items.Count; i++)
            {
                var item = stateInitializer.Items[i];
                if (item is StateInitializer && i > 0)
                {
                    WriteLine(",");
                }

                VisitDynamic(item);

                if (!(item is StateInitializer))
                {
                    WriteLine(";");                                       
                }
            }

            CloseBrace(false);
        }

        /// <inheritdoc />
        public override void WriteInitializer(Expression expression)
        {
            if (expression == null) return;

            if (!(expression is StateInitializer))
                WriteSpace().Write("=");
            WriteSpace();
            VisitDynamic(expression);            
        }

        /// <inheritdoc />
        public override void Visit(Semantic semantic)
        {
            Write(":").WriteSpace();
            Write(semantic.Name);
        }

        /// <inheritdoc />
        public override void Visit(PackOffset packOffset)
        {
            Write(":").WriteSpace();
            Write("packoffset(");
            Write((Identifier)packOffset.Value);
            Write(")");
        }

        /// <inheritdoc />
        public override void Visit(RegisterLocation registerLocation)
        {
            Write(":").WriteSpace();
            Write("register(");
            if (registerLocation.Profile != null)
            {
                Write(registerLocation.Profile);
                Write(",").WriteSpace();
            }

            Write(registerLocation.Register);
            Write(")");
        }

        #endregion

        /// <summary>
        /// Writes the specified identifier.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <returns>
        /// This instance
        /// </returns>
        protected override ShaderWriter Write(Identifier identifier)
        {
            Write(identifier.Text);

            if (identifier.IsSpecialReference)
            {
                Write("<");
            }

            if (identifier is CompositeIdentifier)
            {
                var compositeIdentifier = (CompositeIdentifier)identifier;
                for (int i = 0; i < compositeIdentifier.Identifiers.Count; i++)
                {
                    var subIdentifier = compositeIdentifier.Identifiers[i];
                    if (i > 0) Write(compositeIdentifier.Separator);
                    Write(subIdentifier);
                }
            }

            if (identifier.HasIndices)
            {
                WriteRankSpecifiers(identifier.Indices);
            }

            if (identifier.IsSpecialReference)
            {
                Write(">");
            }

            return this;
        }
    }
}
