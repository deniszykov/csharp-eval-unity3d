using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class ConvertNode : ExecutionNode
	{
		private readonly UnaryExpression convertExpression;
		private readonly ExecutionNode operandNode;
		private readonly Intrinsic.UnaryOperation operation;
		private readonly Type targetType;
		private readonly bool isTargetTypeNullable;
		private readonly Type sourceType;
		private readonly bool isSourceTypeNullable;

		public ConvertNode(UnaryExpression convertExpression, ConstantExpression[] constExpressions, ParameterExpression[] parameterExpressions)
		{
			this.convertExpression = convertExpression;
			this.operandNode = AotCompiler.Compile(convertExpression.Operand, constExpressions, parameterExpressions);
			this.operation = Intrinsic.WrapUnaryOperation(convertExpression.Method) ?? Intrinsic.WrapUnaryOperation(
				convertExpression.Type
					.GetMethods(BindingFlags.Public | BindingFlags.Static)
					.FirstOrDefault(m =>
						(string.Equals(m.Name, "op_Explicit", StringComparison.Ordinal) || string.Equals(m.Name, "op_Implicit", StringComparison.Ordinal)) &&
						m.ReturnType == convertExpression.Type &&
						m.GetParameters().Length == 1 &&
						m.GetParameters()[0].ParameterType == convertExpression.Operand.Type)
			);
			this.targetType = Nullable.GetUnderlyingType(convertExpression.Type) ?? convertExpression.Type;
			this.isTargetTypeNullable = IsNullable(convertExpression.Type);
			this.sourceType = Nullable.GetUnderlyingType(convertExpression.Operand.Type) ?? convertExpression.Operand.Type;
			this.isSourceTypeNullable = IsNullable(convertExpression.Operand);
		}

		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			var operand = closure.Unbox<object>(this.operandNode.Run(closure));
			if (operand == null && (this.convertExpression.Type.IsValueType == false || this.isTargetTypeNullable))
				return null;

			var operandType = closure.GetType(operand);
			var convertType = this.convertExpression.NodeType;
			if (convertType != ExpressionType.Convert)
				convertType = ExpressionType.ConvertChecked;

			// un-box
			if ((this.sourceType == typeof(object) || this.sourceType == typeof(ValueType) || this.sourceType.IsInterface) && this.targetType.IsValueType)
			{
				// null un-box
				if (operand == null) throw new NullReferenceException(string.Format(Properties.Resources.EXCEPTION_EXECUTION_EXPRESSIONGIVESNULLRESULT, this.convertExpression.Operand));
				// type check for un-box
				if (operandType == this.targetType)
					return operand;
				throw new InvalidCastException();
			}
			// box
			else if (this.sourceType.IsValueType && (this.targetType == typeof(object) || this.targetType == typeof(ValueType) || this.targetType.IsInterface))
			{
				// type check for box
				return this.targetType.IsAssignableFrom(operandType) ? operand : null;
			}
			// to enum
			else if (this.targetType.IsEnum && (this.sourceType == typeof(byte) ||
				this.sourceType == typeof(sbyte) ||
				this.sourceType == typeof(short) ||
				this.sourceType == typeof(ushort) ||
				this.sourceType == typeof(int) ||
				this.sourceType == typeof(uint) ||
				this.sourceType == typeof(long) ||
				this.sourceType == typeof(ulong)))
			{
				if (operand == null) throw new NullReferenceException(string.Format(Properties.Resources.EXCEPTION_EXECUTION_EXPRESSIONGIVESNULLRESULT, this.convertExpression.Operand));

				operand = Intrinsic.InvokeConversion(closure, operand, Enum.GetUnderlyingType(this.targetType), this.convertExpression.NodeType, null);
				return Enum.ToObject(this.targetType, closure.Unbox<object>(operand));
			}
			// from enum
			else if (this.sourceType.IsEnum && (this.targetType == typeof(byte) ||
				this.targetType == typeof(sbyte) ||
				this.targetType == typeof(short) ||
				this.targetType == typeof(ushort) ||
				this.targetType == typeof(int) ||
				this.targetType == typeof(uint) ||
				this.targetType == typeof(long) ||
				this.targetType == typeof(ulong)))
			{
				if (operand == null)
					throw new NullReferenceException(string.Format(Properties.Resources.EXCEPTION_EXECUTION_EXPRESSIONGIVESNULLRESULT, this.convertExpression.Operand));

				operand = Convert.ChangeType(closure.Unbox<object>(operand), Enum.GetUnderlyingType(this.sourceType));
				operand = Intrinsic.InvokeConversion(closure, operand, this.targetType, this.convertExpression.NodeType, null);
				return operand;
			}
			// from nullable
			if (this.targetType.IsValueType && this.isSourceTypeNullable)
			{
				if (operand == null) throw new NullReferenceException(string.Format(Properties.Resources.EXCEPTION_EXECUTION_EXPRESSIONGIVESNULLRESULT, this.convertExpression.Operand));

				operand = Intrinsic.InvokeConversion(closure, operand, this.targetType, this.convertExpression.NodeType, null);
			}
			else if (this.targetType.IsAssignableFrom(operandType))
				return operand;

			return Intrinsic.InvokeConversion(closure, operand, this.targetType, convertType, this.operation);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.convertExpression.ToString();
		}
	}
}
