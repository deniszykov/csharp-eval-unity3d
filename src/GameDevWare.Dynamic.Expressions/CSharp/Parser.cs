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
		private static readonly TokenType[] ConditionTerm = new[] { TokenType.Colon };
		private static readonly TokenType[] NewTerm = new[] { TokenType.Call };
		private static readonly TokenType[] GeneticArgumentsTerm = new[] { TokenType.Comma, TokenType.GreaterThan, TokenType.RightShift };
		private static readonly TokenType[] CommonTerm = new[] { TokenType.Comma, TokenType.RightParentheses, TokenType.RightBracket };
		private static readonly TokenType[] NullableTerm = new[] { TokenType.Comma, TokenType.RightParentheses, TokenType.GreaterThan, TokenType.RightShift };

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
				new[] {TokenType.Plus, TokenType.Minus, TokenType.Not, TokenType.Complement, TokenType.Convert},
				// Multiplicative
				new[] {TokenType.Division, TokenType.Multiplication, TokenType.Power, TokenType.Modulo},
				// Additive
				new[] {TokenType.Add, TokenType.Subtract},
				// Shift
				new[] {TokenType.LeftShift, TokenType.RightShift},
				// Relational and type testing
				new[] {TokenType.GreaterThan, TokenType.GreaterThanOrEquals, TokenType.LesserThan, TokenType.LesserThanOrEquals, TokenType.Is, TokenType.As},
				// Equality
				new[] {TokenType.EqualsTo, TokenType.NotEqualsTo},
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
				new[] {TokenType.Conditional},
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

			if(parser.stack.Count > 1)
				throw new ExpressionParserException(Properties.Resources.EXCEPTION_PARSER_MISSING_OPERATOR, parser.stack.First());

			return parser.stack.Pop();
		}

		private bool Expression(TokenType @operator = default(TokenType), TokenType[] terminator = null)
		{
			var iterations = 0;
			var changed = false;
			while (this.tokens.Count > 0)
			{
				var token = this.tokens.Dequeue();
				try
				{
					if (iterations == 0 && UnaryReplacement.ContainsKey((int)token.Type))
						token = new Token((TokenType)UnaryReplacement[(int)token.Type], token.Value, token.LineNumber, token.ColumnNumber, token.TokenLength);

					if ((token.Type == TokenType.LeftParentheses && iterations > 0) || token.Type == TokenType.LeftBracket || token.Type == TokenType.NullIndex)
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
						case TokenType.Complement:
						case TokenType.Not:
						case TokenType.Plus:
						case TokenType.Convert:
						case TokenType.Minus:
							this.stack.Push(node);
							if (!this.Expression(node.Type, terminator))
								throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_OPREQUIRESOPERAND, token.Type), token);
							this.CombineUnary(node.Token);
							changed = true;
							break;
						case TokenType.New:
							this.stack.Push(node);
							if (!this.Expression(node.Type, UnionTerminators(terminator, NewTerm)))
								throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_OPREQUIRESOPERAND, token.Type), token);
							this.CombineUnary(node.Token); // collect Type for 'NEW'
							this.CheckAndConsumeToken(TokenType.Call);
							var argumentsNode = new ParseTreeNode(this.tokens.Dequeue());
							this.stack.Push(argumentsNode); // push 'ARGUMENTS' into stack
							while (this.Expression(argumentsNode.Type, CommonTerm))
							{
								this.CombineUnary(argumentsNode.Token); // push argument
								if (this.tokens.Count == 0 || this.tokens[0].Type != TokenType.Comma)
									break;
								this.CheckAndConsumeToken(TokenType.Comma);
							}
							this.CheckAndConsumeToken(TokenType.RightParentheses, TokenType.RightBracket);
							this.CombineUnary(argumentsNode.Token); // collect Arguments for 'NEW'
							changed = true;
							break;
						case TokenType.Add:
						case TokenType.Subtract:
						case TokenType.Division:
						case TokenType.Multiplication:
						case TokenType.Power:
						case TokenType.Modulo:
						case TokenType.And:
						case TokenType.Or:
						case TokenType.Xor:
						case TokenType.LeftShift:
						case TokenType.RightShift:
						case TokenType.AndAlso:
						case TokenType.OrElse:
						case TokenType.GreaterThan:
						case TokenType.GreaterThanOrEquals:
						case TokenType.LesserThan:
						case TokenType.LesserThanOrEquals:
						case TokenType.EqualsTo:
						case TokenType.NotEqualsTo:
						case TokenType.Resolve:
						case TokenType.NullResolve:
						case TokenType.Coalesce:
						case TokenType.Colon:
						case TokenType.Comma:
						case TokenType.Is:
						case TokenType.As:
						case TokenType.Call:
						case TokenType.Lambda:
							if (token.Type == TokenType.LesserThan && this.TryMakeGenericArguments(token))
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
							this.CombineBinary(node.Token);

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

								var newNode = node[1].WithOtherType(newType);

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
										var newArguments = new ParseTreeNode(lambdaArguments.Token, TokenType.Arguments, "(");
										newArguments.Add(lambdaArguments);
										lambda.Replace(0, newArguments);
										break;
									case TokenType.Convert:
									case TokenType.Group:
										var convertedArguments = lambdaArguments.WithOtherType(TokenType.Arguments);
										lambda.Replace(0, convertedArguments);
										break;
									default:
										throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_UNEXPECTEDTOKEN, token), token);
								}
								this.stack.Push(lambda);
							}
							changed = true;
							break;
						case TokenType.Conditional:
							if (@operator != TokenType.None && this.ComputePrecedence(node.Type, @operator) <= 0)
							{
								this.tokens.Insert(0, token);
								return changed;
							}

							if (this.TryMakeNullableType(token))
							{
								changed = true;
								continue;
							}

							this.stack.Push(node);
							var colonIdx = this.FindConditionClosingToken();
							if (colonIdx < 0)
								throw new ExpressionParserException(Properties.Resources.EXCEPTION_PARSER_COLONISEXPRECTED, token);
							this.Expression(terminator: ConditionTerm);
							this.CheckAndConsumeToken(TokenType.Colon);
							this.Expression(terminator: UnionTerminators(CommonTerm, terminator).ToArray());
							this.CombineTernary(node.Token);
							changed = true;
							break;
						case TokenType.NullIndex:
						case TokenType.LeftBracket:
						case TokenType.LeftParentheses:
						case TokenType.Arguments:
							if (token.Type == TokenType.LeftParentheses)
								node = node.WithOtherType(TokenType.Group);
							this.stack.Push(node);
							while (this.Expression(node.Type, CommonTerm))
							{
								this.CombineUnary(node.Token);
								if (this.tokens.Count == 0 || this.tokens[0].Type != TokenType.Comma)
									break;
								this.CheckAndConsumeToken(TokenType.Comma);
							}
							this.CheckAndConsumeToken(TokenType.RightParentheses, TokenType.RightBracket);
							if (token.Type == TokenType.LeftParentheses && iterations == 0 && node.Count == 1 && (node[0].Type == TokenType.Identifier || node[0].Type == TokenType.Resolve))
							{
								if (this.Expression(TokenType.Convert, terminator) && this.stack.Any(n => ReferenceEquals(n, node)))
								{
									this.CombineUnary(node.Token);
									this.stack.Pop();
									this.stack.Push(node.WithOtherType(TokenType.Convert));
								}
							}
							changed = true;
							break;
						default:
							throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_UNEXPECTEDTOKEN, token), token);
					}
					iterations++;
				}
				catch (ExpressionParserException)
				{
					throw;
				}
