// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Core.Serialization
{
    public abstract class ClassDataSerializer<T> : DataSerializer<T> where T : class, new()
    {
        /// <inheritdoc/>
        public override void PreSerialize(ref T obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize && obj == null)
            {
                try
                {
                    obj = new T();
                }
                catch (System.Exception)
                {
                    //obj = (T)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(T));
                    //return;
                    throw;
                }
            }
        }
    }
}
