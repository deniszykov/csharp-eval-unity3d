using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class NewArrayBoundsNode : ExecutionNode
	{
		private readonly NewArrayExpression newArrayExpression;
		private readonly ExecutionNode[] rankNodes;

		public NewArrayBoundsNode(NewArrayExpression newArrayExpression, ConstantExpression[] constExpressions, ParameterExpression[] parameterExpressions)
		{
			if (newArrayExpression == null) throw new ArgumentNullException("newArrayExpression");
			if (constExpressions == null) throw new ArgumentNullException("constExpressions");
			if (parameterExpressions == null) throw new ArgumentNullException("parameterExpressions");

			this.newArrayExpression = newArrayExpression;

			this.rankNodes = new ExecutionNode[newArrayExpression.Expressions.Count];
			for (var i = 0; i < this.rankNodes.Length; i++)
				this.rankNodes[i] = AotCompiler.Compile(newArrayExpression.Expressions[i], constExpressions, parameterExpressions);
		}

		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			var ranks = new int[this.rankNodes.Length];
			for (var i = 0; i < this.rankNodes.Length; i++)
				ranks[i] = closure.Unbox<int>(this.rankNodes[i].Run(closure));

			// ReSharper disable once AssignNullToNotNullAttribute
			var array = Array.CreateInstance(this.newArrayExpression.Type.GetElementType(), ranks);
			return array;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.newArrayExpression.ToString();
		}
	}
}
