using System;
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

			var indexExpression = default(Expression);
			if (target.Type.IsArray)
			{
				var indexType = TypeDescription.Int32Type;
				var indexingExpressions = new Expression[arguments.Count];
				for (var i = 0; i < indexingExpressions.Length; i++)
				{
					var argument = default(SyntaxTreeNode);
					if (arguments.TryGetValue(i, out argument) == false)
					{
						bindingError = bindingError = new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_MISSINGMETHODPARAMETER, i), node);
						return false;
					}

					if (AnyBinder.TryBind(argument, bindingContext, indexType, out indexingExpressions[i], out bindingError) == false)
						return false;
				}

				try
				{
					if (indexingExpressions.Length == 1)
						indexExpression = Expression.ArrayIndex(target, indexingExpressions[0]);
					else
						indexExpression = Expression.ArrayIndex(target, indexingExpressions);
				}
				catch (Exception exception)
				{
					bindingError = new ExpressionParserException(exception.Message, exception, node);
					return false;
				}
			}
			else
			{
				var typeDescription = TypeDescription.GetTypeDescription(target.Type);
				var selectedIndexerQuality = MemberDescription.QUALITY_INCOMPATIBLE;
				foreach (var indexer in typeDescription.Indexers)
				{
					var indexerQuality = MemberDescription.QUALITY_INCOMPATIBLE;
					var indexerCall = default(Expression);
					if (indexer.TryMakeCall(target, arguments, bindingContext, out indexerCall, out indexerQuality) == false)
						continue;
					if (indexerQuality <= selectedIndexerQuality)
						continue;

					indexExpression = indexerCall;
					selectedIndexerQuality = indexerQuality;
				}
			}
			if (indexExpression == null)
			{
				bindingError = new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_UNABLETOBINDINDEXER, target.Type), node);
				return false;
			}

			if (useNullPropagation)
				boundExpression = ExpressionUtils.MakeNullPropagationExpression(target, indexExpression);
			else
				boundExpression = indexExpression;
			return true;
		}
	}
}
