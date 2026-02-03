using System;
using System.Linq;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class NewArrayInitBinder
	{
		public static bool TryBind
			(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (bindingContext == null) throw new ArgumentNullException("bindingContext");
			if (expectedType == null) throw new ArgumentNullException("expectedType");

			boundExpression = null;
			bindingError = null;

			var typeName = node.GetTypeName(throwOnError: true);
			if (!bindingContext.TryResolveType(typeName, out var type))
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVETYPE, typeName), node);
				return false;
			}

			var elementType = TypeDescription.GetTypeDescription(type);
			var initializers = node.EnumerateInitializers(throwOnError: true).ToList();
			var valueExpressions = new Expression[initializers.Count];
			var index = 0;
			foreach (var initializerNode in initializers)
			{
				if (initializerNode == null)
				{
					return false;
				}

				if (!AnyBinder.TryBindInNewScope(initializerNode, bindingContext, elementType, out valueExpressions[index], out bindingError))
				{
					return false;
				}

				index++;
			}

			boundExpression = Expression.NewArrayInit(type, valueExpressions);
			return true;
		}
	}
}
