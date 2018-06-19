// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Android.Content;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.App;

namespace MonoDroid.Dialog
{
	public class EntryElement : Element, ITextWatcher
	{	
		public string Value
		{
			get { return _val; }
			set
			{
				_val = value;
                if (_entry != null && _entry.Text != value)
                {
                    _entry.Text = value;
                    if (ValueChanged != null)
                        ValueChanged(this, EventArgs.Empty);
                }
			}
		}
		
		public event EventHandler ValueChanged;

		public EntryElement(string caption, string value)
            : this(caption, value, (int)DroidResources.ElementLayout.dialog_textfieldright)
		{
			
		}
		
		public EntryElement(string caption, string hint,string value) : this(caption,value)
		{
			Hint = hint;
		}

        public EntryElement(string caption, string @value, int layoutId)
            : base(caption, layoutId)
        {
            _val = @value;
            Lines = 1;
        }
        
        public override string Summary()
		{
			return _val;
		}

        public bool Password { get; set; }
        public bool Numeric { get; set; }
        public string Hint { get; set; }
        public int Lines { get; set; }

        protected EditText _entry;
        private string _val;
		protected Action _entryClicked { get;set; }
		

        public override View GetView(Context context, View convertView, ViewGroup parent)
		{
            Log.Debug("MDD", "EntryElement: GetView: ConvertView: " + ((convertView == null) ? "false" : "true") +
                " Value: " + Value + " Hint: " + Hint + " Password: " + (Password ? "true" : "false"));
			
			
            TextView label;
            var view = DroidResources.LoadStringEntryLayout(context, convertView, parent, LayoutId, out label, out _entry);
            if (view != null)
            {
                // Warning! Crazy ass hack ahead!
                // since we can't know when out convertedView was was swapped from inside us, we store the
                // old textwatcher in the tag element so it can be removed!!!! (barf, rech, yucky!)
                if (_entry.Tag != null)
                    _entry.RemoveTextChangedListener((ITextWatcher)_entry.Tag);

                _entry.Text = this.Value;
                _entry.Hint = this.Hint;

                if (this.Password)
                    _entry.InputType = (InputTypes.ClassText | InputTypes.TextVariationPassword);
                else if (this.Numeric)
                    _entry.InputType = (InputTypes.ClassNumber | InputTypes.NumberFlagDecimal | InputTypes.NumberFlagSigned);
                else
                    _entry.InputType = InputTypes.ClassText;

                // continuation of crazy ass hack, stash away the listener value so we can look it up later
                _entry.Tag = this;
                _entry.AddTextChangedListener(this);

                label.Text = (label != null) ? Caption: string.Empty;
            }
			return view;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				//_entry.Dispose();
				_entry = null;
			}
		}

		public override bool Matches(string text)
		{
			return (Value != null ? Value.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) != -1 : false) || base.Matches(text);
		}

        public void OnTextChanged(Java.Lang.ICharSequence s, int start, int before, int count)
        {
            this.Value = s.ToString();
        }

        public void AfterTextChanged(IEditable s)
        {
			Console.Write("foo");
            // nothing needed
        }

        public void BeforeTextChanged(Java.Lang.ICharSequence s, int start, int count, int after)
        {
			Console.Write("foo");
            // nothing needed
        }
		
		public override void Selected ()
		{
			base.Selected ();
			
			if(_entry != null) {
				var context = this.GetContext();
				var entryDialog = new AlertDialog.Builder(context)
					.SetTitle("Enter Text")
					.SetView(_entry)
					.SetPositiveButton("OK", (o, e) => {
							this.Value = _entry.Text;
					})
					.Create();
				
				entryDialog.Show();
			}
		}
    }
}
