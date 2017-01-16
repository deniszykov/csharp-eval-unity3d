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

using GameDevWare.Dynamic.Expressions;

// ReSharper disable once CheckNamespace
namespace System.Linq.Expressions
{
	/// <summary>
	/// Extention method for <see cref="Expression{DelegateT}"/> types.
	/// </summary>
	public static class ExpressionExtentions
	{
		/// <summary>
		/// Compiles specified expression into <see cref="Func{TResult}"/> delegate using AOT aware expression compiler.
		/// </summary>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="expression">An expression syntax tree. Not null.</param>
		/// <param name="forceAot">True to always use AOT compiler event if environment is JIT and supports dynamic code.</param>
		/// <returns>A compiled expression.</returns>
		public static Func<TResult> CompileAot<TResult>(this Expression<Func<TResult>> expression, bool forceAot = false)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			if (AotCompilation.IsAotCompiled || forceAot)
				return Executor.Prepare<TResult>(expression.Body);
			else
				return expression.Compile();
		}
		/// <summary>
		///  Compiles specified expression into <see cref="Func{TArg1, TResult}"/> delegate using AOT aware expression compiler.
		/// </summary>
		/// <typeparam name="TArg1">First argument type.</typeparam>
		/// <typeparam name="TResult">Result type.</typeparam>
		/// <param name="expression">An expression syntax tree. Not null.</param>
		/// <param name="forceAot">True to always use AOT compiler event if environment is JIT and supports dynamic code.</param>
		/// <returns>A compiled expression.</returns>
		public static Func<TArg1, TResult> CompileAot<TArg1, TResult>(this Expression<Func<TArg1, TResult>> expression, bool forceAot = false)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			if (AotCompilation.IsAotCompiled || forceAot)
				return Executor.Prepare<TArg1, TResult>(expression.Body, expression.Parameters);
			else
				return expression.Compile();
		}
		/// <summary>
		///  Compiles specified expression into <see cref="Func{TArg1, TArg2, TResult}"/> delegate using AOT aware expression compiler.
		/// </summary>
		/// <typeparam name="TArg1">First argument type.</typeparam>
		/// <typeparam name="TArg2">Second argument type.</typeparam>
		/// <typeparam name="TResult">Result type.</typeparam>
		/// <param name="expression">An expression syntax tree. Not null.</param>
		/// <param name="forceAot">True to always use AOT compiler event if environment is JIT and supports dynamic code.</param>
		/// <returns>A compiled expression.</returns>
		public static Func<TArg1, TArg2, TResult> CompileAot<TArg1, TArg2, TResult>(this Expression<Func<TArg1, TArg2, TResult>> expression, bool forceAot = false)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			if (AotCompilation.IsAotCompiled || forceAot)
				return Executor.Prepare<TArg1, TArg2, TResult>(expression.Body, expression.Parameters);
			else
				return expression.Compile();
		}
		/// <summary>
		///  Compiles specified expression into <see cref="Func{TArg1, TArg2, TArg3, TResult}"/> delegate using AOT aware expression compiler.
		/// </summary>
		/// <typeparam name="TArg1">First argument type.</typeparam>
		/// <typeparam name="TArg2">Second argument type.</typeparam>
		/// <typeparam name="TArg3">Third argument type.</typeparam>
		/// <typeparam name="TResult">Result type.</typeparam>
		/// <param name="expression">An expression syntax tree. Not null.</param>
		/// <param name="forceAot">True to always use AOT compiler event if environment is JIT and supports dynamic code.</param>
		/// <returns>A compiled expression.</returns>
		public static Func<TArg1, TArg2, TArg3, TResult> CompileAot<TArg1, TArg2, TArg3, TResult>(this Expression<Func<TArg1, TArg2, TArg3, TResult>> expression, bool forceAot = false)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			if (AotCompilation.IsAotCompiled || forceAot)
				return Executor.Prepare<TArg1, TArg2, TArg3, TResult>(expression.Body, expression.Parameters);
			else
				return expression.Compile();
		}
		/// <summary>
		///  Compiles specified expression into <see cref="Func{TArg1, TArg2, TArg3, TArg4, TResult}"/> delegate using AOT aware expression compiler.
		/// </summary>
		/// <typeparam name="TArg1">First argument type.</typeparam>
		/// <typeparam name="TArg2">Second argument type.</typeparam>
		/// <typeparam name="TArg3">Third argument type.</typeparam>
		/// <typeparam name="TArg4">Fourth argument type.</typeparam>
		/// <typeparam name="TResult">Result type.</typeparam>
		/// <param name="expression">An expression syntax tree. Not null.</param>
		/// <param name="forceAot">True to always use AOT compiler event if environment is JIT and supports dynamic code.</param>
		/// <returns>A compiled expression.</returns>
		public static Func<TArg1, TArg2, TArg3, TArg4, TResult> CompileAot<TArg1, TArg2, TArg3, TArg4, TResult>(this Expression<Func<TArg1, TArg2, TArg3, TArg4, TResult>> expression, bool forceAot = false)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			if (AotCompilation.IsAotCompiled || forceAot)
				return Executor.Prepare<TArg1, TArg2, TArg3, TArg4, TResult>(expression.Body, expression.Parameters);
			else
				return expression.Compile();
		}
	}
}
