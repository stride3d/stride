// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.BuildEngine
{
    [ContentSerializer(typeof(DataContentSerializer<CommandResultEntry>))]
    [DataContract,Serializable]
    public class CommandResultEntry
    {
        public Dictionary<ObjectUrl, ObjectId> InputDependencyVersions;
        /// <summary>
        /// Output object ids as saved in the object database.
        /// </summary>
        public Dictionary<ObjectUrl, ObjectId> OutputObjects;

        /// <summary>
        /// Log messages corresponding to the execution of the command.
        /// </summary>
        public List<SerializableLogMessage> LogMessages;

        /// <summary>
        /// Tags added for a given URL.
        /// </summary>
        public List<KeyValuePair<ObjectUrl, string>> TagSymbols;

        public CommandResultEntry()
        {
            InputDependencyVersions = new Dictionary<ObjectUrl, ObjectId>();
            OutputObjects = new Dictionary<ObjectUrl, ObjectId>();
            LogMessages = new List<SerializableLogMessage>();
            TagSymbols = new List<KeyValuePair<ObjectUrl, string>>();
        }
    }
}
