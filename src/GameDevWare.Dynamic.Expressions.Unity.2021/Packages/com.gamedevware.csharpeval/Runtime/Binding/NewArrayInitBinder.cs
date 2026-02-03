using System;
using System.Linq;
using System.Linq.Expressions;
using GameDevWare.Dynamic.Expressions.Properties;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class NewArrayInitBinder
	{
		public static bool TryBind
			(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException(nameof(node));
			if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));
			if (expectedType == null) throw new ArgumentNullException(nameof(expectedType));

			boundExpression = null;
			bindingError = null;

			var typeName = node.GetTypeName(true);
			if (!bindingContext.TryResolveType(typeName, out var type))
			{
				bindingError = new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_UNABLETORESOLVETYPE, typeName), node);
				return false;
			}

			var elementType = TypeDescription.GetTypeDescription(type);
			var initializers = node.EnumerateInitializers(true).ToList();
			var valueExpressions = new Expression[initializers.Count];
			var index = 0;
			foreach (var initializerNode in initializers)
			{
				if (initializerNode == null) return false;

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
