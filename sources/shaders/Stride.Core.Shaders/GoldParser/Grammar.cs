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
using System.IO;
using System.Text;
using System.Collections;

#endregion

namespace GoldParser
{
	/// <summary>
	/// Contains grammar tables required for parsing.
	/// </summary>
    internal class Grammar
	{
		#region Fields and constants

		/// <summary>
		/// Identifies Gold parser grammar file.
		/// </summary>
		public const string FileHeader = "GOLD Parser Tables/v1.0";

		// Grammar header information
		private string m_name;                // Name of the grammar
		private string m_version;             // Version of the grammar
		private string m_author;              // Author of the grammar
		private string m_about;               // Grammar description
		private int    m_startSymbolIndex;    // Start symbol index
		private bool   m_caseSensitive;       // Grammar is case sensitive or not

		// Tables read from the binary grammar file
		private  Symbol[]    m_symbolTable;    // Symbol table
		private  String[]    m_charSetTable;   // Charset table
		internal Rule[]      m_ruleTable;      // Rule table
		internal DfaState[]  m_dfaStateTable;  // DFA state table
		internal LRState[]   m_lrStateTable;   // LR state table

		// Initial states
		internal int m_dfaInitialStateIndex;   // DFA initial state index
		internal DfaState m_dfaInitialState;   // DFA initial state 
		internal int m_lrInitialState;         // LR initial state

		// Internal state of grammar parser
		private BinaryReader m_reader;         // Source of the grammar    
		private int m_entryCount;              // Number of entries left

		internal Symbol m_errorSymbol;
		internal Symbol m_endSymbol;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new instance of <c>Grammar</c> class
		/// </summary>
		/// <param name="reader"></param>
		public Grammar(BinaryReader reader)
		{
			if (reader == null)
			{
				throw new ArgumentNullException("reader");
			}

			m_reader = reader;
			Load();
		}

		#endregion

		#region Public members

        /// <summary>
        /// Gets the symbol table.
        /// </summary>
	    public Symbol[] SymbolTable
	    {
	        get
	        {
	            return m_symbolTable;
	        }
	    }

	    /// <summary>
		/// Gets grammar name.
		/// </summary>
		public string Name
		{
			get { return m_name; }
		}

		/// <summary>
		/// Gets grammar version.
		/// </summary>
		public string Version
		{
			get { return m_version; }
		}

		/// <summary>
		/// Gets grammar author.
		/// </summary>
		public string Author
		{
			get { return m_author; }
		}

		/// <summary>
		/// Gets grammar description.
		/// </summary>
		public string About
		{
			get { return m_about; }
		}

		/// <summary>
		/// Gets the start symbol for the grammar.
		/// </summary>
		public Symbol StartSymbol 
		{
			get { return m_symbolTable[m_startSymbolIndex]; }
		}

		/// <summary>
		/// Gets the value indicating if the grammar is case sensitive.
		/// </summary>
		public bool CaseSensitive
		{
			get { return m_caseSensitive; }
		}

		/// <summary>
		/// Gets initial DFA state.
		/// </summary>
		public DfaState DfaInitialState
		{
			get { return m_dfaInitialState; }
		}

		/// <summary>
		/// Gets initial LR state.
		/// </summary>
		public LRState InitialLRState
		{
			get { return m_lrStateTable[m_lrInitialState]; }
		}

		/// <summary>
		/// Gets a special symbol to designate last token in the input stream.
		/// </summary>
		public Symbol EndSymbol 
		{
			get { return m_endSymbol; }
		}

		#endregion

		#region Private members

