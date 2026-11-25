// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Concurrent;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Compilers.SDSL;

public partial class ShaderMixer
{
    /// <summary>
    /// Generic parameters used by the module part of each generic class (null: assume all), per class name and macros.
    /// </summary>
    private readonly ConcurrentDictionary<(string ClassName, string Macros), bool[]?> usedGenericParameters = new();

    /// <summary>
    /// Places in the root one companion module for each module identity used in compositions, in the contribution order:
    /// <list type="bullet">
    /// <item>the root mixins keep their order; a class also mixed at the root has no separate companion module,</item>
    /// <item>the companion module of a class used only in compositions sits just before the root shader that declares the
    /// composition where the class is first met (walk: root shaders in order, their compositions in declaration order, depth
    /// first; in a node, classes in order, a class's own compositions before the class),</item>
    /// <item>but never before one of its parents: after a parent mixed later at the root.</item>
    /// </list>
    /// Modules (static references only) used in compositions leave their node the same way.
    /// </summary>
    private static void CollectModules(ShaderMixinInstantiation root)
    {
        var rootMixins = root.Mixins;

        // Module identity → index of the root entry holding that module part
        var rootIndexByIdentity = new Dictionary<string, int>();
        var rootIndexByName = new Dictionary<string, int>();
        for (int i = 0; i < rootMixins.Count; i++)
        {
            rootIndexByIdentity.TryAdd(rootMixins[i].ModuleIdentity!, i);
            rootIndexByName.TryAdd(rootMixins[i].ToClassNameWithGenerics(), i);
        }

        var companions = new List<(ShaderClassInstantiation Entry, int Anchor)>();
        var companionIndexByIdentity = new Dictionary<string, int>();
        var companionIndexByName = new Dictionary<string, int>();
        var visited = new HashSet<ShaderMixinInstantiation>();

        for (int i = 0; i < rootMixins.Count; i++)
        {
            foreach (var child in CompositionNodes(root, rootMixins[i]))
            {
                if (visited.Add(child))
                    Walk(child, i);
            }
        }

        // Insert the companion modules: stable by anchor, before the root entry at that index
        if (companions.Count > 0)
        {
            var placed = companions.Select((x, order) => (x.Entry, x.Anchor, Order: order)).OrderBy(x => x.Anchor).ThenBy(x => x.Order).ToList();
            var merged = new List<ShaderClassInstantiation>(rootMixins.Count + placed.Count);
            var next = 0;
            for (int i = 0; i <= rootMixins.Count; i++)
            {
                while (next < placed.Count && placed[next].Anchor == i)
                    merged.Add(placed[next++].Entry);
                if (i < rootMixins.Count)
                    merged.Add(rootMixins[i]);
            }
            rootMixins.Clear();
            rootMixins.AddRange(merged);
        }

        // Modules are only in the root node
        foreach (var node in visited)
            node.Mixins.RemoveAll(x => x.IsModule);

        void Walk(ShaderMixinInstantiation node, int anchor)
        {
            foreach (var shaderClass in node.Mixins)
            {
                foreach (var child in CompositionNodes(node, shaderClass))
                {
                    if (visited.Add(child))
                        Walk(child, anchor);
                }

                if (!HasModulePart(shaderClass))
                    continue;

                var name = shaderClass.ToClassNameWithGenerics();
                var identity = shaderClass.ModuleIdentity!;
                if (rootIndexByIdentity.TryGetValue(identity, out var rootIndex))
                {
                    AddAlias(name, rootMixins[rootIndex].ToClassNameWithGenerics());
                    continue;
                }
                if (companionIndexByIdentity.TryGetValue(identity, out var companionIndex))
                {
                    AddAlias(name, companions[companionIndex].Entry.ToClassNameWithGenerics());
                    continue;
                }

                // Never before a parent
                var companionAnchor = anchor;
                foreach (var ancestor in shaderClass.AncestorNames ?? [])
                {
                    // Parents come first in a node, so an alias of this parent is known (another instantiation holds its module part)
                    var holder = root.RootAliases.GetValueOrDefault(ancestor, ancestor);
                    if (rootIndexByName.TryGetValue(holder, out var ancestorRootIndex))
                        companionAnchor = Math.Max(companionAnchor, ancestorRootIndex + 1);
                    else if (companionIndexByName.TryGetValue(holder, out var ancestorCompanionIndex))
                        companionAnchor = Math.Max(companionAnchor, companions[ancestorCompanionIndex].Anchor);
                }

                var entry = new ShaderClassInstantiation(shaderClass.ClassName, shaderClass.GenericArguments)
                {
                    Buffer = shaderClass.Buffer,
                    Symbol = shaderClass.Symbol,
                    IsModule = shaderClass.IsModule,
                    IsCompanionModule = !shaderClass.IsModule,
                    ModuleIdentity = identity,
                    CompositionPath = shaderClass.CompositionPath,
                };
                companionIndexByIdentity.Add(identity, companions.Count);
                companionIndexByName.TryAdd(name, companions.Count);
                companions.Add((entry, companionAnchor));
            }
        }

        void AddAlias(string name, string rootName)
        {
            if (name != rootName)
                root.RootAliases.TryAdd(name, rootName);
        }

        // The nodes of the compositions a class declares, in declaration order (module compositions are root compositions)
        IEnumerable<ShaderMixinInstantiation> CompositionNodes(ShaderMixinInstantiation node, ShaderClassInstantiation shaderClass)
        {
            foreach (var (name, isModule, _) in CompositionVariables(shaderClass.Buffer!.Value))
            {
                if ((isModule ? root : node).Compositions.TryGetValue(name, out var nodes))
                {
                    foreach (var child in nodes)
                        yield return child;
                }
            }
        }
    }

