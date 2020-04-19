// Copyright (c) 2013 SharpYaml - Alexandre Mutel
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
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;
using Stride.Core.Yaml.Events;

namespace Stride.Core.Yaml.Serialization.Serializers
{
    /// <summary>
    /// Base class for serializing an object that can be a Yaml !!map or !!seq.
    /// </summary>
    [YamlSerializerFactory(YamlSerializerFactoryAttribute.Default)]
    public class ObjectSerializer : IYamlSerializable, IYamlSerializableFactory
    {
        /// <inheritdoc/>
        public virtual IYamlSerializable TryCreate(SerializerContext context, ITypeDescriptor typeDescriptor)
        {
            // always accept
            return this;
        }

        /// <summary>
        /// Checks if a type is a sequence.
        /// </summary>
        /// <param name="objectContext"></param>
        /// <returns><c>true</c> if a type is a sequence, <c>false</c> otherwise.</returns>
        protected virtual bool CheckIsSequence(ref ObjectContext objectContext)
        {
            // By default an object serializer is a mapping
            return false;
        }

        /// <summary>
        /// Gets the style that will be used to serialized the object.
        /// </summary>
        /// <param name="objectContext"></param>
        /// <returns>The <see cref="DataStyle"/> to use. Default is <see cref="IYamlTypeDescriptor.Style"/>.</returns>
        protected virtual DataStyle GetStyle(ref ObjectContext objectContext)
        {
            return objectContext.ObjectSerializerBackend.GetStyle(ref objectContext);
        }

        public virtual object ReadYaml(ref ObjectContext objectContext)
        {
            // Create or transform the value to deserialize
            // If the new value to serialize is not the same as the one we were expecting to serialize
            if (CreateOrTransformObjectInternal(ref objectContext))
            {
                // Route to serializer for converted type
                objectContext.SerializerContext.Serializer.RoutingSerializer.ReadYaml(ref objectContext);
            }
            // Get the object accessor for the corresponding class
            else if (CheckIsSequence(ref objectContext))
            {
                ReadMembers<SequenceStart, SequenceEnd>(ref objectContext);
            }
            else
            {
                ReadMembers<MappingStart, MappingEnd>(ref objectContext);
            }

            TransformObjectAfterRead(ref objectContext);

            // Process members
            return objectContext.Instance;
        }


        private bool CreateOrTransformObjectInternal(ref ObjectContext objectContext)
        {
            CreateOrTransformObject(ref objectContext);
            var newValue = objectContext.Instance;

            if (newValue != null && newValue.GetType() != objectContext.Descriptor.Type)
            {
                objectContext.Descriptor = objectContext.SerializerContext.FindTypeDescriptor(newValue.GetType());
                return true;
            }

            return false;
        }

        /// <summary>
        /// Overrides this method when deserializing/serializing an object that needs a special instantiation or transformation. By default, this is calling
        /// <see cref="IObjectFactory.Create" /> if the <see cref="ObjectContext.Instance" /> is null or returning <see cref="ObjectContext.Instance" />.
        /// </summary>
        /// <param name="objectContext">The object context.</param>
        /// <returns>A new instance of the object or <see cref="ObjectContext.Instance" /> if not null</returns>
        protected virtual void CreateOrTransformObject(ref ObjectContext objectContext)
        {
            if (objectContext.Instance == null)
            {
                objectContext.Instance = objectContext.SerializerContext.ObjectFactory.Create(objectContext.Descriptor.Type);
            }
        }

        /// <summary>
        /// Transforms the object after it has been read. This method is called after an object has been read and before returning the object to
        /// the deserialization process. See remarks for usage.
        /// </summary>
        /// <param name="objectContext"></param>
        /// <returns>The actual object deserialized. By default same as <see cref="ObjectContext.Instance"/>.</returns>
        /// <remarks>
        /// This method is usefull in conjunction with <see cref="CreateOrTransformObject"/>.
        /// For example, in the case of deserializing to an immutable member, where we need to call the constructor of a type instead of setting each of 
        /// its member, we can instantiate a mutable object in <see cref="CreateOrTransformObject"/>, receive the mutable object filled in 
        /// <see cref="TransformObjectAfterRead"/> and transform it back to an immutable object.
        /// </remarks>
        protected virtual void TransformObjectAfterRead(ref ObjectContext objectContext)
        {
        }

