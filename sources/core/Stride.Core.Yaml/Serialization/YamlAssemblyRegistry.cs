// Copyright (c) 2015 SharpYaml - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// -------------------------------------------------------------------------------
// SharpYaml is a fork of YamlDotNet https://github.com/aaubry/YamlDotNet
// published with the following license:
// -------------------------------------------------------------------------------
// 
// Copyright (c) 2008, 2009, 2010, 2011, 2012 Antoine Aubry
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Core.Yaml.Schemas;

namespace Stride.Core.Yaml.Serialization
{
    /// <summary>
    /// Default implementation of ITagTypeRegistry.
    /// </summary>
    internal class YamlAssemblyRegistry : ITagTypeRegistry
    {
        private static readonly Logger Log = GlobalLogger.GetLogger("YamlAssemblyRegistry");

        private readonly IYamlSchema schema;
        private readonly Dictionary<string, MappedType> tagToType;
        private readonly Dictionary<Type, string> typeToTag;
        private readonly List<Assembly> lookupAssemblies;
        private readonly object lockCache = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="YamlAssemblyRegistry"/> class.
        /// </summary>
        public YamlAssemblyRegistry(IYamlSchema schema)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            this.schema = schema;
            tagToType = new Dictionary<string, MappedType>();
            typeToTag = new Dictionary<Type, string>();
            lookupAssemblies = new List<Assembly> { typeof(int).Assembly };

            SerializableFactories = new List<IYamlSerializableFactory>();
        }

        /// <summary>
        /// Gets the serializable factories.
        /// </summary>
        /// <value>The serializable factories.</value>
        public List<IYamlSerializableFactory> SerializableFactories { get; }

        /// <summary>
        /// Gets or sets a value indicating whether [use short type name].
        /// </summary>
        /// <value><c>true</c> if [use short type name]; otherwise, <c>false</c>.</value>
        public bool UseShortTypeName { get; set; } = true;

        public void RegisterAssembly(Assembly assembly, IAttributeRegistry attributeRegistry)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            if (attributeRegistry == null) throw new ArgumentNullException(nameof(attributeRegistry));

