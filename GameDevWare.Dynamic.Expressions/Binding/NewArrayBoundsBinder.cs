using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class NewArrayBoundsBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			boundExpression = null;
			bindingError = null;
			return false;
			/*
			var typeName = node.GetTypeName(throwOnError: true);
			var typeReference = default(TypeReference);
			var type = default(Type);
			if (TryGetTypeReference(typeName, out typeReference) == false || this.typeResolver.TryGetType(typeReference, out type) == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVETYPE, typeReference ?? typeName), node);

			var arguments = node.GetArguments(throwOnError: true);
			var argumentExpressions = new Expression[arguments.Count];
			for (var i = 0; i < arguments.Count; i++)
			{
				var argument = default(SyntaxTreeNode);
				if (arguments.TryGetValue(i, out argument) == false)
					throw new ExpressionParserException(Properties.Resources.EXCEPTION_BOUNDEXPR_ARGSDOESNTMATCHPARAMS, node);
				argumentExpressions[i] = Build(argument, context, typeHint: typeof(int));
			}

			return Expression.NewArrayBounds(type, argumentExpressions);
			*/
		}
	}
}
