// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Core.Serialization.Contents
{
    /// <summary>
    /// A content data storing its own Location.
    /// </summary>
    public interface IContentData
    {
        string Url { get; set; }
    }
}