		/// <summary>
		/// Loads grammar from the binary reader.
		/// </summary>
		private void Load()
		{
			if (FileHeader != ReadString())
			{
				throw new GoldParserException(SR.GetString(SR.Grammar_WrongFileHeader));
			}
			while (m_reader.PeekChar() != -1)
			{
				RecordType recordType = ReadNextRecord();
				switch (recordType)
				{
					case RecordType.Parameters: 
						ReadHeader();
						break;

					case RecordType.TableCounts: 
						ReadTableCounts();
						break;

					case RecordType.Initial: 
						ReadInitialStates();
						break;

					case RecordType.Symbols: 
						ReadSymbols();
						break;

					case RecordType.CharSets: 
						ReadCharSets();
						break;

					case RecordType.Rules: 
						ReadRules();
						break;

					case RecordType.DfaStates: 
						ReadDfaStates();
						break;

					case RecordType.LRStates: 
						ReadLRStates();
						break;
    
					default:
						throw new GoldParserException(SR.GetString(SR.Grammar_InvalidRecordType));
				}
			}
			m_dfaInitialState = m_dfaStateTable[m_dfaInitialStateIndex];
			OptimizeDfaTransitionVectors();
		}

		/// <summary>
		/// Reads the next record in the binary grammar file.
		/// </summary>
		/// <returns>Read record type.</returns>
		private RecordType ReadNextRecord()
		{
			char recordType = (char) ReadByte();
			//Structure below is ready for future expansion
			switch (recordType)
			{
				case 'M': 
					//Read the number of entry's
					m_entryCount = ReadInt16();
					return (RecordType) ReadByteEntry();

				default:
					throw new GoldParserException(SR.GetString(SR.Grammar_InvalidRecordHeader));
			}
		}

		/// <summary>
		/// Reads grammar header information.
		/// </summary>
		private void ReadHeader()
		{
			m_name             = ReadStringEntry();
			m_version          = ReadStringEntry();
			m_author           = ReadStringEntry();
			m_about            = ReadStringEntry(); 
			m_caseSensitive    = ReadBoolEntry(); 
			m_startSymbolIndex = ReadInt16Entry(); 
		}

		/// <summary>
		/// Reads table record counts and initializes tables.
		/// </summary>
		private void ReadTableCounts()
		{
			// Initialize tables
			m_symbolTable    = new Symbol   [ReadInt16Entry()];
			m_charSetTable   = new String   [ReadInt16Entry()];
			m_ruleTable      = new Rule     [ReadInt16Entry()];
			m_dfaStateTable  = new DfaState [ReadInt16Entry()];
			m_lrStateTable   = new LRState  [ReadInt16Entry()];
		}

		/// <summary>
		/// Read initial DFA and LR states.
		/// </summary>
		private void ReadInitialStates()
		{
			m_dfaInitialStateIndex = ReadInt16Entry();
			m_lrInitialState       = ReadInt16Entry();
		}

		/// <summary>
		/// Read symbol information.
		/// </summary>
		private void ReadSymbols()
		{
			int index             = ReadInt16Entry();
			string name           = ReadStringEntry();
			SymbolType symbolType = (SymbolType) ReadInt16Entry();
			
			Symbol symbol = new Symbol(index, name, symbolType);
			switch (symbolType)
			{
				case SymbolType.Error:
					m_errorSymbol = symbol;
					break;

				case SymbolType.End:
					m_endSymbol = symbol;
					break;
			}
			m_symbolTable[index] = symbol;
		}

		/// <summary>
		/// Read char set information.
		/// </summary>
		private void ReadCharSets()
		{
			m_charSetTable[ReadInt16Entry()] = ReadStringEntry();
		}

		/// <summary>
		/// Read rule information.
		/// </summary>
		private void ReadRules()
		{
			int index = ReadInt16Entry();
			Symbol nonTerminal = m_symbolTable[ReadInt16Entry()];
			ReadEmptyEntry();
			Symbol[] symbols = new Symbol[m_entryCount];
			for (int i = 0 ; i < symbols.Length; i++)
			{
				symbols[i] = m_symbolTable[ReadInt16Entry()];
			}
			Rule rule = new Rule(index, nonTerminal, symbols);
			m_ruleTable[index] = rule;
		}

