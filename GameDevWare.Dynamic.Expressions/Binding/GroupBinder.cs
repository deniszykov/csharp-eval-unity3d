using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class GroupBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			boundExpression = null;
			bindingError = null;

			var expression = node.GetExpression(throwOnError: true);

			return AnyBinder.TryBind(expression, bindingContext, expectedType, out boundExpression, out bindingError);
		}
	}
}