    /// <summary>
    /// The class name with the generic arguments its module part doesn't use replaced by <c>*</c>.
    /// </summary>
    private string ModuleIdentity(IExternalShaderLoader shaderLoader, ShaderClassInstantiation shaderClass, ShaderMacro[] macros)
    {
        var name = shaderClass.ToClassNameWithGenerics();
        var genericsStart = name.IndexOf('<');
        if (genericsStart == -1 || shaderClass.IsModule || !name.EndsWith('>'))
            return name;

        var className = name[..genericsStart];
        var arguments = SplitGenericArguments(name[(genericsStart + 1)..^1]);
        var macrosKey = string.Join(";", macros.Select(x => $"{x.Name}={x.Definition}"));
        var used = usedGenericParameters.GetOrAdd((className, macrosKey), key => UsedGenericParameters(shaderLoader, key.ClassName, macros));
        if (used == null || used.Length != arguments.Count)
            return name;

        return $"{className}<{string.Join(",", arguments.Select((argument, index) => used[index] ? argument : "*"))}>";
    }

    /// <summary>
    /// For each generic parameter of a class, whether its module part uses it: reachable from the <c>module</c> variables and
    /// methods or the struct types (decorations included, e.g. the link name of a <c>LinkType</c> parameter). Semantic and
    /// member name parameters always count. Null when it can't be told (the class is recompiled per instantiation).
    /// </summary>
    private static bool[]? UsedGenericParameters(IExternalShaderLoader shaderLoader, string className, ShaderMacro[] macros)
    {
        var shader = SpirvBuilder.GetOrLoadShader(shaderLoader, className, macros, out _, out _);

        var genericParameters = new List<(int Id, bool AlwaysUsed)>();
        var dependencies = new Dictionary<int, List<int>>();
        var roots = new List<int>();
        var genericReferences = new HashSet<int>();

        void AddDependencies(int id, OpData instruction)
        {
            if (!dependencies.TryGetValue(id, out var list))
                dependencies.Add(id, list = new());
            SpirvBuilder.CollectIds(instruction, list.Add);
        }

        foreach (var i in shader.Context)
        {
            if (i.Op == Op.OpGenericParameterSDSL && (OpGenericParameterSDSL)i is { } genericParameter)
            {
                var kind = shader.Context.ReverseTypes.TryGetValue(genericParameter.ResultType, out var type) && type is GenericParameterType g ? g.Kind : (GenericParameterKindSDSL?)null;
                genericParameters.Add((genericParameter.ResultId, kind is GenericParameterKindSDSL.Semantic or GenericParameterKindSDSL.MemberName or GenericParameterKindSDSL.MemberNameResolved));
            }
            else if (i.Op == Op.OpGenericReferenceSDSL && i.Data.IdResult is int genericReference)
            {
                genericReferences.Add(genericReference);
            }

            if (i.Op == Op.OpTypeStruct)
                roots.Add(i.Data.IdResult!.Value);

            // Decorations count for their target (e.g. a link name given by a generic parameter)
            if (i.Op is Op.OpDecorate or Op.OpDecorateString or Op.OpMemberDecorate or Op.OpMemberDecorateString)
                AddDependencies(i.Data.Memory.Span[1], i.Data);
            else if (i.Op != Op.OpName && i.Op != Op.OpMemberName && i.Data.IdResult is int id)
                AddDependencies(id, i.Data);
        }

        if (genericParameters.Count == 0)
            return null;

        int? currentFunction = null;
        for (var index = 0; index < shader.Buffer.Count; index++)
        {
            var i = shader.Buffer[index];
            if (i.Op == Op.OpUnresolvableShaderSDSL)
                return null;

            if (i.Op == Op.OpFunction && (OpFunction)i is { } function)
            {
                currentFunction = function.ResultId;
                if (index + 1 < shader.Buffer.Count && shader.Buffer[index + 1].Op == Op.OpFunctionMetadataSDSL
                    && ((OpFunctionMetadataSDSL)shader.Buffer[index + 1]).Flags.HasFlag(FunctionFlagsMask.Stage))
                    roots.Add(function.ResultId);
            }

            if (currentFunction is int functionId)
            {
                // A function depends on everything its body uses
                AddDependencies(functionId, i.Data);
                if (i.Op == Op.OpFunctionEnd)
                    currentFunction = null;
            }
            else if (i.Op == Op.OpVariableSDSL && (OpVariableSDSL)i is { } variable)
            {
                AddDependencies(variable.ResultId, i.Data);
                if (variable.StorageClass != StorageClass.Function && (variable.Flags & VariableFlagsMask.Stage) != 0)
                    roots.Add(variable.ResultId);
            }
        }

        var reached = new HashSet<int>();
        var pending = new Stack<int>(roots);
        while (pending.Count > 0)
        {
            var id = pending.Pop();
            if (!reached.Add(id))
                continue;
            if (dependencies.TryGetValue(id, out var list))
            {
                foreach (var dependency in list)
                {
                    if (!reached.Contains(dependency))
                        pending.Push(dependency);
                }
            }
        }

        // A reference to a parent's generic parameter: can't tell which of ours it stands for
        if (reached.Overlaps(genericReferences))
            return null;

        return genericParameters.Select(x => x.AlwaysUsed || reached.Contains(x.Id)).ToArray();
    }

    /// <summary>
    /// Splits generic arguments at the commas that are not inside parentheses or angle brackets.
    /// </summary>
    private static List<string> SplitGenericArguments(string arguments)
    {
        var result = new List<string>();
        var depth = 0;
        var start = 0;
        for (int i = 0; i < arguments.Length; i++)
        {
            switch (arguments[i])
            {
                case '(' or '<' or '[':
                    depth++;
                    break;
                case ')' or '>' or ']':
                    depth--;
                    break;
                case ',' when depth == 0:
                    result.Add(arguments[start..i]);
                    start = i + 1;
                    break;
            }
        }
        result.Add(arguments[start..]);
        return result;
    }
}
