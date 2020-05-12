// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using System.Runtime.Versioning;
using Stride.Core.AssemblyProcessor.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Mono.Cecil;
using Stride.Core;

namespace Stride.Core.AssemblyProcessor
{
    internal class ComplexSerializerRegistry
    {
        private static HashSet<string> forbiddenKeywords;
        private static HashSet<IMemberDefinition> ignoredMembers;

        static ComplexSerializerRegistry()
        {
            ignoredMembers = new HashSet<IMemberDefinition>();

            forbiddenKeywords = new HashSet<string>(new[]
                { "obj", "stream", "mode",
                    "abstract", "event", "new", "struct",
                    "as", "explicit", "null", "switch",
                    "base", "extern", "object", "this",
                    "bool", "false", "operator", "throw",
                    "break", "finally", "out", "true",
                    "byte", "fixed", "override", "try",
                    "case", "float", "params", "typeof",
                    "catch", "for", "private", "uint",
                    "char", "foreach", "protected", "ulong",
                    "checked", "goto", "public", "unchecked",
                    "class", "if", "readonly", "unsafe",
                    "const", "implicit", "ref", "ushort",
                    "continue", "in", "return", "using",
                    "decimal", "int", "sbyte", "virtual",
                    "default", "interface", "sealed", "volatile",
                    "delegate", "internal", "short", "void",
                    "do", "is", "sizeof", "while",
                    "double", "lock", "stackalloc",
                    "else", "long", "static",
                    "enum", "namespace", "string" });
        }

        public List<TypeReference> ReferencedAssemblySerializerFactoryTypes { get; } = new List<TypeReference>();

        public CecilSerializerContext Context { get; }

        //private List<IDataSerializerFactory> serializerFactories = new List<IDataSerializerFactory>();

        public string TargetFramework { get; }

        public string ClassName { get; }

        public AssemblyDefinition Assembly { get; }

        public List<ICecilSerializerDependency> SerializerDependencies { get; } = new List<ICecilSerializerDependency>();

        public List<ICecilSerializerFactory> SerializerFactories { get; } = new List<ICecilSerializerFactory>();

        public ComplexSerializerRegistry(PlatformType platform, AssemblyDefinition assembly, TextWriter log)
        {
            Assembly = assembly;
            ClassName = Utilities.BuildValidClassName(assembly.Name.Name) + "SerializerFactory";

            // Register referenced assemblies serializer factory, so that we can call them recursively
            foreach (var referencedAssemblyName in assembly.MainModule.AssemblyReferences)
            {
                try
                {
                    var referencedAssembly = assembly.MainModule.AssemblyResolver.Resolve(referencedAssemblyName);

                    var assemblySerializerFactoryType = GetSerializerFactoryType(referencedAssembly);
                    if (assemblySerializerFactoryType != null)
                        ReferencedAssemblySerializerFactoryTypes.Add(assemblySerializerFactoryType);
                }
                catch (AssemblyResolutionException)
                {
                    continue;
                }
            }

            // Find target framework and replicate it for serializer assembly.
            var targetFrameworkAttribute = assembly.CustomAttributes
                .FirstOrDefault(x => x.AttributeType.FullName == typeof(TargetFrameworkAttribute).FullName);
            if (targetFrameworkAttribute != null)
            {
                TargetFramework = "\"" + (string)targetFrameworkAttribute.ConstructorArguments[0].Value + "\"";
                var frameworkDisplayNameField = targetFrameworkAttribute.Properties.FirstOrDefault(x => x.Name == "FrameworkDisplayName");
                if (frameworkDisplayNameField.Name != null)
                {
                    TargetFramework += ", FrameworkDisplayName=\"" + (string)frameworkDisplayNameField.Argument.Value + "\"";
                }
            }

            // Prepare serializer processors
            Context = new CecilSerializerContext(platform, assembly, log);
            var processors = new List<ICecilSerializerProcessor>();

            // Import list of serializer registered by referenced assemblies
            processors.Add(new ReferencedAssemblySerializerProcessor());

            // Generate serializers for types tagged as serializable
            processors.Add(new CecilComplexClassSerializerProcessor());

            // Generate serializers for PropertyKey and ParameterKey
            processors.Add(new PropertyKeySerializerProcessor());

            // Update Engine (with AnimationData<T>)
            processors.Add(new UpdateEngineProcessor());

            // Profile serializers
            processors.Add(new ProfileSerializerProcessor());

            // Data contract aliases
            processors.Add(new DataContractAliasProcessor());

            // Apply each processor
            foreach (var processor in processors)
                processor.ProcessSerializers(Context);
        }

