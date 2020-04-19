// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

using Stride.Core.Diagnostics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Assets.Diagnostics
{
    /// <summary>
    /// Extension to <see cref="Logger"/> for loggin specific error with assets.
    /// </summary>
    public static class AssetLoggerExtensions
    {
        public static void Error(this ILogger logger, Package package, IReference assetReference, AssetMessageCode code, params object[] arguments)
        {
            Error(logger, package, assetReference, code, (IEnumerable<IReference>)null, arguments);
        }

        public static void Error(this ILogger logger, Package package, IReference assetReference, AssetMessageCode code, IEnumerable<IReference> relatedGuids, params object[] arguments)
        {
            Error(logger, package, assetReference, code, relatedGuids, (Exception)null, arguments);
        }

        public static void Error(this ILogger logger, Package package, IReference assetReference, AssetMessageCode code, IReference[] relatedGuids, Exception exception = null)
        {
            Error(logger, package, assetReference, code, (IEnumerable<IReference>)relatedGuids, exception);
        }

        public static void Error(this ILogger logger, Package package, IReference assetReference, AssetMessageCode code, IEnumerable<IReference> relatedGuids, Exception exception = null)
        {
            var logMessage = new AssetLogMessage(package, assetReference, LogMessageType.Error, code) { Exception = exception };
            if (relatedGuids != null)
            {
                logMessage.Related.AddRange(relatedGuids);
            }
            logger.Log(logMessage);
        }

        public static void Error(this ILogger logger, Package package, IReference assetReference, AssetMessageCode code, Exception exception, params object[] arguments)
        {
            Error(logger, package, assetReference, code, null, exception, arguments);
        }

        public static void Error(this ILogger logger, Package package, IReference assetReference, AssetMessageCode code, IEnumerable<IReference> relatedGuids, Exception exception, params object[] arguments)
        {
            var logMessage = new AssetLogMessage(package, assetReference, LogMessageType.Error, code, arguments) { Exception = exception };
            if (relatedGuids != null)
            {
                logMessage.Related.AddRange(relatedGuids);
            }
            logger.Log(logMessage);
        }

        public static void Warning(this ILogger logger, Package package, IReference assetReference, AssetMessageCode code, IReference[] relatedGuids)
        {
            Warning(logger, package, assetReference, code, (IEnumerable<IReference>)null);
        }

        public static void Warning(this ILogger logger, Package package, IReference assetReference, AssetMessageCode code, IEnumerable<IReference> relatedGuids)
        {
            var logMessage = new AssetLogMessage(package, assetReference, LogMessageType.Warning, code);
            if (relatedGuids != null)
            {
                logMessage.Related.AddRange(relatedGuids);
            }
            logger.Log(logMessage);
        }

        public static void Warning(this ILogger logger, Package package, IReference assetReference, AssetMessageCode code, params object[] arguments)
        {
            Warning(logger, package, assetReference, code, null, arguments);
        }

        public static void Warning(this ILogger logger, Package package, IReference assetReference, AssetMessageCode code, IEnumerable<IReference> relatedGuids, params object[] arguments)
        {
            var logMessage = new AssetLogMessage(package, assetReference, LogMessageType.Warning, code, arguments);
            if (relatedGuids != null)
            {
                logMessage.Related.AddRange(relatedGuids);
            }
            logger.Log(logMessage);
        }
    }
}
