// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.IO;

namespace Xenko.Core.Translation.Extractor
{
    internal class Message
    {
        public string Comment;
        public string Context;
        public string PluralText;
        public string Text;

        public long LineNumber;
        public UFile Source;
    }
}
