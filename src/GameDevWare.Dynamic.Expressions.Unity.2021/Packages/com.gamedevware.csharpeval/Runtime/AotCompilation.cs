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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using GameDevWare.Dynamic.Expressions.Execution;

namespace GameDevWare.Dynamic.Expressions
{
	/// <summary>
	///     Helper class for Ahead-of-Time(AOT) compiled environments.
	/// </summary>
	public static partial class AotCompilation
	{
		/// <summary>
		///     Is current runtime is AOT compiled.
		/// </summary>
		public static bool IsAotRuntime;

		static AotCompilation()
		{
			StaticConstructor();

#if ((UNITY_WEBGL || UNITY_IOS || ENABLE_IL2CPP) && !UNITY_EDITOR)
			IsAotRuntime = true;
#endif
		}

		static partial void StaticConstructor();

		/// <summary>
		///     Prepares method with specified signature for fast execution in AOT compiled environment.
		/// </summary>
		/// <typeparam name="InstanceT">Type of instance which method belongs.</typeparam>
		/// <typeparam name="Arg1T">Method's first argument type.</typeparam>
		/// <typeparam name="Arg2T">Method's second argument type.</typeparam>
		/// <typeparam name="Arg3T">Method's third argument type.</typeparam>
		/// <typeparam name="ResultT">Method's return type.</typeparam>
		public static void RegisterForFastCall<InstanceT, Arg1T, Arg2T, Arg3T, ResultT>()
		{
			FastCall.RegisterInstanceMethod<InstanceT, Arg1T, Arg2T, Arg3T, ResultT>();
		}
		/// <summary>
		///     Prepares method with specified signature for fast execution in AOT compiled environment.
		/// </summary>
		/// <typeparam name="InstanceT">Type of instance which method belongs.</typeparam>
		/// <typeparam name="Arg1T">Method's first argument type.</typeparam>
		/// <typeparam name="Arg2T">Method's second argument type.</typeparam>
		/// <typeparam name="ResultT">Method's return type.</typeparam>
		public static void RegisterForFastCall<InstanceT, Arg1T, Arg2T, ResultT>()
		{
			FastCall.RegisterInstanceMethod<InstanceT, Arg1T, Arg2T, ResultT>();
		}
		/// <summary>
		///     Prepares method with specified signature for fast execution in AOT compiled environment.
		/// </summary>
		/// <typeparam name="InstanceT">Type of instance which method belongs.</typeparam>
		/// <typeparam name="Arg1T">Method's first argument type.</typeparam>
		/// <typeparam name="ResultT">Method's return type.</typeparam>
		public static void RegisterForFastCall<InstanceT, Arg1T, ResultT>()
		{
			FastCall.RegisterInstanceMethod<InstanceT, Arg1T, ResultT>();
		}
		/// <summary>
		///     Prepares method with specified signature for fast execution in AOT compiled environment.
		/// </summary>
		/// <typeparam name="InstanceT">Type of instance which method belongs.</typeparam>
		/// <typeparam name="ResultT">Method's return type.</typeparam>
		public static void RegisterForFastCall<InstanceT, ResultT>()
		{
			FastCall.RegisterInstanceMethod<InstanceT, ResultT>();
		}

