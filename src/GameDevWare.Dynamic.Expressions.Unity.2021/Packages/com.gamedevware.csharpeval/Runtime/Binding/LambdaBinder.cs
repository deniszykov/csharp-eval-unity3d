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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using GameDevWare.Dynamic.Expressions.Properties;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class LambdaBinder
	{
		public static bool TryBind
			(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException(nameof(node));
			if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));
			if (expectedType == null) throw new ArgumentNullException(nameof(expectedType));

			boundExpression = null;
			bindingError = null;

			// try get type of lambda from node
			var lambdaTypeName = node.GetTypeName(false);
			if (lambdaTypeName != null)
			{
				if (!bindingContext.TryResolveType(lambdaTypeName, out var lambdaType))
				{
					bindingError = new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_UNABLETORESOLVETYPE, lambdaTypeName), node);
					return false;
				}

				expectedType = TypeDescription.GetTypeDescription(lambdaType);
			}

			if (expectedType.HasGenericParameters || !expectedType.IsDelegate)
			{
				bindingError = new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_VALIDDELEGATETYPEISEXPECTED,
					expectedType));
				return false;
			}

			var expressionType = node.GetExpressionType(true);
			var bodyNode = node.GetExpression(true);
			var argumentsTree = node.GetArguments(false);
			var lambdaInvokeMethod = expectedType.GetMembers(Constants.DELEGATE_INVOKE_NAME).FirstOrDefault(m => m.IsMethod && !m.IsStatic);
			if (lambdaInvokeMethod == null)
			{
				bindingError = new MissingMethodException(string.Format(Resources.EXCEPTION_BIND_MISSINGMETHOD, expectedType,
					Constants.DELEGATE_INVOKE_NAME));
				return false;
			}

			if (lambdaInvokeMethod.GetParametersCount() != argumentsTree.Count)
			{
				bindingError = new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_INVALIDLAMBDAARGUMENTS, expectedType));
				return false;
			}

			var argumentNames = new string[argumentsTree.Count];
			for (var i = 0; i < argumentNames.Length; i++)
			{
				var argumentNameTreeType = default(string);
				if (!argumentsTree.TryGetValue(i, out var argumentNameTree) ||
					argumentNameTree == null ||
					(argumentNameTreeType = argumentNameTree.GetExpressionType(true)) == null ||
					!(argumentNameTreeType == Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD ||
						argumentNameTreeType == Constants.EXPRESSION_TYPE_MEMBER_RESOLVE ||
						argumentNameTreeType == Constants.EXPRESSION_TYPE_PARAMETER))
				{
					bindingError = new ExpressionParserException(
						string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_ATTRIBUTE, expressionType), node);
					return false;
				}

				argumentNames[i] = argumentNameTree.GetMemberName(true);
			}

			var lambdaParameters = new ParameterExpression[argumentsTree.Count];
			for (var i = 0; i < argumentsTree.Count; i++)
				lambdaParameters[i] = Expression.Parameter(lambdaInvokeMethod.GetParameter(i).ParameterType, argumentNames[i]);

			var currentParameters = bindingContext.Parameters;
			var newParameters = new List<ParameterExpression>(lambdaParameters.Length + currentParameters.Count);

			// add all lambda's parameters
			newParameters.AddRange(lambdaParameters);

			// add closure parameters
			foreach (var parameterExpr in currentParameters)
			{
				if (Array.IndexOf(argumentNames, parameterExpr.Name) < 0)
					newParameters.Add(parameterExpr);
			}

			var nestedBindingContext = bindingContext.CreateNestedContext(newParameters.AsReadOnly(), lambdaInvokeMethod.ResultType);
			if (!AnyBinder.TryBindInNewScope(bodyNode, nestedBindingContext, TypeDescription.GetTypeDescription(lambdaInvokeMethod.ResultType), out var body,
					out bindingError))
				return false;

			Debug.Assert(body != null, "body != null");

			boundExpression = Expression.Lambda(expectedType, body, lambdaParameters);
			return true;
		}

		public static string[] ExtractArgumentNames(SyntaxTreeNode node)
		{
			var arguments = node.GetArguments(false);
			var argumentNames = new string[arguments.Count];
			for (var i = 0; i < argumentNames.Length; i++)
			{
				if (!arguments.TryGetValue(i, out var argumentNameTree) || argumentNameTree == null)
				{
					throw new ExpressionParserException(
						string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_ATTRIBUTE, Constants.EXPRESSION_TYPE_LAMBDA),
						node);
				}

				var argumentNameType = argumentNameTree.GetExpressionType(true);
				if (argumentNameType != Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD &&
					argumentNameType != Constants.EXPRESSION_TYPE_MEMBER_RESOLVE &&
					argumentNameType != Constants.EXPRESSION_TYPE_PARAMETER)
				{
					throw new ExpressionParserException(
						string.Format(Resources.EXCEPTION_BIND_INVALIDLAMBDAPARAMETERTYPE, argumentNameType, Constants.EXPRESSION_TYPE_PARAMETER), node);
				}

				argumentNames[i] = argumentNameTree.GetMemberName(true);
			}

			return argumentNames;
		}
	}
}
