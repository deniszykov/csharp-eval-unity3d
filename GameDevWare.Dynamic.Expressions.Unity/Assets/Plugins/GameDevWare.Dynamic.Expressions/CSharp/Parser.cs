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
	/// <summary>
	/// Expression parser. Converts stream of <see cref="Token"/> to parser tree(<see cref="ParseTreeNode"/>).
	/// </summary>
	public class Parser
	{
		private static readonly Dictionary<int, int> UnaryReplacement;
		private static readonly Dictionary<int, int> TokenPrecedence;
		private static readonly TokenType[] CondTerm = new[] { TokenType.Colon };
		private static readonly TokenType[] NewTerm = new[] { TokenType.Call };
		private static readonly TokenType[] GenArgTerm = new[] { TokenType.Comma, TokenType.Gt, TokenType.Rshift };
		private static readonly TokenType[] DefaultTerm = new[] { TokenType.Comma, TokenType.Rparen, TokenType.Rbracket };
		private static readonly TokenType[] NullableTerm = new[] { TokenType.Comma, TokenType.Rparen, TokenType.Gt, TokenType.Rshift };

		private readonly List<Token> tokens;
		private readonly Stack<ParseTreeNode> stack;

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
				new[] {TokenType.Resolve, TokenType.NullResolve, TokenType.Call, TokenType.Typeof, TokenType.Default},
				// New
				new[] {TokenType.New },
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
				new[] {TokenType.Colon, TokenType.Lambda}
			});
			for (var i = 0; i < tokenPrecedenceList.Length; i++)
			{
				var tokenList = tokenPrecedenceList[i];
				foreach (var token in tokenList)
					TokenPrecedence.Add((int)token, i);
			}
		}
		private Parser(IEnumerable<Token> tokens)
		{
			if (tokens == null) throw new ArgumentNullException("tokens");

			this.tokens = new List<Token>(tokens as List<Token> ?? new List<Token>(tokens));
			this.stack = new Stack<ParseTreeNode>();
		}
		/// <summary>
		/// Converts stream of <see cref="Token"/> to parser tree(<see cref="ParseTreeNode"/>).
		/// </summary>
		/// <param name="tokens">Stream of <see cref="Token"/>.</param>
		/// <returns>A parser tree(<see cref="ParseTreeNode"/></returns>
		public static ParseTreeNode Parse(IEnumerable<Token> tokens)
		{
			if (tokens == null) throw new ArgumentNullException("tokens");

			var parser = new Parser(tokens);
			parser.Expression();
			if (parser.stack.Count == 0)
				throw new ExpressionParserException(Properties.Resources.EXCEPTION_PARSER_EXPRESSIONISEMPTY, default(ILineInfo));

			return parser.stack.Pop();
		}

		private bool Expression(TokenType @operator = default(TokenType), TokenType[] terminator = null)
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

					if (terminator != null && Array.IndexOf(terminator, token.Type) >= 0)
					{
						this.tokens.Insert(0, token);
						break;
					}

					var node = new ParseTreeNode(token);
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
							this.stack.Push(node);
							if (!this.Expression(node.Type, terminator))
								throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_OPREQUIRESOPERAND, token.Type), token);
							this.CombineUnary(node.Lexeme);
							changed = true;
							break;
						case TokenType.New:
							this.stack.Push(node);
							if (!this.Expression(node.Type, UnionTerminators(terminator, NewTerm)))
								throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_OPREQUIRESOPERAND, token.Type), token);
							this.CombineUnary(node.Lexeme); // collect Type for 'NEW'
							this.CheckAndConsumeToken(TokenType.Call);
							var argumentsNode = new ParseTreeNode(this.tokens.Dequeue());
							this.stack.Push(argumentsNode); // push 'ARGUMENTS' into stack
							while (this.Expression(argumentsNode.Type, DefaultTerm))
							{
								this.CombineUnary(argumentsNode.Lexeme); // push argument
								if (this.tokens.Count == 0 || this.tokens[0].Type != TokenType.Comma)
									break;
								this.CheckAndConsumeToken(TokenType.Comma);
							}
							this.CheckAndConsumeToken(TokenType.Rparen, TokenType.Rbracket);
							this.CombineUnary(argumentsNode.Lexeme); // collect Arguments for 'NEW'
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
						case TokenType.Comma:
						case TokenType.Is:
						case TokenType.As:
						case TokenType.Call:
						case TokenType.Lambda:
							if (token.Type == TokenType.Lt && this.GenericArguments(token))
							{
								changed = true;
								continue;
							}

							if (@operator != TokenType.None && this.ComputePrecedence(node.Type, @operator) <= 0)
							{
								this.tokens.Insert(0, token);
								return changed;
							}

							this.stack.Push(node);
							if (!this.Expression(node.Type, terminator))
								throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_OPREQUIRESSECONDOPERAND, token.Type), token);
							this.CombineBinary(node.Lexeme);

							if (token.Type == TokenType.Call &&
								node.Count == 2 &&
								node[0].Type == TokenType.Identifier &&
								(
									string.Equals(node[0].Value, "unchecked", StringComparison.Ordinal) ||
									string.Equals(node[0].Value, "checked", StringComparison.Ordinal) ||
									string.Equals(node[0].Value, "typeof", StringComparison.Ordinal) ||
									string.Equals(node[0].Value, "default", StringComparison.Ordinal)
								) &&
								node[1].Count == 1)
							{
								var newType = string.Equals(node[0].Value, "unchecked", StringComparison.Ordinal) ? TokenType.UncheckedScope :
									string.Equals(node[0].Value, "checked", StringComparison.Ordinal) ? TokenType.CheckedScope :
										string.Equals(node[0].Value, "typeof", StringComparison.Ordinal) ? TokenType.Typeof :
											string.Equals(node[0].Value, "default", StringComparison.Ordinal) ? TokenType.Default :
												TokenType.Call;

								var newNode = node[1].ChangeType(newType);

								this.stack.Pop();
								this.stack.Push(newNode);
							}
							else if (token.Type == TokenType.Lambda)
							{
								var lambda = this.stack.Pop();
								var lambdaArguments = lambda[0];
								switch (lambdaArguments.Type)
								{
									case TokenType.Identifier:
										lambda.RemoveAt(0);
										var newArguments = new ParseTreeNode(TokenType.Arguments, lambdaArguments.Lexeme, "(");
										newArguments.Add(lambdaArguments);
										lambda.Insert(0, newArguments);
										break;
									case TokenType.Convert:
									case TokenType.Group:
										var convertedArguments = lambdaArguments.ChangeType(TokenType.Arguments);
										lambda.RemoveAt(0);
										lambda.Insert(0, convertedArguments);
										break;
									default:
										throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_UNEXPECTEDTOKEN, token), token);
								}
								this.stack.Push(lambda);
							}
							changed = true;
							break;
						case TokenType.Cond:
							if (@operator != TokenType.None && this.ComputePrecedence(node.Type, @operator) <= 0)
							{
								this.tokens.Insert(0, token);
								return changed;
							}

							if (NullableType(token))
							{
								changed = true;
								continue;
							}

							this.stack.Push(node);
							var colonIdx = this.FindCondClosingToken();
							if (colonIdx < 0)
								throw new ExpressionParserException(Properties.Resources.EXCEPTION_PARSER_COLONISEXPRECTED, token);
							this.Expression(terminator: CondTerm);
							this.CheckAndConsumeToken(TokenType.Colon);
							this.Expression(terminator: UnionTerminators(DefaultTerm, terminator).ToArray());
							this.CombineTernary(node.Lexeme);
							changed = true;
							break;
						case TokenType.NullIndex:
						case TokenType.Lbracket:
						case TokenType.Lparen:
						case TokenType.Arguments:
							if (token.Type == TokenType.Lparen)
								node = node.ChangeType(TokenType.Group);
							this.stack.Push(node);
							while (this.Expression(node.Type, DefaultTerm))
							{
								this.CombineUnary(node.Lexeme);
								if (this.tokens.Count == 0 || this.tokens[0].Type != TokenType.Comma)
									break;
								this.CheckAndConsumeToken(TokenType.Comma);
							}
							this.CheckAndConsumeToken(TokenType.Rparen, TokenType.Rbracket);
							if (token.Type == TokenType.Lparen && ct == 0 && node.Count == 1 && (node[0].Type == TokenType.Identifier || node[0].Type == TokenType.Resolve))
							{
								if (this.Expression(TokenType.Convert, terminator) && this.stack.Any(n => ReferenceEquals(n, node)))
								{
									this.CombineUnary(node.Lexeme);
									this.stack.Pop();
									this.stack.Push(node.ChangeType(TokenType.Convert));
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

		private bool GenericArguments(Token currentToken)
		{
			if (this.stack.Count == 0 || this.stack.Peek().Type != TokenType.Identifier)
				return false;

			var closingTokenIndex = this.FindGenericArgClosingToken();
			if (closingTokenIndex < 0)
				return false;

			var argumentsNode = new ParseTreeNode(TokenType.Arguments, currentToken, currentToken.Value);
			this.stack.Push(argumentsNode);

			var closingToken = default(Token);
			do
			{
				var argumentAdded = false;

				while (this.Expression(argumentsNode.Type, GenArgTerm))
				{
					argumentAdded = true;
					// put argument into 'Arguments' node
					this.CombineUnary(currentToken);

					if (this.tokens.Count == 0 || this.tokens[0].Type != TokenType.Comma)
						break;
					this.CheckAndConsumeToken(TokenType.Comma);
					argumentAdded = false;
				}

				closingToken = tokens.Dequeue();
				if (closingToken.Type == TokenType.Rshift) // split '>>' into 2 '>' tokens
					this.tokens.Insert(0, new Token(TokenType.Gt, ">", closingToken.LineNumber, closingToken.ColumnNumber + 1, closingToken.TokenLength - 1));
				else if (closingToken.Type != TokenType.Comma && closingToken.Type != TokenType.Gt)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_UNEXPECTEDTOKENWHILEOTHEREXPECTED, TokenType.Gt), closingToken);

				if (argumentAdded)
					continue;

				// add 'None' as argument if empty argument is specified
				this.stack.Push(new ParseTreeNode(TokenType.Identifier, closingToken, string.Empty));
				this.CombineUnary(currentToken);

			} while (closingToken.Type != TokenType.Gt && closingToken.Type != TokenType.Rshift);

			// put 'Arguments' into 'Identifier' node
			this.CombineUnary(currentToken);

			return true;
		}
		private bool NullableType(Token currentToken)
		{
			if (this.stack.Count == 0)
				return false;

			// '.' or 'Identifier' should be at top of stack
			// and ')' '>' ',' token ahead
			var stackTop = this.stack.Peek();
			if ((stackTop.Type != TokenType.Identifier && stackTop.Type != TokenType.Resolve) ||
				(this.tokens.Count != 0 && Array.IndexOf(NullableTerm, this.tokens[0].Type) < 0))
				return false;

			var identifier = this.stack.Pop();
			var argumentsNode = new ParseTreeNode(TokenType.Arguments, currentToken, "<");
			var nullableNode = new ParseTreeNode(TokenType.Identifier, currentToken, typeof(Nullable).Name);

			argumentsNode.Add(identifier);
			nullableNode.Add(argumentsNode);

			this.stack.Push(nullableNode);
			return true;
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
		private int FindGenericArgClosingToken()
		{
			const int NOT_FOUND = -1;

			var depth = 0;
			for (var i = 0; i < this.tokens.Count; i++)
			{
				switch (this.tokens[i].Type)
				{
					case TokenType.Identifier:
					case TokenType.Resolve:
					case TokenType.Comma:
					case TokenType.Cond:
						continue;
					case TokenType.Gt:
						depth--;
						if (depth < 0)
							return i;
						break;
					case TokenType.Rshift:
						depth -= 2;
						if (depth < 0)
							return i;
						break;
					case TokenType.Lt:
						depth++;
						break;
					default:
						return NOT_FOUND;
				}
			}
			return NOT_FOUND;
		}

		private void CheckAndConsumeToken(TokenType expectedType1, TokenType expectedType2 = 0)
		{
			var token = this.tokens.Count > 0 ? this.tokens.Dequeue() : default(Token);
			var actualType = token.Type;
			if (actualType == TokenType.None || (actualType != expectedType1 && actualType != expectedType2))
			{
				var expectedTokens = expectedType2 != TokenType.None ? string.Concat(expectedType1.ToString(), ", ", expectedType2.ToString()) : expectedType1.ToString();
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_UNEXPECTEDTOKENWHILEOTHEREXPECTED, expectedTokens), token);
			}
		}
		private void CombineUnary(Token op)
		{
			if (this.stack.Count < 2) throw new ExpressionParserException(Properties.Resources.EXCEPTION_PARSER_UNARYOPREQOPERAND, op);
			var operand = this.stack.Pop();
			var @operator = this.stack.Pop();

			@operator.Add(operand);
			this.stack.Push(@operator);
		}
		private void CombineBinary(Token op)
		{
			if (this.stack.Count < 3) throw new ExpressionParserException(Properties.Resources.EXCEPTION_PARSER_BINARYOPREQOPERAND, op);
			var rightOperand = this.stack.Pop();
			var @operator = this.stack.Pop();
			var leftOperand = this.stack.Pop();

			@operator.Add(leftOperand);
			@operator.Add(rightOperand);
			this.stack.Push(@operator);
		}
		private void CombineTernary(Token op)
		{
			if (this.stack.Count < 4) throw new ExpressionParserException(Properties.Resources.EXCEPTION_PARSER_TERNARYOPREQOPERAND, op);

			var rightOperand = this.stack.Pop();
			var leftOperand = this.stack.Pop();
			var @operator = this.stack.Pop();
			var baseOperand = this.stack.Pop();

			@operator.Add(baseOperand);
			@operator.Add(leftOperand);
			@operator.Add(rightOperand);

			this.stack.Push(@operator);
		}

		private static TokenType[] UnionTerminators(TokenType[] first, TokenType[] second)
		{
			if (first == null)
				return second;
			else if (second == null)
				return first;
			if (ReferenceEquals(first, second))
				return first;

			// check if 'first' is subset of 'second'
			var firstMatches = 0;
			foreach (var item in second)
				firstMatches += Array.IndexOf(first, item) >= 0 ? 1 : 0;
			if (firstMatches == second.Length)
				return first; // 'second' is subset of 'first'

			// check if 'second' is subset of 'first'
			var secondMatches = 0;
			foreach (var item in first)
				secondMatches += Array.IndexOf(second, item) >= 0 ? 1 : 0;
			if (secondMatches == first.Length)
				return second; // 'second' is subset of 'first'

			// just combine them
			var newTerms = new TokenType[first.Length + second.Length];
			first.CopyTo(newTerms, 0);
			second.CopyTo(newTerms, first.Length);
			return newTerms;
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
