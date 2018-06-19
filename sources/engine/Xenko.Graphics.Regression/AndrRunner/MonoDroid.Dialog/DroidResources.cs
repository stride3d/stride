// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace MonoDroid.Dialog
{
    public static class DroidResources
    {
        public enum ElementLayout: int
        {
            dialog_boolfieldleft,
            dialog_boolfieldright,
            dialog_boolfieldsubleft,
            dialog_boolfieldsubright,

            dialog_button,
            dialog_datefield,
            dialog_fieldsetlabel,
            dialog_labelfieldbelow,
            dialog_labelfieldright,
            dialog_onofffieldright,
            dialog_panel,
            dialog_root,
            dialog_selectlist,
            dialog_selectlistfield,
            dialog_textarea,

            dialog_floatimage,

            dialog_textfieldbelow,
            dialog_textfieldright,
        }

        public static View LoadFloatElementLayout(Context context, View convertView, ViewGroup parent, int layoutId, out TextView label, out SeekBar slider, out ImageView left, out ImageView right)
        {
            View layout = convertView ?? LoadLayout(context, parent, layoutId);
            if (layout != null)
            {
                label = layout.FindViewById<TextView>(context.Resources.GetIdentifier("dialog_LabelField", "id", context.PackageName));
                slider = layout.FindViewById<SeekBar>(context.Resources.GetIdentifier("dialog_SliderField", "id", context.PackageName));
                left = layout.FindViewById<ImageView>(context.Resources.GetIdentifier("dialog_ImageLeft", "id", context.PackageName));
                right = layout.FindViewById<ImageView>(context.Resources.GetIdentifier("dialog_ImageRight", "id", context.PackageName));
            }
            else
            {
                label = null; 
                slider = null;
                left = right = null;
            }
            return layout;
        }


        private static View LoadLayout(Context context, ViewGroup parent, int layoutId)
        {
            try
            {
                LayoutInflater inflater = LayoutInflater.FromContext(context);
                if (_resourceMap.ContainsKey((ElementLayout)layoutId))
                {
                    string layoutName = _resourceMap[(ElementLayout)layoutId];
                    int layoutIndex = context.Resources.GetIdentifier(layoutName, "layout", context.PackageName);
                    return inflater.Inflate(layoutIndex, parent, false);
                }
                else
                {
                    // TODO: figure out what context to use to get this right, currently doesn't inflate application resources
                    return inflater.Inflate(layoutId, parent, false);
                }
            }
            catch (InflateException ex)
            {
                Log.Error("MDD", "Inflate failed: " + ex.Cause.Message);
            }
            catch (Exception ex)
            {
                Log.Error("MDD", "LoadLayout failed: " + ex.Message);
            }
            return null;
        }

        public static View LoadStringElementLayout(Context context, View convertView, ViewGroup parent, int layoutId, out TextView label, out TextView value)
        {
            View layout = convertView ?? LoadLayout(context, parent, layoutId);
            if (layout != null)
            {
                label = layout.FindViewById<TextView>(context.Resources.GetIdentifier("dialog_LabelField", "id", context.PackageName));
                value = layout.FindViewById<TextView>(context.Resources.GetIdentifier("dialog_ValueField", "id", context.PackageName));
				if(label == null || value == null)
				{
					layout = LoadLayout(context, parent, layoutId);
					label = layout.FindViewById<TextView>(context.Resources.GetIdentifier("dialog_LabelField", "id", context.PackageName));
					value = layout.FindViewById<TextView>(context.Resources.GetIdentifier("dialog_ValueField", "id", context.PackageName));
				}
            }
            else
            {
                label = null;
                value = null;
            }
            return layout;
        }

        public static View LoadButtonLayout(Context context, View convertView, ViewGroup parent, int layoutId, out Button button)
        {
            View layout = LoadLayout(context, parent, layoutId);
            if (layout != null)
            {
                button = layout.FindViewById<Button>(context.Resources.GetIdentifier("dialog_Button", "id", context.PackageName));
            }
            else
            {
                button = null;
            }
            return layout;
        }

        public static View LoadMultilineElementLayout(Context context, View convertView, ViewGroup parent, int layoutId, out EditText value)
        {
            View layout = convertView ?? LoadLayout(context, parent, layoutId);
            if (layout != null)
            {
                value = layout.FindViewById<EditText>(context.Resources.GetIdentifier("dialog_ValueField", "id", context.PackageName));
            }
            else
            {
                value = null;
            }
            return layout;
        }

        public static View LoadBooleanElementLayout(Context context, View convertView, ViewGroup parent, int layoutId, out TextView label, out TextView subLabel, out View value)
        {
            View layout = convertView ?? LoadLayout(context, parent, layoutId);
            if (layout != null)
            {
                label = layout.FindViewById<TextView>(context.Resources.GetIdentifier("dialog_LabelField", "id", context.PackageName));
                value = layout.FindViewById<View>(context.Resources.GetIdentifier("dialog_BoolField", "id", context.PackageName));
                int id = context.Resources.GetIdentifier("dialog_LabelSubtextField", "id", context.PackageName);
                subLabel = (id >= 0) ? layout.FindViewById<TextView>(id): null;
            }
            else
            {
                label = null;
                value = null;
                subLabel = null;
            }
            return layout;
        }

        public static View LoadStringEntryLayout(Context context, View convertView, ViewGroup parent, int layoutId, out TextView label, out EditText value)
        {
            View layout = LoadLayout(context, parent, layoutId);
            if (layout != null)
            {
                label = layout.FindViewById<TextView>(context.Resources.GetIdentifier("dialog_LabelField", "id", context.PackageName));
                value = layout.FindViewById<EditText>(context.Resources.GetIdentifier("dialog_ValueField", "id", context.PackageName));
            }
            else
            {
                label = null;
                value = null;
            }
            return layout;
        }

        private static Dictionary<ElementLayout, string> _resourceMap;

        static DroidResources()
        {
            _resourceMap = new Dictionary<ElementLayout, string>()
            {
                // Label templates
                { ElementLayout.dialog_labelfieldbelow, "dialog_labelfieldbelow"},
                { ElementLayout.dialog_labelfieldright, "dialog_labelfieldright"},

                // Boolean and Checkbox templates
                { ElementLayout.dialog_boolfieldleft, "dialog_boolfieldleft"},
                { ElementLayout.dialog_boolfieldright, "dialog_boolfieldright"},
                { ElementLayout.dialog_boolfieldsubleft, "dialog_boolfieldsubleft"},
                { ElementLayout.dialog_boolfieldsubright, "dialog_boolfieldsubright"},
                { ElementLayout.dialog_onofffieldright, "dialog_onofffieldright"},

                // Root templates
                { ElementLayout.dialog_root, "dialog_root"},

                // Entry templates
                { ElementLayout.dialog_textfieldbelow, "dialog_textfieldbelow"},
                { ElementLayout.dialog_textfieldright, "dialog_textfieldright"},

                // Slider
                { ElementLayout.dialog_floatimage, "dialog_floatimage"},

                // Button templates
                { ElementLayout.dialog_button, "dialog_button"},

                // Date
                { ElementLayout.dialog_datefield, "dialog_datefield"},

                //
                { ElementLayout.dialog_fieldsetlabel, "dialog_fieldsetlabel"},

                { ElementLayout.dialog_panel, "dialog_panel"},

                //
                { ElementLayout.dialog_selectlist, "dialog_selectlist"},
                { ElementLayout.dialog_selectlistfield, "dialog_selectlistfield"},
                { ElementLayout.dialog_textarea, "dialog_textarea"},
            };
        }
    }
}
