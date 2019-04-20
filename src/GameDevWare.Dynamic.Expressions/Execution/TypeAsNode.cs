using System;
using System.Linq.Expressions;
using GameDevWare.Dynamic.Expressions.Binding;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class TypeAsNode : ExecutionNode
	{
		private readonly UnaryExpression typeAsExpression;
		private readonly ConvertNode convertNode;
		private readonly ExecutionNode targetNode;
		private readonly TypeDescription targetType;

		public TypeAsNode(UnaryExpression typeAsExpression, ConstantExpression[] constExpressions, ParameterExpression[] parameterExpressions)
		{
			if (typeAsExpression == null) throw new ArgumentNullException("typeAsExpression");
			if (constExpressions == null) throw new ArgumentNullException("constExpressions");
			if (parameterExpressions == null) throw new ArgumentNullException("parameterExpressions");

			this.typeAsExpression = typeAsExpression;

			this.targetType = TypeDescription.GetTypeDescription(typeAsExpression.Type);
			if (this.targetType.IsValueType)
				this.convertNode = new ConvertNode(typeAsExpression, constExpressions, parameterExpressions);
			else
				this.targetNode = AotCompiler.Compile(typeAsExpression.Operand, constExpressions, parameterExpressions);
		}

		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			if (this.convertNode != null)
				return this.convertNode.Run(closure);

			var target = closure.Unbox<object>(this.targetNode.Run(closure));
			if (target == null)
				return null;

			if (this.targetType.IsAssignableFrom(target.GetType()) == false)
				return null;

			return target;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.typeAsExpression.ToString();
		}
	}
}
