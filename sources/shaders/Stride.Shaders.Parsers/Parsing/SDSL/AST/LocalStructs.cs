// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv.Building;

namespace Stride.Shaders.Parsing.SDSL.AST;

/// <summary>
/// A <c>struct</c> declared inside a method. <see cref="LocalStructs.Hoist"/> moves it to the class under a unique name
/// and renames its uses, so the statement left in the method does nothing.
/// </summary>
public partial class LocalStructDeclaration(ShaderStruct @struct, TextLocation info) : Statement(info)
{
    public ShaderStruct Struct { get; set; } = @struct;
    public bool IsHoisted { get; set; }

    public override SymbolType? Type { get => ScalarType.Void; set { } }

    public override void ProcessSymbol(SymbolTable table)
    {
        if (!IsHoisted)
            table.AddError(new(Info, $"struct {Struct.TypeName} can only be declared in a method of a shader class"));
    }

    public override void Compile(SymbolTable table, CompilerUnit compiler) { }

    public override string ToString() => Struct.ToString();
}

public static class LocalStructs
{
    /// <summary>
    /// Moves the structs declared in <paramref name="method"/> to <paramref name="shaderClass"/> (before the method, in source order),
    /// each under a name unique in the class. A struct's name stays scoped to the rest of its block: only the type names there are
    /// renamed, so two methods (or two blocks) can declare the same name, and a local struct can hide a class member's name.
    /// </summary>
    public static void Hoist(ShaderClass shaderClass, ShaderMethod method)
    {
        if (method.Body == null)
            return;

        var hoister = new Hoister(shaderClass, method);
        hoister.VisitNode(method.Body);
        foreach (var declaration in hoister.Declarations.OrderBy(x => x.Info.Range.StartsAt(x.Info.Original.Length)))
            shaderClass.Elements.Add(declaration.Struct);
    }

    private sealed class Hoister(ShaderClass shaderClass, ShaderMethod method) : NodeWalker
    {
        public List<LocalStructDeclaration> Declarations { get; } = [];

        public override void VisitBlockStatement(BlockStatement blockStatement)
        {
            // Inner blocks first: their structs hide the outer ones of the same name
            base.VisitBlockStatement(blockStatement);

            var statements = blockStatement.Statements;
            for (var i = 0; i < statements.Count; i++)
            {
                if (statements[i] is not LocalStructDeclaration { IsHoisted: false } declaration)
                    continue;

                var name = declaration.Struct.TypeName.Name;
                var uniqueName = UniqueName($"{name}_in_{method.Name.Name}");
                declaration.Struct.TypeName = new Identifier(uniqueName, declaration.Struct.TypeName.Info);
                declaration.IsHoisted = true;
                Declarations.Add(declaration);

                var renamer = new TypeNameRenamer(name, uniqueName);
                for (var j = i + 1; j < statements.Count; j++)
                    renamer.VisitNode(statements[j]);
            }
        }

        private string UniqueName(string name)
        {
            var uniqueName = name;
            for (var index = 2; IsUsed(uniqueName); index++)
                uniqueName = $"{name}{index}";
            return uniqueName;
        }

        private bool IsUsed(string name)
            => shaderClass.Elements.OfType<ShaderStruct>().Any(x => x.TypeName.Name == name)
               || Declarations.Any(x => x.Struct.TypeName.Name == name);
    }

    private sealed class TypeNameRenamer(string from, string to) : NodeWalker
    {
        public override void VisitTypeName(TypeName typeName)
        {
            if (typeName.Name == from)
                typeName.Name = to;
            base.VisitTypeName(typeName);
        }
    }
}
