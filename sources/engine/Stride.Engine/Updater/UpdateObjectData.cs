// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Updater
{
    /// <summary>
    /// Defines an conditional entry for the object to update.
    /// </summary>
    public struct UpdateObjectData
    {
        /// <summary>
        /// Condition for update; if non 0, this object will be updated, otherwise not.
        /// </summary>
        public int Condition;

        /// <summary>
        /// Object value.
        /// </summary>
        public object Value;

        public UpdateObjectData(int condition, object value)
        {
            Condition = condition;
            Value = value;
        }

        public UpdateObjectData(object value) : this()
        {
            Condition = 1;
            Value = value;
        }
    }
}
