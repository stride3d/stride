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
    internal class TypeVisitorGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterImplementationSourceOutput(
                context.CompilationProvider, GenerateVisitors);
        }

        private void GenerateVisitors(SourceProductionContext context, Compilation compilation)
        {
            var classVisitor = new SymbolTypeClassFinder();
            classVisitor.Visit(compilation.GlobalNamespace);

            var symbolTypes = classVisitor.SymbolTypes;

            var sb = new StringBuilder();

            var typeAndGenericFormat = new SymbolDisplayFormat(SymbolDisplayGlobalNamespaceStyle.Omitted, SymbolDisplayTypeQualificationStyle.NameOnly, SymbolDisplayGenericsOptions.IncludeTypeParameters);

            // Source: Stride old shader system VisitorGenerated.tt preprocessed with RuntimeTextTemplate1.tt and linePragmas="false"
            sb.Append("namespace" +
                    " Stride.Shaders.Core\r\n{\r\n    public partial class TypeVisitor<TResult>" +
                    "\r\n    {\r\n");
            foreach (var type in symbolTypes)
            {
                var typeName = type.ToDisplayString();
                var variableName = type.Name.First().ToString().ToLower() + type.Name.Substring(1);
                var genericParameters = type.IsGenericType ? $"<{string.Join(",", type.TypeArguments)}>" : string.Empty;

                var returnType = type.IsValueType ? "bool" : "TResult";
                sb.AppendLine($"        public virtual {returnType} Visit{genericParameters}({(type.IsValueType ? "ref " : "")}{typeName} {variableName})");
                sb.AppendLine("{");
                if (type.IsValueType)
                    sb.AppendLine($"return DefaultVisit(ref {variableName});");
                else
                    sb.AppendLine($"return DefaultVisit({variableName});");
                sb.AppendLine("}");
            }
            sb.Append("    }\r\n\r\n    public partial class TypeRewriter\r\n    {\r\n");
            foreach (var type in symbolTypes)
            {
                var typeName = type.ToDisplayString();
                var variableName = type.Name.First().ToString().ToLower() + type.Name.Substring(1);
                var genericParameters = type.IsGenericType ? $"<{string.Join(",", type.TypeArguments)}>" : string.Empty;

                var returnType = type.IsValueType ? "bool" : "SymbolType";
                sb.AppendLine($"        public override {returnType} Visit{genericParameters}({(type.IsValueType ? "ref " : "")}{typeName} {variableName})");
                sb.AppendLine("{");
                // Process public fields and properties (with getter+setter)
                var ilistName = typeof(IList<>).FullName.Replace("`1", "<>");
                foreach (var member in GetNodeMembers(type))
                {
                    var memberType = GetSymbolType(member);
                    var memberTypeName = memberType.ToDisplayString();
                    var memberVariableName = member.Name.First().ToString().ToLower() + member.Name.Substring(1) + "Temp";
                    var nodeListElementType = memberType.AllInterfaces.FirstOrDefault(x => x.IsGenericType && x.ConstructUnboundGenericType().ToDisplayString() == ilistName && IsSymbolType(x.TypeArguments[0]))?.TypeArguments[0];
                    var isNode = IsSymbolType(memberType);
                    if (isNode)
                    {
                        sb.AppendLine($"            var {memberVariableName} = ({memberTypeName})Visit{(memberType.IsValueType ? "Node" : "Type")}({variableName}.{member.Name});");
                        sb.AppendLine($"            if (!ReferenceEquals({memberVariableName}, {variableName}.{member.Name}))");
                        sb.AppendLine($"                {variableName} = {variableName} with {{ {member.Name} = {memberVariableName} }};");
                    }
                    else if (nodeListElementType != null)
                    {
                        sb.AppendLine($"            var {memberVariableName} = ({memberTypeName})Visit{(nodeListElementType.IsValueType ? "Node" : "Type")}List({variableName}.{member.Name});");
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
            sb.Append("    }\r\n\r\n    public partial class TypeVisitor\r\n    {\r\n");
            foreach (var type in symbolTypes)
            {
                var typeName = type.ToDisplayString();
                var variableName = type.Name.First().ToString().ToLower() + type.Name.Substring(1);
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
            sb.Append("    }\r\n\r\n    public partial class TypeWalker\r\n    {\r\n");
            foreach (var type in symbolTypes)
            {
                var typeName = type.ToDisplayString();
                var variableName = type.Name.First().ToString().ToLower() + type.Name.Substring(1);
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
                foreach (var member in GetNodeMembers(type))
                {
                    var memberType = GetSymbolType(member);
                    var memberTypeName = memberType.ToDisplayString();
                    var nodeListElementType = memberType.AllInterfaces.FirstOrDefault(x => x.IsGenericType && x.ConstructUnboundGenericType().ToDisplayString() == ilistName && IsSymbolType(x.TypeArguments[0]))?.TypeArguments[0];
                    var isNode = IsSymbolType(memberType);
                    if (isNode)
                    {
                        sb.Append($"            Visit{(memberType.IsValueType ? "Node" : "Type")}(");
                        sb.Append(variableName);
                        sb.Append(".");
                        sb.Append(member.Name);
                        sb.Append(");\r\n");
                    }
                    else if (nodeListElementType != null)
                    {
                        sb.Append($"            Visit{(nodeListElementType.IsValueType ? "Node" : "Type")}List(");
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
            sb.Append("    }\r\n}\r\n\r\n");

            foreach (var type in symbolTypes)
            {
                sb.Append("namespace ");
                sb.Append(type.ContainingNamespace.ToDisplayString());
                sb.AppendLine("{");
                sb.AppendLine("public partial record");
                if (type.IsValueType)
                    sb.Append("struct ");
                sb.Append(type.ToDisplayString(typeAndGenericFormat));
                sb.Append(@$"
    {{
        public {(!type.IsValueType ? "override" : string.Empty)} void Accept(TypeVisitor visitor)
        {{");
                sb.AppendLine("visitor.Visit(this);");
                sb.AppendLine("}");
                if (type.IsValueType)
                {
                    sb.AppendLine("public bool Accept<TResult>(TypeVisitor<TResult> visitor)");
                    sb.AppendLine("{");
                    sb.AppendLine("return visitor.Visit(ref this);");
                }
                else
                {
                    sb.AppendLine("public override TResult Accept<TResult>(TypeVisitor<TResult> visitor)");
                    sb.AppendLine("{");
                    sb.AppendLine("return visitor.Visit(this);");
                }
                sb.AppendLine("} } }");
            }
            sb.AppendLine();

            context.AddSource("TypeVisitors.gen.cs",
                SourceText.From(
                    SyntaxFactory
                    .ParseCompilationUnit(sb.ToString())
                    .NormalizeWhitespace()
                    .ToFullString(),
                    Encoding.UTF8
                )
            );
        }

        private static IEnumerable<INamedTypeSymbol> GetNodeTypes(INamedTypeSymbol symbol)
        {
            while (symbol != null && IsSymbolType(symbol))
            {
                yield return symbol;
                symbol = symbol.BaseType;
            }
        }

        private static IEnumerable<ISymbol> GetNodeMembers(INamedTypeSymbol nodeType)
        {
            foreach (var currentNodeType in GetNodeTypes(nodeType).Reverse())
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

            if (type.IsValueType && type.Interfaces.Any(t => t.ToDisplayString() == "Stride.Shaders.Core.ISymbolTypeNode"))
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


        class SymbolTypeClassFinder : SymbolVisitor
        {
            public List<INamedTypeSymbol> SymbolTypes = new List<INamedTypeSymbol>();

            public override void VisitNamedType(INamedTypeSymbol symbol)
            {
                if (IsSymbolType(symbol) && !symbol.IsAbstract)
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
