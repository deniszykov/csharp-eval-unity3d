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

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class NewBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (bindingContext == null) throw new ArgumentNullException("bindingContext");
			if (expectedType == null) throw new ArgumentNullException("expectedType");

			boundExpression = null;
			bindingError = null;

			if (node == null) throw new ArgumentNullException("node");

			var arguments = node.GetArguments(throwOnError: false);
			var typeName = node.GetTypeName(throwOnError: true);
			var type = default(Type);
			if (bindingContext.TryResolveType(typeName, out type) == false)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVETYPE, typeName), node);
				return false;
			}

			var typeDescription = TypeDescription.GetTypeDescription(type);

			// feature: lambda building via new Func()
			var lambdaArgument = default(SyntaxTreeNode);
			if (typeDescription.IsDelegate && arguments.Count == 1 && (lambdaArgument = arguments.Values.Single()).GetExpressionType(throwOnError: true) == Constants.EXPRESSION_TYPE_LAMBDA)
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
	}
}
