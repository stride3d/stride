// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Serialization;
using Stride.Core.UnsafeExtensions;

namespace Stride.Rendering
{
    /// <summary>
    /// Manage several effect parameters (resources and data). A specific data and resource layout can be forced (usually by the consuming effect).
    /// </summary>
    [DataSerializerGlobal(null, typeof(List<ParameterKeyInfo>))]
    [DataSerializer(typeof(Serializer))]
    [DebuggerTypeProxy(typeof(DebugView))]
    public partial class ParameterCollection
    {
        // Important! Alignment must be a power of 2
        private const int Alignment = 16;
        private const int AlignmentMask = Alignment - 1;

        // TODO: Switch to FastListStruct (for serialization)
        private List<ParameterKeyInfo> parameterKeyInfos = new(4);

        // Constants and resources

        [DataMemberIgnore]
        public ReadOnlySpan<byte> DataValues => dataValues;
        private byte[] dataValues = [];

        [DataMemberIgnore]
        public ReadOnlySpan<object> ObjectValues => objectValues;
        private object[] objectValues;

        [DataMemberIgnore]
        public int PermutationCounter { get; private set; } = 1; // TODO: Shoud be named PermutationVersion, as it is to know when the permutation has changed

        [DataMemberIgnore]
        public int LayoutCounter { get; private set; } = 1;  // TODO: Shoud be named LayoutVersion, as it is to know when the layout has changed

        [DataMemberIgnore]
        public List<ParameterKeyInfo> ParameterKeyInfos => parameterKeyInfos;

        [DataMemberIgnore]
        public ParameterCollectionLayout? Layout { get; private set; }

        [DataMemberIgnore]
        public bool HasLayout => Layout is not null;


        public ParameterCollection() { }

        public ParameterCollection(ParameterCollection parameterCollection)
        {
            // Copy layout
            if (parameterCollection.HasLayout)
            {
                Layout = parameterCollection.Layout;
            }

            // Copy parameter keys
            parameterKeyInfos = [.. parameterCollection.parameterKeyInfos];

            // Copy objects
            if (!parameterCollection.ObjectValues.IsEmpty)
            {
                objectValues = [.. parameterCollection.objectValues];
            }

            // Copy data
            if (!parameterCollection.DataValues.IsEmpty)
            {
                dataValues = [.. parameterCollection.dataValues];
            }
        }

