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
	/// Action in a LR State. 
	/// </summary>
    internal class LRStateAction
	{
		private int m_index;
		private Symbol m_symbol;
		private LRAction m_action;
		internal int m_value;

		/// <summary>
		/// Creats a new instance of the <c>LRStateAction</c> class.
		/// </summary>
		/// <param name="index">Index of the LR state action.</param>
		/// <param name="symbol">Symbol associated with the action.</param>
		/// <param name="action">Action type.</param>
		/// <param name="value">Action value.</param>
		public LRStateAction(int index, Symbol symbol, LRAction action, int value)
		{
			m_index = index;
			m_symbol = symbol;
			m_action = action;
			m_value = value;
		}

		/// <summary>
		/// Gets index of the LR state action.
		/// </summary>
		public int Index 
		{
			get { return m_index; }
		}

		/// <summary>
		/// Gets symbol associated with the LR state action.
		/// </summary>
		public Symbol Symbol 
		{
			get { return m_symbol; }
		}

		/// <summary>
		/// Gets action type.
		/// </summary>
		public LRAction Action 
		{
			get { return m_action; }
		}

		/// <summary>
		/// Gets the action value.
		/// </summary>
		public int Value 
		{
			get { return m_value; }
		}
	}
}
