// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Core.Shaders.Ast.Glsl
{
    /// <summary>
    /// An interface type.
    /// </summary>
    public partial class InterfaceType : StructType
    {
        public InterfaceType()
        {
        }

        public InterfaceType(string name)
        {
            Name = name;
        }
    }
}
