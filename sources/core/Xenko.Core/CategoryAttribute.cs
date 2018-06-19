// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_PLATFORM_UWP 
namespace System.ComponentModel
{
    public class CategoryAttribute : Attribute
    {
        public CategoryAttribute()
        {
        }

        public CategoryAttribute(string category)
        {
            this.Category = category;
        }

        public string Category { get; private set; }
    }
}
#endif
