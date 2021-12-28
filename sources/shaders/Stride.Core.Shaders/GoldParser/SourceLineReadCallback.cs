// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
// 
// ----------------------------------------------------------------------
//  Gold Parser engine.
//  See more details on http://www.devincook.com/goldparser/
//  
//  Original code is written in VB by Devin Cook (GOLDParser@DevinCook.com)
// 
//  This translation is done by Vladimir Morozov (vmoroz@hotmail.com)
//  
//  The translation is based on the other engine translations:
//  Delphi engine by Alexandre Rai (riccio@gmx.at)
//  C# engine by Marcus Klimstra (klimstra@home.nl)
// ----------------------------------------------------------------------
#region Using directives

using System;

#endregion

namespace GoldParser
{
	/// <summary>
	/// This callback is used by parser to notify read source line.
	/// Use parser.LineText to get line source.
	/// </summary>
    internal delegate void SourceLineReadCallback(Parser parser, int lineStart, int lineLength);
}
