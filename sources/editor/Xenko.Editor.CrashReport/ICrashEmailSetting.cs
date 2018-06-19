// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Editor.CrashReport
{
    public interface ICrashEmailSetting
    {
        bool StoreCrashEmail { get; set; }

        string Email { get; set; }

        void Save();
    }
}
