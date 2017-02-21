using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class ConditionBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (bindingContext == null) throw new ArgumentNullException("bindingContext");
			if (expectedType == null) throw new ArgumentNullException("expectedType");

			bindingError = null;
			boundExpression = null;

			var test = node.GetTestExpression(throwOnError: true);
			var ifTrue = node.GetIfTrueExpression(throwOnError: true);
			var ifFalse = node.GetIfFalseExpression(throwOnError: true);
			var testExpression = default(Expression);
			var ifTrueBranch = default(Expression);
			var ifFalseBranch = default(Expression);

			if (AnyBinder.TryBindInNewScope(test, bindingContext, TypeDescription.GetTypeDescription(typeof(bool)), out testExpression, out bindingError) == false)
				return false;
			if (AnyBinder.TryBindInNewScope(ifTrue, bindingContext, TypeDescription.ObjectType, out ifTrueBranch, out bindingError) == false)
				return false;
			if (AnyBinder.TryBindInNewScope(ifFalse, bindingContext, TypeDescription.ObjectType, out ifFalseBranch, out bindingError) == false)
				return false;

			Debug.Assert(testExpression != null, "testExpression != null");
			Debug.Assert(ifTrueBranch != null, "ifTrueBranch != null");
			Debug.Assert(ifFalseBranch != null, "ifFalseBranch != null");

			boundExpression = Expression.Condition(testExpression, ifTrueBranch, ifFalseBranch);
			return true;
		}
	}
}
