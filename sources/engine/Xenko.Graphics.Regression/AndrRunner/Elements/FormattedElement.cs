// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright 2011-2012 Xamarin Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System;

using Android.Content;
using Android.Views;
using Android.Widget;

using MonoDroid.Dialog;

namespace Android.NUnitLite.UI {
	
	class FormattedElement : StringElement {
		
        private new TextView _caption;
        private new TextView _text;
		
		public FormattedElement (string caption) : base (caption)
		{
		}
				
		public string Indicator {
			get; set;
		}
		
		public override View GetView (Context context, View convertView, ViewGroup parent)
		{
			var view = convertView as RelativeLayout;
			
			if (view == null)
				view = new RelativeLayout(context);
						
            var parms = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent,
                                                        ViewGroup.LayoutParams.WrapContent);
            parms.SetMargins(5, 3, 5, 0);
            parms.AddRule(LayoutRules.AlignParentLeft);

			_caption = new TextView (context);
			SetCaption (Caption);
		    view.RemoveAllViews();
            view.AddView(_caption, parms);
			
			if (!String.IsNullOrWhiteSpace (Indicator)) {
	            var tparms = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent,
	                                                         ViewGroup.LayoutParams.WrapContent);
	            tparms.SetMargins(5, 3, 5, 5);
	            tparms.AddRule(LayoutRules.CenterVertical);
				tparms.AddRule(LayoutRules.AlignParentRight);
	
	            _text = new TextView (context) {
					Text = Indicator,
					TextSize = 22f
				};
	            view.AddView(_text, tparms);
			}
			return view;
		}
		
		public void SetCaption (string html)
		{
			_caption.SetText (Android.Text.Html.FromHtml (html), TextView.BufferType.Spannable);
		}
	}
}
