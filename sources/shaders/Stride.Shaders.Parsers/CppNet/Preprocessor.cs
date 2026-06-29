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
using System.Collections.Generic;
using System.IO;

namespace CppNet {


/**
 * A C Preprocessor.
 * The Preprocessor outputs a token stream which does not need
 * re-lexing for C or C++. Alternatively, the output text may be
 * reconstructed by concatenating the {@link Token#getText() text}
 * values of the returned {@link Token Tokens}. (See
 * {@link CppReader}, which does this.)
 */


/*
Source file name and line number information is conveyed by lines of the form

     # linenum filename flags

These are called linemarkers. They are inserted as needed into
the output (but never within a string or character constant). They
mean that the following line originated in file filename at line
linenum. filename will never contain any non-printing characters;
they are replaced with octal escape sequences.

After the file name comes zero or more flags, which are `1', `2',
`3', or `4'. If there are multiple flags, spaces separate them. Here
is what the flags mean:

`1'
    This indicates the start of a new file.
`2'
    This indicates returning to a file (after having included another
    file).
`3'
    This indicates that the following text comes from a system header
    file, so certain warnings should be suppressed.
`4'
    This indicates that the following text should be treated as being
    wrapped in an implicit extern "C" block.
*/

internal class Preprocessor : IDisposable {
    private class InternalSource : Source {
        public override Token token()
        {
			throw new LexerException("Cannot read from " + getName());
		}

        internal override String getPath()
        {
			return "<internal-data>";
		}
		
		internal override String getName() {
			return "internal data";
		}
    }

	private static readonly Source		INTERNAL = new InternalSource();
    private static readonly Macro		__LINE__ = new Macro(INTERNAL, "__LINE__");
	private static readonly Macro		__FILE__ = new Macro(INTERNAL, "__FILE__");
	private static readonly Macro		__COUNTER__ = new Macro(INTERNAL, "__COUNTER__");

	private List<Source>			inputs;

	/* The fundamental engine. */
	private Dictionary<String,Macro>		macros;
	private Stack<State>			states;
	private Source					source;

	/* Miscellaneous support. */
	private int						counter;

	/* Support junk to make it work like cpp */
	private List<String>			quoteincludepath;	/* -iquote */
	private List<String>			sysincludepath;		/* -I */
	private List<String>			frameworkspath;
	private Feature			features;
	private Warning			warnings;
	private VirtualFileSystem		filesystem;
	private PreprocessorListener	listener;

    private List<string> _importedPaths = new List<string>();

	public Preprocessor() {
		this.inputs = new List<Source>();

		this.macros = new Dictionary<String,Macro>();
		macros.Add(__LINE__.getName(), __LINE__);
		macros.Add(__FILE__.getName(), __FILE__);
		macros.Add(__COUNTER__.getName(), __COUNTER__);
		this.states = new Stack<State>();
		states.Push(new State());
		this.source = null;

		this.counter = 0;

		this.quoteincludepath = new List<String>();
		this.sysincludepath = new List<String>();
		this.frameworkspath = new List<String>();
		this.features = Feature.NONE;
		this.warnings = Warning.NONE;
		this.filesystem = new JavaFileSystem();
		this.listener = null;

	    EmitExtraLineInfo = true;
	}

	public Preprocessor(Source initial) :
		this() {
		addInput(initial);
	}

	/** Equivalent to
	 * 'new Preprocessor(new {@link FileLexerSource}(file))'
	 */
	public Preprocessor(FileInfo file) :
		this(new FileLexerSource(file)) {
	}

    public bool EmitExtraLineInfo { get; set; }

	/**
	 * Sets the VirtualFileSystem used by this Preprocessor.
	 */
	public void setFileSystem(VirtualFileSystem filesystem) {
		this.filesystem = filesystem;
	}

	/**
	 * Returns the VirtualFileSystem used by this Preprocessor.
	 */
	public VirtualFileSystem getFileSystem() {
		return filesystem;
	}

	/**
	 * Sets the PreprocessorListener which handles events for
	 * this Preprocessor.
	 *
	 * The listener is notified of warnings, errors and source
	 * changes, amongst other things.
	 */
	public void setListener(PreprocessorListener listener) {
		this.listener = listener;
		Source	s = source;
		while (s != null) {
			// s.setListener(listener);
			s.init(this);
			s = s.getParent();
		}
	}

	/**
	 * Returns the PreprocessorListener which handles events for
	 * this Preprocessor.
	 */
	public PreprocessorListener getListener() {
		return listener;
	}

	/**
	 * Returns the feature-set for this Preprocessor.
	 *
	 * This set may be freely modified by user code.
	 */
	public Feature getFeatures() {
		return features;
	}

	/**
	 * Adds a feature to the feature-set of this Preprocessor.
	 */
	public void addFeature(Feature f) {
        features |= f;
	}

	/**
	 * Adds features to the feature-set of this Preprocessor.
	 */
	public void addFeatures(Feature f) {
        features |= f;
	}

	/**
	 * Returns true if the given feature is in
	 * the feature-set of this Preprocessor.
	 */
	public bool getFeature(Feature f) {
        return (features & f) != Feature.NONE;
	}

	/**
	 * Returns the warning-set for this Preprocessor.
	 *
	 * This set may be freely modified by user code.
	 */
	public Warning getWarnings() {
		return warnings;
	}

	/**
	 * Adds a warning to the warning-set of this Preprocessor.
	 */
	public void addWarning(Warning w) {
        warnings |= w;
	}

	/**
	 * Adds warnings to the warning-set of this Preprocessor.
	 */
	public void addWarnings(Warning w) {
        warnings |= w;
	}

	/**
	 * Returns true if the given warning is in
	 * the warning-set of this Preprocessor.
	 */
	public bool getWarning(Warning w) {
        return (warnings & w) != Warning.NONE;
	}

	/**
	 * Adds input for the Preprocessor.
	 *
	 * Inputs are processed in the order in which they are added.
	 */
	public void addInput(Source source) {
		source.init(this);
		inputs.Add(source);
	}

	/**
	 * Adds input for the Preprocessor.
	 *
	 * @see #addInput(Source)
	 */
	public void addInput(FileInfo file) {
		addInput(new FileLexerSource(file));
	}


	/**
	 * Handles an error.
	 *
	 * If a PreprocessorListener is installed, it receives the
	 * error. Otherwise, an exception is thrown.
	 */
	protected void error(int line, int column, String msg) {
		if (listener != null)
			listener.handleError(source, line, column, msg);
		else
			throw new LexerException("Error at " + line + ":" + column + ": " + msg);
	}

	/**
	 * Handles an error.
	 *
	 * If a PreprocessorListener is installed, it receives the
	 * error. Otherwise, an exception is thrown.
	 *
	 * @see #error(int, int, String)
	 */
	protected void error(Token tok, String msg) {
		error(tok.getLine(), tok.getColumn(), msg);
	}

	/**
	 * Handles a warning.
	 *
	 * If a PreprocessorListener is installed, it receives the
	 * warning. Otherwise, an exception is thrown.
	 */
	protected void warning(int line, int column, String msg) {
		if (warnings.HasFlag(Warning.ERROR))
			error(line, column, msg);
		else if (listener != null)
			listener.handleWarning(source, line, column, msg);
		else
			throw new LexerException("Warning at " + line + ":" + column + ": " + msg);
	}

	/**
	 * Handles a warning.
	 *
	 * If a PreprocessorListener is installed, it receives the
	 * warning. Otherwise, an exception is thrown.
	 *
	 * @see #warning(int, int, String)
	 */
	protected void warning(Token tok, String msg) {
		warning(tok.getLine(), tok.getColumn(), msg);
	}

	/**
	 * Adds a Macro to this Preprocessor.
	 *
	 * The given {@link Macro} object encapsulates both the name
	 * and the expansion.
	 */
	public void addMacro(Macro m) {
		// System.out.println("Macro " + m);
		String	name = m.getName();
		/* Already handled as a source error in macro(). */
		if ("defined" == name)
			throw new LexerException("Cannot redefine name 'defined'");
		macros[m.getName()] = m;
	}

