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
using System.Collections;

#endregion

namespace GoldParser
{
	/// <summary>
	/// State in the Deterministic Finite Automata 
	/// which is used by the tokenizer.
	/// </summary>
	internal class DfaState
	{
		private int m_index;
		internal Symbol m_acceptSymbol;
		internal ObjectMap m_transitionVector; 

		/// <summary>
		/// Creates a new instance of the <c>DfaState</c> class.
		/// </summary>
		/// <param name="index">Index in the DFA state table.</param>
		/// <param name="acceptSymbol">Symbol to accept.</param>
		/// <param name="transitionVector">Transition vector.</param>
		public DfaState(int index, Symbol acceptSymbol, ObjectMap transitionVector)
		{
			m_index = index;
			m_acceptSymbol = acceptSymbol;
			m_transitionVector = transitionVector;
		}

		/// <summary>
		/// Gets index of the state in DFA state table.
		/// </summary>
		public int Index 
		{
			get { return m_index; }
		}

		/// <summary>
		/// Gets the symbol which can be accepted in this DFA state.
		/// </summary>
		public Symbol AcceptSymbol 
		{
			get { return m_acceptSymbol; }
		}
	}
}