#if !NETSTANDARD
				catch (System.Threading.ThreadAbortException)
				{
					throw;
				}
#endif
				catch (Exception exception)
				{
					throw new ExpressionParserException(exception.Message, exception, token);
				}
			}
			return changed;
		}

		private bool TryMakeGenericArguments(Token currentToken)
		{
			if (this.stack.Count == 0 || this.stack.Peek().Type != TokenType.Identifier)
				return false;

			var closingTokenIndex = this.FindGenericArgClosingToken();
			if (closingTokenIndex < 0)
				return false;

			var argumentsNode = new ParseTreeNode(currentToken, TokenType.Arguments, currentToken.Value);
			this.stack.Push(argumentsNode);

			var closingToken = default(Token);
			do
			{
				var argumentAdded = false;

				while (this.Expression(argumentsNode.Type, GeneticArgumentsTerm))
				{
					argumentAdded = true;
					// put argument into 'Arguments' node
					this.CombineUnary(currentToken);

					if (this.tokens.Count == 0 || this.tokens[0].Type != TokenType.Comma)
						break;
					this.CheckAndConsumeToken(TokenType.Comma);
					argumentAdded = false;
				}

				closingToken = this.tokens.Dequeue();
				if (closingToken.Type == TokenType.RightShift) // split '>>' into 2 '>' tokens
					this.tokens.Insert(0, new Token(TokenType.GreaterThan, ">", closingToken.LineNumber, closingToken.ColumnNumber + 1, closingToken.TokenLength - 1));
				else if (closingToken.Type != TokenType.Comma && closingToken.Type != TokenType.GreaterThan)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_UNEXPECTEDTOKENWHILEOTHEREXPECTED, TokenType.GreaterThan), closingToken);

				if (argumentAdded)
					continue;

				// add 'None' as argument if empty argument is specified
				this.stack.Push(new ParseTreeNode(closingToken, TokenType.Identifier, string.Empty));
				this.CombineUnary(currentToken);

			} while (closingToken.Type != TokenType.GreaterThan && closingToken.Type != TokenType.RightShift);

			// put 'Arguments' into 'Identifier' node
			this.CombineUnary(currentToken);

			return true;
		}
		private bool TryMakeNullableType(Token currentToken)
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
			var argumentsNode = new ParseTreeNode(currentToken, TokenType.Arguments, "<");
			var nullableNode = new ParseTreeNode(currentToken, TokenType.Identifier, typeof(Nullable).Name);

			argumentsNode.Add(identifier);
			nullableNode.Add(argumentsNode);

			this.stack.Push(nullableNode);
			return true;
		}

		private int FindConditionClosingToken()
		{
			var lastTokenIdx = -1;
			var depth = 0;
			for (var i = 0; i < this.tokens.Count; i++)
			{
				switch (this.tokens[i].Type)
				{
					case TokenType.Colon:
						if (depth == 0)
							lastTokenIdx = i + 1;
						continue;
					case TokenType.LeftParentheses:
					case TokenType.LeftBracket:
					case TokenType.NullIndex:
						depth++;
						continue;
					case TokenType.RightParentheses:
					case TokenType.RightBracket:
					depth--;
						continue;
					case TokenType.Comma:
						if (depth == 0)
							break;
						else
							continue;
					default:
						continue;
				}
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
					case TokenType.Conditional:
						continue;
					case TokenType.GreaterThan:
						depth--;
						if (depth < 0)
							return i;
						break;
					case TokenType.RightShift:
						depth -= 2;
						if (depth < 0)
							return i;
						break;
					case TokenType.LesserThan:
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
		private void CombineUnary(Token operation)
		{
			if (this.stack.Count < 2) throw new ExpressionParserException(Properties.Resources.EXCEPTION_PARSER_UNARYOPREQOPERAND, operation);
			var operand = this.stack.Pop();
			var @operator = this.stack.Pop();

			@operator.Add(operand);
			this.stack.Push(@operator);
		}
		private void CombineBinary(Token operation)
		{
			if (this.stack.Count < 3) throw new ExpressionParserException(Properties.Resources.EXCEPTION_PARSER_BINARYOPREQOPERAND, operation);
			var rightOperand = this.stack.Pop();
			var @operator = this.stack.Pop();
			var leftOperand = this.stack.Pop();

			@operator.Add(leftOperand);
			@operator.Add(rightOperand);
			this.stack.Push(@operator);
		}
		private void CombineTernary(Token operation)
		{
			if (this.stack.Count < 4) throw new ExpressionParserException(Properties.Resources.EXCEPTION_PARSER_TERNARYOPREQOPERAND, operation);

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
