using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class ConditionBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			bindingError = null;
			boundExpression = null;
			try
			{
				var test = node.GetTestExpression(throwOnError: true);
				var ifTrue = node.GetIfTrueExpression(throwOnError: true);
				var ifFalse = node.GetIfFalseExpression(throwOnError: true);
				var testExpression = default(Expression);
				var ifTrueBranch = default(Expression);
				var ifFalseBranch = default(Expression);

				if (AnyBinder.TryBind(test, bindingContext, Metadata.GetTypeDescription(typeof(bool)), out testExpression, out bindingError) == false)
					return false;
				if (AnyBinder.TryBind(ifTrue, bindingContext, null, out ifTrueBranch, out bindingError) == false)
					return false;
				if (AnyBinder.TryBind(ifFalse, bindingContext, null, out ifFalseBranch, out bindingError) == false)
					return false;

				boundExpression = Expression.Condition(testExpression, ifTrueBranch, ifFalseBranch);
				return true;
			}
			catch (Exception error)
			{
				bindingError = error;
				return false;
			}
		}
	}
}
