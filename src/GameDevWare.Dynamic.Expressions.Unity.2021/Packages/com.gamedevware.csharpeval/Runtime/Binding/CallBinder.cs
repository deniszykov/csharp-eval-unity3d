/*
	Copyright (c) 2016 Denis Zykov, GameDevWare.com

	This a part of "C# Eval()" Unity Asset - https://www.assetstore.unity3d.com/en/#!/content/56706

	THIS SOFTWARE IS DISTRIBUTED "AS-IS" WITHOUT ANY WARRANTIES, CONDITIONS AND
	REPRESENTATIONS WHETHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION THE
	IMPLIED WARRANTIES AND CONDITIONS OF MERCHANTABILITY, MERCHANTABLE QUALITY,
	FITNESS FOR A PARTICULAR PURPOSE, DURABILITY, NON-INFRINGEMENT, PERFORMANCE
	AND THOSE ARISING BY STATUTE OR FROM CUSTOM OR USAGE OF TRADE OR COURSE OF DEALING.

	This source code is distributed via Unity Asset Store,
	to use it in your project you should accept Terms of Service and EULA
	https://unity3d.com/ru/legal/as_terms
*/

using System;
using System.Linq.Expressions;
using System.Reflection;
using GameDevWare.Dynamic.Expressions.Properties;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class CallBinder
	{
		public static bool TryBind
			(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException(nameof(node));
			if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));
			if (expectedType == null) throw new ArgumentNullException(nameof(expectedType));

			boundExpression = null;
			bindingError = null;

			var target = default(Expression);
			var arguments = node.GetArguments(false);
			var methodName = node.GetMethodName(true);
			var useNullPropagation = node.GetUseNullPropagation(false);

			if (bindingContext.TryResolveMember(methodName, out var methodMember))
			{
				if (!methodMember.IsMethod)
				{
					bindingError = new ExpressionParserException(
						string.Format(Resources.EXCEPTION_BIND_CALLMEMBERISNOTMETHOD, methodMember.Name, methodMember.DeclaringType), node);
					return false;
				}

				var targetNode = node.GetExpression(true);

				if (!AnyBinder.TryBind(targetNode, bindingContext, TypeDescription.ObjectType, out target, out bindingError))
					return false;

				if (!methodMember.TryMakeCall(target, arguments, bindingContext, out boundExpression, out _))
				{
					bindingError = new ExpressionParserException(
						string.Format(Resources.EXCEPTION_BIND_UNABLETOBINDMETHOD, methodMember.Name, target.Type, arguments.Count), node);
					return false;
				}

				return true;
			}

			if (!BindingContext.TryGetMethodReference(methodName, out var methodRef))
			{
				bindingError = new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_UNABLETORESOLVENAME, methodName), node);
				return false;
			}

			if (!TryBindTarget(node, bindingContext, out target, out var targetType, out bindingError)) return false;

			var isStatic = target == null;
			var selectedMethodQuality = MemberDescription.QUALITY_INCOMPATIBLE;
			var hasGenericParameters = methodRef.IsGenericType;
			var genericArguments = default(Type[]);
			if (hasGenericParameters)
			{
				genericArguments = new Type[methodRef.TypeArguments.Count];
				for (var i = 0; i < genericArguments.Length; i++)
				{
					var typeArgument = methodRef.TypeArguments[i];
					if (!bindingContext.TryResolveType(typeArgument, out genericArguments[i]))
					{
						bindingError = new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_UNABLETORESOLVETYPE, typeArgument), node);
						return false;
					}
				}
			}

			var targetTypeDescription = TypeDescription.GetTypeDescription(targetType);
			var foundMethod = default(MethodInfo);
			BindMethodGroup(targetTypeDescription.GetMembers(methodRef.Name), target, false, isStatic, genericArguments, hasGenericParameters, arguments,
				bindingContext, ref boundExpression, ref selectedMethodQuality, ref foundMethod, ref bindingError);

			// extension methods are only looked up when no declared method of the target's type is applicable
			if (boundExpression == null &&
				bindingError == null &&
				target != null &&
				bindingContext.TryGetExtensionMethods(targetType, methodRef.Name, out var extensionMethods))
			{
				BindMethodGroup(extensionMethods, target, true, isStatic, genericArguments, hasGenericParameters, arguments, bindingContext,
					ref boundExpression, ref selectedMethodQuality, ref foundMethod, ref bindingError);
			}

			if (bindingError != null)
				return false;

			if (boundExpression == null)
			{
				if (foundMethod != null)
				{
					bindingError = new ExpressionParserException(
						string.Format(Resources.EXCEPTION_BIND_UNABLETOBINDMETHOD, methodRef.Name, targetType, arguments.Count), node);
				}
				else
				{
					bindingError = new ExpressionParserException(
						string.Format(Resources.EXCEPTION_BIND_UNABLETOBINDCALL, methodRef.Name, targetType, arguments.Count), node);
				}

				return false;
			}

			if (useNullPropagation && target == null)
			{
				bindingError = new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_UNABLETOAPPLYNULLCONDITIONALOPERATORONTYPEREF,
					targetType));
				return false;
			}

			if (useNullPropagation && targetTypeDescription.CanBeNull)
				bindingContext.RegisterNullPropagationTarget(target);

			if (targetTypeDescription.IsAssignableFrom(typeof(Type)) &&
				!bindingContext.IsKnownType(typeof(Type)) &&
				(!bindingContext.IsKnownType(targetType) || methodRef.Name.Equals("InvokeMember", StringComparison.Ordinal)))
			{
				bindingError = new ExpressionParserException(
					string.Format(Resources.EXCEPTION_BIND_RESTRICTED_MEMBER_INVOCATION, methodName, targetType, typeof(ITypeResolver)), node);
				return false;
			}

			return true;
		}

		/// <summary>
		///     Selects the best overload of a method group and binds a call to it. A generic method invoked without explicit
		///     type arguments has them inferred from the call's arguments.
		/// </summary>
		private static void BindMethodGroup
		(
			MemberDescription[] members,
			Expression target,
			bool isExtension,
			bool isStatic,
			Type[] genericArguments,
			bool hasGenericParameters,
			ArgumentsTree arguments,
			BindingContext bindingContext,
			ref Expression boundExpression,
			ref float selectedMethodQuality,
			ref MethodInfo foundMethod,
			ref Exception bindingError
		)
		{
			foreach (var memberDescription in members)
			{
				if (!memberDescription.IsMethod) continue;

				var methodDescription = memberDescription;
				var method = (MethodInfo)memberDescription;

				foundMethod = foundMethod ?? method;

				if (!isExtension && method.IsStatic != isStatic)
					continue;

				if (hasGenericParameters && (!method.IsGenericMethod || memberDescription.GenericArgumentsCount != genericArguments.Length))
					continue;

				if (hasGenericParameters)
				{
					try
					{
						methodDescription = methodDescription.MakeGenericMethod(genericArguments);
					}
					catch (ArgumentException exception)
					{
						bindingError = exception;
						continue; /* An element of typeArguments does not satisfy the constraints specified for the corresponding type parameter of the current generic method definition. */
					}
				}
				else if (method.IsGenericMethod)
				{
					if (!TypeInference.TryInferGenericArguments(methodDescription, isExtension ? target : null, arguments, bindingContext,
							out var inferredArguments))
						continue;

					try
					{
						methodDescription = methodDescription.MakeGenericMethod(inferredArguments);
					}
					catch (ArgumentException)
					{
						continue; // inferred type arguments don't satisfy the method's constraints
					}
				}

				if (!methodDescription.TryMakeCall(target, arguments, bindingContext, isExtension, out var methodCallExpression, out var methodQuality))
					continue;

				if (float.IsNaN(methodQuality) || methodQuality <= selectedMethodQuality)
					continue;

				boundExpression = methodCallExpression;
				selectedMethodQuality = methodQuality;

				if (Math.Abs(methodQuality - MemberDescription.QUALITY_EXACT_MATCH) < float.Epsilon)
					break; // best match
			}
		}

		private static bool TryBindTarget(SyntaxTreeNode node, BindingContext bindingContext, out Expression target, out Type type, out Exception bindingError)
		{
			type = null;
			target = null;
			bindingError = null;

			// target is passed as Expression from InvokeBinder
			if (node.TryGetValue(Constants.EXPRESSION_ATTRIBUTE, out var targetObj))
			{
				if (targetObj is Expression targetExpr)
				{
					target = targetExpr;
					type = target.Type;
					return true;
				}

				if (targetObj is Type typeValue)
				{
					target = null;
					type = typeValue;
					return true;
				}
			}

			var targetNode = node.GetExpression(false);
			if (targetNode == null)
			{
				if (bindingContext.Global == null)
				{
					var methodName = node.GetMethodName(false);
					bindingError = new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_UNABLETORESOLVENAME, methodName ?? "<unknown>"),
						node);
					return false;
				}

				target = bindingContext.Global;
				type = target.Type;
			}
			else if (bindingContext.TryResolveType(targetNode, out type))
				target = null;
			else
			{
				if (!TryBind(targetNode, bindingContext, TypeDescription.ObjectType, out target, out bindingError))
					return false;

				type = target.Type;
			}

			return true;
		}
	}
}
