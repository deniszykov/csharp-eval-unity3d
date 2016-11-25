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

using System;
using System.Collections.Generic;
using System.Linq;

namespace GameDevWare.Dynamic.Expressions.CSharp
{
	public class Parser
	{
		private static readonly Dictionary<int, int> UnaryReplacement;
		private static readonly Dictionary<int, int> TokenPrecedence;
		private static readonly TokenType[] CondTerm = new[] { TokenType.Colon };
		private static readonly TokenType[] DefaultTerm = new[] { TokenType.Comma, TokenType.Rparen, TokenType.Rbracket };

		private readonly List<Token> tokens;
		private readonly Stack<ParserNode> stack;

		static Parser()
		{
			UnaryReplacement = new Dictionary<int, int>
			{
				{ (int)TokenType.Add, (int)TokenType.Plus },
				{ (int)TokenType.Subtract, (int)TokenType.Minus }
			};
			TokenPrecedence = new Dictionary<int, int>();
			var tokenPrecedenceList = (new[]
			{
				// Primary
				new[] {TokenType.Resolve, TokenType.NullResolve, TokenType.Call, TokenType.Typeof, TokenType.Default },
				// New
				new[] { TokenType.New },
				// Unary
				new[] {TokenType.Plus, TokenType.Minus, TokenType.Not, TokenType.Compl, TokenType.Convert},
				// Multiplicative
				new[] {TokenType.Div, TokenType.Mul, TokenType.Pow, TokenType.Mod},
				// Additive
				new[] {TokenType.Add, TokenType.Subtract},
				// Shift
				new[] {TokenType.Lshift, TokenType.Rshift},
				// Relational and type testing
				new[] {TokenType.Gt, TokenType.Gte, TokenType.Lt, TokenType.Lte, TokenType.Is, TokenType.As},
				// Equality
				new[] {TokenType.Eq, TokenType.Neq},
				// Logical AND
				new[] {TokenType.And},
				// Logical XOR
				new[] {TokenType.Xor},
				// Logical OR
				new[] {TokenType.Or},
				// Conditional AND
				new[] {TokenType.AndAlso},
				// Conditional OR
				new[] {TokenType.OrElse},
				// Null-coalescing
				new[] {TokenType.Coalesce},
				// Conditional
				new[] {TokenType.Cond},
				// Other
				new[] {TokenType.Colon}
			});
			for (var i = 0; i < tokenPrecedenceList.Length; i++)
			{
				var tokenList = tokenPrecedenceList[i];
				foreach (var token in tokenList)
					TokenPrecedence.Add((int)token, i);
			}
		}
		public Parser(IEnumerable<Token> tokens)
		{
			if (tokens == null) throw new ArgumentNullException("tokens");

			this.tokens = new List<Token>(tokens as List<Token> ?? new List<Token>(tokens));
			this.stack = new Stack<ParserNode>();
		}
		public static ParserNode Parse(IEnumerable<Token> tokens)
		{
			if (tokens == null) throw new ArgumentNullException("tokens");

			var parser = new Parser(tokens);
			parser.Expression();
			if (parser.stack.Count == 0)
				throw new ExpressionParserException(Properties.Resources.EXCEPTION_PARSER_EXPRESSIONISEMPTY, default(ILineInfo));

			return parser.stack.Pop();
		}

