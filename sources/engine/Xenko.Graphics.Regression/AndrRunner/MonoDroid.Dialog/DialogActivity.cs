// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Android.App;
using Android.OS;
using Android.Widget;

namespace MonoDroid.Dialog
{
    public class DialogInstanceData : Java.Lang.Object
    {
        public DialogInstanceData()
        {
            _dialogState = new Dictionary<string, string>();
        }

        private Dictionary<String, String> _dialogState;
    }

	public class DialogActivity : ListActivity
	{
		public RootElement Root { get; set; }
        private DialogHelper Dialog { get; set; }
		
		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
            Dialog = new DialogHelper(this, this.ListView, this.Root);

            if (this.LastNonConfigurationInstance != null)
            {
                // apply value changes that are saved
            }
        }

        public override Java.Lang.Object OnRetainNonConfigurationInstance()
        {
            return null;
        }
		
		public void ReloadData()
		{
			if(Root == null) {
				return;
			}
			
			this.Dialog.ReloadData();
			
			
		}
	}
}
