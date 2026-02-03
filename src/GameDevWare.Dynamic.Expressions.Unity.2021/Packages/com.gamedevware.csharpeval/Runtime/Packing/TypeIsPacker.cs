using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Packing
{
	internal static class TypeIsPacker
	{
		public static Dictionary<string, object> Pack(TypeBinaryExpression expression)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			return new Dictionary<string, object>(3) {
				{ Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_TYPE_IS },
				{ Constants.EXPRESSION_ATTRIBUTE, AnyPacker.Pack(expression.Expression) },
				{ Constants.TYPE_ATTRIBUTE, AnyPacker.Pack(expression.TypeOperand) }
			};
		}
	}
}
