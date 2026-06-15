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
using System.IO;
using System.Text;

namespace CppNet {

/** Does not handle digraphs. */
internal class LexerSource : Source {
    static bool isJavaIdentifierStart(int c) {
        return char.IsLetter((char)c) || c == '$' || c == '_';
    }
    static bool isJavaIdentifierPart(int c)
    {
        return char.IsLetter((char)c) || c == '$' || c == '_' || char.IsDigit((char)c);
    }
    static bool isIdentifierIgnorable(int c) {
        return c >= 0 && c <= 8 ||
            c >= 0xE && c <= 0x1B ||
            c >= 0x7F && c <= 0x9F ||
            char.GetUnicodeCategory((char)c) == System.Globalization.UnicodeCategory.Format;
    }

    static int digit(char ch, int radix)
    {
        string alphabet;

        switch(radix) {
            case 8:
                alphabet = "012345678";
                break;

            case 10:
                alphabet = "0123456789";
                break;

            case 16:
                ch = char.ToLower(ch);
                alphabet = "0123456789abcdef";
                break;

            default:
                throw new NotSupportedException();
        }

        return alphabet.IndexOf(ch);
    }
	private static readonly bool	DEBUG = false;

	private JoinReader		reader;
	private bool			ppvalid;
	private bool			bol;
	private bool			include;

	private bool			digraphs;

	/* Unread. */
	private int				u0, u1;
	private int				ucount;

	private int				line;
	private int				column;
	private int				lastcolumn;
	private bool			cr;

	/* ppvalid is:
	 * false in StringLexerSource,
	 * true in FileLexerSource */
	public LexerSource(TextReader r, bool ppvalid) {
		this.reader = new JoinReader(r);
		this.ppvalid = ppvalid;
		this.bol = true;
		this.include = false;

		this.digraphs = true;

		this.ucount = 0;

		this.line = 1;
		this.column = 0;
		this.lastcolumn = -1;
		this.cr = false;
	}

	override internal void init(Preprocessor pp) {
		base.init(pp);
		this.digraphs = pp.getFeature(Feature.DIGRAPHS);
		this.reader.init(pp, this);
	}

	
	override public int getLine() {
		return line;
	}

	
	override public int getColumn() {
		return column;
	}

	
	override internal bool isNumbered() {
		return true;
	}

/* Error handling. */

	private void _error(String msg, bool error) {
		int	_l = line;
		int	_c = column;
		if (_c == 0) {
			_c = lastcolumn;
			_l--;
		}
		else {
			_c--;
		}
		if (error)
			base.error(_l, _c, msg);
		else
			base.warning(_l, _c, msg);
	}

	/* Allow JoinReader to call this. */
    internal void error(String msg)
    {
		_error(msg, true);
	}

	/* Allow JoinReader to call this. */
	internal void warning(String msg) {
		_error(msg, false);
	}

/* A flag for string handling. */

    internal void setInclude(bool b)
    {
		this.include = b;
	}

/*
	private bool _isLineSeparator(int c) {
		return Character.getType(c) == Character.LINE_SEPARATOR
				|| c == -1;
	}
*/

	/* XXX Move to JoinReader and canonicalise newlines. */
	private static bool isLineSeparator(int c) {
		switch ((char)c) {
			case '\r':
			case '\n':
			case '\u2028':
			case '\u2029':
			case '\u000B':
			case '\u000C':
			case '\u0085':
				return true;
			default:
				return (c == -1);
		}
	}


	private int read() {
        System.Diagnostics.Debug.Assert(ucount <= 2, "Illegal ucount: " + ucount);
		switch (ucount) {
			case 2:
				ucount = 1;
				return u1;
			case 1:
				ucount = 0;
				return u0;
		}

		if (reader == null)
			return -1;

		int	c = reader.read();
		switch (c) {
			case '\r':
				cr = true;
				line++;
				lastcolumn = column;
				column = 0;
				break;
			case '\n':
				if (cr) {
					cr = false;
					break;
				}
                goto case '\u2028';
				/* fallthrough */
			case '\u2028':
			case '\u2029':
			case '\u000B':
			case '\u000C':
			case '\u0085':
				cr = false;
				line++;
				lastcolumn = column;
				column = 0;
				break;
			default:
				cr = false;
				column++;
				break;
		}

/*
		if (isLineSeparator(c)) {
			line++;
			lastcolumn = column;
			column = 0;
		}
		else {
			column++;
		}
*/

		return c;
	}

