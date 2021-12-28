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
using System.Text;

#endregion

namespace GoldParser
{
	/// <summary>
	/// Rule is the logical structures of the grammar.
	/// </summary>
	/// <remarks>
	/// Rules consist of a head containing a nonterminal 
	/// followed by a series of both nonterminals and terminals.
	/// </remarks>	
    internal class Rule
	{
		private int m_index;
		internal Symbol m_nonTerminal;
		internal Symbol[] m_symbols;
		internal bool m_hasOneNonTerminal;

		/// <summary>
		/// Creates a new instance of <c>Rule</c> class.
		/// </summary>
		/// <param name="index">Index of the rule in the grammar rule table.</param>
		/// <param name="nonTerminal">Nonterminal of the rule.</param>
		/// <param name="symbols">Terminal and nonterminal symbols of the rule.</param>
		public Rule(int index, Symbol nonTerminal, Symbol[] symbols)
		{
			m_index = index;
			m_nonTerminal = nonTerminal;
			m_symbols = symbols;
			m_hasOneNonTerminal = (symbols.Length == 1) 
				&& (symbols[0].SymbolType == SymbolType.NonTerminal); 
		}

		/// <summary>
		/// Gets index of the rule in the rule table.
		/// </summary>
		public int Index 
		{
			get { return m_index; }
		}

		/// <summary>
		/// Gets the head symbol of the rule.
		/// </summary>
		public Symbol NonTerminal 
		{
			get { return m_nonTerminal; }
		}

		/// <summary>
		/// Gets name of the rule.
		/// </summary>
		public string Name
		{
			get { return '<' + m_nonTerminal.Name + '>'; }
		}

		/// <summary>
		/// Gets number of symbols.
		/// </summary>
		public int Count 
		{
			get { return m_symbols.Length; }
		}

		/// <summary>
		/// Gets symbol by its index.
		/// </summary>
		public Symbol this[int index] 
		{
			get { return m_symbols[index]; }
		}

		/// <summary>
		/// Gets true if the rule contains exactly one symbol.
		/// </summary>
		/// <remarks>Used by the Parser object to TrimReductions</remarks>
		public bool ContainsOneNonTerminal
		{
			get { return m_hasOneNonTerminal; }
		}

		/// <summary>
		/// Gets the rule definition.
		/// </summary>
		public string Definition
		{
			get 
			{
				StringBuilder result = new StringBuilder();
				for (int i = 0; i < m_symbols.Length; i++)
				{
					result.Append(m_symbols[i].ToString());
					if (i < m_symbols.Length - 1)
						result.Append(' ');
				}
				return result.ToString();
			}
		}

		/// <summary>
		/// Returns the Backus-Noir representation of the rule.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Name + " ::= " + Definition;
		}
	}
}
