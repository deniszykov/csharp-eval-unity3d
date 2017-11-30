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
using GameDevWare.Dynamic.Expressions.Execution;

// ReSharper disable once CheckNamespace
namespace System.Linq.Expressions
{
	/// <summary>
	/// Extension method for <see cref="Expression{DelegateT}"/> types.
	/// </summary>
	public static class ExpressionExtensions
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

			AotCompilation.RegisterFunc<TResult>();

			if (AotCompilation.IsAotRuntime || forceAot)
				return AotCompiler.PrepareFunc<TResult>(expression.Body);
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

			AotCompilation.RegisterFunc<TArg1, TResult>();

			if (AotCompilation.IsAotRuntime || forceAot)
				return AotCompiler.PrepareFunc<TArg1, TResult>(expression.Body, expression.Parameters);
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

			AotCompilation.RegisterFunc<TArg1, TArg2, TResult>();

			if (AotCompilation.IsAotRuntime || forceAot)
				return AotCompiler.PrepareFunc<TArg1, TArg2, TResult>(expression.Body, expression.Parameters);
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

			AotCompilation.RegisterFunc<TArg1, TArg2, TArg3, TResult>();

			if (AotCompilation.IsAotRuntime || forceAot)
				return AotCompiler.PrepareFunc<TArg1, TArg2, TArg3, TResult>(expression.Body, expression.Parameters);
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

			AotCompilation.RegisterFunc<TArg1, TArg2, TArg3, TArg4, TResult>();

			if (AotCompilation.IsAotRuntime || forceAot)
				return AotCompiler.PrepareFunc<TArg1, TArg2, TArg3, TArg4, TResult>(expression.Body, expression.Parameters);
			else
				return expression.Compile();
		}

		/// <summary>
		/// Compiles specified expression into <see cref="Action"/> delegate using AOT aware expression compiler.
		/// </summary>
		/// <param name="expression">An expression syntax tree. Not null.</param>
		/// <param name="forceAot">True to always use AOT compiler event if environment is JIT and supports dynamic code.</param>
		/// <returns>A compiled expression.</returns>
		public static Action CompileAot(this Expression<Action> expression, bool forceAot = false)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			AotCompilation.RegisterAction();

			if (AotCompilation.IsAotRuntime || forceAot)
				return AotCompiler.PrepareAction(expression.Body);
			else
				return expression.Compile();
		}
		/// <summary>
		///  Compiles specified expression into <see cref="Action{TArg1}"/> delegate using AOT aware expression compiler.
		/// </summary>
		/// <typeparam name="TArg1">First argument type.</typeparam>
		/// <param name="expression">An expression syntax tree. Not null.</param>
		/// <param name="forceAot">True to always use AOT compiler event if environment is JIT and supports dynamic code.</param>
		/// <returns>A compiled expression.</returns>
		public static Action<TArg1> CompileAot<TArg1>(this Expression<Action<TArg1>> expression, bool forceAot = false)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			AotCompilation.RegisterAction<TArg1>();

			if (AotCompilation.IsAotRuntime || forceAot)
				return AotCompiler.PrepareAction<TArg1>(expression.Body, expression.Parameters);
			else
				return expression.Compile();
		}
		/// <summary>
		///  Compiles specified expression into <see cref="Action{TArg1, TArg2}"/> delegate using AOT aware expression compiler.
		/// </summary>
		/// <typeparam name="TArg1">First argument type.</typeparam>
		/// <typeparam name="TArg2">Second argument type.</typeparam>
		/// <param name="expression">An expression syntax tree. Not null.</param>
		/// <param name="forceAot">True to always use AOT compiler event if environment is JIT and supports dynamic code.</param>
		/// <returns>A compiled expression.</returns>
		public static Action<TArg1, TArg2> CompileAot<TArg1, TArg2>(this Expression<Action<TArg1, TArg2>> expression, bool forceAot = false)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			AotCompilation.RegisterAction<TArg1, TArg2>();

			if (AotCompilation.IsAotRuntime || forceAot)
				return AotCompiler.PrepareAction<TArg1, TArg2>(expression.Body, expression.Parameters);
			else
				return expression.Compile();
		}
		/// <summary>
		///  Compiles specified expression into <see cref="Action{TArg1, TArg2, TArg3}"/> delegate using AOT aware expression compiler.
		/// </summary>
		/// <typeparam name="TArg1">First argument type.</typeparam>
		/// <typeparam name="TArg2">Second argument type.</typeparam>
		/// <typeparam name="TArg3">Third argument type.</typeparam>
		/// <param name="expression">An expression syntax tree. Not null.</param>
		/// <param name="forceAot">True to always use AOT compiler event if environment is JIT and supports dynamic code.</param>
		/// <returns>A compiled expression.</returns>
		public static Action<TArg1, TArg2, TArg3> CompileAot<TArg1, TArg2, TArg3>(this Expression<Action<TArg1, TArg2, TArg3>> expression, bool forceAot = false)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			AotCompilation.RegisterAction<TArg1, TArg2, TArg3>();

			if (AotCompilation.IsAotRuntime || forceAot)
				return AotCompiler.PrepareAction<TArg1, TArg2, TArg3>(expression.Body, expression.Parameters);
			else
				return expression.Compile();
		}
		/// <summary>
		///  Compiles specified expression into <see cref="Action{TArg1, TArg2, TArg3, TArg4}"/> delegate using AOT aware expression compiler.
		/// </summary>
		/// <typeparam name="TArg1">First argument type.</typeparam>
		/// <typeparam name="TArg2">Second argument type.</typeparam>
		/// <typeparam name="TArg3">Third argument type.</typeparam>
		/// <typeparam name="TArg4">Fourth argument type.</typeparam>
		/// <param name="expression">An expression syntax tree. Not null.</param>
		/// <param name="forceAot">True to always use AOT compiler event if environment is JIT and supports dynamic code.</param>
		/// <returns>A compiled expression.</returns>
		public static Action<TArg1, TArg2, TArg3, TArg4> CompileAot<TArg1, TArg2, TArg3, TArg4>(this Expression<Action<TArg1, TArg2, TArg3, TArg4>> expression, bool forceAot = false)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			AotCompilation.RegisterAction<TArg1, TArg2, TArg3, TArg4>();

			if (AotCompilation.IsAotRuntime || forceAot)
				return AotCompiler.PrepareAction<TArg1, TArg2, TArg3, TArg4>(expression.Body, expression.Parameters);
			else
				return expression.Compile();
		}
	}
}
