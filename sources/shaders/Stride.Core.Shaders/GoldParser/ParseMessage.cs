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
	/// Available parse messages.
	/// </summary>
    internal enum ParseMessage
	{
		/// <summary>
		/// Nothing
		/// </summary>
		Empty = 0,  
	
		/// <summary>
		/// Each time a token is read, this message is generated.
		/// </summary>
		TokenRead = 1,

		/// <summary>
		/// When the engine is able to reduce a rule,
		/// this message is returned. The rule that was
		/// reduced is set in the GOLDParser's ReduceRule property.
		/// The tokens that are reduced and correspond the
		/// rule's definition are stored in the Tokens() property.
		/// </summary>
		Reduction = 2,

		/// <summary>
		/// The engine will returns this message when the source
		/// text has been accepted as both complete and correct.
		/// In other words, the source text was successfully analyzed.
		/// </summary>
		Accept = 3,

		/// <summary>
		/// Before any parsing can take place,
		/// a Compiled Grammar Table file must be loaded.
		/// </summary>
		NotLoadedError = 4,

		/// <summary>
		/// The tokenizer will generate this message when
		/// it is unable to recognize a series of characters
		/// as a valid token. To recover, pop the invalid
		/// token from the input queue.
		/// </summary>
		LexicalError = 5,

		/// <summary>
		/// Often the parser will read a token that is not expected
		/// in the grammar. When this happens, the Tokens() property
		/// is filled with tokens the parsing engine expected to read.
		/// To recover: push one of the expected tokens on the input queue.
		/// </summary>
		SyntaxError = 6, 
	
		/// <summary>
		/// The parser reached the end of the file while reading a comment.
		/// This is caused when the source text contains a "run-away"
		/// comment, or in other words, a block comment that lacks the
		/// delimiter.
		/// </summary>
		CommentError = 7,
 
		/// <summary>
		/// Something is wrong, very wrong.
		/// </summary>
		InternalError = 8,

		/// <summary>
		/// A block comment is complete.
		/// When this message is returned, the content of the CurrentComment
		/// property is set to the comment text. The text includes starting and ending
		/// block comment characters.
		/// </summary>
		CommentBlockRead = 9,

		/// <summary>
		/// Line comment is read.
		/// When this message is returned, the content of the CurrentComment
		/// property is set to the comment text. The text includes starting 
		/// line comment characters.
		/// </summary>
		CommentLineRead = 10,
	}
}
