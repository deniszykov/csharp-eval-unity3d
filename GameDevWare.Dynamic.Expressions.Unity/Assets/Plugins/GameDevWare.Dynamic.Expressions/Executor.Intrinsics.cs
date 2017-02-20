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
#if !UNITY_WEBGL || UNITY_5 || UNITY_5_0_OR_NEWER
#define UNSIGNED_TYPES
#endif

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;


// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantCast
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable UnusedMethodReturnValue.Local

#pragma warning disable 0675

namespace GameDevWare.Dynamic.Expressions
{
	partial class Executor
	{
		private delegate object BinaryOperation(Closure closure, object left, object right);
		private delegate object UnaryOperation(Closure closure, object operand);

		private static UnaryOperation CreateUnaryOperationFn(MethodInfo method)
		{
			if (method == null) throw new ArgumentNullException("method");

			return (UnaryOperation)Delegate.CreateDelegate(typeof(UnaryOperation), method, true);
		}
		private static BinaryOperation CreateBinaryOperationFn(MethodInfo method)
		{
			if (method == null) throw new ArgumentNullException("method");

			return (BinaryOperation)Delegate.CreateDelegate(typeof(BinaryOperation), method, true);
		}
		private static UnaryOperation WrapUnaryOperation(Type type, string methodName)
		{
			if (type == null) throw new ArgumentNullException("type");
			if (methodName == null) throw new ArgumentNullException("methodName");

			var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
			return WrapUnaryOperation(method);
		}
		private static UnaryOperation WrapUnaryOperation(MethodInfo method)
		{
			if (method == null) return null;

			var invoker = MethodCall.TryCreate(method);
			if (invoker != null)
			{
				var argFns = new ExecuteFunc[] { closure => closure.Locals[LOCAL_OPERAND1] };

				return (closure, operand) =>
				{
					closure.Locals[LOCAL_OPERAND1] = operand;

					var result = invoker(closure, argFns);

					closure.Locals[LOCAL_OPERAND1] = null;

					return result;
				};
			}
			else
			{
				return (closure, operand) => { return method.Invoke(null, new object[] { operand }); };
			}
		}
		private static BinaryOperation WrapBinaryOperation(Type type, string methodName)
		{
			if (type == null) throw new ArgumentNullException("type");
			if (methodName == null) throw new ArgumentNullException("methodName");

			var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
			if (method == null) return null;

			return WrapBinaryOperation(method);
		}
		private static BinaryOperation WrapBinaryOperation(MethodInfo method)
		{
			if (method == null) return null;


			var invoker = MethodCall.TryCreate(method);
			if (invoker != null)
			{
				var argFns = new ExecuteFunc[] { closure => closure.Locals[LOCAL_OPERAND1], closure => closure.Locals[LOCAL_OPERAND2] };

				return (closure, left, right) =>
				{
					closure.Locals[LOCAL_OPERAND1] = left;
					closure.Locals[LOCAL_OPERAND2] = right;

					var result = invoker(closure, argFns);

					closure.Locals[LOCAL_OPERAND1] = null;
					closure.Locals[LOCAL_OPERAND2] = null;

					return result;
				};
			}
			else
			{
				return (closure, left, right) => { return method.Invoke(null, new object[] { left, right }); };
			}
		}

		private static class Intrinsic
		{
			private static readonly Dictionary<Type, Dictionary<int, Delegate>> Operations;
			private static readonly Dictionary<Type, Dictionary<Type, Delegate>> Convertions;

			static Intrinsic()
			{
				// AOT
				if (typeof(Intrinsic).Name == string.Empty)
				{
					op_Boolean.Not(default(Closure), default(object));
					op_Byte.Negate(default(Closure), default(object));
					op_SByte.Negate(default(Closure), default(object));
					op_Int16.Negate(default(Closure), default(object));
					op_UInt16.Negate(default(Closure), default(object));
					op_Int32.Negate(default(Closure), default(object));
#if !UNITY_WEBGL
					op_UInt32.Negate(default(Closure), default(object));
					op_Int64.Negate(default(Closure), default(object));
					op_UInt64.UnaryPlus(default(Closure), default(object));
#endif
					op_Single.Negate(default(Closure), default(object));
					op_Double.Negate(default(Closure), default(object));
					op_Decimal.Negate(default(Closure), default(object));
					op_Object.Equal(default(Closure), default(object), default(object));

					BinaryOperation(default(Closure), default(object), default(object), default(ExpressionType), default(BinaryOperation));
					UnaryOperation(default(Closure), default(object), default(ExpressionType), default(UnaryOperation));
					Convert(default(Closure), default(object), default(Type), default(ExpressionType), default(UnaryOperation));
				}

				var expressionTypeNames = Enum.GetNames(typeof(ExpressionType));
				Array.Sort(expressionTypeNames, StringComparer.Ordinal);

				Operations = new Dictionary<Type, Dictionary<int, Delegate>>();
				foreach (var opType in typeof(Executor).GetNestedTypes(BindingFlags.NonPublic))
				{
					if (opType.Name.StartsWith("op_", StringComparison.Ordinal) == false) continue;
					var type = Type.GetType("System." + opType.Name.Substring(3), false);
					if (type == null) continue;

					var delegatesByExpressionType = default(Dictionary<int, Delegate>);
					if (Operations.TryGetValue(type, out delegatesByExpressionType) == false)
						Operations[type] = delegatesByExpressionType = new Dictionary<int, Delegate>();

					foreach (var method in opType.GetMethods(BindingFlags.Public | BindingFlags.Static))
					{
						if (Array.BinarySearch(expressionTypeNames, method.Name) < 0)
							continue;

						var expressionType = (ExpressionType)Enum.Parse(typeof(ExpressionType), method.Name);
						var methodParams = method.GetParameters();
						var fn = methodParams.Length == 3 ? (Delegate)CreateBinaryOperationFn(method) :
								methodParams.Length == 2 ? (Delegate)CreateUnaryOperationFn(method) : null;

						delegatesByExpressionType[(int)expressionType] = fn;
					}
				}

				Convertions = new Dictionary<Type, Dictionary<Type, Delegate>>();
				foreach (var opType in typeof(Executor).GetNestedTypes(BindingFlags.NonPublic))
				{
					if (opType.Name.StartsWith("op_", StringComparison.Ordinal) == false) continue;
					var type = Type.GetType("System." + opType.Name.Substring(3), false);
					if (type == null) continue;

					var convertorsByType = default(Dictionary<Type, Delegate>);
					if (Convertions.TryGetValue(type, out convertorsByType) == false)
						Convertions[type] = convertorsByType = new Dictionary<Type, Delegate>();

					foreach (var method in opType.GetMethods(BindingFlags.Public | BindingFlags.Static))
					{
						if (method.Name.StartsWith("To", StringComparison.Ordinal) == false)
							continue;

						var fn = (Delegate)CreateBinaryOperationFn(method);
						var toType = Type.GetType("System." + method.Name.Substring(2), false);
						if (toType == null)
							continue;

						convertorsByType[toType] = fn;
					}
				}
			}

			public static object BinaryOperation(Closure closure, object left, object right,
				ExpressionType binaryOperationType, BinaryOperation userDefinedBinaryOperation)
			{
				if (closure == null) throw new ArgumentNullException("closure");

				var type = left != null ? left.GetType() : right != null ? right.GetType() : typeof(object);
				var dictionary = default(Dictionary<int, Delegate>);
				var func = default(Delegate);

				if (Operations.TryGetValue(type, out dictionary) && dictionary.TryGetValue((int)binaryOperationType, out func))
					return ((BinaryOperation)func)(closure, left, right);

				if (binaryOperationType == ExpressionType.Equal)
					userDefinedBinaryOperation = (BinaryOperation)Operations[typeof(object)][(int)ExpressionType.Equal];
				else if (binaryOperationType == ExpressionType.NotEqual)
					userDefinedBinaryOperation = (BinaryOperation)Operations[typeof(object)][(int)ExpressionType.NotEqual];

				if (userDefinedBinaryOperation == null)
					throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_COMPIL_NOBINARYOPONTYPE, binaryOperationType, type));

				return userDefinedBinaryOperation(closure, left, right);
			}

