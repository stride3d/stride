// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv.Building;

namespace Stride.Shaders.Parsing.SDSL.AST;

/// <summary>
/// Partial structs: <c>partial struct X</c> declares X in its owner class, <c>extend struct X</c> in a class that inherits
/// or imports the owner adds fields to it. A class sees a view of X: the fields of the parts it can reach. The linker joins
/// the parts of the effect into one struct (ShaderMixer.PartialStructs.cs).
/// </summary>
internal static class PartialStructs
{
    private sealed class PartialStruct(string name, string owner)
    {
        public string Name { get; } = name;
        public string Owner { get; } = owner;
        public List<(string Name, SymbolType Type, TypeModifier Modifier, string Declarer)> Fields { get; } = [];
        /// <summary>The <c>partial struct</c> / <c>extend struct</c> elements of the class being compiled.</summary>
        public List<ShaderStruct> OwnParts { get; } = [];
    }

    /// <summary>
    /// Declares the structs of the class: the view of each partial struct it sees (inherited, imported, or its own) and its
    /// plain structs, each after the structs it contains, so a partial struct can have a struct field and a plain struct can
    /// contain a partial struct. Also reports two plain structs of one name reached through two classes (SDSL0215).
    /// </summary>
    /// <returns>false when a struct contains itself (SDSL0128): nothing more can be declared.</returns>
    public static bool DeclareStructs(SymbolTable table, SpirvContext context, ShaderClass shaderClass)
    {
        var className = shaderClass.Name.Name;
        var partials = new Dictionary<string, PartialStruct>();
        var plainOwners = new Dictionary<string, string>();

        foreach (var inherited in table.InheritedShaders)
        {
            var shader = inherited.Symbol!;
            foreach (var (type, _) in shader.StructTypes)
            {
                if (type is not StructType structType)
                    continue;
                if (structType.PartialOwner is { } owner)
                {
                    if (!partials.TryGetValue(structType.Name, out var partial))
                        partials.Add(structType.Name, partial = new(structType.Name, owner));
                    else if (partial.Owner != owner)
                    {
                        table.AddError(new(shaderClass.Name.Info, string.Format(SDSLErrorMessages.SDSL0124, className, structType.Name, partial.Owner, owner)));
                        continue;
                    }
                    for (var index = 0; index < structType.Members.Count; index++)
                    {
                        var member = structType.Members[index];
                        AddField(table, shaderClass.Name.Info, partial, member.Name, member.Type, member.TypeModifier, structType.PartialDeclarers![index]);
                    }
                }
                else if (!plainOwners.TryAdd(structType.Name, shader.Name) && plainOwners[structType.Name] != shader.Name)
                {
                    table.AddWarning(new(shaderClass.Name.Info, string.Format(SDSLErrorMessages.SDSL0215, className, structType.Name, plainOwners[structType.Name], shader.Name)));
                }
            }
        }

        foreach (var element in shaderClass.Elements.OfType<ShaderStruct>())
        {
            var name = element.TypeName.Name;
            switch (element.Kind)
            {
                case ShaderStructKind.Plain:
                    if (partials.TryGetValue(name, out var hidden))
                        table.AddError(new(element.TypeName.Info, string.Format(SDSLErrorMessages.SDSL0123, className, name, hidden.Owner)));
                    break;
                case ShaderStructKind.Partial:
                    if (partials.TryGetValue(name, out var existing))
                    {
                        table.AddError(new(element.TypeName.Info, string.Format(SDSLErrorMessages.SDSL0123, className, name, existing.Owner)));
                        break;
                    }
                    if (shaderClass.Generics is { Parameters.Count: > 0 })
                    {
                        table.AddError(new(element.TypeName.Info, string.Format(SDSLErrorMessages.SDSL0126, className, name)));
                        break;
                    }
                    var declared = new PartialStruct(name, className);
                    partials.Add(name, declared);
                    declared.OwnParts.Add(element);
                    break;
                case ShaderStructKind.Extend:
                    if (partials.TryGetValue(name, out var extended))
                        extended.OwnParts.Add(element);
                    else if (plainOwners.TryGetValue(name, out var plainOwner))
                        table.AddError(new(element.TypeName.Info, string.Format(SDSLErrorMessages.SDSL0122, className, name, plainOwner)));
                    else
                        table.AddError(new(element.TypeName.Info, string.Format(SDSLErrorMessages.SDSL0121, className, name)));
                    break;
            }
        }

        // A plain struct hidden by a partial struct is an error (SDSL0123), not declared
        var plains = shaderClass.Elements.OfType<ShaderStruct>().Where(x => x.Kind == ShaderStructKind.Plain && !partials.ContainsKey(x.TypeName.Name)).ToList();
        var plainsByName = new Dictionary<string, ShaderStruct>();
        foreach (var plain in plains)
            plainsByName.TryAdd(plain.TypeName.Name, plain);

        // Depth first, in declaration order: a struct is declared after the structs it contains. The path is the chain of
        // fields from the first struct being declared, for the message of a cycle.
        var done = new HashSet<string>();
        var declaredPlains = new HashSet<ShaderStruct>();
        var path = new List<(string Struct, string Field, string Declarer)>();
        foreach (var plain in plains)
        {
            if (!Declare(plain.TypeName.Name, plain))
                return false;
        }
        foreach (var partial in partials.Values)
        {
            if (!Declare(partial.Name, null))
                return false;
        }
        return true;

        // plain: a plain struct of this class, given in declaration order (a second plain struct of one name is declared too,
        // and fails as before). A struct of another class is only walked, for the cycle check.
        bool Declare(string name, ShaderStruct? plain)
        {
            var partial = plain == null ? partials.GetValueOrDefault(name) : null;
            if (plain == null && partial == null)
                plain = plainsByName.GetValueOrDefault(name);
            if (plain != null ? declaredPlains.Contains(plain) : done.Contains(name))
                return true;
            if (path.FindIndex(x => x.Struct == name) is var start and not -1)
            {
                var chain = string.Join(" -> ", path.Skip(start).Select(x => $"{x.Struct}.{x.Field} ({x.Declarer})").Append(name));
                table.AddError(new(partial?.OwnParts.FirstOrDefault()?.TypeName.Info ?? plain?.TypeName.Info ?? shaderClass.Name.Info, string.Format(SDSLErrorMessages.SDSL0128, name, chain)));
                return false;
            }

            foreach (var (field, uses, declarer) in Uses(name, partial, plain))
            {
                path.Add((name, field, declarer));
                foreach (var used in uses)
                {
                    if (!Declare(used, null))
                        return false;
                }
                path.RemoveAt(path.Count - 1);
            }

            if (partial != null)
            {
                DeclareView(table, context, className, partial);
            }
            else if (plain != null)
            {
                plain.ProcessSymbol(table, context);
                declaredPlains.Add(plain);
            }
            done.Add(name);
            return true;
        }

        // The fields of a struct and the struct names their types use (through arrays and the plain structs of other classes)
        IEnumerable<(string Field, IEnumerable<string> Uses, string Declarer)> Uses(string name, PartialStruct? partial, ShaderStruct? plain)
        {
            if (partial != null)
            {
                foreach (var field in partial.Fields)
                    yield return (field.Name, StructNames(field.Type), field.Declarer);
                foreach (var member in partial.OwnParts.SelectMany(x => x.Members))
                    yield return (member.Name.Name, StructNames(member.TypeName), className);
            }
            else if (plain != null)
            {
                foreach (var member in plain.Members)
                    yield return (member.Name.Name, StructNames(member.TypeName), className);
            }
            // A struct of another class
            else if (table.DeclaredTypes.TryGetValue(name, out var type) && type is StructType { PartialOwner: null } other)
            {
                foreach (var member in other.Members)
                    yield return (member.Name, StructNames(member.Type), plainOwners.GetValueOrDefault(name, "?"));
            }
        }

        IEnumerable<string> StructNames(object type)
        {
            switch (type)
            {
                case TypeName typeName when partials.ContainsKey(typeName.Name) || plainsByName.ContainsKey(typeName.Name):
                    yield return typeName.Name;
                    break;
                case TypeName typeName when table.DeclaredTypes.TryGetValue(typeName.Name, out var declaredType):
                    foreach (var used in StructNames(declaredType))
                        yield return used;
                    break;
                case ArrayType array:
                    foreach (var used in StructNames(array.BaseType))
                        yield return used;
                    break;
                case StructType structType:
                    yield return structType.Name;
                    break;
            }
        }
    }

