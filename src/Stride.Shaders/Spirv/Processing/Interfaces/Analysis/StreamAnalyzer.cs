using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Processing.Interfaces.Models;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Processing.Interfaces.Analysis;

internal static class StreamAnalyzer
{
    public static AnalysisResult Analyze(NewSpirvBuffer buffer, SpirvContext context)
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
                && ((OpVariableSDSL)i) is { Storageclass: StorageClass.Uniform, ResultType: var pointerType2, ResultId: var bufferId }
                && context.ReverseTypes[pointerType2] is PointerType { BaseType: ConstantBufferSymbol })
            {
                var name = nameTable[bufferId];
                // Note: cbuffer names might be suffixed with .0 .1 (as in Shader.RenameCBufferVariables)
                // Adjust for it
                cbuffers.Add(bufferId, new(name));
            }

            if (i.Op == Op.OpVariableSDSL && ((OpVariableSDSL)i) is
                {
                    Storageclass: StorageClass.Private or StorageClass.Workgroup,
                    ResultId: int
                } variable)
            {
                var name = nameTable.TryGetValue(variable.ResultId, out var nameId)
                    ? nameId
                    : $"unnamed_{variable.ResultId}";
                var type = (PointerType)context.ReverseTypes[variable.ResultType];

                if (variable.Flags.HasFlag(VariableFlagsMask.Stream))
                {
                    semanticTable.TryGetValue(variable.ResultId, out var semantic);

                    if (variable.MethodInitializer != null)
                        throw new NotImplementedException("Variable initializer is not supported on streams variable");

                    streams.Add(variable.ResultId, new StreamVariableInfo(semantic, name, type, variable.ResultId) { Patch = patchVariables.Contains(variable.ResultId) });
                }
                else
                {
                    variables.Add(variable.ResultId, new VariableInfo(name, type, variable.ResultId)
                    {
                        VariableMethodInitializerId = variable.MethodInitializer,
                    });
                }
            }

            if (i.Op == Op.OpVariableSDSL && ((OpVariableSDSL)i) is
                {
                    Storageclass: StorageClass.UniformConstant or StorageClass.StorageBuffer,
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

        // Process ResourceGroupId and build ResourceGroups
        Dictionary<int, ResourceGroup> resourceGroups = new();
        foreach (var i in context)
        {
            if (i.Op == Op.OpDecorate && (OpDecorate)i is { Decoration: Decoration.ResourceGroupIdSDSL, DecorationParameters: { } m } resourceGroupIdDecorate)
            {
                var n = m.To<DecorationParams.ResourceGroupIdSDSL>();

                if (resources.TryGetValue(resourceGroupIdDecorate.Target, out var resourceInfo))
                {
                    if (!resourceGroups.TryGetValue(n.ResourceGroup, out var resourceGroup))
                        resourceGroups.Add(n.ResourceGroup, resourceGroup = new());

                    resourceGroup.Resources.Add(resourceInfo);

                    resourceInfo.ResourceGroup = resourceGroup;

                }
            }
        }

        // Process ResourceGroup and LogicalGroup decorations
        foreach (var i in context)
        {
            if (i.Op == Op.OpDecorateString && (OpDecorateString)i is { Decoration: Decoration.ResourceGroupSDSL, Value: string m2  } resourceGroupDecorate)
            {
                if (resources.TryGetValue(resourceGroupDecorate.Target, out var resourceInfo)
                    // Note: ResourceGroup should not be null if set
                    && resourceInfo.ResourceGroup.Name == null)
                {
                    resourceInfo.ResourceGroup.Name = m2;
                }
            }
            else if (i.Op == Op.OpDecorateString && (OpDecorateString)i is { Decoration: Decoration.LogicalGroupSDSL, Value: string m3 } logicalGroupDecorate)
            {
                if (resources.TryGetValue(logicalGroupDecorate.Target, out var resourceInfo)
                    // Note: ResourceGroup should not be null if this decoration is set
                    && resourceInfo.ResourceGroup.LogicalGroup == null)
                {
                    resourceInfo.ResourceGroup.LogicalGroup = m3;
                }
                else if (cbuffers.TryGetValue(logicalGroupDecorate.Target, out var cbufferInfo))
                {
                    cbufferInfo.LogicalGroup = m3;
                }
            }
        }

        return new(nameTable, streams, variables, cbuffers, resourceGroups, resources);
    }
}
