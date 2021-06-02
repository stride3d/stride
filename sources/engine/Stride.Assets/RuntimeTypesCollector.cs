// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Visitors;
using Stride.Core.Reflection;

namespace Stride.Assets
{
    /// <summary>
    /// Dynamically detects runtime content types in a given object
    /// </summary>
    public class RuntimeTypesCollector : AssetVisitorBase
    {
        private readonly HashSet<Type> runtimeTypes = new();

        public IEnumerable<Type> GetRuntimeTypes(object obj)
        {
            Visit(obj);
            return runtimeTypes;
        }

        public override void VisitObject(object obj, ObjectDescriptor descriptor, bool visitMembers)
        {
            if (AssetRegistry.IsContentType(obj.GetType()))
            {
                // Asset compiler will sort out any dependencies so we dont need to visit any content types
                runtimeTypes.Add(obj.GetType());
            }
            else
            {
                base.VisitObject(obj, descriptor, visitMembers);
            }
        }
    }
}
