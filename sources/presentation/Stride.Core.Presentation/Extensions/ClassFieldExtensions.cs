// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq.Expressions;

namespace Stride.Core.Presentation.Extensions;

public static class ClassFieldExtensions
{
    public static Func<TInstance, TValue> GetFieldAccessor<TInstance, TValue>(string fieldName)
    {
        var instanceParam = Expression.Parameter(typeof(TInstance), "instance");
        var member = Expression.Field(instanceParam, fieldName);
        var lambda = Expression.Lambda(typeof(Func<TInstance, TValue>), member, instanceParam);

        return (Func<TInstance, TValue>)lambda.Compile();
    }

    public static Func<object, object> GetFieldAccessor(string fieldName, Type instanceType, Type valueType)
    {
        var instanceParam = Expression.Parameter(instanceType, "instance");
        var member = Expression.Field(instanceParam, fieldName);
        var lambda = Expression.Lambda(typeof(Func<object, object>), member, instanceParam);

        return (Func<object, object>)lambda.Compile();
    }

    public static Action<TInstance, TValue> SetFieldAccessor<TInstance, TValue>(string fieldName)
    {
        var instanceParam = Expression.Parameter(typeof(TInstance), "instance");
        var valueParam = Expression.Parameter(typeof(TValue), "value");
        var member = Expression.Field(instanceParam, fieldName);
        var assign = Expression.Assign(member, valueParam);
        var lambda = Expression.Lambda(typeof(Action<TInstance, TValue>), assign, instanceParam, valueParam);

        return (Action<TInstance, TValue>)lambda.Compile();
    }

    public static Action<object, object> SetFieldAccessor(string fieldName, Type instanceType,  Type valueType)
    {
        var instanceParam = Expression.Parameter(instanceType, "instance");
        var valueParam = Expression.Parameter(valueType, "value");
        var member = Expression.Field(instanceParam, fieldName);
        var assign = Expression.Assign(member, valueParam);
        var lambda = Expression.Lambda(typeof(Action<object, object>), assign, instanceParam, valueParam);

        return (Action<object, object>)lambda.Compile();
    }
}
