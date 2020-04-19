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
using Stride.Core.Yaml.Events;

namespace Stride.Core.Yaml.Serialization.Serializers
{
    public abstract class ScalarSerializerBase : IYamlSerializable
    {
        public object ReadYaml(ref ObjectContext objectContext)
        {
            var scalar = objectContext.Reader.Expect<Scalar>();
            return ConvertFrom(ref objectContext, scalar);
        }

        public abstract object ConvertFrom(ref ObjectContext context, Scalar fromScalar);

        public void WriteYaml(ref ObjectContext objectContext)
        {
            var value = objectContext.Instance;
            var typeOfValue = value.GetType();

            var context = objectContext.SerializerContext;

            var isSchemaImplicitTag = context.Schema.IsTagImplicit(objectContext.Tag);
            var scalar = new ScalarEventInfo(value, typeOfValue)
            {
                IsPlainImplicit = isSchemaImplicitTag,
                Style = objectContext.ScalarStyle,
                Anchor = objectContext.Anchor,
                Tag = objectContext.Tag,
            };


            if (scalar.Style == ScalarStyle.Any)
            {
                // Parse default types 
                switch (Type.GetTypeCode(typeOfValue))
                {
                    case TypeCode.Object:
                    case TypeCode.String:
                    case TypeCode.Char:
                        break;
                    default:
                        scalar.Style = ScalarStyle.Plain;
                        break;
                }
            }

            scalar.RenderedValue = ConvertTo(ref objectContext);

            // Emit the scalar
            WriteScalar(ref objectContext, scalar);
        }

        /// <summary>
        /// Writes the scalar to the <see cref="SerializerContext.Writer"/>. See remarks.
        /// </summary>
        /// <param name="objectContext">The object context.</param>
        /// <param name="scalar">The scalar.</param>
        /// <remarks>
        /// This method can be overloaded to replace the converted scalar just before writing it.
        /// </remarks>
        protected virtual void WriteScalar(ref ObjectContext objectContext, ScalarEventInfo scalar)
        {
            // Emit the scalar
            objectContext.SerializerContext.Writer.Emit(scalar);
        }

        public abstract string ConvertTo(ref ObjectContext objectContext);
    }
}