    /// <summary>
    /// Declares the view of a partial struct: the fields of the parts the class reaches, its own fields resolved now.
    /// </summary>
    private static void DeclareView(SymbolTable table, SpirvContext context, string className, PartialStruct partial)
    {
        foreach (var element in partial.OwnParts)
        {
            foreach (var member in element.Members)
            {
                member.TypeName.ProcessSymbol(table);
                member.Type = member.TypeName.Type;
                if (member.Type == null)
                    continue;
                table.DeclaredTypes.TryAdd(member.Type.ToString(), member.Type);
                AddField(table, member.Name.Info, partial, member.Name.Name, member.Type, member.TypeModifier, className);
            }
        }

        // Owner first, then the other parts by class name: the same set of classes gives the same layout in every effect
        var fields = partial.Fields
            .Select((field, index) => (field, index))
            .OrderBy(x => x.field.Declarer == partial.Owner ? 0 : 1)
            .ThenBy(x => x.field.Declarer, StringComparer.Ordinal)
            .ThenBy(x => x.index)
            .Select(x => x.field)
            .ToList();
        var view = new StructType(partial.Name, fields.Select(x => new StructuredTypeMember(x.Name, x.Type, x.Modifier)).ToList())
        {
            PartialOwner = partial.Owner,
            PartialDeclarers = fields.Select(x => x.Declarer).ToList(),
        };
        table.DeclaredTypes[partial.Name] = view;
        context.DeclarePartialStructView(view, context.Bound++);
        foreach (var element in partial.OwnParts)
            element.Type = view;
    }

