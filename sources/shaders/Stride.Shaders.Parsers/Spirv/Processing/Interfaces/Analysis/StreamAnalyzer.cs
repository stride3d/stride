using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Processing.Interfaces.Models;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Processing.Interfaces.Analysis;

internal static class StreamAnalyzer
{
    public static AnalysisResult Analyze(SpirvBuffer buffer, SpirvContext context)
    {
        var streams = new Dictionary<int, StreamVariableInfo>();

        HashSet<int> blockTypes = [];
        Dictionary<int, int> blockPointerTypes = [];
        Dictionary<int, CBufferInfo> cbuffers = [];
        Dictionary<int, ResourceInfo> resources = [];
        Dictionary<int, VariableInfo> variables = [];

        // Build name table
        Dictionary<int, string> nameTable = [];
        Dictionary<int, string> semanticTable = [];
        HashSet<int> patchVariables = [];
        foreach (var i in context)
        {
            // Names
            {
                if (i.Op == Op.OpName
                    && ((OpName)i) is
                    {
                        Target: int t,
                        Name: string n
                    }
                    )
                {
                    nameTable[t] = new(n);
                }
                else if (i.Op == Op.OpMemberName
                    && ((OpMemberName)i) is
                    {
                        Type: int t2,
                        Member: int m,
                        Name: string n2
                    }
                    )
                {
                    nameTable[t2] = new(n2);
                }
            }

            // Semantic
            {
                if (i.Op == Op.OpDecorateString
                    && ((OpDecorateString)i) is
                    {
                        Target: int t,
                        Decoration: Decoration.UserSemantic,
                        Value: string m
                    }
                    )
                {
                    semanticTable[t] = m;
                }
            }

            // Patch
            if (i.Op == Op.OpDecorate && (OpDecorate)i is { Target: int t3, Decoration: Decoration.Patch })
            {
                patchVariables.Add(t3);
            }
        }

        // Analyze streams
        foreach (var i in buffer)
        {
            if (i.Op == Op.OpVariableSDSL
                && ((OpVariableSDSL)i) is { StorageClass: StorageClass.Uniform, ResultType: var pointerType2, ResultId: var bufferId }
                && context.ReverseTypes[pointerType2] is PointerType { BaseType: ConstantBufferSymbol })
            {
                var name = nameTable[bufferId];
                // Note: cbuffer names might be suffixed with .0 .1 (as in Shader.RenameCBufferVariables)
                // Adjust for it
                cbuffers.Add(bufferId, new(name));
            }

            if (i.Op == Op.OpVariableSDSL && ((OpVariableSDSL)i) is
                {
                    StorageClass: StorageClass.Private or StorageClass.Workgroup or StorageClass.Uniform,
                    ResultId: int
                } variable
                && context.ReverseTypes[variable.ResultType] is PointerType { BaseType: not ConstantBufferSymbol })
            {
                var name = nameTable.TryGetValue(variable.ResultId, out var nameId)
                    ? nameId
                    : $"unnamed_{variable.ResultId}";
                var type = (PointerType)context.ReverseTypes[variable.ResultType];

                if (variable.Flags.HasFlag(VariableFlagsMask.Stream))
                {
                    semanticTable.TryGetValue(variable.ResultId, out var semantic);

                    if (variable.MethodOrConstantInitializer != null)
                        throw new NotImplementedException("Variable initializer is not supported on streams variable");

                    streams.Add(variable.ResultId, new StreamVariableInfo(semantic, name, type, variable.ResultId) { Patch = patchVariables.Contains(variable.ResultId) });
                }
                else
                {
                    variables.Add(variable.ResultId, new VariableInfo(name, type, variable.ResultId)
                    {
                        VariableMethodInitializerId = variable.MethodOrConstantInitializer,
                    });
                }
            }

            if (i.Op == Op.OpVariableSDSL && ((OpVariableSDSL)i) is
                {
                    StorageClass: StorageClass.UniformConstant or StorageClass.StorageBuffer,
                    ResultId: int
                } resource)
            {
                var name = nameTable.TryGetValue(resource.ResultId, out var nameId)
                    ? nameId
                    : $"unnamed_{resource.ResultId}";
                var type = context.ReverseTypes[resource.ResultType];

                resources.Add(resource.ResultId, new ResourceInfo(name));
            }
        }

        // Collect decoration strings and group IDs by target
        Dictionary<int, string> resourceGroupNamesByTarget = new();
        Dictionary<int, string> logicalGroupNamesByTarget = new();
        Dictionary<int, int> groupIdByTarget = new();
        foreach (var i in context)
        {
            if (i.Op == Op.OpDecorateString && (OpDecorateString)i is { Decoration: Decoration.ResourceGroupSDSL, Value: string groupName } resourceGroupDecorate)
                resourceGroupNamesByTarget.TryAdd(resourceGroupDecorate.Target, groupName);
            else if (i.Op == Op.OpDecorateString && (OpDecorateString)i is { Decoration: Decoration.LogicalGroupSDSL, Value: string logicalName } logicalGroupDecorate)
                logicalGroupNamesByTarget.TryAdd(logicalGroupDecorate.Target, logicalName);
            else if (i.Op == Op.OpDecorate && (OpDecorate)i is { Decoration: Decoration.ResourceGroupIdSDSL, DecorationParameters: { } m } resourceGroupIdDecorate)
                groupIdByTarget.TryAdd(resourceGroupIdDecorate.Target, m.To<DecorationParams.ResourceGroupIdSDSL>().ResourceGroup);
        }

        // Resolve group ID → name (first target with both a group ID and a ResourceGroupSDSL name wins)
        Dictionary<int, string> groupNames = new();
        foreach (var (target, groupId) in groupIdByTarget)
        {
            if (!groupNames.ContainsKey(groupId) && resourceGroupNamesByTarget.TryGetValue(target, out var name))
                groupNames.Add(groupId, name);
        }

        // Build ResourceGroups and assign resources/cbuffers
        Dictionary<int, ResourceGroup> resourceGroups = new();
        foreach (var (target, groupId) in groupIdByTarget)
        {
            if (!resourceGroups.TryGetValue(groupId, out var resourceGroup))
            {
                if (!groupNames.TryGetValue(groupId, out var name))
                    throw new InvalidOperationException($"ResourceGroup {groupId} has no ResourceGroupSDSL name decoration");
                resourceGroups.Add(groupId, resourceGroup = new(name));
            }

            if (resources.TryGetValue(target, out var resourceInfo))
            {
                resourceGroup.Resources.Add(resourceInfo);
                resourceInfo.ResourceGroup = resourceGroup;
            }
            else if (cbuffers.TryGetValue(target, out var cbufferInfo))
            {
                cbufferInfo.ResourceGroup = resourceGroup;
            }

            // Apply LogicalGroup if present
            if (logicalGroupNamesByTarget.TryGetValue(target, out var logicalGroup))
            {
                resourceGroup.LogicalGroup ??= logicalGroup;
                if (cbuffers.TryGetValue(target, out var ci))
                    ci.LogicalGroup ??= logicalGroup;
            }
        }

        return new(nameTable, streams, variables, cbuffers, resourceGroups, resources);
    }
}
