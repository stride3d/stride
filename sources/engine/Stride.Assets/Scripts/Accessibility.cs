// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Assets.Scripts
{
    /// <summary>
    /// Describes accessibility of a <see cref="VisualScriptAsset"/>, <see cref="Method"/> or <see cref="Symbol"/>.
    /// </summary>
    public enum Accessibility
    {
        Public = 0,
        Private = 1,
        Protected = 2,
        Internal = 3,
        ProtectedOrInternal = 4,
    }
}
