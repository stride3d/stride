// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Xenko.Editor.EditorGame.ContentLoader
{
    public class ContentLoadEventArgs : EventArgs
    {
        public ContentLoadEventArgs(int contentLoadingCount)
        {
            ContentLoadingCount = contentLoadingCount;
        }

        public int ContentLoadingCount { get; }
    }
}
