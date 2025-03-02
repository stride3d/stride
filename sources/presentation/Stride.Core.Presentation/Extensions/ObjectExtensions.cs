// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Reflection.Emit;

namespace Stride.Core.Presentation.Extensions;

public static class ObjectExtensions
{
    private static readonly Dictionary<Type, Delegate> CachedMemberwiseCloneMethods = [];

    public static object MemberwiseClone(this object instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var instanceType = instance.GetType();

        if (!CachedMemberwiseCloneMethods.TryGetValue(instanceType, out var method))
        {
            var dynamicMethod = GenerateDynamicMethod(instanceType);

            var methodType = typeof(Func<,>).MakeGenericType(instanceType, instanceType);
            method = dynamicMethod.CreateDelegate(methodType);

            CachedMemberwiseCloneMethods.Add(instanceType, method);
        }

        return method.DynamicInvoke(instance);
    }

    public static T MemberwiseClone<T>(this T instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var instanceType = typeof(T);

        if (!CachedMemberwiseCloneMethods.TryGetValue(instanceType, out var method))
        {
            var dynamicMethod = GenerateDynamicMethod(instanceType);

            method = dynamicMethod.CreateDelegate(typeof(Func<T, T>));

            CachedMemberwiseCloneMethods.Add(typeof(T), method);
        }

        return ((Func<T, T>)method)(instance);
    }

    private static DynamicMethod GenerateDynamicMethod(Type instanceType)
    {
        var dymMethod = new DynamicMethod("DynamicCloneMethod", instanceType, [instanceType], true);

        var generator = dymMethod.GetILGenerator();

        generator.DeclareLocal(instanceType);

        var isValueType = instanceType.IsValueType;

        if (isValueType)
        {
            generator.Emit(OpCodes.Ldloca, 0);
            generator.Emit(OpCodes.Initobj, instanceType);
        }
        else
        {
            var constructorInfo = instanceType.GetConstructor([]);
            generator.Emit(OpCodes.Newobj, constructorInfo);
            generator.Emit(OpCodes.Stloc_0);
        }

        foreach (var field in instanceType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            // Load the new object on the eval stack... (currently 1 item on eval stack)
            if (isValueType)
                generator.Emit(OpCodes.Ldloca, 0);
            else
                generator.Emit(OpCodes.Ldloc_0);
            // Load initial object (parameter)          (currently 2 items on eval stack)
            generator.Emit(OpCodes.Ldarg_0);
            // Replace value by field value             (still currently 2 items on eval stack)
            generator.Emit(OpCodes.Ldfld, field);
            // Store the value of the top on the eval stack into the object underneath that value on the value stack.
            //  (0 items on eval stack)
            generator.Emit(OpCodes.Stfld, field);
        }

        // Load new constructed obj on eval stack -> 1 item on stack
        generator.Emit(OpCodes.Ldloc_0);
        // Return constructed object.   --> 0 items on stack
        generator.Emit(OpCodes.Ret);

        return dymMethod;
    }
}
