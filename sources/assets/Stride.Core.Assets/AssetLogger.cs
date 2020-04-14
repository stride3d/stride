// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Diagnostics;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Assets
{
    class AssetLogger : Logger
    {
        private readonly Package package;
        private readonly IReference assetReference;
        private readonly string assetFullPath;
        private readonly ILogger loggerToForward;

        public AssetLogger(Package package, IReference assetReference, string assetFullPath, ILogger loggerToForward)
        {
            this.package = package;
            this.assetReference = assetReference;
            this.assetFullPath = assetFullPath;
            this.loggerToForward = loggerToForward;
            ActivateLog(LogMessageType.Debug);
        }

        protected override void LogRaw(ILogMessage logMessage)
        {
            loggerToForward?.Log(AssetLogMessage.From(package, assetReference, logMessage, assetFullPath));
        }
    }
}
