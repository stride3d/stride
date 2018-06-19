// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Android.App;
using Android.Content;

namespace MonoDroid.Dialog
{
    public class DateTimeElement : StringElement
    {
        public DateTime DateValue
        {
            get { return DateTime.Parse(Value); }
            set { Value = Format(value); }
        }

        public DateTimeElement(string caption, DateTime date)
            : base(caption)
        {
            DateValue = date;
        }

        public DateTimeElement(string caption, DateTime date, int layoutId)
            : base(caption, layoutId)
        {
            DateValue = date;
        }
        
        public virtual string Format(DateTime dt)
        {
            return dt.ToShortDateString() + " " + dt.ToShortTimeString();
        }
    }

    public class DateElement : DateTimeElement
    {
        public DateElement(string caption, DateTime date)
            : base(caption, date)
        {
            this.Click = delegate { EditDate(); };
        }

        public DateElement(string caption, DateTime date, int layoutId)
            : base(caption, date, layoutId)
        {
            this.Click = delegate { EditDate(); };
        }

        public override string Format(DateTime dt)
        {
            return dt.ToShortDateString();
        }

        // the event received when the user "sets" the date in the dialog
        void OnDateSet(object sender, DatePickerDialog.DateSetEventArgs e)
        {
            DateTime current = DateValue;
            DateValue = new DateTime(e.Date.Year, e.Date.Month, e.Date.Day, current.Hour, current.Minute, 0);
        }

        private void EditDate()
        {
            Context context = GetContext();
            if (context == null)
            {
                Android.Util.Log.Warn("DateElement", "No Context for Edit");
                return;
            }
            DateTime val = DateValue;
            new DatePickerDialog(context, OnDateSet, val.Year, val.Month - 1, val.Day).Show();
        }
    }

    public class TimeElement : DateTimeElement
    {
        public TimeElement(string caption, DateTime date)
            : base(caption, date)
        {
            this.Click = delegate { EditDate(); };
        }

        public TimeElement(string caption, DateTime date, int layoutId)
            : base(caption, date, layoutId)
        {
            this.Click = delegate { EditDate(); };
        }

        public override string Format(DateTime dt)
        {
            return dt.ToShortTimeString();
        }

        // the event received when the user "sets" the date in the dialog
        void OnDateSet(object sender, TimePickerDialog.TimeSetEventArgs e)
        {
            DateTime current = DateValue;
            DateValue = new DateTime(current.Year, current.Month, current.Day, e.HourOfDay, e.Minute, 0);
        }

        private void EditDate()
        {
            Context context = GetContext();
            if (context == null)
            {
                Android.Util.Log.Warn("TimeElement", "No Context for Edit");
                return;
            }
            DateTime val = DateValue;
            // TODO: get the current time setting for thge 24 hour clock
            new TimePickerDialog(context, OnDateSet, val.Hour, val.Minute, false).Show();
        }
    }
}
