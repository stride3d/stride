// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Assets
{
    /// <summary>
    /// Defines a context for overrides when upgrading an asset.
    /// </summary>
    public enum OverrideUpgraderHint
    {
        /// <summary>
        /// The upgrader is performed on an asset that may be used as the base for another asset
        /// </summary>
        Unknown,

        /// <summary>
        /// The upgrader is performed on an asset that has at least one base asset (for asset templating)
        /// </summary>
        Derived,

        /// <summary>
        /// The upgrader is performed on the base asset of an asset being upgraded (<see cref="Asset.Base"/> or <see cref="Asset.BaseParts"/>)
        /// </summary>
        Base
    }
}
