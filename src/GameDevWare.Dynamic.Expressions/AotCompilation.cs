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
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection.Emit;
using GameDevWare.Dynamic.Expressions.Execution;

namespace GameDevWare.Dynamic.Expressions
{
	/// <summary>
	/// Helper class for Ahead-of-Time(AOT) compiled environments.
	/// </summary>
	public static partial class AotCompilation
	{
		/// <summary>
		/// Is current runtime is AOT compiled.
		/// </summary>
		public static readonly bool IsAotRuntime;

		static AotCompilation()
		{
			StaticConstructor();

#if ((UNITY_WEBGL || UNITY_IOS || ENABLE_IL2CPP) && !UNITY_EDITOR)
			IsAotRuntime = true;
#else
			try
			{
				// check lambdas are supported
				Expression.Lambda<Func<bool>>(Expression.Constant(true)).Compile();

#if !NETSTANDARD1_3
				// check dynamic methods are supported
				var voidDynamicMethod = new DynamicMethod("TestVoidMethod", typeof(void), Type.EmptyTypes, restrictedSkipVisibility: true);
				var il = voidDynamicMethod.GetILGenerator();
				il.Emit(OpCodes.Nop);
				voidDynamicMethod.CreateDelegate(typeof(Action));
#endif
			}
			catch (Exception) { IsAotRuntime = true; }
#endif
		}

		static partial void StaticConstructor();

		/// <summary>
		/// Prepares method with specified signature for fast execution in AOT compiled environment.
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
		/// Prepares method with specified signature for fast execution in AOT compiled environment.
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
		/// Prepares method with specified signature for fast execution in AOT compiled environment.
		/// </summary>
		/// <typeparam name="InstanceT">Type of instance which method belongs.</typeparam>
		/// <typeparam name="Arg1T">Method's first argument type.</typeparam>
		/// <typeparam name="ResultT">Method's return type.</typeparam>
		public static void RegisterForFastCall<InstanceT, Arg1T, ResultT>()
		{
			FastCall.RegisterInstanceMethod<InstanceT, Arg1T, ResultT>();
		}
		/// <summary>
		/// Prepares method with specified signature for fast execution in AOT compiled environment.
		/// </summary>
		/// <typeparam name="InstanceT">Type of instance which method belongs.</typeparam>
		/// <typeparam name="ResultT">Method's return type.</typeparam>
		public static void RegisterForFastCall<InstanceT, ResultT>()
		{
			FastCall.RegisterInstanceMethod<InstanceT, ResultT>();
		}

