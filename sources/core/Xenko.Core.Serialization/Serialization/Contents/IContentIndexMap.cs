// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xenko.Core.Storage;

namespace Xenko.Core.Serialization.Contents
{
    public interface IContentIndexMap : IDisposable
    {
        bool TryGetValue(string url, out ObjectId objectId);

        IEnumerable<KeyValuePair<string, ObjectId>> SearchValues(Func<KeyValuePair<string, ObjectId>, bool> predicate);

        bool Contains(string url);

        ObjectId this[string url] { get; set; }

        IEnumerable<KeyValuePair<string, ObjectId>> GetMergedIdMap();
    }
}