			public static object UnaryOperation(Closure closure, object operand, ExpressionType unaryOperationType,
				UnaryOperation userDefinedUnaryOperation)
			{
				if (closure == null) throw new ArgumentNullException("closure");

				var type = operand != null ? operand.GetType() : typeof(object);
				var dictionary = default(Dictionary<int, Delegate>);
				var func = default(Delegate);

				if (Operations.TryGetValue(type, out dictionary) && dictionary.TryGetValue((int)unaryOperationType, out func))
					return ((UnaryOperation)func)(closure, operand);

				if (userDefinedUnaryOperation == null)
					throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_COMPIL_NOUNARYOPONTYPE, unaryOperationType, type));

				return userDefinedUnaryOperation(closure, operand);
			}

			public static object Convert(Closure closure, object value, Type toType, ExpressionType convertType, UnaryOperation userDefinedConvertOperation)
			{
				if (closure == null) throw new ArgumentNullException("closure");
				if (toType == null) throw new ArgumentNullException("toType");

				var type = value != null ? value.GetType() : typeof(object);
				var dictionary = default(Dictionary<Type, Delegate>);
				var func = default(Delegate);

				if (Convertions.TryGetValue(type, out dictionary) && dictionary.TryGetValue(toType, out func))
					return ((BinaryOperation)func)(closure, value, convertType == ExpressionType.Convert ? bool.FalseString : bool.TrueString);

				if (userDefinedConvertOperation == null)
					throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_COMPIL_NOCONVERTIONBETWEENTYPES, type, toType));

				return userDefinedConvertOperation(closure, value);
			}
		}

		private static class op_Object
		{
			static op_Object()
			{
				// AOT
				if (typeof(op_Object).Name == string.Empty)
				{
					Default(default(Closure));
					Equal(default(Closure), default(object), default(object));
					NotEqual(default(Closure), default(object), default(object));
					ToObject(default(Closure), default(object), default(object));
					ToBoolean(default(Closure), default(object), default(object));
					ToSByte(default(Closure), default(object), default(object));
					ToByte(default(Closure), default(object), default(object));
					ToInt16(default(Closure), default(object), default(object));
					ToUInt16(default(Closure), default(object), default(object));
					ToInt32(default(Closure), default(object), default(object));
#if !UNITY_WEBGL
					ToUInt32(default(Closure), default(object), default(object));
					ToInt64(default(Closure), default(object), default(object));
					ToUInt64(default(Closure), default(object), default(object));
#endif
					ToSingle(default(Closure), default(object), default(object));
					ToDouble(default(Closure), default(object), default(object));
					ToDecimal(default(Closure), default(object), default(object));
				}
			}

			public static object Default(Closure closure)
			{
				return closure.Box(default(object));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box(Equals(closure.Unbox<object>(left), closure.Unbox<object>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box(!Equals(closure.Unbox<object>(left), closure.Unbox<object>(right)));
			}

			public static object ToObject(Closure closure, object left, object isChecked)
			{
				return closure.Box(left);
			}
			public static object ToBoolean(Closure closure, object left, object isChecked)
			{
				return closure.Box(closure.Unbox<bool>(left));
			}
			public static object ToSByte(Closure closure, object left, object isChecked)
			{
				return closure.Box(closure.Unbox<sbyte>(left));
			}
			public static object ToByte(Closure closure, object left, object isChecked)
			{
				return closure.Box(closure.Unbox<byte>(left));
			}
			public static object ToInt16(Closure closure, object left, object isChecked)
			{
				return closure.Box(closure.Unbox<short>(left));
			}
			public static object ToUInt16(Closure closure, object left, object isChecked)
			{
				return closure.Box(closure.Unbox<ushort>(left));
			}
			public static object ToInt32(Closure closure, object left, object isChecked)
			{
				return closure.Box(closure.Unbox<int>(left));
			}
#if UNSIGNED_TYPES
			public static object ToUInt32(Closure closure, object left, object isChecked)
			{
				return closure.Box(closure.Unbox<uint>(left));
			}
			public static object ToInt64(Closure closure, object left, object isChecked)
			{
				return closure.Box(closure.Unbox<long>(left));
			}
			public static object ToUInt64(Closure closure, object left, object isChecked)
			{
				return closure.Box(closure.Unbox<ulong>(left));
			}
#endif
			public static object ToSingle(Closure closure, object left, object isChecked)
			{
				return closure.Box(closure.Unbox<float>(left));
			}
			public static object ToDouble(Closure closure, object left, object isChecked)
			{
				return closure.Box(closure.Unbox<double>(left));
			}
			public static object ToDecimal(Closure closure, object left, object isChecked)
			{
				return closure.Box(closure.Unbox<decimal>(left));
			}
		}
		private static class op_Boolean
		{
			static op_Boolean()
			{
				// AOT
				if (typeof(op_Boolean).Name == string.Empty)
				{
					Default(default(Closure));
					Not(default(Closure), default(bool));
					Equal(default(Closure), default(object), default(object));
					NotEqual(default(Closure), default(object), default(object));
					ToObject(default(Closure), default(object), default(object));
					ToBoolean(default(Closure), default(object), default(object));
				}
			}

			public static object Default(Closure closure)
			{
				return closure.Box(default(bool));
			}
			public static object Not(Closure closure, object operand)
			{
				return closure.Box(!closure.Unbox<bool>(operand));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box(Equals(closure.Unbox<bool>(left), closure.Unbox<bool>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box(!Equals(closure.Unbox<bool>(left), closure.Unbox<bool>(right)));
			}

			public static object ToObject(Closure closure, object left, object _)
			{
				return closure.Box(left);
			}
			public static object ToBoolean(Closure closure, object left, object _)
			{
				return closure.Box(closure.Unbox<bool>(left));
			}
		}
		private static class op_Byte
		{
			static op_Byte()
			{
				// AOT
				if (typeof(op_Byte).Name == string.Empty)
				{
					Default(default(Closure));
					Negate(default(Closure), default(Byte));
					NegateChecked(default(Closure), default(object));
					UnaryPlus(default(Closure), default(object));
					Not(default(Closure), default(object));
					Add(default(Closure), default(object), default(object));
					AddChecked(default(Closure), default(object), default(object));
					And(default(Closure), default(object), default(object));
					Divide(default(Closure), default(object), default(object));
					Equal(default(Closure), default(object), default(object));
					ExclusiveOr(default(Closure), default(object), default(object));
					GreaterThan(default(Closure), default(object), default(object));
					GreaterThanOrEqual(default(Closure), default(object), default(object));
					LeftShift(default(Closure), default(object), default(object));
					Power(default(Closure), default(object), default(object));
					RightShift(default(Closure), default(object), default(object));
					LessThan(default(Closure), default(object), default(object));
					LessThanOrEqual(default(Closure), default(object), default(object));
					Modulo(default(Closure), default(object), default(object));
					Multiply(default(Closure), default(object), default(object));
					MultiplyChecked(default(Closure), default(object), default(object));
					NotEqual(default(Closure), default(object), default(object));
					Or(default(Closure), default(object), default(object));
					Subtract(default(Closure), default(object), default(object));
					SubtractChecked(default(Closure), default(object), default(object));
					ToObject(default(Closure), default(object), default(object));
					ToSByte(default(Closure), default(object), default(object));
					ToByte(default(Closure), default(object), default(object));
					ToInt16(default(Closure), default(object), default(object));
					ToUInt16(default(Closure), default(object), default(object));
					ToInt32(default(Closure), default(object), default(object));
#if !UNITY_WEBGL
					ToUInt32(default(Closure), default(object), default(object));
					ToInt64(default(Closure), default(object), default(object));
					ToUInt64(default(Closure), default(object), default(object));
#endif
					ToSingle(default(Closure), default(object), default(object));
					ToDouble(default(Closure), default(object), default(object));
					ToDecimal(default(Closure), default(object), default(object));
				}
			}

			public static object Default(Closure closure)
			{
				return closure.Box(default(byte));
			}
			public static object Negate(Closure closure, object operand)
			{
				return closure.Box((byte)unchecked(-closure.Unbox<byte>(operand)));
			}
			public static object NegateChecked(Closure closure, object operand)
			{
				return closure.Box((byte)checked(-closure.Unbox<byte>(operand)));
			}
			public static object UnaryPlus(Closure closure, object operand)
			{
				return closure.Box((byte)unchecked(+closure.Unbox<byte>(operand)));
			}
			public static object Not(Closure closure, object operand)
			{
				return closure.Box((byte)~closure.Unbox<byte>(operand));
			}
			public static object Add(Closure closure, object left, object right)
			{
				return closure.Box((byte)unchecked(closure.Unbox<byte>(left) + closure.Unbox<byte>(right)));
			}
			public static object AddChecked(Closure closure, object left, object right)
			{
				return closure.Box((byte)checked(closure.Unbox<byte>(left) + closure.Unbox<byte>(right)));
			}
			public static object And(Closure closure, object left, object right)
			{
				return closure.Box((byte)(closure.Unbox<byte>(left) & closure.Unbox<byte>(right)));
			}
			public static object Divide(Closure closure, object left, object right)
			{
				return closure.Box((byte)(closure.Unbox<byte>(left) / closure.Unbox<byte>(right)));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<byte>(left) == closure.Unbox<byte>(right)));
			}
			public static object ExclusiveOr(Closure closure, object left, object right)
			{
				return closure.Box((byte)(closure.Unbox<byte>(left) ^ closure.Unbox<byte>(right)));
			}
			public static object GreaterThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<byte>(left) > closure.Unbox<byte>(right)));
			}
			public static object GreaterThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<byte>(left) >= closure.Unbox<byte>(right)));
			}
			public static object LeftShift(Closure closure, object left, object right)
			{
				return closure.Box((byte)(closure.Unbox<byte>(left) << closure.Unbox<int>(right)));
			}
			public static object Power(Closure closure, object left, object right)
			{
				return closure.Box((byte)Math.Pow(closure.Unbox<byte>(left), closure.Unbox<double>(right)));
			}
			public static object RightShift(Closure closure, object left, object right)
			{
				return closure.Box((byte)(closure.Unbox<byte>(left) >> closure.Unbox<int>(right)));
			}
			public static object LessThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<byte>(left) < closure.Unbox<byte>(right)));
			}
			public static object LessThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<byte>(left) <= closure.Unbox<byte>(right)));
			}
			public static object Modulo(Closure closure, object left, object right)
			{
				return closure.Box((byte)(closure.Unbox<byte>(left) % closure.Unbox<byte>(right)));
			}
			public static object Multiply(Closure closure, object left, object right)
			{
				return closure.Box((byte)unchecked(closure.Unbox<byte>(left) * closure.Unbox<byte>(right)));
			}
			public static object MultiplyChecked(Closure closure, object left, object right)
			{
				return closure.Box((byte)checked(closure.Unbox<byte>(left) * closure.Unbox<byte>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<byte>(left) != closure.Unbox<byte>(right)));
			}
			public static object Or(Closure closure, object left, object right)
			{
				return closure.Box((byte)(closure.Unbox<byte>(left) | closure.Unbox<byte>(right)));
			}
			public static object Subtract(Closure closure, object left, object right)
			{
				return closure.Box((byte)unchecked(closure.Unbox<byte>(left) - closure.Unbox<byte>(right)));
			}
			public static object SubtractChecked(Closure closure, object left, object right)
			{
				return closure.Box((byte)checked(closure.Unbox<byte>(left) - closure.Unbox<byte>(right)));
			}

			public static object ToObject(Closure closure, object left, object isChecked)
			{
				return closure.Box(left);
			}
			public static object ToSByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((sbyte)closure.Unbox<byte>(left)));
				else
					return unchecked(closure.Box((sbyte)closure.Unbox<byte>(left)));
			}
			public static object ToByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((byte)closure.Unbox<byte>(left)));
				else
					return unchecked(closure.Box((byte)closure.Unbox<byte>(left)));
			}
			public static object ToInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((short)closure.Unbox<byte>(left)));
				else
					return unchecked(closure.Box((short)closure.Unbox<byte>(left)));
			}
			public static object ToUInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ushort)closure.Unbox<byte>(left)));
				else
					return unchecked(closure.Box((ushort)closure.Unbox<byte>(left)));
			}
			public static object ToInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((int)closure.Unbox<byte>(left)));
				else
					return unchecked(closure.Box((int)closure.Unbox<byte>(left)));
			}
#if UNSIGNED_TYPES
			public static object ToUInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((uint)closure.Unbox<byte>(left)));
				else
					return unchecked(closure.Box((uint)closure.Unbox<byte>(left)));
			}
			public static object ToInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((long)closure.Unbox<byte>(left)));
				else
					return unchecked(closure.Box((long)closure.Unbox<byte>(left)));
			}
			public static object ToUInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ulong)closure.Unbox<byte>(left)));
				else
					return unchecked(closure.Box((ulong)closure.Unbox<byte>(left)));
			}