		/// <summary>
		/// Prepares function <see cref="System.Func{Arg1T,Arg2T,Arg3T,Arg4T,ResultT}"/> with specified signature for execution in AOT compiled environment.
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
				var fn = Expression.Lambda<Func<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>>(default(Expression), default(ParameterExpression[])).CompileAot();
				fn.Invoke(default(Arg1T), default(Arg2T), default(Arg3T), default(Arg4T));
				fn.DynamicInvoke(default(Arg1T), default(Arg2T), default(Arg3T), default(Arg4T));
				AotCompiler.PrepareFunc<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>(default(Expression), default(ReadOnlyCollection<ParameterExpression>));
			}
		}
		/// <summary>
		/// Prepares function <see cref="System.Func{Arg1T,Arg2T,Arg3T,ResultT}"/> with specified signature for execution in AOT compiled environment.
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
				var fn = Expression.Lambda<Func<Arg1T, Arg2T, Arg3T, ResultT>>(default(Expression), default(ParameterExpression[])).CompileAot();
				fn.Invoke(default(Arg1T), default(Arg2T), default(Arg3T));
				fn.DynamicInvoke(default(Arg1T), default(Arg2T), default(Arg3T));
				AotCompiler.PrepareFunc<Arg1T, Arg2T, Arg3T, ResultT>(default(Expression), default(ReadOnlyCollection<ParameterExpression>));
			}
		}
		/// <summary>
		/// Prepares function <see cref="System.Func{Arg1T,Arg2T,ResultT}"/> with specified signature for execution in AOT compiled environment.
		/// </summary>
		/// <typeparam name="Arg1T">Function's first argument.</typeparam>
		/// <typeparam name="Arg2T">Function's second argument.</typeparam>
		/// <typeparam name="ResultT">Function result type.</typeparam>
		public static void RegisterFunc<Arg1T, Arg2T, ResultT>()
		{
			if (typeof(AotCompilation).Name == string.Empty)
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				var fn = Expression.Lambda<Func<Arg1T, Arg2T, ResultT>>(default(Expression), default(ParameterExpression[])).CompileAot();
				fn.Invoke(default(Arg1T), default(Arg2T));
				fn.DynamicInvoke(default(Arg1T), default(Arg2T));
				AotCompiler.PrepareFunc<Arg1T, Arg2T, ResultT>(default(Expression), default(ReadOnlyCollection<ParameterExpression>));
			}
		}
		/// <summary>
		/// Prepares function <see cref="System.Func{Arg1T,ResultT}"/> with specified signature for execution in AOT compiled environment.
		/// </summary>
		/// <typeparam name="Arg1T">Function's first argument.</typeparam>
		/// <typeparam name="ResultT">Function result type.</typeparam>
		public static void RegisterFunc<Arg1T, ResultT>()
		{
			if (typeof(AotCompilation).Name == string.Empty)
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				var fn = Expression.Lambda<Func<Arg1T, ResultT>>(default(Expression), default(ParameterExpression[])).CompileAot();
				fn.Invoke(default(Arg1T));
				fn.DynamicInvoke(default(Arg1T));
				AotCompiler.PrepareFunc<Arg1T, ResultT>(default(Expression), default(ReadOnlyCollection<ParameterExpression>));
			}
		}
		/// <summary>
		/// Prepares function <see cref="System.Func{ResultT}"/> with specified signature for execution in AOT compiled environment.
		/// </summary>
		/// <typeparam name="ResultT">Function result type.</typeparam>
		public static void RegisterFunc<ResultT>()
		{
			if (typeof(AotCompilation).Name == string.Empty)
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				var fn = Expression.Lambda<Func<ResultT>>(default(Expression), default(ParameterExpression[])).CompileAot();
				fn.Invoke();
				fn.DynamicInvoke();
				AotCompiler.PrepareFunc<ResultT>(default(Expression));
			}
		}

		/// <summary>
		/// Prepares function <see cref="System.Func{Arg1T,Arg2T,Arg3T,Arg4T,ResultT}"/> with specified signature for execution in AOT compiled environment.
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
				var fn = Expression.Lambda<Action<Arg1T, Arg2T, Arg3T, Arg4T>>(default(Expression), default(ParameterExpression[])).CompileAot();
				fn.Invoke(default(Arg1T), default(Arg2T), default(Arg3T), default(Arg4T));
				fn.DynamicInvoke(default(Arg1T), default(Arg2T), default(Arg3T), default(Arg4T));
				AotCompiler.PrepareAction<Arg1T, Arg2T, Arg3T, Arg4T>(default(Expression), default(ReadOnlyCollection<ParameterExpression>));
			}
		}
		/// <summary>
		/// Prepares function <see cref="System.Func{Arg1T,Arg2T,Arg3T,ResultT}"/> with specified signature for execution in AOT compiled environment.
		/// </summary>
		/// <typeparam name="Arg1T">Function's first argument.</typeparam>
		/// <typeparam name="Arg2T">Function's second argument.</typeparam>
		/// <typeparam name="Arg3T">Function's third argument.</typeparam>
		public static void RegisterAction<Arg1T, Arg2T, Arg3T>()
		{
			if (typeof(AotCompilation).Name == string.Empty)
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				var fn = Expression.Lambda<Action<Arg1T, Arg2T, Arg3T>>(default(Expression), default(ParameterExpression[])).CompileAot();
				fn.Invoke(default(Arg1T), default(Arg2T), default(Arg3T));
				fn.DynamicInvoke(default(Arg1T), default(Arg2T), default(Arg3T));
				AotCompiler.PrepareAction<Arg1T, Arg2T, Arg3T>(default(Expression), default(ReadOnlyCollection<ParameterExpression>));
			}
		}
		/// <summary>
		/// Prepares function <see cref="System.Func{Arg1T,Arg2T,ResultT}"/> with specified signature for execution in AOT compiled environment.
		/// </summary>
		/// <typeparam name="Arg1T">Function's first argument.</typeparam>
		/// <typeparam name="Arg2T">Function's second argument.</typeparam>
		public static void RegisterAction<Arg1T, Arg2T>()
		{
			if (typeof(AotCompilation).Name == string.Empty)
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				var fn = Expression.Lambda<Action<Arg1T, Arg2T>>(default(Expression), default(ParameterExpression[])).CompileAot();
				fn.Invoke(default(Arg1T), default(Arg2T));
				fn.DynamicInvoke(default(Arg1T), default(Arg2T));
				AotCompiler.PrepareAction<Arg1T, Arg2T>(default(Expression), default(ReadOnlyCollection<ParameterExpression>));
			}
		}
		/// <summary>
		/// Prepares function <see cref="System.Func{Arg1T,ResultT}"/> with specified signature for execution in AOT compiled environment.
		/// </summary>
		/// <typeparam name="Arg1T">Function's first argument.</typeparam>
		public static void RegisterAction<Arg1T>()
		{
			if (typeof(AotCompilation).Name == string.Empty)
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				var fn = Expression.Lambda<Action<Arg1T>>(default(Expression), default(ParameterExpression[])).CompileAot();
				fn.Invoke(default(Arg1T));
				fn.DynamicInvoke(default(Arg1T));
				AotCompiler.PrepareAction<Arg1T>(default(Expression), default(ReadOnlyCollection<ParameterExpression>));
			}
		}
		/// <summary>
		/// Prepares function <see cref="System.Func{ResultT}"/> with specified signature for execution in AOT compiled environment.
		/// </summary>
		public static void RegisterAction()
		{
			if (typeof(AotCompilation).Name == string.Empty)
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				var fn = Expression.Lambda<Action>(default(Expression), default(ParameterExpression[])).CompileAot();
				fn.Invoke();
				fn.DynamicInvoke();
				AotCompiler.PrepareAction(default(Expression));
			}
		}
	}
}
