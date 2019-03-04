// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using Xenko.Core.Annotations;
using System.CodeDom;
using Microsoft.CSharp;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

namespace Xenko.Core.Presentation.ValueConverters
{
    /// <summary>
    /// This converter convert any object to a string representing the name of its type (without assembly or namespace qualification).
    /// It accepts null and will convert it to a string representation of null.
    /// </summary>
    /// <seealso cref="ObjectToFullTypeName"/>
    /// <seealso cref="ObjectToType"/>
    public class ObjectToTypeName : OneWayValueConverter<ObjectToTypeName>
    {
        /// <summary>
        /// The string representation of the type of a null object
        /// </summary>
        public const string NullObjectType = "(None)";        

        /// <inheritdoc/>
        [NotNull]
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return NullObjectType;

            return value.GetType().ToSimpleCSharpName();
            
        }
    }


    /// <summary>
    /// Helper class to get formatted names from <see cref="Type"/>.
    /// </summary>
    public static class TypeNameHelper
    {
        private static readonly CSharpCodeProvider codeProvider = new CSharpCodeProvider();
        private static readonly HashSet<Type> valueTupleTypes = new HashSet<Type>
        {
            typeof(ValueTuple<>),
            typeof(ValueTuple<,>),
            typeof(ValueTuple<,,>),
            typeof(ValueTuple<,,,>),
            typeof(ValueTuple<,,,,>),
            typeof(ValueTuple<,,,,,>),
            typeof(ValueTuple<,,,,,,>),
            typeof(ValueTuple<,,,,,,,>),
        };

        private static bool IsValueTuple(Type type) =>
            type.IsGenericType &&
            valueTupleTypes.Contains(type.GetGenericTypeDefinition());

        /// <summary>
        /// Gets C# syntax like type declaration from <see cref="Type"/>
        /// </summary>
        /// <param name="type">The type to get the name from.</param>
        /// <returns>C# syntax like type declaration from the type provided</returns>
        /// <exception cref="ArgumentNullException">If type parameter is null.</exception>
        public static string ToSimpleCSharpName(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (type.IsArray)
            {
                return ToSimpleCSharpName(type.GetElementType()) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
            }

            if (!type.IsGenericType)
            {
                //Use a CSharpCodeProvider to handle conversions like 'Int32' to 'int'
                var fullTypeName = codeProvider.GetTypeOutput(new CodeTypeReference(type));

                var simpleNameStart = fullTypeName.LastIndexOf('.') + 1;

                if (simpleNameStart >= fullTypeName.Length)
                    return fullTypeName;

                return fullTypeName.Substring(simpleNameStart);
            }

            if(Nullable.GetUnderlyingType(type) is Type nullableType)
            {
                return ToSimpleCSharpName(nullableType) + "?";
            }

            var genericParameters = string.Join(", ", type.GetGenericArguments().Select(ToSimpleCSharpName));

            if (IsValueTuple(type))
            {
                return "(" + genericParameters + ")";
            }

            return type.Name.Substring(0, type.Name.LastIndexOf('`')) + "<" + genericParameters + ">";

        }
    }
}