		private bool Expression(ParserNode op = default(ParserNode), TokenType[] term = null)
		{
			var ct = 0;
			var changed = false;
			while (this.tokens.Count > 0)
			{
				var token = this.tokens.Dequeue();
				try
				{
					if (ct == 0 && UnaryReplacement.ContainsKey((int)token.Type))
						token = new Token((TokenType)UnaryReplacement[(int)token.Type], token.Value, token.LineNumber, token.ColumnNumber, token.TokenLength);

					if ((token.Type == TokenType.Lparen && ct > 0) || token.Type == TokenType.Lbracket || token.Type == TokenType.NullIndex)
					{
						var callToken = new Token(TokenType.Call, token.Value, token.LineNumber, token.ColumnNumber, token.TokenLength);
						token = new Token(TokenType.Arguments, token.Value, token.LineNumber, token.ColumnNumber, token.TokenLength);

						this.tokens.Insert(0, token);
						this.tokens.Insert(0, callToken);
						continue;
					}

					if (term != null && Array.IndexOf(term, token.Type) >= 0)
					{
						this.tokens.Insert(0, token);
						break;
					}

					var node = new ParserNode(token);
					switch (token.Type)
					{
						case TokenType.Identifier:
						case TokenType.Literal:
						case TokenType.Number:
							this.stack.Push(node);
							changed = true;
							break;
						case TokenType.Compl:
						case TokenType.Not:
						case TokenType.Plus:
						case TokenType.Convert:
						case TokenType.Minus:
						case TokenType.New:
							this.stack.Push(node);
							if (!this.Expression(node, term))
								throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_OPREQUIRESOPERAND, token.Type), token);
							this.CombineUnary(node.Lexeme);
							changed = true;
							break;
						case TokenType.Add:
						case TokenType.Subtract:
						case TokenType.Div:
						case TokenType.Mul:
						case TokenType.Pow:
						case TokenType.Mod:
						case TokenType.And:
						case TokenType.Or:
						case TokenType.Xor:
						case TokenType.Lshift:
						case TokenType.Rshift:
						case TokenType.AndAlso:
						case TokenType.OrElse:
						case TokenType.Gt:
						case TokenType.Gte:
						case TokenType.Lt:
						case TokenType.Lte:
						case TokenType.Eq:
						case TokenType.Neq:
						case TokenType.Resolve:
						case TokenType.NullResolve:
						case TokenType.Coalesce:
						case TokenType.Colon:
						case TokenType.Is:
						case TokenType.As:
						case TokenType.Call:
							if (op.Type != TokenType.None && this.ComputePrecedence(node.Type, op.Type) <= 0)
							{
								this.tokens.Insert(0, token);
								return changed;
							}
							this.stack.Push(node);
							if (!this.Expression(node, term))
								throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_OPREQUIRESSECONDOPERAND, token.Type), token);
							this.CombineBinary(node.Lexeme);

							if (token.Type == TokenType.Call &&
								node.Childs.Count == 2 &&
								node.Childs[0].Type == TokenType.Identifier &&
								(
									string.Equals(node.Childs[0].Value, "unchecked", StringComparison.Ordinal) ||
									string.Equals(node.Childs[0].Value, "checked", StringComparison.Ordinal) ||
									string.Equals(node.Childs[0].Value, "typeof", StringComparison.Ordinal) ||
									string.Equals(node.Childs[0].Value, "default", StringComparison.Ordinal)
									) &&
								node.Childs[1].Childs.Count == 1)
							{
								var arguments = node.Childs[1];
								var newType = string.Equals(node.Childs[0].Value, "unchecked", StringComparison.Ordinal) ? TokenType.UncheckedScope :
									string.Equals(node.Childs[0].Value, "checked", StringComparison.Ordinal) ? TokenType.CheckedScope :
										string.Equals(node.Childs[0].Value, "typeof", StringComparison.Ordinal) ? TokenType.Typeof :
											string.Equals(node.Childs[0].Value, "default", StringComparison.Ordinal) ? TokenType.Default :
												TokenType.Call;

								this.stack.Pop();
								this.stack.Push(new ParserNode(newType, node.Lexeme, node.Value, arguments.Childs));
							}