	/* You can unget AT MOST one newline. */
	private void unread(int c)  {
		/* XXX Must unread newlines. */
		if (c != -1) {
			if (isLineSeparator(c)) {
				line--;
				column = lastcolumn;
				cr = false;
			}
			else {
				column--;
			}
			switch (ucount) {
				case 0:
					u0 = c;
					ucount = 1;
					break;
				case 1:
					u1 = c;
					ucount = 2;
					break;
				default:
					throw new InvalidOperationException(
							"Cannot unget another character!"
								);
			}
			// reader.unread(c);
		}
	}

	/* Consumes the rest of the current line into an invalid. */
	private Token invalid(StringBuilder text, String reason) {
		int	d = read();
		while (!isLineSeparator(d)) {
			text.Append((char)d);
			d = read();
		}
		unread(d);
        return new Token(Token.INVALID, text.ToString(), reason);
	}

	private Token ccomment() {
		StringBuilder	text = new StringBuilder("/*");
		int				d;
		do {
			do {
				d = read();
				text.Append((char)d);
			} while (d != '*');
			do {
				d = read();
				text.Append((char)d);
			} while (d == '*');
		} while (d != '/');
		return new Token(Token.CCOMMENT, text.ToString());
	}

	private Token cppcomment() {
		StringBuilder	text = new StringBuilder("//");
		int				d = read();
		while (!isLineSeparator(d)) {
			text.Append((char)d);
			d = read();
		}
		unread(d);
        return new Token(Token.CPPCOMMENT, text.ToString());
	}

	private int escape(StringBuilder text) {
		int		d = read();
		switch (d) {
			case 'a': text.Append('a'); return 0x07;
			case 'b': text.Append('b'); return '\b';
			case 'f': text.Append('f'); return '\f';
			case 'n': text.Append('n'); return '\n';
			case 'r': text.Append('r'); return '\r';
			case 't': text.Append('t'); return '\t';
			case 'v': text.Append('v'); return 0x0b;
			case '\\': text.Append('\\'); return '\\';

			case '0': case '1': case '2': case '3':
			case '4': case '5': case '6': case '7':
				int	len = 0;
				int	val = 0;
				do {
					val = (val << 3) + digit((char)d, 8);
					text.Append((char)d);
					d = read();
                } while(++len < 3 && digit((char)d, 8) != -1);
				unread(d);
				return val;

			case 'x':
				len = 0;
				val = 0;
				do {
                    val = (val << 4) + digit((char)d, 16);
					text.Append((char)d);
					d = read();
                } while(++len < 2 && digit((char)d, 16) != -1);
				unread(d);
				return val;

			/* Exclude two cases from the warning. */
			case '"': text.Append('"'); return '"';
			case '\'': text.Append('\''); return '\'';

			default:
				warning("Unnecessary escape character " + (char)d);
				text.Append((char)d);
				return d;
		}
	}

	private Token character() {
		StringBuilder	text = new StringBuilder("'");
		int				d = read();
		if (d == '\\') {
			text.Append('\\');
			d = escape(text);
		}
		else if (isLineSeparator(d)) {
			unread(d);
			return new Token(Token.INVALID, text.ToString(),
							"Unterminated character literal");
		}
		else if (d == '\'') {
			text.Append('\'');
            return new Token(Token.INVALID, text.ToString(),
							"Empty character literal");
		}
		else if (char.IsControl((char)d)) {
			text.Append('?');
			return invalid(text, "Illegal unicode character literal");
		}
		else {
			text.Append((char)d);
		}

		int		e = read();
		if (e != '\'') {
			// error("Illegal character constant");
			/* We consume up to the next ' or the rest of the line. */
			for (;;) {
				if (isLineSeparator(e)) {
					unread(e);
					break;
				}
				text.Append((char)e);
				if (e == '\'')
					break;
				e = read();
			}
            return new Token(Token.INVALID, text.ToString(),
							"Illegal character constant " + text);
		}
		text.Append('\'');
		/* XXX It this a bad cast? */
        return new Token(Token.CHARACTER,
				text.ToString(), (char)d);
	}

	private Token String(char open, char close) {
		StringBuilder	text = new StringBuilder();
		text.Append(open);

		StringBuilder	buf = new StringBuilder();

		for (;;) {
			int	c = read();
			if (c == close) {
				break;
			}
			else if (c == '\\') {
				text.Append('\\');
				if (!include) {
					char	d = (char)escape(text);
					buf.Append(d);
				}
			}
			else if (c == -1) {
				unread(c);
				// error("End of file in string literal after " + buf);
				return new Token(Token.INVALID, text.ToString(),
						"End of file in string literal after " + buf);
			}
			else if (isLineSeparator(c)) {
				unread(c);
				// error("Unterminated string literal after " + buf);
                return new Token(Token.INVALID, text.ToString(),
						"Unterminated string literal after " + buf);
			}
			else {
				text.Append((char)c);
				buf.Append((char)c);
			}
		}
		text.Append(close);
        return new Token(close == '>' ? Token.HEADER : Token.STRING,
						text.ToString(), buf.ToString());
	}

