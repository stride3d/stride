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

using System;
using Stride.Core.Reflection;

namespace Stride.Core.Yaml.Serialization.Serializers
{
    /// <summary>
    /// Default implementation for <see cref="IObjectSerializerBackend"/>
    /// </summary>
    public class DefaultObjectSerializerBackend : IObjectSerializerBackend
    {
        public virtual DataStyle GetStyle(ref ObjectContext objectContext)
        {
            var context = objectContext.SerializerContext;

            // Resolve the style, use default style if not defined.
            // First pop style of current member being serialized.
            var style = objectContext.Style;

            // If no style yet defined
            if (style != DataStyle.Any)
            {
                return style;
            }

            // Try to get the style from this serializer
            style = objectContext.Descriptor.Style;

            // In case of any style, allow to emit a flow sequence depending on Settings LimitPrimitiveFlowSequence.
            // Apply this only for primitives
            if (style == DataStyle.Any)
            {
                bool isPrimitiveElementType = false;
                var collectionDescriptor = objectContext.Descriptor as CollectionDescriptor;
                int count = 0;
                if (collectionDescriptor != null)
                {
                    isPrimitiveElementType = PrimitiveDescriptor.IsPrimitive(collectionDescriptor.ElementType);
                    count = collectionDescriptor.GetCollectionCount(objectContext.Instance);
                }
                else
                {
                    var arrayDescriptor = objectContext.Descriptor as ArrayDescriptor;
                    if (arrayDescriptor != null)
                    {
                        isPrimitiveElementType = PrimitiveDescriptor.IsPrimitive(arrayDescriptor.ElementType);
                        count = ((Array)objectContext.Instance)?.Length ?? -1;
                    }
                }

                style = objectContext.Instance == null || count >= objectContext.SerializerContext.Settings.LimitPrimitiveFlowSequence || !isPrimitiveElementType
                    ? DataStyle.Normal
                    : DataStyle.Compact;
            }

            // If not defined, get the default style
            if (style == DataStyle.Any)
            {
                style = context.Settings.DefaultStyle;

                // If default style is set to Any, set it to Block by default.
                if (style == DataStyle.Any)
                {
                    style = DataStyle.Normal;
                }
            }

            return style;
        }

        /// <inheritdoc/>
        public virtual string ReadMemberName(ref ObjectContext objectContext, string memberName, out bool skipMember)
        {
            skipMember = false;
            return memberName;
        }

        /// <inheritdoc/>
        public virtual object ReadMemberValue(ref ObjectContext objectContext, IMemberDescriptor memberDescriptor, object memberValue,
            Type memberType)
        {
            var memberObjectContext = new ObjectContext(objectContext.SerializerContext, memberValue, objectContext.SerializerContext.FindTypeDescriptor(memberType));
            return ReadYaml(ref memberObjectContext);
        }

        /// <inheritdoc/>
        public virtual object ReadCollectionItem(ref ObjectContext objectContext, object value, Type itemType, int index)
        {
            var itemObjectContext = new ObjectContext(objectContext.SerializerContext, value, objectContext.SerializerContext.FindTypeDescriptor(itemType));
            return ReadYaml(ref itemObjectContext);
        }

        /// <inheritdoc/>
        public virtual object ReadDictionaryKey(ref ObjectContext objectContext, Type keyType)
        {
            var keyObjectContext = new ObjectContext(objectContext.SerializerContext, null, objectContext.SerializerContext.FindTypeDescriptor(keyType));
            return ReadYaml(ref keyObjectContext);
        }

        /// <inheritdoc/>
        public virtual object ReadDictionaryValue(ref ObjectContext objectContext, Type valueType, object key)
        {
            var valueObjectContext = new ObjectContext(objectContext.SerializerContext, null, objectContext.SerializerContext.FindTypeDescriptor(valueType));
            return ReadYaml(ref valueObjectContext);
        }

        /// <inheritdoc/>
        public virtual void WriteMemberName(ref ObjectContext objectContext, IMemberDescriptor member, string name)
        {
            // Emit the key name
            objectContext.Writer.Emit(new ScalarEventInfo(name, typeof(string))
            {
                RenderedValue = name,
                IsPlainImplicit = true,
                Style = ScalarStyle.Plain
            });
        }

        /// <inheritdoc/>
        public virtual void WriteMemberValue(ref ObjectContext objectContext, IMemberDescriptor memberDescriptor, object memberValue, Type memberType)
        {
            // Push the style of the current member
            var memberObjectContext = new ObjectContext(objectContext.SerializerContext, memberValue, objectContext.SerializerContext.FindTypeDescriptor(memberType), objectContext.ParentTypeDescriptor, memberDescriptor)
            {
                Style = memberDescriptor.Style,
                ScalarStyle = memberDescriptor.ScalarStyle,
            };
            WriteYaml(ref memberObjectContext);
        }

        /// <inheritdoc/>
        public virtual void WriteCollectionItem(ref ObjectContext objectContext, object item, Type itemType, int index)
        {
            var itemObjectcontext = new ObjectContext(objectContext.SerializerContext, item, objectContext.SerializerContext.FindTypeDescriptor(itemType));
            WriteYaml(ref itemObjectcontext);
        }

        /// <inheritdoc/>
        public virtual void WriteDictionaryKey(ref ObjectContext objectContext, object key, Type keyType)
        {
            var itemObjectcontext = new ObjectContext(objectContext.SerializerContext, key, objectContext.SerializerContext.FindTypeDescriptor(keyType));
            WriteYaml(ref itemObjectcontext);
        }

        /// <inheritdoc/>
        public virtual void WriteDictionaryValue(ref ObjectContext objectContext, object key, object value, Type valueType)
        {
            var itemObjectcontext = new ObjectContext(objectContext.SerializerContext, value, objectContext.SerializerContext.FindTypeDescriptor(valueType));
            WriteYaml(ref itemObjectcontext);
        }

        /// <inheritdoc/>
        public virtual bool ShouldSerialize(IMemberDescriptor member, ref ObjectContext objectContext) => member.ShouldSerialize(objectContext.Instance, objectContext.ParentTypeMemberDescriptor);

        protected object ReadYaml(ref ObjectContext objectContext)
        {
            var node = objectContext.SerializerContext.Reader.Parser.Current;
            try
            {
                return objectContext.SerializerContext.Serializer.ObjectSerializer.ReadYaml(ref objectContext);
            }
            catch (YamlException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ex = ex.Unwrap();
                throw new YamlException(node, ex);
            }
        }

        protected void WriteYaml(ref ObjectContext objectContext)
        {
            objectContext.SerializerContext.Serializer.ObjectSerializer.WriteYaml(ref objectContext);
        }
    }
}
