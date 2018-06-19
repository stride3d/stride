// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace MonoDroid.Dialog
{
    public class FloatElement : Element, SeekBar.IOnSeekBarChangeListener
	{
		public bool ShowCaption;
		public int Value;
		public int MinValue, MaxValue;
	    public Bitmap Left;
	    public Bitmap Right;

        public FloatElement(string caption)
            : this(caption, (int)DroidResources.ElementLayout.dialog_floatimage)
        {
            Value = 0;
            MinValue = 0;
            MaxValue = 10;
        }

        public FloatElement(string caption, int layoutId)
            : base(caption, layoutId)
        {
            Value = 0;
            MinValue = 0;
            MaxValue = 10;
        }
        
        public FloatElement(Bitmap left, Bitmap right, int value)
            : this(left, right, value, (int)DroidResources.ElementLayout.dialog_floatimage)
        {
        }

        public FloatElement(Bitmap left, Bitmap right, int value, int layoutId)
            : base(string.Empty, layoutId)
        {
            Left = left;
            Right = right;
            MinValue = 0;
            MaxValue = 10;
            Value = value;
        }

        public override View GetView(Context context, View convertView, ViewGroup parent)
        {
            TextView label;
            SeekBar slider;
            ImageView left;
            ImageView right;

            View view = DroidResources.LoadFloatElementLayout(context, convertView, parent, LayoutId, out label, out slider, out left, out right);

            if (view != null)
            {
                if (left != null)
                {
                    if (Left != null)
                        left.SetImageBitmap(Left);
                    else
                        left.Visibility = ViewStates.Gone;
                }
                if (right != null)
                {
                    if (Right != null)
                        right.SetImageBitmap(Right);
                    else
                        right.Visibility = ViewStates.Gone;
                }
                slider.Max = MaxValue - MinValue;
                slider.Progress = Value - MinValue;
                slider.SetOnSeekBarChangeListener(this);
                if (label != null)
                {
                    if (ShowCaption)
                        label.Text = Caption;
                    else
                        label.Visibility = ViewStates.Gone;
                }
            }
            else
            {
                Android.Util.Log.Error("FloatElement", "GetView failed to load template view");
            }

            return view;
        }

        void SeekBar.IOnSeekBarChangeListener.OnProgressChanged(SeekBar seekBar, int progress, bool fromUser)
        {
            Value = MinValue + progress;
        }

        void SeekBar.IOnSeekBarChangeListener.OnStartTrackingTouch(SeekBar seekBar)
        {
        }

        void SeekBar.IOnSeekBarChangeListener.OnStopTrackingTouch(SeekBar seekBar)
        {
        }
    }
}
