using AuraLang.Exceptions.Scanner;
using AuraLang.Location;
using AuraLang.Token;
using Range = AuraLang.Location.Range;

namespace AuraLang.Scanner;

/// <summary>
/// 
/// </summary>
public class AuraScanner
{
	/// <summary>
	/// The Aura source code
	/// </summary>
	private readonly string _source;
	/// <summary>
	/// The starting index of the token currently being scanned
	/// </summary>
	private int _start;
	/// <summary>
	/// The starting character position on the current line of the token currently being scanned
	/// </summary>
	private int _startCharPos;
	/// <summary>
	/// The current index in the source code
	/// </summary>
	private int _current;
	/// <summary>
	/// The current character position on the current line of the token currently being scanned
	/// </summary>
	private int _currentCharPos;
	/// <summary>
	/// The current line in the Aura source code
	/// </summary>
	private int _line;
	/// <summary>
	/// Helps determine if an implicit semicolon should be added at the end of the current line. No semicolons
	/// are added at the end of a blank line.
	/// </summary>
	private bool _isLineBlank;
	private readonly List<Tok> _tokens;
	private readonly ScannerExceptionContainer _exContainer;
	private string FilePath { get; }

	public AuraScanner(string source, string filePath)
	{
		_source = source;
		_start = 0;
		_startCharPos = 0;
		_current = 0;
		_currentCharPos = 0;
		_line = 1;
		_isLineBlank = true;
		_tokens = new List<Tok>();
		FilePath = filePath;
		_exContainer = new ScannerExceptionContainer(filePath);
	}

	public List<Tok> ScanTokens()
	{
		// Scan each character of the source code
		for (var i = 0; i < _source.Length; i++)
		{
			SkipWhitespace();
			// Ensure that we haven't reached the end of the source before scanning the next character
			if (IsAtEnd()) break;
			// At the beginning of each token, we ensure that the `start` and `current` indices both point to the beginning
			// of the token
			_start = _current;
			_startCharPos = _currentCharPos;
			// Scan the first character of the next token
			var c = Advance();
			// Now that we have scanned a non-blank character, we know the current line isn't blank
			_isLineBlank = false;

			try
			{
				_tokens.Add(ScanToken(c));
			}
			catch (ScannerException ex)
			{
				_exContainer.Add(ex);
			}
		}

		if (!_exContainer.IsEmpty()) throw _exContainer;
		// We append an EOF token to the end of the returned list of tokens to clearly delineate the end of the source
		_tokens.Add(new Tok(
			typ: TokType.Eof,
			value: "eof",
			range: new Range(
				start: new Position(
					character: _currentCharPos,
					line: _line
				),
				end: new Position(
					character: _currentCharPos,
					line: _line
				)
			),
			line: _line));
		return _tokens;
	}

	private void SkipWhitespace()
	{
		while (!IsAtEnd() && IsWhitespace())
		{
			var ch = Advance();
			if (ch == '\n')
			{
				if (!_isLineBlank && _tokens[^1].Typ != TokType.LeftBrace)
					_tokens.Add(MakeSingleCharToken(TokType.Semicolon, ';'));
				_tokens.Add(MakeSingleCharToken(TokType.Newline, '\n'));
				_isLineBlank = true;
				_line++;
				_currentCharPos = 0;
			}
		}
	}

