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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class NewBinder
	{
		public static bool TryBind
			(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (bindingContext == null) throw new ArgumentNullException("bindingContext");
			if (expectedType == null) throw new ArgumentNullException("expectedType");

			if (node == null) throw new ArgumentNullException("node");

			var arguments = node.GetArguments(throwOnError: false);
			var methodName = node.GetMethodName(throwOnError: false);
			if (methodName != null)
			{
				return TryBindToMethod(node, methodName, arguments, bindingContext, expectedType, out boundExpression, out bindingError);
			}
			else
			{
				var typeName = node.GetTypeName(throwOnError: true);
				return TryBindToType(node, typeName, arguments, bindingContext, expectedType, out boundExpression, out bindingError);
			}
		}
		private static bool TryBindToType
		(
			SyntaxTreeNode node,
			object typeName,
			ArgumentsTree arguments,
			BindingContext bindingContext,
			TypeDescription expectedType,
			out Expression boundExpression,
			out Exception bindingError)
		{
			boundExpression = null;
			bindingError = null;

			var type = default(Type);
			if (bindingContext.TryResolveType(typeName, out type) == false)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVETYPE, typeName), node);
				return false;
			}

			var typeDescription = TypeDescription.GetTypeDescription(type);

			// feature: lambda building via new Func()
			var lambdaArgument = default(SyntaxTreeNode);
			if (typeDescription.IsDelegate &&
				arguments.Count == 1 &&
				(lambdaArgument = arguments.Values.Single()).GetExpressionType(throwOnError: true) == Constants.EXPRESSION_TYPE_LAMBDA)
				return LambdaBinder.TryBind(lambdaArgument, bindingContext, typeDescription, out boundExpression, out bindingError);

			var selectedConstructorQuality = MemberDescription.QUALITY_INCOMPATIBLE;
			foreach (var constructorDescription in typeDescription.Constructors)
			{
				var constructorQuality = MemberDescription.QUALITY_INCOMPATIBLE;
				var constructorCall = default(Expression);
				if (constructorDescription.TryMakeCall(null, arguments, bindingContext, out constructorCall, out constructorQuality) == false)
					continue;

				if (float.IsNaN(constructorQuality) || constructorQuality <= selectedConstructorQuality)
					continue;

				boundExpression = constructorCall;
				selectedConstructorQuality = constructorQuality;

				if (Math.Abs(constructorQuality - MemberDescription.QUALITY_EXACT_MATCH) < float.Epsilon)
					break; // best match
			}

			if (boundExpression == null)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETOBINDCONSTRUCTOR, type), node);
				return false;
			}

			return true;
		}

		private static bool TryBindToMethod
		(
			SyntaxTreeNode node,
			object methodName,
			ArgumentsTree arguments,
			BindingContext bindingContext,
			TypeDescription expectedType,
			out Expression boundExpression,
			out Exception bindingError)
		{
			boundExpression = null;
			bindingError = null;

			var constructorDescription = default(MemberDescription);
			if (bindingContext.TryResolveMember(methodName, out constructorDescription) == false || constructorDescription.IsConstructor == false)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETOBINDCONSTRUCTOR, methodName), node);
				return false;
			}

			var typeDescription = TypeDescription.GetTypeDescription(constructorDescription.DeclaringType);

			// feature: lambda building via new Func()
			var lambdaArgument = default(SyntaxTreeNode);
			if (typeDescription.IsDelegate &&
				arguments.Count == 1 &&
				(lambdaArgument = arguments.Values.Single()).GetExpressionType(throwOnError: true) == Constants.EXPRESSION_TYPE_LAMBDA)
				return LambdaBinder.TryBind(lambdaArgument, bindingContext, typeDescription, out boundExpression, out bindingError);

			var constructorQuality = MemberDescription.QUALITY_INCOMPATIBLE;
			if (constructorDescription.TryMakeCall(null, arguments, bindingContext, out boundExpression, out constructorQuality))
			{
				return true;
			}

			bindingError = new ExpressionParserException(
				string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETOBINDCONSTRUCTOR, constructorDescription.DeclaringType), node);
			return false;
		}
	}
}
