using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Packing
{
	internal static class UnaryPacker
	{
		public static Dictionary<string, object> Pack(UnaryExpression expression)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			var expressionType = string.Empty;

			// ReSharper disable once SwitchStatementMissingSomeCases
			switch (expression.NodeType)
			{
				case ExpressionType.Not:
					expressionType = Constants.EXPRESSION_TYPE_NOT;
					break;
				case ExpressionType.NegateChecked:
					expressionType = Constants.EXPRESSION_TYPE_NEGATE_CHECKED;
					break;
				case ExpressionType.Negate:
					expressionType = Constants.EXPRESSION_TYPE_NEGATE;
					break;
				case ExpressionType.UnaryPlus:
					expressionType = Constants.EXPRESSION_TYPE_UNARY_PLUS;
					break;
				default: throw new InvalidOperationException("Invalid expression type for this packer.");
			}

			var node = new Dictionary<string, object>(3) {
				{ Constants.EXPRESSION_TYPE_ATTRIBUTE, expressionType },
				{ Constants.EXPRESSION_ATTRIBUTE, AnyPacker.Pack(expression.Operand) }
			};
			if (expression.Method != null) node.Add(Constants.METHOD_ATTRIBUTE, AnyPacker.Pack(expression.Method));

			return node;
		}
	}
}
