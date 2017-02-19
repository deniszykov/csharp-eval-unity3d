using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class NewBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			boundExpression = null;
			bindingError = null;
			return false;
			/*
			if (node == null) throw new ArgumentNullException("node");

			var arguments = node.GetArguments(throwOnError: false);
			var typeName = node.GetTypeName(throwOnError: true);
			var typeReference = default(TypeReference);
			var type = default(Type);
			if (bindingContext.TryResolveType(typeName, out type) == false)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVETYPE, typeReference ?? typeName), node);
				return false;
			}

			var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
			Array.Sort(constructors, (x, y) => x.GetParameters().Length.CompareTo(y.GetParameters().Length));

			// feature: lambda building via new Func()
			var lambdaArgument = default(SyntaxTreeNode);
			if (typeof(Delegate).IsAssignableFrom(type) && arguments.Count == 1 && (lambdaArgument = arguments.Values.Single()).GetExpressionType(throwOnError: true) == Constants.EXPRESSION_TYPE_LAMBDA)
			{
				if (LambdaBinder.TryBind(lambdaArgument, bindingContext, TypeMatch.Exact(type), out boundExpression, out bindingError) == false)
					return false;
				return true;
			}

			foreach (var constructorInfo in constructors)
			{
				var methodParameters = constructorInfo.GetParameters();
				var argumentExpressions = default(Expression[]);
				if (TryBindMethod(methodParameters, arguments, context, out argumentExpressions) <= 0)
					continue;

				try
				{
					return Expression.New(constructorInfo, argumentExpressions);
				}
				catch (Exception exception)
				{
					throw new ExpressionParserException(exception.Message, exception, node);
				}
			}
			throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETOBINDCONSTRUCTOR, type), node);
		*/
		}
	}
}
