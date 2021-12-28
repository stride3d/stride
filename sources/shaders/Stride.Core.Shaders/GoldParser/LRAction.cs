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
	/// LR parser action type.
	/// </summary>
    internal enum LRAction
	{
		/// <summary>
		/// No action. Not used.
		/// </summary>
		None = 0,

		/// <summary>
		/// Shift a symbol and go to a state
		/// </summary>
		Shift = 1,   
    
		/// <summary>
		/// Reduce by a specified rule
		/// </summary>
		Reduce = 2,      

		/// <summary>
		/// Goto to a state on reduction
		/// </summary>
		Goto = 3,        

		/// <summary>
		/// Input successfully parsed
		/// </summary>
		Accept = 4,      

		/// <summary>
		/// Error
		/// </summary>
		Error = 5       
	}
}
