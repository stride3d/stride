// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Serialization;

namespace Stride.Engine.Design
{
    public class ParameterContainerExtensions
    {
        public static SerializerSelector DefaultSceneSerializerSelector;

        static ParameterContainerExtensions()
        {
            DefaultSceneSerializerSelector = new SerializerSelector("Default", "Content");
        }
    }
}
