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
using System.Collections;
using System.Collections.Generic;
using Stride.Core.Reflection;

namespace Stride.Core.Yaml.Serialization
{
    /// <summary>
    /// Creates objects using Activator.CreateInstance.
    /// </summary>
    public sealed class DefaultObjectFactory : IObjectFactory
    {
        private static readonly Type[] EmptyTypes = new Type[0];

        private static readonly Dictionary<Type, Type> DefaultInterfaceImplementations = new Dictionary<Type, Type>
        {
            {typeof(IList), typeof(List<object>)},
            {typeof(IDictionary), typeof(Dictionary<object, object>)},
            {typeof(IEnumerable<>), typeof(List<>)},
            {typeof(ICollection<>), typeof(List<>)},
            {typeof(IList<>), typeof(List<>)},
            {typeof(IDictionary<,>), typeof(Dictionary<,>)},
        };

        /// <summary>
        /// Gets the default implementation for a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The type of the implem or the same type as input if there is no default implementation</returns>
        public static Type GetDefaultImplementation(Type type)
        {
            if (type == null)
                return null;

            // TODO change this code. Make it configurable?
            if (type.IsInterface)
            {
                Type implementationType;
                if (type.IsGenericType)
                {
                    if (DefaultInterfaceImplementations.TryGetValue(type.GetGenericTypeDefinition(), out implementationType))
                    {
                        type = implementationType.MakeGenericType(type.GetGenericArguments());
                    }
                }
                else
                {
                    if (DefaultInterfaceImplementations.TryGetValue(type, out implementationType))
                    {
                        type = implementationType;
                    }
                }
            }
            return type;
        }

        public object Create(Type type)
        {
            type = GetDefaultImplementation(type);

            // We can't instantiate primitive or arrays
            if (PrimitiveDescriptor.IsPrimitive(type) || type.IsArray)
                return null;

            if (type.GetConstructor(EmptyTypes) != null || type.IsValueType)
            {
                try
                {
                    return Activator.CreateInstance(type);
                }
                catch (Exception e)
                {
                    throw new InstanceCreationException($"'{typeof(Activator)}' failed to create instance of type '{type}', see inner exception.", e);
                }
            }

            return null;
        }

        public class InstanceCreationException : Exception
        {
            public InstanceCreationException(string message, Exception innerException) : base(message, innerException) { }
        }
    }
}
