// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using GNU.Gettext;
using GNU.Gettext.Utils;
using Stride.Core.Annotations;
using Stride.Core.Extensions;

namespace Stride.Core.Translation.Extractor
{
    // ReSharper disable once InconsistentNaming
    internal class POExporter
    {
        private readonly Catalog previousCatalog;

        public POExporter([NotNull] Options options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Catalog = new Catalog();
            if (File.Exists(Options.OutputFile))
            {
                if (Options.Overwrite)
                {
                    previousCatalog = new Catalog();
                    previousCatalog.Load(Options.OutputFile);
                }
                else
                {
                    Catalog.Load(Options.OutputFile);
                    Catalog.ForEach(e => e.ClearReferences());
                }
            }
        }

        public Catalog Catalog { get; }

        public Options Options { get; }

        public void Merge([ItemNotNull, NotNull]  IEnumerable<Message> messages)
        {
            if (messages == null) throw new ArgumentNullException(nameof(messages));
            messages.ForEach(MergeMessage);
        }

        public void Save()
        {
            if (Options.Backup && File.Exists(Options.OutputFile))
            {
                var bakFileName = Options.OutputFile + ".bak";
                File.Copy(Options.OutputFile, bakFileName, true);
                File.Delete(Options.OutputFile);
                Log(Tr._("Created backup file '{0}'."), bakFileName);
            }
            Catalog.Save(Options.OutputFile);
            Log(Tr._("Exported messages to '{0}'."), Options.OutputFile);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Log(string message, params object[] arg)
        {
            if (!Options.Verbose)
                return;

            Console.WriteLine(message, arg);
        }

        private void MergeMessage([NotNull] Message message)
        {
            var entry = Catalog.FindItem(message.Text, message.Context);
            var newEntry = entry == null;
            if (newEntry)
            {
                entry = new CatalogEntry(Catalog, message.Text, message.PluralText);
            }

            // Source reference
            if (message.Source != null)
            {
                var filePath = FileUtils.GetRelativeUri(message.Source.FullPath, Path.GetFullPath(Options.OutputFile));
                entry.AddReference($"{filePath}:{message.LineNumber}");
            }
            // Plural
            if (!string.IsNullOrEmpty(message.PluralText))
            {
                entry.SetTranslations(Enumerable.Repeat("", Catalog.PluralFormsCount).ToArray());
                entry.SetPluralString(message.PluralText);
            }
            // Context
            if (!string.IsNullOrEmpty(message.Context))
            {
                entry.Context = message.Context;
            }
            // Auto comments
            if (!string.IsNullOrEmpty(message.Comment))
            {
                entry.AddAutoComment(message.Comment, true);
            }
            // Preserve previous comments
            if (newEntry && Options.PreserveComments)
            {
                var previousEntry = previousCatalog?.FindItem(entry);
                if (previousEntry != null)
                {
                    // Simple comment
                    if (previousEntry.HasComment && !entry.HasComment)
                    {
                        entry.Comment = previousEntry.Comment;
                    }
                    // Auto comments
                    foreach (var comment in previousEntry.AutoComments)
                    {
                        entry.AddAutoComment(comment, true);
                    }
                }
            }
            // Add entry if it did not exist
            if (newEntry)
                Catalog.AddItem(entry);
        }
    }
}
