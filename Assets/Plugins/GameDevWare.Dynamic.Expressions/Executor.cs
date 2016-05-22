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
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

// ReSharper disable UnusedParameter.Local

namespace GameDevWare.Dynamic.Expressions
{
	internal static partial class Executor
	{
		private const int LOCAL_OPERAND1 = 0;
		private const int LOCAL_OPERAND2 = 1;
		private const int LOCAL_FIRST_PARAMETER = 2; // this is offset of first parameter in Closure locals

		private sealed class Closure
		{
			public readonly object[] Constants;
			public readonly object[] Locals; // first two locals is reserved, third and others is parameters

			public Closure(object[] constants, object[] locals)
			{
				if (constants == null) throw new ArgumentNullException("constants");
				if (locals == null) throw new ArgumentNullException("locals");
				this.Constants = constants;
				this.Locals = locals;
			}

			public object Box<T>(T value)
			{
				return value;
			}

			public T Unbox<T>(object boxed)
			{
				//if (boxed is StrongBox<T>)
				//	return ((StrongBox<T>)boxed).Value;
				//else if (boxed is IStrongBox)
				//	boxed = ((IStrongBox)boxed).Value;

				if (boxed is T)
					return (T)boxed;
				else
					return (T)System.Convert.ChangeType(boxed, typeof(T));
			}

			public bool Is<T>(object boxed)
			{
				return boxed is T;
			}
		}
		private sealed class ConstantsCollector : ExpressionVisitor
		{
			public readonly List<ConstantExpression> Constants = new List<ConstantExpression>();

			protected override Expression VisitConstant(ConstantExpression c)
			{
				this.Constants.Add(c);
				return c;
			}
		}

