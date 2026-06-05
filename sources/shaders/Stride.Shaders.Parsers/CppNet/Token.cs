/*
 * Anarres C Preprocessor
 * Copyright (c) 2007-2008, Shevek
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied.  See the License for the specific language governing
 * permissions and limitations under the License.
 */

#pragma warning disable

using System;
using System.Text;

namespace CppNet {

/**
 * A Preprocessor token.
 *
 * @see Preprocessor
 */
    internal sealed class Token
    {
        // public const int	EOF        = -1;

        private int type;
        private int line;
        private int column;
        private Object value;
        private String text;

        public Token(int type, int line, int column,
                        String text, Object value)
        {
            this.type = type;
            this.line = line;
            this.column = column;
            this.text = text;
            this.value = value;
        }

        public Token(int type, int line, int column, String text) :
            this(type, line, column, text, null)
        {
        }

        /* pp */
        internal Token(int type, String text, Object value) :
            this(type, -1, -1, text, value)
        {
        }

        /* pp */
        internal Token(int type, String text) :
            this(type, text, null)
        {
        }

        /* pp */
        internal Token(int type) :
            this(type, type < _TOKENS ? texts[type] : "TOK" + type)
        {
        }

        /**
         * Returns the semantic type of this token.
         */
        public int getType()
        {
            return type;
        }

        internal void setLocation(int line, int column)
        {
            this.line = line;
            this.column = column;
        }

        /**
         * Returns the line at which this token started.
         *
         * Lines are numbered from zero.
         */
        public int getLine()
        {
            return line;
        }

        /**
         * Returns the column at which this token started.
         *
         * Columns are numbered from zero.
         */
        public int getColumn()
        {
            return column;
        }

        /**
         * Returns the original or generated text of this token.
         *
         * This is distinct from the semantic value of the token.
         *
         * @see #getValue()
         */
        public String getText()
        {
            return text;
        }

        /**
         * Returns the semantic value of this token.
         *
         * For strings, this is the parsed String.
         * For integers, this is an Integer object.
         * For other token types, as appropriate.
         *
         * @see #getText()
         */
        public Object getValue()
        {
            return value;
        }

        /**
         * Returns a description of this token, for debugging purposes.
         */
        public String ToString()
        {
            StringBuilder buf = new StringBuilder();

            buf.Append('[').Append(getTokenName(type));
            if(line != -1) {
                buf.Append('@').Append(line);
                if(column != -1)
                    buf.Append(',').Append(column);
            }
            buf.Append("]:");
            if(text != null)
                buf.Append('"').Append(text).Append('"');
            else if(type > 3 && type < 256)
                buf.Append((char)type);
            else
                buf.Append('<').Append(type).Append('>');
            if(value != null)
                buf.Append('=').Append(value);
            return buf.ToString();
        }

        /**
         * Returns the descriptive name of the given token type.
         *
         * This is mostly used for stringification and debugging.
         */
        public static String getTokenName(int type)
        {
            if(type < 0)
                return "Invalid" + type;
            if(type >= names.Length)
                return "Invalid" + type;
            if(names[type] == null)
                return "Unknown" + type;
            return names[type];
        }

        /** The token type AND_EQ. */
        public const int AND_EQ = 257;
        /** The token type ARROW. */
        public const int ARROW = 258;
        /** The token type CHARACTER. */
        public const int CHARACTER = 259;
        /** The token type CCOMMENT. */
        public const int CCOMMENT = 260;
        /** The token type CPPCOMMENT. */
        public const int CPPCOMMENT = 261;
        /** The token type DEC. */
        public const int DEC = 262;
        /** The token type DIV_EQ. */
        public const int DIV_EQ = 263;
        /** The token type ELLIPSIS. */
        public const int ELLIPSIS = 264;
        /** The token type EOF. */
        public const int EOF = 265;
        /** The token type EQ. */
        public const int EQ = 266;
        /** The token type GE. */
        public const int GE = 267;
        /** The token type HASH. */
        public const int HASH = 268;
        /** The token type HEADER. */
        public const int HEADER = 269;
        /** The token type IDENTIFIER. */
        public const int IDENTIFIER = 270;
        /** The token type INC. */
        public const int INC = 271;
        /** The token type INTEGER. */
        public const int INTEGER = 272;
        /** The token type LAND. */
        public const int LAND = 273;
        /** The token type LAND_EQ. */
        public const int LAND_EQ = 274;
        /** The token type LE. */
        public const int LE = 275;
        /** The token type LITERAL. */
        public const int LITERAL = 276;
        /** The token type LOR. */
        public const int LOR = 277;
        /** The token type LOR_EQ. */
        public const int LOR_EQ = 278;
        /** The token type LSH. */
        public const int LSH = 279;
        /** The token type LSH_EQ. */
        public const int LSH_EQ = 280;
        /** The token type MOD_EQ. */
        public const int MOD_EQ = 281;
        /** The token type MULT_EQ. */
        public const int MULT_EQ = 282;
        /** The token type NE. */
        public const int NE = 283;
        /** The token type NL. */
        public const int NL = 284;
        /** The token type OR_EQ. */
        public const int OR_EQ = 285;
        /** The token type PASTE. */
        public const int PASTE = 286;
        /** The token type PLUS_EQ. */
        public const int PLUS_EQ = 287;
        /** The token type RANGE. */
        public const int RANGE = 288;
        /** The token type RSH. */
        public const int RSH = 289;
        /** The token type RSH_EQ. */
        public const int RSH_EQ = 290;
        /** The token type STRING. */
        public const int STRING = 291;
        /** The token type SUB_EQ. */
        public const int SUB_EQ = 292;
        /** The token type WHITESPACE. */
        public const int WHITESPACE = 293;
        /** The token type XOR_EQ. */
        public const int XOR_EQ = 294;
        /** The token type M_ARG. */
        public const int M_ARG = 295;
        /** The token type M_PASTE. */
        public const int M_PASTE = 296;
        /** The token type M_STRING. */
        public const int M_STRING = 297;
        /** The token type P_LINE. */
        public const int P_LINE = 298;
        /** The token type INVALID. */
        public const int INVALID = 299;
        /**
         * The number of possible semantic token types.
         *
         * Please note that not all token types below 255 are used.
         */
        public const int _TOKENS = 300;

