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
using System.Reflection;

// ReSharper disable PossibleNullReferenceException

namespace GameDevWare.Dynamic.Expressions
{
	partial class Executor
	{
		private delegate object InvokeOperation(Closure closure, ExecuteFunc[] argumentFns);
		private delegate InvokeOperation InvokeOperationCreator(MethodInfo method, ParameterInfo[] parameters);

		private class MethodCall
		{
			private static readonly Dictionary<MethodInfo, InvokeOperation> StaticMethods = new Dictionary<MethodInfo, InvokeOperation>();
			private static readonly Dictionary<MethodInfo, InvokeOperation> InstanceMethods = new Dictionary<MethodInfo, InvokeOperation>();
			private static readonly Dictionary<Type, Dictionary<MethodCallSignature, InvokeOperationCreator>[]> InstanceMethodCreators = new Dictionary<Type, Dictionary<MethodCallSignature, InvokeOperationCreator>[]>();

			private readonly Delegate fn;

			private MethodCall(Type delegateType, MethodInfo method)
			{
				if (delegateType == null) throw new ArgumentNullException("method");
				if (method == null) throw new ArgumentNullException("method");

				this.fn = Delegate.CreateDelegate(delegateType, method, true);
			}

			private object FuncInvoker<ResultT>(Closure closure, ExecuteFunc[] argumentFns)
			{
				return ((Func<ResultT>)this.fn).Invoke();
			}
			private object FuncInvoker<Arg1T, ResultT>(Closure closure, ExecuteFunc[] argumentFns)
			{
				var arg1 = closure.Unbox<Arg1T>(argumentFns[0](closure));
				return ((Func<Arg1T, ResultT>)this.fn).Invoke(arg1);
			}
			private object FuncInvoker<Arg1T, Arg2T, ResultT>(Closure closure, ExecuteFunc[] argumentFns)
			{
				var arg1 = closure.Unbox<Arg1T>(argumentFns[0](closure));
				var arg2 = closure.Unbox<Arg2T>(argumentFns[1](closure));
				return ((Func<Arg1T, Arg2T, ResultT>)this.fn).Invoke(arg1, arg2);
			}
			private object FuncInvoker<Arg1T, Arg2T, Arg3T, ResultT>(Closure closure, ExecuteFunc[] argumentFns)
			{
				var arg1 = closure.Unbox<Arg1T>(argumentFns[0](closure));
				var arg2 = closure.Unbox<Arg2T>(argumentFns[1](closure));
				var arg3 = closure.Unbox<Arg3T>(argumentFns[2](closure));
				return ((Func<Arg1T, Arg2T, Arg3T, ResultT>)this.fn).Invoke(arg1, arg2, arg3);
			}
			private object FuncInvoker<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>(Closure closure, ExecuteFunc[] argumentFns)
			{
				var arg1 = closure.Unbox<Arg1T>(argumentFns[0](closure));
				var arg2 = closure.Unbox<Arg2T>(argumentFns[1](closure));
				var arg3 = closure.Unbox<Arg3T>(argumentFns[2](closure));
				var arg4 = closure.Unbox<Arg4T>(argumentFns[3](closure));
				return ((Func<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>)this.fn).Invoke(arg1, arg2, arg3, arg4);
			}

