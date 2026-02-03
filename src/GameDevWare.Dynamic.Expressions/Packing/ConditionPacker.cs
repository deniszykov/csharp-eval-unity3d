using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Packing
{
	internal static class ConditionPacker
	{
		public static Dictionary<string, object> Pack(ConditionalExpression expression)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			return new Dictionary<string, object>(4) {
				{ Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_CONDITION },
				{ Constants.TEST_ATTRIBUTE, AnyPacker.Pack(expression.Test) },
				{ Constants.IF_TRUE_ATTRIBUTE, AnyPacker.Pack(expression.IfTrue) },
				{ Constants.IF_FALSE_ATTRIBUTE, AnyPacker.Pack(expression.IfFalse) }
			};
		}
	}
}