#endif
			public static object ToSingle(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((float)closure.Unbox<byte>(left)));
				else
					return unchecked(closure.Box((float)closure.Unbox<byte>(left)));
			}
			public static object ToDouble(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((double)closure.Unbox<byte>(left)));
				else
					return unchecked(closure.Box((double)closure.Unbox<byte>(left)));
			}
			public static object ToDecimal(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((decimal)closure.Unbox<byte>(left)));
				else
					return unchecked(closure.Box((decimal)closure.Unbox<byte>(left)));
			}
		}
		private static class op_SByte
		{
			static op_SByte()
			{
				// AOT
				if (typeof(op_SByte).Name == string.Empty)
				{
					Default(default(Closure));
					Negate(default(Closure), default(byte));
					NegateChecked(default(Closure), default(object));
					UnaryPlus(default(Closure), default(object));
					Not(default(Closure), default(object));
					Add(default(Closure), default(object), default(object));
					AddChecked(default(Closure), default(object), default(object));
					And(default(Closure), default(object), default(object));
					Divide(default(Closure), default(object), default(object));
					Equal(default(Closure), default(object), default(object));
					ExclusiveOr(default(Closure), default(object), default(object));
					GreaterThan(default(Closure), default(object), default(object));
					GreaterThanOrEqual(default(Closure), default(object), default(object));
					LeftShift(default(Closure), default(object), default(object));
					Power(default(Closure), default(object), default(object));
					RightShift(default(Closure), default(object), default(object));
					LessThan(default(Closure), default(object), default(object));
					LessThanOrEqual(default(Closure), default(object), default(object));
					Modulo(default(Closure), default(object), default(object));
					Multiply(default(Closure), default(object), default(object));
					MultiplyChecked(default(Closure), default(object), default(object));
					NotEqual(default(Closure), default(object), default(object));
					Or(default(Closure), default(object), default(object));
					Subtract(default(Closure), default(object), default(object));
					SubtractChecked(default(Closure), default(object), default(object));
					ToObject(default(Closure), default(object), default(object));
					ToSByte(default(Closure), default(object), default(object));
					ToByte(default(Closure), default(object), default(object));
					ToInt16(default(Closure), default(object), default(object));
					ToUInt16(default(Closure), default(object), default(object));
					ToInt32(default(Closure), default(object), default(object));
#if !UNITY_WEBGL
					ToUInt32(default(Closure), default(object), default(object));
					ToInt64(default(Closure), default(object), default(object));
					ToUInt64(default(Closure), default(object), default(object));
#endif
					ToSingle(default(Closure), default(object), default(object));
					ToDouble(default(Closure), default(object), default(object));
					ToDecimal(default(Closure), default(object), default(object));
				}
			}

			public static object Default(Closure closure)
			{
				return closure.Box(default(sbyte));
			}
			public static object Negate(Closure closure, object operand)
			{
				return closure.Box((sbyte)unchecked(-closure.Unbox<sbyte>(operand)));
			}
			public static object NegateChecked(Closure closure, object operand)
			{
				return closure.Box((sbyte)checked(-closure.Unbox<sbyte>(operand)));
			}
			public static object UnaryPlus(Closure closure, object operand)
			{
				return closure.Box((sbyte)unchecked(+closure.Unbox<sbyte>(operand)));
			}
			public static object Not(Closure closure, object operand)
			{
				return closure.Box((sbyte)~closure.Unbox<sbyte>(operand));
			}
			public static object Add(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)unchecked(closure.Unbox<sbyte>(left) + closure.Unbox<sbyte>(right)));
			}
			public static object AddChecked(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)checked(closure.Unbox<sbyte>(left) + closure.Unbox<sbyte>(right)));
			}
			public static object And(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)(closure.Unbox<sbyte>(left) & closure.Unbox<sbyte>(right)));
			}
			public static object Divide(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)(closure.Unbox<sbyte>(left) / closure.Unbox<sbyte>(right)));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<sbyte>(left) == closure.Unbox<sbyte>(right)));
			}
			public static object ExclusiveOr(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)(closure.Unbox<sbyte>(left) ^ closure.Unbox<sbyte>(right)));
			}
			public static object GreaterThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<sbyte>(left) > closure.Unbox<sbyte>(right)));
			}
			public static object GreaterThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<sbyte>(left) >= closure.Unbox<sbyte>(right)));
			}
			public static object LeftShift(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)(closure.Unbox<sbyte>(left) << closure.Unbox<int>(right)));
			}
			public static object Power(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)Math.Pow(closure.Unbox<sbyte>(left), closure.Unbox<double>(right)));
			}
			public static object RightShift(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)(closure.Unbox<sbyte>(left) >> closure.Unbox<int>(right)));
			}
			public static object LessThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<sbyte>(left) < closure.Unbox<sbyte>(right)));
			}
			public static object LessThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<sbyte>(left) <= closure.Unbox<sbyte>(right)));
			}
			public static object Modulo(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)(closure.Unbox<sbyte>(left) % closure.Unbox<sbyte>(right)));
			}
			public static object Multiply(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)unchecked(closure.Unbox<sbyte>(left) * closure.Unbox<sbyte>(right)));
			}
			public static object MultiplyChecked(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)checked(closure.Unbox<sbyte>(left) * closure.Unbox<sbyte>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<sbyte>(left) != closure.Unbox<sbyte>(right)));
			}
			public static object Or(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)(closure.Unbox<sbyte>(left) | closure.Unbox<sbyte>(right)));
			}
			public static object Subtract(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)unchecked(closure.Unbox<sbyte>(left) - closure.Unbox<sbyte>(right)));
			}
			public static object SubtractChecked(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)checked(closure.Unbox<sbyte>(left) - closure.Unbox<sbyte>(right)));
			}

			public static object ToObject(Closure closure, object left, object isChecked)
			{
				return closure.Box(left);
			}
			public static object ToSByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((sbyte)closure.Unbox<sbyte>(left)));
				else
					return unchecked(closure.Box((sbyte)closure.Unbox<sbyte>(left)));
			}
			public static object ToByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((byte)closure.Unbox<sbyte>(left)));
				else
					return unchecked(closure.Box((byte)closure.Unbox<sbyte>(left)));
			}
			public static object ToInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((short)closure.Unbox<sbyte>(left)));
				else
					return unchecked(closure.Box((short)closure.Unbox<sbyte>(left)));
			}
			public static object ToUInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ushort)closure.Unbox<sbyte>(left)));
				else
					return unchecked(closure.Box((ushort)closure.Unbox<sbyte>(left)));
			}
			public static object ToInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((int)closure.Unbox<sbyte>(left)));
				else
					return unchecked(closure.Box((int)closure.Unbox<sbyte>(left)));
			}
#if UNSIGNED_TYPES
			public static object ToUInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((uint)closure.Unbox<sbyte>(left)));
				else
					return unchecked(closure.Box((uint)closure.Unbox<sbyte>(left)));
			}
			public static object ToInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((long)closure.Unbox<sbyte>(left)));
				else
					return unchecked(closure.Box((long)closure.Unbox<sbyte>(left)));
			}
			public static object ToUInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ulong)closure.Unbox<sbyte>(left)));
				else
					return unchecked(closure.Box((ulong)closure.Unbox<sbyte>(left)));
			}
#endif
			public static object ToSingle(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((float)closure.Unbox<sbyte>(left)));
				else
					return unchecked(closure.Box((float)closure.Unbox<sbyte>(left)));
			}
			public static object ToDouble(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((double)closure.Unbox<sbyte>(left)));
				else
					return unchecked(closure.Box((double)closure.Unbox<sbyte>(left)));
			}
			public static object ToDecimal(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((decimal)closure.Unbox<sbyte>(left)));
				else
					return unchecked(closure.Box((decimal)closure.Unbox<sbyte>(left)));
			}
		}
		private static class op_Int16
		{
			static op_Int16()
			{
				// AOT
				if (typeof(op_Int16).Name == string.Empty)
				{
					Default(default(Closure));
					Negate(default(Closure), default(short));
					NegateChecked(default(Closure), default(object));
					UnaryPlus(default(Closure), default(object));
					Not(default(Closure), default(object));
					Add(default(Closure), default(object), default(object));
					AddChecked(default(Closure), default(object), default(object));
					And(default(Closure), default(object), default(object));
					Divide(default(Closure), default(object), default(object));
					Equal(default(Closure), default(object), default(object));
					ExclusiveOr(default(Closure), default(object), default(object));
					GreaterThan(default(Closure), default(object), default(object));
					GreaterThanOrEqual(default(Closure), default(object), default(object));
					LeftShift(default(Closure), default(object), default(object));
					Power(default(Closure), default(object), default(object));
					RightShift(default(Closure), default(object), default(object));
					LessThan(default(Closure), default(object), default(object));
					LessThanOrEqual(default(Closure), default(object), default(object));
					Modulo(default(Closure), default(object), default(object));
					Multiply(default(Closure), default(object), default(object));
					MultiplyChecked(default(Closure), default(object), default(object));
					NotEqual(default(Closure), default(object), default(object));
					Or(default(Closure), default(object), default(object));
					Subtract(default(Closure), default(object), default(object));
					SubtractChecked(default(Closure), default(object), default(object));
					ToObject(default(Closure), default(object), default(object));
					ToSByte(default(Closure), default(object), default(object));
					ToByte(default(Closure), default(object), default(object));
					ToInt16(default(Closure), default(object), default(object));
					ToUInt16(default(Closure), default(object), default(object));
					ToInt32(default(Closure), default(object), default(object));
#if !UNITY_WEBGL
					ToUInt32(default(Closure), default(object), default(object));
					ToInt64(default(Closure), default(object), default(object));
					ToUInt64(default(Closure), default(object), default(object));
#endif
					ToSingle(default(Closure), default(object), default(object));
					ToDouble(default(Closure), default(object), default(object));
					ToDecimal(default(Closure), default(object), default(object));
				}
			}

			public static object Default(Closure closure)
			{
				return closure.Box(default(short));
			}
			public static object Negate(Closure closure, object operand)
			{
				return closure.Box((short)unchecked(-closure.Unbox<short>(operand)));
			}
			public static object NegateChecked(Closure closure, object operand)
			{
				return closure.Box((short)checked(-closure.Unbox<short>(operand)));
			}
			public static object UnaryPlus(Closure closure, object operand)
			{
				return closure.Box((short)unchecked(+closure.Unbox<short>(operand)));
			}
			public static object Not(Closure closure, object operand)
			{
				return closure.Box((short)~closure.Unbox<short>(operand));
			}
			public static object Add(Closure closure, object left, object right)
			{
				return closure.Box((short)unchecked(closure.Unbox<short>(left) + closure.Unbox<short>(right)));
			}
			public static object AddChecked(Closure closure, object left, object right)
			{
				return closure.Box((short)checked(closure.Unbox<short>(left) + closure.Unbox<short>(right)));
			}
			public static object And(Closure closure, object left, object right)
			{
				return closure.Box((short)(closure.Unbox<short>(left) & closure.Unbox<short>(right)));
			}
			public static object Divide(Closure closure, object left, object right)
			{
				return closure.Box((short)(closure.Unbox<short>(left) / closure.Unbox<short>(right)));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<short>(left) == closure.Unbox<short>(right)));
			}
			public static object ExclusiveOr(Closure closure, object left, object right)
			{
				return closure.Box((short)(closure.Unbox<short>(left) ^ closure.Unbox<short>(right)));
			}
			public static object GreaterThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<short>(left) > closure.Unbox<short>(right)));
			}
			public static object GreaterThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<short>(left) >= closure.Unbox<short>(right)));
			}
			public static object LeftShift(Closure closure, object left, object right)
			{
				return closure.Box((short)(closure.Unbox<short>(left) << closure.Unbox<int>(right)));
			}
			public static object Power(Closure closure, object left, object right)
			{
				return closure.Box((short)Math.Pow(closure.Unbox<short>(left), closure.Unbox<double>(right)));
			}
			public static object RightShift(Closure closure, object left, object right)
			{
				return closure.Box((short)(closure.Unbox<short>(left) >> closure.Unbox<int>(right)));
			}
			public static object LessThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<short>(left) < closure.Unbox<short>(right)));
			}
			public static object LessThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<short>(left) <= closure.Unbox<short>(right)));
			}
			public static object Modulo(Closure closure, object left, object right)
			{
				return closure.Box((short)(closure.Unbox<short>(left) % closure.Unbox<short>(right)));
			}
			public static object Multiply(Closure closure, object left, object right)
			{
				return closure.Box((short)unchecked(closure.Unbox<short>(left) * closure.Unbox<short>(right)));
			}
			public static object MultiplyChecked(Closure closure, object left, object right)
			{
				return closure.Box((short)checked(closure.Unbox<short>(left) * closure.Unbox<short>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<short>(left) != closure.Unbox<short>(right)));
			}
			public static object Or(Closure closure, object left, object right)
			{
				return closure.Box((short)(closure.Unbox<short>(left) | closure.Unbox<short>(right)));
			}
			public static object Subtract(Closure closure, object left, object right)
			{
				return closure.Box((short)unchecked(closure.Unbox<short>(left) - closure.Unbox<short>(right)));
			}
			public static object SubtractChecked(Closure closure, object left, object right)
			{
				return closure.Box((short)checked(closure.Unbox<short>(left) - closure.Unbox<short>(right)));
			}

			public static object ToObject(Closure closure, object left, object _)
			{
				return closure.Box(left);
			}
			public static object ToSByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((sbyte)closure.Unbox<short>(left)));
				else
					return unchecked(closure.Box((sbyte)closure.Unbox<short>(left)));
			}
			public static object ToByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((byte)closure.Unbox<short>(left)));
				else
					return unchecked(closure.Box((byte)closure.Unbox<short>(left)));
			}
			public static object ToInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((short)closure.Unbox<short>(left)));
				else
					return unchecked(closure.Box((short)closure.Unbox<short>(left)));
			}
			public static object ToUInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ushort)closure.Unbox<short>(left)));
				else
					return unchecked(closure.Box((ushort)closure.Unbox<short>(left)));
			}
			public static object ToInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((int)closure.Unbox<short>(left)));
				else
					return unchecked(closure.Box((int)closure.Unbox<short>(left)));
			}
