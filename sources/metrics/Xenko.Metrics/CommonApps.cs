using System;

namespace Xenko.Metrics
{
    /// <summary>
    /// Identifiers for common apps.
    /// </summary>
    public static class CommonApps
    {
        /// <summary>
        /// The xenko launcher application identifier
        /// </summary>
        public static readonly MetricAppId XenkoLauncherAppId =
            new MetricAppId(new Guid("B3149460-226D-4877-9E54-057308847969"), "XenkoLauncher");

        /// <summary>
        /// The xenko editor application identifier
        /// </summary>
        public static readonly MetricAppId XenkoEditorAppId =
            new MetricAppId(new Guid("BD1F1188-9ED7-410C-8080-F4E0A698DE2A"), "XenkoEditor");
    }
}