    private static void AddField(SymbolTable table, TextLocation location, PartialStruct partial, string name, SymbolType type, TypeModifier modifier, string declarer)
    {
        var existing = partial.Fields.FindIndex(x => x.Name == name);
        if (existing == -1)
            partial.Fields.Add((name, type, modifier, declarer));
        // The same part reached twice (two classes import its declarer) is one part
        else if (partial.Fields[existing].Declarer != declarer)
            table.AddError(new(location, string.Format(SDSLErrorMessages.SDSL0120, partial.Name, name, partial.Fields[existing].Declarer, declarer)));
    }

    /// <summary>
    /// A partial struct changes per effect, so it can't be where the layout is shared with C#, another stage or another
    /// effect: streams, cbuffers, buffers, groupshared (SDSL0125). Locals, parameters and static variables are fine.
    /// </summary>
    public static void CheckFixedLayout(SymbolTable table, TextLocation location, string what, string name, SymbolType type)
    {
        if (Find(type) is { } partial)
            table.AddError(new(location, string.Format(SDSLErrorMessages.SDSL0125, what, name, type, partial.Name)));
    }

    /// <summary>
    /// The partial struct in a type, if any (through arrays, plain struct members, pointers and buffer elements).
    /// </summary>
    public static StructType? Find(SymbolType? type) => type switch
    {
        StructType { PartialOwner: not null } partial => partial,
        StructType plain => plain.Members.Select(x => Find(x.Type)).FirstOrDefault(x => x != null),
        ArrayType array => Find(array.BaseType),
        PointerType pointer => Find(pointer.BaseType),
        StructuredBufferType buffer => Find(buffer.BaseType),
        AppendStructuredBufferType buffer => Find(buffer.BaseType),
        ConsumeStructuredBufferType buffer => Find(buffer.BaseType),
        _ => null,
    };
}
