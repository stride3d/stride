// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core
{
    /// <summary>
    /// <para>Specify the way to store a property or field of some class or structure.</para>
    /// </summary>
    public enum DataMemberMode
    {
        /// <summary>
        /// Use the default mode depending on the type of the field/property.
        /// </summary>
        Default = 0,

        /// <summary>
        /// When restored, new object is created by using the parameters in
        /// the YAML data and assigned to the property / field. When the
        /// property / field is writeable, this is the default.
        /// </summary>
        Assign = 1,

        /// <summary>
        ///  Only valid for a property / field that return a class, no strings, primitives or value types.
        ///  When restored, instead of recreating the whole class,
        ///  the members are independently restored. When the property / field
        ///  is not writeable this is the default.
        /// </summary>
        Content = 2,

        /// <summary>
        /// The property / field will not be stored.
        /// </summary>
        Never = 4,
    }
}
