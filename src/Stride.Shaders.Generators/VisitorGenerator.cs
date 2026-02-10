using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Stride.Shaders.Spirv.Generators
{
    [Generator]
    internal class VisitorGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterImplementationSourceOutput(context.CompilationProvider, GenerateTypeVisitors);
            //context.RegisterImplementationSourceOutput(context.CompilationProvider, GenerateNodeVisitors);
        }

        private void GenerateTypeVisitors(SourceProductionContext context, Compilation compilation)
        {
            GenerateVisitorsBase(context, compilation, true, "Type", IsSymbolType);
        }

        private void GenerateNodeVisitors(SourceProductionContext context, Compilation compilation)
        {
            GenerateVisitorsBase(context, compilation, false, "Node", IsNodeType);
        }

        private string GenerateVariableName(string name)
        {
            var variableName = name.First().ToString().ToLower() + name.Substring(1);
            if (variableName is "if" or "else" or "continue" or "while" or "return" or "break" or "for")
                variableName = "@" + variableName;

            return variableName;
        }
        
        private void GenerateVisitorsBase(SourceProductionContext context, Compilation compilation, bool generateRewriter, string visitorName, Func<ITypeSymbol, bool> isNodeType)
        {
            var classVisitor = new NodeTypeClassFinder(isNodeType);
            classVisitor.Visit(compilation.GlobalNamespace);

            var symbolTypes = classVisitor.SymbolTypes;

            var sb = new StringBuilder();

            var typeAndGenericFormat = new SymbolDisplayFormat(SymbolDisplayGlobalNamespaceStyle.Omitted, SymbolDisplayTypeQualificationStyle.NameOnly, SymbolDisplayGenericsOptions.IncludeTypeParameters);

            // Source: Stride old shader system VisitorGenerated.tt preprocessed with RuntimeTextTemplate1.tt and linePragmas="false"
            sb.AppendLine("using Stride.Shaders.Core;");
            sb.AppendLine("namespace Stride.Shaders.Core");
            sb.AppendLine("{");
            sb.Append($"    public partial class {visitorName}Visitor");
            sb.AppendLine("    {");
            foreach (var type in symbolTypes)
            {
                var typeName = type.ToDisplayString();
                var variableName = GenerateVariableName(type.Name);
                var genericParameters = type.IsGenericType ? $"<{string.Join(",", type.TypeArguments)}>" : string.Empty;

                sb.Append("        public virtual void Visit");
                sb.Append(genericParameters);
                sb.Append("(");
                sb.Append(typeName);
                sb.Append(" ");
                sb.Append(variableName);
                sb.Append(")\r\n        {\r\n            DefaultVisit(");
                sb.Append(variableName);
                sb.Append(");\r\n        }\r\n");
            }
            sb.Append($"    }}\r\n\r\n    public partial class {visitorName}Walker\r\n    {{\r\n");
            foreach (var type in symbolTypes)
            {
                var typeName = type.ToDisplayString();
                var variableName = GenerateVariableName(type.Name);
                var genericParameters = type.IsGenericType ? $"<{string.Join(",", type.TypeArguments)}>" : string.Empty;

                sb.Append("        public override void Visit");
                sb.Append(genericParameters);
                sb.Append("(");
                sb.Append(typeName);
                sb.Append(" ");
                sb.Append(variableName);
                sb.Append(")\r\n        {\r\n");
                // Process public fields and properties (with getter+setter)
                var ilistName = typeof(IList<>).FullName.Replace("`1", "<>");
                foreach (var member in GetNodeMembers(type, isNodeType))
                {
                    var memberType = GetSymbolType(member);
                    var memberTypeName = memberType.ToDisplayString();
                    var nodeListElementType = memberType.AllInterfaces.FirstOrDefault(x => x.IsGenericType && x.ConstructUnboundGenericType().ToDisplayString() == ilistName && isNodeType(x.TypeArguments[0]))?.TypeArguments[0];
                    var isNode = isNodeType(memberType);
                    if (isNode)
                    {
                        sb.Append($"            Visit{(memberType.IsValueType ? "Item" : visitorName)}(");
                        sb.Append(variableName);
                        sb.Append(".");
                        sb.Append(member.Name);
                        sb.Append(");\r\n");
                    }
                    else if (nodeListElementType != null)
                    {
                        sb.Append($"            Visit{(nodeListElementType.IsValueType ? "Item" : visitorName)}List(");
                        sb.Append(variableName);
                        sb.Append(".");
                        sb.Append(member.Name);
                        sb.Append(");\r\n");
                    }
                }
                sb.Append("            base.Visit");
                sb.Append(genericParameters);
                sb.Append("(");
                sb.Append(variableName);
                sb.Append(");\r\n        }\r\n");
            }

            sb.AppendLine("    }");
            
            if (generateRewriter)
            {
                sb.AppendLine($"    public partial class {visitorName}Visitor<TResult>");
                sb.AppendLine("    {");
                foreach (var type in symbolTypes)
                {
                    var typeName = type.ToDisplayString();
                    var variableName = GenerateVariableName(type.Name);
                    var genericParameters = type.IsGenericType ? $"<{string.Join(",", type.TypeArguments)}>" : string.Empty;
                
                    if (variableName is "if" or "else" or "continue" or "while" or "return" or "break" or "for")
                    {
                        variableName = "@" + variableName;
                    }

                    var returnType = type.IsValueType ? "bool" : "TResult";
                    sb.AppendLine($"        public virtual {returnType} Visit{genericParameters}({(type.IsValueType ? "ref " : "")}{typeName} {variableName})");
                    sb.AppendLine("{");
                    if (type.IsValueType)
                        sb.AppendLine($"return DefaultVisit(ref {variableName});");
                    else
                        sb.AppendLine($"return DefaultVisit({variableName});");
                    sb.AppendLine("}");
                }

                sb.AppendLine("    }");
                
                sb.AppendLine($"    public partial class {visitorName}Rewriter");
                sb.AppendLine("    {");
                foreach (var type in symbolTypes)
                {
                    var typeName = type.ToDisplayString();
                    var variableName = GenerateVariableName(type.Name);
                    var genericParameters = type.IsGenericType ? $"<{string.Join(",", type.TypeArguments)}>" : string.Empty;

                    var returnType = type.IsValueType ? "bool" : "SymbolType";
                    sb.AppendLine($"        public override {returnType} Visit{genericParameters}({(type.IsValueType ? "ref " : "")}{typeName} {variableName})");
                    sb.AppendLine("{");
                    // Process public fields and properties (with getter+setter)
                    var ilistName = typeof(IList<>).FullName.Replace("`1", "<>");
                    foreach (var member in GetNodeMembers(type, isNodeType))
                    {
                        var memberType = GetSymbolType(member);
                        var memberTypeName = memberType.ToDisplayString();
                        var memberVariableName = GenerateVariableName(member.Name + "Temp");
                        var nodeListElementType = memberType.AllInterfaces.FirstOrDefault(x => x.IsGenericType && x.ConstructUnboundGenericType().ToDisplayString() == ilistName && isNodeType(x.TypeArguments[0]))?.TypeArguments[0];
                        var isNode = isNodeType(memberType);
                        if (isNode)
                        {
                            sb.AppendLine($"            var {memberVariableName} = ({memberTypeName})Visit{(memberType.IsValueType ? "Item" : visitorName)}({variableName}.{member.Name});");
                            sb.AppendLine($"            if (!ReferenceEquals({memberVariableName}, {variableName}.{member.Name}))");
                            sb.AppendLine($"                {variableName} = {variableName} with {{ {member.Name} = {memberVariableName} }};");
                        }
                        else if (nodeListElementType != null)
                        {
                            sb.AppendLine($"            var {memberVariableName} = ({memberTypeName})Visit{(nodeListElementType.IsValueType ? "Item" : visitorName)}List({variableName}.{member.Name});");
                            sb.AppendLine($"            if (!ReferenceEquals({memberVariableName}, {variableName}.{member.Name}))");
                            sb.AppendLine($"                {variableName} = {variableName} with {{ {member.Name} = {memberVariableName} }};");
                        }
                    }
                    if (type.IsValueType)
                    {
                        sb.AppendLine($"            return base.Visit{genericParameters}(ref {variableName});");
                    }
                    else
                    {
                        sb.AppendLine($"            return (SymbolType)base.Visit{genericParameters}({variableName});");
                    }
                    sb.Append("}\r\n");
                }

                sb.AppendLine("    }");
            }
            
            sb.AppendLine("}");

            foreach (var type in symbolTypes)
            {
                sb.Append("namespace ");
                sb.Append(type.ContainingNamespace.ToDisplayString());
                sb.AppendLine("{");

                var typeKind = (type.IsRecord, type.IsValueType) switch
                {
                    (true, true) => "record struct",
                    (true, false) => "record",
                    (false, true) => "struct",
                    (false, false) => "class",
                };

                sb.AppendLine($"public partial {typeKind}");
                sb.Append(type.ToDisplayString(typeAndGenericFormat));
                sb.Append(@$"
    {{
        public {(!type.IsValueType ? "override" : string.Empty)} void Accept({visitorName}Visitor visitor)
        {{");
                sb.AppendLine("visitor.Visit(this);");
                sb.AppendLine("}");
                if (generateRewriter)
                {
                    if (type.IsValueType)
                    {
                        sb.AppendLine($"public bool Accept<TResult>({visitorName}Visitor<TResult> visitor)");
                        sb.AppendLine("{");
                        sb.AppendLine("return visitor.Visit(ref this);");
                        sb.AppendLine("}");
                    }
                    else
                    {
                        sb.AppendLine($"public override TResult Accept<TResult>({visitorName}Visitor<TResult> visitor)");
                        sb.AppendLine("{");
                        sb.AppendLine("return visitor.Visit(this);");
                        sb.AppendLine("}");
                    }
                }

                sb.AppendLine("} }");
            }
            sb.AppendLine();

            context.AddSource($"{visitorName}Visitors.gen.cs",
                SourceText.From(
                    SyntaxFactory
                    .ParseCompilationUnit(sb.ToString())
                    .NormalizeWhitespace()
                    .ToFullString(),
                    Encoding.UTF8
                )
            );
        }

        private static IEnumerable<INamedTypeSymbol> GetNodeTypes(INamedTypeSymbol symbol, Func<ITypeSymbol, bool> isNodeType)
        {
            while (symbol != null && isNodeType(symbol))
            {
                yield return symbol;
                symbol = symbol.BaseType;
            }
        }

        private static IEnumerable<ISymbol> GetNodeMembers(INamedTypeSymbol nodeType, Func<ITypeSymbol, bool> isNodeType)
        {
            foreach (var currentNodeType in GetNodeTypes(nodeType, isNodeType).Reverse())
            {
                foreach (var member in currentNodeType.GetMembers().Where(CanVisitMember))
                    yield return member;
            }
        }

        private static bool CanVisitMember(ISymbol symbol)
        {
            if (symbol.DeclaredAccessibility != Accessibility.Public || symbol.IsStatic)
                return false;

            if (symbol.GetAttributes().Any(x => x.AttributeClass.ToDisplayString() == "Stride.Core.Shaders.Ast.VisitorIgnoreAttribute"))
                return false;

            if (symbol.Kind == SymbolKind.Field)
            {
                var field = (IFieldSymbol)symbol;
                if (field.IsReadOnly)
                    return false;

                return true;
            }

            if (symbol.Kind == SymbolKind.Property)
            {
                var property = (IPropertySymbol)symbol;
                if (property.IsReadOnly || property.IsWriteOnly || property.IsIndexer)
                    return false;

                if (property.GetMethod == null || property.SetMethod == null)
                    return false;

                if (property.GetMethod.DeclaredAccessibility != Accessibility.Public
                  || property.SetMethod.DeclaredAccessibility != Accessibility.Public)
                    return false;

                return true;
            }

            return false;
        }

        private static bool IsSymbolType(ITypeSymbol type)
        {
            if (GetBaseTypesAndThis(type).Any(t => t.ToDisplayString() == "Stride.Shaders.Core.SymbolType"))
                return true;

            if (type.IsValueType && type.Interfaces.Any(t => t.ToDisplayString() == "Stride.Shaders.Core.ISymbolTypeItem"))
                return true;

            return false;
        }

        private static bool IsNodeType(ITypeSymbol type)
        {
            if (GetBaseTypesAndThis(type).Any(t => t.ToDisplayString() == "Stride.Shaders.Parsing.Node"))
                return true;

            return false;
        }
        
        private static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(ITypeSymbol type)
        {
            var current = type;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        private static ITypeSymbol GetSymbolType(ISymbol symbol)
        {
            var localSymbol = symbol as ILocalSymbol;
            if (localSymbol != null)
            {
                return localSymbol.Type;
            }

            var fieldSymbol = symbol as IFieldSymbol;
            if (fieldSymbol != null)
            {
                return fieldSymbol.Type;
            }

            var propertySymbol = symbol as IPropertySymbol;
            if (propertySymbol != null)
            {
                return propertySymbol.Type;
            }

            var parameterSymbol = symbol as IParameterSymbol;
            if (parameterSymbol != null)
            {
                return parameterSymbol.Type;
            }

            var aliasSymbol = symbol as IAliasSymbol;
            if (aliasSymbol != null)
            {
                return aliasSymbol.Target as ITypeSymbol;
            }

            return symbol as ITypeSymbol;
        }


        class NodeTypeClassFinder(Func<ITypeSymbol, bool> isNodeType) : SymbolVisitor
        {
            public List<INamedTypeSymbol> SymbolTypes = new();

            public override void VisitNamedType(INamedTypeSymbol symbol)
            {
                if (isNodeType(symbol) && !symbol.IsAbstract)
                    SymbolTypes.Add(symbol);
            }

            public override void VisitNamespace(INamespaceSymbol symbol)
            {
                foreach (var childSymbol in symbol.GetMembers())
                {
                    //We must implement the visitor pattern ourselves and 
                    //accept the child symbols in order to visit their children
                    childSymbol.Accept(this);
                }
            }
        }
    }
}