	private Tok ScanToken(char c)
	{
		// If the token begins with an alphabetical character, its either a keyword token or an indentifier
		if (IsAlpha(c))
		{
			return CheckIdentifier(c);
		}

		if (IsDigit(c))
		{
			return ParseNumber(c);
		}

		// If the token doesn't start with an alphabetical or numeric character, then we check if its a symbol
		switch (c)
		{
			case '(':
				return MakeSingleCharToken(TokType.LeftParen, c);
			case ')':
				return MakeSingleCharToken(TokType.RightParen, c);
			case '{':
				return MakeSingleCharToken(TokType.LeftBrace, c);
			case '}':
				return MakeSingleCharToken(TokType.RightBrace, c);
			case '[':
				return MakeSingleCharToken(TokType.LeftBracket, c);
			case ']':
				return MakeSingleCharToken(TokType.RightBracket, c);
			case '=':
				if (!IsAtEnd() && Peek() == '=') return MakeToken(TokType.EqualEqual, _start, _current);
				return MakeSingleCharToken(TokType.Equal, c);
			case '+':
				if (!IsAtEnd() && Peek() == '+') return MakeToken(TokType.PlusPlus, _start, _current);
				if (!IsAtEnd() && Peek() == '=') return MakeToken(TokType.PlusEqual, _start, _current);
				return MakeSingleCharToken(TokType.Plus, c);
			case '-':
				if (!IsAtEnd() && Peek() == '-') return MakeToken(TokType.MinusMinus, _start, _current);
				if (!IsAtEnd() && Peek() == '>') return MakeToken(TokType.Arrow, _start, _current);
				if (!IsAtEnd() && Peek() == '=') return MakeToken(TokType.MinusEqual, _start, _current);
				return MakeSingleCharToken(TokType.Minus, c);
			case '/':
				if (!IsAtEnd() && Peek() == '/')
				{
					if (_tokens.Count > 0 && _tokens[^1].Line == _line)
						_tokens.Add(MakeSingleCharToken(TokType.Semicolon, ';'));

					while (!IsAtEnd() && Peek() != '\n') Advance();
					return MakeToken(TokType.Comment, _start, _current - 1);
				}
				if (!IsAtEnd() && Peek() == '*')
				{
					Advance(); // Advance past the `*` character
					while (!IsAtEnd() && Peek() != '*' && PeekNext() != '/')
					{
						Advance();
						if (Peek() == '\n')
						{
							_tokens.Add(MakeToken(TokType.Comment, _start, _current - 1));
							_startCharPos = _currentCharPos - 1;
							_tokens.Add(MakeSingleCharToken(TokType.Semicolon, ';'));
							Advance();
							_line++;
							_startCharPos = 0;
							_currentCharPos = 0;
							_start = _current;
						}
					}

					Advance();
					Advance();
					return MakeToken(TokType.Comment, _start, _current - 1);
				}

				if (!IsAtEnd() && Peek() == '=') return MakeToken(TokType.SlashEqual, _start, _current);
				return MakeSingleCharToken(TokType.Slash, c);
			case '*':
				if (!IsAtEnd() && Peek() == '=') return MakeToken(TokType.StarEqual, _start, _current);
				return MakeSingleCharToken(TokType.Star, c);
			case '>':
				if (!IsAtEnd() && Peek() == '=') return MakeToken(TokType.GreaterEqual, _start, _current);
				return MakeSingleCharToken(TokType.Greater, c);
			case '<':
				if (!IsAtEnd() && Peek() == '=') return MakeToken(TokType.LessEqual, _start, _current);
				return MakeSingleCharToken(TokType.Less, c);
			case '!':
				if (!IsAtEnd() && Peek() == '=') return MakeToken(TokType.BangEqual, _start, _current);
				return MakeSingleCharToken(TokType.Bang, c);
			case '"':
				return ParseString();
			case '\'':
				return ParseChar();
			case ':':
				if (!IsAtEnd() && Peek() == '=') return MakeToken(TokType.ColonEqual, _start, _current);
				return MakeSingleCharToken(TokType.Colon, c);
			case ';':
				return MakeSingleCharToken(TokType.Semicolon, c);
			case '.':
				return MakeSingleCharToken(TokType.Dot, c);
			case ',':
				return MakeSingleCharToken(TokType.Comma, c);
			default:
				// If the character isn't an alphabetical or numeric character, and it isn't a valid symbol,
				// then it must be an invalid character
				throw new InvalidCharacterException(c, _line);
		}
	}

