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
