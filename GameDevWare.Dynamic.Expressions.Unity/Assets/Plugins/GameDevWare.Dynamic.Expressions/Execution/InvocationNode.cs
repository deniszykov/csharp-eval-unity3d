using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class InvocationNode : ExecutionNode
	{
		private readonly InvocationExpression invocationExpression;
		private readonly ExecutionNode target;
		private readonly ExecutionNode[] argumentNodes;

		public InvocationNode(InvocationExpression invocationExpression, ConstantExpression[] constExpressions, ParameterExpression[] parameterExpressions)
		{
			if (invocationExpression == null) throw new ArgumentNullException("invocationExpression");
			if (constExpressions == null) throw new ArgumentNullException("constExpressions");
			if (parameterExpressions == null) throw new ArgumentNullException("parameterExpressions");

			this.invocationExpression = invocationExpression;
			this.target = AotCompiler.Compile(invocationExpression.Expression, constExpressions, parameterExpressions);
			this.argumentNodes = new ExecutionNode[invocationExpression.Arguments.Count];
			for (var i = 0; i < this.argumentNodes.Length; i++)
				this.argumentNodes[i] = AotCompiler.Compile(invocationExpression.Arguments[i], constExpressions, parameterExpressions);
		}

		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			var targetDelegate = closure.Unbox<Delegate>(this.target.Run(closure));
			var invokeArguments = new object[this.argumentNodes.Length];
			for (var i = 0; i < invokeArguments.Length; i++)
				invokeArguments[i] = closure.Unbox<object>(this.argumentNodes[i].Run(closure));

			if (targetDelegate == null)
				throw new NullReferenceException(string.Format(Properties.Resources.EXCEPTION_EXECUTION_EXPRESSIONGIVESNULLRESULT, this.invocationExpression.Expression));

			return targetDelegate.DynamicInvoke(invokeArguments);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.invocationExpression.ToString();
		}
	}
}