	/// <summary>
	/// Returns the current character in the scanner's source without advancing the <c>current</c> index
	/// </summary>
	/// <returns>The current character in the source code</returns>
	private char Peek()
	{
		return _source[_current];
	}

	private char PeekNext()
	{
		return _source[_current + 1];
	}

	/// <summary>
	/// Returns the current character in the scanner's source and advanced the <c>current</c> index
	/// </summary>
	/// <returns>The current character in the source code</returns>
	private char Advance()
	{
		var c = Peek();
		_current++;
		_currentCharPos++;
		return c;
	}

	/// <summary>
	/// Produces a token that corresponds to a single character in the scanner's source
	/// </summary>
	/// <param name="tokType">The type of the token</param>
	/// <param name="c">The value of the token</param>
	/// <returns>A token encapsulating the supplied information</returns>
	private Tok MakeSingleCharToken(TokType tokType, char c)
	{
		var s = "" + c;
		var range = new Range(
			start: new Position(
				character: _startCharPos,
				line: _line
			),
			end: new Position(
				character: _currentCharPos,
				line: _line
			)
		);
		return new Tok(tokType, s, range, _line);
	}

	/// <summary>
	/// Produces a token containing the characters between the <c>start</c> and <c>end</c> indices (inclusive) in the source
	/// </summary>
	/// <param name="tokType">The type of the token that will be produced</param>
	/// <param name="start">The start index in the source of the token's value</param>
	/// <param name="end">The end index (inclusive) in the source of the token's value</param>
	/// <returns>A token encapsulating the supplied information</returns>
	private Tok MakeToken(TokType tokType, int start, int end)
	{
		_currentCharPos++;
		var s = _source[start..(end + 1)];
		var range = new Range(
			start: new Position(
				character: _startCharPos,
				line: _line
			),
			end: new Position(
				character: _currentCharPos,
				line: _line
			)
		);
		_current = end + 1;
		return new Tok(tokType, s, range, _line);
	}

	/// <summary>
	/// Returns a token whose type is either the supplied <c>TokType</c> (if the <c>actual</c> value
	/// matches the <c>expected</c> value, or else <c>Identifier</c>
	/// </summary>
	/// <param name="tokType">The type of the token, if the <c>actual</c> matches the <c>expected</c></param>
	/// <param name="actual">The actual value from the source</param>
	/// <param name="expected">The expected value</param>
	/// <returns>A token containing the <c>actual</c> value and a token type depending on whether the <c>actual</c>
	/// value matched the <c>expected</c> value.</returns>
	private Tok CheckKeywordToken(TokType tokType, string actual, string expected)
	{
		if (actual != expected) tokType = TokType.Identifier;
		var range = new Range(
			start: new Position(
				character: _startCharPos,
				line: _line
			),
			end: new Position(
				character: _currentCharPos,
				line: _line
			)
		);
		return new Tok(tokType, actual, range, _line);
	}

