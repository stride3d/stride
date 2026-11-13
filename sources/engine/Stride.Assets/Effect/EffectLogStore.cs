// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Serialization;
using Stride.Rendering;
using Stride.Shaders.Compiler;

namespace Stride.Assets.Effect
{
    public class EffectLogStore : DictionaryStore<EffectCompileRequest, bool>
    {
        private byte[] documentMarker = Encoding.UTF8.GetBytes("---\r\n");
        private readonly ILogger logger;
        private readonly string path;

        public EffectLogStore(Stream stream)
            : base(stream)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EffectLogStore"/> class that names the entries it skips.
        /// </summary>
        /// <param name="stream">The stream of the effect log.</param>
        /// <param name="logger">The logger that gets a warning for each entry that can't be read, such as an entry with a key of an assembly that isn't loaded.</param>
        /// <param name="path">The path of the effect log, for the warnings.</param>
        public EffectLogStore(Stream stream, ILogger logger, string path)
            : base(stream)
        {
            this.logger = logger;
            this.path = path;
        }

        protected override void WriteEntry(Stream stream, KeyValuePair<EffectCompileRequest, bool> value)
        {
            stream.Write(documentMarker, 0, documentMarker.Length);
            AssetYamlSerializer.Default.Serialize(stream, value.Key);
        }

        protected override List<KeyValuePair<EffectCompileRequest, bool>> ReadEntries(Stream localStream)
        {
            var result = new List<KeyValuePair<EffectCompileRequest, bool>>();

            var reader = new EventReader(new Parser(new StreamReader(localStream)));
            reader.Expect<StreamStart>();
            while (reader.Accept<DocumentStart>())
            {
                var start = reader.Parser.Current.Start;

                // The serializer goes on after an error, so an entry with one may have lost keys
                var entryLogger = new LoggerResult();
                var effectCompileRequest = AssetYamlSerializer.Default.Deserialize(reader, null, typeof(EffectCompileRequest), out _, new SerializerContextSettings(entryLogger)) as EffectCompileRequest;
                var error = entryLogger.Messages.FirstOrDefault(x => x.IsAtLeast(LogMessageType.Warning));
                if (error != null || effectCompileRequest?.EffectName == null || effectCompileRequest.UsedParameters == null)
                {
                    var (place, reason) = error is LogMessage { Exception: YamlException yamlException }
                        ? ($"{yamlException.Start.Line + 1},{yamlException.Start.Column + 1}", WithoutPosition(yamlException.Message))
                        : ($"{start.Line + 1}", error?.Text ?? "it has no effect name or parameters");
                    logger?.Warning($"{path}({place}): Skipped the effect log entry of [{effectCompileRequest?.EffectName}]: {reason}");
                    continue;
                }

                result.Add(new KeyValuePair<EffectCompileRequest, bool>(effectCompileRequest, true));
            }

            return result;
        }

        // A YamlException message ends with its position, "((Lin: 5, Col: 4, Chr: 100) -> (...))", which counts lines from 0
        private static string WithoutPosition(string message)
        {
            var position = message.LastIndexOf(" ((", StringComparison.Ordinal);
            return position >= 0 ? message[..position] : message;
        }
    }
}
