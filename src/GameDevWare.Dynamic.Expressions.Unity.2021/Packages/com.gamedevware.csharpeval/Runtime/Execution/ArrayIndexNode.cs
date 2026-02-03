using System;
using System.Linq.Expressions;
using GameDevWare.Dynamic.Expressions.Properties;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class ArrayIndexNode : ExecutionNode
	{
		private readonly Expression expression;
		private readonly ExecutionNode indexNode;
		private readonly CallNode methodCallNode;
		private readonly ExecutionNode targetNode;

		public ArrayIndexNode(Expression expression, ConstantExpression[] constExpressions, ParameterExpression[] parameterExpressions)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));
			if (constExpressions == null) throw new ArgumentNullException(nameof(constExpressions));
			if (parameterExpressions == null) throw new ArgumentNullException(nameof(parameterExpressions));

			var methodCallExpression = expression as MethodCallExpression;
			if (expression is BinaryExpression binaryExpression)
			{
				this.targetNode = AotCompiler.Compile(binaryExpression.Left, constExpressions, parameterExpressions);
				this.indexNode = AotCompiler.Compile(binaryExpression.Right, constExpressions, parameterExpressions);
			}
			else
			{
				this.methodCallNode = new CallNode(methodCallExpression, constExpressions, parameterExpressions);
			}

			this.expression = expression;
		}

		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			if (this.methodCallNode != null) return this.methodCallNode.Run(closure);

			var target = closure.Unbox<Array>(this.targetNode.Run(closure));

			if (target == null)
				throw new NullReferenceException(string.Format(Resources.EXCEPTION_EXECUTION_EXPRESSIONGIVESNULLRESULT, this.expression));

			var index = this.indexNode.Run(closure);
			return closure.Is<int[]>(index)
				? target.GetValue(closure.Unbox<int[]>(index))
				: target.GetValue(closure.Unbox<int>(index));
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.expression.ToString();
		}
	}
}
