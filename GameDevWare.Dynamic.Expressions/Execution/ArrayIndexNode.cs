using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class ArrayIndexNode : ExecutionNode
	{
		private readonly Expression expression;
		private readonly ExecutionNode targetNode;
		private readonly ExecutionNode indexNode;
		private readonly CallNode methodCallNode;

		public ArrayIndexNode(Expression expression, ConstantExpression[] constExpressions, ParameterExpression[] parameterExpressions)
		{
			if (expression == null) throw new ArgumentNullException("expression");
			if (constExpressions == null) throw new ArgumentNullException("constExpressions");
			if (parameterExpressions == null) throw new ArgumentNullException("parameterExpressions");

			var binaryExpression = expression as BinaryExpression;
			var methodCallExpression = expression as MethodCallExpression;
			if (binaryExpression != null)
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
			if (this.methodCallNode != null)
			{
				return this.methodCallNode.Run(closure);
			}
			else
			{
				var target = closure.Unbox<Array>(this.targetNode.Run(closure));

				if (target == null)
					throw new NullReferenceException(string.Format(Properties.Resources.EXCEPTION_EXECUTION_EXPRESSIONGIVESNULLRESULT, this.expression));

				var index = this.indexNode.Run(closure);
				return closure.Is<int[]>(index)
					? target.GetValue(closure.Unbox<int[]>(index))
					: target.GetValue(closure.Unbox<int>(index));
			}
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.expression.ToString();
		}
	}
}
