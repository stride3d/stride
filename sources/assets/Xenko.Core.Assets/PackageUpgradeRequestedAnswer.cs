// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Core.Assets
{
    /// <summary>
    /// An enum representing the user answer to a package upgrade request.
    /// </summary>
    public enum PackageUpgradeRequestedAnswer
    {
        /// <summary>
        /// The related package should be upgraded.
        /// </summary>
        Upgrade,
        /// <summary>
        /// The related package and all following packages should be upgraded.
        /// </summary>
        UpgradeAll,
        /// <summary>
        /// The related package should not be upgraded.
        /// </summary>
        DoNotUpgrade,
        /// <summary>
        /// The related package and all following packages should not be upgraded.
        /// </summary>
        DoNotUpgradeAny,
    }
}
