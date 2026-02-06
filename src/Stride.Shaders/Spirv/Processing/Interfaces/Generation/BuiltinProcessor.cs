using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Processing.InterfaceProcessorInternal.Models;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Processing.InterfaceProcessorInternal.Generation;

/// <summary>
/// Handles processing of builtin variables and semantics (SV_*).
/// </summary>
internal static class BuiltinProcessor
{
    /// <summary>
    /// Adds a BuiltIn decoration to a variable.
    /// </summary>
    public static bool AddBuiltin(SpirvContext context, int variable, BuiltIn builtin)
    {
        context.Add(new OpDecorate(variable, Decoration.BuiltIn, [(int)builtin]));
        return true;
    }

    /// <summary>
    /// Adds a Location decoration to a variable.
    /// </summary>
    public static bool AddLocation(SpirvContext context, int variable, string location)
    {
        // If it fails, default is 0
        int.TryParse(location, out var targetIndex);
        context.Add(new OpDecorate(variable, Decoration.Location, [targetIndex]));
        return true;
    }

    /// <summary>
    /// Converts interface variables between types, handling vector and array size mismatches.
    /// Used when builtin types have different sizes than shader types.
    /// </summary>
    public static int ConvertInterfaceVariable(
        NewSpirvBuffer buffer,
        SpirvContext context,
        SymbolType sourceType,
        SymbolType castType,
        int value)
    {
        if (sourceType == castType)
            return value;

        if (sourceType is VectorType v1 && castType is VectorType v2 && v1.BaseType == v2.BaseType)
        {
            Span<int> components = stackalloc int[v2.Size];
            for (int i = 0; i < v2.Size; ++i)
            {
                components[i] = i < v1.Size
                    ? buffer.Add(new OpCompositeExtract(context.GetOrRegister(v1.BaseType), context.Bound++, value, [i])).ResultId
                    : context.CreateDefaultConstantComposite(v1.BaseType).Id;
            }

            return buffer.Add(new OpCompositeConstruct(context.GetOrRegister(v2), context.Bound++, new(components))).ResultId;
        }

        if (sourceType is ArrayType a1 && castType is ArrayType a2 && a1.BaseType == a2.BaseType)
        {
            Span<int> components = stackalloc int[a2.Size];
            for (int i = 0; i < a2.Size; ++i)
            {
                components[i] = i < a1.Size
                    ? buffer.Add(new OpCompositeExtract(context.GetOrRegister(a1.BaseType), context.Bound++, value, [i])).ResultId
                    : context.CreateDefaultConstantComposite(a1.BaseType).Id;
            }

            return buffer.Add(new OpCompositeConstruct(context.GetOrRegister(a2), context.Bound++, new(components))).ResultId;
        }

        throw new InvalidOperationException($"Can't convert interface variable from {sourceType} to {castType}");
    }

