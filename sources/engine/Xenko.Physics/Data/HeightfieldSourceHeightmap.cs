// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Annotations;

namespace Xenko.Physics
{
    [DataContract]
    public class HeightfieldSourceHeightmap : IHeightfieldInitialHeights
    {
        [DataMember(10)]
        [DefaultValue(null)]
        [InlineProperty]
        public Heightmap Heightmap { get; set; }

        public HeightfieldSourceHeightmap()
            : this(null)
        {
        }

        public HeightfieldSourceHeightmap(Heightmap heightmap)
        {
            Heightmap = heightmap;
        }

        public T GetSource<T>() where T : class
        {
            return Heightmap as T;
        }

        public object GetSource()
        {
            return Heightmap;
        }

        public T[] GetHeights<T>() where T : struct
        {
            if (typeof(T) == typeof(byte))
            {
                return Heightmap?.Bytes?.ToArray() as T[];
            }
            else if (typeof(T) == typeof(short))
            {
                return Heightmap?.Shorts?.ToArray() as T[];
            }
            else if (typeof(T) == typeof(float))
            {
                return Heightmap?.Floats?.ToArray() as T[];
            }
            else
            {
                return null;
            }
        }

        public Type GetSourceType()
        {
            return Heightmap?.GetType();
        }

        public void SetSource(object source)
        {
            Heightmap = source as Heightmap;
        }
    }
}
