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
    }

    /// <summary>
    /// Declares the view of each partial struct the class sees (inherited, imported, or its own), before its other structs
    /// so they can contain one. Also reports two plain structs of one name reached through two classes (SDSL0215).
    /// </summary>
    public static void DeclareViews(SymbolTable table, SpirvContext context, ShaderClass shaderClass)
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
                    AddOwnFields(table, shaderClass, element, declared);
                    break;
                case ShaderStructKind.Extend:
                    if (partials.TryGetValue(name, out var extended))
                        AddOwnFields(table, shaderClass, element, extended);
                    else if (plainOwners.TryGetValue(name, out var plainOwner))
                        table.AddError(new(element.TypeName.Info, string.Format(SDSLErrorMessages.SDSL0122, className, name, plainOwner)));
                    else
                        table.AddError(new(element.TypeName.Info, string.Format(SDSLErrorMessages.SDSL0121, className, name)));
                    break;
            }
        }

        foreach (var partial in partials.Values)
        {
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
            foreach (var element in shaderClass.Elements.OfType<ShaderStruct>())
            {
                if (element.Kind != ShaderStructKind.Plain && element.TypeName.Name == partial.Name)
                    element.Type = view;
            }
        }
    }

    private static void AddOwnFields(SymbolTable table, ShaderClass shaderClass, ShaderStruct element, PartialStruct partial)
    {
        var className = shaderClass.Name.Name;
        foreach (var member in element.Members)
        {
            // A struct of this class is declared after the partial structs: not resolved yet
            if (shaderClass.Elements.OfType<ShaderStruct>().Any(x => x.Kind == ShaderStructKind.Plain && x.TypeName.Name == member.TypeName.Name))
            {
                table.AddError(new(member.Name.Info, string.Format(SDSLErrorMessages.SDSL0127, className, partial.Name, member.Name.Name, member.TypeName)));
                continue;
            }
            member.TypeName.ProcessSymbol(table);
            member.Type = member.TypeName.Type;
            if (member.Type == null)
                continue;
            table.DeclaredTypes.TryAdd(member.Type.ToString(), member.Type);
            var baseType = member.Type;
            while (baseType is ArrayType arrayType)
                baseType = arrayType.BaseType;
            if (baseType is StructuredType)
            {
                table.AddError(new(member.Name.Info, string.Format(SDSLErrorMessages.SDSL0127, className, partial.Name, member.Name.Name, member.Type)));
                continue;
            }
            AddField(table, member.Name.Info, partial, member.Name.Name, member.Type, member.TypeModifier, className);
        }
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