	/**
	 * Defines the given name as a macro.
	 *
	 * The String value is lexed into a token stream, which is
	 * used as the macro expansion.
	 */
	public void addMacro(String name, String value) {
		try {
			Macro				m = new Macro(name);
			StringLexerSource	s = new StringLexerSource(value);
			for (;;) {
				Token	tok = s.token();
                if(tok.getType() == Token.EOF)
					break;
				m.addToken(tok);
			}
			addMacro(m);
		}
		catch (IOException e) {
			throw new LexerException(e);
		}
	}

	/**
	 * Defines the given name as a macro, with the value <code>1</code>.
	 *
	 * This is a convnience method, and is equivalent to
	 * <code>addMacro(name, "1")</code>.
	 */
	public void addMacro(String name) {
		addMacro(name, "1");
	}

	/**
	 * Sets the user include path used by this Preprocessor.
	 */
	/* Note for future: Create an IncludeHandler? */
	public void setQuoteIncludePath(List<String> path) {
		this.quoteincludepath = path;
	}

	/**
	 * Returns the user include-path of this Preprocessor.
	 *
	 * This list may be freely modified by user code.
	 */
	public List<String> getQuoteIncludePath() {
		return quoteincludepath;
	}

	/**
	 * Sets the system include path used by this Preprocessor.
	 */
	/* Note for future: Create an IncludeHandler? */
	public void setSystemIncludePath(List<String> path) {
		this.sysincludepath = path;
	}

	/**
	 * Returns the system include-path of this Preprocessor.
	 *
	 * This list may be freely modified by user code.
	 */
	public List<String> getSystemIncludePath() {
		return sysincludepath;
	}

	/**
	 * Sets the Objective-C frameworks path used by this Preprocessor.
	 */
	/* Note for future: Create an IncludeHandler? */
	public void setFrameworksPath(List<String> path) {
		this.frameworkspath = path;
	}

	/**
	 * Returns the Objective-C frameworks path used by this
	 * Preprocessor.
	 *
	 * This list may be freely modified by user code.
	 */
	public List<String> getFrameworksPath() {
		return frameworkspath;
	}

	/**
	 * Returns the Map of Macros parsed during the run of this
	 * Preprocessor.
	 */
	public Dictionary<String,Macro> getMacros() {
		return macros;
	}

	/**
	 * Returns the named macro.
	 *
	 * While you can modify the returned object, unexpected things
	 * might happen if you do.
	 */
	public Macro getMacro(String name) {
        Macro retval;
        macros.TryGetValue(name, out retval);
        return retval;
	}

/* States */

	private void push_state() {
		State	top = states.Peek();
		states.Push(new State(top));
	}

	private void pop_state() {
		State	s = states.Pop();
		if (states.Count == 0) {
			error(0, 0, "#" + "endif without #" + "if");
			states.Push(s);
		}
	}

	private bool isActive() {
		State	state = states.Peek();
		return state.isParentActive() && state.isActive();
	}


/* Sources */

	/**
	 * Returns the top Source on the input stack.
	 *
	 * @see Source
	 * @see #push_source(Source,bool)
	 * @see #pop_source()
	 */
	public Source getSource() {
		return source;
	}

	/**
	 * Pushes a Source onto the input stack.
	 *
	 * @see #getSource()
	 * @see #pop_source()
	 */
	protected void push_source(Source source, bool autopop) {
		source.init(this);
		source.setParent(this.source, autopop);
		// source.setListener(listener);
		if (listener != null)
			listener.handleSourceChange(this.source, "suspend");
		this.source = source;
		if (listener != null)
			listener.handleSourceChange(this.source, "push");
	}

	/**
	 * Pops a Source from the input stack.
	 *
	 * @see #getSource()
	 * @see #push_source(Source,bool)
	 */
	protected void pop_source() {
		if (listener != null)
			listener.handleSourceChange(this.source, "pop");
		Source	s = this.source;
		this.source = s.getParent();
		/* Always a noop unless called externally. */
		s.close();
		if (listener != null && this.source != null)
			listener.handleSourceChange(this.source, "resume");
	}


/* Source tokens */

	private Token	_source_token;

	/* XXX Make this include the Token.NL, and make all cpp directives eat
	 * their own Token.NL. */
	private Token line_token(int line, String name, String extra) {
		StringBuilder	buf = new StringBuilder();
		buf.Append("#line ").Append(line)
			.Append(" \"");
		/* XXX This call to escape(name) is correct but ugly. */
		MacroTokenSource.escape(buf, name);
	    buf.Append("\"");
        if (EmitExtraLineInfo)
	        buf.Append(extra);
        buf.Append("\n");
        return new Token(Token.P_LINE, line, 0, buf.ToString(), null);
	}

	private Token source_token() {
        if(_source_token != null) {
            Token tok = _source_token;
            _source_token = null;
			if (getFeature(Feature.DEBUG))
				System.Console.Error.WriteLine("Returning unget token " + tok);
			return tok;
		}

		for (;;) {
			Source	s = getSource();
			if (s == null) {
				if (inputs.Count == 0)
                    return new Token(Token.EOF);
				Source	t = inputs[0];
                inputs.RemoveAt(0);
				push_source(t, true);
				if (getFeature(Feature.LINEMARKERS))
					return line_token(t.getLine(), t.getName(), " 1");
				continue;
			}
			Token	tok = s.token();
			/* XXX Refactor with skipline() */
            if(tok.getType() == Token.EOF && s.isAutopop()) {
				// System.out.println("Autopop " + s);
				pop_source();
				Source	t = getSource();
				if (getFeature(Feature.LINEMARKERS)
						&& s.isNumbered()
						&& t != null) {
					/* We actually want 'did the nested source
					 * contain a newline token', which isNumbered()
					 * approximates. This is not perfect, but works. */
					return line_token(t.getLine() + 1, t.getName(), " 2");
				}
				continue;
			}
			if (getFeature(Feature.DEBUG))
				System.Console.Error.WriteLine("Returning fresh token " + tok);
			return tok;
		}
	}

	private void source_untoken(Token tok) {
		if (this._source_token != null)
			throw new InvalidOperationException("Cannot return two tokens");
		this._source_token = tok;
	}

	private bool isWhite(Token tok) {
		int	type = tok.getType();
        return (type == Token.WHITESPACE)
            || (type == Token.CCOMMENT)
            || (type == Token.CPPCOMMENT);
	}

	private Token source_token_nonwhite() {
		Token	tok;
		do {
			tok = source_token();
		} while (isWhite(tok));
		return tok;
	}

	/**
	 * Returns an Token.NL or an Token.EOF token.
	 *
	 * The metadata on the token will be correct, which is better
	 * than generating a new one.
	 *
	 * This method can, as of recent patches, return a P_LINE token.
	 */
	private Token source_skipline(bool white) {
		// (new Exception("skipping line")).printStackTrace(System.out);
		Source	s = getSource();
		Token	tok = s.skipline(white);
		/* XXX Refactor with source_token() */
		if (tok.getType() == Token.EOF && s.isAutopop()) {
			// System.out.println("Autopop " + s);
			pop_source();
			Source	t = getSource();
			if (getFeature(Feature.LINEMARKERS)
					&& s.isNumbered()
					&& t != null) {
				/* We actually want 'did the nested source
				 * contain a newline token', which isNumbered()
				 * approximates. This is not perfect, but works. */
				return line_token(t.getLine() + 1, t.getName(), " 2");
			}
		}
		return tok;
	}

