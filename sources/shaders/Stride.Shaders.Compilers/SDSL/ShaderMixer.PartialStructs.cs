// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Text;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Compilers.SDSL;

public partial class ShaderMixer
{
    /// <summary>
    /// The struct of the effect that joins the parts of one partial struct: the owner's fields first, then the other parts
    /// by class name, each in declaration order.
    /// </summary>
    private sealed class PartialStructLayout(string name, string owner)
    {
        public string Name { get; } = name;
        public string Owner { get; } = owner;
        public List<(string Name, SymbolType Type, TypeModifier Modifier, string Declarer)> Fields { get; } = [];
        public Dictionary<string, int> IndexByName { get; } = [];
        public int Id { get; set; }
    }

    /// <summary>
    /// Collects the views of each partial struct from every class of the effect and declares the joined struct, before
    /// the classes are merged (each class's view is then replaced by it, see <see cref="PartialStructRewriter"/>).
    /// The plain structs used by its fields are declared first, and replace the ones of their classes
    /// (<see cref="MixinGlobalContext.PartialFieldStructs"/>).
    /// </summary>
    private static void DeclarePartialStructs(MixinGlobalContext globalContext, SpirvContext context, ShaderMixinInstantiation root)
    {
        var layouts = globalContext.PartialStructs;
        var visited = new HashSet<ShaderMixinInstantiation>();
        // Plain struct name → class that declares it, for the message of a cycle
        var plainDeclarers = new Dictionary<string, string>();
        Collect(root);
        if (layouts.Count == 0)
            return;

        foreach (var layout in layouts.Values)
        {
            var fields = layout.Fields
                .Select((field, index) => (field, index))
                .OrderBy(x => x.field.Declarer == layout.Owner ? 0 : 1)
                .ThenBy(x => x.field.Declarer, StringComparer.Ordinal)
                .ThenBy(x => x.index)
                .Select(x => x.field)
                .ToList();
            layout.Fields.Clear();
            layout.Fields.AddRange(fields);
            for (var index = 0; index < fields.Count; index++)
                layout.IndexByName.Add(fields[index].Name, index);
        }

        // Depth first: a struct is declared after the structs its fields use. The path is the chain of fields from the
        // first struct being declared, for the message of a cycle (two classes can each add one half of it).
        var path = new List<(StructType Struct, string Field, string? Declarer)>();
        foreach (var layout in layouts.Values)
            Declare(new StructType(layout.Name, []) { PartialOwner = layout.Owner });

        void Declare(SymbolType type)
        {
            while (type is ArrayType array)
                type = array.BaseType;
            if (type is not StructType structType)
                return;
            var layout = structType.PartialOwner is { } owner ? layouts[(owner, structType.Name)] : null;
            if (layout != null ? layout.Id != 0 : context.Types.ContainsKey(structType))
                return;
            if (path.FindIndex(x => x.Struct.Equals(structType)) is var start and not -1)
            {
                var chain = string.Join(" -> ", path.Skip(start).Select(x => x.Declarer != null ? $"{x.Struct.Name}.{x.Field} ({x.Declarer})" : $"{x.Struct.Name}.{x.Field}").Append(structType.Name));
                throw new InvalidOperationException(string.Format(SDSLErrorMessages.SDSL0128, structType.Name, chain));
            }

            var fields = layout != null
                ? layout.Fields.Select(x => (x.Name, x.Type, (string?)x.Declarer))
                : structType.Members.Select(x => (x.Name, x.Type, plainDeclarers.GetValueOrDefault(structType.Name)));
            foreach (var (name, fieldType, declarer) in fields)
            {
                path.Add((structType, name, declarer));
                Declare(fieldType);
                path.RemoveAt(path.Count - 1);
            }

            if (layout == null)
            {
                globalContext.PartialFieldStructs.Add(structType, context.DeclareStructuredType(structType, context.Bound++));
                return;
            }
            // A partial owner: the views of the classes find it in Types (equal by name and owner)
            var joined = new StructType(layout.Name, layout.Fields.Select(x => new StructuredTypeMember(x.Name, x.Type, x.Modifier)).ToList())
            {
                PartialOwner = layout.Owner,
                PartialDeclarers = layout.Fields.Select(x => x.Declarer).ToList(),
            };
            layout.Id = context.DeclareStructuredType(joined, context.Bound++);
            // The member indexes access chains are rewritten to: declared now, so the classes' own int type and constants
            // are merged with them (types added during a class merge would not be)
            for (var index = 0; index < layout.Fields.Count; index++)
                context.CompileConstant(index);
        }

        void Collect(ShaderMixinInstantiation node)
        {
            if (!visited.Add(node))
                return;
            foreach (var shaderClass in node.Mixins)
            {
                if (shaderClass.Buffer is not { } buffers)
                    continue;
                foreach (var i in buffers.Context)
                {
                    if (i.Op == Op.OpTypeStruct && buffers.Context.ReverseTypes.TryGetValue(i.Data.IdResult!.Value, out var declared) && declared is StructType { PartialOwner: null } plain)
                        plainDeclarers.TryAdd(plain.Name, shaderClass.ClassName);
                }
                foreach (var type in buffers.Context.ReverseTypes.Values)
                {
                    if (type is not StructType { PartialOwner: { } owner } view)
                        continue;
                    if (!layouts.TryGetValue((owner, view.Name), out var layout))
                        layouts.Add((owner, view.Name), layout = new(view.Name, owner));
                    for (var index = 0; index < view.Members.Count; index++)
                    {
                        var member = view.Members[index];
                        var declarer = view.PartialDeclarers![index];
                        var existing = layout.Fields.FindIndex(x => x.Name == member.Name);
                        if (existing == -1)
                            layout.Fields.Add((member.Name, member.Type, member.TypeModifier, declarer));
                        // Two parts that never meet in one class: found when they meet in the effect
                        else if (layout.Fields[existing].Declarer != declarer || !layout.Fields[existing].Type.Equals(member.Type))
                            throw new InvalidOperationException(string.Format(SDSLErrorMessages.SDSL0120, view.Name, member.Name, layout.Fields[existing].Declarer, declarer));
                    }
                }
            }
            foreach (var compositions in node.Compositions.Values)
            {
                foreach (var composition in compositions)
                    Collect(composition);
            }
        }
    }

