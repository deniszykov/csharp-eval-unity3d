using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class NewArrayBoundsBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (bindingContext == null) throw new ArgumentNullException("bindingContext");
			if (expectedType == null) throw new ArgumentNullException("expectedType");

			boundExpression = null;
			bindingError = null;

			var typeName = node.GetTypeName(throwOnError: true);
			var type = default(Type);
			if (bindingContext.TryResolveType(typeName, out type) == false)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVETYPE, typeName), node);
				return false;
			}

			var indexTypeDescription = TypeDescription.Int32Type;
			var arguments = node.GetArguments(throwOnError: true);
			var argumentExpressions = new Expression[arguments.Count];
			for (var i = 0; i < arguments.Count; i++)
			{
				var argument = default(SyntaxTreeNode);
				if (arguments.TryGetValue(i, out argument) == false)
				{
					bindingError = new ExpressionParserException(Properties.Resources.EXCEPTION_BOUNDEXPR_ARGSDOESNTMATCHPARAMS, node);
					return false;
				}

				if (AnyBinder.TryBindInNewScope(argument, bindingContext, indexTypeDescription, out argumentExpressions[i], out bindingError) == false)
					return false;

				Debug.Assert(argumentExpressions[i] != null, "argumentExpressions[i] != null");
			}

			boundExpression = Expression.NewArrayBounds(type, argumentExpressions);
			return true;
		}
	}
}
