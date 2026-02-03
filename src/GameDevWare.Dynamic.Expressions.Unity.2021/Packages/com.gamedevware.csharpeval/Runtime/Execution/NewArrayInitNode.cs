using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class NewArrayInitNode : ExecutionNode
	{
		private readonly ExecutionNode[] initializationValueNodes;
		private readonly NewArrayExpression newArrayExpression;

		public NewArrayInitNode(NewArrayExpression newArrayExpression, ConstantExpression[] constExpressions, ParameterExpression[] parameterExpressions)
		{
			if (newArrayExpression == null) throw new ArgumentNullException(nameof(newArrayExpression));
			if (constExpressions == null) throw new ArgumentNullException(nameof(constExpressions));
			if (parameterExpressions == null) throw new ArgumentNullException(nameof(parameterExpressions));

			this.newArrayExpression = newArrayExpression;

			this.initializationValueNodes = new ExecutionNode[newArrayExpression.Expressions.Count];
			for (var i = 0; i < this.initializationValueNodes.Length; i++)
			{
				this.initializationValueNodes[i] = AotCompiler.Compile(newArrayExpression.Expressions[i], constExpressions, parameterExpressions);
			}
		}

		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			// ReSharper disable once AssignNullToNotNullAttribute
			var array = Array.CreateInstance(this.newArrayExpression.Type.GetElementType(), this.initializationValueNodes.Length);
			for (var i = 0; i < this.initializationValueNodes.Length; i++)
			{
				var initializationValue = this.initializationValueNodes[i].Run(closure);
				array.SetValue(closure.Unbox<object>(initializationValue), i);
			}

			return array;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.newArrayExpression.ToString();
		}
	}
}
