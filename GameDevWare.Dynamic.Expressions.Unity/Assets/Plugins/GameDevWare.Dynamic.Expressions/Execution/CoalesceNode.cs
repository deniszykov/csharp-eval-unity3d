using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class CoalesceNode : ExecutionNode
	{
		private readonly BinaryExpression binaryExpression;
		private readonly ExecutionNode leftNode;
		private readonly ExecutionNode rightNode;

		public CoalesceNode(BinaryExpression binaryExpression, ConstantExpression[] constExpressions, ParameterExpression[] parameterExpressions)
		{
			if (binaryExpression == null) throw new ArgumentNullException("binaryExpression");
			if (constExpressions == null) throw new ArgumentNullException("constExpressions");
			if (parameterExpressions == null) throw new ArgumentNullException("parameterExpressions");

			this.binaryExpression = binaryExpression;
			this.leftNode = AotCompiler.Compile(binaryExpression.Left, constExpressions, parameterExpressions);
			this.rightNode = AotCompiler.Compile(binaryExpression.Right, constExpressions, parameterExpressions);
		}
		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			var left = closure.Unbox<object>(this.leftNode.Run(closure));
			if (left != null)
				return left;
			else
				return this.rightNode.Run(closure);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.binaryExpression.ToString();
		}
	}
}