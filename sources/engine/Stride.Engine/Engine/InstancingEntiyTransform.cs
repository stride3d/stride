// Copyright (c) Stride contributors (https://stride3d.net) and Tebjan Halm
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Engine
{
    [DataContract("InstancingEntityTransform")]
    [Display("EntityTransform")]
    public class InstancingEntityTransform : InstancingUserArray
    {
        [DataMemberIgnore]
        public override ModelTransformUsage ModelTransformUsage 
        { 
            get => ModelTransformUsage.Ignore;
        }

        private readonly List<InstanceComponent> instances = new List<InstanceComponent>();

        internal InstanceComponent GetInstanceAt(int instanceId)
        {
            return instances[instanceId];
        }

        internal void ClearInstances()
        {
            instances.Clear();
        }

        internal void AddInstance(InstanceComponent instance)
        {
            instances.Add(instance);
            //Debug.WriteLine("Instance Added: " + instance.Entity?.Name + " Instance Count: " + instances.Count);
        }

        internal void RemoveInstance(InstanceComponent instance)
        {
            instances.Remove(instance);
            //Debug.WriteLine("Instance Removed: " + instance.Entity?.Name + " Instance Count: " + instances.Count);
        }

        public override void Update()
        {
            // Manage array
            var maxInstanceCount = instances.Count;
            var matrices = WorldMatrices;
            if (matrices == null || matrices.Length < maxInstanceCount)
            {
                matrices = new Matrix[maxInstanceCount];
            }

            // Gather instance transforms
            var instanceCount = 0;
            for (int i = 0; i < maxInstanceCount; i++)
            {
                var instance = instances[i];
                if (instance.Enabled)
                {
                    matrices[instanceCount++] = instance.Entity.Transform.WorldMatrix;
                }
            }

            // Set array back
            UpdateWorldMatrices(matrices, instanceCount);

            // Do base work
            base.Update();
        }
    }

    public static class InstancingExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEntityTransform(this InstancingComponent component, out InstancingEntityTransform instancing)
        {
            if (component != null && component.Type is InstancingEntityTransform type)
            {
                instancing = type;
                return true;
            }

            instancing = null;
            return false;
        }
    }
}