	/* processes and expands a macro. */
	private bool macro(Macro m, Token orig) {
		Token			tok;
		List<Argument>	args;

		// System.out.println("pp: expanding " + m);

		if (m.isFunctionLike()) {
			for (;;) {
				tok = source_token();
				// System.out.println("pp: open: token is " + tok);
				switch (tok.getType()) {
					case Token.WHITESPACE:	/* XXX Really? */
					case Token.CCOMMENT:
					case Token.CPPCOMMENT:
					case Token.NL:
						break;	/* continue */
					case '(':
                        goto BREAK_OPEN;
					default:
						source_untoken(tok);
						return false;
				}
			}
        BREAK_OPEN:

			// tok = expanded_token_nonwhite();
			tok = source_token_nonwhite();

			/* We either have, or we should have args.
			 * This deals elegantly with the case that we have
			 * one empty arg. */
			if (tok.getType() != ')' || m.getArgs() > 0) {
				args = new List<Argument>();

				Argument		arg = new Argument();
				int				depth = 0;
				bool			space = false;

				ARGS: for (;;) {
					// System.out.println("pp: arg: token is " + tok);
					switch (tok.getType()) {
                        case Token.EOF:
							error(tok, "EOF in macro args");
							return false;

						case ',':
							if (depth == 0) {
								if (m.isVariadic() &&
									/* We are building the last arg. */
									args.Count == m.getArgs() - 1) {
									/* Just add the comma. */
									arg.addToken(tok);
								}
								else {
									args.Add(arg);
									arg = new Argument();
								}
							}
							else {
								arg.addToken(tok);
							}
							space = false;
							break;
						case ')':
							if (depth == 0) {
								args.Add(arg);
								goto BREAK_ARGS;
							}
							else {
								depth--;
								arg.addToken(tok);
							}
							space = false;
							break;
						case '(':
							depth++;
							arg.addToken(tok);
							space = false;
							break;

                        case Token.WHITESPACE:
						case Token.CCOMMENT:
						case Token.CPPCOMMENT:
							/* Avoid duplicating spaces. */
							space = true;
							break;

						default:
							/* Do not put space on the beginning of
							 * an argument token. */
							if (space && arg.Count != 0)
								arg.addToken(Token.space);
							arg.addToken(tok);
							space = false;
							break;

					}
					// tok = expanded_token();
					tok = source_token();
				}
            BREAK_ARGS:

                if(m.isVariadic() && args.Count < m.getArgs()) {
                    args.Add(new Argument());
                }
				/* space may still be true here, thus trailing space
				 * is stripped from arguments. */

				if (args.Count != m.getArgs()) {
					error(tok,
							"macro " + m.getName() +
							" has " + m.getArgs() + " parameters " +
							"but given " + args.Count + " args");
					/* We could replay the arg tokens, but I
					 * note that GNU cpp does exactly what we do,
					 * i.e. output the macro name and chew the args.
					 */
					return false;
				}

				/*
				for (Argument a : args)
					a.expand(this);
				*/

				for (int i = 0; i < args.Count; i++) {
					args[i].expand(this);
				}

				// System.out.println("Macro " + m + " args " + args);
			}
			else {
				/* nargs == 0 and we (correctly) got () */
				args = null;
			}

		}
		else {
			/* Macro without args. */
				args = null;
		}

		if (m == __LINE__) {
			push_source(new FixedTokenSource(
					new Token[] { new Token(Token.INTEGER,
							orig.getLine(), orig.getColumn(),
							orig.getLine().ToString(),
							orig.getLine()) }
						), true);
		}
		else if (m == __FILE__) {
			StringBuilder	buf = new StringBuilder("\"");
			String			name = getSource().getName();
			if (name == null)
				name = "<no file>";
			for (int i = 0; i < name.Length; i++) {
				char	c = name[i];
				switch (c) {
					case '\\':
						buf.Append("\\\\");
						break;
					case '"':
						buf.Append("\\\"");
						break;
					default:
						buf.Append(c);
						break;
				}
			}
			buf.Append("\"");
			String			text = buf.ToString();
			push_source(new FixedTokenSource(
                    new Token[] { new Token(Token.STRING,
							orig.getLine(), orig.getColumn(),
							text, text) }
						), true);
		}
		else if (m == __COUNTER__) {
			/* This could equivalently have been done by adding
			 * a special Macro subclass which overrides getTokens(). */
			int	value = this.counter++;
			push_source(new FixedTokenSource(
                    new Token[] { new Token(Token.INTEGER,
							orig.getLine(), orig.getColumn(),
							value.ToString(),
							value) }
						), true);
		}
		else {
			push_source(new MacroTokenSource(m, args), true);
		}

		return true;
	}

	/**
	 * Expands an argument.
	 */
	/* I'd rather this were done lazily, but doing so breaks spec. */
	internal List<Token> expand(List<Token> arg) {
		List<Token>	expansion = new List<Token>();
		bool		space = false;

		push_source(new FixedTokenSource(arg), false); 

		for (;;) {
			Token	tok = expanded_token();
			switch (tok.getType()) {
                case Token.EOF:
					goto BREAK_EXPANSION;

                case Token.WHITESPACE:
                case Token.CCOMMENT:
                case Token.CPPCOMMENT:
					space = true; 
					break;

				default:
					if (space &&  expansion.Count != 0)
						expansion.Add(Token.space);
					expansion.Add(tok);
					space = false;
					break;
			}
		}
        BREAK_EXPANSION:

		pop_source();

		return expansion;
	}

	/* processes a #define directive */
	private Token define() {
		Token	tok = source_token_nonwhite();
		if (tok.getType() != Token.IDENTIFIER) {
			error(tok, "Expected Token.IDENTIFIER");
			return source_skipline(false);
		}
		/* if predefined */

		String			name = tok.getText();
		if ("defined" == name) {
			error(tok, "Cannot redefine name 'defined'");
			return source_skipline(false);
		}

		Macro			m = new Macro(getSource(), name);
		List<String>	args;

		tok = source_token();
		if (tok.getType() == '(') {
			tok = source_token_nonwhite();
			if (tok.getType() != ')') {
				args = new List<String>();
				for (;;) {
					switch (tok.getType()) {
                        case Token.IDENTIFIER:
                            if(m.isVariadic()) {
                                throw new Exception();
                            }
							args.Add(tok.getText());
							break;
                        case Token.ELLIPSIS:
                            m.setVariadic(true);
                            args.Add("__VA_ARGS__");
                            break;
                        case Token.NL:
                        case Token.EOF:
							error(tok,
								"Unterminated macro parameter list");
							return tok;
						default:
							error(tok,
								"error in macro parameters: " +
								tok.getText());
							return source_skipline(false);
					}
					tok = source_token_nonwhite();
					switch (tok.getType()) {
						case ',':
							break;
                        case Token.ELLIPSIS:
							tok = source_token_nonwhite();
							if (tok.getType() != ')')
								error(tok,
									"ellipsis must be on last argument");
							m.setVariadic(true);
							goto BREAK_ARGS;
						case ')':
							goto BREAK_ARGS;

						case Token.NL:
						case Token.EOF:
							/* Do not skip line. */
							error(tok,
								"Unterminated macro parameters");
							return tok;
						default:
							error(tok,
								"Bad token in macro parameters: " +
								tok.getText());
							return source_skipline(false);
					}
					tok = source_token_nonwhite();
				}
            BREAK_ARGS:;
			}
			else {
                System.Diagnostics.Debug.Assert(tok.getType() == ')', "Expected ')'");
                args = new List<string>();
			}

			m.setArgs(args);
		}
		else {
			/* For searching. */
            args = new List<string>();
			source_untoken(tok);
		}

		/* Get an expansion for the macro, using IndexOf. */
		bool	space = false;
		bool	paste = false;
		int		idx;

		/* Ensure no space at start. */
		tok = source_token_nonwhite();
		for (;;) {
			switch (tok.getType()) {
                case Token.EOF:
					goto BREAK_EXPANSION;
                case Token.NL:
					goto BREAK_EXPANSION;

                case Token.CCOMMENT:
                case Token.CPPCOMMENT:
					/* XXX This is where we implement GNU's cpp -CC. */
					// break;
                case Token.WHITESPACE:
					if (!paste)
						space = true;
					break;

				/* Paste. */
                case Token.PASTE:
					space = false;
					paste = true;
                    m.addPaste(new Token(Token.M_PASTE,
							tok.getLine(), tok.getColumn(),
							"#" + "#", null));
					break;

				/* Stringify. */
				case '#':
					if (space)
                        m.addToken(Token.space);
					space = false;
					Token	la = source_token_nonwhite();
                    if(la.getType() == Token.IDENTIFIER &&
						((idx = args.IndexOf(la.getText())) != -1)) {
                            m.addToken(new Token(Token.M_STRING,
								la.getLine(), la.getColumn(),
								"#" + la.getText(),
								idx));
					}
					else {
						m.addToken(tok);
						/* Allow for special processing. */
						source_untoken(la);
					}
					break;

                case Token.IDENTIFIER:
					if (space)
						m.addToken(Token.space);
					space = false;
					paste = false;
					idx = args.IndexOf(tok.getText());
					if (idx == -1)
						m.addToken(tok);
					else
						m.addToken(new Token(Token.M_ARG,
								tok.getLine(), tok.getColumn(),
								tok.getText(),
								idx));
					break;

				default:
					if (space)
						m.addToken(Token.space);
					space = false;
					paste = false;
					m.addToken(tok);
					break;
			}
			tok = source_token();
		}
        BREAK_EXPANSION:

		if (getFeature(Feature.DEBUG))
			System.Console.Error.WriteLine("Defined macro " + m);
		addMacro(m);

		return tok;	/* Token.NL or Token.EOF. */
	}

