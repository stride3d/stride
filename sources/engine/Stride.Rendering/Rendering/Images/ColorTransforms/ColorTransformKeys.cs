// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Rendering.Images
{
    /// <summary>
    /// Parameter keys used by <see cref="ColorTransformBase"/>
    /// </summary>
    internal static class ColorTransformKeys
    {
        /// <summary>
        /// A boolean indicating wheter a <see cref="ColorTransformBase"/> is active or not.
        /// </summary>
        public static readonly PermutationParameterKey<bool> Enabled = ParameterKeys.NewPermutation(true);

        /// <summary>
        /// The shader used by <see cref="ColorTransformBase"/>.
        /// </summary>
        public static readonly PermutationParameterKey<string> Shader = ParameterKeys.NewPermutation("ColorTransformShader");

        /// <summary>
        /// The shader used by <see cref="ColorTransformBase"/>.
        /// </summary>
        public static readonly PermutationParameterKey<object[]> GenericArguments = ParameterKeys.NewPermutation((object[])null);
    }
}
