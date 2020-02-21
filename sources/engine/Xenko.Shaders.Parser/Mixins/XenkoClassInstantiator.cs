// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Xenko.Core.Shaders.Ast.Xenko;
using Xenko.Shaders.Parser.Utility;
using Xenko.Core.Shaders.Ast;
using Xenko.Core.Shaders.Ast.Hlsl;
using Xenko.Core.Shaders.Utility;
using Xenko.Core.Shaders.Visitor;

using StorageQualifier = Xenko.Core.Shaders.Ast.StorageQualifier;

namespace Xenko.Shaders.Parser.Mixins
{
    internal class XenkoClassInstantiator : ShaderWalker
    {
        private ShaderClassType shaderClassType;

        private LoggerResult logger;

        private bool autoGenericInstances;

        private Dictionary<string, Variable> variableGenerics;

        private Dictionary<string, Expression> expressionGenerics;
        
        private Dictionary<string, Identifier> identifiersGenerics;

        private Dictionary<string, string> stringGenerics;

        private XenkoClassInstantiator(ShaderClassType classType, Dictionary<string, Expression> expressions, Dictionary<string, Identifier> identifiers, bool autoGenericInstances, LoggerResult log)
            : base(false, false)
        {
            shaderClassType = classType;
            expressionGenerics = expressions;
            identifiersGenerics = identifiers;
            this.autoGenericInstances = autoGenericInstances;
            logger = log;
            variableGenerics = shaderClassType.ShaderGenerics.ToDictionary(x => x.Name.Text, x => x);
        }

        public static void Instantiate(ShaderClassType classType, Dictionary<string, Expression> expressions, Dictionary<string, Identifier> identifiers, bool autoGenericInstances, LoggerResult log)
        {
            var instantiator = new XenkoClassInstantiator(classType, expressions, identifiers, autoGenericInstances, log);
            instantiator.Run();
        }

        private void Run()
        {
            stringGenerics = identifiersGenerics.ToDictionary(x => x.Key, x => x.Value.ToString());

            foreach (var baseClass in shaderClassType.BaseClasses)
                VisitDynamic(baseClass); // look for IdentifierGeneric

            foreach (var member in shaderClassType.Members)
                VisitDynamic(member); // look for IdentifierGeneric and Variable

            // Process each constant buffer encoded as tag
            foreach (var constantBuffer in shaderClassType.Members.OfType<Variable>().Select(x => (ConstantBuffer)x.GetTag(XenkoTags.ConstantBuffer)).Where(x => x != null).Distinct())
                VisitDynamic(constantBuffer);

            int insertIndex = 0;
            foreach (var variable in shaderClassType.ShaderGenerics)
            {
                // For all string generic argument, don't try to assign an initial value as they are replaced directly at visit time. 
                if (variable.Type is IGenericStringArgument)
                    continue;

                variable.InitialValue = expressionGenerics[variable.Name.Text];
                
                // TODO: be more precise

                if (!(variable.InitialValue is VariableReferenceExpression || variable.InitialValue is MemberReferenceExpression))
                {
                    variable.Qualifiers |= StorageQualifier.Const;
                    variable.Qualifiers |= Xenko.Core.Shaders.Ast.Hlsl.StorageQualifier.Static;
                }
                // Because FindDeclaration is broken for variable declared at the scope of the class, make sure  to
                // put const at the beginning of the class to allow further usage of the variable to work
                shaderClassType.Members.Insert(insertIndex++, variable);
            }
        }

        public override void Visit(MemberReferenceExpression memberReferenceExpression)
        {
            base.Visit(memberReferenceExpression);

            // Try to find usage of all MemberName 'yyy' in reference expressions like "xxx.yyy" and replace by their
            // generic instantiation
            var memberVariableName = memberReferenceExpression.Member.Text;
            if (variableGenerics.TryGetValue(memberVariableName, out var memberVariable) && memberVariable.Type is MemberName)
            {
                string memberName;
                if (stringGenerics.TryGetValue(memberVariableName, out memberName) && !autoGenericInstances)
                {
                    memberReferenceExpression.Member = new Identifier(memberName);
                }
                else
                {
                    memberReferenceExpression.TypeInference.Declaration = memberVariable;
                }
            }
        }

        public override void Visit(ConstantBuffer constantBuffer)
        {
            string remappedConstantBufferName;
            if (stringGenerics.TryGetValue(constantBuffer.Name.Text, out remappedConstantBufferName))
                constantBuffer.Name = new Identifier(remappedConstantBufferName);

            base.Visit(constantBuffer);
        }

        public override void Visit(Variable variable)
        {
            base.Visit(variable);
            //TODO: check types

            // Don't perform any replacement if we are just auto instancing shaders
            if (autoGenericInstances)
            {
                return;
            }

            // no call on base
            // Semantic keyword: replace semantics
            foreach (var sem in variable.Qualifiers.Values.OfType<Semantic>())
            {
                string replacementSemantic;
                if (stringGenerics.TryGetValue(sem.Name, out replacementSemantic))
                {
                    if (logger != null && !(variableGenerics[sem.Name].Type is SemanticType))
                        logger.Warning(XenkoMessageCode.WarningUseSemanticType, variable.Span, variableGenerics[sem.Name]);
                    sem.Name = replacementSemantic;
                }
            }

            // MemberName keyword: replace variable names
            if (variableGenerics.TryGetValue(variable.Name, out var genVariable) && genVariable.Type is MemberName)
            {
                string memberName;
                if (stringGenerics.TryGetValue(variable.Name, out memberName))
                {
                    variable.Name = new Identifier(memberName);
                }
            }

            foreach (var annotation in variable.Attributes.OfType<AttributeDeclaration>().Where(x => x.Name == "Link" && x.Parameters.Count > 0))
            {
                var linkName = (string)annotation.Parameters[0].Value;

                if (String.IsNullOrEmpty(linkName))
                    continue;

                var replacements = new List<Tuple<string, int>>();

                foreach (var generic in variableGenerics.Where(x => x.Value.Type is LinkType))
                {
                    var index = linkName.IndexOf(generic.Key, 0);
                    if (index >= 0)
                        replacements.Add(Tuple.Create(generic.Key, index));
                }

                if (replacements.Count > 0)
                {
                    var finalString = "";
                    var currentIndex = 0;
                    foreach (var replacement in replacements.OrderBy(x => x.Item2))
                    {
                        var replacementIndex = replacement.Item2;
                        var stringToReplace = replacement.Item1;

                        if (replacementIndex - currentIndex > 0)
                            finalString += linkName.Substring(currentIndex, replacementIndex - currentIndex);
                        finalString += stringGenerics[stringToReplace];
                        currentIndex = replacementIndex + stringToReplace.Length;
                    }

                    if (currentIndex < linkName.Length)
                        finalString += linkName.Substring(currentIndex);

                    annotation.Parameters[0] = new Literal(finalString);
                }
            }
        }

        public override void Visit(IdentifierGeneric identifierGeneric)
        {
            base.Visit(identifierGeneric);

            for (var i = 0; i < identifierGeneric.Identifiers.Count; ++i)
            {
                Identifier replacement;
                if (identifiersGenerics.TryGetValue(identifierGeneric.Identifiers[i].ToString(), out replacement))
                    identifierGeneric.Identifiers[i] = replacement;
            }
        }
    }
}