	private Token undef() {
		Token	tok = source_token_nonwhite();
		if (tok.getType() != Token.IDENTIFIER) {
			error(tok,
				"Expected identifier, not " + tok.getText());
            if(tok.getType() == Token.NL || tok.getType() == Token.EOF)
				return tok;
		}
		else {
			Macro	m;
            macros.TryGetValue(tok.getText(), out m);
			if (m != null) {
				/* XXX error if predefined */
				macros.Remove(m.getName());
			}
		}
		return source_skipline(true);
	}

	/**
	 * Attempts to include the given file.
	 *
	 * User code may override this method to implement a virtual
	 * file system.
	 */
	private bool include(VirtualFile file, bool isImport, bool checkOnly) {
		// System.out.println("Try to include " + file);
		if (!file.isFile())
			return false;
        
        if(!checkOnly) {
            if(isImport) {
                if(_importedPaths.Contains(file.getPath())) {
                    return true;
                }

                _importedPaths.Add(file.getPath());
            }

            if(getFeature(Feature.DEBUG))
                System.Console.WriteLine("pp: including " + file);

            push_source(file.getSource(), true);
        }
		return true;
	}

	/**
	 * Includes a file from an include path, by name.
	 */
	private bool include(IEnumerable<String> path, String name, bool isImport, bool checkOnly) {
		foreach (String dir in path) {
			VirtualFile	file = filesystem.getFile(dir, name);
			if (include(file, isImport, checkOnly))
				return true;
		}
		return false;
	}

    private bool includeFramework(IEnumerable<string> path, string name, bool isImport, bool checkOnly)
    {
        string[] framework = name.Split(new char[] { '/' }, 2);
        if(framework.Length < 2) {
            return false;
        }
        name = Path.Combine(Path.Combine(framework[0] + ".framework", "Headers"), framework[1]);

        foreach(String dir in path) {
            VirtualFile file = filesystem.getFile(dir, name);
            if(include(file, isImport, checkOnly))
                return true;
        }
        return false;

    }

	/**
	 * Handles an include directive.
	 */
	private bool include(String parent, int line, String name, bool quoted, bool isImport, bool checkOnly) {
		VirtualFile	pdir = null;
		if (quoted) {
			VirtualFile	pfile = filesystem.getFile(parent);
			pdir = pfile.getParentFile();
			VirtualFile	ifile = pdir.getChildFile(name);
            if(include(ifile, isImport, checkOnly))
				return true;
            if(include(quoteincludepath, name, isImport, checkOnly))
				return true;
		}

        if(include(sysincludepath, name, isImport, checkOnly))
			return true;

        if(includeFramework(frameworkspath, name, isImport, checkOnly)) {
            return true;
        }
        if(checkOnly) {
            return false;
        }

		StringBuilder	buf = new StringBuilder();
		buf.Append("File not found: ").Append(name);
		buf.Append(" in");
		if (quoted) {
			buf.Append(" .").Append('(').Append(pdir).Append(')');
			foreach (String dir in quoteincludepath)
				buf.Append(" ").Append(dir);
		}
		foreach (String dir in sysincludepath)
			buf.Append(" ").Append(dir);
		error(line, 0, buf.ToString());
        return false;
	}

