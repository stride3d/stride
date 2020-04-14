// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Xunit;
using Stride.Core.Settings;

namespace Stride.Core.Design.Tests
{
    class ValueSettingsKeys
    {
        public static SettingsKey<int> IntValue;
        public static SettingsKey<double> DoubleValue;
        public static SettingsKey<string> StringValue;

        public static void Initialize()
        {
            IntValue = new SettingsKey<int>("Test/Simple/IntValue", TestSettings.SettingsContainer, 10);
            DoubleValue = new SettingsKey<double>("Test/Simple/DoubleValue", TestSettings.SettingsContainer, 3.14);
            StringValue = new SettingsKey<string>("Test/Simple/StringValue", TestSettings.SettingsContainer, "Test string");
            Console.WriteLine(@"Static settings keys initialized (ValueSettingsKeys)");
        }
    }

    class ListSettingsKeys
    {
        public static SettingsKey<List<int>> IntList;
        public static SettingsKey<List<double>> DoubleList;
        public static SettingsKey<List<string>> StringList;

        public static void Initialize()
        {
            IntList = new SettingsKey<List<int>>("Test/Lists/IntList", TestSettings.SettingsContainer, Enumerable.Empty<int>().ToList());
            DoubleList = new SettingsKey<List<double>>("Test/Lists/DoubleList", TestSettings.SettingsContainer, new[] { 2.0, 6.0, 9.0 }.ToList());
            StringList = new SettingsKey<List<string>>("Test/Lists/StringList", TestSettings.SettingsContainer, new[] { "String 1", "String 2", "String 3" }.ToList());
            Console.WriteLine(@"Static settings keys initialized (ListSettingsKeys)");
        }
    }

    public class TestSettings
    {
        public static Guid SessionGuid = Guid.NewGuid();
        public static SettingsContainer SettingsContainer = new SettingsContainer();

        public static string TempPath(string file)
        {
            var dir = Path.Combine(Path.GetTempPath(), SessionGuid.ToString());
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, file);
        }

        public TestSettings()
        {
            SettingsContainer.ClearSettings();
        }

        [Fact]
        public void TestSettingsInitialization()
        {
            ValueSettingsKeys.Initialize();
            Assert.Equal(10, ValueSettingsKeys.IntValue.GetValue());
            Assert.Equal(3.14, ValueSettingsKeys.DoubleValue.GetValue());
            Assert.Equal("Test string", ValueSettingsKeys.StringValue.GetValue());
        }

        [Fact]
        public void TestSettingsWrite()
        {
            ValueSettingsKeys.Initialize();
            ValueSettingsKeys.IntValue.SetValue(20);
            ValueSettingsKeys.DoubleValue.SetValue(6.5);
            ValueSettingsKeys.StringValue.SetValue("New string");
            Assert.Equal(20, ValueSettingsKeys.IntValue.GetValue());
            Assert.Equal(6.5, ValueSettingsKeys.DoubleValue.GetValue());
            Assert.Equal("New string", ValueSettingsKeys.StringValue.GetValue());

            ValueSettingsKeys.IntValue.SetValue(30);
            ValueSettingsKeys.DoubleValue.SetValue(9.1);
            ValueSettingsKeys.StringValue.SetValue("Another string");
            Assert.Equal(30, ValueSettingsKeys.IntValue.GetValue());
            Assert.Equal(9.1, ValueSettingsKeys.DoubleValue.GetValue());
            Assert.Equal("Another string", ValueSettingsKeys.StringValue.GetValue());
        }

        [Fact]
        public void TestSettingsValueChanged()
        {
            // We use an array to avoid a closure issue (resharper)
            int[] settingsChangedCount = { 0 };

            ValueSettingsKeys.Initialize();
            ListSettingsKeys.Initialize();
            ValueSettingsKeys.IntValue.ChangesValidated += (s, e) => ++settingsChangedCount[0];
            ValueSettingsKeys.DoubleValue.ChangesValidated += (s, e) => ++settingsChangedCount[0];
            ValueSettingsKeys.StringValue.ChangesValidated += (s, e) => ++settingsChangedCount[0];

            ValueSettingsKeys.IntValue.SetValue(20);
            ValueSettingsKeys.DoubleValue.SetValue(6.5);
            ValueSettingsKeys.StringValue.SetValue("New string");
            SettingsContainer.CurrentProfile.ValidateSettingsChanges();
            Assert.Equal(3, settingsChangedCount[0]);
            settingsChangedCount[0] = 0;
        }