        private static TypeDefinition GetSerializerFactoryType(AssemblyDefinition referencedAssembly)
        {
            var assemblySerializerFactoryAttribute =
                referencedAssembly.CustomAttributes.FirstOrDefault(
                    x =>
                        x.AttributeType.FullName ==
                        "Stride.Core.Serialization.AssemblySerializerFactoryAttribute");

            // No serializer factory?
            if (assemblySerializerFactoryAttribute == null)
                return null;

            var typeReference = (TypeReference)assemblySerializerFactoryAttribute.Fields.Single(x => x.Name == "Type").Argument.Value;
            if (typeReference == null)
                return null;

            return typeReference.Resolve();
        }

        private static string TypeNameWithoutGenericEnding(TypeReference type)
        {
            var typeName = type.Name;

            // Remove generics ending (i.e. `1)
            var genericCharIndex = typeName.LastIndexOf('`');
            if (genericCharIndex != -1)
                typeName = typeName.Substring(0, genericCharIndex);

            return typeName;
        }

        public static string SerializerTypeName(TypeReference type, bool appendGenerics, bool appendSerializer)
        {
            var typeName = TypeNameWithoutGenericEnding(type);

            // Prepend nested class
            if (type.IsNested)
                typeName = TypeNameWithoutGenericEnding(type.DeclaringType) + "_" + typeName;

            // Prepend namespace
            if (!String.IsNullOrEmpty(type.Namespace))
                typeName = type.Namespace.Replace(".", String.Empty) + "_" + typeName;

            // Append Serializer
            if (appendSerializer)
                typeName += "Serializer";

            // Append Generics
            if (appendGenerics)
                typeName += type.GenerateGenerics();

            return typeName;
        }

        public static string GetSerializerInstantiateMethodName(TypeReference serializerType, bool appendGenerics)
        {
            return "Instantiate_" + SerializerTypeName(serializerType, appendGenerics, false);
        }

        /// <summary>
        /// Generates the generic constraints in a code form.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static string GenerateGenericConstraints(TypeReference type)
        {
            if (!type.HasGenericParameters)
                return String.Empty;

            var result = new StringBuilder();
            foreach (var genericParameter in type.GenericParameters)
            {
                // If no constraints, skip it
                var hasContraints = genericParameter.HasReferenceTypeConstraint || genericParameter.HasNotNullableValueTypeConstraint || genericParameter.Constraints.Count > 0 || genericParameter.HasDefaultConstructorConstraint;
                if (!hasContraints)
                {
                    continue;
                }

                bool hasFirstContraint = false;

                result.AppendFormat(" where {0}: ", genericParameter.Name);

                // Where class/struct constraint must be before any other constraint
                if (genericParameter.HasReferenceTypeConstraint)
                {
                    result.AppendFormat("class");
                    hasFirstContraint = true;
                }
                else if (genericParameter.HasNotNullableValueTypeConstraint)
                {
                    result.AppendFormat("struct");
                    hasFirstContraint = true;
                }

                foreach (var genericParameterConstraint in genericParameter.Constraints)
                {
                    // Skip value type constraint
                    if (genericParameterConstraint.ConstraintType.FullName != typeof(ValueType).FullName)
                    {
                        if (hasFirstContraint)
                        {
                            result.Append(", ");
                        }

                        result.AppendFormat("{0}", genericParameterConstraint.ConstraintType.ConvertCSharp());
                        result.AppendLine();

                        hasFirstContraint = true;
                    }
                }


                // New constraint must be last
                if (!genericParameter.HasNotNullableValueTypeConstraint && genericParameter.HasDefaultConstructorConstraint)
                {
                    if (hasFirstContraint)
                    {
                        result.Append(", ");
                    }

                    result.AppendFormat("new()");
                    result.AppendLine();
                }
            }

            return result.ToString();
        }

