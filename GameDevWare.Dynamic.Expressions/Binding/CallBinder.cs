using System;
using System.Linq.Expressions;
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class CallBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (bindingContext == null) throw new ArgumentNullException("bindingContext");
			if (expectedType == null) throw new ArgumentNullException("expectedType");

			boundExpression = null;
			bindingError = null;

			var target = default(Expression);
			var arguments = node.GetArguments(throwOnError: false);
			var methodName = node.GetMethodName(throwOnError: true);
			var useNullPropagation = node.GetUseNullPropagation(throwOnError: false);

			var methodRef = default(TypeReference);
			if (BindingContext.TryGetMethodReference(methodName, out methodRef) == false)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVENAME, methodName), node);
				return false;
			}

			var targetType = default(Type);
			if (TryBindTarget(node, bindingContext, out target, out targetType, out bindingError) == false)
			{
				return false;
			}

			var isStatic = target == null;
			var selectedMethodQuality = MemberDescription.QUALITY_INCOMPATIBLE;
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

			var targetTypeDescription = TypeDescription.GetTypeDescription(targetType);
			foreach (var memberDescription in targetTypeDescription.GetMembers(methodRef.Name))
			{
				if (memberDescription.IsMethod == false) continue;

				var methodDescription = memberDescription;
				var method = (MethodInfo)memberDescription;

				if (method == null || method.IsStatic != isStatic || method.IsGenericMethod != hasGenericParameters)
					continue;
				if (hasGenericParameters && memberDescription.GenericArgumentsCount != methodRef.TypeArguments.Count)
					continue;

				if (hasGenericParameters)
				{
					try
					{
						methodDescription = methodDescription.MakeGenericMethod(genericArguments);
						method = methodDescription;
					}
					catch (ArgumentException exception)
					{
						bindingError = exception;
						continue; /* An element of typeArguments does not satisfy the constraints specified for the corresponding type parameter of the current generic method definition. */
					}
				}


				var methodQuality = 0.0f;
				var methodCallExpression = default(Expression);
				if (methodDescription.TryMakeCall(target, arguments, bindingContext, out methodCallExpression, out methodQuality) == false)
					continue;

				if (float.IsNaN(methodQuality) || methodQuality <= selectedMethodQuality)
					continue;

				boundExpression = methodCallExpression;
				selectedMethodQuality = methodQuality;

				if (Math.Abs(methodQuality - MemberDescription.QUALITY_EXACT_MATCH) < float.Epsilon)
					break; // best match
			}

			if (bindingError != null)
				return false;

			if (boundExpression == null)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETOBINDCALL, methodRef.Name, targetType, arguments.Count), node);
				return false;
			}

			if (useNullPropagation && target == null)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETOAPPLYNULLCONDITIONALOPERATORONTYPEREF, targetType));
				return false;
			}

			if (useNullPropagation && targetTypeDescription.CanBeNull)
				bindingContext.RegisterNullPropagationTarger(target);

			return true;
		}

		private static bool TryBindTarget(SyntaxTreeNode node, BindingContext bindingContext, out Expression target, out Type type, out Exception bindingError)
		{
			type = null;
			target = null;
			bindingError = null;

			// target is passed as Expression from InvokeBinder
			var targetObj = default(object);
			if (node.TryGetValue(Constants.EXPRESSION_ATTRIBUTE, out targetObj))
			{
				if (targetObj is Expression)
				{
					target = (Expression)targetObj;
					type = target.Type;
					return true;
				}
				else if (targetObj is Type)
				{
					target = null;
					type = (Type)targetObj;
					return true;
				}
			}

			var targetNode = node.GetExpression(throwOnError: false);
			if (targetNode == null)
			{
				if (bindingContext.Global == null)
				{
					var methodName = node.GetMethodName(throwOnError: false);
					bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVENAME, methodName ?? "<unknown>"), node);
					return false;
				}

				target = bindingContext.Global;
				type = target.Type;
			}
			else if (bindingContext.TryResolveType(targetNode, out type))
			{
				target = null;
			}
			else
			{
				if (TryBind(targetNode, bindingContext, null, out target, out bindingError) == false)
					return false;

				type = target.Type;
			}
			return true;
		}
	}
}
