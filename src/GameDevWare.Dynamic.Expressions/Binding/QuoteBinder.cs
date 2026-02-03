using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class QuoteBinder
	{
		public static bool TryBind
			(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			bindingError = null;

			boundExpression = null;
			bindingError = null;

			var operandNode = node.GetExpression(true);

			if (!AnyBinder.TryBindInNewScope(operandNode, bindingContext, TypeDescription.ObjectType, out var operand, out bindingError))
				return false;

			boundExpression = Expression.Quote(operand);
			return true;
		}
	}
}