        /// <summary>
        /// Reads the members from the current stream.
        /// </summary>
        /// <typeparam name="TStart">The type of the t start.</typeparam>
        /// <typeparam name="TEnd">The type of the t end.</typeparam>
        /// <param name="objectContext"></param>
        /// <returns>Return the object being read, by default thisObject passed by argument.</returns>
        protected virtual void ReadMembers<TStart, TEnd>(ref ObjectContext objectContext)
            where TStart : NodeEvent
            where TEnd : ParsingEvent
        {
            var reader = objectContext.Reader;
            var start = reader.Expect<TStart>();

            // throws an exception while deserializing
            if (objectContext.Instance == null)
            {
                throw new YamlException(start.Start, start.End, $"Cannot instantiate an object for type [{objectContext.Descriptor}]");
            }

            while (!reader.Accept<TEnd>())
            {
                ReadMember(ref objectContext);
            }
            reader.Expect<TEnd>();
        }

        /// <summary>
        /// Reads an item of the object from the YAML flow (either a sequence item or mapping key/value item).
        /// </summary>
        /// <param name="objectContext"></param>
        /// <exception cref="YamlException">Unable to deserialize property [{0}] not found in type [{1}].DoFormat(propertyName, typeDescriptor)</exception>
        protected virtual void ReadMember(ref ObjectContext objectContext)
        {
            Scalar memberScalar;
            string memberName;
            if (!TryReadMember(ref objectContext, out memberScalar, out memberName))
            {
                throw new YamlException(memberScalar.Start, memberScalar.End, $"Unable to deserialize property [{memberName}] not found in type [{objectContext.Descriptor}]");
            }
        }

        /// <summary>
        /// Tries to read a member.
        /// </summary>
        /// <param name="objectContext">The object context.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <returns><c>true</c> if the member was successfully read, <c>false</c> otherwise.</returns>
        protected bool TryReadMember(ref ObjectContext objectContext, out string memberName)
        {
            Scalar scalar;
            return TryReadMember(ref objectContext, out scalar, out memberName);
        }

        /// <summary>
        /// Tries to read an item of the object from the YAML flow (either a sequence item or mapping key/value item).
        /// </summary>
        /// <param name="objectContext">The object context.</param>
        /// <param name="memberScalar">The member scalar.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="YamlException">Unable to deserialize property [{0}] not found in type [{1}].DoFormat(propertyName, typeDescriptor)</exception>
        public virtual bool TryReadMember(ref ObjectContext objectContext, out Scalar memberScalar, out string memberName)
        {
            memberScalar = null;
            memberName = null;

            var currentDepth = objectContext.Reader.CurrentDepth;

            // For a regular object, the key is expected to be a simple scalar
            memberScalar = objectContext.Reader.Expect<Scalar>();

            var currentEvent = objectContext.Reader.Parser.Current;
            bool result = true;

            bool skipMember = false;
            try
            {

                var memberResult = TryReadMemberCore(ref objectContext, memberScalar, out memberName);
                if (memberResult == ReadMemberState.Skip)
                {
                    skipMember = true;
                }
                else if (memberResult == ReadMemberState.Error)
                {
                    if (objectContext.SerializerContext.AllowErrors)
                    {
                        skipMember = true;
                    }
                    else
                    {
                        result = false;
                    }
                }
            }
            catch (YamlException ex)
            {
                if (objectContext.SerializerContext.AllowErrors)
                {
                    var logger = objectContext.SerializerContext.Logger;
                    logger?.Warning($"Ignored member [{objectContext.Descriptor?.Type.Name ?? "(Unknown)"}.{memberName ?? "(Unknown)"}] that could not be deserialized:\n{ex.Message}", ex);
                    skipMember = true;
                }
                else throw;
            }

            if (skipMember)
            {
                objectContext.Reader.Skip(currentDepth, currentEvent == objectContext.Reader.Parser.Current);
            }

            return result;
        }


        private enum ReadMemberState
        {
            Sucess,
            Error,
            Skip
        }

        private ReadMemberState TryReadMemberCore(ref ObjectContext objectContext, Scalar memberScalar, out string memberName)
        {
            bool skipMember;
            memberName = ReadMemberName(ref objectContext, memberScalar.Value, out skipMember);

            // Do we want to skip this member?
            if (skipMember)
            {
                return ReadMemberState.Skip;
            }

            var memberAccessor = objectContext.Descriptor.TryGetMember(memberName);

            // If the member was remapped, store this in the context
            if (objectContext.Descriptor.IsMemberRemapped(memberName))
            {
                objectContext.SerializerContext.HasRemapOccurred = true;
            }

            // Check that property exist before trying to access the descriptor
            if (memberAccessor == null)
            {
                return ReadMemberState.Error;
            }

            // Read the value according to the type
            var memberValue = memberAccessor.Mode == DataMemberMode.Content ? memberAccessor.Get(objectContext.Instance) : null;
            var memberType = memberAccessor.Type;

            // In case of serializing a property/field which is not writeable
            // we need to change the expected type to the actual type of the 
            // content value
            if (memberValue != null)
                memberType = memberValue.GetType();

            var oldMemberValue = memberValue;
            memberValue = ReadMemberValue(ref objectContext, memberAccessor, memberValue, memberType);

            // Handle late binding
            // Value types need to be reassigned even if it was a Content
            if (memberAccessor.HasSet && (memberAccessor.Mode != DataMemberMode.Content || memberAccessor.Type.IsValueType || memberValue != oldMemberValue))
            {
                try
                {
                    memberAccessor.Set(objectContext.Instance, memberValue);
                }
                catch (Exception ex)
                {
                    ex = ex.Unwrap();
                    throw new YamlException(objectContext.Reader.Parser.Current.Start, objectContext.Reader.Parser.Current.End, $"Cannot set member [{objectContext.Descriptor?.Type.Name ?? "(Unknown)"}.{memberName ?? "(Unknown)"}]:\n{ex.Message}");
                }
            }

            return ReadMemberState.Sucess;
        }

