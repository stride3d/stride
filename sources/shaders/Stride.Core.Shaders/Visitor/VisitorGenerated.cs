// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.




using Xenko.Core.Shaders.Ast;
using Xenko.Core.Shaders.Visitor;

namespace Xenko.Core.Shaders.Visitor
{
    public partial class ShaderVisitor<TResult>
    {
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Xenko.ClassIdentifierGeneric classIdentifierGeneric)
        {
            return DefaultVisit(classIdentifierGeneric);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Xenko.EnumType enumType)
        {
            return DefaultVisit(enumType);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Xenko.ForEachStatement forEachStatement)
        {
            return DefaultVisit(forEachStatement);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Xenko.ImportBlockStatement importBlockStatement)
        {
            return DefaultVisit(importBlockStatement);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Xenko.LinkType linkType)
        {
            return DefaultVisit(linkType);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Xenko.LiteralIdentifier literalIdentifier)
        {
            return DefaultVisit(literalIdentifier);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Xenko.MemberName memberName)
        {
            return DefaultVisit(memberName);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Xenko.MixinStatement mixinStatement)
        {
            return DefaultVisit(mixinStatement);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Xenko.NamespaceBlock namespaceBlock)
        {
            return DefaultVisit(namespaceBlock);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Xenko.ParametersBlock parametersBlock)
        {
            return DefaultVisit(parametersBlock);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Xenko.SemanticType semanticType)
        {
            return DefaultVisit(semanticType);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Xenko.EffectBlock effectBlock)
        {
            return DefaultVisit(effectBlock);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Xenko.ShaderClassType shaderClassType)
        {
            return DefaultVisit(shaderClassType);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Xenko.ShaderRootClassType shaderRootClassType)
        {
            return DefaultVisit(shaderRootClassType);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Xenko.ShaderTypeName shaderTypeName)
        {
            return DefaultVisit(shaderTypeName);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Xenko.TypeIdentifier typeIdentifier)
        {
            return DefaultVisit(typeIdentifier);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Xenko.UsingParametersStatement usingParametersStatement)
        {
            return DefaultVisit(usingParametersStatement);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Xenko.UsingStatement usingStatement)
        {
            return DefaultVisit(usingStatement);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Xenko.VarType varType)
        {
            return DefaultVisit(varType);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Xenko.XenkoConstantBufferType xenkoConstantBufferType)
        {
            return DefaultVisit(xenkoConstantBufferType);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.ArrayInitializerExpression arrayInitializerExpression)
        {
            return DefaultVisit(arrayInitializerExpression);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.ArrayType arrayType)
        {
            return DefaultVisit(arrayType);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.AssignmentExpression assignmentExpression)
        {
            return DefaultVisit(assignmentExpression);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.BinaryExpression binaryExpression)
        {
            return DefaultVisit(binaryExpression);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.BlockStatement blockStatement)
        {
            return DefaultVisit(blockStatement);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.CaseStatement caseStatement)
        {
            return DefaultVisit(caseStatement);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.CompositeEnum compositeEnum)
        {
            return DefaultVisit(compositeEnum);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.ConditionalExpression conditionalExpression)
        {
            return DefaultVisit(conditionalExpression);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.EmptyStatement emptyStatement)
        {
            return DefaultVisit(emptyStatement);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.EmptyExpression emptyExpression)
        {
            return DefaultVisit(emptyExpression);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Glsl.LayoutKeyValue layoutKeyValue)
        {
            return DefaultVisit(layoutKeyValue);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Glsl.LayoutQualifier layoutQualifier)
        {
            return DefaultVisit(layoutQualifier);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Glsl.InterfaceType interfaceType)
        {
            return DefaultVisit(interfaceType);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Hlsl.ClassType classType)
        {
            return DefaultVisit(classType);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Hlsl.IdentifierGeneric identifierGeneric)
        {
            return DefaultVisit(identifierGeneric);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Hlsl.IdentifierNs identifierNs)
        {
            return DefaultVisit(identifierNs);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Hlsl.IdentifierDot identifierDot)
        {
            return DefaultVisit(identifierDot);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Hlsl.TextureType textureType)
        {
            return DefaultVisit(textureType);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Hlsl.Annotations annotations)
        {
            return DefaultVisit(annotations);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Hlsl.AsmExpression asmExpression)
        {
            return DefaultVisit(asmExpression);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Hlsl.AttributeDeclaration attributeDeclaration)
        {
            return DefaultVisit(attributeDeclaration);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Hlsl.CastExpression castExpression)
        {
            return DefaultVisit(castExpression);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Hlsl.CompileExpression compileExpression)
        {
            return DefaultVisit(compileExpression);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Hlsl.ConstantBuffer constantBuffer)
        {
            return DefaultVisit(constantBuffer);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Hlsl.ConstantBufferType constantBufferType)
        {
            return DefaultVisit(constantBufferType);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Hlsl.InterfaceType interfaceType)
        {
            return DefaultVisit(interfaceType);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Hlsl.PackOffset packOffset)
        {
            return DefaultVisit(packOffset);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Hlsl.Pass pass)
        {
            return DefaultVisit(pass);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Hlsl.RegisterLocation registerLocation)
        {
            return DefaultVisit(registerLocation);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Hlsl.Semantic semantic)
        {
            return DefaultVisit(semantic);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Hlsl.StateExpression stateExpression)
        {
            return DefaultVisit(stateExpression);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Hlsl.StateInitializer stateInitializer)
        {
            return DefaultVisit(stateInitializer);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Hlsl.Technique technique)
        {
            return DefaultVisit(technique);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Hlsl.Typedef typedef)
        {
            return DefaultVisit(typedef);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.ExpressionList expressionList)
        {
            return DefaultVisit(expressionList);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.GenericDeclaration genericDeclaration)
        {
            return DefaultVisit(genericDeclaration);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.GenericParameterType genericParameterType)
        {
            return DefaultVisit(genericParameterType);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.DeclarationStatement declarationStatement)
        {
            return DefaultVisit(declarationStatement);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.ExpressionStatement expressionStatement)
        {
            return DefaultVisit(expressionStatement);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.ForStatement forStatement)
        {
            return DefaultVisit(forStatement);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.GenericType genericType)
        {
            return DefaultVisit(genericType);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Identifier identifier)
        {
            return DefaultVisit(identifier);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.IfStatement ifStatement)
        {
            return DefaultVisit(ifStatement);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.IndexerExpression indexerExpression)
        {
            return DefaultVisit(indexerExpression);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.KeywordExpression keywordExpression)
        {
            return DefaultVisit(keywordExpression);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Literal literal)
        {
            return DefaultVisit(literal);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.LiteralExpression literalExpression)
        {
            return DefaultVisit(literalExpression);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.MatrixType matrixType)
        {
            return DefaultVisit(matrixType);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.MemberReferenceExpression memberReferenceExpression)
        {
            return DefaultVisit(memberReferenceExpression);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.MethodDeclaration methodDeclaration)
        {
            return DefaultVisit(methodDeclaration);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.MethodDefinition methodDefinition)
        {
            return DefaultVisit(methodDefinition);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.MethodInvocationExpression methodInvocationExpression)
        {
            return DefaultVisit(methodInvocationExpression);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.ObjectType objectType)
        {
            return DefaultVisit(objectType);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Parameter parameter)
        {
            return DefaultVisit(parameter);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.ParenthesizedExpression parenthesizedExpression)
        {
            return DefaultVisit(parenthesizedExpression);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Qualifier qualifier)
        {
            return DefaultVisit(qualifier);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.ReturnStatement returnStatement)
        {
            return DefaultVisit(returnStatement);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.ScalarType scalarType)
        {
            return DefaultVisit(scalarType);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Shader shader)
        {
            return DefaultVisit(shader);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.StatementList statementList)
        {
            return DefaultVisit(statementList);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.StructType structType)
        {
            return DefaultVisit(structType);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.SwitchCaseGroup switchCaseGroup)
        {
            return DefaultVisit(switchCaseGroup);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.SwitchStatement switchStatement)
        {
            return DefaultVisit(switchStatement);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.TypeName typeName)
        {
            return DefaultVisit(typeName);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.TypeReferenceExpression typeReferenceExpression)
        {
            return DefaultVisit(typeReferenceExpression);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.UnaryExpression unaryExpression)
        {
            return DefaultVisit(unaryExpression);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.Variable variable)
        {
            return DefaultVisit(variable);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.VariableReferenceExpression variableReferenceExpression)
        {
            return DefaultVisit(variableReferenceExpression);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.VectorType vectorType)
        {
            return DefaultVisit(vectorType);
        }
        public virtual TResult Visit(Xenko.Core.Shaders.Ast.WhileStatement whileStatement)
        {
            return DefaultVisit(whileStatement);
        }
    }

    public partial class ShaderRewriter
    {
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.ClassIdentifierGeneric classIdentifierGeneric)
        {
            VisitList(classIdentifierGeneric.Indices);
            VisitList(classIdentifierGeneric.Generics);
            return base.Visit(classIdentifierGeneric);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.EnumType enumType)
        {
            VisitList(enumType.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(enumType.Name);
            if (!ReferenceEquals(nameTemp, enumType.Name))
                enumType.Name = nameTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(enumType.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, enumType.Qualifiers))
                enumType.Qualifiers = qualifiersTemp;
            VisitList(enumType.Values);
            return base.Visit(enumType);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.ForEachStatement forEachStatement)
        {
            VisitList(forEachStatement.Attributes);
            var collectionTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(forEachStatement.Collection);
            if (!ReferenceEquals(collectionTemp, forEachStatement.Collection))
                forEachStatement.Collection = collectionTemp;
            var variableTemp = (Xenko.Core.Shaders.Ast.Variable)VisitDynamic(forEachStatement.Variable);
            if (!ReferenceEquals(variableTemp, forEachStatement.Variable))
                forEachStatement.Variable = variableTemp;
            var bodyTemp = (Xenko.Core.Shaders.Ast.Statement)VisitDynamic(forEachStatement.Body);
            if (!ReferenceEquals(bodyTemp, forEachStatement.Body))
                forEachStatement.Body = bodyTemp;
            return base.Visit(forEachStatement);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.ImportBlockStatement importBlockStatement)
        {
            VisitList(importBlockStatement.Attributes);
            var statementsTemp = (Xenko.Core.Shaders.Ast.StatementList)VisitDynamic(importBlockStatement.Statements);
            if (!ReferenceEquals(statementsTemp, importBlockStatement.Statements))
                importBlockStatement.Statements = statementsTemp;
            return base.Visit(importBlockStatement);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.LinkType linkType)
        {
            VisitList(linkType.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(linkType.Name);
            if (!ReferenceEquals(nameTemp, linkType.Name))
                linkType.Name = nameTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(linkType.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, linkType.Qualifiers))
                linkType.Qualifiers = qualifiersTemp;
            return base.Visit(linkType);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.LiteralIdentifier literalIdentifier)
        {
            VisitList(literalIdentifier.Indices);
            var valueTemp = (Xenko.Core.Shaders.Ast.Literal)VisitDynamic(literalIdentifier.Value);
            if (!ReferenceEquals(valueTemp, literalIdentifier.Value))
                literalIdentifier.Value = valueTemp;
            return base.Visit(literalIdentifier);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.MemberName memberName)
        {
            VisitList(memberName.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(memberName.Name);
            if (!ReferenceEquals(nameTemp, memberName.Name))
                memberName.Name = nameTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(memberName.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, memberName.Qualifiers))
                memberName.Qualifiers = qualifiersTemp;
            return base.Visit(memberName);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.MixinStatement mixinStatement)
        {
            VisitList(mixinStatement.Attributes);
            var valueTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(mixinStatement.Value);
            if (!ReferenceEquals(valueTemp, mixinStatement.Value))
                mixinStatement.Value = valueTemp;
            return base.Visit(mixinStatement);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.NamespaceBlock namespaceBlock)
        {
            VisitList(namespaceBlock.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(namespaceBlock.Name);
            if (!ReferenceEquals(nameTemp, namespaceBlock.Name))
                namespaceBlock.Name = nameTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(namespaceBlock.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, namespaceBlock.Qualifiers))
                namespaceBlock.Qualifiers = qualifiersTemp;
            VisitList(namespaceBlock.Body);
            return base.Visit(namespaceBlock);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.ParametersBlock parametersBlock)
        {
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(parametersBlock.Name);
            if (!ReferenceEquals(nameTemp, parametersBlock.Name))
                parametersBlock.Name = nameTemp;
            var bodyTemp = (Xenko.Core.Shaders.Ast.BlockStatement)VisitDynamic(parametersBlock.Body);
            if (!ReferenceEquals(bodyTemp, parametersBlock.Body))
                parametersBlock.Body = bodyTemp;
            return base.Visit(parametersBlock);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.SemanticType semanticType)
        {
            VisitList(semanticType.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(semanticType.Name);
            if (!ReferenceEquals(nameTemp, semanticType.Name))
                semanticType.Name = nameTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(semanticType.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, semanticType.Qualifiers))
                semanticType.Qualifiers = qualifiersTemp;
            return base.Visit(semanticType);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.EffectBlock effectBlock)
        {
            VisitList(effectBlock.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(effectBlock.Name);
            if (!ReferenceEquals(nameTemp, effectBlock.Name))
                effectBlock.Name = nameTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(effectBlock.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, effectBlock.Qualifiers))
                effectBlock.Qualifiers = qualifiersTemp;
            var bodyTemp = (Xenko.Core.Shaders.Ast.BlockStatement)VisitDynamic(effectBlock.Body);
            if (!ReferenceEquals(bodyTemp, effectBlock.Body))
                effectBlock.Body = bodyTemp;
            return base.Visit(effectBlock);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.ShaderClassType shaderClassType)
        {
            VisitList(shaderClassType.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(shaderClassType.Name);
            if (!ReferenceEquals(nameTemp, shaderClassType.Name))
                shaderClassType.Name = nameTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(shaderClassType.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, shaderClassType.Qualifiers))
                shaderClassType.Qualifiers = qualifiersTemp;
            VisitList(shaderClassType.BaseClasses);
            VisitList(shaderClassType.GenericParameters);
            VisitList(shaderClassType.GenericArguments);
            VisitList(shaderClassType.Members);
            VisitList(shaderClassType.ShaderGenerics);
            return base.Visit(shaderClassType);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.ShaderRootClassType shaderRootClassType)
        {
            VisitList(shaderRootClassType.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(shaderRootClassType.Name);
            if (!ReferenceEquals(nameTemp, shaderRootClassType.Name))
                shaderRootClassType.Name = nameTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(shaderRootClassType.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, shaderRootClassType.Qualifiers))
                shaderRootClassType.Qualifiers = qualifiersTemp;
            VisitList(shaderRootClassType.BaseClasses);
            VisitList(shaderRootClassType.GenericParameters);
            VisitList(shaderRootClassType.GenericArguments);
            VisitList(shaderRootClassType.Members);
            VisitList(shaderRootClassType.ShaderGenerics);
            return base.Visit(shaderRootClassType);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.ShaderTypeName shaderTypeName)
        {
            VisitList(shaderTypeName.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(shaderTypeName.Name);
            if (!ReferenceEquals(nameTemp, shaderTypeName.Name))
                shaderTypeName.Name = nameTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(shaderTypeName.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, shaderTypeName.Qualifiers))
                shaderTypeName.Qualifiers = qualifiersTemp;
            return base.Visit(shaderTypeName);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.TypeIdentifier typeIdentifier)
        {
            VisitList(typeIdentifier.Indices);
            var typeTemp = (Xenko.Core.Shaders.Ast.TypeBase)VisitDynamic(typeIdentifier.Type);
            if (!ReferenceEquals(typeTemp, typeIdentifier.Type))
                typeIdentifier.Type = typeTemp;
            return base.Visit(typeIdentifier);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.UsingParametersStatement usingParametersStatement)
        {
            VisitList(usingParametersStatement.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(usingParametersStatement.Name);
            if (!ReferenceEquals(nameTemp, usingParametersStatement.Name))
                usingParametersStatement.Name = nameTemp;
            var bodyTemp = (Xenko.Core.Shaders.Ast.BlockStatement)VisitDynamic(usingParametersStatement.Body);
            if (!ReferenceEquals(bodyTemp, usingParametersStatement.Body))
                usingParametersStatement.Body = bodyTemp;
            return base.Visit(usingParametersStatement);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.UsingStatement usingStatement)
        {
            VisitList(usingStatement.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(usingStatement.Name);
            if (!ReferenceEquals(nameTemp, usingStatement.Name))
                usingStatement.Name = nameTemp;
            return base.Visit(usingStatement);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.VarType varType)
        {
            VisitList(varType.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(varType.Name);
            if (!ReferenceEquals(nameTemp, varType.Name))
                varType.Name = nameTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(varType.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, varType.Qualifiers))
                varType.Qualifiers = qualifiersTemp;
            return base.Visit(varType);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.XenkoConstantBufferType xenkoConstantBufferType)
        {
            return base.Visit(xenkoConstantBufferType);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.ArrayInitializerExpression arrayInitializerExpression)
        {
            VisitList(arrayInitializerExpression.Items);
            return base.Visit(arrayInitializerExpression);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.ArrayType arrayType)
        {
            VisitList(arrayType.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(arrayType.Name);
            if (!ReferenceEquals(nameTemp, arrayType.Name))
                arrayType.Name = nameTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(arrayType.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, arrayType.Qualifiers))
                arrayType.Qualifiers = qualifiersTemp;
            VisitList(arrayType.Dimensions);
            var typeTemp = (Xenko.Core.Shaders.Ast.TypeBase)VisitDynamic(arrayType.Type);
            if (!ReferenceEquals(typeTemp, arrayType.Type))
                arrayType.Type = typeTemp;
            return base.Visit(arrayType);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.AssignmentExpression assignmentExpression)
        {
            var targetTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(assignmentExpression.Target);
            if (!ReferenceEquals(targetTemp, assignmentExpression.Target))
                assignmentExpression.Target = targetTemp;
            var valueTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(assignmentExpression.Value);
            if (!ReferenceEquals(valueTemp, assignmentExpression.Value))
                assignmentExpression.Value = valueTemp;
            return base.Visit(assignmentExpression);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.BinaryExpression binaryExpression)
        {
            var leftTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(binaryExpression.Left);
            if (!ReferenceEquals(leftTemp, binaryExpression.Left))
                binaryExpression.Left = leftTemp;
            var rightTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(binaryExpression.Right);
            if (!ReferenceEquals(rightTemp, binaryExpression.Right))
                binaryExpression.Right = rightTemp;
            return base.Visit(binaryExpression);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.BlockStatement blockStatement)
        {
            VisitList(blockStatement.Attributes);
            var statementsTemp = (Xenko.Core.Shaders.Ast.StatementList)VisitDynamic(blockStatement.Statements);
            if (!ReferenceEquals(statementsTemp, blockStatement.Statements))
                blockStatement.Statements = statementsTemp;
            return base.Visit(blockStatement);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.CaseStatement caseStatement)
        {
            VisitList(caseStatement.Attributes);
            var caseTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(caseStatement.Case);
            if (!ReferenceEquals(caseTemp, caseStatement.Case))
                caseStatement.Case = caseTemp;
            return base.Visit(caseStatement);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.CompositeEnum compositeEnum)
        {
            return base.Visit(compositeEnum);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.ConditionalExpression conditionalExpression)
        {
            var conditionTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(conditionalExpression.Condition);
            if (!ReferenceEquals(conditionTemp, conditionalExpression.Condition))
                conditionalExpression.Condition = conditionTemp;
            var leftTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(conditionalExpression.Left);
            if (!ReferenceEquals(leftTemp, conditionalExpression.Left))
                conditionalExpression.Left = leftTemp;
            var rightTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(conditionalExpression.Right);
            if (!ReferenceEquals(rightTemp, conditionalExpression.Right))
                conditionalExpression.Right = rightTemp;
            return base.Visit(conditionalExpression);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.EmptyStatement emptyStatement)
        {
            VisitList(emptyStatement.Attributes);
            return base.Visit(emptyStatement);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.EmptyExpression emptyExpression)
        {
            return base.Visit(emptyExpression);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Glsl.LayoutKeyValue layoutKeyValue)
        {
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(layoutKeyValue.Name);
            if (!ReferenceEquals(nameTemp, layoutKeyValue.Name))
                layoutKeyValue.Name = nameTemp;
            var valueTemp = (Xenko.Core.Shaders.Ast.LiteralExpression)VisitDynamic(layoutKeyValue.Value);
            if (!ReferenceEquals(valueTemp, layoutKeyValue.Value))
                layoutKeyValue.Value = valueTemp;
            return base.Visit(layoutKeyValue);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Glsl.LayoutQualifier layoutQualifier)
        {
            VisitList(layoutQualifier.Layouts);
            return base.Visit(layoutQualifier);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Glsl.InterfaceType interfaceType)
        {
            VisitList(interfaceType.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(interfaceType.Name);
            if (!ReferenceEquals(nameTemp, interfaceType.Name))
                interfaceType.Name = nameTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(interfaceType.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, interfaceType.Qualifiers))
                interfaceType.Qualifiers = qualifiersTemp;
            VisitList(interfaceType.Fields);
            return base.Visit(interfaceType);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.ClassType classType)
        {
            VisitList(classType.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(classType.Name);
            if (!ReferenceEquals(nameTemp, classType.Name))
                classType.Name = nameTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(classType.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, classType.Qualifiers))
                classType.Qualifiers = qualifiersTemp;
            VisitList(classType.BaseClasses);
            VisitList(classType.GenericParameters);
            VisitList(classType.GenericArguments);
            VisitList(classType.Members);
            return base.Visit(classType);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.IdentifierGeneric identifierGeneric)
        {
            VisitList(identifierGeneric.Indices);
            VisitList(identifierGeneric.Identifiers);
            return base.Visit(identifierGeneric);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.IdentifierNs identifierNs)
        {
            VisitList(identifierNs.Indices);
            VisitList(identifierNs.Identifiers);
            return base.Visit(identifierNs);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.IdentifierDot identifierDot)
        {
            VisitList(identifierDot.Indices);
            VisitList(identifierDot.Identifiers);
            return base.Visit(identifierDot);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.TextureType textureType)
        {
            VisitList(textureType.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(textureType.Name);
            if (!ReferenceEquals(nameTemp, textureType.Name))
                textureType.Name = nameTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(textureType.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, textureType.Qualifiers))
                textureType.Qualifiers = qualifiersTemp;
            return base.Visit(textureType);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.Annotations annotations)
        {
            VisitList(annotations.Variables);
            return base.Visit(annotations);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.AsmExpression asmExpression)
        {
            return base.Visit(asmExpression);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.AttributeDeclaration attributeDeclaration)
        {
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(attributeDeclaration.Name);
            if (!ReferenceEquals(nameTemp, attributeDeclaration.Name))
                attributeDeclaration.Name = nameTemp;
            VisitList(attributeDeclaration.Parameters);
            return base.Visit(attributeDeclaration);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.CastExpression castExpression)
        {
            var fromTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(castExpression.From);
            if (!ReferenceEquals(fromTemp, castExpression.From))
                castExpression.From = fromTemp;
            var targetTemp = (Xenko.Core.Shaders.Ast.TypeBase)VisitDynamic(castExpression.Target);
            if (!ReferenceEquals(targetTemp, castExpression.Target))
                castExpression.Target = targetTemp;
            return base.Visit(castExpression);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.CompileExpression compileExpression)
        {
            var functionTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(compileExpression.Function);
            if (!ReferenceEquals(functionTemp, compileExpression.Function))
                compileExpression.Function = functionTemp;
            var profileTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(compileExpression.Profile);
            if (!ReferenceEquals(profileTemp, compileExpression.Profile))
                compileExpression.Profile = profileTemp;
            return base.Visit(compileExpression);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.ConstantBuffer constantBuffer)
        {
            VisitList(constantBuffer.Attributes);
            var typeTemp = (Xenko.Core.Shaders.Ast.Hlsl.ConstantBufferType)VisitDynamic(constantBuffer.Type);
            if (!ReferenceEquals(typeTemp, constantBuffer.Type))
                constantBuffer.Type = typeTemp;
            VisitList(constantBuffer.Members);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(constantBuffer.Name);
            if (!ReferenceEquals(nameTemp, constantBuffer.Name))
                constantBuffer.Name = nameTemp;
            var registerTemp = (Xenko.Core.Shaders.Ast.Hlsl.RegisterLocation)VisitDynamic(constantBuffer.Register);
            if (!ReferenceEquals(registerTemp, constantBuffer.Register))
                constantBuffer.Register = registerTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(constantBuffer.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, constantBuffer.Qualifiers))
                constantBuffer.Qualifiers = qualifiersTemp;
            return base.Visit(constantBuffer);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.ConstantBufferType constantBufferType)
        {
            return base.Visit(constantBufferType);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.InterfaceType interfaceType)
        {
            VisitList(interfaceType.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(interfaceType.Name);
            if (!ReferenceEquals(nameTemp, interfaceType.Name))
                interfaceType.Name = nameTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(interfaceType.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, interfaceType.Qualifiers))
                interfaceType.Qualifiers = qualifiersTemp;
            VisitList(interfaceType.GenericParameters);
            VisitList(interfaceType.GenericArguments);
            VisitList(interfaceType.Methods);
            return base.Visit(interfaceType);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.PackOffset packOffset)
        {
            var valueTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(packOffset.Value);
            if (!ReferenceEquals(valueTemp, packOffset.Value))
                packOffset.Value = valueTemp;
            return base.Visit(packOffset);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.Pass pass)
        {
            VisitList(pass.Attributes);
            VisitList(pass.Items);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(pass.Name);
            if (!ReferenceEquals(nameTemp, pass.Name))
                pass.Name = nameTemp;
            return base.Visit(pass);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.RegisterLocation registerLocation)
        {
            var profileTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(registerLocation.Profile);
            if (!ReferenceEquals(profileTemp, registerLocation.Profile))
                registerLocation.Profile = profileTemp;
            var registerTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(registerLocation.Register);
            if (!ReferenceEquals(registerTemp, registerLocation.Register))
                registerLocation.Register = registerTemp;
            return base.Visit(registerLocation);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.Semantic semantic)
        {
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(semantic.Name);
            if (!ReferenceEquals(nameTemp, semantic.Name))
                semantic.Name = nameTemp;
            return base.Visit(semantic);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.StateExpression stateExpression)
        {
            var initializerTemp = (Xenko.Core.Shaders.Ast.Hlsl.StateInitializer)VisitDynamic(stateExpression.Initializer);
            if (!ReferenceEquals(initializerTemp, stateExpression.Initializer))
                stateExpression.Initializer = initializerTemp;
            var stateTypeTemp = (Xenko.Core.Shaders.Ast.TypeBase)VisitDynamic(stateExpression.StateType);
            if (!ReferenceEquals(stateTypeTemp, stateExpression.StateType))
                stateExpression.StateType = stateTypeTemp;
            return base.Visit(stateExpression);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.StateInitializer stateInitializer)
        {
            VisitList(stateInitializer.Items);
            return base.Visit(stateInitializer);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.Technique technique)
        {
            var typeTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(technique.Type);
            if (!ReferenceEquals(typeTemp, technique.Type))
                technique.Type = typeTemp;
            VisitList(technique.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(technique.Name);
            if (!ReferenceEquals(nameTemp, technique.Name))
                technique.Name = nameTemp;
            VisitList(technique.Passes);
            return base.Visit(technique);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.Typedef typedef)
        {
            VisitList(typedef.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(typedef.Name);
            if (!ReferenceEquals(nameTemp, typedef.Name))
                typedef.Name = nameTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(typedef.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, typedef.Qualifiers))
                typedef.Qualifiers = qualifiersTemp;
            VisitList(typedef.SubDeclarators);
            var typeTemp = (Xenko.Core.Shaders.Ast.TypeBase)VisitDynamic(typedef.Type);
            if (!ReferenceEquals(typeTemp, typedef.Type))
                typedef.Type = typeTemp;
            return base.Visit(typedef);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.ExpressionList expressionList)
        {
            VisitList(expressionList.Expressions);
            return base.Visit(expressionList);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.GenericDeclaration genericDeclaration)
        {
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(genericDeclaration.Name);
            if (!ReferenceEquals(nameTemp, genericDeclaration.Name))
                genericDeclaration.Name = nameTemp;
            return base.Visit(genericDeclaration);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.GenericParameterType genericParameterType)
        {
            VisitList(genericParameterType.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(genericParameterType.Name);
            if (!ReferenceEquals(nameTemp, genericParameterType.Name))
                genericParameterType.Name = nameTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(genericParameterType.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, genericParameterType.Qualifiers))
                genericParameterType.Qualifiers = qualifiersTemp;
            return base.Visit(genericParameterType);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.DeclarationStatement declarationStatement)
        {
            VisitList(declarationStatement.Attributes);
            var contentTemp = (Xenko.Core.Shaders.Ast.Node)VisitDynamic(declarationStatement.Content);
            if (!ReferenceEquals(contentTemp, declarationStatement.Content))
                declarationStatement.Content = contentTemp;
            return base.Visit(declarationStatement);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.ExpressionStatement expressionStatement)
        {
            VisitList(expressionStatement.Attributes);
            var expressionTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(expressionStatement.Expression);
            if (!ReferenceEquals(expressionTemp, expressionStatement.Expression))
                expressionStatement.Expression = expressionTemp;
            return base.Visit(expressionStatement);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.ForStatement forStatement)
        {
            VisitList(forStatement.Attributes);
            var startTemp = (Xenko.Core.Shaders.Ast.Statement)VisitDynamic(forStatement.Start);
            if (!ReferenceEquals(startTemp, forStatement.Start))
                forStatement.Start = startTemp;
            var conditionTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(forStatement.Condition);
            if (!ReferenceEquals(conditionTemp, forStatement.Condition))
                forStatement.Condition = conditionTemp;
            var nextTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(forStatement.Next);
            if (!ReferenceEquals(nextTemp, forStatement.Next))
                forStatement.Next = nextTemp;
            var bodyTemp = (Xenko.Core.Shaders.Ast.Statement)VisitDynamic(forStatement.Body);
            if (!ReferenceEquals(bodyTemp, forStatement.Body))
                forStatement.Body = bodyTemp;
            return base.Visit(forStatement);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.GenericType genericType)
        {
            VisitList(genericType.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(genericType.Name);
            if (!ReferenceEquals(nameTemp, genericType.Name))
                genericType.Name = nameTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(genericType.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, genericType.Qualifiers))
                genericType.Qualifiers = qualifiersTemp;
            VisitList(genericType.Parameters);
            return base.Visit(genericType);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Identifier identifier)
        {
            VisitList(identifier.Indices);
            return base.Visit(identifier);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.IfStatement ifStatement)
        {
            VisitList(ifStatement.Attributes);
            var conditionTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(ifStatement.Condition);
            if (!ReferenceEquals(conditionTemp, ifStatement.Condition))
                ifStatement.Condition = conditionTemp;
            var elseTemp = (Xenko.Core.Shaders.Ast.Statement)VisitDynamic(ifStatement.Else);
            if (!ReferenceEquals(elseTemp, ifStatement.Else))
                ifStatement.Else = elseTemp;
            var thenTemp = (Xenko.Core.Shaders.Ast.Statement)VisitDynamic(ifStatement.Then);
            if (!ReferenceEquals(thenTemp, ifStatement.Then))
                ifStatement.Then = thenTemp;
            return base.Visit(ifStatement);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.IndexerExpression indexerExpression)
        {
            var indexTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(indexerExpression.Index);
            if (!ReferenceEquals(indexTemp, indexerExpression.Index))
                indexerExpression.Index = indexTemp;
            var targetTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(indexerExpression.Target);
            if (!ReferenceEquals(targetTemp, indexerExpression.Target))
                indexerExpression.Target = targetTemp;
            return base.Visit(indexerExpression);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.KeywordExpression keywordExpression)
        {
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(keywordExpression.Name);
            if (!ReferenceEquals(nameTemp, keywordExpression.Name))
                keywordExpression.Name = nameTemp;
            return base.Visit(keywordExpression);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Literal literal)
        {
            VisitList(literal.SubLiterals);
            return base.Visit(literal);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.LiteralExpression literalExpression)
        {
            var literalTemp = (Xenko.Core.Shaders.Ast.Literal)VisitDynamic(literalExpression.Literal);
            if (!ReferenceEquals(literalTemp, literalExpression.Literal))
                literalExpression.Literal = literalTemp;
            return base.Visit(literalExpression);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.MatrixType matrixType)
        {
            VisitList(matrixType.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(matrixType.Name);
            if (!ReferenceEquals(nameTemp, matrixType.Name))
                matrixType.Name = nameTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(matrixType.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, matrixType.Qualifiers))
                matrixType.Qualifiers = qualifiersTemp;
            var typeTemp = (Xenko.Core.Shaders.Ast.TypeBase)VisitDynamic(matrixType.Type);
            if (!ReferenceEquals(typeTemp, matrixType.Type))
                matrixType.Type = typeTemp;
            return base.Visit(matrixType);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.MemberReferenceExpression memberReferenceExpression)
        {
            var memberTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(memberReferenceExpression.Member);
            if (!ReferenceEquals(memberTemp, memberReferenceExpression.Member))
                memberReferenceExpression.Member = memberTemp;
            var targetTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(memberReferenceExpression.Target);
            if (!ReferenceEquals(targetTemp, memberReferenceExpression.Target))
                memberReferenceExpression.Target = targetTemp;
            return base.Visit(memberReferenceExpression);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.MethodDeclaration methodDeclaration)
        {
            VisitList(methodDeclaration.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(methodDeclaration.Name);
            if (!ReferenceEquals(nameTemp, methodDeclaration.Name))
                methodDeclaration.Name = nameTemp;
            VisitList(methodDeclaration.Parameters);
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(methodDeclaration.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, methodDeclaration.Qualifiers))
                methodDeclaration.Qualifiers = qualifiersTemp;
            var returnTypeTemp = (Xenko.Core.Shaders.Ast.TypeBase)VisitDynamic(methodDeclaration.ReturnType);
            if (!ReferenceEquals(returnTypeTemp, methodDeclaration.ReturnType))
                methodDeclaration.ReturnType = returnTypeTemp;
            return base.Visit(methodDeclaration);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.MethodDefinition methodDefinition)
        {
            VisitList(methodDefinition.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(methodDefinition.Name);
            if (!ReferenceEquals(nameTemp, methodDefinition.Name))
                methodDefinition.Name = nameTemp;
            VisitList(methodDefinition.Parameters);
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(methodDefinition.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, methodDefinition.Qualifiers))
                methodDefinition.Qualifiers = qualifiersTemp;
            var returnTypeTemp = (Xenko.Core.Shaders.Ast.TypeBase)VisitDynamic(methodDefinition.ReturnType);
            if (!ReferenceEquals(returnTypeTemp, methodDefinition.ReturnType))
                methodDefinition.ReturnType = returnTypeTemp;
            var bodyTemp = (Xenko.Core.Shaders.Ast.StatementList)VisitDynamic(methodDefinition.Body);
            if (!ReferenceEquals(bodyTemp, methodDefinition.Body))
                methodDefinition.Body = bodyTemp;
            return base.Visit(methodDefinition);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.MethodInvocationExpression methodInvocationExpression)
        {
            var targetTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(methodInvocationExpression.Target);
            if (!ReferenceEquals(targetTemp, methodInvocationExpression.Target))
                methodInvocationExpression.Target = targetTemp;
            VisitList(methodInvocationExpression.Arguments);
            return base.Visit(methodInvocationExpression);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.ObjectType objectType)
        {
            VisitList(objectType.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(objectType.Name);
            if (!ReferenceEquals(nameTemp, objectType.Name))
                objectType.Name = nameTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(objectType.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, objectType.Qualifiers))
                objectType.Qualifiers = qualifiersTemp;
            return base.Visit(objectType);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Parameter parameter)
        {
            VisitList(parameter.Attributes);
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(parameter.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, parameter.Qualifiers))
                parameter.Qualifiers = qualifiersTemp;
            var typeTemp = (Xenko.Core.Shaders.Ast.TypeBase)VisitDynamic(parameter.Type);
            if (!ReferenceEquals(typeTemp, parameter.Type))
                parameter.Type = typeTemp;
            var initialValueTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(parameter.InitialValue);
            if (!ReferenceEquals(initialValueTemp, parameter.InitialValue))
                parameter.InitialValue = initialValueTemp;
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(parameter.Name);
            if (!ReferenceEquals(nameTemp, parameter.Name))
                parameter.Name = nameTemp;
            VisitList(parameter.SubVariables);
            return base.Visit(parameter);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.ParenthesizedExpression parenthesizedExpression)
        {
            var contentTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(parenthesizedExpression.Content);
            if (!ReferenceEquals(contentTemp, parenthesizedExpression.Content))
                parenthesizedExpression.Content = contentTemp;
            return base.Visit(parenthesizedExpression);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Qualifier qualifier)
        {
            return base.Visit(qualifier);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.ReturnStatement returnStatement)
        {
            VisitList(returnStatement.Attributes);
            var valueTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(returnStatement.Value);
            if (!ReferenceEquals(valueTemp, returnStatement.Value))
                returnStatement.Value = valueTemp;
            return base.Visit(returnStatement);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.ScalarType scalarType)
        {
            VisitList(scalarType.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(scalarType.Name);
            if (!ReferenceEquals(nameTemp, scalarType.Name))
                scalarType.Name = nameTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(scalarType.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, scalarType.Qualifiers))
                scalarType.Qualifiers = qualifiersTemp;
            return base.Visit(scalarType);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Shader shader)
        {
            VisitList(shader.Declarations);
            return base.Visit(shader);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.StatementList statementList)
        {
            VisitList(statementList.Attributes);
            VisitList(statementList.Statements);
            return base.Visit(statementList);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.StructType structType)
        {
            VisitList(structType.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(structType.Name);
            if (!ReferenceEquals(nameTemp, structType.Name))
                structType.Name = nameTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(structType.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, structType.Qualifiers))
                structType.Qualifiers = qualifiersTemp;
            VisitList(structType.Fields);
            return base.Visit(structType);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.SwitchCaseGroup switchCaseGroup)
        {
            VisitList(switchCaseGroup.Cases);
            var statementsTemp = (Xenko.Core.Shaders.Ast.StatementList)VisitDynamic(switchCaseGroup.Statements);
            if (!ReferenceEquals(statementsTemp, switchCaseGroup.Statements))
                switchCaseGroup.Statements = statementsTemp;
            return base.Visit(switchCaseGroup);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.SwitchStatement switchStatement)
        {
            VisitList(switchStatement.Attributes);
            var conditionTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(switchStatement.Condition);
            if (!ReferenceEquals(conditionTemp, switchStatement.Condition))
                switchStatement.Condition = conditionTemp;
            VisitList(switchStatement.Groups);
            return base.Visit(switchStatement);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.TypeName typeName)
        {
            VisitList(typeName.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(typeName.Name);
            if (!ReferenceEquals(nameTemp, typeName.Name))
                typeName.Name = nameTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(typeName.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, typeName.Qualifiers))
                typeName.Qualifiers = qualifiersTemp;
            return base.Visit(typeName);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.TypeReferenceExpression typeReferenceExpression)
        {
            var typeTemp = (Xenko.Core.Shaders.Ast.TypeBase)VisitDynamic(typeReferenceExpression.Type);
            if (!ReferenceEquals(typeTemp, typeReferenceExpression.Type))
                typeReferenceExpression.Type = typeTemp;
            return base.Visit(typeReferenceExpression);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.UnaryExpression unaryExpression)
        {
            var expressionTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(unaryExpression.Expression);
            if (!ReferenceEquals(expressionTemp, unaryExpression.Expression))
                unaryExpression.Expression = expressionTemp;
            return base.Visit(unaryExpression);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Variable variable)
        {
            VisitList(variable.Attributes);
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(variable.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, variable.Qualifiers))
                variable.Qualifiers = qualifiersTemp;
            var typeTemp = (Xenko.Core.Shaders.Ast.TypeBase)VisitDynamic(variable.Type);
            if (!ReferenceEquals(typeTemp, variable.Type))
                variable.Type = typeTemp;
            var initialValueTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(variable.InitialValue);
            if (!ReferenceEquals(initialValueTemp, variable.InitialValue))
                variable.InitialValue = initialValueTemp;
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(variable.Name);
            if (!ReferenceEquals(nameTemp, variable.Name))
                variable.Name = nameTemp;
            VisitList(variable.SubVariables);
            return base.Visit(variable);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.VariableReferenceExpression variableReferenceExpression)
        {
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(variableReferenceExpression.Name);
            if (!ReferenceEquals(nameTemp, variableReferenceExpression.Name))
                variableReferenceExpression.Name = nameTemp;
            return base.Visit(variableReferenceExpression);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.VectorType vectorType)
        {
            VisitList(vectorType.Attributes);
            var nameTemp = (Xenko.Core.Shaders.Ast.Identifier)VisitDynamic(vectorType.Name);
            if (!ReferenceEquals(nameTemp, vectorType.Name))
                vectorType.Name = nameTemp;
            var qualifiersTemp = (Xenko.Core.Shaders.Ast.Qualifier)VisitDynamic(vectorType.Qualifiers);
            if (!ReferenceEquals(qualifiersTemp, vectorType.Qualifiers))
                vectorType.Qualifiers = qualifiersTemp;
            var typeTemp = (Xenko.Core.Shaders.Ast.TypeBase)VisitDynamic(vectorType.Type);
            if (!ReferenceEquals(typeTemp, vectorType.Type))
                vectorType.Type = typeTemp;
            return base.Visit(vectorType);
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.WhileStatement whileStatement)
        {
            VisitList(whileStatement.Attributes);
            var conditionTemp = (Xenko.Core.Shaders.Ast.Expression)VisitDynamic(whileStatement.Condition);
            if (!ReferenceEquals(conditionTemp, whileStatement.Condition))
                whileStatement.Condition = conditionTemp;
            var statementTemp = (Xenko.Core.Shaders.Ast.Statement)VisitDynamic(whileStatement.Statement);
            if (!ReferenceEquals(statementTemp, whileStatement.Statement))
                whileStatement.Statement = statementTemp;
            return base.Visit(whileStatement);
        }
    }

    public partial class ShaderCloner
    {
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.ClassIdentifierGeneric classIdentifierGeneric)
        {
            classIdentifierGeneric = (Xenko.Core.Shaders.Ast.Xenko.ClassIdentifierGeneric)base.Visit(classIdentifierGeneric);
            return new Xenko.Core.Shaders.Ast.Xenko.ClassIdentifierGeneric
            {
                Span = classIdentifierGeneric.Span,
                Tags = classIdentifierGeneric.Tags,
                Indices = classIdentifierGeneric.Indices,
                IsSpecialReference = classIdentifierGeneric.IsSpecialReference,
                Text = classIdentifierGeneric.Text,
                Generics = classIdentifierGeneric.Generics,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.EnumType enumType)
        {
            enumType = (Xenko.Core.Shaders.Ast.Xenko.EnumType)base.Visit(enumType);
            return new Xenko.Core.Shaders.Ast.Xenko.EnumType
            {
                Span = enumType.Span,
                Tags = enumType.Tags,
                Attributes = enumType.Attributes,
                TypeInference = enumType.TypeInference,
                Name = enumType.Name,
                Qualifiers = enumType.Qualifiers,
                IsBuiltIn = enumType.IsBuiltIn,
                Values = enumType.Values,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.ForEachStatement forEachStatement)
        {
            forEachStatement = (Xenko.Core.Shaders.Ast.Xenko.ForEachStatement)base.Visit(forEachStatement);
            return new Xenko.Core.Shaders.Ast.Xenko.ForEachStatement
            {
                Span = forEachStatement.Span,
                Tags = forEachStatement.Tags,
                Attributes = forEachStatement.Attributes,
                Collection = forEachStatement.Collection,
                Variable = forEachStatement.Variable,
                Body = forEachStatement.Body,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.ImportBlockStatement importBlockStatement)
        {
            importBlockStatement = (Xenko.Core.Shaders.Ast.Xenko.ImportBlockStatement)base.Visit(importBlockStatement);
            return new Xenko.Core.Shaders.Ast.Xenko.ImportBlockStatement
            {
                Span = importBlockStatement.Span,
                Tags = importBlockStatement.Tags,
                Attributes = importBlockStatement.Attributes,
                Statements = importBlockStatement.Statements,
                Name = importBlockStatement.Name,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.LinkType linkType)
        {
            linkType = (Xenko.Core.Shaders.Ast.Xenko.LinkType)base.Visit(linkType);
            return new Xenko.Core.Shaders.Ast.Xenko.LinkType
            {
                Span = linkType.Span,
                Tags = linkType.Tags,
                Attributes = linkType.Attributes,
                TypeInference = linkType.TypeInference,
                Name = linkType.Name,
                Qualifiers = linkType.Qualifiers,
                IsBuiltIn = linkType.IsBuiltIn,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.LiteralIdentifier literalIdentifier)
        {
            literalIdentifier = (Xenko.Core.Shaders.Ast.Xenko.LiteralIdentifier)base.Visit(literalIdentifier);
            return new Xenko.Core.Shaders.Ast.Xenko.LiteralIdentifier
            {
                Span = literalIdentifier.Span,
                Tags = literalIdentifier.Tags,
                Indices = literalIdentifier.Indices,
                IsSpecialReference = literalIdentifier.IsSpecialReference,
                Text = literalIdentifier.Text,
                Value = literalIdentifier.Value,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.MemberName memberName)
        {
            memberName = (Xenko.Core.Shaders.Ast.Xenko.MemberName)base.Visit(memberName);
            return new Xenko.Core.Shaders.Ast.Xenko.MemberName
            {
                Span = memberName.Span,
                Tags = memberName.Tags,
                Attributes = memberName.Attributes,
                TypeInference = memberName.TypeInference,
                Name = memberName.Name,
                Qualifiers = memberName.Qualifiers,
                IsBuiltIn = memberName.IsBuiltIn,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.MixinStatement mixinStatement)
        {
            mixinStatement = (Xenko.Core.Shaders.Ast.Xenko.MixinStatement)base.Visit(mixinStatement);
            return new Xenko.Core.Shaders.Ast.Xenko.MixinStatement
            {
                Span = mixinStatement.Span,
                Tags = mixinStatement.Tags,
                Attributes = mixinStatement.Attributes,
                Type = mixinStatement.Type,
                Value = mixinStatement.Value,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.NamespaceBlock namespaceBlock)
        {
            namespaceBlock = (Xenko.Core.Shaders.Ast.Xenko.NamespaceBlock)base.Visit(namespaceBlock);
            return new Xenko.Core.Shaders.Ast.Xenko.NamespaceBlock
            {
                Span = namespaceBlock.Span,
                Tags = namespaceBlock.Tags,
                Attributes = namespaceBlock.Attributes,
                TypeInference = namespaceBlock.TypeInference,
                Name = namespaceBlock.Name,
                Qualifiers = namespaceBlock.Qualifiers,
                IsBuiltIn = namespaceBlock.IsBuiltIn,
                Body = namespaceBlock.Body,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.ParametersBlock parametersBlock)
        {
            parametersBlock = (Xenko.Core.Shaders.Ast.Xenko.ParametersBlock)base.Visit(parametersBlock);
            return new Xenko.Core.Shaders.Ast.Xenko.ParametersBlock
            {
                Span = parametersBlock.Span,
                Tags = parametersBlock.Tags,
                Name = parametersBlock.Name,
                Body = parametersBlock.Body,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.SemanticType semanticType)
        {
            semanticType = (Xenko.Core.Shaders.Ast.Xenko.SemanticType)base.Visit(semanticType);
            return new Xenko.Core.Shaders.Ast.Xenko.SemanticType
            {
                Span = semanticType.Span,
                Tags = semanticType.Tags,
                Attributes = semanticType.Attributes,
                TypeInference = semanticType.TypeInference,
                Name = semanticType.Name,
                Qualifiers = semanticType.Qualifiers,
                IsBuiltIn = semanticType.IsBuiltIn,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.EffectBlock effectBlock)
        {
            effectBlock = (Xenko.Core.Shaders.Ast.Xenko.EffectBlock)base.Visit(effectBlock);
            return new Xenko.Core.Shaders.Ast.Xenko.EffectBlock
            {
                Span = effectBlock.Span,
                Tags = effectBlock.Tags,
                Attributes = effectBlock.Attributes,
                TypeInference = effectBlock.TypeInference,
                Name = effectBlock.Name,
                Qualifiers = effectBlock.Qualifiers,
                IsBuiltIn = effectBlock.IsBuiltIn,
                IsPartial = effectBlock.IsPartial,
                Body = effectBlock.Body,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.ShaderClassType shaderClassType)
        {
            shaderClassType = (Xenko.Core.Shaders.Ast.Xenko.ShaderClassType)base.Visit(shaderClassType);
            return new Xenko.Core.Shaders.Ast.Xenko.ShaderClassType
            {
                Span = shaderClassType.Span,
                Tags = shaderClassType.Tags,
                Attributes = shaderClassType.Attributes,
                TypeInference = shaderClassType.TypeInference,
                Name = shaderClassType.Name,
                Qualifiers = shaderClassType.Qualifiers,
                IsBuiltIn = shaderClassType.IsBuiltIn,
                AlternativeNames = shaderClassType.AlternativeNames,
                BaseClasses = shaderClassType.BaseClasses,
                GenericParameters = shaderClassType.GenericParameters,
                GenericArguments = shaderClassType.GenericArguments,
                Members = shaderClassType.Members,
                ShaderGenerics = shaderClassType.ShaderGenerics,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.ShaderRootClassType shaderRootClassType)
        {
            shaderRootClassType = (Xenko.Core.Shaders.Ast.Xenko.ShaderRootClassType)base.Visit(shaderRootClassType);
            return new Xenko.Core.Shaders.Ast.Xenko.ShaderRootClassType
            {
                Span = shaderRootClassType.Span,
                Tags = shaderRootClassType.Tags,
                Attributes = shaderRootClassType.Attributes,
                TypeInference = shaderRootClassType.TypeInference,
                Name = shaderRootClassType.Name,
                Qualifiers = shaderRootClassType.Qualifiers,
                IsBuiltIn = shaderRootClassType.IsBuiltIn,
                AlternativeNames = shaderRootClassType.AlternativeNames,
                BaseClasses = shaderRootClassType.BaseClasses,
                GenericParameters = shaderRootClassType.GenericParameters,
                GenericArguments = shaderRootClassType.GenericArguments,
                Members = shaderRootClassType.Members,
                ShaderGenerics = shaderRootClassType.ShaderGenerics,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.ShaderTypeName shaderTypeName)
        {
            shaderTypeName = (Xenko.Core.Shaders.Ast.Xenko.ShaderTypeName)base.Visit(shaderTypeName);
            return new Xenko.Core.Shaders.Ast.Xenko.ShaderTypeName
            {
                Span = shaderTypeName.Span,
                Tags = shaderTypeName.Tags,
                Attributes = shaderTypeName.Attributes,
                TypeInference = shaderTypeName.TypeInference,
                Name = shaderTypeName.Name,
                Qualifiers = shaderTypeName.Qualifiers,
                IsBuiltIn = shaderTypeName.IsBuiltIn,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.TypeIdentifier typeIdentifier)
        {
            typeIdentifier = (Xenko.Core.Shaders.Ast.Xenko.TypeIdentifier)base.Visit(typeIdentifier);
            return new Xenko.Core.Shaders.Ast.Xenko.TypeIdentifier
            {
                Span = typeIdentifier.Span,
                Tags = typeIdentifier.Tags,
                Indices = typeIdentifier.Indices,
                IsSpecialReference = typeIdentifier.IsSpecialReference,
                Text = typeIdentifier.Text,
                Type = typeIdentifier.Type,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.UsingParametersStatement usingParametersStatement)
        {
            usingParametersStatement = (Xenko.Core.Shaders.Ast.Xenko.UsingParametersStatement)base.Visit(usingParametersStatement);
            return new Xenko.Core.Shaders.Ast.Xenko.UsingParametersStatement
            {
                Span = usingParametersStatement.Span,
                Tags = usingParametersStatement.Tags,
                Attributes = usingParametersStatement.Attributes,
                Name = usingParametersStatement.Name,
                Body = usingParametersStatement.Body,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.UsingStatement usingStatement)
        {
            usingStatement = (Xenko.Core.Shaders.Ast.Xenko.UsingStatement)base.Visit(usingStatement);
            return new Xenko.Core.Shaders.Ast.Xenko.UsingStatement
            {
                Span = usingStatement.Span,
                Tags = usingStatement.Tags,
                Attributes = usingStatement.Attributes,
                Name = usingStatement.Name,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.VarType varType)
        {
            varType = (Xenko.Core.Shaders.Ast.Xenko.VarType)base.Visit(varType);
            return new Xenko.Core.Shaders.Ast.Xenko.VarType
            {
                Span = varType.Span,
                Tags = varType.Tags,
                Attributes = varType.Attributes,
                TypeInference = varType.TypeInference,
                Name = varType.Name,
                Qualifiers = varType.Qualifiers,
                IsBuiltIn = varType.IsBuiltIn,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Xenko.XenkoConstantBufferType xenkoConstantBufferType)
        {
            xenkoConstantBufferType = (Xenko.Core.Shaders.Ast.Xenko.XenkoConstantBufferType)base.Visit(xenkoConstantBufferType);
            return new Xenko.Core.Shaders.Ast.Xenko.XenkoConstantBufferType
            {
                Span = xenkoConstantBufferType.Span,
                Tags = xenkoConstantBufferType.Tags,
                IsFlag = xenkoConstantBufferType.IsFlag,
                Key = xenkoConstantBufferType.Key,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.ArrayInitializerExpression arrayInitializerExpression)
        {
            arrayInitializerExpression = (Xenko.Core.Shaders.Ast.ArrayInitializerExpression)base.Visit(arrayInitializerExpression);
            return new Xenko.Core.Shaders.Ast.ArrayInitializerExpression
            {
                Span = arrayInitializerExpression.Span,
                Tags = arrayInitializerExpression.Tags,
                TypeInference = arrayInitializerExpression.TypeInference,
                Items = arrayInitializerExpression.Items,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.ArrayType arrayType)
        {
            arrayType = (Xenko.Core.Shaders.Ast.ArrayType)base.Visit(arrayType);
            return new Xenko.Core.Shaders.Ast.ArrayType
            {
                Span = arrayType.Span,
                Tags = arrayType.Tags,
                Attributes = arrayType.Attributes,
                TypeInference = arrayType.TypeInference,
                Name = arrayType.Name,
                Qualifiers = arrayType.Qualifiers,
                IsBuiltIn = arrayType.IsBuiltIn,
                Dimensions = arrayType.Dimensions,
                Type = arrayType.Type,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.AssignmentExpression assignmentExpression)
        {
            assignmentExpression = (Xenko.Core.Shaders.Ast.AssignmentExpression)base.Visit(assignmentExpression);
            return new Xenko.Core.Shaders.Ast.AssignmentExpression
            {
                Span = assignmentExpression.Span,
                Tags = assignmentExpression.Tags,
                TypeInference = assignmentExpression.TypeInference,
                Operator = assignmentExpression.Operator,
                Target = assignmentExpression.Target,
                Value = assignmentExpression.Value,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.BinaryExpression binaryExpression)
        {
            binaryExpression = (Xenko.Core.Shaders.Ast.BinaryExpression)base.Visit(binaryExpression);
            return new Xenko.Core.Shaders.Ast.BinaryExpression
            {
                Span = binaryExpression.Span,
                Tags = binaryExpression.Tags,
                TypeInference = binaryExpression.TypeInference,
                Left = binaryExpression.Left,
                Operator = binaryExpression.Operator,
                Right = binaryExpression.Right,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.BlockStatement blockStatement)
        {
            blockStatement = (Xenko.Core.Shaders.Ast.BlockStatement)base.Visit(blockStatement);
            return new Xenko.Core.Shaders.Ast.BlockStatement
            {
                Span = blockStatement.Span,
                Tags = blockStatement.Tags,
                Attributes = blockStatement.Attributes,
                Statements = blockStatement.Statements,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.CaseStatement caseStatement)
        {
            caseStatement = (Xenko.Core.Shaders.Ast.CaseStatement)base.Visit(caseStatement);
            return new Xenko.Core.Shaders.Ast.CaseStatement
            {
                Span = caseStatement.Span,
                Tags = caseStatement.Tags,
                Attributes = caseStatement.Attributes,
                Case = caseStatement.Case,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.CompositeEnum compositeEnum)
        {
            compositeEnum = (Xenko.Core.Shaders.Ast.CompositeEnum)base.Visit(compositeEnum);
            return new Xenko.Core.Shaders.Ast.CompositeEnum
            {
                Span = compositeEnum.Span,
                Tags = compositeEnum.Tags,
                IsFlag = compositeEnum.IsFlag,
                Key = compositeEnum.Key,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.ConditionalExpression conditionalExpression)
        {
            conditionalExpression = (Xenko.Core.Shaders.Ast.ConditionalExpression)base.Visit(conditionalExpression);
            return new Xenko.Core.Shaders.Ast.ConditionalExpression
            {
                Span = conditionalExpression.Span,
                Tags = conditionalExpression.Tags,
                TypeInference = conditionalExpression.TypeInference,
                Condition = conditionalExpression.Condition,
                Left = conditionalExpression.Left,
                Right = conditionalExpression.Right,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.EmptyStatement emptyStatement)
        {
            emptyStatement = (Xenko.Core.Shaders.Ast.EmptyStatement)base.Visit(emptyStatement);
            return new Xenko.Core.Shaders.Ast.EmptyStatement
            {
                Span = emptyStatement.Span,
                Tags = emptyStatement.Tags,
                Attributes = emptyStatement.Attributes,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.EmptyExpression emptyExpression)
        {
            emptyExpression = (Xenko.Core.Shaders.Ast.EmptyExpression)base.Visit(emptyExpression);
            return new Xenko.Core.Shaders.Ast.EmptyExpression
            {
                Span = emptyExpression.Span,
                Tags = emptyExpression.Tags,
                TypeInference = emptyExpression.TypeInference,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Glsl.LayoutKeyValue layoutKeyValue)
        {
            layoutKeyValue = (Xenko.Core.Shaders.Ast.Glsl.LayoutKeyValue)base.Visit(layoutKeyValue);
            return new Xenko.Core.Shaders.Ast.Glsl.LayoutKeyValue
            {
                Span = layoutKeyValue.Span,
                Tags = layoutKeyValue.Tags,
                Name = layoutKeyValue.Name,
                Value = layoutKeyValue.Value,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Glsl.LayoutQualifier layoutQualifier)
        {
            layoutQualifier = (Xenko.Core.Shaders.Ast.Glsl.LayoutQualifier)base.Visit(layoutQualifier);
            return new Xenko.Core.Shaders.Ast.Glsl.LayoutQualifier
            {
                Span = layoutQualifier.Span,
                Tags = layoutQualifier.Tags,
                IsFlag = layoutQualifier.IsFlag,
                Key = layoutQualifier.Key,
                IsPost = layoutQualifier.IsPost,
                Layouts = layoutQualifier.Layouts,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Glsl.InterfaceType interfaceType)
        {
            interfaceType = (Xenko.Core.Shaders.Ast.Glsl.InterfaceType)base.Visit(interfaceType);
            return new Xenko.Core.Shaders.Ast.Glsl.InterfaceType
            {
                Span = interfaceType.Span,
                Tags = interfaceType.Tags,
                Attributes = interfaceType.Attributes,
                TypeInference = interfaceType.TypeInference,
                Name = interfaceType.Name,
                Qualifiers = interfaceType.Qualifiers,
                IsBuiltIn = interfaceType.IsBuiltIn,
                Fields = interfaceType.Fields,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.ClassType classType)
        {
            classType = (Xenko.Core.Shaders.Ast.Hlsl.ClassType)base.Visit(classType);
            return new Xenko.Core.Shaders.Ast.Hlsl.ClassType
            {
                Span = classType.Span,
                Tags = classType.Tags,
                Attributes = classType.Attributes,
                TypeInference = classType.TypeInference,
                Name = classType.Name,
                Qualifiers = classType.Qualifiers,
                IsBuiltIn = classType.IsBuiltIn,
                AlternativeNames = classType.AlternativeNames,
                BaseClasses = classType.BaseClasses,
                GenericParameters = classType.GenericParameters,
                GenericArguments = classType.GenericArguments,
                Members = classType.Members,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.IdentifierGeneric identifierGeneric)
        {
            identifierGeneric = (Xenko.Core.Shaders.Ast.Hlsl.IdentifierGeneric)base.Visit(identifierGeneric);
            return new Xenko.Core.Shaders.Ast.Hlsl.IdentifierGeneric
            {
                Span = identifierGeneric.Span,
                Tags = identifierGeneric.Tags,
                Indices = identifierGeneric.Indices,
                IsSpecialReference = identifierGeneric.IsSpecialReference,
                Text = identifierGeneric.Text,
                Identifiers = identifierGeneric.Identifiers,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.IdentifierNs identifierNs)
        {
            identifierNs = (Xenko.Core.Shaders.Ast.Hlsl.IdentifierNs)base.Visit(identifierNs);
            return new Xenko.Core.Shaders.Ast.Hlsl.IdentifierNs
            {
                Span = identifierNs.Span,
                Tags = identifierNs.Tags,
                Indices = identifierNs.Indices,
                IsSpecialReference = identifierNs.IsSpecialReference,
                Text = identifierNs.Text,
                Identifiers = identifierNs.Identifiers,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.IdentifierDot identifierDot)
        {
            identifierDot = (Xenko.Core.Shaders.Ast.Hlsl.IdentifierDot)base.Visit(identifierDot);
            return new Xenko.Core.Shaders.Ast.Hlsl.IdentifierDot
            {
                Span = identifierDot.Span,
                Tags = identifierDot.Tags,
                Indices = identifierDot.Indices,
                IsSpecialReference = identifierDot.IsSpecialReference,
                Text = identifierDot.Text,
                Identifiers = identifierDot.Identifiers,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.TextureType textureType)
        {
            textureType = (Xenko.Core.Shaders.Ast.Hlsl.TextureType)base.Visit(textureType);
            return new Xenko.Core.Shaders.Ast.Hlsl.TextureType
            {
                Span = textureType.Span,
                Tags = textureType.Tags,
                Attributes = textureType.Attributes,
                TypeInference = textureType.TypeInference,
                Name = textureType.Name,
                Qualifiers = textureType.Qualifiers,
                IsBuiltIn = textureType.IsBuiltIn,
                AlternativeNames = textureType.AlternativeNames,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.Annotations annotations)
        {
            annotations = (Xenko.Core.Shaders.Ast.Hlsl.Annotations)base.Visit(annotations);
            return new Xenko.Core.Shaders.Ast.Hlsl.Annotations
            {
                Span = annotations.Span,
                Tags = annotations.Tags,
                Variables = annotations.Variables,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.AsmExpression asmExpression)
        {
            asmExpression = (Xenko.Core.Shaders.Ast.Hlsl.AsmExpression)base.Visit(asmExpression);
            return new Xenko.Core.Shaders.Ast.Hlsl.AsmExpression
            {
                Span = asmExpression.Span,
                Tags = asmExpression.Tags,
                TypeInference = asmExpression.TypeInference,
                Text = asmExpression.Text,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.AttributeDeclaration attributeDeclaration)
        {
            attributeDeclaration = (Xenko.Core.Shaders.Ast.Hlsl.AttributeDeclaration)base.Visit(attributeDeclaration);
            return new Xenko.Core.Shaders.Ast.Hlsl.AttributeDeclaration
            {
                Span = attributeDeclaration.Span,
                Tags = attributeDeclaration.Tags,
                Name = attributeDeclaration.Name,
                Parameters = attributeDeclaration.Parameters,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.CastExpression castExpression)
        {
            castExpression = (Xenko.Core.Shaders.Ast.Hlsl.CastExpression)base.Visit(castExpression);
            return new Xenko.Core.Shaders.Ast.Hlsl.CastExpression
            {
                Span = castExpression.Span,
                Tags = castExpression.Tags,
                TypeInference = castExpression.TypeInference,
                From = castExpression.From,
                Target = castExpression.Target,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.CompileExpression compileExpression)
        {
            compileExpression = (Xenko.Core.Shaders.Ast.Hlsl.CompileExpression)base.Visit(compileExpression);
            return new Xenko.Core.Shaders.Ast.Hlsl.CompileExpression
            {
                Span = compileExpression.Span,
                Tags = compileExpression.Tags,
                TypeInference = compileExpression.TypeInference,
                Function = compileExpression.Function,
                Profile = compileExpression.Profile,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.ConstantBuffer constantBuffer)
        {
            constantBuffer = (Xenko.Core.Shaders.Ast.Hlsl.ConstantBuffer)base.Visit(constantBuffer);
            return new Xenko.Core.Shaders.Ast.Hlsl.ConstantBuffer
            {
                Span = constantBuffer.Span,
                Tags = constantBuffer.Tags,
                Attributes = constantBuffer.Attributes,
                Type = constantBuffer.Type,
                Members = constantBuffer.Members,
                Name = constantBuffer.Name,
                Register = constantBuffer.Register,
                Qualifiers = constantBuffer.Qualifiers,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.ConstantBufferType constantBufferType)
        {
            constantBufferType = (Xenko.Core.Shaders.Ast.Hlsl.ConstantBufferType)base.Visit(constantBufferType);
            return new Xenko.Core.Shaders.Ast.Hlsl.ConstantBufferType
            {
                Span = constantBufferType.Span,
                Tags = constantBufferType.Tags,
                IsFlag = constantBufferType.IsFlag,
                Key = constantBufferType.Key,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.InterfaceType interfaceType)
        {
            interfaceType = (Xenko.Core.Shaders.Ast.Hlsl.InterfaceType)base.Visit(interfaceType);
            return new Xenko.Core.Shaders.Ast.Hlsl.InterfaceType
            {
                Span = interfaceType.Span,
                Tags = interfaceType.Tags,
                Attributes = interfaceType.Attributes,
                TypeInference = interfaceType.TypeInference,
                Name = interfaceType.Name,
                Qualifiers = interfaceType.Qualifiers,
                IsBuiltIn = interfaceType.IsBuiltIn,
                AlternativeNames = interfaceType.AlternativeNames,
                GenericParameters = interfaceType.GenericParameters,
                GenericArguments = interfaceType.GenericArguments,
                Methods = interfaceType.Methods,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.PackOffset packOffset)
        {
            packOffset = (Xenko.Core.Shaders.Ast.Hlsl.PackOffset)base.Visit(packOffset);
            return new Xenko.Core.Shaders.Ast.Hlsl.PackOffset
            {
                Span = packOffset.Span,
                Tags = packOffset.Tags,
                IsFlag = packOffset.IsFlag,
                Key = packOffset.Key,
                IsPost = packOffset.IsPost,
                Value = packOffset.Value,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.Pass pass)
        {
            pass = (Xenko.Core.Shaders.Ast.Hlsl.Pass)base.Visit(pass);
            return new Xenko.Core.Shaders.Ast.Hlsl.Pass
            {
                Span = pass.Span,
                Tags = pass.Tags,
                Attributes = pass.Attributes,
                Items = pass.Items,
                Name = pass.Name,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.RegisterLocation registerLocation)
        {
            registerLocation = (Xenko.Core.Shaders.Ast.Hlsl.RegisterLocation)base.Visit(registerLocation);
            return new Xenko.Core.Shaders.Ast.Hlsl.RegisterLocation
            {
                Span = registerLocation.Span,
                Tags = registerLocation.Tags,
                IsFlag = registerLocation.IsFlag,
                Key = registerLocation.Key,
                IsPost = registerLocation.IsPost,
                Profile = registerLocation.Profile,
                Register = registerLocation.Register,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.Semantic semantic)
        {
            semantic = (Xenko.Core.Shaders.Ast.Hlsl.Semantic)base.Visit(semantic);
            return new Xenko.Core.Shaders.Ast.Hlsl.Semantic
            {
                Span = semantic.Span,
                Tags = semantic.Tags,
                IsFlag = semantic.IsFlag,
                Key = semantic.Key,
                IsPost = semantic.IsPost,
                Name = semantic.Name,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.StateExpression stateExpression)
        {
            stateExpression = (Xenko.Core.Shaders.Ast.Hlsl.StateExpression)base.Visit(stateExpression);
            return new Xenko.Core.Shaders.Ast.Hlsl.StateExpression
            {
                Span = stateExpression.Span,
                Tags = stateExpression.Tags,
                TypeInference = stateExpression.TypeInference,
                Initializer = stateExpression.Initializer,
                StateType = stateExpression.StateType,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.StateInitializer stateInitializer)
        {
            stateInitializer = (Xenko.Core.Shaders.Ast.Hlsl.StateInitializer)base.Visit(stateInitializer);
            return new Xenko.Core.Shaders.Ast.Hlsl.StateInitializer
            {
                Span = stateInitializer.Span,
                Tags = stateInitializer.Tags,
                TypeInference = stateInitializer.TypeInference,
                Items = stateInitializer.Items,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.Technique technique)
        {
            technique = (Xenko.Core.Shaders.Ast.Hlsl.Technique)base.Visit(technique);
            return new Xenko.Core.Shaders.Ast.Hlsl.Technique
            {
                Span = technique.Span,
                Tags = technique.Tags,
                Type = technique.Type,
                Attributes = technique.Attributes,
                Name = technique.Name,
                Passes = technique.Passes,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Hlsl.Typedef typedef)
        {
            typedef = (Xenko.Core.Shaders.Ast.Hlsl.Typedef)base.Visit(typedef);
            return new Xenko.Core.Shaders.Ast.Hlsl.Typedef
            {
                Span = typedef.Span,
                Tags = typedef.Tags,
                Attributes = typedef.Attributes,
                TypeInference = typedef.TypeInference,
                Name = typedef.Name,
                Qualifiers = typedef.Qualifiers,
                IsBuiltIn = typedef.IsBuiltIn,
                SubDeclarators = typedef.SubDeclarators,
                Type = typedef.Type,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.ExpressionList expressionList)
        {
            expressionList = (Xenko.Core.Shaders.Ast.ExpressionList)base.Visit(expressionList);
            return new Xenko.Core.Shaders.Ast.ExpressionList
            {
                Span = expressionList.Span,
                Tags = expressionList.Tags,
                TypeInference = expressionList.TypeInference,
                Expressions = expressionList.Expressions,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.GenericDeclaration genericDeclaration)
        {
            genericDeclaration = (Xenko.Core.Shaders.Ast.GenericDeclaration)base.Visit(genericDeclaration);
            return new Xenko.Core.Shaders.Ast.GenericDeclaration
            {
                Span = genericDeclaration.Span,
                Tags = genericDeclaration.Tags,
                Name = genericDeclaration.Name,
                Holder = genericDeclaration.Holder,
                Index = genericDeclaration.Index,
                IsUsingBase = genericDeclaration.IsUsingBase,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.GenericParameterType genericParameterType)
        {
            genericParameterType = (Xenko.Core.Shaders.Ast.GenericParameterType)base.Visit(genericParameterType);
            return new Xenko.Core.Shaders.Ast.GenericParameterType
            {
                Span = genericParameterType.Span,
                Tags = genericParameterType.Tags,
                Attributes = genericParameterType.Attributes,
                TypeInference = genericParameterType.TypeInference,
                Name = genericParameterType.Name,
                Qualifiers = genericParameterType.Qualifiers,
                IsBuiltIn = genericParameterType.IsBuiltIn,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.DeclarationStatement declarationStatement)
        {
            declarationStatement = (Xenko.Core.Shaders.Ast.DeclarationStatement)base.Visit(declarationStatement);
            return new Xenko.Core.Shaders.Ast.DeclarationStatement
            {
                Span = declarationStatement.Span,
                Tags = declarationStatement.Tags,
                Attributes = declarationStatement.Attributes,
                Content = declarationStatement.Content,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.ExpressionStatement expressionStatement)
        {
            expressionStatement = (Xenko.Core.Shaders.Ast.ExpressionStatement)base.Visit(expressionStatement);
            return new Xenko.Core.Shaders.Ast.ExpressionStatement
            {
                Span = expressionStatement.Span,
                Tags = expressionStatement.Tags,
                Attributes = expressionStatement.Attributes,
                Expression = expressionStatement.Expression,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.ForStatement forStatement)
        {
            forStatement = (Xenko.Core.Shaders.Ast.ForStatement)base.Visit(forStatement);
            return new Xenko.Core.Shaders.Ast.ForStatement
            {
                Span = forStatement.Span,
                Tags = forStatement.Tags,
                Attributes = forStatement.Attributes,
                Start = forStatement.Start,
                Condition = forStatement.Condition,
                Next = forStatement.Next,
                Body = forStatement.Body,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.GenericType genericType)
        {
            genericType = (Xenko.Core.Shaders.Ast.GenericType)base.Visit(genericType);
            return new Xenko.Core.Shaders.Ast.GenericType
            {
                Span = genericType.Span,
                Tags = genericType.Tags,
                Attributes = genericType.Attributes,
                TypeInference = genericType.TypeInference,
                Name = genericType.Name,
                Qualifiers = genericType.Qualifiers,
                IsBuiltIn = genericType.IsBuiltIn,
                ParameterTypes = genericType.ParameterTypes,
                Parameters = genericType.Parameters,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Identifier identifier)
        {
            identifier = (Xenko.Core.Shaders.Ast.Identifier)base.Visit(identifier);
            return new Xenko.Core.Shaders.Ast.Identifier
            {
                Span = identifier.Span,
                Tags = identifier.Tags,
                Indices = identifier.Indices,
                IsSpecialReference = identifier.IsSpecialReference,
                Text = identifier.Text,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.IfStatement ifStatement)
        {
            ifStatement = (Xenko.Core.Shaders.Ast.IfStatement)base.Visit(ifStatement);
            return new Xenko.Core.Shaders.Ast.IfStatement
            {
                Span = ifStatement.Span,
                Tags = ifStatement.Tags,
                Attributes = ifStatement.Attributes,
                Condition = ifStatement.Condition,
                Else = ifStatement.Else,
                Then = ifStatement.Then,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.IndexerExpression indexerExpression)
        {
            indexerExpression = (Xenko.Core.Shaders.Ast.IndexerExpression)base.Visit(indexerExpression);
            return new Xenko.Core.Shaders.Ast.IndexerExpression
            {
                Span = indexerExpression.Span,
                Tags = indexerExpression.Tags,
                TypeInference = indexerExpression.TypeInference,
                Index = indexerExpression.Index,
                Target = indexerExpression.Target,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.KeywordExpression keywordExpression)
        {
            keywordExpression = (Xenko.Core.Shaders.Ast.KeywordExpression)base.Visit(keywordExpression);
            return new Xenko.Core.Shaders.Ast.KeywordExpression
            {
                Span = keywordExpression.Span,
                Tags = keywordExpression.Tags,
                TypeInference = keywordExpression.TypeInference,
                Name = keywordExpression.Name,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Literal literal)
        {
            literal = (Xenko.Core.Shaders.Ast.Literal)base.Visit(literal);
            return new Xenko.Core.Shaders.Ast.Literal
            {
                Span = literal.Span,
                Tags = literal.Tags,
                Value = literal.Value,
                Text = literal.Text,
                SubLiterals = literal.SubLiterals,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.LiteralExpression literalExpression)
        {
            literalExpression = (Xenko.Core.Shaders.Ast.LiteralExpression)base.Visit(literalExpression);
            return new Xenko.Core.Shaders.Ast.LiteralExpression
            {
                Span = literalExpression.Span,
                Tags = literalExpression.Tags,
                TypeInference = literalExpression.TypeInference,
                Literal = literalExpression.Literal,
                Text = literalExpression.Text,
                Value = literalExpression.Value,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.MatrixType matrixType)
        {
            matrixType = (Xenko.Core.Shaders.Ast.MatrixType)base.Visit(matrixType);
            return new Xenko.Core.Shaders.Ast.MatrixType
            {
                Span = matrixType.Span,
                Tags = matrixType.Tags,
                Attributes = matrixType.Attributes,
                TypeInference = matrixType.TypeInference,
                Name = matrixType.Name,
                Qualifiers = matrixType.Qualifiers,
                IsBuiltIn = matrixType.IsBuiltIn,
                RowCount = matrixType.RowCount,
                ColumnCount = matrixType.ColumnCount,
                Type = matrixType.Type,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.MemberReferenceExpression memberReferenceExpression)
        {
            memberReferenceExpression = (Xenko.Core.Shaders.Ast.MemberReferenceExpression)base.Visit(memberReferenceExpression);
            return new Xenko.Core.Shaders.Ast.MemberReferenceExpression
            {
                Span = memberReferenceExpression.Span,
                Tags = memberReferenceExpression.Tags,
                TypeInference = memberReferenceExpression.TypeInference,
                Member = memberReferenceExpression.Member,
                Target = memberReferenceExpression.Target,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.MethodDeclaration methodDeclaration)
        {
            methodDeclaration = (Xenko.Core.Shaders.Ast.MethodDeclaration)base.Visit(methodDeclaration);
            return new Xenko.Core.Shaders.Ast.MethodDeclaration
            {
                Span = methodDeclaration.Span,
                Tags = methodDeclaration.Tags,
                Attributes = methodDeclaration.Attributes,
                Name = methodDeclaration.Name,
                ParameterConstraints = methodDeclaration.ParameterConstraints,
                Parameters = methodDeclaration.Parameters,
                Qualifiers = methodDeclaration.Qualifiers,
                ReturnType = methodDeclaration.ReturnType,
                IsBuiltin = methodDeclaration.IsBuiltin,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.MethodDefinition methodDefinition)
        {
            methodDefinition = (Xenko.Core.Shaders.Ast.MethodDefinition)base.Visit(methodDefinition);
            return new Xenko.Core.Shaders.Ast.MethodDefinition
            {
                Span = methodDefinition.Span,
                Tags = methodDefinition.Tags,
                Attributes = methodDefinition.Attributes,
                Name = methodDefinition.Name,
                ParameterConstraints = methodDefinition.ParameterConstraints,
                Parameters = methodDefinition.Parameters,
                Qualifiers = methodDefinition.Qualifiers,
                ReturnType = methodDefinition.ReturnType,
                IsBuiltin = methodDefinition.IsBuiltin,
                Body = methodDefinition.Body,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.MethodInvocationExpression methodInvocationExpression)
        {
            methodInvocationExpression = (Xenko.Core.Shaders.Ast.MethodInvocationExpression)base.Visit(methodInvocationExpression);
            return new Xenko.Core.Shaders.Ast.MethodInvocationExpression
            {
                Span = methodInvocationExpression.Span,
                Tags = methodInvocationExpression.Tags,
                TypeInference = methodInvocationExpression.TypeInference,
                Target = methodInvocationExpression.Target,
                Arguments = methodInvocationExpression.Arguments,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.ObjectType objectType)
        {
            objectType = (Xenko.Core.Shaders.Ast.ObjectType)base.Visit(objectType);
            return new Xenko.Core.Shaders.Ast.ObjectType
            {
                Span = objectType.Span,
                Tags = objectType.Tags,
                Attributes = objectType.Attributes,
                TypeInference = objectType.TypeInference,
                Name = objectType.Name,
                Qualifiers = objectType.Qualifiers,
                IsBuiltIn = objectType.IsBuiltIn,
                AlternativeNames = objectType.AlternativeNames,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Parameter parameter)
        {
            parameter = (Xenko.Core.Shaders.Ast.Parameter)base.Visit(parameter);
            return new Xenko.Core.Shaders.Ast.Parameter
            {
                Span = parameter.Span,
                Tags = parameter.Tags,
                Attributes = parameter.Attributes,
                Qualifiers = parameter.Qualifiers,
                Type = parameter.Type,
                InitialValue = parameter.InitialValue,
                Name = parameter.Name,
                SubVariables = parameter.SubVariables,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.ParenthesizedExpression parenthesizedExpression)
        {
            parenthesizedExpression = (Xenko.Core.Shaders.Ast.ParenthesizedExpression)base.Visit(parenthesizedExpression);
            return new Xenko.Core.Shaders.Ast.ParenthesizedExpression
            {
                Span = parenthesizedExpression.Span,
                Tags = parenthesizedExpression.Tags,
                TypeInference = parenthesizedExpression.TypeInference,
                Content = parenthesizedExpression.Content,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Qualifier qualifier)
        {
            qualifier = (Xenko.Core.Shaders.Ast.Qualifier)base.Visit(qualifier);
            return new Xenko.Core.Shaders.Ast.Qualifier
            {
                Span = qualifier.Span,
                Tags = qualifier.Tags,
                IsFlag = qualifier.IsFlag,
                Key = qualifier.Key,
                IsPost = qualifier.IsPost,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.ReturnStatement returnStatement)
        {
            returnStatement = (Xenko.Core.Shaders.Ast.ReturnStatement)base.Visit(returnStatement);
            return new Xenko.Core.Shaders.Ast.ReturnStatement
            {
                Span = returnStatement.Span,
                Tags = returnStatement.Tags,
                Attributes = returnStatement.Attributes,
                Value = returnStatement.Value,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.ScalarType scalarType)
        {
            scalarType = (Xenko.Core.Shaders.Ast.ScalarType)base.Visit(scalarType);
            return new Xenko.Core.Shaders.Ast.ScalarType
            {
                Span = scalarType.Span,
                Tags = scalarType.Tags,
                Attributes = scalarType.Attributes,
                TypeInference = scalarType.TypeInference,
                Name = scalarType.Name,
                Qualifiers = scalarType.Qualifiers,
                IsBuiltIn = scalarType.IsBuiltIn,
                Type = scalarType.Type,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Shader shader)
        {
            shader = (Xenko.Core.Shaders.Ast.Shader)base.Visit(shader);
            return new Xenko.Core.Shaders.Ast.Shader
            {
                Span = shader.Span,
                Tags = shader.Tags,
                Declarations = shader.Declarations,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.StatementList statementList)
        {
            statementList = (Xenko.Core.Shaders.Ast.StatementList)base.Visit(statementList);
            return new Xenko.Core.Shaders.Ast.StatementList
            {
                Span = statementList.Span,
                Tags = statementList.Tags,
                Attributes = statementList.Attributes,
                Statements = statementList.Statements,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.StructType structType)
        {
            structType = (Xenko.Core.Shaders.Ast.StructType)base.Visit(structType);
            return new Xenko.Core.Shaders.Ast.StructType
            {
                Span = structType.Span,
                Tags = structType.Tags,
                Attributes = structType.Attributes,
                TypeInference = structType.TypeInference,
                Name = structType.Name,
                Qualifiers = structType.Qualifiers,
                IsBuiltIn = structType.IsBuiltIn,
                Fields = structType.Fields,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.SwitchCaseGroup switchCaseGroup)
        {
            switchCaseGroup = (Xenko.Core.Shaders.Ast.SwitchCaseGroup)base.Visit(switchCaseGroup);
            return new Xenko.Core.Shaders.Ast.SwitchCaseGroup
            {
                Span = switchCaseGroup.Span,
                Tags = switchCaseGroup.Tags,
                Cases = switchCaseGroup.Cases,
                Statements = switchCaseGroup.Statements,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.SwitchStatement switchStatement)
        {
            switchStatement = (Xenko.Core.Shaders.Ast.SwitchStatement)base.Visit(switchStatement);
            return new Xenko.Core.Shaders.Ast.SwitchStatement
            {
                Span = switchStatement.Span,
                Tags = switchStatement.Tags,
                Attributes = switchStatement.Attributes,
                Condition = switchStatement.Condition,
                Groups = switchStatement.Groups,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.TypeName typeName)
        {
            typeName = (Xenko.Core.Shaders.Ast.TypeName)base.Visit(typeName);
            return new Xenko.Core.Shaders.Ast.TypeName
            {
                Span = typeName.Span,
                Tags = typeName.Tags,
                Attributes = typeName.Attributes,
                TypeInference = typeName.TypeInference,
                Name = typeName.Name,
                Qualifiers = typeName.Qualifiers,
                IsBuiltIn = typeName.IsBuiltIn,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.TypeReferenceExpression typeReferenceExpression)
        {
            typeReferenceExpression = (Xenko.Core.Shaders.Ast.TypeReferenceExpression)base.Visit(typeReferenceExpression);
            return new Xenko.Core.Shaders.Ast.TypeReferenceExpression
            {
                Span = typeReferenceExpression.Span,
                Tags = typeReferenceExpression.Tags,
                TypeInference = typeReferenceExpression.TypeInference,
                Type = typeReferenceExpression.Type,
                Declaration = typeReferenceExpression.Declaration,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.UnaryExpression unaryExpression)
        {
            unaryExpression = (Xenko.Core.Shaders.Ast.UnaryExpression)base.Visit(unaryExpression);
            return new Xenko.Core.Shaders.Ast.UnaryExpression
            {
                Span = unaryExpression.Span,
                Tags = unaryExpression.Tags,
                TypeInference = unaryExpression.TypeInference,
                Operator = unaryExpression.Operator,
                Expression = unaryExpression.Expression,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.Variable variable)
        {
            variable = (Xenko.Core.Shaders.Ast.Variable)base.Visit(variable);
            return new Xenko.Core.Shaders.Ast.Variable
            {
                Span = variable.Span,
                Tags = variable.Tags,
                Attributes = variable.Attributes,
                Qualifiers = variable.Qualifiers,
                Type = variable.Type,
                InitialValue = variable.InitialValue,
                Name = variable.Name,
                SubVariables = variable.SubVariables,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.VariableReferenceExpression variableReferenceExpression)
        {
            variableReferenceExpression = (Xenko.Core.Shaders.Ast.VariableReferenceExpression)base.Visit(variableReferenceExpression);
            return new Xenko.Core.Shaders.Ast.VariableReferenceExpression
            {
                Span = variableReferenceExpression.Span,
                Tags = variableReferenceExpression.Tags,
                TypeInference = variableReferenceExpression.TypeInference,
                Name = variableReferenceExpression.Name,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.VectorType vectorType)
        {
            vectorType = (Xenko.Core.Shaders.Ast.VectorType)base.Visit(vectorType);
            return new Xenko.Core.Shaders.Ast.VectorType
            {
                Span = vectorType.Span,
                Tags = vectorType.Tags,
                Attributes = vectorType.Attributes,
                TypeInference = vectorType.TypeInference,
                Name = vectorType.Name,
                Qualifiers = vectorType.Qualifiers,
                IsBuiltIn = vectorType.IsBuiltIn,
                Dimension = vectorType.Dimension,
                Type = vectorType.Type,
            };
        }
        public override Node Visit(Xenko.Core.Shaders.Ast.WhileStatement whileStatement)
        {
            whileStatement = (Xenko.Core.Shaders.Ast.WhileStatement)base.Visit(whileStatement);
            return new Xenko.Core.Shaders.Ast.WhileStatement
            {
                Span = whileStatement.Span,
                Tags = whileStatement.Tags,
                Attributes = whileStatement.Attributes,
                Condition = whileStatement.Condition,
                IsDoWhile = whileStatement.IsDoWhile,
                Statement = whileStatement.Statement,
            };
        }
    }

    public partial class ShaderVisitor
    {
        public virtual void Visit(Xenko.Core.Shaders.Ast.Xenko.ClassIdentifierGeneric classIdentifierGeneric)
        {
            DefaultVisit(classIdentifierGeneric);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Xenko.EnumType enumType)
        {
            DefaultVisit(enumType);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Xenko.ForEachStatement forEachStatement)
        {
            DefaultVisit(forEachStatement);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Xenko.ImportBlockStatement importBlockStatement)
        {
            DefaultVisit(importBlockStatement);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Xenko.LinkType linkType)
        {
            DefaultVisit(linkType);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Xenko.LiteralIdentifier literalIdentifier)
        {
            DefaultVisit(literalIdentifier);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Xenko.MemberName memberName)
        {
            DefaultVisit(memberName);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Xenko.MixinStatement mixinStatement)
        {
            DefaultVisit(mixinStatement);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Xenko.NamespaceBlock namespaceBlock)
        {
            DefaultVisit(namespaceBlock);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Xenko.ParametersBlock parametersBlock)
        {
            DefaultVisit(parametersBlock);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Xenko.SemanticType semanticType)
        {
            DefaultVisit(semanticType);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Xenko.EffectBlock effectBlock)
        {
            DefaultVisit(effectBlock);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Xenko.ShaderClassType shaderClassType)
        {
            DefaultVisit(shaderClassType);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Xenko.ShaderRootClassType shaderRootClassType)
        {
            DefaultVisit(shaderRootClassType);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Xenko.ShaderTypeName shaderTypeName)
        {
            DefaultVisit(shaderTypeName);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Xenko.TypeIdentifier typeIdentifier)
        {
            DefaultVisit(typeIdentifier);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Xenko.UsingParametersStatement usingParametersStatement)
        {
            DefaultVisit(usingParametersStatement);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Xenko.UsingStatement usingStatement)
        {
            DefaultVisit(usingStatement);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Xenko.VarType varType)
        {
            DefaultVisit(varType);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Xenko.XenkoConstantBufferType xenkoConstantBufferType)
        {
            DefaultVisit(xenkoConstantBufferType);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.ArrayInitializerExpression arrayInitializerExpression)
        {
            DefaultVisit(arrayInitializerExpression);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.ArrayType arrayType)
        {
            DefaultVisit(arrayType);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.AssignmentExpression assignmentExpression)
        {
            DefaultVisit(assignmentExpression);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.BinaryExpression binaryExpression)
        {
            DefaultVisit(binaryExpression);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.BlockStatement blockStatement)
        {
            DefaultVisit(blockStatement);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.CaseStatement caseStatement)
        {
            DefaultVisit(caseStatement);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.CompositeEnum compositeEnum)
        {
            DefaultVisit(compositeEnum);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.ConditionalExpression conditionalExpression)
        {
            DefaultVisit(conditionalExpression);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.EmptyStatement emptyStatement)
        {
            DefaultVisit(emptyStatement);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.EmptyExpression emptyExpression)
        {
            DefaultVisit(emptyExpression);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Glsl.LayoutKeyValue layoutKeyValue)
        {
            DefaultVisit(layoutKeyValue);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Glsl.LayoutQualifier layoutQualifier)
        {
            DefaultVisit(layoutQualifier);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Glsl.InterfaceType interfaceType)
        {
            DefaultVisit(interfaceType);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Hlsl.ClassType classType)
        {
            DefaultVisit(classType);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Hlsl.IdentifierGeneric identifierGeneric)
        {
            DefaultVisit(identifierGeneric);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Hlsl.IdentifierNs identifierNs)
        {
            DefaultVisit(identifierNs);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Hlsl.IdentifierDot identifierDot)
        {
            DefaultVisit(identifierDot);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Hlsl.TextureType textureType)
        {
            DefaultVisit(textureType);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Hlsl.Annotations annotations)
        {
            DefaultVisit(annotations);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Hlsl.AsmExpression asmExpression)
        {
            DefaultVisit(asmExpression);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Hlsl.AttributeDeclaration attributeDeclaration)
        {
            DefaultVisit(attributeDeclaration);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Hlsl.CastExpression castExpression)
        {
            DefaultVisit(castExpression);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Hlsl.CompileExpression compileExpression)
        {
            DefaultVisit(compileExpression);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Hlsl.ConstantBuffer constantBuffer)
        {
            DefaultVisit(constantBuffer);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Hlsl.ConstantBufferType constantBufferType)
        {
            DefaultVisit(constantBufferType);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Hlsl.InterfaceType interfaceType)
        {
            DefaultVisit(interfaceType);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Hlsl.PackOffset packOffset)
        {
            DefaultVisit(packOffset);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Hlsl.Pass pass)
        {
            DefaultVisit(pass);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Hlsl.RegisterLocation registerLocation)
        {
            DefaultVisit(registerLocation);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Hlsl.Semantic semantic)
        {
            DefaultVisit(semantic);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Hlsl.StateExpression stateExpression)
        {
            DefaultVisit(stateExpression);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Hlsl.StateInitializer stateInitializer)
        {
            DefaultVisit(stateInitializer);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Hlsl.Technique technique)
        {
            DefaultVisit(technique);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Hlsl.Typedef typedef)
        {
            DefaultVisit(typedef);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.ExpressionList expressionList)
        {
            DefaultVisit(expressionList);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.GenericDeclaration genericDeclaration)
        {
            DefaultVisit(genericDeclaration);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.GenericParameterType genericParameterType)
        {
            DefaultVisit(genericParameterType);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.DeclarationStatement declarationStatement)
        {
            DefaultVisit(declarationStatement);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.ExpressionStatement expressionStatement)
        {
            DefaultVisit(expressionStatement);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.ForStatement forStatement)
        {
            DefaultVisit(forStatement);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.GenericType genericType)
        {
            DefaultVisit(genericType);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Identifier identifier)
        {
            DefaultVisit(identifier);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.IfStatement ifStatement)
        {
            DefaultVisit(ifStatement);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.IndexerExpression indexerExpression)
        {
            DefaultVisit(indexerExpression);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.KeywordExpression keywordExpression)
        {
            DefaultVisit(keywordExpression);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Literal literal)
        {
            DefaultVisit(literal);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.LiteralExpression literalExpression)
        {
            DefaultVisit(literalExpression);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.MatrixType matrixType)
        {
            DefaultVisit(matrixType);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.MemberReferenceExpression memberReferenceExpression)
        {
            DefaultVisit(memberReferenceExpression);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.MethodDeclaration methodDeclaration)
        {
            DefaultVisit(methodDeclaration);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.MethodDefinition methodDefinition)
        {
            DefaultVisit(methodDefinition);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.MethodInvocationExpression methodInvocationExpression)
        {
            DefaultVisit(methodInvocationExpression);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.ObjectType objectType)
        {
            DefaultVisit(objectType);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Parameter parameter)
        {
            DefaultVisit(parameter);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.ParenthesizedExpression parenthesizedExpression)
        {
            DefaultVisit(parenthesizedExpression);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Qualifier qualifier)
        {
            DefaultVisit(qualifier);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.ReturnStatement returnStatement)
        {
            DefaultVisit(returnStatement);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.ScalarType scalarType)
        {
            DefaultVisit(scalarType);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Shader shader)
        {
            DefaultVisit(shader);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.StatementList statementList)
        {
            DefaultVisit(statementList);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.StructType structType)
        {
            DefaultVisit(structType);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.SwitchCaseGroup switchCaseGroup)
        {
            DefaultVisit(switchCaseGroup);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.SwitchStatement switchStatement)
        {
            DefaultVisit(switchStatement);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.TypeName typeName)
        {
            DefaultVisit(typeName);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.TypeReferenceExpression typeReferenceExpression)
        {
            DefaultVisit(typeReferenceExpression);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.UnaryExpression unaryExpression)
        {
            DefaultVisit(unaryExpression);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.Variable variable)
        {
            DefaultVisit(variable);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.VariableReferenceExpression variableReferenceExpression)
        {
            DefaultVisit(variableReferenceExpression);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.VectorType vectorType)
        {
            DefaultVisit(vectorType);
        }
        public virtual void Visit(Xenko.Core.Shaders.Ast.WhileStatement whileStatement)
        {
            DefaultVisit(whileStatement);
        }
    }

    public partial class ShaderWalker
    {
        public override void Visit(Xenko.Core.Shaders.Ast.Xenko.ClassIdentifierGeneric classIdentifierGeneric)
        {
            VisitList(classIdentifierGeneric.Indices);
            VisitList(classIdentifierGeneric.Generics);
            base.Visit(classIdentifierGeneric);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Xenko.EnumType enumType)
        {
            VisitList(enumType.Attributes);
            VisitDynamic(enumType.Name);
            VisitDynamic(enumType.Qualifiers);
            VisitList(enumType.Values);
            base.Visit(enumType);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Xenko.ForEachStatement forEachStatement)
        {
            VisitList(forEachStatement.Attributes);
            VisitDynamic(forEachStatement.Collection);
            VisitDynamic(forEachStatement.Variable);
            VisitDynamic(forEachStatement.Body);
            base.Visit(forEachStatement);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Xenko.ImportBlockStatement importBlockStatement)
        {
            VisitList(importBlockStatement.Attributes);
            VisitDynamic(importBlockStatement.Statements);
            base.Visit(importBlockStatement);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Xenko.LinkType linkType)
        {
            VisitList(linkType.Attributes);
            VisitDynamic(linkType.Name);
            VisitDynamic(linkType.Qualifiers);
            base.Visit(linkType);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Xenko.LiteralIdentifier literalIdentifier)
        {
            VisitList(literalIdentifier.Indices);
            VisitDynamic(literalIdentifier.Value);
            base.Visit(literalIdentifier);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Xenko.MemberName memberName)
        {
            VisitList(memberName.Attributes);
            VisitDynamic(memberName.Name);
            VisitDynamic(memberName.Qualifiers);
            base.Visit(memberName);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Xenko.MixinStatement mixinStatement)
        {
            VisitList(mixinStatement.Attributes);
            VisitDynamic(mixinStatement.Value);
            base.Visit(mixinStatement);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Xenko.NamespaceBlock namespaceBlock)
        {
            VisitList(namespaceBlock.Attributes);
            VisitDynamic(namespaceBlock.Name);
            VisitDynamic(namespaceBlock.Qualifiers);
            VisitList(namespaceBlock.Body);
            base.Visit(namespaceBlock);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Xenko.ParametersBlock parametersBlock)
        {
            VisitDynamic(parametersBlock.Name);
            VisitDynamic(parametersBlock.Body);
            base.Visit(parametersBlock);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Xenko.SemanticType semanticType)
        {
            VisitList(semanticType.Attributes);
            VisitDynamic(semanticType.Name);
            VisitDynamic(semanticType.Qualifiers);
            base.Visit(semanticType);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Xenko.EffectBlock effectBlock)
        {
            VisitList(effectBlock.Attributes);
            VisitDynamic(effectBlock.Name);
            VisitDynamic(effectBlock.Qualifiers);
            VisitDynamic(effectBlock.Body);
            base.Visit(effectBlock);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Xenko.ShaderClassType shaderClassType)
        {
            VisitList(shaderClassType.Attributes);
            VisitDynamic(shaderClassType.Name);
            VisitDynamic(shaderClassType.Qualifiers);
            VisitList(shaderClassType.BaseClasses);
            VisitList(shaderClassType.GenericParameters);
            VisitList(shaderClassType.GenericArguments);
            VisitList(shaderClassType.Members);
            VisitList(shaderClassType.ShaderGenerics);
            base.Visit(shaderClassType);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Xenko.ShaderRootClassType shaderRootClassType)
        {
            VisitList(shaderRootClassType.Attributes);
            VisitDynamic(shaderRootClassType.Name);
            VisitDynamic(shaderRootClassType.Qualifiers);
            VisitList(shaderRootClassType.BaseClasses);
            VisitList(shaderRootClassType.GenericParameters);
            VisitList(shaderRootClassType.GenericArguments);
            VisitList(shaderRootClassType.Members);
            VisitList(shaderRootClassType.ShaderGenerics);
            base.Visit(shaderRootClassType);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Xenko.ShaderTypeName shaderTypeName)
        {
            VisitList(shaderTypeName.Attributes);
            VisitDynamic(shaderTypeName.Name);
            VisitDynamic(shaderTypeName.Qualifiers);
            base.Visit(shaderTypeName);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Xenko.TypeIdentifier typeIdentifier)
        {
            VisitList(typeIdentifier.Indices);
            VisitDynamic(typeIdentifier.Type);
            base.Visit(typeIdentifier);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Xenko.UsingParametersStatement usingParametersStatement)
        {
            VisitList(usingParametersStatement.Attributes);
            VisitDynamic(usingParametersStatement.Name);
            VisitDynamic(usingParametersStatement.Body);
            base.Visit(usingParametersStatement);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Xenko.UsingStatement usingStatement)
        {
            VisitList(usingStatement.Attributes);
            VisitDynamic(usingStatement.Name);
            base.Visit(usingStatement);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Xenko.VarType varType)
        {
            VisitList(varType.Attributes);
            VisitDynamic(varType.Name);
            VisitDynamic(varType.Qualifiers);
            base.Visit(varType);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Xenko.XenkoConstantBufferType xenkoConstantBufferType)
        {
            base.Visit(xenkoConstantBufferType);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.ArrayInitializerExpression arrayInitializerExpression)
        {
            VisitList(arrayInitializerExpression.Items);
            base.Visit(arrayInitializerExpression);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.ArrayType arrayType)
        {
            VisitList(arrayType.Attributes);
            VisitDynamic(arrayType.Name);
            VisitDynamic(arrayType.Qualifiers);
            VisitList(arrayType.Dimensions);
            VisitDynamic(arrayType.Type);
            base.Visit(arrayType);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.AssignmentExpression assignmentExpression)
        {
            VisitDynamic(assignmentExpression.Target);
            VisitDynamic(assignmentExpression.Value);
            base.Visit(assignmentExpression);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.BinaryExpression binaryExpression)
        {
            VisitDynamic(binaryExpression.Left);
            VisitDynamic(binaryExpression.Right);
            base.Visit(binaryExpression);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.BlockStatement blockStatement)
        {
            VisitList(blockStatement.Attributes);
            VisitDynamic(blockStatement.Statements);
            base.Visit(blockStatement);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.CaseStatement caseStatement)
        {
            VisitList(caseStatement.Attributes);
            VisitDynamic(caseStatement.Case);
            base.Visit(caseStatement);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.CompositeEnum compositeEnum)
        {
            base.Visit(compositeEnum);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.ConditionalExpression conditionalExpression)
        {
            VisitDynamic(conditionalExpression.Condition);
            VisitDynamic(conditionalExpression.Left);
            VisitDynamic(conditionalExpression.Right);
            base.Visit(conditionalExpression);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.EmptyStatement emptyStatement)
        {
            VisitList(emptyStatement.Attributes);
            base.Visit(emptyStatement);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.EmptyExpression emptyExpression)
        {
            base.Visit(emptyExpression);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Glsl.LayoutKeyValue layoutKeyValue)
        {
            VisitDynamic(layoutKeyValue.Name);
            VisitDynamic(layoutKeyValue.Value);
            base.Visit(layoutKeyValue);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Glsl.LayoutQualifier layoutQualifier)
        {
            VisitList(layoutQualifier.Layouts);
            base.Visit(layoutQualifier);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Glsl.InterfaceType interfaceType)
        {
            VisitList(interfaceType.Attributes);
            VisitDynamic(interfaceType.Name);
            VisitDynamic(interfaceType.Qualifiers);
            VisitList(interfaceType.Fields);
            base.Visit(interfaceType);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Hlsl.ClassType classType)
        {
            VisitList(classType.Attributes);
            VisitDynamic(classType.Name);
            VisitDynamic(classType.Qualifiers);
            VisitList(classType.BaseClasses);
            VisitList(classType.GenericParameters);
            VisitList(classType.GenericArguments);
            VisitList(classType.Members);
            base.Visit(classType);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Hlsl.IdentifierGeneric identifierGeneric)
        {
            VisitList(identifierGeneric.Indices);
            VisitList(identifierGeneric.Identifiers);
            base.Visit(identifierGeneric);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Hlsl.IdentifierNs identifierNs)
        {
            VisitList(identifierNs.Indices);
            VisitList(identifierNs.Identifiers);
            base.Visit(identifierNs);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Hlsl.IdentifierDot identifierDot)
        {
            VisitList(identifierDot.Indices);
            VisitList(identifierDot.Identifiers);
            base.Visit(identifierDot);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Hlsl.TextureType textureType)
        {
            VisitList(textureType.Attributes);
            VisitDynamic(textureType.Name);
            VisitDynamic(textureType.Qualifiers);
            base.Visit(textureType);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Hlsl.Annotations annotations)
        {
            VisitList(annotations.Variables);
            base.Visit(annotations);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Hlsl.AsmExpression asmExpression)
        {
            base.Visit(asmExpression);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Hlsl.AttributeDeclaration attributeDeclaration)
        {
            VisitDynamic(attributeDeclaration.Name);
            VisitList(attributeDeclaration.Parameters);
            base.Visit(attributeDeclaration);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Hlsl.CastExpression castExpression)
        {
            VisitDynamic(castExpression.From);
            VisitDynamic(castExpression.Target);
            base.Visit(castExpression);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Hlsl.CompileExpression compileExpression)
        {
            VisitDynamic(compileExpression.Function);
            VisitDynamic(compileExpression.Profile);
            base.Visit(compileExpression);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Hlsl.ConstantBuffer constantBuffer)
        {
            VisitList(constantBuffer.Attributes);
            VisitDynamic(constantBuffer.Type);
            VisitList(constantBuffer.Members);
            VisitDynamic(constantBuffer.Name);
            VisitDynamic(constantBuffer.Register);
            VisitDynamic(constantBuffer.Qualifiers);
            base.Visit(constantBuffer);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Hlsl.ConstantBufferType constantBufferType)
        {
            base.Visit(constantBufferType);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Hlsl.InterfaceType interfaceType)
        {
            VisitList(interfaceType.Attributes);
            VisitDynamic(interfaceType.Name);
            VisitDynamic(interfaceType.Qualifiers);
            VisitList(interfaceType.GenericParameters);
            VisitList(interfaceType.GenericArguments);
            VisitList(interfaceType.Methods);
            base.Visit(interfaceType);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Hlsl.PackOffset packOffset)
        {
            VisitDynamic(packOffset.Value);
            base.Visit(packOffset);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Hlsl.Pass pass)
        {
            VisitList(pass.Attributes);
            VisitList(pass.Items);
            VisitDynamic(pass.Name);
            base.Visit(pass);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Hlsl.RegisterLocation registerLocation)
        {
            VisitDynamic(registerLocation.Profile);
            VisitDynamic(registerLocation.Register);
            base.Visit(registerLocation);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Hlsl.Semantic semantic)
        {
            VisitDynamic(semantic.Name);
            base.Visit(semantic);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Hlsl.StateExpression stateExpression)
        {
            VisitDynamic(stateExpression.Initializer);
            VisitDynamic(stateExpression.StateType);
            base.Visit(stateExpression);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Hlsl.StateInitializer stateInitializer)
        {
            VisitList(stateInitializer.Items);
            base.Visit(stateInitializer);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Hlsl.Technique technique)
        {
            VisitDynamic(technique.Type);
            VisitList(technique.Attributes);
            VisitDynamic(technique.Name);
            VisitList(technique.Passes);
            base.Visit(technique);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Hlsl.Typedef typedef)
        {
            VisitList(typedef.Attributes);
            VisitDynamic(typedef.Name);
            VisitDynamic(typedef.Qualifiers);
            VisitList(typedef.SubDeclarators);
            VisitDynamic(typedef.Type);
            base.Visit(typedef);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.ExpressionList expressionList)
        {
            VisitList(expressionList.Expressions);
            base.Visit(expressionList);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.GenericDeclaration genericDeclaration)
        {
            VisitDynamic(genericDeclaration.Name);
            base.Visit(genericDeclaration);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.GenericParameterType genericParameterType)
        {
            VisitList(genericParameterType.Attributes);
            VisitDynamic(genericParameterType.Name);
            VisitDynamic(genericParameterType.Qualifiers);
            base.Visit(genericParameterType);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.DeclarationStatement declarationStatement)
        {
            VisitList(declarationStatement.Attributes);
            VisitDynamic(declarationStatement.Content);
            base.Visit(declarationStatement);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.ExpressionStatement expressionStatement)
        {
            VisitList(expressionStatement.Attributes);
            VisitDynamic(expressionStatement.Expression);
            base.Visit(expressionStatement);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.ForStatement forStatement)
        {
            VisitList(forStatement.Attributes);
            VisitDynamic(forStatement.Start);
            VisitDynamic(forStatement.Condition);
            VisitDynamic(forStatement.Next);
            VisitDynamic(forStatement.Body);
            base.Visit(forStatement);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.GenericType genericType)
        {
            VisitList(genericType.Attributes);
            VisitDynamic(genericType.Name);
            VisitDynamic(genericType.Qualifiers);
            VisitList(genericType.Parameters);
            base.Visit(genericType);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Identifier identifier)
        {
            VisitList(identifier.Indices);
            base.Visit(identifier);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.IfStatement ifStatement)
        {
            VisitList(ifStatement.Attributes);
            VisitDynamic(ifStatement.Condition);
            VisitDynamic(ifStatement.Else);
            VisitDynamic(ifStatement.Then);
            base.Visit(ifStatement);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.IndexerExpression indexerExpression)
        {
            VisitDynamic(indexerExpression.Index);
            VisitDynamic(indexerExpression.Target);
            base.Visit(indexerExpression);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.KeywordExpression keywordExpression)
        {
            VisitDynamic(keywordExpression.Name);
            base.Visit(keywordExpression);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Literal literal)
        {
            VisitList(literal.SubLiterals);
            base.Visit(literal);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.LiteralExpression literalExpression)
        {
            VisitDynamic(literalExpression.Literal);
            base.Visit(literalExpression);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.MatrixType matrixType)
        {
            VisitList(matrixType.Attributes);
            VisitDynamic(matrixType.Name);
            VisitDynamic(matrixType.Qualifiers);
            VisitDynamic(matrixType.Type);
            base.Visit(matrixType);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.MemberReferenceExpression memberReferenceExpression)
        {
            VisitDynamic(memberReferenceExpression.Member);
            VisitDynamic(memberReferenceExpression.Target);
            base.Visit(memberReferenceExpression);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.MethodDeclaration methodDeclaration)
        {
            VisitList(methodDeclaration.Attributes);
            VisitDynamic(methodDeclaration.Name);
            VisitList(methodDeclaration.Parameters);
            VisitDynamic(methodDeclaration.Qualifiers);
            VisitDynamic(methodDeclaration.ReturnType);
            base.Visit(methodDeclaration);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.MethodDefinition methodDefinition)
        {
            VisitList(methodDefinition.Attributes);
            VisitDynamic(methodDefinition.Name);
            VisitList(methodDefinition.Parameters);
            VisitDynamic(methodDefinition.Qualifiers);
            VisitDynamic(methodDefinition.ReturnType);
            VisitDynamic(methodDefinition.Body);
            base.Visit(methodDefinition);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.MethodInvocationExpression methodInvocationExpression)
        {
            VisitDynamic(methodInvocationExpression.Target);
            VisitList(methodInvocationExpression.Arguments);
            base.Visit(methodInvocationExpression);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.ObjectType objectType)
        {
            VisitList(objectType.Attributes);
            VisitDynamic(objectType.Name);
            VisitDynamic(objectType.Qualifiers);
            base.Visit(objectType);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Parameter parameter)
        {
            VisitList(parameter.Attributes);
            VisitDynamic(parameter.Qualifiers);
            VisitDynamic(parameter.Type);
            VisitDynamic(parameter.InitialValue);
            VisitDynamic(parameter.Name);
            VisitList(parameter.SubVariables);
            base.Visit(parameter);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.ParenthesizedExpression parenthesizedExpression)
        {
            VisitDynamic(parenthesizedExpression.Content);
            base.Visit(parenthesizedExpression);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Qualifier qualifier)
        {
            base.Visit(qualifier);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.ReturnStatement returnStatement)
        {
            VisitList(returnStatement.Attributes);
            VisitDynamic(returnStatement.Value);
            base.Visit(returnStatement);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.ScalarType scalarType)
        {
            VisitList(scalarType.Attributes);
            VisitDynamic(scalarType.Name);
            VisitDynamic(scalarType.Qualifiers);
            base.Visit(scalarType);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Shader shader)
        {
            VisitList(shader.Declarations);
            base.Visit(shader);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.StatementList statementList)
        {
            VisitList(statementList.Attributes);
            VisitList(statementList.Statements);
            base.Visit(statementList);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.StructType structType)
        {
            VisitList(structType.Attributes);
            VisitDynamic(structType.Name);
            VisitDynamic(structType.Qualifiers);
            VisitList(structType.Fields);
            base.Visit(structType);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.SwitchCaseGroup switchCaseGroup)
        {
            VisitList(switchCaseGroup.Cases);
            VisitDynamic(switchCaseGroup.Statements);
            base.Visit(switchCaseGroup);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.SwitchStatement switchStatement)
        {
            VisitList(switchStatement.Attributes);
            VisitDynamic(switchStatement.Condition);
            VisitList(switchStatement.Groups);
            base.Visit(switchStatement);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.TypeName typeName)
        {
            VisitList(typeName.Attributes);
            VisitDynamic(typeName.Name);
            VisitDynamic(typeName.Qualifiers);
            base.Visit(typeName);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.TypeReferenceExpression typeReferenceExpression)
        {
            VisitDynamic(typeReferenceExpression.Type);
            base.Visit(typeReferenceExpression);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.UnaryExpression unaryExpression)
        {
            VisitDynamic(unaryExpression.Expression);
            base.Visit(unaryExpression);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.Variable variable)
        {
            VisitList(variable.Attributes);
            VisitDynamic(variable.Qualifiers);
            VisitDynamic(variable.Type);
            VisitDynamic(variable.InitialValue);
            VisitDynamic(variable.Name);
            VisitList(variable.SubVariables);
            base.Visit(variable);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.VariableReferenceExpression variableReferenceExpression)
        {
            VisitDynamic(variableReferenceExpression.Name);
            base.Visit(variableReferenceExpression);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.VectorType vectorType)
        {
            VisitList(vectorType.Attributes);
            VisitDynamic(vectorType.Name);
            VisitDynamic(vectorType.Qualifiers);
            VisitDynamic(vectorType.Type);
            base.Visit(vectorType);
        }
        public override void Visit(Xenko.Core.Shaders.Ast.WhileStatement whileStatement)
        {
            VisitList(whileStatement.Attributes);
            VisitDynamic(whileStatement.Condition);
            VisitDynamic(whileStatement.Statement);
            base.Visit(whileStatement);
        }
    }
}

namespace Xenko.Core.Shaders.Ast.Xenko
{
    public partial class ClassIdentifierGeneric
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Xenko
{
    public partial class EnumType
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Xenko
{
    public partial class ForEachStatement
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Xenko
{
    public partial class ImportBlockStatement
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Xenko
{
    public partial class LinkType
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Xenko
{
    public partial class LiteralIdentifier
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Xenko
{
    public partial class MemberName
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Xenko
{
    public partial class MixinStatement
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Xenko
{
    public partial class NamespaceBlock
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Xenko
{
    public partial class ParametersBlock
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Xenko
{
    public partial class SemanticType
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Xenko
{
    public partial class EffectBlock
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Xenko
{
    public partial class ShaderClassType
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Xenko
{
    public partial class ShaderRootClassType
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Xenko
{
    public partial class ShaderTypeName
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Xenko
{
    public partial class TypeIdentifier
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Xenko
{
    public partial class UsingParametersStatement
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Xenko
{
    public partial class UsingStatement
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Xenko
{
    public partial class VarType
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Xenko
{
    public partial class XenkoConstantBufferType
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class ArrayInitializerExpression
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class ArrayType
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class AssignmentExpression
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class BinaryExpression
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class BlockStatement
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class CaseStatement
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class CompositeEnum
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class ConditionalExpression
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class EmptyStatement
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class EmptyExpression
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Glsl
{
    public partial class LayoutKeyValue
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Glsl
{
    public partial class LayoutQualifier
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Glsl
{
    public partial class InterfaceType
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Hlsl
{
    public partial class ClassType
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Hlsl
{
    public partial class IdentifierGeneric
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Hlsl
{
    public partial class IdentifierNs
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Hlsl
{
    public partial class IdentifierDot
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Hlsl
{
    public partial class TextureType
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Hlsl
{
    public partial class Annotations
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Hlsl
{
    public partial class AsmExpression
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Hlsl
{
    public partial class AttributeDeclaration
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Hlsl
{
    public partial class CastExpression
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Hlsl
{
    public partial class CompileExpression
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Hlsl
{
    public partial class ConstantBuffer
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Hlsl
{
    public partial class ConstantBufferType
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Hlsl
{
    public partial class InterfaceType
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Hlsl
{
    public partial class PackOffset
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Hlsl
{
    public partial class Pass
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Hlsl
{
    public partial class RegisterLocation
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Hlsl
{
    public partial class Semantic
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Hlsl
{
    public partial class StateExpression
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Hlsl
{
    public partial class StateInitializer
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Hlsl
{
    public partial class Technique
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast.Hlsl
{
    public partial class Typedef
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class ExpressionList
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class GenericDeclaration
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class GenericParameterType
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class DeclarationStatement
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class ExpressionStatement
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class ForStatement
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class GenericType
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class Identifier
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class IfStatement
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class IndexerExpression
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class KeywordExpression
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class Literal
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class LiteralExpression
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class MatrixType
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class MemberReferenceExpression
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class MethodDeclaration
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class MethodDefinition
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class MethodInvocationExpression
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class ObjectType
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class Parameter
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class ParenthesizedExpression
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class Qualifier
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class ReturnStatement
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class ScalarType
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class Shader
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class StatementList
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class StructType
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class SwitchCaseGroup
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class SwitchStatement
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class TypeName
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class TypeReferenceExpression
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class UnaryExpression
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class Variable
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class VariableReferenceExpression
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class VectorType
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
namespace Xenko.Core.Shaders.Ast
{
    public partial class WhileStatement
    {
        public override void Accept(ShaderVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override TResult Accept<TResult>(ShaderVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}

