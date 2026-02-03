using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class ConditionalNode : ExecutionNode
	{
		private readonly ConditionalExpression conditionalExpression;
		private readonly ExecutionNode conditionTestNode;
		private readonly ExecutionNode falseBranchNode;
		private readonly ExecutionNode trueBranchNode;

		public ConditionalNode(ConditionalExpression conditionalExpression, ConstantExpression[] constExpressions, ParameterExpression[] parameterExpressions)
		{
			if (conditionalExpression == null) throw new ArgumentNullException(nameof(conditionalExpression));
			if (constExpressions == null) throw new ArgumentNullException(nameof(constExpressions));
			if (parameterExpressions == null) throw new ArgumentNullException(nameof(parameterExpressions));

			this.conditionalExpression = conditionalExpression;

			this.trueBranchNode = AotCompiler.Compile(conditionalExpression.IfTrue, constExpressions, parameterExpressions);
			this.falseBranchNode = AotCompiler.Compile(conditionalExpression.IfFalse, constExpressions, parameterExpressions);
			this.conditionTestNode = AotCompiler.Compile(conditionalExpression.Test, constExpressions, parameterExpressions);
		}

		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			var testValue = closure.Unbox<bool>(this.conditionTestNode.Run(closure));
			var value = testValue ? this.trueBranchNode.Run(closure) : this.falseBranchNode.Run(closure);
			return value;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.conditionalExpression.ToString();
		}
	}
}
