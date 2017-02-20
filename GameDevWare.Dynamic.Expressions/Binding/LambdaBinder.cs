using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class LambdaBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (bindingContext == null) throw new ArgumentNullException("bindingContext");
			if (expectedType == null) throw new ArgumentNullException("expectedType");

			boundExpression = null;
			bindingError = null;

			if (expectedType.IsOpenGenericType || !expectedType.IsDelegate)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_VALIDDELEGATETYPEISEXPECTED, expectedType != null ? expectedType.ToString() : "<null>"));
				return false;
			}

			var expressionType = node.GetExpressionType(throwOnError: true);
			var bodyNode = node.GetExpression(throwOnError: true);
			var argumentsTree = node.GetArguments(throwOnError: false);
			var lambdaInvokeMethod = expectedType.GetMembers(Constants.DELEGATE_INVOKE_NAME).FirstOrDefault(m => m.IsMethod && !m.IsStatic);
			if (lambdaInvokeMethod == null)
			{
				bindingError = new MissingMethodException(expectedType.ToString(), Constants.DELEGATE_INVOKE_NAME);
				return false;
			}

			if (lambdaInvokeMethod.GetParametersCount() != argumentsTree.Count)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_INVALIDLAMBDAARGUMENTS, expectedType));
				return false;
			}

			var argumentNames = new string[argumentsTree.Count];
			for (var i = 0; i < argumentNames.Length; i++)
			{
				var argumentNameTree = default(SyntaxTreeNode);
				if (argumentsTree.TryGetValue(i, out argumentNameTree) == false || argumentNameTree == null || argumentNameTree.GetExpressionType(throwOnError: true) != Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD)
				{
					bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_ATTRIBUTE, expressionType), node);
					return false;
				}
				argumentNames[i] = argumentNameTree.GetPropertyOrFieldName(throwOnError: true);
			}

			var lambdaParameters = new ParameterExpression[argumentsTree.Count];
			for (var i = 0; i < argumentsTree.Count; i++)
				lambdaParameters[i] = Expression.Parameter(lambdaInvokeMethod.GetParameter(i).ParameterType, argumentNames[i]);

			var currentParameters = bindingContext.Parameters;
			var builderParameters = new List<ParameterExpression>(lambdaParameters.Length + currentParameters.Count);
			// add all lambda's parameters
			builderParameters.AddRange(lambdaParameters);
			// add closure parameters
			foreach (var parameterExpr in currentParameters)
				if (Array.IndexOf(argumentNames, parameterExpr.Name) < 0)
					builderParameters.Add(parameterExpr);

			var nestedBindingContext = bindingContext.CreateNestedContext(builderParameters, lambdaInvokeMethod.ResultType);
			var body = default(Expression);
			if (AnyBinder.TryBind(bodyNode, nestedBindingContext, TypeDescription.GetTypeDescription(lambdaInvokeMethod.ResultType), out body, out bindingError) == false)
				return false;

			boundExpression = Expression.Lambda(expectedType, body, lambdaParameters);
			return true;
		}

		public static string[] ExtractArgumentNames(SyntaxTreeNode node)
		{
			var arguments = node.GetArguments(throwOnError: false);
			var argumentNames = new string[arguments.Count];
			for (var i = 0; i < argumentNames.Length; i++)
			{
				var argumentNameTree = default(SyntaxTreeNode);
				if (arguments.TryGetValue(i, out argumentNameTree) == false || argumentNameTree == null || argumentNameTree.GetExpressionType(throwOnError: true) != Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_ATTRIBUTE, Constants.EXPRESSION_TYPE_LAMBDA), node);
				argumentNames[i] = argumentNameTree.GetPropertyOrFieldName(throwOnError: true);
			}
			return argumentNames;
		}
	}
}
