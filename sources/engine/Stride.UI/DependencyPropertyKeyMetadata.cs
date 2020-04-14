// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;

namespace Stride.UI
{
    [Flags]
    public enum DependencyPropertyFlags
    {
        Default = 0,
        ReadOnly = 0x1,
        Attached = 0x2,
    }

    public class DependencyPropertyKeyMetadata : PropertyKeyMetadata
    {
        public static readonly DependencyPropertyKeyMetadata Attached = new DependencyPropertyKeyMetadata(DependencyPropertyFlags.Attached);

        public static readonly DependencyPropertyKeyMetadata AttachedReadOnly = new DependencyPropertyKeyMetadata(DependencyPropertyFlags.Attached | DependencyPropertyFlags.ReadOnly);

        public static readonly DependencyPropertyKeyMetadata Default = new DependencyPropertyKeyMetadata(DependencyPropertyFlags.Default);

        public static readonly DependencyPropertyKeyMetadata ReadOnly = new DependencyPropertyKeyMetadata(DependencyPropertyFlags.ReadOnly);

        internal DependencyPropertyKeyMetadata(DependencyPropertyFlags flags)
        {
            Flags = flags;
        }

        public DependencyPropertyFlags Flags { get; }
    }
}