#if UNSIGNED_TYPES
			public static object ToUInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((uint)closure.Unbox<short>(left)));
				else
					return unchecked(closure.Box((uint)closure.Unbox<short>(left)));
			}
			public static object ToInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((long)closure.Unbox<short>(left)));
				else
					return unchecked(closure.Box((long)closure.Unbox<short>(left)));
			}
			public static object ToUInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ulong)closure.Unbox<short>(left)));
				else
					return unchecked(closure.Box((ulong)closure.Unbox<short>(left)));
			}
#endif
			public static object ToSingle(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((float)closure.Unbox<short>(left)));
				else
					return unchecked(closure.Box((float)closure.Unbox<short>(left)));
			}
			public static object ToDouble(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((double)closure.Unbox<short>(left)));
				else
					return unchecked(closure.Box((double)closure.Unbox<short>(left)));
			}
			public static object ToDecimal(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((decimal)closure.Unbox<short>(left)));
				else
					return unchecked(closure.Box((decimal)closure.Unbox<short>(left)));
			}
		}
		private static class op_UInt16
		{
			static op_UInt16()
			{
				// AOT
				if (typeof(op_UInt16).Name == string.Empty)
				{
					Default(default(Closure));
					Negate(default(Closure), default(ushort));
					NegateChecked(default(Closure), default(object));
					UnaryPlus(default(Closure), default(object));
					Not(default(Closure), default(object));
					Add(default(Closure), default(object), default(object));
					AddChecked(default(Closure), default(object), default(object));
					And(default(Closure), default(object), default(object));
					Divide(default(Closure), default(object), default(object));
					Equal(default(Closure), default(object), default(object));
					ExclusiveOr(default(Closure), default(object), default(object));
					GreaterThan(default(Closure), default(object), default(object));
					GreaterThanOrEqual(default(Closure), default(object), default(object));
					LeftShift(default(Closure), default(object), default(object));
					Power(default(Closure), default(object), default(object));
					RightShift(default(Closure), default(object), default(object));
					LessThan(default(Closure), default(object), default(object));
					LessThanOrEqual(default(Closure), default(object), default(object));
					Modulo(default(Closure), default(object), default(object));
					Multiply(default(Closure), default(object), default(object));
					MultiplyChecked(default(Closure), default(object), default(object));
					NotEqual(default(Closure), default(object), default(object));
					Or(default(Closure), default(object), default(object));
					Subtract(default(Closure), default(object), default(object));
					SubtractChecked(default(Closure), default(object), default(object));
					ToObject(default(Closure), default(object), default(object));
					ToSByte(default(Closure), default(object), default(object));
					ToByte(default(Closure), default(object), default(object));
					ToInt16(default(Closure), default(object), default(object));
					ToUInt16(default(Closure), default(object), default(object));
					ToInt32(default(Closure), default(object), default(object));
#if !UNITY_WEBGL
					ToUInt32(default(Closure), default(object), default(object));
					ToInt64(default(Closure), default(object), default(object));
					ToUInt64(default(Closure), default(object), default(object));
#endif
					ToSingle(default(Closure), default(object), default(object));
					ToDouble(default(Closure), default(object), default(object));
					ToDecimal(default(Closure), default(object), default(object));
				}
			}

			public static object Default(Closure closure)
			{
				return closure.Box(default(ushort));
			}
			public static object Negate(Closure closure, object operand)
			{
				return closure.Box((ushort)unchecked(-closure.Unbox<ushort>(operand)));
			}
			public static object NegateChecked(Closure closure, object operand)
			{
				return closure.Box((ushort)checked(-closure.Unbox<ushort>(operand)));
			}
			public static object UnaryPlus(Closure closure, object operand)
			{
				return closure.Box((ushort)unchecked(+closure.Unbox<ushort>(operand)));
			}
			public static object Not(Closure closure, object operand)
			{
				return closure.Box((ushort)~closure.Unbox<ushort>(operand));
			}
			public static object Add(Closure closure, object left, object right)
			{
				return closure.Box((ushort)unchecked(closure.Unbox<ushort>(left) + closure.Unbox<ushort>(right)));
			}
			public static object AddChecked(Closure closure, object left, object right)
			{
				return closure.Box((ushort)checked(closure.Unbox<ushort>(left) + closure.Unbox<ushort>(right)));
			}
			public static object And(Closure closure, object left, object right)
			{
				return closure.Box((ushort)(closure.Unbox<ushort>(left) & closure.Unbox<ushort>(right)));
			}
			public static object Divide(Closure closure, object left, object right)
			{
				return closure.Box((ushort)(closure.Unbox<ushort>(left) / closure.Unbox<ushort>(right)));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<ushort>(left) == closure.Unbox<ushort>(right)));
			}
			public static object ExclusiveOr(Closure closure, object left, object right)
			{
				return closure.Box((ushort)(closure.Unbox<ushort>(left) ^ closure.Unbox<ushort>(right)));
			}
			public static object GreaterThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<ushort>(left) > closure.Unbox<ushort>(right)));
			}
			public static object GreaterThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<ushort>(left) >= closure.Unbox<ushort>(right)));
			}
			public static object LeftShift(Closure closure, object left, object right)
			{
				return closure.Box((ushort)(closure.Unbox<ushort>(left) << closure.Unbox<int>(right)));
			}
			public static object Power(Closure closure, object left, object right)
			{
				return closure.Box((ushort)Math.Pow(closure.Unbox<ushort>(left), closure.Unbox<double>(right)));
			}
			public static object RightShift(Closure closure, object left, object right)
			{
				return closure.Box((ushort)(closure.Unbox<ushort>(left) >> closure.Unbox<int>(right)));
			}
			public static object LessThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<ushort>(left) < closure.Unbox<ushort>(right)));
			}
			public static object LessThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<ushort>(left) <= closure.Unbox<ushort>(right)));
			}
			public static object Modulo(Closure closure, object left, object right)
			{
				return closure.Box((ushort)(closure.Unbox<ushort>(left) % closure.Unbox<ushort>(right)));
			}
			public static object Multiply(Closure closure, object left, object right)
			{
				return closure.Box((ushort)unchecked(closure.Unbox<ushort>(left) * closure.Unbox<ushort>(right)));
			}
			public static object MultiplyChecked(Closure closure, object left, object right)
			{
				return closure.Box((ushort)checked(closure.Unbox<ushort>(left) * closure.Unbox<ushort>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<ushort>(left) != closure.Unbox<ushort>(right)));
			}
			public static object Or(Closure closure, object left, object right)
			{
				return closure.Box((ushort)(closure.Unbox<ushort>(left) | closure.Unbox<ushort>(right)));
			}
			public static object Subtract(Closure closure, object left, object right)
			{
				return closure.Box((ushort)unchecked(closure.Unbox<ushort>(left) - closure.Unbox<ushort>(right)));
			}
			public static object SubtractChecked(Closure closure, object left, object right)
			{
				return closure.Box((ushort)checked(closure.Unbox<ushort>(left) - closure.Unbox<ushort>(right)));
			}

			public static object ToObject(Closure closure, object left, object _)
			{
				return closure.Box(left);
			}
			public static object ToSByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((sbyte)closure.Unbox<ushort>(left)));
				else
					return unchecked(closure.Box((sbyte)closure.Unbox<ushort>(left)));
			}
			public static object ToByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((byte)closure.Unbox<ushort>(left)));
				else
					return unchecked(closure.Box((byte)closure.Unbox<ushort>(left)));
			}
			public static object ToInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((short)closure.Unbox<ushort>(left)));
				else
					return unchecked(closure.Box((short)closure.Unbox<ushort>(left)));
			}
			public static object ToUInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ushort)closure.Unbox<ushort>(left)));
				else
					return unchecked(closure.Box((ushort)closure.Unbox<ushort>(left)));
			}
			public static object ToInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((int)closure.Unbox<ushort>(left)));
				else
					return unchecked(closure.Box((int)closure.Unbox<ushort>(left)));
			}
#if UNSIGNED_TYPES
			public static object ToUInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((uint)closure.Unbox<ushort>(left)));
				else
					return unchecked(closure.Box((uint)closure.Unbox<ushort>(left)));
			}
			public static object ToInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((long)closure.Unbox<ushort>(left)));
				else
					return unchecked(closure.Box((long)closure.Unbox<ushort>(left)));
			}
			public static object ToUInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ulong)closure.Unbox<ushort>(left)));
				else
					return unchecked(closure.Box((ulong)closure.Unbox<ushort>(left)));
			}
