// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Rendering;
using Stride.Updater;

using static System.Runtime.CompilerServices.Unsafe;

namespace Stride.Engine.Design
{
    public class ParameterCollectionResolver : UpdateMemberResolver
    {
        public override Type SupportedType => typeof(ParameterCollection);


        public override UpdatableMember ResolveIndexer(string indexerName)
        {
            // TODO: PropertyKeyParseException from UpdateEngine should be thrown here
            var key = ParameterKeys.FindByName(indexerName)
                ?? throw new InvalidOperationException($"Property Key path parse error: could not parse indexer value '{indexerName}'");

            switch (key.Type)
            {
                case ParameterKeyType.Value:
                    var accessorType = typeof(ValueParameterCollectionAccessor<>).MakeGenericType(key.PropertyType);
                    return (UpdatableMember) Activator.CreateInstance(accessorType, key);

                case ParameterKeyType.Object:
                    return new ObjectParameterCollectionAccessor(key);

                default:
                    throw new NotSupportedException($"{nameof(ParameterCollectionResolver)} can only handle Value and Object keys");
            }
        }

        // Needed for AOT platforms
        public static void InstantiateValueAccessor<T>() where T : unmanaged
        {
            new ValueParameterCollectionAccessor<T>(parameterKey: null);
        }

        private class ValueParameterCollectionAccessor<T> : UpdatableCustomAccessor where T : unmanaged
        {
            private readonly ValueParameterKey<T> parameterKey;

            public ValueParameterCollectionAccessor(ValueParameterKey<T> parameterKey)
            {
                this.parameterKey = parameterKey;
            }

            /// <inheritdoc/>
            public override Type MemberType => parameterKey.PropertyType;

            /// <inheritdoc/>
            public override void GetBlittable(nint obj, nint data)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override unsafe void SetBlittable(nint obj, nint data)
            {
                var parameterCollection = UpdateEngineHelper.PointerToObject<ParameterCollection>(obj);

                var value = ReadUnaligned<T>((void*)data);
                parameterCollection.Set(parameterKey, ref value);
            }

            /// <inheritdoc/>
            public override void SetStruct(nint obj, object data)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override unsafe nint GetStructAndUnbox(nint obj, object data)
            {
                var parameterCollection = UpdateEngineHelper.PointerToObject<ParameterCollection>(obj);

                ref var valuePtr = ref Unbox<T>(data);

                valuePtr = parameterCollection.Get(parameterKey);

                return (nint)AsPointer(ref valuePtr);
            }

            /// <inheritdoc/>
            public override object GetObject(IntPtr obj)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override void SetObject(IntPtr obj, object data)
            {
                throw new NotSupportedException();
            }
        }

        private class ObjectParameterCollectionAccessor : UpdatableCustomAccessor
        {
            private readonly ParameterKey parameterKey;

            public ObjectParameterCollectionAccessor(ParameterKey parameterKey)
            {
                this.parameterKey = parameterKey;
            }

            /// <inheritdoc/>
            public override Type MemberType => parameterKey.PropertyType;

            /// <inheritdoc/>
            public override void GetBlittable(IntPtr obj, IntPtr data)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override void SetBlittable(IntPtr obj, IntPtr data)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override void SetStruct(IntPtr obj, object data)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override IntPtr GetStructAndUnbox(IntPtr obj, object data)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override object GetObject(IntPtr obj)
            {
                var parameterCollection = UpdateEngineHelper.PointerToObject<ParameterCollection>(obj);
                return parameterCollection.GetObject(parameterKey);
            }

            /// <inheritdoc/>
            public override void SetObject(IntPtr obj, object data)
            {
                var parameterCollection = UpdateEngineHelper.PointerToObject<ParameterCollection>(obj);
                parameterCollection.SetObject(parameterKey, data);
            }
        }

        #region Module Initializer

        [ModuleInitializer]
        internal static void InitializeModule()
        {
            UpdateEngine.RegisterMemberResolver(new ParameterCollectionResolver());
        }

        #endregion
    }
}
