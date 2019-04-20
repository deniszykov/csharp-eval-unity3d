using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class CallNode : ExecutionNode
	{
		private readonly MethodCallExpression methodCallExpression;
		private readonly ExecutionNode targetNode;
		private readonly ExecutionNode[] argumentNodes;
		private readonly FastCall.Invoker fastCallInvoker;
		private readonly bool isStatic;

		public CallNode(MethodCallExpression methodCallExpression, ConstantExpression[] constExpressions, ParameterExpression[] parameterExpressions)
		{
			if (methodCallExpression == null) throw new ArgumentNullException("methodCallExpression");
			if (constExpressions == null) throw new ArgumentNullException("constExpressions");
			if (parameterExpressions == null) throw new ArgumentNullException("parameterExpressions");

			this.methodCallExpression = methodCallExpression;
			this.targetNode = AotCompiler.Compile(methodCallExpression.Object, constExpressions, parameterExpressions);
			this.argumentNodes = new ExecutionNode[methodCallExpression.Arguments.Count];
			for (var i = 0; i < this.argumentNodes.Length; i++)
				this.argumentNodes[i] = AotCompiler.Compile(methodCallExpression.Arguments[i], constExpressions, parameterExpressions);
			this.fastCallInvoker = FastCall.TryCreate(methodCallExpression.Method);
			this.isStatic = this.methodCallExpression.Method.IsStatic;
		}

		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			if (this.fastCallInvoker != null)
			{
				return this.fastCallInvoker(closure, this.argumentNodes);
			}
			else
			{
				var target = this.targetNode.Run(closure);

				if (this.isStatic == false && target == null)
					throw new NullReferenceException(string.Format(Properties.Resources.EXCEPTION_EXECUTION_EXPRESSIONGIVESNULLRESULT, this.methodCallExpression.Object));

				var arguments = new object[this.argumentNodes.Length];
				for (var i = 0; i < arguments.Length; i++)
					arguments[i] = closure.Unbox<object>(this.argumentNodes[i].Run(closure));

				return this.methodCallExpression.Method.Invoke(target, arguments);
			}
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.methodCallExpression.ToString();
		}
	}
}
