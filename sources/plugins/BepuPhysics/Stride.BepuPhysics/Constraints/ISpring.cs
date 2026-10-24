// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.BepuPhysics.Constraints;

public interface ISpring
{
    /// <summary>
    /// The target number of undamped oscillations per unit of time.
    /// Higher frequency values create stiffer connections, while lower values allow more elasticity in the joint.
    /// </summary>
    /// <userdoc>
    /// The target number of undamped oscillations per unit of time.
    /// Higher frequency values create stiffer connections, while lower values allow more elasticity in the joint.
    /// </userdoc>
    public float SpringFrequency { get; set; }

    /// <summary>
    /// The ratio of the spring's actual damping to its critical damping. 0 is undamped, 1 is critically damped, and higher values are overdamped.
    /// Higher damping ratios reduce oscillations and make the connection less elastic.
    /// </summary>
    /// <userdoc>
    /// The ratio of the spring's actual damping to its critical damping. 0 is undamped, 1 is critically damped, and higher values are overdamped.
    /// Higher damping ratios reduce oscillations and make the connection less elastic.
    /// </userdoc>
    public float SpringDampingRatio { get; set; }
}