    private bool has_feature() {
        Token tok;
        tok = token_nonwhite();
        if(tok.getType() != '(') {
            throw new Exception();
        }
        tok = token_nonwhite();
        string feature = tok.getText();

                tok = token_nonwhite();
        if(tok.getType() != ')') {
            throw new Exception();
        }
        switch(feature) {

            case "address_sanitizer": return true; //, LangOpts.Sanitize.Address)
            case "attribute_analyzer_noreturn": return true;
           case "attribute_availability": return true;
           case "attribute_availability_with_message": return true;
           case "attribute_cf_returns_not_retained": return true;
           case "attribute_cf_returns_retained": return true;
           case "attribute_deprecated_with_message": return true;
           case "attribute_ext_vector_type": return true;
           case "attribute_ns_returns_not_retained": return true;
           case "attribute_ns_returns_retained": return true;
           case "attribute_ns_consumes_self": return true;
           case "attribute_ns_consumed": return true;
           case "attribute_cf_consumed": return true;
           case "attribute_objc_ivar_unused": return true;
           case "attribute_objc_method_family": return true;
           case "attribute_overloadable": return true;
           case "attribute_unavailable_with_message": return true;
           case "attribute_unused_on_fields": return true;
           case "blocks": return true; //, LangOpts.Blocks)
           case "c_thread_safety_attributes": return true;
           case "cxx_exceptions": return true; //, LangOpts.CXXExceptions)
           case "cxx_rtti": return true; //, LangOpts.RTTI)
           case "enumerator_attributes": return true;
           case "memory_sanitizer": return true; //, LangOpts.Sanitize.Memory)
           case "thread_sanitizer": return true; //, LangOpts.Sanitize.Thread)
           case "dataflow_sanitizer": return true; //, LangOpts.Sanitize.DataFlow)

           case "objc_arr": return true; //, LangOpts.ObjCAutoRefCount) // FIXME: REMOVE?
           case "objc_arc": return true; //, LangOpts.ObjCAutoRefCount)
           case "objc_arc_weak": return true; //, LangOpts.ObjCARCWeak)
           case "objc_default_synthesize_properties": return true; //, LangOpts.ObjC2)
           case "objc_fixed_enum": return true; //, LangOpts.ObjC2)
           case "objc_instancetype": return true; //, LangOpts.ObjC2)
           case "objc_modules": return true; //, LangOpts.ObjC2 && LangOpts.Modules)
           case "objc_nonfragile_abi": return true; //, LangOpts.ObjCRuntime.isNonFragile())
           case "objc_property_explicit_atomic": return true; // Does clang support explicit "atomic" keyword?
           case "objc_protocol_qualifier_mangling": return true;
           case "objc_weak_class": return true; //, LangOpts.ObjCRuntime.hasWeakClassImport())
           case "ownership_holds": return true;
           case "ownership_returns": return true;
           case "ownership_takes": return true;
           case "objc_bool": return true;
           case "objc_subscripting": return true; //, LangOpts.ObjCRuntime.isNonFragile())
           case "objc_array_literals": return true; //, LangOpts.ObjC2)
           case "objc_dictionary_literals": return true; //, LangOpts.ObjC2)
           case "objc_boxed_expressions": return true; //, LangOpts.ObjC2)
           case "arc_cf_code_audited": return true;
           // C11 features
           case "c_alignas": return true; //, LangOpts.C11)
           case "c_atomic": return true; //, LangOpts.C11)
           case "c_generic_selections": return true; //, LangOpts.C11)
           case "c_static_assert": return true; //, LangOpts.C11)
            case "c_thread_local": return true; // LangOpts.C11 && PP.getTargetInfo().isTLSSupported())
           // C++11 features
           case "cxx_access_control_sfinae": return true; //, LangOpts.CPlusPlus11)
           case "cxx_alias_templates": return true; //, LangOpts.CPlusPlus11)
           case "cxx_alignas": return true; //, LangOpts.CPlusPlus11)
           case "cxx_atomic": return true; //, LangOpts.CPlusPlus11)
           case "cxx_attributes": return true; //, LangOpts.CPlusPlus11)
           case "cxx_auto_type": return true; //, LangOpts.CPlusPlus11)
           case "cxx_constexpr": return true; //, LangOpts.CPlusPlus11)
           case "cxx_decltype": return true; //, LangOpts.CPlusPlus11)
           case "cxx_decltype_incomplete_return_types": return true; //, LangOpts.CPlusPlus11)
           case "cxx_default_function_template_args": return true; //, LangOpts.CPlusPlus11)
           case "cxx_defaulted_functions": return true; //, LangOpts.CPlusPlus11)
           case "cxx_delegating_constructors": return true; //, LangOpts.CPlusPlus11)
           case "cxx_deleted_functions": return true; //, LangOpts.CPlusPlus11)
           case "cxx_explicit_conversions": return true; //, LangOpts.CPlusPlus11)
           case "cxx_generalized_initializers": return true; //, LangOpts.CPlusPlus11)
           case "cxx_implicit_moves": return true; //, LangOpts.CPlusPlus11)
           case "cxx_inheriting_constructors": return true; //, LangOpts.CPlusPlus11)
           case "cxx_inline_namespaces": return true; //, LangOpts.CPlusPlus11)
           case "cxx_lambdas": return true; //, LangOpts.CPlusPlus11)
           case "cxx_local_type_template_args": return true; //, LangOpts.CPlusPlus11)
           case "cxx_nonstatic_member_init": return true; //, LangOpts.CPlusPlus11)
           case "cxx_noexcept": return true; //, LangOpts.CPlusPlus11)
           case "cxx_nullptr": return true; //, LangOpts.CPlusPlus11)
           case "cxx_override_control": return true; //, LangOpts.CPlusPlus11)
           case "cxx_range_for": return true; //, LangOpts.CPlusPlus11)
           case "cxx_raw_string_literals": return true; //, LangOpts.CPlusPlus11)
           case "cxx_reference_qualified_functions": return true; //, LangOpts.CPlusPlus11)
           case "cxx_rvalue_references": return true; //, LangOpts.CPlusPlus11)
           case "cxx_strong_enums": return true; //, LangOpts.CPlusPlus11)
           case "cxx_static_assert": return true; //, LangOpts.CPlusPlus11)
            case "cxx_thread_local": return true; //LangOpts.CPlusPlus11 && PP.getTargetInfo().isTLSSupported())
           case "cxx_trailing_return": return true; //, LangOpts.CPlusPlus11)
           case "cxx_unicode_literals": return true; //, LangOpts.CPlusPlus11)
           case "cxx_unrestricted_unions": return true; //, LangOpts.CPlusPlus11)
           case "cxx_user_literals": return true; //, LangOpts.CPlusPlus11)
           case "cxx_variadic_templates": return true; //, LangOpts.CPlusPlus11)
           // C++1y features
           case "cxx_aggregate_nsdmi": return true; //, LangOpts.CPlusPlus1y)
           case "cxx_binary_literals": return true; //, LangOpts.CPlusPlus1y)
           case "cxx_contextual_conversions": return true; //, LangOpts.CPlusPlus1y)
           case "cxx_decltype_auto": return true; //, LangOpts.CPlusPlus1y)
           case "cxx_generic_lambdas": return true; //, LangOpts.CPlusPlus1y)
           case "cxx_init_captures": return true; //, LangOpts.CPlusPlus1y)
           case "cxx_relaxed_constexpr": return true; //, LangOpts.CPlusPlus1y)
           case "cxx_return_type_deduction": return true; //, LangOpts.CPlusPlus1y)
           case "cxx_variable_templates": return true; //, LangOpts.CPlusPlus1y)
           case "has_nothrow_assign": return true; //, LangOpts.CPlusPlus)
           case "has_nothrow_copy": return true; //, LangOpts.CPlusPlus)
           case "has_nothrow_constructor": return true; //, LangOpts.CPlusPlus)
           case "has_trivial_assign": return true; //, LangOpts.CPlusPlus)
           case "has_trivial_copy": return true; //, LangOpts.CPlusPlus)
           case "has_trivial_constructor": return true; //, LangOpts.CPlusPlus)
           case "has_trivial_destructor": return true; //, LangOpts.CPlusPlus)
           case "has_virtual_destructor": return true; //, LangOpts.CPlusPlus)
           case "is_abstract": return true; //, LangOpts.CPlusPlus)
           case "is_base_of": return true; //, LangOpts.CPlusPlus)
           case "is_class": return true; //, LangOpts.CPlusPlus)
           case "is_constructible": return true; //, LangOpts.CPlusPlus)
           case "is_convertible_to": return true; //, LangOpts.CPlusPlus)
           case "is_empty": return true; //, LangOpts.CPlusPlus)
           case "is_enum": return true; //, LangOpts.CPlusPlus)
           case "is_final": return true; //, LangOpts.CPlusPlus)
           case "is_literal": return true; //, LangOpts.CPlusPlus)
           case "is_standard_layout": return true; //, LangOpts.CPlusPlus)
           case "is_pod": return true; //, LangOpts.CPlusPlus)
           case "is_polymorphic": return true; //, LangOpts.CPlusPlus)
           case "is_sealed": return true; //, LangOpts.MicrosoftExt)
           case "is_trivial": return true; //, LangOpts.CPlusPlus)
           case "is_trivially_assignable": return true; //, LangOpts.CPlusPlus)
           case "is_trivially_constructible": return true; //, LangOpts.CPlusPlus)
           case "is_trivially_copyable": return true; //, LangOpts.CPlusPlus)
           case "is_union": return true; //, LangOpts.CPlusPlus)
           case "modules": return true; //, LangOpts.Modules)
            case "tls": return true; // PP.getTargetInfo().isTLSSupported())
           case "underlying_type": return true; //, LangOpts.CPlusPlus)
            default:
                return false;
        }


        
    }

    private bool has_include(bool next)
    {
        LexerSource lexer = (LexerSource)source;
        string name;
        bool quoted;

        Token tok;
        tok = token_nonwhite();
        if(tok.getType() != '(') {
            throw new Exception();
        }

        lexer.setInclude(true);
        tok = token_nonwhite();

        if(tok.getType() == Token.STRING) {
            /* XXX Use the original text, not the value.
             * Backslashes must not be treated as escapes here. */
            StringBuilder buf = new StringBuilder((String)tok.getValue());
            for(; ; ) {
                tok = token_nonwhite();
                switch(tok.getType()) {
                    case Token.STRING:
                        buf.Append((String)tok.getValue());
                        break;
                    case Token.NL:
                    case Token.EOF:
                        goto BREAK_HEADER;
                    default:
                        warning(tok,
                            "Unexpected token on #" + "include line");
                        return false;
                }
            }
        BREAK_HEADER:
            name = buf.ToString();
            quoted = true;
        } else if(tok.getType() == Token.HEADER) {
            name = (String)tok.getValue();
            quoted = false;
        } else {
            throw new Exception();
        }

        tok = token_nonwhite();
        if(tok.getType() != ')') {
            throw new Exception();
        }

        return include(source.getPath(), tok.getLine(), name, quoted, false, true);
    }

