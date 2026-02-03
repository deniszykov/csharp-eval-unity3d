using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class BinaryNode : ExecutionNode
	{
		private readonly BinaryExpression binaryExpression;
		private readonly ExecutionNode leftNode;
		private readonly ExecutionNode rightNode;
		private readonly bool isNullable;
		private readonly Intrinsic.BinaryOperation operation;
		private readonly object shortcutLeftValue;

		private BinaryNode
		(
			BinaryExpression binaryExpression,
			ConstantExpression[] constExpressions,
			ParameterExpression[] parameterExpressions,
			string binaryOperationMethodName)
		{
			if (binaryExpression == null) throw new ArgumentNullException(nameof(binaryExpression));
			if (constExpressions == null) throw new ArgumentNullException(nameof(constExpressions));
			if (parameterExpressions == null) throw new ArgumentNullException(nameof(parameterExpressions));

			this.binaryExpression = binaryExpression;
			this.leftNode = AotCompiler.Compile(binaryExpression.Left, constExpressions, parameterExpressions);
			this.rightNode = AotCompiler.Compile(binaryExpression.Right, constExpressions, parameterExpressions);
			this.operation = Intrinsic.WrapBinaryOperation(binaryExpression.Method) ??
				(binaryOperationMethodName == null ? null : Intrinsic.WrapBinaryOperation(binaryExpression.Left.Type, binaryOperationMethodName));
			this.isNullable = IsNullable(binaryExpression.Left) || IsNullable(binaryExpression.Right);
			this.shortcutLeftValue = binaryExpression.NodeType == ExpressionType.OrElse ? Constants.TrueObject :
				binaryExpression.NodeType == ExpressionType.AndAlso ? Constants.FalseObject : null;
		}

		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			var left = this.leftNode.Run(closure);

			// shortcut for and-also(&&) and or-else(||)
			if (this.shortcutLeftValue != null && Equals(this.shortcutLeftValue, closure.Unbox<object>(left)))
				return this.shortcutLeftValue;

			var right = this.rightNode.Run(closure);

			if (this.isNullable && (left == null || right == null))
			{
				left = closure.Unbox<object>(left);
				right = closure.Unbox<object>(right);

				// ReSharper disable once SwitchStatementMissingSomeCases
				switch (this.binaryExpression.NodeType)
				{
					case ExpressionType.Equal: return ReferenceEquals(left, right) ? Constants.TrueObject : Constants.FalseObject;
					case ExpressionType.NotEqual: return ReferenceEquals(left, right) ? Constants.FalseObject : Constants.TrueObject;
					case ExpressionType.GreaterThan:
					case ExpressionType.GreaterThanOrEqual:
					case ExpressionType.LessThan:
					case ExpressionType.LessThanOrEqual: return Constants.FalseObject;

					// C# Specs -> 7.11.4 Nullable boolean logical operators
					case ExpressionType.And:
						if (Equals(left, Constants.FalseObject) || Equals(right, Constants.FalseObject))
							return Constants.FalseObject;

						goto default;
					case ExpressionType.Or:
						if (Equals(left, Constants.TrueObject) || Equals(right, Constants.TrueObject))
							return Constants.TrueObject;

						goto default;
					default:
						return null;
				}
			}

			return Intrinsic.InvokeBinaryOperation(closure, left, right, this.binaryExpression.NodeType, this.operation);
		}

		public static ExecutionNode Create(BinaryExpression unaryExpression, ConstantExpression[] constExpressions, ParameterExpression[] parameterExpressions)
		{
			if (unaryExpression == null) throw new ArgumentNullException(nameof(unaryExpression));
			if (constExpressions == null) throw new ArgumentNullException(nameof(constExpressions));
			if (parameterExpressions == null) throw new ArgumentNullException(nameof(parameterExpressions));

			const string NO_OPERATION = null;

			// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
			switch (unaryExpression.NodeType)
			{
				case ExpressionType.Add:
				case ExpressionType.AddChecked: return new BinaryNode(unaryExpression, constExpressions, parameterExpressions, "op_Addition");
				case ExpressionType.And: return new BinaryNode(unaryExpression, constExpressions, parameterExpressions, "op_BitwiseAnd");
				case ExpressionType.Divide: return new BinaryNode(unaryExpression, constExpressions, parameterExpressions, "op_Division");
				case ExpressionType.Equal: return new BinaryNode(unaryExpression, constExpressions, parameterExpressions, "op_Equality");
				case ExpressionType.ExclusiveOr: return new BinaryNode(unaryExpression, constExpressions, parameterExpressions, "op_ExclusiveOr");
				case ExpressionType.GreaterThan: return new BinaryNode(unaryExpression, constExpressions, parameterExpressions, "op_GreaterThan");
				case ExpressionType.GreaterThanOrEqual: return new BinaryNode(unaryExpression, constExpressions, parameterExpressions, "op_GreaterThanOrEqual");
				case ExpressionType.LessThan: return new BinaryNode(unaryExpression, constExpressions, parameterExpressions, "op_LessThan");
				case ExpressionType.LessThanOrEqual: return new BinaryNode(unaryExpression, constExpressions, parameterExpressions, "op_LessThanOrEqual");
				case ExpressionType.Modulo: return new BinaryNode(unaryExpression, constExpressions, parameterExpressions, "op_Modulus");
				case ExpressionType.Multiply:
				case ExpressionType.MultiplyChecked: return new BinaryNode(unaryExpression, constExpressions, parameterExpressions, "op_Multiply");
				case ExpressionType.NotEqual: return new BinaryNode(unaryExpression, constExpressions, parameterExpressions, "op_Inequality");
				case ExpressionType.Or: return new BinaryNode(unaryExpression, constExpressions, parameterExpressions, "op_BitwiseOr");
				case ExpressionType.Subtract:
				case ExpressionType.SubtractChecked: return new BinaryNode(unaryExpression, constExpressions, parameterExpressions, "op_Subtraction");
				case ExpressionType.AndAlso:
				case ExpressionType.LeftShift:
				case ExpressionType.OrElse:
				case ExpressionType.Power:
				case ExpressionType.RightShift: return new BinaryNode(unaryExpression, constExpressions, parameterExpressions, NO_OPERATION);
				default: throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_COMPIL_UNKNOWNEXPRTYPE, unaryExpression.Type));
			}
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.binaryExpression.ToString();
		}
	}
}
