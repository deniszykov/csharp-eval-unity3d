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
				if (constructorDescription.TryMakeCall(null, arguments, bindingContext, out constructorCall, out constructorQuality))
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