        [Fact]
        public void TestSettingsList()
        {
            ListSettingsKeys.Initialize();
            var intList = ListSettingsKeys.IntList.GetValue();
            intList.Add(1);
            intList.Add(3);
            var doubleList = ListSettingsKeys.DoubleList.GetValue();
            doubleList.Remove(2.0);
            doubleList.RemoveAt(0);
            var stringList = ListSettingsKeys.StringList.GetValue();
            stringList.Insert(1, "String 1.5");
            stringList[2] = "String 2.0";

            intList = ListSettingsKeys.IntList.GetValue();
            Assert.Equal(intList, new[] { 1, 3 });
            doubleList = ListSettingsKeys.DoubleList.GetValue();
            Assert.Equal(doubleList, new[] { 9.0 });
            stringList = ListSettingsKeys.StringList.GetValue();
            Assert.Equal(stringList, new[] { "String 1", "String 1.5", "String 2.0", "String 3" });
        }

        [Fact]
        public void TestSettingsSaveAndLoad()
        {
            TestSettingsWrite();
            TestSettingsList();
            SettingsContainer.SaveSettingsProfile(SettingsContainer.CurrentProfile, TempPath("TestSettingsSaveAndLoad.txt"));
            SettingsContainer.LoadSettingsProfile(TempPath("TestSettingsSaveAndLoad.txt"), true);

            Assert.Equal(30, ValueSettingsKeys.IntValue.GetValue());
            Assert.Equal(9.1, ValueSettingsKeys.DoubleValue.GetValue());
            Assert.Equal("Another string", ValueSettingsKeys.StringValue.GetValue());

            var intList = ListSettingsKeys.IntList.GetValue();
            Assert.Equal(intList, new[] { 1, 3 });
            var doubleList = ListSettingsKeys.DoubleList.GetValue();
            Assert.Equal(doubleList, new[] { 9.0 });
            var stringList = ListSettingsKeys.StringList.GetValue();
            Assert.Equal(stringList, new[] { "String 1", "String 1.5", "String 2.0", "String 3" });
        }

        const string TestSettingsLoadFileText =
@"!SettingsFile
Settings:
    Test/Lists/DoubleList:
        - 9
    Test/Lists/IntList:
        - 1
        - 3
    Test/Lists/StringList:
        - String 1
        - String 1.5
        - String 2
        - String 3
    Test/Simple/DoubleValue: 25.0
    Test/Simple/IntValue: 45
    Test/Simple/StringValue: 07/25/2004 18:18:00";

        [Fact]
        public void TestSettingsLoad()
        {
            using (var writer = new StreamWriter(TempPath("TestSettingsLoad.txt")))
            {
                writer.Write(TestSettingsLoadFileText);
            }
            SettingsContainer.LoadSettingsProfile(TempPath("TestSettingsLoad.txt"), true);

            ValueSettingsKeys.Initialize();
            ListSettingsKeys.Initialize();
            Assert.Equal(45, ValueSettingsKeys.IntValue.GetValue());
            Assert.Equal(25.0, ValueSettingsKeys.DoubleValue.GetValue());
            Assert.Equal(new DateTime(2004, 7, 25, 18, 18, 00).ToString(CultureInfo.InvariantCulture), ValueSettingsKeys.StringValue.GetValue());
            var intList = ListSettingsKeys.IntList.GetValue();
            Assert.Equal(intList, new[] { 1, 3 });
            var doubleList = ListSettingsKeys.DoubleList.GetValue();
            Assert.Equal(doubleList, new[] { 9.0 });
            var stringList = ListSettingsKeys.StringList.GetValue();
            Assert.Equal(stringList, new[] { "String 1", "String 1.5", "String 2", "String 3" });
        }

        const string TestSettingsValueChangedOnLoadText =
@"!SettingsFile
Settings:
    Test/Lists/DoubleList: # Same as default
        - 2.0
        - 6.0
        - 9.0
    Test/Lists/IntList:
        - 1
    Test/Simple/DoubleValue: 3.14 # Same as default
    Test/Simple/IntValue: 45 # Different from default
    # String value unset";
        