		/// <summary>
		///     Prepares function <see cref="System.Func{Arg1T,Arg2T,Arg3T,Arg4T,ResultT}" /> with specified signature for
		///     execution in AOT compiled environment.
		/// </summary>
		/// <typeparam name="Arg1T">Function's first argument.</typeparam>
		/// <typeparam name="Arg2T">Function's second argument.</typeparam>
		/// <typeparam name="Arg3T">Function's third argument.</typeparam>
		/// <typeparam name="Arg4T">Function's fourth argument.</typeparam>
		/// <typeparam name="ResultT">Function result type.</typeparam>
		public static void RegisterFunc<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>()
		{
			if (typeof(AotCompilation).Name == string.Empty)
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				var fn = Expression.Lambda<Func<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>>(default, default(ParameterExpression[])).CompileAot();
				fn.Invoke(default, default, default, default);
				fn.DynamicInvoke(default(Arg1T), default(Arg2T), default(Arg3T), default(Arg4T));
				AotCompiler.PrepareFunc<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>(default, default);
			}
		}
		/// <summary>
		///     Prepares function <see cref="System.Func{Arg1T,Arg2T,Arg3T,ResultT}" /> with specified signature for execution in
		///     AOT compiled environment.
		/// </summary>
		/// <typeparam name="Arg1T">Function's first argument.</typeparam>
		/// <typeparam name="Arg2T">Function's second argument.</typeparam>
		/// <typeparam name="Arg3T">Function's third argument.</typeparam>
		/// <typeparam name="ResultT">Function result type.</typeparam>
		public static void RegisterFunc<Arg1T, Arg2T, Arg3T, ResultT>()
		{
			if (typeof(AotCompilation).Name == string.Empty)
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				var fn = Expression.Lambda<Func<Arg1T, Arg2T, Arg3T, ResultT>>(default, default(ParameterExpression[])).CompileAot();
				fn.Invoke(default, default, default);
				fn.DynamicInvoke(default(Arg1T), default(Arg2T), default(Arg3T));
				AotCompiler.PrepareFunc<Arg1T, Arg2T, Arg3T, ResultT>(default, default);
			}
		}
		/// <summary>
		///     Prepares function <see cref="System.Func{Arg1T,Arg2T,ResultT}" /> with specified signature for execution in AOT
		///     compiled environment.
		/// </summary>
		/// <typeparam name="Arg1T">Function's first argument.</typeparam>
		/// <typeparam name="Arg2T">Function's second argument.</typeparam>
		/// <typeparam name="ResultT">Function result type.</typeparam>
		public static void RegisterFunc<Arg1T, Arg2T, ResultT>()
		{
			if (typeof(AotCompilation).Name == string.Empty)
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				var fn = Expression.Lambda<Func<Arg1T, Arg2T, ResultT>>(default, default(ParameterExpression[])).CompileAot();
				fn.Invoke(default, default);
				fn.DynamicInvoke(default(Arg1T), default(Arg2T));
				AotCompiler.PrepareFunc<Arg1T, Arg2T, ResultT>(default, default);
			}
		}
		/// <summary>
		///     Prepares function <see cref="System.Func{Arg1T,ResultT}" /> with specified signature for execution in AOT compiled
		///     environment.
		/// </summary>
		/// <typeparam name="Arg1T">Function's first argument.</typeparam>
		/// <typeparam name="ResultT">Function result type.</typeparam>
		public static void RegisterFunc<Arg1T, ResultT>()
		{
			if (typeof(AotCompilation).Name == string.Empty)
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				var fn = Expression.Lambda<Func<Arg1T, ResultT>>(default, default(ParameterExpression[])).CompileAot();
				fn.Invoke(default);
				fn.DynamicInvoke(default(Arg1T));
				AotCompiler.PrepareFunc<Arg1T, ResultT>(default, default);
			}
		}
		/// <summary>
		///     Prepares function <see cref="System.Func{ResultT}" /> with specified signature for execution in AOT compiled
		///     environment.
		/// </summary>
		/// <typeparam name="ResultT">Function result type.</typeparam>
		public static void RegisterFunc<ResultT>()
		{
			if (typeof(AotCompilation).Name == string.Empty)
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				var fn = Expression.Lambda<Func<ResultT>>(default, default(ParameterExpression[])).CompileAot();
				fn.Invoke();
				fn.DynamicInvoke();
				AotCompiler.PrepareFunc<ResultT>(default);
			}
		}

		/// <summary>
		///     Prepares <see cref="Enumerable" /> queries over a sequence of <typeparamref name="ItemT" /> for execution in AOT
		///     compiled environment. Projections are only prepared for a result of <see cref="object" />, so a query projecting
		///     elements to another type needs <see cref="RegisterLinqFunc{ItemT,ResultT}" /> for that type.
		/// </summary>
		/// <typeparam name="ItemT">Type of an element of a queried sequence.</typeparam>
		public static void RegisterLinqFunc<ItemT>()
		{
			if (typeof(AotCompilation).Name == string.Empty)
			{
				RegisterFunc<ItemT, bool>();
				RegisterFunc<ItemT, int, bool>();
				RegisterLinqFunc<ItemT, object>();

				var source = default(IEnumerable<ItemT>);
				var predicate = default(Func<ItemT, bool>);
				var indexedPredicate = default(Func<ItemT, int, bool>);

				Enumerable.Where(source, predicate);
				Enumerable.Where(source, indexedPredicate);
				Enumerable.First(source);
				Enumerable.First(source, predicate);
				Enumerable.FirstOrDefault(source);
				Enumerable.FirstOrDefault(source, predicate);
				Enumerable.Last(source);
				Enumerable.Last(source, predicate);
				Enumerable.LastOrDefault(source);
				Enumerable.LastOrDefault(source, predicate);
				Enumerable.Single(source);
				Enumerable.SingleOrDefault(source);
				Enumerable.ElementAt(source, 0);
				Enumerable.ElementAtOrDefault(source, 0);
				Enumerable.Any(source);
				Enumerable.Any(source, predicate);
				Enumerable.All(source, predicate);
				Enumerable.Count(source);
				Enumerable.Count(source, predicate);
				Enumerable.Contains(source, default(ItemT));
				Enumerable.Take(source, 0);
				Enumerable.TakeWhile(source, predicate);
				Enumerable.Skip(source, 0);
				Enumerable.SkipWhile(source, predicate);
				Enumerable.Distinct(source);
				Enumerable.Reverse(source);
				Enumerable.DefaultIfEmpty(source);
				Enumerable.Concat(source, source);
				Enumerable.Union(source, source);
				Enumerable.Intersect(source, source);
				Enumerable.Except(source, source);
				Enumerable.ToList(source);
				Enumerable.ToArray(source);
			}
		}
		/// <summary>
		///     Prepares <see cref="Enumerable" /> queries projecting a sequence of <typeparamref name="ItemT" /> to
		///     <typeparamref name="ResultT" /> for execution in AOT compiled environment. The projected sequence is prepared as
		///     well, so a query continuing after the projection needs no further registration.
		/// </summary>
		/// <typeparam name="ItemT">Type of an element of a queried sequence.</typeparam>
		/// <typeparam name="ResultT">Type an element is projected to: the result of Select or the key of OrderBy.</typeparam>
		public static void RegisterLinqFunc<ItemT, ResultT>()
		{
			if (typeof(AotCompilation).Name == string.Empty)
			{
				RegisterFunc<ItemT, ResultT>();
				RegisterFunc<ItemT, int, ResultT>();
				RegisterLinqFunc<ResultT>();

				var source = default(IEnumerable<ItemT>);
				var orderedSource = default(IOrderedEnumerable<ItemT>);
				var selector = default(Func<ItemT, ResultT>);
				var indexedSelector = default(Func<ItemT, int, ResultT>);
				var sequenceSelector = default(Func<ItemT, IEnumerable<ResultT>>);

				Enumerable.Select(source, selector);
				Enumerable.Select(source, indexedSelector);
				Enumerable.SelectMany(source, sequenceSelector);
				Enumerable.OrderBy(source, selector);
				Enumerable.OrderByDescending(source, selector);
				Enumerable.ThenBy(orderedSource, selector);
				Enumerable.ThenByDescending(orderedSource, selector);
				Enumerable.Min(source, selector);
				Enumerable.Max(source, selector);
			}
		}

		/// <summary>
		///     Prepares function <see cref="System.Func{Arg1T,Arg2T,Arg3T,Arg4T,ResultT}" /> with specified signature for
		///     execution in AOT compiled environment.
		/// </summary>
		/// <typeparam name="Arg1T">Function's first argument.</typeparam>
		/// <typeparam name="Arg2T">Function's second argument.</typeparam>
		/// <typeparam name="Arg3T">Function's third argument.</typeparam>
		/// <typeparam name="Arg4T">Function's fourth argument.</typeparam>
		public static void RegisterAction<Arg1T, Arg2T, Arg3T, Arg4T>()
		{
			if (typeof(AotCompilation).Name == string.Empty)
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				var fn = Expression.Lambda<Action<Arg1T, Arg2T, Arg3T, Arg4T>>(default, default(ParameterExpression[])).CompileAot();
				fn.Invoke(default, default, default, default);
				fn.DynamicInvoke(default(Arg1T), default(Arg2T), default(Arg3T), default(Arg4T));
				AotCompiler.PrepareAction<Arg1T, Arg2T, Arg3T, Arg4T>(default, default);
			}
		}
		/// <summary>
		///     Prepares function <see cref="System.Func{Arg1T,Arg2T,Arg3T,ResultT}" /> with specified signature for execution in
		///     AOT compiled environment.
		/// </summary>
		/// <typeparam name="Arg1T">Function's first argument.</typeparam>
		/// <typeparam name="Arg2T">Function's second argument.</typeparam>
		/// <typeparam name="Arg3T">Function's third argument.</typeparam>
		public static void RegisterAction<Arg1T, Arg2T, Arg3T>()
		{
			if (typeof(AotCompilation).Name == string.Empty)
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				var fn = Expression.Lambda<Action<Arg1T, Arg2T, Arg3T>>(default, default(ParameterExpression[])).CompileAot();
				fn.Invoke(default, default, default);
				fn.DynamicInvoke(default(Arg1T), default(Arg2T), default(Arg3T));
				AotCompiler.PrepareAction<Arg1T, Arg2T, Arg3T>(default, default);
			}
		}
		/// <summary>
		///     Prepares function <see cref="System.Func{Arg1T,Arg2T,ResultT}" /> with specified signature for execution in AOT
		///     compiled environment.
		/// </summary>
		/// <typeparam name="Arg1T">Function's first argument.</typeparam>
		/// <typeparam name="Arg2T">Function's second argument.</typeparam>
		public static void RegisterAction<Arg1T, Arg2T>()
		{
			if (typeof(AotCompilation).Name == string.Empty)
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				var fn = Expression.Lambda<Action<Arg1T, Arg2T>>(default, default(ParameterExpression[])).CompileAot();
				fn.Invoke(default, default);
				fn.DynamicInvoke(default(Arg1T), default(Arg2T));
				AotCompiler.PrepareAction<Arg1T, Arg2T>(default, default);
			}
		}
		/// <summary>
		///     Prepares function <see cref="System.Func{Arg1T,ResultT}" /> with specified signature for execution in AOT compiled
		///     environment.
		/// </summary>
		/// <typeparam name="Arg1T">Function's first argument.</typeparam>
		public static void RegisterAction<Arg1T>()
		{
			if (typeof(AotCompilation).Name == string.Empty)
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				var fn = Expression.Lambda<Action<Arg1T>>(default, default(ParameterExpression[])).CompileAot();
				fn.Invoke(default);
				fn.DynamicInvoke(default(Arg1T));
				AotCompiler.PrepareAction<Arg1T>(default, default);
			}
		}
		/// <summary>
		///     Prepares function <see cref="System.Func{ResultT}" /> with specified signature for execution in AOT compiled
		///     environment.
		/// </summary>
		public static void RegisterAction()
		{
			if (typeof(AotCompilation).Name == string.Empty)
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				var fn = Expression.Lambda<Action>(default, default(ParameterExpression[])).CompileAot();
				fn.Invoke();
				fn.DynamicInvoke();
				AotCompiler.PrepareAction(default);
			}
		}
	}
}
