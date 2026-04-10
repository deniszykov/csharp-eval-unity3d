using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal abstract class ExecutionNode
	{
		public const int LOCAL_FIRST_PARAMETER = 3; // this is offset of first parameter in Closure locals
		public const int LOCAL_OPERAND1 = 0;
		public const int LOCAL_OPERAND2 = 1;
		public const int LOCAL_SLOT1 = 2;

		public abstract object Run(Closure closure);

		/// <summary>
		///     Writes specified <paramref name="value" /> back to the expression's source.
		///     Used for propagating <c>out</c> and <c>ref</c> parameters.
		/// </summary>
		/// <param name="closure">Current execution closure. Not null.</param>
		/// <param name="value">Value to write back.</param>
		/// <returns>True if value was written back; otherwise, false.</returns>
		public virtual bool WriteBack(Closure closure, object value)
		{
			return false;
		}
		protected static bool IsNullable(Expression expression)
		{
			if (expression == null) throw new ArgumentException("expression");

			if (expression is ConstantExpression constantExpression && constantExpression.Type == typeof(object) && constantExpression.Value == null)
			{
				return true;
			}

			return IsNullable(expression.Type);
		}
		protected static bool IsNullable(Type type)
		{
			if (type == null) throw new ArgumentException("type");

			return Nullable.GetUnderlyingType(type) != null;
		}
	}
}
