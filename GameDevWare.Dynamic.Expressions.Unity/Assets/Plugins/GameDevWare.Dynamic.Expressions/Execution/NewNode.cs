using System;
using System.Linq.Expressions;
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class NewNode : ExecutionNode
	{
		private static readonly object[] EmptyArguments = new object[0];

		private readonly NewExpression newExpression;
		private readonly ExecutionNode[] initializationValueNodes;
		private readonly int constructorParametersCount;
		private readonly bool isNullableType;

		public NewNode(NewExpression newExpression, ConstantExpression[] constExpressions, ParameterExpression[] parameterExpressions)
		{
			if (newExpression == null) throw new ArgumentNullException("newExpression");
			if (constExpressions == null) throw new ArgumentNullException("constExpressions");
			if (parameterExpressions == null) throw new ArgumentNullException("parameterExpressions");

			this.newExpression = newExpression;

			this.initializationValueNodes = new ExecutionNode[newExpression.Arguments.Count];
			for (var i = 0; i < this.initializationValueNodes.Length; i++)
				this.initializationValueNodes[i] = AotCompiler.Compile(newExpression.Arguments[i], constExpressions, parameterExpressions);
			this.constructorParametersCount = newExpression.Constructor.GetParameters().Length;
			this.isNullableType = IsNullable(newExpression.Type);
		}

		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			var initializationValues = new object[this.initializationValueNodes.Length];
			for (var i = 0; i < initializationValues.Length; i++)
				initializationValues[i] = closure.Unbox<object>(this.initializationValueNodes[i].Run(closure));

			var constructorArguments = EmptyArguments;
			if (this.constructorParametersCount > 0)
			{
				constructorArguments = new object[this.constructorParametersCount];
				Array.Copy(initializationValues, constructorArguments, this.constructorParametersCount);
			}

			var newInstance = this.isNullableType ? null : Activator.CreateInstance(this.newExpression.Type, constructorArguments);
			if (this.newExpression.Members == null)
				return newInstance;

			if (newInstance == null)
				throw new NullReferenceException(string.Format(Properties.Resources.EXCEPTION_EXECUTION_EXPRESSIONGIVESNULLRESULT, this.newExpression));

			for (var j = 0; j < this.newExpression.Members.Count; j++)
			{
				var member = this.newExpression.Members[j];
				var fieldInfo = member as FieldInfo;
				var propertyInfo = member as PropertyInfo;
				var value = initializationValues[constructorArguments.Length + j];

				if (fieldInfo != null)
					fieldInfo.SetValue(newInstance, value);
				else if (propertyInfo != null)
					propertyInfo.SetValue(newInstance, value, null);
				else
					throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_EXECUTION_INVALIDMEMBERFOREXPRESSION, member));
			}

			return newInstance;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.newExpression.ToString();
		}
	}
}