	private Token include(bool next, bool isImport) {
		LexerSource	lexer = (LexerSource)source;
		try {
			lexer.setInclude(true);
			Token	tok = token_nonwhite();

			String	name;
			bool	quoted;

            if(tok.getType() == Token.STRING) {
				/* XXX Use the original text, not the value.
				 * Backslashes must not be treated as escapes here. */
				StringBuilder	buf = new StringBuilder((String)tok.getValue());
				for (;;) {
					tok = token_nonwhite();
					switch (tok.getType()) {
						case Token.STRING:
							buf.Append((String)tok.getValue());
							break;
						case Token.NL:
                        case Token.EOF:
							goto BREAK_HEADER;
						default:
							warning(tok,
								"Unexpected token on #"+"include line");
							return source_skipline(false);
					}
				}
            BREAK_HEADER:
				name = buf.ToString();
				quoted = true;
			} else if(tok.getType() == Token.HEADER) {
				name = (String)tok.getValue();
				quoted = false;
				tok = source_skipline(true);
			}
			else {
				error(tok,
					"Expected string or header, not " + tok.getText());
				switch (tok.getType()) {
                    case Token.NL:
                    case Token.EOF:
						return tok;
					default:
						/* Only if not a Token.NL or Token.EOF already. */
						return source_skipline(false);
				}
			}

			/* Do the inclusion. */
			include(source.getPath(), tok.getLine(), name, quoted, isImport, false);

			/* 'tok' is the 'nl' after the include. We use it after the
			 * #line directive. */
			if (getFeature(Feature.LINEMARKERS))
				return line_token(1, source.getName(), " 1");
			return tok;
		}
		finally {
			lexer.setInclude(false);
		}
	}

	protected void pragma(Token name, List<Token> value) {
		warning(name, "Unknown #" + "pragma: " + name.getText());
	}

	private Token pragma() {
		Token		name;

		for (;;) {
			Token	tok = token();
			switch (tok.getType()) {
                case Token.EOF:
					/* There ought to be a newline before Token.EOF.
					 * At least, in any skipline context. */
					/* XXX Are we sure about this? */
					warning(tok,
						"End of file in #" + "pragma");
					return tok;
                case Token.NL:
					/* This may contain one or more newlines. */
					warning(tok,
						"Empty #" + "pragma");
					return tok;
				case Token.CCOMMENT:
				case Token.CPPCOMMENT:
				case Token.WHITESPACE:
					continue;
				case Token.IDENTIFIER:
					name = tok;
					goto BREAK_NAME;
				default:
					return source_skipline(false);
			}
		}
        BREAK_NAME:

		Token		tok2;
		List<Token>	value = new List<Token>();
		for (;;) {
			tok2 = token();
			switch (tok2.getType()) {
                case Token.EOF:
					/* There ought to be a newline before Token.EOF.
					 * At least, in any skipline context. */
					/* XXX Are we sure about this? */
					warning(tok2,
						"End of file in #" + "pragma");
					goto BREAK_VALUE;
                case Token.NL:
					/* This may contain one or more newlines. */
					goto BREAK_VALUE;
                case Token.CCOMMENT:
                case Token.CPPCOMMENT:
					break;
                case Token.WHITESPACE:
					value.Add(tok2);
					break;
				default:
					value.Add(tok2);
					break;
			}
		}
        BREAK_VALUE:

		pragma(name, value);

		return tok2;	/* The Token.NL. */
	}

	/* For #error and #warning. */
	private void error(Token pptok, bool is_error) {
		StringBuilder	buf = new StringBuilder();
		buf.Append('#').Append(pptok.getText()).Append(' ');
		/* Peculiar construction to ditch first whitespace. */
		Token		tok = source_token_nonwhite();
		for (;;) {
			switch (tok.getType()) {
				case Token.NL:
				case Token.EOF:
					goto BREAK_ERROR;
				default:
					buf.Append(tok.getText());
					break;
			}
			tok = source_token();
		}
        BREAK_ERROR:
		if (is_error)
			error(pptok, buf.ToString());
		else
			warning(pptok, buf.ToString());
	}




	/* This bypasses token() for #elif expressions.
	 * If we don't do this, then isActive() == false
	 * causes token() to simply chew the entire input line. */
	private Token expanded_token() {
		for (;;) {
			Token	tok = source_token();
			// System.out.println("Source token is " + tok);
			if (tok.getType() == Token.IDENTIFIER) {
                Macro m;
                macros.TryGetValue(tok.getText(), out m);
				if (m == null)
					return tok;
				if (source.isExpanding(m))
					return tok;
				if (macro(m, tok))
					continue;
			}
			return tok;
		}
	}

	private Token expanded_token_nonwhite() {
		Token	tok;
		do {
			tok = expanded_token();
			// System.out.println("expanded token is " + tok);
		} while (isWhite(tok));
		return tok;
	}


	private Token	_expr_token = null;

	private Token expr_token() {
        Token tok = _expr_token;

		if (tok != null) {
			// System.out.println("ungetting");
            _expr_token = null;
		}
		else {
			tok = expanded_token_nonwhite();
			// System.out.println("expt is " + tok);

			if (tok.getType() == Token.IDENTIFIER &&
				tok.getText() == "defined") {
				Token	la = source_token_nonwhite();
				bool	paren = false;
				if (la.getType() == '(') {
					paren = true;
					la = source_token_nonwhite();
				}

				// System.out.println("Core token is " + la);

				if (la.getType() != Token.IDENTIFIER) {
					error(la,
						"defined() needs identifier, not " +
						la.getText());
					tok = new Token(Token.INTEGER,
							la.getLine(), la.getColumn(),
							"0", 0);
				}
				else if (macros.ContainsKey(la.getText())) {
					// System.out.println("Found macro");
					tok = new Token(Token.INTEGER,
							la.getLine(), la.getColumn(),
							"1", 1);
                } else if(la.getText() == "__has_include_next" || la.getText() == "__has_include" || la.getText() == "__has_feature") {
                    tok = new Token(Token.INTEGER,
                            la.getLine(), la.getColumn(),
                            "1", 1);
                } else {
                    // System.out.println("Not found macro");
                    tok = new Token(Token.INTEGER,
                            la.getLine(), la.getColumn(),
                            "0", 0);
                }

				if (paren) {
					la = source_token_nonwhite();
					if (la.getType() != ')') {
						expr_untoken(la);
						error(la, "Missing ) in defined()");
					}
				}
			}
		}

		// System.out.println("expr_token returns " + tok);

		return tok;
	}

	private void expr_untoken(Token tok) {
        if(_expr_token != null)
			throw new Exception (
					"Cannot unget two expression tokens."
						);
        _expr_token = tok;
	}

	private int expr_priority(Token op) {
		switch (op.getType()) {
			case '/': return 11;
			case '%': return 11;
			case '*': return 11;
			case '+': return 10;
			case '-': return 10;
            case Token.LSH: return 9;
            case Token.RSH: return 9;
			case '<': return 8;
			case '>': return 8;
			case Token.LE: return 8;
			case Token.GE: return 8;
			case Token.EQ: return 7;
			case Token.NE: return 7;
			case '&': return 6;
			case '^': return 5;
			case '|': return 4;
            case Token.LAND: return 3;
            case Token.LOR: return 2;
			case '?': return 1;
			default:
				// System.out.println("Unrecognised operator " + op);
				return 0;
		}
	}

