// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Editor.CrashReport;
using Stride.LauncherApp.Services;

namespace Stride.LauncherApp
{
    internal class CrashReportSettings : ICrashEmailSetting
    {
        public CrashReportSettings()
        {
            Email = GameStudioSettings.CrashReportEmail;
            StoreCrashEmail = !string.IsNullOrEmpty(Email);
        }

        public bool StoreCrashEmail { get; set; }

        public string Email { get; set; }

        public void Save()
        {
            GameStudioSettings.CrashReportEmail = Email;
        }
    }
}