        protected virtual string ReadMemberName(ref ObjectContext objectContext, string memberName, out bool skipMember)
        {
            return objectContext.ObjectSerializerBackend.ReadMemberName(ref objectContext, memberName, out skipMember);
        }

        protected virtual object ReadMemberValue(ref ObjectContext objectContext, IMemberDescriptor member,
            object memberValue,
            Type memberType)
        {
            return objectContext.ObjectSerializerBackend.ReadMemberValue(ref objectContext, member, memberValue, memberType);
        }

        /// <inheritdoc/>
        public virtual void WriteYaml(ref ObjectContext objectContext)
        {
            var value = objectContext.Instance;
            var typeOfValue = value.GetType();

            var isSequence = CheckIsSequence(ref objectContext);

            // Allow to create on the fly an object that will be used to serialize an object
            if (CreateOrTransformObjectInternal(ref objectContext))
            {
                // Route again
                objectContext.SerializerContext.Serializer.RoutingSerializer.WriteYaml(ref objectContext);
                return;
            }

            // Resolve the style, use default style if not defined.
            var style = GetStyle(ref objectContext);

            var context = objectContext.SerializerContext;
            if (isSequence)
            {
                context.Writer.Emit(new SequenceStartEventInfo(value, typeOfValue) {Tag = objectContext.Tag, Anchor = objectContext.Anchor, Style = style});
                WriteMembers(ref objectContext);
                context.Writer.Emit(new SequenceEndEventInfo(value, typeOfValue));
            }
            else
            {
                context.Writer.Emit(new MappingStartEventInfo(value, typeOfValue) {Tag = objectContext.Tag, Anchor = objectContext.Anchor, Style = style});
                WriteMembers(ref objectContext);
                context.Writer.Emit(new MappingEndEventInfo(value, typeOfValue));
            }
        }

        /// <summary>
        /// Writes the members of the object to serialize. By default this method is iterating on the <see cref="IYamlTypeDescriptor.Members"/> and
        /// calling <see cref="WriteMember"/> on each member.
        /// </summary>
        /// <param name="objectContext"></param>
        protected virtual void WriteMembers(ref ObjectContext objectContext)
        {
            foreach (var member in objectContext.Descriptor.Members)
            {
                WriteMember(ref objectContext, member);
            }
        }

        /// <summary>
        /// Writes a member.
        /// </summary>
        /// <param name="objectContext"></param>
        /// <param name="member">The member.</param>
        protected virtual void WriteMember(ref ObjectContext objectContext, IMemberDescriptor member)
        {
            // Filter members by mask
            if ((member.Mask & objectContext.SerializerContext.MemberMask) == 0)
                return;

            // Skip any member that we won't serialize
            if (!objectContext.SerializerContext.ObjectSerializerBackend.ShouldSerialize(member, ref objectContext))
                return;

            // Emit the key name
            WriteMemberName(ref objectContext, member, member.Name);

            var memberValue = member.Get(objectContext.Instance);
            var memberType = member.Type;

            // In case of serializing a property/field which is not writeable
            // we need to change the expected type to the actual type of the 
            // content value
            if (member.Mode == DataMemberMode.Content && !member.HasSet)
            {
                if (memberValue != null)
                {
                    memberType = memberValue.GetType();
                }
            }

            // Write the member value
            WriteMemberValue(ref objectContext, member, memberValue, memberType);
        }

        protected virtual void WriteMemberName(ref ObjectContext objectContext, IMemberDescriptor member, string name)
        {
            objectContext.ObjectSerializerBackend.WriteMemberName(ref objectContext, member, name);
        }

        protected virtual void WriteMemberValue(ref ObjectContext objectContext, IMemberDescriptor member, object memberValue,
            Type memberType)
        {
            objectContext.ObjectSerializerBackend.WriteMemberValue(ref objectContext, member, memberValue, memberType);
        }
    }
}
