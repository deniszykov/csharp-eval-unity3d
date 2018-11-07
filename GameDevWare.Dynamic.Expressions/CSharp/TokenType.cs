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

#pragma warning disable 1591
namespace GameDevWare.Dynamic.Expressions.CSharp
{
	/// <summary>
	/// Types of tokens
	/// </summary>
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
		Division,
		[Token("*")]
		Multiplication,
		[Token("**")]
		Power,
		[Token("%")]
		Modulo,
		// bitwise
		[Token("&")]
		And,
		[Token("|")]
		Or,
		[Token("^")]
		Xor,
		[Token("~")]
		Complement,
		[Token("<<")]
		LeftShift,
		[Token(">>")]
		RightShift,
		// logical
		[Token("&&")]
		AndAlso,
		[Token("||")]
		OrElse,
		[Token("!")]
		Not,
		[Token(">")]
		GreaterThan,
		[Token(">=")]
		GreaterThanOrEquals,
		[Token("<")]
		LesserThan,
		[Token("<=")]
		LesserThanOrEquals,
		[Token("==")]
		EqualsTo,
		[Token("!=")]
		NotEqualsTo,
		// other
		[Token("?")]
		Conditional,
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
		LeftParentheses,
		[Token(")")]
		RightParentheses,
		[Token("[")]
		LeftBracket,
		[Token("?[")]
		NullIndex,
		[Token("]")]
		RightBracket,
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