    /// <summary>
    /// Replaces the views of partial structs of one class with the structs of the effect: the view types are mapped to the
    /// joined struct, and every member index into a view (access chains, composite extract / insert) becomes the index of
    /// the same field in the joined struct.
    /// </summary>
    private sealed class PartialStructRewriter
    {
        private readonly SpirvContext classContext;
        private readonly Dictionary<(string Owner, string Name), PartialStructLayout> layouts;
        private readonly Dictionary<int, int> resultTypes = new();

        /// <summary>View type id (class ids) → joined struct.</summary>
        public Dictionary<int, PartialStructLayout> Views { get; } = new();

        /// <summary>Instruction index in the class buffer → (word, new value) to write once the instruction is copied.
        /// A new value is a global constant id (access chain) or a literal (composite extract / insert).</summary>
        public Dictionary<int, List<(int Word, int Value)>> Patches { get; } = new();

        private PartialStructRewriter(ShaderBuffers buffers, Dictionary<(string Owner, string Name), PartialStructLayout> layouts)
        {
            classContext = buffers.Context;
            this.layouts = layouts;
        }

        public static PartialStructRewriter? Create(MixinGlobalContext globalContext, SpirvContext context, ShaderBuffers buffers)
        {
            if (globalContext.PartialStructs.Count == 0)
                return null;
            var rewriter = new PartialStructRewriter(buffers, globalContext.PartialStructs);
            foreach (var (id, type) in buffers.Context.ReverseTypes)
            {
                if (type is StructType { PartialOwner: { } owner } view && rewriter.layouts.TryGetValue((owner, view.Name), out var layout))
                    rewriter.Views.Add(id, layout);
            }
            if (rewriter.Views.Count == 0)
                return null;
            rewriter.Analyze(context, buffers);
            return rewriter;
        }

        private void Analyze(SpirvContext context, ShaderBuffers buffers)
        {
            foreach (var i in buffers.Context)
            {
                if (i.Data.IdResult is int id && i.Data.IdResultType is int typeId)
                    resultTypes[id] = typeId;
            }
            var buffer = buffers.Buffer;
            for (var index = 0; index < buffer.Count; index++)
            {
                var i = buffer[index];
                if (i.Data.IdResult is int id && i.Data.IdResultType is int typeId)
                    resultTypes[id] = typeId;
            }

            for (var index = 0; index < buffer.Count; index++)
            {
                var i = buffer[index];
                var words = i.Data.Memory.Span;
                switch (i.Op)
                {
                    // [header, result type, result, base, indexes (constant ids for struct steps)...]
                    case Op.OpAccessChain or Op.OpInBoundsAccessChain:
                        Walk(index, TypeOf(words[3]) is PointerType pointer ? pointer.BaseType : null, words, 4, constantIndexes: true, context);
                        break;
                    // [header, result type, result, composite, literal indexes...]
                    case Op.OpCompositeExtract:
                        Walk(index, TypeOf(words[3]), words, 4, constantIndexes: false, context);
                        break;
                    // [header, result type, result, object, composite, literal indexes...]
                    case Op.OpCompositeInsert:
                        Walk(index, TypeOf(words[4]), words, 5, constantIndexes: false, context);
                        break;
                    case Op.OpCompositeConstruct or Op.OpConstantComposite when IsView(words[1]):
                        throw new NotSupportedException($"A partial struct value built from its fields is not supported ({classContext.ReverseTypes[words[1]]})");
                }
            }
        }

