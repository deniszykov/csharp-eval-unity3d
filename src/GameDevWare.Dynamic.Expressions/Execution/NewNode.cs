using System;
using System.Linq.Expressions;
using GameDevWare.Dynamic.Expressions.Properties;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class NewNode : ExecutionNode
	{
		private static readonly object[] EmptyArguments = Array.Empty<object>();
		private readonly int constructorParametersCount;
		private readonly ExecutionNode[] initializationValueNodes;
		private readonly bool isNullableType;

		private readonly NewExpression newExpression;

		public NewNode(NewExpression newExpression, ConstantExpression[] constExpressions, ParameterExpression[] parameterExpressions)
		{
			if (newExpression == null) throw new ArgumentNullException(nameof(newExpression));
			if (constExpressions == null) throw new ArgumentNullException(nameof(constExpressions));
			if (parameterExpressions == null) throw new ArgumentNullException(nameof(parameterExpressions));

			this.newExpression = newExpression;

			this.initializationValueNodes = new ExecutionNode[newExpression.Arguments.Count];
			for (var i = 0; i < this.initializationValueNodes.Length; i++)
			{
				this.initializationValueNodes[i] = AotCompiler.Compile(newExpression.Arguments[i], constExpressions, parameterExpressions);
			}
			this.constructorParametersCount = newExpression.Constructor?.GetParameters().Length ?? 0;
			this.isNullableType = IsNullable(newExpression.Type);
		}

		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			var initializationValues = new object[this.initializationValueNodes.Length];
			for (var i = 0; i < initializationValues.Length; i++)
				initializationValues[i] = closure.Unbox<object>(this.initializationValueNodes[i].Run(closure));

			var constructorArguments = EmptyArguments;
			if (this.constructorParametersCount > 0)
			{
				constructorArguments = new object[this.constructorParametersCount];
				Array.Copy(initializationValues, constructorArguments, this.constructorParametersCount);
			}

			var newInstance = this.isNullableType ? null : Activator.CreateInstance(this.newExpression.Type, constructorArguments);
			if (newInstance == null)
				throw new NullReferenceException(string.Format(Resources.EXCEPTION_EXECUTION_EXPRESSIONGIVESNULLRESULT, this.newExpression));

			return newInstance;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.newExpression.ToString();
		}
	}
}
