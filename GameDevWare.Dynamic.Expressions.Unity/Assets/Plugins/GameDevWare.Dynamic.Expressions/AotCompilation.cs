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

namespace GameDevWare.Dynamic.Expressions
{
	public static class AotCompilation
	{
		public static readonly bool AotRuntime;

		static AotCompilation()
		{
			try { Expression.Lambda<Func<bool>>(Expression.Constant(true)).Compile(); }
			catch (Exception) { AotRuntime = true; }

			// AOT
			if (typeof(Expression).Name == string.Empty)
			{
				// ReSharper disable AssignNullToNotNullAttribute
				Expression.TypeIs(default(Expression), default(Type));
				Expression.MakeUnary(default(ExpressionType), default(Expression), default(Type));
				Expression.MakeUnary(default(ExpressionType), default(Expression), default(Type), default(System.Reflection.MethodInfo));
				Expression.Negate(default(Expression));
				Expression.Negate(default(Expression), default(System.Reflection.MethodInfo));
				Expression.UnaryPlus(default(Expression));
				Expression.UnaryPlus(default(Expression), default(System.Reflection.MethodInfo));
				Expression.NegateChecked(default(Expression));
				Expression.NegateChecked(default(Expression), default(System.Reflection.MethodInfo));
				Expression.Not(default(Expression));
				Expression.Not(default(Expression), default(System.Reflection.MethodInfo));
				Expression.TypeAs(default(Expression), default(Type));
				Expression.Convert(default(Expression), default(Type));
				Expression.Convert(default(Expression), default(Type), default(System.Reflection.MethodInfo));
				Expression.ConvertChecked(default(Expression), default(Type));
				Expression.ConvertChecked(default(Expression), default(Type), default(System.Reflection.MethodInfo));
				Expression.ArrayLength(default(Expression));
				Expression.Quote(default(Expression));
				Expression.ListInit(default(NewExpression), default(ElementInit[]));
				Expression.ListInit(default(NewExpression), default(System.Collections.Generic.IEnumerable<ElementInit>));
				Expression.Bind(default(System.Reflection.MemberInfo), default(Expression));
				Expression.Bind(default(System.Reflection.MethodInfo), default(Expression));
				Expression.Field(default(Expression), default(System.Reflection.FieldInfo));
				Expression.Field(default(Expression), default(string));
				Expression.Property(default(Expression), default(string));
				Expression.Property(default(Expression), default(System.Reflection.PropertyInfo));
				Expression.Property(default(Expression), default(System.Reflection.MethodInfo));
				Expression.PropertyOrField(default(Expression), default(string));
				Expression.MakeMemberAccess(default(Expression), default(System.Reflection.MemberInfo));
				Expression.MemberInit(default(NewExpression), default(MemberBinding[]));
				Expression.MemberInit(default(NewExpression), default(System.Collections.Generic.IEnumerable<MemberBinding>));
				Expression.ListBind(default(System.Reflection.MemberInfo), default(ElementInit[]));
				Expression.ListBind(default(System.Reflection.MemberInfo), default(System.Collections.Generic.IEnumerable<ElementInit>));
				Expression.ListBind(default(System.Reflection.MethodInfo), default(ElementInit[]));
				Expression.ListBind(default(System.Reflection.MethodInfo), default(System.Collections.Generic.IEnumerable<ElementInit>));
				Expression.MemberBind(default(System.Reflection.MemberInfo), default(MemberBinding[]));
				Expression.MemberBind(default(System.Reflection.MemberInfo), default(System.Collections.Generic.IEnumerable<MemberBinding>));
				Expression.MemberBind(default(System.Reflection.MethodInfo), default(MemberBinding[]));
				Expression.MemberBind(default(System.Reflection.MethodInfo), default(System.Collections.Generic.IEnumerable<MemberBinding>));
				Expression.Call(default(System.Reflection.MethodInfo), default(Expression[]));
				Expression.Call(default(Expression), default(System.Reflection.MethodInfo));
				Expression.Call(default(Expression), default(System.Reflection.MethodInfo), default(Expression[]));
				Expression.Call(default(Expression), default(System.Reflection.MethodInfo), default(System.Collections.Generic.IEnumerable<Expression>));
				Expression.Call(default(Expression), default(string), default(Type[]), default(Expression[]));
				Expression.Call(default(Type), default(string), default(Type[]), default(Expression[]));
				Expression.ArrayIndex(default(Expression), default(Expression[]));
				Expression.ArrayIndex(default(Expression), default(System.Collections.Generic.IEnumerable<Expression>));
				Expression.NewArrayInit(default(Type), default(Expression[]));
				Expression.NewArrayInit(default(Type), default(System.Collections.Generic.IEnumerable<Expression>));
				Expression.NewArrayBounds(default(Type), default(Expression[]));
				Expression.NewArrayBounds(default(Type), default(System.Collections.Generic.IEnumerable<Expression>));
				Expression.New(default(System.Reflection.ConstructorInfo));
				Expression.New(default(System.Reflection.ConstructorInfo), default(Expression[]));
				Expression.New(default(System.Reflection.ConstructorInfo), default(System.Collections.Generic.IEnumerable<Expression>));
				Expression.New(default(System.Reflection.ConstructorInfo), default(System.Collections.Generic.IEnumerable<Expression>), default(System.Collections.Generic.IEnumerable<System.Reflection.MemberInfo>));
				Expression.New(default(System.Reflection.ConstructorInfo), default(System.Collections.Generic.IEnumerable<Expression>), default(System.Reflection.MemberInfo[]));
				Expression.New(default(Type));
				Expression.Parameter(default(Type), default(string));
				Expression.Invoke(default(Expression), default(Expression[]));
				Expression.Invoke(default(Expression), default(System.Collections.Generic.IEnumerable<Expression>));
				Expression.Lambda(default(Expression), default(ParameterExpression[]));
				Expression.Lambda(default(Type), default(Expression), default(System.Collections.Generic.IEnumerable<ParameterExpression>));
				Expression.Lambda(default(Type), default(Expression), default(ParameterExpression[]));
				Expression.GetFuncType(default(Type[]));
				Expression.GetActionType(default(Type[]));
				Expression.ListInit(default(NewExpression), default(Expression[]));
				Expression.ListInit(default(NewExpression), default(System.Collections.Generic.IEnumerable<Expression>));
				Expression.ListInit(default(NewExpression), default(System.Reflection.MethodInfo), default(Expression[]));
				Expression.ListInit(default(NewExpression), default(System.Reflection.MethodInfo), default(System.Collections.Generic.IEnumerable<Expression>));
				Expression.LeftShift(default(Expression), default(Expression));
				Expression.LeftShift(default(Expression), default(Expression), default(System.Reflection.MethodInfo));
				Expression.RightShift(default(Expression), default(Expression));
				Expression.RightShift(default(Expression), default(Expression), default(System.Reflection.MethodInfo));
				Expression.And(default(Expression), default(Expression));
				Expression.And(default(Expression), default(Expression), default(System.Reflection.MethodInfo));
				Expression.Or(default(Expression), default(Expression));
				Expression.Or(default(Expression), default(Expression), default(System.Reflection.MethodInfo));
				Expression.ExclusiveOr(default(Expression), default(Expression));
				Expression.ExclusiveOr(default(Expression), default(Expression), default(System.Reflection.MethodInfo));
				Expression.Power(default(Expression), default(Expression));
				Expression.Power(default(Expression), default(Expression), default(System.Reflection.MethodInfo));
				Expression.ArrayIndex(default(Expression), default(Expression));
				Expression.Condition(default(Expression), default(Expression), default(Expression));
				Expression.Constant(default(object));
				Expression.Constant(default(object), default(Type));
				Expression.ElementInit(default(System.Reflection.MethodInfo), default(Expression[]));
				Expression.ElementInit(default(System.Reflection.MethodInfo), default(System.Collections.Generic.IEnumerable<Expression>));
				Expression.MakeBinary(default(ExpressionType), default(Expression), default(Expression));
				Expression.MakeBinary(default(ExpressionType), default(Expression), default(Expression), default(bool), default(System.Reflection.MethodInfo));
				Expression.MakeBinary(default(ExpressionType), default(Expression), default(Expression), default(bool), default(System.Reflection.MethodInfo), default(LambdaExpression));
				Expression.Equal(default(Expression), default(Expression));
				Expression.Equal(default(Expression), default(Expression), default(bool), default(System.Reflection.MethodInfo));
				Expression.NotEqual(default(Expression), default(Expression));
				Expression.NotEqual(default(Expression), default(Expression), default(bool), default(System.Reflection.MethodInfo));
				Expression.GreaterThan(default(Expression), default(Expression));
				Expression.GreaterThan(default(Expression), default(Expression), default(bool), default(System.Reflection.MethodInfo));
				Expression.LessThan(default(Expression), default(Expression));
				Expression.LessThan(default(Expression), default(Expression), default(bool), default(System.Reflection.MethodInfo));
				Expression.GreaterThanOrEqual(default(Expression), default(Expression));
				Expression.GreaterThanOrEqual(default(Expression), default(Expression), default(bool), default(System.Reflection.MethodInfo));
				Expression.LessThanOrEqual(default(Expression), default(Expression));
				Expression.LessThanOrEqual(default(Expression), default(Expression), default(bool), default(System.Reflection.MethodInfo));
				Expression.AndAlso(default(Expression), default(Expression));
				Expression.AndAlso(default(Expression), default(Expression), default(System.Reflection.MethodInfo));
				Expression.OrElse(default(Expression), default(Expression));
				Expression.OrElse(default(Expression), default(Expression), default(System.Reflection.MethodInfo));
				Expression.Coalesce(default(Expression), default(Expression));
				Expression.Coalesce(default(Expression), default(Expression), default(LambdaExpression));
				Expression.Add(default(Expression), default(Expression));
				Expression.Add(default(Expression), default(Expression), default(System.Reflection.MethodInfo));
				Expression.AddChecked(default(Expression), default(Expression));
				Expression.AddChecked(default(Expression), default(Expression), default(System.Reflection.MethodInfo));
				Expression.Subtract(default(Expression), default(Expression));
				Expression.Subtract(default(Expression), default(Expression), default(System.Reflection.MethodInfo));
				Expression.SubtractChecked(default(Expression), default(Expression));
				Expression.SubtractChecked(default(Expression), default(Expression), default(System.Reflection.MethodInfo));
				Expression.Divide(default(Expression), default(Expression));
				Expression.Divide(default(Expression), default(Expression), default(System.Reflection.MethodInfo));
				Expression.Modulo(default(Expression), default(Expression));
				Expression.Modulo(default(Expression), default(Expression), default(System.Reflection.MethodInfo));
				Expression.Multiply(default(Expression), default(Expression));
				Expression.Multiply(default(Expression), default(Expression), default(System.Reflection.MethodInfo));
				Expression.MultiplyChecked(default(Expression), default(Expression));
				Expression.MultiplyChecked(default(Expression), default(Expression), default(System.Reflection.MethodInfo));
				// ReSharper restore AssignNullToNotNullAttribute
			}
		}

