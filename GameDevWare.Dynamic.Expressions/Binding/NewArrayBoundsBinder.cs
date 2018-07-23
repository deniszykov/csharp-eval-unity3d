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

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class NewArrayBoundsBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (bindingContext == null) throw new ArgumentNullException("bindingContext");
			if (expectedType == null) throw new ArgumentNullException("expectedType");

			boundExpression = null;
			bindingError = null;

			var typeName = node.GetTypeName(throwOnError: true);
			var type = default(Type);
			if (bindingContext.TryResolveType(typeName, out type) == false)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVETYPE, typeName), node);
				return false;
			}

			var typeDescription = TypeDescription.GetTypeDescription(type);
			var elementType = typeDescription.UnderlyingType;
			var indexTypeDescription = TypeDescription.Int32Type;
			var arguments = node.GetArguments(throwOnError: true);
			var argumentExpressions = new Expression[arguments.Count];
			for (var i = 0; i < arguments.Count; i++)
			{
				var argument = default(SyntaxTreeNode);
				if (arguments.TryGetValue(i, out argument) == false)
				{
					bindingError = new ExpressionParserException(Properties.Resources.EXCEPTION_BOUNDEXPR_ARGSDOESNTMATCHPARAMS, node);
					return false;
				}

				if (AnyBinder.TryBindInNewScope(argument, bindingContext, indexTypeDescription, out argumentExpressions[i], out bindingError) == false)
					return false;

				Debug.Assert(argumentExpressions[i] != null, "argumentExpressions[i] != null");
			}

			boundExpression = Expression.NewArrayBounds(elementType, argumentExpressions);
			return true;
		}
	}
}
