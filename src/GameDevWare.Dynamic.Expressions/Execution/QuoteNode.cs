using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class QuoteNode : ExecutionNode
	{
		private readonly UnaryExpression unaryExpression;

		public QuoteNode(UnaryExpression unaryExpression)
		{
			if (unaryExpression == null) throw new ArgumentNullException(nameof(unaryExpression));

			this.unaryExpression = unaryExpression;
		}

		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			return this.unaryExpression.Operand;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.unaryExpression.ToString();
		}
	}
}
