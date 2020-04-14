// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Stride.Framework.ViewModel;

namespace Stride.DebugTools
{
    /// <summary>
    /// Interaction logic for PropertyGridTestControl.xaml
    /// </summary>
    public partial class PropertyGridTest1Control : UserControl
    {
        public PropertyGridTest1Control()
        {
            InitializeComponent();

            PropertyItems = new[] { CreateSampleTree() };
            this.DataContext = this;
        }

        public object PropertyItems { get; private set; }

        private IViewModelNode CreateSampleTree()
        {
            MyDateTime now = MyDateTime.FromDateTime(DateTime.Now);

            var context = new ViewModelContext(new ViewModelGlobalContext());
            var contextUI = new ViewModelContext(new ViewModelGlobalContext());

            context.ChildrenPropertyEnumerators.Add(new ChildrenPropertyInfoEnumerator());
            // add some more here...

            var testModel = new ViewModelNode("Root", now);

            var view = ObservableViewModelNode.CreateObservableViewModel(contextUI, testModel);

            ObservableViewModelNode.Refresh(contextUI, context, new ViewModelState());

            return view;
        }
    }

    public struct MyDateTime
    {
        //
        // Summary:
        //     Gets the day of the month represented by this instance.
        //
        // Returns:
        //     The day component, expressed as a value between 1 and 31.
        public int Day { get; private set; }
        //
        // Summary:
        //     Gets the day of the week represented by this instance.
        //
        // Returns:
        //     A System.DayOfWeek enumerated constant that indicates the day of the week
        //     of this System.DateTime value.
        public DayOfWeek DayOfWeek { get; private set; }
        //
        // Summary:
        //     Gets the day of the year represented by this instance.
        //
        // Returns:
        //     The day of the year, expressed as a value between 1 and 366.
        public int DayOfYear { get; private set; }
        //
        // Summary:
        //     Gets the hour component of the date represented by this instance.
        //
        // Returns:
        //     The hour component, expressed as a value between 0 and 23.
        public int Hour { get; private set; }
        //
        // Summary:
        //     Gets a value that indicates whether the time represented by this instance
        //     is based on local time, Coordinated Universal Time (UTC), or neither.
        //
        // Returns:
        //     One of the System.DateTimeKind values. The default is System.DateTimeKind.Unspecified.
        public DateTimeKind Kind { get; private set; }
        //
        // Summary:
        //     Gets the milliseconds component of the date represented by this instance.
        //
        // Returns:
        //     The milliseconds component, expressed as a value between 0 and 999.
        public int Millisecond { get; private set; }
        //
        // Summary:
        //     Gets the minute component of the date represented by this instance.
        //
        // Returns:
        //     The minute component, expressed as a value between 0 and 59.
        public int Minute { get; private set; }
        //
        // Summary:
        //     Gets the month component of the date represented by this instance.
        //
        // Returns:
        //     The month component, expressed as a value between 1 and 12.
        public int Month { get; private set; }
        //
        // Summary:
        //     Gets the seconds component of the date represented by this instance.
        //
        // Returns:
        //     The seconds, between 0 and 59.
        public int Second { get; private set; }
        //
        // Summary:
        //     Gets the number of ticks that represent the date and time of this instance.
        //
        // Returns:
        //     The number of ticks that represent the date and time of this instance. The
        //     value is between DateTime.MinValue.Ticks and DateTime.MaxValue.Ticks.
        public long Ticks { get; private set; }
        //
        // Summary:
        //     Gets the time of day for this instance.
        //
        // Returns:
        //     A System.TimeSpan that represents the fraction of the day that has elapsed
        //     since midnight.
        public TimeSpan TimeOfDay { get; private set; }
        //
        // Summary:
        //     Gets the year component of the date represented by this instance.
        //
        // Returns:
        //     The year, between 1 and 9999.
        public int Year { get; private set; }

        public static MyDateTime FromDateTime(DateTime dateTime)
        {
            return new MyDateTime
            {
                Day = dateTime.Day,
                DayOfWeek = dateTime.DayOfWeek,
                DayOfYear = dateTime.DayOfYear,
                Hour = dateTime.Hour,
                Kind = dateTime.Kind,
                Millisecond= dateTime.Millisecond,
                Minute = dateTime.Minute,
                Month = dateTime.Month,
                Second = dateTime.Second,
                Ticks = dateTime.Ticks,
                TimeOfDay = dateTime.TimeOfDay,
                Year = dateTime.Year,
            };
        }
    }
}