        private SymbolType? TypeOf(int id)
            => resultTypes.TryGetValue(id, out var typeId) && classContext.ReverseTypes.TryGetValue(typeId, out var type) ? type : null;

        private bool IsView(int typeId) => Views.ContainsKey(typeId);

        private void Walk(int instructionIndex, SymbolType? type, Span<int> words, int firstIndexWord, bool constantIndexes, SpirvContext context)
        {
            for (var word = firstIndexWord; word < words.Length && type != null; word++)
            {
                switch (type)
                {
                    case StructType { PartialOwner: not null } partial:
                        {
                            // Indexes into a partial struct are those of this class's view
                            var view = classContext.Types.TryGetValue(partial, out var viewId) ? (StructType)classContext.ReverseTypes[viewId] : partial;
                            var layout = layouts[(view.PartialOwner!, view.Name)];
                            var localIndex = constantIndexes ? Convert.ToInt32(classContext.GetConstantValue(words[word])) : words[word];
                            var member = view.Members[localIndex];
                            var finalIndex = layout.IndexByName[member.Name];
                            if (finalIndex != localIndex)
                            {
                                if (!Patches.TryGetValue(instructionIndex, out var patches))
                                    Patches.Add(instructionIndex, patches = []);
                                patches.Add((word, constantIndexes ? context.CompileConstant(finalIndex).Id : finalIndex));
                            }
                            type = member.Type;
                            break;
                        }
                    case StructuredType structType:
                        {
                            var index = constantIndexes ? Convert.ToInt32(classContext.GetConstantValue(words[word])) : words[word];
                            // An imported struct whose members are not known here
                            if (index >= structType.Members.Count)
                                return;
                            type = structType.Members[index].Type;
                            break;
                        }
                    case ArrayType array:
                        type = array.BaseType;
                        break;
                    // Nothing below can be a partial struct
                    default:
                        return;
                }
            }
        }

        public void Apply(int instructionIndex, ref OpData data)
        {
            if (!Patches.TryGetValue(instructionIndex, out var patches))
                return;
            var words = data.Memory.Span;
            foreach (var (word, value) in patches)
                words[word] = value;
        }
    }

    private static void ExplainPartialStructs(StringBuilder builder, MixinGlobalContext globalContext)
    {
        var layouts = globalContext.PartialStructs;
        foreach (var layout in layouts.Values.OrderBy(x => x.Name, StringComparer.Ordinal))
        {
            builder.AppendLine($"partial struct {layout.Name} (owner {layout.Owner}): {layout.Fields.Count} fields, {SizeInBytes(layout)} bytes");
            foreach (var field in layout.Fields)
                builder.AppendLine($"  field {layout.Name}.{field.Name} : {field.Type} ({field.Declarer})");
        }

        int SizeInBytes(PartialStructLayout layout) => layout.Fields.Sum(x => FieldSizeInBytes(x.Type));

        // Packed size (no padding): what the fields cost when the struct is stored or passed on
        int FieldSizeInBytes(SymbolType type) => type switch
        {
            ScalarType { Type: Scalar.Half } => 2,
            ScalarType { Type: Scalar.Double or Scalar.Int64 or Scalar.UInt64 } => 8,
            ScalarType => 4,
            VectorType vector => vector.Size * FieldSizeInBytes(vector.BaseType),
            MatrixType matrix => matrix.Rows * matrix.Columns * FieldSizeInBytes(matrix.BaseType),
            ArrayType { Size: > 0 } array => array.Size * FieldSizeInBytes(array.BaseType),
            // A partial struct field has the fields of the effect, not those of the view its class sees
            StructType { PartialOwner: { } owner } partial => SizeInBytes(layouts[(owner, partial.Name)]),
            StructType plain => plain.Members.Sum(x => FieldSizeInBytes(x.Type)),
            _ => 0,
        };
    }
}
