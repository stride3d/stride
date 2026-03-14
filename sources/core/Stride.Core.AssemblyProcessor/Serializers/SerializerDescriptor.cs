// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Mono.Cecil;

namespace Stride.Core.AssemblyProcessor.Serializers;

/// <summary>
/// Describes a serializer to be generated. Collected during the discovery phase
/// and consumed by the code generation phase to create the actual <see cref="TypeDefinition"/>.
/// </summary>
internal class SerializerDescriptor
{
    /// <summary>
    /// The data type being serialized.
    /// </summary>
    public required TypeDefinition DataType { get; init; }

    /// <summary>
    /// The class name for the generated serializer (e.g. "MyTypeSerializer").
    /// </summary>
    public required string SerializerClassName { get; init; }

    /// <summary>
    /// Whether the serializer type should be public (true for generic types).
    /// </summary>
    public required bool IsPublic { get; init; }

    /// <summary>
    /// Whether to use ClassDataSerializer{T} (true) or DataSerializer{T} (false) as the base class.
    /// </summary>
    public required bool UseClassDataSerializer { get; init; }

    /// <summary>
    /// The serializable type info created during collection, which holds the serializer type reference
    /// and metadata used by the registration/factory phase.
    /// </summary>
    public required CecilSerializerContext.SerializableTypeInfo SerializableTypeInfo { get; init; }

    /// <summary>
    /// Base type whose serializer should be called first (for inheritance chains).
    /// Set during <see cref="CecilSerializerContext.CollectSerializerDependencies"/>.
    /// </summary>
    public TypeReference? SerializedParentType { get; set; }

    /// <summary>
    /// The serializable fields/properties of <see cref="DataType"/>.
    /// Computed once during collection and reused during code generation.
    /// </summary>
    public SerializerRegistry.SerializableItem[] SerializableItems { get; set; }
}