	private long expr(int priority) {
		/*
		System.out.flush();
		(new Exception("expr(" + priority + ") called")).printStackTrace();
		System.err.flush();
		*/

		Token	tok = expr_token();
		long	lhs, rhs;

		// System.out.println("Expr lhs token is " + tok);

		switch (tok.getType()) {
			case '(':
				lhs = expr(0);
				tok = expr_token();
				if (tok.getType() != ')') {
					expr_untoken(tok);
					error(tok, "missing ) in expression");
					return 0;
				}
				break;

			case '~': lhs = ~expr(11);              break;
			case '!': lhs =  expr(11) == 0 ? 1 : 0; break;
			case '-': lhs = -expr(11);              break;
            case Token.INTEGER:
				lhs = Convert.ToInt64(tok.getValue());
				break;
            case Token.CHARACTER:
				lhs = (long)((char)tok.getValue());
				break;
            case Token.IDENTIFIER:
                if(tok.getText() == "__has_include_next") {
                    lhs = has_include(true) ? 1 : 0;
                } else if(tok.getText() == "__has_include") {
                    lhs = has_include(false) ? 1 : 0;
                } else if(tok.getText() == "__has_feature") {
                    lhs = has_feature() ? 1 : 0;


                } else  {
                    if(warnings.HasFlag(Warning.UNDEF)) {
                        warning(tok, "Undefined token '" + tok.getText() +
                                "' encountered in conditional.");
                    }
				    lhs = 0;
                }
				break;

			default:
				expr_untoken(tok);
				error(tok,
					"Bad token in expression: " + tok.getText());
				return 0;
		}

		for (;;) {
			// System.out.println("expr: lhs is " + lhs + ", pri = " + priority);
			Token	op = expr_token();
			int		pri = expr_priority(op);	/* 0 if not a binop. */
			if (pri == 0 || priority >= pri) {
				expr_untoken(op);
				goto BREAK_EXPR;
			}
			rhs = expr(pri);
			// System.out.println("rhs token is " + rhs);
			switch (op.getType()) {
				case '/': 
					if (rhs == 0) {
						error(op, "Division by zero");
						lhs = 0;
					}
					else {
						lhs = lhs / rhs;
					}
					break;
				case '%': 
					if (rhs == 0) {
						error(op, "Modulus by zero");
						lhs = 0;
					}
					else {
						lhs = lhs % rhs;
					}
					break;
				case '*':  lhs = lhs * rhs; break;
				case '+':  lhs = lhs + rhs; break;
				case '-':  lhs = lhs - rhs; break;
				case '<':  lhs = lhs < rhs ? 1 : 0; break;
				case '>':  lhs = lhs > rhs ? 1 : 0; break;
				case '&':  lhs = lhs & rhs; break;
				case '^':  lhs = lhs ^ rhs; break;
				case '|':  lhs = lhs | rhs; break;

				case Token.LSH:  lhs = lhs << (int)rhs; break;
                case Token.RSH: lhs = lhs >> (int)rhs; break;
				case Token.LE:   lhs = lhs <= rhs ? 1 : 0; break;
				case Token.GE:   lhs = lhs >= rhs ? 1 : 0; break;
				case Token.EQ:   lhs = lhs == rhs ? 1 : 0; break;
				case Token.NE:   lhs = lhs != rhs ? 1 : 0; break;
				case Token.LAND: lhs = (lhs != 0) && (rhs != 0) ? 1 : 0; break;
				case Token.LOR:  lhs = (lhs != 0) || (rhs != 0) ? 1 : 0; break;

				case '?':
                    Token colon = expr_token();
                    if(colon.getText() != ":") {
                        throw new Exception();
                    }
                    long rrhs = expr(0);
                    if(lhs == 1) {
                        lhs = rhs;
                    } else {
                        lhs = rrhs;
                    }
                    break;

				default:
					error(op,
						"Unexpected operator " + op.getText());
					return 0;

			}
		}
        BREAK_EXPR:
		/*
		System.out.flush();
		(new Exception("expr returning " + lhs)).printStackTrace();
		System.err.flush();
		*/
		// System.out.println("expr returning " + lhs);

		return lhs;
	}

	private Token toWhitespace(Token tok) {
		String	text = tok.getText();
        int len = text.Length;
		bool	cr = false;
		int		nls = 0;

		for (int i = 0; i < len; i++) {
			char	c = text[i];

			switch (c) {
				case '\r':
					cr = true;
					nls++;
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
					nls++;
					break;
			}
		}

        char[] cbuf = new char[nls];
        for(int i = 0; i < nls; i++) { cbuf[i] = '\n'; }

        return new Token(Token.WHITESPACE,
				tok.getLine(), tok.getColumn(),
				new String(cbuf));
	}

