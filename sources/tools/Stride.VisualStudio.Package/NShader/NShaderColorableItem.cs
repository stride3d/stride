#region Header Licence
//  ---------------------------------------------------------------------
// 
//  Copyright (c) 2009 Alexandre Mutel and Microsoft Corporation.  
//  All rights reserved.
// 
//  This code module is part of NShader, a plugin for visual studio
//  to provide syntax highlighting for shader languages (hlsl, glsl, cg)
// 
//  ------------------------------------------------------------------
// 
//  This code is licensed under the Microsoft Public License. 
//  See the file License.txt for the license details.
//  More info on: http://nshader.codeplex.com
// 
//  ------------------------------------------------------------------
#endregion
using System.Drawing;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using Stride.VisualStudio.Classifiers;

namespace NShader
{
    public class NShaderColorableItem : ColorableItem
    {
        public Color HiForeColorLight { get; private set; }
        public Color HiForeColorDark { get; private set; }
        public COLORINDEX ForeColorLight { get; private set; }
        public COLORINDEX ForeColorDark { get; private set; }

        public NShaderColorableItem(VisualStudioTheme theme, string name, COLORINDEX foreColorLight, COLORINDEX foreColorDark, COLORINDEX backColor)
            : base(name, name, theme == VisualStudioTheme.Dark ? foreColorDark : foreColorLight, backColor, Color.Empty, Color.Empty, FONTFLAGS.FF_DEFAULT)
        {
            ForeColorLight = foreColorLight;
            ForeColorDark = foreColorDark;
        }

        public NShaderColorableItem(VisualStudioTheme theme, string name, COLORINDEX foreColorLight, COLORINDEX foreColorDark, COLORINDEX backColor, FONTFLAGS fontFlags)
            : base(name, name, theme == VisualStudioTheme.Dark ? foreColorDark : foreColorLight, backColor, Color.Empty, Color.Empty, fontFlags)
        {
            ForeColorLight = foreColorLight;
            ForeColorDark = foreColorDark;
        }

        public NShaderColorableItem(VisualStudioTheme theme, string name, string displayName, COLORINDEX foreColorLight, COLORINDEX foreColorDark, COLORINDEX backColor)
            : base(name, displayName, theme == VisualStudioTheme.Dark ? foreColorDark : foreColorLight, backColor, Color.Empty, Color.Empty, FONTFLAGS.FF_DEFAULT)
        {
            ForeColorLight = foreColorLight;
            ForeColorDark = foreColorDark;
        }

        public NShaderColorableItem(VisualStudioTheme theme, string name, string displayName, COLORINDEX foreColorLight, COLORINDEX foreColorDark, COLORINDEX backColor, FONTFLAGS fontFlags)
            : base(name, displayName, theme == VisualStudioTheme.Dark ? foreColorDark : foreColorLight, backColor, Color.Empty, Color.Empty, fontFlags)
        {
            ForeColorLight = foreColorLight;
            ForeColorDark = foreColorDark;
        }

        public NShaderColorableItem(VisualStudioTheme theme, string name, string displayName, COLORINDEX foreColorLight, COLORINDEX foreColorDark, COLORINDEX backColor, Color hiForeColorLight, Color hiForeColorDark, Color hiBackColor, FONTFLAGS fontFlags)
            : base(name, displayName, theme == VisualStudioTheme.Dark ? foreColorDark : foreColorLight, backColor, theme == VisualStudioTheme.Dark ? hiForeColorDark : hiForeColorLight,  hiBackColor, fontFlags)
        {
            ForeColorLight = foreColorLight;
            ForeColorDark = foreColorDark;

            HiForeColorLight = hiForeColorLight;
            HiForeColorDark = hiForeColorDark;
        }

        public override int GetMergingPriority(out int priority)
        {
           priority = 0x2000;
           return VSConstants.S_OK;
        }

        public override int GetColorData(int cdElement, out uint crColor)
        {
            return base.GetColorData(cdElement, out crColor);
        }

        public override int GetDefaultColors(COLORINDEX[] foreColor, COLORINDEX[] backColor)
        {
            return base.GetDefaultColors(foreColor, backColor);
        }
    }
}