							changed = true;
							break;
						case TokenType.Cond:
							if (op.Type != TokenType.None && this.ComputePrecedence(node.Type, op.Type) <= 0)
							{
								this.tokens.Insert(0, token);
								return changed;
							}
							this.stack.Push(node);
							var colonIdx = this.FindCondClosingToken();
							if (colonIdx < 0)
								throw new ExpressionParserException(Properties.Resources.EXCEPTION_PARSER_COLONISEXPRECTED, token);
							this.Expression(term: CondTerm);
							this.CheckAndConsumeToken(token.Position, TokenType.Colon);
							this.Expression(term: DefaultTerm.Union(term ?? new TokenType[0]).ToArray());
							this.CombineTernary(node.Lexeme);
							changed = true;
							break;
						case TokenType.NullIndex:
						case TokenType.Lbracket:
						case TokenType.Lparen:
						case TokenType.Arguments:
							if (token.Type == TokenType.Lparen)
								node = new ParserNode(TokenType.Group, node.Lexeme, node.Value, node.Childs);
							this.stack.Push(node);
							while (this.Expression(node, DefaultTerm))
							{
								this.CombineUnary(node.Lexeme);
								if (this.tokens.Count == 0 || this.tokens[0].Type != TokenType.Comma)
									break;
								this.CheckAndConsumeToken(token.Position, TokenType.Comma);
							}
							this.CheckAndConsumeToken(token.Position, TokenType.Rparen, TokenType.Rbracket);
							if (token.Type == TokenType.Lparen && ct == 0 && node.Childs.Count == 1 && (node.Childs.First().Type == TokenType.Identifier || node.Childs.First().Type == TokenType.Resolve || node.Childs.First().Type == TokenType.NullResolve))
							{
								node = new ParserNode(TokenType.Convert, node.Lexeme, node.Value, node.Childs);
								if (this.Expression(node, term) && this.stack.Any(n => n.Childs == node.Childs))
								{
									this.CombineUnary(node.Lexeme);
									this.stack.Pop();
									this.stack.Push(node);
								}
							}
							changed = true;
							break;
						default:
							throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_UNEXPECTEDTOKEN, token), token);
					}
					ct++;
				}
				catch (ExpressionParserException)
				{
					throw;
				}
				catch (System.Threading.ThreadAbortException)
				{
					throw;
				}
				catch (Exception exception)
				{
					throw new ExpressionParserException(exception.Message, exception, token);
				}
			}
			return changed;
		}

		private int FindCondClosingToken()
		{
			var lastTokenIdx = -1;
			for (var i = 0; i < this.tokens.Count; i++)
			{
				if (this.tokens[i].Type == TokenType.Colon)
					lastTokenIdx = i + 1;
				else if (this.tokens[i].Type == TokenType.Comma)
					break;
			}
			return lastTokenIdx;
		}

		private void CheckAndConsumeToken(string position, params TokenType[] tokens)
		{
			var token = this.tokens.Count > 0 ? this.tokens.Dequeue() : default(Token);
			if (token.Type == TokenType.None || Array.IndexOf(tokens, token.Type) < 0)
			{
				var expectedTokens = string.Join(", ", Array.ConvertAll(tokens, t => t.ToString()));
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_UNEXPECTEDTOKENWHILEOTHEREXPECTED, expectedTokens), token);
			}
		}
		private void CombineUnary(Token op)
		{
			if (this.stack.Count < 2) throw new ExpressionParserException(Properties.Resources.EXCEPTION_PARSER_UNARYOPREQOPERAND, op);
			var operand = this.stack.Pop();
			var @operator = this.stack.Pop();

			@operator.Childs.Add(operand);
			this.stack.Push(@operator);
		}
		private void CombineBinary(Token op)
		{
			if (this.stack.Count < 3) throw new ExpressionParserException(Properties.Resources.EXCEPTION_PARSER_BINARYOPREQOPERAND, op);
			var rightOperand = this.stack.Pop();
			var @operator = this.stack.Pop();
			var leftOperand = this.stack.Pop();

			@operator.Childs.Add(leftOperand);
			@operator.Childs.Add(rightOperand);
			this.stack.Push(@operator);
		}
		private void CombineTernary(Token op)
		{
			if (this.stack.Count < 4) throw new ExpressionParserException(Properties.Resources.EXCEPTION_PARSER_TERNARYOPREQOPERAND, op);

			var rightOperand = this.stack.Pop();
			var leftOperand = this.stack.Pop();
			var @operator = this.stack.Pop();
			var baseOperand = this.stack.Pop();

			@operator.Childs.Add(baseOperand);
			@operator.Childs.Add(leftOperand);
			@operator.Childs.Add(rightOperand);

			this.stack.Push(@operator);
		}

		private int ComputePrecedence(TokenType tokenType1, TokenType tokenType2)
		{
			var prec1 = 0;
			if (TokenPrecedence.TryGetValue((int)tokenType1, out prec1) == false)
				prec1 = int.MaxValue;

			var prec2 = 0;
			if (TokenPrecedence.TryGetValue((int)tokenType2, out prec2) == false)
				prec2 = int.MaxValue;

			return prec2.CompareTo(prec1);
		}

	}
}
