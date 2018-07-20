using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Packing
{
	internal static class ArrayLengthPacker
	{
		public static Dictionary<string, object> Pack(UnaryExpression expression)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			return new Dictionary<string, object>(2) {
				{Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_ARRAY_LENGTH},
				{Constants.EXPRESSION_ATTRIBUTE, AnyPacker.Pack(expression.Operand)},
			};
		}
	}
}