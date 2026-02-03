using System;
using System.Linq.Expressions;
using GameDevWare.Dynamic.Expressions.Binding;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class TypeIsNode : ExecutionNode
	{
		private readonly ExecutionNode targetNode;
		private readonly TypeDescription targetType;
		private readonly TypeBinaryExpression typeBinaryExpression;

		public TypeIsNode(TypeBinaryExpression typeBinaryExpression, ConstantExpression[] constExpressions, ParameterExpression[] parameterExpressions)
		{
			if (typeBinaryExpression == null) throw new ArgumentNullException(nameof(typeBinaryExpression));
			if (constExpressions == null) throw new ArgumentNullException(nameof(constExpressions));
			if (parameterExpressions == null) throw new ArgumentNullException(nameof(parameterExpressions));

			this.typeBinaryExpression = typeBinaryExpression;
			this.targetType = TypeDescription.GetTypeDescription(this.typeBinaryExpression.TypeOperand);
			this.targetNode = AotCompiler.Compile(typeBinaryExpression.Expression, constExpressions, parameterExpressions);
		}

		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			var target = closure.Unbox<object>(this.targetNode.Run(closure));
			if (target == null)
			{
				return Constants.FalseObject;
			}

			return this.targetType.IsAssignableFrom(target.GetType()) ? Constants.TrueObject : Constants.FalseObject;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.typeBinaryExpression.ToString();
		}
	}
}
