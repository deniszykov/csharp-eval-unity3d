using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class ConstantNode : ExecutionNode
	{
		private readonly ConstantExpression constantExpression;
		private readonly int constantIndex;

		public ConstantNode(ConstantExpression constantExpression, ConstantExpression[] constExpressions)
		{
			if (constantExpression == null) throw new ArgumentNullException(nameof(constantExpression));
			if (constExpressions == null) throw new ArgumentNullException(nameof(constExpressions));

			this.constantExpression = constantExpression;
			this.constantIndex = Array.IndexOf(constExpressions, constantExpression);

			if (this.constantIndex < 0)
				throw new ArgumentException("Constant expression is not found in passed constant list.", nameof(constantExpression));
		}

		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			var constant = closure.Constants[this.constantIndex];
			return constant;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.constantExpression.ToString();
		}
	}
}