        public static void IgnoreMember(IMemberDefinition memberInfo)
        {
            ignoredMembers.Add(memberInfo);
        }

        public static IEnumerable<SerializableItem> GetSerializableItems(TypeReference type, bool serializeFields, ComplexTypeSerializerFlags? flagsOverride = null)
        {
            foreach (var serializableItemOriginal in GetSerializableItems(type.Resolve(), serializeFields, flagsOverride))
            {
                var serializableItem = serializableItemOriginal;

                // Try to resolve open generic types with context to have closed types.
                if (serializableItem.Type.ContainsGenericParameter())
                {
                    serializableItem.Type = ResolveGenericsVisitor.Process(type, serializableItem.Type);
                }

                yield return serializableItem;
            }
        }

        public static IEnumerable<SerializableItem> GetSerializableItems(TypeDefinition type, bool serializeFields, ComplexTypeSerializerFlags? flagsOverride = null)
        {
            ComplexTypeSerializerFlags flags;

            var fields = new List<FieldDefinition>();
            var properties = new List<PropertyDefinition>();

            var fieldEnum = type.Fields.Where(x => (x.IsPublic || (x.IsAssembly && x.CustomAttributes.Any(a => a.AttributeType.FullName == "Stride.Core.DataMemberAttribute"))) && !x.IsStatic && !ignoredMembers.Contains(x));

            // If there is a explicit or sequential layout, sort by offset
            if (type.IsSequentialLayout || type.IsExplicitLayout)
                fieldEnum = fieldEnum.OrderBy(x => x.Offset);

            foreach (var field in fieldEnum)
            {
                fields.Add(field);
            }

            foreach (var property in type.Properties)
            {
                // Need a non-static public get method
                if (property.GetMethod == null || !property.GetMethod.IsPublic || property.GetMethod.IsStatic)
                    continue;

                // If it's a struct (!IsValueType), we need a public set method as well
                if (property.PropertyType.IsValueType && (property.SetMethod == null || !(property.SetMethod.IsAssembly || property.SetMethod.IsPublic)))
                    continue;

                // Only take virtual properties (override ones will be handled by parent serializers)
                if (property.GetMethod.IsVirtual && !property.GetMethod.IsNewSlot)
                {
                    // Exception: if this one has a DataMember, let's assume parent one was Ignore and we explicitly want to serialize this one
                    if (!property.CustomAttributes.Any(x => x.AttributeType.FullName == "Stride.Core.DataMemberAttribute"))
                        continue;
                }

                // Ignore blacklisted properties
                if (ignoredMembers.Contains(property))
                    continue;

                properties.Add(property);
            }

            if (flagsOverride.HasValue)
                flags = flagsOverride.Value;
            else if (type.IsClass && !type.IsValueType)
                flags = ComplexTypeSerializerFlags.SerializePublicFields | ComplexTypeSerializerFlags.SerializePublicProperties;
            else if (type.Fields.Any(x => x.IsPublic && !x.IsStatic))
                flags = ComplexTypeSerializerFlags.SerializePublicFields;
            else
                flags = ComplexTypeSerializerFlags.SerializePublicProperties;

            // Find default member mode (find DataContractAttribute in the hierarchy)
            var defaultMemberMode = DataMemberMode.Default;
            var currentType = type;
            while (currentType != null)
            {
                var dataContractAttribute = currentType.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == "Stride.Core.DataContractAttribute");
                if (dataContractAttribute != null)
                {
                    var dataMemberModeArg = dataContractAttribute.Properties.FirstOrDefault(x => x.Name == "DefaultMemberMode").Argument;
                    if (dataMemberModeArg.Value != null)
                    {
                        defaultMemberMode = (DataMemberMode)(int)dataMemberModeArg.Value;
                        break;
                    }
                }
                currentType = currentType.BaseType?.Resolve();
            }

