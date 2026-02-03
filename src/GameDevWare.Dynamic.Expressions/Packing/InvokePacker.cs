using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Packing
{
	internal static class InvokePacker
	{
		public static Dictionary<string, object> Pack(InvocationExpression expression)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			var arguments = expression.Arguments.ToArray();

			return new Dictionary<string, object>(3) {
				{ Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_INVOKE },
				{ Constants.EXPRESSION_ATTRIBUTE, AnyPacker.Pack(expression.Expression) },
				{ Constants.ARGUMENTS_ATTRIBUTE, AnyPacker.Pack(arguments, null) }
			};
		}
	}
}
