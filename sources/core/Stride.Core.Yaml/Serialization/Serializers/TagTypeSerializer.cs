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
using Stride.Core.Reflection;
using Stride.Core.Yaml.Events;

namespace Stride.Core.Yaml.Serialization.Serializers
{
    internal class TagTypeSerializer : ChainedSerializer
    {
        public override object ReadYaml(ref ObjectContext objectContext)
        {
            var parsingEvent = objectContext.Reader.Peek<ParsingEvent>();
            // Can this happen here?
            if (parsingEvent == null)
            {
                // TODO check how to put a location in this case?
                throw new YamlException("Unable to parse input");
            }

            var node = parsingEvent as NodeEvent;
            if (node == null)
            {
                throw new YamlException(parsingEvent.Start, parsingEvent.End, $"Unexpected parsing event found [{parsingEvent}]. Expecting Scalar, Mapping or Sequence");
            }

            var type = objectContext.Descriptor != null ? objectContext.Descriptor.Type : null;

            // Tries to get a Type from the TagTypes
            Type typeFromTag = null;
            if (!string.IsNullOrEmpty(node.Tag))
            {
                bool remapped;
                typeFromTag = objectContext.SerializerContext.TypeFromTag(node.Tag, out remapped);
                if (typeFromTag == null)
                {
                    throw new YamlException(parsingEvent.Start, parsingEvent.End, $"Unable to resolve tag [{node.Tag}] to type from tag resolution or registered assemblies");
                }

                // Store the fact that remap has occured on this tag
                if (remapped)
                {
                    objectContext.SerializerContext.HasRemapOccurred = true;
                }
            }

            // Use typeFromTag when type are different
            if (typeFromTag != null && type != typeFromTag)
                type = typeFromTag;

            // If type is null, use type from tag
            if (type == null)
                type = typeFromTag;

            object value = objectContext.Instance;

            // Handle explicit null scalar
            if (node is Scalar && objectContext.SerializerContext.Schema.TryParse((Scalar) node, typeof(object), out value))
            {
                // The value was pick up, go to next
                objectContext.Reader.Parser.MoveNext();
                return value;
            }

            // If type is null or equal to typeof(object) and value is null
            // and we have a node starting with a Sequence or Mapping
            // Set the type to accept IList<object> for sequences
            // or IDictionary<object, object> for mappings
            // This allow to load any YAML documents into dictionary/list
            // automatically
            if ((type == null || type == typeof(object)) && value == null)
            {
                // If the node is a sequence start, fallback to a IList<object>
                if (node is SequenceStart)
                {
                    type = typeof(IList<object>);
                }
                else if (node is MappingStart)
                {
                    // If the node is a mapping start, fallback to a IDictionary<object, object>
                    type = typeof(IDictionary<object, object>);
                }
            }

            if (type == null && value == null)
            {
                throw new YamlException(node.Start, node.End, $"Unable to find a type for this element [{node}]");
            }

            if (type == null)
            {
                type = value.GetType();
            }
            else if (typeFromTag != null && value != null && value.GetType() != typeFromTag)
            {
                // Reset the instance if the value loaded from the tag is not of the same type then the value already instantiated
                type = typeFromTag;
                objectContext.Instance = null;
            }

            objectContext.Descriptor = objectContext.SerializerContext.FindTypeDescriptor(type);

            // If this is a nullable descriptor, use its underlying type directly
            if (objectContext.Descriptor is NullableDescriptor)
            {
                objectContext.Descriptor = objectContext.SerializerContext.FindTypeDescriptor(((NullableDescriptor) objectContext.Descriptor).UnderlyingType);
            }
            return base.ReadYaml(ref objectContext);
        }

        public override void WriteYaml(ref ObjectContext objectContext)
        {
            var value = objectContext.Instance;

            // If value is null, then just output a plain null scalar
            if (value == null)
            {
                objectContext.Writer.Emit(new ScalarEventInfo(null, typeof(object)) {RenderedValue = "null", IsPlainImplicit = true, Style = ScalarStyle.Plain});
                return;
            }

            var typeOfValue = value.GetType();


            // If we have a nullable value, get its type directly and replace the descriptor
            if (objectContext.Descriptor is NullableDescriptor)
            {
                objectContext.Descriptor = objectContext.SerializerContext.FindTypeDescriptor(((NullableDescriptor) objectContext.Descriptor).UnderlyingType);
            }

            // Expected type 
            var expectedType = objectContext.Descriptor != null ? objectContext.Descriptor.Type : null;
            bool isAutoMapSeq = false;

            // Allow to serialize back to plain YAML !!map and !!seq if the expected type is an object
            // and the value is of the type Dictionary<object, object> or List<object>
            if (expectedType == typeof(object))
            {
                if (typeOfValue == typeof(Dictionary<object, object>) || typeOfValue == typeof(List<object>))
                {
                    isAutoMapSeq = true;
                }
            }

            // Auto !!map !!seq for collections/dictionaries
            var defaultImplementationType = DefaultObjectFactory.GetDefaultImplementation(expectedType);
            if (defaultImplementationType != null && defaultImplementationType == typeOfValue)
            {
                isAutoMapSeq = true;
            }

            // If this is an anonymous tag we will serialize only a default untyped YAML mapping
            var tag = typeOfValue.IsAnonymous() || typeOfValue == expectedType || isAutoMapSeq
                ? null
                : objectContext.SerializerContext.TagFromType(typeOfValue);

            // Set the tag
            objectContext.Tag = objectContext.Settings.EmitTags ? tag : null;

            // We will use the type of the value for the rest of the WriteYaml serialization
            objectContext.Descriptor = objectContext.SerializerContext.FindTypeDescriptor(typeOfValue);

            // Go next to the chain
            base.WriteYaml(ref objectContext);
        }
    }
}
