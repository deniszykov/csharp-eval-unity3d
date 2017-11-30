using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GameDevWare.Dynamic.Expressions.CSharp;

namespace GameDevWare.Dynamic.Expressions.Tests
{
	public class ExpressionUtils
	{
		public static object Evaluate(string expression, Type[] types, bool forceAot, ITypeResolver typeResolver = null, params object[] arguments)
		{
			var expressionObj = ParseFunc(expression, types, typeResolver);

			var compileMethod = typeof(ExpressionExtensions)
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.Single(m => m.Name == "CompileAot" && m.ReturnType.Name.StartsWith("Func") && m.IsGenericMethod && m.GetGenericArguments().Length == types.Length)
				.MakeGenericMethod(types);

			var @delegate = (Delegate)compileMethod.Invoke(null, new object[] { expressionObj, forceAot });
			return @delegate.DynamicInvoke(arguments);
		}

		public static void Execute(string expression, Type[] types, bool forceAot, ITypeResolver typeResolver = null, params object[] arguments)
		{
			var expressionObj = ParseAction(expression, types, typeResolver);

			var compileMethod = typeof(ExpressionExtensions)
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.Single(m => m.Name == "CompileAot" && m.ReturnType.Name.StartsWith("Action") && (types.Length > 0 ? (m.IsGenericMethod && m.GetGenericArguments().Length == types.Length) : m.IsGenericMethod == false));

			if (compileMethod.IsGenericMethodDefinition)
				compileMethod = compileMethod.MakeGenericMethod(types);

			var @delegate = (Delegate)compileMethod.Invoke(null, new object[] { expressionObj, forceAot });
			@delegate.DynamicInvoke(arguments);
		}

		public static LambdaExpression ParseFunc(string expression, Type[] types, ITypeResolver typeResolver = null)
		{
			var parseMethod = typeof(CSharpExpression)
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.Single(m => m.Name == "ParseFunc" && m.IsGenericMethod && m.GetGenericArguments().Length == types.Length)
				.MakeGenericMethod(types);

			var parseArguments = new object[parseMethod.GetParameters().Length];
			foreach (var parameter in parseMethod.GetParameters())
			{
				if (parameter.ParameterType == typeof(ITypeResolver))
					parseArguments[parameter.Position] = typeResolver;
				else
					parseArguments[parameter.Position] = parameter.DefaultValue;
			}
			parseArguments[0] = expression;

			var expressionObj = parseMethod.Invoke(null, parseArguments);
			return (LambdaExpression)expressionObj;
		}
		public static LambdaExpression ParseAction(string expression, Type[] types, ITypeResolver typeResolver = null)
		{
			var parseMethod = typeof(CSharpExpression)
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.Single(m => m.Name == "ParseAction" && (types.Length > 0 ? (m.IsGenericMethod && m.GetGenericArguments().Length == types.Length) : m.IsGenericMethod == false));

			if(parseMethod.IsGenericMethodDefinition)
				parseMethod = parseMethod.MakeGenericMethod(types);

			var parseArguments = new object[parseMethod.GetParameters().Length];
			foreach (var parameter in parseMethod.GetParameters())
			{
				if (parameter.ParameterType == typeof(ITypeResolver))
					parseArguments[parameter.Position] = typeResolver;
				else
					parseArguments[parameter.Position] = parameter.DefaultValue;
			}
			parseArguments[0] = expression;

			var expressionObj = parseMethod.Invoke(null, parseArguments);
			return (LambdaExpression)expressionObj;
		}
	}
}
