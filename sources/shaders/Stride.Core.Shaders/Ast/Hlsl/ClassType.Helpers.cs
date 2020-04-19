// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Shaders.Ast.Hlsl
{
    /// <summary>
    /// Definition of a class.
    /// </summary>
    public partial class ClassType
    {
        /// <summary>
        /// Determines whether the specified type is a a stream type.
        /// </summary>
        /// <param name="targetType">Type of the target.</param>
        /// <returns><c>true</c> if [the specified target type] [is stream type] ; otherwise, <c>false</c>.</returns>
        public static bool IsStreamOutputType(TypeBase targetType)
        {
            return targetType is ClassType && ((ClassType)targetType).GenericArguments.Count > 0 && targetType.IsStreamTypeName();
        }
    }
}
