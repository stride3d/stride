// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.Yaml.Events;

namespace Stride.Core.Yaml
{
    public static class UnloadableObjectInstantiator
    {
        private static Dictionary<Type, Type> proxyTypes = new Dictionary<Type, Type>();

        public delegate void ProcessProxyTypeDelegate(Type baseType, TypeBuilder typeBuilder);

        /// <summary>
        /// Callback to perform additional changes to the generated proxy object.
        /// </summary>
        public static ProcessProxyTypeDelegate ProcessProxyType;

        /// <summary>
        /// Creates an object that implements the given <paramref name="baseType"/> and <see cref="IUnloadable"/>.
        /// </summary>
        /// <param name="baseType"></param>
        /// <param name="typeName"></param>
        /// <param name="parsingEvents"></param>
        /// <returns></returns>
        public static IUnloadable CreateUnloadableObject(Type baseType, string typeName, string assemblyName, string error, List<ParsingEvent> parsingEvents)
        {
            Type proxyType;
            lock (proxyTypes)
            {
                if (!proxyTypes.TryGetValue(baseType, out proxyType))
                {
                    var asmName = new AssemblyName($"YamlProxy_{Guid.NewGuid():N}");

                    // Create assembly (in memory)
                    var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
                    var moduleBuilder = asmBuilder.DefineDynamicModule("DynamicModule");

                    // Create type
                    var typeBuilder = moduleBuilder.DefineType($"{baseType}YamlProxy");
                    AbstractObjectInstantiator.InitializeTypeBuilderFromType(typeBuilder, baseType);

                    // Add DisplayAttribute
                    var displayAttributeCtor = typeof(DisplayAttribute).GetConstructor(new Type[] { typeof(string), typeof(string) });
                    var displayAttribute = new CustomAttributeBuilder(displayAttributeCtor, new object[] { "Error: unable to load this object", null });
                    typeBuilder.SetCustomAttribute(displayAttribute);

                    // Add NonInstantiableAttribute
                    var nonInstantiableAttributeCtor = typeof(NonInstantiableAttribute).GetConstructor(Type.EmptyTypes);
                    var nonInstantiableAttribute = new CustomAttributeBuilder(nonInstantiableAttributeCtor, new object[0]);
                    typeBuilder.SetCustomAttribute(nonInstantiableAttribute);

                    // Implement IUnloadable
                    typeBuilder.AddInterfaceImplementation(typeof(IUnloadable));

                    var backingFields = new List<FieldBuilder>();
                    foreach (var property in new[] { new { Name = nameof(IUnloadable.TypeName), Type = typeof(string) }, new { Name = nameof(IUnloadable.AssemblyName), Type = typeof(string) }, new { Name = nameof(IUnloadable.Error), Type = typeof(string) }, new { Name = nameof(IUnloadable.ParsingEvents), Type = typeof(List<ParsingEvent>) } })
                    {
                        // Add backing field
                        var backingField = typeBuilder.DefineField($"{property.Name.ToLowerInvariant()}", property.Type, FieldAttributes.Private);
                        backingFields.Add(backingField);

                        // Create property
                        var propertyBuilder = typeBuilder.DefineProperty(property.Name, PropertyAttributes.HasDefault, property.Type, Type.EmptyTypes);

                        // Create getter method
                        var propertyGetter = typeBuilder.DefineMethod($"get_{property.Name}",
                            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual, property.Type, Type.EmptyTypes);
                        var propertyGetterIL = propertyGetter.GetILGenerator();
                        propertyGetterIL.Emit(OpCodes.Ldarg_0);
                        propertyGetterIL.Emit(OpCodes.Ldfld, backingField);
                        propertyGetterIL.Emit(OpCodes.Ret);
                        propertyBuilder.SetGetMethod(propertyGetter);

                        // Add DataMemberIgnoreAttribute
                        var dataMemberIgnoreAttributeCtor = typeof(DataMemberIgnoreAttribute).GetConstructor(Type.EmptyTypes);
                        var dataMemberIgnoreAttribute = new CustomAttributeBuilder(dataMemberIgnoreAttributeCtor, new object[0]);
                        propertyBuilder.SetCustomAttribute(dataMemberIgnoreAttribute);
                    }

                    // .ctor (initialize backing fields too)
                    var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, backingFields.Select(x => x.FieldType).ToArray());
                    var ctorIL = ctor.GetILGenerator();
                    // Call parent ctor (if one without parameters exist)
                    var defaultCtor = baseType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
                    if (defaultCtor != null)
                    {
                        ctorIL.Emit(OpCodes.Ldarg_0);
                        ctorIL.Emit(OpCodes.Call, defaultCtor);
                    }
                    // Initialize fields
                    for (var index = 0; index < backingFields.Count; index++)
                    {
                        var backingField = backingFields[index];
                        ctorIL.Emit(OpCodes.Ldarg_0);
                        ctorIL.Emit(OpCodes.Ldarg, index + 1);
                        ctorIL.Emit(OpCodes.Stfld, backingField);
                    }
                    ctorIL.Emit(OpCodes.Ret);

                    // User-registered callbacks
                    ProcessProxyType?.Invoke(baseType, typeBuilder);

                    proxyType = typeBuilder.CreateTypeInfo();
                    proxyTypes.Add(baseType, proxyType);
                }
            }

            return (IUnloadable)Activator.CreateInstance(proxyType, typeName, assemblyName, error, parsingEvents);
        }
    }
}