#endif
			public static object ToSingle(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((float)closure.Unbox<ushort>(left)));
				else
					return unchecked(closure.Box((float)closure.Unbox<ushort>(left)));
			}
			public static object ToDouble(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((double)closure.Unbox<ushort>(left)));
				else
					return unchecked(closure.Box((double)closure.Unbox<ushort>(left)));
			}
			public static object ToDecimal(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((decimal)closure.Unbox<ushort>(left)));
				else
					return unchecked(closure.Box((decimal)closure.Unbox<ushort>(left)));
			}
		}
		private static class op_Int32
		{
			static op_Int32()
			{
				// AOT
				if (typeof(op_Int32).Name == string.Empty)
				{
					Default(default(Closure));
					Negate(default(Closure), default(int));
					NegateChecked(default(Closure), default(object));
					UnaryPlus(default(Closure), default(object));
					Not(default(Closure), default(object));
					Add(default(Closure), default(object), default(object));
					AddChecked(default(Closure), default(object), default(object));
					And(default(Closure), default(object), default(object));
					Divide(default(Closure), default(object), default(object));
					Equal(default(Closure), default(object), default(object));
					ExclusiveOr(default(Closure), default(object), default(object));
					GreaterThan(default(Closure), default(object), default(object));
					GreaterThanOrEqual(default(Closure), default(object), default(object));
					LeftShift(default(Closure), default(object), default(object));
					Power(default(Closure), default(object), default(object));
					RightShift(default(Closure), default(object), default(object));
					LessThan(default(Closure), default(object), default(object));
					LessThanOrEqual(default(Closure), default(object), default(object));
					Modulo(default(Closure), default(object), default(object));
					Multiply(default(Closure), default(object), default(object));
					MultiplyChecked(default(Closure), default(object), default(object));
					NotEqual(default(Closure), default(object), default(object));
					Or(default(Closure), default(object), default(object));
					Subtract(default(Closure), default(object), default(object));
					SubtractChecked(default(Closure), default(object), default(object));
					ToObject(default(Closure), default(object), default(object));
					ToSByte(default(Closure), default(object), default(object));
					ToByte(default(Closure), default(object), default(object));
					ToInt16(default(Closure), default(object), default(object));
					ToUInt16(default(Closure), default(object), default(object));
					ToInt32(default(Closure), default(object), default(object));
#if !UNITY_WEBGL
					ToUInt32(default(Closure), default(object), default(object));
					ToInt64(default(Closure), default(object), default(object));
					ToUInt64(default(Closure), default(object), default(object));
#endif
					ToSingle(default(Closure), default(object), default(object));
					ToDouble(default(Closure), default(object), default(object));
					ToDecimal(default(Closure), default(object), default(object));
				}
			}

			public static object Default(Closure closure)
			{
				return closure.Box(default(int));
			}
			public static object Negate(Closure closure, object operand)
			{
				return closure.Box((int)unchecked(-closure.Unbox<int>(operand)));
			}
			public static object NegateChecked(Closure closure, object operand)
			{
				return closure.Box((int)checked(-closure.Unbox<int>(operand)));
			}
			public static object UnaryPlus(Closure closure, object operand)
			{
				return closure.Box((int)unchecked(+closure.Unbox<int>(operand)));
			}
			public static object Not(Closure closure, object operand)
			{
				return closure.Box((int)~closure.Unbox<int>(operand));
			}
			public static object Add(Closure closure, object left, object right)
			{
				return closure.Box((int)unchecked(closure.Unbox<int>(left) + closure.Unbox<int>(right)));
			}
			public static object AddChecked(Closure closure, object left, object right)
			{
				return closure.Box((int)checked(closure.Unbox<int>(left) + closure.Unbox<int>(right)));
			}
			public static object And(Closure closure, object left, object right)
			{
				return closure.Box((int)(closure.Unbox<int>(left) & closure.Unbox<int>(right)));
			}
			public static object Divide(Closure closure, object left, object right)
			{
				return closure.Box((int)(closure.Unbox<int>(left) / closure.Unbox<int>(right)));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<int>(left) == closure.Unbox<int>(right)));
			}
			public static object ExclusiveOr(Closure closure, object left, object right)
			{
				return closure.Box((int)(closure.Unbox<int>(left) ^ closure.Unbox<int>(right)));
			}
			public static object GreaterThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<int>(left) > closure.Unbox<int>(right)));
			}
			public static object GreaterThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<int>(left) >= closure.Unbox<int>(right)));
			}
			public static object LeftShift(Closure closure, object left, object right)
			{
				return closure.Box((int)(closure.Unbox<int>(left) << closure.Unbox<int>(right)));
			}
			public static object Power(Closure closure, object left, object right)
			{
				return closure.Box((int)Math.Pow(closure.Unbox<int>(left), closure.Unbox<double>(right)));
			}
			public static object RightShift(Closure closure, object left, object right)
			{
				return closure.Box((int)(closure.Unbox<int>(left) >> closure.Unbox<int>(right)));
			}
			public static object LessThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<int>(left) < closure.Unbox<int>(right)));
			}
			public static object LessThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<int>(left) <= closure.Unbox<int>(right)));
			}
			public static object Modulo(Closure closure, object left, object right)
			{
				return closure.Box((int)(closure.Unbox<int>(left) % closure.Unbox<int>(right)));
			}
			public static object Multiply(Closure closure, object left, object right)
			{
				return closure.Box((int)unchecked(closure.Unbox<int>(left) * closure.Unbox<int>(right)));
			}
			public static object MultiplyChecked(Closure closure, object left, object right)
			{
				return closure.Box((int)checked(closure.Unbox<int>(left) * closure.Unbox<int>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<int>(left) != closure.Unbox<int>(right)));
			}
			public static object Or(Closure closure, object left, object right)
			{
				return closure.Box((int)(closure.Unbox<int>(left) | closure.Unbox<int>(right)));
			}
			public static object Subtract(Closure closure, object left, object right)
			{
				return closure.Box((int)unchecked(closure.Unbox<int>(left) - closure.Unbox<int>(right)));
			}
			public static object SubtractChecked(Closure closure, object left, object right)
			{
				return closure.Box((int)checked(closure.Unbox<int>(left) - closure.Unbox<int>(right)));
			}

			public static object ToObject(Closure closure, object left, object _)
			{
				return closure.Box(left);
			}
			public static object ToSByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((sbyte)closure.Unbox<int>(left)));
				else
					return unchecked(closure.Box((sbyte)closure.Unbox<int>(left)));
			}
			public static object ToByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((byte)closure.Unbox<int>(left)));
				else
					return unchecked(closure.Box((byte)closure.Unbox<int>(left)));
			}
			public static object ToInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((short)closure.Unbox<int>(left)));
				else
					return unchecked(closure.Box((short)closure.Unbox<int>(left)));
			}
			public static object ToUInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ushort)closure.Unbox<int>(left)));
				else
					return unchecked(closure.Box((ushort)closure.Unbox<int>(left)));
			}
			public static object ToInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((int)closure.Unbox<int>(left)));
				else
					return unchecked(closure.Box((int)closure.Unbox<int>(left)));
			}
#if UNSIGNED_TYPES
			public static object ToUInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((uint)closure.Unbox<int>(left)));
				else
					return unchecked(closure.Box((uint)closure.Unbox<int>(left)));
			}
			public static object ToInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((long)closure.Unbox<int>(left)));
				else
					return unchecked(closure.Box((long)closure.Unbox<int>(left)));
			}
			public static object ToUInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ulong)closure.Unbox<int>(left)));
				else
					return unchecked(closure.Box((ulong)closure.Unbox<int>(left)));
			}
#endif
			public static object ToSingle(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((float)closure.Unbox<int>(left)));
				else
					return unchecked(closure.Box((float)closure.Unbox<int>(left)));
			}
			public static object ToDouble(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((double)closure.Unbox<int>(left)));
				else
					return unchecked(closure.Box((double)closure.Unbox<int>(left)));
			}
			public static object ToDecimal(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((decimal)closure.Unbox<int>(left)));
				else
					return unchecked(closure.Box((decimal)closure.Unbox<int>(left)));
			}
		}
#if UNSIGNED_TYPES
		private static class op_UInt32
		{
			static op_UInt32()
			{
				// AOT
				if (typeof(op_UInt32).Name == string.Empty)
				{
					Default(default(Closure));
					Negate(default(Closure), default(uint));
					NegateChecked(default(Closure), default(object));
					UnaryPlus(default(Closure), default(object));
					Not(default(Closure), default(object));
					Add(default(Closure), default(object), default(object));
					AddChecked(default(Closure), default(object), default(object));
					And(default(Closure), default(object), default(object));
					Divide(default(Closure), default(object), default(object));
					Equal(default(Closure), default(object), default(object));
					ExclusiveOr(default(Closure), default(object), default(object));
					GreaterThan(default(Closure), default(object), default(object));
					GreaterThanOrEqual(default(Closure), default(object), default(object));
					LeftShift(default(Closure), default(object), default(object));
					Power(default(Closure), default(object), default(object));
					RightShift(default(Closure), default(object), default(object));
					LessThan(default(Closure), default(object), default(object));
					LessThanOrEqual(default(Closure), default(object), default(object));
					Modulo(default(Closure), default(object), default(object));
					Multiply(default(Closure), default(object), default(object));
					MultiplyChecked(default(Closure), default(object), default(object));
					NotEqual(default(Closure), default(object), default(object));
					Or(default(Closure), default(object), default(object));
					Subtract(default(Closure), default(object), default(object));
					SubtractChecked(default(Closure), default(object), default(object));
					ToObject(default(Closure), default(object), default(object));
					ToSByte(default(Closure), default(object), default(object));
					ToByte(default(Closure), default(object), default(object));
					ToInt16(default(Closure), default(object), default(object));
					ToUInt16(default(Closure), default(object), default(object));
					ToInt32(default(Closure), default(object), default(object));
					ToUInt32(default(Closure), default(object), default(object));
					ToInt64(default(Closure), default(object), default(object));
					ToUInt64(default(Closure), default(object), default(object));
					ToSingle(default(Closure), default(object), default(object));
					ToDouble(default(Closure), default(object), default(object));
					ToDecimal(default(Closure), default(object), default(object));
				}
			}

			public static object Default(Closure closure)
			{
				return closure.Box(default(uint));
			}
			public static object Negate(Closure closure, object operand)
			{
				return closure.Box((uint)unchecked(-closure.Unbox<uint>(operand)));
			}
			public static object NegateChecked(Closure closure, object operand)
			{
				return closure.Box((uint)checked(-closure.Unbox<uint>(operand)));
			}
			public static object UnaryPlus(Closure closure, object operand)
			{
				return closure.Box((uint)unchecked(+closure.Unbox<uint>(operand)));
			}
			public static object Not(Closure closure, object operand)
			{
				return closure.Box((uint)~closure.Unbox<uint>(operand));
			}
			public static object Add(Closure closure, object left, object right)
			{
				return closure.Box((uint)unchecked(closure.Unbox<uint>(left) + closure.Unbox<uint>(right)));
			}
			public static object AddChecked(Closure closure, object left, object right)
			{
				return closure.Box((uint)checked(closure.Unbox<uint>(left) + closure.Unbox<uint>(right)));
			}
			public static object And(Closure closure, object left, object right)
			{
				return closure.Box((uint)(closure.Unbox<uint>(left) & closure.Unbox<uint>(right)));
			}
			public static object Divide(Closure closure, object left, object right)
			{
				return closure.Box((uint)(closure.Unbox<uint>(left) / closure.Unbox<uint>(right)));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<uint>(left) == closure.Unbox<uint>(right)));
			}
			public static object ExclusiveOr(Closure closure, object left, object right)
			{
				return closure.Box((uint)(closure.Unbox<uint>(left) ^ closure.Unbox<uint>(right)));
			}
			public static object GreaterThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<uint>(left) > closure.Unbox<uint>(right)));
			}
			public static object GreaterThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<uint>(left) >= closure.Unbox<uint>(right)));
			}
			public static object LeftShift(Closure closure, object left, object right)
			{
				return closure.Box((uint)(closure.Unbox<uint>(left) << closure.Unbox<int>(right)));
			}
			public static object Power(Closure closure, object left, object right)
			{
				return closure.Box((uint)Math.Pow(closure.Unbox<uint>(left), closure.Unbox<double>(right)));
			}
			public static object RightShift(Closure closure, object left, object right)
			{
				return closure.Box((uint)(closure.Unbox<uint>(left) >> closure.Unbox<int>(right)));
			}
			public static object LessThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<uint>(left) < closure.Unbox<uint>(right)));
			}
			public static object LessThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<uint>(left) <= closure.Unbox<uint>(right)));
			}
			public static object Modulo(Closure closure, object left, object right)
			{
				return closure.Box((uint)(closure.Unbox<uint>(left) % closure.Unbox<uint>(right)));
			}
			public static object Multiply(Closure closure, object left, object right)
			{
				return closure.Box((uint)unchecked(closure.Unbox<uint>(left) * closure.Unbox<uint>(right)));
			}
			public static object MultiplyChecked(Closure closure, object left, object right)
			{
				return closure.Box((uint)checked(closure.Unbox<uint>(left) * closure.Unbox<uint>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<uint>(left) != closure.Unbox<uint>(right)));
			}
			public static object Or(Closure closure, object left, object right)
			{
				return closure.Box((uint)(closure.Unbox<uint>(left) | closure.Unbox<uint>(right)));
			}
			public static object Subtract(Closure closure, object left, object right)
			{
				return closure.Box((uint)unchecked(closure.Unbox<uint>(left) - closure.Unbox<uint>(right)));
			}
			public static object SubtractChecked(Closure closure, object left, object right)
			{
				return closure.Box((uint)checked(closure.Unbox<uint>(left) - closure.Unbox<uint>(right)));
			}

			public static object ToObject(Closure closure, object left, object _)
			{
				return closure.Box(left);
			}
			public static object ToSByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((sbyte)closure.Unbox<uint>(left)));
				else
					return unchecked(closure.Box((sbyte)closure.Unbox<uint>(left)));
			}
			public static object ToByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((byte)closure.Unbox<uint>(left)));
				else
					return unchecked(closure.Box((byte)closure.Unbox<uint>(left)));
			}
			public static object ToInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((short)closure.Unbox<uint>(left)));
				else
					return unchecked(closure.Box((short)closure.Unbox<uint>(left)));
			}
			public static object ToUInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ushort)closure.Unbox<uint>(left)));
				else
					return unchecked(closure.Box((ushort)closure.Unbox<uint>(left)));
			}
			public static object ToInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((int)closure.Unbox<uint>(left)));
				else
					return unchecked(closure.Box((int)closure.Unbox<uint>(left)));
			}
