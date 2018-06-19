// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xenko.Core.Annotations;

namespace Xenko.Core.Reflection
{
    public static class CustomAttributeExtensions
    {
        public static T GetCustomAttributeEx<T>([NotNull] this Assembly assembly) where T : Attribute
        {
            return (T)GetCustomAttributeEx(assembly, typeof(T));
        }

        public static Attribute GetCustomAttributeEx([NotNull] this Assembly assembly, [NotNull] Type attributeType)
        {
#if XENKO_PLATFORM_MONO_MOBILE
            return Attribute.GetCustomAttribute(assembly, attributeType);
#else
            return assembly.GetCustomAttribute(attributeType);
#endif
        }

        public static IEnumerable<Attribute> GetCustomAttributesEx([NotNull] this Assembly assembly, [NotNull] Type attributeType)
        {
#if XENKO_PLATFORM_MONO_MOBILE
            return Attribute.GetCustomAttributes(assembly, attributeType);
#else
            return assembly.GetCustomAttributes(attributeType);
#endif
        }

        [NotNull]
        public static IEnumerable<T> GetCustomAttributesEx<T>([NotNull] this Assembly assembly) where T : Attribute
        {
            return GetCustomAttributesEx(assembly, typeof(T)).Cast<T>();
        }
    }
}
