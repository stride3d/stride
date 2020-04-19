// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core
{
    /// <summary>
    /// Interface for serializable object having an url (so referenceable by other assets and saved into a single blob file)
    /// </summary>
    public interface IContentUrl
    {
        /// <summary>
        /// The URL of this asset.
        /// </summary>
        string Url { get; set; }
    }
}