	private Token _token() {

    SKIP_TOKEN:
		for (;;) {
			Token	tok;
			if (!isActive()) {
				try {
					/* XXX Tell lexer to ignore warnings. */
					source.setActive(false);
					tok = source_token();
				}
				finally {
					/* XXX Tell lexer to stop ignoring warnings. */
					source.setActive(true);
				}
				switch (tok.getType()) {
					case Token.HASH:
					case Token.NL:
					case Token.EOF:
						/* The preprocessor has to take action here. */
						break;
                    case Token.WHITESPACE:
						return tok;
					case Token.CCOMMENT:
					case Token.CPPCOMMENT:
						// Patch up to preserve whitespace.
						if (getFeature(Feature.KEEPALLCOMMENTS))
							return tok;
						if (!isActive())
							return toWhitespace(tok);
						if (getFeature(Feature.KEEPCOMMENTS))
							return tok;
						return toWhitespace(tok);
					default:
						// Return Token.NL to preserve whitespace.
						/* XXX This might lose a comment. */
						return source_skipline(false);
				}
			}
			else {
				tok = source_token();
			}

        LEX: switch (tok.getType()) {
				case Token.EOF:
					/* Pop the stacks. */
					return tok;

				case Token.WHITESPACE:
				case Token.NL:
                    //goto SKIP_TOKEN;
					return tok;

				case Token.CCOMMENT:
				case Token.CPPCOMMENT:

                    //if(!getFeature(Feature.KEEPALLCOMMENTS)) {
                    //    goto SKIP_TOKEN;
                    //}
					return tok;

				case '!': case '%': case '&':
				case '(': case ')': case '*':
				case '+': case ',': case '-':
				case '/': case ':': case ';':
				case '<': case '=': case '>':
				case '?': case '[': case ']':
				case '^': case '{': case '|':
				case '}': case '~': case '.':

				/* From Olivier Chafik for Objective C? */
				case '@':	
				/* The one remaining ASCII, might as well. */
				case '`':

				// case '#':

				case Token.AND_EQ:
				case Token.ARROW:
				case Token.CHARACTER:
				case Token.DEC:
				case Token.DIV_EQ:
				case Token.ELLIPSIS:
				case Token.EQ:
				case Token.GE:
				case Token.HEADER:	/* Should only arise from include() */
				case Token.INC:
				case Token.LAND:
				case Token.LE:
				case Token.LOR:
				case Token.LSH:
				case Token.LSH_EQ:
				case Token.SUB_EQ:
				case Token.MOD_EQ:
				case Token.MULT_EQ:
				case Token.NE:
				case Token.OR_EQ:
				case Token.PLUS_EQ:
				case Token.RANGE:
				case Token.RSH:
				case Token.RSH_EQ:
				case Token.STRING:
				case Token.XOR_EQ:
					return tok;

				case Token.INTEGER:
					return tok;

				case Token.IDENTIFIER:
                    Macro m;
                    macros.TryGetValue(tok.getText(), out m);
                    if(tok.getText() == "__has_include_next") {
                        Console.WriteLine();
                    }
					if (m == null)
						return tok;
					if (source.isExpanding(m))
						return tok;
					if (macro(m, tok))
						break;
					return tok;

				case Token.P_LINE:
					if (getFeature(Feature.LINEMARKERS))
						return tok;
					break;

				case Token.INVALID:
					if (getFeature(Feature.CSYNTAX))
						error(tok, (String)tok.getValue());
					return tok;

				default:
                    throw new Exception("Bad token " + tok);
					// break;

				case Token.HASH:
					tok = source_token_nonwhite();
					// (new Exception("here")).printStackTrace();
					switch (tok.getType()) {
						case Token.NL:
							goto BREAK_LEX;	/* Some code has #\n */
						case Token.IDENTIFIER:
							break;
						default:
							error(tok,
								"Preprocessor directive not a word " +
								tok.getText());
							return source_skipline(false);
					}
					int	_ppcmd = ppcmds[tok.getText()];
					if (_ppcmd == null) {
						error(tok,
							"Unknown preprocessor directive " +
							tok.getText());
						return source_skipline(false);
					}
					int	ppcmd = _ppcmd;

                PP: switch(ppcmd) {

                        case PP_DEFINE:
                            if(!isActive())
                                return source_skipline(false);
                            else
                                return define();
                        // break;

                        case PP_UNDEF:
                            if(!isActive())
                                return source_skipline(false);
                            else
                                return undef();
                        // break;

                        case PP_INCLUDE:
                            if(!isActive())
                                return source_skipline(false);
                            else
                                return include(false, false);
                        // break;
                        case PP_INCLUDE_NEXT:
                            if(!isActive())
                                return source_skipline(false);
                            if(!getFeature(Feature.INCLUDENEXT)) {
                                error(tok,
                                    "Directive include_next not enabled"
                                    );
                                return source_skipline(false);
                            }
                            return include(true, false);
                        // break;

                        case PP_WARNING:
                        case PP_ERROR:
                            if(!isActive())
                                return source_skipline(false);
                            else
                                error(tok, ppcmd == PP_ERROR);
                            break;

                        case PP_IF:
                            push_state();
                            if(!isActive()) {
                                return source_skipline(false);
                            }
                            _expr_token = null;
                            states.Peek().setActive(expr(0) != 0);
                            tok = expr_token();	/* unget */
                            if(tok.getType() == Token.NL)
                                return tok;
                            return source_skipline(true);
                        // break;

                        case PP_ELIF:
                            State state = states.Peek();
                            if(false) {
                                /* Check for 'if' */
                                ;
                            } else if(state.sawElse()) {
                                error(tok,
                                    "#elif after #" + "else");
                                return source_skipline(false);
                            } else if(!state.isParentActive()) {
                                /* Nested in skipped 'if' */
                                return source_skipline(false);
                            } else if(state.isActive()) {
                                /* The 'if' part got executed. */
                                state.setParentActive(false);
                                /* This is like # else # if but with
                                 * only one # end. */
                                state.setActive(false);
                                return source_skipline(false);
                            } else {
                                _expr_token = null;
                                state.setActive(expr(0) != 0);
                                tok = expr_token();	/* unget */
                                if(tok.getType() == Token.NL)
                                    return tok;
                                return source_skipline(true);
                            }
                        // break;

                        case PP_ELSE:
                            state = states.Peek();
                            if(false)
                                /* Check for 'if' */
                                ;
                            else if(state.sawElse()) {
                                error(tok,
                                    "#" + "else after #" + "else");
                                return source_skipline(false);
                            } else {
                                state.setSawElse();
                                state.setActive(!state.isActive());
                                return source_skipline(warnings.HasFlag(Warning.ENDIF_LABELS));
                            }
                        // break;

                        case PP_IFDEF:
                            push_state();
                            if(!isActive()) {
                                return source_skipline(false);
                            } else {
                                tok = source_token_nonwhite();
                                // System.out.println("ifdef " + tok);
                                if(tok.getType() != Token.IDENTIFIER) {
                                    error(tok,
                                        "Expected identifier, not " +
                                        tok.getText());
                                    return source_skipline(false);
                                } else {
                                    String text = tok.getText();
                                    bool exists =
                                        macros.ContainsKey(text);
                                    states.Peek().setActive(exists);
                                    return source_skipline(true);
                                }
                            }
                        // break;

                        case PP_IFNDEF:
                            push_state();
                            if(!isActive()) {
                                return source_skipline(false);
                            } else {
                                tok = source_token_nonwhite();
                                if(tok.getType() != Token.IDENTIFIER) {
                                    error(tok,
                                        "Expected identifier, not " +
                                        tok.getText());
                                    return source_skipline(false);
                                } else {
                                    String text = tok.getText();
                                    bool exists =
                                        macros.ContainsKey(text);
                                    states.Peek().setActive(!exists);
                                    return source_skipline(true);
                                }
                            }
                        // break;

                        case PP_ENDIF:
                            pop_state();
                            return source_skipline(warnings.HasFlag(Warning.ENDIF_LABELS));
                        // break;

                        case PP_LINE:
                            return source_skipline(false);
                        // break;

                        case PP_PRAGMA:
                            if(!isActive())
                                return source_skipline(false);
                            return pragma();
                        // break;

                        case PP_IMPORT:
                            if(!isActive())
                                return source_skipline(false);
                            else
                                return import();

                        default:
                            /* Actual unknown directives are
                             * processed above. If we get here,
                             * we succeeded the map lookup but
                             * failed to handle it. Therefore,
                             * this is (unconditionally?) fatal. */
                            // if (isActive()) /* XXX Could be warning. */
                            throw new Exception(
                                    "Internal error: Unknown directive "
                                    + tok);
                        // return source_skipline(false);
                    }
                BREAK_PP: ;
                    break;


			}
    BREAK_LEX: ;
		}
	}

    private Token import()
    {
        return include(false, true);
    }

	public Token token_nonwhite() {
		Token	tok;
		do {
			tok = _token();
		} while (isWhite(tok));
		return tok;
	}

	/**
	 * Returns the next preprocessor token.
	 *
	 * @see Token
	 * @throws LexerException if a preprocessing error occurs.
	 * @throws InternalException if an unexpected error condition arises.
	 */
	public Token token() {
		Token	tok = _token();
		if (getFeature(Feature.DEBUG))
			System.Console.Error.WriteLine("pp: Returning " + tok);
		return tok;
	}

	/* First ppcmd is 1, not 0. */
	public const int PP_DEFINE = 1;
	public const int PP_ELIF = 2;
	public const int PP_ELSE = 3;
	public const int PP_ENDIF = 4;
	public const int PP_ERROR = 5;
	public const int PP_IF = 6;
	public const int PP_IFDEF = 7;
	public const int PP_IFNDEF = 8;
	public const int PP_INCLUDE = 9;
	public const int PP_LINE = 10;
	public const int PP_PRAGMA = 11;
	public const int PP_UNDEF = 12;
	public const int PP_WARNING = 13;
	public const int PP_INCLUDE_NEXT = 14;
	public const int PP_IMPORT = 15;

    private static readonly Dictionary<String, int> ppcmds =
            new Dictionary<String, int>();

	static Preprocessor() {
		ppcmds.Add("define", PP_DEFINE);
        ppcmds.Add("elif", PP_ELIF);
        ppcmds.Add("else", PP_ELSE);
        ppcmds.Add("endif", PP_ENDIF);
        ppcmds.Add("error", PP_ERROR);
        ppcmds.Add("if", PP_IF);
        ppcmds.Add("ifdef", PP_IFDEF);
        ppcmds.Add("ifndef", PP_IFNDEF);
		ppcmds.Add("include", PP_INCLUDE);
        ppcmds.Add("line", PP_LINE);
		ppcmds.Add("pragma", PP_PRAGMA);
		ppcmds.Add("undef", PP_UNDEF);
		ppcmds.Add("warning", PP_WARNING);
		ppcmds.Add("include_next", PP_INCLUDE_NEXT);
		ppcmds.Add("import", PP_IMPORT);
	}


	override public String ToString() {
		StringBuilder	buf = new StringBuilder();

		Source	s = getSource();
		while (s != null) {
			buf.Append(" -> ").Append(s).Append("\n");
			s = s.getParent();
		}

		Dictionary<String,Macro>	macros = getMacros();
		List<String>		keys = new List<String>(
				macros.Keys
					);
        keys.Sort();
        foreach(string key in keys) {
            Macro macro = macros[key];
			buf.Append("#").Append("macro ").Append(macro).Append("\n");
		}

		return buf.ToString();
	}

	public void Dispose() {
		{
			Source	s = source;
			while (s != null) {
                s.close();
				s = s.getParent();
			}
		}
		foreach (Source s in inputs) {
			s.close();
		}
	}

}

}