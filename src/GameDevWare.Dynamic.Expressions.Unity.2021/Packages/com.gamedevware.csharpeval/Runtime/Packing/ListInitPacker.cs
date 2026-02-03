using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Packing
{
	internal static class ListInitPacker
	{
		public static Dictionary<string, object> Pack(ListInitExpression expression)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			return new Dictionary<string, object>(3) {
				{ Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_LIST_INIT },
				{ Constants.NEW_ATTRIBUTE, AnyPacker.Pack(expression.NewExpression) },
				{ Constants.INITIALIZERS_ATTRIBUTE, MemberInitPacker.Pack(expression.Initializers) }
			};
		}
	}
}
