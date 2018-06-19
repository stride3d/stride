// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Android.Content;
using Android.Views;
using Android.Widget;

namespace MonoDroid.Dialog
{
    public abstract class BoolElement : Element
    {
        private bool _val;

        public bool Value
        {
            get { return _val; }
            set
            {
                if (_val != value)
                {
                    _val = value;
                    if (ValueChanged != null)
                        ValueChanged(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler ValueChanged;

        public BoolElement(string caption, bool value) : base(caption)
        {
            _val = value;
        }

        public BoolElement(string caption, bool value, int layoutId)
            : base(caption, layoutId)
        {
            _val = value;
        }
        
        public override string Summary()
        {
            return _val ? "On" : "Off";
        }
    }

    /// <summary>
    /// Used to display toggle button on the screen.
    /// </summary>
    public class BooleanElement : BoolElement, CompoundButton.IOnCheckedChangeListener
    {
        private ToggleButton _toggleButton;
        private TextView _caption;
        private TextView _subCaption;

        public BooleanElement(string caption, bool value)
            : base(caption, value, (int) DroidResources.ElementLayout.dialog_onofffieldright)
        {
        }

        public BooleanElement(string caption, bool value, int layoutId)
            : base(caption, value, layoutId)
        {
        }

        public override View GetView(Context context, View convertView, ViewGroup parent)
        {
            View toggleButtonView;
            View view = DroidResources.LoadBooleanElementLayout(context, convertView, parent, LayoutId, out _caption, out _subCaption, out toggleButtonView);

            if (view != null)
            {
                _caption.Text = Caption;
                _toggleButton = toggleButtonView as ToggleButton;
                _toggleButton.SetOnCheckedChangeListener(null);
                _toggleButton.Checked = Value;
                _toggleButton.SetOnCheckedChangeListener(this);
            }
            return view;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                //_toggleButton.Dispose();
                _toggleButton = null;
                //_caption.Dispose();
                _caption = null;
            }
        }

        public void OnCheckedChanged(CompoundButton buttonView, bool isChecked)
        {
            this.Value = isChecked;
        }
    }
}
