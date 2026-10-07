// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable 436 // The type 'type' in 'assembly' conflicts with the imported type 'type2' in 'assembly' (due to StrideVersion being duplicated)

using System;
using System.Runtime.InteropServices;
using Stride.Core.Annotations;
using Stride.Graphics;

namespace Stride.GameStudio.Helpers
{
    public static class StrideGameStudio
    {
        [NotNull]
        public static string CopyrightText1 => "© 2018 Stride contributors";

        [NotNull]
        public static string CopyrightText2 => "© 2011-2018 Silicon Studio Corp.";

        [NotNull]
        public static string EditorName => $"Stride Game Studio {EditorVersion} ({RuntimeLabel})";

        // EditorName plus the active graphics API in the environment group; for the main window title.
        [NotNull]
        public static string EditorNameWithGraphicsApi => $"Stride Game Studio {EditorVersion} ({EditorEnvironment})";

        /// <summary>Runtime and graphics API of this process, e.g. ".NET 11.0.0-rc.1, Vulkan".</summary>
        [NotNull]
        public static string EditorEnvironment => $"{RuntimeLabel}, {GraphicsDevice.Platform}";

        // ".NET 11.0.0-rc.1.26425.128" -> ".NET 11.0.0-rc.1": the build number is too long for a title.
        [NotNull]
        public static string RuntimeLabel
        {
            get
            {
                var match = System.Text.RegularExpressions.Regex.Match(RuntimeInformation.FrameworkDescription, @"^\.NET \d+\.\d+\.\d+(-[a-z]+\.\d+)?");
                return match.Success ? match.Value : RuntimeInformation.FrameworkDescription;
            }
        }

        [NotNull]
        public static string EditorVersion => StrideVersion.NuGetVersion;

        [NotNull]
        public static string EditorVersionWithMetadata => StrideVersion.NuGetVersion + StrideVersion.BuildMetadata;

        public static string EditorVersionMajor => new Version(StrideVersion.PublicVersion).ToString(2);

        [NotNull]
        public static string AnswersUrl => "https://gamedev.stackexchange.com/tags/stride"; // #706

        [NotNull]
        public static string DocumentationUrl => $"https://doc.stride3d.net/{EditorVersionMajor}/";

        [NotNull]
        public static string ForumsUrl => "https://github.com/stride3d/stride/discussions";

        [NotNull]
        public static string ReportIssueUrl => "https://github.com/stride3d/stride/issues/";
    }
}
