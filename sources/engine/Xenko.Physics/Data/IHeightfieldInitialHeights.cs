// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Xenko.Physics
{
    public interface IHeightfieldInitialHeights
    {
        T GetSource<T>() where T : class;

        object GetSource();

        T[] GetHeights<T>() where T : struct;

        Type GetSourceType();

        void SetSource(object source);
    }
}
