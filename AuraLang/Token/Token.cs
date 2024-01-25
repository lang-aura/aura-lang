namespace AuraLang.Token;

public record struct Tok(TokType Typ, string Value, int Line);

public enum TokType
{
	LeftParen, // (
	RightParen, // )
	LeftBrace, // {
	RightBrace, // }
	LeftBracket, // [
	RightBracket, // ]
	Equal, // =
	EqualEqual, // ==
	Plus, // +
	Minus, // -
	Slash, // /
	Star, // *
	Greater, // >
	GreaterEqual, // >=
	Less, // <
	LessEqual, // <=
	Bang, // !
	BangEqual, // !=
	DoubleQuote, // "
	Colon, // :
	Semicolon, // ;
	Dot, // .
	Comma, // ,
	ColonEqual, // :=
	PlusPlus, // ++
	PlusEqual, // +=
	MinusEqual, // -=
	StarEqual, // *=
	SlashEqual, // /=
	MinusMinus, // --
	Arrow, // ->
	Newline, // \n
	Mod, // mod
	Fn, // fn
	String, // string
	Char, // char
	Int, // int
	Bool, // bool
	Any, // any
	Let, // let
	Return, // return
	Import, // import
	Pub, // pub
	If, // if
	Else, // else
	True, // true
	False, // false
	While, // while
	For, // for
	ForEach, // foreach
	In, // in
	Mut, // mut
	Float, // float
	Class, // class
	Defer, // defer
	And, // and
	Or, // or
	Nil, // nil
	This, // this
	As, // as
	Map, // map
	Break, // break
	Continue, // continue
	Yield, // yield
	Interface, // interface
	Is, // is
	Error, // error
	Check, // check
	Identifier,
	StringLiteral,
	CharLiteral,
	IntLiteral,
	FloatLiteral,
	Comment,
	Eof
}
