using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal class ListInitBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (bindingContext == null) throw new ArgumentNullException("bindingContext");
			if (expectedType == null) throw new ArgumentNullException("expectedType");

			boundExpression = null;
			bindingError = null;

			var newNode = node.GetNewExpression(throwOnError: true);
			var newExpression = default(Expression);
			if (AnyBinder.TryBind(newNode, bindingContext, TypeDescription.ObjectType, out newExpression, out bindingError) == false ||
				newExpression is NewExpression == false)
			{
				if (bindingError == null)
					bindingError = new ExpressionParserException(Properties.Resources.EXCEPTION_BIND_FAILEDTOBINDNEWEXPRESSION, node);
				return false;
			}

			var initializers = default(ElementInit[]);
			if (TryGetListInitializers(node, bindingContext, out initializers, out bindingError) == false)
			{
				if (bindingError == null)
					bindingError = new ExpressionParserException(Properties.Resources.EXCEPTION_BIND_FAILEDTOBINDLISTINITIALIZERS, node);
				return false;
			}

			boundExpression = Expression.ListInit((NewExpression)newExpression, initializers);
			return true;
		}

		internal static bool TryGetListInitializers(SyntaxTreeNode listNode, BindingContext bindingContext, out ElementInit[] initializers, out Exception bindingError)
		{
			if (listNode == null) throw new ArgumentNullException("listNode");
			if (bindingContext == null) throw new ArgumentNullException("bindingContext");

			bindingError = null;
			var initializerNodes = listNode.GetInitializers(throwOnError: true);
			initializers = new ElementInit[initializerNodes.Count];
			for (var i = 0; i < initializers.Length; i++)
			{
				var index = Constants.GetIndexAsString(i);
				var initializerObj = default(object);
				if (initializerNodes.TryGetValue(index, out initializerObj) == false || initializerObj is SyntaxTreeNode == false)
				{
					return false; // failed to get initializer #i
				}
				var initializerNode = (SyntaxTreeNode)initializerObj;
				var addMethodName = initializerNode.GetMethodName(throwOnError: true);
				var addMethod = default(MemberDescription);
				if (bindingContext.TryResolveMember(addMethodName, out addMethod) == false || addMethod.IsMethod == false)
				{
					return false; // failed to resolve 'Add' method
				}

				var argumentNodes = initializerNode.GetArguments(throwOnError: true);
				var arguments = new Expression[argumentNodes.Count];
				for (var p = 0; p < arguments.Length; p++)
				{
					var parameter = addMethod.GetParameter(p);
					var parameterType = TypeDescription.GetTypeDescription(parameter.ParameterType);
					var argumentNode = default(SyntaxTreeNode);
					if (argumentNodes.TryGetValue(p, out argumentNode) == false && argumentNodes.TryGetValue(parameter.Name, out argumentNode) == false)
					{
						return false; // failed to find argument #p
					}

					if (AnyBinder.TryBindInNewScope(argumentNode, bindingContext, parameterType, out arguments[p], out bindingError) == false)
					{
						return false; // failed to bind argument #p
					}
				}
				initializers[i] = Expression.ElementInit(addMethod, arguments);
			}
			return true;
		}
	}
}
