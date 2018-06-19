// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Annotations;

namespace Xenko.Core.Serialization.Contents
{
    public interface ILoadableReference
    {
        string Location { get; }

        [NotNull]
        Type Type { get; }
    }
}