        [Fact]
        public void TestSettingsValueChangedOnLoad()
        {
            ValueSettingsKeys.Initialize();
            ListSettingsKeys.Initialize();
            using (var writer = new StreamWriter(TempPath("TestSettingsValueChangedOnLoadText.txt")))
            {
                writer.Write(TestSettingsValueChangedOnLoadText);
            }

            int intValueChangeCount = 0;
            int doubleValueChangeCount = 0;
            int stringValueChangeCount = 0;
            int intListChangeCount = 0;
            int doubleListChangeCount = 0;
            int stringListChangeCount = 0;
            ValueSettingsKeys.IntValue.ChangesValidated += (s, e) => ++intValueChangeCount;
            ValueSettingsKeys.DoubleValue.ChangesValidated += (s, e) => ++doubleValueChangeCount;
            ValueSettingsKeys.StringValue.ChangesValidated += (s, e) => ++stringValueChangeCount;
            ListSettingsKeys.IntList.ChangesValidated += (s, e) => ++intListChangeCount;
            ListSettingsKeys.DoubleList.ChangesValidated += (s, e) => ++doubleListChangeCount;
            ListSettingsKeys.StringList.ChangesValidated += (s, e) => ++stringListChangeCount;

            SettingsContainer.LoadSettingsProfile(TempPath("TestSettingsValueChangedOnLoadText.txt"), true);
            SettingsContainer.CurrentProfile.ValidateSettingsChanges();

            Assert.Equal(1, intValueChangeCount);
            Assert.Equal(0, doubleValueChangeCount);
            Assert.Equal(0, stringValueChangeCount);
            Assert.Equal(1, intListChangeCount);
            Assert.Equal(0, doubleListChangeCount);
            Assert.Equal(0, stringListChangeCount);
        }

        const string TestSettingsLoadWrongTypeFileText =
@"!SettingsFile
Settings:
    Test/Lists/DoubleList:
        - String 1
        - String 2    
    Test/Lists/IntList: This is a string
    Test/Simple/DoubleValue: This is a string
    Test/Simple/IntValue:
        - String 1
        - String 2";

        [Fact]
        public void TestSettingsLoadWrongType()
        {
            using (var writer = new StreamWriter(TempPath("TestSettingsLoadWrongType.txt")))
            {
                writer.Write(TestSettingsLoadWrongTypeFileText);
            }
            SettingsContainer.LoadSettingsProfile(TempPath("TestSettingsLoadWrongType.txt"), true);

            ValueSettingsKeys.Initialize();
            ListSettingsKeys.Initialize();
            Assert.Equal(ValueSettingsKeys.IntValue.DefaultValue, ValueSettingsKeys.IntValue.GetValue());
            Assert.Equal(ValueSettingsKeys.DoubleValue.DefaultValue, ValueSettingsKeys.DoubleValue.GetValue());
            Assert.Equal(ValueSettingsKeys.StringValue.DefaultValue, ValueSettingsKeys.StringValue.GetValue());
            var intList = ListSettingsKeys.IntList.GetValue();
            Assert.Equal(intList, ListSettingsKeys.IntList.DefaultValue);
            var doubleList = ListSettingsKeys.DoubleList.GetValue();
            Assert.Equal(doubleList, ListSettingsKeys.DoubleList.DefaultValue);
        }

        const string TestSettingsFileModifiedText1 =
@"!SettingsFile
Settings:
    Test/Simple/IntValue: 55";

        const string TestSettingsFileModifiedText2 =
@"!SettingsFile
Settings:
    Test/Simple/IntValue: 75";

        [Fact]
        public void TestSettingsFileModified()
        {
            // NUnit does not support async tests so lets wrap this task into a synchronous operation
            var task = Task.Run(async () =>
                {
                    var tcs = new TaskCompletionSource<int>();
                    EventHandler<FileModifiedEventArgs> settingsModified = (s, e) => e.ReloadFile = true;
                    EventHandler<SettingsFileLoadedEventArgs> settingsLoaded = (s, e) => tcs.SetResult(0);
                    try
                    {
                        using (var writer = new StreamWriter(TempPath("TestSettingsFileModified.txt")))
                        {
                            writer.Write(TestSettingsFileModifiedText1);
                        }
                        SettingsContainer.LoadSettingsProfile(TempPath("TestSettingsFileModified.txt"), true);
                        SettingsContainer.CurrentProfile.MonitorFileModification = true;
                        SettingsContainer.CurrentProfile.FileModified += settingsModified;
                        ValueSettingsKeys.Initialize();
                        ListSettingsKeys.Initialize();
                        Assert.Equal(55, ValueSettingsKeys.IntValue.GetValue());

                        SettingsContainer.SettingsFileLoaded += settingsLoaded;

                        using (var writer = new StreamWriter(TempPath("TestSettingsFileModified.txt")))
                        {
                            writer.Write(TestSettingsFileModifiedText2);
                        }

                        // Gives some time to the file watcher to awake.
                        await tcs.Task;

                        Assert.Equal(75, ValueSettingsKeys.IntValue.GetValue());
                        SettingsContainer.SettingsFileLoaded -= settingsLoaded;
                    }
                    catch
                    {
                        SettingsContainer.SettingsFileLoaded -= settingsLoaded;
                    }
                });

            // Block while the task has not ended to ensure that no other test will start before this one ends.
            task.Wait();
        }
    }
}
