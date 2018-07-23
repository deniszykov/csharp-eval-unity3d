using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Packing
{
	internal static class BinaryPacker
	{
		public static Dictionary<string, object> Pack(BinaryExpression expression)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			var expressionType = string.Empty;
			// ReSharper disable once SwitchStatementMissingSomeCases
			switch (expression.NodeType)
			{
				case ExpressionType.RightShift: expressionType = Constants.EXPRESSION_TYPE_RIGHT_SHIFT; break;
				case ExpressionType.Subtract: expressionType = Constants.EXPRESSION_TYPE_SUBTRACT; break;
				case ExpressionType.SubtractChecked: expressionType = Constants.EXPRESSION_TYPE_SUBTRACT_CHECKED; break;
				case ExpressionType.Power: expressionType = Constants.EXPRESSION_TYPE_POWER; break;
				case ExpressionType.NotEqual: expressionType = Constants.EXPRESSION_TYPE_NOT_EQUAL; break;
				case ExpressionType.Or: expressionType = Constants.EXPRESSION_TYPE_OR; break;
				case ExpressionType.OrElse: expressionType = Constants.EXPRESSION_TYPE_OR_ELSE; break;
				case ExpressionType.MultiplyChecked: expressionType = Constants.EXPRESSION_TYPE_MULTIPLY_CHECKED; break;
				case ExpressionType.Multiply: expressionType = Constants.EXPRESSION_TYPE_MULTIPLY; break;
				case ExpressionType.Modulo: expressionType = Constants.EXPRESSION_TYPE_MODULO; break;
				case ExpressionType.LessThan: expressionType = Constants.EXPRESSION_TYPE_LESS_THAN; break;
				case ExpressionType.LessThanOrEqual: expressionType = Constants.EXPRESSION_TYPE_LESS_THAN_OR_EQUAL; break;
				case ExpressionType.LeftShift: expressionType = Constants.EXPRESSION_TYPE_LEFT_SHIFT; break;
				case ExpressionType.GreaterThanOrEqual: expressionType = Constants.EXPRESSION_TYPE_GREATER_THAN_OR_EQUAL; break;
				case ExpressionType.GreaterThan: expressionType = Constants.EXPRESSION_TYPE_GREATER_THAN; break;
				case ExpressionType.ExclusiveOr: expressionType = Constants.EXPRESSION_TYPE_EXCLUSIVE_OR; break;
				case ExpressionType.Equal: expressionType = Constants.EXPRESSION_TYPE_EQUAL; break;
				case ExpressionType.Divide: expressionType = Constants.EXPRESSION_TYPE_DIVIDE; break;
				case ExpressionType.Coalesce: expressionType = Constants.EXPRESSION_TYPE_COALESCE; break;
				case ExpressionType.AndAlso: expressionType = Constants.EXPRESSION_TYPE_AND_ALSO; break;
				case ExpressionType.And: expressionType = Constants.EXPRESSION_TYPE_AND; break;
				case ExpressionType.AddChecked: expressionType = Constants.EXPRESSION_TYPE_ADD_CHECKED; break;
				case ExpressionType.Add: expressionType = Constants.EXPRESSION_TYPE_ADD; break;
				default: throw new InvalidOperationException("Invalid expression type for this packer.");
			}

			var node = new Dictionary<string, object>(4) {
				{Constants.EXPRESSION_TYPE_ATTRIBUTE, expressionType},
				{Constants.LEFT_ATTRIBUTE, AnyPacker.Pack(expression.Left)},
				{Constants.RIGHT_ATTRIBUTE, AnyPacker.Pack(expression.Right)}
			};
			if (expression.Method != null)
			{
				node.Add(Constants.METHOD_ATTRIBUTE, AnyPacker.Pack(expression.Method));
			}
			if (expression.Conversion != null)
			{
				node.Add(Constants.CONVERSION_ATTRIBUTE, AnyPacker.Pack(expression.Conversion));
			}
			return node;
		}
	}
}