	private Tok CheckIdentifier(char c)
	{
		var tok = "" + c;
		// Scan the entire token
		while (!IsAtEnd() && (IsAlpha(Peek()) || IsDigit(Peek()) || Peek() == '/')) tok += Advance();

		switch (tok[0])
		{
			case 'a':
				if (tok.Length >= 2)
				{
					switch (tok[1])
					{
						case 's':
							return CheckKeywordToken(TokType.As, tok, "as");
						case 'n':
							if (tok.Length >= 3)
							{
								switch (tok[2])
								{
									case 'y':
										return CheckKeywordToken(TokType.Any, tok, "any");
									case 'd':
										return CheckKeywordToken(TokType.And, tok, "and");
								}
							}

							break;
					}
				}

				break;
			case 'b':
				if (tok.Length >= 2)
				{
					switch (tok[1])
					{
						case 'o':
							return CheckKeywordToken(TokType.Bool, tok, "bool");
						case 'r':
							return CheckKeywordToken(TokType.Break, tok, "break");
					}
				}

				break;
			case 'c':
				if (tok.Length >= 2)
				{
					switch (tok[1])
					{
						case 'l':
							return CheckKeywordToken(TokType.Class, tok, "class");
						case 'h':
							if (tok.Length >= 3)
							{
								switch (tok[2])
								{
									case 'a':
										return CheckKeywordToken(TokType.Char, tok, "char");
									case 'e':
										return CheckKeywordToken(TokType.Check, tok, "check");
								}
							}
							break;
						case 'o':
							return CheckKeywordToken(TokType.Continue, tok, "continue");
					}
				}

				break;
			case 'd':
				return CheckKeywordToken(TokType.Defer, tok, "defer");
			case 'e':
				if (tok.Length >= 2)
				{
					switch (tok[1])
					{
						case 'l':
							return CheckKeywordToken(TokType.Else, tok, "else");
						case 'r':
							return CheckKeywordToken(TokType.Error, tok, "error");
					}
				}
				break;
			case 'f':
				if (tok.Length >= 2)
				{
					switch (tok[1])
					{
						case 'l':
							return CheckKeywordToken(TokType.Float, tok, "float");
						case 'n':
							return CheckKeywordToken(TokType.Fn, tok, "fn");
						case 'a':
							return CheckKeywordToken(TokType.False, tok, "false");
						case 'o':
							if (tok.Length == 3) return CheckKeywordToken(TokType.For, tok, "for");
							else if (tok.Length > 3) return CheckKeywordToken(TokType.ForEach, tok, "foreach");
							break;
					}
				}

				break;
			case 'i':
				if (tok.Length == 2)
				{
					switch (tok[1])
					{
						case 'n':
							return CheckKeywordToken(TokType.In, tok, "in");
						case 'f':
							return CheckKeywordToken(TokType.If, tok, "if");
						case 's':
							return CheckKeywordToken(TokType.Is, tok, "is");
					}
				}
				else if (tok.Length > 2)
				{
					switch (tok[1])
					{
						case 'n':
							if (tok.Length == 3) return CheckKeywordToken(TokType.Int, tok, "int");
							if (tok.Length > 3)
							{
								switch (tok[3])
								{
									case 'e':
										return CheckKeywordToken(TokType.Interface, tok, "interface");
								}
							}

							break;
						case 'm':
							return CheckKeywordToken(TokType.Import, tok, "import");
					}
				}

				break;
			case 'l':
				return CheckKeywordToken(TokType.Let, tok, "let");
			case 'm':
				if (tok.Length >= 2)
				{
					switch (tok[1])
					{
						case 'a':
							return CheckKeywordToken(TokType.Map, tok, "map");
						case 'o':
							return CheckKeywordToken(TokType.Mod, tok, "mod");
						case 'u':
							return CheckKeywordToken(TokType.Mut, tok, "mut");
					}
				}

				break;
			case 'n':
				return CheckKeywordToken(TokType.Nil, tok, "nil");
			case 'o':
				return CheckKeywordToken(TokType.Or, tok, "or");
			case 'p':
				return CheckKeywordToken(TokType.Pub, tok, "pub");
			case 'r':
				if (tok.Length >= 3)
				{
					switch (tok[2])
					{
						case 't':
							return CheckKeywordToken(TokType.Return, tok, "return");
						case 's':
							return CheckKeywordToken(TokType.Result, tok, "result");
					}
				}

				break;
			case 's':
				if (tok.Length >= 4)
				{
					switch (tok[3])
					{
						case 'i':
							return CheckKeywordToken(TokType.String, tok, "string");
						case 'u':
							return CheckKeywordToken(TokType.Struct, tok, "struct");
					}
				}

				break;
			case 't':
				if (tok.Length >= 2)
				{
					switch (tok[1])
					{
						case 'r':
							return CheckKeywordToken(TokType.True, tok, "true");
						case 'h':
							return CheckKeywordToken(TokType.This, tok, "this");
					}
				}

				break;
			case 'w':
				return CheckKeywordToken(TokType.While, tok, "while");
			case 'y':
				return CheckKeywordToken(TokType.Yield, tok, "yield");
		}

		var range = new Range(
			start: new Position(
				character: _startCharPos,
				line: _line
			),
			end: new Position(
				character: _currentCharPos,
				line: _line
			)
		);

		return new Tok(
			typ: TokType.Identifier,
			value: tok,
			range: range,
			line: _line
		);
	}

