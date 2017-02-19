using System;
using System.Linq.Expressions;
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class CallBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			boundExpression = null;
			bindingError = null;

			var targetNode = node.GetExpression(throwOnError: false);
			var arguments = node.GetArguments(throwOnError: false);
			var methodName = node.GetMethodName(throwOnError: true);
			var useNullPropagation = node.GetUseNullPropagation(throwOnError: false);

			var methodRef = default(TypeReference);
			if (BindingContext.TryGetMethodReference(methodName, out methodRef) == false)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVENAME, methodName), node);
				return false;
			}
			if (targetNode == null && bindingContext.Global == null)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVENAME, methodRef ?? methodName), node);
				return false;
			}

			var expression = default(Expression);
			var type = default(Type);
			var isStatic = false;
			if (targetNode == null)
			{
				expression = bindingContext.Global;
				type = expression.Type;
				isStatic = false;
			}
			else if (bindingContext.TryResolveType(targetNode, out type))
			{
				expression = null;
				isStatic = true;
			}
			else
			{
				if (TryBind(targetNode, bindingContext, null, out expression, out bindingError) == false)
					return false;

				type = expression.Type;
				isStatic = false;
			}

			var selectedMethodQuality = MemberDescription.QUALITY_INCOMPATIBLE;
			var selectedMethodCallExpression = default(Expression);
			var hasGenericParameters = methodRef.TypeArguments.Count > 0;
			var genericArguments = default(Type[]);
			if (hasGenericParameters)
			{
				genericArguments = new Type[methodRef.TypeArguments.Count];
				for (var i = 0; i < genericArguments.Length; i++)
				{
					var typeArgument = methodRef.TypeArguments[i];
					if (bindingContext.TryResolveType(typeArgument, out genericArguments[i]) == false)
					{
						bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVETYPE, typeArgument), node);
						return false;
					}
				}
			}

			var typeDescription = Metadata.GetTypeDescription(type);
			foreach (var memberDescription in typeDescription.GetMembers(methodRef.Name))
			{
				var methodDescription = memberDescription;
				var method = ((MemberInfo)memberDescription) as MethodInfo;

				if (method == null || method.IsStatic != isStatic || method.IsGenericMethod != hasGenericParameters)
					continue;
				if (hasGenericParameters && memberDescription.GenericArgumentsCount != methodRef.TypeArguments.Count)
					continue;

				if (hasGenericParameters)
				{
					if (!method.IsGenericMethodDefinition) method = method.GetGenericMethodDefinition();
					try
					{
						method = method.MakeGenericMethod(genericArguments);
						methodDescription = new MemberDescription(typeDescription, method);
					}
					catch (ArgumentException exception)
					{
						bindingError = exception;
						continue; /* An element of typeArguments does not satisfy the constraints specified for the corresponding type parameter of the current generic method definition. */
					}
				}


				var methodQuality = 0.0f;
				var methodCallExpression = default(Expression);
				if (methodDescription.TryMakeCall(expression, arguments, bindingContext, out methodCallExpression, out methodQuality) == false)
					continue;

				if (float.IsNaN(methodQuality) || methodQuality <= selectedMethodQuality)
					continue;

				selectedMethodCallExpression = methodCallExpression;
				selectedMethodQuality = methodQuality;
			}

			if (bindingError != null)
				return false;

			if (selectedMethodCallExpression == null)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETOBINDCALL, methodRef.Name, type), node);
				return false;
			}

			if (useNullPropagation && expression == null)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETOAPPLYNULLCONDITIONALOPERATORONTYPEREF, type));
				return false;
			}

			if (useNullPropagation)
				boundExpression = ExpressionUtils.MakeNullPropagationExpression(expression, selectedMethodCallExpression);
			else
				boundExpression = selectedMethodCallExpression;

			return true;
		}
	}
}
