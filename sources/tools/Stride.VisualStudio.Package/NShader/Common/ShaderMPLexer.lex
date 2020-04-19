/*
//  ShaderMPLexer.lex.
//  Lexical description for MPLex. This file is inspired from http://svn.assembla.com/svn/ppjlab/trunk/scanner.lex
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
*/

%namespace NShader.Lexer
%option noparser, verbose, summary, unicode

/**********************************************************************************/
/********************************User Defined Code*********************************/
/**********************************************************************************/

%{
	public IShaderTokenProvider ShaderTokenProvider = null; // Token provider
%}

/**********************************************************************************/
/**********Start Condition Declarations and Lexical Category Definitions***********/
/**********************************************************************************/

%x COMMENT
digit				[0-9]
alpha				[a-zA-Z_]
exponent			[Ee]("+"|"-")?{digit}+
floatsuffix			[fFhH]
white_space			[ \t\v\n\f\r]
hexdigit			[0-9a-fA-F]
CmntStart    \/\*
CmntEnd      \*\/
ABStar       [^\*\n]*

/**********************************************************************************/
/**********************************************************************************/
/********************************The Rules Section*********************************/
/**********************************************************************************/
/**********************************************************************************/

%%

/**********************************************************************************/
/************************************Comments**************************************/
/**********************************************************************************/

"//"(.)*				    {return (int)ShaderToken.COMMENT_LINE;}

{CmntStart}{ABStar}\**{CmntEnd} { return (int)ShaderToken.COMMENT;}
{CmntStart}{ABStar}\**          { BEGIN(COMMENT); return (int)ShaderToken.COMMENT;}
<COMMENT>\n                     |                                
<COMMENT>{ABStar}\**            { return (int)ShaderToken.COMMENT;}
<COMMENT>{ABStar}\**{CmntEnd}   { BEGIN(INITIAL); return (int)ShaderToken.COMMENT;}

/**********************************************************************************/
/***********************************Identifier*************************************/
/**********************************************************************************/


":"{white_space}*{alpha}({alpha}|{digit})* 		{return (int)ShaderTokenProvider.GetTokenFromSemantics(yytext);}

{alpha}({alpha}|{digit})* 		{return (int)ShaderTokenProvider.GetTokenFromIdentifier(yytext);}

"0x"{hexdigit}+				{return (int)ShaderToken.NUMBER;}


/**********************************************************************************/
/*************************************Integer**************************************/
/**********************************************************************************/

{digit}+				{return (int)ShaderToken.NUMBER;}

/**********************************************************************************/
/**************************************Float***************************************/
/**********************************************************************************/

{digit}+{exponent}			{return (int)ShaderToken.FLOAT;}
{digit}*"."{digit}+({exponent})?({floatsuffix})?	{return (int)ShaderToken.FLOAT;}
{digit}+"."{digit}*({exponent})?({floatsuffix})?	{return (int)ShaderToken.FLOAT;}


/**********************************************************************************/
/*************************************String***************************************/
/**********************************************************************************/

\"(\\.|[^\\"])*\"			{return (int)ShaderToken.STRING_LITERAL;}

^{white_space}*#{alpha}+    {return (int)ShaderToken.PREPROCESSOR;}

/**********************************************************************************/
/***************************Operators And Special Signs****************************/
/**********************************************************************************/

"+="					{return (int)ShaderToken.OPERATOR;}
"-="					{return (int)ShaderToken.OPERATOR;}
"*="					{return (int)ShaderToken.OPERATOR;}
"/="					{return (int)ShaderToken.OPERATOR;}
"%="					{return (int)ShaderToken.OPERATOR;}
"&&"					{return (int)ShaderToken.OPERATOR;}
"||"					{return (int)ShaderToken.OPERATOR;}
"<="					{return (int)ShaderToken.OPERATOR;}
">="					{return (int)ShaderToken.OPERATOR;}
"=="					{return (int)ShaderToken.OPERATOR;}
"!="					{return (int)ShaderToken.OPERATOR;}
";"					    {return (int)ShaderToken.DELIMITER;}
"{"					    {return (int)ShaderToken.LEFT_BRACKET;}
"}"					    {return (int)ShaderToken.RIGHT_BRACKET;}
","					    {return (int)ShaderToken.DELIMITER;}
"="					    {return (int)ShaderToken.OPERATOR;}
"("					    {return (int)ShaderToken.LEFT_PARENTHESIS;}
")"					    {return (int)ShaderToken.RIGHT_PARENTHESIS;}
"["				    	{return (int)ShaderToken.LEFT_SQUARE_BRACKET;}
"]"					    {return (int)ShaderToken.RIGHT_SQUARE_BRACKET;}
"."	    				{return (int)ShaderToken.OPERATOR;}
"&"		    			{return (int)ShaderToken.OPERATOR;}
"!"			    		{return (int)ShaderToken.OPERATOR;}
"-"				    	{return (int)ShaderToken.OPERATOR;}
"+"					    {return (int)ShaderToken.OPERATOR;}
"*" 					{return (int)ShaderToken.OPERATOR;}
"/"	    				{return (int)ShaderToken.OPERATOR;}
"%"		    			{return (int)ShaderToken.OPERATOR;}
"<"			    		{return (int)ShaderToken.OPERATOR;}
">"				    	{return (int)ShaderToken.OPERATOR;}

/**********************************************************************************/
/****************************White Space & Unrecognized****************************/
/**********************************************************************************/
{white_space}					{/* Ignore */}
.					{return (int)ShaderToken.UNDEFINED;}


/**********************************************************************************/
/**********************************************************************************/
/**************************The User Defined Code Section***************************/
/**********************************************************************************/
/**********************************************************************************/

%%