		/// <summary>
		/// Read DFA state information.
		/// </summary>
		private void ReadDfaStates()
		{
			int index = ReadInt16Entry();
			Symbol acceptSymbol = null;
			bool acceptState = ReadBoolEntry();
			if (acceptState)
			{
				acceptSymbol = m_symbolTable[ReadInt16Entry()];
			}
			else
			{
				ReadInt16Entry();  // Skip the entry.
			}
			ReadEmptyEntry();

			// Read DFA edges
			DfaEdge[] edges = new DfaEdge[m_entryCount / 3];
			for (int i = 0; i < edges.Length; i++)
			{
				edges[i].CharSetIndex = ReadInt16Entry();
				edges[i].TargetIndex  = ReadInt16Entry();
				ReadEmptyEntry();
			}
	
			// Create DFA state and store it in DFA state table
			ObjectMap transitionVector = CreateDfaTransitionVector(edges); 
			DfaState dfaState = new DfaState(index, acceptSymbol, transitionVector);
			m_dfaStateTable[index] = dfaState;
		}

		/// <summary>
		/// Read LR state information.
		/// </summary>
		private void ReadLRStates()
		{
			int index = ReadInt16Entry();
			ReadEmptyEntry();
			LRStateAction[] stateTable = new LRStateAction[m_entryCount / 4]; 
			for (int i = 0; i < stateTable.Length; i++)
			{
				Symbol symbol     = m_symbolTable[ReadInt16Entry()];
				LRAction action = (LRAction) ReadInt16Entry();
				int targetIndex   = ReadInt16Entry();
				ReadEmptyEntry();
				stateTable[i] = new LRStateAction(i, symbol, action, targetIndex);
			}

			// Create the transition vector
			LRStateAction[] transitionVector = new LRStateAction[m_symbolTable.Length]; 
			for (int i = 0; i < transitionVector.Length; i++)
			{
				transitionVector[i] = null;
			}
			for (int i = 0; i < stateTable.Length; i++)
			{
				transitionVector[stateTable[i].Symbol.Index] = stateTable[i];
			}

			LRState lrState = new LRState(index, stateTable, transitionVector);
			m_lrStateTable[index] = lrState;
		}
	
		/// <summary>
		/// Creates the DFA state transition vector.
		/// </summary>
		/// <param name="edges">Array of automata edges.</param>
		/// <returns>Hashtable with the transition information.</returns>
		private ObjectMap CreateDfaTransitionVector(DfaEdge[] edges)
		{
			ObjectMap transitionVector = new ObjectMap(); 
			for (int i = edges.Length; --i >= 0;) 
			{
				string charSet = m_charSetTable[edges[i].CharSetIndex];
				for (int j = 0; j < charSet.Length; j++)
				{
					transitionVector[charSet[j]] = edges[i].TargetIndex;
				}
			}
			return transitionVector;
		}

		/// <summary>
		/// Reads empty entry from the grammar file.
		/// </summary>
		private void ReadEmptyEntry()
		{
			if (ReadEntryType() != EntryType.Empty)
			{
				throw new GoldParserException(SR.GetString(SR.Grammar_EmptyEntryExpected));
			}
			m_entryCount--;
		}

		/// <summary>
		/// Reads string entry from the grammar file.
		/// </summary>
		/// <returns>String entry content.</returns>
		private string ReadStringEntry()
		{
			if (ReadEntryType() != EntryType.String)
			{
				throw new GoldParserException(SR.GetString(SR.Grammar_StringEntryExpected));
			}
			m_entryCount--;
			return ReadString();
		}

		/// <summary>
		/// Reads Int16 entry from the grammar file.
		/// </summary>
		/// <returns>Int16 entry content.</returns>
		private int ReadInt16Entry()
		{
			if (ReadEntryType() != EntryType.Integer)
			{
				throw new GoldParserException(SR.GetString(SR.Grammar_IntegerEntryExpected));
			}
			m_entryCount--;
			return ReadInt16();
		}

