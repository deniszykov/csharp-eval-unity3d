using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class TypeIsNode : ExecutionNode
	{
		private readonly TypeBinaryExpression typeBinaryExpression;
		private readonly ExecutionNode targetNode;

		public TypeIsNode(TypeBinaryExpression typeBinaryExpression, ConstantExpression[] constExpressions, ParameterExpression[] parameterExpressions)
		{
			if (typeBinaryExpression == null) throw new ArgumentNullException("typeBinaryExpression");
			if (constExpressions == null) throw new ArgumentNullException("constExpressions");
			if (parameterExpressions == null) throw new ArgumentNullException("parameterExpressions");

			this.typeBinaryExpression = typeBinaryExpression;

			this.targetNode = AotCompiler.Compile(typeBinaryExpression.Expression, constExpressions, parameterExpressions);
		}

		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			var target = closure.Unbox<object>(this.targetNode.Run(closure));
			if (target == null)
				return Constants.FalseObject;

			return this.typeBinaryExpression.TypeOperand.IsInstanceOfType(target) ? Constants.TrueObject : Constants.FalseObject;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.typeBinaryExpression.ToString();
		}
	}
}
