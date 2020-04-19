// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Reflection;
using Stride.Core;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Updater;

namespace Stride.Engine.Design
{
    internal class EntityChildPropertyResolver : UpdateMemberResolver
    {
        [ModuleInitializer]
        internal static void InitializeModule()
        {
            UpdateEngine.RegisterMemberResolver(new EntityChildPropertyResolver());
        }

        public override Type SupportedType
        {
            get { return typeof(Entity); }
        }

        public override UpdatableMember ResolveProperty(string memberName)
        {
            return new EntityChildPropertyAccessor(memberName);
        }

        public override UpdatableMember ResolveIndexer(string indexerName)
        {
            // Note: we currently only support component with data contract aliases
            var dotIndex = indexerName.LastIndexOf('.');

            // TODO: Temporary hack to get static field of the requested type/property name
            // Need to have access to DataContract name<=>type mapping in the runtime (only accessible in Stride.Core.Design now)
            var typeName = (dotIndex == -1) ? indexerName : indexerName.Substring(0, dotIndex);
            var type = DataSerializerFactory.GetTypeFromAlias(typeName);
            if (type == null)
                throw new InvalidOperationException($"Can't find a type with alias {typeName}; did you properly set a DataContractAttribute with this alias?");

            return new EntityComponentPropertyAccessor(type);
        }

        private class EntityChildPropertyAccessor : UpdatableCustomAccessor
        {
            private readonly string childName;

            public EntityChildPropertyAccessor(string childName)
            {
                this.childName = childName;
            }

            /// <inheritdoc/>
            public override Type MemberType => typeof(Entity);

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
                var entity = UpdateEngineHelper.PtrToObject<Entity>(obj);
                foreach (var child in entity.Transform.Children)
                {
                    var childEntity = child.Entity;
                    if (childEntity.Name == childName)
                    {
                        return childEntity;
                    }
                }

                // TODO: Instead of throwing an exception, we could just skip it
                // If we do that, we need to add how many entries to skip in the state machine
                throw new InvalidOperationException(string.Format("Could not find child entity named {0}", childName));
            }

            /// <inheritdoc/>
            public override void SetObject(IntPtr obj, object data)
            {
                throw new NotSupportedException();
            }
        }

        private class EntityComponentPropertyAccessor : UpdatableCustomAccessor
        {
            private readonly Type componentType;
            private readonly TypeInfo componentTypeInfo;

            public EntityComponentPropertyAccessor(Type componentType)
            {
                this.componentType = componentType;
                componentTypeInfo = componentType.GetTypeInfo();
            }

            /// <inheritdoc/>
            public override Type MemberType => componentType;

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
                var entity = UpdateEngineHelper.PtrToObject<Entity>(obj);
                var components = entity.Components;
                for (int i = 0; i < components.Count; i++)
                {
                    var component = components[i];
                    if (componentTypeInfo.IsAssignableFrom(component.GetType().GetTypeInfo()))
                    {
                        return component;
                    }
                }
                return null;
            }

            /// <inheritdoc/>
            public override void SetObject(IntPtr obj, object data)
            {
                var entity = UpdateEngineHelper.PtrToObject<Entity>(obj);
                var components = entity.Components;
                bool notSet = true;
                for (int i = 0; i < components.Count; i++)
                {
                    var component = components[i];
                    if (componentTypeInfo.IsAssignableFrom(component.GetType().GetTypeInfo()))
                    {
                        components[i] = (EntityComponent)data;
                        notSet = false;
                    }
                }
                if (notSet)
                {
                    components.Add((EntityComponent)data);
                }
            }
        }
    }
}
