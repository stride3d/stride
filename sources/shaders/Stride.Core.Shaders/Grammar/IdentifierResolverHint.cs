// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Text.RegularExpressions;

using Irony.Parsing;

namespace Stride.Core.Shaders.Grammar
{

    // Semi-automatic conflict resolution hint
    public class ResolveInCode : CustomGrammarHint
    {
        private Func<ConflictResolutionArgs, bool> resolver;


        public ResolveInCode(ParserActionType parserAction, Func<ConflictResolutionArgs, bool> resolver)
            : base(parserAction)
        {
            this.resolver = resolver;
        }

        public override bool Match(ConflictResolutionArgs args)
        {
            return resolver(args);
        }
    }



    // Semi-automatic conflict resolution hint
    public class IdentifierResolverHint : CustomGrammarHint
    {
        private bool isExpectingType;

        private CustomGrammarHint nextGrammarHint;

        public IdentifierResolverHint(bool isExpectingType, CustomGrammarHint nextGrammarHint = null)
            : base(ParserActionType.Reduce)
        {
            this.isExpectingType = isExpectingType;
            this.nextGrammarHint = nextGrammarHint;
        }

        private string[] identifierPostFix = new string[] { "_OUTPUT", "OUT", "_INPUT", "IN", "OUTPUT_SCENE", "OUTPUT_SCENEENV", "PSSceneIn" };

        private Regex regex = new Regex("(.*\r?\n)");

        public override void Init(GrammarData grammarData)
        {
            base.Init(grammarData);
            if (nextGrammarHint != null)
                nextGrammarHint.Init(grammarData);
        }

        public override bool Match(ConflictResolutionArgs args)
        {
            string identifier = args.Context.PreviousToken.Text;

            //var type = DeclarationManager.Instance.Find(args.Context, identifier);

            bool result;

            //if (type == DeclarationType.NotFound)
            //{
            result = false;
            if (isExpectingType)
            {
                // We are probably in a type cast
                if (args.Context.CurrentToken.Text == ")")
                {
                    // Verify that next token is an identifier, a number or left parenthesis => Then the current expression is certainly a cast
                    args.Scanner.BeginPreview();
                    Token nextToken;
                    do
                    {
                        nextToken = args.Scanner.GetToken();
                    }
                    while (nextToken.Terminal.FlagIsSet(TermFlags.IsNonGrammar) && nextToken.Terminal != Grammar.Eof);

                    var nextTokenName = nextToken.Terminal.Name;

                    if (nextTokenName == "identifier" || nextTokenName == "integer_literal" || nextTokenName == "float_literal" || nextTokenName == "(")
                        result = true;
                    args.Scanner.EndPreview(true);
                }
            }

            if (!result && nextGrammarHint != null)
            {
                result = nextGrammarHint.Match(args);
            }

            // In case that we found something, use the reduce production where this hint is used
            if (result)
            {
                args.Result = ParserActionType.Reduce;
                args.ReduceProduction = null;
            }

            //} else if (isExpectingType)
            //    result = type == DeclarationType.TypeName;
            //else
            //    result = type == DeclarationType.Variable;
            return result;

        }
    }

    // Semi-automatic conflict resolution hint
    public class GenericResolverHint : CustomGrammarHint
    {
        private TerminalSet skipTokens;
        public  GenericResolverHint(TerminalSet skipTokens) : base(ParserActionType.Reduce)
        {
            this.skipTokens = skipTokens;
        }

        public override bool Match(ConflictResolutionArgs args)
        {
            if (args.Context.CurrentParserInput.Term.Name == "<")
            {
                args.Scanner.BeginPreview();
                int ltCount = 0;
                string previewSym;
                bool isKeyword = false;
                while (true)
                {
                    // Find first token ahead (using preview mode) that is either end of generic parameter (">") or something else
                    Token preview;
                    do
                    {
                        preview = args.Scanner.GetToken();
                    }
                    while ((preview.Terminal.FlagIsSet(TermFlags.IsNonGrammar) || skipTokens.Contains(preview.Terminal)) && preview.Terminal != Grammar.Eof);

                    isKeyword = preview.EditorInfo.Color == TokenColor.Keyword;

                    // See what did we find
                    previewSym = preview.Terminal.Name;
                    if (previewSym == "<")
                    {
                        ltCount++;
                    }
                    else if (previewSym == ">" && ltCount > 0)
                    {
                        ltCount--;
                    }
                    else
                        break;
                }
                args.Scanner.EndPreview(true);

                // if we see ">", then it is type argument, not operator
                // if previewSym == ">" then shift else reduce
                if (previewSym == ">" || isKeyword)
                {
                    args.Result = ParserActionType.Shift;
                    return true;
                }
                else
                {
                    args.Result = ParserActionType.Reduce;
                    return true;                    
                }
            }
            return false;
        }

    }
}
