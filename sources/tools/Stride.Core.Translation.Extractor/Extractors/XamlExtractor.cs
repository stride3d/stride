// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xaml;
using Stride.Core.Annotations;
using Stride.Core.IO;
using Stride.Core.Translation.Presentation.MarkupExtensions;

namespace Stride.Core.Translation.Extractor
{
    internal class XamlExtractor : ExtractorBase
    {
        public XamlExtractor([NotNull] ICollection<UFile> inputFiles)
            : base(inputFiles, ".xaml")
        {
        }

        protected override IEnumerable<Message> ExtractMessagesFromFile(UFile file)
        {
            try
            {
                // Read all content
                var reader = new XamlXmlReader(file.ToWindowsPath(), new XamlXmlReaderSettings { ProvideLineInfo = true });
                return DoExtractMessagesFromFile(file, reader);
            }
            catch (XamlException ex)
            {
                Console.Error.WriteLine($"{file.ToWindowsPath()}: {ex.Message}");
                return Enumerable.Empty<Message>();
            }
        }

        private IEnumerable<Message> DoExtractMessagesFromFile([NotNull] UFile file, [NotNull] XamlXmlReader reader)
        {
            while (reader.Read())
            {
                if (reader.NodeType != XamlNodeType.StartObject)
                    continue;

                var lineNumber = reader.LineNumber;
                var type = reader.Type.UnderlyingType;
                if (type == typeof(LocalizeExtension))
                {
                    var readSubtree = reader.ReadSubtree();
                    readSubtree.Read(); // read once to position on the first node
                    var message = ParseLocalizeExtension(readSubtree);
                    message.LineNumber = lineNumber;
                    message.Source = file;
                    yield return message;
                }
            }
        }

        [NotNull]
        private Message ParseLocalizeExtension([NotNull] XamlReader reader)
        {
            var message = new Message();
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XamlNodeType.StartObject:
                        // Skip this object
                        while (reader.Read() && reader.NodeType != XamlNodeType.EndObject) { }
                        break;

                    case XamlNodeType.StartMember:
                        var member = reader.Member;
                        switch (member.Name)
                        {
                            case nameof(LocalizeExtension.Text):
                                if (reader.Read() && reader.NodeType == XamlNodeType.Value)
                                {
                                    message.Text = reader.Value?.ToString();
                                }
                                break;

                            case nameof(LocalizeExtension.Plural):
                                if (reader.Read() && reader.NodeType == XamlNodeType.Value)
                                {
                                    message.PluralText = reader.Value?.ToString();
                                }
                                break;

                            case nameof(LocalizeExtension.Context):
                                if (reader.Read() && reader.NodeType == XamlNodeType.Value)
                                {
                                    message.Context = reader.Value?.ToString();
                                }
                                break;

                            case nameof(LocalizeExtension.Count):
                            case nameof(LocalizeExtension.IsStringFormat):
                                // Ignore
                                break;

                            default:
                                // Positional parameter in the constructor
                                var paramIndex = 0;
                                while (reader.Read() && reader.NodeType == XamlNodeType.Value)
                                {
                                    switch (paramIndex)
                                    {
                                        case 0:
                                            message.Text = reader.Value?.ToString();
                                            break;

                                        default:
                                            throw new IndexOutOfRangeException();
                                    }
                                    paramIndex++;
                                }
                                break;
                        }
                        break;
                }
            }
            return message;
        }
    }
}
