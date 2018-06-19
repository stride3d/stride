// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace MonoDroid.Dialog
{
	public class SectionAdapter : BaseAdapter<Element>
	{
		public SectionAdapter (Section section)
		{
			this.Section = section;
		}

		public Context Context { get; set; }

		public Section Section { get; private set; }

		public override int Count {
			get { return this.Section.Elements.Count; }
		}

		public override Element this [int position] {
			get {
				return Section.Elements [position];
			}
		}

		public override int ViewTypeCount {
			get { return this.Section.ElementViewTypeCount;	}
		}

		public override int GetItemViewType (int position)
		{
			return this.Section.GetElementViewType (this [position]);
		}			

		public override long GetItemId (int position)
		{
			return position;
		}

		public override View GetView (int position, View convertView, ViewGroup parent)
		{
			var element = Section.Elements [position];
			var view = element.GetView (this.Context, convertView, parent);
			
			return view;
		}
	}
}