            // Add automatically the assembly for lookup
            if (!lookupAssemblies.Contains(assembly))
            {
                lookupAssemblies.Add(assembly);

                // Register all tags automatically.
                var assemblySerializers = DataSerializerFactory.GetAssemblySerializers(assembly);
                if (assemblySerializers != null)
                {
                    foreach (var dataContractAlias in assemblySerializers.DataContractAliases)
                    {
                        RegisterTagMapping(dataContractAlias.Name, dataContractAlias.Type, dataContractAlias.IsAlias);
                    }
                }
                else
                {
                    Log.Warning($"Assembly [{assembly}] has not been processed by assembly processor with --serialization flags. [DataContract] aliases won't be available.");
                }
                // Automatically register YamlSerializableFactory
                var assemblyScanTypes = AssemblyRegistry.GetScanTypes(assembly);
                if (assemblyScanTypes != null)
                {
                    List<Type> types;

                    // Register serializer factories
                    if (assemblyScanTypes.Types.TryGetValue(typeof(IYamlSerializableFactory), out types))
                    {
                        foreach (var type in types)
                        {
                            if (typeof(IYamlSerializableFactory).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) != null)
                            {
                                try
                                {
                                    SerializableFactories.Add((IYamlSerializableFactory)Activator.CreateInstance(type));
                                }
                                catch
                                {
                                    // Registrying an assembly should not fail, so we are silently discarding a factory if 
                                    // we are not able to load it.
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Register a mapping between a tag and a type.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <param name="type">The type.</param>
        /// <param name="alias"></param>
        public virtual void RegisterTagMapping(string tag, Type type, bool alias)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            if (type == null) throw new ArgumentNullException(nameof(type));

            // Prefix all tags by !
            tag = Uri.EscapeUriString(tag);
            if (tag.StartsWith("tag:"))
            {
                // shorten tag
                // TODO this is not really failsafe
                var shortTag = "!!" + tag.Substring(tag.LastIndexOf(':') + 1);

                // Auto register tag to schema
                schema.RegisterTag(shortTag, tag);
                tag = shortTag;
            }

            tag = tag.StartsWith("!") ? tag : "!" + tag;

            lock (lockCache)
            {
                tagToType[tag] = new MappedType(type, alias);

                // Only register types that are not aliases
                if (!alias)
                    typeToTag[type] = tag;
            }
        }

        public virtual Type TypeFromTag(string tag, out bool isAlias)
        {
            isAlias = false;

            if (tag == null)
            {
                return null;
            }

            // Get the default schema type if there is any
            var shortTag = schema.ShortenTag(tag);
            Type type;
            if (shortTag != tag || shortTag.StartsWith("!!"))
            {
                type = schema.GetTypeForDefaultTag(shortTag);
                if (type != null)
                {
                    return type;
                }
            }

            // un-escape tag
            shortTag = Uri.UnescapeDataString(shortTag);

            lock (lockCache)
            {
                MappedType mappedType;
                // Else try to find a registered alias
                if (tagToType.TryGetValue(shortTag, out mappedType))
                {
                    isAlias = mappedType.Remapped;
                    return mappedType.Type;
                }

                // Else resolve type from assembly
                var tagAsType = shortTag.StartsWith("!") ? shortTag.Substring(1) : shortTag;

                // Try to resolve the type from registered assemblies
                type = ResolveType(tagAsType);

                // Register a type that was found
                tagToType.Add(shortTag, new MappedType(type, false));
                if (type != null && !typeToTag.ContainsKey(type))
                {
                    typeToTag.Add(type, shortTag);
                }
            }

            return type;
        }

        public virtual string TagFromType(Type type)
        {
            if (type == null)
            {
                return "!!null";
            }

            string tagName;

            lock (lockCache)
            {
                // First try to resolve a tag from registered tag
                if (!typeToTag.TryGetValue(type, out tagName))
                {
                    // Else try to use schema tag for scalars
                    // Else use full name of the type

                    var typeName = GetShortAssemblyQualifiedName(type);
                    if (!UseShortTypeName)
                        throw new NotSupportedException("UseShortTypeName supports only True.");

                    // TODO: either remove completely support of UseShortTypeName == false, or make it work in all scenario (with unit tests, etc.)
                    //var typeName = UseShortTypeName ? type.GetShortAssemblyQualifiedName() : type.AssemblyQualifiedName;

                    tagName = schema.GetDefaultTag(type) ?? $"!{typeName}";
                    typeToTag.Add(type, tagName);
                }
            }

            return Uri.EscapeUriString(tagName);
        }

        public virtual Type ResolveType(string typeName)
        {
            if (typeName == null) throw new ArgumentNullException(nameof(typeName));
            List<string> genericArguments;
            int arrayNesting;
            var resolvedTypeName = GetGenericArgumentsAndArrayDimension(typeName, out genericArguments, out arrayNesting);
            var resolvedType = ResolveSingleType(resolvedTypeName);
            if (genericArguments != null)
            {
                var genericTypes = new List<Type>();
                foreach (var genericArgument in genericArguments)
                {
                    var genericType = ResolveType(genericArgument);
                    genericTypes.Add(genericType);
                }
                resolvedType = resolvedType.MakeGenericType(genericTypes.ToArray());
            }
            while (arrayNesting > 0)
            {
                resolvedType = resolvedType.MakeArrayType();
                --arrayNesting;
            }
            return resolvedType;
        }

        private Type ResolveSingleType(string typeName)
        {
            string assemblyName;

            // Find assembly name start (skip up to one space if needed)
            // We ignore everything else (version, publickeytoken, etc...)
            if (UseShortTypeName)
            {
                ParseType(typeName, out typeName, out assemblyName);
            }
            else
            {
                // TODO: either remove completely support of UseShortTypeName == false, or make it work in all scenario (with unit tests, etc.)
                throw new NotSupportedException("UseShortTypeName supports only True.");
            }

            // Look for type in loaded assemblies
            foreach (var assembly in lookupAssemblies)
            {
                if (assemblyName != null)
                {
                    // Check that assembly name match, by comparing up to the first comma
                    var assemblyFullName = assembly.FullName;
                    if (string.Compare(assemblyFullName, 0, assemblyName, 0, assemblyName.Length) != 0
                        || !(assemblyFullName.Length == assemblyName.Length || assemblyFullName[assemblyName.Length] == ','))
                    {
                        continue;
                    }
                }

                var type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        public void ParseType(string typeFullName, out string typeName, out string assemblyName)
        {
            var typeNameEnd = typeFullName.IndexOf(',');
            var assemblyNameStart = typeNameEnd;
            if (assemblyNameStart != -1 && typeFullName[++assemblyNameStart] == ' ') // Skip first comma and check if we have a space
                assemblyNameStart++; // Skip first space

            // Extract assemblyName and readjust typeName to not include assemblyName anymore
            if (assemblyNameStart != -1)
            {
                var assemblyNameEnd = typeFullName.IndexOf(',', assemblyNameStart);
                assemblyName = assemblyNameEnd != -1
                    ? typeFullName.Substring(assemblyNameStart, assemblyNameEnd - assemblyNameStart)
                    : typeFullName.Substring(assemblyNameStart);

                typeName = typeFullName.Substring(0, typeNameEnd);
            }
            else
            {
                typeName = typeFullName;
                assemblyName = null;
            }
        }

        /// <summary>
        /// Gets the assembly qualified name of the type, but without the assembly version or public token.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The assembly qualified name of the type, but without the assembly version or public token.</returns>
        /// <exception cref="InvalidOperationException">Unable to get an assembly qualified name for type.</exception>
        /// <example>
        ///     <list type="bullet">
        ///         <item><c>typeof(string).GetShortAssemblyQualifiedName(); // System.String,mscorlib</c></item>
        ///         <item><c>typeof(string[]).GetShortAssemblyQualifiedName(); // System.String[],mscorlib</c></item>
        ///         <item><c>typeof(List&lt;string&gt;).GetShortAssemblyQualifiedName(); // System.Collection.Generics.List`1[[System.String,mscorlib]],mscorlib</c></item>
        ///     </list>
        /// </example>
        private static string GetShortAssemblyQualifiedName(Type type)
        {
            if (type.AssemblyQualifiedName == null)
                throw new InvalidOperationException($"Unable to get an assembly qualified name for type [{type}]");

            var sb = new StringBuilder();
            DoGetShortAssemblyQualifiedName(type, sb);
            return sb.ToString();
        }

        /// <summary>
        /// Split the given short assembly-qualified type name into a generic definition type and a collection of generic argument types, and retrieve the dimension of the array if the type is an array type.
        /// </summary>
        /// <param name="shortAssemblyQualifiedName">The given short assembly-qualified type name to split.</param>
        /// <param name="genericArguments">The generic argument types extracted, if the given type was generic. Otherwise null.</param>
        /// <param name="arrayNesting">The number of arrays that are nested if the type is an array type.</param>
        /// <returns>The corresponding generic definition type.</returns>
        /// <remarks>If the given type is not generic, this method sets <paramref name="genericArguments"/> to null and returns <paramref name="shortAssemblyQualifiedName"/>.</remarks>
        [NotNull]
        private static string GetGenericArgumentsAndArrayDimension([NotNull] string shortAssemblyQualifiedName, [CanBeNull] out List<string> genericArguments, out int arrayNesting)
        {
            if (shortAssemblyQualifiedName == null) throw new ArgumentNullException(nameof(shortAssemblyQualifiedName));
            var firstBracket = int.MaxValue;
            var lastBracket = int.MinValue;
            var bracketLevel = 0;
            genericArguments = null;
            arrayNesting = 0;
            var startIndex = 0;
            for (var i = 0; i < shortAssemblyQualifiedName.Length; ++i)
            {
                if (shortAssemblyQualifiedName[i] == '[')
                {
                    firstBracket = Math.Min(firstBracket, i);
                    ++bracketLevel;
                    if (bracketLevel == 2)
                    {
                        startIndex = i + 1;
                    }
                }
                if (shortAssemblyQualifiedName[i] == ']')
                {
                    lastBracket = Math.Max(lastBracket, i);
                    --bracketLevel;
                    if (bracketLevel == 1)
                    {
                        if (genericArguments == null)
                            genericArguments = new List<string>();

                        genericArguments.Add(shortAssemblyQualifiedName.Substring(startIndex, i - startIndex));
                    }
                    if (bracketLevel == 0 && i > 0)
                    {
                        if (shortAssemblyQualifiedName[i - 1] == '[')
                        {
                            ++arrayNesting;
                        }
                    }
                }
            }
            if (genericArguments != null || arrayNesting > 0)
            {
                var genericType = shortAssemblyQualifiedName.Substring(0, firstBracket) + shortAssemblyQualifiedName.Substring(lastBracket + 1);
                return genericType;
            }
            return shortAssemblyQualifiedName;
        }

        private static void DoGetShortAssemblyQualifiedName(Type type, StringBuilder sb, bool appendAssemblyName = true)
        {
            // namespace
            sb.Append(type.Namespace).Append(".");
            // check if it's an array, store the information, and work on the element type
            var arrayNesting = 0;
            while (type.IsArray)
            {
                if (type.GetArrayRank() != 1)
                    throw new NotSupportedException("Multi-dimensional arrays are not supported.");
                type = type.GetElementType();
                ++arrayNesting;
            }
            // nested declaring types
            var declaringType = type.DeclaringType;
            if (declaringType != null)
            {
                var declaringTypeName = string.Empty;
                do
                {
                    declaringTypeName = declaringType.Name + "+" + declaringTypeName;
                    declaringType = declaringType.DeclaringType;
                } while (declaringType != null);
                sb.Append(declaringTypeName);
            }
            // type
            sb.Append(type.Name);
            // generic arguments
            if (type.IsGenericType)
            {
                sb.Append("[[");
                var genericArguments = type.GetGenericArguments();
                for (var i = 0; i < genericArguments.Length; i++)
                {
                    if (i > 0)
                        sb.Append("],[");
                    DoGetShortAssemblyQualifiedName(genericArguments[i], sb);
                }
                sb.Append("]]");
            }
            while (arrayNesting > 0)
            {
                --arrayNesting;
                sb.Append("[]");
            }
            // assembly
            if (appendAssemblyName)
                sb.Append(",").Append(GetShortAssemblyName(type.Assembly));
        }

        /// <summary>
        /// Gets the qualified name of the assembly, but without the assembly version or public token.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>The qualified name of the assembly, but without the assembly version or public token.</returns>
        private static string GetShortAssemblyName(Assembly assembly)
        {
            var assemblyName = assembly.FullName;
            var indexAfterAssembly = assemblyName.IndexOf(',');
            if (indexAfterAssembly >= 0)
            {
                assemblyName = assemblyName.Substring(0, indexAfterAssembly);
            }
            return assemblyName;
        }
        struct MappedType
        {
            public MappedType(Type type, bool remapped)
            {
                Type = type;
                Remapped = remapped;
            }

            public readonly Type Type;

            public readonly bool Remapped;
        }
    }
}
