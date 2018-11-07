using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameDevWare.Dynamic.Expressions.CSharp
{
	/// <summary>
	/// Helper class allowing conversion from <see cref="ParseTreeNode"/> to <see cref="SyntaxTreeNode"/>.
	/// </summary>
	public static class SyntaxTreeBuilder
	{
		[Flags]
		internal enum TypeNameOptions
		{
			None = 0,
			Aliases = 0x1 << 0,
			ShortNames = 0x1 << 1,
			Arrays = 0x1 << 2,

			All = Aliases | ShortNames | Arrays
		}

		private static readonly Dictionary<int, string> ExpressionTypeByToken;
		private static readonly Dictionary<string, string> TypeAliases;

		static SyntaxTreeBuilder()
		{
			ExpressionTypeByToken = new Dictionary<int, string>
			{
				{ (int)TokenType.Resolve, Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD },
				{ (int)TokenType.NullResolve, Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD },
				{ (int)TokenType.Identifier, Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD },
				{ (int)TokenType.Literal, Constants.EXPRESSION_TYPE_CONSTANT },
				{ (int)TokenType.Number, Constants.EXPRESSION_TYPE_CONSTANT },
				{ (int)TokenType.Convert, Constants.EXPRESSION_TYPE_CONVERT },
				{ (int)TokenType.Group, Constants.EXPRESSION_TYPE_GROUP },
				{ (int)TokenType.UncheckedScope, Constants.EXPRESSION_TYPE_UNCHECKED_SCOPE },
				{ (int)TokenType.CheckedScope, Constants.EXPRESSION_TYPE_CHECKED_SCOPE },
				{ (int)TokenType.Plus, Constants.EXPRESSION_TYPE_UNARY_PLUS },
				{ (int)TokenType.Minus, Constants.EXPRESSION_TYPE_NEGATE },
				{ (int)TokenType.Not, Constants.EXPRESSION_TYPE_NOT },
				{ (int)TokenType.Complement, Constants.EXPRESSION_TYPE_COMPLEMENT },
				{ (int)TokenType.Division, Constants.EXPRESSION_TYPE_DIVIDE },
				{ (int)TokenType.Multiplication, Constants.EXPRESSION_TYPE_MULTIPLY },
				{ (int)TokenType.Power, Constants.EXPRESSION_TYPE_POWER },
				{ (int)TokenType.Modulo, Constants.EXPRESSION_TYPE_MODULO },
				{ (int)TokenType.Add, Constants.EXPRESSION_TYPE_ADD },
				{ (int)TokenType.Subtract, Constants.EXPRESSION_TYPE_SUBTRACT },
				{ (int)TokenType.LeftShift, Constants.EXPRESSION_TYPE_LEFT_SHIFT },
				{ (int)TokenType.RightShift, Constants.EXPRESSION_TYPE_RIGHT_SHIFT},
				{ (int)TokenType.GreaterThan, Constants.EXPRESSION_TYPE_GREATER_THAN },
				{ (int)TokenType.GreaterThanOrEquals, Constants.EXPRESSION_TYPE_GREATER_THAN_OR_EQUAL },
				{ (int)TokenType.LesserThan, Constants.EXPRESSION_TYPE_LESS_THAN },
				{ (int)TokenType.LesserThanOrEquals, Constants.EXPRESSION_TYPE_LESS_THAN_OR_EQUAL },
				{ (int)TokenType.Is, Constants.EXPRESSION_TYPE_TYPE_IS  },
				{ (int)TokenType.As, Constants.EXPRESSION_TYPE_TYPE_AS },
				{ (int)TokenType.EqualsTo, Constants.EXPRESSION_TYPE_EQUAL },
				{ (int)TokenType.NotEqualsTo, Constants.EXPRESSION_TYPE_NOT_EQUAL },
				{ (int)TokenType.And, Constants.EXPRESSION_TYPE_AND },
				{ (int)TokenType.Or, Constants.EXPRESSION_TYPE_OR },
				{ (int)TokenType.Xor, Constants.EXPRESSION_TYPE_EXCLUSIVE_OR },
				{ (int)TokenType.AndAlso, Constants.EXPRESSION_TYPE_AND_ALSO },
				{ (int)TokenType.OrElse, Constants.EXPRESSION_TYPE_OR_ELSE },
				{ (int)TokenType.Coalesce, Constants.EXPRESSION_TYPE_COALESCE },
				{ (int)TokenType.Conditional, Constants.EXPRESSION_TYPE_CONDITION },
				{ (int)TokenType.Call, Constants.EXPRESSION_TYPE_INVOKE },
				{ (int)TokenType.Typeof, Constants.EXPRESSION_TYPE_TYPE_OF },
				{ (int)TokenType.Default, Constants.EXPRESSION_TYPE_DEFAULT },
				{ (int)TokenType.New, Constants.EXPRESSION_TYPE_NEW },
				{ (int)TokenType.LeftBracket, Constants.EXPRESSION_TYPE_INDEX },
				{ (int)TokenType.Lambda, Constants.EXPRESSION_TYPE_LAMBDA },
			};

			TypeAliases = new Dictionary<string, string>
			{
				// ReSharper disable StringLiteralTypo
				{ "void", typeof(void).FullName },
				{ "char", typeof(char).FullName },
				{ "bool", typeof(bool).FullName },
				{ "byte", typeof(byte).FullName },
				{ "sbyte", typeof(sbyte).FullName },
				{ "decimal", typeof(decimal).FullName },
				{ "double", typeof(double).FullName },
				{ "float", typeof(float).FullName },
				{ "int", typeof(int).FullName },
				{ "uint", typeof(uint).FullName },
				{ "long", typeof(long).FullName },
				{ "ulong", typeof(ulong).FullName },
				{ "object", typeof(object).FullName },
				{ "short", typeof(short).FullName },
				{ "ushort", typeof(ushort).FullName },
				{ "string", typeof(string).FullName }
				// ReSharper restore StringLiteralTypo
			};
		}

		/// <summary>
		/// Convert <see cref="ParseTreeNode"/> to <see cref="SyntaxTreeNode"/> which is bindable on <see cref="System.Linq.Expressions.Expression"/> tree.
		/// </summary>
		/// <param name="parseNode">Parse tree node.</param>
		/// <param name="checkedScope">Numeric operation scope. Checked mean - no number overflow is allowed. Unchecked mean - overflow is allowed.</param>
		/// <returns>Prepared <see cref="SyntaxTreeNode"/> representing <see cref="ParseTreeNode"/>.</returns>
		public static SyntaxTreeNode ToSyntaxTree(this ParseTreeNode parseNode, bool checkedScope = CSharpExpression.DEFAULT_CHECKED_SCOPE)
		{
			if (parseNode == null) throw new ArgumentNullException("parseNode");

			try
			{
				var expressionType = default(string);
				if (ExpressionTypeByToken.TryGetValue((int)parseNode.Type, out expressionType) == false)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_UNEXPECTEDTOKENTYPE, parseNode.Type), parseNode);

				var syntaxNode = new Dictionary<string, object>
				{
					{ Constants.EXPRESSION_POSITION, parseNode.Token.Position },
					{ Constants.EXPRESSION_TYPE_ATTRIBUTE, expressionType },
				};

				switch (parseNode.Type)
				{
					case TokenType.NullResolve:
					case TokenType.Resolve:
						ToResolveNode(parseNode, checkedScope, syntaxNode);
						break;
					case TokenType.Identifier:
						ToIdentifierNode(parseNode, syntaxNode);
						break;
					case TokenType.Literal:
						syntaxNode[Constants.TYPE_ATTRIBUTE] = string.IsNullOrEmpty(parseNode.Value) == false && parseNode.Value[0] == '\'' ? typeof(char).FullName : typeof(string).FullName;
						syntaxNode[Constants.VALUE_ATTRIBUTE] = UnescapeAndUnquote(parseNode.Value, parseNode.Token);
						break;
					case TokenType.Number:
						ToNumberNode(parseNode, syntaxNode);
						break;
					case TokenType.Convert:
						ToConvertNode(parseNode, checkedScope, syntaxNode);
						break;
					case TokenType.CheckedScope:
						CheckNode(parseNode, 1);
						// ReSharper disable once RedundantArgumentDefaultValue
						syntaxNode[Constants.EXPRESSION_ATTRIBUTE] = parseNode[0].ToSyntaxTree(checkedScope: true);
						break;
					case TokenType.UncheckedScope:
						CheckNode(parseNode, 1);
						syntaxNode[Constants.EXPRESSION_ATTRIBUTE] = parseNode[0].ToSyntaxTree(checkedScope: false);
						break;
					case TokenType.As:
					case TokenType.Is:
						CheckNode(parseNode, 2);
						syntaxNode[Constants.EXPRESSION_ATTRIBUTE] = parseNode[0].ToSyntaxTree(checkedScope);
						syntaxNode[Constants.TYPE_ATTRIBUTE] = ToTypeName(parseNode[1], TypeNameOptions.All);
						break;
					case TokenType.Default:
					case TokenType.Typeof:
						CheckNode(parseNode, 1);
						syntaxNode[Constants.TYPE_ATTRIBUTE] = ToTypeName(parseNode[0], TypeNameOptions.All);
						break;
					case TokenType.Group:
					case TokenType.Plus:
					case TokenType.Minus:
					case TokenType.Not:
					case TokenType.Complement:
						CheckNode(parseNode, 1);
						syntaxNode[Constants.EXPRESSION_ATTRIBUTE] = parseNode[0].ToSyntaxTree(checkedScope);
						if (checkedScope && parseNode.Type == TokenType.Minus)
							syntaxNode[Constants.EXPRESSION_TYPE_ATTRIBUTE] += Constants.EXPRESSION_TYPE_CHECKED_SUFFIX;
						break;
					case TokenType.Division:
					case TokenType.Multiplication:
					case TokenType.Power:
					case TokenType.Modulo:
					case TokenType.Add:
					case TokenType.Subtract:
					case TokenType.LeftShift:
					case TokenType.RightShift:
					case TokenType.GreaterThan:
					case TokenType.GreaterThanOrEquals:
					case TokenType.LesserThan:
					case TokenType.LesserThanOrEquals:
					case TokenType.EqualsTo:
					case TokenType.NotEqualsTo:
					case TokenType.And:
					case TokenType.Or:
					case TokenType.Xor:
					case TokenType.AndAlso:
					case TokenType.OrElse:
					case TokenType.Coalesce:
						ToBinaryNode(parseNode, checkedScope, syntaxNode);
						break;
					case TokenType.Conditional:
						ToConditionalNode(parseNode, checkedScope, syntaxNode);
						break;
					case TokenType.Lambda:
						ToLambdaNode(parseNode, checkedScope, syntaxNode);
						break;
					case TokenType.Call:
						ToCallNode(parseNode, checkedScope, syntaxNode);
						break;
					case TokenType.New:
						ToNewNode(parseNode, checkedScope, syntaxNode);
						break;
					case TokenType.None:
					case TokenType.Colon:
					case TokenType.Comma:
					case TokenType.LeftParentheses:
					case TokenType.RightParentheses:
					case TokenType.LeftBracket:
					case TokenType.NullIndex:
					case TokenType.RightBracket:
					case TokenType.Arguments:
					default:
						throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_UNEXPECTEDTOKENWHILEBUILDINGTREE, parseNode.Type), parseNode);
				}

				return new SyntaxTreeNode(syntaxNode);
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
				throw new ExpressionParserException(exception.Message, exception, parseNode);
			}
		}

		private static void ToNewNode(ParseTreeNode parseNode, bool checkedScope, Dictionary<string, object> syntaxNode)
		{
			CheckNode(parseNode, 2, TokenType.None, TokenType.Arguments);

			if (parseNode[1].Value == "[")
				syntaxNode[Constants.EXPRESSION_TYPE_ATTRIBUTE] = Constants.EXPRESSION_TYPE_NEW_ARRAY_BOUNDS;

			syntaxNode[Constants.TYPE_ATTRIBUTE] = ToTypeName(parseNode[0], TypeNameOptions.All);
			syntaxNode[Constants.ARGUMENTS_ATTRIBUTE] = PrepareArguments(parseNode, 1, checkedScope);
		}
		private static void ToCallNode(ParseTreeNode parseNode, bool checkedScope, Dictionary<string, object> syntaxNode)
		{
			CheckNode(parseNode, 1);

			var isNullPropagation = false;
			if (parseNode.Value == "[" || parseNode.Value == "?[")
			{
				syntaxNode[Constants.EXPRESSION_TYPE_ATTRIBUTE] = ExpressionTypeByToken[(int)TokenType.LeftBracket];
				isNullPropagation = parseNode.Value == "?[";
			}

			syntaxNode[Constants.EXPRESSION_ATTRIBUTE] = parseNode[0].ToSyntaxTree(checkedScope);
			syntaxNode[Constants.ARGUMENTS_ATTRIBUTE] = PrepareArguments(parseNode, 1, checkedScope);
			syntaxNode[Constants.USE_NULL_PROPAGATION_ATTRIBUTE] = isNullPropagation ? Constants.TrueObject : Constants.FalseObject;
		}
		private static void ToLambdaNode(ParseTreeNode parseNode, bool checkedScope, Dictionary<string, object> syntaxNode)
		{
			CheckNode(parseNode, 2, TokenType.Arguments);
			syntaxNode[Constants.ARGUMENTS_ATTRIBUTE] = PrepareArguments(parseNode, 0, checkedScope);
			syntaxNode[Constants.EXPRESSION_ATTRIBUTE] = parseNode[1].ToSyntaxTree(checkedScope);
		}
		private static void ToConditionalNode(ParseTreeNode parseNode, bool checkedScope, Dictionary<string, object> syntaxNode)
		{
			CheckNode(parseNode, 3);
			syntaxNode[Constants.TEST_ATTRIBUTE] = parseNode[0].ToSyntaxTree(checkedScope);
			syntaxNode[Constants.IF_TRUE_ATTRIBUTE] = parseNode[1].ToSyntaxTree(checkedScope);
			syntaxNode[Constants.IF_FALSE_ATTRIBUTE] = parseNode[2].ToSyntaxTree(checkedScope);
		}
		private static void ToBinaryNode(ParseTreeNode parseNode, bool checkedScope, Dictionary<string, object> syntaxNode)
		{
			CheckNode(parseNode, 2);
			syntaxNode[Constants.LEFT_ATTRIBUTE] = parseNode[0].ToSyntaxTree(checkedScope);
			syntaxNode[Constants.RIGHT_ATTRIBUTE] = parseNode[1].ToSyntaxTree(checkedScope);
			if (checkedScope && (parseNode.Type == TokenType.Add || parseNode.Type == TokenType.Multiplication || parseNode.Type == TokenType.Subtract))
				syntaxNode[Constants.EXPRESSION_TYPE_ATTRIBUTE] += Constants.EXPRESSION_TYPE_CHECKED_SUFFIX;
		}
		private static void ToConvertNode(ParseTreeNode parseNode, bool checkedScope, Dictionary<string, object> syntaxNode)
		{
			CheckNode(parseNode, 2);
			syntaxNode[Constants.TYPE_ATTRIBUTE] = ToTypeName(parseNode[0], TypeNameOptions.All);
			syntaxNode[Constants.EXPRESSION_ATTRIBUTE] = parseNode[1].ToSyntaxTree(checkedScope);
			if (checkedScope)
				syntaxNode[Constants.EXPRESSION_TYPE_ATTRIBUTE] += Constants.EXPRESSION_TYPE_CHECKED_SUFFIX;
		}
		private static void ToNumberNode(ParseTreeNode parseNode, Dictionary<string, object> syntaxNode)
		{
			var floatTrait = parseNode.Value.IndexOf('f') >= 0;
			var doubleTrait = parseNode.Value.IndexOf('.') >= 0 || parseNode.Value.IndexOf('d') >= 0;
			var longTrait = parseNode.Value.IndexOf('l') >= 0;
			var unsignedTrait = parseNode.Value.IndexOf('u') >= 0;
			var decimalTrait = parseNode.Value.IndexOf('m') >= 0;

			if (decimalTrait)
				syntaxNode[Constants.TYPE_ATTRIBUTE] = typeof(decimal).FullName;
			else if (floatTrait)
				syntaxNode[Constants.TYPE_ATTRIBUTE] = typeof(float).FullName;
			else if (doubleTrait)
				syntaxNode[Constants.TYPE_ATTRIBUTE] = typeof(double).FullName;
			else if (longTrait && !unsignedTrait)
				syntaxNode[Constants.TYPE_ATTRIBUTE] = typeof(long).FullName;
			else if (longTrait)
				syntaxNode[Constants.TYPE_ATTRIBUTE] = typeof(ulong).FullName;
			else if (unsignedTrait)
				syntaxNode[Constants.TYPE_ATTRIBUTE] = typeof(uint).FullName;
			else
				syntaxNode[Constants.TYPE_ATTRIBUTE] = typeof(int).FullName;

			syntaxNode[Constants.VALUE_ATTRIBUTE] = parseNode.Value.TrimEnd('f', 'F', 'd', 'd', 'l', 'L', 'u', 'U', 'm', 'M');
		}
		private static void ToIdentifierNode(ParseTreeNode parseNode, Dictionary<string, object> syntaxNode)
		{
			if (parseNode.Count == 0 &&
				(parseNode.Value == Constants.VALUE_TRUE_STRING || parseNode.Value == Constants.VALUE_FALSE_STRING || parseNode.Value == Constants.VALUE_NULL_STRING))
			{
				syntaxNode[Constants.EXPRESSION_TYPE_ATTRIBUTE] = ExpressionTypeByToken[(int)TokenType.Literal]; // constant
				syntaxNode[Constants.TYPE_ATTRIBUTE] = parseNode.Value == Constants.VALUE_NULL_STRING ? typeof(object).FullName : typeof(bool).FullName;
				syntaxNode[Constants.VALUE_ATTRIBUTE] = parseNode.Value == Constants.VALUE_TRUE_STRING ? Constants.TrueObject :
					parseNode.Value == Constants.VALUE_FALSE_STRING ? Constants.FalseObject : null;
			}

			syntaxNode[Constants.EXPRESSION_ATTRIBUTE] = null;
			syntaxNode[Constants.ARGUMENTS_ATTRIBUTE] = PrepareTypeArguments(parseNode, 0);
			syntaxNode[Constants.PROPERTY_OR_FIELD_NAME_ATTRIBUTE] = parseNode.Value;
		}
		private static void ToResolveNode(ParseTreeNode parseNode, bool checkedScope, Dictionary<string, object> syntaxNode)
		{
			CheckNode(parseNode, 2, TokenType.None, TokenType.Identifier);
			syntaxNode[Constants.EXPRESSION_ATTRIBUTE] = parseNode[0].ToSyntaxTree(checkedScope);
			syntaxNode[Constants.PROPERTY_OR_FIELD_NAME_ATTRIBUTE] = parseNode[1].Value;
			syntaxNode[Constants.ARGUMENTS_ATTRIBUTE] = PrepareTypeArguments(parseNode[1], 0);
			syntaxNode[Constants.USE_NULL_PROPAGATION_ATTRIBUTE] = parseNode.Type == TokenType.NullResolve ? Constants.TrueObject : Constants.FalseObject;
		}
		private static object ToTypeName(ParseTreeNode parseNode, TypeNameOptions options)
		{
			var allowShortName = (options & TypeNameOptions.ShortNames) != 0;
			var allowAliases = (options & TypeNameOptions.Aliases) != 0;
			var allowArrays = (options & TypeNameOptions.Arrays) != 0;
			if (parseNode.Type == TokenType.Identifier && parseNode.Count == 0 && allowShortName)
			{
				var typeName = default(string);
				if (allowAliases && TypeAliases.TryGetValue(parseNode.Value, out typeName))
					return typeName;
				else
					return parseNode.Value;
			}

			if (parseNode.Type == TokenType.Call && parseNode.Count == 2 && parseNode.Value == "[" && parseNode[1].Count == 0 && allowArrays)
			{
				var arrayNode = new ParseTreeNode(parseNode.Token, TokenType.Identifier, typeof(Array).Name);
				var argumentsNode = new ParseTreeNode(parseNode.Token, TokenType.Arguments, "<");
				argumentsNode.Add(parseNode[0]);
				arrayNode.Add(argumentsNode);
				return ToTypeName(arrayNode, TypeNameOptions.None);
			}

			var syntaxNode = new Dictionary<string, object>
			{
				{ Constants.EXPRESSION_POSITION, parseNode.Token.Position },
				{ Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD },
			};

			if (parseNode.Type == TokenType.Resolve)
			{
				CheckNode(parseNode, 2, TokenType.None, TokenType.Identifier);
				syntaxNode[Constants.EXPRESSION_ATTRIBUTE] = ToTypeName(parseNode[0], TypeNameOptions.None);
				syntaxNode[Constants.PROPERTY_OR_FIELD_NAME_ATTRIBUTE] = parseNode[1].Value;
				syntaxNode[Constants.ARGUMENTS_ATTRIBUTE] = PrepareTypeArguments(parseNode[1], 0);
				syntaxNode[Constants.USE_NULL_PROPAGATION_ATTRIBUTE] = Constants.FalseObject;
			}
			else if (parseNode.Type == TokenType.Identifier)
			{
				var typeName = parseNode.Value;
				if (allowAliases && TypeAliases.TryGetValue(parseNode.Value, out typeName) == false)
					typeName = parseNode.Value;

				syntaxNode[Constants.EXPRESSION_ATTRIBUTE] = null;
				syntaxNode[Constants.PROPERTY_OR_FIELD_NAME_ATTRIBUTE] = typeName;
				syntaxNode[Constants.USE_NULL_PROPAGATION_ATTRIBUTE] = Constants.FalseObject;
				syntaxNode[Constants.ARGUMENTS_ATTRIBUTE] = PrepareTypeArguments(parseNode, 0);
			}
			else
			{
				throw new ExpressionParserException(Properties.Resources.EXCEPTION_PARSER_TYPENAMEEXPECTED, parseNode);
			}

			return new SyntaxTreeNode(syntaxNode);
		}

		private static Dictionary<string, object> PrepareArguments(ParseTreeNode parseNode, int argumentChildIndex, bool checkedScope)
		{
			var args = default(Dictionary<string, object>);
			if (argumentChildIndex >= parseNode.Count || parseNode[argumentChildIndex].Count == 0)
				return null;

			var argIdx = 0;
			var argumentsNode = parseNode[argumentChildIndex];
			args = new Dictionary<string, object>(argumentsNode.Count);
			for (var i = 0; i < argumentsNode.Count; i++)
			{
				var argNode = argumentsNode[i];
				if (argNode.Type == TokenType.Colon)
				{
					CheckNode(argNode, 2, TokenType.Identifier);

					var argName = argNode[0].Value;
					args[argName] = argNode[1].ToSyntaxTree(checkedScope);
				}
				else
				{
					var argName = Constants.GetIndexAsString(argIdx++);
					args[argName] = argNode.ToSyntaxTree(checkedScope);
				}
			}
			return args;
		}
		private static Dictionary<string, object> PrepareTypeArguments(ParseTreeNode parseNode, int argumentChildIndex)
		{
			var args = default(Dictionary<string, object>);
			if (argumentChildIndex >= parseNode.Count || parseNode[argumentChildIndex].Count == 0)
				return null;

			var argIdx = 0;
			var argumentsNode = parseNode[argumentChildIndex];
			args = new Dictionary<string, object>(argumentsNode.Count);
			for (var i = 0; i < argumentsNode.Count; i++)
			{
				var argNode = argumentsNode[i];
				var argName = Constants.GetIndexAsString(argIdx++);
				args[argName] = ToTypeName(argNode, TypeNameOptions.Aliases | TypeNameOptions.Arrays);
			}
			return args;
		}
		private static void CheckNode(ParseTreeNode parseNode, int childCount, TokenType childType0 = 0, TokenType childType1 = 0, TokenType childType2 = 0)
		{
			// ReSharper disable HeapView.BoxingAllocation
			if (parseNode.Count < childCount)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_INVALIDCHILDCOUNTOFNODE, parseNode.Type, parseNode.Count, childCount), parseNode);

			for (int i = 0, ct = Math.Min(3, childCount); i < ct; i++)
			{
				var childNode = parseNode[i];
				var childNodeType = parseNode[i].Type;
				if (i == 0 && childType0 != TokenType.None && childType0 != childNodeType)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_INVALIDCHILDTYPESOFNODE, parseNode.Type, childNodeType, childType0), childNode);
				if (i == 1 && childType1 != TokenType.None && childType1 != childNodeType)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_INVALIDCHILDTYPESOFNODE, parseNode.Type, childNodeType, childType1), childNode);
				if (i == 2 && childType2 != TokenType.None && childType2 != childNodeType)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_INVALIDCHILDTYPESOFNODE, parseNode.Type, childNodeType, childType2), childNode);
			}
			// ReSharper restore HeapView.BoxingAllocation
		}
		private static object UnescapeAndUnquote(string value, Token token)
		{
			if (value == null) return null;
			try
			{
				return StringUtils.UnescapeAndUnquote(value);
			}
			catch (InvalidOperationException e)
			{
				throw new ExpressionParserException(e.Message, e, token);
			}
		}
		
	}
}
