using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class ParameterNode : ExecutionNode
	{
		private readonly ParameterExpression parameterExpression;
		private readonly int parameterIndex;

		public ParameterNode(ParameterExpression parameterExpression, ParameterExpression[] parameterExpressions)
		{
			if (parameterExpression == null) throw new ArgumentNullException("parameterExpression");
			if (parameterExpressions == null) throw new ArgumentNullException("parameterExpressions");

			this.parameterExpression = parameterExpression;
			this.parameterIndex = Array.IndexOf(parameterExpressions, parameterExpression);

			if (this.parameterIndex < 0)
				throw new ArgumentException("Parameter expression is not found in passed parameter list.", "parameterExpression");
		}
		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			var parameter = closure.Locals[LOCAL_FIRST_PARAMETER + this.parameterIndex];
			return parameter;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.parameterExpression.ToString();
		}
	}
}