#if UNSIGNED_TYPES
			public static object ToUInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((uint)closure.Unbox<uint>(left)));
				else
					return unchecked(closure.Box((uint)closure.Unbox<uint>(left)));
			}
			public static object ToInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((long)closure.Unbox<uint>(left)));
				else
					return unchecked(closure.Box((long)closure.Unbox<uint>(left)));
			}
			public static object ToUInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ulong)closure.Unbox<uint>(left)));
				else
					return unchecked(closure.Box((ulong)closure.Unbox<uint>(left)));
			}
#endif
			public static object ToSingle(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((float)closure.Unbox<uint>(left)));
				else
					return unchecked(closure.Box((float)closure.Unbox<uint>(left)));
			}
			public static object ToDouble(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((double)closure.Unbox<uint>(left)));
				else
					return unchecked(closure.Box((double)closure.Unbox<uint>(left)));
			}
			public static object ToDecimal(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((decimal)closure.Unbox<uint>(left)));
				else
					return unchecked(closure.Box((decimal)closure.Unbox<uint>(left)));
			}
		}
		private static class op_Int64
		{
			static op_Int64()
			{
				// AOT
				if (typeof(op_Int64).Name == string.Empty)
				{
					Default(default(Closure));
					Negate(default(Closure), default(long));
					NegateChecked(default(Closure), default(object));
					UnaryPlus(default(Closure), default(object));
					Not(default(Closure), default(object));
					Add(default(Closure), default(object), default(object));
					AddChecked(default(Closure), default(object), default(object));
					And(default(Closure), default(object), default(object));
					Divide(default(Closure), default(object), default(object));
					Equal(default(Closure), default(object), default(object));
					ExclusiveOr(default(Closure), default(object), default(object));
					GreaterThan(default(Closure), default(object), default(object));
					GreaterThanOrEqual(default(Closure), default(object), default(object));
					LeftShift(default(Closure), default(object), default(object));
					Power(default(Closure), default(object), default(object));
					RightShift(default(Closure), default(object), default(object));
					LessThan(default(Closure), default(object), default(object));
					LessThanOrEqual(default(Closure), default(object), default(object));
					Modulo(default(Closure), default(object), default(object));
					Multiply(default(Closure), default(object), default(object));
					MultiplyChecked(default(Closure), default(object), default(object));
					NotEqual(default(Closure), default(object), default(object));
					Or(default(Closure), default(object), default(object));
					Subtract(default(Closure), default(object), default(object));
					SubtractChecked(default(Closure), default(object), default(object));
					ToObject(default(Closure), default(object), default(object));
					ToSByte(default(Closure), default(object), default(object));
					ToByte(default(Closure), default(object), default(object));
					ToInt16(default(Closure), default(object), default(object));
					ToUInt16(default(Closure), default(object), default(object));
					ToInt32(default(Closure), default(object), default(object));
					ToUInt32(default(Closure), default(object), default(object));
					ToInt64(default(Closure), default(object), default(object));
					ToUInt64(default(Closure), default(object), default(object));
					ToSingle(default(Closure), default(object), default(object));
					ToDouble(default(Closure), default(object), default(object));
					ToDecimal(default(Closure), default(object), default(object));
				}
			}

			public static object Default(Closure closure)
			{
				return closure.Box(default(long));
			}
			public static object Negate(Closure closure, object operand)
			{
				return closure.Box((long)unchecked(-closure.Unbox<long>(operand)));
			}
			public static object NegateChecked(Closure closure, object operand)
			{
				return closure.Box((long)checked(-closure.Unbox<long>(operand)));
			}
			public static object UnaryPlus(Closure closure, object operand)
			{
				return closure.Box((long)unchecked(+closure.Unbox<long>(operand)));
			}
			public static object Not(Closure closure, object operand)
			{
				return closure.Box((long)~closure.Unbox<long>(operand));
			}
			public static object Add(Closure closure, object left, object right)
			{
				return closure.Box((long)unchecked(closure.Unbox<long>(left) + closure.Unbox<long>(right)));
			}
			public static object AddChecked(Closure closure, object left, object right)
			{
				return closure.Box((long)checked(closure.Unbox<long>(left) + closure.Unbox<long>(right)));
			}
			public static object And(Closure closure, object left, object right)
			{
				return closure.Box((long)(closure.Unbox<long>(left) & closure.Unbox<long>(right)));
			}
			public static object Divide(Closure closure, object left, object right)
			{
				return closure.Box((long)(closure.Unbox<long>(left) / closure.Unbox<long>(right)));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<long>(left) == closure.Unbox<long>(right)));
			}
			public static object ExclusiveOr(Closure closure, object left, object right)
			{
				return closure.Box((long)(closure.Unbox<long>(left) ^ closure.Unbox<long>(right)));
			}
			public static object GreaterThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<long>(left) > closure.Unbox<long>(right)));
			}
			public static object GreaterThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<long>(left) >= closure.Unbox<long>(right)));
			}
			public static object LeftShift(Closure closure, object left, object right)
			{
				return closure.Box((long)(closure.Unbox<long>(left) << closure.Unbox<int>(right)));
			}
			public static object Power(Closure closure, object left, object right)
			{
				return closure.Box((long)Math.Pow(closure.Unbox<long>(left), closure.Unbox<double>(right)));
			}
			public static object RightShift(Closure closure, object left, object right)
			{
				return closure.Box((long)(closure.Unbox<long>(left) >> closure.Unbox<int>(right)));
			}
			public static object LessThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<long>(left) < closure.Unbox<long>(right)));
			}
			public static object LessThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<long>(left) <= closure.Unbox<long>(right)));
			}
			public static object Modulo(Closure closure, object left, object right)
			{
				return closure.Box((long)(closure.Unbox<long>(left) % closure.Unbox<long>(right)));
			}
			public static object Multiply(Closure closure, object left, object right)
			{
				return closure.Box((long)unchecked(closure.Unbox<long>(left) * closure.Unbox<long>(right)));
			}
			public static object MultiplyChecked(Closure closure, object left, object right)
			{
				return closure.Box((long)checked(closure.Unbox<long>(left) * closure.Unbox<long>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<long>(left) != closure.Unbox<long>(right)));
			}
			public static object Or(Closure closure, object left, object right)
			{
				return closure.Box((long)(closure.Unbox<long>(left) | closure.Unbox<long>(right)));
			}
			public static object Subtract(Closure closure, object left, object right)
			{
				return closure.Box((long)unchecked(closure.Unbox<long>(left) - closure.Unbox<long>(right)));
			}
			public static object SubtractChecked(Closure closure, object left, object right)
			{
				return closure.Box((long)checked(closure.Unbox<long>(left) - closure.Unbox<long>(right)));
			}

			public static object ToObject(Closure closure, object left, object _)
			{
				return closure.Box(left);
			}
			public static object ToSByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((sbyte)closure.Unbox<long>(left)));
				else
					return unchecked(closure.Box((sbyte)closure.Unbox<long>(left)));
			}
			public static object ToByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((byte)closure.Unbox<long>(left)));
				else
					return unchecked(closure.Box((byte)closure.Unbox<long>(left)));
			}
			public static object ToInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((short)closure.Unbox<long>(left)));
				else
					return unchecked(closure.Box((short)closure.Unbox<long>(left)));
			}
			public static object ToUInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ushort)closure.Unbox<long>(left)));
				else
					return unchecked(closure.Box((ushort)closure.Unbox<long>(left)));
			}
			public static object ToInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((int)closure.Unbox<long>(left)));
				else
					return unchecked(closure.Box((int)closure.Unbox<long>(left)));
			}
#if UNSIGNED_TYPES
			public static object ToUInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((uint)closure.Unbox<long>(left)));
				else
					return unchecked(closure.Box((uint)closure.Unbox<long>(left)));
			}
			public static object ToInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((long)closure.Unbox<long>(left)));
				else
					return unchecked(closure.Box((long)closure.Unbox<long>(left)));
			}
			public static object ToUInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ulong)closure.Unbox<long>(left)));
				else
					return unchecked(closure.Box((ulong)closure.Unbox<long>(left)));
			}
#endif
			public static object ToSingle(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((float)closure.Unbox<long>(left)));
				else
					return unchecked(closure.Box((float)closure.Unbox<long>(left)));
			}
			public static object ToDouble(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((double)closure.Unbox<long>(left)));
				else
					return unchecked(closure.Box((double)closure.Unbox<long>(left)));
			}
			public static object ToDecimal(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((decimal)closure.Unbox<long>(left)));
				else
					return unchecked(closure.Box((decimal)closure.Unbox<long>(left)));
			}
		}
		private static class op_UInt64
		{
			static op_UInt64()
			{
				// AOT
				if (typeof(op_UInt64).Name == string.Empty)
				{
					Default(default(Closure));
					UnaryPlus(default(Closure), default(ulong));
					Not(default(Closure), default(object));
					Add(default(Closure), default(object), default(object));
					AddChecked(default(Closure), default(object), default(object));
					And(default(Closure), default(object), default(object));
					Divide(default(Closure), default(object), default(object));
					Equal(default(Closure), default(object), default(object));
					ExclusiveOr(default(Closure), default(object), default(object));
					GreaterThan(default(Closure), default(object), default(object));
					GreaterThanOrEqual(default(Closure), default(object), default(object));
					LeftShift(default(Closure), default(object), default(object));
					Power(default(Closure), default(object), default(object));
					RightShift(default(Closure), default(object), default(object));
					LessThan(default(Closure), default(object), default(object));
					LessThanOrEqual(default(Closure), default(object), default(object));
					Modulo(default(Closure), default(object), default(object));
					Multiply(default(Closure), default(object), default(object));
					MultiplyChecked(default(Closure), default(object), default(object));
					NotEqual(default(Closure), default(object), default(object));
					Or(default(Closure), default(object), default(object));
					Subtract(default(Closure), default(object), default(object));
					SubtractChecked(default(Closure), default(object), default(object));
					ToObject(default(Closure), default(object), default(object));
					ToSByte(default(Closure), default(object), default(object));
					ToByte(default(Closure), default(object), default(object));
					ToInt16(default(Closure), default(object), default(object));
					ToUInt16(default(Closure), default(object), default(object));
					ToInt32(default(Closure), default(object), default(object));
					ToUInt32(default(Closure), default(object), default(object));
					ToInt64(default(Closure), default(object), default(object));
					ToUInt64(default(Closure), default(object), default(object));
					ToSingle(default(Closure), default(object), default(object));
					ToDouble(default(Closure), default(object), default(object));
					ToDecimal(default(Closure), default(object), default(object));
				}
			}

			public static object Default(Closure closure)
			{
				return closure.Box(default(ulong));
			}
			public static object UnaryPlus(Closure closure, object operand)
			{
				return closure.Box((ulong)unchecked(+closure.Unbox<ulong>(operand)));
			}
			public static object Not(Closure closure, object operand)
			{
				return closure.Box((ulong)~closure.Unbox<ulong>(operand));
			}
			public static object Add(Closure closure, object left, object right)
			{
				return closure.Box((ulong)unchecked(closure.Unbox<ulong>(left) + closure.Unbox<ulong>(right)));
			}
			public static object AddChecked(Closure closure, object left, object right)
			{
				return closure.Box((ulong)checked(closure.Unbox<ulong>(left) + closure.Unbox<ulong>(right)));
			}
			public static object And(Closure closure, object left, object right)
			{
				return closure.Box((ulong)(closure.Unbox<ulong>(left) & closure.Unbox<ulong>(right)));
			}
			public static object Divide(Closure closure, object left, object right)
			{
				return closure.Box((ulong)(closure.Unbox<ulong>(left) / closure.Unbox<ulong>(right)));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<ulong>(left) == closure.Unbox<ulong>(right)));
			}
			public static object ExclusiveOr(Closure closure, object left, object right)
			{
				return closure.Box((ulong)(closure.Unbox<ulong>(left) ^ closure.Unbox<ulong>(right)));
			}
			public static object GreaterThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<ulong>(left) > closure.Unbox<ulong>(right)));
			}
			public static object GreaterThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<ulong>(left) >= closure.Unbox<ulong>(right)));
			}
			public static object LeftShift(Closure closure, object left, object right)
			{
				return closure.Box((ulong)(closure.Unbox<ulong>(left) << closure.Unbox<int>(right)));
			}
			public static object Power(Closure closure, object left, object right)
			{
				return closure.Box((ulong)Math.Pow(closure.Unbox<ulong>(left), closure.Unbox<double>(right)));
			}
			public static object RightShift(Closure closure, object left, object right)
			{
				return closure.Box((ulong)(closure.Unbox<ulong>(left) >> closure.Unbox<int>(right)));
			}
			public static object LessThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<ulong>(left) < closure.Unbox<ulong>(right)));
			}
			public static object LessThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<ulong>(left) <= closure.Unbox<ulong>(right)));
			}
			public static object Modulo(Closure closure, object left, object right)
			{
				return closure.Box((ulong)(closure.Unbox<ulong>(left) % closure.Unbox<ulong>(right)));
			}
			public static object Multiply(Closure closure, object left, object right)
			{
				return closure.Box((ulong)unchecked(closure.Unbox<ulong>(left) * closure.Unbox<ulong>(right)));
			}
			public static object MultiplyChecked(Closure closure, object left, object right)
			{
				return closure.Box((ulong)checked(closure.Unbox<ulong>(left) * closure.Unbox<ulong>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<ulong>(left) != closure.Unbox<ulong>(right)));
			}
			public static object Or(Closure closure, object left, object right)
			{
				return closure.Box((ulong)(closure.Unbox<ulong>(left) | closure.Unbox<ulong>(right)));
			}
			public static object Subtract(Closure closure, object left, object right)
			{
				return closure.Box((ulong)unchecked(closure.Unbox<ulong>(left) - closure.Unbox<ulong>(right)));
			}
			public static object SubtractChecked(Closure closure, object left, object right)
			{
				return closure.Box((ulong)checked(closure.Unbox<ulong>(left) - closure.Unbox<ulong>(right)));
			}

			public static object ToObject(Closure closure, object left, object _)
			{
				return closure.Box(left);
			}
			public static object ToSByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((sbyte)closure.Unbox<ulong>(left)));
				else
					return unchecked(closure.Box((sbyte)closure.Unbox<ulong>(left)));
			}
			public static object ToByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((byte)closure.Unbox<ulong>(left)));
				else
					return unchecked(closure.Box((byte)closure.Unbox<ulong>(left)));
			}
			public static object ToInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((short)closure.Unbox<ulong>(left)));
				else
					return unchecked(closure.Box((short)closure.Unbox<ulong>(left)));
			}
			public static object ToUInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ushort)closure.Unbox<ulong>(left)));
				else
					return unchecked(closure.Box((ushort)closure.Unbox<ulong>(left)));
			}
			public static object ToInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((int)closure.Unbox<ulong>(left)));
				else
					return unchecked(closure.Box((int)closure.Unbox<ulong>(left)));
			}
