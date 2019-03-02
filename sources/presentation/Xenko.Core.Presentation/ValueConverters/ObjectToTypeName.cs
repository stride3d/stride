// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using Xenko.Core.Annotations;
using System.CodeDom;
using Microsoft.CSharp;
using System.Text.RegularExpressions;

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

        private readonly CSharpCodeProvider codeProvider = new CSharpCodeProvider();

        /// <inheritdoc/>
        [NotNull]
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return NullObjectType;

            Type valueType = value.GetType();
            var fullTypeName = codeProvider.GetTypeOutput(new CodeTypeReference(valueType));

            //Change System.Nullable to "Type?" format
            var typeName = Regex.Replace(fullTypeName, @"System\.Nullable<([^>]*)>", "$1?");

            //Simplify name. i.e. remove namespaces etc.
            return Regex.Replace(typeName, @"[^,<>]*\.", "");
        }
    }
}
