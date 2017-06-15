using System.Collections.Generic;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class ConstantsCollector : ExpressionVisitor
	{
		public readonly List<ConstantExpression> Constants = new List<ConstantExpression>();

		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			this.Constants.Add(constantExpression);
			return constantExpression;
		}
	}
}
