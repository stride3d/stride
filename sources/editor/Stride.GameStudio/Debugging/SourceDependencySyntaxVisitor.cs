// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Stride.GameStudio.Debugging
{
    /// <summary>
    /// A roslyn visitor that can detect references to other <see cref="SyntaxTree"/> and as a result, between source files.
    /// </summary>
    class SourceDependencySyntaxVisitor : CSharpSyntaxVisitor<HashSet<SyntaxTree>>
    {
        private readonly SemanticModel semanticModel;

        private readonly HashSet<SyntaxTree> syntaxTrees;

        private readonly HashSet<SyntaxTree> dependencies;

        public SourceDependencySyntaxVisitor(HashSet<SyntaxTree> syntaxTrees, SemanticModel semanticModel)
        {
            this.semanticModel = semanticModel;
            this.syntaxTrees = syntaxTrees;

            dependencies = new HashSet<SyntaxTree>();
        }

        public override HashSet<SyntaxTree> DefaultVisit(SyntaxNode node)
        {
            // Make sure that for type declaration, partial declarations are referenced
            if (node is BaseTypeDeclarationSyntax)
                AnalyzeNode(node);

            foreach (var childNode in node.ChildNodes())
            {
                Visit(childNode);
            }

            return dependencies;
        }

        public override HashSet<SyntaxTree> VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            AnalyzeNode(node);
            return base.VisitObjectCreationExpression(node);
        }

        public override HashSet<SyntaxTree> VisitIdentifierName(IdentifierNameSyntax node)
        {
            return AnalyzeNode(node);
        }

        public override HashSet<SyntaxTree> VisitGenericName(GenericNameSyntax node)
        {
            AnalyzeNode(node);
            return DefaultVisit(node);
        }

        public override HashSet<SyntaxTree> VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is MemberAccessExpressionSyntax)
                AnalyzeNode(node);
            return base.VisitInvocationExpression(node);
        }

        private HashSet<SyntaxTree> AnalyzeNode(SyntaxNode node)
        {
            // Check for declared symbols (partial class)
            var symbol = node is BaseTypeDeclarationSyntax ? semanticModel.GetDeclaredSymbol(node) : null;
            if (node is BaseTypeDeclarationSyntax)
                AddSymbolDependency(symbol);

            // If it's a type, add link to files that describe this type
            symbol = semanticModel.GetTypeInfo(node).Type;
            if (symbol != null)
                AddSymbolDependency(symbol);

            // If it's a method, add reference to method
            symbol = semanticModel.GetSymbolInfo(node).Symbol as IMethodSymbol;
            if (symbol != null)
                AddSymbolDependency(symbol);

            return dependencies;
        }

        private void AddSymbolDependency(ISymbol typeSymbol)
        {
            foreach (var syntaxReference in typeSymbol.DeclaringSyntaxReferences)
            {
                if (syntaxTrees.Contains(syntaxReference.SyntaxTree))
                    dependencies.Add(syntaxReference.SyntaxTree);
            }
        }
    }
}