            if ((flags & ComplexTypeSerializerFlags.SerializePublicFields) != 0)
            {
                foreach (var field in fields)
                {
                    if (IsMemberIgnored(field.CustomAttributes, flags, defaultMemberMode)) continue;
                    var attributes = field.CustomAttributes;
                    var fixedAttribute = field.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == typeof(FixedBufferAttribute).FullName);
                    var assignBack = !field.IsInitOnly;

                    // If not assigned back, check that type is serializable in place
                    if (!assignBack && !IsReadOnlyTypeSerializable(field.FieldType))
                        continue;

                    yield return new SerializableItem { MemberInfo = field, Type = field.FieldType, Name = field.Name, Attributes = attributes, AssignBack = assignBack, NeedReference = false, HasFixedAttribute = fixedAttribute != null };
                }
            }
            if ((flags & ComplexTypeSerializerFlags.SerializePublicProperties) != 0)
            {
                // Only process properties with public get and set methods
                foreach (var property in properties)
                {
                    // Ignore properties with indexer
                    if (property.GetMethod.Parameters.Count > 0)
                        continue;
                    if (IsMemberIgnored(property.CustomAttributes, flags, defaultMemberMode)) continue;
                    var attributes = property.CustomAttributes;
                    var assignBack = property.SetMethod != null && (property.SetMethod.IsPublic || property.SetMethod.IsAssembly);

                    // If not assigned back, check that type is serializable in place
                    if (!assignBack && !IsReadOnlyTypeSerializable(property.PropertyType))
                        continue;

                    yield return new SerializableItem { MemberInfo = property, Type = property.PropertyType, Name = property.Name, Attributes = attributes, AssignBack = assignBack, NeedReference = !type.IsClass || type.IsValueType };
                }
            }
        }

        internal static bool IsMemberIgnored(ICollection<CustomAttribute> customAttributes, ComplexTypeSerializerFlags flags, DataMemberMode dataMemberMode)
        {
            // Check for DataMemberIgnore
            if (customAttributes.Any(x => x.AttributeType.FullName == "Stride.Core.DataMemberIgnoreAttribute"))
            {
                // Still allow members with DataMemberUpdatable if we are running UpdateEngineProcessor
                if (!((flags & ComplexTypeSerializerFlags.Updatable) != 0
                      && customAttributes.Any(x => x.AttributeType.FullName == "Stride.Updater.DataMemberUpdatableAttribute")))
                    return true;
            }
            var dataMemberAttribute = customAttributes.FirstOrDefault(x => x.AttributeType.FullName == "Stride.Core.DataMemberAttribute");
            if (dataMemberAttribute != null)
            {
                var dataMemberModeArg = dataMemberAttribute.ConstructorArguments.FirstOrDefault(x => x.Type.Name == nameof(DataMemberMode));
                if (dataMemberModeArg.Value != null)
                {
                    dataMemberMode = (DataMemberMode)(int)dataMemberModeArg.Value;
                }
                else
                {
                    // Default value if not specified in .ctor
                    dataMemberMode = DataMemberMode.Default;
                }
            }

            // Ignored?
            if (dataMemberMode == DataMemberMode.Never)
                return true;

            return false;
        }

        private static bool IsReadOnlyTypeSerializable(TypeReference type)
        {
            // For now, we allow any non-valuetype (class & interface) which is not a string (since they are immutable)
            return type.MetadataType != MetadataType.String
                // sometimes class/valuetype is not properly set in some reference types (not sure if it was the exact same case)
                && !((type.MetadataType == MetadataType.ValueType || type.MetadataType == MetadataType.Class) && type.Resolve().IsValueType);
        }

        public static string CreateMemberVariableName(IMemberDefinition memberInfo)
        {
            var memberVariableName = Char.ToLowerInvariant(memberInfo.Name[0]) + memberInfo.Name.Substring(1);
            if (forbiddenKeywords.Contains(memberVariableName))
                memberVariableName += "_";
            return memberVariableName;
        }

        public struct SerializableItem
        {
            public bool HasFixedAttribute;
            public string Name;
            public IMemberDefinition MemberInfo;
            public TypeReference Type { get; set; }
            public bool NeedReference;
            public bool AssignBack;
            public IList<CustomAttribute> Attributes;
        }
    }
}