			public static InvokeOperation TryCreate(MethodInfo method)
			{
				if (method == null) throw new ArgumentNullException("method");

				if (method.IsStatic)
					return TryCreateStaticMethod(method);
				else
					return TryCreateInstanceMethod(method);
			}
			private static InvokeOperation TryCreateStaticMethod(MethodInfo method)
			{
				// try get from cache
				var invoker = default(InvokeOperation);
				lock (StaticMethods)
					if (StaticMethods.TryGetValue(method, out invoker))
						return invoker;

				var parameters = method.GetParameters();
				// ReSharper disable once SwitchStatementMissingSomeCases
				switch (parameters.Length)
				{
					case 0:
						invoker =
							TryCreate<bool>(method, parameters) ??
							TryCreate<byte>(method, parameters) ??
							TryCreate<sbyte>(method, parameters) ??
							TryCreate<short>(method, parameters) ??
							TryCreate<ushort>(method, parameters) ??
							TryCreate<int>(method, parameters) ??
							TryCreate<uint>(method, parameters) ??
							TryCreate<long>(method, parameters) ??
							TryCreate<ulong>(method, parameters) ??
							TryCreate<float>(method, parameters) ??
							TryCreate<double>(method, parameters) ??
							TryCreate<decimal>(method, parameters) ??
							TryCreate<string>(method, parameters) ??
							TryCreate<object>(method, parameters) ??
							TryCreate<TimeSpan>(method, parameters) ??
							TryCreate<DateTime>(method, parameters);
						break;
					case 1:
						invoker =
							TryCreate<bool, bool>(method, parameters) ??
							TryCreate<byte, byte>(method, parameters) ??
							TryCreate<sbyte, sbyte>(method, parameters) ??
							TryCreate<short, short>(method, parameters) ??
							TryCreate<ushort, ushort>(method, parameters) ??
							TryCreate<int, int>(method, parameters) ??
							TryCreate<uint, long>(method, parameters) ??
							TryCreate<long, long>(method, parameters) ??
							TryCreate<ulong, ulong>(method, parameters) ??
							TryCreate<float, float>(method, parameters) ??
							TryCreate<double, double>(method, parameters) ??
							TryCreate<decimal, decimal>(method, parameters) ??
							TryCreate<string, string>(method, parameters) ??
							TryCreate<object, object>(method, parameters) ??
							TryCreate<TimeSpan, TimeSpan>(method, parameters) ??
							TryCreate<DateTime, DateTime>(method, parameters) ??
							TryCreate<DateTime, TimeSpan>(method, parameters) ??
							TryCreate<byte, bool>(method, parameters) ??
							TryCreate<sbyte, bool>(method, parameters) ??
							TryCreate<short, bool>(method, parameters) ??
							TryCreate<ushort, bool>(method, parameters) ??
							TryCreate<int, bool>(method, parameters) ??
							TryCreate<uint, bool>(method, parameters) ??
							TryCreate<long, bool>(method, parameters) ??
							TryCreate<ulong, bool>(method, parameters) ??
							TryCreate<float, bool>(method, parameters) ??
							TryCreate<double, bool>(method, parameters) ??
							TryCreate<decimal, bool>(method, parameters) ??
							TryCreate<string, bool>(method, parameters) ??
							TryCreate<object, bool>(method, parameters) ??
							TryCreate<object, string>(method, parameters) ??
							TryCreate<TimeSpan, bool>(method, parameters) ??
							TryCreate<DateTime, bool>(method, parameters);
						break;
					case 2:
						invoker =
							TryCreate<bool, bool, bool>(method, parameters) ??
							TryCreate<byte, byte, byte>(method, parameters) ??
							TryCreate<sbyte, sbyte, sbyte>(method, parameters) ??
							TryCreate<short, short, short>(method, parameters) ??
							TryCreate<ushort, ushort, ushort>(method, parameters) ??
							TryCreate<int, int, int>(method, parameters) ??
							TryCreate<uint, uint, long>(method, parameters) ??
							TryCreate<long, long, long>(method, parameters) ??
							TryCreate<ulong, ulong, ulong>(method, parameters) ??
							TryCreate<float, float, float>(method, parameters) ??
							TryCreate<double, double, double>(method, parameters) ??
							TryCreate<decimal, decimal, decimal>(method, parameters) ??
							TryCreate<string, string, string>(method, parameters) ??
							TryCreate<object, object, object>(method, parameters) ??
							TryCreate<TimeSpan, TimeSpan, TimeSpan>(method, parameters) ??
							TryCreate<DateTime, DateTime, DateTime>(method, parameters) ??
							TryCreate<DateTime, DateTime, TimeSpan>(method, parameters) ??
							TryCreate<DateTime, TimeSpan, DateTime>(method, parameters) ??
							TryCreate<byte, byte, bool>(method, parameters) ??
							TryCreate<sbyte, sbyte, bool>(method, parameters) ??
							TryCreate<short, short, bool>(method, parameters) ??
							TryCreate<ushort, ushort, bool>(method, parameters) ??
							TryCreate<int, int, bool>(method, parameters) ??
							TryCreate<uint, uint, bool>(method, parameters) ??
							TryCreate<long, long, bool>(method, parameters) ??
							TryCreate<ulong, ulong, bool>(method, parameters) ??
							TryCreate<float, float, bool>(method, parameters) ??
							TryCreate<double, double, bool>(method, parameters) ??
							TryCreate<decimal, decimal, bool>(method, parameters) ??
							TryCreate<string, string, bool>(method, parameters) ??
							TryCreate<object, object, bool>(method, parameters) ??
							TryCreate<object, object, string>(method, parameters) ??
							TryCreate<TimeSpan, TimeSpan, bool>(method, parameters) ??
							TryCreate<DateTime, DateTime, bool>(method, parameters);
						break;
					case 3:
						invoker =
							TryCreate<bool, bool, bool, bool>(method, parameters) ??
							TryCreate<byte, byte, byte, byte>(method, parameters) ??
							TryCreate<sbyte, sbyte, sbyte, sbyte>(method, parameters) ??
							TryCreate<short, short, short, short>(method, parameters) ??
							TryCreate<ushort, ushort, ushort, ushort>(method, parameters) ??
							TryCreate<int, int, int, int>(method, parameters) ??
							TryCreate<uint, uint, uint, long>(method, parameters) ??
							TryCreate<long, long, long, long>(method, parameters) ??
							TryCreate<ulong, ulong, ulong, ulong>(method, parameters) ??
							TryCreate<float, float, float, float>(method, parameters) ??
							TryCreate<double, double, double, double>(method, parameters) ??
							TryCreate<decimal, decimal, decimal, decimal>(method, parameters) ??
							TryCreate<string, string, string, string>(method, parameters) ??
							TryCreate<object, object, object, object>(method, parameters) ??
							TryCreate<byte, byte, byte, bool>(method, parameters) ??
							TryCreate<sbyte, sbyte, sbyte, bool>(method, parameters) ??
							TryCreate<short, short, short, bool>(method, parameters) ??
							TryCreate<ushort, ushort, ushort, bool>(method, parameters) ??
							TryCreate<int, int, int, bool>(method, parameters) ??
							TryCreate<uint, uint, uint, bool>(method, parameters) ??
							TryCreate<long, long, long, bool>(method, parameters) ??
							TryCreate<ulong, ulong, ulong, bool>(method, parameters) ??
							TryCreate<float, float, float, bool>(method, parameters) ??
							TryCreate<double, double, double, bool>(method, parameters) ??
							TryCreate<decimal, decimal, decimal, bool>(method, parameters) ??
							TryCreate<string, string, string, bool>(method, parameters) ??
							TryCreate<object, object, object, bool>(method, parameters) ??
							TryCreate<object, object, object, string>(method, parameters);
						break;
					case 4:
						invoker =
							TryCreate<bool, bool, bool, bool, bool>(method, parameters) ??
							TryCreate<byte, byte, byte, byte, byte>(method, parameters) ??
							TryCreate<sbyte, sbyte, sbyte, sbyte, sbyte>(method, parameters) ??
							TryCreate<sbyte, short, short, short, short>(method, parameters) ??
							TryCreate<ushort, ushort, ushort, ushort, ushort>(method, parameters) ??
							TryCreate<int, int, int, int, int>(method, parameters) ??
							TryCreate<uint, uint, uint, uint, long>(method, parameters) ??
							TryCreate<long, long, long, long, long>(method, parameters) ??
							TryCreate<ulong, ulong, ulong, ulong, ulong>(method, parameters) ??
							TryCreate<float, float, float, float, float>(method, parameters) ??
							TryCreate<double, double, double, double, double>(method, parameters) ??
							TryCreate<decimal, decimal, decimal, decimal, decimal>(method, parameters) ??
							TryCreate<string, string, string, string, string>(method, parameters) ??
							TryCreate<object, object, object, object, object>(method, parameters) ??
							TryCreate<byte, byte, byte, byte, bool>(method, parameters) ??
							TryCreate<sbyte, sbyte, sbyte, sbyte, bool>(method, parameters) ??
							TryCreate<short, short, short, short, bool>(method, parameters) ??
							TryCreate<ushort, ushort, ushort, ushort, bool>(method, parameters) ??
							TryCreate<int, int, int, int, bool>(method, parameters) ??
							TryCreate<uint, uint, uint, uint, bool>(method, parameters) ??
							TryCreate<long, long, long, long, bool>(method, parameters) ??
							TryCreate<ulong, ulong, ulong, ulong, bool>(method, parameters) ??
							TryCreate<float, float, float, float, bool>(method, parameters) ??
							TryCreate<double, double, double, double, bool>(method, parameters) ??
							TryCreate<decimal, decimal, decimal, decimal, bool>(method, parameters) ??
							TryCreate<string, string, string, string, bool>(method, parameters) ??
							TryCreate<object, object, object, object, bool>(method, parameters) ??
							TryCreate<object, object, object, object, string>(method, parameters);
						break;
				}

				// cache it
				lock (StaticMethods)
					StaticMethods[method] = invoker;

				return invoker;
			}
			private static InvokeOperation TryCreateInstanceMethod(MethodInfo method)
			{
				if (method.DeclaringType == null)
					return null;

				// try get from cache
				var invoker = default(InvokeOperation);
				lock (InstanceMethods)
					if (InstanceMethods.TryGetValue(method, out invoker))
						return invoker;

				var creatorsByParamsCount = default(Dictionary<MethodCallSignature, InvokeOperationCreator>[]);
				lock (InstanceMethodCreators)
					if (InstanceMethodCreators.TryGetValue(method.DeclaringType, out creatorsByParamsCount) == false || creatorsByParamsCount == null)
						goto cacheAndReturn;

				var methodCallSignature = new MethodCallSignature(method);
				if (creatorsByParamsCount.Length < methodCallSignature.Count || creatorsByParamsCount[methodCallSignature.Count] == null)
					goto cacheAndReturn;

				var creatorsBySignature = creatorsByParamsCount[methodCallSignature.Count];
				var creator = default(InvokeOperationCreator);
				lock (creatorsBySignature)
					if (creatorsBySignature.TryGetValue(methodCallSignature, out creator) == false || creator == null)
						goto cacheAndReturn;

				invoker = creator(method, method.GetParameters());

				cacheAndReturn:
				// cache it
				lock (InstanceMethods)
					InstanceMethods[method] = invoker;

				return invoker;
			}

