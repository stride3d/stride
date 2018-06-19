// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using Android.Views;

namespace MonoDroid.Dialog
{
    public class ViewElement : IEnumerable<View>
    {
        public ViewElement(object o, View view, bool b)
        {
            
        }

        public IEnumerator<View> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
