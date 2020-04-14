// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Reflection;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Reflection
{
    /// <summary>
    /// Describes a descriptor for a primitive (bool, char, sbyte, byte, int, uint, long, ulong, float, double, decimal, string, DateTime).
    /// </summary>
    public class PrimitiveDescriptor : ObjectDescriptor
    {
        private static readonly List<IMemberDescriptor> EmptyMembers = new List<IMemberDescriptor>();
        private readonly Dictionary<string, object> enumRemap;

        public PrimitiveDescriptor(ITypeDescriptorFactory factory, Type type, bool emitDefaultValues, IMemberNamingConvention namingConvention)
            : base(factory, type, emitDefaultValues, namingConvention)
        {
            if (!IsPrimitive(type))
                throw new ArgumentException("Type [{0}] is not a primitive");

            // Handle remap for enum items
            if (type.IsEnum)
            {
                foreach (var member in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    var attributes = AttributeRegistry.GetAttributes(member);
                    foreach (var attribute in attributes)
                    {
                        var aliasAttribute = attribute as DataAliasAttribute;
                        if (aliasAttribute != null)
                        {
                            if (enumRemap == null)
                            {
                                enumRemap = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                            }
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
        public object ParseEnum(string enumAsText, out bool remapped)
        {
            object value;
            remapped = false;
            if (enumRemap != null && enumRemap.TryGetValue(enumAsText, out value))
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
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Object:
                case TypeCode.Empty:
                    return type == typeof(object) || type == typeof(string) || type == typeof(TimeSpan) || type == typeof(DateTime);
            }
            return true;
        }

        protected override List<IMemberDescriptor> PrepareMembers()
        {
            return EmptyMembers;
        }
    }
}