			public static void RegisterInstanceMethod<InstanceT, Arg1T, Arg2T, Arg3T, ResultT>()
			{
				const int PARAMS_INDEX = 3;

				var creatorsByParamsCount = default(Dictionary<MethodCallSignature, InvokeOperationCreator>[]);
				lock (InstanceMethodCreators)
					if (InstanceMethodCreators.TryGetValue(typeof(InstanceT), out creatorsByParamsCount) == false)
						InstanceMethodCreators[typeof(InstanceT)] = creatorsByParamsCount = new Dictionary<MethodCallSignature, InvokeOperationCreator>[4];

				var creatorsBySignature = creatorsByParamsCount[PARAMS_INDEX];
				if (creatorsBySignature == null)
					creatorsByParamsCount[PARAMS_INDEX] = creatorsBySignature = new Dictionary<MethodCallSignature, InvokeOperationCreator>();

				var methodCallSignature = new MethodCallSignature(typeof(Arg1T), "", typeof(Arg2T), "", typeof(Arg3T), "", typeof(ResultT));
				lock (creatorsBySignature)
					creatorsBySignature[methodCallSignature] = new InvokeOperationCreator(TryCreate<InstanceT, Arg1T, Arg2T, Arg3T, ResultT>);
			}
			public static void RegisterInstanceMethod<InstanceT, Arg1T, Arg2T, ResultT>()
			{
				const int PARAMS_INDEX = 2;

				var creatorsByParamsCount = default(Dictionary<MethodCallSignature, InvokeOperationCreator>[]);
				lock (InstanceMethodCreators)
					if (InstanceMethodCreators.TryGetValue(typeof(InstanceT), out creatorsByParamsCount) == false)
						InstanceMethodCreators[typeof(InstanceT)] = creatorsByParamsCount = new Dictionary<MethodCallSignature, InvokeOperationCreator>[4];

				var creatorsBySignature = creatorsByParamsCount[PARAMS_INDEX];
				if (creatorsBySignature == null)
					creatorsByParamsCount[PARAMS_INDEX] = creatorsBySignature = new Dictionary<MethodCallSignature, InvokeOperationCreator>();

				var methodCallSignature = new MethodCallSignature(typeof(Arg1T), "", typeof(Arg2T), "", typeof(ResultT));
				lock (creatorsBySignature)
					creatorsBySignature[methodCallSignature] = new InvokeOperationCreator(TryCreate<InstanceT, Arg1T, Arg2T, ResultT>);
			}
			public static void RegisterInstanceMethod<InstanceT, Arg1T, ResultT>()
			{
				const int PARAMS_INDEX = 1;

				var creatorsByParamsCount = default(Dictionary<MethodCallSignature, InvokeOperationCreator>[]);
				lock (InstanceMethodCreators)
					if (InstanceMethodCreators.TryGetValue(typeof(InstanceT), out creatorsByParamsCount) == false)
						InstanceMethodCreators[typeof(InstanceT)] = creatorsByParamsCount = new Dictionary<MethodCallSignature, InvokeOperationCreator>[4];

				var creatorsBySignature = creatorsByParamsCount[PARAMS_INDEX];
				if (creatorsBySignature == null)
					creatorsByParamsCount[PARAMS_INDEX] = creatorsBySignature = new Dictionary<MethodCallSignature, InvokeOperationCreator>();

				var methodCallSignature = new MethodCallSignature(typeof(Arg1T), "", typeof(ResultT));
				lock (creatorsBySignature)
					creatorsBySignature[methodCallSignature] = new InvokeOperationCreator(TryCreate<InstanceT, Arg1T, ResultT>);
			}
			public static void RegisterInstanceMethod<InstanceT, ResultT>()
			{
				const int PARAMS_INDEX = 0;

				var creatorsByParamsCount = default(Dictionary<MethodCallSignature, InvokeOperationCreator>[]);
				lock (InstanceMethodCreators)
					if (InstanceMethodCreators.TryGetValue(typeof(InstanceT), out creatorsByParamsCount) == false)
						InstanceMethodCreators[typeof(InstanceT)] = creatorsByParamsCount = new Dictionary<MethodCallSignature, InvokeOperationCreator>[4];

				var creatorsBySignature = creatorsByParamsCount[PARAMS_INDEX];
				if (creatorsBySignature == null)
					creatorsByParamsCount[PARAMS_INDEX] = creatorsBySignature = new Dictionary<MethodCallSignature, InvokeOperationCreator>();

				var methodCallSignature = new MethodCallSignature(typeof(ResultT));
				lock (creatorsBySignature)
					creatorsBySignature[methodCallSignature] = new InvokeOperationCreator(TryCreate<InstanceT, ResultT>);
			}

