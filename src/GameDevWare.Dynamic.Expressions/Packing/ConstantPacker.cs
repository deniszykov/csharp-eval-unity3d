using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Packing
{
	internal static class ConstantPacker
	{
		public static Dictionary<string, object> Pack(ConstantExpression expression)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			return new Dictionary<string, object>(3) {
				{Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_CONSTANT},
				{Constants.TYPE_ATTRIBUTE, AnyPacker.Pack(expression.Type)},
				{Constants.VALUE_ATTRIBUTE, expression.Value},
			};
		}
	}
}
