// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Stride.Assets.Scripts
{
    internal static class RoslynHelper
    {
        public static void AnalyzeBlockFlow(IList<Slot> newSlots, Compilation compilation, BlockSyntax block)
        {
            const string ExpressionToReturn = nameof(ExpressionToReturn);

            // Nothing to analyze
            if (block.Statements.Count == 0)
                return;

            // Create a compilation unit with our expression
            var compilationUnit = CreateCompilationUnitFromBlock(ref block);

            compilation = compilation.AddSyntaxTrees(compilationUnit.SyntaxTree);

            // Detect missing variables
            var unresolvedSymbolsTable = new Dictionary<string, int>();

            foreach (var diagnostic in compilation.GetDiagnostics())
            {
                // Only process diagnostics from our generated syntax tree
                if (diagnostic.Location.SourceTree != compilationUnit.SyntaxTree)
                    continue;

                if (diagnostic.Id == "CS0103")
                {
                    // error CS0103: The name 'foo' does not exist in the current context
                    var node = compilationUnit.FindNode(diagnostic.Location.SourceSpan);

                    var identifierName = node as IdentifierNameSyntax ?? node.ChildNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();
                    if (identifierName != null)
                    {
                        var identifierText = identifierName.Identifier.Text;

                        // Update location with earliest found
                        int location;
                        if (!unresolvedSymbolsTable.TryGetValue(identifierText, out location))
                            location = Int32.MaxValue;

                        if (diagnostic.Location.SourceSpan.Start < location)
                            unresolvedSymbolsTable[identifierText] = diagnostic.Location.SourceSpan.Start;
                    }
                }
            }

            // Order symbols by appearance order in source code
            var unresolvedSymbols = unresolvedSymbolsTable.OrderBy(x => x.Value).Select(x => x.Key).ToList();

            if (unresolvedSymbols.Count > 0)
            {
                // Includes comma
                var syntaxNodesOrToken = new SyntaxNodeOrToken[unresolvedSymbols.Count*2 - 1];
                for (int i = 0; i < unresolvedSymbols.Count; ++i)
                {
                    if (i > 0)
                        syntaxNodesOrToken[i * 2 - 1] = SyntaxFactory.Token(SyntaxKind.CommaToken);
                    syntaxNodesOrToken[i * 2] = SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(unresolvedSymbols[i]));
                }

                // Perform a second analysis with those missing symbols declared with var
                var newCompilationUnit = compilationUnit.ReplaceNode(
                    block,
                    SyntaxFactory.Block(SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.LocalDeclarationStatement(
                        SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                            .WithVariables(SyntaxFactory.SeparatedList<VariableDeclaratorSyntax>(syntaxNodesOrToken))))
                        .AddRange(block.Statements)));

                compilation = compilation.ReplaceSyntaxTree(compilationUnit.SyntaxTree, newCompilationUnit.SyntaxTree);
                compilationUnit = newCompilationUnit;
            }

            block = compilationUnit.DescendantNodes().OfType<BlockSyntax>().First();

            // Perform semantic analysis
            var semanticModel = compilation.GetSemanticModel(compilationUnit.SyntaxTree);

            // Perform data flow analysis to know which one is input, which one is output, which one is declared
            var dataFlow = semanticModel.AnalyzeDataFlow(block.Statements[unresolvedSymbols.Count > 0 ? 1 : 0], block.Statements.Last());

            // Input: belongs to unresolvedSymbols and DataFlowsIn
            // InOut: Input + belongs to WrittenInside
            foreach (var input in dataFlow.DataFlowsIn)
            {
                if (unresolvedSymbols.Contains(input.Name))
                {
                    // Input value
                    newSlots.Add(new Slot(SlotDirection.Input, SlotKind.Value, input.Name));

                    // Check if it's also an output
                    if (dataFlow.WrittenInside.Contains(input))
                    {
                        // Output value
                        newSlots.Add(new Slot(SlotDirection.Output, SlotKind.Value, input.Name));
                    }
                }
            }

            // Output: belongs to VariablesDeclared, AlwaysAssigned, and available after the scope
            foreach (var declaredVariable in dataFlow.VariablesDeclared)
            {
                if (dataFlow.AlwaysAssigned.Contains(declaredVariable)
                    && semanticModel.LookupSymbols(block.Statements.Last().Span.End, null, declaredVariable.Name).Length > 0)
                {
                    // Output value
                    newSlots.Add(new Slot(SlotDirection.Output, SlotKind.Value, declaredVariable.Name));
                }
            }

            // Find if return type is not void
            var returnStatement = block.Statements.Last() as ReturnStatementSyntax;
            if (returnStatement != null)
            {
                var returnType = semanticModel.GetTypeInfo(returnStatement.Expression);

                if (returnType.Type.SpecialType != SpecialType.System_Void)
                {
                    // Output value
                    newSlots.Add(new Slot(SlotDirection.Output, SlotKind.Value));
                }
            }
        }

        public static CompilationUnitSyntax CreateCompilationUnitFromBlock(ref BlockSyntax block)
        {
            var compilationUnit =
                SyntaxFactory.CompilationUnit()
                    .WithMembers(
                        SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                            SyntaxFactory.ClassDeclaration("C")
                                .WithMembers(
                                    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                                        SyntaxFactory.MethodDeclaration(
                                                SyntaxFactory.PredefinedType(
                                                    SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                                                SyntaxFactory.Identifier("M"))
                                            .WithBody(block)))))
                    .NormalizeWhitespace();

            block = compilationUnit.DescendantNodes().OfType<BlockSyntax>().First();

            return compilationUnit;
        }
    }
}
