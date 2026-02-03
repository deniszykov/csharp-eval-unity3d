using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Packing
{
	internal static class MemberAccessPacker
	{
		public static Dictionary<string, object> Pack(MemberExpression expression)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			if (expression.Expression != null)
			{
				var memberAccessExpression = new Dictionary<string, object>(3) {
					{ Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_MEMBER_RESOLVE },
					{ Constants.NAME_ATTRIBUTE, expression.Member.Name },
					{ Constants.EXPRESSION_ATTRIBUTE, AnyPacker.Pack(expression.Expression) }
				};
				return memberAccessExpression;
			}
			else
			{
				var memberAccessExpression = new Dictionary<string, object>(2) {
					{ Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_MEMBER_RESOLVE },
					{ Constants.MEMBER_ATTRIBUTE, AnyPacker.Pack(expression.Member) }
				};
				return memberAccessExpression;
			}
		}
	}
}
