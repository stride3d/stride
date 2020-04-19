// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
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
// -----------------------------------------------------------------------------
// Original code from SlimMath project. http://code.google.com/p/slimmath/
// Greetings to SlimDX Group. Original code published with the following license:
// -----------------------------------------------------------------------------
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
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

using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;

using Stride.Core.Mathematics;

namespace Stride.Core.TypeConverters
{
    /// <summary>
    /// Provides a type converter to convert <see cref="T:SlimDX.Half" /> objects to and from various
    /// other representations.
    /// </summary>
    public class HalfConverter : ExpandableObjectConverter
    {
        /// <summary>
        /// Returns whether this converter can convert an object of the given type to the type of this converter, using the specified context.
        /// </summary>
        /// <param name="context">A <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context.</param>
        /// <param name="sourceType">A System::Type that represents the type you want to convert from.</param>
        /// <returns>
        /// <c>true</c> if this converter can perform the conversion; otherwise, <c>false</c>.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return ((sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType));
        }

        /// <summary>
        /// Returns whether this converter can convert the object to the specified type, using the specified context.
        /// </summary>
        /// <param name="context">A <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context.</param>
        /// <param name="destinationType">A <see cref="T:System.Type" /> that represents the type you want to convert to.</param>
        /// <returns>
        /// <c>true</c> if this converter can perform the conversion; otherwise, <c>false</c>.</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if ((destinationType != typeof(string)) && (destinationType != typeof(InstanceDescriptor)))
            {
                return base.CanConvertTo(context, destinationType);
            }
            return true;
        }

        /// <summary>
        /// Converts the given object to the type of this converter, using the specified context and culture information.
        /// </summary>
        /// <param name="context">A <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context.</param>
        /// <param name="culture">A <see cref="T:System.Globalization.CultureInfo" />. If <c>null</c> is passed, the current culture is assumed.</param>
        /// <param name="value">The <see cref="T:System.Object" /> to convert.</param>
        /// <returns>An <see cref="T:System.Object" /> that represents the converted value.</returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (culture == null)
            {
                culture = CultureInfo.CurrentCulture;
            }
            string @string = value as string;
            if (@string == null)
            {
                return base.ConvertFrom(context, culture, value);
            }
            @string = @string.Trim();
            char[] separator = new char[] { culture.TextInfo.ListSeparator[0] };
            string[] stringArray = @string.Split(separator);
            if (stringArray.Length != 1)
            {
                throw new ArgumentException("Invalid half format.");
            }
            float h = (float)TypeDescriptor.GetConverter(typeof(float)).ConvertFromString(context, culture, stringArray[0]);
            Half type = new Half(h);
            return type;
        }

        /// <summary>
        /// Converts the given value object to the specified type, using the specified context and culture information.
        /// </summary>
        /// <param name="context">A <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context.</param>
        /// <param name="culture">A <see cref="T:System.Globalization.CultureInfo" />. If <c>null</c> is passed, the current culture is assumed.</param>
        /// <param name="value">The <see cref="T:System.Object" /> to convert.</param>
        /// <param name="destinationType">A <see cref="T:System.Type" /> that represents the type you want to convert to.</param>
        /// <returns>An <see cref="T:System.Object" /> that represents the converted value.</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }
            if (culture == null)
            {
                culture = CultureInfo.CurrentCulture;
            }
            if ((destinationType == typeof(string)) && value is Half)
            {
                TypeConverter converter = TypeDescriptor.GetConverter(typeof(float));
                return string.Join(culture.TextInfo.ListSeparator + " ", new string[] { converter.ConvertToString(context, culture, (float)((Half)value)) });
            }
            if ((destinationType == typeof(InstanceDescriptor)) && value is Half)
            {
                var info = typeof(Half).GetConstructor(new Type[] { typeof(float) });
                if (info != null)
                {
                    return new InstanceDescriptor(info, new object[] { (float)((Half)value) });
                }
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