	private Token _number(StringBuilder text, long val, int d) {
		int	bits = 0;
		for (;;) {
			/* XXX Error check duplicate bits. */
			if (d == 'U' || d == 'u') {
				bits |= 1;
				text.Append((char)d);
				d = read();
			}
			else if (d == 'L' || d == 'l') {
				if ((bits & 4) != 0)
					/* XXX warn */ ;
				bits |= 2;
				text.Append((char)d);
				d = read();
			}
			else if (d == 'I' || d == 'i') {
				if ((bits & 2) != 0)
					/* XXX warn */ ;
				bits |= 4;
				text.Append((char)d);
				d = read();
			}
			else if (char.IsLetter((char)d)) {
				unread(d);
                return new Token(Token.INVALID, text.ToString(),
						"Invalid suffix \"" + (char)d +
						"\" on numeric constant");
			}
			else {
				unread(d);
                return new Token(Token.INTEGER,
					text.ToString(), (long)val);
			}
		}
	}

	/* We already chewed a zero, so empty is fine. */
	private Token number_octal()  {
		StringBuilder	text = new StringBuilder("0");
		int				d = read();
		long			val = 0;
		while (digit((char)d, 8) != -1) {
			val = (val << 3) + digit((char)d, 8);
			text.Append((char)d);
			d = read();
		}
		return _number(text, val, d);
	}

	/* We do not know whether know the first digit is valid. */
	private Token number_hex(char x)  {
		StringBuilder	text = new StringBuilder("0");
		text.Append(x);
		int				d = read();
		if (digit((char)d, 16) == -1) {
			unread(d);
			// error("Illegal hexadecimal constant " + (char)d);
            return new Token(Token.INVALID, text.ToString(),
					"Illegal hexadecimal digit " + (char)d +
					" after "+ text);
		}
		long	val = 0;
		do {
			val = (val << 4) + digit((char)d, 16);
			text.Append((char)d);
			d = read();
		} while (digit((char)d, 16) != -1);
		return _number(text, val, d);
	}

	/* We know we have at least one valid digit, but empty is not
	 * fine. */
	/* XXX This needs a complete rewrite. */
	private Token number_decimal(int c) {
		StringBuilder	text = new StringBuilder((char)c);
		int				d = c;
		long			val = 0;
		do {
			val = val * 10 + digit((char)d, 10);
			text.Append((char)d);
			d = read();
		} while (digit((char)d, 10) != -1);
		return _number(text, val, d);
	}

	private Token identifier(int c)  {
		StringBuilder	text = new StringBuilder();
		int				d;
		text.Append((char)c);
		for (;;) {
			d = read();
			if (isIdentifierIgnorable(d))
				;
			else if (isJavaIdentifierPart(d))
				text.Append((char)d);
			else
				break;
		}
		unread(d);
        return new Token(Token.IDENTIFIER, text.ToString());
	}

	private Token whitespace(int c) {
		StringBuilder	text = new StringBuilder();
		int				d;
		text.Append((char)c);
		for (;;) {
			d = read();
			if (ppvalid && isLineSeparator(d))	/* XXX Ugly. */
				break;
			if (char.IsWhiteSpace((char)d))
				text.Append((char)d);
			else
				break;
		}
		unread(d);
        return new Token(Token.WHITESPACE, text.ToString());
	}

	/* No token processed by cond() contains a newline. */
	private Token cond(char c, int yes, int no) {
		int	d = read();
		if (c == d)
			return new Token(yes);
		unread(d);
		return new Token(no);
	}

