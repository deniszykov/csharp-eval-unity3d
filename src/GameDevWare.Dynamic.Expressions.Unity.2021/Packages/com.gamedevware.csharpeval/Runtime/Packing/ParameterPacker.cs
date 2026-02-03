using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Packing
{
	internal static class ParameterPacker
	{
		public static Dictionary<string, object> Pack(ParameterExpression expression)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			return new Dictionary<string, object>(3) {
				{ Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_PARAMETER },
				{ Constants.TYPE_ATTRIBUTE, AnyPacker.Pack(expression.Type) },
				{ Constants.NAME_ATTRIBUTE, expression.Name }
			};
		}
	}
}
