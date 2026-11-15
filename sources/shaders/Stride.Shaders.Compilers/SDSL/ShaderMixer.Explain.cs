// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Text;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL.AST;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Compilers.SDSL;

public partial class ShaderMixer
{
    /// <summary>
    /// When set, <see cref="MergeSDSL"/> fills <see cref="Explanation"/>.
    /// </summary>
    public bool CollectExplanation { get; set; }

    /// <summary>
    /// How the last <see cref="MergeSDSL"/> linked the effect, as diff-friendly text (one fact per line): for each mixin node
    /// (the root, then each composition), the final mixin order, the members each shader placed in that node, and for each
    /// method the override chain from the first definition to the winner (the one called; <c>base</c> goes one step left).
    /// Members of a <c>stage</c> shader used in a composition are placed in the root node; that shader then appears there as
    /// "stage only".
    /// </summary>
    public string? Explanation { get; private set; }

    private static string Explain(MixinNode root)
    {
        var nodePaths = new Dictionary<ShaderInfo, string>();
        var nodes = new List<MixinNode>();
        CollectNodes(root);

        var builder = new StringBuilder();
        foreach (var node in nodes)
        {
            var nodePath = NodePath(node);
            builder.AppendLine($"node {nodePath}");

            foreach (var shader in node.Shaders)
                builder.AppendLine($"  mixin {shader.ShaderName}{(shader.ImportStageOnly ? " (stage only)" : "")}");

            foreach (var shader in node.Shaders)
            {
                foreach (var (name, (id, type)) in shader.Variables)
                {
                    if (type is PointerType { BaseType: ShaderSymbol or ArrayType { BaseType: ShaderSymbol } } pointer)
                    {
                        var composed = node.Compositions.TryGetValue(id, out var composition)
                            ? [composition]
                            : node.CompositionArrays.TryGetValue(id, out var compositionArray) ? compositionArray : [];
                        builder.AppendLine($"  compose {shader.ShaderName}.{name} : {pointer.BaseType} = [{string.Join(", ", composed.Select(NodePath))}]");
                    }
                    else
                    {
                        builder.AppendLine($"  {(shader.StageVariables.Contains(name) ? "stage " : "")}var {shader.ShaderName}.{name} : {StripPointer(type)}");
                    }
                }
            }

            // One group per method, whatever the number of overrides; sorted so that unrelated changes don't move lines
            var methodGroups = node.MethodGroups.Values.Distinct()
                .Select(group => (Signature: Signature(group.Name, group.FunctionType), Group: group))
                .OrderBy(x => x.Signature, StringComparer.Ordinal)
                .ThenBy(x => x.Group.Shader.ShaderName, StringComparer.Ordinal);
            foreach (var (signature, group) in methodGroups)
            {
                var isStage = group.Methods.Any(x => (x.Flags & FunctionFlagsMask.Stage) != 0);
                var chain = group.Methods.Select(method =>
                {
                    var owner = method.Shader.ShaderName;
                    // A stage method of a composition shader is grouped in the root node: say where it comes from
                    if (nodePaths.TryGetValue(method.Shader, out var ownerPath) && ownerPath != nodePath)
                        owner += $"@{ownerPath}";
                    if ((method.Flags & FunctionFlagsMask.Abstract) != 0)
                        owner += " (abstract)";
                    return owner;
                });
                builder.AppendLine($"  {(isStage ? "stage " : "")}method {signature} = {string.Join(" > ", chain)}");
            }
        }
        return builder.ToString();

        void CollectNodes(MixinNode node)
        {
            nodes.Add(node);
            foreach (var shader in node.Shaders)
                nodePaths[shader] = NodePath(node);
            foreach (var composition in node.Compositions.Values)
                CollectNodes(composition);
            foreach (var compositionArray in node.CompositionArrays.Values)
                foreach (var composition in compositionArray)
                    CollectNodes(composition);
        }

        static string NodePath(MixinNode node) => node.CompositionPath ?? "<root>";

        static SymbolType StripPointer(SymbolType type) => type is PointerType pointer ? pointer.BaseType : type;

        // e.g. "Compute(inout float4) -> float4"
        static string Signature(string name, FunctionType type)
        {
            var parameters = type.ParameterTypes.Select(x => x.Modifiers == ParameterModifiers.None
                ? $"{StripPointer(x.Type)}"
                : $"{x.Modifiers.ToString().ToLowerInvariant().Replace(",", "")} {StripPointer(x.Type)}");
            return $"{name}({string.Join(", ", parameters)}) -> {type.ReturnType}";
        }
    }
}
