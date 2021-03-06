using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;

namespace Stride.Physics.Constraints
{
    /// <summary>
    /// A constraint description that includes rotation axis.
    /// </summary>
    public interface IRotateConstraintDesc : IConstraintDesc
    {
        public Quaternion AxisInA { get; set; }

        public Quaternion AxisInB { get; set; }
    }
}
