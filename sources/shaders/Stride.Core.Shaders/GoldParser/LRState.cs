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
	/// State of LR parser.
	/// </summary>
    internal class LRState
	{
		private int m_index;
		private LRStateAction[] m_actions;
		internal LRStateAction[] m_transitionVector;

		/// <summary>
		/// Creates a new instance of the <c>LRState</c> class
		/// </summary>
		/// <param name="index">Index of the LR state in the LR state table.</param>
		/// <param name="actions">List of all available LR actions in this state.</param>
		/// <param name="transitionVector">Transition vector which has symbol index as an index.</param>
		public LRState(int index, LRStateAction[] actions, LRStateAction[] transitionVector)
		{
			m_index = index;
			m_actions = actions;
			m_transitionVector = transitionVector;
		}

		/// <summary>
		/// Gets index of the LR state in LR state table.
		/// </summary>
		public int Index 
		{
			get { return m_index; }
		}

		/// <summary>
		/// Gets LR state action count.
		/// </summary>
		public int ActionCount 
		{
			get { return m_actions.Length; }
		}

		/// <summary>
		/// Returns state action by its index.
		/// </summary>
		/// <param name="index">State action index.</param>
		/// <returns>LR state action for the given index.</returns>
		public LRStateAction GetAction(int index)
		{
			return m_actions[index];
		}

		/// <summary>
		/// Returns LR state action by symbol index.
		/// </summary>
		/// <param name="symbolIndex">Symbol Index to search for.</param>
		/// <returns>LR state action object.</returns>
		public LRStateAction GetActionBySymbolIndex(int symbolIndex)
		{
			return m_transitionVector[symbolIndex];
		}
	}
}
