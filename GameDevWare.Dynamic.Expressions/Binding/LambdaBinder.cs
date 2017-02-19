using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class LambdaBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			boundExpression = null;
			bindingError = null;

			var lambdaType = (Type)expectedType;
			if (lambdaType == null || lambdaType.ContainsGenericParameters || !expectedType.IsDelegate)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_VALIDDELEGATETYPEISEXPECTED, lambdaType != null ? lambdaType.ToString() : "<null>"));
				return false;
			}

			var expressionType = node.GetExpressionType(throwOnError: true);
			var expression = node.GetExpression(throwOnError: true);
			var arguments = node.GetArguments(throwOnError: false);
			var lambdaInvokeMethod = lambdaType.GetMethod(Constants.DELEGATE_INVOKE_NAME, BindingFlags.Public | BindingFlags.Instance);
			if (lambdaInvokeMethod == null)
			{
				bindingError = new MissingMethodException(lambdaType.FullName, Constants.DELEGATE_INVOKE_NAME);
				return false;
			}
			var lambdaInvokeMethodParameters = lambdaInvokeMethod.GetParameters();
			if (lambdaInvokeMethodParameters.Length != arguments.Count)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_INVALIDLAMBDAARGUMENTS, lambdaType));
				return false;
			}

			var argumentNames = new string[arguments.Count];
			for (var i = 0; i < argumentNames.Length; i++)
			{
				var argumentNameTree = default(SyntaxTreeNode);
				if (arguments.TryGetValue(i, out argumentNameTree) == false || argumentNameTree == null || argumentNameTree.GetExpressionType(throwOnError: true) != Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD)
				{
					bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_ATTRIBUTE, expressionType), node);
					return false;
				}
				argumentNames[i] = argumentNameTree.GetPropertyOrFieldName(throwOnError: true);
			}

			var lambdaParameters = new ParameterExpression[lambdaInvokeMethodParameters.Length];
			for (var i = 0; i < lambdaInvokeMethodParameters.Length; i++)
				lambdaParameters[i] = Expression.Parameter(lambdaInvokeMethodParameters[i].ParameterType, argumentNames[i]);

			var currentParameters = bindingContext.Parameters;
			var builderParameters = new List<ParameterExpression>(lambdaParameters.Length + currentParameters.Count);
			// add all lambda's parameters
			builderParameters.AddRange(lambdaParameters);
			// add closure parameters
			foreach (var parameterExpr in currentParameters)
				if (Array.IndexOf(argumentNames, parameterExpr.Name) < 0)
					builderParameters.Add(parameterExpr);

			var nestedBindingContext = bindingContext.CreateNestedContext(builderParameters, lambdaInvokeMethod.ReturnType);
			var body = default(Expression);
			if (AnyBinder.TryBind(expression, nestedBindingContext, Metadata.GetTypeDescription(lambdaInvokeMethod.ReturnType), out body, out bindingError) == false)
				return false;

			boundExpression = Expression.Lambda(lambdaType, body, lambdaParameters);
			return true;
		}
	}
}
