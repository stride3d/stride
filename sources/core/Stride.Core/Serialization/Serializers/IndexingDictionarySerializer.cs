// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Collections;

namespace Xenko.Core.Serialization.Serializers
{
    internal class IndexingDictionarySerializer<TValue> : DictionaryAllSerializer<IndexingDictionary<TValue>, int, TValue> where TValue : class
    {
    }
}
