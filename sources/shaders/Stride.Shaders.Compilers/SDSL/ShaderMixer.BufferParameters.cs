// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Compilers.SDSL;

public partial class ShaderMixer
{
    /// <summary>
    /// Gives each call that passes structured or byte address buffers a copy of the called method in which these parameters are
    /// the call's global buffers, and removes them from the copy and from the call.
    /// </summary>
    /// <remarks>
    /// A buffer parameter is a StorageBuffer pointer, which SPIR-V only accepts as a function argument with the variable
    /// pointers capability (optional in Vulkan). DXC inlines such functions; copies keep them as functions, one per method and
    /// set of buffers. A copy's own calls now pass global buffers, so the next round handles nested calls. The originals are
    /// no longer called, and the dead code removal of <see cref="Spirv.Processing.Interfaces.InterfaceProcessor"/> drops them.
    /// </remarks>
    /// <returns>False if a call passes a buffer that is not a global buffer of the parameter's type.</returns>
    private static bool SpecializeBufferParameters(SpirvContext context, SpirvBuffer buffer, MixinNode rootMixin, ILogger log)
    {
        // Indices and pointer types of the buffer parameters of each method that has some
        var bufferParameters = new Dictionary<int, List<int>>();
        var bufferParameterTypes = new Dictionary<int, List<int>>();
        // Global buffer id, its pointer type
        var globalBuffers = new Dictionary<int, int>();
        var currentFunction = 0;
        var parameterIndex = 0;
        foreach (var i in buffer)
        {
            if (i.Op == Op.OpFunction)
            {
                currentFunction = ((OpFunction)i).ResultId;
                parameterIndex = 0;
            }
            else if (i.Op == Op.OpFunctionParameter)
            {
                var parameterType = ((OpFunctionParameter)i).ResultType;
                if (IsBufferPointer(context, parameterType))
                {
                    if (!bufferParameters.TryGetValue(currentFunction, out var indices))
                    {
                        bufferParameters.Add(currentFunction, indices = []);
                        bufferParameterTypes.Add(currentFunction, []);
                    }
                    indices.Add(parameterIndex);
                    bufferParameterTypes[currentFunction].Add(parameterType);
                }
                parameterIndex++;
            }
            else if (i.Op == Op.OpVariableSDSL && (OpVariableSDSL)i is { StorageClass: StorageClass.StorageBuffer } variable)
            {
                globalBuffers.Add(variable.ResultId, variable.ResultType);
            }
        }

        if (bufferParameters.Count == 0)
            return true;

        var copies = new Dictionary<(int Function, string Buffers), int>();
        var success = true;
        bool changed;
        do
        {
            changed = false;
            (int File, int Line, int Column)? currentLine = null;
            for (var index = 0; index < buffer.Count; index++)
            {
                var i = buffer[index];
                if (i.Op == Op.OpFunction)
                {
                    currentFunction = ((OpFunction)i).ResultId;
                    currentLine = null;
                }
                else if (i.Op == Op.OpLine)
                {
                    OpLine line = i;
                    currentLine = (line.File, line.Line, line.Column);
                }
                // A call inside an original passes the original's parameters; the original itself is no longer called
                else if (i.Op == Op.OpFunctionCall && !bufferParameters.ContainsKey(currentFunction)
                    && (OpFunctionCall)i is { } call && bufferParameters.TryGetValue(call.Function, out var indices))
                {
                    var arguments = call.Arguments.Elements.Span.ToArray();
                    var buffers = new int[indices.Count];
                    var known = true;
                    for (var k = 0; k < indices.Count; k++)
                    {
                        buffers[k] = arguments[indices[k]];
                        var parameterType = ((PointerType)context.ReverseTypes[bufferParameterTypes[call.Function][k]]).BaseType;
                        string? error = null;
                        if (!globalBuffers.TryGetValue(buffers[k], out var bufferPointerType))
                            error = "must be a buffer declared in a shader, or a buffer parameter of the calling method: a call can't choose its buffer while the shader runs";
                        // The copy uses the buffer as the parameter's type: HLSL accepts only the same type, read-write included.
                        // Compared by name: a buffer type declared by another shader can have its own id.
                        else if (((PointerType)context.ReverseTypes[bufferPointerType]).BaseType is var bufferType && bufferType.ToId() != parameterType.ToId())
                            error = $"is a {bufferType}, but the parameter is a {parameterType}. Give a buffer of the parameter's type";
                        if (error != null)
                        {
                            var location = currentLine is { } l && context.GetBuffer().TryGetInstructionById(l.File, out var file) && file.Op == Op.OpString
                                ? $"{((OpString)file).Value}({l.Line},{l.Column}): "
                                : null;
                            var name = context.Names.TryGetValue(call.Function, out var functionName) ? functionName : $"%{call.Function}";
                            log.Error($"{location}Argument {indices[k] + 1} of {name} {error}.");
                            known = false;
                        }
                    }
                    if (!known)
                    {
                        success = false;
                        continue;
                    }

                    var key = (call.Function, string.Join(',', buffers));
                    if (!copies.TryGetValue(key, out var copy))
                    {
                        (copy, var insertedAt, var insertedCount) = CopyWithBuffers(context, buffer, call.Function, indices, buffers);
                        copies.Add(key, copy);
                        AdjustIndicesAfterAppendInstructions(rootMixin, insertedAt, insertedCount);
                        if (insertedAt <= index)
                            index += insertedCount;
                    }

                    var remaining = new List<int>(arguments.Length - indices.Count);
                    for (var k = 0; k < arguments.Length; k++)
                    {
                        if (!indices.Contains(k))
                            remaining.Add(arguments[k]);
                    }
                    buffer.Replace(index, new OpFunctionCall(call.ResultType, call.ResultId, copy, new(remaining.ToArray())));
                    changed = true;
                }
            }
        } while (changed && success);

        return success;
    }

