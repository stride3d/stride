// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Serialization.Contents
{
    /// <summary>
    /// A content data storing its own Location.
    /// </summary>
    public interface IContentData
    {
        string Url { get; set; }
    }
}
