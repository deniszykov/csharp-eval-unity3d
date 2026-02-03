using System;
using System.Linq;
using System.Linq.Expressions;
using GameDevWare.Dynamic.Expressions.Properties;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal class ListInitBinder
	{
		public static bool TryBind
			(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException(nameof(node));
			if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));
			if (expectedType == null) throw new ArgumentNullException(nameof(expectedType));

			boundExpression = null;
			bindingError = null;

			var newNode = node.GetNewExpression(true);
			if (AnyBinder.TryBind(newNode, bindingContext, TypeDescription.ObjectType, out var newExpressionObj, out bindingError) &&
				newExpressionObj is NewExpression newExpression)
			{
				if (!TryGetListInitializers(newExpression.Type, node, bindingContext, out var initializers, out bindingError))
				{
					if (bindingError == null)
						bindingError = new ExpressionParserException(Resources.EXCEPTION_BIND_FAILEDTOBINDLISTINITIALIZERS, node);
					return false;
				}

				boundExpression = Expression.ListInit(newExpression, initializers);
				return true;
			}

			if (bindingError == null)
				bindingError = new ExpressionParserException(Resources.EXCEPTION_BIND_FAILEDTOBINDNEWEXPRESSION, node);
			return false;
		}

		internal static bool TryGetListInitializers
			(Type newExpressionType, SyntaxTreeNode listNode, BindingContext bindingContext, out ElementInit[] initializers, out Exception bindingError)
		{
			if (listNode == null) throw new ArgumentNullException(nameof(listNode));
			if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));

			bindingError = null;
			var initializerNodes = listNode.EnumerateInitializers(true).ToList();
			initializers = new ElementInit[initializerNodes.Count];
			var index = 0;
			foreach (var initializerNode in initializerNodes)
			{
				if (initializerNode == null) return false; // failed to get initializer #i

				if (!TryCreateElementInitNode(newExpressionType, initializerNode, bindingContext, ref bindingError, out var elemInit))
				{
					return false;
				}

				initializers[index] = elemInit;
				index++;
			}

			return true;
		}
		private static bool TryCreateElementInitNode
		(
			Type newExpressionType,
			SyntaxTreeNode initializerNode,
			BindingContext bindingContext,
			ref Exception bindingError,
			out ElementInit elemInit)
		{
			var initializers = initializerNode.EnumerateInitializers(true).ToList();
			var addMethod = default(MemberDescription);
			var addMethodNameObj = initializerNode.GetMethodName(false);
			if (addMethodNameObj != null)
			{
				bindingContext.TryResolveMember(addMethodNameObj, out addMethod);
				if (!addMethod.IsMethod) addMethodNameObj = null;
			}
			else
			{
				addMethodNameObj = "Add";
				var typeDescription = TypeDescription.GetTypeDescription(newExpressionType);
				addMethod = typeDescription.GetMembers("Add").FirstOrDefault(memberDesc =>
					!memberDesc.IsStatic && memberDesc.IsMethod && memberDesc.GetParametersCount() == initializers.Count);
			}

			if (addMethod == null)
			{
				bindingError = new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_UNABLETOBINDMEMBER, addMethodNameObj, newExpressionType),
					initializerNode);
				elemInit = null;
				return false;
			}

			var arguments = new Expression[initializers.Count];
			var index = 0;
			foreach (var initializerValueNode in initializers)
			{
				if (initializerValueNode == null)
				{
					// invalid syntax node
					elemInit = null;
					return false;
				}

				var parameter = addMethod.GetParameter(index);
				var parameterType = TypeDescription.GetTypeDescription(parameter.ParameterType);

				if (!AnyBinder.TryBindInNewScope(initializerValueNode, bindingContext, parameterType, out arguments[index], out bindingError))
				{
					elemInit = null;
					return false;
				}

				index++;
			}

			elemInit = Expression.ElementInit(addMethod, arguments);
			return true;
		}
	}
}