	/// <summary>
	/// Parses a string literal into a token
	/// </summary>
	/// <returns>A token containing the string literal, and whose type is <c>StringLiteral</c></returns>
	private Tok ParseString()
	{
		string s = "";
		// Scan the entire token, up until the closing double quote
		while (!IsAtEnd() && Peek() != '"') s += Advance();
		// If we've reached the end of the file without encountering the closing ", we throw an exception
		if (IsAtEnd()) throw new UnterminatedStringException(_line);

		var range = new Range(
			start: new Position(
				character: _startCharPos,
				line: _line
			),
			end: new Position(
				character: _currentCharPos,
				line: _line
			)
		);

		// Advance past the closing "
		Advance();

		return new Tok(
			typ: TokType.StringLiteral,
			value: s,
			range: range,
			line: _line
		);
	}

	private Tok ParseChar()
	{
		string s = "";
		// Scan the entire token, up until the closing single quote
		while (!IsAtEnd() && Peek() != '\'') s += Advance();
		// If we've reached the end of the file without encountering the closing ', we throw an exception
		if (IsAtEnd()) throw new UnterminatedCharException(_line);
		// Advance past the closing '
		Advance();
		// Ensure that the char is a single character
		if (s.Length == 0) throw new EmptyCharException(_line);
		if (s.Length > 1) throw new CharLengthGreaterThanOneException(_line);

		var range = new Range(
			start: new Position(
				character: _startCharPos,
				line: _line
			),
			end: new Position(
				character: _currentCharPos,
				line: _line
			)
		);

		return new Tok(
			typ: TokType.CharLiteral,
			value: s,
			range: range,
			line: _line
		);
	}

	private Tok ParseNumber(char c)
	{
		// We start out assuming the number is an integer
		var ttype = TokType.IntLiteral;

		string s = "" + c;
		// Scan the entire token. Numbers in Aura can contain underscores to improve readability
		while (!IsAtEnd() && (IsDigit(Peek()) || Peek() == '_')) s += Advance();
		// When we reach the first non-numeric token, we check if its a dot - if so, we also scan the floating-point part
		// of the number
		if (!IsAtEnd() && Peek() == '.')
		{
			s += Advance();
			while (!IsAtEnd() && (IsDigit(Peek()) || Peek() == '_')) s += Advance();
			ttype = TokType.FloatLiteral;
		}

		var range = new Range(
			start: new Position(
				character: _startCharPos,
				line: _line
			),
			end: new Position(
				character: _currentCharPos,
				line: _line
			)
		);

		return new Tok(
			typ: ttype,
			value: s,
			range: range,
			line: _line
		);
	}

	/// <summary>
	/// Checks if we've advanced past the end of the scanner's source
	/// </summary>
	/// <returns>A boolean indicating if the end of the source has been reached</returns>
	private bool IsAtEnd() => _current >= _source.Length;

	/// <summary>
	/// Checks if the supplied character is alphabetic
	/// </summary>
	/// <param name="c">The character that will be checked to see if its alphabetic</param>
	/// <returns>A boolean indicating if the supplied characater is alphabetic</returns>
	private bool IsAlpha(char c) => c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c == '_';

	private bool IsWhitespace() => Peek() == ' ' || Peek() == '\n' || Peek() == '\t';

	private bool IsDigit(char c) => c >= '0' && c <= '9';
}
