// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading;
using Stride.Core.Diagnostics;
using Stride.Graphics;

namespace Stride.Editor
{
    /// <summary>
    /// The loss of a graphics device in the editor process. The loss is adapter-wide (every editor game, the preview and
    /// the thumbnail generator go with it) and is not recovered: the first component that sees it reports it here, and
    /// the studio restarts.
    /// </summary>
    public static class GraphicsDeviceLoss
    {
        private static readonly Logger Log = GlobalLogger.GetLogger(nameof(GraphicsDeviceLoss));

        /// <summary>
        /// Raised on the reporting thread, once per component that saw the loss.
        /// </summary>
        public static event EventHandler<GraphicsDeviceException> Lost;

        private static GraphicsDeviceException firstLoss;

        /// <summary>
        /// Whether a loss was reported. The games are gone from then on, so any call into them can fail.
        /// </summary>
        public static bool Occurred => FirstLoss is not null;

        /// <summary>
        /// The first reported loss, or <see langword="null"/> if none was.
        /// </summary>
        public static GraphicsDeviceException FirstLoss => Volatile.Read(ref firstLoss);

        public static void Report(object sender, GraphicsDeviceException exception)
        {
            Interlocked.CompareExchange(ref firstLoss, exception, null);
            // The dialog shows the status only; the call that noticed the loss and its error are for the log
            Log.Error($"The graphics device was lost ({exception.Status}), reported by {sender?.GetType().Name}.", exception);
            Lost?.Invoke(sender, exception);
        }
    }
}
