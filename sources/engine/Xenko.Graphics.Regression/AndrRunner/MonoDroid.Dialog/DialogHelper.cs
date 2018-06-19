// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Android.Content;
using Android.Widget;

namespace MonoDroid.Dialog
{
    public class DialogHelper
    {
        private Context context;
        private RootElement formLayer;

        //public event Action<Section, Element> ElementClick;
        //public event Action<Section, Element> ElementLongClick;

        public RootElement Root { get; set; }

        private DialogAdapter DialogAdapter { get; set; }

        public DialogHelper(Context context, ListView dialogView, RootElement root)
        {
            this.Root = root;
            this.Root.Context = context;

            dialogView.Adapter = this.DialogAdapter = new DialogAdapter(context, this.Root);
            dialogView.ItemClick += ListView_ItemClick;
            // FIXME: should I comment out this? some branch seems to have done it.
            dialogView.ItemLongClick += ListView_ItemLongClick;;
            dialogView.Tag = root;
        }

        void ListView_ItemLongClick (object sender, AdapterView.ItemLongClickEventArgs e)
        {
			var elem = this.DialogAdapter.ElementAtIndex(e.Position);
            if (elem != null && elem.LongClick != null) {
				elem.LongClick();
			}
        }

        void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            var elem = this.DialogAdapter.ElementAtIndex(e.Position);
			if(elem != null)
				elem.Selected();
        }
		
		public void ReloadData()
		{
			if(Root == null) {
				return;
			}
			
			this.DialogAdapter.ReloadData();
		}
		
    }
}
