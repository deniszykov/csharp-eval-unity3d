using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class ListInitNode : ExecutionNode
	{
		private readonly KeyValuePair<MethodInfo, ExecutionNode[]>[] initializationNodes;
		private readonly ListInitExpression listInitExpression;
		private readonly NewNode newNode;

		public ListInitNode(ListInitExpression listInitExpression, ConstantExpression[] constExpressions, ParameterExpression[] parameterExpressions)
		{
			if (listInitExpression == null) throw new ArgumentNullException(nameof(listInitExpression));
			if (constExpressions == null) throw new ArgumentNullException(nameof(constExpressions));
			if (parameterExpressions == null) throw new ArgumentNullException(nameof(parameterExpressions));

			this.listInitExpression = listInitExpression;

			this.newNode = new NewNode(listInitExpression.NewExpression, constExpressions, parameterExpressions);
			this.initializationNodes = new KeyValuePair<MethodInfo, ExecutionNode[]>[listInitExpression.Initializers.Count];
			for (var i = 0; i < this.initializationNodes.Length; i++)
			{
				var initialization = listInitExpression.Initializers[i];
				var argumentNodes = new ExecutionNode[initialization.Arguments.Count];
				for (var a = 0; a < initialization.Arguments.Count; a++)
				{
					argumentNodes[a] = AotCompiler.Compile(initialization.Arguments[a], constExpressions, parameterExpressions);
				}
				this.initializationNodes[i] = new KeyValuePair<MethodInfo, ExecutionNode[]>(initialization.AddMethod, argumentNodes);
			}
		}

		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			var list = closure.Unbox<object>(this.newNode.Run(closure));

			if (this.initializationNodes.Length == 0)
				return list;

			foreach (var listInit in this.initializationNodes)
			{
				var addMethod = listInit.Key;
				var argumentNodes = listInit.Value;
				var addArguments = new object[argumentNodes.Length];

				for (var i = 0; i < argumentNodes.Length; i++)
					addArguments[i] = closure.Unbox<object>(argumentNodes[i].Run(closure));

				addMethod.Invoke(list, addArguments);
			}

			return list;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.listInitExpression.ToString();
		}
	}
}
