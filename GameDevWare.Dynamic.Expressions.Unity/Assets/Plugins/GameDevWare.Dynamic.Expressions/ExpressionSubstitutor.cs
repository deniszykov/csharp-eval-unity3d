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
			if (substitutions == null) throw new ArgumentNullException("substitutions");

			this.substitutions = substitutions;
		}
		protected override Expression VisitParameter(ParameterExpression parameterExpression)
		{
			var substitutionParameter = default(Expression);
			if (this.substitutions.TryGetValue(parameterExpression, out substitutionParameter))
				return substitutionParameter;
			return base.VisitParameter(parameterExpression);
		}
		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			var substitutionParameter = default(Expression);
			if (this.substitutions.TryGetValue(constantExpression, out substitutionParameter))
				return substitutionParameter;
			return base.VisitConstant(constantExpression);
		}

		public static Expression Visit(Expression expression, Dictionary<Expression, Expression> substitutions)
		{
			if (expression == null) throw new ArgumentNullException("expression");
			if (substitutions == null) throw new ArgumentNullException("substitutions");

			var substitutor = new ExpressionSubstitutor(substitutions);
			return substitutor.Visit(expression);
		}
	}

}
