// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Settings;

namespace Stride.Core.Assets.Editor.Settings
{
#if DEBUG
    public static class DebugTestSettings
    {
        public struct StructSettings
        {
            public bool Bool;
            public int Integer;
            public double Double;
            public string String;
        }

        public static SettingsContainer Container = EditorSettings.SettingsContainer;

        public static SettingsKey<bool> BoolValue = new SettingsKey<bool>("Test/Values/BoolValue", Container, true);

        public static SettingsKey<int> IntValue = new SettingsKey<int>("Test/Values/IntValue", Container, 10);

        public static SettingsKey<double> DoubleValue = new SettingsKey<double>("Test/Values/DoubleValue", Container, 3.14);

        public static SettingsKey<string> StringValue = new SettingsKey<string>("Test/Values/StringValue", Container, "Test string");

        public static SettingsKey<StructSettings> StuctValue = new SettingsKey<StructSettings>("Test/Values/StructValue", Container, new StructSettings { Bool = true, Integer = 6, Double = 3.14, String = "Struct!" });

        public static SettingsKey<List<int>> IntList = new SettingsKey<List<int>>("Test/Lists/IntList", Container, new List<int> { 2, 6 });

        public static SettingsKey<List<double>> DoubleList = new SettingsKey<List<double>>("Test/Lists/DoubleList", Container, new List<double> { 3.1, 3.2 });

        public static SettingsKey<List<string>> StringList = new SettingsKey<List<string>>("Test/Lists/StringList", Container, new List<string> { "aaa", "bbb" });

        public static SettingsKey<List<StructSettings>> StructList = new SettingsKey<List<StructSettings>>("Test/Lists/StructList", Container, new List<StructSettings> { new StructSettings { Bool = true, Integer = 6, Double = 3.14, String = "Struct!" } });

        public static void Initialize()
        {
            BoolValue.ChangesValidated += (s, e) => Console.WriteLine(@"BoolValue ChangesValidated");
            IntValue.ChangesValidated += (s, e) => Console.WriteLine(@"IntValue ChangesValidated");
            DoubleValue.ChangesValidated += (s, e) => Console.WriteLine(@"DoubleValue ChangesValidated");
            StringValue.ChangesValidated += (s, e) => Console.WriteLine(@"StringValue ChangesValidated");
            StuctValue.ChangesValidated += (s, e) => Console.WriteLine(@"StuctValue ChangesValidated");
            IntList.ChangesValidated += (s, e) => Console.WriteLine(@"IntList ChangesValidated");
            DoubleList.ChangesValidated += (s, e) => Console.WriteLine(@"DoubleList ChangesValidated");
            StringList.ChangesValidated += (s, e) => Console.WriteLine(@"StringList ChangesValidated");
            StructList.ChangesValidated += (s, e) => Console.WriteLine(@"StructList ChangesValidated");
        }
    }
#endif
}
