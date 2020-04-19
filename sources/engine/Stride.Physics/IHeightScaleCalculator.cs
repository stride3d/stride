// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Physics
{
    public interface IHeightScaleCalculator
    {
        float Calculate(IHeightStickParameters heightDescription);
    }
}
