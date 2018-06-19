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

using MonoDroid.Dialog;

namespace Android.NUnitLite.UI {
	
	public class ActivityElement : StringElement {
		
		Type activity;
		
		public ActivityElement (string name, Type type) : base (name)
		{
			activity = type;
			Value = ">"; // hint there's something more to show
		}
		
		public override View GetView (Context context, View convertView, ViewGroup parent)
		{
			View view = base.GetView (context, convertView, parent);
			view.Click += delegate {
				Intent intent = new Intent (context, activity);
				intent.AddFlags (ActivityFlags.NewTask);			
				context.StartActivity (intent);
			};
			return view;
		}
	}
}

