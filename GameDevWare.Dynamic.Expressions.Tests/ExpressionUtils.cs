using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GameDevWare.Dynamic.Expressions.CSharp;

namespace GameDevWare.Dynamic.Expressions.Tests
{
	public class ExpressionUtils
	{
		public static object Evaluate(string expression, Type[] types, bool forceAot, params object[] arguments)
		{
			var expressionObj = Parse(expression, types);

			var compileMethod = typeof(ExpressionExtentions)
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.Single(m => m.Name == "CompileAot" && m.IsGenericMethod && m.GetGenericArguments().Length == types.Length)
				.MakeGenericMethod(types);

			var @delegate = (Delegate)compileMethod.Invoke(null, new object[] { expressionObj, forceAot });
			return @delegate.DynamicInvoke(arguments);
		}

		public static LambdaExpression Parse(string expression, Type[] types)
		{
			var parseMethod = typeof(CSharpExpression)
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.Single(m => m.Name == "Parse" && m.IsGenericMethod && m.GetGenericArguments().Length == types.Length)
				.MakeGenericMethod(types);

			var parseArguments = new object[parseMethod.GetParameters().Length];
			foreach (var parameter in parseMethod.GetParameters())
				parseArguments[parameter.Position] = parameter.DefaultValue;
			parseArguments[0] = expression;

			var expressionObj = parseMethod.Invoke(null, parseArguments);
			return (LambdaExpression)expressionObj;
		}
	}
}
