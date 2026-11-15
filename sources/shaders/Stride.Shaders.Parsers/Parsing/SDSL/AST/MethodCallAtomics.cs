// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv;

namespace Stride.Shaders.Parsing.SDSL.AST;

/// <summary>
/// Checks of the Interlocked* intrinsics, done once the call is resolved: the intrinsic signature only says "a reference to an
/// int or uint", and the call's arguments are converted to it, so the destination itself is checked here, as DXC does.
/// </summary>
public partial class MethodCall
{
    /// <summary>
    /// Reports what DXC rejects (or Stride doesn't support) in an Interlocked* call, and gives the destination its real type.
    /// </summary>
    private void CheckAtomicCall(SymbolTable table, ref IntrinsicTemplateExpander.IntrinsicOverload overload)
    {
        if (FindAtomicError(out var destinationType) is { } error)
        {
            table.AddError(new(Info, error));
            return;
        }

        // The overload's destination is "a reference to int or uint" in no storage: give it the argument's, so the atomic
        // knows its type and its scope (a texel goes through OpImageTexelPointer, a pointer to Image)
        if (destinationType != null)
        {
            var parameterTypes = overload.Type.ParameterTypes.ToList();
            parameterTypes[0] = parameterTypes[0] with { Type = destinationType };
            overload = overload with { Type = new FunctionType(overload.Type.ReturnType, parameterTypes) };
        }
    }

    private string? FindAtomicError(out PointerType? destinationType)
    {
        destinationType = null;
        var name = Name.Name;
        if (!name.StartsWith("Interlocked"))
            return null;

        // RWByteAddressBuffer methods: the destination is a uint of the buffer
        if (MemberCallBaseType is ByteAddressBufferType)
        {
            if (name.EndsWith("64"))
                return string.Format(SDSLErrorMessages.SDSL0188, name);
            if (name.Contains("Float"))
                return string.Format(SDSLErrorMessages.SDSL0187, name);
            return null;
        }
        if (MemberCallBaseType != null || Arguments.Values.Count == 0)
            return null;

        if (name.Contains("FloatBitwise"))
            return string.Format(SDSLErrorMessages.SDSL0187, name);

        var destination = Arguments.Values[0];
        var valueType = destination.Type is PointerType pointerType ? pointerType.BaseType : TryGetTexelType(destination) ?? destination.Type;
        switch (valueType)
        {
            case ScalarType { Type: Scalar.Int or Scalar.UInt }:
                break;
            case ScalarType { Type: Scalar.Int64 or Scalar.UInt64 }:
                return string.Format(SDSLErrorMessages.SDSL0188, name);
            case ScalarType { Type: Scalar.Float } when name == "InterlockedExchange":
                return string.Format(SDSLErrorMessages.SDSL0187, name);
            default:
                return string.Format(SDSLErrorMessages.SDSL0185, name, destination, valueType);
        }

        if (DescribeNonSharedDestination(destination) is { } description)
            return string.Format(SDSLErrorMessages.SDSL0186, name, destination, description);

        destinationType = destination.Type as PointerType ?? new PointerType(valueType, Specification.StorageClass.Image);
        return null;
    }

    /// <summary>
    /// The texel type of <c>buffer[i]</c> or <c>texture[uv]</c> on a read-write typed buffer or texture, which an atomic reaches
    /// through a texel pointer.
    /// </summary>
    private static SymbolType? TryGetTexelType(Expression destination)
    {
        if (destination is not AccessorChainExpression { Accessors: [.., IndexerExpression] } chain)
            return null;
        var baseType = chain.Accessors.Count > 1 ? chain.Accessors[^2].Type : chain.Source.Type;
        return baseType switch
        {
            PointerType { BaseType: BufferType { WriteAllowed: true } buffer } => buffer.BaseType,
            PointerType { BaseType: TextureType { Sampled: 2 } texture } => texture.ReturnType,
            _ => null,
        };
    }

    /// <summary>
    /// Why the destination is not memory that other threads see (null when it is): atomics need a read-write buffer, texture or
    /// groupshared variable.
    /// </summary>
    private static string? DescribeNonSharedDestination(Expression destination)
    {
        if (destination.Type is not PointerType pointerType)
            return TryGetTexelType(destination) != null ? null : "a value, not a variable";

        // A read-only buffer is StorageBuffer memory too
        if (destination is AccessorChainExpression chain
            && chain.Accessors.Prepend(chain.Source).Any(x => x.Type is PointerType { BaseType: StructuredBufferType { WriteAllowed: false } }))
            return "an element of a read-only buffer";

        return pointerType.StorageClass switch
        {
            Specification.StorageClass.StorageBuffer or Specification.StorageClass.Workgroup => null,
            Specification.StorageClass.Function => "a local variable or a parameter",
            Specification.StorageClass.Uniform => "a constant buffer member",
            _ => "a static variable or a stream",
        };
    }
}