	public override Token token() {
		Token	tok = null;

		int		_l = line;
		int		_c = column;

		int		c = read();
		int		d;

		switch (c) {
			case '\n':
				if (ppvalid) {
					bol = true;
					if (include) {
                        tok = new Token(Token.NL, _l, _c, "\n");
					}
					else {
						int	nls = 0;
						do {
							nls++;
							d = read();
						} while (d == '\n');
						unread(d);
						char[]	text = new char[nls];
						for (int i = 0; i < text.Length; i++)
							text[i] = '\n';
						// Skip the bol = false below.
						tok = new Token(Token.NL, _l, _c, new String(text));
					}
					if (DEBUG)
						System.Console.Error.WriteLine("lx: Returning NL: " + tok);
					return tok;
				}
				/* Let it be handled as whitespace. */
				break;

			case '!':
				tok = cond('=', Token.NE, '!');
				break;

			case '#':
				if (bol)
					tok = new Token(Token.HASH);
				else
					tok = cond('#', Token.PASTE, '#');
				break;

			case '+':
				d = read();
				if (d == '+')
					tok = new Token(Token.INC);
				else if (d == '=')
					tok = new Token(Token.PLUS_EQ);
				else
					unread(d);
				break;
			case '-':
				d = read();
				if (d == '-')
					tok = new Token(Token.DEC);
				else if (d == '=')
					tok = new Token(Token.SUB_EQ);
				else if (d == '>')
					tok = new Token(Token.ARROW);
				else
					unread(d);
				break;

			case '*':
				tok = cond('=', Token.MULT_EQ, '*');
				break;
			case '/':
				d = read();
				if (d == '*')
					tok = ccomment();
				else if (d == '/')
					tok = cppcomment();
				else if (d == '=')
					tok = new Token(Token.DIV_EQ);
				else
					unread(d);
				break;

			case '%':
				d = read();
				if (d == '=')
					tok = new Token(Token.MOD_EQ);
				else if (digraphs && d == '>')
					tok = new Token('}');	// digraph
				else if (digraphs && d == ':') {
                    bool paste = true;
					d = read();
					if (d != '%') {
						unread(d);
						tok = new Token('#');	// digraph
                        paste = false;
					}
					d = read();
					if (d != ':') {
						unread(d);	// Unread 2 chars here.
						unread('%');
						tok = new Token('#');	// digraph
                        paste = false;
					}
                    if(paste) {
					    tok = new Token(Token.PASTE);	// digraph
                    }
				}
				else
					unread(d);
				break;

			case ':':
				/* :: */
				d = read();
				if (digraphs && d == '>')
					tok = new Token(']');	// digraph
				else
					unread(d);
				break;

			case '<':
				if (include) {
					tok = String('<', '>');
				}
				else {
					d = read();
					if (d == '=')
                        tok = new Token(Token.LE);
					else if (d == '<')
                        tok = cond('=', Token.LSH_EQ, Token.LSH);
					else if (digraphs && d == ':')
						tok = new Token('[');	// digraph
					else if (digraphs && d == '%')
						tok = new Token('{');	// digraph
					else
						unread(d);
				}
				break;

			case '=':
                tok = cond('=', Token.EQ, '=');
				break;

			case '>':
				d = read();
				if (d == '=')
                    tok = new Token(Token.GE);
				else if (d == '>')
                    tok = cond('=', Token.RSH_EQ, Token.RSH);
				else
					unread(d);
				break;

			case '^':
                tok = cond('=', Token.XOR_EQ, '^');
				break;

			case '|':
				d = read();
				if (d == '=')
                    tok = new Token(Token.OR_EQ);
				else if (d == '|')
                    tok = cond('=', Token.LOR_EQ, Token.LOR);
				else
					unread(d);
				break;
			case '&':
				d = read();
				if (d == '&')
                    tok = cond('=', Token.LAND_EQ, Token.LAND);
				else if (d == '=')
                    tok = new Token(Token.AND_EQ);
				else
					unread(d);
				break;

			case '.':
				d = read();
				if (d == '.')
                    tok = cond('.', Token.ELLIPSIS, Token.RANGE);
				else
					unread(d);
				/* XXX decimal fraction */
				break;

			case '0':
				/* octal or hex */
				d = read();
				if (d == 'x' || d == 'X')
					tok = number_hex((char)d);
				else {
					unread(d);
					tok = number_octal();
				}
				break;

			case '\'':
				tok = character();
				break;

			case '"':
				tok = String('"', '"');
				break;

			case -1:
				close();
				tok = new Token(Token.EOF, _l, _c, "<eof>");
				break;
		}

		if (tok == null) {
			if (char.IsWhiteSpace((char)c)) {
				tok = whitespace(c);
			}
			else if (char.IsDigit((char)c)) {
				tok = number_decimal(c);
			}
			else if (isJavaIdentifierStart(c)) {
				tok = identifier(c);
			}
			else {
				tok = new Token(c);
			}
		}

		if (bol) {
			switch (tok.getType()) {
				case Token.WHITESPACE:
                case Token.CCOMMENT:
					break;
				default:
					bol = false;
					break;
			}
		}

		tok.setLocation(_l, _c);
		if (DEBUG)
			System.Console.WriteLine("lx: Returning " + tok);
		// (new Exception("here")).printStackTrace(System.out);
		return tok;
	}

    public override void close()
    {
        if(reader != null) {
            reader.close();
            reader = null;
        }
        base.close();
    }
}

}