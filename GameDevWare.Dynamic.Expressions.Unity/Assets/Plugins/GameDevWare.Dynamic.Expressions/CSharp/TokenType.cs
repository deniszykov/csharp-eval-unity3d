/*
	Copyright (c) 2016 Denis Zykov, GameDevWare.com

	This a part of "C# Eval()" Unity Asset - https://www.assetstore.unity3d.com/en/#!/content/56706

	THIS SOFTWARE IS DISTRIBUTED "AS-IS" WITHOUT ANY WARRANTIES, CONDITIONS AND
	REPRESENTATIONS WHETHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION THE
	IMPLIED WARRANTIES AND CONDITIONS OF MERCHANTABILITY, MERCHANTABLE QUALITY,
	FITNESS FOR A PARTICULAR PURPOSE, DURABILITY, NON-INFRINGEMENT, PERFORMANCE
	AND THOSE ARISING BY STATUTE OR FROM CUSTOM OR USAGE OF TRADE OR COURSE OF DEALING.

	This source code is distributed via Unity Asset Store,
	to use it in your project you should accept Terms of Service and EULA
	https://unity3d.com/ru/legal/as_terms
*/

namespace GameDevWare.Dynamic.Expressions.CSharp
{
	public enum TokenType
	{
		None,
		Number,
		Literal,
		Identifier,
		// arithmetic
		[Token("+")]
		Add,
		Plus,
		[Token("-")]
		Subtract,
		Minus,
		[Token("/")]
		Div,
		[Token("*")]
		Mul,
		[Token("**")]
		Pow,
		[Token("%")]
		Mod,
		// bitwise
		[Token("&")]
		And,
		[Token("|")]
		Or,
		[Token("^")]
		Xor,
		[Token("~")]
		Compl,
		[Token("<<")]
		Lshift,
		[Token(">>")]
		Rshift,
		// logical
		[Token("&&")]
		AndAlso,
		[Token("||")]
		OrElse,
		[Token("!")]
		Not,
		[Token(">")]
		Gt,
		[Token(">=")]
		Gte,
		[Token("<")]
		Lt,
		[Token("<=")]
		Lte,
		[Token("==")]
		Eq,
		[Token("!=")]
		Neq,
		// other
		[Token("?")]
		Cond,
		[Token("is")]
		Is,
		[Token("as")]
		As,
		[Token(":")]
		Colon,
		[Token(",")]
		Comma,
		[Token("??")]
		Coalesce,
		[Token("new")]
		New,
		// structure
		[Token(".")]
		Resolve,
		[Token("?.")]
		NullResolve,
		[Token("(")]
		Lparen,
		[Token(")")]
		Rparen,
		[Token("[")]
		Lbracket,
		[Token("?[")]
		NullIndex,
		[Token("]")]
		Rbracket,
		[Token("=>")]
		Lambda,
		Call,
		Arguments,
		Convert,
		Typeof,
		Default,
		Group,
		CheckedScope,
		UncheckedScope
	}
}
