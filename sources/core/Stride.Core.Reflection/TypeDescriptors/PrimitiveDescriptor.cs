// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Reflection;

/// <summary>
/// Describes a descriptor for a primitive (bool, char, sbyte, byte, int, uint, long, ulong, float, double, decimal, string, DateTime).
/// </summary>
public class PrimitiveDescriptor : ObjectDescriptor
{
    private static readonly List<IMemberDescriptor> EmptyMembers = [];
    private readonly Dictionary<string, object?> enumRemap;

    public PrimitiveDescriptor(ITypeDescriptorFactory factory, Type type, bool emitDefaultValues, IMemberNamingConvention namingConvention)
        : base(factory, type, emitDefaultValues, namingConvention)
    {
        if (!IsPrimitive(type))
            throw new ArgumentException("Type [{0}] is not a primitive");
        enumRemap = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // Handle remap for enum items
        if (type.IsEnum)
        {
            foreach (var member in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                foreach (var attribute in AttributeRegistry.GetAttributes(member))
                {
                    if (attribute is DataAliasAttribute aliasAttribute)
                    {
                        enumRemap[aliasAttribute.Name] = member.GetValue(null);
                    }
                }
            }
        }
    }

    public override DescriptorCategory Category => DescriptorCategory.Primitive;

    /// <summary>
    /// Parses the enum and trying to use remap if any declared.
    /// </summary>
    /// <param name="enumAsText">The enum as text.</param>
    /// <param name="remapped">if set to <c>true</c> the enum was remapped.</param>
    /// <returns>System.Object.</returns>
    public object? ParseEnum(string enumAsText, out bool remapped)
    {
        remapped = false;
        if (enumRemap.TryGetValue(enumAsText, out var value))
        {
            remapped = true;
            return value;
        }

        return Enum.Parse(Type, enumAsText, true);
    }

    /// <summary>
    /// Determines whether the specified type is a primitive.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns><c>true</c> if the specified type is primitive; otherwise, <c>false</c>.</returns>
    public static bool IsPrimitive(Type type)
    {
        return Type.GetTypeCode(type) switch
        {
            TypeCode.Object or TypeCode.Empty => type == typeof(object) || type == typeof(string) || type == typeof(TimeSpan) || type == typeof(DateTime),
            _ => true,
        };
    }

    protected override List<IMemberDescriptor> PrepareMembers()
    {
        return EmptyMembers;
    }
}