    /// <summary>
    /// Processes builtin decorations for system-value semantics (SV_*).
    /// Adjusts types as needed for Vulkan compatibility and adds appropriate decorations.
    /// </summary>
    /// <returns>True if the variable is a builtin and doesn't need forwarding, false if it needs forwarding.</returns>
    public static bool ProcessBuiltinsDecoration(
        SpirvContext context,
        ExecutionModel executionModel,
        int variable,
        StreamVariableType type,
        string? semantic,
        ref SymbolType symbolType)
    {
        semantic = semantic?.ToUpperInvariant();
        symbolType = (executionModel, type, semantic) switch
        {
            // DX might use float[2] or float[3] or float[4] but Vulkan expects float[4] in all cases
            (ExecutionModel.TessellationControl, StreamVariableType.Output, "SV_TESSFACTOR") => new ArrayType(ScalarType.Float, 4),
            // DX might use float or float[2] but Vulkan expects float[2] in all cases
            (ExecutionModel.TessellationControl, StreamVariableType.Output, "SV_INSIDETESSFACTOR") => new ArrayType(ScalarType.Float, 2),
            // DX might use float2 or float3 but Vulkan expects float3 in all cases
            (ExecutionModel.TessellationControl, StreamVariableType.Output, "SV_DOMAINLOCATION") => new VectorType(ScalarType.Float, 3),
            _ => symbolType,
        };

        // Note: false means it needs to be forwarded
        // TODO: review the case where we don't use automatic forwarding for HS/DS/GS stages, i.e. SV_POSITION and SV_PrimitiveID
        return (executionModel, type, semantic) switch
        {
            // SV_Depth/SV_Target
            (ExecutionModel.Fragment, StreamVariableType.Output, "SV_DEPTH") => AddBuiltin(context, variable, BuiltIn.FragDepth),
            (ExecutionModel.Fragment, StreamVariableType.Output, { } semantic2) when semantic2.StartsWith("SV_TARGET") => AddLocation(context, variable, semantic2.Substring("SV_TARGET".Length)),
            // SV_Position
            (not ExecutionModel.Fragment, StreamVariableType.Output, "SV_POSITION") => AddBuiltin(context, variable, BuiltIn.Position),
            (not ExecutionModel.Fragment and not ExecutionModel.Vertex, StreamVariableType.Input, "SV_POSITION") => AddBuiltin(context, variable, BuiltIn.Position),
            (ExecutionModel.Fragment, StreamVariableType.Input, "SV_POSITION") => AddBuiltin(context, variable, BuiltIn.FragCoord),
            // SV_InstanceID/SV_VertexID
            (ExecutionModel.Vertex, StreamVariableType.Input, "SV_INSTANCEID") => AddBuiltin(context, variable, BuiltIn.InstanceIndex),
            (ExecutionModel.Vertex, StreamVariableType.Input, "SV_VERTEXID") => AddBuiltin(context, variable, BuiltIn.VertexIndex),
            (not ExecutionModel.Vertex, StreamVariableType.Input, "SV_INSTANCEID" or "SV_VERTEXID") => false,
            // SV_IsFrontFace
            (ExecutionModel.Fragment, StreamVariableType.Input, "SV_ISFRONTFACE") => AddBuiltin(context, variable, BuiltIn.FrontFacing),
            // SV_PrimitiveID
            (ExecutionModel.Geometry, StreamVariableType.Output, "SV_PRIMITIVEID") => AddBuiltin(context, variable, BuiltIn.PrimitiveId),
            (not ExecutionModel.Vertex, StreamVariableType.Input, "SV_PRIMITIVEID") => AddBuiltin(context, variable, BuiltIn.PrimitiveId),
            // Tessellation
            (ExecutionModel.TessellationControl or ExecutionModel.TessellationEvaluation, _, "SV_TESSFACTOR") => AddBuiltin(context, variable, BuiltIn.TessLevelOuter),
            (ExecutionModel.TessellationControl or ExecutionModel.TessellationEvaluation, _, "SV_INSIDETESSFACTOR") => AddBuiltin(context, variable, BuiltIn.TessLevelInner),
            (ExecutionModel.TessellationEvaluation, StreamVariableType.Input, "SV_DOMAINLOCATION") => AddBuiltin(context, variable, BuiltIn.TessCoord),
            (ExecutionModel.TessellationControl, StreamVariableType.Input, "SV_OUTPUTCONTROLPOINTID") => AddBuiltin(context, variable, BuiltIn.InvocationId),
            // Compute shaders
            (ExecutionModel.GLCompute, StreamVariableType.Input, "SV_GROUPID") => AddBuiltin(context, variable, BuiltIn.WorkgroupId),
            (ExecutionModel.GLCompute, StreamVariableType.Input, "SV_GROUPINDEX") => AddBuiltin(context, variable, BuiltIn.LocalInvocationIndex),
            (ExecutionModel.GLCompute, StreamVariableType.Input, "SV_GROUPTHREADID") => AddBuiltin(context, variable, BuiltIn.LocalInvocationId),
            (ExecutionModel.GLCompute, StreamVariableType.Input, "SV_DISPATCHTHREADID") => AddBuiltin(context, variable, BuiltIn.GlobalInvocationId),
            (_, _, { } semantic2) when semantic2.StartsWith("SV_") => throw new NotImplementedException($"System-value Semantic not implemented: {semantic2} for stage {executionModel} as {type}"),
            _ => false,
        };
    }
}