    private static bool IsBufferPointer(SpirvContext context, int typeId)
        => context.ReverseTypes.TryGetValue(typeId, out var type)
            && type is PointerType { StorageClass: StorageClass.StorageBuffer, BaseType: StructuredBufferType or ByteAddressBufferType };

    /// <summary>
    /// Inserts after <paramref name="function"/> a copy of it with new ids, where the parameters at <paramref name="indices"/>
    /// are replaced by <paramref name="buffers"/> and removed.
    /// </summary>
    private static (int Copy, int InsertedAt, int InsertedCount) CopyWithBuffers(SpirvContext context, SpirvBuffer buffer, int function, List<int> indices, int[] buffers)
    {
        var (start, end) = SpirvBuilder.FindMethodBounds(buffer, function);

        // All the new ids first: branches and phis refer to blocks further down
        var remapIds = new Dictionary<int, int>();
        var parameterIndex = 0;
        for (var index = start; index < end; index++)
        {
            var i = buffer[index];
            if (i.Op == Op.OpFunctionParameter)
            {
                var bufferIndex = indices.IndexOf(parameterIndex++);
                if (bufferIndex >= 0)
                {
                    remapIds.Add(((OpFunctionParameter)i).ResultId, buffers[bufferIndex]);
                    continue;
                }
            }
            if (i.Data.IdResult is int resultId)
                remapIds.Add(resultId, context.Bound++);
        }

        var copied = new List<OpData>(end - start);
        parameterIndex = 0;
        for (var index = start; index < end; index++)
        {
            var i = buffer[index];
            if (i.Op == Op.OpFunctionParameter && indices.Contains(parameterIndex++))
                continue;

            var copy = new OpData(i.Data.Memory.Span);
            SpirvBuilder.RemapIds(remapIds, ref copy);
            if (copy.Op == Op.OpFunction)
            {
                // A new function type: the original's can be shared by other functions
                var copiedFunction = new OpFunction(ref copy);
                var functionType = (FunctionType)context.ReverseTypes[copiedFunction.FunctionType];
                var remaining = new List<FunctionParameter>(functionType.ParameterTypes.Count - indices.Count);
                for (var k = 0; k < functionType.ParameterTypes.Count; k++)
                {
                    if (!indices.Contains(k))
                        remaining.Add(functionType.ParameterTypes[k]);
                }
                copiedFunction.FunctionType = context.GetOrRegister(functionType with { ParameterTypes = remaining });
            }
            copied.Add(copy);

            if (i.Data.IdResult is int resultId && context.Names.TryGetValue(resultId, out var name))
            {
                // The copy of Reset(target, ...) called with Values is Reset_Values
                if (resultId == function)
                    name += string.Concat(buffers.Select(x => context.Names.TryGetValue(x, out var bufferName) ? $"_{bufferName}" : $"_{x}"));
                context.AddName(remapIds[resultId], name);
            }
        }

        buffer.InsertRange(end, System.Runtime.InteropServices.CollectionsMarshal.AsSpan(copied));
        return (remapIds[function], end, copied.Count);
    }
}