        /** The position-less space token. */
        /* pp */
        public static readonly Token space = new Token(WHITESPACE, -1, -1, " ");

        private static readonly String[] names = new String[_TOKENS];
        private static readonly String[] texts = new String[_TOKENS];
        static Token()
        {
            for(int i = 0; i < 255; i++) {
                texts[i] = ((char)i).ToString();
                names[i] = texts[i];
            }

            texts[AND_EQ] = "&=";
            texts[ARROW] = "->";
            texts[DEC] = "--";
            texts[DIV_EQ] = "/=";
            texts[ELLIPSIS] = "...";
            texts[EQ] = "==";
            texts[GE] = ">=";
            texts[HASH] = "#";
            texts[INC] = "++";
            texts[LAND] = "&&";
            texts[LAND_EQ] = "&&=";
            texts[LE] = "<=";
            texts[LOR] = "||";
            texts[LOR_EQ] = "||=";
            texts[LSH] = "<<";
            texts[LSH_EQ] = "<<=";
            texts[MOD_EQ] = "%=";
            texts[MULT_EQ] = "*=";
            texts[NE] = "!=";
            texts[NL] = "\n";
            texts[OR_EQ] = "|=";
            /* We have to split the two hashes or Velocity eats them. */
            texts[PASTE] = "#" + "#";
            texts[PLUS_EQ] = "+=";
            texts[RANGE] = "..";
            texts[RSH] = ">>";
            texts[RSH_EQ] = ">>=";
            texts[SUB_EQ] = "-=";
            texts[XOR_EQ] = "^=";

            names[AND_EQ] = "AND_EQ";
            names[ARROW] = "ARROW";
            names[CHARACTER] = "CHARACTER";
            names[CCOMMENT] = "CCOMMENT";
            names[CPPCOMMENT] = "CPPCOMMENT";
            names[DEC] = "DEC";
            names[DIV_EQ] = "DIV_EQ";
            names[ELLIPSIS] = "ELLIPSIS";
            names[EOF] = "EOF";
            names[EQ] = "EQ";
            names[GE] = "GE";
            names[HASH] = "HASH";
            names[HEADER] = "HEADER";
            names[IDENTIFIER] = "IDENTIFIER";
            names[INC] = "INC";
            names[INTEGER] = "INTEGER";
            names[LAND] = "LAND";
            names[LAND_EQ] = "LAND_EQ";
            names[LE] = "LE";
            names[LITERAL] = "LITERAL";
            names[LOR] = "LOR";
            names[LOR_EQ] = "LOR_EQ";
            names[LSH] = "LSH";
            names[LSH_EQ] = "LSH_EQ";
            names[MOD_EQ] = "MOD_EQ";
            names[MULT_EQ] = "MULT_EQ";
            names[NE] = "NE";
            names[NL] = "NL";
            names[OR_EQ] = "OR_EQ";
            names[PASTE] = "PASTE";
            names[PLUS_EQ] = "PLUS_EQ";
            names[RANGE] = "RANGE";
            names[RSH] = "RSH";
            names[RSH_EQ] = "RSH_EQ";
            names[STRING] = "STRING";
            names[SUB_EQ] = "SUB_EQ";
            names[WHITESPACE] = "WHITESPACE";
            names[XOR_EQ] = "XOR_EQ";
            names[M_ARG] = "M_ARG";
            names[M_PASTE] = "M_PASTE";
            names[M_STRING] = "M_STRING";
            names[P_LINE] = "P_LINE";
            names[INVALID] = "INVALID";
        }

    }
}