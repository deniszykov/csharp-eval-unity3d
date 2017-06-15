/*
	Copyright (c) 2016 Denis Zykov, GameDevWare.com

	This a part of "C# Eval()" Unity Asset - https://www.assetstore.unity3d.com/en/#!/content/56706

	THIS SOFTWARE IS DISTRIBUTED "AS-IS" WITHOUT ANY WARRANTIES, CONDITIONS AND
	REPRESENTATIONS WHETHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION THE
	IMPLIED WARRANTIES AND CONDITIONS OF MERCHANTABILITY, MERCHANTABLE QUALITY,
	FITNESS FOR A PARTICULAR PURPOSE, DURABILITY, NON-INFRINGEMENT, PERFORMANCE
	AND THOSE ARISING BY STATUTE OR FROM CUSTOM OR USAGE OF TRADE OR COURSE OF DEALING.

	This source code is distributed via Unity Asset Store,
	to use it in your project you should accept Terms of Service and EULA
	https://unity3d.com/ru/legal/as_terms
*/

using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.CSharp
{
	/// <summary>
	/// Helpers method for C# expression parsing and evaluation.
	/// </summary>
	public static class CSharpExpression
	{
		/// <summary>
		/// Default value of "checked scope" parameter for <see cref="Parse{ResultT}"/> and <see cref="Evaluate{ResultT}"/> methods.
		/// </summary>
		public const bool DefaultCheckedScope = true;
		/// <summary>
		/// Default name of first argument for <see cref="Parse{Arg1,ResultT}"/> and <see cref="Evaluate{Arg1,ResultT}"/> methods.
		/// </summary>
		public const string ARG1_DEFAULT_NAME = "arg1";
		/// <summary>
		/// Default name of second argument for <see cref="Parse{Arg1,Arg2,ResultT}"/> and <see cref="Evaluate{Arg1,Arg2,ResultT}"/> methods.
		/// </summary>
		public const string ARG2_DEFAULT_NAME = "arg2";
		/// <summary>
		/// Default name of third argument for <see cref="Parse{Arg1,Arg2,Arg3,ResultT}"/> and <see cref="Evaluate{Arg1,Arg2,Arg3,ResultT}"/> methods.
		/// </summary>
		public const string ARG3_DEFAULT_NAME = "arg3";
		/// <summary>
		/// Default name of fourth argument for <see cref="Parse{Arg1,Arg2,Arg3,Arg4,ResultT}"/> and <see cref="Evaluate{Arg1,Arg2,Arg3,Arg4,ResultT}"/> methods.
		/// </summary>
		public const string ARG4_DEFAULT_NAME = "arg4";

		/// <summary>
		/// Evaluate specified C# expression and return result.
		/// </summary>
		/// <typeparam name="ResultT">Result type.</typeparam>
		/// <param name="expression">A valid c# expression. Not null, not empty string.</param>
		/// <param name="typeResolver">Type resolver for C# expression. Or <seealso cref="Binder.DefaultTypeResolver"/> if not specified.</param>
		/// <returns>Evaluated value.</returns>
		public static ResultT Evaluate<ResultT>(string expression, ITypeResolver typeResolver = null)
		{
			var func = Parse<ResultT>(expression, typeResolver).CompileAot();
			var result = func.Invoke();
			return result;
		}
		/// <summary>
		/// Evaluate specified C# expression and return result.
		/// </summary>
		/// <typeparam name="Arg1T">First argument type.</typeparam>
		/// <typeparam name="ResultT">Result type.</typeparam>
		/// <param name="expression">A valid c# expression. Not null, not empty string.</param>
		/// <param name="arg1">First argument value.</param>
		/// <param name="arg1Name">First argument name or <see cref="ARG1_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="typeResolver">Type resolver for C# expression. Or <seealso cref="Binder.DefaultTypeResolver"/> if not specified.</param>
		/// <returns>Evaluated value.</returns>
		public static ResultT Evaluate<Arg1T, ResultT>(string expression, Arg1T arg1, string arg1Name = ARG1_DEFAULT_NAME, ITypeResolver typeResolver = null)
		{
			var func = Parse<Arg1T, ResultT>(expression, arg1Name, typeResolver).CompileAot();
			var result = func.Invoke(arg1);
			return result;
		}
		/// <summary>
		/// Evaluate specified C# expression and return result.
		/// </summary>
		/// <typeparam name="Arg1T">First argument type.</typeparam>
		/// <typeparam name="Arg2T">Second argument type.</typeparam>
		/// <typeparam name="ResultT">Result type.</typeparam>
		/// <param name="expression">A valid c# expression. Not null, not empty string.</param>
		/// <param name="arg1">First argument value.</param>
		/// <param name="arg2">Second argument value.</param>
		/// <param name="arg1Name">First argument name or <see cref="ARG1_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="arg2Name">Second argument name or <see cref="ARG2_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="typeResolver">Type resolver for C# expression. Or <seealso cref="Binder.DefaultTypeResolver"/> if not specified.</param>
		/// <returns>Evaluated value.</returns>
		public static ResultT Evaluate<Arg1T, Arg2T, ResultT>(string expression, Arg1T arg1, Arg2T arg2, string arg1Name = ARG1_DEFAULT_NAME, string arg2Name = ARG2_DEFAULT_NAME, ITypeResolver typeResolver = null)
		{
			var func = Parse<Arg1T, Arg2T, ResultT>(expression, arg1Name, arg2Name, typeResolver).CompileAot();
			var result = func.Invoke(arg1, arg2);
			return result;
		}
		/// <summary>
		/// Evaluate specified C# expression and return result.
		/// </summary>
		/// <typeparam name="Arg1T">First argument type.</typeparam>
		/// <typeparam name="Arg2T">Second argument type.</typeparam>
		/// <typeparam name="Arg3T">Third argument type.</typeparam>
		/// <typeparam name="ResultT">Result type.</typeparam>
		/// <param name="expression">A valid c# expression. Not null, not empty string.</param>
		/// <param name="arg1">First argument value.</param>
		/// <param name="arg2">Second argument value.</param>
		/// <param name="arg3">Third argument value.</param>
		/// <param name="arg1Name">First argument name or <see cref="ARG1_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="arg2Name">Second argument name or <see cref="ARG2_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="arg3Name">Third argument name or <see cref="ARG3_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="typeResolver">Type resolver for C# expression. Or <seealso cref="Binder.DefaultTypeResolver"/> if not specified.</param>
		/// <returns>Evaluated value.</returns>
		public static ResultT Evaluate<Arg1T, Arg2T, Arg3T, ResultT>(string expression, Arg1T arg1, Arg2T arg2, Arg3T arg3, string arg1Name = ARG1_DEFAULT_NAME, string arg2Name = ARG2_DEFAULT_NAME, string arg3Name = ARG3_DEFAULT_NAME, ITypeResolver typeResolver = null)
		{
			var func = Parse<Arg1T, Arg2T, Arg3T, ResultT>(expression, arg1Name, arg2Name, arg3Name, typeResolver).CompileAot();
			var result = func.Invoke(arg1, arg2, arg3);
			return result;
		}
		/// <summary>
		/// Evaluate specified C# expression and return result.
		/// </summary>
		/// <typeparam name="Arg1T">First argument type.</typeparam>
		/// <typeparam name="Arg2T">Second argument type.</typeparam>
		/// <typeparam name="Arg3T">Third argument type.</typeparam>
		/// <typeparam name="Arg4T">Fourth argument type.</typeparam>
		/// <typeparam name="ResultT">Result type.</typeparam>
		/// <param name="expression">A valid c# expression. Not null, not empty string.</param>
		/// <param name="arg1">First argument value.</param>
		/// <param name="arg2">Second argument value.</param>
		/// <param name="arg3">Third argument value.</param>
		/// <param name="arg4">Fourth argument value.</param>
		/// <param name="arg1Name">First argument name or <see cref="ARG1_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="arg2Name">Second argument name or <see cref="ARG2_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="arg3Name">Third argument name or <see cref="ARG3_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="arg4Name">Fourth argument name or <see cref="ARG4_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="typeResolver">Type resolver for C# expression. Or <seealso cref="Binder.DefaultTypeResolver"/> if not specified.</param>
		/// <returns>Evaluated value.</returns>
		public static ResultT Evaluate<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>(string expression, Arg1T arg1, Arg2T arg2, Arg3T arg3, Arg4T arg4, string arg1Name = ARG1_DEFAULT_NAME, string arg2Name = ARG2_DEFAULT_NAME, string arg3Name = ARG3_DEFAULT_NAME, string arg4Name = ARG4_DEFAULT_NAME, ITypeResolver typeResolver = null)
		{
			var func = Parse<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>(expression, arg1Name, arg2Name, arg3Name, arg4Name, typeResolver).CompileAot();
			var result = func.Invoke(arg1, arg2, arg3, arg4);
			return result;
		}

		/// <summary>
		/// Parses specified C# expression and returns <see cref="Expression{TDelegate}"/> which could be compiled with <see cref="ExpressionExtensions.CompileAot{TResult}"/> and executed.
		/// </summary>
		/// <typeparam name="ResultT">Result type.</typeparam>
		/// <param name="expression">A valid c# expression. Not null, not empty string.</param>
		/// <param name="typeResolver">Type resolver for C# expression. Or <seealso cref="Binder.DefaultTypeResolver"/> if not specified.</param>
		/// <returns>A parsed and bound syntax tree.</returns>
		public static Expression<Func<ResultT>> Parse<ResultT>(string expression, ITypeResolver typeResolver = null)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			var tokens = Tokenizer.Tokenize(expression);
			var parseTree = Parser.Parse(tokens);
			var expressionTree = parseTree.ToSyntaxTree();
			var expressionBinder = new Binder(new ParameterExpression[0], resultType: typeof(ResultT), typeResolver: typeResolver);
			return (Expression<Func<ResultT>>)expressionBinder.Bind(expressionTree);
		}
		/// <summary>
		/// Parses specified C# expression and returns <see cref="Expression{TDelegate}"/> which could be compiled with <see cref="ExpressionExtensions.CompileAot{TResult}"/> and executed.
		/// </summary>
		/// <typeparam name="Arg1T">First argument type.</typeparam>
		/// <typeparam name="ResultT">Result type.</typeparam>
		/// <param name="expression">A valid c# expression. Not null, not empty string.</param>
		/// <param name="arg1Name">First argument name or <see cref="ARG1_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="typeResolver">Type resolver for C# expression. Or <seealso cref="Binder.DefaultTypeResolver"/> if not specified.</param>
		/// <returns>A parsed and bound syntax tree.</returns>
		public static Expression<Func<Arg1T, ResultT>> Parse<Arg1T, ResultT>(string expression, string arg1Name = ARG1_DEFAULT_NAME, ITypeResolver typeResolver = null)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			var tokens = Tokenizer.Tokenize(expression);
			var parseTree = Parser.Parse(tokens);
			var expressionTree = parseTree.ToSyntaxTree();
			var expressionBuilder = new Binder(new[]
			{
				Expression.Parameter(typeof(Arg1T), arg1Name ?? ARG1_DEFAULT_NAME)
			}, resultType: typeof(ResultT), typeResolver: typeResolver);
			return (Expression<Func<Arg1T, ResultT>>)expressionBuilder.Bind(expressionTree);
		}
		/// <summary>
		/// Parses specified C# expression and returns <see cref="Expression{TDelegate}"/> which could be compiled with <see cref="ExpressionExtensions.CompileAot{TResult}"/> and executed.
		/// </summary>
		/// <typeparam name="Arg1T">First argument type.</typeparam>
		/// <typeparam name="Arg2T">Second argument type.</typeparam>
		/// <typeparam name="ResultT">Result type.</typeparam>
		/// <param name="expression">A valid c# expression. Not null, not empty string.</param>
		/// <param name="arg1Name">First argument name or <see cref="ARG1_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="arg2Name">Second argument name or <see cref="ARG2_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="typeResolver">Type resolver for C# expression. Or <seealso cref="Binder.DefaultTypeResolver"/> if not specified.</param>
		/// <returns>A parsed and bound syntax tree.</returns>
		public static Expression<Func<Arg1T, Arg2T, ResultT>> Parse<Arg1T, Arg2T, ResultT>(string expression, string arg1Name = ARG1_DEFAULT_NAME, string arg2Name = ARG2_DEFAULT_NAME, ITypeResolver typeResolver = null)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			var tokens = Tokenizer.Tokenize(expression);
			var parseTree = Parser.Parse(tokens);
			var expressionTree = parseTree.ToSyntaxTree();
			var expressionBinder = new Binder(new[]
			{
				Expression.Parameter(typeof(Arg1T), arg1Name ?? ARG1_DEFAULT_NAME),
				Expression.Parameter(typeof(Arg2T), arg2Name ?? ARG2_DEFAULT_NAME),
			}, resultType: typeof(ResultT), typeResolver: typeResolver);

			return (Expression<Func<Arg1T, Arg2T, ResultT>>)expressionBinder.Bind(expressionTree);
		}
		/// <summary>
		/// Parses specified C# expression and returns <see cref="Expression{TDelegate}"/> which could be compiled with <see cref="ExpressionExtensions.CompileAot{TResult}"/> and executed.
		/// </summary>
		/// <typeparam name="Arg1T">First argument type.</typeparam>
		/// <typeparam name="Arg2T">Second argument type.</typeparam>
		/// <typeparam name="Arg3T">Third argument type.</typeparam>
		/// <typeparam name="ResultT">Result type.</typeparam>
		/// <param name="expression">A valid c# expression. Not null, not empty string.</param>
		/// <param name="arg1Name">First argument name or <see cref="ARG1_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="arg2Name">Second argument name or <see cref="ARG2_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="arg3Name">Third argument name or <see cref="ARG3_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="typeResolver">Type resolver for C# expression. Or <seealso cref="Binder.DefaultTypeResolver"/> if not specified.</param>
		/// <returns>A parsed and bound syntax tree.</returns>
		public static Expression<Func<Arg1T, Arg2T, Arg3T, ResultT>> Parse<Arg1T, Arg2T, Arg3T, ResultT>(string expression, string arg1Name = ARG1_DEFAULT_NAME, string arg2Name = ARG2_DEFAULT_NAME, string arg3Name = ARG3_DEFAULT_NAME, ITypeResolver typeResolver = null)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			var tokens = Tokenizer.Tokenize(expression);
			var parseTree = Parser.Parse(tokens);
			var expressionTree = parseTree.ToSyntaxTree();
			var expressionBinder = new Binder(new[]
			{
				Expression.Parameter(typeof(Arg1T), arg1Name ?? ARG1_DEFAULT_NAME),
				Expression.Parameter(typeof(Arg2T), arg2Name ?? ARG2_DEFAULT_NAME),
				Expression.Parameter(typeof(Arg3T), arg3Name ?? ARG3_DEFAULT_NAME),
			}, resultType: typeof(ResultT), typeResolver: typeResolver);

			return (Expression<Func<Arg1T, Arg2T, Arg3T, ResultT>>)expressionBinder.Bind(expressionTree);
		}
		/// <summary>
		/// Parses specified C# expression and returns <see cref="Expression{TDelegate}"/> which could be compiled with <see cref="ExpressionExtensions.CompileAot{TResult}"/> and executed.
		/// </summary>
		/// <typeparam name="Arg1T">First argument type.</typeparam>
		/// <typeparam name="Arg2T">Second argument type.</typeparam>
		/// <typeparam name="Arg3T">Third argument type.</typeparam>
		/// <typeparam name="Arg4T">Fourth argument type.</typeparam>
		/// <typeparam name="ResultT">Result type.</typeparam>
		/// <param name="expression">A valid c# expression. Not null, not empty string.</param>
		/// <param name="arg1Name">First argument name or <see cref="ARG1_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="arg2Name">Second argument name or <see cref="ARG2_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="arg3Name">Third argument name or <see cref="ARG3_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="arg4Name">Fourth argument name or <see cref="ARG4_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="typeResolver">Type resolver for C# expression. Or <seealso cref="Binder.DefaultTypeResolver"/> if not specified.</param>
		/// <returns>A parsed and bound syntax tree.</returns>
		public static Expression<Func<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>> Parse<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>(string expression, string arg1Name = ARG1_DEFAULT_NAME, string arg2Name = ARG2_DEFAULT_NAME, string arg3Name = ARG3_DEFAULT_NAME, string arg4Name = ARG4_DEFAULT_NAME, ITypeResolver typeResolver = null)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			var tokens = Tokenizer.Tokenize(expression);
			var parseTree = Parser.Parse(tokens);
			var expressionTree = parseTree.ToSyntaxTree();
			var expressionBinder = new Binder(new[]
			{
				Expression.Parameter(typeof(Arg1T), arg1Name ?? ARG1_DEFAULT_NAME),
				Expression.Parameter(typeof(Arg2T), arg2Name ?? ARG2_DEFAULT_NAME),
				Expression.Parameter(typeof(Arg3T), arg3Name ?? ARG3_DEFAULT_NAME),
				Expression.Parameter(typeof(Arg4T), arg3Name ?? ARG4_DEFAULT_NAME),
			}, resultType: typeof(ResultT), typeResolver: typeResolver);

			return (Expression<Func<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>>)expressionBinder.Bind(expressionTree);
		}
	}
}
