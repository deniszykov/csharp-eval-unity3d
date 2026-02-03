using System;
using System.Linq.Expressions;
using GameDevWare.Dynamic.Expressions.Properties;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class ArrayLengthNode : ExecutionNode
	{
		private readonly ExecutionNode targetNode;
		private readonly UnaryExpression unaryExpression;

		public ArrayLengthNode(UnaryExpression unaryExpression, ConstantExpression[] constExpressions, ParameterExpression[] parameterExpressions)
		{
			this.unaryExpression = unaryExpression;
			if (unaryExpression == null) throw new ArgumentNullException(nameof(unaryExpression));
			if (constExpressions == null) throw new ArgumentNullException(nameof(constExpressions));
			if (parameterExpressions == null) throw new ArgumentNullException(nameof(parameterExpressions));

			this.targetNode = AotCompiler.Compile(unaryExpression.Operand, constExpressions, parameterExpressions);
		}

		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			var target = closure.Unbox<Array>(this.targetNode.Run(closure));
			if (target == null)
				throw new NullReferenceException(string.Format(Resources.EXCEPTION_EXECUTION_EXPRESSIONGIVESNULLRESULT, this.unaryExpression.Operand));

			return closure.Box(target.Length);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.unaryExpression.ToString();
		}
	}
}
