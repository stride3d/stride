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
using System.Linq;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;
using Stride.Core.Yaml.Events;
using Scalar = Stride.Core.Yaml.Events.Scalar;

namespace Stride.Core.Yaml.Serialization.Serializers
{
    /// <summary>
    /// Class for serializing a <see cref="IDictionary{TKey,TValue}"/> or <see cref="System.Collections.IDictionary"/>
    /// </summary>
    [YamlSerializerFactory(YamlSerializerFactoryAttribute.Default)]
    public class DictionarySerializer : ObjectSerializer
    {
        /// <inheritdoc/>
        public override IYamlSerializable TryCreate(SerializerContext context, ITypeDescriptor typeDescriptor)
        {
            return typeDescriptor is DictionaryDescriptor ? this : null;
        }

        protected override void ReadMember(ref ObjectContext objectContext)
        {
            var dictionaryDescriptor = (DictionaryDescriptor)objectContext.Descriptor;

            if (dictionaryDescriptor.IsPureDictionary)
            {
                ReadDictionaryItems(ref objectContext);
            }
            else if (objectContext.Settings.SerializeDictionaryItemsAsMembers && dictionaryDescriptor.KeyType == typeof(string))
            {
                // Read dictionaries that can be serialized as items
                string memberName;
                if (!TryReadMember(ref objectContext, out memberName))
                {
                    var value = ReadMemberValue(ref objectContext, null, null, dictionaryDescriptor.ValueType);
                    dictionaryDescriptor.AddToDictionary(objectContext.Instance, memberName, value);
                }
            }
            else
            {
                var keyEvent = objectContext.Reader.Peek<Scalar>();
                if (keyEvent != null && keyEvent.Value == objectContext.Settings.SpecialCollectionMember)
                {
                    var reader = objectContext.Reader;
                    reader.Parser.MoveNext();

                    reader.Expect<MappingStart>();
                    ReadDictionaryItems(ref objectContext);
                    reader.Expect<MappingEnd>();
                    return;
                }

                base.ReadMember(ref objectContext);
            }
        }

        protected override void WriteMembers(ref ObjectContext objectContext)
        {
            var dictionaryDescriptor = (DictionaryDescriptor)objectContext.Descriptor;
            if (dictionaryDescriptor.IsPureDictionary)
            {
                WriteDictionaryItems(ref objectContext);
            }
            else if (objectContext.Settings.SerializeDictionaryItemsAsMembers && dictionaryDescriptor.KeyType == typeof(string))
            {
                // Serialize Dictionary members and items together
                foreach (var member in dictionaryDescriptor.Members)
                {
                    WriteMember(ref objectContext, member);
                }
                WriteDictionaryItems(ref objectContext);
            }
            else
            {
                // Serialize Dictionary members
                foreach (var member in dictionaryDescriptor.Members)
                {
                    WriteMember(ref objectContext, member);
                }

                WriteMemberName(ref objectContext, null, objectContext.Settings.SpecialCollectionMember);

                objectContext.Writer.Emit(new MappingStartEventInfo(objectContext.Instance, objectContext.Instance.GetType()) { Style = objectContext.Style });
                WriteDictionaryItems(ref objectContext);
                objectContext.Writer.Emit(new MappingEndEventInfo(objectContext.Instance, objectContext.Instance.GetType()));
            }
        }

        /// <summary>
        /// Reads the dictionary items key-values.
        /// </summary>
        /// <param name="objectContext"></param>
        protected virtual void ReadDictionaryItems(ref ObjectContext objectContext)
        {
            var dictionaryDescriptor = (DictionaryDescriptor)objectContext.Descriptor;

            var reader = objectContext.Reader;
            while (!reader.Accept<MappingEnd>())
            {
                var currentDepth = objectContext.Reader.CurrentDepth;
                var startParsingEvent = objectContext.Reader.Parser.Current;

                try
                {
                    // Read key and value
                    var keyValue = ReadDictionaryItem(ref objectContext, new KeyValuePair<Type, Type>(dictionaryDescriptor.KeyType, dictionaryDescriptor.ValueType));
                    try
                    {
                        dictionaryDescriptor.AddToDictionary(objectContext.Instance, keyValue.Key, keyValue.Value);
                    }
                    catch (Exception ex)
                    {
                        ex = ex.Unwrap();
                        throw new YamlException(reader.Parser.Current.Start, reader.Parser.Current.End, $"Cannot add item with key [{keyValue.Key}] to dictionary of type [{objectContext.Descriptor}]:\n{ex.Message}", ex);
                    }
                }
                catch (YamlException ex)
                {
                    if (objectContext.SerializerContext.AllowErrors)
                    {
                        var logger = objectContext.SerializerContext.Logger;
                        logger?.Warning($"{ex.Message}, this dictionary item will be ignored", ex);
                        objectContext.Reader.Skip(currentDepth, startParsingEvent == objectContext.Reader.Parser.Current);
                    }
                    else throw;
                }
            }
        }

        /// <summary>
        /// Reads a dictionary item key-value.
        /// </summary>
        /// <param name="objectContext">The object context.</param>
        /// <param name="keyValueTypes">The types corresponding to the key and the value.</param>
        /// <returns>A <see cref="KeyValuePair{Object, Object}"/> representing the dictionary item.</returns>
        protected virtual KeyValuePair<object, object> ReadDictionaryItem(ref ObjectContext objectContext, KeyValuePair<Type, Type> keyValueTypes)
        {
            var keyResult = objectContext.ObjectSerializerBackend.ReadDictionaryKey(ref objectContext, keyValueTypes.Key);
            var valueResult = objectContext.ObjectSerializerBackend.ReadDictionaryValue(ref objectContext, keyValueTypes.Value, keyResult);
            return new KeyValuePair<object, object>(keyResult, valueResult);
        }

        /// <summary>
        /// Writes the dictionary items keys-values.
        /// </summary>
        /// <param name="objectContext">The object context.</param>
        protected virtual void WriteDictionaryItems(ref ObjectContext objectContext)
        {
            var dictionaryDescriptor = (DictionaryDescriptor) objectContext.Descriptor;
            var keyValues = dictionaryDescriptor.GetEnumerator(objectContext.Instance).ToList();

            var settings = objectContext.Settings;
            if (settings.SortKeyForMapping && settings.ComparerForKeySorting != null)
            {
                var keyComparer = settings.ComparerForKeySorting;
                keyValues.Sort((left, right) => keyComparer.Compare(left.Key, right.Key));
            }

            var keyValueType = new KeyValuePair<Type, Type>(dictionaryDescriptor.KeyType, dictionaryDescriptor.ValueType);

            foreach (var keyValue in keyValues)
            {
                WriteDictionaryItem(ref objectContext, keyValue, keyValueType);
            }
        }

        /// <summary>
        /// Writes the dictionary item key-value.
        /// </summary>
        /// <param name="objectContext">The object context.</param>
        /// <param name="keyValue">The key value.</param>
        /// <param name="keyValueTypes">The types corresponding to the key and the value.</param>
        protected virtual void WriteDictionaryItem(ref ObjectContext objectContext, KeyValuePair<object, object> keyValue, KeyValuePair<Type, Type> keyValueTypes)
        {
            objectContext.ObjectSerializerBackend.WriteDictionaryKey(ref objectContext, keyValue.Key, keyValueTypes.Key);
            objectContext.ObjectSerializerBackend.WriteDictionaryValue(ref objectContext, keyValue.Key, keyValue.Value, keyValueTypes.Value);
        }
    }
}
