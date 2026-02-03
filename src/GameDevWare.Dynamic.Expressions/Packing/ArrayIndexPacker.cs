using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Packing
{
	internal static class ArrayIndexPacker
	{
		public static Dictionary<string, object> Pack(Expression expression)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			var binaryExpression = expression as BinaryExpression;
			var methodCall = expression as MethodCallExpression;
			var arguments = default(Expression[]);
			var argumentNames = default(string[]);
			var operand = default(Expression);
			if (methodCall != null)
			{
				arguments = methodCall.Arguments.ToArray();
				operand = methodCall.Object;
				argumentNames = methodCall.Method.GetParameters().ConvertAll(p => p.Name);
			}
			else if (binaryExpression != null)
			{
				arguments = new[] { binaryExpression.Right };
				operand = binaryExpression.Left;
			}
			else
				throw new InvalidOperationException("Invalid expression type for this packer.");

			return new Dictionary<string, object>(4) {
				{ Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_INDEX },
				{ Constants.EXPRESSION_ATTRIBUTE, AnyPacker.Pack(operand) },
				{ Constants.ARGUMENTS_ATTRIBUTE, AnyPacker.Pack(arguments, argumentNames) }
			};
		}
	}
}
