using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Packing
{
	internal static class LambdaPacker
	{
		public static Dictionary<string, object> Pack(LambdaExpression expression)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			var arguments = new Dictionary<string, object>(expression.Parameters.Count);
			for (var p = 0; p < expression.Parameters.Count; p++)
			{
				var key = Constants.GetIndexAsString(p);
				arguments[key] = AnyPacker.Pack(expression.Parameters[p]);
			}

			return new Dictionary<string, object>(4) {
				{ Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_LAMBDA },
				{ Constants.TYPE_ATTRIBUTE, AnyPacker.Pack(expression.Type) },
				{ Constants.EXPRESSION_ATTRIBUTE, AnyPacker.Pack(expression.Body) },
				{ Constants.ARGUMENTS_ATTRIBUTE, arguments }
			};
		}
	}
}
