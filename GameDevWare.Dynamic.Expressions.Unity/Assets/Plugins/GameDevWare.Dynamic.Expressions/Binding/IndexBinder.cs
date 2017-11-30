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
using System.Diagnostics;
using System.Linq.Expressions;
using GameDevWare.Dynamic.Expressions.Properties;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class IndexBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			boundExpression = null;
			bindingError = null;

			var useNullPropagation = node.GetUseNullPropagation(throwOnError: false);
			var arguments = node.GetArguments(throwOnError: true);
			var targetNode = node.GetExpression(throwOnError: true);
			var target = default(Expression);
			if (AnyBinder.TryBind(targetNode, bindingContext, TypeDescription.ObjectType, out target, out bindingError) == false)
				return false;

			Debug.Assert(target != null, "target != null");

			var targetTypeDescription = TypeDescription.GetTypeDescription(target.Type);
			if (target.Type.IsArray)
			{
				var indexType = TypeDescription.Int32Type;
				var indexingExpressions = new Expression[arguments.Count];
				for (var i = 0; i < indexingExpressions.Length; i++)
				{
					var argument = default(SyntaxTreeNode);
					if (arguments.TryGetValue(i, out argument) == false)
					{
						bindingError = new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_MISSINGMETHODPARAMETER, i), node);
						return false;
					}

					if (AnyBinder.TryBindInNewScope(argument, bindingContext, indexType, out indexingExpressions[i], out bindingError) == false)
						return false;

					Debug.Assert(indexingExpressions[i] != null, "indexingExpressions[i] != null");
				}

				try
				{
					if (indexingExpressions.Length == 1)
						boundExpression = Expression.ArrayIndex(target, indexingExpressions[0]);
					else
						boundExpression = Expression.ArrayIndex(target, indexingExpressions);
				}
				catch (Exception exception)
				{
					bindingError = new ExpressionParserException(exception.Message, exception, node);
					return false;
				}
			}
			else
			{
				var selectedIndexerQuality = MemberDescription.QUALITY_INCOMPATIBLE;
				foreach (var indexer in targetTypeDescription.Indexers)
				{
					var indexerQuality = MemberDescription.QUALITY_INCOMPATIBLE;
					var indexerCall = default(Expression);
					if (indexer.TryMakeCall(target, arguments, bindingContext, out indexerCall, out indexerQuality) == false)
						continue;
					if (indexerQuality <= selectedIndexerQuality)
						continue;

					boundExpression = indexerCall;
					selectedIndexerQuality = indexerQuality;
				}
			}
			if (boundExpression == null)
			{
				bindingError = new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_UNABLETOBINDINDEXER, target.Type), node);
				return false;
			}

			if (useNullPropagation && targetTypeDescription.CanBeNull)
				bindingContext.RegisterNullPropagationTarget(target);

			return true;
		}
	}
}
