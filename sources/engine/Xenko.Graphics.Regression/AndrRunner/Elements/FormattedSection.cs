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

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using MonoDroid.Dialog;

namespace Android.NUnitLite.UI {
	
	// can't really name it HtmlSection wrt HtmlElement ;-)
	public class FormattedSection : Section {
			
		public FormattedSection (string html) 
			: base (html)
		{
		}
		
		public override View GetView (Context context, View convertView, ViewGroup parent)
		{
			TextView tv = new TextView (context);
			tv.TextSize = 20f;
			tv.SetText (Android.Text.Html.FromHtml (Caption), TextView.BufferType.Spannable);

			var parms = new RelativeLayout.LayoutParams (ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
			parms.AddRule (LayoutRules.CenterHorizontal);
			
			RelativeLayout view = new RelativeLayout (context, null, Android.Resource.Attribute.ListSeparatorTextViewStyle);
			view.AddView (tv, parms);
			return view;
		}
	}
}