#if UNSIGNED_TYPES
			public static object ToUInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((uint)closure.Unbox<ulong>(left)));
				else
					return unchecked(closure.Box((uint)closure.Unbox<ulong>(left)));
			}
			public static object ToInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((long)closure.Unbox<ulong>(left)));
				else
					return unchecked(closure.Box((long)closure.Unbox<ulong>(left)));
			}
			public static object ToUInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ulong)closure.Unbox<ulong>(left)));
				else
					return unchecked(closure.Box((ulong)closure.Unbox<ulong>(left)));
			}
#endif
			public static object ToSingle(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((float)closure.Unbox<ulong>(left)));
				else
					return unchecked(closure.Box((float)closure.Unbox<ulong>(left)));
			}
			public static object ToDouble(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((double)closure.Unbox<ulong>(left)));
				else
					return unchecked(closure.Box((double)closure.Unbox<ulong>(left)));
			}
			public static object ToDecimal(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((decimal)closure.Unbox<ulong>(left)));
				else
					return unchecked(closure.Box((decimal)closure.Unbox<ulong>(left)));
			}
		}
#endif
		private static class op_Single
		{
			static op_Single()
			{
				// AOT
				if (typeof(op_Single).Name == string.Empty)
				{
					Default(default(Closure));
					Negate(default(Closure), default(float));
					NegateChecked(default(Closure), default(object));
					UnaryPlus(default(Closure), default(object));
					Add(default(Closure), default(object), default(object));
					AddChecked(default(Closure), default(object), default(object));
					Divide(default(Closure), default(object), default(object));
					Equal(default(Closure), default(object), default(object));
					GreaterThan(default(Closure), default(object), default(object));
					GreaterThanOrEqual(default(Closure), default(object), default(object));
					Power(default(Closure), default(object), default(object));
					LessThan(default(Closure), default(object), default(object));
					LessThanOrEqual(default(Closure), default(object), default(object));
					Modulo(default(Closure), default(object), default(object));
					Multiply(default(Closure), default(object), default(object));
					MultiplyChecked(default(Closure), default(object), default(object));
					NotEqual(default(Closure), default(object), default(object));
					Subtract(default(Closure), default(object), default(object));
					SubtractChecked(default(Closure), default(object), default(object));
					ToObject(default(Closure), default(object), default(object));
					ToSByte(default(Closure), default(object), default(object));
					ToByte(default(Closure), default(object), default(object));
					ToInt16(default(Closure), default(object), default(object));
					ToUInt16(default(Closure), default(object), default(object));
					ToInt32(default(Closure), default(object), default(object));
#if !UNITY_WEBGL
					ToUInt32(default(Closure), default(object), default(object));
					ToInt64(default(Closure), default(object), default(object));
					ToUInt64(default(Closure), default(object), default(object));
#endif
					ToSingle(default(Closure), default(object), default(object));
					ToDouble(default(Closure), default(object), default(object));
					ToDecimal(default(Closure), default(object), default(object));
				}
			}

			public static object Default(Closure closure)
			{
				return closure.Box(default(float));
			}
			public static object Negate(Closure closure, object operand)
			{
				return closure.Box((float)unchecked(-closure.Unbox<float>(operand)));
			}
			public static object NegateChecked(Closure closure, object operand)
			{
				return closure.Box((float)checked(-closure.Unbox<float>(operand)));
			}
			public static object UnaryPlus(Closure closure, object operand)
			{
				return closure.Box((float)unchecked(+closure.Unbox<float>(operand)));
			}
			public static object Add(Closure closure, object left, object right)
			{
				return closure.Box((float)unchecked(closure.Unbox<float>(left) + closure.Unbox<float>(right)));
			}
			public static object AddChecked(Closure closure, object left, object right)
			{
				return closure.Box((float)checked(closure.Unbox<float>(left) + closure.Unbox<float>(right)));
			}
			public static object Divide(Closure closure, object left, object right)
			{
				return closure.Box((float)(closure.Unbox<float>(left) / closure.Unbox<float>(right)));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<float>(left) == closure.Unbox<float>(right)));
			}
			public static object GreaterThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<float>(left) > closure.Unbox<float>(right)));
			}
			public static object GreaterThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<float>(left) >= closure.Unbox<float>(right)));
			}
			public static object Power(Closure closure, object left, object right)
			{
				return closure.Box((float)Math.Pow(closure.Unbox<float>(left), closure.Unbox<double>(right)));
			}
			public static object LessThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<float>(left) < closure.Unbox<float>(right)));
			}
			public static object LessThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<float>(left) <= closure.Unbox<float>(right)));
			}
			public static object Modulo(Closure closure, object left, object right)
			{
				return closure.Box((float)(closure.Unbox<float>(left) % closure.Unbox<float>(right)));
			}
			public static object Multiply(Closure closure, object left, object right)
			{
				return closure.Box((float)unchecked(closure.Unbox<float>(left) * closure.Unbox<float>(right)));
			}
			public static object MultiplyChecked(Closure closure, object left, object right)
			{
				return closure.Box((float)checked(closure.Unbox<float>(left) * closure.Unbox<float>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<float>(left) != closure.Unbox<float>(right)));
			}
			public static object Subtract(Closure closure, object left, object right)
			{
				return closure.Box((float)unchecked(closure.Unbox<float>(left) - closure.Unbox<float>(right)));
			}
			public static object SubtractChecked(Closure closure, object left, object right)
			{
				return closure.Box((float)checked(closure.Unbox<float>(left) - closure.Unbox<float>(right)));
			}

			public static object ToObject(Closure closure, object left, object isChecked)
			{
				return closure.Box(left);
			}
			public static object ToSByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((sbyte)closure.Unbox<float>(left)));
				else
					return unchecked(closure.Box((sbyte)closure.Unbox<float>(left)));
			}
			public static object ToByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((byte)closure.Unbox<float>(left)));
				else
					return unchecked(closure.Box((byte)closure.Unbox<float>(left)));
			}
			public static object ToInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((short)closure.Unbox<float>(left)));
				else
					return unchecked(closure.Box((short)closure.Unbox<float>(left)));
			}
			public static object ToUInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ushort)closure.Unbox<float>(left)));
				else
					return unchecked(closure.Box((ushort)closure.Unbox<float>(left)));
			}
			public static object ToInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((int)closure.Unbox<float>(left)));
				else
					return unchecked(closure.Box((int)closure.Unbox<float>(left)));
			}
#if UNSIGNED_TYPES
			public static object ToUInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((uint)closure.Unbox<float>(left)));
				else
					return unchecked(closure.Box((uint)closure.Unbox<float>(left)));
			}
			public static object ToInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((long)closure.Unbox<float>(left)));
				else
					return unchecked(closure.Box((long)closure.Unbox<float>(left)));
			}
			public static object ToUInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ulong)closure.Unbox<float>(left)));
				else
					return unchecked(closure.Box((ulong)closure.Unbox<float>(left)));
			}
