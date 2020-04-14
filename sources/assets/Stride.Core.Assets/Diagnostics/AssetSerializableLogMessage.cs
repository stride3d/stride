// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Core.Assets.Diagnostics
{
    [DataContract]
    public class AssetSerializableLogMessage : SerializableLogMessage
    {
        public AssetSerializableLogMessage()
        {

        }

        public AssetSerializableLogMessage(AssetLogMessage logMessage)
            : base(logMessage)
        {
            if (logMessage.AssetReference != null)
            {
                AssetId = logMessage.AssetReference.Id;
                AssetUrl = logMessage.AssetReference.Location;
            }
        }

        public AssetSerializableLogMessage(AssetId assetId, UFile assetUrl, LogMessageType type, string text, ExceptionInfo exceptionInfo = null)
            : base("", type, text, exceptionInfo)
        {
            AssetId = assetId;
            AssetUrl = assetUrl;
        }

        public AssetId AssetId { get; set; }

        public UFile AssetUrl { get; set; }

        public string File { get; set; }

        public int Line { get; set; }

        public int Character { get; set; }

        public override string ToString()
        {
            return $"{AssetUrl}({Line},{Character}): {base.ToString()}";
        }
    }
}
