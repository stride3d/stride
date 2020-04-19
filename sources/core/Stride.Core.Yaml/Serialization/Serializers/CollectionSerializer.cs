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
using System.Linq;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;
using Stride.Core.Yaml.Events;

namespace Stride.Core.Yaml.Serialization.Serializers
{
    /// <summary>
    /// Class for serializing a <see cref="System.Collections.Generic.ICollection{T}"/> or <see cref="System.Collections.ICollection"/>
    /// </summary>
    [YamlSerializerFactory(YamlSerializerFactoryAttribute.Default)]
    public class CollectionSerializer : ObjectSerializer
    {
        public override IYamlSerializable TryCreate(SerializerContext context, ITypeDescriptor typeDescriptor)
        {
            return typeDescriptor is CollectionDescriptor ? this : null;
        }

        protected override bool CheckIsSequence(ref ObjectContext objectContext)
        {
            var collectionDescriptor = (CollectionDescriptor) objectContext.Descriptor;

            // If the dictionary is pure, we can directly output a sequence instead of a mapping
            return collectionDescriptor.IsPureCollection;
        }

        protected override void ReadMember(ref ObjectContext objectContext)
        {
            if (CheckIsSequence(ref objectContext))
            {
                ReadCollectionItems(ref objectContext);
            }
            else
            {
                var keyEvent = objectContext.Reader.Peek<Scalar>();
                if (keyEvent != null)
                {
                    if (keyEvent.Value == objectContext.Settings.SpecialCollectionMember)
                    {
                        var reader = objectContext.Reader;
                        reader.Parser.MoveNext();

                        // Read inner sequence
                        reader.Expect<SequenceStart>();
                        ReadCollectionItems(ref objectContext);
                        reader.Expect<SequenceEnd>();
                        return;
                    }
                }

                base.ReadMember(ref objectContext);
            }
        }

        protected override void WriteMembers(ref ObjectContext objectContext)
        {
            if (CheckIsSequence(ref objectContext))
            {
                WriteCollectionItems(ref objectContext);
            }
            else
            {
                // Serialize Dictionary members
                foreach (var member in objectContext.Descriptor.Members)
                {
                    if (member.OriginalName == "Capacity" && !objectContext.Settings.EmitCapacityForList)
                    {
                        continue;
                    }

                    WriteMember(ref objectContext, member);
                }

                WriteMemberName(ref objectContext, null, objectContext.Settings.SpecialCollectionMember);

                objectContext.Writer.Emit(new SequenceStartEventInfo(objectContext.Instance, objectContext.Instance.GetType()) {Style = objectContext.Style});
                WriteCollectionItems(ref objectContext);
                objectContext.Writer.Emit(new SequenceEndEventInfo(objectContext.Instance, objectContext.Instance.GetType()));
            }
        }

        /// <summary>
        /// Reads the collection items.
        /// </summary>
        /// <param name="objectContext">The object context.</param>
        /// <exception cref="System.InvalidOperationException">Cannot deserialize list to type [{0}]. No Add method found.DoFormat(thisObject.GetType())
        /// or
        /// Cannot deserialize list to readonly collection type [{0}]..DoFormat(thisObject.GetType())</exception>
        protected virtual void ReadCollectionItems(ref ObjectContext objectContext)
        {
            var collectionDescriptor = (CollectionDescriptor) objectContext.Descriptor;
            var thisObject = objectContext.Instance;

            if (!collectionDescriptor.HasAdd)
            {
                throw new InvalidOperationException($"Cannot deserialize list to type [{thisObject.GetType()}]. No Add method found");
            }
            if (collectionDescriptor.IsReadOnly(thisObject))
            {
                throw new InvalidOperationException($"Cannot deserialize list to readonly collection type [{thisObject.GetType()}].");
            }

            var reader = objectContext.Reader;

            var elementType = collectionDescriptor.ElementType;
            var index = 0;
            while (!reader.Accept<SequenceEnd>())
            {
                var currentDepth = objectContext.Reader.CurrentDepth;
                var startParsingEvent = objectContext.Reader.Parser.Current;

                try
                {
                    ReadAddCollectionItem(ref objectContext, elementType, collectionDescriptor, thisObject, index);
                }
                catch (YamlException ex)
                {
                    if (objectContext.SerializerContext.AllowErrors)
                    {
                        var logger = objectContext.SerializerContext.Logger;
                        logger?.Warning($"Ignored collection item of type [{elementType}] that could not be deserialized:\n{ex.Message}", ex);
                        objectContext.Reader.Skip(currentDepth, startParsingEvent == objectContext.Reader.Parser.Current);
                    }
                    else throw;
                }
                index++;
            }
        }

        /// <summary>
        /// Reads and adds item to the collection.
        /// </summary>
        /// <param name="objectContext">The object context.</param>
        /// <param name="elementType">Type of the element.</param>
        /// <param name="collectionDescriptor">The collection descriptor.</param>
        /// <param name="thisObject">The this object.</param>
        /// <param name="index">The index.</param>
        protected virtual void ReadAddCollectionItem(ref ObjectContext objectContext, Type elementType, CollectionDescriptor collectionDescriptor, object thisObject, int index)
        {
            var value = ReadCollectionItem(ref objectContext, null, elementType, index);
            collectionDescriptor.Add(thisObject, value);
        }

        /// <summary>
        /// Reads a collection item.
        /// </summary>
        /// <param name="objectContext">The object context.</param>
        /// <param name="value">The value.</param>
        /// <param name="itemType">Type of the item.</param>
        /// <param name="index"></param>
        /// <returns>The item to add to the current collection.</returns>
        protected virtual object ReadCollectionItem(ref ObjectContext objectContext, object value, Type itemType, int index)
        {
            return objectContext.ObjectSerializerBackend.ReadCollectionItem(ref objectContext, value, itemType, index);
        }

        /// <summary>
        /// Writes the collection items.
        /// </summary>
        /// <param name="objectContext">The object context.</param>
        protected virtual void WriteCollectionItems(ref ObjectContext objectContext)
        {
            var collectionDescriptor = (CollectionDescriptor) objectContext.Descriptor;
            var collection = (IEnumerable) objectContext.Instance;
            int index = 0;
            foreach (var item in collection)
            {
                WriteCollectionItem(ref objectContext, item, collectionDescriptor.ElementType, index);
                index++;
            }
        }

        /// <summary>
        /// Writes the collection item.
        /// </summary>
        /// <param name="objectContext">The object context.</param>
        /// <param name="item">The item.</param>
        /// <param name="itemType">Type of the item.</param>
        /// <param name="index"></param>
        protected virtual void WriteCollectionItem(ref ObjectContext objectContext, object item, Type itemType, int index)
        {
            objectContext.ObjectSerializerBackend.WriteCollectionItem(ref objectContext, item, itemType, index);
        }
    }
}
