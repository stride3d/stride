// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Stride.Assets;

public static partial class CodeUpgrades
{
    /// <summary>
    /// Adds the attribute <paramref name="attributeType"/> (a metadata name) to every class or struct that declares a
    /// static field whose type derives from <paramref name="fieldBaseType"/>, unless the type already has it. A partial
    /// type gets it once, on the part that declares its first such field. The attribute may not exist in the
    /// old-version closure, so it is matched and written by name: short when its namespace is in scope, qualified otherwise.
    /// </summary>
    public static CodeUpgrade AddAttributeToStaticFieldHolders(string fieldBaseType, string attributeType)
    {
        ArgumentNullException.ThrowIfNull(fieldBaseType);
        ArgumentNullException.ThrowIfNull(attributeType);
        var lastDot = attributeType.LastIndexOf('.');
        var attributeNamespace = lastDot >= 0 ? attributeType[..lastDot] : null;
        var shortName = attributeType[(lastDot + 1)..];
        if (shortName.EndsWith("Attribute", StringComparison.Ordinal))
            shortName = shortName[..^"Attribute".Length];
        var qualifiedName = attributeNamespace is null ? shortName : attributeNamespace + "." + shortName;

        return async (solution, targets, cancellationToken) =>
        {
            foreach (var projectId in targets)
            {
                var project = solution.GetProject(projectId);
                if (project is null)
                    continue;

                foreach (var documentId in project.DocumentIds)
                {
                    var document = solution.GetDocument(documentId);
                    if (document is null || !SymbolRewriteEngine.IsUpgradableSource(document))
                        continue;

                    var root = await document.GetSyntaxRootAsync(cancellationToken);
                    var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
                    var baseType = semanticModel?.Compilation.GetTypeByMetadataName(fieldBaseType);
                    if (root is null || baseType is null)
                        continue;

                    var names = new Dictionary<TypeDeclarationSyntax, string>();
                    foreach (var declaration in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
                    {
                        if (declaration is not (ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax))
                            continue;
                        if (semanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol type)
                            continue;

                        // Fields of generated parts (shader keys) are named by their generator
                        var firstField = type.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(x =>
                            x.IsStatic && !x.IsImplicitlyDeclared && DerivesFrom(x.Type, baseType)
                            && x.DeclaringSyntaxReferences.Length > 0 && SymbolRewriteEngine.IsUpgradableSource(solution, x.DeclaringSyntaxReferences[0].SyntaxTree));
                        if (firstField is null
                            || firstField.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken).FirstAncestorOrSelf<TypeDeclarationSyntax>() != declaration
                            || HasAttributeNamed(type, shortName, cancellationToken))
                            continue;

                        names[declaration] = IsNamespaceInScope(semanticModel, declaration, type, attributeNamespace, cancellationToken) ? shortName : qualifiedName;
                    }
                    if (names.Count == 0)
                        continue;

                    var endOfLine = root.DescendantTrivia().FirstOrDefault(x => x.IsKind(SyntaxKind.EndOfLineTrivia));
                    if (!endOfLine.IsKind(SyntaxKind.EndOfLineTrivia))
                        endOfLine = SyntaxFactory.EndOfLine(Environment.NewLine);

                    var newRoot = root.ReplaceNodes(names.Keys, (original, rewritten) => AddAttributeList(rewritten, names[original], endOfLine));
                    solution = solution.WithDocumentSyntaxRoot(documentId, newRoot);
                }
            }
            return solution;
        };
    }
}
