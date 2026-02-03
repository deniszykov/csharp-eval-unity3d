using System;
using System.Linq.Expressions;
using System.Reflection;
using GameDevWare.Dynamic.Expressions.Properties;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class MemberAccessNode : ExecutionNode
	{
		private readonly FieldInfo fieldInfo;
		private readonly bool isStatic;
		private readonly MemberExpression memberExpression;
		private readonly MethodInfo propertyGetter;
		private readonly ExecutionNode targetNode;

		public MemberAccessNode(MemberExpression memberExpression, ConstantExpression[] constExpressions, ParameterExpression[] parameterExpressions)
		{
			if (memberExpression == null) throw new ArgumentNullException(nameof(memberExpression));
			if (constExpressions == null) throw new ArgumentNullException(nameof(constExpressions));
			if (parameterExpressions == null) throw new ArgumentNullException(nameof(parameterExpressions));

			this.memberExpression = memberExpression;

			this.targetNode = AotCompiler.Compile(memberExpression.Expression, constExpressions, parameterExpressions);

			var member = this.memberExpression.Member;
			this.fieldInfo = member as FieldInfo;

			var properFieldInfo = member as PropertyInfo;
			if (properFieldInfo != null)
				this.propertyGetter = properFieldInfo.GetAnyGetter();

			this.isStatic = this.fieldInfo != null ? this.fieldInfo.IsStatic : this.propertyGetter != null && this.propertyGetter.IsStatic;
		}

		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			var target = closure.Unbox<object>(this.targetNode.Run(closure));

			if (!this.isStatic && target == null)
			{
				throw new NullReferenceException(string.Format(Resources.EXCEPTION_EXECUTION_EXPRESSIONGIVESNULLRESULT,
					this.memberExpression.Expression));
			}

			if (this.fieldInfo != null) return this.fieldInfo.GetValue(target);

			if (this.propertyGetter != null) return this.propertyGetter.Invoke(target, null);

			throw new InvalidOperationException(string.Format(Resources.EXCEPTION_EXECUTION_INVALIDMEMBERFOREXPRESSION,
				this.memberExpression.Member));
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.memberExpression.ToString();
		}
	}
}
