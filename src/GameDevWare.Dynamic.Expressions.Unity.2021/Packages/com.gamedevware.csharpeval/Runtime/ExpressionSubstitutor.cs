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
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions
{
	internal class ExpressionSubstitutor : ExpressionVisitor
	{
		private readonly Dictionary<Expression, Expression> substitutions;

		private ExpressionSubstitutor(Dictionary<Expression, Expression> substitutions)
		{
			if (substitutions == null) throw new ArgumentNullException(nameof(substitutions));

			this.substitutions = substitutions;
		}
		protected override Expression VisitParameter(ParameterExpression parameterExpression)
		{
			if (this.substitutions.TryGetValue(parameterExpression, out var substitutionParameter))
				return substitutionParameter;

			return base.VisitParameter(parameterExpression);
		}
		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			if (this.substitutions.TryGetValue(constantExpression, out var substitutionParameter))
				return substitutionParameter;

			return base.VisitConstant(constantExpression);
		}

		public static Expression Visit(Expression expression, Dictionary<Expression, Expression> substitutions)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));
			if (substitutions == null) throw new ArgumentNullException(nameof(substitutions));

			var substitutor = new ExpressionSubstitutor(substitutions);
			return substitutor.Visit(expression);
		}
	}
}