		static Executor()
		{
			// AOT
			if (typeof(Executor).Name == string.Empty)
			{
				Expression(default(Expression), default(ConstantExpression[]), default(ParameterExpression[]));
				Conditional(default(ConditionalExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				Constant(default(ConstantExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				Invocation(default(InvocationExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				Lambda(default(LambdaExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				ListInit(default(ListInitExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				MemberAccess(default(MemberExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				MemberInit(default(MemberInitExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				MemberAssignments(default(IEnumerable<MemberBinding>), default(ConstantExpression[]), default(ParameterExpression[]));
				MemberListBindings(default(IEnumerable<MemberBinding>), default(ConstantExpression[]), default(ParameterExpression[]));
				MemberMemberBindings(default(IEnumerable<MemberBinding>), default(ConstantExpression[]), default(ParameterExpression[]));
				Call(default(MethodCallExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				New(default(NewExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				NewArray(default(NewArrayExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				Parameter(default(ParameterExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				TypeIs(default(TypeBinaryExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				TypeAs(default(UnaryExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				Convert(default(UnaryExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				Unary(default(UnaryExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				Binary(default(BinaryExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				CreateUnaryOperationFn(default(MethodInfo));
				CreateBinaryOperationFn(default(MethodInfo));
				WrapUnaryOperation(default(Type), default(string));
				WrapUnaryOperation(default(MethodInfo));
				WrapBinaryOperation(default(Type), default(string));
				WrapBinaryOperation(default(MethodInfo));
			}
		}

		public static Func<ResultT> Prepare<ResultT>(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
		{
			if (body == null) throw new ArgumentNullException("body");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constantsExprs = collector.Constants.ToArray();
			var localsExprs = parameters.ToArray();
			var compiledFn = Expression(body, constantsExprs, localsExprs);

			return (() =>
			{
				var constants = Array.ConvertAll(constantsExprs, c => c.Value);
				var locals = new object[] { null, null };
				var closure = new Closure(constants, locals);

				return (ResultT)compiledFn(closure);
			});
		}
		public static Func<Arg1T, ResultT> Prepare<Arg1T, ResultT>(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
		{
			if (body == null) throw new ArgumentNullException("body");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constantsExprs = collector.Constants.ToArray();
			var localsExprs = parameters.ToArray();
			var compiledFn = Expression(body, constantsExprs, localsExprs);
			var constants = Array.ConvertAll(constantsExprs, c => c.Value);

			return (arg1 =>
			{
				var locals = new object[] { null, null, arg1 };
				var closure = new Closure(constants, locals);

				return (ResultT)compiledFn(closure);
			});
		}
		public static Func<Arg1T, Arg2T, ResultT> Prepare<Arg1T, Arg2T, ResultT>(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
		{
			if (body == null) throw new ArgumentNullException("body");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constantsExprs = collector.Constants.ToArray();
			var localsExprs = parameters.ToArray();
			var compiledFn = Expression(body, constantsExprs, localsExprs);

			return ((arg1, arg2) =>
			{
				var constants = Array.ConvertAll(constantsExprs, c => c.Value);
				var locals = new object[] { null, null, arg1, arg2 };
				var closure = new Closure(constants, locals);

				return (ResultT)compiledFn(closure);
			});
		}
		public static Func<Arg1T, Arg2T, Arg3T, ResultT> Prepare<Arg1T, Arg2T, Arg3T, ResultT>(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
		{
			if (body == null) throw new ArgumentNullException("body");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constantsExprs = collector.Constants.ToArray();
			var localsExprs = parameters.ToArray();
			var compiledFn = Expression(body, constantsExprs, localsExprs);

			return ((arg1, arg2, arg3) =>
			{
				var constants = Array.ConvertAll(constantsExprs, c => c.Value);
				var locals = new object[] { null, null, arg1, arg2, arg3 };
				var closure = new Closure(constants, locals);

				return (ResultT)compiledFn(closure);
			});
		}
		public static Func<Arg1T, Arg2T, Arg3T, Arg4T, ResultT> Prepare<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
		{
			if (body == null) throw new ArgumentNullException("body");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constantsExprs = collector.Constants.ToArray();
			var localsExprs = parameters.ToArray();
			var compiledFn = Expression(body, constantsExprs, localsExprs);

			return ((arg1, arg2, arg3, arg4) =>
			{
				var constants = Array.ConvertAll(constantsExprs, c => c.Value);
				var locals = new object[] { null, null, arg1, arg2, arg3, arg4 };
				var closure = new Closure(constants, locals);

				return (ResultT)compiledFn(closure);
			});
		}

		public static void RegisterForFastCall<InstanceT, Arg1T, Arg2T, Arg3T, ResultT>()
		{
			MethodCall.RegisterInstanceMethod<InstanceT, Arg1T, Arg2T, Arg3T, ResultT>();
		}
		public static void RegisterForFastCall<InstanceT, Arg1T, Arg2T, ResultT>()
		{
			MethodCall.RegisterInstanceMethod<InstanceT, Arg1T, Arg2T, ResultT>();
		}
		public static void RegisterForFastCall<InstanceT, Arg1T, ResultT>()
		{
			MethodCall.RegisterInstanceMethod<InstanceT, Arg1T, ResultT>();
		}
		public static void RegisterForFastCall<InstanceT, ResultT>()
		{
			MethodCall.RegisterInstanceMethod<InstanceT, ResultT>();
		}

		private static Func<Closure, object> Expression(Expression exp, ConstantExpression[] constantsExprs,
			ParameterExpression[] localsExprs)
		{
			if (exp == null)
				return (closure => null);

			switch (exp.NodeType)
			{
				case ExpressionType.Add:
				case ExpressionType.AddChecked:
				case ExpressionType.And:
				case ExpressionType.AndAlso:
				case ExpressionType.ArrayIndex:
				case ExpressionType.Coalesce:
				case ExpressionType.Divide:
				case ExpressionType.Equal:
				case ExpressionType.ExclusiveOr:
				case ExpressionType.GreaterThan:
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.LeftShift:
				case ExpressionType.LessThan:
				case ExpressionType.LessThanOrEqual:
				case ExpressionType.Modulo:
				case ExpressionType.Multiply:
				case ExpressionType.MultiplyChecked:
				case ExpressionType.NotEqual:
				case ExpressionType.Or:
				case ExpressionType.OrElse:
				case ExpressionType.Power:
				case ExpressionType.RightShift:
				case ExpressionType.Subtract:
				case ExpressionType.SubtractChecked:
					return Binary((BinaryExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.ArrayLength:
				case ExpressionType.Negate:
				case ExpressionType.UnaryPlus:
				case ExpressionType.NegateChecked:
				case ExpressionType.Not:
					return Unary((UnaryExpression)exp, constantsExprs, localsExprs);
				case ExpressionType.Quote:
					return closure => ((UnaryExpression)exp).Operand;
				case ExpressionType.Call:
					return Call((MethodCallExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.Conditional:
					return Conditional((ConditionalExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.Constant:
					return Constant((ConstantExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.Invoke:
					return Invocation((InvocationExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.Lambda:
					return Lambda((LambdaExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.ListInit:
					return ListInit((ListInitExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.MemberAccess:
					return MemberAccess((MemberExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.MemberInit:
					return MemberInit((MemberInitExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.New:
					return New((NewExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.NewArrayInit:
				case ExpressionType.NewArrayBounds:
					return NewArray((NewArrayExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.Parameter:
					return Parameter((ParameterExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
					return Convert((UnaryExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.TypeAs:
					return TypeAs((UnaryExpression)exp, constantsExprs, localsExprs);

				case ExpressionType.TypeIs:
					return TypeIs((TypeBinaryExpression)exp, constantsExprs, localsExprs);
			}
			throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_COMPIL_UNKNOWNEXPRTYPE, exp.Type));
		}

		private static Func<Closure, object> Conditional(ConditionalExpression conditionalExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			var trueFn = Expression(conditionalExpression.IfTrue, constantsExprs, localsExprs);
			var falseFn = Expression(conditionalExpression.IfFalse, constantsExprs, localsExprs);
			var testFn = Expression(conditionalExpression.Test, constantsExprs, localsExprs);

			return closure => closure.Unbox<bool>(testFn(closure)) ? trueFn(closure) : falseFn(closure);
		}

		private static Func<Closure, object> Constant(ConstantExpression constantExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			return closure => closure.Constants[Array.IndexOf(constantsExprs, constantExpression)];
		}

		private static Func<Closure, object> Invocation(InvocationExpression invocationExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			var target = Expression(invocationExpression.Expression, constantsExprs, localsExprs);
			var argsFns = invocationExpression.Arguments.Select(e => Expression(e, constantsExprs, localsExprs)).ToArray();

			return closure =>
			{
				var targetDelegate = (Delegate)target(closure);
				var args = new object[argsFns.Length];
				for (var i = 0; i < args.Length; i++)
					args[i] = argsFns[i](closure);

				return targetDelegate.DynamicInvoke(args);
			};
		}

		private static Func<Closure, object> Lambda(LambdaExpression lambdaExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			throw new NotSupportedException();
		}

		private static Func<Closure, object> ListInit(ListInitExpression listInitExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			var newFn = New(listInitExpression.NewExpression, constantsExprs, localsExprs);
			var listInits =
			(
				from elemInit in listInitExpression.Initializers
				let argsFns = elemInit.Arguments.Select(e => Expression(e, constantsExprs, localsExprs)).ToArray()
				select new { addMethod = elemInit.AddMethod, argsFns }
			).ToArray();

			return closure =>
			{
				var list = newFn(closure);
				if (listInits.Length == 0) return list;

				foreach (var listInit in listInits)
				{
					var addMethod = listInit.addMethod;
					var addArgs = new object[listInit.argsFns.Length];

					for (var i = 0; i < listInit.argsFns.Length; i++)
						addArgs[i] = listInit.argsFns[i](closure);

					addMethod.Invoke(list, addArgs);
				}
				return list;
			};
		}

		private static Func<Closure, object> MemberAccess(MemberExpression memberExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			var valueFn = Expression(memberExpression.Expression, constantsExprs, localsExprs);

			return closure =>
			{
				var value = valueFn(closure);
				var member = memberExpression.Member;
				if (member is FieldInfo)
					return ((FieldInfo)member).GetValue(value);
				else
					return ((PropertyInfo)member).GetValue(value, null);
			};
		}

		private static Func<Closure, object> MemberInit(MemberInitExpression memberInitExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			var newFn = New(memberInitExpression.NewExpression, constantsExprs, localsExprs);
			var memberAssignments = MemberAssignments(memberInitExpression.Bindings, constantsExprs, localsExprs);
			var listBindings = MemberListBindings(memberInitExpression.Bindings, constantsExprs, localsExprs);
			var memberMemberBindings = MemberMemberBindings(memberInitExpression.Bindings, constantsExprs, localsExprs);

			return closure =>
			{

				var instance = newFn(closure);

				closure.Locals[LOCAL_OPERAND1] = instance;
				memberAssignments(closure);

				closure.Locals[LOCAL_OPERAND1] = instance;
				listBindings(closure);

				closure.Locals[LOCAL_OPERAND1] = instance;
				memberMemberBindings(closure);

				closure.Locals[LOCAL_OPERAND1] = null;
				return instance;
			};
		}

		private static Func<Closure, object> MemberAssignments(IEnumerable<MemberBinding> bindings,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			var assignFns = (
				from bind in bindings
				let assign = bind as MemberAssignment
				where assign != null
				select new { member = bind.Member, valueFn = Expression(assign.Expression, constantsExprs, localsExprs) }
			).ToArray();

			return closure =>
			{
				var instance = closure.Locals[LOCAL_OPERAND1];
				if (assignFns.Length == 0) return instance;

				foreach (var assignFn in assignFns)
				{
					var member = assignFn.member;
					var valueFn = assignFn.valueFn;

					if (member is FieldInfo)
						((FieldInfo)member).SetValue(instance, valueFn(closure));
					else
						((PropertyInfo)member).SetValue(instance, valueFn(closure), null);
				}
				return instance;
			};
		}

		private static Func<Closure, object> MemberListBindings(IEnumerable<MemberBinding> bindings,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			var listBindGroups =
			(
				from bind in bindings
				let listBind = bind as MemberListBinding
				where listBind != null
				from elemInit in listBind.Initializers
				let argsFns = elemInit.Arguments.Select(e => Expression(e, constantsExprs, localsExprs)).ToArray()
				select new { member = bind.Member, addMethod = elemInit.AddMethod, argsFns }
			).ToLookup(m => m.member);

			return closure =>
			{
				var instance = closure.Locals[LOCAL_OPERAND1];
				if (listBindGroups.Count == 0) return instance;

				foreach (var listBindGroup in listBindGroups)
				{
					var member = listBindGroup.Key;
					var addTarget = default(object);
					if (member is FieldInfo)
						addTarget = ((FieldInfo)member).GetValue(instance);
					else
						addTarget = ((PropertyInfo)member).GetValue(instance, null);
					if (addTarget == null) throw new NullReferenceException();

					foreach (var bindGroup in listBindGroup)
					{
						var addMethod = bindGroup.addMethod;
						var addArgs = new object[bindGroup.argsFns.Length];
						for (var i = 0; i < bindGroup.argsFns.Length; i++)
							addArgs[i] = bindGroup.argsFns[i](closure);
						addMethod.Invoke(addTarget, addArgs);
					}
				}
				return instance;
			};
		}

		private static Func<Closure, object> MemberMemberBindings(IEnumerable<MemberBinding> bindings,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			var bindGroups =
			(
				from bind in bindings
				let memberBinding = bind as MemberMemberBinding
				where memberBinding != null
				let memberAssignments = MemberAssignments(memberBinding.Bindings, constantsExprs, localsExprs)
				let listBindings = MemberListBindings(memberBinding.Bindings, constantsExprs, localsExprs)
				let memberMemberBindings = MemberMemberBindings(memberBinding.Bindings, constantsExprs, localsExprs)
				select new { member = memberBinding.Member, memberAssignments, listBindings, memberMemberBindings }
			).ToLookup(m => m.member);

			return closure =>
			{
				var instance = closure.Locals[LOCAL_OPERAND1];
				if (bindGroups.Count == 0) return instance;

				foreach (var bindGroup in bindGroups)
				{
					var member = bindGroup.Key;
					var bindTarget = default(object);
					if (member is FieldInfo)
						bindTarget = ((FieldInfo)member).GetValue(instance);
					else
						bindTarget = ((PropertyInfo)member).GetValue(instance, null);
					if (bindTarget == null) throw new NullReferenceException();

					foreach (var bind in bindGroup)
					{
						closure.Locals[LOCAL_OPERAND1] = bindTarget;
						bind.memberAssignments(closure);

						closure.Locals[LOCAL_OPERAND1] = bindTarget;
						bind.listBindings(closure);

						closure.Locals[LOCAL_OPERAND1] = bindTarget;
						bind.memberMemberBindings(closure);
					}
				}
				return instance;
			};
		}

		private static Func<Closure, object> Call(MethodCallExpression methodCallExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			var targetFn = Expression(methodCallExpression.Object, constantsExprs, localsExprs);
			var argsFns = methodCallExpression.Arguments.Select(e => Expression(e, constantsExprs, localsExprs)).ToArray();
			var invokeFn = MethodCall.TryCreate(methodCallExpression.Method);

			if (invokeFn != null)
			{
				return closure => { return invokeFn(closure, argsFns); };
			}
			else
			{
				return closure =>
				{
					var target = targetFn(closure);
					var args = new object[argsFns.Length];
					for (var i = 0; i < args.Length; i++)
						args[i] = argsFns[i](closure);

					return methodCallExpression.Method.Invoke(target, args);
				};
			}
		}

		private static Func<Closure, object> New(NewExpression newExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			var valuesFns = newExpression.Arguments.Select(e => Expression(e, constantsExprs, localsExprs)).ToArray();

			return closure =>
			{
				var source = new object[valuesFns.Length];
				for (var i = 0; i < source.Length; i++)
					source[i] = valuesFns[i](closure);

				var args = source.Take(newExpression.Constructor.GetParameters().Length).ToArray();
				var instance = Activator.CreateInstance(newExpression.Type, args);

				if (newExpression.Members != null)
				{
					for (var j = 0; j < newExpression.Members.Count; j++)
					{
						var member = newExpression.Members[j];
						if (member is FieldInfo)
							((FieldInfo)member).SetValue(instance, source[args.Length + j]);
						else
							((PropertyInfo)member).SetValue(instance, source[args.Length + j], null);
					}
				}
				return instance;
			};
		}

		private static Func<Closure, object> NewArray(NewArrayExpression newArrayExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			if (newArrayExpression.NodeType == ExpressionType.NewArrayBounds)
			{
				var lengthFns = newArrayExpression.Expressions.Select(e => Expression(e, constantsExprs, localsExprs)).ToArray();

				return closure =>
				{
					var lengths = new int[lengthFns.Length];
					for (var i = 0; i < lengthFns.Length; i++)
						lengths[i] = closure.Unbox<int>(lengthFns[i](closure));

					// ReSharper disable once AssignNullToNotNullAttribute
					var array = Array.CreateInstance(newArrayExpression.Type.GetElementType(), lengths);
					return array;
				};
			}
			else
			{
				var valuesFns = newArrayExpression.Expressions.Select(e => Expression(e, constantsExprs, localsExprs)).ToArray();

				return closure =>
				{
					// ReSharper disable once AssignNullToNotNullAttribute
					var array = Array.CreateInstance(newArrayExpression.Type.GetElementType(), valuesFns.Length);
					for (var i = 0; i < valuesFns.Length; i++)
						array.SetValue(valuesFns[i](closure), i);

					return array;
				};
			}
		}

		private static Func<Closure, object> Parameter(ParameterExpression parameterExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			return
				closure => closure.Locals[LOCAL_FIRST_PARAMETER + Array.IndexOf(localsExprs, parameterExpression)];
		}

		private static Func<Closure, object> TypeIs(TypeBinaryExpression typeBinaryExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			var valueFn = Expression(typeBinaryExpression.Expression, constantsExprs, localsExprs);

			return closure =>
			{
				var value = valueFn(closure);
				if (value == null) return false;

				return typeBinaryExpression.TypeOperand.IsAssignableFrom(value.GetType());
			};
		}

		private static Func<Closure, object> TypeAs(UnaryExpression typeAsExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			if (typeAsExpression.Type.IsValueType)
				return Convert(typeAsExpression, constantsExprs, localsExprs);

			var valueFn = Expression(typeAsExpression.Operand, constantsExprs, localsExprs);
			return closure =>
			{
				var value = valueFn(closure);
				if (value == null)
					return null;

				if (typeAsExpression.Type.IsAssignableFrom(value.GetType()) == false)
					return null;

				return value;
			};
		}

		private static Func<Closure, object> Convert(UnaryExpression convertExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			var valueFn = Expression(convertExpression.Operand, constantsExprs, localsExprs);
			var convertOperator = WrapUnaryOperation(convertExpression.Method) ?? WrapUnaryOperation(
				convertExpression.Type
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.FirstOrDefault(m =>
					(string.Equals(m.Name, "op_Explicit", StringComparison.Ordinal) || string.Equals(m.Name, "op_Implicit", StringComparison.Ordinal)) &&
					m.ReturnType == convertExpression.Type &&
					m.GetParameters().Length == 1 &&
					m.GetParameters()[0].ParameterType == convertExpression.Operand.Type)
			);
			var toType = Nullable.GetUnderlyingType(convertExpression.Type) ?? convertExpression.Type;
			var toIsNullable = Nullable.GetUnderlyingType(convertExpression.Type) != null;
			var fromType = Nullable.GetUnderlyingType(convertExpression.Operand.Type) ?? convertExpression.Operand.Type;
			var fromIsNullable = IsNullable(convertExpression.Operand);

			return closure =>
			{
				var value = closure.Unbox<object>(valueFn(closure));
				if (value == null && (convertExpression.Type.IsValueType == false || toIsNullable))
					return null;

				var convertType = convertExpression.NodeType;
				if (convertType != ExpressionType.Convert)
					convertType = ExpressionType.ConvertChecked;

				// un-box
				if ((fromType == typeof(object) || fromType == typeof(ValueType) || fromType.IsInterface) && toType.IsValueType)
				{
					// null un-box
					if (value == null) throw new NullReferenceException("Attempt to unbox a null value.");
					// typecheck for un-box
					if (value.GetType() == toType)
						return value;
					throw new InvalidCastException();
				}
				// box
				else if (fromType.IsValueType && (toType == typeof(object) || toType == typeof(ValueType) || toType.IsInterface))
				{
					// typecheck for box
					return toType.IsAssignableFrom(value.GetType()) ? value : null;
				}
				// to enum
				else if (toType.IsEnum && (fromType == typeof(byte) || fromType == typeof(sbyte) ||
					fromType == typeof(short) || fromType == typeof(ushort) ||
					fromType == typeof(int) || fromType == typeof(uint) ||
					fromType == typeof(long) || fromType == typeof(ulong)))
				{
					if (value == null) throw new NullReferenceException("Attempt to unbox a null value.");

					value = Intrinsics.Convert(closure, value, Enum.GetUnderlyingType(toType), convertExpression.NodeType, null);
					return Enum.ToObject(toType, closure.Unbox<object>(value));
				}
				// from enum
				else if (fromType.IsEnum && (toType == typeof(byte) || toType == typeof(sbyte) ||
					toType == typeof(short) || toType == typeof(ushort) ||
					toType == typeof(int) || toType == typeof(uint) ||
					toType == typeof(long) || toType == typeof(ulong)))
				{
					if (value == null)
						throw new NullReferenceException("Attempt to unbox a null value.");

					value = System.Convert.ChangeType(value, Enum.GetUnderlyingType(fromType));
					value = Intrinsics.Convert(closure, value, toType, convertExpression.NodeType, null);
					return value;
				}
				// from nullable
				if (toType.IsValueType && fromIsNullable)
				{
					if (value == null) throw new NullReferenceException("Attempt to unbox a null value.");

					value = Intrinsics.Convert(closure, value, Nullable.GetUnderlyingType(toType) ?? toType, convertExpression.NodeType, null);
				}
				else if (toType.IsAssignableFrom(fromType))
					return value;

				return Intrinsics.Convert(closure, value, toType, convertType, convertOperator);
			};
		}

		private static Func<Closure, object> Unary(UnaryExpression unaryExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			var valueFn = Expression(unaryExpression.Operand, constantsExprs, localsExprs);
			var opUnaryNegation = WrapUnaryOperation(unaryExpression.Method) ?? WrapUnaryOperation(unaryExpression.Operand.Type, "op_UnaryNegation");
			var opUnaryPlus = WrapUnaryOperation(unaryExpression.Method) ?? WrapUnaryOperation(unaryExpression.Operand.Type, "op_UnaryPlus");
			var opOnesComplement = WrapUnaryOperation(unaryExpression.Method) ?? WrapUnaryOperation(unaryExpression.Operand.Type, "op_OnesComplement");
			var isNullable = IsNullable(unaryExpression.Operand);

			return closure =>
			{
				var operand = valueFn(closure);

				if (isNullable && operand == null && unaryExpression.NodeType != ExpressionType.ArrayLength)
					return null;

				switch (unaryExpression.NodeType)
				{
					case ExpressionType.Negate:
					case ExpressionType.NegateChecked:
						return Intrinsics.UnaryOperation(closure, operand, unaryExpression.NodeType, opUnaryNegation);
					case ExpressionType.UnaryPlus:
						return Intrinsics.UnaryOperation(closure, operand, unaryExpression.NodeType, opUnaryPlus);
					case ExpressionType.Not:
						return Intrinsics.UnaryOperation(closure, operand, unaryExpression.NodeType, opOnesComplement);
					case ExpressionType.ArrayLength:
						return closure.Unbox<Array>(operand).Length;
				}

				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_COMPIL_UNKNOWNUNARYEXPRTYPE, unaryExpression.Type));
			};
		}

		private static Func<Closure, object> Binary(BinaryExpression binaryExpression,
			ConstantExpression[] constantsExprs, ParameterExpression[] localsExprs)
		{
			var leftFn = Expression(binaryExpression.Left, constantsExprs, localsExprs);
			var rightFn = Expression(binaryExpression.Right, constantsExprs, localsExprs);
			var opAddition = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_Addition");
			var opBitwiseAnd = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_BitwiseAnd");
			var opDivision = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_Division");
			var opEquality = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_Equality");
			var opExclusiveOr = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_ExclusiveOr");
			var opGreaterThan = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_GreaterThan");
			var opGreaterThanOrEqual = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_GreaterThanOrEqual");
			var opLessThan = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_LessThan");
			var opLessThanOrEqual = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_LessThanOrEqual");
			var opModulus = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_Modulus");
			var opMultiply = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_Multiply");
			var opBitwiseOr = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_BitwiseOr");
			var opSubtraction = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_Subtraction");
			var isNullable = IsNullable(binaryExpression.Left) || IsNullable(binaryExpression.Right);

			return closure =>
			{
				switch (binaryExpression.NodeType)
				{
					case ExpressionType.AndAlso:
						return closure.Unbox<bool>(leftFn(closure)) && closure.Unbox<bool>(rightFn(closure));
					case ExpressionType.OrElse:
						return closure.Unbox<bool>(leftFn(closure)) || closure.Unbox<bool>(rightFn(closure));
				}

				var left = leftFn(closure);
				var right = rightFn(closure);

				if
				(
					isNullable &&
					(left == null || right == null) &&
					binaryExpression.NodeType != ExpressionType.Coalesce &&
					binaryExpression.NodeType != ExpressionType.ArrayIndex
				)
				{
					// ReSharper disable once SwitchStatementMissingSomeCases
					switch (binaryExpression.NodeType)
					{
						case ExpressionType.Equal: return closure.Box(left == right);
						case ExpressionType.NotEqual: return closure.Box(left != right);
						case ExpressionType.GreaterThan:
						case ExpressionType.GreaterThanOrEqual:
						case ExpressionType.LessThan:
						case ExpressionType.LessThanOrEqual: return closure.Box(false);
						default: return null;
					}
				}

				switch (binaryExpression.NodeType)
				{
					case ExpressionType.Add:
					case ExpressionType.AddChecked:
						return Intrinsics.BinaryOperation(closure, left, right, binaryExpression.NodeType, opAddition);
					case ExpressionType.And:
						return Intrinsics.BinaryOperation(closure, left, right, binaryExpression.NodeType, opBitwiseAnd);
					case ExpressionType.ArrayIndex:
						return closure.Is<int[]>(right)
							? closure.Unbox<Array>(left).GetValue(closure.Unbox<int[]>(right))
							: closure.Unbox<Array>(left).GetValue(closure.Unbox<int>(right));
					case ExpressionType.Coalesce:
						return left ?? right;
					case ExpressionType.Divide:
						return Intrinsics.BinaryOperation(closure, left, right, binaryExpression.NodeType, opDivision);
					case ExpressionType.Equal:
						return Intrinsics.BinaryOperation(closure, left, right, binaryExpression.NodeType, opEquality);
					case ExpressionType.NotEqual:
						return closure.Box(closure.Unbox<bool>(Intrinsics.BinaryOperation(closure, left, right, ExpressionType.Equal, opEquality)) == false);
					case ExpressionType.ExclusiveOr:
						return Intrinsics.BinaryOperation(closure, left, right, binaryExpression.NodeType, opExclusiveOr);
					case ExpressionType.GreaterThan:
						return Intrinsics.BinaryOperation(closure, left, right, binaryExpression.NodeType, opGreaterThan);
					case ExpressionType.GreaterThanOrEqual:
						return Intrinsics.BinaryOperation(closure, left, right, binaryExpression.NodeType, opGreaterThanOrEqual);
					case ExpressionType.LeftShift:
					case ExpressionType.Power:
					case ExpressionType.RightShift:
						return Intrinsics.BinaryOperation(closure, left, right, binaryExpression.NodeType, null);
					case ExpressionType.LessThan:
						return Intrinsics.BinaryOperation(closure, left, right, binaryExpression.NodeType, opLessThan);
					case ExpressionType.LessThanOrEqual:
						return Intrinsics.BinaryOperation(closure, left, right, binaryExpression.NodeType, opLessThanOrEqual);
					case ExpressionType.Modulo:
						return Intrinsics.BinaryOperation(closure, left, right, binaryExpression.NodeType, opModulus);
					case ExpressionType.Multiply:
					case ExpressionType.MultiplyChecked:
						return Intrinsics.BinaryOperation(closure, left, right, binaryExpression.NodeType, opMultiply);
					case ExpressionType.Or:
						return Intrinsics.BinaryOperation(closure, left, right, binaryExpression.NodeType, opBitwiseOr);
					case ExpressionType.Subtract:
					case ExpressionType.SubtractChecked:
						return Intrinsics.BinaryOperation(closure, left, right, binaryExpression.NodeType, opSubtraction);
				}

				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_COMPIL_UNKNOWNBINARYEXPRTYPE, binaryExpression.Type));
			};
		}

		private static bool IsNullable(Expression expression)
		{
			var constantExpression = expression as ConstantExpression;
			if (constantExpression != null && constantExpression.Type == typeof(Object) && constantExpression.Value == null)
				return true;

			return Nullable.GetUnderlyingType(expression.Type) != null;
		}
	}
}
