// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Android.Content;
using Android.Views;

namespace MonoDroid.Dialog
{
    public class MultilineElement : EntryElement
    {
        public int Lines { get; set; }
		public int MaxLength {get;set;}

        public MultilineElement(string caption, string value)
            : base(caption, value, (int)DroidResources.ElementLayout.dialog_textarea)
        {
            Lines = 3;
        }

        public override View GetView(Context context, View convertView, ViewGroup parent)
        {
            View view = DroidResources.LoadMultilineElementLayout(context, convertView, parent, LayoutId, out _entry);
            if (_entry != null)
            {
                _entry.SetLines(Lines);
                _entry.Text = Value;
                _entry.Hint = Caption;
				_entry.TextChanged += delegate(object sender, Android.Text.TextChangedEventArgs e) {
					if(MaxLength > 0 && _entry.Text.Length > MaxLength)
						_entry.Text = _entry.Text.Substring(0,MaxLength);
				};
            }
            return view;
        }

    }
}
