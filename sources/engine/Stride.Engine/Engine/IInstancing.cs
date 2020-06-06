using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core.Mathematics;

namespace Stride.Engine
{
    public interface IInstancing
    {
        int InstanceCount { get; }
        BoundingBox BoundingBox { get; }
        void Update();
    }
}
