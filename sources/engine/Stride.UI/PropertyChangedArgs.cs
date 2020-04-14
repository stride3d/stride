// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.UI
{
    /// <summary>
    /// An argument class containing information about a property that changed.
    /// </summary>
    /// <typeparam name="T">The type of the property that changed</typeparam>
    public class PropertyChangedArgs<T>
    {
        public T OldValue { get; internal set; }
        public T NewValue { get; internal set; }
    }
}