        /// <summary>Increases <paramref name="value"/> to the next multiple of <see cref="Alignment"/>,
        /// if it is not already a multiple.
        /// <para>It is blindly assumed <see cref="Alignment"/> is a power of 2.</para></summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <summary>Computes the buffer size required for a single parameter value
        /// or an array of parameters of the same type.
        /// <para>This is an incremental size for buffers that contain
        /// more than one type of parameter.</para></summary>
        /// <returns>The size of a buffer required to store <paramref name="elementCount"/>
        /// elements of size <paramref name="elementSize"/>, including padding to
        /// <see cref="Alignment"/> for all but the last element.</returns>
        private static int Align(int value)
        {
            Debug.Assert(int.IsPow2(Alignment), "Alignment must be a power of 2");

            return (value + AlignmentMask) & ~AlignmentMask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ComputeAlignedSizeMinusTrailingPadding(int elementSize, int elementCount)
        {
            var result = elementSize;

            if (--elementCount != 0)
                result += Align(elementSize) * elementCount;

            return result;
        }

        public override string ToString()
        {
            var parameterKeysByType = ParameterKeyInfos
                .GroupBy(x => x.Key.Type)
                .Select(x => $"{x.Count()} {x.Key}(s)");

            return $"ParameterCollection: {string.Join(", ", parameterKeysByType)}";
        }

        #region Accessors

        /// <summary>
        /// Gets an accessor to get and set objects more quickly.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameterKey"></param>
        /// <returns></returns>
        public ObjectParameterAccessor<T> GetAccessor<T>(ObjectParameterKey<T> parameterKey, bool createIfNew = true)
        {
            var accessor = GetObjectParameterHelper(parameterKey, createIfNew);
            return new ObjectParameterAccessor<T>(accessor.Offset, accessor.Count);
        }

        /// <summary>
        /// Gets an accessor to get and set permutations more quickly.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameterKey"></param>
        /// <returns></returns>
        public PermutationParameterAccessor<T> GetAccessor<T>(PermutationParameterKey<T> parameterKey, bool createIfNew = true)
        {
            // Remap it as PermutationParameter
            var accessor = GetObjectParameterHelper(parameterKey, createIfNew);
            return new PermutationParameterAccessor<T>(accessor.Offset, accessor.Count);
        }

        /// <summary>
        /// Gets an accessor to get and set blittable values more quickly.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameterKey"></param>
        /// <returns></returns>
        public ValueParameterAccessor<T> GetAccessor<T>(ValueParameterKey<T> parameterKey, int elementCount = 1) where T : unmanaged
        {
            var accessor = GetValueAccessorHelper(parameterKey, elementCount);
            return new ValueParameterAccessor<T>(accessor.Offset, accessor.Count);
        }

        protected ParameterAccessor GetObjectParameterHelper(ParameterKey parameterKey, bool createIfNew = true)
        {
            ArgumentNullException.ThrowIfNull(parameterKey);

            // Find existing first
            var parameterKeyInfosSpan = CollectionsMarshal.AsSpan(parameterKeyInfos);
            for (int i = 0; i < parameterKeyInfosSpan.Length; ++i)
            {
                if (parameterKeyInfosSpan[i].Key == parameterKey)
                {
                    return parameterKeyInfosSpan[i].GetObjectAccessor();
                }
            }

            if (!createIfNew)
                return ParameterAccessor.Invalid;

            if (parameterKey.Type == ParameterKeyType.Permutation)
                PermutationCounter++;

            LayoutCounter++;

            // Check layout if it exists
            if (Layout is not null)
            {
                foreach (var layoutParameterKeyInfo in Layout.LayoutParameterKeyInfos)
                {
                    if (layoutParameterKeyInfo.Key == parameterKey)
                    {
                        parameterKeyInfos.Add(layoutParameterKeyInfo);
                        return layoutParameterKeyInfo.GetObjectAccessor();
                    }
                }
            }

            // Create info entry
            var resourceValuesSize = objectValues?.Length ?? 0;
            Array.Resize(ref objectValues, resourceValuesSize + 1);
            parameterKeyInfos.Add(new ParameterKeyInfo(parameterKey, resourceValuesSize));

            // Initialize default value
            if (parameterKey.DefaultValueMetadata is not null)
            {
                objectValues[resourceValuesSize] = parameterKey.DefaultValueMetadata.GetDefaultValue();
            }

            return new ParameterAccessor(resourceValuesSize, 1);
        }

        private unsafe ParameterAccessor GetValueAccessorHelper(ParameterKey parameterKey, int elementCount = 1)
        {
            ArgumentNullException.ThrowIfNull(parameterKey);

            // Try to find an existing parameter key first and return its accessor
            var parameterKeyInfosSpan = CollectionsMarshal.AsSpan(parameterKeyInfos);
            for (int i = 0; i < parameterKeyInfosSpan.Length; ++i)
            {
                if (parameterKeyInfosSpan[i].Key == parameterKey)
                {
                    return parameterKeyInfosSpan[i].GetValueAccessor();
                }
            }

            LayoutCounter++;

            // Check layout if it exists
            if (Layout is not null)
            {
                foreach (var layoutParameterKeyInfo in Layout.LayoutParameterKeyInfos)
                {
                    if (layoutParameterKeyInfo.Key == parameterKey)
                    {
                        parameterKeyInfos.Add(layoutParameterKeyInfo);
                        return layoutParameterKeyInfo.GetValueAccessor();
                    }
                }
            }

            // Compute size
            var additionalSize = ComputeAlignedSizeMinusTrailingPadding(parameterKey.Size, elementCount);

            // Create offset entry
            var memberOffset = dataValues.Length;
            parameterKeyInfos.Add(new ParameterKeyInfo(parameterKey, memberOffset, elementCount));

            // We append at the end; resize array to accomodate new data
            Array.Resize(ref dataValues, dataValues.Length + additionalSize);

            // Initialize default value
            if (parameterKey.DefaultValueMetadata is not null)
            {
                fixed (byte* dataValuesPtr = dataValues)
                    parameterKey.DefaultValueMetadata.WriteValue((IntPtr) dataValuesPtr + memberOffset, Alignment);
            }

            return new ParameterAccessor(memberOffset, elementCount);
        }

        #endregion

        /// <summary>
        /// Sets an object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameter"></param>
        /// <param name="value"></param>
        public void Set<T>(ObjectParameterKey<T> parameterKey, T value)
        {
            Set(GetAccessor(parameterKey), value);
        }

        /// <summary>
        /// Gets an object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public T Get<T>(ObjectParameterKey<T> parameterKey, bool createIfNew = false)
        {
            var accessor = GetAccessor(parameterKey, createIfNew);
            if (accessor.BindingSlot == ParameterKeyInfo.Invalid)
                return parameterKey.DefaultValueMetadataT.DefaultValue;

            return Get(accessor);
        }

        /// <summary>
        /// Sets a permutation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameter"></param>
        /// <param name="value"></param>
        public void Set<T>(PermutationParameterKey<T> parameterKey, T value)
        {
            Set(GetAccessor(parameterKey), value);
        }

        /// <summary>
        /// Gets a permutation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public T Get<T>(PermutationParameterKey<T> parameterKey, bool createIfNew = false)
        {
            var accessor = GetAccessor(parameterKey, createIfNew);
            if (accessor.BindingSlot == ParameterKeyInfo.Invalid)
                return parameterKey.DefaultValueMetadataT.DefaultValue;

            return Get(accessor);
        }

        /// <summary>
        /// Sets a blittable value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameter"></param>
        /// <param name="value"></param>
        public void Set<T>(ValueParameterKey<T> parameterKey, T value) where T : unmanaged
        {
            Set(GetAccessor(parameterKey), value);
        }

        public void Set<T>(ValueParameterKey<T> parameterKey, ref readonly T value) where T : unmanaged
        {
            Set(GetAccessor(parameterKey), in value);
        }

        /// <summary>
        /// Sets a blittable value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameter"></param>
        /// <param name="value"></param>
        public void Set<T>(ValueParameterKey<T> parameterKey, ReadOnlySpan<T> values) where T : unmanaged
        {
            Set(GetAccessor(parameterKey, values.Length), values.Length, in values[0]);
        }

        /// <summary>
        /// Sets blittable values.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameter"></param>
        /// <param name="values"></param>
        public void Set<T>(ValueParameterKey<T> parameterKey, T[] values) where T : unmanaged
        {
            Set(GetAccessor(parameterKey, values.Length), values.Length, ref values.GetReference());
        }

        /// <summary>
        /// Sets blittable values.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameter"></param>
        /// <param name="values"></param>
        public void Set<T>(ValueParameterKey<T> parameterKey, int count, ref readonly T firstValue) where T : unmanaged
        {
            Set(GetAccessor(parameterKey, count), count, in firstValue);
        }

        /// <summary>
        /// Gets a blittable value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public T Get<T>(ValueParameterKey<T> parameterKey) where T : unmanaged
        {
            return Get(GetAccessor(parameterKey));
        }

        /// <summary>
        /// Gets blittable values.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        private unsafe void GetValues<T>(ValueParameterAccessor<T> parameter, Span<T> valuesSpan) where T : unmanaged
        {
            if (valuesSpan.Length < parameter.Count)
            {
                throw new ArgumentException($"The provided span is not large enough to hold {parameter.Count} values of type {typeof(T).Name}.", nameof(valuesSpan));
            }

            scoped ref byte dataRef = ref MemoryMarshal.GetArrayDataReference(dataValues);
            dataRef = ref Unsafe.Add(ref dataRef, parameter.Offset);

            scoped ReadOnlySpan<byte> dataSpan = MemoryMarshal.CreateReadOnlySpan(ref dataRef, parameter.Count);

            // Align to float4
            var stride = Align(sizeof(T));

            for (int i = 0; i < valuesSpan.Length; ++i)
            {
                valuesSpan[i] = Unsafe.ReadUnaligned<T>(ref dataRef);
                dataRef = ref Unsafe.Add(ref dataRef, stride);
            }
        }

        public unsafe void GetValues<T>(ValueParameterKey<T> parameterKey, Span<T> valuesSpan) where T : unmanaged
        {
            var parameter = GetAccessor(parameterKey);

            GetValues(parameter, valuesSpan);
        }

        /// <summary>
        /// Copies all blittable values of a given key to the specified <see cref="ParameterCollection"/>.
        public unsafe T[] GetValues<T>(ValueParameterKey<T> parameterKey) where T : unmanaged
        {
            var parameter = GetAccessor(parameterKey);

            var values = new T[parameter.Count];
            GetValues(parameter, values);

            return values;
        }

        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key for the values to copy.</param>
        /// <param name="destination">The collection to copy the values to.</param>
        /// <param name="destinationKey">The key for the values of the destination collection.</param>
        public unsafe void CopyTo<T>(ValueParameterKey<T> sourceParameterKey,
                                     ParameterCollection destination, ValueParameterKey<T> destinationParameterKey)
            where T : unmanaged
        {
            ArgumentNullException.ThrowIfNull(destination);

            var sourceParameter = GetAccessor(sourceParameterKey);
            var destParameter = destination.GetAccessor(destinationParameterKey, sourceParameter.Count);

            if (sourceParameter.Count > destParameter.Count)
            {
                throw new ArgumentException(
                    $"The source parameter '{sourceParameterKey}' has {sourceParameter.Count} elements, " +
                    $"but the destination parameter '{destinationParameterKey}' can only hold {destParameter.Count} elements.",
                    nameof(destinationParameterKey));
            }

            // Align to float4
            var sizeInBytes = ComputeAlignedSizeMinusTrailingPadding(sizeof(T), sourceParameter.Count);
            Debug.Assert(
                (destParameter.Offset | sourceParameter.Offset | sizeInBytes) >= 0 &&
                (uint) sourceParameter.Offset + (uint) sizeInBytes <= (uint) (dataValues?.Length ?? 0) &&
                (uint) destParameter.Offset + (uint) sizeInBytes <= (uint) (destination.dataValues?.Length ?? 0));

            scoped ReadOnlySpan<byte> sourceDataValues = dataValues.AsSpan(start: sourceParameter.Offset);
            scoped Span<byte> destDataValues = destination.dataValues.AsSpan(start: destParameter.Offset);

            sourceDataValues.CopyTo(destDataValues);
        }

        /// <summary>
        /// Sets a blittable value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameter"></param>
        /// <param name="value"></param>
        public unsafe void Set<T>(ValueParameterAccessor<T> parameter, T value) where T : unmanaged
        {
            Debug.Assert(parameter.Offset <= dataValues.Length - sizeof(T));

            Unsafe.WriteUnaligned(ref dataValues[parameter.Offset], value);
        }

        /// <summary>
        /// Sets a blittable value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameter"></param>
        /// <param name="value"></param>
        public unsafe void Set<T>(ValueParameterAccessor<T> parameter, ref readonly T value) where T : unmanaged
        {
            Debug.Assert(parameter.Offset + sizeof(T) <= dataValues.Length);

            Unsafe.WriteUnaligned(ref dataValues[parameter.Offset], value);
        }

        /// <summary>
        /// Sets blittable values.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameter"></param>
        /// <param name="count"></param>
        /// <param name="firstValue"></param>
        public unsafe void Set<T>(ValueParameterAccessor<T> parameter, int count, ref readonly T firstValue) where T : unmanaged
        {
            int bufferSize = dataValues.Length;
            Debug.Assert(parameter.Offset < bufferSize, $"The offset {parameter.Offset:X} is out of bounds! (Buffer size: {bufferSize})");

            // Align to float4 (16 bytes)
            var stride = Align(sizeof(T));

            Debug.Assert(parameter.Offset + ComputeAlignedSizeMinusTrailingPadding(sizeof(T), count) <= dataValues.Length);
            Debug.Assert(parameter.Offset + ComputeAlignedSizeMinusTrailingPadding(sizeof(T), count) <= bufferSize,
                         $"The data will overrun the buffer size! (Offset: {parameter.Offset:X}, " +
                         $"Size: {ComputeAlignedSizeMinusTrailingPadding(sizeof(T), count)}, Buffer size: {bufferSize})");

            if (count > parameter.Count)
            {
                throw new ArgumentException($"The count {count} exceeds the maximum allowed count {parameter.Count} for the parameter.", nameof(count));
            }

            scoped ref var dataRef = ref dataValues[parameter.Offset];
            scoped ref var firstValueRef = ref Unsafe.AsRef(in firstValue);

            for (var i = 0; i < count; i++)
            {
                Unsafe.WriteUnaligned(ref dataRef, firstValueRef);

                dataRef = ref Unsafe.Add(ref dataRef, stride);
                firstValueRef = ref Unsafe.Add(ref firstValueRef, 1);
            }
        }

        /// <summary>
        /// Sets a permutation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameter"></param>
        /// <param name="value"></param>
        public void Set<T>(PermutationParameterAccessor<T> parameter, T value)
        {
            Debug.Assert(parameter.BindingSlot < objectValues.Length);

            bool isSame = EqualityComparer<T>.Default.Equals((T) objectValues[parameter.BindingSlot], value);
            if (!isSame)
            {
                PermutationCounter++;
            }

            // For value types, we don't assign again because this causes boxing
            if (!typeof(T).IsValueType || !isSame)
            {
                objectValues[parameter.BindingSlot] = value;
            }
        }

        /// <summary>
        /// Sets an object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameterAccessor"></param>
        /// <param name="value"></param>
        public void Set<T>(ObjectParameterAccessor<T> parameter, T value)
        {
            Debug.Assert(parameter.BindingSlot < objectValues.Length);

            objectValues[parameter.BindingSlot] = value;
        }

        /// <summary>
        /// Gets a value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public unsafe T Get<T>(ValueParameterAccessor<T> parameter) where T : unmanaged
        {
            Debug.Assert(parameter.Offset + sizeof(T) <= dataValues.Length);

            return Unsafe.ReadUnaligned<T>(ref dataValues[parameter.Offset]);
        }

        /// <summary>
        /// Gets a permutation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public T Get<T>(PermutationParameterAccessor<T> parameter)
        {
            Debug.Assert(parameter.BindingSlot < objectValues.Length);

            return (T) objectValues[parameter.BindingSlot];
        }

        /// <summary>
        /// Gets an object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameterAccessor"></param>
        /// <returns></returns>
        public T Get<T>(ObjectParameterAccessor<T> parameter)
        {
            Debug.Assert(parameter.BindingSlot < objectValues.Length);

            return (T) objectValues[parameter.BindingSlot];
        }

        public void SetObject(ParameterKey parameterKey, object? value)
        {
            ArgumentNullException.ThrowIfNull(parameterKey);

            if (parameterKey.Type is not ParameterKeyType.Permutation and not ParameterKeyType.Object)
                throw new ArgumentException("The parameter key must be of type Permutation or Object", nameof(parameterKey));

            var accessor = GetObjectParameterHelper(parameterKey);

            if (parameterKey.Type == ParameterKeyType.Permutation)
            {
                var oldValue = objectValues[accessor.Offset];
                if ((oldValue is not null && (value is null || !oldValue.Equals(value))) // oldValue non-null => Check equality
                    || (oldValue is null && value is not null))                          // oldValue null => Check if value too
                {
                    PermutationCounter++;
                }
            }
            objectValues[accessor.Offset] = value;
        }

        public object? GetObject(ParameterKey parameterKey) //------------------------ Return null if not found
        {
            ArgumentNullException.ThrowIfNull(parameterKey);

            if (parameterKey.Type is not ParameterKeyType.Permutation and not ParameterKeyType.Object)
                throw new ArgumentException("The parameter key must be of type Permutation or Object", nameof(parameterKey));

            var accessor = GetObjectParameterHelper(parameterKey, createIfNew: false);
            if (accessor.Offset == ParameterKeyInfo.Invalid)
                return null;

            return objectValues[accessor.Offset];
        }

        public bool Remove(ParameterKey parameterKey)
        {
            ArgumentNullException.ThrowIfNull(parameterKey);

            for (int i = 0; i < parameterKeyInfos.Count; ++i)
            {
                if (parameterKeyInfos[i].Key == parameterKey)
                {
                    parameterKeyInfos.SwapRemoveAt(i);
                    LayoutCounter++;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Clears the collection, including the layout.
        /// </summary>
        public void Clear()
        {
            dataValues = [];
            objectValues = null;
            Layout = null;
            parameterKeyInfos.Clear();
        }

        /// <summary>
        /// Determines whether current collection contains a value for this key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(ParameterKey parameterKey)
        {
            ArgumentNullException.ThrowIfNull(parameterKey);

            for (int i = 0; i < parameterKeyInfos.Count; ++i)
            {
                if (parameterKeyInfos[i].Key == parameterKey)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Reorganizes internal data and resources to match the given objects, and append extra values at the end.
        /// </summary>
        /// <param name="collectionLayout"></param>
        public unsafe void UpdateLayout(ParameterCollectionLayout collectionLayout)
        {
            var oldLayout = Layout;
            Layout = collectionLayout;

            // Same layout, or removed layout
            if (oldLayout == collectionLayout || collectionLayout is null)
                return;

            var layoutParameterKeyInfos = collectionLayout.LayoutParameterKeyInfos;

            // Do a first pass to measure Constant Buffer size
            var newParameterKeyInfos = new List<ParameterKeyInfo>(capacity: Math.Max(1, parameterKeyInfos.Count));
            newParameterKeyInfos.AddRange(parameterKeyInfos);
            var newParameterKeyInfosSpan = CollectionsMarshal.AsSpan(newParameterKeyInfos);
            var processedParameters = new bool[parameterKeyInfos.Count];

            var bufferSize = collectionLayout.BufferSize;
            var resourceCount = collectionLayout.ResourceCount;

            foreach (var layoutParameterKeyInfo in layoutParameterKeyInfos)
            {
                // Find the same parameter in the old collection
                // Is this parameter already added?
                for (int i = 0; i < parameterKeyInfos.Count; ++i)
                {
                    if (parameterKeyInfos[i].Key == layoutParameterKeyInfo.Key)
                    {
                        processedParameters[i] = true;
                        newParameterKeyInfosSpan[i] = layoutParameterKeyInfo;
                        break;
                    }
                }
            }

            // Append new elements that don't exist in new layouts (to preserve their values)
            for (int i = 0; i < processedParameters.Length; ++i)
            {
                // Skip parameters already processed before
                if (processedParameters[i])
                    continue;

                var parameterKeyInfo = newParameterKeyInfosSpan[i];

                if (parameterKeyInfo.IsValueParameter)
                {
                    newParameterKeyInfosSpan[i].Offset = bufferSize;

                    var additionalSize = ComputeAlignedSizeMinusTrailingPadding(
                        elementSize: newParameterKeyInfosSpan[i].Key.Size,
                        newParameterKeyInfosSpan[i].Count);

                    bufferSize += additionalSize;
                }
                else if (parameterKeyInfo.IsResourceParameter)
                {
                    newParameterKeyInfosSpan[i].BindingSlot = resourceCount++;
                }
            }

            var newDataValues = new byte[bufferSize];
            var newResourceValues = new object[resourceCount];

            // Update default values
            scoped ref byte newDataValuesRef = ref MemoryMarshal.GetArrayDataReference(newDataValues);

            foreach (var layoutParameterKeyInfo in layoutParameterKeyInfos)
            {
                if (layoutParameterKeyInfo.IsValueParameter)
                {
                    // TODO: Set default value
                    // TODO: Is it not what this is doing?
                    var defaultValueMetadata = layoutParameterKeyInfo.Key?.DefaultValueMetadata;
                    if (defaultValueMetadata is not null)
                    {
                        int dataOffset = layoutParameterKeyInfo.Offset;
                        int dataSize = layoutParameterKeyInfo.Key.Size;
                        Debug.Assert(dataOffset < bufferSize, $"The offset {dataOffset:X} is out of bounds! (Buffer size: {bufferSize})");
                        Debug.Assert(dataOffset + dataSize <= bufferSize, $"The data will overrun the buffer size! (Offset: {dataOffset:X}, Size: {dataSize}, Buffer size: {bufferSize})");

                        scoped ref byte destRef = ref Unsafe.Add(ref newDataValuesRef, dataOffset);
                        defaultValueMetadata.WriteValue(ref destRef, Alignment);
                    }
                }
            }

            // Second pass to copy existing data at new offsets / slots
            for (int i = 0; i < parameterKeyInfos.Count; ++i)
            {
                var oldParameterKeyInfo = parameterKeyInfos[i];
                var newParameterKeyInfo = newParameterKeyInfos[i];

                if (newParameterKeyInfo.IsValueParameter)
                {
                    var newTotalSize = ComputeAlignedSizeMinusTrailingPadding(
                        elementSize: newParameterKeyInfo.Key.Size,
                        elementCount: newParameterKeyInfo.Count);

                    var oldTotalSize = ComputeAlignedSizeMinusTrailingPadding(
                        elementSize: oldParameterKeyInfo.Key.Size,
                        elementCount: oldParameterKeyInfo.Count);

                    scoped var oldSpan = dataValues.AsSpan(oldParameterKeyInfo.Offset, oldTotalSize);
                    scoped var newSpan = newDataValues.AsSpan(newParameterKeyInfo.Offset, newTotalSize);

                    if (oldParameterKeyInfo.Key.Size == newParameterKeyInfo.Key.Size)
                    {
                        var minTotalSize = Math.Min(oldTotalSize, newTotalSize);
                        oldSpan[..minTotalSize].CopyTo(newSpan);

                        if (newTotalSize > oldTotalSize)
                            newSpan[minTotalSize..].Clear();
                    }
                    else // Different size
                    {
                        // TODO: What to do about this?
                        #warning Partially copying parameter values and leaving remaining bytes zero may cause undesired side effects such as e.g. Color4.Alpha becoming zero.

                        var oldElementSize = oldParameterKeyInfo.Key.Size;
                        var newElementSize = newParameterKeyInfo.Key.Size;
                        var oldElementSizeAligned = Align(oldElementSize);
                        var newElementSizeAligned = Align(newElementSize);
                        var minElementSize = Math.Min(oldElementSize, newElementSize);

                        var minCount = Math.Min(oldParameterKeyInfo.Count, newParameterKeyInfo.Count);

                        for (var elementIndex = 0; elementIndex < minCount; elementIndex++)
                        {
                            oldSpan[..minElementSize].CopyTo(newSpan);

                            // Don't slice for the last element, since there is no alignment padding after it
                            var nextElementIndex = elementIndex + 1;

                            if (nextElementIndex < minCount)
                                oldSpan = oldSpan[oldElementSizeAligned..];
                            if (nextElementIndex < newParameterKeyInfo.Count)
                                newSpan = newSpan[newElementSizeAligned..];
                        }
                    }
                }
                else if (newParameterKeyInfo.IsResourceParameter)
                {
                    Debug.Assert(oldParameterKeyInfo.BindingSlot < objectValues.Length);
                    Debug.Assert(newParameterKeyInfo.BindingSlot < newResourceValues.Length);
                    Debug.Assert(newResourceValues[newParameterKeyInfo.BindingSlot] is null, $"Overwritten resource in binding slot {newParameterKeyInfo.BindingSlot}");

                    newResourceValues[newParameterKeyInfo.BindingSlot] = objectValues[oldParameterKeyInfo.BindingSlot];
                }
            }

            // Update new content
            parameterKeyInfos = newParameterKeyInfos;

            dataValues = newDataValues;
            objectValues = newResourceValues;
        }

        public void NotifyPermutationChange()
        {
            PermutationCounter++;
        }

        #region Serializer

        public class Serializer : ClassDataSerializer<ParameterCollection>
        {
            public override void Serialize(ref ParameterCollection parameterCollection, ArchiveMode mode, SerializationStream stream)
            {
                stream.Serialize(ref parameterCollection.parameterKeyInfos, mode);
                stream.SerializeExtended(ref parameterCollection.objectValues, mode);
                stream.Serialize(ref parameterCollection.dataValues, mode);
            }
        }

        #endregion

        #region DebugView

        private class DebugView(ParameterCollection collection)
        {
            /// <summary>
            /// Copies data from source to destination according to previously compiled layout.
            /// </summary>
            /// <param name="source"></param>
            public ParameterCollectionLayout Layout => collection.Layout;

            /// <summary>
            /// Compute copy operations. Assumes destination layout is sequential.
            /// </summary>
            /// <param name="dest"></param>
            /// <param name="source"></param>
            /// <param name="keyRoot"></param>
            public int PermutationCounter => collection.PermutationCounter;

            // NOTE: This should be named "Parameters", but since its name is hidden and we want it
            //       to appear after PermutationCounter, we prepend ZZ
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public unsafe object[] ZZParameters
                => [.. collection.ParameterKeyInfos
                    // Sort by offsets or binding slot (values will go after objects)
                    .OrderBy(pki => pki.Key.Type == ParameterKeyType.Value ? 0x10000 + pki.Offset : pki.BindingSlot)
                    .SelectMany(pki =>
                    {
                        if (pki.Key.Type == ParameterKeyType.Value)
                        {
                            // Values
                            var stride = Align(pki.Key.Size);
                            var values = new object[pki.Count];

                            var offset = pki.Offset;

                            for (int i = 0; i < pki.Count; ++i, offset += stride)
                            {
                                // Safety check: Check if we read outside of array
                                var outOfBound = offset + pki.Key.Size > collection.DataValues.Length;

                                scoped ref readonly byte paramValueRef = ref collection.DataValues[offset..][0];

                                // Create debug object for this parameter
                                values[i] = new ValueParameter
                                {
                                    Key = pki.Key,
                                    Index = pki.Count > 1 ? i : -1,
                                    Offset = offset,
                                    Value = outOfBound ? "Error (out of bound)" : pki.Key.ReadValue(in paramValueRef)
                                };
                            }

                            return values;
                        }
                        else
                        {
                            // Objects and permutations
                            var objects = new object[pki.Count];
                            var slot = pki.BindingSlot;

                            for (int i = 0; i < pki.Count; ++i, ++slot)
                            {
                                // Create debug object for this parameter
                                objects[i] = new ObjectParameter
                                {
                                    Key = pki.Key,
                                    Index = pki.Count > 1 ? i : -1,
                                    BindingSlot = slot,
                                    Value = collection.objectValues[slot]
                                };
                            }

                            return objects;
                        }
                    })];

            // Represents a value
            private class ValueParameter
            {
                public ParameterKey Key;
                public object Value;
                public int Index;
                public int Offset;

                public override string ToString()
                {
                    var index = Index != -1 ? $"[{Index}]" : string.Empty;
                    return $"{Key.Type} at Offset 0x{Offset:X4}: {Key}{index} = {Value ?? "null"}";
                }
            }

            // Represents an object or permutation
            private class ObjectParameter
            {
                public ParameterKey Key;
                public object Value;
                public int Index;
                public int BindingSlot;

                public override string ToString()
                {
                    var index = Index != -1 ? $"[{Index}]" : string.Empty;
                    return $"{Key.Type} at Slot {BindingSlot}: {Key}{index} = {Value ?? "null"}";
                }
            }
        }

        #endregion
    }
}
