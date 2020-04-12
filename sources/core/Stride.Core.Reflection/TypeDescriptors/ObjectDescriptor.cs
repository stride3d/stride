// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Stride.Core.Annotations;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Reflection
{
    /// <summary>
    /// Default implementation of a <see cref="ITypeDescriptor"/>.
    /// </summary>
    public class ObjectDescriptor : ITypeDescriptor
    {
        protected static readonly string SystemCollectionsNamespace = typeof(int).Namespace;
        public static readonly ShouldSerializePredicate ShouldSerializeDefault = (obj, parentTypeMemberDesc) => true;
        private static readonly List<IMemberDescriptor> EmptyMembers = new List<IMemberDescriptor>();

        private readonly ITypeDescriptorFactory factory;
        private IMemberDescriptor[] members;
        private Dictionary<string, IMemberDescriptor> mapMembers;
        private HashSet<string> remapMembers;
        private static readonly object[] EmptyObjectArray = new object[0];
        private readonly bool emitDefaultValues;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectDescriptor" /> class.
        /// </summary>
        public ObjectDescriptor(ITypeDescriptorFactory factory, [NotNull] Type type, bool emitDefaultValues, IMemberNamingConvention namingConvention)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (namingConvention == null) throw new ArgumentNullException(nameof(namingConvention));

            this.factory = factory;
            Type = type;
            IsCompilerGenerated = AttributeRegistry.GetAttribute<CompilerGeneratedAttribute>(type) != null;
            this.emitDefaultValues = emitDefaultValues;
            NamingConvention = namingConvention;

            Attributes = AttributeRegistry.GetAttributes(type);

            Style = DataStyle.Any;
            foreach (var attribute in Attributes)
            {
                var styleAttribute = attribute as DataStyleAttribute;
                if (styleAttribute != null)
                {
                    Style = styleAttribute.Style;
                }
            }

            // Get DefaultMemberMode from DataContract
            DefaultMemberMode = DataMemberMode.Default;
            var currentType = type;
            while (currentType != null)
            {
                var dataContractAttribute = AttributeRegistry.GetAttribute<DataContractAttribute>(currentType);
                if (dataContractAttribute != null && (dataContractAttribute.Inherited || currentType == type))
                {
                    DefaultMemberMode = dataContractAttribute.DefaultMemberMode;
                    break;
                }
                currentType = currentType.BaseType;
            }
        }

        protected IAttributeRegistry AttributeRegistry => factory.AttributeRegistry;

        [NotNull]
        public Type Type { get; }

        public IEnumerable<IMemberDescriptor> Members => members;

        public int Count => members?.Length ?? 0;

        public bool HasMembers => members?.Length > 0;

        public virtual DescriptorCategory Category => DescriptorCategory.Object;

        /// <summary>
        /// Gets the naming convention.
        /// </summary>
        /// <value>The naming convention.</value>
        public IMemberNamingConvention NamingConvention { get; }

        /// <summary>
        /// Gets attributes attached to this type.
        /// </summary>
        public List<Attribute> Attributes { get; }

        public DataStyle Style { get; }

        public DataMemberMode DefaultMemberMode { get; }

        public bool IsCompilerGenerated { get; }

        public bool IsMemberRemapped(string name)
        {
            return remapMembers != null && remapMembers.Contains(name);
        }

        public IMemberDescriptor this[string name]
        {
            get
            {
                if (mapMembers == null)
                    throw new KeyNotFoundException();
                return mapMembers[name];
            }
        }

        public IMemberDescriptor TryGetMember(string name)
        {
            if (mapMembers == null)
                return null;
            IMemberDescriptor member;
            mapMembers.TryGetValue(name, out member);
            return member;
        }

        public override string ToString()
        {
            return Type.ToString();
        }

        public virtual void Initialize(IComparer<object> keyComparer)
        {
            if (members != null)
                return;

            var memberList = PrepareMembers();

            // Sort members by name
            // This is to make sure that properties/fields for an object
            // are always displayed in the same order
            if (keyComparer != null)
            {
                memberList.Sort(keyComparer);
            }

            // Free the member list
            members = memberList.ToArray();

            // If no members found, we don't need to build a dictionary map
            if (members.Length <= 0)
                return;

            mapMembers = new Dictionary<string, IMemberDescriptor>(members.Length);

            foreach (var member in members)
            {
                IMemberDescriptor existingMember;
                if (mapMembers.TryGetValue(member.Name, out existingMember))
                {
                    throw new InvalidOperationException("Failed to get ObjectDescriptor for type [{0}]. The member [{1}] cannot be registered as a member with the same name is already registered [{2}]".ToFormat(Type.FullName, member, existingMember));
                }

                mapMembers.Add(member.Name, member);

                // If there is any alternative names, register them
                if (member.AlternativeNames != null)
                {
                    foreach (var alternateName in member.AlternativeNames)
                    {
                        if (mapMembers.TryGetValue(alternateName, out existingMember))
                        {
                            throw new InvalidOperationException($"Failed to get ObjectDescriptor for type [{Type.FullName}]. The member [{member}] cannot be registered as a member with the same name [{alternateName}] is already registered [{existingMember}]");
                        }
                        if (remapMembers == null)
                        {
                            remapMembers = new HashSet<string>();
                        }

                        mapMembers[alternateName] = member;
                        remapMembers.Add(alternateName);
                    }
                }
            }
        }

        public bool Contains(string memberName)
        {
            return mapMembers != null && mapMembers.ContainsKey(memberName);
        }

        protected virtual List<IMemberDescriptor> PrepareMembers()
        {
            if (Type == typeof(Type))
            {
                return EmptyMembers;
            }

            var bindingFlags = BindingFlags.Instance | BindingFlags.Public;

            var metadataTypeAttributes = Type.GetCustomAttributes<DataContractMetadataTypeAttribute>(inherit: true);
            var metadataClassMemberInfos = metadataTypeAttributes.Any() ? new List<(MemberInfo MemberInfo, Type MemberType)>() : null;
            foreach (var metadataTypeAttr in metadataTypeAttributes)
            {
                var metadataType = metadataTypeAttr.MetadataClassType;
                metadataClassMemberInfos.AddRange(from propertyInfo in metadataType.GetProperties(bindingFlags)
                                                  where propertyInfo.CanRead && propertyInfo.GetIndexParameters().Length == 0 && IsMemberToVisit(propertyInfo)
                                                  select (propertyInfo as MemberInfo, propertyInfo.PropertyType));

                // Add all public fields
                metadataClassMemberInfos.AddRange(from fieldInfo in metadataType.GetFields(bindingFlags)
                                                  where fieldInfo.IsPublic && IsMemberToVisit(fieldInfo)
                                                  select (fieldInfo as MemberInfo, fieldInfo.FieldType));
            }

            // TODO: we might want an option to disable non-public.
            if (Category == DescriptorCategory.Object || Category == DescriptorCategory.NotSupportedObject)
                bindingFlags |= BindingFlags.NonPublic;

            var memberList = (from propertyInfo in Type.GetProperties(bindingFlags)
                              where propertyInfo.CanRead && propertyInfo.GetIndexParameters().Length == 0 && IsMemberToVisit(propertyInfo)
                              select new PropertyDescriptor(factory.Find(propertyInfo.PropertyType), propertyInfo, NamingConvention.Comparer)
                              into member
                              where PrepareMember(member, metadataClassMemberInfos?.FirstOrDefault(x => x.MemberInfo.Name == member.OriginalName && x.MemberType == member.Type).MemberInfo)
                              select member as IMemberDescriptor).ToList();

            // Add all public fields
            memberList.AddRange(from fieldInfo in Type.GetFields(bindingFlags)
                                where fieldInfo.IsPublic && IsMemberToVisit(fieldInfo)
                                select new FieldDescriptor(factory.Find(fieldInfo.FieldType), fieldInfo, NamingConvention.Comparer)
                                into member
                                where PrepareMember(member, metadataClassMemberInfos?.FirstOrDefault(x => x.MemberInfo.Name == member.OriginalName && x.MemberType == member.Type).MemberInfo)
                                select member);

            // Allows adding dynamic members per type
            (AttributeRegistry as AttributeRegistry)?.PrepareMembersCallback?.Invoke(this, memberList);

            return memberList;
        }

        protected virtual bool PrepareMember(MemberDescriptorBase member, MemberInfo metadataClassMemberInfo)
        {
            var memberType = member.Type;

            // Start with DataContractAttribute.DefaultMemberMode (if set)
            member.Mode = DefaultMemberMode;
            member.Mask = 1;

            var attributes = AttributeRegistry.GetAttributes(member.MemberInfo);
            if (metadataClassMemberInfo != null)
            {
                var metadataAttributes = AttributeRegistry.GetAttributes(metadataClassMemberInfo);
                attributes.InsertRange(0, metadataAttributes);
            }

            // Gets the style
            var styleAttribute = attributes.FirstOrDefault(x => x is DataStyleAttribute) as DataStyleAttribute;
            if (styleAttribute != null)
            {
                member.Style = styleAttribute.Style;
                member.ScalarStyle = styleAttribute.ScalarStyle;
            }

            // Handle member attribute
            var memberAttribute = attributes.FirstOrDefault(x => x is DataMemberAttribute) as DataMemberAttribute;
            if (memberAttribute != null)
            {
                ((IMemberDescriptor)member).Mask = memberAttribute.Mask;
                if (!member.HasSet)
                {
                    if (memberAttribute.Mode == DataMemberMode.Assign ||
                        (memberType.IsValueType && member.Mode == DataMemberMode.Content))
                        throw new ArgumentException($"{memberType.FullName} {member.OriginalName} is not writeable by {memberAttribute.Mode.ToString()}.");
                }

                member.Mode = memberAttribute.Mode;
                member.Order = memberAttribute.Order;
            }

            // If mode is Default, let's resolve to the actual mode depending on getter/setter existence and object type
            if (member.Mode == DataMemberMode.Default)
            {
                // The default mode is Content, which will not use the setter to restore value if the object is a class (but behave like Assign for value types)
                member.Mode = DataMemberMode.Content;
                if (!member.HasSet && (memberType == typeof(string) || !memberType.IsClass) && !memberType.IsInterface && !Type.IsAnonymous())
                {
                    // If there is no setter, and the value is a string or a value type, we won't write the object at all.
                    member.Mode = DataMemberMode.Never;
                }
            }

            // Process all attributes just once instead of getting them one by one
            DefaultValueAttribute defaultValueAttribute = null;
            foreach (var attribute in attributes)
            {
                var valueAttribute = attribute as DefaultValueAttribute;
                if (valueAttribute != null)
                {
                    // If we've already found one, don't overwrite it
                    defaultValueAttribute = defaultValueAttribute ?? valueAttribute;
                    continue;
                }

                var yamlRemap = attribute as DataAliasAttribute;
                if (yamlRemap != null)
                {
                    if (member.AlternativeNames == null)
                    {
                        member.AlternativeNames = new List<string>();
                    }
                    if (!string.IsNullOrWhiteSpace(yamlRemap.Name))
                    {
                        member.AlternativeNames.Add(yamlRemap.Name);
                    }
                }
            }

            // If it's a private member, check it has a YamlMemberAttribute on it
            if (!member.IsPublic)
            {
                if (memberAttribute == null)
                    return false;
            }

            if (member.Mode == DataMemberMode.Binary)
            {
                if (!memberType.IsArray)
                    throw new InvalidOperationException($"{memberType.FullName} {member.OriginalName} of {Type.FullName} is not an array. Can not be serialized as binary.");
                if (!memberType.GetElementType().IsPureValueType())
                    throw new InvalidOperationException($"{memberType.GetElementType()} is not a pure ValueType. {memberType.FullName} {member.OriginalName} of {Type.FullName} can not serialize as binary.");
            }

            // If this member cannot be serialized, remove it from the list
            if (member.Mode == DataMemberMode.Never)
            {
                return false;
            }

            // ShouldSerialize
            //	  YamlSerializeAttribute(Never) => false
            //	  ShouldSerializeSomeProperty => call it
            //	  DefaultValueAttribute(default) => compare to it
            //	  otherwise => true
            var shouldSerialize = Type.GetMethod("ShouldSerialize" + member.OriginalName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (shouldSerialize != null && shouldSerialize.ReturnType == typeof(bool) && member.ShouldSerialize == null)
                member.ShouldSerialize = (obj, parentTypeMemberDesc) => (bool)shouldSerialize.Invoke(obj, EmptyObjectArray);

            if (defaultValueAttribute != null && member.ShouldSerialize == null && !emitDefaultValues)
            {
                member.DefaultValueAttribute = defaultValueAttribute;
                object defaultValue = defaultValueAttribute.Value;
                Type defaultType = defaultValue?.GetType();
                if (defaultType != null && defaultType.IsNumeric() && defaultType != memberType)
                {
                    try
                    {
                        defaultValue = Convert.ChangeType(defaultValue, memberType);
                    }
                    catch (InvalidCastException)
                    {
                    }
                }
                member.ShouldSerialize = (obj, parentTypeMemberDesc) =>
                {
                    if (parentTypeMemberDesc?.HasDefaultValue ?? false)
                    {
                        var parentDefaultValue = parentTypeMemberDesc.DefaultValue;
                        if (parentDefaultValue != null)
                        {
                                // The parent class holding this object type has defined it's own default value for this type
                                var parentDefaultValueMemberValue = member.Get(parentDefaultValue); // This is the real default value for this object
                                return !Equals(parentDefaultValueMemberValue, member.Get(obj));
                        }
                    }
                    return !Equals(defaultValue, member.Get(obj));
                };
            }

            if (member.ShouldSerialize == null)
                member.ShouldSerialize = ShouldSerializeDefault;

            member.Name = !string.IsNullOrEmpty(memberAttribute?.Name) ? memberAttribute.Name : NamingConvention.Convert(member.OriginalName);

            return true;
        }

        protected bool IsMemberToVisit(MemberInfo memberInfo)
        {
            // Remove all SyncRoot from members
            if (memberInfo is PropertyInfo && memberInfo.Name == "SyncRoot" && memberInfo.DeclaringType != null && (memberInfo.DeclaringType.Namespace ?? string.Empty).StartsWith(SystemCollectionsNamespace))
            {
                return false;
            }

            Type memberType = null;
            var fieldInfo = memberInfo as FieldInfo;
            if (fieldInfo != null)
            {
                memberType = fieldInfo.FieldType;
            }
            else
            {
                var propertyInfo = memberInfo as PropertyInfo;
                if (propertyInfo != null)
                {
                    memberType = propertyInfo.PropertyType;
                }
            }

            if (memberType  != null)
            {
                if (typeof(Delegate).IsAssignableFrom(memberType))
                {
                    return false;
                }
            }


            // Member is not displayed if there is a YamlIgnore attribute on it
            if (AttributeRegistry.GetAttribute<DataMemberIgnoreAttribute>(memberInfo) != null)
                return false;

            return true;
        }
    }
}