		/// <summary>
		/// Reads byte entry from the grammar file.
		/// </summary>
		/// <returns>Byte entry content.</returns>
		private byte ReadByteEntry()
		{
			if (ReadEntryType() != EntryType.Byte)
			{
				throw new GoldParserException(SR.GetString(SR.Grammar_ByteEntryExpected));
			}
			m_entryCount--;
			return ReadByte();
		}

		/// <summary>
		/// Reads boolean entry from the grammar file.
		/// </summary>
		/// <returns>Boolean entry content.</returns>
		private bool ReadBoolEntry()
		{
			if (ReadEntryType() != EntryType.Boolean)
			{
				throw new GoldParserException(SR.GetString(SR.Grammar_BooleanEntryExpected));
			}
			m_entryCount--;
			return ReadBool();
		}

		/// <summary>
		/// Reads entry type.
		/// </summary>
		/// <returns>Entry type.</returns>
		private EntryType ReadEntryType()
		{
			if (m_entryCount == 0)
			{
				throw new GoldParserException(SR.GetString(SR.Grammar_NoEntry));
			}  
			return (EntryType) ReadByte();
		}

		/// <summary>
		/// Reads string from the grammar file.
		/// </summary>
		/// <returns>String value.</returns>
		private string ReadString()
		{  
			StringBuilder result = new StringBuilder(); 
			char unicodeChar = (char) ReadInt16();
			while (unicodeChar != (char) 0)
			{
				result.Append(unicodeChar);
				unicodeChar = (char) ReadInt16();
			}
			return result.ToString();
		}

		/// <summary>
		/// Reads two byte integer Int16 from the grammar file.
		/// </summary>
		/// <returns>Int16 value.</returns>
		private int ReadInt16()
		{
			return m_reader.ReadUInt16();
		}

		/// <summary>
		/// Reads byte from the grammar file.
		/// </summary>
		/// <returns>Byte value.</returns>
		private byte ReadByte()
		{
			return m_reader.ReadByte();
		}

		/// <summary>
		/// Reads boolean from the grammar file.
		/// </summary>
		/// <returns>Boolean value.</returns>
		private bool ReadBool()
		{
			return (ReadByte() == 1);
		}

		private void OptimizeDfaTransitionVectors()
		{
			DfaState[] dfaStates = m_dfaStateTable;
			foreach (DfaState state in dfaStates)
			{
				ObjectMap transitions = state.m_transitionVector;
				for (int i = transitions.Count; --i >= 0;)
				{
					int key = transitions.GetKey(i);
					object transition = transitions[key];
					if (transition != null)
					{
						int transitionIndex = (int) transition;
						if (transitionIndex >= 0)
						{
							transitions[key] = dfaStates[transitionIndex];
						}
						else
						{
							transitions[key] = null;
						}
					}
				}
				transitions.ReadOnly = true;
			}
		}

		#endregion

		#region Private type definitions

		/// <summary>
		/// Record type byte in the binary grammar file.
		/// </summary>
		private enum RecordType
		{
			Parameters  = (int) 'P', // 80
			TableCounts = (int) 'T', // 84
			Initial     = (int) 'I', // 73
			Symbols     = (int) 'S', // 83
			CharSets    = (int) 'C', // 67
			Rules       = (int) 'R', // 82
			DfaStates   = (int) 'D', // 68
			LRStates    = (int) 'L', // 76
			Comment     = (int) '!'  // 33
		}

		/// <summary>
		/// Entry type byte in the binary grammar file.
		/// </summary>
		private enum EntryType
		{
			Empty		= (int) 'E', // 69
			Integer		= (int) 'I', // 73
			String		= (int) 'S', // 83
			Boolean		= (int) 'B', // 66
			Byte		= (int) 'b'  // 98
		}

		/// <summary>
		/// Edge between DFA states.
		/// </summary>
		private struct DfaEdge 
		{
			public int CharSetIndex;
			public int TargetIndex;
		}

		#endregion
	}
}
