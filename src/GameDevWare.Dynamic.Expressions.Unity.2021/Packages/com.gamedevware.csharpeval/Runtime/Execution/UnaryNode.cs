using System;
using System.Linq.Expressions;
using GameDevWare.Dynamic.Expressions.Properties;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class UnaryNode : ExecutionNode
	{
		private readonly bool isNullable;
		private readonly ExecutionNode operandNode;
		private readonly Intrinsic.UnaryOperation operation;
		private readonly UnaryExpression unaryExpression;

		private UnaryNode
		(
			UnaryExpression unaryExpression,
			ConstantExpression[] constExpressions,
			ParameterExpression[] parameterExpressions,
			string unaryOperationMethodName)
		{
			this.unaryExpression = unaryExpression;
			if (unaryExpression == null) throw new ArgumentNullException(nameof(unaryExpression));
			if (constExpressions == null) throw new ArgumentNullException(nameof(constExpressions));
			if (parameterExpressions == null) throw new ArgumentNullException(nameof(parameterExpressions));

			this.operandNode = AotCompiler.Compile(unaryExpression.Operand, constExpressions, parameterExpressions);
			this.isNullable = IsNullable(unaryExpression.Operand);
			this.operation = Intrinsic.WrapUnaryOperation(unaryExpression.Method) ??
				(unaryOperationMethodName == null ? null : Intrinsic.WrapUnaryOperation(unaryExpression.Operand.Type, unaryOperationMethodName));
		}

		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			var operand = this.operandNode.Run(closure);

			if (this.isNullable && operand == null)
				return null;

			return Intrinsic.InvokeUnaryOperation(closure, operand, this.unaryExpression.NodeType, this.operation);
		}

		public static ExecutionNode Create(UnaryExpression unaryExpression, ConstantExpression[] constExpressions, ParameterExpression[] parameterExpressions)
		{
			// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
			switch (unaryExpression.NodeType)
			{
				case ExpressionType.UnaryPlus: return new UnaryNode(unaryExpression, constExpressions, parameterExpressions, "op_UnaryPlus");
				case ExpressionType.Negate:
				case ExpressionType.NegateChecked: return new UnaryNode(unaryExpression, constExpressions, parameterExpressions, "op_UnaryNegation");
				case ExpressionType.Not: return new UnaryNode(unaryExpression, constExpressions, parameterExpressions, "op_OnesComplement");
				default: throw new InvalidOperationException(string.Format(Resources.EXCEPTION_COMPIL_UNKNOWNEXPRTYPE, unaryExpression.Type));
			}
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.unaryExpression.ToString();
		}
	}
}
