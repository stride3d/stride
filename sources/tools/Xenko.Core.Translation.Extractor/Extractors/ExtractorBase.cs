// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Annotations;
using Xenko.Core.IO;

namespace Xenko.Core.Translation.Extractor
{
    internal abstract class ExtractorBase
    {
        private readonly string[] supportedExtensions;

        protected ExtractorBase([NotNull] ICollection<UFile> inputFiles, params string[] supportedExtensions)
        {
            this.supportedExtensions = supportedExtensions;
            InputFiles = inputFiles ?? throw new ArgumentNullException(nameof(inputFiles));
        }

        protected ICollection<UFile> InputFiles { get; }

        [NotNull]
        public IReadOnlyList<Message> ExtractMessages()
        {
            return InputFiles.Where(f => supportedExtensions.Contains(f.GetFileExtension())).SelectMany(ExtractMessagesFromFile).ToList();
        }

        [ItemNotNull]
        protected abstract IEnumerable<Message> ExtractMessagesFromFile([NotNull] UFile file);
    }
}