#endif
			public static object ToSingle(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((float)closure.Unbox<float>(left)));
				else
					return unchecked(closure.Box((float)closure.Unbox<float>(left)));
			}
			public static object ToDouble(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((double)closure.Unbox<float>(left)));
				else
					return unchecked(closure.Box((double)closure.Unbox<float>(left)));
			}
			public static object ToDecimal(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((decimal)closure.Unbox<float>(left)));
				else
					return unchecked(closure.Box((decimal)closure.Unbox<float>(left)));
			}
		}
		private static class op_Double
		{
			static op_Double()
			{
				// AOT
				if (typeof(op_Double).Name == string.Empty)
				{
					Default(default(Closure));
					Negate(default(Closure), default(double));
					NegateChecked(default(Closure), default(object));
					UnaryPlus(default(Closure), default(object));
					Add(default(Closure), default(object), default(object));
					AddChecked(default(Closure), default(object), default(object));
					Divide(default(Closure), default(object), default(object));
					Equal(default(Closure), default(object), default(object));
					GreaterThan(default(Closure), default(object), default(object));
					GreaterThanOrEqual(default(Closure), default(object), default(object));
					Power(default(Closure), default(object), default(object));
					LessThan(default(Closure), default(object), default(object));
					LessThanOrEqual(default(Closure), default(object), default(object));
					Modulo(default(Closure), default(object), default(object));
					Multiply(default(Closure), default(object), default(object));
					MultiplyChecked(default(Closure), default(object), default(object));
					NotEqual(default(Closure), default(object), default(object));
					Subtract(default(Closure), default(object), default(object));
					SubtractChecked(default(Closure), default(object), default(object));
					ToObject(default(Closure), default(object), default(object));
					ToSByte(default(Closure), default(object), default(object));
					ToByte(default(Closure), default(object), default(object));
					ToInt16(default(Closure), default(object), default(object));
					ToUInt16(default(Closure), default(object), default(object));
					ToInt32(default(Closure), default(object), default(object));
#if !UNITY_WEBGL
					ToUInt32(default(Closure), default(object), default(object));
					ToInt64(default(Closure), default(object), default(object));
					ToUInt64(default(Closure), default(object), default(object));
#endif
					ToSingle(default(Closure), default(object), default(object));
					ToDouble(default(Closure), default(object), default(object));
					ToDecimal(default(Closure), default(object), default(object));
				}
			}

			public static object Default(Closure closure)
			{
				return closure.Box(default(double));
			}
			public static object Negate(Closure closure, object operand)
			{
				return closure.Box((double)unchecked(-closure.Unbox<double>(operand)));
			}
			public static object NegateChecked(Closure closure, object operand)
			{
				return closure.Box((double)checked(-closure.Unbox<double>(operand)));
			}
			public static object UnaryPlus(Closure closure, object operand)
			{
				return closure.Box((double)unchecked(+closure.Unbox<double>(operand)));
			}
			public static object Add(Closure closure, object left, object right)
			{
				return closure.Box((double)unchecked(closure.Unbox<double>(left) + closure.Unbox<double>(right)));
			}
			public static object AddChecked(Closure closure, object left, object right)
			{
				return closure.Box((double)checked(closure.Unbox<double>(left) + closure.Unbox<double>(right)));
			}
			public static object Divide(Closure closure, object left, object right)
			{
				return closure.Box((double)(closure.Unbox<double>(left) / closure.Unbox<double>(right)));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<double>(left) == closure.Unbox<double>(right)));
			}
			public static object GreaterThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<double>(left) > closure.Unbox<double>(right)));
			}
			public static object GreaterThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<double>(left) >= closure.Unbox<double>(right)));
			}
			public static object Power(Closure closure, object left, object right)
			{
				return closure.Box((double)Math.Pow(closure.Unbox<double>(left), closure.Unbox<double>(right)));
			}
			public static object LessThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<double>(left) < closure.Unbox<double>(right)));
			}
			public static object LessThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<double>(left) <= closure.Unbox<double>(right)));
			}
			public static object Modulo(Closure closure, object left, object right)
			{
				return closure.Box((double)(closure.Unbox<double>(left) % closure.Unbox<double>(right)));
			}
			public static object Multiply(Closure closure, object left, object right)
			{
				return closure.Box((double)unchecked(closure.Unbox<double>(left) * closure.Unbox<double>(right)));
			}
			public static object MultiplyChecked(Closure closure, object left, object right)
			{
				return closure.Box((double)checked(closure.Unbox<double>(left) * closure.Unbox<double>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<double>(left) != closure.Unbox<double>(right)));
			}
			public static object Subtract(Closure closure, object left, object right)
			{
				return closure.Box((double)unchecked(closure.Unbox<double>(left) - closure.Unbox<double>(right)));
			}
			public static object SubtractChecked(Closure closure, object left, object right)
			{
				return closure.Box((double)checked(closure.Unbox<double>(left) - closure.Unbox<double>(right)));
			}

			public static object ToObject(Closure closure, object left, object _)
			{
				return closure.Box(left);
			}
			public static object ToSByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((sbyte)closure.Unbox<double>(left)));
				else
					return unchecked(closure.Box((sbyte)closure.Unbox<double>(left)));
			}
			public static object ToByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((byte)closure.Unbox<double>(left)));
				else
					return unchecked(closure.Box((byte)closure.Unbox<double>(left)));
			}
			public static object ToInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((short)closure.Unbox<double>(left)));
				else
					return unchecked(closure.Box((short)closure.Unbox<double>(left)));
			}
			public static object ToUInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ushort)closure.Unbox<double>(left)));
				else
					return unchecked(closure.Box((ushort)closure.Unbox<double>(left)));
			}
			public static object ToInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((int)closure.Unbox<double>(left)));
				else
					return unchecked(closure.Box((int)closure.Unbox<double>(left)));
			}
#if UNSIGNED_TYPES
			public static object ToUInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((uint)closure.Unbox<double>(left)));
				else
					return unchecked(closure.Box((uint)closure.Unbox<double>(left)));
			}
			public static object ToInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((long)closure.Unbox<double>(left)));
				else
					return unchecked(closure.Box((long)closure.Unbox<double>(left)));
			}
			public static object ToUInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ulong)closure.Unbox<double>(left)));
				else
					return unchecked(closure.Box((ulong)closure.Unbox<double>(left)));
			}
#endif
			public static object ToSingle(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((float)closure.Unbox<double>(left)));
				else
					return unchecked(closure.Box((float)closure.Unbox<double>(left)));
			}
			public static object ToDouble(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((double)closure.Unbox<double>(left)));
				else
					return unchecked(closure.Box((double)closure.Unbox<double>(left)));
			}
			public static object ToDecimal(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((decimal)closure.Unbox<double>(left)));
				else
					return unchecked(closure.Box((decimal)closure.Unbox<double>(left)));
			}
		}
		private static class op_Decimal
		{
			static op_Decimal()
			{
				// AOT
				if (typeof(op_Decimal).Name == string.Empty)
				{
					Default(default(Closure));
					Negate(default(Closure), default(decimal));
					NegateChecked(default(Closure), default(object));
					UnaryPlus(default(Closure), default(object));
					Add(default(Closure), default(object), default(object));
					AddChecked(default(Closure), default(object), default(object));
					Divide(default(Closure), default(object), default(object));
					Equal(default(Closure), default(object), default(object));
					GreaterThan(default(Closure), default(object), default(object));
					GreaterThanOrEqual(default(Closure), default(object), default(object));
					Power(default(Closure), default(object), default(object));
					LessThan(default(Closure), default(object), default(object));
					LessThanOrEqual(default(Closure), default(object), default(object));
					Modulo(default(Closure), default(object), default(object));
					Multiply(default(Closure), default(object), default(object));
					MultiplyChecked(default(Closure), default(object), default(object));
					NotEqual(default(Closure), default(object), default(object));
					Subtract(default(Closure), default(object), default(object));
					SubtractChecked(default(Closure), default(object), default(object));
					ToObject(default(Closure), default(object), default(object));
					ToSByte(default(Closure), default(object), default(object));
					ToByte(default(Closure), default(object), default(object));
					ToInt16(default(Closure), default(object), default(object));
					ToUInt16(default(Closure), default(object), default(object));
					ToInt32(default(Closure), default(object), default(object));
#if !UNITY_WEBGL
					ToUInt32(default(Closure), default(object), default(object));
					ToInt64(default(Closure), default(object), default(object));
					ToUInt64(default(Closure), default(object), default(object));
#endif
					ToSingle(default(Closure), default(object), default(object));
					ToDouble(default(Closure), default(object), default(object));
					ToDecimal(default(Closure), default(object), default(object));
				}
			}

			public static object Default(Closure closure)
			{
				return closure.Box(default(decimal));
			}
			public static object Negate(Closure closure, object operand)
			{
				return closure.Box((decimal)unchecked(-closure.Unbox<decimal>(operand)));
			}
			public static object NegateChecked(Closure closure, object operand)
			{
				return closure.Box((decimal)checked(-closure.Unbox<decimal>(operand)));
			}
			public static object UnaryPlus(Closure closure, object operand)
			{
				return closure.Box((decimal)unchecked(+closure.Unbox<decimal>(operand)));
			}
			public static object Add(Closure closure, object left, object right)
			{
				return closure.Box((decimal)unchecked(closure.Unbox<decimal>(left) + closure.Unbox<decimal>(right)));
			}
			public static object AddChecked(Closure closure, object left, object right)
			{
				return closure.Box((decimal)checked(closure.Unbox<decimal>(left) + closure.Unbox<decimal>(right)));
			}
			public static object Divide(Closure closure, object left, object right)
			{
				return closure.Box((decimal)(closure.Unbox<decimal>(left) / closure.Unbox<decimal>(right)));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<decimal>(left) == closure.Unbox<decimal>(right)));
			}
			public static object GreaterThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<decimal>(left) > closure.Unbox<decimal>(right)));
			}
			public static object GreaterThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<decimal>(left) >= closure.Unbox<decimal>(right)));
			}
			public static object Power(Closure closure, object left, object right)
			{
				return closure.Box((decimal)Math.Pow((double)closure.Unbox<decimal>(left), closure.Unbox<double>(right)));
			}
			public static object LessThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<decimal>(left) < closure.Unbox<decimal>(right)));
			}
			public static object LessThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<decimal>(left) <= closure.Unbox<decimal>(right)));
			}
			public static object Modulo(Closure closure, object left, object right)
			{
				return closure.Box((decimal)(closure.Unbox<decimal>(left) % closure.Unbox<decimal>(right)));
			}
			public static object Multiply(Closure closure, object left, object right)
			{
				return closure.Box((decimal)unchecked(closure.Unbox<decimal>(left) * closure.Unbox<decimal>(right)));
			}
			public static object MultiplyChecked(Closure closure, object left, object right)
			{
				return closure.Box((decimal)checked(closure.Unbox<decimal>(left) * closure.Unbox<decimal>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<decimal>(left) != closure.Unbox<decimal>(right)));
			}
			public static object Subtract(Closure closure, object left, object right)
			{
				return closure.Box((decimal)unchecked(closure.Unbox<decimal>(left) - closure.Unbox<decimal>(right)));
			}
			public static object SubtractChecked(Closure closure, object left, object right)
			{
				return closure.Box((decimal)checked(closure.Unbox<decimal>(left) - closure.Unbox<decimal>(right)));
			}

			public static object ToObject(Closure closure, object left, object isChecked)
			{
				return closure.Box(left);
			}
			public static object ToSByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((sbyte)closure.Unbox<decimal>(left)));
				else
					return unchecked(closure.Box((sbyte)closure.Unbox<decimal>(left)));
			}
			public static object ToByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((byte)closure.Unbox<decimal>(left)));
				else
					return unchecked(closure.Box((byte)closure.Unbox<decimal>(left)));
			}
			public static object ToInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((short)closure.Unbox<decimal>(left)));
				else
					return unchecked(closure.Box((short)closure.Unbox<decimal>(left)));
			}
			public static object ToUInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ushort)closure.Unbox<decimal>(left)));
				else
					return unchecked(closure.Box((ushort)closure.Unbox<decimal>(left)));
			}
			public static object ToInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((int)closure.Unbox<decimal>(left)));
				else
					return unchecked(closure.Box((int)closure.Unbox<decimal>(left)));
			}
#if UNSIGNED_TYPES
			public static object ToUInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((uint)closure.Unbox<decimal>(left)));
				else
					return unchecked(closure.Box((uint)closure.Unbox<decimal>(left)));
			}
			public static object ToInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((long)closure.Unbox<decimal>(left)));
				else
					return unchecked(closure.Box((long)closure.Unbox<decimal>(left)));
			}
			public static object ToUInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ulong)closure.Unbox<decimal>(left)));
				else
					return unchecked(closure.Box((ulong)closure.Unbox<decimal>(left)));
			}
#endif
			public static object ToSingle(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((float)closure.Unbox<decimal>(left)));
				else
					return unchecked(closure.Box((float)closure.Unbox<decimal>(left)));
			}
			public static object ToDouble(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((double)closure.Unbox<decimal>(left)));
				else
					return unchecked(closure.Box((double)closure.Unbox<decimal>(left)));
			}
			public static object ToDecimal(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((decimal)closure.Unbox<decimal>(left)));
				else
					return unchecked(closure.Box((decimal)closure.Unbox<decimal>(left)));
			}
		}
	}
}