		public static void RegisterForFastCall<InstanceT, Arg1T, Arg2T, Arg3T, ResultT>()
		{
			Executor.RegisterForFastCall<InstanceT, Arg1T, Arg2T, Arg3T, ResultT>();
		}
		public static void RegisterForFastCall<InstanceT, Arg1T, Arg2T, ResultT>()
		{
			Executor.RegisterForFastCall<InstanceT, Arg1T, Arg2T, ResultT>();
		}
		public static void RegisterForFastCall<InstanceT, Arg1T, ResultT>()
		{
			Executor.RegisterForFastCall<InstanceT, Arg1T, ResultT>();
		}
		public static void RegisterForFastCall<InstanceT, ResultT>()
		{
			Executor.RegisterForFastCall<InstanceT, ResultT>();
		}

		public static void RegisterFunc<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>()
		{
			if (typeof(AotCompilation).Name == string.Empty)
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				var fn = Expression.Lambda<Func<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>>(default(Expression), default(ParameterExpression[])).CompileAot();
				fn.Invoke(default(Arg1T), default(Arg2T), default(Arg3T), default(Arg4T));
			}
		}
		public static void RegisterFunc<Arg1T, Arg2T, Arg3T, ResultT>()
		{
			if (typeof(AotCompilation).Name == string.Empty)
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				var fn = Expression.Lambda<Func<Arg1T, Arg2T, Arg3T, ResultT>>(default(Expression), default(ParameterExpression[])).CompileAot();
				fn.Invoke(default(Arg1T), default(Arg2T), default(Arg3T));
			}
		}
		public static void RegisterFunc<Arg1T, Arg2T, ResultT>()
		{
			if (typeof(AotCompilation).Name == string.Empty)
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				var fn = Expression.Lambda<Func<Arg1T, Arg2T, ResultT>>(default(Expression), default(ParameterExpression[])).CompileAot();
				fn.Invoke(default(Arg1T), default(Arg2T));
			}
		}
		public static void RegisterFunc<Arg1T, ResultT>()
		{
			if (typeof(AotCompilation).Name == string.Empty)
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				var fn = Expression.Lambda<Func<Arg1T, ResultT>>(default(Expression), default(ParameterExpression[])).CompileAot();
				fn.Invoke(default(Arg1T));
			}
		}
		public static void RegisterFunc<Arg1T>()
		{
			if (typeof(AotCompilation).Name == string.Empty)
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				var fn = Expression.Lambda<Func<Arg1T>>(default(Expression), default(ParameterExpression[])).CompileAot();
				fn.Invoke();
			}
		}
	}
}
