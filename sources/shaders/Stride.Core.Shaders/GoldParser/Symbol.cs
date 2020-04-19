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
	/// Represents a terminal or nonterminal symbol used by the Deterministic
	/// Finite Automata (DFA) and LR Parser. 
	/// </summary>
	/// <remarks>
	/// Symbols can be either terminals (which represent a class of 
	/// tokens - such as identifiers) or nonterminals (which represent 
	/// the rules and structures of the grammar).  Terminal symbols fall 
	/// into several categories for use by the GOLD Parser Engine 
	/// which are enumerated in <c>SymbolType</c> enumeration.
	/// </remarks>
    internal class Symbol
	{
		internal int       m_index;      // symbol index in symbol table
		private string     m_name;       // name of the symbol
		internal SymbolType m_symbolType; // type of the symbol
		private string     m_text;       // printable representation of symbol

		private const string m_quotedChars = "|-+*?()[]{}<>!";

		/// <summary>
		/// Creates a new instance of <c>Symbol</c> class.
		/// </summary>
		/// <param name="index">Symbol index in symbol table.</param>
		/// <param name="name">Name of the symbol.</param>
		/// <param name="symbolType">Type of the symbol.</param>
		public Symbol(int index, string name, SymbolType symbolType)
		{
			m_index = index;
			m_name = name;
			m_symbolType = symbolType;
		}

		/// <summary>
		/// Returns the index of the symbol in the GOLDParser object's Symbol Table.
		/// </summary>
		public int Index 
		{
			get { return m_index; }
		}

		/// <summary>
		/// Returns the name of the symbol.
		/// </summary>
		public string Name
		{
			get { return m_name; }
		}

		/// <summary>
		/// Returns an enumerated data type that denotes
		/// the class of symbols that the object belongs to.
		/// </summary>
		public SymbolType SymbolType 
		{
			get { return m_symbolType; }
		}

		/// <summary>
		/// Returns the text representation of the symbol.
		/// In the case of nonterminals, the name is delimited by angle brackets,
		/// special terminals are delimited by parenthesis
		/// and terminals are delimited by single quotes 
		/// (if special characters are present).
		/// </summary>
		/// <returns>String representation of symbol.</returns>
		public override string ToString()
		{
			if (m_text == null)
			{
				switch (SymbolType)
				{
					case SymbolType.NonTerminal:  
						m_text = '<' + Name + '>';
						break;

					case SymbolType.Terminal: 
						m_text = FormatTerminalSymbol(Name);
						break;
				
					default:
						m_text = '(' + Name + ')';
						break;
				}
			}
			return m_text;
		}

		private static string FormatTerminalSymbol(string source)
		{
			StringBuilder result = new StringBuilder();
			for (int i = 0; i < source.Length; i++)
			{
				char ch = source[i]; 
				if (ch == '\'')
				{
					result.Append("''");
				}
				else if (IsQuotedChar(ch) || (ch == '"'))
				{
					result.Append(new Char[] {'\'', ch, '\''});
				}
				else
				{
					result.Append(ch);
				}
			}
			return result.ToString();
		}

		private static bool IsQuotedChar(char value) 
		{
			return (m_quotedChars.IndexOf(value) >= 0);
		}
	}
}