			private static InvokeOperation TryCreate<ResultT>(MethodInfo method, ParameterInfo[] parameters)
			{
				if (method == null) throw new ArgumentNullException("method");
				if (parameters == null) throw new ArgumentNullException("parameters");

				if (parameters.Length != 0 || method.ReturnType != typeof(ResultT))
					return null;

				var wrapper = new MethodCall(typeof(Func<ResultT>), method);

				// never happens, just for AOT
#pragma warning disable 1720
				if (parameters.Length == int.MaxValue)
				{
					wrapper.FuncInvoker<ResultT>(null, null);
					((Func<ResultT>)null).Invoke();
				}
#pragma warning restore 1720

				return wrapper.FuncInvoker<ResultT>;
			}
			private static InvokeOperation TryCreate<Arg1T, ResultT>(MethodInfo method, ParameterInfo[] parameters)
			{
				if (method == null) throw new ArgumentNullException("method");
				if (parameters == null) throw new ArgumentNullException("parameters");

				if (parameters.Length != 1 || method.ReturnType != typeof(ResultT) || parameters[0].ParameterType != typeof(Arg1T))
					return null;

				var wrapper = new MethodCall(typeof(Func<Arg1T, ResultT>), method);

				// never happens, just for AOT
#pragma warning disable 1720
				if (parameters.Length == int.MaxValue)
				{
					wrapper.FuncInvoker<Arg1T, ResultT>(null, null);
					((Func<Arg1T, ResultT>)null).Invoke(default(Arg1T));
				}
#pragma warning restore 1720

				return wrapper.FuncInvoker<Arg1T, ResultT>;
			}
			private static InvokeOperation TryCreate<Arg1T, Arg2T, ResultT>(MethodInfo method, ParameterInfo[] parameters)
			{
				if (method == null) throw new ArgumentNullException("method");
				if (parameters == null) throw new ArgumentNullException("parameters");

				if (parameters.Length != 2 || method.ReturnType != typeof(ResultT) || parameters[0].ParameterType != typeof(Arg1T) ||
					parameters[1].ParameterType != typeof(Arg2T))
					return null;

				var wrapper = new MethodCall(typeof(Func<Arg1T, Arg2T, ResultT>), method);

				// never happens, just for AOT
#pragma warning disable 1720
				if (parameters.Length == int.MaxValue)
				{
					wrapper.FuncInvoker<Arg1T, Arg2T, ResultT>(null, null);
					((Func<Arg1T, Arg2T, ResultT>)null).Invoke(default(Arg1T), default(Arg2T));
				}
#pragma warning restore 1720

				return wrapper.FuncInvoker<Arg1T, Arg2T, ResultT>;
			}
			private static InvokeOperation TryCreate<Arg1T, Arg2T, Arg3T, ResultT>(MethodInfo method, ParameterInfo[] parameters)
			{
				if (method == null) throw new ArgumentNullException("method");
				if (parameters == null) throw new ArgumentNullException("parameters");

				if (parameters.Length != 3 || method.ReturnType != typeof(ResultT) || parameters[0].ParameterType != typeof(Arg1T) ||
					parameters[1].ParameterType != typeof(Arg2T) || parameters[2].ParameterType != typeof(Arg3T))
					return null;

				var wrapper = new MethodCall(typeof(Func<Arg1T, Arg2T, Arg3T, ResultT>), method);

				// never happens, just for AOT
#pragma warning disable 1720
				if (parameters.Length == int.MaxValue)
				{
					wrapper.FuncInvoker<Arg1T, Arg2T, Arg3T, ResultT>(null, null);
					((Func<Arg1T, Arg2T, Arg3T, ResultT>)null).Invoke(default(Arg1T), default(Arg2T), default(Arg3T));
				}
#pragma warning restore 1720

				return wrapper.FuncInvoker<Arg1T, Arg2T, Arg3T, ResultT>;
			}
			private static InvokeOperation TryCreate<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>(MethodInfo method, ParameterInfo[] parameters)
			{
				if (method == null) throw new ArgumentNullException("method");
				if (parameters == null) throw new ArgumentNullException("parameters");

				if (parameters.Length != 4 || method.ReturnType != typeof(ResultT) || parameters[0].ParameterType != typeof(Arg1T) ||
					parameters[1].ParameterType != typeof(Arg2T) || parameters[2].ParameterType != typeof(Arg3T) || parameters[3].ParameterType != typeof(Arg4T))
					return null;

				var wrapper = new MethodCall(typeof(Func<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>), method);

				// never happens, just for AOT
#pragma warning disable 1720
				if (parameters.Length == int.MaxValue)
				{
					wrapper.FuncInvoker<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>(null, null);
					((Func<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>)null).Invoke(default(Arg1T), default(Arg2T), default(Arg3T), default(Arg4T));
				}
#pragma warning restore 1720

				return wrapper.FuncInvoker<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>;
			}
		}
	}
}
